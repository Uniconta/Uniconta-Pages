using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Pages.Creditor.Payments;

namespace UnicontaISO20022CreditTransfer
{
    public class BankSpecificSettingsDK : BankSpecificSettings
    {

        #region Properties
        private CreditorPaymentFormatClientISODK CredPaymFormat { get; set; }
        #endregion

        public BankSpecificSettingsDK(CreditorPaymentFormat credPaymFormat)
        {
            CreditorPaymentFormatClientISODK credPaymFormatISODK = new CreditorPaymentFormatClientISODK();
            StreamingManager.Copy(credPaymFormat, credPaymFormatISODK);
            CredPaymFormat = credPaymFormatISODK;
        }

        /// <summary>
        /// Banks which will be supported in DK
        /// </summary>
        public override CompanyBankENUM CompanyBank()
        {
            switch (CredPaymFormat.Bank)
            {
                case dkBank.Danske_Bank:
                    companyBankEnum = CompanyBankENUM.DanskeBank;
                    return companyBankEnum;
                case dkBank.Nordea:
                    companyBankEnum = CompanyBankENUM.Nordea_DK;
                    return companyBankEnum;
                case dkBank.BankConnect:
                    companyBankEnum = CompanyBankENUM.BankConnect;
                    return companyBankEnum;
                case dkBank.Handelsbanken:
                    companyBankEnum = CompanyBankENUM.Handelsbanken;
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
                case CompanyBankENUM.Nordea_DK:
                    return "Nordea";
                case CompanyBankENUM.DanskeBank:
                    return "Danske Bank";
                case CompanyBankENUM.BankConnect:
                    return "Bank Connect"; // CredPaymFormat.BankCentral.ToString();
                case CompanyBankENUM.Handelsbanken:
                    return "Handelsbanken"; 
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Country currency - used for calculation of Payment type
        /// </summary>
        public override string LocalCurrency()
        {
            return BaseDocument.CCYDKK;
        }

        /// <summary>
        /// Allowed characters
        /// </summary>
        public override void AllowedCharactersRegEx(bool internationalPayment = false)
        {
            if (internationalPayment)
                allowedCharactersRegEx = "[^a-zA-Z0-9 -?:().,'+/]";
            else
                allowedCharactersRegEx = "[^a-zA-Z0-9æøåÆØÅ &-?:().,'+/]";

        }

        /// <summary>
        /// Date and time at which the message was created
        /// </summary>
        public override string CreDtTim()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Handelsbanken:
                    return DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
                default:
                    return DateTimeOffset.Now.ToString("o");
            }
        }

        /// <summary>
        /// Identifies whether a single entry per individual transaction or a batch entry for the sum of the amounts of all transactions 
        /// in the message is requested 
        /// </summary>
        public override string BatchBooking()
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.Nordea_DK:
                case CompanyBankENUM.Nordea_NO:
                case CompanyBankENUM.DanskeBank:
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
                    case CompanyBankENUM.DanskeBank:
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
                case CompanyBankENUM.DanskeBank:
                    return string.Format("Feedback={0}", "XDY"); //XDY is default - need a parameter if user should have the possibility to change the value
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
        public override string IdentificationId(string identificationId, string companyCVR)
        {
            identificationId = identificationId ?? string.Empty;
            companyCVR = companyCVR ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Nordea_DK:
                case CompanyBankENUM.Handelsbanken: //Its called SHB no. for Handelsbanken
                    return identificationId; 
                case CompanyBankENUM.DanskeBank:
                    identificationId = companyCVR.Replace(" ", String.Empty);
                    return identificationId;
                case CompanyBankENUM.BankConnect:
                    return identificationId; //Pt. ukendt - Kode skal sandsynligvis aftale med Banken
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
                case CompanyBankENUM.BankConnect:
                case CompanyBankENUM.Nordea_DK:
                    return "CUST"; //Nordea only accept the code CUST
                case CompanyBankENUM.Handelsbanken:
                case CompanyBankENUM.DanskeBank:
                    return "BANK"; //Default value for Danske Bank
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
                case CompanyBankENUM.BankConnect:
                case CompanyBankENUM.Nordea_DK:
                    return string.Empty;
                case CompanyBankENUM.Handelsbanken:
                case CompanyBankENUM.DanskeBank:
                    return "ONCL"; //Default value for Danske Bank
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
                case CompanyBankENUM.Nordea_DK:
                    return debtorId;

                default:
                    return string.Empty;
            }
        }


