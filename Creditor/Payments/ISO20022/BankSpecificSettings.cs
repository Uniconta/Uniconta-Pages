using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;
using UnicontaClient.Pages.Creditor.Payments;

namespace UnicontaISO20022CreditTransfer
{
    /// <summary>
    /// Class with methods to handle bank specific settings and other methods to build the XML file.
    /// </summary>
    public abstract class BankSpecificSettings
    {
        #region Member constants
        protected const string TRUE_VALUE = "true";
        protected const string FALSE_VALUE = "false";
        #endregion

        #region Member variables
        protected CompanyBankENUM companyBankEnum;
        protected string companyCountryId;
        protected string allowedCharactersRegEx;
        protected bool internationalPayment;
        protected Dictionary<string, string> replaceCharactersRegEx;
        #endregion

        #region Properties
        public CompanyBankENUM CompanyBankEnum
        {
            get
            {
                return companyBankEnum;
            }

            set
            {
                companyBankEnum = value;
            }
        }

        public string  CompanyCountryId
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
        #endregion

        static public BankSpecificSettings BankSpecTypeInstance(CreditorPaymentFormat credPaymFormat)
        {
            switch (credPaymFormat._ExportFormat)
            {
                case (byte)ExportFormatType.ISO20022_DK:
                    return new BankSpecificSettingsDK(credPaymFormat);
                case (byte)ExportFormatType.ISO20022_NO:
                    return new BankSpecificSettingsNO(credPaymFormat);
                case (byte)ExportFormatType.ISO20022_NL:
                    return new BankSpecificSettingsNL(credPaymFormat);
                case (byte)ExportFormatType.ISO20022_DE:
                    return new BankSpecificSettingsDE(credPaymFormat);
                case (byte)ExportFormatType.ISO20022_EE:
                    return new BankSpecificSettingsEE(credPaymFormat);
                case (byte)ExportFormatType.ISO20022_SE:
                    return new BankSpecificSettingsSE(credPaymFormat);
                case (byte)ExportFormatType.ISO20022_UK:
                    return new BankSpecificSettingsUK(credPaymFormat);
                case (byte)ExportFormatType.ISO20022_LT:
                    return new BankSpecificSettingsLT(credPaymFormat);
                case (byte)ExportFormatType.ISO20022_CH:
                    return new BankSpecificSettingsCH(credPaymFormat);
                default:
                    return new BankSpecificSettingsDK(credPaymFormat); //This is active for all the proprietary bank formats - In a future version Test class has to be differentiated
            }
        }

        public abstract CompanyBankENUM CompanyBank();

        public bool SEPACountry(string countryId)
        {
            return Enum.IsDefined(typeof(SEPACountries), countryId);
        }

        /// <summary>
        /// character set (Encoding)
        /// </summary>
        public virtual Encoding EncodingFormat()
        {
            return new UTF8Encoding(false);
        }


        /// <summary>
        /// Allowed characters
        /// </summary>
        public virtual void AllowedCharactersRegEx(bool internationalPayment = false)
        {
            allowedCharactersRegEx = "[^a-zA-Z0-9 -?:().,'+/]";
        }

        /// <summary>
        /// Replacement characters
        /// </summary>
        public virtual Dictionary<string, string> ReplaceCharactersRegEx()
        {
            replaceCharactersRegEx = null;
            return replaceCharactersRegEx;
        }

        /// <summary>
        /// XML Attribute NS
        /// </summary>
        public virtual string XMLAttributeNS()
        {
            return BaseDocument.XMLNS_PAIN003;
        }

        /// <summary>
        /// Unique reference ID per Payment. Number sequence is maintained in Uniconta table.CreditorPaymentReference
        /// </summary>
        public virtual int NumberSeqPaymentRefId(int paymentRefIf)
        {
            return paymentRefIf;
        }

