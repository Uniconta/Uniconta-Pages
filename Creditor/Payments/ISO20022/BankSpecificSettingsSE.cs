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
using UnicontaClient.Pages;
using UnicontaClient.Pages.Creditor.Payments;

namespace UnicontaISO20022CreditTransfer
{
    public class BankSpecificSettingsSE : BankSpecificSettings
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
                case seBank.Nordea:
                    companyBankEnum = CompanyBankENUM.Nordea_SE;
                    return companyBankEnum;
                case seBank.Danske_Bank:
                    companyBankEnum = CompanyBankENUM.DanskeBank_SE;
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
                case CompanyBankENUM.DanskeBank_SE:
                    return "Danske Bank";
                case CompanyBankENUM.Nordea_SE:
                    return "Nordea";
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
        /// Identifies whether a single entry per individual transaction or a batch entry for the sum of the amounts of all transactions 
        /// in the message is requested 
        /// </summary>
        public override string BatchBooking()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank_SE:
                    return CredPaymFormat.BatchBooking ? TRUE_VALUE : FALSE_VALUE;

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Date and time at which the message was created
        /// </summary>
        public override string CreDtTim()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.SEB:
                    return DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                default:
                    return DateTimeOffset.Now.ToString("o");
            }
        }

        /// <summary>
        /// Test marked payment
        /// </summary>
        public override bool TestMarked()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank_SE:
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
                case CompanyBankENUM.DanskeBank_SE:
                    return string.Format("Feedback={0}", "XDY"); //XDY is default - need a parameter if user should have the possibility to change the value
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Identification assigned by an institution.
        /// Max. 35 characters.
        /// SEB: It's not used but Danske Bank recommend to use the CVR number (Setup as Danske Bank)
        /// </summary>
        public override string IdentificationId(string identificationId, string companyCVR)
        {
            identificationId = identificationId ?? string.Empty;
            companyCVR = companyCVR ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.DanskeBank_SE:
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
                case CompanyBankENUM.DanskeBank_SE:
                    return "BANK";
                case CompanyBankENUM.Nordea_SE:
                    return "CUST"; //Nordea only accept the code CUST
                default:
                    return "BANK";
            }
        }

        public override string CdtrAgtClrSysId(ISO20022PaymentTypes ISOPaymType)
        {
            if (ISOPaymType == ISO20022PaymentTypes.DOMESTIC)
            {
                switch (companyBankEnum)
                {
                    case CompanyBankENUM.SEB:
                        return "SESBA";
                    default:
                        return null;
                }
            }

            return null;
        }

        public override string CdtrAgtMmbId(PaymentTypes paymentMethod)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.SEB:
                    if (paymentMethod == PaymentTypes.PaymentMethod3)
                        return "9900";
                    else if (paymentMethod == PaymentTypes.PaymentMethod5)
                        return "9960";
                    return null;
                default:
                    return null;
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
                case CompanyBankENUM.DanskeBank_SE:
                    return "ONCL";
                case CompanyBankENUM.SEB:
                case CompanyBankENUM.Nordea_SE:
                    return string.Empty;
                default:
                    return "ONCL";
            }
        }

        public override string DebtorIdentificationCode(string debtorId)
        {
            debtorId = debtorId ?? string.Empty;

            switch (companyBankEnum)
            {
                //Service code given by Nordea is mandatory
                case CompanyBankENUM.SEB:
                case CompanyBankENUM.Nordea_SE:
                    return debtorId;

                default:
                    return string.Empty;
            }
        }


        /// <summary>
        /// Exclude section CdtrAgt
        /// </summary>
        public override bool ExcludeSectionCdtrAgt(ISO20022PaymentTypes ISOPaymType, string creditorSWIFT)
        {
            if (companyBankEnum == CompanyBankENUM.Nordea_SE && creditorSWIFT == string.Empty)
                return true;

            return false;
        }


        /// <summary>
        /// ITS NOT ALIGNED FOR SWEDEN - WAIT FOR TEST
        /// Unambiguous identification of the BBAN account of the creditor (domestic account number).
        /// In Denmark it will be: Reg. no. + Account no. 14 char (4+10)
        /// </summary>
        public override string CreditorBBAN(string recBBAN, string credBBAN, string bic)
        {
            var regNum = string.Empty;

            var bban = recBBAN ?? string.Empty;

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
        /// Swedish BankGirot
        /// </summary>
        public override Tuple<string, string> CreditorFIK71(string ocrLine, string creditorPaymId)
        {
            ocrLine = ocrLine != null ? Regex.Replace(ocrLine, "[^0-9]", "") : string.Empty;
            creditorPaymId = creditorPaymId != null ? Regex.Replace(creditorPaymId, "[^0-9]", "") : string.Empty;

            return new Tuple<string, string>(ocrLine, creditorPaymId);
        }

        /// <summary>
        /// Swedish PlusGirot
        /// </summary>
        public override Tuple<string, string> CreditorFIK75(string ocrLine, string creditorPaymId)
        {
            ocrLine = ocrLine != null ? Regex.Replace(ocrLine, "[^0-9]", "") : string.Empty;
            creditorPaymId = creditorPaymId != null ? Regex.Replace(creditorPaymId, "[^0-9]", "") : string.Empty;

            return new Tuple<string, string>(ocrLine, creditorPaymId);
        }

        public override string OCRPaymentType(string creditorOCRPaymentId)
        {
            if (companyBankEnum == CompanyBankENUM.SEB)
                return null;

            if (!string.IsNullOrEmpty(creditorOCRPaymentId))
                return BaseDocument.OCR;

            return null;
        }

        public override string RemittanceInfo(string externalAdvText, ISO20022PaymentTypes ISOPaymType, PaymentTypes paymentMethod)
        {
            if (companyBankEnum == CompanyBankENUM.SEB)
                return string.Empty;
            
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
        /// SE: Ustrd allowed for BankGirot when there's no OCR
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
                case CompanyBankENUM.Nordea_SE:
                    if ((trans._PaymentMethod == PaymentTypes.PaymentMethod3 || (trans._PaymentMethod == PaymentTypes.PaymentMethod5)) && !string.IsNullOrEmpty(trans._PaymentId))
                        return resultList;

                    if (ISOPaymType == ISO20022PaymentTypes.DOMESTIC)
                    {
                        maxLines = 50;
                        maxStrLen = 75;
                    }
                    else if (ISOPaymType == ISO20022PaymentTypes.SEPA)
                    {
                        maxLines = 1;
                        maxStrLen = 140;
                    }
                    else
                    {
                        maxLines = 1;
                        maxStrLen = 105;
                    }
                    break;
                case CompanyBankENUM.SEB:
                    if (ISOPaymType == ISO20022PaymentTypes.DOMESTIC)
                    {
                        maxLines = 3;
                        maxStrLen = 140;
                    }
                    else
                    {
                        maxLines = 1;
                        maxStrLen = 140;
                    }
                    break;
                case CompanyBankENUM.DanskeBank_SE:
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
