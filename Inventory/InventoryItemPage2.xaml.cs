using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.Common.Utility;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InventoryItemPage2 : FormBasePage
    {
        public override Type TableType { get { return typeof(InvItemClient); } }

        InvItemClient editrow;
        SQLCache warehouse, itemCache;
        public override void OnClosePage(object[] RefreshParams)
        {
            ((InvItemClient)RefreshParams[1]).NotifyPropertyChanged("UserField");
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.InventoryItemPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (InvItemClient)value; } }
        bool isCopiedRow = false;

        public InventoryItemPage2(UnicontaBaseEntity sourcedata, bool IsEdit)
           : base(sourcedata, IsEdit)
        {
            InitializeComponent();
            isCopiedRow = !IsEdit;
            InitPage(api);
        }

        public InventoryItemPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtItem, txtItem);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            var Comp = crudapi.CompanyEntity;
            if (!Comp.Storage || Comp.StorageOnAll)
                itemUsestorage.Visibility = Visibility.Collapsed;
            if (!Comp.SerialBatchNumbers)
            {
                itemUseSerialBatch.Visibility = Visibility.Collapsed;
                itemMandatorySerialBatch.Visibility = Visibility.Collapsed;
                itemMandatorySerialBatchMarkg.Visibility = Visibility.Collapsed;
            }
            if (!Comp.InvBOM)
            {
                itemBOMCostOfLines.Visibility = Visibility.Collapsed;
                liItemIncludedInBOM.Visibility = Visibility.Collapsed;
            }

            if (!Comp.UnitConversion)
            {
                liPurchaseUnit.Visibility = Visibility.Collapsed;
                liUSalesUnit.Visibility = Visibility.Collapsed;
                liUnitGroup.Visibility = Visibility.Collapsed;
            }
            if (!Comp.InvPrice && !Comp.CreditorPrice)
                liDiscountGroup.Visibility = Visibility.Collapsed;

            if (Comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);

            if (!Comp.ItemVariants)
                useVariants.Visibility = Visibility.Collapsed;
            else
                cmbStandardVariant.api = crudapi;

            if (!Comp.Project)
                projectLayGrp.Visibility = Visibility.Collapsed;

            if (!Comp.Location || !Comp.Warehouse)
                itemLocation.Visibility = Visibility.Collapsed;

            if (!Comp.Warehouse)
                itemWarehouse.Visibility = Visibility.Collapsed;
            else
                this.warehouse = Comp.GetCache(typeof(Uniconta.DataModel.InvWarehouse));

            if (!Comp.Storage)
                grpQty.Visibility = Visibility.Collapsed;

            if (!Comp.InvDuty)
                liDutyGroup.Visibility = Visibility.Collapsed;

            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            leAlternativeItem.api = leGroup.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = cmbPrCategory.api =
                leBrandGrp.api = leCategoryGrp.api = cmbPayrollCategory.api = cmbPurchaseAccount.api = cmbWarehouse.api = cmbLocation.api = leDiscountGroup.api = leUnitGroup.api = leDutyGroup.api = crudapi;

            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                if (!isCopiedRow)
                    editrow = CreateNew() as InvItemClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            StartLoadCache();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save" && !VaidateEAN(editrow._EAN))
                return;
            frmRibbon_BaseActions(ActionType);
        }

        bool VaidateEAN(string ean)
        {
            if (Utility.IsValidEAN(ean, api.CompanyEntity))
                return true;
            else
                UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), ean), Uniconta.ClientTools.Localization.lookup("Warning"));
            return false;
        }

        protected override void OnLayoutCtrlLoaded()
        {
            if (editrow._Warehouse != null && api.CompanyEntity.Location && this.warehouse != null)
            {
                var wareHouse = this.warehouse.Get(editrow._Warehouse) as InvWarehouseClient;
                setLocation(wareHouse);
            }
        }
        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            parentGroup = invDtlLastGroup;
            return true;
        }
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (Comp.Warehouse && this.warehouse == null)
                this.warehouse = await Comp.LoadCache(typeof(Uniconta.DataModel.InvWarehouse), api).ConfigureAwait(false);
            itemCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);

            var t = new List<Type> { typeof(Uniconta.DataModel.InvGroup) };
            if (Comp.ItemVariants)
                t.Add(typeof(Uniconta.DataModel.InvStandardVariant));
            LoadType(t.ToArray());
        }

        private void cmbWarehouse_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (warehouse != null)
            {
                var selectedItem = cmbWarehouse.SelectedItem as InvWarehouseClient;
                setLocation(selectedItem);
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

        private void cmbLocation_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = cmbWarehouse.SelectedItem as InvWarehouseClient;
            setLocation(selectedItem);
        }

        private void txtItem_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded && itemCache != null)
            {
                var item = itemCache.Get(s.Text);
                if (item != null && item.RowId != editrow.RowId)
                    UnicontaMessageBox.Show(string.Format("{0} {1} ", Uniconta.ClientTools.Localization.lookup("Item"), string.Format(Uniconta.ClientTools.Localization.lookup("AlreadyExistOBJ"), s.Text)), Uniconta.ClientTools.Localization.lookup("Warning"));
            }
        }
    }
}
