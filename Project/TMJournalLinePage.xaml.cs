using DevExpress.Data;
using DevExpress.Emf;
using DevExpress.Xpf.Core.Serialization;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.PivotGrid.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using Uniconta.API.Project;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Pages.Project.TimeManagement;
using UnicontaClient.Utilities;
using static UnicontaClient.Pages.Project.TimeManagement.TMJournalLineHelper;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TMJournalLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TMJournalLineClient); } }

        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override IComparer GridSorting { get { return new TMJournalLineSort(); } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return true; } }
        public DateTime JnlLineDate { get; set; }
        public string RegType { get; set; }
        internal string WorkSpaceDefault;
        public Uniconta.DataModel.Employee Employee { get; set; }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (TMJournalLineClient)this.SelectedItem;
            if ((selectedItem != null && (JnlLineDate.AddDays(6) <= Employee._TMApproveDate || JnlLineDate.AddDays(6) <= Employee._TMCloseDate)) || selectedItem == null || selectedItem._Project == null || selectedItem._PayrollCategory == null)
                return false;
            return true;
        }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var header = (Uniconta.DataModel.Employee)this.masterRecord;
            var newRow = (TMJournalLineClient)dataEntity;
            newRow.Date = JnlLineDate;
            newRow.RegistrationType = RegType;
            newRow._WorkSpace = WorkSpaceDefault;
            newRow.SetMaster(header);
        }

        protected override List<string> GridSkipFields { get { return new List<string>(2) { "ProjectName", "PayrollCategoryName" }; } }

        protected override bool SetValuesOnPaste { get { return true; } }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            if (Employee._TMCloseDate >= JnlLineDate)
                return null;
            var copyrow = copyFromRows.FirstOrDefault();
            if (copyrow is Uniconta.DataModel.TMJournalLine)
            {
                foreach (var row in copyFromRows)
                    ((Uniconta.DataModel.TMJournalLine)row)._Date = this.JnlLineDate;
                return copyFromRows;
            }
            return null;
        }
    }

    public partial class TMJournalLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.TMJournalLinePage; } }
        TMJournalLineHelper tmHelper;
        PropValuePair[] tmJournalLineFilter, tmJournalLineTransReg;
        DateTime JournalLineDate;
        string defaultWrkSpace;
        Uniconta.DataModel.Employee employee;
        DateTime employeeCalenderStartDate = GetSystemDefaultDate();

        SQLCache CategoryCache, ItemCache;
        SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> payrollCache;
        SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupCache;
        SQLTableCache<Uniconta.DataModel.Project> projCache;
        SQLTableCache<Uniconta.DataModel.PrWorkSpace> workspaceCache;
        double vacationYTD, vacationNotApproved, otherVacationYTD, otherVacationNotApproved, overTimeYTD, overTimeNotApproved, flexTimeYTD, flexTimeNotApproved, mileageYTD, mileageNotApproved;
        bool clearMileageList, clearHoursList;
        HashSet<string> lstCatMileage, lstCatVacation, lstCatOtherVacation, lstCatFlexTime, lstCatOverTime, lstCatSickness, lstCatOtherAbsence;

        double[] normHoursArr;
        bool RefreshBaseData = true;

        UnicontaAPI.Project.API.PostingAPI postingApi;

        public TMJournalLinePage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public TMJournalLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitPage();
            SetEmployee(master as Uniconta.DataModel.Employee);
        }

        public TMJournalLinePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            UnicontaBaseEntity master = syncEntity.Row;
            Uniconta.DataModel.Employee argsEmpl = null;
            var ApproveReport = master as UnicontaClient.Pages.TimeSheetApprovalLocalClient;
            if (ApproveReport != null)
            {
                employeeCalenderStartDate = ApproveReport.Date;
                argsEmpl = ApproveReport.EmployeeRef;
            }

            InitPage();
            if (argsEmpl != null)
            {
                SetEmployee(argsEmpl);
                SetHeader();
            }
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            var ApproveReport = args as UnicontaClient.Pages.TimeSheetApprovalLocalClient;
            if (ApproveReport != null)
            {
                employeeCalenderStartDate = ApproveReport.Date;
                SetEmployee(ApproveReport.EmployeeRef);

                clearMileageList = true;
                clearHoursList = true;
                LoadGridOnWeekChange(false);

                SetHeader();
            }
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            string employee = null;
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Employee", StringComparison.CurrentCultureIgnoreCase) == 0)
                    employee = rec.Value;
                if (string.Compare(rec.Name, "Date", StringComparison.CurrentCultureIgnoreCase) == 0)
                    employeeCalenderStartDate = StringSplit.DateParse(rec.Value, DateFormat.dmy);
            }
            if (employee != null)
            {
                var cache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Employee), api);
                SetEmployee((Uniconta.DataModel.Employee)cache.Get(employee));
                SetHeader();
            }

            base.SetParameter(Parameters);
        }

        private void SetHeader()
        {
            var header = string.Concat(Uniconta.ClientTools.Localization.lookup("TimeRegistration"), ": ", employee._Name);
            SetHeader(header);
        }

        void InitPage()
        {
            InitializeComponent();
            ((TableView)dgTMJournalLineTransRegGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            ((TableView)dgTMJournalLineGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            postingApi = new UnicontaAPI.Project.API.PostingAPI(api);
            localMenu.dataGrid = dgTMJournalLineGrid;
            SetRibbonControl(localMenu, dgTMJournalLineGrid);
            dgTMJournalLineGrid.api = api;
            dgTMJournalLineTransRegGrid.api = api;
            dgTMJournalLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgTMJournalLineGrid.ShowTotalSummary();
            dgTMJournalLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgTMJournalLineGrid.CustomSummary += DgTMJournalLineGrid_CustomSummary;
            dgTMJournalLineGrid.View.ShowFixedTotalSummary = true;
            applySummaryStyle = true;
            DXSerializer.AddCreateCollectionItemEventHandler(dgTMJournalLineGrid, CreateCollectionItemEventHandler);
            DXSerializer.AddCreateCollectionItemEventHandler(dgTMJournalLineTransRegGrid, CreateCollectionItemEventHandler);
            dgTMJournalLineTransRegGrid.ShowTotalSummary();
            dgTMJournalLineTransRegGrid.CustomSummary += DgTMJournalLineTransRegGrid_CustomSummary;
            dgTMJournalLineGrid.GotFocus += DgTMJournalLineGrid_GotFocus;
            dgTMJournalLineTransRegGrid.GotFocus += DgTMJournalLineTransRegGrid_GotFocus;
            dgTMJournalLineTransRegGrid.View.DataControl.CurrentItemChanged += DgTMJournalLineTransRegGridDataControl_CurrentItemChanged1;
            ribbonControl.lowerSearchGrid = dgTMJournalLineTransRegGrid;
            ribbonControl.UpperSearchNullText = Uniconta.ClientTools.Localization.lookup("Hours");
            ribbonControl.LowerSearchNullText = Uniconta.ClientTools.Localization.lookup("Mileage");
            dgTMJournalLineGrid.tableView.ShowingEditor += TableView_ShowingEditor;
            dgTMJournalLineTransRegGrid.tableView.ShowingEditor += TableView_ShowingEditor1;
            normHoursArr = new double[7];

            CategoryCache = api.GetCache(typeof(Uniconta.DataModel.PrCategory));
            ItemCache = api.GetCache(typeof(Uniconta.DataModel.InvItem));
            payrollCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>();
            workspaceCache = api.GetCache<Uniconta.DataModel.PrWorkSpace>();
        }

        private void TableView_ShowingEditor1(object sender, ShowingEditorEventArgs e)
        {
            var selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem != null)
            {
                if (e.Column.FieldName != "Invoiceable") return;
                e.Cancel = selectedItem.IsEditable == 0 ? false : true;
            }
        }

        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem != null)
            {
                if (e.Column.FieldName != "Invoiceable") return;
                e.Cancel = selectedItem.IsEditable == 0 ? false : true;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;

            if (!Comp.ProjectTask)
            {
                Task.Visible = false;
                Task.ShowInColumnChooser = false;
            }
            else
                Task.ShowInColumnChooser = true;

            if (Comp._Country != 57)
            {
                UtilDisplay.RemoveMenuCommand(rb, "KmRegnskab");
                UtilDisplay.RemoveMenuCommand(rb, "FerieogFlex");
                UtilDisplay.RemoveMenuCommand(rb, "Produktion");
            }
            if (employee?._UserLogidId != api?.session?.LoginId)
                UtilDisplay.RemoveMenuCommand(rb, "EmployeeRegistrationLinePage");
        }

        async void SetEmployee(Uniconta.DataModel.Employee master)
        {
            this.employee = master;
            api.Read(employee);

            SetFields(employeeCalenderStartDate);
            dgTMJournalLineGrid.UpdateMaster(master);
            dgTMJournalLineTransRegGrid.UpdateMaster(master);
            GetDateStatusOnCalender(master);
            if (tmHelper != null)
                await tmHelper.EmployeeChanged(master);
            else
                tmHelper = new TMJournalLineHelper(api, master);

            employee.EmpProjects = null;
            await employee.LoadEmpProjects(api);
            var empProj = employee.EmpProjects != null ? employee.EmpProjects.ToList() : null;
            if (empProj != null && empProj.Count > 0)
                employee.EmpProjects = empProj.Where(s => (s._Blocked == false && (s._Phase == ProjectPhase.Created || s._Phase == ProjectPhase.Accepted || s._Phase == ProjectPhase.InProgress))).OrderBy(x => x.Number);

            SetButtons();
        }

        void GetDateStatusOnCalender(Uniconta.DataModel.Employee employee)
        {
            var emplCalStart = GetSystemDefaultDate().AddYears(-1);
            var approveDte = employee._TMApproveDate == DateTime.MinValue ? GetSystemDefaultDate().AddYears(-1) : employee._TMApproveDate;
            var specialDates = new ObservableCollection<MySpecialDate>();
            for (DateTime date = emplCalStart; date.Date <= approveDte; date = date.AddDays(1))
                specialDates.Add(new MySpecialDate { Date = date, Color = System.Windows.Media.Brushes.Green });
            for (DateTime date = approveDte.AddDays(1); date.Date <= employee._TMCloseDate; date = date.AddDays(1))
                specialDates.Add(new MySpecialDate { Date = date, Color = System.Windows.Media.Brushes.Yellow });
            txtDateTo.MySpecialDates = specialDates;
        }

        private void DgTMJournalLineTransRegGridDataControl_CurrentItemChanged1(object sender, CurrentItemChangedEventArgs e)
        {
            TMJournalLineClient oldselectedItem = e.OldItem as TMJournalLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= DgTMJournalLineTransRegSelectedItem_PropertyChanged;

            TMJournalLineClient selectedItem = e.NewItem as TMJournalLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += DgTMJournalLineTransRegSelectedItem_PropertyChanged;
            }
        }
        private void DgTMJournalLineTransRegSelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (TMJournalLineClient)sender;
            if (e.PropertyName == "Project" ||
                e.PropertyName == "Day1" ||
                e.PropertyName == "Day2" ||
                e.PropertyName == "Day3" ||
                e.PropertyName == "Day4" ||
                e.PropertyName == "Day5" ||
                e.PropertyName == "Day6" ||
                e.PropertyName == "Day7")
            {
                if (rec._InternalType == Uniconta.DataModel.InternalType.Mileage)
                {
                    clearMileageList = true;
                    RecalculateWeekInternalMileage();
                }
                if (e.PropertyName == "Project")
                {
                    rec.Task = null;
                    if (IsProjectBlocked(rec._Project))
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ProjectIsBlocked"), Uniconta.ClientTools.Localization.lookup("Warning"));
                }
            }
            else if (e.PropertyName == "PayrollCategory")
            {
                SetInvoiceable(rec);
                clearMileageList = true;
                RecalculateWeekInternalMileage();
            }
            else if (e.PropertyName == "Task")
            {
                if (!rec.InsidePropChange)
                {
                    rec.InsidePropChange = true;

                    if (rec._Task != null && rec._Project != null)
                    {
                        var pro = (Uniconta.DataModel.Project)projCache.Get(rec._Project);
                        var task = pro.FindTask(rec._Task);
                        if (task != null)
                        {
                            rec.WorkSpace = task._WorkSpace;
                            rec.PayrollCategory = task._PayrollCategory != null ? task._PayrollCategory : rec.PayrollCategory;
                        }
                    }
                    rec.InsidePropChange = false;
                }
            }
            else if (e.PropertyName == "WorkSpace")
            {
                if (!rec.InsidePropChange)
                {
                    rec.InsidePropChange = true;
                    if (rec._Task != null && rec._Project != null)
                    {
                        var pro = (Uniconta.DataModel.Project)projCache.Get(rec._Project);
                        var task = pro.FindTask(rec._Task);
                        if (task != null && task._WorkSpace != rec._WorkSpace)
                            rec.Task = null;
                    }
                    rec.InsidePropChange = false;
                }
            }
        }

        bool mileageGridFocus = false;
        private void DgTMJournalLineTransRegGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            mileageGridFocus = true;
        }

        private void DgTMJournalLineGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            mileageGridFocus = false;
        }

        bool applySummaryStyle = false;
        private void CreateCollectionItemEventHandler(object sender, XtraCreateCollectionItemEventArgs e)
        {
            if (e.CollectionName == "TotalSummary")
            {
                var col = e.Collection as DevExpress.Xpf.Grid.GridSummaryItemCollection;
                var summary = e.CollectionItem as DevExpress.Xpf.Grid.GridSummaryItem;
                col.Remove(summary);
                var newItem = new SumColumn();
                col.Add(newItem);
                e.CollectionItem = newItem;
                applySummaryStyle = true;
            }
        }

        bool KeyAllowed(string shortCut)
        {
            TMJournalLineClient selectedItem;
            if (mileageGridFocus)
                selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
            else
                selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClient;

            if (selectedItem == null)
                return false;

            var dayStatLst = new List<DayStatus>(7);
            dayStatLst.Add(new DayStatus { Status = selectedItem.StatusDay1, Amount = selectedItem.Day1 });
            dayStatLst.Add(new DayStatus { Status = selectedItem.StatusDay2, Amount = selectedItem.Day2 });
            dayStatLst.Add(new DayStatus { Status = selectedItem.StatusDay3, Amount = selectedItem.Day3 });
            dayStatLst.Add(new DayStatus { Status = selectedItem.StatusDay4, Amount = selectedItem.Day4 });
            dayStatLst.Add(new DayStatus { Status = selectedItem.StatusDay5, Amount = selectedItem.Day5 });
            dayStatLst.Add(new DayStatus { Status = selectedItem.StatusDay6, Amount = selectedItem.Day6 });
            dayStatLst.Add(new DayStatus { Status = selectedItem.StatusDay7, Amount = selectedItem.Day7 });

            var statusCloselst = dayStatLst.Where(x => x.Status == 1 && x.Amount != 0).ToList();
            var statusApprovelst = dayStatLst.Where(x => x.Status == 2 && x.Amount != 0).ToList();

            switch (shortCut)
            {
                case "CopyLine":
                case "DeleteLine":
                case "CopyField":
                    return statusCloselst.Count == 0 && statusApprovelst.Count == 0;
                case "NewLine":
                    return 7 - statusCloselst.Count > 0 && 7 - statusApprovelst.Count > 0;
            }

            return true;
        }

        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            bool keyAllowed = true;

            if (e.Key == Key.Delete && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                keyAllowed = KeyAllowed("DeleteLine");
            else if (e.Key == Key.F2 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                keyAllowed = KeyAllowed("CopyLine");
            else if (e.Key == Key.F2 || (e.Key == Key.N && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
                keyAllowed = KeyAllowed("NewLine");
            else if (e.Key == Key.F5 || (e.Key == Key.F4 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)))
                keyAllowed = KeyAllowed("CopyField");
            else
                base.OnPreviewKeyDown(e);

            if (keyAllowed)
                base.OnPreviewKeyDown(e);
            else
                e.Handled = true;
        }

        void SetSummaryLayoutStyle()
        {
            foreach (DevExpress.Xpf.Grid.GridSummaryItem item in dgTMJournalLineGrid.TotalSummary)
            {
                if (item is SumColumn)
                {
                    SumColumn customItem = (SumColumn)item;
                    var tag = customItem.SerializableTag;
                    if (tag == "TotalDay1" ||
                        tag == "TotalDay2" ||
                        tag == "TotalDay3" ||
                        tag == "TotalDay4" ||
                        tag == "TotalDay5" ||
                        tag == "TotalDay6" ||
                        tag == "TotalDay7" ||
                        tag == "TotalSum")
                    {
                        customItem.TotalSummaryElementStyle = this.FindResource("SummaryTotalStyle") as Style;
                    }
                }
            }

            applySummaryStyle = false;
        }

        private void DataControl_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            dgTMJournalLineGrid.UpdateTotalSummary();
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            TMJournalLineClient oldselectedItem = e.OldItem as TMJournalLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            TMJournalLineClient selectedItem = e.NewItem as TMJournalLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
        }

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (TMJournalLineClient)sender;
            var PropertyName = e.PropertyName;
            if (PropertyName == "Project" ||
                PropertyName == "Day1" ||
                PropertyName == "Day2" ||
                PropertyName == "Day3" ||
                PropertyName == "Day4" ||
                PropertyName == "Day5" ||
                PropertyName == "Day6" ||
                PropertyName == "Day7")
            {
                if (rec._InternalType != 0)
                {
                    clearHoursList = true;
                    RecalculateWeekInternalHour();
                }
                RecalculateEfficiencyPercentage();
                if (PropertyName == "Project")
                {
                    rec.Task = null;

                    if (IsProjectBlocked(rec._Project))
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ProjectIsBlocked"), Uniconta.ClientTools.Localization.lookup("Warning"));

                    var proj = (Uniconta.DataModel.Project)projCache.Get(rec._Project);
                    SetProjectTask(proj, rec);
                }
            }
            else if (PropertyName == "PayrollCategory")
            {
                SetInvoiceable(rec);
                rec.NotifyPropertyChanged("IsMatched");
                if (rec._InternalType != 0)
                {
                    clearHoursList = true;
                    RecalculateWeekInternalHour();
                }
            }
            else if (PropertyName == "Task")
            {
                if (!rec.InsidePropChange)
                {
                    rec.InsidePropChange = true;

                    if (rec._Task != null && rec._Project != null)
                    {
                        var pro = (Uniconta.DataModel.Project)projCache.Get(rec._Project);
                        var task = pro.FindTask(rec._Task);
                        if (task != null)
                        {
                            rec.WorkSpace = task._WorkSpace;
                            rec.PayrollCategory = task._PayrollCategory ?? rec.PayrollCategory;
                        }
                    }
                    rec.InsidePropChange = false;
                }
            }
            else if (PropertyName == "WorkSpace")
            {
                if (!rec.InsidePropChange)
                {
                    rec.InsidePropChange = true;
                    if (rec._Task != null && rec._Project != null)
                    {
                        var pro = (Uniconta.DataModel.Project)projCache.Get(rec._Project);
                        var task = pro.FindTask(rec._Task);
                        if (task != null && task._WorkSpace != rec._WorkSpace)
                            rec.Task = null;
                    }
                    rec.InsidePropChange = false;
                }
            }

            dgTMJournalLineGrid.UpdateTotalSummary();
        }

        async void SetProjectTask(Uniconta.DataModel.Project project, TMJournalLineClient rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                {
                    var tasks = project.Tasks ?? await project.LoadTasks(api);
                    rec.ProjectTaskSource = tasks?.Where(s => s.Ended == false && (rec._WorkSpace == null || s._WorkSpace == rec._WorkSpace));
                }
                else
                {
                    rec.ProjectTaskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("ProjectTaskSource");
            }
        }

        async void SetMileageProjectTask(Uniconta.DataModel.Project project, TMJournalLineClient rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                {
                    var tasks = project.Tasks ?? await project.LoadTasks(api);
                    rec.MileageProjectTaskSource = tasks?.Where(s => s.Ended == false && (rec._WorkSpace == null || s._WorkSpace == rec._WorkSpace));
                }
                else
                {
                    rec.MileageProjectTaskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("MileageProjectTaskSource");
            }
        }

        private void ProjectTask_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem?._Project != null)
            {
                var selected = (Uniconta.DataModel.Project)projCache.Get(selectedItem._Project);
                SetProjectTask(selected, selectedItem);
            }
        }

        private void Mileage_ProjectTask_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem?._Project != null)
            {
                var selected = (Uniconta.DataModel.Project)projCache.Get(selectedItem._Project);
                SetMileageProjectTask(selected, selectedItem);
            }
        }

        void SetFields(DateTime selectedDate)
        {
            cmbRegistration.ItemsSource = Uniconta.ClientTools.AppEnums.RegistrationType.Values;
            cmbRegistration.SelectedIndex = 0;
            JournalLineDate = FirstDayOfWeek(selectedDate);
            txtDateTo.DateTime = JournalLineDate;
            tmJournalLineFilter = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements("Date", JournalLineDate, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("RegistrationType", "0", CompareOperator.Equal, typeof(int))
            };
            tmJournalLineTransReg = new PropValuePair[]
            {
                 PropValuePair.GenereteWhereElements("Date", JournalLineDate, CompareOperator.Equal),
                 PropValuePair.GenereteWhereElements("RegistrationType", "1", CompareOperator.Equal, typeof(int))
            };
            SetColumnHeader();
        }

        TMApprovalSetupClient[] approverLst;
        Uniconta.DataModel.Employee[] employeeLst;
        async void SetButtons()
        {
            if (payrollCache == null)
                payrollCache = await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>();

            if (projGroupCache == null)
                projGroupCache = await api.LoadCache<Uniconta.DataModel.ProjectGroup>();

            var ribbonControl = this.ribbonControl;
            if (ribbonControl == null)
                return;

            EnableMileageRegistration();

            if (JournalLineDate.AddDays(6) <= employee._TMCloseDate)
            {
                ribbonControl.DisableButtons("Close");
                ribbonControl.DisableButtons("AddRow");
                ribbonControl.DisableButtons("CopyRow");
                ribbonControl.DisableButtons("DeleteRow");
                ribbonControl.DisableButtons("SaveGrid");

                if (JournalLineDate.AddDays(6) <= employee._TMApproveDate)
                    ribbonControl.DisableButtons("Open");
                else
                    ribbonControl.EnableButtons("Open");
            }
            else
            {
                ribbonControl.EnableButtons("AddRow");
                ribbonControl.EnableButtons("CopyRow");
                ribbonControl.EnableButtons("SaveGrid");
                ribbonControl.EnableButtons("Close");
                ribbonControl.EnableButtons("DeleteRow");

                if (employee._TMCloseDate >= JournalLineDate && employee._TMApproveDate != employee._TMCloseDate)
                    ribbonControl.EnableButtons("Open");
                else
                    ribbonControl.DisableButtons("Open");
            }

            if (approverLst == null)
                approverLst = await api.Query<TMApprovalSetupClient>();
            if (approverLst == null || approverLst.Length == 0)
            {
                ribbonControl.EnableButtons("Approve");
                return;
            }

            Uniconta.DataModel.Employee curUser = null;

            if (employeeLst == null)
            {
                employeeLst = await api.Query<Uniconta.DataModel.Employee>(BasePage.session.User);
                if (employeeLst != null && employeeLst.Length > 0)
                    curUser = employeeLst[0];
                else
                    ribbonControl.DisableButtons("Approve");
            }
            else if (employeeLst.Length > 0)
                curUser = employeeLst[0];

            if (curUser != null)
            {
                if (approverLst.Where(x => x._Approver == curUser._Number && ((x.Employee == employee._Number || (x._Employee == null && x._EmployeeGroup != null)) && (x._EmployeeGroup == employee._Group || (x._EmployeeGroup == null && x.Employee != null)) ||
                                            (x._EmployeeGroup == null && x.Employee == null)) && (x.ValidFrom <= JournalLineDate && (x.ValidTo == DateTime.MinValue || x.ValidTo >= JournalLineDate))).Any() == false)
                {
                    ribbonControl.DisableButtons("Approve");
                }
                else
                {
                    if (JournalLineDate.AddDays(6) <= employee._TMApproveDate)
                    {
                        ribbonControl.DisableButtons("Approve");
                        ribbonControl.DisableButtons("ValidateJournal");
                    }
                    else
                    {
                        if (employee._TMCloseDate < JournalLineDate)
                            ribbonControl.DisableButtons("Approve");
                        else
                            ribbonControl.EnableButtons("Approve");

                        ribbonControl.EnableButtons("ValidateJournal");
                    }
                }
            }
        }

        ItemBase transRegBtn;
        void EnableMileageRegistration()
        {
            if (payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.Mileage).FirstOrDefault() == null)
            {
                ribbonControl.DisableButtons("ShowMileage");
                ribbonControl.DisableButtons("AddMileage");
            }
            else
            {
                ribbonControl.DisableButtons("AddMileage");

                if (JournalLineDate.AddDays(6) <= employee._TMCloseDate || JournalLineDate.AddDays(6) <= employee._TMApproveDate)
                    ribbonControl.DisableButtons("AddMileage");
                else
                    ribbonControl.EnableButtons("AddMileage");

                ShowHideMileage();
            }
        }

        void ShowHideMileage()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            transRegBtn = UtilDisplay.GetMenuCommandByName(rb, "ShowMileage");
            if (layOutInvItemStorage.Visibility == Visibility.Collapsed)
            {
                mileageGridFocus = false;
                transRegBtn.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("Mileage"));
                ribbonControl.DisableButtons("AddMileage");
            }
            else
            {
                transRegBtn.Caption = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Hide"), Uniconta.ClientTools.Localization.lookup("Mileage"));
                ribbonControl.EnableButtons("AddMileage");
            }
        }

        async void CopyLines()
        {
            if (employee._TMCloseDate >= JournalLineDate)
                return;

            bool HasContent(TMJournalLineClient line) =>
                line.Project != null ||
                line.PayrollCategory != null ||
                line.Day1 != 0 ||
                line.Day2 != 0 ||
                line.Day3 != 0 ||
                line.Day4 != 0 ||
                line.Day5 != 0 ||
                line.Day6 != 0 ||
                line.Day7 != 0;

            var gridHours = dgTMJournalLineGrid.ItemsSource as IEnumerable<TMJournalLineClient>;
            if (gridHours?.Any(HasContent) == true)
                return;

            var pairJournalTrans = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), employee.KeyStr),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), JournalLineDate.AddDays(-7), CompareOperator.Equal),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.RegistrationType), "0", CompareOperator.Equal, typeof(int)),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.CopyLine), "1", CompareOperator.Equal, typeof(bool))
            };
            var copyPrevJournalLine = await api.Query<TMJournalLineClient>(pairJournalTrans);
            if (copyPrevJournalLine == null || copyPrevJournalLine.Length == 0)
                return;

            foreach (var rec in copyPrevJournalLine)
            {
                if (IsProjectBlocked(rec._Project))
                    continue;

                rec._Date = JournalLineDate;
                rec._Day1 = rec._Day2 = rec._Day3 = rec._Day4 = rec._Day5 = rec._Day6 = rec._Day7 = 0;
                rec._AddressFrom = rec._AddressTo = rec._VechicleRegNo = null;
                rec._LineNumber = 0;
                rec._JournalPostedId = 0;
                rec.RowId = 0;
                dgTMJournalLineGrid.AddRow(rec, -1, false);
            }
        }

        bool IsProjectBlocked(string project)
        {
            var proj = (Uniconta.DataModel.Project)projCache.Get(project);
            if (proj != null && (proj._Blocked || (proj._Phase != ProjectPhase.Created && proj._Phase != ProjectPhase.Accepted && proj._Phase != ProjectPhase.InProgress)))
                return true;
            return false;
        }

        void SetValuesForGridProperties()
        {
            dgTMJournalLineGrid.JnlLineDate = JournalLineDate;
            dgTMJournalLineGrid.Employee = employee;
            dgTMJournalLineGrid.RegType = Uniconta.ClientTools.AppEnums.RegistrationType.Values[0];

            dgTMJournalLineTransRegGrid.JnlLineDate = JournalLineDate;
            dgTMJournalLineTransRegGrid.Employee = employee;
            dgTMJournalLineTransRegGrid.RegType = Uniconta.ClientTools.AppEnums.RegistrationType.Values[1];
        }

        DateTime FirstDayOfWeek(DateTime selectedDate)
        {
            var dt = selectedDate;
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-diff).Date;
        }

        void SetInvoiceable(TMJournalLineClient rec)
        {
            var payroll = (Uniconta.DataModel.EmpPayrollCategory)payrollCache?.Get(rec.PayrollCategory);
            if (payroll != null)
            {
                var Cat = (PrCategory)CategoryCache.Get(payroll._PrCategory);
                if (Cat != null)
                    rec.Invoiceable = Cat._Invoiceable;
            }
        }

        ProjectTransClient[] internalTransLst;
        TMJournalLineClient[] journalLineNotApprovedLst;
        DateTime approvedCutOffDate;
        DateTime lastEmpApproveDate = DateTime.MinValue;
        async void InitializeStatusText()
        {
            if (employee == null)
                return;

            if (employee._TMApproveDate != lastEmpApproveDate)
            {
                internalTransLst = null;
                journalLineNotApprovedLst = null;
                lastEmpApproveDate = employee._TMApproveDate;
            }

            RibbonBase rb = (RibbonBase)localMenu.DataContext;

            if ((internalTransLst == null || journalLineNotApprovedLst == null) && payrollCache != null && payrollCache.Count > 0)
            {
                approvedCutOffDate = employee._TMApproveDate == DateTime.MinValue ? DateTime.MinValue :
                                     employee._TMApproveDate.DayOfWeek == DayOfWeek.Sunday ? employee._TMApproveDate.AddDays(1) :
                                     employee._TMApproveDate.DayOfWeek == DayOfWeek.Monday ? employee._TMApproveDate :
                                     FirstDayOfWeek(employee._TMApproveDate);

                HashSet<string> lstCatPay = null;

                foreach (var val in payrollCache)
                {
                    if (val._InternalType == 0 || val._InternalType == Uniconta.DataModel.InternalType.OtherAbsence || val._InternalType == Uniconta.DataModel.InternalType.Sickness)
                        continue;

                    switch (val._InternalType)
                    {
                        case Uniconta.DataModel.InternalType.FlexTime: AddToList(ref lstCatFlexTime, val._Number); break;
                        case Uniconta.DataModel.InternalType.Vacation: AddToList(ref lstCatVacation, val._Number); break;
                        case Uniconta.DataModel.InternalType.OtherVacation: AddToList(ref lstCatOtherVacation, val._Number); break;
                        case Uniconta.DataModel.InternalType.OverTime: AddToList(ref lstCatOverTime, val._Number); break;
                        case Uniconta.DataModel.InternalType.Sickness: AddToList(ref lstCatSickness, val._Number); break;
                        case Uniconta.DataModel.InternalType.OtherAbsence: AddToList(ref lstCatOtherAbsence, val._Number); break;
                        case Uniconta.DataModel.InternalType.Mileage: AddToList(ref lstCatMileage, val._Number); break;
                    }

                    AddToList(ref lstCatPay, val._Number);
                }

                var catPayDist = string.Join(";", lstCatPay ?? Enumerable.Empty<string>());

                if (internalTransLst == null)
                {
                    var pairInternalTrans = new PropValuePair[]
                    {
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Employee), typeof(string), employee.KeyStr),
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.PayrollCategory), typeof(string), catPayDist),
                    };
                    internalTransLst = await api.Query<ProjectTransClient>(pairInternalTrans);
                }

                if (journalLineNotApprovedLst == null)
                {
                    var pairJournalTrans = new List<PropValuePair>
                    {
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), employee.KeyStr),
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.PayrollCategory), typeof(string), catPayDist),
                    };
                    journalLineNotApprovedLst = await api.Query<TMJournalLineClient>(pairJournalTrans);
                }
            }

            (vacationYTD, vacationNotApproved) = CalculateBalances(rb, "VacationBal", lstCatVacation, Uniconta.DataModel.InternalType.Vacation);
            (otherVacationYTD, otherVacationNotApproved) = CalculateBalances(rb, "OtherVacationBal", lstCatOtherVacation, Uniconta.DataModel.InternalType.OtherVacation);
            (overTimeYTD, overTimeNotApproved) = CalculateBalances(rb, "OvertimeBal", lstCatOverTime, Uniconta.DataModel.InternalType.OverTime);
            (flexTimeYTD, flexTimeNotApproved) = CalculateBalances(rb, "FlexTimeBal", lstCatFlexTime, Uniconta.DataModel.InternalType.FlexTime);
            (mileageYTD, mileageNotApproved) = CalculateBalances(rb, "MileageBal", lstCatMileage, Uniconta.DataModel.InternalType.Mileage);

            RecalculateWeekInternalHour();
            RecalculateWeekInternalMileage();
            RecalculateEfficiencyPercentage();
        }


        (double ytd, double notApproved) CalculateBalances(RibbonBase rb, string menuCommand, HashSet<string> catList, Uniconta.DataModel.InternalType internalType)
        {
            if (catList == null)
            {
                UtilDisplay.RemoveMenuCommand(rb, menuCommand);
                return (0, 0);
            }

            if (internalTransLst == null && journalLineNotApprovedLst == null)
                return (0, 0);

            double ytd = 0, notApproved = 0;

            DateTime startDate = DateTime.MinValue;
            var registrationType = RegistrationType.Hours;
            var isMileage = internalType == Uniconta.DataModel.InternalType.Mileage;
            var isOverTimeFlex = internalType == Uniconta.DataModel.InternalType.OverTime || internalType == Uniconta.DataModel.InternalType.FlexTime;

            if (isMileage)
            {
                startDate = new DateTime(JournalLineDate.Year, 1, 1);
                registrationType = RegistrationType.Mileage;
            }
            else if (isOverTimeFlex)
                startDate = DateTime.MinValue;
            else
                startDate = JournalLineDate < new DateTime(2023, 1, 1) ? new DateTime(JournalLineDate.Year, 1, 1) : new DateTime(2023, 1, 1);

            if (internalTransLst != null)
            {
                var endDate = JournalLineDate.AddDays(6);
                ytd = internalTransLst
                    .Where(s => catList.Contains(s._PayrollCategory) && s.Date >= startDate && s.Date <= endDate)
                    .Sum(x => isMileage ? x.Qty : -x.Qty);
            }

            if (journalLineNotApprovedLst != null)
            {
                var cutoff = JournalLineDate.AddDays(-1);
                var approved = FirstDayOfWeek(employee._TMApproveDate);
                foreach (var line in journalLineNotApprovedLst.Where(s => s._RegistrationType == registrationType &&
                                                                           s._InternalType == internalType &&
                                                                           (isMileage || !s._Invoiceable) &&
                                                                           s.Date >= approved && s.Date <= cutoff))
                {
                    var calcQty = CalcNotApprovedHoursMileage(line);
                    if (calcQty == 0)
                        continue;

                    if (isOverTimeFlex)
                    {
                        var factor = payrollCache.Get(line?.PayrollCategory)?._Factor ?? 0;
                        if (factor != 0)
                            calcQty = factor * calcQty;
                    }
                    notApproved += calcQty;
                }
            }

            return (ytd, notApproved);
        }


        void RecalculateWeekInternalHour()
        {
            double vacationTotal = 0;
            double otherVacationTotal = 0;
            double overTimeTotal = 0;
            double flexTimeTotal = 0;

            if (approvedCutOffDate < JournalLineDate.AddDays(6))
            {
                var gridHours = (IEnumerable<TMJournalLineClient>)dgTMJournalLineGrid.ItemsSource;
                if (gridHours != null)
                {
                    foreach (var x in gridHours)
                    {
                        if (x._InternalType == Uniconta.DataModel.InternalType.Vacation)
                            vacationTotal += CalcNotApprovedHoursMileage(x); 
                        else if (x._InternalType == Uniconta.DataModel.InternalType.OtherVacation)
                            otherVacationTotal += CalcNotApprovedHoursMileage(x);
                        else if (x._InternalType == Uniconta.DataModel.InternalType.OverTime || x._InternalType == Uniconta.DataModel.InternalType.FlexTime)
                        {
                            var calc = CalcNotApprovedHoursMileage(x);
                            if (calc == 0)
                                continue;

                            var factor = payrollCache.Get(x?.PayrollCategory)?._Factor ?? 0; 
                            if (factor != 0)
                                calc = factor * calc;

                            if (x._InternalType == Uniconta.DataModel.InternalType.OverTime)
                                overTimeTotal += calc;
                            else
                                flexTimeTotal += calc;
                        }
                    }
                }
            }

            SetStatusTextHour(vacationTotal, otherVacationTotal, overTimeTotal, flexTimeTotal);
        }

        private double CalcNotApprovedHoursMileage(TMJournalLineClient line)
        {
            double total = 0;
            for (int i = 0; i < 7; i++)
            {
                var dt = line.Date.AddDays(i);
                if (dt <= employee._TMApproveDate)
                    continue;

                switch (dt.DayOfWeek)
                {
                    case DayOfWeek.Monday: total += line._Day1; break;
                    case DayOfWeek.Tuesday: total += line._Day2; break;
                    case DayOfWeek.Wednesday: total += line._Day3; break;
                    case DayOfWeek.Thursday: total += line._Day4; break;
                    case DayOfWeek.Friday: total += line._Day5; break;
                    case DayOfWeek.Saturday: total += line._Day6; break;
                    case DayOfWeek.Sunday: total += line._Day7; break;
                }
            }
            return total;
        }

        void RecalculateEfficiencyPercentage()
        {
            double invoiceableHours = 0;
            double notInvoiceableHours = 0;
            double efficiencyPercentage = 0;

            var gridHours = (IEnumerable<TMJournalLineClient>)dgTMJournalLineGrid.ItemsSource;
            if (gridHours != null)
            {
                var grpLstInvoiceable = gridHours.Where(s => s._InternalType == 0).GroupBy(x => x._Invoiceable).Select(x => new { GroupKey = x.Key, Sum = x.Sum(y => y.Total) });
                foreach (var i in grpLstInvoiceable)
                {
                    if (i.GroupKey)
                        invoiceableHours = i.Sum;
                    else
                        notInvoiceableHours = i.Sum;
                }

                efficiencyPercentage = invoiceableHours + notInvoiceableHours != 0 ? invoiceableHours / (invoiceableHours + notInvoiceableHours) * 100 : 0;
            }

            SetStatusTextEfficiencyPercentage(efficiencyPercentage);
            SetStatusTextInvoiceableHours(invoiceableHours);
        }

        void RecalculateWeekInternalMileage()
        {
            double mileageTotal = 0;

            if (employee._TMApproveDate < JournalLineDate.AddDays(6))
            {
                var gridMileage = (IEnumerable<TMJournalLineClient>)dgTMJournalLineTransRegGrid.ItemsSource;
                if (gridMileage != null)
                {
                    foreach (var x in gridMileage)
                    {
                        if (x._InternalType == Uniconta.DataModel.InternalType.Mileage)
                           mileageTotal += CalcNotApprovedHoursMileage(x);//x.Total;
                    }
                }
            }

            SetStatusTextMileage(mileageTotal);
        }

        void SetStatusTextHour(double vacation = 0, double otherVacation = 0, double overTime = 0, double flexTime = 0)
        {
            string format = "N2";
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var lblVacationBal = Uniconta.ClientTools.Localization.lookup("Vacation");
            var lblOtherVacationBal = Uniconta.ClientTools.Localization.lookup("OtherVacation");
            var lblOverTimeBal = Uniconta.ClientTools.Localization.lookup("Overtime");
            var lblFlexTimeBal = Uniconta.ClientTools.Localization.lookup("Flextime");
            var lblEfficiencyPercentage = Uniconta.ClientTools.Localization.lookup("EfficiencyPercentage");

            foreach (var grp in groups)
            {
                if (grp.Caption == lblVacationBal)
                {
                    vacation = vacationYTD - vacation - vacationNotApproved;
                    grp.StatusValue = vacation.ToString(format);
                }
                else if (grp.Caption == lblOtherVacationBal)
                {
                    otherVacation = otherVacationYTD - otherVacation - otherVacationNotApproved;
                    grp.StatusValue = otherVacation.ToString(format);
                }
                else if (grp.Caption == lblOverTimeBal)
                {
                    overTime = overTimeYTD - overTime - overTimeNotApproved;
                    grp.StatusValue = overTime.ToString(format);
                }
                else if (grp.Caption == lblFlexTimeBal)
                {
                    flexTime = flexTimeYTD - flexTime - flexTimeNotApproved;
                    grp.StatusValue = flexTime.ToString(format);
                }
            }
        }

        double mileageHighTotal;
        void SetStatusTextMileage(double mileage = 0)
        {
            string format = "N2";
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var mileageBal = Uniconta.ClientTools.Localization.lookup("Mileage");

            foreach (var grp in groups)
            {
                if (grp.Caption == mileageBal)
                {
                    mileageHighTotal = mileage + mileageYTD + mileageNotApproved;
                    grp.StatusValue = mileageHighTotal.ToString(format);
                }
            }
        }

        void SetStatusTextEfficiencyPercentage(double efficiencyPercentage = 0)
        {
            const string format = "N2";
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var lblEfficiencyPercentage = Uniconta.ClientTools.Localization.lookup("EfficiencyPercentage");

            foreach (var grp in groups)
            {
                if (grp.Caption == lblEfficiencyPercentage)
                    grp.StatusValue = efficiencyPercentage.ToString(format);
            }
        }

        void SetStatusTextInvoiceableHours(double invoiceable = 0)
        {
            const string format = "N2";
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var lblInvoiceable = Uniconta.ClientTools.Localization.lookup("InvoiceableHours");

            foreach (var grp in groups)
            {
                if (grp.Caption == lblInvoiceable)
                    grp.StatusValue = invoiceable.ToString(format);
            }
        }

        void EmployeeNormalHours()
        {
            if (RefreshBaseData)
            { 
                FindNormHours.RefreshBaseData();
                RefreshBaseData = false;
            }
            var normDict = System.Threading.Tasks.Task.Run(() => FindNormHours.GetByDay(api, employee._Number, JournalLineDate, JournalLineDate.AddDays(6))).GetAwaiter().GetResult();
           
            SetArrayNorm(normDict, ref normHoursArr);
        }

        void SetArrayNorm(Dictionary<DateTime, double> dict, ref double[] arr)
        {
            arr = new double[7];
            foreach (var kvp in dict)
            {
                switch (kvp.Key.DayOfWeek)
                {
                    case DayOfWeek.Monday: arr[0] = kvp.Value; break;
                    case DayOfWeek.Tuesday: arr[1] = kvp.Value; break;
                    case DayOfWeek.Wednesday: arr[2] = kvp.Value; break;
                    case DayOfWeek.Thursday: arr[3] = kvp.Value; break;
                    case DayOfWeek.Friday: arr[4] = kvp.Value; break;
                    case DayOfWeek.Saturday: arr[5] = kvp.Value; break;
                    case DayOfWeek.Sunday: arr[6] = kvp.Value; break;
                }
            }
        }

        private async void txtDateTo_PopupClosed(object sender, DevExpress.Xpf.Editors.ClosePopupEventArgs e)
        {
            var dteEdit = sender as DateEdit;
            if (dteEdit == null) return;
            var selectedDate = (DateTime)dteEdit.EditValue;

            if (selectedDate >= JournalLineDate && selectedDate <= JournalLineDate.AddDays(6))
            {
                txtDateTo.EditValue = JournalLineDate;
                return;
            }

            SetFields(selectedDate);
            txtDateTo.EditValue = JournalLineDate;
            await saveGrid();
            await BindGrid();
            CopyLines();
            SetButtons();
        }

        double day1Sum, day2Sum, day3Sum, day4Sum, day5Sum, day6Sum, day7Sum, totalSum;
        void GetGridColumnsSum()
        {
            var lst = dgTMJournalLineGrid.ItemsSource as IEnumerable<TMJournalLineClient>;
            if (lst != null)
            {
                bool first = true;
                day1Sum = day2Sum = day3Sum = day4Sum = day5Sum = day6Sum = day7Sum = 0;
                foreach (var x in lst)
                {
                    day1Sum += x._Day1;
                    day2Sum += x._Day2;
                    day3Sum += x._Day3;
                    day4Sum += x._Day4;
                    day5Sum += x._Day5;
                    day6Sum += x._Day6;
                    day7Sum += x._Day7;
                    if (first)
                    {
                        cmbRegistration.SelectedItem = x.RegistrationType;
                        first = false;
                    }
                }
                totalSum = Math.Round(day1Sum + day2Sum + day3Sum + day4Sum + day5Sum + day6Sum + day7Sum, 2);
                if (first)
                    cmbRegistration.SelectedIndex = 0;
            }
        }

        CorasauGridLookupEditorClient prevProject, prevMilageProject;
        private void Hours_Project_GotFocus(object sender, RoutedEventArgs e)
        {
            TMJournalLineClient selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem != null)
            {
                SetProjectSource(selectedItem);
                if (prevProject != null)
                    prevProject.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevProject = editor;
                editor.isValidate = true;
            }
        }

        private void Milage_Project_GotFocus(object sender, RoutedEventArgs e)
        {
            TMJournalLineClient selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem != null)
            {
                SetProjectSource(selectedItem);
                if (prevMilageProject != null)
                    prevMilageProject.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevMilageProject = editor;
                editor.isValidate = true;
            }
        }

        private void Hours_Project_LostFocus(object sender, RoutedEventArgs e)
        {
            SetProjectByLookupText(sender, true);
        }

        private void Milage_Project_LostFocus(object sender, RoutedEventArgs e)
        {
            SetProjectByLookupText(sender, false);
        }

        void SetProjectByLookupText(object sender, bool isHours)
        {
            TMJournalLineClient selectedItem = isHours ? dgTMJournalLineGrid.SelectedItem as TMJournalLineClient : dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem == null)
                return;
            var le = sender as CorasauGridLookupEditor;
            if (string.IsNullOrEmpty(le.EnteredText))
                return;

            if (projCache != null)
            {
                var prjt = projCache.FirstOrDefault(s => s._Number == le.EnteredText);
                if (prjt != null)
                {
                    if (isHours)
                        dgTMJournalLineGrid.SetLoadedRow(selectedItem);
                    else
                        dgTMJournalLineTransRegGrid.SetLoadedRow(selectedItem);
                    selectedItem.Project = prjt.KeyStr;
                    le.EditValue = prjt.KeyStr;
                    if (isHours)
                        dgTMJournalLineGrid.SetModifiedRow(selectedItem);
                    else
                        dgTMJournalLineTransRegGrid.SetModifiedRow(selectedItem);
                }
            }
            le.EnteredText = null;
        }

        CorasauGridLookupEditorClient prevPayroll, prevMilagePayroll;
        private void Hours_Payroll_GotFocus(object sender, RoutedEventArgs e)
        {
            TMJournalLineClient selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem != null)
            {
                SetPayrollSource(selectedItem);
                if (prevPayroll != null)
                    prevPayroll.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevPayroll = editor;
                editor.isValidate = true;
            }
        }

        private void Milage_Payroll_GotFocus(object sender, RoutedEventArgs e)
        {
            TMJournalLineClient selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem != null)
            {
                SetPayrollSource(selectedItem, isHoursPayroll: false);
                if (prevMilagePayroll != null)
                    prevMilagePayroll.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevMilagePayroll = editor;
                editor.isValidate = true;
            }
        }

        private void Hours_Payroll_LostFocus(object sender, RoutedEventArgs e)
        {
            SetPayrollByLookupText(sender, true);
        }

        private void Milage_Payroll_LostFocus(object sender, RoutedEventArgs e)
        {
            SetPayrollByLookupText(sender, false);
        }
        void SetPayrollByLookupText(object sender, bool isHours)
        {
            TMJournalLineClient selectedItem = isHours ? dgTMJournalLineGrid.SelectedItem as TMJournalLineClient : dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
            if (selectedItem == null)
                return;
            var le = sender as CorasauGridLookupEditor;
            if (string.IsNullOrEmpty(le.EnteredText))
                return;

            if (payrollCache != null)
            {
                var payroll = payrollCache.FirstOrDefault(s => s._Number == le.EnteredText);
                if (payroll != null)
                {
                    if (isHours)
                        dgTMJournalLineGrid.SetLoadedRow(selectedItem);
                    else
                        dgTMJournalLineTransRegGrid.SetLoadedRow(selectedItem);
                    selectedItem.PayrollCategory = payroll.KeyStr;
                    le.EditValue = payroll.KeyStr;
                    if (isHours)
                        dgTMJournalLineGrid.SetModifiedRow(selectedItem);
                    else
                        dgTMJournalLineTransRegGrid.SetModifiedRow(selectedItem);
                }
            }
            le.EnteredText = null;
        }

        private void DgTMJournalLineTransRegGrid_CustomSummary(object sender, CustomSummaryEventArgs e)
        {
            if (e.SummaryProcess == CustomSummaryProcess.Start)
            {
                if (e.Item is SumColumn sumColumn)
                {
                    var tagName = sumColumn.SerializableTag;
                    if (tagName == "Sum")
                        e.TotalValue = Uniconta.ClientTools.Localization.lookup("Mileage");
                }
            }
        }

        private void DgTMJournalLineGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            if (applySummaryStyle)
                SetSummaryLayoutStyle();

            GetGridColumnsSum();
            if (e.Item is SumColumn sumColumn)
            {
                var fieldName = sumColumn.FieldName;
                var tagName = sumColumn.SerializableTag;
                if (e.SummaryProcess == CustomSummaryProcess.Start)
                {
                    switch (fieldName)
                    {
                        case "Project":
                            if (tagName == "Sum")
                                e.TotalValue = Uniconta.ClientTools.Localization.lookup("RegisteredHours");
                            else if (tagName == "NormHours")
                                e.TotalValue = Uniconta.ClientTools.Localization.lookup("NormHours");
                            else if (tagName == "Total")
                                e.TotalValue = Uniconta.ClientTools.Localization.lookup("Dif");
                            break;
                        case "Day1":
                            if (tagName == "HoursDay1")
                                e.TotalValue = normHoursArr[0];
                            else if (tagName == "TotalDay1")
                                e.TotalValue = day1Sum - normHoursArr[0];
                            break;
                        case "Day2":
                            if (tagName == "HoursDay2")
                                e.TotalValue = normHoursArr[1];
                            else if (tagName == "TotalDay2")
                                e.TotalValue = day2Sum - normHoursArr[1];
                            break;
                        case "Day3":
                            if (tagName == "HoursDay3")
                                e.TotalValue = normHoursArr[2];
                            else if (tagName == "TotalDay3")
                                e.TotalValue = day3Sum - normHoursArr[2];
                            break;
                        case "Day4":
                            if (tagName == "HoursDay4")
                                e.TotalValue = normHoursArr[3];
                            else if (tagName == "TotalDay4")
                                e.TotalValue = day4Sum - normHoursArr[3];
                            break;
                        case "Day5":
                            if (tagName == "HoursDay5")
                                e.TotalValue = normHoursArr[4];
                            else if (tagName == "TotalDay5")
                                e.TotalValue = day5Sum - normHoursArr[4];
                            break;
                        case "Day6":
                            if (tagName == "HoursDay6")
                                e.TotalValue = normHoursArr[5];
                            else if (tagName == "TotalDay6")
                                e.TotalValue = day6Sum - normHoursArr[5];
                            break;
                        case "Day7":
                            if (tagName == "HoursDay7")
                                e.TotalValue = normHoursArr[6];
                            else if (tagName == "TotalDay7")
                                e.TotalValue = day7Sum - normHoursArr[6];
                            break;
                        case "Total":
                            if (tagName == "EmpNormHoursSum")
                                e.TotalValue = normHoursArr.Sum();
                            else if (tagName == "TotalSum")
                                e.TotalValue = totalSum - normHoursArr.Sum();
                            break;
                    }
                }
            }
        }
        static string getHeader(DateTime dt)
        {
            return dt.ToString("ddd", Thread.CurrentThread.CurrentCulture) + " " + dt.ToString("dd.MM");
        }

        void SetColumnHeader()
        {
            var dt = JournalLineDate;
            clDay1.Header = getHeader(dt);
            colDay1.Header = getHeader(dt);
            dt = dt.AddDays(1);
            clDay2.Header = getHeader(dt);
            colDay2.Header = getHeader(dt);
            dt = dt.AddDays(1);
            clDay3.Header = getHeader(dt);
            colDay3.Header = getHeader(dt);
            dt = dt.AddDays(1);
            clDay4.Header = getHeader(dt);
            colDay4.Header = getHeader(dt);
            dt = dt.AddDays(1);
            clDay5.Header = getHeader(dt);
            colDay5.Header = getHeader(dt);
            dt = dt.AddDays(1);
            clDay6.Header = getHeader(dt);
            colDay6.Header = getHeader(dt);
            dt = dt.AddDays(1);
            clDay7.Header = getHeader(dt);
            colDay7.Header = getHeader(dt);
            colText.Header = Uniconta.ClientTools.Localization.lookup("Purpose");
            EmployeeNormalHours();
            SetValuesForGridProperties();
        }

        public async override Task InitQuery()
        {
            await BindGrid();
            AddEmptyTMJournalLineRow();
        }

        void AddEmptyTMJournalLineRow()
        {
            if (employee._TMCloseDate >= JournalLineDate)
                return;
            var itemSource = (IList)dgTMJournalLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgTMJournalLineGrid.AddFirstRow();
        }

        async Task BindGrid()
        {
            SortingProperties rowId = new SortingProperties("RowId") { Ascending = true };
            var sortlst = new SortingProperties[] { rowId };
            var sorter = new FilterSorter(sortlst);
            await dgTMJournalLineGrid.Filter(tmJournalLineFilter);
            await dgTMJournalLineTransRegGrid.Filter(tmJournalLineTransReg, PropSort: sorter);
            InitializeStatusText();
        }

        public override void AssignMultipleGrid(List<CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgTMJournalLineGrid);
            gridCtrls.Add(dgTMJournalLineTransRegGrid);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClient;
            var selectedItems = dgTMJournalLineGrid.SelectedItems;
            switch (ActionType)
            {
                case "AddRow":
                    if (JournalLineDate.AddDays(6) <= employee._TMApproveDate || JournalLineDate.AddDays(6) <= employee._TMCloseDate)
                        return;
                    var journalLine = new TMJournalLineClient();
                    journalLine.SetMaster(api.CompanyEntity);
                    journalLine._Date = JournalLineDate;
                    journalLine._WorkSpace = defaultWrkSpace;
                    if (!mileageGridFocus)
                    {
                        journalLine._RegistrationType = Uniconta.DataModel.RegistrationType.Hours;
                        journalLine.NotifyPropertyChanged("RegistrationType");
                        dgTMJournalLineGrid.AddRow(journalLine);
                    }
                    break;
                case "CopyRow":
                    if (selectedItem != null && KeyAllowed("CopyLine") && !mileageGridFocus)
                    {
                        dgTMJournalLineGrid.CopyRow();
                        RecalculateWeekInternalHour();
                        RecalculateEfficiencyPercentage();
                    }
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (selectedItem != null && KeyAllowed("DeleteLine") && !mileageGridFocus)
                    {
                        dgTMJournalLineGrid.DeleteRow();
                        RecalculateWeekInternalHour();
                        RecalculateEfficiencyPercentage();
                    }
                    else
                    {
                        var selectedMileage = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClient;
                        if (selectedMileage != null && KeyAllowed("DeleteLine") && mileageGridFocus)
                            dgTMJournalLineTransRegGrid.DeleteRow();
                    }
                    break;
                case "Today":
                    saveGrid();
                    txtDateTo.DateTime = JournalLineDate = FirstDayOfWeek(GetSystemDefaultDate());
                    LoadGridOnWeekChange();
                    break;
                case "Forward":
                    saveGrid();
                    if (JournalLineDate != DateTime.MinValue)
                        txtDateTo.DateTime = JournalLineDate = JournalLineDate.AddDays(7);
                    LoadGridOnWeekChange();
                    SetButtons();
                    break;
                case "BackWard":
                    saveGrid();
                    if (JournalLineDate != DateTime.MinValue)
                        txtDateTo.DateTime = JournalLineDate = JournalLineDate.AddDays(-7);
                    LoadGridOnWeekChange();
                    SetButtons();
                    break;
                case "Close":
                    if (dgTMJournalLineGrid.ItemsSource == null) return;
                    ActionClose();
                    break;
                case "Open":
                    if (dgTMJournalLineGrid.ItemsSource == null) return;
                    ActionOpen();
                    break;
                case "Approve":
                    if (dgTMJournalLineGrid.ItemsSource == null) return;
                    ActionApprove();
                    break;
                case "RefreshGrid":
                    if (dgTMJournalLineGrid.HasUnsavedData)
                        UnicontaClient.Utilities.Utility.ShowConfirmationOnRefreshGrid(dgTMJournalLineGrid);
                    if (dgTMJournalLineTransRegGrid.HasUnsavedData)
                        UnicontaClient.Utilities.Utility.ShowConfirmationOnRefreshGrid(dgTMJournalLineTransRegGrid);
                    internalTransLst = null;
                    journalLineNotApprovedLst = null;
                    LoadGridOnWeekChange();
                    break;
                case "ValidateJournal":
                    if (dgTMJournalLineGrid.ItemsSource == null) return;
                    ValidateJournal();
                    break;
                case "AddMileage":
                    if (JournalLineDate.AddDays(6) <= employee._TMApproveDate || JournalLineDate.AddDays(6) <= employee._TMCloseDate)
                        return;
                    if (selectedItem != null && layOutInvItemStorage.Visibility == Visibility.Visible)
                        TimeRegistration(selectedItem);
                    break;
                case "ShowMileage":
                    if (layOutInvItemStorage.Visibility == Visibility.Collapsed)
                        layOutInvItemStorage.Visibility = Visibility.Visible;
                    else
                        layOutInvItemStorage.Visibility = Visibility.Collapsed;
                    ShowHideMileage();
                    break;
                case "Edit":
                    gridRibbon_BaseActions(ActionType);
                    if (layOutInvItemStorage.Visibility == Visibility.Collapsed)
                        dgTMJournalLineTransRegGrid.tableView.HideColumnChooser();
                    break;
                case "RemainingBudget":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0} : {1} ({2}: {3} {4}: {5})", Uniconta.ClientTools.Localization.lookup("RemainingBudget"), selectedItem?._Project, Uniconta.ClientTools.Localization.lookup("WorkSpace"),
                                                                             selectedItem?._WorkSpace, Uniconta.ClientTools.Localization.lookup("Task"), selectedItem?._Task);
                        AddDockItem(TabControls.RemainingBudgetLine, dgTMJournalLineGrid.syncEntity, true, header, null, new System.Windows.Point() { X = 25, Y = 350 });
                    }
                    break;
                case "CrmFollowUp":
                    if (this.employee != null)
                    {
                        var empParam = new List<BasePage.ValuePair> { new BasePage.ValuePair("Employee", employee.KeyStr) };
                        AddDockItem(TabControls.CrmFollowUpPage, null, Uniconta.ClientTools.Localization.lookup("FollowUp"), null, true, null, empParam);
                    }
                    break;
                case "Task":
                    if (this.employee != null)
                    {
                        var empParams = new List<BasePage.ValuePair> { new BasePage.ValuePair("Employee", employee.KeyStr) };
                        AddDockItem(TabControls.ProjectTaskGridPage, null, Uniconta.ClientTools.Localization.lookup("Tasks"), null, true, null, empParams);
                    }
                    break;
                case "Planning":
                    if (this.employee != null)
                        AddDockItem(TabControls.ProjectTransBudgetPivotPage, employee, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Planning"), employee._Name), null, true, null);
                    break;
                case "KmRegnskab":
                    var Parameters = new List<BasePage.ValuePair> { new BasePage.ValuePair("Dashboard", "UCDK-std-Km-Regnskab") };
                    AddDockItem(TabControls.DashBoardViewerPage, null, string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", "Km Regnskab"), null, true, null, Parameters);
                    break;
                case "FerieogFlex":
                    var param = new List<BasePage.ValuePair> { new BasePage.ValuePair("Dashboard", "UCDK-Std-Ferie-og-Flex") };
                    AddDockItem(TabControls.DashBoardViewerPage, null, string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", "Ferie og Flex"), null, true, null, param);
                    break;
                case "Produktion":
                    var prodParam = new List<BasePage.ValuePair> { new BasePage.ValuePair("Dashboard", "UCDK-Std-Medarbejder-TimeProduktion") };
                    AddDockItem(TabControls.DashBoardViewerPage, null, string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", "Produktion"), null, true, null, prodParam);
                    break;
                case "BudgetPanningSchedule":
                    var ctrl = dockCtrl.AddDockItem(TabControls.ProjectBudgetPlanningSchedulePage, null, this.employee, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BudgetPlanningSchedule"), this.employee._Name)) as ProjectBudgetPlanningSchedulePage;
                    ctrl.startupStartDate = txtDateTo.DateTime;
                    ctrl.startupView = "WeekView";
                    break;
                case "EmployeeRegistrationLinePage":
                    if (employee?._UserLogidId == api.session.LoginId)
                        AddDockItem(TabControls.EmployeeRegistrationLinePage, employee, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Register"), employee._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void RefreshGrid(bool chkUnSaved = true)
        {
            if (chkUnSaved && dgTMJournalLineGrid.HasUnsavedData)
                UnicontaClient.Utilities.Utility.ShowConfirmationOnRefreshGrid(dgTMJournalLineGrid);
            if (chkUnSaved && dgTMJournalLineTransRegGrid.HasUnsavedData)
                UnicontaClient.Utilities.Utility.ShowConfirmationOnRefreshGrid(dgTMJournalLineTransRegGrid);
            internalTransLst = null;
            journalLineNotApprovedLst = null;
            LoadGridOnWeekChange();
        }

        void TimeRegistration(TMJournalLineClient selectedItem)
        {
            var cw = new CwTransportRegistration(selectedItem, api);
            cw.Closed += delegate
            {
                if (cw.DialogResult == true)
                {
                    var journalLine = new TMJournalLineClient
                    {
                        _Project = cw.RegistrationProject,
                        _Date = JournalLineDate,
                        _RegistrationType = Uniconta.DataModel.RegistrationType.Mileage,
                        _WorkSpace = cw.WorkSpace,
                        _Task = cw.PrTask,
                        _Text = cw.Purpose,
                        _VechicleRegNo = cw.VechicleRegNo,
                        _PayrollCategory = cw.PayType,
                        _Day1 = cw.Day1,
                        _Day2 = cw.Day2,
                        _Day3 = cw.Day3,
                        _Day4 = cw.Day4,
                        _Day5 = cw.Day5,
                        _Day6 = cw.Day6,
                        _Day7 = cw.Day7,
                        _Mileage = cw.Mileage
                    };
                    journalLine.SetMaster(api.CompanyEntity);
                    SetInvoiceable(journalLine);
                    dgTMJournalLineTransRegGrid.AddRow(journalLine);
                    if (cw.Returning)
                    {
                        journalLine._Mileage = cw.MileageReturn;
                        dgTMJournalLineTransRegGrid.AddRow(journalLine);
                    }
                    dgTMJournalLineTransRegGrid.SaveData();
                    RecalculateWeekInternalMileage();
                }
            };
            cw.Show();
        }

        internal class DayStatus
        {
            public int Status;
            public double Amount;
        }

        private async Task ValidateJournal()
        {
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
            busyIndicator.IsBusy = true;

            var err = await saveGrid();
            if (err != 0)
            {
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(err);
                return;
            }

            var postingRes = await postingApi.ValidateTimeJournal(employee, JournalLineDate);
            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

            ShowJournalInfo(postingRes);

            if (postingRes.Err == 0)
                RefreshGrid();
        }

        private void ShowJournalInfo(TMPostingResult result, bool showMsgBox = true)
        {
            var sb = StringBuilderReuse.Create();

            if (result == null)
                return;

            if (result.IsPrevalidation || (result.Err != 0 && result.Lines == null))
            {
                var line = result.Lines?.FirstOrDefault();
                var msgKey = line?.MessageText ?? Uniconta.ClientTools.Localization.lookup(result.Err.ToString());
                sb.Append(Uniconta.ClientTools.Localization.lookup(msgKey));

                UnicontaMessageBox.Show(sb.ToStringAndRelease(), Uniconta.ClientTools.Localization.lookup("Error"));
                return;
            }

            var tmLines = (dgTMJournalLineGrid.ItemsSource as IEnumerable<TMJournalLineClient>) ?? Enumerable.Empty<TMJournalLineClient>();
            var tmLinesMileage = (dgTMJournalLineTransRegGrid.ItemsSource as IEnumerable<TMJournalLineClient>) ?? Enumerable.Empty<TMJournalLineClient>();
            var allLines = tmLines.Concat(tmLinesMileage).ToList();

            foreach (var line in allLines)
            {
                line.LineStatus = TMLineStatus.OK.ToString();
                line.ErrorInfo = null;
            }

            dgTMJournalLineGrid.Columns.GetColumnByName("LineStatus").Visible = true;
            dgTMJournalLineTransRegGrid.Columns.GetColumnByName("colLineStatus").Visible = true;

            if (result.Err == ErrorCodes.Succes && result.CountWarnings == 0)
            {
                if (showMsgBox)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("JournalOK"), Uniconta.ClientTools.Localization.lookup("Message"));
                return;
            }

            dgTMJournalLineGrid.Columns.GetColumnByName("ErrorInfo").Visible = true;
            dgTMJournalLineTransRegGrid.Columns.GetColumnByName("colErrorInfo").Visible = true;

            var map = allLines.GroupBy(x => x.RowId).ToDictionary(g => g.Key, g => g.First());
            int cntErr = 0, cntErrMileage = 0, cntWarning = 0, cntWarningMileage = 0;

            if (result.Lines != null)
            {
                foreach (var error in result.Lines)
                {
                    var isMileage = error.RegistrationType == RegistrationType.Mileage;
                    var isWarning = error.LineStatus == TMLineStatus.Warning;
                    if (isMileage)
                    {
                        if (isWarning) cntWarningMileage++;
                        else cntErrMileage++;
                    }
                    else
                    {
                        if (isWarning) cntWarning++;
                        else cntErr++;
                    }

                    if (map.TryGetValue(error.TransRowId, out var rec))
                    {
                        rec.ErrorInfo = error.MessageText;
                        rec.LineStatus = error.LineStatus.ToString();
                    }
                }
            }

            string headerTxt = null;
            if (result.CountErrors != 0)
            {
                headerTxt = Uniconta.ClientTools.Localization.lookup("Error");
                if (cntErrMileage != 0)
                    sb.Append("(").Append(Uniconta.ClientTools.Localization.lookup("Hours")).Append(") ");

                sb.Append(cntErr).Append(" ").Append(Uniconta.ClientTools.Localization.lookup("JournalFailedValidation"));

                if (cntErrMileage != 0)
                {
                    sb.AppendLine().AppendLine().Append("(").Append(Uniconta.ClientTools.Localization.lookup("Mileage")).Append(") ")
                .Append(NumberConvert.ToString(cntErrMileage)).Append(" ").Append(Uniconta.ClientTools.Localization.lookup("JournalFailedValidation"));

                    if (layOutInvItemStorage.Visibility == Visibility.Collapsed)
                    {
                        layOutInvItemStorage.Visibility = Visibility.Visible;
                        ShowHideMileage();
                    }
                }
            }
            else if (result.CountWarnings > 0)
            {
                headerTxt = Uniconta.ClientTools.Localization.lookup("Information");
                sb.Append(cntWarning).Append(" ").Append(Uniconta.ClientTools.Localization.lookup("JournalWarningValidation"));
                sb.AppendLine().AppendLine().Append(Uniconta.ClientTools.Localization.lookup("JournalWarningNote"));

            }
            UnicontaMessageBox.Show(sb.ToStringAndRelease(), headerTxt);
        }

        private void ActionClose()
        {
            double[] daySums = { day1Sum, day2Sum, day3Sum, day4Sum, day5Sum, day6Sum, day7Sum };
            var closeDate = (int)employee._TMCloseDate.DayOfWeek;
            int closeDay = 7;

            double totalDaySums = daySums.Sum();
            double totalNormHours = normHoursArr.Sum();
            var zeroCalendar = totalNormHours == 0;
            if (totalDaySums < totalNormHours || zeroCalendar)
            {
                for (int i = 6; i >= 0; i--)
                {
                    if (zeroCalendar && daySums[i] != 0)
                    {
                        closeDay = i >= 4 ? 7 : i + 1;
                        break;
                    }
                    else
                    {
                        var remaining = normHoursArr[i] - daySums[i];
                        if (closeDate < i + 1 && remaining > 0)
                            closeDay = i;
                    }
                }
            }
            var calcCloseDate = closeDay == 0 ? JournalLineDate : JournalLineDate.AddDays(closeDay - 1);

            var cwDate = new CwSetPeriodPerDate(TMJournalActionType.Close, calcCloseDate, employee, api, true);
            cwDate.DialogTableId = 2000000055;

            cwDate.Closing += async delegate
            {
                if (cwDate.DialogResult == true)
                {
                    api.AllowBackgroundCrud = false;
                    var err = await saveGrid();
                    if (err != 0)
                    {
                        UtilDisplay.ShowErrorCode(err);
                        return;
                    }
                    api.AllowBackgroundCrud = true;

                    var postingRes = await postingApi.CloseTimeJournal(employee, cwDate.StartDate);

                    ShowJournalInfo(postingRes, postingRes.CreatedOvertime);
                    if (postingRes.Err == 0)
                    {
                        await api.Read(employee);
                        SetButtons();
                        GetDateStatusOnCalender(employee);
                        RefreshGrid();
                    }
                }
            };
            cwDate.Show();
        }

        private void ActionOpen()
        {
            var maxDate = JournalLineDate.AddDays(6);
            var openDate = employee._TMApproveDate < JournalLineDate ? JournalLineDate :
                employee._TMApproveDate < maxDate ? employee._TMApproveDate.AddDays(1) :
                maxDate;

            var cwDate = new CwSetPeriodPerDate(TMJournalActionType.Open, openDate, employee, api);
            cwDate.DialogTableId = 2000000055;

            cwDate.Closing += async delegate
            {
                if (cwDate.DialogResult == true)
                {
                    var postingRes = await postingApi.OpenTimeJournal(employee, cwDate.StartDate);
                    ShowJournalInfo(postingRes, false);
                    if (postingRes.Err == 0)
                    {
                        await api.Read(employee);
                        SetButtons();
                        GetDateStatusOnCalender(employee);
                        RefreshGrid();
                    }
                }
            };
            cwDate.Show();
        }

        private void ActionApprove()
        {
            var approveDate = employee._TMCloseDate >= JournalLineDate && employee._TMCloseDate <= JournalLineDate.AddDays(6) ? employee._TMCloseDate : JournalLineDate.AddDays(6);
            CWTimePosting postingDialog = new CWTimePosting(approveDate, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Period"), GetPeriod(JournalLineDate)), api.CompanyEntity.Name);
            postingDialog.DialogTableId = 2000000065;
            postingDialog.Closed += async delegate
            {
                if (postingDialog.DialogResult == true)
                {
                    var postingRes = await postingApi.ApproveTimeJournal(employee, postingDialog.PostedDate, postingDialog.IsSimulation);
                    ShowJournalInfo(postingRes, postingRes.CreatedOvertime);
                    if (postingRes.Err == 0)
                    {
                        if (postingDialog.IsSimulation)
                        {
                            AddDockItem(TabControls.SimulatedPrJournalLinePage, postingRes.SimulatedLines, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                        }
                        else
                        {
                            await api.Read(employee);
                            SetButtons();
                            GetDateStatusOnCalender(employee);
                            RefreshGrid();
                        }
                    }
                }
            };
            postingDialog.Show();
        }

        public class TMJournalProjectFilter : SQLCacheFilter
        {
            public TMJournalProjectFilter(SQLCache cache) : base(cache) { }
            public override bool IsValid(object rec)
            {
                var p = ((Uniconta.DataModel.Project)rec);
                return (p._Phase == ProjectPhase.Created || p._Phase == ProjectPhase.Accepted || p._Phase == ProjectPhase.InProgress) && !p._Blocked;
            }
        }

        private void SetProjectSource(TMJournalLineClient rec)
        {
            if (employee.EmpProjects != null && employee.EmpProjects.Count() != 0)
                rec._projectSource = employee.EmpProjects;
            else if (projCache != null && projCache.Count != 0)
                rec._projectSource = new TMJournalProjectFilter(projCache.cache);

            if (rec._projectSource != null)
                rec.NotifyPropertyChanged("ProjectSource");

        }

        public class TMJournalPayrollFilter : SQLCacheFilter
        {
            readonly bool Invoiceable;
            readonly bool IsHoursPayroll;
            public TMJournalPayrollFilter(SQLCache cache, bool Invoiceable, bool isHoursPayroll) : base(cache) { this.Invoiceable = Invoiceable; this.IsHoursPayroll = isHoursPayroll; }

            public override bool IsValid(object rec)
            {
                var p = ((Uniconta.DataModel.EmpPayrollCategory)rec);
                if (IsHoursPayroll)
                    return (p._Invoiceable == this.Invoiceable && p._InternalType < Uniconta.DataModel.InternalType.Mileage);
                else
                    return (p._Invoiceable == this.Invoiceable && p._InternalType == Uniconta.DataModel.InternalType.Mileage);
            }
        }

        private void SetPayrollSource(TMJournalLineClient rec, bool isHoursPayroll = true)
        {
            if (payrollCache != null && payrollCache.Count != 0)
            {
                var pg = rec.ProjectRef?.ProjectGroup;
                if (pg != null)
                {
                    rec.PayrollSource = new TMJournalPayrollFilter(payrollCache.cache, pg._Invoiceable, isHoursPayroll);
                    if (rec.PayrollSource != null)
                        rec.NotifyPropertyChanged("PayrollSource");
                }
            }
        }

        async void LoadGridOnWeekChange(bool copyLines = true)
        {
            if (clearMileageList || clearHoursList)
            {
                journalLineNotApprovedLst = null;
                internalTransLst = null;
                clearMileageList = false;
                clearHoursList = false;
            }

            tmJournalLineFilter = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements("Date", JournalLineDate, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("RegistrationType", "0", CompareOperator.Equal, typeof(int))
            };
            tmJournalLineTransReg = new PropValuePair[]
            {
                 PropValuePair.GenereteWhereElements("Date", JournalLineDate, CompareOperator.Equal),
                 PropValuePair.GenereteWhereElements("RegistrationType", "1", CompareOperator.Equal, typeof(int))
            };
            SetColumnHeader();
            await BindGrid();

            if (copyLines)
                CopyLines();
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            projCache = projCache ?? await api.LoadCache<Uniconta.DataModel.Project>().ConfigureAwait(false);
            payrollCache = payrollCache ?? await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>().ConfigureAwait(false);
            CategoryCache = CategoryCache ?? await api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).ConfigureAwait(false);
            projGroupCache = projGroupCache ?? await api.LoadCache<Uniconta.DataModel.ProjectGroup>().ConfigureAwait(false);
            ItemCache = ItemCache ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            workspaceCache = workspaceCache ?? await api.LoadCache<Uniconta.DataModel.PrWorkSpace>().ConfigureAwait(false);

            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.ProjectTask) });

            defaultWrkSpace = workspaceCache?.FirstOrDefault(s => s._Default)?._Number;
            dgTMJournalLineGrid.WorkSpaceDefault = defaultWrkSpace;
            dgTMJournalLineTransRegGrid.WorkSpaceDefault = defaultWrkSpace;
        }

        protected async override Task<ErrorCodes> saveGrid()
        {
            await dgTMJournalLineTransRegGrid.SaveData();
            return await dgTMJournalLineGrid.SaveData();
        }

        public override bool FilterOnLoadLayout => false;
    }
}
