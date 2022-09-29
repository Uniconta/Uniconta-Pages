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
using Uniconta.API.System;
using System.Threading.Tasks;

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
        private CrudAPI crudAPI;
        private Company company;

        private bool glJournalGenerated;
        private ISO20022PaymentTypes isoPaymentType;

        protected string creditorIBAN = string.Empty;
        protected string creditorSWIFT = string.Empty;


        List<CheckError> checkErrors = new List<CheckError>();
        #endregion

        #region Constants
        private const double RGLTRYRPTGLIMITAMOUNT_SEK = 150000.00;
        private const double RGLTRYRPTGLIMITAMOUNT_NOK = 100000.00;
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

        /// <summary>
        /// Default Constructor
        /// </summary>
        public PaymentISO20022Validate(CrudAPI api, CreditorPaymentFormat credPaymFormat)
        {
            crudAPI = api;
            company = api.CompanyEntity;

            BankSpecificSettings bankSpecific = BankSpecificSettings.BankSpecTypeInstance(credPaymFormat);
            this.bankSpecificSettings = bankSpecific;
            this.credPaymFormat = credPaymFormat;

            CompanyBankEnum = bankSpecific.CompanyBank();
        }

        static public string UnicontaCountryToISO(CountryCode code)
        {
            return ((CountryISOCode)code).ToString();
        }

        /// <summary>
        /// Validate proposed payment records before generating the payment file in the XML format Credit Transfer ISO20022 pain003.
        /// </summary>
        /// <param name="xxx">xxx.</param> 
        /// <param name="xxx">xxx.</param>
        /// <returns>An XML payment file</returns>
        public async Task<XMLDocumentGenerateResult> ValidateISO20022(UnicontaClient.Pages.CreditorTransPayment trans, SQLCache bankAccountCache, bool journalGenerated = false)
        {
            XmlDocument dummyDoc = new XmlDocument();

            CreditTransferDocument doc = new CreditTransferDocument();

            glJournalGenerated = journalGenerated;
            var bankAccount = (BankStatement)bankAccountCache.Get(credPaymFormat._BankAccount);
            var credCache = company.GetCache(typeof(Uniconta.DataModel.Creditor));
            var creditor = (Creditor)credCache.Get(trans.Account);
            if (creditor == null && !glJournalGenerated)
            {
                CheckError.Add(new CheckError(String.Format("{0} : {1}",
                    Uniconta.ClientTools.Localization.lookup("AccountDoesNotExist"),
                    Uniconta.ClientTools.Localization.lookup(trans.Account))));
                return new XMLDocumentGenerateResult(dummyDoc, CheckError.Count > 0, 0, CheckError);
            }

            exportFormat = (ExportFormatType)credPaymFormat._ExportFormat;
            
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

            var countryCode = glJournalGenerated ? (CountryCode)company._Country : creditor._Country;
            ISOPaymentType(trans.CurrencyLocalStr, bankAccount._IBAN, trans._PaymentMethod, UnicontaCountryToISO(countryCode));

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

                case PaymentTypes.PaymentMethod3: //FIK71 + BankGirot
                    PaymentMethodFIK71(creditor, trans._PaymentId);
                    break;

                case PaymentTypes.PaymentMethod4: //FIK73 + PlusGirot
                    PaymentMethodFIK73(trans._PaymentId);
                    break;

                case PaymentTypes.PaymentMethod5: //FIK75
                    PaymentMethodFIK75(creditor, trans._PaymentId);
                    break;

                case PaymentTypes.PaymentMethod6: //FIK04
                    PaymentMethodFIK04(trans._PaymentId);
                    break;
            }
          
            await RegulatoryReporting(trans);
            
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

            if (credPaymFormat._ExportFormat == (byte)ExportFormatType.ISO20022_DK && (CompanyBankEnum == CompanyBankENUM.DanskeBank || CompanyBankEnum == CompanyBankENUM.Nordea_DK || CompanyBankEnum == CompanyBankENUM.Nordea_NO || CompanyBankEnum == CompanyBankENUM.Nordea_SE))
            {
                if (paymentType != PaymentTypes.IBAN && isoPaymentType == ISO20022PaymentTypes.DOMESTIC && paymentCcy == BaseDocument.CCYEUR)
                    checkErrors.Add(new CheckError(String.Format("It's required to use IBAN as creditor account for Domestic EUR payments.")));

                //Nordea
                if ((CompanyBankEnum == CompanyBankENUM.Nordea_DK || CompanyBankEnum == CompanyBankENUM.Nordea_NO || CompanyBankEnum == CompanyBankENUM.Nordea_SE) && (paymentType != PaymentTypes.IBAN && isoPaymentType == ISO20022PaymentTypes.SEPA))
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

            if (CompanyBankEnum == CompanyBankENUM.CreditSuisse && creditor._ZipCode == null)
            {
                var string1 = fieldCannotBeEmpty("ZipCode");
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
        private void PaymentMethodIBAN(Creditor creditor, Company company, string iban)
        {
            //Estonia and Switzerland: PaymentId can be used for Payment Reference when PaymentType = IBAN 
            //IBAN will be retrieved from Creditor
            if (exportFormat == ExportFormatType.ISO20022_EE || exportFormat == ExportFormatType.ISO20022_CH)
            {
                var paymRefNumber = iban;
                iban = glJournalGenerated ? string.Empty : creditor._PaymentId ?? string.Empty;

                //Validate Creditor IBAN >>
                if (iban == string.Empty)
                {
                    checkErrors.Add(new CheckError(String.Format("The Creditor IBAN number has not been filled in")));
                }
                else
                {
                    creditorIBAN = Regex.Replace(iban, "[^\\w\\d]", "");
                    if (!StandardPaymentFunctions.ValidateIBAN(creditorIBAN))
                        checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                }
                //Validate Creditor BBAN <<

                //Validate Estonia and Switzerland Payment Reference >>
                if (paymRefNumber != null)
                {
                    if (exportFormat == ExportFormatType.ISO20022_CH && isoPaymentType == ISO20022PaymentTypes.DOMESTIC)
                    {
                        //Validate QRR-number >>
                        if (paymRefNumber.Length != 27)
                            checkErrors.Add(new CheckError(String.Format("The QRR-number lenght is not valid")));
                        else if (QRRCheckDigitCheck(paymRefNumber) == false)
                            checkErrors.Add(new CheckError(String.Format("The QRR-number failed the modulus check")));
                        //Validate QRR-number <<

                        //Need information from Estonia to implement
                    }
                    //Validate Estonia and Switzerland Payment Reference <<
                }
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

            if (exportFormat == ExportFormatType.ISO20022_SE)
            {
                bban = glJournalGenerated ? string.Empty : creditor._PaymentId ?? string.Empty;

                //Validate Creditor BBAN >>
                if (bban == string.Empty)
                {
                    checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("CredBBANMissing")));
                }
                else
                {
                    bban = Regex.Replace(bban, "[^0-9]", "");
                    if (bban.Length < 11)
                        checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                }
                //Validate Creditor BBAN <<
            }
            else if (exportFormat == ExportFormatType.ISO20022_NO)
            {
                var kidNo = bban ?? string.Empty;
                bban = glJournalGenerated ? string.Empty : creditor._PaymentId ?? string.Empty;

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
        /// Validate Regulatory Reporting
        /// Norway: Regulatory reporting may be needed when transferring money abroad from accounts held in Norway when the Amount exceeds 100.000 NOK
        /// Sweden: Regulatory reporting may be needed when transferring money abroad from accounts held in Norway when the Amount exceeds 150.000 SEK
        /// </summary>
        private async Task RegulatoryReporting(CreditorTransPayment trans)
        {
            if (exportFormat == ExportFormatType.ISO20022_NO && (isoPaymentType == ISO20022PaymentTypes.CROSSBORDER || isoPaymentType == ISO20022PaymentTypes.SEPA))
            {
                double paymAmount = 0;
                var transCcy = trans.Currency ?? company._CurrencyId;

                if (transCcy != Currencies.NOK)
                {
                    var rate = await crudAPI.session.ExchangeRate(transCcy, Currencies.NOK, trans.Date, company);
                    paymAmount = Math.Round(trans.PaymentAmount * rate, 2);
                }
                else
                    paymAmount = trans.PaymentAmount;

                if (paymAmount >= RGLTRYRPTGLIMITAMOUNT_NOK)
                {
                    if (trans.RgltryRptgCode == 0)
                        checkErrors.Add(new CheckError(string.Concat("Regulatory reporting: purpose code is required (Payment amount > ", RGLTRYRPTGLIMITAMOUNT_NOK, " NOK")));
                    if (trans.RgltryRptgText == null)
                        checkErrors.Add(new CheckError(string.Concat("Regulatory reporting: supplementary text concerning the purpose is required (Payment amount > ", RGLTRYRPTGLIMITAMOUNT_NOK, " NOK")));
                }
            }
            else if (exportFormat == ExportFormatType.ISO20022_SE && (isoPaymentType == ISO20022PaymentTypes.CROSSBORDER || isoPaymentType == ISO20022PaymentTypes.SEPA))
            {
                double paymAmount = 0;
                var transCcy = trans.Currency ?? company._CurrencyId;
                if (transCcy != Currencies.SEK)
                {
                    var rate = await crudAPI.session.ExchangeRate(transCcy, Currencies.SEK, trans.Date, company);
                    paymAmount = Math.Round(trans.PaymentAmount * rate, 2);
                }
                else
                    paymAmount = trans.PaymentAmount;

                if (paymAmount >= RGLTRYRPTGLIMITAMOUNT_SEK)
                {
                    if (trans.RgltryRptgCode == 0)
                        checkErrors.Add(new CheckError(string.Concat("Regulatory reporting purpose code is required (Payment amount > ", RGLTRYRPTGLIMITAMOUNT_SEK," SEK")));
                }
            }
        }


        /// <summary>
        /// Validate Payment method FIK71
        /// </summary>
        private Boolean PaymentMethodFIK71(Creditor creditor, string ocrLine)
        {
            if (exportFormat == ExportFormatType.ISO20022_SE)
            {
                //Validate Creditor BBAN >>
                var bban = creditor._PaymentId;
                if (bban == null)
                { 
                    checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("CredBBANMissing")));
                }
                else
                {
                    bban = Regex.Replace(bban, "[^0-9]", "");

                    if (bban.Length < 7 || bban.Length > 8)
                        checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                }
                //Validate Creditor BBAN <<

                //Validate OCR-number >>
                if (!string.IsNullOrEmpty(ocrLine))
                {
                    if (ocrLine.Length < 4 || ocrLine.Length > 25)
                    {
                        checkErrors.Add(new CheckError(String.Format("The OCR-number lenght is not valid.")));
                    }
                    else if (Mod10Check(ocrLine) == false)
                    {
                        checkErrors.Add(new CheckError(String.Format("The OCR-number failed the Modulus 10 check digit.")));
                    }
                }
                //Validate OCR-number <<
            }
            else
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
            }

            return true;
        }

        /// <summary>
        /// Validate Payment method FIK73
        /// </summary>
        private Boolean PaymentMethodFIK73(string ocrLine)
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
        private Boolean PaymentMethodFIK75(Creditor creditor, string ocrLine)
        {
            if (exportFormat == ExportFormatType.ISO20022_SE)
            {
                //Validate Creditor BBAN >>
                var bban = creditor._PaymentId;
                if (bban == null)
                {
                    checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("CredBBANMissing")));
                }
                else
                {
                    bban = Regex.Replace(bban, "[^0-9]", "");

                    if (bban.Length < 2 || bban.Length > 8)
                        checkErrors.Add(new CheckError(Uniconta.ClientTools.Localization.lookup("PaymentIdInvalid")));
                }
                //Validate Creditor BBAN <<

                //Validate OCR-number >>
                if (!string.IsNullOrEmpty(ocrLine))
                {
                    if (ocrLine.Length < 2 || ocrLine.Length > 25)
                    {
                        checkErrors.Add(new CheckError(String.Format("The OCR-number lenght is not valid.")));
                    }
                    else if (Mod10Check(ocrLine) == false)
                    {
                        checkErrors.Add(new CheckError(String.Format("The OCR-number failed the Modulus 10 check digit.")));
                    }
                }
                //Validate OCR-number <<
            }
            else
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

        /// <summary>
        /// Switzerland QRR code check digit validation
        /// </summary>
        private  bool QRRCheckDigitCheck(string number)
        {
            int[] tabelle = { 0, 9, 4, 6, 8, 2, 7, 1, 3, 5 };
            int uebertrag = 0;

            var sb = StringBuilderReuse.Create(number);
            sb.Length = 26;

            for (int i = 0; i < sb.Length; i++)
                uebertrag = tabelle[(uebertrag + sb[i] - '0') % 10];

            sb.Append((10 - uebertrag) % 10);

            return string.Compare(sb.ToStringAndRelease(), number) == 0;
        }
    }
}
