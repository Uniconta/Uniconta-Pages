using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.Client.Pages;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.Threading.Tasks;
using Uniconta.Common.Utility;
using System.Collections;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectTransactionGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTransClient); } }
        public IList ToListLocal(ProjectTransClient[] Arr) { return this.ToList(Arr); }

        public override bool SingleBufferUpdate { get { return false; } } // we need two buffers to update project number in trans
    }
    public partial class ProjectTransactionPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectTransactionPage; } }

        ItemBase iIncludeSubProBase, iIncludeTimeJournalBase, iViewOnlyOpenBase;
        static bool includeSubProject, InclTimeJournals, viewOnlyOpen;
        UnicontaBaseEntity master;
        string projLst = null;
        List<ProjectTransClient> timetransLst;
        private string parmWorkspace;

        bool timeTransFound;

        private const string AND_OPERATOR = "And";
        private const string FILTERVALUE_ISTIMEJOURNAL = @"[IsTimeJournal] = false";

        SQLTableCache<Uniconta.DataModel.Employee> Employees;
        SQLTableCache<Uniconta.DataModel.Project> Projects;
        SQLCache Payrolls;

        Uniconta.API.Project.FindPricesEmpl priceLookup;

        protected override Filter[] DefaultFilters()
        {
            if (dgProjectTransaction.masterRecords != null)
                return null;
            Filter dateFilter = new Filter
            {
                name = "Date",
                value = String.Format("{0:d}..", BasePage.GetSystemDefaultDate().AddYears(-1).Date)
            };
            Filter projectFilter = null;
            if (api.CompanyEntity.HideInternalProjects)
            {
                projectFilter = new Filter
                {
                    name = "HideInternalProjects",
                    value = "1"
                };
            }

            if (projectFilter != null)
                return new Filter[] { dateFilter, projectFilter };
            else
                return new Filter[] { dateFilter };
        }

        public ProjectTransactionPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializePage(null);
        }

        public ProjectTransactionPage(UnicontaBaseEntity master) : base(master)
        {
            InitializePage(master);
        }

        static UnicontaBaseEntity getMaster(UnicontaBaseEntity master)
        {
            var proj = master as Uniconta.DataModel.Project;
            if (proj != null)
                return proj;
            var projectPosted = master as ProjectJournalPostedClient;
            if (projectPosted != null)
                return projectPosted;
            var WIPreport = master as ProjectWIPTotalsClient;
            if (WIPreport != null)
                return WIPreport.ProjectRef;
            else
                return master as ProjectBudgetLine;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var parm in Parameters)
            {
                if (string.Compare(parm.Name, "Workspace", StringComparison.OrdinalIgnoreCase) == 0 && string.Compare(parm.Value, "True", StringComparison.OrdinalIgnoreCase) == 0)
                    parmWorkspace = parmWorkspace ?? "null";
            }
            base.SetParameter(Parameters);
        }

        public ProjectTransactionPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            UnicontaBaseEntity argsProj = getMaster(syncEntity.Row);
            InitializePage(argsProj);
            SetWorkspace(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            projLst = null;
            ClearTimeTrans();
            UnicontaBaseEntity argsProj = getMaster(args);
            master = argsProj;
            dgProjectTransaction.UpdateMaster(argsProj);
            SetWorkspace(args);
            SetHeader();
            InitQuery();
        }

        private void SetWorkspace(UnicontaBaseEntity master)
        {
            var WIPreport = master as ProjectWIPTotalsClient;
            if (WIPreport != null)
                parmWorkspace = WIPreport.Workspace;
        }
        private void SetHeader()
        {
            string header;
            var syncMaster = master as Uniconta.DataModel.Project;
            if (syncMaster != null)
                header = string.Concat(Uniconta.ClientTools.Localization.lookup("Transactions"), ": ", syncMaster._Number);
            else
            {
                var syncMaster2 = master as Uniconta.DataModel.PrJournalPosted;
                if (syncMaster2 != null)
                    header = string.Concat(Uniconta.ClientTools.Localization.lookup("PrTransaction"), ": ", syncMaster2.RowId);
                else
                {
                    var syncMaster3 = master as Uniconta.DataModel.ProjectBudgetLine;
                    if (syncMaster3 != null)
                        header = string.Concat(Uniconta.ClientTools.Localization.lookup("PrTransaction"), ": ", syncMaster3.RowId);
                    else
                        return;
                }
            }

            if (parmWorkspace != null)
                header += string.Format(" ({0}: {1})", Uniconta.ClientTools.Localization.lookup("Workspace"), parmWorkspace);

            SetHeader(header);
        }

        void InitializePage(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            this.DataContext = this;
            master = _master;
            SetRibbonControl(localMenu, dgProjectTransaction);
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (master != null)
            {
                dgProjectTransaction.UpdateMaster(master);
                ribbonControl.DisableButtons("Save");
            }
            else
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "EditAll", "Save" });

            if (master is Uniconta.DataModel.Project)
            {
                iIncludeSubProBase = UtilDisplay.GetMenuCommandByName(rb, "InclSubProjects");
                iIncludeSubProBase.IsChecked = includeSubProject;
            }
            else
                UtilDisplay.RemoveMenuCommand(rb, "InclSubProjects");

            if (api.CompanyEntity.TimeManagement)
            {
                iIncludeTimeJournalBase = UtilDisplay.GetMenuCommandByName(rb, "InclTimeJournals");
                iIncludeTimeJournalBase.IsChecked = InclTimeJournals;
            }
            else
            {
                InclTimeJournals = false;
                UtilDisplay.RemoveMenuCommand(rb, "InclTimeJournals");
            }

            iViewOnlyOpenBase = UtilDisplay.GetMenuCommandByName(rb, "ViewOnlyOpen");
            iViewOnlyOpenBase.IsChecked = viewOnlyOpen;

            dgProjectTransaction.api = api;
            dgProjectTransaction.BusyIndicator = busyIndicator;
            dgProjectTransaction.ShowTotalSummary();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            localMenu.OnChecked += LocalMenu_OnChecked;

            Employees = api.GetCache<Uniconta.DataModel.Employee>();
            Projects = api.GetCache<Uniconta.DataModel.Project>();
            Payrolls = api.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory));
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (!includeSubProject)
            {
                var master = dgProjectTransaction.masterRecords?.First();
                this.ProjectCol.Visible = !(master is Uniconta.DataModel.Project);
            }
            var Comp = api.CompanyEntity;
            if (!Comp.ProjectTask)
                this.Task.Visible = this.Task.ShowInColumnChooser = false;
            this.IsTimeJournal.Visible = InclTimeJournals;
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            dgProjectTransaction.Readonly = true;
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            if (Comp.HideCostPrice)
            {
                Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
                CostAmount.Visible = CostAmount.ShowInColumnChooser = CostPrice.Visible = CostPrice.ShowInColumnChooser = false;
            }
        }

        private void ClearTimeTrans()
        {
            timetransLst = null;
            timeTransFound = false;
            InclTimeJournals = false;
            iIncludeTimeJournalBase.IsChecked = InclTimeJournals;
        }

        private void SetIncludeFilter()
        {
            if (!api.CompanyEntity.TimeManagement)
                return;

            string filterString = dgProjectTransaction.FilterString ?? string.Empty;

            if (InclTimeJournals)
            {
                filterString = filterString.Replace(FILTERVALUE_ISTIMEJOURNAL, string.Empty).Trim();
                if (filterString != string.Empty && filterString.IndexOf(AND_OPERATOR, 0, 3) != -1)
                    filterString = filterString.Substring(3).Trim();
                else if (filterString != string.Empty && filterString.IndexOf(AND_OPERATOR, filterString.Length - 3) != -1)
                    filterString = filterString.Substring(0, filterString.IndexOf(AND_OPERATOR, filterString.Length - 3)).Trim();
            }
            else
            {
                if (filterString == string.Empty)
                    filterString = FILTERVALUE_ISTIMEJOURNAL;
                else
                    filterString += filterString.IndexOf(FILTERVALUE_ISTIMEJOURNAL) == -1 ? string.Format(" {0} {1}", AND_OPERATOR, FILTERVALUE_ISTIMEJOURNAL) : string.Empty;
            }

            dgProjectTransaction.FilterString = filterString;
        }


        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = false;
            return true;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectTransaction.SelectedItem as ProjectTransClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgProjectTransaction.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgProjectTransaction.syncEntity, vheader);
                    }
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgProjectTransaction.syncEntity, api, busyIndicator);
                    break;
                case "InvTransactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvTransactions, dgProjectTransaction.syncEntity);
                    break;
                case "Save":
                    SaveGrid();
                    break;
                case "RefreshGrid":
                    ClearTimeTrans();
                    if (dgProjectTransaction.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgProjectTransaction);
                    else
                        gridRibbon_BaseActions(ActionType);
                    break;
                case "Filter":
                    if (dgProjectTransaction.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgProjectTransaction);
                    gridRibbon_BaseActions(ActionType);
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "ClearFilter":
                    ClearTimeTrans();
                    gridRibbon_BaseActions(ActionType);
                    break;
                case "ShowMilege":
                    if (selectedItem != null)
                        ShowMileage(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ShowMileage(ProjectTransClient projectTransaction)
        {
            if (projectTransaction._MileageRef != 0)
            {
                var employeRegLine = (await api.Query<EmployeeRegistrationLineClient>(projectTransaction))?.FirstOrDefault();

                if (employeRegLine != null)
                {
                    var registerControl = dockCtrl.AddDockItem(TabControls.RegisterMileage, ParentControl, new object[2] { employeRegLine, true }, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Mileage"), employeRegLine.RowId)) as RegisterMileage;
                    registerControl.ReadOnly = true;
                }
                else
                    UtilDisplay.ShowErrorCode(ErrorCodes.CouldNotFind);
            }
        }

        async Task LoadGrid()
        {
            List<PropValuePair> filter;
            if (includeSubProject)
                filter = new List<PropValuePair>(2) { PropValuePair.GenereteParameter("IncludeSubProject", typeof(string), "1") };
            else
                filter = null;

            if (api.CompanyEntity.HideInternalProjects)
            {
                if (filter == null) filter = new List<PropValuePair>(1);
                filter.Add(PropValuePair.GenereteParameter("HideInternalProjects", typeof(string), "1"));
            }

            if (parmWorkspace != null)
            {
                if (filter == null) filter = new List<PropValuePair>(1);
                filter.Add(PropValuePair.GenereteWhereElements(nameof(ProjectTrans._WorkSpace), typeof(string), parmWorkspace));
                WorkSpace.Visible = true;
            }

            if (viewOnlyOpen)
            {
                if (filter == null) filter = new List<PropValuePair>(1);
                filter.Add(PropValuePair.GenereteWhereElements(nameof(ProjectTrans._Invoiced), typeof(int), 0));
            }

            await dgProjectTransaction.Filter(filter);

            ClearTimeTrans();
            IncludeTimeJournals();
        }

        public async override Task InitQuery()
        {
            if (master is Uniconta.DataModel.Project)
                await ShowInculdeSubProject();
            else
                await base.InitQuery();

            if (InclTimeJournals)
                await IncludeTimeJournals();
        }

        private DateTime AdjustToPreviousSunday(DateTime date)
        {
            if (date == DateTime.MinValue)
                return date;
            int daysBack = (int)date.DayOfWeek;
            return date.AddDays(-daysBack);
        }
        private async Task IncludeTimeJournals()
        {
            if (!InclTimeJournals || timeTransFound)
                return;

            ProjectClient proj = null;
            EmployeeClient emp = null;

            if (master != null)
            {
                emp = master as EmployeeClient;
                if (emp == null)
                    proj = master as ProjectClient;
                if (proj == null)
                {
                    var WIPreport = master as UnicontaClient.Pages.ProjectWIPTotalsClient;
                    if (WIPreport != null)
                        proj = WIPreport.ProjectRef;
                }
            }

            if (timetransLst == null && (master == null || emp != null || proj != null))
            {
                timetransLst = new List<ProjectTransClient>();
                timeTransFound = true;
                busyIndicator.IsBusy = true;

                var pairTM = new List<PropValuePair>();
                UnicontaBaseEntity unicontaBaseEntity = null;

                DateTime? baseDate = emp?._TMApproveDate;
                if (baseDate == null)
                {
                    baseDate = Employees
                        .Where(x => x._TMApproveDate != DateTime.MinValue && x._Terminated == DateTime.MinValue)
                        .Min(x => x._TMApproveDate as DateTime?);
                }
                DateTime minApprovedDate = AdjustToPreviousSunday(baseDate ?? DateTime.MinValue);
                pairTM.Add(PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Date), minApprovedDate, CompareOperator.GreaterThanOrEqual));


                if (emp != null)
                {
                    unicontaBaseEntity = emp;
                }
                else if (proj != null)
                {
                    if (includeSubProject)
                    {
                        var strb = StringBuilderReuse.Create();
                        strb.Append(proj._Number);
                        foreach (var x in Projects)
                        {
                            if (x._MasterProject == proj._Number)
                                strb.Append(';').Append(x._Number);
                        }
                        projLst = strb.ToString();
                        strb.Release();
                        pairTM.Add(PropValuePair.GenereteWhereElements(nameof(TMJournalLineClient.Project), typeof(string), projLst));

                    }
                    else
                        unicontaBaseEntity = proj;
                }
                var tmLines = await api.Query<TMJournalLineClient>(unicontaBaseEntity, pairTM);

                var search = new TMJournalLineClient();
                var sort = new TMJournalEmpDateSort();
                int pos = 0;
                Array.Sort(tmLines, sort);

                string lastEmployee = null;
                string lastPayroll = null;
                string lastProject = null;
                EmpPayrollCategory payrollCat = null;
                Uniconta.DataModel.Project project = null;
                var grpEmpDate = tmLines.GroupBy(x => new { x.Employee, x.Date }).Select(g => new { g.Key.Employee, g.Key.Date, EmployeeTable = Employees.Get(g.Key.Employee) });

                bool isApprovedSunday = true;
                DateTime approvedDateSunday = DateTime.MinValue;
                var approveDate = DateTime.MinValue;
                foreach (var rec in grpEmpDate)
                {
                    if (lastEmployee != rec.Employee)
                    {
                        lastEmployee = rec.Employee;
                        await priceLookup.EmployeeChanged(rec.EmployeeTable);

                        approveDate = rec.EmployeeTable._TMApproveDate;
                        if (approveDate != DateTime.MinValue)
                        {
                            approvedDateSunday = AdjustToPreviousSunday(approveDate);
                            isApprovedSunday = approveDate.DayOfWeek == DayOfWeek.Sunday;
                        }
                    }

                    if (rec.Date < approvedDateSunday)
                        continue;

                    int dayOfStart;
                    if (!isApprovedSunday && approvedDateSunday == rec.Date.AddDays(-1))
                        dayOfStart = (int)approveDate.DayOfWeek;
                    else
                        dayOfStart = 0;

                    search._Employee = rec.Employee;
                    search._Date = rec.Date;
                    pos = Array.BinarySearch(tmLines, search, sort);
                    if (pos < 0)
                        pos = ~pos;
                    while (pos < tmLines.Length)
                    {
                        var s = tmLines[pos++];
                        if (s._Employee != rec.Employee || s._Date != rec.Date)
                            break;

                        if (s._Project == null || s._PayrollCategory == null || s._Employee == null)
                            continue;

                        if (s.Total != 0)
                        {
                            if (lastPayroll != s._PayrollCategory)
                            {
                                payrollCat = (Uniconta.DataModel.EmpPayrollCategory)Payrolls?.Get(s._PayrollCategory);
                                lastPayroll = s._PayrollCategory;
                            }

                            for (int x = dayOfStart + 1; x <= 7; x++)
                            {
                                double dayHours = s.GetHoursDayN(x);
                                if (dayHours == 0)
                                    continue;

                                var line = new ProjectTransClient();
                                line.IsTimeJournal = true;
                                line._Project = s._Project;
                                line._Employee = s._Employee;
                                line._PayrollCategory = s._PayrollCategory;
                                line._PrCategory = payrollCat?._PrCategory;
                                line._WorkSpace = s._WorkSpace;
                                line._Task = s._Task;
                                line._Invoiceable = s._Invoiceable;
                                line._Text = s._Text ?? payrollCat._Name;
                                line._Unit = s._RegistrationType == RegistrationType.Hours ? (byte)ItemUnit.Hours : (byte)ItemUnit.km;
                                line._Date = s._Date.AddDays(x - 1);

                                if (api.CompanyEntity._DimFromProject)
                                {
                                    if (lastProject != s._Project)
                                    {
                                        project = (Uniconta.DataModel.Project)Projects.Get(s._Project);
                                        lastProject = s._Project;
                                    }

                                    line._Dim1 = project._Dim1;
                                    line._Dim2 = project._Dim2;
                                    line._Dim3 = project._Dim3;
                                    line._Dim4 = project._Dim4;
                                    line._Dim5 = project._Dim5;
                                }
                                else
                                {
                                    line._Dim1 = rec.EmployeeTable._Dim1;
                                    line._Dim2 = rec.EmployeeTable._Dim2;
                                    line._Dim3 = rec.EmployeeTable._Dim3;
                                    line._Dim4 = rec.EmployeeTable._Dim4;
                                    line._Dim5 = rec.EmployeeTable._Dim5;
                                }

                                double factor = 1;
                                if (payrollCat != null && payrollCat._Factor != 0 && (payrollCat._InternalType == Uniconta.DataModel.InternalType.OverTime || payrollCat._InternalType == Uniconta.DataModel.InternalType.FlexTime))
                                    factor = payrollCat._Factor;

                                line._Qty = dayHours * factor;

                                await priceLookup.GetEmployeePrice(s);
                                line._CostPrice = s.GetCostPricesDayN(x);
                                line._SalesPrice = s.GetSalesPricesDayN(x);

                                timetransLst.Add(line);
                            }
                        }
                    }
                }

                busyIndicator.IsBusy = false;

            }

            if (timetransLst != null)
            {
                var listType = typeof(List<>).MakeGenericType(dgProjectTransaction.TableTypeUser);
                var transLst = (IList)Activator.CreateInstance(listType, new object[] { dgProjectTransaction.ItemsSource});
                foreach (var item in timetransLst)
                {
                    var userItem = Activator.CreateInstance(dgProjectTransaction.TableTypeUser);
                   
                    StreamingManager.Copy(item, userItem as UnicontaBaseEntity);
                    var tmj = userItem as ProjectTransClient;
                    tmj.IsTimeJournal = true;
                    transLst.Add(userItem);
                }
                dgProjectTransaction.ItemsSource = transLst;
                dgProjectTransaction.Visibility = Visibility.Visible;
            }
        }

        private void LocalMenu_OnChecked(string actionType, bool IsChecked)
        {
            switch (actionType)
            {
                case "InclSubProjects":
                    includeSubProject = IsChecked;
                    ShowInculdeSubProject();
                    break;
                case "InclTimeJournals":
                    InclTimeJournals = IsChecked;
                    this.IsTimeJournal.Visible = InclTimeJournals;
                    if (InclTimeJournals)
                        IncludeTimeJournals();
                    SetIncludeFilter();
                    break;
                case "ViewOnlyOpen":
                    viewOnlyOpen = IsChecked;
                    ViewOnlyOpenTrans();
                    break;
            }
        }

        Task ShowInculdeSubProject()
        {
            if (includeSubProject)
                this.ProjectCol.Visible = true;
            iIncludeSubProBase.IsChecked = includeSubProject;
            return LoadGrid();
        }

        Task ViewOnlyOpenTrans()
        {
            iViewOnlyOpenBase.IsChecked = viewOnlyOpen;
            return LoadGrid();
        }

        async private void JournalPosted(ProjectTransClient selectedItem)
        {
            if (selectedItem._JournalPostedId != 0)
            {
                var result = await api.Query(new GLDailyJournalPostedClient(), new[] { selectedItem }, null);
                if (result != null && result.Length == 1)
                    new CWGLPostedClientFormView(result[0]).Show();
            }
            else
            {
                var result = await api.Query(new ProjectJournalPostedClient(), new[] { selectedItem }, null);
                if (result != null && result.Length == 1)
                    new CWProjPostedClientFormView(result[0]).Show();
            }
        }

        private async void SaveGrid()
        {
            var err = await dgProjectTransaction.SaveData();
            if (err != 0)
                return;

            dgProjectTransaction.Readonly = true;
            dgProjectTransaction.tableView.CloseEditor();
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase != null)
            {
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                ribbonControl.DisableButtons("Save");
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgProjectTransaction.HasUnsavedData;
            }
        }
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;

            if (dgProjectTransaction.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgProjectTransaction.MakeEditable();
                ProjectCol.AllowEditing = DevExpress.Utils.DefaultBoolean.True;
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons("Save");
                editAllChecked = false;
            }
            else
            {
                ProjectCol.AllowEditing = DevExpress.Utils.DefaultBoolean.False;
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                     {
                         if (confirmationDialog.DialogResult == null)
                             return;

                         switch (confirmationDialog.ConfirmationResult)
                         {
                             case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                 var err = await dgProjectTransaction.SaveData();
                                 if (err != 0)
                                 {
                                     api.AllowBackgroundCrud = true;
                                     return;
                                 }
                                 break;
                             case CWConfirmationBox.ConfirmationResultEnum.No:
                                 ClearTimeTrans(); 
                                 dgProjectTransaction.CancelChanges();
                                 break;
                         }
                         editAllChecked = true;
                         dgProjectTransaction.Readonly = true;
                         dgProjectTransaction.tableView.CloseEditor();
                         ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                         ribbonControl.DisableButtons("Save");
                     };
                    confirmationDialog.Show();
                }
                else
                {
                    dgProjectTransaction.Readonly = true;
                    dgProjectTransaction.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons("Save");
                }
            }
        }

        protected override async Task LoadCacheInBackGroundAsync()
        {
            if (Employees == null)
                Employees = await api.LoadCache<Uniconta.DataModel.Employee>().ConfigureAwait(false);
            if (Projects == null)
                Projects = await api.LoadCache<Uniconta.DataModel.Project>().ConfigureAwait(false);
            if (Payrolls == null)
                Payrolls = await api.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory)).ConfigureAwait(false);
            if (this.priceLookup == null)
                priceLookup = new Uniconta.API.Project.FindPricesEmpl(api);
        }
    }
}
