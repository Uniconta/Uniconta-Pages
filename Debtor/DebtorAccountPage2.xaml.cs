using DevExpress.Mvvm.Native;
using DevExpress.Xpf.Editors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Enums;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

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
        public bool DoNotSave;
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
            FocusManager.SetFocusedElement(txtAccount, txtAccount);
        }
        bool lookupZipCode = true;
        void InitPage(CrudAPI crudapi)
        {
            ribbonControl = frmRibbon;
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            cbDeliveryCountry.ItemsSource = cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            ItemNameGrouplookupeditior.api =
            Vatlookupeditior.api = VatOprlookupeditior.api = PriceListlookupeditior.api = Employeelookupeditor.api = leInvoiceAccount.api = leShipment.api =
            dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api = Paymentlookupeditior.api =
            grouplookupeditor.api = LayoutGrouplookupeditior.api = lePostingAccount.api = leCrmGroup.api = leDeliveryTerm.api = lePaymtFormat.api = leTransType.api =
            leIndustryCode.api = crudapi;

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
                    if (editrow.Country == 0)
                        editrow.Country = crudapi.CompanyEntity._CountryId;
                }
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
            editrow.PropertyChanged += Editrow_PropertyChanged;
            txtCompanyRegNo.LostFocus += TxtCompanyRegNo_LostFocus;
            txtEndpointId.LostFocus += TxtEndPointId_LostFocus;
            _lastCheckedEndPointId = editrow.EndpointId;
            txtEAN.LostFocus += TxtEAN_LostFocus;
            _lastCheckedEan = editrow._EAN;
            _participantLookup = new EDocumentParticipantLookupCache(crudapi, EDocumentDocumentType.Invoice);
        }

        string _lastCheckedEndPointId;
        private async void TxtEndPointId_LostFocus(object sender, RoutedEventArgs e)
        {
            var endPointId = txtEndpointId.Text?.Trim();
            if (string.IsNullOrEmpty(endPointId) || endPointId == _lastCheckedEndPointId)
                return;
            _lastCheckedEndPointId = endPointId;
            if (!EDocumentHelper.IsWellFormedEndPointId(endPointId))
            {
                SetStatusColor(tbEndpointIdStatus, null);
                return;
            }

            await CheckEndPoint(endPointId, Uniconta.ClientTools.Localization.lookup("EndPointId"), endPointId, tbEndpointIdStatus);
        }

        string _lastCheckedEan;
        private async void TxtEAN_LostFocus(object sender, RoutedEventArgs e)
        {
            var ean = txtEAN.Text?.Trim();
            if (string.IsNullOrEmpty(ean) || ean == _lastCheckedEan)
                return;
            _lastCheckedEan = ean;
            if (!EDocumentHelper.IsValidGln(ean))
            {
                SetStatusColor(tbEanStatus, null);
                return;
            }

            await CheckEndPoint(string.Concat("0088:", ean), Uniconta.ClientTools.Localization.lookup("GLNNumber"), ean, tbEanStatus);
        }

        EDocumentParticipantLookupCache _participantLookup;

        private async Task CheckEndPoint(string endPointId, string displayFieldName, string displayValue, TextBlock statusIcon)
        {
            var companyCountry = api.CompanyEntity._CountryId;

            if (!EDocumentHelper.IsPeppolSenderCountry(companyCountry))
            {
                SetStatusColor(statusIcon, null);
                return;
            }

            if (editrow._einvoice)
            {
                if (!editrow._InvoiceInPepPol && !EDocumentHelper.IsNemhandelSenderCountry(companyCountry))
                {
                    SetStatusColor(statusIcon, null);
                    return;
                }

                var network = editrow._InvoiceInPepPol ? EDocumentNetwork.Peppol : EDocumentNetwork.Nemhandel;
                var lookup = await _participantLookup.Lookup(network, endPointId);
                var result = lookup.Info;
                if (result == null)
                {
                    UtilDisplay.ShowFormattedErrorCode(lookup.LastError);
                    return;
                }
                SetStatusColor(statusIcon, result.ParticipantAbleToReceive);
                if (!result.ParticipantAbleToReceive)
                    UnicontaMessageBox.Show(string.Concat(displayFieldName,": ",displayValue, " ", string.Format(Uniconta.ClientTools.Localization.lookup("DoesNotExistOnNetworkOBJ").ToLower(), network)), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
            }
            else
            {
                var lookup = await _participantLookup.LookupAnyNetwork(endPointId, editrow._Country);
                var result = lookup.Info;
                if (result == null)
                {
                    UtilDisplay.ShowFormattedErrorCode(lookup.LastError);
                    return;
                }

                if (result.MatchedNetwork == EDocumentNetwork.Peppol)
                {
                    SetNetwork(EDocumentNetwork.Peppol);
                    SetStatusColor(statusIcon, true);
                }
                else if (result.MatchedNetwork == EDocumentNetwork.Nemhandel)
                {
                    SetNetwork(EDocumentNetwork.Nemhandel);
                    SetStatusColor(statusIcon, true);
                }
                else
                {
                    SetStatusColor(statusIcon, false);
                    UnicontaMessageBox.Show(string.Concat(displayFieldName, ": ", displayValue," ", string.Format(Uniconta.ClientTools.Localization.lookup("DoesNotExistOnNetworkOBJ").ToLower(), string.Concat(" Nemhandel ", Uniconta.ClientTools.Localization.lookup("OR"), " Peppol"))), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            }
        }
        private void DeliveryContactEmail_ButtonClicked(object sender)
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
                return;
            OfficeComponentUtil.SendEmailToOutlook(txtEmail.Text);
        }
        private void TxtCompanyRegNo_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!hasValueChanged)
                return;

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
                if (vatInfo == null)/* error in service */
                    return null;
                else if (string.IsNullOrEmpty(vatInfo.VatNumber))
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NotValidVatNo"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return null;
                }
            }
            return (CountryCode)countryCode;
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
                        if (!string.IsNullOrEmpty(add1) && string.IsNullOrWhiteSpace(editrow.Address1))
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
                        if (!string.IsNullOrEmpty(add1) && string.IsNullOrWhiteSpace(editrow.DeliveryAddress1))
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
            var cache = api.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmInterest));
            cmbInterests.ItemsSource = cache.GetKeyList();
            cache = api.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProduct));
            cmbProducts.ItemsSource = cache.GetKeyList();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    if (!VaidateEAN(editrow._EAN))
                        return;
                    if (DoNotSave)
                    {
                        MoveFocus();
                        close(1);
                        return;
                    }
                    break;
                case "Delete":
                    if (DoNotSave)
                    {
                        close(3);
                        return;
                    }
                    break;
            }
            frmRibbon_BaseActions(ActionType);
        }

        public override List<IAIAssistantCapability> GetAIAssistantCapabilities(bool hasFileAttached)
        {
            var retval = base.GetAIAssistantCapabilities(hasFileAttached);

            retval.Add(new DeterminePublicProfileCapability());

            return retval;
        }

        async void close(int action)
        {
            await ClosePage(action);
            CloseDockItem();
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
            {
                shipmentItem.Visibility = Visibility.Collapsed;
                delivertTremItem.Visibility = Visibility.Collapsed;
            }
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
            if (DoNotSave)
                txtAccount.IsReadOnly = true;
            if (!Comp.InvPackaging)
                EPRGroup.Visibility = Visibility.Collapsed;
        }

        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            parentGroup = lastGroup;
            return true;
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVat)).ConfigureAwait(false);
            if (!Comp._AllowPurchaseVatOnSales)
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

        private bool onlyRunOnce;
        bool cvrFound;
        bool hasValueChanged;
        private async void TxtCVR_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded && s.IsKeyboardFocusWithin)
            {
                hasValueChanged = s.IsLoaded;
                var cvr = s.Text?.Trim();
                if (cvr == null || cvr.Length < 5)
                    return;

                var allIsLetter = cvr?.All(x => char.IsLetter(x));
                if (allIsLetter.HasValue && allIsLetter.Value == true)
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
                    {
                        cvrFound = false;
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                    {
                        cvrFound = true;
                        if (api.CompanyEntity._CountryId == CountryCode.Iceland && string.IsNullOrEmpty(editrow._Account))
                        {
                            var kt = new string(cvr.Where(char.IsDigit).ToArray());
                            if (kt.Length == 10 && debtorCache?.Get(kt) == null)
                                editrow.Account = kt;
                        }
                        editrow.IndustryCode = ci.industrycode?.code;
                        var address = ci.address;
                        if (address != null)
                        {
                            var streetAddress = address.CompleteStreet;
                            if (string.Equals(ci.life.name, editrow._Name, StringComparison.CurrentCultureIgnoreCase) && streetAddress == editrow._Address1 &&
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
                            if (editrow.ZipCode != address.zipcode)
                            {
                                lookupZipCode = false;
                                editrow.ZipCode = address.zipcode;
                            }
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
                        if (!string.IsNullOrEmpty(ci.vat) && editrow.Country == CountryCode.Denmark)
                            editrow.VatNumber = ci.vat;

                        if (api.CompanyEntity._CountryId == CountryCode.Iceland)
                        {
                            editrow.VatNumber = ci.vat;
                            editrow.CompanyState = ci.companystatus.StatusCode().ToString();
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
            var userNote = new UserNotesClient();
            userNote._SendTo = txtEmail.Text;
            OutlookNotes outNotes = new OutlookNotes(api, editrow);
            outNotes.OpenOutLook(userNote, false);
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

        private void liCompanyRegNo_ButtonClicked(object sender)
        {
            Utility.OpenCVR(editrow._Country, editrow._LegalIdent);
        }

        int _eanStatusRequestId;
        int _endpointIdStatusRequestId;

        private static void SetStatusColor(TextBlock icon, bool? status)
        {
            if (status == true)
                icon.Foreground = System.Windows.Media.Brushes.Green;
            else if (status == false)
                icon.Foreground = System.Windows.Media.Brushes.Orange;
            else
                icon.ClearValue(TextBlock.ForegroundProperty);
        }

        private void SetNetwork(EDocumentNetwork network)
        {
            editrow._InvoiceInPepPol = network == EDocumentNetwork.Peppol;
            editrow._InvoiceInXML = network == EDocumentNetwork.Nemhandel;
            editrow.NotifyPropertyChanged("InvoiceInXML");
        }

        private void cmbEinvoice_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.OldValue == e.NewValue)
                return;
            ++_eanStatusRequestId;
            ++_endpointIdStatusRequestId;
            SetStatusColor(tbEanStatus, null);
            SetStatusColor(tbEndpointIdStatus, null);
        }

        private async void CheckEanStatus()
        {
            var requestId = ++_eanStatusRequestId;
            var ean = txtEAN.Text?.Trim();
            var status = EDocumentHelper.IsValidGln(ean) ? await IsParticipantAbleToReceive(string.Concat(EDocumentHelper.GlnScheme, ":", ean)) : null;
            if (requestId == _eanStatusRequestId)
                SetStatusColor(tbEanStatus, status);
        }

        private async void CheckEndpointIdStatus()
        {
            var requestId = ++_endpointIdStatusRequestId;
            var endPointId = txtEndpointId.Text?.Trim();
            var status = EDocumentHelper.IsWellFormedEndPointId(endPointId) ? await IsParticipantAbleToReceive(endPointId) : null;
            if (requestId == _endpointIdStatusRequestId)
                SetStatusColor(tbEndpointIdStatus, status);
        }

        private async Task<bool?> IsParticipantAbleToReceive(string endPointId)
        {
            if (!CanOpenNetworkLookup())
                return null;
            var lookup = await _participantLookup.Lookup(editrow._InvoiceInPepPol ? EDocumentNetwork.Peppol : EDocumentNetwork.Nemhandel, endPointId);
            if (lookup.Info != null)
                return lookup.Info.ParticipantAbleToReceive;
            return lookup.LastError == ErrorCodes.Exception ? (bool?)null : false;
        }

        private bool CanOpenNetworkLookup()
        {
            if (!editrow._InvoiceInXML && !editrow._InvoiceInPepPol)
                return false;
            return editrow._InvoiceInPepPol
                ? EDocumentHelper.IsPeppolSenderCountry(api.CompanyEntity._CountryId)
                : EDocumentHelper.IsNemhandelSenderCountry(api.CompanyEntity._CountryId);
        }

        private void liEAN_ButtonClicked(object sender)
        {
            if (string.IsNullOrEmpty(txtEAN.Text))
                return;
            _lastCheckedEan = txtEAN.Text.Trim();
            CheckEanStatus();
            if (!CanOpenNetworkLookup())
                return;
            var url = editrow._InvoiceInPepPol
                ? EDocumentHelper.BuildSmpLookupUrl(EDocumentHelper.GlnScheme, txtEAN.Text)
                : EDocumentHelper.BuildNhrLookupUrl(isGln: true, txtEAN.Text);
            Utility.OpenWebSite(url);
        }

        private void liEndpointId_ButtonClicked(object sender)
        {
            var parts = txtEndpointId.Text?.Split(new[] { ':' }, 2);
            if (parts == null || parts.Length != 2)
                return;
            _lastCheckedEndPointId = txtEndpointId.Text.Trim();
            CheckEndpointIdStatus();
            if (!CanOpenNetworkLookup())
                return;
            var url = editrow._InvoiceInPepPol
                ? EDocumentHelper.BuildSmpLookupUrl(parts[0], parts[1])
                : EDocumentHelper.BuildNhrLookupUrl(isGln: parts[0] == EDocumentHelper.GlnScheme, parts[1]);
            Utility.OpenWebSite(url);
        }
    }
}