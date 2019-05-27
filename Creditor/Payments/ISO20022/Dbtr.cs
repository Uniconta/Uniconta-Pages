using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class Dbtr
    {
        private readonly PostalAddress postalAddress;
        private readonly string debtorName;
        private readonly string debtorIdentificationCode;

        private const string PSTLADR = "PstlAdr";
        private const string HDBTR = "Dbtr";
        private const string NAME = "Nm";
        private const string ID = "Id";
        private const string HORGID = "OrgId";
        private const string HOTHR = "Othr";
        private const string HSCHMENM = "SchmeNm";
        private const string CD = "Cd";
        private const string SCHMENAME_BANK = "BANK";


        #region Properties
        /// <summary>
        /// Postal Address.
        /// </summary>
        public PostalAddress PostalAddress
        {
            get
            {
                return postalAddress;
            }
        }
        #endregion

        /// <summary>
        /// Debtor identifies the owner of the DebtorAccount
        /// </summary>
        /// <param name="debtorName">Name by which a party is known and which is usually used to identify that party.</param> 
        /// <param name="postalAddress">Information that locates and identifies a specific address, as defined by postal services.</param>
        public Dbtr(string debtorName, PostalAddress postalAddress, string debtorIdentificationCode = "") 
        {
            this.postalAddress = postalAddress;
            this.debtorName = debtorName;
            this.debtorIdentificationCode = debtorIdentificationCode;
        }

        protected virtual void AppendPostalAddress(BaseDocument baseDoc, XmlDocument doc, XmlElement parent, string addressName, PostalAddress address)
        {
            address.Append(baseDoc, doc, parent, addressName);
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement dbtr = baseDoc.AppendElement(doc, parent, HDBTR);
            baseDoc.AppendElement(doc, dbtr, NAME, debtorName);

            AppendPostalAddress(baseDoc, doc, dbtr, PSTLADR, this.PostalAddress);

            if (debtorIdentificationCode != string.Empty)
            {
                XmlElement id = baseDoc.AppendElement(doc, dbtr, ID);
                XmlElement orgId = baseDoc.AppendElement(doc, id, HORGID);
                XmlElement othr = baseDoc.AppendElement(doc, orgId, HOTHR);
                baseDoc.AppendElement(doc, othr, ID, debtorIdentificationCode);

                XmlElement schmeNm = baseDoc.AppendElement(doc, othr, HSCHMENM);
                baseDoc.AppendElement(doc, schmeNm, CD, SCHMENAME_BANK);
            }
        }
    }
}


