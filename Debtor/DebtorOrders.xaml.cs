using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
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
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.API.DebtorCreditor;
using System.Windows.Threading;
using System.IO;
using Uniconta.ClientTools.Util;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.System;
using Uniconta.API.Service;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
#if !SILVERLIGHT
using UnicontaClient.Pages;
using FromXSDFile.OIOUBL.ExportImport;
using ubl_norway_uniconta;
using UBL.Iceland;
using Microsoft.Win32;
#endif
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorOrdersGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(DebtorOrderClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }
    }
    public partial class DebtorOrders : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorOrders; } }
        private SynchronizeEntity syncEntity;
        public DebtorOrders(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init(null);
        }

        public DebtorOrders(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public DebtorOrders(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            if (syncEntity != null)
                Init(syncEntity.Row);
            SetHeader();
        }

        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties orderNoSort = new SortingProperties("OrderNumber");
            orderNoSort.Ascending = false;
            return new SortingProperties[] { orderNoSort };
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDebtorOrdersGrid.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }
        void SetHeader()
        {
            var syncMaster = dgDebtorOrdersGrid.masterRecord as Uniconta.DataModel.Debtor;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorOrders"), syncMaster._Account);
            else
            {
                var projectMaster = dgDebtorOrdersGrid.masterRecord as Uniconta.DataModel.Project;
                if (projectMaster != null)
                    header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("DebtorOrders"), projectMaster._DCAccount);
            }
            if (header != null)
                SetHeader(header);
        }
        public DebtorOrders(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
            if (syncEntity != null)
                dgDebtorOrdersGrid.UpdateMaster(master);

        }
        private void Init(UnicontaBaseEntity master)
        {
            LoadNow(typeof(DebtorOrderGroup));
            InitializeComponent();
            dgDebtorOrdersGrid.UpdateMaster(master);
            dgDebtorOrdersGrid.RowDoubleClick += dgDebtorOrdersGrid_RowDoubleClick;
            dgDebtorOrdersGrid.api = api;
            dgDebtorOrdersGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgDebtorOrdersGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorOrdersGrid.ShowTotalSummary();
            ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
            dgDebtorOrdersGrid.CustomSummary += dgDebtorOrdersGrid_CustomSummary;
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void dgDebtorOrdersGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as DebtorOrderClient;
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
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgDebtorOrdersGrid.masterRecords == null);
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
            }
            dgDebtorOrdersGrid.Readonly = true;
            if (!Comp.ApproveSalesOrders)
                Approver.ShowInColumnChooser = Approved.ShowInColumnChooser = ApprovedDate.ShowInColumnChooser = false;
            else
                Approver.ShowInColumnChooser = Approved.ShowInColumnChooser = ApprovedDate.ShowInColumnChooser = true;
            if (!Comp.Project)
                Project.ShowInColumnChooser = Project.Visible = PrCategory.ShowInColumnChooser = PrCategory.Visible = Task.ShowInColumnChooser = Task.Visible =
                    WorkSpace.ShowInColumnChooser = WorkSpace.Visible = false;
            else
                Project.ShowInColumnChooser = PrCategory.ShowInColumnChooser = Task.ShowInColumnChooser = true;
            if (!Comp.ProjectTask)
                Task.ShowInColumnChooser = Task.Visible = false;
            else
                Task.ShowInColumnChooser = Task.Visible = true;
        }

        void dgDebtorOrdersGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("OrderLine");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            string header;
            var dgDebtorOrdersGrid = this.dgDebtorOrdersGrid;
            var selectedItem = dgDebtorOrdersGrid.SelectedItem as DebtorOrderClient;
            switch (ActionType)
            {
                case "AddRow":
                    if (dgDebtorOrdersGrid.masterRecords != null)
                        AddDockItem(TabControls.DebtorOrdersPage2, new object[] { api, dgDebtorOrdersGrid.masterRecord }, Uniconta.ClientTools.Localization.lookup("Orders"), "Add_16x16.png");
                    else
                        AddDockItem(TabControls.DebtorOrdersPage2, api, Uniconta.ClientTools.Localization.lookup("Orders"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
                    if (dgDebtorOrdersGrid.masterRecords != null)
                        AddDockItem(TabControls.DebtorOrdersPage2, new object[] { selectedItem, dgDebtorOrdersGrid.masterRecord }, header);
                    else
                        AddDockItem(TabControls.DebtorOrdersPage2, selectedItem, header);
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), selectedItem._OrderNumber, selectedItem.Name);
                    AddDockItem(TabControls.DebtorOrderLines, dgDebtorOrdersGrid.syncEntity, header);
                    break;
                case "Invoices":
                    if (selectedItem != null)
                    {
                        header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
                        AddDockItem(TabControls.Invoices, selectedItem, header);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem._OrderNumber);
                        AddDockItem(TabControls.UserNotesPage, dgDebtorOrdersGrid.syncEntity, header);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._OrderNumber);
                        AddDockItem(TabControls.UserDocsPage, dgDebtorOrdersGrid.syncEntity, header);
                    }
                    break;
                case "Contacts":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.ContactPage, selectedItem);
                    break;
                case "OrderConfirmation":
                    if (selectedItem != null)
                        OrderConfirmation(selectedItem, CompanyLayoutType.OrderConfirmation);
                    break;
                case "PackNote":
                    if (selectedItem != null)
                        OrderConfirmation(selectedItem, CompanyLayoutType.Packnote);
                    break;
                case "PickList":
                    if (selectedItem != null)
                        PickingListReport(selectedItem);
                    break;
                case "EditDebtor":
                    if (selectedItem?._DCAccount != null)
                        jumpToDebtor(selectedItem);
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                    {
                        CWOrderFromOrder cwOrderFromOrder = new CWOrderFromOrder(api);
#if !SILVERLIGHT
                        cwOrderFromOrder.DialogTableId = 2000000020;
#endif
                        cwOrderFromOrder.Closed += async delegate
                        {
                            if (cwOrderFromOrder.DialogResult == true)
                            {
                                var perSupplier = cwOrderFromOrder.orderPerPurchaseAccount;
                                if (!perSupplier && string.IsNullOrEmpty(cwOrderFromOrder.Account))
                                    return;
                                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                                busyIndicator.IsBusy = true;
                                var orderApi = new OrderAPI(api);
                                var inversign = cwOrderFromOrder.InverSign;
                                var account = cwOrderFromOrder.Account;
                                var copyAttachment = cwOrderFromOrder.copyAttachment;
                                var dcOrder = cwOrderFromOrder.dcOrder;
                                dcOrder._DeliveryDate = cwOrderFromOrder.DeliveryDate;
                                var copyDelAddress = cwOrderFromOrder.copyDeliveryAddress;
                                var reCalPrice = cwOrderFromOrder.reCalculatePrice;
                                var onlyItemsWthSupp = cwOrderFromOrder.onlyItemsWithSupplier;
                                var result = await orderApi.CreateOrderFromOrder(selectedItem, dcOrder, account, inversign, CopyAttachments: copyAttachment, CopyDeliveryAddress: copyDelAddress, RecalculatePrices: reCalPrice, OrderPerPurchaseAccount: perSupplier, OnlyItemsWithSupplier: onlyItemsWthSupp);
                                busyIndicator.IsBusy = false;
                                if (result != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(result);
                                else
                                    CreditorOrders.ShowOrderLines(1, dcOrder, this, dgDebtorOrdersGrid);
                            }
                        };
                        cwOrderFromOrder.Show();
                    }
                    break;
                case "EditAll":
                    if (dgDebtorOrdersGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddLine":
                    dgDebtorOrdersGrid.AddRow();
                    break;
                case "CopyRow":
                    selectedItem = dgDebtorOrdersGrid.CopyRow() as DebtorOrderClient;
                    selectedItem.OrderNumber = 0;
                    break;
                case "DeleteRow":
                    dgDebtorOrdersGrid.DeleteRow();
                    break;
                case "UndoDelete":
                    dgDebtorOrdersGrid.UndoDeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "CreateInvoice":
                    if (selectedItem != null)
                    {
                        if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                            GenerateInvoice(selectedItem);
                        else
                            UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    }
                    break;
                case "ProjectTransaction":
                    if (selectedItem?._Project != null)
                        AddDockItem(TabControls.DebtorOrderProjectLinePage, dgDebtorOrdersGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectAdjustments"), selectedItem._OrderNumber));
                    break;
                case "RefreshGrid":
                    TestDebtorReload(true, dgDebtorOrdersGrid.ItemsSource as IEnumerable<DebtorOrder>);
                    break;
                case "RegenerateOrderFromProject":
                    if (selectedItem != null)
                        AddDockItem(TabControls.RegenerateOrderFromProjectPage, dgDebtorOrdersGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("RegenerateOrder"), selectedItem._OrderNumber));
                    break;
                case "ApproveOrder":
                    if (selectedItem != null && api.CompanyEntity.ApproveSalesOrders)
                        Utility.ApproveOrder(api, selectedItem);
                    break;
                case "PostProjectOrder":
                    if (string.IsNullOrEmpty(selectedItem._Project))
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ProjectCannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Message"));
                        return;
                    }
                    PostProjectOrder(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void PostProjectOrder(DebtorOrderClient order)
        {
            var dialog = new CwPostProjectOrder();
            dialog.DialogTableId = 2000000087;
            dialog.Closed += async delegate
            {
                if (dialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var invApi = new Uniconta.API.DebtorCreditor.InvoiceAPI(api);
                    var postingResult = await invApi.PostProjectOrder(order, null, dialog.Date, dialog.Simulation, new GLTransClientTotal(), null, dialog.PostOnlyDelivered);
                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    var ledgerRes = postingResult.ledgerRes;
                    if (ledgerRes == null)
                        return;
                    if (ledgerRes.Err != ErrorCodes.Succes)
                        Utility.ShowJournalError(ledgerRes, dgDebtorOrdersGrid, false);
                    else if (dialog.Simulation && ledgerRes.SimulatedTrans != null && ledgerRes.SimulatedTrans.Length > 0)
                        AddDockItem(TabControls.SimulatedTransactions, ledgerRes.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                    else
                    {
                        string msg;
                        if (ledgerRes.JournalPostedlId != 0)
                            msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), ledgerRes.JournalPostedlId);
                        else
                            msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                    }
                }
            };
            dialog.Show();
        }

        async void TestDebtorReload(bool refresh, IEnumerable<DebtorOrder> lst)
        {
            bool reload = false;
            if (lst != null && lst.Count() > 0)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
                if (cache == null)
                    return;

                var Contacts = api.GetCache(typeof(Uniconta.DataModel.Contact));
                foreach (var rec in lst)
                {
                    if (rec._DCAccount != null && cache.Get(rec._DCAccount) == null)
                    {
                        reload = true;
                        break;
                    }
                    if (rec._ContactRef != 0 && Contacts != null && Contacts.Get(rec._ContactRef) == null)
                    {
                        Contacts = null;
                        api.LoadCache(typeof(Uniconta.DataModel.Contact), true);
                    }
                }
                if (reload)
                    await api.LoadCache(typeof(Uniconta.DataModel.Debtor), true);
            }
            if (refresh)
                gridRibbon_BaseActions("RefreshGrid");
        }

        async void jumpToDebtor(DebtorOrderClient selectedItem)
        {
            var dc = selectedItem.Debtor;
            if (dc == null)
            {
                await api.CompanyEntity.LoadCache(typeof(Debtor), api, true);
                dc = selectedItem.Debtor;
                if (dc == null)
                    return;
            }
            var param = new object[2] { dc, true };
            AddDockItem(TabControls.DebtorAccountPage2, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorAccount"), dc._Account));
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var iBase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (iBase == null) return;

            if (dgDebtorOrdersGrid.Readonly)
            {
                dgDebtorOrdersGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgDebtorOrdersGrid);
                iBase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                editAllChecked = false;
            }
            else
            {
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                     {
                         if (confirmationDialog.DialogResult == null)
                             return;

                         switch (confirmationDialog.ConfirmationResult)
                         {
                             case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                 await dgDebtorOrdersGrid.SaveData();
                                 break;
                             case CWConfirmationBox.ConfirmationResultEnum.No:
                                 break;
                         }
                         editAllChecked = true;
                         dgDebtorOrdersGrid.Readonly = true;
                         dgDebtorOrdersGrid.tableView.CloseEditor();
                         iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                         ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                     };
                    confirmationDialog.Show();
                }
                else
                {
                    dgDebtorOrdersGrid.Readonly = true;
                    dgDebtorOrdersGrid.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                }
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgDebtorOrdersGrid.HasUnsavedData;
            }
        }

        private void Save()
        {
            dgDebtorOrdersGrid.SaveData();
        }

        static public void Updatedata(DCOrder dbOrder, CompanyLayoutType doctype)
        {
            string prop;
            if (doctype == CompanyLayoutType.Packnote)
            {
                dbOrder._PackNotePrinted = BasePage.GetSystemDefaultDate();
                prop = "PackNotePrinted";
            }
            else if (doctype == CompanyLayoutType.OrderConfirmation)
            {
                dbOrder._ConfirmPrinted = BasePage.GetSystemDefaultDate();
                prop = "ConfirmPrinted";
            }
            else if (doctype == CompanyLayoutType.PickingList)
            {
                dbOrder._PicklistPrinted = BasePage.GetSystemDefaultDate();
                prop = "PicklistPrinted";
            }
            else
                return;
            dbOrder.NotifyPropertyChanged(prop);
            //we just update client, since server update was done in posting.
        }

        static bool showPrintPreview = true;
        private void OrderConfirmation(DebtorOrderClient dbOrder, CompanyLayoutType doctype)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var debtor = dbOrder.Debtor;
            bool showSendByMail = true;
            if (debtor != null)
                showSendByMail = (!string.IsNullOrEmpty(debtor.InvoiceEmail) || debtor.EmailDocuments);
            string debtorName = debtor?._Name ?? dbOrder._DCAccount;
            bool showUpdateInv = api.CompanyEntity.Storage || (doctype == CompanyLayoutType.Packnote && api.CompanyEntity.Packnote);
            var accountName = string.Format("{0} ({1})", dbOrder._DCAccount, dbOrder.Name);
            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, doctype.ToString(), isShowInvoiceVisible: true, askForEmail: true, showNoEmailMsg: !showSendByMail, debtorName: debtorName,
                isShowUpdateInv: showUpdateInv, isDebtorOrder: true, AccountName: accountName);
