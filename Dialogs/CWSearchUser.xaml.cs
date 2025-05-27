using Uniconta.API.System;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Windows.Input;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.Common.User;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Pages;
using DevExpress.Utils.Extensions;
using DevExpress.Xpf.Editors;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWSearchUser : ChildWindow
    {
        CompanyAccessAPI comApi;
        int userType;
        public UserTypes UserType { get { return (UserTypes)userType; } set { userType =  (int)value; SetAsPerUserType(); } }
        public CWSearchUser(CompanyAccessAPI api)
        {
            comApi = api;
            this.DataContext = this;
            InitializeComponent();


            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("User"));

            lstSetupType.ItemsSource = new string[] { string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("New")), Uniconta.ClientTools.Localization.lookup("Existing") };
            this.Loaded += CW_Loaded;
            lstSetupType.SelectedIndexChanged += LstSetupTypeOnSelectedIndexChanged;
            lblCopyUserRights.Text = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("UserRights"));

            if (api.session.Uid != api.CompanyEntity._OwnerUid || api.session.User._Role == (int)UserRoles.Accountant)
            {
                lblSetupText.Visibility = Visibility.Collapsed;
                lblSetupType.Visibility = Visibility.Collapsed;
                lstSetupType.Visibility = Visibility.Collapsed;
            }
        }

        void SetAsPerUserType()
        {
            switch (UserType)
            {
                case UserTypes.StandardUser:
                    this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("StandardUser"));
                    break;
                case UserTypes.ProjectTimeUser:
                    this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("ProjectTimeUser"));
                    cmbUserRights.SelectedIndex = 3;
                    break;
                case UserTypes.InvoiceUser:
                    this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceUser"));
                    cmbUserRights.SelectedIndex = 3;
                    break;
            }
        }

        public CWSearchUser(CompanyAccessAPI api, CompanyUserAccessClient[] users) : this(api)
        {
            cmbUsers.ItemsSource = users;
        }

        private void LstSetupTypeOnSelectedIndexChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            if (lstSetupType.SelectedIndex == 0)
            {
                lblSpecifyId.Visibility = Visibility.Collapsed;
                txtSearch.Visibility = Visibility.Collapsed;
                lblUserRights.Visibility = Visibility.Collapsed;
                cmbUserRights.Visibility = Visibility.Collapsed;
                lblCopyUserRights.Visibility = Visibility.Collapsed;
                cmbUsers.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblSpecifyId.Visibility = Visibility.Visible;
                txtSearch.Visibility = Visibility.Visible;
                lblUserRights.Visibility = Visibility.Visible;
                cmbUserRights.Visibility = Visibility.Visible;
                lblCopyUserRights.Visibility = Visibility.Visible;
                cmbUsers.Visibility = Visibility.Visible;
            }
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtSearch.Focus(); }));
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
        public string SearchedText;
        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (lstSetupType.SelectedIndex != 0 && !string.IsNullOrEmpty(txtSearch.Text))
            {
                SearchedText = txtSearch.Text;
                var selectedUser = cmbUsers.SelectedItem as CompanyUserAccessClient;
                ErrorCodes err;
                var expiryDate = txtExpire.DateTime;
                if (selectedUser == null)
                    err = await comApi.GiveNewUserAccess(SearchedText, (CompanyPermissions)cmbUserRights.SelectedIndex, expiryDate);
                else
                    err = await comApi.GiveNewUserAccess(SearchedText, (CompanyPermissions)selectedUser._Rights, expiryDate);

                if (err != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(err);
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void cmbUsers_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                ((ComboBoxEdit)sender).EditValue = null;
            }
        }
    }
}

