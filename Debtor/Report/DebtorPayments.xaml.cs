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
#if !SILVERLIGHT
using UnicontaClient.Pages;
using Microsoft.Win32;
#endif

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

        public List<DebtorTransPayment> ChildRecords { get; set; }

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

        public bool IsStandardPrint = false;

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            if (!IsStandardPrint && format == null)
                OnPrintClick();
            else
                base.PrintGrid(reportName, printparam, format, page);
        }
    }

    public partial class DebtorPayments : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string ToAccount { get; set; }

        List<DebtorPaymentStatementList> statementList;


        public override string NameOfControl
        {
            get { return TabControls.DebtorPayments.ToString(); }
        }

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
        string collectionType;
        private void InitPage()
        {
            InitializeComponent();
            dgDebtorTranOpenGrid.api = api;
            SetRibbonControl(localMenu, dgDebtorTranOpenGrid);
            dgDebtorTranOpenGrid.BusyIndicator = busyIndicator;
            dgDebtorTranOpenGrid.api = api;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorTranOpenGrid.OnPrintClick += DgDebtorTranOpenGrid_OnPrintClick;
            statementList = new List<DebtorPaymentStatementList>();
            if (toDate == DateTime.MinValue)
                toDate = txtDateTo.DateTime.Date;
            txtDateTo.DateTime = toDate;
            txtDateFrm.DateTime = fromDate;
            dgDebtorTranOpenGrid.ShowTotalSummary();
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            accountCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor));
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

                byte[] logo = await UtilDisplay.GetLogo(api);
                var Comp = api.CompanyEntity;

                var companyClient = new CompanyClient();
                StreamingManager.Copy(api.CompanyEntity, companyClient);

                LoadDataForReport();
#if SILVERLIGHT
                if (dgDebtorTranOpenGrid.SelectedItem != null)
                {
                    var selectedAccount = ((DebtorTransPayment)dgDebtorTranOpenGrid.SelectedItem).Account;
                    var selectedItem = statementList.Where(p => p.AccountNumber == selectedAccount).First() as DebtorPaymentStatementList;
                    if (selectedItem.ChildRecords.Count == 0) return;

                    if (string.IsNullOrEmpty(collectionType))
                    {
                        CWCollectionLetter cwCollectionLetter = new CWCollectionLetter();
                        cwCollectionLetter.Closed += async delegate
                         {
                             if (cwCollectionLetter.DialogResult == true)
                                 await PrintDebtorPaymentStatementPage(companyClient, logo, selectedItem, cwCollectionLetter.Result);
                         };
                        cwCollectionLetter.Show();
                    }
                    else
                        await PrintDebtorPaymentStatementPage(companyClient, logo, selectedItem, collectionType);
                }
#else
                var layoutSelectedDebtorCollectionList = new Dictionary<string, List<IDebtorStandardReport>>();
                var comp = api.CompanyEntity;
                var layoutgrpCache = comp.GetCache(typeof(DebtorLayoutGroup)) ?? await comp.LoadCache(typeof(DebtorLayoutGroup), api);

                var xtraReports = new List<IPrintReport>();

                 int indexDebtorEmailType = 0;

                if (string.IsNullOrEmpty(collectionType))
                    indexDebtorEmailType = SelectCollectionType();
                else
                    indexDebtorEmailType = AppEnums.DebtorEmailType.IndexOf(collectionType);
                if (indexDebtorEmailType == -1)
                    return;

                var debtorEmailType = (DebtorEmailType)indexDebtorEmailType;

                foreach (var debt in statementList.ToList())
                {
                    var selectedItem = debt as DebtorPaymentStatementList;
                    if (selectedItem.ChildRecords.Count == 0) continue;

                    var collectionPrint = await GenerateStandardCollectionReport(companyClient, BasePage.GetSystemDefaultDate(), selectedItem, logo, debtorEmailType);
                    if (collectionPrint == null)
                        continue;

                    var standardReports = new IDebtorStandardReport[1] { collectionPrint };

                    IPrintReport standardPrint;
                    if (this.AddInterest)
                        standardPrint = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.InterestNote);
                    else
                        standardPrint = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.CollectionLetter);
                    await standardPrint.InitializePrint();

                    if (standardPrint.Report != null)
                        xtraReports.Add(standardPrint);
                }

                if (xtraReports.Count > 0)
                {
                    var reportName = AddInterest ? Uniconta.ClientTools.Localization.lookup("InterestNote") : Uniconta.ClientTools.Localization.lookup("CollectionLetter");
                    var dockName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), reportName);
                    AddDockItem(UnicontaTabs.StandardPrintReportPage, new object[] { xtraReports, reportName }, dockName);
                }
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("DebtorPayment.PrintData(), CompanyId={0}", api.CompanyId));
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
        }

