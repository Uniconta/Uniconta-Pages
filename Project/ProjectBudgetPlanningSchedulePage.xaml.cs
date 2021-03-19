using DevExpress.Xpf.Bars;
using DevExpress.Xpf.Scheduling;
using DevExpress.XtraScheduler;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectBudgetPlanningSchedulePage : ControlBasePage
    {
        public ObservableCollection<ProjectBudgetLineClient> _budgetLinesLst;
        private EmployeeClient Employee;
        DataSource schedulerDataSource;
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
            InitializeComponent();
            MainControl = projBudgetPlanScheduler;
            ribbonControl = frmRibbon;
            BusyIndicator = busyIndicator;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            projBudgetPlanScheduler.AllowAppointmentEdit = false;
            projBudgetPlanScheduler.AllowAppointmentDelete = false;
            projBudgetPlanScheduler.AllowInplaceEditor = false;
            projBudgetPlanScheduler.AllowAppointmentCopy = false;
            if (master != null)
                 Employee = master as EmployeeClient;
            GetMappings();
            BindScheduler();
            projBudgetPlanScheduler.Loaded += ProjBudgetPlanScheduler_Loaded;
            projBudgetPlanScheduler.PopupMenuShowing += ProjBudgetPlanScheduler_PopupMenuShowing;
        }

        private void ProjBudgetPlanScheduler_PopupMenuShowing(object sender, PopupMenuShowingEventArgs e)
        {
            if (e.MenuType == ContextMenuType.CellContextMenu || e.MenuType == ContextMenuType.AppointmentContextMenu)
            {
                PopupMenu menu = (PopupMenu)e.Menu;
                menu?.Items.Clear();
            }
        }

        private void ProjBudgetPlanScheduler_Loaded(object sender, RoutedEventArgs e)
        {
            projBudgetPlanScheduler.ActiveViewIndex = 1;
            projBudgetPlanScheduler.GroupType = SchedulerGroupType.Date;
        }
      
        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Backward":
                    if (projBudgetPlanScheduler.ActiveView is DayView)
                        projBudgetPlanScheduler.Start = projBudgetPlanScheduler.Start.AddDays(-1);
                    else if(projBudgetPlanScheduler.ActiveView is TimelineView)
                        projBudgetPlanScheduler.Start = projBudgetPlanScheduler.Start.AddDays(-7);
                    break;
                case "Forward":
                    if (projBudgetPlanScheduler.ActiveView is DayView)
                        projBudgetPlanScheduler.Start = projBudgetPlanScheduler.Start.AddDays(1);
                    else if (projBudgetPlanScheduler.ActiveView is TimelineView)
                        projBudgetPlanScheduler.Start = projBudgetPlanScheduler.Start.AddDays(7);
                    break;
                case "GoToday":
                    {
                        projBudgetPlanScheduler.Start = BasePage.GetSystemDefaultDate();
                        TimeSpan currentSelectionDuration = projBudgetPlanScheduler.SelectedInterval.End.Subtract(projBudgetPlanScheduler.SelectedInterval.Start);
                        projBudgetPlanScheduler.SelectedInterval = new DevExpress.Mvvm.DateTimeRange(projBudgetPlanScheduler.Start, projBudgetPlanScheduler.Start.Add(currentSelectionDuration));
                    }
                    break;
                case "DayView":
                    {
                        projBudgetPlanScheduler.ActiveViewIndex = 0;
                        projBudgetPlanScheduler.GroupType = SchedulerGroupType.Date;
                    }
                    break;
                case "TimeLineView":
                    {
                        projBudgetPlanScheduler.ActiveViewIndex = 1;
                        projBudgetPlanScheduler.GroupType = SchedulerGroupType.Resource;
                    }
                    break;
                default:
                    controlRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void BindScheduler()
        {
            busyIndicator.IsBusy = true;
            var projBudgetLinesLst = await api.Query<ProjectBudgetLineClient>(Employee);
           
            if(projBudgetLinesLst == null)
            { 
                busyIndicator.IsBusy = false;
                return;
            }
            _budgetLinesLst = new ObservableCollection<ProjectBudgetLineClient>(projBudgetLinesLst?.Where(x => x.Employee != null && x.EmployeeName != null));

            var emplst = from c in _budgetLinesLst
                         group c by new { c.Employee, c.EmployeeName } into gcs
                         select new EmployeeLocal()
                         {
                             Employee = gcs.Key.Employee,
                             EmployeeName = gcs.Key.EmployeeName,
                         };

            schedulerDataSource.AppointmentLabelsSource = _budgetLinesLst.ToArray();
            schedulerDataSource.AppointmentStatusesSource = emplst.ToArray();
            schedulerDataSource.AppointmentsSource = _budgetLinesLst;
            schedulerDataSource.ResourcesSource = emplst.ToArray();
            projBudgetPlanScheduler.DataSource = schedulerDataSource;
            busyIndicator.IsBusy = false;
        }

        void GetMappings()
        {
            schedulerDataSource = new DataSource();
            AppointmentMappings AppointmentMapp = new AppointmentMappings();
            AppointmentMapp.Start = "TimeFrom";
            AppointmentMapp.End = "TimeTo";
            AppointmentMapp.LabelId = "Project";
            AppointmentMapp.Subject = "Text";
            AppointmentMapp.ResourceId = "Employee";
            schedulerDataSource.AppointmentMappings = AppointmentMapp;

            ResourceMappings ResourceMapp = new ResourceMappings();
            ResourceMapp.Caption = "EmployeeName";
            ResourceMapp.Id = "Employee";
            schedulerDataSource.ResourceMappings = ResourceMapp;
        }

        internal class EmployeeLocal
        {
            public string Employee { get; set; }
            public string EmployeeName { get; set; }
        }
    }
}
