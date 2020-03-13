using UnicontaClient.Models;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PartInvItemsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvBOMClient); } }
        public override bool Readonly { get { return false; } }
        public override IComparer GridSorting { get { return new InvBOMSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort
        {
            get
            {
                return false;
            }
        }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (InvBOMClient)this.SelectedItem;
            return (selectedItem?._ItemPart != null);
        }
    }
    public partial class PartInvItemsPage : GridBasePage
    {
        SQLCache items, warehouse;
        InvItemClient Invitem;
        UnicontaBaseEntity master;
        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, Invitem);
            base.PageClosing();
        }

        public PartInvItemsPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            if (syncEntity != null)
                InitPage(syncEntity.Row);
            SetHeader();
        }

        public PartInvItemsPage(UnicontaBaseEntity _master)
            : base(_master)
        {
            InitPage(_master);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            var item = args as Uniconta.DataModel.InvItem;
            if (item._ItemType < (byte)Uniconta.DataModel.ItemType.BOM)
                return;
            dgPartInvItemsGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetHeader()
        {
            var syncMaster = dgPartInvItemsGrid.masterRecord as Uniconta.DataModel.InvItem;
            string header = null;
            if (syncMaster != null)
                header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BOM"), syncMaster._Item);
            if (header != null)
                SetHeader(header);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if ( (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null))
            {
                dgPartInvItemsGrid.UpdateItemSource(argument);
            }
        }

        void InitPage(UnicontaBaseEntity _master)
        {   
            master = _master;
            Invitem = master as InvItemClient;
            InitializeComponent();
            ((TableView)dgPartInvItemsGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgPartInvItemsGrid;
            SetRibbonControl(localMenu, dgPartInvItemsGrid);
            dgPartInvItemsGrid.api = api;
            dgPartInvItemsGrid.UpdateMaster(master);
            dgPartInvItemsGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgPartInvItemsGrid.SelectedItemChanged += DgPartInvItemsGrid_SelectedItemChanged;
            InitialLoad();
            dgPartInvItemsGrid.ShowTotalSummary();
        }

        private void DgPartInvItemsGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var oldSelectedItem = e.OldItem as InvBOMClient;
            if (oldSelectedItem != null)
                oldSelectedItem.PropertyChanged -= DgPartInvItemsGrid_PropertyChanged;

            var selectedItem = e.NewItem as InvBOMClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += DgPartInvItemsGrid_PropertyChanged;
        }

        private void DgPartInvItemsGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvBOMClient;
            switch (e.PropertyName)
            {
                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
            }
        }

        async void setLocation(InvWarehouse master, InvBOMClient rec)
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

        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (warehouse == null)
                return;

            var selectedItem = dgPartInvItemsGrid.SelectedItem as InvBOMClient;
            if (selectedItem?._Warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = false;
            if (master is InvItem)
                showFields = (Invitem != null && Invitem._ItemType == (byte)Uniconta.DataModel.ItemType.BOM);
            else if (master is InvBOM)
            {
                var invBom = master as InvBOMClient;
                Invitem = invBom.InvItemMaster;
                showFields = Invitem._ItemType == (byte)Uniconta.DataModel.ItemType.BOM;
            }

            if (Invitem != null && Invitem._ItemType == (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                this.UnfoldBOM.Visible = true;

            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;

            MoveType.Visible = showFields;
            ShowOnInvoice.Visible = showFields;
            ShowOnPacknote.Visible = showFields;
            InclValueOnInvoice.Visible = showFields;

            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }

        private void InitialLoad()
        {
            var Comp = api.CompanyEntity;
            this.items = Comp.GetCache(typeof(Uniconta.DataModel.InvItem));
            this.warehouse = Comp.GetCache(typeof(InvWarehouse));
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (this.items == null)
                this.items = await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);
            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = api.GetCache(typeof(Uniconta.DataModel.InvWarehouse)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);
        }

        CorasauGridLookupEditorClient prevVariant1;
        private void variant1_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant1 != null)
                prevVariant1.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant1 = editor;
            editor.isValidate = true;
        }

        CorasauGridLookupEditorClient prevVariant2;
        private void variant2_GotFocus(object sender, RoutedEventArgs e)
        {
            if (prevVariant2 != null)
                prevVariant2.isValidate = false;
            var editor = (CorasauGridLookupEditorClient)sender;
            prevVariant2 = editor;
            editor.isValidate = true;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = GetSelectedItem();
            switch (ActionType)
            {
                case "AddRow":
                    dgPartInvItemsGrid.AddRow();
                    break;
                case "CopyRow":
                    dgPartInvItemsGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGridLocal();
                    break;
                case "DeleteRow":
                    dgPartInvItemsGrid.DeleteRow();
                    break;
                case "Storage":
                    ViewStorage();
                    break;
                case "CalculateSalesPrices":
                    CalcPrices();
                    break;
                case "InvBOMPartOfContains":
                    if (selectedItem != null && AppEnums.ItemType.IndexOf(selectedItem.ItemType) >= (byte)Uniconta.DataModel.ItemType.BOM)
                        AddDockItem(TabControls.PartInvItemsPage, selectedItem, string.Format("{0}:{1}/{2}", Uniconta.ClientTools.Localization.lookup("BOM"), selectedItem.ItemPart, selectedItem.Name));
                    break;
                case "InvBOMPartOfWhereUsed":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvBOMPartOfPage, selectedItem, string.Format("{0}:{1}/{2}", Uniconta.ClientTools.Localization.lookup("BOM"), selectedItem.ItemPart, selectedItem.Name));
                    break;
                case "InvBOMProductionPosted":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProductionPostedGridPage, selectedItem, string.Format("{0}:{1}/{2}", Uniconta.ClientTools.Localization.lookup("ProductionPosted"), selectedItem.ItemPart, selectedItem.Name));
                    break;
                case "HierarichalInvBOMReport":
                    AddDockItem(TabControls.InventoryHierarchicalBOMStatement, Invitem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), Invitem._Item));
                    break;
                case "AddNote":
                    if (selectedItem != null)
                    {
                        AddDockItem(TabControls.UserNotesPage, dgPartInvItemsGrid.syncEntity);
                    }
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        AddDockItem(TabControls.UserDocsPage, dgPartInvItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), selectedItem.RowId));
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private InvBOMClient GetSelectedItem()
        {
            var selectedItem = dgPartInvItemsGrid.SelectedItem as InvBOMClient;
            return selectedItem;
        }

        async void ViewStorage()
        {
            var savetask = saveGridLocal(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            AddDockItem(TabControls.InvItemStoragePage, dgPartInvItemsGrid.syncEntity, true);
        }

        bool refreshOnHand;
        Task saveGridLocal()
        {
            var invBom = dgPartInvItemsGrid.SelectedItem as InvBOMClient;
            refreshOnHand = invBom != null && invBom.RowId == 0;
            dgPartInvItemsGrid.SelectedItem = null;
            dgPartInvItemsGrid.SelectedItem = invBom;
            if (dgPartInvItemsGrid.HasUnsavedData)
                return saveGrid();
            return null;
        }

        Dictionary<int, double> LastRate;
        async Task<double> GetValue(double price, byte currency, double qty, byte MasterCurrency)
        {
            if (qty == 0d)
                return 0d;
            if (price == 0d)
                return double.NaN;
            if (currency != MasterCurrency)
            {
                if (LastRate == null)
                    LastRate = new Dictionary<int, double>();

                double d;
                if (!LastRate.TryGetValue(currency * 256 + MasterCurrency, out d))
                {
                    d = await this.api.session.ExchangeRate((Currencies)currency, (Currencies)MasterCurrency, DateTime.MinValue, api.CompanyEntity);
                    LastRate.Add(currency * 256 + MasterCurrency, d);
                }
                if (d == 0d)
                    return double.NaN;
                price *= d;
            }
            return price * qty;
        }

        async void CalcPrices()
        {
            var items = this.items;
            var item = Invitem;
            var stdQty = item._PurchaseQty;
            if (stdQty == 0d)
                stdQty = 1d;

            double p0 = 0d, p1 = 0d, p2 = 0d, p3 = 0d;

            var lst = dgPartInvItemsGrid.ItemsSource as IEnumerable<InvBOMClient>;
            foreach (var bom in lst)
            {
                var itm = (InvItem)items.Get(bom._ItemPart);
                if (itm != null)
                {
                    var qty = bom.GetBOMQty(stdQty);

                    p0 += (qty * itm._CostPrice);

                    double val;
                    if (!double.IsNaN(p1))
                    {
                        val = await GetValue(itm._SalesPrice1, itm._Currency1, qty, item._Currency1);
                        if (!double.IsNaN(val))
                            p1 += val;
                        else
                            p1 = double.NaN;
                    }
                    if (!double.IsNaN(p2))
                    {
                        val = await GetValue(itm._SalesPrice2, itm._Currency2, qty, item._Currency2);
                        if (!double.IsNaN(val))
                            p2 += val;
                        else
                            p2 = double.NaN;
                    }
                    if (!double.IsNaN(p3))
                    {
                        val = await GetValue(itm._SalesPrice3, itm._Currency3, qty, item._Currency3);
                        if (!double.IsNaN(val))
                            p3 += val;
                        else
                            p3 = double.NaN;
                    }
                }
            }

            p0 = Math.Round(p0 / stdQty, 2);
            p1 = Math.Round(p1 / stdQty, 2);
            p2 = Math.Round(p2 / stdQty, 2);
            p3 = Math.Round(p3 / stdQty, 2);

            var dailog = new CWCalculateSalesPrice(p0, p1, p2, p3, Invitem);
            dailog.Closed += delegate
            {
                if (dailog.DialogResult == true)
                {
                    var org = StreamingManager.Clone(Invitem);
                    if (dailog.costPrice != 0d)
                        Invitem.CostPrice = dailog.costPrice;
                    if (dailog.salesPrice1 != 0d)
                        Invitem.SalesPrice1 = dailog.salesPrice1;
                    if (dailog.salesPrice2 != 0d)
                        Invitem.SalesPrice2 = dailog.salesPrice2;
                    if (dailog.salesPrice3 != 0d)
                        Invitem.SalesPrice3 = dailog.salesPrice3;
                    api.UpdateNoResponse(org, Invitem);
                }
            };
            dailog.Show();
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            if (dgPartInvItemsGrid.HasUnsavedData)
            {
                ErrorCodes res = await dgPartInvItemsGrid.SaveData();
                if (res == ErrorCodes.Succes)
                {
                    globalEvents.OnRefresh(NameOfControl, Invitem);
                    if (refreshOnHand)
                    {
                        refreshOnHand = false;
                    }
                }
                return res;
            }
            return 0;
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;
            return true;
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var invBOM = (sender as Image).Tag as InvBOMClient;
            if (invBOM != null)
                AddDockItem(TabControls.UserDocsPage, invBOM);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var invBOM = (sender as Image).Tag as InvBOMClient;
            if (invBOM != null)
                AddDockItem(TabControls.UserNotesPage, invBOM);
        }
    }
}
