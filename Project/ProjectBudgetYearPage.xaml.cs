using DevExpress.Data;
using DevExpress.Xpf.Core.Serialization;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Project;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetYearGridClient : CorasauDataGridClient
    {
        public override Type TableType => typeof(ProjectBudgetYearClient);
        public override bool Readonly { get { return false; } }
    }

    /// <summary>
    /// Interaction logic for ProjectBudgetYearPage.xaml
    /// </summary>
    public partial class ProjectBudgetYearPage : GridBasePage
    {
        EmployeeClient _empMaster;
        ProjectBudgetGroup budgetGrpMaster;

        ProjectBudgetLineClient[] _OrginalProjectBugetLines;
        List<ProjectBudgetYearClient> _ProjectBudgetYearClients;
        SQLCache budgetGroups, payrolls, projects;

        double normHoursMonth1, normHoursMonth2, normHoursMonth3, normHoursMonth4, normHoursMonth5, normHoursMonth6, normHoursMonth7,
            normHoursMonth8, normHoursMonth9, normHoursMonth10, normHoursMonth11, normHoursMonth12, normHoursTotal;

        double month1Sum, month2Sum, month3Sum, month4Sum, month5Sum, month6Sum, month7Sum, month8Sum, month9Sum,
            month10Sum, month11Sum, month12Sum, totalSum;

        static int defaultYear;
        static string defaultBudgetGroup;
        Uniconta.API.Project.FindPricesEmpl priceLookup;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectBudgetGroup))]
        public string BudgetGroup { get; set; }

        public override string NameOfControl => TabControls.ProjectBudgetYearPage;

        bool isDataChaged;
        public override bool IsDataChaged { get { return isDataChaged || base.IsDataChaged; } }

        public ProjectBudgetYearPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            this.DataContext = this;
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            if (master is EmployeeClient emp)
                _empMaster = emp;

            if (defaultYear == 0)
                defaultYear = (DateTime.Now.Year + 1);
            txtYear.Text = defaultYear.ToString();

            cmbProjectBudgetGroup.api = api;
            dgProjectBudgetYear.api = api;
            dgProjectBudgetYear.BusyIndicator = busyIndicator;
            dgProjectBudgetYear.ShowTotalSummary();
            dgProjectBudgetYear.View.ShowFixedTotalSummary = true;
            DXSerializer.AddCreateCollectionItemEventHandler(dgProjectBudgetYear, CreateCollectionItemEventHandler);
            dgProjectBudgetYear.CustomSummary += DgProjectBudgetYear_CustomSummary;
            dgProjectBudgetYear.tableView.ShowingEditor += TableView_ShowingEditor; ;
            dgProjectBudgetYear.SelectedItemChanged += DgProjectBudgetYear_SelectedItemChanged;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            SetRibbonControl(localMenu, dgProjectBudgetYear);

            payrolls = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory));
            projects = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Project));
            budgetGroups = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.ProjectBudgetGroup));
        }
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
            }
        }
        private void TableView_ShowingEditor(object sender, DevExpress.Xpf.Grid.ShowingEditorEventArgs e)
        {
            if (e.Column.FieldName == "MonthQty1" || e.Column.FieldName == "MonthQty2" || e.Column.FieldName == "MonthQty3" ||
                e.Column.FieldName == "MonthQty4" || e.Column.FieldName == "MonthQty5" || e.Column.FieldName == "MonthQty6" ||
                e.Column.FieldName == "MonthQty7" || e.Column.FieldName == "MonthQty8" || e.Column.FieldName == "MonthQty9" ||
                e.Column.FieldName == "MonthQty10" || e.Column.FieldName == "MonthQty11" || e.Column.FieldName == "MonthQty12")
            {
                return;
            }
        }

        private void DgProjectBudgetYear_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
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
                        case "MonthQty1":
                            if (tagName == "HoursMonth1")
                                e.TotalValue = normHoursMonth1;
                            else if (tagName == "TotalMonth1")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month1Sum, normHoursMonth1);
                            break;
                        case "MonthQty2":
                            if (tagName == "HoursMonth2")
                                e.TotalValue = normHoursMonth2;
                            else if (tagName == "TotalMonth2")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month2Sum, normHoursMonth2);
                            break;
                        case "MonthQty3":
                            if (tagName == "HoursMonth3")
                                e.TotalValue = normHoursMonth3;
                            else if (tagName == "TotalMonth3")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month3Sum, normHoursMonth3);
                            break;
                        case "MonthQty4":
                            if (tagName == "HoursMonth4")
                                e.TotalValue = normHoursMonth4;
                            else if (tagName == "TotalMonth4")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month4Sum, normHoursMonth4);
                            break;
                        case "MonthQty5":
                            if (tagName == "HoursMonth5")
                                e.TotalValue = normHoursMonth5;
                            else if (tagName == "TotalMonth5")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month5Sum, normHoursMonth5);
                            break;
                        case "MonthQty6":
                            if (tagName == "HoursMonth6")
                                e.TotalValue = normHoursMonth6;
                            else if (tagName == "TotalMonth6")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month6Sum, normHoursMonth6);
                            break;
                        case "MonthQty7":
                            if (tagName == "HoursMonth7")
                                e.TotalValue = normHoursMonth7;
                            else if (tagName == "TotalMonth7")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month7Sum, normHoursMonth7);
                            break;
                        case "MonthQty8":
                            if (tagName == "HoursMonth8")
                                e.TotalValue = normHoursMonth8;
                            else if (tagName == "TotalMonth8")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month8Sum, normHoursMonth8);
                            break;
                        case "MonthQty9":
                            if (tagName == "HoursMonth9")
                                e.TotalValue = normHoursMonth9;
                            else if (tagName == "TotalMonth9")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month9Sum, normHoursMonth9);
                            break;
                        case "MonthQty10":
                            if (tagName == "HoursMonth10")
                                e.TotalValue = normHoursMonth10;
                            else if (tagName == "TotalMonth10")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month10Sum, normHoursMonth10);
                            break;
                        case "MonthQty11":
                            if (tagName == "HoursMonth11")
                                e.TotalValue = normHoursMonth11;
                            else if (tagName == "TotalMonth11")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month11Sum, normHoursMonth11);
                            break;
                        case "MonthQty12":
                            if (tagName == "HoursMonth12")
                                e.TotalValue = normHoursMonth12;
                            else if (tagName == "TotalMonth12")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(month12Sum, normHoursMonth12);
                            break;
                        case "TotalQty":
                            if (tagName == "EmpNormHoursSum")
                                e.TotalValue = normHoursTotal;
                            else if (tagName == "TotalSum")
                                e.TotalValue = new ProjectBudgetSumColumnWrapper(totalSum, normHoursTotal);
                            break;
                    }
                }
            }
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            dgProjectBudgetYear.tableView.TotalSummaryElementStyle = (Style)FindResource("SummaryTotalStyle");
        }
        void GetGridColumnsSum()
        {
            var lst = dgProjectBudgetYear.ItemsSource as IEnumerable<ProjectBudgetYearClient>;
            if (lst != null)
            {
                month1Sum = month2Sum = month3Sum = month4Sum = month5Sum = month6Sum = month7Sum =
                    month8Sum = month9Sum = month10Sum = month11Sum = month12Sum = 0;

                foreach (var x in lst)
                {
                    month1Sum += x.MonthQty1;
                    month2Sum += x.MonthQty2;
                    month3Sum += x.MonthQty3;
                    month4Sum += x.MonthQty4;
                    month5Sum += x.MonthQty5;
                    month6Sum += x.MonthQty6;
                    month7Sum += x.MonthQty7;
                    month8Sum += x.MonthQty8;
                    month9Sum += x.MonthQty9;
                    month10Sum += x.MonthQty10;
                    month11Sum += x.MonthQty11;
                    month12Sum += x.MonthQty12;
                }

                totalSum = Math.Round(month1Sum + month2Sum + month3Sum + month4Sum + month5Sum + month6Sum + month7Sum +
                    month8Sum + month9Sum + month10Sum + month11Sum + month12Sum, 2);
            }
        }


        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    AddRow();
                    break;
                case "DeleteRow":
                    if (dgProjectBudgetYear.SelectedItem != null)
                        DeleteRow();
                    break;
                case "Search":
                    LoadBudgetYear();
                    break;
                case "Save":
                case "SaveDataGrid":
                    SaveBudgetYear();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override async Task InitQuery()
        {
            if (_empMaster != null)
            {
                await SetBudgetGroup();
                if (budgetGrpMaster != null)
                    LoadBudgetYear();
            }
        }

        private void AddRow()
        {
            if (budgetGrpMaster == null)
                return;

            try
            {
                var projectBudgetLine = api.CompanyEntity.CreateUserType<ProjectBudgetLineClient>();
                projectBudgetLine.SetMaster(_empMaster);
                var newRow = new ProjectBudgetYearClient(projectBudgetLine);
                var selectedIndex = dgProjectBudgetYear.View.FocusedRowHandle < 0 ? -1 : dgProjectBudgetYear.View.FocusedRowHandle;

                if (_ProjectBudgetYearClients != null)
                {
                    int len = _ProjectBudgetYearClients.Count;
                    int pos = selectedIndex + 1;
                    if (pos < 0)
                        pos = 0;
                    if (pos >= len)
                        _ProjectBudgetYearClients.Add(newRow);
                    else
                        _ProjectBudgetYearClients.Insert(pos, newRow);
                }
                else
                    _ProjectBudgetYearClients = new List<ProjectBudgetYearClient> { newRow };

                ResetSourceOnGrid();
                isDataChaged = true;
            }
            catch (System.ArgumentException)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InternalError"), Uniconta.ClientTools.Localization.lookup("Exception"));
                return;
            }
        }

        private void DeleteRow()
        {
            if (dgProjectBudgetYear.SelectedItem != null)
            {
                var selectedRow = dgProjectBudgetYear.SelectedItem as ProjectBudgetYearClient;
                if (selectedRow != null)
                {
                    selectedRow.MonthQty1 = selectedRow.MonthQty2 = selectedRow.MonthQty3 = selectedRow.MonthQty4 = selectedRow.MonthQty5 = selectedRow.MonthQty6 =
                    selectedRow.MonthQty7 = selectedRow.MonthQty8 = selectedRow.MonthQty9 = selectedRow.MonthQty10 = selectedRow.MonthQty11 = selectedRow.MonthQty12 = 0;
                    ResetSourceOnGrid();
                    isDataChaged = true;
                }
            }
        }

        void ResetSourceOnGrid()
        {
            dgProjectBudgetYear.ItemsSource = null;
            var _ProjectBudgetYearClientArr = _ProjectBudgetYearClients.ToArray();
            Array.Sort(_ProjectBudgetYearClientArr, new ProjectBudgetYearSort());
            dgProjectBudgetYear.ItemsSource = _ProjectBudgetYearClientArr;
            dgProjectBudgetYear.Visibility = Visibility.Visible;
        }

        private void DgProjectBudgetYear_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as UnicontaClient.Pages.ProjectBudgetYearClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= DgProjectBudgetYear_PropertyChanged;
            var selectedItem = e.NewItem as UnicontaClient.Pages.ProjectBudgetYearClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += DgProjectBudgetYear_PropertyChanged;
        }

        private void DgProjectBudgetYear_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            isDataChaged = true;
        }

        async void SaveBudgetYear()
        {
            if (_ProjectBudgetYearClients == null || _ProjectBudgetYearClients.Count == 0)
                return;

            int prBudgetYearCount = _ProjectBudgetYearClients.Count;

            var insertBudgetLine = new List<ProjectBudgetLineClient>(prBudgetYearCount);
            var updateBudgetLine = new List<ProjectBudgetLineClient>(prBudgetYearCount);
            var deleteBudgetLine = new List<ProjectBudgetLineClient>(prBudgetYearCount);

            // Group original lines by their key (project, employee, PayrollCategory, PrCategory, WorkSpace, Task)
            var groupedOriginal = _OrginalProjectBugetLines
                .GroupBy(o => new
                {
                    o.Project,
                    o.Employee,
                    o.PayrollCategory,
                    o.PrCategory,
                    o.WorkSpace,
                    o.Task
                });

            // Iterate through each group
            foreach (var group in groupedOriginal)
            {
                var aList = _ProjectBudgetYearClients.Where(a =>
                       a.Project == group.Key.Project &&
                       a.Employee == group.Key.Employee &&
                       a.PayrollCategory == group.Key.PayrollCategory &&
                       a.PrCategory == group.Key.PrCategory &&
                       a.WorkSpace == group.Key.WorkSpace &&
                       a.Task == group.Key.Task).ToList();

                if (aList.Count == 0)
                    continue; 

                var aggregated = new ProjectBudgetYearClient
                {
                    Project = group.Key.Project,
                    Employee = group.Key.Employee,
                    PayrollCategory = group.Key.PayrollCategory,
                    PrCategory = group.Key.PrCategory,
                    WorkSpace = group.Key.WorkSpace,
                    Task = group.Key.Task,

                    MonthQty1 = aList.Sum(x => x.MonthQty1),
                    MonthQty2 = aList.Sum(x => x.MonthQty2),
                    MonthQty3 = aList.Sum(x => x.MonthQty3),
                    MonthQty4 = aList.Sum(x => x.MonthQty4),
                    MonthQty5 = aList.Sum(x => x.MonthQty5),
                    MonthQty6 = aList.Sum(x => x.MonthQty6),
                    MonthQty7 = aList.Sum(x => x.MonthQty7),
                    MonthQty8 = aList.Sum(x => x.MonthQty8),
                    MonthQty9 = aList.Sum(x => x.MonthQty9),
                    MonthQty10 = aList.Sum(x => x.MonthQty10),
                    MonthQty11 = aList.Sum(x => x.MonthQty11),
                    MonthQty12 = aList.Sum(x => x.MonthQty12),
                };

                for (int month = 1; month <= 12; month++)
                {
                    var originalLinesForMonth = group.Where(o => o.Date.Month == month).ToList();

                    bool isMonthSetInAggregated = aggregated != null && !IsMonthQtySet(aggregated, month);

                    if (!isMonthSetInAggregated)
                    {
                        // Quantity is no longer set — delete all original lines for this month
                        foreach (var line in originalLinesForMonth)
                            deleteBudgetLine.Add(line);
                    }
                    else
                    {
                        if (originalLinesForMonth.Count == 0)
                        {
                            // No original line, but quantity is now set — insert new line
                            var newLine = CreateNewBudgetLineClient(aggregated);
                            UpdateNewBudgetLine(aggregated, newLine, month);
                            await priceLookup.GetEmployeePrice(newLine);
                            insertBudgetLine.Add(newLine);
                        }
                        else
                        {
                            // Update last line, delete others
                            var lastOriginal = originalLinesForMonth.OrderBy(l => l.Date).Last();

                            // Update the quantity
                            SetQuantity(aggregated, month, lastOriginal);

                            // Set date to month-end if needed
                            if (lastOriginal.Date.Day != DateTime.DaysInMonth(lastOriginal.Date.Year, lastOriginal.Date.Month))
                                lastOriginal.Date = new DateTime(lastOriginal.Date.Year, lastOriginal.Date.Month, DateTime.DaysInMonth(lastOriginal.Date.Year, lastOriginal.Date.Month));

                            updateBudgetLine.Add(lastOriginal);

                            // Delete remaining lines in the same month
                            foreach (var redundant in originalLinesForMonth.Where(l => l != lastOriginal))
                                deleteBudgetLine.Add(redundant);
                        }
                    }
                }
            }

            // Also handle any completely new rows (not present in original)
            var unmatchedAggregates = _ProjectBudgetYearClients
                .Where(agg => !_OrginalProjectBugetLines.Any(orig => IsProjectBudgetLineMatch(agg, orig)));

            if (unmatchedAggregates != null)
            {
                var projBudgetArr = await api.Query<ProjectBudget>();
                var projGrouped = unmatchedAggregates.GroupBy(s => s.Project);

                var budgetsGrouped = projBudgetArr.GroupBy(b => Tuple.Create(b._Project, b._Group)).ToDictionary(g => g.Key, g => g.First());

                foreach (var proj in projGrouped)
                {
                    var projectNo = proj.Key;

                    var dictKey = Tuple.Create(projectNo, budgetGrpMaster.KeyStr);
                    budgetsGrouped.TryGetValue(dictKey, out var projBudget);

                    bool createHeader = projBudget == null;

                    if (createHeader)
                    {
                        var project = (Uniconta.DataModel.Project)projects.Get(projectNo);
                        if (project == null)
                            continue;

                        projBudget = new ProjectBudget();
                        projBudget.SetMaster(project);
                        projBudget._Group = budgetGrpMaster.KeyStr;
                        projBudget._Name = Uniconta.ClientTools.Localization.lookup("Budget");
                        projBudget._Current = true;
                    }

                    var insertNewBudgetLine = new List<ProjectBudgetLineClient>();
                    foreach (var newAgg in proj)
                    {
                        for (int month = 1; month <= 12; month++)
                        {
                            if (IsMonthQtySet(newAgg, month))
                                continue;
                                                        
                            var newLine = CreateNewBudgetLineClient(newAgg);
                            UpdateNewBudgetLine(newAgg, newLine, month);
                            await priceLookup.GetEmployeePrice(newLine);

                            if (createHeader)
                                insertNewBudgetLine.Add(newLine);
                            else
                                insertBudgetLine.Add(newLine);
                        }
                    }

                    if (createHeader)
                        await api.Insert(projBudget, insertNewBudgetLine);
                }
            }
            isDataChaged = false;
            var result = await api.MultiCrud(insertBudgetLine, updateBudgetLine, deleteBudgetLine);
            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
            else
            {
                LoadBudgetYear();
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SavedOBJ"), Uniconta.ClientTools.Localization.lookup("Budget")),
                    Uniconta.ClientTools.Localization.lookup("Information"));
            }
        }

        private ProjectBudgetLineClient CreateNewBudgetLineClient(ProjectBudgetYearClient item)
        {
            var prjBudgetLine = new ProjectBudgetLineClient();
            prjBudgetLine.SetMaster(api.CompanyEntity);
            prjBudgetLine.Project = item.Project;
            prjBudgetLine.Employee = item.Employee;
            prjBudgetLine.PrCategory = item.PrCategory;
            prjBudgetLine.PayrollCategory = item.PayrollCategory;
            prjBudgetLine.Task = item.Task;
            prjBudgetLine.WorkSpace = item.WorkSpace;

            return prjBudgetLine;
        }

        private void UpdateNewBudgetLine(ProjectBudgetYearClient item, ProjectBudgetLineClient prjBudgetLine, int mt)
        {
            switch (mt)
            {
                case 1:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty1;
                    break;
                case 2:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty2;
                    break;
                case 3:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty3;
                    break;
                case 4:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty4;
                    break;
                case 5:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty5;
                    break;
                case 6:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty6;
                    break;
                case 7:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty7;
                    break;
                case 8:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty8;
                    break;
                case 9:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty9;
                    break;
                case 10:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty10;
                    break;
                case 11:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty11;
                    break;
                case 12:
                    prjBudgetLine.Date = new DateTime(defaultYear, mt, DateTime.DaysInMonth(defaultYear, mt)); prjBudgetLine.Qty = item.MonthQty12;
                    break;
            }
        }

        private bool IsMonthQtySet(ProjectBudgetYearClient item, int mt)
        {
            switch (mt)
            {
                case 1:
                    return double.IsNaN(item.MonthQty1) || item.MonthQty1 == 0;
                case 2:
                    return double.IsNaN(item.MonthQty2) || item.MonthQty2 == 0;
                case 3:
                    return double.IsNaN(item.MonthQty3) || item.MonthQty3 == 0;
                case 4:
                    return double.IsNaN(item.MonthQty4) || item.MonthQty4 == 0;
                case 5:
                    return double.IsNaN(item.MonthQty5) || item.MonthQty5 == 0;
                case 6:
                    return double.IsNaN(item.MonthQty6) || item.MonthQty6 == 0;
                case 7:
                    return double.IsNaN(item.MonthQty7) || item.MonthQty7 == 0;
                case 8:
                    return double.IsNaN(item.MonthQty8) || item.MonthQty8 == 0;
                case 9:
                    return double.IsNaN(item.MonthQty9) || item.MonthQty9 == 0;
                case 10:
                    return double.IsNaN(item.MonthQty10) || item.MonthQty10 == 0;
                case 11:
                    return double.IsNaN(item.MonthQty11) || item.MonthQty11 == 0;
                case 12:
                    return double.IsNaN(item.MonthQty12) || item.MonthQty12 == 0;
            }
            return false;
        }

        private void SetQuantity(ProjectBudgetYearClient item, int mt, ProjectBudgetLineClient firstLine)
        {
            switch (mt)
            {
                case 1:
                    firstLine.Qty = item.MonthQty1;
                    break;
                case 2:
                    firstLine.Qty = item.MonthQty2;
                    break;
                case 3:
                    firstLine.Qty = item.MonthQty3;
                    break;
                case 4:
                    firstLine.Qty = item.MonthQty4;
                    break;
                case 5:
                    firstLine.Qty = item.MonthQty5;
                    break;
                case 6:
                    firstLine.Qty = item.MonthQty6;
                    break;
                case 7:
                    firstLine.Qty = item.MonthQty7;
                    break;
                case 8:
                    firstLine.Qty = item.MonthQty8;
                    break;
                case 9:
                    firstLine.Qty = item.MonthQty9;
                    break;
                case 10:
                    firstLine.Qty = item.MonthQty10;
                    break;
                case 11:
                    firstLine.Qty = item.MonthQty11;
                    break;
                case 12:
                    firstLine.Qty = item.MonthQty12;
                    break;
            }
        }

        async private Task SetBudgetGroup()
        {
            budgetGroups = budgetGroups ?? await api.LoadCache(typeof(Uniconta.DataModel.ProjectBudgetGroup));
            cmbProjectBudgetGroup.ItemsSource = budgetGroups;
            if (budgetGroups != null && cmbProjectBudgetGroup?.SelectedItem == null)
            {
                if (string.IsNullOrEmpty(defaultBudgetGroup))
                    budgetGrpMaster = ((ProjectBudgetGroup[])budgetGroups?.GetRecords)?.Where(x => x._Default == true)?.FirstOrDefault();
                else
                    budgetGrpMaster = (ProjectBudgetGroup)budgetGroups.Get(defaultBudgetGroup);

                defaultBudgetGroup = budgetGrpMaster?.KeyStr;
                cmbProjectBudgetGroup.SelectedItem = budgetGrpMaster;
            }
        }

        async private Task SetEmployeeNormHours(int currentYear)
        {
            DateTime startOfYear = new DateTime(currentYear, 1, 1);
            DateTime endOfYear = new DateTime(currentYear, 12, 31);

            var byMonth = await FindNormHours.GetByMonth(api, _empMaster.Number, startOfYear, endOfYear);

            var months = new double[12];
            foreach (var kv in byMonth)
            {
                int month = kv.Key.Item2;
                if (month >= 1 && month <= 12)
                    months[month - 1] = kv.Value;
            }

            normHoursMonth1 = months[0];
            normHoursMonth2 = months[1];
            normHoursMonth3 = months[2];
            normHoursMonth4 = months[3];
            normHoursMonth5 = months[4];
            normHoursMonth6 = months[5];
            normHoursMonth7 = months[6];
            normHoursMonth8 = months[7];
            normHoursMonth9 = months[8];
            normHoursMonth10 = months[9];
            normHoursMonth11 = months[10];
            normHoursMonth12 = months[11];

            normHoursTotal = Math.Round(months.Sum(), 2);


            dgProjectBudgetYear?.UpdateTotalSummary();
        }

        async void LoadBudgetYear()
        {
            if (defaultBudgetGroup == null)
            {
                UnicontaMessageBox.Show(string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"), Uniconta.ClientTools.Localization.lookup("BudgetGroup")), Uniconta.ClientTools.Localization.lookup("Information"));
                return;
            }

            try
            {
                busyIndicator.IsBusy = true;

                int currentYear = defaultYear;

                DateTime startOfYear = new DateTime(currentYear, 1, 1);
                DateTime endOfYear = new DateTime(currentYear, 12, 31);

                var propValuePair = new PropValuePair[]
                {
                    PropValuePair.GenereteWhereElements("Date",typeof(DateTime),string.Format("{0}..{1}", startOfYear.ToString(),endOfYear.ToString())),
                    PropValuePair.GenereteWhereElements("Current", typeof(bool), "1")
                };

                _OrginalProjectBugetLines = await api.Query<ProjectBudgetLineClient>(new List<UnicontaBaseEntity>(2) { _empMaster, budgetGrpMaster }, propValuePair);
                _ProjectBudgetYearClients = new List<ProjectBudgetYearClient>(_OrginalProjectBugetLines.Length);

                foreach (var line in _OrginalProjectBugetLines)
                {
                    var month = line.Date.Month;
                    var projectBudgetLine = _ProjectBudgetYearClients.FirstOrDefault(p => IsProjectBudgetLineMatch(p, line));

                    if (projectBudgetLine == null)
                    {
                        projectBudgetLine = new ProjectBudgetYearClient(line);
                        _ProjectBudgetYearClients.Add(projectBudgetLine);
                    }

                    switch (month)
                    {
                        case 1:
                            projectBudgetLine.MonthQty1 += line.Qty;
                            break;
                        case 2:
                            projectBudgetLine.MonthQty2 += line.Qty;
                            break;
                        case 3:
                            projectBudgetLine.MonthQty3 += line.Qty;
                            break;
                        case 4:
                            projectBudgetLine.MonthQty4 += line.Qty;
                            break;
                        case 5:
                            projectBudgetLine.MonthQty5 += line.Qty;
                            break;
                        case 6:
                            projectBudgetLine.MonthQty6 += line.Qty;
                            break;
                        case 7:
                            projectBudgetLine.MonthQty7 += line.Qty;
                            break;
                        case 8:
                            projectBudgetLine.MonthQty8 += line.Qty;
                            break;
                        case 9:
                            projectBudgetLine.MonthQty9 += line.Qty;
                            break;
                        case 10:
                            projectBudgetLine.MonthQty10 += line.Qty;
                            break;
                        case 11:
                            projectBudgetLine.MonthQty11 += line.Qty;
                            break;
                        case 12:
                            projectBudgetLine.MonthQty12 += line.Qty;
                            break;
                    }
                }
                ResetSourceOnGrid();
                await SetEmployeeNormHours(currentYear); // Calculating the norm year for the employee
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
        }

        private bool IsProjectBudgetLineMatch(ProjectBudgetYearClient prBugdetYear, ProjectBudgetLineClient prBudgetLine)
        {
            return prBugdetYear.Project == prBudgetLine.Project &&
                   prBugdetYear.Employee == prBudgetLine.Employee &&
                   prBugdetYear.PayrollCategory == prBudgetLine.PayrollCategory &&
                   prBugdetYear.PrCategory == prBudgetLine.PrCategory &&
                   prBugdetYear.WorkSpace == prBudgetLine.WorkSpace &&
                   prBugdetYear.Task == prBudgetLine.Task;
        }

        private void btnSubYear_Click(object sender, RoutedEventArgs e)
        {
            defaultYear--;
            txtYear.Text = defaultYear.ToString();
        }

        private void btnAddYear_Click(object sender, RoutedEventArgs e)
        {
            defaultYear++;
            txtYear.Text = defaultYear.ToString();
        }

        private void cmbProjectBudgetGroup_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            defaultBudgetGroup = (string)cmbProjectBudgetGroup.EditValue;
            budgetGrpMaster = (ProjectBudgetGroup)budgetGroups.Get(defaultBudgetGroup);
        }

        CorasauGridLookupEditorClient prevPayroll;
        private void Payroll_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgProjectBudgetYear.SelectedItem as ProjectBudgetYearClient;
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

        private void SetPayrollSource(ProjectBudgetYearClient rec)
        {
            ProjectClient project = (ProjectClient)projects?.Get(rec.Project);
            if (project?.ProjectGroup != null)
            {
                payrolls = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory), api);

                rec.PayrollSource = new BudgetYearPayrollFilter(payrolls, project.ProjectGroup._Invoiceable);
                if (rec.PayrollSource != null)
                    rec.NotifyPropertyChanged("PayrollSource");
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.ProjectTask), typeof(Uniconta.DataModel.PrCategory), typeof(Uniconta.DataModel.PrWorkSpace), typeof(Uniconta.DataModel.TMEmpCalendar) });
            projects = projects ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
            payrolls = payrolls ?? await api.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory)).ConfigureAwait(false);
            budgetGroups = budgetGroups ?? await api.LoadCache(typeof(Uniconta.DataModel.ProjectBudgetGroup)).ConfigureAwait(false);

            if (this.priceLookup == null)
                priceLookup = new Uniconta.API.Project.FindPricesEmpl(api);
        }
        protected override Task<ErrorCodes> saveGrid()
        {
            var t = base.saveGrid();
            SaveBudgetYear();
            return t;
        }
    }
}
