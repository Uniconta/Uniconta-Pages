using System;
using System.Xml;

namespace UnicontaISO20022CreditTransfer
{
    public class PostalAddress
    {
        private const string ADRLINE = "AdrLine";
        
        private string streetName;
        private string cityName;
        private string zipCode;
        private string countryId;

        private string addressLine1;
        private string addressLine2;
        private string addressLine3;

        private bool unstructured;

        public string StreetName
        {
            get
            {
                return streetName;
            }

            set
            {
                streetName = value;
            }
        }

        public string CityName
        {
            get
            {
                return cityName;
            }

            set
            {
                cityName = value;
            }
        }

        public string ZipCode
        {
            get
            {
                return zipCode;
            }

            set
            {
                zipCode = value;
            }
        }

        public string CountryId
        {
            get
            {
                return countryId;
            }

            set
            {
                countryId = value;
            }
        }

        public string AddressLine1
        {
            get
            {
                return addressLine1;
            }

            set
            {
                addressLine1 = value;
            }
        }

        public string AddressLine2
        {
            get
            {
                return addressLine2;
            }

            set
            {
                addressLine2 = value;
            }
        }

        public string AddressLine3
        {
            get
            {
                return addressLine3;
            }

            set
            {
                addressLine3 = value;
            }
        }

        public bool Unstructured
        {
            get
            {
                return unstructured;
            }

            set
            {
                unstructured = value;
            }
        }
        
        private void AppendLine(BaseDocument baseDoc, XmlDocument doc, XmlElement parent, string line)
        {
            if (string.IsNullOrWhiteSpace(line) == false)
            {
                baseDoc.AppendElement(doc, parent, ADRLINE, line);
              }
        }

        internal virtual void Append(BaseDocument baseDoc, XmlDocument doc, XmlElement parent, string addressName)
        {
            XmlElement postalAddress = baseDoc.AppendElement(doc, parent, addressName);

            if (unstructured)
            {
                if (countryId != "")
                    baseDoc.AppendElement(doc, postalAddress, BaseDocument.COUNTRY, countryId);

                AppendLine(baseDoc, doc, postalAddress, addressLine1);
                AppendLine(baseDoc, doc, postalAddress, addressLine2);
                AppendLine(baseDoc, doc, postalAddress, addressLine3);
            }
            else
            {
                baseDoc.AppendElement(doc, postalAddress, BaseDocument.STREET_NAME, streetName);
                baseDoc.AppendElement(doc, postalAddress, BaseDocument.POSTAL_CODE, zipCode);
                baseDoc.AppendElement(doc, postalAddress, BaseDocument.TOWN_NAME, cityName);
                baseDoc.AppendElement(doc, postalAddress, BaseDocument.COUNTRY, countryId);
            }
        }
    }
}
