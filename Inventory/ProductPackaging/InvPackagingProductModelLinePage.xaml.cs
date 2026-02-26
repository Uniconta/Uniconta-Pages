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
using UnicontaClient.Utilities;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingProductModelLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackingProductModelLineClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvPackagingProductModelLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvPackagingProductModelLinePage; } }
        public InvPackagingProductModelLinePage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        public InvPackagingProductModelLinePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            Init(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvPackagingProductModelLineGrid.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgInvPackagingProductModelLineGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("Lines"), ": ", key);
            SetHeader(header);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvPackagingProductModelLineGrid;
            SetRibbonControl(localMenu, dgInvPackagingProductModelLineGrid);
            dgInvPackagingProductModelLineGrid.api = api;
            dgInvPackagingProductModelLineGrid.UpdateMaster(master);
            dgInvPackagingProductModelLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgInvPackagingProductModelLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgInvPackagingProductModelLineGrid.tableView.ShowingEditor += TableView_ShowingEditor;
        }

        Task BindGrid()
        {
            return dgInvPackagingProductModelLineGrid.Filter(null);
        }

        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var view = sender as TableView;
            var row = view?.Grid?.GetRow(e.RowHandle) as InvPackingProductModelLine;
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

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvPackingProductModelLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvPackagingProductModelLineGrid_PropertyChanged;
            var selectedItem = e.NewItem as InvPackingProductModelLineClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvPackagingProductModelLineGrid_PropertyChanged;
        }

        private void InvPackagingProductModelLineGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvPackingProductModelLineClient;
            switch (e.PropertyName)
            {
                case "ReportingType":
                    rec.Category = null;
                    rec.PackagingType = null;
                    SetCategorySource(rec);
                    SetTypeSource(rec);
                    break;
            }
        }

        private void SetCategorySource(InvPackingProductModelLineClient rec)
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

        private void SetTypeSource(InvPackingProductModelLineClient rec)
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
            switch (ActionType)
            {
                case "SaveGrid":
                    Save();
                    break;
                case "DeleteRow":
                    dgInvPackagingProductModelLineGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void Save()
        {
            int i = 0;
            foreach (var item in dgInvPackagingProductModelLineGrid.GetVisibleRows() as IEnumerable<InvPackingProductModelLineClient>)
            {
                i++;
                if (item._Reporting == ReportingType.Packing && item._WasteSorting == 0)
                {
                    var msg = $"{string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), Uniconta.ClientTools.Localization.lookup("WasteSorting"))}, {Uniconta.ClientTools.Localization.lookup("RowNumber")}: {i}";
                    UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var savetask = saveGrid();
            if (savetask != null)
                await savetask;
            await dgInvPackagingProductModelLineGrid.RefreshTask();
        }

        private void Category_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvPackagingProductModelLineGrid.SelectedItem as InvPackingProductModelLineClient;
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
            var selectedItem = dgInvPackagingProductModelLineGrid.SelectedItem as InvPackingProductModelLineClient;
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
