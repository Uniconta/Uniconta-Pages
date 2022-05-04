using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using UnicontaClient.Pages;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using Localization = Uniconta.ClientTools.Localization;
using UnicontaClient.Creditor.Payments;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public partial class CreateDanskeBankFileFormatBase : BankFormatBase
    {
        public DanishFormatFieldBase CreateForeignFormatField(CreditorTransPayment tran, CreditorPaymentFormat paymentFormat,
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company,
            bool glJournalGenerated = false)
        {
            var danishFields = new DanskBankFormatFields();

            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            SharedForeignReceiverBankInfo(danishFields, tran);

            danishFields.TransTypeCommand = DanskeBankPayFormat.TRANSTYPE_CMUO;

            danishFields.ToAccountNumber = danishFields.ToAccountNumber == string.Empty ? danishFields.ReceiverIBAN : danishFields.ToAccountNumber;

            danishFields.Amount = Math.Round(tran.PaymentAmount, 2);

            danishFields.CurencyOfEquivalentAmount = string.Empty;

            danishFields.TransferType = 1; //Changed from formtype

            if (glJournalGenerated)
            {
                danishFields.NameOfReceiver = ShortenWordToCriteria(string.Empty, 35);
                danishFields.AddressOfReceiver = ShortenWordToCriteria(string.Empty, 35);
                danishFields.AddressOfReceiver2 = ShortenWordToCriteria(string.Empty, 35);
            }
            else
            {

                danishFields.NameOfReceiver = ShortenWordToCriteria(tran.Creditor.Name, 35);
                danishFields.AddressOfReceiver = ShortenWordToCriteria(string.Format("{0}, {1} {2}", tran.Creditor.Address1, tran.Creditor.ZipCode, tran.Creditor.City), 35);
                danishFields.AddressOfReceiver2 = ShortenWordToCriteria(tran.Creditor.Address2, 35);
            }

            danishFields.Blanks = string.Empty;
            
            danishFields.ReceiverBankInfo = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            danishFields.CostAccountTransfer = 1; //1=To be shared

            var invoiceNumber = tran.invoiceNumbers == null ? tran.Invoice.ToString() : tran.invoiceNumbers.ToString();
            invoiceNumber = invoiceNumber == "0" ? string.Empty : string.Format("INV:{0}", invoiceNumber);

            var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat._Message, tran, company, creditor);
            var message = externalAdvText;

            int maxStrLen = 35;
            int maxLines = 4;

            message = NETSNorge.processString(message, maxStrLen * maxLines, false);

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

            danishFields.NotUsed = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };


            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);

            danishFields.TextToSender = internalAdvText.Length > 20 ? internalAdvText.Substring(0, 20) : internalAdvText;

            danishFields.NotUsed02 = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            danishFields.ExchangeRateType = string.Empty;
            danishFields.Branch = string.Empty;

            danishFields.NotUsed03 = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty
            };

            danishFields.NotUsed04 = new List<string>()
            {
                string.Empty,
                string.Empty
            };

            danishFields.Blanks2 = string.Empty;
            danishFields.Blanks3 = string.Empty;
            danishFields.Reference = tran.PaymentEndToEndId.ToString();
            danishFields.Orderingofelectronicaladvice = string.Empty;
            danishFields.UniquePaymRef = tran.PaymentEndToEndId.ToString();

            return danishFields;
           
        }


        public void StreamToForeignFile(List<DanishFormatFieldBase> listOfDanskeBankPayments, StreamWriter sw)
        {
            char seperator = ',';

            var type = (DanskBankFormatFields) listOfDanskeBankPayments[0];

            var outputFields = new[] {"TransTypeCommand", "FromAccountNumber", "ToAccountNumber", "Currency", "Amount", "CurencyOfEquivalentAmount",
                "TransferDate", "TransferType", "NameOfReceiver", "AddressOfReceiver", "AddressOfReceiver2", "Blanks", "ReceiverBankInfo",
                "CostAccountTransfer", "Messages", "NotUsed", "TextToSender", "NotUsed02", "ExchangeRateType", "Branch", "NotUsed03", "SwiftAddress", "NotUsed04",
                "CountryCode", "Blanks2", "Blanks3", "Reference", "Orderingofelectronicaladvice"};

            var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();

            foreach (var dFFB in listOfDanskeBankPayments)
            {

                var bp = (DanskBankFormatFields) dFFB;

                if (bp.TransTypeCommand == DanskeBankPayFormat.TRANSTYPE_CMUO)
                {
                    bool firstColumn = true;
                    int countOfProps = 0;

                    foreach (FieldInfo field in fields)
                    {
                        countOfProps++;

                        bool secondBool = true;

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
                            case "ReceiverBankInfo":
                                foreach (var rec in bp.ReceiverBankInfo)
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

                            default:
                                if (val is DateTime)
                                {
                                    value = ((DateTime)val).ToString("ddMMyyyy");
                                }
                                else if (val is double)
                                {
                                    value = $"{val:0.00}";
                                    value = value.Replace(".", ",");
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