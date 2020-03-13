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
using System.Windows;
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
    public class ProjectCostCategoryGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectCostCategoryClient); } }
    }
    public partial class CostCategory : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.CostCategory.ToString(); }
        }
        public CostCategory(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCostCategory);
            dgCostCategory.api = api;
            dgCostCategory.BusyIndicator = busyIndicator;

            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CostCategoryPage2)
                dgCostCategory.UpdateItemSource(argument);
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCostCategory.SelectedItem as ProjectCostCategoryClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.CostCategoryPage2, api, Uniconta.ClientTools.Localization.lookup("CostCategory"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.CostCategoryPage2, selectedItem);
                    break;
                case "AddNote":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.UserNotesPage, dgCostCategory.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.UserDocsPage, dgCostCategory.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "InvTrans":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.InventoryTransactions, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
