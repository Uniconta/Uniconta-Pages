using System;
using System.Collections.Generic;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class RmtInf
    {
        private readonly List<string> unstructuredList;
        private readonly string remittanceInfo;
        private readonly string ocrPaymentId;
        private readonly bool   isOCRPayment;


        private const string HRMTINF = "RmtInf";
        private const string USTRD = "Ustrd";
        private const string HSTRD = "Strd";
        private const string HCDTRREFINF = "CdtrRefInf";
        private const string HTP = "Tp";
        private const string CDORPRTRY = "CdOrPrtry";
        private const string CD = "Cd";
        private const string REF = "Ref"; 
        private const string SCOR = "SCOR";
        private const string PURP = "Purp";
        private const string PRTRY = "Prtry";

        /// <summary>
        /// Information that enables the matching, ie, reconciliation, of a payment with the items that the payment is intended to settle, eg, commercial invoices in an account receivable system.
        /// </summary>
        /// <param name="Unstructured">Information supplied to enable the matching of an entry with the items that the transfer is intended to settle, eg, commercial invoices in an accounts' receivable system in an unstructured form.</param> 
        /// <param name="ocrReference">OCR-reference:For Payment Slip payments the OCR-reference is a combination of the former Payment Slip Code and Payment Id with a forward slash separator(fx 71/123456789012345).</param> 
        public RmtInf(List<string> remittanceInfoUnstructList, string remittanceInfo, string ocrPaymentId, bool isOCRPayment)
        {
            this.unstructuredList = remittanceInfoUnstructList;
            this.remittanceInfo = remittanceInfo;
            this.ocrPaymentId = ocrPaymentId;
            this.isOCRPayment = isOCRPayment;
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            if (!string.IsNullOrEmpty(remittanceInfo))
            {
                XmlElement purp = baseDoc.AppendElement(doc, parent, PURP);
                baseDoc.AppendElement(doc, purp, PRTRY, remittanceInfo);
            }

            XmlElement rmtInf = baseDoc.AppendElement(doc, parent, HRMTINF);

            foreach (var i in unstructuredList)
            {
                baseDoc.AppendElement(doc, rmtInf, USTRD, i);
            }

            if (isOCRPayment == true)
            {
                XmlElement strd = baseDoc.AppendElement(doc, rmtInf, HSTRD);
                XmlElement cdtrRefInf = baseDoc.AppendElement(doc, strd, HCDTRREFINF);
                XmlElement tp = baseDoc.AppendElement(doc, cdtrRefInf, HTP);
                XmlElement cdOrPrtry = baseDoc.AppendElement(doc, tp, CDORPRTRY);
                baseDoc.AppendElement(doc, cdOrPrtry, CD, SCOR);
                baseDoc.AppendElement(doc, cdtrRefInf, REF, ocrPaymentId);
            }
        }
    }
}
