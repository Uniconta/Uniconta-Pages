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
            var selectedItem = dgDashboardpage.SelectedItem as DashboardClient;
            switch (ActionType)
            {
                case "OpenDashboard":
                    OpenDashboard();
                    break;
                case "CopyToApp":
                    ConfirmAndCopyToApp(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
      
        async void ConfirmAndCopyToApp(DashboardClient selectedItem)
        {
            var currentApp = Utility.GetCurrentApp();
            if (selectedItem != null && currentApp != null)
            {

                var filter = new List<PropValuePair>() { PropValuePair.GenereteParameter("Name", typeof(string), selectedItem._Name) };
                var appExisting = (await api.Query<AppDashboardClient>(currentApp, filter))?.Where(r => r.Name == selectedItem._Name).FirstOrDefault();
                if (appExisting != null)
                {
                    CWConfirmationBox confirmOverwrite = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("ProceedConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false, true, Uniconta.ClientTools.Localization.lookup("Overwrite"));
                    confirmOverwrite.Closed += async delegate
                    {
                        if (confirmOverwrite.ConfirmationResult == Uniconta.ClientTools.Controls.CWConfirmationBox.ConfirmationResultEnum.Yes)
                        {
                            if (confirmOverwrite.HasConfrimationChecked)
                                CopyToApp(selectedItem, appExisting);
                            else
                                CopyToApp(selectedItem, null);
                        }
                    };
                    confirmOverwrite.Show();
                }
                else
                {
                    if (Utility.ConfirmCopyToApp())
                        CopyToApp(selectedItem, null);
                }

            }
        }
        async void CopyToApp(DashboardClient selectedItem, AppDashboardClient copyTo)
        {
            var currentApp = Utility.GetCurrentApp();
            if (selectedItem != null && currentApp != null)
            {
                try
                {
                    busyIndicator.IsBusy = true;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");
                    await api.Read(selectedItem);
                    AppDashboardClient appDashboard = null;
                    if (copyTo == null)
                    {
                        appDashboard = new AppDashboardClient();
                        appDashboard.SetMaster(currentApp);
                    }
                    else
                        appDashboard = copyTo;

                    appDashboard.SetMaster(selectedItem as Uniconta.DataModel.UserReport);
                    appDashboard._Name = selectedItem._Name;
                    var errCode = copyTo == null ? await api.Insert(appDashboard) : await api.Update(appDashboard);
                    UtilDisplay.ShowErrorCode(errCode);
                    busyIndicator.IsBusy = false;
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK, MessageBoxImage.Error);
                    busyIndicator.IsBusy = false;
                }
            }
        }
        void OpenDashboard()
        {
            var selectedItem = dgDashboardpage.SelectedItem as DashboardClient;

            if (selectedItem == null)
                return;
#if MAC
            var hdr = string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", selectedItem._Name);
            var webDashboardUrl = BasePage.session.Connection.Target == APITarget.Live ? $"https://web.uniconta.com/DashBoards/DashBoardViewer?DashBoardsId={selectedItem._Name}" : 
            $"https://test.uniconta.com:8080/DashBoards/DashBoardViewer?DashBoardsId={selectedItem._Name}";
            AddDockItem(TabControls.ViewUrlPage, new object[] { api.CompanyEntity, webDashboardUrl }, hdr);
#else
            AddDockItem(TabControls.DashBoardViewerPage, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Dashboard"), selectedItem._Name));
#endif
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

                var dashBoardClient = new DashboardClient[moduleWiseList.Count];
                int idx = 0;
                foreach (var item in moduleWiseList)
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

        private void Name_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            DgDashboardpage_RowDoubleClick();
        }
    }
}
