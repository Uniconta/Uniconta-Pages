using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;
using System.Collections.ObjectModel;
using UnicontaClient.Controls.TaskScheduler;
using Uniconta.ClientTools.DataModel;
using System.Threading.Tasks;
using DevExpress.Xpf.Scheduler;
using DevExpress.XtraScheduler;
using DevExpress.XtraScheduler.Commands;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using DevExpress.Xpf.Scheduler.UI;
using DevExpress.Xpf.Scheduler.Menu;
using DevExpress.Xpf.Bars;


using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class EmployeeTaskSchedulerPage : ControlBasePage
    {
        private List<EmployeeJournalLineClient> _employelinesslist;
        private List<UnicontaBaseEntity> _masterList;
        private UnicontaBaseEntity _currentCorasauBaseEntity;
        private string _employee;
        public EmployeeTaskSchedulerPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            employeeScheduler.EditAppointmentFormShowing += employeeScheduler_EditAppointmentFormShowing;
            employeeScheduler.AppointmentViewInfoCustomizing += employeeScheduler_AppointmentViewInfoCustomizing;
            employeeScheduler.PopupMenuShowing += employeeScheduler_PopupMenuShowing;
            employeeScheduler.SchedulerAppointmentActionEvent += employeeScheduler_SchedulerAppointmentActionEvent;
            employeeScheduler.StorageAppointmentActionEvent += employeeScheduler_StorageAppointmentActionEvent;
            employeeScheduler.TaskDataType = master.GetType();
            if (master != null)
            {
                var empClient = master as EmployeeClient;
                _employee = empClient.Number;
                _masterList = new List<UnicontaBaseEntity>();
                _masterList.Add(master);
                InitPage();
            }

        }

        void employeeScheduler_PopupMenuShowing(object sender, SchedulerMenuEventArgs e)
        {
            if (e.Menu.Name == SchedulerMenuItemName.DefaultMenu)
            {
                ISchedulerCustomPopup customPopup = new EmployeeTaskSchedulerCustomPopup();
                
                //Modifying the Contents
                for (int i = 0; i < e.Menu.ItemLinks.Count; i++)
                {
                    
                    if (e.Menu.ItemLinks[i] is BarSubItemLink)
                    {
                        var menuCheckItem = e.Menu.ItemLinks[i] as BarSubItemLink;

                        foreach (var itemlink in menuCheckItem.ItemLinks)
                        {
                            var menuSubItem = itemlink as BarButtonItemLink;
                            customPopup.ModifyItem(menuSubItem.Item as SchedulerBarCheckItem);
                        }
                    }
                    else if (e.Menu.ItemLinks[i] is BarButtonItemLink)
                    {
                        var menuItem = e.Menu.ItemLinks[i] as BarButtonItemLink;
                        customPopup.ModifyItem(menuItem.Item as SchedulerBarItem);
                    }
                }
                //Removing the Items
                var itemsRemove = customPopup.RemoveItems();
                foreach (string item in itemsRemove)
                {
                    e.Customizations.Add(new RemoveBarItemAndLinkAction() { ItemName = item });
                }
            }
        }

        async void employeeScheduler_SchedulerAppointmentActionEvent(Appointment Original, Appointment Edited, SchedulerAppointmentAction Action)
        {
            busyIndicator.IsBusy = true;
            var orifinalSource = CreateSourceObject(Original);
            var editedSource = ConvertToCorasauEntity(Edited);
            var errorCode = await api.Update(orifinalSource, editedSource);
            busyIndicator.IsBusy = false;
            if (errorCode != ErrorCodes.Succes)
                UnicontaMessageBox.Show(errorCode.ToString(), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
        }

        async void employeeScheduler_StorageAppointmentActionEvent(Appointment appointment, SchedulerAppointmentAction action)
        {
            var employeeJournalLine = ConvertToCorasauEntity(appointment);
            busyIndicator.IsBusy = true;
            var result = ErrorCodes.NoSucces;
            switch (action)
            {
                case SchedulerAppointmentAction.Delete:
                    result = await api.Delete(employeeJournalLine);
                    break;

                case SchedulerAppointmentAction.Insert:
                    result = await api.Insert(employeeJournalLine);
                    break;

                case SchedulerAppointmentAction.Modify:
                    if (_currentCorasauBaseEntity == null)
                        return;
                    result = await api.Update(_currentCorasauBaseEntity, employeeJournalLine);
                    break;
            }
            busyIndicator.IsBusy = false;
            if (result != ErrorCodes.Succes)
                UnicontaMessageBox.Show(result.ToString(), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
        }

        async void employeeScheduler_TaskResizeDragDropEvent(Appointment OriginalAppointment, Appointment EditedAppointment)
        {
            var originalSource = CreateSourceObject(OriginalAppointment);
            var editedSource = ConvertToCorasauEntity(EditedAppointment);
            busyIndicator.IsBusy = true;
            var errorCodes = await api.Update(originalSource, editedSource);
            busyIndicator.IsBusy = false;
        }

        private UnicontaBaseEntity CreateSourceObject(Appointment appointment)
        {
            var source = appointment.GetSourceObject(employeeScheduler.GetCoreStorage()) as EmployeeJournalLineClient;
            if (source == null)
                return null;
            return StreamingManager.Clone(source);
        }

        private UnicontaBaseEntity ConvertToCorasauEntity(Appointment appointment)
        {
            var employeJournalline = CreateSourceObject(appointment) as EmployeeJournalLineClient;

            if (employeJournalline == null)
                employeJournalline = new EmployeeJournalLineClient();

            employeJournalline.FromTime = appointment.Start;
            employeJournalline.ToTime = appointment.End;
            employeJournalline.Employee = _employee;
            employeJournalline.Project = employeeScheduler.Storage.AppointmentStorage.Labels[appointment.LabelId].DisplayName;
            employeJournalline.CostCategory = employeeScheduler.Storage.AppointmentStorage.Statuses[appointment.StatusId].DisplayName;
            employeJournalline.Qty = appointment.CustomFields["FieldQty"] != null ? Convert.ToDouble(appointment.CustomFields["FieldQty"]) : 0.0;
            employeJournalline.SalesPrice = appointment.CustomFields["FieldSalesPrice"] != null ? Convert.ToDouble(appointment.CustomFields["FieldSalesPrice"]) : 0.0;
            employeJournalline.CostPrice = appointment.CustomFields["FieldCostPrice"] != null ? Convert.ToDouble(appointment.CustomFields["FieldCostPrice"]) : 0.0;
            employeJournalline.DiscountPct = appointment.CustomFields["FieldDiscountPct"] != null ? Convert.ToDouble(appointment.CustomFields["FieldDiscountPct"]) : 0.0;
            employeJournalline.Text = appointment.CustomFields["FieldText"] != null ? Convert.ToString(appointment.CustomFields["FieldText"]) : string.Empty;

            return StreamingManager.Clone(employeJournalline);
        }

        async private void InitPage()
        {
            MainControl = employeeScheduler;
            ribbonControl = frmRibbon;
            GetMappings();
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            busyIndicator.IsBusy = true;
            await GenerateProjectAndCostCategories(employeeScheduler.Storage.AppointmentStorage);
            _employelinesslist = await GetEmployeJournalLines();
            employeeScheduler.Storage.AppointmentStorage.DataSource = _employelinesslist;
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
                    employeeScheduler.ActiveView.GotoTimeInterval(new TimeInterval(BasePage.GetSystemDefaultDate(), BasePage.GetSystemDefaultDate()));
                    break;
                case "ZoomIn":
                    employeeScheduler.ActiveView.ZoomIn();
                    break;
                case "ZoomOut":
                    employeeScheduler.ActiveView.ZoomOut();
                    break;
                case "DayView":
                    employeeScheduler.ActiveViewType = SchedulerViewType.Day;
                    break;
                case "WorkWeekView":
                    employeeScheduler.ActiveViewType = SchedulerViewType.WorkWeek;
                    break;
                case "WeekView":
                    employeeScheduler.ActiveViewType = SchedulerViewType.Week;
                    break;
                case "MonthView":
                    employeeScheduler.ActiveViewType = SchedulerViewType.Month;
                    break;
                default:
                    controlRibbon_BaseActions(ActionType);
                    break;


            }
        }

        private void GetMappings()
        {
            AppointmentMapping mappings = employeeScheduler.Storage.AppointmentStorage.Mappings;
            mappings.Start = "FromTime";
            mappings.End = "ToTime";
            mappings.Label = "ProjectId";
            mappings.Status = "CostCategoryId";
            employeeScheduler.Storage.AppointmentStorage.CustomFieldMappings.Add(new SchedulerCustomFieldMapping() { Name = "FieldText", Member = "Text", ValueType = FieldValueType.String });
            employeeScheduler.Storage.AppointmentStorage.CustomFieldMappings.Add(new SchedulerCustomFieldMapping() { Name = "FieldCostPrice", Member = "CostPrice", ValueType = FieldValueType.Object });
            employeeScheduler.Storage.AppointmentStorage.CustomFieldMappings.Add(new SchedulerCustomFieldMapping() { Name = "FieldDiscountPct", Member = "DiscountPct", ValueType = FieldValueType.Object });
            employeeScheduler.Storage.AppointmentStorage.CustomFieldMappings.Add(new SchedulerCustomFieldMapping() { Name = "FieldSalesPrice", Member = "SalesPrice", ValueType = FieldValueType.Object });
            employeeScheduler.Storage.AppointmentStorage.CustomFieldMappings.Add(new SchedulerCustomFieldMapping() { Name = "FieldQty", Member = "Qty", ValueType = FieldValueType.Object });
        }

        async private Task GenerateProjectAndCostCategories(AppointmentStorage appointmentStorage)
        {
            appointmentStorage.Statuses.Clear();
            appointmentStorage.Labels.Clear();

            //Adding Projects
            var projects = await api.Query<ProjectClient>();

            foreach (var proj in projects)
            {
                int projNumber = 0;
                int.TryParse(proj.Number, out projNumber);
                appointmentStorage.Labels.Add(new AppointmentLabel(GetColorForLabel(projNumber), proj.Number, string.Concat("Project#", proj.Number)));
            }

            //Adding Cost Category
            var costCategories = await api.Query<ProjectCostCategoryClient>();
            foreach (var costCat in costCategories)
            {
                appointmentStorage.Statuses.Add(new AppointmentStatus(AppointmentStatusType.Custom, GetColorForStatus(costCat.Name), costCat.Number, costCat.Number));
            }
        }

        private Color GetColorForStatus(string name)
        {
            var hashCode = name.GetHashCode();

            if (hashCode < 0)
                hashCode *= -1;

            byte[] bytes = BitConverter.GetBytes(hashCode);

            return Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
        }

        private Color GetColorForLabel(int factor)
        {
            byte[] values = BitConverter.GetBytes(factor);

            if (!BitConverter.IsLittleEndian)
                Array.Reverse(values);

            return Color.FromArgb(values[0], values[1], values[2], values[3]);
        }

        async private Task<List<EmployeeJournalLineClient>> GetEmployeJournalLines()
        {
            var employeJournalLine = new List<EmployeeJournalLineClient>();
            var results = await api.Query<EmployeeJournalLineClient>(_masterList, null);
            if (results != null)
            {
                foreach (var line in results)
                {
                    line.ProjectId = employeeScheduler.Storage.AppointmentStorage.Labels.Select((v, i) => new { Lab = v, ind = i }).First(p => p.Lab.DisplayName == line.Project).ind;
                    line.CostCategoryId = employeeScheduler.Storage.AppointmentStorage.Statuses.Select((v, i) => new { Lab = v, ind = i }).First(p => p.Lab.DisplayName == line.CostCategory).ind;
                    employeJournalLine.Add(line);
                }
            }
            return employeJournalLine;
        }

        void employeeScheduler_AppointmentViewInfoCustomizing(object sender, DevExpress.Xpf.Scheduler.AppointmentViewInfoCustomizingEventArgs e)
        {
            EmployeeTaskSchedulerData customFieldsData = new EmployeeTaskSchedulerData();
            customFieldsData.CostPrice = e.ViewInfo.Appointment.CustomFields["FieldCostPrice"] != null ? Convert.ToDouble(e.ViewInfo.Appointment.CustomFields["FieldCostPrice"]) : 0.0;
            customFieldsData.DiscountPct = e.ViewInfo.Appointment.CustomFields["FieldDiscountPct"] != null ? Convert.ToDouble(e.ViewInfo.Appointment.CustomFields["FieldDiscountPct"]) : 0.0;
            customFieldsData.SalesPrice = e.ViewInfo.Appointment.CustomFields["FieldSalesPrice"] != null ? Convert.ToDouble(e.ViewInfo.Appointment.CustomFields["FieldSalesPrice"]) : 0.0;
            customFieldsData.Qty = e.ViewInfo.Appointment.CustomFields["FieldQty"] != null ? Convert.ToDouble(e.ViewInfo.Appointment.CustomFields["FieldQty"]) : 0.0;
            customFieldsData.Text = e.ViewInfo.Appointment.CustomFields["FieldText"] != null ? e.ViewInfo.Appointment.CustomFields["FieldText"].ToString() : "";
            e.ViewInfo.CustomViewInfo = customFieldsData;
        }

        void employeeScheduler_EditAppointmentFormShowing(object sender, DevExpress.Xpf.Scheduler.EditAppointmentFormEventArgs e)
        {
            _currentCorasauBaseEntity = CreateSourceObject(e.Appointment);
            e.Form = new EmployeeTaskSchedulerDialog(employeeScheduler, e.Appointment);
        }

        public override string NameOfControl
        {
            get { return TabControls.EmployeeTaskSchedulerPage; }
        }
    }

}
