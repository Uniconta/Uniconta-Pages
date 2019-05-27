using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorPriceListGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorPriceListClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class VendorPriceListPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.VendorPriceListPage; } }
        public VendorPriceListPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public VendorPriceListPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        private void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorPriceListGrid);
            dgCreditorPriceListGrid.api = api;
            dgCreditorPriceListGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCreditorPriceListGrid.RowDoubleClick += DgCreditorPriceListGrid_RowDoubleClick; ;
        }

        private void DgCreditorPriceListGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("Lines");
        }

        bool CopiedData;
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorPriceListGrid.SelectedItem as InvPriceList;
            switch (ActionType)
            {
                case "AddRow":
                    selectedItem = dgCreditorPriceListGrid.AddRow() as InvPriceList;
                    if (selectedItem != null)
                        selectedItem._Active = true;
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgCreditorPriceListGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "DeleteRow":
                    if (selectedItem == null) return;
                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        dgCreditorPriceListGrid.DeleteRow();
                    break;
                case "Lines":
                    if (selectedItem != null)
                        SaveAndOpenPriceList(selectedItem);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgCreditorPriceListGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgCreditorPriceListGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveAndOpenPriceList(InvPriceList selectedItem)
        {
            if (dgCreditorPriceListGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.CustomerPriceListLinePage, dgCreditorPriceListGrid.syncEntity);
        }
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.InvGroup), typeof(Uniconta.DataModel.InvDiscountGroup) });
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (sender as Image).Tag as CreditorPriceListClient;
            if (row != null)
                AddDockItem(TabControls.UserDocsPage, dgCreditorPriceListGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (sender as Image).Tag as CreditorPriceListClient;
            if (row != null)
                AddDockItem(TabControls.UserNotesPage, dgCreditorPriceListGrid.syncEntity);
        }
    }
}
