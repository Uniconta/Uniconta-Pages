using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.Editors;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorInvoicePage2 : FormBasePage
    {
        CreditorInvoiceClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.CreditorInvoicePage2; } }

        public override Type TableType { get { return typeof(CreditorInvoiceClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CreditorInvoiceClient)value; } }

        public CreditorInvoicePage2(UnicontaBaseEntity sourcedata, bool isEdit = true)
            : base(sourcedata, isEdit)
        {
            InitPage(sourcedata);
        }

        void InitPage(UnicontaBaseEntity sourcedata)
        {
            InitializeComponent();
            ribbonControl = frmRibbon;
            layoutControl = layoutItems;
            layoutItems.DataContext = editrow;
            cmbDelCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            leDeliveryTerm.api = lePayment.api = leShipment.api = leInstallation.api = leOrderGroup.api = leEmployee.api = cmbDim1.api = cmbDim2.api = leDeliveryAccount.api =
            cmbDim3.api = cmbDim4.api = cmbDim5.api = api;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            if (api.CompanyEntity.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
        bool isDocumentRefLookupSet;
        private void liDocumentRef_LookupButtonClicked(object sender)
        {
            var lookupDocumentRefEditor = sender as LookupEditor;
            if (!isDocumentRefLookupSet)
            {
                lookupDocumentRefEditor.PopupContentTemplate = (Application.Current).Resources["LookUpUrlDocumentClientPopupContent"] as ControlTemplate;
                lookupDocumentRefEditor.ValueMember = "RowId";
                lookupDocumentRefEditor.SelectedIndexChanged += LookupDocumentRefEditor_SelectedIndexChanged;
                isDocumentRefLookupSet = true;
                lookupDocumentRefEditor.ItemsSource = api.Query<VouchersClient>(editrow).GetAwaiter().GetResult();
            }
        }

        private void LookupDocumentRefEditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var lookUpEditor = sender as LookupEditor;
            var voucherClient = lookUpEditor.SelectedItem as VouchersClient;
            editrow.DocumentRef = voucherClient?.RowId ?? 0;
        }

        async private void liDocumentRef_ButtonClicked(object sender)
        {
            if (editrow != null)
            {
                ViewVoucher(TabControls.VouchersPage3, editrow);
            }
        }
    }
}
