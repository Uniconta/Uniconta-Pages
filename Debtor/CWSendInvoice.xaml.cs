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
    public partial class CWSendInvoice : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Email", ResourceType = typeof(InputFieldDataText))]
        public string Emails { get; set; }
        [InputFieldData]
        [Display(Name = "SendOnlyToThisEmail", ResourceType = typeof(InputFieldDataText))]
        public bool sendOnlyToThisEmail { get; set; }

        public static bool sendInBackgroundOnly = true;
#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWSendInvoice()
        {
            this.DataContext = this;
            InitializeComponent();
            chkSendOnlyInBackGround.IsChecked = sendInBackgroundOnly;
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("Sendinvoicebyemail");
#else
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                if (string.IsNullOrWhiteSpace(txtEmail.Text))
                    txtEmail.Focus();
                else
                    OKButton.Focus();
            }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
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
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Emails = (string.IsNullOrEmpty(txtEmail.Text) || string.IsNullOrWhiteSpace(txtEmail.Text)) ? null : txtEmail.Text;
            sendOnlyToThisEmail = chkSendOnlyEmail.IsChecked.Value;
            sendInBackgroundOnly = chkSendOnlyInBackGround.IsChecked.Value;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
