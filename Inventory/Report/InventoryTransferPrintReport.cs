using DevExpress.Data.Filtering.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class InventoryTransferPrintReport : PrintReportBaseClient<DebtorInvoiceClient, InvTransInvoice, InvTransferOrderClient>
    {
        #region Properties

        public string ReportName { get; private set; }
        public InvTransferOrderClient InventoryTransferOrder { get { return BaseEntityClient; } }
        public DCInvoiceClient InventoryTransferHeader { get { return PrintHeader; } }
        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="invPostingResult"></param>
        /// <param name="api"></param>
        /// <param name="companyLayoutType"></param>
        /// <param name="entityOrder"></param>
        public InventoryTransferPrintReport(InvoicePostingResult invPostingResult, CrudAPI api, CompanyLayoutType companyLayoutType, InvTransferOrderClient entityOrder) : base(invPostingResult, api, companyLayoutType, entityOrder)
        {

        }

        #endregion

        #region Methods

        protected override Language GetLanguage()
        {
            return ReportGenUtil.GetLanguageFromCompany(base.Company);
        }

        async protected override Task FinalizeTask()
        {
            ReportName = layoutType == CompanyLayoutType.TransferPacknote ? layoutType.ToString() : CompanyLayoutType.PickingList.ToString();
        }

        #endregion
    }
}
