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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwAddFollowUp : ChildWindow
    {
        CrudAPI api;
        public CrmFollowUpClient NewFollowUp { get;set;}
        CrmFollowUpClient followUp;
        public CwAddFollowUp(CrudAPI _api)
        {
            var newItem = Activator.CreateInstance(typeof(CrmFollowUpClient)) as UnicontaBaseEntity;
            api = _api;
            followUp = newItem as CrmFollowUpClient;
            followUp.SetMaster(api.CompanyEntity);
            this.DataContext = followUp;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("FollowUp"));
            leGroup.api = leEmployee.api =  api;
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtText, txtText);
#endif
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            NewFollowUp = followUp;
            this.DialogResult = true;
        }
    }
}
