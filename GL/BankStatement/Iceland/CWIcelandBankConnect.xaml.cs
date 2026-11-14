using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    // Connects a bank statement to Íslandsbanki, Arion or Landsbankinn: the server tests the login with the bank, saves it on
    // the statement's import format, and fetches the statement every hour (Corasau.GeneralLedger/Iceland/IcelandBankConnect.cs).
    // After a connect the dialog shows the account as the bank reports it, with warnings when the owner is not the company or
    // the currency is not the statement's; the server refuses an account another bank statement already has.
    public partial class CWIcelandBankConnect : ChildWindow
    {
        static readonly BankImportFormatType[] Banks = { BankImportFormatType.ISLANDSBANKI, BankImportFormatType.ARION, BankImportFormatType.LANDSBANKINN };

        readonly CrudAPI api;
        readonly BankStatementAPI bankApi;
        readonly BankStatementClient bankStatement;
        IcelandBankConnection state;

        public CWIcelandBankConnect(CrudAPI api, BankStatementClient bankStatement)
        {
            InitializeComponent();
            this.api = api;
            this.bankApi = new BankStatementAPI(api);
            this.bankStatement = bankStatement;
            this.DataContext = this;
            this.Title = string.Concat(Uniconta.ClientTools.Localization.lookup("ConnectToBank"), " - ", bankStatement.Account, " ", bankStatement.Name);

            cmbBank.ItemsSource = new[] { "Íslandsbanki", "Arion banki", "Landsbankinn" };
            cmbBank.SelectedIndex = 0;
            var today = BasePage.GetSystemDefaultDate().Date;
            dteFrom.DateTime = today.AddDays(-7);
            dteTo.DateTime = today;
            this.Loaded += async delegate { Show(await bankApi.IcelandBankState(bankStatement)); };
        }

        void Show(IcelandBankConnection result)
        {
            if (result == null)
            {
                txtStatus.Text = Uniconta.ClientTools.Localization.lookup(bankApi.LastError.ToString());
                return;
            }
            state = result;
            var i = Array.IndexOf(Banks, result.Format);
            if (i >= 0)
                cmbBank.SelectedIndex = i;
            if (result.LoginId != null)
                txtLoginId.Text = result.LoginId;
            if (result.BankAccountNo != null)
                txtBankAccountNo.Text = result.BankAccountNo;

            string status;
            if (!result.Connected)
                status = Uniconta.ClientTools.Localization.lookup("NoBankConnection");
            else if (result.Status == 2)
                status = string.Concat(Uniconta.ClientTools.Localization.lookup("ConnectedTo"), " ", cmbBank.Text, ". ", Uniconta.ClientTools.Localization.lookup("UserDoesNotExist"));
            else
                status = string.Concat(Uniconta.ClientTools.Localization.lookup("ConnectedTo"), " ", cmbBank.Text);
            if (result.LastFetch != DateTime.MinValue)
                status = string.Concat(status, "\n", Uniconta.ClientTools.Localization.lookup("Updatedon"), ": ", result.LastFetch.ToLocalTime().ToString("g"));
            if (!api.CompanyEntity._DirectDebit) // the server refuses connect and fetch; disconnect still works
                status = string.Concat(status, "\n", Uniconta.ClientTools.Localization.lookup("AutoBankingNotActivated"));
            txtStatus.Text = status;
            FetchButton.IsEnabled = DisconnectButton.IsEnabled = lgFetch.IsEnabled = result.Connected;
        }

        // The account as the bank reported it at connect, and how it fits the company and this bank statement
        void ShowAccount(IcelandBankConnection result)
        {
            if (result == null || result.Err != ErrorCodes.Succes)
                return;
            txtAccountName.Text = result.AccountName;
            txtIban.Text = result.Iban;
            txtAccountCurrency.Text = result.AccountCurrency;
            txtOwner.Text = Kennitala(result.OwnerId);

            var lines = new List<string>();
            if (result.AccountNote != null)
                lines.Add(string.Format(Uniconta.ClientTools.Localization.lookup("IcelandBankAccountUnread"), result.AccountNote));
            if (result.OwnerDiffers)
                lines.Add(string.Format(Uniconta.ClientTools.Localization.lookup("IcelandBankOwnerDiffers"),
                    Kennitala(result.OwnerId), Kennitala(api.CompanyEntity._Id)));
            if (result.CurrencyDiffers)
                lines.Add(string.Format(Uniconta.ClientTools.Localization.lookup("IcelandBankCurrencyDiffers"),
                    result.AccountCurrency, bankStatement._Currency != 0 ? bankStatement.Currency : api.CompanyEntity._CurrencyId.ToString()));
            if (result.OwnAccount == 1)
                lines.Add(Uniconta.ClientTools.Localization.lookup("IcelandBankAccountMatches"));
            else if (result.OwnAccount == 2)
                lines.Add(string.Format(Uniconta.ClientTools.Localization.lookup("IcelandBankAccountDiffers"), StatementAccountNo()));
            txtAccountCheck.Text = string.Join("\n", lines);
            liAccountCheck.Visibility = lines.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            lgAccount.Visibility = Visibility.Visible;
        }

        string StatementAccountNo()
        {
            var parts = string.Concat(bankStatement.BankAccountPart1, " ", bankStatement.BankAccountPart2).Trim();
            return parts.Length > 0 ? parts : bankStatement.IBAN;
        }

        // 1234567890 as 123456-7890
        static string Kennitala(string id)
        {
            if (id == null)
                return null;
            id = id.Trim();
            if (id.Length == 10)
            {
                foreach (var c in id)
                    if (c < '0' || c > '9')
                        return id;
                return string.Concat(id.Substring(0, 6), "-", id.Substring(6));
            }
            return id;
        }

        // The other bank statement that already has the account, by its account number and name
        string OtherStatement(IcelandBankConnection result)
        {
            var other = api.GetCache(typeof(Uniconta.DataModel.BankStatement))?.Get(result.OtherStatement) as Uniconta.DataModel.BankStatement;
            var name = other != null ? string.Concat(other._Account, " ", other._Name).Trim() : result.OtherStatementName;
            return string.Format(Uniconta.ClientTools.Localization.lookup("IcelandAccountOnOtherStatement"), name);
        }

        // The bank's text when it refused, otherwise the code
        void ShowResult(IcelandBankConnection result, string done)
        {
            Show(result);
            string text;
            if (result == null)
                text = Uniconta.ClientTools.Localization.lookup(bankApi.LastError.ToString());
            else if (result.Err == ErrorCodes.RecordExists && result.OtherStatement != 0)
                text = OtherStatement(result);
            else if (result.Err != ErrorCodes.Succes)
                text = result.Message ?? Uniconta.ClientTools.Localization.lookup(result.Err.ToString());
            else
                text = done;
            if (text != null)
                UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"));
        }

        async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            var login = txtLoginId.Text?.Trim();
            var accountNo = txtBankAccountNo.Text?.Trim();
            var password = txtPassword.Text;
            if (string.IsNullOrEmpty(password) && state != null && state.LoginId == login)
                password = null; // keep the saved one
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(accountNo) || password == string.Empty)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("FieldCannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            var format = Banks[Math.Max(cmbBank.SelectedIndex, 0)];
            busyIndicator(true);
            var result = await bankApi.IcelandBankConnect(bankStatement, format, login, password, accountNo);
            busyIndicator(false);
            txtPassword.Text = null;
            ShowAccount(result);
            string done = null;
            if (result != null && result.Err == ErrorCodes.Succes)
            {
                done = string.Concat(Uniconta.ClientTools.Localization.lookup("ConnectedTo"), " ", result.Message);
                if (result.AccountName != null)
                    done = string.Concat(done, " (", result.AccountName, ")");
                if (result.OwnerDiffers || result.CurrencyDiffers)
                    done = string.Concat(done, "\n\n", txtAccountCheck.Text);
            }
            ShowResult(result, done);
        }

        async void FetchButton_Click(object sender, RoutedEventArgs e)
        {
            busyIndicator(true);
            var result = await bankApi.IcelandBankFetch(bankStatement, dteFrom.DateTime.Date, dteTo.DateTime.Date);
            busyIndicator(false);
            ShowResult(result, result != null ? string.Format("{0}: {1}, {2}: {3}", Uniconta.ClientTools.Localization.lookup("Transactions"), result.Lines,
                Uniconta.ClientTools.Localization.lookup("Imported"), result.Inserted) : null);
        }

        async void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UnregisterBank") + "?", Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            busyIndicator(true);
            var result = await bankApi.IcelandBankDisconnect(bankStatement);
            busyIndicator(false);
            ShowResult(result, null);
        }

        void busyIndicator(bool busy)
        {
            ConnectButton.IsEnabled = !busy;
            FetchButton.IsEnabled = DisconnectButton.IsEnabled = !busy && state != null && state.Connected;
            this.Cursor = busy ? System.Windows.Input.Cursors.Wait : null;
        }

        void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                SetDialogResult(false);
        }
    }
}
