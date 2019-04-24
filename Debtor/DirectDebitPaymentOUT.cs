using Corasau.Client.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaISO20022CreditTransfer;
using ISO20022CreditTransfer;
using System.Globalization;
using Uniconta.WPFClient.Pages;
using System.Text.RegularExpressions;
using Uniconta.Common.Utility;
using System.IO;
using Microsoft.Win32;

namespace UnicontaDirectDebitPayment 
{
    class DirectDebitPaymentOUT
    {
        internal List<LSFieldsOUT> listOfTrans = new List<LSFieldsOUT>();

        internal List<DebtorTransPayment> lstDebtorTransOpenLocal;

        internal Company comp;
        internal DateTime paymentDate;
        internal string debtorAccount;
        internal string curDebtorAccount;
        internal string debtorRegNum;
        internal string debtorBankAccount;
        internal string debtorCvrNumber;
        internal string paymentAmountStr;
        internal double paymentAmountTotal;
        internal int countDebtors;
        internal int lineNumber;
        internal double amountCollectionTotal;
        internal double amountDisbursementTotal;
        internal bool headerExp;
        internal bool footerExp;

        //TODO:Validering skal checke om PaymentType er angivet til VendorBankAccount
        //TODO:Der skal valideres for at Debtor CVR nummer er udfyldt
        //Hvad sker der hvis bankkonto ændres uden at NETS har fået besked - der kommer sandsynligvis fejlmelding. Kan det fanges inden ved at kigge i NETS oplysninger angående kunden?
        //TODO:Der må kun være en Collection or Disbursement pr. Debitor pr. dag - det skal der valideres for


        /// <summary>
        /// Creates a NETS Leverandørservice file.
        /// </summary>
        internal void /*DirectDebitGenerateResult*/ StreamToFile(Company company, IEnumerable<DebtorTransDirectDebit> lstDebtorTransDirectDebit, SQLCache DebtorCache, StreamWriter sw)
        {
            comp = company;
            var oldPaymentDate = DateTime.MinValue;

            buildRecordType000();
            foreach (var trans in lstDebtorTransDirectDebit.OrderBy(s => s._PaymentDate).ThenBy(s => s._PaymentAmount).ThenBy(s => s.Account))
            {
                paymentDate = trans.PaymentDate;
                debtorAccount = trans.Account;
                var debtor = (Debtor)DebtorCache.Get(trans.Account);

                debtorRegNum = DirectDebitPaymentHelper.DebtorBBAN(debtor._PaymentId).Item1;
                debtorBankAccount = DirectDebitPaymentHelper.DebtorBBAN(debtor._PaymentId).Item2;
                debtorCvrNumber = debtor?._LegalIdent == null ? "99999999" : Regex.Replace(debtor?._LegalIdent, "[^0-9]", ""); //TODO:SKAL RETTES
                paymentAmountStr = NumberConvert.ToLong(Math.Abs(trans._PaymentAmount) * 100d).ToString();

                paymentAmountTotal += trans._PaymentAmount;
                countDebtors++;

                if (trans.PaymentDate != oldPaymentDate)
                    buildRecordType001();

                //TODO: Registration and Cancellation of mandate - skal håndteres i egen sektion
                //ls.buildRecordType510();
                //ls.buildRecordType540();

                if (trans._PaymentAmount > 0)
                {
                    amountCollectionTotal += trans._PaymentAmount;
                    buildRecordType580();
                }
                else
                {
                    amountDisbursementTotal += trans._PaymentAmount;
                    buildRecordType585();
                }

                //TODO: Change of mandate - skal håndteres i egen sektion
                //ls.buildRecordType595();

                oldPaymentDate = trans.PaymentDate;
            }
            buildRecordType999();

            if (listOfTrans.Count > 0)
            {
                IEnumerable<LSFieldsOUT> queryTrans = listOfTrans.AsEnumerable();//.OrderBy(s => s.LineNumber);

                foreach (var trans in queryTrans)
                {
                    writeRecordType000(sw, trans);
                    writeRecordType001(sw, trans);
                    //ls.writeRecordType510(writer, trans);
                    //ls.writeRecordType540(writer, trans);
                    writeRecordType580(sw, trans);
                    writeRecordType585(sw, trans);
                    writeRecordType595(sw, trans);
                    writeRecordType999(sw, trans);
                }
            }

            //return new DirectDebitGenerateResult(sw, countDebtors);

        }

