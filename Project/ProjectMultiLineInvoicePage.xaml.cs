using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.DataModel;

#if !SILVERLIGHT
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
#elif SILVERLIGHT
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
#endif
{
    public class ProjectMultiLineInvoiceGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool Readonly { get { return false; } }
    }
    /// <summary>
    /// Interaction logic for ProjectMultiLinePage.xaml
    /// </summary>
    public partial class ProjectMultiLineInvoicePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectMultiLineInvoicePage; } }

        public ProjectMultiLineInvoicePage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectMultiLineGrid);
            dgProjectMultiLineGrid.api = api;
            dgProjectMultiLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgProjectMultiLineGrid.ShowTotalSummary();
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectMultiLineGrid.SelectedItem as ProjectClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem == null)
                        return;

                    string projectHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Project"), selectedItem._Number);
                    if (dgProjectMultiLineGrid.masterRecords != null)
                    {
                        object[] arr = new object[] { selectedItem, dgProjectMultiLineGrid.masterRecord };
                        AddDockItem(TabControls.ProjectPage2, arr, projectHeader);
                    }
                    else
                    {
                        var param = new object[2] { selectedItem, true };
                        AddDockItem(TabControls.ProjectPage2, param, projectHeader);
                    }
                    break;
                case "DeleteRow":
                    dgProjectMultiLineGrid.DeleteRow();
                    break;
                case "GenerateInvoice":
                    GenerateInvoice();
                    break;
                case "GenerateInvoice2":
                    if (selectedItem == null) return;

                    GenerateSelectedInvoice(selectedItem);
                    break;
                case "CreateOrder":
                    CreateMulitOrder();
                    break;
                case "CreateOrder2":
                    if (selectedItem == null) return;

                    CreateMulitOrder(false);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void CreateMulitOrder(bool IsMultiOrder = true)
        {
            var cwCreateOrder = new CWCreateOrderFromProject(api);
#if !SILVERLIGHT
            cwCreateOrder.DialogTableId = 2000000052;
#endif
            cwCreateOrder.Closed += async delegate
            {
                if (cwCreateOrder.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    busyIndicator.IsBusy = true;

                    IList projectList = null;

                    if (!IsMultiOrder)
                    {
                        var Arr = Array.CreateInstance(dgProjectMultiLineGrid.TableTypeUser, 1);
                        Arr.SetValue(dgProjectMultiLineGrid.SelectedItem, 0);
                        projectList = Arr;
                    }
                    else
                        projectList = dgProjectMultiLineGrid.GetVisibleRows();

                    var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                    var debtorOrderType = api.CompanyEntity.GetUserType(typeof(DebtorOrderClient)) ?? typeof(DebtorOrderClient);
                    var errorlist = new List<string>();
                    DebtorOrderClient debtorOrderInstance = null;
                    foreach (var proj in projectList)
                    {
                        var selectedItem = proj as ProjectClient;
                        debtorOrderInstance = Activator.CreateInstance(debtorOrderType) as DebtorOrderClient;
                        var result = await invoiceApi.CreateOrderFromProject(debtorOrderInstance, selectedItem._Number, CWCreateOrderFromProject.InvoiceCategory, CWCreateOrderFromProject.GenrateDate,
                            CWCreateOrderFromProject.FromDate, CWCreateOrderFromProject.ToDate);

                        if (result != Uniconta.Common.ErrorCodes.Succes)
                        {
                            var error = string.Format("{0}: {1} - {2}", Uniconta.ClientTools.Localization.lookup("Project"), selectedItem._Number, Uniconta.ClientTools.Localization.lookup(result.ToString()));
                            errorlist.Add(error);
                        }
                    }
                    busyIndicator.IsBusy = false;

                    if (errorlist.Count > 1)
                    {
                        var errorDialog = new CWErrorBox(errorlist.ToArray(), true);
                        errorDialog.Show();
                    }
                    else if (!IsMultiOrder && errorlist.Count == 0)
                        ShowOrderLines(debtorOrderInstance);
                    else if (errorlist.Count == 1)
                        UnicontaMessageBox.Show(errorlist[0], Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);

                    else
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SalesOrderCreated"), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                }
            };
            cwCreateOrder.Show();
        }

        private void ShowOrderLines(DebtorOrderClient order)
        {
            if (order == null) return;

            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup("SalesOrderCreated"), Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Orderline")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(TabControls.DebtorOrderLines, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, order._DCAccount));
                        break;
                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();
        }

        private void GenerateSelectedInvoice(ProjectClient selectedItem)
        {
            var generateInvoiceDialog = new CWProjectGenerateInvoice(api, BasePage.GetSystemDefaultDate(), showToFromDate: true, showEmailList: true, showSendOnlyEmailCheck: true);
#if SILVERLIGHT
            generateInvoiceDialog.Height = 360.0d;
#else
            generateInvoiceDialog.DialogTableId = 2000000051;
#endif
            generateInvoiceDialog.Closed += async delegate
            {
                if (generateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
#if !SILVERLIGHT
                    var invoicePostingResult = new UnicontaClient.Pages.InvoicePostingPrintGenerator(
#else
                    var invoicePostingResult = new InvoicePostingPrintGenerator(
#endif
                    api, this, selectedItem, null, generateInvoiceDialog.GenrateDate, generateInvoiceDialog.IsSimulation, CompanyLayoutType.Invoice, (generateInvoiceDialog.ShowInvoice || generateInvoiceDialog.InvoiceQuickPrint), generateInvoiceDialog.InvoiceQuickPrint,
                    generateInvoiceDialog.NumberOfPages, generateInvoiceDialog.SendByEmail, generateInvoiceDialog.EmailList, generateInvoiceDialog.SendOnlyEmail, generateInvoiceDialog.FromDate, generateInvoiceDialog.ToDate,
                    generateInvoiceDialog.InvoiceCategory, null);

                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        Task reloadTask = null;
                        if (!generateInvoiceDialog.IsSimulation)
                            reloadTask = Filter();

                        string msg = Uniconta.ClientTools.Localization.lookup("InvoiceProposal");
                        if (invoicePostingResult.PostingResult.Header._InvoiceNumber != 0)
                        {
                            msg = string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceHasBeenGenerated"), invoicePostingResult.PostingResult.Header._InvoiceNumber);
                            msg = string.Format("{0}{1}{2} {3}", msg, Environment.NewLine, Uniconta.ClientTools.Localization.lookup("LedgerVoucher"), invoicePostingResult.PostingResult.Header._Voucher);

#if !SILVERLIGHT
                            if (generateInvoiceDialog.GenerateOIOUBLClicked)
                                DebtorOrders.GenerateOIOXml(api, invoicePostingResult.PostingResult);
#endif
                        }
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgProjectMultiLineGrid);
                }
            };
            generateInvoiceDialog.Show();
        }

#if !SILVERLIGHT
        Dictionary<InvoicePostingResult, ProjectClient> invoicePosted;
#elif SILVERLIGHT
        List<InvoicePostingResult> invoicePosted;
#endif
        private void GenerateInvoice()
        {
#if !SILVERLIGHT
            invoicePosted = new Dictionary<InvoicePostingResult, ProjectClient>();
#elif SILVERLIGHT
            invoicePosted = new List<InvoicePostingResult>();
#endif
            var generateProjectInvoice = new CWProjectGenerateInvoice(api, BasePage.GetSystemDefaultDate(), true, true, false, true, false, true, true, true);
#if SILVERLIGHT
            generateProjectInvoice.Height = 360.0d;
#else
            generateProjectInvoice.DialogTableId = 2000000050;
#endif
            generateProjectInvoice.Closed += async delegate
            {
                if (generateProjectInvoice.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;

                    var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                    int count = 0;
                    var errorlist = new List<string>();
                    var projects = dgProjectMultiLineGrid.GetVisibleRows() as IEnumerable<ProjectClient>;

                    foreach (var proj in projects)
                    {
                        var result = await invoiceApi.PostInvoice(proj, null, generateProjectInvoice.GenrateDate, generateProjectInvoice.IsSimulation, generateProjectInvoice.FromDate, generateProjectInvoice.ToDate,
                            generateProjectInvoice.InvoiceCategory, new DebtorInvoiceClient(), new InvTransClient(), generateProjectInvoice.SendByEmail, true, Uniconta.DataModel.CompanyLayoutType.Invoice,
                            generateProjectInvoice.EmailList, generateProjectInvoice.SendOnlyEmail, new GLTransClient(), null);

                        if (result != null && result.Err == 0)
                        {
#if !SILVERLIGHT
                            invoicePosted.Add(result, proj);
#elif SILVERLIGHT
                            invoicePosted.Add(result);
#endif
                            count++;
                        }
                        else
                        {
                            var error = string.Format("{0}:{1}", proj._Number, Uniconta.ClientTools.Localization.lookup(result.Err.ToString()));
                            errorlist.Add(error);
                        }
                    }
                    busyIndicator.IsBusy = false;
                    string updateMsg = Uniconta.ClientTools.Localization.lookup("Success");

                    if (!generateProjectInvoice.IsSimulation)
                        updateMsg = string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), count, Uniconta.ClientTools.Localization.lookup("Project"));
                    if (errorlist.Count == 0)
                    {
                        if (UnicontaMessageBox.Show(updateMsg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK) == MessageBoxResult.OK)
                            if (generateProjectInvoice.ShowInvoice)
                                ShowMultipleInvoicePreview();
                    }
                    else
                    {
                        var errorDialog = new CWErrorBox(errorlist.ToArray(), true);
                        errorDialog.Closed += delegate
                        {
                            ShowMultipleInvoicePreview();
                        };
                        errorDialog.Show();
                    }
                }
            };
            generateProjectInvoice.Show();
        }

        private void ShowMultipleInvoicePreview()
        {
            var timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(1000);
            timer.Tick += async delegate
            {
                timer.Stop();
#if !SILVERLIGHT
                var xtraReports = new List<IPrintReport>();
#elif SILVERLIGHT
                int top = 200;
                int left = 300;
                int invoiceCount = 1;
                int itemCount = invoicePosted.Count;
#endif

                foreach (var invPost in invoicePosted)
                {
#if !SILVERLIGHT
                    if (invPost.Key.Header == null) continue;
                    var standardPrint = await ValidateStandardPrint(invPost.Key, invPost.Value, CompanyLayoutType.Invoice);

                    if (standardPrint?.Report != null)
                        xtraReports.Add(standardPrint);
                }

                if (xtraReports.Count > 0)
                {
                    var dockname = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("Invoices"));
                    AddDockItem(TabControls.StandardPrintReportPage, new object[] { xtraReports, Uniconta.ClientTools.Localization.lookup("Invoices") }, dockname);
                }
#elif SILVERLIGHT

                    if (invPost.Header == null) continue;

                    var deb = (DebtorInvoiceClient)invPost.Header;
                    var printHeader = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ShowPrint") + "-" + invoiceCount.ToString(), deb.Account, deb.Name);

                    object[] ob = new object[2];
                    ob[0] = invPost;
                    ob[1] = CompanyLayoutType.Invoice;

                    AddDockItem(TabControls.ProformaInvoice, ob, true, printHeader, null, new Point(top, left));
                    left = left - left / itemCount;
                    top = top - top / itemCount;
                    invoiceCount++;
                }
