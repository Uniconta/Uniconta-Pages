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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TMJournalLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TMJournalLineClientLocal); } }
        public override bool Readonly { get { return false; } }
        public DateTime JnlLineDate { get; set; }
        public string RegType { get; set; }
        public Uniconta.DataModel.Employee Employee { get; set; }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (TMJournalLineClientLocal)this.SelectedItem;
            if ((selectedItem != null && (JnlLineDate.AddDays(6) <= Employee._TMApproveDate || JnlLineDate.AddDays(6) <= Employee._TMCloseDate)) || selectedItem == null || selectedItem._Project == null || selectedItem._PayrollCategory == null)
                return false;
            return true; ;
        }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var header = (Uniconta.DataModel.Employee)this.masterRecord;
            var newRow = (TMJournalLineClientLocal)dataEntity;
            newRow.Date = JnlLineDate;
            newRow.RegistrationType = RegType;
            newRow.SetMaster(header);
        }

        protected override List<string> GridSkipFields { get { return new List<string>() { "ProjectName", "PayrollCategoryName" }; } }

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
        EmpPayrollCategoryEmployeeClient[] empPriceLst;
        IEnumerable<PropValuePair> tmJournalLineFilter, tmJournalLineTransReg;
        DateTime JournalLineDate;
        Uniconta.DataModel.Employee employee;
        DateTime employeeCalenderStartDate = DateTime.MinValue;
        SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> payrollCache;
        SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupCache;

        double vacationYTD, vacationNotApproved, otherVacationYTD, otherVacationNotApproved, overTimeYTD, overTimeNotApproved, flexTimeYTD, flexTimeNotApproved, mileageYTD, mileageNotApproved;
        bool clearMileageList, clearHoursList;
        
        UnicontaAPI.Project.API.PostingAPI postingApi;

        public TMJournalLinePage(UnicontaBaseEntity master, DateTime startDate) : base(master)
        {
            employeeCalenderStartDate = startDate;
            InitPage(master);
        }

        public TMJournalLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            ((TableView)dgTMJournalLineTransRegGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            ((TableView)dgTMJournalLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            postingApi = new UnicontaAPI.Project.API.PostingAPI(api);
            dgTMJournalLineGrid.UpdateMaster(master);
            dgTMJournalLineTransRegGrid.UpdateMaster(master);
            localMenu.dataGrid = dgTMJournalLineGrid;
            SetRibbonControl(localMenu, dgTMJournalLineGrid);
            dgTMJournalLineGrid.api = api;
            dgTMJournalLineTransRegGrid.api = api;
            dgTMJournalLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            employee = master as Uniconta.DataModel.Employee;
            GetDateStatusOnCalender(employee);
            tmHelper = new TMJournalLineHelper(api, employee);
            StartLoadCache();
            SetFields(employeeCalenderStartDate == DateTime.MinValue ? DateTime.Today : employeeCalenderStartDate);
            SetButtons();
            LoadEmpPrices();
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
        }

        void GetDateStatusOnCalender(Uniconta.DataModel.Employee employee)
        {
#if !SILVERLIGHT
            var emplCalStart = DateTime.Today.AddYears(-1);

            var specialDates = new ObservableCollection<MySpecialDate>();
            for (DateTime date = emplCalStart; date.Date <= employee._TMApproveDate; date = date.AddDays(1))
                specialDates.Add(new MySpecialDate { Date = date, Color = Brushes.Green });
            for (DateTime date = employee._TMApproveDate.AddDays(1); date.Date <= employee._TMCloseDate; date = date.AddDays(1))
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
            switch (e.PropertyName)
            {
                case "PayrollCategory":
                    SetInvoiceable(rec);
                    clearMileageList = true;
                    RecalculateWeekInternalMileage();
                    break;
                case "Project":
                case "Day1":
                case "Day2":
                case "Day3":
                case "Day4":
                case "Day5":
                case "Day6":
                case "Day7":
                    if (rec._InternalType == Uniconta.DataModel.InternalType.Mileage)
                    {
                        clearMileageList = true;
                        RecalculateWeekInternalMileage();
                    }
                    break;
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

            var dayStatLst = new List<DayStatus>();
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
                    return statusCloselst.Count() == 0 && statusApprovelst.Count() == 0 ? true : false;
                case "NewLine":
                    return 7 - statusCloselst.Count() > 0 && 7 - statusApprovelst.Count() > 0 ? true : false;
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
                    if (customItem.SerializableTag == "TotalDay1")
                    {
                        Style style = this.FindResource("SummaryTotalStyle") as Style;
                        customItem.TotalSummaryElementStyle = style;
                    }
                    if (customItem.SerializableTag == "TotalDay2")
                    {
                        Style style = this.FindResource("SummaryTotalStyle") as Style;
                        customItem.TotalSummaryElementStyle = style;
                    }
                    if (customItem.SerializableTag == "TotalDay3")
                    {
                        Style style = this.FindResource("SummaryTotalStyle") as Style;
                        customItem.TotalSummaryElementStyle = style;
                    }
                    if (customItem.SerializableTag == "TotalDay4")
                    {
                        Style style = this.FindResource("SummaryTotalStyle") as Style;
                        customItem.TotalSummaryElementStyle = style;
                    }
                    if (customItem.SerializableTag == "TotalDay5")
                    {
                        Style style = this.FindResource("SummaryTotalStyle") as Style;
                        customItem.TotalSummaryElementStyle = style;
                    }
                    if (customItem.SerializableTag == "TotalDay6")
                    {
                        Style style = this.FindResource("SummaryTotalStyle") as Style;
                        customItem.TotalSummaryElementStyle = style;
                    }
                    if (customItem.SerializableTag == "TotalDay7")
                    {
                        Style style = this.FindResource("SummaryTotalStyle") as Style;
                        customItem.TotalSummaryElementStyle = style;
                    }
                    if (customItem.SerializableTag == "TotalSum")
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
            switch (e.PropertyName)
            {
                case "PayrollCategory":
                    SetInvoiceable(rec);
                    rec.NotifyPropertyChanged("IsMatched");
                    if (rec._InternalType != Uniconta.DataModel.InternalType.None)
                    {
                        clearHoursList = true;
                        RecalculateWeekInternalHour();
                    }
                    break;
                case "Project":
                case "Day1":
                case "Day2":
                case "Day3":
                case "Day4":
                case "Day5":
                case "Day6":
                case "Day7":
                    if (rec._InternalType != Uniconta.DataModel.InternalType.None)
                    {
                        clearHoursList = true;
                        RecalculateWeekInternalHour();
                    }
                    RecalculateEfficiencyPercentage();
                    break;
            }

            dgTMJournalLineGrid.UpdateTotalSummary();
        }

        void SetFields(DateTime selectedDate)
        {
            cmbRegistration.ItemsSource = Uniconta.ClientTools.AppEnums.RegistrationType.Values;
            cmbRegistration.SelectedIndex = 0;
            JournalLineDate = FirstDayOfWeek(selectedDate);
            txtDateTo.DateTime = JournalLineDate;
            tmJournalLineFilter = new List<PropValuePair>()
            {
                PropValuePair.GenereteWhereElements("Date", typeof(DateTime), JournalLineDate.ToString()),
                PropValuePair.GenereteWhereElements("RegistrationType", typeof(int), "0"),
            };
            tmJournalLineTransReg = new List<PropValuePair>()
            {
                 PropValuePair.GenereteWhereElements("Date", typeof(DateTime), JournalLineDate.ToString()),
                 PropValuePair.GenereteWhereElements("RegistrationType", typeof(int), "1")
            };
            SetColumnHeader();
        }

        TMApprovalSetupClient[] approverLst;
        async void SetButtons()
        {
            if (payrollCache == null)
                payrollCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>() ?? await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>();

            if (projGroupCache == null)
                projGroupCache = api.GetCache<Uniconta.DataModel.ProjectGroup>() ?? await api.LoadCache<Uniconta.DataModel.ProjectGroup>();

            EnableMileageRegistration();

            var emps = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Employee));
            var curEmployee = (from rec in (Uniconta.DataModel.Employee[])emps.GetNotNullArray where rec._Uid == BasePage.session.Uid select rec).FirstOrDefault();

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

            List<PropValuePair> filter;
            if (approverLst == null)
            {
                filter = new List<PropValuePair>()
                {
                    PropValuePair.GenereteTakeN(1),
                    PropValuePair.GenereteOrderByElement("RowId", true)
                };
                approverLst = await api.Query<TMApprovalSetupClient>(filter);

                if (approverLst == null || approverLst.Count() == 0)
                {
                    ribbonControl.EnableButtons("Approve");
                    return;
                }
            }

            if (curEmployee == null)
            {
                ribbonControl.DisableButtons("Approve");
                return;
            }

            filter = new List<PropValuePair>()
            {
                PropValuePair.GenereteWhereElements(nameof(TMApprovalSetupClient.Approver), typeof(string), curEmployee.KeyStr)
            };
            approverLst = await api.Query<TMApprovalSetupClient>(filter);

            if (approverLst?.Where(x => ((x.Employee == employee._Number || (x._Employee == null && x._EmployeeGroup != null)) && (x._EmployeeGroup == employee._Group || (x._EmployeeGroup == null && x.Employee != null)) ||
                                         (x._EmployeeGroup == null && x.Employee == null)) && (x.ValidFrom <= JournalLineDate && (x.ValidTo == DateTime.MinValue || x.ValidTo >= JournalLineDate))).Count() == 0)
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

            var gridHours = (IEnumerable<TMJournalLineClient>)dgTMJournalLineGrid.ItemsSource;
            if (gridHours.Count() != 0)
                return;

            var pairJournalTrans = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), employee.KeyStr),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), typeof(DateTime), String.Format("{0:d}", JournalLineDate.AddDays(-7))),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.RegistrationType), typeof(int), 0),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.CopyLine), typeof(bool), "true")
            };

            var copyPrevJournalLine = await api.Query<TMJournalLineClientLocal>(pairJournalTrans);

            if (copyPrevJournalLine == null || copyPrevJournalLine.Count() == 0)
                return;

            TMJournalLineClientLocal journalLine = new TMJournalLineClientLocal();

            foreach (var rec in copyPrevJournalLine)
            {
                journalLine._Employee = rec._Employee;
                journalLine._Date = JournalLineDate;
                journalLine._Project = rec._Project;
                journalLine._Invoiceable = rec._Invoiceable;
                journalLine.PayrollCategory = rec._PayrollCategory;
                journalLine._CopyLine = rec._CopyLine;
                journalLine._Text = rec._Text;
                journalLine._Task = rec._Task;
                dgTMJournalLineGrid.AddRow(journalLine);
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
            return dt.AddDays(-1 * diff).Date;
        }

        void SetInvoiceable(TMJournalLineClientLocal rec)
        {
            var payroll = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(rec.PayrollCategory);
            if (payroll != null)
                rec.Invoiceable = payroll._Invoiceable;
        }

        ProjectTransClient[] internalTransLst;
        TMJournalLineClient[] journalLineNotApprovedLst;
        DateTime approvedCutOffDate;
        async void InitializeStatusText() 
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;

            if ((internalTransLst == null || journalLineNotApprovedLst == null) && payrollCache != null && payrollCache.Count() != 0)
            {
                approvedCutOffDate = employee._TMApproveDate == DateTime.MinValue ? DateTime.MinValue :
                                     employee._TMApproveDate.DayOfWeek == DayOfWeek.Sunday ? employee._TMApproveDate.AddDays(1) :
                                     employee._TMApproveDate.DayOfWeek == DayOfWeek.Monday ? employee._TMApproveDate :
                                     FirstDayOfWeek(employee._TMApproveDate);

                var sbInternalProj = new StringBuilder();
                var sbPayrollCat = new StringBuilder();
                foreach (var val in payrollCache.Where(s => (s._InternalType == Uniconta.DataModel.InternalType.Vacation ||
                                                                  s._InternalType == Uniconta.DataModel.InternalType.OtherVacation ||
                                                                  s._InternalType == Uniconta.DataModel.InternalType.OverTime ||
                                                                  s._InternalType == Uniconta.DataModel.InternalType.FlexTime ||
                                                                  s._InternalType == Uniconta.DataModel.InternalType.Mileage)))
                {
                    sbInternalProj.Append(val._InternalProject).Append(";");
                    sbPayrollCat.Append(val._Number).Append(";");
                }

                if (internalTransLst == null)
                {
                    var pairInternalTrans = new PropValuePair[]  //We query all ProjTrans
                    {
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Employee), typeof(string), employee.KeyStr),
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Project), typeof(string), sbInternalProj.ToString()),
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.PayrollCategory), typeof(int), sbPayrollCat.ToString()),
                    };

                    internalTransLst = await api.Query<ProjectTransClient>(pairInternalTrans);
                }

                if (journalLineNotApprovedLst == null)
                {
                    var pairJournalTrans = new PropValuePair[]
                    {
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), employee.KeyStr),
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Project), typeof(string), sbInternalProj.ToString()),
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.PayrollCategory), typeof(int), sbPayrollCat.ToString()),
                        PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), typeof(DateTime), String.Format("{0:d}..", approvedCutOffDate))
                    };

                    journalLineNotApprovedLst = await api.Query<TMJournalLineClient>(pairJournalTrans);
                }
            }


            #region Vacation
            vacationYTD = 0;
            vacationNotApproved = 0;

            if (payrollCache.Any(s => s._InternalType == Uniconta.DataModel.InternalType.Vacation) == false)
                UtilDisplay.RemoveMenuCommand(rb, "VacationBal");
            else
            {
                if (internalTransLst != null || journalLineNotApprovedLst != null)
                {
                    var vacationStartDate = JournalLineDate < new DateTime(JournalLineDate.Year, 5, 1) ? new DateTime(JournalLineDate.Year - 1, 5, 1) : new DateTime(JournalLineDate.Year, 5, 1);

                    if (internalTransLst != null)
                    {
                        var projtransEndDate = approvedCutOffDate == DateTime.MinValue ? vacationStartDate :
                                               approvedCutOffDate.AddDays(-1) < vacationStartDate ? vacationStartDate : approvedCutOffDate.AddDays(-1);

                        if (projtransEndDate > JournalLineDate.AddDays(6))
                            projtransEndDate = JournalLineDate.AddDays(6);
                        else if (JournalLineDate.AddDays(6) >= vacationStartDate.AddYears(1))
                            projtransEndDate = JournalLineDate;

                        var transList = new List<ProjectTransClient>();

                        foreach (var y in payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.Vacation))
                        {
                            transList.AddRange(internalTransLst?.Where(s => s.Project == y._InternalProject && s.PayrollCategory == y._Number && s.Date >= vacationStartDate && s.Date <= projtransEndDate));
                        }
                        if (transList != null)
                            vacationYTD = -1 * transList.Select(x => x.Qty).Sum();
                    }

                    if (journalLineNotApprovedLst != null)
                    {
                        var projtransEndDateAppr = approvedCutOffDate == DateTime.MinValue ? vacationStartDate :
                                               approvedCutOffDate.AddDays(-1) < vacationStartDate ? vacationStartDate : approvedCutOffDate.AddDays(-1);

                        if (projtransEndDateAppr > JournalLineDate.AddDays(6))
                            projtransEndDateAppr = JournalLineDate.AddDays(6);

                        var transList = journalLineNotApprovedLst?.Where(s => s._RegistrationType == Uniconta.DataModel.RegistrationType.Hours &&
                                                                              s.InternalType == AppEnums.InternalType.ToString((int)Uniconta.DataModel.InternalType.Vacation) &&
                                                                              s.Invoiceable == false && s.Date >= projtransEndDateAppr && s.Date < JournalLineDate);
                        if (transList != null)
                            vacationNotApproved = transList.Select(x => x.Total).Sum();
                    }
                }
            }
            #endregion Vacation

            #region Other vacation
            otherVacationYTD = 0;
            otherVacationNotApproved = 0;

            if (payrollCache.Any(s => s._InternalType == Uniconta.DataModel.InternalType.OtherVacation) == false)
                UtilDisplay.RemoveMenuCommand(rb, "OtherVacationBal");
            else
            {
                if (internalTransLst != null || journalLineNotApprovedLst != null)
                {
                    var vacationStartDate = JournalLineDate < new DateTime(JournalLineDate.Year, 5, 1) ? new DateTime(JournalLineDate.Year - 1, 5, 1) : new DateTime(JournalLineDate.Year, 5, 1);

                    if (internalTransLst != null)
                    {
                        var projtransEndDate = approvedCutOffDate == DateTime.MinValue ? vacationStartDate :
                                               approvedCutOffDate.AddDays(-1) < vacationStartDate ? vacationStartDate : approvedCutOffDate.AddDays(-1);

                        if (projtransEndDate > JournalLineDate.AddDays(6))
                            projtransEndDate = JournalLineDate.AddDays(6);
                        else if (JournalLineDate.AddDays(6) >= vacationStartDate.AddYears(1))
                            projtransEndDate = JournalLineDate;

                        var transList = new List<ProjectTransClient>();
                        foreach (var y in payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.OtherVacation))
                        {
                            transList.AddRange(internalTransLst?.Where(s => s.Project == y._InternalProject && s.PayrollCategory == y._Number && s.Date >= vacationStartDate && s.Date <= projtransEndDate));
                        }
                        if (transList != null)
                            otherVacationYTD = -1 * transList.Select(x => x.Qty).Sum();
                    }

                    if (journalLineNotApprovedLst != null)
                    {
                        var projtransEndDateAppr = approvedCutOffDate == DateTime.MinValue ? vacationStartDate :
                                               approvedCutOffDate.AddDays(-1) < vacationStartDate ? vacationStartDate : approvedCutOffDate.AddDays(-1);

                        if (projtransEndDateAppr > JournalLineDate.AddDays(6))
                            projtransEndDateAppr = JournalLineDate.AddDays(6);

                        var transList = journalLineNotApprovedLst?.Where(s => s._RegistrationType == Uniconta.DataModel.RegistrationType.Hours &&
                                                                              s.InternalType == AppEnums.InternalType.ToString((int)Uniconta.DataModel.InternalType.OtherVacation) &&
                                                                              s.Invoiceable == false && s.Date >= projtransEndDateAppr && s.Date < JournalLineDate);
                        if (transList != null)
                            otherVacationNotApproved = transList.Select(x => x.Total).Sum();
                    }
                }
            }
            #endregion Other vacation

            #region Overtime
            overTimeYTD = 0;
            overTimeNotApproved = 0;

            if (payrollCache.Any(s => s._InternalType == Uniconta.DataModel.InternalType.OverTime) == false)
                UtilDisplay.RemoveMenuCommand(rb, "OvertimeBal");
            else
            {
                if (internalTransLst != null || journalLineNotApprovedLst != null)
                {
                    if (internalTransLst != null)
                    {
                        var projtransEndDateOvertime = approvedCutOffDate == DateTime.MinValue ? DateTime.MaxValue : approvedCutOffDate.AddDays(-1);
                        if (projtransEndDateOvertime > JournalLineDate.AddDays(6))
                            projtransEndDateOvertime = JournalLineDate.AddDays(6);

                        var transList = new List<ProjectTransClient>();
                        foreach (var y in payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.OverTime))
                        {
                            transList.AddRange(internalTransLst?.Where(s => s.Project == y._InternalProject && s.PayrollCategory == y._Number && s.Date <= projtransEndDateOvertime));
                        }
                        if (transList != null)
                            overTimeYTD = -1 * transList.Select(x => x.Qty).Sum();
                    }

                    if (journalLineNotApprovedLst != null)
                    {
                        var projtransEndDateOvertimeNotAppr = approvedCutOffDate == DateTime.MinValue ? DateTime.MinValue : approvedCutOffDate.AddDays(-1);

                        var transList = journalLineNotApprovedLst?.Where(s => s._RegistrationType == Uniconta.DataModel.RegistrationType.Hours &&
                                                                              s.InternalType == AppEnums.InternalType.ToString((int)Uniconta.DataModel.InternalType.OverTime) &&
                                                                              s.Invoiceable == false && s.Date >= projtransEndDateOvertimeNotAppr && s.Date < JournalLineDate);
                        if (transList != null)
                            overTimeNotApproved = transList.Select(x => x.Total).Sum();
                    }

                }
            }
            #endregion Overtime

            #region FlexTime
            flexTimeYTD = 0;
            flexTimeNotApproved = 0;

            if (payrollCache.Any(s => s._InternalType == Uniconta.DataModel.InternalType.FlexTime) == false)
                UtilDisplay.RemoveMenuCommand(rb, "FlexTimeBal");
            else
            {
                if (internalTransLst != null || journalLineNotApprovedLst != null)
                {
                    if (internalTransLst != null)
                    {
                        var projtransEndDateFlex = approvedCutOffDate == DateTime.MinValue ? DateTime.MaxValue : approvedCutOffDate.AddDays(-1);
                        if (projtransEndDateFlex > JournalLineDate.AddDays(6))
                            projtransEndDateFlex = JournalLineDate.AddDays(6);

                        var transList = new List<ProjectTransClient>();
                        foreach (var y in payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.FlexTime))
                        {
                            transList.AddRange(internalTransLst?.Where(s => s.Project == y._InternalProject && s.PayrollCategory == y._Number && s.Date <= projtransEndDateFlex));
                        }
                        if (transList != null)
                            flexTimeYTD = -1 * transList.Select(x => x.Qty).Sum();
                    }

                    if (journalLineNotApprovedLst != null)
                    {
                        var projtransEndDateFlexNotAppr = approvedCutOffDate == DateTime.MinValue ? DateTime.MinValue : approvedCutOffDate.AddDays(-1);

                        var transList = journalLineNotApprovedLst?.Where(s => s._RegistrationType == Uniconta.DataModel.RegistrationType.Hours &&
                                                                              s.InternalType == AppEnums.InternalType.ToString((int)Uniconta.DataModel.InternalType.FlexTime) &&
                                                                              s.Invoiceable == false && s.Date >= projtransEndDateFlexNotAppr && s.Date < JournalLineDate);
                        if (transList != null)
                            flexTimeNotApproved = transList.Select(x => x.Total).Sum();
                    }
                }
            }
            #endregion FlexTime

            #region Mileage
            mileageYTD = 0;
            mileageNotApproved = 0;

            if (payrollCache.Any(s => s._InternalType == Uniconta.DataModel.InternalType.Mileage) == false)
                UtilDisplay.RemoveMenuCommand(rb, "MileageHighRateBal");
            else 
            {
                if (internalTransLst != null || journalLineNotApprovedLst != null)
                {
                    var mileageStartDate = new DateTime(JournalLineDate.Year, 1, 1);
                    
                    if (internalTransLst != null)
                    {
                        var projtransMileageEndDate = approvedCutOffDate == DateTime.MinValue ? JournalLineDate.AddDays(6) :
                                                      approvedCutOffDate.AddDays(-1) < mileageStartDate ? mileageStartDate : approvedCutOffDate.AddDays(-1);

                        var startDateNorm = employee._Hired < calendarStartDate ? calendarStartDate : employee._Hired;

                        if (projtransMileageEndDate < startDateNorm)
                            projtransMileageEndDate = startDateNorm;

                        var transList = new List<ProjectTransClient>();
                        foreach (var y in payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.Mileage))
                        {
                            transList.AddRange(internalTransLst?.Where(s => s.Project == y._InternalProject && s.PayrollCategory == y._Number && s.Date >= mileageStartDate && s.Date <= projtransMileageEndDate));
                        }
                        if (transList != null)
                            mileageYTD = transList.Select(x => x.Qty).Sum();
                    }

                    if (journalLineNotApprovedLst != null)
                    {
                        var transMileageEndDate = approvedCutOffDate == DateTime.MinValue ? mileageStartDate :
                                                 approvedCutOffDate.AddDays(-1) < mileageStartDate ? mileageStartDate : approvedCutOffDate.AddDays(-1);

                        var transList = journalLineNotApprovedLst?.Where(s => s._RegistrationType == Uniconta.DataModel.RegistrationType.Mileage &&
                                                                              s.InternalType == AppEnums.InternalType.ToString((int)Uniconta.DataModel.InternalType.Mileage) &&
                                                                              s.Invoiceable == false && s.Date >= transMileageEndDate && s.Date < JournalLineDate);
                        if (transList != null)
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

            if (employee._TMApproveDate < JournalLineDate.AddDays(6))
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
                var grpLstInvoiceable = gridHours?.Where(s => s._InternalType == Uniconta.DataModel.InternalType.None).GroupBy(x => x._Invoiceable).Select(x => new { GroupKey = x.Key, Sum = x.Sum(y => y.Total) });

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
                    foreach (var x in gridMileage.Where(x => x.Invoiceable == false && x.InternalType == AppEnums.InternalType.ToString((int)Uniconta.DataModel.InternalType.Mileage)))
                    {
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
            var mileageHighRateBal = Uniconta.ClientTools.Localization.lookup("MileageHighRate");

            foreach (var grp in groups)
            {
                if (grp.Caption == mileageHighRateBal)
                {
                    mileageHighTotal = mileage + mileageYTD + mileageNotApproved;
                    grp.StatusValue = mileageHighTotal.ToString(format);
                }
            }
        }

        void SetStatusTextEfficiencyPercentage(double efficiencyPercentage = 0)
        {
            string format = "N2";
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var lblEfficiencyPercentage = Uniconta.ClientTools.Localization.lookup("EfficiencyPercentage");

            foreach (var grp in groups)
            {
                if (grp.Caption == lblEfficiencyPercentage)
                    grp.StatusValue = efficiencyPercentage.ToString(format);
            }
        }

        TMEmpCalendarSetupClient[] calenderLst;
        List<TMEmpCalendarLineClient> calendarLineLst = new List<TMEmpCalendarLineClient>();
        DateTime calendarStartDate;
        async void EmployeeNormalHours()
        {
            busyIndicator.IsBusy = true;
            calendarStartDate = DateTime.MinValue;

            if (calenderLst == null)
            {
                var filter = new List<PropValuePair>() { PropValuePair.GenereteWhereElements("Employee", typeof(string), employee.KeyStr) };
                calenderLst = await api.Query<TMEmpCalendarSetupClient>(filter);
            }

            var weekStartDate = employee._Hired > JournalLineDate ? employee._Hired : JournalLineDate;
            var weekEnddate = JournalLineDate.AddDays(6);

            var calenders = calenderLst?.
                Where(x => (x.ValidFrom <= weekStartDate || x.ValidFrom == DateTime.MinValue || weekEnddate >= x.ValidFrom) &&
                (x.ValidTo >= weekStartDate || x.ValidTo == DateTime.MinValue)).ToList();

            normHoursDay1 = normHoursDay2 = normHoursDay3 = normHoursDay4 = normHoursDay5 = normHoursDay6 = normHoursDay7 = normHoursTotal = 0d;

            if (calenders != null)
            {
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

                    var dateList = calendarLineLst?.Where(d => d.Calendar == tmEmpCalender.Calendar &&  d.Date >= weekStartDate && d.Date <= weekEnddate).ToList(); // get 7 days hours
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
                    var firstCalenderdates = calendarLineLst?.Where(x => x.Calendar == calenders[0].Calendar && x.Date >= calenders[0].ValidFrom && x.Date <= calenders[0].ValidTo);

                    if (calendarLineLst.Any(s => s.Calendar == calenders[1].Calendar) == false)
                    {
                        var calLst = await api.Query<TMEmpCalendarLineClient>(calenders[1].CalendarRef);
                        if (calLst != null)
                            calendarLineLst.AddRange(calLst);
                    }
                    var secCalenderdates = calendarLineLst.Where(x => x.Calendar == calenders[1].Calendar && x.Date >= calenders[1].ValidFrom && x.Date <= calenders[1].ValidTo);

                    var lst = firstCalenderdates?.Concat(secCalenderdates).ToList();
                    var dateList = lst?.Where(d => d.Date >= weekStartDate && d.Date <= weekEnddate).ToList(); // get 7 days hours
                    GetHoursForSevenDays(dateList);
                }
            }

            busyIndicator.IsBusy = false;
        }

        double normHoursDay1, normHoursDay2, normHoursDay3, normHoursDay4, normHoursDay5, normHoursDay6, normHoursDay7, normHoursTotal = 0d;
        void GetHoursForSevenDays(List<TMEmpCalendarLineClient> calenderLine)
        {
            if (calenderLine?.Count() > 7)
                calenderLine = calenderLine.Take(7).ToList();
            var calenderLineLst = calenderLine.OrderBy(x => x.Date);

            var startDateNorm = employee._Hired < calendarStartDate ? calendarStartDate : employee._Hired;

            foreach (var line in calenderLineLst)
            {
                line.Hours = startDateNorm > line.Date ? 0 : line.Hours;

                var day = line.Date.DayOfWeek.ToString();
                switch (day)
                {
                    case "Monday":
                        normHoursDay1 = line.Hours;
                        break;
                    case "Tuesday":
                        normHoursDay2 = line.Hours;
                        break;
                    case "Wednesday":
                        normHoursDay3 = line.Hours;
                        break;
                    case "Thursday":
                        normHoursDay4 = line.Hours;
                        break;
                    case "Friday":
                        normHoursDay5 = line.Hours;
                        break;
                    case "Saturday":
                        normHoursDay6 = line.Hours;
                        break;
                    case "Sunday":
                        normHoursDay7 = line.Hours;
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

            if (lst.Count() > 0)
                cmbRegistration.SelectedItem = lst.FirstOrDefault().RegistrationType;
            else
                cmbRegistration.SelectedIndex = 0;
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
                            e.TotalValue = normHoursDay3;
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
            journalline.Date = JournalLineDate;
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
            EmployeeNormalHours();
            SetValuesForGridProperties();
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
            var sortlst = new  SortingProperties[] { rowId };
            var sorter = new FilterSorter(sortlst);
            await dgTMJournalLineGrid.Filter(tmJournalLineFilter);
            await dgTMJournalLineTransRegGrid.Filter(tmJournalLineTransReg, PropSort: sorter);
            InitializeStatusText();
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
                    journalLine.Date = JournalLineDate;
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
                    if (selectedItem != null)
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
                        var selectedMileage = dgTMJournalLineTransRegGrid.SelectedItem as TMJournalLineClientLocal;
                        if (selectedMileage != null && KeyAllowed("DeleteLine") && mileageGridFocus)
                            dgTMJournalLineTransRegGrid.DeleteRow();
                    }
                    break;
                case "Today":
                    saveGrid();
                    txtDateTo.DateTime = JournalLineDate = FirstDayOfWeek(System.DateTime.Today);
                    LoadGridOnWeekChange();
                    break;
                case "Forward":
                    saveGrid();
                    txtDateTo.DateTime = JournalLineDate = JournalLineDate.AddDays(7);
                    LoadGridOnWeekChange();
                    SetButtons();
                    break;
                case "BackWard":
                    saveGrid();
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
                    ValidateJournal();
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void TimeRegistration(TMJournalLineClientLocal selectedItem, bool returning, bool addMileage = false)
        {
            var cw = new CwTransportRegistration(selectedItem, api, mileageHighTotal, returning, addMileage);
            cw.Closed += async delegate
            {
                if (cw.DialogResult == true)
                {
                    if (!cw.ExistingTransportJrnlLine)
                    {
                        var journalLine = new TMJournalLineClientLocal();
                        journalLine.Project = cw.InternalProject;
                        journalLine.Date = JournalLineDate;
                        journalLine._RegistrationType = Uniconta.DataModel.RegistrationType.Mileage;
                        journalLine.AddressFrom = cw.FromAddress;
                        journalLine.AddressTo = cw.ToAddress;
                        journalLine.Text = cw.Purpose;
                        journalLine.PayrollCategory = cw.PayType;
                        journalLine.VechicleRegNo = cw.VechicleRegNo;
                        journalLine.Day1 = cw.Day1;
                        journalLine.Day2 = cw.Day2;
                        journalLine.Day3 = cw.Day3;
                        journalLine.Day4 = cw.Day4;
                        journalLine.Day5 = cw.Day5;
                        journalLine.Day6 = cw.Day6;
                        journalLine.Day7 = cw.Day7;
                        SetInvoiceable(journalLine);
                        dgTMJournalLineTransRegGrid.AddRow(journalLine);
                        if (cw.Returning)
                        {
                            journalLine._AddressFrom = cw.ToAddress;
                            journalLine._AddressTo = cw.FromAddress;
                            dgTMJournalLineTransRegGrid.AddRow(journalLine);
                        }
                        await dgTMJournalLineTransRegGrid.SaveData();
                    }
                    else
                    {
                        dgTMJournalLineTransRegGrid.SetLoadedRow(selectedItem);
                        selectedItem.Project = cw.InternalProject;
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
            empPriceLst = await api.Query<EmpPayrollCategoryEmployeeClient>(employee);
        }

        internal class DayStatus
        {
            public int Status;
            public double Amount;
        }

        async private void ActionApprove()
        {
#if !SILVERLIGHT
            if (ValidateJournal(TMJournalActionType.Approve, false))
            {
                this.api.AllowBackgroundCrud = false;
                var savetask = saveGrid();
                this.api.AllowBackgroundCrud = true;
                bool emptyCloseDate = false;
                bool emptyApproveDate = false;
                var lstInsert = new List<ProjectJournalLineClient>();
                var companySettings = await api.Query<CompanySettingsClient>();
                var tmVoucher = companySettings.FirstOrDefault()._TMJournalVoucherSerie;
                var firstDay = employee._TMApproveDate > JournalLineDate ? (employee._TMApproveDate.AddDays(2) - JournalLineDate).Days : 1;
      
                CWTimePosting postingDialog = new CWTimePosting(JournalLineDate.AddDays(6), string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Period"), GetPeriod(JournalLineDate)), api.CompanyEntity?.Name);
                postingDialog.DialogTableId = 2000000065;
                postingDialog.Closed += async delegate
                {
                    if (postingDialog.DialogResult == true)
                    {
                        var dlgApprovedDate = postingDialog.PostedDate;

                        #region DialogValidation
                        
                        if (dlgApprovedDate == DateTime.MinValue)
                        {
                            UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("Date")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        await api.Read(employee);

                        var calendarStartDate = DateTime.MinValue;
                        TMEmpCalendarSetupClient calendar;
                        if (employee._TMCloseDate == DateTime.MinValue || employee._TMApproveDate == DateTime.MinValue)
                        {
                            calendar = calenderLst?.OrderBy(s => s.ValidFrom).FirstOrDefault();
                            if (calendar == null)
                            {
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoCalendars"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }

                            calendarStartDate = calendar.ValidFrom;
                        }

                        if (dlgApprovedDate < JournalLineDate || dlgApprovedDate > JournalLineDate.AddDays(6))
                        {
                            UnicontaMessageBox.Show(string.Format("'{0}' only allowed in current Period", TMJournalActionType.Approve), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        if (employee._TMCloseDate == DateTime.MinValue)
                        {
                            employee._TMCloseDate = employee._Hired != DateTime.MinValue && employee._Hired.AddDays(-1) > calendarStartDate.AddDays(-1) ? employee._Hired.AddDays(-1) : calendarStartDate.AddDays(-1);
                            emptyCloseDate = true;

                        }

                        if (employee._TMApproveDate == DateTime.MinValue)
                        {
                            employee._TMApproveDate = employee._Hired != DateTime.MinValue && employee._Hired.AddDays(-1) > calendarStartDate.AddDays(-1) ? employee._Hired.AddDays(-1) : calendarStartDate.AddDays(-1);
                            emptyApproveDate = true;
                        }

                        if (dlgApprovedDate > employee._TMCloseDate)
                        {
                            UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ClosePeriodNotApproved")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        else if (dlgApprovedDate <= employee._TMApproveDate)
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

                        #region Hours
                        empPriceLst = await api.Query<EmpPayrollCategoryEmployeeClient>(employee);

                        var tmLinesTime = dgTMJournalLineGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;

                        var approveDate = employee._TMApproveDate;
                        var startDate = approveDate >= JournalLineDate ? approveDate.AddDays(1) : JournalLineDate;
                        var endDate = JournalLineDate.AddDays(6);
                        tmHelper.SetEmplPrice(tmLinesTime, empPriceLst, startDate, endDate);

                        foreach (var rec in tmLinesTime)
                        {
                            var days = (dlgApprovedDate.AddDays(1) - rec.Date).Days;

                            for (int x = firstDay; x <= days; x++)
                            {
                                var qty = (double)rec.GetType().GetProperty(string.Format("Day{0}", x)).GetValue(rec);

                                if (qty != 0)
                                {
                                    var lineclient = new ProjectJournalLineClient();
                       
                                    lineclient._Approved = true;
                                    lineclient._Text = rec.Text;
                                    lineclient._Unit = Uniconta.DataModel.ItemUnit.Hours;
                                    lineclient._TransType = rec.TransType;
                                    lineclient._Project = rec.Project;
                                    lineclient._PayrollCategory = rec.PayrollCategory;
                                    lineclient._Task = rec.Task;

                                    var payrollCat = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(rec.PayrollCategory);

                                    if (payrollCat._InternalType == Uniconta.DataModel.InternalType.OverTime || payrollCat._InternalType == Uniconta.DataModel.InternalType.FlexTime)
                                    {
                                        var factor = payrollCat._Factor == 0 ? 1 : payrollCat._Factor;
                                        lineclient._Qty = qty * factor;
                                    }
                                    else
                                        lineclient._Qty = qty;

                                    lineclient._PrCategory = payrollCat._PrCategory;
                                    lineclient._Employee = rec.Employee;

                                    lineclient._Dim1 = employee._Dim1;
                                    lineclient._Dim2 = employee._Dim2;
                                    lineclient._Dim3 = employee._Dim3;
                                    lineclient._Dim4 = employee._Dim4;
                                    lineclient._Dim5 = employee._Dim5;

                                    lineclient._Invoiceable = rec.Invoiceable;

                                    lineclient._Date = rec.Date.AddDays(x - 1);

                                    lineclient._SalesPrice = (double)rec.GetType().GetProperty(string.Format("SalesPriceDay{0}", x)).GetValue(rec);
                                    lineclient._CostPrice = (double)rec.GetType().GetProperty(string.Format("CostPriceDay{0}", x)).GetValue(rec);

                                    lstInsert.Add(lineclient);
                                }
                            }
                        }
                        #endregion

                        #region Mileage
                        var tmLinesMileage = dgTMJournalLineTransRegGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;

                        foreach (var rec in tmLinesMileage)
                        {
                            var days = (dlgApprovedDate.AddDays(1) - rec.Date).Days;

                            for (int x = firstDay; x <= days; x++)
                            {
                                var qty = (double)rec.GetType().GetProperty(string.Format("Day{0}", x)).GetValue(rec);

                                if (qty != 0)
                                {
                                    var lineclient = new ProjectJournalLineClient();
                       
                                    lineclient._Approved = true;

                                    var purpose = Regex.Replace(rec.Text ?? string.Empty, @"\r\n?|\n", ", ") + Environment.NewLine;
                                    var fromAdr = Regex.Replace(rec.AddressFrom ?? string.Empty, @"\r\n?|\n", ", ") + Environment.NewLine;
                                    var toAdr = Regex.Replace(rec.AddressTo ?? string.Empty, @"\r\n?|\n", ", ") + Environment.NewLine;
                                    var VechicleNo = Regex.Replace(rec.VechicleRegNo ?? string.Empty, @"\r\n?|\n", ", ") + Environment.NewLine;

                                    lineclient._Text = string.Format("{0}: {1}{2}: {3}{4}: {5}{6}: {7}", Uniconta.ClientTools.Localization.lookup("Purpose"), purpose, Uniconta.ClientTools.Localization.lookup("From"), fromAdr, Uniconta.ClientTools.Localization.lookup("To"), toAdr, Uniconta.ClientTools.Localization.lookup("VechicleRegNo"), rec.VechicleRegNo);
                                    lineclient._Unit = Uniconta.DataModel.ItemUnit.km;
                                    lineclient._TransType = rec.TransType;
                                    lineclient._Project = rec.Project;
                                    lineclient._PayrollCategory = rec.PayrollCategory;

                                    var payrollCat = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(rec.PayrollCategory);

                                    lineclient._PrCategory = payrollCat._PrCategory;
                                    lineclient._Employee = rec.Employee;

                                    lineclient._Dim1 = employee._Dim1;
                                    lineclient._Dim2 = employee._Dim2;
                                    lineclient._Dim3 = employee._Dim3;
                                    lineclient._Dim4 = employee._Dim4;
                                    lineclient._Dim5 = employee._Dim5;

                                    lineclient._Qty = qty;
                                    lineclient._Invoiceable = rec.Invoiceable;

                                    lineclient._Date = rec.Date.AddDays(x - 1);

                                    lineclient._SalesPrice = payrollCat._Rate;

                                    lstInsert.Add(lineclient);
                                }
                            }
                        }
                        #endregion

                        if (lstInsert == null)
                            return;

                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                        busyIndicator.IsBusy = true;
                        if (savetask != null)
                            await savetask;

                        if (postingDialog.IsSimulation)
                        {
                            AddDockItem(TabControls.SimulatedPrJournalLinePage, lstInsert?.ToArray(), Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                        }
                        else
                        {
                            if (lstInsert.Count != 0)
                            {
                                Task<PostingResult> task;

                                task = postingApi.PostEmpJournal((Uniconta.DataModel.Employee)employee, lstInsert, dlgApprovedDate, postingDialog.comments, false, new GLTransClientTotal());

                                var postingResult = await task;

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
                                    employee._TMApproveDate = dlgApprovedDate;
                                    SetButtons();
                                    await saveGrid();
                                    await BindGrid();
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
                               
                                var res = await postingApi.EmployeeSetDates(employee._Number, employee._TMCloseDate, dlgApprovedDate);

                                busyIndicator.IsBusy = false;
                                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                                if (res != ErrorCodes.Succes)
                                    UtilDisplay.ShowErrorCode(res);
                                else
                                {
                                    employee._TMApproveDate = dlgApprovedDate;
                                    await saveGrid();
                                    await Filter(tmJournalLineFilter);
                                    await FilterTransRegGrid(tmJournalLineTransReg);
                                    SetButtons();
                                    GetDateStatusOnCalender(employee);
                                }
                            }
                        }
                    }
                };
                postingDialog.Show();
            }
#endif
        }

        private void ActionCloseOpen(TMJournalActionType actionType)
        {
#if !SILVERLIGHT
            bool emptyCloseDate = false;
            bool updateEmpl = false;

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

                    var calendarStartDate = DateTime.MinValue;
                    TMEmpCalendarSetupClient calendar;
                    if (employee._TMCloseDate == DateTime.MinValue || employee._TMApproveDate == DateTime.MinValue)
                    {
                        calendar = calenderLst?.OrderBy(s => s.ValidFrom).FirstOrDefault();
                        if (calendar == null)
                        {
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoCalendars"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                        employee._TMCloseDate = employee._Hired != DateTime.MinValue && employee._Hired.AddDays(-1) > calendarStartDate.AddDays(-1) ? employee._Hired.AddDays(-1) : calendarStartDate.AddDays(-1);
                        emptyCloseDate = true;
                    }

                    if (employee._TMApproveDate == DateTime.MinValue)
                        employee._TMApproveDate = employee._Hired != DateTime.MinValue && employee._Hired.AddDays(-1) > calendarStartDate.AddDays(-1) ? employee._Hired.AddDays(-1) : calendarStartDate.AddDays(-1);


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
                                if (ValidateJournal(actionType, false))
                                {
                                    employee._TMCloseDate = cwDate.StartDate;
                                    updateEmpl = true;
                                }
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
                                employee._TMCloseDate = cwDate.StartDate.AddDays(-1);
                                updateEmpl = true;
                            }
                            break;
                    }

                    if (updateEmpl)
                    {
                        var res = await postingApi.EmployeeSetDates(employee._Number, employee._TMCloseDate, employee._TMApproveDate);
                        if (res != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(res);
                        else
                        {
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

        private bool ValidateJournal(TMJournalActionType actionType = TMJournalActionType.Close, bool showMsgOK = true)
        {
            bool ret = true;
#if !SILVERLIGHT
            if (employee._TMApproveDate >= JournalLineDate.AddDays(6))
                return false;

            saveGrid();

            var preValidateRes = tmHelper.PreValidate(normHoursTotal, payrollCache);

            if (preValidateRes != null && preValidateRes.Count() > 0)
            {
                var preErrors = new List<string>();
                foreach (var error in preValidateRes)
                {
                    preErrors.Add(error.Message);
                }

                if (preErrors != null && preErrors.Count() != 0)
                {
                    CWErrorBox cwError = new CWErrorBox(preErrors.ToArray());
                    cwError.Show();
                    ret = false;
                }
            }
            else
            {
                // Validate Hours >>
                dgTMJournalLineGrid.Columns.GetColumnByName("ErrorInfo").Visible = true;

                var tmLinesHours = dgTMJournalLineGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;

                var valErrorsHours = tmHelper.ValidateLinesHours(tmLinesHours, JournalLineDate, JournalLineDate.AddDays(6), empPriceLst, payrollCache, projGroupCache);
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
                if (actionType == TMJournalActionType.Close)
                {
                    if (JournalLineDate.AddDays(6) > employee._TMCloseDate && ret == true && totalSum - normHoursTotal != 0 &&
                        (payrollCache.Any(s => s._InternalType == Uniconta.DataModel.InternalType.OverTime) == true || payrollCache.Any(s => s._InternalType == Uniconta.DataModel.InternalType.FlexTime) == true))
                    {
                        var payrollCategory = payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.OverTime).OrderBy(s => s._Factor).FirstOrDefault();
                        payrollCategory = payrollCategory == null ? payrollCache.Where(s => s._InternalType == Uniconta.DataModel.InternalType.FlexTime).OrderBy(s => s._Factor).FirstOrDefault() : payrollCategory;

                        TMJournalLineClientLocal journalLine = new TMJournalLineClientLocal();
                        journalLine.Project = payrollCategory._InternalProject;
                        journalLine.Date = JournalLineDate;
                        journalLine._Invoiceable = false;
                        journalLine._RegistrationType = Uniconta.DataModel.RegistrationType.Hours;
                        journalLine.NotifyPropertyChanged("RegistrationType");
                        journalLine.PayrollCategory = totalSum - normHoursTotal != 0 ? payrollCategory.KeyStr : null;
                        journalLine.Day1 = JournalLineDate.AddDays(0) > employee._TMCloseDate ? normHoursDay1 - day1Sum : 0;
                        journalLine.Day2 = JournalLineDate.AddDays(1) > employee._TMCloseDate ? normHoursDay2 - day2Sum : 0;
                        journalLine.Day3 = JournalLineDate.AddDays(2) > employee._TMCloseDate ? normHoursDay3 - day3Sum : 0;
                        journalLine.Day4 = JournalLineDate.AddDays(3) > employee._TMCloseDate ? normHoursDay4 - day4Sum : 0;
                        journalLine.Day5 = JournalLineDate.AddDays(4) > employee._TMCloseDate ? normHoursDay5 - day5Sum : 0;
                        journalLine.Day6 = JournalLineDate.AddDays(5) > employee._TMCloseDate ? normHoursDay6 - day6Sum : 0;
                        journalLine.Day7 = JournalLineDate.AddDays(6) > employee._TMCloseDate ? normHoursDay7 - day7Sum : 0;
                        dgTMJournalLineGrid.AddRow(journalLine);
                    }
                }
                // Validate Hours <<

                // Validate Mileage >>
                dgTMJournalLineTransRegGrid.Columns.GetColumnByName("colErrorInfo").Visible = true;

                var tmLinesMileage = dgTMJournalLineTransRegGrid.ItemsSource as IEnumerable<TMJournalLineClientLocal>;

                var valErrorsMileage = tmHelper.ValidateLinesMileage(tmLinesMileage, JournalLineDate, payrollCache);
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

        async void LoadGridOnWeekChange()
        {
            if (clearMileageList || clearHoursList)
            {
                journalLineNotApprovedLst = null;
                clearMileageList = false;
                clearHoursList = false;
            }

            tmJournalLineFilter = new List<PropValuePair>()
            {
                PropValuePair.GenereteWhereElements("Date", typeof(DateTime), JournalLineDate.ToString()),
                PropValuePair.GenereteWhereElements("RegistrationType", typeof(int), "0")
            };
            tmJournalLineTransReg = new List<PropValuePair>()
            {
                 PropValuePair.GenereteWhereElements("Date", typeof(DateTime), JournalLineDate.ToString()),
                 PropValuePair.GenereteWhereElements("RegistrationType", typeof(int), "1")
            };
            SetColumnHeader();
            await BindGrid();
            CopyLines();
        }

        protected override async void LoadCacheInBackGround()
        {
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
