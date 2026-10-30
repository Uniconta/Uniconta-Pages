using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uniconta.API.System;
using Newtonsoft.Json;
using Uniconta.ClientTools.Util;
using Uniconta.DataModel;
using Uniconta.ClientTools.AI;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.AI.Services.Context;
using Localization = Uniconta.ClientTools.Localization;
using Uniconta.ClientTools.AI.Services;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Controls.Dialogs;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Anomaly
{
    public sealed class LedgerTrendAnomalyFinder
    {
        private const int MaximumAITransactionsPerAccount = 100;
        private const int MaximumAIPromptLength = 30000;

        public double MinimumAbsoluteDifference { get; set; }
        public double MinimumPercentageChange { get; set; }
        public int MaximumAnomalies { get; set; }
        public int MaximumTransactionsPerAnomaly { get; set; }

        public LedgerTrendAnomalyFinder()
        {
            MinimumAbsoluteDifference = 1000d;
            MinimumPercentageChange = 30d;
            MaximumAnomalies = 20;
            MaximumTransactionsPerAnomaly = 10;
        }

        public Task<IReadOnlyList<LedgerTrendAnomalyResult>> FindAnomalies(CrudAPI api, LedgerTrendAnomalyParameters parameters, IProgress<LedgerTrendAnomalyProgress> progress)
        {
            return FindAnomalies(api, parameters, progress, CancellationToken.None);
        }

        public async Task<IReadOnlyList<LedgerTrendAnomalyResult>> FindAnomalies(CrudAPI api, LedgerTrendAnomalyParameters parameters, IProgress<LedgerTrendAnomalyProgress> progress, CancellationToken cancellationToken)
        {
            if (api == null)
                throw new ArgumentNullException("api");

            if (parameters == null)
                throw new ArgumentNullException("parameters");

            cancellationToken.ThrowIfCancellationRequested();

            ApplyParameters(parameters);
            var periods = parameters.GetPeriods();
            var accountNumbers = parameters.GetAccountNumberSet();

            Report(progress, 5, Localization.lookup("AnomalyLoadingTransactions"));
            var transactions = await LoadTransactionsAsync(api, periods, accountNumbers, progress, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            Report(progress, 45, Localization.lookup("AnomalyGroupingBalances"));
            var balances = BuildBalances(transactions, periods);

            cancellationToken.ThrowIfCancellationRequested();

            Report(progress, 60, Localization.lookup("AnomalyDetectingMovements"));
            var results = BuildResults(balances, transactions, periods);
            Report(progress, 75, Localization.lookup("AnomalyPreparingExplanations"), results);

            cancellationToken.ThrowIfCancellationRequested();

            Report(progress, 80, Localization.lookup("AnomalyRequestingExplanations"));
            await AddAIExplanationsPerAccountAsync(api, results, transactions, periods, progress, cancellationToken).ConfigureAwait(false);

            Report(progress, 100, Localization.lookup("AnomalyAnalysisComplete"));
            return results;
        }

        private void ApplyParameters(LedgerTrendAnomalyParameters parameters)
        {
            MinimumAbsoluteDifference = parameters.MinimumAbsoluteDifference;
            MinimumPercentageChange = parameters.MinimumPercentageChange;
            MaximumAnomalies = parameters.MaximumAnomalies;
            MaximumTransactionsPerAnomaly = parameters.MaximumTransactionsPerAnomaly;
        }

        internal IReadOnlyList<LedgerTrendAnomalyResult> BuildResults(IEnumerable<LedgerAccountPeriodBalance> balances, IEnumerable<LedgerTransactionLine> transactions, LedgerPeriod[] periods)
        {
            var balanceList = balances == null ? new List<LedgerAccountPeriodBalance>() : balances.ToList();
            var transactionList = transactions == null ? new List<LedgerTransactionLine>() : transactions.ToList();
            var currentTransactions = transactionList.Where(t => t.Date >= periods[2].FromDate && t.Date <= periods[2].ToDate).ToList();
            var results = new List<LedgerTrendAnomalyResult>();

            foreach (var accountGroup in balanceList.GroupBy(b => b.AccountNumber))
            {
                var minus2 = GetAmount(accountGroup, periods[0].Caption);
                var minus1 = GetAmount(accountGroup, periods[1].Caption);
                var current = GetAmount(accountGroup, periods[2].Caption);

                var previousMovement = minus1 - minus2;
                var currentMovement = current - minus1;
                var percentageChange = CalculatePercentageChange(currentMovement, minus1);
                var reasons = GetReasons(minus2, minus1, current, previousMovement, currentMovement, percentageChange);

                if (reasons.Count == 0)
                    continue;

                var firstBalance = accountGroup.FirstOrDefault();

                var topTransactions = currentTransactions
                    .Where(t => string.Equals(t.AccountNumber, accountGroup.Key, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(t => Math.Abs(t.Amount))
                    .Take(MaximumTransactionsPerAnomaly)
                    .ToList();

                results.Add(new LedgerTrendAnomalyResult
                {
                    AccountNumber = accountGroup.Key,
                    AccountName = firstBalance == null ? null : firstBalance.AccountName,
                    PeriodMinus2 = periods[0].Caption,
                    PeriodMinus1 = periods[1].Caption,
                    CurrentPeriod = periods[2].Caption,
                    AmountMinus2 = minus2,
                    AmountMinus1 = minus1,
                    CurrentAmount = current,
                    PreviousMovement = previousMovement,
                    CurrentMovement = currentMovement,
                    CurrentPercentageChange = percentageChange,
                    Reasons = string.Join("; ", reasons.ToArray()),
                    TopCurrentPeriodTransactionsText = FormatTransactions(topTransactions),
                    RiskLevel = GetDeterministicRiskLevel(reasons.Count, currentMovement, percentageChange)
                });
            }

            return results
                .OrderByDescending(r => Math.Abs(r.CurrentMovement))
                .ThenBy(r => r.AccountNumber)
                .Take(MaximumAnomalies)
                .ToList();
        }

        private static List<LedgerAccountPeriodBalance> BuildBalances(IEnumerable<LedgerTransactionLine> transactions, LedgerPeriod[] periods)
        {
            var balances = new List<LedgerAccountPeriodBalance>();

            foreach (var period in periods)
            {
                balances.AddRange(transactions
                    .Where(t => t.Date >= period.FromDate && t.Date <= period.ToDate && !string.IsNullOrWhiteSpace(t.AccountNumber))
                    .GroupBy(t => t.AccountNumber)
                    .Select(g => new LedgerAccountPeriodBalance
                    {
                        AccountNumber = g.Key,
                        AccountName = g.Select(t => t.AccountName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)),
                        Period = period.Caption,
                        Amount = g.Sum(t => t.Amount)
                    }));
            }

            return balances;
        }

        private async Task<List<LedgerTransactionLine>> LoadTransactionsAsync(CrudAPI api, LedgerPeriod[] periods, HashSet<string> accountNumbers, IProgress<LedgerTrendAnomalyProgress> progress, CancellationToken cancellationToken)
        {
            var transactions = new List<LedgerTransactionLine>();
            var fromDate = periods.Min(p => p.FromDate);
            var toDate = periods.Max(p => p.ToDate);

            await api.QueryPerMonthAsync<GLTransClient>(nameof(GLTransClient.Date), fromDate, toDate, dataPerMonth =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (dataPerMonth != null)
                {
                    transactions.AddRange(dataPerMonth
                        .Where(t => IncludeAccount(t.Account, accountNumbers))
                        .Select(t => new LedgerTransactionLine
                        {
                            AccountNumber = t.Account,
                            AccountName = t.Name,
                            Date = t.Date,
                            Voucher = t.Voucher,
                            Text = t.Text,
                            Amount = t.Amount
                        }));
                }

                Report(progress, 25, string.Format(Localization.lookup("AnomalyTransactionsLoaded"), transactions.Count));
                cancellationToken.ThrowIfCancellationRequested();
            }).ConfigureAwait(false);

            return transactions;
        }

        private static bool IncludeAccount(string accountNumber, HashSet<string> accountNumbers)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
                return false;

            return accountNumbers == null || accountNumbers.Contains(accountNumber);
        }

        private async Task AddAIExplanationsPerAccountAsync(CrudAPI api, IReadOnlyList<LedgerTrendAnomalyResult> results, List<LedgerTransactionLine> transactions, LedgerPeriod[] periods, IProgress<LedgerTrendAnomalyProgress> progress, CancellationToken cancellationToken)
        {
            if (results == null || results.Count == 0)
                return;

            IGPTService gptService;
            try
            {
                gptService = GPTServiceFactory.Instance.GetActiveService(api);
                if (gptService?.SettingsData?.IsValid() != true)
                    return;
            }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Warning(ex.Message);
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = results[i];
                Report(progress, 80 + (i * 15) / results.Count,
                    string.Format(Localization.lookup("AnomalyRequestingAccountExplanation"), result.AccountNumber));
                try
                {
                    var context = new GPTConversationContext();
                    PromptProvider.From(() => AccountExplanationPrompt)
                        .Replace("Evidence", BuildAccountPrompt(result, transactions, periods))
                        .EnrichContext(context);
                    var response = await gptService.GetResponse<LedgerTrendAnomalyAIResult>(context, cancellationToken).ConfigureAwait(false);
                    if (response != null)
                    {
                        result.AIExplanation = response.Reason;
                        result.AICertaintyPercentage = NormalizeCertaintyPercentage(response.CertaintyPercentage);
                        result.AIRiskLevel = NormalizeRiskLevel(response.RiskLevel);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // One provider failure must not prevent explanations for the remaining accounts.
                    Uniconta.ClientTools.InfoLog.Log.Warning($"AI explanation for account '{result.AccountNumber}' failed: {ex.Message}");
                }
                Report(progress, 80 + ((i + 1) * 15) / results.Count,
                    string.Format(Localization.lookup("AnomalyReceivedAccountExplanation"), result.AccountNumber), results);
            }
        }

        [PromptDefinition("GeneralLedger_AnomalyExplanation", "Explains a deterministic ledger trend anomaly using the supplied account balances and sampled transactions.")]
        [PromptDefinitionReplacement("Evidence", "The three-period account balances, deterministic anomaly reasons and sampled transactions.")]
        public static PromptDefinition AccountExplanationPrompt => PromptDefinition.FromMessages(new[]
        {
            new PromptRoleNode
            {
                Role = MessageRole.Developer,
                Content = "Act as a financial controller reviewing one ledger account anomaly. Use only the supplied evidence. " +
                    "Distinguish the deterministic findings from your interpretation. Do not invent missing facts or treat sampled transactions as complete. " +
                    "Return Reason, CertaintyPercentage (an integer from 0 to 100), and RiskLevel (high, medium or low)."
            },
            new PromptRoleNode { Role = MessageRole.User, Content = "Explain this account anomaly:\n{{Evidence}}" }
        }, expectsExplanationInUserLanguage: true);

        private string BuildAccountPrompt(LedgerTrendAnomalyResult result, List<LedgerTransactionLine> transactions, LedgerPeriod[] periods)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Account summary:");
            sb.AppendLine("AccountNumber: " + NullSafe(result.AccountNumber));
            sb.AppendLine("AccountName: " + NullSafe(result.AccountName));
            sb.AppendLine(result.PeriodMinus2 + ": " + FormatAmount(result.AmountMinus2));
            sb.AppendLine(result.PeriodMinus1 + ": " + FormatAmount(result.AmountMinus1));
            sb.AppendLine(result.CurrentPeriod + ": " + FormatAmount(result.CurrentAmount));
            sb.AppendLine("PreviousMovement: " + FormatAmount(result.PreviousMovement));
            sb.AppendLine("CurrentMovement: " + FormatAmount(result.CurrentMovement));
            sb.AppendLine("CurrentPercentageChange: " + result.CurrentPercentageChange + "%");
            sb.AppendLine("DeterministicReasons: " + NullSafe(result.Reasons));
            sb.AppendLine("DeterministicRiskLevel: " + NullSafe(result.RiskLevel));
            sb.AppendLine();

            // Reserve equal space for every period so older evidence cannot crowd out current activity.
            var periodBudget = Math.Max(0, (MaximumAIPromptLength - sb.Length) / 3);
            AppendPeriodTransactions(sb, "Period before previous transactions", result.AccountNumber, transactions, periods[0], periodBudget);
            AppendPeriodTransactions(sb, "Previous period transactions", result.AccountNumber, transactions, periods[1], periodBudget);
            AppendPeriodTransactions(sb, "Current period transactions", result.AccountNumber, transactions, periods[2], periodBudget);

            return sb.ToString();
        }

        private static void AppendPeriodTransactions(StringBuilder sb, string title, string accountNumber,
            List<LedgerTransactionLine> transactions, LedgerPeriod period, int budget)
        {
            var section = new StringBuilder();
            section.AppendLine(title + " (" + period.Caption + "):");
            var all = transactions.Where(t =>
                    string.Equals(t.AccountNumber, accountNumber, StringComparison.OrdinalIgnoreCase) &&
                    t.Date >= period.FromDate && t.Date <= period.ToDate)
                .OrderByDescending(t => Math.Abs(t.Amount)).ToList();
            var included = 0;
            foreach (var transaction in all.Take(MaximumAITransactionsPerAccount))
            {
                var text = NullSafe(transaction.Text);
                if (text.Length > 500) text = text.Substring(0, 500) + " [text truncated]";
                var line = string.Format("{0:yyyy-MM-dd} | voucher {1} | amount {2} | text {3}",
                    transaction.Date, transaction.Voucher, FormatAmount(transaction.Amount), text);
                if (section.Length + line.Length + 200 > budget) break;
                section.AppendLine(line);
                included++;
            }
            section.AppendLine($"Included {included} of {all.Count} transactions, selected by absolute amount.");
            section.AppendLine();
            sb.Append(section);
        }

        private static int NormalizeCertaintyPercentage(int certaintyPercentage)
        {
            if (certaintyPercentage < 0)
                return 0;

            if (certaintyPercentage > 100)
                return 100;

            return certaintyPercentage;
        }

        private static void Report(IProgress<LedgerTrendAnomalyProgress> progress, int value, string status)
        {
            Report(progress, value, status, null);
        }

        private static void Report(IProgress<LedgerTrendAnomalyProgress> progress, int value, string status, IReadOnlyList<LedgerTrendAnomalyResult> results)
        {
            if (progress != null)
                progress.Report(new LedgerTrendAnomalyProgress(value, status, results));
        }

        private List<string> GetReasons(double minus2, double minus1, double current, double previousMovement, double currentMovement, double percentageChange)
        {
            var reasons = new List<string>();

            if (Math.Abs(currentMovement) >= MinimumAbsoluteDifference)
                reasons.Add(Localization.lookup("AnomalyLargeAbsoluteMovement"));

            if (Math.Abs(percentageChange) >= MinimumPercentageChange)
                reasons.Add(Localization.lookup("AnomalyLargePercentageMovement"));

            if (Math.Abs(currentMovement - previousMovement) >= MinimumAbsoluteDifference)
                reasons.Add(Localization.lookup("AnomalyUnusualMovement"));

            if (previousMovement != 0d && currentMovement != 0d && Math.Sign(previousMovement) != Math.Sign(currentMovement))
                reasons.Add(Localization.lookup("AnomalyDirectionChanged"));

            if (minus2 == 0d && minus1 == 0d && current != 0d)
                reasons.Add(Localization.lookup("AnomalyNewActivity"));

            if (minus2 != 0d && minus1 != 0d && current == 0d)
                reasons.Add(Localization.lookup("AnomalyAccountDroppedToZero"));

            return reasons;
        }

        private static double GetAmount(IEnumerable<LedgerAccountPeriodBalance> balances, string period)
            => balances.Where(b => string.Equals(b.Period, period, StringComparison.OrdinalIgnoreCase))
                       .Sum(b => b.Amount);


        private static double CalculatePercentageChange(double movement, double previousAmount)
        {
            if (previousAmount == 0d)
                return movement == 0d ? 0d : 100d;

            return Math.Round((movement / Math.Abs(previousAmount)) * 100d, 2);
        }

        private static string GetDeterministicRiskLevel(int reasonCount, double movement, double percentageChange)
        {
            if (reasonCount >= 4 || Math.Abs(movement) >= 10000d || Math.Abs(percentageChange) >= 100d)
                return Localization.lookup("LevelHigh");

            if (reasonCount >= 2)
                return Localization.lookup("LevelMedium");

            return Localization.lookup("LevelLow");
        }

        private static string NormalizeRiskLevel(string riskLevel)
        {
            switch (riskLevel?.Trim().ToLowerInvariant())
            {
                case "high": return Localization.lookup("LevelHigh");
                case "medium": return Localization.lookup("LevelMedium");
                case "low": return Localization.lookup("LevelLow");
                default: return null;
            }
        }

        private static string FormatTransactions(IEnumerable<LedgerTransactionLine> transactions)
        {
            if (transactions == null)
                return null;

            return string.Join(" | ", transactions
                .Select(t => string.Format(Localization.lookup("AnomalyTransactionSummary"), t.Date, Localization.lookup("Voucher"), t.Voucher, t.Amount, t.Text))
                .ToArray());
        }

        private static string FormatAmount(double amount) => amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        private static string NullSafe(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value;
    }

    public sealed class LedgerAccountPeriodBalance
    {
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
        public string Period { get; set; }
        public double Amount { get; set; }
    }

    public sealed class LedgerTransactionLine
    {
        public string AccountNumber { get; set; }
        public DateTime Date { get; set; }
        public int Voucher { get; set; }
        public string AccountName { get; set; }
        public string Text { get; set; }
        public double Amount { get; set; }
    }

    public sealed class LedgerTrendAnomalyProgress
    {
        public LedgerTrendAnomalyProgress(int progressValue, string statusText, IReadOnlyList<LedgerTrendAnomalyResult> results = null)
        {
            ProgressValue = progressValue;
            StatusText = statusText;
            Results = results;
        }

        public int ProgressValue { get; private set; }
        public string StatusText { get; private set; }
        public IReadOnlyList<LedgerTrendAnomalyResult> Results { get; private set; }
    }

    public sealed class LedgerTrendAnomalyParameters : LastValue.IPackableWithKey
    {
        public const string LastValueKey = nameof(LedgerTrendAnomalyParameters);

        [JsonIgnore]
        public string PackKey => LastValueKey;

        public DateTime CurrentPeriodFrom { get; set; }
        public DateTime CurrentPeriodTo { get; set; }
        public DateTime PreviousPeriodFrom { get; set; }
        public DateTime PreviousPeriodTo { get; set; }
        public DateTime PeriodBeforePreviousFrom { get; set; }
        public DateTime PeriodBeforePreviousTo { get; set; }

        public double MinimumAbsoluteDifference { get; set; }
        public double MinimumPercentageChange { get; set; }
        public int MaximumAnomalies { get; set; }
        public int MaximumTransactionsPerAnomaly { get; set; }

        // Account selection belongs to the current page, not the saved dialog settings.
        [JsonIgnore]
        public IReadOnlyList<string> AccountNumbers { get; set; }

        public HashSet<string> GetAccountNumberSet()
        {
            if (AccountNumbers == null || AccountNumbers.Count == 0)
                return null;

            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var accountNumber in AccountNumbers)
            {
                if (!string.IsNullOrWhiteSpace(accountNumber))
                    set.Add(accountNumber);
            }

            return set.Count == 0 ? null : set;
        }

        public static LedgerTrendAnomalyParameters CreateDefault(DateTime today, Company company = null)
        {
            var parameters = new LedgerTrendAnomalyParameters();

            if (!LastValue.Unpack(company, parameters) || !parameters.HasValidValues())
                parameters.Reset(today);

            return parameters;
        }

        public void InitParmDefault() => Reset(DateTime.Today);

        public void Reset(DateTime today)
        {
            var currentStart = new DateTime(today.Year, today.Month, 1).AddMonths(-1);

            PeriodBeforePreviousFrom = currentStart.AddMonths(-2);
            PeriodBeforePreviousTo = currentStart.AddMonths(-1).AddDays(-1);

            PreviousPeriodFrom = currentStart.AddMonths(-1);
            PreviousPeriodTo = currentStart.AddDays(-1);

            CurrentPeriodFrom = currentStart;
            CurrentPeriodTo = currentStart.AddMonths(1).AddDays(-1);

            MinimumAbsoluteDifference = 1000d;
            MinimumPercentageChange = 30d;
            MaximumAnomalies = 20;
            MaximumTransactionsPerAnomaly = 10;
        }

        /// <summary>
        /// Changes the start of the current period and shifts all periods by the
        /// same calendar offset.
        ///
        /// Example:
        ///
        /// Current:          01-09-2026 - 30-09-2026
        /// Previous:         01-08-2026 - 31-08-2026
        /// Before previous:  01-07-2026 - 31-07-2026
        ///
        /// ChangeCurrentPeriodFrom(01-09-2025) results in:
        ///
        /// Current:          01-09-2025 - 30-09-2025
        /// Previous:         01-08-2025 - 31-08-2025
        /// Before previous:  01-07-2025 - 31-07-2025
        /// </summary>
        public void ChangeCurrentPeriodFrom(DateTime newFrom)
        {
            newFrom = newFrom.Date;

            if (CurrentPeriodFrom.Date == newFrom)
                return;

            ShiftAllPeriods(CurrentPeriodFrom.Date, newFrom);
        }

        public void ChangeCurrentPeriodTo(DateTime newTo)
        {
            newTo = newTo.Date;

            if (CurrentPeriodTo.Date == newTo)
                return;

            ShiftAllPeriods(CurrentPeriodTo.Date, newTo);
        }

        private void ShiftAllPeriods(DateTime oldAnchor, DateTime newAnchor)
        {
            int months;

            if (TryGetMonthShift(oldAnchor, newAnchor, out months))
            {
                PeriodBeforePreviousFrom = PeriodBeforePreviousFrom.AddMonths(months);
                PeriodBeforePreviousTo = PeriodBeforePreviousTo.AddMonths(months);

                PreviousPeriodFrom = PreviousPeriodFrom.AddMonths(months);
                PreviousPeriodTo = PreviousPeriodTo.AddMonths(months);

                CurrentPeriodFrom = CurrentPeriodFrom.AddMonths(months);
                CurrentPeriodTo = CurrentPeriodTo.AddMonths(months);

                return;
            }

            var days = (newAnchor - oldAnchor).Days;

            PeriodBeforePreviousFrom = PeriodBeforePreviousFrom.AddDays(days);
            PeriodBeforePreviousTo = PeriodBeforePreviousTo.AddDays(days);

            PreviousPeriodFrom = PreviousPeriodFrom.AddDays(days);
            PreviousPeriodTo = PreviousPeriodTo.AddDays(days);

            CurrentPeriodFrom = CurrentPeriodFrom.AddDays(days);
            CurrentPeriodTo = CurrentPeriodTo.AddDays(days);
        }

        private static bool TryGetMonthShift(DateTime oldDate, DateTime newDate, out int months)
        {
            months =
                ((newDate.Year - oldDate.Year) * 12) +
                (newDate.Month - oldDate.Month);

            return oldDate.AddMonths(months) == newDate;
        }

        private bool HasValidValues()
        {
            return
                CurrentPeriodFrom > DateTime.MinValue &&
                CurrentPeriodTo >= CurrentPeriodFrom &&

                PreviousPeriodFrom > DateTime.MinValue &&
                PreviousPeriodTo >= PreviousPeriodFrom &&

                PeriodBeforePreviousFrom > DateTime.MinValue &&
                PeriodBeforePreviousTo >= PeriodBeforePreviousFrom &&

                !double.IsNaN(MinimumAbsoluteDifference) &&
                !double.IsInfinity(MinimumAbsoluteDifference) &&
                MinimumAbsoluteDifference >= 0 &&
                MinimumAbsoluteDifference <= int.MaxValue &&

                !double.IsNaN(MinimumPercentageChange) &&
                !double.IsInfinity(MinimumPercentageChange) &&
                MinimumPercentageChange >= 0 &&
                MinimumPercentageChange <= int.MaxValue &&

                MaximumAnomalies >= 1 &&
                MaximumAnomalies <= 100 &&

                MaximumTransactionsPerAnomaly >= 1 &&
                MaximumTransactionsPerAnomaly <= 50;
        }

        public LedgerPeriod[] GetPeriods()
        => new[]
        {
            CreatePeriod( PeriodBeforePreviousFrom, PeriodBeforePreviousTo, Localization.lookup("PeriodBeforePrevious")),
            CreatePeriod( PreviousPeriodFrom, PreviousPeriodTo, Localization.lookup("PreviousPeriod")),
            CreatePeriod(CurrentPeriodFrom, CurrentPeriodTo, Localization.lookup("CurrentPeriod"))
        };

        private static LedgerPeriod CreatePeriod(
            DateTime fromDate,
            DateTime toDate,
            string fallbackCaption)
        {
            return new LedgerPeriod
            {
                FromDate = fromDate.Date,
                ToDate = toDate.Date,

                Caption =
                    fromDate.Year == toDate.Year &&
                    fromDate.Month == toDate.Month
                        ? fromDate.ToString("yyyy-MM")
                        : string.Format(
                            "{0}: {1:yyyy-MM-dd} - {2:yyyy-MM-dd}",
                            fallbackCaption,
                            fromDate,
                            toDate)
            };
        }
    }

    public sealed class LedgerPeriod
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Caption { get; set; }
    }

    public sealed class LedgerTrendAnomalyAIResponse
    {
        public List<LedgerTrendAnomalyAIResult> Results { get; set; }
    }

    public sealed class LedgerTrendAnomalyAIResult
    {
        public string AccountNumber { get; set; }
        public string Reason { get; set; }
        public string RiskLevel { get; set; }
        public int CertaintyPercentage { get; set; }
    }
}
