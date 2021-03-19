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
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvBOMPartOfGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvBOMClient); } }
        public override IComparer GridSorting { get { return new InvBOMSort(); } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return true; } }
    }

    public partial class InvBOMPartOfPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvBOMPartOfPage; } }
        string MasterItem;

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            setMaster(args);
            SetHeader(args);
            InitQuery();
        }

        private void SetHeader(UnicontaBaseEntity table)
        {
            if (table == null)
                return;
            string key;
            var itm = (table as InvItem);
            if (itm != null)
                key = itm._Item;
            else if (table is IVariant)
            {
                var variantMaster = table as IVariant;
                key = variantMaster.Item + "/" + variantMaster.Variant;
            }
            else
            {
                var bom = (table as InvBOM);
                if (bom != null)
                    key = bom._ItemPart;
                else
                    key = Utility.GetHeaderString(table);
            }
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("BOM"), ": ", key);
            SetHeader(header);
        }

        public InvBOMPartOfPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitPage();
            setMaster(syncEntity.Row);
            SetHeader(syncEntity.Row);
        }

        public InvBOMPartOfPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public InvBOMPartOfPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage();
            setMaster(master);
        }

        void InitPage()
        {
            InitializeComponent();
            ((TableView)dgInvBOMPartOfGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgInvBOMPartOfGrid;
            SetRibbonControl(localMenu, dgInvBOMPartOfGrid);
            dgInvBOMPartOfGrid.api = api;
            //dgInvBOMPartOfGrid.UpdateMaster(master); we will not set master, since we search without master, we just search with a where
            dgInvBOMPartOfGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvBOMPartOfGrid.ShowTotalSummary();
        }

        private void setMaster(UnicontaBaseEntity master)
        {
            string item = null;
            InvItem invItem = master as InvItem;
            if (invItem != null)
                item = invItem._Item;
            else if (master is IVariant)
            {
                var variantMaster = master as IVariant;
                var items = api.GetCache(typeof(Uniconta.DataModel.InvItem));
                if (variantMaster != null)
                    invItem = items?.Get(variantMaster.Item) as InvItemClient;

                item = invItem?._Item;
            }
            else
            {
                var bom = master as InvBOM;
                if (bom != null)
                    item = bom._ItemPart;
            }
            if (invItem != null && invItem._ItemType == (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                this.UnfoldBOM.Visible = true;

            setItem(item);
        }

        void setItem(string item)
        {
            if (item != null)
            {
                MasterItem = item;
                propValuePair = new PropValuePair[] { PropValuePair.GenereteWhereElements("ItemPart", typeof(string), item) };
            }
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Item", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    setItem(rec.Value);
                    string header = string.Concat(Uniconta.ClientTools.Localization.lookup("BOM"), ": ", rec.Value);
                    SetHeader(header);
                }
            }
            base.SetParameter(Parameters);
        }

        PropValuePair[] propValuePair;
        public override Task InitQuery()
        {
            return dgInvBOMPartOfGrid.Filter(propValuePair);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectItem = dgInvBOMPartOfGrid.SelectedItem as InvBOMClient;
            switch (ActionType)
            {
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "HierarichalInvBOMReport":
                    if (selectItem != null)
                        AddDockItem(TabControls.InventoryHierarchicalBOMStatement, selectItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("HierarchicalBOM"), MasterItem));
                    break;
                case "InvBOMPartOfContains":
                    if (selectItem != null && AppEnums.ItemType.IndexOf(selectItem.ItemType) >= (byte)Uniconta.DataModel.ItemType.BOM)
                        AddDockItem(TabControls.PartInvItemsPage, selectItem, string.Format("{0}:{1}/{2}", Uniconta.ClientTools.Localization.lookup("BOM"), selectItem.ItemPart, selectItem.Name));
                    break;
                case "InvBOMPartOfWhereUsed":
                    if (selectItem != null)
                    {
                        if (selectItem != null)
                            AddDockItem(TabControls.InvBOMPartOfPage, selectItem, string.Format("{0}:{1}/{2}", Uniconta.ClientTools.Localization.lookup("BOM"), selectItem.ItemPart, selectItem.Name));
                    }
                    break;
                case "InvBOMProductionPosted":
                    if (selectItem != null)
                        AddDockItem(TabControls.ProductionPostedGridPage, selectItem, string.Format("{0}:{1}/{2}", Uniconta.ClientTools.Localization.lookup("ProductionPosted"), selectItem.ItemPart, selectItem.Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;
            return true;
        }
    }
}
