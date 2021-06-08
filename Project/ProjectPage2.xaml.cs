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
            Debtor = master as Uniconta.DataModel.Debtor;
            InitPage(api);
        }

        public ProjectPage2(CrudAPI crudApi, UnicontaBaseEntity master)
         : base(crudApi, "")
        {
            InitializeComponent();
            Debtor = master as Uniconta.DataModel.Debtor;
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtNumber, txtNumber);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;            
			dAddress.Header = Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            lePersonInCharge.api = lePurchaser.api = lePrStandard.api =  leAccount.api = lePayment.api = leVat.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = leGroup.api = leMasterProject.api = lePrType.api= leInstallation.api= crudapi;
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
                    else
                        editrow.SetMaster(crudapi.CompanyEntity);
                }
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            editrow.PropertyChanged += Editrow_PropertyChanged;
            StartLoadCache();
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
                var city = await UtilDisplay.GetCityAndAddress(editrow.ZipCode, editrow._WorkCountry != 0 ? editrow._WorkCountry : api.CompanyEntity._CountryId);
                if (city != null)
                {
                    editrow.City = city[0];
                    var add1 = city[1];
                    if (!string.IsNullOrEmpty(add1))
                        editrow.WorkAddress1 = add1;
                    var zip = city[2];
                    if (!string.IsNullOrEmpty(zip))
                        editrow.ZipCode = zip;
                }
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
            if (debtor != null && debtor._Blocked)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AccountIsBlocked"), Uniconta.ClientTools.Localization.lookup("Information"));
                return;
            }
            if (debtor != null && installationCache != null)
            {
                leInstallation.cacheFilter = new AccountCacheFilter(installationCache, 1, debtor._Account);
                leInstallation.InvalidCache();
            }
        }

        SQLCache installationCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;

            if (api.CompanyEntity.DeliveryAddress)
            {
                installationCache = api.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) ?? await api.LoadCache(typeof(Uniconta.DataModel.WorkInstallation)).ConfigureAwait(false);
                if (editrow._DCAccount != null)
                    leInstallation.cacheFilter = new AccountCacheFilter(installationCache, 1, editrow._DCAccount);
            }
            LoadType(typeof(Uniconta.DataModel.Debtor));
        }
    }
}
