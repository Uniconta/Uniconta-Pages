using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using DevExpress.XtraGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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
using DevExpress.Data;
using System.Threading;
using DevExpress.Xpf.Editors;
using System.Reflection;
using Uniconta.ClientTools.Util;
using System.Collections;
using UnicontaClient.Pages.Project.TimeManagement;
using Uniconta.ClientTools.Controls;
using DevExpress.Xpf.Core.Serialization;
using System.Text.RegularExpressions;
using UnicontaAPI.Project.API;
using Uniconta.ClientTools;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using Uniconta.API.GeneralLedger;
using static UnicontaClient.Pages.Project.TimeManagement.TMJournalLineHelper;
using System.Collections.ObjectModel;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TMJournalLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TMJournalLineClientLocal); } }

        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override IComparer GridSorting { get { return new TMJournalLineSort(); } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return true; } }
        public DateTime JnlLineDate { get; set; }
        public string RegType { get; set; }
        public Uniconta.DataModel.Employee Employee { get; set; }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (TMJournalLineClientLocal)this.SelectedItem;
            if ((selectedItem != null && (JnlLineDate.AddDays(6) <= Employee._TMApproveDate || JnlLineDate.AddDays(6) <= Employee._TMCloseDate)) || selectedItem == null || selectedItem._Project == null || selectedItem._PayrollCategory == null)
                return false;
            return true;
        }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var header = (Uniconta.DataModel.Employee)this.masterRecord;
            var newRow = (TMJournalLineClientLocal)dataEntity;
            newRow.Date = JnlLineDate;
            newRow.RegistrationType = RegType;
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
        bool anyChange;
        List<EmpPayrollCategoryEmployeeClient> empPriceLst;
        PropValuePair[] tmJournalLineFilter, tmJournalLineTransReg;
        DateTime JournalLineDate;
        Uniconta.DataModel.Employee employee;
        DateTime employeeCalenderStartDate = GetSystemDefaultDate();
        SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> payrollCache;
        SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupCache;
        SQLTableCache<Uniconta.ClientTools.DataModel.ProjectClient> projCache;
        double vacationYTD, vacationNotApproved, otherVacationYTD, otherVacationNotApproved, overTimeYTD, overTimeNotApproved, flexTimeYTD, flexTimeNotApproved, mileageYTD, mileageNotApproved;
        bool clearMileageList, clearHoursList;
        HashSet<string> lstCatMileage, lstCatVacation, lstCatOtherVacation, lstCatFlexTime, lstCatOverTime, lstCatSickness, lstCatOtherAbsence;

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

#if !SILVERLIGHT
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
#endif

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
                var cache = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? api.LoadCache(typeof(Uniconta.DataModel.Employee)).GetAwaiter().GetResult();
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
            ((TableView)dgTMJournalLineTransRegGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            ((TableView)dgTMJournalLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
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
        }

        private void TableView_ShowingEditor1(object sender, ShowingEditorEventArgs e)
        {
            var selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
            if (selectedItem != null)
            {
                if (e.Column.FieldName != "Invoiceable") return;
                e.Cancel = selectedItem.IsEditable == 0 ? false : true;
            }
        }

        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClientLocal;
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
            if (!Comp.ProjectTask)
            {
                Task.Visible = false;
                Task.ShowInColumnChooser = false;
            }
            else
                Task.ShowInColumnChooser = true;
        }

        void SetEmployee(Uniconta.DataModel.Employee master)
        {
            this.employee = master;
            SetFields(employeeCalenderStartDate);
            dgTMJournalLineGrid.UpdateMaster(master);
            dgTMJournalLineTransRegGrid.UpdateMaster(master);
            GetDateStatusOnCalender(master);
            if (tmHelper != null)
                tmHelper.employee = master;
            else
                tmHelper = new TMJournalLineHelper(api, master);
            LoadEmpPrices();
            SetButtons();
        }

        void GetDateStatusOnCalender(Uniconta.DataModel.Employee employee)
        {
#if !SILVERLIGHT
            var emplCalStart = GetSystemDefaultDate().AddYears(-1);
            var approveDte = employee._TMApproveDate == DateTime.MinValue ? GetSystemDefaultDate().AddYears(-1) : employee._TMApproveDate;
            var specialDates = new ObservableCollection<MySpecialDate>();
            for (DateTime date = emplCalStart; date.Date <= approveDte; date = date.AddDays(1))
                specialDates.Add(new MySpecialDate { Date = date, Color = Brushes.Green });
            for (DateTime date = approveDte.AddDays(1); date.Date <= employee._TMCloseDate; date = date.AddDays(1))
                specialDates.Add(new MySpecialDate { Date = date, Color = Brushes.Yellow });
            txtDateTo.MySpecialDates = specialDates;
#endif
        }

        private void DgTMJournalLineTransRegGridDataControl_CurrentItemChanged1(object sender, CurrentItemChangedEventArgs e)
        {
            TMJournalLineClientLocal oldselectedItem = e.OldItem as TMJournalLineClientLocal;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= DgTMJournalLineTransRegSelectedItem_PropertyChanged;

            TMJournalLineClientLocal selectedItem = e.NewItem as TMJournalLineClientLocal;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += DgTMJournalLineTransRegSelectedItem_PropertyChanged;
            }
        }
        private void DgTMJournalLineTransRegSelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (TMJournalLineClientLocal)sender;
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
            }
            else if (e.PropertyName == "PayrollCategory")
            {
                SetInvoiceable(rec);
                clearMileageList = true;
                RecalculateWeekInternalMileage();
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
                var col = e.Collection as GridSummaryItemCollection;
                var summary = e.CollectionItem as GridSummaryItem;
                col.Remove(summary);
                var newItem = new SumColumn();
                col.Add(newItem);
                e.CollectionItem = newItem;
                applySummaryStyle = true;
            }
        }

        bool KeyAllowed(string shortCut)
        {
            TMJournalLineClientLocal selectedItem;
            if (mileageGridFocus)
                selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
            else
                selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClientLocal;

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

#if !SILVERLIGHT
        protected override void OnPreviewKeyDown(KeyEventArgs e)
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
            foreach (GridSummaryItem item in dgTMJournalLineGrid.TotalSummary)
            {
                if (item is SumColumn)
                {
                    SumColumn customItem = (SumColumn)item;
                    if (customItem.SerializableTag == "TotalDay1" ||
                        customItem.SerializableTag == "TotalDay2" ||
                        customItem.SerializableTag == "TotalDay3" ||
                        customItem.SerializableTag == "TotalDay4" ||
                        customItem.SerializableTag == "TotalDay5" ||
                        customItem.SerializableTag == "TotalDay6" ||
                        customItem.SerializableTag == "TotalDay7" ||
                        customItem.SerializableTag == "TotalSum")
                    {
                        Style style = this.FindResource("SummaryTotalStyle") as Style;
                        customItem.TotalSummaryElementStyle = style;
                    }
                }
            }

            applySummaryStyle = false;
        }
