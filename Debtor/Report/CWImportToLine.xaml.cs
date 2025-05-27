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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.Common;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Controls;
using System.ComponentModel.DataAnnotations;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWImportToLine.xaml
    /// </summary>
    public partial class CWImportToLine : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [InputFieldData]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string Journal { get { return _Journal; } set { _Journal = value; } }
        static string _Journal;

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime Date { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        [InputFieldData]
        [Display(Name = "Offsetaccount", ResourceType = typeof(InputFieldDataText))]
        public string BankAccount { get { return _BankAccount; } set { _BankAccount = value; } }
        static string _BankAccount;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLTransType))]
        [InputFieldData]
        [Display(Name = "Text", ResourceType = typeof(InputFieldDataText))]
        public string TransType { get { return _TransType; } set { _TransType = value; } }
        static string _TransType;

        public bool AggregateAmount { get; set; }
        [Display(Name = "Per", ResourceType = typeof(InputFieldDataText))]
        [InputFieldData]
        public bool PerAccount { get { return _PerAccount; } set { _PerAccount = value; } }
        static bool _PerAccount;

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }

        public CWImportToLine(CrudAPI api, DateTime dateTime)
        {
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateJournalLines");
            lblPer.Text = Uniconta.ClientTools.Localization.lookup("Per");
            cmbtypeValue.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Transaction"), Uniconta.ClientTools.Localization.lookup("Account") };
            Date = dateTime != DateTime.MinValue ? dateTime : Uniconta.ClientTools.Page.BasePage.GetSystemDefaultDate();
            this.DataContext = this;
            lookupJournal.api = lookupAccount.api = lookupTransType.api = api;
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            cmbtypeValue.SelectedIndex = PerAccount ? 1 : 0;
            Dispatcher.BeginInvoke(new Action(() => { lookupJournal.Focus(); }));
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
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (PerAccount)
                AggregateAmount = true;

            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void cmbtypeValue_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var index = cmbtypeValue.SelectedIndex;
            if (index == -1) return;
            PerAccount = index == 0 ? false : true;
        }
    }
}
