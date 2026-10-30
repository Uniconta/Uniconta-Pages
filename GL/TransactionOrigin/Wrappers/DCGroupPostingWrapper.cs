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
    public sealed class DCGroupPostingWrapper : GroupPostingWrapper
    {
        public DCGroupPostingWrapper(DCGroup dcGroup, VatZones vatZone)
        {
            if (dcGroup == null)
                return;

            VatZone = vatZone;
            DCGroup = dcGroup._Group;
            IsDebtor = dcGroup is DebtorGroup;


            //PurchaseAccount = dcGroup._c;
            //PurchaseAccount1 = dcGroup.PurchaseAccount1;
            //PurchaseAccount2 = dcGroup.PurchaseAccount2;
            //PurchaseAccount3 = dcGroup.PurchaseAccount3;
            //PurchaseAccount4 = dcGroup.PurchaseAccount4;

            RevenueAccount = dcGroup._RevenueAccount;    //VatZones.Domestic,
            RevenueAccount1 = dcGroup._RevenueAccount1;   //VatZones.EU,
            RevenueAccount2 = dcGroup._RevenueAccount2;   //VatZones.Foreign,
            RevenueAccount3 = dcGroup._RevenueAccount3;   //VatZones.NoVatReg,
            RevenueAccount4 = dcGroup._RevenueAccount4;   //VatZones.VatFree

            Vat = dcGroup._Vat;    //VatZones.Domestic,
            Vat1 = dcGroup._Vat1;   //VatZones.EU,
            Vat2 = dcGroup._Vat2;   //VatZones.Foreign,
            Vat3 = dcGroup._Vat3;   //VatZones.NoVatReg,
            Vat4 = dcGroup._Vat4;   //VatZones.VatFree

            //CostAccount =  dcGroup.;
            //CostAccount1 = dcGroup._CostAccount1;
            //CostAccount2 = dcGroup._CostAccount2;
            //CostAccount3 = dcGroup._CostAccount3;
            //CostAccount4 = dcGroup._CostAccount4;
        }

        public override Type SourceTableType => IsDebtor ? typeof(DebtorGroupClient) : typeof(CreditorGroupClient);
        public override string SourceTableKey => DCGroup;

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
