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
    public class InventoryAndCostPostingTraceWorker : InvoicePostingTraceResolverWorker
    {
        public override void AddTraceSteps(IPostingTraceContext context, PostingTraceResult result)
        {
            TraceInventoryAndCostPostingSources(context, result);
        }

        #region 6-9 Item cost / inventory accounts

        protected virtual void TraceInventoryAndCostPostingSources(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            FindCostOfGoodsSoldAndInventoryAccount(context, result);

            FindScrapAdjustmentAccount(context, result);
            FindPurchaseProjectStockConsumptionAccount(context, result);
            FindFixedCostPurchaseProfitLossAccount(context, result);
        }

        /// <summary>
        /// 6. Debtor cost of goods sold and inventory offset.
        ///
        /// Source:
        /// - DCGroupPosting cost account, falling back to InvGroup.CostAccount
        /// - InvGroup.InvAccount
        /// </summary>
        protected virtual void FindCostOfGoodsSoldAndInventoryAccount(IPostingTraceContext context, PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            var itemGroups = context.InvGroupsForPosting?.Where(i => i != null && i.HasItemGroup).ToList();

            if (itemGroups != null)
            {
                // First usfind DCGroupPosting overrides.
                AddCostOfGoodsSoldCandidates(itemGroups.Where(i => !i.IsOnItemLevelOnly), candidates, itemLevelOnly: false);

                // Only fall back to the original item-level group when the override
                // resolution produced no usable account.
                if (candidates.Count == 0)
                {
                    AddCostOfGoodsSoldCandidates(itemGroups.Where(i => i.IsOnItemLevelOnly), candidates, itemLevelOnly: true);
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.DebtorCostOfGoodsSoldAndInventoryOffset,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountDebtorCostGoods"));
        }

        private static void AddCostOfGoodsSoldCandidates(IEnumerable<GroupPostingWrapper> itemGroups, ICollection<PostingAccountCandidate> candidates, bool itemLevelOnly)
        {
            foreach (var invGroup in itemGroups)
            {
                var vatZoneAccount = invGroup.NonDomesticAccount(cost: true);

                if (!string.IsNullOrEmpty(vatZoneAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = vatZoneAccount,
                        Reason = invGroup.VatZoneReason(cost: true),
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceItemGroupZoneCostMatch"), invGroup.VatZone),
                        SourceTable = invGroup.SourceTableType,
                        SourceTableKey = invGroup.SourceTableKey,
                        SourceField = invGroup.VatZoneAccountFieldName(cost: true)
                    });

                    continue;
                }

                var baseAccount = invGroup.DomesticAccount(cost: true);

                if (!string.IsNullOrEmpty(baseAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = baseAccount,
                        Reason = invGroup.BaseReason(cost: true),
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = invGroup.IsDomesticVat ? Uniconta.ClientTools.Localization.lookup("GLTraceEligibleItemGroupDomesticCost") :
                                                               Uniconta.ClientTools.Localization.lookup("GLTraceEligibleItemGroupBaseCost"),
                        SourceTable = invGroup.SourceTableType,
                        SourceTableKey = invGroup.SourceTableKey,
                        SourceField = invGroup.BaseAccountFieldName(cost: true)
                    });
                }
            }
        }

        /// <summary>
        /// 7. Scrap profit/loss adjustment.
        ///
        /// Source:
        /// - InvGroup.InvAccount
        /// - InvGroup.LossProfit
        /// </summary>
        /// <summary>
        /// 7. Scrap profit/loss adjustment.
        ///
        /// Source:
        /// - InvGroup.InvAccount
        /// - InvGroup.LossProfit
        /// </summary>
        protected virtual void FindScrapAdjustmentAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.InvGroupsForPosting != null)
            {
                foreach (var invGroup in context.InvGroupsForPosting.Where(i => i.HasItemGroup && (!i.HasDCGroup || i.DCGroup == context.DCGroup.KeyStr)))
                {
                    if (invGroup == null)
                        continue;

                    var inventoryAccount = invGroup.InventoryAccount;

                    if (!string.IsNullOrEmpty(invGroup.InventoryAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = invGroup.InventoryAccount,
                            Reason = PostingAccountReason.ScrapInventoryReversalAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceEligibleItemGroupInventoryAccount"),
                            SourceTable = invGroup.SourceTableType,
                            SourceTableKey = invGroup.SourceTableKey,
                            SourceField = invGroup.InventoryAccountFieldName()
                        });
                    }

                    if (!string.IsNullOrEmpty(invGroup.LossProfitAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = invGroup.LossProfitAccount,
                            Reason = PostingAccountReason.ScrapLossProfitAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceEligibleItemGroupLossProfit"),
                            SourceTable = invGroup.SourceTableType,
                            SourceTableKey = invGroup.SourceTableKey,
                            SourceField = invGroup.LossProfitAccountFieldName()
                        });
                    }
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.ScrapProfitLossAdjustment,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountScrapProfitLoss"));
        }

        /// <summary>
        /// 8. Purchase project stock consumption.
        ///
        /// Source:
        /// - InvGroup.CostAccount
        /// - InvGroup.InvAccount
        /// </summary>
        protected virtual void FindPurchaseProjectStockConsumptionAccount(IPostingTraceContext context, PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.PurchaseProjectStockConsumptionGroups != null)
            {
                foreach (var invGroup in context.PurchaseProjectStockConsumptionGroups.Where(i => i != null && i.HasItemGroup && !i.HasDCGroup))
                {
                    var costAccount = invGroup.CostAccount;

                    if (!string.IsNullOrEmpty(costAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = costAccount,
                            Reason = PostingAccountReason.PurchaseProjectCostAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceInventoryGroupCostAccountExplain"),
                            SourceTable = invGroup.SourceTableType,
                            SourceTableKey = invGroup.SourceTableKey,
                            SourceField = "CostAccount"
                        });
                    }

                    var inventoryAccount = invGroup.InventoryAccount;

                    if (!string.IsNullOrEmpty(inventoryAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = inventoryAccount,
                            Reason = PostingAccountReason.PurchaseProjectInventoryAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceInventoryGroupInventoryAccountExplain"),
                            SourceTable = invGroup.SourceTableType,
                            SourceTableKey = invGroup.SourceTableKey,
                            SourceField = invGroup.InventoryAccountFieldName()
                        });
                    }
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.PurchaseProjectStockConsumption,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountPurchaseProjectStock"));
        }

        /// <summary>
        /// 9. Fixed-cost purchase profit/loss.
        ///
        /// Source:
        /// - current revenue/expense account
        /// - InvGroup.ProfitLossPurchase
        /// </summary>
        protected virtual void FindFixedCostPurchaseProfitLossAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.FixedCostPurchaseProfitLossGroups != null)
            {
                foreach (var invGroup in context.FixedCostPurchaseProfitLossGroups.Where(i => i != null && i.HasItemGroup && !i.HasDCGroup))
                {
                    var currentLineAccount = invGroup.DomesticAccount();

                    if (!string.IsNullOrEmpty(currentLineAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = currentLineAccount,
                            Reason = PostingAccountReason.FixedCostPurchaseCurrentRevenueExpenseAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceCurrentPurchaseLineAccountExplain"),
                            SourceTable = invGroup.SourceTableType,
                            SourceTableKey = invGroup.SourceTableKey,
                            SourceField = invGroup.BaseAccountFieldName()
                        });
                    }

                    var profitLossPurchaseAccount = invGroup.ProfitLossPurchaseAccount;

                    if (!string.IsNullOrEmpty(profitLossPurchaseAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = profitLossPurchaseAccount,
                            Reason = PostingAccountReason.FixedCostPurchaseProfitLossPurchaseAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceInventoryGroupFixedCostPurchase"),
                            SourceTable = invGroup.SourceTableType,
                            SourceTableKey = invGroup.SourceTableKey,
                            SourceField = invGroup.ProfitLossPurchaseAccountFieldName()
                        });
                    }
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.FixedCostPurchaseProfitLoss,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountFixedCostPurchase"));
        }
        #endregion

    }
}
