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
using UnicontaClient.Pages;
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
                case dkBank.BankData:
                    companyBankEnum = CompanyBankENUM.BankData;
                    return companyBankEnum;
                case dkBank.BEC:
                    companyBankEnum = CompanyBankENUM.BEC;
                    return companyBankEnum;
                case dkBank.SDC:
                    companyBankEnum = CompanyBankENUM.SDC;
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
                case CompanyBankENUM.BankData:
                case CompanyBankENUM.BEC:
                case CompanyBankENUM.SDC:
                    return companyBankEnum.ToString();
                case CompanyBankENUM.Handelsbanken:
                    return "Handelsbanken"; 
                default:
                    return string.Empty;
            }
        }

        public override string GenerateFileName(int fileID, int companyID)
        {
            switch (companyBankEnum)
            {
                case CompanyBankENUM.BankData:
                case CompanyBankENUM.BEC:
                case CompanyBankENUM.SDC:
                    return string.Format("{0}_{1}_{2}_{3}", "ISO20022", companyBankEnum.ToString(), fileID, companyID);
                default:
                    return string.Format("{0}_{1}_{2}", "ISO20022", fileID, companyID);
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
                case CompanyBankENUM.BankData:
                case CompanyBankENUM.BEC:
                case CompanyBankENUM.SDC:
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
                case CompanyBankENUM.BankData:
                case CompanyBankENUM.BEC:
                case CompanyBankENUM.SDC:
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
                case CompanyBankENUM.BankData:
                case CompanyBankENUM.BEC:
                case CompanyBankENUM.SDC:
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
                case CompanyBankENUM.Nordea_DK:
                    return string.Empty;
                case CompanyBankENUM.Handelsbanken:
                case CompanyBankENUM.DanskeBank:
                case CompanyBankENUM.BankData:
                case CompanyBankENUM.BEC:
                case CompanyBankENUM.SDC:
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
        public override Tuple<string, string> CreditorFIK71(String ocrLine, string creditorPaymId)
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

                paymID = BaseDocument.FIK71 + "/" + paymID;

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
        public override Tuple<string, string> CreditorFIK75(string ocrLine, string creditorPaymId)
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

                paymID = BaseDocument.FIK75 + "/" + paymID;

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

        public override PostalAddress CreditorAddress(Uniconta.DataModel.Creditor creditor, PostalAddress creditorAddress, ISO20022PaymentTypes paymentType, bool unstructured = false)
        {
            var adr1 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address1, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr2 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address2, allowedCharactersRegEx, replaceCharactersRegEx);
            var adr3 = StandardPaymentFunctions.RegularExpressionReplace(creditor._Address3, allowedCharactersRegEx, replaceCharactersRegEx);
            var zipCode = StandardPaymentFunctions.RegularExpressionReplace(creditor._ZipCode, allowedCharactersRegEx, replaceCharactersRegEx);
            var city = StandardPaymentFunctions.RegularExpressionReplace(creditor._City, allowedCharactersRegEx, replaceCharactersRegEx);

            if (paymentType != ISO20022PaymentTypes.DOMESTIC && (companyBankEnum == CompanyBankENUM.Nordea_DK || companyBankEnum == CompanyBankENUM.Nordea_NO || companyBankEnum == CompanyBankENUM.Nordea_SE))
            {
                int maxLines = 2;
                int maxStrLen = 34;

                if (paymentType == ISO20022PaymentTypes.SEPA)
                    maxLines = 3;

                var adrText = StringBuilderReuse.Create().Append(adr1).Append(adr2 != null ? ", " : null).Append(adr2).Append(adr3 != null ? ", " : null).Append(adr3). Append(zipCode != null ? ", " : null).Append(zipCode).Append(city != null ? ", " : null).Append(city).ToStringAndRelease();
                if (adrText.Length > maxLines * maxStrLen)
                    adrText = adrText.Substring(0, maxLines * maxStrLen);

                var resultList = adrText.Select((x, i) => i)
                            .Where(i => i % maxStrLen == 0)
                            .Select(i => adrText.Substring(i, adrText.Length - i >= maxStrLen ? maxStrLen : adrText.Length - i)).ToArray();

                var len = resultList.Length;
                adr1 = len > 0 ? resultList[0].Trim() : null;
                adr2 = len > 1 ? resultList[1].Trim() : null;
                adr3 = len > 2 ? resultList[2].Trim() : null;

                unstructured = true;
            }

            if (zipCode != null && !unstructured)
            {
                creditorAddress.ZipCode = zipCode;
                creditorAddress.CityName = city;
                creditorAddress.StreetName = adr1;
            }
            else
            {
                creditorAddress.AddressLine1 = adr1;
                creditorAddress.AddressLine2 = adr2;
                creditorAddress.AddressLine3 = adr3;
                creditorAddress.Unstructured = true;
            }

            creditorAddress.CountryId = ((CountryISOCode)creditor._Country).ToString();

            return creditorAddress;
        }

        /// <summary>
        /// Nordea: Reference quoted on statement. This reference will be presented on Creditor’s account statement. It may only be used for domestic payments. Only used by Norway, Denmark and Sweden.
        /// Max 20 characters
        /// </summary>
        public override string RemittanceInfo(string externalAdvText, ISO20022PaymentTypes ISOPaymType, PaymentTypes paymentMethod)
        {
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
                        if (remittanceInfo.Length > 20)
                            remittanceInfo = remittanceInfo.Substring(0, 20);
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
        /// </summary>
        public override List<string> Ustrd(string externalAdvText, ISO20022PaymentTypes ISOPaymType, CreditorTransPayment trans, bool extendedText)
        {

            var ustrdText = StandardPaymentFunctions.RegularExpressionReplace(externalAdvText, allowedCharactersRegEx, replaceCharactersRegEx);

            if (ustrdText == null)
                return null;

            //Extended notification
            if (ISOPaymType == ISO20022PaymentTypes.DOMESTIC && companyBankEnum != CompanyBankENUM.SDC) //SDC benytter den første linie i <RmtInf><Ustrd> som posteringstekst
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
                    maxLines = 10;
                    maxStrLen = 140;
                    if (trans._PaymentMethod == PaymentTypes.PaymentMethod6 || trans._PaymentMethod == PaymentTypes.PaymentMethod3) //Not allowed for FIK71 and Giro04 
                        return resultList;
                    break;

                case CompanyBankENUM.Handelsbanken:
                    maxLines = 1;
                    maxStrLen = 140;
                    if (trans._PaymentMethod == PaymentTypes.PaymentMethod6 || trans._PaymentMethod == PaymentTypes.PaymentMethod3) //Not allowed for FIK71 and Giro04 
                        return resultList;
                    break;

                case CompanyBankENUM.DanskeBank:
                    maxLines = 4;
                    maxStrLen = 35;
                    break;

                case CompanyBankENUM.BankData:
                case CompanyBankENUM.BEC:
                case CompanyBankENUM.SDC:
                    maxLines = 5;
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
