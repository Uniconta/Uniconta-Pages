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
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwChangeAmountCurrency : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Amount", ResourceType = typeof(InputFieldDataText))]
        public double AmountCur { get; set; }

        public Currencies _Currency;
        [AppEnumAttribute(EnumName = "Currencies")]
        [InputFieldData]
        public string Currency
        {
            get { return AppEnums.Currencies.ToString((int)_Currency); }
            set
            {
                if (value == null) return;
                var _Cur = (Currencies)AppEnums.Currencies.IndexOf(value);
                if (_Cur != _Currency)
                {
                    _Currency = _Cur;
                    NotifyPropertyChanged("Currency");
                }
            }
        }
        protected override int DialogId => 2000000111;
        public CwChangeAmountCurrency(GLTransClient glTrans)
        {
            AmountCur = glTrans._AmountCur;
            Currency = glTrans.Currency;
            InitializeComponent();
            this.DataContext = this;
            this.Title = String.Format(Uniconta.ClientTools.Localization.lookup("ChangeOBJ"),
                Uniconta.ClientTools.Localization.lookup("AmountCur"));
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
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("AmountCur"))), Uniconta.ClientTools.Localization.lookup("Warning"));
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
