using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Windows;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using DevExpress.Xpf.Grid;
using Uniconta.API.Project;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Controls.Dialogs;
using System.Collections.Generic;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using UnicontaClient.Utilities;
using System.Linq;
using System.Windows.Input;
using System.Drawing;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectTaskGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTaskClient); } }
    }

    public partial class ProjectTaskGridPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectTaskGridPage; } }
        SQLCache ProjectCache;
        Uniconta.DataModel.Project proj;

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ProjectTaskPage2)
                dgProjectTaskGrid.UpdateItemSource(argument);
        }
        public ProjectTaskGridPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectTaskGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        public ProjectTaskGridPage(BaseAPI api, string lookupKey)
       : base(api, lookupKey)
        {
            InitializeComponent();
            InitPage(null);
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgProjectTaskGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Tasks"), key);
            SetHeader(header);
        }
        public ProjectTaskGridPage(UnicontaBaseEntity _master) : base(null)
        {
            InitializeComponent();
            InitPage(_master);
        }

        public ProjectTaskGridPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity _master)
        {
            ((TableView)dgProjectTaskGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            SetRibbonControl(localMenu, dgProjectTaskGrid);
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            dgProjectTaskGrid.api = api;
            dgProjectTaskGrid.BusyIndicator = busyIndicator;
            dgProjectTaskGrid.UpdateMaster(_master);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            proj = (_master as Uniconta.DataModel.Project);
            if (proj != null)
            {
                Project.Visible = false;
                proj.Tasks = null;
            }
            else
            {
                var emp = (_master as Uniconta.DataModel.Employee);
                if (emp != null)
                    Employee.Visible = false;

                UtilDisplay.RemoveMenuCommand(rb, new string[] { "CreateTaskFromTask" });
            }
            dgProjectTaskGrid.ShowTotalSummary();
            ribbonControl.DisableButtons(new string[] { "AddLine", "DeleteRow", "UndoDelete", "SaveGrid" });
        }

        Filter defaultFilter;
        protected override Filter[] DefaultFilters()
        {
            if (defaultFilter != null)
                return new Filter[] { defaultFilter };
            else
                return null;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            string employee = null;
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Employee", StringComparison.CurrentCultureIgnoreCase) == 0)
                    employee = rec.Value;
            }
            if (employee != null)
            {
                defaultFilter = new Filter();
                defaultFilter.name = "Employee";
                defaultFilter.value = employee;
                SetHeader();
            }

            base.SetParameter(Parameters);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;
            return true;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectTaskGrid.SelectedItem as ProjectTaskClient;
            var selectedTasks = dgProjectTaskGrid.SelectedItems?.Cast<ProjectTaskClient>();
            switch (ActionType)
            {
                case "EditAll":
                    if (dgProjectTaskGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    var newItem = dgProjectTaskGrid.GetChildInstance();
                    object[] param = new object[3];
                    param[0] = newItem;
                    param[1] = false;
                    param[2] = dgProjectTaskGrid.masterRecord;
                    AddDockItem(TabControls.ProjectTaskPage2, param, Uniconta.ClientTools.Localization.lookup("Task"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] para = new object[3];
                    para[0] = selectedItem;
                    para[1] = true;
                    para[2] = dgProjectTaskGrid.masterRecord;
                    AddDockItem(TabControls.ProjectTaskPage2, para, selectedItem.Name, null, true);
                    break;
                case "AddLine":
                    dgProjectTaskGrid.AddRow();
                    break;
                case "DeleteRow":
                    dgProjectTaskGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "CreateTaskFromTask":
                    if (dgProjectTaskGrid.ItemsSource != null)
                        CreateTaskFromTask();
                    break;
                case "CreateBudgetTask":
                    if (selectedTasks != null)
                        CreateBudgetTask(selectedTasks);
                    break;
                case "CloseTask":
                    if (selectedTasks != null)
                        CloseTask(selectedTasks);
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var header = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("Task"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgProjectTaskGrid.syncEntity, header);
                    }
                    break;
                case "UndoDelete":
                    dgProjectTaskGrid.UndoDeleteRow();
                    break;
                case "AddPeriod":
                    AddPeriod();
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgProjectTaskGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgProjectTaskGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void AddPeriod()
        {
            var prjBudgetLns = dgProjectTaskGrid.GetVisibleRows() as IEnumerable<ProjectTaskClient>;

            if (prjBudgetLns == null || prjBudgetLns.Count() == 0)
                return;

            var cwSelectPeriod = new CWAddPeriod(true);
            cwSelectPeriod.DialogTableId = 2000000105;
            cwSelectPeriod.Closed += delegate
            {
                if (cwSelectPeriod.DialogResult == true)
                {
                    var periodType = cwSelectPeriod.PeriodType;
                    var isFromDate = cwSelectPeriod.IsFromDate;
                    var isToDate = cwSelectPeriod.IsToDate;
                    var isBothDate = cwSelectPeriod.IsBothDate;

                    foreach (ProjectTaskClient line in prjBudgetLns)
                    {
                        var startDate = line._StartDate == DateTime.MinValue ? DateTime.Now : line._StartDate;
                        var endDate = line._EndDate == DateTime.MinValue ? DateTime.Now : line._EndDate;

                        dgProjectTaskGrid.SetLoadedRow(line);

                        if (cwSelectPeriod.IsFromDate)
                        {
                            switch (periodType)
                            {
                                case 0:
                                    line._StartDate = startDate.AddDays(cwSelectPeriod.PeriodValue);
                                    break;
                                case 1:
                                    line._StartDate = startDate.AddMonths(cwSelectPeriod.PeriodValue);
                                    break;
                                case 2:
                                    line._StartDate = startDate.AddYears(cwSelectPeriod.PeriodValue);
                                    break;
                            }
                            line.NotifyPropertyChanged("StartDate");
                        }
                        else if (cwSelectPeriod.IsToDate)
                        {
                            switch (periodType)
                            {
                                case 0:
                                    line._EndDate = endDate.AddDays(cwSelectPeriod.PeriodValue);
                                    break;
                                case 1:
                                    line._EndDate = endDate.AddMonths(cwSelectPeriod.PeriodValue);
                                    break;
                                case 2:
                                    line._EndDate = endDate.AddYears(cwSelectPeriod.PeriodValue);
                                    break;
                            }
                            line.NotifyPropertyChanged("EndDate");
                        }
                        else
                        {
                            switch (periodType)
                            {
                                case 0:
                                    line._StartDate = startDate.AddDays(cwSelectPeriod.PeriodValue);
                                    line._EndDate = endDate.AddDays(cwSelectPeriod.PeriodValue);
                                    break;
                                case 1:
                                    line._StartDate = startDate.AddMonths(cwSelectPeriod.PeriodValue);
                                    line._EndDate = endDate.AddMonths(cwSelectPeriod.PeriodValue);
                                    break;
                                case 2:
                                    line._StartDate = startDate.AddYears(cwSelectPeriod.PeriodValue);
                                    line._EndDate = endDate.AddYears(cwSelectPeriod.PeriodValue);
                                    break;
                            }
                            line.NotifyPropertyChanged("StartDate");
                            line.NotifyPropertyChanged("EndDate");
                        }
                        dgProjectTaskGrid.SetModifiedRow(line);
                    }

                    saveGrid();
                }
            };
            cwSelectPeriod.Show();
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgProjectTaskGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgProjectTaskGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgProjectTaskGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "DeleteRow", "UndoDelete", "SaveGrid" });
                editAllChecked = false;
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
                                var err = await dgProjectTaskGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgProjectTaskGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgProjectTaskGrid.Readonly = true;
                        dgProjectTaskGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "DeleteRow", "UndoDelete", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgProjectTaskGrid.Readonly = true;
                    dgProjectTaskGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "DeleteRow", "UndoDelete", "SaveGrid" });
                }
            }

        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgProjectTaskGrid.HasUnsavedData;
            }
        }
        async private void Save()
        {
            SetBusy();
            dgProjectTaskGrid.BusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");
            var err = await dgProjectTaskGrid.SaveData();
            ClearBusy();
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            if (api.CompanyEntity.ProjectTask)
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
        }

        private void FollowsTask_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgProjectTaskGrid.SelectedItem as ProjectTaskClient;
            if (selectedItem?._Project != null)
            {
                var selected = (ProjectClient)ProjectCache.Get(selectedItem._Project);
                setTask(selected, selectedItem);
            }
        }

        async void setTask(ProjectClient project, ProjectTaskClient rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                    rec.taskSource = project.Tasks ?? await project.LoadTasks(api);
                else
                {
                    rec.taskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("TaskSource");
            }
        }

        private void CreateTaskFromTask()
        {
            var cwCreateTask = new CWCreateTaskFromTask(api, proj?._Number);
            cwCreateTask.DialogTableId = 2000000104;
            cwCreateTask.Closed += async delegate
            {
                if (cwCreateTask.DialogResult == true)
                {
                    var taskLst = (IEnumerable<Uniconta.DataModel.ProjectTask>)dgProjectTaskGrid.GetVisibleRows();

                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.CreateTaskFromTask(CWCreateTaskFromTask.FromPrWorkSpace, CWCreateTaskFromTask.ToPrWorkSpace, cwCreateTask.ToProject, cwCreateTask.BudgetTaskDatePrincip, cwCreateTask.NewDate, CWCreateTaskFromTask.AddYear, taskLst);

                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                        UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("Tasks"), " ", Uniconta.ClientTools.Localization.lookup("Created").ToLower()),
                            Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwCreateTask.Show();
        }

        private void CreateBudgetTask(IEnumerable<ProjectTaskClient> taskLst)
        {
            var cwCreateBjtTask = new CwCreateBudgetTask(api, 1, taskLst.Count());
            cwCreateBjtTask.DialogTableId = 2000000180;
            cwCreateBjtTask.Closed += async delegate
            {
                if (cwCreateBjtTask.DialogResult == true)
                {
                    BudgetAPI budgetApi = new BudgetAPI(api);
                    var result = await budgetApi.CreateBudgetTask(CwCreateBudgetTask.Employee, CwCreateBudgetTask.Payroll, CwCreateBudgetTask.Group,
                                                                  CwCreateBudgetTask.PrWorkSpace, cwCreateBjtTask.DeleteBudget, CwCreateBudgetTask.BudgetTaskPrincip,
                                                                  CwCreateBudgetTask.TaskHours, null, taskLst);

                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                        UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("Budget"), " ", Uniconta.ClientTools.Localization.lookup("Created").ToLower()),
                            Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwCreateBjtTask.Show();
        }

        private void CloseTask(IEnumerable<ProjectTaskClient> lst)
        {
            var icount = lst.Count();
            var cwProjectTaskClose = new CWProjectTaskClose(icount);
            cwProjectTaskClose.DialogTableId = 2000000089;
            cwProjectTaskClose.Closed += delegate
            {
                if (cwProjectTaskClose.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;
                    foreach (var rec in lst)
                    {
                        rec.Ended = true;
                    }
                    api.Update(lst);
                    busyIndicator.IsBusy = false;
                }
            };
            cwProjectTaskClose.Show();
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glAccount = (sender as System.Windows.Controls.Image).Tag as ProjectTaskClient;
            if (glAccount != null)
                AddDockItem(TabControls.UserDocsPage, dgProjectTaskGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glAccount = (sender as System.Windows.Controls.Image).Tag as ProjectTaskClient;
            if (glAccount != null)
                AddDockItem(TabControls.UserNotesPage, dgProjectTaskGrid.syncEntity);
        }
    }
}
