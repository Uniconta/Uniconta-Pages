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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SimulateDebtorBudgetLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorBudgetLineClient); } }
        public override bool CanInsert { get { return false; } }
        public override bool Readonly { get { return true; } }
    }

    /// <summary>
    /// Interaction logic for SimulatedDebtorBudgetLine.xaml
    /// </summary>
    public partial class SimulatedDebtorBudgetLinePage : GridBasePage
    {
        public SimulatedDebtorBudgetLinePage(object param) : base(null)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgSimulatedDebtorBudgetLine);
            localMenu.dataGrid = dgSimulatedDebtorBudgetLine;
            dgSimulatedDebtorBudgetLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgSimulatedDebtorBudgetLine.ItemsSource = param;
            dgSimulatedDebtorBudgetLine.Visibility = Visibility.Visible;
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Aggregate":
                    if (aggregateList != null)
                    {
                        dgSimulatedDebtorBudgetLine.ItemsSource = mainList;
                        aggregateList = null;
                    }
                    else
                        aggregate();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        IList<DebtorBudgetLineClient> mainList;
        IList<DebtorBudgetLineClient> aggregateList;
        void aggregate()
        {
            if (mainList == null)
                mainList = (IList<DebtorBudgetLineClient>)dgSimulatedDebtorBudgetLine.ItemsSource;
            if (mainList == null || mainList.Count == 0)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoDataCollected"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }

            aggregateList = (from rec in mainList
                             group rec by new { rec._Account, rec._Date, rec._Dim1, rec._Dim2, rec._Dim3, rec._Dim4, rec._Dim5 } into g
                             select new DebtorBudgetLineClient
                             {
                                 _Date = g.Key._Date,
                                 _Account = g.Key._Account,
                                 _Dim1 = g.Key._Dim1,
                                 _Dim2 = g.Key._Dim2,
                                 _Dim3 = g.Key._Dim3,
                                 _Dim4 = g.Key._Dim4,
                                 _Dim5 = g.Key._Dim5,
                                 _Amount = g.Sum(d => d._Amount)
                             }).ToList();

            if (aggregateList != null)
            {
                var companyID = api.CompanyId;
                foreach (var item in aggregateList)
                {
                    item.SetMaster(api.CompanyEntity);
                }
                dgSimulatedDebtorBudgetLine.ItemsSource = aggregateList;
            }
        }
    }
}
