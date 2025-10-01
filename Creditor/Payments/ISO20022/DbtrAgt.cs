using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class DbtrAgt
    {
        private readonly string bic;
        private readonly string name;
        private readonly bool newPaymentFormat;

        private const string HDBTRAGT = "DbtrAgt";
        private const string HFININSTNID = "FinInstnId";

        
        private const string BIC = "BIC";
        private const string BICFI = "BICFI";

        private const string NAME = "Nm";
        

        /// <summary>
        /// BIC/SWIFT Id.
        /// </summary>
        public string Bic
        {
            get
            {
                return bic;
            }
        }

        /// <summary>
        /// Name of the bank.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Financial institution servicing an account for the debtor.
        /// </summary>
        /// <param name="bic">Bank identifier code.</param> 
        /// <param name="name">Name by which an agent (normally a bank name) is known and which is usually used to identify that agent.</param>
        public DbtrAgt(CreditTransferDocument doc)
        {
            this.bic = doc.CompanyBIC;
            this.name = doc.CompanyBankName;
            newPaymentFormat = doc.NewPaymentFormat;
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement dbtrAgt = baseDoc.AppendElement(doc, parent, HDBTRAGT);
            XmlElement finInstnId = baseDoc.AppendElement(doc, dbtrAgt, HFININSTNID);

            if(!string.IsNullOrWhiteSpace(bic))
                baseDoc.AppendElement(doc, finInstnId, newPaymentFormat ? BICFI : BIC, bic);

            if(!string.IsNullOrEmpty(name))
                baseDoc.AppendElement(doc, finInstnId, NAME, name);
        }
    }
}




