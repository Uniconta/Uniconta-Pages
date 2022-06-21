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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectMultiLineInvoiceGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
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
                    if (selectedItem != null)
                    {
                        string projectHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Project"), selectedItem._Number);
                        if (dgProjectMultiLineGrid.masterRecords != null)
                            AddDockItem(TabControls.ProjectPage2, new object[] { selectedItem, dgProjectMultiLineGrid.masterRecord }, projectHeader);
                        else
                            AddDockItem(TabControls.ProjectPage2, new object[] { selectedItem, IdObject.get(true) }, projectHeader);
                    }
                    break;
                case "DeleteRow":
                    dgProjectMultiLineGrid.RemoveFocusedRowFromGrid();
                    break;
                case "GenerateInvoice":
                    GenerateInvoice();
                    break;
                case "GenerateInvoice2":
                    if (selectedItem != null)
                        GenerateSelectedInvoice(selectedItem);
                    break;
                case "CreateOrder":
                    CreateMulitOrder();
                    break;
                case "CreateOrder2":
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
            cwCreateOrder.DialogTableId = 2000000052;
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
                    var debtorOrderType = api.CompanyEntity.GetUserTypeNotNull(typeof(ProjectInvoiceProposalClient));
                    var errorlist = new List<string>();
                    ProjectInvoiceProposalClient debtorOrderInstance = null;
                    foreach (var proj in projectList)
                    {
                        var selectedItem = proj as ProjectClient;
                        debtorOrderInstance = Activator.CreateInstance(debtorOrderType) as ProjectInvoiceProposalClient;
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
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvProposalCreated"), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                }
            };
            cwCreateOrder.Show();
        }

        private void ShowOrderLines(ProjectInvoiceProposalClient order)
        {
            if (order == null) return;

            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup("InvProposalCreated"), Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Orderline")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(TabControls.ProjInvoiceProposalLine, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, order._DCAccount));
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
            generateInvoiceDialog.DialogTableId = 2000000051;
            generateInvoiceDialog.Closed += async delegate
            {
                if (generateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var invoicePostingResult = new UnicontaClient.Pages.InvoicePostingPrintGenerator(
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
                            msg = string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceHasBeenGenerated"), Uniconta.ClientTools.Localization.lookup("Invoice"), invoicePostingResult.PostingResult.Header.InvoiceNum);
                            msg = string.Format("{0}{1}{2} {3}", msg, Environment.NewLine, Uniconta.ClientTools.Localization.lookup("LedgerVoucher"), invoicePostingResult.PostingResult.Header._Voucher);
                            if (generateInvoiceDialog.GenerateOIOUBLClicked && api.CompanyEntity._OIOUBLSendOnServer == false)
                                DebtorOrders.GenerateOIOXml(api, invoicePostingResult.PostingResult);
                        }
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgProjectMultiLineGrid);
                }
            };
            generateInvoiceDialog.Show();
        }

        Dictionary<InvoicePostingResult, ProjectClient> invoicePosted;
        bool IsInvoiceTaskInProgress = false;

        private void GenerateInvoice()
        {
            if (IsInvoiceTaskInProgress)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateInBackground"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            invoicePosted = new Dictionary<InvoicePostingResult, ProjectClient>();
            var generateProjectInvoice = new CWProjectGenerateInvoice(api, BasePage.GetSystemDefaultDate(), true, true, false, true, false, true, true, true);
            generateProjectInvoice.DialogTableId = 2000000050;
            generateProjectInvoice.Closed += async delegate
            {
                if (generateProjectInvoice.DialogResult == true)
                {
                    try
                    {
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                        busyIndicator.IsBusy = true;
                        MultiDocumentTaskProgress(true);
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
                                invoicePosted.Add(result, proj);
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
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                    }
                    finally
                    {
                        MultiDocumentTaskProgress(false);
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
                var xtraReports = new List<IPrintReport>();
                foreach (var invPost in invoicePosted)
                {
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
            };
            timer.Start();
        }

        private void MultiDocumentTaskProgress(bool status)
        {
            IsInvoiceTaskInProgress = status;

            if (status)
                ribbonControl?.DisableButtons(new string[] { "GenerateInvoice" });
            else
                ribbonControl?.EnableButtons(new string[] { "GenerateInvoice" });
        }

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
                    standardPrint = new StandardPrintReport(api, new[] { standardDebtorInvoice }, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Invoice);
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
    }
}
