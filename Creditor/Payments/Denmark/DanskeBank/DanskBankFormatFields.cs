using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public class DanskBankFormatFields : DanishFormatFieldBase
    {
        public string TextToSender;
        public string LetterToSend;
        public string TextToBeneficiary;
        public string DebtorId;
        public string Orderingofelectronicaladvice;
        public string CurencyOfEquivalentAmount;
        public string ForwardRate;
        public string AgreeddRate;
        public string ChequeToBeCrossed;
        public string ChequeToBeSend;
        public string CostsCheque;
        public string ExchangeRateType;
        public string Branch;
        public int CostAccountTransfer;
        public string NameOfReceiver;
        public string AddressOfReceiver;
        public string AddressOfReceiver2;
    }
}
