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
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreateBackupConfirmation : ChildWindow
    {
        string ConfirmWord;
        public string name;
        public bool copyTrans, copyPhysicalVouchers, copyAttachments;
        public CreateBackupConfirmation()
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("Backup");
            ConfirmWord = Uniconta.ClientTools.Localization.lookup("Start");
            lblAskBackup.Text= Uniconta.ClientTools.Localization.lookup("AskBackup");
            lblBackupTime.Text= Uniconta.ClientTools.Localization.lookup("BackupTime");
            lblprompt.Text = string.Format(Uniconta.ClientTools.Localization.lookup("TypeToStart"), Uniconta.ClientTools.Localization.lookup("Start"));
            cbxCopyTrans.Content= string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Transactions"));
            cbxPhysicalVoucher.Content= string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("PhysicalVouchers"));
            cbxCopyAttachments.Content= string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Attachments"));
            lblName.Text= Uniconta.ClientTools.Localization.lookup("Name");
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            copyTrans = cbxCopyTrans.IsChecked.GetValueOrDefault();
            copyPhysicalVouchers = cbxPhysicalVoucher.IsChecked.GetValueOrDefault();
            copyAttachments = cbxCopyAttachments.IsChecked.GetValueOrDefault();
            name = txtName.Text;
            this.DialogResult = (string.Compare(txtStart.Text, ConfirmWord, StringComparison.CurrentCultureIgnoreCase) == 0);
         
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
