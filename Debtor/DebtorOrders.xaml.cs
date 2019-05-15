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
        public override string NameOfControl
        {
            get { return TabControls.DebtorOrders.ToString(); }
        }
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
            InitializeComponent();
            dgDebtorOrdersGrid.UpdateMaster(master);
            dgDebtorOrdersGrid.RowDoubleClick += dgDebtorOrdersGrid_RowDoubleClick;
            dgDebtorOrdersGrid.api = api;
            dgDebtorOrdersGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgDebtorOrdersGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorOrdersGrid.ShowTotalSummary();
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });
            dgDebtorOrdersGrid.CustomSummary += dgDebtorOrdersGrid_CustomSummary;
            LoadNow(typeof(Debtor));
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
            dgDebtorOrdersGrid.Readonly = true;
        }

        void dgDebtorOrdersGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("OrderLine");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorOrdersGrid.SelectedItem as DebtorOrderClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "AddRow":
                    if (dgDebtorOrdersGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { api, dgDebtorOrdersGrid.masterRecord };
                        AddDockItem(TabControls.DebtorOrdersPage2, arr, Uniconta.ClientTools.Localization.lookup("Orders"), ";component/Assets/img/Add_16x16.png");
                    }
                    else
                    {
                        AddDockItem(TabControls.DebtorOrdersPage2, api, Uniconta.ClientTools.Localization.lookup("Orders"), ";component/Assets/img/Add_16x16.png");
                    }
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    if (dgDebtorOrdersGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgDebtorOrdersGrid.masterRecord };
                        AddDockItem(TabControls.DebtorOrdersPage2, arr, salesHeader);
                    }
                    else
                    {
                        AddDockItem(TabControls.DebtorOrdersPage2, selectedItem, salesHeader);
                    }
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), selectedItem._OrderNumber, selectedItem.Name);
                    AddDockItem(TabControls.DebtorOrderLines, dgDebtorOrdersGrid.syncEntity, olheader);
                    break;
                case "Invoices":
                    AddDockItem(TabControls.Invoices, selectedItem, salesHeader);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem._OrderNumber);
                        AddDockItem(TabControls.UserNotesPage, dgDebtorOrdersGrid.syncEntity, header);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._OrderNumber);
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
                                var copyDelAddress = cwOrderFromOrder.copyDeliveryAddress;
                                var reCalPrice = cwOrderFromOrder.reCalculatePrice;
                                var result = await orderApi.CreateOrderFromOrder(selectedItem, dcOrder, account, inversign, CopyAttachments: copyAttachment, CopyDeliveryAddress: copyDelAddress, RecalculatePrices: reCalPrice, OrderPerPurchaseAccount: perSupplier);
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
                    dgDebtorOrdersGrid.CopyRow();
                    break;
                case "DeleteRow":
                    dgDebtorOrdersGrid.DeleteRow();
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
                        AddDockItem(TabControls.DebtorOrderProjectLinePage, selectedItem, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("ProjectAdjustments"), selectedItem._OrderNumber));
                    break;
                case "RefreshGrid":
                    TestDebtorReload();
                    break;
                case "RegenerateOrderFromProject":
                    if (selectedItem != null)
                        AddDockItem(TabControls.RegenerateOrderFromProjectPage, selectedItem, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("RegenerateOrder"), selectedItem._OrderNumber));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }   

        async void TestDebtorReload()
        {
            var lst = dgDebtorOrdersGrid.ItemsSource as IEnumerable<DebtorOrderClient>;
            if (lst != null && lst.Count() > 0)
            {
                bool reload = false;
                var api = this.api;
                var cache = api.CompanyEntity.GetCache(typeof(Debtor));
                if (cache == null)
                    reload = true;
                else
                {
                    foreach (var rec in lst)
                    {
                        if (rec._DCAccount != null && cache.Get(rec._DCAccount) == null)
                        {
                            reload = true;
                            break;
                        }
                    }
                }
                if (reload)
                    await api.CompanyEntity.LoadCache(typeof(Debtor), api, true);
            }
            gridRibbon_BaseActions("RefreshGrid");
        }

        async void jumpToDebtor(DebtorOrderClient selectedItem)
        {
            var dc = selectedItem.Debtor;
            if (dc == null)
            {
                await api.CompanyEntity.LoadCache(typeof(Debtor), api, true);
                dc = selectedItem.Debtor;
            }
            var param = new object[2] { dc, true };
            AddDockItem(TabControls.DebtorAccountPage2, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorAccount"), dc.Account));
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
                                 await dgDebtorOrdersGrid.SaveData();
                                 break;
                             case CWConfirmationBox.ConfirmationResultEnum.No:
                                 break;
                         }
                         editAllChecked = true;
                         dgDebtorOrdersGrid.Readonly = true;
                         dgDebtorOrdersGrid.tableView.CloseEditor();
                         iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                         ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });
                     };
                    confirmationDialog.Show();
                }
                else
                {
                    dgDebtorOrdersGrid.Readonly = true;
                    dgDebtorOrdersGrid.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "SaveGrid" });
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

        private async void Save()
        {
            var err = await dgDebtorOrdersGrid.SaveData();
            if (err == ErrorCodes.Succes)
                BindGrid();
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
                showSendByMail = !string.IsNullOrEmpty(debtor.InvoiceEmail);
            string debtorName = debtor?._Name ?? dbOrder._DCAccount;
            bool showUpdateInv = api.CompanyEntity.Storage || (doctype == CompanyLayoutType.Packnote && api.CompanyEntity.Packnote);
            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, Uniconta.ClientTools.Localization.lookup(doctype.ToString()), isShowInvoiceVisible: true, askForEmail: true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isShowUpdateInv: showUpdateInv, isDebtorOrder: true);
