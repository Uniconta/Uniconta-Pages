using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using DevExpress.Xpf.Editors;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectPage2 : FormBasePage
    {
        ProjectClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override Type TableType { get { return typeof(ProjectClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ProjectClient)value; } }
        bool isCopiedRow = false;
        Uniconta.DataModel.Debtor Debtor;
        ContactClient Contact;
        ProjectClient Project;
        bool lookupZipCode = true;

        public ProjectPage2(UnicontaBaseEntity sourcedata, bool IsEdit)
            : base(sourcedata, IsEdit)
        {
            InitializeComponent();
            isCopiedRow = !IsEdit;
            InitPage(api);
        }
        public ProjectPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtNumber, txtNumber);
#endif
        }

        public ProjectPage2(UnicontaBaseEntity sourcedata, UnicontaBaseEntity master)
            : base(sourcedata, true)
        {
            InitializeComponent();
            if (master != null)
                Debtor = master as DebtorClient;
            InitPage(api);
        }

        public ProjectPage2(CrudAPI crudApi, UnicontaBaseEntity master)
         : base(crudApi, "")
        {
            InitializeComponent();
            if (master != null)
                Debtor = master as DebtorClient;
            InitPage(crudApi);
            FocusManager.SetFocusedElement(txtNumber, txtNumber);
        }

        void InitPage(CrudAPI crudapi)
        {
            StartLoadCache();
            dAddress.Header = Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            lePersonInCharge.api = lePurchaser.api = lePrStandard.api = leAccount.api = lePayment.api = leVat.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api =
                leGroup.api = leMasterProject.api = lePrType.api = leInstallation.api = lePriceList.api = crudapi;
            Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                if (!isCopiedRow)
                {
                    editrow = CreateNew() as ProjectClient;
                    if (Debtor != null)
                    {
                        editrow.SetMaster(Debtor);
                        leAccount.IsEnabled = false;
                    }
                    if (Contact != null)
                    {
                        editrow.SetMaster(Contact);
                        leAccount.IsEnabled = txtName.IsEnabled = cmbContactName.IsEnabled = false;
                    }
                    else
                        editrow.SetMaster(crudapi.CompanyEntity);
                }
            }

            if (editrow != null)
                BindContact(editrow.Debtor);

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            editrow.PropertyChanged += Editrow_PropertyChanged;
            liContactName.ButtonClicked += LiContactName_ButtonClicked;
            var Comp = api.CompanyEntity;
            if (!Comp.Contacts)
                cmbContactName.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void LiContactName_ButtonClicked(object sender)
        {
            if (editrow.Debtor == null)
                return;
            var contactClient = api.CompanyEntity.CreateUserType<ContactClient>();
            var debt = editrow.Debtor;
            api.SetMaster(contactClient, debt);
            AddDockItem(TabControls.ContactPage2, new object[] { contactClient, false, debt }, string.Format("{0} : {1},{2}",
                Uniconta.ClientTools.Localization.lookup("Contacts"), editrow.Number, debt.Name), "Add_16x16");
        }

        async void BindContact(Debtor debtor)
        {
            if (debtor == null) return;

            var cache = api.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact));
            SetContactSource(cache, debtor);
            cmbContactName.DisplayMember = "KeyName";

            if (editrow == null) return;

            var contactRefId = editrow._ContactRef;
            if (contactRefId != 0)
            {
                var contact = cache.Get(contactRefId);
                if (contact == null)
                {
                    cache = api.LoadCache(typeof(Uniconta.DataModel.Contact), true).GetAwaiter().GetResult();
                    contact = cache.Get(contactRefId);
                    SetContactSource(cache, debtor);
                }
                cmbContactName.SelectedItem = contact;
                if (contact == null)
                {
                    editrow._ContactRef = 0;
                    editrow.ContactName = null;
                }
            }
        }

        void SetContactSource(SQLCache cache, Debtor debtor)
        {
            cmbContactName.ItemsSource = cache != null ? new ContactCacheFilter(cache, 1, debtor._Account) : null;
        }
        protected override void AfterTemplateSet(UnicontaBaseEntity row)
        {
            if (this.Debtor != null)
                (row as ProjectClient).SetMaster(this.Debtor);
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZipCode")
            {
                if (lookupZipCode)
                {
                    var city = await UtilDisplay.GetCityAndAddress(editrow.ZipCode, editrow._WorkCountry != 0 ? editrow._WorkCountry : api.CompanyEntity._CountryId);
                    if (city != null)
                    {
                        editrow.City = city[0];
                        var add1 = city[1];
                        if (!string.IsNullOrEmpty(add1))
                            editrow.WorkAddress1 = add1;
                        var zip = city[2];
                        if (!string.IsNullOrEmpty(zip))
                        {
                            lookupZipCode = false;
                            editrow.ZipCode = zip;
                        }
                    }
                }
                else
                    lookupZipCode = true;
            }
        }
        
