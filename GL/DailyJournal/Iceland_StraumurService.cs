using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnicontaClient.Pages;
using UnicontaClient.Pages.GL.DailyJournal;

namespace Straumur.CreditCard.Settlement
{
    public class Straumur_CreditCardClient : Iceland_RESTCli
    {
        public StraumurSettlements settlements;
        public StraumurTransactions transactions;
        public StraumurDeductions deductions;
        private string merchantID;
        private DateTime datefrom;
        private DateTime dateto;
        private decimal afdregingjold;

        public Straumur_CreditCardClient(string _username, string _password) : base("https://partner-api.straumur.is", _username, _password)
        {
        }

        internal async void GetSettlements(string _merchantID, DateTime _datefrom, DateTime _dateto)
        {
            merchantID = _merchantID;
            datefrom = _datefrom;
            dateto = _dateto;
            settlements = data<StraumurSettlements>(await api($"/api/Settlements?AgreementNumber={_merchantID}&" +
                            $"ScheduledDateFrom={_datefrom.ToString("yyyy-MM-ddT00:00:00")}" +
                            $"&ScheduledDateTo={_dateto.ToString("yyyy-MM-ddT00:00:00")}"));
        }

        internal async void GetTransactionsByRunNumber(string settlementRunNumber)
        {
            transactions = data<StraumurTransactions>(await api($"/api/transactions?settlementNumber={settlementRunNumber}"));
        }

        internal async void GetDeductionItems(string settlementid)
        {
            var sid = "";
            var settl = data<StraumurSettlements>(await api($"/api/Settlements?AgreementNumber={merchantID}&" +
                $"ScheduledDateFrom={datefrom.ToString("yyyy-MM-ddT00:00:00")}" +
                $"&ScheduledDateTo={dateto.ToString("yyyy-MM-ddT00:00:00")}"));
            foreach (var se in settl.Settlements)
                if (se.SettlementNumber == settlementid)
                {
                    sid = se.id;
                    break;
                }

            if (sid != "")
            {
                deductions = null;
                var h = data<StraumurSingleSettlement>(await api($"/api/settlements/id/{sid}"));
                if (h != null)
                    deductions = getDeductions(h);
            }
        }

        private StraumurDeduction getFee(int feeno, StraumurSettlement settlement)
        {
            StraumurDeduction feerec = null;
            var texti = "";
            decimal upphaed = 0.0M;
            if (isFee(feeno, settlement, out texti, out upphaed))
            {
                var kodi = texti.Replace("Fee", "");
                if (feeno == 2)
                    kodi = "Pos";
                feerec = new StraumurDeduction()
                {
                    Amount = upphaed,
                    Text = texti,
                    Code = kodi,
                    CurrencyCode = settlement.currency,
                    SettlementRunNumber = settlement.SettlementNumber
                };
            }
            return feerec;
        }

        private StraumurDeductions getDeductions(StraumurSingleSettlement straumurSettlement)
        {
            var t = new StraumurDeductions();
            List<StraumurDeduction> items = new List<StraumurDeduction>();
            afdregingjold = 0.0M;

            if (straumurSettlement.Settlement != null)
            {
                for (var i = 0; i < 18; i++)
                {
                    var f = getFee(i, straumurSettlement.Settlement);
                    if (f != null)
                        items.Add(f);
                }
                if (Math.Round(afdregingjold, 0) != (decimal)straumurSettlement.Settlement.feeAmount)
                {
                    items.Add(new StraumurDeduction()
                    {
                        Amount = (decimal)straumurSettlement.Settlement.feeAmount - Math.Round(afdregingjold + (decimal)straumurSettlement.Settlement.refundAmount, 0),
                        Text = "feeAmount",
                        Code = "fee",
                        CurrencyCode = straumurSettlement.Settlement.currency,
                        SettlementRunNumber = straumurSettlement.Settlement.SettlementNumber
                    });
                }
            }

            t.DeductedItems = items;
            return t;
        }

