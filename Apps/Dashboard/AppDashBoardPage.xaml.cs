using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Reports;
using Uniconta.Reports.Utilities;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppDashBoardGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppDashboardClient); } }
    }

    public partial class AppDashBoardPage : GridBasePage
    {
        public AppDashBoardPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgDashboardpage;
            dgDashboardpage.api = api;
            dgDashboardpage.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgDashboardpage);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDashboardpage.RowDoubleClick += DgDashboardpage_RowDoubleClick;
        }

        private void DgDashboardpage_RowDoubleClick()
        {
            OpenDashboard();
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDashboardpage.SelectedItem as AppDashboardClient;
            switch (ActionType)
            {
                case "OpenDashboard":
                    OpenDashboard();
                    break;
                case "RefreshGrid":
                    LoadDashBoardGridSource();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
       
        void OpenDashboard()
        {
            var selectedItem = dgDashboardpage.SelectedItem as AppDashboardClient;

            if (selectedItem == null)
                return;
#if MAC
            var hdr = string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", selectedItem._Name);
            var webDashboardUrl = BasePage.session.Connection.Target == APITarget.Live ? $"https://web.uniconta.com/DashBoards/DashBoardViewer?DashBoardsId={selectedItem._Name}" : 
            $"https://test.uniconta.com:8080/DashBoards/DashBoardViewer?DashBoardsId={selectedItem._Name}";
            AddDockItem(TabControls.ViewUrlPage, new object[] { api.CompanyEntity, webDashboardUrl }, hdr);
#else
            AddDockItem(TabControls.AppDashBoardViewerPage, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Dashboard"), selectedItem._Name));
#endif
        }

        public override Task InitQuery()
        {
            return LoadDashBoardGridSource();
        }

        async Task LoadDashBoardGridSource()
        {
            busyIndicator.IsBusy = true;
            var Arr = await api.Query<AppDashboardClient>(Utility.GetCurrentApp());
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
                            if (comp.ShowLedger && comp.Ledger)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 2:
                            if (comp.FixedAsset)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 3:
                            if (comp.ShowDebtor && comp.Debtor)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 4:
                            if (comp.ShowCreditor && comp.Creditor)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 5:
                            if (comp.ShowProject && comp.Project)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 6:
                            if (comp.ShowTools)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 7:
                            if (comp.ShowCrm && comp.CRM)
                                moduleWiseList.AddRange(grp);
                            break;
                        case 8:
                            if (comp.ShowInventory && comp.Inventory)
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

                var dashBoardClient = new AppDashboardClient[moduleWiseList.Count];
                int idx = 0;
                foreach (var item in moduleWiseList)
                {
                    AppDashboardClient dashBoard = new AppDashboardClient();
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
