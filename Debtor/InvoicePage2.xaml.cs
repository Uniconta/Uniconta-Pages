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
using DevExpress.CodeParser;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InvoicePage2 : FormBasePage
    {
        DebtorInvoiceClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.InvoicePage2; } }

        public override Type TableType { get { return typeof(DebtorInvoiceClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorInvoiceClient)value; } }

        public InvoicePage2(UnicontaBaseEntity sourcedata, bool isEdit = true)
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
            leCompanyAddress.api = leDeliveryTerm.api = lePayment.api = leShipment.api = leInstallation.api = leOrderGroup.api = leLayoutGroup.api = leEmployee.api = cmbDim1.api = cmbDim2.api = leDeliveryAccount.api =
            cmbDim3.api = cmbDim4.api = cmbDim5.api = api;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            if (api.CompanyEntity.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            BindContact(editrow.Debtor);
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private void cmbContactName_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var selectedItem = cmbContactName.SelectedItem as Contact;
            if (selectedItem != null)
            {
                editrow._ContactRef = selectedItem.RowId;
                editrow.ContactName = selectedItem._Name;
            }
            else
            {
                editrow._ContactRef = 0;
                editrow.ContactName = null;
            }
        }

        async void BindContact(Debtor debtor)
        {
            if (debtor == null) return;

            var cache = api.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact));
            SetContactSource(cache, debtor);
            cmbContactName.DisplayMember = "KeyName";

            if (editrow == null) return;

            var contactRefId = editrow._ContactRef;
            if (contactRefId != 0)
            {
                var contact = cache.Get(contactRefId);
                if (contact == null)
                {
                    cache = api.LoadCache(typeof(Uniconta.DataModel.Contact), true).GetAwaiter().GetResult();
                    contact = cache.Get(contactRefId);
                    SetContactSource(cache, debtor);
                }
                cmbContactName.SelectedItem = contact;
                if (contact == null)
                {
                    editrow._ContactRef = 0;
                    editrow.ContactName = null;
                }
            }
        }

        void SetContactSource(SQLCache cache, Debtor debtor)
        {
            cmbContactName.ItemsSource = cache != null ? new ContactCacheFilter(cache, 1, debtor._Account) : null;
        }

        private void cmbContactName_KeyDown(object sender, KeyEventArgs e)
        {
            var selectedItem = cmbContactName.SelectedItem as Contact;
            GoToContact(selectedItem, e.Key);
        }
    }
}
