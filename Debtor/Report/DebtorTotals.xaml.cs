using UnicontaClient.Pages;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common.Utility;
using System.Collections;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Charts;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorTotalClient : DebtorAgeTotalClient
    {
        public double _Total;
        [Display(Name = "Total", ResourceType = typeof(GLTableText))]
        public double Total { get { return _Total; } set { _Total = value; } }
    }

    public class DebtorTotalsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorTotalClient); } }

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            base.PrintGrid(PrintReportName, printparam, format, page);
        }
        public override IComparer GridSorting { get { return new DCAgeCompare(); } }

        public string PrintReportName { get; set; }
    }
    public partial class DebtorTotals : GridBasePage
    {
        static int reportType = 0;
        static DateTime balDate = BasePage.GetSystemDefaultDate();
        static int interval = 31;
        static int count = 5;

        CWServerFilter debtorFilterDialog;
        bool debtorFilterCleared;
        public TableField[] DebtorUserFields { get; set; }

        public DebtorTotals(BaseAPI API) : base(API, string.Empty)
        {
            reportType = 0;
            InitializeComponent();
            SetRibbonControl(localMenu, dgDebtorTotalsGrid);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            cmbReportType.SelectedIndex = reportType;
            BalanceDate.DateTime = balDate;
            if (interval > 0)
                intervalEdit.EditValue = interval;
            if (count > 0)
                countEdit.EditValue = count;
            dgDebtorTotalsGrid.ShowTotalSummary();
            chkSkipOnHold.IsEnabled = false;
            cmbCurrency.ItemsSource = AppEnums.Currencies.GetLabels();
            cmbCurrency.SelectedIndex = 0;
            var Comp = api.CompanyEntity;
            if (Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) == null)
                Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api);

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            GetShowHideChart();
            IntialLoad();
        }

        void IntialLoad()
        {
            TransactionReport.SetDailyJournal(cmbJournal, api);
        }
        public override Task InitQuery()
        {
            return null;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Search":
                    LoadData();
                    break;
                case "DebtorFilter":
                    if (debtorFilterDialog == null)
                    {
                        if (debtorFilterCleared)
                            debtorFilterDialog = new CWServerFilter(api, typeof(DebtorClient), null, null, DebtorUserFields);
                        else
                            debtorFilterDialog = new CWServerFilter(api, typeof(DebtorClient), null, null, DebtorUserFields);
                        debtorFilterDialog.GridSource = dgDebtorTotalsGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        debtorFilterDialog.Closing += debtorFilterDialog_Closing;
                        debtorFilterDialog.Show();
                    }
                    else
                    {
                        debtorFilterDialog.GridSource = dgDebtorTotalsGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        debtorFilterDialog.Show(true);
                    }
                    break;

                case "ClearDebtorFilter":
                    debtorFilterDialog = null;
                    debtorFilterValues = null;
                    debtorFilterCleared = true;
                    break;
                case "EnableChart":
                    hideChart = !hideChart;
                    SetShowHideChart(hideChart);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        ItemBase ibase;
        bool hideChart = false;
        void GetShowHideChart()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "EnableChart");
        }

        private void SetShowHideChart(bool _hideChart)
        {
            if (ibase == null)
                return;
            if (_hideChart)
            {
                totalAmountByDateChart.Visibility = Visibility.Collapsed;
                rowgridSplitter.Height = new GridLength(0);
                rowChartControl.Height = new GridLength(0);
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("Graph"));
            }
            else
            {
                if (rowgridSplitter.Height.Value == 0 && rowChartControl.Height.Value == 0)
                {
                    totalAmountByDateChart.Visibility = Visibility.Visible;
                    rowgridSplitter.Height = new GridLength(2);
                    rowChartControl.Height = new GridLength(1, GridUnitType.Auto);
                }
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("Graph"));
            }
        }

        public IEnumerable<PropValuePair> debtorFilterValues;
        public FilterSorter debtorPropSort;

        void debtorFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (debtorFilterDialog.DialogResult == true)
            {
                debtorFilterValues = debtorFilterDialog.PropValuePair;
                debtorPropSort = debtorFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            debtorFilterDialog.Hide();
#endif
        }
        List<PropValuePair> inputs;
        private async void LoadData()
        {
            balDate = BalanceDate.DateTime;
            if (balDate == DateTime.MinValue)
                balDate = DateTime.Today;
            string reportTypeContent = ((ComboBoxItem)cmbReportType.SelectedItem).Content.ToString();
            if (cmbReportType.SelectedIndex == 0)
                reportTypeContent = String.Format("{0} {1:d}", reportTypeContent, balDate);
            if (cmbCurrency.SelectedIndex > 0)
                reportTypeContent = String.Format("{0} {1}", reportTypeContent, cmbCurrency.SelectedItem.ToString());
            dgDebtorTotalsGrid.PrintReportName = string.Format("{0} {1} ({2})", Uniconta.ClientTools.Localization.lookup("Debtor"), Uniconta.ClientTools.Localization.lookup("AgeingReport"), reportTypeContent);
            reportType = cmbReportType.SelectedIndex;
            interval = (int)NumberConvert.ToInt(intervalEdit.Text);
            count = (int)NumberConvert.ToInt(countEdit.Text);
            for (int i = 0; i < 10; i++)
            {
                var index = NumberConvert.ToString(i);
                GridColumn column = dgDebtorTotalsGrid.Columns.Where(x => x.Name.Contains(index)).FirstOrDefault();
                if (column != null)
                {
                    ColumnBase cb = (ColumnBase)column;
                    if (i < count)
                        cb.Visible = cb.ShowInColumnChooser = true;
                    else
                        cb.Visible = cb.ShowInColumnChooser = false;
                }
            }

            inputs = new List<PropValuePair>();
            if (cmbReportType.SelectedIndex == 0)
            {
                inputs.Add(PropValuePair.GenereteParameter("PrDate", typeof(DateTime), String.Format("{0:d}", BalanceDate.DateTime)));
            }
            else
            {
                inputs.Add(PropValuePair.GenereteParameter("DueDate", typeof(DateTime), String.Format("{0:d}", BalanceDate.DateTime)));
                if (chkSkipOnHold.IsChecked == true)
                    inputs.Add(PropValuePair.GenereteParameter("SkipOnHold", typeof(Int32), "1"));
            }
            if (cmbCurrency.SelectedIndex > 0)
            {
                var currency = AppEnums.Currencies.IndexOf(cmbCurrency.SelectedItem.ToString());
                inputs.Add(PropValuePair.GenereteParameter("Currency", typeof(Int32), Convert.ToString(currency)));
            }

            if (reportType < 0 || interval == 0 || count == 0)
                return;
            inputs.Add(PropValuePair.GenereteParameter("Interval", typeof(Int32), intervalEdit.Text));
            inputs.Add(PropValuePair.GenereteParameter("Count", typeof(Int32), countEdit.Text));
            inputs.Add(PropValuePair.GenereteParameter("Journal", typeof(string), cmbJournal.Text));
            
            if (debtorFilterValues != null)
                inputs.AddRange(debtorFilterValues);

            var t = dgDebtorTotalsGrid.Filter(inputs);
            SetHeader();
            await t;

            var Debs = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            var lst = (IEnumerable<DebtorTotalClient>)dgDebtorTotalsGrid.ItemsSource;
            if (lst == null)
                return;
            if (lst != null)
            {
                foreach (var rec in lst)
                {
                    rec.deb = (Uniconta.DataModel.Debtor)Debs.Get(rec._Account);
                    var a = rec._Amount;
                    double tot = 0d;
                    for (int i = a.Length; (--i >= 0);)
                        tot += a[i];
                    rec._Total = tot;
                }
            }
            dgDebtorTotalsGrid.UpdateTotalSummary();
            /*
            var N = dgDebtorTotalsGrid.VisibleRowCount;
            for (int i = 0; i < N; i++)
            {
                double totalAmount = 0;
                int rowHandle = dgDebtorTotalsGrid.GetRowHandleByVisibleIndex(i);
                foreach (var column in dgDebtorTotalsGrid.Columns)
                {
                    var colName = column.Name;
                    if (column.Visible && colName != "Account" && colName != "Name")
                    {
                        if (colName != "Total")
                            totalAmount += (Double)dgDebtorTotalsGrid.GetCellValue(rowHandle, column);
                        else
                            dgDebtorTotalsGrid.SetCellValue(rowHandle, column, totalAmount);
                    }
                }
            }
            */

            totalAmountByDateChart.DataSource = CreateDataForChart();
            totalAmountByDateChart.Visibility = Visibility.Visible;
        }

        private List<Chart> CreateDataForChart()
        {
            var list = new List<Chart>();
            foreach (var column in dgDebtorTotalsGrid.Columns)
            {
                if (column.HasTotalSummaries && column.Visible && column.Name != "Total")
                {
                    var summaryHeader = column.HeaderCaption;
                    var totalSummaries = dgDebtorTotalsGrid.GetTotalSummaryValue(column.TotalSummaries[0].Item);
                    list.Add(new Chart { Date = Convert.ToString(summaryHeader), Amount = Convert.ToDouble(totalSummaries) });
                }
            }
            return list;
        }

        private void SetHeader()
        {
            bool DueReport = (cmbReportType.SelectedIndex != 0);
            int count = (int)NumberConvert.ToInt(countEdit.Text);
            int interval = (int)NumberConvert.ToInt(intervalEdit.Text);
            var date = BalanceDate.DateTime;
            if (date == DateTime.MinValue)
                date = DateTime.Today;
            bool MonthStep = (interval == 31);
            if (MonthStep)
                interval = DateTime.DaysInMonth(date.Year, date.Month);
            bool EndMonth = (MonthStep && date.Day == interval);
            for (int i = 0; i < count; i++)
            {
                DateTime NextDate;
                if (MonthStep)
                {
                    if (EndMonth)
                    {
                        NextDate = date.AddDays(-interval);
                        interval = DateTime.DaysInMonth(NextDate.Year, NextDate.Month);
                    }
                    else
                        NextDate = date.AddMonths(-1);
                }
                else
                    NextDate = date.AddDays(-interval);

                string hdr;
                if (i + 1 < count)
                {
                    if (DueReport && i == 0)
                    {
                        NextDate = date;
                        hdr = String.Format(">= {0:d}", date.AddDays(1));
                    }
                    else
                        hdr = String.Format("{0:d} - {1:d}", date, NextDate.AddDays(1));
                }
                else if (count > 1)
                    hdr = Uniconta.ClientTools.Localization.lookup("Older");
                else
                    hdr = String.Format("{0:d}", date);
                var index = NumberConvert.ToString(i);
                GridColumn col = dgDebtorTotalsGrid.Columns.Where(x => x.Name.Contains(index)).FirstOrDefault();
                if (col != null)
                    col.Header = hdr;
                date = NextDate;
            }
        }

        private void cmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndex = cmbReportType.SelectedIndex;
            if (selectedIndex == 1)
            {
                chkSkipOnHold.IsEnabled = true;
            }
            else
            {
                chkSkipOnHold.IsEnabled = false;
                chkSkipOnHold.IsChecked = false;
            }
        }
    }

    public class Chart
    {
        public string Date { get; set; }
        public Double Amount { get; set; }
    }

}
