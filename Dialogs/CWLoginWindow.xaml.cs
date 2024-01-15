using Uniconta.API.Service;
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
using Uniconta.ClientTools;
using System.Windows;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWLoginWindow : ChildWindow
    {
        Session session;
        string loginId;
        public CWLoginWindow(string loginId, Session sess)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("LoginWindowTitle");  
            session = sess;
            this.loginId = loginId;
            txtLoginId.Text = loginId;
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtPassword.Focus(); }));
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string password = txtPassword.Password;
            if (string.IsNullOrEmpty(password))
                return;
            var err = await session.LoginAsync(loginId, password, LoginPage.PCtype, LoginPage.ApplicationGuid, Uniconta.ClientTools.Localization.InititalLanguageCode);
            if (err == Uniconta.Common.ErrorCodes.Succes)
                SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
        
        private void ChildWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && this.OKButton.IsEnabled)
            {
                this.OKButton_Click(sender, e);
            }
        }
    }
}

