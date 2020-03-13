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
using Uniconta.API.GeneralLedger;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AccountPostingBalanceClientText
    {

        public static string Account { get { return Uniconta.ClientTools.Localization.lookup("Account"); } }
        public static string AccountName { get { return Uniconta.ClientTools.Localization.lookup("AccountName"); } }
        public static string BalanceBefore { get { return Uniconta.ClientTools.Localization.lookup("BalanceBefore"); } }
        public static string PostingAmount { get { return Uniconta.ClientTools.Localization.lookup("LineTotal"); } }
        public static string BalanceNow { get { return Uniconta.ClientTools.Localization.lookup("BalanceNow"); } }
    }

    public class AccountPostingBalanceClient
    {
        private AccountPostingBalance accPostingBalance;
        private GLAccount glAccount;
        public AccountPostingBalanceClient(AccountPostingBalance accPostingbal, GLAccount account)
        {
            accPostingBalance = accPostingbal;
            glAccount = account;
        }

        [Display(Name = "Account", ResourceType = typeof(AccountPostingBalanceClientText))]
        public string Account { get { return glAccount?.AccountNumber ?? null; } }

        [Display(Name = "AccountName", ResourceType = typeof(AccountPostingBalanceClientText))]
        public string AccountName { get { return glAccount?._Name ?? null; } }

        [Display(Name = "BalanceBefore", ResourceType = typeof(AccountPostingBalanceClientText))]
        public double BalanceBefore { get { return accPostingBalance?.BalanceBefore ?? 0; } }

        [Display(Name = "PostingAmount", ResourceType = typeof(AccountPostingBalanceClientText))]
        public double PostingAmount { get { return accPostingBalance?.PostingAmount ?? 0; } }

        [Display(Name = "BalanceNow", ResourceType = typeof(AccountPostingBalanceClientText))]
        public double BalanceNow { get { return BalanceBefore + PostingAmount; } }
    }

    public class AccountPostingBalanceGridClient : CorasauDataGrid
    {
        public override Type TableType { get { return typeof(AccountPostingBalanceClient); } }
        public override bool Readonly { get { return true; } }
    }

    /// <summary>
    /// Interaction logic for AccountPostingBalancePage.xaml
    /// </summary>
    public partial class AccountPostingBalancePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.AccountPostingBalancePage; ; } }
        public AccountPostingBalancePage(CrudAPI api, AccountPostingBalance[] accountPostingBalance) : base(api, string.Empty)
        {
            InitializeComponent();
            dgAccountPostingGrid.ItemsSource = GetSource(accountPostingBalance);
            dgAccountPostingGrid.Visibility = Visibility.Visible;
        }

        private AccountPostingBalanceClient[] GetSource(AccountPostingBalance[] accountPostingBalance)
        {
            int count = accountPostingBalance.Length;
            AccountPostingBalanceClient[] accountPostingBalClientCollection = new AccountPostingBalanceClient[count];
            var glAccounCache = api.GetCache(typeof(GLAccount)) ?? api.LoadCache(typeof(GLAccount)).GetAwaiter().GetResult();
            for (int iCtr = 0; iCtr < count; iCtr++)
            {
                var accPostingBal = accountPostingBalance[iCtr];
                var glAccount = glAccounCache.Get(accPostingBal.AccountRowId) as GLAccount;
                accountPostingBalClientCollection[iCtr] = new AccountPostingBalanceClient(accPostingBal, glAccount);
            }

            return accountPostingBalClientCollection;
        }
    }
}
