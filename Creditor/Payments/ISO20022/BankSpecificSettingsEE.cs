using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages.Creditor.Payments;

namespace UnicontaISO20022CreditTransfer
{
    public class BankSpecificSettingsEE : BankSpecificSettings
    {

        #region Properties
        private CreditorPaymentFormatClientISOEE CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsEE(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISOEE credPaymFormatISOEE = new CreditorPaymentFormatClientISOEE();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISOEE);
            CredPaymFormat = credPaymFormatISOEE;
        }

        /// <summary>
        /// Banks which will be supported in EE (Estonia)
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case eeBank.Swedbank:
                    companyBankEnum = CompanyBankENUM.Swedbank;
                    return companyBankEnum;
                case eeBank.LHV_bank:
                    companyBankEnum = CompanyBankENUM.LHV_bank;
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
        /// Identification assigned by an institution.
        /// Max. 35 characters.
        /// </summary>
        public override string IdentificationId(string identificationId, string companyCVR)
        {
            identificationId = identificationId ?? string.Empty;
            companyCVR = companyCVR ?? string.Empty;

            return identificationId;
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
        /// EE: Only two Addresslines are accepted
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
        /// Specifies the local instrument, as published in an external local instrument code list.
        /// Allowed Codes: 
        /// ONCL (Standard Transfer)
        /// SDCL (Same-day Transfer)
        /// SWEDBANK: ONCL is not supported
        /// </summary>
        public override string ExternalLocalInstrument(string currencyCode, DateTime executionDate)
        {
            switch (CredPaymFormat.Bank)
            {
                case eeBank.Swedbank:
                    return "SDCL";
                default:
                    return "ONCL";
            }
        }

        /// <summary>
        /// DOMESTIC Payment:
        /// Transfers within the same country where either sender or receiver uses BBAN. If both parts uses IBAN/SWIFT it will be a SEPA. This is country specific for Estonia
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
        /// Estonia Creditor Payment Reference Number used for Domestic payments
        /// </summary>
        public override string CreditorRefNumberIBAN(String refNumber, CountryCode companyCountryId, CountryCode credCountryId)
        {
            refNumber = refNumber ?? string.Empty;

            //Domestic payment
            if (companyCountryId == CountryCode.Estonia && credCountryId == CountryCode.Estonia)
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
            if (companyCountryId == CountryCode.Estonia && credCountryId == CountryCode.Estonia)
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
    }
}
