using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class CdtrAgt
    {
        private readonly string bic;
        private readonly string name;
        private readonly string countryId;
        private readonly bool excludeSection;

        private const string HCDTRAGT = "CdtrAgt";
        private const string HFININSTNID = "FinInstnId";
        private const string BIC = "BIC";
        private const string NAME = "Nm";
        private const string HPSTLADR = "PstlAdr";
        private const string CTRY = "Ctry";

        /// <summary>
        /// Financial institution servicing an account for the creditor.
        /// </summary>
        /// <param name="bic">Bank identifier code.</param> 
        /// <param name="name">Name by which a party is known and which is usually used to identify that party.</param>
        /// <param name="countryId">Country Id - Nation with its own government.</param>
        /// <param name="excludeSection"></param>
        public CdtrAgt(string bic, string name, string countryId, bool excludeSection)
        {
            this.bic = bic;
            this.name = name;
            this.countryId = countryId;
            this.excludeSection = excludeSection;
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            if (excludeSection)
                return;

            XmlElement cdtrAgt = baseDoc.AppendElement(doc, parent, HCDTRAGT);
            XmlElement finInstnId = baseDoc.AppendElement(doc, cdtrAgt, HFININSTNID);

            if(!string.IsNullOrEmpty(bic))
                baseDoc.AppendElement(doc, finInstnId, BIC, bic);

            if(!string.IsNullOrEmpty(name))
                baseDoc.AppendElement(doc, finInstnId, NAME, name);

            if (countryId != string.Empty)
            {
                XmlElement pstlAdr = baseDoc.AppendElement(doc, finInstnId, HPSTLADR);
                baseDoc.AppendElement(doc, pstlAdr, CTRY, countryId);
            }
        }
    }
}





