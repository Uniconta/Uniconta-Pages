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
using DevExpress.Xpf.WindowsUI;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
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
            List<ServerInformation> servers = new List<ServerInformation>();
            servers.Add(new ServerInformation { Host = "smtp.office365.com", Port = 587, SSL = true });
            servers.Add(new ServerInformation { Host = "asmtp.yousee.dk", Port = 587, SSL = true });
            servers.Add(new ServerInformation { Host = "smtp.gmail.com", Port = 587, SSL = true, User = "@gmail.com" });
            cmbSMTPServer.ItemsSource = servers;
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
#if !SILVERLIGHT
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
#endif
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
