using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Pages.Maintenance;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using Uniconta.API.Crm;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CrmCampaignPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmCampaignClient); } }
        public override IComparer GridSorting { get { return new CrmCampaignSort(); } }
    }

    public partial class CrmCampaignPage : GridBasePage
    {
        public CrmCampaignPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }
        public CrmCampaignPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            ((TableView)dgCrmCampaignGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            dgCrmCampaignGrid.UpdateMaster(master);
            localMenu.dataGrid = dgCrmCampaignGrid;
            dgCrmCampaignGrid.api = api;
            dgCrmCampaignGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmCampaignGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CrmCampaignPage2)
                dgCrmCampaignGrid.UpdateItemSource(argument);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCrmCampaignGrid.SelectedItem as CrmCampaignClient;

            switch (ActionType)
            {
                case "AddRow":
                    object[] param = new object[2];
                    param[0] = api;
                    param[1] = null;
                    AddDockItem(TabControls.CrmCampaignPage2, param, Uniconta.ClientTools.Localization.lookup("Campaign"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Campaign"), selectedItem.Name);
                        AddDockItem(TabControls.CrmCampaignPage2, selectedItem, header);
                    }
                    break;

                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgCrmCampaignGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCrmCampaignGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "CreateEmailList":
                    if (selectedItem == null) return;
                    var createTitle = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("EmailList"));
                    AddDockItem(TabControls.CreateEmailListPage, selectedItem, string.Format("{0}:{2} {1:g}", createTitle, selectedItem.Created, Uniconta.ClientTools.Localization.lookup("Campaign")));
                    break;
                case "ShowEmailList":
                    if (selectedItem == null) return;
                    var showTitle = string.Format(Uniconta.ClientTools.Localization.lookup("showOBJ"), Uniconta.ClientTools.Localization.lookup("EmailList"));
                    AddDockItem(TabControls.EmailListPage, selectedItem, string.Format("{0}:{2} {1:g}", showTitle, selectedItem.Created, Uniconta.ClientTools.Localization.lookup("Campaign")));
                    break;
                case "DeleteEmailList":
                    if (selectedItem != null)
                        DeleteEmailList(selectedItem);
                    break;
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var header = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("Campaign"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgCrmCampaignGrid.syncEntity, header);
                    }
                    break;
                case "AddFollowUp":
                    if (selectedItem == null) return;
                    object[] fuParam = new object[2];
                    fuParam[0] = api;
                    fuParam[1] = selectedItem;
                    AddDockItem(TabControls.CrmCampaignFollowUp, fuParam, true, String.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("FollowUp")), ";component/Assets/img/Add_16x16.png");
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void DeleteEmailList(CrmCampaignClient campaign)
        {
            EraseYearWindow EraseYearWindowDialog = new EraseYearWindow(campaign.Name, false);
            EraseYearWindowDialog.Closed += async delegate
            {
                if (EraseYearWindowDialog.DialogResult == true)
                {
                    CrmAPI crmApi = new CrmAPI(api);
                    var res = await crmApi.DeleteMembers(campaign);

                    if (res != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(res);
                    else
                    {
                        UtilDisplay.ShowErrorCode(res);
                    }
                }
            };
            EraseYearWindowDialog.Show();
        }
    }
}
