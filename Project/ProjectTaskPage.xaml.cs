using UnicontaClient.Models;
using DevExpress.Xpf.Grid;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls.GantChart;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProjectTaskPage : ControlBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectTaskPage; } }
        CrudAPI crudApi;
        UnicontaBaseEntity master;
        public ProjectTaskPage(UnicontaBaseEntity _master)
            : base(null)
        {
            InitializeComponent();
            ribbonControl = localMenu;
            crudApi = api;
            master = _master;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            SetProjectTask();
        }
    
        async void SetProjectTask()
        {
            busyIndicator.IsBusy = true;
            var ProjTaskList = await crudApi.Query<ProjectTaskClient>(new [] { master }, null);
            var dt = this.DataContext as ProjectTaskVM;
            dt.Master = master;
            var proj = (master as Uniconta.DataModel.Project);
            if (proj != null)
            {
                dt.Project = proj._Number;
                proj.Tasks = ProjTaskList;
            }
            dt.BindGantChart(ProjTaskList);
            busyIndicator.IsBusy = false;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var dt = this.DataContext as ProjectTaskVM;
            switch (ActionType)
            {
                case "SaveGrid":
                    SaveData(dt);
                    break;
                case "Delete":
                    var selectedrow = treeListView.FocusedNode;
                    if(selectedrow != null)
                     dt.DeleteRow(selectedrow);
                    break; 
            }
        }

        async void SaveData(ProjectTaskVM dt)
        {
            busyIndicator.IsBusy = true;
            var result = await dt.SaveData(crudApi);
            busyIndicator.IsBusy = false;
            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
            else
                dockCtrl.CloseDockItem();
        }

        private void treeListView_ShowingEditor(object sender, DevExpress.Xpf.Grid.TreeList.TreeListShowingEditorEventArgs e)
        {
            if (e.Column.FieldName == "Task" || e.Column.FieldName == "Name" || e.Column.FieldName == "Start" || e.Column.FieldName == "End")
            {
                e.Cancel = ((GantData)e.Node.Content)?.State != ProjectTaskStatus.Default ? false : true;
            }
        }

        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            var dt = this.DataContext as ProjectTaskVM;
            dt.OnDateChange();
        }

        private void treeListView_CellValueChanging(object sender, DevExpress.Xpf.Grid.TreeList.TreeListCellValueChangedEventArgs e)
        {
            ((TreeListView)sender)?.PostEditor();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedrow = treeListView.FocusedNode;
            if(selectedrow != null)
            {
                var dt = this.DataContext as ProjectTaskVM;
                dt.DeleteRow(selectedrow);
            }
        }
    }
}
