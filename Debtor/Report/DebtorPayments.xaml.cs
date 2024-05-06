using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;
using Uniconta.ClientTools.Util;
using System.IO;
using System.Reflection;
using DevExpress.Xpf.Grid.Native;
using DevExpress.Xpf.Grid;
using UnicontaClient.Controls;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools;
using UnicontaClient.Utilities;
using Uniconta.API.System;
using Uniconta.Common.Utility;
using Microsoft.Win32;
using System.Windows.Markup.Localizer;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorTransPayment : DebtorTransOpenClient
    {
        public double _SumAmount;
        [Display(Name = "Amount", ResourceType = typeof(GLDailyJournalText))]
        public double SumAmount { get { return _SumAmount; } }

        public double _SumAmountCur;
        [Display(Name = "AmountCur", ResourceType = typeof(GLDailyJournalText))]
        public double SumAmountCur { get { return _SumAmountCur; } }
    }

    public class DebtorPaymentStatementList
    {
        [Display(Name = "Account", ResourceType = typeof(GLTableText))]
        public string AccountNumber { get; set; }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string Name { get; set; }

        public DebtorTransPayment[] ChildRecords { get; set; }

        public double _SumAmount;
        public double SumAmount { get { return _SumAmount; } }

        public double _SumAccountCur;
        public double SumAccountCur { get { return _SumAccountCur; } }

        public double _collectionAmount;
        public double CollectionAmount { get { return _collectionAmount; } }

        public double _sumFeeAmount;
        public double SumFeeAmount { get { return _sumFeeAmount; } }

    }

    public class DebtorPaymentsGrid : CorasauDataGridClient
    {
        public delegate void PrintClickDelegate();
        public event PrintClickDelegate OnPrintClick;

        public override Type TableType { get { return typeof(DebtorTransPayment); } }
        public override bool Readonly { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public bool IsStandardPrint = false;

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            if (!IsStandardPrint && format == null)
                OnPrintClick();
            else
                base.PrintGrid(reportName, printparam, format, page);
        }

        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            var arr = (DebtorTransPayment[])Arr;
            for (int i = 0; i < arr.Length; i++)
            {
                var rec = arr[i];
                rec._FeeAmount = 0;
                rec._PaymentCharge = 0;
            }
        }
    }

    public partial class DebtorPayments : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string ToAccount { get; set; }

        List<DebtorPaymentStatementList> statementList;

        public override string NameOfControl { get { return TabControls.DebtorPayments; } }
        public override bool IsDataChaged { get { return false; } }

        public DebtorPayments(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        public DebtorPayments(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        bool AddInterest;
        SQLCache accountCache;

        private void InitPage()
        {
            InitializeComponent();
            dgDebtorTranOpenGrid.api = api;
            SetRibbonControl(localMenu, dgDebtorTranOpenGrid);
            dgDebtorTranOpenGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorTranOpenGrid.OnPrintClick += DgDebtorTranOpenGrid_OnPrintClick;
            statementList = new List<DebtorPaymentStatementList>();
            if (toDate == DateTime.MinValue)
                toDate = txtDateTo.DateTime.Date;
            txtDateTo.DateTime = toDate;
            txtDateFrm.DateTime = fromDate;
            neDunningDays.Text = NumberConvert.ToStringNull(noDaysSinceLastDunning);
            dgDebtorTranOpenGrid.ShowTotalSummary();
            cmbPrintintPreview.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Internal"), Uniconta.ClientTools.Localization.lookup("External") };
            cmbPrintintPreview.SelectedIndex = 1;
            tbDateFrom.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("From"), Uniconta.ClientTools.Localization.lookup("DueDate"));
            tbDateTo.Text = string.Format(Uniconta.ClientTools.Localization.lookup("ToOBJ"), Uniconta.ClientTools.Localization.lookup("DueDate"));
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            accountCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            LoadType(new[] { typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.PaymentTerm) });
        }

        private void DgDebtorTranOpenGrid_OnPrintClick()
        {
            dgDebtorTranOpenGrid.IsStandardPrint = false;
            if (dgDebtorTranOpenGrid.ItemsSource != null)
                PrintData();
        }

        async private void PrintData()
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                //Get Company related details

                byte[] logo = await UtilCommon.GetLogo(api);
                var Comp = api.CompanyEntity;

                var companyClient = Comp.CreateUserType<CompanyClient>();
                StreamingManager.Copy(Comp, companyClient);

                lastMessage = null; // just to reload message in case it has changed
                LoadDataForReport();

                if (statementList.Count > 0)
                {
                    var FirstLine = statementList[0].ChildRecords[0];
                    var xtraReports = await GeneratePrintReport(statementList, companyClient, CWSetFeeAmount._PrDate, logo, FirstLine._Code);
                    if (xtraReports.Count() > 0)
                    {
                        var reportName = AddInterest ? Uniconta.ClientTools.Localization.lookup("InterestNote") : Uniconta.ClientTools.Localization.lookup("CollectionLetter");
                        var dockName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), reportName);
                        AddDockItem(UnicontaTabs.StandardPrintReportPage, new object[] { xtraReports, reportName }, dockName);
                    }
                }
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("DebtorPayment.PrintData(), CompanyId={0}", api.CompanyId));
                UnicontaMessageBox.Show(ex);
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
        }

        string lastMessage;
        Language messageLanguage;

        async private void OpenOutlook()
        {
            try
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
                var selectedAccount = ((DebtorTransPayment)dgDebtorTranOpenGrid.SelectedItem).Account;
                var code = ((DebtorTransPayment)dgDebtorTranOpenGrid.SelectedItem)._Code;
                byte[] logo = await UtilCommon.GetLogo(api);
                var Comp = api.CompanyEntity;

                var companyClient = Comp.CreateUserType<CompanyClient>();
                StreamingManager.Copy(Comp, companyClient);

                lastMessage = null; // just to reload message in case it has changed
                LoadDataForReport(code);

                var debtor = accountCache.Get(selectedAccount) as Uniconta.DataModel.Debtor;
                var selectedAccountstatementList = statementList.Where(p => p.AccountNumber == selectedAccount).ToList();
                if (selectedAccountstatementList.Count > 0)
                {
                    var FirstLine = selectedAccountstatementList[0].ChildRecords[0];
                    var paymentStandardReport = await GeneratePrintReport(selectedAccountstatementList, companyClient, CWSetFeeAmount._PrDate, logo, FirstLine._Code, true);
                    if (paymentStandardReport != null && paymentStandardReport.Count() == 1)
                        InvoicePostingPrintGenerator.OpenReportInOutlook(api, paymentStandardReport.First(), debtor, FirstLine._Code);
                }
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
            finally { busyIndicator.IsBusy = false; }
        }

        private void UpdateDate(IEnumerable<DebtorTransPayment> payments)
        {
            if (payments != null)
                new ReportAPI(api).UpdateCollectionLog(payments, CWSetFeeAmount._PrDate);
        }

        async private Task<IEnumerable<IPrintReport>> GeneratePrintReport(IEnumerable<DebtorPaymentStatementList> paymentStatementList, CompanyClient companyClient, DateTime date, byte[] logo, DebtorEmailType debtorEmailType, bool outlook = false)
        {

            var updateWithLastCollectionLetter = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateWithLastCollection"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.YesNo);
            var iprintReportList = new List<IPrintReport>();

            List<DebtorTransPayment> updates = new List<DebtorTransPayment>(100);
            foreach (var selectedItem in paymentStatementList.ToList())
            {
                var FirstLine = selectedItem.ChildRecords[0];
                var collectionPrint = await GenerateStandardCollectionReport(companyClient, date, selectedItem, logo, FirstLine._Code);
                if (collectionPrint == null)
                    continue;

                var standardReports = new[] { collectionPrint };
                var standardPrint = new StandardPrintReport(api, standardReports, this.AddInterest ? chkShowCurrency.IsChecked == true ? (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.InterestNoteCurrency :
                    (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.InterestNote : chkShowCurrency.IsChecked == true ? (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.CollectionLetterCurrency :
                    (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.CollectionLetter);
                await standardPrint.InitializePrint();

                if (standardPrint.Report != null)
                {
                    iprintReportList.Add(standardPrint);
                    updates.AddRange(selectedItem.ChildRecords);
                }
            }
            if (updates.Count > 0 && updateWithLastCollectionLetter == MessageBoxResult.Yes)
            {
                if (outlook && iprintReportList.Count() == 1)
                    UpdateDate(paymentStatementList.FirstOrDefault()?.ChildRecords);
                else
                    UpdateDate(updates);
            }
            return iprintReportList;
        }

        async private Task<DebtorCollectionReportClient> GenerateStandardCollectionReport(CompanyClient companyClient, DateTime dueDate, DebtorPaymentStatementList selectedItem, byte[] logo, DebtorEmailType debtorEmailType)
        {
            var dbClientTotal = selectedItem.ChildRecords[0];
            var debtorType = Uniconta.Reports.Utilities.ReportUtil.GetUserType(typeof(DebtorClient), api.CompanyEntity);
            var debtorClient = Activator.CreateInstance(debtorType) as DebtorClient;
            StreamingManager.Copy(dbClientTotal.Debtor, debtorClient);

            var lan = UtilDisplay.GetLanguage(debtorClient, companyClient);
            if (lastMessage == null || messageLanguage != lan)
            {
                messageLanguage = lan;
                var res = await UtilCommon.GetDebtorMessageClient(api, lan, debtorEmailType);
                if (res != null)
                    lastMessage = res._Text;
                else
                    lastMessage = string.Empty;
            }

            debtorClient.OpenTransactions = selectedItem.ChildRecords.ToArray();
            string _reportName = StandardReportUtility.GetLocalizedReportName(debtorClient, companyClient, debtorEmailType.ToString());

            return new DebtorCollectionReportClient(companyClient, debtorClient, dueDate, logo, this.AddInterest, _reportName, lastMessage);
        }

        public override object GetPrintParameter()
        {
            int dataRowCount = statementList.Count;
            for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                dgDebtorTranOpenGrid.ExpandMasterRow(rowHandle);

            return base.GetPrintParameter();
        }

        static DateTime fromDate;
        static DateTime toDate;
        static int noDaysSinceLastDunning = 7;

        protected override Filter[] DefaultFilters()
        {
            var filters = new List<Filter>()
            {
                new Filter() { name = "OnHold", value = "0" },
                new Filter() { name = "Paid", value = "0" }
            };

            if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
            {
                string filter;
                if (fromDate != DateTime.MinValue)
                    filter = String.Format("{0:d}..", fromDate);
                else
                    filter = "..";
                if (toDate != DateTime.MinValue)
                    filter += String.Format("{0:d}", toDate);

                filters.Add(new Filter() { name = "DueDate", value = filter });
            }

            /*
            if (noDaysSinceLastDunning != 0)
            {
                var dayValid = BasePage.GetSystemDefaultDate().AddDays(-noDaysSinceLastDunning);
                filters.Add(new Filter() { name = "LastCollectionLetter", value = String.Format("..{0:d};null", dayValid) });
            }
            */

            return filters.ToArray();
        }

        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties dateSort = new SortingProperties("DueDate");
            SortingProperties VoucherSort = new SortingProperties("Date");
            return new SortingProperties[] { dateSort, VoucherSort };
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorTranOpenGrid.SelectedItem as DebtorTransOpenClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    dgDebtorTranOpenGrid.RemoveFocusedRowFromGrid();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgDebtorTranOpenGrid.syncEntity, api, busyIndicator);
                    break;
                case "GenerateJournalLines":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    CWImportToLine cwLine = new CWImportToLine(api, CWSetFeeAmount._PrDate);
                    cwLine.DialogTableId = 2000000034;
                    cwLine.Closed
                        += async delegate
                        {
                            if (cwLine.DialogResult == true && !string.IsNullOrEmpty(cwLine.Journal))
                            {
                                busyIndicator.IsBusy = true;
                                Uniconta.API.GeneralLedger.PostingAPI posApi = new Uniconta.API.GeneralLedger.PostingAPI(api);
                                var LineNumber = (int)(await posApi.MaxLineNumber(cwLine.Journal)) + 2;

                                NumberSerieAPI numberserieApi = new NumberSerieAPI(posApi);
                                int nextVoucherNumber = 0;

                                SQLCache payments = api.GetCache(typeof(Uniconta.DataModel.PaymentTerm));
                                string payment = (from pay in (IEnumerable<Uniconta.DataModel.PaymentTerm>)payments.GetNotNullArray where pay._UseForCollection select pay._Payment).FirstOrDefault();

                                SQLCache journalCache = api.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
                                var DJclient = (Uniconta.DataModel.GLDailyJournal)journalCache.Get(cwLine.Journal);
                                var listLineClient = new List<Uniconta.DataModel.GLDailyJournalLine>();

                                if (!DJclient._GenerateVoucher && !DJclient._ManualAllocation)
                                    nextVoucherNumber = (int)await numberserieApi.ViewNextNumber(DJclient._NumberSerie);

                                var visibleRows = dgDebtorTranOpenGrid.GetVisibleRows() as IEnumerable<DebtorTransPayment>;
                                if (cwLine.AggregateAmount)
                                {
                                    var rowsGroupBy = visibleRows.Where(p => p._Code != 0).GroupBy(a => new { a.Account, a._Code });
                                    foreach (var group in rowsGroupBy)
                                    {
                                        string invoice;
                                        double FeeAmount, Charge;
                                        var rec = group.FirstOrDefault();
                                        if (group.Count() > 1)
                                        {
                                            FeeAmount = group.Sum(p => p._FeeAmount);
                                            Charge = group.Sum(p => p._PaymentCharge);
                                            invoice = null;
                                        }
                                        else
                                        {
                                            FeeAmount = rec._FeeAmount;
                                            Charge = rec._PaymentCharge;
                                            invoice = rec.InvoiceAN;
                                        }
                                        if (FeeAmount > 0)
                                        {
                                            CreateGLDailyJournalLine(listLineClient, group.Key.Account, FeeAmount, Charge, invoice, DJclient, LineNumber, cwLine.Date, cwLine.TransType, cwLine.BankAccount, rec.Currency, nextVoucherNumber, payment);
                                            if (nextVoucherNumber != 0)
                                                nextVoucherNumber++;
                                        }
                                    }
                                }
                                else
                                {
                                    DebtorTransPayment lastRec = null;
                                    foreach (var row in visibleRows)
                                    {
                                        if (row._Code != 0 && (row._FeeAmount > 0 || row._PaymentCharge > 0) && !object.ReferenceEquals(row, lastRec))
                                        {
                                            lastRec = row;
                                            CreateGLDailyJournalLine(listLineClient, row.Account, row._FeeAmount, row._PaymentCharge, row.InvoiceAN, DJclient, LineNumber, cwLine.Date, cwLine.TransType, cwLine.BankAccount, row.Currency, nextVoucherNumber, payment);
                                            if (nextVoucherNumber != 0)
                                                nextVoucherNumber++;
                                        }
                                    }
                                }

                                if (listLineClient.Count > 0)
                                {
                                    ErrorCodes errorCode = await api.Insert(listLineClient);
                                    busyIndicator.IsBusy = false;
                                    if (errorCode != ErrorCodes.Succes)
                                        UtilDisplay.ShowErrorCode(errorCode);
                                    else
                                    {
                                        if (nextVoucherNumber != 0)
                                            numberserieApi.SetNumber(DJclient._NumberSerie, nextVoucherNumber - 1);

                                        var text = string.Concat(Uniconta.ClientTools.Localization.lookup("GenerateJournalLines"), "; ", Uniconta.ClientTools.Localization.lookup("Completed"),
                                            Environment.NewLine, string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Journallines")), " ?");
                                        var select = UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel);
                                        if (select == MessageBoxResult.OK)
                                            AddDockItem(TabControls.GL_DailyJournalLine, DJclient, null, null, true);
                                    }
                                }
                            }
                            busyIndicator.IsBusy = false;
                        };
                    cwLine.Show();
                    break;
                case "AddInterest":
                    if (selectedItem != null)
                        SetFee(true);
                    break;
                case "AddCollection":
                    if (selectedItem != null)
                        SetFee(false);
                    break;
                case "SendAllEmail":
                    SendReport((IEnumerable<DebtorTransPayment>)dgDebtorTranOpenGrid.GetVisibleRows());
                    break;
                case "SendMarkedEmail":
                    var markedRows = dgDebtorTranOpenGrid.SelectedItems?.Cast<DebtorTransPayment>();
                    if (markedRows != null)
                        SendReport(markedRows);
                    break;
                case "SendCurrentEmail":
                    if (dgDebtorTranOpenGrid.SelectedItem != null)
                    {
                        var cwSendInvoice = new CWSendInvoice();
                        cwSendInvoice.DialogTableId = 2000000031;
                        cwSendInvoice.Closed += delegate
                        {
                            SendReport(new[] { (DebtorTransPayment)dgDebtorTranOpenGrid.SelectedItem }, cwSendInvoice.Emails, cwSendInvoice.sendOnlyToThisEmail);
                        };
                        cwSendInvoice.Show();
                    }
                    break;
                case "SendAsOutlook":
                    if (dgDebtorTranOpenGrid.SelectedItem != null)
                        OpenOutlook();
                    break;
                case "Search":
                    Search();
                    break;
                case "CollectionLetterLog":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CollectionLetterLog"), selectedItem.InvoiceAN);
                        AddDockItem(TabControls.DebtorTransCollectPage, dgDebtorTranOpenGrid.syncEntity, header);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        Uniconta.DataModel.GLDailyJournalLine CreateGLDailyJournalLine(List<Uniconta.DataModel.GLDailyJournalLine> lst, string account, double feeAmount, double charge, string invoice, Uniconta.DataModel.GLDailyJournal dailyJournal, int lineNumber, DateTime date, string transType, string offsetAccount, Currencies? currency, int nextVoucherNumber, string payment)
        {
            var line = new Uniconta.DataModel.GLDailyJournalLine();
            line.SetMaster(dailyJournal);
            if (this.AddInterest)
                line._DCPostType = DCPostType.InterestFee;
            else
            {
                line._DCPostType = DCPostType.CollectionLetter;
                line._Payment = payment;
            }
            line._LineNumber = lineNumber + lst.Count;
            line._Date = date;
            line._DueDate = date;
            line._Invoice = invoice;
            line._TransType = transType;
            line._OffsetAccount = offsetAccount;
            line._Voucher = nextVoucherNumber;

            line._AccountType = (byte)GLJournalAccountType.Debtor;
            line._Account = account;
            var acc = (DCAccount)accountCache.Get(account);
            if (acc != null)
            {
                if (acc._Dim1 != null)
                    line._Dim1 = acc._Dim1;
                if (acc._Dim2 != null)
                    line._Dim2 = acc._Dim2;
                if (acc._Dim3 != null)
                    line._Dim3 = acc._Dim3;
                if (acc._Dim4 != null)
                    line._Dim4 = acc._Dim4;
                if (acc._Dim5 != null)
                    line._Dim5 = acc._Dim5;
            }
            if (currency.HasValue)
            {
                line._DebitCur = feeAmount;
                line._Currency = (byte)currency;
            }
            else
                line._Debit = feeAmount;

            lst.Add(line);
            if (charge != 0)
            {
                var chargelin = CreateGLDailyJournalLine(lst, account, charge, 0, invoice, dailyJournal, lineNumber, date, transType, offsetAccount, currency, nextVoucherNumber, payment);
                chargelin._DCPostType = DCPostType.PaymentCharge;
            }
            return line;
        }

        void SendReport(IEnumerable<DebtorTransPayment> dcTransOpenClientlist, string emails = null, bool onlyThisEmail = false)
        {
            if (dcTransOpenClientlist.Count() > 0)
                ExecuteDebtorCollection(api, busyIndicator, dcTransOpenClientlist, chkShowCurrency.IsChecked == true, emails, onlyThisEmail, AddInterest);
        }

        static public void ExecuteDebtorCollection(CrudAPI Api, BusyIndicator busyIndicator, IEnumerable<DCTransOpen> dcTransOpenList, bool isCurrencyReport, string emails, bool onlyThisEmail, bool AddInterest)
        {
            var rapi = new ReportAPI(Api);

            var cwDateSelector = new CWDateSelector();
            cwDateSelector.DialogTableId = 2000000025;
            cwDateSelector.Closed += async delegate
            {
                if (cwDateSelector.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    var result = await rapi.DebtorCollection(dcTransOpenList, cwDateSelector.SelectedDate, isCurrencyReport, emails, onlyThisEmail);
                    busyIndicator.IsBusy = false;

                    if (result == ErrorCodes.Succes)
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), AddInterest ? Uniconta.ClientTools.Localization.lookup("InterestNote") : Uniconta.ClientTools.Localization.lookup("CollectionLetter")),
                            Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            cwDateSelector.Show();
        }

        void SetFee(bool AddInterest)
        {
            CWSetFeeAmount cwwin = new CWSetFeeAmount(AddInterest);
            cwwin.DialogTableId = AddInterest ? 2000000070 : 2000000071;
            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    var day = cwwin.PrDate.AddDays(-noDaysSinceLastDunning);

                    this.AddInterest = AddInterest;
                    var debtorPayments = dgDebtorTranOpenGrid.GetVisibleRows() as IEnumerable<DebtorTransPayment>;
                    if (AddInterest)
                    {
                        if (cwwin.value != 0)
                        {
                            bool prDay = (cwwin.NoOfDays != "30");
                            bool _PerAccount = !cwwin.PerTransaction;
                            var desm = api.CompanyEntity.HasDecimals ? 2 : 0;
                            if (!cwwin.PerTransaction)
                            {
                                SetInterestFeeAmount(debtorPayments, 0, cwwin.PrDate, day, prDay, desm);
                                var selectedAccounts = debtorPayments.GroupBy(x => new { x.Account, x._Code }).Select(x => x.FirstOrDefault(x2 => x2._AmountOpen > 0 && !x2._OnHold));
                                SetInterestFeeAmount(selectedAccounts, cwwin.value, cwwin.PrDate, day, prDay, desm);
                            }
                            else
                                SetInterestFeeAmount(debtorPayments, cwwin.value, cwwin.PrDate, day, prDay, desm);
                        }
                    }
                    else
                    {
                        var currencuEnum = CurrencyUtil.Parse(cwwin.FeeCurrency);
                        var chargeCurrency = CurrencyUtil.Parse(cwwin.ChargeCurrency);

                        if (cwwin.PerTransaction)
                            SetFeeAmount(debtorPayments, cwwin.value, currencuEnum, cwwin.Charge, chargeCurrency, day, cwwin.CollectionLetterTypes, cwwin.FeeOnReminder, cwwin.FirstCollectionIndex);
                        else
                        {
                            SetFeeAmount(debtorPayments, 0, currencuEnum, 0, chargeCurrency, day, cwwin.CollectionLetterTypes, cwwin.FeeOnReminder, cwwin.FirstCollectionIndex);
                            var selectedAccounts = debtorPayments.GroupBy(x => new { x.Account, x._Code }).Select(x => x.FirstOrDefault(x2 => x2._AmountOpen > 0 && !x2._OnHold));
                            SetFeeAmount(selectedAccounts, cwwin.value, currencuEnum, cwwin.Charge, chargeCurrency, day, cwwin.CollectionLetterTypes, cwwin.FeeOnReminder, cwwin.FirstCollectionIndex);
                        }
                    }
                }
            };
            cwwin.Show();
        }

        private void SetInterestFeeAmount(IEnumerable<DebtorTransPayment> debtorPayments, double interestValue, DateTime prDate, DateTime day, bool prDay, int desm)
        {
            foreach (var rec in debtorPayments)
            {
                if (rec == null)
                    continue;
                rec.PaymentCharge = 0d;
                if (!rec._OnHold && (rec._LastInterest == DateTime.MinValue || rec._LastInterest <= day))
                {
                    var factor = interestValue;
                    if (prDay)
                    {
                        var dt = rec._LastInterest != DateTime.MinValue ? rec._LastInterest : (rec._DueDate != DateTime.MinValue ? rec._DueDate : rec.Date);
                        factor = Math.Max(interestValue * Math.Min((prDate - dt).TotalDays, 360d) / 30d, 0d);
                    }
                    rec.FeeAmount = Math.Round((rec._AmountOpenCur != 0 ? rec._AmountOpenCur : rec._AmountOpen) * factor / 100d, desm);
                    rec._Code = DebtorEmailType.InterestNote;
                    rec.NotifyPropertyChanged("Code");
                }
                else
                {
                    rec.FeeAmount = 0;
                    rec.Code = null;
                }
            }
        }

        private void SetFeeAmount(IEnumerable<DebtorTransPayment> debtorPayments, double feeAmount, Currencies feeCurrency, double ChargeAmount, Currencies chargeCurrency, DateTime day, int CollectionLetterTypes, bool FeeOnReminder, int FirstCollectionIndex)
        {
            foreach (var rec in debtorPayments)
            {
                if (rec != null)
                {
                    if (rec._AmountOpen > 0 && !rec._OnHold && (rec._LastCollectionLetter == DateTime.MinValue || rec._LastCollectionLetter <= day))
                    {
                        DebtorEmailType code = 0;
                        switch (rec._LastCollectionsLetterCode)
                        {
                            case DebtorEmailType.PaymentReminder: if ((CollectionLetterTypes & 0x02) != 0) code = DebtorEmailType.CollectionLetter1; break;
                            case DebtorEmailType.CollectionLetter1: if ((CollectionLetterTypes & 0x04) != 0) code = DebtorEmailType.CollectionLetter2; break;
                            case DebtorEmailType.CollectionLetter2: if ((CollectionLetterTypes & 0x08) != 0) code = DebtorEmailType.CollectionLetter3; break;
                            case DebtorEmailType.CollectionLetter3: if ((CollectionLetterTypes & 0x10) != 0) code = DebtorEmailType.Collection; break;
                            case DebtorEmailType.Collection: if ((CollectionLetterTypes & 0x10) != 0) code = DebtorEmailType.Collection; break;
                            default:
                                if ((CollectionLetterTypes & (0x01 << FirstCollectionIndex)) != 0)
                                    code = FirstCollectionIndex == 0 ? DebtorEmailType.PaymentReminder :
                                        (FirstCollectionIndex == 1 ? DebtorEmailType.CollectionLetter1 :
                                        (FirstCollectionIndex == 2 ? DebtorEmailType.CollectionLetter2 :
                                        (FirstCollectionIndex == 3 ? DebtorEmailType.CollectionLetter3 : DebtorEmailType.Collection)));
                                break;
                        }

                        if (code != 0)
                        {
                            rec._Code = code;
                            rec.NotifyPropertyChanged("Code");

                            if (rec.Currency == feeCurrency || (!rec.Currency.HasValue && feeCurrency == Currencies.XXX))
                                rec.FeeAmount = code != DebtorEmailType.PaymentReminder || FeeOnReminder ? feeAmount : 0;
                            else
                                rec.FeeAmount = 0;

                            if (rec.Currency == chargeCurrency || (!rec.Currency.HasValue && chargeCurrency == Currencies.XXX))
                                rec.PaymentCharge = code != DebtorEmailType.PaymentReminder || FeeOnReminder ? ChargeAmount : 0;
                            else
                                rec.PaymentCharge = 0;
                        }
                    }
                    else
                    {
                        rec.FeeAmount = 0;
                        rec.PaymentCharge = 0;
                        rec.Code = null;
                    }
                }
            }
        }

        private void Search()
        {
            if (txtDateFrm.Text == string.Empty)
                fromDate = DateTime.MinValue;
            else
                fromDate = txtDateFrm.DateTime.Date;

            if (txtDateTo.Text == string.Empty)
                toDate = DateTime.MinValue;
            else
                toDate = txtDateTo.DateTime.Date;

            noDaysSinceLastDunning = (int)NumberConvert.ToInt(neDunningDays.Text);

            Filter();
        }

        void LoadDataForReport(DebtorEmailType onlyCode = 0)
        {
            statementList.Clear();
            var visibleRows = dgDebtorTranOpenGrid.GetVisibleRows() as ICollection<DebtorTransPayment>;
            if (visibleRows.Count > 0)
            {
                string currentItem = null;
                DebtorEmailType curCode = 0;
                DebtorPaymentStatementList masterDbPymtStatement = null;
                List<DebtorTransPayment> dbTransClientChildList = new List<DebtorTransPayment>(20);
                double SumAmount = 0d, SumAmountCur = 0d, CollectionAmount = 0d, SumFee = 0d;
                Uniconta.ClientTools.Localization debtLocalize = null;
                var listOpenTrans = visibleRows.Where(r => (r._Code != 0 && (onlyCode == 0 || r._Code == onlyCode))).ToList();
                listOpenTrans.Sort(new DebtorTransOpenClient.transSort());
                foreach (var trans in listOpenTrans)
                {
                    if (trans.Account != currentItem || trans._Code != curCode)
                    {
                        if (masterDbPymtStatement != null)
                        {
                            masterDbPymtStatement.ChildRecords = dbTransClientChildList.ToArray();
                            statementList.Add(masterDbPymtStatement);
                        }

                        curCode = trans._Code;
                        currentItem = trans.Account;
                        var dbt = (Debtor)accountCache.Get(currentItem);
                        var lan = UtilDisplay.GetLanguage(dbt, api.CompanyEntity);
                        debtLocalize = Uniconta.ClientTools.Localization.GetLocalization(lan);

                        masterDbPymtStatement = new DebtorPaymentStatementList();
                        if (dbt != null)
                        {
                            masterDbPymtStatement.AccountNumber = dbt._Account;
                            masterDbPymtStatement.Name = dbt._Name;
                        }
                        SumAmount = SumAmountCur = CollectionAmount = SumFee = 0d;
                        dbTransClientChildList.Clear();
                    }

                    trans.Trans.LocOb = debtLocalize;

                    SumAmount += trans._AmountOpen;
                    trans._SumAmount = SumAmount;
                    masterDbPymtStatement._SumAmount = SumAmount;

                    SumFee += trans._FeeAmount;
                    masterDbPymtStatement._sumFeeAmount = SumFee;

                    CollectionAmount += (trans._AmountOpen + trans._FeeAmount);
                    masterDbPymtStatement._collectionAmount = CollectionAmount;

                    SumAmountCur += trans._AmountOpenCur;
                    trans._SumAmount = SumAmountCur;
                    masterDbPymtStatement._SumAccountCur = SumAmountCur;

                    dbTransClientChildList.Add(trans);
                }
                if (masterDbPymtStatement != null)
                {
                    masterDbPymtStatement.ChildRecords = dbTransClientChildList.ToArray();
                    statementList.Add(masterDbPymtStatement);
                }
            }
        }

        private void cmbPrintintPreview_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var cmbItems = sender as DimComboBoxEditor;
            dgDebtorTranOpenGrid.IsStandardPrint = (cmbItems.SelectedIndex == 0);
        }
    }
}


