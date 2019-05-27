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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void CreateProdcution(ProductionPostedClient fromProductionPosted)
        {
            var cwProductionOrderLine = new CWProductionOrderLine(api, true,Uniconta.ClientTools.Localization.lookup("ProductionOrder"),Decimal: fromProductionPosted.Decimals);
            cwProductionOrderLine.Closed += async delegate
            {
                if (cwProductionOrderLine.DialogResult == true)
                {
                    var prodOrder = new ProductionOrderClient();
                    var prodApi = new ProductionAPI(api);
                    var result = await prodApi.CreateProductionFromProduction(fromProductionPosted, prodOrder, cwProductionOrderLine.quantity, (StorageRegister)cwProductionOrderLine.storage);
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
