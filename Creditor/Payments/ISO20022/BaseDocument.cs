using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
#if !SILVERLIGHT
using System.Xml.XPath;
#endif
namespace UnicontaISO20022CreditTransfer
{
    /// <summary>
    /// Base class for documents.
    /// </summary>
    public abstract class BaseDocument : IDocument
    {
        #region Constants

        protected const string XMLNS = "xmlns";
        protected const string XML_PROCESSING_INSTRUCTION = " version='1.0' encoding='UTF-8'";

        protected const string CAC_URI = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
        protected const string CBC_URI = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
        protected const string UDT_URI = "urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2";
        protected const string CCTS_URI = "urn:oasis:names:specification:ubl:schema:xsd:CoreComponentParameters-2";

        protected const string XMLNS_CAC = "xmlns:cac";
        protected const string XMLNS_CBC = "xmlns:cbc";
        public const string XMLNS_XSI = "xmlns:xsi";
        protected const string XMLNS_UDT = "xmlns:udt";
        protected const string XMLNS_CCTS = "xmlns:ccts";
        public const string XMLNS_XSI_VALUE = "http://www.w3.org/2001/XMLSchema-instance";
        protected const string CAC = "cac";
        protected const string CBC = "cbc";

        protected const string UBL_VERSION_KEY = "UBLVersionID";
        protected const string UBL_VERSION_VAL = "2.0";

        protected const string UBL_CUSTOM_KEY = "CustomizationID";
        protected const string UBL_CUSTOM_VAL = "OIOUBL-2.02";

      //  public const string PROFILE_ID_VAL = "ProfileID";
        public const string SCHEME_AGENCY_ID_KEY = "schemeAgencyID";
        public const string SCHEME_AGENCY_ID_VAL = "320";

        public const string SCHEME_ID_KEY = "schemeID";
        protected const string OIOUBL_PROFILEID = "urn:oioubl:id:profileid-1.2";

        public const string ID = "Id";
        protected const string TRUE = "true";
        protected const string FALSE = "false";

        public const string LIST_AGENCY_ID = "listAgencyID";
        public const string LIST_ID = "listID";

        protected const string DOCUMENT_TYPE_CODE = "DocumentTypeCode";
        protected const string RESPONSE_DOC_LIST_ID = "urn:oioubl:codelist:responsedocumenttypecode-1.1";

        protected const string ATTACHMENT = "Attachment";
        protected const string BINARY_OBJ = "EmbeddedDocumentBinaryObject";

        protected const string MIME_CODE = "mimeCode";
        protected const string APP_PDF = "application/pdf";
        protected const string ENCODING_CODE = "encodingCode";
        protected const string BASE64 = "Base64";
        protected const string CHARSET_CODE = "characterSetCode";
        protected const string UTF8 = "UTF-8";

        protected const string ENDPOINT_ID = "EndpointID";
        protected const string PARTY_NAME = "PartyName";

        public const string ADDRESS_LIST_ID = "urn:oioubl:codelist:addressformatcode-1.1";
        public const string ADDRESS_FORMAT_CODE = "AddressFormatCode";
        public const string STRUCTURED_LAX = "StructuredLax";
        public const string UNSTRUCTURED = "Unstructured";
        public const string POSTAL_ADDRESS = "PostalAddress";
        public const string ADDRESSLINE = "AdrLine";
        public const string STREET_NAME = "StrtNm";
        
        public const string TOWN_NAME = "TwnNm";
        public const string POSTAL_CODE = "PstCd";
        public const string COUNTRY = "Ctry";
        public const string IDENTIFICATION_CODE = "IdentificationCode";
        public const string PARTY_LEGAL_ENTITY = "PartyLegalEntity";
        public const string REGISTRATION_NAME = "RegistrationName";
        public const string COMPANY_ID = "CompanyID";
        public const string REGISTRATION_ADDRESS = "RegistrationAddress";
        protected const string CONTACT_PERSON = "Contact";
        protected const string PHONE = "Telephone";
        protected const string EMAIL = "ElectronicMail";
        public const string CURRENCY_ID = "Ccy";
        public const string PERCENT = "Percent";
        public const string NOTE = "Note";
        public const string VALIDATE_OK = "Ok";
        public const string COUNTRY_DK = "DK";


