using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using System.Windows;
using UnicontaClient.Pages.Project.TimeManagement;
using Uniconta.DataModel;
using Uniconta.API.Project;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectBudgetClient); } }
        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (ProjectBudgetClient)this.SelectedItem;
            return (selectedItem?._Project != null);
        }
    }

    public partial class ProjectBudgetPage : GridBasePage
    {
        SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> payrollCache;
        SQLTableCache<Uniconta.ClientTools.DataModel.ProjectClient> projectCache;
        SQLTableCache<Uniconta.DataModel.Employee> employeeCache;
        SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupCache;
        SQLTableCache<Uniconta.DataModel.ProjectBudgetGroup> budgetGroupCache;
        SQLTableCache<Uniconta.DataModel.PrCategory> prCategoryCache;
        SQLTableCache<Uniconta.DataModel.PrWorkSpace> workspaceCache;

        public override string NameOfControl { get { return TabControls.ProjectBudgetPage; } }
        public ProjectBudgetPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectBudgetPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        public ProjectBudgetPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectBudgetGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            var syncMaster = dgProjectBudgetGrid.masterRecord as Uniconta.DataModel.Project;
            if (syncMaster == null) return;
            string header = null;
            header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("ProjectBudget"), syncMaster._Name);
            SetHeader(header);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            SetRibbonControl(localMenu, dgProjectBudgetGrid);
            dgProjectBudgetGrid.api = api;
            if (master == null)
                Project.Visible = true;
            else
            {
                Project.Visible = false;
                dgProjectBudgetGrid.UpdateMaster(master);
            }
            dgProjectBudgetGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            projectCache = api.GetCache<Uniconta.ClientTools.DataModel.ProjectClient>();
            payrollCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>();
            prCategoryCache = api.GetCache<Uniconta.DataModel.PrCategory>();
            projGroupCache = api.GetCache<Uniconta.DataModel.ProjectGroup>();
            budgetGroupCache = api.GetCache<Uniconta.DataModel.ProjectBudgetGroup>();

            employeeCache = api.GetCache<Uniconta.DataModel.Employee>();

            StartLoadCache();
        }

        ProjectBudgetClient GetSelectedItem(out string Header)
        {

            var selectedItem = dgProjectBudgetGrid.SelectedItem as ProjectBudgetClient;
            if (selectedItem == null)
            {
                Header = string.Empty;
                return null;
            }
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Budget"), selectedItem.Name);
            Header = header;
            return selectedItem;
        }

        bool CopiedData;
        private void localMenu_OnItemClicked(string ActionType)
        {
            string header = string.Empty;
            ProjectBudgetClient selectedItem = GetSelectedItem(out header);

            switch (ActionType)
            {
                case "AddRow":
                    dgProjectBudgetGrid.AddRow();
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgProjectBudgetGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "DeleteRow":
                    if (selectedItem != null && UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), selectedItem._Name), Uniconta.ClientTools.Localization.lookup("Confirmation"),
#if !SILVERLIGHT
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
#else
                        MessageBoxButton.OKCancel) == MessageBoxResult.OK)
