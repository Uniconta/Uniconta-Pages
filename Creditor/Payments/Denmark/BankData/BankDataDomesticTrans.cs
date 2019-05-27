using System;
using System.Collections.Generic;
using System.Globalization;
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
using UnicontaISO20022CreditTransfer;
using System.Text.RegularExpressions;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public partial class CreateBankDataFileFormatBase : BankFormatBase
    {
        public DanishFormatFieldBase CreateDomesticFormatField(CreditorTransPayment tran, CreditorPaymentFormat paymentFormat, 
                                                               BankStatement bankAccount, Uniconta.DataModel.Creditor creditor, Company company, bool glJournalGenerated = false)
        {
            var danishFields = new BankDataFormatFields();
            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            DomesticPaymentType(danishFields, tran);
                
            danishFields.TransTypeCommand = BankDataPayFormat.TRANSTYPE_IB030202000005;
            danishFields.Index = BankDataPayFormat.INDEX01;

            var lineamountint = NumberConvert.ToLong(tran._PaymentAmount * 100d);
            danishFields.AmountLong = lineamountint;

            danishFields.FromAccountType = 2;
            danishFields.TransferType = 2;
            danishFields.ClearingTypeChannel = "0";

            if (glJournalGenerated)
            {
                danishFields.NameOfReceiver = NETSNorge.processString(string.Empty, 32, false);
                danishFields.AddressOfReceiver = NETSNorge.processString(string.Empty, 32, false);
                danishFields.AddressOfReceiver2 = NETSNorge.processString(string.Empty, 32, false);
                danishFields.ZipCodeOfReceiver = NETSNorge.processString(string.Empty, 4, false);
                danishFields.CityOfReceiver = NETSNorge.processString(string.Empty, 32, false);
            }
            else
            {
                danishFields.NameOfReceiver = NETSNorge.processString(tran.Creditor.Name, 32, false);
                danishFields.AddressOfReceiver = NETSNorge.processString(tran.Creditor.Address1, 32, false);
                danishFields.AddressOfReceiver2 = NETSNorge.processString(tran.Creditor.Address2, 32, false);
                danishFields.ZipCodeOfReceiver = NETSNorge.processString(tran.Creditor.ZipCode, 4, false);
                danishFields.CityOfReceiver = NETSNorge.processString(tran.Creditor.City, 32, false);
            }

            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);
            danishFields.OwnVoucherNumber = NETSNorge.processString(internalAdvText, 35, false);

            //Message to Beneficiary >>
            var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat._Message, tran, company, creditor);
            var message = externalAdvText;

            danishFields.ReceiverAccountStatement = NETSNorge.processString(externalAdvText, 35, false);
            
            message = NETSNorge.processString(message, 315, false);

            int maxStrLen = 35;
            int maxLines = 9;

            List<string> messageList = new List<string>();

            if (message != string.Empty)
            {
                if (message.Length > maxLines * maxStrLen)
                    message = message.Substring(0, maxLines * maxStrLen);

                messageList = message.Select((x, i) => i)
                            .Where(i => i % maxStrLen == 0)
                            .Select(i => message.Substring(i, message.Length - i >= maxStrLen ? maxStrLen : message.Length - i)).ToList<string>();
            }

            danishFields.ReceiverAccountStatement = NETSNorge.processString(message, 35, false);
            danishFields.AdviceText = messageList;
            //Message to Beneficiary <<             

            danishFields.Blanks = NETSNorge.processString(string.Empty, 1, false);
            danishFields.Blanks2 = NETSNorge.processString(string.Empty, 215, false);
            
            danishFields.UniquePaymRef = tran._PaymentRefId.ToString();

            return danishFields;
        }

        public DanishFormatFieldBase SecondaryCreateDomesticFormatField(CreditorTransPayment tran, 
            CreditorPaymentFormat paymentFormat, BankStatement bankAccount, Company company, bool glJournalGenerated = false)
        {

            var danishFields = new BankDataFormatFields();
            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            DomesticPaymentType(danishFields, tran);

            danishFields.TransTypeCommand = BankDataPayFormat.TRANSTYPE_IB030202000005;
            danishFields.Index = BankDataPayFormat.INDEX02;
            danishFields.SenderName = NETSNorge.processString(company._Name, 35, false);
            danishFields.SenderAddress1 = NETSNorge.processString(company._Address1, 35, false);
            danishFields.SenderAddress2 = NETSNorge.processString(company._Address2, 35, false);
            danishFields.SenderAddress3 = NETSNorge.processString(company._Address3, 35, false);

            danishFields.ReservetForXML = new List<string>
            {
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),
                NETSNorge.processString(string.Empty, 35, false),

                NETSNorge.processString(string.Empty, 48, false),
                NETSNorge.processString(string.Empty, 255, false),
                NETSNorge.processString(string.Empty, 255, false)
            };

            return danishFields;
        }


        public void DomesticPaymentType(DanishFormatFieldBase _field, CreditorTransPayment tran)
        {
            var field = _field as BankDataFormatFields;

            field.SwiftAddress = string.Empty;
            field.CountryCode = string.Empty;

            field.FormType = "Vendor Bank Account";

            var bban = tran.PaymentId ?? string.Empty;
            bban = Regex.Replace(bban, "[^0-9]", "");
            string regNum = bban.Substring(0, 4);
            bban = bban.Remove(0, 4);
            bban = bban.PadLeft(10, '0');
        
            field.ToAccountRegNr = NETSNorge.processString(regNum, 4, false);
            field.ToAccountNumber = NETSNorge.processString(bban, 10, false);
        }

        public void StreamToDomesticFile(List<DanishFormatFieldBase> listOfDanskeBankPayments, StreamWriter sw)
        {
            char seperator = ',';

            var type = (BankDataFormatFields)listOfDanskeBankPayments[0];

            foreach (var dFFdB in listOfDanskeBankPayments)
            {
                var bp = (BankDataFormatFields)dFFdB;
                bool firstColumn = true;

                if (bp.Index == BankDataPayFormat.INDEX01 && bp.TransTypeCommand == BankDataPayFormat.TRANSTYPE_IB030202000005)
                {
                    var outputFields = new[]
                    {
                        "TransTypeCommand", "Index", "TransferDate", "AmountLong", "Currency", "FromAccountType",
                        "FromAccountNumber", "TransferType", "ToAccountRegNr", "ToAccountNumber", "ClearingTypeChannel",
                        "ReceiverAccountStatement", "NameOfReceiver", "AddressOfReceiver", "AddressOfReceiver2", "ZipCodeOfReceiver", "CityOfReceiver",
                        "OwnVoucherNumber", "AdviceText", "Blanks", "Blanks2"
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
                            case "AdviceText":
                                foreach (var a in bp.AdviceText)
                                {
                                    if (!secondBool)
                                    {
                                        sw.Write(seperator);
                                    }
                                    else
                                    {
                                        secondBool = false;
                                    }
                                    value = a;
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
                else if (bp.Index == BankDataPayFormat.INDEX02 && bp.TransTypeCommand == BankDataPayFormat.TRANSTYPE_IB030202000005)
                {
                    var outputFields = new[]
                    {
                        "TransTypeCommand", "Index", "SenderName", "SenderAddress1",
                        "SenderAddress2", "SenderAddress3", "ReservetForXML"
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
                            case "ReservetForXML":
                                foreach (var r in bp.ReservetForXML)
                                {
                                    if (!secondBool)
                                    {
                                        sw.Write(seperator);
                                    }
                                    else
                                    {
                                        secondBool = false;
                                    }
                                    value = r;
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
