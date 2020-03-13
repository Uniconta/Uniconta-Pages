using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWImportToJournal.xaml
    /// </summary>
    public partial class CWImportToJournal : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        [InputFieldData]
        [Display(Name = "Journal", ResourceType = typeof(InputFieldDataText))]
        static public string Journal { get; set; }

        CrudAPI api;
        public CWImportToJournal(CrudAPI crudApi, string title = null)
        {
            DataContext = this;
            InitializeComponent();
            Title = title ?? Uniconta.ClientTools.Localization.lookup("Import");
            api = crudApi;
            lookupJournal.api = crudApi;
            browseFile.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.CSV);
            browseFile.NotLoadFileInfo = true;
            InitCaches(crudApi);
        }

        async private void InitCaches(CrudAPI api)
        {
            if (api.GetCache(typeof(Uniconta.DataModel.GLVat)) == null)
                await api.LoadCache(typeof(Uniconta.DataModel.GLVat)).ConfigureAwait(false);
            if (api.GetCache(typeof(Uniconta.DataModel.GLAccount)) == null)
                await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            if (api.GetCache(typeof(Uniconta.DataModel.Debtor)) == null)
                await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (api.GetCache(typeof(Uniconta.DataModel.Creditor)) == null)
                await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
        }

        async private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            System.IO.FileStream stream = null;
            try
            {
                var selectedJournal = lookupJournal.SelectedItem as GLDailyJournalClient;
                if (selectedJournal == null)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("RecordNotSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }

                var importDateV = new ImportDATEV(api, selectedJournal);

                stream = new System.IO.FileStream(browseFile.FilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                var journalLines = await importDateV.CreateJournalLines(stream);
                stream.Dispose();
                stream = null;

                if (importDateV.faultyAccounts.Count != 0)
                {
                    journalLines = null;
                    var sb = new StringBuilder();
                    sb.AppendFormat(Uniconta.ClientTools.Localization.lookup("MissingOBJ"), Uniconta.ClientTools.Localization.lookup("Account")).AppendLine(":");
                    foreach (var s in importDateV.faultyAccounts)
                        sb.AppendLine(s);

                    UnicontaMessageBox.Show(sb.ToString(), "", MessageBoxButton.OK);
                    return;
                }

                if (journalLines != null && journalLines.Count > 0)
                {
                    var result = await api.Insert(journalLines);
                    if (result == ErrorCodes.Succes)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("Succes"), Uniconta.ClientTools.Localization.lookup("Message"));
                        DialogResult = true;
                    }
                }
                else
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoJournalLinesCreated"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return;
                }
            }
            catch (Exception ex)
            {
                stream?.Dispose();
                UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"));
                DialogResult = false;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else if (e.Key == Key.Enter)
            {
                if (ImportButton.IsFocused)
                    ImportButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }
    }
}
