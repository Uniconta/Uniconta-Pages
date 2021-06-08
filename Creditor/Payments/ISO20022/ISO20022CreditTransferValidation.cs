using System;
using System.Collections.Generic;
using System.Xml;

using Uniconta;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaAPI;
using Uniconta.ClientTools.DataModel;
using System.Text.RegularExpressions;
using UnicontaISO20022CreditTransfer;
using System.Text;
using UnicontaClient.Pages;
using System.Windows;
using Uniconta.ClientTools.Page;
using UnicontaClient.Pages.Creditor.Payments;
using Uniconta.Common.Utility;

namespace ISO20022CreditTransfer
{
    /// <summary>
    /// Class with static methods for creating ISO 20022 Credit Transfer document.
    /// </summary>
    public class PaymentISO20022Validate
    {
        #region Variables
        private CompanyBankENUM companyBankEnum;
        private BankSpecificSettings bankSpecificSettings;
        private CreditorPaymentFormat credPaymFormat;
        private ExportFormatType exportFormat;
        private string companyCountryId;

        private bool glJournalGenerated;
        private ISO20022PaymentTypes isoPaymentType;

        protected string creditorIBAN = string.Empty;
        protected string creditorSWIFT = string.Empty;


        List<CheckError> checkErrors = new List<CheckError>();
        #endregion

        #region Properties

        private List<CheckError> CheckError
        {
            get
            {
                return checkErrors;
            }

            set
            {
                checkErrors = value;
            }
        }

        private BankSpecificSettings BankSpecificSettings
        {
            get
            {
                return bankSpecificSettings;
            }

            set
            {
                bankSpecificSettings = value;
            }
        }

        /// <summary>
        /// Company bank
        /// </summary>
        /// 
        private CompanyBankENUM CompanyBankEnum
        {
            get
            {
                return companyBankEnum;
            }

            set
            {
                companyBankEnum = value;
            }
        }

        private CreditorPaymentFormat CredPaymFormat
        {
            get
            {
                return credPaymFormat;
            }

            set
            {
                credPaymFormat = value;
            }
        }

        #endregion


        static public string UnicontaCountryToISO(CountryCode code)
        {
            return ((CountryISOCode)code).ToString();
        }

        public void CompanyBank(CreditorPaymentFormat credPaymFormat)
        {
            BankSpecificSettings bankSpecific = BankSpecificSettings.BankSpecTypeInstance(credPaymFormat);
            this.bankSpecificSettings = bankSpecific;
            this.credPaymFormat = credPaymFormat;

            CompanyBankEnum = bankSpecific.CompanyBank();
        }

