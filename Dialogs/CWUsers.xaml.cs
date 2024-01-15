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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWUsers : ChildWindow
    {
        public UserClient selectedUser;
        public CWUsers(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            dgUsers.api = api;
            dgUsers.View.ShowSearchPanel(false);
            this.Title = Uniconta.ClientTools.Localization.lookup("Users");
            dgUsers.Filter(null);
            dgUsers.View.Loaded += View_Loaded;
            dgUsers.View.ShowSearchPanelMode = DevExpress.Xpf.Grid.ShowSearchPanelMode.Always;
        }

        private void View_Loaded(object sender, RoutedEventArgs e)
        {
            if (dgUsers.View.SearchControl != null)
                dgUsers.View.SearchControl.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var user = dgUsers.SelectedItem as UserClient;
            if (user != null)
            {
                selectedUser = user;
            }
            SetDialogResult(true);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
#if SILVERLIGHT
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
#endif

    }
}
