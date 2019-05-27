using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public class SDCFormatFields : DanishFormatFieldBase
    {
        public string AmountSTR;
        public string Receipt;
        public List<string> OtherSender = new List<string>();
        public string AdviceTextLines;
        public string BeneficiaryAdviceText;
        public string TransferTypeStr;
        public Currencies TransferCurrency;
        public string NameOfReceiver;
        public string AddressOfReceiver;
        public string AddressOfReceiver2;
        public string ExchRateType;
        public string ExchRateTermContract;
        public string ExchRateTerm;
        public string ChargeAccountSeparate;
        public string ChargeAccount;
        public string ChargeType;
        public List<string> EmptyFields = new List<string>();
        public List<string> DescriptionOfPayment = new List<string>();
    }
}
