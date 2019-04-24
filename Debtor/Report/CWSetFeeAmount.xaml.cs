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
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWSetFeeAmount : ChildWindow
    {
        public double value { get; set; }
        public string CWName { get; set; }
        public string SelectedType { get; set; }
        public string CollectionType { get; set; }
        public string FeeCurrency { get; set; }

        public CWSetFeeAmount(bool AddInterest)
        {
            InitializeComponent();
            this.DataContext = this;
#if SILVERLIGHT
                Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            if (AddInterest)
            {
                CWName = Uniconta.ClientTools.Localization.lookup("Interest") + " %";
                rowFeeTransaction.Height = new GridLength(0);
                rowCollectionLetter.Height = new GridLength(0);
                colFeeCurrency.Width = new GridLength(0);
                colMarginFeeCurrency.Width = new GridLength(0);
            }
            else
            {
                CWName = Uniconta.ClientTools.Localization.lookup("AddCollection");
                lblFee.Text = Uniconta.ClientTools.Localization.lookup("Per");
                cmbtypeValue.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Transaction"), Uniconta.ClientTools.Localization.lookup("Account") };
                SelectedType = Uniconta.ClientTools.Localization.lookup("Transaction");
                cmbCurrency.ItemsSource = Utility.GetCurrencyEnum();
                cmbCollections.ItemsSource = Utility.GetDebtorCollectionLetters().OrderBy(p => p);
                cmbCollections.SelectedIndex = 0;
            }

            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (string.IsNullOrWhiteSpace(txtValue.Text))
                    txtValue.Focus();
                else
                    OKButton.Focus();
            }));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
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
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }
    }
}
