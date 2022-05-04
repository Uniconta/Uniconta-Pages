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

            InsuranceAmount.HasDecimals =
            NonDepreciableAmount.HasDecimals =
            ResidualValue.HasDecimals =
            DecommissionCost.HasDecimals =
            ReversedDepreciation.HasDecimals =
            WriteUp.HasDecimals =
            WriteOff.HasDecimals =
            WriteDown.HasDecimals =
            Sale.HasDecimals =
            CostValue.HasDecimals =
            ReversedAcquisition.HasDecimals =
            BookedValue.HasDecimals =
            ManualDepreciation.HasDecimals = api.CompanyEntity.HasDecimals;

            dgFAMGrid.ShowTotalSummary();
        }

        protected override void OnLayoutCtrlLoaded()
        {
            famDetailControl.api = api;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.FAMGroup), typeof(Uniconta.DataModel.Employee) });
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
                    if (selectedItem != null)
                    {
                        AddDockItem(TabControls.FAMPage2, new object[2] { selectedItem, false },
                            string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem._Asset));
                    }
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
                        AddDockItem(TabControls.FAMPage2, new object[2] { selectedItem, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Asset"), selectedItem._Asset));
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
                case "SaleOfAsset":
                    if (selectedItem != null)
                        SaleOfAsset(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        static Uniconta.DataModel.GLDailyJournalLine CreateLine(Uniconta.DataModel.GLDailyJournalLine org)
        {
            var to = new Uniconta.DataModel.GLDailyJournalLine();
            to._Date = org._Date;
            to._Text = org._Text;
            to._Asset = org._Asset;
            return to;
        }

        void SaleOfAsset(FamClient Asset)
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.GLAccount) });

            var cw = new CwSaleOfAsset(api);
            cw.DialogTableId = 2000000088;
            cw.Closed += async delegate
            {
                if (cw.DialogResult == true)
                {
                    SQLCache Cache = api.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
                    var jour = (Uniconta.DataModel.GLDailyJournal)Cache.Get(cw.Journal);
                    if (jour != null)
                    {
                        Cache = api.GetCache(typeof(Uniconta.DataModel.FAMGroup));
                        var grp = (Uniconta.DataModel.FAMGroup)Cache.Get(Asset._Group);

                        Cache = api.GetCache(typeof(Uniconta.DataModel.GLAccount));

                        var lst = new List<Uniconta.DataModel.GLDailyJournalLine>(4);
                        var line = new Uniconta.DataModel.GLDailyJournalLine();
                        line.SetMaster(jour);
                        line._Date = cw.Date != DateTime.MinValue ? cw.Date : BasePage.GetSystemDefaultDate();
                        line._Text = cw.Text;
                        line._Asset = Asset._Asset;
                        line._AssetPostType = FAMTransCodes.ReversedDepreciation;
                        line._Debit = - Asset._Depreciation;
                        line._Account = grp._DepreciationAccount;
                        lst.Add(line);

                        line = CreateLine(line);
                        line.SetMaster(jour);
                        line._AssetPostType = FAMTransCodes.ReversedAcquisition;
                        line._Credit = Asset._CostValue;
                        line._Account = grp._SalesAccount;
                        lst.Add(line);

                        line = CreateLine(line);
                        line.SetMaster(jour);
                        line._AssetPostType = FAMTransCodes.WriteOff;
                        var val = cw.Amount - Asset.BookedValue;
                        if (val > 0)
                            line._Credit = val;
                        else
                            line._Debit = -val;
                        line._Account = grp._WriteOffOffset;
                        lst.Add(line);

                        line = CreateLine(line);
                        line.SetMaster(jour);
                        line._AssetPostType = FAMTransCodes.Sale;
                        line._Debit = cw.Amount;
                        line._Account = cw.SalesAccount;
                        lst.Add(line);

                        ErrorCodes errorCode = await api.Insert(lst);
                        if (errorCode != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(errorCode);
                        else
                        {
                            jour._NumberOfLines += 4;
                            var text = string.Concat(Uniconta.ClientTools.Localization.lookup("GenerateJournalLines"), "; ", Uniconta.ClientTools.Localization.lookup("Completed"),
                                Environment.NewLine, string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Journallines")), " ?");
                            var select = UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel);
                            if (select == MessageBoxResult.OK)
                                AddDockItem(TabControls.GL_DailyJournalLine, jour, null, null, true);
                        }
                    }
                }
            }; 
            cw.ShowDialog();
        }

        void GenerateDepreciation()
        {
            LoadType(typeof(Uniconta.DataModel.GLDailyJournal));

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
                            var header = string.Concat(Uniconta.ClientTools.Localization.lookup("Journal"), ": ", journalName);
                            AddDockItem(TabControls.GL_DailyJournalLine, null, header, null, true, null, new[] { new BasePage.ValuePair("Journal", journalName) });
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
