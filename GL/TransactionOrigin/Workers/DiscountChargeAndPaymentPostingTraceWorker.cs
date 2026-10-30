using DevExpress.Xpf.Core;
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
    public class DiscountChargeAndPaymentPostingTraceWorker : InvoicePostingTraceResolverWorker
    {
        public override void AddTraceSteps(IPostingTraceContext context, PostingTraceResult result)
        {
            ResolveDiscountRoundingChargePaymentSpecialPaths(context, result);
        }

        #region 15-22 Discounts, rounding, charges, payments, special postings

        protected virtual void ResolveDiscountRoundingChargePaymentSpecialPaths(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            FindLineDiscountAccounts(context, result);
            FindEndDiscountOrRoundingFallbackAccount(context, result);
            FindPurchaseChargeAccounts(context, result);
            FindDutyAccounts(context, result);
            FindProjectWipAndReverseWipAccounts(context, result);
            FindEnteredInvoiceAmountPennyDifferenceAccount(context, result);
            FindWithholdingTaxAccounts(context, result);
            FindPaymentAndSettlementAccountsAccounts(context, result);
        }

        /// <summary>
        /// 15. Line discount accounts.
        ///
        /// Source:
        /// - DCGroup.DiscountAccount
        /// - DCGroup.DiscountAccountOffset
        /// </summary>
        protected virtual void FindLineDiscountAccounts(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.DCGroup != null)
            {
                var discountAccount = context.DCGroup._DiscountAccount;

                if (!string.IsNullOrEmpty(discountAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = discountAccount,
                        Reason = PostingAccountReason.LineDiscountAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = context.IsDebtor ? Uniconta.ClientTools.Localization.lookup("GLTraceDebtorGroupDiscountAccountExplain") :
                                                         Uniconta.ClientTools.Localization.lookup("GLTraceCreditorGroupDiscountAccountExplain"),
                        SourceTable = context.IsDebtor ? typeof(DebtorGroupClient) : typeof(CreditorGroupClient),
                        SourceTableKey = context.DCGroup._Group,
                        SourceField = "DiscountAccount"

                    });
                }

                var discountOffsetAccount = context.DCGroup._DiscountAccountOffset;

                if (!string.IsNullOrEmpty(discountOffsetAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = discountOffsetAccount,
                        Reason = PostingAccountReason.LineDiscountOffsetAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = context.IsDebtor ? Uniconta.ClientTools.Localization.lookup("GLTraceDebtorGroupDiscountOffsetAccount") :
                                                         Uniconta.ClientTools.Localization.lookup("GLTraceCreditorGroupDiscountOffsetAccount"),
                        SourceTable = context.IsDebtor ? typeof(DebtorGroupClient) : typeof(CreditorGroupClient),
                        SourceTableKey = context.DCGroup._Group,
                        SourceField = "DiscountAccountOffset",

                    });
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.LineDiscountAccounts,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountLineDiscountLine"));
        }
        /// <summary>
        /// 16. End-discount account and rounding fallback.
        ///
        /// Source:
        /// - DCGroup.EndDiscountAccount
        /// - existing sales/discount booking adjusted instead of new account
        /// </summary>
        protected virtual void FindEndDiscountOrRoundingFallbackAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.DCGroup != null)
            {
                var endDiscountAccount = context.DCGroup._EndDiscountAccount;

                if (!string.IsNullOrEmpty(endDiscountAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = endDiscountAccount,
                        Reason = PostingAccountReason.EndDiscountOwnAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = context.IsDebtor ? Uniconta.ClientTools.Localization.lookup("GLTraceDebtorGroupEndDiscountAccount") :
                                                         Uniconta.ClientTools.Localization.lookup("GLTraceCreditorGroupEndDiscountAccount"),
                        SourceTable = context.IsDebtor ? typeof(DebtorGroupClient) : typeof(CreditorGroupClient),
                        SourceTableKey = context.DCGroup._Group,
                        SourceField = "EndDiscountAccount"
                    });
                }
            }



            if (!string.IsNullOrEmpty(context.SystemErrorAccount))
            {
                candidates.Add(new PostingAccountCandidate
                {
                    Account = context.SystemErrorAccount,
                    Reason = PostingAccountReason.EndDiscountErrorAccountFallback,
                    Confidence = ResolutionConfidence.Low,
                    Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceSystemErrorAccountExplainPosted"),
                    SourceTable = typeof(GLAccountClient),
                    SourceTableKey = context.SystemErrorAccount,
                    SourceField = "ErrorAccount"
                });
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.EndDiscountAccountAndRoundingFallback,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountEndDiscountAccount"));
        }

        /// <summary>
        /// 17. Purchase charges.
        ///
        /// Source:
        /// - CreditorOrderCost.Account
        /// - creditor total adjustment when CreditorOrderCost._PostOnCreditor is used
        ///
        /// Notes:
        /// - Distributed additions to target line accounts are intentionally not resolved here yet.
        /// - Project add-on cost/inventory postings are intentionally not resolved here yet.
        /// </summary>
        protected virtual void FindPurchaseChargeAccounts(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (context.DCType != GLTransRefType.Creditor)
                return;

            var candidates = new List<PostingAccountCandidate>();

            if (context.PurchaseChargeCosts != null)
            {
                foreach (var cost in context.PurchaseChargeCosts.Where(c => c != null))
                {
                    if (cost._PostOnCreditor)
                    {
                        if (context.DCGroup == null)
                            continue;

                        if (string.IsNullOrEmpty(context.DCGroup._SummeryAccount))
                            continue;

                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = context.DCGroup._SummeryAccount,
                            Reason = PostingAccountReason.PurchaseChargePostedOnCreditorTotal,
                            Confidence = ResolutionConfidence.Low,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTracePurchaseChargeSetupConfiguredPost"),
                            SourceTable = typeof(CreditorOrderCostClient),
                            SourceTableKey = cost.KeyStr,
                            SourceField = "PostOnCreditor"
                        });

                        continue;
                    }

                    if (string.IsNullOrEmpty(cost._Account))
                        continue;

                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = cost._Account,
                        Reason = PostingAccountReason.PurchaseChargeSetupAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = Uniconta.ClientTools.Localization.lookup("GLTracePurchaseChargeSetupAccountExplain"),
                        SourceTable = typeof(CreditorOrderCostClient),
                        SourceTableKey = cost.KeyStr,
                        SourceField = "Account"
                    });
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.PurchaseCharges,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountPurchaseChargeSetup"));
        }

        /// <summary>
        /// 18. Duties.
        ///
        /// Source:
        /// - Duty account
        /// - Duty offset account
        ///
        /// Notes:
        /// - Generated duty item lines are intentionally not resolved here yet.
        ///   They go through the normal UpdateTran account cascade, so resolving them
        ///   here without a concrete generated-line-to-duty relation would duplicate
        ///   paths 1-5.
        /// </summary>
        protected virtual void FindDutyAccounts(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.InvDuties != null)
            {
                foreach (var duty in context.InvDuties.Where(d => d != null))
                {
                    if (!string.IsNullOrEmpty(duty._Account))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = duty._Account,
                            Reason = PostingAccountReason.DutyAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceDutySetupAccountExplainPosted"),
                            SourceTable = typeof(InvDutyGroupClient),
                            SourceTableKey = duty.DutyGroup,
                            SourceField = "Account"
                        });
                    }

                    if (!string.IsNullOrEmpty(duty._OffsetAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = duty._OffsetAccount,
                            Reason = PostingAccountReason.DutyOffsetAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceDutySetupOffsetAccountExplain"),
                            SourceTable = typeof(InvDutyGroupClient),
                            SourceTableKey = duty.DutyGroup,
                            SourceField = "OffsetAccount"
                        });
                    }
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.Duties,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountDutySetup"));
        }

        /// <summary>
        /// 19. Project WIP and reverse WIP.
        ///
        /// Source:
        /// - ProjectGroup.WorkInProgress
        /// - ProjectGroup.WorkInProgressOffset
        /// - ProjectGroup.Materials
        /// - ProjectGroup.MaterialsOffset
        /// - ProjectGroup.Outlay
        /// - ProjectGroup.OutlayOffset
        /// - ProjectGroup.Adjustment
        /// - ProjectGroup.AdjustmentOffset
        /// - ProjectGroup.ReversedWIP
        /// - ProjectGroup.ReversedWIPOffset
        /// - ProjectGroup.InvoiceAdjustment
        /// - ProjectGroup.InvoiceAdjustmentOffset
        /// </summary>
        protected virtual void FindProjectWipAndReverseWipAccounts(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.ProjectGroupsForPosting != null)
            {
                foreach (var projectGroup in context.ProjectGroupsForPosting.Where(g => g != null))
                {
                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._WorkInProgress,
                        PostingAccountReason.ProjectWorkInProgressAccount,
                        "WorkInProgress",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupWorkProgressAccount"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._WorkInProgressOffset,
                        PostingAccountReason.ProjectWorkInProgressOffsetAccount,
                        "WorkInProgressOffset",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupWorkProgressOffset"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._Materials,
                        PostingAccountReason.ProjectMaterialsAccount,
                        "Materials",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupMaterialsAccountExplain"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._MaterialsOffset,
                        PostingAccountReason.ProjectMaterialsOffsetAccount,
                        "MaterialsOffset",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupMaterialsOffsetAccount"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._Outlay,
                        PostingAccountReason.ProjectOutlayAccount,
                        "Outlay",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupOutlayAccountExplain"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._OutlayOffset,
                        PostingAccountReason.ProjectOutlayOffsetAccount,
                        "OutlayOffset",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupOutlayOffsetAccount"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._Adjustment,
                        PostingAccountReason.ProjectAdjustmentAccount,
                        "Adjustment",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupAdjustmentAccountExplain"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._AdjustmentOffset,
                        PostingAccountReason.ProjectAdjustmentOffsetAccount,
                        "AdjustmentOffset",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupAdjustmentOffsetAccount"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._ReversedWIP,
                        PostingAccountReason.ProjectReversedWipAccount,
                        "ReversedWIP",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupReversedWIPAccount"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._ReversedWIPOffset,
                        PostingAccountReason.ProjectReversedWipOffsetAccount,
                        "ReversedWIPOffset",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupReversedWIPOffset"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._InvoiceAdjustment,
                        PostingAccountReason.ProjectInvoiceAdjustmentAccount,
                        "InvoiceAdjustment",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupInvoiceAdjustmentAccount"),
                        projectGroup);

                    AddProjectGroupAccountCandidate(
                        candidates,
                        projectGroup._InvoiceAdjustmentOffset,
                        PostingAccountReason.ProjectInvoiceAdjustmentOffsetAccount,
                        "InvoiceAdjustmentOffset",
                        Uniconta.ClientTools.Localization.lookup("GLTraceProjectGroupInvoiceAdjustmentOffset"),
                        projectGroup);
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.ProjectWipAndReverseWip,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountProjectWIPReverse"));
        }

        protected virtual void AddProjectGroupAccountCandidate(List<PostingAccountCandidate> candidates,
                                                               string account,
                                                               PostingAccountReason reason,
                                                               string sourceField,
                                                               string explanation,
                                                               ProjectGroupClient projectGroup)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));

            if (projectGroup == null)
                return;

            if (string.IsNullOrEmpty(account))
                return;

            candidates.Add(new PostingAccountCandidate
            {
                Account = account,
                Reason = reason,
                Confidence = ResolutionConfidence.Medium,
                Explanation = explanation,

                SourceTable = typeof(ProjectGroupClient),
                SourceTableKey = projectGroup.KeyStr,
                SourceField = sourceField
            });
        }

        /// <summary>
        /// 20. Entered invoice amount penny difference.
        ///
        /// Source:
        /// - system PennyDif
        /// </summary>
        protected virtual void FindEnteredInvoiceAmountPennyDifferenceAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {

            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (!string.IsNullOrEmpty(context.SystemPennyDifferenceAccount))
            {
                RecordPossiblePostingSource(
                    context,
                    result,
                    new PostingAccountCandidate
                    {
                        Account = context.SystemPennyDifferenceAccount,
                        Reason = PostingAccountReason.EndDiscountPennyDifFallback,
                        Confidence = ResolutionConfidence.Low,
                        Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceSystemPennyDifferenceAccountExplain"),
                        SourceTable = typeof(GLAccountClient),
                        SourceTableKey = context.SystemPennyDifferenceAccount,
                        SourceField = "PennyDif"
                    },
                    PostingTraceArea.VatRoundingAccount,
                    PostingAccountReason.EndDiscountPennyDifFallback,
                    ResolutionConfidence.Medium,
                    Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountVATRounding"));
            }
        }

        /// <summary>
        /// 21. Withholding tax.
        ///
        /// Source:
        /// - system WithholdingSales
        /// - system WithholdingBuy
        ///
        /// Notes:
        /// - This is intentionally separate from VAT paths 10-14.
        ///   VAT paths use GLVat setup accounts. Withholding uses system accounts.
        /// </summary>
        protected virtual void FindWithholdingTaxAccounts(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.IsDebtor)
            {
                if (!string.IsNullOrEmpty(context.SystemWithholdingSalesAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = context.SystemWithholdingSalesAccount,
                        Reason = PostingAccountReason.WithholdingSalesAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceSystemWithholdingSalesAccountExplain"),
                        SourceTable = typeof(GLAccountClient),
                        SourceTableKey = context.SystemWithholdingSalesAccount,
                        SourceField = "WithholdingSales"
                    });
                }
            }
            else if (context.DCType == GLTransRefType.Creditor)
            {
                if (!string.IsNullOrEmpty(context.SystemWithholdingBuyAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = context.SystemWithholdingBuyAccount,
                        Reason = PostingAccountReason.WithholdingBuyAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceSystemWithholdingBuyAccountExplain"),
                        SourceTable = typeof(GLAccountClient),
                        SourceTableKey = context.SystemWithholdingBuyAccount,
                        SourceField = "WithholdingBuy"
                    });
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.WithholdingTax,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountWithholdingTaxSystem"));
        }

        /// <summary>
        /// 22. Payment and settlement accounts.
        ///
        /// Source:
        /// - explicit API payment account
        /// - PaymentTerm.Account for net-cash payment
        /// - PaymentTerm.DebtorAccount related group summary account for credit-card terms
        /// - DCGroup.SummeryAccount
        /// - debtor group summary account through creditor D2CAccount path
        /// - PaymentTerm.Account / PaymentTerm.OffsetAccount for prepayment
        /// </summary>
        protected virtual void FindPaymentAndSettlementAccountsAccounts(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (!string.IsNullOrEmpty(context.Invoice?.Payment)
            &&  context.PaymentTerms?.FirstOrDefault(p => p.KeyStr == context.Invoice.Payment) is PaymentTermClient paymentTermClient
            && paymentTermClient._PaymentMethod == PaymentMethodTypes.NetCash)
            {
                RecordPossiblePostingSource(
                    context,
                    result,
                    new PostingAccountCandidate
                    {
                        Account = paymentTermClient.Account,
                        Reason = PostingAccountReason.CashPayment,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = context.IsDebtor ? Uniconta.ClientTools.Localization.lookup("GLTracePaymentTermCashAccountExplain") :
                                                         Uniconta.ClientTools.Localization.lookup("GLTracePaymentTermCashAccountExplain2"),
                        SourceTable = typeof(PaymentTermClient),
                        SourceTableKey = context.Invoice?.Payment,
                        SourceField = nameof(PaymentTermClient.Account)
                    },

                    PostingTraceArea.PaymentAndSettlementAccounts,
                    PostingAccountReason.CashPayment,
                    ResolutionConfidence.Medium,
                    Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountPaymentTermType"));
            }

            if (context.DCGroup != null)
            {
                RecordPossiblePostingSource(
                    context,
                    result,
                    new PostingAccountCandidate
                    {
                        Account = context.DCGroup._SummeryAccount,
                        Reason = PostingAccountReason.DebtorCreditorGroupSummaryAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = context.IsDebtor ? Uniconta.ClientTools.Localization.lookup("GLTraceDebtorGroupSummaryAccountExplain") :
                                                         Uniconta.ClientTools.Localization.lookup("GLTraceCreditorGroupSummaryAccountExplain"),
                        SourceTable = context.IsDebtor ? typeof(DebtorGroupClient) : typeof(CreditorGroupClient),
                        SourceTableKey = context.DCGroup._Group,
                        SourceField = nameof(DCGroup._SummeryAccount)
                    },
                    PostingTraceArea.PaymentAndSettlementAccounts,
                    PostingAccountReason.DebtorCreditorGroupSummaryAccount,
                    ResolutionConfidence.Medium,
                    Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountPaymentCashSummary"));
            }
        }

        #endregion

    }
}
