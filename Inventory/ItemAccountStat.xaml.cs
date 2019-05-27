using Uniconta.API.Inventory;
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
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
using System.Windows;
using System.Threading.Tasks;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class ItemAccountStat: GridBasePage
    {
        ReportAPI reportApi;
        public ItemAccountStat(BaseAPI API) : base(API, string.Empty)
        {
            Initialize();
        }

        private void Initialize()
        {
            this.DataContext = this;
            InitializeComponent();
            SetRibbonControl(localMenu, dgInvStatsGrid);
            dgInvStatsGrid.api = api;
            dgInvStatsGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var Now = BasePage.GetSystemDefaultDate();
            txtDateTo.DateTime = Now;
            txtDateFrm.DateTime = Now.AddDays(1 - Now.Day).AddMonths(-2);
            dgInvStatsGrid.ShowTotalSummary();
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch(ActionType)
            {
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
            if (reportApi == null)
                reportApi = new ReportAPI(api);

            dgInvStatsGrid.ItemsSource = null;
            busyIndicator.IsBusy = true;
            var invStatsArray = (InvStatistickClient[])await reportApi.ItemAccountStat(new InvStatistickClient(), txtDateFrm.DateTime, txtDateTo.DateTime);
            dgInvStatsGrid.ItemsSource = invStatsArray.ToList();
            busyIndicator.IsBusy = false;
            dgInvStatsGrid.Visibility = Visibility.Visible;
        }

        public override string NameOfControl
        {
            get { return TabControls.ItemAccountStat; }
        }

        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            InitQuery();
        }
    }
}