#if !SILVERLIGHT
            if (doctype == CompanyLayoutType.OrderConfirmation)
                GenrateOfferDialog.DialogTableId = 2000000009;
            else if (doctype == CompanyLayoutType.Packnote)
                GenrateOfferDialog.DialogTableId = 2000000018;
#endif
            GenrateOfferDialog.SetInvPrintPreview(showPrintPreview);
            var additionalOrdersList = Utility.GetAdditionalOrders(api, dbOrder);
            if (additionalOrdersList != null)
                GenrateOfferDialog.SetAdditionalOrders(additionalOrdersList);
            GenrateOfferDialog.Closed += async delegate
            {
                if (GenrateOfferDialog.DialogResult == true)
                {
                    showPrintPreview = GenrateOfferDialog.ShowInvoice || GenrateOfferDialog.InvoiceQuickPrint || GenrateOfferDialog.SendByOutlook;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    var openOutlook = doctype == CompanyLayoutType.Packnote ? GenrateOfferDialog.UpdateInventory && GenrateOfferDialog.SendByOutlook : GenrateOfferDialog.SendByOutlook;
                    invoicePostingResult.SetUpInvoicePosting(dbOrder, null, doctype, GenrateOfferDialog.GenrateDate, null, !GenrateOfferDialog.UpdateInventory, GenrateOfferDialog.ShowInvoice, GenrateOfferDialog.PostOnlyDelivered,
                        GenrateOfferDialog.InvoiceQuickPrint, GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, openOutlook, GenrateOfferDialog.sendOnlyToThisEmail, GenrateOfferDialog.Emails,
                        false, null, false);
                    invoicePostingResult.SetAdditionalOrders(GenrateOfferDialog.AdditionalOrders?.Cast<DCOrder>().ToList());
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                        Updatedata(dbOrder, doctype);
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOrdersGrid);
                }
            };
            GenrateOfferDialog.Show();
        }

        private void PickingListReport(DebtorOrderClient dbOrder)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var debtor = dbOrder.Debtor;
            string debtorName = string.Empty, accountName = string.Empty;
            bool showSendByMail = false;

            if (debtor != null)
            {
                debtorName = debtor._Name ?? dbOrder._DCAccount;
                accountName = string.Format("{0} ({1})", dbOrder._DCAccount, dbOrder.Name);
                showSendByMail = !string.IsNullOrEmpty(debtor.InvoiceEmail) || debtor.EmailDocuments;
            }

