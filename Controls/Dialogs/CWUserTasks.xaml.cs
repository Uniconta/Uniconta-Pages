using Uniconta.API.System;
using Uniconta.Common.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools;
using UnicontaClient.Pages;
using System.Windows;

namespace UnicontaClient.Controls.Dialogs
{
    public class TaskGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(TasksAccess); }
        }

        public override bool Readonly { get { return false; } }
    }

    public partial class CWUserTasks : ChildWindow
    {
        CompanyAccessAPI api;
        CompanyUserAccessClient userAccess;
        UserProfileClient userProfile;
        List<TasksAccess> lstAccess;
        bool isNewAccount = false;
        public CWUserTasks(CompanyAccessAPI api, int userID)
        {
            var ua = new CompanyUserAccessClient();
            ua._Uid = userID;
            this.userAccess = ua;
            Init(api);
        }
        public CWUserTasks(CompanyAccessAPI api, CompanyUserAccessClient user)
        {
            this.DataContext = this;
            this.userAccess = user;
            Init(api);
        }

        public CWUserTasks(CompanyAccessAPI api, UserProfileClient _userProfile)
        {
            this.DataContext = this;
            userProfile = _userProfile;
            Init(api);
        }

        public CWUserTasks(CompanyAccessAPI api, CompanyUserAccessClient user, bool _isNewAccount)
        {
            this.DataContext = this;
            this.isNewAccount = _isNewAccount;
            this.userAccess = user;
            Init(api);
        }

        void Init(CompanyAccessAPI api)
        {
            this.api = api;
            lstAccess = new List<TasksAccess>();
            long Rights;
            if (userAccess != null)
                Rights = userAccess._Rights;
            else if (userProfile != null)
                Rights = userProfile._Rights;
            else
                return;
            foreach (CompanyTasks task in Enum.GetValues(typeof(CompanyTasks)))
            {
                if (task == CompanyTasks.FixedProfile)
                    continue;

                TasksAccess access = new TasksAccess();
                access._task = task;
                access._permission = AccessLevel.Get(Rights, task);
                lstAccess.Add(access);
            }
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("SetPermissions");
            dgUserRights.ItemsSource = lstAccess;
            dgUserRights.Visibility = Visibility.Visible;
            this.Loaded += CW_Loaded;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
        }
        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
                {
                    if (OKButton.IsFocused)
                        OKButton_Click(null, null);
                    else if (CancelButton.IsFocused)
                        SetDialogResult(false);
                }
        }
        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (TasksAccess access in lstAccess)
            {
                if (userAccess != null)
                    userAccess._Rights = AccessLevel.Set(userAccess._Rights, access._task, access._permission);
                else if (userProfile != null)
                {
                    userProfile._Rights = AccessLevel.Set(userProfile._Rights, access._task, access._permission);
                }
            }
            if (userAccess != null  && !isNewAccount)
            {
                var err = await api.GiveCompanyAccess(userAccess._Uid, userAccess._Rights);
                if (err != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(err);
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }

    public class TasksAcessTextKey
    {
        public static string Task { get { return Uniconta.ClientTools.Localization.lookup("UserTasks"); } }
        public static string Permission { get { return Uniconta.ClientTools.Localization.lookup("SetPermissions"); } }
    }
    public class TasksAccess
    {
        public CompanyPermissions _permission;
        public CompanyTasks _task;

        [AppEnumAttribute(EnumName = "CompanyTasks")]
        [Display(Name = "Task", ResourceType = typeof(TasksAcessTextKey))]
        public string Task { get { return GetTask((int)_task); } set { _task = (CompanyTasks)AppEnums.CompanyTasks.IndexOf(value); } }

        [AppEnumAttribute(EnumName = "CompanyPermissions")]
        [Display(Name = "Permission", ResourceType = typeof(TasksAcessTextKey))]
        public string Permission { get { return AppEnums.CompanyPermissions.ToString((int)_permission); } set { if (value == null)return; _permission = (CompanyPermissions)AppEnums.CompanyPermissions.IndexOf(value); } }

        string GetTask(int task)
        {
            string _task = string.Empty;
            if (task == (int)CompanyTasks.RunUserTableGroup1)
                _task = string.Format(Uniconta.ClientTools.Localization.lookup("RunUserTableGroupOBJ"), "1");
            else if (task == (int)CompanyTasks.RunUserTableGroup2)
                _task = string.Format(Uniconta.ClientTools.Localization.lookup("RunUserTableGroupOBJ"), "2");
            else if (task == (int)CompanyTasks.RunUserTableGroup3)
                _task = string.Format(Uniconta.ClientTools.Localization.lookup("RunUserTableGroupOBJ"), "3");
            else
                _task = AppEnums.CompanyTasks.ToString(task);
            return _task;
        }

    }
}

