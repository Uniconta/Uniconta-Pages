using Uniconta.API.DebtorCreditor;
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
using System.Net;
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

using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Pages;
using Uniconta.Common.Utility;

#if !SILVERLIGHT
using Microsoft.Win32;
using ubl_norway_uniconta;
#endif
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvItemStorageClientGridReadOnly : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvItemStorageClient); } }
        public override IComparer GridSorting { get { return new InvItemStorageClientSort(); } }
        public override bool Readonly { get { return true; } }
    }
    public class DebtorOrderLineGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get
            {
                return typeof(DebtorOrderLineClient);
            }
        }

        public override bool SingleBufferUpdate { get { return false; } }
        public override IComparer GridSorting { get { return new DCOrderLineSort(); } }
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
            var selectedItem = (DCOrderLine)this.SelectedItem;
            return (selectedItem != null) && (selectedItem._Item != null || selectedItem._Text != null);
        }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            var row = copyFromRows.FirstOrDefault();
            var type = this.TableTypeUser;
            List<DebtorOrderLineClient> lst = null;
            if (row is InvTrans)
            {
                lst = new List<DebtorOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (InvTrans)_it;
                    lst.Add(CreateNewOrderLine(it._Item, it.MovementTypeEnum == InvMovementType.Debtor ? -it._Qty : it._Qty, it._Text, it._Price, it.MovementTypeEnum == InvMovementType.Debtor ? -it._AmountEntered : it._AmountEntered,
                        it._DiscountPct, it._Discount, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5, it._Warehouse, it._Location, it._Unit, it._Date, it._Week, it._Note));
                }
            }
            else if (row is DCOrderLine)
            {
                lst = new List<DebtorOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (DCOrderLine)_it;
                    lst.Add(CreateNewOrderLine(it._Item, it._Qty, it._Text, it._Price, it._AmountEntered, it._DiscountPct, it._Discount, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5, it._Warehouse, it._Location, it._Unit,
                        it._Date, it._Week, it._Note));
                }
            }
            else if (row is InvItemClient)
            {
                lst = new List<DebtorOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    double qty = (double)_it.GetType().GetProperty("Qty").GetValue(_it, null);
                    var it = (InvItemClient)_it;
                    lst.Add(CreateNewOrderLine(it._Item, qty, null, 0d, 0d, 0d, 0d, null, null, null, null, null, null, null, 0, DateTime.MinValue, 0, null));
                }
            }
            return lst;
        }

        private DebtorOrderLineClient CreateNewOrderLine(string item, double qty, string text, double price, double amountEntered, double discPct, double disc, string variant1, string variant2, string variant3, string variant4, string variant5, string warehouse,
           string location, ItemUnit unit, DateTime date, byte week, string note)
        {
            var type = this.TableTypeUser;
            var orderline = Activator.CreateInstance(type) as DebtorOrderLineClient;
            orderline._Qty = qty;
            orderline._Item = item;
            orderline._Text = text;
            orderline._Price = price;
            orderline._AmountEntered = amountEntered;
            orderline._DiscountPct = discPct;
            orderline._Discount = disc;
            orderline._Variant1 = variant1;
            orderline._Variant2 = variant2;
            orderline._Variant3 = variant3;
            orderline._Variant4 = variant4;
            orderline._Variant5 = variant5;
            orderline._Warehouse = warehouse;
            orderline._Location = location;
            orderline._Unit = unit;
            orderline._Date = date;
            orderline._Week = week;
            orderline._Note = note;

            return orderline;
        }

        public override bool ClearSelectedItemOnSave
        {
            get
            {
                return false;
            }
        }
        public bool allowSave = true;
        public override bool AllowSave { get { return allowSave; } }
    }
    public partial class DebtorOrderLines : GridBasePage
    {
        SQLCache items, warehouse, standardVariants, variants1, variants2, employees;
        DebtorOrderClient Order { get { return dgDebtorOrderLineGrid.masterRecord as DebtorOrderClient; } }
        Uniconta.API.DebtorCreditor.FindPrices PriceLookup;
        Company company;
        double exchangeRate;

        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, Order);
            base.PageClosing();
        }
        public override string NameOfControl { get { return TabControls.DebtorOrderLines; } }
        public DebtorOrderLines(UnicontaBaseEntity master)
           : base(master)
        {
            Init(master);
        }
        public DebtorOrderLines(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }
        public void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            company = api.CompanyEntity;
            ((TableView)dgDebtorOrderLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            ((TableView)dgInvItemStorageClientGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgDebtorOrderLineGrid;
            SetRibbonControl(localMenu, dgDebtorOrderLineGrid);
            dgDebtorOrderLineGrid.api = api;
            dgInvItemStorageClientGrid.api = api;
            dgInvItemStorageClientGrid.ShowTotalSummary();
            SetupMaster(master);
            dgDebtorOrderLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorOrderLineGrid.SelectedItemChanged += DgDebtorOrderLineGrid_SelectedItemChanged;
            layOutDebtorOrderLine.Caption = Uniconta.ClientTools.Localization.lookup("OrdersLine");
            layOutInvItemStorage.Caption = Uniconta.ClientTools.Localization.lookup("OnHand");
            OnHandScreenInOrder = api.CompanyEntity._OnHandScreenInOrder;
            layOutInvItemStorage.Visibility = OnHandScreenInOrder ? Visibility.Visible : Visibility.Collapsed;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (!company.Production)
                UtilDisplay.RemoveMenuCommand(rb, "CreateProduction");
            InitialLoad();
            dgDebtorOrderLineGrid.ShowTotalSummary();
            dgDebtorOrderLineGrid.CustomSummary += dgDebtorOrderLineGrid_CustomSummary;
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += Page_KeyDown;
#else
            this.KeyDown += Page_KeyDown;
#endif
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
                localMenu_OnItemClicked("AddItems");
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void dgDebtorOrderLineGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case DevExpress.Data.CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case DevExpress.Data.CustomSummaryProcess.Calculate:
                    var row = e.Row as DebtorOrderLineClient;
                    sumSales += row.SalesValue;
                    sumMargin += row.Margin;
                    break;
                case DevExpress.Data.CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        sumMarginRatio = 100 * sumMargin / sumSales;
                        e.TotalValue = sumMarginRatio;
                    }
                    break;
            }
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            SetupMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetupMaster(UnicontaBaseEntity args)
        {
            PriceLookup = null;
            var OrderId = Order?.RowId;
            dgDebtorOrderLineGrid.UpdateMaster(args);
            if (Order?.RowId != OrderId)
                PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(Order, api);
        }

        void SetHeader()
        {
            var syncMaster = Order;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("OrdersLine"), syncMaster._OrderNumber, syncMaster._DCAccount);
            if (header != null)
                SetHeader(header);
        }
        bool OnHandScreenInOrder;
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = this.company;
            if (!company.Storage)
            {
                Storage.Visible = Storage.ShowInColumnChooser = false;
                QtyDelivered.Visible = QtyDelivered.ShowInColumnChooser = false;
            }
            else
            {
                Storage.ShowInColumnChooser = QtyDelivered.ShowInColumnChooser = true;
                QtyDelivered.AllowEditing = company._OrderLineEditDelivered ? DevExpress.Utils.DefaultBoolean.True : DevExpress.Utils.DefaultBoolean.False;
            }
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
            if (!company.SerialBatchNumbers)
                SerieBatch.Visible = SerieBatch.ShowInColumnChooser = false;
            else
                SerieBatch.ShowInColumnChooser = true;
            if (!company.Project)
            {
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
                Project.Visible = Project.ShowInColumnChooser = false;
            }
            else
                PrCategory.ShowInColumnChooser = Project.ShowInColumnChooser = true;
            if (!company.ProjectTask)
                Task.Visible = Task.ShowInColumnChooser = false;
            else
                Task.ShowInColumnChooser = true;
            SetVariantColumns();

            layOutInvItemStorage.Visibility = OnHandScreenInOrder ? Visibility.Visible : Visibility.Collapsed;

            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        void SetVariantColumns()
        {
            if (!company.ItemVariants)
                colVariant.Visible = colVariant.ShowInColumnChooser = false;
            var n = company.ItemVariants ? company.NumberOfVariants : 0;

            if (n >= 1)
                colVariant1.Header = colInvItemVariant1.Header = company._Variant1;
            else
                colVariant1.Visible = colVariant1.ShowInColumnChooser = colInvItemVariant1.Visible = colInvItemVariant1.ShowInColumnChooser = Variant1Name.ShowInColumnChooser = Variant1Name.Visible = false;

            if (n >= 2)
                colVariant2.Header = colInvItemVariant2.Header = company._Variant2;
            else
                colVariant2.Visible = colVariant2.ShowInColumnChooser = colInvItemVariant2.Visible = colInvItemVariant2.ShowInColumnChooser = Variant2Name.ShowInColumnChooser = Variant2Name.Visible = false;

            if (n >= 3)
                colVariant3.Header = colInvItemVariant3.Header = company._Variant3;
            else
                colVariant3.Visible = colVariant3.ShowInColumnChooser = colInvItemVariant3.Visible = colInvItemVariant3.ShowInColumnChooser = Variant3Name.ShowInColumnChooser = Variant3Name.Visible = false;

            if (n >= 4)
                colVariant4.Header = colInvItemVariant4.Header = company._Variant4;
            else
                colVariant4.Visible = colVariant4.ShowInColumnChooser = colInvItemVariant4.Visible = colInvItemVariant4.ShowInColumnChooser = Variant4Name.ShowInColumnChooser = Variant4Name.Visible = false;

            if (n >= 5)
                colVariant5.Header = colInvItemVariant5.Header = company._Variant5;
            else
                colVariant5.Visible = colVariant5.ShowInColumnChooser = colInvItemVariant5.Visible = colInvItemVariant5.ShowInColumnChooser = Variant5Name.ShowInColumnChooser = Variant5Name.Visible = false;
        }

        public override async void Utility_Refresh(string screenName, object argument)
        {
            if (screenName == TabControls.InvItemStoragePage && argument != null)
            {
                var storeloc = argument as InvItemStorageClient;
                if (storeloc == null) return;
                var selected = dgDebtorOrderLineGrid.SelectedItem as DCOrderLineClient;
                if (selected != null && (selected.Warehouse != storeloc.Warehouse || selected.Location != storeloc.Location))
                {
                    dgDebtorOrderLineGrid.SetLoadedRow(selected);
                    selected.Warehouse = storeloc.Warehouse;
                    selected.Location = storeloc.Location;
                    dgDebtorOrderLineGrid.SetModifiedRow(selected);
                    this.DataChaged = true;
                }
            }

            var param = argument as object[];
            if (param != null)
            {
                if (screenName == TabControls.AddMultipleInventoryItem)
                {
                    var orderNumber = (int)NumberConvert.ToInt(Convert.ToString(param[1]));
                    if (orderNumber == Order._OrderNumber)
                    {
                        if (dgDebtorOrderLineGrid.isDefaultFirstRow)
                        {
                            dgDebtorOrderLineGrid.DeleteRow();
                            dgDebtorOrderLineGrid.isDefaultFirstRow = false;
                        }
                        var invItems = param[0] as List<UnicontaBaseEntity>;
                        dgDebtorOrderLineGrid.PasteRows(invItems);
                    }
                }
                else if (screenName == TabControls.ItemVariantAddPage)
                {
                    var orderNumber = (int)NumberConvert.ToInt(Convert.ToString(param[1]));
                    if (orderNumber == Order._OrderNumber)
                    {
                        var invItems = param[0] as List<UnicontaBaseEntity>;
                        dgDebtorOrderLineGrid.PasteRows(invItems);
                    }
                }
                else if (screenName == TabControls.SerialToOrderLinePage)
                {
                    var orderLine = param[0] as DebtorOrderLineClient;
                    refreshOnHand = orderLine != null && orderLine.RowId == 0;
                    if (IsDataChaged)
                    {
                        var t = saveGrid();
                        if (t != null && orderLine.RowId == 0)
                            await t;
                    }
                    if (api.CompanyEntity.Warehouse)
                        dgDebtorOrderLineGrid.SetLoadedRow(orderLine);
                }
            }

            if (screenName == TabControls.RegenerateOrderFromProjectPage)
                InitQuery();
        }

        public bool DataChaged;

        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgInvItemStorageClientGrid);
            gridCtrls.Add(dgDebtorOrderLineGrid);
        }

        private void DgDebtorOrderLineGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as DebtorOrderLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= DebtorOrderLineGrid_PropertyChanged;
            var selectedItem = e.NewItem as DebtorOrderLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += DebtorOrderLineGrid_PropertyChanged;
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

        private void LoadInvItemStorageGrid(DebtorOrderLineClient selectedRow)
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

        public override void RowsPastedDone() { RecalculateAmount(); }

        public override void RowPasted(UnicontaBaseEntity rec)
        {
            var Comp = api.CompanyEntity;
            var orderLine = rec as DebtorOrderLineClient;
            if (orderLine == null)
                return;
            if (Comp._InvoiceUseQtyNow)
                orderLine.QtyNow = orderLine._Qty;
            if (orderLine._Item != null)
            {
                var selectedItem = (InvItem)items.Get(orderLine._Item);
                if (selectedItem != null)
                {
                    PriceLookup?.SetPriceFromItem(orderLine, selectedItem);
                    orderLine.SetItemValues(selectedItem, Comp._OrderLineStorage, true);
                    TableField.SetUserFieldsFromRecord(selectedItem, orderLine);
                }
                else
                    orderLine._Item = null;
            }
        }

        private void DebtorOrderLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as DebtorOrderLineClient;
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

                        var _priceLookup = this.PriceLookup;
                        this.PriceLookup = null; // avoid that we call priceupdated in property change on Qty
                        if (selectedItem._SalesQty != 0d)
                            rec.Qty = selectedItem._SalesQty;
                        else if (api.CompanyEntity._OrderLineOne)
                            rec.Qty = 1d;
                        rec.SetItemValues(selectedItem, api.CompanyEntity._OrderLineStorage);
                        this.PriceLookup = _priceLookup;
                        _priceLookup?.SetPriceFromItem(rec, selectedItem);

                        if (company._InvoiceUseQtyNow)
                            rec.QtyNow = rec._Qty;

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

                        globalEvents.NotifyRefreshViewer(NameOfControl, rec);
                    }
                    break;
                case "Qty":
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    if (company._InvoiceUseQtyNow)
                        rec.QtyNow = rec._Qty;
                    break;
                case "Subtotal":
                case "Total":
                    Dispatcher.BeginInvoke(new Action(() => { RecalculateAmount(); }));
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
                    DebtorOfferLines.FindOnEAN(rec, this.items, api, this.PriceLookup);
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
                case "Variant":
                    globalEvents.NotifyRefreshViewer(NameOfControl, rec);
                    break;
                case "CustomerItemNumber":
                    if (!string.IsNullOrEmpty(rec.CustomerItemNumber))
                        DebtorOfferLines.FindItemFromCustomerItem(rec, Order, api, rec.CustomerItemNumber);
                    break;
            }
        }

        async void setVariant(DebtorOrderLineClient rec, bool SetVariant2)
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
                                if (v2 == null)
                                {
                                    variants2 = await api.LoadCache(typeof(Uniconta.DataModel.InvVariant2), true);
                                    v2 = (InvVariant2)variants2.Get(cmb._Variant2);
                                }
                                invs2.Add(v2);
                                if (var2Value == v2._Variant)
                                    hasVariantValue = true;
                            }
                        }
                        else if (LastVariant != cmb._Variant1)
                        {
                            LastVariant = cmb._Variant1;
                            var v1 = (InvVariant1)variants1.Get(cmb._Variant1);
                            if (v1 == null)
                            {
                                variants1 = await api.LoadCache(typeof(Uniconta.DataModel.InvVariant1), true);
                                v1 = (InvVariant1)variants1.Get(cmb._Variant1);
                            }
                            if (v1 != null)
                                invs1.Add(v1);
                        }
                    }
                    if (SetVariant2)
                    {
                        rec.variant2Source = invs2;
                        if (!hasVariantValue)
                            rec.Variant2 = null;
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

        async void setLocation(InvWarehouse master, DebtorOrderLineClient rec)
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

            var selectedItem = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
            if (selectedItem?._Warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }

        void RecalculateAmount()
        {
            var ret = DebtorOfferLines.RecalculateLineSum(Order, (IEnumerable<DCOrderLineClient>)dgDebtorOrderLineGrid.ItemsSource, this.exchangeRate);
            double Amountsum = ret.Item1;
            double Costsum = ret.Item2;
            double sales = ret.Item3;
            if (Order != null && Order._EndDiscountPct != 0)
                sales *= (100d - Order._EndDiscountPct) / 100d;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            var strAmount = Uniconta.ClientTools.Localization.lookup("Amount");
            var strCost = Uniconta.ClientTools.Localization.lookup("CostValue");
            var strDB = Uniconta.ClientTools.Localization.lookup("DB");
            var strProjTotal = Uniconta.ClientTools.Localization.lookup("ProjectTotal");
            foreach (var grp in groups)
            {
                if (grp.Caption == strAmount)
                    grp.StatusValue = Amountsum.ToString("N2");
                else if (grp.Caption == strCost)
                    grp.StatusValue = Costsum.ToString("N2");
                else if (grp.Caption == strDB)
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
                else if (grp.Caption == strProjTotal)
                {
                    var projTotal = (Order != null && Order.ProjectTotal != 0d) ? Order.ProjectTotal : 0d;
                    grp.StatusValue = projTotal.ToString("N2");
                }
                else
                    grp.StatusValue = string.Empty;
            }
        }
        bool addingRow;
        private void localMenu_OnItemClicked(string ActionType)
        {
            DebtorOrderLineClient row;
            var selectedItem = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    addingRow = true;
                    row = dgDebtorOrderLineGrid.AddRow() as DebtorOrderLineClient;
                    row._ExchangeRate = this.exchangeRate;
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                    {
                        row = dgDebtorOrderLineGrid.CopyRow() as DebtorOrderLineClient;
                        row._ExchangeRate = this.exchangeRate;
                        row._CostPriceLine = selectedItem._CostPriceLine;
                        row._QtyDelivered = 0;
                        row._QtyInvoiced = 0;
                    }
                    break;
                case "SaveGrid":
                    saveGridLocal();
                    break;
                case "DeleteRow":
                    dgDebtorOrderLineGrid.DeleteRow();
                    break;
                case "ShowInvoice":
                case "CreateInvoice":
                    if (Order != null)
                    {
                        if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                            GenerateInvoice(Order, ActionType == "ShowInvoice" ? true : false);
                        else
                            UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    }
                    break;
                case "OrderConfirmation":
                    if (Order != null)
                        OrderConfirmation(Order, CompanyLayoutType.OrderConfirmation);
                    break;
                case "PackNote":
                    if (Order != null)
                        OrderConfirmation(Order, CompanyLayoutType.Packnote);
                    break;
                case "PickList":
                    if (Order != null)
                        PickingListReport(Order);
                    break;
                case "Storage":
                    ViewStorage();
                    break;
                case "Serial":
                    if (selectedItem?._Item != null)
                        LinkSerialNumber(selectedItem);
                    break;
                case "InsertSubTotal":
                    row = dgDebtorOrderLineGrid.AddRow() as DebtorOrderLineClient;
                    if (row != null)
                        row.Subtotal = true;
                    break;
                case "StockLines":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.DebtorInvoiceLines, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), selectedItem._Item));
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
                    object[] paramArr = new object[] { selectedItem };
                    AddDockItem(TabControls.CreditorOrderLineMarkedPage, paramArr, true);
                    break;
                case "MarkOrderLineAgnstInvTrans":
                    if (selectedItem?._Item == null) return;
                    saveGridLocal();
                    object[] param = new object[] { selectedItem };
                    AddDockItem(TabControls.InventoryTransactionsMarkedPage, param, true);
                    break;
                case "UnfoldBOM":
                    if (selectedItem != null)
                    {
                        CwUsePriceFromBOM cw = new CwUsePriceFromBOM();
#if !SILVERLIGHT
                        cw.DialogTableId = 2000000045;
#endif 
                        cw.Closing += delegate
                         {
                             if (cw.DialogResult == true)
                                 UnfoldBOM(selectedItem, cw.UsePricesFromBOM);
                         };
                        cw.Show();
                    }
                    break;
                case "CreateProduction":
                    if (selectedItem != null)
                        CreateProductionOrder(selectedItem);
                    break;
                case "AddItems":
                    if (this.items == null)
                        return;
                    object[] paramArray = new object[3] { new InvItemSalesCacheFilter(this.items), dgDebtorOrderLineGrid.TableTypeUser, Order };
                    AddDockItem(TabControls.AddMultipleInventoryItem, paramArray, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    break;
                case "EditOrder":
                    if (Order != null)
                        AddDockItem(TabControls.DebtorOrdersPage2, Order, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orders"), Order._OrderNumber));
                    break;
                case "ProjectTransaction":
                    if (Order != null)
                    {
                        saveGridLocal();
                        var o = Order;
                        AddDockItem(TabControls.DebtorOrderProjectLinePage, o, string.Format("{0}: {1} ({2})", Uniconta.ClientTools.Localization.lookup("ProjectAdjustments"), o._OrderNumber, o._Project));
                    }
                    break;
                case "RegenerateOrderFromProject":
                    if (Order != null)
                    {
                        var o = Order;
                        AddDockItem(TabControls.RegenerateOrderFromProjectPage, o, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("RegenerateOrder"), o._OrderNumber));
                    }
                    break;
                case "AddVariants":
                    var itm = selectedItem?.InvItem;
                    if (itm?._StandardVariant != null)
                    {
                        var paramItem = new object[] { selectedItem, Order };
                        dgDebtorOrderLineGrid.SetLoadedRow(selectedItem);
                        AddDockItem(TabControls.ItemVariantAddPage, paramItem, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Variants")), null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "CreateFromInvoice":
                    try
                    {
                        CWCreateOrderFromQuickInvoice createOrderCW = new CWCreateOrderFromQuickInvoice(api, Order.Account, true, Order);
                        createOrderCW.Closing += delegate
                       {
                           if (createOrderCW.DialogResult == true)
                           {
                               var orderApi = new OrderAPI(api);
                               var checkIfCreditNote = createOrderCW.chkIfCreditNote.IsChecked.HasValue ? createOrderCW.chkIfCreditNote.IsChecked.Value : false;
                               var debtorInvoice = createOrderCW.dgCreateOrderGrid.SelectedItem as DebtorInvoiceClient;
                               dgDebtorOrderLineGrid.PasteRows(createOrderCW.debtorOrderLines);
                           }
                       };
                        createOrderCW.Show();
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex);
                    }
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
                    break;
                case "InvStockProfile":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvStorageProfileReport, dgDebtorOrderLineGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), selectedItem._Item));
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
            RecalculateAmount();
        }

        async void CreateProductionOrder(DebtorOrderLineClient orderLine)
        {
            var t = saveGridLocal();
            if (t != null && orderLine.RowId == 0)
                await t;

            object[] arr = new object[3] { api, orderLine, dgDebtorOrderLineGrid.masterRecord };
            AddDockItem(TabControls.ProductionOrdersPage2, arr, Uniconta.ClientTools.Localization.lookup("Production"), "Add_16x16.png");
        }

        static bool showPrintPreview = true;
        private void OrderConfirmation(DebtorOrderClient dbOrder, CompanyLayoutType doctype)
        {
            var savetask = saveGridLocal();

            InvoiceAPI Invapi = new InvoiceAPI(api);
            var debtor = dbOrder.Debtor;
            bool showSendByMail = true;
            if (debtor != null)
                showSendByMail = (!string.IsNullOrEmpty(debtor.InvoiceEmail) || debtor.EmailDocuments);
            string debtorName = debtor?._Name ?? dbOrder._DCAccount;
            bool showUpdateInv = api.CompanyEntity.Storage || (doctype == CompanyLayoutType.Packnote && api.CompanyEntity.Packnote);
            bool invoiceInXML = debtor?._InvoiceInXML ?? false;
            var accountName = string.Format("{0} ({1})", dbOrder._DCAccount, dbOrder.Name);
            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, doctype.ToString(), isShowInvoiceVisible: true, askForEmail: true, showNoEmailMsg: !showSendByMail, debtorName: debtorName,
                isShowUpdateInv: showUpdateInv, isDebtorOrder: true, InvoiceInXML: invoiceInXML, AccountName: accountName);
