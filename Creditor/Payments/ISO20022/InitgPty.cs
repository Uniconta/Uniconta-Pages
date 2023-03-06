using System;
using System.Xml;
using Uniconta.ClientTools.Page;

namespace UnicontaISO20022CreditTransfer
{
    public class InitgPty
    {
       // private readonly PostalAddress postalAddress;
        private readonly string companyName;
        private readonly string identificationId;
        private readonly string identificationCode;
        private readonly bool excludeSection;


        private const string PSTLADR = "PstlAdr";
        private const string HINITGPTY = "InitgPty";
        private const string NAME = "Nm";
        private const string ID = "Id";
        private const string HORGID = "OrgId";
        private const string HOTHR = "Othr";
        private const string HSCHMENM = "SchmeNm";
        private const string CD = "Cd";
        private const string CTCTDTLS = "CtctDtls";


        /// <summary>
        /// Company name.
        /// </summary>
        public string CompanyName
        {
            get
            {
                return companyName;
            }
        }

        /// <summary>
        /// Identification Id. (Label: Identifikation af aftalen under Bankafstemning)
        /// </summary>
        public string IdentificationId
        {
            get
            {
                return identificationId;
            }
        }

        /// <summary>
        /// Identification Code. (Label: Kunde-Id under Bankafstemning)
        /// </summary>
        public string IdentificationCode
        {
            get
            {
                return identificationCode;
            }
        }

        public InitgPty(string companyName, string identificationId, string identificationCode, bool excludeSection)
        {
            this.companyName = companyName;
            this.identificationId = identificationId;
            this.identificationCode = identificationCode;
            this.excludeSection = excludeSection;
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            if (excludeSection)
                return;

            XmlElement initgPty = baseDoc.AppendElement(doc, parent, HINITGPTY);
            baseDoc.AppendElement(doc, initgPty, NAME, companyName);

            if (!string.IsNullOrEmpty(identificationId) || !string.IsNullOrEmpty(identificationCode))
            {
                XmlElement id = baseDoc.AppendElement(doc, initgPty, ID);
                XmlElement orgId = baseDoc.AppendElement(doc, id, HORGID);
                XmlElement othr = baseDoc.AppendElement(doc, orgId, HOTHR);

                if (!string.IsNullOrEmpty(identificationId))
                    baseDoc.AppendElement(doc, othr, ID, identificationId);

                if (!string.IsNullOrEmpty(identificationCode))
                {
                    XmlElement SchmeNm = baseDoc.AppendElement(doc, othr, HSCHMENM);
                    baseDoc.AppendElement(doc, SchmeNm, CD, identificationCode);
                }
            }
        }
    }
}

