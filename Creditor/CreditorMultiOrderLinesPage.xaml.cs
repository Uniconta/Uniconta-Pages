using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.Service;
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
    public class CreditorMultiOrderLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorOrderLineClient); } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (DCOrderLine)this.SelectedItem;
            return (selectedItem != null) && selectedItem._OrderNumber != 0 && (selectedItem._Item != null || selectedItem._Text != null);
        }
        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var last = (this.ItemsSource as IEnumerable<CreditorOrderLineClient>)?.LastOrDefault();
            if (last != null)
            {
                var newRow = (DCOrderLine)dataEntity;
                newRow.SetMaster(last);
            }
        }
    }

    /// <summary>
    /// Interaction logic for CreditorMultiOrderLinesPage.xaml
    /// </summary>
    public partial class CreditorMultiOrderLinesPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CreditorMultiOrderLinesPage; } }
        SQLCache items, warehouse, creditorOrders;
        Dictionary<int, Uniconta.API.DebtorCreditor.FindPrices> dictPriceLookup;

        public CreditorMultiOrderLinesPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            dgMultiCreditorOrderLine.UpdateMaster(api.CompanyEntity);
            SetRibbonControl(localMenu, dgMultiCreditorOrderLine);
            dgMultiCreditorOrderLine.api = api;
            dgMultiCreditorOrderLine.BusyIndicator = busyIndicator;

            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dictPriceLookup = new Dictionary<int, Uniconta.API.DebtorCreditor.FindPrices>();
            dgMultiCreditorOrderLine.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            InitialLoad();
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "AddOrder");
            if (ibase != null)
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("PurchaseOrder"));
        }

        public override Task InitQuery()
        {
            return null;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorOrdersPage2)
            {
                var args = argument as object[];
                var order = args[args.Length - 1] as CreditorOrderClient;
                if (order != null)
                {
                    var row = Activator.CreateInstance(dgMultiCreditorOrderLine.TableTypeUser) as CreditorOrderLineClient;
                    row._OrderNumber = order._OrderNumber;
                    dgMultiCreditorOrderLine.AddRow(row);
                    dataChanged = true;
                }
            }
        }

        private void InitialLoad()
        {
            var comp = api.CompanyEntity;
            items = comp.GetCache(typeof(Uniconta.DataModel.InvItem));
            warehouse = comp.GetCache(typeof(Uniconta.DataModel.InvWarehouse));
            creditorOrders = comp.GetCache(typeof(Uniconta.DataModel.CreditorOrder));
            dgMultiCreditorOrderLine.Visibility = Visibility.Visible;
            if (comp.UnitConversion)
                Unit.Visible = true;
        }

        async protected override void LoadCacheInBackGround()
        {
            var api = this.api;

            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);

            if (this.creditorOrders == null)
                this.creditorOrders =await api.LoadCache(typeof(Uniconta.DataModel.CreditorOrder)).ConfigureAwait(false);

            if (api.CompanyEntity.Warehouse && this.warehouse == null)
                this.warehouse = await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);
        }
        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as CreditorOrderLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= CreditorOrderLineGrid_PropertyChanged;
            var selectedItem = e.NewItem as CreditorOrderLineClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += CreditorOrderLineGrid_PropertyChanged;
        }

        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            if (warehouse == null)
                return;

            var selectedItem = dgMultiCreditorOrderLine.SelectedItem as CreditorOrderLineClient;
            if (selectedItem?._Warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                SetLocation(selected, selectedItem);
                if (prevLocation != null)
                    prevLocation.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevLocation = editor;
                editor.isValidate = true;
            }
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

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Project)
            {
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
                Project.Visible = Project.ShowInColumnChooser = false;
            }
            else
            {
                PrCategory.ShowInColumnChooser = true;
                Project.ShowInColumnChooser = true;
            }
            if (!company.Storage)
            {
                Storage.Visible = Storage.ShowInColumnChooser = false;
                QtyDelivered.Visible = QtyDelivered.ShowInColumnChooser = false;
            }
            else if (!company._OrderLineEditDelivered)
                QtyDelivered.Visible = QtyDelivered.ShowInColumnChooser = false;
            else
            {
                Storage.ShowInColumnChooser = true;
                QtyDelivered.ShowInColumnChooser = true;
            }
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            if (!company.SerialBatchNumbers)
                SerieBatch.Visible = SerieBatch.ShowInColumnChooser = false;
            else
                SerieBatch.ShowInColumnChooser = true;
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, colDim1, colDim2, colDim3, colDim4, colDim5);
        }

        Uniconta.API.DebtorCreditor.FindPrices SetPriceLookup(CreditorOrderLineClient rec)
        {
            var OrderNumber = rec.OrderRowId;
            if (OrderNumber != 0)
            {
                if (dictPriceLookup.ContainsKey(OrderNumber))
                    return dictPriceLookup[OrderNumber];

                var order = (CreditorOrder)creditorOrders.Get(OrderNumber);
                if (order != null)
                {
                    var priceLookup = new Uniconta.API.DebtorCreditor.FindPrices(order, api);
                    dictPriceLookup.Add(OrderNumber, priceLookup);
                    return priceLookup;
                }
            }
            return null;
        }

        void UpdatePrice(CreditorOrderLineClient rec)
        {
            var priceLookup = SetPriceLookup(rec);
            if (priceLookup != null && priceLookup.UseCustomerPrices)
                priceLookup.GetCustomerPrice(rec, false);
        }

        private void CreditorOrderLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as CreditorOrderLineClient;
            switch (e.PropertyName)
            {
                case "Item":
                    var selectedItem = (InvItem)items?.Get(rec._Item);
                    if (selectedItem != null)
                    {
                        if (selectedItem._AlternativeItem != null && selectedItem._UseAlternative == UseAlternativeItem.Always)
                        {
                            var altItem = (InvItem)items.Get(selectedItem._AlternativeItem);
                            if (altItem != null && altItem._AlternativeItem == null)
                            {
                                rec.Item = selectedItem._AlternativeItem;
                                return;
                            }
                        }
                        var lookup = SetPriceLookup(rec);
                        if (lookup != null)
                            lookup.UseCustomerPrices = false;
                        if (selectedItem._SalesQty != 0d)
                            rec.Qty = selectedItem._SalesQty;
                        else if (api.CompanyEntity._PurchaseLineOne)
                            rec.Qty = 1d;
                        if (api.CompanyEntity._InvoiceUseQtyNowCre)
                            rec.QtyNow = rec._Qty;
                        rec.SetItemValues(selectedItem, api.CompanyEntity._PurchaseLineStorage);
                        if (lookup != null)
                        {
                            lookup.UseCustomerPrices = true;
                            lookup.SetPriceFromItem(rec, selectedItem);
                        }
                        else if (selectedItem._PurchasePrice != 0)
                            rec.Price = selectedItem._PurchasePrice;
                        else
                            rec.Price = selectedItem._CostPrice;
                        TableField.SetUserFieldsFromRecord(selectedItem, rec);
                        if (selectedItem._Blocked)
                            UtilDisplay.ShowErrorCode(ErrorCodes.ItemIsOnHold, null);
                    }
                    break;
                case "OrderNumber":
                    var order = (CreditorOrder)creditorOrders?.Get(NumberConvert.ToString(rec._OrderNumber));
                    if (order != null)
                    {
                        rec.SetMaster(order);
                        SetPriceLookup(rec);
                    }
                    break;
                case "Qty":
                    UpdatePrice(rec);
                    if (api.CompanyEntity._InvoiceUseQtyNowCre)
                        rec.QtyNow = rec._Qty;
                    break;
                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        SetLocation(selected, rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
                case "EAN":
                    UnicontaClient.Pages.DebtorOfferLines.FindOnEAN(rec, this.items, api);
                    break;
                case "Variant1":
                case "Variant2":
                case "Variant3":
                case "Variant4":
                case "Variant5":
                    UpdatePrice(rec);
                    break;
            }
        }

        async void SetLocation(InvWarehouse master, CreditorOrderLineClient rec)
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

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgMultiCreditorOrderLine.AddRow();
                    break;
                case "CopyRow":
                    dgMultiCreditorOrderLine.CopyRow();
                    break;
                case "SaveGrid":
                    SaveGrid();
                    break;
                case "DeleteRow":
                    dgMultiCreditorOrderLine.DeleteRow();
                    break;
                case "AddOrder":
                    AddDockItem(TabControls.CreditorOrdersPage2, api, Uniconta.ClientTools.Localization.lookup("Orders"), "Add_16x16.png");
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveGrid()
        {
            dgMultiCreditorOrderLine.SelectedItem = null;
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");
            ErrorCodes res = await dgMultiCreditorOrderLine.SaveData();
            busyIndicator.IsBusy = false;
            if (res == ErrorCodes.Succes)
            {
                dataChanged = false;
                dgMultiCreditorOrderLine.ItemsSource = null;
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SavedOBJ"), Uniconta.ClientTools.Localization.lookup("Data")), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
            }
            else
                UtilDisplay.ShowErrorCode(res);
        }
        private bool dataChanged;
        public override bool IsDataChaged
        {
            get
            {
                if (dataChanged)
                    return true;
                return base.IsDataChaged;
            }
        }
    }
}
