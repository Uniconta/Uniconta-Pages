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
    public class TMEmpCalendarPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TMEmpCalendarClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class TMEmpCalendarPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.TMEmpCalendarPage; } }

        public TMEmpCalendarPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            ((TableView)dgTMEmpCalendarGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgTMEmpCalendarGrid;
            SetRibbonControl(localMenu, dgTMEmpCalendarGrid);
            dgTMEmpCalendarGrid.api = api;
            dgTMEmpCalendarGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgTMEmpCalendarGrid.RowDoubleClick += DgTMEmpCalendarGrid_RowDoubleClick;
        }

        private void DgTMEmpCalendarGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("Lines");
        }

        bool CopiedData;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTMEmpCalendarGrid.SelectedItem as TMEmpCalendarClient;
            switch (ActionType)
            {
                case "AddRow":
                    selectedItem = dgTMEmpCalendarGrid.AddRow() as TMEmpCalendarClient;
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgTMEmpCalendarGrid.CopyRow();
                    break;
                case "SaveGrid":
                    if (selectedItem != null)
                        saveGrid(selectedItem);
                    break;
                case "DeleteRow":
                    if (selectedItem == null) return;
                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        dgTMEmpCalendarGrid.DeleteRow(false);
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

        async void SaveAndOpenLines(TMEmpCalendarClient selectedItem)
        {
            if (dgTMEmpCalendarGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.TMEmpCalendarLinePage, selectedItem);
        }

        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DgTMEmpCalendarGrid_RowDoubleClick();
        }
    }
}
