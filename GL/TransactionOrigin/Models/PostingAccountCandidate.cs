using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public class PostingAccountCandidate
    {
        public string Account { get; set; }

        public  bool IsManual { get; set; }

        public string AccountName { get; set; }

        public string AccountType { get; set; }

        public PostingAccountReason Reason { get; set; }

        public ResolutionConfidence Confidence { get; set; }

        public string Explanation { get; set; }

        public System.Type SourceTable { get; set; }
        public string SourceTableKey { get; set; }

        public string SourceField { get; set; }

    }
}