        /// <summary>
        /// Validate proposed payment records before generating the payment file in the XML format Credit Transfer ISO20022 pain003.
        /// </summary>
        /// <param name="xxx">xxx.</param> 
        /// <param name="xxx">xxx.</param>
        /// <returns>An XML payment file</returns>
        public XMLDocumentGenerateResult ValidateISO20022(Company company, UnicontaClient.Pages.CreditorTransPayment trans, SQLCache bankAccountCache, bool journalGenerated = false)
        {
            XmlDocument dummyDoc = new XmlDocument();

            CreditTransferDocument doc = new CreditTransferDocument();

            var bankAccount = (BankStatement)bankAccountCache.Get(credPaymFormat._BankAccount);
            var credCache = company.GetCache(typeof(Uniconta.DataModel.Creditor));
            var creditor = (Creditor)credCache.Get(trans.Account);

            exportFormat = (ExportFormatType)credPaymFormat._ExportFormat;
            glJournalGenerated = journalGenerated;
            companyCountryId = UnicontaCountryToISO(company._CountryId);

            creditorIBAN = string.Empty;
            creditorSWIFT = string.Empty;

            checkErrors.Clear();

            //Validations >>

            RequestedExecutionDate(trans._PaymentDate, company);
            PaymentCurrency(trans.CurrencyLocalStr, trans._PaymentMethod, trans._PaymentId, trans.SWIFT);

            if(glJournalGenerated == false)
                CreditorAddress(creditor);

            if (trans.PaymentMethod == null)
                checkErrors.Add(new CheckError(fieldCannotBeEmpty("PaymentMethod" )));
            else if (trans._PaymentMethod == PaymentTypes.None)
                checkErrors.Add(new CheckError(string.Concat(Uniconta.ClientTools.Localization.lookup("PaymentMethod"), " = ", trans.PaymentMethod)));

            if (trans.MergePaymId == Uniconta.ClientTools.Localization.lookup("Excluded") && trans.PaymentAmount <= 0)
                checkErrors.Add(new CheckError(string.Concat(Uniconta.ClientTools.Localization.lookup("PaymentAmount"), "< 0")));

            switch (trans._PaymentMethod)
            {
                case PaymentTypes.VendorBankAccount:
                    PaymentMethodBBAN(creditor, company, trans._PaymentId);
                    CreditorBIC(trans._SWIFT, true);
                    break;

                case PaymentTypes.IBAN:
                     PaymentMethodIBAN(creditor, company, trans._PaymentId);
                     CreditorBIC(trans._SWIFT);
                    break;

                case PaymentTypes.PaymentMethod3: //FIK71
                    PaymentMethodFIK71(trans._PaymentId);
                    break;

                case PaymentTypes.PaymentMethod4: //FIK73
                    PaymentMethodFIK73(trans._PaymentId);
                    break;

                case PaymentTypes.PaymentMethod5: //FIK75
                    PaymentMethodFIK75(trans._PaymentId);
                    break;

                case PaymentTypes.PaymentMethod6: //FIK04
                    PaymentMethodFIK04(trans._PaymentId);
                    break;
            }

            var countryCode = glJournalGenerated ? (CountryCode)company._Country : creditor._Country;

            ISOPaymentType(trans.CurrencyLocalStr, bankAccount._IBAN, trans._PaymentMethod, UnicontaCountryToISO(countryCode));
            
            //Validations <<

            return new XMLDocumentGenerateResult(dummyDoc, CheckError.Count > 0, 0, CheckError);
        }
       

        /// <summary>
        /// Validate PaymentType
        /// </summary>
        private void ISOPaymentType(string paymentCcy, string companyIBAN, PaymentTypes paymentType, string creditorCountryId)
        {
            companyIBAN = companyIBAN ?? string.Empty;
            companyCountryId = companyCountryId ?? string.Empty;
            creditorCountryId = creditorCountryId ?? string.Empty;

            isoPaymentType = BankSpecificSettings.ISOPaymentType(paymentCcy, companyIBAN, creditorIBAN, creditorSWIFT, creditorCountryId, companyCountryId);

            if (credPaymFormat._ExportFormat == (byte)ExportFormatType.ISO20022_DK && (CompanyBankEnum == CompanyBankENUM.DanskeBank || CompanyBankEnum == CompanyBankENUM.Nordea_DK || CompanyBankEnum == CompanyBankENUM.Nordea_NO))
            {
                if (paymentType != PaymentTypes.IBAN && isoPaymentType == ISO20022PaymentTypes.DOMESTIC && paymentCcy == BaseDocument.CCYEUR)
                    checkErrors.Add(new CheckError(String.Format("It's required to use IBAN as creditor account for Domestic EUR payments.")));

                //Nordea
                if ((CompanyBankEnum == CompanyBankENUM.Nordea_DK || CompanyBankEnum == CompanyBankENUM.Nordea_NO) && (paymentType != PaymentTypes.IBAN && isoPaymentType == ISO20022PaymentTypes.SEPA))
                    checkErrors.Add(new CheckError(String.Format("It's required to use IBAN as creditor account for SEPA payments.")));
            }
            else if (credPaymFormat._ExportFormat == (byte)ExportFormatType.ISO20022_DK)
            {
                if (paymentType == PaymentTypes.IBAN && isoPaymentType == ISO20022PaymentTypes.DOMESTIC) //Not sure which banks has this requirement.
                    checkErrors.Add(new CheckError(String.Format("It's not allowed to use IBAN as creditor account for domestic payments.")));
            }

            if (credPaymFormat._ExportFormat == (byte)ExportFormatType.ISO20022_LT && bankSpecificSettings.CompanyBankEnum == CompanyBankENUM.Standard)
            {
                if (isoPaymentType != ISO20022PaymentTypes.SEPA)
                    checkErrors.Add(new CheckError(String.Format("Only SEPA payments are allowed")));
            }
        }

