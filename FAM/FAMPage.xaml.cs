using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class FAMGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(FamClient); } }
    }

    public partial class FAMPage : GridBasePage
    {
        public FAMPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        public FAMPage(BaseAPI api) : base(api, string.Empty)
        {
            InitPage(null);
        }

        public FAMPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            InitPage(null);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            LayoutControl = famDetailControl.layoutItems;
            dgFAMGrid.RowDoubleClick += dgFAMGrid_RowDoubleClick;
            dgFAMGrid.BusyIndicator = busyIndicator;
            dgFAMGrid.api = api;
            SetRibbonControl(localMenu, dgFAMGrid);
            dgFAMGrid.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            ribbonControl.DisableButtons(new string[] { "DeleteRow", "SaveGrid" });
            dgFAMGrid.ShowTotalSummary();
            //InsuranceAmount.HasDecimals = NonDepreciableAmount.HasDecimals = ResidualValue.HasDecimals = DecommissionCost.HasDecimals = false;
        }

        protected override void OnLayoutCtrlLoaded()
        {
            famDetailControl.api = api;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.FAMGroup) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgFAMGrid.SelectedItem as FamClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgFAMGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string hdr = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem._Asset);
                    AddDockItem(TabControls.FAMPage2, copyParam, hdr);
                    break;
                case "DeleteRow":
                    dgFAMGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    dgFAMGrid.SaveData();
                    break;
                case "AddRow":
                    AddDockItem(TabControls.FAMPage2, api, Uniconta.ClientTools.Localization.lookup("Asset"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem != null)
                    {
                        object[] EditParam = new object[2];
                        EditParam[0] = selectedItem;
                        EditParam[1] = true;
                        AddDockItem(TabControls.FAMPage2, EditParam, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Asset"), selectedItem._Asset));
                    }
                    break;
                case "SubAsset":
                    if (selectedItem != null)
                        AddDockItem(TabControls.FAMPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SubFAM"), selectedItem._Asset));
                    break;
                case "FAMTrans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.FAMTransGridPage, dgFAMGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Asset"), selectedItem._Asset));
                    break;
                case "RefreshGrid":
                    if (gridControl.Visibility == Visibility.Visible)
                        gridRibbon_BaseActions(ActionType);
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgFAMGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgFAMGrid.syncEntity);
                    break;
                case "GenerateDepreciation":
                    GenerateDepreciation();
                    break;
                case "FAMTransPrimo":
                    if (selectedItem != null)
                        AddDockItem(TabControls.FAMPrimoTransGridPage, selectedItem, string.Concat(Uniconta.ClientTools.Localization.lookup("PrimoTransactions"), " :",
                            Uniconta.ClientTools.Localization.lookup("AccountTypeFixedAsset"), "-", selectedItem._Asset));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void GenerateDepreciation()
        {
            var famLst = dgFAMGrid.GetVisibleRows() as IEnumerable<FAM>;
            if (famLst.Count() == 0)
                return;
            var cw = new CwGenerateDepreciation(api);
#if !SILVERLIGHT
            cw.DialogTableId = 2000000072;
#endif
            cw.Closed += async delegate
            {
                string journalName;
                if (cw.DialogResult == true)
                {
                    var postingApi = new Uniconta.API.GeneralLedger.PostingAPI(api);
                    journalName = cw.Journal;
                    var result = await postingApi.GenerateDepreciation(famLst, cw.Journal, cw.TransType, cw.PostingDate);
                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                    {
                        var text = string.Concat(Uniconta.ClientTools.Localization.lookup("TransferedToJournal"), ": ", journalName,
                                    Environment.NewLine, string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Journallines")), " ?");
                        var select = UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.YesNo);
                        if (select == MessageBoxResult.Yes)
                        {
                            var parms = new[] { new BasePage.ValuePair("Journal", journalName) };
                            var header = string.Concat(Uniconta.ClientTools.Localization.lookup("Journal"), ": ", journalName);
                            AddDockItem(TabControls.GL_DailyJournalLine, null, header, null, true, null, parms);
                        }
                    }
                }
            };
            cw.ShowDialog();
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgFAMGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgFAMGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgFAMGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "DeleteRow", "SaveGrid" });
                editAllChecked = false;
            }
            else
            {
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
                                var err = await dgFAMGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                break;
                        }
                        editAllChecked = true;
                        dgFAMGrid.Readonly = true;
                        dgFAMGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "DeleteRow", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgFAMGrid.Readonly = true;
                    dgFAMGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "DeleteRow", "SaveGrid" });
                }
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgFAMGrid.HasUnsavedData;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.FAMPage2)
                dgFAMGrid.UpdateItemSource(argument);
            if (dgFAMGrid.Visibility == Visibility.Collapsed)
            {
                //detailControl.Refresh(argument, dgFAMGrid.SelectedItem);
            }
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgFAMGrid.UpdateItemSource(argument);
        }

        private void dgFAMGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("FAMTrans");
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var FamAccount = (sender as Image).Tag as FamClient;
            if (FamAccount != null)
                AddDockItem(TabControls.UserDocsPage, dgFAMGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var FamAccount = (sender as Image).Tag as FamClient;
            if (FamAccount != null)
                AddDockItem(TabControls.UserNotesPage, dgFAMGrid.syncEntity);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
