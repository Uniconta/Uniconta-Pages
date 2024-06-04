using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using DevExpress.Xpf.Grid;
using DevExpress.Data;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvTransactionMarkedGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvTransMarkedLocalGrid); } }
    }

    public class InvTransMarkedLocalGrid : InvTransClient
    {
        internal bool _marked;
        [Display(Name = "Marked", ResourceType = typeof(InvTransText))]
        public bool Marked { get { return _marked; }  }
    }

    public partial class InventoryTransactionsMarkedPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryTransactionsMarkedPage; } }
        long costRefTrans;
        string Item, dcaccount;
        byte DCType;
        double qty;
        DCOrderLineClient debtorOrderLine;
        InvTrans invtrans;
        public InventoryTransactionsMarkedPage(UnicontaBaseEntity orderLine) : base(null)
        {
            this.DataContext = this;
            InitializeComponent();
            debtorOrderLine = orderLine as DCOrderLineClient;
            if (debtorOrderLine != null)
            {
                Item = debtorOrderLine._Item;
                DCType = debtorOrderLine.__DCType();
                this.qty = debtorOrderLine._Qty;
                costRefTrans = debtorOrderLine.CostRefTrans;
                if (DCType == 2)
                    dcaccount = ((CreditorOrderLineClient)debtorOrderLine).Order?._DCAccount;
                else if (DCType == 4)
                    dcaccount = ((ProductionOrderLineClient)debtorOrderLine).Production?._DCAccount;
                else
                    dcaccount = ((DebtorOrderLineClient)debtorOrderLine).Order?._DCAccount;
            }
            else
            {
                invtrans = orderLine as InvTrans;
                if (invtrans != null)
                {
                    Item = invtrans._Item;
                    DCType = invtrans._MovementType;
                    this.qty = invtrans._Qty;
                    dcaccount = invtrans._DCAccount;
                    if (DCType != 2 && this.qty < 0)
                        this.qty = -this.qty;
                }
            }
            SetHeader();
            dgInvTransGrid.api = api;
            localMenu.dataGrid = dgInvTransGrid;
            SetRibbonControl(localMenu, dgInvTransGrid);
            dgInvTransGrid.BusyIndicator = busyIndicator;
            dgInvTransGrid.Readonly = true;
            dgInvTransGrid.CustomSummary += DgInvTransGrid_CustomSummary;
            dgInvTransGrid.ShowTotalSummary();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }
        double sumMargin, sumSales, sumMarginRatio;
        private void DgInvTransGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as InvTransClient;
                    sumSales += row.SalesPrice;
                    sumMargin += row.Margin;
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        sumMarginRatio = 100 * sumMargin / sumSales;
                        e.TotalValue = sumMarginRatio;
                    }
                    break;
            }
        }
        void SetHeader()
        {
            var itemName = ClientHelper.GetName(api.CompanyId, typeof(InvItem), Item);
            string header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), Item, itemName);
            SetHeader(header);
        }

        async public override Task InitQuery()
        {
            var dctype = DCType;

            busyIndicator.IsBusy = true;
            var pair = new List<PropValuePair>()
            {
                PropValuePair.GenereteWhereElements("Item", Item, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("Qty", 0d, dctype == 2 || qty > 0d ? CompareOperator.GreaterThan : CompareOperator.LessThan),
                PropValuePair.GenereteWhereElements("Date", DateTime.Now.Date.AddYears(-1), CompareOperator.GreaterThanOrEqual),
            };
            if (dctype == 2 || qty < 0) // creditnota
            {
                pair.Add(PropValuePair.GenereteWhereElements("MovementType", Convert.ToString(dctype), CompareOperator.Equal));
                if (dcaccount != null)
                    pair.Add(PropValuePair.GenereteWhereElements("DCAccount", dcaccount, CompareOperator.Equal));
            }

            var lst = await api.Query<InvTransMarkedLocalGrid>(pair);
            var costRefTrans = this.costRefTrans;
            foreach (var line in lst)
            {
                if (line._MarkingId == costRefTrans)
                {
                    line._marked = true;
                    break;
                }
            }
            dgInvTransGrid.SetSource(lst);
            busyIndicator.IsBusy = false;
            dgInvTransGrid.Visibility = Visibility.Visible;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!company.Project)
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
           CostPrice.Visible = CostPrice.ShowInColumnChooser = CostValue.Visible = CostValue.ShowInColumnChooser = !api.CompanyEntity.HideCostPrice;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvTransGrid.SelectedItem as InvTransClient;
            switch (ActionType)
            {
                case "Link":
                    if (selectedItem != null)
                        MarkOrderLineAgainstIT(selectedItem);
                    break;
                case "Unlink":
                    if (selectedItem != null)
                        MarkOrderLineAgainstIT(null);
                    break;
                case "Cancel":
                    CloseDockItem();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void MarkOrderLineAgainstIT(InvTransClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            Task<ErrorCodes> t;
            if (debtorOrderLine != null)
                t = (new Uniconta.API.DebtorCreditor.OrderAPI(api)).MarkedOrderLineAgainstInvTrans(debtorOrderLine, selectedItem);
            else
                t = (new Uniconta.API.Inventory.TransactionsAPI(api)).MarkInvTransAgainstInvTrans(invtrans, selectedItem);
            var err = await t;
            busyIndicator.IsBusy = false;
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            else
               CloseDockItem();
        }
    }
}
