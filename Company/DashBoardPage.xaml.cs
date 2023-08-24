using UnicontaClient.Models;
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
using Uniconta.Common;
using DevExpress.Data.Filtering;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DashBoardPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DashboardClient); } }
    }

    public partial class DashBoardPage : GridBasePage
    {
        public DashBoardPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgDashboardpage;
            dgDashboardpage.api = api;
            dgDashboardpage.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgDashboardpage);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDashboardpage.RowDoubleClick += DgDashboardpage_RowDoubleClick;
            colUserId.Header = Uniconta.ClientTools.Localization.lookup("UserId");
        }

        private void DgDashboardpage_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("OpenDashboard");
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDashboardpage.SelectedItem as DashboardClient;
            switch (ActionType)
            {
                case "OpenDashboard":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.DashBoardViewerPage, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Dashboard"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
