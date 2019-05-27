using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnicontaClient.Pages;
using Uniconta.ClientTools;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Creditor.Payments;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public partial class CreateBankDataFileFormatBase : BankFormatBase
    {
        public DanishFormatFieldBase CreateForeignFormatField(CreditorTransPayment tran,
            CreditorPaymentFormat paymentFormat,
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company,
            bool glJournalGenerated = false)
        {
            var danishFields = new BankDataFormatFields();
            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            SharedForeignReceiverBankInfo(danishFields, tran);

            var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat._Message, tran, company, creditor);
            var message = externalAdvText;

            message = NETSNorge.processString(message, 140, false);

            int maxStrLen = 35;
            int maxLines = 4;

            List<string> messageList = new List<string>();

            if (message != string.Empty)
            {
                if (message.Length > maxLines * maxStrLen)
                    message = message.Substring(0, maxLines * maxStrLen);

                messageList = message.Select((x, i) => i)
                            .Where(i => i % maxStrLen == 0)
                            .Select(i => message.Substring(i, message.Length - i >= maxStrLen ? maxStrLen : message.Length - i)).ToList<string>();
            }

            danishFields.DescriptionOfPayment = messageList;

            danishFields.TransTypeCommand = BankDataPayFormat.TRANSTYPE_IB030204000003;
            danishFields.Index = BankDataPayFormat.INDEX01;

            var lineamountint = NumberConvert.ToLong(tran._PaymentAmount * 100d);
            danishFields.AmountLong = lineamountint;

            danishFields.FromAccountType = 2;

            danishFields.TransferCoin = new string(' ', 3);

            var paymentType = ISOPaymentType(tran._CurrencyLocal, bankAccount, danishFields.ReceiverIBAN, company);

            danishFields.TransferType = paymentType == UnicontaISO20022CreditTransfer.ISO20022PaymentTypes.SEPA ? BankDataPayFormat.FOREIGN_SEPATRANSFER : BankDataPayFormat.FOREIGN_STANDARDTRANSFER;

            if (glJournalGenerated)
            {
                danishFields.NameOfReceiver = NETSNorge.processString(string.Empty, 35, false);
                danishFields.AddressOfReceiver = NETSNorge.processString(string.Empty, 35, false);
                danishFields.AddressOfReceiver2 = NETSNorge.processString(string.Empty, 35, false);
            }
            else
            { 
                danishFields.NameOfReceiver = NETSNorge.processString(tran.Creditor.Name, 35, false);
                danishFields.AddressOfReceiver = NETSNorge.processString(tran.Creditor.Address1 + ", " + tran.Creditor.ZipCode + " " + tran.Creditor.City, 35, false);
                danishFields.AddressOfReceiver2 = NETSNorge.processString(tran.Creditor.Address2, 35, false);
            }

            danishFields.OtherTransfers = new List<string>()
            {
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 4, false),
                NETSNorge.processString(string.Empty, 6, false),
                NETSNorge.processString(string.Empty, 2, false),
                NETSNorge.processString(string.Empty, 75, false),
                NETSNorge.processString(string.Empty, 75, false),
                NETSNorge.processString(string.Empty, 75, false),
                NETSNorge.processString(string.Empty, 24, false),
                NETSNorge.processString(string.Empty, 215, false)
            };

            danishFields.UniquePaymRef = tran._PaymentRefId.ToString();

            return danishFields;
        }

        public DanishFormatFieldBase SecondaryCreateForeignFormatField(CreditorTransPayment tran,
            CreditorPaymentFormat paymentFormat,
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company,
            bool glJournalGenerated = false
            )
        {
            var danishFields = new BankDataFormatFields();

            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            SharedForeignReceiverBankInfo(danishFields, tran); 

            danishFields.TransTypeCommand = BankDataPayFormat.TRANSTYPE_IB030204000003;
            danishFields.Index = BankDataPayFormat.INDEX02;

            danishFields.TransferType = 0;
            danishFields.Blanks = NETSNorge.processString(string.Empty, 1, false);
            danishFields.SwiftAddress = NETSNorge.processString(danishFields.SwiftAddress, 11, false);

            danishFields.ReceiverBankInfo = new List<string>()
            {
                NETSNorge.processString("Bank", 35, false), //Dummy text - ask bank if correct Bank name is needed
                NETSNorge.processString("Address", 35, false), //Dummy text - ask bank if correct Bank address is needed
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(danishFields.CountryCode, 35, false),
                NETSNorge.processString(string.Empty, 33, false) //BankCode
            };


            danishFields.ToAccountNumber = NETSNorge.processString(danishFields.ToAccountNumber, 34, false);
            danishFields.ReceiverIBAN = NETSNorge.processString(danishFields.ReceiverIBAN, 35, false);  

            var paymentType = ISOPaymentType(tran._CurrencyLocal, bankAccount, danishFields.ReceiverIBAN, company);
            danishFields.TransferTypeForeign = paymentType == UnicontaISO20022CreditTransfer.ISO20022PaymentTypes.SEPA ? 1 : 0;
            danishFields.Blanks2 = NETSNorge.processString(string.Empty, 15, false);

            danishFields.Messages = new List<string>()
            {
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false)
            };

            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);
            danishFields.OwnVoucherNumber = NETSNorge.processString(internalAdvText, 35, false);

            danishFields.SenderInformation = new List<string>()
            {
                NETSNorge.processString(string.Empty, 15, false),
                NETSNorge.processString(string.Empty, 13, false),
                NETSNorge.processString(string.Empty, 1, false),
                NETSNorge.processString(string.Empty, 7, false),
                NETSNorge.processString(string.Empty, 15, false),
                NETSNorge.processString(string.Empty, 13, false),
                NETSNorge.processString(string.Empty, 1, false),
                NETSNorge.processString(string.Empty, 7, false),
                NETSNorge.processString(string.Empty, 15, false),
                NETSNorge.processString(string.Empty, 13, false),
                NETSNorge.processString(string.Empty, 1, false),
                NETSNorge.processString(string.Empty, 7, false),
                NETSNorge.processString(string.Empty, 15, false),
                NETSNorge.processString(string.Empty, 13, false),
                NETSNorge.processString(string.Empty, 1, false),
                NETSNorge.processString(string.Empty, 7, false),
                NETSNorge.processString(string.Empty, 15, false),
                NETSNorge.processString(string.Empty, 13, false),
                NETSNorge.processString(string.Empty, 1, false),
                NETSNorge.processString(string.Empty, 7, false),
                NETSNorge.processString(string.Empty, 169, false),
            };


            danishFields.UniquePaymRef = tran._PaymentRefId.ToString();

            return danishFields;
        }

        public void StreamToForeignFile(List<DanishFormatFieldBase> listOfBankDataPayments, StreamWriter sw)
        {
            char seperator = ',';
            var type = (BankDataFormatFields)listOfBankDataPayments[0];

            foreach (var dFFdB in listOfBankDataPayments)
            {
                var bp = (BankDataFormatFields) dFFdB;
                bool firstColumn = true;

                if (bp.Index == BankDataPayFormat.INDEX01 && bp.TransTypeCommand == BankDataPayFormat.TRANSTYPE_IB030204000003)
                {
                    var outputFields = new[]
                    {
                        "TransTypeCommand", "Index", "TransferDate", "AmountLong", "FromAccountType", "FromAccountNumber",
                        "Currency", "TransferCoin", "TransferType", "DescriptionOfPayment", "NameOfReceiver",
                        "AddressOfReceiver", "AddressOfReceiver2", "OtherTransfers"
                    };

                    var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();
                    foreach (FieldInfo field in fields)
                    {
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
                            case "AmountLong":
                                sw.Write("\"{0:D13}+\"", val);
                                break;
                            case "FromAccountNumber":
                                value = "0" + val;
                                value = NETSNorge.processString(value, 15, false);
                                sw.Write('"');
                                sw.Write(value);
                                sw.Write('"');
                                break;
                            case "DescriptionOfPayment":
                                foreach (var dp in bp.DescriptionOfPayment)
                                {
                                    if (!secondBool)
                                    {
                                        sw.Write(seperator);
                                    }
                                    else
                                    {
                                        secondBool = false;
                                    }
                                    value = dp;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(NETSNorge.processString(value, 35, false));
                                    sw.Write('"');
                                }
                                break;
                            case "OtherTransfers":
                                foreach (var ot in bp.OtherTransfers)
                                {
                                    if (!secondBool)
                                    {
                                        sw.Write(seperator);
                                    }
                                    else
                                    {
                                        secondBool = false;
                                    }
                                    value = ot;
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
                else if (bp.Index == BankDataPayFormat.INDEX02 && bp.TransTypeCommand == BankDataPayFormat.TRANSTYPE_IB030204000003)

                    {
                        var outputFields = new[]
                    {
                        "TransTypeCommand", "Index", "TransferType", "Blanks", "SwiftAddress", "ReceiverBankInfo",
                        "ToAccountNumber", "ReceiverIBAN", "TransferTypeForeign", "Blanks2", "Messages", "OwnVoucherNumber", "SenderInformation"
                    };

                    var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();
                    foreach (FieldInfo field in fields)
                    {
                        bool secondBool = true;

                        var val = field.GetValue(bp);
                        string value;
                        if (val is DateTime)
                        {
                            value = ((DateTime)val).ToString("yyyyMMdd");
                        }
                        else
                        {
                            value = Convert.ToString(val);
                        }

                        if (!firstColumn)
                        {
                            sw.Write(seperator);
                        }
                        else
                        {
                            firstColumn = false;
                        }

                        
                        switch (field.Name)
                        {
                            case "ReceiverBankInfo":
                                foreach (var rb in bp.ReceiverBankInfo)
                                {
                                    if (!secondBool)
                                    {
                                        sw.Write(seperator);
                                    }
                                    else
                                    {
                                        secondBool = false;
                                    }
                                    value = rb;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "Messages":
                                foreach (var m in bp.Messages)
                                {
                                    if (!secondBool)
                                    {
                                        sw.Write(seperator);
                                    }
                                    else
                                    {
                                        secondBool = false;
                                    }
                                    value = m;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            case "SenderInformation":
                                foreach (var si in bp.SenderInformation)
                                {
                                    if (!secondBool)
                                    {
                                        sw.Write(seperator);
                                    }
                                    else
                                    {
                                        secondBool = false;
                                    }
                                    value = si;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write('"');
                                    sw.Write(value);
                                    sw.Write('"');
                                }
                                break;
                            default:
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

