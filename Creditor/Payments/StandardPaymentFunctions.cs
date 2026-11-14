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

        /// <summary>
        /// Foreign payments in the old Danish bank formats - the banks phase them out from November 2026 (ISO 20022 required)
        /// </summary>
        public static bool ForeignPaymentInOldFormat(ExportFormatType exportFormat, IEnumerable<CreditorTransPayment> paymentList)
        {
            if (!OldDanishFormat(exportFormat))
                return false;

            // Bankdata, SDC and BEC always create IBAN payments as foreign transfers in the old formats - also to a Danish IBAN
            var ibanIsForeign = exportFormat == ExportFormatType.BankData || exportFormat == ExportFormatType.SDC || exportFormat == ExportFormatType.BEC_CSV;

            foreach (var rec in paymentList)
            {
                if (rec._PaymentMethod != PaymentTypes.IBAN && rec._PaymentMethod != PaymentTypes.VendorBankAccount)
                    continue;

                if (ibanIsForeign && rec._PaymentMethod == PaymentTypes.IBAN)
                    return true;

                var countryId = AccountCountryId(rec);
                if (countryId != string.Empty && countryId != "DK")
                    return true;
            }

            return false;
        }

        static readonly DateTime OldFormatDomesticDiscontinuedDate = new DateTime(2027, 3, 1);

        /// <summary>
        /// Domestic account transfers in the old Danish bank formats - the banks phase them out in spring 2027 (Bankdata 31-03-2027, STEP2 DKK/NPC Q2 2027)
        /// </summary>
        public static bool DomesticPaymentInOldFormat(ExportFormatType exportFormat, IEnumerable<CreditorTransPayment> paymentList)
        {
            if (!OldDanishFormat(exportFormat))
                return false;

            foreach (var rec in paymentList)
            {
                if (rec._PaymentMethod != PaymentTypes.IBAN && rec._PaymentMethod != PaymentTypes.VendorBankAccount)
                    continue;

                var countryId = AccountCountryId(rec);
                if ((countryId == string.Empty || countryId == "DK") && rec._PaymentDate >= OldFormatDomesticDiscontinuedDate)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Merged payment advice ending with a comma separated invoice list: cut at the last whole invoice number and add " +N" (number of omitted invoices).
        /// Returns null if no invoice fits.
        /// </summary>
        public static string TruncateInvoiceList(string text, int maxLength, out int omitted)
        {
            omitted = 0;
            if (text == null || text.Length <= maxLength)
                return text;

            for (int pos = text.LastIndexOf(',', Math.Min(maxLength, text.Length - 1)); pos > 0; pos = text.LastIndexOf(',', pos - 1))
            {
                int count = 0;
                for (int i = pos; i < text.Length; i++)
                    if (text[i] == ',')
                        count++;

                var suffix = string.Concat(" +", count.ToString());
                if (pos + suffix.Length <= maxLength)
                {
                    omitted = count;
                    return string.Concat(text.Substring(0, pos), suffix);
                }
            }

            return null;
        }

        static bool OldDanishFormat(ExportFormatType exportFormat)
        {
            switch (exportFormat)
            {
                case ExportFormatType.Nordea_CSV:
                case ExportFormatType.DanskeBank_CSV:
                case ExportFormatType.BankData:
                case ExportFormatType.SDC:
                case ExportFormatType.BEC_CSV:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Country of the creditor account - from IBAN, otherwise from BIC
        /// </summary>
        static string AccountCountryId(CreditorTransPayment rec)
        {
            if (rec._PaymentMethod == PaymentTypes.IBAN)
            {
                var iban = Regex.Replace(rec._PaymentId ?? string.Empty, "[^\\w\\d]", "");
                if (iban.Length >= 2)
                    return iban.Substring(0, 2).ToUpper();
            }

            var swift = Regex.Replace(rec._SWIFT ?? string.Empty, "[^\\w\\d]", "");
            return swift.Length >= 6 ? swift.Substring(4, 2).ToUpper() : string.Empty;
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

            return result.Trim();
        }

        const string UKPostCode = @"[A-Z]{1,2}\d[A-Z\d]?\s*\d[A-Z]{2}";

        /// <summary>
        /// Postal code pattern per country - null if the postal code format is unknown
        /// </summary>
        static string ZipCodeCityPattern(CountryCode country)
        {
            switch (country)
            {
                case CountryCode.Denmark:
                case CountryCode.Greenland:
                case CountryCode.Norway:
                case CountryCode.Switzerland:
                case CountryCode.Austria: return @"^(?:[A-Z]{2}-)?(?<zip>\d{4})\s*(?<city>[^\d\s].*)$";
                case CountryCode.FaroeIslands:
                case CountryCode.Iceland: return @"^(?:[A-Z]{2}-)?(?<zip>\d{3})\s*(?<city>[^\d\s].*)$";
                case CountryCode.Sweden: return @"^(?:[A-Z]{2}-)?(?<zip>\d{3}\s?\d{2})\s*(?<city>[^\d\s].*)$";
                case CountryCode.Finland:
                case CountryCode.Germany:
                case CountryCode.Estonia:
                case CountryCode.Lithuania: return @"^(?:[A-Z]{2}-)?(?<zip>\d{5})\s*(?<city>[^\d\s].*)$";
                case CountryCode.Netherlands: return @"^(?:[A-Z]{2}-)?(?<zip>\d{4}\s?[A-Z]{2})\s+(?<city>.+)$";
                case CountryCode.UnitedKingdom: return @"^(?<zip>" + UKPostCode + @")\s+(?<city>.+)$|^(?<city>.+?)[,\s]+(?<zip>" + UKPostCode + ")$";
                default: return null;
            }
        }

        /// <summary>
        /// Postal code and city from an address line like "8000 Aarhus C", "1234 AB Amsterdam" or "London SW1A 1AA".
        /// Returns null if the line doesn't match the postal code format of the country.
        /// </summary>
        public static Tuple<string, string> GetZipCodeCity(string adr, CountryCode country)
        {
            var pattern = ZipCodeCityPattern(country);
            if (pattern == null || string.IsNullOrWhiteSpace(adr))
                return null;

            var match = Regex.Match(adr.Trim(), pattern, RegexOptions.IgnoreCase);
            if (!match.Success)
                return null;

            var city = Regex.Replace(match.Groups["city"].Value, "[,;]", "").Trim();
            if (city.Length == 0)
                return null;

            return new Tuple<string, string>(match.Groups["zip"].Value.Trim(), city);
        }

        /// <summary>
        /// Postal code and city of the company, which must be in Address 3 or Address 2.
        /// For countries with an unknown postal code format the last filled line is used as city (without postal code).
        /// lineNo returns the line used (3 or 2)
        /// </summary>
        public static Tuple<string, string> CompanyZipCodeCity(string adr2, string adr3, CountryCode country, out int lineNo)
        {
            lineNo = 0;
            if (ZipCodeCityPattern(country) == null)
            {
                if (!string.IsNullOrWhiteSpace(adr3))
                {
                    lineNo = 3;
                    return new Tuple<string, string>(null, adr3.Trim());
                }
                if (!string.IsNullOrWhiteSpace(adr2))
                {
                    lineNo = 2;
                    return new Tuple<string, string>(null, adr2.Trim());
                }
                return null;
            }

            var zipCity = GetZipCodeCity(adr3, country);
            if (zipCity != null)
            {
                lineNo = 3;
                return zipCity;
            }

            zipCity = GetZipCodeCity(adr2, country);
            if (zipCity != null)
                lineNo = 2;
            return zipCity;
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
