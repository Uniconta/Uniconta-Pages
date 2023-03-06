using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.System;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserProfileDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserProfileClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class UserProfilePage : GridBasePage
    {
        CompanyAccessAPI companyAPI;
        public override string NameOfControl { get { return TabControls.UserProfilePage; } }

        public UserProfilePage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgUserProfile;
            dgUserProfile.api = api;
            dgUserProfile.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgUserProfile);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }
        
        int selectedRowIndex = 0;
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserProfile.SelectedItem as UserProfileClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgUserProfile.AddRow();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserProfile.DeleteRow();
                    break;
                case "SaveGrid":
                    dgUserProfile.SaveData();
                    break;
                case "Lines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "UserProfileUser":
                    if (selectedItem != null)
                        SaveAndOpenUsers(selectedItem);
                    break;
                case "SetPermissions":
                    if (selectedItem == null)
                        return;
                    dgUserProfile.SetLoadedRow(selectedItem);
                    CWUserTasks userTask = new CWUserTasks(companyAPI, selectedItem);
                    userTask.Closing += delegate
                    {
                        if (userTask.DialogResult == true)
                        {
                            dgUserProfile.SetModifiedRow(selectedItem);
                            dgUserProfile.UpdateItemSource(2, selectedItem);
                        }
                    };
                    userTask.Show();
                    break;
                case "TableRights":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserProfileTableAccessPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TableRights"), selectedItem._Name));
                    break;
                case "BlockFunctions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserProfileRestrictedMethodPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BlockFunctions"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private bool dataChanged;
        public override bool IsDataChaged
        {
            get
            {
                if (dataChanged)
                    return true;
                return base.IsDataChaged;
            }
        }

        async void SaveAndOpenLines(UserProfileClient selectedItem)
        {
            if (dgUserProfile.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && selectedItem.RowId == 0)
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.UserProfileControlPage, dgUserProfile.syncEntity, string.Format("{0}: {1}", Localization.lookup("ProfileLines"), selectedItem._Name));
        }
        async void SaveAndOpenUsers(UserProfileClient selectedItem)
        {
            if (dgUserProfile.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && selectedItem.RowId == 0)
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.UserProfileUserPage, dgUserProfile.syncEntity, string.Format("{0}: {1}", Localization.lookup("User"), selectedItem._Name));
        }
    }
}
