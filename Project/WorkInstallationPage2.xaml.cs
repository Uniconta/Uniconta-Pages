using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class WorkInstallationPage2 : FormBasePage
    {
        WorkInstallationClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override Type TableType { get { return typeof(WorkInstallationClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (WorkInstallationClient)value; } }

        public WorkInstallationPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api, null);
        }

        public WorkInstallationPage2(UnicontaBaseEntity sourcedata, bool isEdit, UnicontaBaseEntity master = null): base(sourcedata , isEdit)
        {
            InitializeComponent();
            InitPage(api, master);
        }

        public WorkInstallationPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi, null);
        }

        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master)
        {
            ribbonControl = frmRibbon;
            var Comp = api.CompanyEntity;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as WorkInstallationClient;
                editrow.SetMaster(master ?? api.CompanyEntity);
                var dc = master as Uniconta.DataModel.DCAccount;
                if (dc != null)
                    editrow._Country = dc._Country;
            }
            leWorkStationGroup.api = crudapi;
            leDCAccount.api = crudapi;
            cbCountry.ItemsSource= Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            txtZipCode.EditValueChanged += TextEditor_EditValueChanged;
            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "CopyAddress")
            {
                var debtorCache = api.CompanyEntity.GetOrLoadCache(typeof(Uniconta.DataModel.Debtor), api);
                if (debtorCache != null)
                {
                    var debtor = debtorCache.Get(editrow.DCAccount) as Uniconta.DataModel.Debtor;
                    if (debtor != null)
                    {
                        editrow.Address1 = debtor._Address1;
                        editrow.Address2 = debtor._Address2;
                        editrow.Address3 = debtor._Address3;
                        editrow.ZipCode = debtor._ZipCode;
                        editrow.City = debtor._City;
                        editrow.Country = debtor._Country;
                        editrow.Phone = debtor._Phone;
                    }
                }
            }
            else
            frmRibbon_BaseActions(ActionType);
        }

        private async void TextEditor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded)
            {
                var city = await UtilDisplay.GetCityName(s.Text, editrow.Country);
                if (city != null)
                    editrow.City = city;
            }
        }

        private bool onlyRunOnce;

        private async void TxtCVR_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded)
            {
                var cvr = s.Text?.Trim();
                if (cvr == null || cvr.Length < 5)
                    return;

                var allIsLetter = cvr.All(x => char.IsLetter(x));
                if (allIsLetter)
                    return;
                CompanyInfo ci = null;
                try
                {
#if !SILVERLIGHT
                    ci = await CVR.CheckCountry(cvr, editrow._Country);
#else
                    var lookupApi = new Uniconta.API.System.UtilityAPI(api);
                    ci = await lookupApi.LookupCVR(cvr, editrow._Country);
#endif
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
                    return;
                }
                if (!onlyRunOnce)
                {
                    if (ci == null)
                        return;

                    if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                    {
                        var address = ci.address;
                        if (address != null)
                        {
                            var streetAddress = address.CompleteStreet;
                            if (ci.life.name == editrow._Name && streetAddress == editrow._Address1 &&
                                address.zipcode == editrow._ZipCode)
                                return; // we wil not override since address has not changed

                            onlyRunOnce = true;
                            if (editrow._Address1 == null)
                            {
                                editrow.Address1 = address.CompleteStreet;
                                editrow.ZipCode = address.zipcode;
                                editrow.City = address.cityname;
                                editrow.Country = address.Country;
                            }
                            else
                            {
                                var result = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateAddress"), Uniconta.ClientTools.Localization.lookup("Information"), UnicontaMessageBox.YesNo);
                                if (result != UnicontaMessageBox.Yes)
                                    return;
                                {
                                    editrow.Address1 = address.CompleteStreet;
                                    editrow.ZipCode = address.zipcode;
                                    editrow.City = address.cityname;
                                    editrow.Country = address.Country;
                                }
                            }
                        }

                        if (string.IsNullOrWhiteSpace(editrow._Name))
                            editrow.Name = ci.life.name;

                        var contact = ci.contact;
                        if (contact != null)
                        {
                            editrow.Phone = contact.phone;
                            editrow.ContactEmail = contact.email;
                        }
                    }
                }
                else
                    onlyRunOnce = false;
            }
        }
#if !SILVERLIGHT
        private void Email_ButtonClicked(object sender)
        {
            var txtEmail = ((CorasauLayoutItem)sender).Content as TextEditor;
            if (txtEmail == null)
                return;
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }

        private void LiZipCode_OnButtonClicked(object sender)
        {
            var location = string.Format("{0}+{1}+{2}+{3}+{4}+{5}", editrow._Address1, editrow._Address2, editrow._Address3, editrow._ZipCode, editrow._City, editrow.Country);
            Utility.OpenGoogleMap(location);
        }
#endif
    }
}