        void buildRecordType000()
        {
            var lsField = new LSFieldsOUT();

            lineNumber++;
            lsField.LineNumber = lineNumber;

            lsField.RecordType = DirectDebitPaymentHelper.RECORDTYPE_000;

            lsField.Filler01 = DirectDebitPaymentHelper.processStringNum(5);
            lsField.Filler02 = DirectDebitPaymentHelper.processStringNum(15);
            lsField.Filler03 = DirectDebitPaymentHelper.processStringAlpha(7);

            lsField.DataSupplierNo = DirectDebitPaymentHelper.processStringAlpha(6, comp.CompanyId.ToString());
            lsField.DataDeliveryType = DirectDebitPaymentHelper.processStringNum(2, DirectDebitPaymentHelper.DELIVERYTYPE_40);

            lsField.Year = DateTime.Today.ToString("yy");
            lsField.Month = DateTime.Today.ToString("MM");
            lsField.Date = DateTime.Today.ToString("dd");

            lsField.Filler04 = DirectDebitPaymentHelper.processStringAlpha(1);
            lsField.Filler05 = DirectDebitPaymentHelper.processStringAlpha(4, DirectDebitPaymentHelper.DELIVERY_TEST);

            lsField.Filler06 = DirectDebitPaymentHelper.processStringAlpha(1);
            lsField.Filler07 = DirectDebitPaymentHelper.processStringAlpha(3);
            lsField.Filler08 = DirectDebitPaymentHelper.processStringAlpha(9);

            lsField.DataSupplierCVR = DirectDebitPaymentHelper.processStringNum(8, DirectDebitPaymentHelper.UNICONTA_CVR);
            lsField.DataSupplierCVRCheck = DirectDebitPaymentHelper.processStringAlpha(1);

            lsField.Filler09 = DirectDebitPaymentHelper.processStringAlpha(9);

            listOfTrans.Add(lsField);
        }

        void buildRecordType001()
        {
            var lsField = new LSFieldsOUT();

            lineNumber++;
            lsField.LineNumber = lineNumber;

            lsField.RecordType = DirectDebitPaymentHelper.RECORDTYPE_001;

            lsField.CreditorNo = DirectDebitPaymentHelper.processStringNum(5, DirectDebitPaymentHelper.CREDITORNUMBER);
            lsField.Filler01 = DirectDebitPaymentHelper.processStringNum(15);

            lsField.Year = paymentDate.ToString("yy");
            lsField.Month = paymentDate.ToString("MM");
            lsField.Date = paymentDate.ToString("dd");

            lsField.Filler02 = DirectDebitPaymentHelper.processStringNum(14);
            lsField.Filler03 = DirectDebitPaymentHelper.processStringAlpha(37);

            lsField.CustomerNo = DirectDebitPaymentHelper.processStringAlpha(15, debtorAccount);

            listOfTrans.Add(lsField);
        }

        void buildRecordType510()
        {
            var lsField = new LSFieldsOUT();

            lineNumber++;
            lsField.LineNumber = lineNumber;

            lsField.RecordType = DirectDebitPaymentHelper.RECORDTYPE_510;

            lsField.CreditorNo = DirectDebitPaymentHelper.processStringNum(5, DirectDebitPaymentHelper.CREDITORNUMBER);
            lsField.CustomerNo = DirectDebitPaymentHelper.processStringAlpha(15, debtorAccount);

            lsField.DebtorRegNum = DirectDebitPaymentHelper.processStringNum(4, debtorRegNum);
            lsField.DebtorAccNumber = DirectDebitPaymentHelper.processStringNum(10, debtorBankAccount);
            lsField.DebtorCVR = DirectDebitPaymentHelper.processStringNum(8, debtorCvrNumber);

            lsField.Filler01 = DirectDebitPaymentHelper.processStringNum(11);
            lsField.Filler02 = DirectDebitPaymentHelper.processStringAlpha(24);

            listOfTrans.Add(lsField);
        }

        void buildRecordType540()
        {
            var lsField = new LSFieldsOUT();

            lineNumber++;
            lsField.LineNumber = lineNumber;

            lsField.RecordType = DirectDebitPaymentHelper.RECORDTYPE_540;

            lsField.CreditorNo = DirectDebitPaymentHelper.processStringNum(5, DirectDebitPaymentHelper.CREDITORNUMBER);
            lsField.CustomerNo = DirectDebitPaymentHelper.processStringAlpha(15, debtorAccount);

            lsField.Filler01 = DirectDebitPaymentHelper.processStringNum(33);
            lsField.Filler02 = DirectDebitPaymentHelper.processStringAlpha(24);

            listOfTrans.Add(lsField);
        }

