using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
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
using Uniconta.API.System;
using Uniconta.Common;
using System.Windows;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorDetailControl : System.Windows.Controls.UserControl
    {
        CrudAPI _api;
        public CrudAPI api
        {
            get
            {
                return _api;
            }
            set
            {
                _api = value;
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, usedim);
                setUserFields();
            }
        }
        public DebtorDetailControl()
        {
            InitializeComponent();
            layoutItems.Tag = this;
            
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
        }

        async void GetInterestAndProduct()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            var crmInterestCache = Comp.GetCache(typeof(CrmInterest));
            if (crmInterestCache == null)
                crmInterestCache = await Comp.LoadCache(typeof(CrmInterest), api);

            var crmProductCache = Comp.GetCache(typeof(CrmProduct));
            if (crmProductCache == null)
                crmProductCache = await Comp.LoadCache(typeof(CrmProduct), api);

        }

        public Visibility Visible { get { return this.Visibility; } set { this.Visibility = value; this.layoutItems.Visibility = value; } }
        public void Refresh(object argument, object dataContext)
        {
            var argumentParams = argument as object[];
            if (argumentParams != null)
            {
                int operation = -1;
                if (int.TryParse(argumentParams[0] as string, out operation))
                {
                    if (operation != 3)
                        layoutItems.DataContext = dataContext;
                }
            }
        }
        void setUserFields()
        {
           var Comp = api.CompanyEntity;
            if (Comp.CRM)
            {
                crmGroup.Visibility = Visibility.Visible;
                GetInterestAndProduct();
            }
            if (!Comp._UseVatOperation)
                itmVatOpr.Visibility = Visibility.Collapsed;
            if (!Comp.InvPrice)
                priceListLayoutItem.Visibility = Visibility.Collapsed;
            if (!Comp.InvClientName)
                itemNameGrpLayoutItem.Visibility = Visibility.Collapsed;
            if (!Comp.Shipments)
                shipmentItem.Visibility = Visibility.Collapsed;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            if (!Comp.DeliveryAddress)
                dAddress.Visibility = Visibility.Collapsed;
#if !SILVERLIGHT
            if (Comp._CountryId != CountryCode.Estonia)
                liEEIsNotVatDeclOrg.Visibility = Visibility.Collapsed;
#endif
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
            var debtor = layoutItems.DataContext as DebtorClient;
            if (debtor != null)
            {
                var location = debtor.Address1 + "+" + debtor.Address2 + "+" + debtor.Address3 + "+" + debtor.ZipCode + "+" + debtor.City + "+" + debtor.Country;
                Utility.OpenGoogleMap(location);
            }
        }

        private void LiDeliveryZipCode_OnButtonClicked(object sender)
        {
            var debtor = layoutItems.DataContext as DebtorClient;
            if (debtor != null)
            {
                var location = debtor._DeliveryAddress1 + "+" + debtor._DeliveryAddress2 + "+" + debtor._DeliveryAddress3 + "+" + debtor._DeliveryZipCode + "+" + debtor._DeliveryCity + "+" + debtor.DeliveryCountry;
                Utility.OpenGoogleMap(location);
            }
        }

         private void liWww_ButtonClicked(object sender)
        {
            var debtor = layoutItems.DataContext as DebtorClient;
            Utility.OpenWebSite(debtor._Www);
        }
#endif
    }
}
