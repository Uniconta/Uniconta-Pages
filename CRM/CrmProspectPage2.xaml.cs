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

    public partial class CrmProspectPage2 : FormBasePage
    {
        CrmProspectClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            object[] argsArray = new object[4];
            argsArray[0] = RefreshParams[0];
            argsArray[1] = RefreshParams[1];
            ((CrmProspectClient)argsArray[1]).NotifyPropertyChanged("UserField");
            argsArray[2] = this.backTab;
            argsArray[3] = editrow;
            globalEvents.OnRefresh(NameOfControl, argsArray);
        }
        public override string NameOfControl { get { return TabControls.CrmProspectPage2.ToString(); } }

        public override Type TableType { get { return typeof(CrmProspectClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CrmProspectClient)value; } }
        bool isCopiedRow = false;
        public CrmProspectPage2(UnicontaBaseEntity sourcedata, bool IsEdit)
            : base(sourcedata, IsEdit)
        {
            InitializeComponent();
            isCopiedRow = !IsEdit;
            InitPage(api);
        }

        public CrmProspectPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
            FocusManager.SetFocusedElement(txtName, txtName);
        }

        void InitPage(CrudAPI crudapi)
        {
            ribbonControl = frmRibbon;
            layoutControl = layoutItems;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            DebGroupLookUpEditor.api = ItemNameGrouplookupeditior.api = PriceListlookupeditior.api = Employeelookupeditor.api = dim1lookupeditior.api = dim2lookupeditior.api =
            dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api = grouplookupeditor.api = LayoutGrouplookupeditior.api = leIndustryCode.api = crudapi;

            var Comp = crudapi.CompanyEntity;

            if (!Comp.InvPrice)
                PriceListlookupeditior.Visibility = Visibility.Collapsed;
            if (!Comp.InvPrice)
                PriceListlookupeditior.Visibility = Visibility.Collapsed;
            if (!Comp.InvClientName)
                ItemNameGrouplookupeditior.Visibility = Visibility.Collapsed;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, usedim);
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                if (!isCopiedRow)
                {
                    editrow = CreateNew() as CrmProspectClient;
                    editrow.SetMaster(Comp);
                    editrow.Country = Comp._CountryId;
                }
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            GetInterestAndProduct();
            editrow.PropertyChanged += Editrow_PropertyChanged;
            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
        }

        async void GetInterestAndProduct()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var InterestCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmInterest), api);
            cmbInterests.ItemsSource = InterestCache.GetKeyList();

            var ProductCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmProduct), api);
            cmbProducts.ItemsSource = ProductCache.GetKeyList();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save" && !VaidateEAN(editrow.EAN))
                return;
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

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZipCode")
            {
                var city = await UtilDisplay.GetCityAndAddress(editrow.ZipCode, editrow.Country);
                if (city != null)
                {
                    editrow.City = city[0];
                    var add1 = city[1];
                    if (!string.IsNullOrEmpty(add1))
                        editrow.Address1 = add1;
                    var zip = city[2];
                    if (!string.IsNullOrEmpty(zip))
                        editrow.ZipCode = zip;
                }
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
                    ci = await CVR.CheckCountry(cvr, editrow._Country);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                    return;
                }
                if (!onlyRunOnce)
                {
                    if (ci == null)
                        return;

                    if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                    {
                        editrow.IndustryCode = ci.industrycode?.code;
                        var address = ci.address;
                        if (address != null)
                        {
                            var streetAddress = address.CompleteStreet;
                            if (string.Compare(ci.life.name, editrow._Name, StringComparison.CurrentCultureIgnoreCase) == 0 && streetAddress == editrow._Address1 &&
                                address.zipcode == editrow._ZipCode)
                                return; // we wil not override since address has not changed

                            onlyRunOnce = true;
                            if (editrow._Address1 != null)
                            {
                                var result = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateAddress"), Uniconta.ClientTools.Localization.lookup("Information"), UnicontaMessageBox.YesNo);
                                if (result != UnicontaMessageBox.Yes)
                                    return;
                            }
                            editrow.Name = ci.life.name;
                            editrow.Address1 = streetAddress;
                            editrow.Address2 = address.street2;
                            editrow.ZipCode = address.zipcode;
                            editrow.City = address.cityname;
                            editrow.Country = address.Country;
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
                        var state = ci.companystatus;
                        if (state != null)
                        {
                            editrow._StateOfCompany = state.StatusCode();
                            editrow.NotifyPropertyChanged("CompanyState");
                        }
                    }
                }
                else
                    onlyRunOnce = false;
            }
        }

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
            Utility.OpenWebSite(editrow._Www);
        }
        private void liCompanyRegNo_ButtonClicked(object sender)
        {
            Utility.OpenCVR(editrow._Country, editrow._LegalIdent);
        }
    }
}
