using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Controls;
using DevExpress.XtraReports.UI;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Uniconta.Client.Pages;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using Uniconta.Common.Utility;


#if !SILVERLIGHT
using ubl_norway_uniconta;
using FromXSDFile.OIOUBL.ExportImport;
using Microsoft.Win32;
using UBL.Iceland;
using UnicontaClient.Pages;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvoicesGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorInvoiceClient); } }
        public override IComparer GridSorting { get { return new DCInvoiceSort(); } }

        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            if (api.CompanyEntity.DeliveryAddress && Arr != null)
            {
                var cache = api.GetCache(typeof(Debtor));
                if (cache != null)
                {
                    foreach (var rec in (IEnumerable<DCInvoice>)Arr)
                        DebtorOrders.SetDeliveryAdress(rec, (DCAccount)cache.Get(rec._DCAccount), api);
                }
            }
        }
    }

    public partial class Invoices : GridBasePage
    {
        SQLCache Debcache, LayoutGroupCache;
        public override string NameOfControl { get { return TabControls.Invoices.ToString(); } }

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

        public Invoices(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }
        public Invoices(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvoicesGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string header;
            var masterClient1 = dgInvoicesGrid.masterRecord as Debtor;
            if (masterClient1 != null)
                header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), masterClient1._Account);
            else
            {
                var masterClient2 = dgInvoicesGrid.masterRecord as Uniconta.DataModel.Project;
                if (masterClient2 != null)
                    header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("ProjectInvoice"), masterClient2._DCAccount);
                else
                {
                    var masterClient3 = dgInvoicesGrid.masterRecord as Uniconta.DataModel.DebtorTrans;
                    if (masterClient3 != null)
                        header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), masterClient3._Account);
                    else
                    {
                        var masterClient4 = dgInvoicesGrid.masterRecord as DebtorTransOpenClient;
                        if (masterClient4 != null)
                            header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"), masterClient4.Account);
                        else
                            return;
                    }
                }
            }
            SetHeader(header);
        }

        public Invoices(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            dgInvoicesGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgInvoicesGrid);
            dgInvoicesGrid.BusyIndicator = busyIndicator;
            dgInvoicesGrid.api = api;
            var Comp = api.CompanyEntity;
            if (master == null)
                filterDate = BasePage.GetFilterDate(Comp, false);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            initialLoad();
            dgInvoicesGrid.ShowTotalSummary();
            if (Comp.RoundTo100)
                CostValue.HasDecimals = NetAmount.HasDecimals = TotalAmount.HasDecimals = Margin.HasDecimals = SalesValue.HasDecimals = false;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (!Comp.Order && !Comp.Purchase)
                UtilDisplay.RemoveMenuCommand(rb, "CreateOrder");
#if SILVERLIGHT
            UtilDisplay.RemoveMenuCommand(rb, "GenerateOioXml");
#endif
            dgInvoicesGrid.CustomSummary += dgInvoicesGrid_CustomSummary;
        }

#if !SILVERLIGHT
        public override bool IsDataChaged { get { return IsGeneratingDocument; } }
