using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Windows;
using Uniconta.ClientTools.Util;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class RegisterMileage : FormBasePage
    {
        SQLCache employeeCache, debtorCache, companyAddressCache;
        EmployeeRegistrationLineClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            if (DoNotSave)
                RefreshParams[1] = ModifiedRow;
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.RegisterMileage.ToString(); } }
        public override Type TableType { get { return typeof(EmployeeRegistrationLineClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (EmployeeRegistrationLineClient)value; } }
        /*For Edit*/
        public bool DoNotSave;
        public RegisterMileage(UnicontaBaseEntity sourcedata, bool isEdit = true)
            : base(sourcedata, isEdit)
        {
            StartLoadCache();
            InitializeComponent();
            InitPage(api);
        }

        void InitPage(CrudAPI crudapi)
        {
            layoutControl = layoutItems;
            leFromAccount.api = leToAccount.api = leProject.api = leFromCompanyAddress.api = leToCompanyAddress.api = crudapi;
            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as EmployeeRegistrationLineClient;
                editrow.SetMaster(api.CompanyEntity);
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            editrow.PropertyChanged += Editrow_PropertyChanged;
        }

        private async void frmRibbon_OnItemClicked(string ActionType)
        {
            if (DoNotSave && (ActionType == "Save" || ActionType == "Delete"))
            {
                MoveFocus();
                if (ActionType == "Save")
                    await ClosePage(1);
                else if (ActionType == "Delete")
                    await ClosePage(3);
                CloseDockItem();
                return;
            }
            frmRibbon_BaseActions(ActionType);
        }

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FromZipCode")
            {
                var city = await UtilDisplay.GetCityAndAddress(editrow.FromZipCode, api.CompanyEntity._CountryId);
                if (city != null)
                {
                    editrow.FromCity = city[0];
                    var add1 = city[1];
                    if (!string.IsNullOrEmpty(add1))
                        editrow.FromAddress1 = add1;
                    var zip = city[2];
                    if (!string.IsNullOrEmpty(zip))
                        editrow.FromZipCode = zip;
                }
            }
            else if (e.PropertyName == "ToZipCode")
            {
                var city = await UtilDisplay.GetCityAndAddress(editrow.ToZipCode, api.CompanyEntity._CountryId);
                if (city != null)
                {
                    editrow.ToCity = city[0];
                    var add1 = city[1];
                    if (!string.IsNullOrEmpty(add1))
                        editrow.ToAddress1 = add1;
                    var zip = city[2];
                    if (!string.IsNullOrEmpty(zip))
                        editrow.ToZipCode = zip;
                }
            }
        }

        protected async override Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            debtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            employeeCache = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Employee)).ConfigureAwait(false);
            companyAddressCache = api.GetCache(typeof(Uniconta.DataModel.CompanyAddress)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CompanyAddress)).ConfigureAwait(false);
        }
        private void LiFromZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._FromAddress1 + "+" + editrow._FromAddress2 + "+" + editrow._FromZipCode + "+" + editrow._FromCity;
            Utility.OpenGoogleMap(location);
        }

        private void LiToZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._ToAddress1 + "+" + editrow._ToAddress2 + "+" + editrow._ToZipCode + "+" + editrow._ToCity;
            Utility.OpenGoogleMap(location);
        }

        private void chkFromHome_Checked(object sender, RoutedEventArgs e)
        {
            editrow.FromCompanyAddress = null;
            editrow.FromAccount = null;
            editrow.FromWork = false;
            var emp = (Uniconta.DataModel.Employee)employeeCache.Get(editrow.Employee);
            SetFromAddress("", emp._Address1, emp._Address2, emp._ZipCode, emp._City, CountryCode.Unknown);
        }

        private void chkFromWork_Checked(object sender, RoutedEventArgs e)
        {
            editrow.FromCompanyAddress = null;
            editrow.FromAccount = null;
            editrow.FromHome = false;
            var comp = api.CompanyEntity;
            SetFromAddress(string.Empty, comp._Address1, comp._Address2, comp._Address3, string.Empty, comp._CountryId);
        }

        private void chkToHome_Checked(object sender, RoutedEventArgs e)
        {
            editrow.ToCompanyAddress = null;
            editrow.ToAccount = null;
            editrow.ToWork = false;
            var emp = (Uniconta.DataModel.Employee)employeeCache.Get(editrow.Employee);
            SetToAddress(string.Empty, emp._Address1, emp._Address2, emp._ZipCode, emp._City, CountryCode.Unknown);
        }

        private void chkToWork_Checked(object sender, RoutedEventArgs e)
        {
            editrow.ToCompanyAddress = null;
            editrow.ToAccount = null;
            editrow.ToHome = false;
            var comp = api.CompanyEntity;
            SetToAddress(string.Empty, comp._Address1, comp._Address2, comp._Address3, string.Empty, comp._CountryId);
        }

        private void leToAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var debtor = debtorCache.Get(e.NewValue.ToString()) as DebtorClient;
                SetToAddress(debtor?.Name, debtor?.Address1, debtor?.Address2, debtor?.ZipCode, debtor?.City, debtor?.Country ?? CountryCode.Unknown);
                editrow.ToHome = editrow.ToWork = false;
                editrow.ToCompanyAddress = null;
            }
        }

        private void leToCompanyAddress_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var compAddr = companyAddressCache.Get(e.NewValue.ToString()) as CompanyAddressClient;
                SetToAddress(string.Empty, compAddr?.Address1, compAddr?.Address2, compAddr?.ZipCode, compAddr?.City, compAddr?.Country ?? CountryCode.Unknown);
                editrow.ToHome = editrow.ToWork = false;
                editrow.ToAccount = null;
            }
        }

        private void leFromAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var debtor = debtorCache.Get(e.NewValue.ToString()) as DebtorClient;
                SetFromAddress(debtor?.Name, debtor?.Address1, debtor?.Address2, debtor?.ZipCode, debtor?.City, debtor?.Country ?? CountryCode.Unknown);
                editrow.FromHome = editrow.FromWork = false;
                editrow.FromCompanyAddress = null;
            }
        }

        private void leFromCompanyAddress_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                var compAddr = companyAddressCache.Get(e.NewValue.ToString()) as CompanyAddressClient;
                SetFromAddress(string.Empty, compAddr?.Address1, compAddr?.Address2, compAddr?.ZipCode, compAddr?.City, compAddr?.Country ?? CountryCode.Unknown);
                editrow.FromHome = editrow.FromWork = false;
                editrow.FromAccount = null;
            }
        }

        private void SetFromAddress(string name, string addr1, string addr2, string zipCode, string city, CountryCode country)
        {
            editrow.FromName = name;
            editrow.FromAddress1 = addr1;
            editrow.FromAddress2 = addr2;
            editrow.FromZipCode = zipCode;
            editrow.FromCity = city;
            editrow.FromCountry = country;
        }

        private void SetToAddress(string name, string addr1, string addr2, string zipCode, string city, CountryCode country)
        {
            editrow.ToName = name;
            editrow.ToAddress1 = addr1;
            editrow.ToAddress2 = addr2;
            editrow.ToZipCode = zipCode;
            editrow.ToCity = city;
            editrow.ToCountry = country;
        }
    }
}