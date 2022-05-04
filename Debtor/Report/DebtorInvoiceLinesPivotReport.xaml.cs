using UnicontaClient.Models;
using DevExpress.Xpf.PivotGrid;
using System.Collections.Generic;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;
using System.Windows;
using System.Threading.Tasks;
using Uniconta.API.Service;
using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorInvoiceLinesPivotReport : PivotBasePage
    {
        CWServerFilter filterDialog = null;
        bool filterCleared;
        TableField[] UserFields { get; set; }

        public override string NameOfControl { get { return TabControls.DebtorInvoiceLinesPivotReport; } }
        bool pivotIsLoaded = false;
        DateTime filterDate;
        Filter[] DefaultFilters()
        {
           
                Filter dateFilter = new Filter() { name = "Date", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
        }
        public DebtorInvoiceLinesPivotReport(UnicontaBaseEntity master) : base(master)
        {
            init(master);
        }

        public DebtorInvoiceLinesPivotReport(BaseAPI API) : base(API, string.Empty)
        {
            init(null);
        }

        void init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetPageControl(pivotDgDebtorInvLines, chartControl);
            layoutControl = layoutItems;
            pivotDgDebtorInvLines.BusyIndicator = busyIndicator;
            fieldInvQty.Caption = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), Uniconta.ClientTools.Localization.lookup("Qty"));
            ribbonControl = localMenu;
            pivotDgDebtorInvLines.BeginUpdate();
            pivotDgDebtorInvLines.AllowCrossGroupVariation = false;
            pivotDgDebtorInvLines.api = api;
            pivotDgDebtorInvLines.TableType = typeof(DebtorInvoiceLines);
            pivotDgDebtorInvLines.UpdateMaster(master);
            if (master == null)
                SetDefaultFilter();
            BindGrid();
            pivotDgDebtorInvLines.EndUpdate();
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
            var filterSorthelper = new FilterSortHelper(typeof(DebtorInvoiceLines), DefaultFilters(), null);
            List<string> errors;
            filterValues = filterSorthelper.GetPropValuePair(out errors);
        }

        void SetDefaultFilter()
        {
            filterDate = BasePage.GetFilterDate(api.CompanyEntity, false);
            var filterSorthelper = new FilterSortHelper(typeof(DebtorInvoiceLines), DefaultFilters(), null);
            List<string> errors;
            filterValues = filterSorthelper.GetPropValuePair(out errors);
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
                    pivotDgDebtorInvLines.RefreshData();
                    break;
                case "LocalFilter":
                    if (filterDialog == null)
                    {
                        if (filterCleared)
                            filterDialog = new CWServerFilter(api, typeof(DebtorInvoiceLines), null, null, UserFields);
                        else
                            filterDialog = new CWServerFilter(api, typeof(DebtorInvoiceLines), DefaultFilters(), null, UserFields);
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
                    pivotDgDebtorInvLines.RefreshData();
                    break;
                case "ChartSettings":
                    CWChartSettings cwChartSettingDialog = new CWChartSettings(pivotDgDebtorInvLines.ChartSelectionOnly, pivotDgDebtorInvLines.ChartProvideColumnGrandTotals,
                        pivotDgDebtorInvLines.ChartProvideRowGrandTotals, labelVisibility, seriesIndex, pivotDgDebtorInvLines.ChartProvideDataByColumns, chartEnable);
                    cwChartSettingDialog.Closed += delegate
                    {
                        if (cwChartSettingDialog.DialogResult == true)
                        {
                            if (cwChartSettingDialog.IsChartVisible)
                            {
                                chartControl.Diagram = cwChartSettingDialog.ChartDaigram;
                                pivotDgDebtorInvLines.ChartProvideEmptyCells = IsPivotTableProvideEmptyCells();
                                chartControl.Diagram.SeriesTemplate.LabelsVisibility = cwChartSettingDialog.labelVisibility;
                                chartControl.CrosshairEnabled = cwChartSettingDialog.crossHairEnabled;
                                pivotDgDebtorInvLines.ChartProvideDataByColumns = cwChartSettingDialog.chartProvideDataByColumns;
                                pivotDgDebtorInvLines.ChartSelectionOnly = cwChartSettingDialog.ChartSelectionOnly;
                                pivotDgDebtorInvLines.ChartProvideColumnGrandTotals = cwChartSettingDialog.ChartProvideColumnGrandTotals;
                                pivotDgDebtorInvLines.ChartProvideRowGrandTotals = cwChartSettingDialog.ChartProvideRowGrandTotals;
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
                pivotDgDebtorInvLines.RefreshData();
            }
            e.Cancel = true;
            filterDialog.Hide();
        }

        private void pivotDgDebtorInvLines_Loaded(object sender, RoutedEventArgs e)
        {
            if (!pivotIsLoaded)
            {
                pivotDgDebtorInvLines.BestFit();
                pivotIsLoaded = true;
            }
        }

        private Task Filter(bool refreshData)
        {
            return pivotDgDebtorInvLines.Filter(refreshData ? null : filterValues);
        }
        private void BindGrid(bool refreshData = false)
        {
            var t = Filter(refreshData);
        }

        private void chkGrdTtlRow_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkGrdTtlRow.IsChecked;
            pivotDgDebtorInvLines.ShowRowGrandTotalHeader = value;
            pivotDgDebtorInvLines.ShowRowGrandTotals = value;
        }

        private void chkGrdTtlCol_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkGrdTtlCol.IsChecked;
            pivotDgDebtorInvLines.ShowColumnGrandTotalHeader = value;
            pivotDgDebtorInvLines.ShowColumnGrandTotals = value;
            pivotDgDebtorInvLines.ShowColumnTotals = value;
        }

        private void chkTtlOnFlds_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkTtlOnFlds.IsChecked;
            pivotDgDebtorInvLines.ShowRowTotals = value;
        }
    }    
 }
