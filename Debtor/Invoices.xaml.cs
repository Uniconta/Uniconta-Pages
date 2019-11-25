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

    public class DebtorInvoiceLocal : DebtorInvoiceClient
    {
        [Display(Name = "System Info")]
        public string SystemInfo { get { return _SystemInfo; } }
        public string _SystemInfo;

        public void NotifySystemInfoSet()
        {
            NotifyPropertyChanged("SystemInfo");
        }
    }


    public class InvoicesGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorInvoiceLocal); } }
        public override IComparer GridSorting { get { return new DCInvoiceSort(); } }
    }

    public partial class Invoices : GridBasePage
    {
        SQLCache Debcache;
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
                    return;
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
            filterDate = BasePage.GetFilterDate(Comp, master != null);
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
            var selectedItem = dgInvoicesGrid.SelectedItem as DebtorInvoiceLocal;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "InvoiceLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorInvoiceLines, dgInvoicesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), selectedItem._InvoiceNumber));
                    break;
                case "ShowInvoice":
                    if (dgInvoicesGrid.SelectedItem == null || dgInvoicesGrid.SelectedItems == null)
                        return;

                    var selectedItems = dgInvoicesGrid.SelectedItems.Cast<DebtorInvoiceLocal>();
                    ShowInvoice(selectedItems);

                    break;
                case "ShowPackNote":
                    if (dgInvoicesGrid.SelectedItem == null || dgInvoicesGrid.SelectedItems == null)
                        return;

                    var items = dgInvoicesGrid.SelectedItems.Cast<DebtorInvoiceLocal>();
                    ShowInvoiceForPackNote(items);
                    break;
                case "Trans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PostedTransactions, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._InvoiceNumber));
                    break;
                case "SendInvoice":
                    if (dgInvoicesGrid.SelectedItem == null || dgInvoicesGrid.SelectedItems == null)
                        return;
                    var selectedInvoiceEmails = dgInvoicesGrid.SelectedItems.Cast<DebtorInvoiceLocal>();
                    SendInvoice(selectedInvoiceEmails);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._InvoiceNumber));
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
                        GenerateOIOXmlForAll(new List<DebtorInvoiceLocal>() { selectedItem });
                    break;
                case "GenerateMarkedOioXml":
                    var selected = dgInvoicesGrid.SelectedItems;
                    if (selected != null)
                        GenerateOIOXmlForAll(selected, true);
                    break;
                case "GenerateAllOioXml":
                    var filteredRows = dgInvoicesGrid.GetVisibleRows();
                    GenerateOIOXmlForAll(filteredRows);
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
                    if (selectedItem != null && selectedItem?.Debtor != null)
                        AddDockItem(TabControls.DCPreviousAddressPage, selectedItem.Debtor, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PreviousAddresses"), selectedItem.Debtor._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(DebtorInvoiceLocal selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        async private void ShowInvoice(IEnumerable<DebtorInvoiceLocal> debtorInvoices)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                var invoicesList = debtorInvoices.ToList();
#if !SILVERLIGHT
                var failedPrints = new List<long>();
                var invoiceReports = new List<IPrintReport>();
                long invNumber = 0;
#elif SILVERLIGHT
                int top = 200, left = 300;
                int count = invoicesList.Count();

                if (count > 1)
                {
#endif
                foreach (var debtInvoice in invoicesList)
                {
#if !SILVERLIGHT
                    IPrintReport printReport = await PrintInvoice(debtInvoice);
                    invNumber = debtInvoice._InvoiceNumber;
                    if (printReport?.Report != null)
                        invoiceReports.Add(printReport);
                    else
                        failedPrints.Add(debtInvoice.InvoiceNumber);
                }

                if (invoiceReports.Count() > 0)
                {
                    if (invoiceReports.Count == 1 && invNumber > 0)
                    {
                        var reportName = string.Format("{0}_{1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invNumber);
                        var dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invNumber));
                        AddDockItem(TabControls.StandardPrintReportPage, new object[] { invoiceReports, reportName }, dockName);
                    }
                    else
                    {
                        var reportsName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("Invoices"));
                        AddDockItem(TabControls.StandardPrintReportPage, new object[] { invoiceReports, Uniconta.ClientTools.Localization.lookup("Invoices") }, reportsName);
                    }

                    if (failedPrints.Count > 0)
                    {
                        var failedList = string.Join(",", failedPrints);
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    }
                }
                else
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("LayoutDoesNotExist"), Uniconta.ClientTools.Localization.lookup("Invoice")),
                       Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
