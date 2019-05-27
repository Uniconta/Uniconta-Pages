using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public class DanishFormatFieldBase
    {
        public string TransTypeCommand;
        public string SwiftAddress;
        public string FromAccountNumber;
        public string ToAccountNumber; //Bruges også til DabnkData i stedet for CardTypeCode
        public string ReceiverIBAN; 
        public double Amount;
        public Currencies Currency;
        public DateTime TransferDate;
        public string BankCode;
        public List<string> ReceiverBankInfo; //Include BankAddress1 and 2, BankCountry, Bankname and BankAccountNumber
        public string PaymentId; // Bruges til indbetalingskort kode. 04, 71, 73, 75
        public List<String> ReceiverAccountInfo; // Include NameOfReceiver, 
        public int FromAccountType;
        public int TransferType;
        public string ToAccountRegNr;
        public string ClearingTypeChannel;
        public string CollectorPostNumber;
        public string ZipCodeOfReceiver;
        public string CityOfReceiver;
        public List<string> SenderInformation = new List<string>();
        public string OwnVoucherNumber;
        public string HomeCountry;
        public List<string> FromAccountForgeinTransfer = new List<string>();
        public List<string> PriceType = new List<string>();
        public List<string> ForwardNumber = new List<string>();
        public List<string> AdviceText = new List<string>();
        public string ReferenceToPrimaryDoc;
        public string EndToEndReference;
        public string BeneficiaryName;
        public string FormType;
        public string Reference;
        public string DebtorsIdentificationOfThePayment;   
        public string CountryCode;
        public List<string> Messages;
        public string ReceiversIdentifictionOfSender;
        public string AlternativSender;
        public string UniquePaymRef;
        public string SenderName;
        public string SenderAddress1;
        public string SenderAddress2;
        public string SenderAddress3;
        public string ReceiverAccountStatement;
        public List<string> NotUsed = new List<string>();
        public List<string> NotUsed02 = new List<string>();
        public List<string> NotUsed03 = new List<string>();
        public List<string> NotUsed04 = new List<string>();
        public List<string> NotUsed05 = new List<string>();
        public List<string> NotUsed06 = new List<string>();
        public List<string> NotUsed07 = new List<string>();
        public string Blanks;
        public string Blanks2;
        public string Blanks3;
    }
}
