using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages.Creditor.Payments;

namespace UnicontaISO20022CreditTransfer
{
    public class BankSpecificSettingsLT : BankSpecificSettings
    {
        #region Properties
        private CreditorPaymentFormatClientISOLT CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsLT(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISOLT credPaymFormatISOLT = new CreditorPaymentFormatClientISOLT();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISOLT);
            CredPaymFormat = credPaymFormatISOLT;
        }

        /// <summary>
        /// Banks which will be supported in LT Lithuania
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Standard:
                    companyBankEnum = CompanyBankENUM.Standard;
                    return companyBankEnum;
                case ltBank.Swedbank:
                    companyBankEnum = CompanyBankENUM.Swedbank;
                    return companyBankEnum;
                case ltBank.SEB:
                    companyBankEnum = CompanyBankENUM.SEB;
                    return companyBankEnum;
                case ltBank.Luminor:
                    companyBankEnum = CompanyBankENUM.Luminor;
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
            return BaseDocument.CCYEUR;
        }


        /// <summary>
        /// Allowed characters
        /// </summary>
        public override void AllowedCharactersRegEx(bool internationalPayment = false)
        {
            if (internationalPayment)
                allowedCharactersRegEx = "[^a-zA-Z0-9 -?:().,'+/]";
            else
                allowedCharactersRegEx = "[^a-zA-Z0-9ąęėįšųūžĄĘĖĮŠŲŪŽ -?:().,'+/]";
        }


        /// <summary>
        /// Identification assigned by an institution.
        /// Max. 35 characters.
        /// </summary>
        public override string IdentificationId(string identificationId, string companyCVR)
        {
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Standard:
                case ltBank.Swedbank:
                case ltBank.SEB:
                    return identificationId ?? companyCVR;
                default: return null;
            }
        }

        /// <summary>
        /// Name of the identification scheme, in a coded form as published in an external list
        /// Max. 35 characters.
        /// </summary>
        public override string IdentificationCode()
        {
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Standard:
                case ltBank.Swedbank:
                case ltBank.SEB:
                    return "BANK";
                default: return null;
            }
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
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Standard:
                case ltBank.Swedbank: 
                case ltBank.SEB: return true;
                case ltBank.Luminor: return false;
                default: return false;
            }
        }

        /// <summary>
        /// Activate number of transactions of all individual amounts per PaymentInfoId
        /// </summary>
        public override bool PmtInfNumberOfTransActive()
        {
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Standard:
                case ltBank.Swedbank:
                case ltBank.SEB: return true;
                case ltBank.Luminor: return false;
                default: return false;
            }
        }

        /// <summary>
        /// LT: Only two Addresslines are accepted
        /// </summary>
        public override PostalAddress DebtorAddress(Company company, PostalAddress debtorAddress)
        {
            var adr1 = StandardPaymentFunctions.RegularExpressionReplace(company._Address1, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr2 = StandardPaymentFunctions.RegularExpressionReplace(company._Address2, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr3 = StandardPaymentFunctions.RegularExpressionReplace(company._Address3, allowedCharactersRegEx, replaceCharactersRegEx);

            debtorAddress.AddressLine1 = adr1;
            debtorAddress.AddressLine2 = adr2;
            debtorAddress.AddressLine3 = adr3;
            debtorAddress.CountryId = ((CountryISOCode)company._CountryId).ToString();

            debtorAddress.Unstructured = true;
            debtorAddress.AddressLine2 = adr3 != null ? string.Format("{0} {1}", adr2, adr3) : adr2;
            debtorAddress.AddressLine3 = null;

            return debtorAddress;
        }


        /// <summary>
        /// LT: Only two Addresslines are accepted
        /// </summary>
        public override PostalAddress CreditorAddress(Uniconta.DataModel.Creditor creditor, PostalAddress creditorAddress)
        {
            if (CredPaymFormat.Bank == ltBank.Standard)
                return null;

            var adr1 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address1, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr2 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address2, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr3 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address3, allowedCharactersRegEx, replaceCharactersRegEx);
            var zipCode = StandardPaymentFunctions.RegularExpressionReplace(creditor._ZipCode, allowedCharactersRegEx, replaceCharactersRegEx);
            var city = StandardPaymentFunctions.RegularExpressionReplace(creditor._City, allowedCharactersRegEx, replaceCharactersRegEx);

            bool unstructured = false;
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Swedbank:
                case ltBank.SEB:
                case ltBank.Luminor:
                    unstructured = false;
                    break;
                default:
                    unstructured = false;
                    break;
            }

            StringBuilder strB = new StringBuilder();
            if (unstructured)
            {
                var adr1_result = strB.Append(adr1).Append(AddSeparator(adr2)).Append(adr2).ToString();
                creditorAddress.AddressLine1 = adr1_result.Length > 70 ? adr1_result.Substring(0, 70) : adr1_result;
                strB.Clear();
                var adr2_result = strB.Append(adr3).Append(AddSeparator(zipCode)).Append(zipCode).Append(AddSeparator(city)).Append(city).ToString();
                creditorAddress.AddressLine2 = adr2_result.Length > 70 ? adr2_result.Substring(0, 70) : adr2_result;
                creditorAddress.Unstructured = true;
            }
            else
            {
                creditorAddress.ZipCode = zipCode;
                creditorAddress.CityName = city;
                creditorAddress.StreetName = adr1;
            }

            creditorAddress.CountryId = ((CountryISOCode)creditor._Country).ToString();

            return creditorAddress;
        }
      

        /// <summary>
        /// 
        /// </summary>
        public override string ExtCategoryPurpose(ISO20022PaymentTypes ISOPaymType)
        {
            return string.Empty;
        }


        /// <summary>
        /// CdtrAgt - Creditor Bank CountryId
        /// </summary>
        public override string CdtrAgtCountryId(string countryId)
        {
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Swedbank:
                case ltBank.SEB:
                case ltBank.Luminor:
                    return countryId;
                case ltBank.Standard:
                    return string.Empty;
                default: return string.Empty;
            }
        }

        /// <summary>
        /// Indicator of the urgency or order of importance that the instructing party would like the instructed party to apply to the processing of the instruction. 
        /// Valid codes
        /// NORM (Normal Instruction) - default)
        /// </summary>
        public override string InstructionPriority()
        {
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Standard:
                case ltBank.Swedbank:
                case ltBank.SEB:
                case ltBank.Luminor:
                    return BaseDocument.INSTRUCTIONPRIORITY_NORM;
                default: return BaseDocument.INSTRUCTIONPRIORITY_NORM;
            }
        }

        /// <summary>
        /// Specifies the local instrument, as published in an external local instrument code list.
        /// Allowed Codes: 
        /// ONCL (Standard Transfer)
        /// SDCL (Same-day Transfer)
        /// </summary>
        public override string ExternalLocalInstrument(string currencyCode, DateTime executionDate)
        {
            switch (CredPaymFormat.Bank)
            {
                case ltBank.Standard:
                case ltBank.SEB:
                case ltBank.Luminor:
                case ltBank.Swedbank:
                    return string.Empty;
                default: return "ONCL";
            }
        }

        /// <summary>
        /// </summary>
        public override string ExtProprietaryCode()
        {
            switch (CredPaymFormat.Bank)
            {
                case ltBank.SEB:
                case ltBank.Luminor:
                case ltBank.Swedbank:
                    return "NORM";
                default: return string.Empty;
            }
        }

        /// <summary>
        /// DOMESTIC Payment:
        /// Transfers within the same country where either sender or receiver uses BBAN. If both parts uses IBAN/SWIFT it will be a SEPA. This is country specific for Lithunia
        /// 
        /// SEPA Payment:
        /// The conditions for a SEPA payment
        /// 1.Creditor payment has currency code 'EUR'
        /// 2.Sender - Bank og Receiver-Bank has to be member of the  European Economic Area.
        /// 3.Creditor account has to be IBAN
        /// 4.Payment must be Non-urgent
        /// 
        /// CROSS BORDER Payment:
        /// 
        /// </summary>
        public override ISO20022PaymentTypes ISOPaymentType(string paymentCcy, string companyIBAN, string creditorIBAN, string creditorSWIFT, string creditorCountryId, string companyCountryId)
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
                if (SEPACountry(companyBankCountryId) && SEPACountry(creditorBankCountryId))
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
        ///This reference will be presented on Creditor’s account statement. 
        /// Max 20 characters
        /// Lithuania: Not used
        /// </summary>
        public override string RemittanceInfo(string externalAdvText, ISO20022PaymentTypes ISOPaymType, PaymentTypes paymentMethod)
        {
            return string.Empty;
        }
    }
}
