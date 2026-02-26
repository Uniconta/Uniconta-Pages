using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.Grid;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Pages;
using UnicontaClient.Pages;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvTransferOrderLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvTransferOrderLineClient); } }
        public override IComparer GridSorting { get { return new DCOrderLineSort(); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort
        {
            get
            {
                return false;
            }
        }
        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (InvTransferOrderLineClient)this.SelectedItem;
            return (selectedItem != null) && (selectedItem._Item != null || selectedItem._Text != null);
        }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            var row = copyFromRows.FirstOrDefault();
            List<InvTransferOrderLineClient> lst = null;
            if (row is InvTrans)
            {
                lst = new List<InvTransferOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (InvTrans)_it;
                    lst.Add(CreateNewOrderLine(it._Item, it.MovementTypeEnum == InvMovementType.Debtor ? -it._Qty : it._Qty, it._Text, it._Price,
                        it.MovementTypeEnum == InvMovementType.Debtor ? -it._AmountEntered : it._AmountEntered, it._DiscountPct, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5,
                        it._Warehouse, it._Location, it._Unit, it._Date, it._Week, it._Note, it._Discount));
                }
            }
            else if (row is DCOrderLine)
            {
                lst = new List<InvTransferOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (DCOrderLine)_it;
                    var line = CreateNewOrderLine(it._Item, it._Qty, it._Text, it._Price, it._AmountEntered, it._DiscountPct, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5,
                        it._Warehouse, it._Location, it._Unit, it._Date, it._Week, it._Note, it._Discount);
                    TableField.SetUserFieldsFromRecord(it, line);
                    lst.Add(line);
                }
            }
            else if (row is InvItem)
            {
                var qProp = row.GetType().GetProperty("Quantity");
                if (qProp == null)
                    qProp = row.GetType().GetProperty("PurchaseQty");
                lst = new List<InvTransferOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    double qty = Convert.ToDouble(qProp?.GetValue(_it, null));
                    var it = (InvItem)_it;
                    lst.Add(CreateNewOrderLine(it._Item, qty, null, 0.0d, 0.0d, 0.0d, null, null, null, null, null, null, null, 0, DateTime.MinValue, 0, null, 0));
                }
            }
            return lst;
        }

        private InvTransferOrderLineClient CreateNewOrderLine(string item, double qty, string text, double price, double amountEntered, double discPct, string variant1, string variant2, string variant3, string variant4, string variant5, string warehouse,
            string location, ItemUnit unit, DateTime date, byte week, string note, double discount)
        {
            var type = this.TableTypeUser;
            var orderline = Activator.CreateInstance(type) as InvTransferOrderLineClient;
            orderline._Qty = qty;
            orderline._Item = item;
            orderline._Text = text;
            orderline._Price = price;
            orderline._AmountEntered = amountEntered;
            orderline._DiscountPct = discPct;
            orderline._Variant1 = variant1;
            orderline._Variant2 = variant2;
            orderline._Variant3 = variant3;
            orderline._Variant4 = variant4;
            orderline._Variant5 = variant5;
            orderline._Discount = discount;
            orderline._Warehouse = warehouse;
            orderline._Location = location;
            orderline._Unit = unit;
            orderline._Date = date;
            orderline._Week = week;
            orderline._Note = note;
            return orderline;
        }

        public bool allowSave = true;
        public override bool AllowSave { get { return allowSave; } }
    }

    public partial class InvTransferOrderLines : GridBasePage
    {
        SQLCache items, warehouse, standardVariants, variants1, variants2;
        public override string NameOfControl { get { return TabControls.InvTransferOrderLines; } }
        InvTransferOrderClient orderMaster { get { return dgInvTransferOrderLineGrid.masterRecord as InvTransferOrderClient; } }

        double exchangeRate;
        bool OnHandScreenInOrder;
        bool addingRow;
        public InvTransferOrderLines(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }
        public void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            ((TableView)dgInvTransferOrderLineGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            ((TableView)dgInvItemStorageClientGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            localMenu.dataGrid = dgInvTransferOrderLineGrid;
            SetRibbonControl(localMenu, dgInvTransferOrderLineGrid);
            dgInvTransferOrderLineGrid.api = api;
            dgInvItemStorageClientGrid.api = api;
            dgInvItemStorageClientGrid.ShowTotalSummary();
            SetupMaster(master);
            dgInvTransferOrderLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvTransferOrderLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            layOutInvTransferOrderLine.Caption = Uniconta.ClientTools.Localization.lookup("PurchaseLines");
            layOutInvItemStorage.Caption = Uniconta.ClientTools.Localization.lookup("OnHand");
            OnHandScreenInOrder = api.CompanyEntity._OnHandScreenInPurchase;
            layOutInvItemStorage.Visibility = OnHandScreenInOrder ? Visibility.Visible : Visibility.Collapsed;
            dgInvTransferOrderLineGrid.ShowTotalSummary();
            InitialLoad();
            this.KeyDown += Page_KeyDown;
        }

        private void Page_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F8)
                ribbonControl.PerformRibbonAction("AddItems");
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            SetupMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetupMaster(UnicontaBaseEntity args)
        {
            dgInvTransferOrderLineGrid.UpdateMaster(args);
        }

        void SetHeader()
        {
            var syncMaster = orderMaster;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), syncMaster._OrderNumber, syncMaster._DCAccount);
            if (header != null)
                SetHeader(header);
        }

        public InvTransferOrderLines(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Storage)
            {
                Storage.Visible = Storage.ShowInColumnChooser = false;
                QtyDelivered.Visible = QtyDelivered.ShowInColumnChooser = false;
            }
            else
            {
                Storage.ShowInColumnChooser = QtyDelivered.ShowInColumnChooser = true;
                QtyDelivered.AllowEditing = company._PurchaseLineEditDelivered ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False;
            }
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = LocationFrom.Visible = LocationFrom.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = LocationFrom.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!company.SerialBatchNumbers)
                SerieBatch.Visible = SerieBatch.ShowInColumnChooser = false;
            else
                SerieBatch.ShowInColumnChooser = true;
            if (!company.UnitConversion)
                UnitGroup.Visible = UnitGroup.ShowInColumnChooser = false;
            layOutInvItemStorage.Visibility = OnHandScreenInOrder ? Visibility.Visible : Visibility.Collapsed;
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        bool DataChanged;

        public override async void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InvItemStoragePage && argument != null)
            {
                var storeloc = argument as InvItemStorageClient;
                if (storeloc == null) return;
                var selected = dgInvTransferOrderLineGrid.SelectedItem as DCOrderLineClient;
                if (selected != null && (selected.Warehouse != storeloc.Warehouse || selected.Location != storeloc.Location))
                {
                    dgInvTransferOrderLineGrid.SetLoadedRow(selected);
                    selected.Warehouse = storeloc.Warehouse;
                    selected.Location = storeloc.Location;
                    dgInvTransferOrderLineGrid.SetModifiedRow(selected);
                    this.DataChanged = true;
                }
            }

            var param = argument as object[];
            if (param == null)
                return;

            if (screenName == TabControls.AddMultipleInventoryItem)
            {
                var orderNumber = (int)Uniconta.Common.Utility.NumberConvert.ToInt(Convert.ToString(param[1]));
                if (orderNumber == orderMaster._OrderNumber)
                {
                    if (dgInvTransferOrderLineGrid.isDefaultFirstRow)
                    {
                        dgInvTransferOrderLineGrid.DeleteRow();
                        dgInvTransferOrderLineGrid.isDefaultFirstRow = false;
                    }
                    var invItems = param[0] as List<UnicontaBaseEntity>;
                    dgInvTransferOrderLineGrid.PasteRows(invItems);
                }
            }
            else if (screenName == TabControls.ItemVariantAddPage)
            {
                var orderNumber = (int)Uniconta.Common.Utility.NumberConvert.ToInt(Convert.ToString(param[1]));
                if (orderNumber == orderMaster._OrderNumber)
                {
                    var invItems = param[0] as List<UnicontaBaseEntity>;
                    dgInvTransferOrderLineGrid.PasteRows(invItems);
                }
            }
            else if (screenName == TabControls.SerialToOrderLinePage)
            {
                var orderLine = param[0] as InvTransferOrderLineClient;
                if (dgInvTransferOrderLineGrid.HasUnsavedData)
                {
                    var t = saveGridLocal();
                    if (t != null && orderLine.RowId == 0)
                        await t;
                }
                if (api.CompanyEntity.Warehouse)
                    dgInvTransferOrderLineGrid.SetLoadedRow(orderLine);
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                if (DataChanged)
                    return true;
                return base.IsDataChaged;
            }
        }

        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, orderMaster);
            base.PageClosing();
        }
        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgInvItemStorageClientGrid);
            gridCtrls.Add(dgInvTransferOrderLineGrid);
        }
        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvTransferOrderLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvTransferOrderLineGrid_PropertyChanged;

            var selectedItem = e.NewItem as InvTransferOrderLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += InvTransferOrderLineGrid_PropertyChanged;
                if (selectedItem.Variant1Source == null)
                    setVariant(selectedItem, false);
                if (selectedItem.Variant2Source == null)
                    setVariant(selectedItem, true);
                if (addingRow && selectedItem._Item != null)
                    return;
                else
                    LoadInvItemStorageGrid(selectedItem);
                addingRow = false;
            }
        }
        private void LoadInvItemStorageGrid(InvTransferOrderLineClient selectedRow)
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
        async void setVariant(InvTransferOrderLineClient rec, bool SetVariant2)
        {
            if (items == null || variants1 == null || variants2 == null)
                return;

            //Check for Variant2 Exist
            if (string.IsNullOrEmpty(api?.CompanyEntity?._Variant2))
                SetVariant2 = false;

            var item = (InvItem)items.Get(rec._Item);
            if (item != null && item._StandardVariant != null)
            {
                rec.standardVariant = item._StandardVariant;
                var master = (InvStandardVariant)standardVariants?.Get(item._StandardVariant);
                if (master == null)
                    return;
                if (master._AllowAllCombinations)
                {
                    rec.Variant1Source = (IEnumerable<InvVariant1>)this.variants1?.GetKeyStrRecords;
                    rec.Variant2Source = (IEnumerable<InvVariant2>)this.variants2?.GetKeyStrRecords;
                }
                else
                {
                    var Combinations = master.Combinations ?? await master.LoadCombinations(api);
                    if (Combinations == null)
                        return;
                    List<InvVariant1> invs1 = null;
                    List<InvVariant2> invs2 = null;
                    string vr1 = null;
                    if (SetVariant2)
                    {
                        vr1 = rec._Variant1;
                        invs2 = new List<InvVariant2>();
                    }
                    else
                        invs1 = new List<InvVariant1>();

                    string LastVariant = null;
                    var var2Value = rec._Variant2;
                    bool hasVariantValue = (var2Value == null);
                    foreach (var cmb in Combinations)
                    {
                        if (SetVariant2)
                        {
                            if (cmb._Variant1 == vr1 && cmb._Variant2 != null)
                            {
                                var v2 = (InvVariant2)variants2.Get(cmb._Variant2);
                                if (v2 != null)
                                {
                                    invs2.Add(v2);
                                    if (var2Value == v2._Variant)
                                        hasVariantValue = true;
                                }

                            }
                        }
                        else if (LastVariant != cmb._Variant1)
                        {
                            LastVariant = cmb._Variant1;
                            var v1 = (InvVariant1)variants1.Get(cmb._Variant1);
                            if (v1 != null)
                                invs1.Add(v1);
                        }
                    }
                    if (SetVariant2)
                    {
                        rec.variant2Source = invs2;
                        //if (!hasVariantValue)
                        //    rec.Variant2 = null;
                    }
                    else
                        rec.variant1Source = invs1;
                }
            }
            else
            {
                rec.variant1Source = null;
                rec.variant2Source = null;
            }
            if (SetVariant2)
                rec.NotifyPropertyChanged("Variant2Source");
            else
                rec.NotifyPropertyChanged("Variant1Source");
        }
        async void setLocation(InvWarehouse master, InvTransferOrderLineClient rec)
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
        async void setLocationFrom(InvWarehouse master, InvTransferOrderLineClient rec)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                    rec.locationFromSource = master.Locations ?? await master.LoadLocations(api);
                else
                {
                    rec.locationFromSource = null;
                    rec.LocationFrom = null;
                }
                rec.NotifyPropertyChanged("LocationFromSource");
            }
        }
        public override void RowsPastedDone() { RecalculateAmount(); }

        public override void RowPasted(UnicontaBaseEntity rec)
        {
            var Comp = api.CompanyEntity;
            var order = orderMaster;
            var orderLine = (InvTransferOrderLineClient)rec;
            if (orderLine._Item != null)
            {
                if (Comp._InvoiceUseQtyNowCre)
                    orderLine.QtyNow = orderLine._Qty;
                var selectedItem = (InvItem)items.Get(orderLine._Item);
                if (selectedItem != null)
                {
                    if (selectedItem._PurchasePrice != 0 && Comp.SameCurrency(selectedItem._PurchaseCurrency, (byte)order._Currency))
                        orderLine.Price = selectedItem._PurchasePrice;
                    else
                        orderLine.Price = (exchangeRate == 0d) ? selectedItem._CostPrice : Math.Round(selectedItem._CostPrice * exchangeRate, 2);

                    orderLine.SetItemValues(selectedItem, Comp._PurchaseLineStorage, true);
                    TableField.SetUserFieldsFromRecord(selectedItem, orderLine);
                }
                else
                    orderLine._Item = null;
            }
        }

        private void InvTransferOrderLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (InvTransferOrderLineClient)sender;
            switch (e.PropertyName)
            {
                case "Item":
                    var selectedItem = (InvItem)items?.Get(rec._Item);
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

                        bool MakeConversion = false;
                        var Comp = api.CompanyEntity;
                        if (selectedItem._PurchaseQty != 0d)
                        {
                            MakeConversion = (selectedItem._PurchaseUnit != 0 && selectedItem._PurchaseUnit != selectedItem._Unit && Comp.UnitConversion);
                            rec.Qty = selectedItem._PurchaseQty;
                        }
                        else if (Comp._PurchaseLineOne)
                            rec.Qty = 1d;
                        if (Comp._InvoiceUseQtyNowCre)
                            rec.QtyNow = rec._Qty;
                        rec.SetItemValues(selectedItem, Comp._PurchaseLineStorage);
                        if (selectedItem._PurchasePrice != 0 && Comp.SameCurrency(selectedItem._PurchaseCurrency, (byte)orderMaster._Currency))
                            rec.Price = selectedItem._PurchasePrice;
                        else
                            rec.Price = (exchangeRate == 0d) ? selectedItem._CostPrice : Math.Round(selectedItem._CostPrice * exchangeRate, 2);

                        if (selectedItem._StandardVariant != rec.standardVariant)
                        {
                            rec.Variant1 = null;
                            rec.Variant2 = null;
                            rec.variant2Source = null;
                            rec.NotifyPropertyChanged("Variant2Source");
                        }
                        setVariant(rec, false);
                        TableField.SetUserFieldsFromRecord(selectedItem, rec);
                        if (selectedItem._Blocked)
                            UtilDisplay.ShowErrorCode(ErrorCodes.ItemIsOnHold, null);

                        globalEvents.NotifyRefreshViewer(NameOfControl, selectedItem);
                        if (MakeConversion)
                            SaveLineToGetConversion(rec);
                    }
                    break;
                case "Qty":
                    if (api.CompanyEntity._InvoiceUseQtyNowCre)
                        rec.QtyNow = rec._Qty;
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
                case "WarehouseFrom":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._WarehouseFrom);
                        setLocationFrom(selected, rec);
                    }
                    break;
                case "LocationFrom":
                    if (string.IsNullOrEmpty(rec._WarehouseFrom))
                        rec._LocationFrom = null;
                    break;
                case "Total":
                    Dispatcher.BeginInvoke(new Action(() => { RecalculateAmount(); }));
                    break;
                case "Variant1":
                    if (rec._Variant1 != null)
                        setVariant(rec, true);
                    break;
            }
        }

        void RecalculateAmount()
        {
            var ret = DebtorOfferLines.RecalculateLineSum(orderMaster, (IEnumerable<DCOrderLineClient>)dgInvTransferOrderLineGrid.ItemsSource, this.exchangeRate);
            double Amountsum = ret.Item1;
            double Costsum = ret.Item2;
            double AmountsumCompCur = ret.Item3;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            foreach (var grp in groups)
                grp.StatusValue = Amountsum.ToString("N2");
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvTransferOrderLineGrid.SelectedItem as InvTransferOrderLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    addingRow = true;
                    selectedItem = dgInvTransferOrderLineGrid.AddRow() as InvTransferOrderLineClient;
                    selectedItem._ExchangeRate = this.exchangeRate;
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                    {
                        selectedItem = dgInvTransferOrderLineGrid.CopyRow() as InvTransferOrderLineClient;
                        selectedItem._QtyDelivered = 0;
                        selectedItem._QtyInvoiced = 0;
                    }
                    break;
                case "SaveGrid":
                    saveGridLocal();
                    break;
                case "DeleteRow":
                    dgInvTransferOrderLineGrid.DeleteRow();
                    break;
                case "Storage":
                    ViewStorage();
                    break;
                case "Serial":
                    if (selectedItem != null)
                        LinkSerialNumber(selectedItem);
                    break;
                case "StockLines":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.InvTransactions, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), selectedItem._Item));
                    break;
                case "AddItems":
                    if (this.items == null)
                        return;
                    object[] paramArray = new object[] { new InvItemPurchaseCacheFilter(this.items), dgInvTransferOrderLineGrid.TableTypeUser, orderMaster };
                    AddDockItem(TabControls.AddMultipleInventoryItem, paramArray, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    break;
                case "UpdatePickList":
                case "UpdateDeliveryNote":
                    if (orderMaster != null)
                        OrderConfirmation(orderMaster, ActionType == "UpdateDeliveryNote" ? CompanyLayoutType.TransferPacknote : CompanyLayoutType.PickingList);
                    break;
                case "EditOrder":
                    AddDockItem(TabControls.InvTransferOrderPage2, orderMaster, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Orders"), orderMaster._OrderNumber));
                    break;
                case "UnfoldBOM":
                    if (selectedItem != null)
                    {
                        CwUsePriceFromBOM cw = new CwUsePriceFromBOM();
                        cw.DialogTableId = 2000000062;
                        cw.Closing += delegate
                        {
                            if (cw.DialogResult == true)
                                UnfoldBOM(selectedItem, cw.UsePricesFromBOM);
                        };
                        cw.Show();
                    }
                    break;
                case "AddVariants":
                    var itm = selectedItem?.InvItem;
                    if (itm?._StandardVariant != null)
                    {
                        var paramItem = new object[] { selectedItem, orderMaster };
                        dgInvTransferOrderLineGrid.SetLoadedRow(selectedItem);
                        AddDockItem(TabControls.ItemVariantAddPage, paramItem, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Variants")), null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "DebtorOrderLines":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.DebtorOrderLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), selectedItem._Item));
                    break;
                case "DebtorOfferLines":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.DebtorOfferLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OfferLine"), selectedItem._Item));
                    break;
                case "PurchaseOrderLines":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem._Item));
                    break;
                case "ProductionOrderLines":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.ProductionOrderLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionLines"), selectedItem._Item));
                    break;
                case "RefreshGrid":
                    RefreshGrid();
                    return; // RecalculateAmount called in refresh method
                case "InvStockProfile":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvStorageProfileReport, dgInvTransferOrderLineGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), selectedItem._Item));
                    break;
                case "ViewItemAttachments":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.UserDocsPage, selectedItem.InvItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem?.InvItem?._Name));
                    break;
                case "ViewNotes":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.UserNotesPage, selectedItem.InvItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Notes"), selectedItem?.InvItem?._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
            RecalculateAmount();
        }

        async void UnfoldBOM(InvTransferOrderLineClient selectedItem, bool usePriceFromBOM)
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
                var type = dgInvTransferOrderLineGrid.TableTypeUser;
                var Qty = selectedItem._Qty;
                var lst = new List<UnicontaBaseEntity>(list.Length);
                foreach (var bom in list)
                {
                    var invJournalLine = Activator.CreateInstance(type) as InvTransferOrderLineClient;
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
                    invJournalLine._Price = 0d;
                    TableField.SetUserFieldsFromRecord(bom, invJournalLine);
                    lst.Add(invJournalLine);
                }
                dgInvTransferOrderLineGrid.PasteRows(lst);
                this.DataChanged = true;

                dgInvTransferOrderLineGrid.SetLoadedRow(selectedItem);

                double _AmountEntered = 0d;
                if (!usePriceFromBOM)
                    _AmountEntered = selectedItem._Amount;
                selectedItem.Price = 0; // will clear amountEntered
                if (!usePriceFromBOM)
                    selectedItem._AmountEntered = _AmountEntered;
                selectedItem.Qty = 0;
                selectedItem.DiscountPct = 0;
                selectedItem.Discount = 0;
                dgInvTransferOrderLineGrid.SetModifiedRow(selectedItem);
            }
            busyIndicator.IsBusy = false;
        }

        static bool showInvPrintPreview = true;
        private void OrderConfirmation(InvTransferOrderClient dbOrder, CompanyLayoutType doctype)
        {
            var savetask = saveGridLocal();
            InvoiceAPI Invapi = new InvoiceAPI(api);
            bool showSendByMail = true;
            var comp = api.CompanyEntity;
            bool showUpdateInv = comp.Storage || ((doctype == CompanyLayoutType.TransferPacknote || doctype == CompanyLayoutType.PickingList) && comp.CreditorPacknote);
            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, doctype.ToString(), showInputforInvNumber: false, isShowInvoiceVisible: true,
                askForEmail: false, showNoEmailMsg: !showSendByMail, isShowUpdateInv: showUpdateInv);
            switch (doctype)
            {
                case CompanyLayoutType.PickingList:
                    GenrateOfferDialog.DialogTableId = 2000000113;
                    break;
                case CompanyLayoutType.TransferPacknote:
                    GenrateOfferDialog.DialogTableId = 2000000114;
                    break;
            }
            GenrateOfferDialog.SetInvPrintPreview(showInvPrintPreview);

            GenrateOfferDialog.Closed += async delegate
            {
                if (GenrateOfferDialog.DialogResult == true)
                {
                    if (savetask != null)
                    {
                        var err = await savetask;
                        if (err != ErrorCodes.Succes)
                            return;
                    }

                    showInvPrintPreview = GenrateOfferDialog.ShowInvoice || GenrateOfferDialog.InvoiceQuickPrint || GenrateOfferDialog.SendByOutlook;

                    var openOutlook = doctype == CompanyLayoutType.TransferPacknote || doctype ==  CompanyLayoutType.PickingList ? GenrateOfferDialog.UpdateInventory && GenrateOfferDialog.SendByOutlook : GenrateOfferDialog.SendByOutlook;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(dbOrder, null, doctype, GenrateOfferDialog.GenrateDate, null, !GenrateOfferDialog.UpdateInventory, GenrateOfferDialog.ShowInvoice, false,
                        GenrateOfferDialog.InvoiceQuickPrint, GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, openOutlook, GenrateOfferDialog.sendOnlyToThisEmail,
                        GenrateOfferDialog.Emails, false, null, false);
                    if (api.CompanyEntity.AllowSkipCreditMax)
                        invoicePostingResult.SetAllowCreditMax(GenrateOfferDialog.AllowSkipCreditMax);

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                        DebtorOrders.Updatedata(dbOrder, doctype);
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgInvTransferOrderLineGrid);
                }
            };
            GenrateOfferDialog.Show();
        }

        async void LinkSerialNumber(InvTransferOrderLineClient orderLine)
        {
            var syncEntity = dgInvTransferOrderLineGrid.syncEntity;
            var item = (InvItem)items.Get(orderLine._Item);
            if (item == null || !item._UseSerialBatch)
                return;
            var t = saveGridLocal();
            if (t != null && orderLine.RowId == 0)
                await t;
            if (api.CompanyEntity.Warehouse)
                dgInvTransferOrderLineGrid.SetLoadedRow(orderLine); // serial page add warehouse and location
            AddDockItem(TabControls.SerialToOrderLinePage, syncEntity, string.Format("{0}:{1}/{2},{3}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), orderLine.OrderRowId, orderLine._Item, orderLine.RowId));
        }

        async void RefreshGrid()
        {
            var savetask = saveGridLocal(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            await dgInvTransferOrderLineGrid.RefreshTask();
            RecalculateAmount();
            if (this.items.CacheAge.TotalMinutes > 10d)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem), true);
        }

        async void ViewStorage()
        {
            var t = saveGridLocal();
            if (t != null)
                await t;
            AddDockItem(TabControls.InvItemStoragePage, dgInvTransferOrderLineGrid.syncEntity, true);
        }

        async void SaveLineToGetConversion(InvTransferOrderLineClient rec)
        {
            rec._Price = 0;
            rec._Unit = 0;
            var tsk = dgInvTransferOrderLineGrid.SaveData();
            if (tsk != null)
            {
                await tsk;
                rec.NotifyPropertyChanged("Qty");
                rec.NotifyPropertyChanged("Price");
                var ind = dgInvTransferOrderLineGrid.GetVisibleRows().IndexOf(rec);
                if (ind >= 0)
                    dgInvTransferOrderLineGrid.RefreshRow(ind);
            }
        }
        Task<ErrorCodes> saveGridLocal()
        {
            var orderLine = dgInvTransferOrderLineGrid.SelectedItem as DCOrderLine;
            dgInvTransferOrderLineGrid.SelectedItem = null;
            dgInvTransferOrderLineGrid.SelectedItem = orderLine;
            return saveGrid();
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            if (dgInvTransferOrderLineGrid.HasUnsavedData)
            {
                ErrorCodes res = await dgInvTransferOrderLineGrid.SaveData();
                if (res == ErrorCodes.Succes)
                {
                    DataChanged = false;
                    globalEvents.OnRefresh(NameOfControl, orderMaster);
                }
                return res;
            }
            return ErrorCodes.Succes;
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgInvTransferOrderLineGrid.Filter(propValuePair);
        }

        public async override Task InitQuery()
        {
            await Filter(null);
            var itemSource = (IList)dgInvTransferOrderLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgInvTransferOrderLineGrid.AddFirstRow();
            RecalculateAmount();
        }

        private void InitialLoad()
        {
            var Comp = api.CompanyEntity;
            this.items = Comp.GetCache(typeof(InvItem));
            this.warehouse = Comp.GetCache(typeof(InvWarehouse));
            this.variants1 = Comp.GetCache(typeof(InvVariant1));
            this.variants2 = Comp.GetCache(typeof(InvVariant2));
            this.standardVariants = Comp.GetCache(typeof(InvStandardVariant));

            if (Comp.UnitConversion)
                Unit.Visible = true;
            if (dgInvTransferOrderLineGrid.IsLoadedFromLayoutSaved)
            {
                dgInvTransferOrderLineGrid.ClearSorting();
                dgInvTransferOrderLineGrid.IsLoadedFromLayoutSaved = false;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (this.items == null)
                this.items = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);

            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = Comp.GetCache(typeof(Uniconta.DataModel.InvWarehouse)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);
            if (Comp.ItemVariants)
            {
                if (this.standardVariants == null)
                    this.standardVariants = Comp.GetCache(typeof(Uniconta.DataModel.InvStandardVariant)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvStandardVariant)).ConfigureAwait(false);
                if (this.variants1 == null)
                    this.variants1 = Comp.GetCache(typeof(Uniconta.DataModel.InvVariant1)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvVariant1)).ConfigureAwait(false);
                if (this.variants2 == null)
                    this.variants2 = Comp.GetCache(typeof(Uniconta.DataModel.InvVariant2)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvVariant2)).ConfigureAwait(false);
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var selectedItem = dgInvTransferOrderLineGrid.SelectedItem as InvTransferOrderLineClient;
                    if (selectedItem != null)
                    {
                        if (selectedItem.Variant1Source == null)
                            setVariant(selectedItem, false);
                        if (selectedItem.Variant2Source == null)
                            setVariant(selectedItem, true);
                    }
                }));
            }
        }

        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvTransferOrderLineGrid.SelectedItem as InvTransferOrderLineClient;
            if (selectedItem?._Warehouse != null && warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
                if (prevLocation != null)
                    prevLocation.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevLocation = editor;
                editor.isValidate = true;
            }
        }
        CorasauGridLookupEditorClient prevVariant1;
        private void variant1_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant1 != null)
                prevVariant1.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant1 = editor;
            editor.isValidate = true;
        }

        CorasauGridLookupEditorClient prevVariant2;

        CorasauGridLookupEditorClient prevLocationFrom;
        private void LocationFrom_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvTransferOrderLineGrid.SelectedItem as InvTransferOrderLineClient;
            if (selectedItem?._WarehouseFrom != null && warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._WarehouseFrom);
                setLocationFrom(selected, selectedItem);
                if (prevLocationFrom != null)
                    prevLocationFrom.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevLocationFrom = editor;
                editor.isValidate = true;
            }
        }

        private void variant2_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant2 != null)
                prevVariant2.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant2 = editor;
            editor.isValidate = true;
        }


        protected override bool LoadTemplateHandledLocally(IEnumerable<UnicontaBaseEntity> templateRows)
        {
            foreach (var _it in templateRows)
            {
                var row = _it as DCOrderLineClient;
                row._CostPrice = 0;
                row.ClearRef();
                var item = (Uniconta.DataModel.InvItem)this.items.Get(row._Item);
                if (item != null)
                    row.SetCostFromItem(item);
            }
            return false;
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
    }
}
