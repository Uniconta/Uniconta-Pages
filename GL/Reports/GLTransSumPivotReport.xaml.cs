using UnicontaClient.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System.ComponentModel.DataAnnotations;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class GLTransSumPivotReport : PivotBasePage
    {
        CWServerFilter filterDialog = null;
        bool filterCleared;
        TableField[] UserFields { get; set; }
        public override string NameOfControl { get { return TabControls.GLTransSumPivotReport; } }

        public GLTransSumPivotReport(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        public GLTransSumPivotReport(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetPageControl(pivotDgGLTransSum, chartControl);
            layoutControl = layoutItems;
            pivotDgGLTransSum.BusyIndicator = busyIndicator;
            ribbonControl = localMenu;
            pivotDgGLTransSum.BeginUpdate();
            pivotDgGLTransSum.AllowCrossGroupVariation = false;
            pivotDgGLTransSum.api = api;
            pivotDgGLTransSum.TableType = typeof(GLTransSumClient);
            pivotDgGLTransSum.UpdateMaster(master);
            BindGrid();
            pivotDgGLTransSum.EndUpdate();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            SetDimension();
            tbGrdTtlRow.Text = Uniconta.ClientTools.Localization.lookup("ShowRowGrandTotals");
            tbGrdTtlCol.Text = Uniconta.ClientTools.Localization.lookup("ShowColumnGrandTotals");
            tbTtlOnFlds.Text = Uniconta.ClientTools.Localization.lookup("ShowTotals");
            chkGrdTtlCol.IsChecked = chkGrdTtlRow.IsChecked = chkTtlOnFlds.IsChecked = true;
        }

        protected override void OnLayoutLoaded()
        {
            if (chartControl.Diagram != null)
            {
                chartControl.Visibility = Visibility.Visible;
                labelVisibility = chartControl.Diagram.SeriesTemplate.LabelsVisibility;
                seriesIndex = GetSeriesId();
            }
        }

        void SetDimension()
        {
            var c = api.CompanyEntity;
            if (c == null)
                return;
            fieldDim1.Caption = (string)c._Dim1;
            fieldDim2.Caption = (string)c._Dim2;
            fieldDim3.Caption = (string)c._Dim3;
            fieldDim4.Caption = (string)c._Dim4;
            fieldDim5.Caption = (string)c._Dim5;
            var noofDimensions = c.NumberOfDimensions;
            if (noofDimensions < 5)
                fieldDim5.Visible = false;
            if (noofDimensions < 4)
                fieldDim4.Visible = false;
            if (noofDimensions < 3)
                fieldDim3.Visible = false;
            if (noofDimensions < 2)
                fieldDim2.Visible = false;
            if (noofDimensions < 1)
                fieldDim1.Visible = false;
        }

        int seriesIndex = 0;
        bool labelVisibility = true;
        bool chartEnable = true;
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "RefreshGrid":
                    BindGrid(true);
                    pivotDgGLTransSum.RefreshData();
                    break;
                case "LocalFilter":
                    if (filterDialog == null)
                    {
                        if (filterCleared)
                            filterDialog = new CWServerFilter(api, typeof(GLTransSumClient), null, null, UserFields);
                        else
                            filterDialog = new CWServerFilter(api, typeof(GLTransSumClient), null, null, UserFields);
                        filterDialog.Closing += filterDialog_Closing;
                        filterDialog.Show();
                    }
                    else
                        filterDialog.Show(true);
                    break;
                case "ClearLocalFilter":
                    filterDialog = null;
                    filterValues = null;
                    filterCleared = true;
                    BindGrid();
                    pivotDgGLTransSum.RefreshData();
                    break;
                case "ChartSettings":

                    CWChartSettings cwChartSettingDialog = new CWChartSettings(pivotDgGLTransSum.ChartSelectionOnly, pivotDgGLTransSum.ChartProvideColumnGrandTotals,
                        pivotDgGLTransSum.ChartProvideRowGrandTotals, labelVisibility, seriesIndex, pivotDgGLTransSum.ChartProvideDataByColumns, chartEnable);
                    cwChartSettingDialog.Closed += delegate
                    {
                        if (cwChartSettingDialog.DialogResult == true)
                        {
                            if (cwChartSettingDialog.IsChartVisible)
                            {
                                chartControl.Diagram = cwChartSettingDialog.ChartDaigram;
                                pivotDgGLTransSum.ChartProvideEmptyCells = IsPivotTableProvideEmptyCells();
                                chartControl.Diagram.SeriesTemplate.LabelsVisibility = cwChartSettingDialog.labelVisibility;
                                chartControl.CrosshairEnabled = cwChartSettingDialog.crossHairEnabled;
                                pivotDgGLTransSum.ChartProvideDataByColumns = cwChartSettingDialog.chartProvideDataByColumns;
                                pivotDgGLTransSum.ChartSelectionOnly = cwChartSettingDialog.ChartSelectionOnly;
                                pivotDgGLTransSum.ChartProvideColumnGrandTotals = cwChartSettingDialog.ChartProvideColumnGrandTotals;
                                pivotDgGLTransSum.ChartProvideRowGrandTotals = cwChartSettingDialog.ChartProvideRowGrandTotals;
                                seriesIndex = cwChartSettingDialog.SeriesIndex;
                                chartControl.Visibility = Visibility.Visible;
                                if (rowgridSplitter.Height.Value == 0 && rowChartControl.Height.Value == 0)
                                {
                                    rowgridSplitter.Height = new GridLength(5);
                                    var converter = new GridLengthConverter();
                                    rowChartControl.Height = (GridLength)converter.ConvertFrom("Auto");
                                }
                            }
                            else
                            {
                                chartControl.Visibility = Visibility.Collapsed;
                                rowgridSplitter.Height = new GridLength(0);
                                rowChartControl.Height = new GridLength(0);
                            }
                            chartEnable = cwChartSettingDialog.IsChartVisible;
                            labelVisibility = cwChartSettingDialog.labelVisibility;
                        }
                    };
                    cwChartSettingDialog.Show();
                    break;
                case "ImportPivotTableLayout":
                    controlRibbon_BaseActions(ActionType);
                    if (chartControl.Diagram != null)
                    {
                        chartControl.Visibility = Visibility.Visible;
                        labelVisibility = chartControl.Diagram.SeriesTemplate.LabelsVisibility;
                        seriesIndex = GetSeriesId();
                    }
                    break;
                default:
                    controlRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public IEnumerable<PropValuePair> filterValues;
        public FilterSorter PropSort;

        void filterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (filterDialog.DialogResult == true)
            {
                filterValues = filterDialog.PropValuePair;
                PropSort = filterDialog.PropSort;
                BindGrid();
                pivotDgGLTransSum.RefreshData();
            }
            e.Cancel = true;
            filterDialog.Hide();
        }

        private Task Filter(bool refreshData)
        {
            return pivotDgGLTransSum.Filter(refreshData ? null : filterValues);
        }
        private void BindGrid(bool refreshData = false)
        {
            var t = Filter(refreshData);
        }
        bool pivotIsLoaded = false;
        private void pivotDgGLTransSUM_Loaded(object sender, RoutedEventArgs e)
        {
            if (!pivotIsLoaded)
            {
                pivotDgGLTransSum.BestFit();
                pivotIsLoaded = true;
            }
        }

        private void chkGrdTtlRow_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkGrdTtlRow.IsChecked;
            pivotDgGLTransSum.ShowRowGrandTotalHeader = value;
            pivotDgGLTransSum.ShowRowGrandTotals = value;
        }

        private void chkGrdTtlCol_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkGrdTtlCol.IsChecked;
            pivotDgGLTransSum.ShowColumnGrandTotalHeader = value;
            pivotDgGLTransSum.ShowColumnGrandTotals = value;
            pivotDgGLTransSum.ShowColumnTotals = value;
        }

        private void chkTtlOnFlds_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkTtlOnFlds.IsChecked;
            pivotDgGLTransSum.ShowRowTotals = value;
        }
    }
}
