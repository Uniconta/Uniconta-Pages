using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWJoinTwoGLAccounts : ChildWindow
    {

        SQLCache accountCache;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string ToAccount { get; set; }

        CrudAPI api;
        public CWJoinTwoGLAccounts(CrudAPI crudapi)
        {
            InitializeComponent();
            this.api = crudapi;
            cmbFromAccount.api = cmbToAccount.api = api;
            this.DataContext = this;
            LoadCacheInBackGround();

            this.Title = Uniconta.ClientTools.Localization.lookup("JoinTwoAccounts");
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;

        }

        protected async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            accountCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLAccount), api);

            cmbFromAccount.ItemsSource = cmbToAccount.ItemsSource = accountCache;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbFromAccount.Focus(); }));
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
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var fromAccount = cmbFromAccount.SelectedItem as GLAccount;
            var toAccount = cmbToAccount.SelectedItem as GLAccount;

            if (fromAccount == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("FromAccount"))), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
            if (toAccount == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("ToAccount"))), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }
            if ((fromAccount._AccountType >= (byte)GLAccountTypes.BalanceSheet && toAccount._AccountType < (byte)GLAccountTypes.BalanceSheet) ||
                (fromAccount._AccountType < (byte)GLAccountTypes.BalanceSheet && toAccount._AccountType >= (byte)GLAccountTypes.BalanceSheet))
            {
                var s1 = string.Format(Uniconta.ClientTools.Localization.lookup("AccountIsOfType"), fromAccount._Account, AppEnums.GLAccountTypes.ToString(fromAccount._AccountType));
                var s2 = string.Format(Uniconta.ClientTools.Localization.lookup("AccountIsOfType"), toAccount._Account, AppEnums.GLAccountTypes.ToString(toAccount._AccountType));
                var msg = string.Format("{0}\n{1}\n{2}", s1, s2, Uniconta.ClientTools.Localization.lookup("ProceedConfirmation"));

                if (UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Warning"),
#if !SILVERLIGHT
                    MessageBoxButton.YesNo) != MessageBoxResult.Yes)
#else
                    MessageBoxButton.OKCancel) != MessageBoxResult.OK)
#endif
                    return;
            }

            DeletePostedJournal delDialog = new DeletePostedJournal(true);
            delDialog.Closed += delegate
            {
                if (delDialog.DialogResult == true)
                {
                    CallJoinTwoAccount(fromAccount, toAccount);
                }

            };
            delDialog.Show();
        }

        async void CallJoinTwoAccount(GLAccount fromAccount, GLAccount toAccount)
        {
            MaintableAPI tableApi = new MaintableAPI(api);
            ErrorCodes res = await tableApi.JoinTwoAccounts(fromAccount, toAccount);
            if (res == ErrorCodes.Succes)
                this.DialogResult = true;
            UtilDisplay.ShowErrorCode(res);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