#endif
                        dgProjectBudgetGrid.DeleteRow();
                    break;
                case "BudgetLines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                case "BudgetCategorySum":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectBudgetCategorySumPage, selectedItem, header);
                    break;
                case "CreateBudget":
                    CreateBudget();
                    break;
                case "CreateBudgetTask":
                    CreateBudgetTask();
                    break;
                case "UpdatePrices":
                    if (dgProjectBudgetGrid.ItemsSource != null)
                        UpdatePrices();
                    break;
                case "CreateAnchorBudget":
                    if (dgProjectBudgetGrid.ItemsSource != null)
                        CreateAnchorBudget();
                    break;
                case "Check":
                    AddDockItem(TabControls.TMPlanningCheckPage, null);
                    break;
                case "RefreshGrid":
                    gridRibbon_BaseActions(ActionType);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        #region Create Budget
        private void CreateBudget()
        {
            var cwCreateBjt = new CwCreateUpdateBudget(api);
#if !SILVERLIGHT
            cwCreateBjt.DialogTableId = 2000000100;
#endif
            cwCreateBjt.Closed += async delegate
            {
                if (cwCreateBjt.DialogResult == true) 
                {
                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.CreateBudget(CwCreateUpdateBudget.FromDate, CwCreateUpdateBudget.ToDate, CwCreateUpdateBudget.Employee, CwCreateUpdateBudget.Payroll,
                                                              CwCreateUpdateBudget.PrCategory, CwCreateUpdateBudget.Group, (byte)CwCreateUpdateBudget.BudgetMethod, CwCreateUpdateBudget.BudgetName,
                                                               CwCreateUpdateBudget.PrWorkSpace, cwCreateBjt.DeleteBudget, cwCreateBjt.InclProjectTask, null);

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
            var cwCreateBjtTask = new CwCreateBudgetTask(api);
#if !SILVERLIGHT
            cwCreateBjtTask.DialogTableId = 2000000106;
#endif
            cwCreateBjtTask.Closed += async delegate
            {
                if (cwCreateBjtTask.DialogResult == true)
                {
                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.CreateBudgetTask(CwCreateBudgetTask.Employee, CwCreateBudgetTask.Payroll, CwCreateBudgetTask.Group,
                                                                  CwCreateBudgetTask.PrWorkSpace, cwCreateBjtTask.DeleteBudget, (byte)CwCreateBudgetTask.BudgetTaskPrincip,
                                                                  CwCreateBudgetTask.TaskHours, null);

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

        #region UpdatePrices
        private void UpdatePrices()
        {
            var cwUpdateBjt = new CwCreateUpdateBudget(api, 1);
#if !SILVERLIGHT
            cwUpdateBjt.DialogTableId = 2000000101;
#endif
            cwUpdateBjt.Closed += async delegate
            {
                if (cwUpdateBjt.DialogResult == true)
                {
                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.UpdatePrices(CwCreateUpdateBudget.FromDate, CwCreateUpdateBudget.ToDate, CwCreateUpdateBudget.Employee, CwCreateUpdateBudget.Payroll,
                                                              CwCreateUpdateBudget.PrCategory, CwCreateUpdateBudget.Group, CwCreateUpdateBudget.PrWorkSpace, cwUpdateBjt.InclProjectTask, null);

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

        #region Create Anchor Budget
        private void CreateAnchorBudget()
        {
            var cwCreateAnchorBjt = new CwCreateAnchorBudget(api);
#if !SILVERLIGHT
            cwCreateAnchorBjt.DialogTableId = 2000000102;
#endif
            cwCreateAnchorBjt.Closed += async delegate
            {
                if (cwCreateAnchorBjt.DialogResult == true)
                {
                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.CreateAnchorBudget(CwCreateAnchorBudget.Group);

                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                        UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("AnchorBudget"), " ", Uniconta.ClientTools.Localization.lookup("Updated").ToLower()),
                      Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwCreateAnchorBjt.Show();
        }
        #endregion

        async void SaveAndOpenLines(ProjectBudgetClient selectedItem)
        {
            if (dgProjectBudgetGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.ProjectBudgetLinePage, dgProjectBudgetGrid.syncEntity, string.Format("{0} {1}: {2}", Uniconta.ClientTools.Localization.lookup("Budget"), Uniconta.ClientTools.Localization.lookup("Lines"), selectedItem._Project));
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;

            if (projectCache == null)
                projectCache = await api.LoadCache<Uniconta.ClientTools.DataModel.ProjectClient>().ConfigureAwait(false);
            if (payrollCache == null)
                payrollCache = await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>().ConfigureAwait(false);
            if (prCategoryCache == null)
                prCategoryCache = await api.LoadCache<Uniconta.DataModel.PrCategory>().ConfigureAwait(false);
            if (projGroupCache == null)
                projGroupCache = await api.LoadCache<Uniconta.DataModel.ProjectGroup>().ConfigureAwait(false);
            if (employeeCache == null)
                employeeCache = await api.LoadCache<Uniconta.DataModel.Employee>().ConfigureAwait(false);
            if (budgetGroupCache == null)
                budgetGroupCache = await api.LoadCache<Uniconta.DataModel.ProjectBudgetGroup>().ConfigureAwait(false);
            
            if (workspaceCache == null)
                workspaceCache = await api.LoadCache<Uniconta.DataModel.PrWorkSpace>().ConfigureAwait(false);
        }
    }

    public enum BudgetMethod : byte
    {
        Month,
        Week,
        Day
    };

    public enum BudgetTaskPrincip : byte
    {
        Princip1,
        Princip2
    };
}


