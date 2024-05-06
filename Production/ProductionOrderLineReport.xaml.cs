using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProductionOrderLineReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProductionOrderLineClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }

    public partial class ProductionOrderLineReport : GridBasePage
    {
        SQLCache items, warehouse;
        public override string NameOfControl
        {
            get { return TabControls.ProductionOrderLineReport.ToString(); }
        }

        public ProductionOrderLineReport(UnicontaBaseEntity masterRecord)
            : base(masterRecord)
        {
            InitializeComponent();
            dgProductionOrderLineGrid.UpdateMaster(masterRecord);
            InitPage();
        }

        public ProductionOrderLineReport(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        private void InitPage()
        {
            dgProductionOrderLineGrid.tableView.ShowGroupPanel = true;
            SetRibbonControl(localMenu, dgProductionOrderLineGrid);
            dgProductionOrderLineGrid.api = api;
            dgProductionOrderLineGrid.ShowTotalSummary();
            dgProductionOrderLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProductionOrderLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            ribbonControl.DisableButtons(new string[] { "DeleteRow", "SaveGrid" });
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
#if !SILVERLIGHT
            Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
#else
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
#endif
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProductionOrderLineGrid.SelectedItem as ProductionOrderLineClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgProductionOrderLineGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "SaveGrid":
                    dgProductionOrderLineGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgProductionOrderLineGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgProductionOrderLineGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgProductionOrderLineGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgProductionOrderLineGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "DeleteRow", "SaveGrid" });
                editAllChecked = false;
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
                                var err = await dgProductionOrderLineGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgProductionOrderLineGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgProductionOrderLineGrid.Readonly = true;
                        dgProductionOrderLineGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgProductionOrderLineGrid.Readonly = true;
                    dgProductionOrderLineGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "DeleteRow", "SaveGrid" });
                }
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgProductionOrderLineGrid.HasUnsavedData;
            }
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            ProductionOrderLineClient oldselectedItem = e.OldItem as ProductionOrderLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= ProductionOrderLineGrid_PropertyChanged;

            ProductionOrderLineClient selectedItem = e.NewItem as ProductionOrderLineClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += ProductionOrderLineGrid_PropertyChanged;
        }

        private void ProductionOrderLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (ProductionOrderLineClient)sender;
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

                        globalEvents?.NotifyRefreshViewer(NameOfControl, selectedItem);
                    }
                    break;
                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, (ProductionOrderLineClient)rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
                case "SerieBatch":
                    var selectedSerieBatch = rec.SerieBatches?.Where(x => x.Number == rec.SerieBatch).FirstOrDefault();
                    if (selectedSerieBatch != null && api.CompanyEntity.Warehouse)
                    {
                        rec.Warehouse = selectedSerieBatch.Warehouse;
                        if (api.CompanyEntity.Location)
                            rec.Location = selectedSerieBatch.Location;
                    }
                    break;
            }
        }

        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            ProductionOrderLineClient selectedItem = dgProductionOrderLineGrid.SelectedItem as ProductionOrderLineClient;
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

        async void setLocation(InvWarehouse master, ProductionOrderLineClient rec)
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

        private void SerieBatch_GotFocus(object sender, RoutedEventArgs e)
        {
            var selItem = dgProductionOrderLineGrid.SelectedItem as ProductionOrderLineClient;
            if (selItem == null || string.IsNullOrEmpty(selItem._Item))
                return;
            setSerieBatchSource(selItem);
        }

        async void setSerieBatchSource(ProductionOrderLineClient row)
        {
            var cache = api.CompanyEntity.GetCache(typeof(InvItem));
            var invItemMaster = cache.Get(row._Item) as InvItem;
            if (invItemMaster == null)
                return;
            if (row.SerieBatches != null && row.SerieBatches.First()._Item == row._Item)/*Bind if Item changed*/
                return;
            List<UnicontaBaseEntity> masters = null;
            if (row._Qty < 0)
            {
                masters = new List<UnicontaBaseEntity>() { invItemMaster };
            }
            else
            {
                // We only select opens
                var mast = new InvSerieBatchOpen();
                mast.SetMaster(invItemMaster);
                masters = new List<UnicontaBaseEntity>() { mast };
            }
            var res = await api.Query<SerialToOrderLineClient>(masters, null);
            if (res != null && res.Length > 0)
            {
                row.SerieBatches = res;
                row.NotifyPropertyChanged("SerieBatches");
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (this.items == null)
                this.items = await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);

            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = await Comp.LoadCache(typeof(Uniconta.DataModel.InvWarehouse), api).ConfigureAwait(false);

            await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);
        }
    }
}
