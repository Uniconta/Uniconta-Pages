using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using System.Windows;
using UnicontaClient.Utilities;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWAddBankImportMapping : ChildWindow
    {
        private CrudAPI api;
        Uniconta.DataModel.BankStatement master;
        public CWAddBankImportMapping(CrudAPI api, Uniconta.DataModel.BankStatement master, BankStatementLineClient bankStatement)
        {
            this.api = api;
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("AutomaticAccountSelection");
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#else
            if (string.IsNullOrWhiteSpace(cmdBankFormats.Text))
                FocusManager.SetFocusedElement(cmdBankFormats, cmdBankFormats);
#endif
            this.Loaded += CW_Loaded;

            if (master != null && master._BankImportId != 0) // last import
            {
                this.master = master;
                cmdBankFormats.Visibility = Visibility.Collapsed;
            }
            else
                SetBankFormats(true);

            txtAccountType.Text = bankStatement.AccountType;
            txtAccount.Text = bankStatement._Account;
            txtText.Text = bankStatement._Text;
        }

        async void SetBankFormats(bool initial)
        {
            var bankFormats = await api.Query<BankImportFormatClient>();
            cmdBankFormats.ItemsSource = bankFormats;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmdBankFormats.Focus(); }));
            txtAccount.KeyDown += txtAccount_KeyDown;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtText.Text))
                return;

            var bankImportMap = new BankImportMap();
            if (master != null)
                bankImportMap.SetMaster(master);
            else if (currentBankFormat != null)
                bankImportMap.SetMaster(currentBankFormat);
            else
                return;

            bankImportMap._AccountType = (GLJournalAccountType)AppEnums.GLAccountType.IndexOf(txtAccountType.Text);
            bankImportMap._Account = txtAccount.Text;
            bankImportMap._Text = txtText.Text;
            var err = await api.Insert(bankImportMap);
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            else
                this.DialogResult = true;
        }

        BankImportFormatClient currentBankFormat;
        private void cmdBankFormats_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmdBankFormats.SelectedItem == null) return;
            currentBankFormat = cmdBankFormats.SelectedItem as BankImportFormatClient;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void txtAccount_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(sender, e);
            else if (e.Key == Key.Escape)
                CancelButton_Click(sender, e);
        }
    }
}
