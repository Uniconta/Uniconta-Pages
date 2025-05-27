using Uniconta.DataModel;
using Uniconta.API.GeneralLedger;
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
using Uniconta.ClientTools.Controls;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.ClientTools.Page;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.API.System;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
   
    public partial class CWCopyVoucherToJrnl : ChildWindow
    {
        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        public static string Journal { get; set; }

        [InputFieldData]
        [Display(Name = "InvertSign", ResourceType = typeof(InputFieldDataText))]
        public static bool InvertSign { get; set; }

        [InputFieldData]
        [Display(Name = "Date", ResourceType = typeof(InputFieldDataText))]
        public static DateTime Date { get; set; }

        [InputFieldData]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLTransType))]
        [Display(Name = "TransType", ResourceType = typeof(InputFieldDataText))]
        public static string TransType { get; set; }

        [InputFieldData]
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public static string Comment { get; set; }

        [InputFieldData]
        [Display(Name = "CopyVATTrans", ResourceType = typeof(InputFieldDataText))]
        public static bool CopyVATTrans { get; set; }
        public Uniconta.DataModel.GLDailyJournal GlDailyJournal;

        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }

        SQLCache JournalCache;
        public CWCopyVoucherToJrnl(CrudAPI api)
        {
            InitializeComponent();
            this.DataContext = this;
            var Comp = api.CompanyEntity;
            JournalCache = Comp.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
            this.Title = Uniconta.ClientTools.Localization.lookup("CopyVoucherToJournal");
            leJournal.api = leTransType.api = api;
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { leJournal.Focus(); }));
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
            if (string.IsNullOrWhiteSpace(Journal))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Journal"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            GlDailyJournal =  (Uniconta.DataModel.GLDailyJournal)JournalCache?.Get(Journal) ;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
