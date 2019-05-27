using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
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
using Uniconta.ClientTools;
using System.Collections;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectPostedJournalGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectJournalPostedClient); } }
        public override IComparer GridSorting { get { return new ProjectJournalPostedClientSort(); } }
    }  
    public partial class ProjectPostedJournalsPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.PostedJournals; } }

        protected override Filter[] DefaultFilters()
        {
            Filter postedFilter = new Filter();
            postedFilter.name = "Posted";
            postedFilter.value = BasePage.GetSystemDefaultDate().AddDays(-90).ToShortDateString() + "..";
            return new Filter[] { postedFilter };
        }
        public ProjectPostedJournalsPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            dgProjectPostedJournal.UpdateMaster(master);
            Init();
        }
        public ProjectPostedJournalsPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            Init();
        }
        private void Init()
        {
            localMenu.dataGrid = dgProjectPostedJournal;
            SetRibbonControl(localMenu, dgProjectPostedJournal);
            dgProjectPostedJournal.api = api;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectPostedJournal.BusyIndicator = busyIndicator;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectPostedJournal.SelectedItem as ProjectJournalPostedClient;
            string header;
            switch (ActionType)
            {
                case "PostedTransaction":
                    if (selectedItem != null && selectedItem._GLJournalPostedId != 0)
                    {
                        header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem._GLJournalPostedId);
                        AddDockItem(TabControls.PostedTransactions, selectedItem, header);
                    }
                    break;
                case "InvPostedTransaction":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTransactionPage, dgProjectPostedJournal.syncEntity, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem.RowId));
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

        private void DeleteJournal(ProjectJournalPostedClient selectedItem)
        {
            if (selectedItem == null)
                return;
            var deleteDialog = new DeletePostedJournal();
            deleteDialog.Closed += async delegate
            {
                if (deleteDialog.DialogResult == true)
                {
                    var pApi = new UnicontaAPI.Project.API.PostingAPI(api);
                    ErrorCodes res = await pApi.DeletePostedJournal(selectedItem, deleteDialog.Comment);
                    if (res == ErrorCodes.Succes)
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("Journaldeleted"), selectedItem.RowId), Uniconta.ClientTools.Localization.lookup("Message"));
                        dgProjectPostedJournal.UpdateItemSource(2, selectedItem);
                    }
                    else
                        UtilDisplay.ShowErrorCode(res);
                }
            };
            deleteDialog.Show();
        }
    }
}