#if !SILVERLIGHT
        private void Email_ButtonClicked(object sender)
        {
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }

        private void LiZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._WorkAddress1 + "+" + editrow._WorkAddress2 + "+" + editrow._WorkAddress3 + "+" + editrow._ZipCode + "+" + editrow._City + "+" + editrow.WorkCountry;
            Utility.OpenGoogleMap(location);
        }
#endif

        private void leAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var debtors = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            var debtor = (Uniconta.DataModel.Debtor)debtors?.Get(Convert.ToString(e.NewValue));
            if (debtor == null)
                return;
            if (debtor._Blocked)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AccountIsBlocked"), Uniconta.ClientTools.Localization.lookup("Information"));
                return;
            }
            editrow?.SetMaster(debtor);
            if (installationCache != null)
            {
                leInstallation.cacheFilter = new AccountCacheFilter(installationCache, 1, debtor._Account);
                leInstallation.InvalidCache();
            }

            if (editrow != null)
            {
                string debAcc = Convert.ToString(e.OldValue);
                string id = Convert.ToString(e.NewValue);
                if (id != editrow._DCAccount ||
                    editrow.RowId == 0 ||
                    (!string.IsNullOrEmpty(debAcc) && debAcc != id) ||
                    ((LoadedRow as DCOrder)?._DCAccount != id))
                {
                    if (leAccount.IsEnabled)
                    {
                        editrow.Installation = null;
                        SetFieldFromDebtor(editrow, (Uniconta.DataModel.Debtor)api.GetCache(typeof(Uniconta.DataModel.Debtor))?.Get(id));
                    }
                }
            }
        }

        async void SetFieldFromDebtor(ProjectClient editrow, Debtor debtor)
        {
            TableField.SetUserFieldsFromRecord(debtor, editrow);
            BindContact(debtor);
            layoutItems.DataContext = null;
            layoutItems.DataContext = editrow;
            await api.Read(debtor);
        }
        private void cmbContactName_EditValueChanged(object sender, EditValueChangedEventArgs e)
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

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ContactPage2 && argument != null)
            {
                BindContact(editrow.Debtor);

                if (argument is object[] arg && arg.Length == 2)
                    cmbContactName.SelectedItem = arg[arg.Length - 1];
            }
        }

        SQLCache installationCache;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;

            if (api.CompanyEntity.DeliveryAddress)
            {
                installationCache = api.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await api.LoadCache(typeof(Uniconta.DataModel.WorkInstallation)).ConfigureAwait(false);
                if (editrow?._DCAccount != null)
                    leInstallation.cacheFilter = new AccountCacheFilter(installationCache, 1, editrow._DCAccount);
            }
            LoadType(typeof(Uniconta.DataModel.Debtor));
        }

        private void cmbContactName_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var selectedItem = cmbContactName.SelectedItem as Contact;
            GoToContact(selectedItem, e.Key);
        }

        private void lblInstallation_ButtonClicked(object sender)
        {
            var selectedInstallation = leInstallation.SelectedItem as WorkInstallationClient;
            if (selectedInstallation != null)
                CopyAddressToRow(selectedInstallation._Name, selectedInstallation._Address1, selectedInstallation._Address2, selectedInstallation._Address3, selectedInstallation._ZipCode, selectedInstallation._City, selectedInstallation._Country);
        }

        private void CopyAddressToRow(string name, string address1, string address2, string address3, string zipCode, string city, CountryCode? country)
        {
            var row = this.editrow;
            row.WorkAddress1 = address1;
            row.WorkAddress2 = address2;
            row.WorkAddress3 = address3;
            row.City = city;
            if (row.ZipCode != zipCode)
            {
                lookupZipCode = false;
                row.ZipCode = zipCode;
            }
            row.WorkCountry = country;
        }
    }
}
