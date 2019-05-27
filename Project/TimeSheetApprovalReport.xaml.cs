using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using System.Resources;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TimeSheetApprovalReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TimeSheetApprovalLocalClient); } }
    }

    public partial class TimeSheetApprovalReport : GridBasePage
    {
        TMApprovalSetupClient[] approverLst;
        Uniconta.DataModel.Employee employee;
        static DateTime fromDate;
        SQLCache employeeCache, empPayrollCatCache;

        public TimeSheetApprovalReport(UnicontaBaseEntity master, TMApprovalSetupClient[] approvalList) : base(master)
        {
            InitializeComponent();
            fromDate = fromDate == DateTime.MinValue ? DateTime.Today.AddMonths(-1) : fromDate;
            approverLst = approvalList;
            employee = master as Uniconta.DataModel.Employee;
            localMenu.dataGrid = dgTimeSheetApprovalRpt;
            SetRibbonControl(localMenu, dgTimeSheetApprovalRpt);
            dgTimeSheetApprovalRpt.api = api;
            dgTimeSheetApprovalRpt.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgTimeSheetApprovalRpt.ShowTotalSummary();
            dgTimeSheetApprovalRpt.tableView.AllowFixedColumnMenu = true;
            dgTimeSheetApprovalRpt.tableView.GroupSummaryDisplayMode = GroupSummaryDisplayMode.AlignByColumns;
            StartLoadCache();
            LoadGrid();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            SetDimensionLocalMenu();
        }

        public void SetDimensionLocalMenu()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var c = api.CompanyEntity;
            if (c == null)
                return;
            var ibase1 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension1");
            var ibase2 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension2");
            var ibase3 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension3");
            var ibase4 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension4");
            var ibase5 = UtilDisplay.GetMenuCommandByName(rb, "GroupByDimension5");
            ibase1.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim1);
            ibase2.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim2);
            ibase3.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim3);
            ibase4.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim4);
            ibase5.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), (string)c._Dim5);
            var noofDimensions = c.NumberOfDimensions;
            if (noofDimensions < 5)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension5");
            if (noofDimensions < 4)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension4");
            if (noofDimensions < 3)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension3");
            if (noofDimensions < 2)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension2");
            if (noofDimensions < 1)
                UtilDisplay.RemoveMenuCommand(rb, "GroupByDimension1");
        }

        public override Task InitQuery()
        {
            return null;
        }

        List<TMEmpCalendarSetupClient> lstCalenderSetup;
        Dictionary<Tuple<string, string>, double> dictCalendar = new Dictionary<Tuple<string, string>, double>();

        async Task LoadCalenders()
        {
            if (lstCalenderSetup != null)
                return;

            var lstCal = await api.Query<TMEmpCalendarSetupClient>();
            lstCalenderSetup = lstCal?.ToList();

            var todayDate = DateTime.Today;

            var calendarFromDate = approverLst.Min(s => s._ValidFrom);
            if (calendarFromDate < todayDate.AddYears(-2))
                calendarFromDate = todayDate.AddYears(-2);

            foreach (var rec in approverLst.Where(s => s.Employee != null))
            {
                var calenders = lstCalenderSetup.Where(x => (x._Employee == rec.Employee && (x.ValidTo >= calendarFromDate || x.ValidTo == DateTime.MinValue)));

                foreach (var recCal in calenders)
                {
                    if (dictCalendar.ContainsKey(new Tuple<string, string>(recCal.Calendar, string.Empty)))
                        continue;

                    var pairCalendar = new PropValuePair[]
                    {
                        PropValuePair.GenereteWhereElements(nameof(TMEmpCalendarLineClient.Calendar), typeof(string), recCal.Calendar),
                        PropValuePair.GenereteWhereElements(nameof(TMEmpCalendarLineClient.Date), typeof(DateTime), String.Format("{0:d}..{1:d}", calendarFromDate, todayDate.AddMonths(2)))
                    };

                    var tmEmpCalenderLineLst = await api.Query<TMEmpCalendarLineClient>(pairCalendar);

                    var grpCalendarLst = tmEmpCalenderLineLst.GroupBy(x => new { x.Period, x.Calendar }).Select(x => new { Key = x.Key, Hours = x.Sum(y => y.Hours) }).ToList();

                    foreach (var cal in grpCalendarLst)
                    {

                        if (!dictCalendar.ContainsKey(new Tuple<string, string>(cal.Key.Calendar, cal.Key.Period)))
                            dictCalendar.Add(new Tuple<string, string>(cal.Key.Calendar, cal.Key.Period), cal.Hours);

                        if (!dictCalendar.ContainsKey(new Tuple<string, string>(cal.Key.Calendar, string.Empty)))
                            dictCalendar.Add(new Tuple<string, string>(cal.Key.Calendar, string.Empty), 0);
                    }
                }
            }
        }

        IEnumerable<IGrouping<InternalType, EmpPayrollCategoryClient>> activeInternalTypeLst;
        SQLTableCache<Uniconta.ClientTools.DataModel.EmpPayrollCategoryClient> empPayrollCatList;

        StringBuilder sbInternalProj = new StringBuilder();
        StringBuilder sbPayrollCat = new StringBuilder();

        double mileageYTD;
        double mileageYTDnextPeriod;
        DateTime mileageStartDate;

        double vacationYTD;
        double vacationYTDnextPeriod;
        double vacationPrimoNextPeriod;
        DateTime vacationStartDate;

        double otherVacationYTD;
        double otherVacationYTDnextPeriod;
        double otherVacationPrimoNextPeriod;
        DateTime otherVacationStartDate;

        double overTimeYTD;
        double flexTimeYTD;

        async void LoadGrid()
        {
            busyIndicator.IsBusy = true;

            await LoadCalenders();

            var todayDate = DateTime.Today;

            var emplCache = await api.LoadCache<Uniconta.ClientTools.DataModel.EmployeeClient>();

            //Internal Registration >>
           
            empPayrollCatList = await api.LoadCache<Uniconta.ClientTools.DataModel.EmpPayrollCategoryClient>();

            activeInternalTypeLst = empPayrollCatList.Where(s => s._InternalType != InternalType.None).GroupBy(s => s._InternalType);

            if (activeInternalTypeLst != null && activeInternalTypeLst.Count() != 0)
            {
                var empPayrollGrpLst = empPayrollCatList.GroupBy(s => new { s.InternalProject, s.Number, s._InternalType });
               
                if (empPayrollGrpLst != null && empPayrollGrpLst.Count() != 0)
                {
                    sbInternalProj = new StringBuilder();
                    sbPayrollCat = new StringBuilder();
                    foreach (var val in empPayrollGrpLst.Where(s => s.Key._InternalType != InternalType.None))
                    {
                        sbInternalProj.Append(val.Key.InternalProject).Append(";");
                        sbPayrollCat.Append(val.Key.Number).Append(";");
                    }
                }
            }
            //Internal Registration <<

            var tsApprovalLst = new List<TimeSheetApprovalLocalClient>();
            foreach (var rec in approverLst.Where(s => s.Employee != null))
            {
                var empl = emplCache.Get(rec.Employee);

                if (empl == null)
                    continue;

                var startDate = todayDate;
                DateTime startDateMonday = DateTime.MinValue;
                var endDate = todayDate;

                if (empl.Hired != DateTime.MinValue && empl.Hired >= rec._ValidFrom)
                    startDate = empl.Hired;

                var calenders = lstCalenderSetup.Where(x => (x._Employee == rec.Employee)).ToList();

                if (calenders != null && calenders.Count() == 1)
                {
                    var tmEmpCalender = calenders.FirstOrDefault();

                    if (startDate < tmEmpCalender.ValidFrom)
                        startDate = tmEmpCalender.ValidFrom;
                }

                if ((empl.Hired != DateTime.MinValue && empl.Hired <= rec._ValidFrom) || empl.Hired == DateTime.MinValue)
                    startDate = rec._ValidFrom == DateTime.MinValue ? startDate : rec._ValidFrom;


                if (rec._ValidTo != DateTime.MinValue && rec._ValidTo <= startDate)
                    endDate = rec._ValidTo;

                if (startDate < empl.TMApproveDate)
                {
                    if (empl.TMApproveDate.DayOfWeek == DayOfWeek.Sunday)
                        startDate = empl.TMApproveDate;
                    else
                    {
                        int diff = (7 + (empl.TMApproveDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                        startDate = empl.TMApproveDate.AddDays(-1 * diff).AddDays(-1).Date;
                    }
                }

                if (empl.TMApproveDate < startDate && startDate.DayOfWeek != DayOfWeek.Monday)
                {
                    int diff = (7 + (startDate.DayOfWeek - DayOfWeek.Monday)) % 7;
                    startDateMonday = startDate.AddDays(-1 * diff);
                }
                else
                    startDateMonday = startDate;

                endDate = endDate < empl.Terminated ? endDate : empl.Terminated == DateTime.MinValue ? todayDate : empl.Terminated;

                var pairTM = new PropValuePair[]
                {
                    PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), rec.Employee),
                    PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), typeof(DateTime), String.Format("{0:d}..{1:d}", startDateMonday, endDate))
                };

                var journalLineLst = await api.Query<TMJournalLineClient>(pairTM);
                
                #region Internal Registration
                if (activeInternalTypeLst != null && activeInternalTypeLst.Count() != 0)
                {
                    var pairInternalTrans = new PropValuePair[]
                    {
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Employee), typeof(string), rec.Employee),
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Project), typeof(string), sbInternalProj.ToString()),
                        PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.PayrollCategory), typeof(int), sbPayrollCat.ToString()),
                    };

                    var internalTransLst = await api.Query<ProjectTransClient>(pairInternalTrans);
                    var empPayrollGrpLst = empPayrollCatList.GroupBy(s => new { s.InternalProject, s.Number, s._InternalType });

                    #region Mileage
                    if (internalTransLst != null && activeInternalTypeLst.Any(s => s.Key == InternalType.Mileage))
                    {
                        var payrollCatLst = empPayrollGrpLst.Where(s => (s.Key._InternalType == InternalType.Mileage));
                        var transList = new List<ProjectTransClient>();
                        mileageStartDate = new DateTime(startDate.Year, 1, 1);
                        foreach (var y in payrollCatLst)
                        {
                            transList.AddRange(internalTransLst.Where(s => s.Project == y.Key.InternalProject && s.PayrollCategory == y.Key.Number && s.Date >= mileageStartDate && s.Date <= startDate));
                        }
                        mileageYTD = transList.Select(x => x.Qty).Sum();

                        transList = new List<ProjectTransClient>();
                        mileageStartDate = new DateTime(startDate.Year + 1, 1, 1);
                        foreach (var y in payrollCatLst)
                        {
                            transList.AddRange(internalTransLst.Where(s => s.Project == y.Key.InternalProject && s.PayrollCategory == y.Key.Number && s.Date == mileageStartDate));
                        }
                        mileageYTDnextPeriod = transList.Select(x => x.Qty).Sum();
                    }
                    #endregion

                    #region Vacation
                    if (internalTransLst != null && activeInternalTypeLst.Any(s => s.Key == InternalType.Vacation))
                    {
                        var payrollCatLst = empPayrollGrpLst.Where(s => s.Key._InternalType == InternalType.Vacation);
                        var transList = new List<ProjectTransClient>();
                        vacationStartDate = new DateTime(startDate.Year, 5, 1);
                        if (startDate < vacationStartDate)
                            vacationStartDate = vacationStartDate.AddYears(-1);

                        foreach (var zy in payrollCatLst)
                        {
                            transList.AddRange(internalTransLst.Where(s => s.Project == zy.Key.InternalProject && s.PayrollCategory == zy.Key.Number && s.Date >= vacationStartDate && s.Date < startDate));
                        }
                        vacationYTD = transList.Select(x => x.Qty).Sum();

                        transList = new List<ProjectTransClient>();
                        vacationStartDate = new DateTime(vacationStartDate.Year + 1, 5, 1);
                        foreach (var y in payrollCatLst)
                        {
                            transList.AddRange(internalTransLst.Where(s => s.Project == y.Key.InternalProject && s.PayrollCategory == y.Key.Number && s.Date == vacationStartDate));
                        }
                        vacationPrimoNextPeriod = transList.Select(x => x.Qty).Sum();
                    }
                    #endregion

                    #region Other vacation
                    if (internalTransLst != null && activeInternalTypeLst.Any(s => s.Key == InternalType.OtherVacation))
                    {
                        var payrollCatLst = empPayrollGrpLst.Where(s => s.Key._InternalType == InternalType.OtherVacation);
                        var transList = new List<ProjectTransClient>();
                        otherVacationStartDate = new DateTime(startDate.Year, 5, 1);
                        if (startDate < otherVacationStartDate)
                            otherVacationStartDate = otherVacationStartDate.AddYears(-1);
                        foreach (var y in payrollCatLst)
                        {
                            transList.AddRange(internalTransLst.Where(s => s.Project == y.Key.InternalProject && s.PayrollCategory == y.Key.Number && s.Date >= otherVacationStartDate && s.Date < startDate));
                        }
                        otherVacationYTD = transList.Select(x => x.Qty).Sum();

                        transList = new List<ProjectTransClient>();
                        otherVacationStartDate = new DateTime(otherVacationStartDate.Year + 1, 5, 1);
                        foreach (var y in payrollCatLst)
                        {
                            transList.AddRange(internalTransLst.Where(s => s.Project == y.Key.InternalProject && s.PayrollCategory == y.Key.Number && s.Date == otherVacationStartDate));
                        }
                        otherVacationPrimoNextPeriod = transList.Select(x => x.Qty).Sum();
                    }
                    #endregion

                    #region Overtime
                    if (internalTransLst != null && activeInternalTypeLst.Any(s => s.Key == InternalType.OverTime))
                    {
                        var payrollCatLst = empPayrollGrpLst.Where(s => s.Key._InternalType == InternalType.OverTime);
                        var transList = new List<ProjectTransClient>();
                        foreach (var y in payrollCatLst)
                        {
                            transList.AddRange(internalTransLst.Where(s => s.Project == y.Key.InternalProject && s.PayrollCategory == y.Key.Number));
                        }
                        overTimeYTD = -1 * transList.Select(x => x.Qty).Sum();
                    }
                    #endregion

                    #region FlexTime
                    if (internalTransLst != null && activeInternalTypeLst.Any(s => s.Key == InternalType.FlexTime))
                    {
                        var payrollCatLst = empPayrollGrpLst.Where(s => s.Key._InternalType == InternalType.FlexTime);
                        var transList = new List<ProjectTransClient>();
                        foreach (var y in payrollCatLst)
                        {
                            transList.AddRange(internalTransLst.Where(s => s.Project == y.Key.InternalProject && s.PayrollCategory == y.Key.Number));
                        }
                        flexTimeYTD = -1 * transList.Select(x => x.Qty).Sum();
                    }
                    #endregion FlexTime

                    #endregion Internal Registration

                }
                //Internal Registration <<

                var linesLst = await CreateApprovalList(rec, journalLineLst, startDateMonday, endDate);
                tsApprovalLst.AddRange(linesLst);
            }

            var ApprovalLst = (from x in tsApprovalLst
                        orderby x.Employee, x.Period ascending
                        select x).ToList();

            dgTimeSheetApprovalRpt.ItemsSource = ApprovalLst;
            dgTimeSheetApprovalRpt.Visibility = Visibility.Visible;

            busyIndicator.IsBusy = false;
        }


        async Task<List<TimeSheetApprovalLocalClient>> CreateApprovalList(TMApprovalSetupClient approvalSetuUp,TMJournalLineClient[] journalLineLst, DateTime startDate, DateTime endDate)
        {
            List<DateTime> lstMondays = new List<DateTime>();
            DateTime date = startDate;
             
            EmployeeClient empl = (Uniconta.ClientTools.DataModel.EmployeeClient)employeeCache.Get(approvalSetuUp.Employee);

            while (date <= endDate)
            {
                if (date.DayOfWeek == DayOfWeek.Monday)
                    lstMondays.Add(date);

                date = date.AddDays(1);
            }

            bool mileageNextYear = false;
            bool vacationNextYear = false;
            bool otherVacationNextYear = false;
            DateTime firstMondayVacationYear = DateTime.MinValue;
            DateTime currentVacationYear = DateTime.MinValue;


            var lines = new List<TimeSheetApprovalLocalClient>();
            foreach (var monday in lstMondays)
            {
                #region Mileage
                double mileageWeek = 0;
                if (activeInternalTypeLst.Any(s => s.Key == InternalType.Mileage))
                {
                    var lstMileage = journalLineLst?.Where(x => (x._Date == monday && x._RegistrationType == Uniconta.DataModel.RegistrationType.Mileage && x.InternalType == AppEnums.InternalType.ToString((int)InternalType.Mileage) && x.Invoiceable == false)).ToList();
                    if (lstMileage != null && lstMileage.Count > 0)
                    {
                        if (monday.Year != monday.AddDays(6).Year)
                        {
                            foreach (var trans in lstMileage)
                            {
                                for (int x = 1; x <= 7; x++)
                                {
                                    var qty = (double)trans.GetType().GetProperty(string.Format("Day{0}", x)).GetValue(trans);
                                    if (monday.Year != monday.AddDays(x - 1).Year)
                                        mileageYTDnextPeriod += qty;

                                    mileageWeek += qty;
                                }
                            }
                        }
                        else
                        {
                            var grpLstMileage = lstMileage.GroupBy(x => x._Date).Select(x => new { Date = x.Key, Sum = x.Sum(y => y.Total), Period = x.Select(y => y.Date).FirstOrDefault() }).FirstOrDefault();
                            mileageWeek = grpLstMileage.Sum;
                        }

                        mileageYTD += mileageWeek - mileageYTDnextPeriod;
                    }
                    else
                    {
                        if (monday.Year != monday.AddDays(6).Year)
                            mileageYTD = 0;
                    }

                    if (mileageNextYear)
                    {
                        mileageYTD = mileageYTDnextPeriod + mileageWeek;
                        mileageYTDnextPeriod = 0;
                        mileageNextYear = false;
                    }
                }
                #endregion Mileage

                #region Vacation, Other vacation
                double vacationWeek = 0;
                double otherVacationWeek = 0;
                if (activeInternalTypeLst.Any(s => s.Key == InternalType.Vacation))
                {
                    if (firstMondayVacationYear == DateTime.MinValue)
                        firstMondayVacationYear = monday < new DateTime(monday.Year, 5, 1) ? new DateTime(monday.Year-1, 5, 1) : new DateTime(monday.Year, 5, 1);

                    currentVacationYear = monday.AddDays(6) < new DateTime(monday.Year, 5, 1) ? new DateTime(monday.Year - 1, 5, 1) : new DateTime(monday.Year, 5, 1);

                    var lstVacation = journalLineLst?.Where(x => x._Date == monday &&
                                                                x._RegistrationType == Uniconta.DataModel.RegistrationType.Hours &&
                                                               (x.InternalType == AppEnums.InternalType.ToString((int)InternalType.Vacation) ||
                                                                x.InternalType == AppEnums.InternalType.ToString((int)InternalType.OtherVacation))).ToList();

                    if (lstVacation != null && lstVacation.Count > 0)
                    {
                        if (monday.Year != monday.AddDays(6).Year)
                        {
                            foreach (var trans in lstVacation)
                            {
                                for (int x = 1; x <= 7; x++)
                                {
                                    var qty = (double)trans.GetType().GetProperty(string.Format("Day{0}", x)).GetValue(trans);
                                    if (qty == 0)
                                        continue;

                                    if (monday.Year != monday.AddDays(x - 1).Year)
                                    {
                                        switch (trans._InternalType)
                                        {
                                            case InternalType.Vacation: vacationYTDnextPeriod += qty; break;
                                            case InternalType.OtherVacation: otherVacationYTDnextPeriod += qty; break;
                                        }
                                    }

                                    switch (trans._InternalType)
                                    {
                                        case InternalType.Vacation: vacationWeek += qty; break;
                                        case InternalType.OtherVacation: otherVacationWeek += qty; break;

                                    }
                                }
                            }
                        }
                        else
                        {
                            var grpLstVacation = lstVacation.GroupBy(x => new { x.Date, x._InternalType }).Select(x => new { GroupKey = x.Key, Sum = x.Sum(y => y.Total), Period = x.Select(y => y.Date) });
                            foreach (var trans in grpLstVacation)
                            {
                                switch (trans.GroupKey._InternalType)
                                {
                                    case InternalType.Vacation: vacationWeek = trans.Sum; break;
                                    case InternalType.OtherVacation: otherVacationWeek = trans.Sum; break;

                                }
                            }
                        }

                        if (vacationWeek != 0)
                            vacationYTD += vacationWeek - vacationYTDnextPeriod;

                        if (otherVacationWeek != 0)
                            otherVacationYTD += otherVacationWeek - otherVacationYTDnextPeriod;
                    }

                    if (vacationNextYear)
                    {
                        vacationYTD = vacationPrimoNextPeriod + vacationYTDnextPeriod + vacationWeek;
                        vacationYTDnextPeriod = 0;
                        vacationPrimoNextPeriod = 0;
                        vacationNextYear = false;
                    }

                    if (otherVacationNextYear)
                    {
                        otherVacationYTD = otherVacationPrimoNextPeriod + otherVacationYTDnextPeriod + otherVacationWeek;
                        otherVacationYTDnextPeriod = 0;
                        otherVacationPrimoNextPeriod = 0;
                        otherVacationNextYear = false;
                    }
                }
                #endregion Vacation, Other vacation

                var lstHours = journalLineLst?.Where(x => (x._Date == monday && x._RegistrationType == Uniconta.DataModel.RegistrationType.Hours)).ToList();
                if (lstHours != null && lstHours.Count > 0)
                {
                    var grpLst = lstHours.GroupBy(x => x._Date).Select(x => new { Date = x.Key, Sum = x.Sum(y => y.Total), Period = x.Select(y => y.Date).FirstOrDefault() }).FirstOrDefault();

                    var tsApproval = new TimeSheetApprovalLocalClient();
                    tsApproval.Employee = approvalSetuUp.Employee;
                    tsApproval.EmployeeName = approvalSetuUp.EmployeeName;
                    tsApproval.EmployeeGroup = empl._Group;
                    tsApproval.EmployeeGroupName = GetEmployeeGroupName(empl);
                    tsApproval.Date = grpLst.Date;
                    tsApproval.Total = grpLst.Sum;
                    tsApproval.Period = GetPeriod(grpLst.Date);
                    tsApproval.Dimension1 = empl._Dim1;
                    tsApproval.Dimension2 = empl._Dim2;
                    tsApproval.Dimension3 = empl._Dim3;
                    tsApproval.Dimension4 = empl._Dim4;
                    tsApproval.Dimension5 = empl._Dim5;
                    tsApproval.EmplyeeRef = empl;
                    tsApproval.NormHours = await GetNormalHours(empl, grpLst.Date, tsApproval.Period);
                    tsApproval.Status = GetStatus(empl, monday, tsApproval.TotalHours);
                    tsApproval.Mileage = mileageWeek;
                    tsApproval.MileageYTD = mileageYTD;
                    tsApproval.Vacation = vacationWeek;
                    tsApproval.VacationYTD = -1 * vacationYTD;
                    tsApproval.OtherVacation = otherVacationWeek;
                    tsApproval.OtherVacationYTD = -1 * otherVacationYTD;

                    #region OverTime, FlexTime, OtherAbsence and Sickness
                    if (activeInternalTypeLst.Any(s => s.Key == InternalType.Sickness || s.Key == InternalType.OtherAbsence || s.Key == InternalType.FlexTime || s.Key == InternalType.OverTime))
                    {
                        var lstInternalTrans = journalLineLst?.Where(x => (x._Date == monday && x._RegistrationType == Uniconta.DataModel.RegistrationType.Hours &&
                                                                         (x._InternalType == InternalType.Sickness || x._InternalType == InternalType.OtherAbsence ||
                                                                         x._InternalType == InternalType.FlexTime || x._InternalType == InternalType.OverTime))).ToList();

                        if (lstInternalTrans != null && lstInternalTrans.Count > 0)
                        {
                            var grpLstInternal = lstInternalTrans.GroupBy(x => new { x.Date, x._InternalType, x._PayrollCategory }).Select(x => new { GroupKey = x.Key, Sum = x.Sum(y => y.Total), Period = x.Select(y => y.Date) });

                            foreach (var rec in grpLstInternal)
                            {
                                var empPayCat = (Uniconta.DataModel.EmpPayrollCategory)empPayrollCatCache.Get(rec.GroupKey._PayrollCategory);
                                double factor = 0;
                                switch (rec.GroupKey._InternalType)
                                {
                                    case InternalType.Sickness: tsApproval.Sickness += rec.Sum; break; 
                                    case InternalType.OtherAbsence: tsApproval.OtherAbsence += rec.Sum; break;
                                    case InternalType.OverTime:
                                        factor = empPayCat != null ? (empPayCat._Factor == 0 ? 1 : empPayCat._Factor) : 1;
                                        tsApproval.OverTime += -1 * rec.Sum * factor;
                                        break; 
                                    case InternalType.FlexTime:
                                        factor = empPayCat != null ? (empPayCat._Factor == 0 ? 1 : empPayCat._Factor) : 1;
                                        tsApproval.FlexTime += -1 * rec.Sum * factor;
                                        break; 
                                }
                            }
                        }

                        overTimeYTD = overTimeYTD + tsApproval.OverTime;
                        tsApproval.OverTimeYTD = overTimeYTD;

                        flexTimeYTD =  flexTimeYTD + tsApproval.FlexTime;
                        tsApproval.FlexTimeYTD = flexTimeYTD;
                    }
                    #endregion OverTime, FlexTime, OtherAbsence and Sickness

                    #region Efficiency percentage
                    var grpLstInvoiceable = journalLineLst?.Where(s => s._Date == monday && 
                                                                       s._RegistrationType == Uniconta.DataModel.RegistrationType.Hours && 
                                                                       s._InternalType == InternalType.None).GroupBy(x => x._Invoiceable).Select(x => new { GroupKey = x.Key, Sum = x.Sum(y => y.Total) });

                    foreach (var i in grpLstInvoiceable)
                    {
                        if (i.GroupKey)
                            tsApproval.InvoiceableHours = i.Sum;
                        else
                            tsApproval.NotInvoiceableHours = i.Sum;
                    }

                    tsApproval.EfficiencyPercentage = tsApproval.InvoiceableHours + tsApproval.NotInvoiceableHours != 0 ? tsApproval.InvoiceableHours / (tsApproval.InvoiceableHours + tsApproval.NotInvoiceableHours) * 100 : 0;

                    #endregion Efficiency percentage

                    lines.Add(tsApproval);
                }
                else
                {
                    var tsApproval = new TimeSheetApprovalLocalClient();
                    tsApproval.Employee = approvalSetuUp.Employee;
                    tsApproval.EmployeeName = approvalSetuUp.EmployeeName;
                    tsApproval.EmployeeGroup = empl._Group;
                    tsApproval.EmployeeGroupName = GetEmployeeGroupName(empl);
                    tsApproval.Date = monday;
                    tsApproval.Period = GetPeriod(monday);
                    tsApproval.Dimension1 = empl._Dim1;
                    tsApproval.Dimension2 = empl._Dim2;
                    tsApproval.Dimension3 = empl._Dim3;
                    tsApproval.Dimension4 = empl._Dim4;
                    tsApproval.Dimension5 = empl._Dim5;
                    tsApproval.EmplyeeRef = empl;
                    tsApproval.NormHours = await GetNormalHours(empl, monday, GetPeriod(monday)); 
                    tsApproval.Status = GetMissingRecordsStatus(monday,tsApproval.TotalHours);
                    tsApproval.Mileage = mileageWeek;
                    tsApproval.MileageYTD = mileageYTD;
                    tsApproval.Vacation = vacationWeek;
                    tsApproval.VacationYTD = -1 * vacationYTD;
                    tsApproval.OtherVacation = otherVacationWeek;
                    tsApproval.OtherVacationYTD = -1 * otherVacationYTD;
                    tsApproval.OverTimeYTD = overTimeYTD;
                    tsApproval.FlexTimeYTD = flexTimeYTD;
                    lines.Add(tsApproval);
                }

                if (mileageYTDnextPeriod != 0)
                    mileageNextYear = true;

                if (activeInternalTypeLst.Any(s => s.Key == InternalType.Vacation))
                {
                    if (firstMondayVacationYear.Year != currentVacationYear.Year)
                    {
                        vacationNextYear = vacationYTDnextPeriod != 0 || vacationPrimoNextPeriod != 0;
                        otherVacationNextYear = otherVacationYTDnextPeriod != 0 || otherVacationPrimoNextPeriod != 0;
                    }
                }
            }
            return lines;
        }

        async Task<double> GetNormalHours(EmployeeClient empl, DateTime monday, string period)
        {
            //var weekStartDate = startDate;
            var calendarStartDate = DateTime.MinValue;
            var weekStartDate = empl._Hired > monday ? empl._Hired : monday;

            var weekEnddate = monday.AddDays(6);
            double hours = 0;

            var calenders = lstCalenderSetup.Where(x => (x._Employee == empl.Number) && (x.ValidFrom <= weekStartDate || x.ValidFrom == DateTime.MinValue || weekEnddate >= x.ValidFrom) && (x.ValidTo >= weekStartDate || x.ValidTo == DateTime.MinValue)).ToList();

            if (calenders == null || calenders.Count() == 0)
                return 0;

            if (calenders.Count() == 1)
            {
                var tmEmpCalender = calenders.FirstOrDefault();
                calendarStartDate = tmEmpCalender.ValidFrom;

                if (weekStartDate < calendarStartDate && calendarStartDate < weekEnddate)
                {
                    var pairCalendar = new PropValuePair[]
                    {
                    PropValuePair.GenereteWhereElements(nameof(TMEmpCalendarLineClient.Calendar), typeof(string), tmEmpCalender.Calendar),
                    PropValuePair.GenereteWhereElements(nameof(TMEmpCalendarLineClient.Date), typeof(DateTime), String.Format("{0:d}..{1:d}", calendarStartDate, weekEnddate))
                    };
                    TMEmpCalendarLineClient[] calweek = await api.Query<TMEmpCalendarLineClient>(pairCalendar);
                    var hoursWeek = calweek.Sum(s => s.Hours);
                    return hoursWeek;
                }
                else
                {
                    var hasValue = dictCalendar.TryGetValue(new Tuple<string, string>(tmEmpCalender.Calendar, period), out hours);
                    if (hasValue)
                        return hours;
                }
            }
            else
            {
                var fromDate = weekStartDate >= calenders[0].ValidFrom ? weekStartDate : calenders[0].ValidFrom;
                var toDate = weekEnddate <= calenders[0].ValidTo || calenders[0].ValidTo == DateTime.MinValue ? weekEnddate : calenders[0].ValidTo;
                var pairCalendar1 = new PropValuePair[]
                {
                    PropValuePair.GenereteWhereElements(nameof(TMEmpCalendarLineClient.Calendar), typeof(string), calenders[0].Calendar),
                    PropValuePair.GenereteWhereElements(nameof(TMEmpCalendarLineClient.Date), typeof(DateTime), String.Format("{0:d}..{1:d}", fromDate, toDate))
                };
                TMEmpCalendarLineClient[] lst1 = await api.Query<TMEmpCalendarLineClient>(pairCalendar1);

                fromDate = weekStartDate >= calenders[1].ValidFrom ? weekStartDate : calenders[1].ValidFrom;
                toDate = weekEnddate <= calenders[1].ValidTo || calenders[1].ValidTo == DateTime.MinValue ? weekEnddate : calenders[1].ValidTo;
                var pairCalendar2 = new PropValuePair[]
                {
                    PropValuePair.GenereteWhereElements(nameof(TMEmpCalendarLineClient.Calendar), typeof(string), calenders[1].Calendar),
                    PropValuePair.GenereteWhereElements(nameof(TMEmpCalendarLineClient.Date), typeof(DateTime), String.Format("{0:d}..{1:d}", fromDate, toDate))
                };
                TMEmpCalendarLineClient[] lst2 = await api.Query<TMEmpCalendarLineClient>(pairCalendar2);

                var lstMerge = lst1?.Concat(lst2).ToList();

                if (lstMerge != null)
                {
                    DateTime date = weekStartDate;
                    while (date < weekEnddate)
                    {
                        foreach (var calDay in lstMerge)
                        {
                            if (calDay.Date == date)
                            {
                                hours += calDay.Hours;
                                break;
                            }
                        }

                        date = date.AddDays(1);
                    }
                }

                return hours;
            }

            return 0;
        }

        string GetMissingRecordsStatus(DateTime startDate, double totalHours)
        {
            StatusType status = StatusType.Missing ;
            var firstDayOfWeek = FirstDayOfWeek(System.DateTime.Today);
            if (startDate >= firstDayOfWeek && startDate <= firstDayOfWeek.AddDays(6))
                status = StatusType.Empty;
            else if (totalHours < 0)
                status = StatusType.Missing;
            else if (totalHours >= 0)
                status = StatusType.DiffGreaterThanZero;
            return AppEnums.StatusType.ToString((int)status);
        }

        string GetStatus(EmployeeClient employee, DateTime startDate, double totalHours)
        {
            StatusType status = StatusType.Missing;
            var firstDayOfWeek = FirstDayOfWeek(System.DateTime.Today);
            if (startDate >= firstDayOfWeek && startDate <= firstDayOfWeek.AddDays(6))
                status = StatusType.Empty;
            else if (employee._TMCloseDate == DateTime.MinValue && employee._TMApproveDate == DateTime.MinValue)
            {
                if (totalHours < 0)
                    status = StatusType.Missing;
                else if (totalHours >= 0)
                    status = StatusType.DiffGreaterThanZero;
            }
            else if (startDate > employee._TMCloseDate)
            {
                if (totalHours < 0)
                    status = StatusType.Missing;
                else if (totalHours >= 0)
                    status = StatusType.DiffGreaterThanZero;
            }
            else if (startDate <= employee._TMApproveDate && employee._TMApproveDate < startDate.AddDays(6) && employee._TMCloseDate > employee._TMApproveDate)
                status = StatusType.ApprovedClosed;
            else if (startDate <= employee._TMApproveDate && employee._TMApproveDate < startDate.AddDays(6))
                status = StatusType.ApprovedEmpty;
            else if (startDate <= employee._TMCloseDate && employee._TMCloseDate < startDate.AddDays(6) && (employee._TMApproveDate == DateTime.MinValue || startDate >= employee._TMApproveDate))
                status = StatusType.ClosedEmpty;
            else if (startDate <= employee._TMCloseDate && (employee._TMApproveDate == DateTime.MinValue || startDate >= employee._TMApproveDate))
                status = StatusType.Closed;
            else if (startDate <= employee._TMCloseDate && startDate <= employee._TMApproveDate && employee._TMApproveDate < startDate.AddDays(6))
                status = StatusType.ApprovedClosed;
            else if (startDate <= employee._TMCloseDate && startDate <= employee._TMApproveDate)
                status = StatusType.Approved;
            else if (employee._TMCloseDate == DateTime.MinValue && startDate <= employee._TMApproveDate)
                status = StatusType.Approved;
                return AppEnums.StatusType.ToString((int)status); 
        }
        DateTime FirstDayOfWeek(DateTime selectedDate)
        {
            var dt = selectedDate;
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        string GetPeriod(DateTime Date)
        {
            var dayOffset = DayOfWeek.Thursday - Date.DayOfWeek;
            if (dayOffset > 0)
                Date = Date.AddDays(dayOffset);

            var week = CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(Date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString();

            return string.Format("{0}_{1}", Date.Year, week.PadLeft(2, '0'));
        }

        string  GetEmployeeGroupName(EmployeeClient employee) //TODO:Ligger denne ikke på Client-class - der må være en smartere måde
        {
            var GroupName = string.Empty;
            if (employee != null)
            {
                if (employee._Group != null)
                {
                    var rec = ClientHelper.GetRef(api.CompanyId, typeof(Uniconta.DataModel.EmployeeGroup), employee._Group);
                    GroupName = ((Uniconta.DataModel.EmployeeGroup)rec)?._Name;
                }
            }
            return GroupName;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTimeSheetApprovalRpt.SelectedItem as TimeSheetApprovalLocalClient;
            switch (ActionType)
            {
                case "RefreshGrid":
                    LoadGrid();
                    break;
                case "GroupByEmployee":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.GroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension4");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByEmployeeGroup":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.GroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension4");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByPeriod":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.GroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension4");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension1":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.GroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension4");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension2":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.GroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension4");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension3":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.GroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension4");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension4":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.GroupBy("Dimension4");
                    dgTimeSheetApprovalRpt.UngroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension5":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.GroupBy("Dimension5");
                    dgTimeSheetApprovalRpt.UngroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension4");
                    break;
                case "UnGroupAll":
                    if (dgTimeSheetApprovalRpt.ItemsSource == null) return;
                    dgTimeSheetApprovalRpt.UngroupBy("Employee");
                    dgTimeSheetApprovalRpt.UngroupBy("EmployeeGroup");
                    dgTimeSheetApprovalRpt.UngroupBy("Period");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension1");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension2");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension3");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension4");
                    dgTimeSheetApprovalRpt.UngroupBy("Dimension5");
                    break;
                case "TimeSheet":
                    if (selectedItem != null)
                    {
                        var employee = selectedItem.EmplyeeRef;
                        var startDate = selectedItem.Date;
                        var param = new object[2] { employee, startDate };
                        AddDockItem(TabControls.TMJournalLinePage, param, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("TimeRegistration"), employee._Name));
                    }
                    break;
                 default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            var comp = api.CompanyEntity;
            LoadType(new Type[] { typeof(Uniconta.DataModel.TMEmpCalendar) });

            if (employeeCache == null)
                employeeCache = await api.LoadCache(typeof(Uniconta.DataModel.Employee)).ConfigureAwait(false);

            if (empPayrollCatCache == null)
                empPayrollCatCache = await api.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory)).ConfigureAwait(false);
        }

        internal class Calender
        {
            public DateTime StartDate { get; set; }
            public int CalenderId { get; set; }
            public double NormalHours { get; set; }
        }
    }

    public class TimeSheetApprovalLocalClient :  INotifyPropertyChanged
    {
        string _Employee;
        [Display(Name = "Employee", ResourceType = typeof(ProjectTransClientText))]
        public string Employee { get { return _Employee; } set { if (ClientHelper.KeyEqual(_Employee, value)) return; _Employee = value; NotifyPropertyChanged("Employee"); } }

        [Display(Name = "EmployeeName", ResourceType = typeof(ProjectTransClientText))]
        public string EmployeeName { get; set; }

        [Display(Name = "Group", ResourceType = typeof(TMJournalLineText))]
        public string EmployeeGroup { get; set; }

        [Display(Name = "GroupName", ResourceType = typeof(TMJournalLineText))]
        public string EmployeeGroupName { get; set; }

        [Display(Name = "StartDate", ResourceType = typeof(ProjectText))]
        public DateTime Date { get; set; }
        
        double _Total = 0d;
        [Display(Name = "RegisteredHours", ResourceType = typeof(TMJournalLineText))]
        public double Total { get { return _Total; } set { _Total = value; NotifyPropertyChanged("Total"); } }

        double _NormHours = 0d;
        [Display(Name = "NormHours", ResourceType = typeof(TMJournalLineText))]
        public double NormHours { get { return _NormHours; } set { _NormHours = value; NotifyPropertyChanged("NormHours"); NotifyPropertyChanged("TotalHours"); } }

        double _TotalHours = 0d;
        [Display(Name = "Dif", ResourceType = typeof(TMJournalLineText))]
        public double TotalHours { get { return _TotalHours = Total - _NormHours; }  }

        double _Mileage = 0d;
        [Display(Name = "Mileage", ResourceType = typeof(TMJournalLineText))]
        public double Mileage { get { return _Mileage; } set { _Mileage = value; NotifyPropertyChanged("Mileage"); } }

        double _MileageYTD = 0d;
        [Display(Name = "MileageYTD", ResourceType = typeof(TMJournalLineText))]
        public double MileageYTD { get { return _MileageYTD; } set { _MileageYTD = value; NotifyPropertyChanged("MileageYTD"); } }

        double _Vacation = 0d;
        [Display(Name = "Vacation", ResourceType = typeof(TMJournalLineText))]
        public double Vacation { get { return _Vacation; } set { _Vacation = value; NotifyPropertyChanged("Vacation"); } }

        double _VacationYTD = 0d;
        [Display(Name = "VacationYTD", ResourceType = typeof(TMJournalLineText))]
        public double VacationYTD { get { return _VacationYTD; } set { _VacationYTD = value; NotifyPropertyChanged("VacationYTD"); } }

        double _OtherVacation = 0d;
        [Display(Name = "OtherVacation", ResourceType = typeof(TMJournalLineText))]
        public double OtherVacation { get { return _OtherVacation; } set { _OtherVacation = value; NotifyPropertyChanged("OtherVacation"); } }

        double _OtherVacationYTD = 0d;
        [Display(Name = "OtherVacationYTD", ResourceType = typeof(TMJournalLineText))]
        public double OtherVacationYTD { get { return _OtherVacationYTD; } set { _OtherVacationYTD = value; NotifyPropertyChanged("OtherVacationYTD"); } }

        double _FlexTime = 0d;
        [Display(Name = "FlexTime", ResourceType = typeof(TMJournalLineText))]
        public double FlexTime { get { return _FlexTime; } set { _FlexTime = value; NotifyPropertyChanged("FlexTime"); } }

        double _FlexTimeYTD = 0d;
        [Display(Name = "FlexTimeYTD", ResourceType = typeof(TMJournalLineText))]
        public double FlexTimeYTD { get { return _FlexTimeYTD; } set { _FlexTimeYTD = value; NotifyPropertyChanged("FlexTimeYTD"); } }

        double _OverTime = 0d;
        [Display(Name = "OverTime", ResourceType = typeof(TMJournalLineText))]
        public double OverTime { get { return _OverTime; } set { _OverTime = value; NotifyPropertyChanged("OverTime"); } }

        double _OverTimeYTD = 0d;
        [Display(Name = "OverTimeYTD", ResourceType = typeof(TMJournalLineText))]
        public double OverTimeYTD { get { return _OverTimeYTD; } set { _OverTimeYTD = value; NotifyPropertyChanged("OverTimeYTD"); } }

        double _Sickness = 0d;
        [Display(Name = "Sickness", ResourceType = typeof(TMJournalLineText))]
        public double Sickness { get { return _Sickness; } set { _Sickness = value; NotifyPropertyChanged("Sickness"); } }

        double _OtherAbsence = 0d;
        [Display(Name = "OtherAbsence", ResourceType = typeof(TMJournalLineText))]
        public double OtherAbsence { get { return _OtherAbsence; } set { _OtherAbsence = value; NotifyPropertyChanged("OtherAbsence"); } }

        double _Absence = 0d;
        [Display(Name = "Absence", ResourceType = typeof(TMJournalLineText))]
        public double Absence { get { return _Absence; } set { _Absence = value; NotifyPropertyChanged("Absence"); } }

        double _InvoiceableHours = 0d;
        [Display(Name = "Invoiceable", ResourceType = typeof(ProjectTransClientText))]
        public double InvoiceableHours { get { return _InvoiceableHours; } set { _InvoiceableHours = value; NotifyPropertyChanged("InvoiceableHours"); } }

        double _NotInvoiceableHours = 0d;
        [Display(Name = "NotInvoiceable", ResourceType = typeof(TMJournalLineText))]
        public double NotInvoiceableHours { get { return _NotInvoiceableHours; } set { _NotInvoiceableHours = value; NotifyPropertyChanged("NotInvoiceableHours"); } }

        double _EfficiencyPercentage = 0d;
        [Display(Name = "EfficiencyPercentage", ResourceType = typeof(TMJournalLineText))]
        public double EfficiencyPercentage { get { return _EfficiencyPercentage; } set { _EfficiencyPercentage = value; NotifyPropertyChanged("EfficiencyPercentage"); } }

        [Display(Name = "Period", ResourceType = typeof(TMJournalLineText))]
        public string Period { get; set; }

        [Display(Name = "ClosedBy", ResourceType = typeof(TMJournalLineText))]
        public string TMClosedBy { get; set; }

        [Display(Name = "ApprovedBy", ResourceType = typeof(TMJournalLineText))]
        public string TMApprovedBy { get; set; }

        StatusType _Status;
        [Display(Name = "Status", ResourceType = typeof(TMJournalLineText))]
        public string Status
        {
            get
            {
                return AppEnums.StatusType.ToString((int)_Status); ;
            }
            set
            {
                if (value == null) return; _Status = (StatusType)AppEnums.StatusType.IndexOf(value);
                NotifyPropertyChanged("Status");
            }
        }

        public string Dimension1 { get; set; }
        public string Dimension2 { get; set; }
        public string Dimension3 { get; set; }
        public string Dimension4 { get; set; }
        public string Dimension5 { get; set; }

        public EmployeeClient EmplyeeRef { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum StatusType
    {
        Empty, // Empty
        Closed, // Light Yellow Color
        Approved, // Green
        Missing, // Red
        ApprovedClosed, // Light Green and Light Yellow 
        ClosedEmpty, // Light Yellow and Light Red
        ApprovedEmpty, // Green and Light Red
        DiffGreaterThanZero // Light Red
    }
}