        public const string DUMMYITEM = "Item";
        public const string VALUE_NOT_AVAILABLE = "na";
        public const string PAYMENTMETHOD_TRF = "TRF";
        public const string CCYEUR = "EUR";
        public const string CCYDKK = "DKK";
        public const string CCYSEK = "SEK";
        public const string CCYNOK = "NOK";
        public const string CCYGBP = "GBP";

        public const string FIK71 = "71";
        public const string FIK73 = "73";
        public const string FIK75 = "75";
        public const string FIK04 = "04";

        public const int FIK71LENGTH = 15;
        public const int FIK75LENGTH = 16;
        public const int FIK04LENGTH = 16;

        public const string EXTSERVICECODE_NURG = "NURG";
        public const string EXTSERVICECODE_SEPA = "SEPA";

        public const string INSTRUCTIONPRIORITY_NORM = "NORM";
        public const string EXTCATEGORYPURPOSE_SUPP = "SUPP";
   
        public const string CHRGBR_CRED = "CRED";
        public const string CHRGBR_DEBT = "DEBT";
        public const string CHRGBR_SHAR = "SHAR";
        public const string CHRGBR_SLEV = "SLEV";


        #endregion
#if !SILVERLIGHT
#region Member variables

        protected readonly DocumentType documentType;
      //  protected readonly Profile profile;
        private readonly NumberFormatInfo numberFormatInfo;
        protected XmlDocument validationResult;
        protected bool validate;
        //protected readonly List<ValidationError> errors;
        public int numberDecimalDigits;
        public CompanyBankENUM companyBank;


#endregion

#region Properties

        /*
        public bool HasErrors
        {
            get
            {
                return errors.Count > 0;
            }
        }
        */
        
        public bool Validate
        {
            get
            {
                return validate;
            }

            set
            {
                validate = value;
            }
        }

        public XmlDocument ValidationResult
        {
            get
            {
                return validationResult;
            }
        }

        public int NumberDecimalDigits
        {
            get
            {
                return numberDecimalDigits;
            }

            set
            {
                numberDecimalDigits = value;
            }
        }


        public CompanyBankENUM CompanyBank
        {
            get
            {
                return companyBank;
            }

            set
            {
                companyBank = value;
            }
        }

        /// <summary>
        /// The type of document.
        /// </summary>
        public DocumentType DocumentType { get { return documentType; } }

        /*
        /// <summary>
        /// The document profile.
        /// </summary>
        public Profile Profile { get { return profile; } }

        protected abstract string ProfileId
        {
            get;
        }
        */
#endregion

#region Methods

        protected virtual string EnsureString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="documentType">The type of document.</param>
        /// <param name="profile">The profile of the document.</param> //TODO:DELETE
        public BaseDocument(DocumentType documentType)
        {
            this.documentType = documentType;
         //   this.profile = profile;
          //  this.documents = new Dictionary<string, EmbeddedDocument>();
            this.numberFormatInfo = new NumberFormatInfo();

            this.numberFormatInfo.NumberDecimalDigits = 2;
            this.numberFormatInfo.NumberDecimalSeparator = ".";
            this.numberFormatInfo.NumberGroupSeparator = "";
            this.numberFormatInfo.NumberNegativePattern = 1;
          //  this.errors = new List<ValidationError>(64);
        }

        /// <summary>
        /// Writes the document to a stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        public virtual void ToStream(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
            {
                this.ToStreamWriter(writer);
            }
        }

        /// <summary>
        /// Writes the document to a stream writer.
        /// </summary>
        /// <param name="stream">The stream writer used for writing.</param>
        protected virtual void ToStreamWriter(StreamWriter stream)
        {
            XmlDocument doc = CreateXmlDocument();
            stream.Write(doc.InnerXml);
        }

