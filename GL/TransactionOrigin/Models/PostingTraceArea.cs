using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public enum PostingTraceArea
    {
        Unknown = 0,

        ManualJournalLineEntry = 1,
        ExplicitInvoiceOrderLinePostingAccount = 2,
        OrderCustomerVendorPostingAccountOverride = 3,
        ItemGroupAccountByVatZone = 4,
        DebtorCreditorGroupPostingProfile = 5,
        DebtorCreditorGroupRevenueAccountByVatZone = 6,

        DebtorCostOfGoodsSoldAndInventoryOffset = 7,
        ScrapProfitLossAdjustment = 8,
        PurchaseProjectStockConsumption = 9,
        FixedCostPurchaseProfitLoss = 10,

        VatAccountFollowsPostingAccount = 11,
        UnrealizedVatAccount = 12,
        StandardVatAccountAndRate2VatAccount = 13,
        VatOffsetAccount = 14,
        VatRoundingAccount = 15,
        VatOnGroupSetup = 16,

        LineDiscountAccounts = 17,
        EndDiscountAccountAndRoundingFallback = 18,
        PurchaseCharges = 19,
        Duties = 20,
        ProjectWipAndReverseWip = 21,
        EnteredInvoiceAmountPennyDifference = 22,
        WithholdingTax = 23,
        PaymentAndSettlementAccounts = 24,
        MiscCharges = 25
    }
}
