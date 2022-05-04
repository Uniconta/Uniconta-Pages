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
    public partial class CWAppendEnumeratedLabel : ChildWindow
    {
        private CrudAPI api;
        SQLCache LedgerCache, DebtorCache, CreditorCache;
        public CWAppendEnumeratedLabel(CrudAPI crudApi)
        {
            api = crudApi;
            this.DataContext = this;
            InitializeComponent();
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("TransType");
#else
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            cmbAccType.ItemsSource = AppEnums.GLAccountType.Values;
            cmbOffSetAccType.ItemsSource = AppEnums.GLAccountType.Values;
            var Comp = api.CompanyEntity;
            LedgerCache = Comp.GetCache(typeof(GLAccount));
            DebtorCache = Comp.GetCache(typeof(Debtor));
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));

            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { txtTransType.Focus(); }));
            txtTransType.KeyDown+=txtTransType_KeyDown;
        }

        private void cmbAccType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbAccType.SelectedIndex == -1) return;
            SQLCache cache;
            switch (cmbAccType.SelectedIndex)
            {
                case (byte)GLJournalAccountType.Finans:
                    cache = LedgerCache;
                    break;
                case (byte)GLJournalAccountType.Debtor:
                    cache = DebtorCache;
                    break;
                case (byte)GLJournalAccountType.Creditor:
                    cache = CreditorCache;
                    break;
                default: return;
            }

            if (cache != null)
            {
                leAccount.ItemsSource = cache;
            }
        }

        private void cmbOffSetAccType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbOffSetAccType.SelectedIndex == -1) return;
            SQLCache cache;
            switch (cmbOffSetAccType.SelectedIndex)
            {
                case (byte)GLJournalAccountType.Finans:
                    cache = LedgerCache;
                    break;
                case (byte)GLJournalAccountType.Debtor:
                    cache = DebtorCache;
                    break;
                case (byte)GLJournalAccountType.Creditor:
                    cache = CreditorCache;
                    break;
                default: return;
            }

            if (cache != null)
            {
                leOffsetAccount.ItemsSource = cache;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            busyIndicator.IsBusy = true;
            GLTransTypeClient enumText = new GLTransTypeClient();
            enumText.TransType = txtTransType.Text;
            enumText.Code = txtCode.Text;
            enumText.AccountType = cmbAccType.SelectedText;
            enumText.Account = leAccount.SelectedText;
            enumText.OffsetAccountType = cmbOffSetAccType.SelectedText;
            enumText.OffsetAccount = leOffsetAccount.SelectedText;
            var err = await api.Insert(enumText);
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            busyIndicator.IsBusy = false;
            SetDialogResult(true);
        }
        
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void txtTransType_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(sender, e);
            else if (e.Key == Key.Escape)
                CancelButton_Click(sender, e);
        }
    }
}

