using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
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
using Uniconta.ClientTools.Util;
using UnicontaClient.Pages.Maintenance;
using UnicontaAPI.Project.API;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectJournalGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectJournalClient); } }
    }
    public partial class ProjectJournalPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectJournalPage; } }
        public ProjectJournalPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public ProjectJournalPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgProjectJournal);
            dgProjectJournal.api = api;
            dgProjectJournal.BusyIndicator = busyIndicator;
            
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectJournal.RowDoubleClick += DgProjectJournal_RowDoubleClick;
        }

        private void DgProjectJournal_RowDoubleClick()
        {
            var selectedItem = dgProjectJournal.SelectedItem as ProjectJournalClient;
            if (selectedItem != null)
                AddDockItem(TabControls.ProjectJournalLinePage, selectedItem, string.Format("{0} {1} : {2}", Uniconta.ClientTools.Localization.lookup("Project"), Uniconta.ClientTools.Localization.lookup("Journal"), selectedItem.Journal));

        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.PrCategory), typeof(Uniconta.DataModel.Project), typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.EmpPayrollCategory), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.InvItem) });
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ProjectJournalPage2)
                dgProjectJournal.UpdateItemSource(argument);
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectJournal.SelectedItem as ProjectJournalClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.ProjectJournalPage2, api, string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Project"), Uniconta.ClientTools.Localization.lookup("Journal")), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectJournalPage2, selectedItem, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Journal"), selectedItem.Journal));
                    break;
                case "ProjectJournalLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectJournalLinePage, selectedItem, string.Format("{0} {1} : {2}", Uniconta.ClientTools.Localization.lookup("Project"), Uniconta.ClientTools.Localization.lookup("Journal"), selectedItem.Journal));
                    break;
                case "JournalPosted":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.ProjectPostedJournalsPage, selectedItem, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("PostedJournals"), selectedItem.Journal));
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
                            CloseDockItem(TabControls.ProjectJournalLinePage, selectedItem);
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void OpenImportDataPage(ProjectJournalClient selectedItem)
        {
            var projectJournalLine = new ProjectJournalLineClient();
            string header = selectedItem.Journal;
            UnicontaBaseEntity[] baseEntityArray = new UnicontaBaseEntity[2] { projectJournalLine, selectedItem };
            object[] param = new object[2];
            param[0] = baseEntityArray;
            param[1] = header;
            AddDockItem(TabControls.ImportPage, param, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Import"), header));
        }
    }
}