#if !SILVERLIGHT
            if (doctype == CompanyLayoutType.OrderConfirmation)
                GenrateOfferDialog.DialogTableId = 2000000009;
            else if (doctype == CompanyLayoutType.Packnote)
                GenrateOfferDialog.DialogTableId = 2000000018;
#endif
            GenrateOfferDialog.SetInvPrintPreview(showPrintPreview);
            GenrateOfferDialog.Closed += async delegate
            {
                if (GenrateOfferDialog.DialogResult == true)
                {
                    showPrintPreview = GenrateOfferDialog.ShowInvoice || GenrateOfferDialog.InvoiceQuickPrint;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this, dbOrder, null, GenrateOfferDialog.GenrateDate, 0, !GenrateOfferDialog.UpdateInventory, doctype, showPrintPreview, GenrateOfferDialog.InvoiceQuickPrint,
                        GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, GenrateOfferDialog.Emails, GenrateOfferDialog.sendOnlyToThisEmail, false, GenrateOfferDialog.PostOnlyDelivered, null);
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
            string debtorName = debtor?._Name ?? dbOrder._DCAccount;

            var cwPickingList = new CWGeneratePickingList();
#if !SILVERLIGHT
            cwPickingList.DialogTableId = 2000000049;
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
                     var invoicePostingResult = new InvoicePostingPrintGenerator(api, this, dbOrder, null, selectedDate, 0, false, CompanyLayoutType.PickingList, true, printDoc,
                         cwPickingList.NumberOfPages, cwPickingList.EmailList);

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
            var Comp = api.CompanyEntity;
            var installation = (WorkInstallation)Comp.GetCache(typeof(WorkInstallation), api)?.Get(InvClient._Installation);
            var deliveryAccount = (DCAccount)Comp.GetCache(typeof(Debtor), api)?.Get(InvClient._DeliveryAccount);
            var debtor = client ?? (DCAccount)Comp.GetCache(typeof(Debtor), api)?.Get(InvClient._DCAccount);
            var contact = (Contact)Comp.GetCache(typeof(Contact), api)?.Get(InvClient._ContactRef);

            InvClient._EAN = installation?._GLN ?? deliveryAccount?._EAN ?? contact?._EAN ?? debtor?._EAN;
        }

        static public void SetDeliveryAdress(DCInvoice InvClient, DCAccount client, QueryAPI api)
        {
            SetGLNnumber(InvClient, client, api);

            if (InvClient._DeliveryAddress1 != null)
                return;

            var Comp = api.CompanyEntity;
            if (InvClient._Installation != null)
            {
                var installation = (WorkInstallation)Comp.GetCache(typeof(WorkInstallation), api)?.Get(InvClient._Installation);
                if (installation != null)
                {
                    InvClient._DeliveryName = installation._Name;
                    InvClient._DeliveryAddress1 = installation._Address1;
                    InvClient._DeliveryAddress2 = installation._Address2;
                    InvClient._DeliveryAddress3 = installation._Address3;
                    InvClient._DeliveryZipCode = installation._ZipCode;
                    InvClient._DeliveryCity = installation._City;
                    if (Comp._Country != (byte)installation._Country)
                        InvClient._DeliveryCountry = installation._Country;
                }
            }
            else
            {
                DCAccount deb;
                bool UseDebAddress;
                if (InvClient._DeliveryAccount != null)
                {
                    deb = (DCAccount)Comp.GetCache(typeof(Debtor), api)?.Get(InvClient._DeliveryAccount);
                    UseDebAddress = true;
                }
                else
                {
                    deb = client ?? (DCAccount)Comp.GetCache(typeof(Debtor), api)?.Get(InvClient._DCAccount);
                    UseDebAddress = false;
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
                        if (Comp._Country != (byte)deb._Country)
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
                    return;
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
            var Comp = api.CompanyEntity;
            var lst = new List<Type>() { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.DebtorOrderGroup), typeof(Uniconta.DataModel.Employee) };
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
            LoadType(lst);
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void GenerateInvoice(DCOrder dbOrder)
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
                    showSendByMail = !string.IsNullOrEmpty(debtor._InvoiceEmail);
                }
            }
            else
                api.CompanyEntity.LoadCache(typeof(Debtor), api, true);

            string debtorName = debtor?._Name ?? dbOrder._DCAccount;
            bool invoiceInXML = debtor?._InvoiceInXML ?? false;
            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(true, string.Empty, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isOrderOrQuickInv: true, isDebtorOrder: true, InvoiceInXML: invoiceInXML);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000010;