        public class StraumurTransactions
        {
            public List<StraumurTransaction> Transactions { get; set; }
            public DateTime responseDateTime { get; set; }
        }

        public class StraumurTransaction
        {
            public string merchantName { get; set; }
            public string merchantRegistrationNumber { get; set; }
            public string transactionID { get; set; }
            public string arn { get; set; }
            public DateTime? purchaseDate { get; set; }
            public DateTime registrationDate { get; set; }
            public string cardNumber { get; set; }
            public string transactionType { get; set; }
            public string currency { get; set; }
            public double grossAmount { get; set; }
            public object interchange { get; set; }
            public double fees { get; set; }
            public double netAmount { get; set; }
            public string authorizationNumber { get; set; }
            public string transactionCode { get; set; }
            public string reasonCode { get; set; }
            public double cashbackAmount { get; set; }
            public string terminalID { get; set; }
            public string dbaName { get; set; }
            public string physicalTerminalID { get; set; }
            public string id { get; set; }
            public string merchantID { get; set; }
            public string batchNumber { get; set; }
            public string settlementNumber { get; set; }
            public int settlementID { get; set; }
            public string agreementID { get; set; }
            public string batchID { get; set; }
            public string partnerID { get; set; }
            public string merchantBucketID { get; set; }
            public string merchantBucketName { get; set; }
            public double originalAmount { get; set; }
            public string originalCurrency { get; set; }
            public string cardType { get; set; }
            public string scheme { get; set; }
            public object schemeFeeFixed { get; set; }
            public object schemeFeePercent { get; set; }
            public object schemeFeeCurrency { get; set; }
            public object schemeFee { get; set; }
            public object schemeFeeBase { get; set; }
            public string cardHolderCurrency { get; set; }
            public double cardHolderAmount { get; set; }
            public string referenceData { get; set; }
            public string transactionLifeCycleID { get; set; }
            public string systemSettlementType { get; set; }
            public string settlementType { get; set; }
            public DateTime? paidDate { get; set; }
            public string SettlementRunNumber { get => settlementNumber; set { } }
            public string AuthorizationCode { get => authorizationNumber; set { } }
            public string CurrencyCode { get => originalCurrency; set { } }
            public decimal Amount { get => (decimal)grossAmount; set { } }
            public DateTime? BatchDate
            {
                get
                {
                    if (paidDate.HasValue)
                    {
                        return paidDate;
                    }
                    else if (purchaseDate.HasValue)
                    {
                        return purchaseDate;
                    }
                    return DateTime.MinValue;
                }
                set { }
            }
        }

        public class StraumurSettlements
        {
            public List<StraumurSettlement> Settlements { get; set; }
            public DateTime responseDateTime { get; set; }
        }

