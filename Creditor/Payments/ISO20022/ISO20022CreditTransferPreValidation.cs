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
using UnicontaClient.Pages.Creditor.Payments;

namespace ISO20022CreditTransfer
{
    
    /// <summary>
    /// Class with static methods for Pre-validation of ISO 20022 Credit Transfer document.
    /// </summary>
    public class PaymentISO20022PreValidate
    {

        #region Member variables
        private CompanyBankENUM companyBankEnum;
        private CreditorPaymentFormat credPaymFormat;
        private BankSpecificSettings bankSpecificSettings;
        private bool SWIFTok;
        private bool IBANok;
        private bool formatTypeISO;
        List<PreCheckError> preCheckErrors = new List<PreCheckError>();
        #endregion

        #region Properties

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

        private List<PreCheckError> PreCheckErrors
        {
            get
            {
                return preCheckErrors;
            }

            set
            {
                preCheckErrors = value;
            }
        }
        #endregion


        static public string UnicontaCountryToISO(CountryCode code)
        {
            return ((CountryISOCode)code).ToString();
        }

        private void CompanyBank(CreditorPaymentFormat credPaymFormat)
        {
            BankSpecificSettings bankSpecific = BankSpecificSettings.BankSpecTypeInstance(credPaymFormat);
            this.bankSpecificSettings = bankSpecific;
            this.credPaymFormat = credPaymFormat;

            companyBankEnum = bankSpecific.CompanyBank();
        }

        /// <summary>
        /// Pre-validate general settings before generating the payment file in the XML format Credit Transfer ISO20022 pain003.
        /// </summary>
        /// <param name="xxx">xxx.</param> 
        /// <param name="xxx">xxx.</param>
        /// <returns>An XML payment file</returns>
        public XMLDocumentGenerateResult PreValidateISO20022(Company company, SQLCache bankAccountCache, CreditorPaymentFormat credPaymFormat, bool glJournalGenerated = false, bool schemaValidation = true)
        {
            XmlDocument dummyDoc = new XmlDocument();

            if (bankAccountCache.Count == 0 || credPaymFormat._BankAccount == null)
            {
                PreCheckErrors.Add(new PreCheckError(String.Format("There need to be specified a bank in Creditor Payment Format setup for '{0}'.", credPaymFormat._Format)));
            }
            else
            {
                var bankAccount = (BankStatement)bankAccountCache.Get(credPaymFormat._BankAccount);

                if (bankAccount == null)
                {
                    PreCheckErrors.Add(new PreCheckError(String.Format("There need to be specified a bank in Creditor Payment Format setup for '{0}'.", credPaymFormat._Format)));
                }
                else
                {
                    CompanyBank(credPaymFormat);

                    var paymentformat = (ExportFormatType)credPaymFormat._ExportFormat;

                    formatTypeISO = paymentformat == ExportFormatType.ISO20022_DK || paymentformat == ExportFormatType.ISO20022_NL || paymentformat == ExportFormatType.ISO20022_NO ||
                                    paymentformat == ExportFormatType.ISO20022_DE || paymentformat == ExportFormatType.ISO20022_SE || paymentformat == ExportFormatType.ISO20022_UK || 
                                    paymentformat == ExportFormatType.ISO20022_LT || paymentformat == ExportFormatType.ISO20022_CH;

                    if (glJournalGenerated && formatTypeISO) 
                        PreCheckErrors.Add(new PreCheckError(string.Format("Payment format '{0}' is not available for GL Journal generated payments", credPaymFormat._ExportFormat))); //TODO:Opret label

                    CompanyBankName(paymentformat);
                    CustomerIdentificationId(bankAccount._BankCompanyId, paymentformat); //Field Bank "Kunde-Id"
                    BankIdentificationId(bankAccount._ContractId, paymentformat); //Field Bank "Identifikation af aftalen"

                    CompanySWIFT(bankAccount._SWIFT, paymentformat);
                    CompanyIBAN(bankAccount._IBAN, paymentformat);

                    //For ISO20022 only validate BBAN if IBAN/SWIFT are not available
                    if (formatTypeISO && (!IBANok || !SWIFTok) || !formatTypeISO)
                    {
                        CompanyBBANRegNum(bankAccount._BankAccountPart1, paymentformat);
                        CompanyBBAN(bankAccount._BankAccountPart2, paymentformat);
                    }
                }
            }

            return new XMLDocumentGenerateResult(dummyDoc, PreCheckErrors.Count > 0, 0, PreCheckErrors);
        }

