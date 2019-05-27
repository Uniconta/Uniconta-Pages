using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class DbtrAgt
    {
        private readonly string bic;
        private readonly string name;

        private const string HDBTRAGT = "DbtrAgt";
        private const string HFININSTNID = "FinInstnId";

        
        private const string BIC = "BIC";
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
        public DbtrAgt(string bic, string name)
        {
            this.bic = bic;
            this.name = name;
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement dbtrAgt = baseDoc.AppendElement(doc, parent, HDBTRAGT);
            XmlElement finInstnId = baseDoc.AppendElement(doc, dbtrAgt, HFININSTNID);

            if(!string.IsNullOrWhiteSpace(bic))
                baseDoc.AppendElement(doc, finInstnId, BIC, bic);

            if(!string.IsNullOrEmpty(name))
                baseDoc.AppendElement(doc, finInstnId, NAME, name);
        }
    }
}




