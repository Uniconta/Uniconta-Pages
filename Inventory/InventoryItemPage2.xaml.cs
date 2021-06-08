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
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InventoryItemPage2 : FormBasePage
    {
        public override Type TableType { get { return typeof(InvItemClient); } }

        InvItemClient editrow;
        SQLCache warehouse, itemCache, prCatCache;
        public override void OnClosePage(object[] RefreshParams)
        {
            ((InvItemClient)RefreshParams[1]).NotifyPropertyChanged("UserField");
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.InventoryItemPage2; } }
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
            itemCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem));
            prCatCache = Comp.GetCache(typeof(Uniconta.DataModel.PrCategory));

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

            if (!Comp.SetupSizes)
                grpSize.Visibility = Visibility.Collapsed;
            if (!Comp.Storage)
                grpQty.Visibility = Visibility.Collapsed;

            if (!Comp.InvDuty)
                liDutyGroup.Visibility = Visibility.Collapsed;

            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            leAlternativeItem.api = leGroup.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = cmbPrCategory.api =
                leBrandGrp.api = leCategoryGrp.api = cmbPayrollCategory.api = cmbPurchaseAccount.api = cmbWarehouse.api = cmbLocation.api =
                leDiscountGroup.api = leUnitGroup.api = leDutyGroup.api = crudapi;

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

        protected override void BeforeTemplateSet(UnicontaBaseEntity row)
        {
            ((InvItem)row)._Decimals = 2;
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
        bool isLayoutCtrlLoaded;
        protected override void OnLayoutCtrlLoaded()
        {
            if (editrow._Warehouse != null && api.CompanyEntity.Location && this.warehouse != null)
            {
                var wareHouse = this.warehouse.Get(editrow._Warehouse) as InvWarehouseClient;
                setLocation(wareHouse);
            }
            isLayoutCtrlLoaded = true;
        }
        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup)
        {
            parentGroup = invDtlLastGroup;
            return true;
        }
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            if (api.CompanyEntity.Warehouse && this.warehouse == null)
                this.warehouse = await api.LoadCache(typeof(Uniconta.DataModel.InvWarehouse)).ConfigureAwait(false);
            if (itemCache == null)
                itemCache = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            if (prCatCache == null)
                prCatCache = await api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).ConfigureAwait(false);
            var t = new List<Type>(2) { typeof(Uniconta.DataModel.InvGroup) };
            if (api.CompanyEntity.ItemVariants)
                t.Add(typeof(Uniconta.DataModel.InvStandardVariant));
            LoadType(t);

            if (api.CompanyEntity.Project)
                cmbPrCategory.cacheFilter = new PrCategoryCostFilter(prCatCache);
        }

        private void cmbWarehouse_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (warehouse != null)
                setLocation(cmbWarehouse.SelectedItem as InvWarehouseClient);
        }

        async private void setLocation(InvWarehouseClient master)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                    editrow.locationSource = master.Locations ?? await master.LoadLocations(api);
                else
                    editrow.locationSource = api.GetCache(typeof(Uniconta.DataModel.InvLocation));

                cmbLocation.ItemsSource = editrow.LocationSource;
            }
        }

        private void cmbLocation_GotFocus(object sender, RoutedEventArgs e)
        {
            setLocation(cmbWarehouse.SelectedItem as InvWarehouseClient);
        }

        bool lookupIsSet;
        private void liPhoto_LookupButtonClicked(object sender)
        {
            var lookupEditor = sender as LookupEditor;
            if (!lookupIsSet)
            {
                lookupEditor.PopupContentTemplate = (Application.Current).Resources["LookUpDocumentClientPopupContent"] as ControlTemplate;
                lookupEditor.ValueMember = "RowId";
                lookupEditor.SelectedIndexChanged += LookupEditor_SelectedIndexChanged;
                lookupIsSet = true;
                lookupEditor.ItemsSource = api.Query<UserDocsClient>(editrow).GetAwaiter().GetResult();
            }
        }

        async private void liPhoto_ButtonClicked(object sender)
        {
            if (editrow != null)
            {
                var userDocsClient = new UserDocsClient();
                userDocsClient.SetMaster(editrow);
                userDocsClient._RowId = editrow._Photo;
                await api.Read(userDocsClient);
                ViewDocument(TabControls.UserDocsPage3, userDocsClient);
            }
        }

        bool isUrlLookupSet;
        private void liURL_LookupButtonClicked(object sender)
        {
            var lookupUrlEditor = sender as LookupEditor;
            if(!isUrlLookupSet)
            {
                lookupUrlEditor.PopupContentTemplate = (Application.Current).Resources["LookUpUrlDocumentClientPopupContent"] as ControlTemplate;
                lookupUrlEditor.ValueMember = "RowId";
                lookupUrlEditor.SelectedIndexChanged += LookupUrlEditor_SelectedIndexChanged;
                isUrlLookupSet = true;
                var filter = PropValuePair.GenereteWhereElements("DocumentType", FileextensionsTypes.WWW, CompareOperator.Equal);
                lookupUrlEditor.ItemsSource = api.Query<UserDocsClient>(editrow, new PropValuePair[] { filter }).GetAwaiter().GetResult();
            }
        }

        private void LookupUrlEditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var lookUpEditor = sender as LookupEditor;
            var docsClient = lookUpEditor.SelectedItem as UserDocsClient;;
            editrow.URL = docsClient?.RowId ?? 0;
        }

        async private void liURL_ButtonClicked(object sender)
        {
            if (editrow != null)
            {
                var userDocsClient = new UserDocsClient();
                userDocsClient.SetMaster(editrow);
                userDocsClient._RowId = editrow._URL;
                await api.Read(userDocsClient);

#if !SILVERLIGHT
                if (session.User._UseDefaultBrowser)
                    Utility.OpenWebSite(userDocsClient.Url);
                else
#endif
                    ViewDocument(TabControls.UserDocsPage3, userDocsClient);
            }
        }

        private void LookupEditor_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var lookupEditor = sender as LookupEditor;
            var docsClient = lookupEditor.SelectedItem as UserDocsClient;
            editrow.Photo = docsClient?._RowId ?? 0;
        }

        private void txtItem_LostFocus(object sender, RoutedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && isLayoutCtrlLoaded && s.IsLoaded && itemCache != null)
            {
                var item = itemCache.Get(s.Text);
                if (item != null && item.RowId != editrow.RowId)
                    UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Item"), string.Format(Uniconta.ClientTools.Localization.lookup("AlreadyExistOBJ"), s.Text)), Uniconta.ClientTools.Localization.lookup("Warning"));
            }
        }
    }

    public class PrCategoryCostFilter : SQLCacheFilter
    {
        public PrCategoryCostFilter(SQLCache cache) : base(cache) { }
        public override bool IsValid(object rec)
        {
            var cattype = ((Uniconta.DataModel.PrCategory)rec)._CatType;
            return cattype != CategoryType.Revenue && cattype != CategoryType.Sum;
        }
    }
}
