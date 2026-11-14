using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    public class SIEPeriodValue
    {
        public SIEAccount Account { get; set; }
        public int YearNr { get; set; }
        public int Period { get; set; }
        public decimal Amount { get; set; }
        public decimal? Quantity { get; set; }
        public List<SIEObject> Objects { get; set; }
        public string Token { get; set; }

        public SIEVoucherRow ToVoucherRow()
        {
            var vr = new SIEVoucherRow()
            {
                Account = this.Account,
                Amount = this.Amount,
                Objects = this.Objects,
                Quantity = this.Quantity
            };
            return vr;
        }

        public SIEVoucherRow ToInvertedVoucherRow()
        {
            var vr = ToVoucherRow();
            vr.Amount *= -1;

            return vr;
        }

    }
}
