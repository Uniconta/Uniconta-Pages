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
using Uniconta.Common;
using UnicontaClient.Controls;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWSetFeeAmount : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "Value", ResourceType = typeof(InputFieldDataText))]
        public double value { get; set; }

        public string CWName { get; set; }

        [InputFieldData]
        [Display(Name = "PerTransaction", ResourceType = typeof(InputFieldDataText))]
        public bool PerTransaction { get; set; }
        public string SelectedType { get; set; }

        [InputFieldData]
        [Display(Name = "CollectionLetter", ResourceType = typeof(InputFieldDataText))]
        [AppEnumAttribute(EnumName = "DebtorEmailType", ValidIndexes = new[] { 3, 4, 5, 6, 13 })]
        public string CollectionType { get; set; }

        [InputFieldData]
        [AppEnumAttribute(EnumName = "Currencies", Enumtype = typeof(Currencies))]
        [Display(Name = "Currency", ResourceType = typeof(InputFieldDataText))]
        public string FeeCurrency { get; set; }
        
        [InputFieldData]
        [Display(Name = "Charge", ResourceType = typeof(InputFieldDataText))] 
        public double Charge { get; set; }

        [InputFieldData]
        [AppEnumAttribute(EnumName = "Currencies", Enumtype = typeof(Currencies))]
        [Display(Name = "Currency", ResourceType = typeof(InputFieldDataText))]
        public string ChargeCurrency { get; set; }

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))] 
        public DateTime PrDate { get; set; }

#if !SILVERLIGHT

        protected override int DialogId => DialogTableId;
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton => true;
#endif
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
                rowFeeCharge.Height = new GridLength(0);
            }
            else
            {
                CWName = Uniconta.ClientTools.Localization.lookup("AddCollection");
                lblFee.Text = Uniconta.ClientTools.Localization.lookup("Per");
                cmbtypeValue.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Transaction"), Uniconta.ClientTools.Localization.lookup("Account") };
                cmbCurrency.ItemsSource = Utility.GetCurrencyEnum();
                cmbCollections.ItemsSource = Utility.GetDebtorCollectionLetters().OrderBy(p => p);
                cmbCollections.SelectedIndex = -1;
            }
            dePrDate.DateTime = DateTime.Now;
            
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
#if !SILVERLIGHT
            cmbtypeValue.SelectedIndex = PerTransaction ? 0 : 1;
#endif
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
