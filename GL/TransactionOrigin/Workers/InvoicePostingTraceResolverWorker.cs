using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public abstract class InvoicePostingTraceResolverWorker : IInvoicePostingTraceResolverWorker
    {

        public abstract void AddTraceSteps(IPostingTraceContext context, PostingTraceResult result);

        #region Matching helpers

        protected void RecordPossiblePostingSource(
            IPostingTraceContext context,
            PostingTraceResult result,
            PostingAccountCandidate candidate,
            PostingTraceArea mainPath,
            PostingAccountReason fallbackReason,
            ResolutionConfidence confidence,
            string explanation)
        {
            if (candidate == null)
                return;

            var isMatch = EqualsPostedAccount(context, candidate.Account);

            //if (!EqualsPostedAccount(context, candidate.Account))
            //return;

            result.Add(new PostingTraceStep
            {
                IsMatch = isMatch,
                Area = mainPath,
                Reason = candidate.Reason == PostingAccountReason.Unknown
                    ? fallbackReason
                    : candidate.Reason,
                Confidence = candidate.Confidence == ResolutionConfidence.Unknown
                    ? confidence
                    : candidate.Confidence,
                MatchingAccount = candidate.Account,
                AccountName = string.IsNullOrWhiteSpace(candidate.AccountName) ? context.GetFinancialAccountName(candidate.Account) : candidate.AccountName,
                AccountType = string.IsNullOrWhiteSpace(candidate.AccountType) ? context.GetFinancialAccountType(candidate.Account) : candidate.AccountType,
                Explanation = string.IsNullOrWhiteSpace(candidate.Explanation) ? explanation : candidate.Explanation,
                IsManual = candidate.IsManual,
                SourceTable = candidate.SourceTable,
                SourceTableKey = candidate.SourceTableKey,
                SourceField = candidate.SourceField
                //TODO: remove action?
                //Action = r => Uniconta.ClientTools.InfoLog.Log.Info("Click")
            });
        }

        protected void RecordPossiblePostingSources(
            IPostingTraceContext context,
            PostingTraceResult result,
            IEnumerable<PostingAccountCandidate> candidates,
            PostingTraceArea mainPath,
            ResolutionConfidence confidence,
            string explanation)
        {
            if (candidates == null)
                return;

            foreach (var candidate in candidates)
            {
                RecordPossiblePostingSource(
                    context,
                    result,
                    candidate,
                    mainPath,
                    PostingAccountReason.Unknown,
                    confidence,
                    explanation);
            }
        }

        protected virtual bool EqualsPostedAccount(IPostingTraceContext context, string candidateAccount) => !string.IsNullOrEmpty(candidateAccount) && context.PostedAccount == candidateAccount;
        #endregion
    }
}
