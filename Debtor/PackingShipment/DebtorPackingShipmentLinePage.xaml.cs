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
    public class DebtorPackingShipmentLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorPackingShipmentLineClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class DebtorPackingShipmentLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorPackingShipmentLinePage; } }
        public DebtorPackingShipmentLinePage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgDebtorPackingShipmentLineGrid;
            SetRibbonControl(localMenu, dgDebtorPackingShipmentLineGrid);
            dgDebtorPackingShipmentLineGrid.api = api;
            dgDebtorPackingShipmentLineGrid.UpdateMaster(master);
            dgDebtorPackingShipmentLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "DeleteRow":
                    dgDebtorPackingShipmentLineGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
