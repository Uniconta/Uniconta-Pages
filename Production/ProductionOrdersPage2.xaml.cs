using UnicontaClient.Models;
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
using Uniconta.API.Inventory;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ProductionOrdersPage2 : FormBasePage
    {
        ProductionOrderClient editrow;
        SQLCache warehouse;
        public override void OnClosePage(object[] RefreshParams)
        {
            object[] argsArray = new object[4];
            argsArray[0] = RefreshParams[0];
            argsArray[1] = RefreshParams[1];
            ((ProductionOrderClient)argsArray[1]).NotifyPropertyChanged("UserField");
            argsArray[2] = this.backTab;
            argsArray[3] = editrow;
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.ProductionOrdersPage2.ToString(); } }
        public override Type TableType { get { return typeof(ProductionOrderClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ProductionOrderClient)value; } }

        public ProductionOrdersPage2(CrudAPI crudApi, UnicontaBaseEntity sourcedata)
           : base(crudApi,string.Empty)
        {
            InitializeComponent();
            InitPage(api, sourcedata);
        }

        public ProductionOrdersPage2(CrudAPI crudApi, UnicontaBaseEntity master, UnicontaBaseEntity debtorOrder)
          : base(crudApi, string.Empty)
        {
            InitializeComponent();
            InitPage(api, master, debtorOrder);
        }

        public ProductionOrdersPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api, sourcedata);
        }
        public ProductionOrdersPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi, null);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(leProdItem, leProdItem);
#endif
        }
        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master, UnicontaBaseEntity debtorOrder = null)
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
             cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = leGroup.api = leProject.api= lePrCategory.api = leEmployee.api
            = leRelatedOrder.api = leProdItem.api = leGroup.api = leAccount.api = cmbWarehouse.api= cmbLocation.api= crudapi;
            if (editrow == null)
            {
                frmRibbon.DisableButtons( "Delete" );
                liCreatedTime.Visibility = Visibility.Collapsed;
                editrow = CreateNew() as ProductionOrderClient;
                editrow._Created = DateTime.MinValue;
                if (master != null)
                {
                    editrow.SetMaster(master);
                    editrow.SetMaster(debtorOrder);
                    editrow._EndDiscountPct = 0;
                    editrow._Storage = crudapi.CompanyEntity._PurchaseLineStorage;
                }
            }

            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            StartLoadCache();
        }

        protected override void OnLayoutCtrlLoaded()
        {
            AdjustLayout();
            var Comp = api.CompanyEntity;

            if (!Comp.Project)
            {
                liProject.Visibility = Visibility.Collapsed;
                liPrCategory.Visibility = Visibility.Collapsed;
            }
            if (!Comp.Location)
                itemLocation.Visibility = Visibility.Collapsed;

            if (!Comp.Warehouse)
                itemWarehouse.Visibility = Visibility.Collapsed;

            else if (editrow._Warehouse != null)
            {
                var wareHouse = Comp.GetCache(typeof(Uniconta.DataModel.InvWarehouse))?.Get(editrow._Warehouse) as InvWarehouseClient;
                setLocation(wareHouse);
            }
            if (!Comp.ItemVariants)
                liVariant.Visibility = Visibility.Collapsed;
        }

        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            parentGroup = lastGroup;
            return true;
        }
        
        public class BOMCacheFilter : SQLCacheFilter
        {
            public BOMCacheFilter(SQLCache cache) : base(cache) {  }
            public override bool IsValid(object rec) { return ((InvItem)rec)._ItemType == (byte)ItemType.ProductionBOM; }
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Cache = api.GetCache(typeof(InvItem)) ?? await api.LoadCache(typeof(InvItem)).ConfigureAwait(false);
            leProdItem.cacheFilter = new BOMCacheFilter(Cache);

            if (api.CompanyEntity.Warehouse)
                this.warehouse = api.GetCache(typeof(Uniconta.DataModel.InvWarehouse)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "SaveAndCreateLines":
                    SaveAndCreateLine();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async void SaveAndCreateLine()
        {
            closePageOnSave = false;
            var res = await saveForm(false);
            closePageOnSave = true;
            if (res)
                CreateOrderLines(editrow);
        }

        void CreateOrderLines(ProductionOrderClient productionOrder)
        {
            CWProductionOrderLine dialog = new CWProductionOrderLine(api);
            dialog.Closing += async delegate
            {
                if (dialog.DialogResult == true)
                {
                    var prodAPI = new ProductionAPI(api);
                    var result = await prodAPI.CreateProductionLines(productionOrder, (StorageRegister)dialog.storage);
                    if (result == ErrorCodes.Succes)
                    {
                        var olheader = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ProductionLines"), productionOrder._OrderNumber, productionOrder._DCAccount);
                        AddDockItem(TabControls.ProductionOrderLines, productionOrder, olheader);
                        dockCtrl?.JustClosePanel(this.ParentControl);
                    }
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            dialog.Show();
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);

            if (!Comp.SerialBatchNumbers)
                liBatchNumber.Visibility = Visibility.Collapsed;

            if (!Comp.Location || !Comp.Warehouse)
                itemLocation.Visibility = Visibility.Collapsed;

            if (!Comp.Warehouse)
                itemWarehouse.Visibility = Visibility.Collapsed;
        }

        private void leProdItem_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (editrow.RowId == 0)
            {
                var item = leProdItem.SelectedItem as InvItem;
                if (item != null)
                {
                    editrow.ProdQty = item._PurchaseQty;
                    editrow.Warehouse = item._Warehouse;
                    editrow.Location = item._Location;
                    editrow.NotifyPropertyChanged("ProdQty");
                    TableField.SetUserFieldsFromRecord(item, editrow);
                }
            }
        }

        async private void setLocation(InvWarehouseClient master)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                    editrow.locationSource = master.Locations ?? await master.LoadLocations(api);
                else
                    editrow.locationSource = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.InvLocation));

                cmbLocation.ItemsSource = editrow.LocationSource;
            }
        }

        private void cmbWarehouse_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (warehouse != null)
            {
                var selectedItem = cmbWarehouse.SelectedItem as InvWarehouseClient;
                setLocation(selectedItem);
            }
        }

        private void cmbLocation_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = cmbWarehouse.SelectedItem as InvWarehouseClient;
            setLocation(selectedItem);
        }

        private void leProject_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var project = leProject.SelectedItem as ProjectClient;
            if (project != null)
            {
                editrow.Account = project._DCAccount;
                editrow.NotifyPropertyChanged("Account");
                editrow.NotifyPropertyChanged("AccountName");
            }
        }
    }
}
