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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWExchangeRate : ChildWindow
    {
        public Currencies _Currency;
        [AppEnumAttribute(EnumName = "Currencies")]
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

        public double ExchangeRate { get; set; }

        public CWExchangeRate()
        {
            DataContext = this;
            InitializeComponent();
            Title = Uniconta.ClientTools.Localization.lookup("ExchangeRate");
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
    }
}
