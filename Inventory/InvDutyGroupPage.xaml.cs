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
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvDutyGroupPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvDutyGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    /// <summary>
    /// Interaction logic for InvDutyGroupPage.xaml
    /// </summary>
    public partial class InvDutyGroupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvDutyGroupPage; } }
        public InvDutyGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public InvDutyGroupPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            ((TableView)dgInvDutyGroupGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgInvDutyGroupGrid;
            SetRibbonControl(localMenu, dgInvDutyGroupGrid);
            dgInvDutyGroupGrid.api = api;
            dgInvDutyGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked; ;
            dgInvDutyGroupGrid.RowDoubleClick += DgInvDutyGroupGrid_RowDoubleClick;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var addBtn = UtilDisplay.GetMenuCommandByName(rb, "AddRow");
            addBtn.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("Duty"));
        }

        private void DgInvDutyGroupGrid_RowDoubleClick()
        {
            LocalMenu_OnItemClicked("Lines");
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.InvItem) });
        }

        bool CopiedData;
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvDutyGroupGrid.SelectedItem as InvDutyGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgInvDutyGroupGrid.AddRow();
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgInvDutyGroupGrid.CopyRow();
                    break;
                case "SaveGrid":
                    if (dgInvDutyGroupGrid.SelectedItem != null)
                        saveGrid(dgInvDutyGroupGrid.SelectedItem as InvDutyGroupClient);
                    break;
                case "DeleteRow":
                    if (dgInvDutyGroupGrid.SelectedItem == null) return;

                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        dgInvDutyGroupGrid.DeleteRow();
                    break;
                case "Lines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgInvDutyGroupGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgInvDutyGroupGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void SaveAndOpenLines(InvDutyGroupClient selectedItem)
        {
            if (dgInvDutyGroupGrid.HasUnsavedData)
            {
                var task = saveGrid(selectedItem);

                if (task != null && (CopiedData || selectedItem.RowId == 0))
                    await task;
            }

            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.InvDutyGroupLinePage, selectedItem);
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (sender as Image).Tag as InvDutyGroupClient;
            if (row != null)
                AddDockItem(TabControls.UserDocsPage, dgInvDutyGroupGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = (sender as Image).Tag as InvDutyGroupClient;
            if (row != null)
                AddDockItem(TabControls.UserNotesPage, dgInvDutyGroupGrid.syncEntity);
        }
    }
}
