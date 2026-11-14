using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.API.DebtorCreditor;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.API.Service;

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
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutControl = layoutItems;
            cbDeliveryCountry.ItemsSource = cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            ItemNameGrouplookupeditior.api =
               Withholdinglookupeditior.api =
               Vatlookupeditior.api = VatOprlookupeditior.api = Employeelookupeditor.api = leInvoiceAccount.api =
               dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api =
               Paymentlookupeditior.api = grouplookupeditor.api = PriceListlookupeditior.api = lePostingAccount.api = lePaymtFormat.api =
               leShipment.api = leDeliveryTerm.api = LayoutGrouplookupeditior.api = prCategoryLookUpeditor.api = leD2CAccount.api = leCrmGroup.api = leTransType.api =
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
                    editrow = CreateNew() as CreditorClient;
                    if (editrow.Country == 0)
                        editrow.Country = crudapi.CompanyEntity._CountryId;
                }
                editrow._D2CAccount = null;
            }

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            if (editrow.RowId != 0)
                leD2CAccount.IsReadOnly = true;
            editrow.PropertyChanged += Editrow_PropertyChanged;
            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
            txtCompanyRegNo.LostFocus += TxtCompanyRegNo_LostFocus;
            txtEndpointId.LostFocus += TxtEndPointId_LostFocus;
            _lastCheckedEndPointId = editrow.EndpointId;
            txtEAN.LostFocus += TxtEAN_LostFocus;
            _lastCheckedEan = editrow._EAN;
            _participantLookup = new EDocumentParticipantLookupCache(crudapi, EDocumentDocumentType.Order);
            _participantLookupInvoice = new EDocumentParticipantLookupCache(crudapi, EDocumentDocumentType.Invoice);
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

            await CheckEndPoint(endPointId, Uniconta.ClientTools.Localization.lookup("EndpointId"), endPointId, tbEndpointIdStatus);
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

        EDocumentParticipantLookupCache _participantLookup, _participantLookupInvoice;

        private async Task<bool> ShowUnableToReceiveOrders(EDocumentNetwork? network, string endPointId, string displayFieldName, string displayValue)
        {
            EDocumentNetwork foundNetwork;
            if (network.HasValue)
            {
                var info = (await _participantLookupInvoice.Lookup(network.Value, endPointId)).Info;
                if (info == null || !info.ParticipantAbleToReceive)
                    return false;
                foundNetwork = network.Value;
            }
            else
            {
                var info = (await _participantLookupInvoice.LookupAnyNetwork(endPointId, editrow._Country)).Info;
                if (info == null || (info.MatchedNetwork != EDocumentNetwork.Peppol && info.MatchedNetwork != EDocumentNetwork.Nemhandel))
                    return false;
                foundNetwork = info.MatchedNetwork.Value;
            }

            UnicontaMessageBox.Show(string.Concat(displayFieldName, ": ", displayValue, " (", foundNetwork, ") - ",
                string.Format(Uniconta.ClientTools.Localization.lookup("UnableToReceiveOBJ"), Uniconta.ClientTools.Localization.lookup("Orders").ToLower())),
                Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
            return true;
        }

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
                if (!result.ParticipantAbleToReceive && !await ShowUnableToReceiveOrders(network, endPointId, displayFieldName, displayValue))
                    UnicontaMessageBox.Show(string.Concat(displayFieldName, ": ", displayValue, " ", string.Format(Uniconta.ClientTools.Localization.lookup("DoesNotExistOnNetworkOBJ").ToLower(), network)), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
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
                    if (!await ShowUnableToReceiveOrders(null, endPointId, displayFieldName, displayValue))
                        UnicontaMessageBox.Show(string.Concat(displayFieldName, ": ", displayValue, " ", string.Format(Uniconta.ClientTools.Localization.lookup("DoesNotExistOnNetworkOBJ").ToLower(), string.Concat(" Nemhandel ", Uniconta.ClientTools.Localization.lookup("OR"), " Peppol"))), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            }
        }
        private void DeliveryContactEmail_ButtonClicked(object sender)
        {
            if (string.IsNullOrWhiteSpace(txtEmail?.Text))
                return;
            OfficeComponentUtil.SendEmailToOutlook(txtEmail.Text);
        }
        private void TxtCompanyRegNo_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!hasValueChanged)
                return;

            var countryCode = DebtorAccountPage2.CheckEuropeanVatInformation(editrow._LegalIdent, editrow._Country, cvrFound);
            if (countryCode != null && editrow._Country != countryCode)
                editrow.Country = countryCode.Value;
        }

        async Task GetInterestAndProduct()
        {
            var cache = api.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmInterest));
            cmbInterests.ItemsSource = cache.GetKeyList();
            cache = api.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProduct));
            cmbProducts.ItemsSource = cache.GetKeyList();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save" && !VaidateEAN(editrow._EAN))
                return;
            frmRibbon_BaseActions(ActionType);
        }

        protected override void OnLayoutCtrlLoaded()
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
            if (!Comp.CRM)
                crmGroup.Visibility = Visibility.Collapsed;
            else
                crmGroup.Visibility = Visibility.Visible;

            if (Comp.CreditorBankApprovement)
            {
                txtSWIFT.IsReadOnly = true;
                txtPaymentId.IsReadOnly = true;
            }
            else
            {
                liCreditorBankApprovement.Visibility = Visibility.Collapsed;
            }
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
                Vatlookupeditior.cacheFilter = new VatCacheFilter(Cache, GLVatSaleBuy.Buy);

            if (Comp._UseVatOperation)
            {
                Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLVatType)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVatType)).ConfigureAwait(false);
                VatOprlookupeditior.cacheFilter = new VatTypeSQLCacheFilter(Cache, GLVatSaleBuy.Buy);
            }

            creditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);

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

        public override List<IAIAssistantCapability> GetAIAssistantCapabilities(bool hasFileAttached)
        {
            var retval = base.GetAIAssistantCapabilities(hasFileAttached);

            retval.Add(new DeterminePublicProfileCapability());

            return retval;
        }

        private void txtAccount_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded && creditorCache != null)
            {
                var creditor = creditorCache.Get(s.Text);
                if (creditor != null && creditor.RowId != editrow.RowId)
                    UnicontaMessageBox.Show(string.Format("{0} {1} ", Uniconta.ClientTools.Localization.lookup("AccountNumber"), string.Format(Uniconta.ClientTools.Localization.lookup("AlreadyExistOBJ"),
                        s.Text)), Uniconta.ClientTools.Localization.lookup("Warning"));
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
                    UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
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
                            if (kt.Length == 10 && creditorCache?.Get(kt) == null)
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
                        }
                        var state = ci.companystatus;
                        if (state != null)
                        {
                            editrow._StateOfCompany = state.StatusCode();
                            editrow.NotifyPropertyChanged("CompanyState");
                        }
                        if (!string.IsNullOrEmpty(ci.vat) && editrow.Country == CountryCode.Denmark)
                            editrow.VatNumber = ci.vat;
                    }
                }
                else
                    onlyRunOnce = false;
            }
        }

        bool VaidateEAN(string ean)
        {
            if (Utility.IsValidEAN(ean, api.CompanyEntity))
                return true;
            UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), ean), Uniconta.ClientTools.Localization.lookup("Warning"));
            return false;
        }

        private void Email_ButtonClicked(object sender)
        {
            var txtEmail = ((CorasauLayoutItem)sender).Content as TextEditor;
            if (string.IsNullOrWhiteSpace(txtEmail?.Text))
                return;
            OfficeComponentUtil.SendEmailToOutlook(txtEmail.Text);
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
            editrow.NotifyPropertyChanged("eDocument");
        }

        private void cmbEDocument_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
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
