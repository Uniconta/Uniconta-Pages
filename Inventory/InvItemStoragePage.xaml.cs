using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.DataModel;
using System.Windows;
using DevExpress.Xpf.Grid;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvItemStorageClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvItemStorageClientLocal); } }
        public override IComparer GridSorting { get { return new InvItemStorageClientSort(); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool Readonly { get { return readOnly; } }

        internal bool readOnly;
    }

    public partial class InvItemStoragePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvItemStoragePage; } }

        SQLCache items, warehouse;

        public InvItemStoragePage(UnicontaBaseEntity sourcedata) : base(sourcedata)
        {
            Init(sourcedata);
            RemoveMenuItem();
        }
        public InvItemStoragePage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Init(syncEntity.Row);
            RemoveMenuItem();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvItemStorageClientGrid.UpdateMaster(args);
            BindGrid();
            SetHeader();
        }
        public InvItemStoragePage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (this.syncEntity == null);
            Item.Visible = showFields;
            ItemName.Visible = showFields;
        }

        Uniconta.DataModel.InvItem lastItem;

        void SetHeader()
        {
            var masterRecord = dgInvItemStorageClientGrid.masterRecord;
            Uniconta.DataModel.InvItem itemRec = null;
            string Item = null;

            var storage = masterRecord as Uniconta.DataModel.InvItemStorage;
            if (storage != null)
                Item = storage._Item;
            else
            {
                var orderline = masterRecord as Uniconta.DataModel.DCOrderLine;
                if (orderline != null)
                    Item = orderline._Item;
                else
                {
                    var jourline = masterRecord as Uniconta.DataModel.InvJournalLine;
                    if (jourline != null)
                        Item = jourline._Item;
                    else
                    {
                        var invtran = masterRecord as Uniconta.DataModel.InvTrans;
                        if (invtran != null)
                            Item = invtran._Item;
                        else
                        {
                            var invBom = masterRecord as Uniconta.DataModel.InvBOM;
                            if (invBom != null)
                                Item = invBom._ItemPart;
                            else
                            {
                                var invSerieBatch = masterRecord as Uniconta.DataModel.InvSerieBatch;
                                if (invSerieBatch != null)
                                    Item = invSerieBatch._Item;
                                else
                                    itemRec = masterRecord as Uniconta.DataModel.InvItem;
                            }
                        }
                    }
                }
            }

            if (Item != null)
            {
                var cache = api.GetCache(typeof(Uniconta.DataModel.InvItem));
                if (cache != null)
                    itemRec = (Uniconta.DataModel.InvItem)cache.Get(Item);
                else
                {
                    api.LoadCache(typeof(Uniconta.DataModel.InvItem));
                    itemRec = null;
                }
            }

            if (itemRec != null && itemRec != lastItem)
            {
                lastItem = itemRec;
                string header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("Storage"), itemRec._Item, itemRec._Name);
                SetHeader(header);

                // here we could set number of decimals. itemRec._Decimals
            }
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "Filter", "ClearFilter" });
            SetHeader();
        }

        private void Init(UnicontaBaseEntity sourcedata = null)
        {
            InitializeComponent();
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            ((TableView)dgInvItemStorageClientGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            SetRibbonControl(localMenu, dgInvItemStorageClientGrid);
            dgInvItemStorageClientGrid.api = api;
            dgInvItemStorageClientGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvItemStorageClientGrid.ShowTotalSummary();
            if (sourcedata != null)
            {
                dgInvItemStorageClientGrid.UpdateMaster(sourcedata);
                dgInvItemStorageClientGrid.readOnly = ! (sourcedata is InvItem);
            }
            else
                dgInvItemStorageClientGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            this.items = api.GetCache(typeof(InvItem));
            this.warehouse = api.GetCache(typeof(InvWarehouse));
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvItemStorageClientLocal;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvItemStorageClientGrid_PropertyChanged;
            var selectedItem = e.NewItem as InvItemStorageClientLocal;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvItemStorageClientGrid_PropertyChanged;
        }

        private void InvItemStorageClientGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvItemStorageClientLocal;
            if (rec.CompanyId == 0)
                rec.SetMaster(api.CompanyEntity);
            switch (e.PropertyName)
            {
                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
            }
        }

        async void setLocation(InvWarehouse master, InvItemStorageClientLocal rec)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                    rec.locationSource = master.Locations ?? await master.LoadLocations(api);
                else
                {
                    rec.locationSource = null;
                    rec.Location = null;
                }
                rec.NotifyPropertyChanged("LocationSource");
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            if (api.CompanyEntity.Warehouse && this.warehouse == null)
                this.warehouse = await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvItemStorageClientGrid.SelectedItem as InvItemStorageClientLocal;

            switch (ActionType)
            {
                case "AddRow":
                    dgInvItemStorageClientGrid.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        dgInvItemStorageClientGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgInvItemStorageClientGrid.SaveData();
                    break;
                case "OrderLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOrderLineReport, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("OrderLines"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("OnHand")));
                    break;
                case "PurchaseLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("OnHand")));
                    break;
                case "InvReservation":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryReservationReport, dgInvItemStorageClientGrid.syncEntity, Uniconta.ClientTools.Localization.lookup("Reservations"));
                    break;
                case "InvStockProfile":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvStorageProfileReport, dgInvItemStorageClientGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), selectedItem._Item));
                    break;
                case "SetWarehouse":
                    SetWarehouse();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        public void SetWarehouse()
        {
            var selected = dgInvItemStorageClientGrid.SelectedItem as Uniconta.DataModel.InvItemStorage;
            if (selected != null)
                globalEvents.OnRefresh(TabControls.InvItemStoragePage, selected);
        }
        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgInvItemStorageClientGrid.Filter(propValuePair);
        }
        private async void BindGrid()
        {
            await Filter(null);
            dgInvItemStorageClientGrid.SelectedItem = null;
        }
        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (warehouse == null)
                return;

            var selectedItem = dgInvItemStorageClientGrid.SelectedItem as InvItemStorageClientLocal;
            if (selectedItem != null && selectedItem._Warehouse != null && warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }
    }

    public class InvItemStorageClientLocal : InvItemStorageClient
    {
        internal object locationSource;
        public object LocationSource { get { return locationSource; } }
    }
}
