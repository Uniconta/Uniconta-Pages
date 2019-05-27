using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ItemStockStatusPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvStockStatus); } }
        public override IComparer GridSorting { get { return new InvSumInvCmp(); } }

        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            base.PrintGrid(PrintReportName, printparam, format, page);
        }

        public string PrintReportName { get; set; }
    }

    public partial class ItemStockStatusPage : GridBasePage
    {
        static DateTime balDate;

        CWServerFilter itemFilterDialog;
        bool itemFilterCleared;
        public TableField[] ItemUserFields { get; set; }

        public ItemStockStatusPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public ItemStockStatusPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgItemStockStatusGrid;
            SetRibbonControl(localMenu, dgItemStockStatusGrid);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            if (balDate == DateTime.MinValue)
                balDate = BasePage.GetSystemDefaultDate();
            ToDate.DateTime = balDate;
            dgItemStockStatusGrid.ShowTotalSummary();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "Aggregate" );
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            if (!Comp.Warehouse)
                Location.Visible = Warehouse.Visible = false;
            else if (!Comp.Location)
                Location.Visible = false;

            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            LoadType(typeof(Uniconta.DataModel.InvItem));
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ItemFilter":
                    if (itemFilterDialog == null)
                    {
                        if (itemFilterCleared)
                            itemFilterDialog = new CWServerFilter(api, typeof(InvStockStatus), null, null, ItemUserFields);
                        else
                            itemFilterDialog = new CWServerFilter(api, typeof(InvStockStatus), null, null, ItemUserFields);
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
            dgItemStockStatusGrid.PrintReportName = string.Format("{0} {1} ({2:d})", Uniconta.ClientTools.Localization.lookup("ItemStockStatus"), Uniconta.ClientTools.Localization.lookup("Report"), ToDate.DateTime);
            var inputs = new List<PropValuePair>();
            inputs.Add(PropValuePair.GenereteParameter("ToDate", typeof(DateTime), String.Format("{0:d}", ToDate.DateTime)));

            if (itemFilterValues != null)
                inputs.AddRange(itemFilterValues);
            var t = Filter(inputs);
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgItemStockStatusGrid.Filter(propValuePair);
        }
    }
}
