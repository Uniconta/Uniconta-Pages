using UnicontaClient.Utilities;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwSendEmailToApprover : ChildWindow
    {
       
        public string Approver { get; set; }
        [InputFieldData]
        [Display(Name = "Note", ResourceType = typeof(InputFieldDataText))]
        public string Note { get; set; }

#if !SILVERLIGHT
        public int DialogTableId;
        protected override int DialogId { get { return DialogTableId; } }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        Dictionary<string, string> approverDic;
        public CwSendEmailToApprover(VouchersClient document)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("ResendToApprover");
            approverDic = new Dictionary<string, string>();
            if (document._Approver1 != null)
                approverDic.Add(document._Approver1, string.Format("{0} ({1})", document._Approver1, document.Approver1Name));
            if (document._Approver2 != null && document.Approver2 != document.Approver1)
                approverDic.Add(document._Approver2, string.Format("{0} ({1})", document._Approver2, document.Approver2Name));
            cmbEmployee.ItemsSource = approverDic;
            cmbEmployee.SelectedIndex = 0;
            this.Loaded += CwSendEmailToApprover_Loaded;
        }

        private void CwSendEmailToApprover_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() =>{ cmbEmployee.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbEmployee.SelectedItem != null)
            {
                Approver = approverDic.FirstOrDefault(x=>x.Value == cmbEmployee.SelectedText).Key;
                Note = txtNote.Text;
                this.DialogResult = true;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
