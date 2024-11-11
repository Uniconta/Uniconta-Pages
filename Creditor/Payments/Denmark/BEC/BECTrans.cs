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
using Uniconta.DataModel;
using UnicontaClient.Creditor.Payments;
using System.Text.RegularExpressions;
using UnicontaISO20022CreditTransfer;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public partial class CreateBECFileFormatBase : BankFormatBase
    {
        public DanishFormatFieldBase CreateFormatField(CreditorTransPayment tran,
            CreditorPaymentFormat paymentFormat, 
            BankStatement bankAccount,
            Uniconta.DataModel.Creditor creditor,
            Company company,
            bool glJournalGenerated = false)
        {
            var danishFields = new BECFormatFields();

            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);

            switch (tran._PaymentMethod)
            {
                case PaymentTypes.VendorBankAccount:
                    danishFields.TransTypeCommand = BECPayFormat.TRANSTYPE_ERH356;
                    BBANIBANPaymentType(danishFields, tran);
                    break;
                case PaymentTypes.IBAN:
                    danishFields.TransTypeCommand = BECPayFormat.TRANSTYPE_ERH400;
                    BBANIBANPaymentType(danishFields, tran);
                    danishFields.ToAccountNumber = danishFields.ToAccountNumber != string.Empty ? danishFields.ToAccountNumber : danishFields.ReceiverIBAN;
                    danishFields.PaymentId = danishFields.SwiftAddress;
                    break;
                case PaymentTypes.PaymentMethod3:
                    SharedFIKPayment(danishFields, tran);
                    danishFields.TransTypeCommand = BECPayFormat.TRANSTYPE_ERH351;
                    danishFields.PaymentId = string.Format("{0}{1}", BankFormatBase.FIK71, danishFields.PaymentId);
                    danishFields.ToAccountNumber = string.Format("{0}    {1}", BankFormatBase.FI, danishFields.ToAccountNumber);
                    break;
                case PaymentTypes.PaymentMethod4:
                    SharedFIKPayment(danishFields, tran);
                    danishFields.TransTypeCommand = BECPayFormat.TRANSTYPE_ERH357;
                    danishFields.PaymentId = string.Format("{0}{1}", BankFormatBase.FIK73, danishFields.PaymentId);
                    danishFields.ToAccountNumber = string.Format("{0}    {1}", BankFormatBase.FI, danishFields.ToAccountNumber);
                    break;
                case PaymentTypes.PaymentMethod5:
                    SharedFIKPayment(danishFields, tran);
                    danishFields.TransTypeCommand = BECPayFormat.TRANSTYPE_ERH358;
                    danishFields.PaymentId = string.Format("{0}{1}", BankFormatBase.FIK75, danishFields.PaymentId);
                    danishFields.ToAccountNumber = string.Format("{0}    {1}", BankFormatBase.FI, danishFields.ToAccountNumber);
                    break;
                case PaymentTypes.PaymentMethod6:
                    SharedFIKPayment(danishFields, tran);
                    danishFields.TransTypeCommand = BECPayFormat.TRANSTYPE_ERH352;
                    danishFields.PaymentId = string.Format("{0}{1}", BankFormatBase.FIK04, danishFields.PaymentId);
                    danishFields.ToAccountNumber = string.Format("{0}  {1}", BankFormatBase.GIRO, danishFields.ToAccountNumber);
                    break;
                default:
                    break;
            }

            danishFields.BeneficiaryName = glJournalGenerated ? string.Empty : ShortenWordToCriteria(tran.Creditor.Name, 35); 

            var paymentAmount = Math.Round(tran.PaymentAmount, 2);

            if (danishFields.TransTypeCommand == BECPayFormat.TRANSTYPE_ERH400)
            {
                danishFields.Column06 = ShortenWordToCriteria(string.Format("{0}, {1} {2}", tran.Creditor.Address1, tran.Creditor.ZipCode, tran.Creditor.City), 35); //Modtager adresse1
                danishFields.Column07 = ShortenWordToCriteria(tran.Creditor.Address2, 35); //Modtager adresse2
                danishFields.Column08 = ShortenWordToCriteria(tran.Creditor.Address3, 35); //Modtager adresse3
                danishFields.AmountForeignStr = paymentAmount.ToString("F");
                danishFields.CurrencyCode = danishFields.Currency.ToString();
                danishFields.ReferenceToPrimaryDoc = "02";
                danishFields.ExpenseCode = "S";
            }
            else
            {
                danishFields.Column06 = string.Empty; //Frekvens
                danishFields.Column07 = string.Empty; //Antal gange
                danishFields.Column08 = string.Empty; //Slutdato
                danishFields.AmountLocalStr = paymentAmount.ToString("F");
                danishFields.CurrencyCode = String.Empty;
                danishFields.ReferenceToPrimaryDoc = "N";
                danishFields.ExpenseCode = string.Empty;
            }

            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);
            danishFields.OwnVoucherNumber = ShortenWordToCriteria(internalAdvText, 20);

            danishFields.ClearingCode = string.Empty;
            danishFields.Blanks2 = string.Empty;

            danishFields.NotUsed = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            danishFields.NotUsed02 = new List<string>()
            {
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty
            };

            if (tran._PaymentMethod == PaymentTypes.VendorBankAccount || tran._PaymentMethod == PaymentTypes.PaymentMethod4)
            {
                danishFields.SenderName = ShortenWordToCriteria(company._Name, 35);
                danishFields.SenderAddress1 = ShortenWordToCriteria(company._Address1, 35);
                danishFields.SenderAddress2 = ShortenWordToCriteria(company._Address2, 35);
                danishFields.SenderAddress3 = ShortenWordToCriteria(company._Address3, 35);
            }
            else
            {
                danishFields.SenderName = string.Empty;
                danishFields.SenderAddress1 = string.Empty;
                danishFields.SenderAddress2 = string.Empty;
                danishFields.SenderAddress3 = string.Empty;
            }

            danishFields.Blanks = string.Empty;

            var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat, tran, company, creditor);
            var message = externalAdvText;

            if (tran._PaymentMethod == PaymentTypes.VendorBankAccount && danishFields.TransTypeCommand != BECPayFormat.TRANSTYPE_ERH400)
                danishFields.PaymentId = ShortenWordToCriteria(message, 20);


            //Extended notification
            if (danishFields.TransTypeCommand != BECPayFormat.TRANSTYPE_ERH400)
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
            int maxLines = 6;

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

            danishFields.UniquePaymRef = tran.PaymentEndToEndId.ToString();

            return danishFields;
        }

        
        public void BBANIBANPaymentType(DanishFormatFieldBase _field, CreditorTransPayment tran)
        {
            var field = _field as BECFormatFields;

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
                    iban = iban.ToUpper();
                    countryId = iban.Substring(0, 2);
                }
                field.ToAccountNumber = string.Empty;
            }
            else
            {
                bban = tran.PaymentId ?? string.Empty;
                bban = Regex.Replace(bban, "[^0-9]", "");
                regNum = bban.Substring(0, 4);
                bban = bban.Remove(0, 4);
                bban = bban.PadLeft(10, '0');
                field.ToAccountNumber = string.Format("{0}{1}", regNum, bban);
            }

            swift = tran._SWIFT ?? string.Empty;
            if (swift != string.Empty)
            {
                swift = Regex.Replace(swift, "[^\\w\\d]", "");
                swift = swift.ToUpper();
                if (swift.Length >= 6)
                    countryId = countryId == string.Empty ? swift.Substring(4, 2) : countryId;
            }


            if (tran._PaymentMethod == PaymentTypes.VendorBankAccount && countryId != string.Empty && countryId != BaseDocument.COUNTRY_DK)
            {
                field.TransTypeCommand = BECPayFormat.TRANSTYPE_ERH400;

                bban = tran.PaymentId ?? string.Empty;
                bban = Regex.Replace(bban, "[^0-9]", "");
                field.ToAccountNumber = bban;
                field.PaymentId = swift;
            }


            field.SwiftAddress = swift;
            field.ReceiverIBAN = iban;
            field.CountryCode = countryId;
        }

        public void StreamToFile(List<DanishFormatFieldBase> listOfDanskeBankPayments, StreamWriter sw)
        {
            char seperator = ',';

            var type = (BECFormatFields)listOfDanskeBankPayments[0];

            var outputFields = new[]
            {
                "TransTypeCommand", "FromAccountNumber", "OwnVoucherNumber", "ToAccountNumber", "BeneficiaryName", "Column06",
                "Column07", "Column08", "AmountLocalStr", "TransferDate", "PaymentId", "ClearingCode", "CurrencyCode", "AmountForeignStr", "NotUsed",
                "ReferenceToPrimaryDoc", "Blanks2", "ExpenseCode", "NotUsed02", "SenderName","SenderAddress1","SenderAddress2","SenderAddress3","Blanks", "Messages"
            };

            var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();

            foreach (var bff in listOfDanskeBankPayments)
            {
                var bp = (BECFormatFields)bff;

                if (bp.TransTypeCommand == BECPayFormat.TRANSTYPE_ERH356 ||
                    bp.TransTypeCommand == BECPayFormat.TRANSTYPE_ERH351 ||
                    bp.TransTypeCommand == BECPayFormat.TRANSTYPE_ERH352 ||
                    bp.TransTypeCommand == BECPayFormat.TRANSTYPE_ERH357 ||
                    bp.TransTypeCommand == BECPayFormat.TRANSTYPE_ERH358 ||
                    bp.TransTypeCommand == BECPayFormat.TRANSTYPE_ERH400)
                {

                    bool firstColumn = true;
                    int countOffields = 0;

                    foreach (FieldInfo field in fields)
                    {
                        bool secondBool = true;

                        countOffields++;

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
                            default:
                                if (val is DateTime)
                                {
                                    value = ((DateTime)val).ToString("ddMMyy");
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
