using UnicontaClient.Models;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.ClientTools;
using UnicontaClient.Controls.Dialogs;
using EnumsNET;
using Uniconta.DataModel;
using DevExpress.Diagram.Core.Shapes;
using NPOI.SS.Formula.Functions;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingProductPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackagingProductClient); } }
        public override bool Readonly { get { return false; } }
        protected virtual bool ModelLines { get { return false; } }

        protected override IList ToList(UnicontaBaseEntity[] Arr)
        {
            if (Arr != null)
            {
                var rows = Arr.Where(r => (((InvPackagingProduct)r)._PackingModel != null) == ModelLines).ToArray();
                var typed = Array.CreateInstance(Arr.GetType().GetElementType(), rows.Length);
                Array.Copy(rows, typed, rows.Length);
                Arr = (UnicontaBaseEntity[])typed;
            }
            return base.ToList(Arr);
        }
    }

    public class InvPackagingModelLineGrid : InvPackagingProductPageGrid
    {
        protected override bool ModelLines { get { return true; } }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            base.SetDefaultValues(dataEntity, selectedIndex);
            ((InvPackagingProductClient)dataEntity)._Weight = 1; // one of the model
        }
    }

    public partial class InvPackagingProductPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvPackagingProductPage; } }
        public InvPackagingProductPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        public InvPackagingProductPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            Init(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvPackagingProductGrid.UpdateMaster(args);
            dgPackagingModelGrid.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgInvPackagingProductGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("ProducerResponsibility"), ": ", key);
            SetHeader(header);
        }


        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvPackagingProductGrid;
            SetRibbonControl(localMenu, dgInvPackagingProductGrid);
            dgInvPackagingProductGrid.api = api;
            dgInvPackagingProductGrid.UpdateMaster(master);
            dgInvPackagingProductGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgInvPackagingProductGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgInvPackagingProductGrid.tableView.ShowingEditor += TableView_ShowingEditor;

            dgPackagingModelGrid.api = api;
            dgPackagingModelGrid.UpdateMaster(master);
            dgPackagingModelGrid.BusyIndicator = busyIndicator;
            dgInvPackagingProductGrid.GotFocus += (s, e) => modelGridFocus = false;
            dgPackagingModelGrid.GotFocus += (s, e) => modelGridFocus = true;
        }

        bool modelGridFocus;
        InvPackagingProductPageGrid ActiveGrid { get { return modelGridFocus ? dgPackagingModelGrid : dgInvPackagingProductGrid; } }

        public override void AssignMultipleGrid(List<CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgInvPackagingProductGrid);
            gridCtrls.Add(dgPackagingModelGrid);
        }

        public override async Task InitQuery()
        {
            await base.InitQuery();
            await dgPackagingModelGrid.Filter(null);
            FocusOnGrid(dgInvPackagingProductGrid);
            modelGridFocus = false;
        }

        protected override bool FocusOnLastGrid => false;

        Task BindGrid()
        {
            return Task.WhenAll(dgInvPackagingProductGrid.Filter(null), dgPackagingModelGrid.Filter(null));
        }

        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var view = sender as TableView;
            var row = view?.Grid?.GetRow(e.RowHandle) as InvPackagingProductClient;
            if (row == null) return;

            switch (e.Column.FieldName)
            {
                case "PackagingType":
                    if (row._Reporting != ReportingType.Packing && row._Reporting != ReportingType.Batteries)
                        e.Cancel = true;
                    break;
                case "WasteSorting":
                case "PackagingRateLevel":
                    if (row._Reporting != ReportingType.Packing)
                        e.Cancel = true;
                    break;

                case "PackagingConsumer":
                    if (row._Reporting == ReportingType.Batteries || row._Reporting == ReportingType.OneTimeUsePlastic)
                        e.Cancel = true;
                    break;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var item = dgInvPackagingProductGrid.masterRecord as InvItemClient;
            if (item != null && item._ItemType != (byte)ItemType.ProductionBOM)
                localMenu.DisableButtons("CopyFromBOM");
        }
        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvPackagingProductClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvPackagingProductGrid_PropertyChanged;
            var selectedItem = e.NewItem as InvPackagingProductClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvPackagingProductGrid_PropertyChanged;
        }
        private void InvPackagingProductGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvPackagingProductClient;
            switch (e.PropertyName)
            {
                case "ReportingType":
                    if (dgInvPackagingProductGrid.HasUnsavedData)
                    {
                        rec.Category = null;
                        rec.PackagingType = null;
                    }
                    SetCategorySource(rec);
                    SetTypeSource(rec);
                    break;
            }
        }

        private void SetCategorySource(InvPackagingProductClient rec)
        {
            if (rec?._Reporting == null)
                return;
            switch (rec._Reporting)
            {
                case ReportingType.Packing:
                    var validCat = new ArraySegment<string>(AppEnums.PackagingCategory.Values, 0, 17);//0 to PlasticFoam
                    rec.CategorySource = validCat.ToList();
                    break;
                case ReportingType.Batteries:
                    validCat = new ArraySegment<string>(AppEnums.PackagingCategory.Values, 40, 6);//BB to SLI
                    rec.CategorySource = validCat.ToList();
                    break;
                case ReportingType.Electronic:
                    validCat = new ArraySegment<string>(AppEnums.PackagingCategory.Values, 60, 8);//Temp to MediumEquipment
                    rec.CategorySource = validCat.ToList();
                    break;
                case ReportingType.OneTimeUsePlastic:
                    validCat = new ArraySegment<string>(AppEnums.PackagingCategory.Values, 80, 8);//FoodContainers to FiltersTobacco
                    rec.CategorySource = validCat.ToList();
                    break;
            }
        }

        private void SetTypeSource(InvPackagingProductClient rec)
        {
            if (rec?._Reporting == null)
                return;
            switch (rec._Reporting)
            {
                case ReportingType.Packing:
                    var validType = new ArraySegment<string>(AppEnums.PackagingType.Values, 0, 21);//0 to BubbleWrap
                    rec.TypeSource = validType.ToList();
                    break;
                case ReportingType.Batteries:
                    validType = new ArraySegment<string>(AppEnums.PackagingType.Values, 40, 6);//Lithium to NonRechargeable
                    rec.TypeSource = validType.ToList();
                    break;
               default:
                    return;
            }
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var grid = ActiveGrid;
            var selectedItem = grid.SelectedItem as InvPackagingProductClient;
            switch (ActionType)
            {
                case "AddRow":
                    grid.AddRow();
                    break;
                case "DeleteRow":
                    grid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        grid.CopyRow();
                    break;
                case "CopyFromItem":
                    CopyFromItem();
                    break;
                case "CopyFromBOM":
                    CopyFromBOM();
                    break;
                case "PackagingOverview":
                    if (dgInvPackagingProductGrid.masterRecord != null)
                        AddDockItem(TabControls.InvPackagingOverviewPage, dgInvPackagingProductGrid.masterRecord);
                    break;
                case "RefreshGrid":
                    gridRibbon_BaseActions(ActionType);
                    if (dgPackagingModelGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgPackagingModelGrid);
                    dgPackagingModelGrid.Filter(null);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void Save()
        {
            int i = 0;
            foreach (var item in dgInvPackagingProductGrid.GetVisibleRows() as IEnumerable<InvPackagingProductClient>)
            {
                i++;
                if (item._Category == 0)
                {
                    var msg = $"{string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), Uniconta.ClientTools.Localization.lookup("PackagingCategory"))}, {Uniconta.ClientTools.Localization.lookup("RowNumber")}: {i}";
                    UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (item._Reporting == ReportingType.Packing && item._WasteSorting == 0)
                {
                    var msg = $"{string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), Uniconta.ClientTools.Localization.lookup("WasteSorting"))}, {Uniconta.ClientTools.Localization.lookup("RowNumber")}: {i}";
                    UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            i = 0;
            foreach (var item in dgPackagingModelGrid.GetVisibleRows() as IEnumerable<InvPackagingProductClient>)
            {
                i++;
                if (item._PackingModel == null)
                {
                    var msg = $"{string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), Uniconta.ClientTools.Localization.lookup("Configuration"))}, {Uniconta.ClientTools.Localization.lookup("RowNumber")}: {i}";
                    UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var savetask = saveGrid();
            if (savetask != null)
                await savetask;
            if (dgPackagingModelGrid.HasUnsavedData)
                await dgPackagingModelGrid.SaveData();
            await dgInvPackagingProductGrid.RefreshTask();
            await dgPackagingModelGrid.RefreshTask();
        }
        void CopyFromItem()
        {
            var itemDialog = new CWInventoryItems(api);
            itemDialog.Closing += async delegate
            {
                var item = itemDialog.InvItem;
                if (item != null)
                {
                    var lines = await api.Query(Activator.CreateInstance(dgInvPackagingProductGrid.TableTypeUser) as UnicontaBaseEntity, new[] { item }, null);
                    if (lines?.Length > 0)
                        PasteLines(lines);
                }
            };
            itemDialog.Show();
        }
        async void CopyFromBOM()
        {
            var item = dgInvPackagingProductGrid.masterRecord as InvItemClient;
            if (item != null)
            {
                var boms = await api.Query<InvBOMClient>(item);
                 var lines = new List<UnicontaBaseEntity>();
                foreach (var bom in boms)
                {
                    var newLines = await api.Query(Activator.CreateInstance(dgInvPackagingProductGrid.TableTypeUser) as UnicontaBaseEntity, new[] { bom }, null);
                    foreach (InvPackagingProductClient line in newLines)
                    {
                        line._Weight = line._Weight * bom._Qty;
                    }
                    if (newLines?.Length > 0)
                        lines.AddRange(newLines);
                }
                PasteLines(lines);
            }
        }

        void PasteLines(IEnumerable<UnicontaBaseEntity> lines)
        {
            var packagingLines = lines.Where(l => ((InvPackagingProduct)l)._PackingModel == null).ToList();
            var modelLines = lines.Where(l => ((InvPackagingProduct)l)._PackingModel != null).ToList();
            if (packagingLines.Count > 0)
                dgInvPackagingProductGrid.PasteRows(packagingLines);
            if (modelLines.Count > 0)
                dgPackagingModelGrid.PasteRows(modelLines);
        }
        private void Category_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvPackagingProductGrid.SelectedItem as InvPackagingProductClient;
            if (selectedItem?.CategorySource == null)
                SetCategorySource(selectedItem);
            if (selectedItem != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var editor = (ComboBoxEditor)sender;
                    editor.ItemsSource = selectedItem.CategorySource;
                }));
                
            }
        }

        private void Type_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvPackagingProductGrid.SelectedItem as InvPackagingProductClient;
            if (selectedItem?.TypeSource == null)
                SetTypeSource(selectedItem);
            if (selectedItem != null)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var editor = (ComboBoxEditor)sender;
                    editor.ItemsSource = selectedItem.TypeSource;
                }));

            }
        }
    }
}
