using ISO20022CreditTransfer;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWBankAPI.xaml
    /// </summary>
    public partial class CWBankAPI : ChildWindow
    {
        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.BankStatement))]
        [Display(Name = "BankAccount", ResourceType = typeof(InputFieldDataText))]
        public string BankAccount { get; set; }

        [InputFieldData]
        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime FromDate { get; set; }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime ToDate { get; set; }

        [InputFieldData]
        [Display(Name = "ServiceId", ResourceType = typeof(InputFieldDataText))]
        public string ServiceId { get; set; }

        [InputFieldData]
        [Display(Name = "ActivationCode", ResourceType = typeof(InputFieldDataText))]
        public string ActivationCode { get; set; }

        public Bank Bank { get; set; }

        public Company Company { get; set; }
        public static int Type { get; set; }
        private int BankService;
        public string BankServiceName;

        #region Variables
        private string[] BankServiceLst;
        bool isAiia;
        #endregion

        #region Constants
        private const string BANKCONNECT = "Bank Connect";
        private const string NORDEA = "Nordea";
        private const string AIIA = "AutoBanking";
        #endregion


        CrudAPI api;

        public CWBankAPI(CrudAPI api, BankStatementClient bankStatement, int bankService)
        {
            InitializeComponent();
            this.api = api;
            ServiceId = bankStatement.ServiceId;
           
            BankServiceLst = new string[] { AIIA, BANKCONNECT, NORDEA };
            BankService = bankService;
            BankServiceName = BankServiceLst[BankService];

            string accountText = null;
            isAiia = bankService == 0;
            if (isAiia)
                accountText = string.Concat(" ", Uniconta.ClientTools.Localization.lookup("Account"), ": ", bankStatement.Account," - ",bankStatement.Name);

            BankAccount = bankStatement._Account;
            cmbBank.ItemsSource = AppEnums.Bank.Values;
           
            cmbBankAPIFunction.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Register"), Uniconta.ClientTools.Localization.lookup("Connect"), Uniconta.ClientTools.Localization.lookup("Unregister"), Uniconta.ClientTools.Localization.lookup("WebServiceInfo") };

            cmbBankAPIFunction.SelectedIndex = Type;
            cmbBank.SelectedIndex = 0;

            FromDate = FromDate != DateTime.MinValue ? FromDate : BasePage.GetSystemDefaultDate();
            ToDate = ToDate != DateTime.MinValue ? ToDate : BasePage.GetSystemDefaultDate();

            this.DataContext = this;

            this.Title = string.Concat(Uniconta.ClientTools.Localization.lookup("FinancialInstitution")," (", BankServiceName,")", accountText);
            this.Loaded += CW_Loaded;
            SetFields();
            this.Loaded += CWSelectCompany_Loaded;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtServiceId.Focus(); }));
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


        private void SetDescription()
        {
            switch (Type)
            {
                case 0: //Register
                    if (BankService == 1)
                        txtDescription.Text = String.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("RegisterCompanyTo"), BANKCONNECT), Environment.NewLine,
                                                          "Bankdata: ", Uniconta.ClientTools.Localization.lookup("ServiceId"), " = 13 ", Uniconta.ClientTools.Localization.lookup("Digits").ToLower(), Environment.NewLine,
                                                          "BEC: ", Uniconta.ClientTools.Localization.lookup("ServiceId"), " = 11 ", Uniconta.ClientTools.Localization.lookup("Digits").ToLower(), Environment.NewLine,
                                                          "SDC: ", Uniconta.ClientTools.Localization.lookup("ServiceId"), " = 26 ", Uniconta.ClientTools.Localization.lookup("Digits").ToLower(), Environment.NewLine);
                    else
                        txtDescription.Text = String.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("RegisterCompanyTo"), NORDEA), Environment.NewLine, NORDEA, ": ", Uniconta.ClientTools.Localization.lookup("ServiceId"), " = 10 ", Uniconta.ClientTools.Localization.lookup("Digits").ToLower());
                    break;
                case 1: txtDescription.Text = Uniconta.ClientTools.Localization.lookup("AddCompanyToConnection"); break;//Connect
                case 2: txtDescription.Text = string.Concat(Uniconta.ClientTools.Localization.lookup("Unregister"), " ", Uniconta.ClientTools.Localization.lookup("Connection").ToLower()); break;//Unregister
                case 3: txtDescription.Text = string.Format(Uniconta.ClientTools.Localization.lookup("InfoAgreement"), BankServiceLst[BankService]); break;//Service Info
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

            if (isAiia)
            {
                if (ToDate > DateTime.Now.Date)
                    errText = string.Format(Uniconta.ClientTools.Localization.lookup("ValueMayNoBeGreater"), Uniconta.ClientTools.Localization.lookup("ToDate"), Uniconta.ClientTools.Localization.lookup("TodaysDate").ToLower());
                if (FromDate < DateTime.Now.Date.AddYears(-2))
                    errText = string.Format(Uniconta.ClientTools.Localization.lookup("PleaseNotOBJ"), string.Concat(Uniconta.ClientTools.Localization.lookup("ToDate"), " >= ", DateTime.Now.Date.AddYears(-2).ToString("dd.MM.yyyy")));
            }
            else if (Type == 0)
            {
                if (Bank == Bank.None)
                    errText = fieldCannotBeEmpty("FinancialInstitution");
                else if (string.IsNullOrEmpty(ServiceId))
                    errText = fieldCannotBeEmpty("ServiceId");
                else if (Bank != Bank.Nordea && string.IsNullOrEmpty(ActivationCode))
                    errText = fieldCannotBeEmpty("ActivationCode");
            }
            else
            {
                if (string.IsNullOrEmpty(ServiceId))
                    errText = fieldCannotBeEmpty("ServiceId");
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

        private void BankService_SelectedIndexChanged(object sender, RoutedEventArgs e)
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
            if (BankService == 0)
            {
                lgParm.Visibility = Visibility.Visible;
                lgType.Visibility = Visibility.Collapsed;
                lgCustomer.Visibility = Visibility.Collapsed;
                liBank.Visibility = Visibility.Collapsed;
                liCompany.Visibility = Visibility.Collapsed;
                lgConnect.Visibility = Visibility.Collapsed;

                txtDescription.Text = Uniconta.ClientTools.Localization.lookup("RetrieveBankTransPeriod");
                return;
            }
            
            SetDescription();

            lgParm.Visibility = Visibility.Visible;
            lgCustomer.Visibility = Visibility.Visible;
            lgConnect.Visibility = Visibility.Visible;

            //Tilmeld=0, Tilknyt=1, Afmeld=2, Info=3
            if (Type == 0)
            {
                liBank.Visibility = Visibility.Visible;
                liActivationCode.Visibility = Visibility.Visible;
                liCompany.Visibility = Visibility.Collapsed;
            }
            else if (Type == 2 || Type == 3)
            {
                lgConnect.Visibility = Visibility.Collapsed;
            }
            else if (Type == 1)
            {
                liBank.Visibility = Visibility.Collapsed;
                liCompany.Visibility = Visibility.Visible;
                liActivationCode.Visibility = Visibility.Collapsed;
            }

            if (BankService == 1)
            {
                lgParm.Visibility = Visibility.Collapsed;
                cmbBank.SelectedIndex = cmbBank.SelectedIndex == (int)Bank.Nordea ? 0 : cmbBank.SelectedIndex;
                liActivationCode.Visibility = Type == 1 ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (BankService == 2)
            {
                lgParm.Visibility = Visibility.Collapsed;
                cmbBank.SelectedIndex = (int)Bank.Nordea;
                liBank.Visibility = Visibility.Collapsed;
                liActivationCode.Visibility = Visibility.Collapsed;
                lgConnect.Visibility = Type == 1 ? Visibility.Visible : Visibility.Collapsed;
                liCompany.Visibility = Type == 1 ? Visibility.Visible : Visibility.Collapsed;
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
