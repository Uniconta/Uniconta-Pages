using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using UnicontaClient.Utilities;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;
using Uniconta.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Collections;
using Uniconta.ClientTools.DataModel;
using UnicontaAPI.Project.API;
using UnicontaClient.Models;
using Uniconta.API.Project;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectTransInvoiceClient : ProjectTransClient
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItem))]
        [Display(Name = "InvoicingItemNumber", ResourceType = typeof(PrCategoryText))]
        public string InvoiceItem { get { return _Item; } set { if (_Item == value) return; _Item = value; _itemRec = null; NotifyPropertyChanged("Item"); NotifyPropertyChanged("ItemName"); NotifyPropertyChanged("Text"); } }

        public bool IsEnabled { get { return !Invoiced; } }

        public double _SalesAmountAgr, _CostAmountAgr;
    }

    public class ProjectTransClientInvoiceGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTransInvoiceClient); } }
        public override bool Readonly { get { return false; } }
        public override bool CanInsert { get { return false; } }
    }

    public partial class ProjectInvoiceBase : GridBasePage
    {
        UnicontaBaseEntity master;
        public override string NameOfControl { get { return TabControls.ProjectInvoiceBase; } }
        public ProjectInvoiceBase(UnicontaBaseEntity master)
           : base(master)
        {
            Init(master);
        }

        public ProjectInvoiceBase(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            Init(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectTransClientInvoiceGrid.UpdateMaster(args);
            SetHeader();
            BindGrid();
            dgProjectTransClientInvoiceGrid.Readonly = false;
        }

        private void SetHeader()
        {
            var syncMaster = dgProjectTransClientInvoiceGrid.masterRecord as Uniconta.DataModel.Project;
            if (syncMaster == null) return;
            string header = null;
            header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("GenerateInvoice"), syncMaster._Name);
            SetHeader(header);
        }

        private void Init(UnicontaBaseEntity master)
        {
            if (!(master is Uniconta.DataModel.Project))
            {
                throw new Exception("This page support only 'Project' as master");
            }
            this.master = master;
            InitializeComponent();
            localMenu.dataGrid = dgProjectTransClientInvoiceGrid;
            SetRibbonControl(localMenu, dgProjectTransClientInvoiceGrid);
            dgProjectTransClientInvoiceGrid.api = api;
            dgProjectTransClientInvoiceGrid.UpdateMaster(master);
            dgProjectTransClientInvoiceGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectTransClientInvoiceGrid.ShowTotalSummary();
            localMenu.DisableButtons(new string[] { "Aggregate", "GenerateInvoice", "MarkAsInvoice" });
            ((DevExpress.Xpf.Grid.TableView)dgProjectTransClientInvoiceGrid.View).RowStyle = Application.Current.Resources["DisableStyleRow"] as Style;
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override Task InitQuery()
        {
            return null;
        }

        SQLCache ItemCache, CategoryCache;
        protected override async void LoadCacheInBackGround()
        {
            var Comp = api.CompanyEntity;
            ItemCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ??
                        await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);
            CategoryCache = Comp.GetCache(typeof(Uniconta.DataModel.PrCategory)) ??
                        await Comp.LoadCache(typeof(Uniconta.DataModel.PrCategory), api).ConfigureAwait(false);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "SaveGrid":
                    dgProjectTransClientInvoiceGrid.SaveData();
                    break;
                case "Search":
                    BindGrid();
                    dgProjectTransClientInvoiceGrid.Readonly = false;
                    break;
                case "Aggregate":
                    aggregate(cbxItem.IsChecked.Value, cbxEmployee.IsChecked.Value, cbxCategory.IsChecked.Value);
                    dgProjectTransClientInvoiceGrid.Readonly = true;
                    break;
                case "GenerateInvoice":
                    GenerateOrderLines();
                    break;
                case "MarkAsInvoice":
                    MarkLineAsInvoiced();
                    break;
                default:
                     gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
  
        async void MarkLineAsInvoiced()
        {
            var lst = mainList ?? dgProjectTransClientInvoiceGrid.ItemsSource as IEnumerable<ProjectTransClient>;
            if (lst != null && lst.Count() > 0)
            {
                InvoiceAPI Invapi = new InvoiceAPI(api);
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("BusyMessage");
                busyIndicator.IsBusy = true;
                var result = await Invapi.MarkAsInvoiced(this.master as Uniconta.DataModel.Project, lst);
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(result);
            }
        }

        void GenerateOrderLines()
        {
            var orderLines = new List<DebtorOrderLineClient>();
            var source = (IEnumerable<ProjectTransInvoiceClient>)dgProjectTransClientInvoiceGrid.GetVisibleRows();
            if (source != null)
            {
                int i = 0;
                foreach (var line in source)
                {
                    if (! line._canInvoice)
                        continue;

                    var ol = new DebtorOrderLineClient();
                    ol._LineNumber = ++i;
                    ol._Unit = (ItemUnit)line._Unit;
                    ol._Date = line._Date;
                    ol._CostPrice = line._CostPrice;
                    ol._Text = line._Text;
                    ol._Item = line._Item;
                    ol._Variant1 = line._Variant1;
                    ol._Variant2 = line._Variant2;
                    ol._Variant3 = line._Variant3;
                    ol._Variant4 = line._Variant4;
                    ol._Variant5 = line._Variant5;
                    var itm = (InvItem)ItemCache.Get(line._Item);
                    if (itm != null)
                    {
                        if (ol._Text == null)
                            ol._Text = itm._Name;
                        ol._Dim1 = itm._Dim1;
                        ol._Dim2 = itm._Dim2;
                        ol._Dim3 = itm._Dim3;
                        ol._Dim4 = itm._Dim4;
                        ol._Dim5 = itm._Dim5;
                        if (ol._Unit == 0)
                            ol._Unit = itm._Unit;
                    }
                    if (line._Dim1 != null)
                        ol._Dim1 = line._Dim1;
                    if (line._Dim2 != null)
                        ol._Dim2 = line._Dim2;
                    if (line._Dim3 != null)
                        ol._Dim3 = line._Dim3;
                    if (line._Dim4 != null)
                        ol._Dim4 = line._Dim4;
                    if (line._Dim5 != null)
                        ol._Dim5 = line._Dim5;

                    ol._Employee = line._Employee;
                    ol._Qty = line._Qty != 0d ? line._Qty : 1d;
                    if (line._SalesPrice != 0)
                    {
                        ol._Price = line._SalesPrice;
                        ol._DiscountPct = line._DiscountPct;
                    }
                    else
                        ol._AmountEntered = line._SalesAmountAgr;
                    if (ol._Unit == 0 && line._PrCategory != null)
                    {
                        var cat = (PrCategory)CategoryCache.Get(line._PrCategory);
                        if (cat != null)
                            ol._Unit = cat._Unit;
                    }
                    orderLines.Add(ol);
                }
            }
            var order = new DebtorOrder();
            order.SetMaster(master);
            order._NoItemUpdate = true;

            string ValueFound = null;
            foreach (var cat in (Uniconta.DataModel.PrCategory[])CategoryCache.GetNotNullArray)
                if (cat._CatType == CategoryType.Revenue)
                {
                    ValueFound = cat._Number;
                    if (cat._Default)
                        break;
                }

            order._PrCategory = ValueFound;

            var paramArr = new object[] { order, orderLines.ToArray() };
            AddDockItem(TabControls.CreateInvoicePage, paramArr);
        }

        private async void BindGrid()
        {
            localMenu.EnableButtons(new string[] { "Aggregate", "GenerateInvoice", "MarkAsInvoice" });

            mainList = null;
            cbxItem.IsEnabled = true;
            cbxCategory.IsEnabled = true;
            cbxEmployee.IsEnabled = true;
            await Filter();
            var source = dgProjectTransClientInvoiceGrid.ItemsSource as IList<ProjectTransInvoiceClient>;
            var removeList = new List<ProjectTransInvoiceClient>();
            if (source != null)
            {
                SQLCache categoryCache = this.CategoryCache;
                foreach (var pti in source)
                {
                    pti._SalesAmountAgr = pti._SalesAmount;
                    pti._CostAmountAgr = pti._CostAmount;
                    var category = categoryCache.Get(pti.PrCategory) as PrCategory;
                    if (category == null)
                        continue;
                    if (category._CatType == CategoryType.Revenue || category._CatType == CategoryType.Sum || category._CatType == CategoryType.OnAccountInvoicing || category._CatType == CategoryType.Header)
                        removeList.Add(pti);
                    else if (string.IsNullOrEmpty(pti.Item))
                    {
                        pti.Item = category._Item;
                    }
                }
            }
            if (removeList.Count > 0)
            {
                foreach(var item in removeList)
                    source.Remove(item);
            }

            if (source != null && source.Count > 0)
            {
                var arr = source.ToArray();
                mainList = arr;
                dgProjectTransClientInvoiceGrid.SetSource(arr);
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"));

        }
        protected override Filter[] DefaultFilters()
        {
            var Filter = new List<Filter>();
            if (txtDateFrm.DateTime != DateTime.MinValue)
            {
                Filter dateFilterFrom = new Filter();
                dateFilterFrom.name = "Date";
                dateFilterFrom.value = String.Format("{0:d}..", txtDateFrm.DateTime);
                Filter.Add(dateFilterFrom);
            }
            if (txtDateTo.DateTime != DateTime.MinValue)
            {
                Filter dateFilterTo = new Filter();
                dateFilterTo.name = "Date";
                dateFilterTo.value = String.Format("..{0:d}", txtDateTo.DateTime);
                Filter.Add(dateFilterTo);
            }
            var invoiced = new Filter() { name = "Invoiced", value = "0" };
            Filter.Add(invoiced);
            return Filter.ToArray();
        }

        IEnumerable<ProjectTransInvoiceClient> mainList;
        async void aggregate(bool aggregateItem, bool aggregateEmployee, bool aggregateCategory)
        {
            var result = await dgProjectTransClientInvoiceGrid.SaveData();
            if (result != ErrorCodes.Succes) return;

            if (mainList == null)
                mainList = (IEnumerable<ProjectTransInvoiceClient>)dgProjectTransClientInvoiceGrid.ItemsSource;
            if (mainList == null || !mainList.Any())
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoDataCollected"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }

            IEnumerable<ProjectTransInvoiceClient> lst;

            if (!aggregateCategory && !aggregateEmployee && !aggregateItem)
            {
                lst = mainList;
            }
            else
            {
                var categoryCache = this.CategoryCache;

                var dict = new Dictionary<string, ProjectTransInvoiceClient>();
                List<ProjectTransInvoiceClient> NoAggregate = null;

                StringBuilder sb = new StringBuilder(300);
                foreach (var rec in mainList)
                {
                    if (!rec._canInvoice)
                        continue;

                    var cat = (PrCategory)categoryCache.Get(rec._PrCategory);
                    if ((cat != null && !cat._CanAggregate) ||
                       ((aggregateItem || aggregateEmployee) && rec._Employee == null && rec._Item == null))
                    {
                        if (NoAggregate == null)
                            NoAggregate = new List<ProjectTransInvoiceClient>();
                        NoAggregate.Add(rec);
                        continue;
                    }

                    sb.Clear();
                    if (aggregateCategory)
                        sb.Append(rec._PrCategory);
                    if (aggregateEmployee)
                        sb.Append('\n').Append(rec._Employee ?? "?");
                    if (aggregateItem)
                        sb.Append('\n').Append(rec._Item ?? "?");

                    var key = sb.ToString();
                    ProjectTransInvoiceClient val;
                    if (dict.TryGetValue(key, out val))
                    {
                        val._Qty += rec._Qty;
                        val._SalesAmountAgr += rec._SalesAmountAgr;
                        if (NumberConvert.ToLong(val._SalesAmountAgr * 100d) == 0)
                        {
                            val._SalesPrice = 0;
                            val._Qty = 0;
                        }
                        val._CostAmountAgr += rec._CostAmountAgr;
                        if (NumberConvert.ToLong(val._CostAmountAgr * 100d) == 0)
                            val._CostPrice = 0;

                        if (!aggregateCategory && val._PrCategory != rec._PrCategory)
                        {
                            val._PrCategory = null;
                            if (val._Unit == 0)
                                val._Unit = (byte)cat._Unit;
                        }
                        if (!aggregateEmployee && val._Employee != rec._Employee)
                            val._Employee = null;
                        if (!aggregateItem && val._Item != rec._Item)
                        {
                            if (val._Unit == 0)
                            {
                                var itm = (InvItem)ItemCache.Get(val._Item ?? rec._Item);
                                if (itm != null)
                                    val._Unit = (byte)itm._Unit;
                            }
                            val._Item = null;
                        }
                        if (val._Dim1 != rec._Dim1)
                            val._Dim1 = null;
                        if (val._Dim2 != rec._Dim2)
                            val._Dim2 = null;
                        if (val._Dim3 != rec._Dim3)
                            val._Dim3 = null;
                        if (val._Dim4 != rec._Dim4)
                            val._Dim4 = null;
                        if (val._Dim5 != rec._Dim5)
                            val._Dim5 = null;
                    }
                    else
                    {
                        val = new ProjectTransInvoiceClient();
                        StreamingManager.Copy(rec, val);
                        val._SalesAmountAgr = rec._SalesAmountAgr;
                        val._CostAmountAgr = rec._CostAmountAgr;

                        dict.Add(key, val);
                    }
                }

                lst = dict.Values;
                foreach(var rec in lst)
                {
                    if (rec._Qty != 0)
                    {
                        rec._SalesPrice = Math.Round(Math.Abs(rec._SalesAmountAgr) / rec._Qty, 2);
                        rec._CostPrice = Math.Round(Math.Abs(rec._CostAmountAgr) / rec._Qty, 2);
                        rec._DiscountPct = 0;
                    }
                }
                if (NoAggregate != null)
                {
                    NoAggregate.AddRange(lst);
                    lst = NoAggregate;
                }

                var sortedLst = lst.ToList();
                sortedLst.Sort(new sortTrans(categoryCache));
                lst = sortedLst;
            }

            dgProjectTransClientInvoiceGrid.ItemsSource = lst;
        }

        class sortTrans : IComparer<ProjectTransInvoiceClient>
        {
            public sortTrans(SQLCache CategoryCache) { this.CategoryCache = CategoryCache; }
            SQLCache CategoryCache;
            public int Compare(ProjectTransInvoiceClient x, ProjectTransInvoiceClient y)
            {
                var prx = (PrCategory)CategoryCache.Get(x._PrCategory);
                var pry = (PrCategory)CategoryCache.Get(y._PrCategory);

                int c;
                if (prx != null && pry != null)
                {
                    c = prx._Sorting - pry._Sorting;
                    if (c != 0)
                        return c;
                }
                c = DateTime.Compare( x._Date, y._Date);
                if (c != 0)
                    return c;
                c = string.Compare(prx?.KeyStr, pry?.KeyStr);
                if (c != 0)
                    return c;
                return x._LineNumber - y._LineNumber;
            }
        }
    }
}
