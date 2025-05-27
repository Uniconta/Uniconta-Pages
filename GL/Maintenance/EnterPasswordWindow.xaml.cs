using System;
using System.Windows;
using Uniconta.ClientTools;
using System.Windows.Input;
using Uniconta.DataModel;
using System.Windows.Controls;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for EnterPasswordWindow.xaml
    /// </summary>
    public partial class EnterPasswordWindow : ChildWindow
    {
        public string Password { get; set; }
        public EnterPasswordWindow()
        {
            InitializeComponent();
            this.Title = "Password Confirmation";
            lblHeader.Text = "Please enter the password";
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
       
                Password = txtPassword.Password;
                if (string.IsNullOrEmpty(Password))
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Password")), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }
                SetDialogResult(true);          
           
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
