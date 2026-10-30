using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public static class PostingTraceTreeBuilder
    {
        public static void Build(
            IList<PostingTraceResult> results,
            ObservableCollection<PostingTraceTreeItemViewModel> items)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            if (items == null)
                throw new ArgumentNullException(nameof(items));

            items.Clear();

            foreach (var result in results.Where(r => r != null))
            {
                var transactionItem = BuildTransactionItem(result);

                var steps = result.CheckedPostingSources != null ? result.CheckedPostingSources.Where(o => o != null).ToList() : new List<PostingTraceStep>();

                if (steps.Count == 0)
                {
                    transactionItem.Children.Add(new PostingTraceTreeItemViewModel
                    {
                        Title = Uniconta.ClientTools.Localization.lookup("GLTraceNoPostingSourcesFound"),
                        Detail = Uniconta.ClientTools.Localization.lookup("GLTraceEntryDidNotMatchAny"),
                        IsMatch = false,
                        Confidence = ResolutionConfidence.Unknown
                    });
                }
                else
                {
                    foreach (var pathGroup in steps
                        .GroupBy(o => o.Area)
                        .OrderBy(g => (int)g.Key))
                    {
                        var pathItem = new PostingTraceTreeItemViewModel
                        {
                            Title = PostingTraceTextFormatter.Area(pathGroup.Key),
                            Detail = BuildPathSummary(pathGroup),
                            IsMatch = pathGroup.Any(o => o.IsMatch),
                            Confidence = HighestConfidence(pathGroup)
                        };

                        foreach (var step in pathGroup
                            .OrderByDescending(o => o.IsMatch)
                            .ThenBy(o => (int)o.Reason)
                            .ThenBy(o => o.MatchingAccount))
                        {
                            pathItem.Children.Add(BuildReasonItem(step));
                        }

                        transactionItem.Children.Add(pathItem);
                    }
                }

                items.Add(transactionItem);
            }
        }

        private static PostingTraceTreeItemViewModel BuildTransactionItem(
            PostingTraceResult result)
        {
            var glTrans = result.GLTrans;

            var account = Convert.ToString(GetPropertyValue(glTrans, "Account"));
            var voucher = Convert.ToString(GetPropertyValue(glTrans, "Voucher"));
            var amount = Convert.ToString(GetPropertyValue(glTrans, "Amount"));
            var date = Convert.ToString(GetPropertyValue(glTrans, "Date"));
            var invoice = Convert.ToString(GetPropertyValue(glTrans, "Invoice"));

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(voucher))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Voucher") + ": " + voucher);

            if (!string.IsNullOrWhiteSpace(amount))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Amount") + ": " + amount);

            if (!string.IsNullOrWhiteSpace(date))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Date") + ": " + date);

            if (!string.IsNullOrWhiteSpace(invoice))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Invoice") + ": " + invoice);

            return new PostingTraceTreeItemViewModel
            {
                Title = Uniconta.ClientTools.Localization.lookup("GLTracePostedLedgerEntry") + " " + Safe(account),
                Detail = string.Join(" | ", parts),
                IsMatch = result.HasPostingSourceMatch,
                Confidence = HighestConfidence(result.CheckedPostingSources)
            };
        }

        private static PostingTraceTreeItemViewModel BuildReasonItem(
            PostingTraceStep step)
        {
            var title = step.IsMatch ? "Matched" : "Checked";
            title += " - " + PostingTraceTextFormatter.Reason(step.Reason);

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(step.MatchingAccount))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Account") + ": " + step.MatchingAccount);

            if (!string.IsNullOrWhiteSpace(step.AccountName))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Name") + ": " + step.AccountName);

            if (!string.IsNullOrWhiteSpace(step.AccountType))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Type") + ": " + step.AccountType);

            parts.Add(Uniconta.ClientTools.Localization.lookup("GLTraceMatchQuality2") + PostingTraceTextFormatter.Confidence(step.Confidence));

            if (step.SourceTable != null)
                parts.Add(Uniconta.ClientTools.Localization.lookup("GLTraceSetupTable") + PostingTraceTextFormatter.SetupArea(step.SourceTable.Name));

            if (!string.IsNullOrWhiteSpace(step.SourceTableKey))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Key") + ": " + step.SourceTableKey);

            if (!string.IsNullOrWhiteSpace(step.SourceField))
                parts.Add(Uniconta.ClientTools.Localization.lookup("Field") + ": " + step.SourceField);


            if (!string.IsNullOrWhiteSpace(step.Explanation))
                parts.Add(PostingTraceTextFormatter.Explanation(step.Explanation));

            return new PostingTraceTreeItemViewModel
            {
                Title = title,
                Detail = string.Join(Environment.NewLine, parts),
                IsMatch = step.IsMatch,
                Confidence = step.Confidence,
                Area = step.Area,
                Reason = step.Reason,
                Account = step.MatchingAccount
            };
        }

        private static string BuildPathSummary(IEnumerable<PostingTraceStep> steps)
        {
            var list = steps != null
                ? steps.Where(o => o != null).ToList()
                : new List<PostingTraceStep>();

            var matched = list.Count(o => o.IsMatch);
            var tried = list.Count;

            return matched + Uniconta.ClientTools.Localization.lookup("GLTraceMatched") + tried + Uniconta.ClientTools.Localization.lookup("GLTraceSetupAccountSChecked");
        }

        private static ResolutionConfidence HighestConfidence(IEnumerable<PostingTraceStep> steps)
        {
            if (steps == null)
                return ResolutionConfidence.Unknown;

            return steps
                .Where(o => o != null && o.IsMatch)
                .Select(o => o.Confidence)
                .DefaultIfEmpty(ResolutionConfidence.Unknown)
                .Max();
        }

        private static object GetPropertyValue(object instance, string propertyName)
        {
            if (instance == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            if (property == null)
                return null;

            return property.GetValue(instance, null);
        }

        private static string FormatEnum(Enum value)
        {
            if (value == null)
                return string.Empty;

            var text = value.ToString();
            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                    sb.Append(' ');

                sb.Append(text[i]);
            }

            return sb.ToString();
        }

        private static string Safe(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "(unknown)" : value;
        }
    }
}
