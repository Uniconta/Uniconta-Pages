using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
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
using Uniconta.ClientTools.Util;
using Uniconta.DataModel;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorPurchageLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorOrderLineClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class PurchaseLinesPage : GridBasePage
    {
        SQLCache items, warehouse;
        public PurchaseLinesPage(UnicontaBaseEntity masterRecord)
            : base(masterRecord)
        {
            InitializeComponent();
            dgCreditorPurchagelineGrid.UpdateMaster(masterRecord);
            InitPage();
        }

        public PurchaseLinesPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        private void InitPage()
        {
            dgCreditorPurchagelineGrid.tableView.ShowGroupPanel = true;
            SetRibbonControl(localMenu, dgCreditorPurchagelineGrid);
            dgCreditorPurchagelineGrid.api = api;
            dgCreditorPurchagelineGrid.BusyIndicator = busyIndicator;
            dgCreditorPurchagelineGrid.api = api;
            dgCreditorPurchagelineGrid.ShowTotalSummary();
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCreditorPurchagelineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            InitialLoad();
        }

        private void InitialLoad()
        {
            var Comp = api.CompanyEntity;
            this.items = Comp.GetCache(typeof(InvItem));
            this.warehouse = Comp.GetCache(typeof(InvWarehouse));
            if (Comp.UnitConversion)
                Unit.Visible = true;
        }
        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            CreditorOrderLineClient oldselectedItem = e.OldItem as CreditorOrderLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= CreditorOrderLineGrid_PropertyChanged;

            CreditorOrderLineClient selectedItem = e.NewItem as CreditorOrderLineClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += CreditorOrderLineGrid_PropertyChanged;
        }

        private void CreditorOrderLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (CreditorOrderLineClient)sender;
            switch (e.PropertyName)
            {
                case "Item":
                    var selectedItem = (InvItem)items.Get(rec._Item);
                    if (selectedItem != null)
                    {
                        rec.Text = selectedItem._Name;
                        rec.NotifyPropertyChanged("Text");
                        TableField.SetUserFieldsFromRecord(selectedItem, rec);
                        if (selectedItem._Blocked)
                            UtilDisplay.ShowErrorCode(ErrorCodes.ItemIsOnHold, null);
                    }
                    break;
                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, (CreditorOrderLineClient)rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
            }
        }
        async void setLocation(InvWarehouse master, CreditorOrderLineClient rec)
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

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;

            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            setDim();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorPurchagelineGrid.SelectedItem as CreditorOrderLineClient;
            switch (ActionType)
            {
                case "SaveGrid":
                    dgCreditorPurchagelineGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgCreditorPurchagelineGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            CreditorOrderLineClient selectedItem = dgCreditorPurchagelineGrid.SelectedItem as CreditorOrderLineClient;
            if (selectedItem != null && selectedItem._Warehouse != null && warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
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

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (this.items == null)
                this.items = await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);

            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = await Comp.LoadCache(typeof(Uniconta.DataModel.InvWarehouse), api).ConfigureAwait(false);

            await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api).ConfigureAwait(false);
        }
    }

   
}
