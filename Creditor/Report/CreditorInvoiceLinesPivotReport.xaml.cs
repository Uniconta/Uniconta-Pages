using UnicontaClient.Models;
using DevExpress.Xpf.PivotGrid;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorInvoiceLinesPivotReport : PivotBasePage
    {
        CWServerFilter filterDialog = null;
        bool filterCleared;
        TableField[] UserFields { get; set; }
        DateTime filterDate;
        Filter[] DefaultFilters()
        {

            Filter dateFilter = new Filter() { name = "Date", value = String.Format("{0:d}..", filterDate) };
            return new Filter[] { dateFilter };
        }
        public override string NameOfControl { get { return TabControls.CreditorInvoiceLinesPivotReport; } }
        bool pivotIsLoaded = false;
        public CreditorInvoiceLinesPivotReport(UnicontaBaseEntity master) : base(master)
        {
            init(master);
        }

        public CreditorInvoiceLinesPivotReport(BaseAPI API) : base(API, string.Empty)
        {
            init(null);
        }

        void init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetPageControl(pivotDgCreditorInvLines, chartControl);
            layoutControl = layoutItems;
            pivotDgCreditorInvLines.BusyIndicator = busyIndicator;
            fieldInvQty.Caption = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), Uniconta.ClientTools.Localization.lookup("Qty"));
            ribbonControl = localMenu;
            pivotDgCreditorInvLines.BeginUpdate();
            pivotDgCreditorInvLines.AllowCrossGroupVariation = false;
            pivotDgCreditorInvLines.api = api;
            pivotDgCreditorInvLines.TableType = typeof(CreditorInvoiceLines);
            pivotDgCreditorInvLines.UpdateMaster(master);
            if (master == null)
                SetDefaultFilter();
            BindGrid();
            pivotDgCreditorInvLines.EndUpdate();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            if (!api.CompanyEntity.Warehouse)
                fieldWarehouse.Visible = false;
            else if (!api.CompanyEntity.Location)
                fieldLocation.Visible = false;
            tbGrdTtlRow.Text = Uniconta.ClientTools.Localization.lookup("ShowRowGrandTotals");
            tbGrdTtlCol.Text = Uniconta.ClientTools.Localization.lookup("ShowColumnGrandTotals");
            tbTtlOnFlds.Text = Uniconta.ClientTools.Localization.lookup("ShowTotals");
            chkGrdTtlCol.IsChecked = chkGrdTtlRow.IsChecked = chkTtlOnFlds.IsChecked = true;
        }

        void SetDefaultFilter()
        {
            filterDate = BasePage.GetFilterDate(api.CompanyEntity, false);
            var filterSorthelper = new FilterSortHelper(typeof(CreditorInvoiceLines), DefaultFilters(), null);
            List<string> errors;
            filterValues = filterSorthelper.GetPropValuePair(out errors);
        }
        private Task Filter(bool refreshData)
        {
            return pivotDgCreditorInvLines.Filter(refreshData ? null : filterValues);
        }
        private void BindGrid(bool refreshData = false)
        {
            var t = Filter(refreshData);
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
                case "RefreshGrid":
                    BindGrid(true);
                    pivotDgCreditorInvLines.RefreshData();
                    break;
                case "LocalFilter":
                    if (filterDialog == null)
                    {
                        if (filterCleared)
                            filterDialog = new CWServerFilter(api, typeof(CreditorInvoiceLines), null, null, UserFields);
                        else
                            filterDialog = new CWServerFilter(api, typeof(CreditorInvoiceLines), DefaultFilters(), null, UserFields);
                        filterDialog.GridSource = pivotDgCreditorInvLines.DataSource as IList<UnicontaBaseEntity>;
                        filterDialog.Closing += filterDialog_Closing;
                        filterDialog.Show();
                    }
                    else
                    {
                        filterDialog.GridSource = pivotDgCreditorInvLines.DataSource as IList<UnicontaBaseEntity>;
                        filterDialog.Show(true);
                    }
                    break;
                case "ClearLocalFilter":
                    filterDialog = null;
                    filterValues = null;
                    filterCleared = true;
                    BindGrid();
                    pivotDgCreditorInvLines.RefreshData();
                    break;
                case "ChartSettings":

                    CWChartSettings cwChartSettingDialog = new CWChartSettings(pivotDgCreditorInvLines.ChartSelectionOnly, pivotDgCreditorInvLines.ChartProvideColumnGrandTotals,
                        pivotDgCreditorInvLines.ChartProvideRowGrandTotals, labelVisibility, seriesIndex, pivotDgCreditorInvLines.ChartProvideDataByColumns, chartEnable);
                    cwChartSettingDialog.Closed += delegate
                    {
                        if (cwChartSettingDialog.DialogResult == true)
                        {
                            if (cwChartSettingDialog.IsChartVisible)
                            {
                                chartControl.Diagram = cwChartSettingDialog.ChartDaigram;
                                pivotDgCreditorInvLines.ChartProvideEmptyCells = IsPivotTableProvideEmptyCells();
                                chartControl.Diagram.SeriesTemplate.LabelsVisibility = cwChartSettingDialog.labelVisibility;
                                chartControl.CrosshairEnabled = cwChartSettingDialog.crossHairEnabled;
                                pivotDgCreditorInvLines.ChartProvideDataByColumns = cwChartSettingDialog.chartProvideDataByColumns;
                                pivotDgCreditorInvLines.ChartSelectionOnly = cwChartSettingDialog.ChartSelectionOnly;
                                pivotDgCreditorInvLines.ChartProvideColumnGrandTotals = cwChartSettingDialog.ChartProvideColumnGrandTotals;
                                pivotDgCreditorInvLines.ChartProvideRowGrandTotals = cwChartSettingDialog.ChartProvideRowGrandTotals;
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
                pivotDgCreditorInvLines.RefreshData();
            }
            e.Cancel = true;
            filterDialog.Hide();
        }

        private void PivotDgInvTrans_Loaded(object sender, RoutedEventArgs e)
        {
            if (!pivotIsLoaded)
            {
                pivotDgCreditorInvLines.BestFit();
                pivotIsLoaded = true;
            }
        }
  
        private void pivotDgCreditorInvLines_Loaded(object sender, RoutedEventArgs e)
        {
            pivotDgCreditorInvLines.BestFit();
        }

        private void chkGrdTtlRow_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkGrdTtlRow.IsChecked;
            pivotDgCreditorInvLines.ShowRowGrandTotalHeader = value;
            pivotDgCreditorInvLines.ShowRowGrandTotals = value;
        }

        private void chkGrdTtlCol_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkGrdTtlCol.IsChecked;
            pivotDgCreditorInvLines.ShowColumnGrandTotalHeader = value;
            pivotDgCreditorInvLines.ShowColumnGrandTotals = value;
            pivotDgCreditorInvLines.ShowColumnTotals = value;
        }

        private void chkTtlOnFlds_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkTtlOnFlds.IsChecked;
            pivotDgCreditorInvLines.ShowRowTotals = value;
        }
    }
}
