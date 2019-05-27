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
using System.Collections;
using Uniconta.API.DebtorCreditor;
using System.Windows.Threading;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.Service;
using System.IO;
using Uniconta.Common.Utility;
#if !SILVERLIGHT
using UnicontaClient.Pages;
#endif
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
            InitializeComponent();
            dgCreditorOrdersGrid.UpdateMaster(master);
            dgCreditorOrdersGrid.RowDoubleClick += dgCreditorOrdersGrid_RowDoubleClick;
            localMenu.dataGrid = dgCreditorOrdersGrid;
            dgCreditorOrdersGrid.api = api;
            dgCreditorOrdersGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCreditorOrdersGrid);

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });

            RemoveMenuItem();

            LoadNow(typeof(Uniconta.DataModel.Creditor));
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;

#if !SILVERLIGHT
            if (Comp._CountryId != CountryCode.Denmark)
                UtilDisplay.RemoveMenuCommand(rb, "ReadOIOUBL");
#else
             UtilDisplay.RemoveMenuCommand(rb, "ReadOIOUBL");
#endif
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgCreditorOrdersGrid.masterRecords == null);
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
            dgCreditorOrdersGrid.Readonly = true;
        }

        void dgCreditorOrdersGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("OrderLine");
        }

        VouchersClient attachedVoucher;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorOrdersGrid.SelectedItem as CreditorOrderClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber, true);
            switch (ActionType)
            {
                case "AddRow":
                    if (dgCreditorOrdersGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { api, dgCreditorOrdersGrid.masterRecord };
                        AddDockItem(TabControls.CreditorOrdersPage2, arr, Uniconta.ClientTools.Localization.lookup("Orders"), ";component/Assets/img/Add_16x16.png");
                    }
                    else
                        AddDockItem(TabControls.CreditorOrdersPage2, api, Uniconta.ClientTools.Localization.lookup("Orders"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    if (dgCreditorOrdersGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgCreditorOrdersGrid.masterRecord };
                        AddDockItem(TabControls.CreditorOrdersPage2, arr, salesHeader);
                    }
                    else
                        AddDockItem(TabControls.CreditorOrdersPage2, selectedItem, salesHeader);
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem._OrderNumber, selectedItem.Name);
                    AddDockItem(TabControls.CreditorOrderLines, dgCreditorOrdersGrid.syncEntity, olheader);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        string noteHeader = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem.Account);
                        AddDockItem(TabControls.UserNotesPage, dgCreditorOrdersGrid.syncEntity, noteHeader);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        string docHeader = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.Account);
                        AddDockItem(TabControls.UserDocsPage, dgCreditorOrdersGrid.syncEntity, docHeader);
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
#if !SILVERLIGHT
                        cwOrderFromOrder.DialogTableId = 2000000027;
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
                                var copyDelAddress = cwOrderFromOrder.copyDeliveryAddress;
                                var reCalPrice = cwOrderFromOrder.reCalculatePrice;
                                var result = await orderApi.CreateOrderFromOrder(selectedItem, dcOrder, account, inversign, CopyAttachments: copyAttachment, CopyDeliveryAddress: copyDelAddress, RecalculatePrices: reCalPrice, OrderPerPurchaseAccount: perSupplier);
                                busyIndicator.IsBusy = false;
                                if (result != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(result);
                                else
                                    ShowOrderLines(2, dcOrder, this, dgCreditorOrdersGrid);
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

                    AddDockItem(TabControls.AttachVoucherGridPage, new object[1] { _refferedVouchers }, true);
                    break;
                case "ViewVoucher":
                    if (selectedItem == null)
                        return;
                    ViewVoucher(TabControls.VouchersPage3, dgCreditorOrdersGrid.syncEntity);
                    break;

                case "ImportVoucher":
                    if (selectedItem == null)
                        return;
                    var voucher = new VouchersClient();
                    voucher._Content = ContentTypes.PurchaseInvoice;
                    voucher.PurchaseNumber = selectedItem._OrderNumber;
                    CWAddVouchers addVouvhersDialog = new CWAddVouchers(api, false, voucher);
                    addVouvhersDialog.Closed += delegate
                    {
                        if (addVouvhersDialog.DialogResult == true)
                        {
                            var propertyInfo = selectedItem.GetType().GetProperty("DocumentRef");
                            if (addVouvhersDialog.VoucherRowIds.Count() > 0 && propertyInfo != null)
                            {
                                propertyInfo.SetValue(selectedItem, addVouvhersDialog.VoucherRowIds[0], null);
                                UpdateVoucher(selectedItem);
                            }
                        }
                    };
                    addVouvhersDialog.Show();
                    break;
                case "RemoveVoucher":
                    if (selectedItem == null)
                        return;
                    RemoveVoucher(selectedItem);
                    UpdateVoucher(selectedItem);
                    break;
                case "EditAll":
                    if (dgCreditorOrdersGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddLine":
                    dgCreditorOrdersGrid.AddRow();
                    break;
                case "CopyRow":
                    dgCreditorOrdersGrid.CopyRow();
                    break;
                case "DeleteRow":
                    dgCreditorOrdersGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "PurchaseCharges":
                    if (selectedItem == null)
                        return;
                    var header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("PurchaseCharges"), selectedItem._DCAccount, selectedItem._OrderNumber);
                    AddDockItem(TabControls.CreditorOrderCostLinePage, dgCreditorOrdersGrid.syncEntity, header);
                    break;
                case "CreateInvoice":
                    if (selectedItem != null)
                        GenerateInvoice(selectedItem);
                    break;
                case "ReadOIOUBL":
#if !SILVERLIGHT
                    var orderOIOCW = new CWOrderOIOUBLImport();
                    orderOIOCW.Closing += delegate
                    {
                        if (orderOIOCW.DialogResult != true)
                            return;

                        ReadOIOUBL(orderOIOCW.OneOrMultiple);
                    };
                    orderOIOCW.Show();


                    gridRibbon_BaseActions("RefreshGrid");
#endif
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void GenerateInvoice(DCOrder creditorOrderClient)
        {
            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(true, string.Empty, true, false, false);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000002;
#endif
            if (creditorOrderClient._InvoiceNumber != 0)
                GenrateInvoiceDialog.SetInvoiceNumber(creditorOrderClient._InvoiceNumber);
            if (creditorOrderClient._InvoiceDate != DateTime.MinValue)
                GenrateInvoiceDialog.SetInvoiceDate(creditorOrderClient._InvoiceDate);

            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {

                    var showOrPrint = GenrateInvoiceDialog.ShowInvoice || GenrateInvoiceDialog.InvoiceQuickPrint;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this, creditorOrderClient, null, GenrateInvoiceDialog.GenrateDate, GenrateInvoiceDialog.InvoiceNumber, GenrateInvoiceDialog.IsSimulation,
                        CompanyLayoutType.PurchaseInvoice, showOrPrint, GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, false, GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.sendOnlyToThisEmail);

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        Task reloadTask = null;
                        if (!GenrateInvoiceDialog.IsSimulation && creditorOrderClient._DeleteLines)
                            reloadTask = Filter();

                        string msg;
                        if (invoicePostingResult.PostingResult.Header._InvoiceNumber != 0)
                        {
                            msg = string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceHasBeenGenerated"), invoicePostingResult.PostingResult.Header._InvoiceNumber);
                            msg = string.Format("{0}{1}{2} {3}", msg, Environment.NewLine, Uniconta.ClientTools.Localization.lookup("LedgerVoucher"), invoicePostingResult.PostingResult.Header._Voucher);
                            UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                        }
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
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });
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
                                break;
                        }
                        editAllChecked = true;
                        dgCreditorOrdersGrid.Readonly = true;
                        dgCreditorOrdersGrid.tableView.CloseEditor();
                        iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgCreditorOrdersGrid.Readonly = true;
                    dgCreditorOrdersGrid.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });
                }
            }
        }

        public async void ReadOIOUBL(bool oneOrMultiple)
        {
#if !SILVERLIGHT
            try
            {
                var creditor = await api.Query<CreditorClient>();
                var orderlist = ((CreditorOrderClient[])dgCreditorOrdersGrid.ItemsSource).ToList();

                if (!oneOrMultiple)
                {
                    var orderlistToAddToGrid = new List<CreditorOrderClient>();
                    var listOfFailedFiles = Uniconta.ClientTools.Localization.lookup("ViewerFailed") + ":\n";

                    var openFolderDialog = UtilDisplay.LoadFolderBrowserDialog;
                    if (openFolderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                        return;

                    string[] files = Directory.GetFiles(openFolderDialog.SelectedPath);

                    foreach (string file in files)
                    {
                        var xmlText = File.ReadAllText(file);
                        var order = await OIOUBL.ReadInvoiceCreditNoteOrOrder(xmlText, creditor, api, oneOrMultiple);

                        if (order == null)
                        {
                            listOfFailedFiles = listOfFailedFiles + file + "\n";
                            continue;
                        }

                        var orderlines = order.Lines;
                        order.Lines = null;

                        var err = await api.Insert(order);
                        if (err != ErrorCodes.Succes)
                        {
                            listOfFailedFiles = listOfFailedFiles + file + "\n";
                            continue;
                        }

                        int countLine = 0;
                        foreach (var line in orderlines)
                        {
                            line.SetMaster(order);
                            line._LineNumber = ++countLine;
                        }

                        var err2 = await api.Insert(orderlines);
                        if (err2 != ErrorCodes.Succes)
                        {
                            listOfFailedFiles = listOfFailedFiles + file + "\n";
                            continue;
                        }


                        if (order.DocumentRef != 0)
                            UpdateVoucher(order);

                        order.Lines = orderlines;
                        orderlistToAddToGrid.Add(order);
                    }

                    if (orderlist == null || orderlist.Count <= 0)
                        orderlist = orderlistToAddToGrid;
                    else
                        orderlist.AddRange(orderlistToAddToGrid);

                    dgCreditorOrdersGrid.ItemsSource = orderlist;

                    if (listOfFailedFiles != null || listOfFailedFiles.Length > 20)
                    {
                        listOfFailedFiles = listOfFailedFiles + Uniconta.ClientTools.Localization.lookup("RunFileIndividual");
                        UnicontaMessageBox.Show(listOfFailedFiles, Uniconta.ClientTools.Localization.lookup("Error"));
                    }
                }
                else
                {
                    var sfd = UtilDisplay.LoadOpenFileDialog;
                    sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.XML);

                    var userClickedSave = sfd.ShowDialog();
                    if (userClickedSave != true)
                        return;

                    using (var stream = File.Open(sfd.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var oioublText = await sr.ReadToEndAsync();
                            var order = await OIOUBL.ReadInvoiceCreditNoteOrOrder(oioublText, creditor, api, oneOrMultiple);

                            if (order == null)
                                return;

                            var orderlines = order.Lines;
                            order.Lines = null;

                            var err = await api.Insert(order);
                            if (err != ErrorCodes.Succes)
                            {
                                UnicontaMessageBox.Show(err.ToString(), Uniconta.ClientTools.Localization.lookup("Error"));
                                return;
                            }

                            int countLine = 0;
                            foreach (var line in orderlines)
                            {
                                line.SetMaster(order);
                                line._LineNumber = ++countLine;
                            }

                            var err2 = await api.Insert(orderlines);
                            if (err2 != ErrorCodes.Succes)
                            {
                                UnicontaMessageBox.Show(err2.ToString(), Uniconta.ClientTools.Localization.lookup("Error"));
                                return;
                            }

                            if (order.DocumentRef != 0)
                                UpdateVoucher(order);

                            if (orderlist == null || orderlist.Count <= 0)
                                orderlist = new List<CreditorOrderClient>();

                            orderlist.Add(order);

                            dgCreditorOrdersGrid.ItemsSource = orderlist;
                        }
                        stream.Close();
                    }
                }
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show(e.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
#endif
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
            if (err == ErrorCodes.Succes)
                await BindGrid();
            busyIndicator.IsBusy = false;
        }
        public void ImportVoucher(UnicontaBaseEntity selectedItem, VouchersClient voucher = null)
        {
            if (selectedItem == null)
                return;

        }

        async void UpdateVoucher(CreditorOrderClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            var err = await api.Update(selectedItem);
            busyIndicator.IsBusy = false;
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
        }

        async void UpdateAttachedVoucher(int orderNumber)
        {
            busyIndicator.IsBusy = true;
            attachedVoucher.PurchaseNumber = orderNumber;
            var err = await api.Update(attachedVoucher);
            busyIndicator.IsBusy = false;
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
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
            bool showUpdateInv = api.CompanyEntity.Storage;
            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, Uniconta.ClientTools.Localization.lookup(doctype.ToString()), isShowInvoiceVisible: true, askForEmail: true, showNoEmailMsg: !showSendByMail, debtorName: creditorName, isShowUpdateInv: showUpdateInv);
#if !SILVERLIGHT
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
#endif
            GenrateOfferDialog.SetInvPrintPreview(showInvPrintPreview);
            GenrateOfferDialog.Closed += async delegate
            {
                if (GenrateOfferDialog.DialogResult == true)
                {
                    showInvPrintPreview = GenrateOfferDialog.ShowInvoice || GenrateOfferDialog.InvoiceQuickPrint;

                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this, dbOrder, null, GenrateOfferDialog.GenrateDate, 0, !GenrateOfferDialog.UpdateInventory, doctype,
                        showInvPrintPreview, GenrateOfferDialog.InvoiceQuickPrint, GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, GenrateOfferDialog.Emails, GenrateOfferDialog.sendOnlyToThisEmail);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        DebtorOrders.Updatedata(dbOrder, doctype);
                        UtilDisplay.ShowErrorCode(ErrorCodes.Succes);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgCreditorOrdersGrid);
                }
            };
            GenrateOfferDialog.Show();
        }

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorOrdersPage2)
                dgCreditorOrdersGrid.UpdateItemSource(argument);
            else if (screenName == TabControls.CreditorOrderLines)
            {
                var creditorOrder = argument as CreditorOrderClient;
                if (creditorOrder == null)
                    return;
                var err = await api.Read(creditorOrder);
                if (err == ErrorCodes.CouldNotFind)
                    dgCreditorOrdersGrid.UpdateItemSource(3, creditorOrder);
                else if (err == ErrorCodes.Succes)
                    dgCreditorOrdersGrid.UpdateItemSource(2, creditorOrder);
            }
            else if (screenName == TabControls.AttachVoucherGridPage)
            {
                var voucherObj = argument as object[];

                if (voucherObj != null && voucherObj[0] is VouchersClient)
                {
                    var selectedItem = dgCreditorOrdersGrid.SelectedItem as CreditorOrderClient;
                    attachedVoucher = voucherObj[0] as VouchersClient;
                    selectedItem.DocumentRef = attachedVoucher.RowId;
                    UpdateAttachedVoucher(selectedItem._OrderNumber);
                    UpdateVoucher(selectedItem);
                }
            }
        }

        private Task BindGrid()
        {
            return dgCreditorOrdersGrid.Filter(null);
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;
            var lst = new List<Type>() { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.CreditorGroup) };
            if (Comp.CreditorPrice)
                lst.Add(typeof(Uniconta.DataModel.CreditorPriceList));
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
            LoadType(lst);
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

    }
}