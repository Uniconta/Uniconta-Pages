using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using System.Collections;
using Uniconta.Common;
using Uniconta.API.Inventory;
using Uniconta.DataModel;
using System.Windows;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.Grid;
using Uniconta.Common.Utility;
using System.Windows.Input;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProductionPostedGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProductionPostedClient); } }
        public override IComparer GridSorting { get { return new ProductionPostedSort(); } }
        public override bool Readonly { get { return true; } }
    }

    public partial class ProductionPostedGridPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProductionPostedGridPage; } }

        public ProductionPostedGridPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProductionPostedGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            var table = dgProductionPostedGrid.masterRecord;
            string key = string.Empty;
            if (table is InvItem)
                key = (table as InvItem)?._Item;
            else
                key = Utility.GetHeaderString(dgProductionPostedGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("ProductionPosted"), key);
            SetHeader(header);
        }

        public ProductionPostedGridPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            if (master != null)
                dgProductionPostedGrid.UpdateMaster(master);

            dgProductionPostedGrid.api = api;
            dgProductionPostedGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgProductionPostedGrid);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked; ;
            dgProductionPostedGrid.RowDoubleClick += dgProductionPostedGrid_RowDoubleClick;
        }

        private void dgProductionPostedGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("StockTransaction");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgProductionPostedGrid_RowDoubleClick();
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProductionPostedGrid.SelectedItem as ProductionPostedClient;

            switch (ActionType)
            {
                case "StockTransaction":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryTransactions, dgProductionPostedGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionPosted"), selectedItem._Item));
                    break;

                case "CreateProduction":
                    if (selectedItem != null)
                        CreateProdcution(selectedItem);
                    break;
                case "PostedTransaction":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AccountsTransaction, selectedItem, Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher));
                    break;
                case "DeleteProduction":
                    if (selectedItem != null)
                        DeleteJournal(selectedItem);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._OrderNumber);
                        AddDockItem(TabControls.UserDocsPage, dgProductionPostedGrid.syncEntity, header);
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void DeleteJournal(ProductionPostedClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var deleteDialog = new DeletePostedJournal();
            deleteDialog.Closed += async delegate
            {
                if (deleteDialog.DialogResult == true)
                {
                    var pApi = new ProductionAPI(api);
                    ErrorCodes res = await pApi.DeletePostedProduction(selectedItem, deleteDialog.Comment);
                    if (res == ErrorCodes.Succes)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("Journaldeleted"), selectedItem._LineNumber), Uniconta.ClientTools.Localization.lookup("Message"));
                        dgProductionPostedGrid.UpdateItemSource(3, selectedItem);
                    }
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            deleteDialog.Show();
        }

        private void CreateProdcution(ProductionPostedClient fromProductionPosted)
        {
            var prodOrder = new ProductionOrderClient() { _ProdItem = fromProductionPosted._Item, _ProdQty = fromProductionPosted._Qty };
            var cwProductionOrderLine = new CWProductionOrderLine(prodOrder, api, true, Uniconta.ClientTools.Localization.lookup("ProductionOrder"));
#if !SILVERLIGHT
            cwProductionOrderLine.DialogTableId = 2000000079;
#endif
            cwProductionOrderLine.Closed += async delegate
            {
                if (cwProductionOrderLine.DialogResult == true)
                {
                    prodOrder._DeliveryDate = cwProductionOrderLine.DeliveryDate;
                    var prodApi = new ProductionAPI(api);
                    var result = await prodApi.CreateProductionFromProduction(fromProductionPosted, prodOrder, cwProductionOrderLine.quantity, (StorageRegister)cwProductionOrderLine.Storage);
                    if (result != ErrorCodes.Succes)
                        Uniconta.ClientTools.Util.UtilDisplay.ShowErrorCode(result);
                    else
                    {
#if !SILVERLIGHT
                        if (UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"),Uniconta.ClientTools.Localization.lookup("ProductionLines")), 
                        Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
#elif SILVERLIGHT
                        if (UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("ProductionLines")),
                        Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
#endif
                            AddDockItem(TabControls.ProductionOrderLines, prodOrder);
                    }
                }
            };
            cwProductionOrderLine.Show();
        }

        public ProductionPostedGridPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }
    }
}
