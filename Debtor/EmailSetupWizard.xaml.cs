using System.Collections.Generic;
using System.Windows;
using DevExpress.Xpf.WindowsUI;
using Uniconta.ClientTools.Controls;
using System.Diagnostics;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class EmailSetupWizard : UnicontaWizardControl
    {
        public EmailSetupWizard()
        {
            InitializeComponent();
            DataContext = this;
            cmbSMTPServer.ItemsSource = new List<ServerInformation>
            {
                new ServerInformation { Host = "smtp.office365.com", Port = 587, SSL = true },
                new ServerInformation { Host = "asmtp.yousee.dk", Port = 587, SSL = true },
                new ServerInformation { Host = "smtp.gmail.com", Port = 587, SSL = true, User = "@gmail.com" }
            }; ;
        }

        public override string Header
        {
            get
            {
                return Uniconta.ClientTools.Localization.lookup("EmailSetUp");
            }
        }

        public override bool IsLastView { get { return false; } }

        public override void NavigateView(NavigationFrame frameView)
        {
            var selectedServer = cmbSMTPServer.SelectedItem as ServerInformation;
            if (selectedServer != null)
            {
                WizardData = selectedServer;
                frameView.Navigate(new EmailUserPasswordWizard(WizardData));
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoServerSelected"), Uniconta.ClientTools.Localization.lookup("Warning"),
                    MessageBoxButton.OK);
        }
        private void cmbSMTPServer_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (cmbSMTPServer.SelectedIndex == 2)
                hlgmail.Visibility = Visibility.Visible;
            else
                hlgmail.Visibility = Visibility.Collapsed;
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }

    public class ServerInformation
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public bool SSL { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
