using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UnicontaClient.Pages;
using ISO20022CreditTransfer;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaISO20022CreditTransfer;
using Localization = Uniconta.ClientTools.Localization;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public class BankFormatBase
    {
        public const string FIK04 = "04";
        public const string FIK71 = "71";
        public const string FIK73 = "73";
        public const string FIK75 = "75";

        public const string FI = "FI";
        public const string GIRO = "GIRO";

        public void SharedCodeForCreateBankFormatFields(Company company, CreditorTransPayment tran, CreditorPaymentFormat paymentFormat, BankStatement bankAccount, DanishFormatFieldBase danishFields)
        {
            danishFields.TransferDate = tran.PaymentDate;
            danishFields.Currency = tran.Trans._Currency != 0 ? (Currencies)tran.Trans._Currency : company._CurrencyId;
            
            var regNum = bankAccount._BankAccountPart1 ?? string.Empty;
            var bban = bankAccount._BankAccountPart2 ?? string.Empty;
            
            regNum = Regex.Replace(regNum, "[^0-9]", "");
            bban = Regex.Replace(bban, "[^0-9]", "");
            bban = bban.PadLeft(10, '0');

            danishFields.FromAccountNumber = string.Format("{0}{1}", regNum, bban);
        }

        public void SharedForeignReceiverBankInfo(DanishFormatFieldBase field, CreditorTransPayment tran)
        {
            var countryId = string.Empty;
            var iban = string.Empty;
            var bban = string.Empty;

            if (tran._PaymentMethod == PaymentTypes.IBAN)
            {
                field.FormType = "IBAN";
                iban = tran.PaymentId ?? string.Empty;
                if (iban != string.Empty)
                {
                    iban = Regex.Replace(iban, "[^\\w\\d]", "");
                    iban = iban.ToUpper();
                    countryId = iban.Substring(0, 2);
                }
            }
            else
            {
                field.FormType = "Vendor Bank Account";
                bban = tran.PaymentId ?? string.Empty;
                bban = Regex.Replace(bban, "[^0-9]", "");
            }

            var swift = tran._SWIFT ?? string.Empty;
            if (swift != string.Empty)
            {
                swift = Regex.Replace(swift, "[^\\w\\d]", "");
                swift = swift.ToUpper();
                if (swift.Length > 6)
                    countryId = countryId == string.Empty ? swift.Substring(4, 2) : countryId;
            }

            field.SwiftAddress = swift;
            field.ReceiverIBAN = iban;
            field.ToAccountNumber = bban;
            field.CountryCode = countryId;
        }

        public void SharedFIKPayment(DanishFormatFieldBase field, CreditorTransPayment tran)
        {
            var ocrLine = tran.PaymentId;

            ocrLine = ocrLine.Replace(" ", "");
            ocrLine = ocrLine.Replace("+71<", "");
            ocrLine = ocrLine.Replace(">71<", "");
            ocrLine = ocrLine.Replace("+73<", "");
            ocrLine = ocrLine.Replace(">73<", "");
            ocrLine = ocrLine.Replace("+75<", "");
            ocrLine = ocrLine.Replace(">75<", "");
            ocrLine = ocrLine.Replace("+04<", "");
            ocrLine = ocrLine.Replace(">04<", "");
            ocrLine = ocrLine.Replace("<", "");
            ocrLine = ocrLine.Replace(">", "");

            var paymID = string.Empty;
            var creditorAccount = string.Empty;

            if (tran._PaymentMethod == PaymentTypes.PaymentMethod4) //FIK73
            {
                ocrLine = ocrLine.Replace("+", "");
                creditorAccount = ocrLine;
            }
            else
            {
                int index = ocrLine.IndexOf("+");
                if (index > 0)
                {
                    paymID = ocrLine.Substring(0, index);
                    creditorAccount = ocrLine.Remove(0, index + 1);
                }
            }

            switch (tran._PaymentMethod)
            {
                case PaymentTypes.PaymentMethod3:
                    field.FormType = BankFormatBase.FIK71;
                    if (paymID.Length > BaseDocument.FIK71LENGTH)
                        paymID = paymID.Substring(paymID.Length - BaseDocument.FIK71LENGTH, BaseDocument.FIK71LENGTH);
                    else
                        paymID = paymID.PadLeft(BaseDocument.FIK71LENGTH, '0');
                    break;

                case PaymentTypes.PaymentMethod4:
                    field.FormType = BankFormatBase.FIK73;

                    break;
                case PaymentTypes.PaymentMethod5:
                    field.FormType = BankFormatBase.FIK75;
                    if (paymID.Length > BaseDocument.FIK75LENGTH)
                        paymID = paymID.Substring(paymID.Length - BaseDocument.FIK75LENGTH, BaseDocument.FIK75LENGTH);
                    else
                        paymID = paymID.PadLeft(BaseDocument.FIK75LENGTH, '0');
                    break;

                case PaymentTypes.PaymentMethod6:
                    field.FormType = BankFormatBase.FIK04;
                    if (paymID.Length > BaseDocument.FIK04LENGTH)
                        paymID = paymID.Substring(paymID.Length-BaseDocument.FIK04LENGTH, BaseDocument.FIK04LENGTH);
                    else
                        paymID = paymID.PadLeft(BaseDocument.FIK04LENGTH, '0');
                    break;
            }

            field.PaymentId = paymID; 
            field.ToAccountNumber = creditorAccount;
        }

        public string ShortenWordToCriteria(string value, int lengthAllowed)
        {
            string returnString = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
                return returnString;
            else
                returnString = value;


            if (returnString.Length > lengthAllowed)
            {
                returnString = returnString.Remove(lengthAllowed);
            }
            return returnString;
        }

        /// <summary>
        /// DOMESTIC Payment:
        /// Transfers within the same country.
        /// 
        /// SEPA Payment:
        /// The conditions for a SEPA payment
        /// 1.Creditor payment has currency code 'EUR'
        /// 2.Sender - Bank og Receiver-Bank has to be member of the  European Economic Area.
        /// 3.Creditor account has to be IBAN
        /// 4.Payment must be Non-urgent
        /// 
        /// CROSS BORDER Payment:
        /// 
        /// </summary>
        public ISO20022PaymentTypes ISOPaymentType(string paymentCcy, BankStatement _bankStatement, string creditorIBAN, Company company)
        {
            var companyIBAN = _bankStatement._IBAN ?? string.Empty;
            var companySWIFT = _bankStatement._SWIFT ?? string.Empty;

            var companyBankCountryId = string.Empty;
            if (companyIBAN != string.Empty)
                companyBankCountryId = companyIBAN.Substring(0, 2);
            else if (companySWIFT != string.Empty)
                companyBankCountryId = companySWIFT.Substring(4, 2);
            else
                companyBankCountryId = company._CountryId.ToString();

            creditorIBAN = creditorIBAN ?? string.Empty;

            if (creditorIBAN != string.Empty && companyBankCountryId != string.Empty)
            {
                var creditorBankCountryId = creditorIBAN.Substring(0, 2);

                //SEPA payment:
                if (paymentCcy == BaseDocument.CCYEUR)
                {
                    if (SEPACountry(companyBankCountryId) && SEPACountry(creditorBankCountryId))
                        return ISO20022PaymentTypes.SEPA;
                }
                else if (companyBankCountryId == creditorBankCountryId)
                {
                    return ISO20022PaymentTypes.DOMESTIC;
                }
                else
                {
                    return ISO20022PaymentTypes.CROSSBORDER;
                }
            }

            return ISO20022PaymentTypes.DOMESTIC;
        }

        public bool SEPACountry(string countryId)
        {
            return Enum.IsDefined(typeof(SEPACountries), countryId);
        }
     }
}