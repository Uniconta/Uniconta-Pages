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
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWPostPayment : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [InputFieldData]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public string Journal { get { return _Journal; } set { _Journal = value; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.BankStatement))]
        [InputFieldData]
        [Display(Name = "Bank", ResourceType = typeof(GLDailyJournalLineText))]
        public string Bank { get { return _Bank; } set { _Bank = value; } }

        [InputFieldData]
        [Display(Name = "PayDate", ResourceType = typeof(InputFieldDataText))]
        public DateTime PayDate { get { return _PayDate; } set { _PayDate = value; } }

        [InputFieldData]
        [Display(Name = "Simulation", ResourceType = typeof(InputFieldDataText))]
        public bool IsSimulation { get { return simulation; } set { simulation = value; } }
        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string comments { get { return _comment; } set { _comment = value; } }

        bool simulation;
        static string _Journal, _Bank, _comment;
        static DateTime _PayDate;
        protected override int DialogId { get { return 2000000104; } }
        protected override bool ShowTableValueButton { get { return true; } }

        public CWPostPayment(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("PostPayments");
            this.SizeToContent = SizeToContent.Height;
            lookupJournal.api = lookupBank.api = api;
            this.Loaded += CW_Loaded;
        }
        
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { lookupJournal.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Journal))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Journal"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

    }
}

