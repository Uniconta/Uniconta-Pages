using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using Uniconta.API.Inventory;
using Uniconta.API.Service;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ReOrderListPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ReOrderListPageGridClient); } }
        public override bool Readonly { get { return false; } }
    }

    public class ReOrderListPageGridClient : InvItemClient
    {
        public double _Quantity, _QtyReserved, _QtyOrdered, _QtyStock;
        internal double QtyExpanded;
        internal List<InvBOM> bomLst;

        // this is so we can press F6
        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItem))]
        [NoSQL]
        [Display(Name = "Item", ResourceType = typeof(InventoryText))]
        public string Item2 { get { return _Item; } }

        [Display(Name = "ToOrder", ResourceType = typeof(DCOrderText))]
        [NoSQL]
        public double Quantity { get { return _Quantity; } set { _Quantity = value; } }

        [Display(Name = "QtyReserved", ResourceType = typeof(InvItemStorageClientText))]
        [NoSQL]
        public double QtyResv { get { return _QtyReserved; } }

        [Display(Name = "QtyOrdered", ResourceType = typeof(InvItemStorageClientText))]
        [NoSQL]
        public double QtyOdr { get { return _QtyOrdered; } }

        [Display(Name = "InStock", ResourceType = typeof(InventoryText))]
        [NoSQL]
        public double QtyStock { get { return _QtyStock; } }

        [Display(Name = "Available", ResourceType = typeof(InvItemStorageClientText))]
        [NoSQL]
        public double QtyAvailable { get { return _QtyStock + _QtyOrdered - _QtyReserved; } }

        public string _Variant1, _Variant2, _Variant3, _Variant4, _Variant5;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvVariant1)), Key]
        [Display(Name = "Variant1", ResourceType = typeof(InvVariantClientsText))]
        public string Variant1 { get { return _Variant1; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvVariant2)), Key]
        [Display(Name = "Variant2", ResourceType = typeof(InvVariantClientsText))]
        public string Variant2 { get { return _Variant2; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvVariant3)), Key]
        [Display(Name = "Variant3", ResourceType = typeof(InvVariantClientsText))]
        public string Variant3 { get { return _Variant3; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvVariant4)), Key]
        [Display(Name = "Variant4", ResourceType = typeof(InvVariantClientsText))]
        public string Variant4 { get { return _Variant4; } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvVariant5)), Key]
        [Display(Name = "Variant5", ResourceType = typeof(InvVariantClientsText))]
        public string Variant5 { get { return _Variant5; } }

        [Display(Name = "Variant", ResourceType = typeof(InvVariantClientsText)), Key]
        [NoSQL]
        public string Variant
        {
            get
            {
                if (this._Variant5 != null)
                    return string.Format("{0};{1};{2};{3};{4}", _Variant1, _Variant2, _Variant3, _Variant4, _Variant5);
                if (this._Variant4 != null)
                    return string.Format("{0};{1};{2};{3}", _Variant1, _Variant2, _Variant3, _Variant4);
                if (this._Variant3 != null)
                    return string.Format("{0};{1};{2}", _Variant1, _Variant2, _Variant3);
                if (this._Variant2 != null)
                    return string.Concat(_Variant1, ";", _Variant2);
                return _Variant1;
            }
        }

        [Display(Name = "VariantNames", ResourceType = typeof(InvVariantClientsText))]
        [NoSQL]
        public string VariantName
        {
            get
            {
                if (this._Variant5 != null)
                    return string.Format("{0};{1};{2};{3};{4}", Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
                if (this._Variant4 != null)
                    return string.Format("{0};{1};{2};{3}", Variant1Name, Variant2Name, Variant3Name, Variant4Name);
                if (this._Variant3 != null)
                    return string.Format("{0};{1};{2}", Variant1Name, Variant2Name, Variant3Name);
                if (this._Variant2 != null)
                    return string.Concat(Variant1Name, ";", Variant2Name);
                if (this._Variant1 != null)
                    return Variant1Name;
                return null;
            }
        }

        [Display(Name = "Variant1Name", ResourceType = typeof(InvVariantClientsText))]
        [NoSQL]
        public string Variant1Name { get { return ClientHelper.GetNameKey(CompanyId, typeof(InvVariant1), _Variant1); } }

        [Display(Name = "Variant2Name", ResourceType = typeof(InvVariantClientsText))]
        [NoSQL]
        public string Variant2Name { get { return ClientHelper.GetNameKey(CompanyId, typeof(InvVariant2), _Variant2); } }

        [Display(Name = "Variant3Name", ResourceType = typeof(InvVariantClientsText))]
        [NoSQL]
        public string Variant3Name { get { return ClientHelper.GetNameKey(CompanyId, typeof(InvVariant3), _Variant3); } }

        [Display(Name = "Variant4Name", ResourceType = typeof(InvVariantClientsText))]
        [NoSQL]
        public string Variant4Name { get { return ClientHelper.GetNameKey(CompanyId, typeof(InvVariant4), _Variant4); } }

        [Display(Name = "Variant5Name", ResourceType = typeof(InvVariantClientsText))]
        [NoSQL]
        public string Variant5Name { get { return ClientHelper.GetNameKey(CompanyId, typeof(InvVariant5), _Variant5); } }

        public string _Project;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        [Display(Name = "Project", ResourceType = typeof(PrStandardCategoryText))]
        public string Project { get { return _Project; } set { _Project = value; NotifyPropertyChanged("Project"); } }

        public bool _Expanded;
               

        [Display(Name = "Added", ResourceType = typeof(InventoryText))]
        public bool Added { get { return _Expanded; } }

        string invPurchaseAccount;
        [Display(Name = "InvPurchaseAccount", ResourceType = typeof(InventoryText))]
        public String InvPurchaseAccount { get { return invPurchaseAccount; }  set { invPurchaseAccount = value; NotifyPropertyChanged("InvPurchaseAccount"); } }

        internal object invPurchaseAccSource;
        public object InvPurchaseAccSource { get { return invPurchaseAccSource; } }

        [ReportingAttribute]
        public InvItemClient InvItem
        {
            get
            {
                return ClientHelper.GetRefClient<InvItemClient>(CompanyId, typeof(Uniconta.DataModel.InvItem), RowId);
            }
        }
    }

    public partial class ReOrderListPage : GridBasePage
    {
        public ReOrderListPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public ReOrderListPage(UnicontaBaseEntity _master)
            : base(_master)
        {
            InitPage(_master);
        }

        public override bool IsDataChaged { get { return false; } }
        bool _useStorage;
        static public bool ReorderPrWarehouse, ReorderPrLocation;

        private void InitPage(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            dgReOrderList.UpdateMaster(_master);
            dgReOrderList.ShowTotalSummary();
            localMenu.dataGrid = dgReOrderList;
            SetRibbonControl(localMenu, dgReOrderList);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            localMenu.OnChecked += LocalMenu_OnChecked;
            var Comp = api.CompanyEntity;
            _useStorage = Comp.Storage;
            if (!_useStorage)
                QtyResv.Visible = QtyOdr.Visible = QtyAvailable.Visible = false;

            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
            {
                if (!Comp.InvBOM)
                {
                    UtilDisplay.RemoveMenuCommand(rb, "ReportAsFinished");
                    UtilDisplay.RemoveMenuCommand(rb, "UnfoldBOM");
#if !SILVERLIGHT
                    Added.Visible = false;
#endif
                }

                if (!Comp.Location || !Comp.Warehouse || !Comp.Storage)
                    UtilDisplay.RemoveMenuCommand(rb, "PerLocation");
                else
                {
                    var rbMenuLocation = UtilDisplay.GetMenuCommandByName(rb, "PerLocation");
                    rbMenuLocation.IsChecked = ReorderPrLocation;
                }

                if (!Comp.Warehouse || !Comp.Storage)
                {
                    UtilDisplay.RemoveMenuCommand(rb, "PerWarehouse");
                    UtilDisplay.RemoveMenuCommand(rb, "MoveFromWarehouse");
                }
                else
                {
                    var rbMenuWarehouse = UtilDisplay.GetMenuCommandByName(rb, "PerWarehouse");
                    rbMenuWarehouse.IsChecked = ReorderPrWarehouse;
                }

                if (!Comp.Production)
                {
                    UtilDisplay.RemoveMenuCommand(rb, "ProductionOrders");
                    UtilDisplay.RemoveMenuCommand(rb, "ProductionLines");
                    UtilDisplay.RemoveMenuCommand(rb, "CreateProductionOrder");
                }
                if (!Comp.PurchaseAccounts)
                {
                    InvPurchaseAccount.Visible = InvPurchaseAccount.ShowInColumnChooser = false;
                }
            }
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;

            if (!Comp.Location || !Comp.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!Comp.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;

            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);

        }

        private void LocalMenu_OnChecked(string ActionType, bool IsChecked)
        {
            switch (ActionType)
            {
                case "PerWarehouse":
                    ReorderPrWarehouse = IsChecked;
                    break;
                case "PerLocation":
                    ReorderPrLocation = IsChecked;
                    break;
            }
        }
        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return false;
        }
        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgReOrderList.SelectedItem as InvItemClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    if (selectedItem != null)
                        dgReOrderList.DeleteRow();
                    break;
                case "Filter":
                    var pairs = ribbonControl.filterValues;
                    var sort = ribbonControl.PropSort;
                    Filter(pairs, sort, false);
                    break;
                case "ClearFilter":
                    Filter(null, null, false);
                    break;
                case "UnfoldBOM":
                    ExpandBOM();
                    break;
                case "CreateOrder":
                    CwPurOrderDfltVal dailog = new CwPurOrderDfltVal(api);
                    dailog.WarehouseCheck = ReorderPrWarehouse;
                    dailog.LocationCheck = ReorderPrLocation;
                    dailog.Closed += delegate
                    {
                        if (dailog.DialogResult == true)
                        {
                            CreateOrder(dailog.DefaultCreditorOrder, dailog.OrderLinePerWareHouse, dailog.OrderLinePerLocation);
                        }
                    };
                    dailog.Show();
                    break;
                case "ReportAsFinished":
                    dgReOrderList.SelectedItem = null;
                    CwInvJournal journals = new CwInvJournal(api);
                    journals.Closed += delegate
                    {
                        if (journals.DialogResult == true)
                        {
                            ReportAsFinished(journals.InvJournal);
                        }
                    };
                    journals.Show();
                    break;
                case "AddItem":
                    CWInventoryItems cw = new CWInventoryItems(api);
                    cw.Closed += delegate
                    {
                        if (cw.DialogResult == true)
                        {
                            if (cw.InvItem != null)
                            {
                                var item = new ReOrderListPageGridClient();
                                StreamingManager.Copy(cw.InvItem, item);
                                dgReOrderList.PasteRows(new [] { item });
                            }
                        }
                    };
                    cw.Show();
                    break;
                case "CreateProductionOrder":
                    CwPurOrderDfltVal cWindow = new CwPurOrderDfltVal(api, isProdOrder: true);
                    cWindow.Closed += delegate
                    {
                        if (cWindow.DialogResult == true)
                            CreateProductionOrder(cWindow.DefaultProductionOrder, cWindow.CreatePurchaseLines, cWindow.Storage);
                    };
                    cWindow.Show();
                    break;
                case "InvTrans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryTransactions, dgReOrderList.syncEntity);
                    break;
                case "OrderLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOrderLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OrderLines"), selectedItem._Item));
                    break;
                case "PurchaseLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem._Item));
                    break;
                case "ProductionOrders":
                    if (selectedItem != null && selectedItem._ItemType == (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                        AddDockItem(TabControls.ProductionOrders, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionOrders"), selectedItem._Item));
                    break;
                case "ProductionLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProductionOrderLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionLines"), selectedItem._Item));
                    break;
                case "MoveFromWarehouse":
                    dgReOrderList.SelectedItem = null;
                    CwMoveBtwWareHouse cwJournal = new CwMoveBtwWareHouse(api);
                    cwJournal.Closed += delegate
                    {
                        if (cwJournal.DialogResult == true)
                            MoveBetweenWarehouse(cwJournal.InvJournal, cwJournal.Warehouse, cwJournal.Location);
                    };
                    cwJournal.Show();
                    break;
                case "Storage":
                    AddDockItem(TabControls.InvItemStoragePage, dgReOrderList.syncEntity, true);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "InvStockProfile":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvStorageProfileReport, dgReOrderList.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), selectedItem._Item));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override Task InitQuery()
        {
            return Filter(null, null, true);
        }

        bool isCorrectType;
        InvItemStorageClientSort reservedSort;
        Uniconta.DataModel.InvItemStorage[] Reserved;
        Uniconta.DataModel.InvItemStorage searchrec;

        private async Task Filter(IEnumerable<PropValuePair> propValuePair, FilterSorter PropSort, bool firstTime)
        {
            SetBusy();
            Task< Uniconta.DataModel.InvItemStorage[]> ReservedTask;

            DateTime FromDate = txtDateFrm.DateTime, ToDate = txtDateTo.DateTime;

            if (_useStorage)
                ReservedTask = api.Query<Uniconta.DataModel.InvItemStorage>();
            else
                ReservedTask = null;

            IEnumerable<InvItem> lstEntity;
            if (propValuePair != null)
            {
                var query = await api.Query<ReOrderListPageGridClient>(propValuePair);
                Array.Sort(query, new SearchItem());
                lstEntity = query;
                isCorrectType = true;
            }
            else
            {
                var Comp = api.CompanyEntity;
                var cache = Comp.GetCache(typeof(InvItem));
                if (cache == null || (ReservedTask == null && (DateTime.UtcNow - cache.Loaded).TotalMinutes > 10))
                    cache = await Comp.LoadCache(typeof(InvItem), api, true);
                this.items = cache;
                lstEntity = (IEnumerable<InvItem>)cache?.GetKeyStrRecords;
                isCorrectType = false;
            }

            reservedSort = null;
            Reserved = null;
            searchrec = null;
            if (ReservedTask != null)
            {
                Uniconta.DataModel.DCOrderLineStorageRef[] ExclOrders = null;
                if (FromDate != DateTime.MinValue || ToDate != DateTime.MinValue)
                {
                    var filter = new List<PropValuePair>(2);
                    if (FromDate != DateTime.MinValue)
                        filter.Add(PropValuePair.GenereteParameter("FromDate", typeof(string), NumberConvert.ToString(FromDate.Ticks)));
                    if (ToDate != DateTime.MinValue)
                        filter.Add(PropValuePair.GenereteParameter("ToDate", typeof(string), NumberConvert.ToString(ToDate.Ticks)));
                    ExclOrders = await api.Query<Uniconta.DataModel.DCOrderLineStorageRef>(filter);
                }

                Reserved = await ReservedTask;
                if (Reserved != null)
                {
                    reservedSort = new InvItemStorageClientSort();
                    Array.Sort(Reserved, reservedSort);
                    searchrec = new Uniconta.DataModel.InvItemStorage();
                    if (! ReOrderListPage.ReorderPrWarehouse)
                        ReOrderListPage.ReorderPrLocation = false;

                    if (ExclOrders != null && ExclOrders.Length > 0)
                    {
                        for(int i = 0; (i < ExclOrders.Length); i++)
                        {
                            var ord = ExclOrders[i];
                            if (ord._MoveType > 0)
                            {
                                searchrec._Item = ord._Item;
                                searchrec._Variant1 = ord._Variant1;
                                searchrec._Variant2 = ord._Variant2;
                                searchrec._Variant3 = ord._Variant3;
                                searchrec._Variant4 = ord._Variant4;
                                searchrec._Variant5 = ord._Variant5;
                                searchrec._Warehouse = ord._Warehouse;
                                searchrec._Location = ord._Location;
                                var pos = Array.BinarySearch(Reserved, searchrec, reservedSort);
                                if (pos >= 0 && pos < Reserved.Length)
                                {
                                    var r = Reserved[pos];
                                    if (ord._MoveType == 1)
                                        r._QtyReserved -= (ord._Qty - ord._QtyDelivered);
                                    else
                                        r._QtyOrdered -= (ord._Qty - ord._QtyDelivered);
                                }
                            }
                        }
                    }
                }
            }

            if (firstTime)
                StartLoadCache();

            var lst = new List<ReOrderListPageGridClient>(200);
            if (lstEntity != null)
            {
                foreach (var rec in lstEntity)
                {
                    if (rec._ItemType == (byte)Uniconta.DataModel.ItemType.Service)
                        continue;
                    if (rec._HideInPurchase && rec._ItemType != (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                        continue;

                    AddEntry(lst, rec);
                 }
            }

            dgReOrderList.ItemsSource = lst;
            dgReOrderList.Visibility = Visibility.Visible;

            ClearBusy();
        }

        ReOrderListPageGridClient AddEntry(List<ReOrderListPageGridClient> lst, InvItem rec)
        {
            ReOrderListPageGridClient varrec;

            bool found = false;
            double QtyReserved = 0d, QtyOrdered = 0d, Qty = 0d;
            double MinStockLevel = rec._MinStockLevel;
            double MaxStockLevel = rec._MaxStockLevel;
            string Variant1 = null, Variant2 = null, Variant3 = null, Variant4 = null, Variant5 = null, Warehouse = null, Location = null;
            if (searchrec != null)
            {
                var _useVariants = rec._UseVariants;
                searchrec._Item = rec._Item;
                var pos = Array.BinarySearch(Reserved, searchrec, reservedSort);
                if (pos < 0)
                    pos = ~pos;

                for (; (pos < Reserved.Length); pos++)
                {
                    var r = Reserved[pos];
                    if (r._Item != rec._Item)
                        break;
                    if (_useVariants || ReorderPrWarehouse)
                    {
                        if (!found || Variant1 != r._Variant1 || Variant2 != r._Variant2 || Variant3 != r._Variant3 || Variant4 != r._Variant4 || Variant5 != r._Variant5 ||
                            (ReorderPrWarehouse && r._Warehouse != Warehouse) ||
                            (ReorderPrLocation && r._Location != Location))
                        {
                            if (found)
                            {
                                varrec = new ReOrderListPageGridClient();
                                StreamingManager.Copy(rec, varrec);
                                varrec._Qty = 0d;
                                varrec._Item = rec._Item;
                                varrec._Variant1 = Variant1;
                                varrec._Variant2 = Variant2;
                                varrec._Variant3 = Variant3;
                                varrec._Variant4 = Variant4;
                                varrec._Variant5 = Variant5;
                                if (ReorderPrWarehouse)
                                {
                                    varrec._Warehouse = Warehouse;
                                    if (ReorderPrLocation)
                                        varrec._Location = Location;
                                }
                                AddToLst(lst, varrec, QtyReserved, QtyOrdered, Qty, MinStockLevel, MaxStockLevel);
                                QtyReserved = 0d; QtyOrdered = 0d; Qty = 0d;
                            }
                            Variant1 = r._Variant1;
                            Variant2 = r._Variant2;
                            Variant3 = r._Variant3;
                            Variant4 = r._Variant4;
                            Variant5 = r._Variant5;
                            if (ReorderPrWarehouse)
                            {
                                Warehouse = r._Warehouse;
                                if (ReorderPrLocation)
                                    Location = r._Location;
                            }
                            if (r._MinStockLevel != 0 || r._MaxStockLevel != 0)
                            {
                                MinStockLevel = r._MinStockLevel;
                                MaxStockLevel = r._MaxStockLevel;
                            }
                            else
                            {
                                MinStockLevel = rec._MinStockLevel;
                                MaxStockLevel = rec._MaxStockLevel;
                            }
                        }
                    }

                    Qty += r._Qty;
                    QtyOrdered += r._QtyOrdered;
                    QtyReserved += r._QtyReserved;
                    found = true;
                }
            }

            if (!found)
                Qty = rec._qtyOnStock;

            if (isCorrectType)
                varrec = (ReOrderListPageGridClient)rec;
            else
            {
                varrec = new ReOrderListPageGridClient();
                StreamingManager.Copy(rec, varrec);
            }

            varrec._Variant1 = Variant1;
            varrec._Variant2 = Variant2;
            varrec._Variant3 = Variant3;
            varrec._Variant4 = Variant4;
            varrec._Variant5 = Variant5;
            if (ReorderPrWarehouse)
            {
                varrec._Warehouse = Warehouse;
                if (ReorderPrLocation)
                    varrec._Location = Location;
            }
            AddToLst(lst, varrec, QtyReserved, QtyOrdered, Qty, MinStockLevel, MaxStockLevel);
            return varrec;
        }

        bool SetPurchaseQty(ReOrderListPageGridClient rec, double MinStockLevel, double MaxStockLevel)
        {
            var Available = rec._QtyStock + rec._QtyOrdered - rec._QtyReserved;
            if (MaxStockLevel == 0d)
                MaxStockLevel = MinStockLevel;
            if (Available < MinStockLevel)
            {
                var PurchaseQty = rec._PurchaseQty;
                if (PurchaseQty > 0d && PurchaseQty != 1d)
                {
                    rec._Quantity = Math.Max(PurchaseQty, rec._PurchaseMin);

                    var dif = MinStockLevel - (rec._Quantity + Available);
                    if (dif > 0)
                    {
                        rec._Quantity += Math.Round(dif / PurchaseQty) * PurchaseQty;
                        if (rec._Quantity + Available < MinStockLevel)
                            rec._Quantity += PurchaseQty;
                    }

                    dif = MaxStockLevel - (rec._Quantity + PurchaseQty + Available);
                    if (dif > 0)
                    {
                        rec._Quantity += Math.Round(dif / PurchaseQty) * PurchaseQty;
                        if (rec._Quantity + PurchaseQty + Available < MaxStockLevel)
                            rec._Quantity += PurchaseQty;
                    }
                }
                else
                    rec._Quantity = Math.Max(rec._PurchaseMin, MaxStockLevel - Available);
                return true;
            }
            return false;
        }

        void AddToLst(List<ReOrderListPageGridClient> lst, ReOrderListPageGridClient rec, double QtyReserved, double QtyOrdered, double Qty, double MinStockLevel, double MaxStockLevel)
        {
            rec._QtyStock = Qty;
            rec._QtyOrdered = QtyOrdered;
            rec._QtyReserved = QtyReserved;
            rec._MinStockLevel = MinStockLevel;
            rec._MaxStockLevel = MaxStockLevel;
            if (SetPurchaseQty(rec, MinStockLevel, MaxStockLevel))
                lst.Add(rec);
        }

        SQLCache items;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            if (this.items == null)
                this.items = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            if (api.CompanyEntity.Warehouse)
                LoadType(new Type[] { typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.InvWarehouse), typeof(Uniconta.DataModel.InvJournal) });
            else
                LoadType(new Type[] { typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.InvJournal) });
        }

        class SearchItem : IComparer<ReOrderListPageGridClient>
        {
            public int Compare(ReOrderListPageGridClient x, ReOrderListPageGridClient y)
            {  
                var c = string.Compare(x._Item, y._Item);
                if (c != 0)
                    return c;
                c = string.Compare(x._Variant1, y._Variant1);
                if (c != 0)
                    return c;
                c = string.Compare(x._Variant2, y._Variant2);
                if (c != 0)
                    return c;
                c = string.Compare(x._Variant3, y._Variant3);
                if (c != 0)
                    return c;
                c = string.Compare(x._Variant4, y._Variant4);
                if (c != 0)
                    return c;
                c = string.Compare(x._Variant5, y._Variant5);
                if (c != 0)
                    return c;
                c = string.Compare(x._Warehouse, y._Warehouse);
                if (c != 0)
                    return c;
                return string.Compare(x._Location, y._Location);
            }
        }

        async void ExpandBOM()
        {
            var rows = dgReOrderList.ItemsSource as List<ReOrderListPageGridClient>;

            var newItems = new List<ReOrderListPageGridClient>(100);
            var cmp = new SearchItem();
            var key = new ReOrderListPageGridClient();
            bool anyFound = false;

            InvBOM[] bomArr = null;
            int bomlen = 0;
            var bomSort = new InvBOMSort();
            var bomSearch = new InvBOM();
            bomSearch._LineNumber = float.MinValue;
            isCorrectType = false;

            var looprows = dgReOrderList.GetVisibleRows() as IList<ReOrderListPageGridClient>;
            if (looprows == null || looprows.Count == 0)
                return;

            var bomLst = new List<InvBOM>(100);
            for (int loop = 10; (--loop >= 0);) // we loop up to 10 times to reserved nested BOMs
            {
                int i = -1;
                bool loopFound = false;
                foreach (var rec in looprows)
                {
                    i++;
                    if (rec._ItemType < (byte)Uniconta.DataModel.ItemType.BOM)
                        continue;
                    var QtyReserved = rec._QtyReserved;
                    if (QtyReserved <= 0d)
                        continue;
                    if (rec.QtyExpanded >= QtyReserved)
                        continue;

                    if (rec.bomLst == null)
                    {
                        if (bomArr == null)
                        {
                            SetBusy();
                            bomArr = await api.Query<Uniconta.DataModel.InvBOM>();
                            if (bomArr == null || bomArr.Length == 0)
                                return;
                            bomlen = bomArr.Length;
                            Array.Sort(bomArr, bomSort);
                        }

                        var item = rec._Item;
                        bomSearch._ItemMaster = item;
                        var idx = Array.BinarySearch(bomArr, bomSearch, bomSort);
                        if (idx < 0)
                            idx = ~idx;
                        for (; (idx < bomlen); idx++)
                        {
                            var r = bomArr[idx];
                            if (r._ItemMaster != item)
                                break;
                            r._ItemMaster = item;
                            bomLst.Add(r);
                        }
                        rec.bomLst = new List<InvBOM>(bomLst);
                        bomLst.Clear();
                    }

                    QtyReserved -= rec.QtyExpanded;
                    rec.QtyExpanded = rec._QtyReserved;

                    foreach (var part in rec.bomLst)
                    {
                        var itemPart = part._ItemPart;
                        var variant1 = part._Variant1;
                        var variant2 = part._Variant2;
                        var variant3 = part._Variant3;
                        var variant4 = part._Variant4;
                        var variant5 = part._Variant5;
                        var qtyRes = part.GetBOMQty(QtyReserved);

                        ReOrderListPageGridClient recItem;
                        key._Item = itemPart;
                        key._Variant1 = variant1;
                        key._Variant2 = variant2;
                        key._Variant3 = variant3;
                        key._Variant4 = variant4;
                        key._Variant5 = variant5;
                        var idx = rows.BinarySearch(key, cmp);
                        if (idx >= 0 && idx < rows.Count)
                            recItem = rows[idx];
                        else
                        {
                            recItem = null;
                            foreach (var it in newItems)
                            {
                                if (it._Item == itemPart && it._Variant1 == variant1 && it._Variant2 == variant2 && it._Variant3 == variant3 && it._Variant4 == variant4 && it._Variant5 == variant5)
                                {
                                    recItem = it;
                                    break;
                                }
                            }
                            if (recItem == null)
                            {
                                var item = (InvItem)items.Get(itemPart);
                                if (item == null || item._ItemType == (byte)Uniconta.DataModel.ItemType.Service)
                                    continue;

                                recItem = AddEntry(newItems, item);
                                if (newItems.Count == 0 || !object.ReferenceEquals(recItem, newItems[newItems.Count-1]))
                                    newItems.Add(recItem);
                            }
                        }
                        recItem._QtyReserved += qtyRes;
                        recItem._Expanded = true;
                        loopFound = true;
                    }
                }
                if (!loopFound)
                    break;
            }

            if (newItems.Count > 0)
            {
                foreach(var rec in newItems)
                {
                    if (SetPurchaseQty(rec, rec.MinStockLevel, rec.MaxStockLevel))
                    {
                        rows.Add(rec);
                        anyFound = true;
                    }
                }
                if (anyFound)
                {
                    rows.Sort(cmp);
                    dgReOrderList.RefreshData();
                }
            }
            ClearBusy();
        }

        private async void CreateOrder(Uniconta.DataModel.CreditorOrder dfltCreditorOrder, bool PrWarehouse, bool PrLocation)
        {
            var rows = dgReOrderList.GetVisibleRows() as ICollection<ReOrderListPageGridClient>;
            if (rows == null || rows.Count == 0)
                return;
            var accounts = (from rec in rows where rec._PurchaseAccount != null select rec._PurchaseAccount).Distinct().ToList();

            var Comp = api.CompanyEntity;
            var defaultStorage = Comp._PurchaseLineStorage;
            var CompCur = Comp._Currency;
            var Creditors = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));

            var creditorOrders = new List<CreditorOrderClient>(accounts.Count);
            foreach (var acc in accounts)
            {
                var creditor = (Uniconta.DataModel.Creditor)Creditors.Get(acc);
                if (creditor != null)
                {
                    var ord = this.CreateGridObject(typeof(CreditorOrderClient)) as CreditorOrderClient;
                    ord.SetMaster(creditor);
                    ord._OurRef = dfltCreditorOrder._OurRef;
                    ord._Remark = dfltCreditorOrder._Remark;
                    ord._Group = dfltCreditorOrder._Group;
                    ord._DeliveryDate = dfltCreditorOrder._DeliveryDate;
                    if (dfltCreditorOrder._Employee != null)
                        ord._Employee = dfltCreditorOrder._Employee;

                    TableField.SetUserFieldsFromRecord(creditor, ord);

                    creditorOrders.Add(ord);

                    if (creditor._DeliveryAddress1 != null)
                    {
                        ord.DeliveryName = creditor._DeliveryName;
                        ord._DeliveryAddress1 = creditor._DeliveryAddress1;
                        ord._DeliveryAddress2 = creditor._DeliveryAddress2;
                        ord._DeliveryAddress3 = creditor._DeliveryAddress3;
                        ord.DeliveryCity = creditor._DeliveryCity;
                        ord.DeliveryZipCode = creditor._DeliveryZipCode;
                        if (creditor._DeliveryCountry != 0)
                            ord._DeliveryCountry = creditor._DeliveryCountry;
                    }
                }
            }

            var oldval = busyIndicator.BusyContent;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("CreatingInProcess");
            busyIndicator.IsBusy = true;

            var HasWarehouse = Comp.Warehouse;
            if (PrLocation)
                PrWarehouse = true;
            if (!PrWarehouse)
                PrLocation = false;

            var items = this.items;
            var result = await api.Insert(creditorOrders);
            if (result == ErrorCodes.Succes)
            {
                var PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(api);
                var _InvoiceUseQtyNowCre = Comp._InvoiceUseQtyNowCre;

                var orderList = new List<CreditorOrderLineClient>(creditorOrders.Count * 2);
                foreach (var order in creditorOrders)
                {
                    await PriceLookup.OrderChanged(order);

                    var acc = order._DCAccount;
                    var creditor = (Uniconta.DataModel.Creditor)Creditors.Get(acc);
                    int line = 0;
                    foreach (var item in rows)
                    {
                        if (item._Quantity > 0d && item._PurchaseAccount == acc)
                        {
                            if (HasWarehouse && (!PrWarehouse || !PrLocation))
                            {
                                bool found = false;
                                foreach(var lin in orderList)
                                {
                                    if (lin._Item == item._Item && lin._Variant1 == item._Variant1 && lin._Variant2 == item._Variant2 && lin._Variant3 == item._Variant3 && lin._Variant4 == item._Variant4 && lin._Variant5 == item._Variant5 &&
                                        (!PrWarehouse || lin._Warehouse == item._Warehouse) &&
                                        (!PrLocation || lin._Location == item._Location))
                                    {
                                        lin._Qty += item._Quantity;
                                        if (_InvoiceUseQtyNowCre)
                                            lin._QtyNow = lin._Qty;
                                        found = true;
                                        break;
                                    }
                                }
                                if (found)
                                    continue;
                            }

                            var orderLine = new CreditorOrderLineClient();
                            orderLine._LineNumber = ++line;
                            orderLine._Item = item._Item;
                            orderLine._Variant1 = item._Variant1;
                            orderLine._Variant2 = item._Variant2;
                            orderLine._Variant3 = item._Variant3;
                            orderLine._Variant4 = item._Variant4;
                            orderLine._Variant5 = item._Variant5;
                            orderLine._Warehouse = item._Warehouse;
                            orderLine._Location = item._Location;
                            orderLine._Qty = item._Quantity;
                            if (_InvoiceUseQtyNowCre)
                                orderLine._QtyNow = orderLine._Qty;
                            orderLine._Storage = defaultStorage;
                            orderLine._DiscountPct = creditor._LineDiscountPct;
                            if (item._Project != null)
                            {
                                orderLine._Project = item._Project;
                                orderLine._PrCategory = item._PrCategory;
                            }
                            orderLine.SetMaster(order);
                            TableField.SetUserFieldsFromRecord(orderLine, item);
                            orderList.Add(orderLine);
                        }
                    }
                    DCOrderLine last = null;
                    foreach(var lin in orderList)
                    {
                        var item = (InvItem)items.Get(lin._Item);
                        if (item == null)
                            continue;
                        if (PrWarehouse && lin._Warehouse == null)
                        {
                            lin._Warehouse = item._Warehouse;
                            if (PrLocation && lin._Location == null)
                                lin._Location = item._Location;
                        }
                        if (last != null && last._Item == lin._Item)
                        {
                            if (!PrWarehouse && last._Warehouse != lin._Warehouse)
                            {
                                lin._Warehouse = null;
                                lin._Location = null;
                                last._Warehouse = null;
                                last._Location = null;
                            }
                            else if (!PrLocation && last._Location != lin._Location)
                                last._Location = lin._Location = null;
                        }
                        last = lin;
                        if (item._UnitGroup == null || item._PurchaseUnit == 0 || item._PurchaseUnit == item._Unit)
                            lin._Unit = item._Unit;

                        var t = PriceLookup.SetPriceFromItem(lin, item);
                        if (t != null)
                            await t;

                        if (item._UnitGroup != null && (lin._Price == item._PurchasePrice || lin._Price == item._CostPrice))
                        {
                            lin._Price = 0;  // Server will set it
                            lin._Unit = 0;
                        }
                    }
                }
                api.InsertNoResponse(orderList);
            }
            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = oldval;

            UtilDisplay.ShowErrorCode(result);
        }

        private async void CreateProductionOrder(Uniconta.DataModel.ProductionOrder dfltProductionOrder, bool createProdLines, int storage)
        {
            var rows = dgReOrderList.GetVisibleRows() as ICollection<ReOrderListPageGridClient>;
            if (rows == null || rows.Count == 0)
                return;

            var productionOrders = new List<ProductionOrderClient>();
            foreach (var rec in rows)
            {
                if (rec._Quantity <= 0 || rec._ItemType != (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                    continue;

                var ord = this.CreateGridObject(typeof(ProductionOrderClient)) as ProductionOrderClient;
                ord.SetMaster(rec);
                ord._OurRef = dfltProductionOrder._OurRef;
                ord._Remark = dfltProductionOrder._Remark;
                ord._Group = dfltProductionOrder._Group;
                ord._DeliveryDate = dfltProductionOrder._DeliveryDate;
                ord._Shipment = dfltProductionOrder._Shipment;
                ord._Employee = dfltProductionOrder._Employee;
                ord._ProdQty = Math.Max(Math.Max(rec._PurchaseQty, rec._PurchaseMin), rec._Quantity);
                ord._Storage = (StorageRegister)storage;
                TableField.SetUserFieldsFromRecord(rec, ord);
                productionOrders.Add(ord);
            }

            ErrorCodes result;
            if (productionOrders.Count == 0)
                result = ErrorCodes.NoLinesFound;
            else
            {
                busyIndicator.IsBusy = true;
                result = await api.Insert(productionOrders);
                if (result == ErrorCodes.Succes && createProdLines)
                {
                    var prodAPI = new ProductionAPI(api);
                    foreach (var production in productionOrders)
                        result = await prodAPI.CreateProductionLines(production, (StorageRegister)storage);
                }
                busyIndicator.IsBusy = false;
            }
            UtilDisplay.ShowErrorCode(result);
        }

        async void ReportAsFinished(Uniconta.DataModel.InvJournal invJournal)
        {
            if (invJournal == null) return;
            var lst = dgReOrderList.GetVisibleRows() as IEnumerable<ReOrderListPageGridClient>;
            var invJournalLineList = new List<InvJournalLine>();
            foreach (var item in lst)
            {
                if (item._Quantity > 0 && item._ItemType == (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                {
                    var journalLine = new InvJournalLine();
                    journalLine._MovementType = Uniconta.DataModel.InvMovementType.ReportAsFinished;
                    journalLine._Item = item._Item;
                    journalLine._Variant1 = item._Variant1;
                    journalLine._Variant2 = item._Variant2;
                    journalLine._Variant3 = item._Variant3;
                    journalLine._Variant4 = item._Variant4;
                    journalLine._Variant5 = item._Variant5;
                    journalLine._Warehouse = item._Warehouse;
                    journalLine._Location = item._Location;
                    journalLine._CostPrice = item._CostPrice;
                    journalLine._Qty = item._Quantity;
                    journalLine.SetMaster(invJournal);
                    journalLine._Dim1 = invJournal._Dim1;
                    journalLine._Dim2 = invJournal._Dim2;
                    journalLine._Dim3 = invJournal._Dim3;
                    journalLine._Dim4 = invJournal._Dim4;
                    journalLine._Dim5 = invJournal._Dim5;
                    invJournalLineList.Add(journalLine);
                }
            }

            ErrorCodes result;
            if (invJournalLineList.Count == 0)
                result = ErrorCodes.NoLinesFound;
            else
            {
                api.AllowBackgroundCrud = true;
                result = await api.Insert(invJournalLineList);
            }
            UtilDisplay.ShowErrorCode(result);
        }

        async void MoveBetweenWarehouse(Uniconta.DataModel.InvJournal invJournal, string FromWarehouse, string FromLocation)
        {
            if (invJournal == null) return;
            var mainList = dgReOrderList.GetVisibleRows() as IEnumerable<ReOrderListPageGridClient>;
            var invJournalLineList = new List<InvJournalLine>();
            foreach (var item in mainList)
            {
                if (item._Quantity > 0d)
                {
                    var journalLine = new InvJournalLine();
                    journalLine._MovementType = Uniconta.DataModel.InvMovementType.Project; // this is a warehouse movement in journal
                    journalLine._Item = item._Item;
                    journalLine._Variant1 = item._Variant1;
                    journalLine._Variant2 = item._Variant2;
                    journalLine._Variant3 = item._Variant3;
                    journalLine._Variant4 = item._Variant4;
                    journalLine._Variant5 = item._Variant5;

                    if (FromWarehouse != null)
                    {
                        journalLine._Warehouse = FromWarehouse;
                        journalLine._Location = FromLocation;
                    }
                    else
                    {
                        var itm = (InvItem)this.items.Get(item._Item);
                        if (itm != null)
                        {
                            journalLine._Warehouse = itm._Warehouse;
                            journalLine._Location = itm._Location;
                        }
                    }
                    journalLine._WarehouseTo = item._Warehouse;
                    journalLine._LocationTo = item._Location;
                    journalLine._Qty = item._Quantity;  // move from and to is always positive.
                    journalLine.SetMaster(invJournal);
                    journalLine._Dim1 = invJournal._Dim1;
                    journalLine._Dim2 = invJournal._Dim2;
                    journalLine._Dim3 = invJournal._Dim3;
                    journalLine._Dim4 = invJournal._Dim4;
                    journalLine._Dim5 = invJournal._Dim5;
                    invJournalLineList.Add(journalLine);
                }
            }

            ErrorCodes result;
            if (invJournalLineList.Count == 0)
                result = ErrorCodes.NoLinesFound;
            else
            {
                api.AllowBackgroundCrud = true;
                result = await api.Insert(invJournalLineList);
            }
            UtilDisplay.ShowErrorCode(result);
        }

        CorasauGridLookupEditorClient prevInvPurAcc;
        private void InvPurchaseAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgReOrderList.SelectedItem as ReOrderListPageGridClient;
            if (selectedItem != null)
            {
                SetInvPurAccountSource(selectedItem);
                if (prevInvPurAcc != null)
                    prevInvPurAcc.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevInvPurAcc = editor;
                editor.isValidate = true;
            }
        }

        async void SetInvPurAccountSource(ReOrderListPageGridClient rec)
        {
            if (api.CompanyEntity.PurchaseAccounts)
            {
                rec.invPurchaseAccSource = await api.Query<InvPurchaseAccountClient>(rec);
                rec.NotifyPropertyChanged("InvPurchaseAccSource");
            }
        }
    }
}