        /// <summary>
        /// Unambiguous identification of the BBAN account of the creditor (domestic account number).
        /// In Denmark it will be: Reg. no. + Account no. 14 char (4+10)
        /// </summary>
        public override string CreditorBBAN(string recBBAN, string credBBAN, string bic)
        {
            string credbankCountryId = null;
            if (bic != null && bic.Length > 6)
                credbankCountryId = bic.Substring(4, 2);

            var regNum = string.Empty;
            var bban = recBBAN ?? string.Empty;

            bban = Regex.Replace(bban, "[^0-9]", "");

            if (bban != string.Empty)
            {
                if (credbankCountryId == null || credbankCountryId == "DK")
                {
                    regNum = bban.Substring(0, 4);
                    bban = bban.Remove(0, 4);
                    bban = bban.PadLeft(10, '0');
                }
            }

            return regNum + bban;
        }

        /// <summary>
        /// Danish Inpayment Form FIK04
        /// </summary>
        public override Tuple<string, string> CreditorFIK04(string ocrLine)
        {
            ocrLine = ocrLine ?? string.Empty;

            string paymID = string.Empty;
            string creditorAccount = string.Empty;

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+04<", "");
            ocrLine = ocrLine.Replace(">04<", "");
            ocrLine = ocrLine.Replace("<", "");
            int index = ocrLine.IndexOf("+");
            if (index > 0)
            {
                paymID = ocrLine.Substring(0, index);

                if (paymID.Length > BaseDocument.FIK04LENGTH)
                    paymID = paymID.Substring(0, BaseDocument.FIK04LENGTH);
                else
                    paymID = paymID.PadLeft(BaseDocument.FIK04LENGTH, '0');

                creditorAccount = ocrLine.Remove(0, index + 1);
            }

            return new Tuple<string, string>(paymID, creditorAccount);
        }

        /// <summary>
        /// Danish Inpayment Form FIK71
        /// </summary>
        public override Tuple<string, string> CreditorFIK71(String ocrLine)
        {
            ocrLine = ocrLine ?? string.Empty;

            string paymID = string.Empty;
            string creditorAccount = string.Empty;

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+71<", "");
            ocrLine = ocrLine.Replace(">71<", "");
            ocrLine = ocrLine.Replace("<", "");
            int index = ocrLine.IndexOf("+");
            if (index > 0)
            {
                paymID = ocrLine.Substring(0, index);

                if (paymID.Length > BaseDocument.FIK71LENGTH)
                    paymID = paymID.Substring(0, BaseDocument.FIK71LENGTH);
                else
                    paymID = paymID.PadLeft(BaseDocument.FIK71LENGTH, '0');

                creditorAccount = ocrLine.Remove(0, index + 1);
            }

            return new Tuple<string, string>(paymID, creditorAccount);
        }

        /// <summary>
        /// Danish Inpayment Form FIK73
        /// </summary>
        public override string CreditorFIK73(String ocrLine)
        {
            ocrLine = ocrLine ?? string.Empty;

            string creditorAccount = string.Empty;

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+73<", "");
            ocrLine = ocrLine.Replace(">73<", "");
            ocrLine = ocrLine.Replace("<", "");
            creditorAccount = ocrLine.Replace("+", "");

            return creditorAccount;
        }

        /// <summary>
        /// Danish Inpayment Form FIK75
        /// </summary>
        public override Tuple<string, string> CreditorFIK75(string ocrLine)
        {
            ocrLine = ocrLine ?? string.Empty;

            string paymID = string.Empty;
            string creditorAccount = string.Empty;

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+75<", "");
            ocrLine = ocrLine.Replace(">75<", "");
            ocrLine = ocrLine.Replace("<", "");
            int index = ocrLine.IndexOf("+");
            if (index > 0)
            {
                paymID = ocrLine.Substring(0, index);

                if (paymID.Length > BaseDocument.FIK75LENGTH)
                    paymID = paymID.Substring(0, BaseDocument.FIK75LENGTH);
                else
                    paymID = paymID.PadLeft(BaseDocument.FIK75LENGTH, '0');

                creditorAccount = ocrLine.Remove(0, index + 1);
            }

            return new Tuple<string, string>(paymID, creditorAccount);
        }


