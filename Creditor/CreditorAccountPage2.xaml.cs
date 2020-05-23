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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorAccountPage2 : FormBasePage
    {
        CreditorClient editrow;
        SQLCache creditorCache;
        public override void OnClosePage(object[] RefreshParams)
        {
            object[] argsArray = new object[4];
            argsArray[0] = RefreshParams[0];
            argsArray[1] = RefreshParams[1];
            ((CreditorClient)argsArray[1]).NotifyPropertyChanged("UserField");

            argsArray[2] = this.backTab;
            argsArray[3] = editrow;
            globalEvents.OnRefresh(NameOfControl, argsArray);
        }

        public override Type TableType { get { return typeof(CreditorClient); } }
        public override string NameOfControl { get { return TabControls.CreditorAccountPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CreditorClient)value; } }
        bool isCopiedRow = false;
        bool lookupZipCode = true;
        public CreditorAccountPage2(UnicontaBaseEntity sourcedata, bool IsEdit) : base(sourcedata, IsEdit)
        {
            InitializeComponent();
            isCopiedRow = !IsEdit;
            InitPage(api);
        }
        public CreditorAccountPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtAccount, txtAccount);
#endif
        }

        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            cbDeliveryCountry.ItemsSource = cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            ItemNameGrouplookupeditior.api =
           Withholdinglookupeditior.api =
           Vatlookupeditior.api = VatOprlookupeditior.api = Employeelookupeditor.api = leInvoiceAccount.api =
           dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api = Paymentlookupeditior.api = grouplookupeditor.api = PriceListlookupeditior.api = lePostingAccount.api = lePaymtFormat.api = leShipment.api = leDeliveryTerm.api = LayoutGrouplookupeditior.api = prCategoryLookUpeditor.api = crudapi;

            AdjustLayout();

            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                if (!isCopiedRow)
                    editrow = CreateNew() as CreditorClient;
                editrow.Country = crudapi.CompanyEntity._CountryId;
            }

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_BaseActions;

            StartLoadCache();

            editrow.PropertyChanged += Editrow_PropertyChanged;
            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
        }

        protected override void OnLayoutCtrlLoaded()
        {
            AdjustLayout();
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (!Comp._UseVatOperation)
                ItemVatOprlookupeditior.Visibility = Visibility.Collapsed;
            if (!Comp._HasWithholding)
                ItemWithholdinglookupeditior.Visibility = Visibility.Collapsed;
            if (!Comp.InvClientName)
                itemNameGrpLayoutItem.Visibility = Visibility.Collapsed;
            if (!Comp.DeliveryAddress)
                dAddress.Visibility = Visibility.Collapsed;
            if (!Comp.Shipments)
                shipmentItem.Visibility = Visibility.Collapsed;
            if (!Comp.Project)
                liPrCategory.Visibility = Visibility.Collapsed;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, usedim);

            if (Comp._CountryId != CountryCode.Estonia)
                liEEIsNotVatDeclOrg.Visibility = Visibility.Collapsed;
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
            Vatlookupeditior.cacheFilter = new VatCacheFilter(Cache, GLVatSaleBuy.Buy);

            if (api.CompanyEntity._UseVatOperation)
            {
                Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLVatType)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLVatType), api).ConfigureAwait(false);
                VatOprlookupeditior.cacheFilter = new VatTypeSQLCacheFilter(Cache, GLVatSaleBuy.Buy);
            }

            creditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api).ConfigureAwait(false);

            LoadType(new Type[] { typeof(Uniconta.DataModel.CreditorGroup), typeof(Uniconta.DataModel.PaymentTerm) });
        }


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

        private void txtAccount_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded && creditorCache != null)
            {
                var creditor = creditorCache.Get(s.Text);
                if (creditor != null && creditor.RowId != editrow.RowId)
                    UnicontaMessageBox.Show(string.Format("{0} {1} ", Uniconta.ClientTools.Localization.lookup("AccountNumber"), string.Format(Uniconta.ClientTools.Localization.lookup("AlreadyExistOBJ"), 
                        s.Text)),Uniconta.ClientTools.Localization.lookup("Warning"));
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
