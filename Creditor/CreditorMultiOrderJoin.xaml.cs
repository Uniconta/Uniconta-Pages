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
using UnicontaClient.Utilities;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorMultiOrderJoin : GridBasePage
    {
        public override string NameOfControl { get { return UnicontaTabs.CreditorMultiOrderJoin; } }
        public override bool IsDataChaged { get { return false; } }
        public CreditorMultiOrderJoin(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            dgJoinMultiPurchaseGrid.Readonly = true;
            dgJoinMultiPurchaseGrid.api = api;
            dgJoinMultiPurchaseGrid.BusyIndicator = busyIndicator;
            dgJoinMultiPurchaseGrid.Readonly = false;
            SetRibbonControl(localMenu, dgJoinMultiPurchaseGrid);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadNow(typeof(Uniconta.DataModel.Creditor));
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }
      
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgJoinMultiPurchaseGrid.SelectedItem as CreditorOrderClient;
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
                    dgJoinMultiPurchaseGrid.DeleteRow();
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    if (dgJoinMultiPurchaseGrid.masterRecords != null)
                    {
                        object[] arr = new object[2] { selectedItem, dgJoinMultiPurchaseGrid.masterRecord };
                        AddDockItem(TabControls.CreditorOrdersPage2, arr, salesHeader);
                    }
                    else
                        AddDockItem(TabControls.CreditorOrdersPage2, selectedItem, salesHeader);
                    break;
                case "OrderLine":
                    if (selectedItem == null)
                        return;
                    var olheader = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("PurchaseLine"), selectedItem._OrderNumber, selectedItem.Name);
                    AddDockItem(TabControls.CreditorOrderLines, dgJoinMultiPurchaseGrid.syncEntity, olheader);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void JoinToSelectedItem(CreditorOrderClient selectedItem)
        {
            var ordersToBeJoined = dgJoinMultiPurchaseGrid.GetVisibleRows() as IEnumerable<CreditorOrderClient>;
            var acc = selectedItem._DCAccount;
            string acc2 = null;
            foreach (var rec in ordersToBeJoined)
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

        private void JoinAllOrdersToSelectedItem(CreditorOrderClient selectedItem, IEnumerable<CreditorOrderClient> allOrderList)
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
                        if (order.OrderNumber == selectedItem.OrderNumber)
                            continue;
                        var result = await ordersApi.JoinTwoOrders(order, selectedItem);
                        if (result != Uniconta.Common.ErrorCodes.Succes)
                            errors.Add(order.OrderNumber);
                    }
                    if (errors.Count > 0)
                    {
                        var failedOrderNumbers = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("OrderNumber"), string.Join(",", errors));
                        var message = string.Format(Uniconta.ClientTools.Localization.lookup("FailedJoinOBJ"), failedOrderNumbers, selectedItem.OrderNumber);
                       UnicontaMessageBox.Show(message, Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    }
                    var propValpair = Uniconta.Common.PropValuePair.GenereteWhereElements("OrderNumber", typeof(int), selectedItem.OrderNumber.ToString());
                    await dgJoinMultiPurchaseGrid.Filter(new List<Uniconta.Common.PropValuePair>() { propValpair });
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
