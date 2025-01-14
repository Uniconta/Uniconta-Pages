using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Pages;
using UnicontaClient.Pages.Creditor.Payments;

namespace UnicontaISO20022CreditTransfer
{
    public class BankSpecificSettingsNL : BankSpecificSettings
    {

        #region Properties
        private CreditorPaymentFormatClientISONL CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsNL(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISONL credPaymFormatISONL = new CreditorPaymentFormatClientISONL();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISONL);
            CredPaymFormat = credPaymFormatISONL;
        }

        /// <summary>
        /// Banks which will be supported in NL
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case nlBank.Rabobank:
                    companyBankEnum = CompanyBankENUM.Rabobank;
                    return companyBankEnum;
                case nlBank.ABN_Amro_bank:
                    companyBankEnum = CompanyBankENUM.ABN_Amro_bank;
                    return companyBankEnum;
                case nlBank.ING_Bank:
                    companyBankEnum = CompanyBankENUM.ING_Bank;
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
            switch (companyBankEnum)
            {
                case CompanyBankENUM.ABN_Amro_bank:
                    return "ABN Amro Bank";
                case CompanyBankENUM.ING_Bank:
                    return "ING Bank";
                case CompanyBankENUM.Rabobank:
                    return "Rabobank";
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
            return "";
        }

        /// <summary>
        /// Country currency - used for calculation of Payment type
        /// </summary>
        public override string LocalCurrency()
        {
            return BaseDocument.CCYEUR;
        }

        /// <summary>
        /// Identifies whether a single entry per individual transaction or a batch entry for the sum of the amounts of all transactions 
        /// in the message is requested 
        /// </summary>
        public override string BatchBooking()
        {
            return CredPaymFormat.BatchBooking ? TRUE_VALUE : FALSE_VALUE;
        }


        /// <summary>
        /// Date and time at which the message was created
        /// </summary>
        public override string CreDtTim()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Rabobank:
                    return BasePage.GetSystemDefaultDate().ToString("yyyy-MM-ddTHH:mm:ss");

                default:
                    return DateTimeOffset.Now.ToString("o");
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
        /// DOMESTIC Payment:
        /// Transfers within the same country where either sender or receiver uses BBAN. If both parts uses IBAN/SWIFT it will be a SEPA. This is country specific for Netherland
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
        /// This section is not used in Netherland
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
        public override string ExtCategoryPurpose(ISO20022PaymentTypes ISOPaymType)
        {
            return string.Empty;
        }

