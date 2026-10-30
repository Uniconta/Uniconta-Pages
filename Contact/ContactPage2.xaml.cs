using Uniconta.API.DebtorCreditor;
using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ContactPage2 : FormBasePage
    {
        ContactClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            ((ContactClient)RefreshParams[1]).NotifyPropertyChanged("UserField");
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override Type TableType { get { return typeof(ContactClient); } }
        public override string NameOfControl { get { return TabControls.ContactPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ContactClient)value; } }
        public ContactPage2(UnicontaBaseEntity sourcedata, bool isEdit, UnicontaBaseEntity master = null)
            : base(sourcedata, isEdit)
        {
            InitPage(api, master);
        }
        public ContactPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitPage(crudApi, null);
            FocusManager.SetFocusedElement(txtName, txtName);
        }
        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master)
        {
            InitializeComponent();
            StartLoadCache();
            var Comp = api.CompanyEntity;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                if (editrow == null)
                {
                    editrow = CreateNew() as ContactClient;
                    editrow.SetMaster(master != null ? master : api.CompanyEntity);
                }
                else
                {
                    var dct = editrow._DCType;
                    var dca = editrow._DCAccount;
                    SetTemplate();
                    editrow._DCType = dct;
                    editrow._DCAccount = dca;
                }
                frmRibbon.DisableButtons("Delete");
            }
            lookupDCAccount.api = crudapi;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            txtEndpointId.LostFocus += TxtEndPointId_LostFocus;
            _lastCheckedEndPointId = editrow.EndpointId;
            txtEAN.LostFocus += TxtEAN_LostFocus;
            _lastCheckedEan = editrow._EAN;
            editrow.PropertyChanged += Editrow_PropertyChanged;

            if (master == null)
            {
                itmDCType.Visibility = itemDCAccount.Visibility = Visibility.Visible;

            }
            if (Comp.CRM)
            {
                crmGroup.Visibility = Visibility.Visible;
                GetInterestAndProduct();
            }
        }

        public override CorasauRibbonControl RibbonControl => frmRibbon;

        async void GetInterestAndProduct()
        {
            var api = this.api;
            var cache = api.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmInterest));
            cmbInterests.ItemsSource = cache.GetKeyList();
            cache = api.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProduct));
            cmbProducts.ItemsSource = cache.GetKeyList();
        }

        SQLCache DebtorCache, CreditorCache, CrmProspectCache;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            CrmProspectCache = api.GetCache(typeof(Uniconta.DataModel.CrmProspect)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProspect)).ConfigureAwait(false);

            Dispatcher.BeginInvoke(new Action(() => SetAccountSource()));
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save" && !VaidateEAN(editrow._EAN))
                return;
            frmRibbon_BaseActions(ActionType);
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

            await CheckEndPoint(string.Concat(EDocumentHelper.GlnScheme, ":", ean), Uniconta.ClientTools.Localization.lookup("GLNNumber"), ean, tbEanStatus);
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

        DCAccount MasterAccount => (DCAccount)editrow.Debtor ?? editrow.Creditor;

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
                var country = MasterAccount?._Country ?? api.CompanyEntity._CountryId;
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

        private void Editrow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DCType" || e.PropertyName == "DCAccount")
            {
                _lastCheckedEan = _lastCheckedEndPointId = null;
                SetStatusColor(tbEanStatus, null);
                SetStatusColor(tbEndpointIdStatus, null);
            }
        }

        private async void liEAN_ButtonClicked(object sender)
        {
            var ean = txtEAN.Text?.Trim();
            if (!EDocumentHelper.IsValidGln(ean))
                return;
            _lastCheckedEan = ean;
            var status = await LookupStatus(string.Concat(EDocumentHelper.GlnScheme, ":", ean));
            SetStatusColor(tbEanStatus, status.AbleToReceive);
            if (status.Network == null)
                return;
            var url = status.Network == EDocumentNetwork.Peppol
                ? EDocumentHelper.BuildSmpLookupUrl(EDocumentHelper.GlnScheme, ean)
                : EDocumentHelper.BuildNhrLookupUrl(isGln: true, ean);
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

        bool VaidateEAN(string ean)
        {
            if (Utility.IsValidEAN(ean, api.CompanyEntity))
                return true;
            else
                UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), ean), Uniconta.ClientTools.Localization.lookup("Error"));
            return false;
        }

        private void lookupDCAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            SetAccountSource();
        }

        private void SetAccountSource()
        {
            SQLCache cache;
            switch (editrow._DCType)
            {
                case 1: cache = DebtorCache; break;
                case 2: cache = CreditorCache; break;
                case 3: cache = CrmProspectCache; break;
                case 4: cache = null; break;
                default: return;
            }
            editrow.accntSource = cache;
            editrow.NotifyPropertyChanged("AccountSource");
            editrow.NotifyPropertyChanged("DCAccount");
        }

        private void Email_ButtonClicked(object sender)
        {
            if (string.IsNullOrWhiteSpace(txtEmail.Text))
                return;
            OfficeComponentUtil.SendEmailToOutlook(txtEmail.Text);
        }
    }

}
