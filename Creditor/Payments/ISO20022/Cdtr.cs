using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class Cdtr
    {
        private readonly string creditorName;
        private readonly PostalAddress postalAddress;

        private const string HCDTR = "Cdtr";
        private const string PSTLADR = "PstlAdr";
        private const string HDBTR = "Dbtr";
        private const string NAME = "Nm";

        /// <summary>
        /// The postal Creditor address.
        /// </summary>
        public PostalAddress PostalAddress
        {
            get
            {
                return postalAddress;
            }
        }

        /// <summary>
        /// Party to which an amount of money is due.
        /// </summary>
        /// <param name="name">Name by which a party is known and which is usually used to identify that party.</param>
        /// <param name="postalAddress">Information that locates and identifies a specific address, as defined by postal services.</param>
        public Cdtr(string creditorName, PostalAddress postalAddress)
        {
            this.creditorName = creditorName;
            this.postalAddress = postalAddress;
        }

        protected virtual void AppendPostalAddress(BaseDocument baseDoc, XmlDocument doc, XmlElement parent, string addressName, PostalAddress address)
        {
            address.Append(baseDoc, doc, parent, addressName);
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent)
        {
            XmlElement cdtr = baseDoc.AppendElement(doc, parent, HCDTR);
            baseDoc.AppendElement(doc, cdtr, NAME, creditorName);

            if (PostalAddress != null)
                AppendPostalAddress(baseDoc, doc, cdtr, PSTLADR, this.PostalAddress);
        }
    }
}





