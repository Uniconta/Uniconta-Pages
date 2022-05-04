using UnicontaClient.Pages;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWCreditorJoinTwoAccounts.xaml
    /// </summary>
    public partial class CWCreditorJoinTwoAccounts : ChildWindow
    {

        SQLCache accountCache;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string ToAccount { get; set; }

        CrudAPI api;
        public CWCreditorJoinTwoAccounts(CrudAPI crudapi)
        {
            this.api = crudapi;
            InitializeComponent();
            cmbFromAccount.api = cmbToAccount.api = api;
            this.DataContext = this;
            LoadCacheInBackGround();

            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("JoinTwoOBJ"), Uniconta.ClientTools.Localization.lookup("Accounts"));
#if SILVERLIGHT
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }

        protected async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            accountCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
            if (accountCache == null)
                accountCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api);

            cmbFromAccount.ItemsSource = cmbToAccount.ItemsSource = accountCache;
        }


        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                if (string.IsNullOrWhiteSpace(cmbFromAccount.Text))
                    cmbFromAccount.Focus();
                else
                    OKButton.Focus();
            }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var fromAccount = cmbFromAccount.SelectedItem as DCAccount;
            var toAccount = cmbToAccount.SelectedItem as DCAccount;

            if (fromAccount == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("FromAccount"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            if (toAccount == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("ToAccount"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            DeletePostedJournal delDialog = new DeletePostedJournal(true);
            delDialog.Closed += async delegate
            {
                if (delDialog.DialogResult == true)
                {
                    CallJoinTwoAccount(fromAccount, toAccount);
                }

            };
            delDialog.Show();


        }


        async void CallJoinTwoAccount(DCAccount fromAccount, DCAccount toAccount)
        {
            MaintableAPI tableApi = new MaintableAPI(api);
            ErrorCodes res = await tableApi.JoinTwoAccounts(fromAccount, toAccount);
            if (res == ErrorCodes.Succes)
                SetDialogResult(true);
            UtilDisplay.ShowErrorCode(res);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
