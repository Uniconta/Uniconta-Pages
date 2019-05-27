using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnicontaClient.Pages;
using Uniconta.ClientTools;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Creditor.Payments;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public partial class CreateSDCFileFormatBase : BankFormatBase
    {
        public DanishFormatFieldBase CreateFIKFormatField(CreditorTransPayment tran,
            CreditorPaymentFormat paymentFormat,
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company,
            bool glJournalGenerated = false)
        {
            var danishFields = new SDCFormatFields();

            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            SharedFIKPayment(danishFields, tran);

            switch (tran._PaymentMethod)
            {
                case PaymentTypes.PaymentMethod3: //FIK71
                    danishFields.TransTypeCommand = SDCPayFormat.RECORDTYPE_K020;
                    danishFields.ToAccountNumber = danishFields.ToAccountNumber.PadLeft(8, '0');
                    danishFields.PaymentId = danishFields.PaymentId.PadLeft(15, '0');
                    break;
                case PaymentTypes.PaymentMethod4: //FIK73
                    danishFields.TransTypeCommand = SDCPayFormat.RECORDTYPE_K073;
                    danishFields.ToAccountNumber = danishFields.ToAccountNumber.PadLeft(8, '0');
                    break;
                case PaymentTypes.PaymentMethod5: //FIK75
                    danishFields.TransTypeCommand = SDCPayFormat.RECORDTYPE_K075;
                    danishFields.ToAccountNumber = danishFields.ToAccountNumber.PadLeft(8, '0');
                    danishFields.PaymentId = danishFields.PaymentId.PadLeft(16, '0');
                    break;
                case PaymentTypes.PaymentMethod6: //FIK04
                    danishFields.TransTypeCommand = SDCPayFormat.RECORDTYPE_K006;
                    danishFields.ToAccountNumber = danishFields.ToAccountNumber.PadLeft(10, '0');
                    danishFields.PaymentId = danishFields.PaymentId.PadLeft(19, '0');
                    break;
            }

            var paymentAmount = Math.Round(tran._PaymentAmount, 2);
            var paymentAmountSTR = paymentAmount.ToString("F");
            danishFields.AmountSTR = NETSNorge.processString(paymentAmountSTR, 15, true); ;
            danishFields.Receipt = "N";

            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);
            danishFields.OwnVoucherNumber = NETSNorge.processString(internalAdvText, 20, false);

            if (danishFields.FormType == BankFormatBase.FIK04 || danishFields.FormType == BankFormatBase.FIK73)
            {
                danishFields.OtherSender = new List<string>()
                {
                    NETSNorge.processString("N", 1, false),
                    NETSNorge.processString(string.Empty, 18, false),
                    NETSNorge.processString(string.Empty, 32, false),
                    NETSNorge.processString(string.Empty, 32, false),
                    NETSNorge.processString(string.Empty, 4, false),
                };
            }

            if (danishFields.FormType == BankFormatBase.FIK73 || danishFields.FormType == BankFormatBase.FIK75 || danishFields.FormType == BankFormatBase.FIK04)
            {
                //Message to Beneficiary >>
                var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat._Message, tran, company, creditor);

                danishFields.ReceiverAccountStatement = NETSNorge.processString(externalAdvText, 35, false);

                var message = externalAdvText;
                var maxStrLen = 35;
                var numLines = message.Length / (double)maxStrLen;
                var maxLines = (int)Math.Ceiling(numLines);
                maxLines = maxLines > 40 ? 40 : maxLines;
                message = NETSNorge.processString(message, maxLines*maxStrLen, false);

                List<string> messageList = new List<string>();

                if (message != string.Empty)
                {
                    if (message.Length > maxLines * maxStrLen)
                        message = message.Substring(0, maxLines * maxStrLen);

                    messageList = message.Select((x, i) => i)
                                .Where(i => i % maxStrLen == 0)
                                .Select(i => message.Substring(i, message.Length - i >= maxStrLen ? maxStrLen : message.Length - i)).ToList<string>();
                }

                danishFields.AdviceText = messageList;

                var maxLinesSTR = maxLines.ToString();
                danishFields.AdviceTextLines = maxLinesSTR.PadLeft(3, '0');
                //Message to Beneficiary <<         
            }
            else
            {
                danishFields.AdviceTextLines = string.Empty;
            }

            danishFields.UniquePaymRef = tran._PaymentRefId.ToString();

            return danishFields;
        }

        public void StreamToFIKFile(List<DanishFormatFieldBase> listOfSDCPayments, StreamWriter sw)
        {
            var specialChar = new char[] { '"', ';' };

            var type = (SDCFormatFields)listOfSDCPayments[0];

            foreach (var dFFdB in listOfSDCPayments)
            {
                var bp = (SDCFormatFields)dFFdB;
                int countOffields = 0;

                if (bp.TransTypeCommand == SDCPayFormat.RECORDTYPE_K006 ||
                    bp.TransTypeCommand == SDCPayFormat.RECORDTYPE_K020 ||
                    bp.TransTypeCommand == SDCPayFormat.RECORDTYPE_K073 ||
                    bp.TransTypeCommand == SDCPayFormat.RECORDTYPE_K075)
                {

                    string[] outputFields;

                    if (bp.TransTypeCommand == SDCPayFormat.RECORDTYPE_K006) //FIK04
                    {
                        outputFields = new[]
                        {
                        "TransTypeCommand", "FromAccountNumber", "TransferDate", "AmountSTR", "Receipt", "OwnVoucherNumber","OtherSender",
                        "ToAccountNumber", "FormType", "PaymentId", "AdviceTextLines", "AdviceText"
                        };
                    }
                    else
                    {
                        outputFields = new[]
                        {
                        "TransTypeCommand", "FromAccountNumber", "TransferDate", "AmountSTR", "Receipt", "OwnVoucherNumber", "ToAccountNumber", "FormType",
                        "OtherSender", "PaymentId", "AdviceTextLines", "AdviceText"
                        };
                    }

                    var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();
                    foreach (FieldInfo field in fields)
                    {
                        countOffields++;

                        var val = field.GetValue(bp);
                        string value;
                        if (val is DateTime)
                        {
                            value = ((DateTime)val).ToString("ddMMyyyy");
                        }
                        else
                        {
                            value = Convert.ToString(val);
                        }

                        switch (field.Name)
                        {
                            case "OtherSender":
                                foreach (var os in bp.OtherSender)
                                {
                                    value = os;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write(value);
                                }
                                break;
                            case "AdviceText":
                                foreach (var a in bp.AdviceText)
                                {
                                    value = a;
                                    value = Regex.Replace(value, "[\"\';]", " ");
                                    sw.Write(value);
                                }
                                break;
                            default:
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