#if !SILVERLIGHT
        async private Task<DebtorCollectionReportClient> GenerateStandardCollectionReport(CompanyClient companyClient, DateTime dueDate, DebtorPaymentStatementList selectedItem, byte[] logo, DebtorEmailType debtorEmailType)
        {
            DebtorCollectionReportClient debtorCollectionReportClient = null;

            var dbClientTotal = selectedItem.ChildRecords.FirstOrDefault();
            var debtorType = Uniconta.Reports.Utilities.ReportUtil.GetUserType(typeof(DebtorClient), api.CompanyEntity);
            var debtorClient = Activator.CreateInstance(debtorType) as DebtorClient;
            StreamingManager.Copy(dbClientTotal.Debtor, debtorClient);

            var debtorMessageClient = await Utility.GetDebtorMessageClient(api, debtorClient._Language, debtorEmailType);

            var debtorTransOpen = selectedItem.ChildRecords.Cast<DebtorTransOpenClient>().ToList();
            debtorClient.OpenTransactions = debtorTransOpen;
            string _reportName = StandardReportUtility.GetLocalizedReportName(debtorClient, companyClient, debtorEmailType.ToString());

            debtorCollectionReportClient = new DebtorCollectionReportClient(companyClient, debtorClient, dueDate, logo, this.AddInterest, _reportName, debtorMessageClient);

            return debtorCollectionReportClient;

        }

#elif SILVERLIGHT

        async private Task PrintDebtorPaymentStatementPage(CompanyClient companyClient, byte[] logo, DebtorPaymentStatementList selectedItem, string emailType)
        {
            int indexDebtorEmailType = AppEnums.DebtorEmailType.IndexOf(emailType);
            var debtorEmailType = (DebtorEmailType)indexDebtorEmailType;

            var dbClientTotal = selectedItem.ChildRecords.FirstOrDefault();
            if (dbClientTotal != null)
            {
                var debtorClient = new DebtorClient();
                StreamingManager.Copy(dbClientTotal.Debtor, debtorClient);

                var debtorMessageClient = await Utility.GetDebtorMessageClient(api, debtorClient._Language, debtorEmailType);
                string messageText = debtorMessageClient?.Text;

                var dbStatementCustomPrint = new DebtorPaymentStatementCustPrint(api, selectedItem, companyClient, debtorClient,
                 txtDateFrm.DateTime, txtDateTo.DateTime, logo, debtorEmailType.ToString(), this.AddInterest, messageText);

                object[] obj = new object[1];
                obj[0] = dbStatementCustomPrint as CustomPrintTemplateData;

                AddDockItem(TabControls.DebtorPaymentStatementPrintPage, obj, true, string.Format("{0} : {1},{2}", Uniconta.ClientTools.Localization.lookup("PrintPreview"), selectedItem.Name, selectedItem.AccountNumber));
            }
        }
#endif

        public override object GetPrintParameter()
        {
            int dataRowCount = statementList.Count;
            for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                dgDebtorTranOpenGrid.ExpandMasterRow(rowHandle);

            return base.GetPrintParameter();
        }

        static DateTime fromDate;
        static DateTime toDate;

        protected override Filter[] DefaultFilters()
        {
            Filter[] filters;
            if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
            {
                filters = new Filter[3];
                var dateFilter = new Filter() { name = "DueDate" };
                filters[2] = dateFilter;

                string filter;
                if (fromDate != DateTime.MinValue)
                    filter = String.Format("{0:d}..", fromDate);
                else
                    filter = "..";
                if (toDate != DateTime.MinValue)
                    filter += String.Format("{0:d}", toDate);
                dateFilter.value = filter;
            }
            else
                filters = new Filter[2];

            filters[0] = new Filter() { name = "OnHold", value = "0" };
            filters[1] = new Filter() { name = "Paid", value = "0" };
            return filters;
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
                    dgDebtorTranOpenGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null) return;
                    DebtorTransactions.ShowVoucher(dgDebtorTranOpenGrid.syncEntity, api, busyIndicator);
                    break;
                case "GenerateJournalLines":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    CWImportToLine cwLine = new CWImportToLine(api, GetSystemDefaultDate());
