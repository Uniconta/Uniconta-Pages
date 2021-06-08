using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.Windows.Controls;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.Client.Pages;
using System.ComponentModel.DataAnnotations;
#if !SILVERLIGHT
using UnicontaClient.Pages;
#endif
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class CreditorInvoiceLocal : CreditorInvoiceClient
    {
        [Display(Name = "System Info")]
        public string SystemInfo { get { return _SystemInfo; } }
        public string _SystemInfo;

        internal void NotifySystemInfoSet()
        {
            NotifyPropertyChanged("SystemInfo");
        }
    }


    public class CreditorInvoicesGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorInvoiceLocal); } }
        public override IComparer GridSorting { get { return new DCInvoiceSort(); } }
        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            var api = this.api;
            var comp = api.CompanyEntity;
            if (comp.DeliveryAddress && Arr != null)
            {
                foreach (var rec in Arr)
                {
                    var dcInvoice = rec as CreditorInvoiceLocal;
                    DebtorOrders.SetDeliveryAdress(dcInvoice, dcInvoice.Creditor, api);
                }
            }
        }
    }

    public partial class CreditorInvoice : GridBasePage
    {
        private SynchronizeEntity syncEntity;
        public override string NameOfControl { get { return TabControls.CreditorInvoice.ToString(); } }
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
        public CreditorInvoice(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public CreditorInvoice(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }
        public CreditorInvoice(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCrdInvoicesGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            string header;
            var masterClient = dgCrdInvoicesGrid.masterRecord as Uniconta.DataModel.Creditor;
            if (masterClient != null)
                header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), masterClient._Account);
            else
            {
                var masterClient2 = dgCrdInvoicesGrid.masterRecord as Uniconta.DataModel.CreditorTrans;
                if (masterClient2 != null)
                    header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), masterClient2._Account);
                else
                {
                    var masterClient3 = dgCrdInvoicesGrid.masterRecord as CreditorTransOpenClient;
                    if (masterClient3 != null)
                        header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), masterClient3.Account);
                    else
                        return;
                }
            }
            SetHeader(header);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            dgCrdInvoicesGrid.UpdateMaster(master);
            setDim();
            SetRibbonControl(localMenu, dgCrdInvoicesGrid);
            localMenu.dataGrid = dgCrdInvoicesGrid;
            dgCrdInvoicesGrid.api = api;
            var Comp = api.CompanyEntity;
            if (master == null)
                filterDate = BasePage.GetFilterDate(Comp, false);
            dgCrdInvoicesGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            if (Comp.RoundTo100)
                NetAmount.HasDecimals = TotalAmount.HasDecimals = false;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (!Comp.PurchaseCharges)
                UtilDisplay.RemoveMenuCommand(rb, "PurchaseCharges");
            if (!Comp.Order && !Comp.Purchase)
                UtilDisplay.RemoveMenuCommand(rb, "CreateOrder");

            dgCrdInvoicesGrid.ShowTotalSummary();
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
            bool showFields = (dgCrdInvoicesGrid.masterRecords == null);
            Account.Visible = showFields;
            Name.Visible = showFields;
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

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;

            var lst = new List<Type>(12) { typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.Employee) };
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

            LoadType(lst);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrdInvoicesGrid.SelectedItem as CreditorInvoiceLocal;
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
                    AddDockItem(TabControls.CreditorInvoicePage2, EditParam, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoices"), selectedItem.Name));
                    break;
                case "InvoiceLine":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.CreditorInvoiceLine, dgCrdInvoicesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), selectedItem.InvoiceNum));
                    break;
                case "ShowInvoice":
                case "ShowPackNote":
                    if (dgCrdInvoicesGrid.SelectedItems == null || dgCrdInvoicesGrid.SelectedItem == null)
                        return;
                    if (ActionType == "ShowInvoice")
                        ShowDocument(true);
                    else
                        ShowDocument(false);
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                    {
                        CWOrderFromInvoice cwOrderInvoice = new CWOrderFromInvoice(api, true);
#if !SILVERLIGHT
                        cwOrderInvoice.DialogTableId = 2000000033;
#endif
                        cwOrderInvoice.Closed += async delegate
                        {
                            if (cwOrderInvoice.DialogResult == true)
                            {
                                var orderApi = new OrderAPI(api);
                                var inversign = cwOrderInvoice.InverSign;
                                var account = cwOrderInvoice.Account;
                                var dcOrder = this.CreateGridObject(typeof(CreditorOrderClient)) as CreditorOrderClient;
                                var copyDelAddress = cwOrderInvoice.copyDeliveryAddress;
                                var reCalPrices = cwOrderInvoice.reCalculatePrices;
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
                case "Trans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PostedTransactions, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem.InvoiceNum));
                    break;
                case "PurchaseCharges":
                    if (selectedItem == null)
                        return;
                    var header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("PurchaseCharges"), selectedItem.InvoiceNum, selectedItem._DCAccount);
                    AddDockItem(TabControls.CreditorOrderCostLinePage, dgCrdInvoicesGrid.syncEntity, header);
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "SendInvoice":
                    if (dgCrdInvoicesGrid.SelectedItem == null || dgCrdInvoicesGrid.SelectedItems == null)
                        return;
                    var selectedInvoiceEmails = dgCrdInvoicesGrid.SelectedItems.Cast<CreditorInvoiceLocal>();
                    SendInvoice(selectedInvoiceEmails);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCrdInvoicesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.InvoiceNum));
                    break;
