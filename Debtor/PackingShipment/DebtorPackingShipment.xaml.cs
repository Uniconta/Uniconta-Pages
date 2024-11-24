using UnicontaClient.Models;
using System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorPackingShipmentGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorPackingShipmentClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class DebtorPackingShipmentPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorPackingShipmentPage; } }
        public DebtorPackingShipmentPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgDebtorPackingShipmentGrid;
            SetRibbonControl(localMenu, dgDebtorPackingShipmentGrid);
            dgDebtorPackingShipmentGrid.api = api;
            dgDebtorPackingShipmentGrid.UpdateMaster(master);
            dgDebtorPackingShipmentGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorPackingShipmentGrid.SelectedItem as DebtorPackingShipmentClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    dgDebtorPackingShipmentGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    SaveGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }

        }

        private async void SaveGrid()
        {
            var result = await dgDebtorPackingShipmentGrid.SaveData();
            if (result == ErrorCodes.Succes)
                CloseDockItem();
        }
    }
}
