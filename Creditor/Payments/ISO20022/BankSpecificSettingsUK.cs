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
    public class BankSpecificSettingsUK : BankSpecificSettings
    {

        #region Properties
        private CreditorPaymentFormatClientISOUK CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsUK(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISOUK credPaymFormatISOUK = new CreditorPaymentFormatClientISOUK();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISOUK);
            CredPaymFormat = credPaymFormatISOUK;
        }

        /// <summary>
        /// Banks which will be supported in UK
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case ukBank.Danske_Bank:
                    companyBankEnum = CompanyBankENUM.DanskeBank;
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
                case CompanyBankENUM.DanskeBank:
                    return "Danske Bank";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Country currency - used for calculation of Payment type
        /// </summary>
        public override string LocalCurrency()
        {
            return BaseDocument.CCYGBP;
        }

        public override string BatchBooking()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank:
                    return CredPaymFormat.BatchBooking ? TRUE_VALUE : FALSE_VALUE;

                default:
                    return string.Empty;
            }
        }


        /// <summary>
        /// Identification assigned by an institution.
        /// Max. 35 characters.
        /// NORDEA CUST: Customer identification Signer Id as agreed with (or assigned by) Nordea, min. 10 and max. 18 characters.
        /// Danske Bank: It's not used but Danske Bank recommend to use the CVR number
        /// </summary>
        public override string IdentificationId(String identificationId, string companyCVR)
        {
            identificationId = identificationId ?? string.Empty;
            companyCVR = companyCVR ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank:
                    identificationId = companyCVR.Replace(" ", String.Empty);
                    return identificationId;
                default:
                    return identificationId;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public override string ExtCategoryPurpose(ISO20022PaymentTypes ISOPaymType)
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
        /// UK:
        /// Danske Bank - not used
        ///
        /// /// This Charge bearer is per payment
        /// </summary>
        public override string ChargeBearer(string ISOPaymType)
        {
            return string.Empty; 
        }

        /// <summary>
        /// Specifies the local instrument, as published in an external local instrument code list.
        /// Allowed Codes: 
        /// ONCL (Standard Transfer)
        /// SDCL (Same-day Transfer)
        /// </summary>
        public override string ExternalLocalInstrument(string currencyCode, DateTime executionDate, PaymentTypes paymentMethod, ISO20022PaymentTypes ISOPaymType)
        {
            return string.Empty;
        }

        /// <summary>
        /// This reference will be presented on Creditor’s account statement. 
        /// UK Danske bank Max 18 characters payment types 'Fast transfer' and 'Internal payment'
        /// </summary>
        public override string RemittanceInfo(string externalAdvText, ISO20022PaymentTypes ISOPaymType, PaymentTypes paymentMethod)
        {
            string remittanceInfo = StandardPaymentFunctions.RegularExpressionReplace(externalAdvText, allowedCharactersRegEx, replaceCharactersRegEx);

            if (remittanceInfo != string.Empty && remittanceInfo.Length > 18)
                remittanceInfo = remittanceInfo.Substring(0, 18);

            return remittanceInfo;
        }

        /// <summary>
        /// Unstructured Remittance Information
        /// UK Danske bank not used for payment types 'Fast transfer' and 'Internal payment'
        /// </summary>
        public override List<string> Ustrd(string externalAdvText, ISO20022PaymentTypes ISOPaymType, CreditorTransPayment trans, bool extendedText)
        {
            List<string> resultList = new List<string>();
            return resultList;
        }
    }
}