#if !SILVERLIGHT
                case "SendAsOutlook":
                    if (selectedItem != null)
                        OpenOutLook(selectedItem);
                    break;
#endif
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void SendInvoice(IEnumerable<CreditorInvoiceLocal> invoiceEmails)
        {
            int icount = invoiceEmails.Count();
            UnicontaClient.Pages.CWSendInvoice cwSendInvoice = new UnicontaClient.Pages.CWSendInvoice();
#if !SILVERLIGHT
            cwSendInvoice.DialogTableId = 2000000063;
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
                    foreach (var inv in invoiceEmails)
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

        async private void JournalPosted(CreditorInvoiceLocal selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        bool hasLookups;
        DebtorMessagesClient[] messagesLookup;
        async private void ShowDocument(bool isInvoice)
        {
            try
            {
                var selectedItems = dgCrdInvoicesGrid.SelectedItems.Cast<CreditorInvoiceLocal>();
#if !SILVERLIGHT
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

                var failedPrints = new List<long>();
                var count = selectedItems.Count();
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
                        dockName = string.Concat(Uniconta.ClientTools.Localization.lookup("Preview"), ": ", Uniconta.ClientTools.Localization.lookup("Vendor"), " ", Uniconta.ClientTools.Localization.lookup("Invoices"));
                        reportName = string.Concat(Uniconta.ClientTools.Localization.lookup("Vendor"), Uniconta.ClientTools.Localization.lookup("Invoices"));
                    }
                    else
                    {
                        dockName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), Uniconta.ClientTools.Localization.lookup("CreditorPackNote"));
                        reportName = Uniconta.ClientTools.Localization.lookup("CreditorPackNote");
                    }
                }