#if !SILVERLIGHT
                    cwLine.DialogTableId = 2000000034;
#endif
                    cwLine.Closed
                        += async delegate
                        {
                            if (cwLine.DialogResult == true && !string.IsNullOrEmpty(cwLine.Journal))
                            {
                                busyIndicator.IsBusy = true;
                                Uniconta.API.GeneralLedger.PostingAPI posApi = new Uniconta.API.GeneralLedger.PostingAPI(api);
                                long LineNumber = await posApi.MaxLineNumber(cwLine.Journal);

                                NumberSerieAPI numberserieApi = new NumberSerieAPI(posApi);
                                int nextVoucherNumber = 0;

                                SQLCache payments = api.GetCache(typeof(Uniconta.DataModel.PaymentTerm)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PaymentTerm));
                                string payment = (from pay in (IEnumerable<Uniconta.DataModel.PaymentTerm>)payments.GetNotNullArray where pay._UseForCollection select pay._Payment).FirstOrDefault();

                                SQLCache journalCache = api.GetCache(typeof(Uniconta.DataModel.GLDailyJournal)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal));
                                var DJclient = (Uniconta.DataModel.GLDailyJournal)journalCache.Get(cwLine.Journal);
                                var listLineClient = new List<Uniconta.DataModel.GLDailyJournalLine>();

                                if (!DJclient._GenerateVoucher && !DJclient._ManualAllocation)
                                    nextVoucherNumber = (int)await numberserieApi.ViewNextNumber(DJclient._NumberSerie);

                                var visibleRows = dgDebtorTranOpenGrid.GetVisibleRows() as IEnumerable<DebtorTransPayment>;
                                var rows = visibleRows.Where(p => p._FeeAmount > 0.0d && p._OnHold == false);
                                if (cwLine.AggregateAmount)
                                {
                                    var rowsGroupBy = rows.GroupBy(a => a.Account);
                                    foreach (var group in rowsGroupBy)
                                    {
                                        long invoice;
                                        double aggregatedFeeAmount;
                                        var rec = group.FirstOrDefault();
                                        if (group.Count() > 1)
                                        {
                                            aggregatedFeeAmount = group.Sum(p => p._FeeAmount);
                                            invoice = 0;
                                        }
                                        else
                                        {
                                            aggregatedFeeAmount = rec._FeeAmount;
                                            invoice = rec.Invoice;
                                        }
                                        listLineClient.Add(CreateGLDailyJournalLine(group.Key, aggregatedFeeAmount, invoice, DJclient, ++LineNumber, cwLine.Date, cwLine.TransType, cwLine.BankAccount, rec.Currency, nextVoucherNumber, payment));

                                        if (nextVoucherNumber != 0)
                                            nextVoucherNumber++;
                                    }
                                }
                                else
                                {
                                    foreach (var row in rows)
                                    {
                                        listLineClient.Add(CreateGLDailyJournalLine(row.Account, row._FeeAmount, row.Invoice, DJclient, ++LineNumber, cwLine.Date, cwLine.TransType, cwLine.BankAccount, row.Currency, nextVoucherNumber, payment));

                                        if (nextVoucherNumber != 0)
                                            nextVoucherNumber++;
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
                    if (selectedItem == null)
                        return;
                    SetFee(true);
                    break;
                case "AddCollection":
                    if (selectedItem == null)
                        return;
                    SetFee(false);
                    break;
                case "SendAllEmail":
                    var visibleAllRows = (IEnumerable<DebtorTransPayment>)dgDebtorTranOpenGrid.GetVisibleRows();
                    if (visibleAllRows != null && visibleAllRows.Count() > 0)
                        SendReport(visibleAllRows);
                    break;
                case "SendMarkedEmail":
                    var markedRows = dgDebtorTranOpenGrid.SelectedItems.Cast<DebtorTransPayment>();
                    if (markedRows != null && markedRows.Count() > 0)
                        SendReport(markedRows);
                    break;
                case "SendCurrentEmail":
                    if (dgDebtorTranOpenGrid.SelectedItem != null)
                    {
                        var cwSendInvoice = new CWSendInvoice();
#if !SILVERLIGHT
                        cwSendInvoice.DialogTableId = 2000000031;
#endif
                        cwSendInvoice.Closed += delegate
                         {
                             var selectedRow = new DebtorTransPayment[] { (DebtorTransPayment)dgDebtorTranOpenGrid.SelectedItem };
                             SendReport(selectedRow, cwSendInvoice.Emails, cwSendInvoice.sendOnlyToThisEmail);

                         };
                        cwSendInvoice.Show();
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private Uniconta.DataModel.GLDailyJournalLine CreateGLDailyJournalLine(string account, double feeAmount, long invoice, Uniconta.DataModel.GLDailyJournal dailyJournal, long lineNumber, DateTime date, string transType, string offsetAccount, Currencies? currency, int nextVoucherNumber, string payment)
        {
            var glDailJournallineclient = new Uniconta.DataModel.GLDailyJournalLine();
            glDailJournallineclient.SetMaster(dailyJournal);
            if (this.AddInterest)
                glDailJournallineclient._DCPostType = DCPostType.InterestFee;
            else
            {
                glDailJournallineclient._DCPostType = DCPostType.CollectionLetter;
                glDailJournallineclient._Payment = payment;
            }
            glDailJournallineclient._LineNumber = ++lineNumber;
            glDailJournallineclient._Date = date;
            glDailJournallineclient._Invoice = invoice;
            glDailJournallineclient._TransType = transType;
            glDailJournallineclient._AccountType = (byte)GLJournalAccountType.Debtor;
            glDailJournallineclient._Account = account;
            glDailJournallineclient._OffsetAccount = offsetAccount;
            glDailJournallineclient._Voucher = nextVoucherNumber;

            if (currency.HasValue)
            {
                glDailJournallineclient._DebitCur = feeAmount;
                glDailJournallineclient._Currency = (byte)currency;
            }
            else
                glDailJournallineclient._Debit = feeAmount;

            return glDailJournallineclient;
        }

        void SendReport(IEnumerable<DebtorTransPayment> paymentList, string emails = null, bool onlyThisEmail = false)
        {
            var dcTransOpenClientlist = paymentList;
            var n = dcTransOpenClientlist.Count();
            if (n > 0)
            {
                var feelist = new double[n];
                int i = 0;
                foreach (var r in dcTransOpenClientlist)
                    feelist[i++] = r._FeeAmount;

                if (!AddInterest)
                {
                    if (string.IsNullOrEmpty(collectionType))
                    {
#if !SILVERLIGHT
                        var selectedCollectionType = SelectCollectionType();
                        if (selectedCollectionType > -1)
                            GetResult(dcTransOpenClientlist, feelist, AddInterest, (DebtorEmailType)selectedCollectionType, emails, onlyThisEmail);

#elif SILVERLIGHT
                        var collectionLetterWin = new CWCollectionLetter();
                        collectionLetterWin.Closed += delegate
                        {
                            if (collectionLetterWin.DialogResult == true)
                            {
                                int indexDebtorEmailType = AppEnums.DebtorEmailType.IndexOf(collectionLetterWin.Result);
                                GetResult(dcTransOpenClientlist, feelist, AddInterest, (DebtorEmailType)indexDebtorEmailType, emails, onlyThisEmail);
                            }
                            else
                            {
                                busyIndicator.IsBusy = false;
                                return;
                            }
                        };
                        collectionLetterWin.Show();
#endif
                    }
                    else
                    {
                        int indexDebtorEmailType = AppEnums.DebtorEmailType.IndexOf(collectionType);
                        GetResult(dcTransOpenClientlist, feelist, AddInterest, (DebtorEmailType)indexDebtorEmailType, emails, onlyThisEmail);
                    }
                }
                else
                    GetResult(dcTransOpenClientlist, feelist, AddInterest, DebtorEmailType.InterestNote, emails, onlyThisEmail);
            }
        }

        private int SelectCollectionType()
        {
            int collectionType = -1;

            CWCollectionLetter collectionLetterWin = new CWCollectionLetter();
            collectionLetterWin.Closed += delegate
            {
                if (collectionLetterWin.DialogResult == true)
                    collectionType = AppEnums.DebtorEmailType.IndexOf(collectionLetterWin.Result);
                else
                    collectionType = -1;
            };

            collectionLetterWin.Show();

            return collectionType;
        }

        private void GetResult(IEnumerable<DCTransOpen> dcTransOpenList, IEnumerable<double> feelist, bool isAddInterest, DebtorEmailType emailType, string emails = null, bool onlyThisEmail = false)
        {
            var rapi = new ReportAPI(api);

            var cwDateSelector = new CWDateSelector();
#if !SILVERLIGHT
            cwDateSelector.DialogTableId = 2000000025;
#endif
            cwDateSelector.Closed += async delegate
            {
                if (cwDateSelector.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");

                    var result = await rapi.DebtorCollection(dcTransOpenList, feelist, cwDateSelector.SelectedDate, isAddInterest, emailType, emails, onlyThisEmail);
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
            collectionType = string.Empty;
            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    this.AddInterest = AddInterest;
                    var debtorPayments = dgDebtorTranOpenGrid.GetVisibleRows() as IEnumerable<DebtorTransPayment>;
                    if (AddInterest)
                    {
                        if (cwwin.value != 0)
                        {
                            var desm = api.CompanyEntity.HasDecimals ? 2 : 0;
                            foreach (var rec in debtorPayments)
                                rec.FeeAmount = !rec._OnHold ? Math.Round((rec._AmountOpen * cwwin.value) / 100d, desm) : 0d;
                        }
                        collectionType = Uniconta.ClientTools.Localization.lookup("InterestNote");
                    }
                    else
                    {
                        Currencies currencuEnum;
                        Enum.TryParse(cwwin.FeeCurrency, out currencuEnum);

                        if (cwwin.SelectedType == Uniconta.ClientTools.Localization.lookup("Transaction"))
                            SetFeeAmount(debtorPayments, cwwin.value, currencuEnum, false);
                        else if (cwwin.SelectedType == Uniconta.ClientTools.Localization.lookup("Account"))
                        {
                            debtorPayments.ToList().ForEach(p => p.FeeAmount = 0.0d);
                            var selectedAccounts = debtorPayments.GroupBy(x => x.Account).Select(x => x.First());
                            SetFeeAmount(selectedAccounts, cwwin.value, currencuEnum, true);
                        }
                        collectionType = cwwin.CollectionType;
                    }

                }
            };
            cwwin.Show();
        }

        private void SetFeeAmount(IEnumerable<DebtorTransPayment> debtorPayments, double feeAmount, Currencies feeCurrency, bool skipOnHoldAccounts)
        {
            foreach (var rec in debtorPayments)
            {
                if (rec.OnHold && !skipOnHoldAccounts)
                    continue;

                var recCurrency = rec.Currency.ToString();
                if (rec.Currency == feeCurrency || (!rec.Currency.HasValue && feeCurrency == Currencies.XXX))
                    rec.FeeAmount = feeAmount;
            }
        }

        private void btnSerach_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (txtDateFrm.Text == string.Empty)
                fromDate = DateTime.MinValue;
            else
                fromDate = txtDateFrm.DateTime.Date;

            if (txtDateTo.Text == string.Empty)
                toDate = DateTime.MinValue;
            else
                toDate = txtDateTo.DateTime.Date;

            Filter();
        }

        void LoadDataForReport()
        {
            statementList.Clear();
            var visibleRows = dgDebtorTranOpenGrid.GetVisibleRows() as IEnumerable<DebtorTransPayment>;
            if (visibleRows.Count() > 0)
            {
                string currentItem = null;
                DebtorPaymentStatementList masterDbPymtStatement = null;
                List<DebtorTransPayment> dbTransClientChildList = null;
                double SumAmount = 0d, SumAmountCur = 0d, CollectionAmount = 0d, SumFee = 0d;

                var listOpenTrans = visibleRows.OrderBy(p => p.Account);
                foreach (var trans in listOpenTrans)
                {
                    if (trans.Account != currentItem)
                    {
                        currentItem = trans.Account;
                        var dbt = (Debtor)accountCache.Get(currentItem);
                        masterDbPymtStatement = new DebtorPaymentStatementList();
                        masterDbPymtStatement.AccountNumber = dbt._Account;
                        masterDbPymtStatement.Name = dbt._Name;
                        dbTransClientChildList = new List<DebtorTransPayment>();
                        masterDbPymtStatement.ChildRecords = dbTransClientChildList;
                        statementList.Add(masterDbPymtStatement);
                        SumAmount = SumAmountCur = CollectionAmount = SumFee = 0d;
                    }

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

                    dbTransClientChildList.Insert(0, trans);
                }
            }
        }
    }
}


