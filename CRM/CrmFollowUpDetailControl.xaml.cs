using UnicontaClient.Utilities;
using System.Windows;
using System.Windows.Controls;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using System;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CrmFollowUpDetailControl : System.Windows.Controls.UserControl
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
            }
        }

        public CrmFollowUpDetailControl()
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
            var followUp = layoutItems.DataContext as CrmFollowUpClient;
            if (followUp != null)
            {
                var location = followUp.Address1 + "+" + followUp.Address2 + "+" + followUp.Address3 + "+" + followUp.ZipCode + "+" + followUp.City + "+" + followUp.Country;
                Utility.OpenGoogleMap(location);
            }
        }

        private void liWww_ButtonClicked(object sender)
        {
            var followUp = layoutItems.DataContext as CrmFollowUpClient;
            Utility.OpenWebSite(followUp.Www);
        }
#endif
    }
}
