using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public abstract class GroupPostingWrapper
    {
        public string InventoryAccount { get; set; }
        public string LossProfitAccount { get; set; }

        public string PurchaseAccount { get; protected set; }
        public string PurchaseAccount1 { get; protected set; }
        public string PurchaseAccount2 { get; protected set; }
        public string PurchaseAccount3 { get; protected set; }
        public string PurchaseAccount4 { get; protected set; }

        public string RevenueAccount { get; protected set; }
        public string RevenueAccount1 { get; protected set; }
        public string RevenueAccount2 { get; protected set; }
        public string RevenueAccount3 { get; protected set; }
        public string RevenueAccount4 { get; protected set; }

        public string CostAccount { get; protected set; }
        public string CostAccount1 { get; protected set; }
        public string CostAccount2 { get; protected set; }
        public string CostAccount3 { get; protected set; }
        public string CostAccount4 { get; protected set; }

        public string Vat { get; protected set; }
        public string Vat1 { get; protected set; }
        public string Vat2 { get; protected set; }
        public string Vat3 { get; protected set; }
        public string Vat4 { get; protected set; }

        public string InvGroup { get; set; }
        public string DCGroup { get; set; }

        public bool HasItemGroup => !string.IsNullOrEmpty(InvGroup);
        public bool HasDCGroup => !string.IsNullOrEmpty(DCGroup);

        public VatZones VatZone { get; protected set; }

        public bool IsDebtor { get; set; }

        public bool IsOnItemLevelOnly { get; set; }

        public bool IsDomesticVat => VatZone == VatZones.Domestic;

        public abstract System.Type SourceTableType { get; }
        public abstract string SourceTableKey { get; }

        public string NonDomesticAccount(bool cost = false)
        {
            if(cost)
            {
                switch (VatZone)
                {
                    case VatZones.EU:
                        return CostAccount1;

                    case VatZones.Foreign:
                        return CostAccount2;

                    case VatZones.NoVatReg:
                        return CostAccount3;

                    case VatZones.VatFree:
                        return CostAccount4;

                    case VatZones.Domestic:
                    default:
                        return null;
                }
            }
            else
            {
                switch (VatZone)
                {
                    case VatZones.EU:
                        return IsDebtor ? RevenueAccount1 : PurchaseAccount1;

                    case VatZones.Foreign:
                        return IsDebtor ? RevenueAccount2 : PurchaseAccount2;

                    case VatZones.NoVatReg:
                        return IsDebtor ? RevenueAccount3 : PurchaseAccount3;

                    case VatZones.VatFree:
                        return IsDebtor ? RevenueAccount4 : PurchaseAccount4;

                    case VatZones.Domestic:
                    default:
                        return null;
                }
            }

        }

        public string VatZoneAccountFieldName(bool cost = false)
        {
            if(cost)
            {
                switch (VatZone)
                {
                    case VatZones.EU:
                        return "CostAccount1";

                    case VatZones.Foreign:
                        return "CostAccount2";

                    case VatZones.NoVatReg:
                        return "CostAccount3";

                    case VatZones.VatFree:
                        return "CostAccount4";

                    case VatZones.Domestic:
                    default:
                        return "CostAccount";
                }
            }

            switch (VatZone)
            {
                case VatZones.EU:
                    return IsDebtor ? "RevenueAccount1" : "PurchaseAccount1";

                case VatZones.Foreign:
                    return IsDebtor ? "RevenueAccount2" : "PurchaseAccount2";

                case VatZones.NoVatReg:
                    return IsDebtor ? "RevenueAccount3" : "PurchaseAccount3";

                case VatZones.VatFree:
                    return IsDebtor ? "RevenueAccount4" : "PurchaseAccount4";

                case VatZones.Domestic:
                default:
                    return IsDebtor ? "RevenueAccount" : "PurchaseAccount";
            }
        }

        public PostingAccountReason VatZoneReason(bool cost = false)
        {
            if(cost)
            {
                return PostingAccountReason.DebtorCostOfGoodsSoldItemGroupCostAccount ; // TODO: more?
            }

            if (IsDebtor)
            {
                    return VatZone == VatZones.Domestic
                    ? PostingAccountReason.ItemGroupSalesRevenueBaseAccount
                    : PostingAccountReason.ItemGroupSalesRevenueVatZoneAccount;
            }

            return VatZone == VatZones.Domestic
                ? PostingAccountReason.ItemGroupPurchaseBaseAccount
                : PostingAccountReason.ItemGroupPurchaseVatZoneAccount;
        }

        public string DomesticAccount(bool cost = false)
        {
            return cost ? CostAccount : ( IsDebtor ? RevenueAccount : PurchaseAccount);
        }

        public string BaseAccountFieldName(bool cost = false)
        {
            return cost ? "CostAccount" : (IsDebtor ? "RevenueAccount" : "PurchaseAccount");
        }

        public PostingAccountReason BaseReason(bool cost = false) => IsDebtor ? PostingAccountReason.ItemGroupSalesRevenueBaseAccount : PostingAccountReason.ItemGroupPurchaseBaseAccount;
        public bool IsNonDomesticVatZone() => VatZone != VatZones.Domestic;

        public static GroupPostingWrapper From(InvGroupClient invItemGroup, VatZones vatZone, bool isDebtor) => new InvItemGroupPostingWrapper(invItemGroup, isDebtor, vatZone);
        public static GroupPostingWrapper From(DCGroup dcGroup, VatZones vatZone) => new DCGroupPostingWrapper(dcGroup, vatZone);
        public static GroupPostingWrapper From(DCGroupPostingClient dcGroupPosting, VatZones vatZone) => new DCGroupPostingProfileWrapper(dcGroupPosting, vatZone);

        public string InventoryAccountFieldName() => "InvAccount";
        public string LossProfitAccountFieldName() => "LossProfit";

        public string ProfitLossPurchaseAccount { get; set; }

        public string ProfitLossPurchaseAccountFieldName() =>  "ProfitLossPurchase";

        public string GetVatCode()
        {
            switch (VatZone)
            {
                case VatZones.EU:
                    return Vat1;

                case VatZones.Foreign:
                    return Vat2;

                case VatZones.NoVatReg:
                    return Vat3;

                case VatZones.VatFree:
                    return Vat4;

                case VatZones.Domestic:
                default:
                    return Vat;
            }
        }

        public abstract string GetVatCodeFieldName();

    }
}
