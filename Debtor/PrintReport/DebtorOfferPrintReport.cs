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
    /// Class to Initialize before printing of Offers
    /// </summary>
    public class DebtorOfferPrintReport
    {
        public DebtorClient Debtor { get; private set; }
        public CompanyClient Company { get; private set; }
        public DebtorOfferClient[] DebtorOffers { get; private set; }
        public DebtorOfferLineClient[] DebtorOfferLines { get; private set; }
        public byte[] CompanyLogo { get; private set; }

        private CrudAPI crudApi;
        private DebtorOfferClient debtorOffer;

        /// <summary>
        /// Initialization of DebtorOfferPrintReport
        /// </summary>
        /// <param name="debtOffer"></param>
        /// <param name="api"></param>
        public DebtorOfferPrintReport(DebtorOfferClient debtOffer,CrudAPI api)
        {
            debtorOffer = debtOffer;
            Debtor = debtOffer.Debtor;
            crudApi = api;
        }

        /// <summary>
        /// Instantiate with User Fields
        /// </summary>
        /// <returns></returns>
        async public Task InstantiateUserFields()
        {
            var Comp = crudApi.CompanyEntity;

            if (debtorOffer != null)
            {
                //debtor
                var debtorUserType = Global.GetTableWithUserFields(Comp, typeof(DebtorClient), true);
                var debtorInstance = Activator.CreateInstance(debtorUserType) as UnicontaBaseEntity;
                var debtor = await crudApi.Query(debtorInstance, new UnicontaBaseEntity[] { debtorOffer.Debtor }, null);

                //DebtorOffer
                var debtorOfferUserType = Global.GetTableWithUserFields(Comp, typeof(DebtorOfferClient), true);
                var debtorOfferInstance = Activator.CreateInstance(debtorOfferUserType) as UnicontaBaseEntity;
                var debtorOffers = (DebtorOfferClient[])await crudApi.Query(debtorOfferInstance, new UnicontaBaseEntity[] { debtorOffer }, null);
                DebtorOffers = debtorOffers;

                //DebtorOfferLines
                var debtorOfferLineUserType = Global.GetTableWithUserFields(Comp, typeof(DebtorOfferLineClient), true);
                var debtorOfferLineInstance = Activator.CreateInstance(debtorOfferLineUserType) as UnicontaBaseEntity;
                var listDebtorOfferLines = new List<DebtorOfferLineClient>();
                foreach (var debtOffer in debtorOffers)
                {
                    var debtorOfferLine = (DebtorOfferLineClient[])await crudApi.Query(debtorOfferLineInstance, new UnicontaBaseEntity[] { debtOffer }, null);
                    listDebtorOfferLines.AddRange(debtorOfferLine);
                }
                DebtorOfferLines = listDebtorOfferLines.ToArray();
            }

            Company = Comp as CompanyClient;
            if (Company == null)
            {
                Company = new CompanyClient();
                StreamingManager.Copy(Comp, Company);
            }

            CompanyLogo = await Uniconta.ClientTools.Util.UtilDisplay.GetLogo(crudApi);
        }
    }
}
