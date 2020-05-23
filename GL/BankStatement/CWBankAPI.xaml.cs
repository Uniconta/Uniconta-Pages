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
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWBankAPI.xaml
    /// </summary>
    public partial class CWBankAPI : ChildWindow
    {
        public string CustomerNo { get; set; }
        public string  ActivationCode { get; set; }
        public Bank bank { get; set; }
        public static int Type { get; set; } //TODO:Husk at kompilere i VS2015 inden frigivelse
        private string[] BankFunctionLst;
        CrudAPI api;
        private const int HEIGTH_STATUS = 120; //TODO:Mangler styring af højden
        private const int HEIGTH_CONNECT = 180;


        public CWBankAPI(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
            this.api = api;
            cmbBank.ItemsSource = AppEnums.Bank.Values;
            BankFunctionLst = new string[] { Uniconta.ClientTools.Localization.lookup("Create"), Uniconta.ClientTools.Localization.lookup("Status") };
            cmbBankAPIFunction.ItemsSource = BankFunctionLst;
            cmbBankAPIFunction.SelectedIndex = Type;
            bank = Bank.None;
            cmbBank.SelectedIndex = (int)bank;
            this.Title = "Bank Connect";
            this.Loaded += CW_Loaded;
            SetFields();
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
#if WPF
            Dispatcher.BeginInvoke(new Action(() => { txtCustomerNo.Focus(); }));
#endif
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
                OKButton_Click(null, null);
        }
       
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Type = cmbBankAPIFunction.SelectedIndex;
            bank = (Bank)cmbBank.SelectedIndex;

            string errText = null;
            if (Type == 0)
            {
                if (cmbBank.SelectedIndex == -1)
                    errText = fieldCannotBeEmpty("FinancialInstitution");
                else if (string.IsNullOrEmpty(CustomerNo))
                    errText = fieldCannotBeEmpty("CustomerNo");
                else if (string.IsNullOrEmpty(ActivationCode))
                    errText = fieldCannotBeEmpty("ActivationCode");
            }
            else
            {
                if (string.IsNullOrEmpty(CustomerNo))
                    errText = fieldCannotBeEmpty("CustomerNo");
            }

            if (errText == null)
            {
                this.DialogResult = true;
            }
            else
            { 
                UnicontaMessageBox.Show(errText, Uniconta.ClientTools.Localization.lookup("Warning"));
                this.DialogResult = false;
                return;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void BankAPIFunction_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            SetFields();
        }

        private void SetFields()
        {
            Type = cmbBankAPIFunction.SelectedIndex;

            if (Type == 1)
            {
                lblBank.Visibility = Visibility.Collapsed;
                cmbBank.Visibility = Visibility.Collapsed;
               
                lblActivationCode.Visibility = Visibility.Collapsed;
                txtActivationCode.Visibility = Visibility.Collapsed;
            }
            else
            {
                lblBank.Visibility = Visibility.Visible;
                cmbBank.Visibility = Visibility.Visible;

                lblActivationCode.Visibility = Visibility.Visible;
                txtActivationCode.Visibility = Visibility.Visible;
            }
        }

        private static string fieldCannotBeEmpty(string field)
        {
            return String.Format("{0} ({1})",
                    Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"),
                    Uniconta.ClientTools.Localization.lookup(field));
        }
    }
}
