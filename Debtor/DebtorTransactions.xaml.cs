using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Threading.Tasks;
using Uniconta.DataModel;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.System;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.Client.Pages;
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GridDebtorTransaction : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorTransClient); } }
    }
    public partial class DebtorTransactions : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorTransactions; } }
        DateTime filterDate;

        protected override Filter[] DefaultFilters()
        {
            if (filterDate != DateTime.MinValue)
            {
                Filter dateFilter = new Filter() { name = "Date", value = String.Format("{0:d}..", filterDate) };
                return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }

        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties dateSort = new SortingProperties("Date") { Ascending = false };
            SortingProperties VoucherSort = new SortingProperties("Voucher");
            return new SortingProperties[] { dateSort, VoucherSort };
        }

        public DebtorTransactions(UnicontaBaseEntity master)
            : base(master)
        {
            Initialize(master);
        }

        public DebtorTransactions(BaseAPI API) : base(API, string.Empty)
        {
            Initialize(null);
        }
        public DebtorTransactions(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            Initialize(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDebtorTran.UpdateMaster(args);
            SetHeader();
            FilterGrid(gridControl, false, false);
        }
        void SetHeader()
        {
            var masterClient = dgDebtorTran.masterRecord as Debtor;
            if (masterClient != null)
                SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("DebtorTransactions"), "/", masterClient._Account));
        }

        UnicontaBaseEntity master;
        private void Initialize(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            master = _master;
            dgDebtorTran.UpdateMaster(master);
            dgDebtorTran.api = api;
            var Comp = api.CompanyEntity;
            filterDate = BasePage.GetFilterDate(Comp, master != null);
            SetRibbonControl(localMenu, dgDebtorTran);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorTran.BusyIndicator = busyIndicator;
            dgDebtorTran.ShowTotalSummary();
            if (Comp.RoundTo100)
                Amount.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        public override Task InitQuery()
        {
            return FilterGrid(gridControl, master == null, false);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgDebtorTran.masterRecords == null);
            colAccount.Visible = showFields;
            colName.Visible = showFields;
            Open.Visible = !showFields;
            if (!api.CompanyEntity.Project)
                Project.Visible = false;
            Text.ReadOnly = Invoice.ReadOnly = PostType.ReadOnly = TransType.ReadOnly = showFields;
            if (showFields)
            {
                Open.Visible = false;
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "SaveGrid");
            }
            dgDebtorTran.Readonly = showFields;
            FromCreditor.Visible = ((master as Uniconta.DataModel.Debtor)?._D2CAccount != null);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            DebtorTransClient selectedItem = dgDebtorTran.SelectedItem as DebtorTransClient;
            switch (ActionType)
            {
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        ShowVoucher(dgDebtorTran.syncEntity, api, busyIndicator);
                    break;
                case "Settlements":
                    if (selectedItem != null)
                    {
                        string header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("Settlements"), selectedItem._Voucher);
                        AddDockItem(TabControls.DebtorSettlements, dgDebtorTran.syncEntity, true, header, null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgDebtorTran.syncEntity, vheader);
                    }
                    break;
                case "InvoiceLine":
                    if (selectedItem != null)
                        ShowInvoiceLines(selectedItem);
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "RefreshGrid":
                    FilterGrid(gridControl, master == null, false);
                    break;
                case "Invoices":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), selectedItem._Account);
                        AddDockItem(TabControls.Invoices, dgDebtorTran.syncEntity, header);
                    }
                    break;
                case "CollectionLetterLog":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CollectionLetterLog"), selectedItem.InvoiceAN);
                        AddDockItem(TabControls.DebtorTransCollectPage, dgDebtorTran.syncEntity, header);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(DebtorTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        async private void Save()
        {
            SetBusy();
            dgDebtorTran.BusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");
            var err = await dgDebtorTran.SaveData();
            ClearBusy();
        }

        async void ShowInvoiceLines(DebtorTransClient debTrans)
        {
            var debInvoice = await api.Query<DebtorInvoiceClient>(new UnicontaBaseEntity[] { debTrans }, null);
            if (debInvoice != null && debInvoice.Length > 0)
            {
                var debInv = debInvoice[0];
                AddDockItem(TabControls.DebtorInvoiceLines, debInv, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), debInv.InvoiceNum));
            }
        }

        public async static void ShowVoucher(SynchronizeEntity selectedRow, CrudAPI crudapi, BusyIndicator voucherBusyIndicator)
        {
            var Row = selectedRow.Row;
            if (Row == null)
                return;

            try
            {
                var post = UtilDisplay.CheckPostType(Row);
                if (post.postType == 0)
                    return;

                voucherBusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                if (post.documentId != 0)
                {
                    voucherBusyIndicator.IsBusy = true;
                    ShowInvoiceVoucher(TabControls.VouchersPage3, selectedRow);
                }
                else if (post.postType == (byte)DCPostType.InterestFee)
                {
                    IPrintReport printReport = null;
                    var debtorCache = crudapi.GetCache(typeof(Debtor)) ?? await crudapi.LoadCache(typeof(Debtor));
                    var reportName = Uniconta.ClientTools.Localization.lookup("InterestNote");

                    var debtor = debtorCache.Get(post.debtorTransopen.Trans._Account) as DebtorClient;
                    var showOnlySelectedRec = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("Message")),
                       Uniconta.ClientTools.Localization.lookup("AllTransactions"), System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes ? false : true;
                    printReport = await Utility.GenerateStandardCollectionReport(post.debtorTransopen, debtor, post.debtorTransopen.Trans._Date, DebtorEmailType.InterestNote, crudapi, showOnlySelectedRec);
                    DockInvoiceVoucher(UnicontaTabs.StandardPrintReportPage, new object[] { new IPrintReport[] { printReport }, reportName },
                        string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), reportName));

                    return;
                }
                else if (post.postType == (byte)DCPostType.CollectionLetter || post.postType == (byte)DCPostType.Collection)
                {
                    IPrintReport printReport = null;
                    var debtorCache = crudapi.GetCache(typeof(Debtor)) ?? await crudapi.LoadCache(typeof(Debtor));
                    var reportName = Uniconta.ClientTools.Localization.lookup("CollectionLetter");

                    var debtor = debtorCache.Get(post.debtorTransopen.Trans._Account) as DebtorClient;
                    var showOnlySelectedRec = UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("AllTransactions")), 
                        Uniconta.ClientTools.Localization.lookup("Message"), System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes ? false : true;
                    printReport = await Utility.GenerateStandardCollectionReport(post.debtorTransopen, debtor, post.debtorTransopen.Trans._Date, DebtorEmailType.Collection, crudapi, showOnlySelectedRec);
                    DockInvoiceVoucher(UnicontaTabs.StandardPrintReportPage, new object[] { new IPrintReport[] { printReport }, reportName },
                        string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), reportName));
                    return;
                }
                else if (post.postType != (byte)DCPostType.Payment)
                {
                    if (post.invoiceNumber != 0)
                    {
                        string reportName;
                        voucherBusyIndicator.IsBusy = true;
                        if (post.dcType != 2)
                        {
                            var invoicePdf = await new InvoiceAPI(crudapi).GetDebtorInvoicePdf(null, DateTime.MinValue, (int)post.invoiceNumber, false);
                            if (invoicePdf != null)
                            {
                                ViewDocument(invoicePdf, FileextensionsTypes.PDF, string.Concat(Uniconta.ClientTools.Localization.lookup("Invoice"), ": ", NumberConvert.ToString(post.invoiceNumber)), selectedRow);
                                return;
                            }
                            var arr = await crudapi.Query<DebtorInvoiceClient>(Row);
                            if (arr != null && arr.Length > 0)
                            {
                                var inv = arr[0];
                                var standardPrintReport = await StandardPrint(inv, crudapi);
                                reportName = await Utilities.Utility.GetLocalizedReportName(crudapi, inv, CompanyLayoutType.Invoice);
                                StandardPrint(standardPrintReport, inv._InvoiceNumber, reportName);
                                return;
                            }
                        }
                        if (post.dcType != 1)
                        {
                            var arr = await crudapi.Query<CreditorInvoiceClient>(Row);
                            if (arr != null && arr.Length > 0)
                            {
                                var inv = arr[0];
                                var standardPrintReport = await StandardPrint(inv, crudapi);
                                reportName = await Utilities.Utility.GetLocalizedReportName(crudapi, inv, CompanyLayoutType.PurchaseInvoice);
                                StandardPrint(standardPrintReport, inv._InvoiceNumber, reportName);
                                return;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                crudapi.ReportException(ex, string.Format("ShowVoucher, CompanyId={0}", crudapi.CompanyId));
            }
            finally
            {
                voucherBusyIndicator.IsBusy = false;
            }
        }

        public static void StandardPrint(IPrintReport standardPrintReport, long invoiceNumber)
        {
            StandardPrint(standardPrintReport, invoiceNumber, null);
        }

        public static void StandardPrint(IPrintReport standardPrintReport, long invoiceNumber, string formatReportName)
        {
            if (standardPrintReport?.Report != null)
            {
                var reportName = formatReportName ?? string.Format("{0}_{1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invoiceNumber);
                var dockName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invoiceNumber);
                DockInvoiceVoucher(UnicontaTabs.StandardPrintReportPage, new object[] { new IPrintReport[] { standardPrintReport }, reportName }, dockName);
            }
        }

        public static async Task<IPrintReport> StandardPrint(DebtorInvoiceClient debtorInvoice, CrudAPI crudapi)
        {
            var debtorInvoicePrint = new UnicontaClient.Pages.DebtorInvoicePrintReport(debtorInvoice, crudapi);
            var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardDebtorInvoice = new DebtorInvoiceReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DebtorOrder,
                    debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, isCreditNote: debtorInvoicePrint.IsCreditNote, messageClient: debtorInvoicePrint.MessageClient);

                var iprintReport = new StandardPrintReport(crudapi, new[] { standardDebtorInvoice }, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Invoice) { UseReportCache = true };
                await iprintReport.InitializePrint();
                if (iprintReport.Report != null)
                    return iprintReport;

                //Call LayoutInvoice
                var layoutReport = new LayoutPrintReport(crudapi, debtorInvoice);
                layoutReport.SetupLayoutPrintFields(debtorInvoicePrint);
                await layoutReport.InitializePrint();
                return layoutReport;
            }
            return null;
        }

        private static async Task<IPrintReport> StandardPrint(CreditorInvoiceClient creditorInvoice, CrudAPI crudApi)
        {
            var creditorInvoicePrint = new UnicontaClient.Pages.CreditorPrintReport(creditorInvoice, crudApi);
            var isInitializedSuccess = await creditorInvoicePrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardCreditorInvoice = new CreditorStandardReportClient(creditorInvoicePrint.Company, creditorInvoicePrint.Creditor, creditorInvoicePrint.CreditorInvoice, creditorInvoicePrint.InvTransInvoiceLines, creditorInvoicePrint.CreditorOrder,
                    creditorInvoicePrint.CompanyLogo, creditorInvoicePrint.ReportName, (int)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchaseInvoice, creditorInvoicePrint.CreditorMessage, creditorInvoicePrint.IsCreditNote);

                var iprintReport = new StandardPrintReport(crudApi, new[] { standardCreditorInvoice }, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchaseInvoice) { UseReportCache = true };
                await iprintReport.InitializePrint();
                if (iprintReport.Report != null)
                    return iprintReport;

                var layoutReport = new LayoutPrintReport(crudApi, creditorInvoice);
                await layoutReport.InitializePrint();
                return layoutReport;
            }
            return null;
        }
    }
}