#if !SILVERLIGHT
            var cwPickingList = new CWGeneratePickingList(accountName, true, true, debtorName, showSendByMail);
            cwPickingList.DialogTableId = 2000000049;
#else
            var cwPickingList = new CWGeneratePickingList();
#endif
            cwPickingList.Closed += async delegate
             {
                 if (cwPickingList.DialogResult == true)
                 {
                     var selectedDate = cwPickingList.SelectedDate;

#if !SILVERLIGHT
                     var printDoc = cwPickingList.PrintDocument;
#else
                     var printDoc = false;
#endif
                     var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                     invoicePostingResult.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.PickingList, selectedDate, null, false, cwPickingList.ShowDocument, false, printDoc, cwPickingList.NumberOfPages,
                        cwPickingList.SendByEmail, cwPickingList.SendByOutlook, cwPickingList.sendOnlyToThisEmail, cwPickingList.EmailList, false, null, false);

                     busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                     busyIndicator.IsBusy = true;
                     var result = await invoicePostingResult.Execute();
                     busyIndicator.IsBusy = false;

                     if (result)
                         Updatedata(dbOrder, CompanyLayoutType.PickingList);
                     else
                         Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOrdersGrid);
                 }
             };
            cwPickingList.Show();
        }

        static public void SetGLNnumber(DCInvoice InvClient, DCAccount client, QueryAPI api)
        {
            var installation = (WorkInstallation)api.GetCache(typeof(WorkInstallation))?.Get(InvClient._Installation);
            var dctype = InvClient.__DCType();
            var dccache = api.GetCache((dctype == 2 || dctype == 9) ? typeof(Uniconta.DataModel.Creditor) : typeof(Uniconta.DataModel.Debtor));
            var debtor = client ?? (DCAccount)dccache?.Get(InvClient._DCAccount);
            var contact = (Contact)api.GetCache(typeof(Contact))?.Get(InvClient._ContactRef);

            InvClient._EAN = installation?._GLN ?? contact?._EAN ?? debtor?._EAN;
        }

        static public void SetDeliveryAdress(DCInvoice InvClient, DCAccount client, QueryAPI api)
        {
            SetGLNnumber(InvClient, client, api);

            if (InvClient._DeliveryAddress1 != null)
                return;

            if (InvClient._Installation != null)
            {
                var wInsCache = api.GetCache(typeof(WorkInstallation)) ?? api.LoadCache(typeof(WorkInstallation)).GetAwaiter().GetResult();
                var installation = (WorkInstallation)wInsCache.Get(InvClient._Installation);
                if (installation == null)
                {
                    wInsCache = api.LoadCache(typeof(WorkInstallation), true).GetAwaiter().GetResult();
                    installation = (WorkInstallation)wInsCache.Get(InvClient._Installation);
                }
                if (installation != null)
                {
                    InvClient._DeliveryName = installation._Name;
                    InvClient._DeliveryAddress1 = installation._Address1;
                    InvClient._DeliveryAddress2 = installation._Address2;
                    InvClient._DeliveryAddress3 = installation._Address3;
                    InvClient._DeliveryZipCode = installation._ZipCode;
                    InvClient._DeliveryCity = installation._City;
                    if (api.CompanyEntity._Country != (byte)installation._Country)
                        InvClient._DeliveryCountry = installation._Country;
                }
            }
            else
            {
                DCAccount deb = null;
                string ac;
                bool UseDebAddress;
                if (InvClient._DeliveryAccount != null)
                {
                    ac = InvClient._DeliveryAccount;
                    UseDebAddress = true;
                }
                else
                {
                    UseDebAddress = false;
                    if (client != null)
                    {
                        deb = client;
                        ac = null;
                    }
                    else
                        ac = InvClient._DCAccount;
                }
                if (ac != null)
                {
                    var dctype = InvClient.__DCType();
                    var t = (dctype == 2 || dctype == 9) ? typeof(Uniconta.DataModel.Creditor) : typeof(Uniconta.DataModel.Debtor);
                    var dcCache = api.GetCache(t) ?? api.LoadCache(t).GetAwaiter().GetResult();
                    deb = (DCAccount)dcCache.Get(ac);
                    if (deb == null)
                    {
                        dcCache = api.LoadCache(t, true).GetAwaiter().GetResult();
                        deb = (DCAccount)dcCache.Get(ac);
                    }
                }
                if (deb != null)
                {
                    if (deb._DeliveryAddress1 != null)
                    {
                        InvClient._DeliveryName = deb._DeliveryName;
                        InvClient._DeliveryAddress1 = deb._DeliveryAddress1;
                        InvClient._DeliveryAddress2 = deb._DeliveryAddress2;
                        InvClient._DeliveryAddress3 = deb._DeliveryAddress3;
                        InvClient._DeliveryZipCode = deb._DeliveryZipCode;
                        InvClient._DeliveryCity = deb._DeliveryCity;
                        InvClient._DeliveryCountry = deb._DeliveryCountry;
                    }
                    else if (UseDebAddress)
                    {
                        InvClient._DeliveryName = deb._Name;
                        InvClient._DeliveryAddress1 = deb._Address1;
                        InvClient._DeliveryAddress2 = deb._Address2;
                        InvClient._DeliveryAddress3 = deb._Address3;
                        InvClient._DeliveryZipCode = deb._ZipCode;
                        InvClient._DeliveryCity = deb._City;
                        if (api.CompanyEntity._Country != (byte)deb._Country)
                            InvClient._DeliveryCountry = deb._Country;
                    }
                }
            }
        }

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorOrdersPage2)
                dgDebtorOrdersGrid.UpdateItemSource(argument);
            else if (screenName == TabControls.DebtorOrderLines)
            {
                var Debtorder = argument as DebtorOrderClient;
                if (Debtorder == null)
                {
                    var orderDeletedargs = argument as object[];
                    if (orderDeletedargs != null && orderDeletedargs.Length == 2)
                    {
                        var dborder = orderDeletedargs[0] as DebtorOrderClient;
                        bool isDeleted = (bool)orderDeletedargs[1];
                        if (dborder != null && isDeleted)
                            dgDebtorOrdersGrid.UpdateItemSource(3, dborder);
                    }
                    return;
                }
                var err = await api.Read(Debtorder);
                if (err == ErrorCodes.CouldNotFind)
                    dgDebtorOrdersGrid.UpdateItemSource(3, Debtorder);
                else if (err == ErrorCodes.Succes)
                    dgDebtorOrdersGrid.UpdateItemSource(2, Debtorder);
            }
        }

        private Task BindGrid()
        {
            return dgDebtorOrdersGrid.Filter(null);
        }

        protected override void LoadCacheInBackGround()
        {
            var orders = api.GetCache(typeof(Uniconta.DataModel.DebtorOrder));
            TestDebtorReload(false, orders?.GetNotNullArray as IEnumerable<DebtorOrder>);

            var Comp = api.CompanyEntity;
            var lst = new List<Type>(20) { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.GLVat) };
            if (Comp.Contacts)
                lst.Add(typeof(Uniconta.DataModel.Contact));
            if (Comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));
            if (Comp.InvPrice)
                lst.Add(typeof(Uniconta.DataModel.DebtorPriceList));
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
                lst.Add(typeof(Uniconta.DataModel.InvStandardVariant));
            }
            if (Comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
            lst.Add(typeof(Uniconta.DataModel.InvGroup));
            if (Comp.NumberOfDimensions >= 1)
                lst.Add(typeof(Uniconta.DataModel.GLDimType1));
            if (Comp.NumberOfDimensions >= 2)
                lst.Add(typeof(Uniconta.DataModel.GLDimType2));
            if (Comp.NumberOfDimensions >= 3)
                lst.Add(typeof(Uniconta.DataModel.GLDimType3));
            if (Comp.NumberOfDimensions >= 4)
                lst.Add(typeof(Uniconta.DataModel.GLDimType4));
            if (Comp.NumberOfDimensions >= 5)
                lst.Add(typeof(Uniconta.DataModel.GLDimType5));
            lst.Add(typeof(Uniconta.DataModel.InvItem));
            LoadType(lst);
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void GenerateInvoice(DebtorOrderClient dbOrder)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            bool showSendByMail = false;

            var debtor = ClientHelper.GetRef(dbOrder.CompanyId, typeof(Debtor), dbOrder._DCAccount) as Debtor;
            if (debtor != null)
            {
                var InvoiceAccount = dbOrder._InvoiceAccount ?? debtor._InvoiceAccount;
                if (InvoiceAccount != null)
                    debtor = ClientHelper.GetRef(dbOrder.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                if (debtor != null)
                {
                    if (debtor._PricesInclVat != dbOrder._PricesInclVat)
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }
                    if (!api.CompanyEntity.SameCurrency(dbOrder._Currency, debtor._Currency))
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("CurrencyMismatch"), AppEnums.Currencies.ToString((int)debtor._Currency), dbOrder.Currency),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }
                    showSendByMail = (!string.IsNullOrEmpty(debtor._InvoiceEmail) || debtor._EmailDocuments);
                }
            }
            else
                api.LoadCache(typeof(Debtor), true);

            string debtorName = debtor?._Name ?? dbOrder._DCAccount;
            bool invoiceInXML = debtor?._InvoiceInXML ?? false;
            var accountName = string.Format("{0} ({1})", dbOrder._DCAccount, dbOrder.Name);
            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(true, string.Empty, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isOrderOrQuickInv: true, isDebtorOrder: true,
                InvoiceInXML: invoiceInXML, AccountName: accountName);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000010;
