using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace UnicontaISO20022CreditTransfer
{
    /// <summary>
    /// Implementation of a Credit Transfer document
    /// </summary>
    public class CreditTransferDocument : BaseDocument, IXMLDocument
    {
        #region Constants

        // protected const string XMLNS_INVOICE = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
        protected const string XMLNS_PAIN003 = "urn:iso:std:iso:20022:tech:xsd:pain.001.001.03";
        //  protected const string PROFILE_ID_NES_UBL_PROFILE_5 = "urn:www.nesubl.eu:profiles:profile5:ver2.0";

        protected const string UNICONTA = "UNICONTA";

        protected const string DOCUMENT_TAG_NAME = "Document";
        protected const string COPY_INDICATOR = "CopyIndicator";
        protected const string UUID_KEY = "UUID";
        protected const string ISSUE_DATE = "IssueDate";

        protected const string PROFORMA_TYPE_CODE = "325";
        protected const string STANDARDINVOICE_TYPE_CODE = "380";
        protected const string INVOICE_TYPE_CODE = "InvoiceTypeCode";
        protected const string INVOICE_TYPE_CODE_LIST = "urn:oioubl:codelist:invoicetypecode-1.1";
        protected const string DEFAULT_CURRENCY_CODE = "DKK";
        protected const string DOCUMENT_CURRENCY_CODE = "DocumentCurrencyCode";
        protected const string ACCOUNTING_COST_CODE = "AccountingCost";
        protected const string DISCREPANCYRESPONSE = "DiscrepancyResponse";
        protected const string REFERENCEID = "ReferenceID";
        protected const string DESCRIPTION = "Description";
        protected const string ORDER_REF = "OrderReference";
        protected const string SALESORDER_ID = "SalesOrderID";
        protected const string CUSTOMER_REF = "CustomerReference";
        protected const string ADDITIONAL_DOC_REF = "AdditionalDocumentReference";
        protected const string ATTACHMENT_ID = "Invoice";
        protected const string ACCOUNTING_SUPPLIER_PARTY = "AccountingSupplierParty";
        protected const string PARTY = "Party";
        protected const string ACCOUNTING_CUSTOMER_PARTY = "AccountingCustomerParty";

        protected const string HCSTMRCDTTRFINITN = "CstmrCdtTrfInitn";
        protected const string HGRPHDR = "GrpHdr";

        protected const string MSGID = "MsgId";
        protected const string CREDTTM = "CreDtTm";
        protected const string NBOFTXS = "NbOfTxs";
        protected const string AUTHSTN = "Authstn";
        protected const string CTRLSUM = "CtrlSum";

        protected const string ERR_INVOICEID_IS_NULL_OR_WHITESPACE = "Invoice id is mandatory and can not be null or whitespace";
        protected const string ERR_ISSUEDATE_IS_NULL = "Issue date is mandatory and can not be null.";
        protected const string ERR_INVALID_PROFILE = "The profile {0} is not valid for invoices";
        protected const string ERR_CURRENCY_IS_NULL_OR_WHITESPACE = "Currency code is mandatory and can not be null or whitespace";
        protected const string ERR_SALESORDER_ID_IS_NULL_OR_WHITESPACE = "Salesorder id is mandatory and can not be null or whitespace";
        protected const string ERR_SALESORDER_ISSUEDATE_IS_NULL = "Salesorder issue date is mandatory and can not be null.";




        #endregion

        #region Member variables

        protected int companyID;
        protected int numberSeqPaymentFileId;
        protected int currentNumberSeqPaymentRefId;
        protected string identificationId;
        protected string identificationCode;
        protected string paymentInfoId;
        protected string paymentMethod;
        protected string batchBooking;
        protected DateTime requestedExecutionDate;
        protected string extServiceCode;
        protected string externalLocalInstrument;
        protected string companyName;
        protected string companyBBAN;
        protected string companyIBAN;
        protected string companyCcy;
        protected string companyBIC;
        protected string companyBankName;
        protected string companyCountryId;
        protected string instructionPriority;
        protected string extCategoryPurpose;
        protected int endToEndId;


        protected string uuid;
        protected DateTime issueDate;
        protected bool proforma;
        protected bool creditNote;
        protected ISO20022PaymentTypes isoPaymentType;
        protected Double endDiscount;
        protected Double cashDiscount;
        protected DateTime cashDiscountDate;
        protected string note;
        protected string currencyCode;
        protected string accountingCostCode;
        protected string salesOrderId;
        protected string requistionNumber;
        protected DateTime salesOrderIssueDate;
        protected string customerReference;
        protected string debtorIdentificationCode;
        protected int headerNumberOfTrans;
        protected double headerCtrlSum;
        protected bool pmtInfCtrlSumActive;
        protected bool pmtInfnumberOfTransActive;
        protected string companyPaymentMethod;
        protected string authstnCodeFeedback;
        protected bool authstnCodeTest;
        protected string creDtTm;
        protected Encoding encodingFormat;
        protected string allowedCharactersRegEx;
        protected Dictionary<string, string> replaceCharactersRegExDict;
        protected string chargeBearer;

        protected InitgPty initgPty;
        protected PmtInf pmtInf;
        protected Dbtr dbtr;


        protected DateTime dueDate;
        protected bool excludeSectionCdtrAgt;

        protected List<PmtInf> pmtInfList;
        protected List<CdtTrfTxInf> cdtTrfTxInfList;

        #endregion

        #region Properties

        public int HeaderNumberOfTrans
        {
            get
            {
                return headerNumberOfTrans;
            }

            set
            {
                headerNumberOfTrans = value;
            }
        }

        public double HeaderCtrlSum
        {
            get
            {
                return headerCtrlSum;
            }

            set
            {
                headerCtrlSum = value;
            }
        }

        public bool PmtInfCtrlSumActive
        {
            get
            {
                return pmtInfCtrlSumActive;
            }

            set
            {
                pmtInfCtrlSumActive = value;
            }
        }

        public string CreDtTm
        {
            get
            {
                return creDtTm;
            }

            set
            {
                creDtTm = value;
            }
        }

        public string BatchBooking
        {
            get
            {
                return batchBooking;
            }

            set
            {
                batchBooking = value;
            }
        }

        public string AuthstnCodeFeedback
        {
            get
            {
                return authstnCodeFeedback;
            }

            set
            {
                authstnCodeFeedback = value;
            }
        }

        public bool AuthstnCodeTest
        {
            get
            {
                return authstnCodeTest;
            }

            set
            {
                authstnCodeTest = value;
            }
        }




        public bool PmtInfNumberOfTransActive
        {
            get
            {
                return pmtInfnumberOfTransActive;
            }

            set
            {
                pmtInfnumberOfTransActive = value;
            }
        }


        public InitgPty InitgPty
        {
            get
            {
                return initgPty;
            }

            set
            {
                initgPty = value;
            }
        }
/*
        public PmtInf PmtInf
        {
            get
            {
                return pmtInf;
            }

            set
            {
                pmtInf = value;
            }
        }
*/
        public Dbtr Dbtr
        {
            get
            {
                return dbtr;
            }

            set
            {
                dbtr = value;
            }
        }

        /// <summary>
        /// Uniconta CompanyID
        /// </summary>
        public int CompanyID
        {
            get
            {
                return companyID;
            }

            set
            {
                companyID = value;
            }
        }

        /// <summary>
        /// Unique ID per Payment file
        /// </summary>
        public int NumberSeqPaymentFileId
        {
            get
            {
                return numberSeqPaymentFileId;
            }

            set
            {
                numberSeqPaymentFileId = value;
            }
        }

        /// <summary>
        /// Unique reference ID per Payment
        /// </summary>
        public int CurrentNumberSeqPaymentRefId
        {
            get
            {
                return currentNumberSeqPaymentRefId;
            }

            set
            {
                currentNumberSeqPaymentRefId = value;
            }
        }



        /// <summary>
        /// The customers reference id for the salesorder. Not mandatory and may be null.
        /// </summary>
        public string CustomerReference
        {
            get
            {
                return customerReference;
            }

            set
            {
                customerReference = value;
            }
        }

        /// <summary>
        /// The currency code used in the document. Mandatory and may not be null.
        /// This version only supports one currency for all operations. Default is "DKK".
        /// </summary>
        public string CurrencyCode
        {
            get
            {
                return currencyCode;
            }

            set
            {
                currencyCode = value;
            }
        }

        /// <summary>
        /// A note to the customer that does not fit into any of the structured elements. Not mandatory and may be null.
        /// This version does not support multiple language notes.
        /// </summary>
        public string Note
        {
            get
            {
                return note;
            }

            set
            {
                note = value;
            }
        }


        /// <summary>
        /// Tdentification assigned by an institution.
        /// Max. 35 characters.
        /// </summary>
        public string IdentificationId
        {
            get
            {
                return identificationId;
            }

            set
            {
                identificationId = value;
            }
        }

        /// <summary>
        /// Name of the identification scheme, in a coded form as published in an external list
        /// Valid codes: CUST and BANK
        /// Max. 35 characters.
        /// </summary>
        public string IdentificationCode
        {
            get
            {
                return identificationCode;
            }

            set
            {
                identificationCode = value;
            }
        }

        /// <summary>
        /// All credit transfer transactions for the same debit account, payment date and currency must be stated under the same Payment level. 
        /// Max. 35 characters.
        /// </summary>
        public string PaymentInfoId
        {
            get
            {
                return paymentInfoId;
            }

            set
            {
                paymentInfoId = value;
            }
        }


        /// <summary>
        /// Specifies the means of payment that will be used to move the amount of money. CHK Cheque TRF CreditTransfer
        /// Allowed Codes: 
        /// 'CHK' Cheque
        /// 'TRF' CreditTransfe
        /// </summary>
        public string PaymentMethod
        {
            get
            {
                return paymentMethod;
            }

            set
            {
                paymentMethod = value;
            }
        }

        /// <summary>
        /// Date at which the initiating party requests the clearing agent to process the payment. To be provided in YYYY-MM-DD
        /// </summary>
        public DateTime RequestedExecutionDate
        {
            get
            {
                return requestedExecutionDate;
            }

            set
            {
                requestedExecutionDate = value;
            }
        }

        /// <summary>
        /// Specifies a pre-agreed service or level of service between the parties, as 
        /// published in an external service level code list
        /// Allowed Codes:         
        /// NURG Non-urgent payment
        /// SDVA SameDayValue
        /// SEPA SingleEuroPaymentsArea
        /// URGP Urgent Payment.
        /// </summary>
        public string ExtServiceCode
        {
            get
            {
                return extServiceCode;
            }

            set
            {
                extServiceCode = value;
            }
        }

        /// <summary>
        /// Specifies the local instrument, as published in an external local instrument code list.
        /// Allowed Codes: 
        /// ONCL (Standard Transfer) - default
        /// SDCL (Same-day Transfer) note: Nordea only accept this code.
        /// </summary>
        public string ExternalLocalInstrument
        {
            get
            {
                return externalLocalInstrument;
            }

            set
            {
                externalLocalInstrument = value;
            }
        }

        /// <summary>
        /// Indicator of the urgency or order of importance that the instructing party would like the instructed party to apply to the processing of the instruction. 
        /// Valid codes
        /// NORM (Normal Instruction) - default)
        /// </summary>
        public string InstructionPriority
        {
            get
            {
                return instructionPriority;
            }

            set
            {
                instructionPriority = value;
            }
        }

        /// <summary>
        /// Instruction for the payment type. Some codes are linked to the service level.This element can either be used here or at the transaction(credit)
        /// level, but not both.SALA or PENS only valid at this level.All credits must be the same.See document Payment Types for more information on codes. 
        /// Allowed Codes: 
        /// CORT Financial payment
        /// INTC Intra company payment
        /// PENS Pension payment
        /// SALA Salary payment
        /// SUPP Supplier payment (Default Value)
        /// TREA Financial payment
        /// </summary>
        public string ExtCategoryPurpose
        {
            get
            {
                return extCategoryPurpose;
            }

            set
            {
                extCategoryPurpose = value;
            }
        }

        /// <summary>
        /// Unique and unambiguous way of identifying an organisation or an individual person.
        /// </summary>
        public string DebtorIdentificationCode
        {
            get
            {
                return debtorIdentificationCode;
            }

            set
            {
                debtorIdentificationCode = value;
            }
        }

        /// <summary>
        /// List of PmtInf
        /// </summary>
        public List<PmtInf> PmtInfList
        {
            get
            {
                return pmtInfList;
            }
        }

        /// <summary>
        /// List of CdtTrfTxInf
        /// </summary>
        public List<CdtTrfTxInf> CdtTrfTxInfList
        {
            get
            {
                return cdtTrfTxInfList;
            }
        }

        /// <summary>
        /// Company name
        /// </summary>
        public string CompanyName
        {
            get
            {
                return companyName;
            }

            set
            {
                companyName = value;
            }
        }



        /// <summary>
        /// Unambiguous identification of the BBAN account of the debtor to which a debit entry will be made as a result of the transaction.
        /// In Denmark it will be: Reg. no. + Account no. 14 char (4+10)
        /// </summary>
        public string CompanyBBAN
        {
            get
            {
                return companyBBAN;
            }

            set
            {
                companyBBAN = value;
            }
        }

        /// <summary>
        /// Unambiguous identification of the IBAN account of the debtor to which a debit entry will be made as a result of the transaction.
        /// </summary>
        public string CompanyIBAN
        {
            get
            {
                return companyIBAN;
            }

            set
            {
                companyIBAN = value;
            }
        }

        /// <summary>
        /// Identification of the currency in which the account is held.
        /// </summary>
        public string CompanyCcy
        {
            get
            {
                return companyCcy;
            }

            set
            {
                companyCcy = value;
            }
        }

        /// <summary>
        /// Bank identifier code.
        /// </summary>
        public string CompanyBIC
        {
            get
            {
                return companyBIC;
            }

            set
            {
                companyBIC = value;
            }
        }

        /// <summary>
        /// Company Payment method.
        /// </summary>
        public string CompanyPaymentMethod
        {
            get
            {
                return companyPaymentMethod;
            }

            set
            {
                companyPaymentMethod = value;
            }
        }



        /// <summary>
        /// Name of the agent (typical a bank name).
        /// </summary>
        public string CompanyBankName
        {
            get
            {
                return companyBankName;
            }

            set
            {
                companyBankName = value;
            }
        }


        /// <summary>
        /// ISO CountryId for the Company
        /// </summary>
        public string CompanyCountryId
        {
            get
            {
                return companyCountryId;
            }

            set
            {
                companyCountryId = value;
            }
        }


        /// <summary>
        /// Payment Reference - goes with payment from debtor to creditor and travels through clearing system. Must be unique. It will be returned in the status reports. 
        /// Max35Text
        /// Nordea: If Instruction Id is missing, Nordea will use EndToEndId as customer reference. This will be used for duplicate control on transaction level.
        /// Danske Bank: Unique for each Business Online agreement min. 3 monthsIf not unique - payment will be rejected.EndToEndIdentification will be returned in PSR
        /// </summary>
        public int EndToEndId
        {
            get
            {
                return endToEndId;
            }

            set
            {
                endToEndId = value;
            }
        }


        /// <summary>
        /// Is it a SEPA payment
        /// </summary>
        public ISO20022PaymentTypes ISOPaymentType
        {
            get
            {
                return isoPaymentType;
            }

            set
            {
                isoPaymentType = value;
            }
        }

        /// <summary>
        /// Exclude section CdtrAgt
        /// </summary>
        public bool ExcludeSectionCdtrAgt
        {
            get
            {
                return excludeSectionCdtrAgt;
            }

            set
            {
                excludeSectionCdtrAgt = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Encoding EncodingFormat
        {
            get
            {
                return encodingFormat;
            }

            set
            {
                encodingFormat = value;
            }
        }

        /// <summary>
        /// Allowed characters - Regular expression
        /// </summary>
        public string AllowedCharactersRegEx
        {
            get
            {
                return allowedCharactersRegEx;
            }

            set
            {
                allowedCharactersRegEx = value;
            }
        }

        /// <summary>
        /// Allowed characters - Regular expression
        /// </summary>
        public Dictionary<string, string> ReplaceCharactersRegExDict
        {
            get
            {
                return replaceCharactersRegExDict;
            }

            set
            {
                replaceCharactersRegExDict = value;
            }
        }

        /// <summary>
        /// Charge Bearer
        /// </summary>
        public string ChargeBearer
        {
            get
            {
                return chargeBearer;
            }

            set
            {
                chargeBearer = value;
            }
        }


        #endregion

        #region Methods

        /// <summary>
        /// Creates a new instance of XMLDocument.
        /// </summary>
        /// <param name="profile">The profile to use. Current implementation supports only PAIN 003</param> //TODO:DELETE
        public CreditTransferDocument() : base(DocumentType.General)
        {
            this.pmtInfList = new List<PmtInf>();
            this.cdtTrfTxInfList = new List<CdtTrfTxInf>();
        }

        protected override XmlElement CreateRootElement(XmlDocument doc)
        {
            XmlElement root = doc.CreateElement(DOCUMENT_TAG_NAME);

            root.SetAttribute(XMLNS, XMLNS_PAIN003);

            return root;
        }

        protected override void ValidateValues()
        {
            
        }
        
        protected virtual void AppendGrpHdr(XmlDocument doc, XmlElement root, InitgPty initgPty) //InitgPTY skal med som parameter
        {
            string msgId = string.Format("{0}{1}{2}", UNICONTA, companyID.ToString().PadLeft(totalWidth: 7, paddingChar: '0'), numberSeqPaymentFileId.ToString().PadLeft(totalWidth: 7, paddingChar: '0'));

            XmlElement grpHdr = AppendElement(doc, root, HGRPHDR);
            AppendElement(doc, grpHdr, MSGID, msgId);
            AppendElement(doc, grpHdr, CREDTTM, creDtTm);

            if (authstnCodeTest)
            {
                XmlElement authstnTest = AppendElement(doc, grpHdr, AUTHSTN);
                AppendElement(doc, authstnTest, "Prtry", "TEST");
            }

            if (!string.IsNullOrEmpty(authstnCodeFeedback))
            {
                XmlElement authstnFeedBack = AppendElement(doc, grpHdr, AUTHSTN);
                AppendElement(doc, authstnFeedBack, "Prtry", authstnCodeFeedback);
            }

            AppendElement(doc, grpHdr, NBOFTXS, headerNumberOfTrans.ToString());

            if (HeaderCtrlSum != 0)
                AppendElement(doc, grpHdr, CTRLSUM, HeaderCtrlSum);

            initgPty.Append(this, doc, grpHdr);
        }


        public override XmlDocument ValidateDocument(XmlDocument doc)
        {
            XmlDocument retval = null;

            return retval;
        }

        protected override void CreateDocument(XmlDocument doc, XmlElement root)
        {
            root = AppendElement(doc, root, HCSTMRCDTTRFINITN);

            AppendGrpHdr(doc, root, InitgPty);

            foreach (var pmtInf in pmtInfList)
            {
                pmtInf.Append(this, doc, root, CdtTrfTxInfList);
            }
        }

    }
    #endregion
}
