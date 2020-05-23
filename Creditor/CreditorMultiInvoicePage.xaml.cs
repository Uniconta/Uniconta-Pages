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

        VouchersClient attachedVoucher;
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
                    dgMultiInvGrid.DeleteRow();
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
                    CWAddVouchers addVouvhersDialog = new CWAddVouchers(api, false, voucher);
                    addVouvhersDialog.Closed += delegate
                    {
                        if (addVouvhersDialog.DialogResult == true)
                        {
                            var propertyInfo = selectedItem.GetType().GetProperty("DocumentRef");
                            if (propertyInfo != null && addVouvhersDialog.VoucherRowIds.Length > 0)
                            {
                                propertyInfo.SetValue(selectedItem, addVouvhersDialog.VoucherRowIds[0], null);
                                UpdateVoucher(selectedItem);
                            }
                        }
                    };
                    addVouvhersDialog.Show();
                    UpdateVoucher(selectedItem);
                    break;
                case "RemoveVoucher":
                    if (selectedItem == null)
                        return;
                    RemoveVoucher(selectedItem);
                    UpdateVoucher(selectedItem);
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

        private void GenerateInvoice()
        {
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, false, isQuickPrintVisible: false);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000000;
            GenrateInvoiceDialog.HideOutlookOption(true);
#endif
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;

                    var crVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<CreditorOrderClient>;

                    var postingprintGenerator = new InvoicePostingPrintGenerator(api, this);
                    postingprintGenerator.SetUpInvoicePosting(crVisibleOrders, GenrateInvoiceDialog.GenrateDate, GenrateInvoiceDialog.IsSimulation, CompanyLayoutType.PurchaseInvoice, false, false, false,
                        GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails, false);

                    await postingprintGenerator.Execute();
                    busyIndicator.IsBusy = false;
                }
            };
            GenrateInvoiceDialog.Show();
        }

        private void UpdateDocument(CompanyLayoutType documentType)
        {
            bool showUpdateInv = api.CompanyEntity.Storage;
            var generateDoc = new CWGenerateInvoice(false, documentType.ToString(), false, true, true, false, isQuickPrintVisible: false, isShowUpdateInv: showUpdateInv);
#if !SILVERLIGHT
            generateDoc.DialogTableId = 2000000000;
            generateDoc.HideOutlookOption(true);
#endif
            generateDoc.Closed += async delegate
             {
                 if (generateDoc.DialogResult == true)
                 {
                     busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                     busyIndicator.IsBusy = true;

                     var crVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<CreditorOrderClient>;

                     var invoicePostingPrintGenerator = new InvoicePostingPrintGenerator(api, this);
                     invoicePostingPrintGenerator.SetUpInvoicePosting(crVisibleOrders, generateDoc.GenrateDate, !generateDoc.UpdateInventory, documentType, false, false, false,
                         generateDoc.SendByEmail, generateDoc.sendOnlyToThisEmail, generateDoc.Emails, false);

                     await invoicePostingPrintGenerator.Execute();
                     busyIndicator.IsBusy = false;
                 }
             };
            generateDoc.Show();
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

                if (voucherObj[0] is VouchersClient)
                {
                    var selectedItem = dgMultiInvGrid.SelectedItem as CreditorOrderClient;
                    attachedVoucher = voucherObj[0] as VouchersClient;
                    selectedItem.DocumentRef = attachedVoucher.RowId;
                    UpdateAttachedVoucher(selectedItem._OrderNumber);
                    UpdateVoucher(selectedItem);
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
