using DevExpress.XtraReports.UI;
using System;
using System.Collections;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Reports;
using Uniconta.Reports.Controls.Reporting;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppClientSort : IComparer
    {
        public int Compare(object x, object y)
        {
            var xTag = Convert.ToString(x);
            var yTag = Convert.ToString(y);
            return string.Compare(xTag, yTag, StringComparison.Ordinal);
        }
    }

    public class AppsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(AppClient); } }
        public override IComparer GridSorting => new AppClientSort();
        public override bool IsAutoSave => false;
    }

    public partial class AppsPage : GridBasePage
    {
        public override string NameOfControl => TabControls.AppsPage;

        public AppsPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            dgGrid.api = api;
            dgGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }


        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGrid.SelectedItem as AppClient;
            switch (ActionType)
            {
                case "AddItem":
                case "AddRow":
                    AddDockItem(TabControls.AppsPage2, api, Uniconta.ClientTools.Localization.lookup("App"), "Add_16x16");
                    break;
                case "EditItem":
                case "EditRow":
                    {
                        if (selectedItem == null)
                            return;
                        selectedItem.CompanyId = api.CompanyId;
                        AddDockItem(TabControls.AppsPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("App"));
                        break;
                    }
                case "CopyApp":
                    if (selectedItem == null)
                        return;
                    CopyApp(selectedItem);
                    break;
                case "UpgradeApp":
                    if (selectedItem == null)
                        return;
                    UpgradeApp(selectedItem);
                    break;
                case "SetAsActive":
                    if (selectedItem == null)
                        return;
                    SetAppAsActive(selectedItem);
                    break;
                case "ShowContent":
                    if (selectedItem == null)
                        return;
                    ShowContent(selectedItem);
                    break;
                case "Subscriptions":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.AppSharePage, selectedItem, Uniconta.ClientTools.Localization.lookup("App"));
                    break;
                case "AppLog":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.AppLogPage, selectedItem, $"{Uniconta.ClientTools.Localization.lookup("Log")}: {selectedItem.Name}");
                    break;
                case "ReloadApp":
                    if (selectedItem == null)
                        return;
                    ReloadApp(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        private async Task ReloadApp(AppClient selectedItem)
        {
            var userApi = new Uniconta.API.System.UserAPI(api);
            var err = await userApi.AppReload(selectedItem);
            UtilDisplay.ShowErrorCode(err);
        }
        private async Task CopyApp(AppClient selectedItem)
        {
            var userApi = new Uniconta.API.System.UserAPI(api);
            var appId = await userApi.AppCopy(selectedItem);
            if (appId > 0)
                localMenu_OnItemClicked("RefreshGrid");
            else
                UtilDisplay.ShowErrorCode(userApi.LastError);
        }

        private async Task UpgradeApp(AppClient selectedItem)
        {
            var userApi = new Uniconta.API.System.UserAPI(api);
            var appId = await userApi.AppUpgrade(selectedItem);
            UtilDisplay.ShowErrorCode(userApi.LastError);
        }
        public static void SetAppAsActive(Uniconta.DataModel.App app)
        {
            App.MainPageObj.ActiveApp = app;
            App.MainPageObj.LeftMenu.UpdateAppLabel();
        }
        XtraReport xtraReport = null;
        async void ShowContent(Uniconta.DataModel.App app)
        {
            var xtraReport = new Uniconta.Reports.Controls.Reporting.AppContentStandardReport();

            var appContent = new AppContent(app, api);
            await appContent.LoadContent();
            var collection = new[] { appContent };
            xtraReport.DataSource = collection;
            AddDockItem(TabControls.ReportsPreviewPage, new object[] { xtraReport }, $"{Uniconta.ClientTools.Localization.lookup("Preview")}: {app._Name}");
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AppsPage2 && argument != null)
                dgGrid.UpdateItemSource(argument);
        }

        private void URL_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var text = (sender as System.Windows.Controls.TextBlock)?.Text;
            if (string.IsNullOrWhiteSpace(text))
                return;

            if (!text.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !text.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                text = "https://" + text;

            Utility.OpenWebSite(text);
            e.Handled = true;
        }
    }
}
