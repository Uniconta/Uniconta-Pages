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
    public class RevenueAndExpensePostingTraceWorker : InvoicePostingTraceResolverWorker
    {
        public override void AddTraceSteps(IPostingTraceContext context, PostingTraceResult result)
        {
            TraceRevenueAndExpensePostingSources(context, result);
        }

        #region 1-5 Revenue / expense line account paths

        protected virtual void TraceRevenueAndExpensePostingSources(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            FindLineSpecifiedPostingAccount(context, result);
            FindOrderOrBusinessPartnerOverrideAccount(context, result);

            FindItemGroupRevenueOrExpenseAccount(context, result);
            FindCustomerOrVendorPostingProfileAccount(context, result);

            FindCustomerOrVendorGroupRevenueAccount(context, result);
        }

        public PostingAccountCandidate GetExplicitLinePostingAccount(IPostingTraceContext context)
        {
            // 1. Find the invoice/order/project line that produced this GLTrans.
            // 2. Check whether that line had an explicit posting account.
            // 3. If yes, return it as a candidate.
            // 4. If not, return null.

            foreach (var line in context.InvoiceLines)
            {
                if (string.IsNullOrEmpty(line.PostingAccount)) // TODO: is this the manual account?
                    continue;

                return new PostingAccountCandidate
                {
                    Account = line.PostingAccount,
                    IsManual = true,
                    Reason = PostingAccountReason.LineRevenueAccount,
                    Confidence = ResolutionConfidence.High,
                    Explanation = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceLineAccountMatch"), context.IsDebtor ? Uniconta.ClientTools.Localization.lookup("Revenue") : Uniconta.ClientTools.Localization.lookup("Cost")),
                    SourceTable = typeof(InvTransInvoice),// line.GetType()?.Name,
                    SourceTableKey = line.SourceLineId,
                    SourceField = context.IsDebtor ? "RevenueAccount" : "CostAccount"
                };
            }

            return null;
        }


        public PostingAccountCandidate GetOrderCustomerVendorPostingAccount(IPostingTraceContext context)
        {
            // 2. If no order posting account exists, check debtor/creditor posting account.
            // 3. If either exists and matches the posted GL account, return it as candidate.
            // 4. Otherwise return null.

            // Path 2:
            // Order/customer/vendor posting-account override.
            //
            // Posting logic:
            // DCOrder.PostingAccount ?? DCAccount.PostingAccount.
            //
            // Reverse trace:
            // If the posted GL account equals either of those configured accounts,
            // this path could explain the GLTrans.

            // TODO: the "order posting account"  dissapears when an invoice is posted. e.g. if an order is fully invoiced it is removed from the order list??
            if (context.Order != null && !string.IsNullOrEmpty(context.Order._PostingAccount))
            {
                return new PostingAccountCandidate
                {
                    Account = context.Order._PostingAccount,
                    IsManual = true,
                    Reason = PostingAccountReason.OrderPostingAccount,
                    Confidence = ResolutionConfidence.Medium,
                    Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceOrderPostingAccountOverrideExplain"),
                    SourceTable = context.Order.GetType(),
                    SourceTableKey = context.Order._OrderNumber.ToString(),
                    SourceField = "PostingAccount"
                };
            }

            if (context.DCAccount != null && !string.IsNullOrEmpty(context.DCAccount._PostingAccount))
            {
                return new PostingAccountCandidate
                {
                    Account = context.DCAccount._PostingAccount,
                    IsManual = true,
                    Reason = PostingAccountReason.OrderCustomerVendorPostingAccount,
                    Confidence = ResolutionConfidence.Medium,
                    Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceCustomerVendorPostingAccountOverride"),
                    SourceTable = context.DCAccount.GetType(),
                    SourceTableKey = context.DCAccount._Account,
                    SourceField = "PostingAccount",
                };

            }

            return null;
        }

        /// <summary>
        /// 1. Explicit invoice/order/project line posting account.
        ///
        /// Source:
        /// - InvTransExtended.RevenueAccount
        /// - post.PostingAccount
        /// - ProjectOnAccountInvoiceLine.Account copied into RevenueAccount
        /// </summary>
        protected virtual void FindLineSpecifiedPostingAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context.GLTrans._Origin == LedgerPostingType.BlankEntry
            && context.GLTrans.JournalPostedRef != null)
            {
                RecordPossiblePostingSource(
                                context,
                                result,
                                new PostingAccountCandidate
                                {
                                    Account = context.GLTrans.Account,
                                    IsManual = true,
                                    Reason = PostingAccountReason.ManualAccountEntry,
                                    Confidence = ResolutionConfidence.Medium,
                                    Explanation = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceManualJournalOrigin"), context.GLTrans.JournalPostedRef.JournalPostedId, context.GLTrans.JournalPostedRef.JournalRef?.Name),
                                    SourceTable = typeof(GLDailyJournalPostedClient),
                                    SourceTableKey = context.GLTrans.JourRowId.ToString(),
                                    SourceField = nameof(GLTransClient.Account)
                                },
                                PostingTraceArea.ExplicitInvoiceOrderLinePostingAccount,
                                PostingAccountReason.ManualAccountEntry,
                                ResolutionConfidence.High,
                                string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceManualJournalOrigin"), context.GLTrans.JournalPostedRef.JournalPostedId, context.GLTrans.JournalPostedRef.JournalRef?.Name));
            }


            RecordPossiblePostingSource(
                context,
                result,
                GetExplicitLinePostingAccount(context),
                PostingTraceArea.ExplicitInvoiceOrderLinePostingAccount,
                PostingAccountReason.LineRevenueAccount,
                ResolutionConfidence.High,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountExplicitLinePosting"));
        }

        /// <summary>
        /// 2. Order/customer/vendor posting-account override.
        ///
        /// Source:
        /// - DCOrder.PostingAccount
        /// - fallback DCAccount.PostingAccount
        /// </summary>
        protected virtual void FindOrderOrBusinessPartnerOverrideAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            RecordPossiblePostingSource(
                context,
                result,
                GetOrderCustomerVendorPostingAccount(context),
                PostingTraceArea.OrderCustomerVendorPostingAccountOverride,
                PostingAccountReason.OrderCustomerVendorPostingAccount,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountOrderCustomerVendor"));
        }

        /// <summary>
        /// 3. Item group account by VAT zone.
        ///
        /// Sales source:
        /// - InvGroup.RevenueAccount
        /// - InvGroup.RevenueAccount1-4
        ///
        /// Purchase source:
        /// - InvGroup.PurchaseAccount
        /// - InvGroup.PurchaseAccount1-4
        ///
        /// Fallback:
        /// - InvGroup._UseFirstIfBlank can fall back to base account.
        /// - An overridden item group is checked first.
        /// - If the override has no usable account, the original item-level group is used.
        /// </summary>
        protected virtual void FindItemGroupRevenueOrExpenseAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            var itemGroups = context.InvGroupsForPosting?
                .Where(i => i != null && i.HasItemGroup)
                .ToList();

            if (itemGroups != null)
            {
                // First inspect DCGroupPosting overrides and regular item groups.
                AddItemGroupRevenueOrExpenseCandidates(
                    itemGroups.Where(i => !i.IsOnItemLevelOnly),
                    candidates);

                // When the override contains no applicable account, fall back to the
                // original inventory group configured directly on the item.
                if (candidates.Count == 0)
                {
                    AddItemGroupRevenueOrExpenseCandidates(
                        itemGroups.Where(i => i.IsOnItemLevelOnly),
                        candidates);
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.ItemGroupAccountByVatZone,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountEligibleItemGroup"));
        }

        private static void AddItemGroupRevenueOrExpenseCandidates(
            IEnumerable<GroupPostingWrapper> itemGroups,
            ICollection<PostingAccountCandidate> candidates)
        {
            foreach (var invGroup in itemGroups)
            {
                var vatZoneAccount = invGroup.NonDomesticAccount();

                if (!string.IsNullOrEmpty(vatZoneAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = vatZoneAccount,
                        Reason = invGroup.VatZoneReason(),
                        Confidence = ResolutionConfidence.Medium,
                        Explanation =
                            string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceItemGroupVatZoneMatch"), invGroup.VatZone),
                        SourceTable = invGroup.SourceTableType,
                        SourceTableKey = invGroup.SourceTableKey,
                        SourceField = invGroup.VatZoneAccountFieldName()
                    });

                    continue;
                }

                var baseAccount = invGroup.DomesticAccount();

                if (!string.IsNullOrEmpty(baseAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = baseAccount,
                        Reason = invGroup.BaseReason(),
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = invGroup.IsDomesticVat
                            ? Uniconta.ClientTools.Localization.lookup("GLTraceEligibleItemGroupDomesticAccount")
                            : Uniconta.ClientTools.Localization.lookup("GLTraceEligibleItemGroupBaseAccount"),
                        SourceTable = invGroup.SourceTableType,
                        SourceTableKey = invGroup.SourceTableKey,
                        SourceField = invGroup.BaseAccountFieldName()
                    });
                }
            }
        }

        /// <summary>
        /// 4. Debtor/creditor group posting profile by item group/VAT zone.
        ///
        /// Source:
        /// - DCGroupPosting setup keyed by debtor/creditor group, item group and VAT zone.
        ///
        /// Possible account step:
        /// - Profile revenue/line account.
        ///
        /// Note:
        /// Profile cost account should probably be handled in the COGS/inventory path,
        /// not here, unless you intentionally want this path to report both.
        /// </summary>
        protected virtual void FindCustomerOrVendorPostingProfileAccount(IPostingTraceContext context,
                                                                    PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.InvGroupsForPosting != null)
            {
                foreach (var dcGroup in context.InvGroupsForPosting.Where(g => g.HasDCGroup && !g.HasItemGroup))
                {
                    if (dcGroup == null)
                        continue;

                    var vatZoneAccount = dcGroup.NonDomesticAccount();

                    if (!string.IsNullOrEmpty(vatZoneAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = vatZoneAccount,
                            Reason = dcGroup.VatZoneReason(),
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceGroupProfileVatZoneMatch"), dcGroup.VatZone),
                            SourceTable = dcGroup.SourceTableType,
                            SourceTableKey = dcGroup.SourceTableKey,
                            SourceField = dcGroup.VatZoneAccountFieldName(),
                        });

                        continue;
                    }

                    var baseAccount = dcGroup.DomesticAccount();

                    if (!string.IsNullOrEmpty(baseAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = baseAccount,
                            Reason = dcGroup.BaseReason(),
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = dcGroup.IsDomesticVat ? Uniconta.ClientTools.Localization.lookup("GLTraceEligibleDebtorCreditorGroupPosting") :
                                                                  Uniconta.ClientTools.Localization.lookup("GLTraceEligibleDebtorCreditorGroupPosting2"),
                            SourceTable = dcGroup.SourceTableType,
                            SourceTableKey = dcGroup.SourceTableKey,
                            SourceField = dcGroup.BaseAccountFieldName(),
                        });
                    }
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.DebtorCreditorGroupPostingProfile,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountDebtorCreditorGroup"));
        }

        /// <summary>
        /// 5. Debtor/creditor group revenue account by VAT zone.
        ///
        /// Source:
        /// - DCGroup.RevenueAccount
        /// - DCGroup.RevenueAccount1-4
        ///
        /// Fallback:
        /// - DCGroup._UseFirstIfBlank can fall back to base revenue account.
        /// </summary>
        protected virtual void FindCustomerOrVendorGroupRevenueAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            // No comprende senor

            //RecordPossiblePostingSources(
            //    context,
            //    result,
            //    _lookup.GetDebtorCreditorGroupRevenueVatZoneAccounts(context),
            //    PostingTraceArea.DebtorCreditorGroupRevenueAccountByVatZone,
            //    ResolutionConfidence.Medium,
            //    Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountDebtorCreditorGroup2"));
        }

        #endregion

    }
}
