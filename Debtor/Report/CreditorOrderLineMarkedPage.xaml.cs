using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorOrderLineMarkedPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CreditorOrderLineMarkedPage; } }
        DebtorOrderLineClient debtorOrderLine;
        long orderRefLine;
        public CreditorOrderLineMarkedPage(UnicontaBaseEntity orderLine) : base(null)
        {
            InitializeComponent();
            ((TableView)dgCreditorOrderLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            debtorOrderLine = orderLine as DebtorOrderLineClient;
            orderRefLine = debtorOrderLine.OrderRefLine;
            SetHeader();
            dgCreditorOrderLineGrid.api = api;
            localMenu.dataGrid = dgCreditorOrderLineGrid;
            SetRibbonControl(localMenu, dgCreditorOrderLineGrid);
            dgCreditorOrderLineGrid.BusyIndicator = busyIndicator;
            dgCreditorOrderLineGrid.Readonly = true;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        void SetHeader()
        {
            var itemName = ClientHelper.GetName(api.CompanyId, typeof(InvItem), debtorOrderLine._Item);
            string header = string.Format("{0}: {1}  {2}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), debtorOrderLine._Item, itemName);
            SetHeader(header);
        }

        public async override Task InitQuery()
        {
            if (debtorOrderLine._Item == null)
                return;

            busyIndicator.IsBusy = true;
            var pair = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements("Item", debtorOrderLine._Item, CompareOperator.Equal)
            };
            var lst = await api.Query<CreditorOrderLineClient>(pair);
            var orderRefLine = this.orderRefLine;
            foreach (var line in lst)
            {
                if (line._MarkingId == orderRefLine)
                {
                    line._marked = true;
                    break;
                }
            }
            dgCreditorOrderLineGrid.SetSource(lst);
            busyIndicator.IsBusy = false;
            dgCreditorOrderLineGrid.Visibility = Visibility.Visible;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Project)
            {
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
                Project.Visible = Project.ShowInColumnChooser = false;
            }
            if (!company.Storage)
                Storage.Visible = Storage.ShowInColumnChooser = false;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            if (!company.SerialBatchNumbers)
                SerieBatch.Visible = SerieBatch.ShowInColumnChooser = false;

            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorOrderLineGrid.SelectedItem as CreditorOrderLineClient;
            switch (ActionType)
            {
                case "Link":
                    if (selectedItem != null)
                        MarkOrderLineAgainstOL(selectedItem);
                    break;
                case "Unlink":
                    if (selectedItem != null)
                        MarkOrderLineAgainstOL();
                    break;
                case "Cancel":
                    dockCtrl.CloseDockItem();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void MarkOrderLineAgainstOL(CreditorOrderLineClient selectedItem = null)
        {
            OrderAPI orderApi = new OrderAPI(api);
            busyIndicator.IsBusy = true;
            var err = await orderApi.MarkedOrderLineAgainstOrderLine(debtorOrderLine, selectedItem);
            busyIndicator.IsBusy = false;
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            else
                dockCtrl.CloseDockItem();
        }
    }
}
