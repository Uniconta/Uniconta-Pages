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
    public partial class InvTransferOrderPage2 : FormBasePage
    {
        InvTransferOrderClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

        public override Type TableType { get { return typeof(InvTransferOrderClient); } }
        public override string NameOfControl { get { return TabControls.InvTransferOrderPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (InvTransferOrderClient)value; } }
        CreditorClient Creditor;
        ContactClient Contact;
        bool lookupZipCode = true;
        public InvTransferOrderPage2(UnicontaBaseEntity sourcedata, UnicontaBaseEntity master) /* called for edit from particular account */
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
        public InvTransferOrderPage2(CrudAPI crudApi, UnicontaBaseEntity master) /* called for edit from particular account */
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
        public InvTransferOrderPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public InvTransferOrderPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
        }

        void InitPage(CrudAPI crudapi)
        {
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            Employeelookupeditor.api = lePayment.api = cmbDim1.api
                 = leTransType.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = leGroup.api = leShipment.api =
                 PrCategorylookupeditor.api = Projectlookupeditor.api = leApprover.api = leDeliveryTerm.api =
                 leVat.api = prTasklookupeditor.api = lePrWorkSpace.api = leCompanyAddress.api = leDeliveryAddress.api = crudapi;

            leRelatedOrder.CrudApi = crudapi;

            cbDeliveryCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            if (editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                liCreatedTime.Visibility = Visibility.Collapsed;
                editrow = CreateNew() as InvTransferOrderClient;
                editrow._Created = DateTime.MinValue;
                if (Creditor != null)
                {
                    editrow.SetMaster(this.Creditor);
                    if (editrow.RowId == 0)
                        SetValuesFromMaster(this.Creditor);
                }
                if (Contact != null)
                    editrow.SetMaster(Contact);

            }
            AdjustLayout();

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            editrow.PropertyChanged += Editrow_PropertyChanged;
            if (crudapi.GetCache(typeof(Uniconta.DataModel.Creditor)) == null)
                crudapi.LoadCache(typeof(Uniconta.DataModel.Creditor));
        }

        int contactRefId;

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DeliveryZipCode")
            {
                if (lookupZipCode)
                {
                    var deliveryCountry = editrow.DeliveryCountry?? CountryCode.Unknown;
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
            var editrow = (row as InvTransferOrderClient);
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
        SQLCache installationCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (Comp.DeliveryAddress)
            {
                installationCache = Comp.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await api.LoadCache(typeof(Uniconta.DataModel.WorkInstallation)).ConfigureAwait(false);
                if (editrow._DCAccount != null)
                    leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, 2, editrow._DCAccount);
            }
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

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorAccountPage2)
            {
                var args = argument as object[];
                if (args[2] == this.ParentControl)
                {
                    var dc = args[3] as CreditorClient;
                    editrow.SetMaster(dc);
                }
            }

            if (screenName == TabControls.AttachVoucherGridPage && argument != null)
            {
                var voucherObj = argument as object[];
                attachedVoucher = voucherObj[0] as VouchersClient;
                if (attachedVoucher != null)
                {
                    var openedFrom = voucherObj[1];
                    if (openedFrom == this.ParentControl)
                        editrow.DocumentRef = attachedVoucher.RowId;
                }
            }
        }

        VouchersClient attachedVoucher;
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    //if (Utility.IsExecuteWithBlockedAccount(editrow.Creditor))
                    //{
                    //    UpdateVoucher();
                        frmRibbon_BaseActions(ActionType);
                    //}
                    break;
                case "SaveAndOrderLines":
                    //if (Utility.IsExecuteWithBlockedAccount(editrow.Creditor))
                        saveFormAndOpenControl(TabControls.InvTransferOrderLines);
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
            if (installationCache != null)
            {
                leDeliveryAddress.cacheFilter = new AccountCacheFilter(installationCache, 2, creditor._Account);
                leDeliveryAddress.InvalidCache();
            }
            await api.Read(creditor);
            editrow.RefreshBalance();
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
            DebtorOrdersPage2.CheckOrderNo(sender as TextEdit, typeof(InvTransferOrder), api, originalOrderNumber);
        }

        private void lblDeliveryAddress_ButtonClicked(object sender)
        {
            var selectedInstallation = leDeliveryAddress.SelectedItem as WorkInstallationClient;
            if (selectedInstallation != null)
            {
                CopyAddressToRow(selectedInstallation._Name, selectedInstallation._Address1, selectedInstallation._Address2, selectedInstallation._Address3, selectedInstallation._ZipCode, selectedInstallation._City, selectedInstallation._Country);
                editrow.DeliveryContactPerson = selectedInstallation._ContactPerson;
                editrow.DeliveryContactEmail = selectedInstallation._ContactEmail;
                editrow.DeliveryPhone = selectedInstallation._Phone;
                if (selectedInstallation._DeliveryTerm != null)
                    editrow.DeliveryTerm = selectedInstallation._DeliveryTerm;
            }
        }
    }
}
