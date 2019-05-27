using System;
using System.Collections.Generic;
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
using System.Text.RegularExpressions;
using UnicontaClient.Creditor.Payments;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public partial class CreateDanskeBankFileFormatBase : BankFormatBase
    {

        public DanishFormatFieldBase CreateDanishFormatField(CreditorTransPayment tran,
            CreditorPaymentFormat paymentFormat,
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company, 
            bool glJournalGenerated = false)
        {
            var danishFields = new DanskBankFormatFields();

            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
        
            danishFields.TransTypeCommand = DanskeBankPayFormat.TRANSTYPE_CMBO;

            switch (tran._PaymentMethod)
            {
                case PaymentTypes.PaymentMethod3: //FIK71
                    SharedFIKPayment(danishFields, tran);
                    danishFields.ToAccountNumber = string.Format("IK{0}", danishFields.ToAccountNumber);
                    danishFields.PaymentId = danishFields.PaymentId.PadLeft(15, '0');
                    break;
                case PaymentTypes.PaymentMethod4: //FIK73
                    SharedFIKPayment(danishFields, tran);
                    danishFields.ToAccountNumber = string.Format("IK{0}", danishFields.ToAccountNumber);
                    break;
                case PaymentTypes.PaymentMethod5: //FIK75
                    SharedFIKPayment(danishFields, tran);
                    danishFields.ToAccountNumber = string.Format("IK{0}", danishFields.ToAccountNumber);
                    danishFields.PaymentId = danishFields.PaymentId.PadLeft(16, '0');
                    break;
                case PaymentTypes.PaymentMethod6: //FIK04
                    SharedFIKPayment(danishFields, tran);
                    danishFields.ToAccountNumber = string.Format("IK{0}", danishFields.ToAccountNumber);
                    danishFields.PaymentId = danishFields.PaymentId.PadLeft(16, '0');
                    break;
                case PaymentTypes.VendorBankAccount:
                    BBANIBANPaymentType(danishFields, tran);
                    break;
                case PaymentTypes.IBAN:
                    BBANIBANPaymentType(danishFields, tran);
                    danishFields.ToAccountNumber = danishFields.ReceiverIBAN;
                    break;
            }

            danishFields.Amount = Math.Round(tran._PaymentAmount, 2);

            danishFields.ClearingTypeChannel = "N";

            danishFields.NotUsed = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            danishFields.LetterToSend = "N";

            danishFields.NotUsed02 = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty 
            };

            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);
        
            danishFields.TextToSender = internalAdvText.Length > 20 ? internalAdvText.Substring(0, 20) : internalAdvText;

            danishFields.Blanks = string.Empty;
                  
            danishFields.DebtorId = string.Empty;
            danishFields.OwnVoucherNumber = danishFields.TextToSender;

            var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat._Message, tran, company, creditor);
            var message = externalAdvText;

            danishFields.TextToBeneficiary = message.Length > 20 ? message.Substring(0, 20) : message;

            int maxStrLen = 35;
            int maxLines = 41;

            if (tran._PaymentMethod == PaymentTypes.PaymentMethod4 || tran._PaymentMethod == PaymentTypes.PaymentMethod5 || tran._PaymentMethod == PaymentTypes.VendorBankAccount)
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
                string.Empty
            };

            if (glJournalGenerated == true)
            {
                danishFields.ZipCodeOfReceiver = string.Empty;
                danishFields.CityOfReceiver =  string.Empty;
            }
            else
            {
                var credZip = tran.Creditor.ZipCode ?? string.Empty;
                var credCity = tran.Creditor.City ?? string.Empty;

                danishFields.ZipCodeOfReceiver = credZip.Length > 4 ? credZip.Substring(0, 4) : credZip;
                danishFields.CityOfReceiver = credCity.Length > 28 ? credCity.Substring(0, 28) : credCity;
            }

            danishFields.NotUsed04 = new List<string>()
            {
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


            danishFields.DebtorsIdentificationOfThePayment = string.Empty;
            danishFields.Reference = tran._PaymentRefId.ToString();  
            danishFields.Orderingofelectronicaladvice = string.Empty;
            danishFields.UniquePaymRef = tran._PaymentRefId.ToString();

            return danishFields;
        }

        public void BBANIBANPaymentType(DanishFormatFieldBase _field, CreditorTransPayment tran)
        {
            var field = _field as DanskBankFormatFields;

            var countryId = string.Empty;
            var iban = string.Empty;
            var swift = string.Empty;
            var bban = string.Empty;

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
                string regNum = bban.Substring(0, 4);
                bban = bban.Remove(0, 4);
                bban = bban.PadLeft(10, '0');
                bban = string.Format("{0}{1}", regNum, bban);
            }

            swift = tran._SWIFT ?? string.Empty;
            if (swift != string.Empty)
            {
                swift = Regex.Replace(swift, "[^\\w\\d]", "");
                swift = swift.ToUpper();
                countryId = countryId == string.Empty ? swift.Substring(4, 2) : countryId;
            }

            field.SwiftAddress = swift;
            field.ReceiverIBAN = iban;
            field.ToAccountNumber = bban;
            field.CountryCode = countryId;
        }


        public void StreamToDanishFile(List<DanishFormatFieldBase> listOfDanskeBankPayments, StreamWriter sw)
        {
            char seperator = ',';

            var type = (DanskBankFormatFields) listOfDanskeBankPayments[0];

            var outputFields = new[]
            {
                "TransTypeCommand", "FromAccountNumber", "ToAccountNumber", "Amount", "TransferDate", "Currency",
                "ClearingTypeChannel", "NotUsed", "LetterToSend", "NotUsed02", "TextToSender", "Blanks", "TextToBeneficiary",
                "FormType", "PaymentId", "DebtorId", "OwnVoucherNumber", "Messages", "NotUsed03", "ZipCodeOfReceiver", "CityOfReceiver",
                "NotUsed04", "DebtorsIdentificationOfThePayment", "Reference", "Orderingofelectronicaladvice"
            };

            var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();

            foreach (var dFFdB in listOfDanskeBankPayments)
            {
                var bp = (DanskBankFormatFields)dFFdB;

                if (bp.TransTypeCommand == DanskeBankPayFormat.TRANSTYPE_CMBO)
                {
                    bool firstColumn = true;
                    int countOffields = 0;

                    foreach (FieldInfo field in fields)
                    {
                        countOffields++;

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