        void buildRecordType580()
        {
            var lsField = new LSFieldsOUT();

            lineNumber++;
            lsField.LineNumber = lineNumber;

            lsField.RecordType = DirectDebitPaymentHelper.RECORDTYPE_580;

            lsField.CreditorNo = DirectDebitPaymentHelper.processStringNum(5, DirectDebitPaymentHelper.CREDITORNUMBER);
            lsField.CustomerNo = DirectDebitPaymentHelper.processStringAlpha(15, debtorAccount);

            lsField.Filler01 = DirectDebitPaymentHelper.processStringNum(22);
            lsField.AmountStr = DirectDebitPaymentHelper.processStringNum(11, paymentAmountStr);
            lsField.Filler02 = DirectDebitPaymentHelper.processStringAlpha(24);

            listOfTrans.Add(lsField);
        }

        void buildRecordType585()
        {
            var lsField = new LSFieldsOUT();

            lineNumber++;
            lsField.LineNumber = lineNumber;

            lsField.RecordType = DirectDebitPaymentHelper.RECORDTYPE_585;

            lsField.CreditorNo = DirectDebitPaymentHelper.processStringNum(5, DirectDebitPaymentHelper.CREDITORNUMBER);
            lsField.CustomerNo = DirectDebitPaymentHelper.processStringAlpha(15, debtorAccount); //TODO:Spørg NETS om de kan håndtere Kontonr. "000015" dvs. foranstillede Nul

            lsField.Filler01 = DirectDebitPaymentHelper.processStringNum(22);
            lsField.AmountStr = DirectDebitPaymentHelper.processStringNum(11, paymentAmountStr);
            lsField.Filler02 = DirectDebitPaymentHelper.processStringAlpha(24);

            listOfTrans.Add(lsField);
        }

        void buildRecordType595()
        {
            var lsField = new LSFieldsOUT();

            lineNumber++;
            lsField.LineNumber = lineNumber;

            lsField.RecordType = DirectDebitPaymentHelper.RECORDTYPE_595;

            lsField.CreditorNo = DirectDebitPaymentHelper.processStringNum(5, DirectDebitPaymentHelper.CREDITORNUMBER);
            lsField.CustomerNo = DirectDebitPaymentHelper.processStringAlpha(15, debtorAccount);

            lsField.NewCreditorNo = DirectDebitPaymentHelper.processStringNum(5, DirectDebitPaymentHelper.CREDITORNUMBER);
            lsField.NewCustomerNo = DirectDebitPaymentHelper.processStringNum(15, "88881"); //TODO:TEST

            lsField.Filler01 = DirectDebitPaymentHelper.processStringAlpha(37);

            listOfTrans.Add(lsField);
        }

        void buildRecordType999()
        {
            var lsField = new LSFieldsOUT();

            lineNumber++;
            lsField.LineNumber = lineNumber;

            var amountCollectionTotalStr = NumberConvert.ToLong(Math.Abs(amountCollectionTotal) * 100d).ToString();
            var amountDisbursementTotalStr = NumberConvert.ToLong(Math.Abs(amountDisbursementTotal) * 100d).ToString();

            lsField.RecordType = DirectDebitPaymentHelper.RECORDTYPE_999;

            lsField.CreditorNo = DirectDebitPaymentHelper.processStringNum(5, DirectDebitPaymentHelper.CREDITORNUMBER);

            lsField.Filler01 = DirectDebitPaymentHelper.processStringAlpha(15);

            lsField.NumberOfDebtorsStr = DirectDebitPaymentHelper.processStringNum(7, countDebtors.ToString());
            lsField.AmountCollectionStr = DirectDebitPaymentHelper.processStringNum(13, amountCollectionTotalStr);
            lsField.AmountDisbursementStr = DirectDebitPaymentHelper.processStringNum(13, amountDisbursementTotalStr);

            lsField.Filler02 = DirectDebitPaymentHelper.processStringAlpha(24);

            listOfTrans.Add(lsField);
        }