        /// <summary>
        /// Date and time at which the message was created
        /// </summary>
        public virtual string CreDtTim()
        {
            return DateTimeOffset.Now.ToString("o");
        }


        /// <summary>
        /// Authstn tag in order to control the feedback type in pain.002.001.03 (PSR) and the communication channel.
        /// </summary>
        public virtual string AuthstnCodeFeedback()
        {
            return string.Empty;
        }


        /// <summary>
        /// Identification assigned by an institution.
        /// Max. 35 characters.
        /// NORDEA CUST: Customer identification Signer Id as agreed with (or assigned by) Nordea, min. 10 and max. 18 characters.
        /// Danske Bank: It's not used but Danske Bank recommend to use the CVR number
        /// </summary>
        public virtual string IdentificationId(string identificationId, string companyCVR)
        {
            return string.Empty;
        }


        /// <summary>
        /// Name of the identification scheme, in a coded form as published in an external list
        /// Valid codes: CUST and BANK
        /// Max. 35 characters.
        /// </summary>
        public virtual string IdentificationCode()
        {
            return "BANK";
        }
      

        /// <summary>
        /// Specifies the local instrument, as published in an external local instrument code list.
        /// Allowed Codes: 
        /// ONCL (Standard Transfer)
        /// SDCL (Same-day Transfer)
        /// </summary>
        public virtual string ExternalLocalInstrument(string currencyCode, DateTime executionDate)
        {
            return "ONCL";
        }
       

