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
using Uniconta.Common.Utility;

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
            if (credPaymFormatCache != null)
            {
                var creditorPaymFormatList = (IEnumerable<CreditorPaymentFormat>)credPaymFormatCache.GetNotNullArray;
                var credPaymFormat = (from dl in creditorPaymFormatList where dl._Default == true select dl).FirstOrDefault();
                if (credPaymFormat != null)
                {
                    var credPaymFormatClient = new CreditorPaymentFormatClient();
                    StreamingManager.Copy(credPaymFormat, credPaymFormatClient);
                    return credPaymFormatClient;
                }
            }
            return null;
        }


        /// <summary>
        /// Validate IBAN
        /// </summary>
        public static bool ValidateIBAN(string iban)
        {
#if !SILVERLIGHT
            return Uniconta.DirectDebitPayment.Common.ValidateIBAN(iban);
#else
            return true;
#endif
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
            var sbAdvText = StringBuilderReuse.Create();

            var advText = string.Empty;
            var country = company._CountryId;
            var tuple = MessageLabel(country);
            
            if (messageFormat != null)
            {
                sbAdvText.Append(messageFormat);

                if (rec.invoiceNumbers != null && rec.MergePaymId != MERGEID_SINGLEPAYMENT)
                {
                    sbAdvText.Remove("Fak:%1").Trim(); //Removes invoicenumbers for merge payment due to limitation of characters 
                    sbAdvText.Remove("Fak: %1").Trim();
                    sbAdvText.Remove("%1").Trim();
                }

                sbAdvText.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}").Replace("%5", "{4}");
                advText = string.Format(sbAdvText.ToStringAndRelease(), rec.InvoiceAN, creditor?._Account, creditor?._Name, NumberConvert.ToStringNull(rec.Voucher), NumberConvert.ToStringNull(rec.PaymentRefId));
            }
            else //Default message
            {
                BuildBankAdviceText(sbAdvText, NumberConvert.ToString(rec.PaymentEndToEndId), tuple.Item3);
                if (rec.invoiceNumbers == null || rec.MergePaymId == MERGEID_SINGLEPAYMENT)
                    BuildBankAdviceText(sbAdvText, rec.InvoiceAN ?? string.Empty, tuple.Item2);

                BuildBankAdviceText(sbAdvText, creditor?._Account, tuple.Item1);
                
                advText = sbAdvText.ToStringAndRelease();
            }

            return advText;
        }

      
        public static string ExternalMessage(CreditorPaymentFormat credPaymFormat, CreditorTransPayment rec, Company company, Uniconta.DataModel.Creditor cred, bool UIMessage = false)
        {
            var sbAdvText = StringBuilderReuse.Create();

            var creditor = (CreditorClient)cred;

            string advText;
            var country = creditor == null || creditor._Country == CountryCode.Unknown ? company._CountryId : creditor._Country;
            var tuple = MessageLabel(country);

            if (UIMessage == false && rec.invoiceNumbers != null && rec.MergePaymId != MERGEID_SINGLEPAYMENT)
            {
                var invNumbers = rec.invoiceNumbers.ToString();
                var invNumbersCheck = Regex.Replace(invNumbers, "[^1-9]", "");

                BuildBankAdviceText(sbAdvText, creditor?._OurAccount, tuple.Item1);
                BuildBankAdviceText(sbAdvText, invNumbersCheck == string.Empty ? invNumbersCheck : invNumbers, tuple.Item2);
                if (sbAdvText.Length == 0)
                    sbAdvText.Append(company.Name);
                advText = sbAdvText.ToStringAndRelease();
            }
            else
            {
                if (!string.IsNullOrEmpty(rec._Message))
                {
                    sbAdvText.Release();
                    return rec._Message;
                }

                var messageFormat = creditor.BankMessage ?? creditor.CreditorGroup?.BankMessage ?? credPaymFormat?._Message ?? null;
                if (messageFormat != null)
                {
                    sbAdvText.Append(messageFormat);

                    if (rec.InvoiceAN == null && sbAdvText.IndexOf("%1") >= 0)
                        MessageFormatRemove(sbAdvText, "%1");

                    if (company.Name == null && sbAdvText.IndexOf("%2") >= 0)
                        MessageFormatRemove(sbAdvText, "%2");

                    if (rec.CashDiscount == 0 && sbAdvText.IndexOf("%3") >= 0)
                        MessageFormatRemove(sbAdvText, "%3");

                    if (creditor?._OurAccount == null && sbAdvText.IndexOf("%4") >= 0)
                        MessageFormatRemove(sbAdvText, "%4");

                    if (rec.TransType == null && sbAdvText.IndexOf("%5") >= 0)
                        MessageFormatRemove(sbAdvText, "%5");

                    advText = string.Format(sbAdvText.Replace("%1", "{0}").Replace("%2", "{1}").Replace("%3", "{2}").Replace("%4", "{3}").Replace("%5", "{4}").ToStringAndRelease(),
                                             rec.InvoiceAN,
                                             company.Name,
                                             rec.CashDiscount,
                                             creditor?._OurAccount,
                                             rec.TransType);
                }
                else if (UIMessage == false) //Default message
                {
                    BuildBankAdviceText(sbAdvText, creditor?._OurAccount, tuple.Item1);
                    BuildBankAdviceText(sbAdvText, rec.InvoiceAN, tuple.Item2);
                    BuildBankAdviceText(sbAdvText, company.Name);
                    advText = sbAdvText.ToStringAndRelease();
                }
                else
                {
                    sbAdvText.Release();
                    advText = string.Empty;
                }
            }

            return advText;
        }

        private static void MessageFormatRemove(StringBuilderReuse msgFormat, string parmValue)
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
            msgFormat.Remove(pos, fromPos + parmValLen - pos).Trim();
        }

        private static void BuildBankAdviceText(StringBuilderReuse advText, string inputValue, string prefix = null)
        {
            if (inputValue != null && inputValue != string.Empty)
            {
                advText = advText.Length != 0 ? advText.Append(" ") : advText;
                if (prefix == null)
                    advText.Append(inputValue);
                else
                    advText.Append(prefix).Append(':').Append(inputValue);

            }
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

        public static void ParseOcr(string ocr, out string fiCreditor, out string fiMask, bool isGIRO04 = false)
        {
            fiCreditor = null;
            fiMask = null;

            if (string.IsNullOrWhiteSpace(ocr))
                return;

            ocr = ocr.Trim().TrimStart('+');
            var idx = ocr.IndexOf('+');
            if (idx != -1)
            {
                fiCreditor = Regex.Replace(ocr.Substring(idx), @"[^\d]", "");
                fiMask = Regex.Replace(ocr.Substring(0, idx), @"^.*?<", "").Trim();
            }
            else if (ocr.Length > 0)
            {
                fiCreditor = Regex.Replace(ocr, @"[^\d]", "");
                fiCreditor = !isGIRO04 && fiCreditor.Length != 8 ? null : fiCreditor;
            }
        }
    }
}
