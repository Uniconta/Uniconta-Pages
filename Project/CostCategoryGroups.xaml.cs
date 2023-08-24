using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
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
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectCostCategoryGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectCostCategoryGroupClient); } }
    }
    public partial class CostCategoryGroups : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CostCategoryGroups.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public CostCategoryGroups(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public CostCategoryGroups(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectCostCategoryGroupGrid);
            dgProjectCostCategoryGroupGrid.api = api;
            dgProjectCostCategoryGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }   

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectCostCategoryGroupGrid.SelectedItem as ProjectCostCategoryGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.CostCategoryGroupPage2, api, Uniconta.ClientTools.Localization.lookup("CategoryGroups"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.CostCategoryGroupPage2, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CostCategoryGroupPage2)
                dgProjectCostCategoryGroupGrid.UpdateItemSource(argument);
        }
    }
}
