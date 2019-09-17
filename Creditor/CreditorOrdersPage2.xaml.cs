using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.ClientTools.Util;
using UnicontaClient.Controls.Dialogs;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorOrdersPage2 : FormBasePage
    {
        CreditorOrderClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

        public override Type TableType { get { return typeof(CreditorOrderClient); } }
        public override string NameOfControl { get { return TabControls.CreditorOrdersPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CreditorOrderClient)value; } }
        CreditorClient Creditor;
        ContactClient Contact;
        public CreditorOrdersPage2(UnicontaBaseEntity sourcedata, UnicontaBaseEntity master) /* called for edit from particular account */
            : base(sourcedata, true)
        {
            InitializeComponent();
            if (master != null)
            {
                Creditor = master as CreditorClient;
                if (Creditor == null)
                {
                    Contact = master as ContactClient;
                    Creditor = Contact?.Creditor;
                }
            }
            InitPage(api);
        }
        public CreditorOrdersPage2(CrudAPI crudApi, UnicontaBaseEntity master) /* called for edit from particular account */
            : base(crudApi, "")
        {
            InitializeComponent();
            if (master != null)
            {
                Creditor = master as CreditorClient;
                if (Creditor == null)
                {
                    Contact = master as ContactClient;
                    Creditor = Contact?.Creditor;
                }
            }
            InitPage(api);
        }
        public CreditorOrdersPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public CreditorOrdersPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(leAccount, leAccount);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            lePostingAccount.api = Employeelookupeditor.api = leAccount.api = lePayment.api = cmbDim1.api
                = leTransType.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = leGroup.api = leShipment.api =
                PrCategorylookupeditor.api = Projectlookupeditor.api = leApprover.api = leDeliveryTerm.api = leInvoiceAccount.api =
                PriceListlookupeditior.api = leRelatedOrder.api = leLayoutGroup.api = crudapi;
            cbDeliveryCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            if (editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as CreditorOrderClient;
                editrow._Created = DateTime.MinValue;
                liCreatedTime.Visibility = Visibility.Collapsed;
                if (Creditor != null)
                {
                    editrow.SetMaster(this.Creditor);
                    if (editrow.RowId == 0)
                        SetValuesFromMaster(this.Creditor);
                    leAccount.IsEnabled = txtName.IsEnabled = false;
                }
                if (Contact != null)
                {
                    editrow.SetMaster(Contact);
                    leAccount.IsEnabled = txtName.IsEnabled = cmbContactName.IsEnabled= false;
                }
            }
            else
                BindContact(editrow.Creditor);
            AdjustLayout();

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            AcItem.ButtonClicked += AcItem_ButtonClicked;
#if !SILVERLIGHT
            editrow.PropertyChanged += Editrow_PropertyChanged;
