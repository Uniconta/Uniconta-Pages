using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.API.DebtorCreditor;
using System.Windows.Threading;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using DevExpress.Xpf.Grid;
using DevExpress.Data;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class ProjectPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.Project; } }

        public ProjectPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public ProjectPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public ProjectPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }
        private void Init(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            if (master != null)
            {
                var dclin = master as DebtorOrderLineClient;
                if (dclin != null)
                    master = dclin.Order;
                dgProjectGrid.UpdateMaster(master);
            }
            localMenu.dataGrid = dgProjectGrid;
            LayoutControl = detailControl.layoutItems;
            dgProjectGrid.api = api;
            dgProjectGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgProjectGrid);
            dgProjectGrid.ShowTotalSummary();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
            dgProjectGrid.CustomSummary += DgProjectGrid_CustomSummary;
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += RootVisual_KeyDown;
#else
            this.PreviewKeyDown += RootVisual_KeyDown;
#endif
            this.BeforeClose += Project_BeforeClose;
        }

        private void Project_BeforeClose()
        {
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown -= RootVisual_KeyDown;
#else
            this.PreviewKeyDown -= RootVisual_KeyDown;
#endif
        }

        private void RootVisual_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F8 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                localMenu_OnItemClicked("PrTrans");
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void DgProjectGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectClient;
                    sumSales += row.SalesValue;
                    sumMargin += row.Margin;
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        sumMarginRatio = 100 * sumMargin / sumSales;
                        e.TotalValue = sumMarginRatio;
                    }
                    break;
            }
        }
        protected override void OnLayoutCtrlLoaded()
        {
            detailControl.api = api;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
            dgProjectGrid.Readonly = true;
        }
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.PrCategory), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Employee) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectGrid.SelectedItem as ProjectClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Project"), selectedItem._Number);
            switch (ActionType)
            {
                case "AddRow":
                    if (dgProjectGrid.masterRecords != null)
                    {
                        object[] arr = new object[] { api, dgProjectGrid.masterRecord };
                        AddDockItem(TabControls.ProjectPage2, arr, Uniconta.ClientTools.Localization.lookup("Project"), "Add_16x16.png");
                    }
                    else
                    {
                        AddDockItem(TabControls.ProjectPage2, api, Uniconta.ClientTools.Localization.lookup("Project"), "Add_16x16.png");
                    }
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    if (dgProjectGrid.masterRecords != null)
                    {
                        object[] arr = new object[] { selectedItem, dgProjectGrid.masterRecord };
                        AddDockItem(TabControls.ProjectPage2, arr, salesHeader);
                    }
                    else
                    {
                        var param = new object[] { selectedItem, true };
                        AddDockItem(TabControls.ProjectPage2, param, salesHeader);
                    }
                    break;
                case "PrTrans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTransactionPage, dgProjectGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._Number));
                    break;
                case "ProjectCategory":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectCategoryPage, dgProjectGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectCategory"), selectedItem._Number));
                    break;
                case "Budget":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectBudgetPage, dgProjectGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectBudget"), selectedItem._Name ?? salesHeader));
                    break;
                case "ProjectTransSum":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("ProjectCategorySum"), selectedItem._Number);
                        AddDockItem(TabControls.ProjectTransCategorySumPage, dgProjectGrid.syncEntity, header);
                    }
                    break;
                case "OnAccountInvoicing":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectOnAccountInvoiceLinePage, dgProjectGrid.syncEntity);
                    break;
                case "SalesOrder":
                    if (selectedItem != null)
                    {
                        salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SalesOrder"), selectedItem._DCAccount);
                        AddDockItem(TabControls.DebtorOrders, dgProjectGrid.syncEntity, salesHeader);
                    }
                    break;
                case "QuickInvoice":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CreateInvoicePage, selectedItem);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgProjectGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgProjectGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "EditAll":
                    if (dgProjectGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddLine":
                    dgProjectGrid.AddRow();
                    break;
                case "CopyRow":
                    if (copyRowIsEnabled)
                        dgProjectGrid.CopyRow();
                    else
                        CopyRecord(selectedItem);
                    break;
                case "DeleteRow":
                    dgProjectGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    dgProjectGrid.SaveData();
                    break;
                case "Offers":
                    if (selectedItem != null)
                    {
                        salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Offers"), selectedItem._DCAccount);
                        AddDockItem(TabControls.DebtorOffers, dgProjectGrid.syncEntity, salesHeader);
                    }
                    break;
                case "Register":
                    if (selectedItem != null)
                        WorkOrderInput(selectedItem);
                    break;
                case "ChartView":
#if SILVERLIGHT
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SilverlightSupport"), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
#else
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTaskPage, selectedItem, string.Format("{0}({1}): {2}", Uniconta.ClientTools.Localization.lookup("Tasks"), Uniconta.ClientTools.Localization.lookup("EnableChart")
                            , selectedItem._Number));
