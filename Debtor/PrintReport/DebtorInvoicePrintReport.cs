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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Class to Initialize before Printing the Invoices
    /// </summary>
    public class DebtorInvoicePrintReport
    {
        public DebtorClient Debtor { get; private set; }
        public CompanyClient Company { get; private set; }
        public DebtorInvoiceClient DebtorInvoice { get; private set; }
        public InvTransInvoice[] InvTransInvoiceLines { get; private set; }
        public byte[] CompanyLogo { get; private set; }
        public string ReportName { get; private set; }
        public bool IsCreditNote { get; private set; }
        public DebtorMessagesClient MessageClient { get; private set; }
        public DCOrder DebtorOrder { get; private set; }
        public DCPreviousAddressClient PreviousAddressClient { get; private set; }

        private readonly CrudAPI crudApi;
        private InvoicePostingResult invoicePostingResult;
        private bool isRePrint;
        private CompanyLayoutType layoutType;
        private DebtorInvoiceClient debtorInvoice;
        private DCPreviousAddressClient[] previousAddressLookup;
        private DebtorMessagesClient[] debtorMessageLookup;
        private bool isMultipleInvoicePrint;

        /// <summary>
        /// Intitialization  for Post Invoice
        /// </summary>
        /// <param name="postingResult">Result from Post Invoice</param>
        /// <param name="api">Current api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        public DebtorInvoicePrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType)
        {
            invoicePostingResult = postingResult;
            crudApi = api;
            isRePrint = true;
            layoutType = companyLayoutType;
        }

        /// <summary>
        /// Initialization for Debtor Invoice Client
        /// </summary>
        /// <param name="debtorInvoiceClient">Invoice Client</param>
        /// <param name="api">Current api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        public DebtorInvoicePrintReport(DebtorInvoiceClient debtorInvoiceClient, CrudAPI api, CompanyLayoutType companyLayoutType = CompanyLayoutType.Invoice)
        {
            debtorInvoice = debtorInvoiceClient;
            crudApi = api;
            isRePrint = false;
            layoutType = companyLayoutType;
        }

        /// <summary>
        /// Initialization for Post Invoice and DebtorOrder Client
        /// </summary>
        /// <param name="postingResult">PostInvoice result</param>
        /// <param name="api">Current api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        /// <param name="orderClient">DebtorOrder/DebtorOffer client</param>
        public DebtorInvoicePrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType, DCOrder orderClient) : this(postingResult, api, companyLayoutType)
        {
            DebtorOrder = orderClient;
        }

        /// <summary>
        /// Method to Update Properties for Print Report
        /// </summary>
        /// <returns></returns>
        async public Task<bool> InstantiateFields()
        {
            try
            {
                var Comp = crudApi.CompanyEntity;
                var debtorInvoiceLineUserType = ReportUtil.GetUserType(typeof(DebtorInvoiceLines), Comp);
                var debtorInvoiceClientUserType = ReportUtil.GetUserType(typeof(DebtorInvoiceClient), Comp);
                DCInvoice dcInv;

                if (!isRePrint)
                {
                    var invApi = new InvoiceAPI(crudApi);
                    var invoiceLIneInstance = Activator.CreateInstance(debtorInvoiceLineUserType) as DebtorInvoiceLines;
                    dcInv = debtorInvoice;
                    InvTransInvoiceLines = (DebtorInvoiceLines[])await invApi.GetInvoiceLines(dcInv, invoiceLIneInstance);
                    PreviousAddressClient = isMultipleInvoicePrint ? LayoutPrintReport.GetPreviousAddressClientForInvoice(previousAddressLookup, debtorInvoice) :
                        await LayoutPrintReport.GetPreviousAddressClientForInvoice(dcInv, crudApi);
                }
                else
                {
                    dcInv = invoicePostingResult.Header;

                    if (invoicePostingResult?.Lines != null && invoicePostingResult.Lines.Count() > 0)
                    {
                        var linesCount = invoicePostingResult.Lines.Count();
                        var lines = invoicePostingResult.Lines;
                        InvTransInvoiceLines = Array.CreateInstance(debtorInvoiceLineUserType, linesCount) as DebtorInvoiceLines[];
                        int i = 0;
                        foreach (var invtrans in invoicePostingResult.Lines)
                        {
                            DebtorInvoiceLines debtorInvoiceLines;
                            if (invtrans.GetType() != debtorInvoiceLineUserType)
                            {
                                debtorInvoiceLines = Activator.CreateInstance(debtorInvoiceLineUserType) as DebtorInvoiceLines;
                                StreamingManager.Copy(invtrans, debtorInvoiceLines);
                            }
                            else
                                debtorInvoiceLines = invtrans as DebtorInvoiceLines;
                            InvTransInvoiceLines[i++] = debtorInvoiceLines;
                        }
                    }
                }

                //For Getting User-Fields for DebtorInvoice
                DebtorInvoiceClient debtorInvoiceClientUser;
                if (dcInv.GetType() != debtorInvoiceClientUserType)
                {
                    debtorInvoiceClientUser = Activator.CreateInstance(debtorInvoiceClientUserType) as DebtorInvoiceClient;
                    StreamingManager.Copy(dcInv, debtorInvoiceClientUser);
                }
                else
                    debtorInvoiceClientUser = dcInv as DebtorInvoiceClient;
                DebtorInvoice = debtorInvoiceClientUser;

                //For Getting User fields for Debtor
                var debtorClietUserType = ReportUtil.GetUserType(typeof(DebtorClient), Comp);
                var debtorClientUser = Activator.CreateInstance(debtorClietUserType) as DebtorClient;
                var dcCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.Debtor));
                var debtor = dcCache.Get(DebtorInvoice._DCAccount);
                if (debtor != null)
                    StreamingManager.Copy((UnicontaBaseEntity)debtor, debtorClientUser);
                else if (DebtorInvoice._Prospect != 0)
                {
                    //Check for Prospect. Create a Debtor for Prospect
                    var prosCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmProspect)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.CrmProspect));
                    var prospect = prosCache?.Get(DebtorInvoice._Prospect) as CrmProspect;
                    if (prospect != null)
                        debtorClientUser.CopyFrom(prospect);
                }

                if (PreviousAddressClient != null) //Setting the Previous Address if Exist for current invoice
                {
                    debtorClientUser._Name = PreviousAddressClient._Name;
                    debtorClientUser._Address1 = PreviousAddressClient._Address1;
                    debtorClientUser._Address2 = PreviousAddressClient._Address2;
                    debtorClientUser._Address3 = PreviousAddressClient._Address3;
                    debtorClientUser._City = PreviousAddressClient._City;
                    debtorClientUser._ZipCode = PreviousAddressClient._ZipCode;
                }

                //to Contact listing for the current debtor
                if (Comp.Contacts)
                {
                    var ContactsCache = Comp.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.Contact)).ConfigureAwait(false);
                    var contactCacheFilter = new ContactCacheFilter(ContactsCache, debtorClientUser.__DCType(), debtorClientUser._Account);
                    if (contactCacheFilter.Any())
                    {
                        try
                        {
                            debtorClientUser.Contacts = contactCacheFilter.Cast<ContactClient>().ToArray();
                        }
                        catch { }
                    }
                }
                Debtor = debtorClientUser;

                if (dcInv._Installation != null && Comp.GetCache(typeof(Uniconta.DataModel.WorkInstallation)) == null)
                    await Comp.LoadCache(typeof(Uniconta.DataModel.WorkInstallation), crudApi);

                UnicontaClient.Pages.DebtorOrders.SetDeliveryAdress(debtorInvoiceClientUser, debtorClientUser, crudApi);
                debtorInvoiceClientUser.SetInvoiceAddress(debtorClientUser);

                /*In case debtor order is null, fill from DCInvoice*/
                if (DebtorOrder == null)
                {
                    var debtorOrderUserType = ReportUtil.GetUserType(typeof(DebtorOrderClient), Comp);
                    var debtorOrderUser = Activator.CreateInstance(debtorOrderUserType) as DebtorOrderClient;
                    debtorOrderUser.CopyFrom(debtorInvoiceClientUser, debtorClientUser);
                    DebtorOrder = debtorOrderUser;
                }

                Company = Utility.GetCompanyClientUserInstance(Comp);

                var InvCache = Comp.GetCache(typeof(InvItem)) ?? await crudApi.LoadCache(typeof(InvItem));

                CompanyLogo = await Uniconta.ClientTools.Util.UtilDisplay.GetLogo(crudApi);

                Language lang = ReportGenUtil.GetLanguage(debtorClientUser, Comp);
                InvTransInvoiceLines = LayoutPrintReport.SetInvTransLines(DebtorInvoice, InvTransInvoiceLines, InvCache, crudApi, debtorInvoiceLineUserType, lang, false);

                //Setting ReportName and Version
                var invoiceNumber = DebtorInvoice._InvoiceNumber;
                var lineTotal = DebtorInvoice._LineTotal;
                IsCreditNote = (lineTotal < -0.0001d);

                ReportName = layoutType != CompanyLayoutType.Invoice ? layoutType.ToString() : invoiceNumber == 0 ? IsCreditNote ? "ProformaCreditNote" : "ProformaInvoice"
                    : IsCreditNote ? "Creditnote" : "Invoice";

                MessageClient = isMultipleInvoicePrint ? LayoutPrintReport.GetDebtorMessageClient(debtorMessageLookup, lang, GetDebtorEmailType()) :
                    await Utility.GetDebtorMessageClient(crudApi, lang, GetDebtorEmailType());

                var _LayoutGroup = DebtorInvoice._LayoutGroup ?? Debtor._LayoutGroup;
                if (_LayoutGroup != null)
                {
                    var cache = crudApi.GetCache(typeof(DebtorLayoutGroup)) ?? await crudApi.LoadCache(typeof(DebtorLayoutGroup));
                    var layClient = (DebtorLayoutGroup)cache.Get(_LayoutGroup);
                    layClient?.SetCompanyBank(Company);
                }

                return true;
            }
            catch (Exception ex)
            {
                crudApi?.ReportException(ex, "Error Occured in DebtorInvoicePrintReport");
                return false;
            }
        }

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
        internal void SetLookUpForDebtorMessageClients(DebtorMessagesClient[] debtorMessageClients)
        {
            isMultipleInvoicePrint = true;

            if (debtorMessageLookup == null)
                debtorMessageLookup = debtorMessageClients;
        }

        /// <summary>
        /// Setup lookup for PreviousAddress Clients
        /// </summary>
        /// <param name="preivousAddressClients"></param>
        internal void SetLookUpForPreviousAddressClients(DCPreviousAddressClient[] preivousAddressClients)
        {
            isMultipleInvoicePrint = true;
            if (previousAddressLookup == null)
                previousAddressLookup = preivousAddressClients;
        }
    }
}
