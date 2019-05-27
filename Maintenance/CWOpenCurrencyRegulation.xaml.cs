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
   
    public partial class CWOpenCurrencyRegulation : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        public string Journal { get; set; }

        public DateTime Date { get; set; }

        public bool AddVoucherNumber { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLTransType))]
        public string TransType { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string DebtorAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string DebtorOffset { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string CreditorAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string CreditorOffset { get; set; }

        CrudAPI api;
        public CWOpenCurrencyRegulation(CrudAPI crudapi)
        {
            this.DataContext = this;
            InitializeComponent();
            this.api = crudapi;
            leJournal.api= leTransType.api=leDebtorAccount.api= leDebtorAccount.api= 
                leCreditorAccount.api=leCreditorOffset.api = api;

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

        protected async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            var glAccCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLAccount), api);
            var glDlJournalCache = Comp.GetCache(typeof(Uniconta.DataModel.GLDailyJournal)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal), api);
            var glTransTypeCache = Comp.GetCache(typeof(Uniconta.DataModel.GLTransType)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLTransType), api);

            leJournal.ItemsSource = glDlJournalCache;
            leTransType.ItemsSource = glTransTypeCache;
            leDebtorAccount.ItemsSource = leDebtorOffset.ItemsSource= leCreditorAccount.ItemsSource= leCreditorOffset.ItemsSource = glAccCache;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Journal = leJournal.Text;
            Date = date.DateTime;
            AddVoucherNumber = (bool)chkAddVouNo.IsChecked;
            TransType = leTransType.Text;
            DebtorAccount = leDebtorAccount.Text;
            DebtorOffset = leDebtorOffset.Text;
            CreditorAccount = leCreditorAccount.Text;
            CreditorOffset = leCreditorOffset.Text;
            this.DialogResult = true;
        }
    }
}
