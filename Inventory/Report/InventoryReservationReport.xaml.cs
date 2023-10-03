using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvReservationReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvReservationClient); } }
        public override IComparer GridSorting { get { return new InvReservationSort(); } }
        public override bool Readonly { get { return true; } }
    }

    public partial class InventoryReservationReport : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryReservationReport; } }

        public InventoryReservationReport(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        public InventoryReservationReport(UnicontaBaseEntity sourcedata) : base(sourcedata)
        {
            Init(sourcedata);
        }

        public InventoryReservationReport(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Init(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvReservationReportGrid.UpdateMaster(args);
            BindGrid();
            SetHeader();
        }

        Uniconta.DataModel.InvItem lastItem;

        void SetHeader()
        {
            var masterRecord = dgInvReservationReportGrid.masterRecord;
            Uniconta.DataModel.InvItem itemRec = null;
            string Item = null;

            var storage = masterRecord as Uniconta.DataModel.InvItemStorage;
            if (storage != null)
                Item = storage._Item;
            else
            {
                var invItem = masterRecord as Uniconta.DataModel.InvItem;
                if (invItem != null)
                    Item = invItem._Item;
            }

            if (Item != null)
            {
                var cache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.InvItem));
                if (cache != null)
                    itemRec = (Uniconta.DataModel.InvItem)cache.Get(Item);
                else
                {
                    api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.InvItem), api);
                    itemRec = null;
                }
            }

            if (itemRec != null && itemRec != lastItem)
            {
                lastItem = itemRec;
                string header = string.Format("{0}; {1}, {2}", Uniconta.ClientTools.Localization.lookup("Reservations"), itemRec._Item, itemRec._Name);
                SetHeader(header);
            }
        }
     
        private void Init(UnicontaBaseEntity sourcedata = null)
        {
            InitializeComponent();
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            dgInvReservationReportGrid.api = api;
            SetRibbonControl(localMenu, dgInvReservationReportGrid);
            dgInvReservationReportGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvReservationReportGrid.ShowTotalSummary();
            if (sourcedata != null)
                dgInvReservationReportGrid.UpdateMaster(sourcedata);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvReservationReportGrid.SelectedItem as InvReservationClient;
            switch (ActionType)
            {
                case "OpenReservationOrders":
                    if (selectedItem != null)
                    {
                        if (selectedItem._DCType == Uniconta.DataModel.OrderType.SalesOrder)
                            AddDockItem(TabControls.DebtorOrders, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("DebtorOrders"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("Reservations")));
                        else if (selectedItem._DCType == Uniconta.DataModel.OrderType.PurchaseOrder)
                            AddDockItem(TabControls.CreditorOrders, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("CreditorOrders"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("Reservations")));
                        else if (selectedItem._DCType == Uniconta.DataModel.OrderType.Production)
                            AddDockItem(TabControls.ProductionOrders, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("ProductionOrders"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("Reservations")));
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void BindGrid()
        {
            await Filter(null);
            dgInvReservationReportGrid.SelectedItem = null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();

            if (!api.CompanyEntity.Location || !api.CompanyEntity.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!api.CompanyEntity.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
        }
        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgInvReservationReportGrid.Filter(propValuePair);
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.InvWarehouse) });
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var invRes = dg.SelectedItem as InvReservationClient;
            if (invRes == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "OrderNumber")
            {
                switch (invRes._DCType)
                {
                    case OrderType.SalesOrder:
                        lookup.TableType = typeof(Uniconta.DataModel.DebtorOrder);
                        break;
                    case OrderType.PurchaseOrder:
                        lookup.TableType = typeof(Uniconta.DataModel.CreditorOrder);
                        break;
                    case OrderType.Offer:
                        lookup.TableType = typeof(Uniconta.DataModel.DebtorOffer);
                        break;
                    case OrderType.Production:
                        lookup.TableType = typeof(Uniconta.DataModel.ProductionOrder);
                        break;
                }
            }
            return lookup;
        }
    }
}
