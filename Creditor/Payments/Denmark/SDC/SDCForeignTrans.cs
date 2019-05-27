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
    public partial class CreateSDCFileFormatBase : BankFormatBase
    {
        public DanishFormatFieldBase CreateForeignFormatField(CreditorTransPayment tran,
            CreditorPaymentFormat paymentFormat,
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company,
            bool glJournalGenerated = false)
        {
            var danishFields = new SDCFormatFields();
            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            SharedForeignReceiverBankInfo(danishFields, tran);
          
            danishFields.TransTypeCommand = SDCPayFormat.RECORDTYPE_K037;
            danishFields.TransferTypeStr = "001";

            danishFields.ToAccountNumber = danishFields.ToAccountNumber == string.Empty ? danishFields.ReceiverIBAN : danishFields.ToAccountNumber;
            danishFields.ToAccountNumber = NETSNorge.processString(danishFields.ToAccountNumber, 35, false);

            var paymentAmount = Math.Round(tran._PaymentAmount, 2);
            var paymentAmountSTR = paymentAmount.ToString("F");
            danishFields.AmountSTR = NETSNorge.processString(paymentAmountSTR, 14, true);
            danishFields.TransferCurrency = danishFields.Currency;
            danishFields.Blanks = NETSNorge.processString(string.Empty, 14, false);

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


            danishFields.Blanks2 = NETSNorge.processString(string.Empty, 35, false);

            danishFields.ReceiverAccountInfo = new List<string>()
            {
                NETSNorge.processString(danishFields.SwiftAddress, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false)
            };

            var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat._Message, tran, company, creditor);

            var message = NETSNorge.processString(externalAdvText, 140, false);

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

            danishFields.Messages = new List<string>()
            {
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false)
            };

            danishFields.ExchRateType = "N"; //N=Noteringskurs, A=Aftaltkurs, T=Terminkurs

            danishFields.ExchRateTermContract = NETSNorge.processString(string.Empty, 10, false);
            danishFields.ExchRateTerm = NETSNorge.processString(string.Empty, 14, false);

            danishFields.ChargeAccountSeparate = "N";
            danishFields.ChargeAccount = NETSNorge.processString(string.Empty, 14, false);
            danishFields.ChargeType = "1"; // 1 = betales i DK af afsender, i udlandet af modtager(SHA). 2 = betales af afsender (OUR), 3 = betales af modtager(BEN)

            danishFields.EmptyFields = new List<string>()
            {
                NETSNorge.processString(string.Empty, 1, false),
                NETSNorge.processString(string.Empty, 2, false),
                NETSNorge.processString(string.Empty, 4, false),
                NETSNorge.processString(string.Empty, 6, false),
                NETSNorge.processString(string.Empty, 140, false),
            };

            danishFields.UniquePaymRef = tran._PaymentRefId.ToString();

            return danishFields;
        }


        public void StreamToForeignFile(List<DanishFormatFieldBase> listOfSDCPayments, StreamWriter sw)
        {
            var type = (SDCFormatFields)listOfSDCPayments[0];

            foreach (var dFFdB in listOfSDCPayments)
            {
                var bp = (SDCFormatFields) dFFdB;

                if ( bp.TransTypeCommand == SDCPayFormat.RECORDTYPE_K037)
                {
                    var outputFields = new[]
                    {
                        "TransTypeCommand", "TransferDate", "TransferTypeStr", "FromAccountNumber",
                        "ToAccountNumber", "Currency", "AmountSTR", "TransferCurrency", "Blanks",
                        "NameOfReceiver", "AddressOfReceiver", "AddressOfReceiver2", "Blanks2",
                        "ReceiverAccountInfo", "DescriptionOfPayment", "Messages", "ExchRateType", "ExchRateTermContract",
                        "ExchRateTerm", "ChargeAccountSeparate", "ChargeAccount", "ChargeType", "EmptyFields"
                    };


                    var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();
                    foreach (FieldInfo field in fields)
                    {
                        string value;

                        var val = field.GetValue(bp);
                        switch (field.Name)
                        {
                            case "ReceiverAccountInfo":
                                foreach (var ri in bp.ReceiverAccountInfo)
                                {
                                    value = ri;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write(value);
                                }
                                break;
                            case "DescriptionOfPayment":
                                foreach (var dp in bp.DescriptionOfPayment)
                                {
                                    value = dp;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write(value);
                                }
                                break;
                            case "Messages":
                                foreach (var m in bp.Messages)
                                {
                                    value = m;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write(value);
                                }
                                break;
                            case "EmptyFields":
                                foreach (var e in bp.EmptyFields)
                                {
                                    value = e;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write(value);
                                }
                                break;
                            default:
                                if (val is DateTime)
                                {
                                    value = ((DateTime)val).ToString("ddMMyyyy");
                                }
                                else
                                {
                                    value = Convert.ToString(val);
                                }

                                value = Regex.Replace(value, "[\"\';]", " "); 
                                sw.Write(value);
                                break;
                        }
                    }
                    sw.WriteLine();
                }
            }
        }
    }
}

