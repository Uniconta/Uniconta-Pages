using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments
{
    public static class StandardPaymentFunctions
    {
        /// <summary>
        /// It returns the default PaymentFormat
        /// </summary>
        /// <param name="comp">Company</param>
        /// <returns></returns>
        public static CreditorPaymentFormatClient GetDefaultCreditorPaymentFormat(SQLCache credPaymFormatCache)
        {
            var creditorPaymFormatList = (IEnumerable<CreditorPaymentFormat>)credPaymFormatCache.GetNotNullArray;

            var credPaymFormat = (from dl in creditorPaymFormatList where dl._Default == true select dl).FirstOrDefault();
            if (credPaymFormat != null)
            {
                var credPaymFormatClient = new CreditorPaymentFormatClient();
                StreamingManager.Copy(credPaymFormat, credPaymFormatClient);
                return credPaymFormatClient;
            }
            return null;
        }


        /// <summary>
        /// Validate IBAN
        /// </summary>
        public static bool ValidateIBAN(string iban)
        {
            iban = iban ?? string.Empty;
            iban = iban.ToUpper();

            if (System.Text.RegularExpressions.Regex.IsMatch(iban, "^[A-Z0-9]"))
            {
                iban = iban.Replace(" ", String.Empty);
                string bank = iban.Substring(4, iban.Length - 4) + iban.Substring(0, 4);
                int asciiShift = 55;
                StringBuilder sb = new StringBuilder();
                foreach (char c in bank)
                {
                    int v;
                    if (Char.IsLetter(c))
                    {
                        v = c - asciiShift;
                    }
                    else
                    {
                        v = int.Parse(c.ToString());
                    }
                    sb.Append(v);
                }
                string checkSumString = sb.ToString();
                int checksum = int.Parse(checkSumString.Substring(0, 1));
                for (int i = 1; i < checkSumString.Length; i++)
                {
                    int v = int.Parse(checkSumString.Substring(i, 1));
                    checksum *= 10;
                    checksum += v;
                    checksum %= 97;
                }
                return checksum == 1;
            }
            else
                return false;
        }

        /// <summary>
        /// Validate BIC  
        /// </summary>
        public static bool ValidateBIC(string bic)
        {
            bic = bic ?? string.Empty;
            bic = bic.ToUpper();

            if (System.Text.RegularExpressions.Regex.IsMatch(bic, "^([a-zA-Z]){4}([a-zA-Z]){2}([0-9a-zA-Z]){2}([0-9a-zA-Z]{3})?$"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public static string MessageFormat(string messageFormat, CreditorTransPayment rec, Company company, Uniconta.DataModel.Creditor creditor)
        {
            if (messageFormat == null || rec._Message != null)
                return rec._Message;

            var format = messageFormat.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}");
            var text = string.Format(format, rec.Invoice == 0 ? string.Empty : rec.Invoice.ToString(), company.Name, rec.CashDiscount, creditor._OurAccount);

            return text;
        }

        private static Tuple<string, string, string> MessageLabel(CountryCode country)
        {
            string label_Acc, label_Inv, label_Ref;
            if (country == CountryCode.Denmark || country == CountryCode.Greenland || country == CountryCode.FaroeIslands)
            {
                label_Acc = "Kto";
                label_Inv = "Fak";
                label_Ref = "Ref";
            }
            else if (country == CountryCode.Germany || country == CountryCode.Switzerland || country == CountryCode.Austria)
            {
                label_Acc = "Kto";
                label_Inv = "RG";
                label_Ref = "Ref";
            }
            else
            {
                label_Acc = "Acc:";
                label_Inv = "Inv:";
                label_Ref = "Ref:";
            }

            return Tuple.Create(label_Acc, label_Inv, label_Ref);
        }

        public static string InternalMessage(string messageFormat, CreditorTransPayment rec, Company company, Uniconta.DataModel.Creditor creditor)
        {
            StringBuilder sbAdvText = new StringBuilder();

            var advText = string.Empty;
            var country = company._CountryId;
            var tuple = MessageLabel(country);
            
            if (messageFormat != null)
            {
                var format = string.Empty;

                if (rec.invoiceNumbers != null)
                {
                    messageFormat = messageFormat.Replace("Fak:%1", "").Trim(); //Removes invoicenumbers for merge payment due to limitation of characters 
                    messageFormat = messageFormat.Replace("Fak: %1", "").Trim();
                    messageFormat = messageFormat.Replace("%1", "").Trim();
                }

                format = messageFormat.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}").Replace("%5", "{4}");
                advText = string.Format(format, rec.Invoice == 0 ? string.Empty : rec.Invoice.ToString(), creditor?._Account, creditor?._Name, rec.Voucher == 0 ? string.Empty : rec.Voucher.ToString(), rec.PaymentRefId == 0 ? string.Empty : rec.PaymentRefId.ToString());
            }
            else //Default message
            {
                if (rec.invoiceNumbers == null) //Merged payments
                    sbAdvText = BuildBankAdviceText(sbAdvText, rec.Invoice == 0 ? string.Empty : rec.Invoice.ToString(), tuple.Item2);
                sbAdvText = BuildBankAdviceText(sbAdvText, creditor?._Account, tuple.Item1);
                sbAdvText = BuildBankAdviceText(sbAdvText, rec.PaymentRefId.ToString(), tuple.Item3);
                advText = sbAdvText.ToString();

            }

            return advText;
        }

        public static string ExternalMessage(string messageFormat, CreditorTransPayment rec, Company company, Uniconta.DataModel.Creditor creditor)
        {
            StringBuilder sbAdvText = new StringBuilder();

            var advText = string.Empty;
            var country = company._CountryId;
            var tuple = MessageLabel(country);

            if (rec.invoiceNumbers != null) //Merged payments
            {
                sbAdvText = BuildBankAdviceText(sbAdvText, creditor?._OurAccount, tuple.Item1);
                sbAdvText = BuildBankAdviceText(sbAdvText, rec.invoiceNumbers.ToString(), tuple.Item2);
                advText = sbAdvText.ToString();
            }
            else
            {
                if (rec._Message != null && rec._Message != string.Empty)
                {
                    advText = rec._Message;
                }
                else if (messageFormat != null)
                { 
                    var format = messageFormat.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}");
                    advText = string.Format(format, rec.Invoice == 0 ? string.Empty : rec.Invoice.ToString(), company.Name, rec.CashDiscount, creditor?._OurAccount);
                }
                else //Default message
                {
                    sbAdvText = BuildBankAdviceText(sbAdvText, creditor?._OurAccount, tuple.Item1);
                    sbAdvText = BuildBankAdviceText(sbAdvText, rec.Invoice == 0 ? string.Empty : rec.Invoice.ToString(), tuple.Item2);
                    sbAdvText = BuildBankAdviceText(sbAdvText, company.Name);
                    advText = sbAdvText.ToString();
                }
            }

            return advText;
        }

        private static StringBuilder BuildBankAdviceText(StringBuilder advText, string inputValue, string prefix = null)
        {
            if (inputValue != null && inputValue != string.Empty)
            {
                advText = advText.Length != 0 ? advText.Append(" ") : advText;
                if (prefix == null)
                    advText.AppendFormat("{0}", inputValue);
                else
                    advText.AppendFormat("{0}:{1}", prefix, inputValue);

            }

            return advText;
        }
    }
}
