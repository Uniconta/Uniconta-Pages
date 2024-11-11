using DevExpress.Xpf.WindowsUI;
using System.Diagnostics;
using System.Windows;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for EmailUserPasswordWizard.xaml
    /// </summary>
    public partial class EmailUserPasswordWizard : UnicontaWizardControl
    {
        ServerInformation _emailSetup;
        public EmailUserPasswordWizard(object WizardData)
        {
            InitializeComponent();
            DataContext = this;
            _emailSetup = WizardData as ServerInformation;
            if (_emailSetup.Host == "smtp.gmail.com")
                hlgmail.Visibility = Visibility.Visible;

            txtUser.Text = _emailSetup?.User;
            txtPwd.Text = _emailSetup?.Password;
        }

        public override string Header { get { return string.Format(Uniconta.ClientTools.Localization.lookup("PleaseEnterOBJ"), Uniconta.ClientTools.Localization.lookup("Details")); } }

        public override bool IsLastView { get { return true; } }

        public override void NavigateView(NavigationFrame frameView)
        {
            _emailSetup.User = txtUser.Text;
            _emailSetup.Password = txtPwd.Text;
            WizardData = _emailSetup;
        }
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
