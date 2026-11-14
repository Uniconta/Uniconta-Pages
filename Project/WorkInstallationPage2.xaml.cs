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
using Uniconta.API.DebtorCreditor;
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
            if (master is CreditorClient)
            {
                liCreditorAccount.Visibility = leCreditorAccount.Visibility = Visibility.Visible;
                liDCAccount.Visibility = leDCAccount.Visibility = Visibility.Collapsed;
                leCreditorAccount.api = crudapi;
            }
            else
            {
                liCreditorAccount.Visibility = leCreditorAccount.Visibility = Visibility.Collapsed;
                liDCAccount.Visibility = leDCAccount.Visibility = Visibility.Visible;
                leDCAccount.api = crudapi;
            }
            leWorkStationGroup.api = leDeliveryTerm.api = crudapi;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            editrow.PropertyChanged += Editrow_PropertyChanged;
            txtCompanyRegNo.EditValueChanged += TxtCVR_EditValueChanged;
            txtEndpointId.LostFocus += TxtEndPointId_LostFocus;
            _lastCheckedEndPointId = editrow.EndpointId;
            txtGLN.LostFocus += TxtGLN_LostFocus;
            _lastCheckedGln = editrow.GLN;
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

        string _lastCheckedGln;
        private async void TxtGLN_LostFocus(object sender, RoutedEventArgs e)
        {
            var gln = txtGLN.Text?.Trim();
            if (string.IsNullOrEmpty(gln) || gln == _lastCheckedGln)
                return;
            _lastCheckedGln = gln;
            if (!EDocumentHelper.IsValidGln(gln))
            {
                SetStatusColor(tbGlnStatus, null);
                return;
            }

            await CheckEndPoint(string.Concat(EDocumentHelper.GlnScheme, ":", gln), Uniconta.ClientTools.Localization.lookup("GLNNumber"), gln, tbGlnStatus);
        }

        class EndPointStatus
        {
            public bool? AbleToReceive;
            public EDocumentNetwork? Network;
            public ErrorCodes Error;
        }

        EDocumentParticipantLookupCache _invoiceLookup, _orderLookup;
        EDocumentParticipantLookupCache ParticipantLookup
        {
            get
            {
                if (editrow._DCType == 2)
                    return _orderLookup ?? (_orderLookup = new EDocumentParticipantLookupCache(api, EDocumentDocumentType.Order));
                return _invoiceLookup ?? (_invoiceLookup = new EDocumentParticipantLookupCache(api, EDocumentDocumentType.Invoice));
            }
        }

        Uniconta.DataModel.DCAccount MasterAccount => (Uniconta.DataModel.DCAccount)editrow.DebtorRef ?? editrow.CreditorRef;
        EDocumentNetwork? MasterNetwork()
        {
            var master = MasterAccount;
            if (master == null)
                return null;
            if (master._InvoiceInPepPol)
                return EDocumentNetwork.Peppol;
            if (master._InvoiceInXML)
                return EDocumentNetwork.Nemhandel;
            return null;
        }

        private async Task<EndPointStatus> LookupStatus(string endPointId)
        {
            var status = new EndPointStatus();
            var companyCountry = api.CompanyEntity._CountryId;
            if (!EDocumentHelper.IsPeppolSenderCountry(companyCountry))
                return status;

            var network = MasterNetwork();
            if (network != null)
            {
                if (network == EDocumentNetwork.Nemhandel && !EDocumentHelper.IsNemhandelSenderCountry(companyCountry))
                    return status;
                status.Network = network;
                var lookup = await ParticipantLookup.Lookup(network.Value, endPointId);
                if (lookup.Info != null)
                    status.AbleToReceive = lookup.Info.ParticipantAbleToReceive;
                else
                    status.Error = lookup.LastError;
            }
            else
            {
                var country = MasterAccount?._Country ?? editrow._Country;
                var lookup = await ParticipantLookup.LookupAnyNetwork(endPointId, country);
                if (lookup.Info != null)
                {
                    status.Network = lookup.Info.MatchedNetwork;
                    status.AbleToReceive = status.Network != null;
                }
                else
                    status.Error = lookup.LastError;
            }
            return status;
        }

        private async Task CheckEndPoint(string endPointId, string displayFieldName, string displayValue, TextBlock statusIcon)
        {
            var status = await LookupStatus(endPointId);
            SetStatusColor(statusIcon, status.AbleToReceive);
            if (status.Error != ErrorCodes.Succes)
            {
                UtilDisplay.ShowFormattedErrorCode(status.Error);
                return;
            }
            if (status.AbleToReceive != false)
                return;

            var networkText = MasterNetwork()?.ToString() ?? string.Concat(" Nemhandel ", Uniconta.ClientTools.Localization.lookup("OR"), " Peppol");
            UnicontaMessageBox.Show(string.Concat(displayFieldName, ": ", displayValue, " ", string.Format(Uniconta.ClientTools.Localization.lookup("DoesNotExistOnNetworkOBJ").ToLower(), networkText)), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
        }

        private static void SetStatusColor(TextBlock icon, bool? status)
        {
            if (status == true)
                icon.Foreground = System.Windows.Media.Brushes.Green;
            else if (status == false)
                icon.Foreground = System.Windows.Media.Brushes.Orange;
            else
                icon.ClearValue(TextBlock.ForegroundProperty);
        }

        private async void liGLN_ButtonClicked(object sender)
        {
            var gln = txtGLN.Text?.Trim();
            if (!EDocumentHelper.IsValidGln(gln))
                return;
            _lastCheckedGln = gln;
            var status = await LookupStatus(string.Concat(EDocumentHelper.GlnScheme, ":", gln));
            SetStatusColor(tbGlnStatus, status.AbleToReceive);
            if (status.Network == null)
                return;
            var url = status.Network == EDocumentNetwork.Peppol
                ? EDocumentHelper.BuildSmpLookupUrl(EDocumentHelper.GlnScheme, gln)
                : EDocumentHelper.BuildNhrLookupUrl(isGln: true, gln);
            Utility.OpenWebSite(url);
        }

        private async void liEndpointId_ButtonClicked(object sender)
        {
            var endPointId = txtEndpointId.Text?.Trim();
            if (!EDocumentHelper.IsWellFormedEndPointId(endPointId))
                return;
            _lastCheckedEndPointId = endPointId;
            var status = await LookupStatus(endPointId);
            SetStatusColor(tbEndpointIdStatus, status.AbleToReceive);
            if (status.Network == null)
                return;
            var parts = endPointId.Split(new[] { ':' }, 2);
            var url = status.Network == EDocumentNetwork.Peppol
                ? EDocumentHelper.BuildSmpLookupUrl(parts[0], parts[1])
                : EDocumentHelper.BuildNhrLookupUrl(isGln: parts[0] == EDocumentHelper.GlnScheme, parts[1]);
            Utility.OpenWebSite(url);
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
        private async void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DCType" || e.PropertyName == "DCAccount" || e.PropertyName == "CreditorAccount")
            {
                _lastCheckedGln = _lastCheckedEndPointId = null;
                SetStatusColor(tbGlnStatus, null);
                SetStatusColor(tbEndpointIdStatus, null);
            }
            else if (e.PropertyName == "ZipCode")
            {
                var city = await UtilDisplay.GetCityAndAddress(editrow.ZipCode, editrow.Country);
                if (city != null)
                {
                    editrow.City = city[0];
                    var add1 = city[1];
                    if (!string.IsNullOrEmpty(add1) && string.IsNullOrWhiteSpace(editrow.Address1))
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
            if (string.IsNullOrWhiteSpace(txtEmail?.Text))
                return;
            OfficeComponentUtil.SendEmailToOutlook(txtEmail.Text);
        }

        private void LiZipCode_OnButtonClicked(object sender)
        {
            var location = editrow._Address1 + "+" + editrow._Address2 + "+" + editrow._Address3 + "+" + editrow._ZipCode + "+" + editrow._City + "+" + editrow.Country;
            Utility.OpenGoogleMap(location);
        }
    }
}
