using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Scheduler.UI;
using DevExpress.Xpf.Scheduling;
using DevExpress.XtraScheduler;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectBudgetPlanningSchedulePage : ControlBasePage
    {
        ObservableCollection<ProjectBudgetLineClient> budgetLinesLst;
        private EmployeeClient Employee;
        Uniconta.DataModel.Employee UserEmployee;
        DataSource schedulerDataSource;
        SQLCache EmployeeCache, ProjectCache, EmpGroupCache, ProjBgtGroupCache;
        ProjectBudget[] projBudgetLst;
        Company Comp;
        ProjectClient project;
        //Default View for Project and Main Menu
        static string DefaultViewAction;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.EmployeeGroup))]
        public string EmployeeGroup { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectBudgetGroup))]
        public string BudgetGroup { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        public string Project { get; set; }

        public ProjectBudgetPlanningSchedulePage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }

        public ProjectBudgetPlanningSchedulePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            StartLoadCache();
            this.DataContext = this;
            InitializeComponent();
            var api = this.api;
            Comp = api.CompanyEntity;
            MainControl = projBudgetPlanScheduler;
            ribbonControl = frmRibbon;
            BusyIndicator = busyIndicator;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            if (master != null)
            {
                if (master is EmployeeClient)
                    Employee = master as EmployeeClient;
                else if (master is ProjectClient)
                    project = master as ProjectClient;
            }
            leEmpGroup.api = leBudgetGroup.api = leProject.api = api;
            projBudgetPlanScheduler.AppointmentWindowShowing += ProjBudgetPlanScheduler_AppointmentWindowShowing;
            projBudgetPlanScheduler.ItemPropertyChanged += ProjBudgetPlanScheduler_ItemPropertyChanged;
            projBudgetPlanScheduler.DropAppointment += ProjBudgetPlanScheduler_DropAppointment;
            projBudgetPlanScheduler.PopupMenuShowing += ProjBudgetPlanScheduler_PopupMenuShowing;
            projBudgetPlanScheduler.PastingFromClipboard += SchedulerControl_PastingFromClipboard;
            projBudgetPlanScheduler.AppointmentAdded += ProjBudgetPlanScheduler_AppointmentAdded;
            projBudgetPlanScheduler.InitNewAppointment += ProjBudgetPlanScheduler_InitNewAppointment;
            projBudgetPlanScheduler.Loaded += ProjBudgetPlanScheduler_Loaded;
            projBudgetPlanScheduler.AppointmentEdited += ProjBudgetPlanScheduler_AppointmentEdited;
            SetControlsDefaultProperties();
            EmployeeCache = api.GetCache(typeof(Uniconta.DataModel.Employee));
            ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project));
            EmpGroupCache = api.GetCache(typeof(Uniconta.DataModel.EmployeeGroup));
            ProjBgtGroupCache = api.GetCache(typeof(Uniconta.DataModel.ProjectBudgetGroup));
        }

        void SetControlsDefaultProperties()
        {
            projBudgetPlanScheduler.AllowAppointmentEdit = true;
            projBudgetPlanScheduler.AllowAppointmentDelete = true;
            projBudgetPlanScheduler.AllowAppointmentCopy = true;
            projBudgetPlanScheduler.AllowAppointmentDrag = true;
            projBudgetPlanScheduler.AllowAppointmentConflicts = false;
            projBudgetPlanScheduler.AllowDrop = true;
            projBudgetPlanScheduler.AppointmentResizeMode = AppointmentDragResizeMode.Precise;
            projBudgetPlanScheduler.AllowAppointmentConflicts = true;
            projBudgetPlanScheduler.AllowAppointmentResize = false;
            projBudgetPlanScheduler.AllowInplaceEditor = false;
            timeLineView.IntervalDuration = TimeSpan.FromDays(1);
            projBudgetPlanScheduler.ToolTipMode = ToolTipMode.ToolTip;
            dayView.ResourcesPerPage = 4;
            timeLineView.IntervalDuration = TimeSpan.FromDays(1);
            timeLineDayView.IntervalDuration = TimeSpan.FromDays(1);
            LoadType(new Type[] { typeof(Uniconta.DataModel.EmployeeGroup), typeof(Uniconta.DataModel.ProjectBudgetGroup), typeof(Uniconta.DataModel.Project) });
            RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
            if (Employee != null)
                leEmpGroup.Visibility = lblEmpGrp.Visibility = Visibility.Collapsed;
            else if (project != null)
                lblProject.Visibility = leProject.Visibility = Visibility.Collapsed;
            GetMappings();
        }

        async void LoadDefaultFilter()
        {
            UserEmployee= EmployeeClient.GetUserEmployee(api);
            if (Employee==null)
            {
                if (EmpGroupCache== null)
                    EmpGroupCache =  await api.LoadCache(typeof(Uniconta.DataModel.ProjectBudgetGroup));
                leEmpGroup.SelectedItem = !string.IsNullOrEmpty(UserEmployee?._Group) ? (EmployeeGroup)EmpGroupCache.Get(UserEmployee?._Group) : null;
            }
            if (ProjBgtGroupCache== null)
                ProjBgtGroupCache = await api.LoadCache(typeof(Uniconta.DataModel.ProjectBudgetGroup));
            var budgetGroup = ((ProjectBudgetGroup[])ProjBgtGroupCache.GetRecords).Where(x => x._Default==true).FirstOrDefault();
            leBudgetGroup.SelectedItem = budgetGroup;

            if(project!= null )
            {
                if (ProjectCache== null)
                    ProjectCache =  await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api);
                leProject.SelectedItem= project;
            }

            if(Employee != null)
            {
                var emp = (EmployeeClient)EmployeeCache?.Get(Employee.KeyStr);
                if (emp != null)
                {
                    var empProj = await emp.LoadEmpProjects(api);
                    if (empProj != null && empProj.Count() > 0)
                        emp.EmpProjects = empProj.Where(s => (s._Blocked == false && (s._Phase == ProjectPhase.Created || s._Phase == ProjectPhase.Accepted || s._Phase == ProjectPhase.InProgress))).OrderBy(x => x.Number).ToList();

                    if (emp.EmpProjects?.Count() > 0)
                    {
                        leProject.HasCustomLookUp = true;
                        leProject.ItemsSource = emp.EmpProjects;
                    }
                }
            }

            projBudgetLst= await api.Query<ProjectBudget>();
        }
        private void ProjBudgetPlanScheduler_InitNewAppointment(object sender, AppointmentItemEventArgs e)
        {
            if (e.Appointment.AllDay)
            {
                e.Appointment.AllDay=false;
                e.Appointment.Start = new DateTime(e.Appointment.Start.Year, e.Appointment.Start.Month, e.Appointment.Start.Day, 8, 0, 0);
                e.Appointment.End = new DateTime(e.Appointment.Start.Year, e.Appointment.Start.Month, e.Appointment.Start.Day, 16, 0, 0);
            }
            else
            {
                if (projBudgetPlanScheduler.ActiveViewIndex != 4)
                {
                    var totalhours = projBudgetPlanScheduler.ActiveView.Scheduler.SelectedInterval.Duration.TotalHours;
                    if (totalhours == 3)
                    {
                        e.Appointment.Start = new DateTime(e.Appointment.Start.Year, e.Appointment.Start.Month, e.Appointment.Start.Day, 10, 0, 0);
                        e.Appointment.End = new DateTime(e.Appointment.Start.Year, e.Appointment.Start.Month, e.Appointment.Start.Day, 14, 0, 0);
                    }
                }
            }
        }

        private void ProjBudgetPlanScheduler_AppointmentEdited(object sender, AppointmentEditedEventArgs e)
        {
            var appointments = e.UpdateInSource;
            var appointment = appointments?.FirstOrDefault();
            var budgetLine = appointment?.SourceObject as ProjectBudgetLineClient;
            if (budgetLine != null)
            {
                budgetLine.Project= appointment.CustomFields["Project"]?.ToString();
                budgetLine.PrCategory = appointment.CustomFields["Category"]?.ToString();
                budgetLine.PayrollCategory = appointment.CustomFields["PayrollCategory"]?.ToString();
                budgetLine.Task = appointment.CustomFields["Task"]?.ToString();
                budgetLine.WorkSpace = appointment.CustomFields["WorkSpace"]?.ToString();
                SaveProjectBudgetLine(budgetLine, BudgetPlanningSchedulerAction.Modify);
            }
        }

        private void ProjBudgetPlanScheduler_AppointmentAdded(object sender, AppointmentAddedEventArgs e)
        {
            foreach (var appointment in e.AddToSource)
                appointment.CustomFields.PropertyChanged += (customFields, args) => CustomFieldsChanged(appointment, customFields, args);
            foreach (var appointment in e.DeleteFromSource)
                appointment.CustomFields.PropertyChanged -= (customFields, args) => CustomFieldsChanged(appointment, customFields, args);
        }

        private void CustomFieldsChanged(AppointmentItem appointment, object customFields, PropertyChangedEventArgs args)
        {
            if (appointment!= null)
            {
                var newBudgetLine = appointment.SourceObject as ProjectBudgetLineClient;
                newBudgetLine.Project= appointment.CustomFields["Project"]?.ToString();
                newBudgetLine.PrCategory = appointment.CustomFields["Category"]?.ToString();
                newBudgetLine.PayrollCategory = appointment.CustomFields["PayrollCategory"]?.ToString();
                newBudgetLine.Task = appointment.CustomFields["Task"]?.ToString();
                newBudgetLine.WorkSpace = appointment.CustomFields["WorkSpace"]?.ToString();
            }
        }

        private void ProjBudgetPlanScheduler_AppointmentWindowShowing(object sender, AppointmentWindowShowingEventArgs e)
        {
            if (Employee!= null)
                e.Appointment.ResourceId = Employee._Number;
            var selectedProject = leProject.SelectedItem as ProjectClient;
            e.Window = new CwProjectBugetPlanning(api, e.Appointment.ResourceId as string, selectedProject);
            e.Window.DataContext = new ProjectBugetPlanningVM(e.Appointment, projBudgetPlanScheduler);
        }
            
        private void ProjBudgetPlanScheduler_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            if (e.MenuType == ContextMenuType.AppointmentContextMenu)
            {
                PopupMenu menu = (PopupMenu)e.Menu;
                for (int i = 0; i < menu.Items.Count; i++)
                {
                    BarItem menuItem = menu.Items[i] as BarItem;
                    if (menuItem != null)
                    {
                        if (menuItem.Content.ToString() == "Open")
                            menuItem.Content = Localization.lookup("Open");
                        else if (menuItem.Content.ToString() == "Copy")
                            menuItem.Content = Localization.lookup("Copy");
                        else if (menuItem.Content.ToString() == "Paste")
                            menuItem.Content = Localization.lookup("Paste");
                        else if (menuItem.Content.ToString() == "Delete")
                            menuItem.Content = Localization.lookup("Delete");
                    }

                    if (i == 6 || i == 7 || i == 9)
                        menuItem.IsVisible = false;
                }
            }

            if (e.MenuType == ContextMenuType.CellContextMenu)
            {
                PopupMenu menu = (PopupMenu)e.Menu;
                for (int i = 0; i < menu.Items.Count; i++)
                {
                    BarItem menuItem = menu.Items[i] as BarItem;
                    if (menuItem != null)
                    {
                        if (menuItem.Content.ToString() == "New Appointment")
                            menuItem.Content = UtilFunctions.ToLowerConditional("NewOBJ", "Task");
                        else if (menuItem.Content.ToString() == "New All Day Event")
                            menuItem.Content = Uniconta.Common.Utility.Util.ConcatParenthesis(UtilFunctions.ToLowerConditional("NewOBJ", "Task"), Localization.lookup("AllDay"));
                        else if (menuItem.Content.ToString() == "Change View To")
                        {
                            menuItem.Content = Localization.lookup("ChangeOrientation");
                            var items = ((DevExpress.Xpf.Bars.BarSubItem)menuItem).Items;
                            if (items != null)
                            {
                                for (int j = 0; j < items.Count; j++)
                                {
                                    BarCheckItem item = items[j] as BarCheckItem;
                                    if (item != null)
                                    {
                                        if (item.Content.ToString() == "Day View")
                                            item.Content = Localization.lookup("DayView");
                                        else if (item.Content.ToString() == "Work Week View")
                                            item.Content = Localization.lookup("WorkWeekView");
                                        else if (item.Content.ToString() == "Week View")
                                            item.Content = Localization.lookup("WeekView");
                                        else if (item.Content.ToString() == "Month View")
                                            item.Content = Localization.lookup("MonthView");
                                    }
                                }
                            }
                        }
                        else if (menuItem.Content.ToString() == "Paste")
                            menuItem.Content = Localization.lookup("Paste");

                        if (i == 3 || i == 4 || i == 6 || i == 7 || i == 9)
                            menuItem.IsVisible = false;
                    }
                }
            }
        }

        private void ProjBudgetPlanScheduler_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDefaultFilter();
            if (Employee != null)
            {
                BindScheduler();
                projBudgetPlanScheduler.ActiveViewIndex = 0;
                projBudgetPlanScheduler.GroupType = SchedulerGroupType.Resource;
                //Set user session default Active View
                if (!string.IsNullOrEmpty(DefaultViewAction))
                    FrmRibbon_OnItemClicked(DefaultViewAction);
            }

            if (project != null)
            {
                schedulerDataSource.AppointmentsSource = null;
                BindScheduler(leEmpGroup.Text);
                projBudgetPlanScheduler.ActiveViewIndex = 5 ;
                projBudgetPlanScheduler.GroupType = SchedulerGroupType.Resource;
                timeLineView.IntervalDuration = TimeSpan.FromDays(5);
                timeLineDayView.IntervalDuration = TimeSpan.FromDays(5);
                //Set user session default Active View
                if (!string.IsNullOrEmpty(DefaultViewAction))
                    FrmRibbon_OnItemClicked(DefaultViewAction);
            }

            projBudgetPlanScheduler.Loaded -= ProjBudgetPlanScheduler_Loaded;
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "GoToday":
                    {
                        if (Employee == null && !projBudgetPlanScheduler.IsVisible)
                            return;
                        if (Employee != null)
                            projBudgetPlanScheduler.ActiveViewIndex = 4;
                        else
                            projBudgetPlanScheduler.ActiveViewIndex = 4;
                        projBudgetPlanScheduler.Start = BasePage.GetSystemDefaultDate();
                        TimeSpan currentSelectionDuration = projBudgetPlanScheduler.SelectedInterval.End.Subtract(projBudgetPlanScheduler.SelectedInterval.Start);
                        projBudgetPlanScheduler.SelectedInterval = new DevExpress.Mvvm.DateTimeRange(projBudgetPlanScheduler.Start, projBudgetPlanScheduler.Start.Add(currentSelectionDuration));
                        timeLineView.IntervalDuration = TimeSpan.FromDays(1);
                        DefaultViewAction = ActionType;
                    }
                    break;
                case "DayView":
                    {
                        if (Employee != null)
                        {
                            projBudgetPlanScheduler.ActiveViewIndex = 0;
                            dayView.ResourcesPerPage = 4;
                        }
                        else
                            projBudgetPlanScheduler.ActiveViewIndex = 4;

                        projBudgetPlanScheduler.GroupType = SchedulerGroupType.Resource;
                        timeLineView.IntervalDuration = TimeSpan.FromDays(1);
                        DefaultViewAction = ActionType;
                    }
                    break;
                case "WorkWeekView":
                    {
                        if (Employee != null)
                        {
                            projBudgetPlanScheduler.ActiveViewIndex = 1;
                            workWeekView.ResourcesPerPage = 4;
                        }
                        else
                            projBudgetPlanScheduler.ActiveViewIndex = 5;

                        projBudgetPlanScheduler.GroupType = SchedulerGroupType.Resource;
                        timeLineView.IntervalDuration = TimeSpan.FromDays(5);
                        timeLineDayView.IntervalDuration = TimeSpan.FromDays(5);
                        SetMondayAsFirstDay();
                        DefaultViewAction = ActionType;
                    }
                    break;
                case "WeekView":
                    {
                        if (Employee != null)
                        {
                            projBudgetPlanScheduler.ActiveViewIndex = 2;
                            weekView.ResourcesPerPage = 4;
                        }
                        else
                            projBudgetPlanScheduler.ActiveViewIndex = 5;

                        projBudgetPlanScheduler.GroupType = SchedulerGroupType.Resource;
                        timeLineView.IntervalDuration = TimeSpan.FromDays(7);
                        timeLineDayView.IntervalDuration = TimeSpan.FromDays(7);
                        SetMondayAsFirstDay();
                        DefaultViewAction = ActionType;
                    }
                    break;
                case "MonthView":
                    {
                        if (Employee != null)
                        {
                            if (!projBudgetPlanScheduler.IsVisible)
                                return;
                            projBudgetPlanScheduler.ActiveViewIndex = 3;
                        }
                        else
                            projBudgetPlanScheduler.ActiveViewIndex = 5;

                        projBudgetPlanScheduler.GroupType = SchedulerGroupType.Date;
                        timeLineView.IntervalDuration = TimeSpan.FromDays(30);
                        timeLineDayView.IntervalDuration = TimeSpan.FromDays(30);
                        DefaultViewAction = ActionType;
                    }
                    break;
                case "Search":
                    if (string.IsNullOrEmpty(leBudgetGroup.Text))
                    {
                        var msgText = string.Format("{0} ({1}: {2})",
                         Localization.lookup("FieldCannotBeEmpty"),
                         Localization.lookup("Field"), Localization.lookup("BudgetGroup"));
                        UnicontaMessageBox.Show(msgText, Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }
                    schedulerDataSource.AppointmentsSource = null;
                    BindScheduler(leEmpGroup.Text);
                    if (Employee == null)
                    {
                        projBudgetPlanScheduler.ActiveViewIndex = 5;
                        projBudgetPlanScheduler.GroupType = SchedulerGroupType.Resource;
                        timeLineView.IntervalDuration = TimeSpan.FromDays(5);
                        timeLineDayView.IntervalDuration = TimeSpan.FromDays(5);
                    }
                    if (!string.IsNullOrEmpty(DefaultViewAction))
                        FrmRibbon_OnItemClicked(DefaultViewAction);
                    break;
                default:
                    controlRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void SetMondayAsFirstDay()
        {
            timeLineDayView.Scheduler.Start = timeLineDayView.Scheduler.Start.AddDays(1-(int)timeLineDayView.Scheduler.Start.DayOfWeek);
            if(Employee!= null)
            {
                weekView.Scheduler.Start = weekView.Scheduler.Start.AddDays(1-(int)weekView.Scheduler.Start.DayOfWeek);
                workWeekView.Scheduler.Start = workWeekView.Scheduler.Start.AddDays(1-(int)workWeekView.Scheduler.Start.DayOfWeek);
            }
        }

        void GetMappings()
        {
            schedulerDataSource = new DataSource();
            AppointmentMappings AppointmentMapp = new AppointmentMappings();
            AppointmentMapp.Start = "TimeFrom";
            AppointmentMapp.End = "TimeTo";
            AppointmentMapp.Subject = "Text";
            AppointmentMapp.ResourceId = "Employee";
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Project", Name = "Project" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "ProjectName", Name = "ProjectName" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Date", Name = "Date" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "PrCategory", Name = "Category" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "PayrollCategory", Name = "PayrollCategory" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Task", Name = "Task" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "WorkSpace", Name = "WorkSpace" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Qty", Name = "Qty" });
            schedulerDataSource.AppointmentMappings = AppointmentMapp;

            ResourceMappings ResourceMapp = new ResourceMappings();
            ResourceMapp.Caption = "EmployeeName";
            ResourceMapp.Id = "Employee";
            schedulerDataSource.ResourceMappings = ResourceMapp;
        }

        async void BindScheduler(string empGrp = null)
        {
            busyIndicator.IsBusy = true;
            var masters = new List<UnicontaBaseEntity>();
            IEnumerable<EmployeeLocal> employeeLst = null;
            if (Employee == null)
            {
                var employees = (Uniconta.DataModel.Employee[])EmployeeCache?.GetRecords;

                if (!string.IsNullOrEmpty(empGrp))
                {
                    employeeLst = from c in employees
                                  where c._Group == empGrp
                                  select new EmployeeLocal
                                  {
                                      Employee = c._Number,
                                      EmployeeName = c._Name
                                  };
                }
                else
                {
                    foreach (var item in employees)
                    {
                        employeeLst = from c in employees
                                      select new EmployeeLocal
                                      {
                                          Employee = c._Number,
                                          EmployeeName = c._Name
                                      };
                    }
                }

                if (!string.IsNullOrEmpty(leEmpGroup.Text))
                {
                    var selEmpGroup = (Uniconta.DataModel.EmployeeGroup)EmpGroupCache.Get(leEmpGroup.Text);
                    if (selEmpGroup != null)
                        masters.Add(selEmpGroup);
                }
                if (!string.IsNullOrEmpty(leBudgetGroup.Text))
                {
                    var selBugetGroup = (Uniconta.DataModel.ProjectBudgetGroup)ProjBgtGroupCache.Get(leBudgetGroup.Text);
                    if (selBugetGroup != null)
                        masters.Add(selBugetGroup);
                }

                if (project != null)
                    masters.Add(project);
            }

            if (!string.IsNullOrWhiteSpace(leProject.Text) && project == null)
            {
                var proj = leProject.SelectedItem as ProjectClient;
                if (proj != null)
                    masters.Add(proj);
            }

            if (Employee!= null)
            {
                if (!string.IsNullOrEmpty(leBudgetGroup.Text))
                {
                    var selBugetGroup = (Uniconta.DataModel.ProjectBudgetGroup)ProjBgtGroupCache.Get(leBudgetGroup.Text);
                    if (selBugetGroup!= null)
                        masters.Add(selBugetGroup);
                }
                masters.Add(Employee);
            }

            var projBudgetLinesLst = await api.Query<ProjectBudgetLineClient>(masters, null);

            if (Employee == null)
                budgetLinesLst = new ObservableCollection<ProjectBudgetLineClient>(projBudgetLinesLst?.Where(x => employeeLst.Any(y => y.Employee == x.Employee)));
            else
                budgetLinesLst = new ObservableCollection<ProjectBudgetLineClient>(projBudgetLinesLst);

            schedulerDataSource.AppointmentsSource = budgetLinesLst;
            schedulerDataSource.ResourcesSource = employeeLst?.Count() > 0 ? employeeLst.ToArray() : new List<EmployeeLocal> { new EmployeeLocal { Employee = Employee?._Number, EmployeeName= Employee?._Name } }.ToArray();
            projBudgetPlanScheduler.DataSource = schedulerDataSource;

            if (budgetLinesLst != null)
                budgetLinesLst.CollectionChanged +=_budgetLinesLst_CollectionChanged;

            busyIndicator.IsBusy = false;
            SetMondayAsFirstDay();
            projBudgetPlanScheduler.Visibility= Visibility.Visible;
        }

        private void _budgetLinesLst_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var empBudgetLine = e.NewItems[0] as ProjectBudgetLineClient;
                if (empBudgetLine != null)
                {
                    ProjectBudget projBudget = null;
                    var projBugtlst = projBudgetLst?.Where(x => x._Project == empBudgetLine._Project && x._Group == leBudgetGroup.Text);
                    if (projBugtlst?.Count() > 1)
                        projBudget = projBugtlst.Where(x => x._Current == true)?.FirstOrDefault();
                    if (projBudget == null)
                        projBudget = projBugtlst?.FirstOrDefault();
                    if (projBudget == null)
                    {
                        var budgetName = string.Format(Localization.lookup("NewOBJ"), Localization.lookup("Budget"));
                        projBudget = new ProjectBudget { _Name = budgetName, _Project = empBudgetLine._Project, _Group = leBudgetGroup.Text };
                    }
                    empBudgetLine._Qty = empBudgetLine.TimeTo.Subtract(empBudgetLine.TimeFrom).TotalHours;
                    if (Employee != null)
                    {
                        empBudgetLine._Employee = Employee._Number;
                        empBudgetLine.SetMaster(Employee);
                    }
                    SaveProjectBudgetLine(empBudgetLine, BudgetPlanningSchedulerAction.Insert, projBudget);
                }
            }

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var empBudgetLine = e.OldItems[0] as ProjectBudgetLineClient;
                if (empBudgetLine != null && empBudgetLine.RowId != 0)
                    SaveProjectBudgetLine(empBudgetLine, BudgetPlanningSchedulerAction.Delete);
            }
        }
       

        bool isHandled = false;
        private void ProjBudgetPlanScheduler_ItemPropertyChanged(object sender, ItemPropertyChangedEventArgs e)
        {
            if (!isHandled)
            {
                isHandled = true;
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var budgetLine = e.Item.SourceObject as ProjectBudgetLineClient;
                    if (budgetLine != null)
                        SaveProjectBudgetLine(budgetLine, BudgetPlanningSchedulerAction.Modify);
                    isHandled = false;
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void ProjBudgetPlanScheduler_DropAppointment(object sender, DropAppointmentEventArgs e)
        {
            for (int i = 0; i < e.DragAppointments.Count; i++)
            {
                if (e.ConflictedAppointments[i].Count == 0)
                {
                    var app = e.SourceAppointments[i];
                    var drag = e.DragAppointments[i];
                    app.Subject= drag.Subject;
                    app.ResourceId= drag.ResourceId;
                    app.LabelId= drag.LabelId;
                    app.Start = drag.Start;
                    app.End = drag.End;
                    app.CustomFields["Project"]= drag.CustomFields["Project"];
                    app.CustomFields["Date"]= drag.CustomFields["Date"];
                    app.CustomFields["Category"]= drag.CustomFields["Category"];
                    app.CustomFields["PayrollCategory"]= drag.CustomFields["PayrollCategory"];
                    app.CustomFields["Task"]= drag.CustomFields["Task"];
                    app.CustomFields["WorkSpace"]= drag.CustomFields["WorkSpace"];
                }
            }
        }

        async void SaveProjectBudgetLine(ProjectBudgetLineClient proBudgetLine, BudgetPlanningSchedulerAction action, ProjectBudget prBudget = null)
        {
            busyIndicator.IsBusy = true;
            var result = ErrorCodes.NoSucces;
            switch (action)
            {
                case BudgetPlanningSchedulerAction.Delete:
                    result = await api.Delete(proBudgetLine);
                    break;

                case BudgetPlanningSchedulerAction.Insert:
                    if (prBudget!= null)
                    {
                        if (prBudget.RowId==0)
                            result = await api.Insert(prBudget);
                        if (result== ErrorCodes.Succes || prBudget.RowId!=0)
                            proBudgetLine.SetMaster(prBudget);
                        else
                        {
                            busyIndicator.IsBusy = false;
                            UtilDisplay.ShowErrorCode(result);
                            budgetLinesLst.Remove(proBudgetLine);
                            return;
                        }
                    }
                    result = await api.Insert(proBudgetLine);
                    break;

                case BudgetPlanningSchedulerAction.Modify:
                    proBudgetLine._Qty = proBudgetLine.TimeTo.Subtract(proBudgetLine.TimeFrom).TotalHours;
                    result = await api.Update(proBudgetLine);
                    break;
            }
            busyIndicator.IsBusy = false;
            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
        }


        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            EmployeeCache = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Employee)).ConfigureAwait(false);
            ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
            EmpGroupCache = api.GetCache(typeof(Uniconta.DataModel.EmployeeGroup)) ?? await api.LoadCache(typeof(Uniconta.DataModel.EmployeeGroup)).ConfigureAwait(false);
            ProjBgtGroupCache = api.GetCache(typeof(Uniconta.DataModel.ProjectBudgetGroup)) ?? await api.LoadCache(typeof(Uniconta.DataModel.ProjectBudgetGroup)).ConfigureAwait(false);
        }

        internal class EmployeeLocal
        {
            public string Employee { get; set; }
            public string EmployeeName { get; set; }
        }

        private void SchedulerControl_PastingFromClipboard(object sender, SchedulerPastingFromClipboardEventArgs e)
        {
            var appointment = e.Appointments[0];
            if (appointment!= null)
            {
                var newBudgetLine = appointment.SourceObject as ProjectBudgetLineClient;
                newBudgetLine.Date= appointment.Start;
                newBudgetLine.TimeFrom= appointment.Start;
                newBudgetLine.TimeTo= appointment.End;
            }
        }
    }

    public enum BudgetPlanningSchedulerAction
    {
        Insert,
        Delete,
        Modify
    }

    public class TimeLineViewLocal : TimelineView
    {
        public override void NavigateForward()
        {
            if (this.Scheduler.VisibleIntervals.Count <= 7)
                this.Scheduler.Start = this.Scheduler.Start.AddDays(7);
            else
                base.NavigateForward();
        }

        public override void NavigateBackward()
        {
            if (this.Scheduler.VisibleIntervals.Count <= 7)
                this.Scheduler.Start = this.Scheduler.Start.AddDays(-7);
            else
                base.NavigateBackward();
        }
        
    }
}
