using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.ComponentModel;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using UnicontaClient.Models;
using Uniconta.DataModel;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvStockAccountingReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvItemStorageCount); } }
        public override IComparer GridSorting { get { return new InvItemStorageLocalSort(); } }
        public override bool Readonly { get { return false; } }
    }

    internal class InvItemStorageLocalSort : IComparer, IComparer<InvItemStorageCount>
    {
        public int Compare(object _x, object _y) { return Compare((InvItemStorageCount)_x, (InvItemStorageCount)_y); }

        public int Compare(InvItemStorageCount x, InvItemStorageCount y)
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
            c = string.Compare(x._SerieBatch, y._SerieBatch);
            if (c != 0)
                return c;
            c = string.Compare(x._Warehouse, y._Warehouse);
            if (c != 0)
                return c;
            return string.Compare(x._Location, y._Location);
        }
    }

    public class InvItemStorageCount : InvItemStorageClient
    {
        public double? _Quantity;
        [NoSQL]
        [Display(Name = "Counted", ResourceType = typeof(DCOrderText))]
        public double? Quantity { get { return _Quantity; } set { _Quantity = value; NotifyPropertyChanged("Quantity"); NotifyPropertyChanged("Difference"); } }

        [NoSQL]
        [Display(Name = "Difference", ResourceType = typeof(DCOrderText))]
        public double Difference { get { return _Quantity.HasValue ? _Quantity.Value - _Qty : 0d; } }

        [NoSQL]
        [Display(Name = "ItemName", ResourceType = typeof(InvSumsText))]
        public new string ItemName { get { return itemRec?._Name; } }

        [NoSQL]
        [AppEnumAttribute(EnumName = "ItemUnit")]
        [Display(Name = "Unit", ResourceType = typeof(InventoryText))]
        public string Unit { get { var rec = itemRec; return rec != null ? AppEnums.ItemUnit.ToString((int)rec._Unit) : null; } }

        [NoSQL]
        [AppEnumAttribute(EnumName = "ItemType")]
        [Display(Name = "ItemType", ResourceType = typeof(InventoryText)), Key]
        public string ItemType { get { var rec = itemRec; return (rec != null) ? AppEnums.ItemType.ToString(rec._ItemType) : null; } }

        [NoSQL]
        [Display(Name = "ItemGroup", ResourceType = typeof(InvSumsText))]
        public string ItemGroup { get { return itemRec?._Group; } }

        [NoSQL]
        [Display(Name = "ItemLocation", ResourceType = typeof(InventoryText))]
        public string StockPosition { get { return itemRec?._StockPosition; } }

        [NoSQL]
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        [Display(Name = "PurchaseAccount", ResourceType = typeof(InventoryText))]
        public String PurchaseAccount { get { return itemRec?._PurchaseAccount; } }

        internal string _SerieBatch;
        [NoSQL]
        [Display(Name = "SerieBatch", ResourceType = typeof(InvSerieBatchText))]
        public string SerieBatch { get { return _SerieBatch; } }

        Uniconta.DataModel.InvItem _itemRec;
        internal Uniconta.DataModel.InvItem itemRec
        {
            get
            {
                return _itemRec ?? (_itemRec = (Uniconta.DataModel.InvItem)ClientHelper.GetRef(CompanyId, typeof(Uniconta.DataModel.InvItem), _Item));
            }
        }

        internal object locationSource;
        public object LocationSource { get { return locationSource; } }
    }

    public partial class InvStockCountingReport : GridBasePage
    {
        public InvStockCountingReport(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public InvStockCountingReport(UnicontaBaseEntity _master)
            : base(_master)
        {
            Init(_master);
        }

        SQLCache items, warehouse;

        private void Init(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            dgInvStockStatus.UpdateMaster(_master);
            ((TableView)dgInvStockStatus.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            SetRibbonControl(localMenu, dgInvStockStatus);
            dgInvStockStatus.api = api;
            dgInvStockStatus.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvStockStatus.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            this.items = api.GetCache(typeof(Uniconta.DataModel.InvItem));
            this.warehouse = api.GetCache(typeof(Uniconta.DataModel.InvWarehouse));
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvItemStorageCount;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvItemStorageClientGrid_PropertyChanged;
            var selectedItem = e.NewItem as InvItemStorageCount;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvItemStorageClientGrid_PropertyChanged;
        }
       
        private void InvItemStorageClientGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvItemStorageCount;
            if (rec.CompanyId == 0)
                rec.SetMaster(api.CompanyEntity);
            switch (e.PropertyName)
            {
                case "Item":
                    var selectedItem = (Uniconta.DataModel.InvItem)items?.Get(rec._Item);
                    if (selectedItem != null)
                    {
                        rec.NotifyPropertyChanged("ItemType");
                        rec.NotifyPropertyChanged("Unit");
                        rec.NotifyPropertyChanged("ItemGroup");
                        rec.NotifyPropertyChanged("ItemLocation");
                        rec.NotifyPropertyChanged("PurchaseAccount");

                        rec.SetItemValues(selectedItem, api.CompanyEntity._OrderLineStorage);
                        if (selectedItem._Qty != 0)
                        {
                            rec._Qty = selectedItem._Qty;
                            rec.NotifyPropertyChanged("Qty");
                            rec.NotifyPropertyChanged("Difference");
                        }
                    }
                    break;
                 case "Warehouse":
                    var selected = (InvWarehouse)warehouse?.Get(rec._Warehouse);
                    if (selected != null)
                        setLocation(selected, rec);
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
            }
        }

        async void setLocation(InvWarehouse master, InvItemStorageCount rec)
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
            var api = this.api;

            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);

            if (api.CompanyEntity.Warehouse && this.warehouse == null)
                this.warehouse = await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);

            LoadType(typeof(Uniconta.DataModel.InvJournal));
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvStockStatus.SelectedItem as InvItemStorageCount;

            switch (ActionType)
            {
                case "DeleteRow":
                    if (selectedItem != null)
                        dgInvStockStatus.DeleteRow();
                    break;
                case "PostJournal":
                    PostInvJournal();
                    break;
                case "AddRow":
                    dgInvStockStatus.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        dgInvStockStatus.CopyRow();
                    break;
                case "OrderLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOrderLineReport, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("OrderLines"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("OnHand")));
                    break;
                case "PurchaseLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem.ItemName, Uniconta.ClientTools.Localization.lookup("OnHand")));
                    break;
                case "Storage":
                    AddDockItem(TabControls.InvItemStoragePage, dgInvStockStatus.syncEntity, true, Uniconta.ClientTools.Localization.lookup("OnHand"));
                    break;
                case "LoadSerieBatch":
                    LoadSerieBatch();
                    break;
                case "Filter":
                    var rb = baseRibbon;
                    var pairs = rb.filterValues;
                    BindGrid(pairs);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        public override void PageClosing()
        {
            var selected = dgInvStockStatus.SelectedItem as Uniconta.DataModel.InvItemStorage;
            if (selected != null)
                globalEvents.OnRefresh(TabControls.InvItemStoragePage, selected);
            base.PageClosing();
        }

        bool batchLoaded;
        async void LoadSerieBatch()
        {
            if (batchLoaded)
                return;
            BusyIndicator.IsBusy = true;
            var lst = await api.Query<InvSerieBatch>(new InvSerieBatchOpen());
            if (lst == null || lst.Length == 0)
            {
                BusyIndicator.IsBusy = false;
                return;
            }

            var extraItems = new List<InvItemStorageCount>();
            var mainList = (List<InvItemStorageCount>)dgInvStockStatus.ItemsSource;
            var search = new InvItemStorageCount();
            var sort = new InvItemStorageLocalSort();
            var cnt = mainList.Count;
            foreach (var rec in lst)
            {
                var itm = rec._Item;
                search._Item = itm;
                var idx = mainList.BinarySearch(search, sort);
                if (idx < 0)
                    idx = ~idx;
                if (idx >= 0 && idx < cnt && mainList[idx]._Item == itm)
                {
                    var r = new InvItemStorageCount();
                    var item = mainList[idx];
                    StreamingManager.Copy(item, r);
                    r._SerieBatch = rec._Number;
                    r._Qty = rec._Qty - rec._QtySold;
                    r._QtyReserved = rec._QtyMarked;
                    r._Warehouse = rec._Warehouse;
                    r._Location = rec._Location;
                    r._QtyOrdered = 0;
                    r._Quantity = null;
                    extraItems.Add(r);
                }
            }
            extraItems.AddRange(mainList);
            extraItems.Sort(sort);
            dgInvStockStatus.ItemsSource = extraItems;
            SerieBatch.Visible = true;
            batchLoaded = true;
            BusyIndicator.IsBusy = false;
        }

        public override Task InitQuery() { return BindGrid(null); }

        public async Task BindGrid(IEnumerable<PropValuePair> propValuePair)
        {
            await dgInvStockStatus.Filter(propValuePair);
            var mainList = (List<InvItemStorageCount>)dgInvStockStatus.ItemsSource;
            var search = new InvItemStorageCount();
            var sort = new InvItemStorageLocalSort();
            var cnt = mainList.Count;

            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem));

            if (propValuePair == null && !propValuePair.Any())
            {
                var extraItems = new List<InvItemStorageCount>();
                foreach (var item in (Uniconta.DataModel.InvItem[])this.items.GetNotNullArray)
                {
                    var isService = (item._ItemType == (byte)ItemType.Service);
                    var itm = item._Item;
                    search._Item = itm;
                    var idx = mainList.BinarySearch(search, sort);
                    if (idx < 0)
                        idx = ~idx;
                    if (idx >= 0 && idx < cnt && mainList[idx]._Item == itm)
                    {
                        if (isService)
                            mainList.RemoveAt(idx);
                        continue;
                    }
                    if (!isService)
                    {
                        var r = new InvItemStorageCount();
                        r.SetMaster(item);
                        extraItems.Add(r);
                    }
                }
                if (extraItems.Count > 0)
                {
                    extraItems.AddRange(mainList);
                    extraItems.Sort(sort);
                    dgInvStockStatus.ItemsSource = extraItems;
                }
            }
            SerieBatch.Visible = false;
            batchLoaded = false;
        }

        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (warehouse == null)
                return;

            var selectedItem = dgInvStockStatus.SelectedItem as InvItemStorageCount;
            if (selectedItem?._Warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var comp = api.CompanyEntity;
            if (!comp.Location || !comp.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!comp.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }

        void PostInvJournal()
        {
            dgInvStockStatus.SelectedItem = null;
            CwInvJournal journals = new CwInvJournal(api, true);
            journals.Closed += delegate
            {
                if (journals.DialogResult == true)
                {
                    PostJournal(journals.InvJournal, CwInvJournal.Date);
                }
            };
            journals.Show();
        }

        async void PostJournal(Uniconta.DataModel.InvJournal invJournal, DateTime date)
        {
            if (invJournal == null) return;
            var mainList = (IEnumerable<InvItemStorageCount>)dgInvStockStatus.GetVisibleRows();
            var invJournalLineList = new List<InvJournalLine>();
            foreach (var item in mainList)
            {
                var dif = item.Difference;
                if (dif != 0d)
                {
                    var journalLine = new InvJournalLine();
                    journalLine._Date = date;
                    journalLine._MovementType = Uniconta.DataModel.InvMovementType.Counting;
                    journalLine._Item = item._Item;
                    journalLine._Variant1 = item._Variant1;
                    journalLine._Variant2 = item._Variant2;
                    journalLine._Variant3 = item._Variant3;
                    journalLine._Variant4 = item._Variant4;
                    journalLine._Variant5 = item._Variant5;
                    journalLine._Warehouse = item._Warehouse;
                    journalLine._Location = item._Location;
                    journalLine._SerieBatch = item._SerieBatch;
                    journalLine._CostPrice = item.itemRec._CostPrice;
                    journalLine._Qty = dif;
                    journalLine.SetMaster(invJournal);
                    journalLine._Dim1 = invJournal._Dim1;
                    journalLine._Dim2 = invJournal._Dim2;
                    journalLine._Dim3 = invJournal._Dim3;
                    journalLine._Dim4 = invJournal._Dim4;
                    journalLine._Dim5 = invJournal._Dim5;
                    invJournalLineList.Add(journalLine);
                }
            }

            if (invJournalLineList.Count() == 0)
                return;

            api.AllowBackgroundCrud = true;
            var result = await api.Insert(invJournalLineList);
            UtilDisplay.ShowErrorCode(result);
        }

        private void DockCtrl_ClosingCancelled(object sender)
        {
            PostInvJournal();
            dockCtrl.ClosingCancelled -= DockCtrl_ClosingCancelled;
        }

        public override bool IsDataChaged
        {
            get
            {
                return CheckDifference();
            }
        }

        bool CheckDifference()
        {
            var list = dgInvStockStatus.GetVisibleRows() as IEnumerable<InvItemStorageCount>;
            if (list != null && list.Select(x => x.Difference).Sum() > 0)
            {
                ShowCustomSaveCloseMessage = true;
                dockCtrl.ClosingCancelled += DockCtrl_ClosingCancelled;
                return true;
            }
            return false;
        }
    }
}