#endif
                    break;

                case "GridView":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTaskGridPage, selectedItem, string.Format("{0}({1}): {2}", Uniconta.ClientTools.Localization.lookup("Tasks"), Uniconta.ClientTools.Localization.lookup("DataGrid")
                            , selectedItem._Number));
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "PrintInvoice":
                    if (selectedItem != null)
                        PrintProjectInvoicePostingResult(selectedItem);
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                        CreateOrder(selectedItem);
                    break;
#if !SILVERLIGHT
                case "PrTransPivot":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTransPivotReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Pivot"), selectedItem._Name));
                    break;
#endif
                case "Invoices":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("ProjectInvoice"), selectedItem._DCAccount);
                        AddDockItem(TabControls.Invoices, dgProjectGrid.syncEntity, header);
                    }
                    break;
                case "ZeroInvoice":
                    if (selectedItem != null)
                        CreateZeroInvoice(selectedItem);
                    break;
                case "UndoDelete":
                    dgProjectGrid.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void CreateOrder(ProjectClient selectedItem)
        {
#if SILVERLIGHT
            var cwCreateOrder = new CWCreateOrderFromProject(api);
#else
            var cwCreateOrder = new UnicontaClient.Pages.CWCreateOrderFromProject(api);
            cwCreateOrder.DialogTableId = 2000000053;
#endif
            cwCreateOrder.Closed += async delegate
             {
                 if (cwCreateOrder.DialogResult == true)
                 {
                     busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                     busyIndicator.IsBusy = true;

                     var debtorOrderType = api.CompanyEntity.GetUserType(typeof(DebtorOrderClient)) ?? typeof(DebtorOrderClient);
                     var debtorOrderInstance = Activator.CreateInstance(debtorOrderType) as DebtorOrderClient;
                     var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                     var result = await invoiceApi.CreateOrderFromProject(debtorOrderInstance, selectedItem._Number, CWCreateOrderFromProject.InvoiceCategory, CWCreateOrderFromProject.GenrateDate,
                         CWCreateOrderFromProject.FromDate, CWCreateOrderFromProject.ToDate);
                     busyIndicator.IsBusy = false;
                     if (result != ErrorCodes.Succes)
                         UtilDisplay.ShowErrorCode(result);
                     else
                         ShowOrderLines(debtorOrderInstance);
                 }
             };
            cwCreateOrder.Show();
        }

        void CreateZeroInvoice(ProjectClient selectedItem)
        {
            var cwCreateZeroInvoice = new UnicontaClient.Pages.CwCreateZeroInvoice(api);
#if !SILVERLIGHT
            cwCreateZeroInvoice.DialogTableId = 2000000067;
#endif
            cwCreateZeroInvoice.Closed += async delegate
            {
                if (cwCreateZeroInvoice.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                    var result = await invoiceApi.CreateZeroInvoice(selectedItem._Number, cwCreateZeroInvoice.InvoiceCategory, cwCreateZeroInvoice.InvoiceDate, cwCreateZeroInvoice.ToDate,
                        cwCreateZeroInvoice.Simulate, new GLTransClientTotal());
                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    var ledgerRes = result.ledgerRes;
                    if (ledgerRes == null)
                        return;
                    if (result.Err != ErrorCodes.Succes)
                        Utility.ShowJournalError(ledgerRes, dgProjectGrid);
                    else if (cwCreateZeroInvoice.Simulate && ledgerRes.SimulatedTrans != null)
                        AddDockItem(TabControls.SimulatedTransactions, ledgerRes.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                    else
                    {
                        var msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), ledgerRes.JournalPostedlId);
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                    }
                }
            };
            cwCreateZeroInvoice.Show();
        }

        private void ShowOrderLines(DebtorOrderClient order)
        {
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

        private void PrintProjectInvoicePostingResult(ProjectClient selectedItem)
        {
            var generateInvoiceDialog = new CWProjectGenerateInvoice(api, BasePage.GetSystemDefaultDate(), showToFromDate: true, showEmailList: true, showSendOnlyEmailCheck: true);
#if SILVERLIGHT
            generateInvoiceDialog.Height = 360.0d;
#else
            generateInvoiceDialog.DialogTableId = 2000000047;
#endif
            generateInvoiceDialog.Closed += async delegate
            {
                if (generateInvoiceDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
#if !SILVERLIGHT
                    var invoicePostingResult = new UnicontaClient.Pages.InvoicePostingPrintGenerator(
#elif SILVERLIGHT
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
                            msg = string.Format(Uniconta.ClientTools.Localization.lookup("InvoiceHasBeenGenerated"), invoicePostingResult.PostingResult.Header.InvoiceNum);
                            msg = string.Format("{0}{1}{2} {3}", msg, Environment.NewLine, Uniconta.ClientTools.Localization.lookup("LedgerVoucher"), invoicePostingResult.PostingResult.Header._Voucher);

#if !SILVERLIGHT
                            if (generateInvoiceDialog.GenerateOIOUBLClicked)
                                DebtorOrders.GenerateOIOXml(api, invoicePostingResult.PostingResult);
#endif
                        }
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgProjectGrid);
                }
            };
            generateInvoiceDialog.Show();
        }

        void CopyRecord(ProjectClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var project = Activator.CreateInstance(selectedItem.GetType()) as ProjectClient;
            StreamingManager.Copy(selectedItem, project);
            var parms = new object[2] { project, false };
            AddDockItem(TabControls.ProjectPage2, parms, Uniconta.ClientTools.Localization.lookup("Project"), "Add_16x16.png");
        }

        async void WorkOrderInput(ProjectClient project)
        {
            var journals = await api.Query<ProjectJournalClient>(api.session.User);
            ProjectJournalClient journal;
            if (journals != null && journals.Length > 0)
                journal = journals[0];
            else
            {
                var employeeCache = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Employee));
                var employee = ((Uniconta.DataModel.Employee[])employeeCache.GetNotNullArray).Where(e => e._Uid == api.session.Uid).FirstOrDefault();
                if (employee == null)
                {
                    var msg = string.Format(Uniconta.ClientTools.Localization.lookup("UserNotEmployee"), api.session.User._LoginId);
                    UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    return;
                }
                journal = new ProjectJournalClient();
                journal._Journal = Uniconta.ClientTools.Localization.lookup("Default");
                journal._Employee = employee.KeyStr;
                var err = await api.Insert(journal);
                if (err != ErrorCodes.Succes)
                {
                    UtilDisplay.ShowErrorCode(err);
                    return;
                }
            }
            var header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Journal"), journal._Journal);
            var param = new object[2] { journal, project };
            AddDockItem(TabControls.WorkOrderInput, param, header);
        }

        bool copyRowIsEnabled = false;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var iBase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (iBase == null) return;

            if (dgProjectGrid.Readonly)
            {
                dgProjectGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgProjectGrid);
                iBase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                editAllChecked = false;
                HasNotes.Visible = false;
                HasDocs.Visible = false;
                copyRowIsEnabled = true;
            }
            else
            {
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                    {
                        if (confirmationDialog.DialogResult == null)
                            return;

                        switch (confirmationDialog.ConfirmationResult)
                        {
                            case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                var err = await dgProjectGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                break;
                        }
                        editAllChecked = true;
                        HasNotes.Visible = true;
                        HasDocs.Visible = true;
                        dgProjectGrid.Readonly = true;
                        dgProjectGrid.tableView.CloseEditor();
                        iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    HasNotes.Visible = true;
                    HasDocs.Visible = true;
                    dgProjectGrid.Readonly = true;
                    dgProjectGrid.tableView.CloseEditor();
                    iBase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    copyRowIsEnabled = false;
                }
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                if (editAllChecked)
                    return false;
                else
                    return dgProjectGrid.HasUnsavedData;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ProjectPage2)
                dgProjectGrid.UpdateItemSource(argument);
            if (dgProjectGrid.Visibility == Visibility.Collapsed && screenName == TabControls.ProjectPage2)
                detailControl.Refresh(argument, dgProjectGrid.SelectedItem);
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgProjectGrid.UpdateItemSource(argument);
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var projectClient = (sender as Image).Tag as ProjectClient;
            if (projectClient != null)
                AddDockItem(TabControls.UserDocsPage, dgProjectGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var projectClient = (sender as Image).Tag as ProjectClient;
            if (projectClient != null)
                AddDockItem(TabControls.UserNotesPage, dgProjectGrid.syncEntity);
        }

#if !SILVERLIGHT
        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var projectClient = (sender as TextBlock).Tag as ProjectClient;
            if (projectClient != null)
            {
                var mail = string.Concat("mailto:", projectClient._Email);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
        }
#endif
    }
}