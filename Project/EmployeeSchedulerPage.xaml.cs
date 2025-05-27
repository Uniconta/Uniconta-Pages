using DevExpress.Xpf.Scheduling;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Controls.TaskScheduler;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DevExpress.XtraScheduler;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class EmployeeSchedulerPage : ControlBasePage
    {
        public ObservableCollection<EmployeeSchedulerLocal> _empSchedulerLinesLst;
        public List<UnicontaBaseEntity> _masterList;
        public List<ProjectLocal> _projectList;
        public List<CostCategoryLocal> _costCatList;
        private UnicontaBaseEntity _currentCorasauBaseEntity;
        private string _employee;
        private EmployeeClient Employee;
        private ProjectBudgetClient _projectBudget;

        public EmployeeSchedulerPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            this.DataContext = this;
            MainControl = employeeScheduler;
            ribbonControl = frmRibbon;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            employeeScheduler.AppointmentWindowShowing += EmployeeScheduler_AppointmentWindowShowing;
            employeeScheduler.ItemPropertyChanged += EmployeeScheduler_ItemPropertyChanged;
            employeeScheduler.Loaded += EmployeeScheduler_Loaded;
            employeeScheduler.InitNewAppointment += EmployeeScheduler_InitNewAppointment;
            employeeScheduler.AppointmentDrop += EmployeeScheduler_AppointmentDrop;
            employeeScheduler.AppointmentResize += EmployeeScheduler_AppointmentResize;
            employeeScheduler.AllowAppointmentEdit = true;
            employeeScheduler.AllowAppointmentDelete = true;
            employeeScheduler.AllowInplaceEditor = true;
            employeeScheduler.CustomAllowAppointmentEdit += EmployeeScheduler_CustomAllowAppointmentEdit;
            employeeScheduler.CustomAllowInplaceEditor += EmployeeScheduler_CustomAllowInplaceEditor;
            employeeScheduler.CustomAllowAppointmentDelete += EmployeeScheduler_CustomAllowAppointmentDelete;
            employeeScheduler.TaskDataType = master.GetType();
            RemoveMenuItem();

            if (master != null)
            {
                Employee = master as EmployeeClient;
                _employee = Employee.Number;
                _masterList = new List<UnicontaBaseEntity>();
                _masterList.Add(master);
                GetMappings();
            }
        }

        private void EmployeeScheduler_CustomAllowAppointmentDelete(object sender, AppointmentItemOperationEventArgs e)
        {
            e.Allow = (bool)e.Appointment.CustomFields["IsBudgetLine"];
        }

        private void EmployeeScheduler_CustomAllowInplaceEditor(object sender, AppointmentItemOperationEventArgs e)
        {
            e.Allow = (bool)e.Appointment.CustomFields["IsBudgetLine"];
        }

        private void EmployeeScheduler_CustomAllowAppointmentEdit(object sender, AppointmentItemOperationEventArgs e)
        {
            e.Allow = (bool)e.Appointment.CustomFields["IsBudgetLine"];
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
            var Comp = api.CompanyEntity;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "ZoomIn", "ZoomOut" });
        }

        private void EmployeeScheduler_InitNewAppointment(object sender, AppointmentItemEventArgs e)
        {
            e.Appointment.Reminders.Clear();
            e.Appointment.CustomFields["FieldCostPrice"] = 0d;
            e.Appointment.CustomFields["FieldDiscountPct"] = 0d;
            e.Appointment.CustomFields["FieldSalesPrice"] = 0d;
            e.Appointment.CustomFields["FieldQty"] = 0d;
            e.Appointment.CustomFields["FieldCost"] = 0d;
            e.Appointment.CustomFields["FieldSales"] = 0d;
            e.Appointment.CustomFields["IsBudgetLine"] = true;
        }

        private void EmployeeScheduler_Loaded(object sender, RoutedEventArgs e)
        {
            BindScheduler();
            employeeScheduler.Loaded -= EmployeeScheduler_Loaded;
        }

        DataSource schedulerDataSource;
        void GetMappings()
        {
            schedulerDataSource = new DataSource();
            AppointmentMappings AppointmentMapp = new AppointmentMappings();
            AppointmentMapp.Start = "FromTime";
            AppointmentMapp.End = "ToTime";
            AppointmentMapp.LabelId = "Project";
            AppointmentMapp.StatusId = "TaskType";
            AppointmentMapp.Subject = "Text";
            AppointmentMapp.ResourceId = "TaskType";
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Date", Name = "Date" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "PrCategory", Name = "Category" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Task", Name = "FieldProjectTask" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Text", Name = "FieldText" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "CostPrice", Name = "FieldCostPrice" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "SalesPrice", Name = "FieldSalesPrice" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Cost", Name = "FieldCost" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Sales", Name = "FieldSales" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "Qty", Name = "FieldQty" });
            AppointmentMapp.CustomFieldMappings.Add(new CustomFieldMapping { Mapping = "IsBudgetLine", Name = "IsBudgetLine" });
            schedulerDataSource.AppointmentMappings = AppointmentMapp;

            AppointmentLabelMappings AppointmentLabelMapp = new AppointmentLabelMappings();
            AppointmentLabelMapp.Caption = "ProjectName";
            AppointmentLabelMapp.Id = "Number";
            AppointmentLabelMapp.Color = "Color";
            AppointmentLabelMapp.ColorSavingType = DXColorSavingType.ColorInstance;
            schedulerDataSource.AppointmentLabelMappings = AppointmentLabelMapp;

            AppointmentStatusMappings AppointmentStatusMapp = new AppointmentStatusMappings();
            AppointmentStatusMapp.Id = "Id";
            AppointmentStatusMapp.Brush = "Brush";
            AppointmentStatusMapp.BrushSavingType = BrushSavingType.Brush;
            schedulerDataSource.AppointmentStatusMappings = AppointmentStatusMapp;

            ResourceMappings ResourceMapp = new ResourceMappings();
            ResourceMapp.Caption = "LineType";
            ResourceMapp.Id = "Id";
            schedulerDataSource.ResourceMappings = ResourceMapp;
        }

        async void BindScheduler()
        {
            await SetList();
            schedulerDataSource.AppointmentLabelsSource = _projectList.ToArray();

            var taskTypeLst = new List<TaskType>();
            taskTypeLst.Add(new TaskType { Id = "1", LineType = Uniconta.ClientTools.Localization.lookup("Budget"), Brush = System.Drawing.Brushes.Red });
            taskTypeLst.Add(new TaskType { Id = "2", LineType = Uniconta.ClientTools.Localization.lookup("Actual"), Brush = System.Drawing.Brushes.Yellow });
            schedulerDataSource.AppointmentStatusesSource = taskTypeLst.ToArray();
            schedulerDataSource.AppointmentsSource = _empSchedulerLinesLst;
            schedulerDataSource.ResourcesSource = taskTypeLst.ToArray();
            employeeScheduler.DataSource = schedulerDataSource;

            if (_empSchedulerLinesLst != null)
                _empSchedulerLinesLst.CollectionChanged += _empSchedulerLinesLst_CollectionChanged;

            employeeScheduler.GroupType = SchedulerGroupType.Date;

            var view = employeeScheduler.Views;
            if (view != null)
            {
                (view[0] as SchedulerViewBase).ResourcesPerPage = 2;
                (view[0] as DayView).ShowAllDayArea = false;
                (view[0] as DayView).VisibleTime = new DevExpress.Mvvm.TimeSpanRange(TimeSpan.FromHours(0), TimeSpan.FromHours(24));
                //(view[0] as DayView).WorkTime = new DevExpress.Mvvm.TimeSpanRange(TimeSpan.FromHours(8), TimeSpan.FromHours(17));
                (view[0] as DayView).TimeScale = new TimeSpan(0, 30, 0);
                (view[0] as DayView).SnapToCellsMode = SnapToCellsMode.Never;
                (view[0] as DayView).TopRowTime = TimeSpan.FromHours(8);

                (view[0] as SchedulerViewBase).ResourcesPerPage = 2;
                (view[1] as WorkWeekView).ShowAllDayArea = false;
                (view[1] as WorkWeekView).VisibleTime = new DevExpress.Mvvm.TimeSpanRange(TimeSpan.FromHours(0), TimeSpan.FromHours(24));
                // TODO (view[1] as WorkWeekView).WorkTime = new DevExpress.Mvvm.TimeSpanRange(TimeSpan.FromHours(8), TimeSpan.FromHours(17));
                (view[1] as WorkWeekView).TimeScale = new TimeSpan(0, 30, 0);
                (view[1] as WorkWeekView).SnapToCellsMode = SnapToCellsMode.Never;
                (view[1] as WorkWeekView).TopRowTime = TimeSpan.FromHours(8);

                (view[0] as SchedulerViewBase).ResourcesPerPage = 2;
                (view[2] as WeekView).ShowAllDayArea = false;
                (view[2] as WeekView).VisibleTime = new DevExpress.Mvvm.TimeSpanRange(TimeSpan.FromHours(0), TimeSpan.FromHours(24));
                //(view[2] as WeekView).WorkTime = new DevExpress.Mvvm.TimeSpanRange(TimeSpan.FromHours(8), TimeSpan.FromHours(17));
                (view[2] as WeekView).TimeScale = new TimeSpan(0, 30, 0);
                (view[2] as WeekView).SnapToCellsMode = SnapToCellsMode.Never;
                (view[2] as WeekView).TopRowTime = TimeSpan.FromHours(8);

                //view[3].ResourcesPerPage = 2;
            }
        }

        async Task SetList()
        {
            busyIndicator.IsBusy = true;
            var projects = await api.Query<ProjectLocal>();
            _projectList = new List<ProjectLocal>();
            foreach (var proj in projects)
            {
                int projNumber = 0;
                int.TryParse(proj.Number, out projNumber);
                proj.Color = GetColorForLabel(projNumber);
                _projectList.Add(proj);
            }

            var costList = await api.Query<CostCategoryLocal>();
            _costCatList = costList.ToList();

            // for ProjectBudgetLineClient
            var projBudget = await api.Query<ProjectBudgetClient>();
            var _projectBudget = projBudget.FirstOrDefault(x => x._Current == true);
            if (_projectBudget == null)
            {
                _projectBudget = new ProjectBudgetClient();
                _projectBudget.Name = Uniconta.ClientTools.Localization.lookup("Default");
                _projectBudget._Current = true;
                _projectBudget.SetMaster(api.CompanyEntity);
                var err = await api.Insert(_projectBudget);
                if (err != ErrorCodes.Succes)
                {
                    UtilDisplay.ShowErrorCode(err);
                    return;
                }
            }

            var projBudgetLinesLst = await api.Query<EmployeeSchedulerLocal>(new UnicontaBaseEntity[] { _projectBudget, Employee }, null);
            _empSchedulerLinesLst = new ObservableCollection<EmployeeSchedulerLocal>(projBudgetLinesLst);

            // for ProjectJournalLineClient
            var projJournalLinesLst = await api.Query<ProjectJournalLineClient>(_masterList, null);

            var Comp = api.CompanyEntity;
            var cache = Comp.GetCache(typeof(Uniconta.DataModel.PrCategory));
            if (cache != null)
            {
                foreach (var empJournalLine in projJournalLinesLst)
                {
                    var rec = (Uniconta.DataModel.PrCategory)cache.Get(empJournalLine._PrCategory);
                    if (rec != null && rec._CatType == Uniconta.DataModel.CategoryType.Labour)
                    {
                        var item = new EmployeeSchedulerLocal()
                        {
                            _Date = empJournalLine._Date,
                            _Project = empJournalLine._Project,
                            _PrCategory = empJournalLine._PrCategory,
                            _Task = empJournalLine._Task,
                            _FromTime = empJournalLine._TimeFrom,
                            _ToTime = empJournalLine._TimeTo,
                            _Text = empJournalLine._Text,
                            _Qty = empJournalLine._Qty,
                            _CostPrice = empJournalLine._CostPrice,
                            _SalesPrice = empJournalLine._SalesPrice,
                            _Item = empJournalLine._Item,
                            IsBudgetLine = false,
                            TaskType = "2"
                        };
                        _empSchedulerLinesLst.Add(item);
                    }
                }
            }
            busyIndicator.IsBusy = false;
        }

        void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddTask":
                    employeeScheduler.NewTask();
                    break;
                case "EditTask":
                    employeeScheduler.EditTask();
                    break;
                case "DeleteTask":
                    employeeScheduler.DeleteTask();
                    break;
                case "Backward":
                    employeeScheduler.NavigateBackward();
                    break;
                case "Forward":
                    employeeScheduler.NavigateForward();
                    break;
                case "GoToday":
                    {
                        employeeScheduler.GoToday();
                        employeeScheduler.GroupType = SchedulerGroupType.Date;
                    }
                    break;
                case "DayView":
                    {
                        employeeScheduler.DayView();
                        employeeScheduler.GroupType = SchedulerGroupType.Date;
                    }
                    break;
                case "WeekView":
                    {
                        employeeScheduler.WeekView();
                        employeeScheduler.GroupType = SchedulerGroupType.Date;
                    }
                    break;
                case "WorkWeekView":
                    {
                        employeeScheduler.WorkWeekView();
                        employeeScheduler.GroupType = SchedulerGroupType.Date;
                    }
                    break;
                case "MonthView":
                    {
                        employeeScheduler.MonthView();
                        employeeScheduler.GroupType = SchedulerGroupType.Resource;
                    }
                    break;
                default:
                    controlRibbon_BaseActions(ActionType);
                    break;
            }
        }

        bool isHandled = false;
        private void EmployeeScheduler_ItemPropertyChanged(object sender, DevExpress.Xpf.Scheduling.ItemPropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Start" || e.PropertyName == "End")
                (e.Item as AppointmentItem).CustomFields["FieldQty"] = (e.Item.SourceObject as EmployeeSchedulerLocal).Qty;

            if (!isHandled)
            {
                isHandled = true;
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    var empBudgetLine = e.Item.SourceObject as EmployeeSchedulerLocal;
                    if (empBudgetLine != null)
                    {
                        if (empBudgetLine.IsBudgetLine)
                            SaveProjectBudgetLine(empBudgetLine, SchedulerAction.Modify);
                        else
                            SaveProjectJournalLine(empBudgetLine, SchedulerAction.Modify);
                    }
                    isHandled = false;
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void _empSchedulerLinesLst_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                var empBudgetLine = e.NewItems[0] as EmployeeSchedulerLocal;
                if (empBudgetLine != null)
                {
                    empBudgetLine._Employee = _employee;
                    empBudgetLine.SetMaster(_projectBudget);
                    SaveProjectBudgetLine(empBudgetLine, SchedulerAction.Insert);
                }
            }

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var empBudgetLine = e.OldItems[0] as EmployeeSchedulerLocal;
                if (empBudgetLine != null)
                    SaveProjectBudgetLine(empBudgetLine, SchedulerAction.Delete);
            }
        }

        private void EmployeeScheduler_AppointmentResize(object sender, AppointmentItemResizeEventArgs e)
        {
            e.Allow = (bool)e.ViewModel.CustomFields["IsBudgetLine"] && e.HitResource.Id.ToString() == "1";
            if (e.Allow)
            {
                AppointmentDragResizeViewModel viewModel = e.ViewModel;
                _currentCorasauBaseEntity = CreateSourceObject(viewModel.Appointment);
            }
        }

        private void EmployeeScheduler_AppointmentDrop(object sender, AppointmentItemDragDropEventArgs e)
        {
            e.Allow = (bool)e.ViewModels[0].CustomFields["IsBudgetLine"] && e.HitResource.Id.ToString() == "1";
            if (e.Allow)
            {
                AppointmentDragResizeViewModel viewModel = e.ViewModels[0];
                viewModel.Appointment.ResourceId = "1";
                var appointment = viewModel.Appointment.SourceObject as EmployeeSchedulerLocal;
                if (viewModel != null && appointment != null)
                {
                    _currentCorasauBaseEntity = null;
                    appointment.IsBudgetLine = true;
                    appointment.TaskType = "1";
                    appointment.Date = viewModel.Start;
                    appointment.FromTime = viewModel.Start;
                    appointment.NotifyPropertyChanged("FromTime");
                    appointment.ToTime = viewModel.End;
                    appointment.NotifyPropertyChanged("ToTime");
                }
            }
            else
            {
                AppointmentDragResizeViewModel viewModel = e.ViewModels[0];
                viewModel.Appointment.ResourceId = e.HitResource.Id.ToString();
                var appointment = viewModel.Appointment.SourceObject as EmployeeSchedulerLocal;
                var apt = new EmployeeSchedulerLocal();
                if (viewModel != null && appointment != null)
                {
                    _currentCorasauBaseEntity = null;
                    apt.IsBudgetLine = false;
                    apt.TaskType = "2";
                    apt.Date = viewModel.Start;
                    apt.FromTime = viewModel.Start;
                    apt.NotifyPropertyChanged("FromTime");
                    apt.ToTime = viewModel.End;
                    apt.NotifyPropertyChanged("ToTime");
                }
            }
        }

        async void SaveProjectJournalLine(EmployeeSchedulerLocal proJournalLine, SchedulerAction action)
        {
            busyIndicator.IsBusy = true;
            var result = ErrorCodes.NoSucces;
            switch (action)
            {
                case SchedulerAction.Delete:
                    result = await api.Delete(proJournalLine);
                    break;

                case SchedulerAction.Insert:
                    result = await api.Insert(proJournalLine);
                    break;

                case SchedulerAction.Modify:
                    if (_currentCorasauBaseEntity == null)
                        result = await api.Update(proJournalLine);
                    else
                        result = await api.Update(_currentCorasauBaseEntity, proJournalLine);
                    break;
            }
            busyIndicator.IsBusy = false;
            if (result != ErrorCodes.Succes)
                UnicontaMessageBox.Show(result.ToString(), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
        }

        async void SaveProjectBudgetLine(EmployeeSchedulerLocal proBudgetLine, SchedulerAction action)
        {
            busyIndicator.IsBusy = true;
            var result = ErrorCodes.NoSucces;
            switch (action)
            {
                case SchedulerAction.Delete:
                    result = await api.Delete(proBudgetLine);
                    break;

                case SchedulerAction.Insert:
                    result = await api.Insert(proBudgetLine);
                    break;

                case SchedulerAction.Modify:
                    result = await api.Update(proBudgetLine);
                    break;
            }
            busyIndicator.IsBusy = false;
            if (result != ErrorCodes.Succes)
                UnicontaMessageBox.Show(result.ToString(), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
        }

        private UnicontaBaseEntity CreateSourceObject(AppointmentItem appointment)
        {
            var source = appointment.SourceObject as ProjectBudgetLineClient;
            if (source == null)
                return null;
            else
                return source;
        }

        private void EmployeeScheduler_AppointmentWindowShowing(object sender, DevExpress.Xpf.Scheduling.AppointmentWindowShowingEventArgs e)
        {
            if (e.Appointment.ResourceId.ToString() == "1")
            {
                _currentCorasauBaseEntity = CreateSourceObject(e.Appointment);
                e.Appointment.ResourceId = "1";
                e.Window = new EmployeeAppointmentDailog(api);
                e.Window.DataContext = new EmployeeAppointmentDailogVM(e.Appointment, employeeScheduler, _costCatList);
            }
            else
            {
                e.Cancel = true;
                e.Handled = true;
            }
        }

        double TimeDifference(AppointmentItem appointment)
        {
            var timeDiff = (appointment.End - appointment.Start).TotalHours;
            timeDiff = Math.Round(timeDiff, 2);
            return timeDiff > 0.0 ? timeDiff : 0d;
        }

        private System.Windows.Media.Brush GetColorForStatus(string name)
        {
            var hashCode = name.GetHashCode();

            if (hashCode < 0)
                hashCode *= -1;

            byte[] bytes = BitConverter.GetBytes(hashCode);
            var color = System.Windows.Media.Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
            var brush = new SolidColorBrush(color);

            return brush;
        }

        private System.Drawing.Color GetColorForLabel(int factor)
        {
            byte[] values = BitConverter.GetBytes(factor);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(values);

            return System.Drawing.Color.FromArgb(values[0], values[1], values[2], values[3]);
        }
    }

    public class ProjectLocal : ProjectClient
    {
        public System.Drawing.Color Color { get; set; }

        public string ProjectName
        {
            get { return string.Format("{0} : {1}", Number, Name); }
        }
    }

    public class CostCategoryLocal : PrCategory
    {
        public string CostCategoryName
        {
            get { return string.Format("{0} : {1}", _Number, _Name); }
        }
    }

    public class TaskType
    {
        public string Id { get; set; }
        public System.Drawing.Brush Brush { get; set; }
        public string LineType { get; set; }
    }

    public class EmployeeSchedulerLocal : ProjectBudgetLineClient
    {
        private bool _IsBudgetLine = true;
        public bool IsBudgetLine { get { return _IsBudgetLine; } set { _IsBudgetLine = value; NotifyPropertyChanged("IsBudgetLine"); } }

        private string tasktype = "1";
        public string TaskType { get { return tasktype; } set { tasktype = value; } }

        public DateTime FromTime
        {
            get { return TimeFrom; }
            set
            {
                TimeFrom = value;
                Qty = ToTime.Subtract(FromTime).TotalHours;
                NotifyPropertyChanged("FromTime");
            }
        }

        public DateTime ToTime
        {
            get { return TimeTo; }
            set
            {
                TimeTo = value;
                Qty = ToTime.Subtract(FromTime).TotalHours;
                NotifyPropertyChanged("ToTime");
            }
        }
    }
}
