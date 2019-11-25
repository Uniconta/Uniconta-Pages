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
        SQLCache GlAccounts;
        public CWImportToJournal(CrudAPI crudApi, string title = null)
        {
            DataContext = this;
            InitializeComponent();
            Title = title ?? Uniconta.ClientTools.Localization.lookup("Import");
            api = crudApi;
            lookupJournal.api = crudApi;
            browseFile.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.CSV);
            GlAccounts = api.GetCache(typeof(Uniconta.DataModel.GLAccount));
        }

        async private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var fileContents = browseFile.FileBytes;
            if (fileContents != null && fileContents.Length > 0)
            {
                System.IO.FileStream stream = null;
                try
                {
                    stream = new System.IO.FileStream(browseFile.FilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    var importDateV = new ImportDATEV(api, stream);
                    var selectedJournal = lookupJournal.SelectedItem as GLDailyJournalClient;
                    if (selectedJournal == null)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("RecordNotSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                        return;
                    }

                    if (GlAccounts == null)
                        GlAccounts = await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);

                    var glAccount = GlAccounts.Get<Uniconta.DataModel.GLAccount>(selectedJournal.Account);

                    var journalLines = await importDateV.CreateJournalLines(selectedJournal);

                    if (glAccount != null && glAccount._DATEVAuto)
                    {
                        foreach (var journalLine in journalLines)
                            journalLine.Vat = glAccount._Vat;
                    }

                    if (journalLines != null && journalLines.Length > 0)
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
                    UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                    DialogResult = false;
                }
                finally { stream?.Close(); }
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoFilesSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
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
            else
                if (e.Key == Key.Enter)
            {
                if (ImportButton.IsFocused)
                    ImportButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }
    }
}
