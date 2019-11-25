using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.Client.Pages;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.Threading.Tasks;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectTransactionGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTransClient); } }
        public override bool SingleBufferUpdate { get { return false; } } // we need two buffers to update project number in trans
    }
    public partial class ProjectTransactionPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectTransactionPage; } }

        ItemBase iIncludeSubProBase;
        static bool includeSubProject;
        UnicontaBaseEntity master;
        protected override Filter[] DefaultFilters()
        {
            if (dgProjectTransaction.masterRecords != null)
                return null;
            Filter dateFilter = new Filter();
            dateFilter.name = "Date";
            dateFilter.value = String.Format("{0:d}..", BasePage.GetSystemDefaultDate().AddYears(-1).Date);
            return new Filter[] { dateFilter };
        }

        public ProjectTransactionPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitializePage(null);
        }

        public ProjectTransactionPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitializePage(master);
        }

        public ProjectTransactionPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitializePage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectTransaction.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string header;
            var syncMaster = dgProjectTransaction.masterRecord as Uniconta.DataModel.Project;
            if (syncMaster != null)
                header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), syncMaster._Number);
            else
            {
                var syncMaster2 = dgProjectTransaction.masterRecord as Uniconta.DataModel.PrJournalPosted;
                if (syncMaster2 != null)
                    header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), syncMaster2.RowId);
                else
                    return;
            }
            SetHeader(header);
        }

        void InitializePage(UnicontaBaseEntity _master)
        {
            this.DataContext = this;
            master = _master;
            SetRibbonControl(localMenu, dgProjectTransaction);
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (master != null)
            {
                dgProjectTransaction.UpdateMaster(master);
                ribbonControl.DisableButtons("Save");
            }
            else
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "EditAll", "Save" });

            if (master is Uniconta.DataModel.Project)
                iIncludeSubProBase = UtilDisplay.GetMenuCommandByName(rb, "InclSubProjects");
            else
                UtilDisplay.RemoveMenuCommand(rb, "InclSubProjects");

            dgProjectTransaction.api = api;
            dgProjectTransaction.BusyIndicator = busyIndicator;
            dgProjectTransaction.ShowTotalSummary();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            localMenu.OnChecked += LocalMenu_OnChecked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var master = dgProjectTransaction.masterRecords?.First();
            this.ProjectCol.Visible = !(master is Uniconta.DataModel.Project);

            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            dgProjectTransaction.Readonly = true;
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = false;
            return true;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectTransaction.SelectedItem as ProjectTransClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgProjectTransaction.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgProjectTransaction.syncEntity, vheader);
                    }
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgProjectTransaction.syncEntity, api, busyIndicator);
                    break;
                case "InvTransactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvTransactions, dgProjectTransaction.syncEntity);
                    break;
                case "Save":
                    SaveGrid();
                    break;
                case "RefreshGrid":
                    if (dgProjectTransaction.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgProjectTransaction);
                    else
                        InitQuery();
                    break;
                case "Filter":
                    if (dgProjectTransaction.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgProjectTransaction);
                    gridRibbon_BaseActions(ActionType);
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async Task LoadGrid()
        {
            List<PropValuePair> filter = new List<PropValuePair>();
            if (includeSubProject)
            {
                var propValuePairFolder = PropValuePair.GenereteParameter("IncludeSubProject", typeof(string), "1");
                filter.Add(propValuePairFolder);
            }
            await dgProjectTransaction.Filter(filter);
        }

        public override Task InitQuery()
        {
            if (master is Uniconta.DataModel.Project)
                return ShowInculdeSubProject();
            else
                return base.InitQuery();
        }

        private void LocalMenu_OnChecked(string actionType, bool IsChecked)
        {
            includeSubProject = IsChecked;
            ShowInculdeSubProject();
        }

        Task ShowInculdeSubProject()
        {
            iIncludeSubProBase.IsChecked = includeSubProject;
            return LoadGrid();
        }

        async private void JournalPosted(ProjectTransClient selectedItem)
        {
            var pairPostedJour = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements(nameof(ProjectJournalPostedClient.GLJournalPostedId), typeof(int), selectedItem._JournalPostedId.ToString())
            };

            var result = await api.Query<ProjectJournalPostedClient>(pairPostedJour);

            if (result != null && result.Length == 1)
            {
                CWProjPostedClientFormView cwPostedClient = new CWProjPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }
        private async void SaveGrid()
        {
            var err = await dgProjectTransaction.SaveData();
            if (err != 0)
                return;

            dgProjectTransaction.Readonly = true;
            dgProjectTransaction.tableView.CloseEditor();
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase != null)
            {
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                ribbonControl.DisableButtons( "Save" );
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked?false: dgProjectTransaction.HasUnsavedData;
            }
        }
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;

            if (dgProjectTransaction.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgProjectTransaction.MakeEditable();
                ProjectCol.AllowEditing = DevExpress.Utils.DefaultBoolean.True;
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons("Save");
                editAllChecked = false;
            }
            else
            {
                ProjectCol.AllowEditing = DevExpress.Utils.DefaultBoolean.False;
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                     {
                         if (confirmationDialog.DialogResult == null)
                             return;

                         switch (confirmationDialog.ConfirmationResult)
                         {
                             case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                 var err = await dgProjectTransaction.SaveData();
                                 if (err != 0)
                                 {
                                     api.AllowBackgroundCrud = true;
                                     return;
                                 }
                                 break;
                         }
                         editAllChecked = true;
                         dgProjectTransaction.Readonly = true;
                         dgProjectTransaction.tableView.CloseEditor();
                         ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                         ribbonControl.DisableButtons("Save" );
                     };
                    confirmationDialog.Show();
                }
                else
                {
                    dgProjectTransaction.Readonly = true;
                    dgProjectTransaction.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons( "Save" );
                }
            }
        }
    }
}
