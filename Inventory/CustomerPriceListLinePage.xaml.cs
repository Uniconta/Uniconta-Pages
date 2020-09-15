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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPriceListLineClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPriceListLineClient); } }
        public override IComparer GridSorting { get { return new InvPristListLineSort(); } }
        public override bool Readonly { get { return false; } }
        protected override List<string> GridSkipFields { get { return new List<string>(2) { "Name", "ItemGroupName" }; } }
        protected override bool SetValuesOnPaste { get { return true; } }
        public override bool SingleBufferUpdate { get { return false; } }

        internal byte FixedDCType;
        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var rec = (InvPriceListLineClient)dataEntity;
            rec.ExchangeRate = this.ExchangeRate;
            if (FixedDCType != 0)
                rec._DCType = FixedDCType;
        }

        internal double ExchangeRate;
        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            var ExchangeRate = this.ExchangeRate;
            if (ExchangeRate == 0d)
                return;

            var lst = Arr as IEnumerable<InvPriceListLineClient>;
            if (lst != null)
            {
                foreach (var rec in lst)
                    rec.ExchangeRate = ExchangeRate;
            }
        }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            var row = copyFromRows.FirstOrDefault();
            var type = this.TableTypeUser;
            if (row is InvItem)
            {
                var lst = new List<InvPriceListLineClient>(copyFromRows.Count());
                foreach (var _it in copyFromRows)
                {
                    var it = (InvItem)_it;
                    var line = new InvPriceListLineClient();
                    line._Item = it._Item;
                    line._Price = it._SalesPrice1;
                    lst.Add(line);
                }
                return lst;
            }
            else if (row.GetType() == type)
            {
                foreach (var inv in copyFromRows)
                   ((InvPriceListLineClient)inv).ExchangeRate = this.ExchangeRate;

                return copyFromRows;
            }
            return null;
        }
    }

    public partial class CustomerPriceListLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CustomerPriceListLinePage; } }
        public CustomerPriceListLinePage(UnicontaBaseEntity sourcedata)
            : base(sourcedata)
        {
            InitializeComponent();
            Init(sourcedata);
        }
        public CustomerPriceListLinePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            Init(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvPriceListLineClientGrid.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgInvPriceListLineClientGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SalesPricesAndDiscounts"), key);
            SetHeader(header);
        }

        private void Init(UnicontaBaseEntity master)
        {
            dgInvPriceListLineClientGrid.UpdateMaster(master);
            localMenu.dataGrid = dgInvPriceListLineClientGrid;
            SetRibbonControl(localMenu, dgInvPriceListLineClientGrid);
            dgInvPriceListLineClientGrid.api = api;
            dgInvPriceListLineClientGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            var Comp = api.CompanyEntity;
            if (Comp.InvPrice != Comp.CreditorPrice)
                dgInvPriceListLineClientGrid.FixedDCType = Comp.InvPrice ? (byte)1 : (byte)2;

            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            dgInvPriceListLineClientGrid.RowDoubleClick += DgInvPriceListLineClientGrid_RowDoubleClick;
        }

        public override Task InitQuery()
        {
            return BindGrid();
        }

        async Task BindGrid()
        { 
            Currencies PriceCurrency;
            var Comp = api.CompanyEntity;
            var master = dgInvPriceListLineClientGrid.masterRecords?.First();

            var priceList = master as Uniconta.DataModel.InvPriceList;
            if (priceList != null)
                PriceCurrency = (Currencies)priceList._Currency;
            else
            {
                var dc = master as Uniconta.DataModel.DCAccount;
                if (dc != null)
                    PriceCurrency = dc._Currency;
                else
                    PriceCurrency = 0;
            }

            double ExchangeRate;
            if (! Comp.SameCurrency(PriceCurrency))
            {
                ExchangeRate = await api.session.ExchangeRate(PriceCurrency, (Currencies)Comp._Currency, DateTime.Now, Comp);
                if (ExchangeRate == 1d)
                    ExchangeRate = 0d;
            }
            else
                ExchangeRate = 0d;

            dgInvPriceListLineClientGrid.ExchangeRate = ExchangeRate;
            await dgInvPriceListLineClientGrid.Filter(null);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool ShowDC = true, ShowItem = true;
            var master = dgInvPriceListLineClientGrid.masterRecords?.First();

            var dc = master as Uniconta.DataModel.DCAccount;
            if (dc != null)
            {
                ShowDC = false;
                if (dc.__DCType() == 1)
                    PriceListCreditor.Visible = false;
                else
                {
                    PriceListDebtor.Visible = false;
                    SalesCharge.Visible = false;
                    Margin.Visible = false;
                    MarginRatio.Visible = false;
                    FixedContributionRate.Visible = false;
                }
            }
            if (master is Uniconta.DataModel.InvPriceList)
                ShowDC = false;
            if (master is Uniconta.DataModel.InvItem)
                ShowItem = false;

            Item.Visible = Name.Visible = ShowItem;
            if (ShowDC)
                PriceListCreditor.Visible = PriceListDebtor.Visible = DCType.Visible = ShowDC;

            var Comp = api.CompanyEntity;
            if (!Comp.CreditorPrice)
                PriceListCreditor.Visible = false;
            if (!Comp.InvPrice)
                PriceListDebtor.Visible = false;
            if (Comp.InvPrice != Comp.CreditorPrice)
                DCType.Visible = false;
            if (!Comp.UnitConversion)
                Unit.Visible = false;
        }

        private void DgInvPriceListLineClientGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("EditRow");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvPriceListLineClientGrid.SelectedItem as InvPriceListLineClient;
            InvPriceListLineClient rec;
            switch (ActionType)
            {
                case "AddRow":
                    rec = dgInvPriceListLineClientGrid.AddRow() as InvPriceListLineClient;
                    if (rec != null)
                        rec.ExchangeRate = dgInvPriceListLineClientGrid.ExchangeRate;
                    break;
                case "CopyRow":
                    rec = dgInvPriceListLineClientGrid.CopyRow() as InvPriceListLineClient;
                    if (rec != null)
                        rec.ExchangeRate = dgInvPriceListLineClientGrid.ExchangeRate;
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgInvPriceListLineClientGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        
        protected override Task<ErrorCodes> saveGrid()
        {
            if (!BlankFieldExist())
            {
                var t = base.saveGrid();
                var lst = dgInvPriceListLineClientGrid.ItemsSource as IEnumerable<InvPriceListLineClient>;
                var rec = lst?.FirstOrDefault();
                if (rec != null)
                {
                    InvPriceList plst = rec.MasterDebtorPriceList;
                    if (plst == null)
                        plst = rec.MasterCreditorPriceList;
                    if (plst != null)
                        plst.ItemPrices = null; // clear cache
                }
                return t;
            }
            else
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("ItemAndGroup")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                return null;
            }
        }

        private bool BlankFieldExist()
        {
           foreach(var row in dgInvPriceListLineClientGrid.GetVisibleRows())
           {
                var item = row as InvPriceListLineClient;
                if (item != null && item._Item == null && item._ItemGroup == null && item._DiscountGroup == null)
                    return true;
           }
           return false;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.InvGroup), typeof(Uniconta.DataModel.InvDiscountGroup), typeof(Uniconta.DataModel.InvItem) });
        }
    }
}