        /// <summary>
        /// Valid codes:
        /// CRED (Creditor)
        /// DEBT (Debtor)
        /// SHAR (Shared)
        /// SLEV (Service Level)
        /// 
        /// </summary>
        public override string ChargeBearer(string ISOPaymType)
        {
            ISOPaymType = ISOPaymType ?? string.Empty;

            switch (companyBankEnum)
            {
                case CompanyBankENUM.Nordea_NO:
                case CompanyBankENUM.Nordea_DK:
                    switch (ISOPaymType)
                    {
                        case "DOMESTIC":
                            return BaseDocument.CHRGBR_SHAR;
                        case "CROSSBORDER":
                            return BaseDocument.CHRGBR_SHAR;
                        case "SEPA":
                            return BaseDocument.CHRGBR_SLEV;
                        default:
                            return BaseDocument.CHRGBR_SHAR;
                    }
                default:
                    switch (ISOPaymType)
                    {
                        case "DOMESTIC":
                            return BaseDocument.CHRGBR_SHAR;
                        case "CROSSBORDER":
                            return BaseDocument.CHRGBR_SHAR;
                        case "SEPA":
                            return BaseDocument.CHRGBR_SLEV;
                        default:
                            return BaseDocument.CHRGBR_SHAR;
                    }
            }
        }

        public override PostalAddress CreditorAddress(Uniconta.DataModel.Creditor creditor, PostalAddress creditorAddress, bool unstructured = false)
        {
            if (paymentType != ISO20022PaymentTypes.DOMESTIC && (companyBankEnum == CompanyBankENUM.Nordea_DK || companyBankEnum == CompanyBankENUM.Nordea_NO))
            {
                int maxLines = 3;
                int maxStrLen = 34;

                string adrText = string.Concat(creditor._Address1, " ", creditor._Address2, " ", creditor._Address3, " ", creditor._ZipCode, " ", creditor._City);

                if (adrText.Length > maxLines * maxStrLen)
                    adrText = adrText.Substring(0, maxLines * maxStrLen);

                var resultList = adrText.Select((x, i) => i)
                            .Where(i => i % maxStrLen == 0)
                            .Select(i => adrText.Substring(i, adrText.Length - i >= maxStrLen ? maxStrLen : adrText.Length - i)).ToArray();

                var len = resultList.Length;
                creditor._Address1 = len > 0 ? resultList[0].Trim() : null;
                creditor._Address2 = len > 1 ? resultList[1].Trim() : null;
                creditor._Address3 = len > 2 ? resultList[2].Trim() : null;

                unstructured = true;
            }

            return base.CreditorAddress(creditor, creditorAddress, unstructured);
        }

        /// <summary>
        /// Unstructured Remittance Information
        /// </summary>
        public override List<string> Ustrd(string externalAdvText, ISO20022PaymentTypes ISOPaymType, PaymentTypes paymentMethod, bool extendedText)
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
                case CompanyBankENUM.Nordea_DK:
                    if (ISOPaymType == ISO20022PaymentTypes.DOMESTIC)
                        return resultList;

                    maxLines = 4;
                    maxStrLen = 35;
                    if (paymentMethod == PaymentTypes.PaymentMethod6 || paymentMethod == PaymentTypes.PaymentMethod3) //Not allowed for FIK71 and Giro04 
                        return resultList;
                    break;

                case CompanyBankENUM.Handelsbanken:
                    maxLines = 1;
                    maxStrLen = 140;
                    if (paymentMethod == PaymentTypes.PaymentMethod6 || paymentMethod == PaymentTypes.PaymentMethod3) //Not allowed for FIK71 and Giro04 
                        return resultList;
                    break;

                case CompanyBankENUM.DanskeBank:
                    maxLines = 4;
                    maxStrLen = 35;
                    break;

                case CompanyBankENUM.BankConnect:
                    maxLines = 41;
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

        /// <summary>
        /// Exclude section CdtrAgt
        /// Nordea: CdtrAgt is only allowed when BIC is included.
        /// </summary>
        public override bool ExcludeSectionCdtrAgt(ISO20022PaymentTypes ISOPaymType, string creditorSWIFT)
        {
            if (companyBankEnum == CompanyBankENUM.Nordea_DK && creditorSWIFT == string.Empty)
                return true;

            return false;
        }
    }
}
