using UnicontaClient.Pages;
using System;
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
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common.Utility;
using System.Collections;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Charts;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvStorageProfileLocalClient : InvStorageProfileClient
    {
        public double _Total;
        [Display(Name = "Total", ResourceType = typeof(GLTableText))]
        public double Total { get { return _Total; } set { _Total = value; } }
    }

    public class InvStorageProfileGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvStorageProfileLocalClient); } }
        public override IComparer GridSorting { get { return new InvStorageProfileCompare(); } }

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            base.PrintGrid(PrintReportName, printparam, format, page);
        }

        public string PrintReportName { get; set; }
    }

    public partial class InvStorageProfileReport : GridBasePage
    {
        static DateTime balDate = StartOfWeek(BasePage.GetSystemDefaultDate(), DayOfWeek.Monday);
        static int interval = 7;
        static int count = 8;
        static bool isMovement = true;
        CWServerFilter itemFilterDialog;
        bool itemFilterCleared;
        public TableField[] InventoryUserFields { get; set; }

        public InvStorageProfileReport(SynchronizeEntity syncEntity)
          : base(syncEntity, false)
        {
            InitPage(syncEntity.Row);
        }

        public InvStorageProfileReport(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgInvStorageProfileGrid);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            BalanceDate.DateTime = balDate;
            if (interval > 0)
                intervalEdit.EditValue = interval;
            if (count > 0)
                countEdit.EditValue = count;
            dgInvStorageProfileGrid.ShowTotalSummary();
            if (api.GetCache(typeof(Uniconta.DataModel.InvItem)) == null)
                api.LoadCache(typeof(Uniconta.DataModel.InvItem));

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            rdbMovement.IsChecked = isMovement;
            rdbInhand.IsChecked = !isMovement;
            if (master != null)
            {
                dgInvStorageProfileGrid.UpdateMaster(master);
                LoadData();
            }
        }

        public static DateTime StartOfWeek(DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvStorageProfileGrid.UpdateMaster(args);
            SetPageHeader();
            LoadData();
        }

        void SetPageHeader()
        {
            string header;
            var syncMaster = dgInvStorageProfileGrid.masterRecord as InvItem;
            if (syncMaster != null)
                header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), syncMaster._Item);
            else
            {
                var syncMaster2 = dgInvStorageProfileGrid.masterRecord as InvItemStorage;
                if (syncMaster2 != null)
                    header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), syncMaster2._Item);

                else
                {
                    var syncMaster3 = dgInvStorageProfileGrid.masterRecord as DCOrderLine;
                    if (syncMaster3 != null)
                        header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), syncMaster3._Item);
                    else return;

                }
            }
            SetHeader(header);
        }

        public override bool IsDataChaged { get { return false; } }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }
       
        public override Task InitQuery()
        {
            return null;
        }
        
        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvStorageProfileGrid.SelectedItem as InvStorageProfileLocalClient;
            switch (ActionType)
            {
                case "Search":
                    LoadData();
                    break;
                case "ItemFilter":
                    if (itemFilterDialog == null)
                    {
                        if (itemFilterCleared)
                            itemFilterDialog = new CWServerFilter(api, typeof(InvItemClient), null, null, InventoryUserFields);
                        else
                            itemFilterDialog = new CWServerFilter(api, typeof(InvItemClient), null, null, InventoryUserFields);
                        itemFilterDialog.Closing += itemFilterDialog_Closing;
#if !SILVERLIGHT
                        itemFilterDialog.Show();
                    }
                    else
                        itemFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    itemFilterDialog.Show();
#endif
                    break;

                case "ClearItemFilter":
                    itemFilterDialog = null;
                    itemFilterValues = null;
                    itemFilterCleared = true;
                    break;
                case "InvReservation":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryReservationReport, selectedItem.InvItem, Uniconta.ClientTools.Localization.lookup("Reservations"));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        List<PropValuePair> inputs;
        private async void LoadData()
        {
            balDate = BalanceDate.DateTime;
            if (balDate == DateTime.MinValue)
                balDate = DateTime.Today;
            string reportTypeContent = String.Format("{0:d}", balDate);
            dgInvStorageProfileGrid.PrintReportName = string.Format("{0} {1} ({2})", Uniconta.ClientTools.Localization.lookup("Item"), Uniconta.ClientTools.Localization.lookup("StockProfile"), reportTypeContent);
            interval = (int)NumberConvert.ToInt(intervalEdit.Text);
            count = (int)NumberConvert.ToInt(countEdit.Text);
            for (int i = 0; i < 10; i++)
            {
                var index = NumberConvert.ToString(i);
                GridColumn column = dgInvStorageProfileGrid.Columns.Where(x => x.Name.Contains("Amount" + index)).FirstOrDefault();
                if (column != null)
                {
                    ColumnBase cb = (ColumnBase)column;
                    if (i < count)
                        cb.Visible = cb.ShowInColumnChooser = true;
                    else
                        cb.Visible = cb.ShowInColumnChooser = false;
                }
            }

            inputs = new List<PropValuePair>();
            inputs.Add(PropValuePair.GenereteParameter("PrDate", typeof(DateTime), String.Format("{0:d}", BalanceDate.DateTime)));
            inputs.Add(PropValuePair.GenereteParameter("Interval", typeof(Int32), intervalEdit.Text));
            inputs.Add(PropValuePair.GenereteParameter("Count", typeof(Int32), countEdit.Text));
            if (itemFilterValues != null)
                inputs.AddRange(itemFilterValues);

            var t = dgInvStorageProfileGrid.Filter(inputs);
            SetHeader();
            await t;

            var InvItems = api.GetCache(typeof(Uniconta.DataModel.InvItem));
            var lst = (IEnumerable<InvStorageProfileLocalClient>)dgInvStorageProfileGrid.ItemsSource;
            if (lst == null)
                return;

            foreach (var rec in lst)
            {
                rec.item = (Uniconta.DataModel.InvItem)InvItems.Get(rec._Item);
                var a = rec._Amount;
                long tot = rec._Qty;
                for (int i = a.Length; (--i >= 0);)
                    tot += a[i];
                rec._Total = tot / 10000d;
            }

            if (rdbInhand.IsChecked == true)
                UpdateData(true);

            dgInvStorageProfileGrid.UpdateTotalSummary();
        }

        public IEnumerable<PropValuePair> itemFilterValues;
        public FilterSorter itemPropSort;

        void itemFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (itemFilterDialog.DialogResult == true)
            {
                itemFilterValues = itemFilterDialog.PropValuePair;
                itemPropSort = itemFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            itemFilterDialog.Hide();
#endif
        }

        private void SetHeader()
        {
            int count = (int)NumberConvert.ToInt(countEdit.Text);
            int interval = (int)NumberConvert.ToInt(intervalEdit.Text);
            var date = BalanceDate.DateTime;
            if (date == DateTime.MinValue)
                date = DateTime.Today;
            bool MonthStep = (interval == 31);
            if (MonthStep)
                interval = DateTime.DaysInMonth(date.Year, date.Month);
            bool EndMonth = (MonthStep && date.Day == interval);
            for (int i = 0; i < count; i++)
            {
                DateTime NextDate;

                string hdr;
                if (i == 0)
                {
                    hdr = Uniconta.ClientTools.Localization.lookup("Older");
                    NextDate = date;
                }
                else
                {
                    if (MonthStep)
                        NextDate = date.AddMonths(1);
                    else
                        NextDate = date.AddDays(interval);

                    if (i + 1 < count)
                        hdr = String.Format("{0:d} - {1:d}", date, NextDate.AddDays(-1));
                    else
                        hdr = String.Format(">= {0:d}", date);
                }
                GridColumn col = dgInvStorageProfileGrid.Columns.Where(x => x.Name.Contains("Amount" + NumberConvert.ToString(i))).FirstOrDefault();
                if (col != null)
                    col.Header = hdr;
                date = NextDate;
            }
        }

        private void rdbMovement_Checked(object sender, RoutedEventArgs e)
        {
            if (rdbInhand != null && rdbMovement.IsChecked == true)
            {
                UpdateData(false);
                rdbInhand.IsChecked = false;
                isMovement = true;
            }
        }

        private void rdbInhand_Checked(object sender, RoutedEventArgs e)
        {
            if (rdbInhand.IsChecked == true)
            {
                UpdateData(true);
                rdbMovement.IsChecked = isMovement = false;
            }
        }

        void UpdateData(bool SetAccumulado)
        {
            var lst = (IEnumerable<InvStorageProfileLocalClient>)dgInvStorageProfileGrid?.ItemsSource;
            if (lst == null)
                return;
            foreach (var line in lst)
                line.SetValues(SetAccumulado);
            dgInvStorageProfileGrid.RefreshData();
            dgInvStorageProfileGrid.UpdateTotalSummary();
        }
    }
}
