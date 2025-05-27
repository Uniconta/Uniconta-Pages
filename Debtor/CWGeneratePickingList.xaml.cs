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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWGeneratePickingList.xaml
    /// </summary>
    public partial class CWGeneratePickingList : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime SelectedDate { get; set; }

        [InputFieldData]
        [Display(Name = "Email", ResourceType = typeof(InputFieldDataText))]
        public string EmailList { get; set; }

        [InputFieldData]
        [Display(Name = "PrintImmediately", ResourceType = typeof(InputFieldDataText))]
        public bool PrintDocument { get; set; }

        [InputFieldData]
        [Display(Name = "Preview", ResourceType = typeof(InputFieldDataText))]
        public bool ShowDocument { get; set; }

        [InputFieldData]
        [Display(Name = "NumberOfCopies", ResourceType = typeof(InputFieldDataText))]
        public short NumberOfPages { get; set; } = 1;

        [InputFieldData]
        [Display(Name = "SendInvoiceByEmail", ResourceType = typeof(InputFieldDataText))]
        public bool SendByEmail { get; set; }

        [InputFieldData]
        [Display(Name = "SendOnlyToThisEmail", ResourceType = typeof(InputFieldDataText))]
        public bool sendOnlyToThisEmail { get; set; }

        [InputFieldData]
        [Display(Name = "SendByOutlook", ResourceType = typeof(InputFieldDataText))]
        public bool SendByOutlook { get; set; }

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
        public CWGeneratePickingList(bool showquickPrint = true, bool showEmail = true)
        {
            this.DataContext = this;
            InitializeComponent();
            Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Generate"), Uniconta.ClientTools.Localization.lookup("PickingList"));
            if (!showquickPrint && !showEmail)
                lgSecondary.Visibility = Visibility.Collapsed;
            else if (!showEmail)
                lgEmail.Visibility = Visibility.Collapsed;
            else if (!showquickPrint)
                lgPrint.Visibility = Visibility.Collapsed;
        }

       

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                SetDialogResult(false);
            else if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(sender, e);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }


        public CWGeneratePickingList(string accountName, bool showQuickPrint, bool showEmail, string debtorName, bool hasEmail) : this(showQuickPrint, showEmail)
        {
            if (!string.IsNullOrEmpty(accountName))
                txtAccountName.Text = accountName;
            else
                lgAccount.Visibility = txtAccountName.Visibility = Visibility.Collapsed;

            if (showEmail)
            {
                if (!string.IsNullOrEmpty(debtorName) && !hasEmail)
                {
                    txtNoMailMsg.Text = string.Format(Uniconta.ClientTools.Localization.lookup("DebtorHasNoEmail"), debtorName);
                    chkSendEmail.IsEnabled = false;
                    chkSendEmail.IsChecked = false;
                }
                else
                    liNoEmailMsg.Visibility = Visibility.Collapsed;
            }
        }

        public CWGeneratePickingList(bool isMultiInvoicePage) : this(null, true, true, null, false)
        {
            if (isMultiInvoicePage)
            {
                liSendByOutlook.Visibility = Visibility.Collapsed;
                liNumberOfPages.Visibility = Visibility.Collapsed;
                liNoEmailMsg.Visibility = Visibility.Collapsed;
                chkSendEmail.IsEnabled = true;
            }
        }

        private void chkShowInvoice_Checked(object sender, RoutedEventArgs e)
        {
            chkPrintInvoice.IsChecked = false;
        }

        private void chkPrintInvoice_Checked(object sender, RoutedEventArgs e)
        {
            chkShowInvoice.IsChecked = false;
        }

        private void chkSendEmail_Checked(object sender, RoutedEventArgs e)
        {
            chkSendOutlook.IsChecked = false;
        }

        private void chkSendOutlook_Checked(object sender, RoutedEventArgs e)
        {
            chkSendEmail.IsChecked = false;
        }
    }
}
