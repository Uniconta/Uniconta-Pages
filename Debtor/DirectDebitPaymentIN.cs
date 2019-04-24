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
using Uniconta.API.System;

namespace UnicontaDirectDebitPayment 
{
    class DirectDebitPaymentIN
    {
        private List<LSFieldsIN> listOfTrans = new List<LSFieldsIN>();
        private IEnumerable<DebtorTransDirectDebit> lstDebtorTransDirectDebit;
        private CrudAPI capi;

        /// <summary>
        /// Read a NETS Leverandørservice file.
        /// </summary>
        internal void StreamFromFile(CrudAPI api, string filePath, IEnumerable<DebtorTransDirectDebit> lstDebTrans)
        {
            int lineNumber = 0;
            lstDebtorTransDirectDebit = lstDebTrans;
            capi = api;
            //var filePath = @"C:\Users\lasse\OneDrive\Dokumenter\Uniconta\NETS\LS\Filer\In\";

            string[] files = Directory.GetFiles(filePath);

            LSFieldsIN fieldIN;
            
            var dataDeliveryType = string.Empty;
            bool headerFooter;

            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file);

                foreach (string line in lines)
                {
                    fieldIN = new LSFieldsIN();
                    lineNumber++;
                    headerFooter = true;
                    fieldIN.LineNumber = lineNumber;
                    fieldIN.RecordType = line.Substring(2, 3);

                    switch (fieldIN.RecordType)
                    {
                        case DirectDebitPaymentHelper.RECORDTYPE_000: //Data Delivery Start - (Registered and Cancelled mandates)
                            dataDeliveryType = line.Substring(36, 2);
                            fieldIN.DataDeliveryType = dataDeliveryType;
                            fieldIN.CreationDate = DirectDebitPaymentHelper.ConvToDate(string.Format("{0}{1}{2}", line.Substring(38, 2), line.Substring(40, 2), line.Substring(42, 2)));
                            fieldIN.TestDelivery = line.Substring(45, 4) == DirectDebitPaymentHelper.DELIVERY_TEST ? true : false;
                            fieldIN.DataSupplierNo = line.Substring(62, 8);
                            fieldIN.DataSupplierCVRCheck = line.Substring(70, 1);
                            break;

                        case DirectDebitPaymentHelper.RECORDTYPE_002: //Data Delivery Start - (Receipt and Remarks) and (Payment Information) 
                            fieldIN.DataSupplierNo = line.Substring(5, 8);
                            dataDeliveryType = line.Substring(16, 4);
                            fieldIN.DataDeliveryType = dataDeliveryType;
                            fieldIN.TestDelivery = line.Substring(45, 4) == DirectDebitPaymentHelper.DELIVERY_TEST ? true : false;
                            fieldIN.CreationDate = DirectDebitPaymentHelper.ConvToDate(line.Substring(49, 6));
                            break;

                        case DirectDebitPaymentHelper.RECORDTYPE_992: //Data Delivery End - (Receipt and Remarks) and (Payment Information) 
                            fieldIN.DataSupplierNo = line.Substring(5, 8);
                            dataDeliveryType = line.Substring(16, 4);
                            fieldIN.DataDeliveryType = dataDeliveryType;
                            fieldIN.NumberOfRecType042 = DirectDebitPaymentHelper.ConvToInt(line.Substring(31, 11));
                            break;

                        case DirectDebitPaymentHelper.RECORDTYPE_999: //Data Delivery End - (Registered and Cancelled mandates)
                            fieldIN.CreditorNo = line.Substring(3, 5);
                            fieldIN.NumberOfDebtors = DirectDebitPaymentHelper.ConvToInt(line.Substring(30, 7));
                            break;

                        default:
                            fieldIN.DataDeliveryType = dataDeliveryType;
                            headerFooter = false;
                            break;
                    }

                    if (headerFooter == false)
                    {
                        if (dataDeliveryType == DirectDebitPaymentHelper.DELIVERYTYPE_0690) //Receipt and Remarks
                        {
                            fieldIN.TransactionCode = line.Substring(13, 4);

                            switch (fieldIN.TransactionCode)
                            {
                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0900:
                                    fieldIN.CreditorNo = line.Substring(8, 5);
                                    fieldIN.NumberMandates = DirectDebitPaymentHelper.ConvToInt(line.Substring(17, 7));
                                    fieldIN.PaymentDate = DirectDebitPaymentHelper.ConvToDate(line.Substring(24, 6));
                                    fieldIN.CreditorRegNum = line.Substring(30, 4);
                                    fieldIN.CreditorAccNumber = line.Substring(34, 10);
                                    fieldIN.NumberCollection = DirectDebitPaymentHelper.ConvToInt(line.Substring(44, 7));
                                    fieldIN.AmountCollection = DirectDebitPaymentHelper.ConvToDouble(line.Substring(51, 13));
                                    fieldIN.NumberDisbursement = DirectDebitPaymentHelper.ConvToInt(line.Substring(64, 7));
                                    fieldIN.AmountDisbursement = DirectDebitPaymentHelper.ConvToDouble(line.Substring(71, 13));
                                    break;

                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0910:
                                    fieldIN.CreditorNo = line.Substring(8, 5);
                                    fieldIN.RemarkCommentCode = line.Substring(17, 3);
                                    fieldIN.CustomerNo = DirectDebitPaymentHelper.ConvToStr(line.Substring(20, 15));
                                    fieldIN.DebtorCVR = line.Substring(35, 8);
                                    fieldIN.DebtorRegNum = line.Substring(43, 4);
                                    fieldIN.DebtorAccNumber = line.Substring(47, 10);
                                    fieldIN.PaymentDate = DirectDebitPaymentHelper.ConvToDate(line.Substring(57, 6));
                                    fieldIN.Amount = DirectDebitPaymentHelper.ConvToDouble(line.Substring(63, 13));
                                    fieldIN.ErrorNumber = line.Substring(76, 4);
                                    break;

                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0920:
                                    fieldIN.CreditorNo = line.Substring(8, 5);
                                    fieldIN.RemarkCommentCode = line.Substring(17, 3);
                                    fieldIN.CustomerNo = DirectDebitPaymentHelper.ConvToStr(line.Substring(20, 15));
                                    fieldIN.PaymentDate = DirectDebitPaymentHelper.ConvToDate(line.Substring(35, 6));
                                    fieldIN.Amount = DirectDebitPaymentHelper.ConvToDouble(line.Substring(41, 13));
                                    fieldIN.ErrorNumber = line.Substring(54, 4);
                                    fieldIN.CreditorRegNum = line.Substring(58, 4);
                                    fieldIN.CreditorAccNumber = line.Substring(62, 10);
                                    break;

                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0930:
                                    fieldIN.CreditorNo = line.Substring(8, 5);
                                    fieldIN.ChangeDate = DirectDebitPaymentHelper.ConvToDate(line.Substring(17, 6));
                                    fieldIN.CurCreditorNo = line.Substring(23, 5);
                                    fieldIN.CurCustomerNo = DirectDebitPaymentHelper.ConvToStr(line.Substring(28, 15));
                                    fieldIN.NewCreditorNo = line.Substring(43, 5);
                                    fieldIN.NewCustomerNo = DirectDebitPaymentHelper.ConvToStr(line.Substring(48, 15));
                                    break;
                            }
                        }
                        else if (dataDeliveryType == DirectDebitPaymentHelper.DELIVERYTYPE_0602) //Payment Information
                        {
                            fieldIN.TransactionCode = line.Substring(13, 4);

                            switch (fieldIN.TransactionCode)
                            {
                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0500:
                                    fieldIN.CreditorNo = line.Substring(8, 5);
                                    fieldIN.TaskNumber = line.Substring(17, 8);
                                    fieldIN.Sign1 = line.Substring(40, 1);
                                    fieldIN.Amount1 = DirectDebitPaymentHelper.ConvToDouble(line.Substring(41, 13));
                                    fieldIN.Sign2 = line.Substring(54, 1);
                                    fieldIN.Amount2 = DirectDebitPaymentHelper.ConvToDouble(line.Substring(55, 13));
                                    fieldIN.Sign3 = line.Substring(68, 1);
                                    fieldIN.Amount3 = DirectDebitPaymentHelper.ConvToDouble(line.Substring(69, 13));
                                    fieldIN.CreditorRegNum = line.Substring(82, 4);
                                    fieldIN.CreditorAccNumber = line.Substring(86, 10);
                                    break;

                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0580:
                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0585:
                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0530:
                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0540:
                                case DirectDebitPaymentHelper.TRANSACTIONCODE_0555:
                                    fieldIN.CreditorNo = line.Substring(8, 5);
                                    fieldIN.TaskNumber = line.Substring(17, 8);
                                    fieldIN.CustomerNo = DirectDebitPaymentHelper.ConvToStr(line.Substring(25, 15));
                                    fieldIN.PaymentDate = DirectDebitPaymentHelper.ConvToDate(line.Substring(49, 6));
                                    fieldIN.Sign = line.Substring(55, 1);
                                    fieldIN.Amount = DirectDebitPaymentHelper.ConvToDouble(line.Substring(56, 13));
                                    fieldIN.CreditorRegNum = line.Substring(69, 4);
                                    fieldIN.CreditorAccNumber = line.Substring(73, 10);
                                    break;
                            }
                        }
                        else if (dataDeliveryType == DirectDebitPaymentHelper.DELIVERYTYPE_30) //Registered and Cancelled mandates
                        {
                            fieldIN.RecordType = line.Substring(0, 3);

                            switch (fieldIN.RecordType)
                            {
                                case DirectDebitPaymentHelper.RECORDTYPE_001:
                                    fieldIN.CreditorNo = line.Substring(3, 5);
                                    fieldIN.DataSupplierNo = line.Substring(23, 13);
                                    break;

                                case DirectDebitPaymentHelper.RECORDTYPE_510:
                                case DirectDebitPaymentHelper.RECORDTYPE_540:
                                case DirectDebitPaymentHelper.RECORDTYPE_500:
                                    fieldIN.CreditorNo = line.Substring(3, 5);
                                    fieldIN.CustomerNo = DirectDebitPaymentHelper.ConvToStr(line.Substring(8, 15));
                                    fieldIN.CreationDate = DirectDebitPaymentHelper.ConvToDate(string.Format("{0}{1}{2}", line.Substring(23, 2), line.Substring(25, 2), line.Substring(27, 2)));
                                    break;
                            }
                        }
                    }

                    listOfTrans.Add(fieldIN);
                }
            }

