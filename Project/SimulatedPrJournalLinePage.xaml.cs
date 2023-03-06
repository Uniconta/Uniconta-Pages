using System;
using System.Collections;
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
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SimulatedPrJournalLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectJournalLineClient); } }
        public override IComparer GridSorting { get { return new ProjectJournalLineSort(); } }
    }

    public partial class SimulatedPrJournalLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.SimulatedPrJournalLinePage; } }

        SQLCache ProjectCache;
        public SimulatedPrJournalLinePage(UnicontaBaseEntity[] simulatedTransactions)
            : base(simulatedTransactions, 0, 0)
        {
            Initialize(simulatedTransactions);
        }

        private void Initialize(UnicontaBaseEntity[] simulatedTransactions)
        {
            InitializeComponent();
            dgSimulatedPrjJrnllLinePageGrid.api = api;
            if (simulatedTransactions != null && simulatedTransactions.Length > 0)
            {
                var lst = new ProjectJournalLineClient[simulatedTransactions.Length];
                int i = 0;
                foreach (var t in (IEnumerable<ProjectJournalLineClient>)simulatedTransactions)
                    lst[i++] = t;
                dgSimulatedPrjJrnllLinePageGrid.ClearSorting();
                dgSimulatedPrjJrnllLinePageGrid.SetSource(lst);
            }
            SetRibbonControl(localMenu, dgSimulatedPrjJrnllLinePageGrid);
            dgSimulatedPrjJrnllLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += gridRibbon_BaseActions;
        }

        public override Task InitQuery() { return null; }

        //async void setTask(Uniconta.DataModel.Project project, SimulatedProjectJournalLineLocal rec)
        //{
        //    if (api.CompanyEntity.ProjectTask)
        //    {
        //        if (project != null)
        //            rec.taskSource = project.Tasks ?? await project.LoadTasks(api);
        //        else
        //        {
        //            rec.taskSource = null;
        //            rec.Task = null;
        //        }
        //        rec.NotifyPropertyChanged("TaskSource");
        //    }
        //}

        //private void Task_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    var selectedItem = dgSimulatedPrjJrnllLinePageGrid.SelectedItem as SimulatedProjectJournalLineLocal;
        //    if (selectedItem?._Project != null)
        //    {
        //        var selected = (Uniconta.DataModel.Project)ProjectCache.Get(selectedItem._Project);
        //        setTask(selected, selectedItem);
        //    }
        //}

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api).ConfigureAwait(false);
        }
    }

    //public class SimulatedProjectJournalLineLocal : ProjectJournalLineClient
    //{
    //    internal object taskSource;
    //    public object TaskSource { get { return taskSource; } }
    //}
}
