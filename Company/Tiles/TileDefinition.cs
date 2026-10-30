using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public sealed class TileDefinition
    {
        public const int CurrentVersion = 12;

        public int Version { get; set; } = CurrentVersion;
        public int TableId { get; set; }
        // Null preserves the existing visualization/table configuration.
        public string TileTypeId { get; set; }
        public string TileTypeParameters { get; set; }
        public string SourceName { get; set; }
        [JsonIgnore] public bool IsCustomTile => !string.IsNullOrWhiteSpace(TileTypeId);
        public string Title { get; set; }
        public string Group { get; set; }
        public string Filter { get; set; }
        public string NavigationTarget { get; set; }
        public string HtmlSource { get; set; }
        /*[JsonIgnore]*/ public TileVisualization Visualization { get; set; } = TileVisualization.Number;
        /*[JsonIgnore]*/ public TileAggregate Aggregate { get; set; } = TileAggregate.Count;
        /*[JsonIgnore]*/ public bool IsDashboardDefinition { get; set; }
        /*[JsonIgnore]*/ public TileDateBucket DateBucket { get; set; } = TileDateBucket.None;
        public TileScope Scope { get; set; } = TileScope.User;
        public string DashboardTab { get; set; }
        public string Profile { get; set; }
        public string GroupBy { get; set; }
        public List<string> ValueColumns { get; set; } = new List<string>();
        public string ChartGroup { get; set; }
        public string Value2Field { get; set; }
        public string WeightField { get; set; }
        public int Order { get; set; }
        public int ColumnSpan { get; set; } = 1;
        public int RowSpan { get; set; } = 1;
        public int? GridColumn { get; set; }
        public int? GridRow { get; set; }

        public void Validate()
        {
            if (Scope == TileScope.Profile && string.IsNullOrWhiteSpace(Profile))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileSelectAProfile"), nameof(Profile));

            if (IsDashboardDefinition)
            {
                if (string.IsNullOrWhiteSpace(DashboardTab))
                    throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileADashboardNameIsRequired"), nameof(DashboardTab));
                return;
            }
            if (!IsCustomTile && Visualization == TileVisualization.HtmlWidget)
            {
                Uri source;
                if (!Uri.TryCreate(HtmlSource, UriKind.Absolute, out source) ||
                    (source.Scheme != Uri.UriSchemeHttp && source.Scheme != Uri.UriSchemeHttps))
                    throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileAnAbsoluteHTTPOrHTTPSWidgetURLIs"), nameof(HtmlSource));
            }
            else if (!IsCustomTile && TableId <= 0 && string.IsNullOrWhiteSpace(SourceName))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileATableIsRequired"), nameof(TableId));

            if (!IsCustomTile && Visualization == TileVisualization.RecordText && (ValueColumns == null || ValueColumns.All(string.IsNullOrWhiteSpace)))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileAtLeastOneDisplayFieldIsRequiredFor"), nameof(ValueColumns));

            if (!IsCustomTile && Visualization != TileVisualization.RecordText && Visualization != TileVisualization.HtmlWidget && Aggregate != TileAggregate.Count &&
                (ValueColumns == null || ValueColumns.All(string.IsNullOrWhiteSpace)))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileAValueColumnIsRequiredForThisAggregate"), nameof(ValueColumns));

            if (!IsCustomTile && !Visualization.SupportsMultipleValues() && ValueColumns != null &&
                ValueColumns.Count(column => !string.IsNullOrWhiteSpace(column)) > 1)
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileThisVisualizationSupportsOnlyOneValueColumn"), nameof(ValueColumns));

            if (!IsCustomTile && Visualization != TileVisualization.Number && Visualization != TileVisualization.RecordText && Visualization != TileVisualization.HtmlWidget &&
                string.IsNullOrWhiteSpace(GroupBy))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileGroupByIsRequiredForChartTiles"), nameof(GroupBy));

            if (!IsCustomTile && Visualization.RequiresSeries() && string.IsNullOrWhiteSpace(ChartGroup) &&
                (ValueColumns == null || ValueColumns.Count(column => !string.IsNullOrWhiteSpace(column)) < 2))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileThisVisualizationRequiresAChartGroupOrAt"), nameof(ChartGroup));

            if (!IsCustomTile && Visualization.RequiresValue2() && string.IsNullOrWhiteSpace(Value2Field))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileThisVisualizationRequiresASecondValueField"), nameof(Value2Field));

            if (!IsCustomTile && Visualization == TileVisualization.Bubble && string.IsNullOrWhiteSpace(WeightField))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileBubbleChartsRequireAWeightField"), nameof(WeightField));

            if (ColumnSpan < 1)
                throw new ArgumentOutOfRangeException(nameof(ColumnSpan), Uniconta.ClientTools.Localization.lookup("TileColumnSpanMustBeAtLeastOne"));

            if (RowSpan < 1)
                throw new ArgumentOutOfRangeException(nameof(RowSpan), Uniconta.ClientTools.Localization.lookup("TileRowSpanMustBeAtLeastOne"));

            if (GridColumn.HasValue != GridRow.HasValue)
                throw new ArgumentException("Grid column and row must be specified together.");

            if (GridColumn < 0 || GridRow < 0)
                throw new ArgumentOutOfRangeException(nameof(GridColumn), "Grid coordinates must be nonnegative.");
        }

        internal static TileDefinition FromUserMenu(UserMenuClient menu)
        {
            TileDefinition definition = null;

            if (!string.IsNullOrWhiteSpace(menu._Args))
            {
                try { definition = JsonConvert.DeserializeObject<TileDefinition>(menu._Args); }
                catch (JsonException) { }
            }

            definition = definition ?? new TileDefinition();
            definition.Version = CurrentVersion;
            definition.TableId = menu._TableId;
            definition.Title = menu._Prompt ?? definition.Title;
            definition.Group = menu._RibbonMenu ?? definition.Group;
            definition.Filter = menu._Filter ?? definition.Filter;
            definition.NavigationTarget = menu._Control ?? definition.NavigationTarget;
            definition.Order = menu._LineNumber;
            definition.Scope = !string.IsNullOrWhiteSpace(menu._Profile) ? TileScope.Profile : menu.RowId == 0 || menu._Uid != 0 ? TileScope.User : TileScope.Company;
            definition.Profile = menu._Profile;
            /*
            definition.Visualization = menu.TileVisualization;
            definition.Aggregate = menu.TileAggregate;
            definition.DateBucket = menu.TileDateBucket;
            definition.IsDashboardDefinition = menu.IsDashboardDefinition;
            */
            if (definition.GridColumn.HasValue != definition.GridRow.HasValue || definition.GridColumn < 0 || definition.GridRow < 0)
                definition.GridColumn = definition.GridRow = null;

            return definition;
        }

        internal void ApplyTo(UserMenuClient menu)
        {
            menu._Profile = Scope == TileScope.Profile ? Profile?.Trim() : null;
            menu._IsUserTile = Scope == TileScope.User;
            menu._IsCompanyTile = Scope != TileScope.User;
            menu._TableId = TableId;
            menu._Prompt = Title;
            menu._RibbonMenu = Group;
            menu._Filter = Filter;
            menu._Control = NavigationTarget;
            menu._LineNumber = Order;
            /*
            menu.TileVisualization = Visualization;
            menu.TileAggregate = Aggregate;
            menu.TileDateBucket = DateBucket;
            menu.IsDashboardDefinition = IsDashboardDefinition;
            */

            menu._Args = JsonConvert.SerializeObject(this, Formatting.None);
        }
    }

    internal static class TileTableCapabilities
    {
        internal static bool SupportsLocalAggregation(Type tableType) => tableType != null && typeof(IdKey).IsAssignableFrom(tableType);
        internal static TileVisualization[] GetVisualizations(Type tableType) => SupportsLocalAggregation(tableType)? Enum.GetValues(typeof(TileVisualization)).Cast<TileVisualization>().ToArray() : new[] { TileVisualization.Number, TileVisualization.HtmlWidget };

        internal static TileAggregate[] GetAggregates(Type tableType)
        {
            return SupportsLocalAggregation(tableType)
                ? Enum.GetValues(typeof(TileAggregate)).Cast<TileAggregate>().ToArray()
                : new[] { TileAggregate.Count };
        }

        internal static void Validate(Type tableType, TileDefinition definition)
        {
            if (definition.IsCustomTile || definition.IsDashboardDefinition || definition.Visualization == TileVisualization.HtmlWidget)
                return;

            if (!GetVisualizations(tableType).Contains(definition.Visualization)
                || !GetAggregates(tableType).Contains(definition.Aggregate))
                throw new ArgumentException("Aggregation and chart tiles require an IdKey table.");
        }
    }

    internal static class TileVisualizationExtensions
    {
        internal static string DisplayName(this TileVisualization value)
        {
            switch (value)
            {
                case TileVisualization.Number: return Uniconta.ClientTools.Localization.lookup("Number");
                case TileVisualization.BarChart: return Uniconta.ClientTools.Localization.lookup("TileVisualizationBarChart");
                case TileVisualization.LineChart: return Uniconta.ClientTools.Localization.lookup("TileVisualizationLineChart");
                case TileVisualization.Pie: return Uniconta.ClientTools.Localization.lookup("TileVisualizationPie");
                case TileVisualization.Donut: return Uniconta.ClientTools.Localization.lookup("TileVisualizationDonut");
                case TileVisualization.AreaChart: return Uniconta.ClientTools.Localization.lookup("TileVisualizationAreaChart");
                case TileVisualization.ColumnChart: return Uniconta.ClientTools.Localization.lookup("TileVisualizationColumnChart");
                case TileVisualization.HtmlWidget: return Uniconta.ClientTools.Localization.lookup("TileVisualizationHtmlWidget");
                case TileVisualization.NestedDonut: return Uniconta.ClientTools.Localization.lookup("TileVisualizationNestedDonut");
                case TileVisualization.SideBySideBar: return Uniconta.ClientTools.Localization.lookup("TileVisualizationSideBySideBar");
                case TileVisualization.SideBySideStackedBar: return Uniconta.ClientTools.Localization.lookup("TileVisualizationSideBySideStackedBar");
                case TileVisualization.SideBySideFullStackedBar: return Uniconta.ClientTools.Localization.lookup("TileVisualizationSideBySideFullStackedBar");
                case TileVisualization.StackedBar: return Uniconta.ClientTools.Localization.lookup("TileVisualizationStackedBar");
                case TileVisualization.FullStackedBar: return Uniconta.ClientTools.Localization.lookup("TileVisualizationFullStackedBar");
                case TileVisualization.OverlappedRangeBar: return Uniconta.ClientTools.Localization.lookup("TileVisualizationOverlappedRangeBar");
                case TileVisualization.SideBySideRangeBar: return Uniconta.ClientTools.Localization.lookup("TileVisualizationSideBySideRangeBar");
                case TileVisualization.Waterfall: return Uniconta.ClientTools.Localization.lookup("TileVisualizationWaterfall");
                case TileVisualization.Point: return Uniconta.ClientTools.Localization.lookup("TileVisualizationPoint");
                case TileVisualization.StackedLine: return Uniconta.ClientTools.Localization.lookup("TileVisualizationStackedLine");
                case TileVisualization.FullStackedLine: return Uniconta.ClientTools.Localization.lookup("TileVisualizationFullStackedLine");
                case TileVisualization.StepLine: return Uniconta.ClientTools.Localization.lookup("TileVisualizationStepLine");
                case TileVisualization.ScatterLine: return Uniconta.ClientTools.Localization.lookup("TileVisualizationScatterLine");
                case TileVisualization.Spline: return Uniconta.ClientTools.Localization.lookup("TileVisualizationSpline");
                case TileVisualization.Bubble: return Uniconta.ClientTools.Localization.lookup("TileVisualizationBubble");
                case TileVisualization.SplineArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationSplineArea");
                case TileVisualization.StepArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationStepArea");
                case TileVisualization.StackedArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationStackedArea");
                case TileVisualization.StackedSplineArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationStackedSplineArea");
                case TileVisualization.StackedStepArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationStackedStepArea");
                case TileVisualization.FullStackedArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationFullStackedArea");
                case TileVisualization.FullStackedSplineArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationFullStackedSplineArea");
                case TileVisualization.FullStackedStepArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationFullStackedStepArea");
                case TileVisualization.RangeArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationRangeArea");
                case TileVisualization.Funnel: return Uniconta.ClientTools.Localization.lookup("TileVisualizationFunnel");
                case TileVisualization.RadarPoint: return Uniconta.ClientTools.Localization.lookup("TileVisualizationRadarPoint");
                case TileVisualization.RadarLine: return Uniconta.ClientTools.Localization.lookup("TileVisualizationRadarLine");
                case TileVisualization.RadarScatterLine: return Uniconta.ClientTools.Localization.lookup("TileVisualizationRadarScatterLine");
                case TileVisualization.RadarArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationRadarArea");
                case TileVisualization.RadarRangeArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationRadarRangeArea");
                case TileVisualization.PolarPoint: return Uniconta.ClientTools.Localization.lookup("TileVisualizationPolarPoint");
                case TileVisualization.PolarLine: return Uniconta.ClientTools.Localization.lookup("TileVisualizationPolarLine");
                case TileVisualization.PolarScatterLine: return Uniconta.ClientTools.Localization.lookup("TileVisualizationPolarScatterLine");
                case TileVisualization.PolarArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationPolarArea");
                case TileVisualization.PolarRangeArea: return Uniconta.ClientTools.Localization.lookup("TileVisualizationPolarRangeArea");
                case TileVisualization.RecordText: return Uniconta.ClientTools.Localization.lookup("TileVisualizationRecordText");
                default: return value.ToString();
            }
        }

        internal static string DisplayName(this TileAggregate value)
        {
            switch (value)
            {
                case TileAggregate.Count: return Uniconta.ClientTools.Localization.lookup("Count");
                case TileAggregate.Sum: return Uniconta.ClientTools.Localization.lookup("Sum");
                case TileAggregate.Average: return Uniconta.ClientTools.Localization.lookup("Average");
                case TileAggregate.Minimum: return Uniconta.ClientTools.Localization.lookup("Min");
                case TileAggregate.Maximum: return Uniconta.ClientTools.Localization.lookup("Max");
                default: return value.ToString();
            }
        }

        internal static string DisplayName(this TileScope value)
        {
            switch (value)
            {
                case TileScope.User: return Uniconta.ClientTools.Localization.lookup("User");
                case TileScope.Profile: return Uniconta.ClientTools.Localization.lookup("Profile");
                case TileScope.Company: return Uniconta.ClientTools.Localization.lookup("Company");
                default: return value.ToString();
            }
        }


        internal static bool SupportsChartGroup(this TileVisualization visualization)
        {
            return visualization != TileVisualization.Number && visualization != TileVisualization.RecordText &&
                   visualization != TileVisualization.HtmlWidget && visualization != TileVisualization.Pie &&
                   visualization != TileVisualization.Donut;
        }

        internal static bool RequiresSeries(this TileVisualization visualization)
        {
            switch (visualization)
            {
                case TileVisualization.NestedDonut:
                case TileVisualization.SideBySideStackedBar:
                case TileVisualization.SideBySideFullStackedBar:
                case TileVisualization.StackedBar:
                case TileVisualization.FullStackedBar:
                case TileVisualization.StackedLine:
                case TileVisualization.FullStackedLine:
                case TileVisualization.StackedArea:
                case TileVisualization.StackedSplineArea:
                case TileVisualization.StackedStepArea:
                case TileVisualization.FullStackedArea:
                case TileVisualization.FullStackedSplineArea:
                case TileVisualization.FullStackedStepArea:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool RequiresValue2(this TileVisualization visualization)
        {
            return visualization == TileVisualization.RangeArea ||
                   visualization == TileVisualization.OverlappedRangeBar ||
                   visualization == TileVisualization.SideBySideRangeBar ||
                   visualization == TileVisualization.RadarRangeArea ||
                   visualization == TileVisualization.PolarRangeArea;
        }

        internal static bool SupportsMultipleValues(this TileVisualization visualization)
        {
            switch (visualization)
            {
                case TileVisualization.Number:
                case TileVisualization.Pie:
                case TileVisualization.Donut:
                case TileVisualization.HtmlWidget:
                case TileVisualization.NestedDonut:
                case TileVisualization.OverlappedRangeBar:
                case TileVisualization.SideBySideRangeBar:
                case TileVisualization.Waterfall:
                case TileVisualization.Bubble:
                case TileVisualization.RangeArea:
                case TileVisualization.Funnel:
                case TileVisualization.RadarRangeArea:
                case TileVisualization.PolarRangeArea:
                    return false;
                default:
                    return true;
            }
        }
    }

    public enum TileScope { User = 0, Company = 1, Profile = 2 }


    #region UI



    public sealed class TileGridPanel : System.Windows.Controls.Panel
    {
        const int MinimumEditingRows = 3;
        const int SpareEditingRows = 2;
        private readonly TileLayoutService layout = new TileLayoutService();
        private TileLayouts plan;

        public static readonly DependencyProperty IsEditingProperty = DependencyProperty.Register("IsEditing",
            typeof(bool), typeof(TileGridPanel), new FrameworkPropertyMetadata(false,
                FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public bool IsEditing { get { return (bool)GetValue(IsEditingProperty); } set { SetValue(IsEditingProperty, value); } }

        public static readonly DependencyProperty ColumnSpanProperty =
            DependencyProperty.RegisterAttached("ColumnSpan", 
                typeof(int), 
                typeof(TileGridPanel),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty RowSpanProperty =
            DependencyProperty.RegisterAttached("RowSpan", 
                typeof(int), 
                typeof(TileGridPanel),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty GridColumnProperty = DependencyProperty.RegisterAttached("GridColumn",
            typeof(int), typeof(TileGridPanel), new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static readonly DependencyProperty GridRowProperty = DependencyProperty.RegisterAttached("GridRow",
            typeof(int), typeof(TileGridPanel), new FrameworkPropertyMetadata(-1, FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        public static void SetColumnSpan(DependencyObject element, int value) { element.SetValue(ColumnSpanProperty, value); }
        public static int GetColumnSpan(DependencyObject element) { return (int)element.GetValue(ColumnSpanProperty); }

        public static void SetRowSpan(DependencyObject element, int value) { element.SetValue(RowSpanProperty, value); }
        public static int GetRowSpan(DependencyObject element) { return (int)element.GetValue(RowSpanProperty); }

        public static void SetGridColumn(DependencyObject element, int value) { element.SetValue(GridColumnProperty, value); }
        public static int GetGridColumn(DependencyObject element) { return (int)element.GetValue(GridColumnProperty); }
        public static void SetGridRow(DependencyObject element, int value) { element.SetValue(GridRowProperty, value); }
        public static int GetGridRow(DependencyObject element) { return (int)element.GetValue(GridRowProperty); }

        public int AvailableColumns { get; private set; }
        internal TileLayouts CurrentPlan { get { return plan; } }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            var width = ResolveWidth(availableSize.Width);
            AvailableColumns = layout.GetColumnCount(width);

            var items = new List<TileLayoutItem>();
            foreach (UIElement child in InternalChildren)
            {
                var column = GetGridColumn(child);
                var row = GetGridRow(child);
                items.Add(new TileLayoutItem(child, GetColumnSpan(child), GetRowSpan(child),
                    column < 0 ? (int?)null : column, row < 0 ? (int?)null : row));
            }

            plan = layout.CreatePlan(items, AvailableColumns);

            foreach (UIElement child in InternalChildren)
            {
                var rect = plan[child];
                child.Measure(new System.Windows.Size(rect.Width, rect.Height));
            }

            var height = plan.Height;
            if (IsEditing)
            {
                var rows = plan.Count == 0 ? 0 : plan.Values.Max(rect => rect.Row + rect.RowSpan);
                height = layout.GetHeight(Math.Max(MinimumEditingRows, rows + SpareEditingRows));
            }
            return new System.Windows.Size(width, height);
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            if (plan == null)
                MeasureOverride(finalSize);

            foreach (UIElement child in InternalChildren)
            {
                var rect = plan[child];
                child.Arrange(new Rect(rect.X, rect.Y, rect.Width, rect.Height));
            }

            return finalSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (!IsEditing)
                return;
            var brush = TryFindResource("LightBackgroundComponentBorder") as System.Windows.Media.Brush ?? TryFindResource("ForegroundBrush") as System.Windows.Media.Brush;
            if (brush == null)
                return;
            var pen = new System.Windows.Media.Pen(brush, 1d);
            var rows = (int)Math.Ceiling((ActualHeight + TileLayoutService.VerticalGap) / (TileLayoutService.CellHeight + TileLayoutService.VerticalGap));
            drawingContext.PushOpacity(0.25d);
            drawingContext.PushClip(new RectangleGeometry(new Rect(RenderSize)));
            for (var row = 0; row < rows; row++)
                for (var column = 0; column < AvailableColumns; column++)
                {
                    var rect = layout.GetRect(column, row, 1, 1);
                    drawingContext.DrawRectangle(null, pen, new Rect(rect.X, rect.Y, rect.Width, rect.Height));
                }
            drawingContext.Pop();
            drawingContext.Pop();
        }

        double ResolveWidth(double width)
        {
            if (!double.IsNaN(width) && !double.IsInfinity(width) && width > 0)
                return width;

            if (ActualWidth > 0 && !double.IsInfinity(ActualWidth))
                return ActualWidth;

            return TileLayoutService.CellWidth;
        }
    }
    #endregion
}
