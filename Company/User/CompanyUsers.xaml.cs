using Uniconta.API.System;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.DataModel.System;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.Common.User;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CompanyUsersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanyUserAccessClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class CompanyUsers : GridBasePage
    {
        CompanyAccessAPI companyAPI;
        private bool _isFromCreateUser;
        private string _loginID;

        public override string NameOfControl
        {
            get { return TabControls.CompanyUsers.ToString(); }
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }

        public override bool CheckCompany { get { return false; } }

        public CompanyUsers(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            CrudAPI Crudapi = new CrudAPI(session, syncEntity.Row as CompanyClient);
            Init(Crudapi);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            var company = args as CompanyClient;
            companyAPI = new CompanyAccessAPI(new CrudAPI(session, company));
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CompanyUsers"), company._Name);
            SetHeader(header);
            InitQuery();
        }

        public CompanyUsers(UnicontaBaseEntity sourcedata)
            : base(null)
        {
            CrudAPI Crudapi = new CrudAPI(session, sourcedata as CompanyClient);
            Init(Crudapi);
        }

        public CompanyUsers(BaseAPI API)
            : base(API, string.Empty)
        {
            Init(api);
        }

        public CompanyUsers(bool isFromCreateUser, string loginID)
            : base(null)
        {
            CrudAPI Crudapi = new CrudAPI(api);
            this._isFromCreateUser = isFromCreateUser;
            this._loginID = loginID;
            Init(Crudapi);
        }

        private void Init(CrudAPI Crudapi)
        {
            InitializeComponent();
            companyAPI = new CompanyAccessAPI(Crudapi);
            SetRibbonControl(localMenu, dgCompanyUsersGrid);
            gridControl.AutoWidth = true;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        
        async void RemoveAccess(CompanyUserAccessClient selectedItem)
        {
            ErrorCodes err = await companyAPI.RemoveCompanyAccess(selectedItem._Uid);
            if (err == ErrorCodes.Succes)
            {
                var view = (TableView) dgCompanyUsersGrid.View;
                view.DeleteRow(view.FocusedRowHandle);
            }
            else
                UtilDisplay.ShowErrorCode(err);
        }
        int selectedRowIndex=0;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCompanyUsersGrid.SelectedItem as CompanyUserAccessClient;
            switch (ActionType)
            {
                case "AddRow":
                    CWSearchUser searchUserDialog = new CWSearchUser(companyAPI);
                    searchUserDialog.Closing += delegate
                    {
                        if (searchUserDialog.DialogResult == true)
                        {
                            if (searchUserDialog.lstSetupType.SelectedIndex != 0)
                                InitQuery();
                            else
                            {
                                object[] parm = new object[3];
                                parm[0] = api;
                                parm[1] = true;
                                parm[2] = dockCtrl.Activpanel;
                                AddDockItem(TabControls.UsersPage2, parm, Uniconta.ClientTools.Localization.lookup("User"), ";component/Assets/img/Add_16x16.png");
                            }
                        }
                    };
                    searchUserDialog.Show();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        RemoveAccess(selectedItem);
                    break;
                case "ChangeOwnership":
                    if (selectedItem == null)
                        return;
                    var msg = string.Format(Uniconta.ClientTools.Localization.lookup("OwnershipConfirmation"),
                        companyAPI.CompanyEntity._Name, selectedItem._Name);
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(msg, null, false);
                    confirmationDialog.Closing += async delegate
                    {
                        if (confirmationDialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                        {
                            var errcode = await companyAPI.TransferOwnershipOfCompany(selectedItem._Uid);
                            if (errcode != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(errcode);
                            else
                            {
                                dockCtrl.CloseDockItem();
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("OwnershipChanged"),Uniconta.ClientTools.Localization.lookup("Warning"));
                            }
                        }
                    };
                    confirmationDialog.Show();
                    break;
                case "SetPermissions":
                    if (selectedItem == null)
                        return;
                    selectedRowIndex = dgCompanyUsersGrid.tableView.FocusedRowHandle;
                    CWUserTasks userTask = new CWUserTasks(companyAPI, selectedItem);
                    userTask.Closing += delegate
                    {
                        if (userTask.DialogResult == true)
                            InitQuery();

                    };
                    userTask.Show();
                    break;
                case "UserOf":
                    if (selectedItem != null)
                    {
                        object[] ob = new object[2];
                        ob[0] = selectedItem;
                        ob[1] = selectedItem.Userid;
                        AddDockItem(TabControls.CompaniesPage, ob,
                            string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Companies"),
                                Uniconta.ClientTools.Localization.lookup("Access")));
                    }
                    break;
                case "OwnerOf":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CompaniesPage, selectedItem,
                            string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Companies"),
                                Uniconta.ClientTools.Localization.lookup("Owner")));
                    break;
                case "RefreshGrid":
                    selectedRowIndex = 0;
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async override Task InitQuery()
        {
            var users = (CompanyUserAccessClient[]) await companyAPI.GetUserRights(new CompanyUserAccessClient());
            dgCompanyUsersGrid.ItemsSource = null;
            dgCompanyUsersGrid.ItemsSource = users?.ToList();
            dgCompanyUsersGrid.Visibility = Visibility.Visible;
            dgCompanyUsersGrid.BusyIndicator = busyIndicator;
            dgCompanyUsersGrid.tableView.FocusedRowHandle = selectedRowIndex;
        }
    }
}
