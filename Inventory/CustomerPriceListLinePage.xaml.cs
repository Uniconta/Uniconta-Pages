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
using Uniconta.API.Service;

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
        public override bool IsAutoSave { get { return false; } }

        internal byte FixedDCType;
        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            if (FixedDCType != 0)
                ((Uniconta.DataModel.InvPriceListLine)dataEntity)._DCType = FixedDCType;
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
                /*
                Give problems setting Exchange rate
                foreach (var inv in copyFromRows)
                   ((InvPriceListLineClient)inv).ExchangeRate = this.ExchangeRate;
                */
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
            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("SalesPricesAndDiscounts"), ": ", key);
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

        Task BindGrid()
        { 
            return dgInvPriceListLineClientGrid.Filter(null);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool ShowDC = true, ShowItem = true;
            var master = dgInvPriceListLineClientGrid.masterRecords?.First();

            var dc = master as Uniconta.DataModel.DCAccount;
            if (dc != null)
            {
                dgInvPriceListLineClientGrid.FixedDCType = dc.__DCType();
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
            var pList = master as Uniconta.DataModel.InvPriceList;
            if (pList != null)
            {
                dgInvPriceListLineClientGrid.FixedDCType = pList.__DCType();
                ShowDC = false;
            }
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
                Unit.Visible = Unit.ShowInColumnChooser = UnitGroup.Visible = UnitGroup.ShowInColumnChooser = false;

        }

        private void DgInvPriceListLineClientGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("EditRow");
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInvPriceListLineClientGrid.AddRow();
                    break;
                case "CopyRow":
                    dgInvPriceListLineClientGrid.CopyRow();
                    break;
                case "DeleteRow":
                    if (dgInvPriceListLineClientGrid.SelectedItem != null)
                        dgInvPriceListLineClientGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void PageClosing()
        {
            ClearCache();
            base.PageClosing();
        }

        void ClearCache()
        {
            string priceList = null;
            var lst = dgInvPriceListLineClientGrid.ItemsSource as IEnumerable<InvPriceListLineClient>;
            if (lst != null)
            {
                foreach (var rec in lst)
                {
                    if (rec._PriceList != priceList)
                    {
                        priceList = rec._PriceList;
                        InvPriceList plst = (InvPriceList)rec.MasterDebtorPriceList ?? (InvPriceList)rec.MasterCreditorPriceList;
                        if (plst != null)
                            plst.ItemPrices = null; // clear cache
                    }
                }
            }
        }

        protected override Task<ErrorCodes> saveGrid()
        {
            if (!BlankFieldExist())
            {
                ClearCache();
                return base.saveGrid();
            }
            else
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("ItemAndGroup")), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                return BaseAPI.RetErr(ErrorCodes.FieldCannotBeBlank);
            }
        }

        private bool BlankFieldExist()
        {
            foreach (var item in dgInvPriceListLineClientGrid.ItemsSource as IEnumerable<InvPriceListLineClient>)
            {
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
