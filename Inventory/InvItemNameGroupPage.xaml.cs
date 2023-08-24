using Uniconta.API.Inventory;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvItemNameGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvItemNameGroupClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class InvItemNameGroupPage : GridBasePage
    {
        public InvItemNameGroupPage(BaseAPI API) : this(API, string.Empty)
        {
        }

        public InvItemNameGroupPage(BaseAPI api, string lookupKey)
           : base(api, lookupKey)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgInventoryNameGroupGrid);
            dgInventoryNameGroupGrid.api = this.api;
            dgInventoryNameGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInventoryNameGroupGrid.RowDoubleClick += dgInventoryNameGroupGrid_RowDoubleClick; ;
        }
        private void dgInventoryNameGroupGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("Lines");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgInventoryNameGroupGrid_RowDoubleClick();
        }

        bool CopiedData;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInventoryNameGroupGrid.SelectedItem as InvItemNameGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgInventoryNameGroupGrid.AddRow();
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgInventoryNameGroupGrid.CopyRow();
                    break;
                case "DeleteRow":
                    if (selectedItem == null) return;
                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        dgInventoryNameGroupGrid.DeleteRow(false);
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "Lines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgInventoryNameGroupGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgInventoryNameGroupGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveAndOpenLines(InvItemNameGroupClient selectedItem)
        {
            if (dgInventoryNameGroupGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.LanguageItemTextPage, dgInventoryNameGroupGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("LanguageItemText"), selectedItem._Name));
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (sender as Image).Tag as InvItemNameGroupClient;
            if (row != null)
                AddDockItem(TabControls.UserDocsPage, dgInventoryNameGroupGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (sender as Image).Tag as InvItemNameGroupClient;
            if (row != null)
                AddDockItem(TabControls.UserNotesPage, dgInventoryNameGroupGrid.syncEntity);
        }
    }
}
