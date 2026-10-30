using System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public class PostingTraceStep
    {
        public bool IsMatch { get; set; }

        public bool IsManual { get; set; }

        public PostingTraceArea Area { get; set; }

        public PostingAccountReason Reason { get; set; }

        public ResolutionConfidence Confidence { get; set; }

        public string MatchingAccount { get; set; }

        public string AccountName { get; set; }

        public string AccountType { get; set; }

        public string Explanation { get; set; }

        public Type SourceTable { get; set; }
        public string SourceTableKey { get; set; }

        public string SourceField { get; set; }


       // public Action<PostingTraceStep> Action { get; set; }
    }
}
