using Uniconta.API.Inventory;
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
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
using System.Windows;
using System.Threading.Tasks;
using Uniconta.API.Service;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorAccountStat : GridBasePage
    {
        ReportAPI reportApi;
        DCAccount _master;
        bool isStatDate = false;
        public DebtorAccountStat(BaseAPI API) : base(API, string.Empty)
        {
            Initialize();
        }
        public DebtorAccountStat(UnicontaBaseEntity master)
            : base(master)
        {
            _master = master as DCAccount;
            Initialize();
        }
        public DebtorAccountStat(object sourceData, bool isDateStat)
            : base(null)
        {
            _master = sourceData as DCAccount;
            isStatDate = isDateStat;
            Initialize();
        }
        byte dctype = 0;
        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var par in Parameters)
            {
                byte.TryParse(par.Value, out dctype);
            }
            base.SetParameter(Parameters);
        }
        private void Initialize()
        {
            this.DataContext = this;
            InitializeComponent();
            localMenu.dataGrid = dgAcStatsGrid;
            SetRibbonControl(localMenu, dgAcStatsGrid);
            dgAcStatsGrid.api = api;
            dgAcStatsGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var Now = BasePage.GetSystemDefaultDate();
            txtDateTo.DateTime = Now;
            txtDateFrm.DateTime = Now.AddDays(1 - Now.Day).AddMonths(-2);
            dgAcStatsGrid.ShowTotalSummary();
            if (api.CompanyEntity.HideCostPrice)
            {
                MarginRatio.Visible = MarginRatio.ShowInColumnChooser = CostValue.Visible = CostValue.ShowInColumnChooser = false;
            }
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "Search":
                    Search();
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
            if (dctype > 0)
                reportApi.DCType = dctype;
            dgAcStatsGrid.ItemsSource = null;
            busyIndicator.IsBusy = true;
            Task<Statistick[]> tsk;
            if (_master == null)
                tsk = reportApi.AccountStat(new InvStatistickClient(), txtDateFrm.DateTime, txtDateTo.DateTime);
            else if (_master != null && isStatDate)
                tsk = reportApi.AccountStatDate(new InvStatistickClient(), txtDateFrm.DateTime, txtDateTo.DateTime, _master);
            else
                tsk = reportApi.AccountStat(new InvStatistickClient(), txtDateFrm.DateTime, txtDateTo.DateTime, _master);
            dgAcStatsGrid.ItemsSource = await tsk;
            busyIndicator.IsBusy = false;
            dgAcStatsGrid.Visibility = Visibility.Visible;
        }

        public override string NameOfControl
        {
            get { return TabControls.DebtorAccountStat; }
        }

        private void Search()
        {
            InitQuery();
        }
    }
}
