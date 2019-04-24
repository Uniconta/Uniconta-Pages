using System;
using System.Collections.Generic;
using System.Xml;

using Uniconta;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaAPI;
using Uniconta.ClientTools.DataModel;
using System.Text.RegularExpressions;
using System.Text;
using Corasau.Client.Pages;
using System.Windows;
using UnicontaDirectDebitPayment;

namespace UnicontaDirectDebitPayment
{

    /// <summary>
    /// Class with static methods for Pre-validation of Direct Debit Payments.
    /// </summary>
    public class DirectDebitPrevalidation
    {

        #region Member variables
        List<PreCheckError> preCheckErrors = new List<PreCheckError>();

        private Company comp;
        #endregion

        #region Properties
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

        /// <summary>
        /// Pre-validate general settings before generating the Direct Debit Payment file
        /// </summary>
        /// <param name="xxx">xxx.</param> //TODO:Mangler at specificere parametre
        /// <param name="xxx">xxx.</param>
        /// <returns></returns>
        public DirectDebitGenerateResult PreValidate(Company company, SQLCache bankAccountCache)//, DebtorPaymentFormat debPaymFormat)
        {
            comp = company;

            CompanySettings();

            return new DirectDebitGenerateResult(PreCheckErrors, 0, PreCheckErrors.Count > 0);
        }

        /// <summary>
        /// Company Settings
        /// </summary>
        public void CompanySettings()
        {
            //if (comp._Duns == null) //TODO:Denne skal rettes til de rigtige felter når de er blevet oprettet
            //    preCheckErrors.Add(new PreCheckError(String.Format("Company settings is not correct")));
        }

        ///// <summary>
        ///// Registration number for a BBAN account.
        ///// In Denmark it has to be 4 char
        ///// </summary>
        //public void CompanyBBANRegNum(String bbanRegNum, ExportFormatType exportFormat)
        //{
        //    if (exportFormat == ExportFormatType.ISO20022_DK || exportFormat == ExportFormatType.BankData || exportFormat == ExportFormatType.BEC_CSV ||
        //        exportFormat == ExportFormatType.DanskeBank_CSV || exportFormat == ExportFormatType.Nordea_CSV || exportFormat == ExportFormatType.SDC)
        //    {
        //        bbanRegNum = bbanRegNum ?? string.Empty;
        //        bbanRegNum = Regex.Replace(bbanRegNum, "[^0-9]", "");

        //        if (string.IsNullOrEmpty(bbanRegNum))
        //            preCheckErrors.Add(new PreCheckError(String.Format("The Bank Registration number has not been filled in. (Format: {0})", credPaymFormat._Format)));
        //        else
        //            if (bbanRegNum.Length != 4)
        //            preCheckErrors.Add(new PreCheckError(String.Format("The Bank Registration number has a wrong format. (Format: {0})", credPaymFormat._Format)));

        //    }
        //}

        ///// <summary>
        ///// Unambiguous identification of the BBAN account of the debtor to which a debit entry will be made as a result of the transaction.
        ///// In Denmark it will be: Reg. no. + Account no. 14 char (4+10)
        ///// </summary>
        //public void CompanyBBAN(String bban, ExportFormatType exportFormat)
        //{
        //    if (exportFormat == ExportFormatType.ISO20022_DK || exportFormat == ExportFormatType.BankData || exportFormat == ExportFormatType.BEC_CSV ||
        //        exportFormat == ExportFormatType.DanskeBank_CSV || exportFormat == ExportFormatType.Nordea_CSV || exportFormat == ExportFormatType.SDC)
        //    {
        //        bban = bban ?? string.Empty;
        //        bban = Regex.Replace(bban, "[^0-9]", "");

        //        if (string.IsNullOrEmpty(bban))
        //        {
        //            preCheckErrors.Add(new PreCheckError(String.Format("The Bank Account number (BBAN) has not been filled in. (Format: {0})", credPaymFormat._Format)));
        //        }
        //        else if (bban.Length > 10)
        //        {
        //            preCheckErrors.Add(new PreCheckError(String.Format("The Bank Account number (BBAN) has a wrong format. (Format: {0})", credPaymFormat._Format)));
        //        }
        //    }
        //}

