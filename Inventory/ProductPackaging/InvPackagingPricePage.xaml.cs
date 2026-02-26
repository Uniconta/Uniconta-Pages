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
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingPricePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackagingPriceClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvPackagingPricePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvPackagingPricePage; } }

        public InvPackagingPricePage(BaseAPI API, string lookupKey) : base(API, lookupKey)
        {
            Init();
        }
        public InvPackagingPricePage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgInvPackagingPriceGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("ProducerResponsibility"), ": ", key);
            SetHeader(header);
        }


        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvPackagingPriceGrid;
            SetRibbonControl(localMenu, dgInvPackagingPriceGrid);
            dgInvPackagingPriceGrid.api = api;
            dgInvPackagingPriceGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgInvPackagingPriceGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgInvPackagingPriceGrid.tableView.ShowingEditor += TableView_ShowingEditor;
        }

        Task BindGrid()
        {
            return dgInvPackagingPriceGrid.Filter(null);
        }

        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var view = sender as TableView;
            var row = view?.Grid?.GetRow(e.RowHandle) as InvPackagingPriceClient;
            if (row == null) return;

            switch (e.Column.FieldName)
            {
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
            var oldselectedItem = e.OldItem as InvPackagingPriceClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvPackagingPriceGrid_PropertyChanged;
            var selectedItem = e.NewItem as InvPackagingPriceClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvPackagingPriceGrid_PropertyChanged;
        }
        private void InvPackagingPriceGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvPackagingPriceClient;
            switch (e.PropertyName)
            {
                case "ReportingType":
                    rec.Category = null;
                    SetCategorySource(rec);
                    break;
            }
        }

        private void SetCategorySource(InvPackagingPriceClient rec)
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

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvPackagingPriceGrid.SelectedItem as InvPackagingPriceClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgInvPackagingPriceGrid.AddRow();
                    break;
                case "DeleteRow":
                    dgInvPackagingPriceGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        dgInvPackagingPriceGrid.CopyRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void Save()
        {
            int i = 0;
            //foreach (var item in dgInvPackagingPriceGrid.GetVisibleRows() as IEnumerable<InvPackagingPriceClient>)
            //{
            //    i++;
            //    if (item._Reporting == ReportingType.Packing && item._WasteSorting == 0)
            //    {
            //        var msg = $"{string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), Uniconta.ClientTools.Localization.lookup("WasteSorting"))}, {Uniconta.ClientTools.Localization.lookup("RowNumber")}: {i}";
            //        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            //        return;
            //    }
            //}
            saveGrid();
        }
       
        private void Category_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvPackagingPriceGrid.SelectedItem as InvPackagingPriceClient;
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
