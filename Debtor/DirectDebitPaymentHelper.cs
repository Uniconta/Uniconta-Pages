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
using Uniconta.API.System;
using Uniconta.ClientTools.Page;

namespace UnicontaDirectDebitPayment //Uniconta.WPFClient.Pages.Debtor.DirectDebitPayment
{
    class DirectDebitPaymentHelper
    {

        #region Constants
        internal const string VALIDATE_OK = "Ok";

        internal const string DELIVERYTYPE_0690 = "0690"; //Data Delivery 0690 = Receipt and remarks
        internal const string DELIVERYTYPE_0602 = "0602"; //Data Delivery 0602 = Payment Information
        internal const string DELIVERYTYPE_30 = "30"; //Delivery type 30 = Registered and Cancelled mandates

        internal const string TRANSACTIONCODE_0900 = "0900"; //Transaction code 0900 = Receipt for collection
        internal const string TRANSACTIONCODE_0910 = "0910"; //Transaction code 0910 = Remark
        internal const string TRANSACTIONCODE_0920 = "0920"; //Transaction code 0920 = Comment
        internal const string TRANSACTIONCODE_0930 = "0930"; //Transaction code 0930 = Change
        internal const string TRANSACTIONCODE_0500 = "0500"; //Transaction code 0500 = Completed payments
        internal const string TRANSACTIONCODE_0580 = "0580"; //Transaction code 0580 = Completed collection
        internal const string TRANSACTIONCODE_0585 = "0585"; //Transaction code 0585 = Completed disbursement
        internal const string TRANSACTIONCODE_0530 = "0530"; //Transaction code 0530 = Rejected payment by debtor
        internal const string TRANSACTIONCODE_0540 = "0540"; //Transaction code 0540 = Mandate Cancelled before payment date
        internal const string TRANSACTIONCODE_0555 = "0555"; //Transaction code 0555 = Charged back

        internal const string RECORDTYPE_000 = "000"; //Data Delivery Start - Registered and Cancelled mandates
        internal const string RECORDTYPE_042 = "042"; //Receipt for collection

        internal const string RECORDTYPE_001 = "001"; //Creditor (Registered and Cancelled mandates)
        internal const string RECORDTYPE_002 = "002"; //Data Delivery Start
        internal const string RECORDTYPE_510 = "510"; //New mandates (Registered and Cancelled mandates)
        internal const string RECORDTYPE_540 = "540"; //Cancelled mandates (Registered and Cancelled mandates)
        internal const string RECORDTYPE_500 = "500"; //Active mandates (Registered and Cancelled mandates)
        internal const string RECORDTYPE_992 = "992"; //Data Delivery End
        internal const string RECORDTYPE_999 = "999"; //Data Delivery End (Registered and Cancelled mandates)

        //internal const string RECORDTYPE_000 = "000"; //Data Delivery start
        //internal const string RECORDTYPE_001 = "001"; //Creditor
        //internal const string RECORDTYPE_510 = "510"; //Registration of mandate
        //internal const string RECORDTYPE_540 = "540"; //Cancellation of mandate
        internal const string RECORDTYPE_580 = "580"; //Collection
        internal const string RECORDTYPE_585 = "585"; //Disbursement
        internal const string RECORDTYPE_595 = "595"; //Change of mandate
        //internal const string RECORDTYPE_999 = "999"; //Data delivery end

        internal const string UNICONTA_CVR = "33266928";
        internal const string CREDITORNUMBER = "3344"; //Uniconta PBS-nr

        //internal const string DELIVERYTYPE_30 = "30"; //Material type 30 = Registered and cancelled mandates
        internal const string DELIVERYTYPE_40 = "40"; //Material type 40 = Debtor Information
        //internal const string DELIVERYTYPE_0690 = "0690"; //Data Delivery 0690 = Receipt and remarks
        //internal const string DELIVERYTYPE_0602 = "0602"; //Data Delivery 0602 = Payment Information

        internal const string DELIVERY_TEST = "TEST"; //Test Delivery
        internal const int DEADLINE_TIMEOFDAY = 15; 

        #endregion

        #region Member variables
        private int numberSeqFileId;
        private int numberSeqRefId;
        #endregion

        #region Properties
        public int NumberSeqFileId
        {
            get
            {
                return numberSeqFileId;
            }
            set
            {
                numberSeqFileId = value;
            }
        }

        public int NumberSeqRefId
        {
            get
            {
                return numberSeqRefId;
            }
            set
            {
                numberSeqRefId = value;
            }
        }
        #endregion

