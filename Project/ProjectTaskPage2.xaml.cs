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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectTaskPage2 : FormBasePage
    {
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }

        public override string NameOfControl { get { return TabControls.ProjectTaskPage2.ToString(); } }
        ProjectTaskClientLocal editrow;
        public override Type TableType { get { return typeof(ProjectTaskClientLocal); } }

        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ProjectTaskClientLocal)value; } }

        SQLCache ProjectCache;
        public ProjectTaskPage2(UnicontaBaseEntity sourcedata, bool isEdit, UnicontaBaseEntity master = null)
             : base(sourcedata, isEdit)
        {
            InitPage(api, master);
        }
        public ProjectTaskPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitPage(crudApi, null);
        }

        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master)
        {
            InitializeComponent();
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                if (editrow == null)
                {
                    editrow = CreateNew() as ProjectTaskClientLocal;
                    editrow.SetMaster(master != null ? master : api.CompanyEntity);
                }
                frmRibbon.DisableButtons("Delete");
            }
            layoutItems.DataContext = editrow;
            leEmployee.api= leProject.api = leGroup.api= lePayrollCategory.api = leWorkSpace.api = leFollows.api= crudapi;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            if (master is Uniconta.DataModel.Project)
                liProject.Visibility = Visibility.Collapsed;
            else if (master is Uniconta.DataModel.Employee)
                liEmployee.Visibility = Visibility.Collapsed;
            ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project));
            if (editrow?._Project != null)
            {
                var project = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Project))?.Get(editrow._Project) as ProjectClient;
                setTask(project);
            }
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }
        private void leFollows_GotFocus(object sender, RoutedEventArgs e)
        {
            if (editrow?._Project != null)
            {
                var selected = (ProjectClient)ProjectCache.Get(editrow._Project);
                setTask(selected);
            }
        }

        async void setTask(ProjectClient project)
        {
            if (project != null)
                editrow.taskSource = project.Tasks ?? await project.LoadTasks(api);
            else
                editrow.taskSource = api.GetCache(typeof(Uniconta.DataModel.ProjectTask));
            editrow.NotifyPropertyChanged("TaskSource");
            leFollows.ItemsSource = editrow.TaskSource;
        }

        private void leProject_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var selectedItem = leProject.SelectedItem as ProjectClient;
            setTask(selectedItem);
        }
    }
}
