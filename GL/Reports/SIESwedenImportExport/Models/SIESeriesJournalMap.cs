using Uniconta.ClientTools.DataModel;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Models
{
    public class SIESeriesJournalMap
    {
        public string Serie { get; set; }

        [InputFieldData]
        [ForeignKey(ForeignKeyTable = typeof(GLDailyJournalClient))]
        public string Journal { get; set; }
    }
}