#endif
            if (dbOrder._InvoiceDate != DateTime.MinValue)
                GenrateInvoiceDialog.SetInvoiceDate(dbOrder._InvoiceDate);
            var additionalOrdersList = Utility.GetAdditionalOrders(api, dbOrder);
            if (additionalOrdersList != null)
                GenrateInvoiceDialog.SetAdditionalOrders(additionalOrdersList);
            GenrateInvoiceDialog.SetOIOUBLLabelText(api.CompanyEntity._OIOUBLSendOnServer);

            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var isSimulated = GenrateInvoiceDialog.IsSimulation;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.Invoice, GenrateInvoiceDialog.GenrateDate, null, isSimulated, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.SendByOutlook, GenrateInvoiceDialog.sendOnlyToThisEmail,
                        GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.GenerateOIOUBLClicked, null, false);
                    invoicePostingResult.SetAdditionalOrders(GenrateInvoiceDialog.AdditionalOrders?.Cast<DCOrder>().ToList());
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        if (invoicePostingResult.PostingResult.OrderDeleted)
                            dgDebtorOrdersGrid.UpdateItemSource(3, dgDebtorOrdersGrid.SelectedItem as DebtorOrderClient);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOrdersGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var order = (sender as Image).Tag as DebtorOrderClient;
            if (order != null)
                AddDockItem(TabControls.UserDocsPage, dgDebtorOrdersGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var order = (sender as Image).Tag as DebtorOrderClient;
            if (order != null)
                AddDockItem(TabControls.UserNotesPage, dgDebtorOrdersGrid.syncEntity);
        }

