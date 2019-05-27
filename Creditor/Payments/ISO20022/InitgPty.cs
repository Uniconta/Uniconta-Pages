using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class InitgPty
    {
       // private readonly PostalAddress postalAddress;
        private readonly string companyName;
        private readonly string identificationId;
        private readonly string identificationCode;

        private const string PSTLADR = "PstlAdr";
        private const string HINITGPTY = "InitgPty";
        private const string NAME = "Nm";
        private const string ID = "Id";
        private const string HORGID = "OrgId";
        private const string HOTHR = "Othr";
        private const string HSCHMENM = "SchmeNm";
        private const string CD = "Cd";


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

        public InitgPty(string companyName, string identificationId, string identificationCode)// , PostalAddress postalAddress)  //TODO:Address er ikke mandatory
        {
           // this.postalAddress = postalAddress;
            this.companyName = companyName;
            this.identificationId = identificationId;
            this.identificationCode = identificationCode;
        }

        /*
        protected virtual void AppendPostalAddress(BaseDocument baseDoc, XmlDocument doc, XmlElement parent, string addressName, PostalAddress address)
        {
            address.Append(baseDoc, doc, parent, addressName);
        }
        */

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement initgPty = baseDoc.AppendElement(doc, parent, HINITGPTY);
            baseDoc.AppendElement(doc, initgPty, NAME, companyName);

            //  AppendPostalAddress(baseDoc, doc, initgPty, PSTLADR, this.PostalAddress);

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