        /// <summary>
        /// Company Bank
        /// </summary>
        public void CompanyBankName(ExportFormatType exportFormat)
        {
            //TODO: Need to check ExportFormatType, because Sebastion also use PreValidation - CSV files could also use Properties it would solve it
            if (companyBankEnum == CompanyBankENUM.None && formatTypeISO)
                preCheckErrors.Add(new PreCheckError(String.Format("No bank has been choosen under properties. (Format: {0})", credPaymFormat._Format)));
        }

        /// <summary>
        /// Registration number for a BBAN account.
        /// In Denmark it has to be 4 char
        /// </summary>
        public void CompanyBBANRegNum(String bbanRegNum, ExportFormatType exportFormat)
        {
            if (exportFormat == ExportFormatType.ISO20022_DK || exportFormat == ExportFormatType.BankData || exportFormat == ExportFormatType.BEC_CSV || 
                exportFormat == ExportFormatType.DanskeBank_CSV || exportFormat == ExportFormatType.Nordea_CSV || exportFormat == ExportFormatType.SDC)
            {
                bbanRegNum = bbanRegNum ?? string.Empty;
                bbanRegNum = Regex.Replace(bbanRegNum, "[^0-9]", "");

                if (string.IsNullOrEmpty(bbanRegNum))
                    preCheckErrors.Add(new PreCheckError(String.Format("The Bank Registration number has not been filled in. (Format: {0})", credPaymFormat._Format)));
                else
                    if (bbanRegNum.Length != 4)
                    preCheckErrors.Add(new PreCheckError(String.Format("The Bank Registration number has a wrong format. (Format: {0})", credPaymFormat._Format)));

            }
        }

        /// <summary>
        /// Unambiguous identification of the BBAN account of the debtor to which a debit entry will be made as a result of the transaction.
        /// In Denmark it will be: Reg. no. + Account no. 14 char (4+10)
        /// </summary>
        public void CompanyBBAN(String bban, ExportFormatType exportFormat)
        {
            if (exportFormat == ExportFormatType.ISO20022_DK || exportFormat == ExportFormatType.BankData || exportFormat == ExportFormatType.BEC_CSV ||
                exportFormat == ExportFormatType.DanskeBank_CSV || exportFormat == ExportFormatType.Nordea_CSV || exportFormat == ExportFormatType.SDC)
            {
                bban = bban ?? string.Empty;
                bban = Regex.Replace(bban, "[^0-9]", "");

                if (string.IsNullOrEmpty(bban))
                {
                    preCheckErrors.Add(new PreCheckError(String.Format("The Bank Account number (BBAN) has not been filled in. (Format: {0})", credPaymFormat._Format)));
                }
                else if (bban.Length > 10)
                {
                    preCheckErrors.Add(new PreCheckError(String.Format("The Bank Account number (BBAN) has a wrong format. (Format: {0})", credPaymFormat._Format)));
                }
            }
        }

        /// <summary>
        /// Company SWIFT/BIC code
        /// </summary>
        public void CompanySWIFT(String swift, ExportFormatType exportFormat)
        {
            //For now we require that IBAN/SWIFT is always filled in for ISO20022 payments - except for BankConnect (BankData, BEC and SDC)
            if (formatTypeISO)
            {
                SWIFTok = true;
                swift = swift ?? string.Empty;
                swift = Regex.Replace(swift, "[^\\w\\d]", "");

                if (string.IsNullOrEmpty(swift))
                {
                    var isBC = companyBankEnum == CompanyBankENUM.BankData || companyBankEnum == CompanyBankENUM.BEC || companyBankEnum == CompanyBankENUM.SDC;
                    if (!isBC)
                        preCheckErrors.Add(new PreCheckError(String.Format("The SWIFT code has not been filled in. (Format: {0})", credPaymFormat._Format)));
                    SWIFTok = false;
                }
                else if (!StandardPaymentFunctions.ValidateBIC(swift))
                {
                    preCheckErrors.Add(new PreCheckError(String.Format("The SWIFT code has not a valid format. (Format: {0})", credPaymFormat._Format)));
                    SWIFTok = false;
                }
            }
        }


