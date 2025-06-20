using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Uniconta.ClientTools.Util;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using Uniconta.API.System;
using DevExpress.Utils.Behaviors.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorOfferLineGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get
            {
                return typeof(DebtorOfferLineClient);
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
            var lst = new List<DebtorOfferLineClient>();

            if (row is InvTrans)
            {
                foreach (var _it in copyFromRows)
                {
                    var it = (InvTrans)_it;
                    var line = Activator.CreateInstance(type) as DebtorOfferLineClient;
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
                    line._Unit = it._Unit;
                    line._Date = it._Date;
                    line._Week = it._Week;
                    line._Note = it._Note;
                    lst.Add(line);
                }
            }
            else if (row is DCOrderLine)
            {
                foreach (var _it in copyFromRows)
                {
                    var it = (DCOrderLine)_it;
                    var line = Activator.CreateInstance(type) as DebtorOfferLineClient;
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
                    line._Unit = it._Unit;
                    line._Date = it._Date;
                    line._Week = it._Week;
                    line._Note = it._Note;
                    TableField.SetUserFieldsFromRecord(it, line);
                    lst.Add(line);
                }
            }
            else
            {
                foreach (var _it in copyFromRows)
                {
                    var line = Activator.CreateInstance(type) as DebtorOfferLineClient;
                    var prop = _it.GetType().GetProperty("Quantity") ?? _it.GetType().GetProperty("Qty");
                    line._Qty = Convert.ToDouble(prop?.GetValue(_it, null));
                    line._Item = _it.GetType().GetProperty("Item")?.GetValue(_it, null) as string;
                    if (line._Item != null || line._Qty != 0)
                        lst.Add(line);
                }
            }
            if (lst.Count > 0)
                return lst;
            return null;
        }
    }

    public partial class DebtorOfferLines : GridBasePage
    {
        SQLCache items, standardVariants, variants1, variants2, employees, warehouse;
        DebtorOfferClient Order { get { return dgDebtorOfferLineGrid.masterRecord as DebtorOfferClient; } }
        Uniconta.API.DebtorCreditor.FindPrices PriceLookup;
        double exchangeRate;
        bool OnHandScreenInOrder;

        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, Order);
            base.PageClosing();
        }
        public override string NameOfControl { get { return TabControls.DebtorOfferLines; } }
        public DebtorOfferLines(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }
        public DebtorOfferLines(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }
        public void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            ((TableView)dgDebtorOfferLineGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            ((TableView)dgInvItemStorageClientGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            SetRibbonControl(localMenu, dgDebtorOfferLineGrid);
            dgDebtorOfferLineGrid.api = api;
            dgInvItemStorageClientGrid.api = api;
            dgInvItemStorageClientGrid.ShowTotalSummary();
            RemoveMenuItem();
            SetupMaster(master);
            dgDebtorOfferLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorOfferLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            layOutDebtorOfferLine.Caption = Uniconta.ClientTools.Localization.lookup("OrdersLine");
            layOutInvItemStorage.Caption = Uniconta.ClientTools.Localization.lookup("OnHand");
            OnHandScreenInOrder = api.CompanyEntity._OnHandScreenInOrder;
            layOutInvItemStorage.Visibility = OnHandScreenInOrder ? Visibility.Visible : Visibility.Collapsed;
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
            var OrderId = Order?.RowId;
            dgDebtorOfferLineGrid.UpdateMaster(args);
            if (Order?.RowId != OrderId)
            {
                if (Order == null)
                    PriceLookup = null;
                else if (PriceLookup == null)
                    PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(Order, api);
                else
                    PriceLookup.OrderChanged(Order);
            }
        }
        void SetHeader()
        {
            var syncMaster = Order;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OfferLine"), syncMaster._OrderNumber, syncMaster._DCAccount);
            if (header != null)
                SetHeader(header);
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetupVariants(api, null, colInvItemVariant1, colInvItemVariant2, colInvItemVariant3, colInvItemVariant4, colInvItemVariant5, null, null, null, null, null);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
            {
                Location.Visible = Location.ShowInColumnChooser = false;
                InvItemLocation.Visible = InvItemLocation.ShowInColumnChooser = false;
            }
            else
                Location.ShowInColumnChooser = InvItemLocation.ShowInColumnChooser = true;
            if (!company.Warehouse)
            {
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
                InvItemWarehouse.Visible = InvItemWarehouse.ShowInColumnChooser = false;
            }
            else
                Warehouse.ShowInColumnChooser = InvItemWarehouse.ShowInColumnChooser = true;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (company.HideCostPrice)
            {
                Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
           CostPrice.Visible = CostPrice.ShowInColumnChooser = CostValue.Visible = CostValue.ShowInColumnChooser = false;
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "CostValue", "DB" });
            }

            if (Order?._PricesInclVat == true)
            {
                Margin.Visible = Margin.ShowInColumnChooser = false;
                MarginRatio.Visible = MarginRatio.ShowInColumnChooser = false;
            }
            if (!company.UnitConversion)
                UnitGroup.Visible = UnitGroup.ShowInColumnChooser = false;
            layOutInvItemStorage.Visibility = OnHandScreenInOrder ? Visibility.Visible : Visibility.Collapsed;
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public bool DataChaged;
        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgInvItemStorageClientGrid);
            gridCtrls.Add(dgDebtorOfferLineGrid);
        }
        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditOrder");
            if (ibase != null)
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), Uniconta.ClientTools.Localization.lookup("Offers"));
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as DebtorOfferLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= DebtorOfferLineGrid_PropertyChanged;
            var selectedItem = e.NewItem as DebtorOfferLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += DebtorOfferLineGrid_PropertyChanged;
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

        public override void RowsPastedDone() { RecalculateAmount(); }

        public override void RowPasted(UnicontaBaseEntity rec)
        {
            var orderLine = (DCOrderLineClient)rec;
            if (orderLine._Item != null)
            {
                var selectedItem = (InvItem)items.Get(orderLine._Item);
                if (selectedItem != null)
                {
                    PriceLookup?.SetPriceFromItem(orderLine, selectedItem);
                    orderLine.SetItemValues(selectedItem, 0, true);
                    TableField.SetUserFieldsFromRecord(selectedItem, orderLine);
                }
                else
                    orderLine._Item = null;
            }
        }
        private void LoadInvItemStorageGrid(DebtorOfferLineClient selectedRow)
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
        private void DebtorOfferLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as DebtorOfferLineClient;
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
                        rec.CostPrice = 0;
                        var _priceLookup = this.PriceLookup;
                        this.PriceLookup = null; // avoid that we call priceupdated in property change on Qty
                        if (selectedItem._SalesQty != 0d)
                            rec.Qty = selectedItem._SalesQty;
                        else if (api.CompanyEntity._OrderLineOne)
                            rec.Qty = 1d;
                        rec.SetItemValues(selectedItem);
                        _priceLookup?.SetPriceFromItem(rec, selectedItem);
                        this.PriceLookup = _priceLookup;

                        if (selectedItem._StandardVariant != rec.standardVariant)
                        {
                            rec.Variant1 = null;
                            rec.Variant2 = null;
                            rec.variant2Source = null;
                            rec.NotifyPropertyChanged("Variant2Source");
                        }
                        setVariant(rec, false);
                        LoadInvItemStorageGrid(rec);
                        TableField.SetUserFieldsFromRecord(selectedItem, rec);
                        if (selectedItem._Blocked)
                            UtilDisplay.ShowErrorCode(ErrorCodes.ItemIsOnHold, null);

                        globalEvents?.NotifyRefreshViewer(NameOfControl, selectedItem);
                    }
                    break;
                case "Qty":
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    break;
                case "Subtotal":
                case "Total":
                    RecalculateAmount();
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
                case "Employee":
                    if (rec._Employee != null)
                    {
                        var item = (InvItem)items?.Get(rec._Item);
                        if (item == null || item._ItemType == (byte)Uniconta.DataModel.ItemType.Service)
                        {
                            var emp = (Uniconta.DataModel.Employee)employees?.Get(rec._Employee);
                            if (emp != null && emp._CostPrice != 0d)
                                rec.CostPrice = emp._CostPrice;
                        }
                    }
                    break;
                case "EAN":
                    FindOnEAN(rec, this.items, api, this.PriceLookup);
                    break;
                case "Variant1":
                    if (rec._Variant1 != null)
                        setVariant(rec, true);
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    break;
                case "Variant2":
                case "Variant3":
                case "Variant4":
                case "Variant5":
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    break;
                case "CustomerItemNumber":
                    if (!string.IsNullOrEmpty(rec.CustomerItemNumber))
                        DebtorOfferLines.FindItemFromCustomerItem(rec, Order, api, rec.CustomerItemNumber);
                    break;
            }
        }

        static public void FindOnEAN(DCOrderLineClient rec, SQLCache Items, QueryAPI api) { FindOnEAN(rec, Items, api, null); }
        static public void FindOnEAN(DCOrderLineClient rec, SQLCache Items, QueryAPI api, Uniconta.API.DebtorCreditor.FindPrices PriceLookup)
        {
            var EAN = rec._EAN;
            if (string.IsNullOrWhiteSpace(EAN))
                return;
            var found = (from item in (InvItem[])Items.GetNotNullArray where string.Compare(item._EAN, EAN, StringComparison.CurrentCultureIgnoreCase) == 0 select item).FirstOrDefault();
            if (found != null)
            {
                rec._EAN = found._EAN;
                rec.Item = found._Item;
            }
            else
                FindOnEANVariant(rec, api, PriceLookup);
        }

        static async void FindOnEANVariant(DCOrderLineClient rec, QueryAPI api, Uniconta.API.DebtorCreditor.FindPrices PriceLookup)
        {
            if (PriceLookup != null && PriceLookup.UseCustomerPrices)
            {
                var found = await PriceLookup.GetCustomerPriceFromEAN(rec);
                if (found)
                    return;
            }
            var ap = new Uniconta.API.Inventory.ReportAPI(api);
            var variant = await ap.GetInvVariantDetail(rec._EAN);
            if (variant != null)
            {
                rec.Item = variant._Item;
                rec.Variant1 = variant._Variant1;
                rec.Variant2 = variant._Variant2;
                rec.Variant3 = variant._Variant3;
                rec.Variant4 = variant._Variant4;
                rec.Variant5 = variant._Variant5;
                rec._EAN = variant._EAN;
                if (variant._CostPrice != 0d)
                    rec._CostPrice = variant._CostPrice;
            }
        }

        public static async void FindItemFromCustomerItem(DCOrderLineClient rec, DCOrder order, QueryAPI api, string CustomerItemNumber)
        {
            var ap = new Uniconta.API.Inventory.ReportAPI(api);
            var variant = await ap.FindItemFromCustomerItem(order, CustomerItemNumber);
            if (variant != null)
            {
                rec.Item = variant._Item;
                rec.Variant1 = variant._Variant1;
                rec.Variant2 = variant._Variant2;
                rec.Variant3 = variant._Variant3;
                rec.Variant4 = variant._Variant4;
                rec.Variant5 = variant._Variant5;
            }
        }

        async void setVariant(DebtorOfferLineClient rec, bool SetVariant2)
        {
            if (items == null || variants1 == null || variants2 == null)
                return;

            //Check for Variant2 Exist
            if (string.IsNullOrEmpty(api.CompanyEntity?._Variant2))
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
                                invs2.Add(v2);
                                if (var2Value == v2._Variant)
                                    hasVariantValue = true;

                            }
                        }
                        else if (LastVariant != cmb._Variant1)
                        {
                            LastVariant = cmb._Variant1;
                            var v1 = (InvVariant1)variants1.Get(cmb._Variant1);
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

        async void setLocation(InvWarehouse master, DebtorOfferLineClient rec)
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

            var selectedItem = dgDebtorOfferLineGrid.SelectedItem as DebtorOfferLineClient;
            if (selectedItem?._Warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }

        static public Tuple<double, double, double> RecalculateLineSum(IList source, double exRate)
        {
            return RecalculateLineSum(null, (IEnumerable<DCOrderLineClient>)source, exRate);
        }
        static public Tuple<double, double, double> RecalculateLineSum(DCOrder order, IEnumerable<DCOrderLineClient> source, double exRate)
        {
            double lastTotal = 0d, lastCost = 0d;
            double Amountsum = 0d;
            double Costsum = 0d;
            string VatCode = null;
            var inclVat = false;
            Company Comp = null;
            if (order != null)
            {
                inclVat = order._PricesInclVat;
                Comp = Company.Get(order.CompanyId);
            }
            if (source != null)
            {
                if (inclVat)
                {
                    var dct = order.__DCType();
                    if (dct == 1 || dct == 3)
                        VatCode = ((Debtor)ClientHelper.GetRef(order.CompanyId, typeof(Debtor), order._DCAccount))?._Vat;
                    else
                        inclVat = false;
                }

                double qty = 0d;
                int i = 0;
                var items = Comp?.GetCache(typeof(Uniconta.DataModel.InvItem));
                bool reloadCache = false;
                foreach (var lin in source)
                {
                    i++;
                    var orderLine = (DCOrderLineClient)lin;
                    if (!orderLine._Subtotal)
                    {
                        if (orderLine._Item != null && items != null && items.Get(orderLine._Item) == null)
                            reloadCache = true;

                        qty += orderLine._Qty;
                        var cost = orderLine.costvalue();
                        Costsum += cost;
                        var sales = orderLine._Amount;
                        Amountsum += sales;
                        if (exRate != 0)
                            orderLine._ExchangeRate = exRate;
                        else
                            exRate = orderLine._ExchangeRate;

                        if (inclVat && VatCode == null)
                        {
                            VatCode = orderLine._Vat;
                            if (VatCode == null && orderLine._Item != null)
                                VatCode = orderLine.InvItem?.InventoryGroup?.SalesVat;
                        }
                    }
                    else
                    {
                        var subtotal = Amountsum - lastTotal;
                        if (subtotal != orderLine._AmountEntered)
                        {
                            orderLine._AmountEntered = subtotal;
                            orderLine._CostPrice = Costsum - lastCost;
                            if (orderLine._Price != 0d)
                                orderLine.Price = 0d; // this will redraw screen 
                            else
                                orderLine.NotifyPropertyChanged(nameof(orderLine.Total));
                        }
                        lastTotal = Amountsum;
                        lastCost = Costsum;
                    }
                }
                if (order != null)
                {
                    order.NoLines = i;
                    order.TotalQty = Math.Round(qty, 10);
                    if (reloadCache)
                        new QueryAPI(BasePage.session, Comp).LoadCache(typeof(Uniconta.DataModel.InvItem), true);
                }
            }
            Amountsum = Math.Round(Amountsum, 2);
            double AmountsumCur = exRate == 0d ? Amountsum : Math.Round(Amountsum / exRate, 2);

            if (inclVat && VatCode != null)
            {
                var vatRef = (GLVat)ClientHelper.GetRef(order.CompanyId, typeof(GLVat), VatCode);
                if (vatRef != null && vatRef._Rate != 0d)
                    AmountsumCur = Math.Round(AmountsumCur * 100 / (100 + vatRef._Rate), 2);
            }
            Costsum = Math.Round(Costsum, 2);
            if (order != null && (order._OrderTotal != Amountsum || order._CostValue != Costsum))
            {
                order._CostValue = Costsum;
                order._OrderTotal = Amountsum;
                order.NotifyPropertyChanged("OrderTotal");
                order.NotifyPropertyChanged("CostValue");
                order.NotifyPropertyChanged("Margin");
                order.NotifyPropertyChanged("MarginRatio");
            }
            return Tuple.Create(Amountsum, Costsum, AmountsumCur);
        }

        void RecalculateAmount()
        {
            var ret = RecalculateLineSum(Order, (IEnumerable<DCOrderLineClient>)dgDebtorOfferLineGrid.ItemsSource, this.exchangeRate);
            double Amountsum = ret.Item1;
            double Costsum = ret.Item2;
            double sales = ret.Item3;
            Order._OrderTotal = Amountsum;
            if (Order._EndDiscountPct != 0)
                sales *= (100d - Order._EndDiscountPct) / 100d;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            foreach (var grp in groups)
            {
                if (grp.Caption == Uniconta.ClientTools.Localization.lookup("Amount"))
                    grp.StatusValue = Amountsum.ToString("N2");
                else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("CostValue"))
                    grp.StatusValue = Costsum.ToString("N2");
                else if (grp.Caption == Uniconta.ClientTools.Localization.lookup("DB"))
                {
                    var margin = (sales - Costsum);
                    var ratio = sales != 0d ? Math.Round(margin * 100d / sales) : 0d;
                    string str;
                    if (ratio != 0 && ratio != 100 && ratio > -100)
                        str = string.Format("{0}% {1:n2}", ratio, margin);
                    else
                        str = margin.ToString("N2");
                    grp.StatusValue = str;
                }
                else
                    grp.StatusValue = string.Empty;
            }
        }
        bool addingRow;
        private void localMenu_OnItemClicked(string ActionType)
        {
            DebtorOfferLineClient row;
            var selectedItem = dgDebtorOfferLineGrid.SelectedItem as DebtorOfferLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    addingRow = true;
                    row = dgDebtorOfferLineGrid.AddRow() as DebtorOfferLineClient;
                    if (row != null)
                        row._ExchangeRate = this.exchangeRate;
                    break;
                case "CopyRow":
                    row = dgDebtorOfferLineGrid.CopyRow() as DebtorOfferLineClient;
                    if (row != null)
                    {
                        row._ExchangeRate = this.exchangeRate;
                        row._CostPriceLine = selectedItem._CostPriceLine;
                    }
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgDebtorOfferLineGrid.DeleteRow();
                    break;
                case "InsertSubTotal":
                    row = dgDebtorOfferLineGrid.AddRow() as DebtorOfferLineClient;
                    row.Subtotal = true;
                    break;
                case "StockLines":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.DebtorInvoiceLines, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), selectedItem._Item));
                    break;
                case "UnfoldBOM":
                    if (selectedItem != null)
                    {
                        CwUsePriceFromBOM cw = new CwUsePriceFromBOM();
                        cw.DialogTableId = 2000000046;
                        cw.Closing += delegate
                        {
                            if (cw.DialogResult == true)
                                UnfoldBOM(selectedItem, cw.UsePricesFromBOM);
                        };
                        cw.Show();
                    }
                    break;
                case "Storage":
                    ViewStorage();
                    break;
                case "AddItems":
                    if (this.items == null)
                        return;
                    object[] paramArray = new object[3] { new InvItemSalesCacheFilter(this.items), dgDebtorOfferLineGrid.TableTypeUser, Order };
                    AddDockItem(TabControls.AddMultipleInventoryItem, paramArray, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    break;
                case "EditOrder":
                    AddDockItem(TabControls.DebtorOfferPage2, Order, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Offers"), Order._OrderNumber));
                    break;
                case "AddVariants":
                    var itm = selectedItem?.InvItem;
                    if (itm?._StandardVariant != null)
                    {
                        var paramItem = new object[] { selectedItem, Order };
                        dgDebtorOfferLineGrid.SetLoadedRow(selectedItem);
                        AddDockItem(TabControls.ItemVariantAddPage, paramItem, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Variants")), null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "CreateProduction":
                    if (selectedItem != null)
                        CreateProductionOrder(selectedItem);
                    break;
                case "CreateFromInvoice":
                    try
                    {
                        var par = new object[4];
                        par[0] = api;
                        par[1] = Order.Account;
                        par[2] = false;
                        par[3] = Order;
                        AddDockItem(TabControls.CreateOrderFromQuickInvoice, par, true, String.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Invoice")));
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex);
                    }
                    break;
                case "ShowInvoice":
                case "PrintOffer":
                    if (Order != null)
                        GenerateOffer(ActionType == "ShowInvoice" ? true : false);
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
                case "RefreshGrid":
                    RefreshGrid();
                    return; // RecalculateAmount called in refresh method
                case "ViewItemAttachments":
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
            RecalculateAmount();
        }

        private void GenerateOffer(bool isPreviewOnly)
        {
            saveGrid();

            if (isPreviewOnly)
            {
                ExecuteGenerateOffer(Order, DateTime.Now, true, false, false, 0, false, false, false, null, true);
                return;
            }

            bool showSendByMail = false;
            var dbOrder = Order;
            string debtorName;
            var debtor = dbOrder.Debtor;

            if (debtor != null)
            {
                debtorName = debtor?._Name ?? dbOrder._DCAccount;
                showSendByMail = (!string.IsNullOrEmpty(debtor.InvoiceEmail) || debtor.EmailDocuments);
            }
            else if (dbOrder._Prospect == 0)
            {
                UtilDisplay.ShowErrorCode(ErrorCodes.AccountIsMissing);
                return;
            }
            else
            {
                debtorName = Uniconta.ClientTools.Localization.lookup("Prospect");
                showSendByMail = true;
            }

            var generateOfferDialog = new CWGenerateInvoice(false, CompanyLayoutType.Offer.ToString(), askForEmail: true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isDebtorOrder: true);
            generateOfferDialog.DialogTableId = 2000000075;
            generateOfferDialog.ShowAllowCredMax(debtor._CreditMax != 0);
            generateOfferDialog.Closed += delegate
            {
                if (generateOfferDialog.DialogResult == true)
                {
                    ExecuteGenerateOffer(dbOrder, generateOfferDialog.GenrateDate, generateOfferDialog.ShowInvoice, generateOfferDialog.PostOnlyDelivered, generateOfferDialog.InvoiceQuickPrint,
                        generateOfferDialog.NumberOfPages, generateOfferDialog.SendByEmail, generateOfferDialog.SendByOutlook, generateOfferDialog.sendOnlyToThisEmail, generateOfferDialog.Emails, false);
                }
            };
            generateOfferDialog.Show();
        }

        async private void ExecuteGenerateOffer(DebtorOfferClient dbOrder, DateTime genrateDate, bool showInvoice, bool postOnlyDelivered, bool isQuickPrint, int noOfPages, bool sendByEmail, bool sendByOutlook, bool sendOnlyToEmail,
            string emailList, bool isPreviewOnly)
        {
            var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
            invoicePostingResult.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.Offer, genrateDate, null, true, showInvoice, postOnlyDelivered, isQuickPrint, noOfPages, sendByEmail, sendByOutlook, sendOnlyToEmail,
                emailList, false, null, false);
            invoicePostingResult.SetAllowCreditMax(api.CompanyEntity.AllowSkipCreditMax);

            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
            busyIndicator.IsBusy = true;
            var result = await invoicePostingResult.Execute();
            busyIndicator.IsBusy = false;

            if (!result)
                Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOfferLineGrid);
            else if (!isPreviewOnly)
                DebtorOrders.Updatedata(dbOrder, CompanyLayoutType.Offer);
        }

        async void CreateProductionOrder(DebtorOfferLineClient offerLine)
        {
            var t = saveGrid();
            if (t != null && offerLine.RowId == 0)
                await t;

            AddDockItem(TabControls.ProductionOrdersPage2, new object[] { api, offerLine, dgDebtorOfferLineGrid.masterRecord }, Uniconta.ClientTools.Localization.lookup("Production"), "Add_16x16");
        }

        public override void Utility_Refresh(string screenName, object argument)
        {
            var param = argument as object[];
            if (param == null)
                return;

            if (screenName == TabControls.AddMultipleInventoryItem)
            {
                var orderNumber = (int)Uniconta.Common.Utility.NumberConvert.ToInt(Convert.ToString(param[1]));
                if (orderNumber == Order._OrderNumber)
                {
                    if (dgDebtorOfferLineGrid.isDefaultFirstRow)
                    {
                        dgDebtorOfferLineGrid.DeleteRow();
                        dgDebtorOfferLineGrid.isDefaultFirstRow = false;
                    }
                    var invItems = param[0] as List<UnicontaBaseEntity>;
                    dgDebtorOfferLineGrid.PasteRows(invItems);
                }
            }
            else if (screenName == TabControls.ItemVariantAddPage)
            {
                var orderNumber = (int)Uniconta.Common.Utility.NumberConvert.ToInt(Convert.ToString(param[1]));
                if (orderNumber == Order._OrderNumber)
                {
                    var invItems = param[0] as List<UnicontaBaseEntity>;
                    dgDebtorOfferLineGrid.PasteRows(invItems);
                }
            }
            else if (screenName == TabControls.CreateOrderFromQuickInvoice && argument != null)
            {
                var args = argument as object[];
                var orderlines = args[2] as IEnumerable<UnicontaBaseEntity>;

                var masterRecord = dgDebtorOfferLineGrid.masterRecord;
                var currentRecrod = args[4] as UnicontaBaseEntity;

                if (currentRecrod != null && object.ReferenceEquals(masterRecord, currentRecrod))
                    dgDebtorOfferLineGrid.PasteRows(orderlines);
            }
        }

        async void RefreshGrid()
        {
            var savetask = saveGrid(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            await dgDebtorOfferLineGrid.RefreshTask();
            RecalculateAmount();
            api.UpdateCache();
        }

        async void ViewStorage()
        {
            var savetask = saveGrid(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            AddDockItem(TabControls.InvItemStoragePage, dgDebtorOfferLineGrid.syncEntity, true);
        }

        public override bool IsDataChaged { get { return DataChaged || base.IsDataChaged; } }

        async void UnfoldBOM(DebtorOfferLineClient selectedItem, bool usePriceFromBOM)
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
                var pl = this.PriceLookup;
                if (!usePriceFromBOM)
                    this.PriceLookup = null;

                var type = dgDebtorOfferLineGrid.TableTypeUser;
                var Qty = selectedItem._Qty;
                var lst = new List<UnicontaBaseEntity>(list.Length);
                foreach (var bom in list)
                {
                    var invJournalLine = Activator.CreateInstance(type) as DebtorOfferLineClient;
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
                dgDebtorOfferLineGrid.PasteRows(lst);
                DataChaged = true;
                this.PriceLookup = pl;

                dgDebtorOfferLineGrid.SetLoadedRow(selectedItem);

                double _AmountEntered = 0d;
                if (!usePriceFromBOM)
                    _AmountEntered = selectedItem._Amount;
                selectedItem.Price = 0; // will clear amountEntered
                if (!usePriceFromBOM)
                    selectedItem._AmountEntered = _AmountEntered;
                selectedItem.Qty = 0;
                selectedItem.DiscountPct = 0;
                selectedItem.Discount = 0;
                selectedItem.CostPrice = 0;
                dgDebtorOfferLineGrid.SetModifiedRow(selectedItem);
            }
            busyIndicator.IsBusy = false;
        }

        bool refreshOnHand;
        protected override async Task<ErrorCodes> saveGrid()
        {
            var offerLine = dgDebtorOfferLineGrid.SelectedItem as DebtorOfferLineClient;
            dgDebtorOfferLineGrid.SelectedItem = null;
            dgDebtorOfferLineGrid.SelectedItem = offerLine;
            refreshOnHand = offerLine?.RowId == 0;
            if (dgDebtorOfferLineGrid.HasUnsavedData)
            {
                ErrorCodes res = await dgDebtorOfferLineGrid.SaveData();
                if (res == ErrorCodes.Succes)
                {
                    DataChaged = false;
                    globalEvents.OnRefresh(NameOfControl, Order);
                }
                if (refreshOnHand)
                {
                    refreshOnHand = false;
                    LoadInvItemStorageGrid(offerLine);
                }
                return res;
            }
            return 0;
        }
        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgDebtorOfferLineGrid.Filter(propValuePair);
        }
        public async override Task InitQuery()
        {
            await Filter(null);
            var itemSource = (IList)dgDebtorOfferLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgDebtorOfferLineGrid.AddFirstRow();
            RecalculateAmount();
        }

        private void InitialLoad()
        {
            var Comp = api.CompanyEntity;
            this.items = Comp.GetCache(typeof(InvItem));
            this.variants1 = Comp.GetCache(typeof(InvVariant1));
            this.variants2 = Comp.GetCache(typeof(InvVariant2));
            this.standardVariants = Comp.GetCache(typeof(InvStandardVariant));

            if (Comp.UnitConversion)
                Unit.Visible = true;

            if (dgDebtorOfferLineGrid.IsLoadedFromLayoutSaved)
            {
                dgDebtorOfferLineGrid.ClearSorting();
                dgDebtorOfferLineGrid.IsLoadedFromLayoutSaved = false;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (this.items == null)
                this.items = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);

            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = api.GetCache(typeof(Uniconta.DataModel.InvWarehouse)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);

            if (api.CompanyEntity.ItemVariants)
            {
                if (this.standardVariants == null)
                    this.standardVariants = api.GetCache(typeof(Uniconta.DataModel.InvStandardVariant)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvStandardVariant)).ConfigureAwait(false);
                if (this.variants1 == null)
                    this.variants1 = api.GetCache(typeof(Uniconta.DataModel.InvVariant1)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvVariant1)).ConfigureAwait(false);
                if (this.variants2 == null)
                    this.variants2 = api.GetCache(typeof(Uniconta.DataModel.InvVariant2)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvVariant2)).ConfigureAwait(false);
            }

            if (this.PriceLookup == null)
                PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(Order, api);
            var t = this.PriceLookup.ExchangeTask;
            this.exchangeRate = this.PriceLookup.ExchangeRate;
            if (this.exchangeRate == 0d && t != null)
                this.exchangeRate = await t.ConfigureAwait(false);

            this.employees = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Employee)).ConfigureAwait(false);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var selectedItem = dgDebtorOfferLineGrid.SelectedItem as DebtorOfferLineClient;
                if (selectedItem != null)
                {
                    if (selectedItem.Variant1Source == null)
                        setVariant(selectedItem, false);
                    if (selectedItem.Variant2Source == null)
                        setVariant(selectedItem, true);
                }
            }));
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
        private void btnSales_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvItemStorageClientGrid.SelectedItem as InvItemStorageClient;
            if (selectedItem != null)
                AddDockItem(TabControls.DebtorOrderLineReport, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("OrderLines"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("OnHand")));
        }
        private void btnPurchase_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvItemStorageClientGrid.SelectedItem as InvItemStorageClient;
            if (selectedItem != null)
                AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("OnHand")));
        }
    }
}
