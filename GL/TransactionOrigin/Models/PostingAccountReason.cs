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
    public enum PostingAccountReason
    {
        Unknown = 0,

        // 1. Explicit line account
        ManualAccountEntry = 101,
        LineRevenueAccount = 102,
        ProjectOnAccountLineRevenueAccount = 103,
        OrderLinePostingAccount = 104,

        // 2. Order/customer/vendor override
        OrderPostingAccount = 201,
        CustomerPostingAccount = 202,
        VendorPostingAccount = 203,
        OrderCustomerVendorPostingAccount = 204,

        // 3. Item group by VAT zone
        ItemGroupSalesRevenueBaseAccount = 301,
        ItemGroupSalesRevenueVatZoneAccount = 302,
        ItemGroupSalesRevenueUseFirstIfBlankFallback = 303,
        ItemGroupPurchaseBaseAccount = 304,
        ItemGroupPurchaseVatZoneAccount = 305,
        ItemGroupPurchaseUseFirstIfBlankFallback = 306,

        // 4. Debtor/creditor posting profile
        DebtorCreditorPostingProfileRevenueAccount = 401,
        DebtorCreditorPostingProfileCostAccount = 402,
        DebtorCreditorPostingProfileVatCodeInfluence = 403,

        // 5. Debtor/creditor group revenue by VAT zone
        DebtorCreditorGroupRevenueBaseAccount = 501,
        DebtorCreditorGroupRevenueVatZoneAccount = 502,
        DebtorCreditorGroupRevenueUseFirstIfBlankFallback = 503,

        // 6. Debtor cost/inventory
        DebtorCostOfGoodsSoldProfileCostAccount = 601,
        DebtorCostOfGoodsSoldItemGroupCostAccount = 602,
        DebtorInventoryOffsetAccount = 603,

        // 7. Scrap
        ScrapInventoryReversalAccount = 701,
        ScrapLossProfitAccount = 702,

        // 8. Purchase project stock consumption
        PurchaseProjectCostAccount = 801,
        PurchaseProjectInventoryAccount = 802,

        // 9. Fixed-cost purchase profit/loss
        FixedCostPurchaseCurrentRevenueExpenseAccount = 901,
        FixedCostPurchaseProfitLossPurchaseAccount = 902,

        // 10. VAT follows account
        VatFollowsOriginalPostingAccount = 1001,

        // 11. Unrealized VAT
        UnrealizedVatAccount = 1101,

        // 12. Standard VAT and rate 2
        StandardVatAccount = 1201,
        StandardVatRate2Account = 1202,
        VatRate2FollowsOriginalPostingAccount = 1203,

        /// <summary>
        /// VAT code resolved from the original inventory item-group setup.
        /// </summary>
        ItemGroupVatCode,

        /// <summary>
        /// VAT code resolved from debtor/creditor-group posting setup,
        /// including item-group-specific posting overrides.
        /// </summary>
        DCGroupPostingVatCode,

        // 13. VAT offset
        VatOffsetAccount = 1301,
        VatOffsetRate2Account = 1302,

        // 14. VAT rounding
        VatRoundingVatSideAccount = 1401,
        VatRoundingSystemVatRoundingAccount = 1402,
        VatRoundingSystemPennyDifAccount = 1403,
        VatRoundingVatAccountFallback = 1404,

        // 15. Line discounts
        LineDiscountAccount = 1501,
        LineDiscountOffsetAccount = 1502,

        // 16. End discount / rounding fallback
        EndDiscountOwnAccount = 1601,
        EndDiscountAdjustedExistingBooking = 1602,
        EndDiscountPennyDifFallback = 1603,
        EndDiscountErrorAccountFallback = 1604,
        EndDiscountSalesBookingFallback = 1605,

        // 17. Purchase charges
        PurchaseChargeSetupAccount = 1701,
        PurchaseChargePostedOnCreditorTotal = 1702,
        PurchaseChargeDistributedLineRevenueAccount = 1703,
        PurchaseChargeDistributedResidualLastRevenueAccount = 1704,
        PurchaseChargeProjectCostAccount = 1705,
        PurchaseChargeProjectInventoryAccount = 1706,

        // 18. Duties
        DutyAccount = 1801,
        DutyOffsetAccount = 1802,
        DutyGeneratedLineResolvedAccount = 1803,

        // 19. Project WIP / reverse WIP
        ProjectWorkInProgressAccount = 1901,
        ProjectWorkInProgressOffsetAccount = 1902,
        ProjectMaterialsAccount = 1903,
        ProjectMaterialsOffsetAccount = 1904,
        ProjectOutlayAccount = 1905,
        ProjectOutlayOffsetAccount = 1906,
        ProjectAdjustmentAccount = 1907,
        ProjectAdjustmentOffsetAccount = 1908,
        ProjectReversedWipAccount = 1909,
        ProjectReversedWipOffsetAccount = 1910,
        ProjectInvoiceAdjustmentAccount = 1911,
        ProjectInvoiceAdjustmentOffsetAccount = 1912,

        // 20. Entered invoice amount penny difference
        EnteredInvoiceAmountPennyDifferenceAccount = 2001,

        // 21. Withholding
        WithholdingSalesAccount = 2101,
        WithholdingBuyAccount = 2102,

        // 22. Payment and settlement
        ExplicitApiPaymentAccount = 2201,
        NetCashPaymentTermAccount = 2202,
        CreditCardPaymentTermDebtorAccountSummaryAccount = 2203,
        DebtorCreditorGroupSummaryAccount = 2204,
        CreditorD2CDebtorGroupSummaryAccount = 2205,
        PrepaymentAccount = 2206,
        PrepaymentOffsetAccount = 2207,
        CashPayment = 2208,

        // 23. Misc charges
        MiscChargesAccount = 2301
    }
}
