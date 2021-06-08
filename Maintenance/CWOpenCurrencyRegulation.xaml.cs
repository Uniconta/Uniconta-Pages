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
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Controls;
using System.ComponentModel.DataAnnotations;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class CWOpenCurrencyRegulation : ChildWindow
    {
        public Uniconta.DataModel.GLDailyJournal Jour;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [InputFieldData]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string Journal { get; set; }

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime Date { get; set; }

        [InputFieldData]
        [Display(Name = "AssignVoucherNumber", ResourceType = typeof(InputFieldDataText))]
        public bool AddVoucherNumber { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLTransType))]
        [InputFieldData]
        [Display(Name = "TransType", ResourceType = typeof(InputFieldDataText))]
        public string TransType { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        [InputFieldData]
        [Display(Name = "Debtor", ResourceType = typeof(InputFieldDataText))]
        public string DebtorAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        [InputFieldData]
        [Display(Name = "DebtorOffset", ResourceType = typeof(InputFieldDataText))]
        public string DebtorOffset { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        [InputFieldData]
        [Display(Name = "Creditor", ResourceType = typeof(InputFieldDataText))]
        public string CreditorAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        [InputFieldData]
        [Display(Name = "CreditorOffset", ResourceType = typeof(InputFieldDataText))]
        public string CreditorOffset { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        [InputFieldData]
        public string LedgerOffset { get; set; }

        CrudAPI api;

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWOpenCurrencyRegulation(CrudAPI crudapi)
        {
            this.DataContext = this;
            InitializeComponent();
            this.api = crudapi;
            leJournal.api = leTransType.api = leDebtorAccount.api = leDebtorAccount.api =
                leCreditorAccount.api = leCreditorOffset.api = api;

            this.Title = Uniconta.ClientTools.Localization.lookup("CurrencyAdjustment");
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            date.DateTime = Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            this.Loaded += CW_Loaded;
            LoadCacheInBackGround();
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { leJournal.Focus(); }));
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

        SQLCache glDlJournalCache;
        protected async void LoadCacheInBackGround()
        {
            var api = this.api;
            var glAccCache = api.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount));
            glDlJournalCache = api.GetCache(typeof(Uniconta.DataModel.GLDailyJournal)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal));
            var glTransTypeCache = api.GetCache(typeof(Uniconta.DataModel.GLTransType)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLTransType));

            leJournal.ItemsSource = glDlJournalCache;
            leTransType.ItemsSource = glTransTypeCache;
            leDebtorAccount.ItemsSource = leDebtorOffset.ItemsSource= leCreditorAccount.ItemsSource= leCreditorOffset.ItemsSource = leLedgerOffset.ItemsSource = glAccCache;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Journal = leJournal.Text;
            Jour = (Uniconta.DataModel.GLDailyJournal)glDlJournalCache.Get(Journal);
            Date = date.DateTime;
            AddVoucherNumber = (bool)chkAddVouNo.IsChecked;
            TransType = leTransType.Text;
            DebtorAccount = leDebtorAccount.Text;
            DebtorOffset = leDebtorOffset.Text;
            CreditorAccount = leCreditorAccount.Text;
            CreditorOffset = leCreditorOffset.Text;
            LedgerOffset = leLedgerOffset.Text;
            this.DialogResult = true;
        }
    }
}
