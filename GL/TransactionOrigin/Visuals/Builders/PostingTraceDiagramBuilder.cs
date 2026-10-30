using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public static class PostingTraceDiagramBuilder
    {
        private const double Margin = 30;

        private const double VoucherX = 0;
        private const double TransactionX = 310;
        private const double SectionX = 620;
        private const double ReasonX = 930;

        private const double VoucherHeight = 150;
        private const double VoucherWidth = 300;

        private const double TransactionHeight = 240;
        private const double TransactionWidth = 250;

        private const double SectionHeight = 120;
        private const double SectionWidth = 240;

        private const double ReasonHeight = 200;
        private const double ReasonWidth = 360;


        private const double ReasonGap = 8;
        private const double SectionGap = 26;
        private const double TransactionBlockGap = 45.0;

        public static void Build(Company company, IList<PostingTraceResult> results, ObservableCollection<DiagramNodeViewModel> nodes, ObservableCollection<DiagramConnectorViewModel> connectors)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));

            if (connectors == null)
                throw new ArgumentNullException(nameof(connectors));

            nodes.Clear();
            connectors.Clear();

            var validResults = results.Where(r => r != null).ToList();

            if (validResults.Count == 0)
                return;

            var voucherNode = GetNewVoucherViewModel(validResults);

            //var voucherNode = new VoucherDiagramNodeViewModel
            //{
            //    Id = "voucher",
            //    Kind = DiagramNodeKind.Voucher,
            //    Title = GetVoucher(validResults) ?? "(unknown)",
            //    VoucherText = GetVoucher(validResults) ?? "(unknown)",
            //    Subtitle = GetVoucherSubtitle(validResults),
            //    VoucherSubtitle = GetVoucherSubtitle(validResults),
            //    Detail = validResults.Count + " financial posting(s) on this voucher",
            //    PostingCountText = validResults.Count + " financial posting(s) on this voucher",
            //    X = VoucherX,
            //    Y = Margin,
            //    Width = VoucherWidth,
            //    Height = VoucherHeight,
            //    MinWidth = 180,
            //    MinHeight = 70
            //};

            nodes.Add(voucherNode);

            var transactionNodes = new List<DiagramNodeViewModel>();
            var sectionsByIdentity = new Dictionary<string, SectionDiagramNodeViewModel>();
            var reasonsByIdentity = new Dictionary<string, ReasonDiagramNodeViewModel>();

            var currentBlockY = Margin;

            //company.Currency

            for (int resultIndex = 0; resultIndex < validResults.Count; resultIndex++)
            {
                var result = validResults[resultIndex];
                var glTrans = result.GLTrans;



                var transactionNode = new TransactionDiagramNodeViewModel
                {
                    Id = "transaction_" + resultIndex,
                    Kind = DiagramNodeKind.Transaction,

                    GLAccount = glTrans.Master,

                    BookedDateText = glTrans.Date.ToString("d"),
                    Amount = glTrans.AmountCur == 0 ? glTrans.Amount : glTrans.AmountCur,
                    CurrencyCode = !string.IsNullOrEmpty(glTrans.Currency) ? glTrans.Currency : ((Currencies)company._Currency).ToString(),
                    Origin = glTrans.Origin,
                    Invoice = glTrans.Invoice.ToString(),
                    Detail = glTrans.Text,
                    X = TransactionX,
                    Y = currentBlockY,
                    Width = TransactionWidth,
                    Height = TransactionHeight,
                    MinWidth = 220,
                    MinHeight = 140
                };

                nodes.Add(transactionNode);
                transactionNodes.Add(transactionNode);

                connectors.Add(new DiagramConnectorViewModel
                {
                    Id = "connector_v_t_" + resultIndex,
                    FromNode = voucherNode,
                    ToNode = transactionNode,
                    Kind = DiagramConnectorKind.VoucherToTransaction,
                    Confidence = ResolutionConfidence.Unknown
                });

                var localReasons = result.MatchedPostingSources != null ? result.MatchedPostingSources.Where(o => o != null).ToList() : new List<PostingTraceStep>();
                if (localReasons.Count == 0)
                {
                    currentBlockY += TransactionHeight + TransactionBlockGap;
                    continue;
                }

                var localSections = localReasons
                    .GroupBy(o => GetSection(o.Area))
                    .OrderBy(g => g.Key.SortOrder)
                    .ToList();

                double sectionY = currentBlockY;
                double blockBottom = currentBlockY + transactionNode.Height;

                foreach (var sectionGroup in localSections)
                {
                    var section = sectionGroup.Key;

                    var steps = sectionGroup
                        .OrderBy(o => (int)o.Area)
                        .ThenBy(o => (int)o.Reason)
                        .ToList();
                    var sectionIdentity = GetSectionIdentity(section, steps);
                    SectionDiagramNodeViewModel sectionNode;
                    var isNewSection = !sectionsByIdentity.TryGetValue(sectionIdentity, out sectionNode);

                    if (isNewSection)
                    {
                        sectionNode = new SectionDiagramNodeViewModel
                        {
                            Id = "section_" + sectionsByIdentity.Count,
                            Kind = DiagramNodeKind.Section,
                            Title = section.Title,
                            SectionTitle = section.Title,
                            Subtitle = section.RangeText,
                            RangeText = section.RangeText,
                            Detail = section.Description,
                            Description = section.Description,
                            X = SectionX,
                            Y = sectionY,
                            Width = SectionWidth,
                            Height = SectionHeight,
                            MinWidth = 200,
                            MinHeight = 70
                        };

                        sectionsByIdentity.Add(sectionIdentity, sectionNode);
                        nodes.Add(sectionNode);
                    }

                    connectors.Add(new DiagramConnectorViewModel
                    {
                        Id = "connector_t_s_" + resultIndex + "_" + section.SortOrder,
                        FromNode = transactionNode,
                        ToNode = sectionNode,
                        Kind = DiagramConnectorKind.TransactionToSection,
                        Confidence = HighestConfidence(sectionGroup.Select(x => x))
                    });

                    double stepY = sectionNode.Y;
                    double sectionBottom = sectionNode.Y + sectionNode.Height;

                    foreach (var step in steps)
                    {
                        var reasonIdentity = GetReasonIdentity(step);
                        ReasonDiagramNodeViewModel stepNode;
                        if (!reasonsByIdentity.TryGetValue(reasonIdentity, out stepNode))
                        {
                            stepNode = CreateReasonNode(step, reasonsByIdentity.Count, stepY);
                            reasonsByIdentity.Add(reasonIdentity, stepNode);
                            nodes.Add(stepNode);
                        }

                        if (!connectors.Any(connector => connector.Kind == DiagramConnectorKind.SectionToReason && connector.FromNode == sectionNode && connector.ToNode == stepNode))
                        {
                            connectors.Add(new DiagramConnectorViewModel
                            {
                                Id = "connector_s_o_" + sectionNode.Id + "_" + stepNode.Id,
                                FromNode = sectionNode,
                                ToNode = stepNode,
                                Kind = DiagramConnectorKind.SectionToReason,
                                Confidence = step.Confidence
                            });
                        }

                        if (isNewSection)
                        {
                            stepY += ReasonHeight + ReasonGap;
                            sectionBottom = Math.Max(sectionBottom, stepY - ReasonGap);
                        }
                    }

                    blockBottom = Math.Max(blockBottom, sectionBottom);
                    sectionY = blockBottom + SectionGap;
                }

                currentBlockY = blockBottom + TransactionBlockGap;
            }

            AlignSharedSections(sectionsByIdentity.Values, connectors);
            AlignVoucherToTransactionMiddle(voucherNode, transactionNodes);
        }

        private static ReasonDiagramNodeViewModel CreateReasonNode(PostingTraceStep step, int index, double y)
        {
            return new ReasonDiagramNodeViewModel
            {
                Id = "step_" + index,
                Kind = DiagramNodeKind.Reason,
                Title = PostingTraceTextFormatter.Area(step.Area),
                Subtitle = PostingTraceTextFormatter.Reason(step.Reason),
                Detail = PostingTraceTextFormatter.Explanation(step.Explanation),
                IsManual = step.IsManual,
                SourceTable = step.SourceTable,
                SourceField = step.SourceField,
                SourceTableKey = step.SourceTableKey,
                Confidence = step.Confidence,
                Area = step.Area,
                Reason = step.Reason,
                PostingTraceStep = step,
                //Action = step.Action,
                X = ReasonX,
                Y = y,
                Width = ReasonWidth,
                Height = ReasonHeight,
                MinWidth = 260,
                MinHeight = 50
            };
        }

        // The key deliberately excludes generated node IDs and layout.  It includes both the
        // displayed section content and the complete, order-independent outcome identity.
        private static string GetSectionIdentity(PostingTraceSection section, IEnumerable<PostingTraceStep> steps)
        {
            var outcomeIdentities = steps.Select(GetReasonIdentity).Distinct().OrderBy(identity => identity);
            return string.Join("|", new[]
            {
                "section",
                section.SortOrder.ToString(),
                section.Title ?? string.Empty,
                section.RangeText ?? string.Empty,
                section.Description ?? string.Empty,
                string.Join(";", outcomeIdentities)
            });
        }

        private static string GetReasonIdentity(PostingTraceStep step)
        {
            return string.Join("|", new[]
            {
                "reason",
                ((int)step.Area).ToString(),
                ((int)step.Reason).ToString(),
                PostingTraceTextFormatter.Area(step.Area) ?? string.Empty,
                PostingTraceTextFormatter.Reason(step.Reason) ?? string.Empty,
                PostingTraceTextFormatter.Explanation(step.Explanation) ?? string.Empty,
                step.SourceTable != null ? step.SourceTable.FullName ?? step.SourceTable.Name : string.Empty,
                step.SourceTableKey ?? string.Empty,
                step.SourceField ?? string.Empty
            });
        }

        private static void AlignSharedSections(IEnumerable<SectionDiagramNodeViewModel> sections, IEnumerable<DiagramConnectorViewModel> connectors)
        {
            foreach (var section in sections)
            {
                var transactions = connectors
                    .Where(connector => connector.Kind == DiagramConnectorKind.TransactionToSection && connector.ToNode == section)
                    .Select(connector => connector.FromNode)
                    .ToList();

                if (transactions.Count > 1)
                    section.Y = transactions.Average(transaction => transaction.MiddleY) - (section.Height / 2.0);
            }
        }

        private static VoucherDiagramNodeViewModel GetNewVoucherViewModel(IList<PostingTraceResult> results)
        {
            var voucherNode = new VoucherDiagramNodeViewModel
            {
                Id = "voucher",
                Kind = DiagramNodeKind.Voucher,
                //Title = GetVoucher(validResults) ?? "(unknown)",
                //VoucherText = GetVoucher(validResults) ?? "(unknown)",
                //Subtitle = GetVoucherSubtitle(validResults),
                //VoucherSubtitle = GetVoucherSubtitle(validResults),
                //Detail = validResults.Count + " financial posting(s) on this voucher",
                PostingCountText = results.Count + Uniconta.ClientTools.Localization.lookup("GLTraceFinancialPostingSVoucher4"),
                X = VoucherX,
                Y = Margin,
                Width = VoucherWidth,
                Height = VoucherHeight,
                MinWidth = 180,
                MinHeight = 70
            };


            string voucher = null;
            DateTime? minDate = null;
            DateTime? maxDate = null;

            foreach (var result in results)
            {
                if (result == null || result.GLTrans == null)
                    continue;

                if (string.IsNullOrWhiteSpace(voucher))
                {
                    voucher = Convert.ToString(GetPropertyValue(result.GLTrans, "Voucher"));
                }

                if(string.IsNullOrEmpty(voucherNode.DCAccount)
                && !string.IsNullOrEmpty(result.GLTrans.DCAccount))
                {
                    voucherNode.DCAccount = result.GLTrans.DCAccount;
                    voucherNode.DCType = result.GLTrans._DCType;

                    if(result.GLTrans._DCType == Uniconta.DataModel.GLTransRefType.Debtor)
                    {
                        voucherNode.DCName = result.GLTrans.RefDebtor?.Name;
                    }
                    else
                    if (result.GLTrans._DCType == Uniconta.DataModel.GLTransRefType.Creditor)
                    {
                        voucherNode.DCName = result.GLTrans.RefCreditor?.Name;
                    }
                }

                var dateValue = GetPropertyValue(result.GLTrans, "Date");
                if (dateValue != null)
                {
                    DateTime date;

                    if (dateValue is DateTime)
                    {
                        date = (DateTime)dateValue;
                    }
                    else if (!DateTime.TryParse(Convert.ToString(dateValue), out date))
                    {
                        continue;
                    }

                    if (minDate == null || date.Date < minDate.Value.Date)
                        minDate = date.Date;

                    if (maxDate == null || date.Date > maxDate.Value.Date)
                        maxDate = date.Date;
                }
            }

            if (!string.IsNullOrWhiteSpace(voucher))
            {

                if (minDate == null)
                {
                    voucherNode.Title = voucher;
                }
                else
                if (minDate.Value.Date == maxDate.Value.Date)
                {
                    voucherNode.Title = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceVoucherDate"), voucher, minDate.Value);
                }
                else
                {
                    voucherNode.Title = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceVoucherDateRange"), voucher, minDate.Value, maxDate.Value);
                }
            }

            return voucherNode;
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

        private static ResolutionConfidence HighestConfidence(IEnumerable<PostingTraceStep> steps)
        {
            if (steps == null)
                return ResolutionConfidence.Unknown;

            return steps
                .Where(o => o != null)
                .Select(o => o.Confidence)
                .DefaultIfEmpty(ResolutionConfidence.Unknown)
                .Max();
        }

        private static void AlignVoucherToTransactionMiddle(
            DiagramNodeViewModel voucherNode,
            IList<DiagramNodeViewModel> transactionNodes)
        {
            if (voucherNode == null || transactionNodes == null || transactionNodes.Count == 0)
                return;

            var minY = transactionNodes.Min(n => n.Y);
            var maxY = transactionNodes.Max(n => n.Y + n.Height);
            var center = minY + ((maxY - minY) / 2.0);

            voucherNode.Y = center - (voucherNode.Height / 2.0);
        }

        //private static string BuildReasonDetail(PostingTraceStep step)
        //{
        //    var parts = new List<string>();
        //
        //    if (!string.IsNullOrWhiteSpace(step.MatchingAccount))
        //        parts.Add("Account: " + step.MatchingAccount);
        //
        //    if (!string.IsNullOrWhiteSpace(step.AccountName))
        //        parts.Add("Name: " + step.AccountName);
        //
        //    if (!string.IsNullOrWhiteSpace(step.AccountType))
        //        parts.Add("Type: " + step.AccountType);
        //
        //    parts.Add("Match quality: " + PostingTraceTextFormatter.Confidence(step.Confidence));
        //
        //    if (step.SourceTable != null)
        //        parts.Add("Setup area: " + PostingTraceTextFormatter.SetupArea(step.SourceTable.Name));
        //
        //    if (!string.IsNullOrWhiteSpace(step.SourceField))
        //        parts.Add("Setup field: " + step.SourceField);
        //
        //    if (!string.IsNullOrWhiteSpace(step.SourceTableKey))
        //        parts.Add("Setup record: " + step.SourceTableKey);
        //
        //    if (!string.IsNullOrWhiteSpace(step.Explanation))
        //        parts.Add(PostingTraceTextFormatter.Explanation(step.Explanation));
        //
        //    return string.Join(Environment.NewLine, parts);
        //}

        private static PostingTraceSection GetSection(PostingTraceArea mainPath)
        {
            switch (mainPath)
            {
                case PostingTraceArea.ManualJournalLineEntry:
                    return new PostingTraceSection(
                                      0,
                                      Uniconta.ClientTools.Localization.lookup("Manual"),
                                      Uniconta.ClientTools.Localization.lookup("GLTraceManualAccountEntry"),
                                      Uniconta.ClientTools.Localization.lookup("GLTraceNoOriginManualEntry"));

                case PostingTraceArea.ExplicitInvoiceOrderLinePostingAccount:
                case PostingTraceArea.OrderCustomerVendorPostingAccountOverride:
                case PostingTraceArea.ItemGroupAccountByVatZone:
                case PostingTraceArea.DebtorCreditorGroupPostingProfile:
                case PostingTraceArea.DebtorCreditorGroupRevenueAccountByVatZone:
                    return new PostingTraceSection(
                        1,
                        Uniconta.ClientTools.Localization.lookup("GLTraceSalesExpenseSource"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceInvoiceOrderSetup"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceWhereSalesPurchaseExpenseAccount"));

                case PostingTraceArea.DebtorCostOfGoodsSoldAndInventoryOffset:
                case PostingTraceArea.ScrapProfitLossAdjustment:
                case PostingTraceArea.PurchaseProjectStockConsumption:
                case PostingTraceArea.FixedCostPurchaseProfitLoss:
                    return new PostingTraceSection(
                        2,
                        Uniconta.ClientTools.Localization.lookup("GLTraceStockCostPostings"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceInventorySetup"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceAccountsUsedCostGoodsInventory"));

                case PostingTraceArea.VatAccountFollowsPostingAccount:
                case PostingTraceArea.UnrealizedVatAccount:
                case PostingTraceArea.StandardVatAccountAndRate2VatAccount:
                case PostingTraceArea.VatOffsetAccount:
                case PostingTraceArea.VatRoundingAccount:
                case PostingTraceArea.VatOnGroupSetup:
                    return new PostingTraceSection(
                        3,
                        Uniconta.ClientTools.Localization.lookup("GLTraceVATPostings"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceVATSetup"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceVATAccountsOffsetsRoundingAccounts"));

                case PostingTraceArea.LineDiscountAccounts:
                case PostingTraceArea.EndDiscountAccountAndRoundingFallback:
                case PostingTraceArea.PurchaseCharges:
                case PostingTraceArea.Duties:
                case PostingTraceArea.ProjectWipAndReverseWip:
                case PostingTraceArea.EnteredInvoiceAmountPennyDifference:
                case PostingTraceArea.WithholdingTax:
                case PostingTraceArea.PaymentAndSettlementAccounts:
                case PostingTraceArea.MiscCharges:
                    return new PostingTraceSection(
                        4,
                        Uniconta.ClientTools.Localization.lookup("GLTraceAdjustmentsSettlement"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceInvoicePaymentSetup"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceDiscountsChargesProjectWIPWithholding"));

                default:
                    return new PostingTraceSection(
                        99,
                        Uniconta.ClientTools.Localization.lookup("GLTraceUnmappedSource"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceNeedsReview"),
                        Uniconta.ClientTools.Localization.lookup("GLTraceNoConfiguredBusinessSetupAccount"));
            }
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
