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
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorPackagingTransGrid.SelectedItem;
            switch (ActionType)
            {
                case "DeleteRow":
                    dgDebtorPackagingTransGrid.DeleteRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                        dgDebtorPackagingTransGrid.CopyRow();
                    break;
                case "AddDebtorPackagingTrans":
                    AddInvPackagingTrans();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void AddInvPackagingTrans()
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
