using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
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
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Creditor.Payments;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{    
    public partial class CWReadReceipt : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        public string Journal { get; set; }
        CrudAPI Capi;
           
        public CWReadReceipt(CrudAPI api)
        {
            Capi = api;
            LoadCache(api);
            this.DataContext = this;
            InitializeComponent();
            lookupJournal.api = api;
            SetImportOption();
            this.Title = Uniconta.ClientTools.Localization.lookup("ReadReceipt");
            this.Loaded += CW_Loaded;
        }
        SQLCache JournalCache = null;
        private async Task LoadCache(CrudAPI api)
        {
            var Comp = api.CompanyEntity;
            JournalCache = Comp.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
            if (JournalCache == null)
                JournalCache = await Comp.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal), api).ConfigureAwait(false);
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cbImportOption.Focus(); }));
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
        UserPluginClient[] plugins;
        void SetImportOption()
        {
            List<string> formatOption = new List<string>
            {
                "NETS Norge"
            };
            cbImportOption.ItemsSource = formatOption;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var res = GenerateJournalLines();
            if (res)
                SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        bool GenerateJournalLines()
        {
            string fileOption = Convert.ToString(cbImportOption.SelectedItem);
            Uniconta.DataModel.GLDailyJournal journal = null;
            var journalText = Convert.ToString(lookupJournal.EditValue);
            if (!string.IsNullOrEmpty(journalText))
                journal = JournalCache.Get(journalText) as Uniconta.DataModel.GLDailyJournal;
            string filePath = browseFile.FilePath;
            if (string.IsNullOrEmpty(fileOption) || journal == null || string.IsNullOrEmpty(filePath))
                return false;

            switch (fileOption)
            {
                case "NETS Norge":
                    NETSNorge.GenerateJournalLines(Capi, journal, filePath);
                    break;
            }
            return true;
        }
    }
}

