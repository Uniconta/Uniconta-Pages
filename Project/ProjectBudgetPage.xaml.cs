using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using System.Windows;
using UnicontaClient.Pages.Project.TimeManagement;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectBudgetClient); } }
        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (ProjectBudgetClient)this.SelectedItem;
            return (selectedItem?._Project != null);
        }
    }

    public partial class ProjectBudgetPage : GridBasePage
    {
        SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> payrollCache;
        SQLTableCache<Uniconta.DataModel.Project> projectCache;
        SQLTableCache<Uniconta.DataModel.Employee> employeeCache;
        SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupCache;
        SQLTableCache<Uniconta.DataModel.ProjectBudgetGroup> budgetGroupCache;
        SQLTableCache<Uniconta.DataModel.PrCategory> prCategoryCache;


        public override string NameOfControl { get { return TabControls.ProjectBudgetPage; } }
        public ProjectBudgetPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectBudgetPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        public ProjectBudgetPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectBudgetGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            var syncMaster = dgProjectBudgetGrid.masterRecord as Uniconta.DataModel.Project;
            if (syncMaster == null) return;
            string header = null;
            header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("ProjectBudget"), syncMaster._Name);
            SetHeader(header);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            SetRibbonControl(localMenu, dgProjectBudgetGrid);
            dgProjectBudgetGrid.api = api;
            if (master == null)
                Project.Visible = true;
            else
            {
                Project.Visible = false;
                dgProjectBudgetGrid.UpdateMaster(master);
            }
            dgProjectBudgetGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            projectCache = api.GetCache<Uniconta.DataModel.Project>();
            payrollCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>();
            prCategoryCache = api.GetCache<Uniconta.DataModel.PrCategory>();
            projGroupCache = api.GetCache<Uniconta.DataModel.ProjectGroup>();
            budgetGroupCache = api.GetCache<Uniconta.DataModel.ProjectBudgetGroup>();

            employeeCache = api.GetCache<Uniconta.DataModel.Employee>();

            StartLoadCache();
        }

        ProjectBudgetClient GetSelectedItem(out string Header)
        {

            var selectedItem = dgProjectBudgetGrid.SelectedItem as ProjectBudgetClient;
            if (selectedItem == null)
            {
                Header = string.Empty;
                return null;
            }
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Budget"), selectedItem.Name);
            Header = header;
            return selectedItem;
        }

        bool CopiedData;
        private void localMenu_OnItemClicked(string ActionType)
        {
            string header = string.Empty;
            ProjectBudgetClient selectedItem = GetSelectedItem(out header);

            switch (ActionType)
            {
                case "AddRow":
                    dgProjectBudgetGrid.AddRow();
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgProjectBudgetGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "DeleteRow":
                    if (UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), selectedItem._Name), Uniconta.ClientTools.Localization.lookup("Confirmation"),
#if !SILVERLIGHT
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
#else
                        MessageBoxButton.OKCancel) == MessageBoxResult.OK)
