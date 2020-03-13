using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class EmployeeProjectTransactionPage : GridBasePage
    {
        public EmployeeProjectTransactionPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitializePage();
        }

        string employee;
        int monthNumber;
        public EmployeeProjectTransactionPage(string emp, int monthNo) : base(null)
        {
            InitializeComponent();
            employee = emp;
            monthNumber = monthNo;
            InitializePage();
        }

        void InitializePage()
        {
            SetRibbonControl(localMenu, dgEmpProjectTrans);
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            dgEmpProjectTrans.api = api;
            dgEmpProjectTrans.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            LoadData();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = false;
            return true;
        }
        public override Task InitQuery()
        {
            return null;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch(ActionType)
            {
                case "RefreshGrid":
                    LoadData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void LoadData()
        {
            var inputs = new List<PropValuePair>() { PropValuePair.GenereteWhereElements("Employee", typeof(string), employee) };
            busyIndicator.IsBusy = true;
            var projLst = await api.Query<ProjectTransClient>(inputs) as IEnumerable<ProjectTransClient>;
            var lst = projLst?.Where(x => x.Date.Month == monthNumber).ToArray();
            dgEmpProjectTrans.SetSource(lst);
            busyIndicator.IsBusy = false;
            dgEmpProjectTrans.Visibility = Visibility.Visible;
        }
    }
}
