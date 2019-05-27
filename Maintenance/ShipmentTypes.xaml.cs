using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ShipmentTypesPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ShipmentTypeClient); } }


        public override bool Readonly { get { return false; } }
    }

    public partial class ShipmentTypes : GridBasePage
    {
        protected override bool IsLayoutSaveRequired() { return false; }

        public ShipmentTypes(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgShipmentTypesPageGrid);
            dgShipmentTypesPageGrid.api = api;
            dgShipmentTypesPageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgShipmentTypesPageGrid.AddRow();
                    break;
                case "CopyRow":
                    dgShipmentTypesPageGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgShipmentTypesPageGrid.SaveData();
                    break;
                case "DeleteRow":
                    dgShipmentTypesPageGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
