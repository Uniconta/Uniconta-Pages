using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Crm;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TileData : INotifyPropertyChanged
    {
        private string header;
        private long count;
        private string tableName;
        private string value;
        private string statusText;
        private string tileType;
        private Visibility statusVisibility = Visibility.Collapsed;

        public string Header
        {
            get { return header; }
            set
            {
                if (header != value)
                {
                    header = value;
                    OnPropertyChanged(nameof(Header));
                }
            }
        }

        public long Count
        {
            get { return count; }
            set
            {
                if (count != value)
                {
                    count = value;
                    OnPropertyChanged(nameof(Count));
                }
            }
        }
        public string Value { get { return value; } set { this.value = value ?? string.Empty; OnPropertyChanged(nameof(Value)); } }
        public string StatusText { get { return statusText; } set { statusText = value; OnPropertyChanged(nameof(StatusText)); } }
        //public string LastUpdated { get { return lastUpdated; } set { lastUpdated = value; OnPropertyChanged(nameof(LastUpdated)); } }
        public string TileType { get { return tileType; } set { tileType = value; OnPropertyChanged(nameof(TileType)); } }
        public Visibility StatusVisibility { get { return statusVisibility; } set { statusVisibility = value; OnPropertyChanged(nameof(StatusVisibility)); } }
        public string TableName
        {
            get { return tableName; }
            set
            {
                tableName = value;
                OnPropertyChanged(nameof(TableName));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class TileControl : System.Windows.Controls.UserControl
    {
        TileData tile;
        UserMenuClient userMenu;
        private BasePage basePage;
        private CrudAPI Api => basePage?.Api;
        ITileDataSource dataSource;
        System.Windows.Controls.UserControl customTile;
        ITileType standaloneTile;
        bool isLoading, reloadDefinitionPending;
        UnicontaWebViewer htmlWidget;
        public TileDefinition Definition { get; private set; }
        public FrameworkElement DragHandle { get { return TileDragHandle; } }
        double resizeHorizontalChange;
        double resizeVerticalChange;
        public event EventHandler<TileResizeEventArgs> ResizePreview;
        public event EventHandler ResizeStarted;
        public event EventHandler<TileResizeEventArgs> ResizeCompleted;
        public event EventHandler ResizeCanceled;
        public event Action MenuRequested;
        bool isDeleteMode;
        public bool IsDeleteMode
        {
            get { return isDeleteMode; }
            set
            {
                isDeleteMode = value;
                btnEditTile.Content = value ? (object)true : "⋮";
                btnEditTile.ContentTemplate = value ? (DataTemplate)FindResource("DeleteIconTemplate") : null;
                btnEditTile.ToolTip = value ? Uniconta.ClientTools.Localization.lookup("Delete") : Uniconta.ClientTools.Localization.lookup("Edit");
                TileDragHandle.Visibility = ResizeGrip.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                TileDragHandle.IsEnabled = ResizeGrip.IsEnabled = value;
                if (!value)
                {
                    TileDragHandle.CancelDrag();
                    ResizeGrip.CancelDrag();
                    HideResizePreview();
                }
            }
        }
        public TileControl(BasePage tilePage, UserMenuClient userMenu)
        {
            InitializeComponent();
            basePage = tilePage;
            this.userMenu = userMenu;
            Definition = TileDefinition.FromUserMenu(userMenu);
            
            var tileData = new TileData() { Header = userMenu._Prompt, TileType = Definition.IsCustomTile ? Definition.TileTypeId : Definition.Visualization.ToString() };
            this.tile = tileData;
            this.DataContext = tileData;

            SizeChanged += (sender, args) => RenderChart();
            Unloaded += (sender, args) => CloseHtmlWidget();
            PreviewDragOver += TileControl_PreviewDragOver;
            PreviewDragLeave += TileControl_PreviewDragLeave;
            PreviewDrop += TileControl_PreviewDrop;

            _ = LoadAsync();
        }

        void TileControl_PreviewDragOver(object sender, System.Windows.DragEventArgs e)
        {
            var dropTarget = standaloneTile as ITileDropTarget;
            var filePaths = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
                ? e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[]
                : null;
            if (standaloneTile is VoucherApprovalTileType voucherTile && e.Data.GetDataPresent("FileGroupDescriptor"))
            {
                voucherTile.OnOutlookDragOver();
                e.Effects = voucherTile.CanAcceptOutlookDrop ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
                e.Handled = true;
                return;
            }
            if (dropTarget == null || filePaths == null)
                return;
            dropTarget.OnFileDragOver(filePaths);
            e.Effects = dropTarget.CanAcceptFileDrop(filePaths) ? System.Windows.DragDropEffects.Copy : System.Windows.DragDropEffects.None;
            e.Handled = true;
        }

        void TileControl_PreviewDragLeave(object sender, System.Windows.DragEventArgs e)
        {
            // Replacing the preview changes the child hit target; keep it visible until the pointer leaves or the drag ends.
            if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop) && !e.Data.GetDataPresent("FileGroupDescriptor"))
                return;
            var dragButtons = System.Windows.DragDropKeyStates.LeftMouseButton | System.Windows.DragDropKeyStates.RightMouseButton | System.Windows.DragDropKeyStates.MiddleMouseButton;
            if ((e.KeyStates & dragButtons) != 0 && new System.Windows.Rect(RenderSize).Contains(System.Windows.Input.Mouse.GetPosition(this)))
                return;
            (standaloneTile as ITileDropTarget)?.OnFileDragLeave();
        }

        async void TileControl_PreviewDrop(object sender, System.Windows.DragEventArgs e)
        {
            var dropTarget = standaloneTile as ITileDropTarget;
            var filePaths = e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)
                ? e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[]
                : null;
            if (standaloneTile is VoucherApprovalTileType voucherTile && e.Data.GetDataPresent("FileGroupDescriptor"))
            {
                e.Handled = true;
                await voucherTile.OnOutlookDropAsync(e.Data);
                return;
            }
            if (dropTarget == null || filePaths == null || filePaths.Length == 0)
                return;

            e.Handled = true;
            await dropTarget.OnFileDropAsync(filePaths);
        }



        void ResizeGrip_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (!IsDeleteMode)
            {
                ResizeGrip.CancelDrag();
                e.Handled = true;
                return;
            }
            resizeHorizontalChange = 0d;
            resizeVerticalChange = 0d;
            ResizeStarted?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        void ResizeGrip_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!IsDeleteMode)
                return;
            resizeHorizontalChange = e.HorizontalChange;
            resizeVerticalChange = e.VerticalChange;

            ResizePreview?.Invoke(this,
                new TileResizeEventArgs(resizeHorizontalChange, resizeVerticalChange));

            e.Handled = true;
        }

        void ResizeGrip_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            ResizeSizePopup.Visibility = Visibility.Collapsed;
            if (e.Canceled || !IsDeleteMode)
                ResizeCanceled?.Invoke(this, EventArgs.Empty);
            else
                ResizeCompleted?.Invoke(this, new TileResizeEventArgs(resizeHorizontalChange, resizeVerticalChange));
            e.Handled = true;
        }

        public void HideResizePreview() { ResizeSizePopup.Visibility = Visibility.Collapsed; }
        public void CancelResizeInteraction() { ResizeGrip.CancelDrag(); HideResizePreview(); }

        public void ShowResizePreview(int columnSpan, int rowSpan)
        {
            if (!IsDeleteMode)
                return;
            ResizeSizeText.Text = string.Format("{0} × {1}", columnSpan, rowSpan);
            ResizeSizePopup.Visibility = Visibility.Visible;
        }
        readonly List<ChartValue> chartValues = new List<ChartValue>();
        public List<Uniconta.ClientTools.Page.BasePage.ValuePair> GetFilterPair(string filterString)
        {
            if (string.IsNullOrEmpty(filterString))
                return null;
            var GridFilter = new List<Uniconta.ClientTools.Page.BasePage.ValuePair>();
            try
            {
                var buf = Convert.FromBase64String(filterString);
                var cs = StreamingManagerReuse.Create(buf);
                var filterValues = ((FilterProperties[])cs.ToArray(typeof(FilterProperties))).ToList();
                foreach (var filter in filterValues)
                {
                    GridFilter.Add(new Uniconta.ClientTools.Page.BasePage.ValuePair { Name = "Filter", Value = $"{filter.PropertyName}:{filter.UserInput}" });
                }
            }
            catch
            {
                var filters = Regex.Split(filterString, ";;;");
                foreach (var filter in filters)
                {
                    GridFilter.Add(new Uniconta.ClientTools.Page.BasePage.ValuePair { Name = "Filter", Value = filter });
                }
            }
            return GridFilter;
        }
        public async Task LoadAsync()
        {
            if (!Dispatcher.CheckAccess())
            {
                await Dispatcher.InvokeAsync(() => LoadAsync()).Task.Unwrap();
                return;
            }

            if (isLoading)
                return;

            isLoading = true;
            if (reloadDefinitionPending)
            {
                reloadDefinitionPending = false;
                Definition = TileDefinition.FromUserMenu(userMenu);
                dataSource = null;
                standaloneTile = null;
                customTile = null;
                TileTypeContent.Content = null;
                AllowDrop = false;
            }
            tile.StatusText = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            tile.StatusVisibility = Visibility.Visible;
            try
            {
                if (Definition.IsCustomTile)
                {
                    CloseHtmlWidget();
                    if (standaloneTile == null)
                    {
                        var type = TileMetadataService.ResolveStandaloneTileType(Definition.TileTypeId);
                        var instance = (ITileType)Activator.CreateInstance(type);
                        instance.Initialize(Api);
                        (instance as ITileTypeWithParms)?.Deserialize(Definition.TileTypeParameters);
                        customTile = instance.GetContent() ?? throw new InvalidOperationException("The tile type did not provide content.");
                        standaloneTile = instance;
                        TileTypeContent.Content = customTile;
                        TileDragHandle.Width = double.NaN;
                        TileDragHandle.Height = 42d;
                        TileDragHandle.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                        TileDragHandle.Margin = new Thickness(14d, 0d, 44d, 0d);
                    }
                    AllowDrop = standaloneTile is ITileDropTarget;
                    await standaloneTile.FetchAsync();
                    TileTypeContent.Visibility = Visibility.Visible;
                    tile.Header = Definition.Title;
                    tile.TableName = TileMetadataService.GetTileTypeCaption(standaloneTile.GetType());
                    tile.StatusVisibility = Visibility.Collapsed;
                    RenderChart();
                    return;
                }
                if (Definition.Visualization == TileVisualization.HtmlWidget)
                {
                    ShowHtmlWidget();
                    tile.TableName = Uniconta.ClientTools.Localization.lookup("Web");
                    tile.Header = Definition.Title;
                    tile.StatusVisibility = Visibility.Collapsed;
                    return;
                }

                CloseHtmlWidget();
                dataSource = dataSource ?? TileDataSource.Create(Api, Definition);
                var result = await dataSource.FetchAsync();
                if (result == null)
                {
                    Uniconta.ClientTools.InfoLog.Log.Warning($"Tile '{tile.Header}' not loaded.");
                    return;
                }
                chartValues.Clear();
                if (Definition.Visualization == TileVisualization.Number 
                &&  Definition.Aggregate == TileAggregate.Count 
                &&  result.Count.HasValue)
                {
                    tile.Count = result.Count.Value;
                    tile.Value = result.Count.Value.ToString("N0");
                }
                else
                {
                    var rows = result.Rows;
                    if (rows == null)
                        throw new InvalidOperationException("The tile datasource did not return rows for aggregation.");

                    if (Definition.Visualization == TileVisualization.RecordText)
                    {
                        var record = rows.FirstOrDefault();
                        tile.Value = record == null ? Uniconta.ClientTools.Localization.lookup("NoRecords") : TileRecordText.Format(record, Definition.ValueColumns);
                    }
                    else if (Definition.Visualization == TileVisualization.Number)
                        tile.Value = Aggregate(rows, Definition.Aggregate, Definition.ValueColumns?.FirstOrDefault()).ToString("N2");
                    else
                        BuildChartValues(rows, dataSource.DataType);
                }
                tile.TableName = dataSource.DisplayName;
                tile.Header = Definition.Title;
                //tile.LastUpdated = DateTime.Now.ToString("g");
                tile.StatusVisibility = Visibility.Collapsed;
                RenderChart();
            }
            catch (Exception ex)
            {
                tile.Value = "—";
                tile.StatusText = ex.Message;
                tile.StatusVisibility = Visibility.Visible;
                ChartSurface.Visibility = Visibility.Collapsed;
                RecordTextSurface.Visibility = Visibility.Collapsed;
                TileTypeContent.Visibility = Visibility.Collapsed;
            }
            finally
            {
                isLoading = false;
                if (reloadDefinitionPending) _ = LoadAsync();
            }
        }

        void BuildChartValues(IEnumerable<UnicontaBaseEntity> rows, Type rowType)
        {
            var groupProperty = rowType.GetProperty(Definition.GroupBy) ??
                CorasauDataGridTemplateColumn.GetPropertyFromFieldName(rowType, Definition.GroupBy);
            if (groupProperty == null)
                throw new InvalidOperationException("The configured group field is no longer available.");
            // Pie and donut have one series: aggregate each category across all chart groups,
            // including definitions saved before the wizard hid this unsupported setting.
            var chartGroup = Definition.Visualization.SupportsChartGroup() ? Definition.ChartGroup : null;
            var seriesProperty = string.IsNullOrWhiteSpace(chartGroup) ? null : rowType.GetProperty(chartGroup) ??
                CorasauDataGridTemplateColumn.GetPropertyFromFieldName(rowType, chartGroup);
            if (!string.IsNullOrWhiteSpace(chartGroup) && seriesProperty == null)
                throw new InvalidOperationException("The configured chart group field is no longer available.");

            var valueColumns = Definition.ValueColumns == null
                ? new List<string>()
                : Definition.ValueColumns.Where(column => !string.IsNullOrWhiteSpace(column)).Distinct().ToList();
            if (valueColumns.Count == 0)
                valueColumns.Add(null);

            foreach (var group in rows.GroupBy(row => new { Argument = NormalizeArgument(groupProperty.GetValue(row)), Series = seriesProperty == null ? null : FormatCategory(seriesProperty.GetValue(row)) }))
            {
                foreach (var valueColumn in valueColumns)
                {
                    var seriesName = group.Key.Series;
                    if (valueColumns.Count > 1)
                        seriesName = string.IsNullOrWhiteSpace(seriesName) ? valueColumn : group.Key.Series + " – " + valueColumn;
                    chartValues.Add(new ChartValue
                    {
                        Argument = group.Key.Argument,
                        Label = FormatCategory(group.Key.Argument),
                        Series = seriesName,
                        Value = Aggregate(group, Definition.Aggregate, valueColumn),
                        Value2 = AggregateOptional(group, Definition.Value2Field),
                        Weight = AggregateOptional(group, Definition.WeightField)
                    });
                }
            }
            if (Definition.Visualization == TileVisualization.Pie || Definition.Visualization == TileVisualization.Donut || Definition.Visualization == TileVisualization.Funnel)
                chartValues.Sort((left, right) => right.Value.CompareTo(left.Value));
            tile.Value = chartValues.Sum(item => item.Value).ToString("N2");
        }

        private decimal? AggregateOptional(IEnumerable<UnicontaBaseEntity> rows, string field) => string.IsNullOrWhiteSpace(field) ? (decimal?)null : Aggregate(rows, Definition.Aggregate, field);

        private object NormalizeArgument(object value)
        {
            if (!(value is DateTime date)) return value ?? "—";
            if (Definition.DateBucket == TileDateBucket.Year) return new DateTime(date.Year, 1, 1);
            if (Definition.DateBucket == TileDateBucket.Month) return new DateTime(date.Year, date.Month, 1);
            return date;
        }

        private string FormatCategory(object value)
        {
            if (value == null)
                return "—";
            if (value is DateTime date)
            {
                if (Definition.DateBucket == TileDateBucket.Year) return date.ToString("yyyy");
                if (Definition.DateBucket == TileDateBucket.Month) return date.ToString("MMM yyyy");
                return date.ToString("d");
            }
            return Convert.ToString(value, CultureInfo.CurrentCulture);
        }

        static decimal Aggregate(IEnumerable<UnicontaBaseEntity> rows, TileAggregate aggregate, string columnName)
        {
            var materialized = rows.ToList();
            if (aggregate == TileAggregate.Count)
                return materialized.Count;
            if (materialized.Count == 0)
                return 0m;
            var property = materialized[0].GetType().GetProperty(columnName) ??
                CorasauDataGridTemplateColumn.GetPropertyFromFieldName(materialized[0].GetType(), columnName);
            if (property == null)
                throw new InvalidOperationException("The configured aggregate field is no longer available.");
            var values = materialized.Select(row => property.GetValue(row))
                .Where(value => value != null)
                .Select(value => Convert.ToDecimal(value, CultureInfo.InvariantCulture)).ToList();
            if (values.Count == 0) return 0m;
            switch (aggregate)
            {
                case TileAggregate.Sum: return values.Sum();
                case TileAggregate.Average: return values.Average();
                case TileAggregate.Minimum: return values.Min();
                case TileAggregate.Maximum: return values.Max();
                default: return values.Count;
            }
        }
        
        private static Uri BuildHtmlWidgetUri(string htmlSource, string theme)
        {
            Uri source;
            if (!Uri.TryCreate(htmlSource, UriKind.Absolute, out source) ||
                (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps))
                throw new InvalidOperationException("The configured widget URL must use HTTP or HTTPS.");

            var builder = new UriBuilder(source);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["theme"] = theme;
            builder.Query = query.ToString();
            return builder.Uri;
        }

        void ShowHtmlWidget()
        {
            CloseHtmlWidget();
            var source = BuildHtmlWidgetUri(Definition.ResolveHtmlSource(Api?.CompanyEntity?._CountryId), AppEnums.Theme.Label(BasePage.session.User._Theme));

            htmlWidget = new UnicontaWebViewer(source) { ShowNavigationError = false };
            HtmlWidgetHost.Children.Add(htmlWidget);
            HtmlWidgetHost.Visibility = Visibility.Visible;
            ChartSurface.Visibility = Visibility.Collapsed;
            NumberValue.Visibility = Visibility.Collapsed;
            RecordTextSurface.Visibility = Visibility.Collapsed;
        }

        void CloseHtmlWidget()
        {
            HtmlWidgetHost.Children.Clear();
            HtmlWidgetHost.Visibility = Visibility.Collapsed;
            if (htmlWidget != null)
            {
                htmlWidget.CloseUnicontaWebViewer();
                htmlWidget = null;
            }
        }

        void RenderChart()
        {
            ChartCanvas.Children.Clear();
            ChartLegend.ItemsSource = null;
            PieChart.DataSource = null;
            TileChart.Diagram = null;
            if (Definition.IsCustomTile)
            {
                ChartSurface.Visibility = NumberValue.Visibility = RecordTextSurface.Visibility = Visibility.Collapsed;
                return;
            }
            var isRecordText = Definition.Visualization == TileVisualization.RecordText;
            var isChart = Definition.Visualization != TileVisualization.Number && !isRecordText && Definition.Visualization != TileVisualization.HtmlWidget;
            ChartSurface.Visibility = isChart ? Visibility.Visible : Visibility.Collapsed;
            NumberValue.Visibility = isChart || isRecordText || Definition.Visualization == TileVisualization.HtmlWidget ? Visibility.Collapsed : Visibility.Visible;
            RecordTextSurface.Visibility = isRecordText ? Visibility.Visible : Visibility.Collapsed;
            var isPie = Definition.Visualization == TileVisualization.Pie || Definition.Visualization == TileVisualization.Donut;
            PieChart.Visibility = isPie ? Visibility.Visible : Visibility.Collapsed;
            TileChart.Visibility = isChart && !isPie ? Visibility.Visible : Visibility.Collapsed;
            CustomChartSurface.Visibility = Visibility.Collapsed;
            if (!isChart || chartValues.Count == 0)
                return;
            if (isPie)
                RenderPie(Definition.Visualization == TileVisualization.Donut);
            else
                RenderDevExpressChart();
        }

        void RenderDevExpressChart()
        {
            ValidateChartData();
            var diagram = CreateDiagram();
            var groupedValues = chartValues.GroupBy(item => item.Series ?? string.Empty).ToList();
            foreach (var values in groupedValues)
            {
                var series = CreateSeries();
                series.DisplayName = string.IsNullOrWhiteSpace(values.Key) ? Definition.ValueColumns?.FirstOrDefault() ?? Definition.Aggregate.DisplayName() : values.Key;
                series.ArgumentDataMember = nameof(ChartValue.Argument);
                series.ValueDataMember = nameof(ChartValue.Value);
                series.DataSource = values.ToList();
                series.LabelsVisibility = false;
                var categoryLegend = Definition.Visualization == TileVisualization.Funnel || Definition.Visualization == TileVisualization.NestedDonut;
                series.ShowInLegend = categoryLegend || groupedValues.Count > 1;
                series.LegendTextPattern = categoryLegend ? "{A}" : "{S}";
                SetSeriesProperty(series, "Value2DataMember", nameof(ChartValue.Value2));
                SetSeriesProperty(series, "WeightDataMember", nameof(ChartValue.Weight));
                SetSeriesProperty(series, "ToolTipPointPattern", groupedValues.Count > 1 ? "{S}\n{A}: {V:G}" : "{A}: {V:G}");
                diagram.Series.Add(series);
            }
            TileChart.Diagram = diagram;
        }

        void ValidateChartData()
        {
            if (Definition.Visualization.RequiresSeries() && chartValues.All(item => string.IsNullOrWhiteSpace(item.Series)))
                throw new InvalidOperationException("This chart requires a Chart group or multiple configured measures.");
            if (Definition.Visualization.RequiresValue2() && chartValues.Any(item => !item.Value2.HasValue))
                throw new InvalidOperationException("This range chart requires a second value field.");
            if (Definition.Visualization == TileVisualization.Bubble && chartValues.Any(item => !item.Weight.HasValue))
                throw new InvalidOperationException("This bubble chart requires a weight field.");
        }

        DevExpress.Xpf.Charts.Diagram CreateDiagram()
        {
            if (Definition.Visualization == TileVisualization.NestedDonut || Definition.Visualization == TileVisualization.Funnel)
                return new DevExpress.Xpf.Charts.SimpleDiagram2D();
            if (Definition.Visualization >= TileVisualization.RadarPoint && Definition.Visualization <= TileVisualization.RadarRangeArea)
                return new DevExpress.Xpf.Charts.RadarDiagram2D();
            if (Definition.Visualization >= TileVisualization.PolarPoint && Definition.Visualization <= TileVisualization.PolarRangeArea)
                return new DevExpress.Xpf.Charts.PolarDiagram2D();
            return new DevExpress.Xpf.Charts.XYDiagram2D
            {
                AxisX = new DevExpress.Xpf.Charts.AxisX2D { Label = new DevExpress.Xpf.Charts.AxisLabel { Staggered = true } },
                AxisY = new DevExpress.Xpf.Charts.AxisY2D()
            };
        }

        DevExpress.Xpf.Charts.Series CreateSeries()
        {
            switch (Definition.Visualization)
            {
                case TileVisualization.NestedDonut: return new DevExpress.Xpf.Charts.NestedDonutSeries2D { LegendTextPattern = "{A}" };
                case TileVisualization.Funnel: return new DevExpress.Xpf.Charts.FunnelSeries2D { LegendTextPattern = "{A}", ShowInLegend = true };
                case TileVisualization.SideBySideBar:
                case TileVisualization.BarChart:
                case TileVisualization.ColumnChart: return new DevExpress.Xpf.Charts.BarSideBySideSeries2D();
                case TileVisualization.SideBySideStackedBar: return new DevExpress.Xpf.Charts.BarSideBySideStackedSeries2D();
                case TileVisualization.SideBySideFullStackedBar: return new DevExpress.Xpf.Charts.BarSideBySideFullStackedSeries2D();
                case TileVisualization.StackedBar: return new DevExpress.Xpf.Charts.BarStackedSeries2D();
                case TileVisualization.FullStackedBar: return new DevExpress.Xpf.Charts.BarFullStackedSeries2D();
                case TileVisualization.OverlappedRangeBar: return new DevExpress.Xpf.Charts.RangeBarOverlappedSeries2D();
                case TileVisualization.SideBySideRangeBar: return new DevExpress.Xpf.Charts.RangeBarSideBySideSeries2D();
                case TileVisualization.Waterfall: return new DevExpress.Xpf.Charts.WaterfallSeries2D();
                case TileVisualization.Point: return new DevExpress.Xpf.Charts.PointSeries2D();
                case TileVisualization.LineChart: return new DevExpress.Xpf.Charts.LineSeries2D();
                case TileVisualization.StackedLine: return new DevExpress.Xpf.Charts.LineStackedSeries2D();
                case TileVisualization.FullStackedLine: return new DevExpress.Xpf.Charts.LineFullStackedSeries2D();
                case TileVisualization.StepLine: return new DevExpress.Xpf.Charts.LineStepSeries2D();
                case TileVisualization.ScatterLine: return new DevExpress.Xpf.Charts.LineScatterSeries2D();
                case TileVisualization.Spline: return new DevExpress.Xpf.Charts.SplineSeries2D();
                case TileVisualization.Bubble: return new DevExpress.Xpf.Charts.BubbleSeries2D();
                case TileVisualization.AreaChart: return new DevExpress.Xpf.Charts.AreaSeries2D();
                case TileVisualization.SplineArea: return new DevExpress.Xpf.Charts.SplineAreaSeries2D();
                case TileVisualization.StepArea: return new DevExpress.Xpf.Charts.AreaStepSeries2D();
                case TileVisualization.StackedArea: return new DevExpress.Xpf.Charts.AreaStackedSeries2D();
                case TileVisualization.StackedSplineArea: return new DevExpress.Xpf.Charts.SplineAreaStackedSeries2D();
                case TileVisualization.StackedStepArea: return new DevExpress.Xpf.Charts.AreaStepStackedSeries2D();
                case TileVisualization.FullStackedArea: return new DevExpress.Xpf.Charts.AreaFullStackedSeries2D();
                case TileVisualization.FullStackedSplineArea: return new DevExpress.Xpf.Charts.SplineAreaFullStackedSeries2D();
                case TileVisualization.FullStackedStepArea: return new DevExpress.Xpf.Charts.AreaStepFullStackedSeries2D();
                case TileVisualization.RangeArea: return new DevExpress.Xpf.Charts.RangeAreaSeries2D();
                case TileVisualization.RadarPoint: return new DevExpress.Xpf.Charts.RadarPointSeries2D();
                case TileVisualization.RadarLine: return new DevExpress.Xpf.Charts.RadarLineSeries2D();
                case TileVisualization.RadarScatterLine: return new DevExpress.Xpf.Charts.RadarLineScatterSeries2D();
                case TileVisualization.RadarArea: return new DevExpress.Xpf.Charts.RadarAreaSeries2D();
                case TileVisualization.RadarRangeArea: return new DevExpress.Xpf.Charts.RadarRangeAreaSeries2D();
                case TileVisualization.PolarPoint: return new DevExpress.Xpf.Charts.PolarPointSeries2D();
                case TileVisualization.PolarLine: return new DevExpress.Xpf.Charts.PolarLineSeries2D();
                case TileVisualization.PolarScatterLine: return new DevExpress.Xpf.Charts.PolarLineScatterSeries2D();
                case TileVisualization.PolarArea: return new DevExpress.Xpf.Charts.PolarAreaSeries2D();
                case TileVisualization.PolarRangeArea: return new DevExpress.Xpf.Charts.PolarRangeAreaSeries2D();
                default: throw new InvalidOperationException("The selected chart visualization is not supported.");
            }
        }

        static void SetSeriesProperty(DevExpress.Xpf.Charts.Series series, string name, object value)
        {
            var property = series.GetType().GetProperty(name);
            if (property != null && property.CanWrite) property.SetValue(series, value, null);
        }

        void RenderPie(bool donut)
        {
            var positive = chartValues.Where(item => item.Value > 0m).ToList();
            TilePieSeries.HoleRadiusPercent = donut ? 55 : 0;
            PieChart.DataSource = positive;
        }

        sealed class ChartValue
        {
            public object Argument { get; set; }
            public string Label { get; set; }
            public string Series { get; set; }
            public decimal Value { get; set; }
            public decimal? Value2 { get; set; }
            public decimal? Weight { get; set; }
        }

        public static string GetTypeDisplayName(Type type)
        {
            return GetTypeDisplayName(type, null);
        }

        public static string GetTypeDisplayName(Type type, Company company)
        {
            if (company?.UserTables != null && typeof(BaseUserTable).IsAssignableFrom(type))
            {
                var userTable = (BaseUserTable)Activator.CreateInstance(type);
                var definition = company.UserTables.FirstOrDefault(table => userTable.TableNo != 0 ? table.RowId == userTable.TableNo :
                    userTable.UserDefinedId != 0 ? table._UserDefinedId == userTable.UserDefinedId : table.UserType == type || table._Name == type.Name);
                var caption = !string.IsNullOrWhiteSpace(definition?._Prompt) ? UserFieldControl.LocalizePrompt(definition._Prompt) : definition?._Name;
                if (!string.IsNullOrWhiteSpace(caption))
                    return caption;
            }
            var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
            if (clientTableAttr.Length > 0)
            {
                var attr = (ClientTableAttribute)clientTableAttr[0];
                return Uniconta.ClientTools.Localization.lookup(attr.LabelKey);
            }
            else
                return type.Name;
        }
        public delegate void OnDeleted();
        public event OnDeleted Deleted;
        public event Action Changed;

        private async void btnEditTile_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeleteMode)
            {
                if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"),
                    Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                    return;
                var result = await new TileCRUDController(Api).DeleteAsync(userMenu);
                if (result == ErrorCodes.Succes)
                    Deleted?.Invoke();
                else
                    Uniconta.ClientTools.InfoLog.Log.Error(result);
                return;
            }

            if (MenuRequested != null)
            {
                MenuRequested();
                return;
            }

            EditTile();
        }

        internal void EditTile()
        {
            var editTile = new CWTileDetails(Api, userMenu);
            editTile.Closed += (s, args) =>
            {
                if (editTile.DialogResult == true)
                {
                    if (editTile.isDeleted)
                        Deleted?.Invoke();
                    else
                    {
                        if (Changed != null) Changed.Invoke();
                        else
                        {
                            reloadDefinitionPending = true;
                            _ = LoadAsync();
                        }
                    }
                }
            };
            editTile.Show();
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadAsync();
        }
    }

    public sealed class TileResizeEventArgs : EventArgs
    {
        public TileResizeEventArgs(double horizontalChange, double verticalChange)
        {
            HorizontalChange = horizontalChange;
            VerticalChange = verticalChange;
        }
        public double HorizontalChange { get; private set; }
        public double VerticalChange { get; private set; }
    }
}
