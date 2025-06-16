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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorPackagingTransGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackagingTransClient); } }
        public override bool Readonly { get { return IsReadOnly; } }

        public bool IsReadOnly;
    }

    public partial class DebtorPackagingTransPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorPackagingTransPage; } }
        public DebtorPackagingTransPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }
        public DebtorPackagingTransPage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgDebtorPackagingTransGrid.IsReadOnly = master == null;
            localMenu.dataGrid = dgDebtorPackagingTransGrid;
            SetRibbonControl(localMenu, dgDebtorPackagingTransGrid);
            dgDebtorPackagingTransGrid.api = api;
            dgDebtorPackagingTransGrid.UpdateMaster(master);
            dgDebtorPackagingTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgDebtorPackagingTransGrid.tableView.ShowingEditor += TableView_ShowingEditor;
        }

        private void TableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var view = sender as TableView;
            var row = view?.Grid?.GetRow(e.RowHandle) as InvPackagingTransClient;
            if (row == null) return;
            var reporting = (ReportingType)AppEnums.PackagingReportingType.IndexOf(row.ReportingType);
            switch (e.Column.FieldName)
            {
                case "PackagingType":
                case "WasteSorting":
                case "PackagingRateLevel":
                    if (reporting != Uniconta.DataModel.ReportingType.Packing)
                        e.Cancel = true;
                    break;

                case "PackagingConsumer":
                    if (reporting != Uniconta.DataModel.ReportingType.Packing && reporting != Uniconta.DataModel.ReportingType.Electronic)
                        e.Cancel = true;
                    break;
            }
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorPackagingTransGrid.SelectedItem;
            switch (ActionType)
            {
                case "DeleteRow":
                    dgDebtorPackagingTransGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        dgDebtorPackagingTransGrid.CopyRow();
                    break;
                case "AddPackingModel":
                    AddPackingModel();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void Save()
        {
            int i = 0;
            foreach (var item in dgDebtorPackagingTransGrid.GetVisibleRows() as IEnumerable<InvPackagingTransClient>)
            {
                i++;
                if (item.ReportingType == Uniconta.ClientTools.Localization.lookup("Packaging") && item._WasteSorting == 0) //TODDO:Skal der testes og/eller testes på mere
                {
                    var msg = $"{string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), Uniconta.ClientTools.Localization.lookup("WasteSorting"))}, {Uniconta.ClientTools.Localization.lookup("RowNumber")}: {i}";
                    UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            saveGrid();
        }

        void AddPackingModel()
        {
            var shipmentDialog = new CWPackingShipmentModel(api);
            shipmentDialog.Closing += async delegate
            {
                if (shipmentDialog.DialogResult == true)
                {
                    busyIndicator.IsBusy = true;
                    var model = shipmentDialog.PackingShipmentModel;
                    var qty = shipmentDialog.Qty;
                    var shipment = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.DebtorPackingShipmentModel)).Get(model) as Uniconta.DataModel.DebtorPackingShipmentModel;
                    if (shipment != null)
                    {
                        var lines = await api.Query<DebtorPackingShipmentLineClient>(shipment, null);
                        if (lines != null)
                        {
                            var trans = new List<InvPackagingTransClient>();
                            foreach (var line in lines)
                            {
                                var ptrans = new InvPackagingTransClient();
                                ptrans._Category = line._Category;
                                ptrans.CompanyId = line.CompanyId;
                                ptrans._Packaging = line._Packaging;
                                ptrans._WasteSorting = line._WasteSorting;
                                ptrans._PackagingRateLevel = line._PackagingRateLevel;
                                ptrans._Weight = line._Weight * qty;
                                ptrans._JournalPostedId = (dgDebtorPackagingTransGrid.masterRecord as DebtorInvoiceClient)._JournalPostedId;
                                trans.Add(ptrans);
                            }
                            dgDebtorPackagingTransGrid.PasteRows(trans);
                        }
                    }
                    busyIndicator.IsBusy = false;
                }
            };
            shipmentDialog.ShowDialog();
        }
    }
}
