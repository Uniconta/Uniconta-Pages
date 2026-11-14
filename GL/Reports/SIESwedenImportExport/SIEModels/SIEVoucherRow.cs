using System;
using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    [Serializable]
    public class SIEVoucherRow
    {
        public SIEAccount Account { get; set; }
        public List<SIEObject> Objects { get; set; }
        public decimal Amount { get; set; }
        public DateTime RowDate { get; set; }
        public string Text { get; set; }
        public decimal? Quantity;
        public string CreatedBy { get; set; }
        public string Token { get; set; }
    }
}