        public virtual XmlDocument CreateXmlDocument()
        {
            XmlDocument doc = new XmlDocument();

            InitDoc(doc);

            return doc;
        }


        protected virtual void AppendXmlProcessingInstruction(XmlDocument doc)
        {
            XmlProcessingInstruction processingInstruction = doc.CreateProcessingInstruction("xml", XML_PROCESSING_INSTRUCTION);
            doc.AppendChild(processingInstruction);
        }

        protected abstract XmlElement CreateRootElement(XmlDocument doc);

        internal virtual string DecimalToString(Double value)
        {
            if (value != 0d)
            {
                numberFormatInfo.NumberDecimalDigits = numberDecimalDigits;
                return value.ToString("N", numberFormatInfo);
            }
            else
            {
                return "0.00";
            }
        }

        internal virtual XmlElement AppendElement(XmlDocument doc, XmlElement parent, string name)
        {
            XmlElement element = doc.CreateElement(name);

            parent.AppendChild(element);

            return element;
        }

        internal virtual XmlElement AppendElement(XmlDocument doc, XmlElement parent, string name, string value)
        {
            XmlElement element = doc.CreateElement(name);

            element.InnerText = value;
            parent.AppendChild(element);

            return element;
        }

        internal virtual XmlElement AppendElement(XmlDocument doc, XmlElement parent, string name, double value)
        {
            XmlElement element = doc.CreateElement(name);

            element.InnerText = DecimalToString(value);
            parent.AppendChild(element);

            return element;
        }

        internal virtual XmlElement AppendElement(XmlDocument doc, XmlElement parent, string name, DateTime value)
        {
            XmlElement element = doc.CreateElement(name);

            element.InnerText = value.ToString("yyyy-MM-dd");
            parent.AppendChild(element);

            return element;
        }

        internal virtual XmlElement AppendElementSectioned(XmlDocument doc, XmlElement parent, string name, string value)
        {
            XmlElement retval = AppendElement(doc, parent, name, string.Empty);
            XmlCDataSection section = doc.CreateCDataSection(EnsureString(value));
            retval.AppendChild(section);
            return retval;
        }


        protected abstract void CreateDocument(XmlDocument doc, XmlElement root);

        protected virtual void ValidateValues()
        {

        }

        protected virtual void FindErrors()
        {
            foreach (XmlNode err in validationResult.SelectNodes("Schematron/Error"))
            {
                string context = string.Empty;
                string pattern = string.Empty;
                string description = string.Empty;
                string xpath = string.Empty;

                if (err.Attributes.GetNamedItem("context") != null)
                {
                    context = err.Attributes.GetNamedItem("context").Value;
                }

                XmlNode patternNode = err.SelectSingleNode("Pattern");
                if (patternNode != null)
                {
                    pattern = patternNode.InnerText;
                }

                XmlNode descNode = err.SelectSingleNode("Description");
                if (descNode != null)
                {
                    description = descNode.InnerText;
                }

                XmlNode xpathNode = err.SelectSingleNode("Xpath");
                if (xpathNode != null)
                {
                    xpath = xpathNode.InnerText;
                }

               // ValidationError error = new ValidationError(context, pattern, description, xpath);
               // errors.Add(error);
            }
        }
        
        protected virtual void InitDoc(XmlDocument doc)
        {
           // errors.Clear();
            ValidateValues();
           //AppendXmlProcessingInstruction(doc);

            XmlElement root = CreateRootElement(doc);

            CreateDocument(doc, root);
            doc.AppendChild(root);
            

            if (validate)
            {
                try
                {
                    validationResult = ValidateDocument(doc);

                    if (validationResult != null)
                    {
                        FindErrors();
                    }
                }
                catch
                {
                }
            }
        }

        public abstract XmlDocument ValidateDocument(XmlDocument doc);

        #endregion
#else
        public virtual void ToStream(Stream stream)
        {
         
        }
        public DocumentType DocumentType { get; }

#endif
    }
}

