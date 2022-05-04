using System;
using System.Xml;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;

namespace UnicontaISO20022CreditTransfer
{
    public class CdtrAcct
    {
        private readonly string accountId;

        private readonly bool isIBAN;
        private readonly bool isOCR;
        private readonly byte exportFormat;
        private readonly CreditorTransPayment trans;

        private const string HCDTRACCT = "CdtrAcct";
        private const string ID = "Id";
        private const string OTHR = "Othr";
        private const string IBAN = "IBAN";
        private const string SCHMENM = "SchmeNm";
        private const string PRTRY = "Prtry";
        private const string CD = "Cd";
        private const string OCR = "OCR";
        private const string BGNR = "BGNR";
        private const string BBAN = "BBAN";


        /// <summary>
        /// Unambiguous identification of the account of the creditor to which a credit entry will be posted as a result of the payment transaction.
        /// </summary>
        /// <param name="AccountId">Either IBAN or BBAN account format.</param> 
        /// <param name="isIBAN">Is Account identifier a IBAN.</param>
        /// <param name="isOCR">Is OCR Creditor Number.</param>
        public CdtrAcct(string accountId, bool isIBAN, bool isOCR, byte exportFormat, CreditorTransPayment trans)
        {
            this.accountId = accountId;
            this.isIBAN = isIBAN;
            this.isOCR = isOCR;
            this.exportFormat = exportFormat;
            this.trans = trans;
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement cdtrAcct = baseDoc.AppendElement(doc, parent, HCDTRACCT);
            XmlElement id = baseDoc.AppendElement(doc, cdtrAcct, ID);

            if (isIBAN)
            {
                baseDoc.AppendElement(doc, id, IBAN, accountId);
            }
            else
            {
                XmlElement othr = baseDoc.AppendElement(doc, id, OTHR);
                baseDoc.AppendElement(doc, othr, ID, accountId);

                XmlElement schmeNm = baseDoc.AppendElement(doc, othr, SCHMENM);

                if (isOCR && exportFormat == (byte)ExportFormatType.ISO20022_DK) 
                    baseDoc.AppendElement(doc, schmeNm, PRTRY, OCR);
                else if (isOCR && exportFormat == (byte)ExportFormatType.ISO20022_SE && trans._PaymentMethod == PaymentTypes.PaymentMethod3)
                    baseDoc.AppendElement(doc, schmeNm, PRTRY, BGNR);
                else
                    baseDoc.AppendElement(doc, schmeNm, CD, BBAN);
            }
        }
    }
}




