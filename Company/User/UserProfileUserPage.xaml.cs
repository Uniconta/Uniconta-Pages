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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserProfileUserPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(UserProfileUserClient); } }
        public override bool Readonly
        {
            get
            {
                return false;
            }
        }
    }

    public partial class UserProfileUserPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.UserProfileUserPage; } }
        CompanyUserAccessClient[] companyUsers;
        public UserProfileUserPage(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgUserProfileUser.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgUserProfileUser.masterRecord as UserProfileClient;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("User"), syncMaster.Name);
            SetHeader(header);
        }

        public UserProfileUserPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgUserProfileUser.UpdateMaster(master);
            localMenu.dataGrid = dgUserProfileUser;
            SetRibbonControl(localMenu, dgUserProfileUser);
            dgUserProfileUser.api = api;
            dgUserProfileUser.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            LoadCompanyUsers(api);
        }

        async void LoadCompanyUsers(CrudAPI crudApi)
        {
            var companyAPI = new CompanyAccessAPI(crudApi);
            companyUsers = (CompanyUserAccessClient[])await companyAPI.GetUserRights(new CompanyUserAccessClient());
        }

        List<CompanyUserAccessClient> UpdateCompanyUserList()
        {
            var lst = dgUserProfileUser.GetVisibleRows() as IList<UserProfileUserClient>;
            List<CompanyUserAccessClient> userList = companyUsers?.ToList();
            foreach (var item in lst)
                userList.Remove(companyUsers?.Where(x => x.UserLoginId == item.LoginId).FirstOrDefault());
            var owner = companyUsers?.Where(x => x._Uid == api.CompanyEntity._OwnerUid).FirstOrDefault();
            if (owner != null)
                userList.Remove(owner);
            return userList;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgUserProfileUser.SelectedItem as UserProfileUserClient;
            switch (ActionType)
            {
                case "AddRow":
                    CWCompanyUserAccess cw = new CWCompanyUserAccess(UpdateCompanyUserList());
                    cw.Closing += delegate
                      {
                          if (cw.DialogResult == true)
                          {
                              var userProfile = new UserProfileUserClient();
                              userProfile.SetMaster(cw.user);
                              dgUserProfileUser.AddRow(userProfile);
                          }
                      };
                    cw.Show();
                    break;
                case "SaveGrid":
                    dgUserProfileUser.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgUserProfileUser.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }

}
