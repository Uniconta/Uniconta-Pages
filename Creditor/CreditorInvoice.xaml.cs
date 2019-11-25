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
            var masterClient = dgCrdInvoicesGrid.masterRecord as Uniconta.DataModel.Creditor;
            if (masterClient == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), masterClient._Account);
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
            filterDate = BasePage.GetFilterDate(Comp, master != null);
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
                case "InvoiceLine":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.CreditorInvoiceLine, dgCrdInvoicesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), selectedItem._InvoiceNumber));
                    break;
                case "ShowInvoice":
                    if (dgCrdInvoicesGrid.SelectedItems == null || dgCrdInvoicesGrid.SelectedItem == null)
                        return;
                    ShowInvoice();
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
                        AddDockItem(TabControls.PostedTransactions, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._InvoiceNumber));
                    break;
                case "PurchaseCharges":
                    if (selectedItem == null)
                        return;
                    var header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("PurchaseCharges"), selectedItem._InvoiceNumber, selectedItem._DCAccount);
                    AddDockItem(TabControls.CreditorOrderCostLinePage, dgCrdInvoicesGrid.syncEntity, header);
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "ShowPackNote":
                    if (dgCrdInvoicesGrid.SelectedItem == null || dgCrdInvoicesGrid.SelectedItems == null)
                        return;

                    var items = dgCrdInvoicesGrid.SelectedItems.Cast<CreditorInvoiceLocal>();
                    ShowInvoiceForPackNote(items);
                    break;
                case "SendInvoice":
                    if (dgCrdInvoicesGrid.SelectedItem == null || dgCrdInvoicesGrid.SelectedItems == null)
                        return;
                    var selectedInvoiceEmails = dgCrdInvoicesGrid.SelectedItems.Cast<CreditorInvoiceLocal>();
                    SendInvoice(selectedInvoiceEmails);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCrdInvoicesGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._InvoiceNumber));
                    break;
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

        async private void ShowInvoiceForPackNote(IEnumerable<CreditorInvoiceLocal> creditorInvoices)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
            try
            {
                var invoicesList = creditorInvoices.ToList();
#if !SILVERLIGHT
                List<IPrintReport> packnoteReports = new List<IPrintReport>();
#elif SILVERLIGHT
                        int top = 200;
                        int left = 300;
                        int count = invoicesList.Count;

                        if (count > 1)
                        {
#endif
                foreach (var creditInvoice in invoicesList)
                {
#if !SILVERLIGHT
                    IPrintReport printReport = await PrintPackNote(creditInvoice);
                    if (printReport?.Report != null)
                        packnoteReports.Add(printReport);
                }

                if (packnoteReports.Count > 0)
                {
                    var reportName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("CreditorPackNote"));
                    AddDockItem(TabControls.StandardPrintReportPage, new object[] { packnoteReports, reportName }, reportName);
                }
                else
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("LayoutDoesNotExist"), Uniconta.ClientTools.Localization.lookup("CreditorPackNote")),
                       Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
#elif SILVERLIGHT

                        DefaultPrint(creditInvoice, true, new Point(top, left));
                                left = left - left / count;
                                top = top - top / count;
                            }
                        }
                        else
                            DefaultPrint(creditorInvoices.Single());
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("Invoices.ShowPurchaseSlip(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }
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

        async private void ShowInvoice()
        {
            var selectedItems = dgCrdInvoicesGrid.SelectedItems.Cast<CreditorInvoiceLocal>();
#if !SILVERLIGHT
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            var failedPrints = new List<long>();
            var invoiceReports = new List<IPrintReport>();
            long invNumber = 0;
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
                IPrintReport printReport = await PrintInvoice(selected);
                invNumber = selected._InvoiceNumber;
                if (printReport?.Report != null)
                    invoiceReports.Add(printReport);
                else
                    failedPrints.Add(selected.InvoiceNumber);
            }
            busyIndicator.IsBusy = false;

            if (invoiceReports.Count > 0)
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
                    AddDockItem(TabControls.StandardPrintReportPage, new object[] { invoiceReports, reportsName }, reportsName);
                }

                if (failedPrints.Count > 0)
                {
                    var failedList = string.Join(",", failedPrints);
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FailedPrintmsg") + failedList, Uniconta.ClientTools.Localization.lookup("Error"),
                        MessageBoxButton.OK);
                }
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

