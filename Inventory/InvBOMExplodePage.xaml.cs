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
using DevExpress.Xpf.Grid;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvBOMExplodeGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvBOMClient); } }
        public override bool Readonly { get { return false; } }
        public override bool CanInsert { get { return false; } }

#if SILVERLIGHT
        protected override DataTemplate RowTemplate()
        {
            return Application.Current.Resources["CustomDataRowTemplateIsParent"] as DataTemplate;
        }
#endif
    }

    /// <summary>
    /// Interaction logic for InvBOMExplodePage.xaml
    /// </summary>
    public partial class InvBOMExplodePage : GridBasePage
    {
        InvBOMClient invBom;
        InvItem invClient;
        double Quantity;
        CWServerFilter filterDialog;
        SortingProperties[] defaultSort;
        Filter[] defaultFilters;
        bool defaultFilterCleared;
        IEnumerable<PropValuePair> filters;
        FilterSorter sort;
        LoadInvBOMDeep LoadBOM;

        public InvBOMExplodePage(UnicontaBaseEntity baseEntity)
            : base(baseEntity)
        {
            InitializeComponent();
            Quantity = 1d;
            invClient = baseEntity as InvItem;
            if (invClient == null)
            {
                var syncMaster2 = baseEntity as IVariant;
                if (syncMaster2 != null)
                    invClient = api.GetCache(typeof(Uniconta.DataModel.InvItem))?.Get(syncMaster2.Item) as InvItemClient;
                else
                {
                    invBom = baseEntity as InvBOMClient;
                    if (invBom != null)
                        Quantity = invBom._Qty;
                }
            }
#if !SILVERLIGHT
            dgInvBomclientGrid.tableView.RowStyle = this.Resources["MatchingRowStyle"] as Style;
#endif
            dgInvBomclientGrid.api = api;
            SetRibbonControl(localMenu, dgInvBomclientGrid);
            dgInvBomclientGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            localMenu.OnCustomEditorValueChanged += LocalMenu_OnCustomEditorValueChanged; ;
            SetDefaultFilterValues();
            LoadBOM = new LoadInvBOMDeep(api);
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            dgInvBomclientGrid.View.ShownColumnChooser += View_ShownColumnChooser;
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += RootVisual_KeyDown;
#else
            this.PreviewKeyDown += RootVisual_KeyDown;
#endif
            dgInvBomclientGrid.ShowTotalSummary();
            dgInvBomclientGrid.CustomSummary += DgInvBomclientGrid_CustomSummary;
            this.BeforeClose += InventoryHierarchicalBOMStatement_BeforeClose;
            HideMenuItems();
            BindQuantity();
        }

        double sumQty, sumCostValue, sumSalesValue, sumCartons, sumWeight, sumVolume;
        private void DgInvBomclientGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            switch(e.SummaryProcess)
            {
                case DevExpress.Data.CustomSummaryProcess.Start:
                    sumQty = sumCostValue = sumSalesValue = sumCartons = sumWeight = sumVolume = 0d;
                    break;
                case DevExpress.Data.CustomSummaryProcess.Calculate:
                    var row = e.Row as InvBOMClient;
                    if(row._ItemType <= Uniconta.DataModel.ItemType.Service)
                    {
                        sumQty += row.Qty;
                        sumCostValue += row.CostValue;
                        sumSalesValue += row.SalesValue;
                        sumCartons += row.Cartons;
                        sumWeight += row.Weight;
                        sumVolume += row.Volume;
                    }
                    break;
                case DevExpress.Data.CustomSummaryProcess.Finalize:
                    var fieldName = ((GridSummaryItem)e.Item).FieldName;

                    switch(fieldName)
                    {
                        case "Qty":
                            e.TotalValue = sumQty;
                            break;
                        case "CostValue":
                            e.TotalValue = sumCostValue;
                            break;
                        case "SalesValue":
                            e.TotalValue = sumSalesValue;
                            break;
                        case "Cartons":
                            e.TotalValue = sumCartons;
                            break;
                        case "Weight":
                            e.TotalValue = sumWeight;
                            break;
                        case "Volume":
                            e.TotalValue = sumVolume;
                            break;
                    }

                    break;
            }
        }

        private void LocalMenu_OnCustomEditorValueChanged(string ActionType, string value)
        {
            switch (ActionType)
            {
                case "SetQuantity":
                    Quantity = Uniconta.Common.Utility.NumberConvert.ToDouble(value);
                    InitQuery();
                    break;
            }
        }

        private void HideMenuItems()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
            {
                UtilDisplay.RemoveMenuCommand(rb, "ExpandAndCollapse");
                UtilDisplay.RemoveMenuCommand(rb, "ExpandRow");
            }
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

        private void SetDefaultFilterValues()
        {
            defaultSort = null;
            defaultFilters = null;
        }

        private void View_ShownColumnChooser(object sender, RoutedEventArgs e)
        {
            dgInvBomclientGrid.View.ColumnChooserState = new DefaultColumnChooserState() { Location = Mouse.GetPosition(this) };
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

        public override void PerformAction(ShortcutAction action)
        {
            if (action == ShortcutAction.ShowColumnChooser)
                dgInvBomclientGrid.View.ShowColumnChooser();
            base.PerformAction(action);
        }

        void BindQuantity()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var itemBase = UtilDisplay.GetMenuCommandByName(rb, "SetQuantity");

            if(itemBase!=null)
                itemBase.CustomText = Convert.ToString(Quantity);
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvBomclientGrid.SelectedItem as InvBOMClient;

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
                        filterDialog.GridSource = dgInvBomclientGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        filterDialog.Closing += FilterDialog_Closing;
                        filterDialog.Show();
                    }
                    else
                    {
                        filterDialog.GridSource = dgInvBomclientGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        filterDialog.Show(true);
                    }
                    break;
                case "ClearFilter":
                    filterDialog = null;
                    filters = null;
                    defaultFilterCleared = true;
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgInvBomclientGrid.SelectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Note"), selectedItem._ItemPart));
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgInvBomclientGrid.SelectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Document"), selectedItem._ItemPart));
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "Storage":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvItemStoragePage, dgInvBomclientGrid.syncEntity, true, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OnHand"), selectedItem._ItemPart));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
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

        public override string NameOfControl { get { return TabControls.InvBOMExplodePage; } }

        public override bool FilterOnLoadLayout => false;
    }
}