            updateReceipt();

        }

        private void updateReceipt()
        {
            foreach (var rec in listOfTrans.Where(s => s.DataDeliveryType == DirectDebitPaymentHelper.DELIVERYTYPE_0690).OrderBy(s => s.LineNumber))
            {
                if (rec.RecordType == DirectDebitPaymentHelper.RECORDTYPE_042)
                {
                    //if (rec.TransactionCode == DirectDebitPaymentHelper.TRANSACTIONCODE_0900)
                    //{
                    //    MessageBox.Show(Uniconta.ClientTools.Localization.lookup(string.Format("There're {0} Collection - Total amount: {1}", rec.NumberCollection, rec.AmountCollection)));
                    //}

                    if (rec.TransactionCode == DirectDebitPaymentHelper.TRANSACTIONCODE_0920)
                    {
                        var lstUpdate = new List<DebtorTransDirectDebit>();

                        foreach (var trans in lstDebtorTransDirectDebit.Where(s => s.Account == rec.CustomerNo && s.PaymentDate == rec.PaymentDate))
                        {
                            //TODO:Her skal opdateres med nye felter
                            trans.Message = "Error";
                            var errMsg = string.Format("({0}) Error: {1}", DateTime.Now.ToString("dd.MM.yy HH:mm"), Lookups.GetError(rec.ErrorNumber));
                            var commentTxt = string.Format("{0}\n{1}", errMsg, trans.Comment);

                            while (commentTxt.Length > 100)
                                commentTxt = commentTxt.Remove(commentTxt.TrimEnd().LastIndexOf("\n"));

                            trans.Comment = commentTxt;
                            trans._ErrorInfo = string.Empty;
                            trans.NotifyErrorSet();

                            lstUpdate.Add(trans);
                        }

                        if (lstUpdate.Count > 0)
                            capi.Update(lstUpdate);
                    }
                }
            }
        }
    }
}
