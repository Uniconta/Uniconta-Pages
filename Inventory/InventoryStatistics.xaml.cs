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
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using System.Threading.Tasks;
using UnicontaClient.Models;
using Uniconta.API.Inventory;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvStatisticsGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(InvStatistickClient); }
        }
    }
    public partial class InventoryStatistics : GridBasePage
    {
        UnicontaBaseEntity _master;
        ReportAPI reportApi;
        public InventoryStatistics(BaseAPI API) : base(API, string.Empty)
        {
            Initialize();
        }

        public InventoryStatistics(UnicontaBaseEntity master)
            : base(null)
        {
            _master = master;
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

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "ShowCurrentRowData");
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch(ActionType)
            {
                case "Search":
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
            InvStatistickClient[] invStatsArray;
            if (_master == null)
                invStatsArray = (InvStatistickClient[])await reportApi.ItemStat(new InvStatistickClient(), txtDateFrm.DateTime, txtDateTo.DateTime);
            else
                invStatsArray = (InvStatistickClient[])await reportApi.ItemStatDate(new InvStatistickClient(), txtDateFrm.DateTime, txtDateTo.DateTime, (InvItemClient)_master);
            dgInvStatsGrid.ItemsSource = invStatsArray?.ToList();
            busyIndicator.IsBusy = false;
            dgInvStatsGrid.Visibility = Visibility.Visible;
        }

        public override string NameOfControl
        {
            get { return TabControls.InventoryStatistics; }
        }
    }
}
