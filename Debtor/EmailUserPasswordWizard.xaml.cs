using DevExpress.Xpf.WindowsUI;
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
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;

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
            txtUser.Text = _emailSetup?.User;
            txtPwd.Text = _emailSetup?.Password;
        }

        public override string Header { get { return Uniconta.ClientTools.Localization.lookup("SmtpUserPassword"); } }

        public override bool IsLastView { get { return true; } }

        public override void NavigateView(NavigationFrame frameView)
        {
            _emailSetup.User = txtUser.Text;
            _emailSetup.Password = txtPwd.Text;
            WizardData = _emailSetup;
        }
    }
}
