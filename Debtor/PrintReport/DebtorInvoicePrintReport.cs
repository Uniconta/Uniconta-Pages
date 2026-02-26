using UnicontaClient.Utilities;
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
using System.Collections.Generic;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Base Class to Initialize before Printing the for Debtor
    /// </summary>
    public abstract class DebtorInvoicePrintReportBase<TOrderClient> : PrintReportBaseClient<DebtorInvoiceClient, DebtorInvoiceLines, TOrderClient>
      where TOrderClient : UnicontaBaseEntity
    {
        #region Properties

        public DebtorClient Debtor { get; private set; }
        public DebtorInvoiceClient DebtorInvoice { get { return PrintHeader; } }
        public InvTransInvoice[] DebtorInvoiceLines { get { return PrintLines; } }
        public string ReportName { get; private set; }
        public bool IsCreditNote { get; private set; }
        public DebtorMessagesClient MessageClient { get; private set; }
        public TOrderClient OrderClient => BaseEntityClient;
        public DCPreviousAddressClient PreviousAddressClient { get; private set; }

        #endregion

        #region Fields

        private DCPreviousAddressClient[] previousAddressLookup;
        private DebtorMessagesClient[] debtorMessageLookup;
        private bool isMultipleInvoicePrint;

        #endregion

        #region Abstract Methods

        protected abstract Type OrderCacheDataModelType { get; }
        protected abstract TOrderClient CreateOrderFromInvoice(Company Comp);
        protected virtual string GetOrderCacheKey() => NumberConvert.ToStringNull(DebtorInvoice._OrderNumber);
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DebtorInvoicePrintReportBase class with the specified posting result, API
        /// context, and company layout type.
        /// </summary>
        /// <param name="postingResult">Result from Post Invoice</param>
        /// <param name="api">Current api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        protected DebtorInvoicePrintReportBase(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType)
            : base(postingResult, api, companyLayoutType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DebtorInvoicePrintReportBase class with the specified debtor invoice
        /// client, API, and company layout type.
        /// </summary>
        /// <param name="debtorInvoiceClient">Invoice Client</param>
        /// <param name="api">Current api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        protected DebtorInvoicePrintReportBase(DebtorInvoiceClient debtorInvoiceClient, CrudAPI api, CompanyLayoutType companyLayoutType = CompanyLayoutType.Invoice)
            : base(debtorInvoiceClient, api, companyLayoutType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DebtorInvoicePrintReportBase class with the specified posting result, API
        /// context, company layout type, and order client.
        /// </summary>
        /// <param name="postingResult">PostInvoice result</param>
        /// <param name="api">Current api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        /// <param name="orderClient">DebtorOrder/DebtorOffer client/Proposal Client</param>
        protected DebtorInvoicePrintReportBase(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType, TOrderClient orderClient)
            : base(postingResult, api, companyLayoutType, orderClient)
        {
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the debtor email type
        /// </summary>
        /// <returns>DebtorEmailType</returns>
        private DebtorEmailType GetDebtorEmailType()
        {
            DebtorEmailType emailType = DebtorEmailType.Invoice;
            switch (layoutType)
            {
                case CompanyLayoutType.Offer:
                    emailType = DebtorEmailType.Offer;
                    break;
                case CompanyLayoutType.OrderConfirmation:
                    emailType = DebtorEmailType.OrderConfirmation;
                    break;
                case CompanyLayoutType.Packnote:
                    emailType = DebtorEmailType.Packnote;
                    break;
            }
            return emailType;
        }

        /// <summary>
        /// Setup lookup for DebtorMessageClients
        /// </summary>
        public void SetLookUpForDebtorMessageClients(DebtorMessagesClient[] debtorMessageClients)
        {
            isMultipleInvoicePrint = true;

            if (debtorMessageLookup == null)
                debtorMessageLookup = debtorMessageClients;
        }

        /// <summary>
        /// Setup lookup for PreviousAddress Clients
        /// </summary>
        /// <param name="preivousAddressClients"></param>
        public void SetLookUpForPreviousAddressClients(DCPreviousAddressClient[] preivousAddressClients)
        {
            isMultipleInvoicePrint = true;
            if (previousAddressLookup == null)
                previousAddressLookup = preivousAddressClients;
        }

        /// <summary>
        /// To get the languaeg for the print
        /// </summary>
        /// <returns>Language enum</returns>
        protected override Language GetLanguage()
        {
            return layoutType != CompanyLayoutType.PickingList ? ReportGenUtil.GetLanguage(Debtor, base.Company) : base.GetLanguage();
        }

        /// <summary>
        /// To set the entity
        /// </summary>
        /// <returns></returns>
        protected override async Task SetEntityTask()
        {
            try
            {
                DebtorClient debtor;

                var Comp = crudApi.CompanyEntity;
                var debtorClientUser = Comp.CreateUserType<DebtorClient>();

                if (DebtorInvoice._OneTimeDC == null)
                {
                    var dcCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.Debtor));
                    debtor = dcCache.Get(DebtorInvoice._DCAccount) as DebtorClient;
                }
                else
                    debtor = DebtorInvoice._OneTimeDC as DebtorClient;

                StreamingManager.Copy(debtor, debtorClientUser);

                Debtor = debtorClientUser;
            }
            catch (Exception ex)
            {
                crudApi?.ReportException(ex, $"Error Occured in SetEntityTask for {GetType().Name} - {ex.Message}");
            }
        }

        /// <summary>
        /// Method to finalize set action for print
        /// </summary>
        /// <returns></returns>
        protected override async Task FinalizeTask()
        {
            try
            {
                var Comp = crudApi.CompanyEntity;

                if (DebtorInvoice._Prospect != 0)
                {
                    //Check for Prospect. Create a Debtor for Prospect
                    var prosCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmProspect)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.CrmProspect));
                    var prospect = prosCache?.Get(DebtorInvoice._Prospect) as CrmProspect;
                    if (prospect != null)
                        Debtor.CopyFrom(prospect);
                }

                if (PreviousAddressClient == null)
                    PreviousAddressClient = isMultipleInvoicePrint ? LayoutPrintReport.GetPreviousAddressClientForInvoice(previousAddressLookup, DebtorInvoice) :
                        await LayoutPrintReport.GetPreviousAddressClientForInvoice(DebtorInvoice, crudApi);

                //Setting the Previous Address if Exist for current invoice
                if (PreviousAddressClient != null)
                {
                    Debtor._Name = PreviousAddressClient._Name;
                    Debtor._Address1 = PreviousAddressClient._Address1;
                    Debtor._Address2 = PreviousAddressClient._Address2;
                    Debtor._Address3 = PreviousAddressClient._Address3;
                    Debtor._City = PreviousAddressClient._City;
                    Debtor._ZipCode = PreviousAddressClient._ZipCode;
                }

                //To Contact listing for the current debtor
                if (Comp.Contacts)
                {
                    var ContactsCache = Comp.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.Contact)).ConfigureAwait(false);
                    if (ContactsCache != null)
                    {
                        var contactCacheFilter = new ContactCacheFilter(ContactsCache, Debtor.__DCType(), Debtor._Account);
                        if (contactCacheFilter.Any())
                        {
                            try
                            {
                                Debtor.Contacts = contactCacheFilter.Cast<ContactClient>().ToArray();
                            }
                            catch { }
                        }
                    }
                }

                if (DebtorInvoice._Installation != null && Comp.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) == null)
                    await Comp.LoadCache(typeof(Uniconta.DataModel.WorkInstallation), crudApi);

                UtilCommon.SetDeliveryAdress(DebtorInvoice, Debtor, crudApi);
                DebtorInvoice.SetInvoiceAddress(Debtor);

                /*In case order is null, fill from DCInvoice*/
                if (BaseEntityClient == null)
                {
                    var key = GetOrderCacheKey();
                    var cache = Comp.GetCache(OrderCacheDataModelType) ?? await crudApi.LoadCache(OrderCacheDataModelType);
                    var order = (TOrderClient)cache?.Get(key);

                    if (order != null)
                        SetOrder(order);
                    else
                        SetOrder(CreateOrderFromInvoice(Comp));
                }

                var invoiceNumber = DebtorInvoice._InvoiceNumber;
                var lineTotal = DebtorInvoice._LineTotal;
                IsCreditNote = (lineTotal < -0.0001d);

                ReportName = layoutType != CompanyLayoutType.Invoice ? layoutType.ToString() : invoiceNumber == 0 ? (IsCreditNote ? "ProformaCreditNote" : "ProformaInvoice")
                        : (IsCreditNote ? "Creditnote" : "Invoice");

                MessageClient = isMultipleInvoicePrint ? LayoutPrintReport.GetDebtorMessageClient(debtorMessageLookup, GetLanguage(), GetDebtorEmailType()) :
                    await UtilCommon.GetDebtorMessageClient(crudApi, GetLanguage(), GetDebtorEmailType());

                var _LayoutGroup = DebtorInvoice._LayoutGroup ?? Debtor._LayoutGroup;
                if (_LayoutGroup != null)
                {
                    var cache = crudApi.GetCache(typeof(DebtorLayoutGroup)) ?? await crudApi.LoadCache(typeof(DebtorLayoutGroup));
                    var layClient = (DebtorLayoutGroup)cache.Get(_LayoutGroup);
                    layClient?.SetCompanyBank(base.Company);
                }
            }
            catch (Exception ex)
            {
                crudApi?.ReportException(ex, $"Error Occured in FinalizeTask for {GetType().Name} - {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Client class to Initialize before Printing the for Debtor documnets like Invoice, Creditnote, Offer, Order Confirmation etc
    /// </summary>
    public class DebtorInvoicePrintReport : DebtorInvoicePrintReportBase<DebtorOrderClient>
    {
        public DebtorOrderClient DebtorOrder => BaseEntityClient; // keeps your old property name

        public DebtorInvoicePrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType)
            : base(postingResult, api, companyLayoutType) { }

        public DebtorInvoicePrintReport(DebtorInvoiceClient debtorInvoiceClient, CrudAPI api, CompanyLayoutType companyLayoutType = CompanyLayoutType.Invoice)
            : base(debtorInvoiceClient, api, companyLayoutType) { }

        public DebtorInvoicePrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType, DebtorOrderClient orderClient)
            : base(postingResult, api, companyLayoutType, orderClient) { }

        protected override Type OrderCacheDataModelType => typeof(Uniconta.DataModel.DebtorOrder);

        protected override DebtorOrderClient CreateOrderFromInvoice(Company Comp)
        {
            var debtorOrderUserType = ReportUtil.GetUserType(typeof(DebtorOrderClient), Comp);
            var debtorOrderUser = Activator.CreateInstance(debtorOrderUserType) as DebtorOrderClient;
            debtorOrderUser.CopyFrom(DebtorInvoice, Debtor);
            return debtorOrderUser;
        }
    }

    /// <summary>
    /// Client class to Initialize before Printing the for Project documnets Project proposal or invoice
    /// </summary>
    public class ProjectInvoiceProposalPrintReport : DebtorInvoicePrintReportBase<ProjectInvoiceProposalClient>
    {
        public ProjectInvoiceProposalClient ProjectInvoiceProposal => BaseEntityClient; 

        public ProjectInvoiceProposalPrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType)
            : base(postingResult, api, companyLayoutType) { }

        public ProjectInvoiceProposalPrintReport(DebtorInvoiceClient debtorInvoiceClient, CrudAPI api, CompanyLayoutType companyLayoutType = CompanyLayoutType.Invoice)
            : base(debtorInvoiceClient, api, companyLayoutType) { }

        public ProjectInvoiceProposalPrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType, ProjectInvoiceProposalClient orderClient)
            : base(postingResult, api, companyLayoutType, orderClient) { }

        protected override Type OrderCacheDataModelType => typeof(Uniconta.DataModel.ProjectInvoiceProposal);

        protected override ProjectInvoiceProposalClient CreateOrderFromInvoice(Company Comp)
        {
            var proposalUserType = ReportUtil.GetUserType(typeof(ProjectInvoiceProposalClient), Comp);
            var proposalUser = Activator.CreateInstance(proposalUserType) as ProjectInvoiceProposalClient;
            proposalUser.CopyFrom(DebtorInvoice, Debtor);
            return proposalUser;
        }
    }

}
