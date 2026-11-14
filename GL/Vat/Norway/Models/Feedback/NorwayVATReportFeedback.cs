using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnicontaClient.Pages.GL.Vat.Norway.Models.Validation;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway.Models.Feedback
{
    public class NorwayVATReportFeedback
    {
        /// <summary>True when the Altinn process has ended (process.ended is set), i.e. Skatteetaten has finished processing</summary>
        public bool IsCompleted { get; set; }
        /// <summary>When the Altinn process ended, if it has</summary>
        public DateTime? Ended { get; set; }
        /// <summary>Name of the current Altinn process task while the submission is still being processed</summary>
        public string CurrentTask { get; set; }
        /// <summary>Payment information (KID, account, amount) as delivered by Skatteetaten right after submission</summary>
        public string PaymentInformationXml { get; set; }
        public Valideringsresultat ValidationResult { get; set; }
        /// <summary>The validation result as delivered, kept for the attachment on the VAT report</summary>
        public string ValidationResultXml { get; set; }
        public byte[] ReceiptPDFData { get; set; }
    }
}
