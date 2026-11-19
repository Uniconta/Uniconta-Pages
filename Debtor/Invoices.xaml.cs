using DevExpress.Data;
using DevExpress.Xpf.Grid;
using Corasau.DebitorCreditor.Germany.EDelivery;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.Client.Pages;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvoicesGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorInvoiceClient); } }
        public override IComparer GridSorting { get { return new DCInvoiceSort(); } }

        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            if (Arr != null)
            {
                var Comp = api.CompanyEntity;
                if (Comp.DeliveryAddress)
                {
                    var cache = api.GetCache(typeof(Debtor))?? api.LoadCache(typeof(Debtor)).GetAwaiter().GetResult();
                    if (cache != null)
                    {
                        foreach (var rec in (IEnumerable<DCInvoice>)Arr)
                            UtilCommon.SetDeliveryAdress(rec, (DCAccount)cache.Get(rec._DCAccount), api);
                    }
                }
                if (Comp.DebtorInvoices != null)
                {
                    Comp.DebtorInvoices = new IRowIdCache<DebtorInvoiceClient>();
                    Comp.DebtorInvoices.SetCache((IEnumerable<DebtorInvoiceClient>)Arr);
                    Comp.DebtorInvoicesArr = null;
                }
            }
        }
    }

    public partial class Invoices : GridBasePage
    {
        SQLCache Debcache, LayoutGroupCache, eDeliveryMappingGroups;
        Dictionary<ItemBase, bool> eDocumentMenuItems;
        Debtor eDocumentMasterDebtor;
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
            eDocumentMasterDebtor = args as Debtor;
            UpdateEDocumentMenuVisibility(eDocumentMasterDebtor ?? GetDebtor(dgInvoicesGrid.SelectedItem as DebtorInvoiceClient));
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
                filterDate = BasePage.GetFilterDate(Comp, false, 2);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            initialLoad();
            dgInvoicesGrid.ShowTotalSummary();
            if (Comp.RoundTo100)
                CostValue.HasDecimals = NetAmount.HasDecimals = TotalAmount.HasDecimals = Margin.HasDecimals = SalesValue.HasDecimals = false;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            InitEDocumentMenu(rb, master);
#if MAC
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "ExportUBL", "ExportUBLNoMark" });
#endif
            if (api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.Admin)
                UtilDisplay.RemoveMenuCommand(rb, "ExportUBLNoMark");
            if (!Comp.Order && !Comp.Purchase)
                UtilDisplay.RemoveMenuCommand(rb, "CreateOrder");
            dgInvoicesGrid.CustomSummary += dgInvoicesGrid_CustomSummary;

            if (!Comp.Packnote)
                UtilDisplay.RemoveMenuCommand(rb, "ShowPackNote");
        }

        public override bool IsDataChaged { get { return IsGeneratingDocument; } }

        double sumCost, sumSales;
        private void dgInvoicesGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumCost = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as DebtorInvoiceClient;
                    sumSales += row._NetAmountCur;
                    sumCost += row._CostValue;
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                        e.TotalValue = 100d * (sumSales - sumCost) / sumSales;
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
            var Comp = api.CompanyEntity;
            if (!Comp.DeliveryAddress)
            {
                DeliveryName.Visible = false;
                DeliveryAddress1.Visible = false;
                DeliveryAddress2.Visible = false;
                DeliveryAddress3.Visible = false;
                DeliveryZipCode.Visible = false;
                DeliveryCity.Visible = false;
                DeliveryCountry.Visible = false;
                DeliveryContactPerson.Visible = false;
                DeliveryPhone.Visible = false;
                DeliveryContactEmail.Visible = false;
            }
            if (Comp.HideCostPrice)
            {
                Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
            CostValue.Visible = CostValue.ShowInColumnChooser = false;
            }
        }

        async void initialLoad()
        {
            Debcache = api.GetCache(typeof(Debtor)) ?? await api.LoadCache(typeof(Debtor));
            UpdateEDocumentMenuVisibility(eDocumentMasterDebtor ?? GetDebtor(dgInvoicesGrid.SelectedItem as DebtorInvoiceClient));
        }

        void InitEDocumentMenu(RibbonBase rb, UnicontaBaseEntity master)
        {
            eDocumentMenuItems = new Dictionary<ItemBase, bool>();
            if (rb?.MenuItems != null)
            {
                foreach (var item in rb.MenuItems)
                {
                    var tag = Convert.ToString(item.Tag);
                    if (tag == "SendUBL" || tag == "DocSendLog")
                        eDocumentMenuItems[item] = item.IsModule;
                }
            }

            eDocumentMasterDebtor = master as Debtor;
            dgInvoicesGrid.SelectedItemChanged += dgInvoicesGrid_SelectedItemChanged;
            UpdateEDocumentMenuVisibility(eDocumentMasterDebtor ?? GetDebtor(dgInvoicesGrid.SelectedItem as DebtorInvoiceClient));
        }

        void dgInvoicesGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (eDocumentMasterDebtor == null)
                UpdateEDocumentMenuVisibility(GetDebtor(e.NewItem as DebtorInvoiceClient));
        }

        Debtor GetDebtor(DebtorInvoiceClient invoice)
        {
            return invoice == null ? null : Debcache?.Get(invoice._DCAccount) as Debtor ?? invoice.Debtor;
        }

        void UpdateEDocumentMenuVisibility(Debtor debtor)
        {
            if (eDocumentMenuItems == null)
                return;

            var visible = ZugferdUtility.CanUseNetworkEInvoiceActions(debtor);
            foreach (var item in eDocumentMenuItems)
                item.Key.IsModule = visible && item.Value;
        }

        public override void ClearMemory()
        {
            dgInvoicesGrid.SelectedItemChanged -= dgInvoicesGrid_SelectedItemChanged;
            eDocumentMenuItems?.Clear();
            base.ClearMemory();
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
#if MAC
                    var selectedItems = new List<DebtorInvoiceClient>() { selectedItem };
#else
                    var selectedItems = dgInvoicesGrid.SelectedItems.Cast<DebtorInvoiceClient>();
#endif
                    if (ActionType == "ShowInvoice")
                        ShowDocument(selectedItems, true);
                    else
                        ShowDocument(selectedItems, false);
                    break;
                case "OriginalInvoicePdf":
                    if (selectedItem != null)
                        OriginalInvoicePdf(selectedItem, api, busyIndicator);
                    break;
                case "Trans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AccountsTransaction, selectedItem, Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher));
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
                        cwOrderInvoice.Args = new Uniconta.ClientTools.ChildWindowCallerArgs(this, api);
                        cwOrderInvoice.DialogTableId = 2000000032;
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
                case "ExportUBL":
                    selectedInvoiceUBL = dgInvoicesGrid.SelectedItems?.Cast<DebtorInvoiceClient>();
                    if (selectedInvoiceUBL != null)
                        ExportUBL(selectedInvoiceUBL);
                    break;
                case "ExportUBLNoMark":
                    selectedInvoiceUBL = dgInvoicesGrid.SelectedItems?.Cast<DebtorInvoiceClient>();
                    if (selectedInvoiceUBL != null)
                        ExportUBL(selectedInvoiceUBL, false);
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
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "PreviousAddresses":
                    if (selectedItem?.Debtor != null)
                        AddDockItem(TabControls.DCPreviousAddressPage, selectedItem.Debtor, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PreviousAddresses"), selectedItem.Debtor._Name));
                    break;
                case "DocSendLog":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DocsSendLogGridPage, dgInvoicesGrid.syncEntity);
                    break;
                case "ProducerResponsibility":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorPackagingTransPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProducerResponsibility"), selectedItem.InvoiceNum));
                    break;
                case "ReturnReasonTransactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvTransReasonReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ReturnReason"), selectedItem.InvoiceNum));
                    break;
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

        StandardPrintReportPage standardViewerPageForDocument;
        bool IsGeneratingDocument;
        DCPreviousAddressClient[] previousAddressLookup;
        DebtorMessagesClient[] messagesLookup;
        bool hasLookups;
        async private void ShowDocument(IEnumerable<DebtorInvoiceClient> debtorInvoices, bool isInvoice)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
