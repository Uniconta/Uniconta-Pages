using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class PmtInf
    {
        private readonly PmtTpInf pmtTpInf;
        private readonly string paymentInfoId;
        private readonly string paymentMethod;
        private readonly string batchBooking;
        private readonly DateTime requestedExecutionDate;
        private readonly bool pmtInfCtrlSumActive;
        private readonly bool pmtInfNumberOfTransActive;
        private readonly string chargeBearer;

        private readonly Dbtr dbtr;
        private readonly DbtrAcct dbtrAcct;
        private readonly DbtrAgt dbtrAgt;
        private readonly CdtTrfTxInf cdtTrfTxInf;


        private const string HPMTINF = "PmtInf";
        private const string PMTINFID = "PmtInfId";
        private const string PMTMTD = "PmtMtd";
        private const string REQDEXCTNDT = "ReqdExctnDt";
        private const string BTCHBOOKG = "BtchBookg";
        private const string CTRLSUM = "CtrlSum";
        private const string NBOFTXS = "NbOfTxs";
        private const string CHRGBR = "ChrgBr";


        #region Properties
        /// <summary>
        /// Payment Information.
        /// </summary>
        public PmtTpInf PmtTpInf
        {
            get
            {
                return pmtTpInf;
            }
        }

        /// <summary>
        /// PaymentInformationIdentification
        /// </summary>
        public string PaymentInfoId
        {
            get
            {
                return paymentInfoId;
            }
        }

        /// <summary>
        /// PaymentMethod
        /// </summary>
        public string PaymentMethod
        {
            get
            {
                return paymentMethod;
            }
        }

        /// <summary>
        /// RequestedExecutionDate
        /// </summary>
        public DateTime RequestedExecutionDate
        {
            get
            {
                return requestedExecutionDate;
            }
        }

        /// <summary>
        /// Debtor Information.
        /// </summary>
        public Dbtr Dbtr
        {
            get
            {
                return dbtr;
            }
        }

        /// <summary>
        /// Debtor Account Information.
        /// </summary>
        public DbtrAcct DbtrAcct
        {
            get
            {
                return dbtrAcct;
            }
        }

        /// <summary>
        /// Debtor Agent Information.
        /// </summary>
        public DbtrAgt DbtrAgt
        {
            get
            {
                return dbtrAgt;
            }
        }

        public string BatchBooking
        {
            get
            {
                return batchBooking;
            }
        }
        #endregion

        /// <summary>
        /// All credit transfer transactions for the same debit account, payment date and currency must be stated under the same Payment level.
        /// For all payments from Germany, Russia and UK, and for international and high value payments from Sweden and the US, only 999 occurrences are
        /// allowed per PaymentInformation. For all other payment types a maximum of 9.999 instances of <PaymentInformation> is allowed.
        /// </summary>
        /// <param name="doc">CreditTransferDocument</param> 
        /// <param name="paymentMethod">Specifies the means of payment that will be used to move the amount of money. CHK Cheque TRF CreditTransfer.</param>
        /// <param name="requestedExecutionDate">Date at which the initiating party requests the clearing agent to process the payment..</param>
        /// <param name="pmtTpInf">Payment information</param>
        /// <param name="dbtr">Debtor Information</param>
        /// <param name="dbtrAcct">Debtor Account</param>
        /// <param name="dbtrAgt">Debtor Agent</param>

        public PmtInf(CreditTransferDocument doc, PmtTpInf pmtTpInf, Dbtr dbtr, DbtrAcct dbtrAcct, DbtrAgt dbtrAgt, string chargeBearer)
        {
            paymentInfoId = doc.PaymentInfoId;
            paymentMethod = doc.PaymentMethod;
            batchBooking = doc.BatchBooking;
            requestedExecutionDate = doc.RequestedExecutionDate;
            pmtInfCtrlSumActive = doc.PmtInfCtrlSumActive;
            pmtInfNumberOfTransActive = doc.PmtInfNumberOfTransActive;
            this.pmtTpInf = pmtTpInf;
            this.dbtr = dbtr;
            this.dbtrAcct = dbtrAcct;
            this.dbtrAgt = dbtrAgt;
            this.chargeBearer = chargeBearer;
        }


        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent, List<CdtTrfTxInf> cdtTrfTxInfList)
        {
            XmlElement pmtInf = baseDoc.AppendElement(doc, parent, HPMTINF);
            baseDoc.AppendElement(doc, pmtInf, PMTINFID, paymentInfoId);
            baseDoc.AppendElement(doc, pmtInf, PMTMTD, paymentMethod);

            if (!string.IsNullOrEmpty(batchBooking))
                baseDoc.AppendElement(doc, pmtInf, BTCHBOOKG, batchBooking);

            if (pmtInfNumberOfTransActive)
            {
                int numberOfTrans = cdtTrfTxInfList.Where(s => s.PaymentInfoIdReference == PaymentInfoId).Count();
                baseDoc.AppendElement(doc, pmtInf, NBOFTXS, numberOfTrans.ToString());
            }

            if (pmtInfCtrlSumActive)
            {
                double ctrlSum = cdtTrfTxInfList.Where(s => s.PaymentInfoIdReference == PaymentInfoId).Sum(s => s.Amount);
                baseDoc.AppendElement(doc, pmtInf, CTRLSUM, ctrlSum);
            }

            pmtTpInf.Append(baseDoc, doc, pmtInf);

            baseDoc.AppendElement(doc, pmtInf, REQDEXCTNDT, requestedExecutionDate);

            dbtr.Append(baseDoc, doc, pmtInf);
            dbtrAcct.Append(baseDoc, doc, pmtInf);
            dbtrAgt.Append(baseDoc, doc, pmtInf);

            if (chargeBearer != string.Empty)
                baseDoc.AppendElement(doc, pmtInf, CHRGBR, chargeBearer);

            foreach (CdtTrfTxInf cdtTrfTxInf in cdtTrfTxInfList.Where(s => s.PaymentInfoIdReference == PaymentInfoId))
            {
                cdtTrfTxInf.Append(baseDoc, doc, pmtInf);
            }
        }
    }
}

