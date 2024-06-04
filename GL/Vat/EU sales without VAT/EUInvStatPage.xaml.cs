using UnicontaClient.Pages;
using System;
using System.Collections;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EUInvStatPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EUInvStat); } }
        public override IComparer GridSorting { get { return new EUInvStatCmp(); } }

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            base.PrintGrid(PrintReportName, printparam, format, page);
        }

        public string PrintReportName { get; set; }
    }

    public partial class EUInvStatPage : GridBasePage
    {
        static DateTime toDate;
        static DateTime fromDate;

        CWServerFilter itemFilterDialog;
        bool itemFilterCleared;
        public TableField[] ItemUserFields { get; set; }

        public EUInvStatPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        public EUInvStatPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgEUInvStatGrid;
            SetRibbonControl(localMenu, dgEUInvStatGrid);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;

            if (toDate == DateTime.MinValue)
            {
                toDate = BasePage.GetSystemDefaultDate();
                fromDate = toDate.AddDays(1 - toDate.Day);
            }
            ToDate.DateTime = toDate;
            FromDate.DateTime = fromDate;
            dgEUInvStatGrid.ShowTotalSummary();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "PerWarehouse", "PerLocation" });
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "Aggregate" });

            LoadType(typeof(Uniconta.DataModel.Debtor));
            Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
            CostValue.Visible = CostValue.ShowInColumnChooser = !api.CompanyEntity.HideCostPrice;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ItemFilter":
                    if (itemFilterDialog == null)
                    {
                        if (itemFilterCleared)
                            itemFilterDialog = new CWServerFilter(api, typeof(EUInvStat), null, null, ItemUserFields);
                        else
                            itemFilterDialog = new CWServerFilter(api, typeof(EUInvStat), null, null, ItemUserFields);
                        itemFilterDialog.GridSource = dgEUInvStatGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        itemFilterDialog.Closing += itemFilterDialog_Closing;
                        itemFilterDialog.Show();
                    }
                    else
                    {
                        itemFilterDialog.GridSource = dgEUInvStatGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        itemFilterDialog.Show(true);
                    }
                    break;
                case "ClearItemFilter":
                    itemFilterDialog = null;
                    itemFilterValues = null;
                    itemFilterCleared = true;
                    break;
                case "Search":
                    LoadData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        IEnumerable<PropValuePair> itemFilterValues;
        FilterSorter itemPropSort;

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

        private void LoadData()
        {
            var reportContent = string.Format("{0:d} {1} {2:d})", FromDate.DateTime, "-", ToDate.DateTime);
            dgEUInvStatGrid.PrintReportName = string.Format("{0} {1} ({2})", Uniconta.ClientTools.Localization.lookup("EUsales"), Uniconta.ClientTools.Localization.lookup("Report"), reportContent);
            var inputs = new List<PropValuePair>();

            inputs.Add(PropValuePair.GenereteParameter("FromDate", typeof(DateTime), String.Format("{0:d}", FromDate.DateTime)));
            inputs.Add(PropValuePair.GenereteParameter("ToDate", typeof(DateTime), String.Format("{0:d}", ToDate.DateTime)));

            if (itemFilterValues != null)
                inputs.AddRange(itemFilterValues);
            var t = Filter(inputs);
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgEUInvStatGrid.Filter(propValuePair);
        }
    }
}
