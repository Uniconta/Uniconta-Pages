using DevExpress.Xpf.Core.Native;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Grid;
using NPOI.POIFS.Properties;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.API.GeneralLedger;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.Client.Pages;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLTransClientLocal : GLTransClientTotal, INotifyPropertyChanged
    {
        internal bool _IsMatched;
        public bool IsMatched { get { return _IsMatched; } set { _IsMatched = value; NotifyPropertyChanged("IsMatched"); NotifyPropertyChanged("StateLocal"); NotifyPropertyChanged("AllowEditing"); } }

        public long _SettlementAmount, _Temp;

        public byte StateLocal { get { return _SettlementAmount == 0 ? (byte)1 : (_SettlementAmount == -_AmountCent ? (byte)0 : (byte)2); } }

        [Display(Name = "Settlement", ResourceType = typeof(GLDailyJournalText))]
        public double SettlementAmount { get { return _SettlementAmount / 100d; } }

        public event PropertyChangedEventHandler PropertyChanged;
        internal void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        internal bool _Settle;

        public bool Settle
        {
            get { return _Settle; }
            set
            {
                _Settle = value; NotifyPropertyChanged("Settle");
            }
        }
    }
    public class TransUpperGrid : CorasauDataGridClient
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }
        public override Type TableType { get { return typeof(GLTransClientLocal); } }
    }
    public class TransSettleGrid : CorasauDataGridClient
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            tableView.RowStyle = System.Windows.Application.Current.Resources["MatchingRowStyle"] as System.Windows.Style;
        }
        public override Type TableType { get { return typeof(GLTransClientLocal); } }
    }
    public partial class GLTransSettlePage : GridBasePage
    {
        internal List<UnicontaBaseEntity> masterlist;
        DateTime filterDate;
        protected override Filter[] DefaultFilters()
        {
            if (masterlist == null || masterlist.First() is GLAccount)
            {
                Filter dateFilter = new Filter() { name = "Date" };
                if (filterDate != DateTime.MinValue)
                {
                    dateFilter.value = String.Format("{0:d}..", filterDate);
                    return new Filter[] { dateFilter };
                }
            }
            return base.DefaultFilters();
        }
        PostingAPI postingApiInv;
        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties dateSort = new SortingProperties("Date");
            var syncMaster = dgAccountsTransGrid.masterRecord;
            dateSort.Ascending = (syncMaster is GLDailyJournalPosted || syncMaster is GLDailyJournalLine);
            return new SortingProperties[] { dateSort, new SortingProperties("Voucher"), new SortingProperties("VoucherLine") };
        }
           
        public GLTransSettlePage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            var mlist = new List<UnicontaBaseEntity>() { syncEntity.Row };
            InitializePage(mlist);
        }

        Dictionary<GLTransSettlement, GLTransSettlement[]> set, setReverse;
        SettleCompare cmp;
        SettleCompareReverse cmpReverse;
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgAccountsTransGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgAccountsTransGrid.masterRecord as GLAccount;
            if (syncMaster == null)
                return;
            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("SettleTran"), ": ", syncMaster._Name);
            SetHeader(header);
        }
        private void InitializePage(List<UnicontaBaseEntity> masters = null)
        {
            InitializeComponent();
            this.DataContext = this;
            var Comp = this.api.CompanyEntity;
            filterDate = BasePage.GetFilterDate(Comp, masters != null && masters.Count > 0);
            localMenu.dataGrid = dgAccountsTransGrid;
            SetRibbonControl(localMenu, dgAccountsTransGrid);
            masterlist = masters;
            gridControl.masterRecords = masters;
            dgAccountsTransGrid.api = dgAccountsTransGridSettle.api = api;
            if (!Comp.HasDecimals)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = false;
            dgAccountsTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgAccountsTransGrid.ShowTotalSummary();
            postingApiInv = new PostingAPI(api);
            dgAccountsTransGrid.ItemsSourceChanged += DgAccountsTransGrid_ItemsSourceChanged;
            dgAccountsTransGrid.SelectedItemChanged += DgAccountsTransGrid_SelectedItemChanged;
            cmp = new SettleCompare();
            set = new Dictionary<GLTransSettlement, GLTransSettlement[]>(cmp);
            cmpReverse = new SettleCompareReverse();
            setReverse = new Dictionary<GLTransSettlement, GLTransSettlement[]>(cmpReverse);
            StateLocal.Header = Uniconta.ClientTools.Localization.lookup("Status");
        }

       
        #region Compare functions

        static bool cmptrans(GLTransSettlement x, GLTrans y)
        {
            return x._JournalPostedIdTo == y._JournalPostedId && x._VoucherTo == y._Voucher && x._VoucherLineTo == y._VoucherLine && x._DateTo == y._Date;
        }
        static bool cmptransReverse(GLTransSettlement x, GLTrans y)
        {
            return x._JournalPostedId == y._JournalPostedId && x._Voucher == y._Voucher && x._VoucherLine == y._VoucherLine && x._Date == y._Date;
        }
        class SettleCompare : IEqualityComparer<GLTransSettlement>
        {
            public bool Equals(GLTransSettlement x, GLTransSettlement y) { return eq(x, y); }
            static internal bool eq(GLTransSettlement x, GLTransSettlement y)
            {
                return x._JournalPostedId == y._JournalPostedId && x._Voucher == y._Voucher && x._VoucherLine == y._VoucherLine && x._Date == y._Date;
            }
            public int GetHashCode(GLTransSettlement rec)
            {
                return (rec._Voucher + 1) * (rec._JournalPostedId + 1) * (rec._VoucherLine + 1) * rec._Date.GetHashCode();
            }
        }
        class SettleCompareReverse : IEqualityComparer<GLTransSettlement>
        {
            public bool Equals(GLTransSettlement x, GLTransSettlement y) { return eq(x, y); }
            static bool eq(GLTransSettlement x, GLTransSettlement y)
            {
                return x._JournalPostedIdTo == y._JournalPostedIdTo && x._VoucherTo == y._VoucherTo && x._VoucherLineTo == y._VoucherLineTo && x._DateTo == y._DateTo;
            }
            public int GetHashCode(GLTransSettlement rec)
            {
                return (rec._VoucherTo + 1) * (rec._JournalPostedIdTo + 1) * (rec._VoucherLineTo + 1) * rec._DateTo.GetHashCode();
            }
        }
        class SettleCompareReverse2 : IComparer<GLTransSettlement>
        {
            public int Compare(GLTransSettlement x, GLTransSettlement y)
            {
                int c = x._JournalPostedIdTo - y._JournalPostedIdTo;
                if (c != 0) return c;
                c = x._VoucherTo - y._VoucherTo;
                if (c != 0) return c;
                c = x._VoucherLineTo - y._VoucherLineTo;
                if (c != 0) return c;
                return DateTime.Compare(x._DateTo, y._DateTo);
            }
        }

        #endregion

        private void DgAccountsTransGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var selectedItem = dgAccountsTransGrid.SelectedItem as GLTransClientLocal;
            if (selectedItem == null)
                return;
            var lst = dgAccountsTransGrid.ItemsSource as IEnumerable<GLTransClientLocal>;
            if (lst == null)
                return;

            var match = new List<GLTransClientLocal>();
            var searchSettle = new GLTransSettlement();
            searchSettle.SetMaster(selectedItem);
            var found = set.TryGetValue(searchSettle, out var settlements);
            var searchSettle2 = new GLTransSettlement()
            {
                _JournalPostedIdTo = selectedItem._JournalPostedId,
                _VoucherTo = selectedItem._Voucher,
                _VoucherLineTo = selectedItem._VoucherLine,
                _DateTo = selectedItem._Date,
                _Account = selectedItem._Account,
            };
            searchSettle2.SetMaster(selectedItem);
            var found2 = setReverse.TryGetValue(searchSettle2, out var settlements2);
            if (found || found2)
            {
                foreach (var rec in lst)
                {
                    //rec.IsMatched = false;
                    double settledAmount = 0;
                    if (found)
                    {
                        for (int i = 0; i < settlements.Length; i++)
                        {
                            var settle = settlements[i];
                            if (cmptrans(settle, rec))
                            {
                                match.Add(rec);
                                //rec.IsMatched = true;
                                break;
                            }
                        }
                    }
                    if (found2)
                    {
                        for (int i = 0; i < settlements2.Length; i++)
                        {
                            var settle = settlements2[i];
                            if (cmptransReverse(settle, rec))
                            {
                                match.Add(rec);
                                //rec.IsMatched = true;
                                break;
                            }
                        }
                    }
                }
            }
            /*
            else
            {
                foreach (var rec in lst)
                    rec.IsMatched = false;
            }
            */
            dgAccountsTransGridSettle.ItemsSource = null;
            dgAccountsTransGridSettle.ItemsSource = match;
            dgAccountsTransGridSettle.Visibility = Visibility.Visible;
        }

        static long GetSettlementAmount(long amount1, long amount2)
        {
            long l;
            if (amount1 * amount2 <= 0) // different sign
            {
                if (amount1 > 0)
                {
                    if (amount1 > -amount2)
                        l = -amount2;
                    else
                        l = amount1;
                }
                else if (amount1 < -amount2)
                    l = -amount2;
                else
                    l = amount1;
            }
            else
            {
                if (amount1 > 0)
                {
                    if (amount1 > amount2)
                        l = amount2;
                    else
                        l = amount1;
                }
                else if (amount1 < amount2)
                    l = amount2;
                else
                    l = amount1;
            }
            return l;
        }

        void CalculateSettledAmounts()
        {
            var lst = dgAccountsTransGrid.ItemsSource as IList<GLTransClientLocal>;
            if (lst == null)
                return;
            var arr = new GLTransClientLocal[lst.Count];
            lst.CopyTo(arr, 0);
            var cmp = new GLTransPostedSort();
            Array.Sort(arr, cmp);

            var searchSettle = new GLTransSettlement();
            var searchTrans = new GLTransClientLocal();
            foreach (var rec in lst)
            {
                rec._SettlementAmount = 0;
                searchSettle.SetMaster(rec);
                if (set.TryGetValue(searchSettle, out var settlements))
                {
                    var amount1 = rec._AmountCent;
                    foreach (var settle in settlements)
                    {
                        searchTrans._JournalPostedId = settle._JournalPostedIdTo;
                        searchTrans._Voucher = settle._VoucherTo;
                        searchTrans._VoucherLine = settle._VoucherLineTo;
                        searchTrans._Date = settle._DateTo;
                        searchTrans._Account = settle._Account;
                        var c = Array.BinarySearch(arr, searchTrans, cmp);
                        if (c >= 0 && c < arr.Length)
                        {
                            var foundTrans = arr[c];
                            var l = GetSettlementAmount(amount1, foundTrans._AmountCent);
                            rec._SettlementAmount -= l;
                            amount1 -= l;
                            foundTrans._Temp -= l;
                        }
                    }
                }
            }
            for (int i = 0; i < arr.Length; i++)
            {
                var rec = arr[i];
                rec._SettlementAmount -= rec._Temp;
                rec._Temp = 0;
                if (rec._SettlementAmount != 0)
                    rec.NotifyPropertyChanged("SettlementAmount");
            }
        }

       void AddSettlement(GLTransSettlement[] lst)
        {
            var workList = new List<GLTransSettlement>();
            GLTransSettlement last = lst[0];
            for (int i = 0; i < lst.Length; i++)
            {
                var rec = lst[i];
                if (!cmp.Equals(last, rec))
                {
                    if (workList.Count > 0)
                    {
                        set.Add(last, workList.ToArray());
                        workList.Clear();
                    }
                }
                workList.Add(rec);
                last = rec;
            }
            if (workList.Count > 0)
            {
                set.Add(last, workList.ToArray());
                workList.Clear();
            }

            Array.Sort(lst, new SettleCompareReverse2());
            last = lst[0];
            for (int i = 0; i < lst.Length; i++)
            {
                var rec = lst[i];
                if (!cmpReverse.Equals(last, rec))
                {
                    if (workList.Count > 0)
                    {
                        setReverse.Add(last, workList.ToArray());
                        workList.Clear();
                    }
                }
                workList.Add(rec);
                last = rec;
            }
            if (workList.Count > 0)
            {
                setReverse.Add(last, workList.ToArray());
                workList.Clear();
            }
        }
        
        private void DgAccountsTransGrid_ItemsSourceChanged(object sender, DevExpress.Xpf.Grid.ItemsSourceChangedEventArgs e)
        {
            /*
            var settleSource = dgAccountsTransGrid.ItemsSource;
            dgAccountsTransGridSettle.ItemsSource = settleSource;
            dgAccountsTransGridSettle.Visibility = Visibility.Visible;
            */
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }
       
        IEnumerable<PropValuePair> filter;
        public override async Task InitQuery()
        {
            set.Clear();
            setReverse.Clear();
            settleMaster = null;
            settleChilds = null;
            var lst = await api.Query(new GLTransSettlement(), masterlist, null); 
            if (filter != null)
                await dgAccountsTransGrid.Filter(filter);
            else
                await Filter();
            Dispatcher.BeginInvoke(new Action(() => { dgAccountsTransGrid.SelectedItem = null; dgAccountsTransGrid.tableView.FocusedRowHandle = 0; }));
            if (lst != null && lst.Length != 0)
                AddSettlement(lst);
            CalculateSettledAmounts();
        }

        private void BindGrid()
        {
            var rb = ribbonControl;
            if (rb != null)
            {
                var pairs = rb.filterValues;
                var sort = rb.PropSort;
                if (pairs != null || sort != null)
                {
                    rb.FilterGrid?.Filter(pairs, sort);
                    return;
                }
            }
            InitQuery();
        }
      
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            AmountConverted.Visible = (GLTransClient.Rates != null);
            setDim();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAccountsTransGrid.SelectedItem as GLTransClient;
            switch (ActionType)
            {
                case "Settle":
                    Settle();
                    break;
                case "Remove":
                    RemoveSettle();
                    break;
                case "RefreshGrid":
                    BindGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void Settle()
        {
            var amount1 = settleMaster._AmountCent;
            var lst = new GLTransSettlement[settleChilds.Count];
            for (int i = 0; i < settleChilds.Count; i++)
            {
                var settleChild = settleChilds[i];
                var settleRec = new GLTransSettlement();
                settleRec.SetMaster(settleMaster);
                settleRec.CopyReleation(settleChild);
                lst[i] = settleRec;

                var l = GetSettlementAmount(amount1, settleChild._AmountCent);
                settleMaster._SettlementAmount -= l;
                amount1 -= l;
                settleChild._SettlementAmount += l;
                settleChild.NotifyPropertyChanged("SettlementAmount");
                settleChild.NotifyPropertyChanged("StateLocal");
            }
            AddSettlement(lst);
            settleMaster.NotifyPropertyChanged("SettlementAmount");
            settleMaster.NotifyPropertyChanged("StateLocal");
            dgAccountsTransGrid.SelectedItem = null;
            dgAccountsTransGrid.SelectedItem = settleMaster;
            settleMaster = null;
            settleChilds = null;
            ResetCheckMarks();

            api.Insert(lst);
        }

        void RemoveSettle()
        {
            var master = dgAccountsTransGrid.SelectedItem as GLTransClientLocal;
            var child = dgAccountsTransGridSettle.SelectedItem as GLTransClientLocal;
            if (master == null || child == null)
                return;
            var rec1 = new GLTransSettlement()
            {
                _JournalPostedIdTo = child._JournalPostedId,
                _VoucherTo = child._Voucher,
                _VoucherLineTo = child._VoucherLine,
                _DateTo = child._Date,
            };
            rec1.SetMaster(master);
            var rec2 = new GLTransSettlement()
            {
                _JournalPostedIdTo = master._JournalPostedId,
                _VoucherTo = master._Voucher,
                _VoucherLineTo = master._VoucherLine,
                _DateTo = master._Date,
            };
            rec2.SetMaster(child);
            api.Delete(new GLTransSettlement[] { rec1, rec2 });

            var l = GetSettlementAmount(master._AmountCent, child._AmountCent);
            master._SettlementAmount += l;
            child._SettlementAmount -= l;
            child.NotifyPropertyChanged("SettlementAmount");
            child.NotifyPropertyChanged("StateLocal");
            master.NotifyPropertyChanged("SettlementAmount");
            master.NotifyPropertyChanged("StateLocal");
            ResetCheckMarks();
            RemoveElements(set, rec1, child, false);
            RemoveElements(setReverse, rec1, master, true);

            DgAccountsTransGrid_SelectedItemChanged(null, null);
        }

        static void RemoveElements(Dictionary<GLTransSettlement, GLTransSettlement[]> set, GLTransSettlement rec1, GLTrans rec2, bool reverse)
        {
            GLTransSettlement[] arr;
            int i;
            if (set.TryGetValue(rec1, out arr))
            {
                for (i = 0; i < arr.Length; i++)
                    if (reverse ? cmptransReverse(arr[i], rec2) : cmptrans(arr[i], rec2))
                    {
                        if (arr.Length == 1)
                            set.Remove(rec1);
                        else
                        {
                            Array.Copy(arr, i, arr, i + 1, arr.Length - i - 1);
                            Array.Resize(ref arr, arr.Length - 1);
                            set[rec1] = arr;
                        }
                        break;
                    }
            }
        }

        void setDim()
        {
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dgAccountsTransGrid, lookup);
        }

        private void HasVoucherImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            busyIndicator.IsBusy = true;
            ViewVoucher(TabControls.VouchersPage3, dgAccountsTransGrid.syncEntity);
            busyIndicator.IsBusy = false;
        }

        private void IsSettle_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void IsSettle_Unchecked(object sender, RoutedEventArgs e)
        {

        }
        GLTransClientLocal settleMaster;
        List<GLTransClientLocal> settleChilds;
        private void Settle_Checked(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgAccountsTransGrid.SelectedItem as GLTransClientLocal;
            if (selectedItem != null)
            {
                if (settleMaster == null)
                {
                    settleMaster = selectedItem;
                    settleChilds = new List<GLTransClientLocal>();
                }
                else
                    settleChilds.Add(selectedItem);
            }
        }

        private void Settle_Unchecked(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgAccountsTransGrid.SelectedItem as GLTransClientLocal;
            if (selectedItem == null)
                return;
            if (object.ReferenceEquals(settleMaster, selectedItem))
            {
                settleMaster = null;
                ResetCheckMarks();
            }
            else
                settleChilds?.Remove(selectedItem);
        }

        void ResetCheckMarks()
        {
            var grid = dgAccountsTransGrid;
            var view = grid.View as DevExpress.Xpf.Grid.TableView;
            if (view == null)
                return;
            for (int i = 0; i < grid.VisibleRowCount; i++)
            {
                int rowHandle = grid.GetRowHandleByVisibleIndex(i);
                if (grid.IsGroupRowHandle(rowHandle))
                    continue;
                var cellElement = view.GetCellElementByRowHandleAndColumn(rowHandle, colSettle);
                if (cellElement == null)
                    continue;
                var checkEdit = FindVisualChild<System.Windows.Controls.CheckBox>(cellElement);
                if (checkEdit != null)
                    checkEdit.IsChecked = false;
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;

                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