#if SILVERLIGHT
        private void DefaultPrint(CreditorInvoiceLocal creditorInvoiceClient, bool isFloat, Point position)
        {
            object[] ob = new object[2];
            ob[0] = creditorInvoiceClient;
            ob[1] = CompanyLayoutType.PurchaseInvoice;
            AddDockItem(TabControls.ProformaInvoice, ob, isFloat, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"),
                creditorInvoiceClient._InvoiceNumber), floatingLoc: position);
        }

        private void DefaultPrint(CreditorInvoiceLocal creditorInvoiceClient)
        {
            object[] ob = new object[2];
            ob[0] = creditorInvoiceClient;
            ob[1] = CompanyLayoutType.PurchaseInvoice;
            AddDockItem(TabControls.ProformaInvoice, ob, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), creditorInvoiceClient._InvoiceNumber));
        }
#endif

#if !SILVERLIGHT

        private async Task<IPrintReport> PrintInvoice(CreditorInvoiceLocal creditorInvoice)
        {
            IPrintReport iprintReport = null;

            var creditorInvoicePrint = new CreditorPrintReport(creditorInvoice, api, CompanyLayoutType.PurchaseInvoice);
            var isCreditorInitialized = await creditorInvoicePrint.InstantiateFields();
            if (isCreditorInitialized)
            {
                var creditorStandardInvoice = new CreditorStandardReportClient(creditorInvoicePrint.Company, creditorInvoicePrint.Creditor, creditorInvoicePrint.CreditorInvoice, creditorInvoicePrint.InvTransInvoiceLines, creditorInvoicePrint.CreditorOrder,
                    creditorInvoicePrint.CompanyLogo, creditorInvoicePrint.ReportName, (int)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchaseInvoice, creditorInvoicePrint.CreditorMessage, creditorInvoicePrint.IsCreditNote);

                var creditorStandardReport = new[] { creditorStandardInvoice };
                iprintReport = new StandardPrintReport(api, creditorStandardReport, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchaseInvoice);
                await iprintReport.InitializePrint();

                if (iprintReport?.Report == null)
                {
                    iprintReport = new LayoutPrintReport(api, creditorInvoice, creditorInvoicePrint.IsCreditNote ? CompanyLayoutType.Creditnote : CompanyLayoutType.Invoice);
                    await iprintReport.InitializePrint();
                }
            }
            return iprintReport;
        }

        private async Task<IPrintReport> PrintPackNote(CreditorInvoiceLocal creditorInvoice)
        {
            IPrintReport iprintReport = null;

            var packnote = Uniconta.ClientTools.Controls.Reporting.StandardReports.PurchasePackNote;

            var creditorInvoicePrint = new UnicontaClient.Pages.CreditorPrintReport(creditorInvoice, api, CompanyLayoutType.Packnote);
            var isInitializedSuccess = await creditorInvoicePrint.InstantiateFields();

            if (isInitializedSuccess)
            {
                var standardCreditorInvoice = new CreditorStandardReportClient(creditorInvoicePrint.Company, creditorInvoicePrint.Creditor, creditorInvoicePrint.CreditorInvoice, creditorInvoicePrint.InvTransInvoiceLines, creditorInvoicePrint.CreditorOrder,
                    creditorInvoicePrint.CompanyLogo, creditorInvoicePrint.ReportName, (int)packnote, creditorInvoicePrint.CreditorMessage);

                var standardReports = new[] { standardCreditorInvoice };
                iprintReport = new StandardPrintReport(api, standardReports, (byte)packnote);
                await iprintReport.InitializePrint();

                if (iprintReport?.Report == null)
                {
                    iprintReport = new LayoutPrintReport(api, creditorInvoice, CompanyLayoutType.Packnote);
                    await iprintReport.InitializePrint();
                }
            }

            return iprintReport;
        }
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
            var api = this.api;
            if (api.CompanyEntity.DeliveryAddress)
            {
                var lst = dgCrdInvoicesGrid.ItemsSource as IEnumerable<CreditorInvoiceLocal>;
                if (lst != null)
                {
                    foreach (var rec in lst)
                    {
                        DebtorOrders.SetDeliveryAdress(rec, rec.Creditor, api);
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