        public class StraumurSettlement
        {
            public string merchantName { get; set; }
            public DateTime createdDate { get; set; }
            public DateTime scheduledDate { get; set; }
            public object paidDate { get; set; }
            public string payGross { get; set; }
            public int settlementNumber { get; set; }
            public string reference { get; set; }
            public int batches { get; set; }
            public int transactions { get; set; }
            public string currency { get; set; }
            public double grossPurchase { get; set; }
            public double grossSettlement { get; set; }
            public double netSettlement { get; set; }
            public double payout { get; set; }
            public double netPurchase { get; set; }
            public string id { get; set; }
            public string agreementNumber { get; set; }
            public object partnerID { get; set; }
            public string merchantID { get; set; }
            public double feeAmount { get; set; }
            public double refundAmount { get; set; }
            public double deduction { get; set; }
            public double representment { get; set; }
            public double chargeback { get; set; }
            public int rollingReserve { get; set; }
            public int rollingRelease { get; set; }
            public double chargebackFees { get; set; }
            public double swiftFees { get; set; }
            public double schemeFees { get; set; }
            public double transactionFee { get; set; }
            public double joiningFee { get; set; }
            public double rejected { get; set; }
            public int addedSum { get; set; }
            public int deductedSum { get; set; }
            public double reversals { get; set; }
            public double preArbitration { get; set; }
            public double dccCommission { get; set; }
            public double posFee { get; set; }
            public double settlementFee { get; set; }
            public int setFee { get; set; }
            public List<string> dbaNames { get; set; }
            public int settlementType { get; set; }
            public int aukakronurFee { get; set; }
            public int otherFees { get; set; }
            public int gomobileFee { get; set; }
            public int gomobileInitialFee { get; set; }
            public int preAuthorizationFee { get; set; }
            public int monthlyFee { get; set; }
            public double authorizationFee { get; set; }
            public int minimumMonthlyServiceFee { get; set; }
            public int cardNotPresentFee { get; set; }
            public int pciFee { get; set; }
            public int mobileAirTimeFee { get; set; }
            public double ecomGatewayMonthlyFee { get; set; }
            public int ecomGatewayTransactionFee { get; set; }
            public string SettlementNumber { get => reference; set { } }
            public string SettlementRunNumber { get => reference; set { } }
            public string CurrencyCode { get => currency; set { } }
            public DateTime? SettlementDate { get => scheduledDate; set { } }
            public decimal Payment { get => (decimal)payout; set { } }
        }

        public class StraumurSingleSettlement
        {
            public StraumurSettlement Settlement { get; set; }
            public DateTime ResponseDateTime { get; set; }
            public string responseIdentifier { get; set; }
        }

        public class StraumurDeduction
        {
            public string SettlementRunNumber { get; set; }
            public decimal Amount { get; set; }
            public string Code { get; set; }
            public string CurrencyCode { get; set; }
            public string Text { get; set; }
        }

        public class StraumurDeductions
        {
            public List<StraumurDeduction> DeductedItems { get; set; }
        }

        private bool isFee(int feeno, StraumurSettlement settlement, out string texti, out decimal upphaed)
        {
            var fees = new (Func<decimal> Fee, string Text)[]
            {
                (() => (decimal)settlement.transactionFee,              "transactionFee"),
                (() => (decimal)settlement.joiningFee,                  "joiningFee"),
                (() => (decimal)settlement.posFee,                      "posFee"),
                (() => (decimal)settlement.settlementFee,               "settlementFee"),
                (() => (decimal)settlement.setFee,                      "setFee"),
                (() => (decimal)settlement.aukakronurFee,               "aukakronurFee"),
                (() => (decimal)settlement.otherFees,                   "otherFees"),
                (() => (decimal)settlement.gomobileFee,                 "gomobileFee"),
                (() => (decimal)settlement.gomobileInitialFee,          "gomobileInitialFee"),
                (() => (decimal)settlement.preAuthorizationFee,         "preAuthorizationFee"),
                (() => (decimal)settlement.monthlyFee,                  "monthlyFee"),
                (() => (decimal)settlement.authorizationFee,            "authorizationFee"),
                (() => (decimal)settlement.minimumMonthlyServiceFee,    "minimumMonthlyServiceFee"),
                (() => (decimal)settlement.cardNotPresentFee,           "cardNotPresentFee"),
                (() => (decimal)settlement.pciFee,                      "pciFee"),
                (() => (decimal)settlement.mobileAirTimeFee,            "mobileAirTimeFee"),
                (() => (decimal)settlement.ecomGatewayMonthlyFee,       "ecomGatewayMonthlyFee"),
                (() => (decimal)settlement.ecomGatewayTransactionFee,   "ecomGatewayTransactionFee")
            };

            texti = "";
            upphaed = 0.0M;

            if (feeno >= 0 && feeno < fees.Length)
            {
                decimal feeValue = fees[feeno].Fee();
                if (feeValue != 0)
                {
                    texti = fees[feeno].Text;
                    upphaed = Math.Round(feeValue, 0);
                    afdregingjold = afdregingjold + feeValue;
                    return true;
                }
            }

            return false;
        }
    }
}
