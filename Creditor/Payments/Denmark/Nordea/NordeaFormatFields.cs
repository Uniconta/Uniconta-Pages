using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public class NordeaFormatFields : DanishFormatFieldBase
    {
        public string TextCode;
        public string BriefFreeAdvice;
        public string StretchedAdvis;
        public string ExchangeRateeReference;
        public string CostCode;
        public string ExchangeRate;
        public string RecordType;
        public string NameOfReceiver;
        public string AddressOfReceiver;
        public string AddressOfReceiver2;
        public string AddressOfReceiver3;
        public string TextToBeneficiary;
        public List<string> LongAdviceText = new List<string>();
        public string ExpenseCode;
        public string PromptAdvice;
    }
}
