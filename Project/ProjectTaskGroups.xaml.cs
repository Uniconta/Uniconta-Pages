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
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PrTaskGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PrTaskGroupClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProjectTaskGroups : GridBasePage
    {
        protected override bool IsLayoutSaveRequired() { return false; }
        public ProjectTaskGroups(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgPrTaskPage);
            dgPrTaskPage.api = api;
            dgPrTaskPage.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgPrTaskPage.SelectedItem as PrTaskGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgPrTaskPage.AddRow();
                    break;
                case "DeleteRow":
                    dgPrTaskPage.DeleteRow();
                    break;
                case "SaveGrid":
                    dgPrTaskPage.SaveData();
                    break;
                case "Debtors":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorProjTaskGroup, dgPrTaskPage.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TaskGroups"), selectedItem._Name));
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
