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
using System.Windows.Shapes;
using Uniconta.API.Inventory;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CorasauDataGridInvPostedJournal : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvJournalPostedClient); } }
        public override IComparer GridSorting { get { return new InvJournalPostedClientSort(); } }
    }

    public partial class InventoryPostedJournals : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InventoryPostedJournals; } }

        protected override Filter[] DefaultFilters()
        {
            Filter postedFilter = new Filter();
            postedFilter.name = "Posted";
            postedFilter.value = BasePage.GetSystemDefaultDate().AddDays(-90).ToShortDateString() + "..";
            return new Filter[] { postedFilter };
        }
        public InventoryPostedJournals(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            dgInvPostedJournal.UpdateMaster(master);
            Init();
        }
        public InventoryPostedJournals(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            Init();
        }

        private void Init()
        {
            SetRibbonControl(localMenu, dgInvPostedJournal);
            dgInvPostedJournal.api = api;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgInvPostedJournal.BusyIndicator = busyIndicator;
            dgInvPostedJournal.RowDoubleClick += dgInvPostedJournal_RowDoubleClick;
        }

        private void dgInvPostedJournal_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("InvPostedTransaction");
        }
        private void Journal_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ribbonControl.PerformRibbonAction("InvPostedTransaction");
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            InvJournalPostedClient selectedItem = dgInvPostedJournal.SelectedItem as InvJournalPostedClient;
            string header;
            switch (ActionType)
            {
                case "PostedTransaction":
                    if (selectedItem != null && selectedItem._JournalPostedId != 0)
                    {
                        header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem._JournalPostedId);
                        AddDockItem(TabControls.PostedTransactions, selectedItem, header);
                    }
                    break;
                case "InvPostedTransaction":
                    if (selectedItem != null)
                    {
                        header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem.RowId);
                        AddDockItem(TabControls.InventoryTransactions, selectedItem, header);
                    }
                    break;
                case "Delete":
                    if (selectedItem != null)
                        DeleteJournal(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        private void DeleteJournal(InvJournalPostedClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var deleteDialog = new DeletePostedJournal();
            deleteDialog.Closed += async delegate
            {
                if (deleteDialog.DialogResult == true)
                {
                    PostingAPI pApi = new PostingAPI(api);
                    ErrorCodes res = await pApi.DeletePostedJournal(selectedItem, deleteDialog.Comment);
                    if (res == ErrorCodes.Succes)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("Journaldeleted"), selectedItem.RowId), Uniconta.ClientTools.Localization.lookup("Message"));
                        dgInvPostedJournal.UpdateItemSource(2, selectedItem);
                    }
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            deleteDialog.Show();
        }
    }
}
