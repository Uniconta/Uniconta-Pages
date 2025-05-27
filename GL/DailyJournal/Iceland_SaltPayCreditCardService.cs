using Borgun.CreditCard.Settlement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Uniconta.DataModel;

namespace Borgun.CreditCard.Settlement
{
    public class SaltPay_CreditCardClient : IDisposable
    {
        public ICreditCard CreditCardClient { get; set; }
        private const string EndpointUri = "https://services.borgun.is/settlement_3_6/CreditCard.svc";

        public SaltPay_CreditCardClient(string _username, string _password)
        {
            var basicHttpBinding = new BasicHttpBinding(BasicHttpSecurityMode.Transport);
            basicHttpBinding.MaxReceivedMessageSize = 1000000;
            basicHttpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;
            var endpointAddress = new EndpointAddress(EndpointUri);
            var factory = new ChannelFactory<ICreditCard>(basicHttpBinding, endpointAddress);
            factory.Credentials.UserName.UserName = _username;
            factory.Credentials.UserName.Password = _password;
            CreditCardClient = factory.CreateChannel();
        }

        public void Dispose()
        {            
            ((IDisposable)CreditCardClient)?.Dispose();
        }
    }

    [System.Runtime.Serialization.DataContractAttribute(Name = "CreditCardSettlement", Namespace = "http://schemas.datacontract.org/2004/07/Borgun.Services.Settlement")]
    public partial class CreditCardSettlement : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        private decimal AmexAmountField;
        private decimal AmountField;
        private CreditCardBatch[] BatchesField;
        private decimal CommissionField;
        private decimal CupAmountField;
        private string CurrencyCodeField;
        private decimal DeductionField;
        private CreditCardSettlementDeduction[] DeductionItemsField;
        private decimal DinersAmountField;
        private decimal JcbAmountField;
        private decimal MasterCardAmountField;
        private string MerchantIDField;
        private decimal PaymentField;
        private System.DateTime SettlementDateField;
        private string SettlementRunNumberField;
        private int SlipCountField;
        private decimal VisaAmountField;
        private decimal BeginBalanceField;

        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal AmexAmount
        {
            get { return this.AmexAmountField; }
            set { this.AmexAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Amount
        {
            get { return this.AmountField; }
            set { this.AmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public CreditCardBatch[] Batches
        {
            get { return this.BatchesField; }
            set { this.BatchesField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Commission
        {
            get { return this.CommissionField; }
            set { this.CommissionField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal CupAmount
        {
            get { return this.CupAmountField; }
            set { this.CupAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string CurrencyCode
        {
            get { return this.CurrencyCodeField; }
            set { this.CurrencyCodeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Deduction
        {
            get { return this.DeductionField; }
            set { this.DeductionField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public CreditCardSettlementDeduction[] DeductionItems
        {
            get { return this.DeductionItemsField; }
            set { this.DeductionItemsField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal DinersAmount
        {
            get { return this.DinersAmountField; }
            set { this.DinersAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal JcbAmount
        {
            get { return this.JcbAmountField; }
            set { this.JcbAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal MasterCardAmount
        {
            get { return this.MasterCardAmountField; }
            set { this.MasterCardAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string MerchantID
        {
            get { return this.MerchantIDField; }
            set { this.MerchantIDField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Payment
        {
            get { return this.PaymentField; }
            set { this.PaymentField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.DateTime SettlementDate
        {
            get { return this.SettlementDateField; }
            set { this.SettlementDateField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string SettlementRunNumber
        {
            get { return this.SettlementRunNumberField; }
            set { this.SettlementRunNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public int SlipCount
        {
            get { return this.SlipCountField; }
            set { this.SlipCountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal VisaAmount
        {
            get { return this.VisaAmountField; }
            set { this.VisaAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 17)]
        public decimal BeginBalance
        {
            get { return this.BeginBalanceField; }
            set { this.BeginBalanceField = value; }
        }
    }

    [System.Runtime.Serialization.DataContractAttribute(Name = "CreditCardBatch", Namespace = "http://schemas.datacontract.org/2004/07/Borgun.Services.Settlement")]
    public partial class CreditCardBatch : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        private System.DateTime BatchDateField;
        private string BatchNumberField;
        private string CardTypeField;
        private string CurrencyCodeField;
        private string MerchantIDField;
        private System.Nullable<System.DateTime> SettlementDateField;
        private string SettlementRunNumberField;
        private int SlipsField;
        private decimal SumField;

        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.DateTime BatchDate
        {
            get { return this.BatchDateField; }
            set { this.BatchDateField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string BatchNumber
        {
            get { return this.BatchNumberField; }
            set { this.BatchNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string CardType
        {
            get { return this.CardTypeField; }
            set { this.CardTypeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string CurrencyCode
        {
            get { return this.CurrencyCodeField; }
            set { this.CurrencyCodeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string MerchantID
        {
            get { return this.MerchantIDField; }
            set { this.MerchantIDField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Nullable<System.DateTime> SettlementDate
        {
            get { return this.SettlementDateField; }
            set { this.SettlementDateField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string SettlementRunNumber
        {
            get { return this.SettlementRunNumberField; }
            set { this.SettlementRunNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public int Slips
        {
            get { return this.SlipsField; }
            set { this.SlipsField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Sum
        {
            get { return this.SumField; }
            set { this.SumField = value; }
        }
    }

    [System.Runtime.Serialization.DataContractAttribute(Name = "CreditCardSettlementDeduction", Namespace = "http://schemas.datacontract.org/2004/07/Borgun.Services.Settlement")]
    public partial class CreditCardSettlementDeduction : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        private decimal AmountField;
        private string CodeField;
        private string CurrencyCodeField;
        private string MerchantIDField;
        private string SettlementRunNumberField;
        private string TextField;
        private string FeeField;
        private double FeeCountField;
        private decimal InterchangeFeeField;
        private string CardTypeField;

        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Amount
        {
            get { return this.AmountField; }
            set { this.AmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Code
        {
            get { return this.CodeField; }
            set { this.CodeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string CurrencyCode
        {
            get { return this.CurrencyCodeField; }
            set { this.CurrencyCodeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string MerchantID
        {
            get { return this.MerchantIDField; }
            set { this.MerchantIDField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string SettlementRunNumber
        {
            get { return this.SettlementRunNumberField; }
            set { this.SettlementRunNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Text
        {
            get { return this.TextField; }
            set { this.TextField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 6)]
        public string Fee
        {
            get { return this.FeeField; }
            set { this.FeeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 7)]
        public double FeeCount
        {
            get { return this.FeeCountField; }
            set { this.FeeCountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 8)]
        public decimal InterchangeFee
        {
            get { return this.InterchangeFeeField; }
            set { this.InterchangeFeeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 9)]
        public string CardType
        {
            get { return this.CardTypeField; }
            set { this.CardTypeField = value; }
        }
    }

    [System.Runtime.Serialization.DataContractAttribute(Name = "ResultStatus", Namespace = "http://schemas.datacontract.org/2004/07/Borgun.Services.Settlement")]
    public partial class ResultStatus : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        private string errorMessageField;
        private string referenceValueField;
        private string resultCodeField;
        private string resultTextField;
        private string resultTextIcelandicField;
        private string stackTraceField;

        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string errorMessage
        {
            get { return this.errorMessageField; }
            set { this.errorMessageField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string referenceValue
        {
            get { return this.referenceValueField; }
            set { this.referenceValueField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string resultCode
        {
            get { return this.resultCodeField; }
            set { this.resultCodeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string resultText
        {
            get { return this.resultTextField; }
            set { this.resultTextField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string resultTextIcelandic
        {
            get { return this.resultTextIcelandicField; }
            set { this.resultTextIcelandicField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string stackTrace
        {
            get { return this.stackTraceField; }
            set { this.stackTraceField = value; }
        }
    }

    [System.Runtime.Serialization.DataContractAttribute(Name = "CreditCardTransaction", Namespace = "http://schemas.datacontract.org/2004/07/Borgun.Services.Settlement")]
    public partial class CreditCardTransaction : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        private decimal AmountField;
        private string AuthorizationCodeField;
        private System.DateTime BatchDateField;
        private string BatchNumberField;
        private string CardNumberField;
        private string CardTypeField;
        private string CurrencyCodeField;
        private bool IsCorporateField;
        private string MerchantIDField;
        private decimal OriginalAmountField;
        private string OriginalCurrencyCodeField;
        private string RRNField;
        private string SettlementRunNumberField;
        private string SlipNumberField;
        private System.DateTime TransactionDateField;
        private string TransactionTypeField;
        private string TerminalIdField;
        private string CardholderCountryField;
        private string TransactionNumberField;
        private decimal ARNField;
        private string TransactionReferenceField;
        private string CardBrandField;
        private decimal CardSchemeFeeAmountField;
        private string CardSchemeFeeCurrencyField;
        private string CardSchemeFeeDescriptionField;
        private decimal CommissionAmountField;
        private string CommissionCurrencyField;
        private string CommissionDescriptionField;
        private decimal InterchangeFeeAmountField;
        private string InterchangeFeeCurrencyField;

        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Amount
        {
            get { return this.AmountField; }
            set { this.AmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string AuthorizationCode
        {
            get { return this.AuthorizationCodeField; }
            set { this.AuthorizationCodeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.DateTime BatchDate
        {
            get { return this.BatchDateField; }
            set { this.BatchDateField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string BatchNumber
        {
            get { return this.BatchNumberField; }
            set { this.BatchNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string CardNumber
        {
            get { return this.CardNumberField; }
            set { this.CardNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string CardType
        {
            get { return this.CardTypeField; }
            set { this.CardTypeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string CurrencyCode
        {
            get { return this.CurrencyCodeField; }
            set { this.CurrencyCodeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public bool IsCorporate
        {
            get { return this.IsCorporateField; }
            set { this.IsCorporateField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string MerchantID
        {
            get { return this.MerchantIDField; }
            set { this.MerchantIDField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal OriginalAmount
        {
            get { return this.OriginalAmountField; }
            set { this.OriginalAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string OriginalCurrencyCode
        {
            get { return this.OriginalCurrencyCodeField; }
            set { this.OriginalCurrencyCodeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string RRN
        {
            get { return this.RRNField; }
            set { this.RRNField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string SettlementRunNumber
        {
            get { return this.SettlementRunNumberField; }
            set { this.SettlementRunNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string SlipNumber
        {
            get { return this.SlipNumberField; }
            set { this.SlipNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.DateTime TransactionDate
        {
            get { return this.TransactionDateField; }
            set { this.TransactionDateField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute()]
        public string TransactionType
        {
            get { return this.TransactionTypeField; }
            set { this.TransactionTypeField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 16)]
        public string TerminalId
        {
            get { return this.TerminalIdField; }
            set { this.TerminalIdField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 17)]
        public string CardholderCountry
        {
            get { return this.CardholderCountryField; }
            set { this.CardholderCountryField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 18)]
        public string TransactionNumber
        {
            get { return this.TransactionNumberField; }
            set { this.TransactionNumberField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 19)]
        public decimal ARN
        {
            get { return this.ARNField; }
            set { this.ARNField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 20)]
        public string TransactionReference
        {
            get { return this.TransactionReferenceField; }
            set { this.TransactionReferenceField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 21)]
        public string CardBrand
        {
            get { return this.CardBrandField; }
            set { this.CardBrandField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 22)]
        public decimal CardSchemeFeeAmount
        {
            get { return this.CardSchemeFeeAmountField; }
            set { this.CardSchemeFeeAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 23)]
        public string CardSchemeFeeCurrency
        {
            get { return this.CardSchemeFeeCurrencyField; }
            set { this.CardSchemeFeeCurrencyField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 24)]
        public string CardSchemeFeeDescription
        {
            get { return this.CardSchemeFeeDescriptionField; }
            set { this.CardSchemeFeeDescriptionField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 25)]
        public decimal CommissionAmount
        {
            get { return this.CommissionAmountField; }
            set { this.CommissionAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 26)]
        public string CommissionCurrency
        {
            get { return this.CommissionCurrencyField; }
            set { this.CommissionCurrencyField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 27)]
        public string CommissionDescription
        {
            get { return this.CommissionDescriptionField; }
            set { this.CommissionDescriptionField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 28)]
        public decimal InterchangeFeeAmount
        {
            get { return this.InterchangeFeeAmountField; }
            set { this.InterchangeFeeAmountField = value; }
        }

        [System.Runtime.Serialization.DataMemberAttribute(Order = 29)]
        public string InterchangeFeeCurrency
        {
            get { return this.InterchangeFeeCurrencyField; }
            set { this.InterchangeFeeCurrencyField = value; }
        }
    }

    [System.Runtime.Serialization.DataContractAttribute(Name = "CreditCardSettlementInformation", Namespace = "http://schemas.datacontract.org/2004/07/Borgun.Services.Settlement")]
    public partial class CreditCardSettlementInformation : object, System.Runtime.Serialization.IExtensibleDataObject
    {

        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get { return this.extensionDataField; }
            set { this.extensionDataField = value; }
        }
    }
}

[System.ServiceModel.ServiceContractAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", ConfigurationName = "CreditCard")]
public interface ICreditCard
{
    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementsByMerchant", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementsByMerchantRespo" +
        "nse")]
    GetSettlementsByMerchantResponse GetSettlementsByMerchant(GetSettlementsByMerchantRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementsByMerchant", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementsByMerchantRespo" +
        "nse")]
    System.Threading.Tasks.Task<GetSettlementsByMerchantResponse> GetSettlementsByMerchantAsync(GetSettlementsByMerchantRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementByRunNumber", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementByRunNumberRespo" +
        "nse")]
    GetSettlementByRunNumberResponse GetSettlementByRunNumber(GetSettlementByRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementByRunNumber", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementByRunNumberRespo" +
        "nse")]
    System.Threading.Tasks.Task<GetSettlementByRunNumberResponse> GetSettlementByRunNumberAsync(GetSettlementByRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCollectedSettlementsByMerc" +
        "hant", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCollectedSettlementsByMerc" +
        "hantResponse")]
    GetCollectedSettlementsByMerchantResponse GetCollectedSettlementsByMerchant(GetCollectedSettlementsByMerchantRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCollectedSettlementsByMerc" +
        "hant", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCollectedSettlementsByMerc" +
        "hantResponse")]
    System.Threading.Tasks.Task<GetCollectedSettlementsByMerchantResponse> GetCollectedSettlementsByMerchantAsync(GetCollectedSettlementsByMerchantRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetBatchesByMerchant", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetBatchesByMerchantResponse")]
    GetBatchesByMerchantResponse GetBatchesByMerchant(GetBatchesByMerchantRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetBatchesByMerchant", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetBatchesByMerchantResponse")]
    System.Threading.Tasks.Task<GetBatchesByMerchantResponse> GetBatchesByMerchantAsync(GetBatchesByMerchantRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetBatchesByRunNumber", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetBatchesByRunNumberResponse" +
        "")]
    GetBatchesByRunNumberResponse GetBatchesByRunNumber(GetBatchesByRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetBatchesByRunNumber", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetBatchesByRunNumberResponse" +
        "")]
    System.Threading.Tasks.Task<GetBatchesByRunNumberResponse> GetBatchesByRunNumberAsync(GetBatchesByRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetTransactionsByBatch", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetTransactionsByBatchRespons" +
        "e")]
    GetTransactionsByBatchResponse GetTransactionsByBatch(GetTransactionsByBatchRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetTransactionsByBatch", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetTransactionsByBatchRespons" +
        "e")]
    System.Threading.Tasks.Task<GetTransactionsByBatchResponse> GetTransactionsByBatchAsync(GetTransactionsByBatchRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetTransactionsByRunNumber", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetTransactionsByRunNumberRes" +
        "ponse")]
    GetTransactionsByRunNumberResponse GetTransactionsByRunNumber(GetTransactionsByRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetTransactionsByRunNumber", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetTransactionsByRunNumberRes" +
        "ponse")]
    System.Threading.Tasks.Task<GetTransactionsByRunNumberResponse> GetTransactionsByRunNumberAsync(GetTransactionsByRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementDeductionItems", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementDeductionItemsRe" +
        "sponse")]
    GetSettlementDeductionItemsResponse GetSettlementDeductionItems(GetSettlementDeductionItemsRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementDeductionItems", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementDeductionItemsRe" +
        "sponse")]
    System.Threading.Tasks.Task<GetSettlementDeductionItemsResponse> GetSettlementDeductionItemsAsync(GetSettlementDeductionItemsRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCardTypeBatchesByMerchant", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCardTypeBatchesByMerchantR" +
        "esponse")]
    GetCardTypeBatchesByMerchantResponse GetCardTypeBatchesByMerchant(GetCardTypeBatchesByMerchantRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCardTypeBatchesByMerchant", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCardTypeBatchesByMerchantR" +
        "esponse")]
    System.Threading.Tasks.Task<GetCardTypeBatchesByMerchantResponse> GetCardTypeBatchesByMerchantAsync(GetCardTypeBatchesByMerchantRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCardTypeBatchesByRunNumber" +
        "", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCardTypeBatchesByRunNumber" +
        "Response")]
    GetCardTypeBatchesByRunNumberResponse GetCardTypeBatchesByRunNumber(GetCardTypeBatchesByRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCardTypeBatchesByRunNumber" +
        "", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetCardTypeBatchesByRunNumber" +
        "Response")]
    System.Threading.Tasks.Task<GetCardTypeBatchesByRunNumberResponse> GetCardTypeBatchesByRunNumberAsync(GetCardTypeBatchesByRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementRunNumbers", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementRunNumbersRespon" +
        "se")]
    GetSettlementRunNumbersResponse GetSettlementRunNumbers(GetSettlementRunNumbersRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementRunNumbers", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetSettlementRunNumbersRespon" +
        "se")]
    System.Threading.Tasks.Task<GetSettlementRunNumbersResponse> GetSettlementRunNumbersAsync(GetSettlementRunNumbersRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetLatestSettlementRunNumber", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetLatestSettlementRunNumberR" +
        "esponse")]
    GetLatestSettlementRunNumberResponse GetLatestSettlementRunNumber(GetLatestSettlementRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetLatestSettlementRunNumber", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/GetLatestSettlementRunNumberR" +
        "esponse")]
    System.Threading.Tasks.Task<GetLatestSettlementRunNumberResponse> GetLatestSettlementRunNumberAsync(GetLatestSettlementRunNumberRequest request);

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/Ping", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/PingResponse")]
    bool Ping();

    [System.ServiceModel.OperationContractAttribute(Action = "http://Borgun.Service.Settlement/2016/02/CreditCard/Ping", ReplyAction = "http://Borgun.Service.Settlement/2016/02/CreditCard/PingResponse")]
    System.Threading.Tasks.Task<bool> PingAsync();
}

[System.ServiceModel.MessageContractAttribute(WrapperName = "GetSettlementsByMerchant", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetSettlementsByMerchantRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public System.DateTime dateFrom;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 2)]
    public System.DateTime dateTo;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 3)]
    public bool includeDeductionItems;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 4)]
    public bool includeBatches;

    public GetSettlementsByMerchantRequest()
    {
    }

    public GetSettlementsByMerchantRequest(string merchantID, System.DateTime dateFrom, System.DateTime dateTo, bool includeDeductionItems, bool includeBatches)
    {
        this.merchantID = merchantID;
        this.dateFrom = dateFrom;
        this.dateTo = dateTo;
        this.includeDeductionItems = includeDeductionItems;
        this.includeBatches = includeBatches;
    }
}

[System.ServiceModel.MessageContractAttribute(WrapperName = "GetSettlementsByMerchantResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetSettlementsByMerchantResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardSettlement[] GetSettlementsByMerchantResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetSettlementsByMerchantResponse()
    {
    }

    public GetSettlementsByMerchantResponse(CreditCardSettlement[] GetSettlementsByMerchantResult, ResultStatus status)
    {
        this.GetSettlementsByMerchantResult = GetSettlementsByMerchantResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetSettlementByRunNumber", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetSettlementByRunNumberRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public string settlementRunNumber;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 2)]
    public bool includeDeductionItems;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 3)]
    public bool includeBatches;

    public GetSettlementByRunNumberRequest()
    {
    }

    public GetSettlementByRunNumberRequest(string merchantID, string settlementRunNumber, bool includeDeductionItems, bool includeBatches)
    {
        this.merchantID = merchantID;
        this.settlementRunNumber = settlementRunNumber;
        this.includeDeductionItems = includeDeductionItems;
        this.includeBatches = includeBatches;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetSettlementByRunNumberResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetSettlementByRunNumberResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardSettlement GetSettlementByRunNumberResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetSettlementByRunNumberResponse()
    {
    }

    public GetSettlementByRunNumberResponse(CreditCardSettlement GetSettlementByRunNumberResult, ResultStatus status)
    {
        this.GetSettlementByRunNumberResult = GetSettlementByRunNumberResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetCollectedSettlementsByMerchant", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetCollectedSettlementsByMerchantRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    public GetCollectedSettlementsByMerchantRequest()
    {
    }

    public GetCollectedSettlementsByMerchantRequest(string merchantID)
    {
        this.merchantID = merchantID;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetCollectedSettlementsByMerchantResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetCollectedSettlementsByMerchantResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardSettlement[] GetCollectedSettlementsByMerchantResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetCollectedSettlementsByMerchantResponse()
    {
    }

    public GetCollectedSettlementsByMerchantResponse(CreditCardSettlement[] GetCollectedSettlementsByMerchantResult, ResultStatus status)
    {
        this.GetCollectedSettlementsByMerchantResult = GetCollectedSettlementsByMerchantResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetBatchesByMerchant", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetBatchesByMerchantRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public System.DateTime dateFrom;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 2)]
    public System.DateTime dateTo;

    public GetBatchesByMerchantRequest()
    {
    }

    public GetBatchesByMerchantRequest(string merchantID, System.DateTime dateFrom, System.DateTime dateTo)
    {
        this.merchantID = merchantID;
        this.dateFrom = dateFrom;
        this.dateTo = dateTo;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetBatchesByMerchantResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetBatchesByMerchantResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardBatch[] GetBatchesByMerchantResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetBatchesByMerchantResponse()
    {
    }

    public GetBatchesByMerchantResponse(CreditCardBatch[] GetBatchesByMerchantResult, ResultStatus status)
    {
        this.GetBatchesByMerchantResult = GetBatchesByMerchantResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetBatchesByRunNumber", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetBatchesByRunNumberRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public string settlementRunNumber;

    public GetBatchesByRunNumberRequest()
    {
    }

    public GetBatchesByRunNumberRequest(string merchantID, string settlementRunNumber)
    {
        this.merchantID = merchantID;
        this.settlementRunNumber = settlementRunNumber;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetBatchesByRunNumberResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetBatchesByRunNumberResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardBatch[] GetBatchesByRunNumberResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetBatchesByRunNumberResponse()
    {
    }

    public GetBatchesByRunNumberResponse(CreditCardBatch[] GetBatchesByRunNumberResult, ResultStatus status)
    {
        this.GetBatchesByRunNumberResult = GetBatchesByRunNumberResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetTransactionsByBatch", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetTransactionsByBatchRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public string batchNumber;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 2)]
    public System.DateTime batchDate;

    public GetTransactionsByBatchRequest()
    {
    }

    public GetTransactionsByBatchRequest(string merchantID, string batchNumber, System.DateTime batchDate)
    {
        this.merchantID = merchantID;
        this.batchNumber = batchNumber;
        this.batchDate = batchDate;
    }
}

[System.ServiceModel.MessageContractAttribute(WrapperName = "GetTransactionsByBatchResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetTransactionsByBatchResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardTransaction[] GetTransactionsByBatchResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetTransactionsByBatchResponse()
    {
    }

    public GetTransactionsByBatchResponse(CreditCardTransaction[] GetTransactionsByBatchResult, ResultStatus status)
    {
        this.GetTransactionsByBatchResult = GetTransactionsByBatchResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetTransactionsByRunNumber", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetTransactionsByRunNumberRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public string settlementRunNumber;

    public GetTransactionsByRunNumberRequest()
    {
    }

    public GetTransactionsByRunNumberRequest(string merchantID, string settlementRunNumber)
    {
        this.merchantID = merchantID;
        this.settlementRunNumber = settlementRunNumber;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetTransactionsByRunNumberResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetTransactionsByRunNumberResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardTransaction[] GetTransactionsByRunNumberResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetTransactionsByRunNumberResponse()
    {
    }

    public GetTransactionsByRunNumberResponse(CreditCardTransaction[] GetTransactionsByRunNumberResult, ResultStatus status)
    {
        this.GetTransactionsByRunNumberResult = GetTransactionsByRunNumberResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetSettlementDeductionItems", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetSettlementDeductionItemsRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public string settlementRunNumber;

    public GetSettlementDeductionItemsRequest()
    {
    }

    public GetSettlementDeductionItemsRequest(string merchantID, string settlementRunNumber)
    {
        this.merchantID = merchantID;
        this.settlementRunNumber = settlementRunNumber;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetSettlementDeductionItemsResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetSettlementDeductionItemsResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardSettlementDeduction[] GetSettlementDeductionItemsResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetSettlementDeductionItemsResponse()
    {
    }

    public GetSettlementDeductionItemsResponse(CreditCardSettlementDeduction[] GetSettlementDeductionItemsResult, ResultStatus status)
    {
        this.GetSettlementDeductionItemsResult = GetSettlementDeductionItemsResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetCardTypeBatchesByMerchant", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetCardTypeBatchesByMerchantRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public System.DateTime dateFrom;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 2)]
    public System.DateTime dateTo;

    public GetCardTypeBatchesByMerchantRequest()
    {
    }

    public GetCardTypeBatchesByMerchantRequest(string merchantID, System.DateTime dateFrom, System.DateTime dateTo)
    {
        this.merchantID = merchantID;
        this.dateFrom = dateFrom;
        this.dateTo = dateTo;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetCardTypeBatchesByMerchantResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetCardTypeBatchesByMerchantResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardBatch[] GetCardTypeBatchesByMerchantResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetCardTypeBatchesByMerchantResponse()
    {
    }

    public GetCardTypeBatchesByMerchantResponse(CreditCardBatch[] GetCardTypeBatchesByMerchantResult, ResultStatus status)
    {
        this.GetCardTypeBatchesByMerchantResult = GetCardTypeBatchesByMerchantResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetCardTypeBatchesByRunNumber", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetCardTypeBatchesByRunNumberRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public string merchantID;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public string settlementRunNumber;

    public GetCardTypeBatchesByRunNumberRequest()
    {
    }

    public GetCardTypeBatchesByRunNumberRequest(string merchantID, string settlementRunNumber)
    {
        this.merchantID = merchantID;
        this.settlementRunNumber = settlementRunNumber;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetCardTypeBatchesByRunNumberResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetCardTypeBatchesByRunNumberResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardBatch[] GetCardTypeBatchesByRunNumberResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetCardTypeBatchesByRunNumberResponse()
    {
    }

    public GetCardTypeBatchesByRunNumberResponse(CreditCardBatch[] GetCardTypeBatchesByRunNumberResult, ResultStatus status)
    {
        this.GetCardTypeBatchesByRunNumberResult = GetCardTypeBatchesByRunNumberResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetSettlementRunNumbers", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetSettlementRunNumbersRequest
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public System.DateTime dateFrom;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public System.DateTime dateTo;

    public GetSettlementRunNumbersRequest()
    {
    }

    public GetSettlementRunNumbersRequest(System.DateTime dateFrom, System.DateTime dateTo)
    {
        this.dateFrom = dateFrom;
        this.dateTo = dateTo;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetSettlementRunNumbersResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetSettlementRunNumbersResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public CreditCardSettlementInformation[] GetSettlementRunNumbersResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetSettlementRunNumbersResponse()
    {
    }

    public GetSettlementRunNumbersResponse(CreditCardSettlementInformation[] GetSettlementRunNumbersResult, ResultStatus status)
    {
        this.GetSettlementRunNumbersResult = GetSettlementRunNumbersResult;
        this.status = status;
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetLatestSettlementRunNumber", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetLatestSettlementRunNumberRequest
{

    public GetLatestSettlementRunNumberRequest()
    {
    }
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ServiceModel.MessageContractAttribute(WrapperName = "GetLatestSettlementRunNumberResponse", WrapperNamespace = "http://Borgun.Service.Settlement/2016/02/", IsWrapped = true)]
public partial class GetLatestSettlementRunNumberResponse
{

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 0)]
    public int GetLatestSettlementRunNumberResult;

    [System.ServiceModel.MessageBodyMemberAttribute(Namespace = "http://Borgun.Service.Settlement/2016/02/", Order = 1)]
    public ResultStatus status;

    public GetLatestSettlementRunNumberResponse()
    {
    }

    public GetLatestSettlementRunNumberResponse(int GetLatestSettlementRunNumberResult, ResultStatus status)
    {
        this.GetLatestSettlementRunNumberResult = GetLatestSettlementRunNumberResult;
        this.status = status;
    }
}
#if !UNICORE
public interface CreditCardChannel : ICreditCard, System.ServiceModel.IClientChannel
{
}

public partial class CreditCardClient : System.ServiceModel.ClientBase<ICreditCard>, ICreditCard
{

    public CreditCardClient()
    {
    }

    public CreditCardClient(string endpointConfigurationName) :
            base(endpointConfigurationName)
    {
    }

    public CreditCardClient(string endpointConfigurationName, string remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
    {
    }

    public CreditCardClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) :
            base(endpointConfigurationName, remoteAddress)
    {
    }

    public CreditCardClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) :
            base(binding, remoteAddress)
    {
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetSettlementsByMerchantResponse ICreditCard.GetSettlementsByMerchant(GetSettlementsByMerchantRequest request)
    {
        return base.Channel.GetSettlementsByMerchant(request);
    }

    public CreditCardSettlement[] GetSettlementsByMerchant(string merchantID, System.DateTime dateFrom, System.DateTime dateTo, bool includeDeductionItems, bool includeBatches, out ResultStatus status)
    {
        GetSettlementsByMerchantRequest inValue = new GetSettlementsByMerchantRequest();
        inValue.merchantID = merchantID;
        inValue.dateFrom = dateFrom;
        inValue.dateTo = dateTo;
        inValue.includeDeductionItems = includeDeductionItems;
        inValue.includeBatches = includeBatches;
        GetSettlementsByMerchantResponse retVal = ((ICreditCard)(this)).GetSettlementsByMerchant(inValue);
        status = retVal.status;
        return retVal.GetSettlementsByMerchantResult;
    }

    public System.Threading.Tasks.Task<GetSettlementsByMerchantResponse> GetSettlementsByMerchantAsync(GetSettlementsByMerchantRequest request)
    {
        return base.Channel.GetSettlementsByMerchantAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetSettlementByRunNumberResponse ICreditCard.GetSettlementByRunNumber(GetSettlementByRunNumberRequest request)
    {
        return base.Channel.GetSettlementByRunNumber(request);
    }

    public CreditCardSettlement GetSettlementByRunNumber(string merchantID, string settlementRunNumber, bool includeDeductionItems, bool includeBatches, out ResultStatus status)
    {
        GetSettlementByRunNumberRequest inValue = new GetSettlementByRunNumberRequest();
        inValue.merchantID = merchantID;
        inValue.settlementRunNumber = settlementRunNumber;
        inValue.includeDeductionItems = includeDeductionItems;
        inValue.includeBatches = includeBatches;
        GetSettlementByRunNumberResponse retVal = ((ICreditCard)(this)).GetSettlementByRunNumber(inValue);
        status = retVal.status;
        return retVal.GetSettlementByRunNumberResult;
    }

    public System.Threading.Tasks.Task<GetSettlementByRunNumberResponse> GetSettlementByRunNumberAsync(GetSettlementByRunNumberRequest request)
    {
        return base.Channel.GetSettlementByRunNumberAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetCollectedSettlementsByMerchantResponse ICreditCard.GetCollectedSettlementsByMerchant(GetCollectedSettlementsByMerchantRequest request)
    {
        return base.Channel.GetCollectedSettlementsByMerchant(request);
    }

    public CreditCardSettlement[] GetCollectedSettlementsByMerchant(string merchantID, out ResultStatus status)
    {
        GetCollectedSettlementsByMerchantRequest inValue = new GetCollectedSettlementsByMerchantRequest();
        inValue.merchantID = merchantID;
        GetCollectedSettlementsByMerchantResponse retVal = ((ICreditCard)(this)).GetCollectedSettlementsByMerchant(inValue);
        status = retVal.status;
        return retVal.GetCollectedSettlementsByMerchantResult;
    }

    public System.Threading.Tasks.Task<GetCollectedSettlementsByMerchantResponse> GetCollectedSettlementsByMerchantAsync(GetCollectedSettlementsByMerchantRequest request)
    {
        return base.Channel.GetCollectedSettlementsByMerchantAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetBatchesByMerchantResponse ICreditCard.GetBatchesByMerchant(GetBatchesByMerchantRequest request)
    {
        return base.Channel.GetBatchesByMerchant(request);
    }

    public CreditCardBatch[] GetBatchesByMerchant(string merchantID, System.DateTime dateFrom, System.DateTime dateTo, out ResultStatus status)
    {
        GetBatchesByMerchantRequest inValue = new GetBatchesByMerchantRequest();
        inValue.merchantID = merchantID;
        inValue.dateFrom = dateFrom;
        inValue.dateTo = dateTo;
        GetBatchesByMerchantResponse retVal = ((ICreditCard)(this)).GetBatchesByMerchant(inValue);
        status = retVal.status;
        return retVal.GetBatchesByMerchantResult;
    }

    public System.Threading.Tasks.Task<GetBatchesByMerchantResponse> GetBatchesByMerchantAsync(GetBatchesByMerchantRequest request)
    {
        return base.Channel.GetBatchesByMerchantAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetBatchesByRunNumberResponse ICreditCard.GetBatchesByRunNumber(GetBatchesByRunNumberRequest request)
    {
        return base.Channel.GetBatchesByRunNumber(request);
    }

    public CreditCardBatch[] GetBatchesByRunNumber(string merchantID, string settlementRunNumber, out ResultStatus status)
    {
        GetBatchesByRunNumberRequest inValue = new GetBatchesByRunNumberRequest();
        inValue.merchantID = merchantID;
        inValue.settlementRunNumber = settlementRunNumber;
        GetBatchesByRunNumberResponse retVal = ((ICreditCard)(this)).GetBatchesByRunNumber(inValue);
        status = retVal.status;
        return retVal.GetBatchesByRunNumberResult;
    }

    public System.Threading.Tasks.Task<GetBatchesByRunNumberResponse> GetBatchesByRunNumberAsync(GetBatchesByRunNumberRequest request)
    {
        return base.Channel.GetBatchesByRunNumberAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetTransactionsByBatchResponse ICreditCard.GetTransactionsByBatch(GetTransactionsByBatchRequest request)
    {
        return base.Channel.GetTransactionsByBatch(request);
    }

    public CreditCardTransaction[] GetTransactionsByBatch(string merchantID, string batchNumber, System.DateTime batchDate, out ResultStatus status)
    {
        GetTransactionsByBatchRequest inValue = new GetTransactionsByBatchRequest();
        inValue.merchantID = merchantID;
        inValue.batchNumber = batchNumber;
        inValue.batchDate = batchDate;
        GetTransactionsByBatchResponse retVal = ((ICreditCard)(this)).GetTransactionsByBatch(inValue);
        status = retVal.status;
        return retVal.GetTransactionsByBatchResult;
    }

    public System.Threading.Tasks.Task<GetTransactionsByBatchResponse> GetTransactionsByBatchAsync(GetTransactionsByBatchRequest request)
    {
        return base.Channel.GetTransactionsByBatchAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetTransactionsByRunNumberResponse ICreditCard.GetTransactionsByRunNumber(GetTransactionsByRunNumberRequest request)
    {
        return base.Channel.GetTransactionsByRunNumber(request);
    }

    public CreditCardTransaction[] GetTransactionsByRunNumber(string merchantID, string settlementRunNumber, out ResultStatus status)
    {
        GetTransactionsByRunNumberRequest inValue = new GetTransactionsByRunNumberRequest();
        inValue.merchantID = merchantID;
        inValue.settlementRunNumber = settlementRunNumber;
        GetTransactionsByRunNumberResponse retVal = ((ICreditCard)(this)).GetTransactionsByRunNumber(inValue);
        status = retVal.status;
        return retVal.GetTransactionsByRunNumberResult;
    }

    public System.Threading.Tasks.Task<GetTransactionsByRunNumberResponse> GetTransactionsByRunNumberAsync(GetTransactionsByRunNumberRequest request)
    {
        return base.Channel.GetTransactionsByRunNumberAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetSettlementDeductionItemsResponse ICreditCard.GetSettlementDeductionItems(GetSettlementDeductionItemsRequest request)
    {
        return base.Channel.GetSettlementDeductionItems(request);
    }

    public CreditCardSettlementDeduction[] GetSettlementDeductionItems(string merchantID, string settlementRunNumber, out ResultStatus status)
    {
        GetSettlementDeductionItemsRequest inValue = new GetSettlementDeductionItemsRequest();
        inValue.merchantID = merchantID;
        inValue.settlementRunNumber = settlementRunNumber;
        GetSettlementDeductionItemsResponse retVal = ((ICreditCard)(this)).GetSettlementDeductionItems(inValue);
        status = retVal.status;
        return retVal.GetSettlementDeductionItemsResult;
    }

    public System.Threading.Tasks.Task<GetSettlementDeductionItemsResponse> GetSettlementDeductionItemsAsync(GetSettlementDeductionItemsRequest request)
    {
        return base.Channel.GetSettlementDeductionItemsAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetCardTypeBatchesByMerchantResponse ICreditCard.GetCardTypeBatchesByMerchant(GetCardTypeBatchesByMerchantRequest request)
    {
        return base.Channel.GetCardTypeBatchesByMerchant(request);
    }

    public CreditCardBatch[] GetCardTypeBatchesByMerchant(string merchantID, System.DateTime dateFrom, System.DateTime dateTo, out ResultStatus status)
    {
        GetCardTypeBatchesByMerchantRequest inValue = new GetCardTypeBatchesByMerchantRequest();
        inValue.merchantID = merchantID;
        inValue.dateFrom = dateFrom;
        inValue.dateTo = dateTo;
        GetCardTypeBatchesByMerchantResponse retVal = ((ICreditCard)(this)).GetCardTypeBatchesByMerchant(inValue);
        status = retVal.status;
        return retVal.GetCardTypeBatchesByMerchantResult;
    }

    public System.Threading.Tasks.Task<GetCardTypeBatchesByMerchantResponse> GetCardTypeBatchesByMerchantAsync(GetCardTypeBatchesByMerchantRequest request)
    {
        return base.Channel.GetCardTypeBatchesByMerchantAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetCardTypeBatchesByRunNumberResponse ICreditCard.GetCardTypeBatchesByRunNumber(GetCardTypeBatchesByRunNumberRequest request)
    {
        return base.Channel.GetCardTypeBatchesByRunNumber(request);
    }

    public CreditCardBatch[] GetCardTypeBatchesByRunNumber(string merchantID, string settlementRunNumber, out ResultStatus status)
    {
        GetCardTypeBatchesByRunNumberRequest inValue = new GetCardTypeBatchesByRunNumberRequest();
        inValue.merchantID = merchantID;
        inValue.settlementRunNumber = settlementRunNumber;
        GetCardTypeBatchesByRunNumberResponse retVal = ((ICreditCard)(this)).GetCardTypeBatchesByRunNumber(inValue);
        status = retVal.status;
        return retVal.GetCardTypeBatchesByRunNumberResult;
    }

    public System.Threading.Tasks.Task<GetCardTypeBatchesByRunNumberResponse> GetCardTypeBatchesByRunNumberAsync(GetCardTypeBatchesByRunNumberRequest request)
    {
        return base.Channel.GetCardTypeBatchesByRunNumberAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetSettlementRunNumbersResponse ICreditCard.GetSettlementRunNumbers(GetSettlementRunNumbersRequest request)
    {
        return base.Channel.GetSettlementRunNumbers(request);
    }

    public CreditCardSettlementInformation[] GetSettlementRunNumbers(System.DateTime dateFrom, System.DateTime dateTo, out ResultStatus status)
    {
        GetSettlementRunNumbersRequest inValue = new GetSettlementRunNumbersRequest();
        inValue.dateFrom = dateFrom;
        inValue.dateTo = dateTo;
        GetSettlementRunNumbersResponse retVal = ((ICreditCard)(this)).GetSettlementRunNumbers(inValue);
        status = retVal.status;
        return retVal.GetSettlementRunNumbersResult;
    }

    public System.Threading.Tasks.Task<GetSettlementRunNumbersResponse> GetSettlementRunNumbersAsync(GetSettlementRunNumbersRequest request)
    {
        return base.Channel.GetSettlementRunNumbersAsync(request);
    }

    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    GetLatestSettlementRunNumberResponse ICreditCard.GetLatestSettlementRunNumber(GetLatestSettlementRunNumberRequest request)
    {
        return base.Channel.GetLatestSettlementRunNumber(request);
    }

    public int GetLatestSettlementRunNumber(out ResultStatus status)
    {
        GetLatestSettlementRunNumberRequest inValue = new GetLatestSettlementRunNumberRequest();
        GetLatestSettlementRunNumberResponse retVal = ((ICreditCard)(this)).GetLatestSettlementRunNumber(inValue);
        status = retVal.status;
        return retVal.GetLatestSettlementRunNumberResult;
    }

    public System.Threading.Tasks.Task<GetLatestSettlementRunNumberResponse> GetLatestSettlementRunNumberAsync(GetLatestSettlementRunNumberRequest request)
    {
        return base.Channel.GetLatestSettlementRunNumberAsync(request);
    }

    public bool Ping()
    {
        return base.Channel.Ping();
    }

    public System.Threading.Tasks.Task<bool> PingAsync()
    {
        return base.Channel.PingAsync();
    }
}

#endif