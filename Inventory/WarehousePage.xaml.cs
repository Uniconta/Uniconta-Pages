using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class WarehouseClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvWarehouseClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class WarehousePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.WarehousePage; } }
        public WarehousePage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        public WarehousePage(BaseAPI API):base(API, string.Empty)
        {
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgWarehouseClientGrid);
            dgWarehouseClientGrid.api = api;
            dgWarehouseClientGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgWarehouseClientGrid.SelectedItem as InvWarehouseClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgWarehouseClientGrid.AddRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgWarehouseClientGrid.DeleteRow();
                    break;
                case "Location":
                    if (selectedItem == null) return;
                    AddDockItem(TabControls.LocationPage, selectedItem, string.Format("{0}:{1},{2}", Uniconta.ClientTools.Localization.lookup("Location"), selectedItem.Warehouse, selectedItem.Name));
                    break;
                case "JoinWarehouse":
                    if (selectedItem == null)
                        return;
                    var cwJoinTwoWareHouse = new CwJoinTwoWarehouseLocation(api, selectedItem);
                    cwJoinTwoWareHouse.Closed += async delegate
                    {
                        if (cwJoinTwoWareHouse.DialogResult == true)
                        {
                            var ret = await cwJoinTwoWareHouse.JoinResult;
                            UtilDisplay.ShowErrorCode(ret);
                            if (ret == ErrorCodes.Succes)
                                dgWarehouseClientGrid.Refresh();
                        }
                    };
                    cwJoinTwoWareHouse.Show();
                    break;
                case "JoinLocation":
                    if (selectedItem == null)
                        return;
                    var cwJoinTwoLocation = new CwJoinTwoWarehouseLocation(api, selectedItem,joinWareHouse: false);
                    cwJoinTwoLocation.Closed += async delegate
                    {
                        if (cwJoinTwoLocation.DialogResult == true)
                        {
                            var ret = await cwJoinTwoLocation.JoinResult;
                            UtilDisplay.ShowErrorCode(ret);
                        }
                    };
                    cwJoinTwoLocation.Show();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}