#elif SILVERLIGHT
                        DefaultPrint(debtInvoice, true, new Point(top, left));
                        left = left - left / count;
                        top = top - top / count;
                    }
                }
                else
                    DefaultPrint(debtorInvoices.Single());
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("Invoices.ShowInvoice(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
        }

        async private void ShowInvoiceForPackNote(IEnumerable<DebtorInvoiceLocal> debtorInvoices)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
            try
            {
                var invoicesList = debtorInvoices.ToList();
#if !SILVERLIGHT
                List<IPrintReport> packnoteReports = new List<IPrintReport>();
#elif SILVERLIGHT
                int top = 200;
                int left = 300;
                int count = invoicesList.Count();

                if (count > 1)
                {
#endif
                foreach (var debtInvoice in invoicesList)
                {
#if !SILVERLIGHT
                    IPrintReport printReport = await PrintPackNote(debtInvoice);

                    if (printReport?.Report != null)
                        packnoteReports.Add(printReport);
                }

                if (packnoteReports.Count() > 0)
                {
                    var reportName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("Packnote"));
                    AddDockItem(TabControls.StandardPrintReportPage, new object[] { packnoteReports, reportName }, reportName);
                }
                else
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("LayoutDoesNotExist"), Uniconta.ClientTools.Localization.lookup("Packnote")),
                       Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
#elif SILVERLIGHT

                        DefaultPrint(debtInvoice, true, new Point(top, left), true);
                        left = left - left / count;
                        top = top - top / count;
                    }
                }
                else
                    DefaultPrint(debtorInvoices.Single(), true);
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("Invoices.ShowPackNote(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
        }

#if !SILVERLIGHT

        private async Task<IPrintReport> PrintInvoice(DebtorInvoiceLocal debtorInvoice)
        {
            IPrintReport iprintReport = null;

            var debtorInvoicePrint = new UnicontaClient.Pages.DebtorInvoicePrintReport(debtorInvoice, api);
            var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();

            if (isInitializedSuccess)
            {
                var standardDebtorInvoice = new DebtorInvoiceReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DebtorOrder,
                    debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, isCreditNote: debtorInvoicePrint.IsCreditNote, messageClient: debtorInvoicePrint.MessageClient);

                var standardReports = new [] { standardDebtorInvoice };
                iprintReport = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Invoice);
                await iprintReport.InitializePrint();


                if (iprintReport?.Report == null)
                {
                    iprintReport = new LayoutPrintReport(api, debtorInvoice, debtorInvoicePrint.IsCreditNote ? CompanyLayoutType.Creditnote : CompanyLayoutType.Invoice);
                    await iprintReport.InitializePrint();
                }
            }

            return iprintReport;
        }

        private async Task<IPrintReport> PrintPackNote(DebtorInvoiceLocal debtorInvoice)
        {
            IPrintReport iprintReport = null;

            var packnote = Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote;

            var debtorInvoicePrint = new UnicontaClient.Pages.DebtorInvoicePrintReport(debtorInvoice, api, CompanyLayoutType.Packnote);
            var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();

            if (isInitializedSuccess)
            {
                var standardDebtorInvoice = new DebtorQCPReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, null,
                    debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, (int)packnote, messageClient: debtorInvoicePrint.MessageClient);

                var standardReports = new [] { standardDebtorInvoice };
                iprintReport = new StandardPrintReport(api, standardReports, (byte)packnote);
                await iprintReport.InitializePrint();


                if (iprintReport?.Report == null)
                {
                    iprintReport = new LayoutPrintReport(api, debtorInvoice, CompanyLayoutType.Packnote);
                    await iprintReport.InitializePrint();
                }
            }

            return iprintReport;
        }


        private async void GenerateOIOXmlForAll(IList lstInvClient, bool selectedrows = false)
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var InvCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api);
            var VatCache = Comp.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLVat), api);

            SystemInfo.Visible = true;

            var applFilePath = string.Empty;
            var listOfXmlPath = new List<string>();
            int countErr = 0;
            bool hasUserFolder = false;
            SaveFileDialog saveDialog = null;
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = null;
            InvoiceAPI Invapi = new InvoiceAPI(api);
            foreach (var inv in lstInvClient)
            {
                var invClient = inv as DebtorInvoiceLocal;
                if (invClient == null)
                    continue;

                var debtor = (Debtor)Debcache.Get(invClient._DCAccount);

                if (!selectedrows && lstInvClient.Count > 1 && (!debtor._InvoiceInXML || invClient.SendTime != DateTime.MinValue))
                    continue;

                var Invoicemasters = new UnicontaBaseEntity[] { invClient };
                var invoiceLines = await api.Query<InvTransClient>(Invoicemasters, null);

                InvItemText[] invItemText = null;
                if (debtor._ItemNameGroup != null)
                    invItemText = await api.Query<InvItemText>(new UnicontaBaseEntity[] { debtor }, null);

                Contact contactPerson = null;
                if (invClient._ContactRef != 0)
                {
                    var queryContact = await api.Query<Contact>(Invoicemasters, null);
                    foreach (var contact in queryContact)
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

                CreationResult result;

                if (Comp._CountryId == CountryCode.Norway || Comp._CountryId == CountryCode.Netherlands)
                    result = EHF.GenerateEHFXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, invItemText, contactPerson);
                else if (Comp._CountryId == CountryCode.Iceland)
                {
                    var paymFormatCache = Comp.GetCache(typeof(DebtorPaymentFormatClientIceland)) ?? await Comp.LoadCache(typeof(DebtorPaymentFormatClientIceland), api);
                    TableAddOnData[] attachments = await UBL.Iceland.Attachments.CollectInvoiceAttachments(invClient, api);

                    result = TS136137.GenerateTS136137XML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, invItemText, contactPerson, paymFormatCache, attachments);
                }
                else
                {
                    TableAddOnData[] attachments = await FromXSDFile.OIOUBL.ExportImport.Attachments.CollectInvoiceAttachments(invClient, api);
                    result = Uniconta.API.DebtorCreditor.OIOUBL.GenerateOioXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, invItemText, contactPerson, attachments);
                }

                bool createXmlFile = true;

                if (result.HasErrors)
                {
                    countErr++;
                    createXmlFile = false;
                    invClient._SystemInfo = string.Empty;
                    foreach (FromXSDFile.OIOUBL.ExportImport.PrecheckError error in result.PrecheckErrors)
                    {
                        invClient._SystemInfo += error.ToString() + "\n";
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
                            applFilePath = string.Format("{0}\\OIOUBL", session.User._AppDocPath);
                            Directory.CreateDirectory(applFilePath);

                            filename = string.Format("{0}\\{1}_{2}.xml", applFilePath, invoice, invClient.InvoiceNumber);
                            hasUserFolder = true;
                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                        }
                    }
                    else
                    {
                        if (saveDialog == null && lstInvClient.Count == 1)
                        {
                            saveDialog = UtilDisplay.LoadSaveFileDialog;
                            saveDialog.FileName = string.Format("{0}_{1}", invoice, invClient.InvoiceNumber);
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

                        if (lstInvClient.Count > 1)
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
                    Invapi.MarkSendInvoice(invClient);
                    invClient.SendTime = BasePage.GetSystemDefaultDate();
                    invClient._SystemInfo = "File created";
                }

                invClient.NotifySystemInfoSet();
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

            //else if(listOfXmlPath != null && listOfXmlPath.Count > 0)
            //{
            //    var result = UnicontaMessageBox.Show("Do you wish to send generated OIOUBL?", Uniconta.ClientTools.Localization.lookup("Send"), MessageBoxButton.YesNo);
            //    if (result == MessageBoxResult.No)
            //        return;

            //    var _log = new ImportLogOIORASP();
            //    OIORASP.OiosiRaspClient raspClient = new OIORASP.OiosiRaspClient(listOfXmlPath, _log);
            //    var hasNoErrors = raspClient.SendDocument();

            //    if (hasNoErrors)
            //    {
            //        UnicontaMessageBox.Show("OIOUBL fil er sendt");
            //        return;
            //    }

            //    var args = new object[1];
            //    args[0] = _log;

            //    AddDockItem(TabControls.OIORASPTrackPage, args, header: "OIORASP Tracker");

            //}
        }

