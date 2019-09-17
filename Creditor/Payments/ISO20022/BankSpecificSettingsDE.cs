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
    class BankSpecificSettingsDE : BankSpecificSettings
    {
        #region Properties
        private CreditorPaymentFormatClientISODE CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsDE(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISODE credPaymFormatISODE = new CreditorPaymentFormatClientISODE();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISODE);
            CredPaymFormat = credPaymFormatISODE;
        }

        /// <summary>
        /// Banks which will be supported in DE
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case deBank.StarMoney_SFirm:
                    companyBankEnum = CompanyBankENUM.StarMoney_SFirm;
                    return companyBankEnum;
                case deBank.Deutsche_Kreditwirtschaft:
                    companyBankEnum = CompanyBankENUM.Deutsche_Kreditwirtschaft;
                    return companyBankEnum;
                case deBank.Volks_Raiffeisenbanken:
                    companyBankEnum = CompanyBankENUM.Volks_Raiffeisenbanken;
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
                case CompanyBankENUM.StarMoney_SFirm:
                    return "StarMoney/SFirm";
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return String.Empty;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Country currency - used for calculation of Payment type
        /// </summary>
        public override string LocalCurrency()
        {
            return BaseDocument.CCYEUR;
        }

        /// <summary>
        /// character set (Encoding)
        /// </summary>
        public override Encoding EncodingFormat()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return Encoding.UTF8;
                default:
                    return Encoding.GetEncoding("ISO-8859-1"); ;
            }
          
        }

        /// <summary>
        /// Allowed characters
        /// </summary>
        public override string AllowedCharactersRegEx()
        {
            allowedCharactersRegEx = "[^a-zA-Z0-9äöüÄÖÜß -?:().,'+/]";
            return allowedCharactersRegEx;
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
        /// Identification assigned by an institution.
        /// Max. 35 characters.
        /// </summary>
        public override string IdentificationId(String identificationId, string companyCVR)
        {
            identificationId = identificationId ?? string.Empty;
            companyCVR = companyCVR ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                   return identificationId; 
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Total of all individual amounts incluede in the message, irrespective of currencies
        /// </summary>
        public override double HeaderCtrlSum(double amount)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return amount;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Activate Total of all individual amounts per PaymentInfoId, irrespective of currencies
        /// </summary>
        public override bool PmtInfCtrlSumActive()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Activate number of transactions of all individual amounts per PaymentInfoId
        /// </summary>
        public override bool PmtInfNumberOfTransActive()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return true;
                default:
                    return false;
            }
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
            ISOPaymType = ISOPaymType ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return string.Empty;
                default:
                    switch (ISOPaymType)
                    {
                        case "DOMESTIC":
                            return BaseDocument.CHRGBR_SHAR;
                        case "CROSSBORDER":
                            return BaseDocument.CHRGBR_DEBT;
                        case "SEPA":
                            return BaseDocument.CHRGBR_SLEV;
                        default:
                            return BaseDocument.CHRGBR_SHAR;
                    }
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
        public override string ChargeBearerDebtor()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return BaseDocument.CHRGBR_SLEV;
                default:
                    return string.Empty;
            }
        }
      

        public override PostalAddress CreditorAddress(Uniconta.DataModel.Creditor creditor, PostalAddress creditorAddress)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return null;
                default:
                    return base.CreditorAddress(creditor, creditorAddress); //TODO:TEST DENNE
            }
        }

        /// <summary>
        /// Bank Deutsche_Kreditwirtschaft and Volks_Raiffeisenbanken: Only two Addresslines are accepted
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

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    debtorAddress = null;
                    break;
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                    debtorAddress.AddressLine2 = adr3 != null ? string.Format("{0} {1}", adr2, adr3) : adr2;
                    debtorAddress.AddressLine3 = null;
                    break;
            }

            return debtorAddress;
        }


        /// <summary>
        /// CdtrAgt - Creditor Bank CountryId
        /// </summary>
        public override string CdtrAgtCountryId(string countryId)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return string.Empty;
                default:
                    return countryId;
            }
        }

        /// <summary>
        /// Specifies the local instrument, as published in an external local instrument code list.
        /// Allowed Codes: 
        /// ONCL (Standard Transfer)
        /// SDCL (Same-day Transfer)
        /// 
        /// Deutsche_Kreditwirtschaft and Volks_Raiffeisenbanken: Not used
        /// Other german banks: For now they have an empty value
        /// </summary>
        public override string ExternalLocalInstrument(string currencyCode, DateTime executionDate)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                case CompanyBankENUM.Volks_Raiffeisenbanken:
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }
    }
}
