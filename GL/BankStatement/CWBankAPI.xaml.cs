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
using Uniconta.ClientTools.Page;
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
        public string ActivationCode { get; set; }
        public Bank Bank { get; set; }
        public Company Company { get; set; }
        public int Type { get; set; }

        private string[] BankFunctionLst;
        public static int type;
        CrudAPI api;

        public CWBankAPI(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
            this.api = api;
            cmbBank.ItemsSource = AppEnums.Bank.Values;
            BankFunctionLst = new string[] { Uniconta.ClientTools.Localization.lookup("Register"), Uniconta.ClientTools.Localization.lookup("Status"), Uniconta.ClientTools.Localization.lookup("Connect"), Uniconta.ClientTools.Localization.lookup("Unregister") };
            cmbBankAPIFunction.ItemsSource = BankFunctionLst;
            Type = 0;
            cmbBank.SelectedIndex = 0;
            this.Title = Uniconta.ClientTools.Localization.lookup("FinancialInstitution");
            this.Loaded += CW_Loaded;
            SetFields();
            this.Loaded += CWSelectCompany_Loaded;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtCustomerNo.Focus(); }));
        }

        private void cbCompany_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cbCompany.SelectedItem != null)
                Company = cbCompany.SelectedItem as Company;
        }
        private async void CWSelectCompany_Loaded(object sender, RoutedEventArgs e)
        {
            await BindCompany();
        }
        private async System.Threading.Tasks.Task BindCompany()
        {
            Company[] companies = await BasePage.session.GetCompanies();
            cbCompany.ItemsSource = companies;
            if (companies != null && companies.Length > 0)
                cbCompany.SelectedIndex = 0;
        }
       

        private void SetDescription(int type)
        {
            switch (type)
            {
                case 0:
                    txtDescription.Text = String.Concat("Tilmelding af regnskab til Bank", Environment.NewLine,
                                                      "Bemærk følgende vedørende kundenummer:", Environment.NewLine,
                                                      "Bank tilknyttet Bankdata: Kundenummer er 13 cifre", Environment.NewLine,
                                                      "Bank tilknyttet BEC: Kundenummer er 11 cifre", Environment.NewLine,
                                                      "Bank tilknyttet SDC: Kundenummer er 26 cifre", Environment.NewLine,
                                                      "Nordea: Kundenummer er 10 cifre");
                    break;
                case 1:
                    txtDescription.Text = "Viser en log over al kommunikation med banken";
                    break;
                case 2:
                    txtDescription.Text = Uniconta.ClientTools.Localization.lookup("Tilknyt regnskabet til en eksisterende forbindelse");
                    break;
                case 3:
                    txtDescription.Text = Uniconta.ClientTools.Localization.lookup("Afmeld forbindelsen");
                    break;
            }
        }


        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
                OKButton_Click(null, null);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string errText = null;
            Bank = cmbBank.SelectedIndex > 0 ? (Bank)cmbBank.SelectedIndex : Bank.None;

            if (Type == 0)
            {
                if (Bank == Bank.None)
                    errText = fieldCannotBeEmpty("FinancialInstitution");
                else if (string.IsNullOrEmpty(CustomerNo))
                    errText = fieldCannotBeEmpty("CustomerNo");
                else if (Bank != Bank.Nordea && string.IsNullOrEmpty(ActivationCode))
                    errText = fieldCannotBeEmpty("ActivationCode");
            }
            else
            {
                if (string.IsNullOrEmpty(CustomerNo))
                    errText = fieldCannotBeEmpty("CustomerNo");
            }

            if (errText == null)
                SetDialogResult(true);
            else
                UnicontaMessageBox.Show(errText, Uniconta.ClientTools.Localization.lookup("Warning"));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void BankAPIFunction_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            SetFields();
        }

        private void BankAPIBank_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            Bank = cmbBank.SelectedIndex > 0 ? (Bank)cmbBank.SelectedIndex : Bank.None;
            liActivationCode.Visibility = Bank == Bank.Nordea ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SetFields()
        {
            type = Type;

            SetDescription(Type);

            //Tilmeld=0, Status=1, Tilknyt=2, Afmeld=3
            if (Type == 1 || Type == 3)
                lgConnect.Visibility = Visibility.Collapsed;
            else if (Type == 0)
            {
                lgConnect.Visibility = Visibility.Visible;
                liBank.Visibility = Visibility.Visible;
                liActivationCode.Visibility = Visibility.Visible;
                liCompany.Visibility = Visibility.Collapsed;
            }
            else if (Type == 2)
            {
                lgConnect.Visibility = Visibility.Visible;
                liBank.Visibility = Visibility.Collapsed;
                liActivationCode.Visibility = Visibility.Collapsed;
                liCompany.Visibility = Visibility.Visible;
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
