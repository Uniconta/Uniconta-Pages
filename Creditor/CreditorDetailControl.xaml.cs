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
using Uniconta.ClientTools.Controls;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorDetailControl : UserControl
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
                var Comp = _api.CompanyEntity;
                if (_api.CompanyEntity.NumberOfDimensions == 0)
                    usedim.Visibility = Visibility.Collapsed;
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
#if !SILVERLIGHT
                if (Comp._CountryId != CountryCode.Estonia)
                    liEEIsNotVatDeclOrg.Visibility = Visibility.Collapsed;
#endif
            }
        }
        public Visibility Visible { get { return this.Visibility; } set { this.Visibility = value; this.layoutItems.Visibility = value; } }

        public CreditorDetailControl()
        {
            InitializeComponent();
            dAddress.Header = Uniconta.ClientTools.Localization.lookup("DeliveryAddr");
            layoutItems.Tag = this;
        }
        public void Refresh(object argument, object dataContext)
        {
            var argumentParams = argument as object[];
            if (argumentParams != null)
            {
                int Operation;
                if (argumentParams[0] != null && int.TryParse(argumentParams[0].ToString(), out Operation) && Operation != 3)
                {
                    layoutItems.DataContext = null;
                    layoutItems.DataContext = dataContext;
                }
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
            var creditor = layoutItems.DataContext as CreditorClient;
            if (creditor != null)
            {
                var location = creditor.Address1 + "+" + creditor.Address2 + "+" + creditor.Address3 + "+" + creditor.ZipCode + "+" + creditor.City + "+" + creditor.Country;
                Utility.OpenGoogleMap(location);
            }
        }

        private void LiDeliveryZipCode_OnButtonClicked(object sender)
        {
            var creditor = layoutItems.DataContext as CreditorClient;
            if (creditor != null)
            {
                var location = creditor.DeliveryAddress1 + "+" + creditor.DeliveryAddress2 + "+" + creditor.DeliveryAddress3 + "+" + creditor.DeliveryZipCode + "+" + creditor.DeliveryCity + "+" + creditor.DeliveryCountry;
                Utility.OpenGoogleMap(location);
            }
        }

        private void liWww_ButtonClicked(object sender)
        {
            var creditor = layoutItems.DataContext as CreditorClient;
            Utility.OpenWebSite(creditor._Www);
        }
#endif
    }
}
