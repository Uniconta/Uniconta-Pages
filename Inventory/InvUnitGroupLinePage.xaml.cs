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
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvUnitGroupLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvUnitGroupLineClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvUnitGroupLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvUnitGroupLinePage; } }

        public InvUnitGroupLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            ((TableView)dgInvUnitGroupLineGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            dgInvUnitGroupLineGrid.UpdateMaster(master);
            localMenu.dataGrid = dgInvUnitGroupLineGrid;
            SetRibbonControl(localMenu, dgInvUnitGroupLineGrid);
            dgInvUnitGroupLineGrid.api = api;
            dgInvUnitGroupLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvUnitGroupLineGrid.SelectedItem as InvUnitGroupLineClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgInvUnitGroupLineGrid.AddRow();
                    break;
                case "CopyRow":
                    dgInvUnitGroupLineGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgInvUnitGroupLineGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgInvUnitGroupLineGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
