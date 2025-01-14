using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;
using UnicontaClient.Pages.Creditor.Payments;

namespace UnicontaISO20022CreditTransfer
{
    public class BankSpecificSettingsCH : BankSpecificSettings
    {
        #region Properties
        private CreditorPaymentFormatClientISOCH CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsCH(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISOCH credPaymFormatISOCH = new CreditorPaymentFormatClientISOCH();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISOCH);
            CredPaymFormat = credPaymFormatISOCH;
        }

        /// <summary>
        /// Banks which will be supported in CH
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case chBank.UBS_SIX:
                    companyBankEnum = CompanyBankENUM.UBS_SIX;
                    return companyBankEnum;
                case chBank.CreditSuisse:
                    companyBankEnum = CompanyBankENUM.CreditSuisse;
                    return companyBankEnum;
                default:
                    return CompanyBankENUM.None;
            }
        }

        /// <summary>
        /// Name of the agent (typical a bank name).
        /// </summary>
        public override string CompanyBankName()
        {
            return string.Empty;
        }

        /// <summary>
        /// Country currency - used for calculation of Payment type
        /// </summary>
        public override string LocalCurrency()
        {
            return BaseDocument.CCYCHF;
        }

        /// <summary>
        /// character set (Encoding)
        /// </summary>
        public override Encoding EncodingFormat()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.UBS_SIX:
                case CompanyBankENUM.CreditSuisse:
                    return new UpperCaseUTF8Encoding(false);
                default:
                    return Encoding.GetEncoding("ISO-8859-1");
            }
        }

        public override string ExtServiceCode(ISO20022PaymentTypes paymentType)
        {
            if (paymentType == ISO20022PaymentTypes.SEPA)
                return BaseDocument.EXTSERVICECODE_SEPA;

            return null;

        }

        /// <summary>
        /// Allowed characters
        /// </summary>
        public override void AllowedCharactersRegEx(bool internationalPayment = false)
        {
            if (internationalPayment)
                allowedCharactersRegEx = "[^a-zA-Z0-9 -?:().,'+/]";
            else
                allowedCharactersRegEx = "[^a-zA-Z0-9äöüÄÖÜß -?:().,'+/]";
        }

        /// <summary>
        /// Replacement characters
        /// </summary>
        public override Dictionary<string, string> ReplaceCharactersRegEx()
        {
            replaceCharactersRegEx = new Dictionary<string, string>
            {
                ["Æ"] = "AE",
                ["Ø"] = "OE",
                ["Å"] = "AA",
                ["æ"] = "ae",
                ["ø"] = "oe",
                ["å"] = "aa"
            };

            return replaceCharactersRegEx;
        }

        /// <summary>
        /// XML Attribute NS
        /// </summary>
        public override string XMLAttributeNS()
        {
            return @"http://www.six-interbank-clearing.com/de/pain.001.001.03.ch.02.xsd";
        }

        /// <summary>
        /// Identifies whether a single entry per individual transaction or a batch entry for the sum of the amounts of all transactions 
        /// in the message is requested 
        /// </summary>
        public override string BatchBooking()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.UBS_SIX:
                case CompanyBankENUM.CreditSuisse:
                    return CredPaymFormat.BatchBooking ? TRUE_VALUE : FALSE_VALUE;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Name of the identification scheme, in a coded form as published in an external list
        /// Valid codes: CUST and BANK
        /// Max. 35 characters.
        /// </summary>
        public override string IdentificationCode()
        {
            return null;
        }

        /// <summary>
        /// Total of all individual amounts incluede in the message, irrespective of currencies
        /// </summary>
        public override double HeaderCtrlSum(double amount)
        {
            return amount;
        }

        /// <summary>
        /// Activate Total of all individual amounts per PaymentInfoId, irrespective of currencies
        /// </summary>
        public override bool PmtInfCtrlSumActive()
        {
            return true;
        }

        /// <summary>
        /// Activate number of transactions of all individual amounts per PaymentInfoId
        /// </summary>
        public override bool PmtInfNumberOfTransActive()
        {
            return true;
        }

        /// <summary>
        /// Generate filename
        /// </summary>
        public override string GenerateFileName(int fileID, int companyID)
        {
            string bankName;
            switch (companyBankEnum)
            {
                case CompanyBankENUM.UBS_SIX:
                    bankName = "UBSSIX";
                    break;
                case CompanyBankENUM.CreditSuisse:
                    bankName = "CreditSuisse";
                    break;
                default:
                    bankName = null;
                    break;
            }

            if (bankName == null)
                return string.Format("{0}_{1}_{2}", "ISO20022", fileID, companyID);
            else
                return string.Format("{0}_{1}_{2}_{3}", "ISO20022", bankName, fileID, companyID);
        }

        /// <summary>
        /// Valid codes:
        /// CRED (Creditor)
        /// DEBT (Debtor)
        /// SHAR (Shared)
        /// SLEV (Service Level)
        /// </summary>
        public override string ChargeBearer(string ISOPaymType)
        {
            return string.Empty;
        }

        public override string InstructionPriority()
        {
            switch (CredPaymFormat.Bank)
            {
                case chBank.CreditSuisse:
                    return null;
                default: return BaseDocument.INSTRUCTIONPRIORITY_NORM;
            }
        }

        /// <summary>
        /// Switzerland Creditor Payment Reference Number used for Domestic payments
        /// </summary>
        public override string CreditorRefNumberIBAN(String refNumber, CountryCode companyCountryId, CountryCode credCountryId)
        {
            refNumber = refNumber ?? string.Empty;

            //Domestic payment
            if (companyCountryId == CountryCode.Switzerland && credCountryId == CountryCode.Switzerland)
                refNumber = Regex.Replace(refNumber, "[^0-9]", "");
            else
                refNumber = string.Empty;

            return refNumber;
        }

        /// <summary>
        /// Number (IBAN) - identifier used internationally by financial institutions to uniquely identify the account of a customer. 
        /// Domestic Payments: IBAN will be retrieved from Creditor table because CreditorTransPayment.PaymentId will be reserved for Creditor Payment Reference
        /// Cross border Payments: There will be no Payment Reference. IBAN and SWIFT from Payments-table will be used
        /// </summary>
        public override string CreditorIBAN(String recIBAN, String credIBAN, CountryCode companyCountryId, CountryCode credCountryId)
        {
            credIBAN = credIBAN ?? string.Empty;
            recIBAN = recIBAN ?? string.Empty;

            var ibanNumber = string.Empty;
            //Domestic payment
            if (companyCountryId == CountryCode.Switzerland && credCountryId == CountryCode.Switzerland)
                ibanNumber = credIBAN;
            else
                ibanNumber = recIBAN;

            if (ibanNumber != string.Empty)
            {
                ibanNumber = Regex.Replace(ibanNumber, "[^\\w\\d]", "");
                ibanNumber = ibanNumber.ToUpper();
            }

            return ibanNumber;
        }


        /// <summary>
        ///This reference will be presented on Creditor’s account statement. 
        /// Max 20 characters
        /// Estonia: Not used
        /// </summary>
        public override string RemittanceInfo(string externalAdvText, ISO20022PaymentTypes ISOPaymType, PaymentTypes paymentMethod)
        {
            return string.Empty;
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
        public override string ChargeBearerDebtor()
        {
            return BaseDocument.CHRGBR_SLEV;
        }

        public override PostalAddress CreditorAddress(Uniconta.DataModel.Creditor creditor, PostalAddress creditorAddress, ISO20022PaymentTypes paymentType, bool unstructured = false)
        {
            if (companyBankEnum == CompanyBankENUM.CreditSuisse)
                return base.CreditorAddress(creditor, creditorAddress, paymentType, unstructured);

            return null;

        }

        public override PostalAddress DebtorAddress(Company company, PostalAddress debtorAddress)
        {
            return null;
        }

        public override string CdtrAgtCountryId(string countryId)
        {
            return string.Empty;
        }

        public override string ExternalLocalInstrument(string currencyCode, DateTime executionDate, PaymentTypes paymentMethod, ISO20022PaymentTypes ISOPaymType)
        {
            return string.Empty;
        }

        public override bool RmtInfStrdTpActive()
        {
            return false;
        }

        public override bool CompanyCcyActive()
        {
            return false;
        }

        public override string OCRPaymentType(string creditorOCRPaymentId)
        {
            if (!string.IsNullOrEmpty(creditorOCRPaymentId))
                return BaseDocument.QRR;

            return null;
        }

    }
}
