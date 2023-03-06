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
using DevExpress.Xpf.Grid;
using System.ComponentModel.DataAnnotations;
using System.Collections;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorTotalClient : CreditorAgeTotalClient
    {
        public double _Total;
        [Display(Name = "Total", ResourceType = typeof(GLTableText))]
        public double Total { get { return _Total; } set { _Total = value; } }
    }

    public class CreditorTotalsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTotalClient); } }

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            base.PrintGrid(PrintReportName, printparam, format, page);
        }
        public override IComparer GridSorting { get { return new DCAgeCompare(); } }

        public string PrintReportName { get; set; }
    }
    public partial class CreditorTotals : GridBasePage
    {
        static int reportType = 0;
        static DateTime balDate = BasePage.GetSystemDefaultDate();
        static int interval = 31;
        static int count = 5;

        CWServerFilter creditorFilterDialog;
        bool creditorFilterCleared;
        public TableField[] CreditorUserFields { get; set; }

        public CreditorTotals(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorTotalsGrid);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            cmbReportType.SelectedIndex = reportType;
            BalanceDate.DateTime = balDate;
            if (interval > 0)
                intervalEdit.EditValue = interval;
            if (count > 0)
                countEdit.EditValue = count;
            dgCreditorTotalsGrid.ShowTotalSummary();
            chkSkipOnHold.IsEnabled = false;
            LoadType(typeof(Uniconta.DataModel.Creditor));
            cmbCurrency.ItemsSource = AppEnums.Currencies.GetLabels();
            cmbCurrency.SelectedIndex = 0;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            GetShowHideChart();
            IntialLoad();
        }

        void IntialLoad()
        {
            TransactionReport.SetDailyJournal(cmbJournal, api);
        }

        public override Task InitQuery() { return null; }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Search":
                    LoadData();
                    break;
                case "CreditorFilter":
                    if (creditorFilterDialog == null)
                    {
                        if (creditorFilterCleared)
                            creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), null, null, CreditorUserFields);
                        else
                            creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), null, null, CreditorUserFields);
                        creditorFilterDialog.GridSource = dgCreditorTotalsGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        creditorFilterDialog.Closing += creditorFilterDialog_Closing;
                        creditorFilterDialog.Show();
                    }
                    else
                    {
                        creditorFilterDialog.GridSource = dgCreditorTotalsGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        creditorFilterDialog.Show(true);
                    }
                    break;

                case "ClearCreditorFilter":
                    creditorFilterDialog = null;
                    creditorFilterValues = null;
                    creditorFilterCleared = true;
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

        private void SetShowHideChart(bool hideGreen)
        {
            if (ibase == null)
                return;
            if (hideGreen)
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

        public IEnumerable<PropValuePair> creditorFilterValues;
        public FilterSorter creditorPropSort;

        void creditorFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (creditorFilterDialog.DialogResult == true)
            {
                creditorFilterValues = creditorFilterDialog.PropValuePair;
                creditorPropSort = creditorFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            creditorFilterDialog.Hide();
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
            dgCreditorTotalsGrid.PrintReportName = string.Format("{0} {1} ({2})", Uniconta.ClientTools.Localization.lookup("Creditor"), Uniconta.ClientTools.Localization.lookup("AgeingReport"), reportTypeContent);
            reportType = cmbReportType.SelectedIndex;
            interval = (int)NumberConvert.ToInt(intervalEdit.Text);
            count = (int)NumberConvert.ToInt(countEdit.Text);
            for (int i = 0; i < 10; i++)
            {
                var index = NumberConvert.ToString(i);
                GridColumn column = dgCreditorTotalsGrid.Columns.Where(x => x.Name.Contains(index)).FirstOrDefault();
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

            if (creditorFilterValues != null)
                inputs.AddRange(creditorFilterValues);

            var t = dgCreditorTotalsGrid.Filter(inputs);
            SetHeader();
            await t;

            var Debs = api.GetCache(typeof(Uniconta.DataModel.Creditor));
            var lst = (IEnumerable<CreditorTotalClient>)dgCreditorTotalsGrid.ItemsSource;
            if (lst == null)
                return;
            foreach (var rec in lst)
            {
                rec.cre = (Uniconta.DataModel.Creditor)Debs.Get(rec._Account);
                var a = rec._Amount;
                double tot = 0d;
                for (int i = a.Length; (--i >= 0);)
                    tot += a[i];
                rec._Total = tot;
            }
            dgCreditorTotalsGrid.UpdateTotalSummary();
            /*
            var N = dgCreditorTotalsGrid.VisibleRowCount;
            for (int i = 0; i < N; i++)
            {
                double totalAmount = 0;
                int rowHandle = dgCreditorTotalsGrid.GetRowHandleByVisibleIndex(i);
                foreach (var column in dgCreditorTotalsGrid.Columns)
                {
                    var colName = column.Name;
                    if (column.Visible && colName != "Account" && colName != "Name")
                    {
                        if (colName != "Total")
                            totalAmount += (Double)dgCreditorTotalsGrid.GetCellValue(rowHandle, column);
                        else
                            dgCreditorTotalsGrid.SetCellValue(rowHandle, column, totalAmount);
                    }
                }
            }
            */

            totalAmountByDateChart.DataSource = CreateDataForChart();
            totalAmountByDateChart.Visibility = Visibility.Visible;
        }

        private List<CreditorChart> CreateDataForChart()
        {
            var list = new List<CreditorChart>();
            foreach (var column in dgCreditorTotalsGrid.Columns)
            {
                if (column.HasTotalSummaries && column.Visible && column.Name != "Total")
                {
                    var summaryHeader = column.HeaderCaption;
                    var totalSummaries = dgCreditorTotalsGrid.GetTotalSummaryValue(column.TotalSummaries[0].Item);
                    list.Add(new CreditorChart { Date = Convert.ToString(summaryHeader), Amount = Convert.ToDouble(totalSummaries) });
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
                GridColumn col = dgCreditorTotalsGrid.Columns.Where(x => x.Name.Contains(index)).FirstOrDefault();
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

    public class CreditorChart
    {
        public string Date { get; set; }
        public Double Amount { get; set; }
    }
}
