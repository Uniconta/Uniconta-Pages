using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class DbtrAcct
    {
        private readonly string accountId;
        private readonly string bic;
        private readonly string ccy;
        private readonly bool companyCcyActive;

        private const string HDBTRACCT = "DbtrAcct";
        private const string ID = "Id";
        private const string CCY = "Ccy";
        private const string OTHR = "Othr";
        private const string IBAN = "IBAN";
        private const string BBAN = "BBAN";
        private const string SCHMENM = "SchmeNm";
        private const string CD = "Cd";

        /// <summary>
        /// Unambiguous identification of the account of the debtor to which a debit entry will be made as a result of the transaction.
        /// </summary>
        /// <param name="currencyCode">Identification of the currency in which the account is held.</param> 
        /// <param name="accountId">BBAN account format.</param> 
        public DbtrAcct(string ccy, string accountId)
        {
            this.ccy = ccy;
            this.accountId = accountId;
        }

        /// <summary>
        /// Unambiguous identification of the account of the debtor to which a debit entry will be made as a result of the transaction.
        /// </summary>
        /// <param name="currencyCode">Identification of the currency in which the account is held.</param> 
        /// <param name="accountId">IBAN account format</param> 
        /// <param name="bic">BIC</param> 
        public DbtrAcct(string ccy, string accountId, string bic, bool companyCcyActive)
        {
            this.ccy = ccy;
            this.accountId = accountId;
            this.bic = bic;
            this.companyCcyActive = companyCcyActive;
        }


        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement dbtrAcct = baseDoc.AppendElement(doc, parent, HDBTRACCT);
            XmlElement id = baseDoc.AppendElement(doc, dbtrAcct, ID);

            if(string.IsNullOrWhiteSpace(bic))
            {
                XmlElement othr = baseDoc.AppendElement(doc, id, OTHR);
                baseDoc.AppendElement(doc, othr, ID, accountId);

                XmlElement schmeNm = baseDoc.AppendElement(doc, othr, SCHMENM);
                baseDoc.AppendElement(doc, schmeNm, CD, BBAN);
            }
            else
            {
                baseDoc.AppendElement(doc, id, IBAN, accountId);
            }

            if (companyCcyActive)
                baseDoc.AppendElement(doc, dbtrAcct, CCY, ccy);
        }
    }
}



