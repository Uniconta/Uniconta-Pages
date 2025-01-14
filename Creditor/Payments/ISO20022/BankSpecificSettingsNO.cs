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
    public class BankSpecificSettingsNO : BankSpecificSettings
    {

        #region Properties
        private CreditorPaymentFormatClientISONO CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsNO(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISONO credPaymFormatISONO = new CreditorPaymentFormatClientISONO();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISONO);
            CredPaymFormat = credPaymFormatISONO;
        }

        /// <summary>
        /// Banks which will be supported in NO
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case noBank.Nordea:
                    companyBankEnum = CompanyBankENUM.Nordea_NO;
                    return companyBankEnum;
                case noBank.Danske_Bank:
                    companyBankEnum = CompanyBankENUM.DanskeBank_NO;
                    return companyBankEnum;
                case noBank.DNB_Bank:
                    companyBankEnum = CompanyBankENUM.DNB_Bank;
                    return companyBankEnum;
                case noBank.SpareBank:
                    companyBankEnum = CompanyBankENUM.SpareBank;
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
                case CompanyBankENUM.DanskeBank_NO:
                    return "Danske Bank";
                case CompanyBankENUM.Nordea_NO:
                    return "Nordea";
                case CompanyBankENUM.DNB_Bank:
                    return "DNB Bank";
                case CompanyBankENUM.SpareBank:
                    return "SpareBank";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Country currency - used for calculation of Payment type
        /// </summary>
        public override string LocalCurrency()
        {
            return BaseDocument.CCYNOK;
        }

        /// <summary>
        /// Identifies whether a single entry per individual transaction or a batch entry for the sum of the amounts of all transactions 
        /// in the message is requested 
        /// </summary>
        public override string BatchBooking()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank_NO:
                    return CredPaymFormat.BatchBooking ? TRUE_VALUE : FALSE_VALUE;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Test marked payment
        /// </summary>
        public override bool TestMarked()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank_NO:
                    return CredPaymFormat.TestMarked;

                default:
                    return false;
            }
        }

        /// <summary>
        /// DANSKE BANK: It's only used by Danske Bank
        /// </summary>
        public override string AuthstnCodeFeedback()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank_NO:
                    return string.Format("Feedback={0}", "XDY"); //XDY is default - need a parameter if user should have the possibility to change the value
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Identification assigned by an institution.
        /// Max. 35 characters.
        /// Danske Bank: It's not used but Danske Bank recommend to use the CVR number
        /// </summary>
        public override string IdentificationId(string identificationId, string companyCVR)
        {
            identificationId = identificationId ?? string.Empty;
            companyCVR = companyCVR ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank_NO:
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
                case CompanyBankENUM.DanskeBank_NO:
                    return "BANK"; //Default value for Danske Bank Norge
                case CompanyBankENUM.Nordea_NO:
                    return "CUST"; //Nordea only accept the code CUST
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
        public override string ExternalLocalInstrument(string currencyCode, DateTime executionDate, PaymentTypes paymentMethod, ISO20022PaymentTypes ISOPaymType)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Nordea_NO:
                    if (currencyCode == BaseDocument.CCYNOK && DateTime.Now.Date == executionDate)
                        return "SDCL"; //SDCL code for same day payments
                    else
                        return string.Empty;
                case CompanyBankENUM.DanskeBank_NO:
                    return "ONCL"; //Default value for Danske Bank Norge
                case CompanyBankENUM.DNB_Bank:
                    return string.Empty; //Not used by DNB Norway
                default:
                    return "ONCL";
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
        public override string ExtCategoryPurpose(ISO20022PaymentTypes ISOPaymType)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.DNB_Bank:
                    return string.Empty; //Not used by DNB Norway
                default:
                    return BaseDocument.EXTCATEGORYPURPOSE_SUPP;
            }
        }

        /// <summary>
        /// Exclude section CdtrAgt
        /// DNB Norway: CdtrAgt isn't needed for Domestic paymenst and if included BIC or Address need to be provided.
        /// </summary>
        public override bool ExcludeSectionCdtrAgt(ISO20022PaymentTypes ISOPaymType, string creditorSWIFT)
        {
            if (companyBankEnum == CompanyBankENUM.DNB_Bank && ISOPaymType == ISO20022PaymentTypes.DOMESTIC && creditorSWIFT == string.Empty)
                return true;

            if (companyBankEnum == CompanyBankENUM.Nordea_NO && creditorSWIFT == string.Empty)
                return true;

            return false;
        }

        /// <summary>
        /// Unique and unambiguous way of identifying an organisation or an individual person.
        /// Service code is mandatory for all Norwegian banks
        /// </summary>
        public override string DebtorIdentificationCode(string debtorId)
        {
            debtorId = debtorId ?? string.Empty;

            return debtorId;
        }


        /// <summary>
        /// Unambiguous identification of the BBAN account of the creditor (domestic account number).
        /// In Norway it will be: Reg. no. + Account no. 11 char (4+7)
        /// BBAN will be retrieved from Creditor because PaymentId will KidNo
        /// </summary>
        public override string CreditorBBAN(string recBBAN, string credBBAN, string bic)
        {
            var regNum = string.Empty;

            var bban = credBBAN ?? string.Empty;

            bban = Regex.Replace(bban, "[^0-9]", "");

            if (bban != string.Empty)
            {
                regNum = bban.Substring(0, 4);
                bban = bban.Remove(0, 4);
                bban = bban.PadLeft(7, '0');
            }

            return regNum + bban;
        }


        /// <summary>
        /// Norwegian KID-number
        /// </summary>
        public override string CreditorRefNumber(String refNumber)
        {
            refNumber = refNumber ?? string.Empty;

            refNumber = Regex.Replace(refNumber, "[^0-9]", "");

            return refNumber;
        }


        /// <summary>
        /// Unstructured Remittance Information
        /// </summary>
        public override List<string> Ustrd(string externalAdvText, ISO20022PaymentTypes ISOPaymType, CreditorTransPayment trans, bool extendedText)
        {
            var ustrdText = StandardPaymentFunctions.RegularExpressionReplace(externalAdvText, allowedCharactersRegEx, replaceCharactersRegEx);

            if (ustrdText == null)
                return null;

            //Extended notification
            if (ISOPaymType == ISO20022PaymentTypes.DOMESTIC)
            {
                if (extendedText)
                {
                    if (ustrdText.Length <= 20)
                        return null;
                }
                else
                    return null;
            }

            int maxLines = 0;
            int maxStrLen = 0;
            List<string> resultList = new List<string>();

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Nordea_NO:
                    if (ISOPaymType == ISO20022PaymentTypes.DOMESTIC)
                        return resultList;

                    maxLines = 5;
                    maxStrLen = 70;
                    break;
                          
                case CompanyBankENUM.DanskeBank_NO:
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
