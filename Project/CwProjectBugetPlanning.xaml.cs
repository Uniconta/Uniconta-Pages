using System;
using System.Collections.Generic;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Scheduling;
using System;
using System.Globalization;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DevExpress.Mvvm;
using Uniconta.Common;
using DevExpress.XtraScheduler;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwProjectBugetPlanning : ChildWindow
    {
        public string TimeEditMask { get { return CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern; } }
        CrudAPI crudApi;
        ProjectBugetPlanningVM dc;
        SQLCache payrollCatCache, taskCache, workspaceCache, projectCache, employeeCache;
        string employee, projectName;
        ProjectClient _project;
        public CwProjectBugetPlanning(CrudAPI api, string _employee, ProjectClient project=null)
        {
            InitializeComponent();
            employee= _employee;
            crudApi = api;
            LoadCacheInBackGround();
            lePayrollCategory.api= leTask.api= leWorkspace.api= leProject.api = crudApi;
            Loaded+=CwProjectBugetPlanning_Loaded;
            if (!api.CompanyEntity.ProjectTask)
            {
                leTask.Visibility = Visibility.Collapsed;
                lblTask.Visibility = Visibility.Collapsed;
            }
            if (project != null)
            {
                leProject.IsEnabled = false;
                _project = project;
            }
        }

        protected async void LoadCacheInBackGround()
        {
            var Comp = crudApi.CompanyEntity;
            payrollCatCache = Comp.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory));
            if (payrollCatCache == null)
                payrollCatCache = await Comp.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory), crudApi);
            lePayrollCategory.ItemsSource = payrollCatCache;

            workspaceCache = Comp.GetCache(typeof(Uniconta.DataModel.PrWorkSpace));
            if (workspaceCache == null)
                workspaceCache = await Comp.LoadCache(typeof(Uniconta.DataModel.PrWorkSpace), crudApi);
            leWorkspace.ItemsSource = workspaceCache;

            projectCache = crudApi.GetCache(typeof(Uniconta.DataModel.Project));
            if (projectCache == null)
                projectCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Project), crudApi);

            employeeCache = Comp.GetCache(typeof(Uniconta.DataModel.Employee));
            if (employeeCache == null)
                employeeCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Employee), crudApi);

            var emp = (EmployeeClient)employeeCache?.Get(employee);
            if (emp!= null)
            {
                emp.EmpProjects = null;
                await emp.LoadEmpProjects(crudApi);
                var empProj = emp.EmpProjects != null ? emp.EmpProjects.ToList() : null;
                if (empProj != null && empProj.Count > 0)
                    emp.EmpProjects = empProj.Where(s => (s._Blocked == false && (s._Phase == ProjectPhase.Created || s._Phase == ProjectPhase.Accepted || s._Phase == ProjectPhase.InProgress))).OrderBy(x => x.Number);
                
                if (emp.EmpProjects?.Count() >0)
                    leProject.ItemsSource = emp.EmpProjects;
                else
                    leProject.ItemsSource= projectCache;
            }
            else
                leProject.ItemsSource = projectCache;

            if (_project != null)
                leProject.SelectedItem = _project;

            var workspace = ((PrWorkSpace[])workspaceCache?.GetRecords)?.Where(x => x._Default==true)?.FirstOrDefault();
            leWorkspace.SelectedItem= workspace;

            SetProjectTask(leProject.Text);
        }

        async void SetProjectTask(string projectId)
        {
            if (crudApi.CompanyEntity.ProjectTask)
            {
                if (!string.IsNullOrEmpty( projectId))
                {
                    var prj = (Uniconta.DataModel.Project)projectCache?.Get(projectId);
                    var tasks = prj.Tasks ?? await prj.LoadTasks(crudApi);
                    leTask.ItemsSource = tasks?.Where(s => s.Ended == false && s._WorkSpace == leWorkspace.Text);
                }
                else
                {
                    leTask.ItemsSource = null;
                }
            }
        }

        private void CwProjectBugetPlanning_Loaded(object sender, RoutedEventArgs e)
        {
            if (dc== null)
                this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("NewOBJ"), Uniconta.ClientTools.Localization.lookup("Task"));
            else
                this.Title = string.Format("{0}- ['{1}' {2}]", Uniconta.ClientTools.Localization.lookup("Edit"), dc.Project, Uniconta.ClientTools.Localization.lookup("Task"));
        }

        private void btnAddnSave_Click(object sender, RoutedEventArgs e)
        {
            if (dc == null || !Validate(dc)) return;
            dc.Subject = txtFieldText.Text;
            dc.TimeInterval.EndDate = dc.TimeInterval.StartDate;
            dc.Appointment.CustomFields["Project"] = leProject.Text;
            dc.Appointment.CustomFields["ProjectName"] = projectName;
            dc.Appointment.CustomFields["PayrollCategory"] = lePayrollCategory.Text;
            dc.Appointment.CustomFields["Task"] = leTask.Text;
            dc.Appointment.CustomFields["WorkSpace"] = leWorkspace.Text;
            dc.Appointment.CustomFields["Qty"] = dc.TimeInterval.EndTime.Subtract(dc.TimeInterval.StartTime).TotalHours;
            if (dc.SaveAndCloseAppointmentCommand.CanExecute(null))
                dc.SaveAndCloseAppointmentCommand.Execute(null);
        }

        private void btnCance_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        bool Validate(ProjectBugetPlanningVM dc)
        {
            bool val = true;
            if (dc.CustomFields["PayrollCategory"] == null || string.IsNullOrWhiteSpace(lePayrollCategory.Text))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("PayrollCategory")),
                    Uniconta.ClientTools.Localization.lookup("Warning"));
                val = false;
            }
            if(dc.TimeInterval.StartTime.Equals(dc.TimeInterval.EndTime))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("TimeCannotBeSame"),
                    Uniconta.ClientTools.Localization.lookup("Warning"));
                val= false;
            }
            return val;
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                this.Close();
        }

        private void leProject_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var project = leProject.SelectedItem as Uniconta.DataModel.Project;
            if (project != null)
            {
                SetProjectTask(project?.KeyStr);
                projectName = project._Name;
            }
        }

        private void leTask_GotFocus(object sender, RoutedEventArgs e)
        {
            var project = leProject.SelectedItem as Uniconta.DataModel.Project;
            if (project != null)
                SetProjectTask(project?.KeyStr);
        }

        private void schLabel_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var appointment = e.NewValue as AppointmentLabelItem;
            if (appointment != null)
            {
                string id = appointment.Id as string;
                projectCache = projectCache ?? crudApi.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Project));
                var project = (Uniconta.DataModel.Project)projectCache?.Get(id) as ProjectClient;
                SetProjectTask(project?.KeyStr);
            }
        }

        private void btnRecurrence_Click(object sender, RoutedEventArgs e)
        {
            if (dc == null) return;
            dc.Scheduler.ShowRecurrenceWindow(dc.Appointment);
        }

        private void leCategory_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if(dc== null)
                dc= this.DataContext as ProjectBugetPlanningVM;
            var payRollCat = lePayrollCategory.SelectedItem as EmpPayrollCategory;
            if(payRollCat!= null)
            {
                if (payRollCat._PrCategory != null)
                    dc.CustomFields["Category"] = payRollCat._PrCategory;
                else
                {
                    System.Windows.MessageBox.Show(string.Format("'{0}' {1}", payRollCat.KeyStr,Uniconta.ClientTools.Localization.lookup("PrCategoryMessage")));
                    lePayrollCategory.SelectedItem= null;
                    dc.CustomFields["Category"]= null;
                }
            }
        }
    }

    public class ProjectBugetPlanningVM : AppointmentWindowViewModel
    {
        string category;
        string payrollCategory;
        string task;
        string workspace;
        string project;
        DateTime startDate;
        public ProjectBugetPlanningVM(AppointmentItem appointmentItem, SchedulerControl scheduler) : base(appointmentItem, scheduler)
        {
            CustomFields["Project"]= project = appointmentItem.CustomFields["Project"]?.ToString();
            CustomFields["ProjectName"] = appointmentItem.CustomFields["ProjectName"]?.ToString();
            CustomFields["Date"] = startDate = appointmentItem.Start.Date;
            CustomFields["FieldText"] = appointmentItem.Subject;
            CustomFields["Category"] = category = appointmentItem.CustomFields["Category"]?.ToString();
            CustomFields["PayrollCategory"]= payrollCategory = appointmentItem.CustomFields["PayrollCategory"]?.ToString();
            CustomFields["Task"]= task= appointmentItem.CustomFields["Task"]?.ToString();
            CustomFields["WorkSpace"]= workspace = appointmentItem.CustomFields["WorkSpace"]?.ToString();
        }

        public string Project
        {
            get { return project; }
            set
            {
                project=value; ;
                CustomFields["Project"] = project;
            }
        }

        public string Category
        { 
            get { return category; }
            set
            {
                category=value; ;
                CustomFields["Category"] = category;
            }
        }

        public string PayrollCategory
        {
            get { return payrollCategory; }
            set
            {
                payrollCategory=value;
                CustomFields["PayrollCategory"] = payrollCategory;
            }
        }

        public string Task
        {
            get { return task; }
            set
            {
                task=value;
                CustomFields["Task"] = task;
            }
        }

        public string WorkSpace
        {
            get { return workspace; }
            set
            {
                workspace=value;
                CustomFields["WorkSpace"] = workspace;
            }
        }
    }
}
