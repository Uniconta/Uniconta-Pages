using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.GetVatReport.Models
{
    [Obsolete]
    class VatReportLineUser : VatReportLine
    {
        public GLVat glVat { get; set; }
    }
}
