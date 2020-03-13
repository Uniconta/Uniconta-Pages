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
    public class ProjectGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectGroupClient); } }
    }
    public partial class ProjectGroups : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.ProjectGroups.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public ProjectGroups(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public ProjectGroups(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectGroupGrid);
            dgProjectGroupGrid.api = api;
            dgProjectGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }     

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectGroupGrid.SelectedItem as ProjectGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.ProjectGroupPage2, api, Uniconta.ClientTools.Localization.lookup("ProjectGroup"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.ProjectGroupPage2, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ProjectGroupPage2)
                dgProjectGroupGrid.UpdateItemSource(argument);
        }
    }
}