        static string fieldCannotBeEmpty(string field)
        {
            return String.Format("{0} : {1}",
                    Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"),
                    Uniconta.ClientTools.Localization.lookup(field));
        }


        /// <summary>
        /// Validate RequestedExecutionDate
        /// </summary>
        private void RequestedExecutionDate(DateTime? executionDate, Company company)
        {
            if (executionDate.HasValue == false)
            {
                checkErrors.Add(new CheckError(fieldCannotBeEmpty("PaymentDate")));
            }
            else if (executionDate < BasePage.GetSystemDefaultDate().Date)
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentDateLessThanToday")));
            }
            else if (!Uniconta.DirectDebitPayment.Common.IsBankDay(company._CountryId, executionDate.Value))
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentDateNotABankday")));
            }
        }

        /// <summary>
        /// Validate Payment currency
        /// </summary>
        private void PaymentCurrency(string paymentCcy, PaymentTypes paymentType, string paymentId, string swift)
        {
            if (exportFormat == ExportFormatType.DanskeBank_CSV && PaymentTypes.VendorBankAccount == paymentType)
            {
                var countryId = string.Empty;
                if (!string.IsNullOrEmpty(swift))
                {
                    swift = Regex.Replace(swift, "[^\\w\\d]", "");
                    if (swift.Length > 6)
                        countryId = countryId == string.Empty ? swift.Substring(4, 2).ToUpper() : countryId;
                }

                if (paymentCcy != BaseDocument.CCYDKK && paymentCcy != BaseDocument.CCYEUR && (countryId == string.Empty || countryId == BaseDocument.COUNTRY_DK))
                    checkErrors.Add(new CheckError(String.Format("Only currency DKK and EUR is allowed for domestic payments")));
            }

            if (exportFormat == ExportFormatType.Nordea_CSV || exportFormat == ExportFormatType.BEC_CSV)
            {
                var countryId = string.Empty;
                if (paymentType == PaymentTypes.IBAN)
                {
                    if (! string.IsNullOrEmpty(paymentId))
                    {
                        paymentId = Regex.Replace(paymentId, "[^\\w\\d]", "");
                        countryId = paymentId.Length < 2 ? string.Empty : paymentId.Substring(0, 2).ToUpper();
                    }
                }

                if (! string.IsNullOrEmpty(swift))
                {
                    swift = Regex.Replace(swift, "[^\\w\\d]", "");
                    if (swift.Length > 6)
                        countryId = countryId == string.Empty ? swift.Substring(4, 2).ToUpper() : countryId;
                }

                if (paymentType == PaymentTypes.VendorBankAccount && paymentCcy != BaseDocument.CCYDKK && (countryId == string.Empty || countryId == BaseDocument.COUNTRY_DK))
                    checkErrors.Add(new CheckError(String.Format("Only currency DKK is allowed for domestic payments")));
            }
        }

     
        /// <summary>
        /// Validate Creditor address
        /// </summary>
        private void CreditorAddress(Creditor creditor)
        {
            string countryId = UnicontaCountryToISO(creditor._Country);
            if (string.IsNullOrEmpty(countryId))
            {
                var string1 = fieldCannotBeEmpty("Country");
                var string2 = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Creditor"), creditor._Account);
                checkErrors.Add(new CheckError(String.Format("{0}, {1}", string1, string2)));
            }
        }

        /// <summary>
        /// Validate Creditor SWIFT
        /// </summary>
        private void CreditorBIC(String swift, bool bbanSwift = false)
        {
            if (! string.IsNullOrEmpty(swift))
            {
                swift = Regex.Replace(swift, "[^\\w\\d]", "");

                creditorSWIFT = swift;

                if (!StandardPaymentFunctions.ValidateBIC(creditorSWIFT))
                {
                    checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("ErrorSWIFTAddress")));
                }
                else
                {
                    string countryId = string.Empty;
                    if (creditorSWIFT.Length >= 6)
                        countryId = creditorSWIFT.Substring(4, 2);
                    
                    if (glJournalGenerated && countryId != BaseDocument.COUNTRY_DK)
                        checkErrors.Add(new CheckError(String.Format("Creditor information is required for foreign payments and they're not available.")));
                }
            }
            else
            {
                if(!bbanSwift)
                    checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("ErrorSWIFTAddress")));
            }
        }


        /// <summary>
        /// Validate Payment method IBAN
        /// </summary>
        private void PaymentMethodIBAN(Creditor creditor, Company company, String iban)
        {
            //ESTONIA: PaymentId can be used for Payment Reference when PaymentType = IBAN 
            //IBAN will be retrieved from Creditor
            if (exportFormat == ExportFormatType.ISO20022_EE)
            {
                var paymRefNumber = iban ?? string.Empty;
                iban = creditor._PaymentId ?? string.Empty;

                //Validate Creditor IBAN >>
                if (iban == string.Empty)
                {
                    checkErrors.Add(new CheckError(String.Format("The Creditor IBAN number has not been filled in")));
                }
                else
                {
                    creditorIBAN = Regex.Replace(iban, "[^\\w\\d]", "");

                    if (!StandardPaymentFunctions.ValidateIBAN(creditorIBAN))
                    {
                        checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                    }
                    else
                    {
                        string countryId = string.Empty;

                        if (iban.Length >= 2)
                            countryId = iban.Substring(0, 2);

                        if (glJournalGenerated && countryId != BaseDocument.COUNTRY_DK)
                            checkErrors.Add(new CheckError(String.Format("Creditor information is required for foreign payments and they're not available.")));
                    }
                }
                //Validate Creditor BBAN <<

                //Validate Estonia Payment Reference >>
                if (paymRefNumber != string.Empty)
                {
                    //Need information from Estonia to implement
                }
                //Validate Estonia Payment Reference <<
            }
            else
            {
                if (!string.IsNullOrEmpty(iban))
                {
                    creditorIBAN = Regex.Replace(iban, "[^\\w\\d]", "");

                    if (!StandardPaymentFunctions.ValidateIBAN(creditorIBAN))
                    {
                        checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid"))); 
                    }
                    else
                    {
                        string countryId = string.Empty;

                        if (iban.Length >= 2)
                            countryId = iban.Substring(0, 2);

                        if (glJournalGenerated && countryId != BaseDocument.COUNTRY_DK)
                            checkErrors.Add(new CheckError(String.Format("Creditor information is required for foreign payments and they're not available.")));
                    }
                }
                else
                {
                    checkErrors.Add(new CheckError(fieldCannotBeEmpty("PaymentId")));
                }
            }
        }


        /// <summary>
        /// Validate Payment method BBAN
        /// </summary>
        private void PaymentMethodBBAN(Creditor creditor, Company company, String bban)
        {
            //Norway: PaymentId can be used for Kid-No when PaymentType = VendorBankAccount 
            //BBAN will be retrieved from Creditor
            if (exportFormat == ExportFormatType.ISO20022_NO)
            {
                var kidNo = bban ?? string.Empty;
                bban = creditor._PaymentId ?? string.Empty;

                //Validate Creditor BBAN >>
                if (bban == string.Empty)
                {
                    checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("CredBBANMissing")));
                }
                else
                {
                    bban = Regex.Replace(bban, "[^0-9]", "");

                    if (bban.Length <= 4 || bban.Length > 11)
                        checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                }
                //Validate Creditor BBAN <<

                //Validate KID-number >>
                if (kidNo != string.Empty)
                {
                    if (kidNo.Length < 4 || kidNo.Length > 25)
                    {
                        checkErrors.Add(new CheckError(String.Format("The KID-number lenght is not valid.")));
                    }
                    else if (Mod10Check(kidNo) == false)
                    {
                        if (Mod11Check(kidNo) == false)
                        {
                            checkErrors.Add(new CheckError(String.Format("The KID-number failed the Modulus 10 and 11 check digit.")));
                        }
                    }
                }
                //Validate KID-number <<
            }
            else
            {
                if (string.IsNullOrEmpty(bban))
                {
                    checkErrors.Add(new CheckError(fieldCannotBeEmpty("PaymentId")));
                }
                else
                {
                    bban = Regex.Replace(bban, "[^0-9]", "");
                    if (bban.Length <= 4)
                        checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid"))); 

                    var countryId = glJournalGenerated ? UnicontaCountryToISO(company._CountryId) : UnicontaCountryToISO(creditor._Country);

                    if (countryId == BaseDocument.COUNTRY_DK)
                    {
                        if (bban.Length > 14)
                            checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid"))); 
                    }
                }
            }
        }


        /// <summary>
        /// Validate Payment method FIK71
        /// </summary>
        private Boolean PaymentMethodFIK71(String ocrLine)
        {
            if (string.IsNullOrEmpty(ocrLine))
            {
                checkErrors.Add(new CheckError(fieldCannotBeEmpty("PaymentId"))); 
                return false;
            }

            if (ocrLine.IndexOf("N") != -1 || ocrLine.IndexOf("n") != -1)
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                return false;
            }

            string paymID = string.Empty;
            string creditorAccount = string.Empty;

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+71<", "");
            ocrLine = ocrLine.Replace(">71<", "");
            ocrLine = ocrLine.Replace("<", "");
            ocrLine = ocrLine.Replace(">", "");
            int index = ocrLine.IndexOf("+");
            if (index > 0)
            {
                paymID = ocrLine.Substring(0, index);
                creditorAccount = ocrLine.Remove(0, index + 1);
            }

            if (paymID == string.Empty || paymID.Length > BaseDocument.FIK71LENGTH || creditorAccount == string.Empty || creditorAccount.Length != 8 || creditorAccount[0] != '8' || Mod10Check(paymID) == false)
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid"))); 
                return false;
            }

            if (glJournalGenerated)
            {
                switch (exportFormat)
                {
                    case ExportFormatType.Nordea_CSV:
                        checkErrors.Add(new CheckError(String.Format("Creditor information is required for FIK71 payments and they're not available.")));
                        break;
                }
            }

            return true;
        }


        /// <summary>
        /// Validate Payment method FIK73
        /// </summary>
        private Boolean PaymentMethodFIK73(String ocrLine)
        {
            if (string.IsNullOrEmpty(ocrLine))
            {
                checkErrors.Add(new CheckError(fieldCannotBeEmpty("PaymentId")));
                return false;
            }

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+73<", "");
            ocrLine = ocrLine.Replace(">73<", "");
            ocrLine = ocrLine.Replace("<", "");
            ocrLine = ocrLine.Replace(">", "");
            var creditorAccount = ocrLine.Replace("+", "");
           
            if (creditorAccount == string.Empty || creditorAccount.Length != 8 || creditorAccount[0] != '8')
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                return false;
            }

            if (glJournalGenerated)
            {
                switch (exportFormat)
                {
                    case ExportFormatType.Nordea_CSV:
                        checkErrors.Add(new CheckError(String.Format("Creditor information is required for FIK73 payments and they're not available.")));
                        break;
                }
            }

            return true;
        }


        /// <summary>
        /// Validate Payment method FIK75
        /// </summary>
        private Boolean PaymentMethodFIK75(String ocrLine)
        {
            if (string.IsNullOrEmpty(ocrLine))
            {
                checkErrors.Add(new CheckError(fieldCannotBeEmpty("PaymentId")));
                return false;
            }

            if (ocrLine.IndexOf("N") != -1 || ocrLine.IndexOf("n") != -1)
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid"))); 
                return false;
            }

            string paymID = string.Empty;
            string creditorAccount = string.Empty;

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+75<", "");
            ocrLine = ocrLine.Replace(">75<", "");
            ocrLine = ocrLine.Replace("<", "");
            ocrLine = ocrLine.Replace(">", "");
            int index = ocrLine.IndexOf("+");
            if (index > 0)
            {
                paymID = ocrLine.Substring(0, index);
                creditorAccount = ocrLine.Remove(0, index + 1);
            }

            if (paymID == string.Empty || paymID.Length > BaseDocument.FIK75LENGTH || creditorAccount == string.Empty || creditorAccount.Length != 8 || creditorAccount[0] != '8' || Mod10Check(paymID) == false)
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                return false;
            }

            if (glJournalGenerated)
            {
                switch (exportFormat)
                {
                    case ExportFormatType.Nordea_CSV:
                        checkErrors.Add(new CheckError(String.Format("Creditor information is required for FIK75 payments and they're not available.")));
                        break;
                }
            }

            return true;
        }


        /// <summary>
        /// Validate Payment method FIK04
        /// </summary>
        private Boolean PaymentMethodFIK04(String ocrLine)
        {
            if (string.IsNullOrEmpty(ocrLine))
            {
                checkErrors.Add(new CheckError(fieldCannotBeEmpty("PaymentId"))); 
                return false;
            }

            if (ocrLine.IndexOf("N") != -1 || ocrLine.IndexOf("n") != -1)
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                return false;
            }

            string paymID = string.Empty;
            string giroAccount = string.Empty;

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+04<", "");
            ocrLine = ocrLine.Replace(">04<", "");
            ocrLine = ocrLine.Replace("<", "");
            ocrLine = ocrLine.Replace(">", "");
            int index = ocrLine.IndexOf("+");
            if (index > 0)
            {
                paymID = ocrLine.Substring(0, index);
                giroAccount = ocrLine.Remove(0, index + 1);
            }

            if (paymID == string.Empty || paymID.Length > BaseDocument.FIK04LENGTH || giroAccount == string.Empty || giroAccount.Length < 2 || giroAccount.Length > 8 || Mod10Check(paymID) == false)
            {
                checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                return false;
            }

            if (CompanyBankEnum == CompanyBankENUM.DanskeBank)
            {
                if (int.Parse(giroAccount) > 69999999)
                {
                    checkErrors.Add(new CheckError(String.Format("Giro account part of the FIK04 OCR-reference can max be 69999999.")));
                    return false;
                }
            }

            if (glJournalGenerated)
            {
                switch (exportFormat)
                {
                    case ExportFormatType.Nordea_CSV:
                        checkErrors.Add(new CheckError(String.Format("Creditor information is required for FIK04 payments and they're not available.")));
                        break;
                }
            }

            return true;
        }

        /// <summary>
        /// Tdentification assigned by an institution.
        /// Max. 35 characters.
        /// </summary>
        private bool IdentificationId(String identificationId)
        {
            identificationId = identificationId ?? string.Empty;

            switch (CompanyBankEnum)
            {
                case CompanyBankENUM.Nordea_DK:
                    if (identificationId == string.Empty)
                        return false;
                    else
                        return true;
                case CompanyBankENUM.DanskeBank:
                    return true; //Pt. ukendt - Kode skal sandsynligvis aftale med Banken
                case CompanyBankENUM.BankConnect:
                    return true; //Pt. ukendt - Kode skal sandsynligvis aftale med Banken
                default:
                    return true;
            }
        }

        /// <summary>
        /// Modulus 10 check digit validation
        /// </summary>
        private bool Mod10Check(string number)
        {
            number = number.Trim() ?? string.Empty;

            var total = 0;
            var alt = false;
            for (int i = number.Length; (--i >= 0); )
            {
                var curDigit = (number[i] - '0');
                if (alt)
                {
                    curDigit *= 2;
                    if (curDigit > 9)
                        curDigit -= 9;
                }
                total += curDigit;
                alt = !alt;
            }
            return total % 10 == 0;
        }

        /// <summary>
        /// Modulus 11 check digit validation
        /// </summary>
        private bool Mod11Check(string number)
        {
            if (number == null)
                return false;
            number = number.Trim();
            var i = number.Length - 1;
            if (i <= 0)
                return false;

            int checkCiffer = number[i] - '0';
            int Sum = 0;
            int Multiplier = 2;
            while (--i >= 0)
            {
                Sum += (number[i] - '0') * Multiplier;
                if (++Multiplier > 7) Multiplier = 2;
            }
            int checkCifferCalc = (11 - (Sum % 11));
            if (checkCifferCalc >= 10)
                checkCifferCalc -= 10;

            return (checkCiffer == checkCifferCalc);
        }
    }
}