        ///// <summary>
        ///// Company SWIFT/BIC code
        ///// </summary>
        //public void CompanySWIFT(String swift, ExportFormatType exportFormat)
        //{
        //    swift = swift ?? string.Empty;
        //    swift = Regex.Replace(swift, "[^\\w\\d]", "");

        //    //For now we require that IBAN/SWIFT is always filled in for ISO20022 payments - we probably has to ease the rule on the long run
        //    if (exportFormat == ExportFormatType.ISO20022_DK || exportFormat == ExportFormatType.ISO20022_NL || exportFormat == ExportFormatType.ISO20022_NO || exportFormat == ExportFormatType.ISO20022_DE)
        //    {
        //        if (string.IsNullOrEmpty(swift))
        //        {
        //            preCheckErrors.Add(new PreCheckError(String.Format("The SWIFT code has not been filled in. (Format: {0})", credPaymFormat._Format)));
        //        }
        //        else if (!StandardPaymentFunctions.ValidateBIC(swift))
        //        {
        //            preCheckErrors.Add(new PreCheckError(String.Format("The SWIFT code has not a valid format. (Format: {0})", credPaymFormat._Format)));
        //        }
        //    }
        //}


        ///// <summary>
        ///// Company IBAN number
        ///// </summary>
        //public void CompanyIBAN(String iban, ExportFormatType exportFormat)
        //{
        //    iban = iban ?? string.Empty;
        //    iban = Regex.Replace(iban, "[^\\w\\d]", "");

        //    //For now we require that IBAN/SWIFT is always filled in for ISO20022 payments - we probably has to ease the rule on the long run
        //    if (exportFormat == ExportFormatType.ISO20022_DK || exportFormat == ExportFormatType.ISO20022_NL || exportFormat == ExportFormatType.ISO20022_NO || exportFormat == ExportFormatType.ISO20022_DE)
        //    {
        //        if (string.IsNullOrEmpty(iban))
        //        {
        //            preCheckErrors.Add(new PreCheckError(String.Format("The IBAN number has not been filled in. (Format: {0})", credPaymFormat._Format)));
        //        }
        //        else if (!StandardPaymentFunctions.ValidateIBAN(iban))
        //        {
        //            preCheckErrors.Add(new PreCheckError(String.Format("The IBAN number has not a valid format. (Format: {0})", credPaymFormat._Format)));
        //        }
        //    }
        //}

        ///// <summary>
        ///// Customer identification assigned by an institution.
        ///// Max. 35 characters.
        ///// </summary>
        //public void CustomerIdentificationId(String customerIdentificationId, ExportFormatType exportFormat)
        //{
        //    customerIdentificationId = customerIdentificationId ?? string.Empty;

        //    if (exportFormat == ExportFormatType.ISO20022_DK)
        //    {
        //        switch (companyBankEnum)
        //        {
        //            //Customer identification (Signer) as agreed with(or assigned by) Nordea.If provided by Nordea the identification consists of maximum 13 digits.
        //            case CompanyBankENUM.Nordea_DK:
        //                if (string.IsNullOrEmpty(customerIdentificationId))
        //                    preCheckErrors.Add(new PreCheckError(String.Format("The customer Identification Id is mandatory for '{0}'.", companyBankEnum)));
        //                break;
        //        }
        //    }
        //}

        ///// <summary>
        ///// Unique identification of an organisation , as assigned by an institution, using an identification scheme.
        ///// </summary>
        //public void BankIdentificationId(String bankIdentificationId)
        //{
        //    bankIdentificationId = bankIdentificationId ?? string.Empty;

        //    switch (companyBankEnum)
        //    {
        //        //Unique identification of Corporate Cash Management agreement with Nordea. Customer agreement identification with Nordea is mandatory (BANK)and the identification consist of minimum 10 and maximum 18 digits.
        //        case CompanyBankENUM.Nordea_DK:
        //            if (string.IsNullOrEmpty(bankIdentificationId))
        //                preCheckErrors.Add(new PreCheckError(String.Format("The Bank Identification Id is mandatory for '{0}'.", companyBankEnum)));
        //            break;
        //    }
        //}
    }
}
