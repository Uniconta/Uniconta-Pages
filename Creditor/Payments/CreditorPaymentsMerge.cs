using UnicontaClient.Pages;
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
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments
{
    class CreditorPaymentsMerge
    {
        public const string MERGEID_SINGLEPAYMENT = "-";
        public bool MergePayments(Company company, PaymentsGrid dgCreditorTranOpenGrid, CreditorPaymentFormat credPaymFormat, SQLCache bankAccountCache)
        {
            string creditorAcc = string.Empty;
            string creditorOCRPaymentId = string.Empty;
            string creditorBIC = string.Empty;
            string mergePaymIdString = string.Empty;

            if (credPaymFormat._PaymentGrouping == PaymentGroupingType.Invoice)
                return false;

            var credCache = company.GetCache(typeof(Uniconta.DataModel.Creditor));

            PaymentISO20022Validate paymentISO20022Validate = new PaymentISO20022Validate();
            BankSpecificSettings bankSpecific = BankSpecificSettings.BankSpecTypeInstance(credPaymFormat);

            try
            {
                var grid = dgCreditorTranOpenGrid.GetVisibleRows() as IEnumerable<CreditorTransPayment>;
                foreach (var rec in grid)
                {
                    if (rec._PaymentFormat != credPaymFormat._Format || rec._OnHold || rec._Paid)
                    {
                        rec._MergePaymId = Uniconta.ClientTools.Localization.lookup("Excluded"); 
                        rec.NotifyMergePaymIdSet();
                        continue;
                    }

                    //Validate payments >>
                    paymentISO20022Validate.CompanyBank(credPaymFormat);  
                    var validateRes = paymentISO20022Validate.ValidateISO20022(company, rec, bankAccountCache);

                    if (validateRes.HasErrors)
                    {
                        foreach (CheckError error in validateRes.CheckErrors)
                        {
                            rec._ErrorInfo += error.ToString() + "\n";
                        }
                        rec._MergePaymId = Uniconta.ClientTools.Localization.lookup("Excluded");
                        rec.NotifyMergePaymIdSet();
                        continue;
                    }
                    //Validate payments <<

                    var creditor = (Uniconta.DataModel.Creditor)credCache.Get(rec.Account);

                    string creditorNumber = creditor._Account;
                    string paymGrpVal = string.Empty;
                    switch (credPaymFormat._PaymentGrouping)
                    {
                        case PaymentGroupingType.Date:
                            paymGrpVal = rec._PaymentDate.ToString("yyyyMMdd");
                            break;

                        case PaymentGroupingType.Week:
                            paymGrpVal = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(rec._PaymentDate, CalendarWeekRule.FirstFullWeek, DayOfWeek.Monday).ToString();
                            break;

                        case PaymentGroupingType.All:
                            break;
                    }

                    StringBuilder mergePaymId = new StringBuilder();

                    switch (rec._PaymentMethod)
                    {
                        case PaymentTypes.VendorBankAccount:
                            creditorAcc = bankSpecific.CreditorBBAN(rec._PaymentId, creditor._PaymentId);
                            creditorOCRPaymentId = bankSpecific.CreditorRefNumber(rec._PaymentId);
                            creditorBIC = bankSpecific.CreditorBIC(rec._SWIFT);
                            if (creditorOCRPaymentId != string.Empty && creditorBIC != string.Empty)
                                mergePaymId.Append(creditorNumber).Append('-').Append(creditorAcc).Append('-').Append(creditorOCRPaymentId).Append('-').Append(creditorBIC).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);
                            else if (creditorOCRPaymentId != string.Empty)
                                mergePaymId.Append(creditorNumber).Append('-').Append(creditorAcc).Append('-').Append(creditorOCRPaymentId).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);
                            else if (creditorBIC != string.Empty)
                                mergePaymId.Append(creditorNumber).Append('-').Append(creditorAcc).Append('-').Append(creditorBIC).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);
                            else
                                mergePaymId.Append(creditorNumber).Append('-').Append(creditorAcc).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);

                            break;

                        case PaymentTypes.IBAN:
                            creditorAcc = bankSpecific.CreditorIBAN(rec._PaymentId, creditor._PaymentId, company._CountryId, creditor._Country);
                            creditorBIC = bankSpecific.CreditorBIC(rec._SWIFT);
                            creditorOCRPaymentId = bankSpecific.CreditorRefNumberIBAN(rec._PaymentId, company._CountryId, creditor._Country);

                            mergePaymId.Append(creditorNumber).Append('-').Append(creditorAcc).Append('-').Append(creditorBIC).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);
                            break;

                        case PaymentTypes.PaymentMethod3: //FIK71
                            var tuple71 = bankSpecific.CreditorFIK71(rec._PaymentId);
                            creditorOCRPaymentId = tuple71.Item1;
                            creditorOCRPaymentId = BaseDocument.FIK71 + "/" + creditorOCRPaymentId;
                            creditorAcc = tuple71.Item2;

                            mergePaymId.Append(creditorNumber).Append('-').Append(creditorOCRPaymentId).Append('-').Append(creditorAcc).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);
                            break;

                        case PaymentTypes.PaymentMethod5: //FIK75
                            var tuple75 = bankSpecific.CreditorFIK75(rec._PaymentId);
                            creditorOCRPaymentId = tuple75.Item1;
                            creditorOCRPaymentId = BaseDocument.FIK75 + "/" + creditorOCRPaymentId;
                            creditorAcc = tuple75.Item2;

                            mergePaymId.Append(creditorNumber).Append('-').Append(creditorOCRPaymentId).Append('-').Append(creditorAcc).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);
                            break;

                        case PaymentTypes.PaymentMethod4: //FIK73
                            creditorOCRPaymentId = BaseDocument.FIK73 + "/";
                            creditorAcc = bankSpecific.CreditorFIK73(rec._PaymentId);

                            mergePaymId.Append(creditorNumber).Append('-').Append(creditorOCRPaymentId).Append('-').Append(creditorAcc).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);
                            break;

                        case PaymentTypes.PaymentMethod6: //FIK04
                            var tuple04 = bankSpecific.CreditorFIK04(rec._PaymentId);
                            creditorOCRPaymentId = tuple04.Item1;
                            creditorOCRPaymentId = BaseDocument.FIK04 + "/" + creditorOCRPaymentId;
                            creditorAcc = tuple04.Item2;

                            mergePaymId.Append(creditorNumber).Append('-').Append(creditorOCRPaymentId).Append('-').Append(creditorAcc).Append('-').Append(rec._CurrencyLocal).Append('-').Append(paymGrpVal);
                            break;
                    }

                    if (mergePaymId.ToString().Substring(mergePaymId.Length - 1) == "-")
                        mergePaymId.Remove(mergePaymId.Length - 1, 1);

                    rec._MergePaymId = mergePaymId.ToString();
                    rec.NotifyMergePaymIdSet();
                }

                var noDuplicates = grid.Where(x => x._MergePaymId != Uniconta.ClientTools.Localization.lookup("Excluded")).GroupBy(s => s._MergePaymId).Where(grp => grp.Count() == 1).SelectMany(x => x);

                foreach (var rec in noDuplicates)
                {
                    rec._MergePaymId = MERGEID_SINGLEPAYMENT;
                }

                return true;
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), System.Windows.MessageBoxButton.OK);
                return false;
            }
        }
    }
}
