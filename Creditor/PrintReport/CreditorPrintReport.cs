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
    public class CreditorPrintReport
    {
        public CreditorClient Creditor { get; private set; }
        public CompanyClient Company { get; private set; }
        public CreditorInvoiceClient CreditorInvoice { get; private set; }
        public InvTransInvoice[] InvTransInvoiceLines { get; private set; }
        public byte[] CompanyLogo { get; private set; }
        public string ReportName { get; private set; }
        public CreditorOrderClient CreditorOrder { get; private set; }
        public bool IsCreditNote { get; private set; }
        private bool isRePrint;
        private readonly CrudAPI crudApi;
        private InvoicePostingResult invoicePostingResult;
        private CompanyLayoutType layoutType;

        /// <summary>
        /// Initialization for Post invoice
        /// </summary>
        /// <param name="invPostingResult">Result from postinvoice</param>
        /// <param name="api">Api instacce</param>
        /// <param name="companyLayoutType">Layout type</param>
        public CreditorPrintReport(InvoicePostingResult invPostingResult, CrudAPI api, CompanyLayoutType companyLayoutType)
        {
            invoicePostingResult = invPostingResult;
            crudApi = api;
            isRePrint = true;
            layoutType = companyLayoutType;
        }

        /// <summary>
        /// Initialization for CreditorInvoice client
        /// </summary>
        /// <param name="creditorInvoiceClient">Invoice client </param>
        /// <param name="api">Api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        public CreditorPrintReport(CreditorInvoiceClient creditorInvoiceClient, CrudAPI api, CompanyLayoutType companyLayoutType = CompanyLayoutType.PurchaseInvoice)
        {
            CreditorInvoice = creditorInvoiceClient;
            crudApi = api;
            isRePrint = false;
            layoutType = companyLayoutType;
        }

        /// <summary>
        /// Intialization of CreditorInvoice with Creditor Order
        /// </summary>
        /// <param name="postingResult">Invoice posting result</param>
        /// <param name="api">Api instance</param>
        /// <param name="companyLayoutType">Layout type</param>
        /// <param name="orderClient">Creditor Order client instance</param>
        public CreditorPrintReport(InvoicePostingResult postingResult, CrudAPI api, CompanyLayoutType companyLayoutType, CreditorOrderClient orderClient) : this(postingResult, api, companyLayoutType)
        {
            CreditorOrder = orderClient;
        }

        async public Task<bool> InstantiaeFields()
        {
            try
            {
                var Comp = crudApi.CompanyEntity;
                var creditorInvoiceLineUserType = ReportUtil.GetUserType(typeof(CreditorInvoiceLines), Comp);
                var creditorInvoiceUserType = ReportUtil.GetUserType(typeof(CreditorInvoiceClient), Comp);
                if (!isRePrint)
                {
                    var invApi = new InvoiceAPI(crudApi);
                    var invoiceLIneInstance = Activator.CreateInstance(creditorInvoiceLineUserType) as CreditorInvoiceLines;
                    InvTransInvoiceLines = (CreditorInvoiceLines[])await invApi.GetInvoiceLines(CreditorInvoice, invoiceLIneInstance);
                }
                else
                {
                    //for Gettting user firlds for Creditor Invoice
                    var dcInvoice = (DCInvoiceClient)invoicePostingResult.Header;
                    CreditorInvoice = new CreditorInvoiceClient();
                    StreamingManager.Copy(dcInvoice, CreditorInvoice);

                    var linesCount = invoicePostingResult.Lines.Count();
                    if (linesCount > 0)
                    {
                        var lines = invoicePostingResult.Lines;
                        InvTransInvoiceLines = Array.CreateInstance(creditorInvoiceLineUserType, linesCount) as CreditorInvoiceLines[];
                        int i = 0;
                        foreach (var invtrans in invoicePostingResult.Lines)
                        {
                            CreditorInvoiceLines creditorInvoiceLines;
                            if (invtrans.GetType() != creditorInvoiceLineUserType)
                            {
                                creditorInvoiceLines = Activator.CreateInstance(creditorInvoiceLineUserType) as CreditorInvoiceLines;
                                StreamingManager.Copy(invtrans, creditorInvoiceLines);
                            }
                            else
                                creditorInvoiceLines = invtrans as CreditorInvoiceLines;
                            InvTransInvoiceLines[i++] = creditorInvoiceLines;
                        }
                    }
                }

                //For Getting User-Fields for CreditorInvoice
                CreditorInvoiceClient creditorInvoiceClientUser;
                if (CreditorInvoice.GetType() != creditorInvoiceUserType)
                {
                    creditorInvoiceClientUser = Activator.CreateInstance(creditorInvoiceUserType) as CreditorInvoiceClient;
                    StreamingManager.Copy(CreditorInvoice, creditorInvoiceClientUser);
                }
                else
                    creditorInvoiceClientUser = CreditorInvoice as CreditorInvoiceClient;
                CreditorInvoice = creditorInvoiceClientUser;

                //for Gettting user fields for Creditor
                var dcCahce = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), crudApi);
                var cred = dcCahce.Get(CreditorInvoice._DCAccount);

                var creditorUserType = ReportUtil.GetUserType(typeof(CreditorClient), Comp);
                if (creditorUserType != cred?.GetType())
                {
                    var creditorClientUser = Activator.CreateInstance(creditorUserType) as CreditorClient;
                    if (cred != null)
                        StreamingManager.Copy((UnicontaBaseEntity)cred, creditorClientUser);
                    Creditor = creditorClientUser;
                }
                else
                    Creditor = cred as CreditorClient;

                UnicontaClient.Pages.DebtorOrders.SetDeliveryAdress(creditorInvoiceClientUser, Creditor, crudApi);

                Company = Utility.GetCompanyClientUserInstance(Comp);

                var InvCache = Comp.GetCache(typeof(InvItem)) ?? await Comp.LoadCache(typeof(InvItem), crudApi);

                CompanyLogo = await Uniconta.ClientTools.Util.UtilDisplay.GetLogo(crudApi);

                Language lang = ReportGenUtil.GetLanguage(Creditor, Comp);
                InvTransInvoiceLines = LayoutPrintReport.SetInvTransLines(CreditorInvoice, InvTransInvoiceLines, InvCache, creditorInvoiceLineUserType, lang, false);

                var lineTotal = CreditorInvoice._LineTotal;
                IsCreditNote = CreditorInvoice._LineTotal < -0.0001d && layoutType == CompanyLayoutType.PurchaseInvoice;
                ReportName = IsCreditNote ? "CreditNote" : layoutType.ToString();

                return true;
            }
            catch (Exception ex)
            {
                crudApi.ReportException(ex, "Error Occured in CreditorPrintReport");
                return false;
            }
        }


    }
}
