using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWSendStatementEmail : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "All", ResourceType = typeof(InputFieldDataText))]
        public bool SendAll { get; set; }
        [InputFieldData]
        [Display(Name = "SelectedRows", ResourceType = typeof(InputFieldDataText))]
        public bool SendOnlyMarked { get; set; }

        public int SentEmailCount
        {
            get
            {
                if (SendAll)
                    return totalEmails;
                else if (SendOnlyMarked)
                    return markedEmails;

                return 0;
            }
        }

        int totalEmails = 0, markedEmails = 0;

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }

        public CWSendStatementEmail()
        {
            InitializeComponent();
            this.DataContext = this;

            this.Title = Uniconta.ClientTools.Localization.lookup("Sendinvoicebyemail");
            this.Loaded += CW_Loaded;
        }

        public void UpdateCount(int totalLines, int markedLines)
        {
            totalEmails = totalLines;
            markedEmails = markedLines;
        }
      
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            if (markedEmails > 0)
                cbxMarked.IsChecked = true;
            else
                cbxAll.IsChecked = true;
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (cbxAll.IsChecked == false && cbxMarked.IsChecked == false)
                cbxAll.IsChecked = true;

            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void AllChecked(object sender, RoutedEventArgs e)
        {
            ShowEmailMessage(totalEmails);
        }

        private void MarkedChecked(object sender, RoutedEventArgs e)
        {

            ShowEmailMessage(markedEmails);
        }

        private void UnChecked(object sender, RoutedEventArgs e)
        {
            emailsendMsg.Visibility = Visibility.Collapsed;
        }

        private void ShowEmailMessage(int count)
        {
            if (count > 0)
                emailsendMsg.Text = string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailCount"), count);
            else
                emailsendMsg.Text = string.Format(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"));

            emailsendMsg.Visibility = Visibility.Visible;
        }
    }
}
