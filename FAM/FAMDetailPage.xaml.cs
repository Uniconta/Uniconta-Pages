using UnicontaClient.Utilities;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class FAMDetailPage : System.Windows.Controls.UserControl
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
                if (Comp.NumberOfDimensions == 0)
                    usedim.Visibility = Visibility.Collapsed;
                else
                    Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, usedim);
            }
        }

        public Visibility Visible { get { return this.Visibility; } set { this.Visibility = value; this.layoutItems.Visibility = value; } }

        public FAMDetailPage()
        {
            InitializeComponent();
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
    }
}
