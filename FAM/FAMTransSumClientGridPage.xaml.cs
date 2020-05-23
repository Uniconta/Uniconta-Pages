using System;
using System.Collections;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class FAMTransSumClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(FAMTransSumClient); } }
        public override bool Readonly { get { return true; } }

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            base.PrintGrid(reportName, printparam, format, page, false);
        }
    }

    public class FAMTransSumSort : IComparer<FAMTransSumClient>
    {
        public int Compare(FAMTransSumClient x, FAMTransSumClient y)
        {
            var c = string.Compare(x._Asset, y._Asset);
            if (c != 0)
                return c;
            if (x._AssetPostType == (byte)Uniconta.DataModel.FAMTransCodes.Acquisition)
            {
                if (y._AssetPostType == (byte)Uniconta.DataModel.FAMTransCodes.Acquisition)
                    return 0;
                return -1;
            }
            if (y._AssetPostType == (byte)Uniconta.DataModel.FAMTransCodes.Acquisition)
            {
                if (x._AssetPostType == (byte)Uniconta.DataModel.FAMTransCodes.Acquisition)
                    return 0;
                return 1;
            }
            return (int)x._AssetPostType - (int)y._AssetPostType;
        }
    }

    /// <summary>
    /// Interaction logic for FAMTransSumClientGridPage.xaml
    /// </summary>
    public partial class FAMTransSumClientGridPage : GridBasePage
    {

        static DateTime fromDate, toDate;
        public FAMTransSumClientGridPage(BaseAPI Api) : base(Api, string.Empty)
        {
            Init();
        }

        public override string NameOfControl { get { return TabControls.FAMTransSumClientGridPage; } }
        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgFAMTransSumGrid);
            dgFAMTransSumGrid.api = api;
            dgFAMTransSumGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            FromDate.DateTime = fromDate;
            ToDate.DateTime = toDate;
            dgFAMTransSumGrid.ShowTotalSummary();
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Search":
                    LoadGrid();
                    break;
                case "Aggregate":
                    ShowAggregatePage();
                    break;
                case "Transactions":
                    LoadFAMTransactionPage();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void LoadFAMTransactionPage()
        {
            var param = new object[3];
            var selectedItem = dgFAMTransSumGrid.SelectedItem as FAMTransSumClient;
            if (selectedItem != null)
            {
                param[0] = selectedItem;
                param[1] = fromDate;
                param[2] = toDate;

                AddDockItem(TabControls.FAMTransGridPage, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Asset"), selectedItem.Asset));
            }
        }

        private void ShowAggregatePage()
        {
            object[] paramArr = new object[4];
            paramArr[0] = api;
            paramArr[1] = dgFAMTransSumGrid.GetVisibleRows();
            paramArr[2] = FromDate.DateTime;
            paramArr[3] = ToDate.DateTime;

            AddDockItem(TabControls.FAMTransSumClientAggregateGridPage, paramArr);
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.FAM));
        }

        async Task LoadGrid()
        {
            fromDate = FromDate.DateTime;
            toDate = ToDate.DateTime;

            var filters = new List<PropValuePair>();
            if (fromDate != DateTime.MinValue)
                filters.Add(PropValuePair.GenereteParameter("FromDate", typeof(string), Convert.ToString(fromDate.Ticks)));
            if (toDate != DateTime.MinValue)
                filters.Add(PropValuePair.GenereteParameter("ToDate", typeof(string), Convert.ToString(toDate.Ticks)));

            busyIndicator.IsBusy = true;
            var transSum = await api.Query(new FAMTransSumClient(), null, filters);
            if (transSum == null)
                dgFAMTransSumGrid.ItemsSource = null;
            else
            {
                Array.Sort(transSum, new FAMTransSumSort());
                dgFAMTransSumGrid.SetSource(transSum);
            }
            busyIndicator.IsBusy = false;
        }

        public override object GetPrintParameter()
        {
            var fromDate = FromDate.Text == string.Empty ? string.Empty : FromDate.DateTime.ToShortDateString();
            var toDate = ToDate.Text == string.Empty ? string.Empty : ToDate.DateTime.ToShortDateString();

            var printData = new PageReportHeader()
            {
                CurDateTime = DateTime.Now.ToString("g"),
                CompanyName = api.CompanyEntity._Name,
                ReportName = Uniconta.ClientTools.Localization.lookup("Statement"),
                Header = string.Format("({0} - {1})", fromDate, toDate),
            };
            return printData;
        }
    }
}
