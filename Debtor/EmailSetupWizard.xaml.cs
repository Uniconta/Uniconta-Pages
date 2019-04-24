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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
 
    public partial class EmailSetupWizard : UnicontaWizardControl
    {
        ServerInformation _emailSetup;
        public EmailSetupWizard()
        {
            InitializeComponent();
            DataContext = this;
            List<ServerInformation> servers = new List<ServerInformation>();
            servers.Add(new ServerInformation { Host = "smtp.office365.com", Port = 587, SSL= true });
            cmbSMTPServer.ItemsSource = servers;
        }

        public override string Header
        {
            get
            {
                return  Uniconta.ClientTools.Localization.lookup("EmailSetUp");
            }
        }

        public override bool IsLastView { get { return false; } }

        public override void NavigateView(NavigationFrame frameView)
        {
            var selectedServer = cmbSMTPServer.SelectedItem as ServerInformation; 
            if (selectedServer != null)
            {
                _emailSetup = new ServerInformation();
                _emailSetup.Host = selectedServer.Host;
                _emailSetup.Port = selectedServer.Port;
                _emailSetup.SSL = selectedServer.SSL;
                WizardData = _emailSetup;
                frameView.Navigate(new EmailUserPasswordWizard(WizardData));
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoServerSelected"), Uniconta.ClientTools.Localization.lookup("Warning"),
                    MessageBoxButton.OK);
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
