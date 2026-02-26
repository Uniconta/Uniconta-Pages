using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.Reports.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Client class to Initialize before Printing the for debtor offer reoprt.
    /// </summary>
    public class DebtorOfferPrintReport : DebtorInvoicePrintReportBase<DebtorOfferClient>
    {
        public DebtorOfferClient DebtorOffer => BaseEntityClient; // keeps your old property name
        public DebtorOfferPrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType)
            : base(postingResult, api, companyLayoutType) { }
        public DebtorOfferPrintReport(DebtorInvoiceClient debtorInvoiceClient, CrudAPI api, CompanyLayoutType companyLayoutType = CompanyLayoutType.Offer)
            : base(debtorInvoiceClient, api, companyLayoutType) { }
        public DebtorOfferPrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType, DebtorOfferClient orderClient)
            : base(postingResult, api, companyLayoutType, orderClient) { }
        protected override Type OrderCacheDataModelType => typeof(Uniconta.DataModel.DebtorOffer);
        protected override DebtorOfferClient CreateOrderFromInvoice(Company Comp)
        {
            var debtorOfferUserType = ReportUtil.GetUserType(typeof(DebtorOfferClient), Comp);
            var debtorOfferUser = Activator.CreateInstance(debtorOfferUserType) as DebtorOfferClient;
            debtorOfferUser.CopyFrom(DebtorInvoice, Debtor);
            return debtorOfferUser;
        }
    }
}