#if !SILVERLIGHT
            if (doctype == CompanyLayoutType.OrderConfirmation)
                GenrateOfferDialog.DialogTableId = 2000000007;
            else if (doctype == CompanyLayoutType.Packnote)
                GenrateOfferDialog.DialogTableId = 2000000080;
#endif
            GenrateOfferDialog.SetInvPrintPreview(showPrintPreview);
            var additionalOrdersList = Utility.GetAdditionalOrders(api, dbOrder);
            if (additionalOrdersList != null)
                GenrateOfferDialog.SetAdditionalOrders(additionalOrdersList);
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

                    showPrintPreview = GenrateOfferDialog.ShowInvoice || GenrateOfferDialog.InvoiceQuickPrint || GenrateOfferDialog.SendByOutlook;
                    var invoicePostingGenerator = new InvoicePostingPrintGenerator(api, this);
                    var openOutlook = doctype == CompanyLayoutType.Packnote ? GenrateOfferDialog.UpdateInventory && GenrateOfferDialog.SendByOutlook : GenrateOfferDialog.SendByOutlook;
                    invoicePostingGenerator.SetUpInvoicePosting(dbOrder, null, doctype, GenrateOfferDialog.GenrateDate, null, !GenrateOfferDialog.UpdateInventory, GenrateOfferDialog.ShowInvoice, GenrateOfferDialog.PostOnlyDelivered,
                        GenrateOfferDialog.InvoiceQuickPrint, GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, openOutlook, GenrateOfferDialog.sendOnlyToThisEmail, GenrateOfferDialog.Emails,
                        false, null, false);
                    invoicePostingGenerator.SetAdditionalOrders(GenrateOfferDialog.AdditionalOrders?.Cast<DCOrder>().ToList());
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingGenerator.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                        DebtorOrders.Updatedata(dbOrder, doctype);
                    else
                        Utility.ShowJournalError(invoicePostingGenerator.PostingResult.ledgerRes, dgDebtorOrderLineGrid);
                }
            };
            GenrateOfferDialog.Show();
        }

        private void PickingListReport(DebtorOrderClient dbOrder)
        {
            var savetask = saveGridLocal();

            InvoiceAPI Invapi = new InvoiceAPI(api);
            var debtor = dbOrder.Debtor;
            string debtorName = string.Empty, accountName = string.Empty;
            bool showSendByMail = false;

            if (debtor != null)
            {
                debtorName = debtor._Name ?? dbOrder._DCAccount;
                accountName = string.Format("{0} ({1})", dbOrder._DCAccount, dbOrder.Name);
                showSendByMail = !string.IsNullOrEmpty(debtor.InvoiceEmail) || debtor.EmailDocuments;
            }

#if !SILVERLIGHT
            var cwPickingList = new CWGeneratePickingList(accountName, true, true, debtorName, showSendByMail);
            cwPickingList.DialogTableId = 2000000048;
#else
            var cwPickingList = new CWGeneratePickingList();
#endif
            cwPickingList.Closed += async delegate
            {
                if (cwPickingList.DialogResult == true)
                {
                    if (savetask != null)
                    {
                        var err = await savetask;
                        if (err != ErrorCodes.Succes)
                            return;
                    }

#if !SILVERLIGHT
                    var printDoc = cwPickingList.PrintDocument;
#else
                    var printDoc = false;
#endif
                    var invoicePostingGenerator = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingGenerator.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.PickingList, cwPickingList.SelectedDate, null, false, cwPickingList.ShowDocument, false, printDoc,
                        cwPickingList.NumberOfPages, cwPickingList.SendByEmail, cwPickingList.SendByOutlook, cwPickingList.sendOnlyToThisEmail, cwPickingList.EmailList, false, null, false);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingGenerator.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                        DebtorOrders.Updatedata(dbOrder, CompanyLayoutType.PickingList);
                    else
                        Utility.ShowJournalError(invoicePostingGenerator.PostingResult.ledgerRes, dgDebtorOrderLineGrid);
                }
            };
            cwPickingList.Show();
        }

        async void MarkedOrderLine(DebtorOrderLineClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            var orderLineMarked = new CreditorOrderLineClient();
            OrderAPI orderApi = new OrderAPI(api);
            var res = await orderApi.GetMarkedOrderLine(selectedItem, orderLineMarked);
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
            {
                object[] paramArr = new object[] { api, orderLineMarked };
                AddDockItem(TabControls.OrderLineMarkedPage, paramArr, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), orderLineMarked._OrderNumber));
            }
            else
                UtilDisplay.ShowErrorCode(res);
        }

        async void MarkedInvTrans(DebtorOrderLineClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            var invTrans = new InvTransClient();
            OrderAPI orderApi = new OrderAPI(api);
            var res = await orderApi.GetMarkedInvTrans(selectedItem, invTrans);
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
            {
                object[] paramArr = new object[] { api, invTrans };
                AddDockItem(TabControls.InvTransMarkedPage, paramArr, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), invTrans._OrderNumber));
            }
            else
                UtilDisplay.ShowErrorCode(res);
        }

        async void LinkSerialNumber(DebtorOrderLineClient orderLine)
        {
            var t = saveGridLocal();
            if (t != null && orderLine.RowId == 0)
                await t;
            if (api.CompanyEntity.Warehouse)
                dgDebtorOrderLineGrid.SetLoadedRow(orderLine); // serial page add warehouse and location
            AddDockItem(TabControls.SerialToOrderLinePage, dgDebtorOrderLineGrid.syncEntity, string.Format("{0}:{1}/{2},{3}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), orderLine.OrderRowId, orderLine._Item, orderLine.RowId));
        }

        async void RefreshGrid()
        {
            var savetask = saveGridLocal(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            gridRibbon_BaseActions("RefreshGrid");
        }

        async void ViewStorage()
        {
            var savetask = saveGridLocal(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            AddDockItem(TabControls.InvItemStoragePage, dgDebtorOrderLineGrid.syncEntity, true);
        }

        public override bool IsDataChaged
        {
            get
            {
                if (DataChaged)
                    return true;
                return base.IsDataChaged;
            }
        }

        private void GenerateInvoice(DebtorOrderClient dbOrder, bool showProformaInvoice)
        {
            var savetask = saveGridLocal();
            var curpanel = dockCtrl.Activpanel;

            bool showSendByMail = false;
            var debtor = ClientHelper.GetRef(dbOrder.CompanyId, typeof(Debtor), dbOrder._DCAccount) as Debtor;
            if (debtor != null)
            {
                var InvoiceAccount = dbOrder._InvoiceAccount ?? debtor._InvoiceAccount;
                if (InvoiceAccount != null)
                    debtor = ClientHelper.GetRef(dbOrder.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                if (debtor != null)
                {
                    if (debtor._PricesInclVat != dbOrder._PricesInclVat)
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }
                    if (!api.CompanyEntity.SameCurrency(dbOrder._Currency, debtor._Currency))
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("CurrencyMismatch"), AppEnums.Currencies.ToString((int)debtor._Currency), dbOrder.Currency),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }
                    showSendByMail = (!string.IsNullOrEmpty(debtor._InvoiceEmail) || debtor._EmailDocuments);
                }
            }

            if (showProformaInvoice)
            {
                ShowProformaInvoice(dbOrder);
                return;
            }

            string debtorName = debtor?._Name ?? dbOrder._DCAccount;
            bool invoiceInXML = debtor?._InvoiceInXML ?? false;
            var accountName = string.Format("{0} ({1})", dbOrder._DCAccount, dbOrder.Name);
            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(true, string.Empty, false, true, true, showNoEmailMsg: !showSendByMail, debtorName: debtorName, isOrderOrQuickInv: true, isDebtorOrder: true, InvoiceInXML: invoiceInXML, AccountName: accountName);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000008;
#endif
            if (dbOrder._InvoiceDate != DateTime.MinValue)
                GenrateInvoiceDialog.SetInvoiceDate(dbOrder._InvoiceDate);
            var additionalOrdersList = Utility.GetAdditionalOrders(api, dbOrder);
            if (additionalOrdersList != null)
                GenrateInvoiceDialog.SetAdditionalOrders(additionalOrdersList);
            GenrateInvoiceDialog.SetOIOUBLLabelText(api.CompanyEntity._OIOUBLSendOnServer);

            GenrateInvoiceDialog.Closed += async delegate
            {
                if (GenrateInvoiceDialog.DialogResult == true)
                {
                    if (savetask != null)
                    {
                        var err = await savetask;
                        if (err != ErrorCodes.Succes)
                            return;
                    }
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    InvoiceAPI Invapi = new InvoiceAPI(api);
                    var isSimulated = GenrateInvoiceDialog.IsSimulation;
                    var invoicePostingResult = SetupInvoicePostingPrintGenerator(dbOrder, GenrateInvoiceDialog.GenrateDate, isSimulated, GenrateInvoiceDialog.ShowInvoice, GenrateInvoiceDialog.PostOnlyDelivered, GenrateInvoiceDialog.InvoiceQuickPrint,
                        GenrateInvoiceDialog.NumberOfPages, GenrateInvoiceDialog.SendByEmail, !isSimulated && GenrateInvoiceDialog.SendByOutlook, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails,
                        GenrateInvoiceDialog.GenerateOIOUBLClicked);
                    invoicePostingResult.SetAdditionalOrders(GenrateInvoiceDialog.AdditionalOrders?.Cast<DCOrder>().ToList());

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (!result)
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOrderLineGrid);
                    else
                    {
                        Task reloadTask = null;
                        if (!GenrateInvoiceDialog.IsSimulation && dbOrder._DeleteLines)
                            reloadTask = Filter(null);

                        if (reloadTask != null)
                            CloseOrderLineScreen(reloadTask, curpanel);
                    }
                }
            };
            GenrateInvoiceDialog.Show();
        }

        async private void ShowProformaInvoice(DebtorOrderClient order)
        {
            var invoicePostingResult = SetupInvoicePostingPrintGenerator(order, DateTime.Now, true, true, false, false, 0, false, false, false, null, false);

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
            var result = await invoicePostingResult.Execute();
            busyIndicator.IsBusy = false;

            if (!result)
                Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgDebtorOrderLineGrid);

        }

        private InvoicePostingPrintGenerator SetupInvoicePostingPrintGenerator(DebtorOrderClient dbOrder, DateTime generateDate, bool isSimulation, bool showInvoice, bool postOnlyDelivered,
            bool isQuickPrint, int pagePrintCount, bool invoiceSendByEmail, bool invoiceSendByOutlook, bool sendOnlyToEmail, string sendOnlyToEmailList, bool OIOUBLgenerate)
        {
            var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
            invoicePostingResult.SetUpInvoicePosting(dbOrder, null, CompanyLayoutType.Invoice, generateDate, null, isSimulation, showInvoice, postOnlyDelivered, isQuickPrint, pagePrintCount,
                invoiceSendByEmail, !isSimulation && invoiceSendByOutlook, sendOnlyToEmail, sendOnlyToEmailList, OIOUBLgenerate, null, false);


            return invoicePostingResult;
        }

        async void CloseOrderLineScreen(Task reloadTask, DevExpress.Xpf.Docking.DocumentPanel panel)
        {
            await reloadTask;
            if (((IList)dgDebtorOrderLineGrid.ItemsSource).Count == 0)
            {
                globalEvents.OnRefresh(this.NameOfControl, Order);
                dockCtrl?.JustClosePanel(panel);
            }
            else
                RecalculateAmount();
        }

        bool refreshOnHand;
        Task<ErrorCodes> saveGridLocal()
        {
            var orderLine = dgDebtorOrderLineGrid.SelectedItem as DCOrderLine;
            refreshOnHand = orderLine != null && orderLine.RowId == 0;
            dgDebtorOrderLineGrid.SelectedItem = null;
            dgDebtorOrderLineGrid.SelectedItem = orderLine;
            if (IsDataChaged)
                return saveGrid();
            return null;
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            var orderLine = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
            if (dgDebtorOrderLineGrid.HasUnsavedData)
            {
                ErrorCodes res = await dgDebtorOrderLineGrid.SaveData();
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
            return ErrorCodes.Succes;
        }
        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgDebtorOrderLineGrid.Filter(propValuePair);
        }

        public async override Task InitQuery()
        {
            await Filter(null);
            var itemSource = (IList)dgDebtorOrderLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgDebtorOrderLineGrid.AddFirstRow();
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
            if (dgDebtorOrderLineGrid.IsLoadedFromLayoutSaved)
            {
                dgDebtorOrderLineGrid.ClearSorting();
                dgDebtorOrderLineGrid.IsLoadedFromLayoutSaved = false;
            }
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

        async void UnfoldBOM(DebtorOrderLineClient selectedItem, bool usePriceFromBOM)
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

                var type = dgDebtorOrderLineGrid.TableTypeUser;
                var Qty = selectedItem._Qty;
                var lst = new List<UnicontaBaseEntity>(list.Length);
                foreach (var bom in list)
                {
                    var invJournalLine = Activator.CreateInstance(type) as DebtorOrderLineClient;
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
                dgDebtorOrderLineGrid.PasteRows(lst);
                DataChaged = true;
                this.PriceLookup = pl;

                dgDebtorOrderLineGrid.SetLoadedRow(selectedItem);
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
                dgDebtorOrderLineGrid.SetModifiedRow(selectedItem);
            }
            busyIndicator.IsBusy = false;
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (this.items == null)
                this.items = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);

            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = api.GetCache(typeof(Uniconta.DataModel.InvWarehouse)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);
            if (Comp.ItemVariants)
            {
                if (this.standardVariants == null)
                    this.standardVariants = api.GetCache(typeof(Uniconta.DataModel.InvStandardVariant)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvStandardVariant)).ConfigureAwait(false);
                if (this.variants1 == null)
                    this.variants1 = api.GetCache(typeof(Uniconta.DataModel.InvVariant1)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvVariant1)).ConfigureAwait(false);
                if (this.variants2 == null)
                    this.variants2 = api.GetCache(typeof(Uniconta.DataModel.InvVariant2)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvVariant2)).ConfigureAwait(false);
            }

            if (this.PriceLookup == null && Order != null)
                PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(Order, api);
            var t = this.PriceLookup?.ExchangeTask;
            this.exchangeRate = this.PriceLookup != null ? this.PriceLookup.ExchangeRate : 0d;
            if (this.exchangeRate == 0d && t != null)
                this.exchangeRate = await t.ConfigureAwait(false);

            this.employees = api.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Employee)).ConfigureAwait(false);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                var selectedItem = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
                if (selectedItem != null)
                {
                    if (selectedItem.Variant1Source == null)
                        setVariant(selectedItem, false);
                    if (selectedItem.Variant2Source == null)
                        setVariant(selectedItem, true);
                }
            }));
            if (Comp.DeliveryAddress)
                LoadType(typeof(Uniconta.DataModel.WorkInstallation));
        }

        private void SerieBatch_GotFocus(object sender, RoutedEventArgs e)
        {
            var selItem = dgDebtorOrderLineGrid.SelectedItem as DebtorOrderLineClient;
            if (selItem == null || string.IsNullOrEmpty(selItem._Item))
                return;
            setSerieBatchSource(selItem);
        }
        async void setSerieBatchSource(DebtorOrderLineClient row)
        {
            var cache = api.CompanyEntity.GetCache(typeof(InvItem));
            var invItemMaster = cache.Get(row._Item) as InvItem;
            if (invItemMaster == null)
                return;
            if (row.SerieBatches != null && row.SerieBatches.First()._Item == row._Item)/*Bind if Item changed*/
                return;
            List<UnicontaBaseEntity> masters = null;
            if (row._Qty < 0)
            {
                masters = new List<UnicontaBaseEntity>() { invItemMaster };
            }
            else
            {
                // We only select opens
                var mast = new InvSerieBatchOpen();
                mast.SetMaster(invItemMaster);
                masters = new List<UnicontaBaseEntity>() { mast };
            }
            var res = await api.Query<SerialToOrderLineClient>(masters, null);
            if (res != null && res.Length > 0)
            {
                row.SerieBatches = res;
                row.NotifyPropertyChanged("SerieBatches");
            }
        }
    }
}
