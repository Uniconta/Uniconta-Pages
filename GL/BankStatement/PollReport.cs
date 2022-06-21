using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class PollReport : XtraReport
    {
        public PollReport()
        {
            InitializeComponent();
            SetupBinding();
        }

        private void SetupBinding()
        {
            SetupReportHeader();
            SetupBankDetailReport();
            SetupPostedDetailReport();
        }

        private void SetupPostedDetailReport()
        {
            DetailReportBank.DataMember = "Source2";
            xrHeading2.ExpressionBindings.Add(Expression("[Heading2]"));
            xrDetailReportHeaderBankDate.ExpressionBindings.Add(Expression("[DateLabel]"));
            xrDetailReportHeaderBankText.ExpressionBindings.Add(Expression("[TextLabel]"));
            xrDetailReportHeaderBankTotal.ExpressionBindings.Add(Expression("[AmountLabel]"));
            xrDetailReportBankDateValue.ExpressionBindings.Add(Expression("[Date]"));
            xrDetailReportBankTextValue.ExpressionBindings.Add(Expression("[Text]"));
            xrDetailReportBankTotalValue.ExpressionBindings.Add(Expression("[Amount]"));
            xrReportFooterBankSum.Summary = Summary();
            xrReportFooterBankSum.ExpressionBindings.Add(Expression("sumSum(Amount)"));
        }

        private void SetupBankDetailReport()
        {
            DetailReportPosted.DataMember = "Source3";
            xrHeading3.ExpressionBindings.Add(Expression("[Heading3]"));
            xrDetailReportHeaderPostedDate.ExpressionBindings.Add(Expression("[DateLabel]"));
            xrDetailReportHeaderPostedText.ExpressionBindings.Add(Expression("[TextLabel]"));
            xrDetailReportHeaderPostedVoucher.ExpressionBindings.Add(Expression("[VoucherLabel]"));
            xrDetailReportHeaderPostedTotal.ExpressionBindings.Add(Expression("[AmountLabel]"));
            xrDetailReportPostedDateValue.ExpressionBindings.Add(Expression("[Date]"));
            xrDetailReportPostedTextValue.ExpressionBindings.Add(Expression("[Text]"));
            xrDetailReportPostedVoucherValue.ExpressionBindings.Add(Expression("[Voucher]"));
            xrDetailReportPostedTotalValue.ExpressionBindings.Add(Expression("Amount"));
            xrReportFooterPostedSum.Summary = Summary();
            xrReportFooterPostedSum.ExpressionBindings.Add(Expression("sumSum(Amount)"));
        }

        private void SetupReportHeader()
        {
            xrHeading1.ExpressionBindings.Add(Expression("[Heading1]"));
            xrBank.ExpressionBindings.Add(Expression("[Bank]"));
            xrPosted.ExpressionBindings.Add(Expression("[Posted]"));
            xrBalancePr.ExpressionBindings.Add(Expression("[BalancePer]"));
            xrNonreconciledItems.ExpressionBindings.Add(Expression("[NonreconciledItems]"));
            xrVoidItems.ExpressionBindings.Add(Expression("[VoidTransactions]"));
            xrReconciliation.ExpressionBindings.Add(Expression("[Reconciliation]"));
            xrBankTotal.ExpressionBindings.Add(Expression("[BankTotal]"));
            xrPostedTotal.ExpressionBindings.Add(Expression("[PostedTotal]"));
            xrBankTotalNonReconciled.ExpressionBindings.Add(Expression("Iif(IsNull([Source2].Sum([Amount])),0,-[Source2].Sum([Amount]))"));
            xrPostedTotalNonReconciled.ExpressionBindings.Add(Expression("Iif(IsNull([Source3].Sum([Amount])),0,-[Source3].Sum([Amount]))"));
            xrBankTotalVoid.ExpressionBindings.Add(Expression("[BankVoid]"));
            xrPostedTotalVoid.ExpressionBindings.Add(Expression("[PostedVoid]"));
            xrBankReconciled.ExpressionBindings.Add(Expression("[BankTotal]-Iif(IsNull([Source2].Sum([Amount])),0,[Source2].Sum([Amount]))+[BankVoid]"));
            xrPostedReconciled.ExpressionBindings.Add(Expression("[PostedTotal]-Iif(IsNull([Source3].Sum([Amount])),0,[Source3].Sum([Amount]))+[PostedVoid]"));
            xrBankAccountValue.ExpressionBindings.Add(Expression(GetExpressionForConcatenateProperty("[BankAccountLabel]", "[RegNo]","[Account]")));

            //xrDifferencePanel.ExpressionBindings.Add(new ExpressionBinding("BeforePrint", "Visible", "Iif([Source2].Sum([Amount])-[Source3].Sum([Amount]) > '0',True,False)"));
            //xrDifference.ExpressionBindings.Add(Expression("[Difference]"));
            //xrNewBalance.ExpressionBindings.Add(Expression("[NewBalance]"));
            //xrDifferenceValue.ExpressionBindings.Add(Expression("[Source2].Sum([Amount])-[Source3].Sum([Amount])"));
            //xrNewBalanceValue.ExpressionBindings.Add(Expression("[PostedTotal]-[Source3].Sum([Amount])+[Source2].Sum([Amount])-[Source3].Sum([Amount])"));
        }

        private ExpressionBinding Expression(string expression)
        {
            return new ExpressionBinding("BeforePrint", "Text", expression);
        }

        private XRSummary Summary()
        {
            return new XRSummary() { Running = SummaryRunning.Report, Func = SummaryFunc.Sum, FormatString = "{0:n2}" };
        }

        private string GetExpressionForConcatenateProperty(params string[] propertyNames)
        {
            if (propertyNames != null && propertyNames.Length > 0)
            {
                var strBuilder = StringBuilderReuse.Create("FormatString('{0}: {1}-{2}'");
                for (int i = 0; (i < propertyNames.Length); i++)
                    strBuilder.Append(',').Append(propertyNames[i]);
                strBuilder.Append(')');

                return strBuilder.ToStringAndRelease();
            }

            return string.Empty;
        }
    }
}
