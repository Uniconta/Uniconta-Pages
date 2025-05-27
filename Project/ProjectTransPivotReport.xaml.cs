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
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using System.ComponentModel.DataAnnotations;
using Uniconta.API.Service;
using DevExpress.Xpf.PivotGrid;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
   
    public partial class ProjectTransPivotReport : PivotBasePage
    {
        CWServerFilter filterDialog = null;
        bool filterCleared;
        TableField[] UserFields { get; set; }
        public override string NameOfControl { get { return TabControls.ProjectTransPivotReport; } }
        bool pivotIsLoaded = false;
        public ProjectTransPivotReport(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            init(master);
        }

        public ProjectTransPivotReport(BaseAPI API) : base(API, string.Empty)
        {
            init(null);
        }

        void init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetPageControl(pivotDgProjectTrans, chartControl);
            layoutControl = layoutItems;
            pivotDgProjectTrans.BusyIndicator = busyIndicator;
            fieldQty.Caption = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), Uniconta.ClientTools.Localization.lookup("Qty"));
            ribbonControl = localMenu;
            pivotDgProjectTrans.BeginUpdate();
            pivotDgProjectTrans.AllowCrossGroupVariation = false;
            pivotDgProjectTrans.api = api;
            pivotDgProjectTrans.TableType = typeof(ProjectTransClient);
            pivotDgProjectTrans.UpdateMaster(master);
            BindGrid();
            pivotDgProjectTrans.EndUpdate();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            pivotDgProjectTrans.CellDoubleClick += PivotGridControl_CellDoubleClick;
            tbGrdTtlRow.Text = Uniconta.ClientTools.Localization.lookup("ShowRowGrandTotals");
            tbGrdTtlCol.Text = Uniconta.ClientTools.Localization.lookup("ShowColumnGrandTotals");
            tbTtlOnFlds.Text = Uniconta.ClientTools.Localization.lookup("ShowTotals");
            chkGrdTtlCol.IsChecked = chkGrdTtlRow.IsChecked = chkTtlOnFlds.IsChecked = true;
        }

        private void PivotGridControl_CellDoubleClick(object sender, DevExpress.Xpf.PivotGrid.PivotCellEventArgs e)
        {
            var cell = pivotDgProjectTrans.FocusedCell;
            if (e.ColumnField != null && e.ColumnField.GroupInterval == FieldGroupInterval.DateMonth && e.RowField?.FieldName == "Employee")
            {
                object MonthNo = pivotDgProjectTrans.GetFieldValue(e.ColumnField, cell.X);
                object employee = pivotDgProjectTrans.GetFieldValue(e.RowField, cell.Y) as string;

                string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("PrTransaction"), employee);
                AddDockItem(TabControls.EmployeeProjectTransactionPage, new object[] { employee, MonthNo }, vheader);
            }
        }

        protected override void OnLayoutLoaded()
        {
            if (chartControl.Diagram != null)
            {
                chartControl.Visibility = Visibility.Visible;
                labelVisibility = chartControl.Diagram.SeriesTemplate.LabelsVisibility;
                seriesIndex = GetSeriesId();
            }
            if (api.CompanyEntity.HideCostPrice)
                fieldCostAmount.Visible = fieldCostAmount.ShowInCustomizationForm = false;
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
                    pivotDgProjectTrans.RefreshData();
                    break;
                case "LocalFilter":
                    if (filterDialog == null)
                    {
                        if (filterCleared)
                            filterDialog = new CWServerFilter(api, typeof(ProjectTransClient), null, null, UserFields);
                        else
                            filterDialog = new CWServerFilter(api, typeof(ProjectTransClient), null, null, UserFields);
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
                    pivotDgProjectTrans.RefreshData();
                    break;
                case "ChartSettings":

                    CWChartSettings cwChartSettingDialog = new CWChartSettings(pivotDgProjectTrans.ChartSelectionOnly, pivotDgProjectTrans.ChartProvideColumnGrandTotals,
                        pivotDgProjectTrans.ChartProvideRowGrandTotals, labelVisibility, seriesIndex, pivotDgProjectTrans.ChartProvideDataByColumns, chartEnable);
                    cwChartSettingDialog.Closed += delegate
                    {
                        if (cwChartSettingDialog.DialogResult == true)
                        {
                            if (cwChartSettingDialog.IsChartVisible)
                            {
                                chartControl.Diagram = cwChartSettingDialog.ChartDaigram;
                                pivotDgProjectTrans.ChartProvideEmptyCells = IsPivotTableProvideEmptyCells();
                                chartControl.Diagram.SeriesTemplate.LabelsVisibility = cwChartSettingDialog.labelVisibility;
                                chartControl.CrosshairEnabled = cwChartSettingDialog.crossHairEnabled;
                                pivotDgProjectTrans.ChartProvideDataByColumns = cwChartSettingDialog.chartProvideDataByColumns;
                                pivotDgProjectTrans.ChartSelectionOnly = cwChartSettingDialog.ChartSelectionOnly;
                                pivotDgProjectTrans.ChartProvideColumnGrandTotals = cwChartSettingDialog.ChartProvideColumnGrandTotals;
                                pivotDgProjectTrans.ChartProvideRowGrandTotals = cwChartSettingDialog.ChartProvideRowGrandTotals;
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
                pivotDgProjectTrans.RefreshData();
            }
            e.Cancel = true;
            filterDialog.Hide();
        }

        private Task Filter(bool refreshData)
        {
            return pivotDgProjectTrans.Filter(refreshData ? null : filterValues);
        }
        private void BindGrid(bool refreshData = false)
        {
            var t = Filter(refreshData);
        }

        private void pivotDgProjectTrans_Loaded(object sender, RoutedEventArgs e)
        {
            if (!pivotIsLoaded)
            {
                pivotDgProjectTrans.BestFit();
                pivotIsLoaded = true;
            }
        }

        private void chkGrdTtlRow_Checked(object sender, RoutedEventArgs e)
        {
            pivotDgProjectTrans.ShowRowGrandTotalHeader =
            pivotDgProjectTrans.ShowRowGrandTotals = chkGrdTtlRow.IsChecked.GetValueOrDefault();
        }

        private void chkGrdTtlCol_Checked(object sender, RoutedEventArgs e)
        {
            var value = chkGrdTtlCol.IsChecked.GetValueOrDefault();
            pivotDgProjectTrans.ShowColumnGrandTotalHeader = value;
            pivotDgProjectTrans.ShowColumnGrandTotals = value;
            pivotDgProjectTrans.ShowColumnTotals = value;
        }

        private void chkTtlOnFlds_Checked(object sender, RoutedEventArgs e)
        {
            pivotDgProjectTrans.ShowRowTotals = chkTtlOnFlds.IsChecked.GetValueOrDefault();
        }
    }
}
