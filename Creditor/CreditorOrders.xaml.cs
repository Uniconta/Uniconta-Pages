using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.DataModel;
using System.Collections;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.Service;
using System.IO;
using Uniconta.Common.Utility;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorOrdersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorOrderClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class CreditorOrders : GridBasePage
    {
        SQLCache creditorCache;
        public override string NameOfControl
        {
            get { return TabControls.CreditorOrders.ToString(); }
        }

        public CreditorOrders(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init(null);
        }

        public CreditorOrders(BaseAPI API)
            : base(API, string.Empty)
        {
            Init(null);
        }
        public CreditorOrders(SynchronizeEntity syncEntity)
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
            dgCreditorOrdersGrid.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }
        void SetHeader()
        {
            var syncMaster = dgCreditorOrdersGrid.masterRecord as Uniconta.DataModel.Creditor;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorOrders"), syncMaster._Account);
            SetHeader(header);
        }
        public CreditorOrders(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }
        void Init(UnicontaBaseEntity master)
        {
            LoadNow(typeof(Uniconta.DataModel.CreditorOrderGroup));
            InitializeComponent();
            dgCreditorOrdersGrid.UpdateMaster(master);
            dgCreditorOrdersGrid.RowDoubleClick += dgCreditorOrdersGrid_RowDoubleClick;
            localMenu.dataGrid = dgCreditorOrdersGrid;
            dgCreditorOrdersGrid.api = api;
            dgCreditorOrdersGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCreditorOrdersGrid);

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
            creditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor));
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgCreditorOrdersGrid.masterRecords == null);
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
            dgCreditorOrdersGrid.Readonly = true;
            if (!Comp.ApprovePurchaseOrders)
                Approver.ShowInColumnChooser = Approved.ShowInColumnChooser = ApprovedDate.ShowInColumnChooser = false;
            else
                Approver.ShowInColumnChooser = Approved.ShowInColumnChooser = ApprovedDate.ShowInColumnChooser = true;
            if (!Comp.Project)
                Project.ShowInColumnChooser = Project.Visible = ProjectName.ShowInColumnChooser = ProjectName.Visible =
                    PrCategory.ShowInColumnChooser = PrCategory.Visible = CategoryName.ShowInColumnChooser = CategoryName.Visible =
                    Task.ShowInColumnChooser = Task.Visible = WorkSpace.ShowInColumnChooser = WorkSpace.Visible = false;
            else
                Project.ShowInColumnChooser = ProjectName.ShowInColumnChooser =
                       PrCategory.ShowInColumnChooser = CategoryName.ShowInColumnChooser =
                       Task.ShowInColumnChooser = true;
            if (!Comp.ProjectTask)
                Task.ShowInColumnChooser = Task.Visible = false;
            else
                Task.ShowInColumnChooser = true;
        }

        void dgCreditorOrdersGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("OrderLine");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgCreditorOrdersGrid_RowDoubleClick();
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            string header;
            var dgCreditorOrdersGrid = this.dgCreditorOrdersGrid;
            var selectedItem = dgCreditorOrdersGrid.SelectedItem as CreditorOrderClient;
            switch (ActionType)
            {
                case "AddRow":
                    if (dgCreditorOrdersGrid.masterRecords != null)
                        AddDockItem(TabControls.CreditorOrdersPage2, new object[] { api, dgCreditorOrdersGrid.masterRecord }, Uniconta.ClientTools.Localization.lookup("Orders"), "Add_16x16");
                    else
                        AddDockItem(TabControls.CreditorOrdersPage2, api, Uniconta.ClientTools.Localization.lookup("Orders"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber, true);
                    if (dgCreditorOrdersGrid.masterRecords != null)
                        AddDockItem(TabControls.CreditorOrdersPage2, new object[] { selectedItem, dgCreditorOrdersGrid.masterRecord }, header);
                    else
                        AddDockItem(TabControls.CreditorOrdersPage2, selectedItem, header);
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem._OrderNumber, selectedItem._DCAccount);
                    AddDockItem(TabControls.CreditorOrderLines, dgCreditorOrdersGrid.syncEntity, header);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem.Account);
                        AddDockItem(TabControls.UserNotesPage, dgCreditorOrdersGrid.syncEntity, header);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.Account);
                        AddDockItem(TabControls.UserDocsPage, dgCreditorOrdersGrid.syncEntity, header);
                    }
                    break;
                case "Contacts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ContactPage, selectedItem);
                    break;
                case "UpdateRequisition":
                    if (selectedItem != null)
                        OrderConfirmation(selectedItem, CompanyLayoutType.Requisition);
                    break;
                case "UpdatePurchaseOrder":
                    if (selectedItem != null)
                        OrderConfirmation(selectedItem, CompanyLayoutType.PurchaseOrder);
                    break;
                case "UpdateDeliveryNote":
                    if (selectedItem != null)
                        OrderConfirmation(selectedItem, CompanyLayoutType.PurchasePacknote);
                    break;
                case "EditCreditor":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreditorAccountPage2, new object[] { selectedItem.Creditor, true });
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                    {
                        CWOrderFromOrder cwOrderFromOrder = new CWOrderFromOrder(api);
                        cwOrderFromOrder.RelatedOrder= selectedItem.RelatedOrder;
                        cwOrderFromOrder.DialogTableId = 2000000027;
                        cwOrderFromOrder.Closed += async delegate
                        {
                            if (cwOrderFromOrder.DialogResult == true)
                            {
                                var perSupplier = cwOrderFromOrder.orderPerPurchaseAccount;
                                if (!perSupplier && string.IsNullOrEmpty(cwOrderFromOrder.Account) && cwOrderFromOrder.CreateNewOrder)
                                    return;
                                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                                busyIndicator.IsBusy = true;
                                var orderApi = new OrderAPI(api);
                                var inversign = cwOrderFromOrder.InverSign;
                                var account = cwOrderFromOrder.Account;
                                var copyAttachment = cwOrderFromOrder.copyAttachment;
                                var dcOrder = cwOrderFromOrder.dcOrder;
                                bool NewOrder = (dcOrder.RowId == 0);
                                dcOrder._DeliveryDate = cwOrderFromOrder.DeliveryDate;
                                var copyDelAddress = cwOrderFromOrder.copyDeliveryAddress;
                                var reCalPrice = cwOrderFromOrder.reCalculatePrice;
                                var result = await orderApi.CreateOrderFromOrder(selectedItem, dcOrder, account, inversign, CopyAttachments: copyAttachment, CopyDeliveryAddress: copyDelAddress, RecalculatePrices: reCalPrice, OrderPerPurchaseAccount: perSupplier);
                                busyIndicator.IsBusy = false;
                                if (result != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(result);
                                else
                                    ShowOrderLines(NewOrder ? (byte)2 : (byte)0, dcOrder, this, dgCreditorOrdersGrid);
                            }
                        };
                        cwOrderFromOrder.Show();
                    }
                    break;
                case "RefVoucher":
                    if (selectedItem == null)
                        return;
                    var _refferedVouchers = new List<int>();
                    if (selectedItem._DocumentRef != 0)
                        _refferedVouchers.Add(selectedItem._DocumentRef);

                    VoucherOpen = true;
                    AddDockItem(TabControls.AttachVoucherGridPage, new object[] { _refferedVouchers }, true);
                    break;
                case "ViewVoucher":
                    if (selectedItem != null)
                        ViewVoucher(TabControls.VouchersPage3, dgCreditorOrdersGrid.syncEntity);
                    break;
                case "DragDrop":
                case "ImportVoucher":
                    if (selectedItem != null)
                    {
                        dgCreditorOrdersGrid.SetLoadedRow(selectedItem);
                        AddVoucher(selectedItem, ActionType);
                    }
                    break;
                case "RemoveVoucher":
                    if (selectedItem != null)
                        RemoveVoucher(selectedItem);
                    break;
                case "EditAll":
                    if (dgCreditorOrdersGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddLine":
                    dgCreditorOrdersGrid.AddRow();
                    break;
                case "CopyRow":
                    selectedItem = dgCreditorOrdersGrid.CopyRow() as CreditorOrderClient;
                    if (selectedItem != null)
                    {
                        selectedItem.InvoiceAmount = 0;
                        selectedItem.InvoiceNumber = null;
                        selectedItem.OrderNumber = 0;
                    }
                    break;
                case "DeleteRow":
                    dgCreditorOrdersGrid.DeleteRow();
                    break;
                case "UndoDelete":
                    dgCreditorOrdersGrid.UndoDeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "PurchaseCharges":
                    if (selectedItem == null)
                        return;
                    header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("PurchaseCharges"), selectedItem._OrderNumber, selectedItem._DCAccount);
                    AddDockItem(TabControls.CreditorOrderCostLinePage, dgCreditorOrdersGrid.syncEntity, header);
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
                case "RefreshGrid":
                    TestCreditorReload(true, dgCreditorOrdersGrid.ItemsSource as IEnumerable<CreditorOrder>);
                    break;
                case "ApproveOrder":
                    if (selectedItem != null && api.CompanyEntity.ApprovePurchaseOrders)
                        Utility.ApproveOrder(api, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void AddVoucher(CreditorOrderClient selectedItem, string actionType)
        {
            var voucher = new VouchersClient();
            voucher._Content = ContentTypes.PurchaseInvoice;
            voucher._PurchaseNumber = selectedItem._OrderNumber;
            voucher._Project = selectedItem._Project;
            voucher._Approver1 = selectedItem._Approver;
            voucher._CreditorAccount = selectedItem._InvoiceAccount ?? selectedItem._DCAccount;
            if (actionType == "DragDrop")
            {
                var dragDropWindow = new UnicontaDragDropWindow(false);
                Utility.PauseLastAutoSaveTime();
                dragDropWindow.Closed += delegate
                {
                    if (dragDropWindow.DialogResult == true)
                    {
                        var fileInfo = dragDropWindow.FileInfoList?.SingleOrDefault();
                        if (fileInfo != null)
                        {
                            voucher._Data = fileInfo.FileBytes;
                            voucher._Text = fileInfo.FileName;
                            voucher._Fileextension = DocumentConvert.GetDocumentType(fileInfo.FileExtension);
                        }
                        Utility.ImportVoucher(selectedItem, api, voucher, true);
                    }
                };
                dragDropWindow.Show();
            }
            else
                Utility.ImportVoucher(selectedItem, api, voucher, false);
        }

        CreditorOrderClient prevOrder;
        DateTime prevDateTime;
        string prevInvoiceNumber;
        List<DCOrder> prevAddtionalOrdsSel;
        private void GenerateInvoice(CreditorOrderClient creditorOrderClient)
        {
            var accountName = Util.ConcatParenthesis(creditorOrderClient._DCAccount, creditorOrderClient.Name);
            var creditor = ClientHelper.GetRef(creditorOrderClient.CompanyId, typeof(Uniconta.DataModel.Creditor), creditorOrderClient._DCAccount) as Uniconta.DataModel.Creditor;
            bool showSendByEmail = creditor != null ? (!string.IsNullOrEmpty(creditor._InvoiceEmail) || creditor._EmailDocuments) : false;

            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(true, string.Empty, true, true, showNoEmailMsg: !showSendByEmail, AccountName: accountName);
            GenrateInvoiceDialog.DialogTableId = 2000000002;
            GenrateInvoiceDialog.SetSendAsEmailCheck(false);
           
            var additionalOrdersList = Utility.GetAdditionalOrders(api, creditorOrderClient);

            //Save values when the Simulation was setup in the previous action
            if (prevOrder != null && prevOrder.OrderNumber == creditorOrderClient.OrderNumber)
            {
                GenrateInvoiceDialog.SetInvoiceDate(prevDateTime);
                GenrateInvoiceDialog.SetInvoiceNumber(prevInvoiceNumber);
                if (additionalOrdersList != null)
                    GenrateInvoiceDialog.SetAdditionalOrders(additionalOrdersList, prevAddtionalOrdsSel);
            }
            else
            {
                if (creditorOrderClient._InvoiceDate != DateTime.MinValue)
                    GenrateInvoiceDialog.SetInvoiceDate(creditorOrderClient._InvoiceDate);
                GenrateInvoiceDialog.SetInvoiceNumber(creditorOrderClient._InvoiceNumber);
                if (additionalOrdersList != null)
                    GenrateInvoiceDialog.SetAdditionalOrders(additionalOrdersList);
            }

            GenrateInvoiceDialog.SetVouchersFromCreditorOrder(api, creditorOrderClient);
            GenrateInvoiceDialog.ShowAllowCredMax(creditor._CreditMax != 0);

            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {

                    var isSimulated = GenrateInvoiceDialog.IsSimulation;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(creditorOrderClient, null, CompanyLayoutType.PurchaseInvoice, GenrateInvoiceDialog.GenrateDate, GenrateInvoiceDialog.InvoiceNumber, isSimulated,
                        GenrateInvoiceDialog.ShowInvoice, false, GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.SendByOutlook,
                        GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails, false, null, false);
                    invoicePostingResult.SetAdditionalOrders(GenrateInvoiceDialog.AdditionalOrders?.Cast<DCOrder>().ToList());
                    invoicePostingResult.SetDocumentRef(GenrateInvoiceDialog.PhysicalVoucherRef);
                    if (api.CompanyEntity.AllowSkipCreditMax)
                        invoicePostingResult.SetAllowCreditMax(GenrateInvoiceDialog.AllowSkipCreditMax);

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (isSimulated)
                    {
                        prevOrder = creditorOrderClient;
                        prevDateTime = GenrateInvoiceDialog.GenrateDate;
                        prevInvoiceNumber = GenrateInvoiceDialog.InvoiceNumber;
                        prevAddtionalOrdsSel = GenrateInvoiceDialog.AdditionalOrders?.Cast<DCOrder>().ToList();
                    }

                    if (result)
                    {
                        if (invoicePostingResult.PostingResult.OrderDeleted)
                            dgCreditorOrdersGrid.UpdateItemSource(3, dgCreditorOrdersGrid.SelectedItem as CreditorOrderClient);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgCreditorOrdersGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var iBase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (iBase == null) return;

            if (dgCreditorOrdersGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgCreditorOrdersGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgCreditorOrdersGrid);
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
                                var err = await dgCreditorOrdersGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgCreditorOrdersGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgCreditorOrdersGrid.Readonly = true;
                        dgCreditorOrdersGrid.tableView.CloseEditor();
                        iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgCreditorOrdersGrid.Readonly = true;
                    dgCreditorOrdersGrid.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                }
            }
        }

        async void TestCreditorReload(bool refresh, IEnumerable<CreditorOrder> lst)
        {
            if (lst != null && lst.Count() > 0)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.Creditor));
                if (cache != null)
                {
                    bool reload = false;
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
                        await api.LoadCache(typeof(Uniconta.DataModel.Creditor), true);
                }
            }
            if (refresh)
                gridRibbon_BaseActions("RefreshGrid");
        }

        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgCreditorOrdersGrid.HasUnsavedData;
            }
        }

        private async void Save()
        {
            busyIndicator.IsBusy = true;
            var err = await dgCreditorOrdersGrid.SaveData();
            busyIndicator.IsBusy = false;
        }
        public void ImportVoucher(UnicontaBaseEntity selectedItem, VouchersClient voucher = null)
        {
            if (selectedItem == null)
                return;
        }

        void UpdateVoucher(VouchersClient attachedVoucher, CreditorOrderClient editrow)
        {
            if (attachedVoucher == null)
                return;
            var buf = attachedVoucher._Data;
            attachedVoucher._Data = null;
            var org = StreamingManager.Clone(attachedVoucher);
            attachedVoucher._Content = ContentTypes.PurchaseInvoice;
            attachedVoucher._PurchaseNumber = editrow._OrderNumber;
            attachedVoucher._CreditorAccount = editrow._InvoiceAccount ?? editrow._DCAccount;
            api.UpdateNoResponse(org, attachedVoucher);
            attachedVoucher._Data = buf;
        }

        static public void ShowOrderLines(byte thisDCType, DCOrder order, GridBasePage page, CorasauDataGrid grid)
        {
            string orderMsg, lineMsg, ctrl;
            var DCType = order.__DCType();
            switch (DCType)
            {
                case 2:
                    orderMsg = "PurchaseOrderCreated";
                    lineMsg = "PurchaseLines";
                    ctrl = TabControls.CreditorOrderLines;
                    break;
                case 3:
                    orderMsg = "OfferOrderCreated";
                    lineMsg = "OfferLine";
                    ctrl = TabControls.DebtorOfferLines;
                    break;
                case 4:
                    orderMsg = "ProductionOrderCreated";
                    lineMsg = "ProductionLines";
                    ctrl = TabControls.ProductionOrderLines;
                    break;
                default:
                    orderMsg = "SalesOrderCreated";
                    lineMsg = "OrdersLine";
                    ctrl = TabControls.DebtorOrderLines;
                    break;
            }
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup(orderMsg), Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup(lineMsg)), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (DCType == thisDCType)
                    grid.UpdateItemSource(1, order as UnicontaBaseEntity);
                if (confirmationBox.DialogResult == null)
                    return;
                if (confirmationBox.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                    page.AddDockItem(ctrl, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup(lineMsg), order._OrderNumber, order._DCAccount));
            };
            confirmationBox.Show();
        }

        static bool showInvPrintPreview = true;
        private void OrderConfirmation(CreditorOrderClient dbOrder, CompanyLayoutType doctype)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var creditor = dbOrder.Creditor;
            bool showSendByMail = true;
            if (creditor != null)
                showSendByMail = !string.IsNullOrEmpty(creditor._InvoiceEmail);
            string creditorName = creditor?._Name ?? dbOrder._DCAccount;
            var comp = api.CompanyEntity;
            bool showUpdateInv = comp.Storage || (doctype == CompanyLayoutType.PurchasePacknote && comp.CreditorPacknote);
            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, doctype.ToString(), showInputforInvNumber: doctype == CompanyLayoutType.PurchasePacknote ? true : false,
                isShowInvoiceVisible: true, askForEmail: true, showNoEmailMsg: !showSendByMail, debtorName: creditorName, isShowUpdateInv: showUpdateInv);

            switch (doctype)
            {
                case CompanyLayoutType.PurchaseOrder:
                    GenrateOfferDialog.DialogTableId = 2000000003;
                    break;
                case CompanyLayoutType.PurchasePacknote:
                    GenrateOfferDialog.DialogTableId = 2000000056;
                    break;
                case CompanyLayoutType.Requisition:
                    GenrateOfferDialog.DialogTableId = 2000000057;
                    break;
            }

            GenrateOfferDialog.SetInvPrintPreview(showInvPrintPreview);
            var additionalOrdersList = Utility.GetAdditionalOrders(api, dbOrder);
            if (additionalOrdersList != null)
                GenrateOfferDialog.SetAdditionalOrders(additionalOrdersList);

            GenrateOfferDialog.ShowAllowCredMax(creditor._CreditMax != 0);
            GenrateOfferDialog.Closed += async delegate
            {
                if (GenrateOfferDialog.DialogResult == true)
                {
                    showInvPrintPreview = GenrateOfferDialog.ShowInvoice || GenrateOfferDialog.InvoiceQuickPrint || GenrateOfferDialog.SendByOutlook;
                    string documentNumber = null;
                    if (doctype == CompanyLayoutType.PurchasePacknote)
                    {
                        documentNumber = GenrateOfferDialog.InvoiceNumber;
                        dbOrder._InvoiceNumber = documentNumber;
                    }
                    var openOutlook = doctype == CompanyLayoutType.PurchasePacknote ? GenrateOfferDialog.UpdateInventory && GenrateOfferDialog.SendByOutlook : GenrateOfferDialog.SendByOutlook;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(dbOrder, null, doctype, GenrateOfferDialog.GenrateDate, documentNumber, !GenrateOfferDialog.UpdateInventory, GenrateOfferDialog.ShowInvoice, false,
                        GenrateOfferDialog.InvoiceQuickPrint, GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, openOutlook, GenrateOfferDialog.sendOnlyToThisEmail, GenrateOfferDialog.Emails,
                        false, null, false);
                    invoicePostingResult.SetAdditionalOrders(GenrateOfferDialog.AdditionalOrders?.Cast<DCOrder>().ToList());
                    if (api.CompanyEntity.AllowSkipCreditMax)
                        invoicePostingResult.SetAllowCreditMax(GenrateOfferDialog.AllowSkipCreditMax);

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                        DebtorOrders.Updatedata(dbOrder, doctype);
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgCreditorOrdersGrid);
                }
            };
            GenrateOfferDialog.Show();
        }

        bool VoucherOpen;
        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorOrdersPage2)
                dgCreditorOrdersGrid.UpdateItemSource(argument);
            else if (screenName == TabControls.CreditorOrderLines)
            {
                var creditorOrder = argument as CreditorOrderClient;
                if (creditorOrder == null)
                {
                    var orderDeletedargs = argument as object[];
                    if (orderDeletedargs != null && orderDeletedargs.Length == 2)
                    {
                        var credOrder = orderDeletedargs[0] as CreditorOrderClient;
                        bool isdeleted = (bool)orderDeletedargs[1];
                        if (credOrder != null && isdeleted)
                            dgCreditorOrdersGrid.UpdateItemSource(3, credOrder);
                    }
                    return;
                }
                var err = await api.Read(creditorOrder);
                if (err == ErrorCodes.CouldNotFind)
                    dgCreditorOrdersGrid.UpdateItemSource(3, creditorOrder);
                else if (err == ErrorCodes.Succes)
                    dgCreditorOrdersGrid.UpdateItemSource(2, creditorOrder);
            }
            else if (screenName == TabControls.AttachVoucherGridPage && VoucherOpen)
            {
                VoucherOpen = false;
                var voucherObj = argument as object[];
                if (voucherObj != null && voucherObj.Length > 0)
                {
                    var attachedVoucher = voucherObj[0] as VouchersClient;
                    if (attachedVoucher != null)
                    {
                        var openedFrom = voucherObj[1];
                        if (openedFrom == this.ParentControl)
                        {
                            var selectedItem = dgCreditorOrdersGrid.SelectedItem as CreditorOrderClient;
                            if (selectedItem != null)
                            {
                                selectedItem.DocumentRef = attachedVoucher.RowId;
                                UpdateVoucher(attachedVoucher, selectedItem);
                            }
                        }
                    }
                }
            }
        }

        private Task BindGrid()
        {
            return dgCreditorOrdersGrid.Filter(null);
        }

        protected override void LoadCacheInBackGround()
        {
            var orders = api.GetCache(typeof(Uniconta.DataModel.CreditorOrder));
            TestCreditorReload(false, orders?.GetNotNullArray as IEnumerable<CreditorOrder>);

            var Comp = api.CompanyEntity;
            var lst = new List<Type>(20) { typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.Employee) };
            if (Comp.Contacts)
                lst.Add(typeof(Uniconta.DataModel.Contact));
            if (Comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));
            if (Comp.CreditorPrice)
                lst.Add(typeof(Uniconta.DataModel.CreditorPriceList));
            if (Comp.Shipments)
            {
                lst.Add(typeof(Uniconta.DataModel.ShipmentType));
                lst.Add(typeof(Uniconta.DataModel.DeliveryTerm));
            }
            if (Comp.ItemVariants)
            {
                lst.Add(typeof(Uniconta.DataModel.InvVariant1));
                lst.Add(typeof(Uniconta.DataModel.InvVariant2));
                lst.Add(typeof(Uniconta.DataModel.InvStandardVariant));
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

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var order = (sender as System.Windows.Controls.Image).Tag as CreditorOrderClient;
            if (order != null)
                AddDockItem(TabControls.UserDocsPage, dgCreditorOrdersGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var order = (sender as System.Windows.Controls.Image).Tag as CreditorOrderClient;
            if (order != null)
                AddDockItem(TabControls.UserNotesPage, dgCreditorOrdersGrid.syncEntity);
        }

    }
}