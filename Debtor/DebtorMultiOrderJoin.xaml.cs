using UnicontaClient.Utilities;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.DataModel;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools.Controls;
using System.Windows;
using UnicontaClient.Pages.Maintenance;
using UnicontaClient.Models;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for MultiJoinOrders.xaml
    /// </summary>
    public partial class DebtorMultiOrderJoin : GridBasePage
    {
        public override string NameOfControl { get { return UnicontaTabs.DebtorMultiOrderJoin; } }
        public override bool IsDataChaged { get { return false; } }
        public DebtorMultiOrderJoin(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            dgJoinMultiOrderGrid.Readonly = true;
            dgJoinMultiOrderGrid.api = api;
            dgJoinMultiOrderGrid.BusyIndicator = busyIndicator;
            dgJoinMultiOrderGrid.Readonly = false;
            SetRibbonControl(localMenu, dgJoinMultiOrderGrid);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }
        protected override void LoadCacheInBackGround()
        {
            LoadNow(typeof(Debtor));
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgJoinMultiOrderGrid.SelectedItem as DebtorOrderClient;
            string salesHeader = string.Empty;
            if (selectedItem != null)
                salesHeader = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Orders"), selectedItem._OrderNumber);
            switch (ActionType)
            {
                case "JoinOrders":
                    if (selectedItem != null)
                        JoinToSelectedItem(selectedItem);
                    break;
                case "RemoveOrder":
                    dgJoinMultiOrderGrid.DeleteRow();
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    if (dgJoinMultiOrderGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgJoinMultiOrderGrid.masterRecord };
                        AddDockItem(TabControls.DebtorOrdersPage2, arr, salesHeader);
                    }
                    else
                        AddDockItem(TabControls.DebtorOrdersPage2, selectedItem, salesHeader);
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), selectedItem._DCAccount, selectedItem._OrderNumber);
                    AddDockItem(TabControls.DebtorOrderLines, dgJoinMultiOrderGrid.syncEntity, olheader);
                    break;
                case "JoinManyOrders":
                        JoinOrdersPerCustomer();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void JoinOrdersPerCustomer()
        {
            var ordersToBeJoined = dgJoinMultiOrderGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
            if (ordersToBeJoined == null || ordersToBeJoined.Count() == 0)
                return;
            var allOrderList = ordersToBeJoined.GroupBy(x => x.Account).Select(x => x.First()).ToList();
            EraseYearWindow EraseYearWindowDialog = new EraseYearWindow("", true);
            EraseYearWindowDialog.Closed += async delegate
            {
                if (EraseYearWindowDialog.DialogResult == true)
                {
                    var ordersApi = new OrderAPI(api);
                    List<int> errors = new List<int>();
                    List<int> orders = new List<int>();
                    foreach (var order in allOrderList)
                    {
                        foreach (var customerOrder in ordersToBeJoined)
                        {
                            if (order.Account == customerOrder.Account)
                            {
                                if (order._OrderNumber == customerOrder._OrderNumber)
                                    continue;
                                var result = await ordersApi.JoinTwoOrders(customerOrder, order);
                                if (result != Uniconta.Common.ErrorCodes.Succes)
                                {
                                    errors.Add(customerOrder._OrderNumber);
                                    orders.Add(order._OrderNumber);
                                }
                            }
                        }
                    }
                    if (errors.Count > 0)
                    {
                        var failedOrderNumbers = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("OrderNumber"), string.Join(",", errors));
                        var message = string.Format(Uniconta.ClientTools.Localization.lookup("FailedJoinOBJ"), failedOrderNumbers, string.Join(",", orders));
                        UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    }
                    InitQuery();
                }
            };
            EraseYearWindowDialog.Show();
        }

        private void JoinToSelectedItem(DebtorOrderClient selectedItem)
        {
            var ordersToBeJoined = dgJoinMultiOrderGrid.GetVisibleRows() as IEnumerable<DebtorOrderClient>;
            var acc = selectedItem._DCAccount;
            string acc2 = null;
            foreach(var rec in ordersToBeJoined)
            {
                if (rec._DCAccount != acc)
                {
                    acc2 = rec._DCAccount;
                    break;
                }
            }
            if (acc2 != null)
            {
                string msg = string.Format("{0}\r\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DifferentAccountMessage"), acc, acc2),
                    Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"));
                var confirmationDialog = new CWConfirmationBox(msg, Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
                confirmationDialog.Closing += delegate
                {
                    if (confirmationDialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                    {
                        EraseYearWindow EraseYearWindowDialog = new EraseYearWindow("", true);
                        EraseYearWindowDialog.Closed += delegate
                        {
                            if (EraseYearWindowDialog.DialogResult == true)
                            {
                                JoinAllOrdersToSelectedItem(selectedItem, ordersToBeJoined);
                            }
                        };
                        EraseYearWindowDialog.Show();
                    }
                };
                confirmationDialog.Show();
            }
            else
                JoinAllOrdersToSelectedItem(selectedItem, ordersToBeJoined);
        }

        private void JoinAllOrdersToSelectedItem(DebtorOrderClient selectedItem, IEnumerable<DebtorOrderClient> allOrderList)
        {
            EraseYearWindow EraseYearWindowDialog = new EraseYearWindow("", true);
            EraseYearWindowDialog.Closed += async delegate
            {
                if (EraseYearWindowDialog.DialogResult == true)
                {
                    var ordersApi = new OrderAPI(api);
                    List<int> errors = new List<int>();
                    foreach (var order in allOrderList)
                    {
                        if (order._OrderNumber == selectedItem._OrderNumber)
                            continue;
                        var result = await ordersApi.JoinTwoOrders(order, selectedItem);
                        if (result != Uniconta.Common.ErrorCodes.Succes)
                            errors.Add(order._OrderNumber);
                    }
                    if (errors.Count > 0)
                    {
                        var failedOrderNumbers = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("OrderNumber"), string.Join(",", errors));
                        var message = string.Format(Uniconta.ClientTools.Localization.lookup("FailedJoinOBJ"), failedOrderNumbers, selectedItem._OrderNumber);
                        UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    }
                    var propValpair = Uniconta.Common.PropValuePair.GenereteWhereElements("OrderNumber", typeof(int), Convert.ToString(selectedItem._OrderNumber));
                    dgJoinMultiOrderGrid.Filter(new Uniconta.Common.PropValuePair[] { propValpair });
                }
            };
            EraseYearWindowDialog.Show();
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
