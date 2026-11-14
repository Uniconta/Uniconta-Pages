using DevExpress.Data;
using DevExpress.Xpf.Core.Serialization;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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

        // Norm hours for the employee per period 1..12 (index 0..11)
        readonly double[] normHours = new double[12];
        double normHoursTotal;

        // The grid shows 3, 6 or 12 months from a start month, which can be any month, so a period can span two calendar years.
        // MonthQty1..12 are period 1..12 counted from the start month, not January..December. Columns after the number of months are hidden
        static DateTime defaultStart;
        static int defaultMonthCount = 12;
        static string defaultBudgetGroup;
        DateTime loadedStart;
        int loadedMonthCount = 12;
        ProjectBudgetGroup loadedBudgetGroup;
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

            if (defaultStart == DateTime.MinValue)
                defaultStart = new DateTime(DateTime.Now.Year + 1, 1, 1);
            SetPeriodText();
            SetMonthHeaders(defaultStart);
            SetMonthColumnsVisible(defaultMonthCount);
            cmbMonthCount.ItemsSource = new[] { 3, 6, 12 };
            cmbMonthCount.SelectedItem = defaultMonthCount;

            cmbProjectBudgetGroup.api = api;
            dgProjectBudgetYear.api = api;
            dgProjectBudgetYear.BusyIndicator = busyIndicator;
            dgProjectBudgetYear.ShowTotalSummary();
            dgProjectBudgetYear.View.ShowFixedTotalSummary = true;
            // Right align all summary lines in the number columns, also when they have different lengths (e.g. 18,50 below 160,00)
            var summaryContentStyle = (Style)FindResource("RightAlignedSummaryContentStyle");
            foreach (var column in MonthColumns())
                column.TotalSummaryContentStyle = summaryContentStyle;
            TotalQty.TotalSummaryContentStyle = summaryContentStyle;
            dgProjectBudgetYear.Columns.CollectionChanged += Columns_CollectionChanged;
            DXSerializer.AddCreateCollectionItemEventHandler(dgProjectBudgetYear, CreateCollectionItemEventHandler);
            dgProjectBudgetYear.CustomSummary += DgProjectBudgetYear_CustomSummary;
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
        // The grid calls this for every custom summary item. Only the sum the item needs is calculated, so the rows are not summed for all months for each item
        private void DgProjectBudgetYear_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            if (e.SummaryProcess != CustomSummaryProcess.Start || !(e.Item is SumColumn sumColumn))
                return;

            var fieldName = sumColumn.FieldName;
            var tagName = sumColumn.SerializableTag;
            switch (fieldName)
            {
                case "Project":
                    if (tagName == "Sum")
                        e.TotalValue = Uniconta.ClientTools.Localization.lookup("Budget");
                    else if (tagName == "NormHours")
                        e.TotalValue = Uniconta.ClientTools.Localization.lookup("NormHours");
                    else if (tagName == "Total")
                        e.TotalValue = Uniconta.ClientTools.Localization.lookup("Dif");
                    else if (tagName == "Utilization")
                        e.TotalValue = Uniconta.ClientTools.Localization.lookup("UtilizationPct");
                    break;
                case "TotalQty":
                    if (tagName == "EmpNormHoursSum")
                        e.TotalValue = normHoursTotal;
                    else if (tagName == "TotalSum" || tagName == "UtilTotal")
                    {
                        double sum = 0d;
                        for (int period = 1; period <= loadedMonthCount; period++)
                            sum += MonthSum(period);
                        sum = Math.Round(sum, 2);
                        if (tagName == "TotalSum")
                            e.TotalValue = sum - normHoursTotal;
                        else
                            e.TotalValue = ProjectBudgetUtilization.Pct(sum, normHoursTotal);
                    }
                    break;
                default:
                    // MonthQty1..12 with the tags HoursMonth1..12, TotalMonth1..12 and UtilMonth1..12
                    if (fieldName != null && fieldName.StartsWith("MonthQty") && int.TryParse(fieldName.Substring(8), out var p) && p >= 1 && p <= 12)
                    {
                        if (tagName == "HoursMonth" + p)
                            e.TotalValue = normHours[p - 1];
                        else if (tagName == "TotalMonth" + p)
                            e.TotalValue = MonthSum(p) - normHours[p - 1];
                        else if (tagName == "UtilMonth" + p)
                            e.TotalValue = ProjectBudgetUtilization.Pct(MonthSum(p), normHours[p - 1]);
                    }
                    break;
            }
        }

        // Recalculates the totals and marks the months where the budget is below the norm hours
        void RefreshTotals()
        {
            dgProjectBudgetYear.UpdateTotalSummary();
            UpdateUnderbookedHeaders();
        }

        void UpdateUnderbookedHeaders()
        {
            var template = (DataTemplate)FindResource("UnderbookedHeaderTemplate");
            var columns = MonthColumns();
            for (int i = 0; i < columns.Length; i++)
                columns[i].HeaderTemplate = ProjectBudgetUtilization.IsUnderbooked(MonthSum(i + 1), normHours[i]) ? template : null;
        }

        double MonthSum(int period)
        {
            double sum = 0d;
            if (dgProjectBudgetYear.ItemsSource is IEnumerable<ProjectBudgetYearClient> rows)
                foreach (var row in rows)
                    sum += row.GetMonthQty(period);
            return sum;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            dgProjectBudgetYear.tableView.TotalSummaryElementStyle = (Style)FindResource("SummaryTotalStyle");
            // A saved layout can bring back the headers and visible month columns it was saved with
            bool isLoaded = loadedStart != DateTime.MinValue;
            SetMonthHeaders(isLoaded ? loadedStart : defaultStart);
            SetMonthColumnsVisible(isLoaded ? loadedMonthCount : defaultMonthCount);
        }

        // Period 1..12 for a date, counted from the loaded start month. Outside the loaded months gives a value outside 1..number of months
        int PeriodIndex(DateTime date)
        {
            return (date.Year - loadedStart.Year) * 12 + date.Month - loadedStart.Month + 1;
        }

        // Budget lines are dated at the end of their month
        DateTime PeriodEnd(int period)
        {
            var month = loadedStart.AddMonths(period - 1);
            return new DateTime(month.Year, month.Month, DateTime.DaysInMonth(month.Year, month.Month));
        }

        static string MonthText(DateTime month)
        {
            var name = Uniconta.ClientTools.Localization.lookup(CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month.Month));
            if (name.Length > 3)
                name = name.Substring(0, 3);
            return string.Concat(name, " ", month.ToString("yy", CultureInfo.InvariantCulture));
        }

        CorasauDataGridTemplateColumnClient[] MonthColumns()
        {
            return new[] { MonthQty1, MonthQty2, MonthQty3, MonthQty4, MonthQty5, MonthQty6, MonthQty7, MonthQty8, MonthQty9, MonthQty10, MonthQty11, MonthQty12 };
        }

        void SetMonthHeaders(DateTime start)
        {
            var columns = MonthColumns();
            for (int i = 0; i < columns.Length; i++)
                columns[i].Header = MonthText(start.AddMonths(i));
        }

        // Only the months in the period are shown. The rest are also left out of the column chooser, as they have no data
        void SetMonthColumnsVisible(int monthCount)
        {
            var columns = MonthColumns();
            for (int i = 0; i < columns.Length; i++)
            {
                columns[i].Visible = i < monthCount;
                columns[i].ShowInColumnChooser = i < monthCount;
            }
        }

        void SetPeriodText()
        {
            var end = defaultStart.AddMonths(defaultMonthCount - 1);
            txtYear.Text = string.Format("{0} - {1}", MonthText(defaultStart), MonthText(end));
        }

        // Fields added with Layout - "Andre felter" are typically text about the project, employee etc., so they are put with the
        // key columns in the frozen left section. Only while that dialog is used, so a saved layout keeps its own column placement
        // The columns before the dialog was opened. The dialog adds its columns inside BeginUpdate/EndUpdate, which only raises a Reset
        // without the new columns, so new columns are found by comparing with these
        HashSet<GridColumn> columnsBeforeNestedProperty;
        private void Columns_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var before = columnsBeforeNestedProperty;
            if (before == null)
                return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (var column in dgProjectBudgetYear.Columns)
                    if (!before.Contains(column) && column.FieldName != null && column.FieldName.Contains(".") && column.Fixed != FixedStyle.Left)
                        column.Fixed = FixedStyle.Left;
            }));
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            columnsBeforeNestedProperty = ActionType == "AddNestedProperty" ? new HashSet<GridColumn>(dgProjectBudgetYear.Columns) : null;
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
                    _ = SaveBudgetYear();
                    break;
                case "DistributeHours":
                    DistributeHours();
                    break;
                case "CopyToRestOfPeriod":
                    CopyToRestOfPeriod();
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
                var selectedRow = dgProjectBudgetYear.SelectedItem as ProjectBudgetYearClient;
                var projectBudgetLine = api.CompanyEntity.CreateUserType<ProjectBudgetLineClient>();
                projectBudgetLine.SetMaster(_empMaster);
                if (selectedRow != null)
                    projectBudgetLine.Project = selectedRow.Project;
                var newRow = new ProjectBudgetYearClient(projectBudgetLine) { IsModified = true };

                // Insert below the selected row in the order shown, and do not sort again, so the grid stays where the user is working
                var rows = (dgProjectBudgetYear.ItemsSource as IEnumerable<ProjectBudgetYearClient>)?.ToList() ?? new List<ProjectBudgetYearClient>();
                var pos = selectedRow != null ? rows.IndexOf(selectedRow) + 1 : 0;
                rows.Insert(pos > 0 ? pos : 0, newRow);
                _ProjectBudgetYearClients = rows;

                ResetSourceOnGrid(false);
                dgProjectBudgetYear.SelectedItem = newRow;
                dgProjectBudgetYear.View.FocusedRowHandle = dgProjectBudgetYear.GetRowHandleByListIndex(rows.IndexOf(newRow));
                isDataChaged = true;
            }
            catch (System.ArgumentException)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InternalError"), Uniconta.ClientTools.Localization.lookup("Exception"));
                return;
            }
        }

        // Spreads the row's total for the period evenly over the months in the period
        void DistributeHours()
        {
            var row = dgProjectBudgetYear.SelectedItem as ProjectBudgetYearClient;
            if (row == null || isSaving)
                return;
            // Close the editor, not only post it. An open editor writes its value back over the changed months when it closes
            dgProjectBudgetYear.View.CloseEditor();

            int months = loadedMonthCount;
            double total = 0d;
            for (int period = 1; period <= months; period++)
                if (!row.IsMonthEmpty(period))
                    total += row.GetMonthQty(period);
            if (total == 0d)
                return;

            // Rounded to 2 decimals. The rounding difference goes to the last month, so the total is unchanged
            var qty = Math.Round(total / months, 2);
            for (int period = 1; period < months; period++)
                row.SetMonthQty(period, qty);
            row.SetMonthQty(months, Math.Round(total - qty * (months - 1), 2));
            row.IsModified = true;
            isDataChaged = true;
            RefreshTotals();
        }

        // Copies the hours in the focused month to the following months of the period
        void CopyToRestOfPeriod()
        {
            var row = dgProjectBudgetYear.SelectedItem as ProjectBudgetYearClient;
            var fieldName = dgProjectBudgetYear.CurrentColumn?.FieldName;
            if (row == null || isSaving || fieldName == null || !fieldName.StartsWith("MonthQty") ||
                !int.TryParse(fieldName.Substring(8), out var fromPeriod) || fromPeriod < 1 || fromPeriod >= loadedMonthCount)
                return;
            // Close the editor, not only post it. An open editor writes its value back over the changed months when it closes
            dgProjectBudgetYear.View.CloseEditor();

            var qty = row.GetMonthQty(fromPeriod);
            for (int period = fromPeriod + 1; period <= loadedMonthCount; period++)
                row.SetMonthQty(period, qty);
            row.IsModified = true;
            isDataChaged = true;
            RefreshTotals();
        }

        async void DeleteRow()
        {
            var selectedRow = dgProjectBudgetYear.SelectedItem as ProjectBudgetYearClient;
            if (selectedRow == null || isSaving || _ProjectBudgetYearClients == null)
                return;

            var name = string.Format("{0} {1} - {2} {3}", selectedRow.Project, selectedRow.ProjectName, selectedRow.PayrollCategory, selectedRow.PayrollCategoryName);
            if (UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), name), Uniconta.ClientTools.Localization.lookup("Confirmation"),
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            // The row is deleted right away. Lines from the database are deleted as loaded, also if key fields have been changed on the row
            var orgLines = selectedRow.OriginalLines;
            if (orgLines != null && orgLines.Count > 0)
            {
                busyIndicator.IsBusy = true;
                var err = await api.Delete(orgLines);
                busyIndicator.IsBusy = false;
                if (err != ErrorCodes.Succes)
                {
                    UtilDisplay.ShowErrorCode(err);
                    return;
                }
            }

            var rowHandle = dgProjectBudgetYear.View.FocusedRowHandle;
            _ProjectBudgetYearClients.Remove(selectedRow);
            // Deleting the only changed row, e.g. a new row that was never saved, leaves nothing to save
            isDataChaged = _ProjectBudgetYearClients.Any(r => r.IsModified);
            ResetSourceOnGrid();
            if (_ProjectBudgetYearClients.Count > 0)
                dgProjectBudgetYear.View.FocusedRowHandle = Math.Min(Math.Max(rowHandle, 0), _ProjectBudgetYearClients.Count - 1);
        }

        void ResetSourceOnGrid(bool sort = true)
        {
            dgProjectBudgetYear.ItemsSource = null;
            var _ProjectBudgetYearClientArr = _ProjectBudgetYearClients.ToArray();
            if (sort)
                Array.Sort(_ProjectBudgetYearClientArr, new ProjectBudgetYearSort());
            dgProjectBudgetYear.ItemsSource = _ProjectBudgetYearClientArr;
            dgProjectBudgetYear.Visibility = Visibility.Visible;
            UpdateUnderbookedHeaders();
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
            if (sender is ProjectBudgetYearClient row)
                row.IsModified = true;
            // The grid only recalculates the totals when the row is left, so do it when a month is changed (when the cell is left)
            if (e.PropertyName != null && e.PropertyName.StartsWith("MonthQty"))
                RefreshTotals();
        }

        bool isSaving;
        async Task<ErrorCodes> SaveBudgetYear()
        {
            // Ctrl+S performs both "Save" and "SaveGrid", which both end here. Two concurrent saves would work on the same original lines
            if (isSaving || _ProjectBudgetYearClients == null || _ProjectBudgetYearClients.Count == 0 || loadedBudgetGroup == null)
                return ErrorCodes.Succes;

            isSaving = true;
            try
            {
                dgProjectBudgetYear.View.PostEditor();
                return await DoSaveBudgetYear();
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
                return ErrorCodes.Exception;
            }
            finally
            {
                isSaving = false;
            }
        }

        async Task<ErrorCodes> DoSaveBudgetYear()
        {
            int prBudgetYearCount = _ProjectBudgetYearClients.Count;

            var insertBudgetLine = new List<ProjectBudgetLineClient>(prBudgetYearCount);
            // Updates are sent as pairs (original + modified). A plain update list in MultiCrud is written by a background job on the server,
            // so the reload after save could show lines that are not updated yet
            var updateBudgetLine = new List<Uniconta.API.System.CrudAPI.UpdatePair>(prBudgetYearCount);
            var deleteBudgetLine = new List<ProjectBudgetLineClient>(prBudgetYearCount);
            var newRows = new List<ProjectBudgetYearClient>();

            // Each row is saved against the budget lines it was loaded from, so changing a key field (employee, payroll, workspace etc.)
            // moves the existing lines instead of leaving them behind and inserting new ones
            foreach (var row in _ProjectBudgetYearClients)
            {
                var orgLines = row.OriginalLines;
                if (orgLines == null || orgLines.Count == 0)
                {
                    newRows.Add(row);
                    continue;
                }

                // Project is part of the primary key of a budget line, so a project change is delete + insert
                if (row.OrgProject != row.Project)
                {
                    deleteBudgetLine.AddRange(orgLines);
                    newRows.Add(row);
                    continue;
                }

                bool keyChanged = row.IsKeyChanged;
                bool priceChanged = keyChanged && (row.OrgEmployee != row.Employee || row.OrgPayrollCategory != row.PayrollCategory);
                int budgetId = orgLines[0].PrimaryKey1Id;

                for (int month = 1; month <= loadedMonthCount; month++)
                {
                    var originalLinesForMonth = orgLines.Where(o => PeriodIndex(o.Date) == month).ToList();

                    if (row.IsMonthEmpty(month))
                    {
                        // Quantity is no longer set — delete all original lines for this month
                        deleteBudgetLine.AddRange(originalLinesForMonth);
                    }
                    else if (originalLinesForMonth.Count == 0)
                    {
                        // No original line, but quantity is now set — insert new line in the same budget
                        var newLine = CreateNewBudgetLineClient(row);
                        newLine.PrimaryKey1Id = budgetId;
                        UpdateNewBudgetLine(row, newLine, month);
                        await priceLookup.GetEmployeePrice(newLine);
                        insertBudgetLine.Add(newLine);
                    }
                    else
                    {
                        // Update last line, delete others
                        var lastOriginal = originalLinesForMonth.OrderBy(l => l.Date).Last();
                        var loaded = StreamingManager.Clone(lastOriginal);

                        lastOriginal.Qty = row.GetMonthQty(month);

                        // Set date to month-end if needed
                        if (lastOriginal.Date.Day != DateTime.DaysInMonth(lastOriginal.Date.Year, lastOriginal.Date.Month))
                            lastOriginal.Date = new DateTime(lastOriginal.Date.Year, lastOriginal.Date.Month, DateTime.DaysInMonth(lastOriginal.Date.Year, lastOriginal.Date.Month));

                        if (keyChanged)
                        {
                            lastOriginal.Employee = row.Employee;
                            lastOriginal.PayrollCategory = row.PayrollCategory;
                            lastOriginal.PrCategory = row.PrCategory;
                            lastOriginal.WorkSpace = row.WorkSpace;
                            lastOriginal.Task = row.Task;
                            if (priceChanged)
                                await priceLookup.GetEmployeePrice(lastOriginal);
                        }

                        updateBudgetLine.Add(new Uniconta.API.System.CrudAPI.UpdatePair() { loaded = loaded, modified = lastOriginal });

                        // Delete remaining lines in the same month
                        deleteBudgetLine.AddRange(originalLinesForMonth.Where(l => l != lastOriginal));
                    }
                }
            }

            var result = ErrorCodes.Succes;

            if (newRows.Count > 0)
            {
                var projBudgetArr = await api.Query<ProjectBudget>();
                var group = loadedBudgetGroup.KeyStr;

                // Budget header per project in the loaded budget group, preferring the current one
                var budgetsGrouped = projBudgetArr.Where(b => b._Group == group).GroupBy(b => b._Project).ToDictionary(g => g.Key, g => g.FirstOrDefault(b => b._Current) ?? g.First());

                foreach (var proj in newRows.GroupBy(s => s.Project))
                {
                    var projectNo = proj.Key;
                    budgetsGrouped.TryGetValue(projectNo, out var projBudget);

                    bool createHeader = projBudget == null;

                    if (createHeader)
                    {
                        var project = (Uniconta.DataModel.Project)projects.Get(projectNo);
                        if (project == null)
                            continue;

                        projBudget = new ProjectBudget();
                        projBudget.SetMaster(project);
                        projBudget._Group = group;
                        projBudget._Name = Uniconta.ClientTools.Localization.lookup("Budget");
                        projBudget._Current = true;
                    }

                    var insertNewBudgetLine = new List<ProjectBudgetLineClient>();
                    foreach (var newAgg in proj)
                    {
                        for (int month = 1; month <= loadedMonthCount; month++)
                        {
                            if (newAgg.IsMonthEmpty(month))
                                continue;

                            var newLine = CreateNewBudgetLineClient(newAgg);
                            if (!createHeader)
                                newLine.SetMaster(projBudget); // otherwise the server puts the line in the project's anchor budget, which can be another budget group
                            UpdateNewBudgetLine(newAgg, newLine, month);
                            await priceLookup.GetEmployeePrice(newLine);

                            if (createHeader)
                                insertNewBudgetLine.Add(newLine);
                            else
                                insertBudgetLine.Add(newLine);
                        }
                    }

                    if (createHeader && insertNewBudgetLine.Count > 0)
                    {
                        var res = await api.Insert(projBudget, insertNewBudgetLine);
                        if (res != ErrorCodes.Succes)
                            result = res;
                    }
                }
            }

            var crudResult = await api.MultiCrud(insertBudgetLine, updateBudgetLine, deleteBudgetLine);
            if (crudResult != ErrorCodes.Succes)
                result = crudResult;

            // Reload in both cases, so the grid shows what is actually saved and a retry can not insert lines twice
            isDataChaged = false;
            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
            LoadBudgetYear();
            return result;
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

        private void UpdateNewBudgetLine(ProjectBudgetYearClient item, ProjectBudgetLineClient prjBudgetLine, int period)
        {
            prjBudgetLine.Date = PeriodEnd(period);
            prjBudgetLine.Qty = item.GetMonthQty(period);
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

        async private Task SetEmployeeNormHours(DateTime start, DateTime end, int request)
        {
            var byMonth = await FindNormHours.GetByMonth(api, _empMaster.Number, start, end);
            if (request != loadRequest)
                return;

            // Key is (year, month)
            var months = new double[12];
            foreach (var kv in byMonth)
            {
                int period = (kv.Key.Item1 - start.Year) * 12 + kv.Key.Item2 - start.Month + 1;
                if (period >= 1 && period <= 12)
                    months[period - 1] = kv.Value;
            }

            Array.Copy(months, normHours, 12);

            normHoursTotal = Math.Round(months.Sum(), 2);


            RefreshTotals();
        }

        // Clicking the period arrows quickly starts several loads. Only the latest may update the grid
        int loadRequest;
        async void LoadBudgetYear()
        {
            if (defaultBudgetGroup == null)
            {
                UnicontaMessageBox.Show(string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"), Uniconta.ClientTools.Localization.lookup("BudgetGroup")), Uniconta.ClientTools.Localization.lookup("Information"));
                return;
            }

            int request = 0;
            try
            {
                if (!await ConfirmUnsavedChanges())
                    return;

                // Taken after the question: saving from it starts its own load, and this load must be the latest, or nobody stops the busy indicator
                request = ++loadRequest;
                busyIndicator.IsBusy = true;

                var start = defaultStart;
                var monthCount = defaultMonthCount;
                var end = start.AddMonths(monthCount).AddDays(-1);
                // Lines are read for 12 months, so a shorter period shows the same rows as 12 months, also rows without hours in the shown months
                var rowsEnd = start.AddMonths(12).AddDays(-1);

                var propValuePair = new PropValuePair[]
                {
                    PropValuePair.GenereteWhereElements("Date",typeof(DateTime),string.Format("{0}..{1}", start.ToString(), rowsEnd.ToString())),
                    PropValuePair.GenereteWhereElements("Current", typeof(bool), "1")
                };

                var lines = await api.Query<ProjectBudgetLineClient>(new List<UnicontaBaseEntity>(2) { _empMaster, budgetGrpMaster }, propValuePair);
                if (request != loadRequest)
                    return;
                _OrginalProjectBugetLines = lines;
                loadedStart = start;
                loadedMonthCount = monthCount;
                SetMonthColumnsVisible(monthCount);
                loadedBudgetGroup = budgetGrpMaster;
                SetMonthHeaders(start);
                _ProjectBudgetYearClients = new List<ProjectBudgetYearClient>(_OrginalProjectBugetLines.Length);

                foreach (var line in _OrginalProjectBugetLines)
                {
                    var month = PeriodIndex(line.Date);
                    if (month < 1 || month > 12)
                        continue;
                    var projectBudgetLine = _ProjectBudgetYearClients.FirstOrDefault(p => IsProjectBudgetLineMatch(p, line));

                    if (projectBudgetLine == null)
                    {
                        projectBudgetLine = new ProjectBudgetYearClient(line);
                        projectBudgetLine.OriginalLines = new List<ProjectBudgetLineClient>();
                        _ProjectBudgetYearClients.Add(projectBudgetLine);
                    }

                    // Lines after the shown months only give the row. They are not original lines, so save and delete do not touch them
                    if (month > loadedMonthCount)
                        continue;
                    projectBudgetLine.OriginalLines.Add(line);

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
                await SetEmployeeNormHours(start, end, request); // Calculating the norm hours for the employee
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
            finally
            {
                if (request != 0 && request == loadRequest)
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

        // « and » move a whole period (3, 6 or 12 months)
        private void btnSubYear_Click(object sender, RoutedEventArgs e)
        {
            ChangePeriod(-defaultMonthCount);
        }

        private void btnAddYear_Click(object sender, RoutedEventArgs e)
        {
            ChangePeriod(defaultMonthCount);
        }

        bool isRevertingMonthCount;
        private async void cmbMonthCount_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (isRevertingMonthCount || !(cmbMonthCount.SelectedItem is int monthCount) || monthCount == defaultMonthCount)
                return;

            if (!await ConfirmUnsavedChanges())
            {
                isRevertingMonthCount = true;
                try { cmbMonthCount.SelectedItem = defaultMonthCount; }
                finally { isRevertingMonthCount = false; }
                return;
            }

            defaultMonthCount = monthCount;
            SetPeriodText();
            if (budgetGrpMaster != null)
                LoadBudgetYear();
            else
                SetMonthColumnsVisible(monthCount);
        }

        private void btnSubMonth_Click(object sender, RoutedEventArgs e)
        {
            ChangePeriod(-1);
        }

        private void btnAddMonth_Click(object sender, RoutedEventArgs e)
        {
            ChangePeriod(1);
        }

        async void ChangePeriod(int months)
        {
            if (!await ConfirmUnsavedChanges())
                return;

            defaultStart = defaultStart.AddMonths(months);
            SetPeriodText();

            // Show the selected period right away, otherwise the grid shows another period than the one in the period field
            if (budgetGrpMaster != null)
                LoadBudgetYear();
        }

        bool isRevertingBudgetGroup;
        private async void cmbProjectBudgetGroup_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (isRevertingBudgetGroup)
                return;

            var group = (ProjectBudgetGroup)budgetGroups.Get((string)cmbProjectBudgetGroup.EditValue);

            // Before the first load (the page sets the group itself) or when nothing changed, there is nothing to reload
            if (loadedBudgetGroup == null || group == null || group.KeyStr == loadedBudgetGroup.KeyStr)
            {
                defaultBudgetGroup = group?.KeyStr;
                budgetGrpMaster = group;
                return;
            }

            if (!await ConfirmUnsavedChanges())
            {
                // Unsaved changes belong to the loaded group, so keep that group selected
                isRevertingBudgetGroup = true;
                try { cmbProjectBudgetGroup.SelectedItem = loadedBudgetGroup; }
                finally { isRevertingBudgetGroup = false; }
                return;
            }

            defaultBudgetGroup = group.KeyStr;
            budgetGrpMaster = group;
            LoadBudgetYear();
        }

        // Before the grid is reloaded (search, period or budget group change). Unsaved changes are saved or discarded as the user chooses.
        // Returns false when the user cancels or the save fails, and the reload must not happen
        async Task<bool> ConfirmUnsavedChanges()
        {
            // A value still in the cell editor is not on the row yet, so it would not count as a change and would be lost by the reload
            dgProjectBudgetYear.View.CloseEditor();
            if (!isDataChaged)
                return true;

            switch (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.YesNoCancel))
            {
                case MessageBoxResult.Yes:
                    return await SaveBudgetYear() == ErrorCodes.Succes;
                case MessageBoxResult.No:
                    isDataChaged = false;
                    return true;
                default:
                    return false;
            }
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
        protected override async Task<ErrorCodes> saveGrid()
        {
            var err = await base.saveGrid();
            if (err != ErrorCodes.Succes)
                return err;
            return await SaveBudgetYear();
        }
    }
}
