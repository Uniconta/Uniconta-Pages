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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvDutyGroupLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvDutyGroupLineClient); } }
        public override bool Readonly { get { return false; } }
    }

    /// <summary>
    /// Interaction logic for InvDutyGroupLinePage.xaml
    /// </summary>
    public partial class InvDutyGroupLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvDutyGroupLinePage; } }
        public InvDutyGroupLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            ((TableView)dgInvDutyGroupLineGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            dgInvDutyGroupLineGrid.UpdateMaster(master);
            localMenu.dataGrid = dgInvDutyGroupLineGrid;
            SetRibbonControl(localMenu, dgInvDutyGroupLineGrid);
            dgInvDutyGroupLineGrid.api = api;
            dgInvDutyGroupLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked; ;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch(ActionType)
            {
                case "AddRow":
                    dgInvDutyGroupLineGrid.AddRow();
                    break;
                case "CopyRow":
                    dgInvDutyGroupLineGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgInvDutyGroupLineGrid.SaveData();
                    break;
                case "DeleteRow":
                    var selectedItem = dgInvDutyGroupLineGrid.SelectedItem as InvDutyGroupLineClient;
                    if (selectedItem != null)
                        dgInvDutyGroupLineGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
