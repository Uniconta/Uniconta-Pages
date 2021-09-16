using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.BankStatement
{
    public class PollReportSource
    {
        public DateTime Date, FromDate;
        public BankStatementClient BankAcc;
        public List<TextAmount> Source2 { get; set; }
        public List<TextAmount> Source3 { get; set; }
        public double BankTotal { get; set; }
        public double PostedTotal { get; set; }

        public string Heading1 { get { return string.Concat(Localization.lookup("BankReconciliation"), " ",
                FromDate.ToShortDateString(), " - ", Date.ToShortDateString()," ", BankAcc._Account, " ", BankAcc._Name); } }
        public string Heading2 { get { return Localization.lookup("BankStatement") + " - " + Localization.lookup("Unreconciled"); } }
        public string Heading3 { get { return Localization.lookup("AccountsTransaction") + " - " + Localization.lookup("Unreconciled"); } }

        public string Bank { get { return Localization.lookup("AccountTypeBank"); } }
        public string Posted { get { return string.Format(Localization.lookup("PostedOBJ"), ""); } }
        public string BalancePer { get { return string.Format(Localization.lookup("BalancePer"), Date.ToShortDateString()); } }
        public string NonreconciledItems { get { return Localization.lookup("Unreconciled"); } }
        public string Reconciliation { get { return Localization.lookup("Reconciliation"); } }
        public string Difference { get { return Uniconta.ClientTools.Localization.lookup("Difference"); } }
        public string NewBalance { get { return Uniconta.ClientTools.Localization.lookup("NewBalance"); } }
        public string VoucherLabel { get { return Localization.lookup("Voucher"); } }
        public string DateLabel { get { return Localization.lookup("Date"); } }
        public string TextLabel { get { return Localization.lookup("Text"); } }
        public string AmountLabel { get { return Localization.lookup("Amount"); } }
    }

    public class TextAmount
    {
        public DateTime Date { get; set; }
        public string Text { get; set; }
        public long AmountCent;
        public double Amount { get { return AmountCent / 100d; } }
        public int Voucher { get; set; }
    }
}
