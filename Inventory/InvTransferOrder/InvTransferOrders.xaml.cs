using DevExpress.Xpf.WindowsUI.Navigation;
using DevExpress.XtraReports.Design;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
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
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvTransferOrdersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvTransferOrderClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class InvTransferOrders : GridBasePage
    {
        SQLCache creditorCache;
        public override string NameOfControl
        {
            get { return TabControls.InvTransferOrders.ToString(); }
        }

        public InvTransferOrders(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init(null);
        }

        public InvTransferOrders(BaseAPI API)
            : base(API, string.Empty)
        {
            Init(null);
        }
        public InvTransferOrders(SynchronizeEntity syncEntity)
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
            dgInvTransferOrdersGrid.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }
        void SetHeader()
        {
            var syncMaster = dgInvTransferOrdersGrid.masterRecord as Uniconta.DataModel.Creditor;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("TransferOrders"), syncMaster._Account);
            SetHeader(header);
        }
        public InvTransferOrders(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }
        void Init(UnicontaBaseEntity master)
        {
            LoadNow(typeof(Uniconta.DataModel.CreditorOrderGroup));
            InitializeComponent();
            dgInvTransferOrdersGrid.UpdateMaster(master);
            dgInvTransferOrdersGrid.RowDoubleClick += dgInvTransferOrdersGrid_RowDoubleClick;
            localMenu.dataGrid = dgInvTransferOrdersGrid;
            dgInvTransferOrdersGrid.api = api;
            dgInvTransferOrdersGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgInvTransferOrdersGrid);

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
            creditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor));
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgInvTransferOrdersGrid.masterRecords == null);
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
            dgInvTransferOrdersGrid.Readonly = true;
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

        void dgInvTransferOrdersGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("OrderLine");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgInvTransferOrdersGrid_RowDoubleClick();
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            string header;
            var dgInvTransferOrdersGrid = this.dgInvTransferOrdersGrid;
            var selectedItem = dgInvTransferOrdersGrid.SelectedItem as InvTransferOrderClient;
            switch (ActionType)
            {
                case "AddRow":
                    if (dgInvTransferOrdersGrid.masterRecords != null)
                        AddDockItem(TabControls.InvTransferOrderPage2, new object[] { api, dgInvTransferOrdersGrid.masterRecord }, Uniconta.ClientTools.Localization.lookup("Orders"), "Add_16x16");
                    else
                        AddDockItem(TabControls.InvTransferOrderPage2, api, Uniconta.ClientTools.Localization.lookup("Orders"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber, true);
                    if (dgInvTransferOrdersGrid.masterRecords != null)
                        AddDockItem(TabControls.InvTransferOrderPage2, new object[] { selectedItem, dgInvTransferOrdersGrid.masterRecord }, header);
                    else
                        AddDockItem(TabControls.InvTransferOrderPage2, selectedItem, header);
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("TransferOrderLines"), selectedItem._OrderNumber, selectedItem._DCAccount);
                    AddDockItem(TabControls.InvTransferOrderLines, dgInvTransferOrdersGrid.syncEntity, header);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem.KeyName);
                        AddDockItem(TabControls.UserNotesPage, dgInvTransferOrdersGrid.syncEntity, header);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.KeyName);
                        AddDockItem(TabControls.UserDocsPage, dgInvTransferOrdersGrid.syncEntity, header);
                    }
                    break;
                case "PostOrder":
                    PostOrder();
                    break;
                case "CopyOrder":
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
                                   CreditorOrders.ShowOrderLines(NewOrder ? (byte)2 : (byte)0, dcOrder, this, dgInvTransferOrdersGrid);
                            }
                        };
                        cwOrderFromOrder.Show();
                    }
                    break;
                case "EditAll":
                    if (dgInvTransferOrdersGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddLine":
                    dgInvTransferOrdersGrid.AddRow();
                    break;
                case "CopyRow":
                    selectedItem = dgInvTransferOrdersGrid.CopyRow() as InvTransferOrderClient;
                    if (selectedItem != null)
                    {
                       selectedItem.OrderNumber = 0;
                    }
                    break;
                case "DeleteRow":
                    dgInvTransferOrdersGrid.DeleteRow();
                    break;
                case "UndoDelete":
                    dgInvTransferOrdersGrid.UndoDeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "UpdatePickList":
                case "UpdateDeliveryNote":
                    if (selectedItem != null)
                        OrderConfirmation(selectedItem, ActionType == "UpdateDeliveryNote" ? CompanyLayoutType.TransferPacknote : CompanyLayoutType.PickingList);
                    break;
                case "RefreshGrid":
                    TestCreditorReload(true, dgInvTransferOrdersGrid.ItemsSource as IEnumerable<InvTransferOrder>);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        DateTime PostedDate;
        string Comment;
        void PostOrder()
        {
            var selectedItem = dgInvTransferOrdersGrid.SelectedItem as InvTransferOrderClient;
            if (selectedItem == null)
                return;

            var postingDialog = new CWPostClosingSheet(DateTime.MinValue)
            {
                DialogTableId = 2000000112,
                PostedDate = this.PostedDate,
                comments = this.Comment,
            };
            postingDialog.Header = string.Format(Uniconta.ClientTools.Localization.lookup("PostOBJ"), Uniconta.ClientTools.Localization.lookup("TransferOrder"));
            postingDialog.HideCode();
            postingDialog.Closed += async delegate
            {
                if (postingDialog.DialogResult == true)
                {
                    this.PostedDate = postingDialog.PostedDate;
                    this.Comment = postingDialog.comments;

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var postingApi = new Uniconta.API.Inventory.PostingAPI(api);
                    var postingResult = await postingApi.PostInvTransferOrder(selectedItem, PostedDate, Comment, postingDialog.IsSimulation, 0);
                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                    if (postingResult == null)
                        return;

                    if (postingResult.Err != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(postingResult.Err);
                    else
                    {
                        if (postingDialog.IsSimulation)
                        {
                            if (postingResult.SimulatedTrans != null)
                                AddDockItem(TabControls.SimulatedTransactions, postingResult.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                        }
                        else
                        {
                            this.PostedDate = DateTime.MinValue;
                            this.Comment = null;
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("TransferOrderPosted"), Uniconta.ClientTools.Localization.lookup("Message"));
                            dgInvTransferOrdersGrid.Refresh();
                        }
                    }
                }
            };
            postingDialog.Show();
        }

        //void PostOrder()
        //{
       
        //    CWInvPosting postingDialog = new CWInvPosting(api)
        //    {
        //        DialogTableId = 2000000039,
        //        Date = this.PostedDate,
        //        Comment = this.Comment,
        //    };
        //    postingDialog.Closed += async delegate
        //    {
        //        if (postingDialog.DialogResult == true)
        //        {
        //            this.PostedDate = postingDialog.Date;
        //            this.Comment = postingDialog.Comment;

        //            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
        //            busyIndicator.IsBusy = true;
        //            var postingApi = new Uniconta.API.Inventory.PostingAPI(api);
        //            var postingResult = await postingApi.PostInvTransferOrder(selectedItem, postingDialog.Date, postingDialog.Comment, postingDialog.Simulation, source.Count);
        //            busyIndicator.IsBusy = false;
        //            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

        //            if (postingResult == null)
        //                return;

        //            if (postingResult.Err != ErrorCodes.Succes)
        //                Utility.ShowJournalError(postingResult, dgInvJournalLine);

        //            else if (postingDialog.Simulation)
        //            {
        //                if (postingResult.SimulatedTrans != null)
        //                    AddDockItem(TabControls.SimulatedTransactions, postingResult.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
        //                else
        //                {
        //                    var msg = string.Format(Uniconta.ClientTools.Localization.lookup("OBJisEmpty"), Uniconta.ClientTools.Localization.lookup("LedgerTransList"));
        //                    msg = Uniconta.ClientTools.Localization.lookup("JournalOK") + Environment.NewLine + msg;
        //                    UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
        //                }
        //            }
        //            else
        //            {
        //                this.PostedDate = DateTime.MinValue;
        //                this.Txt = null;
        //                this.Comment = null;

        //                string msg;
        //                if (postingResult.JournalPostedlId != 0)
        //                    msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), postingResult.JournalPostedlId);
        //                else
        //                    msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
        //                UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));

        //                if (journal._DeleteLines)
        //                {
        //                    var lst = new List<InvJournalLineGridClient>();
        //                    foreach (var journalLine in source)
        //                        if (journalLine._OnHold)
        //                            lst.Add(journalLine);

        //                    dgInvJournalLine.ItemsSource = null;
        //                    dgInvJournalLine.ItemsSource = lst;
        //                    journal._NumberOfLines = lst.Count;
        //                    (journal as InvJournalClient)?.NotifyPropertyChanged("NumberOfLines");
        //                }
        //            }
        //        }
        //    };
        //    postingDialog.Show();
        //}

        static bool showInvPrintPreview = true;
        private void OrderConfirmation(InvTransferOrderClient InvTransferOrderClient, CompanyLayoutType doctype)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            bool showSendByMail = true;
            var comp = api.CompanyEntity;
            bool showUpdateInv = comp.Storage || ((doctype == CompanyLayoutType.TransferPacknote || doctype == CompanyLayoutType.PickingList) && comp.CreditorPacknote);
            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, doctype.ToString(), showInputforInvNumber: false, isShowInvoiceVisible: true,
                askForEmail: false, showNoEmailMsg: !showSendByMail, isShowUpdateInv: showUpdateInv);
            switch (doctype)
            {
                case CompanyLayoutType.PickingList:
                    GenrateOfferDialog.DialogTableId = 2000000113;
                    break;
                case CompanyLayoutType.TransferPacknote:
                    GenrateOfferDialog.DialogTableId = 2000000114;
                    break;
            }
            GenrateOfferDialog.SetInvPrintPreview(showInvPrintPreview);

            GenrateOfferDialog.Closed += async delegate
            {
                if (GenrateOfferDialog.DialogResult == true)
                {
                    showInvPrintPreview = GenrateOfferDialog.ShowInvoice || GenrateOfferDialog.InvoiceQuickPrint || GenrateOfferDialog.SendByOutlook;

                    var openOutlook = doctype == CompanyLayoutType.TransferPacknote || doctype == CompanyLayoutType.PickingList ? GenrateOfferDialog.UpdateInventory && GenrateOfferDialog.SendByOutlook : GenrateOfferDialog.SendByOutlook;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(InvTransferOrderClient, null, doctype, GenrateOfferDialog.GenrateDate, null, !GenrateOfferDialog.UpdateInventory, GenrateOfferDialog.ShowInvoice, false,
                        GenrateOfferDialog.InvoiceQuickPrint, GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, openOutlook, GenrateOfferDialog.sendOnlyToThisEmail,
                        GenrateOfferDialog.Emails, false, null, false);
                    if (api.CompanyEntity.AllowSkipCreditMax)
                        invoicePostingResult.SetAllowCreditMax(GenrateOfferDialog.AllowSkipCreditMax);

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        if (invoicePostingResult.PostingResult.OrderDeleted)
                            dgInvTransferOrdersGrid.UpdateItemSource(3, dgInvTransferOrdersGrid.SelectedItem as InvTransferOrderClient);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgInvTransferOrdersGrid);
                }
            };
            GenrateOfferDialog.Show();
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var iBase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (iBase == null) return;

            if (dgInvTransferOrdersGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgInvTransferOrdersGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgInvTransferOrdersGrid);
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
                                var err = await dgInvTransferOrdersGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgInvTransferOrdersGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgInvTransferOrdersGrid.Readonly = true;
                        dgInvTransferOrdersGrid.tableView.CloseEditor();
                        iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgInvTransferOrdersGrid.Readonly = true;
                    dgInvTransferOrdersGrid.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                }
            }
        }

        async void TestCreditorReload(bool refresh, IEnumerable<InvTransferOrder> lst)
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
                return editAllChecked ? false : dgInvTransferOrdersGrid.HasUnsavedData;
            }
        }

        private async void Save()
        {
            busyIndicator.IsBusy = true;
            var err = await dgInvTransferOrdersGrid.SaveData();
            busyIndicator.IsBusy = false;
        }
        public void ImportVoucher(UnicontaBaseEntity selectedItem, VouchersClient voucher = null)
        {
            if (selectedItem == null)
                return;
        }

        void UpdateVoucher(VouchersClient attachedVoucher, InvTransferOrderClient editrow)
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
      
        bool VoucherOpen;
        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InvTransferOrderPage2)
                dgInvTransferOrdersGrid.UpdateItemSource(argument);
            else if (screenName == TabControls.InvTransferOrderLines)
            {
                var InvTransferOrder = argument as InvTransferOrderClient;
                if (InvTransferOrder == null)
                {
                    var orderDeletedargs = argument as object[];
                    if (orderDeletedargs != null && orderDeletedargs.Length == 2)
                    {
                        var credOrder = orderDeletedargs[0] as InvTransferOrderClient;
                        bool isdeleted = (bool)orderDeletedargs[1];
                        if (credOrder != null && isdeleted)
                            dgInvTransferOrdersGrid.UpdateItemSource(3, credOrder);
                    }
                    return;
                }
                var err = await api.Read(InvTransferOrder);
                if (err == ErrorCodes.CouldNotFind)
                    dgInvTransferOrdersGrid.UpdateItemSource(3, InvTransferOrder);
                else if (err == ErrorCodes.Succes)
                    dgInvTransferOrdersGrid.UpdateItemSource(2, InvTransferOrder);
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
                            var selectedItem = dgInvTransferOrdersGrid.SelectedItem as InvTransferOrderClient;
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
            return dgInvTransferOrdersGrid.Filter(null);
        }

        protected override void LoadCacheInBackGround()
        {
            var orders = api.GetCache(typeof(Uniconta.DataModel.InvTransferOrder));
            TestCreditorReload(false, orders?.GetNotNullArray as IEnumerable<InvTransferOrder>);

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
            var order = (sender as System.Windows.Controls.Image).Tag as InvTransferOrderClient;
            if (order != null)
                AddDockItem(TabControls.UserDocsPage, dgInvTransferOrdersGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var order = (sender as System.Windows.Controls.Image).Tag as InvTransferOrderClient;
            if (order != null)
                AddDockItem(TabControls.UserNotesPage, dgInvTransferOrdersGrid.syncEntity);
        }

    }
}