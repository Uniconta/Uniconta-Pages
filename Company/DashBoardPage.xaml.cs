using System;
using Uniconta.API.Service;
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Collections.Generic;
using Uniconta.ClientTools;
using System.Threading.Tasks;
using System.Linq;

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
            OpenDashboard();
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "OpenDashboard":
                    OpenDashboard();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void OpenDashboard()
        {
            var selectedItem = dgDashboardpage.SelectedItem as DashboardClient;

            if (selectedItem == null)
                return;
            AddDockItem(TabControls.DashBoardViewerPage, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Dashboard"), selectedItem._Name));
        }

        public override Task InitQuery()
        {
            return LoadDashBoardGridSource();
        }

        async Task LoadDashBoardGridSource()
        {
            busyIndicator.IsBusy = true;
            var Arr = await api.Query<DashboardClient>();
            if (Arr.Length != 0)
            {
                var comp = api.CompanyEntity;
                var moduleWiseList = new List<UnicontaBaseEntity>(Arr.Length);
                var moduleWiseGroup = Arr.GroupBy(x => x.Module);

                foreach (var grp in moduleWiseGroup)
                {
                    var moduleIdx = AppEnums.Modules.IndexOf(grp.Key);

                    switch (moduleIdx)
                    {
                        case 0:
                            if (comp.ShowLedger)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 3:
                            if (comp.ShowDebtor)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 4:
                            if (comp.ShowCreditor)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 5:
                            if (comp.ShowProject)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 6:
                            if (comp.ShowTools)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 7:
                            if (comp.ShowCrm)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 8:
                            if (comp.ShowInventory)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 9:
                            if (comp.Shipments)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 10:
                            if (comp.ShowCompany)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 12:
                            if (comp._ProjectPlanning)
                                moduleWiseList.AddRange(grp);
                            break;
                        default:
                            moduleWiseList.AddRange(grp);
                            break;
                    }
                }

                var dashBoardClient = new DashboardClient[moduleWiseList.Count];
                int idx = 0;
                foreach(var item in moduleWiseList)
                {
                    DashboardClient dashBoard = new DashboardClient();
                    StreamingManager.Copy(item, dashBoard);
                    dashBoardClient[idx] = dashBoard;
                    idx++;
                }

                dgDashboardpage.SetSource(dashBoardClient);
            }
            busyIndicator.IsBusy = false;
        }
    }
}
