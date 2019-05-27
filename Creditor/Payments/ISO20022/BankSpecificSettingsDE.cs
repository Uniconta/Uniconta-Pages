using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

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
            var encoding = Encoding.GetEncoding("ISO-8859-1");
            return encoding;
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
                    return true;
                default:
                    return false;
            }
        }


        /// <summary>
        /// Merged payments
        /// Bank: Deutsche_Kreditwirtschaft, underscore '_' is not allowed
        /// </summary>
        public override string PaymentInfoId(int fileSeqNumber, int recordSeqNumber)
        {
            var paymInfoId = string.Format("{0}_{1}_MERGED", fileSeqNumber.ToString().PadLeft(6, '0'), recordSeqNumber.ToString().PadLeft(6, '0'));

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                    return paymInfoId.Replace(@"_", string.Empty);
                default:
                    return paymInfoId;
            }

        }

        /// <summary>
        /// Bank: Deutsche_Kreditwirtschaft, underscore '_' is not allowed
        /// </summary>
        public override string PaymentInfoId(int fileSeqNumber, DateTime requestedExecutionDate, string paymentCurrency, string isoPaymentType, string companyPaymentMethod, PaymentTypes paymentMethod)
        {
            var paymentMethodDescription = "TYPE";

            switch (paymentMethod)
            {
                case PaymentTypes.VendorBankAccount:
                    paymentMethodDescription = "BBAN";
                    break;

                case PaymentTypes.IBAN:
                    paymentMethodDescription = "IBAN";
                    break;

                case PaymentTypes.PaymentMethod3:
                    paymentMethodDescription = "FIK71";
                    break;

                case PaymentTypes.PaymentMethod5:
                    paymentMethodDescription = "FIK75";
                    break;

                case PaymentTypes.PaymentMethod4:
                    paymentMethodDescription = "FIK73";
                    break;

                case PaymentTypes.PaymentMethod6:
                    paymentMethodDescription = "FIK04";
                    break;
            }

            var paymentInfoIdStr = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", fileSeqNumber, requestedExecutionDate.ToString("yyMMdd"), paymentCurrency, isoPaymentType, companyPaymentMethod, paymentMethodDescription);

            if (paymentInfoIdStr.Length > 35)
                paymentInfoIdStr = paymentInfoIdStr.Substring(0, 35);


            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                    return paymentInfoIdStr.Replace(@"_", string.Empty);
                default:
                    return paymentInfoIdStr;
            }
        }

        public override PostalAddress CreditorAddress(PostalAddress address)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                    return null;
                default:
                    return address;
            }
        }

        /// <summary>
        /// Bank Deutsche_Kreditwirtschaft: Only two Addresslines are accepted
        /// </summary>
        public override PostalAddress DebtorAddress(Company company, PostalAddress debtorAddress)
        {
            debtorAddress.AddressLine1 = company._Address1;
            debtorAddress.AddressLine2 = company._Address2;
            debtorAddress.AddressLine3 = company._Address3;
            debtorAddress.CountryId = ((CountryISOCode)company._CountryId).ToString();

            debtorAddress.Unstructured = true;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                    debtorAddress.AddressLine2 = company._Address3 != null ? string.Format("{0} {1}", company._Address2, company._Address3) : company._Address2;
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
        /// Deutsche_Kreditwirtschaft: Not used
        /// Other german banks: For now they have an empty value
        /// </summary>
        public override string ExternalLocalInstrument(string currencyCode, DateTime executionDate)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Deutsche_Kreditwirtschaft:
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }
    }
}
