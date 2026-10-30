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
    public sealed class InvItemGroupPostingWrapper : GroupPostingWrapper
    {
        public InvItemGroupPostingWrapper(InvGroupClient invItemGroup, bool isDebtor, VatZones vatZone)
        {
            if (invItemGroup == null)
                return;

            VatZone = vatZone;
            IsDebtor = isDebtor;
            InvGroup = invItemGroup.Group;

            PurchaseAccount = invItemGroup.PurchaseAccount;  //VatZones.Domestic,
            PurchaseAccount1 = invItemGroup.PurchaseAccount1; //VatZones.EU,
            PurchaseAccount2 = invItemGroup.PurchaseAccount2; //VatZones.Foreign,
            PurchaseAccount3 = invItemGroup.PurchaseAccount3; //VatZones.NoVatReg,
            PurchaseAccount4 = invItemGroup.PurchaseAccount4; //VatZones.VatFree

            RevenueAccount = invItemGroup.RevenueAccount;    //VatZones.Domestic,
            RevenueAccount1 = invItemGroup.RevenueAccount1;   //VatZones.EU,
            RevenueAccount2 = invItemGroup.RevenueAccount2;   //VatZones.Foreign,
            RevenueAccount3 = invItemGroup.RevenueAccount3;   //VatZones.NoVatReg,
            RevenueAccount4 = invItemGroup.RevenueAccount4;   //VatZones.VatFree

            CostAccount = invItemGroup.CostAccount;

            if (isDebtor)
            {
                Vat = invItemGroup.SalesVat;    //VatZones.Domestic,
                Vat1 = invItemGroup.SalesVat1;   //VatZones.EU,
                Vat2 = invItemGroup.SalesVat2;   //VatZones.Foreign,
                Vat3 = invItemGroup.SalesVat3;   //VatZones.NoVatReg,
                Vat4 = invItemGroup.SalesVat4;   //VatZones.VatFree
            }
            else
            {
                Vat = invItemGroup.PurchaseVat;    //VatZones.Domestic,
                Vat1 = invItemGroup.PurchaseVat1;   //VatZones.EU,
                Vat2 = invItemGroup.PurchaseVat2;   //VatZones.Foreign,
                Vat3 = invItemGroup.PurchaseVat3;   //VatZones.NoVatReg,
                Vat4 = invItemGroup.PurchaseVat4;   //VatZones.VatFree
            }

            InventoryAccount = invItemGroup.InvAccount;
            LossProfitAccount = invItemGroup.LossProfit;

            ProfitLossPurchaseAccount = invItemGroup.ProfitLossPurchase;
        }
        public override Type SourceTableType => typeof(InvGroupClient);
        public override string SourceTableKey => InvGroup;

        public override string GetVatCodeFieldName()
        {
            if (IsDebtor)
            {
                switch (VatZone)
                {
                    case VatZones.EU:
                        return "SalesVat";

                    case VatZones.Foreign:
                        return "SalesVat2";

                    case VatZones.NoVatReg:
                        return "SalesVat3";

                    case VatZones.VatFree:
                        return "SalesVat4";

                    case VatZones.Domestic:
                    default:
                        return "SalesVat";
                }
            }
            else
            {
                switch (VatZone)
                {
                    case VatZones.EU:
                        return "PurchaseVat1";

                    case VatZones.Foreign:
                        return "PurchaseVat2";

                    case VatZones.NoVatReg:
                        return "PurchaseVat3";

                    case VatZones.VatFree:
                        return "PurchaseVat4";

                    case VatZones.Domestic:
                    default:
                        return "PurchaseVat";
                }
            }
        }
    }
}