        public bool MergePayment(Company company, IEnumerable<DebtorTransDirectDebit> lstDebtorTransDirectDebit, DebtorPaymentFormat debtorPaymFormat)
        {
            try
            {
                foreach (var rec in lstDebtorTransDirectDebit)
                {
                    var currencyLocal = rec.Currency ?? company._CurrencyId;
                    if (rec._OnHold || rec._Paid || rec._PaymentFormat != debtorPaymFormat._Format || rec._ErrorInfo != DirectDebitPaymentHelper.VALIDATE_OK || (currencyLocal != Currencies.DKK))
                    {
                        rec._MergeDataId = Uniconta.ClientTools.Localization.lookup("Excluded");
                        rec.NotifyMergeDataIdSet();
                        continue;
                    }

                    StringBuilder mergeDataId = new StringBuilder();

                    mergeDataId.Append(rec.Account).Append('-').Append(rec._PaymentDate.ToString("yyyyMMdd"));

                    rec._MergeDataId = mergeDataId.ToString();
                    rec.NotifyMergeDataIdSet();
                }

                var duplicates = lstDebtorTransDirectDebit.Where(x => x._MergeDataId != Uniconta.ClientTools.Localization.lookup("Excluded")).GroupBy(s => s._MergeDataId).Where(grp => grp.Count() == 1).SelectMany(x => x);  //TODO:Find et nyt navn i stedet for duplicates

                foreach(var rec in duplicates)
                {
                    rec._MergeDataId = "Not merged";
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Warning"), System.Windows.MessageBoxButton.OK);
                return false;
            }
        }

        public void InsertPaymentReference(IEnumerable<DebtorTransDirectDebit> totalPaymentList, CrudAPI capi)
        {
            var lstUpdate = new List<DebtorTransDirectDebit>();
            var lstInsert = new List<DebtorPaymentReference>(); //TODO: VIGTIG Der skal oprettes en Table for DEBTOR - NB. pt. bruges CREDITOR
            foreach (var rec in totalPaymentList)
            {
                if (rec == null || rec._PaymentRefId == 0)
                    continue;

                lstUpdate.Add(rec);

                var debPaymRef = new DebtorPaymentReference();
                debPaymRef._Account = rec.Account;
                debPaymRef._Created = BasePage.GetSystemDefaultDate();
                debPaymRef._PaymentFileId = numberSeqFileId;
                debPaymRef._PaymentRefId = rec._PaymentRefId;
                debPaymRef._TransDate = rec.Date;
                debPaymRef._TransRowId = rec.PrimaryKeyId;
                lstInsert.Add(debPaymRef);

                rec._ErrorInfo = Uniconta.ClientTools.Localization.lookup("Paid");
                rec.Paid = true;
                rec.NotifyErrorSet();

                var statusInfoTxt = rec._StatusInfo;
                switch (rec._PaymentStatus)
                {
                    case PaymentStatusLevel.Resend:
                        statusInfoTxt = string.Format("({0}) Payment Resend to Nets\n{1}", DateTime.Now.ToString("dd.MM.yy HH:mm"), statusInfoTxt);  
                        break;
                    case PaymentStatusLevel.StopPayment:
                        statusInfoTxt = string.Format("({0}) Stop payment request sent to Nets\n{1}", DateTime.Now.ToString("dd.MM.yy HH:mm"), statusInfoTxt);
                        break;
                    default:
                        statusInfoTxt = string.Format("({0}) Sent payment to Nets\n{1}", DateTime.Now.ToString("dd.MM.yy HH:mm"), statusInfoTxt);
                        break;
                }

                while (statusInfoTxt.Length > 1000)
                    statusInfoTxt = statusInfoTxt.Remove(statusInfoTxt.TrimEnd().LastIndexOf("\n"));

                rec._StatusInfo = statusInfoTxt;
                rec._SendTime = BasePage.GetSystemDefaultDate();
                rec._PaymentStatus = PaymentStatusLevel.Sent;
            }

            capi.InsertNoResponse(lstInsert);

            capi.Update(lstUpdate);
        }

        public static void ChangeStatus(Company comp, DebtorTransDirectDebit rec, PaymentStatusLevel changeToStatus)
        {
            var statusInfoTxt = rec._StatusInfo;

            if (changeToStatus == PaymentStatusLevel.None)
            {
                if (rec._PaymentStatus != PaymentStatusLevel.Sent && rec._PaymentStatus != PaymentStatusLevel.Processed && rec._PaymentStatus != PaymentStatusLevel.PaymentReceived && rec._PaymentStatus != changeToStatus)
                {
                    statusInfoTxt = string.Format("({0}) Status changed '{1}'->{2}\n{3}", DateTime.Now.ToString("dd.MM.yy HH:mm"), rec._PaymentStatus, changeToStatus, statusInfoTxt);
                    rec._PaymentStatus = changeToStatus;
                    rec._SendTime = DateTime.MinValue;
                }
                else
                {
                    rec._ErrorInfo = string.Format("Change status from '{0}'->'{1}' not possible", rec._PaymentStatus, changeToStatus);
                }
            }
            else if (changeToStatus == PaymentStatusLevel.Resend)
            {
                if (rec._PaymentStatus != PaymentStatusLevel.Sent && rec._PaymentStatus != PaymentStatusLevel.Processed && rec._PaymentStatus != PaymentStatusLevel.PaymentReceived && rec._PaymentStatus != changeToStatus)
                {
                    statusInfoTxt = string.Format("({0}) Status changed '{1}'->{2}\n{3}", DateTime.Now.ToString("dd.MM.yy HH:mm"), rec._PaymentStatus, changeToStatus, statusInfoTxt);
                    rec._PaymentStatus = changeToStatus;
                    rec._SendTime = DateTime.MinValue;
                }
                else
                {
                    rec._ErrorInfo = string.Format("Change status from '{0}'->'{1}' not possible", rec._PaymentStatus, changeToStatus);
                }
            }
            else if (changeToStatus == PaymentStatusLevel.StopPayment)
            {
                if (rec._PaymentStatus == PaymentStatusLevel.Sent)
                {
                    var today = BasePage.GetSystemDefaultDate();
                    DateTime deadline = new DateTime(rec._PaymentDate.Year, rec._PaymentDate.Month, rec._PaymentDate.Day - 1, DirectDebitPaymentHelper.DEADLINE_TIMEOFDAY, 0, 0);
                    deadline = DirectDebitBankHolidays.AdjustBankDay(comp._CountryId, deadline, false);

                    if (today >= deadline)
                    {
                        rec._ErrorInfo = string.Format("'Stop Payment' not possible (Deadline {0}", deadline.ToString("dd-MM-yyyy kl. HH:mm"));
                        rec.NotifyErrorSet();
                    }
                    else
                    {
                        statusInfoTxt = string.Format("({0}) Status changed '{1}'->{2}\n{3}", DateTime.Now.ToString("dd.MM.yy HH:mm"), rec._PaymentStatus, changeToStatus, statusInfoTxt);
                        rec._PaymentStatus = changeToStatus;
                        rec._SendTime = DateTime.MinValue;
                    }
                }
                else
                {
                    rec._ErrorInfo = string.Format("Change status from '{0}'->'{1}' not possible", rec._PaymentStatus, changeToStatus);
                }
            }
            else if (changeToStatus == PaymentStatusLevel.OnHold)
            {
                if (rec._PaymentStatus != PaymentStatusLevel.Sent && rec._PaymentStatus != PaymentStatusLevel.Processed && rec._PaymentStatus != PaymentStatusLevel.PaymentReceived && rec._PaymentStatus != changeToStatus)
                {
                    statusInfoTxt = string.Format("({0}) Status changed '{1}'->{2}\n{3}", DateTime.Now.ToString("dd.MM.yy HH:mm"), rec._PaymentStatus, changeToStatus, statusInfoTxt);
                    rec._PaymentStatus = changeToStatus;
                    rec._SendTime = DateTime.MinValue;
                }
                else
                {
                    rec._ErrorInfo = string.Format("Change status from '{0}'->'{1}' not possible", rec._PaymentStatus, changeToStatus);
                }
            }

            while (statusInfoTxt != null && statusInfoTxt.Length > 1000)
                statusInfoTxt = statusInfoTxt.Remove(statusInfoTxt.TrimEnd().LastIndexOf("\n"));

            rec._StatusInfo = statusInfoTxt;
            rec.NotifyErrorSet();
        }

        public async Task PaymentRefSequence(CrudAPI capi)
        {
            var seqFileId = 1;
            var seqRefId = 1;
     
            var queryDebtorPaymRef = await capi.Query<DebtorPaymentReference>();
            if (queryDebtorPaymRef.Any())
            {
                seqFileId = queryDebtorPaymRef.Max(s => s._PaymentFileId) + 1;
                seqRefId = queryDebtorPaymRef.Max(s => s._PaymentRefId);
            }

            numberSeqFileId = seqFileId;
            numberSeqRefId = seqRefId;
        }

        public static string UnicontaCountryToISO(CountryCode code)
        {
            return ((CountryISOCode)code).ToString();
        }

        public static string processStringNum(int length, string txt = "")
        {
            if (txt == null)
                txt = string.Empty;
            if (txt.Length > length)
                return txt.Substring(0, length);

            return txt.PadLeft(length, '0');
        }

        public static string processStringAlpha(int length, string txt = " ")
        {
            if (txt == null)
                txt = string.Empty;
            if (txt.Length > length)
                return txt.Substring(0, length);

            return txt.PadRight(length, ' ');
        }

        public static double ConvToDouble(string value)
        {
            return double.Parse(value) / 100;
        }

        public static int ConvToInt(string value)
        {
            return int.Parse(value);
        }

        public static DateTime ConvToDate(string value)
        {
            DateTime convDate = DateTime.MinValue;
            DateTime convDate2 = DateTime.MinValue;

            if (DateTime.TryParseExact(value, "yyMMdd",
                                    new CultureInfo("da"),
                                    DateTimeStyles.None,
                                    out convDate2))
                convDate = convDate2;

            return convDate;
        }

        public static string ConvToStr(string value)
        {
            return value.Trim();
        }

        /// <summary>
        /// Unambiguous identification of the BBAN account of the creditor (domestic account number).
        /// </summary>
        public static Tuple<string, string> DebtorBBAN(string paymentID)
        {
            var bban = string.Empty;
            var regNum = string.Empty;

            bban = paymentID ?? string.Empty;

            bban = Regex.Replace(bban, "[^0-9]", "");

            if (bban != string.Empty)
            {
                regNum = bban.Substring(0, 4);
                bban = bban.Remove(0, 4);
                bban = bban.PadLeft(10, '0');
            }

            return new Tuple<string, string>(regNum, bban);
        }
    }
}
