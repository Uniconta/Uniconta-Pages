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
using Uniconta.API.System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using DevExpress.CodeParser;
using DevExpress.Mvvm.Native;
using Uniconta.ClientTools;

namespace ISO20022CreditTransfer
{
    /// <summary>
    /// Class with static methods for creating ISO 20022 Credit Transfer document.
    /// </summary>
    public class PaymentISO20022
    {
        static public string UnicontaCountryToISO(CountryCode code)
        {
            return ((CountryISOCode)code).ToString();
        }

        static public string UnicontaCurrencyISO(Company company, BankStatement bankAccount)
        {
            if (bankAccount._Currency != 0)
                return CurrencyUtil.GetStringFromId((Currencies)bankAccount._Currency);
            else
                return CurrencyUtil.GetStringFromId((Currencies)company._Currency);
        }


        /// <summary>
        /// Generates a payment file in the XML format Credit Transfer ISO20022 pain003.
        /// </summary>
        /// <param name="Company">Uniconta company</param> 
        /// <param name="xxx">xxx.</param>
        /// <returns>An XML payment file</returns>                                                                                                                                                              
        public XMLDocumentGenerateResult GenerateISO20022(Company company, IEnumerable<CreditorTransPayment> queryPaymentTrans, SQLCache bankAccountCache, CreditorPaymentFormat credPaymFormat, int uniqueFileId, bool doMergePayment, bool schemaValidation = true)
        {
            CreditTransferDocument doc = new CreditTransferDocument();
            BankSpecificSettings bankSpecific = BankSpecificSettings.BankSpecTypeInstance(credPaymFormat);
            PaymentISO20022Validate paymentISO20022Validate = new PaymentISO20022Validate();

            List<PreCheckError> preCheckErrors = new List<PreCheckError>();
            List<CheckError> checkErrors = new List<CheckError>();

            var credCache = company.GetCache(typeof(Uniconta.DataModel.Creditor));
            bankSpecific.AllowedCharactersRegEx(false);
            doc.ReplaceCharactersRegExDict = bankSpecific.ReplaceCharactersRegEx();
           
            doc.CompanyBank = bankSpecific.CompanyBank();
            doc.BatchBooking = bankSpecific.BatchBooking();
            doc.AuthstnCodeTest = bankSpecific.TestMarked();
            doc.AuthstnCodeFeedback = bankSpecific.AuthstnCodeFeedback();

            var bankAccount = (BankStatement)bankAccountCache.Get(credPaymFormat._BankAccount);

            doc.CreDtTm = bankSpecific.CreDtTim();
            doc.NumberDecimalDigits = 2;
            doc.IdentificationId = bankSpecific.IdentificationId(bankAccount._ContractId, company._Id);  //Field Bank "Identifikation af aftalen"
            doc.DebtorIdentificationCode = bankSpecific.DebtorIdentificationCode(bankAccount._BankCompanyId); //Field Bank "Kunde-Id"

            doc.IdentificationCode = bankSpecific.IdentificationCode();
            doc.CompanyName = bankSpecific.CompanyName(company.Name);
            doc.CompanyBBAN = bankSpecific.CompanyBBAN(bankAccount._BankAccountPart1, bankAccount._BankAccountPart2);
            doc.CompanyIBAN = bankSpecific.CompanyIBAN(bankAccount._IBAN);
            doc.CompanyCcy = UnicontaCurrencyISO(company, bankAccount); 
            
            doc.CompanyBIC = bankSpecific.CompanyBIC(bankAccount._SWIFT);
            doc.CompanyBankName = bankSpecific.CompanyBankName();
            doc.CompanyCountryId = UnicontaCountryToISO(company._CountryId);
            bankSpecific.CompanyCountryId = doc.CompanyCountryId;
            doc.EncodingFormat = bankSpecific.EncodingFormat();

            doc.XMLAttributeNS = bankSpecific.XMLAttributeNS();

            doc.ChargeBearer = bankSpecific.ChargeBearerDebtor();

            doc.CompanyID = company.CompanyId;
            doc.NumberSeqPaymentFileId = uniqueFileId;

            string companyAccountId = string.Empty;
            string companyBIC = string.Empty;
            if (doc.CompanyIBAN != string.Empty && doc.CompanyBIC != string.Empty)
            {
                companyAccountId = doc.CompanyIBAN;
                companyBIC = doc.CompanyBIC;
                doc.CompanyPaymentMethod = "IBAN";
            }
            else
            {
                companyAccountId = doc.CompanyBBAN;
                doc.CompanyPaymentMethod = "BBAN";
            }

            doc.InitgPty = new InitgPty(doc.CompanyName, doc.IdentificationId, doc.IdentificationCode);

            //Update ISO PaymentType >>
            foreach (var rec in queryPaymentTrans)
            {
                var currency = rec.CurrencyLocalStr;
                double amount = 0;
                amount = rec.PaymentAmount;

                var BICnumber = "";
                var IBANnumber = "";
                if (rec._PaymentMethod == PaymentTypes.IBAN)
                {
                    IBANnumber = rec._PaymentId == null ? "" : rec._PaymentId;
                    IBANnumber = Regex.Replace(IBANnumber, "[^\\w\\d]", "");

                    BICnumber = rec._SWIFT == null ? "" : rec._SWIFT;
                    BICnumber = Regex.Replace(BICnumber, "[^\\w\\d]", "");
                }

                var creditor = (Creditor)credCache.Get(rec.Account);

                rec.internationalPayment = bankSpecific.InternationalPayment(bankAccount._IBAN, IBANnumber, BICnumber, UnicontaCountryToISO(creditor._Country), doc.CompanyCountryId);
                doc.ISOPaymentType = bankSpecific.ISOPaymentType(currency, bankAccount._IBAN, IBANnumber, BICnumber, UnicontaCountryToISO(creditor._Country), doc.CompanyCountryId);
                rec.ISOPaymentType = doc.ISOPaymentType.ToString();
                bankSpecific.paymentType = doc.ISOPaymentType;
            }
            //Update ISO PaymentType <<

            IOrderedEnumerable<CreditorTransPayment> queryPaymentTransSorted;
            if (doMergePayment)
                queryPaymentTransSorted = from s in queryPaymentTrans orderby s._PaymentRefId select s;
            else
                queryPaymentTransSorted = from s in queryPaymentTrans orderby s._PaymentDate, s.ISOPaymentType, s._PaymentMethod, s.Currency select s;

            paymentISO20022Validate.CompanyBank(credPaymFormat);

            List<string> paymentInfoIdLst = new List<string>();

            foreach (var rec in queryPaymentTransSorted)
            {
                bankSpecific.AllowedCharactersRegEx(rec.internationalPayment);

                doc.HeaderNumberOfTrans++;
                doc.PmtInfNumberOfTransActive = bankSpecific.PmtInfNumberOfTransActive();

                doc.HeaderCtrlSum += bankSpecific.HeaderCtrlSum(rec.PaymentAmount);

                doc.PmtInfCtrlSumActive = bankSpecific.PmtInfCtrlSumActive();

                doc.RequestedExecutionDate = bankSpecific.RequestedExecutionDate(doc.CompanyIBAN, rec._PaymentDate);

                string currency = rec.CurrencyLocalStr;
                currency = string.IsNullOrEmpty(currency) ? doc.CompanyCcy : currency;

                doc.EndToEndId = bankSpecific.EndtoendId(rec.PaymentEndToEndId);

                if (doMergePayment && rec.MergePaymId != UnicontaClient.Pages.Creditor.Payments.StandardPaymentFunctions.MERGEID_SINGLEPAYMENT)
                    doc.PaymentInfoId = bankSpecific.PaymentInfoId(doc.NumberSeqPaymentFileId, doc.RequestedExecutionDate, rec._PaymentMethod, rec.Currency, true);
                else
                    doc.PaymentInfoId = bankSpecific.PaymentInfoId(doc.NumberSeqPaymentFileId, doc.RequestedExecutionDate, rec._PaymentMethod, rec.Currency);

                doc.PaymentMethod = BaseDocument.PAYMENTMETHOD_TRF;

                PostalAddress debtorAddress = new PostalAddress();
                debtorAddress = bankSpecific.DebtorAddress(company, debtorAddress);
                
                string chargeBearer = bankSpecific.ChargeBearer(rec.ISOPaymentType);

                var creditor = (Creditor)credCache.Get(rec.Account);

                string credName = bankSpecific.CreditorName(creditor._Name);

                var internalAdvText = UnicontaClient.Pages.Creditor.Payments.StandardPaymentFunctions.InternalMessage(credPaymFormat._OurMessage, rec, company, creditor);
                string instructionId = bankSpecific.InstructionId(internalAdvText);

                PostalAddress creditorAddress = new PostalAddress();
                creditorAddress = bankSpecific.CreditorAddress(creditor, creditorAddress);

                string credBankName = string.Empty;
                string credBankCountryId = UnicontaCountryToISO(creditor._Country);  
                string creditorAcc = string.Empty;
                string creditorBIC = string.Empty;
                string creditorOCRPaymentId = string.Empty;
                bool isPaymentTypeIBAN = false;
                bool isOCRPayment = false;

                switch (rec._PaymentMethod)
                {
                    case PaymentTypes.VendorBankAccount:
                        creditorAcc = bankSpecific.CreditorBBAN(rec._PaymentId, creditor._PaymentId, rec._SWIFT);
                        creditorOCRPaymentId = bankSpecific.CreditorRefNumber(rec._PaymentId); 

                        creditorBIC = bankSpecific.CreditorBIC(rec._SWIFT);

                        if (creditorBIC.Length > 6)
                            credBankCountryId = creditorBIC.Substring(4, 2);

                        break;

                    case PaymentTypes.IBAN:
                        isPaymentTypeIBAN = true;
                        creditorAcc = bankSpecific.CreditorIBAN(rec._PaymentId, creditor._PaymentId, company._CountryId, creditor._Country);
                        creditorBIC = bankSpecific.CreditorBIC(rec._SWIFT);
                        creditorOCRPaymentId = bankSpecific.CreditorRefNumberIBAN(rec._PaymentId, company._CountryId, creditor._Country);

                        credBankCountryId = creditorAcc.Substring(0, 2);  
                        break;

                    case PaymentTypes.PaymentMethod3: //FIK71
                        var tuple71 = bankSpecific.CreditorFIK71(rec._PaymentId);
                        creditorOCRPaymentId = tuple71.Item1;
                        creditorOCRPaymentId = BaseDocument.FIK71 + "/" + creditorOCRPaymentId;
                        creditorAcc = tuple71.Item2;
                        break;

                    case PaymentTypes.PaymentMethod5: //FIK75
                        var tuple75 = bankSpecific.CreditorFIK75(rec._PaymentId);
                        creditorOCRPaymentId = tuple75.Item1;
                        creditorOCRPaymentId = BaseDocument.FIK75 + "/" + creditorOCRPaymentId;
                        creditorAcc = tuple75.Item2;
                        break;

                    case PaymentTypes.PaymentMethod4: //FIK73
                        creditorOCRPaymentId = BaseDocument.FIK73 + "/";
                        creditorAcc = bankSpecific.CreditorFIK73(rec._PaymentId);
                        break;

                    case PaymentTypes.PaymentMethod6: //FIK04
                        var tuple04 = bankSpecific.CreditorFIK04(rec._PaymentId);
                        creditorOCRPaymentId = tuple04.Item1;
                        creditorOCRPaymentId = BaseDocument.FIK04 + "/" + creditorOCRPaymentId;
                        creditorAcc = tuple04.Item2;
                        break;
                }

                double amount = 0;
                amount = rec.PaymentAmount;

                doc.ISOPaymentType = bankSpecific.ISOPaymentType(currency, bankAccount._IBAN, isPaymentTypeIBAN ? creditorAcc : string.Empty, creditorBIC, credBankCountryId, doc.CompanyCountryId);
                doc.ExtServiceCode = bankSpecific.ExtServiceCode(doc.ISOPaymentType); 
                doc.ExternalLocalInstrument = bankSpecific.ExternalLocalInstrument(currency, doc.RequestedExecutionDate);
                doc.InstructionPriority = bankSpecific.InstructionPriority();
                doc.ExtCategoryPurpose = bankSpecific.ExtCategoryPurpose(doc.ISOPaymentType);
                doc.ExtProprietaryCode = bankSpecific.ExtProprietaryCode();
                doc.ExcludeSectionCdtrAgt = bankSpecific.ExcludeSectionCdtrAgt(doc.ISOPaymentType, creditorBIC);

                isOCRPayment = string.IsNullOrEmpty(creditorOCRPaymentId) ? false : true;

                var externalAdvText = UnicontaClient.Pages.Creditor.Payments.StandardPaymentFunctions.ExternalMessage(credPaymFormat._Message, rec, company, creditor);

                string remittanceInfo = bankSpecific.RemittanceInfo(externalAdvText, doc.ISOPaymentType, rec._PaymentMethod);

                List<string> unstructuredPaymInfoList = bankSpecific.Ustrd(externalAdvText, doc.ISOPaymentType, rec._PaymentMethod, credPaymFormat._ExtendedText);

                if (!paymentInfoIdLst.Contains(doc.PaymentInfoId))
                {
                    paymentInfoIdLst.Add(doc.PaymentInfoId);
                    doc.PmtInfList.Add(new PmtInf(doc,
                        new PmtTpInf(doc.ExtServiceCode, doc.ExternalLocalInstrument, doc.ExtCategoryPurpose, doc.InstructionPriority, doc.ExtProprietaryCode),
                        new Dbtr(doc.CompanyName, debtorAddress, doc.DebtorIdentificationCode),
                        new DbtrAcct(doc.CompanyCcy, companyAccountId, companyBIC),
                        new DbtrAgt(doc.CompanyBIC, doc.CompanyBankName), doc.ChargeBearer));
                }

                var cdtrAgtCountryId = bankSpecific.CdtrAgtCountryId(credBankCountryId);

                doc.CdtTrfTxInfList.Add(new CdtTrfTxInf(doc.PaymentInfoId, instructionId, doc.EndToEndId, amount, currency,
                    new CdtrAgt(creditorBIC, credBankName, cdtrAgtCountryId, doc.ExcludeSectionCdtrAgt),
                    new Cdtr(credName, creditorAddress),
                    new CdtrAcct(creditorAcc, isPaymentTypeIBAN, isOCRPayment, doc.CompanyCountryId),
                    new RmtInf(unstructuredPaymInfoList, remittanceInfo, creditorOCRPaymentId, isOCRPayment), chargeBearer));
            }

            var generatedFileName = bankSpecific.GenerateFileName(doc.NumberSeqPaymentFileId, doc.CompanyID);
            doc.Validate = schemaValidation;

            XmlDocument creditTransferDoc = doc.CreateXmlDocument();
            
            return new XMLDocumentGenerateResult(creditTransferDoc, (preCheckErrors.Count > 0 || checkErrors.Count > 0), doc, checkErrors, generatedFileName);
        }
    }
}
