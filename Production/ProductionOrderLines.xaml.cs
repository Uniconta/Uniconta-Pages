using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
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
using System.Windows.Threading;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
#if !SILVERLIGHT
using UnicontaClient.Pages;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProductionOrderLineGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get
            {
                return typeof(ProductionOrderLineClient);
            }
        }

        public override bool SingleBufferUpdate { get { return false; } }
        public override IComparer GridSorting { get { return new DCOrderLineSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (DCOrderLine)this.SelectedItem;
            return (selectedItem != null) && (selectedItem._Item != null || selectedItem._Text != null);
        }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            var row = copyFromRows.FirstOrDefault();
            var type = this.TableTypeUser;
            if (row is InvTrans)
            {
                var lst = new List<ProductionOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (InvTrans)_it;
                    var line = Activator.CreateInstance(type) as ProductionOrderLineClient;
                    line._Qty = it.MovementTypeEnum == InvMovementType.Debtor ? -it._Qty : it._Qty;
                    line._AmountEntered = it.MovementTypeEnum == InvMovementType.Debtor ? -it._AmountEntered : it._AmountEntered;
                    line._Item = it._Item;
                    line._Text = it._Text;
                    line._Price = it._Price;
                    line._DiscountPct = it._DiscountPct;
                    line._Variant1 = it._Variant1;
                    line._Variant2 = it._Variant2;
                    line._Variant3 = it._Variant3;
                    line._Variant4 = it._Variant4;
                    line._Variant5 = it._Variant5;
                    line._Discount = it._Discount;
                    line._Warehouse = it._Warehouse;
                    line._Location = it._Location;
                    line._Unit = it._Unit;
                    line._Date = it._Date;
                    line._Week = it._Week;
                    line._Note = it._Note;
                    lst.Add(line);
                }
                return lst;
            }
            if (row is DCOrderLine)
            {
                var lst = new List<ProductionOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (DCOrderLine)_it;
                    var line = Activator.CreateInstance(type) as ProductionOrderLineClient;
                    line._Qty = it._Qty;
                    line._Item = it._Item;
                    line._Text = it._Text;
                    line._Price = it._Price;
                    line._AmountEntered = it._AmountEntered;
                    line._DiscountPct = it._DiscountPct;
                    line._Variant1 = it._Variant1;
                    line._Variant2 = it._Variant2;
                    line._Variant3 = it._Variant3;
                    line._Variant4 = it._Variant4;
                    line._Variant5 = it._Variant5;
                    line._Discount = it._Discount;
                    line._Warehouse = it._Warehouse;
                    line._Location = it._Location;
                    line._Unit = it._Unit;
                    line._Date = it._Date;
                    line._Week = it._Week;
                    line._Note = it._Note;
                    lst.Add(line);
                }
                return lst;
            }
            return null;
        }
        public override bool ClearSelectedItemOnSave
        {
            get
            {
                return false;
            }
        }
    }
    public partial class ProductionOrderLines : GridBasePage
    {
        SQLCache items, warehouse;
        ProductionOrder Order { get { return dgProductionOrderLineGrid.masterRecord as ProductionOrder; } }

        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, Order);
            base.PageClosing();
        }

        public override string NameOfControl
        {
            get { return TabControls.ProductionOrderLines.ToString(); }
        }

        public ProductionOrderLines(UnicontaBaseEntity master)
           : base(master)
        {
            Init(master);
        }

        public ProductionOrderLines(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }

        public void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            ((TableView)dgProductionOrderLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            ((TableView)dgInvItemStorageClientGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgProductionOrderLineGrid;
            SetRibbonControl(localMenu, dgProductionOrderLineGrid);
            dgProductionOrderLineGrid.api = api;
            dgInvItemStorageClientGrid.api = api;
            dgInvItemStorageClientGrid.ShowTotalSummary();
            SetupMaster(master);
            dgProductionOrderLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProductionOrderLineGrid.SelectedItemChanged += DgProductionOrderLineGrid_SelectedItemChanged;

            layOutDebtorOrderLine.Caption = Uniconta.ClientTools.Localization.lookup("OrdersLine");
            layOutInvItemStorage.Caption = Uniconta.ClientTools.Localization.lookup("OnHand");
            OnHandScreenInOrder = api.CompanyEntity._OnHandScreenInOrder;
            layOutInvItemStorage.Visibility = OnHandScreenInOrder ? Visibility.Visible : Visibility.Collapsed;
            InitialLoad();
        }
        public async override Task InitQuery()
        {
            await base.InitQuery();
            var itemSource = (IList)dgProductionOrderLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgProductionOrderLineGrid.AddFirstRow();
        }
        private void InitialLoad()
        {
            var Comp = api.CompanyEntity;
            this.items = Comp.GetCache(typeof(InvItem));
            this.warehouse = Comp.GetCache(typeof(InvWarehouse));
            if (dgProductionOrderLineGrid.IsLoadedFromLayoutSaved)
            {
                dgProductionOrderLineGrid.ClearSorting();
                dgProductionOrderLineGrid.IsLoadedFromLayoutSaved = false;
            }
            if (Comp._Variant1 != null && Comp.ItemVariants)
            {
                colVariant1.Header = Comp._Variant1;
                colInvItemVariant1.Header = Comp._Variant1;
            }
            else
            {
                colVariant1.Visible = colVariant1.ShowInColumnChooser = false;
                colInvItemVariant1.Visible = colVariant1.ShowInColumnChooser = false;
            }

            if (Comp._Variant2 != null && Comp.ItemVariants)
            {
                colVariant2.Header = Comp._Variant2;
                colInvItemVariant2.Header = Comp._Variant2;
            }
            else
            {
                colVariant2.Visible = colVariant2.ShowInColumnChooser = false;
                colInvItemVariant2.Visible = colVariant2.ShowInColumnChooser = false;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InvItemStoragePage && argument != null)
            {
                var storeloc = argument as InvItemStorageClient;
                if (storeloc == null) return;
                var selected = dgProductionOrderLineGrid.SelectedItem as DCOrderLineClient;
                if (selected != null && (selected.Warehouse != storeloc.Warehouse || selected.Location != storeloc.Location))
                {
                    dgProductionOrderLineGrid.SetLoadedRow(selected);
                    selected.Warehouse = storeloc.Warehouse;
                    selected.Location = storeloc.Location;
                    dgProductionOrderLineGrid.SetModifiedRow(selected);
                    this.DataChaged = true;
                }
            }
        }

        private void DgProductionOrderLineGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as ProductionOrderLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= ProductionOrderLineGrid_PropertyChanged;
            var selectedItem = e.NewItem as ProductionOrderLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += ProductionOrderLineGrid_PropertyChanged;
                if (addingRow && selectedItem._Item != null)
                    return;
                else
                    LoadInvItemStorageGrid(selectedItem);
                addingRow = false;
            }
        }

        private void LoadInvItemStorageGrid(ProductionOrderLineClient selectedRow)
        {
            if (!OnHandScreenInOrder || selectedRow == null)
                return;
            if (selectedRow._Item == null)
                dgInvItemStorageClientGrid.ItemsSource = null;
            else
            {
                var itm = (Uniconta.DataModel.InvItem)items?.Get(selectedRow._Item);
                if (itm != null && itm._ItemType == (byte)ItemType.Service)
                    dgInvItemStorageClientGrid.ItemsSource = null;
                else
                {
                    dgInvItemStorageClientGrid.UpdateMaster(selectedRow);
                    dgInvItemStorageClientGrid.Filter(null);
                }
            }
        }

        private void ProductionOrderLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as ProductionOrderLineClient;
            switch (e.PropertyName)
            {
                case "Item":
                    if (items != null)
                    {
                        var selectedItem = (InvItem)items.Get(rec._Item);
                        if (selectedItem != null)
                        {
                            if (selectedItem._AlternativeItem != null && selectedItem._UseAlternative == UseAlternativeItem.Always)
                            {
                                var altItem = (InvItem)items.Get(selectedItem._AlternativeItem);
                                if (altItem != null && altItem._AlternativeItem == null)
                                {
                                    rec.Item = selectedItem._AlternativeItem;
                                    return;
                                }
                            }

                            rec.SetItemValues(selectedItem, api.CompanyEntity._OrderLineStorage);
                            rec.SetCostFromItem(selectedItem);
                            rec.Price = selectedItem._CostPrice;

                            double Qty;
                            if (selectedItem._SalesQty != 0d)
                                Qty = selectedItem._SalesQty;
                            else if (api.CompanyEntity._OrderLineOne)
                                Qty = 1d;
                            else
                                Qty = 0d;
                            if (Order._ProdQty != 0d)
                                Qty *= Order._ProdQty;

                            rec.Qty = Qty;

                            LoadInvItemStorageGrid(rec);
                            TableField.SetUserFieldsFromRecord(selectedItem, rec);
                            if (selectedItem._Blocked)
                                UtilDisplay.ShowErrorCode(ErrorCodes.ItemIsOnHold, null);

                            globalEvents?.NotifyRefreshViewer(NameOfControl, selectedItem);
                        }
                    }
                    break;
                case "QtyDelivered":
                    if (rec._QtyDelivered > rec._Qty)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ValueMayNoBeGreater"), Uniconta.ClientTools.Localization.lookup("Consumed"), Uniconta.ClientTools.Localization.lookup("Qty")),
                            Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        rec.QtyDelivered = rec._Qty;
                    }
                    break;
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
                case "EAN":
                    UnicontaClient.Pages.DebtorOfferLines.FindOnEAN(rec, this.items, api);
                    break;
            }
        }

        async void setLocation(InvWarehouse master, ProductionOrderLineClient rec)
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

        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (warehouse == null)
                return;

            var selectedItem = dgProductionOrderLineGrid.SelectedItem as ProductionOrderLineClient;
            if (selectedItem?._Warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }

        public bool DataChaged;
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            SetupMaster(args);
            SetHeader();
            InitQuery();
        }

        bool OnHandScreenInOrder;
        void SetupMaster(UnicontaBaseEntity args)
        {
            dgProductionOrderLineGrid.UpdateMaster(args);
        }

        void SetHeader()
        {
            var syncMaster = Order;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ProductionLine"), syncMaster._OrderNumber, syncMaster._DCAccount);
            if (header != null)
                SetHeader(header);
        }

        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgProductionOrderLineGrid);
            gridCtrls.Add(dgInvItemStorageClientGrid);
        }

        bool addingRow;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProductionOrderLineGrid.SelectedItem as ProductionOrderLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    addingRow = true;
                    dgProductionOrderLineGrid.AddRow();
                    break;
                case "CopyRow":
                    dgProductionOrderLineGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGridLocal();
                    break;
                case "DeleteRow":
                    dgProductionOrderLineGrid.DeleteRow();
                    break;
                case "Storage":
                    ViewStorage();
                    break;
                case "Serial":
                    if (selectedItem?._Item != null)
                        LinkSerialNumber(selectedItem);
                    break;
                case "UnfoldBOM":
                    if (selectedItem?._Item != null)
                        UnfoldBOM(selectedItem);
                    break;
                case "CreateProduction":
                    if (selectedItem != null)
                        CreateProductionOrder(selectedItem);
                    break;
                case "MarkOrderLine":
                    if (selectedItem?._Item != null)
                        MarkedOrderLine(selectedItem);
                    break;
                case "MarkInvTrans":
                    if (selectedItem?._Item != null)
                        MarkedInvTrans(selectedItem);
                    break;
                case "MarkOrderLineAgnstOL":
                    if (selectedItem?._Item == null) return;
                    saveGridLocal();
                    AddDockItem(TabControls.CreditorOrderLineMarkedPage, new object[] { selectedItem }, true);
                    break;
                case "MarkOrderLineAgnstInvTrans":
                    if (selectedItem?._Item == null) return;
                    saveGridLocal();
                    AddDockItem(TabControls.InventoryTransactionsMarkedPage, new object[] { selectedItem }, true);
                    break;
                case "AddNote":
                case "AddDoc":
                    if (selectedItem != null)
                        AddAttachment(ActionType, selectedItem);
                    break;
                case "StockLines":
                    if (selectedItem?.InvItem != null)
                        AddDockItem(TabControls.ProductionPostedTransGridPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), selectedItem._Item));
                    break;
                case "DebtorOrderLines":
                    if (selectedItem?.InvItem != null)
                        AddDockItem(TabControls.DebtorOrderLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), selectedItem._Item));
                    break;
                case "DebtorOfferLines":
                    if (selectedItem?.InvItem != null)
                        AddDockItem(TabControls.DebtorOfferLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OfferLine"), selectedItem._Item));
                    break;
                case "PurchaseOrderLines":
                    if (selectedItem?.InvItem != null)
                        AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem._Item));
                    break;
                case "ProductionOrderLines":
                    if (selectedItem?.InvItem != null)
                        AddDockItem(TabControls.ProductionOrderLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionLines"), selectedItem._Item));
                    break;
                case "InvStockProfile":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvStorageProfileReport, dgProductionOrderLineGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), selectedItem._Item));
                    break;
                case "ViewPhoto":
                    if (selectedItem?.InvItem != null && selectedItem?.Item != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem.InvItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem?.InvItem?._Name));
                    break;
                case "ViewNotes":
                    if (selectedItem?.InvItem != null && selectedItem?.Item != null)
                        AddDockItem(TabControls.UserNotesPage, selectedItem.InvItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Notes"), selectedItem?.InvItem?._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void AddAttachment(string actionType, ProductionOrderLineClient productionOrderLineClient)
        {
            var invBomResult = await api.Query<InvBOMClient>(productionOrderLineClient, new[] { PropValuePair.GenereteParameter("ItemPart", typeof(string), "1") });
            if (invBomResult != null && invBomResult.Length > 0)
                AddDockItem(actionType == "AddNote" ? TabControls.UserNotesPage : TabControls.UserDocsPage, invBomResult[0]);
        }

        async void CreateProductionOrder(ProductionOrderLineClient orderLine)
        {
            var t = saveGridLocal();
            if (t != null && orderLine.RowId == 0)
                await t;

            AddDockItem(TabControls.ProductionOrdersPage2, new object[2] { api, orderLine }, Uniconta.ClientTools.Localization.lookup("Production"), "Add_16x16.png");
        }

        async void UnfoldBOM(ProductionOrderLineClient selectedItem)
        {
            var items = this.items;
            var item = (InvItem)items.Get(selectedItem._Item);
            if (item == null || item._ItemType < (byte)ItemType.BOM)
                return;

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            var list = await api.Query<InvBOM>(selectedItem);

            if (list != null && list.Length > 0)
            {
                var type = dgProductionOrderLineGrid.TableTypeUser;
                var Qty = selectedItem._Qty;
                var lst = new List<UnicontaBaseEntity>(list.Length);
                foreach (var bom in list)
                {
                    var invJournalLine = Activator.CreateInstance(type) as ProductionOrderLineClient;
                    invJournalLine._Date = selectedItem._Date;
                    invJournalLine._Week = selectedItem._Week;
                    invJournalLine._Dim1 = selectedItem._Dim1;
                    invJournalLine._Dim2 = selectedItem._Dim2;
                    invJournalLine._Dim3 = selectedItem._Dim3;
                    invJournalLine._Dim4 = selectedItem._Dim4;
                    invJournalLine._Dim5 = selectedItem._Dim5;
                    invJournalLine._Item = bom._ItemPart;
                    invJournalLine._Variant1 = bom._Variant1;
                    invJournalLine._Variant2 = bom._Variant2;
                    invJournalLine._Variant3 = bom._Variant3;
                    invJournalLine._Variant4 = bom._Variant4;
                    invJournalLine._Variant5 = bom._Variant5;
                    item = (InvItem)items.Get(bom._ItemPart);
                    if (item == null && bom._ItemPart != null)
                    {
                        items = await api.LoadCache(typeof(InvItem), true);
                        item = (InvItem)items.Get(bom._ItemPart);
                    }
                    if (item != null)
                    {
                        invJournalLine._Warehouse = bom._Warehouse ?? item._Warehouse ?? selectedItem._Warehouse;
                        invJournalLine._Location = bom._Location ?? item._Location ?? selectedItem._Location;
                        invJournalLine._CostPriceLine = item._CostPrice;
                        invJournalLine.SetItemValues(item, selectedItem._Storage);
                        invJournalLine._Qty = Math.Round(bom.GetBOMQty(Qty), item._Decimals);
                        TableField.SetUserFieldsFromRecord(item, invJournalLine);
                    }
                    else
                        invJournalLine._Qty = Math.Round(bom.GetBOMQty(Qty), 2);
                    TableField.SetUserFieldsFromRecord(bom, invJournalLine);
                    lst.Add(invJournalLine);
                }
                busyIndicator.IsBusy = false;
                dgProductionOrderLineGrid.ReplaceCurrentRow(lst);
            }
            busyIndicator.IsBusy = false;
        }

        async void LinkSerialNumber(ProductionOrderLineClient orderLine)
        {
            var t = saveGridLocal();
            if (t != null && orderLine.RowId == 0)
                await t;
            if (api.CompanyEntity.Warehouse)
                dgProductionOrderLineGrid.SetLoadedRow(orderLine); // serial page add warehouse and location
            AddDockItem(TabControls.SerialToOrderLinePage, orderLine, string.Format("{0}:{1}/{2},{3}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), orderLine.OrderRowId, orderLine._Item, orderLine.RowId));
        }

        async void MarkedOrderLine(ProductionOrderLineClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            var orderLineMarked = new CreditorOrderLineClient();
            OrderAPI orderApi = new OrderAPI(api);
            var res = await orderApi.GetMarkedOrderLine(selectedItem, orderLineMarked);
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
                AddDockItem(TabControls.OrderLineMarkedPage, new object[] { api, orderLineMarked }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), orderLineMarked._OrderNumber));
            else
                UtilDisplay.ShowErrorCode(res);
        }

        async void MarkedInvTrans(ProductionOrderLineClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            var invTrans = new InvTransClient();
            OrderAPI orderApi = new OrderAPI(api);
            var res = await orderApi.GetMarkedInvTrans(selectedItem, invTrans);
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
                AddDockItem(TabControls.InvTransMarkedPage, new object[] { api, invTrans }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), invTrans._OrderNumber));
            else
                UtilDisplay.ShowErrorCode(res);
        }

        async void ViewStorage()
        {
            var savetask = saveGridLocal(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            AddDockItem(TabControls.InvItemStoragePage, dgProductionOrderLineGrid.syncEntity, true);
        }

        public override bool IsDataChaged { get { return DataChaged || base.IsDataChaged; } }

        Task saveGridLocal()
        {
            var orderLine = dgProductionOrderLineGrid.SelectedItem as ProductionOrderLineClient;
            refreshOnHand = orderLine != null && orderLine.RowId == 0;
            dgProductionOrderLineGrid.SelectedItem = null;
            dgProductionOrderLineGrid.SelectedItem = orderLine;
            if (dgProductionOrderLineGrid.HasUnsavedData)
                return saveGrid();
            return null;
        }

        bool refreshOnHand;
        protected override async Task<ErrorCodes> saveGrid()
        {
            var orderLine = dgProductionOrderLineGrid.SelectedItem as ProductionOrderLineClient;
            if (dgProductionOrderLineGrid.HasUnsavedData)
            {
                ErrorCodes res = await dgProductionOrderLineGrid.SaveData();
                if (res == ErrorCodes.Succes)
                {
                    DataChaged = false;
                    globalEvents.OnRefresh(NameOfControl, Order);
                    if (refreshOnHand)
                    {
                        refreshOnHand = false;
                        LoadInvItemStorageGrid(orderLine);
                    }
                }
                return res;
            }
            return 0;
        }

        private void btnPurchase_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvItemStorageClientGrid.SelectedItem as InvItemStorageClient;
            if (selectedItem != null)
                AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("OnHand")));
        }

        private void btnSales_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvItemStorageClientGrid.SelectedItem as InvItemStorageClient;
            if (selectedItem != null)
                AddDockItem(TabControls.DebtorOrderLineReport, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("OrderLines"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("OnHand")));
        }

        private void SerieBatch_GotFocus(object sender, RoutedEventArgs e)
        {
            var selItem = dgProductionOrderLineGrid.SelectedItem as ProductionOrderLineClient;
            if (string.IsNullOrEmpty(selItem?._Item))
                return;
            setSerieBatchSource(selItem);
        }

        async void setSerieBatchSource(ProductionOrderLineClient row)
        {
            var cache = api.GetCache(typeof(InvItem));
            var invItemMaster = cache.Get(row._Item) as InvItem;
            if (invItemMaster == null)
                return;
            if (row.SerieBatches != null && row.SerieBatches.First()._Item == row._Item)/*Bind if Item changed*/
                return;
            UnicontaBaseEntity master;
            if (row._Qty < 0)
            {
                master = invItemMaster;
            }
            else
            {
                // We only select opens
                var mast = new InvSerieBatchOpen();
                mast.SetMaster(invItemMaster);
                master = mast;
            }
            var res = await api.Query<SerialToOrderLineClient>(master);
            if (res != null && res.Length > 0)
            {
                row.SerieBatches = res;
                row.NotifyPropertyChanged("SerieBatches");
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            if (api.CompanyEntity.Warehouse && this.warehouse == null)
                this.warehouse = await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);
        }
    }
}