#endif

        double sumMargin, sumSales, sumMarginRatio;
        private void dgInvoicesGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as DebtorInvoiceClient;
                    sumSales += row.SalesValue;
                    sumMargin += row.Margin;
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        sumMarginRatio = 100 * sumMargin / sumSales;
                        e.TotalValue = sumMarginRatio;
                    }
                    break;
            }
        }
        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgInvoicesGrid.masterRecords == null);
            Account.Visible = showFields;
            Name.Visible = showFields;
            setDim();
            if (!api.CompanyEntity.DeliveryAddress)
            {
                DeliveryName.Visible = false;
                DeliveryAddress1.Visible = false;
                DeliveryAddress2.Visible = false;
                DeliveryAddress3.Visible = false;
                DeliveryZipCode.Visible = false;
                DeliveryCity.Visible = false;
                DeliveryCountry.Visible = false;
            }
        }

        async void initialLoad()
        {
            Debcache = api.GetCache(typeof(Debtor)) ?? await api.LoadCache(typeof(Debtor));
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;

            var lst = new List<Type>(12) { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.Employee) };
            if (Comp.ItemVariants)
            {
                lst.Add(typeof(Uniconta.DataModel.InvVariant1));
                lst.Add(typeof(Uniconta.DataModel.InvVariant2));
                var n = Comp.NumberOfVariants;
                if (n >= 3)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant3));
                if (n >= 4)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant4));
                if (n >= 5)
                    lst.Add(typeof(Uniconta.DataModel.InvVariant5));
            }
            if (Comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
            if (Comp.Shipments)
            {
                lst.Add(typeof(Uniconta.DataModel.ShipmentType));
                lst.Add(typeof(Uniconta.DataModel.DeliveryTerm));
            }
            if (Comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));

            LoadType(lst);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            IEnumerable<DebtorInvoiceClient> selectedInvoiceUBL;
            var selectedItem = dgInvoicesGrid.SelectedItem as DebtorInvoiceClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.InvoicePage2, EditParam, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoices"), selectedItem.Name));
                    break;
                case "InvoiceLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorInvoiceLines, dgInvoicesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), selectedItem.InvoiceNum));
                    break;
                case "ShowInvoice":
                case "ShowPackNote":
                    if (dgInvoicesGrid.SelectedItem == null || dgInvoicesGrid.SelectedItems == null)
                        return;
                    var selectedItems = dgInvoicesGrid.SelectedItems.Cast<DebtorInvoiceClient>();
                    if (ActionType == "ShowInvoice")
                        ShowDocument(selectedItems, true);
                    else
                        ShowDocument(selectedItems, false);
                    break;
                case "Trans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PostedTransactions, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem.InvoiceNum));
                    break;
                case "SendInvoice":
                    if (dgInvoicesGrid.SelectedItem == null || dgInvoicesGrid.SelectedItems == null)
                        return;
                    var selectedInvoiceEmails = dgInvoicesGrid.SelectedItems.Cast<DebtorInvoiceClient>();
                    SendInvoice(selectedInvoiceEmails);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.InvoiceNum));
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                    {
                        CWOrderFromInvoice cwOrderInvoice = new CWOrderFromInvoice(api);
#if !SILVERLIGHT
                        cwOrderInvoice.DialogTableId = 2000000032;
#endif
                        cwOrderInvoice.Closed += async delegate
                        {
                            if (cwOrderInvoice.DialogResult == true)
                            {
                                var orderApi = new OrderAPI(api);
                                var inversign = cwOrderInvoice.InverSign;
                                var account = cwOrderInvoice.Account;
                                var copyDelAddress = cwOrderInvoice.copyDeliveryAddress;
                                var reCalPrices = cwOrderInvoice.reCalculatePrices;
                                Type t;
                                if (cwOrderInvoice.Offer == true)
                                    t = typeof(DebtorOfferClient);
                                else
                                    t = typeof(DebtorOrderClient);
                                var dcOrder = this.CreateGridObject(t) as DCOrder;
                                dcOrder._DeliveryDate = cwOrderInvoice.DeliveryDate;
                                var result = await orderApi.CreateOrderFromInvoice(selectedItem, dcOrder, account, inversign, CopyDeliveryAddress: copyDelAddress, RecalculatePrices: reCalPrices);
                                if (result != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(result);
                                else
                                    ShowOrderLines(dcOrder);
                            }
                        };
                        cwOrderInvoice.Show();
                    }
                    break;

#if !SILVERLIGHT
                case "GenerateOioXml":
                    if (selectedItem != null)
                        GenerateOIOXmlForAll(new[] { selectedItem });
                    break;
                case "GenerateMarkedOioXml":
                    selectedInvoiceUBL = dgInvoicesGrid.SelectedItems?.Cast<DebtorInvoiceClient>();
                    if (selectedInvoiceUBL != null)
                        GenerateOIOXmlForAll(selectedInvoiceUBL, true);
                    break;
                case "GenerateAllOioXml":
                    GenerateOIOXmlForAll(dgInvoicesGrid.GetVisibleRows() as IEnumerable<DebtorInvoiceClient>);
                    break;
                case "SendUBL":
                    selectedInvoiceUBL = dgInvoicesGrid.SelectedItems?.Cast<DebtorInvoiceClient>();
                    if (selectedInvoiceUBL != null)
                        SendUBL(selectedInvoiceUBL);
                    break;
                case "SendAsOutlook":
                    if (selectedItem != null)
                        OpenOutLook(selectedItem);
                    break;
#endif
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "PreviousAddresses":
                    if (selectedItem?.Debtor != null)
                        AddDockItem(TabControls.DCPreviousAddressPage, selectedItem.Debtor, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PreviousAddresses"), selectedItem.Debtor._Name));
                    break;
#if !SILVERLIGHT
                case "DocSendLog":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DocsSendLogGridPage, dgInvoicesGrid.syncEntity);
                    break;
#endif
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(DebtorInvoiceClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

#if !SILVERLIGHT
        StandardPrintReportPage standardViewerPageForDocument;
        bool IsGeneratingDocument;
        DCPreviousAddressClient[] previousAddressLookup;
        DebtorMessagesClient[] messagesLookup;
        bool hasLookups;
#endif
        async private void ShowDocument(IEnumerable<DebtorInvoiceClient> debtorInvoices, bool isInvoice)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                var docList = debtorInvoices.ToList();
#if !SILVERLIGHT
                var failedPrints = new List<long>();
                var count = docList.Count;
                string dockName = null, reportName = null;
                bool exportAsPdf = false;
                System.Windows.Forms.FolderBrowserDialog folderDialogSaveInvoice = null;
                hasLookups = false;
                if (count > 1)
                {
                    hasLookups = true;
                    if (count > StandardPrintReportPage.MAX_PREVIEW_REPORT_LIMIT)
                    {
                        var confirmMsg = string.Format(Uniconta.ClientTools.Localization.lookup("PreivewRecordsExportMsg"), count);
                        if (UnicontaMessageBox.Show(confirmMsg, Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            exportAsPdf = true;
                    }
                    else if (isInvoice)
                    {
                        dockName = string.Concat(Uniconta.ClientTools.Localization.lookup("Preview"), ": ", Uniconta.ClientTools.Localization.lookup("Debtor"),
                            " ", Uniconta.ClientTools.Localization.lookup("Invoices"));
                        reportName = string.Concat(Uniconta.ClientTools.Localization.lookup("Debtor"), Uniconta.ClientTools.Localization.lookup("Invoices"));
                    }
                    else
                    {
                        dockName = string.Concat(Uniconta.ClientTools.Localization.lookup("Preview"), ": ", Uniconta.ClientTools.Localization.lookup("Packnote"));
                        reportName = Uniconta.ClientTools.Localization.lookup("Packnote");
                    }

                    ribbonControl.DisableButtons(new [] { "ShowInvoice", "ShowPackNote" });
                }

#elif SILVERLIGHT
                int top = 200, left = 300;
                int count = docList.Count;

                if (count > 1)
                {
#endif
                foreach (var debtInvoice in docList)
                {
#if !SILVERLIGHT
                    IsGeneratingDocument = true;
                    IPrintReport printReport = isInvoice ? await PrintInvoice(debtInvoice) : await PrintPackNote(debtInvoice);
                    if (printReport?.Report != null)
                    {
                        var docNumber = isInvoice ? debtInvoice._InvoiceNumber : debtInvoice._PackNote;
                        if (count > 1 && IsGeneratingDocument)
                        {
                            if (exportAsPdf)
                            {
                                string docName = isInvoice ? Uniconta.ClientTools.Localization.lookup("Invoice") : Uniconta.ClientTools.Localization.lookup("Packnote");
                                string directoryPath = string.Empty;
                                if (folderDialogSaveInvoice == null)
                                {
                                    folderDialogSaveInvoice = UtilDisplay.LoadFolderBrowserDialog;
                                    var dialogResult = folderDialogSaveInvoice.ShowDialog();
                                    if (dialogResult == System.Windows.Forms.DialogResult.OK || dialogResult == System.Windows.Forms.DialogResult.Yes)
                                        directoryPath = folderDialogSaveInvoice.SelectedPath;
                                }
                                else
                                    directoryPath = folderDialogSaveInvoice.SelectedPath;

                                Utility.ExportReportAsPdf(printReport.Report, directoryPath, docName, NumberConvert.ToString(docNumber));
                            }
                            else
                            {
                                if (standardViewerPageForDocument == null)
                                    standardViewerPageForDocument = dockCtrl.AddDockItem(api?.CompanyEntity, TabControls.StandardPrintReportPage, ParentControl, new object[] { printReport, reportName }, dockName) as StandardPrintReportPage;
                                else
                                    standardViewerPageForDocument.InsertToMasterReport(printReport.Report);
                            }
                        }
                        else
                        {
                            var docType = isInvoice ? CompanyLayoutType.Invoice : CompanyLayoutType.Packnote;
                            reportName = await Utility.GetLocalizedReportName(api, debtInvoice, docType);
                            dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", isInvoice ? Uniconta.ClientTools.Localization.lookup("Invoice") :
                                Uniconta.ClientTools.Localization.lookup("Packnote"), NumberConvert.ToString(docNumber)));

                            AddDockItem(TabControls.StandardPrintReportPage, new object[] { new List<IPrintReport> { printReport }, reportName }, dockName);
                            break;
                        }
                    }
                    else
                        failedPrints.Add(debtInvoice.InvoiceNumber);
                }

                IsGeneratingDocument = false;
                ribbonControl?.EnableButtons(new [] { "ShowInvoice", "ShowPackNote" });

                if (failedPrints.Count > 0)
                {
                    var failedList = string.Join(",", failedPrints);
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                }
#elif SILVERLIGHT
                        DefaultPrint(debtInvoice, true, new Point(top, left));
                        left = left - left / count;
                        top = top - top / count;
                    }
                }
                else
                    DefaultPrint(debtorInvoices.First());
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("Invoices.ShowDocument(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
        }

#if !SILVERLIGHT

        async private void OpenOutLook(DebtorInvoiceClient invClient)
        {
            try
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
                var debtor = invClient.Debtor;
                var invoiceReport = await PrintInvoice(invClient);
                var documentType = invClient._LineTotal >= -0.0001d ? CompanyLayoutType.Invoice : CompanyLayoutType.Creditnote;
                invClient._SendTime = DateTime.MinValue; // this will for outlook function to update server
                InvoicePostingPrintGenerator.OpenReportInOutlook(api, invoiceReport, invClient, debtor, documentType);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
            finally { busyIndicator.IsBusy = false; }
        }

        private async Task<IPrintReport> PrintInvoice(DebtorInvoiceClient debtorInvoice)
        {
            var debtorInvoicePrint = new DebtorInvoicePrintReport(debtorInvoice, api);

            //In case of Multple invoices we create a lookup
            if (hasLookups)
                await FillLookUps(debtorInvoicePrint);

            var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardDebtorInvoice = new DebtorInvoiceReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DebtorOrder,
                    debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, isCreditNote: debtorInvoicePrint.IsCreditNote, messageClient: debtorInvoicePrint.MessageClient);

                var standardReports = new[] { standardDebtorInvoice };
                IPrintReport iprintReport = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Invoice);
                await iprintReport.InitializePrint();
                if (iprintReport.Report != null)
                    return iprintReport;

                //Call LayoutInvoice
                var layoutPrint = new LayoutPrintReport(api, debtorInvoice, debtorInvoicePrint.IsCreditNote ?
                    CompanyLayoutType.Creditnote : CompanyLayoutType.Invoice);
                layoutPrint.SetupLayoutPrintFields(debtorInvoicePrint);

                //In case of Multple invoices we create lookups
                if (hasLookups)
                    FillLookUps(layoutPrint);

                await layoutPrint.InitializePrint();
                return layoutPrint;
            }
            return null;
        }

        private async Task<IPrintReport> PrintPackNote(DebtorInvoiceClient debtorInvoice)
        {
            var packnote = Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote;
            var debtorInvoicePrint = new DebtorInvoicePrintReport(debtorInvoice, api, CompanyLayoutType.Packnote);

            //In case of Multple invoices we create lookups
            if (hasLookups)
                await FillLookUps(debtorInvoicePrint);

            var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardDebtorInvoice = new DebtorQCPReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice,
                    debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DebtorOrder, debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName,
                    (int)packnote, messageClient: debtorInvoicePrint.MessageClient);

                var standardReports = new[] { standardDebtorInvoice };
                IPrintReport iprintReport = new StandardPrintReport(api, standardReports, (byte)packnote);
                await iprintReport.InitializePrint();
                if (iprintReport.Report != null)
                    return iprintReport;

                //Call Layout Invoice
                var layoutPrint = new LayoutPrintReport(api, debtorInvoice, CompanyLayoutType.Packnote);
                layoutPrint.SetupLayoutPrintFields(debtorInvoicePrint);

                //In case of Multple invoices we create lookups
                if (hasLookups)
                    FillLookUps(layoutPrint);

                await layoutPrint.InitializePrint();
                return layoutPrint;
            }
            return null;
        }

        async private Task FillLookUps(DebtorInvoicePrintReport debtorInvoicePrint)
        {
            if (previousAddressLookup == null)
                previousAddressLookup = await api.Query<DCPreviousAddressClient>();

            debtorInvoicePrint.SetLookUpForPreviousAddressClients(previousAddressLookup);

            if (messagesLookup == null)
                messagesLookup = await api.Query<DebtorMessagesClient>();

            debtorInvoicePrint.SetLookUpForDebtorMessageClients(messagesLookup);
        }

        private void FillLookUps(LayoutPrintReport layoutPrint)
        {
            layoutPrint.SetLookUpForDebtorMessageClients(messagesLookup);
            layoutPrint.SetLookUpForPreviousAddressClients(previousAddressLookup);
        }

        private async void GenerateOIOXmlForAll(IEnumerable<DebtorInvoiceClient> lst, bool selectedrows = false)
        {
            IEnumerable<DebtorInvoiceClient> lstInvClient = (lst is Array) ? lst : lst.ToList();
            var api = this.api;
            var Comp = api.CompanyEntity;

            var InvCache = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem));
            var VatCache = api.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVat));
            LayoutGroupCache = api.GetCache(typeof(DebtorLayoutGroup)) ?? await api.LoadCache(typeof(DebtorLayoutGroup));
            SQLCache workInstallCache = null;
            SQLCache Contacts = null;
            SystemInfo.Visible = true;

            var applFilePath = string.Empty;
            var listOfXmlPath = new List<string>();
            int countErr = 0;
            bool hasUserFolder = false;
            SaveFileDialog saveDialog = null;
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = null;
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var cnt = lstInvClient.Count();
            foreach (var invClient in lstInvClient)
            {
                var debtor = (Debtor)Debcache.Get(invClient._DCAccount);
                if (debtor == null)
                    continue;

                if (!selectedrows && cnt > 1 && (!debtor._InvoiceInXML || invClient._SendTimeOIO != DateTime.MinValue))
                    continue;

                var InvTransInvoiceLines = (DebtorInvoiceLines[])await Invapi.GetInvoiceLines(invClient, new DebtorInvoiceLines());

                Contact contactPerson = null;
                if (invClient._ContactRef != 0)
                {
                    if (Contacts == null)
                        Contacts = api.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact));
                    foreach (var contact in (Uniconta.DataModel.Contact[])Contacts.GetRecords)
                        if (contact.RowId == invClient._ContactRef)
                        {
                            contactPerson = contact;
                            break;
                        }
                }

                DebtorOrders.SetDeliveryAdress(invClient, debtor, api);

                Debtor deliveryAccount;
                if (invClient._DeliveryAccount != null)
                    deliveryAccount = (Debtor)Debcache.Get(invClient._DeliveryAccount);
                else
                    deliveryAccount = null;

                WorkInstallation workInstallation = null;
                if (invClient._Installation != null)
                {
                    if (workInstallCache == null)
                        workInstallCache = api.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await api.LoadCache(typeof(Uniconta.DataModel.WorkInstallation));

                    workInstallation = (WorkInstallation)workInstallCache.Get(invClient._Installation);
                }

                CreationResult result;

                if (Comp._CountryId == CountryCode.Norway || Comp._CountryId == CountryCode.Netherlands)
                    result = EHF.GenerateEHFXML(Comp, debtor, deliveryAccount, invClient, InvTransInvoiceLines, InvCache, VatCache, null, contactPerson);
                else if (Comp._CountryId == CountryCode.Iceland)
                {
                    var paymFormatCache = api.GetCache(typeof(DebtorPaymentFormatClientIceland)) ?? await api.LoadCache(typeof(DebtorPaymentFormatClientIceland));
                    TableAddOnData[] attachments = await UBL.Iceland.Attachments.CollectInvoiceAttachments(invClient, api);

                    result = TS136137.GenerateTS136137XML(Comp, debtor, deliveryAccount, invClient, InvTransInvoiceLines, InvCache, VatCache, null, contactPerson, paymFormatCache, attachments);
                }
                else
                {
                    var attachments = await FromXSDFile.OIOUBL.ExportImport.Attachments.CollectInvoiceAttachments(invClient, api);
                    result = Uniconta.API.DebtorCreditor.OIOUBL.GenerateOioXML(Comp, debtor, deliveryAccount, invClient, InvTransInvoiceLines, InvCache, VatCache, null, contactPerson, attachments, LayoutGroupCache, workInstallation);
                }

                bool createXmlFile = true;

                if (result.HasErrors)
                {
                    countErr++;
                    createXmlFile = false;
                    invClient._SystemInfo = string.Empty;
                    foreach (FromXSDFile.OIOUBL.ExportImport.PrecheckError error in result.PrecheckErrors)
                    {
                        invClient._SystemInfo = invClient._SystemInfo + error.ToString() + "\n";
                    }
                }

                if (result.Document != null && createXmlFile)
                {
                    string invoice = Uniconta.ClientTools.Localization.lookup("Invoice");
                    string filename = null;

                    if (session.User._AppDocPath != string.Empty && Directory.Exists(session.User._AppDocPath))
                    {
                        try
                        {
                            applFilePath = string.Concat(session.User._AppDocPath, "\\OIOUBL");
                            Directory.CreateDirectory(applFilePath);

                            filename = string.Format("{0}\\{1}_{2}.xml", applFilePath, invoice, invClient.InvoiceNumber);
                            hasUserFolder = true;
                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex);
                        }
                    }
                    else
                    {
                        if (saveDialog == null && cnt == 1)
                        {
                            saveDialog = UtilDisplay.LoadSaveFileDialog;
                            saveDialog.FileName = string.Concat(invoice, "_", NumberConvert.ToString(invClient.InvoiceNumber));
                            saveDialog.Filter = "XML-File | *.xml";
                            bool? dialogResult = saveDialog.ShowDialog();
                            if (dialogResult != true)
                                break;
                        }
                        else if (folderBrowserDialog == null)
                        {
                            folderBrowserDialog = UtilDisplay.LoadFolderBrowserDialog;
                            var dialogResult = folderBrowserDialog.ShowDialog();
                            if (dialogResult != System.Windows.Forms.DialogResult.OK)
                                break;
                        }

                        if (cnt > 1)
                        {
                            filename = folderBrowserDialog.SelectedPath;
                            filename = string.Format("{0}\\{1}_{2}.xml", filename, invoice, invClient.InvoiceNumber);
                        }
                        else
                        {
                            filename = saveDialog.FileName;
                        }
                    }

                    listOfXmlPath.Add(filename);

                    result.Document.Save(filename);
                    await Invapi.MarkSendInvoiceOIO(invClient);
                    invClient.SendTimeOIO = BasePage.GetSystemDefaultDate();
                    invClient._SystemInfo = "Ok";
                }

                invClient.NotifyPropertyChanged("SystemInfo");
            }

            if (countErr != 0)
            {
                UnicontaMessageBox.Show(string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceFileCreationFailMsg"), countErr), " ",
                    Uniconta.ClientTools.Localization.lookup("CheckSystemInfoColumnMsg")), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
            }
            else if (hasUserFolder)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SaveFileMsgOBJ"), listOfXmlPath.Count, listOfXmlPath.Count == 1 ?
                    Uniconta.ClientTools.Localization.lookup("Invoice") : Uniconta.ClientTools.Localization.lookup("Invoices"), applFilePath), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
            }
        }

