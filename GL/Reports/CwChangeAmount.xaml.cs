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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;
using UnicontaClient.Controls;
using DevExpress.Xpf.WindowsUI.Navigation;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwChangeAmount : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Amount", ResourceType = typeof(InputFieldDataText))]
        public double Amount { get; set; }

        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string Comment { get; set; }

        public string AmountFormat { get; private set; }

        public CwChangeAmount(GLTransClient glTrans, bool hasDecimal)
        {
            Amount = glTrans._Amount;
            AmountFormat = hasDecimal ? "n2" : "n0";
            InitializeComponent();
            this.DataContext = this;
            this.Title = String.Format(Uniconta.ClientTools.Localization.lookup("ChangeOBJ"),
                Uniconta.ClientTools.Localization.lookup("Amount"));
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtAmount.Focus(); }));
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
            if (string.IsNullOrWhiteSpace(txtAmount.Text))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Amount"))), Uniconta.ClientTools.Localization.lookup("Warning"));
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
