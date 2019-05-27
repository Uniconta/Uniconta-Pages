using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Uniconta.API.Project;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class ProjectMassUpdateGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(ProjectClient); } }
        public override IComparer GridSorting { get { return new ProjectSort(); } }

        public override bool Readonly { get { return false; } }
    }

    public partial class ProjectMassUpdatePage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.ProjectMassUpdatePage.ToString(); }
        }

        public override bool IsDataChaged { get { return false; } }

        SQLCache debtors;

        public ProjectMassUpdatePage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgProjectGrid;
            SetRibbonControl(localMenu, dgProjectGrid);
            dgProjectGrid.api = api;
            dgProjectGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgProjectGrid.ShowTotalSummary();

            var Comp = api.CompanyEntity;
            this.debtors = Comp.GetCache(typeof(ProjectClient));
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var order = dg.SelectedItem as DebtorOrderClient;
            if (order == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "Account")
                lookup.TableType = typeof(Uniconta.DataModel.Debtor);
            return lookup;
        }
        protected override void OnLayoutLoaded()
        {
            //setDim();
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectGrid.SelectedItem as DebtorOrderClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    dgProjectGrid.DeleteRow();
                    break;
                case "GenerateInvoice":
                    //GenerateInvoice();
                    break;
                case "GenerateInvoice2":
                    if (selectedItem != null)
                    {
                        var debtor = ClientHelper.GetRef(selectedItem.CompanyId, typeof(Debtor), selectedItem._DCAccount) as Debtor;
                        if (debtor != null)
                        {
                            var InvoiceAccount = selectedItem._InvoiceAccount ?? debtor._InvoiceAccount;
                            if (InvoiceAccount != null)
                                debtor = ClientHelper.GetRef(selectedItem.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                            if (debtor._PricesInclVat != selectedItem._PricesInclVat)
                            {
                                var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                                Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                                if (confirmationMsgBox != MessageBoxResult.OK)
                                    return;
                            }
                        }
                        // GenerateRecordInvoice(selectedItem);
                    }
                    break;
                case "OrderConfirmation":
                    //OrderConfirmation(false);
                    break;
                case "PackNote":
                    //OrderConfirmation(true);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        //List<InvoicePostingResult> invoicePosted;
        //private void GenerateInvoice()
        //{
        //    invoicePosted = new List<InvoicePostingResult>();

        //    UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true);
        //    GenrateInvoiceDialog.SetInvPrintPreview(printPreview);
        //    GenrateInvoiceDialog.Closed += async delegate
        //    {
        //        if (GenrateInvoiceDialog.DialogResult == true)
        //        {
        //            printPreview = GenrateInvoiceDialog.ShowInvoice;
        //            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
        //            busyIndicator.IsBusy = true;
        //            var InvoiceDate = GenrateInvoiceDialog.GenrateDate;
        //            InvoiceAPI Invapi = new InvoiceAPI(api);
        //            int cnt = 0;
        //            List<string> errorList = new List<string>();
        //            var dbVisibleOrders = dgProjectGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
        //            foreach (var dbOrder in dbVisibleOrders)
        //            {
        //                if (dbOrder._SubscriptionEnded != DateTime.MinValue && dbOrder._SubscriptionEnded < InvoiceDate)
        //                    continue;

        //                var result = await Invapi.PostInvoice(dbOrder, null, InvoiceDate,
        //                    0, GenrateInvoiceDialog.IsSimulation, new DebtorInvoiceClient(),
        //                    new DebtorInvoiceLines(),
        //                    GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.ShowInvoice, Emails: GenrateInvoiceDialog.Emails, OnlyToThisEmail: GenrateInvoiceDialog.sendOnlyToThisEmail);
        //                if (result != null && result.Err == 0)
        //                {
        //                    invoicePosted.Add(result);
        //                    cnt++;
        //                }
        //                else
        //                {
        //                    string error = string.Format("{0}:{1}", dbOrder.OrderNumber, Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
        //                    errorList.Add(error);
        //                }
        //            }

        //            busyIndicator.IsBusy = false;
        //            string updatedMsg = Uniconta.ClientTools.Localization.lookup("Succes");
        //            if (!GenrateInvoiceDialog.IsSimulation)
        //                updatedMsg = string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), cnt, Uniconta.ClientTools.Localization.lookup("DebtorOrders"));
        //            if (errorList.Count == 0)
        //            {
        //                if (UnicontaMessageBox.Show(updatedMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK) == MessageBoxResult.OK)
        //                {
        //                    if (GenrateInvoiceDialog.ShowInvoice)
        //                        ShowMultipleInvoicePreview();
        //                }
        //            }
        //            else
        //            {
        //                errorList[0] = string.Format("{0}/r/n{1}", updatedMsg, errorList[0]);
        //                CWErrorBox errorDialog = new CWErrorBox(errorList.ToArray(), true);
        //                errorDialog.Closed += delegate
        //                {
        //                    if (GenrateInvoiceDialog.ShowInvoice)
        //                        ShowMultipleInvoicePreview();
        //                };
        //                errorDialog.Show();
        //            }
        //        }
        //    };
        //    GenrateInvoiceDialog.Show();

        //}

        //List<InvoicePostingResult> confirmOrder;
        //static bool printPreview = true;
        //private void OrderConfirmation(bool isPacknote)
        //{
        //    confirmOrder = new List<InvoicePostingResult>();
        //    UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(false, isPacknote ? Uniconta.ClientTools.Localization.lookup("Packnote") : Uniconta.ClientTools.Localization.lookup("OrderConfirmation"), isShowInvoiceVisible: true, askForEmail: true, showInputforInvNumber: false, showInvoice: true, isShowUpdateInv: true);
        //    GenrateInvoiceDialog.SetInvPrintPreview(printPreview);
        //    GenrateInvoiceDialog.Closed += async delegate
        //    {
        //        if (GenrateInvoiceDialog.DialogResult == true)
        //        {
        //            printPreview = GenrateInvoiceDialog.ShowInvoice;
        //            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
        //            busyIndicator.IsBusy = true;
        //            var InvoiceDate = GenrateInvoiceDialog.GenrateDate;
        //            InvoiceAPI Invapi = new InvoiceAPI(api);
        //            int cnt = 0;
        //            List<string> errorList = new List<string>();
        //            var dgOrderVisible = dgProjectGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
        //            foreach (var dbOrder in dgOrderVisible)
        //            {
        //                var result = await Invapi.PostInvoice(dbOrder, null, GenrateInvoiceDialog.GenrateDate, 0,
        //                    !GenrateInvoiceDialog.UpdateInventory, new DebtorInvoiceClient(),
        //                    new DebtorInvoiceLines(),
        //                GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.ShowInvoice, isPacknote ? CompanyLayoutType.Packnote : CompanyLayoutType.OrderConfirmation, Emails: GenrateInvoiceDialog.Emails, OnlyToThisEmail: GenrateInvoiceDialog.sendOnlyToThisEmail);

        //                if (result != null && result.Err == 0 && GenrateInvoiceDialog.ShowInvoice)
        //                {
        //                    DebtorOrders.SetDeliveryAdress(result.Header, dbOrder.Debtor, api);
        //                    confirmOrder.Add(result);
        //                    cnt++;
        //                }
        //                else
        //                {
        //                    string error = string.Format("{0}:{1}", dbOrder.OrderNumber, Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
        //                    errorList.Add(error);
        //                }
        //            }
        //            busyIndicator.IsBusy = false;
        //            string updatedMsg = Uniconta.ClientTools.Localization.lookup("Succes");

        //            if (!GenrateInvoiceDialog.IsSimulation)
        //                updatedMsg = string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), cnt, Uniconta.ClientTools.Localization.lookup("DebtorOrders"));
        //            if (errorList.Count == 0)
        //            {
        //                if (UnicontaMessageBox.Show(updatedMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK) == MessageBoxResult.OK)
        //                {
        //                    if (GenrateInvoiceDialog.ShowInvoice)
        //                        ShowMultipleOfferToPrint(isPacknote);
        //                }
        //            }

        //            else
        //            {
        //                errorList[0] = string.Format("{0}/r/n{1}", updatedMsg, errorList[0]);
        //                CWErrorBox errorDialog = new CWErrorBox(errorList.ToArray(), true);
        //                errorDialog.Closed += delegate
        //                {
        //                    if (GenrateInvoiceDialog.ShowInvoice)
        //                        ShowMultipleOfferToPrint(isPacknote);
        //                };
        //                errorDialog.Show();
        //            }

        //        }
        //    };
        //    GenrateInvoiceDialog.Show();
        //}

        //        void ShowMultipleOfferToPrint(bool isPacknote)
        //        {
        //            var docName = isPacknote ? CompanyLayoutType.Packnote : CompanyLayoutType.OrderConfirmation;
        //            DispatcherTimer timer = new DispatcherTimer();
        //            timer.Interval = new TimeSpan(1000);
        //            timer.Tick += async delegate
        //            {
        //                timer.Stop();
        //#if !SILVERLIGHT
        //                List<IPrintReport> xtraReports = new List<IPrintReport>();
        //#elif SILVERLIGHT
        //                int top = 200;
        //                int left = 300;
        //                int invoiceCount = 1;
        //                int itemcount = confirmOrder.Count();
        //#endif
        //                foreach (var item in confirmOrder)
        //                {
        //                    if (item.Header == null) continue;
        //#if !SILVERLIGHT
        //                    var standardPrint = await ValidateStandardPrint(item, true, isPacknote);

        //                    if (standardPrint?.Report != null)
        //                        xtraReports.Add(standardPrint);
        //                }

        //                if (xtraReports.Count > 0)
        //                {
        //                    var reportName = Uniconta.ClientTools.Localization.lookup(docName.ToString());
        //                    AddDockItem(UnicontaTabs.StandardPrintReportPage, new object[] { xtraReports, reportName }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"),
        //                        reportName));
        //                }

        //#elif SILVERLIGHT
        //                    var deb = (DebtorInvoiceClient)item.Header;
        //                    var printHeader = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ShowPrint") + "-" + invoiceCount.ToString(), deb.Account, deb.Name);

        //                    object[] ob = new object[2];
        //                    ob[0] = item;
        //                    ob[1] = isPacknote ? CompanyLayoutType.Packnote : CompanyLayoutType.OrderConfirmation;

        //                    AddDockItem(TabControls.ProformaInvoice, ob, true, printHeader, null, new Point(top, left));
        //                    left = left - left / itemcount;
        //                    top = top - top / itemcount;
        //                    invoiceCount++;
        //                }
        //#endif
        //            };
        //            timer.Start();
        //        }

        //        void ShowMultipleInvoicePreview()
        //        {
        //            DispatcherTimer timer = new DispatcherTimer();
        //            timer.Interval = new TimeSpan(1000);
        //            timer.Tick += async delegate
        //            {
        //                timer.Stop();
        //#if !SILVERLIGHT
        //                List<IPrintReport> xtraReports = new List<IPrintReport>();
        //#elif SILVERLIGHT
        //                int top = 200;
        //                int left = 300;
        //                int invoiceCount = 1;
        //                int itemcount = invoicePosted.Count();
        //#endif
        //                foreach (var item in invoicePosted)
        //                {
        //                    if (item.Header == null) continue;
        //#if !SILVERLIGHT
        //                    var standardPrint = await ValidateStandardPrint(item);

        //                    if (standardPrint?.Report != null)
        //                        xtraReports.Add(standardPrint);
        //                }

        //                if (xtraReports.Count > 0)
        //                {
        //                    var dockname = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("Invoices"));
        //                    AddDockItem(TabControls.StandardPrintReportPage, new object[] { xtraReports, Uniconta.ClientTools.Localization.lookup("Invoices") }, dockname);
        //                }

        //#elif SILVERLIGHT
        //                    var deb = (DebtorInvoiceClient)item.Header;
        //                    var printHeader = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ShowPrint") + "-" + invoiceCount.ToString(), deb.Account, deb.Name);

        //                    object[] ob = new object[2];
        //                    ob[0] = item;
        //                    ob[1] = CompanyLayoutType.Invoice;

        //                    AddDockItem(TabControls.ProformaInvoice, ob, true, printHeader, null, new Point(top, left));
        //                    left = left - left / itemcount;
        //                    top = top - top / itemcount;
        //                    invoiceCount++;
        //                }
        //#endif
        //            };
        //            timer.Start();
        //        }

        //        private void GenerateRecordInvoice(DebtorOrderClient dbOrder)
        //        {
        //            InvoiceAPI Invapi = new InvoiceAPI(api);
        //            var debtor = dbOrder.Debtor;
        //            bool showSendByMail = true;
        //            if (debtor != null)
        //                showSendByMail = !string.IsNullOrEmpty(debtor.InvoiceEmail);
        //            string debtorName = debtor == null ? dbOrder._DCAccount : debtor.Name;
        //            UnicontaClient.Pages.CWGenerateInvoice GenrateInvoiceDialog = new UnicontaClient.Pages.CWGenerateInvoice(true, string.Empty, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName);
        //            GenrateInvoiceDialog.Closed += async delegate
        //            {
        //                if (GenrateInvoiceDialog.DialogResult == true)
        //                {
        //                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
        //                    busyIndicator.IsBusy = true;
        //                    var result = await Invapi.PostInvoice(dbOrder, null, GenrateInvoiceDialog.GenrateDate,
        //                        0, GenrateInvoiceDialog.IsSimulation, new DebtorInvoiceClient(),
        //                        new DebtorInvoiceLines(),
        //                        GenrateInvoiceDialog.SendByEmail, GenrateInvoiceDialog.ShowInvoice, Emails: GenrateInvoiceDialog.Emails, OnlyToThisEmail: GenrateInvoiceDialog.sendOnlyToThisEmail);
        //                    busyIndicator.IsBusy = false;
        //                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
        //                    if (result.ledgerRes.Err == ErrorCodes.Succes)
        //                    {
        //                        if (GenrateInvoiceDialog.ShowInvoice)
        //                        {
        //#if !SILVERLIGHT
        //                            var standardPrint = await ValidateStandardPrint(result);
        //                            if (standardPrint?.Report == null)
        //                            {
        //                                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("LayoutDoesNotExist"), Uniconta.ClientTools.Localization.lookup("Invoice")),
        //                                    Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
        //                                return;
        //                            }
        //                            var iReports = new IPrintReport[1] { standardPrint };
        //                            var invoiceNum = result.Header._InvoiceNumber;
        //                            var reportName = string.Format("{0}_{1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invoiceNum);
        //                            var dockName = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Preview"), string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Invoice"), invoiceNum));

        //                            AddDockItem(TabControls.StandardPrintReportPage, new object[] { iReports, reportName }, dockName);
        //#elif SILVERLIGHT
        //                            object[] ob = new object[2];
        //                            ob[0] = result;
        //                            ob[1] = CompanyLayoutType.Invoice;
        //                            AddDockItem(TabControls.ProformaInvoice, ob, true, Uniconta.ClientTools.Localization.lookup("Invoice"), null, new Point(200, 300));
        //#endif
        //                        }
        //                    }
        //                    else
        //                        UtilDisplay.ShowErrorCode(result.ledgerRes.Err);
        //                }
        //            };
        //            GenrateInvoiceDialog.Show();
        //        }

        //#if !SILVERLIGHT
        //        async private Task<IPrintReport> ValidateStandardPrint(InvoicePostingResult result, bool forOrderConfirmation = false, bool isPackNote = false)
        //        {
        //            CompanyLayoutType docName = CompanyLayoutType.Invoice;
        //            if (forOrderConfirmation)
        //                docName = isPackNote ? CompanyLayoutType.Packnote : CompanyLayoutType.OrderConfirmation;

        //            busyIndicator.IsBusy = true;
        //            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
        //            IPrintReport standardPrint = null;
        //            try
        //            {
        //                var debtorInvoicePrint = new DebtorInvoicePrintReport(result, api, docName);
        //                if (debtorInvoicePrint != null)
        //                {
        //                    await debtorInvoicePrint.InstantiateFields();
        //                    DebtorInvoiceReportClient standardDebtorInvoice = null;
        //                    if (forOrderConfirmation)
        //                    {
        //                        var docVersion = isPackNote ? Uniconta.ClientTools.Controls.Reporting.StandardReports.PackNote : Uniconta.ClientTools.Controls.Reporting.StandardReports.OrderConfirmation;
        //                        standardDebtorInvoice = new DebtorQCPReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DocumentClient.DocumentData,
        //                        debtorInvoicePrint.ReportName, (byte)docVersion, messageClient: debtorInvoicePrint.MessageClient);
        //                    }
        //                    else
        //                    {
        //                        standardDebtorInvoice = new DebtorInvoiceReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines,
        //                        debtorInvoicePrint.DocumentClient.DocumentData, debtorInvoicePrint.ReportName, isCreditNote: debtorInvoicePrint.IsCreditNote, messageClient: debtorInvoicePrint.MessageClient);
        //                    }

        //                    var standardReports = new IDebtorStandardReport[1] { standardDebtorInvoice };
        //                    standardPrint = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Invoice);
        //                    await standardPrint?.InitializePrint();
        //                }
        //                if (standardPrint?.Report == null)
        //                {
        //                    standardPrint = new LayoutPrintReport(api, result, CompanyLayoutType.Invoice);
        //                    await standardPrint?.InitializePrint();
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                busyIndicator.IsBusy = false;
        //                api.ReportException(ex, string.Format("ProjectMassUpdatePage.ValidateStandardPrint(), CompanyId={0}", api.CompanyId));
        //            }
        //            finally { busyIndicator.IsBusy = false; }

        //            return standardPrint;
        //        }
        //#endif

        //        void setDim()
        //{
        //    //UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        //}

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (this.debtors == null)
                this.debtors = await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);

            if (Comp.DeliveryAddress)
                LoadType(typeof(Uniconta.DataModel.WorkInstallation));
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }
    }
}