        /// <summary>
        /// Indicator of the urgency or order of importance that the instructing party would like the instructed party to apply to the processing of the instruction. 
        /// Valid codes
        /// NORM (Normal Instruction) - default)
        /// </summary>
        public virtual string InstructionPriority()
        {
            return BaseDocument.INSTRUCTIONPRIORITY_NORM;
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
        public virtual string ExtCategoryPurpose(ISO20022PaymentTypes ISOPaymType)
        {
            return BaseDocument.EXTCATEGORYPURPOSE_SUPP;
        }

        /// <summary>
        /// Used for Lithuania
        /// </summary>
        public virtual string ExtProprietaryCode()
        {
            return string.Empty;
        }

        /// <summary>
        /// Exclude section CdtrAgt
        /// DNB Norway: CdtrAgt isn't needed for Domestic paymenst and if included BIC or Address need to be provided.
        /// </summary>
        public virtual bool ExcludeSectionCdtrAgt(ISO20022PaymentTypes ISOPaymType, string creditorSWIFT)
        {
            return false;
        }


        /// <summary>
        /// CdtrAgt - Creditor Bank CountryId
        /// </summary>
        public virtual string CdtrAgtCountryId(string countryId)
        {
            return countryId;
        }

        /// <summary>
        /// Unique and unambiguous way of identifying an organisation or an individual person.
        /// NORDEA: Customer agreement identification with Nordea is mandatory (BANK), minimum 10 and maximum 18 digits must be used.
        /// </summary>
        public virtual string DebtorIdentificationCode(string debtorId)
        {
            return string.Empty;
        }

        /// <summary>
        /// Identifies whether a single entry per individual transaction or a batch entry for the sum of the amounts of all transactions 
        /// in the message is requested 
        /// </summary>
        public virtual string BatchBooking()
        {
            return string.Empty;
        }


        /// <summary>
        /// Test marked payment
        /// </summary>
        public virtual bool TestMarked()
        {
            return false;
        }

        /// <summary>
        /// Total of all individual amounts included in the message, irrespective of currencies
        /// </summary>
        public virtual double HeaderCtrlSum(double amount)
        {
            return 0;
        }

      

        /// <summary>
        /// Activate Total of all individual amounts per PaymentInfoId, irrespective of currencies
        /// </summary>
        public virtual bool PmtInfCtrlSumActive()
        {
            return false;
        }

        /// <summary>
        /// Activate number of transactions of all individual amounts per PaymentInfoId
        /// </summary>
        public virtual bool PmtInfNumberOfTransActive()
        {
            return false;
        }

        /// <summary>
        /// Activate Tp section in Structured Remittance info
        /// </summary>
        public virtual bool RmtInfStrdTpActive()
        {
            return true;
        }

        /// <summary>
        /// Activate DebtorAcct Currency
        /// </summary>
        public virtual bool CompanyCcyActive()
        {
            return true;
        }

        /// <summary>
        /// Insert Paymnent type OCR or QRR (Swiss QR bill)
        /// </summary>
        public virtual string OCRPaymentType(string creditorOCRPaymentId)
        {
            if (!string.IsNullOrEmpty(creditorOCRPaymentId))
                return BaseDocument.OCR;

            return null;
        }

        /// <summary>
        /// Generate filename
        /// </summary>
        public virtual string GenerateFileName(int fileID, int companyID)
        {
            return string.Format("{0}_{1}_{2}", "ISO20022", fileID, companyID);
        }

        /// <summary>
        /// Merged payments
        /// </summary>
        public virtual string PaymentInfoId(int fileSeqNumber, DateTime executionDate, PaymentTypes paymentMethod, Currencies? currency, bool merged = false)
        {
            var result = string.Format("{0}{1}{2}{3}{4}", merged ? 1 : 0, currency.HasValue ? (int)currency : 0, (int)paymentMethod, fileSeqNumber.ToString().PadLeft(6, '0'), executionDate.ToString("ddMMyy"));

            return StandardPaymentFunctions.RegularExpressionReplace(result, allowedCharactersRegEx, replaceCharactersRegEx);
        }

        /// <summary>
        /// This is the requested execution date when the payment will be processed if sufficient funds on the account.
        /// </summary>
        public virtual DateTime RequestedExecutionDate(string companyIBAN, DateTime executionDate)
        {

            return executionDate;
        }

        /// <summary>
        /// DOMESTIC Payment:
        /// Transfers within the same country.
        /// 
        /// SEPA Payment:
        /// The conditions for a SEPA payment
        /// 1.Creditor payment has currency code 'EUR'
        /// 2.Sender - Bank og Receiver-Bank has to be member of the  European Economic Area.
        /// 3.Creditor account has to be IBAN
        /// 4.Payment must be Non-urgent
        /// 5.Countries where local currency is EUR and payment is in EUR 
        /// 
        /// CROSS BORDER Payment:
        /// 
        /// </summary>
        public virtual ISO20022PaymentTypes ISOPaymentType(string paymentCcy, string  companyIBAN, string creditorIBAN, string creditorSWIFT, string creditorCountryId, string companyCountryId)
        {
            companyIBAN = companyIBAN ?? string.Empty;
            creditorIBAN = creditorIBAN ?? string.Empty;
            creditorSWIFT = creditorSWIFT ?? string.Empty;
            companyCountryId = companyCountryId ?? string.Empty;
            creditorCountryId = creditorCountryId ?? string.Empty;

            //Company
            var companyBankCountryId = string.Empty;
            if (companyIBAN.Length >= 2)
                companyBankCountryId = companyIBAN.Substring(0, 2);
            else
                companyBankCountryId = companyCountryId;
            
            //Creditor
            var creditorBankCountryId = string.Empty;
            if (creditorIBAN.Length >= 2)
                creditorBankCountryId = creditorIBAN.Substring(0, 2);
            else if (creditorSWIFT.Length > 6)
                creditorBankCountryId = creditorSWIFT.Substring(4, 2);
            else
                creditorBankCountryId = creditorCountryId;

            
            //SEPA payment:
            if (paymentCcy == BaseDocument.CCYEUR)
            {
                if (companyBankCountryId == creditorBankCountryId && LocalCurrency() != BaseDocument.CCYEUR)
                    return ISO20022PaymentTypes.DOMESTIC;
                else if (SEPACountry(companyBankCountryId) && SEPACountry(creditorBankCountryId))
                    return ISO20022PaymentTypes.SEPA;
                else
                    return ISO20022PaymentTypes.CROSSBORDER;
            }
            else if (companyBankCountryId == creditorBankCountryId)
            {
                return ISO20022PaymentTypes.DOMESTIC;
            }
            else
            {
                return ISO20022PaymentTypes.CROSSBORDER;
            }

            return ISO20022PaymentTypes.DOMESTIC;
        }


        /// <summary>
        /// International payment or Local payment (Transfers within the same country)
        /// Used to identifify the correct character set
        /// </summary>
        public virtual bool InternationalPayment(string companyIBAN, string creditorIBAN, string creditorSWIFT, string creditorCountryId, string companyCountryId)
        {
            companyIBAN = companyIBAN ?? string.Empty;
            creditorIBAN = creditorIBAN ?? string.Empty;
            creditorSWIFT = creditorSWIFT ?? string.Empty;
            companyCountryId = companyCountryId ?? string.Empty;
            creditorCountryId = creditorCountryId ?? string.Empty;

            //Company
            var companyBankCountryId = string.Empty;
            if (companyIBAN.Length >= 2)
                companyBankCountryId = companyIBAN.Substring(0, 2);
            else
                companyBankCountryId = companyCountryId;

            //Creditor
            var creditorBankCountryId = string.Empty;
            if (creditorIBAN.Length >= 2)
                creditorBankCountryId = creditorIBAN.Substring(0, 2);
            else if (creditorSWIFT.Length > 6)
                creditorBankCountryId = creditorSWIFT.Substring(4, 2);
            else
                creditorBankCountryId = creditorCountryId;

            if (companyBankCountryId != creditorBankCountryId)
                internationalPayment = true;
            else
                internationalPayment = false;

            return internationalPayment;
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
        public virtual string ExtServiceCode(ISO20022PaymentTypes paymentType)
        {
            if (paymentType == ISO20022PaymentTypes.SEPA)
                return BaseDocument.EXTSERVICECODE_SEPA;

            return BaseDocument.EXTSERVICECODE_NURG;

        }

        /// <summary>
        /// Name of the agent (typical a bank name).
        /// </summary>
        public abstract string CompanyBankName();

        /// <summary>
        /// Country currency - used for calculation of Payment type
        /// </summary>
        public abstract string LocalCurrency();


        /// <summary>
        /// Unambiguous identification of the IBAN account of the debtor to which a debit entry will be made as a result of the transaction.
        /// </summary>
        public virtual string CompanyIBAN(String iban)
        {
            iban = iban ?? string.Empty;

            if (iban != string.Empty)
            {
                iban = Regex.Replace(iban, "[^\\w\\d]", "");
                iban = iban.ToUpper();
            }

            return iban;
        }

        /// <summary>
        /// Bank identifier code.
        /// </summary>
        public virtual string CompanyBIC(String bic)
        {
            bic = bic ?? string.Empty;

            if (bic != string.Empty)
            {
                bic = Regex.Replace(bic, "[^\\w\\d]", "");
                bic = bic.ToUpper();
            }

            return bic;
        }

    
        /// <summary>
        /// Company Name
        /// </summary>
        public virtual string CompanyName(string companyName)
        {
            return StandardPaymentFunctions.RegularExpressionReplace(companyName, allowedCharactersRegEx, replaceCharactersRegEx);
        }

        /// <summary>
        /// Unambiguous identification of the BBAN account of the debtor to which a debit entry will be made as a result of the transaction.
        /// In Denmark it will be: Reg. no. + Account no. 14 char (4+10)
        /// </summary>
        public virtual string CompanyBBAN(String regNum, String bban)
        {
            regNum = regNum ?? string.Empty;
            bban = bban ?? string.Empty;

            if (!string.IsNullOrEmpty(regNum) && !string.IsNullOrEmpty(bban))
            {
                regNum = Regex.Replace(regNum, "[^0-9]", "");
                bban = Regex.Replace(bban, "[^0-9]", "");
                bban = bban.PadLeft(10, '0');
            }

            return regNum+bban;
        }

        /// <summary>
        /// Unambiguous identification of the BBAN account of the creditor (domestic account number).
        /// </summary>
        public virtual string CreditorBBAN(string recBBAN, string credBBAN, string bic)
        {
            var bban = recBBAN ?? string.Empty;

            bban = Regex.Replace(bban, "[^0-9]", "");

            return bban;
        }

        /// <summary>
        /// Number (IBAN) - identifier used internationally by financial institutions to uniquely identify the account of a customer. 
        /// </summary>
        public virtual string CreditorIBAN(String recIBAN, String credIBAN, CountryCode companyCountryId, CountryCode credCountryId)
        {
            recIBAN = recIBAN ?? string.Empty;

            if (recIBAN != string.Empty)
            {
                recIBAN = Regex.Replace(recIBAN, "[^\\w\\d]", "");
                recIBAN = recIBAN.ToUpper();
            }

            return recIBAN;
        }

        /// <summary>
        /// Bank Identifier Code SWIFT. Code allocated to financial institutions by the Registration Authority.
        /// </summary>
        public string CreditorBIC(String bic)
        {
            bic = bic ?? string.Empty;

            if (bic != string.Empty)
            {
                bic = Regex.Replace(bic, "[^\\w\\d]", "");
                bic = bic.ToUpper();
            }

            return bic;
        }

        /// <summary>
        /// Danish Inpayment Form FIK04
        /// </summary>
        public virtual Tuple<string, string> CreditorFIK04(string ocrLine)
        {
            ocrLine = ocrLine ?? string.Empty;
            string paymID = string.Empty;
            string creditorAccount = string.Empty;

            return new Tuple<string, string>(paymID, creditorAccount);
        }

        /// <summary>
        /// Danish Inpayment Form FIK71
        /// </summary>
        public virtual Tuple<string, string> CreditorFIK71(String ocrLine, string creditorPaymId)
        {
            ocrLine = ocrLine ?? string.Empty;
            string paymID = string.Empty;
            string creditorAccount = string.Empty;
             
            return new Tuple<string, string>(paymID, creditorAccount);
        }

        /// <summary>
        /// Danish Inpayment Form FIK73
        /// </summary>
        public virtual string CreditorFIK73(String ocrLine)
        {
            ocrLine = ocrLine ?? string.Empty;
            string creditorAccount = string.Empty;
    
            return creditorAccount;
        }

        /// <summary>
        /// Danish Inpayment Form FIK75
        /// </summary>
        public virtual Tuple<string, string> CreditorFIK75(string ocrLine, string creditorPaymId)
        {
            ocrLine = ocrLine ?? string.Empty;
            string paymID = string.Empty;
            string creditorAccount = string.Empty;

            return new Tuple<string, string>(paymID, creditorAccount);
        }

        /// <summary>
        /// Creditor Payment Reference number
        /// </summary>
        public virtual string CreditorRefNumber(String refNumber)
        {
            return string.Empty;
        }

        /// <summary>
        /// Creditor Payment Reference number (IBAN)
        /// </summary>
        public virtual string CreditorRefNumberIBAN(String refNumber, CountryCode companyCountryId, CountryCode credCountryId)
        {
            return string.Empty;
        }


        /// <summary>
        ///
        /// </summary>
        public virtual PostalAddress DebtorAddress(Company company, PostalAddress debtorAddress)
        {
            debtorAddress.AddressLine1 = StandardPaymentFunctions.RegularExpressionReplace(company._Address1, allowedCharactersRegEx, replaceCharactersRegEx);
            debtorAddress.AddressLine2 = StandardPaymentFunctions.RegularExpressionReplace(company._Address2, allowedCharactersRegEx, replaceCharactersRegEx);
            debtorAddress.AddressLine3 = StandardPaymentFunctions.RegularExpressionReplace(company._Address3, allowedCharactersRegEx, replaceCharactersRegEx);
            debtorAddress.CountryId = ((CountryISOCode)company._CountryId).ToString();

            debtorAddress.Unstructured = true;

            return debtorAddress;
        }

        /// <summary>
        /// Creditor Name
        /// </summary>
        public virtual string CreditorName(string credName)
        {
            var result = string.IsNullOrEmpty(credName) ? BaseDocument.VALUE_NOT_AVAILABLE : credName;
            result = StandardPaymentFunctions.RegularExpressionReplace(result, allowedCharactersRegEx, replaceCharactersRegEx);
            return result;
        }

        /// <summary>
        /// Creditor Address
        /// </summary>
        public virtual PostalAddress CreditorAddress(Uniconta.DataModel.Creditor creditor, PostalAddress creditorAddress, ISO20022PaymentTypes paymentType, bool unstructured = false)
        {
            var adr1 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address1, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr2 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address2, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr3 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address3, allowedCharactersRegEx, replaceCharactersRegEx);
            var zipCode = StandardPaymentFunctions.RegularExpressionReplace(creditor._ZipCode, allowedCharactersRegEx, replaceCharactersRegEx);
            var city = StandardPaymentFunctions.RegularExpressionReplace(creditor._City, allowedCharactersRegEx, replaceCharactersRegEx);

            if (creditor._ZipCode != null && !unstructured)
            {
                creditorAddress.ZipCode = zipCode;
                creditorAddress.CityName =city;
                creditorAddress.StreetName = adr1;
            }
            else
            {
                creditorAddress.AddressLine1 = adr1;
                creditorAddress.AddressLine2 = adr2;
                creditorAddress.AddressLine3 = adr3;
                creditorAddress.Unstructured = true;
            }

            creditorAddress.CountryId = ((CountryISOCode)creditor._Country).ToString();

            return creditorAddress;
        }



        /// <summary>
        /// Instruction Id – Customer reference number - must be unique. It will be returned in the status reports and bank account statement. It will not be send to the beneficiary. 
        /// Nordea specific: If Instruction Id is missing, EndToEndId will be used as customer reference. This will be used for duplicate control on transaction level.
        /// </summary>
        public virtual string InstructionId(string internalMessage)
        {
            string instructionId = StandardPaymentFunctions.RegularExpressionReplace(internalMessage, allowedCharactersRegEx, replaceCharactersRegEx);
        
            if (instructionId.Length > 35)
                instructionId = instructionId.Substring(0, 35);

            return instructionId;
        }

        /// <summary>
        /// The End to End Reference must be unique. This will be used for duplicate control on transaction level, if Instruction Id is not present. It will be returned in the status reports and will be forwarded to beneficiary.
        /// </summary>
        public virtual string EndtoendId(long endtoendId)
        {
            return endtoendId.ToString();
        }

        /// <summary>
        /// Valid codes:
        /// CRED (Creditor)
        /// DEBT (Debtor)
        /// SHAR (Shared)
        /// SLEV (Service Level)
        /// 
        /// Denmark:
        /// Domestic payments: SHAR 
        /// SEPA payments: SLEV
        /// Cross-border payments: DEBT
        /// 
        /// /// This Charge bearer is per payment
        /// </summary>
        public virtual string ChargeBearer(string ISOPaymType) //ISO20022PaymentTypes ISOPaymType) //TODO: This has to be changed - it has to use enum
        {
            ISOPaymType = ISOPaymType ?? string.Empty;
            
            switch (ISOPaymType)
            {
                case "DOMESTIC":
                    return BaseDocument.CHRGBR_SHAR;
                case "CROSSBORDER":
                    return BaseDocument.CHRGBR_SHAR;
                case "SEPA":
                    return BaseDocument.CHRGBR_SLEV;
                default:
                    return BaseDocument.CHRGBR_SHAR;
            }
         }


        /// <summary>
        /// Valid codes:
        /// CRED (Creditor)
        /// DEBT (Debtor)
        /// SHAR (Shared)
        /// SLEV (Service Level)
        /// 
        /// This Charge bearer is per Debtor (only once)
        /// </summary>
        public virtual string ChargeBearerDebtor()
        {
            return string.Empty;
        }


        /// <summary>
        /// Nordea: Reference quoted on statement. This reference will be presented on Creditor’s account statement. It may only be used for domestic payments. Only used by Norway, Denmark and Sweden.
        /// Max 20 characters
        /// </summary>
        public virtual string RemittanceInfo(string externalAdvText, ISO20022PaymentTypes ISOPaymType, PaymentTypes paymentMethod)
        {
            string remittanceInfo = StandardPaymentFunctions.RegularExpressionReplace(externalAdvText, allowedCharactersRegEx, replaceCharactersRegEx);

            if (remittanceInfo != string.Empty && ISOPaymType == ISO20022PaymentTypes.DOMESTIC)
            {
                switch (paymentMethod)
                {
                    case PaymentTypes.VendorBankAccount:
                        if (remittanceInfo.Length > 20)
                            remittanceInfo = remittanceInfo.Substring(0, 20);
                        break;

                    case PaymentTypes.IBAN:
                        remittanceInfo = string.Empty;
                        break;

                    case PaymentTypes.PaymentMethod3: //FIK71
                        remittanceInfo = string.Empty;
                        break;

                    case PaymentTypes.PaymentMethod5: //FIK75
                        remittanceInfo = string.Empty;
                        break;

                    case PaymentTypes.PaymentMethod4: //FIK73
                        remittanceInfo = string.Empty;
                        break;

                    case PaymentTypes.PaymentMethod6: //FIK04
                        remittanceInfo = string.Empty;
                        break;
                }
            }
            else
            {
                remittanceInfo = string.Empty;
            }

            return remittanceInfo;
        }

        /// <summary>
        /// Unstructured Remittance Information
        /// </summary>
        public virtual List<string> Ustrd(string externalAdvText, ISO20022PaymentTypes ISOPaymType, CreditorTransPayment trans, bool extendedText)
        {
            var ustrdText = StandardPaymentFunctions.RegularExpressionReplace(externalAdvText, allowedCharactersRegEx, replaceCharactersRegEx);
            
            int maxLines = 1;
            int maxStrLen = 140;
            List<string> resultList = new List<string>();

            if (ustrdText != string.Empty)
            {
                if (ustrdText.Length > maxLines * maxStrLen)
                    ustrdText = ustrdText.Substring(0, maxLines*maxStrLen);

                resultList = ustrdText.Select((x, i) => i)
                            .Where(i => i % maxStrLen == 0)
                            .Select(i => ustrdText.Substring(i, ustrdText.Length - i >= maxStrLen ? maxStrLen : ustrdText.Length - i)).ToList<string>();
            }

            return resultList;
        }

        protected string AddSeparator(string value)
        {
            return value != null ? ", " : string.Empty;
        }

    }

    public class UpperCaseUTF8Encoding : UTF8Encoding
    {
        public UpperCaseUTF8Encoding() : this(false)
        {
        }

        public UpperCaseUTF8Encoding(bool encoderShouldEmitUTF8Identifier) : this(encoderShouldEmitUTF8Identifier, false)
        {
        }

        public UpperCaseUTF8Encoding(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes) : base(encoderShouldEmitUTF8Identifier, throwOnInvalidBytes)
        {
        }

        public override string WebName => base.WebName.ToUpper(CultureInfo.InvariantCulture);
    }
}
