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
    public sealed class DCGroupPostingProfileWrapper : GroupPostingWrapper
    {
        public DCGroupPostingProfileWrapper(DCGroupPostingClient dcGroupPosting, VatZones vatZone)
        {
            if (dcGroupPosting == null)
                return;

            VatZone = vatZone;
            DCGroup = dcGroupPosting._Group;
            InvGroup = dcGroupPosting._InvGroup;

            if (dcGroupPosting._DCType == (byte)GLTransRefType.Creditor)
            {
                IsDebtor = false;
                PurchaseAccount =  dcGroupPosting._RevenueAccount;
                PurchaseAccount1 = dcGroupPosting._RevenueAccount1;
                PurchaseAccount2 = dcGroupPosting._RevenueAccount2;
                PurchaseAccount3 = dcGroupPosting._RevenueAccount3;
                PurchaseAccount4 = dcGroupPosting._RevenueAccount4;
            }
            else
            { // debtor only
                IsDebtor = true;
                RevenueAccount = dcGroupPosting._RevenueAccount;
                RevenueAccount1 = dcGroupPosting._RevenueAccount1;
                RevenueAccount2 = dcGroupPosting._RevenueAccount2;
                RevenueAccount3 = dcGroupPosting._RevenueAccount3;
                RevenueAccount4 = dcGroupPosting._RevenueAccount4;

                CostAccount  = dcGroupPosting._CostAccount;     // VatZones.Domestic,
                CostAccount1 = dcGroupPosting._CostAccount1;    // VatZones.EU,
                CostAccount2 = dcGroupPosting._CostAccount2;    // VatZones.Foreign,
                CostAccount3 = dcGroupPosting._CostAccount3;    // VatZones.NoVatReg,
                CostAccount4 = dcGroupPosting._CostAccount4;    // VatZones.VatFree
            }

            Vat =  dcGroupPosting._Vat;    //VatZones.Domestic,
            Vat1 = dcGroupPosting._Vat1;   //VatZones.EU,
            Vat2 = dcGroupPosting._Vat2;   //VatZones.Foreign,
            Vat3 = dcGroupPosting._Vat3;   //VatZones.NoVatReg,
            Vat4 = dcGroupPosting._Vat4;   //VatZones.VatFree

            InventoryAccount = dcGroupPosting.InventoryGroup?.InvAccount;
            LossProfitAccount = dcGroupPosting.InventoryGroup?.LossProfit;
            ProfitLossPurchaseAccount = dcGroupPosting.InventoryGroup?.ProfitLossPurchase;
        }

        public override Type SourceTableType => typeof(DCGroupPostingClient);
        public override string SourceTableKey => string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceGroupSourceKey"), DCGroup, InvGroup);

        public override string GetVatCodeFieldName()
        {
            {
                switch (VatZone)
                {
                    case VatZones.EU:
                        return "Vat1";

                    case VatZones.Foreign:
                        return "Vat2";

                    case VatZones.NoVatReg:
                        return "Vat3";

                    case VatZones.VatFree:
                        return "Vat4";

                    case VatZones.Domestic:
                    default:
                        return "Vat";
                }
            }
        }
    }
}