#elif SILVERLIGHT
        private void DefaultPrint(DebtorInvoiceClient debtorInvoice, bool isPackNote = false)
        {
            object[] ob = new object[2];
            ob[0] = debtorInvoice;
            ob[1] = isPackNote ? CompanyLayoutType.Packnote : CompanyLayoutType.Invoice;
            string headerName = isPackNote ? "PackNote" : "Invoice";
            AddDockItem(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName), debtorInvoice.InvoiceNum));
        }

        private void DefaultPrint(DebtorInvoiceClient debtorInvoice, bool isFloat, Point position, bool isPackNote = false)
        {
            object[] ob = new object[2];
            ob[0] = debtorInvoice;
            ob[1] = isPackNote ? CompanyLayoutType.Packnote : CompanyLayoutType.Invoice;
            string headerName = isPackNote ? "PackNote" : "Invoice";
            AddDockItem(TabControls.ProformaInvoice, ob, isFloat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName), debtorInvoice.InvoiceNum), floatingLoc: position);
        }
#endif

        private void ShowOrderLines(DCOrder order)
        {
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup("SalesOrderCreated"), Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Orderline")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(order.__DCType() == 1 ? TabControls.DebtorOrderLines : TabControls.DebtorOfferLines, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, order._DCAccount));
                        break;

                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();

        }
        void SendInvoice(IEnumerable<DebtorInvoiceClient> invoiceEmails)
        {
            int icount = invoiceEmails.Count();
            UnicontaClient.Pages.CWSendInvoice cwSendInvoice = new UnicontaClient.Pages.CWSendInvoice();
#if !SILVERLIGHT
            cwSendInvoice.DialogTableId = 2000000028;
#endif
            cwSendInvoice.Closed += async delegate
             {
                 if (cwSendInvoice.DialogResult == true)
                 {
                     busyIndicator.IsBusy = true;
                     busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                     InvoiceAPI Invapi = new InvoiceAPI(api);
                     List<string> errors = new List<string>();

                     var sendInBackgroundOnly = CWSendInvoice.sendInBackgroundOnly;
                     foreach (var inv in invoiceEmails.ToList()) // we take a copy, since user could do a refresh
                     {
                         var errorCode = await Invapi.SendInvoice(inv, cwSendInvoice.Emails, cwSendInvoice.sendOnlyToThisEmail, sendInBackgroundOnly);
                         sendInBackgroundOnly = true;
                         if (errorCode != ErrorCodes.Succes)
                         {
                             var standardError = await api.session.GetErrors(errorCode);
                             var stformattedErr = UtilDisplay.GetFormattedErrorCode(errorCode, standardError);
                             var errorStr = string.Format("{0}({1}): \n{2}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), inv.InvoiceNum,
                                 Uniconta.ClientTools.Localization.lookup(stformattedErr));
                             errors.Add(errorStr);
                         }
                     }

                     busyIndicator.IsBusy = false;
                     if (errors.Count == 0)
                         UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), icount == 1 ? Uniconta.ClientTools.Localization.lookup("Invoice") :
                             Uniconta.ClientTools.Localization.lookup("Invoices")), Uniconta.ClientTools.Localization.lookup("Message"));
                     else
                     {
                         CWErrorBox errorDialog = new CWErrorBox(errors.ToArray(), true);
                         errorDialog.Show();
                     }
                 }
             };
            cwSendInvoice.Show();
        }

