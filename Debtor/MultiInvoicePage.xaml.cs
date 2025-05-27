using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class MultiInvoiceGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(DebtorOrderClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }

        public override bool Readonly { get { return false; } }
    }

    public partial class MultiInvoicePage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.MultiInvoicePage.ToString(); }
        }

        public override bool IsDataChaged { get { return false; } }

        SQLCache items, debtors;

        public MultiInvoicePage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgMultiInvGrid;
            SetRibbonControl(localMenu, dgMultiInvGrid);
            dgMultiInvGrid.api = api;
            dgMultiInvGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgMultiInvGrid.ShowTotalSummary();

            //Same ribbon menu is used in Creditor MassUpdate page. Physical Voucher local menu is hided.
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "ViewVoucher", "RefVoucher", "ImportVoucher", "RemoveVoucher" });

            var Comp = api.CompanyEntity;
            this.debtors = Comp.GetCache(typeof(Debtor));
            this.items = Comp.GetCache(typeof(InvItem));
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var order = dg.SelectedItem as DebtorOrderClient;
            if (order == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "Account")
                lookup.TableType = typeof(Uniconta.DataModel.Debtor);
            else if (dg.CurrentColumn?.Name == "OrderNumber")
                lookup.TableType = typeof(Uniconta.DataModel.DebtorOrder);
            return lookup;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
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

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgMultiInvGrid.SelectedItem as DebtorOrderClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    string salesHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
                    if (dgMultiInvGrid.masterRecords != null)
                    {
                        AddDockItem(TabControls.DebtorOrdersPage2, new object[] { selectedItem, dgMultiInvGrid.masterRecord }, salesHeader);
                    }
                    else
                    {
                        AddDockItem(TabControls.DebtorOrdersPage2, selectedItem, salesHeader);
                    }
                    break;
                case "DeleteRow":
                    dgMultiInvGrid.DeleteRow();
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), selectedItem._OrderNumber, selectedItem._DCAccount);
                    AddDockItem(TabControls.DebtorOrderLines, dgMultiInvGrid.syncEntity, olheader);
                    break;
                case "GenerateInvoice":
                    if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                        GenerateInvoice();
                    else
                        UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    break;
                case "GenerateInvoice2":
                    if (selectedItem != null)
                    {
                        if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                        {
                            var debtor = ClientHelper.GetRef(selectedItem.CompanyId, typeof(Debtor), selectedItem._DCAccount) as Debtor;
                            if (debtor != null)
                            {
                                var InvoiceAccount = selectedItem._InvoiceAccount ?? debtor._InvoiceAccount;
                                if (InvoiceAccount != null)
                                    debtor = ClientHelper.GetRef(selectedItem.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                                if (debtor._PricesInclVat != selectedItem._PricesInclVat)
                                {
                                    var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                                    Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                                    if (confirmationMsgBox != MessageBoxResult.OK)
                                        return;
                                }
                            }
                            GenerateRecordInvoice(selectedItem);
                        }
                        else
                            UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    }
                    break;
                case "OrderConfirmation":
                    OrderConfirmation(CompanyLayoutType.OrderConfirmation);
                    break;
                case "PackNote":
                    OrderConfirmation(CompanyLayoutType.Packnote);
                    break;
                case "PickList":
                    PickList();
                    break;
                case "RecalculateOrderPrices":
                    RecalculateOrderPrices();
                    break;
                case "PostProjectOrder":
                    if (string.IsNullOrEmpty(selectedItem?._Project))
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
                        Utility.ShowJournalError(ledgerRes, dgMultiInvGrid, false);
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

        private void PickList()
        {
            if (IsMassUpdateTaskInProgress)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateInBackground"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            var cwPickingList = new CWGeneratePickingList(true);
            cwPickingList.DialogTableId = 2000000024;
            cwPickingList.Closed += async delegate
            {
                if (cwPickingList.DialogResult == true)
                {
                    try
                    {
                        var selectedDate = cwPickingList.SelectedDate;
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                        busyIndicator.IsBusy = true;
                        MultiDocumentTaskProgress(true, CompanyLayoutType.PickingList);
                        var dbVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
                        var invoicePostringPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                        invoicePostringPrintGenerator.SetUpInvoicePosting(dbVisibleOrders, selectedDate, false, CompanyLayoutType.PickingList, cwPickingList.ShowDocument, false, cwPickingList.PrintDocument,
                            cwPickingList.SendByEmail, cwPickingList.sendOnlyToThisEmail, cwPickingList.EmailList, false);

                        await invoicePostringPrintGenerator.Execute();
                        busyIndicator.IsBusy = false;
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                    }
                    finally
                    {
                        MultiDocumentTaskProgress(false, CompanyLayoutType.PickingList);
                    }
                }
            };
            cwPickingList.Show();
        }

        bool IsMassUpdateTaskInProgress = false;
        private void GenerateInvoice()
        {

            if (IsMassUpdateTaskInProgress)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateInBackground"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            bool isOrderOrQuickInv = api.CompanyEntity._CountryId == CountryCode.Germany ? false : true;
            var GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, isOrderOrQuickInv: isOrderOrQuickInv, isQuickPrintVisible: true, isPageCountVisible: false, isDebtorOrder: true);
            GenrateInvoiceDialog.DialogTableId = 2000000011;
            GenrateInvoiceDialog.HideOutlookOption(true);
            GenrateInvoiceDialog.SetInvPrintPreview(printPreview);

            if (!api.CompanyEntity._DeactivateSendNemhandel)
                GenrateInvoiceDialog.SentByEInvoice(api, null, true);

            GenrateInvoiceDialog.ShowAllowCredMax(api.CompanyEntity.AllowSkipCreditMax);
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    try
                    {
                        MultiDocumentTaskProgress(true, CompanyLayoutType.Invoice);
                        printPreview = GenrateInvoiceDialog.ShowInvoice;
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                        busyIndicator.IsBusy = true;
                        var InvoiceDate = GenrateInvoiceDialog.GenrateDate;
                        var dbVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
                        if (!(dbVisibleOrders is Array))
                            dbVisibleOrders = dbVisibleOrders.ToArray();
                        var invoicePostingPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                        invoicePostingPrintGenerator.SetUpInvoicePosting(dbVisibleOrders, InvoiceDate, GenrateInvoiceDialog.IsSimulation, CompanyLayoutType.Invoice, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered, GenrateInvoiceDialog.InvoiceQuickPrint,
                            GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.GenerateOIOUBLClicked);
                        if(api.CompanyEntity.AllowSkipCreditMax)
                            invoicePostingPrintGenerator.SetAllowCreditMax(GenrateInvoiceDialog.AllowSkipCreditMax);
                        await invoicePostingPrintGenerator.Execute();
                        busyIndicator.IsBusy = false;
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                    }
                    finally
                    {
                        MultiDocumentTaskProgress(false, CompanyLayoutType.Invoice);
                    }
                }
            };
            GenrateInvoiceDialog.Show();
        }

        private void MultiDocumentTaskProgress(bool status, CompanyLayoutType documentType)
        {
            IsMassUpdateTaskInProgress = status;

            string actionButton;
            switch (documentType)
            {
                case CompanyLayoutType.OrderConfirmation:
                    actionButton = "OrderConfirmation";
                    break;
                case CompanyLayoutType.Packnote:
                    actionButton = "PackNote";
                    break;
                case CompanyLayoutType.PickingList:
                    actionButton = "PickList";
                    break;
                default:
                    actionButton = "GenerateInvoice";
                    break;
            }

            if (status)
                ribbonControl?.DisableButtons(new string[] { actionButton });
            else
                ribbonControl?.EnableButtons(new string[] { actionButton });
        }

        static bool printPreview = true;
        private void OrderConfirmation(CompanyLayoutType docType)
        {

            if (IsMassUpdateTaskInProgress)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateInBackground"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(false, docType.ToString(), isShowInvoiceVisible: true, askForEmail: true, showInputforInvNumber: false,
                showInvoice: true, isShowUpdateInv: true, isQuickPrintVisible: true, isDebtorOrder: true, isPageCountVisible: false);
            GenrateInvoiceDialog.DialogTableId = 2000000012;
            GenrateInvoiceDialog.HideOutlookOption(true);
            GenrateInvoiceDialog.SetInvPrintPreview(printPreview);
            GenrateInvoiceDialog.ShowAllowCredMax(api.CompanyEntity.AllowSkipCreditMax);

            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    try
                    {
                        MultiDocumentTaskProgress(true, docType);
                        printPreview = GenrateInvoiceDialog.ShowInvoice;
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                        busyIndicator.IsBusy = true;
                        var InvoiceDate = GenrateInvoiceDialog.GenrateDate;
                        var updateStatus = GenrateInvoiceDialog.UpdateInventory;
                        var dgOrderVisible = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
                        var invoicePostingPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                        invoicePostingPrintGenerator.SetUpInvoicePosting(dgOrderVisible, InvoiceDate, !updateStatus, docType, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered,
                                GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails, false);
                        if (api.CompanyEntity.AllowSkipCreditMax)
                            invoicePostingPrintGenerator.SetAllowCreditMax(GenrateInvoiceDialog.AllowSkipCreditMax);

                        await invoicePostingPrintGenerator.Execute();

                        busyIndicator.IsBusy = false;
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                    }
                    finally
                    {
                        MultiDocumentTaskProgress(false, docType);
                    }
                }
            };
            GenrateInvoiceDialog.Show();
        }
        

        private void GenerateRecordInvoice(DebtorOrderClient dbOrder)
        {
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var debtor = dbOrder.Debtor;
            bool showSendByMail = true;
            if (debtor != null)
                showSendByMail = (!string.IsNullOrEmpty(debtor.InvoiceEmail) || debtor.EmailDocuments);
            else
                api.LoadCache(typeof(Debtor), true);

            string debtorName = debtor?.Name ?? dbOrder._DCAccount;
            bool invoiceInXML = debtor != null && debtor.IsPeppolSupported && debtor._einvoice;
            bool isOrderOrQuickInv = api.CompanyEntity._CountryId == CountryCode.Germany ? false : true;
            var GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isDebtorOrder: true, isOrderOrQuickInv: isOrderOrQuickInv, InvoiceInXML: invoiceInXML);
            GenrateInvoiceDialog.DialogTableId = 2000000013;

            if (!api.CompanyEntity._DeactivateSendNemhandel)
                GenrateInvoiceDialog.SentByEInvoice(api, UtilCommon.GetEndPoint(dbOrder, debtor, api));

            GenrateInvoiceDialog.ShowAllowCredMax(api.CompanyEntity.AllowSkipCreditMax);

            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var isSimulated = GenrateInvoiceDialog.IsSimulation;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.Invoice, GenrateInvoiceDialog.GenrateDate, null, isSimulated, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, !isSimulated && GenrateInvoiceDialog.SendByOutlook, GenrateInvoiceDialog.sendOnlyToThisEmail,
                        GenrateInvoiceDialog.Emails, GenrateInvoiceDialog.GenerateOIOUBLClicked, null, false);
                    if (api.CompanyEntity.AllowSkipCreditMax)
                        invoicePostingResult.SetAllowCreditMax(GenrateInvoiceDialog.AllowSkipCreditMax);

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = true;
                    busyIndicator.IsBusy = false;

                    if (!result)
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgMultiInvGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            if (this.debtors == null)
                this.debtors = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.WorkInstallation), typeof(Uniconta.DataModel.InvItem) });
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        private void RecalculateOrderPrices()
        {
            var orderLst = dgMultiInvGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
            if (orderLst == null || orderLst.Count() == 0)
                return;
            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            dialog.Closing += async delegate
            {
                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                {
                    busyIndicator.IsBusy = true;
                    OrderAPI orderApi = new OrderAPI(this.api);
                    var err = await orderApi.RecalcOrderPrices(orderLst);
                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(err);
                }
            };
            dialog.Show();
        }
    }
}
