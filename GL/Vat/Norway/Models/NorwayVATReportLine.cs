using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway.Models
{
    public class NorwayVATReportLine
    {
        [Display(Name = nameof(LineNo), ResourceType = typeof(NorwayVATReportText))]
        public string LineNo { get; set; }

        [Display(Name = nameof(Account), ResourceType = typeof(NorwayVATReportText))]
        public string Account { get; set; }

        [Display(Name = nameof(AccountName), ResourceType = typeof(NorwayVATReportText))]
        public string AccountName { get; set; }

        [Display(Name = nameof(VAT), ResourceType = typeof(NorwayVATReportText))]
        public string VAT { get; set; }

        [Display(Name = nameof(VATType), ResourceType = typeof(NorwayVATReportText))]
        public string VATType { get; set; }

        [Display(Name = nameof(StandardVAT), ResourceType = typeof(NorwayVATReportText))]
        public string StandardVAT { get; set; }

        [Display(Name = nameof(Rate), ResourceType = typeof(NorwayVATReportText))]
        public double Rate { get; set; }

        [Display(Name = nameof(BaseAmount), ResourceType = typeof(NorwayVATReportText))]
        public double BaseAmount { get; set; }

        [Display(Name = nameof(ReportingBaseAmount), ResourceType = typeof(NorwayVATReportText))]
        public int ReportingBaseAmount { get; set; }

        [Display(Name = nameof(PostedVAT), ResourceType = typeof(NorwayVATReportText))]
        public double PostedVAT { get; set; }

        [Display(Name = nameof(ReportingPostedVAT), ResourceType = typeof(NorwayVATReportText))]
        public int ReportingPostedVAT { get; set; }

        [Display(Name = nameof(Specification), ResourceType = typeof(NorwayVATReportText))]
        public string Specification { get; set; }

        [Display(Name = nameof(Comment), ResourceType = typeof(NorwayVATReportText))]
        public string Comment { get; set; }

        [Display(Name = nameof(PredefinedComment), ResourceType = typeof(NorwayVATReportText))]
        public string PredefinedComment { get; set; }

        /// <summary>
        /// Result of the rate and sign checks, shown to the user
        /// </summary>
        [Display(Name = nameof(Check), ResourceType = typeof(NorwayVATReportText))]
        public string Check { get; set; }

        /// <summary>
        /// Outgoing (calculated) VAT carries basis and rate. False for deductions
        /// </summary>
        public bool IsOutgoing { get; set; }

        /// <summary>
        /// Rate from Skatteetaten's code list for the SAF-T code, set by the rate check. Null when the code has no rate
        /// </summary>
        public double? ExpectedRate { get; set; }

        /// <summary>
        /// The configured rate does not fit the SAF-T code, or a zero rated code has VAT. Blocks submission
        /// </summary>
        public string RateWarning { get; set; }

        /// <summary>
        /// The effective rate (VAT / basis) differs from the SAF-T code's rate. A warning the user confirms on Send
        /// </summary>
        public string EffectiveRateWarning { get; set; }

        /// <summary>
        /// The line has the opposite sign of what Skatteetaten expects and needs a remark
        /// </summary>
        public bool OppositeSign { get; set; }

        /// <summary>
        /// Posted VAT (with øre, debit positive) per VAT account behind this line. Used to reset the accounts in the settlement journal
        /// </summary>
        public Dictionary<string, double> PostedVATByAccount { get; set; } = new Dictionary<string, double>();

        /// <summary>
        /// The line needs the user's attention (rate warning, effective rate warning or opposite sign). Used for row highlighting
        /// </summary>
        public bool HasWarning { get { return !String.IsNullOrEmpty(RateWarning) || !String.IsNullOrEmpty(EffectiveRateWarning) || OppositeSign; } }

        [Reporting]
        public GLVat VatRef { get; set; }

        public GLVatType VatTypeRef { get; set; }

        [Reporting]
        public GLAccount AccountRef { get; set; }
    }

    public static class NorwayVATReportText
    {
        public static string LineNo => Localization.lookup(nameof(LineNo));
        public static string Account => Localization.lookup(nameof(Account));
        public static string AccountName => Localization.lookup("Name");
        public static string VAT => Localization.lookup("Vat");
        public static string VATType => Localization.lookup("VATtype");
        public static string StandardVAT => Localization.lookup("StandardVATCode");
        public static string Rate => Localization.lookup(nameof(Rate));
        public static string BaseAmount => Localization.lookup("AmountBase");
        public static string ReportingBaseAmount => Localization.lookup("NorwayVATAmountBase");
        public static string PostedVAT => Localization.lookup("PostedVAT");
        public static string ReportingPostedVAT => Localization.lookup("NorwayVATPostedVAT");
        public static string Specification => Localization.lookup("Specification");
        public static string Comment => Localization.lookup("NorwayVATComment");
        public static string PredefinedComment => Localization.lookup("NorwayVATPredefinedComment");
        public static string Check => Localization.lookup("Check");
    }
}