#endif
            };
            timer.Start();
        }

#if !SILVERLIGHT
        async private Task<IPrintReport> ValidateStandardPrint(InvoicePostingResult invoicePostingResult, ProjectClient project, CompanyLayoutType layoutType)
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = string.Format("{0}..{1}: {2}", Uniconta.ClientTools.Localization.lookup("LoadingMsg"), Uniconta.ClientTools.Localization.lookup("Project"), project?._Number);
            IPrintReport standardPrint = null;

            try
            {
                var debtorInvoicePrint = new DebtorInvoicePrintReport(invoicePostingResult, api, layoutType);
                var isInitializedSuccess = await debtorInvoicePrint.InstantiateFields();

                if (isInitializedSuccess)
                {
                    var standardDebtorInvoice = new DebtorInvoiceReportClient(debtorInvoicePrint.Company, debtorInvoicePrint.Debtor, debtorInvoicePrint.DebtorInvoice, debtorInvoicePrint.InvTransInvoiceLines, debtorInvoicePrint.DebtorOrder,
                        debtorInvoicePrint.CompanyLogo, debtorInvoicePrint.ReportName, isCreditNote: debtorInvoicePrint.IsCreditNote, messageClient: debtorInvoicePrint.MessageClient);
                    standardPrint = new StandardPrintReport(api, new [] { standardDebtorInvoice }, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Invoice);
                    standardPrint = new LayoutPrintReport(api, invoicePostingResult, layoutType);
                }
                await standardPrint.InitializePrint();

                if (standardPrint?.Report == null)
                {
                    standardPrint = new LayoutPrintReport(api, invoicePostingResult, layoutType);
                    await standardPrint.InitializePrint();
                }
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("ProjectMultiLineInvoicePage.ValidateStandardPrint(), CompanyId={0}", api.CompanyId));
            }
            finally { busyIndicator.IsBusy = false; }

            return standardPrint;
        }

#endif
    }
}
