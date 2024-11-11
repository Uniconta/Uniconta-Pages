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
using DevExpress.Xpf.Editors;
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
using Uniconta.Common.Utility;
using DevExpress.Data.TreeList;

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
        bool lookupZipCode = true;
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
            FocusManager.SetFocusedElement(leAccount, leAccount);
        }

        void InitPage(CrudAPI crudapi)
        {
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            lePostingAccount.api = Employeelookupeditor.api = leAccount.api = lePayment.api = cmbDim1.api
                = leTransType.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = leGroup.api = leShipment.api =
                PrCategorylookupeditor.api = Projectlookupeditor.api = leApprover.api = leDeliveryTerm.api = leInvoiceAccount.api =
                PriceListlookupeditior.api = leLayoutGroup.api = leVat.api = prTasklookupeditor.api = lePrWorkSpace.api = lePaymentFormat.api = 
                leCompanyAddress.api = crudapi;

            leRelatedOrder.CrudApi = crudapi;

            cbDeliveryCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            if (editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                liCreatedTime.Visibility = Visibility.Collapsed;
                editrow = CreateNew() as CreditorOrderClient;
                editrow._Created = DateTime.MinValue;
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
                    leAccount.IsEnabled = txtName.IsEnabled = cmbContactName.IsEnabled = false;
                }
            }
            else
                BindContact(editrow.Creditor);
            AdjustLayout();

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            AcItem.ButtonClicked += AcItem_ButtonClicked;
            liContactName.ButtonClicked += LiContactName_ButtonClicked;
            editrow.PropertyChanged += Editrow_PropertyChanged;
            if (crudapi.GetCache(typeof(Uniconta.DataModel.Creditor)) == null)
                crudapi.LoadCache(typeof(Uniconta.DataModel.Creditor));
        }

        private void LiContactName_ButtonClicked(object sender)
        {
            if (editrow.Creditor == null)
                return;

            var contactClient = api.CompanyEntity.CreateUserType<ContactClient>();
            var cred = editrow.Creditor;
            api.SetMaster(contactClient, cred);
            AddDockItem(TabControls.ContactPage2, new object[] { contactClient, false, cred }, string.Format("{0} : {1},{2}",
                Uniconta.ClientTools.Localization.lookup("Contacts"), editrow.OrderNumber, cred.Name), "Add_16x16");
        }

        int contactRefId;
        async private void BindContact(Uniconta.DataModel.Creditor creditor)
        {
            if (creditor == null) return;

            var cache = api.GetCache(typeof(Contact)) ?? await api.LoadCache(typeof(Contact));
            if (cache == null || cache.Count == 0) return;

            cmbContactName.ItemsSource = new ContactCacheFilter(cache, 2, creditor._Account);
            cmbContactName.DisplayMember = "KeyName";

            if (editrow != null && editrow._ContactRef != 0)
            {
                var contact = cache.Get(editrow._ContactRef);
                cmbContactName.SelectedItem = contact;
                if (contact == null)
                {
                    editrow._ContactRef = 0;
                    editrow.ContactName = null;
                }
            }
        }

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DeliveryZipCode")
            {
                if (lookupZipCode)
                {
                    var deliveryCountry = editrow.DeliveryCountry ?? editrow.Country;
                    var city = await UtilDisplay.GetCityAndAddress(editrow.DeliveryZipCode, deliveryCountry);
                    if (city != null)
                    {
                        editrow.DeliveryCity = city[0];
                        var add1 = city[1];
                        if (!string.IsNullOrEmpty(add1))
                            editrow.DeliveryAddress1 = add1;
                        var zip = city[2];
                        if (!string.IsNullOrEmpty(zip) && editrow.DeliveryZipCode != zip)
                        {
                            lookupZipCode = false;
                            editrow.DeliveryZipCode = zip;
                        }
                    }
                }
                else
                    lookupZipCode = true;
            }
        }
        protected override void AfterTemplateSet(UnicontaBaseEntity row)
        {
            var editrow = (row as CreditorOrderClient);
            if (this.editrow != null)
            {
                editrow._OrderNumber = this.editrow._OrderNumber;
                editrow.RowId = this.editrow.RowId;
            }
            if (this.Creditor != null)
                editrow.SetMaster(this.Creditor);
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
                grpProject.Visibility = Visibility.Collapsed;
            if (!Comp.Shipments)
            {
                itemShipment.Visibility = Visibility.Collapsed;
                liDeliveryTerm.Visibility = Visibility.Collapsed;
            }
            if (!Comp.ApprovePurchaseOrders)
                grpApproval.Visibility = Visibility.Collapsed;
            if (!Comp.SetupSizes)
                grpSize.Visibility = Visibility.Collapsed;

            if (!Comp.ProjectTask)
                projectTask.Visibility = Visibility.Collapsed;
            else if (editrow?._Project != null)
            {
                var project = Comp.GetCache(typeof(Uniconta.DataModel.Project))?.Get(editrow._Project) as ProjectClient;
                setTask(project);
            }
            if (!Comp.CreditorPrice)
                priceListLayoutItem.Visibility = Visibility.Collapsed;
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
                    if (string.IsNullOrEmpty(leAccount.EditValue as string))
                        leAccount.SelectedItem = dc;
                }
            }

            if (screenName == TabControls.AttachVoucherGridPage && argument != null)
            {
                var voucherObj = argument as object[];
                attachedVoucher = voucherObj[0] as VouchersClient;
                if (attachedVoucher != null)
                    editrow.DocumentRef = attachedVoucher.RowId;
            }

            if (screenName == TabControls.ContactPage2 & argument != null)
            {
                BindContact(editrow.Creditor);

                if (argument is object[] arg && arg.Length == 2)
                    cmbContactName.SelectedItem = arg[arg.Length - 1];
            }
        }

        VouchersClient attachedVoucher;
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    if (Utility.IsExecuteWithBlockedAccount(editrow.Creditor))
                    {
                        UpdateVoucher();
                        frmRibbon_BaseActions(ActionType);
                    }
                    break;
                case "SaveAndOrderLines":
                    if (Utility.IsExecuteWithBlockedAccount(editrow.Creditor))
                        saveFormAndOpenControl(TabControls.CreditorOrderLines);
                    break;
                case "RefVoucher":
                    var _refferedVouchers = new List<int>();
                    if (editrow._DocumentRef != 0)
                        _refferedVouchers.Add(editrow._DocumentRef);

                    AddDockItem(TabControls.AttachVoucherGridPage, new object[] { _refferedVouchers }, true);
                    break;
                case "ViewVoucher":
                    ViewVoucher(TabControls.VouchersPage3, editrow);
                    break;
                case "ImportVoucher":
                    var voucher = new VouchersClient();
                    voucher._Content = ContentTypes.PurchaseInvoice;
                    voucher._PurchaseNumber = editrow._OrderNumber;
                    voucher._CreditorAccount = editrow._InvoiceAccount ?? editrow._DCAccount;
                    Utility.ImportVoucher(editrow, api, voucher);
                    break;
                case "RemoveVoucher":
                    RemoveVoucher(editrow);
                    break;
                case "Delete":
                    if (editrow._OrderTotal != 0)
                    {
                        var msg = Uniconta.ClientTools.Localization.lookup("NumberOfLines") + ": " + NumberConvert.ToString(editrow._Lines);
                        var msg2 = msg + "\r\n" + string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), editrow._OrderNumber);
                        CWConfirmationBox dialog = new CWConfirmationBox(msg2, Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
                        dialog.Closing += delegate
                        {
                            if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                                frmRibbon_BaseActions(ActionType);
                        };
                        dialog.Show();
                    }
                    else
                        frmRibbon_BaseActions(ActionType);
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void UpdateVoucher()
        {
            var attachedVoucher = this.attachedVoucher;
            if (attachedVoucher == null)
                return;
            this.attachedVoucher = null;
            var buf = attachedVoucher._Data;
            attachedVoucher._Data = null;
            var org = StreamingManager.Clone(attachedVoucher);
            attachedVoucher._Content = ContentTypes.PurchaseInvoice;
            attachedVoucher._PurchaseNumber = editrow._OrderNumber;
            attachedVoucher._CreditorAccount = editrow._InvoiceAccount ?? editrow._DCAccount;
            api.UpdateNoResponse(org, attachedVoucher);
            attachedVoucher._Data = buf;
        }

        public override async void saveFormAndOpenControl(string Control, string header = null)
        {
            closePageOnSave = false;
            var res = await saveForm(false);
            closePageOnSave = true;
            if (res)
            {
                header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), editrow._OrderNumber, editrow._DCAccount);
                AddDockItem(Control, ModifiedRow, header);
                dockCtrl?.JustClosePanel(this.ParentControl);

                UpdateVoucher();
            }
        }
        private void AddCreditor_Click(object sender, RoutedEventArgs e)
        {
            AddDockItem(TabControls.CreditorAccountPage2, new object[2] { api, null }, Uniconta.ClientTools.Localization.lookup("Creditorsaccount"), "Add_16x16");
        }

        private void leAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            var creditors = api.GetCache(typeof(Uniconta.DataModel.Creditor));
            SetDefaultCompanyAddress();
            SetValuesFromMaster((Uniconta.DataModel.Creditor)creditors?.Get(id));
            
        }
        async void SetDefaultCompanyAddress()
        {
            if (editrow._CompanyAddress == null)
            {
                var cache = api.GetCache(typeof(CompanyAddress)) ?? await api.LoadCache(typeof(CompanyAddress));
                foreach (var ca in (CompanyAddress[])cache.GetNotNullArray)
                {
                    if (ca._Default)
                    {
                        editrow.CompanyAddress = ca.KeyStr;
                        break;
                    }
                }
            }
        }

        async void SetValuesFromMaster(Uniconta.DataModel.Creditor creditor)
        {
            if (creditor == null)
                return;
            var loadedOrder = LoadedRow as DCOrder;
            if (loadedOrder?._DCAccount == creditor._Account)
                return;
            editrow.SetMaster(creditor);
            layoutItems.DataContext = null;
            layoutItems.DataContext = editrow;
            if (!RecordLoadedFromTemplate || creditor._DeliveryAddress1 != null)
            {
                editrow.DeliveryName = creditor._DeliveryName;
                editrow.DeliveryAddress1 = creditor._DeliveryAddress1;
                editrow.DeliveryAddress2 = creditor._DeliveryAddress2;
                editrow.DeliveryAddress3 = creditor._DeliveryAddress3;
                editrow.DeliveryCity = creditor._DeliveryCity;
                if (editrow.DeliveryZipCode != creditor._DeliveryZipCode)
                {
                    lookupZipCode = false;
                    editrow.DeliveryZipCode = creditor._DeliveryZipCode;
                }
                if (creditor._DeliveryCountry != 0)
                    editrow.DeliveryCountry = creditor._DeliveryCountry;
                else
                    editrow.DeliveryCountry = null;
                editrow.DeliveryPhone = creditor._DeliveryPhone;
                editrow.DeliveryContactPerson = creditor._DeliveryContactPerson;
                editrow.DeliveryContactEmail = creditor._DeliveryContactEmail;
            }
            TableField.SetUserFieldsFromRecord(creditor, editrow);
            BindContact(creditor);
            await api.Read(creditor);
            editrow.RefreshBalance();
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

        private void Projectlookupeditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var selectedItem = Projectlookupeditor.SelectedItem as ProjectClient;
            setTask(selectedItem);
        }

        async private void setTask(ProjectClient master)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (master != null)
                    editrow.taskSource = master.Tasks ?? await master.LoadTasks(api);
                else
                    editrow.taskSource = api.GetCache(typeof(Uniconta.DataModel.ProjectTask));
                editrow.NotifyPropertyChanged("TaskSource");
                prTasklookupeditor.ItemsSource = editrow.TaskSource;
            }
        }

        private void prTasklookupeditor_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = Projectlookupeditor.SelectedItem as ProjectClient;
            setTask(selectedItem);
        }

        private void LiDeliveryZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._DeliveryAddress1 + "+" + editrow._DeliveryAddress2 + "+" + editrow._DeliveryAddress3 + "+" + editrow._DeliveryZipCode + "+" + editrow._DeliveryCity + "+" + editrow.DeliveryCountry;
            Utility.OpenGoogleMap(location);
        }

        private void cmbContactName_KeyDown(object sender, KeyEventArgs e)
        {
            var selectedItem = cmbContactName.SelectedItem as Contact;
            GoToContact(selectedItem, e.Key);
        }

        private void lblCompanyAddress_ButtonClicked(object sender)
        {
            var selectedAddress = leCompanyAddress.SelectedItem as CompanyAddressClient;
            if (selectedAddress != null)
            {
                CopyAddressToRow(selectedAddress._Name, selectedAddress._Address1, selectedAddress._Address2, selectedAddress._Address3, selectedAddress._ZipCode, selectedAddress._City, selectedAddress._Country);
                editrow.DeliveryContactPerson = selectedAddress._ContactPerson;
                editrow.DeliveryContactEmail = selectedAddress._ContactEmail;
                editrow.DeliveryPhone = selectedAddress._Phone;
            }
        }

        private void CopyAddressToRow(string name, string address1, string address2, string address3, string zipCode, string city, CountryCode? country)
        {
            var row = this.editrow;
            row.DeliveryName = name;
            row.DeliveryAddress1 = address1;
            row.DeliveryAddress2 = address2;
            row.DeliveryAddress3 = address3;
            row.DeliveryCity = city;
            if (row.DeliveryZipCode != zipCode)
            {
                lookupZipCode = false;
                row.DeliveryZipCode = zipCode;
            }
            row.DeliveryCountry = country;
        }

        private void OrderNumber_LostFocus(object sender, RoutedEventArgs e)
        {
            string originalOrderNumber = Convert.ToString((this.LoadedRow as DCOrder)?._OrderNumber);
            DebtorOrdersPage2.CheckOrderNo(sender as TextEdit, typeof(CreditorOrder), api, originalOrderNumber);
        }
    }
}