        /// <summary>
        /// Company IBAN number
        /// </summary>
        public void CompanyIBAN(String iban, ExportFormatType exportFormat)
        {
            //For now we require that IBAN/SWIFT is always filled in for ISO20022 payments - except for BankConnect (BankData, BEC and SDC)
            if (formatTypeISO)
            {
                IBANok = true;
                iban = iban ?? string.Empty;
                iban = Regex.Replace(iban, "[^\\w\\d]", "");

                if (string.IsNullOrEmpty(iban))
                {
                    var isBC = companyBankEnum == CompanyBankENUM.BankData || companyBankEnum == CompanyBankENUM.BEC || companyBankEnum == CompanyBankENUM.SDC;
                    if (!isBC)
                        preCheckErrors.Add(new PreCheckError(String.Format("The IBAN number has not been filled in. (Format: {0})", credPaymFormat._Format)));
                    IBANok = false;
                }
                else if (!StandardPaymentFunctions.ValidateIBAN(iban))
                {
                    preCheckErrors.Add(new PreCheckError(String.Format("The IBAN number has not a valid format. (Format: {0})", credPaymFormat._Format)));
                    IBANok = false;
                }
            }
        }

        /// <summary>
        /// Customer identification assigned by an institution.
        /// Max. 35 characters.
        /// </summary>
        public void CustomerIdentificationId(String customerIdentificationId, ExportFormatType exportFormat)
        {
            customerIdentificationId = customerIdentificationId ?? string.Empty;

            if (exportFormat == ExportFormatType.ISO20022_DK)
            {
                switch (companyBankEnum)
                {
                    //Customer identification (Signer) as agreed with(or assigned by) Nordea.If provided by Nordea the identification consists of maximum 13 digits.
                    case CompanyBankENUM.Nordea_DK:
                    case CompanyBankENUM.Nordea_NO:
                    case CompanyBankENUM.Nordea_SE:
                        if (string.IsNullOrEmpty(customerIdentificationId) || (customerIdentificationId.Length < 10 || customerIdentificationId.Length > 18))
                            preCheckErrors.Add(new PreCheckError(String.Format("The customer Identification Id is mandatory for '{0}'. (Min 10 and Max 18 characters may be allowed)", companyBankEnum)));
                        break;
                }
            }
        }

        /// <summary>
        /// Unique identification of an organisation , as assigned by an institution, using an identification scheme.
        /// </summary>
        public void BankIdentificationId(String bankIdentificationId, ExportFormatType exportFormat)
        {
            bankIdentificationId = bankIdentificationId ?? string.Empty;

            if (exportFormat == ExportFormatType.ISO20022_DK)
            {
                switch (companyBankEnum)
                {
                    //Unique identification of Corporate Cash Management agreement with Nordea. Customer agreement identification with Nordea is mandatory (BANK)and the identification consist of minimum 10 and maximum 18 digits.
                    case CompanyBankENUM.Nordea_DK:
                    case CompanyBankENUM.Nordea_SE:
                    case CompanyBankENUM.Nordea_NO:
                    case CompanyBankENUM.Handelsbanken:
                        if (string.IsNullOrEmpty(bankIdentificationId) || (bankIdentificationId.Length < 10 || bankIdentificationId.Length > 18))
                            preCheckErrors.Add(new PreCheckError(String.Format("The Bank Identification Id is mandatory for '{0}'.  (Min 10 and Max 18 characters may be allowed)", companyBankEnum)));
                        break;
                    case CompanyBankENUM.BankData:
                    case CompanyBankENUM.BEC:
                    case CompanyBankENUM.SDC:
                        if (string.IsNullOrEmpty(bankIdentificationId))
                            preCheckErrors.Add(new PreCheckError("Feltet 'Identifikation af aftalen' (Bankafstemning) skal udfyldes"));
                        break;
                }
            }
        }
    }
}