#if MAC
                var invClient = dgInvoicesGrid.SelectedItem as DebtorInvoiceClient;
                var iprintReport = isInvoice ? await PrintInvoice(invClient) : await PrintPackNote(invClient);

                if (iprintReport != null)
                {
                    var report = iprintReport.Report;
                    var control = MacPageUtil.PreviewReportsOnMac(report, iprintReport.AdditionalData as byte[]);
                    var docNumber = isInvoice ? invClient._InvoiceNumber : invClient._PackNote;
                     var dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", isInvoice ? Uniconta.ClientTools.Localization.lookup("Invoice") :
                                Uniconta.ClientTools.Localization.lookup("Packnote"), NumberConvert.ToString(docNumber)));

                    AddDockItem(TabControls.ShowControlPage, new object[] { control }, dockName);
                }
#else
                var docList = debtorInvoices.ToList();
                var failedPrints = new List<long>();
                var count = docList.Count;
                string dockName = null, reportName = null;
                bool exportAsPdf = false;
                DevExpress.Xpf.Dialogs.DXFolderBrowserDialog folderDialogSaveInvoice = null;
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

                    ribbonControl.DisableButtons(new[] { "ShowInvoice", "ShowPackNote" });
                }

                foreach (var debtInvoice in docList)
                {
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
                                    if (dialogResult == true)
                                        directoryPath = folderDialogSaveInvoice.SelectedPath;
                                }
                                else
                                    directoryPath = folderDialogSaveInvoice.SelectedPath;

                                ExportReportAsPdf(printReport, directoryPath, docName, NumberConvert.ToString(docNumber));
                            }
                            else
                            {
                                if (printReport.AdditionalData is byte[] zugferdXmlData && zugferdXmlData.Length > 0)
                                {
                                    var individualReportName = await Utility.GetLocalizedReportName(api, debtInvoice, CompanyLayoutType.Invoice);
                                    var individualDockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"),
                                        string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), NumberConvert.ToString(docNumber)));
                                    AddDockItem(TabControls.StandardPrintReportPage, new object[] { printReport, individualReportName }, individualDockName);
                                }
                                else if (standardViewerPageForDocument == null)
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

                            AddDockItem(TabControls.StandardPrintReportPage, new object[] { printReport, reportName }, dockName);
                            break;
                        }
                    }
                    else
                        failedPrints.Add(debtInvoice.InvoiceNumber);
                }

                IsGeneratingDocument = false;
                ribbonControl?.EnableButtons(new[] { "ShowInvoice", "ShowPackNote" });

                if (failedPrints.Count > 0)
                {
                    var failedList = string.Join(",", failedPrints);
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                }
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("Invoices.ShowDocument(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
        }

        private static void ExportReportAsPdf(IPrintReport printReport, string directoryPath, string documentName, string documentNumber)
        {
            if (!(printReport.AdditionalData is byte[] zugferdXmlData) || zugferdXmlData.Length == 0)
            {
                Utility.ExportReportAsPdf(printReport.Report, directoryPath, documentName, documentNumber);
                return;
            }

            if (string.IsNullOrEmpty(directoryPath))
                return;

            var fileName = string.Format("{0}_{1}.pdf", documentName, documentNumber);
            var filePath = Path.Combine(directoryPath, fileName);
            var pdfExportOptions = Utility.GetPdfExportOptions(fileName) ?? new DevExpress.XtraPrinting.PdfExportOptions();
            ZugferdUtility.PatchZugferdPdfExportOptions(pdfExportOptions);

            byte[] pdfData;
            using (var stream = UnistreamReuse.Create())
            {
                printReport.Report.ExportToPdf(stream, pdfExportOptions);
                pdfData = stream.ToArrayAndRelease();
            }

            File.WriteAllBytes(filePath, ZugferdUtility.AttachZugferdXmlToPdf(zugferdXmlData, pdfData));
        }

        async public static void OriginalInvoicePdf(DebtorInvoiceClient debtorInvoice, CrudAPI api, BusyIndicator busyIndicator)
        {
            if (busyIndicator != null)
                busyIndicator.IsBusy = true;
            var invoicePdf = await new InvoiceAPI(api).GetInvoicePdf(debtorInvoice);
            if (invoicePdf != null)
                new CWDocumentViewer(invoicePdf, FileextensionsTypes.PDF).Show();
            if (busyIndicator != null)
                busyIndicator.IsBusy = false;
        }

        async private void OpenOutLook(DebtorInvoiceClient invClient)
        {
            try
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
                var debtor = invClient.Debtor;
                var invoicePdf = await new InvoiceAPI(api).GetInvoicePdf(invClient);

                if (invoicePdf == null)
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("FileNotFound"), string.Join(Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), ": ",
                        invClient.InvoiceNum)), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }

                var documentType = invClient._LineTotal >= -0.0001d ? CompanyLayoutType.Invoice : CompanyLayoutType.Creditnote;
                invClient._SendTime = DateTime.MinValue; // this will for outlook function to update server
                var language = Uniconta.Reports.Utilities.ReportGenUtil.GetLanguage(debtor, api.CompanyEntity);
                var localization = Uniconta.ClientTools.Localization.GetLocalization(language);
                var docName = InvoicePostingPrintGenerator.FormatDocumentName(documentType, invClient, localization);
                var tempPath = System.IO.Path.GetTempPath();
                var filePath = string.Format("{0}{1}.pdf", tempPath, docName);
                File.WriteAllBytes(filePath, invoicePdf);
                //Include Attachements
                filePath = await InvoicePostingPrintGenerator.AppendExtraAttachements(api, filePath, documentType, localization, null, invClient);

                InvoicePostingPrintGenerator.ExecuteOpenOutlook(api, invClient, debtor, documentType, language, localization, docName, filePath);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
            finally { busyIndicator.IsBusy = false; }
        }

        private async Task<IPrintReport> PrintInvoice(DebtorInvoiceClient debtorInvoice)
        {
            IPrintReport iPrintReport = null;
            var debtorInvoicePrint = new DebtorInvoicePrintReport(debtorInvoice, api);

            //In case of Multple invoices we create a lookup
            if (hasLookups)
                await FillLookUps(debtorInvoicePrint);

            var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardDebtorInvoice = new DebtorInvoiceReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.DebtorInvoiceLines, debtorInvoicePrint.DebtorOrder,
                    debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, isCreditNote: debtorInvoicePrint.IsCreditNote, messageClient: debtorInvoicePrint.MessageClient);


                var standardReports = new[] { standardDebtorInvoice };
                iPrintReport = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Invoice);
                await iPrintReport.InitializePrint();

                if (iPrintReport.Report == null)
                {
                    iPrintReport = new LayoutPrintReport(api, debtorInvoice, debtorInvoicePrint.IsCreditNote ? CompanyLayoutType.Creditnote : CompanyLayoutType.Invoice);

                    if (iPrintReport is LayoutPrintReport layout) // Invoke Special Methods for LayoutPrint Report
                    {
                        layout.SetupLayoutPrintFields(debtorInvoicePrint);

                        //In case of Multple invoices we create lookups
                        if (hasLookups)
                            FillLookUps(layout);
                    }

                    await iPrintReport.InitializePrint();
                }

                iPrintReport.AdditionalData = await InvoicePostingPrintGenerator.XmlDataGenerationForGermany(debtorInvoicePrint, api);
            }
            return iPrintReport;
        }

        private async Task<IPrintReport> PrintPackNote(DebtorInvoiceClient debtorInvoice)
        {
            IPrintReport iprintReport = null;
            var packnote = Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote;
            var debtorInvoicePrint = new DebtorInvoicePrintReport(debtorInvoice, api, CompanyLayoutType.Packnote);

            //In case of Multple invoices we create lookups
            if (hasLookups)
                await FillLookUps(debtorInvoicePrint);

            var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();
            if (isInitializedSuccess)
            {
                var standardDebtorInvoice = new DebtorQCPReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.DebtorInvoiceLines, debtorInvoicePrint.DebtorOrder, debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName,
                  (int)packnote, messageClient: debtorInvoicePrint.MessageClient);

                var standardReports = new[] { standardDebtorInvoice };
                iprintReport = new StandardPrintReport(api, standardReports, (byte)packnote);
                await iprintReport.InitializePrint();

                if (iprintReport.Report == null)
                {
                    //Call Layout Invoice
                    iprintReport = new LayoutPrintReport(api, debtorInvoice, CompanyLayoutType.Packnote);
                    if (iprintReport is LayoutPrintReport layoutPrint) // Invoke Special Methods for LayoutPrint Report
                    {
                        layoutPrint.SetupLayoutPrintFields(debtorInvoicePrint);

                        //In case of Multple invoices we create lookups
                        if (hasLookups)
                            FillLookUps(layoutPrint);
                    }
                    await iprintReport.InitializePrint();
                }
            }
            return iprintReport;
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
            cwSendInvoice.DialogTableId = 2000000028;
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


        async private Task<(int saved, int failed, string targetDir)> ExportPeppol(IEnumerable<DebtorInvoiceClient> invoices, string preSelectedFolder = null, bool markAsSent = true)
        {
            var invList = invoices.ToList();
            if (invList.Count == 0) return (0, 0, null);

            Debcache = Debcache ?? await api.LoadCache(typeof(Debtor));
            string FileNameMask(DebtorInvoiceClient inv)
            {
                var deb = Debcache?.Get(inv._DCAccount) as Debtor;
                var prefix = deb != null && deb._InvoiceInXML ? "Nemhandel_" : "Peppol_";
                return string.Concat(prefix, Uniconta.ClientTools.Localization.lookup("Invoice"), "_{0}.xml");
            }

            string targetDir;

            string appDocPeppolPath = null;
            var session = api.session;
            if (session.User._AppDocPath != string.Empty && Directory.Exists(session.User._AppDocPath))
            {
                try
                {
                    appDocPeppolPath = System.IO.Path.Combine(session.User._AppDocPath, "Peppol");
                    Directory.CreateDirectory(appDocPeppolPath);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                    appDocPeppolPath = null;
                }
            }

            if (invList.Count == 1 && preSelectedFolder == null && appDocPeppolPath == null)
            {
                var saveDialog = UtilDisplay.LoadSaveFileDialog;
                saveDialog.FileName = string.Format(FileNameMask(invList[0]), invList[0].InvoiceNum);
                saveDialog.Filter = "XML-File | *.xml";
                if (saveDialog.ShowDialog() != true) return (0, 0, null);

                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = string.Format(Uniconta.ClientTools.Localization.lookup("ExportingFile"), "...");
                try
                {
                    var xmlBytes = await new EDocumentAPI(api).EDocumentGenerateInvoice(invList[0]);
                    if (xmlBytes == null || xmlBytes.Length == 0)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoResult"), Uniconta.ClientTools.Localization.lookup("Error"));
                        return (0, 1, null);
                    }
                    TrimAndWrite(saveDialog.FileName, xmlBytes);
                    var inv0 = invList[0];
                    if (markAsSent)
                    {
                        await new InvoiceAPI(api).MarkSendInvoiceOIO(inv0);
                        inv0.SendTimeOIO = BasePage.GetSystemDefaultDate();
                    }
                    inv0._SystemInfo = "Ok";
                    inv0.NotifyPropertyChanged("SystemInfo");
                    return (1, 0, Path.GetDirectoryName(saveDialog.FileName));
                }
                catch (Exception ex) { UnicontaMessageBox.Show(ex); return (0, 1, null); }
                finally { busyIndicator.IsBusy = false; }
            }

            if (appDocPeppolPath != null)
                targetDir = appDocPeppolPath;
            else if (preSelectedFolder != null)
                targetDir = preSelectedFolder;
            else
            {
                var folderDialog = UtilDisplay.LoadFolderBrowserDialog;
                if (folderDialog.ShowDialog() != true) return (0, 0, null);
#if !MAC
                targetDir = folderDialog.SelectedPath;
#else
                targetDir = folderDialog.FolderName;    
#endif
            }

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = string.Format(Uniconta.ClientTools.Localization.lookup("ExportingFile"), "...");
            int saved = 0, failed = 0;
            try
            {
                var invApi = new InvoiceAPI(api);
                var edocAPI = new EDocumentAPI(api);
                foreach (var inv in invList)
                {
                    try
                    {
                        var xmlBytes = await edocAPI.EDocumentGenerateInvoice(inv);
                        if (xmlBytes == null || xmlBytes.Length == 0) { failed++; continue; }
                        var path = System.IO.Path.Combine(targetDir, string.Format(FileNameMask(inv), inv.InvoiceNum));
                        TrimAndWrite(path, xmlBytes);
                        if (markAsSent)
                        {
                            await invApi.MarkSendInvoiceOIO(inv);
                            inv.SendTimeOIO = BasePage.GetSystemDefaultDate();
                        }
                        inv._SystemInfo = "Ok";
                        inv.NotifyPropertyChanged("SystemInfo");
                        saved++;
                    }
                    catch { failed++; }
                }
            }
            finally { busyIndicator.IsBusy = false; }

            return (saved, failed, targetDir);
        }

        private static void TrimAndWrite(string path, byte[] xmlBytes)
        {
            int validLen = xmlBytes.Length;
            while (validLen > 0 && xmlBytes[validLen - 1] == 0)
                validLen--;
            if (validLen < xmlBytes.Length)
                Array.Resize(ref xmlBytes, validLen);
            File.WriteAllBytes(path, xmlBytes);
        }



        private async void ExportUBL(IEnumerable<DebtorInvoiceClient> invoiceUBL, bool markAsSent = true)
        {
            eDeliveryMappingGroups = eDeliveryMappingGroups ?? api.GetCache(typeof(eDeliveryMappingGroupClient))
                ?? await api.LoadCache(typeof(eDeliveryMappingGroupClient));

            LayoutGroupCache = api.GetCache(typeof(DebtorLayoutGroup)) ?? await api.LoadCache(typeof(DebtorLayoutGroup));

            Debcache = Debcache ?? api.GetCache(typeof(Debtor)) ?? await api.LoadCache(typeof(Debtor));

            var peppolList = new List<DebtorInvoiceClient>();
            var oioublList = new List<DebtorInvoiceClient>();

            foreach (var inv in invoiceUBL)
            {
                var deb = Debcache?.Get(inv._DCAccount) as Debtor;
                if (deb?._InvoiceInPepPol == true)
                    peppolList.Add(inv);
                else
                    oioublList.Add(inv);
            }

            int totalCnt = peppolList.Count + oioublList.Count;
            if (totalCnt == 0) return;

            if (UnicontaMessageBox.Show(
                    string.Concat(Uniconta.ClientTools.Localization.lookup("QtyMarked"), " ", totalCnt),
                    string.Format(Uniconta.ClientTools.Localization.lookup(markAsSent ? "ExportOBJ" : "ExportNotMarkedSentOBJ"), Uniconta.ClientTools.Localization.lookup("einvoice")),
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            var session = api.session;
            string sharedFolder = null;
            if (peppolList.Count > 0 && oioublList.Count > 0)
            {
                var appDocPathCheck = session.User._AppDocPath;
                if (appDocPathCheck == string.Empty || !Directory.Exists(appDocPathCheck))
                {
                    var dlg = UtilDisplay.LoadFolderBrowserDialog;
                    if (dlg.ShowDialog() != true) return;
#if MAC
                    sharedFolder = dlg.FolderName;
#else
                    sharedFolder = dlg.SelectedPath;
#endif
                }
            }

            int peppolSaved = 0, peppolFailed = 0, oioublSaved = 0, oioublErrors = 0;
            string exportPath = null, ublExportPath = null;

            if (peppolList.Count > 0)
                (peppolSaved, peppolFailed, exportPath) = await ExportPeppol(peppolList, sharedFolder, markAsSent);

            if (oioublList.Count > 0)
                (oioublSaved, oioublErrors, ublExportPath) = await ExportUBL(oioublList, api, SystemInfo, LayoutGroupCache, Debcache, eDeliveryMappingGroups, false, sharedFolder, markAsSent);
            
            int totalSaved = peppolSaved + oioublSaved;
            if (totalSaved > 0)
            {
                exportPath = string.IsNullOrEmpty(exportPath) ? ublExportPath : string.IsNullOrEmpty(ublExportPath) ? exportPath : exportPath + ", " + ublExportPath;
                UnicontaMessageBox.Show(
                    string.Format(Uniconta.ClientTools.Localization.lookup("SaveFileMsgOBJ"), totalSaved,
                        totalSaved == 1 ? Uniconta.ClientTools.Localization.lookup("Invoice") : Uniconta.ClientTools.Localization.lookup("Invoices"),
                        exportPath ?? sharedFolder),
                    Uniconta.ClientTools.Localization.lookup("Message"),
                    MessageBoxButton.OK);
            }
        }

        public static async Task<(int saved, int errors, string targetDir)> ExportUBL(IEnumerable<DebtorInvoiceClient> invoiceUBL, CrudAPI api,
            CorasauDataGridTemplateColumnClient SystemInfo, SQLCache LayoutGroupCache, SQLCache Debcache,
            SQLCache eDeliveryMappingGroups, bool isInvoicePage, string preSelectedFolder = null, bool markAsSent = true)
        {
            int cnt = invoiceUBL.Count();
            if (isInvoicePage && UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("QtyMarked"), " ", cnt),
                    string.Format(Uniconta.ClientTools.Localization.lookup("ExportOBJ"), Uniconta.ClientTools.Localization.lookup("einvoice")), MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return (0, 0, null);

            var Comp = api.CompanyEntity;

            var InvCache = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem));
            var VatCache = api.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVat));
            SQLCache workInstallCache = null;
            SQLCache Contacts = null;
            if (SystemInfo != null)
                SystemInfo.Visible = true;

            var applFilePath = string.Empty;
            var listOfXmlPath = new List<string>();
            int countErr = 0;
            Microsoft.Win32.SaveFileDialog saveDialog = null;
