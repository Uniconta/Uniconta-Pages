using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Threading.Tasks;
using Uniconta.Common.Utility;
using System.Diagnostics;
using System.Text;
using UnicontaClient.Controls.Dialogs;
using Uniconta.Common.Enums;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorAccountPage2 : FormBasePage
    {
        DebtorClient editrow;
        SQLCache debtorCache;
        public override void OnClosePage(object[] RefreshParams)
        {
            object[] argsArray = new object[4];
            argsArray[0] = RefreshParams[0];
            argsArray[1] = RefreshParams[1];
            ((DebtorClient)argsArray[1]).NotifyPropertyChanged("UserField");
            argsArray[2] = this.backTab;
            argsArray[3] = editrow;
            globalEvents.OnRefresh(NameOfControl, argsArray);
        }
        public override string NameOfControl { get { return TabControls.DebtorAccountPage2.ToString(); } }
        public override Type TableType { get { return typeof(DebtorClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorClient)value; } }
        bool isCopiedRow;

        public DebtorAccountPage2(UnicontaBaseEntity sourcedata, bool IsEdit)
            : base(sourcedata, IsEdit)
        {
            InitializeComponent();
            isCopiedRow = !IsEdit;
            InitPage(api);
        }
        public DebtorAccountPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtAccount, txtAccount);
#endif
        }
        bool lookupZipCode = true;
        void InitPage(CrudAPI crudapi)
        {
            ribbonControl = frmRibbon;
            BusyIndicator = busyIndicator;
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            cbDeliveryCountry.ItemsSource = cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            ItemNameGrouplookupeditior.api =
            Vatlookupeditior.api = VatOprlookupeditior.api = PriceListlookupeditior.api = Employeelookupeditor.api = leInvoiceAccount.api = leShipment.api =
            dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api = Paymentlookupeditior.api = grouplookupeditor.api = LayoutGrouplookupeditior.api = lePostingAccount.api = leCrmGroup.api = leDeliveryTerm.api = lePaymtFormat.api = crudapi;

            AdjustLayout();
            Task t;
            if (crudapi.CompanyEntity.CRM)
                t = GetInterestAndProduct();
            else
                t = null;
            StartLoadCache(t);

            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                if (!isCopiedRow)
                {
                    editrow = CreateNew() as DebtorClient;
                    editrow.Country = crudapi.CompanyEntity._CountryId;
                }
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
            editrow.PropertyChanged += Editrow_PropertyChanged;
#if !SILVERLIGHT
            txtCompanyRegNo.LostFocus += TxtCompanyRegNo_LostFocus;
        }

        private async void TxtCompanyRegNo_LostFocus(object sender, RoutedEventArgs e)
        {
            var countryCode = CheckEuropeanVatInformation(editrow._LegalIdent, editrow._Country, cvrFound);
            if (countryCode != null && editrow._Country != countryCode)
                editrow.Country = countryCode.Value;
        }

        public static CountryCode? CheckEuropeanVatInformation(string cvr, CountryCode country, bool cvrFound)
        {
            if (string.IsNullOrEmpty(cvr))
                return null;
            int countryCode = (int)country;
            string twolettercode = null;
            if (Char.IsLetter(cvr.FirstOrDefault()) && cvr.Length > 3)
            {
                var cCode = cvr.Substring(0, 2);
                cvr = cvr.Substring(2, cvr.Length - 2);
                CountryISOCode isoCode;
                if (Enum.TryParse(cCode, out isoCode))
                {
                    twolettercode = cCode;
                    countryCode = (int)isoCode;
                }
            }
            else
                twolettercode = Enum.GetName(typeof(CountryISOCode), countryCode);
            if (twolettercode != null && !cvrFound && Country2Language.IsEU(country))
            {
                var vatInfo = EuropeanVatInformation.Get(twolettercode, cvr);
                if (vatInfo == null)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NotValidVatNo"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return null;
                }
            }
            return (CountryCode)countryCode;
        }
#else
        }
