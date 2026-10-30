using System;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Anomaly
{
    public sealed class LedgerTrendAnomalyResult : UnicontaBaseEntity
    {
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string PeriodMinus2 { get; set; }
        public string PeriodMinus1 { get; set; }
        public string CurrentPeriod { get; set; }
        public double AmountMinus2 { get; set; }
        public double AmountMinus1 { get; set; }
        public double CurrentAmount { get; set; }
        public double PreviousMovement { get; set; }
        public double CurrentMovement { get; set; }
        public double CurrentPercentageChange { get; set; }
        public string Reasons { get; set; }
        public string TopCurrentPeriodTransactionsText { get; set; }
        public string RiskLevel { get; set; }
        public string AIRiskLevel { get; set; }
        public string AIExplanation { get; set; }
        public int AICertaintyPercentage { get; set; }
        public int CompanyId { get => Utilities.Utility.GetDefaultCompany().CompanyId; set { } }

        public Type BaseEntityType() { return typeof(LedgerTrendAnomalyResult); }
        public int ClassId() { return -1; }
        public void loadFields(CustomReader r, int SavedWithVersion) { }
        public void saveFields(CustomWriter w, int SaveVersion) { }
        public int Version(int ClientAPIVersion) { return -1; }
    }
}