#elif SILVERLIGHT
        private void DefaultPrint(DebtorInvoiceLocal debtorInvoice, bool isPackNote = false)
        {
            object[] ob = new object[2];
            ob[0] = debtorInvoice;
            ob[1] = isPackNote ? CompanyLayoutType.Packnote : CompanyLayoutType.Invoice;
            string headerName = isPackNote ? "PackNote" : "Invoice";
            AddDockItem(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName),
                debtorInvoice._InvoiceNumber));
        }

        private void DefaultPrint(DebtorInvoiceLocal debtorInvoice, bool isFloat, Point position, bool isPackNote = false)
        {
            object[] ob = new object[2];
            ob[0] = debtorInvoice;
            ob[1] = isPackNote ? CompanyLayoutType.Packnote : CompanyLayoutType.Invoice;
            string headerName = isPackNote ? "PackNote" : "Invoice";
            AddDockItem(TabControls.ProformaInvoice, ob, isFloat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup(headerName),
                debtorInvoice._InvoiceNumber), floatingLoc: position);
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
                        AddDockItem(order.__DCType() == 1 ? TabControls.DebtorOrderLines : TabControls.DebtorOfferLines, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, ClientHelper.GetName(order.CompanyId, typeof(Debtor), order._DCAccount)));
                        break;

                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();

        }
        void SendInvoice(IEnumerable<DebtorInvoiceLocal> invoiceEmails)
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

                     foreach (var inv in invoiceEmails)
                     {
                         var errorCode = await Invapi.SendInvoice(inv, cwSendInvoice.Emails, cwSendInvoice.sendOnlyToThisEmail, CWSendInvoice.sendInBackgroundOnly);
                         if (errorCode != ErrorCodes.Succes)
                         {
                             var standardError = await api.session.GetErrors();
                             var stformattedErr = UtilDisplay.GetFormattedErrorCode(errorCode, standardError);
                             var errorStr = string.Format("{0}({1}): \n{2}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), inv._InvoiceNumber,
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

        public async override Task InitQuery()
        {
            await Filter();
            var api = this.api;
            if (api.CompanyEntity.DeliveryAddress)
            {
                var lst = dgInvoicesGrid.ItemsSource as IEnumerable<DebtorInvoiceLocal>;
                if (lst != null)
                {
                    foreach (var rec in lst)
                    {
                        DebtorOrders.SetDeliveryAdress(rec, rec.Debtor, api);
                    }
                }
            }
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
