
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.Project.TimeManagement;
using UnicontaClient.Utilities;
using static UnicontaClient.Pages.Project.TimeManagement.TMJournalLineHelper;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class WorkInProgressReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTransLocalClient); } }
    }

    public partial class WorkInProgressReportPage : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string FromPerInCharge { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string ToPerInCharge { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string FromDebtor { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string ToDebtor { get; set; }
     
        static string fromPerInCharge, toPerInCharge, fromDebtor, toDebtor;
        static DateTime DefaultFromDate, DefaultToDate;
        static bool includeZeroBalance;
        static bool showTask;
        static bool includeJournals;


        ItemBase iIncludeZeroBalance, iIncludeJournals, iShowTask;

        private const string AND_OPERATOR = "And";
        private const string FILTERVALUE_ZEROBALANCE = @"[ClosingBalance] <> 0";

        SQLTableCache<EmployeeClient> empCache;
        SQLTableCache<Uniconta.ClientTools.DataModel.ProjectClient> projCache;
        SQLTableCache<EmpPayrollCategory> payrollCache;
        SQLTableCache<ProjectGroupClient> projGroupCache;
        public WorkInProgressReportPage(BaseAPI API) : base(API, string.Empty)
        {
            this.DataContext = this;
            InitializeComponent();
            localMenu.dataGrid = dgWorkInProgressRpt;
            dgWorkInProgressRpt.RowDoubleClick += dgWorkInProgressRpt_RowDoubleClick;
            SetRibbonControl(localMenu, dgWorkInProgressRpt);
            dgWorkInProgressRpt.api = api;
            dgWorkInProgressRpt.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgWorkInProgressRpt.ShowTotalSummary();
            dgWorkInProgressRpt.tableView.AllowFixedColumnMenu = true;

            localMenu.OnChecked += LocalMenu_OnChecked;
            GetMenuItem();
            
            iIncludeZeroBalance.IsChecked = includeZeroBalance;
            iShowTask.IsChecked = showTask;

            iIncludeJournals.isEditLayout = false;
            iShowTask.isEditLayout = api.CompanyEntity.ProjectTask;
            dgWorkInProgressRpt.Columns.GetColumnByName("Task").Visible = showTask;

            if (WorkInProgressReportPage.DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();

                var fromDate = new DateTime(now.Year, now.Month, 1);
                fromDate = fromDate.AddMonths(-2);

                WorkInProgressReportPage.DefaultToDate = now;
                WorkInProgressReportPage.DefaultFromDate = fromDate;
            }


            txtDateTo.DateTime = WorkInProgressReportPage.DefaultToDate;
            txtDateFrm.DateTime = WorkInProgressReportPage.DefaultFromDate;
            StartLoadCache();
            WorkInProgressReportPage.SetDateTime(txtDateFrm, txtDateTo);
        }

        public static void SetDateTime(DateEditor frmDateeditor, DateEditor todateeditor)
        {
            var Now = BasePage.GetSystemDefaultDate();
            var previouslastDayOfTheYear = new DateTime(Now.Year - 1, 12, 31);
            if (frmDateeditor.Text == string.Empty)
                DefaultFromDate = Now;
            else
                DefaultFromDate = frmDateeditor.DateTime.Date;

            if (todateeditor.Text == string.Empty)
                DefaultToDate = previouslastDayOfTheYear;
            else
                DefaultToDate = todateeditor.DateTime.Date;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            SetDimensionLocalMenu();
        }

        public override Task InitQuery()
        {
            return null;
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            iIncludeZeroBalance = UtilDisplay.GetMenuCommandByName(rb, "InclZeroBalance");
            iShowTask = UtilDisplay.GetMenuCommandByName(rb, "ShowTasks");
            iIncludeJournals = UtilDisplay.GetMenuCommandByName(rb, "InclJournals");
        }

        async private void LocalMenu_OnChecked(string actionType, bool IsChecked)
        {
            switch (actionType)
            {
                case "InclZeroBalance":
                    includeZeroBalance = IsChecked;
                    iIncludeZeroBalance.IsChecked = includeZeroBalance;
                    SetIncludeFilter();
                    break;
                case "ShowTasks":
                    showTask = IsChecked;
                    iShowTask.IsChecked = showTask;
                    dgWorkInProgressRpt.Columns.GetColumnByName("Task").Visible = showTask;
                    
                    if (dgWorkInProgressRpt.ItemsSource != null)
                        LoadGrid();
                    break;
                case "InclJournals":
                    includeJournals = IsChecked;
                    iIncludeJournals.IsChecked = includeJournals;

                    await IncludeJournals();

                    StaticValues.IncludeJournals = includeJournals;
                    var wipLst = (IEnumerable<ProjectTransLocalClient>)dgWorkInProgressRpt.ItemsSource;
                    foreach (var rec in wipLst)
                    {
                        rec.NotifyClosingBalance();
                    }

                    dgWorkInProgressRpt.Columns.GetColumnByName("EmployeeHoursJournal").Visible = includeJournals;
                    dgWorkInProgressRpt.Columns.GetColumnByName("EmployeeFeeJournal").Visible = includeJournals;

                    break;
            }

            dgWorkInProgressRpt.UpdateTotalSummary();
        }

        bool tmLinesWIPLoaded;

        class TmpprojectSum
        {
            public string Project;
            public double EmployeeHoursJournal, EmployeeFeeJournal, EmployeeFeeJournalCostValue;
        }

        async private Task IncludeJournals()
        {
            if (tmLinesWIPLoaded)
                return;

            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            busyIndicator.IsBusy = true;

            var pairTM = new List<PropValuePair>()
            {
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.RegistrationType), typeof(int), "0"),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Invoiceable), typeof(bool), "1")
            };

            var minApproveDate = empCache.Where(x => x._TMApproveDate != DateTime.MinValue).Min(x => x._TMApproveDate as DateTime?) ?? DateTime.MinValue;
            if (minApproveDate != DateTime.MinValue)
                pairTM.Add(PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), minApproveDate, CompareOperator.GreaterThanOrEqual));

            var tmJourLines = await api.Query<TMJournalLineClient>(pairTM);

            var tmLines = tmJourLines.Where(s => (s.Project != null &&
                                                  s.PayrollCategory != null &&
                                                  s.Date > empCache.First(z => z.Number == s.Employee).TMApproveDate))
                                                  .GroupBy(x => new { x.Employee, x.Project, x.PayrollCategory, x.Date }).Select(x => new TMJournalLineClientLocal
                                                  {
                                                      Date = x.Key.Date,
                                                      Project = x.Key.Project,
                                                      Employee = x.Key.Employee,
                                                      PayrollCategory = x.Key.PayrollCategory,
                                                      Day1 = x.Sum(y => y._Day1),
                                                      Day2 = x.Sum(y => y._Day2),
                                                      Day3 = x.Sum(y => y._Day3),
                                                      Day4 = x.Sum(y => y._Day4),
                                                      Day5 = x.Sum(y => y._Day5),
                                                      Day6 = x.Sum(y => y._Day6),
                                                      Day7 = x.Sum(y => y._Day7),
                                                  }).ToList();

            var empPriceLst = await api.Query<EmpPayrollCategoryEmployeeClient>();

            var helper = new TMJournalLineHelper(api);
            var tmLinesWIP = new Dictionary<string, TmpprojectSum>();
            var localtmLines = new List<TMJournalLineClientLocal>();
            var localEmpPrices = new List<EmpPayrollCategoryEmployeeClient>();
            string lastEmployee = null;

            var grpEmpDate = tmLines.GroupBy(x => new { x.Employee, x.Date }).Select(g => new { g.Key.Employee, g.Key.Date, EmployeeTable = empCache.Get(g.Key.Employee) });
            foreach (var emp in grpEmpDate)
            {
                var startDate = emp.EmployeeTable.TMApproveDate < emp.Date ? emp.Date : emp.EmployeeTable.TMApproveDate >= emp.Date.AddDays(6) ? DateTime.MinValue : emp.EmployeeTable.TMApproveDate;
                var endDate = emp.Date.AddDays(6);

                if (startDate != DateTime.MinValue)
                {
                    if (lastEmployee != emp.Employee)
                    {
                        lastEmployee = emp.Employee;
                        localEmpPrices.Clear();
                        foreach (var s in empPriceLst)
                            if (s._Employee == lastEmployee)
                                localEmpPrices.Add(s);
                    }
                    localtmLines.Clear();
                    foreach (var s in tmLines)
                        if (s._Date == emp.Date && s._Employee == emp.Employee)
                            localtmLines.Add(s);

                    helper.SetEmplPrice(localtmLines,
                                        localEmpPrices,
                                        payrollCache,
                                        projCache,
                                        startDate,
                                        endDate,
                                        emp.EmployeeTable);

                    foreach (var rec in localtmLines)
                    {
                        TmpprojectSum lineWIP;
                        if (tmLinesWIP.TryGetValue(rec._Project, out lineWIP))
                        {
                            lineWIP.EmployeeHoursJournal += rec.Total;
                            lineWIP.EmployeeFeeJournal += rec.TotalSalesPrice;
                            lineWIP.EmployeeFeeJournalCostValue += rec.TotalCostPrice;
                        }
                        else
                        {
                            tmLinesWIP.Add(rec._Project,
                             new TmpprojectSum
                             {
                                 Project = rec._Project,
                                 EmployeeHoursJournal = rec.Total,
                                 EmployeeFeeJournal = rec.TotalSalesPrice,
                                 EmployeeFeeJournalCostValue = rec.TotalCostPrice,
                             });
                        }
                    }
                }
            }

            var wipLst = (IEnumerable<ProjectTransLocalClient>)dgWorkInProgressRpt.ItemsSource;
            foreach (var rec in wipLst)
            {
                TmpprojectSum lineWIP;
                if (tmLinesWIP.TryGetValue(rec._Project, out lineWIP))
                {
                    rec.EmployeeHoursJournal += lineWIP.EmployeeHoursJournal;
                    rec.EmployeeFeeJournal += lineWIP.EmployeeFeeJournal;
                    rec.EmployeeFeeJournalCostValue += lineWIP.EmployeeFeeJournalCostValue;
                    rec.NotifyEmployeeJournal();
                }
            }
            tmLinesWIPLoaded = true;

            busyIndicator.IsBusy = false;
        }


        private void SetIncludeFilter()
        {
            string filterString = dgWorkInProgressRpt.FilterString ?? string.Empty;

            if (includeZeroBalance)
            {
                filterString = filterString.Replace(FILTERVALUE_ZEROBALANCE, string.Empty).Trim();
                if (filterString != string.Empty && filterString.IndexOf(AND_OPERATOR, 0, 3) != -1)
                    filterString = filterString.Substring(3).Trim();
                else if (filterString != string.Empty && filterString.IndexOf(AND_OPERATOR, filterString.Length - 3) != -1)
                    filterString = filterString.Substring(0, filterString.IndexOf(AND_OPERATOR, filterString.Length - 3)).Trim();
            }
            else
            {
                if (filterString == string.Empty)
                    filterString = FILTERVALUE_ZEROBALANCE;
                else
                    filterString += filterString.IndexOf(FILTERVALUE_ZEROBALANCE) == -1 ? string.Format(" {0} {1}", AND_OPERATOR, FILTERVALUE_ZEROBALANCE) : string.Empty;
            }

            dgWorkInProgressRpt.FilterString = filterString;
        }

        public void SetDimensionLocalMenu()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var c = api.CompanyEntity;
            if (c == null)
                return;
            var groupByIbase = UtilDisplay.GetMenuCommandByName(rb, "GroupBy") as GroupCommand;
            var ibase1 = groupByIbase?.Commands.FirstOrDefault(d => Convert.ToString(d.Tag) == "GroupByDimension1");
            var ibase2 = groupByIbase?.Commands.FirstOrDefault(d => Convert.ToString(d.Tag) == "GroupByDimension2");
            var ibase3 = groupByIbase?.Commands.FirstOrDefault(d => Convert.ToString(d.Tag) == "GroupByDimension3");
            var ibase4 = groupByIbase?.Commands.FirstOrDefault(d => Convert.ToString(d.Tag) == "GroupByDimension4");
            var ibase5 = groupByIbase?.Commands.FirstOrDefault(d => Convert.ToString(d.Tag) == "GroupByDimension5");
            var grpObj = Uniconta.ClientTools.Localization.lookup("GroupByOBJ");
            if (ibase1 != null)
                ibase1.Caption = c._Dim1 != null ? string.Format(grpObj, (string)c._Dim1) : string.Empty;
            if (ibase2 != null)
                ibase2.Caption = c._Dim2 != null ? string.Format(grpObj, (string)c._Dim2) : string.Empty;
            if (ibase3 != null)
                ibase3.Caption = c._Dim3 != null ? string.Format(grpObj, (string)c._Dim3) : string.Empty;
            if (ibase4 != null)
                ibase4.Caption = c._Dim4 != null ? string.Format(grpObj, (string)c._Dim4) : string.Empty;
            if (ibase5 != null)
                ibase5.Caption = c._Dim5 != null ? string.Format(grpObj, (string)c._Dim5) : string.Empty;
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

        async void LoadGrid()
        {
            SetDateTime(txtDateFrm, txtDateTo);
            var transList = new List<ProjectTransLocalClient>();
            var secTransLst = new List<ProjectTransLocalClient>();
            List<ProjectTransLocalClient> newLst;

            tmLinesWIPLoaded = false;

            busyIndicator.IsBusy = true;
            SetIncludeFilter();


            if (projCache == null)
                projCache = api.GetCache<Uniconta.ClientTools.DataModel.ProjectClient>() ?? await api.LoadCache<Uniconta.ClientTools.DataModel.ProjectClient>();

            if (projGroupCache == null)
                projGroupCache = api.GetCache<Uniconta.ClientTools.DataModel.ProjectGroupClient>() ?? await api.LoadCache<Uniconta.ClientTools.DataModel.ProjectGroupClient>();

            if (empCache == null)
                empCache = api.GetCache<EmployeeClient>() ?? await api.LoadCache<EmployeeClient>();

            if (payrollCache == null)
                payrollCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>() ?? await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>();

            var pairTrans = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Date), DefaultToDate, CompareOperator.LessThanOrEqual),
                PropValuePair.GenereteWhereElements(nameof(ProjectTransClient.Invoiceable), typeof(bool), "1")
            };
            var projTransLst = await api.Query<ProjectTransClient>(pairTrans);
            if (projTransLst == null || projTransLst.Length == 0)
            {
                busyIndicator.IsBusy = false;
                return;
            }

            var projTransLstEntity = new List<ProjectTransClient>();
            string lastProj = null;
            bool tmpInvoiceable = false;
            foreach (var x in projTransLst)
            {
                if (lastProj != x._Project)
                {
                    lastProj = x._Project;
                    var proj = (Uniconta.DataModel.Project)projCache.Get(lastProj);
                    var projGrp = projGroupCache.Get(proj?._Group);
                    tmpInvoiceable = (projGrp != null && projGrp._Invoiceable);
                }

                if (tmpInvoiceable && x._Date >= DefaultFromDate)
                    projTransLstEntity.Add(x);
            }

            if (projTransLstEntity.Count > 0)
            {
                var xpensLst = new List<ProjectTransClient>();
                var feeLst = new List<ProjectTransClient>();
                var onAccLst = new List<ProjectTransClient>();
                var finalInvLst = new List<ProjectTransClient>();
                var invCostValueLst = new List<ProjectTransClient>();
                var adjLst = new List<ProjectTransClient>();
                string lastCat = null;
                CategoryType catType = 0;
                foreach (var x in projTransLstEntity)
                {
                    if (lastCat != x._PrCategory)
                    {
                        lastCat = x._PrCategory;
                        var prCat = x.PrCategoryRef;
                        if (prCat != null)
                            catType = prCat._CatType;
                        else
                            catType = (CategoryType)255; // dummy
                    }

                    if (catType == CategoryType.Materials ||
                        catType == CategoryType.Expenses ||
                        catType == CategoryType.ExternalWork ||
                        catType == CategoryType.Miscellaneous ||
                        catType == CategoryType.Other)
                        xpensLst.Add(x);
                    else if (catType == CategoryType.Labour)
                        feeLst.Add(x);
                    else if (catType == CategoryType.OnAccountInvoicing)
                        onAccLst.Add(x);
                    else if (catType == CategoryType.Revenue || (catType == CategoryType.OnAccountInvoicing && x.Invoiced == false))
                        finalInvLst.Add(x);
                    else if (catType == CategoryType.Adjustment)
                        adjLst.Add(x);

                    if (x.Invoiced && (catType == CategoryType.Materials ||
                                       catType == CategoryType.Expenses ||
                                       catType == CategoryType.ExternalWork ||
                                       catType == CategoryType.Miscellaneous ||
                                       catType == CategoryType.Other ||
                                       catType == CategoryType.Labour ||
                                       catType == CategoryType.Adjustment))
                        invCostValueLst.Add(x);
                }
               
                var lstGrouped = GetProjectTransLines(feeLst, isFee: true); 
                transList.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(xpensLst, isExpenses: true);
                transList.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(onAccLst, isOnAccount: true);
                transList.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(finalInvLst, isFinalInvoice: true);
                transList.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(adjLst, isAdjustement: true);
                transList.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(invCostValueLst, isCostInvoice: true);
                transList.AddRange(lstGrouped);

                var newLst1 = transList.GroupBy(x => new { x.Project, x.Task }).Select(y => new ProjectTransLocalClient
                {
                    _CompanyId = api.CompanyId,
                    FromDate = DefaultFromDate.Date,
                    Date = y.First().Date,
                    _Project = y.First()._Project,
                    _Task = y.First().Task,
                    EmployeeFee = y.Sum(xs => xs.EmployeeFee),
                    EmployeeFeeCostValue = y.Sum(xs => xs.EmployeeFeeCostValue),
                    Expenses = y.Sum(xs => xs.Expenses),
                    ExpensesCostValue = y.Sum(xs => xs.ExpensesCostValue),
                    OnAccount = y.Sum(xs => xs.OnAccount),
                    Invoiced = y.Sum(xs => xs.Invoiced),
                    InvoicedCostValue = y.Sum(xs => xs.InvoicedCostValue),
                    Adjustment = y.Sum(xs => xs.Adjustment),
                    AdjustmentCostValue = y.Sum(xs => xs.AdjustmentCostValue),
                    EmployeeHoursPosted = y.Sum(xs => xs.EmployeeHoursPosted),

                }); ;

                newLst = newLst1.ToList();
            }
            else
                newLst = new List<ProjectTransLocalClient>();

            var transEntity = new List<ProjectTransClient>();
            lastProj = null;
            foreach (var x in projTransLst)
            {
                if (lastProj != x._Project)
                {
                    lastProj = x._Project;
                    var proj = (Uniconta.DataModel.Project)projCache.Get(lastProj);
                    var projGrp = projGroupCache.Get(proj?._Group);
                    tmpInvoiceable = (projGrp != null && projGrp._Invoiceable);
                }

                if (tmpInvoiceable && x._Date < DefaultFromDate)
                    transEntity.Add(x);
            }

            if (transEntity.Count > 0)
            {
                var xpensLst = new List<ProjectTransClient>();
                var feeLst = new List<ProjectTransClient>();
                var onAccLst = new List<ProjectTransClient>();
                var invCostValueLst = new List<ProjectTransClient>();
                var finalInvLst = new List<ProjectTransClient>();
                var adjLst = new List<ProjectTransClient>();
                string lastCat = null;
                CategoryType catType = 0;
                foreach (var x in transEntity)
                {
                    if (lastCat != x._PrCategory)
                    {
                        lastCat = x._PrCategory;
                        var prCat = x.PrCategoryRef;
                        if (prCat != null)
                            catType = prCat._CatType;
                        else
                            catType = (CategoryType)255; // dummy
                    }

                    if (catType == CategoryType.Materials ||
                        catType == CategoryType.Expenses ||
                        catType == CategoryType.ExternalWork ||
                        catType == CategoryType.Miscellaneous ||
                        catType == CategoryType.Other)
                        xpensLst.Add(x);
                    else if (catType == CategoryType.Labour)
                        feeLst.Add(x);
                    else if (catType == CategoryType.OnAccountInvoicing)
                        onAccLst.Add(x);
                    else if (catType == CategoryType.Revenue || (catType == CategoryType.OnAccountInvoicing))
                        finalInvLst.Add(x);
                    else if (catType == CategoryType.Adjustment)
                        adjLst.Add(x);

                    if (x.Invoiced && (catType == CategoryType.Materials ||
                                     catType == CategoryType.Expenses ||
                                     catType == CategoryType.ExternalWork ||
                                     catType == CategoryType.Miscellaneous ||
                                     catType == CategoryType.Other ||
                                     catType == CategoryType.Labour ||
                                     catType == CategoryType.Adjustment))
                        invCostValueLst.Add(x);
                }

                var lstGrouped = GetProjectTransLines(feeLst, isFee: true);
                secTransLst.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(xpensLst, isExpenses: true);
                secTransLst.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(onAccLst, isOnAccount: true);
                secTransLst.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(finalInvLst, isFinalInvoice: true);
                secTransLst.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(adjLst, isAdjustement: true);
                secTransLst.AddRange(lstGrouped);
                lstGrouped = GetProjectTransLines(invCostValueLst, isCostInvoice: true);
                secTransLst.AddRange(lstGrouped);

                var newLst2 = secTransLst.GroupBy(x => new { x.Project, x.Task }).Select(y => new ProjectTransLocalClient
                {
                    _CompanyId = api.CompanyId,
                    FromDate = DefaultFromDate.Date,
                    Date = y.First().Date,
                    _Project = y.First()._Project,
                    _Task = y.First().Task,
                    OpeningBalance = Math.Round((y.Sum(xs => xs.EmployeeFee) + y.Sum(xs => xs.Expenses)) + y.Sum(xs => xs.OnAccount) + y.Sum(xs => xs.Invoiced) + y.Sum(xs => xs.Adjustment), 2),
                    OpeningBalanceCostValue = Math.Round((y.Sum(xs => xs.EmployeeFeeCostValue) + y.Sum(xs => xs.ExpensesCostValue)) + y.Sum(xs => xs.InvoicedCostValue) + y.Sum(xs => xs.AdjustmentCostValue), 2)

                });

                newLst.AddRange(newLst2);
            }

            var projs = projCache.ToArray();
            for (int i = 0; (i < projs.Length); i++)
            {
                var proj = projs[i];
                var grp = projGroupCache.Get(proj._Group);
                if (grp != null && grp._Invoiceable)
                {
                    newLst.Add(
                        new ProjectTransLocalClient
                        {
                            _CompanyId = proj.CompanyId,
                            FromDate = DefaultFromDate.Date,
                            _Project = proj._Number,
                        });
                }
            }

            IList<DebtorOrder> debtorOrderLst;
            var orderCache = api.GetCache(typeof(DebtorOrder));
            if (orderCache != null)
            {
                debtorOrderLst = new List<DebtorOrder>();
                var arr = (DebtorOrder[])orderCache.GetRecords;
                for(int i = 0; (i < arr.Length); i++)
                {
                    var rec = arr[i];
                    if (rec?._Project != null)
                        debtorOrderLst.Add(rec);
                }
            }
            else
            {
                var filter = new PropValuePair[] { PropValuePair.GenereteWhereElements(nameof(DebtorOrderClient.Project), typeof(string), "!null") };
                debtorOrderLst = await api.Query<DebtorOrder>(filter);
            }

            var finalLst = newLst.GroupBy(x => new { x.Project, x.Task }).OrderBy(s => s.Key.Project).Select(y => new ProjectTransLocalClient
            {
                _CompanyId = api.CompanyId,
                FromDate = DefaultFromDate.Date,
                Date = y.First().Date,
                _Project = y.Key.Project,
                _Task = y.Key.Task,
                OpenSalesOrder = debtorOrderLst.Any(s => s._Project == y.Key.Project),
                EmployeeFee = y.Sum(xs => xs.EmployeeFee),
                EmployeeFeeCostValue = y.Sum(xs => xs.EmployeeFeeCostValue),
                Expenses = y.Sum(xs => xs.Expenses),
                ExpensesCostValue = y.Sum(xs => xs.ExpensesCostValue),
                OnAccount = y.Sum(xs => xs.OnAccount),
                Invoiced = y.Sum(xs => xs.Invoiced),
                InvoicedCostValue = y.Sum(xs => xs.InvoicedCostValue),
                Adjustment = y.Sum(xs => xs.Adjustment),
                AdjustmentCostValue = y.Sum(xs => xs.AdjustmentCostValue),
                OpeningBalance = y.Sum(xs => xs.OpeningBalance),
                OpeningBalanceCostValue = y.Sum(xs => xs.OpeningBalanceCostValue),
                EmployeeHoursPosted = y.Sum(xs => xs.EmployeeHoursPosted),

            }).ToList();

            dgWorkInProgressRpt.ItemsSource = finalLst;
            dgWorkInProgressRpt.Visibility = Visibility.Visible;

            iIncludeJournals.isEditLayout = true;

            busyIndicator.IsBusy = false;

            if (includeJournals)
                IncludeJournals();
        }

        IEnumerable<ProjectTransLocalClient> GetProjectTransLines(IEnumerable<ProjectTransClient> projectTransLst, 
                                                                  bool isFee = false, 
                                                                  bool isExpenses = false, 
                                                                  bool isOnAccount = false, 
                                                                  bool isFinalInvoice = false, 
                                                                  bool isAdjustement = false, 
                                                                  bool isCostInvoice = false)
        {
            IEnumerable<ProjectTransLocalClient> lst;

            if (showTask)
            {
                lst = projectTransLst.GroupBy(x => new { x.Project, x.Task }).Select(y => new ProjectTransLocalClient
                {
                    _CompanyId = api.CompanyId,
                    Date = y.First().Date,
                    _Project = y.First()._Project,
                    _Task = y.First().Task,
                    EmployeeFee = isFee == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    EmployeeFeeCostValue = isFee == true ? y.Sum(xs => xs.CostAmount) : 0d,
                    Expenses = isExpenses == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    ExpensesCostValue = isExpenses == true ? y.Sum(xs => xs.CostAmount) : 0d,
                    OnAccount = isOnAccount == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    Invoiced = isFinalInvoice == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    InvoicedCostValue = isCostInvoice == true ? -y.Sum(xs => xs.CostAmount) : 0d,
                    Adjustment = isAdjustement == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    AdjustmentCostValue = isAdjustement == true ? y.Sum(xs => xs.CostAmount) : 0d,
                    EmployeeHoursPosted = isFee == true ? y.Sum(xs => xs.Qty) : 0d
                });
            }
            else
            {
                lst = projectTransLst.GroupBy(x => x.Project).Select(y => new ProjectTransLocalClient
                {
                    _CompanyId = api.CompanyId,
                    Date = y.First().Date,
                    _Project = y.First()._Project,
                    EmployeeFee = isFee == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    EmployeeFeeCostValue = isFee == true ? y.Sum(xs => xs.CostAmount) : 0d,
                    Expenses = isExpenses == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    ExpensesCostValue = isExpenses == true ? y.Sum(xs => xs.CostAmount) : 0d,
                    OnAccount = isOnAccount == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    Invoiced = isFinalInvoice == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    InvoicedCostValue = isCostInvoice == true ? -y.Sum(xs => xs.CostAmount) : 0d,
                    Adjustment = isAdjustement == true ? y.Sum(xs => xs.SalesAmount) : 0d,
                    AdjustmentCostValue = isAdjustement == true ? y.Sum(xs => xs.CostAmount) : 0d,
                    EmployeeHoursPosted = isFee == true ? y.Sum(xs => xs.Qty) : 0d
                });
            }

            return lst;
        }

        private void leFromPerInChre_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var value = e.NewValue;
            if (value != null)
                fromPerInCharge = (string)value;
        }

        private void leToPerInChre_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var value = e.NewValue;
            if (value != null)
                toPerInCharge = (string)value;
        }

        private void leFromDebtor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var value = e.NewValue;
            if (value != null)
                fromDebtor = (string)value;
        }

        private void leToDebtor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var value = e.NewValue;
            if (value != null)
                toDebtor = (string)value;
        }

        void dgWorkInProgressRpt_RowDoubleClick()
        {
            localMenu_OnItemClicked("Transactions");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgWorkInProgressRpt.SelectedItem as ProjectTransLocalClient;
            var selectedItems = dgWorkInProgressRpt.SelectedItems;

            switch (ActionType)
            {
                case "GroupByDebtor":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByRootProject":
                    dgWorkInProgressRpt.GroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;

                case "GroupByPersonInCharge":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension1":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension2":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension3":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension4":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "GroupByDimension5":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.GroupBy("Dimension5");
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    break;
                case "UnGroupAll":
                    if (dgWorkInProgressRpt.ItemsSource == null) return;
                    dgWorkInProgressRpt.UngroupBy("Debtor");
                    dgWorkInProgressRpt.UngroupBy("RootProject");
                    dgWorkInProgressRpt.UngroupBy("PersonInCharge");
                    dgWorkInProgressRpt.UngroupBy("Dimension1");
                    dgWorkInProgressRpt.UngroupBy("Dimension2");
                    dgWorkInProgressRpt.UngroupBy("Dimension3");
                    dgWorkInProgressRpt.UngroupBy("Dimension4");
                    dgWorkInProgressRpt.UngroupBy("Dimension5");
                    break;
                case "Transactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTransactionPage, dgWorkInProgressRpt.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem.Project));
                    break;
                case "Search":
                    LoadGrid();
                    break;
                case "CreateOrder":
                    if (selectedItem != null)
                        CreateOrder(selectedItem);
                    break;
                case "SalesOrder":
                    if (selectedItem != null)
                    {
                        var salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SalesOrder"), selectedItem.Debtor);
                        AddDockItem(TabControls.DebtorOrders, selectedItem.ProjectRef, salesHeader);
                    }
                    break;
                case "ZeroInvoice":
                    var markedRows = selectedItems.Cast<ProjectTransLocalClient>();
                    if (markedRows != null && markedRows.Count() > 0)
                        CreateZeroInvoice(markedRows);
                    break;
                case "DebtorAccount":
                    if (selectedItem != null)
                    {
                        var args = new object[2];
                        args[0] = api;
                        args[1] = selectedItem.DebtorRef.Account;
                        string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("DebtorAccount"), selectedItem.DebtorRef?._Name);
                        this.AddDockItem(TabControls.DebtorAccount_lookup, args, header, null, false);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.ProjectGroup), typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Project) });
        }

        private async void CreateOrder(ProjectTransLocalClient selectedItem)
        {
            var project = projCache.Get(selectedItem.Project);
            ProjectTaskClient projTask = null;
            if (api.CompanyEntity.ProjectTask && selectedItem.Task != null)
            {
                var projTaskLst = project.Tasks ?? await project.LoadTasks(api);
                projTask = projTaskLst?.Where(s => s.Task == selectedItem.Task).FirstOrDefault(); //TODO:Check når liste er null

            }
#if SILVERLIGHT
            var cwCreateOrder = new CWCreateOrderFromProject(api);
#else
            var cwCreateOrder = new UnicontaClient.Pages.CWCreateOrderFromProject(api, false, project, projTask);
            cwCreateOrder.DialogTableId = 2000000053;
#endif
            cwCreateOrder.Closed += async delegate
            {
                if (cwCreateOrder.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    busyIndicator.IsBusy = true;
                    var debtorOrderInstance = api.CompanyEntity.CreateUserType<DebtorOrderClient>();
                    var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                    var result = await invoiceApi.CreateOrderFromProject(debtorOrderInstance, project._Number, CWCreateOrderFromProject.InvoiceCategory, CWCreateOrderFromProject.GenrateDate,
                        CWCreateOrderFromProject.FromDate, CWCreateOrderFromProject.ToDate, cwCreateOrder.ProjectTask);
                    busyIndicator.IsBusy = false;
                    if (result != ErrorCodes.Succes)
                    {
                        if (result == ErrorCodes.NoLinesToUpdate)
                        {
                            var message = string.Format("{0}. {1}?", Uniconta.ClientTools.Localization.lookup(result.ToString()), string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("Order")));
                            var res = UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Message"), UnicontaMessageBox.YesNo);
                            if (res == MessageBoxResult.Yes)
                            {
                                debtorOrderInstance.SetMaster(project);
                                debtorOrderInstance._PrCategory = CWCreateOrderFromProject.InvoiceCategory;
                                debtorOrderInstance._NoItemUpdate = true;
                                var er = await api.Insert(debtorOrderInstance);
                                if (er == ErrorCodes.Succes)
                                    ShowOrderLines(debtorOrderInstance);
                            }
                        }
                        else
                            UtilDisplay.ShowErrorCode(result);
                    }
                    else
                        ShowOrderLines(debtorOrderInstance);
                }
            };
            cwCreateOrder.Show();
        }

        void CreateZeroInvoice(IEnumerable<ProjectTransLocalClient> projList)
        {
            dgWorkInProgressRpt.Columns.GetColumnByName("ErrorInfo").Visible = true;
            var cntProjects = projList.Count();
            var cntOK = 0;

            var cwCreateZeroInvoice = new UnicontaClient.Pages.CwCreateZeroInvoice(api);
#if !SILVERLIGHT
            cwCreateZeroInvoice.DialogTableId = 2000000067;
#endif
            cwCreateZeroInvoice.Closed += async delegate
            {
                if (cwCreateZeroInvoice.DialogResult == true)
                {
                    //TODO:Ret denne  - skal være muligt i dialog at disable valg af simulering ved multi opdatering
                    if (cwCreateZeroInvoice.Simulate && cntProjects > 1)
                    {
                        UnicontaMessageBox.Show("Simulation not possible", Uniconta.ClientTools.Localization.lookup("Simulation"), MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    foreach (var rec in projList)
                    {
                        rec.ErrorInfo = TMJournalLineHelper.VALIDATE_OK;

                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                        busyIndicator.IsBusy = true;
                        var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                        var result = await invoiceApi.CreateZeroInvoice(rec.Project, cwCreateZeroInvoice.InvoiceCategory, cwCreateZeroInvoice.AdjustmentCategory, cwCreateZeroInvoice.Employee, cwCreateZeroInvoice.InvoiceDate, cwCreateZeroInvoice.ToDate,
                            cwCreateZeroInvoice.Simulate, new GLTransClientTotal());
                        busyIndicator.IsBusy = false;
                        busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                        var ledgerRes = result.ledgerRes;
                        if (ledgerRes == null)
                            return;

                        if (result.Err != ErrorCodes.Succes)
                        {
                            rec.ErrorInfo = Uniconta.ClientTools.Localization.lookup(result.Err.ToString());
                        }
                        else if (cwCreateZeroInvoice.Simulate && ledgerRes.SimulatedTrans != null)
                            AddDockItem(TabControls.SimulatedTransactions, ledgerRes.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                        else
                        {
                            cntOK++;
                            var msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), ledgerRes.JournalPostedlId);
                            rec.ErrorInfo = msg;
                        }
                    }

                    var cntErr = cntProjects - cntOK;
                    string msgText = null;

                    msgText = cntOK != 0 ? string.Format("{0}: {1} {2}", Uniconta.ClientTools.Localization.lookup("ZeroInvoice"), cntOK, Uniconta.ClientTools.Localization.lookup("Posted")) : msgText;
                    msgText = cntErr != 0 && msgText != null ? Environment.NewLine : msgText;
                    msgText = cntErr != 0 ? string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HasError"), cntErr) : msgText;

                    UnicontaMessageBox.Show(msgText, Uniconta.ClientTools.Localization.lookup("ZeroInvoice"), MessageBoxButton.OK, MessageBoxImage.Information);
                }
            };
            cwCreateZeroInvoice.Show();
        }


        private void ShowOrderLines(DebtorOrderClient order)
        {
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Uniconta.ClientTools.Localization.lookup("SalesOrderCreated"), Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Orderline")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(TabControls.DebtorOrderLines, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), order._OrderNumber, order._DCAccount));
                        break;
                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();
        }
    }

    public class StaticValues
    {
        public static bool IncludeJournals { get; set; }
    }
    public class ProjectTransLocalClient : INotifyPropertyChanged, UnicontaBaseEntity
    {
        public int _CompanyId;
        public DateTime FromDate { get; set; }

        Uniconta.DataModel.Project _projectRef;
        Uniconta.DataModel.Project projectRef { get { return _projectRef ?? (_projectRef = (Uniconta.DataModel.Project)ClientHelper.GetRef(_CompanyId, typeof(Uniconta.DataModel.Project), _Project)); } }

        [Display(Name = "Date", ResourceType = typeof(ProjectTransClientText))]
        public DateTime Date { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Debtor))]
        [Display(Name = "DAccount", ResourceType = typeof(DCTransText))]
        public string Debtor { get { return projectRef?._DCAccount; } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string DebtorName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.Debtor), Debtor); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "RootProject", ResourceType = typeof(ProjectText))]
        public string RootProject { get { return projectRef._RootProject; } }

        public string _Project;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(ProjectTransClientText))]
        public string Project { get { return _Project; } }

        [Display(Name = "ProjectName", ResourceType = typeof(ProjectTransClientText))]
        public string ProjectName { get { return projectRef?._Name; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Phase", ResourceType = typeof(ProjectText))]
        public string Phase { get { var proj = projectRef; return proj != null ? AppEnums.ProjectPhase.ToString((int)proj._Phase) : null; } }

        public string _Task;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectTask))]
        [Display(Name = "Task", ResourceType = typeof(ProjectTransClientText))]
        public string Task { get { return _Task; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "PersonInCharge", ResourceType = typeof(ProjectTransClientText))]
        public string PersonInCharge { get { return projectRef?._PersonInCharge; } }

        [Display(Name = "PersonInChargeName", ResourceType = typeof(ProjectTransClientText))]
        public string PersonInChargeName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.Employee), PersonInCharge); } }

        [Display(Name = "EmployeeFee", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeFee { get; set; }

        [Display(Name = "EmployeeFeeCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeFeeCostValue { get; set; }

        [Display(Name = "EmployeeFeeJournal", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeFeeJournal { get; set; }

        [Display(Name = "EmployeeFeeJournalCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeFeeJournalCostValue { get; set; }

        [Display(Name = "EmployeeHoursPosted", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeHoursPosted { get; set; }

        [Display(Name = "EmployeeHoursJournal", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeHoursJournal { get; set; }

        [Display(Name = "Expenses", ResourceType = typeof(ProjectTransClientText))]
        public double Expenses { get; set; }

        [Display(Name = "ExpensesCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double ExpensesCostValue { get; set; }

        [Display(Name = "Revenue", ResourceType = typeof(ProjectTransClientText))]
        public double Revenue { get { return (Math.Round(EmployeeFee + Expenses, 2)); } }

        [Display(Name = "RevenueCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double RevenueCostValue { get { return (Math.Round(EmployeeFeeCostValue + ExpensesCostValue, 2)); } }

        [Display(Name = "OnAccount", ResourceType = typeof(ProjectTransClientText))]
        public double OnAccount { get; set; }

        [Display(Name = "Invoiced", ResourceType = typeof(ProjectTransClientText))] 
        public double Invoiced { get; set; }

        [Display(Name = "InvoicedCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double InvoicedCostValue { get; set; }

        [Display(Name = "TotalInvoiced", ResourceType = typeof(ProjectTransClientText))]
        public double TotalInvoiced { get { return (Math.Round(OnAccount + Invoiced, 2)); } }

        [Display(Name = "TotalInvoicedCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double TotalInvoicedCostValue { get { return (Math.Round(InvoicedCostValue, 2)); } }

        [Display(Name = "Adjustment", ResourceType = typeof(ProjectTransClientText))]
        public double Adjustment { get; set; }

        [Display(Name = "AdjustmentCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double AdjustmentCostValue { get; set; }

        [Display(Name = "OpeningBalance", ResourceType = typeof(ProjectTransClientText))]
        public double OpeningBalance { get; set; }

        [Display(Name = "OpeningBalanceCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double OpeningBalanceCostValue { get; set; }

        [Display(Name = "ClosingBalance", ResourceType = typeof(ProjectTransClientText))]
        public double ClosingBalance { get { return (Math.Round(OpeningBalance + EmployeeFee + Expenses + Adjustment + OnAccount + Invoiced + (StaticValues.IncludeJournals ? EmployeeFeeJournal : 0), 2)); } }

        [Display(Name = "ClosingBalanceCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double ClosingBalanceCostValue { get { return (Math.Round(OpeningBalanceCostValue + EmployeeFeeCostValue + ExpensesCostValue + AdjustmentCostValue + InvoicedCostValue + (StaticValues.IncludeJournals ? EmployeeFeeJournalCostValue : 0), 2)); } }

        [Display(Name = "SalesOrder", ResourceType = typeof(DCOrderText))]
        public bool OpenSalesOrder { get; set; }

        internal void NotifyClosingBalance()
        {
            NotifyPropertyChanged("ClosingBalance");
        }

        internal void NotifyEmployeeJournal()
        {
            NotifyPropertyChanged("EmployeeFeeJournal");
            NotifyPropertyChanged("EmployeeFeeJournalCostValue");
            NotifyPropertyChanged("EmployeeHoursJournal");
        }

        public string Dimension1 { get { return projectRef?._Dim1; } }
        public string Dimension2 { get { return projectRef?._Dim2; } }
        public string Dimension3 { get { return projectRef?._Dim3; } }
        public string Dimension4 { get { return projectRef?._Dim4; } }
        public string Dimension5 { get { return projectRef?._Dim5; } }

        private string _ErrorInfo;
        [Display(Name = "SystemInfo", ResourceType = typeof(DCTransText))]
        public string ErrorInfo { get { return _ErrorInfo; } set { _ErrorInfo = value; NotifyPropertyChanged("ErrorInfo"); } }

        [ReportingAttribute]
        public ProjectClient ProjectRef
        {
            get
            {
                return ClientHelper.GetRefClient<ProjectClient>(_CompanyId, typeof(Uniconta.DataModel.Project), _Project);
            }
        }

        [ReportingAttribute]
        public DebtorClient DebtorRef
        {
            get
            {
                return ClientHelper.GetRefClient<Uniconta.ClientTools.DataModel.DebtorClient>(_CompanyId, typeof(Uniconta.DataModel.Debtor), this.Debtor);
            }
        }

        [ReportingAttribute]
        public EmployeeClient EmployeeRef
        {
            get
            {
                return ClientHelper.GetRefClient<EmployeeClient>(_CompanyId, typeof(Uniconta.DataModel.Employee), PersonInCharge);
            }
        }

        [ReportingAttribute]
        public CompanyClient CompanyRef { get { return Global.CompanyRef(_CompanyId); } }

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
        public int ClassId() { return 37364; }
    }
}
