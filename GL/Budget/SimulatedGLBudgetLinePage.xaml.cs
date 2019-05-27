using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using UnicontaClient.Controls.Dialogs;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SimulateGLBudgetLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLBudgetLineClient); } }
        public override bool CanInsert { get { return false; } }
        public override bool Readonly { get { return true; } }
    }
    public partial class SimulatedGLBudgetLinePage : GridBasePage
    {
        public SimulatedGLBudgetLinePage(object param) : base(null)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgSimulatedGLBudgetLine);
            localMenu.dataGrid = dgSimulatedGLBudgetLine;
            dgSimulatedGLBudgetLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgSimulatedGLBudgetLine.ItemsSource = param;
            dgSimulatedGLBudgetLine.Visibility = Visibility.Visible;
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                /*
                case "CopyRow":
                    CWCopyBudget cwCopyBudget = new CWCopyBudget(selectedItem.Name);
                    cwCopyBudget.Closing += async delegate
                    {
                        if (cwCopyBudget.DialogResult == true)
                        {
                            int AddMonth = cwCopyBudget.Months;
                            double PctFactor = cwCopyBudget.Pct;

                            double Factor = PctFactor != 0d ? (PctFactor + 100d) / 100d : 0d;

                            ErrorCodes err = ErrorCodes.NoSucces;
                            GLBudgetClient copyBudget = new GLBudgetClient();
                            copyBudget.Name = cwCopyBudget.BudgetName;
                            copyBudget.FromDate = AddMonth2Date(selectedItem._FromDate, AddMonth);
                            copyBudget.ToDate = AddMonth2Date(selectedItem._ToDate, AddMonth);
                            copyBudget.Comment = selectedItem._Comment;
                            copyBudget.BaseBudget = selectedItem._BaseBudget;
                            copyBudget._Active = selectedItem._Active;
                            busyIndicator.IsBusy = true;
                            err = await api.Insert(copyBudget);
                            if (err == ErrorCodes.Succes)
                            {
                                var budgetLines = dgSimulatedGLBudgetLine.GetVisibleRows() as IEnumerable<GLBudgetLineClient>;
                                if (budgetLines != null && budgetLines.Count() > 0)
                                {
                                    foreach (var line in budgetLines)
                                    {
                                        line._Date = AddMonth2Date(line._Date, AddMonth);
                                        line._ToDate = AddMonth2Date(line._ToDate, AddMonth);
                                        if (Factor != 0)
                                            line._Amount = Math.Round(line._Amount * Factor);
                                        line.SetMaster(copyBudget);
                                    }
                                    err = await api.Insert(budgetLines);
                                }
                            }
                            busyIndicator.IsBusy = false;
                            if (err != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(err);
                        }
                    };
                    cwCopyBudget.Show();
                    break;
                */
                case "Aggregate":
                    if (aggregateList != null )
                    {
                        dgSimulatedGLBudgetLine.ItemsSource = mainList;
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

        IList<GLBudgetLineClient> mainList;
        IList<GLBudgetLineClient> aggregateList;
        void aggregate()
        {
            if (mainList == null)
                mainList = (IList<GLBudgetLineClient>)dgSimulatedGLBudgetLine.ItemsSource;
            if (mainList == null || !mainList.Any())
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoDataCollected"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }

            aggregateList = (from rec in mainList
                   group rec by new { rec._Account, rec._Date, rec._Dim1, rec._Dim2, rec._Dim3, rec._Dim4, rec._Dim5 } into g
                   select new GLBudgetLineClient
                   {
                       _Date= g.Key._Date,
                       _Account=g.Key._Account,
                       _Dim1= g.Key._Dim1,
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
                dgSimulatedGLBudgetLine.ItemsSource = aggregateList;
            }
        }
    }
}
