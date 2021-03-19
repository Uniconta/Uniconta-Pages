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
using Uniconta.API.System;
using System.IO;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvStockAccountingReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvItemStorageCount); } }
        public override IComparer GridSorting { get { return new InvItemStorageLocalSort(); } }
        public override bool Readonly { get { return false; } }
    }

    internal class InvItemVariantSort : IComparer<InvItemStorage>
    {
        public int Compare(InvItemStorage x, InvItemStorage y)
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
            return string.Compare(x._Variant5, y._Variant5);
        }
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
        public string SerieBatch { get { return _SerieBatch; } set { _SerieBatch = value; NotifyPropertyChanged("SerieBatch"); } }

        internal string _EAN;
        [StringLength(20)]
        [Display(Name = "EANnumber", ResourceType = typeof(InventoryText))]
        public string EAN { get { return _EAN; } set { _EAN = value; NotifyPropertyChanged("EAN"); } }

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
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            dgInvStockStatus.UpdateMaster(_master);
            ((TableView)dgInvStockStatus.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            SetRibbonControl(localMenu, dgInvStockStatus);
            dgInvStockStatus.api = api;
            dgInvStockStatus.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvStockStatus.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            this.BeforeClose += InvStockCountingReport_BeforeClose;
            this.items = api.GetCache(typeof(Uniconta.DataModel.InvItem));
            this.warehouse = api.GetCache(typeof(Uniconta.DataModel.InvWarehouse));
            dgInvStockStatus.ShowTotalSummary();
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
            if (rec == null)
                return;
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
                case "EAN":
                    FindOnEAN(rec, this.items, api);
                    break;
            }
        }

        static public void FindOnEAN(InvItemStorageCount rec, SQLCache Items, QueryAPI api)
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
                FindOnEANVariant(rec, api);
        }

        static async void FindOnEANVariant(InvItemStorageCount rec, QueryAPI api)
        {
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
                case "ClearFilter":
                    BindGrid(null);
                    break;
                case "Export":
                    dockCtrl.PrintCurrentTabGrids("CSV");
                    break;
                case "Import":
                    ImportCsv();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void ImportCsv()
        {
#if !SILVERLIGHT
            var openFileDailog = UtilDisplay.LoadOpenFileDialog;
            openFileDailog.Filter = "CSV Files |*.csv";
            openFileDailog.Multiselect = false;
            bool? userClickedOK = openFileDailog.ShowDialog();
            if (userClickedOK != true) return;
            try
            {
                using (var sr = new StreamReader(File.OpenRead(openFileDailog.FileName), Encoding.Default))
                {
                    dgInvStockStatus.ItemsSource = null;
                    var delim = UtilFunctions.GetDefaultDeLimiter();
                    dgInvStockStatus.CopyFromExcel(sr, delim , true, true, false);
                }
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
                return;
            }
#endif
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
            SetBusy();
            var lst = await api.Query<InvSerieBatch>(new InvSerieBatchOpen());
            if (lst == null || lst.Length == 0)
            {
                ClearBusy();
                return;
            }

            var mainList = (List<InvItemStorageCount>)dgInvStockStatus.ItemsSource;
            var search = new InvItemStorageCount();
            var sort = new InvItemStorageLocalSort();
            var cnt = mainList.Count;
            var extraItems = new List<InvItemStorageCount>(cnt);
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
            ClearBusy();
        }

        public override Task InitQuery() { return BindGrid(null); }

        public async Task BindGrid(IEnumerable<PropValuePair> propValuePair)
        {
            await dgInvStockStatus.Filter(propValuePair);
            var mainList = (List<InvItemStorageCount>)dgInvStockStatus.ItemsSource;
            if (mainList == null)
                return;

            var search = new InvItemStorageCount();
            var sort = new InvItemStorageLocalSort();
            var cnt = mainList.Count;
            var api = this.api;

            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem));

            if (propValuePair == null || propValuePair.Count() == 0)
            {
                var lst = new List<InvItemStorageCount>(cnt);
                foreach (var item in (Uniconta.DataModel.InvItem[])this.items.GetNotNullArray)
                {
                    if (item._ItemType == (byte)ItemType.Service)
                        continue;
                    var itm = item._Item;
                    search._Item = itm;
                    var idx = mainList.BinarySearch(search, sort);
                    if (idx < 0)
                        idx = ~idx;
                    if (idx >= 0 && idx < cnt && mainList[idx]._Item == itm)
                        continue;
                    var r = new InvItemStorageCount();
                    r.SetMaster(item);
                    lst.Add(r);
                }
                if (lst.Count > 0)
                {
                    lst.AddRange(mainList);
                    lst.Sort(sort);
                    mainList = lst;
                }
            }
            if (api.CompanyEntity.ItemVariants && api.CompanyEntity._LoadVariantsInCounting)
            {
                var stds = await api.Query<InvStandardVariantCombi>();
                var newList = AddVariants(mainList, stds);
                if (newList != null)
                {
                    newList.Sort(sort);
                    mainList = newList;
                }
            }
            dgInvStockStatus.ItemsSource = mainList;

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
            else
                Location.ShowInColumnChooser = true;
            if (!comp.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
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
            var invJournalLineList = new List<InvJournalLine>(100);
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
                    var itm = item.itemRec;
                    if (itm != null)
                        journalLine._CostPrice = itm._CostPrice;
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

            if (invJournalLineList.Count == 0)
                return;

            api.AllowBackgroundCrud = true;
            var result = await api.Insert(invJournalLineList);
            UtilDisplay.ShowErrorCode(result);
            if (result == ErrorCodes.Succes)
                isTransToJrnl = true;
        }
        
        bool isTransToJrnl;

        private void InvStockCountingReport_BeforeClose()
        {
            if (dock != null)
            {
                dock.ClosingCancelled -= DockCtrl_ClosingCancelled;
                dock = null;
            }
        }

        private void DockCtrl_ClosingCancelled(object sender)
        {
            InvStockCountingReport_BeforeClose();
            PostInvJournal();
            checkDiff = null;
        }

        bool? checkDiff;
        DockControl dock;
        public override bool IsDataChaged
        {
            get
            {
                if (isTransToJrnl)
                    return false;
                else
                {
                    if (checkDiff == null)
                        checkDiff = CheckDifference();
                    return checkDiff.Value;
                }
            }
        }

        bool CheckDifference()
        {
            var list = dgInvStockStatus.GetVisibleRows() as IEnumerable<InvItemStorageCount>;
            if (list != null && list.Any(x => x != null && x.Difference != 0))
            {
                ShowCustomSaveCloseMessage = true;
                if (checkDiff == null)
                {
                    dock = dockCtrl;
                    dock.ClosingCancelled += DockCtrl_ClosingCancelled;
                }
                return true;
            }
            return false;
        }

        List<InvItemStorageCount> AddVariants(List<InvItemStorageCount> mainList, InvStandardVariantCombi[] stds)
        {
            var itemSort = new InvItemVariantSort();

            var stdSort = new InvStandardVariantCombiSort();
            Array.Sort(stds, stdSort);

            var stdSearch = new InvStandardVariantCombi() { _LineNumber1 = float.MinValue, _LineNumber2 = float.MinValue, _LineNumber3 = float.MinValue, _LineNumber4 = float.MinValue, _LineNumber5 = float.MinValue };
            var newItems = new List<InvItemStorageCount>(100);
            var itemSearch = new InvItemStorageCount();

            string lastItem = null;
            foreach (var rec in mainList)
            {
                if (rec._Item == lastItem)
                    continue;
                lastItem = rec._Item;
                var item = (InvItem)items.Get(lastItem);
                if (item?._StandardVariant == null || !item._UseVariants)
                    continue;

                itemSearch._Item = lastItem;
                var stdVariant = item._StandardVariant;
                stdSearch._StandardVariant = stdVariant;
                var pos = Array.BinarySearch(stds, stdSearch, stdSort);
                if (pos < 0)
                    pos = ~pos;
                while (pos < stds.Length)
                {
                    var std = stds[pos++];
                    if (std._StandardVariant != stdVariant)
                        break;

                    itemSearch._Variant1 = std._Variant1;
                    itemSearch._Variant2 = std._Variant2;
                    itemSearch._Variant3 = std._Variant3;
                    itemSearch._Variant4 = std._Variant4;
                    itemSearch._Variant5 = std._Variant5;
                    var itemPos = mainList.BinarySearch(itemSearch, itemSort);
                    if (itemPos < 0)
                        itemPos = ~itemPos;
                    if (itemPos >= mainList.Count)
                        itemPos = mainList.Count - 1;
                    var found = mainList[itemPos];
                    if (found._Item != itemSearch._Item) // item is not in the list, or we are past item.
                    {
                        if (itemPos == 0)
                            continue;
                        found = mainList[itemPos - 1]; // we need to test one position before, since binarysearch will be past the item with blank variants
                        if (found._Item != itemSearch._Item) // item is not in the list
                            continue;
                    }
                    if (itemSort.Compare(found, itemSearch) == 0) // we have the variant combination
                        continue;

                    var newrec = new InvItemStorageCount();
                    newrec.SetMaster(item);
                    newrec._Variant1 = std._Variant1;
                    newrec._Variant2 = std._Variant2;
                    newrec._Variant3 = std._Variant3;
                    newrec._Variant4 = std._Variant4;
                    newrec._Variant5 = std._Variant5;
                    newrec._Warehouse = item._Warehouse;
                    newrec._Location = item._Location;
                    newItems.Add(newrec);
                }
            }
            if (newItems.Count > 0)
            {
                newItems.AddRange(mainList);
                return newItems;
            }
            return null;
        }
    }
}
