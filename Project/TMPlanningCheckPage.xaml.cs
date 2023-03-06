using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.Common;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;
using Uniconta.DataModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Collections;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TMPlanningCheckGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TMPlanningCheckLocal); } }
        public override bool Readonly { get { return true; } }
    }
        
    public partial class TMPlanningCheckPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.TMPlanningCheckPage; } }

        static string budgetGroup;
        static string workSpace;

        SQLTableCache<Uniconta.DataModel.Debtor> debtors;
        SQLTableCache<Uniconta.DataModel.Project> projects;
        SQLCache budgetGrpCache;
        SQLTableCache<Uniconta.DataModel.PrWorkSpace> prWorkSpaceCache;

        public TMPlanningCheckPage(BaseAPI Api) :
            base(Api, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgTMPlanningCheckGrid);
            dgTMPlanningCheckGrid.api = api;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
           
            dgTMPlanningCheckGrid.BusyIndicator = busyIndicator;
           

            projects = api.GetCache<Uniconta.DataModel.Project>();
            debtors = api.GetCache<Uniconta.DataModel.Debtor>();
            budgetGrpCache = api.GetCache(typeof(Uniconta.DataModel.ProjectBudgetGroup));
            prWorkSpaceCache = api.GetCache<Uniconta.DataModel.PrWorkSpace>();

            cmbBudgetGroup.api = cmbWorkSpace.api = api;
            SetBudgetGroup();
            cmbBudgetGroup.Text = budgetGroup;
            cmbWorkSpace.ItemsSource = prWorkSpaceCache;
            cmbWorkSpace.Text = workSpace;

            if (!api.CompanyEntity.ProjectTask)
            {
                cmbWorkSpace.Visibility = Visibility.Collapsed;
                lblWorkSpace.Visibility = Visibility.Collapsed;
            }
        }

        public override Task InitQuery() { return null; }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTMPlanningCheckGrid.SelectedItem as TMPlanningCheckLocal;
            switch (ActionType)
            {
                case "Search":
                    LoadGrid();
                    break;

                case "Create":
                    if (selectedItem != null)
                    {
                        var debtor = debtors.Get(selectedItem._Debtor);
                        if (debtor != null)
                            AddDockItem(TabControls.Project, debtor, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Project"), debtor._Name));
                    }
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void LoadGrid()
        {
            budgetGroup = cmbBudgetGroup.Text;
            workSpace = cmbWorkSpace.Text;

            if (string.IsNullOrEmpty(budgetGroup))
            {
                var msgText = string.Concat(Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"), " (", Uniconta.ClientTools.Localization.lookup("Field"), ": ", Uniconta.ClientTools.Localization.lookup("BudgetGroup"), ")");
                UnicontaMessageBox.Show(msgText, Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                return;
            }

            busyIndicator.IsBusy = true;

            if (projects == null)
                projects = await api.LoadCache<Uniconta.DataModel.Project>();

            if (debtors == null)
                debtors = await api.LoadCache<Uniconta.DataModel.Debtor>();

            var projArr = projects.ToArray();

            var lst = new List<TMPlanningCheckLocal>(1000);

            var search = new Uniconta.DataModel.Project();
            var sort = new SortProjectDebtor();
            int pos;
            Array.Sort(projArr, sort);

            foreach (var rec in debtors)
            {
                if (rec._Blocked)
                    continue;

                search._DCAccount = rec._Account;
                pos = Array.BinarySearch(projArr, search, sort);
                if (pos < 0)
                    pos = ~pos;

                bool found = false;
                while (pos < projArr.Length)
                {
                    var s = projArr[pos++];
                    if (s._DCAccount != rec._Account)
                        break;

                    if (s._Blocked || s._Phase == ProjectPhase.Completed || s._Phase == ProjectPhase.ReportedAsFinished || s._Phase == ProjectPhase.Paused)
                        continue;

                    found = true;
                    break;
                }

                if (!found)
                {
                    var cur = new TMPlanningCheckLocal() { _CompanyId = api.CompanyId, _Debtor = rec._Account, _ErrorInfo = string.Concat("1. ", Uniconta.ClientTools.Localization.lookup("DebtorNoActiveProjects")) };
                    lst.Add(cur);
                }
            }

            List<PropValuePair> pairBudget = new List<PropValuePair>();
            pairBudget.Add(PropValuePair.GenereteWhereElements(nameof(ProjectBudget._Group), typeof(string), budgetGroup));
            var projBudgetArr = await api.Query<ProjectBudget>(pairBudget);

            var master = budgetGrpCache.Get(budgetGroup) as ProjectBudgetGroup;
            var budgetLineArr = await api.Query<ProjectBudgetLine>(master);

            List<PropValuePair> pairTask = new List<PropValuePair>();
            pairTask.Add(PropValuePair.GenereteWhereElements(nameof(ProjectTaskClient.WorkSpace), typeof(string), workSpace));

            var projTaskArr = await api.Query<ProjectTaskClient>(pairTask);

            var searchBud = new ProjectBudget();
            var sortBud = new SortProjectBudget();
            Array.Sort(projBudgetArr, sortBud);

            var searchBudLine = new ProjectBudgetLine();
            var sortBudLine = new SortProjectBudgetLine();
            int posBudLine;
            Array.Sort(budgetLineArr, sortBudLine);

            var searchTask = new ProjectTask();
            var sortTask = new SortProjectTask();
            int posTask;
            Array.Sort(projTaskArr, sortTask);

            foreach (var rec in projects)
            {
                if (rec._Blocked || rec._Phase == ProjectPhase.Completed || rec._Phase == ProjectPhase.ReportedAsFinished || rec._Phase == ProjectPhase.Paused)
                    continue;

                searchBud._Project = rec._Number;
                pos = Array.BinarySearch(projBudgetArr, searchBud, sortBud);
                if (pos < 0)
                    pos = ~pos;

                bool found = false;
                while (pos < projBudgetArr.Length)
                {
                    var s = projBudgetArr[pos++];
                    if (s._Project != rec._Number)
                        break;

                    #region BudgetLine
                    searchBudLine._Project = rec._Number;
                    posBudLine = Array.BinarySearch(budgetLineArr, searchBudLine, sortBudLine);
                    if (posBudLine < 0)
                        posBudLine = ~posBudLine;

                    bool foundLine = false;
                    while (posBudLine < budgetLineArr.Length)
                    {
                        var sLine = budgetLineArr[posBudLine++];
                        if (sLine._Project != rec._Number)
                            break;

                        foundLine = true;
                        break;
                    }

                    if (!foundLine)
                    {
                        var cur = new TMPlanningCheckLocal() { _CompanyId = api.CompanyId, _Project = rec._Number, _ErrorInfo = string.Concat("2. ", Uniconta.ClientTools.Localization.lookup("ProjectMissingBudget")) };
                        lst.Add(cur);
                    }
                    #endregion

                    found = true;
                }

                if (api.CompanyEntity.ProjectTask && workSpace != null)
                {
                    searchTask._Project = rec._Number;
                    posTask = Array.BinarySearch(projTaskArr, searchTask, sortTask);
                    if (posTask < 0)
                        posTask = ~posTask;

                    bool foundLine = false;
                    while (posTask < projTaskArr.Length)
                    {
                        var projTask = projTaskArr[posTask++];
                        if (projTask._Project != rec._Number)
                            break;

                        foundLine = true;
                        break;
                    }

                    if (!foundLine)
                    {
                        var cur = new TMPlanningCheckLocal() { _CompanyId = api.CompanyId, _Project = rec._Number, _ErrorInfo = string.Concat("3. ", Uniconta.ClientTools.Localization.lookup("ProjectNoActiveTasks")) };
                        lst.Add(cur);
                    }
                }
            }
            
            dgTMPlanningCheckGrid.ItemsSource = lst;

            if (dgTMPlanningCheckGrid.tableView != null)
            {
                dgTMPlanningCheckGrid.ShowTotalSummary();
                dgTMPlanningCheckGrid.GroupBy("ErrorInfo");
                dgTMPlanningCheckGrid.Visibility = Visibility.Visible;
            }

            busyIndicator.IsBusy = false;
        }

        private async void SetBudgetGroup()
        {
            var budgetGroup = await api.Query<ProjectBudgetGroup>();
            cmbBudgetGroup.ItemsSource = budgetGroup;
        }

        protected override async Task LoadCacheInBackGroundAsync()
        {
            projects = projects ?? await api.LoadCache<Uniconta.DataModel.Project>().ConfigureAwait(false);
            debtors = debtors ?? await api.LoadCache<Uniconta.DataModel.Debtor>().ConfigureAwait(false);
            budgetGrpCache = budgetGrpCache ?? await api.LoadCache(typeof(Uniconta.DataModel.ProjectBudgetGroup)).ConfigureAwait(false);
            prWorkSpaceCache = prWorkSpaceCache ?? await api.LoadCache<Uniconta.DataModel.PrWorkSpace>().ConfigureAwait(false);
        }
    }

    #region ICompare class
    class SortProjectDebtor : IComparer<Uniconta.DataModel.Project>
    {
        public int Compare(Uniconta.DataModel.Project x, Uniconta.DataModel.Project y)
        {
            var c = string.Compare(x._DCAccount, y._DCAccount);
            if (c != 0)
                return c;
            return x.RowId - y.RowId;
        }
    }

    class SortProjectBudget : IComparer<Uniconta.DataModel.ProjectBudget>
    {
        public int Compare(Uniconta.DataModel.ProjectBudget x, Uniconta.DataModel.ProjectBudget y)
        {
            var c = string.Compare(x._Project, y._Project);
            if (c != 0)
                return c;
            return x.RowId - y.RowId;
        }
    }

    class SortProjectBudgetLine : IComparer<Uniconta.DataModel.ProjectBudgetLine>
    {
        public int Compare(Uniconta.DataModel.ProjectBudgetLine x, Uniconta.DataModel.ProjectBudgetLine y)
        {
            var c = string.Compare(x._Project, y._Project);
            if (c != 0)
                return c;
            return x.RowId - y.RowId;
        }
    }

    class SortProjectTask : IComparer<Uniconta.DataModel.ProjectTask>
    {
        public int Compare(Uniconta.DataModel.ProjectTask x, Uniconta.DataModel.ProjectTask y)
        {
            var c = string.Compare(x._Project, y._Project);
            if (c != 0)
                return c;
            return x.RowId - y.RowId;
        }
    }

    #endregion

    public class TMPlanningCheckLocal : UnicontaBaseEntity
    {
        public int _CompanyId;

        public string _Debtor;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Debtor))]
        [Display(Name = "DAccount", ResourceType = typeof(DCTransText))]
        public string Debtor { get { return _Debtor; } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string DebtorName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.Debtor), Debtor); } }

        public string _Project;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(ProjectTransClientText))]
        public string Project { get { return _Project; } }

        [Display(Name = "ProjectName", ResourceType = typeof(ProjectTransClientText))]
        public string ProjectName { get { return ClientHelper.GetName(_CompanyId, typeof(Uniconta.DataModel.Project), Project); } }

        public string _ErrorInfo;
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
        public int Version(int ClientAPIVersion){ return 1; }
        public int ClassId() { return 2187; }
    }
}
