using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWTransferBankStatement : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [InputFieldData]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string Journal { get; set; }
        [InputFieldData]
        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime FromDate { get; set; }
        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime ToDate { get; set; }
        [InputFieldData]
        [Display(Name = "BankAccountPos", ResourceType = typeof(InputFieldDataText))]
        public bool BankAsOffset { get; set; }
        [InputFieldData]
        [Display(Name = "OnlyLineWithAccountNumber", ResourceType = typeof(InputFieldDataText))]
        public bool isMarkLine { get; set; }
        [InputFieldData]
        [Display(Name = "AssignVoucherNumber", ResourceType = typeof(InputFieldDataText))]
        public bool AddVoucherNumber { get; set; }

        static int SelectedBankAccPosIndex = 0;
        static string lclJournal;
        static bool? lclIsMarkLine, lclAddVouNo;
        CrudAPI Capi;
#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWTransferBankStatement(DateTime fromdate, DateTime todate, CrudAPI api, Uniconta.DataModel.BankStatement master, bool IsLedgerPosting = false)
        {
            FromDate = fromdate;
            ToDate = todate;
            Journal = master._Journal;
            BankAsOffset = master._BankAsOffset;
            this.DataContext = this;
            InitializeComponent();
            Capi = api;
            lookupJournal.api = api;
            cbBankAccountPos.ItemsSource = AppEnums.AccountSide.Values;
            string dateinterval = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Date"), Uniconta.ClientTools.Localization.lookup("Interval"));
            cbTransfer.ItemsSource = new string[] { dateinterval, Uniconta.ClientTools.Localization.lookup("SelectedRows") };
            cbBankAccountPos.SelectedIndex = SelectedBankAccPosIndex;
            cbTransfer.SelectedIndex = 0;
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateJournalLines");
#endif
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;

            if (!IsLedgerPosting)
            {
                rowCbTransfer.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            if (BankAsOffset)
                cbBankAccountPos.SelectedIndex = 1;
            else
                cbBankAccountPos.SelectedIndex = SelectedBankAccPosIndex;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                lookupJournal.Focus();
                chkLine.IsChecked = lclIsMarkLine == null ? isMarkLine : lclIsMarkLine;
                cbkAssignVouNo.IsChecked = lclAddVouNo == null ? AddVoucherNumber : lclAddVouNo;
                if (!string.IsNullOrEmpty(lclJournal))
                    SetDefaultJournal();
            }));
        }

        async void SetDefaultJournal()
        {
            var cache = Capi.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
            if (cache == null)
                cache = await Capi.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal), Capi);
            foreach (var r in cache.GetRecords)
            {
                var rec = r as Uniconta.DataModel.GLDailyJournal;
                if (rec._Journal == lclJournal)
                {
                    lookupJournal.SelectedItem = rec;
                    break;
                }
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
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            lclIsMarkLine = (bool)chkLine.IsChecked;
            lclAddVouNo = (bool)cbkAssignVouNo.IsChecked;
            SelectedBankAccPosIndex = cbBankAccountPos.SelectedIndex;
            lclJournal = Journal;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void cbBankAccountPos_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            BankAsOffset = cbBankAccountPos.SelectedIndex == 0 ? false : true;
        }

        private void cbTransfer_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            dpFromDate.IsEnabled = dptoDate.IsEnabled = (cbTransfer.SelectedIndex != 1);
        }
    }
}

