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
using UnicontaClient.Pages.Project.TimeManagement;
using static UnicontaClient.Pages.Project.TimeManagement.TMJournalLineHelper;
using System.Text.RegularExpressions;
using Uniconta.API.GeneralLedger;
using Uniconta.API.Project;
using Uniconta.Common.Utility;
using System.Collections;


//TOOD: I MainPage.xaml.cs kigges der ikke på dato mht. Godkender Lone har alle selvom hun ikke længere skulle være aktiv

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
        UnicontaAPI.Project.API.PostingAPI postingApi;

        SQLCache CategoryCache, ItemCache;
        SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> payrollCache;
        SQLTableCache<Uniconta.DataModel.Project> projCache;
        SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupCache;
        SQLTableCache<Uniconta.DataModel.Employee> emplCache;

        public TimeSheetApprovalReport(UnicontaBaseEntity master, TMApprovalSetupClient[] approvalList) : base(master)
        {
            InitializeComponent();
            postingApi =  new UnicontaAPI.Project.API.PostingAPI(api);
            fromDate = fromDate == DateTime.MinValue ? GetSystemDefaultDate().AddMonths(-1) : fromDate;
            approverLst = approvalList;
            employee = master as Uniconta.DataModel.Employee;
            localMenu.dataGrid = dgTimeSheetApprovalRpt;
            dgTimeSheetApprovalRpt.RowDoubleClick += dgTimeSheetApprovalRpt_RowDoubleClick;
            SetRibbonControl(localMenu, dgTimeSheetApprovalRpt);
            dgTimeSheetApprovalRpt.api = api;
            dgTimeSheetApprovalRpt.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgTimeSheetApprovalRpt.ShowTotalSummary();
            dgTimeSheetApprovalRpt.tableView.AllowFixedColumnMenu = true;
            dgTimeSheetApprovalRpt.tableView.GroupSummaryDisplayMode = GroupSummaryDisplayMode.AlignByColumns;

            projCache = api.GetCache<Uniconta.DataModel.Project>();
            payrollCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>();
            projGroupCache = api.GetCache<Uniconta.DataModel.ProjectGroup>();
            CategoryCache = api.GetCache(typeof(Uniconta.DataModel.PrCategory));
            ItemCache = api.GetCache(typeof(Uniconta.DataModel.InvItem));

            StartLoadCache();
        }

        public override Task InitQuery()
        {
            return LoadGrid();
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
            var grp = Uniconta.ClientTools.Localization.lookup("GroupByOBJ");
            if (ibase1 != null)
                ibase1.Caption = c._Dim1 != null ? string.Format(grp, c._Dim1) : string.Empty;
            if (ibase2 != null)
                ibase2.Caption = c._Dim2 != null ? string.Format(grp, c._Dim2) : string.Empty;
            if (ibase3 != null)
                ibase3.Caption = c._Dim3 != null ? string.Format(grp, c._Dim3) : string.Empty;
            if (ibase4 != null)
                ibase4.Caption = c._Dim4 != null ? string.Format(grp, c._Dim4) : string.Empty;
            if (ibase5 != null)
                ibase5.Caption = c._Dim5 != null ? string.Format(grp, c._Dim5) : string.Empty;
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

        private class DictMappedValue
        {
            public double Hours { get; set; }
            public int CountDays { get; set; }
        }

        class SortTransEmp : IComparer<ProjectTransClient>
        {
            public int Compare(ProjectTransClient x, ProjectTransClient y)
            {
                var c = string.Compare(x._Employee, y._Employee);
                if (c != 0)
                    return c;
                return DateTime.Compare(x._Date, y._Date);
            }
        }

        class SortApproval : IComparer<TimeSheetApprovalLocalClient>
        {
            public int Compare(TimeSheetApprovalLocalClient x, TimeSheetApprovalLocalClient y)
            {
                var c = string.Compare(x._Employee, y._Employee);
                if (c != 0)
                    return c;
                return DateTime.Compare(x.Date, y.Date);
            }
        }

        DateTime queryTransStartDate = DateTime.Today;
        
        SQLTableCache<Uniconta.ClientTools.DataModel.EmpPayrollCategoryClient> empPayrollCatList;

        double mileageYTD;
        double mileageYTDnextPeriod;

        double vacationYTD;
        double vacationYTDnextPeriod;
        double vacationPrimoNextPeriod;

        double otherVacationYTD;
        double otherVacationYTDnextPeriod;
        double otherVacationPrimoNextPeriod;

        double overTimeYTD;
        double flexTimeYTD;

        bool internalActive;
        string mileageInternalProject;
        HashSet<string> lstCatMileage, lstCatVacation, lstCatOtherVacation, lstCatFlexTime, lstCatOverTime, lstCatSickness, lstCatOtherAbsence;
        HashSet<string> lstCatPayProj, lstCatPay;

        async Task LoadGrid()
        {
            busyIndicator.IsBusy = true;

            FindNormHours.RefreshBaseData();

            emplCache = await api.LoadCache<Uniconta.DataModel.Employee>(true); //force
            
            empPayrollCatList = await api.LoadCache<Uniconta.ClientTools.DataModel.EmpPayrollCategoryClient>();

            foreach (var val in empPayrollCatList)
            {
                if (val._InternalType == 0)
                    continue;

                internalActive = true;
                switch (val._InternalType)
                {
                    case InternalType.FlexTime: AddToList(ref lstCatFlexTime, val._Number); break;
                    case InternalType.Vacation: AddToList(ref lstCatVacation, val._Number); break;
                    case InternalType.OtherVacation: AddToList(ref lstCatOtherVacation, val._Number); break;
                    case InternalType.OverTime: AddToList(ref lstCatOverTime, val._Number); break;
                    case InternalType.Sickness: AddToList(ref lstCatSickness, val._Number); break;
                    case InternalType.OtherAbsence: AddToList(ref lstCatOtherAbsence, val._Number); break;
                    case InternalType.Mileage:
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

            var empDistinct = string.Empty;
            if (approverLst.Length <= 30)
            {
                var empLst = approverLst.Select(x => x?._Employee).Distinct();
                if (empLst != null)
                    empDistinct = string.Join(";", empLst);
            }
            else
                empDistinct = "!null";

            var catPayDist = lstCatPay != null ? string.Join(";", lstCatPay) : string.Empty;
            var catPayProjDist = lstCatPayProj != null ? string.Join(";", lstCatPayProj) : string.Empty;

            var pairInternalTrans = new PropValuePair[]
            {
                 PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Employee), typeof(string), empDistinct),
                 PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Project), typeof(string), catPayProjDist),
                 PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.PayrollCategory), typeof(string), catPayDist),
            };
            var internalTransLst = await api.Query<ProjectTransClient>(pairInternalTrans);

            if (lstCatMileage != null)
            {
                var mileageCatDist = string.Join(";", lstCatMileage);

                var pairmileageTrans = new PropValuePair[]
                {
                    PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Employee), typeof(string), empDistinct),
                    PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.PayrollCategory), typeof(string), mileageCatDist),
                };
                var mileageTrans = await api.Query<ProjectTransClient>(pairmileageTrans);
                if (mileageTrans != null && mileageTrans.Length > 0)
                {
                    var orgLen = internalTransLst.Length;
                    if (orgLen == 0)
                        internalTransLst = mileageTrans;
                    else
                    {
                        Array.Resize(ref internalTransLst, orgLen + mileageTrans.Length);
                        Array.Copy(mileageTrans, 0, internalTransLst, orgLen, mileageTrans.Length);
                    }
                }
            }

            foreach (var rec in approverLst)
            {
                var empl = rec._Employee is null ? null : emplCache.Get(rec._Employee);
                if (empl is null)
                    continue;

                var dt = empl._TMApproveDate != DateTime.MinValue ? empl._TMApproveDate : empl._Hired;
                if (dt < queryTransStartDate)
                    queryTransStartDate = dt;
            }

            var empInternalTransLst = new List<ProjectTransClient>(100);
            var searchTrans = new ProjectTransClient();
            var transSort = new SortTransEmp();
            Array.Sort(internalTransLst, transSort);

            queryTransStartDate = queryTransStartDate.AddDays(-6);
            var pairTM = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Employee), typeof(string), empDistinct),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), typeof(DateTime), String.Format("{0:d}..", queryTransStartDate))
            };
            var journalLineLst = await api.Query<TMJournalLineClient>(pairTM);

            var empJournalLineLst = new List<TMJournalLineClient>(100);
            var searchJour = new TMJournalLineClient();
            var jourSort = new TMJournalEmpDateSort();
            Array.Sort(journalLineLst, jourSort);

            var tsApprovalLst = new List<TimeSheetApprovalLocalClient>(approverLst.Length);
            foreach (var rec in approverLst)
            {
                var CurEmployee = rec._Employee;
                if (CurEmployee == null)
                    continue;

                var empl = emplCache.Get(CurEmployee);
                if (empl == null)
                    continue;

                var startDate = empl._TMApproveDate != DateTime.MinValue ? empl._TMApproveDate : empl._Hired != DateTime.MinValue ? empl._Hired : DateTime.Today;
                startDate = rec._ValidFrom > startDate ? rec._ValidFrom : startDate;
                var monday = startDate.AddDays(-(int)(startDate.DayOfWeek - DayOfWeek.Monday));
                var startDateMonday = monday.AddDays(-1);

                var endDate = empl._Terminated == DateTime.MinValue ? DateTime.Today : empl._Terminated;
                endDate = rec._ValidTo < endDate ? rec._ValidTo : endDate;


                #region Internal Registration

                mileageYTD = vacationYTD = vacationPrimoNextPeriod = otherVacationYTD = otherVacationPrimoNextPeriod = overTimeYTD = flexTimeYTD = 0;
                empInternalTransLst.Clear();

                int pos;
                if (internalActive)
                {
                    searchTrans._Employee = CurEmployee;
                    pos = Array.BinarySearch(internalTransLst, searchTrans, transSort);
                    if (pos < 0)
                        pos = ~pos;
                    while (pos < internalTransLst.Length)
                    {
                        var s = internalTransLst[pos++];
                        if (s._Employee != CurEmployee)
                            break;
                        empInternalTransLst.Add(s);
                    }

                    if (empInternalTransLst.Count > 0)
                    {
                        double sum;

                        #region Mileage
                        if (lstCatMileage != null)
                        {
                            sum = 0;
                            var mileageStartDate = new DateTime(startDate.Year, 1, 1);
                            foreach (var s in empInternalTransLst)
                            {
                                if ((s._Date >= mileageStartDate && s._Date <= startDate) && lstCatMileage.Contains(s._PayrollCategory))
                                    sum += s._Qty;
                            }
                            mileageYTD = sum;
                        }
                        #endregion

                        #region Vacation
                        if (lstCatVacation != null)
                        {
                            var vacationStartDate = startDate < new DateTime(2023, 1, 1) ? new DateTime(startDate.Year, 1, 1) : new DateTime(2023, 1, 1);

                            if (startDate < vacationStartDate)
                                vacationStartDate = vacationStartDate.AddYears(-1);

                            startDate = startDate == vacationStartDate ? startDate.AddDays(1) : startDate;
                            var vacationStartDateNext = new DateTime(vacationStartDate.Year + 1, 1, 1);

                            sum = 0;
                            foreach (var s in empInternalTransLst)
                            {
                                if (lstCatVacation.Contains(s._PayrollCategory))
                                {
                                    if (s._Date >= vacationStartDate && s._Date <= startDate)
                                        sum += s._Qty;
                                    else if (s._Date == vacationStartDateNext)
                                        vacationPrimoNextPeriod += s._Qty;
                                }
                            }
                            vacationYTD = sum;
                            vacationPrimoNextPeriod += sum;
                        }
                        #endregion

                        #region Other vacation
                        if (lstCatOtherVacation != null)
                        {
                            var otherVacationStartDate = startDate < new DateTime(2023, 1, 1) ? new DateTime(startDate.Year, 1, 1) : new DateTime(2023, 1, 1);

                            if (startDate < otherVacationStartDate)
                                otherVacationStartDate = otherVacationStartDate.AddYears(-1);

                            var otherVacationStartDateNext = new DateTime(otherVacationStartDate.Year + 1, 1, 1);

                            sum = 0;
                            foreach (var s in empInternalTransLst)
                            {
                                if (lstCatOtherVacation.Contains(s._PayrollCategory))
                                {
                                    if (s._Date >= otherVacationStartDate && s._Date < startDate)
                                        sum += s._Qty;
                                    else if (s._Date == otherVacationStartDateNext)
                                        otherVacationPrimoNextPeriod += s._Qty;
                                }
                            }
                            otherVacationYTD = sum;
                            otherVacationPrimoNextPeriod += sum;
                        }
                        #endregion

                        #region Overtime
                        if (lstCatOverTime != null)
                        {
                            sum = 0;
                            foreach (var s in empInternalTransLst)
                            {
                                if (s._Date <= startDateMonday && lstCatOverTime.Contains(s._PayrollCategory))
                                    sum += s._Qty;
                            }
                            overTimeYTD = -sum;
                        }
                        #endregion

                        #region FlexTime
                        if (lstCatFlexTime != null)
                        {
                            sum = 0;
                            foreach (var s in empInternalTransLst)
                            {
                                if (s._Date <= startDateMonday && lstCatFlexTime.Contains(s._PayrollCategory))
                                    sum += s._Qty;
                            }
                            flexTimeYTD = -sum;
                        }
                        #endregion FlexTime

                    }
                }
                #endregion Internal Registration

                empJournalLineLst.Clear();
                searchJour._Employee = CurEmployee;
                pos = Array.BinarySearch(journalLineLst, searchJour, jourSort);
                if (pos < 0)
                    pos = ~pos;
                while (pos < journalLineLst.Length)
                {
                    var s = journalLineLst[pos++];
                    if (s._Employee != CurEmployee)
                        break;
                    empJournalLineLst.Add(s);
                }
                var linesLst = await CreateApprovalList(empl, empJournalLineLst, startDateMonday, endDate);
                tsApprovalLst.AddRange(linesLst);
            }

            tsApprovalLst.Sort(new SortApproval());
            dgTimeSheetApprovalRpt.ItemsSource = tsApprovalLst;
            dgTimeSheetApprovalRpt.Visibility = Visibility.Visible;

            busyIndicator.IsBusy = false;
        }


        static double GetFactor(dynamic empPayCat) // eller PayrollCategory type
        {
            var f = empPayCat?._Factor ?? 1d;
            return f == 0 ? 1d : f;
        }

        static bool IsInternalTracked(InternalType t) =>
            t == InternalType.Sickness ||
            t == InternalType.OtherAbsence ||
            t == InternalType.FlexTime ||
            t == InternalType.OverTime;

        async Task<List<TimeSheetApprovalLocalClient>> CreateApprovalList(Uniconta.DataModel.Employee empl, List<TMJournalLineClient> empJournalLineLst, DateTime startDate, DateTime endDate)
        {
            bool mileageNextYear = false;
            bool vacationNextYear = false;
            bool otherVacationNextYear = false;
            DateTime firstMondayVacationYear = DateTime.MinValue;
            DateTime currentVacationYear = DateTime.MinValue;

            var lines = new List<TimeSheetApprovalLocalClient>();

            int offset = ((int)DayOfWeek.Monday - (int)startDate.DayOfWeek + 7) % 7;
            for (var monday = startDate.AddDays(offset); monday <= endDate; monday = monday.AddDays(7))
            {
                if (empl._Terminated.HasValue() && empl._Terminated <= empl._TMApproveDate)
                    continue;

                var weekEnd = monday.AddDays(6);
                bool crossesYear = monday.Year != weekEnd.Year;

                var dayLines = empJournalLineLst.Where(x => x._Date == monday).ToList();

                double mileageWeek = 0;
                if (lstCatMileage != null)
                {
                    var mileageLines = dayLines
                        .Where(x => x._RegistrationType == RegistrationType.Mileage &&
                                    x._InternalType == InternalType.Mileage)
                        .ToList();

                    if (mileageLines.Count > 0)
                    {
                        if (crossesYear)
                        {
                            foreach (var trans in mileageLines)
                            {
                                for (int d = 1; d <= 7; d++)
                                {
                                    var qty = trans.GetHoursDayN(d);
                                    if (qty == 0) continue;

                                    if (monday.Year != monday.AddDays(d - 1).Year)
                                        mileageYTDnextPeriod += qty;

                                    mileageWeek += qty;
                                }
                            }
                        }
                        else
                        {
                            mileageWeek = mileageLines.Sum(x => x.Total);
                        }

                        mileageYTD += mileageWeek - mileageYTDnextPeriod;
                    }
                    else if (crossesYear)
                    {
                        mileageYTD = 0;
                    }

                    if (mileageNextYear)
                    {
                        mileageYTD = mileageYTDnextPeriod + mileageWeek;
                        mileageYTDnextPeriod = 0;
                        mileageNextYear = false;
                    }
                }

                // ---------- Vacation / OtherVacation ----------
                double vacationWeek = 0;
                double otherVacationWeek = 0;
                if (lstCatVacation != null || lstCatOtherVacation != null)
                {
                    if (firstMondayVacationYear == DateTime.MinValue)
                        firstMondayVacationYear = monday < new DateTime(monday.Year, 5, 1)
                            ? new DateTime(monday.Year - 1, 5, 1)
                            : new DateTime(monday.Year, 5, 1);

                    currentVacationYear = weekEnd < new DateTime(monday.Year, 5, 1)
                        ? new DateTime(monday.Year - 1, 5, 1)
                        : new DateTime(monday.Year, 5, 1);

                    var vacationLines = dayLines
                        .Where(x => x._RegistrationType == RegistrationType.Hours &&
                                    (x._InternalType == InternalType.Vacation || x._InternalType == InternalType.OtherVacation))
                        .ToList();

                    if (vacationLines.Count > 0)
                    {
                        if (crossesYear)
                        {
                            foreach (var trans in vacationLines)
                            {
                                for (int d = 1; d <= 7; d++)
                                {
                                    var qty = trans.GetHoursDayN(d);
                                    if (qty == 0) continue;

                                    if (monday.Year != monday.AddDays(d - 1).Year)
                                    {
                                        if (trans._InternalType == InternalType.Vacation) vacationYTDnextPeriod += qty;
                                        else otherVacationYTDnextPeriod += qty;
                                    }

                                    if (trans._InternalType == InternalType.Vacation) vacationWeek += qty;
                                    else otherVacationWeek += qty;
                                }
                            }
                        }
                        else
                        {
                            foreach (var g in vacationLines.GroupBy(x => x._InternalType))
                            {
                                var sum = g.Sum(y => y.Total);
                                if (g.Key == InternalType.Vacation) vacationWeek = sum;
                                else otherVacationWeek = sum;
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

                // ---------- Hours (base) ----------
                var hoursLines = dayLines.Where(x => x._RegistrationType == RegistrationType.Hours).ToList();
                var totalHours = hoursLines.Count > 0 ? hoursLines.Sum(x => x.Total) : 0d;

                var period = Project.TimeManagement.TMJournalLineHelper.GetPeriod(monday);
                var normHours = await FindNormHours.GetByWeek(api, empl._Number, monday);
                var tsApproval = new TimeSheetApprovalLocalClient { _CompanyId = api.CompanyId };
                tsApproval._Employee = empl._Number;
                tsApproval._EmployeeGroup = empl._Group;
                tsApproval.Date = monday;
                tsApproval.Total = totalHours;
                tsApproval.Period = period;
                tsApproval.Dimension1 = empl._Dim1;
                tsApproval.Dimension2 = empl._Dim2;
                tsApproval.Dimension3 = empl._Dim3;
                tsApproval.Dimension4 = empl._Dim4;
                tsApproval.Dimension5 = empl._Dim5;

                tsApproval.NormHours = normHours;
                tsApproval.Status = GetStatus(empl, monday, tsApproval.TotalHours);

                tsApproval.Mileage = mileageWeek;
                tsApproval.MileageYTD = mileageYTD;

                tsApproval.Vacation = vacationWeek;
                tsApproval.VacationYTD = -vacationYTD;

                tsApproval.OtherVacation = otherVacationWeek;
                tsApproval.OtherVacationYTD = -otherVacationYTD;

                // ---------- Internal tracked (Sickness/OtherAbsence/OverTime/FlexTime) ----------
                if (lstCatSickness != null || lstCatOtherAbsence != null || lstCatFlexTime != null || lstCatOverTime != null)
                {
                    var internalLines = hoursLines.Where(x => IsInternalTracked(x._InternalType)).ToList();

                    if (internalLines.Count > 0)
                    {
                        foreach (var grp in internalLines.GroupBy(x => new { x._InternalType, x._PayrollCategory }))
                        {
                            var sum = grp.Sum(y => y.Total);

                            switch (grp.Key._InternalType)
                            {
                                case InternalType.Sickness:
                                    tsApproval.Sickness += sum;
                                    break;

                                case InternalType.OtherAbsence:
                                    tsApproval.OtherAbsence += sum;
                                    break;

                                case InternalType.OverTime:
                                    {
                                        var empPayCat = payrollCache.Get(grp.Key._PayrollCategory);
                                        tsApproval.OverTime -= sum * GetFactor(empPayCat);
                                        break;
                                    }

                                case InternalType.FlexTime:
                                    {
                                        var empPayCat = payrollCache.Get(grp.Key._PayrollCategory);
                                        tsApproval.FlexTime -= sum * GetFactor(empPayCat);
                                        break;
                                    }
                            }
                        }
                    }

                    overTimeYTD += tsApproval.OverTime;
                    tsApproval.OverTimeYTD = overTimeYTD;

                    flexTimeYTD += tsApproval.FlexTime;
                    tsApproval.FlexTimeYTD = flexTimeYTD;
                }
                else
                {
                    tsApproval.OverTimeYTD = overTimeYTD;
                    tsApproval.FlexTimeYTD = flexTimeYTD;
                }

                // ---------- Efficiency percentage ----------
                // kun InternalType==0 og Hours
                var invLines = hoursLines.Where(x => x._InternalType == 0);
                double inv = 0, notInv = 0;
                foreach (var l in invLines)
                {
                    if (l._Invoiceable) inv += l.Total;
                    else notInv += l.Total;
                }

                tsApproval.InvoiceableHours = inv;
                tsApproval.NotInvoiceableHours = notInv;

                var denom = inv + notInv;
                tsApproval.EfficiencyPercentage = denom != 0 ? (inv / denom) * 100 : 0;

                // ---------- Settlement hours ----------
                tsApproval._SettlementHours = hoursLines
                    .Where(x => x.Total < 0 &&
                                (x._InternalType == InternalType.FlexTime || x._InternalType == InternalType.OverTime))
                    .Sum(x => x.Total);

                lines.Add(tsApproval);

                if (mileageYTDnextPeriod != 0)
                    mileageNextYear = true;

                if (lstCatVacation != null && firstMondayVacationYear.Year != currentVacationYear.Year)
                {
                    vacationNextYear = vacationYTDnextPeriod != 0 || vacationPrimoNextPeriod != 0;
                    otherVacationNextYear = otherVacationYTDnextPeriod != 0 || otherVacationPrimoNextPeriod != 0;
                }
            }

            return lines;
        }

        string GetStatus(Uniconta.DataModel.Employee employee, DateTime startDate, double totalHours)
        {
            StatusType status = StatusType.Missing;
            var firstDayOfWeek = TimeHelper.GetMondayOfWeek(DateTime.Today);
            if (startDate >= firstDayOfWeek && startDate <= firstDayOfWeek.AddDays(6) && startDate > employee._TMApproveDate && startDate > employee._TMCloseDate)
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

        void dgTimeSheetApprovalRpt_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("TimeSheet");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var dgTimeSheetApprovalRpt = this.dgTimeSheetApprovalRpt;
            var selectedItem = dgTimeSheetApprovalRpt.SelectedItem as TimeSheetApprovalLocalClient;
            var selectedItems = dgTimeSheetApprovalRpt.SelectedItems;

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
                        var employee = selectedItem.EmployeeRef;
                        AddDockItem(TabControls.TMJournalLinePage, dgTimeSheetApprovalRpt.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TimeRegistration"), employee._Name));
                    }
                    break;
                case "ActionValidate":
                    if (selectedItems == null) return;
                        ActionValidate(selectedItems);
                    break;
                case "ActionApprove":
                    var markedRows = selectedItems?.Cast<TimeSheetApprovalLocalClient>().ToList();
                    if (selectedItems != null)
                        ActionApprove(markedRows);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }


        int cntErr = 0, cntWarning = 0, cntOK = 0, cntJournals = 0;
        private async Task ActionValidate(IList journalList)
        {
            cntOK = cntWarning = cntErr = 0;
            cntJournals = journalList.Count;

            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
            busyIndicator.IsBusy = true;

            foreach (TimeSheetApprovalLocalClient rec in journalList)
            {
                var empl = (EmployeeClient)emplCache.Get(rec.Employee);
                var dateMonday = rec.Date;
                var approveDate = empl._TMCloseDate >= dateMonday && empl._TMCloseDate <= dateMonday.AddDays(6) ? empl._TMCloseDate : dateMonday.AddDays(6);
                var postingRes = await postingApi.ValidateTimeJournal(empl, approveDate, 3);

                await UpdateJournalInfo(postingRes, rec);
            }

            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

            ShowDialogInfo();
        }

        private async Task ActionApprove(List<TimeSheetApprovalLocalClient> journalList)
        {
            cntOK = cntWarning = cntErr = 0;

            var cntJournals = journalList.Count;
            var continueMsg = string.Format(Uniconta.ClientTools.Localization.lookup("JournalsMarkedApproval") + ". " +
                Uniconta.ClientTools.Localization.lookup("Accept") + "?", cntJournals);

            var shouldContinue = UnicontaMessageBox.Show(continueMsg, Uniconta.ClientTools.Localization.lookup("TimeSheetApproval"),
                MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (MessageBoxResult.Cancel.Equals(shouldContinue))
                return;

            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

            foreach (var rec in journalList)
            {
                var empl = (EmployeeClient)emplCache.Get(rec.Employee);
                var dateMonday = rec.Date;
                var approveDate = empl._TMCloseDate >= dateMonday && empl._TMCloseDate <= dateMonday.AddDays(6) ? empl._TMCloseDate : dateMonday.AddDays(6);
                var postingRes = await postingApi.ApproveTimeJournal(empl, approveDate, false, rec.ConfirmWarning);

                await UpdateJournalInfo(postingRes, rec, empl, true);
            }

            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

            ShowDialogInfo(true);
        }

        private void ShowDialogInfo(bool approve = false)
        {
            dgTimeSheetApprovalRpt.Columns.GetColumnByName("ErrorInfo").Visible = true;
            dgTimeSheetApprovalRpt.Columns.GetColumnByName("JournalStatus").Visible = true;
            dgTimeSheetApprovalRpt.Columns.GetColumnByName("ConfirmWarning").Visible = true;

            var sb = StringBuilderReuse.Create();
            if (!approve)
            {
                sb = StringBuilderReuse.Create().Append(Uniconta.ClientTools.Localization.lookup("NumberChecked")).Append(" ").
              Append(Uniconta.ClientTools.Localization.lookup("Journals").ToLower()).
              Append(": ").AppendLine(NumberConvert.ToString(cntJournals));
                sb.AppendLine().Append(Uniconta.ClientTools.Localization.lookup("Count")).Append(" ").Append(Uniconta.ClientTools.Localization.lookup("OK")).Append(": ").AppendLine(NumberConvert.ToString(cntOK));
            }
            else
            {
                sb = StringBuilderReuse.Create().AppendLine(string.Format(Uniconta.ClientTools.Localization.lookup("JournalsMarkedApproval"), cntJournals));
                sb.AppendLine().Append(Uniconta.ClientTools.Localization.lookup("Count")).Append(" ").Append(Uniconta.ClientTools.Localization.lookup("Approved").ToLower()).Append(": ").AppendLine(NumberConvert.ToString(cntOK));
            }

            if (cntErr > 0)
                sb.Append(Uniconta.ClientTools.Localization.lookup("NumberOfError")).Append(": ").AppendLine(NumberConvert.ToString(cntErr));
            else if (cntWarning > 0)
            {
                sb.Append(Uniconta.ClientTools.Localization.lookup("Count")).Append(" ").Append(Uniconta.ClientTools.Localization.lookup("Warning").ToLower()).
                    Append(": ").AppendLine(NumberConvert.ToString(cntWarning));

                sb.AppendLine().Append(string.Format(Uniconta.ClientTools.Localization.lookup("PleaseNotOBJ"),
                    string.Format(Uniconta.ClientTools.Localization.lookup("JournalWarningApprovement").ToLower(), string.Concat(Uniconta.ClientTools.Localization.lookup("Confirm"), " ", Uniconta.ClientTools.Localization.lookup("Warning").ToLower()))));
            }
            UnicontaMessageBox.Show(sb.ToStringAndRelease(), Uniconta.ClientTools.Localization.lookup("Information"));
        }

        private async Task UpdateJournalInfo(TMPostingResult result, TimeSheetApprovalLocalClient rec, EmployeeClient empl = null, bool approve = false)
        {
            var sb = StringBuilderReuse.Create();

            if (result == null)
                return;
        
            if (result.IsPrevalidation)
            {
                cntErr++;
                var line = result.Lines?.FirstOrDefault();
                var msgKey = line?.MessageText ?? Uniconta.ClientTools.Localization.lookup(result.Err.ToString());
                sb.Append(Uniconta.ClientTools.Localization.lookup(msgKey));
                rec.JournalStatus = TMLineStatus.Error.ToString();
                rec.ErrorInfo = sb.ToStringAndRelease();
                return;
            }

            bool successWithoutBlockingWarnings = result.Err == ErrorCodes.Succes && (result.CountWarnings == 0 || rec.ConfirmWarning);
            if (successWithoutBlockingWarnings)
            {
                cntOK++;
                rec.JournalStatus = TMLineStatus.OK.ToString();

                if (approve)
                {
                    await api.Read(empl);
                    rec.ErrorInfo = Uniconta.ClientTools.Localization.lookup("Approved");
                }
                else
                    rec.ErrorInfo = null;

                return;
            }

            if (result.CountErrors > 0)
            {
                cntErr++;
                rec.JournalStatus = TMLineStatus.Error.ToString();
                rec.ErrorInfo = string.Concat(Uniconta.ClientTools.Localization.lookup("NumberOfError"),": ", result.CountErrors);
            }
            else if (result.CountWarnings > 0 && !rec.ConfirmWarning)
            {
                cntWarning++;
                rec.JournalStatus = TMLineStatus.Warning.ToString();
                rec.ErrorInfo = string.Concat(Uniconta.ClientTools.Localization.lookup("Antal advarsler"), ": ", result.CountWarnings);
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            projCache = projCache ?? await api.LoadCache<Uniconta.DataModel.Project>().ConfigureAwait(false);
            payrollCache = payrollCache ?? await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>().ConfigureAwait(false);
            projGroupCache = projGroupCache ?? await api.LoadCache<Uniconta.DataModel.ProjectGroup>().ConfigureAwait(false);
            CategoryCache = CategoryCache ?? await api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).ConfigureAwait(false);
            ItemCache = ItemCache ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);

            LoadType(new Type[] { typeof(Uniconta.DataModel.EmployeeGroup) });
        }

        internal class Calender
        {
            public DateTime StartDate { get; set; }
            public int CalenderId { get; set; }
            public double NormalHours { get; set; }
        }
    }

    public class TimeSheetApprovalLocalClient : INotifyPropertyChanged, UnicontaBaseEntity
    {
        public int _CompanyId;

        public string _Employee;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "Employee", ResourceType = typeof(ProjectTransClientText))]
        public string Employee { get { return _Employee; } set { if (ClientHelper.KeyEqual(_Employee, value)) return; _Employee = value; NotifyPropertyChanged("Employee"); NotifyPropertyChanged("EmployeeName"); } }

        [Display(Name = "EmployeeName", ResourceType = typeof(ProjectTransClientText))]
        [NoSQL]
        public string EmployeeName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.Employee), _Employee); } }

        public string _EmployeeGroup;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.EmployeeGroup))]
        [Display(Name = "Group", ResourceType = typeof(TMJournalLineText))]
        public string EmployeeGroup { get { return _EmployeeGroup; } set { if (ClientHelper.KeyEqual(_EmployeeGroup, value)) return; _Employee = value; NotifyPropertyChanged("EmployeeGroup"); NotifyPropertyChanged("EmployeeGroupName"); } }

        [Display(Name = "GroupName", ResourceType = typeof(TMJournalLineText))]
        [NoSQL]
        public string EmployeeGroupName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.EmployeeGroup), _EmployeeGroup); } }

        [Display(Name = "StartDate", ResourceType = typeof(ProjectText))]
        public DateTime Date { get; set; }

        public double _SettlementHours = 0d;
        double _Total = 0d;
        [Display(Name = "RegisteredHours", ResourceType = typeof(TMJournalLineText))]
        public double Total { get { return _Total - _SettlementHours; } set { _Total = value; NotifyPropertyChanged("Total"); } }

        double _NormHours = 0d;
        [Display(Name = "NormHours", ResourceType = typeof(TMJournalLineText))]
        public double NormHours { get { return _NormHours; } set { _NormHours = value; NotifyPropertyChanged("NormHours"); NotifyPropertyChanged("TotalHours"); } }

        double _TotalHours = 0d;
        [Display(Name = "Dif", ResourceType = typeof(TMJournalLineText))]
        public double TotalHours { get { return _TotalHours = Total - _NormHours; } }

        double _Production = 0d;
        [Display(Name = "Production", ResourceType = typeof(TMJournalLineText))]
        public double Production { get { return _Production = _InvoiceableHours + _NotInvoiceableHours; } }

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

        public StatusType _Status;
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

        [Display(Name = "ConfirmWarning", ResourceType = typeof(TMJournalLineText))]
        public bool ConfirmWarning { get; set; }

        private string _ErrorInfo;
        [Display(Name = "SystemInfo", ResourceType = typeof(DCTransText))]
        public string ErrorInfo { get { return _ErrorInfo; } set { _ErrorInfo = value; NotifyPropertyChanged("ErrorInfo"); } }

        private int _JournalStatus;
        [AppEnumAttribute(EnumName = "JournalStatus")]
        [Display(Name = "JournalStatus", ResourceType = typeof(TMJournalLineText))]
        [NoSQL]
        public string JournalStatus { get { return AppEnums.LineStatus.ToString(_JournalStatus); } set { _JournalStatus = AppEnums.LineStatus.TryIndexOf(value); NotifyPropertyChanged("JournalStatus"); } }

        public string Dimension1 { get; set; }
        public string Dimension2 { get; set; }
        public string Dimension3 { get; set; }
        public string Dimension4 { get; set; }
        public string Dimension5 { get; set; }

        [ReportingAttribute]
        public EmployeeClient EmployeeRef
        {
            get
            {
                return ClientHelper.GetRefClient<EmployeeClient>(_CompanyId, typeof(Uniconta.DataModel.Employee), _Employee);
            }
        }

        [ReportingAttribute]
        public EmployeeGroupClient EmployeeGroupRef
        {
            get
            {
                return ClientHelper.GetRefClient<EmployeeGroupClient>(_CompanyId, typeof(Uniconta.DataModel.EmployeeGroup), _EmployeeGroup);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int CompanyId { get { return _CompanyId; } set { _CompanyId = value; } }
        public Type BaseEntityType() { return GetType(); }
        public void loadFields(CustomReader r, int SavedWithVersion) { }
        public void saveFields(CustomWriter w, int SaveVersion) { }
        public int Version(int ClientAPIVersion) { return 1; }
        public int ClassId() { return 37365; }
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
