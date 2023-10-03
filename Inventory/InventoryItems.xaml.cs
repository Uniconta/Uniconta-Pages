using Uniconta.API.Inventory;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
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
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.API.Service;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InventoryItemsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvItemClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class InventoryItems : GridBasePage
    {
        ItemBase ibase;
        SQLCache warehouse;
        public override string NameOfControl
        {
            get { return TabControls.InventoryItems.ToString(); }
        }
        public InventoryItems(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public InventoryItems(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            StartLoadCache();
            InitializeComponent();
            SetVariants();
            LayoutControl = detailControl.layoutItems;
            dgInventoryItemsGrid.api = api;
            SetRibbonControl(localMenu, dgInventoryItemsGrid);
            dgInventoryItemsGrid.BusyIndicator = busyIndicator;

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInventoryItemsGrid.SelectedItemChanged += DgInventoryItemsGrid_SelectedItemChanged;
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += RootVisual_KeyDown;
#else
            this.PreviewKeyDown += RootVisual_KeyDown;
#endif
            this.BeforeClose += DebtorAccount_BeforeClose;
        }

        private void RootVisual_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8 && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                ribbonControl.PerformRibbonAction("InvTrans");
        }

        private void DebtorAccount_BeforeClose()
        {
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown -= RootVisual_KeyDown;
#else
            this.PreviewKeyDown -= RootVisual_KeyDown;
#endif
        }

        private void DgInventoryItemsGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvItemClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= DgInventoryItemsGrid_PropertyChanged;
            var selectedItem = e.NewItem as InvItemClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += DgInventoryItemsGrid_PropertyChanged;
        }

        private void DgInventoryItemsGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvItemClient;
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

        async void setLocation(InvWarehouse master, InvItemClient rec)
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

            var selectedItem = dgInventoryItemsGrid.SelectedItem as InvItemClient;
            if (selectedItem?._Warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }

        protected override void OnLayoutCtrlLoaded()
        {
            detailControl.api = api;
        }
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var lst = new List<Type>(10) { typeof(Uniconta.DataModel.InvGroup) };
            var Comp = api.CompanyEntity;
            if (Comp.ItemVariants)
                lst.Add(typeof(Uniconta.DataModel.InvStandardVariant));
            if (Comp.Warehouse)
                lst.Add(typeof(Uniconta.DataModel.InvWarehouse));
            lst.Add(typeof(Uniconta.DataModel.InvBrandGroup));
            lst.Add(typeof(Uniconta.DataModel.InvCategoryGroup));
            lst.Add(typeof(Uniconta.DataModel.InvDiscountGroup));
            if (Comp.InvDuty)
                lst.Add(typeof(Uniconta.DataModel.InvDutyGroup));
            LoadType(lst);

            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = await Comp.LoadCache(typeof(Uniconta.DataModel.InvWarehouse), api).ConfigureAwait(false);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            if (!Comp.Storage || Comp.StorageOnAll)
                UseStorage.Visible = UseStorage.ShowInColumnChooser = false;
            else
                UseStorage.ShowInColumnChooser = true;
            if (!Comp.SerialBatchNumbers)
                SerialOrBatch.Visible = SerialOrBatch.ShowInColumnChooser = false;
            else
                SerialOrBatch.ShowInColumnChooser = true;
            if (!Comp.ItemVariants)
            {
                clStandardVariant.Visible = clStandardVariant.ShowInColumnChooser = false;
                UseVariants.Visible = UseVariants.ShowInColumnChooser = false;
            }
            else
                clStandardVariant.ShowInColumnChooser = UseVariants.ShowInColumnChooser = true;
            if (!Comp.Project)
            {
                PayrollCategory.Visible = PayrollCategory.ShowInColumnChooser = false;
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
            }
            else
                PayrollCategory.ShowInColumnChooser = PrCategory.ShowInColumnChooser = true;
            if (!Comp.Location || !Comp.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!Comp.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!Comp.InvPrice)
                DiscountGroup.Visible = DiscountGroup.ShowInColumnChooser = false;
            else
                DiscountGroup.ShowInColumnChooser = true;
            if (!Comp.Storage)
            {
                QtyReserved.Visible = QtyReserved.ShowInColumnChooser = false;
                QtyOrdered.Visible = QtyOrdered.ShowInColumnChooser = false;
                Available.Visible = Available.ShowInColumnChooser = false;
                AvailableForReservation.Visible = AvailableForReservation.ShowInColumnChooser = false;
            }
            else
            {
                QtyReserved.ShowInColumnChooser = QtyOrdered.ShowInColumnChooser = Available.ShowInColumnChooser = AvailableForReservation.ShowInColumnChooser = true;
            }
            if (!Comp.UnitConversion)
            {
                SalesUnit.Visible = SalesUnit.ShowInColumnChooser = false;
                PurchaseUnit.Visible = PurchaseUnit.ShowInColumnChooser = false;
                UnitGroup.Visible = UnitGroup.ShowInColumnChooser = false;
            }
            else
                SalesUnit.ShowInColumnChooser = PurchaseUnit.ShowInColumnChooser = UnitGroup.ShowInColumnChooser = true;
            if (!Comp.InvDuty)
                DutyGroup.Visible = DutyGroup.ShowInColumnChooser = false;
            else
                DutyGroup.ShowInColumnChooser = true;
            setDim();
            dgInventoryItemsGrid.Readonly = true;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InventoryItemPage2 || screenName == TabControls.PartInvItemsPage || (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null))
            {
                dgInventoryItemsGrid.UpdateItemSource(argument);
                if (dgInventoryItemsGrid.Visibility == Visibility.Collapsed)
                    detailControl.Refresh(argument, dgInventoryItemsGrid.SelectedItem);
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = GetSelectedItem();

            switch (ActionType)
            {
                case "EditAll":
                    if (dgInventoryItemsGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    AddDockItem(TabControls.InventoryItemPage2, api, Uniconta.ClientTools.Localization.lookup("InventoryItems"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryItemPage2, new object[2] { selectedItem, true }, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("InventoryItems"), selectedItem._Item));
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgInventoryItemsGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgInventoryItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Item));
                    break;
                case "InvTrans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryTransactions, dgInventoryItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvTransactions"), selectedItem._Item));
                    break;
                case "Statistics":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryTotals, dgInventoryItemsGrid.syncEntity);
                    break;
                case "InvBOMPartOfContains":
                    if (selectedItem != null && selectedItem._ItemType >= (byte)Uniconta.DataModel.ItemType.BOM)
                        AddDockItem(TabControls.PartInvItemsPage, dgInventoryItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BOM"), selectedItem._Item));
                    break;
                case "InvBOMPartOfWhereUsed":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvBOMPartOfPage, dgInventoryItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BOM"), selectedItem._Item));
                    break;
                case "RefreshGrid":
                    if (gridControl.Visibility == Visibility.Visible)
                        gridRibbon_BaseActions(ActionType);
                    break;
                case "AddLine":
                    dgInventoryItemsGrid.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                    {
                        if (copyRowIsEnabled)
                            ClearValues(dgInventoryItemsGrid.CopyRow() as InvItem);
                        else
                            CopyRecord(selectedItem);
                    }
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgInventoryItemsGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "Storage":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvItemStoragePage, dgInventoryItemsGrid.syncEntity, true, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OnHand"), selectedItem._Item));
                    break;
                case "VariantCombi":
                    if (selectedItem != null && selectedItem._UseVariants)
                        AddDockItem(TabControls.InvVariantCombiPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ItemVariants"), selectedItem._Item));
                    break;
                case "VariantDetails":
                    if (selectedItem != null && selectedItem._UseVariants)
                        AddDockItem(TabControls.InvVariantDetailPage, dgInventoryItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ItemVariants"), selectedItem._Item));
                    break;
                case "LanguageItemText":
                    if (selectedItem != null)
                        AddDockItem(TabControls.LanguageItemTextPage, dgInventoryItemsGrid.syncEntity);
                    break;
                case "InventorySeriesBatch":
                    if (selectedItem != null && selectedItem._UseSerialBatch)
                    {
                        string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), selectedItem._Item);
                        AddDockItem(TabControls.InvSeriesBatch, dgInventoryItemsGrid.syncEntity, header);
                    }
                    break;
                case "Prices":
                    if (selectedItem != null)
                        AddDockItem(TabControls.CustomerPriceListLinePage, dgInventoryItemsGrid.syncEntity);
                    break;
                case "InvBOMProductionPosted":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProductionPostedGridPage, dgInventoryItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionPosted"), selectedItem._Item));
                    break;
                case "InvReservation":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryReservationReport, dgInventoryItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Reservations"), selectedItem._Item));
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "InvPurchaseAccounts":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvPurchaseAccountPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PurchaseAccounts"), selectedItem._Item));
                    break;
                case "InvHierarichalBOM":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryHierarchicalBOMStatement, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), selectedItem._Item));
                    break;
                case "UndoDelete":
                    dgInventoryItemsGrid.UndoDeleteRow();
                    break;
                case "InvExplodeBOM":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvBOMExplodePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ExplodedBOM"), selectedItem._Item));
                    break;
                case "InvTransPivot":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvTransPivotPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Pivot"), selectedItem._Item));
                    break;
                case "InvStockProfile":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvStorageProfileReport, dgInventoryItemsGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), selectedItem._Item));
                    break;
                case "ViewAttachment":
                    if (selectedItem != null)
                        ViewDocument(UnicontaTabs.UserDocsPage3, dgInventoryItemsGrid.syncEntity, string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"), Uniconta.ClientTools.Localization.lookup("Attachment")), ViewerType.Attachment);
                    break;
                case "ViewWeb":
                    if (selectedItem != null)
                        ViewDocument(UnicontaTabs.UserDocsPage3, dgInventoryItemsGrid.syncEntity, string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"), Uniconta.ClientTools.Localization.lookup("Url")), ViewerType.Url);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        static void ClearValues(InvItem item)
        {
            item._EAN = null;
            item._Qty = 0;
            item._CostValue = 0;
            item._qtyOnStock = 0;
            item._qtyOrdered = 0;
            item._qtyReserved = 0;
            item.HasNotes = false;
            item.HasDocs = false;
        }
        void CopyRecord(InvItemClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var item = Activator.CreateInstance(selectedItem.GetType()) as InvItemClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, item);
            ClearValues(item);
            AddDockItem(TabControls.InventoryItemPage2, new object[] { item, false }, Uniconta.ClientTools.Localization.lookup("InventoryItems"), "Add_16x16");
        }

        bool copyRowIsEnabled;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgInventoryItemsGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgInventoryItemsGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgInventoryItemsGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                editAllChecked = false;
                copyRowIsEnabled = true;
            }
            else
            {
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                    {
                        if (confirmationDialog.DialogResult == null)
                            return;

                        switch (confirmationDialog.ConfirmationResult)
                        {
                            case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                var err = await dgInventoryItemsGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                break;
                        }
                        editAllChecked = true;
                        dgInventoryItemsGrid.Readonly = true;
                        dgInventoryItemsGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgInventoryItemsGrid.Readonly = true;
                    dgInventoryItemsGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    copyRowIsEnabled = false;
                }
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgInventoryItemsGrid.HasUnsavedData;
            }
        }
        private async void Save()
        {
            SetBusy();
            var err = await dgInventoryItemsGrid.SaveData();
            if (err != ErrorCodes.Succes)
                api.AllowBackgroundCrud = true;
            ClearBusy();
        }
        private InvItemClient GetSelectedItem()
        {
            var selectedItem = dgInventoryItemsGrid.SelectedItem as InvItemClient;
            return selectedItem;
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgInventoryItemsGrid.Filter(propValuePair);
        }
        void SetVariants()
        {
            var comp = api.CompanyEntity;
            if (!comp.ItemVariants)
            {
                UseVariants.Visible = UseVariants.ShowInColumnChooser = false;
                clStandardVariant.Visible = clStandardVariant.ShowInColumnChooser = false;
            }
        }
        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void dgInventoryItemsGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            if (ibase == null)
                return;
#if SILVERLIGHT
            var selectedItem = GetSelectedItem();
            if (selectedItem != null)
                ibase.isEditLayout = (selectedItem._ItemType >= (byte)Uniconta.DataModel.ItemType.BOM);
#endif
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var invItem = (sender as Image).Tag as InvItemClient;
            if (invItem != null)
                AddDockItem(TabControls.UserDocsPage, dgInventoryItemsGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var invItem = (sender as Image).Tag as InvItemClient;
            if (invItem != null)
                AddDockItem(TabControls.UserNotesPage, dgInventoryItemsGrid.syncEntity);
        }
    }
}
