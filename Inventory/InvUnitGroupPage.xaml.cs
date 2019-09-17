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
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvUnitGroupPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvUnitGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvUnitGroupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvUnitGroupPage; } }

        public InvUnitGroupPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        public InvUnitGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            ((TableView)dgInvUnitGroupGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgInvUnitGroupGrid;
            SetRibbonControl(localMenu, dgInvUnitGroupGrid);
            dgInvUnitGroupGrid.api = api;
            dgInvUnitGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvUnitGroupGrid.RowDoubleClick += DgInvUnitGroupGrid_RowDoubleClick;
        }

        private void DgInvUnitGroupGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("Lines");
        }

        bool CopiedData;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvUnitGroupGrid.SelectedItem as InvUnitGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    selectedItem = dgInvUnitGroupGrid.AddRow() as InvUnitGroupClient;
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgInvUnitGroupGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "DeleteRow":
                    if (selectedItem == null) return;
                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        dgInvUnitGroupGrid.DeleteRow(false);
                    break;
                case "Lines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveAndOpenLines(InvUnitGroupClient selectedItem)
        {
            if (dgInvUnitGroupGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.InvUnitGroupLinePage, selectedItem);
        }
    }
}
