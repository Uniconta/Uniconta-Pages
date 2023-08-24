using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Pages.Maintenance;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uniconta.API.Inventory;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.Windows.Input;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InventoryJournalGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvJournalClient); } }
    }

    public partial class InventoryJournalPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryJournalPage ; } }

        public InventoryJournalPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public InventoryJournalPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgInventoryJournal);
            dgInventoryJournal.api = api;
            dgInventoryJournal.BusyIndicator = busyIndicator;
            
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInventoryJournal.RowDoubleClick += DgInventoryJournal_RowDoubleClick;
        }

        private void DgInventoryJournal_RowDoubleClick()
        {
            var selectedItem = dgInventoryJournal.SelectedItem as InvJournalClient;
            if (selectedItem != null)
                AddDockItem(TabControls.InventoryJournalLines, selectedItem, string.Format("{0} {1} : {2}", Uniconta.ClientTools.Localization.lookup("Inventory"), Uniconta.ClientTools.Localization.lookup("Journal"), selectedItem.Journal));
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DgInventoryJournal_RowDoubleClick();
        }
        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.InvItem));
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.InventoryJournalPage2)
                dgInventoryJournal.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInventoryJournal.SelectedItem as InvJournalClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.InventoryJournalPage2, api, string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Inventory"), Uniconta.ClientTools.Localization.lookup("Journal")), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryJournalPage2, selectedItem, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Journal"), selectedItem.Journal));
                    break;
                case "InventoryJournalLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryJournalLines , selectedItem, string.Format("{0} {1} : {2}", Uniconta.ClientTools.Localization.lookup("Inventory"), Uniconta.ClientTools.Localization.lookup("Journal"), selectedItem.Journal));
                    break;
                case "DeleteAllJournalLines":
                    if (selectedItem == null)
                        return;
                    var text = string.Format(Uniconta.ClientTools.Localization.lookup("JournalContainsLines"), selectedItem.Journal);
                    EraseYearWindow EraseYearWindowDialog = new EraseYearWindow(text, false);
                    EraseYearWindowDialog.Closed += async delegate
                    {
                        if (EraseYearWindowDialog.DialogResult == true)
                        {
                            CloseDockItem(TabControls.InventoryJournalLines, selectedItem);
                            PostingAPI postApi = new PostingAPI(api);
                            var res = await postApi.DeleteJournalLines(selectedItem);
                            if (res != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(res);
                        }
                    };
                    EraseYearWindowDialog.Show();
                    break;
                case "ImportData":
                    if (selectedItem == null)
                        return;
                    OpenImportDataPage(selectedItem);
                    break;
                case "InvJournalPosted":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.InventoryPostedJournals, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void OpenImportDataPage(InvJournalClient selectedItem)
        {
            var invJournalLine = new InvJournalLineClient();
            string header = selectedItem.Journal;
            UnicontaBaseEntity[] baseEntityArray = new UnicontaBaseEntity[2] { invJournalLine, selectedItem };
            object[] param = new object[2];
            param[0] = baseEntityArray;
            param[1] = header;
            AddDockItem(TabControls.ImportPage, param, string.Format("{0} : {1}", Localization.lookup("Import"), header));
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
