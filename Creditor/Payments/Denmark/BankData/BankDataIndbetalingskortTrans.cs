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
    public partial class CreateBankDataFileFormatBase : BankFormatBase
    {
        public DanishFormatFieldBase CreateIndbetalingskortFormatField(CreditorTransPayment tran,
            CreditorPaymentFormat paymentFormat,
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company,
            bool glJournalGenerated = false)
        {
            var danishFields = new BankDataFormatFields();

            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            SharedFIKPayment(danishFields, tran);

            danishFields.TransTypeCommand = BankDataPayFormat.TRANSTYPE_IB030207000002;
            danishFields.Index = BankDataPayFormat.INDEX01;

            var lineamountint = NumberConvert.ToLong(tran.PaymentAmount * 100d);
            danishFields.AmountLong = lineamountint;

            danishFields.FromAccountType = 2;
            danishFields.GiroReg = NETSNorge.processString(string.Empty, 4, false);

            if (danishFields.FormType == BankFormatBase.FIK04)
            {
                danishFields.ToAccountGiro = NETSNorge.processString(danishFields.ToAccountNumber, 10, false);
                danishFields.ToAccountCreditor = NETSNorge.processString(string.Empty, 8, false);
            }
            else
            {
                danishFields.ToAccountGiro = NETSNorge.processString(string.Empty, 10, false);
                danishFields.ToAccountCreditor = NETSNorge.processString(danishFields.ToAccountNumber, 8, false);
            }
            string credName = glJournalGenerated ? string.Empty : tran.Creditor.Name;
            danishFields.NameOfReceiver = NETSNorge.processString(credName, 32, false);
            danishFields.AlternativSender = NETSNorge.processString(string.Empty, 32, false);

            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);
            danishFields.OwnVoucherNumber = NETSNorge.processString(internalAdvText, 35, false);

            danishFields.SenderName = NETSNorge.processString(company._Name, 35, false);
            danishFields.SenderAddress1 = NETSNorge.processString(company._Address1, 35, false);
            danishFields.SenderAddress2 = NETSNorge.processString(company._Address2, 35, false);
            danishFields.SenderAddress3 = NETSNorge.processString(company._Address3, 35, false);
            danishFields.Blanks = NETSNorge.processString(string.Empty, 35, false);

            if (danishFields.FormType == BankFormatBase.FIK73 || danishFields.FormType == BankFormatBase.FIK75)
            {
                var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat._Message, tran, company, creditor);
                var message = externalAdvText;

                message = NETSNorge.processString(message, 210, false);

                int maxStrLen = 35;
                int maxLines = 6;

                List<string> messageList = new List<string>();

                if (message != string.Empty)
                {
                    if (message.Length > maxLines * maxStrLen)
                        message = message.Substring(0, maxLines * maxStrLen);

                    messageList = message.Select((x, i) => i)
                                .Where(i => i % maxStrLen == 0)
                                .Select(i => message.Substring(i, message.Length - i >= maxStrLen ? maxStrLen : message.Length - i)).ToList<string>();
                }

                danishFields.ReceiverAccountInfo = messageList;
            }
            else
            {
                danishFields.ReceiverAccountInfo = new List<string>()
                {
                    NETSNorge.processString(string.Empty, 35, false),
                    NETSNorge.processString(string.Empty, 35, false),
                    NETSNorge.processString(string.Empty, 35, false),
                    NETSNorge.processString(string.Empty, 35, false),
                    NETSNorge.processString(string.Empty, 35, false),
                    NETSNorge.processString(string.Empty, 35, false),
                };
            }

            danishFields.Blanks2 = NETSNorge.processString(string.Empty, 16, false);
            danishFields.Reserved = NETSNorge.processString(string.Empty, 215, false);

            danishFields.UniquePaymRef = tran.PaymentEndToEndId.ToString();

            return danishFields;
        }

        public void StreamToIndbetalingskortFile(List<DanishFormatFieldBase> listOfDanskeBankPayments, StreamWriter sw)
        {
            char seperator = ',';

            var type = (BankDataFormatFields)listOfDanskeBankPayments[0];
            

            foreach (var dFFdB in listOfDanskeBankPayments)
            {
                var bp = (BankDataFormatFields)dFFdB;
                bool firstColumn = true;
                int countOffields = 0;

                if (bp.Index == BankDataPayFormat.INDEX01 && bp.TransTypeCommand == BankDataPayFormat.TRANSTYPE_IB030207000002)
                {
                    var outputFields = new[]
                    {
                    "TransTypeCommand", "Index", "TransferDate", "AmountLong", "FromAccountType",
                    "FromAccountNumber", "FormType", "PaymentId", "GiroReg", "ToAccountGiro", "ToAccountCreditor",
                    "NameOfReceiver", "AlternativSender", "OwnVoucherNumber", "SenderName", "SenderAddress1",
                    "SenderAddress2", "SenderAddress3", "Blanks", "ReceiverAccountInfo", "Blanks2", "Reserved"
                    };

                    var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();
                    foreach (FieldInfo field in fields)
                    {
                        bool secondBool = true;
                        countOffields++;

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
                            case "AmountLong":
                                sw.Write("\"{0:D13}+\"", val);
                                break;
                            case "FromAccountNumber":
                                string realAcc = "0" + value;
                                realAcc = NETSNorge.processString(realAcc, 15, false);
                                sw.Write('"');
                                sw.Write(realAcc);
                                sw.Write('"');
                                break;

                            case "PaymentId":
                                string paymId = string.Empty;
                                paymId = NETSNorge.processString(value, 19, false);
                                sw.Write('"');
                                sw.Write(paymId);
                                sw.Write('"');
                                break;

                            case "ReceiverAccountInfo":
                                foreach (var ra in bp.ReceiverAccountInfo)
                                {
                                    if (!secondBool)
                                    {
                                        sw.Write(seperator);
                                    }
                                    else
                                    {
                                        secondBool = false;
                                    }
                                    value = ra;
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
