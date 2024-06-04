using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.ClientTools.Util;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System.Threading.Tasks;
using DevExpress.Xpf.Grid;
using Uniconta.API.GeneralLedger;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLAccountClosingSheetGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLAccountClosingSheetClient); } }

        public override string SetColumnTooltip(object row, ColumnBase col)
        {
            var tran = row as GLAccountClosingSheetClient;
            if (tran != null && col != null)
            {
                switch (col.FieldName)
                {
                    case "SheetNote": return tran.SheetNote ?? "";
                }
            }
            return base.SetColumnTooltip(row, col);
        }
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
            showAll = true;
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
            GetMenuItem();

        }

        static int selectedCode;
        string[] code;
        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var comboItem = UtilDisplay.GetMenuCommandByName(rb, "Code");
            if (comboItem != null)
            {
                code = new string[11];
                code[0] = Uniconta.ClientTools.Localization.lookup("All");
                for (int i = 0; (i < 10); i++)
                    code[i + 1] = NumberConvert.ToString(i);
                comboItem.ComboBoxItemSource = code;
                comboItem.SelectedItem = code[selectedCode];
                localMenu.OnSelectedIndexChanged += LocalMenu_OnSelectedIndexChanged;
            }
        }

        private void LocalMenu_OnSelectedIndexChanged(string ActionType, string SelectedItem)
        {
            selectedCode = Array.IndexOf(code, SelectedItem);
            BindGrid();
        }

        public override Task InitQuery()
        {
            var t = BindGrid();
            LoadNotes(t);
            return t;
        }

        async void LoadNotes(Task t)
        {
            var notes = await api.Query<Note>(masterRecord);
            if (notes != null && notes.Length > 0)
            {
                await t;
                var cache = this.AccountListCache;
                if (cache != null)
                {
                    foreach (var n in notes)
                    {
                        if (n._Text != null)
                        {
                            var i = n._Text.IndexOf(':');
                            if (i > 0)
                            {
                                var Acc = (GLAccountClosingSheetClient)cache.Get(n._Text.Substring(0, i));
                                if (Acc != null)
                                {
                                    Acc._Note = n;
                                    Acc.SheetNote = n._Text.Substring(i + 2);
                                }
                            }
                        }
                    }
                }
            }
        }

        void SaveNotes()
        {
            var arr = this.AccountListCache?.GetNotNullArray as GLAccountClosingSheetClient[];
            if (arr != null)
            {
                var ins = new List<Note>();
                var del = new List<Note>();
                var upd = new List<Note>();
                foreach (var Acc in arr)
                {
                    if (Acc._Note != null || Acc._SheetNote != null)
                    {
                        if (Acc._Note == null)
                        {
                            var n = new Note() { _Text = Acc._Account + ": " + Acc._SheetNote };
                            n.SetMaster(masterRecord);
                            ins.Add(n);
                        }
                        else if (string.IsNullOrWhiteSpace(Acc._SheetNote))
                            del.Add(Acc._Note);
                        else
                        {
                            if (string.Compare(Acc._SheetNote, 0, Acc._Note._Text, Acc._Account.Length + 2, 4000) != 0)
                            {
                                Acc._Note._Text = Acc._Account + ": " + Acc._SheetNote;
                                upd.Add(Acc._Note);
                            }
                        }
                    }
                }
                if (ins.Count > 0 || del.Count > 0 || upd.Count > 0)
                {
                    api.MultiCrud(ins, upd, del);
                    if (ins.Count > 0)
                    {
                        masterRecord.HasNotes = true;
                        masterRecord.GetType().GetMethod("NotifyPropertyChanged")?.Invoke(masterRecord, new object[] { "HasNotes" });
                    }
                }
            }
        }

        public override void PageClosing()
        {
            dgGLTable.tableView.PostEditor();
            SaveNotes();
            base.PageClosing();
        }
        public override bool IsDataChaged => false;
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

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        void dgGLTable_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("ClosingSheetLines");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            dgGLTable_RowDoubleClick();
        }
        void localMenu_OnItemClicked(string ActionType)
        {
            GLAccountClosingSheetClient selectedItem = dgGLTable.SelectedItem as GLAccountClosingSheetClient;
            switch (ActionType)
            {
                case "ClosingSheetLines":
                    if (selectedItem != null && selectedItem._PostingAccount)
                        AddDockItem(TabControls.ClosingSheetLines, new object[] { masterRecord, dgGLTable.syncEntity, this.AccountListCache }, string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("ClosingSheetLines"), selectedItem._Account, masterRecord._Name));
                    break;
                case "ClosingSheetLinesAll":
                    AddDockItem(TabControls.ClosingSheetLinesAll, new object[] { masterRecord }, string.Format("{0}: {1}", masterRecord._Name, Uniconta.ClientTools.Localization.lookup("AllLines")));
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
                    AddDockItem(TabControls.AllNotesPage, new object[] { api, this.AccountListCache }, Uniconta.ClientTools.Localization.lookup("Notes"));
                    break;
                case "AddItem":
                    AddDockItem(TabControls.GLAccountPage2, api, Uniconta.ClientTools.Localization.lookup("Accounts"), "Add_16x16");
                    break;
                case "EditItem":
                    if (selectedItem != null)
                        AddDockItem(TabControls.GLAccountPage2, new object[2] { selectedItem, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Accounts"), selectedItem.Account));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        IEnumerable<GLAccountClosingSheetClient> ClosingSheetHasLinesList;
        List<GLAccountClosingSheetClient> mainList;
        bool showAll;
        void ClosingSheetHasLines()
        {
            if (mainList == null)
            {
                var lst = dgGLTable.ItemsSource as IEnumerable<GLAccountClosingSheetClient>;
                mainList = new List<GLAccountClosingSheetClient>(lst.Count());
                foreach (var row in lst)
                    mainList.Add(row);
            }

            if (ClosingSheetHasLinesList == null)
            {
                var dgList = dgGLTable.ItemsSource as IEnumerable<GLAccountClosingSheetClient>;
                if (dgList != null)
                {
                    ClosingSheetHasLinesList = dgList.Where(x => x.IsEmpty == false);
                    dgGLTable.ItemsSource = ClosingSheetHasLinesList;
                    showAll = false;
                }
            }
            else
            {
                dgGLTable.ItemsSource = mainList;
                ClosingSheetHasLinesList = null;
                showAll = true;
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
            postingDialog.DialogTableId = 2000000017;
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
                    if (postingResult.SimulatedTrans != null && postingResult.SimulatedTrans.Length > 0)
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
            var postingResult = await postingApi.PostClosingSheet(masterRecord, selectedCode > 0 ? (byte?)(selectedCode - 1) : (byte?)null, true, masterRecord._ToDate, null, 1, null);

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

            var arr = (GLAccountClosingSheetClient[])this.AccountListCache.GetKeyStrRecords;

            if (!showAll)
                arr = arr.Where(x => x.IsEmpty == false).ToArray();

            if (masterRecord._Skip0Accounts)
            {
                var lst = new List<GLAccountClosingSheetClient>(arr.Length);
                for (int i = 0; (i < arr.Length); i++)
                {
                    var rec = arr[i];
                    if (rec._AccountType < (byte)GLAccountTypes.PL || rec._TmpChange != 0 || rec._Change != 0 || rec._NewBalance != 0 || rec._OrgBalance != 0 || rec._LastPeriod != 0)
                        lst.Add(rec);
                }
                dgGLTable.ItemsSource = lst.ToArray();

            }
            else
                dgGLTable.ItemsSource = arr;

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
            int code = selectedCode - 1;

            foreach (var _lin in ClosingLines)
            {
                if (code >= 0 && code != _lin._Code)
                    continue;

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

        async Task BindGrid()
        {
            busyIndicator.IsBusy = true;

            var rApi = this.rApi;
            var api = this.api;
            LedgerCache = api.GetCache(typeof(GLAccount)) ?? await api.LoadCache(typeof(GLAccount));

            var masterRecord = this.masterRecord;
            var numbers = await rApi.GenerateTotals(masterRecord._FromDate, masterRecord._ToDate);
            if (numbers == null || LedgerCache == null)
                return;

            BalanceHeader[] balance = rApi.Format(numbers, LedgerCache);

            var LocalAccounts = new List<GLAccountClosingSheetClient>(balance.Length);
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
                ac2.Copy(ac);

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
