using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;


using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class PartnerProspectPage2 : FormBasePage
    {
        PartnerProspectClient editrow;
        public override void OnClosePage(object[] refreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, refreshParams);
        }
        public override string NameOfControl { get { return TabControls.PartnerProspectPage2.ToString(); } }

        public override Type TableType { get { return typeof(PartnerProspectClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (PartnerProspectClient)value; } }
        bool isCopiedRow = false;
        public PartnerProspectPage2(UnicontaBaseEntity sourcedata, bool IsEdit)
            : base(sourcedata, IsEdit)
        {
            InitializeComponent();
            isCopiedRow = !IsEdit;
            InitPage(api);
        }

        public PartnerProspectPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtName, txtName);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            ribbonControl = frmRibbon;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            cbCurrentERP.ItemsSource= Enum.GetValues(typeof(Uniconta.Common.ERPSystem));
            cbCompanyType.ItemsSource= Enum.GetValues(typeof(Uniconta.Common.CompanyType));
            cbCommingFrom.ItemsSource= Enum.GetValues(typeof(Uniconta.Common.ContactFrom));
            var Comp = crudapi.CompanyEntity;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                if (!isCopiedRow)
                {
                    editrow = CreateNew() as PartnerProspectClient;
                    editrow.Country = Comp._CountryId;
                }
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            editrow.PropertyChanged += Editrow_PropertyChanged;
            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        bool VaidateEAN(string ean)
        {
            if (Utility.IsValidEAN(ean, api.CompanyEntity))
                return true;
            else
                UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), ean), Uniconta.ClientTools.Localization.lookup("Warning"));
            return false;
        }
        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            parentGroup = lastGroup;
            return true;
        }

        string zip;
        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZipCode")
            {
                if (zip == null)
                {
                    var city = await UtilDisplay.GetCityAndAddress(txtZipCode.Text, editrow.Country);
                    if (city != null)
                    {
                        editrow.City = city[0];
                        var add1 = city[1];
                        if (!string.IsNullOrEmpty(add1))
                            editrow.Address1 = add1;
                        zip = city[2];
                        if (!string.IsNullOrEmpty(zip))
                            editrow.ZipCode = zip;
                    }
                }
                else
                    zip = null;
            }
        }

        private bool onlyRunOnce = false;

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
                            editrow.Www = contact.www;
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

        private void liZipCode_ButtonClicked(object sender)
        {
            var location = editrow.Address1 + "+" + editrow.Address2 + "+" + editrow.Address3 + "+" + editrow.ZipCode + "+" + editrow.City + "+" + editrow.Country;
            Utility.OpenGoogleMap(location);
        }

        private void liWww_ButtonClicked(object sender)
        {
            if (!string.IsNullOrWhiteSpace(editrow.Www))
                Utility.OpenWebSite(editrow.Www);
        }
#endif
    }
}
