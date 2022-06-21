using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class CreditorMultiInvoiceGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorOrderClient); } }
        public override IComparer GridSorting { get { return new DCOrderSort(); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool CanInsert { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
    }

    /// <summary>
    /// Interaction logic for CreditorMultiInvoicePage.xaml
    /// </summary>
    public partial class CreditorMultiInvoicePage : GridBasePage
    {

        public override string NameOfControl
        {
            get { return TabControls.CreditorMultiInvoicePage.ToString(); }
        }

        public override bool IsDataChaged { get { return false; } }
        public CreditorMultiInvoicePage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgMultiInvGrid;
            SetRibbonControl(localMenu, dgMultiInvGrid);
            dgMultiInvGrid.api = api;
            dgMultiInvGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;

            dgMultiInvGrid.ShowTotalSummary();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
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
        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var order = dg.SelectedItem as CreditorOrderClient;
            if (order == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "Account")
                lookup.TableType = typeof(Uniconta.DataModel.Creditor);
            else if (dg.CurrentColumn?.Name == "OrderNumber")
                lookup.TableType = typeof(Uniconta.DataModel.CreditorOrder);
            return lookup;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgMultiInvGrid.SelectedItem as CreditorOrderClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    string salesHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
                    if (dgMultiInvGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgMultiInvGrid.masterRecord };
                        AddDockItem(TabControls.CreditorOrdersPage2, arr, salesHeader);
                    }
                    else
                    {
                        AddDockItem(TabControls.CreditorOrdersPage2, selectedItem, salesHeader);
                    }
                    break;
                case "DeleteRow":
                    dgMultiInvGrid.RemoveFocusedRowFromGrid();
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem._OrderNumber, selectedItem.Name);
                    AddDockItem(TabControls.CreditorOrderLines, dgMultiInvGrid.syncEntity, olheader);
                    break;
                case "GenerateInvoice":
                    if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                        GenerateInvoice();
                    else
                        UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
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
                    busyIndicator.IsBusy = true;
                    ViewVoucher(TabControls.VouchersPage3, selectedItem);
                    busyIndicator.IsBusy = false;
                    break;

                case "ImportVoucher":
                    if (selectedItem == null)
                        return;
                    var voucher = new VouchersClient();
                    voucher._Content = ContentTypes.PurchaseInvoice;
                    voucher._PurchaseNumber = selectedItem._OrderNumber;
                    voucher._CreditorAccount = selectedItem._InvoiceAccount ?? selectedItem._DCAccount;
                    CWAddVouchers addVouvhersDialog = new CWAddVouchers(api, false, voucher);
                    addVouvhersDialog.Closed += delegate
                    {
                        if (addVouvhersDialog.DialogResult == true)
                        {
                            if (addVouvhersDialog.VoucherRowIds.Length > 0)
                                selectedItem._DocumentRef = addVouvhersDialog.VoucherRowIds[0];
                        }
                    };
                    addVouvhersDialog.Show();
                    break;
                case "RemoveVoucher":
                    if (selectedItem != null)
                        RemoveVoucher(selectedItem);
                    break;
                case "UpdateRequisition":
                    UpdateDocument(CompanyLayoutType.Requisition);
                    break;
                case "UpdatePurchaseOrder":
                    UpdateDocument(CompanyLayoutType.PurchaseOrder);
                    break;
                case "UpdateDeliveryNote":
                    UpdateDocument(CompanyLayoutType.PurchasePacknote);
                    break;
                case "RecalculateOrderPrices":
                    RecalculateOrderPrices();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
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

        bool IsMassUpdateTaskInProgress = false;

        private void GenerateInvoice()
        {
            if (IsMassUpdateTaskInProgress)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateInBackground"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, false, isQuickPrintVisible: false);
            GenrateInvoiceDialog.DialogTableId = 2000000000;
            GenrateInvoiceDialog.HideOutlookOption(true);
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    try
                    {
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                        busyIndicator.IsBusy = true;
                        MultiDocumentTaskProgress(true, CompanyLayoutType.PurchaseInvoice);
                        var crVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<CreditorOrderClient>;

                        var postingprintGenerator = new InvoicePostingPrintGenerator(api, this);
                        postingprintGenerator.SetUpInvoicePosting(crVisibleOrders, GenrateInvoiceDialog.GenrateDate, GenrateInvoiceDialog.IsSimulation, CompanyLayoutType.PurchaseInvoice, false, false, false,
                            GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails, false);
                        await postingprintGenerator.Execute();
                        busyIndicator.IsBusy = false;
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                    }
                    finally
                    {
                        MultiDocumentTaskProgress(false, CompanyLayoutType.PurchaseInvoice);
                    }
                }
            };
            GenrateInvoiceDialog.Show();
        }

        private void UpdateDocument(CompanyLayoutType documentType)
        {

            if (IsMassUpdateTaskInProgress)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateInBackground"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            bool showUpdateInv = api.CompanyEntity.Storage;
            var generateDoc = new CWGenerateInvoice(false, documentType.ToString(), false, true, true, false, isQuickPrintVisible: false, isShowUpdateInv: showUpdateInv);
            generateDoc.DialogTableId = 2000000000;
            generateDoc.HideOutlookOption(true);
            generateDoc.Closed += async delegate
             {
                 if (generateDoc.DialogResult == true)
                 {
                     try
                     {
                         busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                         busyIndicator.IsBusy = true;
                         MultiDocumentTaskProgress(true, documentType);

                         var crVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<CreditorOrderClient>;
                         var invoicePostingPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                         invoicePostingPrintGenerator.SetUpInvoicePosting(crVisibleOrders, generateDoc.GenrateDate, !generateDoc.UpdateInventory, documentType, false, false, false,
                             generateDoc.SendByEmail, generateDoc.sendOnlyToThisEmail, generateDoc.Emails, false);

                         await invoicePostingPrintGenerator.Execute();
                         busyIndicator.IsBusy = false;
                     }
                     catch (Exception ex)
                     {
                         UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                     }
                     finally
                     {
                         MultiDocumentTaskProgress(false, documentType);
                     }
                 }
             };
            generateDoc.Show();
        }

        private void MultiDocumentTaskProgress(bool status, CompanyLayoutType documentType)
        {
            IsMassUpdateTaskInProgress = status;

            string actionButton;
            switch (documentType)
            {
                case CompanyLayoutType.PurchaseOrder:
                    IsMassUpdateTaskInProgress = status;
                    actionButton = "UpdatePurchaseOrder";
                    break;
                case CompanyLayoutType.PurchasePacknote:
                    IsMassUpdateTaskInProgress = status;
                    actionButton = "UpdateDeliveryNote";
                    break;
                case CompanyLayoutType.Requisition:
                    IsMassUpdateTaskInProgress = status;
                    actionButton = "UpdateRequisition";
                    break;
                default:
                    IsMassUpdateTaskInProgress = status;
                    actionButton = "GenerateInvoice";
                    break;
            }

            if (status)
                ribbonControl?.DisableButtons(new string[] { actionButton });
            else
                ribbonControl?.EnableButtons(new string[] { actionButton });
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AttachVoucherGridPage && argument != null)
            {
                var voucherObj = argument as object[];
                if (voucherObj != null && voucherObj.Length > 0)
                {
                    var attachedVoucher = voucherObj[0] as VouchersClient;
                    if (attachedVoucher != null)
                    {
                        var selectedItem = dgMultiInvGrid.SelectedItem as CreditorOrderClient;
                        if (selectedItem != null)
                        {
                            selectedItem.DocumentRef = attachedVoucher.RowId;
                            UpdateVoucher(attachedVoucher, selectedItem);
                        }
                    }
                }
            }
        }

        private void RecalculateOrderPrices()
        {
            var orderLst = dgMultiInvGrid.GetVisibleRows() as IEnumerable<CreditorOrderClient>;
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
