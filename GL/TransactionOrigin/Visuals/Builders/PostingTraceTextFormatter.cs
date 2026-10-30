using System;
using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Visuals
{
    public static class PostingTraceTextFormatter
    {
        private static readonly IDictionary<PostingTraceArea, string> AreaLabels = new Dictionary<PostingTraceArea, string>
        {
            { PostingTraceArea.ExplicitInvoiceOrderLinePostingAccount, "GLTraceAreaExplicitInvoiceOrderLinePostingAccount" },
            { PostingTraceArea.OrderCustomerVendorPostingAccountOverride, "GLTraceAreaOrderCustomerVendorPostingAccountOverride" },
            { PostingTraceArea.ItemGroupAccountByVatZone, "GLTraceAreaItemGroupAccountByVatZone" },
            { PostingTraceArea.DebtorCreditorGroupPostingProfile, "GLTraceAreaDebtorCreditorGroupPostingProfile" },
            { PostingTraceArea.DebtorCreditorGroupRevenueAccountByVatZone, "GLTraceAreaDebtorCreditorGroupRevenueAccountByVatZone" },
            { PostingTraceArea.DebtorCostOfGoodsSoldAndInventoryOffset, "GLTraceAreaDebtorCostOfGoodsSoldAndInventoryOffset" },
            { PostingTraceArea.ScrapProfitLossAdjustment, "GLTraceAreaScrapProfitLossAdjustment" },
            { PostingTraceArea.PurchaseProjectStockConsumption, "GLTraceAreaPurchaseProjectStockConsumption" },
            { PostingTraceArea.FixedCostPurchaseProfitLoss, "GLTraceAreaFixedCostPurchaseProfitLoss" },
            { PostingTraceArea.VatAccountFollowsPostingAccount, "GLTraceAreaVatAccountFollowsPostingAccount" },
            { PostingTraceArea.UnrealizedVatAccount, "GLTraceAreaUnrealizedVatAccount" },
            { PostingTraceArea.StandardVatAccountAndRate2VatAccount, "GLTraceAreaStandardVatAccountAndRate2VatAccount" },
            { PostingTraceArea.VatOffsetAccount, "GLTraceAreaVatOffsetAccount" },
            { PostingTraceArea.VatRoundingAccount, "VatRounding" },
            { PostingTraceArea.LineDiscountAccounts, "LineDiscountPct" },
            { PostingTraceArea.EndDiscountAccountAndRoundingFallback, "GLTraceAreaEndDiscountAccountAndRoundingFallback" },
            { PostingTraceArea.PurchaseCharges, "GLTraceAreaPurchaseCharges" },
            { PostingTraceArea.Duties, "Duty" },
            { PostingTraceArea.ProjectWipAndReverseWip, "GLTraceAreaProjectWipAndReverseWip" },
            { PostingTraceArea.EnteredInvoiceAmountPennyDifference, "GLTraceAreaEnteredInvoiceAmountPennyDifference" },
            { PostingTraceArea.WithholdingTax, "Withholding" },
            { PostingTraceArea.PaymentAndSettlementAccounts, "GLTraceAreaPaymentAndSettlementAccounts" },
            { PostingTraceArea.Unknown, "GLTraceAreaUnknown" }
        };

        private static readonly IDictionary<PostingAccountReason, string> ReasonLabels = new Dictionary<PostingAccountReason, string>
        {
            { PostingAccountReason.LineRevenueAccount, "GLTraceReasonLineRevenueAccount" },
            { PostingAccountReason.ProjectOnAccountLineRevenueAccount, "GLTraceReasonProjectOnAccountLineRevenueAccount" },
            { PostingAccountReason.OrderLinePostingAccount, "GLTraceReasonOrderLinePostingAccount" },
            { PostingAccountReason.OrderPostingAccount, "GLTraceReasonOrderPostingAccount" },
            { PostingAccountReason.CustomerPostingAccount, "GLTraceReasonCustomerPostingAccount" },
            { PostingAccountReason.VendorPostingAccount, "GLTraceReasonVendorPostingAccount" },
            { PostingAccountReason.OrderCustomerVendorPostingAccount, "GLTraceReasonOrderCustomerVendorPostingAccount" },
            { PostingAccountReason.ItemGroupSalesRevenueBaseAccount, "GLTraceReasonItemGroupSalesRevenueBaseAccount" },
            { PostingAccountReason.ItemGroupSalesRevenueVatZoneAccount, "GLTraceReasonItemGroupSalesRevenueVatZoneAccount" },
            { PostingAccountReason.ItemGroupSalesRevenueUseFirstIfBlankFallback, "GLTraceReasonItemGroupSalesRevenueUseFirstIfBlankFallback" },
            { PostingAccountReason.ItemGroupPurchaseBaseAccount, "GLTraceReasonItemGroupPurchaseBaseAccount" },
            { PostingAccountReason.ItemGroupPurchaseVatZoneAccount, "GLTraceReasonItemGroupPurchaseVatZoneAccount" },
            { PostingAccountReason.ItemGroupPurchaseUseFirstIfBlankFallback, "GLTraceReasonItemGroupPurchaseUseFirstIfBlankFallback" },
            { PostingAccountReason.DebtorCreditorPostingProfileRevenueAccount, "GLTraceReasonDebtorCreditorPostingProfileRevenueAccount" },
            { PostingAccountReason.DebtorCreditorPostingProfileCostAccount, "GLTraceReasonDebtorCreditorPostingProfileCostAccount" },
            { PostingAccountReason.DebtorCreditorPostingProfileVatCodeInfluence, "GLTraceReasonDebtorCreditorPostingProfileVatCodeInfluence" },
            { PostingAccountReason.DebtorCreditorGroupRevenueBaseAccount, "GLTraceReasonDebtorCreditorGroupRevenueBaseAccount" },
            { PostingAccountReason.DebtorCreditorGroupRevenueVatZoneAccount, "GLTraceReasonDebtorCreditorGroupRevenueVatZoneAccount" },
            { PostingAccountReason.DebtorCreditorGroupRevenueUseFirstIfBlankFallback, "GLTraceReasonDebtorCreditorGroupRevenueUseFirstIfBlankFallback" },
            { PostingAccountReason.DebtorCostOfGoodsSoldProfileCostAccount, "GLTraceReasonDebtorCostOfGoodsSoldProfileCostAccount" },
            { PostingAccountReason.DebtorCostOfGoodsSoldItemGroupCostAccount, "GLTraceReasonDebtorCostOfGoodsSoldItemGroupCostAccount" },
            { PostingAccountReason.DebtorInventoryOffsetAccount, "GLTraceReasonDebtorInventoryOffsetAccount" },
            { PostingAccountReason.ScrapInventoryReversalAccount, "GLTraceReasonScrapInventoryReversalAccount" },
            { PostingAccountReason.ScrapLossProfitAccount, "GLTraceReasonScrapLossProfitAccount" },
            { PostingAccountReason.PurchaseProjectCostAccount, "GLTraceReasonPurchaseProjectCostAccount" },
            { PostingAccountReason.PurchaseProjectInventoryAccount, "GLTraceReasonPurchaseProjectInventoryAccount" },
            { PostingAccountReason.FixedCostPurchaseCurrentRevenueExpenseAccount, "GLTraceReasonFixedCostPurchaseCurrentRevenueExpenseAccount" },
            { PostingAccountReason.FixedCostPurchaseProfitLossPurchaseAccount, "GLTraceReasonFixedCostPurchaseProfitLossPurchaseAccount" },
            { PostingAccountReason.VatFollowsOriginalPostingAccount, "GLTraceReasonVatFollowsOriginalPostingAccount" },
            { PostingAccountReason.UnrealizedVatAccount, "GLTraceReasonUnrealizedVatAccount" },
            { PostingAccountReason.StandardVatAccount, "GLTraceReasonStandardVatAccount" },
            { PostingAccountReason.StandardVatRate2Account, "GLTraceReasonStandardVatRate2Account" },
            { PostingAccountReason.VatRate2FollowsOriginalPostingAccount, "GLTraceReasonVatRate2FollowsOriginalPostingAccount" },
            { PostingAccountReason.VatOffsetAccount, "GLTraceReasonVatOffsetAccount" },
            { PostingAccountReason.VatOffsetRate2Account, "GLTraceReasonVatOffsetRate2Account" },
            { PostingAccountReason.VatRoundingVatSideAccount, "GLTraceReasonVatRoundingVatSideAccount" },
            { PostingAccountReason.VatRoundingSystemVatRoundingAccount, "GLTraceReasonVatRoundingSystemVatRoundingAccount" },
            { PostingAccountReason.VatRoundingSystemPennyDifAccount, "GLTraceReasonVatRoundingSystemPennyDifAccount" },
            { PostingAccountReason.VatRoundingVatAccountFallback, "GLTraceReasonVatRoundingVatAccountFallback" },
            { PostingAccountReason.LineDiscountAccount, "GLTraceReasonLineDiscountAccount" },
            { PostingAccountReason.LineDiscountOffsetAccount, "GLTraceReasonLineDiscountOffsetAccount" },
            { PostingAccountReason.EndDiscountOwnAccount, "GLTraceReasonEndDiscountOwnAccount" },
            { PostingAccountReason.EndDiscountAdjustedExistingBooking, "GLTraceReasonEndDiscountAdjustedExistingBooking" },
            { PostingAccountReason.EndDiscountPennyDifFallback, "GLTraceReasonEndDiscountPennyDifFallback" },
            { PostingAccountReason.EndDiscountErrorAccountFallback, "GLTraceReasonEndDiscountErrorAccountFallback" },
            { PostingAccountReason.EndDiscountSalesBookingFallback, "GLTraceReasonEndDiscountSalesBookingFallback" },
            { PostingAccountReason.PurchaseChargeSetupAccount, "GLTraceReasonPurchaseChargeSetupAccount" },
            { PostingAccountReason.PurchaseChargePostedOnCreditorTotal, "GLTraceReasonPurchaseChargePostedOnCreditorTotal" },
            { PostingAccountReason.PurchaseChargeDistributedLineRevenueAccount, "GLTraceReasonPurchaseChargeDistributedLineRevenueAccount" },
            { PostingAccountReason.PurchaseChargeDistributedResidualLastRevenueAccount, "GLTraceReasonPurchaseChargeDistributedResidualLastRevenueAccount" },
            { PostingAccountReason.PurchaseChargeProjectCostAccount, "GLTraceReasonPurchaseChargeProjectCostAccount" },
            { PostingAccountReason.PurchaseChargeProjectInventoryAccount, "GLTraceReasonPurchaseChargeProjectInventoryAccount" },
            { PostingAccountReason.DutyAccount, "DutyAccount" },
            { PostingAccountReason.DutyOffsetAccount, "GLTraceReasonDutyOffsetAccount" },
            { PostingAccountReason.DutyGeneratedLineResolvedAccount, "GLTraceReasonDutyGeneratedLineResolvedAccount" },
            { PostingAccountReason.ProjectWorkInProgressAccount, "GLTraceReasonProjectWorkInProgressAccount" },
            { PostingAccountReason.ProjectWorkInProgressOffsetAccount, "GLTraceReasonProjectWorkInProgressOffsetAccount" },
            { PostingAccountReason.ProjectMaterialsAccount, "GLTraceReasonProjectMaterialsAccount" },
            { PostingAccountReason.ProjectMaterialsOffsetAccount, "GLTraceReasonProjectMaterialsOffsetAccount" },
            { PostingAccountReason.ProjectOutlayAccount, "GLTraceReasonProjectOutlayAccount" },
            { PostingAccountReason.ProjectOutlayOffsetAccount, "GLTraceReasonProjectOutlayOffsetAccount" },
            { PostingAccountReason.ProjectAdjustmentAccount, "GLTraceReasonProjectAdjustmentAccount" },
            { PostingAccountReason.ProjectAdjustmentOffsetAccount, "GLTraceReasonProjectAdjustmentOffsetAccount" },
            { PostingAccountReason.ProjectReversedWipAccount, "GLTraceReasonProjectReversedWipAccount" },
            { PostingAccountReason.ProjectReversedWipOffsetAccount, "GLTraceReasonProjectReversedWipOffsetAccount" },
            { PostingAccountReason.ProjectInvoiceAdjustmentAccount, "GLTraceReasonProjectInvoiceAdjustmentAccount" },
            { PostingAccountReason.ProjectInvoiceAdjustmentOffsetAccount, "GLTraceReasonProjectInvoiceAdjustmentOffsetAccount" },
            { PostingAccountReason.EnteredInvoiceAmountPennyDifferenceAccount, "GLTraceReasonEnteredInvoiceAmountPennyDifferenceAccount" },
            { PostingAccountReason.WithholdingSalesAccount, "GLTraceReasonWithholdingSalesAccount" },
            { PostingAccountReason.WithholdingBuyAccount, "GLTraceReasonWithholdingBuyAccount" },
            { PostingAccountReason.ExplicitApiPaymentAccount, "GLTraceReasonExplicitApiPaymentAccount" },
            { PostingAccountReason.NetCashPaymentTermAccount, "GLTraceReasonNetCashPaymentTermAccount" },
            { PostingAccountReason.CreditCardPaymentTermDebtorAccountSummaryAccount, "GLTraceReasonCreditCardPaymentTermDebtorAccountSummaryAccount" },
            { PostingAccountReason.DebtorCreditorGroupSummaryAccount, "GLTraceReasonDebtorCreditorGroupSummaryAccount" },
            { PostingAccountReason.CreditorD2CDebtorGroupSummaryAccount, "GLTraceReasonCreditorD2CDebtorGroupSummaryAccount" },
            { PostingAccountReason.PrepaymentAccount, "GLTraceReasonPrepaymentAccount" },
            { PostingAccountReason.PrepaymentOffsetAccount, "GLTraceReasonPrepaymentOffsetAccount" },
            { PostingAccountReason.Unknown, "GLTraceReasonUnknown" }
        };

        public static string Area(PostingTraceArea area)
        {
            return AreaLabels.TryGetValue(area, out var label) ? Uniconta.ClientTools.Localization.lookup(label) : SplitWords(area.ToString());
        }

        public static string Reason(PostingAccountReason reason)
        {
            return ReasonLabels.TryGetValue(reason, out var label) ? Uniconta.ClientTools.Localization.lookup(label) : SplitWords(reason.ToString());
        }


        public static string SetupArea(string sourceTable)
        {
            if (string.IsNullOrWhiteSpace(sourceTable))
                return sourceTable;

            return sourceTable
                .Replace("DCGroupPosting", Uniconta.ClientTools.Localization.lookup("GLTraceCustomerVendorPostingSetup"))
                .Replace("DCGroup", Uniconta.ClientTools.Localization.lookup("GLTraceCustomerVendorGroup"))
                .Replace("DebtorGroup", Uniconta.ClientTools.Localization.lookup("Debtorgroup"))
                .Replace("CreditorGroup", Uniconta.ClientTools.Localization.lookup("Creditorgroup"))
                .Replace("InvGroup", Uniconta.ClientTools.Localization.lookup("InventoryGroup"))
                .Replace("GLVat", Uniconta.ClientTools.Localization.lookup("GLTraceVATCode"))
                .Replace("SystemAccount", Uniconta.ClientTools.Localization.lookup("SystemAccount"))
                .Replace("GLTrans", Uniconta.ClientTools.Localization.lookup("GLTraceLedgerEntry"));
        }

        public static string Explanation(string explanation)
        {
            // Explanations are localized at their source; English substring substitutions
            // can corrupt translated text and cannot express translated sentence structure.
            return explanation;
        }

        public static string Confidence(ResolutionConfidence confidence)
        {
            switch (confidence)
            {
                case ResolutionConfidence.High:
                    return Uniconta.ClientTools.Localization.lookup("GLTraceStrongMatch");
                case ResolutionConfidence.Medium:
                    return Uniconta.ClientTools.Localization.lookup("GLTraceLikelyMatch");
                case ResolutionConfidence.Low:
                    return Uniconta.ClientTools.Localization.lookup("GLTracePossibleMatch");
                default:
                    return Uniconta.ClientTools.Localization.lookup("GLTraceNotAssessed");
            }
        }

        private static string SplitWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var sb = new System.Text.StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && char.IsUpper(text[i]) && !char.IsUpper(text[i - 1]))
                    sb.Append(' ');

                sb.Append(text[i]);
            }

            return sb.ToString();
        }
    }
}
