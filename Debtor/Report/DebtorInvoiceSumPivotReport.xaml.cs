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
    public class DebtorInvoiceSumAndBudgetClient : DebtorInvoiceSumClient
    {
        public bool _Budget;
        [Display(Name = "Budget", ResourceType = typeof(GLBudgetClientText))]
        public bool Budget { get { return _Budget; } }

        [Display(Name = "SalesDeviation", ResourceType = typeof(GLBudgetClientText))]
        public double SalesDeviation
        {
            get 
            {
                if (!_Budget)
                    return _Sales ;
                else
                    return _Sales * -1;
            } 
        }

        [Display(Name = "CostDeviation", ResourceType = typeof(GLBudgetClientText))]
        public double CostDeviation
        {
            get
            {
                if (!_Budget)
                    return _Cost;
                else
                    return _Cost * -1;
            }
        }
    }

    public partial class DebtorInvoiceSumPivotReport : PivotBasePage
    {
        CWServerFilter filterDialog = null;
        bool filterCleared;
        TableField[] UserFields { get; set; }

        public override string NameOfControl { get { return TabControls.DebtorInvoiceSumPivotReport; } }
        bool pivotIsLoaded = false;
        DateTime filterDate;
        Filter[] DefaultFilters()
        {
            Filter dateFilter = new Filter() { name = "Date", value = String.Format("{0:d}..", filterDate) };
            return new Filter[] { dateFilter };
        }

        public DebtorInvoiceSumPivotReport(UnicontaBaseEntity master) : base(master)
        {
            init(master);
        }   

        public DebtorInvoiceSumPivotReport(BaseAPI API) : base(API, string.Empty)
        {
            init(null);
        }

        void init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetPageControl(pivotDgDebtorInvoiceSum, chartControl);
            layoutControl = layoutItems;
            pivotDgDebtorInvoiceSum.BusyIndicator = busyIndicator;
            ribbonControl = localMenu;
            pivotDgDebtorInvoiceSum.BeginUpdate();
            pivotDgDebtorInvoiceSum.AllowCrossGroupVariation = false;
            pivotDgDebtorInvoiceSum.api = api;
            pivotDgDebtorInvoiceSum.TableType = typeof(DebtorInvoiceSumAndBudgetClient);
            pivotDgDebtorInvoiceSum.UpdateMaster(master);
            if (master == null)
                SetDefaultFilter();
            BindGrid();
            pivotDgDebtorInvoiceSum.EndUpdate();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;

            tbGrdTtlRow.Text = Uniconta.ClientTools.Localization.lookup("ShowRowGrandTotals");
            tbGrdTtlCol.Text = Uniconta.ClientTools.Localization.lookup("ShowColumnGrandTotals");
            tbTtlOnFlds.Text = Uniconta.ClientTools.Localization.lookup("ShowTotals");

            chkGrdTtlCol.IsChecked = chkGrdTtlRow.IsChecked = chkTtlOnFlds.IsChecked = true;
        }

        void SetDefaultFilter()
        {
            filterDate = BasePage.GetFilterDate(api.CompanyEntity, false);
            var filterSorthelper = new FilterSortHelper(typeof(DebtorInvoiceSumClient), DefaultFilters(), null);
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
                    pivotDgDebtorInvoiceSum.RefreshData();
                    break;
                case "LocalFilter":
                    if (filterDialog == null)
                    {
                        if (filterCleared)
                            filterDialog = new CWServerFilter(api, typeof(DebtorInvoiceSumClient), null, null, UserFields);
                        else
                            filterDialog = new CWServerFilter(api, typeof(DebtorInvoiceSumClient), DefaultFilters(), null, UserFields);
                        filterDialog.GridSource = pivotDgDebtorInvoiceSum.DataSource as IList<UnicontaBaseEntity>;
                        filterDialog.Closing += filterDialog_Closing;
                        filterDialog.Show();
                    }
                    else
                    {
                        filterDialog.GridSource = pivotDgDebtorInvoiceSum.DataSource as IList<UnicontaBaseEntity>;
                        filterDialog.Show(true);
                    }
                    break;
                case "ClearLocalFilter":
                    filterDialog = null;
                    filterValues = null;
                    filterCleared = true;
                    BindGrid();
                    pivotDgDebtorInvoiceSum.RefreshData();
                    break;
                case "ChartSettings":
                    CWChartSettings cwChartSettingDialog = new CWChartSettings(pivotDgDebtorInvoiceSum.ChartSelectionOnly, pivotDgDebtorInvoiceSum.ChartProvideColumnGrandTotals,
                        pivotDgDebtorInvoiceSum.ChartProvideRowGrandTotals, labelVisibility, seriesIndex, pivotDgDebtorInvoiceSum.ChartProvideDataByColumns, chartEnable);
                    cwChartSettingDialog.Closed += delegate
                    {
                        if (cwChartSettingDialog.DialogResult == true)
                        {
                            if (cwChartSettingDialog.IsChartVisible)
                            {
                                chartControl.Diagram = cwChartSettingDialog.ChartDaigram;
                                pivotDgDebtorInvoiceSum.ChartProvideEmptyCells = IsPivotTableProvideEmptyCells();
                                chartControl.Diagram.SeriesTemplate.LabelsVisibility = cwChartSettingDialog.labelVisibility;
                                chartControl.CrosshairEnabled = cwChartSettingDialog.crossHairEnabled;
                                pivotDgDebtorInvoiceSum.ChartProvideDataByColumns = cwChartSettingDialog.chartProvideDataByColumns;
                                pivotDgDebtorInvoiceSum.ChartSelectionOnly = cwChartSettingDialog.ChartSelectionOnly;
                                pivotDgDebtorInvoiceSum.ChartProvideColumnGrandTotals = cwChartSettingDialog.ChartProvideColumnGrandTotals;
                                pivotDgDebtorInvoiceSum.ChartProvideRowGrandTotals = cwChartSettingDialog.ChartProvideRowGrandTotals;
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
                pivotDgDebtorInvoiceSum.RefreshData();
            }
            e.Cancel = true;
            filterDialog.Hide();
        }

        private void pivotDgDebtorInvoiceSum_Loaded(object sender, RoutedEventArgs e)
        {
            if (!pivotIsLoaded)
            {
                pivotDgDebtorInvoiceSum.BestFit();
                pivotIsLoaded = true;
            }
        }

        static DebtorInvoiceSumAndBudgetClient createPost(DCBudgetLine rec)
        {
            return new DebtorInvoiceSumAndBudgetClient()
            {
                _CompanyId = rec.CompanyId,
                _DCAccount = rec._Account,
                _Employee = rec._Employee,
                _Budget = true
            };
        }

        async void BindGrid(bool refreshData = false)
        {
            var t1 = api.Query(new DebtorBudgetLineClient(), pivotDgDebtorInvoiceSum.masterRecords, refreshData ? null : filterValues);
            var t2 = pivotDgDebtorInvoiceSum.Filter(refreshData ? null : filterValues);

            await Task.WhenAll(new Task[] { t1, t2 } );

            var budgetLines = t1.Result;
            if (budgetLines != null && budgetLines.Length > 0)
            {
                var invList = (IList<DebtorInvoiceSumAndBudgetClient>)pivotDgDebtorInvoiceSum.DataSource;

                var lst = new List<DebtorInvoiceSumAndBudgetClient>(budgetLines.Length + invList.Count);
                var Year = BasePage.GetSystemDefaultDate().Year;
                var BudgetFromDate = new DateTime(Year, 1, 1);

                foreach (var rec in budgetLines)
                {
                    if (rec._Disable)
                        continue;

                    var recDate = rec._Date;
                    if (recDate == DateTime.MinValue)
                        recDate = BudgetFromDate;

                    var recToDate = rec._ToDate;
                    if (recToDate == DateTime.MinValue)
                        recToDate = new DateTime(recDate.Year, 12, 13);

                    int n = (int)rec._Recurring;
                    if (n == 0)
                    {
                        if (recDate >= BudgetFromDate && recDate <= recToDate && rec._Amount != 0d)
                        {
                            var post = createPost(rec);
                            post._Sales = rec._Amount;
                            post._Date = recDate;
                            lst.Add(post);
                        }
                    }
                    else
                    {
                        if (n > 4)
                        {
                            if (n == 5) // half year
                                n = 6;
                            else if (n == 6) // year
                                n = 12;
                        }
                        var RegulatePct = rec._Regulate / 100d;
                        var recAmount = rec._Amount;
                        int i = 0;
                        for (; ; )
                        {
                            var newDate = recDate.AddMonths(i * n);
                            if (newDate > recToDate)
                                break;
                            if (newDate >= BudgetFromDate)
                            {
                                var Amount = Math.Round(recAmount + (recAmount * RegulatePct * i), 2);
                                if (Amount != 0d)
                                {
                                    var post = createPost(rec);
                                    post._Sales = Amount;
                                    post._Date = newDate;
                                    lst.Add(post);
                                }
                            }
                            i++;
                        }
                    }
                }
                lst.AddRange(invList);
                pivotDgDebtorInvoiceSum.DataSource = lst.ToArray();
                pivotDgDebtorInvoiceSum.Visibility = Visibility.Visible;
            }
        }


        private void chkGrdTtlRow_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkGrdTtlRow.IsChecked;
            pivotDgDebtorInvoiceSum.ShowRowGrandTotalHeader = value;
            pivotDgDebtorInvoiceSum.ShowRowGrandTotals = value;
        }

        private void chkGrdTtlCol_Checked(object sender, RoutedEventArgs e)
        {
            var value = chkGrdTtlCol.IsChecked.GetValueOrDefault();
            pivotDgDebtorInvoiceSum.ShowColumnGrandTotalHeader = value;
            pivotDgDebtorInvoiceSum.ShowColumnGrandTotals = value;
            pivotDgDebtorInvoiceSum.ShowColumnTotals = value;
        }

        private void chkTtlOnFlds_Checked(object sender, RoutedEventArgs e)
        {
            var value = (bool)chkTtlOnFlds.IsChecked;
            pivotDgDebtorInvoiceSum.ShowRowTotals = value;
        }
    }
}
