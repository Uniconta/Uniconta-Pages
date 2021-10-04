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
        public string Journal { get; set; }
        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public DateTime Date { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLAccount))]
        [InputFieldData]
        [Display(Name = "Offsetaccount", ResourceType = typeof(InputFieldDataText))]
        public string BankAccount { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLTransType))]
        [InputFieldData]
        [Display(Name = "Text", ResourceType = typeof(InputFieldDataText))]
        public string TransType { get; set; }

        public bool AggregateAmount { get; set; }
        [Display(Name = "Per", ResourceType = typeof(InputFieldDataText))]
        [InputFieldData]
        public bool PerAccount { get; set; }

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif

        public CWImportToLine(CrudAPI api,DateTime dateTime)
        {
            InitializeComponent();
#if !SILVERLIGHT
            this.Title = Uniconta.ClientTools.Localization.lookup("GenerateJournalLines");
#else
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            lblPer.Text = Uniconta.ClientTools.Localization.lookup("Per");
            cmbtypeValue.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Transaction"), Uniconta.ClientTools.Localization.lookup("Account") };
            Date = dateTime;
            this.DataContext = this;
            lookupJournal.api = lookupAccount.api = lookupTransType.api = api;
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            cmbtypeValue.SelectedIndex = PerAccount ? 1 : 0;
            Dispatcher.BeginInvoke(new Action(() => { lookupJournal.Focus(); }));
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
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (PerAccount)
                AggregateAmount = true;

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void cmbtypeValue_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var index = cmbtypeValue.SelectedIndex;
            if (index == -1) return;
            PerAccount = index == 0 ? false : true;
        }
    }
}
