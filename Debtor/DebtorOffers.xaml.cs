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
using Uniconta.ClientTools.Util;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Pages;
using Uniconta.API.Service;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorOffersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorOfferClient); } }
    }
    public partial class DebtorOffers : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.DebtorOffers.ToString(); }
        }

        public DebtorOffers(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public DebtorOffers(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        public DebtorOffers(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            if (syncEntity != null)
                Init(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDebtorOffers.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgDebtorOffers.masterRecord as Uniconta.DataModel.Debtor;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("Offers"), syncMaster._Account);
            else
            {
                var projectMaster = dgDebtorOffers.masterRecord as Uniconta.DataModel.Project;
                if (projectMaster != null)
                    header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("Offers"), projectMaster._DCAccount);
            }
            if (header != null)
                SetHeader(header);
        }

        private void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgDebtorOffers.UpdateMaster(master);
            dgDebtorOffers.RowDoubleClick += dgDebtorOffers_RowDoubleClick;
            localMenu.dataGrid = dgDebtorOffers;
            SetRibbonControl(localMenu, dgDebtorOffers);
            dgDebtorOffers.api = api;
            dgDebtorOffers.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgDebtorOffers.masterRecords == null);
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
            dgDebtorOffers.Readonly = true;
            if (!Comp.Project)
                Project.ShowInColumnChooser = Project.Visible = PrCategory.ShowInColumnChooser = PrCategory.Visible =
                    Task.ShowInColumnChooser = Task.Visible = WorkSpace.ShowInColumnChooser = WorkSpace.Visible = false;
            else
                Project.ShowInColumnChooser = PrCategory.ShowInColumnChooser = Task.ShowInColumnChooser = true;
            if (!Comp.ProjectTask)
                Task.ShowInColumnChooser = Task.Visible = false;
            else
                Task.ShowInColumnChooser = true;
            Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
            CostValue.Visible = CostValue.ShowInColumnChooser = !api.CompanyEntity.HideCostPrice;
        }

        protected override void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;
            var lst = new List<Type>(20) { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Employee) };
            if (Comp.Contacts)
                lst.Add(typeof(Uniconta.DataModel.Contact));
            if (Comp.InvPrice)
                lst.Add(typeof(Uniconta.DataModel.DebtorPriceList));
            if (Comp.Shipments)
            {
                lst.Add(typeof(Uniconta.DataModel.ShipmentType));
                lst.Add(typeof(Uniconta.DataModel.DeliveryTerm));
            }
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

        void dgDebtorOffers_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("OfferLine");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgDebtorOffers_RowDoubleClick();
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorOffers.SelectedItem as DebtorOfferClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Offers"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "AddRow":
                    if (dgDebtorOffers.masterRecords != null)
                    {
                        object[] arr = new object[2] { api, dgDebtorOffers.masterRecord };
                        AddDockItem(TabControls.DebtorOfferPage2, arr, Uniconta.ClientTools.Localization.lookup("Offers"), "Add_16x16", true);
                    }
                    else
                    {
                        AddDockItem(TabControls.DebtorOfferPage2, api, Uniconta.ClientTools.Localization.lookup("Offers"), "Add_16x16", true);
                    }
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    if (dgDebtorOffers.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgDebtorOffers.masterRecord };
                        AddDockItem(TabControls.DebtorOfferPage2, arr, salesHeader);
                    }
                    else
                    {
                        AddDockItem(TabControls.DebtorOfferPage2, selectedItem, salesHeader);
                    }
                    break;
                case "OfferLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OfferLine"), selectedItem._OrderNumber, selectedItem._DCAccount);
                    AddDockItem(TabControls.DebtorOfferLines, dgDebtorOffers.syncEntity, olheader);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("UserNotesInfo"), selectedItem.Account);
                        AddDockItem(TabControls.UserNotesPage, dgDebtorOffers.syncEntity, header);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.Account);
                        AddDockItem(TabControls.UserDocsPage, dgDebtorOffers.syncEntity, header);
                    }
                    break;
                case "Contacts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ContactPage, selectedItem);
                    break;
                case "ConvertOfferToOrder":
                    if (selectedItem != null)
                        ConvertOfferToOrder(selectedItem);
                    break;
                case "PrintOffer":
                    if (selectedItem != null)
                        PrintOffer(selectedItem);
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                    {
                        CWOrderFromOrder cwOrderFromOrder = new CWOrderFromOrder(api);
                        cwOrderFromOrder.DialogTableId = 2000000026;
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
                                var copyDelAddress = cwOrderFromOrder.copyDeliveryAddress;
                                var dcOrder = cwOrderFromOrder.dcOrder;
                                bool NewOrder = (dcOrder.RowId == 0);
                                dcOrder._DeliveryDate = cwOrderFromOrder.DeliveryDate;
                                var reCalPrice = cwOrderFromOrder.reCalculatePrice;
                                var result = await orderApi.CreateOrderFromOrder(selectedItem, dcOrder, account, inversign, CopyAttachments: copyAttachment, CopyDeliveryAddress: copyDelAddress, RecalculatePrices: reCalPrice, OrderPerPurchaseAccount: perSupplier);
                                busyIndicator.IsBusy = false;
                                if (result != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(result);
                                else
                                    CreditorOrders.ShowOrderLines(NewOrder ? (byte)3 : (byte)0, dcOrder, this, dgDebtorOffers);
                            }
                        };
                        cwOrderFromOrder.Show();
                    }
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CrmFollowUpPage, dgDebtorOffers.syncEntity);
                    break;
                case "EditAll":
                    if (dgDebtorOffers.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "UndoDelete":
                    dgDebtorOffers.UndoDeleteRow();
                    break;
                case "DeleteRow":
                    dgDebtorOffers.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgDebtorOffers.HasUnsavedData;
            }
        }

        private async void Save()
        {
            busyIndicator.IsBusy = true;
            if (dgDebtorOffers.HasUnsavedData)
                await dgDebtorOffers.SaveData();
            busyIndicator.IsBusy = false;
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var iBase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (iBase == null) return;

            if (dgDebtorOffers.Readonly)
            {
                dgDebtorOffers.MakeEditable();
                UserFieldControl.MakeEditable(dgDebtorOffers);
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
                                await dgDebtorOffers.SaveData();
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgDebtorOffers.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgDebtorOffers.Readonly = true;
                        dgDebtorOffers.tableView.CloseEditor();
                        iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgDebtorOffers.Readonly = true;
                    dgDebtorOffers.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "UndoDelete", "DeleteRow", "SaveGrid" });
                }
            }
        }

        private void PrintOffer(DebtorOfferClient dbOrder)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var debtor = dbOrder.Debtor;
            bool showSendByMail = true;
            string debtorName;
            if (debtor != null)
            {
                debtorName = debtor._Name ?? dbOrder._DCAccount;
                showSendByMail = (!string.IsNullOrEmpty(debtor.InvoiceEmail) || debtor.EmailDocuments);
            }
            else if (dbOrder._Prospect == 0)
            {
                UtilDisplay.ShowErrorCode(ErrorCodes.AccountIsMissing);
                return;
            }
            else
            {
                debtorName = Uniconta.ClientTools.Localization.lookup("Prospect");
                showSendByMail = true;
            }

            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, CompanyLayoutType.Offer.ToString(), askForEmail: true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isDebtorOrder: true);
            GenrateOfferDialog.DialogTableId = 2000000006;
            GenrateOfferDialog.Closed += async delegate
            {
                if (GenrateOfferDialog.DialogResult == true)
                {
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.Offer, GenrateOfferDialog.GenrateDate, null, true, GenrateOfferDialog.ShowInvoice, GenrateOfferDialog.PostOnlyDelivered,
                        GenrateOfferDialog.InvoiceQuickPrint, GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, GenrateOfferDialog.SendByOutlook, GenrateOfferDialog.sendOnlyToThisEmail,
                        GenrateOfferDialog.Emails, false, null, false);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (!result)
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOffers);
                    else
                        DebtorOrders.Updatedata(dbOrder, CompanyLayoutType.Offer);
                }
            };
            GenrateOfferDialog.Show();
        }

        void ConvertOfferToOrder(DebtorOfferClient offerclient)
        {
            CwOffertoOrder cwOfferToOrder = new CwOffertoOrder();
            cwOfferToOrder.DialogTableId = 2000000044;
            cwOfferToOrder.Closed += async delegate
            {
                if (cwOfferToOrder.DialogResult == true)
                {
                    var odrApi = new OrderAPI(api);
                    DebtorOrder debtorOrder = api.CompanyEntity.CreateUserType<DebtorOrderClient>();
                    ErrorCodes res = await odrApi.ConvertOfferToOrder(offerclient, debtorOrder, cwOfferToOrder.KeepOffer);
                    if (res == ErrorCodes.Succes)
                    {
                        if (!cwOfferToOrder.KeepOffer)
                            dgDebtorOffers.RemoveFocusedRowFromGrid();
                        var text = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup("SalesOrderCreated"), Uniconta.ClientTools.Localization.lookup("OrderNumber"), debtorOrder._OrderNumber,
                         Uniconta.ClientTools.Localization.lookup("Account"), debtorOrder._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Orderline")), " ?"));
                        var select = UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.YesNo);
                        if (select == MessageBoxResult.Yes)
                            AddDockItem(TabControls.DebtorOrderLines, debtorOrder, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), debtorOrder._OrderNumber, debtorOrder._DCAccount));
                    }
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            cwOfferToOrder.Show();
        }

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorOfferPage2)
                dgDebtorOffers.UpdateItemSource(argument);
            else if (screenName == TabControls.DebtorOrderLines)
            {
                var debitOffer = argument as DebtorOfferClient;
                if (debitOffer == null)
                    return;
                var err = await api.Read(debitOffer);
                if (err == ErrorCodes.CouldNotFind)
                    dgDebtorOffers.UpdateItemSource(3, debitOffer);
                else if (err == ErrorCodes.Succes)
                    dgDebtorOffers.UpdateItemSource(2, debitOffer);
            }
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var offer = (sender as Image).Tag as DebtorOfferClient;
            if (offer != null)
                AddDockItem(TabControls.UserDocsPage, dgDebtorOffers.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var offer = (sender as Image).Tag as DebtorOfferClient;
            if (offer != null)
                AddDockItem(TabControls.UserNotesPage, dgDebtorOffers.syncEntity);
        }
    }
}
