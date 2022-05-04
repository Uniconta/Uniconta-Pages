using UnicontaClient.Utilities;
using Uniconta.API.System;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
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
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.DataModel;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Controls.Dialogs;
using Uniconta.Common.User;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class AccountantAccess : ChildWindow
    {
        CompanyAccessAPI accessAPI;
        AccountantClient currentAccountant;
        List<TasksAccess> lstAccess;
        public AccountantAccess(CrudAPI api, AccountantClient accountant)
        {
            this.DataContext = this;
            InitializeComponent();
            currentAccountant = accountant;
            this.Title = Uniconta.ClientTools.Localization.lookup("Accountant");
#if SILVERLIGHT
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            OKButton.Content = string.Format(Uniconta.ClientTools.Localization.lookup("AssignOBJ"), "");
            if (accountant != null)
                txtCurrentAccountant.Text = string.Concat("(", accountant.Name, ")");
            BindAccountant(api, accountant);
            accessAPI = new CompanyAccessAPI(api);
            lstAccess = new List<TasksAccess>();
            GetRights();
            this.Loaded += CW_Loaded;
        }

        private async void GetRights()
        {
            var Rights = await accessAPI.GetAccountantRights();
            var tasks = (CompanyTasks[])Enum.GetValues(typeof(CompanyTasks));
            for (int i = 0; i <= (int)AccessLevel.MaxTask; i++)
            {
                var task = tasks[i];
                TasksAccess access = new TasksAccess();
                access._task = task;
                access._permission = AccessLevel.Get(Rights, task);
                if (currentAccountant == null)
                    access._permission = CompanyPermissions.Full;
                else
                    access._permission = AccessLevel.Get(Rights, task);
                lstAccess.Add(access);
            }
            dgUserRights.ItemsSource = lstAccess;
            dgUserRights.Visibility = Visibility.Visible;
        }

        private async void BindAccountant(CrudAPI api, AccountantClient currentAccountant)
        {
            var acctant = await api.Query<AccountantClient>();
            cbAccountant.ItemsSource = acctant;
            grdAcctantDetail.DataContext = currentAccountant;
            if (currentAccountant != null)
                cbAccountant.SelectedItem = currentAccountant;
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
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }
        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedAcct = (cbAccountant.SelectedItem as AccountantClient) ?? currentAccountant;
            ErrorCodes res = ErrorCodes.RecordNotSelected;
            if (selectedAcct != null)
            {
                long r = 0;
                foreach (TasksAccess access in lstAccess)
                    r = AccessLevel.Set(r, access._task, access._permission);

                res = await accessAPI.GiveAccountantAccess(selectedAcct, r);
            }
            UtilDisplay.ShowErrorCode(res);
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private async void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorCodes res = await accessAPI.GiveAccountantAccess(null, 0);
            UtilDisplay.ShowErrorCode(res);
            SetDialogResult(true);
        }

        private void cbAccountant_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var accountant = cbAccountant.SelectedItem as AccountantClient;
            grdAcctantDetail.DataContext = accountant;
        }
    }
}