        void writeRecordType000(StreamWriter w, LSFieldsOUT trans)
        {
            if (trans.RecordType == DirectDebitPaymentHelper.RECORDTYPE_000 && headerExp == false)
            {
                headerExp = true;
                w.WriteLine(string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}{16}",
                                    trans.RecordType,
                                    trans.Filler01,
                                    trans.Filler02,
                                    trans.Filler03,
                                    trans.DataSupplierNo,
                                    trans.DataDeliveryType,
                                    trans.Year,
                                    trans.Month,
                                    trans.Date,
                                    trans.Filler04,
                                    trans.Filler05,
                                    trans.Filler06,
                                    trans.Filler07,
                                    trans.Filler08,
                                    trans.DataSupplierCVR,
                                    trans.DataSupplierCVRCheck,
                                    trans.Filler09));
            }
        }

        void writeRecordType001(StreamWriter w, LSFieldsOUT trans)
        {
            if (trans.RecordType == DirectDebitPaymentHelper.RECORDTYPE_001)
            {
                w.WriteLine(string.Format("{0}{1}{2}{3}{4}{5}{6}{7}",
                                 trans.RecordType,
                                 trans.CreditorNo,
                                 trans.Filler01,
                                 trans.Year,
                                 trans.Month,
                                 trans.Date,
                                 trans.Filler02,
                                 trans.Filler03));
            }
        }

        void writeRecordType510(StreamWriter w, LSFieldsOUT trans)
        {
            if (trans.RecordType == DirectDebitPaymentHelper.RECORDTYPE_510)
            {
                w.WriteLine(string.Format("{0}{1}{2}{3}{4}{5}{6}{7}",
                                 trans.RecordType,
                                 trans.CreditorNo,
                                 trans.CustomerNo,
                                 trans.DebtorRegNum,
                                 trans.DebtorAccNumber,
                                 trans.DebtorCVR,
                                 trans.Filler01,
                                 trans.Filler02));
            }
        }

        void writeRecordType540(StreamWriter w, LSFieldsOUT trans)
        {
            if (trans.RecordType == DirectDebitPaymentHelper.RECORDTYPE_540)
            {
                w.WriteLine(string.Format("{0}{1}{2}{3}{4}",
                                 trans.RecordType,
                                 trans.CreditorNo,
                                 trans.CustomerNo,
                                 trans.Filler01,
                                 trans.Filler02));
            }
        }

        void writeRecordType580(StreamWriter w, LSFieldsOUT trans)
        {
            if (trans.RecordType == DirectDebitPaymentHelper.RECORDTYPE_580)
            {
                w.WriteLine(string.Format("{0}{1}{2}{3}{4}{5}",
                                 trans.RecordType,
                                 trans.CreditorNo,
                                 trans.CustomerNo,
                                 trans.Filler01,
                                 trans.AmountStr,
                                 trans.Filler02));
            }
        }

        void writeRecordType585(StreamWriter w, LSFieldsOUT trans)
        {
            if (trans.RecordType == DirectDebitPaymentHelper.RECORDTYPE_585)
            {
                w.WriteLine(string.Format("{0}{1}{2}{3}{4}{5}",
                                 trans.RecordType,
                                 trans.CreditorNo,
                                 trans.CustomerNo,
                                 trans.Filler01,
                                 trans.AmountStr,
                                 trans.Filler02));
            }
        }


        void writeRecordType595(StreamWriter w, LSFieldsOUT trans)
        {
            if (trans.RecordType == DirectDebitPaymentHelper.RECORDTYPE_595)
            {
                w.WriteLine(string.Format("{0}{1}{2}{3}{4}{5}",
                                 trans.RecordType,
                                 trans.CreditorNo,
                                 trans.CustomerNo,
                                 trans.NewCreditorNo,
                                 trans.NewCustomerNo,
                                 trans.Filler01));
            }
        }

        void writeRecordType999(StreamWriter w, LSFieldsOUT trans)
        {
            if (trans.RecordType == DirectDebitPaymentHelper.RECORDTYPE_999 && footerExp == false)
            {
                footerExp = true;

                w.WriteLine(string.Format("{0}{1}{2}{3}{4}{5}{6}",
                                 trans.RecordType,
                                 trans.CreditorNo,
                                 trans.Filler01,
                                 trans.NumberOfDebtorsStr,
                                 trans.AmountCollectionStr,
                                 trans.AmountDisbursementStr,
                                 trans.Filler02));
            }
        }
    }
}
