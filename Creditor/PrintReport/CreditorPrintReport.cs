using System;
using System.Linq;
using System.Threading.Tasks;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.Reports.Utilities;
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;
using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Class to Initialize before Printing the any purchase document
    /// </summary>
    public class CreditorPrintReport : PrintReportBaseClient<CreditorInvoiceClient, CreditorInvoiceLines, CreditorOrderClient>
    {
        #region Properties

        public CreditorClient Creditor { get; private set; }
        public CreditorInvoiceClient CreditorInvoice { get { return PrintHeader; } }
        public InvTransInvoice[] CreditorInvoiceLines { get { return PrintLines; } } 
        public string ReportName { get; private set; }
        public CreditorOrderClient CreditorOrder { get; private set; }
        public bool IsCreditNote { get; private set; }
        public string CreditorMessage { get; private set; }

        #endregion

        #region Fields

        private DebtorMessagesClient[] debtorMessageLookup;
        private bool isMultiInvoicePrint;

        #endregion

        #region Constructor

        /// <summary>
        /// Initialization for Post invoice
        /// </summary>
        /// <param name="invPostingResult">Result from postinvoice</param>
        /// <param name="api">Api instacce</param>
        /// <param name="companyLayoutType">Layout type</param>
        public CreditorPrintReport(InvoicePostingResult invPostingResult, CrudAPI api, CompanyLayoutType companyLayoutType) : base(invPostingResult, api, companyLayoutType)
        {

        }

        /// <summary>
        /// Initialization for CreditorInvoice client
        /// </summary>
        /// <param name="creditorInvoiceClient">Invoice client </param>
        /// <param name="api">Api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        public CreditorPrintReport(CreditorInvoiceClient creditorInvoiceClient, CrudAPI api, CompanyLayoutType companyLayoutType = CompanyLayoutType.PurchaseInvoice) : base(creditorInvoiceClient, api, companyLayoutType)
        {

        }

        /// <summary>
        /// Intialization of CreditorInvoice with Creditor Order
        /// </summary>
        /// <param name="postingResult">Invoice posting result</param>
        /// <param name="api">Api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        /// <param name="orderClient">Creditor Order client instance</param>
        public CreditorPrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType, CreditorOrderClient orderClient) : base(postingResult, api, companyLayoutType, orderClient)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets Message Client text    
        /// </summary>
        /// <param name="lang">Language</param>
        /// <returns>Text</returns>
        async private Task<string> GetMessageClientText(Language lang)
        {
            var messageClient = await UtilCommon.GetDebtorMessageClient(crudApi, lang, GetEmailTypeForCreditor());
            return messageClient?._Text;
        }

        /// <summary>
        /// Gets the email type for Creditor
        /// </summary>
        /// <returns></returns>
        private DebtorEmailType GetEmailTypeForCreditor()
        {
            switch (layoutType)
            {
                case CompanyLayoutType.PurchaseOrder:
                    return DebtorEmailType.PurchaseOrder;
                case CompanyLayoutType.PurchasePacknote:
                    return DebtorEmailType.PurchasePacknote;
                case CompanyLayoutType.Requisition:
                    return DebtorEmailType.Requisition;
                default:
                    return DebtorEmailType.PurchaseInvoice;
            }
        }

        /// <summary>
        /// Create lookup for Message clients
        /// </summary>
        /// <returns></returns>
        public void SetLookUpForMessageClient(DebtorMessagesClient[] debtorMessageClients)
        {
            isMultiInvoicePrint = true;
            if (debtorMessageLookup == null)
                debtorMessageLookup = debtorMessageClients;
        }

        /// <summary>
        /// Gets the language for prit
        /// </summary>
        /// <returns>Language enum</returns>
        protected override Language GetLanguage()
        {
            return ReportGenUtil.GetLanguage(Creditor, base.Company);
        }

        /// <summary>
        /// Sets the Entity
        /// </summary>
        /// <returns></returns>
        async protected override Task SetEntityTask()
        {
            try
            {
                var Comp = crudApi.CompanyEntity;
                var creditorUser = Comp.CreateUserType<CreditorClient>();

                var dcCahce = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.Creditor));
                var cred = dcCahce.Get(CreditorInvoice._DCAccount) as UnicontaBaseEntity;

                StreamingManager.Copy(cred, creditorUser);

                Creditor = creditorUser;
            }
            catch(Exception ex)
            {
                crudApi?.ReportException(ex, $"Error Occured in SetEntityTask for SetEntityTask - {ex.Message}");
            }
        }

        /// <summary>
        ///  Method to finalize set action for print
        /// </summary>
        /// <returns></returns>
        async protected override Task FinalizeTask()
        {
            try
            {
                var Comp = crudApi.CompanyEntity;

                if (Comp.Contacts)
                {
                    var contactCache = Comp.GetCache(typeof(Contact)) ?? await crudApi.LoadCache(typeof(Contact));
                    var contactCacheFilter = new ContactCacheFilter(contactCache, Creditor.__DCType(), Creditor._Account);
                    if (contactCacheFilter.Any())
                    {
                        try
                        {
                            Creditor.Contacts = contactCacheFilter.Cast<ContactClient>().ToArray();
                        }
                        catch { }
                    }
                }
                UtilCommon.SetDeliveryAdress(CreditorInvoice, Creditor, crudApi);

                /*In case creditor order is null, fill from DCInvoice*/
                if (CreditorOrder == null)
                {
                    CreditorOrder = Comp.GetCache(typeof(Uniconta.DataModel.CreditorOrder))?.Get(NumberConvert.ToStringNull(CreditorInvoice._OrderNumber)) as CreditorOrderClient;
                    if (CreditorOrder == null)
                    {
                        var creditorOrderUserType = ReportUtil.GetUserType(typeof(CreditorOrderClient), Comp);
                        var creditorOrderUser = Activator.CreateInstance(creditorOrderUserType) as CreditorOrderClient;
                        creditorOrderUser.CopyFrom(CreditorInvoice, Creditor);
                        SetOrder(creditorOrderUser);
                    }
                }

                //Setting ReportName and Version
                var lineTotal = CreditorInvoice._LineTotal;
                IsCreditNote = CreditorInvoice._LineTotal < -0.0001d && layoutType == CompanyLayoutType.PurchaseInvoice;
                ReportName = IsCreditNote ? "CreditNote" : layoutType.ToString();

                CreditorMessage = isMultiInvoicePrint ? LayoutPrintReport.GetDebtorMessageClient(debtorMessageLookup, GetLanguage(), GetEmailTypeForCreditor())?._Text :
                    await GetMessageClientText(GetLanguage());
            }
            catch(Exception ex)
            {
                crudApi?.ReportException(ex, $"Error Occured in FinalizeTask for CreditorPrintReport - {ex.Message}");
            }

        }
        #endregion
    }
}
