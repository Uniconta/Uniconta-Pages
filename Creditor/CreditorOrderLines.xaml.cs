using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
#if !SILVERLIGHT
using UnicontaClient.Pages;
#endif
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorOrderLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorOrderLineClient); } }
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
            var selectedItem = (CreditorOrderLineClient)this.SelectedItem;
            return (selectedItem != null) && (selectedItem._Item != null || selectedItem._Text != null);
        }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            var row = copyFromRows.FirstOrDefault();
            List<CreditorOrderLineClient> lst = null;
            if (row is InvTrans)
            {
                lst = new List<CreditorOrderLineClient>();
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
                lst = new List<CreditorOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    var it = (DCOrderLine)_it;
                    lst.Add(CreateNewOrderLine(it._Item, it._Qty, it._Text, it._Price, it._AmountEntered, it._DiscountPct, it._Variant1, it._Variant2, it._Variant3, it._Variant4, it._Variant5,
                        it._Warehouse, it._Location, it._Unit, it._Date, it._Week, it._Note, it._Discount));
                }
            }
            else if (row is InvItem)
            {
                var qProp = row.GetType().GetProperty("Quantity");
                if (qProp == null)
                    qProp = row.GetType().GetProperty("PurchaseQty");
                lst = new List<CreditorOrderLineClient>();
                foreach (var _it in copyFromRows)
                {
                    double qty = Convert.ToDouble(qProp?.GetValue(_it, null));
                    var it = (InvItem)_it;
                    lst.Add(CreateNewOrderLine(it._Item, qty, null, 0.0d, 0.0d, 0.0d, null, null, null, null, null, null, null, 0, DateTime.MinValue, 0, null, 0));
                }
            }
            return lst;
        }

        private CreditorOrderLineClient CreateNewOrderLine(string item, double qty, string text, double price, double amountEntered, double discPct, string variant1, string variant2, string variant3, string variant4, string variant5, string warehouse,
            string location, ItemUnit unit, DateTime date, byte week, string note, double discount)
        {
            var type = this.TableTypeUser;
            var orderline = Activator.CreateInstance(type) as CreditorOrderLineClient;
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

    public partial class CreditorOrderLines : GridBasePage
    {
        SQLCache items, warehouse, standardVariants, variants1, variants2, ProjectCache;
        public override string NameOfControl { get { return TabControls.CreditorOrderLines; } }
        CreditorOrderClient orderMaster { get { return dgCreditorOrderLineGrid.masterRecord as CreditorOrderClient; } }
        Uniconta.API.DebtorCreditor.FindPrices PriceLookup;

        double exchangeRate;

        public CreditorOrderLines(SynchronizeEntity syncEntity)
           : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }
        public void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            ((TableView)dgCreditorOrderLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgCreditorOrderLineGrid;
            SetRibbonControl(localMenu, dgCreditorOrderLineGrid);
            dgCreditorOrderLineGrid.api = api;
            SetupMaster(master);
            dgCreditorOrderLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCreditorOrderLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;

            InitialLoad();
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

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            SetupMaster(args);
            SetHeader();
            InitQuery();
        }

        async void SetupMaster(UnicontaBaseEntity args)
        {
            PriceLookup = null;
            var OrderId = orderMaster?.RowId;
            dgCreditorOrderLineGrid.UpdateMaster(args);
            if (orderMaster.RowId != OrderId && api.CompanyEntity.CreditorPrice)
                PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(orderMaster, api);
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

        public CreditorOrderLines(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
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
            else
            {
                PrCategory.ShowInColumnChooser = true;
                Project.ShowInColumnChooser = true;
            }
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
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!company.SerialBatchNumbers)
                SerieBatch.Visible = SerieBatch.ShowInColumnChooser = false;
            else
                SerieBatch.ShowInColumnChooser = true;
            if (!company.ProjectTask)
                Task.Visible = Task.ShowInColumnChooser = false;
            else
                Task.ShowInColumnChooser = true;

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
                var selected = dgCreditorOrderLineGrid.SelectedItem as DCOrderLineClient;
                if (selected != null && (selected.Warehouse != storeloc.Warehouse || selected.Location != storeloc.Location))
                {
                    dgCreditorOrderLineGrid.SetLoadedRow(selected);
                    selected.Warehouse = storeloc.Warehouse;
                    selected.Location = storeloc.Location;
                    dgCreditorOrderLineGrid.SetModifiedRow(selected);
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
                    if (dgCreditorOrderLineGrid.isDefaultFirstRow)
                    {
                        dgCreditorOrderLineGrid.DeleteRow();
                        dgCreditorOrderLineGrid.isDefaultFirstRow = false;
                    }
                    var invItems = param[0] as List<UnicontaBaseEntity>;
                    dgCreditorOrderLineGrid.PasteRows(invItems);
                }
            }
            else if (screenName == TabControls.ItemVariantAddPage)
            {
                var orderNumber = (int)Uniconta.Common.Utility.NumberConvert.ToInt(Convert.ToString(param[1]));
                if (orderNumber == orderMaster._OrderNumber)
                {
                    var invItems = param[0] as List<UnicontaBaseEntity>;
                    dgCreditorOrderLineGrid.PasteRows(invItems);
                }
            }
            else if (screenName == TabControls.SerialToOrderLinePage)
            {
                var orderLine = param[0] as CreditorOrderLineClient;
                if (dgCreditorOrderLineGrid.HasUnsavedData)
                {
                    var t = saveGrid();
                    if (t != null && orderLine.RowId == 0)
                        await t;
                }
                if (api.CompanyEntity.Warehouse)
                    dgCreditorOrderLineGrid.SetLoadedRow(orderLine);
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
        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as CreditorOrderLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= CreditorOrderLineGrid_PropertyChanged;

            var selectedItem = e.NewItem as CreditorOrderLineClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += CreditorOrderLineGrid_PropertyChanged;
                if (selectedItem.Variant1Source == null)
                    setVariant(selectedItem, false);
                if (selectedItem.Variant2Source == null)
                    setVariant(selectedItem, true);
            }
        }
        async void setVariant(CreditorOrderLineClient rec, bool SetVariant2)
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
        async void setLocation(InvWarehouse master, CreditorOrderLineClient rec)
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

        public override void RowsPastedDone() { RecalculateAmount(); }

        public override void RowPasted(UnicontaBaseEntity rec)
        {
            var PriceLookup = this.PriceLookup;
            var Comp = api.CompanyEntity;
            var order = orderMaster;
            var orderLine = (CreditorOrderLineClient)rec;
            if (orderLine._Item != null)
            {
                if (Comp._InvoiceUseQtyNowCre)
                    orderLine.QtyNow = orderLine._Qty;
                var selectedItem = (InvItem)items.Get(orderLine._Item);
                if (selectedItem != null)
                {
                    if (PriceLookup != null)
                        PriceLookup.SetPriceFromItem(orderLine, selectedItem);
                    else if (selectedItem._PurchasePrice != 0 && Comp.SameCurrency(selectedItem._PurchaseCurrency, (byte)order._Currency))
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

        private void CreditorOrderLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (CreditorOrderLineClient)sender;
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
                        var _priceLookup = this.PriceLookup;
                        this.PriceLookup = null; // avoid that we call priceupdated in property change on Qty
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
                        if (_priceLookup != null)
                        {
                            this.PriceLookup = _priceLookup;
                            _priceLookup.SetPriceFromItem(rec, selectedItem);
                        }
                        else if (selectedItem._PurchasePrice != 0 && Comp.SameCurrency(selectedItem._PurchaseCurrency, (byte)orderMaster._Currency))
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
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    if (api.CompanyEntity._InvoiceUseQtyNowCre)
                        rec.QtyNow = rec._Qty;
                    break;
                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, (CreditorOrderLineClient)rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
                case "EAN":
                    DebtorOfferLines.FindOnEAN(rec, this.items, api, this.PriceLookup);
                    break;
                case "Total":
                    RecalculateAmount();
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
                case "Project":
                    lookupProjectDim(rec);
                    break;
                case "Task":
                    if (string.IsNullOrEmpty(rec._Project))
                        rec._Task = null;
                    break;
                case "CustomerItemNumber":
                    if (!string.IsNullOrEmpty(rec.CustomerItemNumber))
                        DebtorOfferLines.FindItemFromCustomerItem(rec, orderMaster, api, rec.CustomerItemNumber);
                    break;
            }
        }

        async void lookupProjectDim(CreditorOrderLineClient rec)
        {
            if (ProjectCache == null)
            {
                var api = this.api;
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project));
            }
            var proj = (Uniconta.DataModel.Project)ProjectCache?.Get(rec._Project);
            if (proj != null)
            {
                if (proj._Dim1 != null)
                    rec.Dimension1 = proj._Dim1;
                if (proj._Dim2 != null)
                    rec.Dimension2 = proj._Dim2;
                if (proj._Dim3 != null)
                    rec.Dimension3 = proj._Dim3;
                if (proj._Dim4 != null)
                    rec.Dimension4 = proj._Dim4;
                if (proj._Dim5 != null)
                    rec.Dimension5 = proj._Dim5;

                setTask(proj as ProjectClient, rec);
            }
        }

        void RecalculateAmount()
        {
            var ret = DebtorOfferLines.RecalculateLineSum((IList)dgCreditorOrderLineGrid.ItemsSource, this.exchangeRate);
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
            var selectedItem = dgCreditorOrderLineGrid.SelectedItem as CreditorOrderLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    selectedItem = dgCreditorOrderLineGrid.AddRow() as CreditorOrderLineClient;
                    selectedItem._ExchangeRate = this.exchangeRate;
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                    {
                        selectedItem = dgCreditorOrderLineGrid.CopyRow() as CreditorOrderLineClient;
                        selectedItem._QtyDelivered = 0;
                        selectedItem._QtyInvoiced = 0;
                        selectedItem._ExchangeRate = this.exchangeRate;
                    }
                    break;
                case "SaveGrid":
                    saveGridLocal();
                    break;
                case "DeleteRow":
                    dgCreditorOrderLineGrid.DeleteRow();
                    break;
                case "Storage":
                    ViewStorage();
                    break;
                case "Serial":
                    if (selectedItem != null)
                        LinkSerialNumber(selectedItem);
                    break;
                case "InsertSubTotal":
                    selectedItem = dgCreditorOrderLineGrid.AddRow() as CreditorOrderLineClient;
                    selectedItem.Subtotal = true;
                    break;
                case "StockLines":
                    if (selectedItem?._Item != null)
                        AddDockItem(TabControls.CreditorInvoiceLine, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), selectedItem._Item));
                    break;
                case "AddItems":
                    if (this.items == null)
                        return;
                    object[] paramArray = new object[3] { new InvItemPurchaseCacheFilter(this.items), dgCreditorOrderLineGrid.TableTypeUser, orderMaster };
                    AddDockItem(TabControls.AddMultipleInventoryItem, paramArray, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    break;
                case "MarkInvTrans":
                    if (selectedItem?._Item != null)
                        MarkedInvTrans(selectedItem);
                    break;
                case "MarkOrderLineAgnstInvTrans":
                    if (selectedItem?._Item == null) return;
                    saveGridLocal();
                    object[] param = new object[] { selectedItem };
                    AddDockItem(TabControls.InventoryTransactionsMarkedPage, param, true);
                    break;
                case "MarkOrderLine":
                    if (selectedItem?._Item != null)
                        MarkedOrderLine(selectedItem);
                    break;
                case "UpdateRequisition":
                    if (orderMaster != null)
                        OrderConfirmation(orderMaster, CompanyLayoutType.Requisition);
                    break;
                case "UpdatePurchaseOrder":
                    if (orderMaster != null)
                        OrderConfirmation(orderMaster, CompanyLayoutType.PurchaseOrder);
                    break;
                case "UpdateDeliveryNote":
                    if (orderMaster != null)
                        OrderConfirmation(orderMaster, CompanyLayoutType.PurchasePacknote);
                    break;
                case "ShowInvoice":
                case "CreateInvoice":
                    if (orderMaster != null)
                    {
                        if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                            GenerateInvoice(orderMaster, ActionType == "ShowInvoice" ? true : false);
                        else
                            UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
                    }
                    break;
                case "EditOrder":
                    AddDockItem(TabControls.CreditorOrdersPage2, orderMaster, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Orders"), orderMaster._OrderNumber));
                    break;
                case "UnfoldBOM":
                    if (selectedItem != null)
                    {
                        CwUsePriceFromBOM cw = new CwUsePriceFromBOM();
#if !SILVERLIGHT
                        cw.DialogTableId = 2000000062;
#endif 
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
                        dgCreditorOrderLineGrid.SetLoadedRow(selectedItem);
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
                    break;
                case "InvStockProfile":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvStorageProfileReport, dgCreditorOrderLineGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), selectedItem._Item));
                    break;
                case "ViewPhoto":
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

        async void UnfoldBOM(CreditorOrderLineClient selectedItem, bool usePriceFromBOM)
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

                var type = dgCreditorOrderLineGrid.TableTypeUser;
                var Qty = selectedItem._Qty;
                var lst = new List<UnicontaBaseEntity>(list.Length);
                foreach (var bom in list)
                {
                    var invJournalLine = Activator.CreateInstance(type) as CreditorOrderLineClient;
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
                dgCreditorOrderLineGrid.PasteRows(lst);
                this.DataChanged = true;
                this.PriceLookup = pl;

                dgCreditorOrderLineGrid.SetLoadedRow(selectedItem);

                double _AmountEntered = 0d;
                if (!usePriceFromBOM)
                    _AmountEntered = selectedItem._Amount;
                selectedItem.Price = 0; // will clear amountEntered
                if (!usePriceFromBOM)
                    selectedItem._AmountEntered = _AmountEntered;
                selectedItem.Qty = 0;
                selectedItem.DiscountPct = 0;
                selectedItem.Discount = 0;
                dgCreditorOrderLineGrid.SetModifiedRow(selectedItem);
            }
            busyIndicator.IsBusy = false;
        }

        static bool showInvPrintPreview = true;
        private void OrderConfirmation(CreditorOrderClient dbOrder, CompanyLayoutType doctype)
        {
            var savetask = saveGridLocal();
            InvoiceAPI Invapi = new InvoiceAPI(api);
            var creditor = dbOrder.Creditor;
            bool showSendByMail = true;
            if (creditor != null)
                showSendByMail = !string.IsNullOrEmpty(creditor._InvoiceEmail);
            string creditorName = creditor?._Name ?? dbOrder._DCAccount;
            var comp = api.CompanyEntity;
            bool showUpdateInv = comp.Storage || (doctype == CompanyLayoutType.PurchasePacknote && comp.CreditorPacknote);
            CWGenerateInvoice GenrateOfferDialog = new CWGenerateInvoice(false, doctype.ToString(), showInputforInvNumber: doctype == CompanyLayoutType.PurchasePacknote ? true : false, isShowInvoiceVisible: true,
                askForEmail: true, showNoEmailMsg: !showSendByMail, debtorName: creditorName, isShowUpdateInv: showUpdateInv);
#if !SILVERLIGHT
            switch (doctype)
            {
                case CompanyLayoutType.PurchaseOrder:
                    GenrateOfferDialog.DialogTableId = 2000000059;
                    break;
                case CompanyLayoutType.PurchasePacknote:
                    GenrateOfferDialog.DialogTableId = 2000000060;
                    break;
                case CompanyLayoutType.Requisition:
                    GenrateOfferDialog.DialogTableId = 2000000061;
                    break;
            }
#endif
            GenrateOfferDialog.SetInvPrintPreview(showInvPrintPreview);
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

                    showInvPrintPreview = GenrateOfferDialog.ShowInvoice || GenrateOfferDialog.InvoiceQuickPrint || GenrateOfferDialog.SendByOutlook;
                    string documentNumber = null;
                    if (doctype == CompanyLayoutType.PurchasePacknote)
                    {
                        documentNumber = GenrateOfferDialog.InvoiceNumber;
                        dbOrder._InvoiceNumber = documentNumber;
                    }

                    var openOutlook = doctype == CompanyLayoutType.PurchasePacknote ? GenrateOfferDialog.UpdateInventory && GenrateOfferDialog.SendByOutlook : GenrateOfferDialog.SendByOutlook;
                    var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
                    invoicePostingResult.SetUpInvoicePosting(dbOrder, null, doctype, GenrateOfferDialog.GenrateDate, documentNumber, !GenrateOfferDialog.UpdateInventory, GenrateOfferDialog.ShowInvoice, false,
                        GenrateOfferDialog.InvoiceQuickPrint, GenrateOfferDialog.NumberOfPages, GenrateOfferDialog.SendByEmail, GenrateOfferDialog.SendByOutlook, GenrateOfferDialog.sendOnlyToThisEmail,
                        GenrateOfferDialog.Emails, false, null, false);
                    invoicePostingResult.SetAdditionalOrders(GenrateOfferDialog.AdditionalOrders?.Cast<DCOrder>().ToList());

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                        DebtorOrders.Updatedata(dbOrder, doctype);
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgCreditorOrderLineGrid);
                }
            };
            GenrateOfferDialog.Show();
        }

        async void MarkedOrderLine(CreditorOrderLineClient selectedItem)
        {
            busyIndicator.IsBusy = true;
            var orderLineMarked = new DebtorOrderLineClient();
            OrderAPI orderApi = new OrderAPI(api);
            var res = await orderApi.GetMarkedOrderLine(selectedItem, orderLineMarked);
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
            {
                object[] paramArr = new object[] { api, orderLineMarked };
                AddDockItem(TabControls.OrderLineMarkedPage, paramArr, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Orderline"), orderLineMarked._OrderNumber));
            }
            else
                UtilDisplay.ShowErrorCode(res);
        }

        async void MarkedInvTrans(CreditorOrderLineClient selectedItem)
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

        async void LinkSerialNumber(CreditorOrderLineClient orderLine)
        {
            var item = (InvItem)items.Get(orderLine._Item);
            if (item == null || !item._UseSerialBatch)
                return;
            var t = saveGridLocal();
            if (t != null && orderLine.RowId == 0)
                await t;
            if (api.CompanyEntity.Warehouse)
                dgCreditorOrderLineGrid.SetLoadedRow(orderLine); // serial page add warehouse and location
            AddDockItem(TabControls.SerialToOrderLinePage, dgCreditorOrderLineGrid.syncEntity, string.Format("{0}:{1}/{2},{3}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), orderLine.OrderRowId, orderLine._Item, orderLine.RowId));
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
            var t = saveGridLocal();
            if (t != null)
                await t;
            AddDockItem(TabControls.InvItemStoragePage, dgCreditorOrderLineGrid.syncEntity, true);
        }

        private void GenerateInvoice(CreditorOrderClient dbOrder, bool showProformaInvoice)
        {
            var savetask = saveGridLocal();
            var curpanel = dockCtrl.Activpanel;

            var dc = ClientHelper.GetRef(dbOrder.CompanyId, typeof(Uniconta.DataModel.Creditor), dbOrder._DCAccount) as DCAccount;
            if (dc != null && !api.CompanyEntity.SameCurrency(dc._Currency, dbOrder._Currency))
            {
                var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("CurrencyMismatch"), AppEnums.Currencies.ToString((int)dc._Currency), dbOrder.Currency),
                Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OKCancel);
                if (confirmationMsgBox != MessageBoxResult.OK)
                    return;
            }

            if (showProformaInvoice)
            {
                ShowProformaInvoice(dbOrder);
                return;
            }

            var accountName = string.Format("{0} ({1})", orderMaster._DCAccount, orderMaster.Name);
            bool showSendByEmail = dc != null ? (!string.IsNullOrEmpty(dc._InvoiceEmail) || dc._EmailDocuments) : false;
            CWGenerateInvoice GenrateInvoiceDialog = new CWGenerateInvoice(true, string.Empty, true, true, showNoEmailMsg: !showSendByEmail, AccountName: accountName);
#if !SILVERLIGHT
            GenrateInvoiceDialog.DialogTableId = 2000000001;
#endif
            GenrateInvoiceDialog.SetSendAsEmailCheck(false);
            GenrateInvoiceDialog.SetInvoiceNumber(dbOrder._InvoiceNumber);
            if (dbOrder._InvoiceDate != DateTime.MinValue)
                GenrateInvoiceDialog.SetInvoiceDate(dbOrder._InvoiceDate);
            var additionalOrdersList = Utility.GetAdditionalOrders(api, dbOrder);
            if (additionalOrdersList != null)
                GenrateInvoiceDialog.SetAdditionalOrders(additionalOrdersList);

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

                    var isSimulated = GenrateInvoiceDialog.IsSimulation;
                    var invoicePostingResult = SetupInvoicePostingPrintGenerator(dbOrder, GenrateInvoiceDialog.GenrateDate, GenrateInvoiceDialog.InvoiceNumber, isSimulated, GenrateInvoiceDialog.ShowInvoice,
                        GenrateInvoiceDialog.InvoiceQuickPrint, GenrateInvoiceDialog.NumberOfPages, !isSimulated && GenrateInvoiceDialog.SendByOutlook, GenrateInvoiceDialog.sendOnlyToThisEmail, GenrateInvoiceDialog.Emails);
                    invoicePostingResult.SetAdditionalOrders(GenrateInvoiceDialog.AdditionalOrders?.Cast<DCOrder>().ToList());

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
                    busyIndicator.IsBusy = true;
                    var result = await invoicePostingResult.Execute();
                    busyIndicator.IsBusy = false;

                    if (result)
                    {
                        Task reloadTask = null;
                        if (!isSimulated && dbOrder._DeleteLines)
                            reloadTask = Filter(null);

                        if (reloadTask != null)
                            CloseOrderLineScreen(reloadTask, curpanel);

                    }
                    else
                        Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgCreditorOrderLineGrid);
                }
            };
            GenrateInvoiceDialog.Show();
        }

        private InvoicePostingPrintGenerator SetupInvoicePostingPrintGenerator(CreditorOrderClient crOrder, DateTime generateDate, string invoiceNumber, bool isSimulated, bool showInvoice,
            bool isQuickPrint, int printPageCount, bool sendInvoiceByOutlook, bool sendOnlyToEmail, string SendOnlyEmailList)
        {
            var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
            invoicePostingResult.SetUpInvoicePosting(crOrder, null, CompanyLayoutType.PurchaseInvoice, generateDate, invoiceNumber, isSimulated, showInvoice,
                        false, isQuickPrint, printPageCount, false, !isSimulated && sendInvoiceByOutlook, sendOnlyToEmail, SendOnlyEmailList, false, null, false);
            return invoicePostingResult;
        }

        async private void ShowProformaInvoice(CreditorOrderClient crOrder)
        {
            var invoicePostingResult = SetupInvoicePostingPrintGenerator(crOrder, DateTime.Now, null, true, true, false, 0, false, false, null);

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
            var result = await invoicePostingResult.Execute();
            busyIndicator.IsBusy = false;

            if (!result)
                Utility.ShowJournalError(invoicePostingResult.PostingResult.ledgerRes, dgCreditorOrderLineGrid);
        }

        async void CloseOrderLineScreen(Task reloadTask, DevExpress.Xpf.Docking.DocumentPanel debtOrderLinepanel)
        {
            await reloadTask;
            if (((IList)dgCreditorOrderLineGrid.ItemsSource).Count == 0)
            {
                globalEvents.OnRefresh(this.NameOfControl, orderMaster);
                dockCtrl?.JustClosePanel(debtOrderLinepanel);
            }
            else
                RecalculateAmount();
        }

        Task<ErrorCodes> saveGridLocal()
        {
            var orderLine = dgCreditorOrderLineGrid.SelectedItem as DCOrderLine;
            dgCreditorOrderLineGrid.SelectedItem = null;
            dgCreditorOrderLineGrid.SelectedItem = orderLine;
            if (dgCreditorOrderLineGrid.HasUnsavedData)
                return saveGrid();
            return null;
        }

        async void SaveLineToGetConversion(CreditorOrderLineClient rec)
        {
            rec._Price = 0;
            rec._Unit = 0;
            var tsk = dgCreditorOrderLineGrid.SaveData();
            if (tsk != null)
            {
                await tsk;
                rec.NotifyPropertyChanged("Qty");
                rec.NotifyPropertyChanged("Price");
                var ind = dgCreditorOrderLineGrid.GetVisibleRows().IndexOf(rec);
                if (ind >= 0)
                    dgCreditorOrderLineGrid.RefreshRow(ind);
            }
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            if (dgCreditorOrderLineGrid.HasUnsavedData)
            {
                ErrorCodes res = await dgCreditorOrderLineGrid.SaveData();
                if (res == ErrorCodes.Succes)
                {
                    DataChanged = false;
                    globalEvents.OnRefresh(NameOfControl, orderMaster);
                }
                return res;
            }
            return 0;
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgCreditorOrderLineGrid.Filter(propValuePair);
        }

        public async override Task InitQuery()
        {
            await Filter(null);
            var itemSource = (IList)dgCreditorOrderLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgCreditorOrderLineGrid.AddFirstRow();
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
            if (dgCreditorOrderLineGrid.IsLoadedFromLayoutSaved)
            {
                dgCreditorOrderLineGrid.ClearSorting();
                dgCreditorOrderLineGrid.IsLoadedFromLayoutSaved = false;
            }
        }

        protected override async void LoadCacheInBackGround()
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
            }
            if (Comp.ProjectTask)
                ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);

            var orderMaster = this.orderMaster;
            if (Comp.CreditorPrice)
            {
                if (PriceLookup == null)
                    PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(orderMaster, api);
            }
            else if (orderMaster != null && orderMaster._Currency != 0 && orderMaster._Currency != Comp._CurrencyId)
                exchangeRate = await api.session.ExchangeRate(Comp._CurrencyId, (Currencies)orderMaster._Currency, BasePage.GetSystemDefaultDate(), Comp).ConfigureAwait(false);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var selectedItem = dgCreditorOrderLineGrid.SelectedItem as CreditorOrderLineClient;
                if (selectedItem != null)
                {
                    if (selectedItem.Variant1Source == null)
                        setVariant(selectedItem, false);
                    if (selectedItem.Variant2Source == null)
                        setVariant(selectedItem, true);
                }
            }));
        }

        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgCreditorOrderLineGrid.SelectedItem as CreditorOrderLineClient;
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
        private void variant2_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant2 != null)
                prevVariant2.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant2 = editor;
            editor.isValidate = true;
        }

        CorasauGridLookupEditorClient prevTask;
        private void Task_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgCreditorOrderLineGrid.SelectedItem as CreditorOrderLineClient;
            if (selectedItem?._Project != null)
            {
                var selected = (ProjectClient)ProjectCache.Get(selectedItem._Project);
                setTask(selected, selectedItem);
                if (prevTask != null)
                    prevTask.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevTask = editor;
                editor.isValidate = true;
            }
        }

        async void setTask(ProjectClient project, CreditorOrderLineClient rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                    rec.taskSource = project.Tasks ?? await project.LoadTasks(api);
                else
                {
                    rec.taskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("TaskSource");
            }
        }
    }
}