#elif SILVERLIGHT
            int top = 200;
            int left = 300;
            int count = selectedItems.Count();
            if (count > 1)
            {
#endif
                foreach (var selected in selectedItems)
                {
#if !SILVERLIGHT
                    IsGeneratingDocument = true;
                    IPrintReport printReport = isInvoice ? await PrintInvoice(selected) : await PrintPackNote(selected);
                    var docNumber = selected.InvoiceNum;
                    if (printReport?.Report != null)
                    {
                        if (count > 1 && IsGeneratingDocument)
                        {
                            ribbonControl.DisableButtons(new string[] { "ShowInvoice", "ShowPackNote" });
                            if (exportAsPdf)
                            {
                                string docName = isInvoice ? Uniconta.ClientTools.Localization.lookup("Invoice") : Uniconta.ClientTools.Localization.lookup("CreditorPackNote");
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

                                Utilities.Utility.ExportReportAsPdf(printReport.Report, directoryPath, docName, docNumber);
                            }
                            else
                            {
                                if (standardPreviewPrintPage == null)
                                    standardPreviewPrintPage = dockCtrl.AddDockItem(api?.CompanyEntity, TabControls.StandardPrintReportPage, ParentControl, new object[] { printReport, reportName }, dockName) as StandardPrintReportPage;
                                else
                                    standardPreviewPrintPage.InsertToMasterReport(printReport.Report);
                            }
                        }
                        else
                        {
                            var docType = isInvoice ? CompanyLayoutType.PurchaseInvoice : CompanyLayoutType.PurchasePacknote;
                            reportName = await Utilities.Utility.GetLocalizedReportName(api, selected, docType);
                            dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", isInvoice ?
                                Uniconta.ClientTools.Localization.lookup("Invoice") : Uniconta.ClientTools.Localization.lookup("CreditorPackNote"), docNumber));

                            AddDockItem(TabControls.StandardPrintReportPage, new object[] { new List<IPrintReport> { printReport }, reportName }, dockName);
                            break;
                        }
                    }
                    else
                        failedPrints.Add(selected.InvoiceNumber);
                }

                IsGeneratingDocument = false;
                ribbonControl.EnableButtons(new string[] { "ShowInvoice", "ShowPackNote" });

                if (failedPrints.Count > 0)
                {
                    var failedList = string.Join(",", failedPrints);
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                }
#elif SILVERLIGHT
                    DefaultPrint(selected, true, new Point(top, left));
                    left = left - left / count;
                    top = top - top / count;
                }
            }
            else
                DefaultPrint(selectedItems.Single());
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("CreditorInvoices.ShowDocument(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
        }


#if SILVERLIGHT
        private void DefaultPrint(CreditorInvoiceLocal creditorInvoiceClient, bool isFloat, Point position)
        {
            object[] ob = new object[2];
            ob[0] = creditorInvoiceClient;
            ob[1] = CompanyLayoutType.PurchaseInvoice;
            AddDockItem(TabControls.ProformaInvoice, ob, isFloat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), creditorInvoiceClient.InvoiceNum), floatingLoc: position);
        }

        private void DefaultPrint(CreditorInvoiceLocal creditorInvoiceClient)
        {
            object[] ob = new object[2];
            ob[0] = creditorInvoiceClient;
            ob[1] = CompanyLayoutType.PurchaseInvoice;
            AddDockItem(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), creditorInvoiceClient.InvoiceNum));
        }
#endif

