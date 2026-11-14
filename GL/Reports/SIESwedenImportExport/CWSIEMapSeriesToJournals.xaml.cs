using DevExpress.Xpf.Grid.Native;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SIESeriesJournalMapGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(SIESeriesJournalMap); } }
        public SIESeriesJournalMapGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public SIESeriesJournalMapGrid() { }
    }

    public partial class CWSIEMapSeriesToJournals : Uniconta.ClientTools.ChildWindow
    {
        public ObservableCollection<SIESeriesJournalMap> SeriesJournalsMap { get; set; }

        public CWSIEMapSeriesToJournals(CrudAPI api, IDictionary<string, string> seriesJournalsMap)
        {
            InitializeComponent();

            Title = string.Format(Uniconta.ClientTools.Localization.lookup("PleaseSelectOBJ"), Uniconta.ClientTools.Localization.lookup("Journals"));

            SeriesJournalsMap = new ObservableCollection<SIESeriesJournalMap>(seriesJournalsMap.Select(x => new SIESeriesJournalMap()
            {
                Serie = x.Key,
                Journal = x.Value
            }));

            DataContext = this;

            dgSeriesJournalMap.api = api;
            dgSeriesJournalMap.Visibility = Visibility.Visible;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (SeriesJournalsMap.Any(x => String.IsNullOrEmpty(x.Journal)))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SIEMapJournals"), Uniconta.ClientTools.Localization.lookup("Error"));
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
