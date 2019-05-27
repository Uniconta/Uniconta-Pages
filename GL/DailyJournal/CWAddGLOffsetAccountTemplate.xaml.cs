using System;
using System.Windows;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.Windows.Controls;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWAddGLOffsetAccountTemplate : ChildWindow
    {
        public string  offSetAccountName {get;set;}
        public CWAddGLOffsetAccountTemplate(string name)
        {
            this.DataContext = this;
            offSetAccountName = name;
            InitializeComponent();
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("OffsetAccountTemplate");
#else
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            txtAccount.Text = offSetAccountName;      
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtAccount.Focus(); }));
            txtAccount.KeyDown += txtAccount_KeyDown;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtAccount.Text) || string.IsNullOrWhiteSpace(txtAccount.Text)) return;
            offSetAccountName = txtAccount.Text;
            this.DialogResult = true;
        }

        private void txtAccount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(sender, e);
            else if (e.Key == Key.Escape)
                CancelButton_Click(sender, e);
        }

    }
}
