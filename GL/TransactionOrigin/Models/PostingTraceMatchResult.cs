using System;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    /// <summary>
    /// A flattened matched posting source, used by the Match data grid and its Excel export.
    /// </summary>
    public sealed class PostingTraceMatchResult : UnicontaBaseEntity
    {
        public int Voucher { get; set; }
        public int VoucherLine { get; set; }
        public DateTime Date { get; set; }
        public string TransactionAccount { get; set; }
        public string TransactionAccountName { get; set; }
        public string TransactionText { get; set; }
        public double TransactionAmount { get; set; }
        public bool IsManual { get; set; }
        public string Explanation { get; set; }
        public string MatchingAccount { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }
        public PostingTraceArea Area { get; set; }
        public PostingAccountReason Reason { get; set; }
        public ResolutionConfidence Confidence { get; set; }
        public string SourceTable { get; set; }
        public string SourceTableKey { get; set; }
        public string SourceField { get; set; }

        public int CompanyId { get; set; }
        public Type BaseEntityType() { return typeof(PostingTraceMatchResult); }
        public int ClassId() { return -1; }
        public void loadFields(CustomReader r, int SavedWithVersion) { }
        public void saveFields(CustomWriter w, int SaveVersion) { }
        public int Version(int ClientAPIVersion) { return -1; }
    }
}
