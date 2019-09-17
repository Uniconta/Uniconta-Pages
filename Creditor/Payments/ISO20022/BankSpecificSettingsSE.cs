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
    class BankSpecificSettingsSE : BankSpecificSettings
    {

        #region Properties
        private CreditorPaymentFormatClientISOSE CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsSE(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISOSE credPaymFormatISOSE = new CreditorPaymentFormatClientISOSE();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISOSE);
            CredPaymFormat = credPaymFormatISOSE;
        }

        /// <summary>
        /// Banks which will be supported in SE
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case seBank.SEB:
                    companyBankEnum = CompanyBankENUM.SEB;
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
                case CompanyBankENUM.SEB:
                    return "SEB";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Country currency - used for calculation of Payment type
        /// </summary>
        public override string LocalCurrency()
        {
            return BaseDocument.CCYSEK;
        }


        /// <summary>
        /// Identification assigned by an institution.
        /// Max. 35 characters.
        /// SEB: It's not used but Danske Bank recommend to use the CVR number (Setup as Danske Bank)
        /// </summary>
        public override string IdentificationId(String identificationId, string companyCVR)
        {
            identificationId = identificationId ?? string.Empty;
            companyCVR = companyCVR ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.SEB:
                    identificationId = companyCVR.Replace(" ", String.Empty);
                    return identificationId;
                default:
                    return identificationId;
            }
        }


        /// <summary>
        /// Name of the identification scheme, in a coded form as published in an external list
        /// Valid codes: CUST and BANK
        /// Max. 35 characters.
        /// </summary>
        public override string IdentificationCode()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.SEB:
                    return "BANK"; //Default value for SEB
                default:
                    return "BANK";
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
            switch (companyBankEnum)
            {
                case CompanyBankENUM.SEB:
                    return "ONCL"; //Default value for SEB
                default:
                    return "ONCL";
            }
        }

        /// <summary>
        /// ITS NOT ALIGNED FOR SWEDEN - WAIT FOR TEST
        /// Unambiguous identification of the BBAN account of the creditor (domestic account number).
        /// In Denmark it will be: Reg. no. + Account no. 14 char (4+10)
        /// </summary>
        public override string CreditorBBAN(String recBBAN, String credBBAN)
        {
            var bban = string.Empty;
            var regNum = string.Empty;

            bban = recBBAN ?? string.Empty;

            bban = Regex.Replace(bban, "[^0-9]", "");

            if (bban != string.Empty)
            {
                regNum = bban.Substring(0, 4);
                bban = bban.Remove(0, 4);
                bban = bban.PadLeft(10, '0');
            }

            return regNum + bban;
        }


        /// <summary>
        /// Unstructured Remittance Information
        /// </summary>
        public override List<string> Ustrd(string externalAdvText, ISO20022PaymentTypes ISOPaymType, PaymentTypes paymentMethod)
         {
            var ustrdText = StandardPaymentFunctions.RegularExpressionReplace(externalAdvText, allowedCharactersRegEx, replaceCharactersRegEx);

            int maxLines = 0;
            int maxStrLen = 0;
            List<string> resultList = new List<string>();

            switch (companyBankEnum)
            {
                case CompanyBankENUM.SEB:  //Setup as Danske Bank
                    maxLines = 4;
                    maxStrLen = 35;
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