#endif
                        dgProjectBudgetGrid.DeleteRow();
                    break;
                case "BudgetLines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                case "BudgetCategorySum":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectBudgetCategorySumPage, selectedItem, header);
                    break;
                case "CreateBudget":
                    CreateBudget();
                    break;
                case "UpdatePrices":
                    if (dgProjectBudgetGrid.ItemsSource == null) return;
                        UpdatePrices();
                    break;
                case "RefreshGrid":
                    gridRibbon_BaseActions(ActionType);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        #region UpdatePrices
        private void UpdatePrices()
        {
            var cwUpdateBjt = new CwCreateUpdateBudget(api, 1);
#if !SILVERLIGHT
            cwUpdateBjt.DialogTableId = 2000000073;
#endif

            cwUpdateBjt.Closed += async delegate
            {
                if (cwUpdateBjt.DialogResult == true)
                {
                    DateTime dFromDateUpd = CwCreateUpdateBudget.FromDate;
                    DateTime dToDateUpd = CwCreateUpdateBudget.ToDate;
                    string dEmplNumber = CwCreateUpdateBudget.Employee;
                    string dBudgetGroup = CwCreateUpdateBudget.Group;
                    string dBudgetComment = CwCreateUpdateBudget.Comment;

                    if (string.IsNullOrEmpty(CwCreateUpdateBudget.Group))
                    {
                        UnicontaMessageBox.Show(FieldCannotBeEmpty(Uniconta.ClientTools.Localization.lookup("BudgetGroup")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    if (dFromDateUpd == DateTime.MinValue)
                    {
                        UnicontaMessageBox.Show(FieldCannotBeEmpty(Uniconta.ClientTools.Localization.lookup("FromDate")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    if (dToDateUpd == DateTime.MinValue)
                    {
                        UnicontaMessageBox.Show(FieldCannotBeEmpty(Uniconta.ClientTools.Localization.lookup("ToDate")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    var master = new List<UnicontaBaseEntity>();
                    ProjectBudgetGroup budgetGrp = budgetGroupCache.Get(dBudgetGroup);

                    if (budgetGrp._Blocked)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("IsBlockedOBJ"), Uniconta.ClientTools.Localization.lookup("Budget")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    busyIndicator.IsBusy = true;


                    if (!string.IsNullOrEmpty(dEmplNumber))
                        master.Add(employeeCache.Get(dEmplNumber));

                    List<PropValuePair> pairTrans = new List<PropValuePair>();
                    pairTrans.Add(PropValuePair.GenereteWhereElements(nameof(ProjectBudgetLineClient._Date), typeof(DateTime), String.Format("{0:d}..{1:d}", dFromDateUpd, dToDateUpd)));

                    var empPriceLst = await api.Query<EmpPayrollCategoryEmployeeClient>(master, pairTrans);
                    var budgetLineLst = await api.Query<ProjectBudgetLineClient>(master, pairTrans);

                    var cntUpdate = 0;
                    var tmHelper = new TMJournalLineHelper(api); 
                    foreach (var rec in budgetLineLst)
                    {
                        cntUpdate++;
                        var prices = tmHelper.GetEmplPrice(empPriceLst, payrollCache, projectCache, projGroupCache, employeeCache?.Get(rec._Employee), rec._Project, rec._Date, rec._PayrollCategory);
                        rec._CostPrice = prices.Item1;
                        rec._SalesPrice = prices.Item2;
                        rec._Text = string.Concat("(", TMJournalLineHelper.GetTimeStamp(), ") ", Uniconta.ClientTools.Localization.lookup("PriceUpdate"));
                    }

                    ErrorCodes res;
                    if (cntUpdate > 0)
                        res = await api.Update(budgetLineLst);

                    busyIndicator.IsBusy = false;

                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), cntUpdate, string.Empty), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwUpdateBjt.Show();
        }
        #endregion

        
        private class ProjectBudgetLineLocal : ProjectBudgetLine
        {
            public double _CostTotal;
            public double _SalesTotal;
        }

        private DateTime FirstDayOfWeek(DateTime selectedDate)
        {
            var dt = selectedDate;
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-diff).Date.AddYears(1);
        }

        private DateTime FirstDayOfMonth(DateTime selectedDate)
        {
            return new DateTime(selectedDate.Year, selectedDate.Month, 1).AddYears(1);
        }

        public DateTime GetDate(DateTime date, int method)
        {
            switch (method)
            {
                case 0:
                case 2: return FirstDayOfMonth(date);
                case 1:
                case 3: return FirstDayOfWeek(date);
                default: return DateTime.MinValue;
            }
        }

        private static string FieldCannotBeEmpty(string field)
        {
            return String.Format("{0} ({1}: {2})",
                   Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"),
                   Uniconta.ClientTools.Localization.lookup("Field"), field);
        }

        public int GetHashCode(ProjectTransPivotClient obj, int method) //TODO:Ret til generel class
        {
            switch (method)
            {
                case 0: return obj._Project.GetHashCode() * 
                              (obj._Employee != null ? obj._Employee.GetHashCode() : 1) * 
                              (obj._PayrollCategory != null ? obj._PayrollCategory.GetHashCode() : 1) * 
                               obj._Date.Month.GetHashCode();
                case 1: return obj._Project.GetHashCode() *
                              (obj._Employee != null ? obj._Employee.GetHashCode() : 1) *
                              (obj._PayrollCategory != null ? obj._PayrollCategory.GetHashCode() : 1) * 
                               FirstDayOfWeek(obj._Date).GetHashCode();
                case 2: return obj._Project.GetHashCode() *
                              (obj._Employee != null ? obj._Employee.GetHashCode() : 1) *
                               obj._PrCategory.GetHashCode() *
                               obj._Date.Month.GetHashCode();
                case 3: return obj._Project.GetHashCode() *
                              (obj._Employee != null ? obj._Employee.GetHashCode() : 1) *
                               obj._PrCategory.GetHashCode() * 
                               FirstDayOfWeek(obj._Date).GetHashCode();
                default: return 0;
            }
        }

        public int GetHashCodeBudget(ProjectBudgetLineClient obj, int method) //TODO:Ret til generel class
        {
            switch (method)
            {
                case 0:
                    return obj._Project.GetHashCode() *
                          (obj._Employee != null ? obj._Employee.GetHashCode() : 1) *
                          (obj._PayrollCategory != null ? obj._PayrollCategory.GetHashCode() : 1) *
                           obj._Date.Month.GetHashCode();
                case 1:
                    return obj._Project.GetHashCode() *
                          (obj._Employee != null ? obj._Employee.GetHashCode() : 1) *
                          (obj._PayrollCategory != null ? obj._PayrollCategory.GetHashCode() : 1) *
                           FirstDayOfWeek(obj._Date).GetHashCode();
                case 2:
                    return obj._Project.GetHashCode() *
                          (obj._Employee != null ? obj._Employee.GetHashCode() : 1) *
                           obj._PrCategory.GetHashCode() *
                           obj._Date.Month.GetHashCode();
                case 3:
                    return obj._Project.GetHashCode() *
                          (obj._Employee != null ? obj._Employee.GetHashCode() : 1) *
                           obj._PrCategory.GetHashCode() *
                           FirstDayOfWeek(obj._Date).GetHashCode();
                default: return 0;
            }
        }


        #region Create Budget
        private void CreateBudget()
        {
            var cwCreateBjt = new CwCreateUpdateBudget(api);
#if !SILVERLIGHT
            cwCreateBjt.DialogTableId = 2000000074;
#endif
            cwCreateBjt.Closed += async delegate
            {
                if (cwCreateBjt.DialogResult == true) 
                {
                    int budgetMethod = 0;
                    DateTime dFromDate = CwCreateUpdateBudget.FromDate;
                    DateTime dToDate = CwCreateUpdateBudget.ToDate;
                    string dEmplNumber = CwCreateUpdateBudget.Employee;
                    string dProjectNumber = CwCreateUpdateBudget.Project;
                    string dBudgetGroup = CwCreateUpdateBudget.Group;
                    string dBudgetComment = CwCreateUpdateBudget.Comment;

                    if (string.IsNullOrEmpty(CwCreateUpdateBudget.Group))
                    {
                        UnicontaMessageBox.Show(FieldCannotBeEmpty(Uniconta.ClientTools.Localization.lookup("BudgetGroup")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    if (dFromDate == DateTime.MinValue)
                    {
                        UnicontaMessageBox.Show(FieldCannotBeEmpty(Uniconta.ClientTools.Localization.lookup("FromDate")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    if (dToDate == DateTime.MinValue)
                    {
                        UnicontaMessageBox.Show(FieldCannotBeEmpty(Uniconta.ClientTools.Localization.lookup("ToDate")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    ErrorCodes res;
                    var master = new List<UnicontaBaseEntity>();

                    //Delete Budget Lines >>
                    ProjectBudgetGroup budgetGrp = budgetGroupCache.Get(dBudgetGroup);

                    if (budgetGrp._Blocked)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("IsBlockedOBJ"), Uniconta.ClientTools.Localization.lookup("Budget")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

                    busyIndicator.IsBusy = true;

                    master.Add(budgetGrp); 

                    if (!string.IsNullOrEmpty(dEmplNumber))
                        master.Add(employeeCache.Get(dEmplNumber));

                    if (!string.IsNullOrEmpty(dProjectNumber))
                        master.Add(projectCache.Get(dProjectNumber));

                    List<PropValuePair> pairLine = new List<PropValuePair>();
                    pairLine.Add(PropValuePair.GenereteWhereElements(nameof(ProjectBudgetLine._Date), typeof(DateTime), String.Format("{0:d}..{1:d}", dFromDate, dToDate)));

                    var projBudgetLineDelete = await api.Query<ProjectBudgetLine>(master, pairLine);
                    if (projBudgetLineDelete.Length != 0)
                        res = await api.Delete(projBudgetLineDelete);
                    //Delete Budget Lines <<


                    List<PropValuePair> pairBudget = new List<PropValuePair>();
                    pairBudget.Add(PropValuePair.GenereteWhereElements(nameof(ProjectBudget._Group), typeof(string), dBudgetGroup));
                    var projBudgetLst = await api.Query<ProjectBudget>(pairBudget);

                    var dictProjBudget = new Dictionary<string, ProjectBudget>();
                    foreach (ProjectBudget rec in projBudgetLst)
                    {
                        ProjectBudget projBudget;
                        bool hasValue = dictProjBudget.TryGetValue(string.Concat(rec._Project, dBudgetGroup), out projBudget);
                        if (!hasValue)
                            dictProjBudget.Add(string.Concat(rec._Project, dBudgetGroup), rec);
                    }

                    List<PropValuePair> filterTrans = new List<PropValuePair>();
                    filterTrans.Add(PropValuePair.GenereteWhereElements(nameof(ProjectTrans._Date), typeof(DateTime), String.Format("{0:d}..{1:d}", dFromDate.AddYears(-1), dToDate.AddYears(-1))));

                    var transTask = api.Query(new ProjectTransPivotClient(), master, filterTrans);
                    var projTransLst = await transTask;

                    var projTransLstEntity = new List<ProjectTransPivotClient>();
                    //string lastProj = null;
                    string lastPrCat = null;
                    string lastPayrollCat = null;

                    //Uniconta.DataModel.ProjectGroup projGrp = null;
                    Uniconta.DataModel.PrCategory prCat = null;
                    Uniconta.DataModel.EmpPayrollCategory payrollCat = null;

                    foreach (var x in projTransLst)
                    {
                        //if (lastProj != x._Project)
                        //{
                        //    lastProj = x._Project;
                        //    var proj = x.ProjectRef;
                        //    projGrp = projGroupCache.Get(proj._Group);
                        //}
                        if (lastPrCat != x._PrCategory)
                        {
                            lastPrCat = x._PrCategory;
                            prCat = x.CategoryRef;
                        }
                        if (lastPayrollCat != x._PayrollCategory)
                        {
                            lastPayrollCat = x._PayrollCategory;
                            payrollCat = x.PayrollCategoryRef;
                        }

                        if (prCat != null && payrollCat != null && prCat._CatType == CategoryType.Labour && payrollCat._Unit == ItemUnit.Hours && payrollCat._InternalType == InternalType.None)
                            projTransLstEntity.Add(x);

                    }

                    if (projTransLstEntity == null)
                    {
                        busyIndicator.IsBusy = false;
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), 0, string.Empty), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                        return;
                    }

                    //Only create budget project with transactions
                    var insertLst = new List<UnicontaBaseEntity>();
                    var grpProject = projTransLstEntity.GroupBy(x => x._Project); 
                    foreach (var rec in grpProject)
                    {
                        ProjectBudget projBudget;
                        bool hasValue = dictProjBudget.TryGetValue(string.Concat(rec.Key, dBudgetGroup), out projBudget);
                        if (!hasValue)
                        {
                            insertLst.Add(new ProjectBudget()
                            {
                                _Project = rec.Key,
                                _Group = dBudgetGroup,
                                _Name = dBudgetComment,
                                _Current = true
                            });
                        }
                    }
                    if (insertLst.Count > 0)
                        res = await api.Insert(insertLst); //Create new Budget-Header

                    //Insert new header in dictionary for fast lookup
                    foreach (ProjectBudget rec in insertLst)
                    {
                        ProjectBudget projBudget;
                        bool hasValue = dictProjBudget.TryGetValue(string.Concat(rec._Project, dBudgetGroup), out projBudget);
                        if (!hasValue)
                            dictProjBudget.Add(string.Concat(rec._Project, dBudgetGroup), rec);
                    }

                    ProjectBudgetLineLocal projBudgetLine;
                    var dictProjBudgetLine = new Dictionary<int, ProjectBudgetLineLocal>();
                    foreach (var trans in projTransLstEntity)
                    {
                        if (dictProjBudgetLine.TryGetValue(GetHashCode(trans, budgetMethod), out projBudgetLine))
                        {
                            projBudgetLine._Qty += trans._Qty;
                            projBudgetLine._CostTotal += trans.Cost;
                            projBudgetLine._CostPrice = projBudgetLine._Qty != 0 ? Math.Round(projBudgetLine._CostTotal / projBudgetLine._Qty, 2) : 0; 
                            projBudgetLine._SalesTotal += trans.Sales; 
                            projBudgetLine._SalesPrice = projBudgetLine._Qty != 0 ? Math.Round(projBudgetLine._SalesTotal / projBudgetLine._Qty, 2) : 0;
                        }
                        else
                        {
                            ProjectBudget projBudget;
                            bool hasValue = dictProjBudget.TryGetValue(string.Concat(trans._Project, dBudgetGroup), out projBudget);
                            if (hasValue)
                            {
                                projBudgetLine = new ProjectBudgetLineLocal();
                                projBudgetLine.SetMaster(projBudget);
                                projBudgetLine._Project = trans._Project;
                                projBudgetLine._Qty = trans._Qty;
                                projBudgetLine._CostTotal += trans.Cost; 
                                projBudgetLine._CostPrice = projBudgetLine._Qty != 0 ? Math.Round(projBudgetLine._CostTotal / projBudgetLine._Qty, 2) : 0;
                                projBudgetLine._SalesTotal += trans.Sales;
                                projBudgetLine._SalesPrice = projBudgetLine._Qty != 0 ? Math.Round(projBudgetLine._SalesTotal / projBudgetLine._Qty, 2) : 0;
                                projBudgetLine._Date = GetDate(trans._Date, budgetMethod);
                                projBudgetLine._Employee = trans._Employee;
                                projBudgetLine._PayrollCategory = budgetMethod == 0 || budgetMethod == 1 ? trans._PayrollCategory : null;
                                projBudgetLine._PrCategory = trans._PrCategory;
                                projBudgetLine._Text = string.Concat("(", TMJournalLineHelper.GetTimeStamp(), ") ", Uniconta.ClientTools.Localization.lookup("Created"));
                                dictProjBudgetLine.Add(GetHashCode(trans, budgetMethod), projBudgetLine);
                            }
                        }
                    }

                    if (dictProjBudgetLine.Count > 0)
                    {
                        res = await api.Insert(dictProjBudgetLine.Values);
                        localMenu_OnItemClicked("RefreshGrid");
                    }

                    busyIndicator.IsBusy = false;

                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("RecordsUpdated"), dictProjBudgetLine.Count, string.Empty), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            };
            cwCreateBjt.Show();
            
        }
        #endregion

        async void SaveAndOpenLines(ProjectBudgetClient selectedItem)
        {
            if (dgProjectBudgetGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.ProjectBudgetLinePage, dgProjectBudgetGrid.syncEntity, string.Format("{0} {1}: {2}", Uniconta.ClientTools.Localization.lookup("Budget"), Uniconta.ClientTools.Localization.lookup("Lines"), selectedItem._Project));
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;

            if (projectCache == null)
                projectCache = await api.LoadCache<Uniconta.DataModel.Project>().ConfigureAwait(false);
            if (payrollCache == null)
                payrollCache = await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>().ConfigureAwait(false);
            if (prCategoryCache == null)
                prCategoryCache = await api.LoadCache<Uniconta.DataModel.PrCategory>().ConfigureAwait(false);
            if (projGroupCache == null)
                projGroupCache = await api.LoadCache<Uniconta.DataModel.ProjectGroup>().ConfigureAwait(false);
            if (employeeCache == null)
                employeeCache = await api.LoadCache<Uniconta.DataModel.Employee>().ConfigureAwait(false);
            if (budgetGroupCache == null)
                budgetGroupCache = await api.LoadCache<Uniconta.DataModel.ProjectBudgetGroup>().ConfigureAwait(false);
        }
    }
}