#if !SILVERLIGHT

        async private void OpenOutLook(CreditorInvoiceLocal invClient)
        {
            try
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
                var creditor = invClient.Creditor;
                var invoiceReport = await PrintInvoice(invClient);
                var documentType = invClient._LineTotal >= -0.0001d ? CompanyLayoutType.PurchaseInvoice : CompanyLayoutType.Creditnote;
                InvoicePostingPrintGenerator.OpenReportInOutlook(api, invoiceReport, invClient, creditor, documentType);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
            finally { busyIndicator.IsBusy = false; }
        }

        bool IsGeneratingDocument;
        StandardPrintReportPage standardPreviewPrintPage;

        private async Task<IPrintReport> PrintInvoice(CreditorInvoiceLocal creditorInvoice)
        {
            var creditorInvoicePrint = new CreditorPrintReport(creditorInvoice, api, CompanyLayoutType.PurchaseInvoice);

            //In case of Multiple Invoices printing
            if (hasLookups)
                await FillLookUps(creditorInvoicePrint);

            var isCreditorInitialized = await creditorInvoicePrint.InstantiateFields();
            if (isCreditorInitialized)
            {
                var creditorStandardInvoice = new CreditorStandardReportClient(creditorInvoicePrint.Company, creditorInvoicePrint.Creditor, creditorInvoicePrint.CreditorInvoice, creditorInvoicePrint.InvTransInvoiceLines, creditorInvoicePrint.CreditorOrder,
                    creditorInvoicePrint.CompanyLogo, creditorInvoicePrint.ReportName, (int)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchaseInvoice, creditorInvoicePrint.CreditorMessage, creditorInvoicePrint.IsCreditNote);

                var creditorStandardReport = new[] { creditorStandardInvoice };
                IPrintReport iprintReport = new StandardPrintReport(api, creditorStandardReport, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchaseInvoice);
                await iprintReport.InitializePrint();
                if (iprintReport?.Report != null)
                    return iprintReport;

                //Call LayoutInvoice
                var layoutPrint = new LayoutPrintReport(api, creditorInvoice, creditorInvoicePrint.IsCreditNote ? CompanyLayoutType.Creditnote : CompanyLayoutType.PurchaseInvoice);
                layoutPrint.SetupLayoutPrintFields(creditorInvoicePrint);
                
                //Setup lookup if has lookup
                if (hasLookups)
                    layoutPrint.SetLookUpForDebtorMessageClients(messagesLookup);

                await layoutPrint.InitializePrint();
                return layoutPrint;
            }
            return null;
        }

        async private Task FillLookUps(CreditorPrintReport creditorInvoicePrint)
        {
            if (messagesLookup == null)
                messagesLookup = await api.Query<DebtorMessagesClient>();

            creditorInvoicePrint.SetLookUpForMessageClient(messagesLookup);
        }

        private async Task<IPrintReport> PrintPackNote(CreditorInvoiceLocal creditorInvoice)
        {
            var packnote = Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchasePackNote;
            var creditorInvoicePrint = new UnicontaClient.Pages.CreditorPrintReport(creditorInvoice, api, CompanyLayoutType.Packnote);

            //Lookups in multi print
            if (hasLookups)
                await FillLookUps(creditorInvoicePrint);

            var isInitializedSuccess = await creditorInvoicePrint.InstantiateFields();

            if (isInitializedSuccess)
            {
                var standardCreditorInvoice = new CreditorStandardReportClient(creditorInvoicePrint.Company, creditorInvoicePrint.Creditor, creditorInvoicePrint.CreditorInvoice, creditorInvoicePrint.InvTransInvoiceLines, creditorInvoicePrint.CreditorOrder,
                    creditorInvoicePrint.CompanyLogo, creditorInvoicePrint.ReportName, (int)packnote, creditorInvoicePrint.CreditorMessage);

                var standardReports = new[] { standardCreditorInvoice };
                IPrintReport iprintReport = new StandardPrintReport(api, standardReports, (byte)packnote);
                await iprintReport.InitializePrint();
                if (iprintReport?.Report != null)
                    return iprintReport;

                //Call LayoutInvoice
                var layoutPrint = new LayoutPrintReport(api, creditorInvoice, CompanyLayoutType.Packnote);
                layoutPrint.SetupLayoutPrintFields(creditorInvoicePrint);

                if (hasLookups)
                    layoutPrint.SetLookUpForDebtorMessageClients(messagesLookup);

                await layoutPrint.InitializePrint();
                return layoutPrint;
            }
            return null;
        }

        public override bool IsDataChaged => IsGeneratingDocument;
#endif
        private void ShowOrderLines(DCOrder order)
        {
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup("PurchaseOrderCreated"), Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Orderline")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(TabControls.CreditorOrderLines, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, order._DCAccount));
                        break;

                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgCrdInvoicesGrid.Filter(propValuePair);
        }

        public async override Task InitQuery()
        {
            await Filter();
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.StandardPrintReportPage)
            {
#if !SILVERLIGHT
                IsGeneratingDocument = false;
                standardPreviewPrintPage = null;
#endif
            }
            if (screenName == TabControls.InvoicePage2)
                dgCrdInvoicesGrid.UpdateItemSource(argument);
        }
    }
}
