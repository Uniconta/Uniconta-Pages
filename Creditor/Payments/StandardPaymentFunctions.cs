using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments
{
    public static class StandardPaymentFunctions
    {
        public const string MERGEID_SINGLEPAYMENT = "-";
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

            if (System.Text.RegularExpressions.Regex.IsMatch(iban, "^[A-Z0-9]") && iban.Length > 3)
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
                label_Acc = "Acc";
                label_Inv = "Inv";
                label_Ref = "Ref";
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

                if (rec.invoiceNumbers != null && rec._MergePaymId != MERGEID_SINGLEPAYMENT)
                {
                    messageFormat = messageFormat.Replace("Fak:%1", "").Trim(); //Removes invoicenumbers for merge payment due to limitation of characters 
                    messageFormat = messageFormat.Replace("Fak: %1", "").Trim();
                    messageFormat = messageFormat.Replace("%1", "").Trim();
                }

                format = messageFormat.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}").Replace("%5", "{4}");
                advText = string.Format(format, rec.Invoice == 0 ? string.Empty : rec.Invoice.ToString(), creditor?._Account, creditor?._Name, rec.Voucher == 0 ? string.Empty : rec.Voucher.ToString(), rec.PaymentRefId == 0 ? string.Empty : rec.PaymentEndToEndId.ToString());
            }
            else //Default message
            {
                if (rec.invoiceNumbers == null || rec._MergePaymId == MERGEID_SINGLEPAYMENT)
                    sbAdvText = BuildBankAdviceText(sbAdvText, rec.Invoice == 0 ? string.Empty : rec.Invoice.ToString(), tuple.Item2);
                sbAdvText = BuildBankAdviceText(sbAdvText, creditor?._Account, tuple.Item1);
                sbAdvText = BuildBankAdviceText(sbAdvText, rec.PaymentEndToEndId.ToString(), tuple.Item3);
                advText = sbAdvText.ToString();

            }

            return advText;
        }

      
        public static string ExternalMessage(string messageFormat, CreditorTransPayment rec, Company company, Uniconta.DataModel.Creditor creditor, bool UIMessage = false)
        {
            StringBuilder sbAdvText = new StringBuilder();

            var advText = string.Empty;
            var country = company._CountryId;
            var tuple = MessageLabel(country);

            if (UIMessage == false && rec.invoiceNumbers != null && rec._MergePaymId != MERGEID_SINGLEPAYMENT)
            {
                var invNumbers = rec.invoiceNumbers.ToString();
                var invNumbersCheck = Regex.Replace(invNumbers, "[^1-9]", "");

                sbAdvText = BuildBankAdviceText(sbAdvText, creditor?._OurAccount, tuple.Item1);
                sbAdvText = BuildBankAdviceText(sbAdvText, invNumbersCheck == string.Empty ? invNumbersCheck : invNumbers, tuple.Item2);
                if (sbAdvText.Length == 0)
                    sbAdvText.Append(company.Name);
                advText = sbAdvText.ToString();
            }
            else
            {
                if (!string.IsNullOrEmpty(rec._Message))
                    return rec._Message;
            
                if (messageFormat != null)
                {
                    var parmCode = "%1";
                    if (messageFormat.IndexOf(parmCode) != -1 && rec.Invoice == 0)
                        messageFormat = MessageFormatRemove(messageFormat, parmCode);

                    parmCode = "%2";
                    if (messageFormat.IndexOf(parmCode) != -1 && company.Name == null)
                        messageFormat = MessageFormatRemove(messageFormat, parmCode);

                    parmCode = "%3";
                    if (messageFormat.IndexOf(parmCode) != -1 && rec.CashDiscount == 0)
                        messageFormat = MessageFormatRemove(messageFormat, parmCode);

                    parmCode = "%4";
                    if (messageFormat.IndexOf(parmCode) != -1 && creditor._OurAccount == null)
                        messageFormat = MessageFormatRemove(messageFormat, parmCode);

                    parmCode = "%5";
                    if (messageFormat.IndexOf(parmCode) != -1 && rec.TransType == null)
                        messageFormat = MessageFormatRemove(messageFormat, parmCode);

                    advText = string.Format(messageFormat.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}").Replace("%5", "{4}"),
                                             rec.Invoice.ToString(),
                                             company.Name,
                                             rec.CashDiscount,
                                             creditor._OurAccount,
                                             rec.TransType);
                }
                else if (UIMessage == false) //Default message
                {
                    sbAdvText = BuildBankAdviceText(sbAdvText, creditor?._OurAccount, tuple.Item1);
                    sbAdvText = BuildBankAdviceText(sbAdvText, rec.Invoice == 0 ? string.Empty : rec.Invoice.ToString(), tuple.Item2);
                    sbAdvText = BuildBankAdviceText(sbAdvText, company.Name);
                    advText = sbAdvText.ToString();
                }
            }

            return advText;
        }

        private static string MessageFormatRemove(string msgFormat, string parmValue)
        {
            int msgFormatLen = msgFormat.Length;
            int parmValLen = parmValue.Length;
            int fromPos = msgFormat.IndexOf(parmValue);

            int pos = 0;
            for (int i = fromPos - 1; i >= 0; i--)
            {
                if (msgFormat[i] == '%')
                {
                    int cnt = 0;
                    while (msgFormatLen > i + cnt)
                    {
                        cnt++;
                        if (!char.IsDigit(msgFormat[i + cnt]))
                            break;
                    }
                    pos = i + cnt;
                    break;
                }
            }

            return msgFormat.Remove(pos, fromPos + parmValLen - pos).Trim();
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

        public static string RegularExpressionReplace(string text, string pattern, Dictionary<string, string> replaceDict = null)
        {
            if (text == null)
                return null;

            var result = text;
            if (replaceDict != null)
            {
                var patternDict = $"(?<placeholder>{string.Join("|", replaceDict.Keys)})";
                result = Regex.Replace(result, patternDict, m => replaceDict[m.Groups["placeholder"].Value], RegexOptions.ExplicitCapture);
            }

            var regex = new Regex(pattern);
            result = regex.Replace(result, "");

            return result;
        }
    }
}
