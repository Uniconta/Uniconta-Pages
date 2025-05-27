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
    /// Interaction logic for CWDebtorJoinTwoAccounts.xaml
    /// </summary>
    public partial class CWDebtorJoinTwoAccounts : ChildWindow
    {
        SQLCache accountCache;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string ToAccount { get; set; }

        CrudAPI api;

        public CWDebtorJoinTwoAccounts(CrudAPI crudapi)
        {
            this.api = crudapi;
            InitializeComponent();
            cmbFromAccount.api = cmbToAccount.api = api;
            this.DataContext = this;
            LoadCacheInBackGround();

            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("JoinTwoOBJ"), Uniconta.ClientTools.Localization.lookup("Accounts"));
            this.Loaded += CW_Loaded;
        }

        protected async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            accountCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor));
            if (accountCache == null)
                accountCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api);

            cmbFromAccount.ItemsSource = cmbToAccount.ItemsSource =accountCache;
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
        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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
