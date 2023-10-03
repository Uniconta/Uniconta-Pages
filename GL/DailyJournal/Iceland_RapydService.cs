using Borgun.CreditCard.Settlement;
using DevExpress.Xpf.Charts;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Agreement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using UnicontaClient.Pages;
using UnicontaClient.Pages.GL.DailyJournal;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace Rapyd.CreditCard.Settlement
{
    public class Rapyd_CreditCardClient : Iceland_RESTCli
    {
        private string merchantID;
        private DateTime datefrom;
        private DateTime dateto;

        public Rapyd_CreditCardClient(string _username, string _password)
            : base("https://api-saga.valitor.com/HttpServices", _username, _password)
        {
        }

        private RapydAgreements agreements;
        public RapydSettlements settlements;
        public RapydTransactions transactions;
        public RapydDeductions deductions;

        private async void GetAgreements()
        {
            agreements = JsonConvert.DeserializeObject<RapydAgreements>(await api($"/api/agreements"));
        }

        internal async void GetSettlements(string _merchantID, DateTime _datefrom, DateTime _dateto)
        {
            merchantID = _merchantID;
            datefrom = _datefrom;
            dateto = _dateto;
            settlements = data<RapydSettlements>(await api($"/api/settlements?agreementNumber={_merchantID}&" +
                            $"paidDateFrom={_datefrom.ToString("yyyy-MM-ddTHH:mm:ss")}" +
                            $"&paidDateTo={_dateto.ToString("yyyy-MM-ddTHH:mm:ss")}"));
        }

        internal async void GetTransactionsByRunNumber(string settlementRunNumber)
        {
            transactions = data<RapydTransactions>(await api($"/api/transactions?settlementNumber={settlementRunNumber}"));
        }

        internal async void GetDeductionItems(string settlementid)
        {
            var sid = "";
            var settl = data<RapydSettlements>(await api($"/api/settlements?agreementNumber={merchantID}&" +
                                        $"paidDateFrom={datefrom.ToString("yyyy-MM-ddTHH:mm:ss")}" +
                                        $"&paidDateTo={dateto.ToString("yyyy-MM-ddTHH:mm:ss")}" +
                                        $"&settlementNumber={settlementid}"));
            foreach (var se in settl.Settlements)
                if (se.SettlementNumber == settlementid)
                {
                    sid = se.ID;
                    break;
                }

            var h = data<RapydSingleSettlement>(await api($"/api/settlements/id/{sid}"));
            if (h != null)
                deductions = getDeductions(h);
        }

        private RapydDeductions getDeductions(RapydSingleSettlement rapydSettlement)
        {
            var t = new RapydDeductions();
            List<RapydDeduction> items = new List<RapydDeduction>();

            if (rapydSettlement.Settlement != null)
            {
                if (rapydSettlement.Settlement?.FeeAmount != 0.0)
                    items.Add(new RapydDeduction()
                    {
                        Amount = (-1) * (decimal)rapydSettlement.Settlement.FeeAmount,
                        Text = "FeeAmount",
                        Code = "Fee",
                        CurrencyCode = rapydSettlement.Settlement.Currency,
                        SettlementRunNumber = rapydSettlement.Settlement.SettlementNumber
                    });
                if (rapydSettlement.Settlement?.PosFee != 0.0)
                    items.Add(new RapydDeduction()
                    {
                        Amount = (-1) * (decimal)rapydSettlement.Settlement.PosFee,
                        Text = "PosFee",
                        Code = "Pos",
                        CurrencyCode = rapydSettlement.Settlement.Currency,
                        SettlementRunNumber = rapydSettlement.Settlement.SettlementNumber
                    });
            }

            t.DeductedItems = items;
            return t;
        }

        public class RapydDeduction
        {
            public string SettlementRunNumber { get; set; }
            public decimal Amount { get; set; }
            public string Code { get; set; }
            public string CurrencyCode { get; set; }
            public string Text { get; set; }
        }

        public class RapydDeductions
        {
            public List<RapydDeduction> DeductedItems { get; set; }
        }

        private class RapydAgreement
        {
            public string ID { get; set; }
            public string MerchantName { get; set; }
            public string MerchantRegistrationNumber { get; set; }
            public string AgreementNumber { get; set; }
            public string Type { get; set; }
            public string Status { get; set; }
            public string Currency { get; set; }
            public string CurrencyCode { get; set; }
            public double Balance { get; set; }
            public double RrBalance { get; set; }
            public double TotalBalance { get; set; }
            public string MerchantID { get; set; }
            public string PartnerID { get; set; }
            public string RrPercentage { get; set; }
            public string MerchantNumber { get; set; }
            public object TerminalCountByAgreement { get; set; }
        }

        private class RapydAgreements
        {
            public List<RapydAgreement> Agreements { get; set; }
            public DateTime ResponseDateTime { get; set; }
        }

        public class RapydTransactions
        {
            public List<RapydTransaction> Transactions { get; set; }
            public DateTime ResponseDateTime { get; set; }
        }

        public class RapydTransaction
        {
            public string MerchantName { get; set; }
            public string MerchantRegistrationNumber { get; set; }
            public string TransactionID { get; set; }
            public string Arn { get; set; }
            public DateTime? PurchaseDate { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public string CardNumber { get; set; }
            public string TransactionType { get; set; }
            public string Currency { get; set; }
            public double GrossAmount { get; set; }
            public double Interchange { get; set; }
            public double Fees { get; set; }
            public double NetAmount { get; set; }
            public string AuthorizationNumber { get; set; }
            public string TransactionCode { get; set; }
            public string ReasonCode { get; set; }
            public double CashbackAmount { get; set; }
            public string TerminalID { get; set; }
            public string DbaName { get; set; }
            public string PhysicalTerminalID { get; set; }
            public string ID { get; set; }
            public string MerchantID { get; set; }
            public string BatchNumber { get; set; }
            public string SettlementNumber { get; set; }
            public string SettlementID { get; set; }
            public string AgreementID { get; set; }
            public string BatchID { get; set; }
            public string PartnerID { get; set; }
            public string MerchantBucketID { get; set; }
            public string MerchantBucketName { get; set; }
            public double OriginalAmount { get; set; }
            public object OriginalCurrency { get; set; }
            public string CardType { get; set; }
            public string Scheme { get; set; }
            public double SchemeFeeFixed { get; set; }
            public double SchemeFeePercent { get; set; }
            public string SchemeFeeCurrency { get; set; }
            public double SchemeFee { get; set; }
            public double SchemeFeeBase { get; set; }
            public object CardHolderCurrency { get; set; }
            public double CardHolderAmount { get; set; }
            public string CardholderCountry { get; set; }
            public string ReferenceData { get; set; }
            public string TransactionLifeCycleID { get; set; }
            public string SystemSettlementType { get; set; }
            public string SettlementType { get; set; }
            public DateTime? PaidDate { get; set; }
            public string CardCategory { get; set; }
            public string SettlementRunNumber { get => SettlementNumber; set { } }
            public string AuthorizationCode { get => AuthorizationNumber; set { } }
            public string CurrencyCode { get => Currency; set { } }
            public decimal Amount { get => (decimal)GrossAmount; set { } }
            public DateTime? BatchDate { get => PaidDate; set { } }
        }

        public class RapydSettlements
        {
            public List<RapydSettlement> Settlements { get; set; }
            public DateTime ResponseDateTime { get; set; }
        }

        public class RapydSingleSettlement
        {
            public RapydSettlement Settlement { get; set; }
            public DateTime ResponseDateTime { get; set; }
        }

        public class RapydSettlement
        {
            public string MerchantName { get; set; }
            public DateTime? CreatedDate { get; set; }
            public DateTime? ScheduledDate { get; set; }
            public DateTime? PaidDate { get; set; }
            public object PayGross { get; set; }
            public string SettlementNumber { get; set; }
            public string Batches { get; set; }
            public string Transactions { get; set; }
            public string Currency { get; set; }
            public double GrossPurchase { get; set; }
            public double GrossSettlement { get; set; }
            public double NetSettlement { get; set; }
            public double Payout { get; set; }
            public double NetPurchase { get; set; }
            public string ID { get; set; }
            public string AgreementNumber { get; set; }
            public string PartnerID { get; set; }
            public string MerchantID { get; set; }
            public double FeeAmount { get; set; }
            public double RefundAmount { get; set; }
            public double Deduction { get; set; }
            public double Representment { get; set; }
            public double Chargeback { get; set; }
            public double RollingReserve { get; set; }
            public double RollingRelease { get; set; }
            public double ChargebackFees { get; set; }
            public double SwiftFees { get; set; }
            public double SchemeFees { get; set; }
            public double TransactionFee { get; set; }
            public double JoiningFee { get; set; }
            public double Rejected { get; set; }
            public double AddedSum { get; set; }
            public double DeductedSum { get; set; }
            public double Reversals { get; set; }
            public string PreArbitration { get; set; }
            public double DccCommission { get; set; }
            public double PosFee { get; set; }
            public double SettlementFee { get; set; }
            public double SetFee { get; set; }
            public string DBANames { get; set; }
            public string SettlementType { get; set; }
            public double AukakronurFee { get; set; }
            public double OtherFees { get; set; }
            public double GomobileFee { get; set; }
            public double GomobileInitialFee { get; set; }
            public double PreAuthorizationFee { get; set; }
            public double BalanceTransfer { get; set; }
            public double MonthlyFee { get; set; }
            public double AuthorizationFee { get; set; }
            public double MinimumMonthlyServiceFee { get; set; }
            public double CardNotPresentFee { get; set; }
            public double PciFee { get; set; }
            public double MobileAirTimeFee { get; set; }
            public double EcomGatewayMonthlyFee { get; set; }
            public double EcomGatewayTransactionFee { get; set; }
            public string SettlementRunNumber { get => SettlementNumber; set { } }
            public string CurrencyCode { get => Currency; set { } }
            public DateTime? SettlementDate { get => PaidDate; set { } }
            public decimal Payment { get => (decimal)Payout; set { } }
        }
    }
}
