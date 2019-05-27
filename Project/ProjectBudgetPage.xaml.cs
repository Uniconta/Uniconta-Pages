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
                    if(UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), selectedItem._Name), Uniconta.ClientTools.Localization.lookup("Confirmation"),
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        async void SaveAndOpenLines(ProjectBudgetClient selectedItem)
        {
            if (dgProjectBudgetGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.ProjectBudgetLinePage, selectedItem, string.Format("{0} {1}: {2}", Uniconta.ClientTools.Localization.lookup("Budget"), Uniconta.ClientTools.Localization.lookup("Lines"), selectedItem._Project));
        }
    }
}


