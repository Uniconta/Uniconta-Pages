using UnicontaClient.Utilities;
using System.Windows;
using System.Windows.Controls;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class CrmProspectDetailControl : System.Windows.Controls.UserControl
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
                var Comp = _api.CompanyEntity;

                if (!Comp.InvPrice)
                    PriceListlookupeditior.Visibility = Visibility.Collapsed;
                if (!Comp.InvPrice)
                    PriceListlookupeditior.Visibility = Visibility.Collapsed;
                if (!Comp.InvClientName)
                    ItemNameGrouplookupeditior.Visibility = Visibility.Collapsed;
                if (Comp.NumberOfDimensions == 0)
                    usedim.Visibility = Visibility.Collapsed;
                else
                    Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, usedim);
            }
        }

        public CrmProspectDetailControl()
        {
            InitializeComponent();
            layoutItems.Tag = this;
        }

        public Visibility Visible { get { return this.Visibility; } set { this.Visibility = value; this.layoutItems.Visibility = value; } }
        public void Refresh(object argument, object dataContext)
        {
            var argumentParams = argument as object[];
            if (argumentParams != null)
            {
                var Operation = Convert.ToInt32(argumentParams[0]);
                if (Operation != 3)
                    layoutItems.DataContext = dataContext;
            }
        }

#if !SILVERLIGHT
        private void liZipCode_ButtonClicked(object sender)
        {
            var prospect = layoutItems.DataContext as CrmProspectClient;
            if (prospect != null)
            {
                var location = prospect.Address1 + "+" + prospect.Address2 + "+" + prospect.Address3 + "+" + prospect.ZipCode + "+" + prospect.City + "+" + prospect.Country;
                Utility.OpenGoogleMap(location);
            }
        }

        private void liWww_ButtonClicked(object sender)
        {
            var prospect = layoutItems.DataContext as CrmProspectClient;
            Utility.OpenWebSite(prospect._Www);
        }
#endif
    }
}
