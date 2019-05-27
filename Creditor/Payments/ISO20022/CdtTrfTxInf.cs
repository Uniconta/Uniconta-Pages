using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class CdtTrfTxInf
    {
        private string paymentInfoIdReference;
        private string instructionId;
        private readonly string endToEndId;
        private readonly double amount;
        private readonly string currencyCode;
        private readonly string chargeBearer;

        private readonly CdtrAgt cdtrAgt;
        private readonly Cdtr cdtr;
        private readonly CdtrAcct cdtrAcct;
        private readonly RmtInf rmtInf;

        private const string HCDTTRFTXINF = "CdtTrfTxInf";
        private const string HPMTID = "PmtId";
        private const string INSTRID = "InstrId";
        private const string ENDTOENDID = "EndToEndId";
        private const string HAMT = "Amt";
        private const string INSTDAMT = "InstdAmt";
        private const string CHRGBR = "ChrgBr";


        #region Properties

        public string PaymentInfoIdReference
        {
            get
            {
                return paymentInfoIdReference;
            }

            set
            {
                paymentInfoIdReference = value;
            }
        }

        public string InstructionId
        {
            get
            {
                return instructionId;
            }

            set
            {
                instructionId = value;
            }
        }

        public string EndToEndId
        {
            get { return endToEndId; }
        }

        public double Amount
        {
            get { return amount; }
        }

        public string CurrencyCode
        {
            get { return currencyCode; }
        }

        public string ChargeBearer
        {
            get { return chargeBearer; }
        }

        /// <summary>
        /// 
        /// </summary>
        public CdtrAgt CdtrAgt
        {
            get
            {
                return cdtrAgt;
            }
        }


        /// <summary>
        /// 
        /// </summary>
        public Cdtr Cdtr
        {
            get
            {
                return cdtr;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public CdtrAcct CdtrAcct
        {
            get
            {
                return cdtrAcct;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public RmtInf RmtInf
        {
            get
            {
                return rmtInf;
            }
        }



        #endregion


        /// <summary>
        /// Set of elements providing information specific to the individual transaction(s) included in the message.
        /// </summary>
        /// <param name="paymentInfoIdReference">System value will be used to link CdTrfTxInf to the correct PmtInf.</param> 
        /// <param name="instructionId">Unique identification, as assigned by the initiating party, to unambiguously identify the payment.</param> 
        /// <param name="endToEndId">Unique identification assigned by the initiating party to unambiguously identify the transaction. This identification is passed on, unchanged, throughout the entire end-to-end chain.</param> 
        /// <param name="amount">Amount of money to be moved between the debtor and creditor, before deduction of charges, expressed in the currency as ordered by the initiating party.</param> 
        /// <param name="currencyCode">Currency code.</param> 
        /// <param name="cdtrAgt">Financial institution servicing an account for the creditor.</param> 
        /// <param name="cdtr">Party to which an amount of money is due.</param> 
        /// <param name="cdtrAcct">Unambiguous identification of the account of the creditor to which a credit entry will be posted as a result of the payment transaction.</param> 
        /// <param name="rmtInf">Information that enables the matching, ie, reconciliation, of a payment with the items that the payment is intended to settle, eg, commercial invoices in an account receivable system.</param> 
        public CdtTrfTxInf(string paymentInfoIdReference, string instructionId, int endToEndId, double amount, string currencyCode, CdtrAgt cdtrAgt, Cdtr cdtr, CdtrAcct cdtrAcct, RmtInf rmtInf, string chargeBearer = BaseDocument.CHRGBR_SHAR)
        {
            this.paymentInfoIdReference = paymentInfoIdReference;
            this.instructionId = instructionId;
            this.endToEndId = Convert.ToString(endToEndId);
            this.amount = amount;
            this.currencyCode = currencyCode;
            this.chargeBearer = chargeBearer;
            this.cdtrAgt = cdtrAgt;
            this.cdtr = cdtr;
            this.cdtrAcct = cdtrAcct;
            this.rmtInf = rmtInf;
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement cdtTrfTxInf = baseDoc.AppendElement(doc, parent, HCDTTRFTXINF);
            XmlElement pmtId = baseDoc.AppendElement(doc, cdtTrfTxInf, HPMTID);
            baseDoc.AppendElement(doc, pmtId, INSTRID, instructionId);
            baseDoc.AppendElement(doc, pmtId, ENDTOENDID, endToEndId);

            XmlElement amt = baseDoc.AppendElement(doc, cdtTrfTxInf, HAMT);
            baseDoc.AppendElement(doc, amt, INSTDAMT, amount).SetAttribute(BaseDocument.CURRENCY_ID, currencyCode);

            baseDoc.AppendElement(doc, cdtTrfTxInf, CHRGBR, chargeBearer);

            cdtrAgt.Append(baseDoc, doc, cdtTrfTxInf);
            cdtr.Append(baseDoc, doc, cdtTrfTxInf);
            cdtrAcct.Append(baseDoc, doc, cdtTrfTxInf);
            rmtInf.Append(baseDoc, doc, cdtTrfTxInf);
        }
    }
}
