using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvBOMPartOfGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvBOMClient); } }
        public override IComparer GridSorting { get { return new InvBOMSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return true; } }
    }

    public partial class InvBOMPartOfPage : GridBasePage
    {
        SQLCache items;
        public override string NameOfControl { get { return TabControls.InvBOMPartOfPage; } }
        string MasterItem;

        public InvBOMPartOfPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitPage(syncEntity.Row);
            SetHeader(syncEntity.Row);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            InitialLoad(args);
            SetHeader(args);
            InitQuery();
        }

        private void SetHeader(UnicontaBaseEntity table)
        {
            if (table == null)
                return;
            string key;
            if (table is InvItem)
                key = (table as InvItem)?._Item;
            else
                key = Utility.GetHeaderString(table);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("BOM"), key);
            SetHeader(header);
        }

        public InvBOMPartOfPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master); 
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            ((TableView)dgInvBOMPartOfGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgInvBOMPartOfGrid;
            SetRibbonControl(localMenu, dgInvBOMPartOfGrid);
            dgInvBOMPartOfGrid.api = api;
            //dgInvBOMPartOfGrid.UpdateMaster(master); we will not set master, since we search without master, we just search with a where
            dgInvBOMPartOfGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            InitialLoad(master);
            dgInvBOMPartOfGrid.ShowTotalSummary();
        }

        private void InitialLoad(UnicontaBaseEntity master)
        {
            string item = null;
            InvItem invItem = master as InvItem;
            if (invItem != null)
                item = invItem._Item;
            else
            {
                var bom = master as InvBOM;
                if (bom != null)
                    item = bom._ItemPart;
            }

            var Comp = api.CompanyEntity;
            if (item != null)
            {
                MasterItem = item;
                propValuePair = new PropValuePair[]
                {
                    PropValuePair.GenereteWhereElements("ItemPart", typeof(string), item)
                };
            }

            if (invItem != null && invItem._ItemType == (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                this.UnfoldBOM.Visible = true;
        }

        PropValuePair[] propValuePair;
        public override Task InitQuery()
        {
            return dgInvBOMPartOfGrid.Filter(propValuePair);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "HierarichalInvBOMReport":
                    var selectItem = dgInvBOMPartOfGrid.SelectedItem;
                    if (selectItem != null)
                        AddDockItem(TabControls.InventoryHierarchicalBOMStatement, selectItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), MasterItem));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var inv = dgInvBOMPartOfGrid.masterRecord as Uniconta.DataModel.InvItem;
            bool showFields = (inv != null && inv._ItemType == (byte)Uniconta.DataModel.ItemType.BOM);
            MoveType.Visible = showFields;
            ShowOnInvoice.Visible = showFields;
            ShowOnPacknote.Visible = showFields;
            InclValueOnInvoice.Visible = showFields;

            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }

        protected override async void LoadCacheInBackGround()
        {
            if (this.items == null)
                this.items = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;
            return true;
        }
    }
}
