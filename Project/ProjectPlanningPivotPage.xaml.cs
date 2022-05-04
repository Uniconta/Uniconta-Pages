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
using System.Collections;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectPlanningPivotPage :  PivotBasePage
    {
        CWServerFilter filterDialog = null;
        bool filterCleared;
        TableField[] UserFields { get; set; }
        public override string NameOfControl { get { return TabControls.ProjectPlanningPivotPage; } }
        bool pivotIsLoaded = false;
        List<ProjectTransPivotClient> projectPlaningLst;
        public ProjectPlanningPivotPage(List<ProjectTransPivotClient> entityList, BaseAPI API) : base(API, string.Empty)
        {
            projectPlaningLst = entityList;
            Init();
        }

        public ProjectPlanningPivotPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            SetPageControl(pivotDgProjectPlanning, chartControl);
            layoutControl = layoutItems;
            pivotDgProjectPlanning.BusyIndicator = busyIndicator;
            ribbonControl = localMenu;
            pivotDgProjectPlanning.BeginUpdate();
            pivotDgProjectPlanning.AllowCrossGroupVariation = false;
            pivotDgProjectPlanning.api = api;
            pivotDgProjectPlanning.TableType = typeof(ProjectTransPivotClient);
            BindGrid();
            pivotDgProjectPlanning.EndUpdate();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            pivotDgProjectPlanning.CellDoubleClick += PivotGridControl_CellDoubleClick;
        }

        private void PivotGridControl_CellDoubleClick(object sender, DevExpress.Xpf.PivotGrid.PivotCellEventArgs e)
        {
            var cell = pivotDgProjectPlanning.FocusedCell;
            if (e.ColumnField.GroupInterval == FieldGroupInterval.DateMonth && e.RowField.FieldName == "Employee")
            {
                object columnValue = pivotDgProjectPlanning.GetFieldValue(e.ColumnField, cell.X);
                object rowValue = pivotDgProjectPlanning.GetFieldValue(e.RowField, cell.Y);

                var monthNo = (int)columnValue;
                string employee = (string)rowValue;

                string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("PrTransaction"), employee);
                var param = new object[2];
                param[0] = employee;
                param[1] = monthNo;
                AddDockItem(TabControls.EmployeeProjectTransactionPage, param, vheader);
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
        }

        int seriesIndex = 0;
        bool labelVisibility = true;
        bool chartEnable = true;
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ChartSettings":

                    CWChartSettings cwChartSettingDialog = new CWChartSettings(pivotDgProjectPlanning.ChartSelectionOnly, pivotDgProjectPlanning.ChartProvideColumnGrandTotals,
                        pivotDgProjectPlanning.ChartProvideRowGrandTotals, labelVisibility, seriesIndex, pivotDgProjectPlanning.ChartProvideDataByColumns, chartEnable);
                    cwChartSettingDialog.Closed += delegate
                    {
                        if (cwChartSettingDialog.DialogResult == true)
                        {
                            if (cwChartSettingDialog.IsChartVisible)
                            {
                                chartControl.Diagram = cwChartSettingDialog.ChartDaigram;
                                pivotDgProjectPlanning.ChartProvideEmptyCells = IsPivotTableProvideEmptyCells();
                                chartControl.Diagram.SeriesTemplate.LabelsVisibility = cwChartSettingDialog.labelVisibility;
                                chartControl.CrosshairEnabled = cwChartSettingDialog.crossHairEnabled;
                                pivotDgProjectPlanning.ChartProvideDataByColumns = cwChartSettingDialog.chartProvideDataByColumns;
                                pivotDgProjectPlanning.ChartSelectionOnly = cwChartSettingDialog.ChartSelectionOnly;
                                pivotDgProjectPlanning.ChartProvideColumnGrandTotals = cwChartSettingDialog.ChartProvideColumnGrandTotals;
                                pivotDgProjectPlanning.ChartProvideRowGrandTotals = cwChartSettingDialog.ChartProvideRowGrandTotals;
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
                pivotDgProjectPlanning.RefreshData();
            }
            e.Cancel = true;
            filterDialog.Hide();
        }

        private Task Filter(bool refreshData)
        {
            return pivotDgProjectPlanning.Filter(refreshData ? null : filterValues);
        }

        private void BindGrid()
        {
            pivotDgProjectPlanning.DataSource = projectPlaningLst as IList;
        }

        private void pivotDgProjectTrans_Loaded(object sender, RoutedEventArgs e)
        {
            if (!pivotIsLoaded)
            {
                pivotDgProjectPlanning.BestFit();
                pivotIsLoaded = true;
            }
        }

        private void pivotDgProjectPlanning_CustomSummary(object sender, PivotCustomSummaryEventArgs e)
        {
            string name = e.DataField.Name;
            if (name == "fieldQtyActualBudDiff")
                e.CustomValue = CalculateQty("Qty", "BudgetQty", e.CreateDrillDownDataSource());
            else if(name == "fieldQtyNormBudDiff")
                e.CustomValue = CalculateQty("NormQty", "BudgetQty", e.CreateDrillDownDataSource());
            else if (name == "fieldQtyActualNormDiff")
                e.CustomValue = CalculateQty("Qty", "NormQty", e.CreateDrillDownDataSource());
            else if (name == "fieldSalesActualBudgetDiff")
                e.CustomValue = CalculateQty("Sales", "BudgetSales", e.CreateDrillDownDataSource());
            else if (name == "fieldCostActualBudgetDiff")
                e.CustomValue = CalculateQty("Cost", "BudgetCost", e.CreateDrillDownDataSource());
        }

        double CalculateQty(string field1, string field2, IList list)
        {
            double val = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i] as DevExpress.XtraPivotGrid.PivotDrillDownDataRow;
                object Qty1 = row[field1];
                object Qty2 = row[field2];
                if (Qty1 != null && Qty1 != DBNull.Value && Qty2 != null && Qty2 != DBNull.Value)
                    val = (Convert.ToDouble(Qty1) - Convert.ToDouble(Qty2)) + val;
            }
            return val;
        }
    }
}
