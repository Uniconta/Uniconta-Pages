using NPOI.Util;
using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class CdtrAgt
    {
        private readonly string bic;
        private readonly string name;
        private readonly string countryId;
        private readonly string clrSysId;
        private readonly string mmbId;

        private readonly bool excludeSection;

        private const string HCDTRAGT = "CdtrAgt";
        private const string HFININSTNID = "FinInstnId";
        private const string BIC = "BIC";
        private const string NAME = "Nm";
        private const string HPSTLADR = "PstlAdr";
        private const string CTRY = "Ctry";
        private const string CLRSYSMMBID = "ClrSysMmbId";
        private const string CLRSYSID = "ClrSysId";
        private const string MMBID = "MmbId";
        private const string CD = "Cd";

        /// <summary>
        /// Financial institution servicing an account for the creditor.
        /// </summary>
        /// <param name="bic">Bank identifier code.</param> 
        /// <param name="name">Name by which a party is known and which is usually used to identify that party.</param>
        /// <param name="countryId">Country Id - Nation with its own government.</param>
        /// <param name="excludeSection"></param>
        public CdtrAgt(string bic, string name, string countryId, string clrSysId, string mmbId, bool excludeSection)
        {
            this.bic = bic;
            this.name = name;
            this.countryId = countryId;
            this.clrSysId = clrSysId;
            this.mmbId = mmbId;
            this.excludeSection = excludeSection;
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            if (excludeSection)
                return;

            XmlElement cdtrAgt = baseDoc.AppendElement(doc, parent, HCDTRAGT);
            XmlElement finInstnId = baseDoc.AppendElement(doc, cdtrAgt, HFININSTNID);

            if (!string.IsNullOrEmpty(bic))
                baseDoc.AppendElement(doc, finInstnId, BIC, bic);

            if (clrSysId != null && mmbId != null)
            {
                XmlElement clrSysMmbId = baseDoc.AppendElement(doc, finInstnId, CLRSYSMMBID);
                XmlElement clrSys = baseDoc.AppendElement(doc, clrSysMmbId, CLRSYSID);
                baseDoc.AppendElement(doc, clrSys, CD, clrSysId);
                baseDoc.AppendElement(doc, clrSysMmbId, MMBID, mmbId);
            }

           

            if (!string.IsNullOrEmpty(name))
                baseDoc.AppendElement(doc, finInstnId, NAME, name);

            if (countryId != string.Empty)
            {
                XmlElement pstlAdr = baseDoc.AppendElement(doc, finInstnId, HPSTLADR);
                baseDoc.AppendElement(doc, pstlAdr, CTRY, countryId);
            }
        }
    }
}





