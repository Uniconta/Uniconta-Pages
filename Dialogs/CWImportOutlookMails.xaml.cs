using DevExpress.Utils.Win;
using DevExpress.XtraScheduler.Outlook.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;

namespace UnicontaClient.Pages.Attachments
{
    public partial class CWImportOutlookMails : ChildWindow
    {
        public UserNotesClient userNote;
        public CWImportOutlookMails()
        {
            InitializeComponent();
            this.DataContext = this;
            dtDateFilter.DateTime = DateTime.Now;
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Mail"));
            this.KeyDown += CWCreateFolder_KeyDown;
            this.Loaded += CWCreateFolder_Loaded;
        }

        private void CWCreateFolder_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                SaveButton.Focus();
                rdbInbox.IsChecked = true;
            }));
        }

        private void CWCreateFolder_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                ImportButton_Click(null, null);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            userNote = lstMails.SelectedItem as UserNotesClient;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void ViewMail_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var lbl = sender as System.Windows.Controls.Label;
            var userNote = lbl.Tag as UserNotesClient;
            if (userNote?.Token != null)
                OutlookNotes.ViewOutlookMail(userNote);
        }

        private void rdbSentItems_Checked(object sender, RoutedEventArgs e)
        {
            Import();
        }

        void Import()
        {
            var fromDate = dtDateFilter.DateTime;
            var toDate = dtDateFilter.DateTime.AddDays(1);
            int folder = rdbSentItems.IsChecked == true ? 1 : 0;
            var notes = OutlookNotes.ImportMails(folder, fromDate, toDate);
            lstMails.ItemsSource = notes;
        }
        private void rdbInbox_Checked(object sender, RoutedEventArgs e)
        {
            Import();
        }

        private void dtDateFilter_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            Import();
        }
    }
}

