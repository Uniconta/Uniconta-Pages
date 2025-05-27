using DevExpress.CodeParser;
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
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection.OAuth2.View
{
    /// <summary>
    /// Interaction logic for AuthCodeView.xaml
    /// </summary>
    public partial class AuthCodeView : ChildWindow
    {
        public AuthCodeView(string background)
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public string AuthCode { get; private set; }

        private void btnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtAuthCode.Text))
            {
                System.Windows.MessageBox.Show("Please enter a value in the textbox.");
                return;
            }
            AuthCode = txtAuthCode.Text;
            DialogResult = true;
            Close();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = System.Windows.MessageBox.Show("Are you sure you wish to cancel? You will have to start over if you wish to try again.", "Cancel", MessageBoxButton.YesNo);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    DialogResult = false;
                    Close();
                    return;
                case MessageBoxResult.No:
                    return;
                default:
                    return;
            }
        }
    }
}
