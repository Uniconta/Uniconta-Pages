using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
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
using System.Windows.Threading;
using Uniconta.ClientTools.Util;

using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPriceListGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorPriceListClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CustomerPriceListsPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CustomerPriceListsPage; } }
        public CustomerPriceListsPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public CustomerPriceListsPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvPriceListGrid;
            SetRibbonControl(localMenu, dgInvPriceListGrid);
            dgInvPriceListGrid.api = api;
            dgInvPriceListGrid.BusyIndicator = busyIndicator;
            dgInvPriceListGrid.RowDoubleClick += DgInvPriceListGrid_RowDoubleClick;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void DgInvPriceListGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("Lines");
        }

        bool CopiedData;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvPriceListGrid.SelectedItem as InvPriceList;
            switch (ActionType)
            {
                case "AddRow":
                    selectedItem = dgInvPriceListGrid.AddRow() as InvPriceList;
                    if (selectedItem != null)
                        selectedItem._Active = true;
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgInvPriceListGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "DeleteRow":
                    if (selectedItem == null) return;
                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        dgInvPriceListGrid.DeleteRow(false);
                    break;
                case "Lines":
                    if (selectedItem != null)
                        SaveAndOpenPriceList(selectedItem);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgInvPriceListGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgInvPriceListGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveAndOpenPriceList(InvPriceList selectedItem)
        {
            if (dgInvPriceListGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.CustomerPriceListLinePage, dgInvPriceListGrid.syncEntity);
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.InvGroup), typeof(Uniconta.DataModel.InvDiscountGroup) });
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (sender as Image).Tag as DebtorPriceListClient;
            if (row != null)
                AddDockItem(TabControls.UserDocsPage, dgInvPriceListGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (sender as Image).Tag as DebtorPriceListClient;
            if (row != null)
                AddDockItem(TabControls.UserNotesPage, dgInvPriceListGrid.syncEntity);
        }
    }
}
