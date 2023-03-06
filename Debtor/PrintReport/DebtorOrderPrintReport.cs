using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Class to Initialize before Printing of Orders
    /// </summary>
    public class DebtorOrderPrintReport
    {
        public DebtorClient Debtor { get; private set; }
        public CompanyClient Company { get; private set; }
        public DebtorOrderClient[] DebtorOrders { get; private set; }
        public DebtorOrderLineClient[] DebtorOrderLines { get; private set; }
        public byte[] CompanyLogo { get; private set; }

        private CrudAPI crudApi;
        private DebtorOrderClient debtorOrder;

        public DebtorOrderPrintReport(DebtorOrderClient debtOrder, CrudAPI api)
        {
            debtorOrder = debtOrder;
            Debtor = debtOrder.Debtor;
            crudApi = api;
        }

        async public Task InstantiateUserFields()
        {
            var Comp = crudApi.CompanyEntity;

            if (debtorOrder != null)
            {
                // Debtor
                var debtorUserType = Global.GetTableWithUserFields(Comp, typeof(DebtorClient), true);
                var debtorInstance = Activator.CreateInstance(debtorUserType) as UnicontaBaseEntity;
                var debtor = await crudApi.Query(debtorInstance, new UnicontaBaseEntity[] { debtorOrder.Debtor }, null);
                Debtor = debtor[0] as DebtorClient;

                //DebtorOrder
                var debtorOrderUserType = Global.GetTableWithUserFields(Comp, typeof(DebtorOrderClient), true);
                var debtorOrderInstance = Activator.CreateInstance(debtorOrderUserType) as UnicontaBaseEntity;
                var debtOrders = (DebtorOrderClient[])await crudApi.Query(debtorOrderInstance, new UnicontaBaseEntity[] { debtorOrder }, null);
                DebtorOrders = debtOrders;

                //DebtorOrderLines
                var debtorOrderLineUserType = Global.GetTableWithUserFields(Comp, typeof(DebtorOrderLineClient), true);
                var debtorOrderLineInstance = Activator.CreateInstance(debtorOrderLineUserType) as UnicontaBaseEntity;
                var listDebtorOrderlines = new List<DebtorOrderLineClient>();
                foreach (var debtOrder in debtOrders)
                {
                    var debtororderLines = (DebtorOrderLineClient[])await crudApi.Query(debtorOrderLineInstance, new UnicontaBaseEntity[] { debtOrder }, null);
                    listDebtorOrderlines.AddRange(debtororderLines);
                }
                DebtorOrderLines = listDebtorOrderlines.ToArray();
            }

            Company = Comp as CompanyClient;
            if (Company == null || Company.GetType() != Company.GetUserTypeNotNull(typeof(CompanyClient)))
            {
                Company = Comp.CreateUserType<CompanyClient>();
                StreamingManager.Copy(Comp, Company);
            }

            CompanyLogo = await UnicontaClient.Utilities.UtilCommon.GetLogo(crudApi);
        }
    }
}
