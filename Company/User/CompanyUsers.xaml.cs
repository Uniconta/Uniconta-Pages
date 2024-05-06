using Uniconta.API.System;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using DevExpress.Xpf.Grid;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.Common.User;
using Uniconta.API.Service;
using System.Collections.Generic;

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
        private CompanyUserAccessClient[] _companyUsers;
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
            if (!api.CompanyEntity.Project)
            {
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "AddProjectTimeUser");
            }
        }

        async void RemoveAccess(CompanyUserAccessClient selectedItem)
        {
            ErrorCodes err = await companyAPI.RemoveCompanyAccess(selectedItem._Uid);
            if (err == ErrorCodes.Succes)
            {
                var view = (TableView)dgCompanyUsersGrid.View;
                view.DeleteRow(view.FocusedRowHandle);
            }
            else
                UtilDisplay.ShowErrorCode(err);
        }
        int selectedRowIndex = 0;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCompanyUsersGrid.SelectedItem as CompanyUserAccessClient;
            switch (ActionType)
            {
                case "AddStandardUser":
                    CreateUser(UserTypes.StandardUser);
                    break;
                case "AddWorkUser":
                    CreateUser(UserTypes.WorkAppUser);
                    break;
                case "AddProjectTimeUser":
                    CreateUser(UserTypes.ProjectTimeUser);
                    break;
                case "AddInvoiceUser":
                    CreateUser(UserTypes.InvoiceUser);
                    break;
                case "CopyRecord":
                    CopyUsersFromCompanies();
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.EditCompanyUser, new object[2] { selectedItem, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Edit"), selectedItem.UserName));
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
                                CloseDockItem();
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("OwnershipChanged"), Uniconta.ClientTools.Localization.lookup("Warning"));
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
                case "FixedProfiles":
                    if (selectedItem == null)
                        return;
                    selectedRowIndex = dgCompanyUsersGrid.tableView.FocusedRowHandle;
                    CWFixedProfiles profile = new CWFixedProfiles(companyAPI, selectedItem);
                    profile.Closing += delegate
                    {
                        if (profile.DialogResult == true)
                        {
                            selectedItem._Rights = profile.userAccess._Rights;
                            selectedItem.NotifyPropertyChanged("FixedProfile");
                        }
                    };
                    profile.Show();
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
                case "UserOperations":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserOperationsLog, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("UserOperations"), selectedItem._Name));
                    break;
                case "UserLoginHistory":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AllUsersLoginHistoryPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("UserLoginHistory"), selectedItem._Name));
                    break;
                case "SetDefaultCompany":
                    if (selectedItem != null)
                        SetDefaultCompany(selectedItem);
                    break;
                case "TableRights":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserTableAccessPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TableRights"), selectedItem._Name));
                    break;
                case "BlockFunctions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserRestrictedMethodPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BlockFunctions"), selectedItem._Name));
                    break;
                case "UserProfiles":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserProfileUserPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("UserProfiles"), selectedItem._Name));
                    break;
                case "CreateEmployee":
                    if (selectedItem != null)
                        CreateOrSelectEmployee(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        void CreateUser(UserTypes userType)
        {
            switch (userType)
            {
                case UserTypes.StandardUser:
                case UserTypes.ProjectTimeUser:
                case UserTypes.InvoiceUser:
                    var curComp = UtilDisplay.GetDefaultCompany();
                    long rights = curComp.Rights;
                    if (AccessLevel.Get(rights, CompanyTasks.AsOwner) == CompanyPermissions.Full)
                    {
                        CWSearchUser searchUserDialog = new CWSearchUser(companyAPI, _companyUsers);
                        searchUserDialog.UserType = userType;
                        searchUserDialog.Closing += delegate
                        {
                            if (searchUserDialog.DialogResult == true)
                            {
                                if (searchUserDialog.lstSetupType.SelectedIndex != 0)
                                {

                                    if (userType != UserTypes.StandardUser)
                                        SetFixedProfile(userType, searchUserDialog.SearchedText);
                                    else
                                        InitQuery();
                                }
                                else
                                {
                                    object[] parm = new object[3];
                                    parm[0] = api;
                                    parm[1] = true;
                                    parm[2] = dockCtrl.Activpanel;
                                    var upage = dockCtrl.AddDockItem(TabControls.UsersPage2, ParentControl, parm, Uniconta.ClientTools.Localization.lookup("User"), "Add_16x16") as UsersPage2;
                                    upage.UserType = userType;
                                }
                            }
                        };
                        searchUserDialog.Show();
                    }
                    else
                        UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("NoRightsToInsert"), Uniconta.ClientTools.Localization.lookup("User")), Uniconta.ClientTools.Localization.lookup("Warning"));
                    break;
                case UserTypes.WorkAppUser:
                    object[] par = new object[3];
                    par[0] = api;
                    par[1] = true;
                    par[2] = dockCtrl.Activpanel;
                    var page = dockCtrl.AddDockItem(TabControls.UsersPage2, ParentControl, par, Uniconta.ClientTools.Localization.lookup("User"), "Add_16x16") as UsersPage2;
                    page.UserType = userType;
                    break;
            }
        }
        private void CopyUsersFromCompanies()
        {
            var cwCompanies = new CWSelectCompany(api);
            cwCompanies.Closed += async delegate
            {
                if (cwCompanies.DialogResult == true && cwCompanies._CompanyObj != null)
                {
                    var companyAccessApi = new CompanyAccessAPI(api);
                    var result = await companyAccessApi.CopyUsersToCompany(cwCompanies._CompanyObj);
                    UtilDisplay.ShowErrorCode(result);
                    if (result == ErrorCodes.Succes)
                        InitQuery();
                }
            };
            cwCompanies.Show();
        }

        async private void SetDefaultCompany(CompanyUserAccessClient selectedItem)
        {
            var compApi = new CompanyAPI(api);
            var result = await compApi.SetDefaultCompany(selectedItem);

            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.EditCompanyUser || screenName == TabControls.UsersPage2)
                dgCompanyUsersGrid.UpdateItemSource(argument);
            else if (screenName == TabControls.EmployeePage2 && dgCompanyUsersGrid.SelectedItem != null)
                (dgCompanyUsersGrid.SelectedItem as CompanyUserAccessClient)._Employee = ((argument as object[])?[1] as EmployeeClient).KeyStr;
        }

        async void SetFixedProfile(UserTypes userType, string LoginId)   
        {
            await InitQuery();
            FixedProfiles fixedProfile = FixedProfiles.None;
            if (userType == UserTypes.ProjectTimeUser)
                fixedProfile = FixedProfiles.ProjectTimeUser;
            else if (userType == UserTypes.InvoiceUser)
                fixedProfile = FixedProfiles.Invoice;
            var userAccess= _companyUsers.Where(u => u._LoginId == LoginId).FirstOrDefault();
            if (userAccess != null)
            {
                var rights = AccessLevel.SetFixedProfile(userAccess._Rights, fixedProfile);
                userAccess._Rights = rights;
                userAccess.NotifyPropertyChanged("FixedProfile");
                var err = await companyAPI.GiveCompanyAccess(userAccess._Uid, rights);
            }
        }
        public async override Task InitQuery()
        {
            _companyUsers = (CompanyUserAccessClient[])await companyAPI.GetUserRights(new CompanyUserAccessClient());
            dgCompanyUsersGrid.ItemsSource = null;
            dgCompanyUsersGrid.ItemsSource = _companyUsers?.ToList();
            dgCompanyUsersGrid.Visibility = Visibility.Visible;
            dgCompanyUsersGrid.BusyIndicator = busyIndicator;
            dgCompanyUsersGrid.tableView.FocusedRowHandle = selectedRowIndex;
        }
        void CreateOrSelectEmployee(CompanyUserAccessClient userAccess)
        {
            var dialogEmployees = new CWEmployee(api);
            dialogEmployees.DialogTableId = 2000000107;
            dialogEmployees.IsCreateEmployee = true;
            dialogEmployees.Closing += delegate
            {
                if (dialogEmployees.DialogResult == true)
                {
                    var selectedEmployee = dialogEmployees.SelectedEmployee;
                    if (selectedEmployee != null)
                    {
                        if (!string.IsNullOrEmpty(selectedEmployee._UserLogidId))
                        {
                            var msg = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EmployeeAssigned"), Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"));
                            CWConfirmationBox confirmationDialog = new CWConfirmationBox(msg, null, false);
                            confirmationDialog.Closing += delegate
                            {
                                if (confirmationDialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                                {
                                    SetEmployee(selectedEmployee, userAccess);
                                }
                            };
                            confirmationDialog.Show();
                        }
                        else
                            SetEmployee(selectedEmployee, userAccess);
                    }
                    else
                        CreateEmployee(userAccess);
                }
            };
            dialogEmployees.Show();
        }

        void SetEmployee(Uniconta.DataModel.Employee selectedEmployee, CompanyUserAccessClient userAccess)
        {
            var modified = new List<Uniconta.DataModel.Employee>();
            if (!string.IsNullOrEmpty(userAccess._Employee))
            {
                var alreadyAssignedEmployee = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Employee)).Get(userAccess._Employee) as Uniconta.DataModel.Employee;
                if (alreadyAssignedEmployee != null)
                {
                    alreadyAssignedEmployee._UserLogidId = alreadyAssignedEmployee._UserName = null;
                    modified.Add(alreadyAssignedEmployee);
                }
            }
            selectedEmployee._UserLogidId = userAccess._LoginId;
            selectedEmployee._UserName = userAccess._Name;
            modified.Add(selectedEmployee);
            api.UpdateNoResponse(modified);
            userAccess._Employee = selectedEmployee.KeyStr;
            userAccess.NotifyPropertyChanged("Employee");
        }
        void CreateEmployee(CompanyUserAccessClient userAccess)
        {
            bool isEdit = false;
            var emp = new EmployeeClient();
            emp._Uid = userAccess._Uid;
            emp._Name = userAccess._Name;
            emp._UserLogidId = userAccess._LoginId;
            emp._UserName = userAccess._Name;
            AddDockItem(TabControls.EmployeePage2, new object[2] { emp, isEdit }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Add"), emp._UserName));
        }
    }
}
