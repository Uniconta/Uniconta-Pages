using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using Uniconta.API.DebtorCreditor;
using System.Windows;
using DevExpress.XtraReports.UI;
using Uniconta.API.System;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.Client.Pages;

#if !SILVERLIGHT
using UnicontaClient.Pages;
#endif

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
            if (masterClient == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorTransactions"), masterClient._Account);
            SetHeader(header);
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
                        string header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("Settlements"), selectedItem._Voucher);
                        AddDockItem(TabControls.DebtorSettlements, dgDebtorTran.syncEntity, true, header);
                    }
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
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

#if SILVERLIGHT

        private static void DefaultPrint(UnicontaBaseEntity dcInvoice)
        {
            var dc = dcInvoice as DCInvoice;

            object[] ob = new object[2];
            ob[0] = dcInvoice;
            ob[1] = CompanyLayoutType.Invoice;
            DockInvoiceVoucher(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), dc?.InvoiceNum));
        }
#endif
        public async static void ShowVoucher(SynchronizeEntity selectedRow, CrudAPI crudapi, BusyIndicator voucherBusyIndicator)
        {
            var Row = selectedRow.Row;
            if (Row == null)
                return;

            try
            {
                int documentId = 0;
                int postType = 0;
                long invoiceNumber = 0;
                byte dcType = 0;

                var debtorTrans = Row as DCTrans;
                if (debtorTrans != null)
                {
                    documentId = debtorTrans._DocumentRef;
                    postType = debtorTrans._PostType;
                    invoiceNumber = debtorTrans._Invoice;
                    dcType = debtorTrans.__DCType();
                }
                else
                {
                    var debtorTransopen = Row as DCTransOpen;
                    if (debtorTransopen != null)
                    {
                        documentId = debtorTransopen.Trans._DocumentRef;
                        postType = debtorTransopen.Trans._PostType;
                        invoiceNumber = debtorTransopen.Trans._Invoice;
                        dcType = debtorTransopen.__DCType();
                    }
                    else
                    {
                        var glTrans = Row as GLTrans;
                        if (glTrans != null)
                        {
                            documentId = glTrans._DocumentRef;
                            postType = (byte)DCPostType.Invoice;
                            invoiceNumber = glTrans._Invoice;
                            dcType = (byte)glTrans._DCType;
                        }
                        else
                        {
                            var projectTrans = Row as ProjectTrans;
                            if (projectTrans != null)
                            {
                                documentId = projectTrans._DocumentRef;
                                postType = (byte)DCPostType.Invoice;
                                invoiceNumber = projectTrans._Invoice;
                                if (projectTrans._CreditorAccount != null)
                                    dcType = 2;
                                else
                                    dcType = 1;
                            }
                            else
                            {
                                var iTrans = Row as InvTrans;
                                if (iTrans != null && (iTrans._InvoiceRowId != 0 || iTrans._InvoiceNumber != 0))
                                {
                                    postType = (byte)DCPostType.Invoice;
                                    dcType = iTrans._MovementType;
                                    invoiceNumber = iTrans._InvoiceNumber;
                                    if (invoiceNumber == 0)
                                        invoiceNumber = iTrans._InvoiceRowId;
                                }
                                else
                                    return;
                            }
                        }
                    }
                }
                voucherBusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                if (documentId != 0)
                {
                    voucherBusyIndicator.IsBusy = true;
                    ShowInvoiceVoucher(TabControls.VouchersPage3, selectedRow);
                }
                else
                {
                    if ((postType == (byte)DCPostType.Invoice || postType == (byte)DCPostType.Creditnote) && invoiceNumber != 0)
                    {
                        voucherBusyIndicator.IsBusy = true;
                        if (dcType == 1)
                        {
                            var debtorInvoice = await crudapi.Query<DebtorInvoiceClient>(Row);
                            var invoice = debtorInvoice?.FirstOrDefault();
                            if (invoice == null)
                                return;
#if !SILVERLIGHT
                            var debtInvNumber = invoice._InvoiceNumber;
                            var standardPrintReport = await StandardPrint(invoice, crudapi);
                            var reportName = await Utilities.Utility.GetLocalizedReportName(crudapi, invoice, CompanyLayoutType.Invoice);
                            StandardPrint(standardPrintReport, debtInvNumber, reportName);
#else
                            DefaultPrint(invoice);
#endif
                        }
                        else if (dcType == 2)
                        {
                            var creditorInvoice = await crudapi.Query<CreditorInvoiceClient>(Row);
                            var invoice = creditorInvoice?.FirstOrDefault();
                            if (invoice == null)
                                return;
#if !SILVERLIGHT
                            var credInvNumber = invoice._InvoiceNumber;
                            var iprintReport = await StandardPrint(invoice, crudapi);
                            var reportName = await Utilities.Utility.GetLocalizedReportName(crudapi, invoice, CompanyLayoutType.PurchaseInvoice);
                            StandardPrint(iprintReport, credInvNumber, reportName);
#else
                            DefaultPrint(invoice);
#endif
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

#if !SILVERLIGHT
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
#endif
    }
}
