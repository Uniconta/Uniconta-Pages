using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UnicontaClient.Pages;
using DevExpress.Xpf.CodeView;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using Localization = Uniconta.ClientTools.Localization;
using System.Text.RegularExpressions;
using UnicontaClient.Creditor.Payments;
using UnicontaISO20022CreditTransfer;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public partial class CreateNordeaFileFormatBase : BankFormatBase
    {
        public DanishFormatFieldBase CreateFormatField(CreditorTransPayment tran,
            CreditorPaymentFormat paymentFormat,
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company,
            bool glJournalGenerated = false)
        {
            var danishFields = new NordeaFormatFields();

            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);

            danishFields.RecordType = "0";

            danishFields.NotUsed = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            danishFields.BankCode = string.Empty;

            switch (tran._PaymentMethod)
            {
                case PaymentTypes.PaymentMethod3: //FIK71
                    SharedFIKPayment(danishFields, tran);
                    danishFields.TransTypeCommand = NordeaPaymentFormat.TRANSTYPE_46;
                    break;
                case PaymentTypes.PaymentMethod4: //FIK73
                    SharedFIKPayment(danishFields, tran);
                    danishFields.TransTypeCommand = NordeaPaymentFormat.TRANSTYPE_46;
                    break;
                case PaymentTypes.PaymentMethod5: //FIK75
                    SharedFIKPayment(danishFields, tran);
                    danishFields.TransTypeCommand = NordeaPaymentFormat.TRANSTYPE_46;
                    break;
                case PaymentTypes.PaymentMethod6: //FIK04
                    SharedFIKPayment(danishFields, tran);
                    danishFields.TransTypeCommand = NordeaPaymentFormat.TRANSTYPE_46;
                    break;
                case PaymentTypes.VendorBankAccount:
                    BBANIBANPaymentType(danishFields, tran);
                    break;
                case PaymentTypes.IBAN:
                    BBANIBANPaymentType(danishFields, tran);
                    danishFields.ToAccountNumber = danishFields.ReceiverIBAN;
                    break;
            }

            danishFields.NameOfReceiver = glJournalGenerated ? string.Empty : ShortenWordToCriteria(tran.Creditor.Name, 35);

            var address = string.Empty;
            if (glJournalGenerated == false)
            {
                List<addressFormat> listAddress = new List<addressFormat>()
                {
                    new addressFormat() { AddressStr = tran.Creditor.Address1 },
                    new addressFormat() { AddressStr = tran.Creditor.Address2 },
                    new addressFormat() { AddressStr = tran.Creditor.Address3 },
                };

                address = string.Join(", ", listAddress.Where(l => !string.IsNullOrEmpty(l.AddressStr)).Select(l => l.AddressStr.Trim()));
            }

            danishFields.AddressOfReceiver = glJournalGenerated ? string.Empty : ShortenWordToCriteria(string.Format("{0}", address), 35);
            danishFields.AddressOfReceiver2 = glJournalGenerated ? string.Empty : ShortenWordToCriteria(string.Format("{0} {1}", tran.Creditor.ZipCode, tran.Creditor.City), 35);
            danishFields.AddressOfReceiver3 = glJournalGenerated ? string.Empty : ShortenWordToCriteria(string.Format("{0}", tran.Creditor.Country), 35);

            danishFields.NotUsed02 = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            if (danishFields.TransferDate.DayOfWeek == DayOfWeek.Saturday)
                danishFields.TransferDate = danishFields.TransferDate.AddDays(2);
            else if (danishFields.TransferDate.DayOfWeek == DayOfWeek.Sunday)
                danishFields.TransferDate = danishFields.TransferDate.AddDays(1);

            var invoiceNumber = tran.invoiceNumbers == null ? tran.Invoice.ToString() : tran.invoiceNumbers.ToString();
            invoiceNumber = invoiceNumber == "0" ? string.Empty : string.Format("Faknr:{0}", invoiceNumber);

            var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat, tran, company, creditor);
            var message = externalAdvText;

            //Extended notification
            if (danishFields.TransTypeCommand != NordeaPaymentFormat.TRANSTYPE_49)
            {
                if (paymentFormat._ExtendedText)
                {
                    if (message == null || message.Length <= 20)
                        message = string.Empty;
                }
                else
                {
                    message = string.Empty;
                }
            }

            int maxStrLen = 35;
            int maxLines = 4;

            if (tran._PaymentMethod == PaymentTypes.PaymentMethod4 || tran._PaymentMethod == PaymentTypes.PaymentMethod5 || 
                tran._PaymentMethod == PaymentTypes.VendorBankAccount || tran._PaymentMethod == PaymentTypes.IBAN) 
            {
                message = NETSNorge.processString(message, maxStrLen * maxLines, false);
            }
            else
            {
                message = NETSNorge.processString(string.Empty, maxStrLen * maxLines, false);
            }

            List<string> messageList = new List<string>();

            if (message != string.Empty)
            {
                if (message.Length > maxLines * maxStrLen)
                    message = message.Substring(0, maxLines * maxStrLen);

                messageList = message.Select((x, i) => i)
                            .Where(i => i % maxStrLen == 0)
                            .Select(i => message.Substring(i, message.Length - i >= maxStrLen ? maxStrLen : message.Length - i)).ToList<string>();
            }

            danishFields.Messages = messageList;

            danishFields.NotUsed03 = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            danishFields.Blanks = string.Empty;

            danishFields.Amount = Math.Round(tran.PaymentAmount, 2);

            danishFields.NotUsed04 = new List<string>()
            {
                string.Empty,
                string.Empty
            };

            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);
            danishFields.OwnVoucherNumber = ShortenWordToCriteria(internalAdvText, 20);

            danishFields.NotUsed05 = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };


            danishFields.NotUsed06 = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };


            danishFields.TextCode = danishFields.TransTypeCommand == NordeaPaymentFormat.TRANSTYPE_45 ? NordeaPaymentFormat.TEXTCODE_SHORTADVICE : string.Empty;

            danishFields.Blanks2 = string.Empty;

            danishFields.TextToBeneficiary = danishFields.TransTypeCommand == NordeaPaymentFormat.TRANSTYPE_45 ? ShortenWordToCriteria(externalAdvText, 20) : string.Empty;

          

            maxStrLen = 35;
            maxLines = 37;

            var longAdvice = NETSNorge.processString(string.Empty, maxStrLen * maxLines, false);

            List<string> longAdviceList = new List<string>();

            if (longAdvice != string.Empty)
            {
                if (longAdvice.Length > maxLines * maxStrLen)
                    longAdvice = longAdvice.Substring(0, maxLines * maxStrLen);

                longAdviceList = longAdvice.Select((x, i) => i)
                            .Where(i => i % maxStrLen == 0)
                            .Select(i => longAdvice.Substring(i, longAdvice.Length - i >= maxStrLen ? maxStrLen : longAdvice.Length - i)).ToList<string>();
            }

            danishFields.LongAdviceText = longAdviceList;
            danishFields.PromptAdvice = danishFields.TransTypeCommand == NordeaPaymentFormat.TRANSTYPE_45 ? "0" : string.Empty;
            danishFields.Blanks3 = string.Empty;

            danishFields.UniquePaymRef = danishFields.TransTypeCommand == NordeaPaymentFormat.TRANSTYPE_45 ? tran.PaymentEndToEndId.ToString() : string.Empty;
            danishFields.ExpenseCode = danishFields.TransTypeCommand == NordeaPaymentFormat.TRANSTYPE_49 ? NordeaPaymentFormat.EXPENSECODE_BOTH : string.Empty;

            return danishFields;
        }

        private class addressFormat
        {
            public string AddressStr { get; set; }
        }

        public void BBANIBANPaymentType(DanishFormatFieldBase _field, CreditorTransPayment tran)
        {
            var field = _field as NordeaFormatFields;

            var countryId = string.Empty;
            var iban = string.Empty;
            var swift = string.Empty;
            var bban = string.Empty;
            var regNum = string.Empty;

            field.SwiftAddress = string.Empty;
            field.CountryCode = string.Empty;

            if (tran._PaymentMethod == PaymentTypes.IBAN)
            {
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
                bban = tran.PaymentId ?? string.Empty;  
                bban = Regex.Replace(bban, "[^0-9]", "");
                regNum = bban.Substring(0, 4);
                bban = bban.Remove(0, 4);
                bban = bban.PadLeft(10, '0');
            }

            swift = tran._SWIFT ?? string.Empty;
            if (swift != string.Empty)
            {
                swift = Regex.Replace(swift, "[^\\w\\d]", "");
                swift = swift.ToUpper();
                if (swift.Length > 6)
                    countryId = countryId == string.Empty ? swift.Substring(4, 2) : countryId;
            }

            field.SwiftAddress = swift;
            field.ReceiverIBAN = iban;
            field.ToAccountNumber = string.Format("{0}{1}",regNum,bban);
            field.CountryCode = countryId;

            if (countryId != string.Empty && countryId != BaseDocument.COUNTRY_DK)
            {
                field.TransTypeCommand = NordeaPaymentFormat.TRANSTYPE_49;

                bban = tran.PaymentId ?? string.Empty;
                bban = Regex.Replace(bban, "[^0-9]", "");
                field.ToAccountNumber = bban;
            }
            else
            {
                field.TransTypeCommand = NordeaPaymentFormat.TRANSTYPE_45;
                field.CountryCode = string.Empty;
                field.SwiftAddress = string.Empty;
            }
        }


        public void StreamToFile(List<DanishFormatFieldBase> listOfNordeaPayments, StreamWriter sw)
        {
            char seperator = ',';

            var type = (NordeaFormatFields)listOfNordeaPayments[0];

            var outputFields = new[]
            {
                "RecordType", "TransTypeCommand", "NotUsed", "CountryCode", "NameOfReceiver", "AddressOfReceiver", "AddressOfReceiver2", "AddressOfReceiver3", "BankCode",
                "ToAccountNumber","NotUsed02","SwiftAddress", "Messages", "NotUsed03", "Currency", "Blanks", "Amount", "TransferDate", "NotUsed04", "FromAccountNumber", "OwnVoucherNumber",
                "NotUsed05", "FormType", "PaymentId", "NotUsed06", "TextCode", "Blanks2", "TextToBeneficiary", "LongAdviceText", "PromptAdvice", "Blanks3", "UniquePaymRef","ExpenseCode"
            };

            var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();

            foreach (var np in listOfNordeaPayments)
            {
                var bp = (NordeaFormatFields)np;

                if (bp.TransTypeCommand == NordeaPaymentFormat.TRANSTYPE_45 ||
                    bp.TransTypeCommand == NordeaPaymentFormat.TRANSTYPE_46 ||
                    bp.TransTypeCommand == NordeaPaymentFormat.TRANSTYPE_49)
                {
                    bool firstColumn = true;
                    int countOfProp = 0;

                    foreach (FieldInfo field in fields)
                    {
                        bool secondBool = true;

                        countOfProp++;

                        if (!firstColumn)
                        {
                            sw.Write(seperator);
                        }
                        else
                        {
                            firstColumn = false;
                        }

                        string value = string.Empty;
                        var val = field.GetValue(bp);

                        switch (field.Name)
                        {
                            case "NotUsed":
                                foreach (var rec in bp.NotUsed)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "NotUsed02":
                                foreach (var rec in bp.NotUsed02)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "Messages":
                                foreach (var rec in bp.Messages)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "NotUsed03":
                                foreach (var rec in bp.NotUsed03)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "NotUsed04":
                                foreach (var rec in bp.NotUsed04)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "NotUsed05":
                                foreach (var rec in bp.NotUsed05)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "NotUsed06":
                                foreach (var rec in bp.NotUsed06)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "LongAdviceText":
                                foreach (var rec in bp.LongAdviceText)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "NotUsed07":
                                foreach (var rec in bp.NotUsed07)
                                {
                                    if (!secondBool)
                                        sw.Write(seperator);
                                    else
                                        secondBool = false;

                                    value = rec.Trim();
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;

                            default:
                                if (val is DateTime)
                                {
                                    value = ((DateTime)val).ToString("yyyyMMdd");
                                }
                                else if (val is double)
                                {
                                    value = $"{val:0.00}";
                                    value = value.Replace(",", ".");
                                }
                                else
                                {
                                    value = Convert.ToString(val);
                                }

                                value = Regex.Replace(value, "[\"\';]", " ");

                                sw.Write('"');
                                sw.Write(value);
                                sw.Write('"');
                                break;
                        }
                    }
                    sw.WriteLine();
                }
            }
        }
    }
}