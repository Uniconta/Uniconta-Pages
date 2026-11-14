using UnicontaClient;
using UnicontaClient.Models;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.API.Service;
using DevExpress.XtraSpreadsheet.Model;
using Uniconta.API.System;
using Uniconta.Common.User;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CompanyPreferencePage : FormBasePage
    {
        CompanyClient editrow;

        public override Type TableType { get { return typeof(CompanyClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyClient)value; } }
        protected override bool IsLayoutSaveRequired() { return false; }

        public override void OnClosePage(object[] refreshParams) { }
        public CompanyPreferencePage(BaseAPI API)
             : this(API.CompanyEntity)
        {

        }
        public CompanyPreferencePage(UnicontaBaseEntity sourceData) : base(sourceData, true)
        {
            InitializeComponent();
            cmbOrdLineStorageReg.ItemsSource = AppEnums.StorageRegister.Values;
            cmbPurLineStorageReg.ItemsSource = AppEnums.StorageRegisterPurchage.Values;
            cmbOIOUBLIncludes.ItemsSource = AppEnums.OIOUBLIncludeOptions.Values;
            cmbAccOnInvTrans.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("DeliveryAccount"), Uniconta.ClientTools.Localization.lookup("InvoiceAccount") };
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            cmbAccOnInvTrans.SelectedIndex = (editrow.InvoiceAccountOnInvTrans) ? 1 : 0;
            if (!api.CompanyEntity.Warehouse)
                liChkWarehouse.Visibility = Visibility.Collapsed;

            if (!api.CompanyEntity.CreditorBankApprovement)
                liNemhandelUpdateBankDetails.Visibility = Visibility.Collapsed;
            HideMenuItems();
            InitIcelandEInvoice();
            Loaded += async (s, e) => await InitAsync();
            
        }
        private void HideMenuItems()
        {
            var rb = (Uniconta.ClientTools.Controls.RibbonBase)frmRibbon.DataContext;
            if (rb != null)
                UtilDisplay.RemoveMenuCommand(rb, new[] { "ExpandAll", "CollapseAll" });
        }
        private async Task InitAsync()
        {
            if (api.CompanyEntity.DocumentScanner == 0)
            {
                var hasAutomationPackage = await HasAutomationPackage(api);
                if (!hasAutomationPackage)
                    clgDocumentScanner.Visibility = Visibility.Collapsed;
            }
        }

        #region Iceland e-invoice registration (api.uniconta.is)

        void InitIcelandEInvoice()
        {
            var comp = api.CompanyEntity;
            if (comp._EInvoiceProvider != Uniconta.DataModel.EInvoiceProvider.Iceland || comp._CountryId != Uniconta.Common.CountryCode.Iceland)
                return;
            liIcelandEInvoice.Visibility = Visibility.Visible;
            liIcelandEInvoice.Label = Uniconta.ClientTools.Localization.lookup("einvoice");
            btnIcelandEInvoiceRegister.Content = Uniconta.ClientTools.Localization.lookup("Register");
            btnIcelandEInvoiceUnregister.Content = Uniconta.ClientTools.Localization.lookup("Unregister");
            ShowIcelandEInvoiceState();
            if (api.CompanyEntity._IcelandEInvoiceState == Uniconta.DataModel.IcelandEInvoiceState.Pending)
                RunIcelandEInvoice(2, false); // the server also asks every other minute; ask now so the page shows the latest
        }

        void ShowIcelandEInvoiceState()
        {
            var state = editrow._IcelandEInvoiceState;
            switch (state)
            {
                case Uniconta.DataModel.IcelandEInvoiceState.Pending: txtIcelandEInvoiceState.Text = Uniconta.ClientTools.Localization.lookup("ToApprove"); break;
                case Uniconta.DataModel.IcelandEInvoiceState.Active: txtIcelandEInvoiceState.Text = Uniconta.ClientTools.Localization.lookup("Registered"); break;
                case Uniconta.DataModel.IcelandEInvoiceState.Off: txtIcelandEInvoiceState.Text = Uniconta.ClientTools.Localization.lookup("Disabled"); break;
                default: txtIcelandEInvoiceState.Text = Uniconta.ClientTools.Localization.lookup("NotRegistered"); break;
            }
            btnIcelandEInvoiceRegister.IsEnabled = state == Uniconta.DataModel.IcelandEInvoiceState.None || state == Uniconta.DataModel.IcelandEInvoiceState.Off;
            btnIcelandEInvoiceUnregister.IsEnabled = state != Uniconta.DataModel.IcelandEInvoiceState.None;
        }

        void IcelandEInvoiceRegister_Click(object sender, RoutedEventArgs e) { RunIcelandEInvoice(0, true); }
        void IcelandEInvoiceUnregister_Click(object sender, RoutedEventArgs e) { RunIcelandEInvoice(1, true); }

        async void RunIcelandEInvoice(byte function, bool showErrors)
        {
            var eApi = new Uniconta.API.DebtorCreditor.IcelandEInvoiceAPI(api);
            busyIndicator.IsBusy = true;
            Uniconta.API.DebtorCreditor.IcelandEInvoiceRegistrationResult r;
            try
            {
                r = function == 0 ? await eApi.Register() : function == 1 ? await eApi.Unregister() : await eApi.RefreshRegistration();
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
            if (r != null && r.Err == ErrorCodes.Succes)
            {
                editrow._IcelandEInvoiceState = r.State;
                api.CompanyEntity._IcelandEInvoiceState = r.State;
            }
            ShowIcelandEInvoiceState();
            if (!showErrors)
                return;
            var err = r?.Err ?? eApi.LastError;
            if (err == ErrorCodes.Succes)
                return;
            if (r?.Message != null)
                UtilDisplay.ShowErrorCode(err, new[] { new StandardError { error = err, message = r.Message } });
            else
                UtilDisplay.ShowErrorCode(err);
        }

        #endregion

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private void cmbAccOnInvTrans_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            editrow.InvoiceAccountOnInvTrans = (cmbAccOnInvTrans.SelectedIndex != 0);
        }

        private async Task<bool> HasAutomationPackage(CrudAPI api)
        {
            var ownerId = api.CompanyEntity._OwnerUid;
            if (ownerId == 0)
                return false;

            var propvalpair = new PropValuePair[1]
            {
                PropValuePair.GenereteWhereElements("Uid", ownerId, CompareOperator.Equal)
            };

            var owner = (await api.Query<UserClient>(propvalpair))?
                .FirstOrDefault(o => o.Role == UserRoles.Accountant || o.PartnerId != 0);

            if (owner == null)
                return false;

            propvalpair[0] = PropValuePair.GenereteWhereElements("UnivisorOwner", owner.PartnerId, CompareOperator.Equal);
            return (await api.Query<SubscriptionClient>(propvalpair))?.Any(x => x != null && x.AutomationPackage) ?? false;
        }
    }
}
