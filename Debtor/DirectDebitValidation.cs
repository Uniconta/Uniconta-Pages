using System;
using System.Collections.Generic;
using System.Xml;

using Uniconta;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaAPI;
using Uniconta.ClientTools.DataModel;
using System.Text.RegularExpressions;
using System.Text;
using Corasau.Client.Pages;
using System.Windows;
using Uniconta.ClientTools.Page;
using Uniconta.WPFClient.Pages;
using System.Linq;

namespace UnicontaDirectDebitPayment
{
    /// <summary>
    /// 
    /// </summary>
    public class DirectDebitValidation
    {
        #region Variables
        //private CompanyBankENUM companyBankEnum;
        //private BankSpecificSettings bankSpecificSettings;
        private CreditorPaymentFormat credPaymFormat;
        private ExportFormatType exportFormat;
        private string companyCountryId;
        private DateTime todayDateTime;
        private Company comp;
        private bool err;

        protected string creditorIBAN = string.Empty;
        protected string creditorSWIFT = string.Empty;

        // List<CheckError> checkErrors = new List<CheckError>();
        #endregion

        #region Properties
        //private List<CheckError> CheckError
        //{
        //    get
        //    {
        //        return checkErrors;
        //    }

        //    set
        //    {
        //        checkErrors = value;
        //    }
        //}
        #endregion


        static public string UnicontaCountryToISO(CountryCode code)
        {
            return ((CountryISOCode)code).ToString();
        }

        //public void CompanyBank(CreditorPaymentFormat credPaymFormat)
        //{
        //    BankSpecificSettings bankSpecific = BankSpecificSettings.BankSpecTypeInstance(credPaymFormat);
        //    this.bankSpecificSettings = bankSpecific;
        //    this.credPaymFormat = credPaymFormat;

        //    CompanyBankEnum = bankSpecific.CompanyBank();
        //}

        /// <summary>
        /// Validate proposed payment records before generating the Direct Debit Payment file.
        /// </summary>
        /// <param name="xxx">xxx.</param> //TODO:Mangler at specificere parametre
        /// <param name="xxx">xxx.</param>
        /// <returns></returns>
        public void Validate(Company company, IEnumerable<DebtorTransDirectDebit> queryPaymentTrans, bool mergedPayment)
        {
            //TODO:List CheckErrors kan fjernes

            todayDateTime = BasePage.GetSystemDefaultDate();
            comp = company;

            var countErr = 0;
            foreach (var rec in queryPaymentTrans)
            {
                rec._ErrorInfo = null;

                //Validations >>
                if (mergedPayment && rec._MergeDataId == null)
                {
                    countErr++;
                    rec._ErrorInfo = "Merge of payments has failed";
                }

                var currencyLocal = rec.Currency ?? comp._CurrencyId;
                if (currencyLocal != Currencies.DKK)
                    rec._ErrorInfo = String.Format("Only currency DKK is valid for NETS Leverandørservice.");

                switch (rec._PaymentStatus)
                {
                    case PaymentStatusLevel.None: break;
                    case PaymentStatusLevel.Sent: rec._ErrorInfo = String.Format("Payment has been sent - awaiting return file"); break;
                    case PaymentStatusLevel.Processed: rec._ErrorInfo = String.Format("Payment beeing processed by NETS"); break;
                    case PaymentStatusLevel.Unsubscribed: rec._ErrorInfo = String.Format("Please, read payment status info"); break;
                    case PaymentStatusLevel.Rejected: rec._ErrorInfo = String.Format("Please, read payment status info"); break;
                    case PaymentStatusLevel.Reversed: rec._ErrorInfo = String.Format("Please, read payment status info"); break;
                    case PaymentStatusLevel.Error: rec._ErrorInfo = String.Format("Please, read payment status info"); break;
                    case PaymentStatusLevel.PaymentReceived: rec._ErrorInfo = String.Format("Payment has been received"); break;
                    case PaymentStatusLevel.Resend: rec._ErrorInfo = String.Format("Payment need to be sent"); break;
                    case PaymentStatusLevel.StopPayment: rec._ErrorInfo = String.Format("Payment need to be sent"); break;
                    case PaymentStatusLevel.OnHold: rec._ErrorInfo = String.Format("Payment is onhold"); break;
                }

                ValidateBankholiday(rec);
                ValidateDeadline(rec);


                //Validations <<

                rec._ErrorInfo = rec._ErrorInfo ?? DirectDebitPaymentHelper.VALIDATE_OK;

                rec.NotifyErrorSet();
            }

            //Post-validation >>
            if (!mergedPayment)
            {
                var validateMulti = queryPaymentTrans.Where(x => x._MergeDataId != Uniconta.ClientTools.Localization.lookup("Excluded")).GroupBy(x => new { x.Account, x.PaymentDate }).Where(grp => grp.Count() > 1).SelectMany(x => x);

                foreach (var rec in validateMulti)
                {
                    rec._MergeDataId = Uniconta.ClientTools.Localization.lookup("Excluded");
                    rec._ErrorInfo = "It’s only allowed to have one payment per Day per Customer (Use Merge payment or change payment date)";
                    rec.NotifyErrorSet();
                }
            }
            //Post-validation <<
        }

        private void ValidateBankholiday(DebtorTransDirectDebit rec)
        {
            if (err)
                return;

            if (!DirectDebitBankHolidays.IsBankDay(comp._CountryId, rec.PaymentDate))
            {
                rec._ErrorInfo = String.Format("Payment date {0} is not a bank day", rec._PaymentDate.ToString("dd-MM-yyyy"));
                err = true;
            }
        }

        private void ValidateDeadline(DebtorTransDirectDebit rec)
        {
            if (err)
                return;

            DateTime deadline = new DateTime(rec._PaymentDate.Year, rec._PaymentDate.Month, rec._PaymentDate.Day - 1, DirectDebitPaymentHelper.DEADLINE_TIMEOFDAY, 0, 0);
            deadline = DirectDebitBankHolidays.AdjustBankDay(comp._CountryId, deadline, false);

            if (todayDateTime >= deadline)
            {
                //TODO:Kan erstattes af en linje når Enum for Status bliver oprettet
                switch (rec._PaymentStatus)
                {
                    case PaymentStatusLevel.None:
                        rec._ErrorInfo = string.Format("'Sent payment' not possible (Deadline {0}", deadline.ToString("dd-MM-yyyy kl. HH:mm"));
                        break;
                    case PaymentStatusLevel.Resend:
                        rec._ErrorInfo = string.Format("'Resend payment' not possible (Deadline {0}", deadline.ToString("dd-MM-yyyy kl. HH:mm"));
                        break;
                    case PaymentStatusLevel.StopPayment:
                        rec._ErrorInfo = string.Format("'Stop payment' not possible (Deadline {0}", deadline.ToString("dd-MM-yyyy kl. HH:mm"));
                        break;
                }
            }
        }
    }
}

