using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingProductPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackagingProductClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvPackagingProductPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvPackagingProductPage; } }
        public InvPackagingProductPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
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
        }

        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var view = sender as TableView;
            var row = view?.Grid?.GetRow(e.RowHandle) as InvPackagingProductClient;
            if (row == null) return;
            if (e.Column.FieldName == "PackagingType" || e.Column.FieldName == "WasteSorting" || e.Column.FieldName == "PackagingRateLevel" || e.Column.FieldName == "PackagingConsumer")
            {
                if (row._Reporting != ReportingType.Packing && row._Reporting == ReportingType.Electronic && e.Column.FieldName != "PackagingConsumer")
                    e.Cancel = true;
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
                    rec.Category = null;
                    SetCategorySource(rec);
                    rec.ColumnReadOnly = (rec.ReportingType != AppEnums.PackagingReportingType.ToString((byte)ReportingType.Packing));
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
                    validCat = new ArraySegment<string>(AppEnums.PackagingCategory.Values, 60, 7);//Temp to Photovoltic
                    rec.CategorySource = validCat.ToList();
                    break;
                case ReportingType.OneTimeUsePlastic:
                    validCat = new ArraySegment<string>(AppEnums.PackagingCategory.Values, 80, 8);//FoodContainers to FiltersTobacco
                    rec.CategorySource = validCat.ToList();
                    break;
            }
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvPackagingProductGrid.SelectedItem as InvPackagingProductClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgInvPackagingProductGrid.AddRow();
                    break;
                case "DeleteRow":
                    dgInvPackagingProductGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        dgInvPackagingProductGrid.CopyRow();
                    break;
                case "CopyFromItem":
                    CopyFromItem();
                    break;
                case "CopyFromBOM":
                    CopyFromBOM();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void Save()
        {
            int i = 0;
            foreach (var item in dgInvPackagingProductGrid.GetVisibleRows() as IEnumerable<InvPackagingProductClient>)
            {
                i++;
                if (item._Reporting == ReportingType.Packing && item._WasteSorting == 0)
                {
                    var msg = $"{string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), Uniconta.ClientTools.Localization.lookup("WasteSorting"))}, {Uniconta.ClientTools.Localization.lookup("RowNumber")}: {i}";
                    UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            saveGrid();
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
                    {
                        dgInvPackagingProductGrid.PasteRows(lines);
                    }
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
                dgInvPackagingProductGrid.PasteRows(lines);
            }
        }
        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e)
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
    }
}
