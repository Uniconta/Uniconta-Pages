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
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectBudgetGroupClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool Readonly { get { return false; } }
    }
    public partial class ProjectBudgetGroupPage : GridBasePage
    {
        protected override bool IsLayoutSaveRequired() { return false; }

        public ProjectBudgetGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectBudgetGroup);
            dgProjectBudgetGroup.api = api;
            dgProjectBudgetGroup.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectBudgetGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgProjectBudgetGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgProjectBudgetGroup.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
