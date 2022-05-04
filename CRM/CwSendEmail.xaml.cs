using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwSendEmail : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.CompanySMTP))]
        public CompanySMTPClient CompanySMTP { get; set; }

        public bool SendTestEmail { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public CrmFollowUpClient FollowUp { get; set; }
        public bool AddFollowUp { get; set; }
        public bool IncludeAttachements { get; set; }
        CrudAPI api;

        public CwSendEmail(CrudAPI _api)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("EmailDocuments");
            leCompanySMTP.api = _api;
            this.Loaded += CW_Loaded;
            api = _api;
            tbChkFollowUp.Text = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("FollowUp"));
        }

        public void ShowAttachments()
        {
            chkIncludeAttach.Visibility = tbChkIncludeAttach.Visibility = Visibility.Visible;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { leCompanySMTP.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (CompanySMTP == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("SMTPUser")),
                    Uniconta.ClientTools.Localization.lookup("Warning"));
                leCompanySMTP.Focus();
                return;
            }
            else
                SetDialogResult(true);
        }

        private void leCompanySMTP_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (leCompanySMTP.SelectedItem == null || leCompanySMTP.SelectedIndex == -1) return;
            this.CompanySMTP = leCompanySMTP.SelectedItem as CompanySMTPClient;
        }

        private void chkSendTestEmail_Checked(object sender, RoutedEventArgs e)
        {
            var user = BasePage.session.User;
            txtEmail.Text = Email = user?._Email;
            txtName.Text = Name = user?._Name;
            chkAddFollowUp.IsChecked = false;
            chkAddFollowUp.Visibility = tbChkFollowUp.Visibility = Visibility.Collapsed;
        }

        private void chkSendTestEmail_Unchecked(object sender, RoutedEventArgs e)
        {
            txtEmail.Text = Email = null;
            txtName.Text = Name = null;
            chkAddFollowUp.Visibility = tbChkFollowUp.Visibility = Visibility.Visible;
        }

        private void chkAddFollowUp_Checked(object sender, RoutedEventArgs e)
        {
            var cw = new CwAddFollowUp(api);
            cw.Closed += delegate
            {
                if (cw.DialogResult == true)
                {
                    FollowUp = cw.NewFollowUp;
                    txtFollowUp.Text = FollowUp.Created.ToString();
                }
                else
                    chkAddFollowUp.IsChecked = false;
            };
            cw.Show();
        }

        private void chkAddFollowUp_Unchecked(object sender, RoutedEventArgs e)
        {
            FollowUp = null;
        }
    }
}