        /// <summary>
        ///
        /// </summary>
        public override PostalAddress DebtorAddress(Company company, PostalAddress debtorAddress)
        {
            var adr1 = StandardPaymentFunctions.RegularExpressionReplace(company._Address1, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr2 = StandardPaymentFunctions.RegularExpressionReplace(company._Address2, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr3 = StandardPaymentFunctions.RegularExpressionReplace(company._Address3, allowedCharactersRegEx, replaceCharactersRegEx);

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Rabobank: //Rabobank: AdrLine can occur a maximum of 2 time(s)
                    debtorAddress.AddressLine1 = adr1;
                    debtorAddress.AddressLine2 = string.Format("{0} {1}", adr2, adr3);
                    debtorAddress.CountryId = ((CountryISOCode)company._CountryId).ToString();
                    break;

                default:
                    debtorAddress.AddressLine1 = adr1;
                    debtorAddress.AddressLine2 = adr2;
                    debtorAddress.AddressLine3 = adr3;
                    debtorAddress.CountryId = ((CountryISOCode)company._CountryId).ToString();
                    break;
            }

            debtorAddress.Unstructured = true;

            return debtorAddress;
        }


        /// <summary>
        /// Creditor Address
        /// </summary>
        public override PostalAddress CreditorAddress(Uniconta.DataModel.Creditor creditor, PostalAddress creditorAddress, ISO20022PaymentTypes paymentType, bool unstructured = false)
        {
            var adr1 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address1, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr2 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address2, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr3 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address3, allowedCharactersRegEx, replaceCharactersRegEx);
            var zipCode = StandardPaymentFunctions.RegularExpressionReplace(creditor._ZipCode, allowedCharactersRegEx, replaceCharactersRegEx);
            var city = StandardPaymentFunctions.RegularExpressionReplace(creditor._City, allowedCharactersRegEx, replaceCharactersRegEx);

            if (creditor._ZipCode != null)
            {
                creditorAddress.ZipCode = zipCode;
                creditorAddress.CityName = city;
                creditorAddress.StreetName = adr1;
            }
            else
            {
                if (companyBankEnum == CompanyBankENUM.Rabobank) //Rabobank: Only two Addresslines are accepted
                {
                    var strB = StringBuilderReuse.Create();
                    var adr1_result = strB.Append(adr1).Append(AddSeparator(adr2)).Append(adr2).ToString();
                    creditorAddress.AddressLine1 = adr1_result.Length > 70 ? adr1_result.Substring(0, 70) : adr1_result;
                    strB.Clear();
                    var adr2_result = strB.Append(adr3).Append(AddSeparator(zipCode)).Append(zipCode).Append(AddSeparator(city)).Append(city).ToString();
                    creditorAddress.AddressLine2 = adr2_result.Length > 70 ? adr2_result.Substring(0, 70) : adr2_result;
                    creditorAddress.Unstructured = true;
                    strB.Release();
                }
                else
                {
                    creditorAddress.AddressLine1 = adr1;
                    creditorAddress.AddressLine2 = adr2;
                    creditorAddress.AddressLine3 = adr3;
                    creditorAddress.Unstructured = true;
                }
            }

            creditorAddress.CountryId = ((CountryISOCode)creditor._Country).ToString();

            return creditorAddress;
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
        /// </summary>
        public override string ChargeBearer(string ISOPaymType)
        {
            ISOPaymType = ISOPaymType ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Rabobank:
                    return BaseDocument.CHRGBR_SLEV;
                case CompanyBankENUM.ING_Bank:
                    return BaseDocument.CHRGBR_SLEV;
                case CompanyBankENUM.ABN_Amro_bank:
                    return BaseDocument.CHRGBR_SLEV;
                default:
                    return BaseDocument.CHRGBR_SHAR;
            }
        }


        /// <summary>
        /// Specifies the local instrument, as published in an external local instrument code list.
        /// Allowed Codes: 
        /// ONCL (Standard Transfer)
        /// SDCL (Same-day Transfer)
        /// 
        /// ING_Bank: Not used
        /// </summary>
        public override string ExternalLocalInstrument(string currencyCode, DateTime executionDate, PaymentTypes paymentMethod, ISO20022PaymentTypes ISOPaymType)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.ING_Bank:
                    return string.Empty;
                default:
                    return "ONCL";
            }
        }

        /// <summary>
        /// CdtrAgt - Creditor Bank CountryId
        /// </summary>
        public override string CdtrAgtCountryId(string countryId)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.ABN_Amro_bank:
                    return string.Empty;
                default:
                    return countryId;
            }
        }

        /// <summary>
        /// Unstructured Remittance Information
        /// </summary>
        public override List<string> Ustrd(string externalAdvText, ISO20022PaymentTypes ISOPaymType, CreditorTransPayment trans, bool extendedText)
        {
            var ustrdText = StandardPaymentFunctions.RegularExpressionReplace(externalAdvText, allowedCharactersRegEx, replaceCharactersRegEx);
            
            int maxLines = 0;
            int maxStrLen = 0;
            List<string> resultList = new List<string>();

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Rabobank:
                    maxLines = 1;
                    maxStrLen = 140;
                    break;

                case CompanyBankENUM.ING_Bank:
                    maxLines = 1;
                    maxStrLen = 140;
                    break;

                default:
                    maxLines = 1;
                    maxStrLen = 140;
                    break;
            }

            if (ustrdText != string.Empty)
            {
                if (ustrdText.Length > maxLines * maxStrLen)
                    ustrdText = ustrdText.Substring(0, maxLines * maxStrLen);

                resultList = ustrdText.Select((x, i) => i)
                            .Where(i => i % maxStrLen == 0)
                            .Select(i => ustrdText.Substring(i, ustrdText.Length - i >= maxStrLen ? maxStrLen : ustrdText.Length - i)).ToList<string>();
            }

            return resultList;
        }
    }
}