#endif
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var showOrPrint = GenrateInvoiceDialog.ShowInvoice || GenrateInvoiceDialog.InvoiceQuickPrint;

                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this, dbOrder, null, GenrateInvoiceDialog.GenrateDate, 0, GenrateInvoiceDialog.IsSimulation, CompanyLayoutType.Invoice, showOrPrint,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.sendOnlyToThisEmail,
                        GenrateInvoiceDialog.GenerateOIOUBLClicked, GenrateInvoiceDialog.PostOnlyDelivered, null);

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        Task reloadTask = null;
                        if (!GenrateInvoiceDialog.IsSimulation && dbOrder._DeleteLines)
                            reloadTask = BindGrid();

                        if (invoicePostingResult.PostingResult.Header._InvoiceNumber != 0)
                        {
                            var msg = string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceHasBeenGenerated"), invoicePostingResult.PostingResult.Header._InvoiceNumber);
                            msg = string.Format("{0}{1}{2} {3}", msg, Environment.NewLine, Uniconta.ClientTools.Localization.lookup("LedgerVoucher"), invoicePostingResult.PostingResult.Header._Voucher);
                            UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);

#if !SILVERLIGHT
                            if (GenrateInvoiceDialog.GenerateOIOUBLClicked && !GenrateInvoiceDialog.IsSimulation)
                                GenerateOIOXml(this.api, invoicePostingResult.PostingResult);
#endif
                        }
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOrdersGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

#if !SILVERLIGHT
        static public async void GenerateOIOXml(CrudAPI api, InvoicePostingResult res)
        {
            var Comp = api.CompanyEntity;

            var InvCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api);
            var VatCache = Comp.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLVat), api);

            //SystemInfo.Visible = true;

            int countErr = 0;
            SaveFileDialog saveDialog = null;
            InvoiceAPI Invapi = new InvoiceAPI(api);

            var invClient = (DebtorInvoiceClient)res.Header;

            var Debcache = Comp.GetCache(typeof(Debtor)) ?? await Comp.LoadCache(typeof(Debtor), api);
            var debtor = (Debtor)Debcache.Get(invClient._DCAccount);


            var invoiceLines = (InvTransClient[])res.Lines;

            Contact contactPerson = null;
            if (invClient._ContactRef != 0)
            {
                var queryContact = await api.Query<Contact>(invClient);
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
                result = EHF.GenerateEHFXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, null, contactPerson);
            else
                result = Uniconta.API.DebtorCreditor.OIOUBL.GenerateOioXML(Comp, debtor, deliveryAccount, invClient, invoiceLines, InvCache, VatCache, null, contactPerson);

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

                if (session.User._AppDocPath != string.Empty && Directory.Exists(session.User._AppDocPath))
                {
                    try
                    {
                        applFilePath = string.Format("{0}\\OIOUBL", session.User._AppDocPath);
                        Directory.CreateDirectory(applFilePath);

                        filename = string.Format("{0}\\{1}.xml", applFilePath, filename);
                        hasUserFolder = true;
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                    }
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
                await Invapi.MarkSendInvoice(invClient);

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
