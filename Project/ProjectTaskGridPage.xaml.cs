using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Windows;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class ProjectTaskClientLocal : ProjectTaskClient
    {
        internal object taskSource;
        public object TaskSource { get { return taskSource; } }
    }

    public class ProjectTaskGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTaskClientLocal); } }
        public override bool Readonly { get { return false; } }
    }
    
    public partial class ProjectTaskGridPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectTaskGridPage; } }
        SQLCache ProjectCache;
        public ProjectTaskGridPage(UnicontaBaseEntity _master) : base(null)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectTaskGrid);
            dgProjectTaskGrid.api = api;
            dgProjectTaskGrid.BusyIndicator = busyIndicator;
            dgProjectTaskGrid.UpdateMaster(_master);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectTaskGrid.AddRow();
                    break;
                case "DeleteRow":
                    dgProjectTaskGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void Save()
        {
            SetBusy();
            dgProjectTaskGrid.BusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");
            var err = await dgProjectTaskGrid.SaveData();
            ClearBusy();
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            if (api.CompanyEntity.ProjectTask)
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
        }

        private void FollowsTask_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgProjectTaskGrid.SelectedItem as ProjectTaskClientLocal;
            if (selectedItem?._Project != null)
            {
                var selected = (ProjectClient)ProjectCache.Get(selectedItem._Project);
                setTask(selected, selectedItem);
            }
        }

        async void setTask(ProjectClient project, ProjectTaskClientLocal rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                    rec.taskSource = project.Tasks ?? await project.LoadTasks(api);
                else
                {
                    rec.taskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("TaskSource");
            }
        }

    }
}