#if MAC
            Microsoft.Win32.OpenFolderDialog folderBrowserDialog = null;
#else
            DevExpress.Xpf.Dialogs.DXFolderBrowserDialog folderBrowserDialog = null;
#endif
            InvoiceAPI Invapi = new InvoiceAPI(api);

            foreach (var invClient in invoiceUBL.ToList())
            {
                bool createXmlFile = true;

                if (Debcache == null)
                    Debcache = await api.CompanyEntity.LoadCache(typeof(Debtor), api);
                var debtor = (Debtor)Debcache.Get(invClient._DCAccount);
                if (debtor == null)
                    continue;

                string invoiceType;
                XmlDocument xml = null;
                if (ZugferdUtility.IsGermanDebtor(debtor))
                {
                    if (!ZugferdUtility.ShouldGenerateXml(debtor))
                    {
                        countErr++;
                        createXmlFile = false;
                        invClient._SystemInfo = Localization.lookup("einveInvoiceNotActivatedoice");
                        invClient.NotifyPropertyChanged("SystemInfo");
                        continue;
                    }

                    invoiceType = "ZUGFeRD";
                    var xmlBytes = await new EDocumentAPI(api).EDocumentGenerateZugferdXmlInvoice(invClient);
                    if (xmlBytes != null && xmlBytes.Length > 0)
                    {
                        xml = new XmlDocument();
                        using (var stream = new MemoryStream(xmlBytes, writable: false))
                            xml.Load(stream);
                    }
                    else
                    {
                        countErr++;
                        createXmlFile = false;
                        invClient._SystemInfo =
                            Localization.lookup("Error") + " " + Localization.lookup("GenerateInvoice");
                        invClient.NotifyPropertyChanged("SystemInfo");
                    }
                }
                else
                {
                    invoiceType = Localization.lookup("eDocument");
                    var mapped = await GetEdeliveryMappingDoc(invClient, api, eDeliveryMappingGroups);
                    if (mapped.error != ErrorCodes.Succes)
                    {
                        countErr++;
                        invClient._SystemInfo = EDeliveryMappingPage.GetSendErrorMessage(mapped.error, null);
                        invClient.NotifyPropertyChanged("SystemInfo");
                        continue;
                    }
                    xml = mapped.document;
                    if (xml == null)
                    {
                        var InvTransInvoiceLines =
                            (DebtorInvoiceLines[])await Invapi.GetInvoiceLines(invClient, new DebtorInvoiceLines());

                        Contact contactPerson = null;
                        if (invClient._ContactRef != 0)
                        {
                            if (Contacts == null)
                                Contacts = api.GetCache(typeof(Uniconta.DataModel.Contact)) ??
                                           await api.LoadCache(typeof(Uniconta.DataModel.Contact));
                            foreach (var contact in (Uniconta.DataModel.Contact[])Contacts.GetRecords)
                                if (contact.RowId == invClient._ContactRef)
                                {
                                    contactPerson = contact;
                                    break;
                                }
                        }

                        UtilCommon.SetDeliveryAdress(invClient, debtor, api);

                        Debtor deliveryAccount;
                        if (invClient._DeliveryAccount != null)
                            deliveryAccount = (Debtor)Debcache.Get(invClient._DeliveryAccount);
                        else
                            deliveryAccount = null;

                        WorkInstallation workInstallation = null;
                        if (invClient._Installation != null)
                        {
                            if (workInstallCache == null)
                                workInstallCache = api.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ??
                                                   await api.LoadCache(typeof(Uniconta.DataModel.WorkInstallation));

                            workInstallation = (WorkInstallation)workInstallCache.Get(invClient._Installation);
                        }

                        var attachments =
                            await FromXSDFile.OIOUBL.ExportImport.Attachments.CollectInvoiceAttachments(invClient, api);
                        var result = Uniconta.API.DebtorCreditor.OIOUBL.GenerateOioXML(Comp, debtor, deliveryAccount,
                            invClient, InvTransInvoiceLines, InvCache, VatCache, null, contactPerson, attachments,
                            LayoutGroupCache, workInstallation);

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
                        else
                            xml = result.Document;
                    }
                }

                if (xml != null && createXmlFile)
                {
                    string invoice = Uniconta.ClientTools.Localization.lookup("Invoice");
                    string filename = null;

                    if (session.User._AppDocPath != string.Empty && Directory.Exists(session.User._AppDocPath))
                    {
                        try
                        {
                            applFilePath = string.Concat(session.User._AppDocPath, "\\" + invoiceType);
                            Directory.CreateDirectory(applFilePath);

                            filename = string.Format("{0}\\{1}_{2}.xml", applFilePath, invoice, invClient.InvoiceNumber);
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
                            saveDialog.FileName = string.Concat(invoiceType + "_", invoice, "_", NumberConvert.ToString(invClient.InvoiceNumber));
                            saveDialog.Filter = "XML-File | *.xml";
                            bool? dialogResult = saveDialog.ShowDialog();
                            if (dialogResult != true)
                                break;
                        }
                        else if (folderBrowserDialog == null)
                        {
                            if (preSelectedFolder != null)
                            {
#if MAC
                                folderBrowserDialog = new Microsoft.Win32.OpenFolderDialog { FolderName = preSelectedFolder };
#else
                                folderBrowserDialog = new DevExpress.Xpf.Dialogs.DXFolderBrowserDialog { SelectedPath = preSelectedFolder };
#endif
                            }
                            else
                            {
                                folderBrowserDialog = UtilDisplay.LoadFolderBrowserDialog;
                                var dialogResult = folderBrowserDialog.ShowDialog();
                                if (dialogResult != true)
                                    break;
                            }
                        }

                        if (cnt > 1)
                        {
#if MAC
                            filename = folderBrowserDialog.FolderName;
#else
                            filename = folderBrowserDialog.SelectedPath;
#endif
                            filename = string.Format("{0}\\{1}_{2}_{3}.xml", filename, invoiceType, invoice, invClient.InvoiceNumber);
                        }
                        else
                        {
                            filename = saveDialog.FileName;
                        }
                    }

                    listOfXmlPath.Add(filename);
                    try
                    {
                        xml.Save(filename);
                    }
                    catch (Exception ex)
                    {
                        Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(ex);
                    }
                    if (markAsSent)
                    {
                        await Invapi.MarkSendInvoiceOIO(invClient);
                        invClient.SendTimeOIO = BasePage.GetSystemDefaultDate();
                    }
                    invClient._SystemInfo = "Ok";
                }

                invClient.NotifyPropertyChanged("SystemInfo");
            }

            if (countErr != 0)
            {
                UnicontaMessageBox.Show(string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceFileCreationFailMsg"), countErr), " ",
                    Uniconta.ClientTools.Localization.lookup("CheckSystemInfoColumnMsg")), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
            }

            return (listOfXmlPath.Count, countErr, string.Join(", ", listOfXmlPath.Select(Path.GetDirectoryName).Distinct()));
        }

        private static async Task<(XmlDocument document, ErrorCodes error)> GetEdeliveryMappingDoc(DebtorInvoiceClient inv, CrudAPI api,
            SQLCache eDeliveryMappingGroups)
        {
            if (inv == null || eDeliveryMappingGroups == null)
                return (null, ErrorCodes.Succes);
            var nhrApi = new NHRAPI(api);
            return await GetMappedExportDocumentAsync(() => nhrApi.GetMappingsByInvoice(inv, eDeliveryMappingGroups),
                mappings => GetMappingsWithTags(mappings, () => api.LoadCache(typeof(eDeliveryTagTypeClient), true),
                    () => UnicontaMessageBox.Show(Localization.lookup(ErrorCodes.FieldCannotBeBlank.ToString()) + ": Tag", Localization.lookup("Error"))),
                mappings => nhrApi.GetValidatedeDeliveryMappingDoc(inv, mappings), () => nhrApi.LastError, api.ReportException);
        }

        internal static async Task<(XmlDocument document, ErrorCodes error)> GetMappedExportDocumentAsync(
            Func<Task<eDeliveryMappingClient[]>> loadMappings,
            Func<eDeliveryMappingClient[], Task<List<eDeliveryMappingValidationRequest>>> loadTags,
            Func<List<eDeliveryMappingValidationRequest>, Task<string>> generate,
            Func<ErrorCodes> lastError, Action<Exception> reportException)
        {
            try
            {
                var mappings = await loadMappings();
                if (lastError() != ErrorCodes.Succes)
                    return (null, lastError());
                if (mappings == null || mappings.Length == 0)
                    return (null, ErrorCodes.Succes);
                
                var mappingsWithTags = await loadTags(mappings);
                if (mappingsWithTags == null)
                    return (null, ErrorCodes.FieldCannotBeBlank);

                var xml = await generate(mappingsWithTags);
                if (xml == null)
                    return (null, lastError() == ErrorCodes.Succes ? ErrorCodes.CouldNotLoad : lastError());

                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);
                return (xmlDoc, ErrorCodes.Succes);
            }
            catch (Exception ex)
            {
                reportException(ex);
                return (null, ErrorCodes.Exception);
            }
        }

        internal static async Task<List<eDeliveryMappingValidationRequest>> GetMappingsWithTags(
            IEnumerable<eDeliveryMappingClient> mappings, Func<Task<SQLCache>> loadCache, Action showError)
        {
            var mappingsWithTags = new List<eDeliveryMappingValidationRequest>();
            var reloadedCache = false;
            foreach (var m in mappings)
            {
                var tag = m.eDeliveryTag?.Name;
                if (string.IsNullOrEmpty(tag) && !reloadedCache)
                {
                    await loadCache();
                    reloadedCache = true;
                    tag = m.eDeliveryTag?.Name;
                }
                if (string.IsNullOrEmpty(tag))
                {
                    showError();
                    return null;
                }

                mappingsWithTags.Add(new eDeliveryMappingValidationRequest
                {
                    Mapping = m,
                    TagPath = tag
                });
            }
            return mappingsWithTags;
        }

        private async void SendUBL(IEnumerable<DebtorInvoiceClient> invoiceUBL)
        {
            var oioublList = invoiceUBL.ToList();

            int totalCnt = oioublList.Count;
            if (totalCnt == 0) return;

            Debcache = Debcache ?? api.GetCache(typeof(Debtor)) ?? await api.LoadCache(typeof(Debtor));
            var invalidInvoice = oioublList.FirstOrDefault(inv => !ZugferdUtility.CanUseNetworkEInvoiceActions(GetDebtor(inv)));
            if (invalidInvoice != null)
            {
                UnicontaMessageBox.Show(
                    string.Format("{0} {1}: {2} (Peppol)", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), invalidInvoice.InvoiceNum,
                        Uniconta.ClientTools.Localization.lookup("FunctionNotSupported")),
                    Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UnicontaMessageBox.Show(
                    string.Concat(Uniconta.ClientTools.Localization.lookup("QtyMarked"), " ", totalCnt),
                    Uniconta.ClientTools.Localization.lookup("SendInvoicebyUBL"),
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            if (SystemInfo != null)
                SystemInfo.Visible = true;

            if (oioublList.Count > 0)
                await SendOIOUBLInvoices(oioublList);
        }

        private async Task SendOIOUBLInvoices(List<DebtorInvoiceClient> invoices)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
            var invApi = new InvoiceAPI(api);
            var errors = new List<string>();
            int cntInv = 0;
            bool sendInBackgroundOnly = false;

            foreach (var inv in invoices)
            {
                cntInv++;
                if (cntInv > 10) sendInBackgroundOnly = true;
                var errorCode = await invApi.SendOIOUBLInvoice(inv, sendInBackgroundOnly);
                if (errorCode != ErrorCodes.Succes)
                {
                    var standardError = await api.session.GetErrors(errorCode);
                    var stformattedErr = UtilDisplay.GetFormattedErrorCode(errorCode, standardError.FirstOrDefault());
                    inv._SystemInfo = stformattedErr;
                    errors.Add(string.Format("{0}({1}): \n{2}",
                        Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), inv.InvoiceNum,
                        Uniconta.ClientTools.Localization.lookup(stformattedErr)));
                }
            }

            busyIndicator.IsBusy = false;

            if (errors.Count == 1)
                UnicontaMessageBox.Show(errors[0], Uniconta.ClientTools.Localization.lookup("Warning"));
            else if (errors.Count > 0)
                new CWErrorBox(errors.ToArray(), true).Show();
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("OIOSent"), Uniconta.ClientTools.Localization.lookup("Message"));
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pp = (sender as System.Windows.Controls.Image).Tag as DebtorInvoiceClient;
            if (pp != null)
                AddDockItem(TabControls.UserDocsPage, dgInvoicesGrid.syncEntity);
        }

        public static async void SendUBL(IEnumerable<DebtorInvoiceClient> invoiceUBL, CrudAPI api,
            CorasauDataGridTemplateColumnClient SystemInfo, BusyIndicator busyIndicator)
        {
            int icount = invoiceUBL.Count();

            if (UnicontaMessageBox.Show(
                    string.Concat(Uniconta.ClientTools.Localization.lookup("QtyMarked"), " ", icount),
                    Uniconta.ClientTools.Localization.lookup("SendInvoicebyUBL"),
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                return;

            if (SystemInfo != null)
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
                    var stformattedErr = UtilDisplay.GetFormattedErrorCode(errorCode, standardError.FirstOrDefault());
                    var errorStr = string.Format("{0}({1}): \n{2}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), inv.InvoiceNum,
                        Uniconta.ClientTools.Localization.lookup(stformattedErr));
                    inv._SystemInfo = stformattedErr;
                    errors.Add(errorStr);
                }
            }

            busyIndicator.IsBusy = false;


            if (errors.Count == 1)
                UnicontaMessageBox.Show(errors[0], Uniconta.ClientTools.Localization.lookup("Warning"));
            else if (errors.Count > 0)
                new CWErrorBox(errors.ToArray(), true).Show();
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("OIOSent"), Uniconta.ClientTools.Localization.lookup("Message"));
        }

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
                IsGeneratingDocument = false;
                standardViewerPageForDocument = null;
            }
        }
    }
}
