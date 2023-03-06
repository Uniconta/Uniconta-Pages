using UnicontaClient.Pages;
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
    public class WorkInstallationGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(WorkInstallationGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class WorkInstallationGroupPage : GridBasePage
    {
        public WorkInstallationGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgWorkInstallationGroup;
            dgWorkInstallationGroup.api = api;
            dgWorkInstallationGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgWorkInstallationGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgWorkInstallationGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgWorkInstallationGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgWorkInstallationGroup.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
