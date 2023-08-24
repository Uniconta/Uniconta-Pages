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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CompanyFolderPageGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(CompanyFolderClient); }
        }
        public override bool Readonly { get { return false; } }
    }

    public partial class CompanyFolderPage : GridBasePage
    {
        public CompanyFolderPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        void InitPage()
        {
            InitializeComponent();
            dgFolderGrid.BusyIndicator = busyIndicator;
            dgFolderGrid.api = api;
            SetRibbonControl(localMenu, dgFolderGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgFolderGrid.RowDoubleClick += DgFolderGrid_RowDoubleClick;
        }

        void DgFolderGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("AddDoc");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DgFolderGrid_RowDoubleClick();
        }
        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var order = (sender as Image).Tag as CompanyFolderClient;
            if (order != null)
                AddDockItem(TabControls.UserDocsPage, order, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), order.Name));
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgFolderGrid.SelectedItem as CompanyFolderClient;
            switch (ActionType)
            {
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgFolderGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem.Name));
                    break;
                case "DeleteRow":
                    if (selectedItem?._HasDocs == false)
                        dgFolderGrid.DeleteRow();
                    break;
                default:
                    SaveAndOpenLines(ActionType, selectedItem);
                    break;
            }
        }

        async void SaveAndOpenLines(string ActionType, CompanyFolderClient selectedItem)
        {
            if (selectedItem != null && dgFolderGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && selectedItem.RowId == 0)
                    await tsk;
            }
            gridRibbon_BaseActions(ActionType);
        }
    }
}
