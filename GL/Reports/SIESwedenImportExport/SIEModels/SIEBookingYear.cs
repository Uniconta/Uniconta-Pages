using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    /// <summary>
    /// #RAR
    /// </summary>
    [Serializable]
    public class SIEBookingYear
    {
        public int ID { get; set; }
        public DateTime? Start { get; set; }
        public DateTime? End { get; set; }
    }
}