#if !SILVERLIGHT
        static public async void GenerateOIOXml(CrudAPI api, InvoicePostingResult res)
        {
            var Comp = api.CompanyEntity;
            var InvCache = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem));
            var VatCache = api.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVat));

            //SystemInfo.Visible = true;

            int countErr = 0;
            SaveFileDialog saveDialog = null;
            InvoiceAPI Invapi = new InvoiceAPI(api);

            var invClient = (DebtorInvoiceClient)res.Header;

            var Debcache = Comp.GetCache(typeof(Debtor)) ?? await api.LoadCache(typeof(Debtor));
            var debtor = (Debtor)Debcache.Get(invClient._DCAccount);

            var layoutGroupCache = api.GetCache(typeof(DebtorLayoutGroup)) ?? await api.LoadCache(typeof(DebtorLayoutGroup));

            var invoiceLines = (InvTransClient[])res.Lines;

            Contact contactPerson = null;
            if (invClient._ContactRef != 0)
            {
                var Contacts = api.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact));
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
                var workInstallCache = api.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await api.LoadCache(typeof(Uniconta.DataModel.WorkInstallation));
                workInstallation = (WorkInstallation)workInstallCache.Get(invClient._Installation);
            }

            CreationResult result;

            if (Comp._CountryId == CountryCode.Norway || Comp._CountryId == CountryCode.Netherlands)
                result = EHF.GenerateEHFXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, null, contactPerson);
            else if (Comp._CountryId == CountryCode.Iceland)
            {
                var paymFormatCache = Comp.GetCache(typeof(DebtorPaymentFormatClientIceland)) ?? await api.LoadCache(typeof(DebtorPaymentFormatClientIceland));
                TableAddOnData[] attachments = await UBL.Iceland.Attachments.CollectInvoiceAttachments(invClient, api);
                result = TS136137.GenerateTS136137XML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, null, contactPerson, paymFormatCache, attachments);
            }
            else
            {
                var attachments = await FromXSDFile.OIOUBL.ExportImport.Attachments.CollectInvoiceAttachments(invClient, api);
                result = Uniconta.API.DebtorCreditor.OIOUBL.GenerateOioXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, null, contactPerson, attachments, layoutGroupCache, workInstallation);
            }

            bool createXmlFile = true;

            var errorInfo = "";
            if (result.HasErrors)
            {
                countErr++;
                createXmlFile = false;
                foreach (FromXSDFile.OIOUBL.ExportImport.PrecheckError error in result.PrecheckErrors)
                {
                    errorInfo += error.ToString() + "\n";
                }
            }

            var applFilePath = string.Empty;
            bool hasUserFolder = false;
            if (result.Document != null && createXmlFile)
            {
                var filename = string.Format("{0}_{1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invClient.InvoiceNumber);
                try
                {
                    if (session.User._AppDocPath != string.Empty && Directory.Exists(session.User._AppDocPath))
                    {
                        applFilePath = string.Concat(session.User._AppDocPath, "\\OIOUBL");
                        Directory.CreateDirectory(applFilePath);

                        filename = string.Concat(applFilePath, "\\", filename, ".xml");
                        hasUserFolder = true;
                    }
                    else
                    {
                        saveDialog = UtilDisplay.LoadSaveFileDialog;
                        saveDialog.FileName = filename;
                        saveDialog.Filter = "XML-File | *.xml";
                        bool? dialogResult = saveDialog.ShowDialog();
                        if (dialogResult != true)
                            return;

                        filename = saveDialog.FileName;
                    }

                    result.Document.Save(filename);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                }
                Invapi.MarkSendInvoiceOIO(invClient);

                if (hasUserFolder)
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SaveFileMsgOBJ"), 1, Uniconta.ClientTools.Localization.lookup("Invoice"), applFilePath)
                        , Uniconta.ClientTools.Localization.lookup("Information"));
            }

            if (countErr != 0 && !string.IsNullOrWhiteSpace(errorInfo))
                UnicontaMessageBox.Show(errorInfo, Uniconta.ClientTools.Localization.lookup("Error"));
        }
#endif
    }
}
