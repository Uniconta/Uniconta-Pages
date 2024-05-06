
using System;
using System.Collections;
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
using Uniconta.API.Project;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class WorkInProgressReportGridCopy : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTransLocalClientCopy); } }
    }

    public partial class WorkInProgressReportPageCopy : GridBasePage
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
        static bool showWorkspace;
        static bool includeJournals;

        ItemBase iIncludeZeroBalance, iIncludeJournals, iShowTask, iShowWorkspace;
        bool hasWorkspace;

        private const string AND_OPERATOR = "And";
        private const string FILTERVALUE_ZEROBALANCE = @"([OnAccount] <> 0.0 Or [ClosingBalance] <> 0.0)";

        SQLTableCache<Uniconta.DataModel.Project> projCache;

        public WorkInProgressReportPageCopy(BaseAPI API) : base(API, string.Empty)
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
            iIncludeJournals.isEditLayout = false;

            showTask = api.CompanyEntity.ProjectTask && showTask;
            iShowTask.IsChecked = showTask;
            iShowTask.isEditLayout = api.CompanyEntity.ProjectTask;
            cmbProject.api = api;
            SetProjectSource();

            var workSpaceCache = api.GetCache(typeof(Uniconta.DataModel.PrWorkSpace));
            hasWorkspace = (workSpaceCache == null || workSpaceCache.Count != 0);
            Task.Visible = showTask;
            Workspace.Visible = hasWorkspace && (showWorkspace || showTask);
            iShowWorkspace.isEditLayout = hasWorkspace && !showTask;
            iShowWorkspace.IsChecked = hasWorkspace && (showWorkspace || showTask);

            if (WorkInProgressReportPageCopy.DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();

                var fromDate = new DateTime(now.Year, now.Month, 1);
                fromDate = fromDate.AddMonths(-2);

                WorkInProgressReportPageCopy.DefaultToDate = now;
                WorkInProgressReportPageCopy.DefaultFromDate = fromDate;
            }

            txtDateTo.DateTime = WorkInProgressReportPageCopy.DefaultToDate;
            txtDateFrm.DateTime = WorkInProgressReportPageCopy.DefaultFromDate;
            WorkInProgressReportPageCopy.SetDateTime(txtDateFrm, txtDateTo);

            LoadGrid();
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

        async private void SetProjectSource()
        {
            projCache = api.GetCache<Uniconta.DataModel.Project>() ?? await this.api.LoadCache<Uniconta.DataModel.Project>();
            cmbProject.ItemsSource = projCache;
            StartLoadCache();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (!api.CompanyEntity.ProjectTask)
                this.Task.Visible = this.Task.ShowInColumnChooser = false;
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
            iShowWorkspace = UtilDisplay.GetMenuCommandByName(rb, "ShowWorkspace");
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
                case "ShowWorkspace":
                    showWorkspace = IsChecked;
                    iShowWorkspace.IsChecked = showWorkspace;
                    Workspace.Visible = showWorkspace;

                    if (dgWorkInProgressRpt.ItemsSource != null)
                        LoadGrid();
                    break;
                case "ShowTasks":
                    showTask = IsChecked;
                    iShowTask.IsChecked = showTask;
                    Task.Visible = showTask;
                    Workspace.Visible = hasWorkspace && (showWorkspace || showTask);
                    iShowWorkspace.isEditLayout = hasWorkspace && !showTask;
                    iShowWorkspace.IsChecked = hasWorkspace && (showWorkspace || showTask);

                    if (dgWorkInProgressRpt.ItemsSource != null)
                        LoadGrid();
                    break;
                case "InclJournals":
                    includeJournals = IsChecked;
                    iIncludeJournals.IsChecked = includeJournals;
                    StaticValuesCopy.IncludeJournals = includeJournals;
                    await IncludeJournals();
                    break;
            }

            dgWorkInProgressRpt.UpdateTotalSummary();
        }

        class TmpprojectSum
        {
            public string Project;
            public double EmployeeHoursJournal, EmployeeFeeJournal, EmployeeFeeJournalCostValue;
        }

        private void SetIncludeFilter()
        {
            string filterString = dgWorkInProgressRpt.FilterString ?? string.Empty;
            if (includeZeroBalance)
            {
                filterString = filterString.Replace(FILTERVALUE_ZEROBALANCE, string.Empty).Trim();
                if (filterString != string.Empty && filterString.StartsWith(AND_OPERATOR, true, null))
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

        bool tmLinesWIPLoaded;

        async private Task IncludeJournals()
        {
            var wipLst = (IEnumerable<ProjectTransLocalClientCopy>)dgWorkInProgressRpt.ItemsSource;
            foreach (var rec in wipLst)
            {
                rec.NotifyClosingBalance();
            }

            dgWorkInProgressRpt.Columns.GetColumnByName("EmployeeHoursJournal").Visible = includeJournals;
            dgWorkInProgressRpt.Columns.GetColumnByName("EmployeeFeeJournal").Visible = includeJournals;

            if (tmLinesWIPLoaded)
                return;

            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            busyIndicator.IsBusy = true;

            var empCache = api.GetCache<Uniconta.DataModel.Employee>() ?? await api.LoadCache<Uniconta.DataModel.Employee>();
            var priceLookup = new Uniconta.API.Project.FindPricesEmpl(api);

            var pairTM = new List<PropValuePair>(3)
            {
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.RegistrationType), typeof(int), "0"),
                PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Invoiceable), typeof(bool), "1")
            };

            var minApproveDate = empCache.Where(x => x._TMApproveDate != DateTime.MinValue && x._Terminated == DateTime.MinValue).Min(x => x._TMApproveDate as DateTime?) ?? DateTime.MinValue;
            if (minApproveDate != DateTime.MinValue)
                pairTM.Add(PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), minApproveDate, CompareOperator.GreaterThanOrEqual));

            var tmJourLines = await api.Query<TMJournalLineClient>(projCache.Get(cmbProject.Text), pairTM);

            var tmLines = tmJourLines.Where(s => (s._Project != null &&
                                                  s._PayrollCategory != null &&
                                                  s._Date > empCache.Get(s._Employee)._TMApproveDate))
                                                  .GroupBy(x => new { x._Employee, x._Project, x._PayrollCategory, x._WorkSpace, x._Task, x._Date }).Select(x => new TMJournalLineHelper.TMJournalLineClientLocal
                                                  {
                                                      Date = x.Key._Date,
                                                      Project = x.Key._Project,
                                                      Employee = x.Key._Employee,
                                                      PayrollCategory = x.Key._PayrollCategory,
                                                      Task = x.Key._Task,
                                                      WorkSpace = x.Key._WorkSpace,
                                                      Day1 = x.Sum(y => y._Day1),
                                                      Day2 = x.Sum(y => y._Day2),
                                                      Day3 = x.Sum(y => y._Day3),
                                                      Day4 = x.Sum(y => y._Day4),
                                                      Day5 = x.Sum(y => y._Day5),
                                                      Day6 = x.Sum(y => y._Day6),
                                                      Day7 = x.Sum(y => y._Day7),
                                                  }).ToList();

            var tmLinesWIP = new Dictionary<string, TmpprojectSum>(100);
            var localtmLines = new List<TMJournalLineClient>(100);
            string lastEmployee = null;

            var grpEmpDate = tmLines.GroupBy(x => new { x.Employee, x.Date }).Select(g => new { g.Key.Employee, g.Key.Date });
            foreach (var row in grpEmpDate)
            {
                var emp = empCache.Get(row.Employee);
                var startDate = emp._TMApproveDate < row.Date ? row.Date : emp._TMApproveDate >= row.Date.AddDays(6) ? DateTime.MinValue : emp._TMApproveDate;
                var endDate = row.Date.AddDays(6);

                if (startDate != DateTime.MinValue)
                {
                    if (lastEmployee == null || lastEmployee != emp._Number)
                    {
                        lastEmployee = emp._Number;
                        await priceLookup.EmployeeChanged(emp);
                    }

                    localtmLines.Clear();
                    foreach (var s in tmLines)
                        if (s._Date == row.Date && s._Employee == emp._Number)
                            localtmLines.Add(s);

                    await priceLookup.GetEmployeePrice(localtmLines);

                    foreach (var rec in localtmLines)
                    {
                        var lookupValue = showWorkspace || showTask ? string.Concat(rec._Project, rec._WorkSpace, rec._Task) : rec._Project;

                        TmpprojectSum lineWIP;
                        if (tmLinesWIP.TryGetValue(lookupValue, out lineWIP))
                        {
                            lineWIP.EmployeeHoursJournal += rec.Total;
                            lineWIP.EmployeeFeeJournal += rec.TotalSalesPrice;
                            lineWIP.EmployeeFeeJournalCostValue += rec.TotalCostPrice;
                        }
                        else
                        {
                            tmLinesWIP.Add(lookupValue,
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

            wipLst = (IEnumerable<ProjectTransLocalClientCopy>)dgWorkInProgressRpt.ItemsSource;
            foreach (var rec in wipLst)
            {
                TmpprojectSum lineWIP;
                var lookupValue = showWorkspace || showTask ? string.Concat(rec._Project, rec._Workspace, rec._Task) : rec._Project;
                if (tmLinesWIP.TryGetValue(lookupValue, out lineWIP))
                {
                    rec._EmployeeHoursJournal += (long)(lineWIP.EmployeeHoursJournal * 100d);
                    rec._EmployeeFeeJournal += (long)(lineWIP.EmployeeFeeJournal * 100d);
                    rec._EmployeeFeeJournalCostValue += (long)(lineWIP.EmployeeFeeJournalCostValue * 100d);
                    rec.NotifyEmployeeJournal();
                    rec.NotifyClosingBalance();
                }
            }

            tmLinesWIPLoaded = true;
            busyIndicator.IsBusy = false;
        }

        async void LoadGrid()
        {
            SetDateTime(txtDateFrm, txtDateTo);
            tmLinesWIPLoaded = false;

            busyIndicator.IsBusy = true;
            var lst = await new ReportAPI(api).GetWIPTotals(new ProjectTransLocalClientCopy(), DefaultFromDate, DefaultToDate, cmbProject.Text, showTask, showWorkspace, includeJournals);
            Array.Sort(lst, new sortTransCopy());

            dgWorkInProgressRpt.ItemsSource = lst;
            dgWorkInProgressRpt.Visibility = Visibility.Visible;

            iIncludeJournals.isEditLayout = true;

            busyIndicator.IsBusy = false;

            if (includeJournals)
                IncludeJournals();
        }

        private void leFromPerInChre_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
                fromPerInCharge = (string)e.NewValue;
        }

        private void leToPerInChre_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
                toPerInCharge = (string)e.NewValue;
        }

        private void leFromDebtor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
                fromDebtor = (string)e.NewValue;
        }

        private void leToDebtor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (e.NewValue != null)
                toDebtor = (string)e.NewValue;
        }

        void dgWorkInProgressRpt_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("Transactions");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgWorkInProgressRpt.SelectedItem as ProjectTransLocalClientCopy;
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
                case "ProjectInvoiceProposal":
                    if (selectedItem != null)
                    {
                        var salesHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceProposal"), selectedItem.Debtor);
                        AddDockItem(TabControls.ProjInvProposal, selectedItem.ProjectRef, salesHeader);
                    }
                    break;
                case "ZeroInvoice":
                    if (selectedItem != null)
                        CreateZeroInvoice(selectedItem);
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
                case "ViewNotes":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, selectedItem.ProjectRef, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Notes"), selectedItem?.ProjectRef?._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void ProjectName_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgWorkInProgressRpt_RowDoubleClick();
        }

        private async void CreateOrder(ProjectTransLocalClientCopy selectedItem)
        {
            var project = (ProjectClient)projCache.Get(selectedItem.Project);
            ProjectTaskClient projTask = null;
            if (api.CompanyEntity.ProjectTask && selectedItem.Task != null)
            {
                var projTaskLst = project.Tasks ?? await project.LoadTasks(api);
                projTask = projTaskLst?.Where(s => s.Task == selectedItem.Task).FirstOrDefault();
            }

            var workSpaceCache = api.GetCache(typeof(Uniconta.DataModel.PrWorkSpace));
            var prWorkspace = (PrWorkSpaceClient)workSpaceCache.Get(selectedItem.Workspace);
            var cwCreateOrder = new UnicontaClient.Pages.CWCreateOrderFromProject(api, true, project, projTask, prWorkspace);
            cwCreateOrder.DialogTableId = 2000000053;
            cwCreateOrder.Closed += async delegate
            {
                if (cwCreateOrder.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    busyIndicator.IsBusy = true;

                    var debtorOrderInstance = api.CompanyEntity.CreateUserType<ProjectInvoiceProposalClient>();
                    var invoiceApi = new Uniconta.API.Project.InvoiceAPI(api);
                    var result = await invoiceApi.CreateOrderFromProject(debtorOrderInstance, project._Number, CWCreateOrderFromProject.InvoiceCategory, CWCreateOrderFromProject.GenrateDate,
                        CWCreateOrderFromProject.FromDate, CWCreateOrderFromProject.ToDate, cwCreateOrder.ProjectTask, cwCreateOrder.ProjectWorkspace);
                    busyIndicator.IsBusy = false;
                    if (result != ErrorCodes.Succes)
                    {
                        if (result == ErrorCodes.NoLinesToUpdate)
                        {
                            var message = string.Format("{0}. {1}?", Uniconta.ClientTools.Localization.lookup(result.ToString()), string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceProposal")));
                            var res = UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Message"), UnicontaMessageBox.YesNo);
                            if (res == MessageBoxResult.Yes)
                            {
                                debtorOrderInstance.SetMaster(selectedItem.ProjectRef);
                                debtorOrderInstance._PrCategory = CWCreateOrderFromProject.InvoiceCategory;
                                if (debtorOrderInstance._PrCategory == null)
                                {
                                    var CategoryCache = api.CompanyEntity.GetCache(typeof(PrCategory));
                                    foreach (var cat in (IEnumerable<PrCategory>)CategoryCache.GetRecords)
                                    {
                                        if (cat._CatType == Uniconta.DataModel.CategoryType.Revenue)
                                        {
                                            debtorOrderInstance._PrCategory = cat._Number;
                                            if (cat._Default)
                                                break;
                                        }
                                    }
                                }

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

        void CreateZeroInvoice(ProjectTransLocalClientCopy selectedItem)
        {
            var project = (ProjectClient)projCache.Get(selectedItem.Project);
            var workSpaceCache = api.GetCache(typeof(Uniconta.DataModel.PrWorkSpace));
            var prWorkspace = (PrWorkSpaceClient)workSpaceCache.Get(selectedItem.Workspace);
            var cwCreateZeroInvoice = new UnicontaClient.Pages.CwCreateZeroInvoice(api, project, prWorkspace);
            cwCreateZeroInvoice.DialogTableId = 2000000067;

            cwCreateZeroInvoice.Closed += delegate
            {
                if (cwCreateZeroInvoice.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    if (cwCreateZeroInvoice.Simulate || !cwCreateZeroInvoice.IsCreateInvoiceProposal)
                        CreateZeroInvoice(project._Number, cwCreateZeroInvoice.InvoiceCategory, cwCreateZeroInvoice.AdjustmentCategory, cwCreateZeroInvoice.Employee, cwCreateZeroInvoice.ProjectTask, cwCreateZeroInvoice.ProjectWorkspace,
                            cwCreateZeroInvoice.InvoiceDate, cwCreateZeroInvoice.ToDate, cwCreateZeroInvoice.Simulate);
                    else
                        CreateZeroInvoiceOrder(project._Number, cwCreateZeroInvoice.InvoiceCategory, cwCreateZeroInvoice.InvoiceDate, cwCreateZeroInvoice.ToDate,
                            cwCreateZeroInvoice.AdjustmentCategory, cwCreateZeroInvoice.Employee, cwCreateZeroInvoice.ProjectTask, cwCreateZeroInvoice.ProjectWorkspace);
                }
            };
            cwCreateZeroInvoice.Show();
        }

        private async void CreateZeroInvoice(string projectNumber, string invoiceCategory, string invoiceAdjustmentCategory, string employee, string Task, string WorkSpace, DateTime invoiceDate, DateTime toDate, bool isSimulate)
        {
            var result = await new Uniconta.API.Project.InvoiceAPI(api).CreateZeroInvoice(projectNumber, invoiceCategory, invoiceAdjustmentCategory, employee, Task, WorkSpace, invoiceDate, toDate, isSimulate, new GLTransClientTotal());
            busyIndicator.IsBusy = false;

            var ledgerRes = result.ledgerRes;
            if (ledgerRes == null)
                return;
            if (result.Err != ErrorCodes.Succes)
                Utility.ShowJournalError(ledgerRes, dgWorkInProgressRpt);
            else if (isSimulate && ledgerRes.SimulatedTrans != null)
                AddDockItem(TabControls.SimulatedTransactions, ledgerRes.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
            else
            {
                var msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), ledgerRes.JournalPostedlId);
                UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
            }
        }

        private async void CreateZeroInvoiceOrder(string Project, string InvoiceCategory, DateTime InvoiceDate, DateTime ToDate, string AdjustmentCategory, string Employee, string Task, string WorkSpace)
        {
            var debtorOrderInstance = api.CompanyEntity.CreateUserType<ProjectInvoiceProposalClient>();
            var result = await new Uniconta.API.Project.InvoiceAPI(api).CreateZeroInvoiceOrder(debtorOrderInstance, Project, InvoiceCategory, InvoiceDate, ToDate, AdjustmentCategory, Employee, Task, WorkSpace);

            busyIndicator.IsBusy = false;
            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
            else
                ShowOrderLines(debtorOrderInstance);
        }

        private void ShowOrderLines(ProjectInvoiceProposalClient order)
        {
            var msg = string.Format(Uniconta.ClientTools.Localization.lookup("CreatedOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceProposal"));
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", msg, Uniconta.ClientTools.Localization.lookup("OrderNumber"), order._OrderNumber,
                Uniconta.ClientTools.Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Lines")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(TabControls.ProjInvoiceProposalLine, order, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("InvoiceProposalLine"), order._OrderNumber, order._DCAccount));
                        break;
                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var local = (sender as Image).Tag as ProjectTransLocalClientCopy;
            if (local != null)
                AddDockItem(TabControls.UserNotesPage, local.ProjectRef, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Notes"), local.ProjectRef._Name));
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.PrCategory), typeof(Uniconta.DataModel.PrWorkSpace), typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.PrType) });
        }
    }

    public class StaticValuesCopy
    {
        public static bool IncludeJournals { get; set; }
    }

    class sortTransCopy : IComparer<ProjectWIPTotals>
    {
        public int Compare(ProjectWIPTotals x, ProjectWIPTotals y)
        {
            return string.Compare(x._Project, y._Project);
        }
    }

    public class ProjectTransLocalClientCopy : ProjectWIPTotals, INotifyPropertyChanged, UnicontaBaseEntity
    {
        public int CompanyId { get { return _CompanyId; } set { _CompanyId = value; } }
        public Type BaseEntityType() { return typeof(ProjectWIPTotals); }

        public DateTime FromDate { get { return _FromDate; } set { _FromDate = value; } }

        Uniconta.DataModel.Project _projectRef;
        Uniconta.DataModel.Project projectRef { get { return _projectRef ?? (_projectRef = (Uniconta.DataModel.Project)ClientHelper.GetRef(_CompanyId, typeof(Uniconta.DataModel.Project), _Project)); } }

        [Display(Name = "Date", ResourceType = typeof(ProjectTransClientText))]
        public DateTime Date { get { return _Date; } set { _Date = value; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Debtor))]
        [Display(Name = "DAccount", ResourceType = typeof(DCTransText))]
        public string Debtor { get { return projectRef?._DCAccount; } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string DebtorName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.Debtor), Debtor); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "RootProject", ResourceType = typeof(ProjectText))]
        public string RootProject { get { return projectRef._RootProject; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(ProjectTransClientText))]
        public string Project { get { return _Project; } }

        [Display(Name = "ProjectName", ResourceType = typeof(ProjectTransClientText))]
        public string ProjectName { get { return projectRef?._Name; } }

        [Display(Name = "UserNote", ResourceType = typeof(UserNotesClientText))]
        public bool UserNote { get { return projectRef.HasNotes; } }

        [Display(Name = "Phase", ResourceType = typeof(ProjectText))]
        public string Phase { get { var proj = projectRef; return proj != null ? AppEnums.ProjectPhase.ToString((int)proj._Phase) : null; } }

        [Display(Name = "WorkSpace", ResourceType = typeof(ProjectTransClientText))]
        public string Workspace { get { return _Workspace; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ProjectTask))]
        [Display(Name = "Task", ResourceType = typeof(ProjectTransClientText))]
        public string Task { get { return _Task; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        [Display(Name = "PersonInCharge", ResourceType = typeof(ProjectTransClientText))]
        public string PersonInCharge { get { return projectRef?._PersonInCharge; } }

        [Display(Name = "PersonInChargeName", ResourceType = typeof(ProjectTransClientText))]
        public string PersonInChargeName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.Employee), PersonInCharge); } }

        [Display(Name = "EmployeeFee", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeFee { get { return _EmployeeFee / 100d; } }

        [Display(Name = "EmployeeFeeCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeFeeCostValue { get { return _EmployeeFeeCostValue / 100d; } }

        [Display(Name = "EmployeeFeeJournal", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeFeeJournal { get { return _EmployeeFeeJournal / 100d; } set { value = _EmployeeFeeJournal; } }

        [Display(Name = "EmployeeFeeJournalCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeFeeJournalCostValue { get { return _EmployeeFeeJournalCostValue / 100d; } set { value = _EmployeeFeeJournalCostValue; } }

        [Display(Name = "EmployeeHoursPosted", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeHoursPosted { get { return _EmployeeHoursPosted / 100d; } }

        [Display(Name = "EmployeeHoursJournal", ResourceType = typeof(ProjectTransClientText))]
        public double EmployeeHoursJournal { get { return _EmployeeHoursJournal / 100d; } set { value = _EmployeeHoursJournal; } }

        [Display(Name = "Expenses", ResourceType = typeof(ProjectTransClientText))]
        public double Expenses { get { return _Expenses / 100d; } }

        [Display(Name = "ExpensesCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double ExpensesCostValue { get { return _ExpensesCostValue / 100d; } }

        [Display(Name = "Revenue", ResourceType = typeof(ProjectTransClientText))]
        public double Revenue { get { return (_EmployeeFee + _Expenses) / 100d; } }

        [Display(Name = "RevenueCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double RevenueCostValue { get { return (_EmployeeFeeCostValue + _ExpensesCostValue) / 100d; } }

        [Display(Name = "OnAccount", ResourceType = typeof(ProjectTransClientText))]
        public double OnAccount { get { return _OnAccount / 100d; } }

        [Display(Name = "Invoiced", ResourceType = typeof(ProjectTransClientText))]
        public double Invoiced { get { return _Invoiced / 100d; } }

        [Display(Name = "InvoicedCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double InvoicedCostValue { get { return _InvoicedCostValue / 100d; } }

        [Display(Name = "TotalInvoiced", ResourceType = typeof(ProjectTransClientText))]
        public double TotalInvoiced { get { return (_OnAccount + _Invoiced) / 100d; } }

        [Display(Name = "TotalInvoicedCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double TotalInvoicedCostValue { get { return InvoicedCostValue; } }

        [Display(Name = "Adjustment", ResourceType = typeof(ProjectTransClientText))]
        public double Adjustment { get { return _Adjustment / 100d; } }

        [Display(Name = "AdjustmentCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double AdjustmentCostValue { get { return _AdjustmentCostValue / 100d; } }

        [Display(Name = "OpeningBalance", ResourceType = typeof(ProjectTransClientText))]
        public double OpeningBalance { get { return _OpeningBalance / 100d; } }

        [Display(Name = "OpeningBalanceCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double OpeningBalanceCostValue { get { return _OpeningBalanceCostValue / 100d; } }

        [Display(Name = "ClosingBalance", ResourceType = typeof(ProjectTransClientText))]
        public double ClosingBalance { get { return (_OpeningBalance + _EmployeeFee + _Expenses + _Adjustment + _OnAccount + _Invoiced + (StaticValuesCopy.IncludeJournals ? _EmployeeFeeJournal : 0)) / 100d; } }

        [Display(Name = "ClosingBalanceCostValue", ResourceType = typeof(ProjectTransClientText))]
        public double ClosingBalanceCostValue { get { return (_OpeningBalanceCostValue + _EmployeeFeeCostValue + _ExpensesCostValue + _AdjustmentCostValue + _InvoicedCostValue + (StaticValuesCopy.IncludeJournals ? _EmployeeFeeJournalCostValue : 0)) / 100d; } }

        [Display(Name = "InvoiceProposal", ResourceType = typeof(ProjectTransClientText))]
        public bool OpenInvoiceProposal { get { return _HasInvoiceProposal; } }

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
        public PrWorkSpaceClient WorkSpaceRef
        {
            get
            {
                return ClientHelper.GetRefClient<PrWorkSpaceClient>(_CompanyId, typeof(Uniconta.DataModel.PrWorkSpace), _Workspace);
            }
        }

        [ReportingAttribute]
        public ProjectTaskClient ProjectTaskRef
        {
            get
            {
                if (_Task == null)
                    return null;
                var proj = ClientHelper.GetRef(_CompanyId, typeof(Uniconta.DataModel.Project), _Project);
                if (proj != null)
                {
                    var projClient = proj as Uniconta.DataModel.Project;
                    if (projClient != null)
                        return projClient.FindTask(_Task);
                    else
                        return ClientHelper.GetServerRef<ProjectTaskClient>(proj, typeof(ProjectTask), _Task);
                }
                return null;
            }
        }

        [ReportingAttribute]
        public CompanyClient CompanyRef { get { return Global.CompanyRef(_CompanyId); } }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
