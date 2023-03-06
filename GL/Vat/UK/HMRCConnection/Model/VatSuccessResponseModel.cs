using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.UK.HMRCConnection.Model
{
    class VatSuccessResponseModel
    {
        public string processingDate { get; set; }
        public string paymentIndicator { get; set; }
        public string formBundleNUmber { get; set; }
        public string chargeRefNumber { get; set; }
    }
}
