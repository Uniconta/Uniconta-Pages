using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
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
using System.Windows;
using Uniconta.DataModel;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ImportToLine : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [InputFieldData]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string Journal { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        [InputFieldData]
        [Display(Name = "Bank", ResourceType = typeof(InputFieldDataText))]
        public string BankAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLTransType))]
        [InputFieldData]
        [Display(Name = "Text", ResourceType = typeof(InputFieldDataText))]
        public string TransType { get; set; }

        [InputFieldData]
        [Display(Name = "AssignVoucherNumber", ResourceType = typeof(InputFieldDataText))]
        public bool AddVoucherNumber { get; set; }

        CrudAPI Capi;

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif

        public ImportToLine(CrudAPI api)
        {
            Capi = api;
            this.DataContext = this;
            InitializeComponent();
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateJournalLines");
#else
            Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            
            lookupJournal.api =lookupAccount.api= lookupTransType.api = api;
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
            SetDefaultValues();
        }
        async void SetDefaultValues()
        {
            var cache = Capi.CompanyEntity.GetCache(typeof(CreditorPaymentFormat));
            if (cache == null)
                cache = await Capi.CompanyEntity.LoadCache(typeof(CreditorPaymentFormat), Capi);
            foreach (var r in cache.GetRecords)
            {
                var rec = r as CreditorPaymentFormat;
                if (rec != null && rec._Default)
                {
                    lookupAccount.EditValue = rec._BankAccount;
                    lookupJournal.EditValue = rec._Journal;

                    BankAccount = rec._BankAccount;
                    Journal = rec._Journal;
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
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}

