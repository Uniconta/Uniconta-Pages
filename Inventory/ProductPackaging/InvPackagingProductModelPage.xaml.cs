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
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingProductModelGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackingProductModelClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvPackagingProductModelPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvPackagingProductModelPage; } }

        public InvPackagingProductModelPage(BaseAPI API, string lookupKey) : base(API, lookupKey)
        {
            Init();
        }
        public InvPackagingProductModelPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvPackagingProductModelGrid;
            SetRibbonControl(localMenu, dgInvPackagingProductModelGrid);
            dgInvPackagingProductModelGrid.api = api;
            dgInvPackagingProductModelGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvPackagingProductModelGrid.SelectedItem as InvPackingProductModelClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    dgInvPackagingProductModelGrid.DeleteRow();
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

        async private void SaveAndOpenLines(InvPackingProductModelClient selectedItem)
        {
            if (dgInvPackagingProductModelGrid.HasUnsavedData)
            {
                var task = saveGrid(selectedItem);
                if (task != null && selectedItem.RowId == 0)
                    await task;
            }

            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.InvPackagingProductModelLinePage, dgInvPackagingProductModelGrid.syncEntity);
        }

        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            LocalMenu_OnItemClicked("Lines");
        }
    }
}
