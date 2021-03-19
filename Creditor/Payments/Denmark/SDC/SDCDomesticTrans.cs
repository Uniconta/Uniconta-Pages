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
    public partial class CreateSDCFileFormatBase : BankFormatBase
    {

        public DanishFormatFieldBase CreateDomesticFormatField(CreditorTransPayment tran, CreditorPaymentFormat paymentFormat, BankStatement bankAccount, 
                                                               Uniconta.DataModel.Creditor creditor, Company company, bool glJournalGenerated = false)
        {

            var danishFields = new SDCFormatFields();
            SharedCodeForCreateBankFormatFields(company, tran, paymentFormat, bankAccount, danishFields);
            DomesticPaymentType(danishFields, tran);
          
            danishFields.TransTypeCommand = SDCPayFormat.RECORDTYPE_3;

            var paymentAmount = Math.Round(tran.PaymentAmount, 2);
            var paymentAmountSTR = paymentAmount.ToString("F");
            danishFields.AmountSTR = NETSNorge.processString(paymentAmountSTR, 15, true);
            danishFields.Receipt = "N";

            var internalAdvText = StandardPaymentFunctions.InternalMessage(paymentFormat._OurMessage, tran, company, creditor);
            danishFields.OwnVoucherNumber = NETSNorge.processString(internalAdvText, 20, false);

            var externalAdvText = StandardPaymentFunctions.ExternalMessage(paymentFormat._Message, tran, company, creditor);

            danishFields.Blanks = NETSNorge.processString(string.Empty, 4, false);

            danishFields.BeneficiaryAdviceText = NETSNorge.processString(externalAdvText, 20, false);

            danishFields.UniquePaymRef = tran.PaymentEndToEndId.ToString();

            return danishFields;
        }

        public void DomesticPaymentType(DanishFormatFieldBase _field, CreditorTransPayment tran)
        {
            var field = _field as SDCFormatFields;

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

        public void StreamToDomesticFile(List<DanishFormatFieldBase> listOfSDCPayments, StreamWriter sw)
        {
            var type = (SDCFormatFields)listOfSDCPayments[0];

            foreach (var dFFdB in listOfSDCPayments)
            {
                var bp = (SDCFormatFields)dFFdB;

                if (bp.TransTypeCommand == SDCPayFormat.RECORDTYPE_3)
                {
                    var outputFields = new[]
                    {
                        "TransTypeCommand", "FromAccountNumber", "TransferDate", "AmountSTR", "Receipt", "OwnVoucherNumber",
                        "ToAccountRegNr", "ToAccountNumber", "Blanks", "BeneficiaryAdviceText"
                    };

                    var fields = outputFields.Select(fld => type.GetType().GetField(fld)).ToList();
                    foreach (FieldInfo field in fields)
                    {
                        var val = field.GetValue(bp);

                        string value;
                        if (val is DateTime)
                            value = ((DateTime)val).ToString("ddMMyy");
                        else
                            value = Convert.ToString(val);

                        value = Regex.Replace(value, "[\"\';]", " ");

                        sw.Write(value);
                    }
                    sw.WriteLine();
                }
            }
        }
    }
}
