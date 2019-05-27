using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.Creditor.Payments.Denmark
{
    public class BankDataFormatFields : DanishFormatFieldBase
    {
        public string Index;
        public string NameOfReceiver;
        public string AddressOfReceiver;
        public string AddressOfReceiver2;
        public string VendorReference;
        public List<string> ReservetForNemKonto = new List<string>();
        public List<string> ReservetForXML = new List<string>();
        public string Reserved;
        public string TransferCoin;
        public string ReportPurposeCode;
        public List<string> OtherTransfers = new List<string>();
        public string IndfYear;
        public List<string> DescriptionOfPayment = new List<string>();
        public int TransferTypeForeign;
        public string GiroReg;
        public string ToAccountGiro;
        public string ToAccountCreditor;
        public int TotalPayments;
        public long TotalAmount;
        public long AmountLong;
    }
}
