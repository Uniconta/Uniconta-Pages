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
using Uniconta.API.Project;
using UnicontaClient.Controls.Dialogs;

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
            var projects = api.GetCache(typeof(Uniconta.DataModel.Project));
            TestDebtorReload(false, projects?.GetNotNullArray as IEnumerable<Uniconta.DataModel.Project>);

            var Comp = api.CompanyEntity;
            var lst = new List<Type>(15) { typeof(Uniconta.DataModel.PrType), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.ProjectGroup), typeof(Uniconta.DataModel.PrStandard),
                typeof(Uniconta.DataModel.PrCategory), typeof(Uniconta.DataModel.PrWorkSpace) };
            if (Comp.Contacts)
                lst.Add(typeof(Uniconta.DataModel.Contact));
            if (Comp.NumberOfDimensions >= 1)
                lst.Add(typeof(Uniconta.DataModel.GLDimType1));
            if (Comp.NumberOfDimensions >= 2)
                lst.Add(typeof(Uniconta.DataModel.GLDimType2));
            if (Comp.NumberOfDimensions >= 3)
                lst.Add(typeof(Uniconta.DataModel.GLDimType3));
            if (Comp.NumberOfDimensions >= 4)
                lst.Add(typeof(Uniconta.DataModel.GLDimType4));
            if (Comp.NumberOfDimensions >= 5)
                lst.Add(typeof(Uniconta.DataModel.GLDimType5));
            lst.Add(typeof(Uniconta.DataModel.Employee));
            lst.Add(typeof(Uniconta.DataModel.Debtor));
            if (Comp.DeliveryAddress)
                lst.Add(typeof(Uniconta.DataModel.WorkInstallation));
            LoadType(lst);
        }

        async void TestDebtorReload(bool refresh, IEnumerable<Uniconta.DataModel.Project> lst)
        {
            if (lst != null && lst.Count() > 0)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
                if (cache != null)
                {
                    bool reload = false;
                    var Contacts = api.GetCache(typeof(Uniconta.DataModel.Contact));
                    foreach (var rec in lst)
                    {
                        if (rec._DCAccount != null && cache.Get(rec._DCAccount) == null)
                        {
                            reload = true;
                            break;
                        }
                        if (rec._ContactRef != 0 && Contacts != null && Contacts.Get(rec._ContactRef) == null)
                        {
                            Contacts = null;
                            api.LoadCache(typeof(Uniconta.DataModel.Contact), true);
                        }
                    }
                    if (reload)
                        await api.LoadCache(typeof(Uniconta.DataModel.Debtor), true);
                }
            }
            if (refresh)
                gridRibbon_BaseActions("RefreshGrid");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItems = dgProjectGrid.SelectedItems;
            var selectedItem = dgProjectGrid.SelectedItem as ProjectClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Project"), selectedItem._Number);
            switch (ActionType)
            {
                case "AddRow":
                    if (dgProjectGrid.masterRecords != null)
                    {
                        AddDockItem(TabControls.ProjectPage2, new object[] { api, dgProjectGrid.masterRecord }, Uniconta.ClientTools.Localization.lookup("Project"), "Add_16x16.png");
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
                        AddDockItem(TabControls.ProjectPage2, new object[] { selectedItem, dgProjectGrid.masterRecord }, salesHeader);
                    }
                    else
                    {
                        AddDockItem(TabControls.ProjectPage2, new object[] { selectedItem, IdObject.get(true) }, salesHeader);
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
                case "CreateBudget":
                    if (dgProjectGrid.ItemsSource != null)
                        CreateBudget();
                    break;
                case "CreateBudgetTask":
                    if (dgProjectGrid.ItemsSource != null)
                        CreateBudgetTask();
                    break;
                case "UpdatePrices":
                    if (dgProjectGrid.ItemsSource != null)
                        UpdatePrices();
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
                case "ProjectInvoiceProposal":
                    if (selectedItem != null)
                    {
                        salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), selectedItem._DCAccount);
                        AddDockItem(TabControls.ProjInvProposal, dgProjectGrid.syncEntity, salesHeader);
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
                    if (selectedItem != null)
                    {
                        if (copyRowIsEnabled)
                            dgProjectGrid.CopyRow();
                        else
                            CopyRecord(selectedItem);
                    }
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
                        AddDockItem(TabControls.ProjectTaskGridPage, dgProjectGrid.syncEntity, string.Format("{0}({1}): {2}", Uniconta.ClientTools.Localization.lookup("Tasks"), Uniconta.ClientTools.Localization.lookup("DataGrid")
                            , selectedItem._Number));
                    break;
                case "CreateTaskFromTask":
                    if (dgProjectGrid.ItemsSource != null)
                        CreateTaskFromTask();
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
                case "ProjectEmployee":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectEmployeePage, dgProjectGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Employees"), selectedItem._Name ?? salesHeader));
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var header = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("Project"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgProjectGrid.syncEntity, header);
                    }
                    break;
                case "Contacts":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.ContactPage, selectedItem);
                    break;
                case "BudgetPanningSchedule":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectBudgetPlanningSchedulePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BudgetPlanningSchedule"), selectedItem._Name));
                    break;
                case "PurchaseOrders":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem._Number));
                    break;
                case "RefreshGrid":
                    TestDebtorReload(true, dgProjectGrid.ItemsSource as IEnumerable<Uniconta.DataModel.Project>);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        #region Create Budget
        private void CreateBudget()
        {
            var cwCreateBjt = new CwCreateUpdateBudget(api, 0);
#if !SILVERLIGHT
            cwCreateBjt.DialogTableId = 2000000177;
#endif
            cwCreateBjt.Closed += async delegate
            {
                if (cwCreateBjt.DialogResult == true)
                {
                    var projLst = dgProjectGrid.GetVisibleRows() as IList<Uniconta.DataModel.Project>;

                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.CreateBudget(CwCreateUpdateBudget.FromDate, CwCreateUpdateBudget.ToDate, CwCreateUpdateBudget.Employee, CwCreateUpdateBudget.Payroll,
                                                              CwCreateUpdateBudget.PrCategory, CwCreateUpdateBudget.Group, (byte)CwCreateUpdateBudget.BudgetMethod, CwCreateUpdateBudget.BudgetName,
                                                              CwCreateUpdateBudget.PrWorkSpace, cwCreateBjt.DeleteBudget, cwCreateBjt.InclProjectTask, projLst);

                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                        UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("Budget"), " ", Uniconta.ClientTools.Localization.lookup("Created").ToLower()),
                            Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwCreateBjt.Show();
        }

        private void CreateBudgetTask()
        {
            var cwCreateBjtTask = new CwCreateBudgetTask(api, 0);
#if !SILVERLIGHT
            cwCreateBjtTask.DialogTableId = 2000000179;
#endif
            cwCreateBjtTask.Closed += async delegate
            {
                if (cwCreateBjtTask.DialogResult == true)
                {
                    var projLst = dgProjectGrid.GetVisibleRows() as IList<Uniconta.DataModel.Project>;

                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.CreateBudgetTask(CwCreateBudgetTask.Employee, CwCreateBudgetTask.Payroll, CwCreateBudgetTask.Group,
                                                                  CwCreateBudgetTask.PrWorkSpace, cwCreateBjtTask.DeleteBudget, CwCreateBudgetTask.BudgetTaskPrincip,
                                                                  CwCreateBudgetTask.TaskHours, projLst);

                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                        UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("Budget"), " ", Uniconta.ClientTools.Localization.lookup("Created").ToLower()),
                            Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwCreateBjtTask.Show();
        }
        #endregion

        #region Update Budget Prices
        private void UpdatePrices()
        {
            var cwUpdateBjt = new CwCreateUpdateBudget(api, 1);
#if !SILVERLIGHT
            cwUpdateBjt.DialogTableId = 2000000178;
#endif
            cwUpdateBjt.Closed += async delegate
            {
                if (cwUpdateBjt.DialogResult == true)
                {
                    var projLst = dgProjectGrid.GetVisibleRows() as IList<Uniconta.DataModel.Project>;

                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.UpdatePrices(CwCreateUpdateBudget.FromDate, CwCreateUpdateBudget.ToDate, CwCreateUpdateBudget.Employee, CwCreateUpdateBudget.Payroll,
                                                              CwCreateUpdateBudget.PrCategory, CwCreateUpdateBudget.Group, CwCreateUpdateBudget.PrWorkSpace, cwUpdateBjt.InclProjectTask, projLst);

                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                        UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("Prices"), " ", Uniconta.ClientTools.Localization.lookup("Updated").ToLower()),
                      Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwUpdateBjt.Show();
        }
        #endregion

        #region Create Task from Task
        private void CreateTaskFromTask()
        {
            var cwCreateTask = new CWCreateTaskFromTask(api);
#if !SILVERLIGHT
            cwCreateTask.DialogTableId = 2000000105;
#endif
            cwCreateTask.Closed += async delegate
            {
                if (cwCreateTask.DialogResult == true)
                {
                    var projLst = dgProjectGrid.GetVisibleRows() as IEnumerable<Uniconta.DataModel.Project>;
                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.CreateTaskFromTask(CWCreateTaskFromTask.FromPrWorkSpace, CWCreateTaskFromTask.ToPrWorkSpace, CWCreateTaskFromTask.ProjectTemplate, CWCreateTaskFromTask.AddYear, projLst);

                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                        UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("Tasks"), " ", Uniconta.ClientTools.Localization.lookup("Created").ToLower()),
                            Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwCreateTask.Show();
        }
        #endregion

        private void CreateOrder(ProjectClient selectedItem)
        {
#if SILVERLIGHT
            var cwCreateOrder = new CWCreateOrderFromProject(api);
#else
            var cwCreateOrder = new UnicontaClient.Pages.CWCreateOrderFromProject(api, true, selectedItem);
            cwCreateOrder.DialogTableId = 2000000053;
#endif
            cwCreateOrder.Closed += async delegate
             {
                 if (cwCreateOrder.DialogResult == true)
                 {
                     busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                     busyIndicator.IsBusy = true;

                     var debtorOrderInstance = api.CompanyEntity.CreateUserType<ProjectInvoiceProposalClient>();
                     var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                     var result = await invoiceApi.CreateOrderFromProject(debtorOrderInstance, selectedItem._Number, CWCreateOrderFromProject.InvoiceCategory, CWCreateOrderFromProject.GenrateDate,
                         CWCreateOrderFromProject.FromDate, CWCreateOrderFromProject.ToDate, cwCreateOrder.ProjectTask, cwCreateOrder.ProjectWorkspace);
                     busyIndicator.IsBusy = false;
                     if (result != ErrorCodes.Succes)
                     {
                         if (result == ErrorCodes.NoLinesToUpdate)
                         {
                             var message = string.Format("{0}. {1}?", Uniconta.ClientTools.Localization.lookup(result.ToString()), string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceProposal")));
                             var res = UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Message"), UnicontaMessageBox.YesNo);
                             if (res == MessageBoxResult.Yes)
                             {
                                 debtorOrderInstance.SetMaster(selectedItem);
                                 debtorOrderInstance._PrCategory = CWCreateOrderFromProject.InvoiceCategory;
                                 debtorOrderInstance._NoItemUpdate = true;
                                 var err = await api.Insert(debtorOrderInstance);
                                 if (err == ErrorCodes.Succes)
                                     ShowOrderLines(debtorOrderInstance);
                             }
                         }
                         else
                             UtilDisplay.ShowErrorCode(result);
                     }
                     else
                         ShowOrderLines(debtorOrderInstance);
                 }
             };
            cwCreateOrder.Show();
        }

        void CreateZeroInvoice(ProjectClient selectedItem)
        {
            var cwCreateZeroInvoice = new UnicontaClient.Pages.CwCreateZeroInvoice(api, selectedItem);
#if !SILVERLIGHT
            cwCreateZeroInvoice.DialogTableId = 2000000067;
#endif
            cwCreateZeroInvoice.Closed += delegate
            {
                if (cwCreateZeroInvoice.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    if (cwCreateZeroInvoice.Simulate || !cwCreateZeroInvoice.IsCreateInvoiceProposal)
                        CreateZeroInvoice(selectedItem._Number, cwCreateZeroInvoice.InvoiceCategory, cwCreateZeroInvoice.AdjustmentCategory, cwCreateZeroInvoice.Employee, cwCreateZeroInvoice.ProjectTask, cwCreateZeroInvoice.ProjectWorkspace, 
                            cwCreateZeroInvoice.InvoiceDate, cwCreateZeroInvoice.ToDate, cwCreateZeroInvoice.Simulate);
                    else
                        CreateZeroInvoiceOrder(selectedItem._Number, cwCreateZeroInvoice.InvoiceCategory, cwCreateZeroInvoice.InvoiceDate, cwCreateZeroInvoice.ToDate,
                            cwCreateZeroInvoice.AdjustmentCategory, cwCreateZeroInvoice.Employee, cwCreateZeroInvoice.ProjectTask, cwCreateZeroInvoice.ProjectWorkspace);
                }
            };
            cwCreateZeroInvoice.Show();
        }

        private async void CreateZeroInvoice(string projectNumber, string invoiceCategory, string invoiceAdjustmentCategory, string employee, string Task, string WorkSpace, DateTime invoiceDate, DateTime toDate, bool isSimulate)
        {
            var result = await new Uniconta.API.Project.InvoiceAPI(api).CreateZeroInvoice(projectNumber, invoiceCategory, invoiceAdjustmentCategory, employee, Task, WorkSpace, invoiceDate, toDate, isSimulate, new GLTransClientTotal());
            busyIndicator.IsBusy = false;

            var ledgerRes = result.ledgerRes;
            if (ledgerRes == null)
                return;
            if (result.Err != ErrorCodes.Succes)
                Utility.ShowJournalError(ledgerRes, dgProjectGrid);
            else if (isSimulate && ledgerRes.SimulatedTrans != null)
                AddDockItem(TabControls.SimulatedTransactions, ledgerRes.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
            else
            {
                var msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), ledgerRes.JournalPostedlId);
                UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
            }
        }

        private async void CreateZeroInvoiceOrder(string Project, string InvoiceCategory, DateTime InvoiceDate, DateTime ToDate, string AdjustmentCategory, string Employee, string Task, string WorkSpace)
        {
            var debtorOrderInstance = api.CompanyEntity.CreateUserType<ProjectInvoiceProposalClient>();
            var result = await new Uniconta.API.Project.InvoiceAPI(api).CreateZeroInvoiceOrder(debtorOrderInstance, Project, InvoiceCategory, InvoiceDate, ToDate, AdjustmentCategory, Employee, Task, WorkSpace);

            busyIndicator.IsBusy = false;
            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
            else
                ShowOrderLines(debtorOrderInstance);
        }

        private void ShowOrderLines(ProjectInvoiceProposalClient order)
        {
            var msg = string.Format(Uniconta.ClientTools.Localization.lookup("CreatedOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceProposal"));
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", msg, Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Lines")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(TabControls.ProjInvoiceProposalLine, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("InvoiceProposalLine"), order._OrderNumber, order._DCAccount));
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
                    var isSimulated = generateInvoiceDialog.IsSimulation;
#if !SILVERLIGHT
                    var invoicePostingResult = new UnicontaClient.Pages.InvoicePostingPrintGenerator(
#elif SILVERLIGHT
                    var invoicePostingResult = new InvoicePostingPrintGenerator(
#endif
                    api, this);
                    invoicePostingResult.SetUpInvoicePosting(selectedItem, null, generateInvoiceDialog.GenrateDate, isSimulated, CompanyLayoutType.Invoice, generateInvoiceDialog.ShowInvoice, generateInvoiceDialog.InvoiceQuickPrint,
                        generateInvoiceDialog.NumberOfPages, generateInvoiceDialog.SendByEmail, generateInvoiceDialog.EmailList, generateInvoiceDialog.SendOnlyEmail, generateInvoiceDialog.FromDate, generateInvoiceDialog.ToDate,
                        generateInvoiceDialog.InvoiceCategory, generateInvoiceDialog.GenerateOIOUBLClicked, null);

                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        Task reloadTask = null;
                        if (!isSimulated)
                            reloadTask = Filter();
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
            CorasauDataGrid.CopyAndClearRowId(selectedItem, project);
            var parms = new object[2] { project, false };
            AddDockItem(TabControls.ProjectPage2, parms, Uniconta.ClientTools.Localization.lookup("Project"), "Add_16x16.png");
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