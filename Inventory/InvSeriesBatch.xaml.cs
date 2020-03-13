using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using Uniconta.API.Inventory;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using UnicontaClient.Utilities;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvSeriesBatchGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvSerieBatchClient); } }

        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        protected override List<string> GridSkipFields { get { return new List<string>() { "ItemName" }; } }

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (InvSerieBatchClient)this.SelectedItem;
            return (selectedItem?._Number != null);
        }
    }

    public partial class InvSeriesBatch : GridBasePage
    {
        SQLCache warehouseCache;
        public InvSeriesBatch(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            Init(null);
        }
        UnicontaBaseEntity pageMaster;
        public InvSeriesBatch(UnicontaBaseEntity master) : base(master)
        {
            this.pageMaster = master;
            InitializeComponent();
            Init(master);
        }

        public InvSeriesBatch(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            this.pageMaster = syncEntity.Row;
            InitializeComponent();
            Init(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            if (openMaster == null)
                SetMaster(args, null);
            else
                SetMaster(args, openMaster);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            var table = dgInvSeriesBatchGrid.masterRecord;
            string key = string.Empty;
            if (table is InvTransClient)
            {
                var invTrans = table as InvTransClient;
                key = invTrans._Item;
            }
            else if(table is InvTransProject)
            {
                var invTrans = table as InvTransProject;
                key = invTrans._Item;
            }
            else
                key = Utility.GetHeaderString(dgInvSeriesBatchGrid.masterRecord);

            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers"), key);
            SetHeader(header);
        }

        private void Init(UnicontaBaseEntity master)
        {
            dgInvSeriesBatchGrid.api = api;
            SetMaster(master, null);
            localMenu.dataGrid = dgInvSeriesBatchGrid;
            SetRibbonControl(localMenu, dgInvSeriesBatchGrid);
            dgInvSeriesBatchGrid.BusyIndicator = busyIndicator;
            dgInvSeriesBatchGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged; ;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            if (!Comp.Location || !Comp.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!Comp.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
        }

        async protected override void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (Comp.Warehouse && this.warehouseCache == null)
                this.warehouseCache = Comp.GetCache(typeof(InvWarehouse)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvWarehouse), api).ConfigureAwait(false);
        }

        private void DataControl_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            var oldSelectedItem = e.OldItem as InvSerieBatchClient;
            if (oldSelectedItem != null)
                oldSelectedItem.PropertyChanged -= InvSerieBatch_Changed;

            var selectedItem = e.NewItem as InvSerieBatchClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvSerieBatch_Changed;
        }

        private void InvSerieBatch_Changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (InvSerieBatchClient)sender;

            switch (e.PropertyName)
            {
                case "Warehouse":
                    if (warehouseCache != null && rec._Warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouseCache.Get(rec._Warehouse);
                        SetLocation(selected, rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
            }
        }

        void SetMaster(UnicontaBaseEntity master, InvSerieBatchOpen openMaster)
        {
            List<UnicontaBaseEntity> masterList = null;
            pageMaster = master;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (pageMaster != null || openMaster != null)
            {
                masterList = new List<UnicontaBaseEntity>();
                if (pageMaster != null)
                {
                    if (pageMaster is InvTrans)
                    {
                        dgInvSeriesBatchGrid.Readonly = true;
                        UtilDisplay.RemoveMenuCommand(rb, new string[] { "AddRow", "CopyRow", "DeleteRow", "SaveGrid", "Transactions" });
                    }
                    else
                        UtilDisplay.RemoveMenuCommand(rb, "RemoveFromTransaction");

                    masterList.Add(pageMaster);
                    Item.Visible = false;
                    ItemName.Visible = false;
                }
                if (openMaster != null)
                    masterList.Add(openMaster);
                dgInvSeriesBatchGrid.masterRecords = masterList;
            }
            else
            {
                UtilDisplay.RemoveMenuCommand(rb, "RemoveFromTransaction");
                dgInvSeriesBatchGrid.masterRecords = null;
            }

        }

        async void SetLocation(InvWarehouse master, InvSerieBatchClient rec)
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

        InvSerieBatchOpen openMaster;
        ItemBase ibase;
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            InvSerieBatchClient selectedItem = dgInvSeriesBatchGrid.SelectedItem as InvSerieBatchClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgInvSeriesBatchGrid.AddRow();
                    break;
                case "CopyRow":
                    dgInvSeriesBatchGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgInvSeriesBatchGrid.DeleteRow();
                    break;
                case "OpenOrAll":
                    RibbonBase rb = (RibbonBase)localMenu.DataContext;
                    ibase = UtilDisplay.GetMenuCommandByName(rb, "OpenOrAll");
                    if (openMaster == null)
                    {
                        openMaster = new InvSerieBatchOpen();
                        SetMaster(pageMaster, openMaster);
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("All");
                    }
                    else /*For All*/
                    {
                        SetMaster(pageMaster, null);
                        openMaster = null;
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("Open");
                    }
                    dgInvSeriesBatchGrid.Filter(null);
                    break;
                case "RemoveFromTransaction":
                    if (selectedItem != null)
                        RemoveFromTransaction(selectedItem);
                    break;

                case "Transactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryTransactions, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._Item));
                    break;
                case "Storage":
                    AddDockItem(TabControls.InvItemStoragePage, dgInvSeriesBatchGrid.syncEntity, Uniconta.ClientTools.Localization.lookup("OnHand"));
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgInvSeriesBatchGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgInvSeriesBatchGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Number));
                    break;
                case "BatchLocations":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvSerieBatchStorage, dgInvSeriesBatchGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BatchLocations"), selectedItem._Number));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void RemoveFromTransaction(InvSerieBatch selectedItem)
        {
            var tr = pageMaster as InvTrans;
            if (tr == null)
                return;
            TransactionsAPI objTransactionsAPI = new TransactionsAPI(api);
            var res = await objTransactionsAPI.RemoveSerieBatch(tr, selectedItem);
            if (res != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(res);
            else
                InitQuery();
        }

        CorasauGridLookupEditorClient prevLocation;
        private void Location_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvSeriesBatchGrid.SelectedItem as InvSerieBatchClient;
            if (selectedItem?._Warehouse != null && warehouseCache != null)
            {
                var selected = (InvWarehouse)warehouseCache.Get(selectedItem._Warehouse);
                SetLocation(selected, selectedItem);
                if (prevLocation != null)
                    prevLocation.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevLocation = editor;
                editor.isValidate = true;
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }
    }

    public class ItemDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextTemplate { get; set; }
        public DataTemplate LookupTemplate { get; set; }


        /// <summary>
        /// Method Returns the Template
        /// </summary>
        /// <param name="item"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var data = item as EditGridCellData;
            var row = data.RowData.Row as InvSerieBatchClient;

            if (row?.IsEnabled == true)
                return LookupTemplate;
            else
                return TextTemplate;
        }
    }
}