#endif

        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ZipCode")
            {
                if (lookupZipCode)
                {
                    var city = await UtilDisplay.GetCityAndAddress(editrow.ZipCode, editrow.Country);
                    if (city != null)
                    {
                        editrow.City = city[0];
                        var add1 = city[1];
                        if (!string.IsNullOrEmpty(add1))
                            editrow.Address1 = add1;
                        var zip = city[2];
                        if (!string.IsNullOrEmpty(zip) && editrow.ZipCode != zip)
                        {
                            lookupZipCode = false;
                            editrow.ZipCode = zip;
                        }
                    }
                }
                else
                    lookupZipCode = true;
            }
            else if (e.PropertyName == "DeliveryZipCode")
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

        async Task GetInterestAndProduct()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            var crmInterestCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmInterest), api);
            var crmProductCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmProduct), api);
            cmbInterests.ItemsSource = crmInterestCache.GetKeyList();
            cmbProducts.ItemsSource = crmProductCache.GetKeyList();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save" && !VaidateEAN(editrow._EAN))
                return;
            frmRibbon_BaseActions(ActionType);
        }

        bool VaidateEAN(string ean)
        {
            if (Utility.IsValidEAN(ean, api.CompanyEntity))
                return true;
            UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), ean), Uniconta.ClientTools.Localization.lookup("Warning"));
            return false;
        }
        protected override void OnLayoutCtrlLoaded()
        {
            AdjustLayout();
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (!Comp._UseVatOperation)
                itmVatOpr.Visibility = Visibility.Collapsed;
            if (!Comp.InvPrice)
                priceListLayoutItem.Visibility = Visibility.Collapsed;
            if (!Comp.InvClientName)
                itemNameGrpLayoutItem.Visibility = Visibility.Collapsed;
            if (!Comp.DeliveryAddress)
                dAddress.Visibility = Visibility.Collapsed;
            if (!Comp.Shipments)
                shipmentItem.Visibility = Visibility.Collapsed;
            if (!Comp._DirectDebit)
                liPaymentFormat.Visibility = Visibility.Collapsed;
            if (!Comp.CRM)
                crmGroup.Visibility = Visibility.Collapsed;
            else
                crmGroup.Visibility = Visibility.Visible;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, usedim);

            if (Comp._CountryId != CountryCode.Estonia)
                liEEIsNotVatDeclOrg.Visibility = Visibility.Collapsed;
            if (Comp._CountryId != CountryCode.Iceland)
                liPaymentFee.Visibility = Visibility.Collapsed;
        }

        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            parentGroup = lastGroup;
            return true;
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLVat), api).ConfigureAwait(false);
            Vatlookupeditior.cacheFilter = new VatCacheFilter(Cache, GLVatSaleBuy.Sales);

            if (Comp._UseVatOperation)
            {
                Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLVatType)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLVatType), api).ConfigureAwait(false);
                VatOprlookupeditior.cacheFilter = new VatTypeSQLCacheFilter(Cache, GLVatSaleBuy.Sales);
            }

            debtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);

            LoadType(new Type[] { typeof(Uniconta.DataModel.DebtorGroup), typeof(Uniconta.DataModel.PaymentTerm) });
        }

        private void PriceListlookupeditior_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var selectedItem = PriceListlookupeditior.SelectedItem as Uniconta.DataModel.DebtorPriceList;
            if (selectedItem != null)
                editrow.PricesInclVat = selectedItem._InclVat;
        }

        private void txtAccount_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded && debtorCache != null)
            {
                var debtor = debtorCache.Get(s.Text);
                if (debtor != null && debtor?.RowId != editrow.RowId)
                    UnicontaMessageBox.Show(string.Format("{0} {1} ", Uniconta.ClientTools.Localization.lookup("AccountNumber"), string.Format(Uniconta.ClientTools.Localization.lookup("AlreadyExistOBJ"), s.Text))
                        , Uniconta.ClientTools.Localization.lookup("Information"));
            }
        }

        private bool onlyRunOnce = false;
        bool cvrFound = false;
        private async void TxtCVR_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded)
            {
                var cvr = s.Text?.Trim();
                if (cvr == null || cvr.Length < 5)
                    return;

                var allIsLetter = cvr?.All(x => char.IsLetter(x));
                if (allIsLetter.HasValue && allIsLetter.Value == true)
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
                    UnicontaMessageBox.Show(ex);
                    return;
                }
                if (!onlyRunOnce)
                {
                    if (ci == null)
                    {
                        cvrFound = false;
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                    {
                        cvrFound = true;
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
                                if (editrow.ZipCode != address.zipcode)
                                {
                                    lookupZipCode = false;
                                    editrow.ZipCode = address.zipcode;
                                }
                                editrow.City = address.cityname;
                                editrow.Country = address.Country;
                            }
                            else
                            {
                                var result = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateAddress"), Uniconta.ClientTools.Localization.lookup("Information"), UnicontaMessageBox.YesNo);
                                if (result != UnicontaMessageBox.Yes)
                                    return;
                                editrow.Address1 = address.CompleteStreet;
                                if (editrow.ZipCode != address.zipcode)
                                {
                                    lookupZipCode = false;
                                    editrow.ZipCode = address.zipcode;
                                }
                                editrow.City = address.cityname;
                                editrow.Country = address.Country;
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

        private void LiDeliveryZipCode_OnButtonClicked(object sender)
        {
            var location = editrow.DeliveryAddress1 + "+" + editrow.DeliveryAddress2 + "+" + editrow.DeliveryAddress3 + "+" + editrow.DeliveryZipCode + "+" + editrow.DeliveryCity + "+" + editrow.DeliveryCountry;
            Utility.OpenGoogleMap(location);
        }

        private void liWww_ButtonClicked(object sender)
        {
            Utility.OpenWebSite(editrow._Www);
        }
#endif
    }
}