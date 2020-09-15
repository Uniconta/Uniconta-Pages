using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;
using Uniconta.Common;
using System.Collections;
using UnicontaClient.Utilities;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;
using Uniconta.API.System;
using Uniconta.ClientTools.Util;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AdminAccessGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(CompanyUserAccessClient); }
        }
    }

    public partial class AdminAccess : GridBasePage
    {
        CompanyAccessAPI companyAPI;
        public AdminAccess(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            companyAPI = new CompanyAccessAPI(api);
            SetRibbonControl(localMenu, dgAccessGrid);
            dgAccessGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked +=localMenu_OnItemClicked;
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        private async void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAccessGrid.SelectedItem as CompanyUserAccessClient;
            switch (ActionType)
            {
                case "Accept":
                    if (selectedItem == null) return;
                    ErrorCodes res = await companyAPI.GiveCompanyAccess(selectedItem._Uid, 0);
                    if (res != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(res);
                    else
                    {
                        CWUserTasks userTask = new CWUserTasks(companyAPI, selectedItem);
                        userTask.Closing += delegate
                        {
                            if (userTask.DialogResult == true)
                                InitQuery();

                        };
                        userTask.Show();
                    }
                    break;
                case "Deny":
                    if (selectedItem == null) return;
                    ErrorCodes resdeny = await companyAPI.DenyCompanyAccess(selectedItem._Uid);
                    if (resdeny != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(resdeny);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async override Task InitQuery()
        {
            var selectedItem = dgAccessGrid.SelectedItem;
            var users = (CompanyUserAccessClient[])await companyAPI.GetUserRequests(new CompanyUserAccessClient());
            dgAccessGrid.ItemsSource = users.ToList();
            dgAccessGrid.Visibility = Visibility.Visible;
            dgAccessGrid.BusyIndicator = busyIndicator;
            if (selectedItem != null)
                dgAccessGrid.SelectedItem = selectedItem;
        }
       
        public override string NameOfControl
        {
            get { return TabControls.AdminAccessUsers.ToString(); }
        }
    }
}
