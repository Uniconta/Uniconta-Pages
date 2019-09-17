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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for InventoryHierarichalBOMStatement.xaml
    /// </summary>
    public partial class InventoryHierarchicalBOMStatement : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryHierarchicalBOMStatement; } }

        List<HierarichalBOMStatement> listInvBomClient;
        InvBOM invBom;
        InvItem invClient;
        int Id;
        SQLCache invItemCache;
        CWServerFilter filterDialog;
        SortingProperties[] defaultSort;
        Filter[] defaultFilters;
        bool defaultFilterCleared;
        IEnumerable<PropValuePair> filters;
        FilterSorter sort;
        ItemBase ibase;

        // we support two different masters. InvItem or InvBOM
        public InventoryHierarchicalBOMStatement(UnicontaBaseEntity baseEntity) : base(null)
        {
            InitializeComponent();
            BusyIndicator = busyIndicator;
            invClient = baseEntity as InvItem;
            if (invClient == null)
                invBom = baseEntity as InvBOM;
            baseRibbon = localMenu;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            invItemCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.InvItem));
            SetDefaultFilterValues();
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            GetMenuItem();
            if (invClient is InvItem && invClient._ItemType == (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                this.colUnfoldBom.Visible = true;
            SetTreeListViewStyle();
            dgInvBomclientGrid.View.ShownColumnChooser += View_ShownColumnChooser;
        }

        private void View_ShownColumnChooser(object sender, RoutedEventArgs e)
        {
            dgInvBomclientGrid.View.ColumnChooserState = new DefaultColumnChooserState() { Location = Mouse.GetPosition(this) };
        }

        public override void PerformAction(ShortcutAction action)
        {
            if (action == ShortcutAction.ShowColumnChooser)
                dgInvBomclientGrid.View.ShowColumnChooser();
            base.PerformAction(action);
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
                case "ApplyFilter":
                    if (filterDialog == null)
                    {
                        if (defaultFilterCleared)
                            filterDialog = new CWServerFilter(api, typeof(InvBOMClient), null, defaultSort);
                        else
                            filterDialog = new CWServerFilter(api, typeof(InvBOMClient), defaultFilters, defaultSort);
                        filterDialog.Closing += FilterDialog_Closing;
#if !SILVERLIGHT
                        filterDialog.Show();
                    }
                    else
                        filterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    filterDialog.Show();
#endif
                    break;
                case "ClearFilter":
                    filterDialog = null;
                    filters = null;
                    defaultFilterCleared = true;
                    break;
                case "ExpandAndCollapse":
                    if (dgInvBomclientGrid.ItemsSource == null) return;
                    SetExpandCollapse();
                    break;
                case "ExpandRow":
                    if (dgInvBomclientGrid.SelectedItem == null) return;
                    SetExpandRows();
                    break;
            }
        }

        private void SetExpandRows()
        {
            var rowHandles = dgInvBomclientGrid.GetSelectedRowHandles();
            treeListView.ExpandNode(rowHandles[0]);
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

            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !treeListView.AutoExpandAllNodes)
            {
                treeListView.ExpandAllNodes();
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("CollapseAll");
                ibase.LargeGlyph = Utility.GetGlyph(";component/Assets/img/Collapse_32x32.png");
            }
            else
            {
                treeListView.CollapseAllNodes();
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                ibase.LargeGlyph = Utility.GetGlyph(";component/Assets/img/Expand_32x32.png");
            }
        }

        private void FilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (filterDialog.DialogResult == true)
            {
                filters = filterDialog.PropValuePair;
                sort = filterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            filterDialog.Hide();
#endif
        }

        public override Task InitQuery()
        {
            return BindGrid();
        }

        async private Task BindGrid()
        {
            listInvBomClient = new List<HierarichalBOMStatement>();
            busyIndicator.IsBusy = true;
            if (invItemCache == null)
                invItemCache = await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.InvItem), api);
            if (invClient == null)
                invClient = (InvItem)invItemCache.Get(invBom._ItemMaster);
            if (invClient._ItemType >= (byte)Uniconta.DataModel.ItemType.BOM)
            {
                // lets create the topnode
                Id = 0;
                var hierarichalInvBom = new HierarichalBOMStatement() { _ItemPart = invClient._Item, _Qty = 1, ID = ++Id };
                listInvBomClient.Add(hierarichalInvBom);

                await CreateHierarichalInvBomStatement(invClient, 1, hierarichalInvBom.ID);
            }
            dgInvBomclientGrid.ItemsSource = listInvBomClient;
            busyIndicator.IsBusy = false;
        }

        async private Task CreateHierarichalInvBomStatement(InvItem Item, int level, int ParentID)
        {
            var subInvBoms = await api.Query<HierarichalBOMStatement>(Item, filters); // Applying Filters
            if (subInvBoms != null && subInvBoms.Length > 0)
            {
                SortInvBom(subInvBoms);
                foreach (var sub in subInvBoms)
                {
                    sub.ParentID = ParentID;
                    sub.ID = ++Id;
                    listInvBomClient.Add(sub);

                    if (level < 25)
                    {
                        var invClient = (InvItem)invItemCache.Get(sub.ItemPart);
                        if (invClient != null && invClient._ItemType >= (byte)Uniconta.DataModel.ItemType.BOM)
                        {
                            await CreateHierarichalInvBomStatement(invClient, level + 1, sub.ID);
                        }
                    }
                }
            }
        }

        private void SetTreeListViewStyle()
        {
            if (!session.User._ShowGridLines)
            {
                treeListView.ShowHorizontalLines = false;
                treeListView.ShowVerticalLines = false;
            }

#if !SILVERLIGHT
            Style rowStyle = new Style(typeof(DevExpress.Xpf.Grid.RowControl));
            rowStyle.Setters.Add(new Setter(HeightProperty, 25.0));
#else
            Style rowStyle = new Style(typeof(DevExpress.Xpf.Grid.GridRowContent));
            rowStyle.Setters.Add(new Setter(DevExpress.Xpf.Grid.GridRowContent.HeightProperty,25.0));
#endif
            treeListView.RowStyle = rowStyle;
#if !SILVERLIGHT
            if(!System.Windows.Forms.SystemInformation.TerminalServerSession)
#endif
            if (!session.User._ShowGridLines)
                treeListView.AlternateRowBackground = Application.Current.Resources["AlternateGridRowColor"] as SolidColorBrush;
        }

        private void SortInvBom(HierarichalBOMStatement[] invBoms)
        {
            if (sort != null)
                Array.Sort(invBoms, sort);
        }
    }

    public class HierarichalBOMStatement : InvBOMClient
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
    }
}
