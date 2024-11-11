using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.ComponentModel;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvVariantDetailGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvVariantDetailClient); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort
        {
            get
            {
                return false;
            }
        }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvVariantDetailPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvVariantDetailPage; } }
        InvItemClient invMaster;
        public InvVariantDetailPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            invMaster = (InvItemClient)syncEntity.Row;
            InitializeComponent();
            Init();
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            invMaster = (InvItemClient)args;
            dgInvVariantDetailGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            var table = dgInvVariantDetailGrid.masterRecord;
            string key = string.Empty;
            if (table is InvItemClient)
            {
                var invItem = table as InvItemClient;
                key = invItem._Item;
            }
            else
                key = Utility.GetHeaderString(dgInvVariantDetailGrid.masterRecord);

            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("ItemVariants"), key);
            SetHeader(header);
        }

        public InvVariantDetailPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            invMaster = (InvItemClient)master;
            Init();
            Item.Visible = false;
        }

        public InvVariantDetailPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            Init();
        }

        void Init()
        {
            localMenu.dataGrid = dgInvVariantDetailGrid;
            SetRibbonControl(localMenu, dgInvVariantDetailGrid);
            dgInvVariantDetailGrid.api = api;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            if (invMaster != null)
                dgInvVariantDetailGrid.UpdateMaster(invMaster);
            dgInvVariantDetailGrid.BusyIndicator = busyIndicator;
            dgInvVariantDetailGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            HideMenuCommand(invMaster);
        }

        private void HideMenuCommand(InvItemClient invItem)
        {
            if (invItem == null || invItem._ItemType < (byte)Uniconta.DataModel.ItemType.BOM)
                UtilDisplay.RemoveMenuCommand((RibbonBase)localMenu.DataContext, new string[] { "PartInvItems" });
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvVariantDetailClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvVariantDetailClientGrid_PropertyChanged;
            var selectedItem = e.NewItem as InvVariantDetailClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvVariantDetailClientGrid_PropertyChanged;
        }

        private void InvVariantDetailClientGrid_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var rec = (InvVariantDetailClient)sender;
            switch (e.PropertyName)
            {
                case "EAN":
                    if (!Utility.IsValidEAN(rec.EAN, api.CompanyEntity))
                    {
                        UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), rec.EAN), Uniconta.ClientTools.Localization.lookup("Error"));
                        rec.EAN = null;
                    }
                    break;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvVariantDetailGrid.SelectedItem as InvVariantDetailClient;
            switch (ActionType)
            {
                case "AddRow":
                    var row = dgInvVariantDetailGrid.AddRow();
                    if (invMaster != null)
                    {
                        var currentRow = row as InvVariantDetailClient;
                        if (currentRow != null)
                            currentRow.Item = invMaster.Item;
                    }
                    break;
                case "CopyRow":
                    dgInvVariantDetailGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgInvVariantDetailGrid.DeleteRow();
                    break;
                case "AddDoc":

                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgInvVariantDetailGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.VariantName));
                    break;
                case "Photo":
                    if (selectedItem == null)
                        return;
                    var cw = new CwSelectPhotoId(api, selectedItem);
                    cw.Closing += delegate
                      {
                          if (cw.DialogResult == true)
                          {
                              dgInvVariantDetailGrid.SetLoadedRow(selectedItem);
                              selectedItem.Photo = cw.Photo;
                              dgInvVariantDetailGrid.UpdateItemSource(selectedItem);
                              dgInvVariantDetailGrid.SetModifiedRow(selectedItem);
                          }
                      };
                    cw.Show();
                    break;
                case "InvBOMPartOfContains":
                    if (selectedItem != null)
                    {
                        if (dgInvVariantDetailGrid.HasUnsavedData)
                            saveGrid();
                        AddDockItem(TabControls.PartInvItemsPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BOM"), selectedItem._Item));
                    }
                    break;
                case "InvBOMPartOfWhereUsed":
                    if (selectedItem != null)
                    {
                        if (dgInvVariantDetailGrid.HasUnsavedData)
                            saveGrid();
                        AddDockItem(TabControls.InvBOMPartOfPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BOM"), selectedItem._Item));
                    }
                    break;
                case "InvBOMProductionPosted":
                    if (selectedItem != null)
                    {
                        if (dgInvVariantDetailGrid.HasUnsavedData)
                            saveGrid();
                        AddDockItem(TabControls.ProductionPostedGridPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionPosted"), selectedItem._Item));
                    }
                    break;
                case "InvHierarichalBOM":
                    if (selectedItem != null)
                    {
                        if (dgInvVariantDetailGrid.HasUnsavedData)
                            saveGrid();
                        AddDockItem(TabControls.InventoryHierarchicalBOMStatement, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), selectedItem._Item));
                    }
                    break;
                case "InvExplodeBOM":
                    if (selectedItem != null)
                    {
                        if (dgInvVariantDetailGrid.HasUnsavedData)
                            saveGrid();
                        AddDockItem(TabControls.InvBOMExplodePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ExplodedBOM"), selectedItem._Item));
                    }
                    break;
                case "Packaging":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvPackagingProductPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Packaging"), selectedItem._Item));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            ErrorCodes res = await base.saveGrid();
            if (res == ErrorCodes.Succes)
                globalEvents.OnRefresh(NameOfControl, invMaster);
            return res;
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

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.InvItem));
        }
    }
}