#endif
            if (crudapi.GetCache(typeof(Uniconta.DataModel.Creditor)) == null)
                crudapi.LoadCache(typeof(Uniconta.DataModel.Creditor));
        }

        int contactRefId;
        async private void BindContact(Uniconta.DataModel.Creditor creditor)
        {
            if (creditor == null) return;

            contactRefId = editrow._ContactRef;
            var cache = api.GetCache(typeof(Contact)) ?? await api.LoadCache(typeof(Contact));
            var items = ((IEnumerable<Contact>)cache?.GetNotNullArray)?.Where(x => x._DCType == 2 && x._DCAccount == creditor._Account);
            cmbContactName.ItemsSource = items;
            cmbContactName.DisplayMember = "KeyName";

            if(contactRefId!=0 && items!=null)
            {
                var contact = items.Where(x => x.RowId == contactRefId).FirstOrDefault();
                cmbContactName.SelectedItem = contact;
                if(contact==null)
                {
                    editrow._ContactRef = 0;
                    editrow.ContactName = null;
                }
            }
        }

        string zip;
        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DeliveryZipCode")
            {
                if (zip == null)
                {
                    var deliveryCountry = editrow.DeliveryCountry ?? editrow.Country;
                    var city = await UtilDisplay.GetCityAndAddress(txtDelZipCode.Text, deliveryCountry);
                    if (city != null)
                    {
                        editrow.DeliveryCity = city[0];
                        var add1 = city[1];
                        if (!string.IsNullOrEmpty(add1))
                            editrow.DeliveryAddress1 = add1;
                        zip = city[2];
                        if (!string.IsNullOrEmpty(zip))
                            editrow.DeliveryZipCode = zip;
                    }
                }
                else
                    zip = null;
            }
        }
        protected override void OnLayoutCtrlLoaded()
        {
            AdjustLayout();
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (!Comp.DeliveryAddress)
                dAddress.Visibility = Visibility.Collapsed;
            if (!Comp.Project)
            {
                liProject.Visibility = Visibility.Collapsed;
                liPrCategory.Visibility = Visibility.Collapsed;
            }
            if (!Comp.Shipments)
                itemShipment.Visibility = Visibility.Collapsed;
        }
        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            parentGroup = lastGroup;
            return true;
        }

        public override void RowsPastedDone()
        { 
            if (Creditor != null)
                editrow.SetMaster(Creditor);
            if (Contact != null)
                editrow.SetMaster(Contact);
            SetValuesFromMaster(Creditor);
        }

        void AcItem_ButtonClicked(object sender)
        {
            AddCreditor_Click(null, null);
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorAccountPage2)
            {
                var args = argument as object[];
                if (args[2] == this.ParentControl)
                {
                    leAccount.LoadItemSource();
                    var dc = args[3] as CreditorClient;
                    editrow.SetMaster(dc);
                }
            }

            if (screenName == TabControls.AttachVoucherGridPage && argument != null)
            {
                var voucherObj = argument as object[];
                if (voucherObj[0] is VouchersClient)
                {
                    attachedVoucher = voucherObj[0] as VouchersClient;
                    editrow.DocumentRef = attachedVoucher.RowId;
                }
            }
        }

        VouchersClient attachedVoucher;
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "SaveAndOrderLines":
                    saveFormAndOpenControl(TabControls.CreditorOrderLines);
                    break;
                case "RefVoucher":
                    var _refferedVouchers = new List<int>();
                    if (editrow._DocumentRef != 0)
                        _refferedVouchers.Add(editrow._DocumentRef);

                    AddDockItem(TabControls.AttachVoucherGridPage, new object[] { _refferedVouchers }, true);

                    break;
                case "ViewVoucher":
                    busyIndicator.IsBusy = true;
                    ViewVoucher(TabControls.VouchersPage3, editrow);
                    busyIndicator.IsBusy = false;
                    break;

                case "ImportVoucher":
                    var voucher = new VouchersClient();
                    voucher._Content = ContentTypes.PurchaseInvoice;
                    Utility.ImportVoucher(editrow, api, voucher);
                    break;

                case "RemoveVoucher":
                    RemoveVoucher(editrow);
                    break;
                case "Save":
                    if (!Utility.IsExecuteWithBlockedAccount(editrow.Creditor)) return;
                    if (attachedVoucher != null) UpdateVoucher(editrow._OrderNumber);
                    frmRibbon_BaseActions(ActionType);
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void UpdateVoucher(int orderNumber)
        {
            busyIndicator.IsBusy = true;
            attachedVoucher.PurchaseNumber = orderNumber;
            var err = await api.Update(attachedVoucher);
            busyIndicator.IsBusy = false;
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
        }
        public override async void saveFormAndOpenControl(string Control, string header = null)
        {
            if (!Utility.IsExecuteWithBlockedAccount(editrow.Creditor)) return;

            closePageOnSave = false;
            var res = await saveForm(false);
            closePageOnSave = true;
            if (res)
            {
                header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), editrow._OrderNumber, editrow.Name);
                AddDockItem(Control, ModifiedRow, header);
                dockCtrl?.JustClosePanel(this.ParentControl);
            }
        }
        private void AddCreditor_Click(object sender, RoutedEventArgs e)
        {
            object[] param = new object[2];
            param[0] = api;
            param[1] = null;
            AddDockItem(TabControls.CreditorAccountPage2, param, Uniconta.ClientTools.Localization.lookup("Creditorsaccount"), ";component/Assets/img/Add_16x16.png");
        }

        private void leAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            var creditors = api.GetCache(typeof(Uniconta.DataModel.Creditor));
            SetValuesFromMaster((Uniconta.DataModel.Creditor)creditors?.Get(id));
        }
        void SetValuesFromMaster(Uniconta.DataModel.Creditor creditor)
        {
            if (creditor == null)
                return;
            var loadedOrder = LoadedRow as DCOrder;
            if (loadedOrder?._DCAccount == creditor._Account)
                return;
            editrow.Account = creditor._Account;
            editrow.SetCurrency(creditor._Currency);
            editrow.Payment = creditor._Payment;
            editrow.EndDiscountPct = creditor._EndDiscountPct;
            editrow.PostingAccount = creditor._PostingAccount;
            editrow._PaymentMethod = creditor._PaymentMethod;
            editrow.Shipment = creditor._Shipment;
            editrow.DeliveryTerm = creditor._DeliveryTerm;
            if (!RecordLoadedFromTemplate || creditor._DeliveryAddress1 != null)
            {
                editrow.DeliveryName = creditor._DeliveryName;
                editrow.DeliveryAddress1 = creditor._DeliveryAddress1;
                editrow.DeliveryAddress2 = creditor._DeliveryAddress2;
                editrow.DeliveryAddress3 = creditor._DeliveryAddress3;
                editrow.DeliveryZipCode = creditor._DeliveryZipCode;
                editrow.DeliveryCity = creditor._DeliveryCity;
                if (creditor._DeliveryCountry != 0)
                    editrow.DeliveryCountry = creditor._DeliveryCountry;
                else
                    editrow.DeliveryCountry = null;
            }
            TableField.SetUserFieldsFromRecord(creditor, editrow);
            BindContact(creditor);
            api.Read(creditor);
        }

        private void cmbContactName_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var selectedItem = cmbContactName.SelectedItem as Contact;
            if (selectedItem != null)
            {
                editrow._ContactRef = selectedItem.RowId;
                editrow.ContactName = selectedItem._Name;
            }
            else
            {
                editrow._ContactRef = 0;
                editrow.ContactName = null;
            }
        }

#if !SILVERLIGHT

        private void LiDeliveryZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._DeliveryAddress1 + "+" + editrow._DeliveryAddress2 + "+" + editrow._DeliveryAddress3 + "+" + editrow._DeliveryZipCode + "+" + editrow._DeliveryCity + "+" + editrow.DeliveryCountry;
            Utility.OpenGoogleMap(location);
        }
#endif

    }
}
