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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for ProjectDetailControl.xaml
    /// </summary>
    public partial class ProjectDetailControl : System.Windows.Controls.UserControl
    {
        public ProjectDetailControl()
        {
            InitializeComponent();
            layoutItems.Tag = this;
        }
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
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            }
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
        private void Email_ButtonClicked(object sender)
        {
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }

        private void LiZipCode_OnButtonClicked(object sender)
        {
            var project = layoutItems.DataContext as ProjectClient;
            if (project != null)
            {
                var location = project._WorkAddress1 + "+" + project._WorkAddress2 + "+" + project._WorkAddress3 + "+" + project._ZipCode + "+" + project._City + "+" + project.WorkCountry;
                Utility.OpenGoogleMap(location);
            }
        }
#endif
    }
}
