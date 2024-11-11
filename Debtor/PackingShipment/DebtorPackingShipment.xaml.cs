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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
            
        }
    }
}
