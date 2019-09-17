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
                            if (addVouvhersDialog.VoucherRowIds.Count() > 0 && propertyInfo != null)
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
            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, isQuickPrintVisible: false);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000000;
#endif
            GenrateInvoiceDialog.tbShowInv.Visibility = Visibility.Collapsed;
            GenrateInvoiceDialog.chkShowInvoice.Visibility = Visibility.Collapsed;
            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;

                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    int cnt = 0;
                    List<string> errorList = new List<string>();
                    var crVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<CreditorOrderClient>;
                    foreach (var crOrder in crVisibleOrders)
                    {
                        var result = await Invapi.PostInvoice(crOrder, null, GenrateInvoiceDialog.GenrateDate, 0,
                            GenrateInvoiceDialog.IsSimulation, new CreditorInvoiceClient(),
                            new CreditorInvoiceLines(),
                            GenrateInvoiceDialog.SendByEmail, false, Emails: GenrateInvoiceDialog.Emails, OnlyToThisEmail: GenrateInvoiceDialog.sendOnlyToThisEmail);
                        if (result != null && result.Err == 0)
                            cnt++;
                        else
                        {
                            string error = string.Format("{0}:{1}", crOrder.OrderNumber, Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
                            errorList.Add(error);
                        }
                    }
                    busyIndicator.IsBusy = false;
                    string updatedMsg = Uniconta.ClientTools.Localization.lookup("Succes");
                    if (!GenrateInvoiceDialog.IsSimulation)
                        updatedMsg = string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), cnt, Uniconta.ClientTools.Localization.lookup("CreditorOrders"));
                    if (errorList.Count == 0)
                        UnicontaMessageBox.Show(updatedMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                    else
                    {
                        errorList[0] = string.Format("{0}/r/n{1}", updatedMsg, errorList[0]);
                        CWErrorBox errorDialog = new CWErrorBox(errorList.ToArray(), true);
                        errorDialog.Show();
                    }
                }
            };
            GenrateInvoiceDialog.Show();
        }

        private void UpdateDocument(CompanyLayoutType documentType)
        {
            bool showUpdateInv = api.CompanyEntity.Storage;
            var generateDoc = new CWGenerateInvoice(false, documentType.ToString(), false, true, true, isQuickPrintVisible:false ,isShowUpdateInv: showUpdateInv);
#if !SILVERLIGHT
            generateDoc.DialogTableId = 2000000000;
#endif
            generateDoc.tbShowInv.Visibility = Visibility.Collapsed;
            generateDoc.chkShowInvoice.Visibility = Visibility.Collapsed;
            generateDoc.Closed += async delegate
             {
                 if (generateDoc.DialogResult == true)
                 {
                     busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                     busyIndicator.IsBusy = true;

                     InvoiceAPI Invapi = new InvoiceAPI(api);
                     int cnt = 0;
                     List<string> errorList = new List<string>();
                     var crVisibleOrders = dgMultiInvGrid.GetVisibleRows() as IEnumerable<CreditorOrderClient>;
                     foreach (var crOrder in crVisibleOrders)
                     {
                         var result = await Invapi.PostInvoice(crOrder, null, generateDoc.GenrateDate, 0, !generateDoc.UpdateInventory, new CreditorInvoiceClient(), new CreditorInvoiceLines(),
                             generateDoc.SendByEmail, false, Emails: generateDoc.Emails, OnlyToThisEmail: generateDoc.sendOnlyToThisEmail);
                         if (result != null && result.Err == 0)
                             cnt++;
                         else
                         {
                             string error = string.Format("{0}:{1}", crOrder.OrderNumber, Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
                             errorList.Add(error);
                         }
                     }
                     busyIndicator.IsBusy = false;
                     string updatedMsg = string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), cnt, Uniconta.ClientTools.Localization.lookup("CreditorOrders"));

                     if (errorList.Count == 0)
                         UnicontaMessageBox.Show(updatedMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                     else
                     {
                         errorList[0] = string.Format("{0}/r/n{1}", updatedMsg, errorList[0]);
                         CWErrorBox errorDialog = new CWErrorBox(errorList.ToArray(), true);
                         errorDialog.Show();
                     }
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
    }
}
