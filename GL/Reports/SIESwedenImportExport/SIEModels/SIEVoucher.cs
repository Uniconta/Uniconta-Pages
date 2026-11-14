using System;
using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    [Serializable]
    public class SIEVoucher
    {
        public string Series { get; set; }
        public string Number { get; set; }
        public DateTime VoucherDate { get; set; }
        public string Text { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string Token { get; set; }

        public List<SIEVoucherRow> Rows { get; set; }

        public SIEVoucher()
        {
            Rows = new List<SIEVoucherRow>();
        }
    }
}