#if !SILVERLIGHT
        void SendUBL(IEnumerable<DebtorInvoiceClient> invoiceUBL)
        {
            int icount = invoiceUBL.Count();
            var cwSendUBL = new CWSendUBL(icount);
            cwSendUBL.DialogTableId = 2000000081;
            cwSendUBL.Closed += async delegate
            {
                if (cwSendUBL.DialogResult == true)
                {
                    SystemInfo.Visible = true;
                    busyIndicator.IsBusy = true;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    List<string> errors = new List<string>();

                    int cntInv = 0;
                    var sendInBackgroundOnly = false;
                    foreach (var inv in invoiceUBL.ToList())
                    {
                        cntInv++;
                        if (cntInv > 10)
                            sendInBackgroundOnly = true;
                        var errorCode = await Invapi.SendOIOUBLInvoice(inv, sendInBackgroundOnly);
                        if (errorCode != ErrorCodes.Succes)
                        {
                            var standardError = await api.session.GetErrors(errorCode);
                            var stformattedErr = UtilDisplay.GetFormattedErrorCode(errorCode, standardError);
                            var errorStr = string.Format("{0}({1}): \n{2}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), inv.InvoiceNum,
                                Uniconta.ClientTools.Localization.lookup(stformattedErr));
                            inv._SystemInfo = stformattedErr;

                            errors.Add(errorStr);
                        }
                    }

                    busyIndicator.IsBusy = false;
                    if (errors.Count == 0)
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), icount == 1 ? Uniconta.ClientTools.Localization.lookup("Invoice") :
                            Uniconta.ClientTools.Localization.lookup("Invoices")), Uniconta.ClientTools.Localization.lookup("Message"));
                    else
                    {
                        CWErrorBox errorDialog = new CWErrorBox(errors.ToArray(), true);
                        errorDialog.Show();
                    }
                }
            };
            cwSendUBL.Show();
        }
#endif

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InvoicePage2)
                dgInvoicesGrid.UpdateItemSource(argument);
            else if (screenName == TabControls.StandardPrintReportPage)
            {
#if !SILVERLIGHT
                IsGeneratingDocument = false;
                standardViewerPageForDocument = null;
#endif
            }
        }
    }
}
