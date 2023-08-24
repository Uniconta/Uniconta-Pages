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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using DevExpress.Xpf.Core;
using System.Collections;
using DevExpress.Xpf.Grid;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InventoryHierarchicalBOMGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvBOMClient); } }
        public override IComparer GridSorting { get { return new InvBOMSort(); } }
        public override bool AllowSort { get { return false; } }
        public override bool ShowTreeListView => true;

        public override TreeListView SetTreeListViewFromPage
        {
            get
            {
                var tv = base.SetTreeListViewFromPage;
                tv.AutoWidth = true;
                tv.KeyFieldName = "ID";
                tv.ShowIndicator = false;
                tv.ParentFieldName = "ParentID";
                tv.TreeDerivationMode = TreeDerivationMode.Selfreference;
                tv.ShowTotalSummary = false;
                return tv;
            }
        }
    }

    /// <summary>
    /// Interaction logic for InventoryInvBOMClient.xaml
    /// </summary>
    public partial class InventoryHierarchicalBOMStatement : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryHierarchicalBOMStatement; } }

        InvBOMClient invBom;
        InvItem invClient;
        double Quantity;
        CWServerFilter filterDialog;
        SortingProperties[] defaultSort;
        Filter[] defaultFilters;
        bool defaultFilterCleared;
        IEnumerable<PropValuePair> filters;
        FilterSorter sort;
        ItemBase ibase;
        LoadInvBOMDeep LoadBOM;

        // we support two different masters. InvItem or InvBOM
        public InventoryHierarchicalBOMStatement(UnicontaBaseEntity baseEntity) : base(null)
        {
            InitializeComponent();
            BusyIndicator = busyIndicator;
            Quantity = 1d;
            invClient = baseEntity as InvItem;
            if (invClient == null)
            {
                IVariant syncMaster2 = baseEntity as IVariant;
                if (syncMaster2 is IVariant)
                    invClient = api.GetCache(typeof(Uniconta.DataModel.InvItem))?.Get(syncMaster2.Item) as InvItemClient;
                else
                {
                    invBom = baseEntity as InvBOMClient;
                    if (invBom != null)
                        Quantity = invBom._Qty;
                }
            }
            localMenu.dataGrid = dgInvBomclientGrid;
            SetRibbonControl(localMenu, dgInvBomclientGrid);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            LoadBOM = new LoadInvBOMDeep(api);
            SetDefaultFilterValues();
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            GetMenuItem();
            HideMenuItems();
        }

        private void HideMenuItems()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
            {
                UtilDisplay.RemoveMenuCommand(rb, "SaveGrid");
                UtilDisplay.RemoveMenuCommand(rb, "Layout");
                UtilDisplay.RemoveMenuCommand(rb, "SetQuantity");
                UtilDisplay.RemoveMenuCommand(rb, "Storage");
            }
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Item", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    var cache = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? api.LoadCache(typeof(Uniconta.DataModel.InvItem)).GetAwaiter().GetResult();
                    invClient = (Uniconta.DataModel.InvItem)cache.Get(rec.Value);
                }
                else if (string.Compare(rec.Name, "Quantity", StringComparison.CurrentCultureIgnoreCase) == 0)
                    Quantity = Uniconta.Common.Utility.NumberConvert.ToDoubleNoThousandSeperator(rec.Value);
            }
            base.SetParameter(Parameters);
        }

   

        private void SetDefaultFilterValues()
        {
            defaultSort = null;
            defaultFilters = null;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "RefreshGrid":
                    InitQuery();
                    break;               
                case "ExpandAndCollapse":
                    if (dgInvBomclientGrid.ItemsSource != null)
                        SetExpandCollapse();
                    break;
                case "ExpandRow":
                    if (dgInvBomclientGrid.SelectedItem != null)
                        SetExpandRows();
                    break;
                case "AddNote":
                    if (dgInvBomclientGrid.SelectedItem != null)
                    {
                        var selectedItem = dgInvBomclientGrid.SelectedItem as InvBOMClient;
                        AddDockItem(TabControls.UserNotesPage, dgInvBomclientGrid.SelectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Note"), selectedItem._ItemPart));
                    }
                    break;
                case "AddDoc":
                    if (dgInvBomclientGrid.SelectedItem != null)
                    {
                        var selectedItem = dgInvBomclientGrid.SelectedItem as InvBOMClient;
                        AddDockItem(TabControls.UserDocsPage, dgInvBomclientGrid.SelectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Document"), selectedItem._ItemPart));
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void SetExpandRows()
        {
            var rowHandles = dgInvBomclientGrid.GetSelectedRowHandles();
            dgInvBomclientGrid.treeListView.ExpandNode(rowHandles[0]);
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ExpandAndCollapse");
        }

        private void SetExpandCollapse()
        {
            if (ibase == null)
                return;

            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !dgInvBomclientGrid.treeListView.AutoExpandAllNodes)
            {
                dgInvBomclientGrid.treeListView.ExpandAllNodes();
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("CollapseAll");
                ibase.LargeGlyph = Utility.GetGlyph("Collapse_32x32");
            }
            else
            {
                dgInvBomclientGrid.treeListView.CollapseAllNodes();
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                ibase.LargeGlyph = Utility.GetGlyph("Expand_32x32");
            }
        }

        public override async Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            LoadBOM.Sorter = sort;
            LoadBOM.Filters = filters;
            Task<List<InvBOMClient>> tsk;
            if (invClient != null)
                tsk = LoadBOM.Load(invClient, Quantity);
            else
                tsk = LoadBOM.Load(invBom, Quantity);
            if (tsk != null)
                dgInvBomclientGrid.ItemsSource = await tsk;
            dgInvBomclientGrid.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }
       
        

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Image).Tag is InvBOMClient invBomItem)
                AddDockItem(TabControls.UserDocsPage, invBomItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Note"), invBomItem._ItemPart));
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((sender as Image).Tag is InvBOMClient invBomItem)
                AddDockItem(TabControls.UserNotesPage, invBomItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Document"), invBomItem._ItemPart));
        }
    }
}
