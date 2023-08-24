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
        public static int BankService { get; set; }
        public string BankServiceName
        {
            get
            {
                if (BankService == 1) return BANKCONNECT;
                else if (BankService == 2) return NORDEA;
                else return AIIA;
            }
        }

        #region Variables
        private string[] BankServiceLst;
        #endregion

        #region Constants
        private const string BANKCONNECT = "Bank Connect";
        private const string NORDEA = "Nordea";
        private const string AIIA = "Aiia";
        #endregion


        CrudAPI api;

        public CWBankAPI(CrudAPI api, BankStatementClient bankStatement)
        {
            InitializeComponent();
            this.api = api;
            ServiceId = bankStatement.ServiceId;

            leBankAccount.api = api;
            BankAccount = bankStatement._Account;
            cmbBank.ItemsSource = AppEnums.Bank.Values;

            BankServiceLst = new string[] { AIIA, BANKCONNECT, NORDEA };
            cmbBankService.ItemsSource = BankServiceLst;

            cmbBankService.SelectedIndex = BankService;
            cmbBankAPIFunction.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Register"), Uniconta.ClientTools.Localization.lookup("Connect"), Uniconta.ClientTools.Localization.lookup("Unregister"),
                                                            Uniconta.ClientTools.Localization.lookup("Sync"), string.Concat(Uniconta.ClientTools.Localization.lookup("Download"), " ", Uniconta.ClientTools.Localization.lookup("Transactions").ToLower()),
                                                            Uniconta.ClientTools.Localization.lookup("WebServiceInfo"), Uniconta.ClientTools.Localization.lookup("Settings") };
            //TODO:Udvid label vedr. SYNC
            cmbBankAPIFunction.SelectedIndex = Type;
            cmbBank.SelectedIndex = 0;

            FromDate = FromDate != DateTime.MinValue ? FromDate : BasePage.GetSystemDefaultDate();
            ToDate = ToDate != DateTime.MinValue ? ToDate : BasePage.GetSystemDefaultDate();

            this.DataContext = this;

            this.Title = Uniconta.ClientTools.Localization.lookup("FinancialInstitution");
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
                    else if (BankService == 2)
                        txtDescription.Text = String.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("RegisterCompanyTo"), NORDEA), Environment.NewLine, NORDEA, ": ", Uniconta.ClientTools.Localization.lookup("ServiceId"), " = 10 ", Uniconta.ClientTools.Localization.lookup("Digits").ToLower());
                    else
                    {
                        txtDescription.Text = StringBuilderReuse.Create().Append(string.Format(Uniconta.ClientTools.Localization.lookup("RegisterCompanyTo"), AIIA)).AppendLine().AppendLine()
                            .Append(Uniconta.ClientTools.Localization.lookup("AiiaDialogRegister1")).AppendLine(":")
                            .Append("- ").AppendLine(Uniconta.ClientTools.Localization.lookup("AiiaDialogRegister2"))
                            .Append("- ").AppendLine(Uniconta.ClientTools.Localization.lookup("AiiaDialogRegister3")).ToStringAndRelease();
                    }
                    break;
                case 1: //Connect
                    txtDescription.Text = BankService == 0 ? Uniconta.ClientTools.Localization.lookup("FunctionNotSupported") : Uniconta.ClientTools.Localization.lookup("AddCompanyToConnection");
                    break;
                case 2: //Unregister
                    txtDescription.Text = string.Concat(Uniconta.ClientTools.Localization.lookup("Unregister"), " ", Uniconta.ClientTools.Localization.lookup("Connection").ToLower());
                    break;
                case 3: //Sync
                    txtDescription.Text = BankService == 0 ? Uniconta.ClientTools.Localization.lookup("SynchronizeAiiaWithBank") : Uniconta.ClientTools.Localization.lookup("FunctionNotSupported");
                    break;
                case 4: //OnDemand
                    txtDescription.Text = BankService == 0 ? Uniconta.ClientTools.Localization.lookup("RetrieveBankTransPeriod") : Uniconta.ClientTools.Localization.lookup("FunctionNotSupported");
                    break;
                case 5: //Service Info
                    txtDescription.Text = string.Format(Uniconta.ClientTools.Localization.lookup("InfoAgreement"), BankServiceLst[BankService]);
                    break;
                case 6: //Settings
                    txtDescription.Text = BankService == 0 ? string.Concat("Aiia Hub", Environment.NewLine, Uniconta.ClientTools.Localization.lookup("AiiaConsentOverview")) : Uniconta.ClientTools.Localization.lookup("FunctionNotSupported");
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
                if (BankService != 0)
                {
                    if (Bank == Bank.None)
                        errText = fieldCannotBeEmpty("FinancialInstitution");
                    else if (string.IsNullOrEmpty(ServiceId))
                        errText = fieldCannotBeEmpty("ServiceId");
                    else if (Bank != Bank.Nordea && string.IsNullOrEmpty(ActivationCode))
                        errText = fieldCannotBeEmpty("ActivationCode");
                }
            }
            else if (Type == 4)
            {
                if (ToDate > DateTime.Now.Date)
                    errText = string.Format(Uniconta.ClientTools.Localization.lookup("ValueMayNoBeGreater"), Uniconta.ClientTools.Localization.lookup("ToDate"), Uniconta.ClientTools.Localization.lookup("TodaysDate").ToLower());
                if (FromDate < DateTime.Now.Date.AddYears(-2))
                    errText = string.Format(Uniconta.ClientTools.Localization.lookup("PleaseNotOBJ"), string.Concat(Uniconta.ClientTools.Localization.lookup("ToDate"), " >= ", DateTime.Now.Date.AddYears(-2).ToString("dd.MM.yyyy")));
            }
            else
            {
                if (Type != 6 && Type != 3 && BankService != 0 && string.IsNullOrEmpty(ServiceId))
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
            SetDescription();
            lgParm.Visibility = Visibility.Visible;
            lgCustomer.Visibility = Visibility.Visible;
            lgConnect.Visibility = Visibility.Visible;

            //Tilmeld=0, Tilknyt=1, Afmeld=2, Sync=3, Transactions=4, Info=5, Settings=6
            if (Type == 0)
            {
                liBank.Visibility = Visibility.Visible;
                liActivationCode.Visibility = Visibility.Visible;
                liCompany.Visibility = Visibility.Collapsed;
            }
            else if (Type == 2 || Type == 5)
            {
                lgConnect.Visibility = Visibility.Collapsed;
            }
            else if (Type == 1)
            {
                liBank.Visibility = Visibility.Collapsed;
                liCompany.Visibility = Visibility.Visible;
                liActivationCode.Visibility = Visibility.Collapsed;
            }
            else if (Type == 4)
            {
                lgCustomer.Visibility = Visibility.Collapsed;
                liBank.Visibility = Visibility.Collapsed;
                liCompany.Visibility = Visibility.Collapsed;
                lgConnect.Visibility = Visibility.Collapsed;
            }
            else if (Type == 6 || Type == 3)
            {
                lgConnect.Visibility = Visibility.Collapsed;
                lgCustomer.Visibility = Visibility.Collapsed;
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
            else if (BankService == 0)
            {
                liBank.Visibility = Visibility.Collapsed;
                liActivationCode.Visibility = Visibility.Collapsed;
                lgParm.Visibility = Type == 4 ? Visibility.Visible : Visibility.Collapsed;
                lgCustomer.Visibility = Visibility.Collapsed;
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