#endif

        private void DataControl_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            dgTMJournalLineGrid.UpdateTotalSummary();
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            TMJournalLineClientLocal oldselectedItem = e.OldItem as TMJournalLineClientLocal;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            TMJournalLineClientLocal selectedItem = e.NewItem as TMJournalLineClientLocal;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
        }

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (TMJournalLineClientLocal)sender;
            if (e.PropertyName == "Project" ||
                e.PropertyName == "Day1" ||
                e.PropertyName == "Day2" ||
                e.PropertyName == "Day3" ||
                e.PropertyName == "Day4" ||
                e.PropertyName == "Day5" ||
                e.PropertyName == "Day6" ||
                e.PropertyName == "Day7")
            {
                if (rec._InternalType != Uniconta.DataModel.InternalType.None)
                {
                    clearHoursList = true;
                    RecalculateWeekInternalHour();
                }
                RecalculateEfficiencyPercentage();
                var proj = (Uniconta.DataModel.Project)projCache.Get(rec._Project);
                SetProjectTask(proj, rec);
            }
            else if (e.PropertyName == "PayrollCategory")
            {
                SetInvoiceable(rec);
                rec.NotifyPropertyChanged("IsMatched");
                if (rec._InternalType != Uniconta.DataModel.InternalType.None)
                {
                    clearHoursList = true;
                    RecalculateWeekInternalHour();
                }
            }

            dgTMJournalLineGrid.UpdateTotalSummary();
        }

        async void SetProjectTask(Uniconta.DataModel.Project project, TMJournalLineClientLocal rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                    rec._projectTaskSource = project.Tasks ?? await project.LoadTasks(api);
                else
                {
                    rec._projectTaskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("ProjectTaskSource");
            }
        }

        private void ProjectTask_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClientLocal;
            if (selectedItem?._Project != null)
            {
                var selected = (Uniconta.DataModel.Project)projCache.Get(selectedItem._Project);
                SetProjectTask(selected, selectedItem);
            }
        }

        void SetFields(DateTime selectedDate) //FLYT til bunden af SetEmployee og fjerner fra sync
        {
            cmbRegistration.ItemsSource = Uniconta.ClientTools.AppEnums.RegistrationType.Values;
            cmbRegistration.SelectedIndex = 0;
            JournalLineDate = FirstDayOfWeek(selectedDate);
            txtDateTo.DateTime = JournalLineDate;
            tmJournalLineFilter = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements("Date", JournalLineDate, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("RegistrationType", typeof(int), "0"),
            };
            tmJournalLineTransReg = new PropValuePair[]
            {
                 PropValuePair.GenereteWhereElements("Date", JournalLineDate, CompareOperator.Equal),
                 PropValuePair.GenereteWhereElements("RegistrationType", typeof(int), "1")
            };
            SetColumnHeader();
        }

        TMApprovalSetupClient[] approverLst;
        Uniconta.DataModel.Employee[] employeeLst;
        async void SetButtons()
        {
            if (payrollCache == null)
                payrollCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>() ?? await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>();

            if (projGroupCache == null)
                projGroupCache = api.GetCache<Uniconta.DataModel.ProjectGroup>() ?? await api.LoadCache<Uniconta.DataModel.ProjectGroup>();

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
                ribbonControl.DisableButtons("EditMileage");
            }
            else
            {
                ribbonControl.DisableButtons("AddMileage");
                ribbonControl.DisableButtons("EditMileage");

                if (JournalLineDate.AddDays(6) <= employee._TMCloseDate || JournalLineDate.AddDays(6) <= employee._TMApproveDate)
                {
                    ribbonControl.DisableButtons("AddMileage");
                    ribbonControl.DisableButtons("EditMileage");
                }
                else
                {
                    ribbonControl.EnableButtons("AddMileage");
                    ribbonControl.EnableButtons("EditMileage");
                }

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
                ribbonControl.DisableButtons("EditMileage");
            }
            else
            {
                transRegBtn.Caption = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Hide"), Uniconta.ClientTools.Localization.lookup("Mileage"));
                ribbonControl.EnableButtons("AddMileage");
                ribbonControl.EnableButtons("EditMileage");
            }
        }

        async void CopyLines()
        {
            if (employee._TMCloseDate >= JournalLineDate)
                return;

            var gridHours = dgTMJournalLineGrid.ItemsSource as ICollection;
            if (gridHours.Count > 0)
                return;

            var pairJournalTrans = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), employee.KeyStr),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), JournalLineDate.AddDays(-7), CompareOperator.Equal),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.RegistrationType), typeof(int), "0"),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.CopyLine), typeof(bool), "1")
            };
            var copyPrevJournalLine = await api.Query<TMJournalLineClientLocal>(pairJournalTrans);
            if (copyPrevJournalLine == null || copyPrevJournalLine.Length == 0)
                return;

            foreach (var rec in copyPrevJournalLine)
            {
                rec._Date = JournalLineDate;
                rec._Day1 = rec._Day2 = rec._Day3 = rec._Day4 = rec._Day5 = rec._Day6 = rec._Day7 = 0;
                rec._AddressFrom = rec._AddressTo = rec._VechicleRegNo = null;
                rec._LineNumber = 0;
                rec._JournalPostedId = 0;
                rec.RowId = 0;
                dgTMJournalLineGrid.AddRow(rec, -1, false);
            }
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

        void SetInvoiceable(TMJournalLineClientLocal rec)
        {
            var payroll = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(rec.PayrollCategory);
            if (payroll != null)
                rec.Invoiceable = payroll._Invoiceable;
        }

        List<ProjectTransClient> internalTransLst;
        List<TMJournalLineClient> journalLineNotApprovedLst;
        DateTime approvedCutOffDate;
        string mileageInternalProject;
        async void InitializeStatusText()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;

            if ((internalTransLst == null || journalLineNotApprovedLst == null) && payrollCache != null && payrollCache.Count > 0)
            {
                approvedCutOffDate = employee._TMApproveDate == DateTime.MinValue ? DateTime.MinValue :
                                     employee._TMApproveDate.DayOfWeek == DayOfWeek.Sunday ? employee._TMApproveDate.AddDays(1) :
                                     employee._TMApproveDate.DayOfWeek == DayOfWeek.Monday ? employee._TMApproveDate :
                                     FirstDayOfWeek(employee._TMApproveDate);

                HashSet<string> lstCatPayProj = null, lstCatPay = null;

                foreach (var val in payrollCache)
                {
                    if (val._InternalType == Uniconta.DataModel.InternalType.None || val._InternalType == Uniconta.DataModel.InternalType.OtherAbsence || val._InternalType == Uniconta.DataModel.InternalType.Sickness)
                        continue;

                    switch (val._InternalType)
                    {
                        case Uniconta.DataModel.InternalType.FlexTime: AddToList(ref lstCatFlexTime, val._Number); break;
                        case Uniconta.DataModel.InternalType.Vacation: AddToList(ref lstCatVacation, val._Number); break;
                        case Uniconta.DataModel.InternalType.OtherVacation: AddToList(ref lstCatOtherVacation, val._Number); break;
                        case Uniconta.DataModel.InternalType.OverTime: AddToList(ref lstCatOverTime, val._Number); break;
                        case Uniconta.DataModel.InternalType.Sickness: AddToList(ref lstCatSickness, val._Number); break;
                        case Uniconta.DataModel.InternalType.OtherAbsence: AddToList(ref lstCatOtherAbsence, val._Number); break;
                        case Uniconta.DataModel.InternalType.Mileage:
                            AddToList(ref lstCatMileage, val._Number);
                            mileageInternalProject = val._InternalProject;
                            break;
                    }

                    if (val._InternalType != Uniconta.DataModel.InternalType.Mileage)
                    {
                        AddToList(ref lstCatPayProj, val._InternalProject);
                        AddToList(ref lstCatPay, val._Number);
                    }
                }

                var catPayDist = lstCatPay != null ? string.Join(";", lstCatPay) : string.Empty;
                var catPayProjDist = lstCatPayProj != null ? string.Join(";", lstCatPayProj) : string.Empty;

                if (internalTransLst == null)
                {
                    var pairInternalTrans = new PropValuePair[]
                    {
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Employee), typeof(string), employee.KeyStr),
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Project), typeof(string), catPayProjDist),
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.PayrollCategory), typeof(string), catPayDist),
                    };
                    var internalProjTrans = await api.Query<ProjectTransClient>(pairInternalTrans);
                    internalTransLst = internalProjTrans != null ? internalProjTrans.ToList() : new List<ProjectTransClient>();

                    if (lstCatMileage != null)
                    {
                        var mileageCatDist = string.Join(";", lstCatMileage);

                        var pairmileageTrans = new PropValuePair[]
                        {
                            PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Employee), typeof(string), employee.KeyStr),
                            PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.PayrollCategory), typeof(string), mileageCatDist),
                        };
                        var mileageTrans = await api.Query<ProjectTransClient>(pairmileageTrans);
                        if (mileageTrans != null && mileageTrans.Length > 0)
                            internalTransLst.AddRange(mileageTrans);
                    }
                }

                if (journalLineNotApprovedLst == null)
                {
                    var pairJournalTrans = new List<PropValuePair>
                    {
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Project), typeof(string), catPayProjDist),
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), employee.KeyStr),
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.PayrollCategory), typeof(string), catPayDist),
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), approvedCutOffDate, CompareOperator.GreaterThanOrEqual)
                    };

                    var lineNotApprovedLst = await api.Query<TMJournalLineClient>(pairJournalTrans);
                    journalLineNotApprovedLst = lineNotApprovedLst != null ? lineNotApprovedLst.ToList() : new List<TMJournalLineClient>();

                    if (lstCatMileage != null)
                    {
                        var mileageCatDist = string.Join(";", lstCatMileage);

                        var pairmileageTrans = new PropValuePair[]
                        {
                            PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), employee.KeyStr),
                            PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.RegistrationType), typeof(int), "1"),
                            PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.PayrollCategory), typeof(string), mileageCatDist),
                            PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), approvedCutOffDate, CompareOperator.GreaterThanOrEqual)
                        };
                        var mileageTrans = await api.Query<TMJournalLineClient>(pairmileageTrans);
                        if (mileageTrans != null && mileageTrans.Length > 0)
                            journalLineNotApprovedLst.AddRange(mileageTrans);
                    }
                }
            }

            #region Vacation
            vacationYTD = 0;
            vacationNotApproved = 0;

            if (lstCatVacation == null)
                UtilDisplay.RemoveMenuCommand(rb, "VacationBal");
            else
            {
                if (internalTransLst != null || journalLineNotApprovedLst != null)
                {
                    var vacationStartDate = JournalLineDate < new DateTime(2021, 1, 1) ? new DateTime(2020, 5, 1) : new DateTime(JournalLineDate.Year, 1, 1);

                    if (internalTransLst != null)
                    {
                        var projtransEndDate = JournalLineDate.AddDays(6);

                        var transList = internalTransLst.Where(s => lstCatVacation.Any(n => n == s._PayrollCategory) && s.Date >= vacationStartDate && s.Date <= projtransEndDate).ToList();
                        vacationYTD = -transList.Select(x => x.Qty).Sum();
                    }

                    if (journalLineNotApprovedLst != null)
                    {
                        var projtransEndDateAppr = approvedCutOffDate == DateTime.MinValue ? vacationStartDate :
                                               approvedCutOffDate.AddDays(-1) < vacationStartDate ? vacationStartDate : approvedCutOffDate.AddDays(-1);

                        if (projtransEndDateAppr > JournalLineDate.AddDays(6))
                            projtransEndDateAppr = JournalLineDate.AddDays(6);

                        var transList = journalLineNotApprovedLst.Where(s => s._RegistrationType == RegistrationType.Hours &&
                                                                             s._InternalType == Uniconta.DataModel.InternalType.Vacation &&
                                                                             s._Invoiceable == false && s.Date >= projtransEndDateAppr && s.Date < JournalLineDate);
                        vacationNotApproved = transList.Select(x => x.Total).Sum();
                    }
                }
            }
            #endregion Vacation

            #region Other vacation
            otherVacationYTD = 0;
            otherVacationNotApproved = 0;

            if (lstCatOtherVacation == null)
                UtilDisplay.RemoveMenuCommand(rb, "OtherVacationBal");
            else
            {
                if (internalTransLst != null || journalLineNotApprovedLst != null)
                {
                    var vacationStartDate = JournalLineDate < new DateTime(2021, 1, 1) ? new DateTime(2020, 5, 1) : new DateTime(JournalLineDate.Year, 1, 1);

                    if (internalTransLst != null)
                    {
                        var projtransEndDate = JournalLineDate.AddDays(6);

                        var transList = internalTransLst.Where(s => lstCatOtherVacation.Any(n => n == s._PayrollCategory) && s.Date >= vacationStartDate && s.Date <= projtransEndDate).ToList();
                        otherVacationYTD = -transList.Select(x => x.Qty).Sum();
                    }

                    if (journalLineNotApprovedLst != null)
                    {
                        var projtransEndDateAppr = approvedCutOffDate == DateTime.MinValue ? vacationStartDate :
                                                   approvedCutOffDate.AddDays(-1) < vacationStartDate ? vacationStartDate : approvedCutOffDate.AddDays(-1);

                        if (projtransEndDateAppr > JournalLineDate.AddDays(6))
                            projtransEndDateAppr = JournalLineDate.AddDays(6);

                        var transList = journalLineNotApprovedLst.Where(s => s._RegistrationType == RegistrationType.Hours &&
                                                                             s._InternalType == Uniconta.DataModel.InternalType.OtherVacation &&
                                                                             s._Invoiceable == false && s.Date >= projtransEndDateAppr && s.Date < JournalLineDate);
                        otherVacationNotApproved = transList.Select(x => x.Total).Sum();
                    }
                }
            }
            #endregion Other vacation

            #region Overtime
            overTimeYTD = 0;
            overTimeNotApproved = 0;

            if (lstCatOverTime == null)
                UtilDisplay.RemoveMenuCommand(rb, "OvertimeBal");
            else
            {
                if (internalTransLst != null)
                {
                    var projtransEndDateOvertime = approvedCutOffDate == DateTime.MinValue ? DateTime.MaxValue : approvedCutOffDate.AddDays(-1);
                    if (projtransEndDateOvertime > JournalLineDate.AddDays(6))
                        projtransEndDateOvertime = JournalLineDate.AddDays(6);

                    var transList = internalTransLst.Where(s => lstCatOverTime.Any(n => n == s._PayrollCategory) && s.Date <= projtransEndDateOvertime).ToList();
                    overTimeYTD = -transList.Select(x => x.Qty).Sum();
                }

                if (journalLineNotApprovedLst != null)
                {
                    var projtransEndDateOvertimeNotAppr = approvedCutOffDate == DateTime.MinValue ? DateTime.MinValue : approvedCutOffDate.AddDays(-1);

                    var transList = journalLineNotApprovedLst
                                    .Where(s => s._RegistrationType == RegistrationType.Hours &&
                                                s._InternalType == Uniconta.DataModel.InternalType.OverTime &&
                                                s._Invoiceable == false && s.Date >= projtransEndDateOvertimeNotAppr && s.Date < JournalLineDate)
                                    .GroupBy(s => s.PayrollCategory)
                                    .Select(t => new { Payroll = t.Key, Value = t.Sum(u => u.Total) }).ToList();

                    foreach (var rec in transList)
                    {
                        var factor = (double)payrollCache.Get(rec?.Payroll)?._Factor;
                        overTimeNotApproved += factor != 0 ? factor * rec.Value : rec.Value;
                    }
                }
            }
            #endregion Overtime

            #region FlexTime
            flexTimeYTD = 0;
            flexTimeNotApproved = 0;

            if (lstCatFlexTime == null)
                UtilDisplay.RemoveMenuCommand(rb, "FlexTimeBal");
            else
            {
                if (internalTransLst != null)
                {
                    var projtransEndDateFlex = approvedCutOffDate == DateTime.MinValue ? DateTime.MaxValue : approvedCutOffDate.AddDays(-1);
                    if (projtransEndDateFlex > JournalLineDate.AddDays(6))
                        projtransEndDateFlex = JournalLineDate.AddDays(6);

                    var transList = internalTransLst.Where(s => lstCatFlexTime.Any(n => n == s._PayrollCategory) && s.Date <= projtransEndDateFlex).ToList();
                    flexTimeYTD = -transList.Select(x => x.Qty).Sum();
                }

                if (journalLineNotApprovedLst != null)
                {
                    var projtransEndDateFlexNotAppr = approvedCutOffDate == DateTime.MinValue ? DateTime.MinValue : approvedCutOffDate.AddDays(-1);

                    var transList = journalLineNotApprovedLst
                                 .Where(s => s._RegistrationType == RegistrationType.Hours &&
                                             s._InternalType == Uniconta.DataModel.InternalType.FlexTime &&
                                             s._Invoiceable == false && s.Date >= projtransEndDateFlexNotAppr && s.Date < JournalLineDate)
                                 .GroupBy(s => s.PayrollCategory)
                                 .Select(t => new { Payroll = t.Key, Value = t.Sum(u => u.Total) }).ToList();

                    foreach (var rec in transList)
                    {
                        var factor = (double)payrollCache.Get(rec?.Payroll)?._Factor;
                        flexTimeNotApproved += factor != 0 ? factor * rec.Value : rec.Value;
                    }
                }
            }
            #endregion FlexTime

            #region Mileage
            mileageYTD = 0;
            mileageNotApproved = 0;

            if (lstCatMileage == null)
                UtilDisplay.RemoveMenuCommand(rb, "MileageBal");
            else
            {
                if (internalTransLst != null || journalLineNotApprovedLst != null)
                {
                    var mileageStartDate = new DateTime(JournalLineDate.Year, 1, 1);

                    if (internalTransLst != null)
                    {
                        var projtransMileageEndDate = JournalLineDate.AddDays(6);
                        var startDateNorm = employee._Hired < calendarStartDate ? calendarStartDate : employee._Hired;

                        if (projtransMileageEndDate < startDateNorm)
                            projtransMileageEndDate = startDateNorm;

                        var transList = internalTransLst.Where(s => lstCatMileage.Any(n => n == s._PayrollCategory) && s.Date >= mileageStartDate && s.Date <= projtransMileageEndDate).ToList();
                        mileageYTD = transList.Select(x => x.Qty).Sum();
                    }

                    if (journalLineNotApprovedLst != null)
                    {
                        if (mileageStartDate.DayOfWeek != DayOfWeek.Monday)
                        {
                            int diff = (7 + (mileageStartDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                            mileageStartDate = mileageStartDate.AddDays(-1 * diff);
                        }

                        var transMileageEndDate = approvedCutOffDate == DateTime.MinValue ? mileageStartDate :
                                                  approvedCutOffDate.AddDays(-1) < mileageStartDate ? mileageStartDate : approvedCutOffDate.AddDays(-1);

                        var transList = journalLineNotApprovedLst.Where(s => s._RegistrationType == RegistrationType.Mileage &&
                                                                             s._InternalType == Uniconta.DataModel.InternalType.Mileage &&
                                                                             s.Date >= transMileageEndDate && s.Date < JournalLineDate);
                        mileageNotApproved = transList.Select(x => x.Total).Sum();
                    }
                }
            }
            #endregion Mileage

            RecalculateWeekInternalHour();
            RecalculateWeekInternalMileage();
            RecalculateEfficiencyPercentage();
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
                            vacationTotal += x.Total;
                        else if (x._InternalType == Uniconta.DataModel.InternalType.OtherVacation)
                            otherVacationTotal += x.Total;
                        else if (x._InternalType == Uniconta.DataModel.InternalType.OverTime)
                        {
                            var payroll = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(x.PayrollCategory);
                            var factor = payroll != null ? (payroll._Factor == 0 ? 1 : payroll._Factor) : 1;
                            overTimeTotal += x.Total * factor;
                        }
                        else if (x._InternalType == Uniconta.DataModel.InternalType.FlexTime)
                        {
                            var payroll = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(x.PayrollCategory);
                            var factor = payroll != null ? (payroll._Factor == 0 ? 1 : payroll._Factor) : 1;
                            flexTimeTotal += x.Total * factor;
                        }
                    }
                }
            }

            SetStatusTextHour(vacationTotal, otherVacationTotal, overTimeTotal, flexTimeTotal);
        }

        void RecalculateEfficiencyPercentage()
        {
            double invoiceableHours = 0;
            double notInvoiceableHours = 0;
            double efficiencyPercentage = 0;

            var gridHours = (IEnumerable<TMJournalLineClient>)dgTMJournalLineGrid.ItemsSource;
            if (gridHours != null)
            {
                var grpLstInvoiceable = gridHours.Where(s => s._InternalType == Uniconta.DataModel.InternalType.None).GroupBy(x => x._Invoiceable).Select(x => new { GroupKey = x.Key, Sum = x.Sum(y => y.Total) });
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
                            mileageTotal += x.Total;
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

        List<TMEmpCalendarSetupClient> calenderLst;
        List<TMEmpCalendarLineClient> calendarLineLst = new List<TMEmpCalendarLineClient>();
        DateTime calendarStartDate;
        async void EmployeeNormalHours()
        {
            busyIndicator.IsBusy = true;
            calendarStartDate = DateTime.MinValue;

            if (calenderLst == null || calenderLst.Any(s => s._Employee == employee._Number) == false)
            {
                var calenderEmpLst = await api.Query<TMEmpCalendarSetupClient>(employee);
                if (calenderEmpLst != null)
                {
                    calenderLst = calenderLst ?? new List<TMEmpCalendarSetupClient>();
                    calenderLst.AddRange(calenderEmpLst);
                }
            }

            normHoursDay1 = normHoursDay2 = normHoursDay3 = normHoursDay4 = normHoursDay5 = normHoursDay6 = normHoursDay7 = normHoursTotal = 0d;

            if (calenderLst != null && calenderLst.Any(s => s._Employee == employee._Number))
            {
                var weekStartDate = employee._Hired > JournalLineDate ? employee._Hired : JournalLineDate;
                var weekEnddate = JournalLineDate.AddDays(6);

                var calenders = calenderLst.
                    Where(x => x._Employee == employee._Number &&
                              (x.ValidFrom <= weekStartDate || x.ValidFrom == DateTime.MinValue || weekEnddate >= x.ValidFrom) &&
                              (x.ValidTo >= weekStartDate || x.ValidTo == DateTime.MinValue)).ToList();

                if (calenders.Count == 1)
                {
                    var tmEmpCalender = calenders.FirstOrDefault();
                    calendarStartDate = tmEmpCalender.ValidFrom;

                    if (calendarLineLst.Any(s => s.Calendar == tmEmpCalender.Calendar) == false)
                    {
                        var calLst = await api.Query<TMEmpCalendarLineClient>(tmEmpCalender.CalendarRef);
                        if (calLst != null)
                            calendarLineLst.AddRange(calLst);
                    }

                    var dateList = calendarLineLst.Where(d => d.Calendar == tmEmpCalender.Calendar && d.Date >= weekStartDate && d.Date <= weekEnddate).ToList(); // get 7 days hours
                    GetHoursForSevenDays(dateList);
                }
                else if (calenders.Count >= 2)
                {
                    if (calendarLineLst.Any(s => s.Calendar == calenders[0].Calendar) == false)
                    {
                        var calLst = await api.Query<TMEmpCalendarLineClient>(calenders[0].CalendarRef);
                        if (calLst != null)
                            calendarLineLst.AddRange(calLst);
                    }
                    var firstCalenderdates = calendarLineLst.Where(x => x.Calendar == calenders[0].Calendar && x.Date >= calenders[0].ValidFrom && x.Date <= calenders[0].ValidTo).ToList();

                    if (calendarLineLst.Any(s => s.Calendar == calenders[1].Calendar) == false)
                    {
                        var calLst = await api.Query<TMEmpCalendarLineClient>(calenders[1].CalendarRef);
                        if (calLst != null)
                            calendarLineLst.AddRange(calLst);
                    }
                    var secCalenderdates = calendarLineLst.Where(x => x.Calendar == calenders[1].Calendar && x.Date >= calenders[1].ValidFrom && x.Date <= calenders[1].ValidTo);

                    firstCalenderdates.AddRange(secCalenderdates);
                    var dateList = firstCalenderdates.Where(d => d.Date >= weekStartDate && d.Date <= weekEnddate).ToList(); // get 7 days hours
                    GetHoursForSevenDays(dateList);
                }
            }

            busyIndicator.IsBusy = false;
        }


        Dictionary<DateTime, double> dictNormDayAmount = new Dictionary<DateTime, double>();

        double normHoursDay1, normHoursDay2, normHoursDay3, normHoursDay4, normHoursDay5, normHoursDay6, normHoursDay7, normHoursTotal;
        void GetHoursForSevenDays(List<TMEmpCalendarLineClient> calenderLine)
        {
            if (calenderLine.Count > 7)
                calenderLine = calenderLine.Take(7).ToList();
            var calenderLineLst = calenderLine.OrderBy(x => x.Date);

            var startDateNorm = employee._Hired < calendarStartDate ? calendarStartDate : employee._Hired;

            foreach (var line in calenderLineLst)
            {
                line.Hours = startDateNorm > line._Date ? 0 : line._Hours;

                if (!dictNormDayAmount.ContainsKey(line._Date))
                    dictNormDayAmount.Add(line._Date, line._Hours);

                var day = line._Date.DayOfWeek;
                switch (day)
                {
                    case DayOfWeek.Monday:
                        normHoursDay1 = line._Hours;
                        break;
                    case DayOfWeek.Tuesday:
                        normHoursDay2 = line._Hours;
                        break;
                    case DayOfWeek.Wednesday:
                        normHoursDay3 = line._Hours;
                        break;
                    case DayOfWeek.Thursday:
                        normHoursDay4 = line._Hours;
                        break;
                    case DayOfWeek.Friday:
                        normHoursDay5 = line._Hours;
                        break;
                    case DayOfWeek.Saturday:
                        normHoursDay6 = line._Hours;
                        break;
                    case DayOfWeek.Sunday:
                        normHoursDay7 = line._Hours;
                        break;
                }
            }

            normHoursTotal = normHoursDay1 + normHoursDay2 + normHoursDay3 + normHoursDay4 + normHoursDay5 + normHoursDay6 + normHoursDay7;
            dgTMJournalLineGrid.UpdateTotalSummary();
        }

        double day1Sum, day2Sum, day3Sum, day4Sum, day5Sum, day6Sum, day7Sum, totalSum;

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

        void GetGridColumnsSum()
        {
            var lst = dgTMJournalLineGrid.ItemsSource as List<TMJournalLineClientLocal>;
            if (lst == null)
                return;
            day1Sum = lst.Select(x => x.Day1).Sum();
            day2Sum = lst.Select(x => x.Day2).Sum();
            day3Sum = lst.Select(x => x.Day3).Sum();
            day4Sum = lst.Select(x => x.Day4).Sum();
            day5Sum = lst.Select(x => x.Day5).Sum();
            day6Sum = lst.Select(x => x.Day6).Sum();
            day7Sum = lst.Select(x => x.Day7).Sum();
            totalSum = lst.Select(x => x.Total).Sum();

            if (lst.Count > 0)
                cmbRegistration.SelectedItem = lst[0].RegistrationType;
            else
                cmbRegistration.SelectedIndex = 0;
        }

        CorasauGridLookupEditorClient prevProject, prevMilageProject;
        private void Hours_Project_GotFocus(object sender, RoutedEventArgs e)
        {
            TMJournalLineClientLocal selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClientLocal;
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
            TMJournalLineClientLocal selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
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
            TMJournalLineClientLocal selectedItem = isHours ? dgTMJournalLineGrid.SelectedItem as TMJournalLineClientLocal : dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
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
            TMJournalLineClientLocal selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClientLocal;
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
            TMJournalLineClientLocal selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
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
            TMJournalLineClientLocal selectedItem = isHours ? dgTMJournalLineGrid.SelectedItem as TMJournalLineClientLocal : dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
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
                var tagName = ((SumColumn)e.Item).SerializableTag as string;
                if (tagName == "Sum")
                    e.TotalValue = Uniconta.ClientTools.Localization.lookup("Mileage");
            }
        }

        private void DgTMJournalLineGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
#if !SILVERLIGHT
            if (applySummaryStyle)
                SetSummaryLayoutStyle();
#endif
            GetGridColumnsSum();
            var fieldName = ((SumColumn)e.Item).FieldName;
            var tagName = ((SumColumn)e.Item).SerializableTag as string;
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
                            e.TotalValue = normHoursDay1;
                        else if (tagName == "TotalDay1")
                            e.TotalValue = day1Sum - normHoursDay1;
                        break;
                    case "Day2":
                        if (tagName == "HoursDay2")
                            e.TotalValue = normHoursDay2;
                        else if (tagName == "TotalDay2")
                            e.TotalValue = day2Sum - normHoursDay2;
                        break;
                    case "Day3":
                        if (tagName == "HoursDay3")
                            e.TotalValue = normHoursDay3;
                        else if (tagName == "TotalDay3")
                            e.TotalValue = day3Sum - normHoursDay3;
                        break;
                    case "Day4":
                        if (tagName == "HoursDay4")
                            e.TotalValue = normHoursDay4;
                        else if (tagName == "TotalDay4")
                            e.TotalValue = day4Sum - normHoursDay4;
                        break;
                    case "Day5":
                        if (tagName == "HoursDay5")
                            e.TotalValue = normHoursDay5;
                        else if (tagName == "TotalDay5")
                            e.TotalValue = day5Sum - normHoursDay5;
                        break;
                    case "Day6":
                        if (tagName == "HoursDay6")
                            e.TotalValue = normHoursDay6;
                        else if (tagName == "TotalDay6")
                            e.TotalValue = day6Sum - normHoursDay6;
                        break;
                    case "Day7":
                        if (tagName == "HoursDay7")
                            e.TotalValue = normHoursDay7;
                        else if (tagName == "TotalDay7")
                            e.TotalValue = day7Sum - normHoursDay7;
                        break;
                    case "Total":
                        if (tagName == "EmpNormHoursSum")
                            e.TotalValue = normHoursTotal;
                        else if (tagName == "TotalSum")
                            e.TotalValue = totalSum - normHoursTotal;
                        break;
                }
            }
        }

        private void btnTransReg_Click(object sender, RoutedEventArgs e)
        {
            AddMilageJournalLine();
        }

        void AddMilageJournalLine()
        {
            if (JournalLineDate.AddDays(6) <= employee._TMApproveDate || JournalLineDate.AddDays(6) <= employee._TMCloseDate)
                return;
            var journalline = new TMJournalLineClientLocal();
            journalline.SetMaster(api.CompanyEntity);
            journalline._Date = JournalLineDate;
            journalline._RegistrationType = Uniconta.DataModel.RegistrationType.Mileage;
            dgTMJournalLineTransRegGrid.AddRow(journalline);
        }

        private void btnTransDel_Click(object sender, RoutedEventArgs e)
        {
            DeleteMilageJournalLine();
        }

        void DeleteMilageJournalLine()
        {
            var selectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
            if (selectedItem != null && KeyAllowed("DeleteLine"))
            {
                dgTMJournalLineTransRegGrid.DeleteRow();
                RecalculateWeekInternalMileage();
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
            EmployeeNormalHours(); //SKAL flyttes ingen medarbejder
            SetValuesForGridProperties(); //Skal flyttes op ingen medarbejder
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgTMJournalLineGrid.Filter(propValuePair);
        }

        private Task FilterTransRegGrid(IEnumerable<PropValuePair> propValuePair)
        {
            return dgTMJournalLineTransRegGrid.Filter(propValuePair);
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
            var selectedItem = dgTMJournalLineGrid.SelectedItem as TMJournalLineClientLocal;
            var selectedItems = dgTMJournalLineGrid.SelectedItems;
            switch (ActionType)
            {
                case "AddRow":
                    if (JournalLineDate.AddDays(6) <= employee._TMApproveDate || JournalLineDate.AddDays(6) <= employee._TMCloseDate)
                        return;
                    var journalLine = new TMJournalLineClientLocal();
                    journalLine.SetMaster(api.CompanyEntity);
                    journalLine._Date = JournalLineDate;
                    if (!mileageGridFocus)
                    {
                        journalLine._RegistrationType = Uniconta.DataModel.RegistrationType.Hours;
                        journalLine.NotifyPropertyChanged("RegistrationType");
                        dgTMJournalLineGrid.AddRow(journalLine);
                    }
                    else
                    {
                        journalLine._RegistrationType = Uniconta.DataModel.RegistrationType.Mileage;
                        journalLine.NotifyPropertyChanged("RegistrationType");
                        dgTMJournalLineTransRegGrid.AddRow(journalLine);
                    }
                    break;
                case "CopyRow":
                    if (selectedItem != null && KeyAllowed("CopyLine") && !mileageGridFocus)
                    {
                        dgTMJournalLineGrid.CopyRow();
                        RecalculateWeekInternalHour();
                        RecalculateEfficiencyPercentage();
                    }
                    else
                    {
                        var selectedMileage = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
                        if (selectedMileage != null && KeyAllowed("CopyLine") && mileageGridFocus)
                            dgTMJournalLineTransRegGrid.CopyRow();
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
                        var selectedMileage = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
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
                    ActionCloseOpen(TMJournalActionType.Close);
                    break;
                case "Open":
                    if (dgTMJournalLineGrid.ItemsSource == null) return;
                    ActionCloseOpen(TMJournalActionType.Open);
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
                    LoadEmpPrices();
                    break;
                case "ValidateJournal":
                    if (dgTMJournalLineGrid.ItemsSource == null) return;
                    ValidateJournal(TMJournalActionType.Validate);
                    break;
                case "AddMileage":
                    if (JournalLineDate.AddDays(6) <= employee._TMApproveDate || JournalLineDate.AddDays(6) <= employee._TMCloseDate)
                        return;
                    if (selectedItem != null && layOutInvItemStorage.Visibility == Visibility.Visible)
                        TimeRegistration(selectedItem, true, true);
                    break;
                case "EditMileage":
                    if (JournalLineDate.AddDays(6) <= employee._TMApproveDate || JournalLineDate.AddDays(6) <= employee._TMCloseDate)
                        return;
                    var MileageFirstRow = dgTMJournalLineTransRegGrid.GetRow(0);
                    if (layOutInvItemStorage.Visibility == Visibility.Collapsed || MileageFirstRow == null)
                        return;
                    var MileageSelectedItem = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
                    if (MileageSelectedItem != null)
                        TimeRegistration(MileageSelectedItem, false);
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void TimeRegistration(TMJournalLineClientLocal selectedItem, bool returning, bool addMileage = false)
        {
            var cw = new CwTransportRegistration(selectedItem, api, mileageHighTotal, returning, addMileage);
            cw.Closed += delegate
            {
                if (cw.DialogResult == true)
                {
                    if (!cw.ExistingTransportJrnlLine)
                    {
                        var journalLine = new TMJournalLineClientLocal();
                        journalLine.SetMaster(api.CompanyEntity);
                        journalLine._Project = cw.RegistrationProject;
                        journalLine._Date = JournalLineDate;
                        journalLine._RegistrationType = Uniconta.DataModel.RegistrationType.Mileage;
                        journalLine._AddressFrom = cw.FromAddress;
                        journalLine._AddressTo = cw.ToAddress;
                        journalLine._Text = cw.Purpose;
                        journalLine._PayrollCategory = cw.PayType;
                        journalLine._VechicleRegNo = cw.VechicleRegNo;
                        journalLine._Day1 = cw.Day1;
                        journalLine._Day2 = cw.Day2;
                        journalLine._Day3 = cw.Day3;
                        journalLine._Day4 = cw.Day4;
                        journalLine._Day5 = cw.Day5;
                        journalLine._Day6 = cw.Day6;
                        journalLine._Day7 = cw.Day7;
                        SetInvoiceable(journalLine);
                        dgTMJournalLineTransRegGrid.AddRow(journalLine);
                        if (cw.Returning)
                        {
                            journalLine._AddressFrom = cw.ToAddress;
                            journalLine._AddressTo = cw.FromAddress;
                            dgTMJournalLineTransRegGrid.AddRow(journalLine);
                        }
                        dgTMJournalLineTransRegGrid.SaveData();
                    }
                    else
                    {
                        dgTMJournalLineTransRegGrid.SetLoadedRow(selectedItem);
                        selectedItem.Project = cw.RegistrationProject;
                        selectedItem.Date = JournalLineDate;
                        selectedItem._RegistrationType = Uniconta.DataModel.RegistrationType.Mileage;
                        selectedItem.AddressFrom = cw.FromAddress;
                        selectedItem.AddressTo = cw.ToAddress;
                        selectedItem.Text = cw.Purpose;
                        selectedItem.PayrollCategory = cw.PayType;
                        selectedItem.VechicleRegNo = cw.VechicleRegNo;
                        selectedItem.Day1 = cw.Day1;
                        selectedItem.Day2 = cw.Day2;
                        selectedItem.Day3 = cw.Day3;
                        selectedItem.Day4 = cw.Day4;
                        selectedItem.Day5 = cw.Day5;
                        selectedItem.Day6 = cw.Day6;
                        selectedItem.Day7 = cw.Day7;
                        SetInvoiceable(selectedItem);
                        dgTMJournalLineTransRegGrid.SetModifiedRow(selectedItem);
                        dgTMJournalLineTransRegGrid.UpdateItemSource(2, selectedItem);
                    }
                    RecalculateWeekInternalMileage();
                }
            };
            cw.Show();
        }

        private async void LoadEmpPrices()
        {
            if (empPriceLst == null || empPriceLst.Any(s => s._Employee == employee._Number) == false)
            {
                var priceLst = await api.Query<EmpPayrollCategoryEmployeeClient>(employee);
                if (priceLst != null)
                {
                    empPriceLst = empPriceLst ?? new List<EmpPayrollCategoryEmployeeClient>();
                    empPriceLst.AddRange(priceLst);
                }
            }
        }

        internal class DayStatus
        {
            public int Status;
            public double Amount;
        }

        private void ActionApprove()
        {
#if !SILVERLIGHT
            if (!ValidateJournal(TMJournalActionType.Approve, false))
                return;

            this.api.AllowBackgroundCrud = false;
            var savetask = saveGrid();
            this.api.AllowBackgroundCrud = true;

            var dDate = employee._TMCloseDate >= JournalLineDate && employee._TMCloseDate <= JournalLineDate.AddDays(6) ? employee._TMCloseDate : JournalLineDate.AddDays(6);
            CWTimePosting postingDialog = new CWTimePosting(dDate, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Period"), GetPeriod(JournalLineDate)), api.CompanyEntity.Name);
            postingDialog.DialogTableId = 2000000065;
            postingDialog.Closed += async delegate
            {
                if (postingDialog.DialogResult != true)
                    return;

                bool emptyApproveDate = false;
                DateTime approveDate = DateTime.MinValue;

                approveDate = postingDialog.PostedDate;

                #region DialogValidation

                if (approveDate == DateTime.MinValue)
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Date")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await api.Read(employee);

                var calendarStartDate = DateTime.MinValue;
                TMEmpCalendarSetupClient calendar;
                if (employee._TMCloseDate == DateTime.MinValue || employee._TMApproveDate == DateTime.MinValue)
                {
                    calendar = calenderLst?.Where(s => s._Employee == employee._Number).OrderBy(s => s.ValidFrom).FirstOrDefault();
                    if (calendar == null)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoCalendar"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    calendarStartDate = calendar.ValidFrom;
                }

                if (approveDate < JournalLineDate || approveDate > JournalLineDate.AddDays(6))
                {
                    UnicontaMessageBox.Show(string.Format("'{0}' only allowed in current Period", TMJournalActionType.Approve), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (employee._TMCloseDate == DateTime.MinValue)
                    employee._TMCloseDate = employee._Hired != DateTime.MinValue && employee._Hired.AddDays(-1) > calendarStartDate.AddDays(-1) ? employee._Hired.AddDays(-1) : calendarStartDate.AddDays(-1);

                if (employee._TMApproveDate == DateTime.MinValue)
                {
                    employee._TMApproveDate = employee._Hired != DateTime.MinValue && employee._Hired.AddDays(-1) > calendarStartDate.AddDays(-1) ? employee._Hired.AddDays(-1) : calendarStartDate.AddDays(-1);
                    emptyApproveDate = true;
                }

                if (approveDate > employee._TMCloseDate)
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ClosePeriodNotApproved")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (approveDate <= employee._TMApproveDate)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PeriodAlreadyApproved"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                else if (employee._TMApproveDate < JournalLineDate.AddDays(-1))
                {
                    if (emptyApproveDate)
                        UnicontaMessageBox.Show(string.Format("Please set Employee hired date or Calendar start date to be the first Time registration date."), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    else
                        UnicontaMessageBox.Show(string.Format("Please Approve previous period(s) first - current Approve date '{0}'", employee._TMApproveDate.ToShortDateString()), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                #endregion

                var tmLinesTime = dgTMJournalLineGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;
                var tmLinesMileage = dgTMJournalLineTransRegGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;
                var lstInsert = new List<ProjectJournalLineClient>((tmLinesMileage.Count() + tmLinesTime.Count()) * 5);

                bool emptyJournal = false;
                if (tmLinesTime.Count() == 0 && tmLinesMileage.Count() == 0)
                    emptyJournal = true;

                if (!emptyJournal)
                {
                    var companySettings = await api.Query<CompanySettingsClient>();
                    var tmVoucher = companySettings[0]._TMJournalVoucherSerie;
                    var firstDay = employee._TMApproveDate >= JournalLineDate ? (employee._TMApproveDate.AddDays(2) - JournalLineDate).Days : 1;
                    var comp = api.CompanyEntity;

                    #region Hours
                    if (projCache == null)
                        projCache = api.GetCache<Uniconta.ClientTools.DataModel.ProjectClient>() ?? await api.LoadCache<Uniconta.ClientTools.DataModel.ProjectClient>();

                    var appDate = employee._TMApproveDate;
                    var startDate = appDate >= JournalLineDate ? appDate.AddDays(1) : JournalLineDate;
                    var endDate = JournalLineDate.AddDays(6);
                    tmHelper.SetEmplPrice(tmLinesTime,
                                            empPriceLst,
                                            payrollCache,
                                            projCache,
                                            startDate,
                                            endDate,
                                            employee);

                    foreach (var rec in tmLinesTime)
                    {
                        var days = (approveDate - rec.Date).Days + 1;
                        var proj = rec.ProjectRef;

                        for (int x = firstDay; x <= days; x++)
                        {
                            var qty = rec.GetHoursDayN(x);
                            if (qty != 0)
                            {
                                var lineclient = new ProjectJournalLineClient();

                                lineclient._Approved = true;
                                lineclient._Text = rec._Text;
                                lineclient._Unit = Uniconta.DataModel.ItemUnit.Hours;
                                lineclient._TransType = rec._TransType;
                                lineclient._Project = rec._Project;
                                lineclient._PayrollCategory = rec._PayrollCategory;
                                lineclient._Task = rec._Task;

                                var payrollCat = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(rec._PayrollCategory);

                                if (payrollCat._InternalType == Uniconta.DataModel.InternalType.OverTime || payrollCat._InternalType == Uniconta.DataModel.InternalType.FlexTime)
                                    lineclient._Qty = payrollCat._Factor == 0 ? qty : qty * payrollCat._Factor;
                                else
                                    lineclient._Qty = qty;

                                lineclient._PrCategory = payrollCat._PrCategory;
                                lineclient._Employee = rec._Employee;

                                if (comp._DimFromProject)
                                {
                                    lineclient._Dim1 = proj._Dim1;
                                    lineclient._Dim2 = proj._Dim2;
                                    lineclient._Dim3 = proj._Dim3;
                                    lineclient._Dim4 = proj._Dim4;
                                    lineclient._Dim5 = proj._Dim5;
                                }
                                else
                                {
                                    lineclient._Dim1 = employee._Dim1;
                                    lineclient._Dim2 = employee._Dim2;
                                    lineclient._Dim3 = employee._Dim3;
                                    lineclient._Dim4 = employee._Dim4;
                                    lineclient._Dim5 = employee._Dim5;
                                }

                                lineclient._Invoiceable = rec._Invoiceable;
                                lineclient._Date = rec._Date.AddDays(x - 1);

                                var price = rec.GetPricesDayN(x);
                                lineclient._SalesPrice = price.Item1;
                                lineclient._CostPrice = price.Item2;

                                lstInsert.Add(lineclient);
                            }
                        }
                    }
                    #endregion

                    #region Mileage

                    var purposeTxt = Uniconta.ClientTools.Localization.lookup("Purpose");
                    var fromTxt = Uniconta.ClientTools.Localization.lookup("From");
                    var toTxt = Uniconta.ClientTools.Localization.lookup("To");
                    var vechicleRegNoTxt = Uniconta.ClientTools.Localization.lookup("VechicleRegNo");

                    tmHelper.SetEmplPrice(tmLinesMileage,
                                            empPriceLst,
                                            payrollCache,
                                            projCache,
                                            startDate,
                                            endDate,
                                            employee);

                    foreach (var rec in tmLinesMileage)
                    {
                        var days = (approveDate - rec.Date).Days + 1;

                        for (int x = firstDay; x <= days; x++)
                        {
                            var qty = rec.GetHoursDayN(x);
                            if (qty != 0)
                            {
                                var lineclient = new ProjectJournalLineClient();

                                lineclient._Approved = true;
                                lineclient._Text = TMJournalLineClient.GetMileageFormattedText(rec._Text, rec._AddressFrom, rec._AddressTo, rec._VechicleRegNo);
                                lineclient._Unit = Uniconta.DataModel.ItemUnit.km;
                                lineclient._TransType = rec._TransType;
                                lineclient._Project = rec._Project;
                                lineclient._PayrollCategory = rec._PayrollCategory;

                                var payrollCat = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(rec._PayrollCategory);

                                lineclient._PrCategory = payrollCat._PrCategory;
                                lineclient._Employee = rec._Employee;

                                lineclient._Dim1 = employee._Dim1;
                                lineclient._Dim2 = employee._Dim2;
                                lineclient._Dim3 = employee._Dim3;
                                lineclient._Dim4 = employee._Dim4;
                                lineclient._Dim5 = employee._Dim5;

                                lineclient._Qty = qty;
                                lineclient._Invoiceable = rec._Invoiceable;
                                lineclient._Date = rec._Date.AddDays(x - 1);

                                var price = rec.GetPricesDayN(x);
                                lineclient._SalesPrice = price.Item1;
                                lineclient._CostPrice = price.Item2;

                                lstInsert.Add(lineclient);
                            }
                        }
                    }
                    #endregion
                }
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                busyIndicator.IsBusy = true;
                if (savetask != null)
                    await savetask;

                if (postingDialog.IsSimulation)
                {
                    AddDockItem(TabControls.SimulatedPrJournalLinePage, lstInsert.ToArray(), Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                }
                else
                {
                    if (lstInsert.Count != 0)
                    {
                        var postingResult = await postingApi.PostEmpJournal((Uniconta.DataModel.Employee)employee, lstInsert, approveDate, postingDialog.comments, false, new GLTransClientTotal());

                        busyIndicator.IsBusy = false;
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                        if (postingResult == null)
                            return;

                        if (postingResult.Err != ErrorCodes.Succes)
                        {
                            Utility.ShowJournalError(postingResult, dgTMJournalLineGrid);
                        }
                        else
                        {
                            employee._TMApproveDate = approveDate;
                            SetButtons();
                            BindGrid();
                            string msg;
                            if (postingResult.JournalPostedlId != 0)
                                msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), postingResult.JournalPostedlId);
                            else
                                msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                            UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                            GetDateStatusOnCalender(employee);
                        }
                    }
                    else
                    {
                        var res = await postingApi.EmployeeSetDates(employee._Number, DateTime.MinValue, approveDate);

                        busyIndicator.IsBusy = false;
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                        if (res != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(res);
                        else
                        {
                            employee._TMApproveDate = approveDate;
                            await Filter(tmJournalLineFilter);
                            await FilterTransRegGrid(tmJournalLineTransReg);
                            SetButtons();
                            GetDateStatusOnCalender(employee);
                        }
                    }
                }
            };
            postingDialog.Show();
#endif
        }

        private void ActionCloseOpen(TMJournalActionType actionType)
        {
#if !SILVERLIGHT
            var cwDate = new CwSetPeriodPerDate(actionType, JournalLineDate, api);
            cwDate.DialogTableId = 2000000055;

            cwDate.Closing += async delegate
            {
                if (cwDate.DialogResult == true)
                {
                    if (cwDate.StartDate == DateTime.MinValue)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Date")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await api.Read(employee);

                    bool emptyCloseDate = false;
                    DateTime closeDate = DateTime.MinValue;
                    DateTime approveDate = DateTime.MinValue;

                    var calendarStartDate = DateTime.MinValue;
                    TMEmpCalendarSetupClient calendar;
                    if (employee._TMCloseDate == DateTime.MinValue || employee._TMApproveDate == DateTime.MinValue)
                    {
                        calendar = calenderLst?.Where(s => s._Employee == employee._Number).OrderBy(s => s.ValidFrom).FirstOrDefault();
                        if (calendar == null)
                        {
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoCalendar"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        calendarStartDate = calendar.ValidFrom;
                    }

                    if (cwDate.StartDate < JournalLineDate || cwDate.StartDate > JournalLineDate.AddDays(6))
                    {
                        UnicontaMessageBox.Show(string.Format("'{0}' only allowed in current Period", actionType), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (employee._TMCloseDate == DateTime.MinValue)
                    {
                        closeDate = employee._Hired != DateTime.MinValue && employee._Hired.AddDays(-1) > calendarStartDate ?
                                    employee._Hired.AddDays(-1) : calendarStartDate == DateTime.MinValue ? calendarStartDate : calendarStartDate.AddDays(-1);

                        emptyCloseDate = true;
                    }

                    //Assign approve date
                    if (employee._TMApproveDate == DateTime.MinValue)
                        approveDate = employee._Hired != DateTime.MinValue && employee._Hired.AddDays(-1) > calendarStartDate ?
                                      employee._Hired.AddDays(-1) : calendarStartDate == DateTime.MinValue ? calendarStartDate : calendarStartDate.AddDays(-1);


                    switch (actionType)
                    {
                        case TMJournalActionType.Close:
                            if (cwDate.StartDate <= employee._TMCloseDate)
                            {
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PeriodAlreadyClosed"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            else if (employee._TMCloseDate < JournalLineDate.AddDays(-1))
                            {
                                if (emptyCloseDate)
                                    UnicontaMessageBox.Show(string.Format("Please set Employee hired date or Calendar start date to be the first Time registration date."), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                                else
                                    UnicontaMessageBox.Show(string.Format("Please Close previous period(s) first - current Close date '{0}'", employee._TMCloseDate.ToShortDateString()), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            else
                            {
                                if (ValidateJournal(actionType, false, cwDate.StartDate))
                                    closeDate = cwDate.StartDate;
                            }
                            break;

                        case TMJournalActionType.Open:
                            if (cwDate.StartDate <= employee._TMApproveDate)
                            {
                                UnicontaMessageBox.Show(string.Format("Period can't be reopened, because it has been Approved"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            else if (cwDate.StartDate > employee._TMCloseDate)
                            {
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("PeriodAlreadyOpen"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            else if (employee._TMCloseDate > JournalLineDate.AddDays(6))
                            {
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("Period can't be reopened, because next period is closed"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                            else
                            {
                                closeDate = cwDate.StartDate.AddDays(-1);
                            }
                            break;
                    }

                    if (closeDate != DateTime.MinValue || approveDate != DateTime.MinValue)
                    {
                        var res = await postingApi.EmployeeSetDates(employee._Number, closeDate, approveDate);
                        if (res != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(res);
                        else
                        {
                            if (closeDate != DateTime.MinValue)
                                employee._TMCloseDate = closeDate;
                            if (approveDate != DateTime.MinValue)
                                employee._TMApproveDate = approveDate;

                            saveGrid();
                            Filter(tmJournalLineFilter);
                            FilterTransRegGrid(tmJournalLineTransReg);
                            SetButtons();
                            GetDateStatusOnCalender(employee);
                        }
                    }
                }

            };
            cwDate.Show();
#endif
        }

        private bool ValidateJournal(TMJournalActionType actionType, bool showMsgOK = true, DateTime dialogDate = default(DateTime))
        {
            bool ret = true;
#if !SILVERLIGHT
            if (employee._TMApproveDate >= JournalLineDate.AddDays(6))
                return false;

            saveGrid();

            double valRegHours = 0;
            double valNormHours1 = 0, valNormHours2 = 0, valNormHours3 = 0, valNormHours4 = 0, valNormHours5 = 0, valNormHours6 = 0, valNormHours7 = 0, valNormHoursTotal = 0;

            double daysCnt = 0;
            double normDaysCnt = 0;

            DateTime startDateWeek = DateTime.MinValue, startDateClose = DateTime.MinValue, endDateClose = DateTime.MinValue;

            if (actionType == TMJournalActionType.Close && dialogDate < JournalLineDate.AddDays(6))
            {
                var tmLinesHours = dgTMJournalLineGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;

                var approveDate = employee._TMApproveDate;
                var closeDate = dialogDate;
                startDateWeek = JournalLineDate;
                startDateClose = approveDate >= startDateWeek ? approveDate.AddDays(1) : startDateWeek;
                endDateClose = closeDate >= startDateWeek.AddDays(6) ? startDateWeek.AddDays(6) : closeDate;

                daysCnt = (endDateClose - startDateClose).TotalDays + 1;

                foreach (var line in tmLinesHours)
                {
                    if (line.Date >= startDateClose && line.Date <= endDateClose)
                    {
                        valRegHours += line.Day1;
                        valNormHours1 = normHoursDay1;
                    }
                    if (line.Date.AddDays(1) >= startDateClose && line.Date.AddDays(1) <= endDateClose)
                    {
                        valRegHours += line.Day2;
                        valNormHours2 = normHoursDay2;
                    }
                    if (line.Date.AddDays(2) >= startDateClose && line.Date.AddDays(2) <= endDateClose)
                    {
                        valRegHours += line.Day3;
                        valNormHours3 = normHoursDay3;
                    }
                    if (line.Date.AddDays(3) >= startDateClose && line.Date.AddDays(3) <= endDateClose)
                    {
                        valRegHours += line.Day4;
                        valNormHours4 = normHoursDay4;
                    }
                    if (line.Date.AddDays(4) >= startDateClose && line.Date.AddDays(4) <= endDateClose)
                    {
                        valRegHours += line.Day5;
                        valNormHours5 = normHoursDay5;
                    }
                    if (line.Date.AddDays(5) >= startDateClose && line.Date.AddDays(5) <= endDateClose)
                    {
                        valRegHours += line.Day6;
                        valNormHours6 = normHoursDay6;
                    }
                    if (line.Date.AddDays(6) >= startDateClose && line.Date.AddDays(6) <= endDateClose)
                    {
                        valRegHours += line.Day7;
                        valNormHours7 = normHoursDay7;
                    }
                }
                valNormHoursTotal = valNormHours1 + valNormHours2 + valNormHours3 + valNormHours4 + valNormHours5 + valNormHours6 + valNormHours7;
            }
            else
            {
                valRegHours = totalSum;
                valNormHoursTotal = normHoursTotal;

                startDateWeek = employee._TMApproveDate >= JournalLineDate ? employee._TMApproveDate.AddDays(1) : JournalLineDate;
                startDateClose = startDateWeek;
                var closeDate = JournalLineDate.AddDays(6);
                endDateClose = closeDate >= startDateWeek.AddDays(6) ? startDateWeek.AddDays(6) : closeDate;
                daysCnt = (JournalLineDate.AddDays(6) - startDateWeek).TotalDays + 1;
            }

            var preValidateRes = tmHelper.PreValidate(actionType, valNormHoursTotal, valRegHours, payrollCache);

            var lastDay = JournalLineDate.AddDays(6).Date;
            for (DateTime date = startDateWeek; date.Date <= lastDay; date = date.AddDays(1))
            {
                if (dictNormDayAmount.ContainsKey(date))
                    normDaysCnt++;
            }

            if (daysCnt > normDaysCnt)
            {
                preValidateRes.Add(new TMJournalLineError()
                {
                    Message = Uniconta.ClientTools.Localization.lookup("CalendarDaysMissing"),
                });
            }

            if (preValidateRes != null && preValidateRes.Count > 0)
            {
                var preErrors = new List<string>();
                foreach (var error in preValidateRes)
                {
                    preErrors.Add(error.Message);
                }
                CWErrorBox cwError = new CWErrorBox(preErrors.ToArray());
                cwError.Show();
                ret = false;
            }
            else
            {
                // Validate Hours >>
                dgTMJournalLineGrid.Columns.GetColumnByName("ErrorInfo").Visible = true;

                var tmLinesHours = dgTMJournalLineGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;

                var valErrorsHours = tmHelper.ValidateLinesHours(tmLinesHours,
                                                                 JournalLineDate,
                                                                 JournalLineDate.AddDays(6),
                                                                 empPriceLst,
                                                                 payrollCache,
                                                                 projCache,
                                                                 projGroupCache,
                                                                 employee);

                var cntErrLinesTime = tmLinesHours.Where(s => s.ErrorInfo != TMJournalLineHelper.VALIDATE_OK).Count();

                foreach (TMJournalLineError error in valErrorsHours)
                {
                    var rec = tmLinesHours.Where(s => s.RowId == error.RowId).FirstOrDefault();
                    if (rec != null)
                    {
                        rec.ErrorInfo += rec.ErrorInfo != string.Empty ? "\n" + error.Message : error.Message;
                        ret = false;
                    }
                }

                //Insert Difference hours line
                if ((actionType == TMJournalActionType.Validate || actionType == TMJournalActionType.Close) && valNormHoursTotal != 0)
                {
                    if (JournalLineDate.AddDays(6) > employee._TMCloseDate && ret == true && valRegHours - valNormHoursTotal > 0 && (lstCatOverTime != null || lstCatFlexTime != null))
                    {
                        var lastIdx = tmLinesHours.Count();

                        var payrollCategory = payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.OverTime).OrderBy(s => s._Number).FirstOrDefault();
                        payrollCategory = payrollCategory == null ? payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.FlexTime).OrderBy(s => s._Number).FirstOrDefault() : payrollCategory;

                        TMJournalLineClientLocal journalLine = new TMJournalLineClientLocal();
                        journalLine.SetMaster(api.CompanyEntity);
                        journalLine._Project = payrollCategory._InternalProject;
                        journalLine._Date = JournalLineDate;
                        journalLine._Invoiceable = false;
                        journalLine._RegistrationType = Uniconta.DataModel.RegistrationType.Hours;
                        journalLine._PayrollCategory = payrollCategory.KeyStr;

                        if (JournalLineDate >= startDateClose && JournalLineDate <= endDateClose)
                            journalLine._Day1 = JournalLineDate.AddDays(0) > employee._TMCloseDate ? normHoursDay1 - day1Sum : 0;
                        if (JournalLineDate.AddDays(1) >= startDateClose && JournalLineDate.AddDays(1) <= endDateClose)
                            journalLine._Day2 = JournalLineDate.AddDays(1) > employee._TMCloseDate ? normHoursDay2 - day2Sum : 0;
                        if (JournalLineDate.AddDays(2) >= startDateClose && JournalLineDate.AddDays(2) <= endDateClose)
                            journalLine._Day3 = JournalLineDate.AddDays(2) > employee._TMCloseDate ? normHoursDay3 - day3Sum : 0;
                        if (JournalLineDate.AddDays(3) >= startDateClose && JournalLineDate.AddDays(3) <= endDateClose)
                            journalLine._Day4 = JournalLineDate.AddDays(3) > employee._TMCloseDate ? normHoursDay4 - day4Sum : 0;
                        if (JournalLineDate.AddDays(4) >= startDateClose && JournalLineDate.AddDays(4) <= endDateClose)
                            journalLine._Day5 = JournalLineDate.AddDays(4) > employee._TMCloseDate ? normHoursDay5 - day5Sum : 0;
                        if (JournalLineDate.AddDays(5) >= startDateClose && JournalLineDate.AddDays(5) <= endDateClose)
                            journalLine._Day6 = JournalLineDate.AddDays(5) > employee._TMCloseDate ? normHoursDay6 - day6Sum : 0;
                        if (JournalLineDate.AddDays(6) >= startDateClose && JournalLineDate.AddDays(6) <= endDateClose)
                            journalLine._Day7 = JournalLineDate.AddDays(6) > employee._TMCloseDate ? normHoursDay7 - day7Sum : 0;
                        dgTMJournalLineGrid.AddRow(journalLine, lastIdx);
                    }
                }
                // Validate Hours <<

                // Validate Mileage >>
                dgTMJournalLineTransRegGrid.Columns.GetColumnByName("colErrorInfo").Visible = true;

                var tmLinesMileage = dgTMJournalLineTransRegGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;

                var valErrorsMileage = tmHelper.ValidateLinesMileage(tmLinesMileage,
                                                                     JournalLineDate,
                                                                     JournalLineDate.AddDays(6),
                                                                     empPriceLst,
                                                                     payrollCache,
                                                                     projCache,
                                                                     projGroupCache,
                                                                     employee);

                var cntErrLinesMileage = tmLinesMileage.Where(s => s.ErrorInfo != TMJournalLineHelper.VALIDATE_OK).Count();

                foreach (TMJournalLineError error in valErrorsMileage)
                {
                    var rec = tmLinesMileage.Where(s => s.RowId == error.RowId).FirstOrDefault();
                    if (rec != null)
                    {
                        rec.ErrorInfo += rec.ErrorInfo != string.Empty ? "\n" + error.Message : error.Message;
                        ret = false;
                    }
                }
                // Validate Mileage <<

                if (cntErrLinesTime != 0 || cntErrLinesMileage != 0)
                {
                    string errText = null;

                    if (cntErrLinesTime != 0)
                    {
                        errText = cntErrLinesMileage != 0 ? string.Format("({0}) ", Uniconta.ClientTools.Localization.lookup("Hours")) : string.Empty;
                        errText += string.Format("{0} {1}", cntErrLinesTime, Uniconta.ClientTools.Localization.lookup("JournalFailedValidation"));
                    }

                    if (cntErrLinesMileage != 0)
                    {
                        errText += errText != null ? Environment.NewLine + Environment.NewLine : string.Empty;
                        errText += string.Format("({0}) {1} {2}", Uniconta.ClientTools.Localization.lookup("Mileage"), cntErrLinesMileage, Uniconta.ClientTools.Localization.lookup("JournalFailedValidation"));

                        if (layOutInvItemStorage.Visibility == Visibility.Collapsed)
                        {
                            layOutInvItemStorage.Visibility = Visibility.Visible;
                            ShowHideMileage();
                        }
                    }

                    UnicontaMessageBox.Show(errText, Uniconta.ClientTools.Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else if (showMsgOK)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateNoError"), Uniconta.ClientTools.Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
#endif
            return ret;
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

        private void SetProjectSource(TMJournalLineClientLocal rec)
        {
            if (projCache != null && projCache.Count != 0)
            {
                rec._projectSource = new TMJournalProjectFilter(projCache.cache);
                if (rec._projectSource != null)
                    rec.NotifyPropertyChanged("ProjectSource");
            }
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

        private void SetPayrollSource(TMJournalLineClientLocal rec, bool isHoursPayroll = true)
        {
            if (payrollCache != null && payrollCache.Count != 0)
            {
                var pg = rec.ProjectRef?.ProjectGroup;
                if (pg != null)
                {
                    rec._payrollSource = new TMJournalPayrollFilter(payrollCache.cache, pg._Invoiceable, isHoursPayroll);
                    if (rec._payrollSource != null)
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
                PropValuePair.GenereteWhereElements("RegistrationType", typeof(int), "0")
            };
            tmJournalLineTransReg = new PropValuePair[]
            {
                 PropValuePair.GenereteWhereElements("Date", JournalLineDate, CompareOperator.Equal),
                 PropValuePair.GenereteWhereElements("RegistrationType", typeof(int), "1")
            };
            SetColumnHeader();
            await BindGrid();

            if (copyLines)
                CopyLines();
        }

        protected override async void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;
            if (projCache == null)
                projCache = await api.LoadCache<Uniconta.ClientTools.DataModel.ProjectClient>().ConfigureAwait(false);
            if (payrollCache == null)
                payrollCache = await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>().ConfigureAwait(false);
            if (projGroupCache == null)
                projGroupCache = await api.LoadCache<Uniconta.DataModel.ProjectGroup>().ConfigureAwait(false);

            LoadType(new Type[] { typeof(Uniconta.DataModel.TMEmpCalendar), typeof(Uniconta.DataModel.EmpPayrollCategory), typeof(Uniconta.DataModel.Debtor) });
        }

        protected async override Task<ErrorCodes> saveGrid()
        {
            if (dgTMJournalLineGrid.HasUnsavedData || dgTMJournalLineTransRegGrid.HasUnsavedData)
                anyChange = true;
            await dgTMJournalLineTransRegGrid.SaveData();
            return await dgTMJournalLineGrid.SaveData();
        }
    }
}
