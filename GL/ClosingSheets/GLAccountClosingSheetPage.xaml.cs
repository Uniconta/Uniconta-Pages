using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using System.Collections.ObjectModel;
using DevExpress.Xpf.Grid.LookUp;
using DevExpress.Xpf.Editors.Settings;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System.Threading.Tasks;
using System.Collections;
using DevExpress.Xpf.Grid;
using Uniconta.API.Service;
using Uniconta.API.GeneralLedger;
using System.Windows.Threading;
using System.Text;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLAccountClosingSheetGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLAccountClosingSheetClient); } }
    }
    public partial class GLAccountClosingSheetPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLAccountClosingSheetPage; } }
        GLClosingSheet masterRecord;
        SQLCache AccountListCache, LedgerCache;
        PostingAPI postingApi;
        ReportAPI rApi;

        public GLAccountClosingSheetPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            this.DataContext = this;
            masterRecord = master as GLClosingSheet;
            postingApi = new PostingAPI(api);
            rApi = new Uniconta.API.GeneralLedger.ReportAPI(this.api);
            SetRibbonControl(localMenu, dgGLTable);
            dgGLTable.api = api;
            localMenu.dataGrid = dgGLTable;
            dgGLTable.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGLTable.RowDoubleClick += dgGLTable_RowDoubleClick;
            dgGLTable.BusyIndicator = busyIndicator;
            dgGLTable.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            ((TableView)dgGLTable.View).RowStyle = Application.Current.Resources["RowStyle"] as Style;
        }

        public async override Task InitQuery()
        {
             await BindGrid();
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as GLAccountClosingSheetClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            var selectedItem = e.NewItem as GLAccountClosingSheetClient;
            if (selectedItem != null)
            {
                this.prevReconciled = selectedItem._Reconciled;
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
        }

        DateTime prevReconciled;
        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as GLAccountClosingSheetClient;
            switch (e.PropertyName)
            {
                case "IsReconciled":
                    if (rec._Reconciled != this.prevReconciled)
                    {
                        var GlobalRec = (GLAccount)LedgerCache.Get(rec.RowId);
                        var old = (GLAccount)StreamingManager.Clone(GlobalRec);
                        GlobalRec._Reconciled = rec._Reconciled;
                        api.UpdateNoResponse(old, rec);
                    }
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.ClosingSheetLinesAll)
            {
                BindGrid();
                mainList = null;
                ClosingSheetHasLinesList = null;
            }
            else if (screenName == TabControls.GLAccountPage2)
            {
                var args = argument as object[];
                var entity = args[1] as GLAccountClient;
                if (entity != null)
                {
                    var closingSheetAccount = new GLAccountClosingSheetClient();
                    StreamingManager.Copy(entity, closingSheetAccount);
                    args[1] = closingSheetAccount;
                    dgGLTable.UpdateItemSource(args);
                }
            }
            else if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgGLTable.UpdateItemSource(argument);
        }

        ItemBase ibase;
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);

        }

        void dgGLTable_RowDoubleClick()
        {
            localMenu_OnItemClicked("ClosingSheetLines");
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            GLAccountClosingSheetClient selectedItem = dgGLTable.SelectedItem as GLAccountClosingSheetClient;
            switch (ActionType)
            {
                case "ClosingSheetLines":
                    if (selectedItem == null || !selectedItem._PostingAccount)
                        return;
                    object[] masters = new object[] { masterRecord, dgGLTable.syncEntity, this.AccountListCache };
                    AddDockItem(TabControls.ClosingSheetLines, masters, string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ClosingSheetLines"), selectedItem._Account, masterRecord._Name));
                    break;
                case "ClosingSheetLinesAll":
                    object[] masters2 = new object[] { masterRecord };
                    AddDockItem(TabControls.ClosingSheetLinesAll, masters2, string.Format("{0}: {1}", masterRecord._Name, Uniconta.ClientTools.Localization.lookup("AllLines")));
                    break;
                case "ClosingSheetHasLines":
                    ClosingSheetHasLines();
                    break;
                case "PostClosingSheet":
                    if (selectedItem != null)
                        PostClosingSheet();
                    break;
                case "DeleteClosingSheetLine":
                    if (selectedItem != null)
                        DeleteClosingSheetLine();
                    break;
                case "RefreshGrid":
                    RefreshLines();
                    break;
                case "AccountStatement":
                    AddDockItem(TabControls.AccountsStatementReport, masterRecord, Uniconta.ClientTools.Localization.lookup("AccountsStatement"));
                    break;
                case "ReportCriteria":
                    AddDockItem(TabControls.ReportCriteria, masterRecord, string.Format("{0} - {1}", Uniconta.ClientTools.Localization.lookup("ReportCriteria"), masterRecord._Name));
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgGLTable.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgGLTable.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "AllNotes":
                    object[] masters3 = new object[] { api, this.AccountListCache };
                    AddDockItem(TabControls.AllNotesPage, masters3, Uniconta.ClientTools.Localization.lookup("Notes"));
                    break;
                case "AddItem":
                    AddDockItem(TabControls.GLAccountPage2, api, Uniconta.ClientTools.Localization.lookup("Accounts"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditItem":
                    if (selectedItem == null)
                        return;
                    var param = new object[2] { selectedItem, true };
                    AddDockItem(TabControls.GLAccountPage2, param, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Accounts"), selectedItem.Account));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        IEnumerable<GLAccountClosingSheetClient> ClosingSheetHasLinesList;
        List<GLAccountClosingSheetClient> mainList;
        void ClosingSheetHasLines()
        {
            if (mainList == null)
            {
                mainList = new List<GLAccountClosingSheetClient>();
                foreach (var row in dgGLTable.ItemsSource as IEnumerable<GLAccountClosingSheetClient>)
                    mainList.Add(row);
            }

            if (ClosingSheetHasLinesList == null)
            {
                var dgList = dgGLTable.ItemsSource as  IEnumerable<GLAccountClosingSheetClient>;
                if (dgList != null)
                {
                    ClosingSheetHasLinesList = dgList.Where(x => x.IsEmpty == false);
                    dgGLTable.ItemsSource = ClosingSheetHasLinesList;
                }
            }
            else
            {
                dgGLTable.ItemsSource = mainList;
                ClosingSheetHasLinesList = null;
            }
        }

        void DeleteClosingSheetLine()
        {
            CWPostClosingSheet postingDialog = new CWPostClosingSheet(masterRecord._ToDate, true);
            postingDialog.Closed += async delegate
            {
                if (postingDialog.DialogResult == true)
                {
                    if (UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), Uniconta.ClientTools.Localization.lookup("ClosingSheetLines")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                        return;

                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("BusyMessage");
                    busyIndicator.IsBusy = true;
                    var deleteResult = await postingApi.DeleteJournalLines(masterRecord, postingDialog.Code);
                    if (deleteResult != ErrorCodes.Succes)
                    {
                        busyIndicator.IsBusy = false;
                        UtilDisplay.ShowErrorCode(deleteResult);
                    }
                    else
                        BindGrid();
                }
            };
            postingDialog.Show();
        }

        void PostClosingSheet()
        {
            CWPostClosingSheet postingDialog = new CWPostClosingSheet(masterRecord._ToDate);
#if !SILVERLIGHT
            postingDialog.DialogTableId = 2000000017;
#endif
            postingDialog.Closed += async delegate
            {
                if (postingDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    var postingResult = await postingApi.PostClosingSheet(masterRecord, postingDialog.Code, postingDialog.IsSimulation, postingDialog.PostedDate, postingDialog.comments, 0, new GLTransClientTotal());
                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                    if (postingResult == null)
                        return;
                    if (postingResult.SimulatedTrans != null && postingResult.SimulatedTrans.Count() > 0)
                    {
                        AddDockItem(TabControls.SimulatedTransactions, postingResult.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"));
                        this.UpdateLines(postingResult.ClosingLines, postingResult.SimulatedTrans);
                    }
                    else if (postingResult.Err == ErrorCodes.Succes)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ClosingSheetHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("Message"));
                        BindGrid();
                    }
                    else
                        Utility.ShowJournalError(postingResult, dgGLTable);
                }
            };
            postingDialog.Show();
        }

        async void RefreshLines()
        {
            busyIndicator.IsBusy = true;
            var postingResult = await postingApi.PostClosingSheet(masterRecord, null, true, masterRecord._ToDate, null, 1, null);

            GLTrans[] PostedLines;
            var ClosingLines = postingResult?.ClosingLines;
            if (ClosingLines != null)
                PostedLines = postingResult.SimulatedTrans;
            else
            {
                ClosingLines = await api.Query<GLClosingSheetLineLocal>(dgGLTable.masterRecords, null);
                PostedLines = null;
            }
            busyIndicator.IsBusy = false;

            UpdateLines(ClosingLines, PostedLines);

            var selectedItem = dgGLTable.SelectedItem;

            dgGLTable.ItemsSource = null;
            dgGLTable.ItemsSource = this.AccountListCache.GetKeyStrRecords;

            if (selectedItem != null)
                dgGLTable.SelectedItem = selectedItem;
        }

        void AddLineToAccount(string account, GLClosingSheetLineClient lin, int factor)
        {
            var ac = (GLAccountClosingSheetClient)AccountListCache.Get(account);
            if (ac != null)
            {
                ac._TmpChange += lin._AmountCent * factor;
                if (ac.Lines != null)
                    ((List<GLClosingSheetLineClient>)ac.Lines).Add(lin);
                else
                    ac.Lines = new List<GLClosingSheetLineClient>() { lin };
            }
        }

        void UpdateLines(GLClosingSheetLine[] ClosingLines, GLTrans[] PostedLines)
        {
            if (ClosingLines == null)
                return;

            var AccountListCache = this.AccountListCache;
            var ReconciledDate = masterRecord._ToDate;

            foreach (var ac in (GLAccountClosingSheetClient[])AccountListCache.GetNotNullArray)
            {
                ac._TmpChange = 0;
                ac.Lines = null;
                if (ac.ReconciledDate == DateTime.MinValue)
                    ac.ReconciledDate = ReconciledDate;
            }

            int factor = (PostedLines != null) ? 0 : 1;

            foreach (var _lin in ClosingLines)
            {
                var lin = _lin as GLClosingSheetLineClient;
                if (lin == null)
                {
                    lin = new GLClosingSheetLineLocal();
                    StreamingManager.Copy(_lin, lin);
                }

                AddLineToAccount(lin._Account, lin, factor);

                if (lin._ShowOnAccount != null && lin._ShowOnAccount != lin._Account)
                    AddLineToAccount(lin._ShowOnAccount, lin, factor);

                if (lin._OffsetAccount != null)
                    AddLineToAccount(lin._OffsetAccount, lin, -factor);
            }

            if (PostedLines != null)
            {
                foreach (var lin in PostedLines)
                {
                    var ac = (GLAccountClosingSheetClient)AccountListCache.Get(lin._Account);
                    if (ac != null)
                        ac._TmpChange += lin._AmountCent;
                }
            }

            var SumCol = new List<FinancialBalance>();
            foreach (var ac in (GLAccountClosingSheetClient[])AccountListCache.GetNotNullArray)
            {
                if (ac._TmpChange != 0)
                {
                    var bal = new FinancialBalance();
                    bal.AccountRowId = ac.RowId;
                    bal._Debit = ac._TmpChange;
                    bal._Credit = 0;
                    SumCol.Add(bal);
                }
            }

            var BalanceChange = SumCol.ToArray();
            var balance = rApi.Format(BalanceChange, LedgerCache);
            foreach (var bal in balance)
            {
                var ac = (GLAccountClosingSheetClient)AccountListCache.Get(bal.Account.RowId);
                ac?.setChange(bal.SumAll());
            }

            SumCol.Clear();
            foreach (var ac in (GLAccountClosingSheetClient[])AccountListCache.GetNotNullArray)
            {
                if ((ac._OrgBalance != 0 || ac._Change != 0) && ac._PostingAccount)
                {
                    var bal = new FinancialBalance();
                    bal.AccountRowId = ac.RowId;
                    bal._Debit = ac._OrgBalance + ac._Change;
                    bal._Credit = 0;
                    SumCol.Add(bal);
                }
            }

            var NewBalance = SumCol.ToArray();
            balance = rApi.Format(NewBalance, LedgerCache);
            foreach (var bal in balance)
            {
                var ac = (GLAccountClosingSheetClient)AccountListCache.Get(bal.Account.RowId);
                ac?.setNewBalance(bal.SumAll());
            }
        }

        async Task BindGrid(bool ForceLoadAcc = false)
        {
            busyIndicator.IsBusy = true;

            var rApi = this.rApi;
            var api = this.api;
            LedgerCache = api.CompanyEntity.GetCache(typeof(GLAccount)) ?? await api.CompanyEntity.LoadCache(typeof(GLAccount), api);

            var masterRecord = this.masterRecord;
            var numbers = await rApi.GenerateTotals(masterRecord._FromDate, masterRecord._ToDate);
            if (numbers == null || LedgerCache == null)
                return;

            BalanceHeader[] balance = rApi.Format(numbers, LedgerCache);

            var LocalAccounts = new List<GLAccountClosingSheetClient>();
            var FromAccount = masterRecord._FromAccount;
            var ToAccount = masterRecord._ToAccount;

            foreach (var bal in balance)
            {
                var ac = bal.Account;
                if (FromAccount != null && string.Compare(ac._Account, FromAccount, StringComparison.CurrentCultureIgnoreCase) < 0)
                    continue;
                if (ToAccount != null && string.Compare(ac._Account, ToAccount, StringComparison.CurrentCultureIgnoreCase) > 0)
                    continue;

                var ac2 = new GLAccountClosingSheetClient();
                StreamingManager.Copy(ac, ac2);

                ac2._OrgBalance = bal.SumAll();
                ac2._LastPeriod = 0;
                LocalAccounts.Add(ac2);
            }

            this.AccountListCache = new SQLCache(LocalAccounts.ToArray(), true);

            if (masterRecord._FromDate2 != DateTime.MinValue)
            {
                numbers = null;
                balance = null;
                numbers = await rApi.GenerateTotals(masterRecord._FromDate2, masterRecord._ToDate2);
                if (numbers != null)
                {
                    balance = rApi.Format(numbers, LedgerCache);
                    foreach (var bal in balance)
                    {
                        var ac = (GLAccountClosingSheetClient)AccountListCache.Get(bal.Account.RowId);
                        if (ac != null)
                            ac._LastPeriod = bal.SumAll();
                    }
                }
            }
            numbers = null;
            balance = null;

            RefreshLines();

            dgGLTable.SelectRange(0, 0);
            dgGLTable.Visibility = Visibility.Visible;
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var closingSheet = (sender as Image).Tag as GLAccountClosingSheetClient;
            if (closingSheet != null)
                AddDockItem(TabControls.UserDocsPage, dgGLTable.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var closingSheet = (sender as Image).Tag as GLAccountClosingSheetClient;
            if (closingSheet != null)
                AddDockItem(TabControls.UserNotesPage, dgGLTable.syncEntity);
        }
    }
}
