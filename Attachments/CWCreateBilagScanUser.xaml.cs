using Bilagscan;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.Common;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWCreateBilagScanUser : ChildWindow
    {
        private readonly CrudAPI api;
        private readonly SQLCache employees;

        public CWCreateBilagScanUser(CrudAPI api, string title)
        {
            this.api = api;
            this.DataContext = this;
            InitializeComponent();
            this.Title = title;
            this.Loaded += CW_Loaded;

            employees = api.GetCache(typeof(Uniconta.DataModel.Employee)) ??
                api.LoadCache(typeof(Uniconta.DataModel.Employee)).Result;

            SetEmployeeInfo();
        }

        private void SetEmployeeInfo()
        {
            foreach (Uniconta.DataModel.Employee employee in employees.GetRecords)
            {
                if (employee?._UserLogidId == api.session.LoginId)
                {
                    var names = employee._Name.Split(' ');
                    txtFirstName.Text = names.First();
                    txtLastName.Text = names.Last();
                    txtEmail.Text = employee._Email;
                }
            }
        }

        private void CW_Loaded(object sender, RoutedEventArgs e) =>
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                SetDialogResult(false);
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var email = txtEmail.Text;
            var response = await UserService.Create(txtFirstName.Text, txtLastName.Text, email, txtPassword.Text);
            var error = response?.error;
            if (error == null)
            {
                SetDialogResult(true);
                return;
            }

            var message = error.message;
            if (message.Contains("e-mail address is already in use"))
            {
                var emailInUse = string.Format(Localization.lookup("EmailObjInUse"), email) + ". " + Localization.lookup("InviteUser") +
                        " " + string.Format(Localization.lookup("ToOBJ"), Localization.lookup("Organisation")).ToLower() + "?";

                var inviteUser = UnicontaMessageBox.Show(emailInUse, Localization.lookup("Bilagscan"), MessageBoxButton.YesNo);
                if (MessageBoxResult.Yes.Equals(inviteUser))
                {
                    SetDialogResult(true);
                    return;
                }
            }
            else
                PromptBilagscanWarningMessage(message);

            // TODO: Creating a support ticket for paperflow, this does not work
            /*
            var emailExistMessage = string.Format(Uniconta.ClientTools.Localization.lookup("AlreadyExistOBJ") + ". {1}?",
                Uniconta.ClientTools.Localization.lookup("Email"), Uniconta.ClientTools.Localization.lookup("ChangePassword"));

            var resut = UnicontaMessageBox.Show(emailExistMessage, Uniconta.ClientTools.Localization.lookup("Bilagscan"), MessageBoxButton.YesNo);
            if (resut == MessageBoxResult.Yes)
            {
                try
                {
                    await User.ForgotPassword(txtEmail.Text);
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PasswordNote"),
                        Uniconta.ClientTools.Localization.lookup("Bilagscan"));
                }
                catch (Exception ex2)
                {
                    PromptBilagscanWarningMessage(ex2.Message);
                    SetDialogResult(false);
                    return;
                }
            }*/

            SetDialogResult(false);
        }

        private void PromptBilagscanWarningMessage(string message) =>
            UnicontaMessageBox.Show(string.Format("{0}: {1}", Localization.lookup("Bilagscan"), message), Localization.lookup("Warning"));

        private void CancelButton_Click(object sender, RoutedEventArgs e) => SetDialogResult(false);
    }
}
