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
                invBom = baseEntity as InvBOMClient;
                if (invBom != null)
                    Quantity = invBom._Qty;
            }
            baseRibbon = localMenu;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            LoadBOM = new LoadInvBOMDeep(api);
            SetDefaultFilterValues();
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            GetMenuItem();
            SetTreeListViewStyle();
            dgInvBomclientGrid.View.ShownColumnChooser += View_ShownColumnChooser;
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += RootVisual_KeyDown;
#else
            this.PreviewKeyDown += RootVisual_KeyDown;
#endif
            this.BeforeClose += InventoryHierarchicalBOMStatement_BeforeClose;
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

        private void InventoryHierarchicalBOMStatement_BeforeClose()
        {
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown -= RootVisual_KeyDown;
#else
            this.PreviewKeyDown -= RootVisual_KeyDown;
#endif
        }

        private void RootVisual_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.F6 && dgInvBomclientGrid.CurrentColumn == ItemPart)
            {
                var itempart = dgInvBomclientGrid.CurrentCellValue;
                var lookupTable = new LookUpTable();
                lookupTable.api = this.api;
                lookupTable.KeyStr = Convert.ToString(itempart);
                lookupTable.TableType = typeof(InvItem);
                this.LookUpTable(lookupTable, Uniconta.ClientTools.Localization.lookup("Lookup"), TabControls.InventoryItems);
            }
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
                ibase.LargeGlyph = Utility.GetGlyph("Collapse_32x32.png");
            }
            else
            {
                treeListView.CollapseAllNodes();
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                ibase.LargeGlyph = Utility.GetGlyph("Expand_32x32.png");
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
            if (!System.Windows.Forms.SystemInformation.TerminalServerSession)
#endif
                if (!session.User._ShowGridLines)
                    treeListView.AlternateRowBackground = Application.Current.Resources["AlternateGridRowColor"] as SolidColorBrush;
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var invBomItem = (sender as Image).Tag as InvBOMClient;
            if (invBomItem != null)
                AddDockItem(TabControls.UserDocsPage, invBomItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Note"), invBomItem._ItemPart));
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var invBomItem = (sender as Image).Tag as InvBOMClient;
            if (invBomItem != null)
                AddDockItem(TabControls.UserNotesPage, invBomItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Document"), invBomItem._ItemPart));
        }
    }
}
