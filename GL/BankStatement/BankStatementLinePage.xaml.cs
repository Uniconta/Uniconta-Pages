using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
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
using System.Windows.Threading;
using Uniconta.ClientTools.Util;

using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls.Dialogs;
using DevExpress.Xpf.Grid;
using System.ComponentModel;
using UnicontaClient.Pages;
using Uniconta.Common.Utility;
using Uniconta.API.Service;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class BankStatementLineGrid : CorasauDataGridClient
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            tableView.RowStyle = System.Windows.Application.Current.Resources["MatchingRowStyle"] as System.Windows.Style;
        }
        public override Type TableType { get { return typeof(BankStatementLineGridClient); } }
        public override IComparer GridSorting { get { return new BankStatementLineSort(); } }
        public override bool Readonly { get { return false; } }
        public override bool CanInsert { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool IsAutoSave { get { return true; } }
        protected override bool RenderAllColumns { get { return true; } }
    }

    public class TransGrid : CorasauDataGridClient
    {
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            tableView.RowStyle = System.Windows.Application.Current.Resources["MatchingRowStyle"] as System.Windows.Style;
        }
        public override bool Readonly { get { return false; } }
        public override bool CanInsert { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool AllowSave { get { return false; } }
        public override Type TableType { get { return typeof(GLTransClientTotalBank); } }
    }

    public partial class BankStatementLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.BankStatementLinePage; } }
        static public DateTime fromDate;
        static public DateTime toDate;

        Uniconta.API.GeneralLedger.ReportAPI tranApi;
        BankStatementAPI bankTransApi;
        bool ShowCurrency;
        Uniconta.DataModel.BankStatement master;
        int DaysSlip;
        string showAmountType;
        GLTransClientTotalBank[] GlTransList;
        BankStatementLineGridClient[] BankList;
        System.Windows.Controls.Orientation orient;
        List<BankStatementAPI.BankRemoveJournal> _JournalsRemoved;

        string bankStatCaption;
        string transactionCaption;

        public BankStatementLinePage(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }

        public BankStatementLinePage(UnicontaBaseEntity sourceData)
            : base(sourceData)
        {
            Init();
            master = sourceData as Uniconta.DataModel.BankStatement;
            UpdateMaster();
        }

        void Init()
        {
            InitializeComponent();
            bankStatCaption = Localization.lookup("BankStatement");
            transactionCaption = Localization.lookup("Transactions");

            if (fromDate == DateTime.MinValue)
            {
                DateTime date = GetSystemDefaultDate();
                var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                toDate = firstDayOfMonth.AddMonths(1).AddDays(-1);
                fromDate = firstDayOfMonth.AddMonths(-2);
            }

            dgBankStatementLine.api = api;
            tranApi = new Uniconta.API.GeneralLedger.ReportAPI(api);
            bankTransApi = new BankStatementAPI(api);
            SetRibbonControl(localMenu, dgBankStatementLine);
            dgBankStatementLine.BusyIndicator = busyIndicator;
            dgAccountsTransGrid.BusyIndicator = busyIndicator;
            dgAccountsTransGrid.api = api;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgBankStatementLine.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgAccountsTransGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged1;
            State.Header =
            StateCol.Header = Localization.lookup("Status");
            SetStatusText();
            Mark.Visible = MarkCol.Visible = true;
            GetShowHideGreenMenuItem();

            orient = api.session.Preference.BankStatementHorisontal ? System.Windows.Controls.Orientation.Horizontal : System.Windows.Controls.Orientation.Vertical;
            lGroup.Orientation = orient;

            this.showAmountType = Localization.lookup("All");
            dgBankStatementLine.View.SearchControl = localMenu.SearchControl;
            ribbonControl.lowerSearchGrid = dgAccountsTransGrid;
            ribbonControl.UpperSearchNullText = bankStatCaption;
            ribbonControl.LowerSearchNullText = transactionCaption;

            this.PreviewKeyDown += RootVisual_KeyDown;
            this.BeforeClose += BankStatementLinePage_BeforeClose;
            this.layOutTrans.Caption = string.Empty;
            this.layOutBankStat.Caption = string.Empty;
        }

        void UpdateMaster()
        {
            bool RoundTo100;
            var Comp = api.CompanyEntity;
            DaysSlip = master._DaysSlip;
            if (Comp.SameCurrency(master._Currency))
                RoundTo100 = Comp.RoundTo100;
            else
            {
                ShowCurrency = true;
                RoundTo100 = !CurrencyUtil.HasDecimals(master._Currency);
            }
            if (RoundTo100)
            {
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = Total.HasDecimals = false;
                Debitcol.HasDecimals = Creditcol.HasDecimals = Amountcol.HasDecimals = Totalcol.HasDecimals = false;
            }

            if (!Comp._UseVatOperation)
                VatOperation.Visible = false;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Bank", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    var cache = api.GetCache(typeof(Uniconta.DataModel.BankStatement)) ?? api.LoadCache(typeof(Uniconta.DataModel.BankStatement)).GetAwaiter().GetResult();
                    master = (Uniconta.DataModel.BankStatement)cache.Get(rec.Value);
                    if (master != null)
                        UpdateMaster();
                }
            }
            base.SetParameter(Parameters);
        }

        public override void PageClosing()
        {
            if (dgBankStatementLine.IsAutoSave && IsDataChaged)
                saveGrid();
            base.PageClosing();
        }

        private void BankStatementLinePage_BeforeClose()
        {
            this.PreviewKeyDown -= RootVisual_KeyDown;
        }

        private void RootVisual_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (dgBankStatementLine.CurrentColumn.Name == "HasOffsetAccounts" && e.Key == Key.Down)
                {
                    var currentRow = dgBankStatementLine.SelectedItem as BankStatementLineGridClient;
                    if (currentRow != null)
                        CallOffsetAccount(currentRow);
                }
            }

            if (e.Key == Key.F8)
            {
                ribbonControl.PerformRibbonAction("OpenTran");
                e.Handled = true;
            }
            else if (e.Key == Key.F9)
            {
                ribbonControl.PerformRibbonAction("VoidTransaction");
                e.Handled = true;
            }
        }

        private void DataControl_CurrentItemChanged1(object sender, CurrentItemChangedEventArgs e)
        {
            GLTransClientTotalBank oldselectedItem = e.OldItem as GLTransClientTotalBank;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= selectedItem_PropertyChanged1;

            GLTransClientTotalBank selectedItem = e.NewItem as GLTransClientTotalBank;
            if (selectedItem != null)
                selectedItem.PropertyChanged += selectedItem_PropertyChanged1;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
            Amountcol.Visible = !this.ShowCurrency;
            if (this.ShowCurrency)
                AmountCur.Visible = true;
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dg, lookup);
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            BankStatementLineGridClient oldselectedItem = e.OldItem as BankStatementLineGridClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= selectedItem_PropertyChanged;

            BankStatementLineGridClient selectedItem = e.NewItem as BankStatementLineGridClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += selectedItem_PropertyChanged;
                // SetAccountSource(selectedItem);
            }
        }

        SQLCache LedgerCache, DebtorCache, CreditorCache, ChargeGrp;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            LedgerCache = api.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            ChargeGrp = api.GetCache(typeof(Uniconta.DataModel.GLChargeGroup)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLChargeGroup)).ConfigureAwait(false);
        }

        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            BankStatementLineGridClient selectedItem = dgBankStatementLine.SelectedItem as BankStatementLineGridClient;
            if (selectedItem != null)
            {
                SetAccountSource(selectedItem);
                if (prevAccount != null)
                    prevAccount.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevAccount = editor;
                editor.isValidate = true;
            }
        }
        private void SetAccountSource(BankStatementLineGridClient rec)
        {
            SQLCache cache;
            switch (rec._AccountType)
            {
                case (byte)GLJournalAccountType.Finans:
                    cache = LedgerCache;
                    break;
                case (byte)GLJournalAccountType.Debtor:
                    cache = DebtorCache;
                    break;
                case (byte)GLJournalAccountType.Creditor:
                    cache = CreditorCache;
                    break;
                default: return;
            }

            if (cache != null)
            {
                int ver = cache.version + 10000 * (rec._AccountType + 1);
                if (ver != rec.AccountVersion)
                {
                    rec.AccountVersion = ver;
                    rec.accntSource = cache.GetNotNullArray;
                    rec.NotifyPropertyChanged("AccountSource");
                }
            }
        }

        DCAccount getDCAccount(GLJournalAccountType type, string Acc)
        {
            if (type == GLJournalAccountType.Finans || Acc == null)
                return null;
            var cache = (type == GLJournalAccountType.Debtor) ? DebtorCache : CreditorCache;
            return (DCAccount)cache.Get(Acc);
        }
        DCAccount copyDCAccount(BankStatementLineGridClient rec, GLJournalAccountType type, string Acc)
        {
            var dc = getDCAccount(type, Acc);
            if (dc == null)
                return null;
            if (dc._Dim1 != null)
                rec.Dimension1 = dc._Dim1;
            if (dc._Dim1 != null)
                rec.Dimension2 = dc._Dim2;
            if (dc._Dim1 != null)
                rec.Dimension3 = dc._Dim3;
            if (dc._Dim1 != null)
                rec.Dimension4 = dc._Dim4;
            if (dc._Dim1 != null)
                rec.Dimension5 = dc._Dim5;
            return dc;
        }
        async void GetSearchOpenInvoice(BankStatementLineGridClient line, GLJournalAccountType type)
        {
            InvoiceAPI InvApi = new InvoiceAPI(api);
            string strAccount = (string)await InvApi.SearchOpenInvoice(type, line._Invoice);
            if (strAccount != null)
                line.Account = strAccount;
        }

        double transAmt = 0d;
        void selectedItem_PropertyChanged1(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Mark":
                    long val;
                    var lstAct = ((IEnumerable<GLTransClientTotalBank>)dgAccountsTransGrid.ItemsSource);
                    if (ShowCurrency)
                        val = (from t in lstAct where t._Mark select t._AmountCurCent).Sum();
                    else
                        val = (from t in lstAct where t._Mark select t._AmountCent).Sum();
                    transAmt = (val / 100d);
                    this.layOutTrans.Caption = string.Format("'{0:N2}'    '({1:N2})'", transAmt, bankStatAmt - transAmt);

                    break;
            }
        }

        double bankStatAmt = 0d;
        void selectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as BankStatementLineGridClient;
            switch (e.PropertyName)
            {
                case "AccountType":
                    SetAccountSource(rec);
                    break;
                case "Invoice":
                    if (rec._Invoice != null && rec._Account == null)
                    {
                        GLJournalAccountType type = rec._AmountCent > 0 ? GLJournalAccountType.Debtor : GLJournalAccountType.Creditor;
                        rec.AccountType = AppEnums.GLAccountType.ToString((int)type);
                        GetSearchOpenInvoice(rec, type);
                    }
                    break;
                case "Account":
                    if (rec._AccountType != (byte)GLJournalAccountType.Finans)
                    {
                        var dc = copyDCAccount(rec, (GLJournalAccountType)rec._AccountType, rec._Account);
                        if (dc != null)
                        {
                            rec.Vat = null;
                            rec.VatOperation = null;
                        }
                    }
                    else
                    {
                        var Acc = (GLAccount)LedgerCache.Get(rec._Account);
                        if (Acc != null)
                        {
                            if (Acc._IsDCAccount || Acc._MandatoryTax == VatOptions.NoVat)
                            {
                                rec.Vat = null;
                                rec.VatOperation = null;
                            }
                            else
                            {
                                rec.Vat = Acc._Vat;
                                rec.VatOperation = Acc._VatOperation;
                            }
                        }
                    }
                    break;
                case "Charge":
                    var grp = (GLChargeGroup)ChargeGrp?.Get(rec._Charge);
                    rec.ChargeAmount = grp != null ? Math.Round(grp._Amount + (Math.Abs(rec.Amount) + grp._Amount) * grp._Pct / Math.Max(100 - grp._Pct, 1), 2) : 0;
                    break;
                case "Mark":
                    var lstBst = ((IEnumerable<BankStatementLineGridClient>)dgBankStatementLine.ItemsSource);
                    var val = (from t in lstBst where t._Mark select t._AmountCent).Sum();
                    bankStatAmt = (val / 100d);
                    this.layOutBankStat.Caption = string.Format("'{0:N2}'", bankStatAmt);
                    this.layOutTrans.Caption = string.Format("'{0:N2}'    '({1:N2})'", transAmt, bankStatAmt - transAmt);
                    break;
                case "DocumentRef":
                    dgBankStatementLine.SetLoadedRow(rec);
                    break;
            }
        }

        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgBankStatementLine);
            gridCtrls.Add(dgAccountsTransGrid);
            isChildGridExist = true;
        }

        public async override System.Threading.Tasks.Task InitQuery()
        {
            busyIndicator.IsBusy = true;

            // load gltrans without awit
            var glTransTask = tranApi.GetBank(new GLTransClientTotalBank(), master._Account, fromDate, toDate);

            var bankStmtLines = (BankStatementLineGridClient[])await bankTransApi.GetTransactions(new BankStatementLineGridClient(), master, fromDate, toDate, true);

            if (showAmountType == Localization.lookup("Debit"))
            {
                var stmtLines = bankStmtLines.Where(x => x._AmountCent >= 0).ToArray();
                bankStmtLines = stmtLines;
            }
            else if (showAmountType == Localization.lookup("Credit"))
            {
                var stmtLines = bankStmtLines.Where(x => x._AmountCent <= 0).ToArray();
                bankStmtLines = stmtLines;
            }

            var listtran = (GLTransClientTotalBank[])await glTransTask;  // wait for gltrans

            busyIndicator.IsBusy = false;

            var ShowCurrency = this.ShowCurrency;
            if (listtran != null)
            {
                Array.Sort(listtran, new GLTransClientSort());
                long Total = 0;
                for (int i = 0; (i < listtran.Length); i++)
                {
                    var p = listtran[i];
                    Total += ShowCurrency ? p._AmountCurCent : p._AmountCent;
                    p._Total = Total;
                }
            }
            if (bankStmtLines != null)
            {
                DateTime LastPostedDate = DateTime.MinValue;
                int startGLSearch = 0;
                long Total = 0;
                int l = bankStmtLines.Length;
                if (l > 0)
                    bankStmtLines[0]._AmountCent += Uniconta.Common.Utility.NumberConvert.ToLong(100d * master._StartBalance);

                for (int i = 0; (i < l); i++)
                {
                    var p = bankStmtLines[i];
                    if (p._Primo)
                        p._Text = Localization.lookup("Primo");
                    //if (! p._Void)
                    {
                        Total += p._AmountCent;
                        p._Total = Total;
                    }

                    var PostedDate = p._PostedDate;
                    if (PostedDate != DateTime.MinValue)
                    {
                        if (PostedDate >= LastPostedDate)
                            LastPostedDate = PostedDate;
                        else
                            startGLSearch = 0;

                        var BankStatementLineId = ((int)SmallDate.Pack(p._Date) << 16) + p.LineNumber;

                        for (int n = startGLSearch; (n < listtran.Length); n++)
                        {
                            var t = listtran[n];
                            if (t._Date < PostedDate)
                                startGLSearch = n;
                            else if (t._Date == PostedDate)
                            {
                                if ((t._JournalPostedId != 0 && t._Voucher == p._Voucher && t._VoucherLine == p._VoucherLine && t._JournalPostedId == p._JournalPostedId) ||
                                    (t.BankStatementLine == BankStatementLineId))
                                {
                                    if (p._Trans == null)
                                        p._Trans = new List<GLTransClientTotalBank>(1);
                                    p._Trans.Add(t);

                                    if (t._StatementLines == null)
                                        t._StatementLines = new List<BankStatementLineGridClient>(1);
                                    t._StatementLines.Add(p);

                                    if (p.MultiMark != null)
                                    {
                                        foreach (var mark in p.MultiMark)
                                        {
                                            PostedDate = mark.Date;
                                            int cnt = startGLSearch;
                                            while (++cnt < listtran.Length)
                                            {
                                                t = listtran[cnt];
                                                if (t._Date == PostedDate)
                                                {
                                                    if ((t._Voucher == mark.Voucher && t._VoucherLine == mark.VoucherLine && t._JournalPostedId == mark.JournalPostedId
                                                        && (ShowCurrency ? t._AmountCur : t._Amount) == mark.Amount) ||
                                                        (t.JournalId != 0 && t.JournalId == mark.JournalId && t.JourRowId != 0 && t.JourRowId == mark.JourRowId))
                                                    {
                                                        p._Trans.Add(t);
                                                        if (t._StatementLines == null)
                                                            t._StatementLines = new List<BankStatementLineGridClient>(1);
                                                        t._StatementLines.Add(p);
                                                        t._Reconciled = true;
                                                        if (t.JournalId != 0)
                                                            t.BankStatementLine = BankStatementLineId;
                                                        break;
                                                    }
                                                }
                                                else if (t._Date > PostedDate)
                                                    break;
                                            }
                                        }
                                    }
                                    break;
                                }
                                else if (p.MultiMark != null && t._JournalPostedId == 0 && t.JournalId != 0)
                                {
                                    foreach (var mark in p.MultiMark)
                                    {
                                        PostedDate = mark.Date;
                                        for (int cnt = startGLSearch; (cnt < listtran.Length); cnt++)
                                        {
                                            t = listtran[cnt];
                                            if (t._Date == PostedDate)
                                            {
                                                if (t.JournalId == mark.JournalId && t.JourRowId != 0 && t.JourRowId == mark.JourRowId)
                                                {
                                                    if (p._Trans == null)
                                                        p._Trans = new List<GLTransClientTotalBank>(1);
                                                    p._Trans.Add(t);
                                                    if (t._StatementLines == null)
                                                        t._StatementLines = new List<BankStatementLineGridClient>(1);
                                                    t._StatementLines.Add(p);
                                                    t._Reconciled = true;
                                                    p._InJournal = true;
                                                    break;
                                                }
                                            }
                                            else if (t._Date > PostedDate)
                                                break;
                                        }
                                    }
                                }
                            }
                            else
                                break;
                        }
                    }
                }
            }
            GlTransList = listtran;
            dgAccountsTransGrid.SetSource(listtran);
            BankList = bankStmtLines;
            dgBankStatementLine.SetSource(bankStmtLines);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            GLTransClientTotalBank selectedTrans;

            var selectedItem = getBSLSelecteditem();
            dgAccountsTransGrid.tableView.CloseEditor();
            dgBankStatementLine.tableView.CloseEditor();
            switch (ActionType)
            {
                case "Save":
                    saveGrid();
                    break;
                case "Pre":
                    SetFilter(-1);
                    break;
                case "Next":
                    SetFilter(1);
                    break;
                case "Interval":
                    setInterval();
                    break;
                case "RefVoucher":
                    if (selectedItem == null)
                        return;
                    var source = (IList)dgBankStatementLine.ItemsSource;
                    if (source != null)
                    {
                        var _refferedVouchers = new List<int>();
                        IEnumerable<BankStatementLineGridClient> gridItems = source.Cast<BankStatementLineGridClient>();
                        foreach (var statementLine in gridItems)
                            if (statementLine._DocumentRef != 0)
                                _refferedVouchers.Add(statementLine._DocumentRef);

                        AddDockItem(TabControls.AttachVoucherGridPage, new object[1] { _refferedVouchers }, true);
                    }

                    break;
                case "ViewVoucher":
                    bool useGLTrans = true;
                    if (selectedItem != null)
                    {
                        selectedTrans = dgAccountsTransGrid.SelectedItem as GLTransClientTotalBank;
                        if (selectedTrans == null || selectedTrans._DocumentRef == 0)
                            useGLTrans = false;
                        else if (selectedItem._DocumentRef != 0 && selectedItem._DocumentRef != selectedTrans._DocumentRef)
                        {
                            var page = this as GridBasePage;
                            useGLTrans = (page.CurrentKeyDownGrid == dgAccountsTransGrid);
                        }
                    }
                    if (useGLTrans)
                    {
                        selectedTrans = dgAccountsTransGrid.SelectedItem as GLTransClientTotalBank;
                        if (selectedTrans != null)
                        {
                            dgAccountsTransGrid.syncEntity.Row = selectedTrans;
                            busyIndicator.IsBusy = true;
                            ViewVoucher(TabControls.VouchersPage3, dgAccountsTransGrid.syncEntity);
                            busyIndicator.IsBusy = false;
                        }
                    }
                    else if (selectedItem != null)
                    {
                        dgBankStatementLine.syncEntity.Row = selectedItem;
                        busyIndicator.IsBusy = true;
                        ViewVoucher(TabControls.VouchersPage3, dgBankStatementLine.syncEntity);
                        busyIndicator.IsBusy = false;
                    }
                    break;
                case "DragDrop":
                case "ImportVoucher":
                    if (selectedItem != null)
                    {
                        dgBankStatementLine.SetLoadedRow(selectedItem);
                        AddVoucher(selectedItem, ActionType);
                    }
                    break;

                case "RemoveVoucher":
                    if (selectedItem == null)
                        return;
                    if (selectedItem._DocumentRef != 0)
                        selectedItem.DocumentRef = 0;
                    else
                        UnicontaMessageBox.Show(Localization.lookup("NoVoucherExist"), Localization.lookup("Information"), MessageBoxButton.OK);
                    break;
                case "VoucherTransactions":
                    selectedTrans = dgAccountsTransGrid.SelectedItem as GLTransClientTotalBank;
                    if (selectedTrans != null)
                    {
                        string vheader = Util.ConcatParenthesis(Localization.lookup("VoucherTransactions"), selectedTrans._Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgAccountsTransGrid.syncEntity, vheader);
                    }
                    break;
                case "AddMatch":
                    AddMatch();
                    break;
                case "RemoveMatch":
                    RemoveMatch();
                    break;
                case "TransferBankStatement":
                    saveGrid();
                    TransferBankStatementToJournal();
                    break;
                case "VoidTransaction":
                    selectedTrans = dgAccountsTransGrid.SelectedItem as GLTransClientTotalBank;
                    if (selectedTrans != null)
                        ChangeVoidState(selectedTrans);
                    break;
                case "ChangeOrientation":
                    orient = 1 - orient;
                    lGroup.Orientation = orient;
                    api.session.Preference.BankStatementHorisontal = (orient == System.Windows.Controls.Orientation.Horizontal);
                    break;
                case "ShowHideGreenLines":
                    hideGreen = !hideGreen;
                    setShowHideGreen(hideGreen);
                    string filterString = dgBankStatementLine.FilterString;
                    if (filterString.Contains("[State]"))
                        filterString = "";
                    else
                        filterString = "[State]<2";
                    dgBankStatementLine.FilterString = filterString;
                    dgAccountsTransGrid.FilterString = filterString;
                    break;
                case "ShowAmount":
                    ShowAmountWindow();
                    break;
                case "AutoReconciliation":
                    AutoReconciliation();
                    break;
                case "AddMapping":
                    if (selectedItem != null)
                        (new CWAddBankImportMapping(api, master, selectedItem)).Show();
                    break;
                case "OffSetAccount":
                    if (selectedItem != null)
                        CallOffsetAccount(selectedItem);
                    break;
                case "OpenTran":
                    if (selectedItem != null)
                        SettleOpenTransactionPage(selectedItem);
                    break;
                case "EditJournalLine":
                    selectedTrans = dgAccountsTransGrid.SelectedItem as GLTransClientTotalBank;
                    if (selectedTrans != null)
                        EditGLDailyJournalLine(selectedTrans);
                    break;
                case "RefreshGrid":
                    SetStatusText();
                    saveGrid(true);
                    break;
                case "AccountsTransaction":
                    if (selectedItem != null)
                        OpenTranscation(selectedItem);
                    break;
                case "Adjustment":
                    Adjustment();
                    break;
                case "SendVoucherReminder":
                    if (selectedItem != null)
                        Utility.SendVoucherReminder(api, selectedItem._Date, selectedItem._AmountCur != 0 ? selectedItem._AmountCur : selectedItem._Amount, selectedItem._Currency, selectedItem._Text);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void Adjustment()
        {
            var lstbsl = ((IEnumerable<BankStatementLineGridClient>)dgBankStatementLine.ItemsSource);
            if (lstbsl == null)
                return;
            var lstAct = ((List<GLTransClientTotalBank>)dgAccountsTransGrid.ItemsSource);
            if (lstAct == null)
                return;
            var markedbstList = lstbsl.Where(bl => bl.Mark == true && bl.State == (byte)1).ToList();
            var markedactList = lstAct.Where(ac => ac.Mark == true && ac.State == (byte)1).ToList();
            if (markedbstList.Count != 1 || markedactList.Count != 1)
            {
                UtilDisplay.ShowErrorCode(ErrorCodes.TooManyLinesSelected);
                return;
            }

            var glTran = markedactList[0];
            if (glTran._JournalPostedId == 0)
            {
                UnicontaMessageBox.Show(Localization.lookup("CannotUseMethod") + Environment.NewLine + Localization.lookup("JournalLinesIncluded"), Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }

            var bnk = markedbstList[0];
            var dif = bnk._AmountCent - glTran._AmountCent;

            var labl1 = "CreatePennyDif";
            var labl2 = "PennyDif";
            if (glTran._Currency != 0)
            {
                if (glTran._Currency == master._Currency)
                    dif = bnk._AmountCent - glTran._AmountCurCent;
                else
                {
                    labl1 = "CreateExchangeRegulation";
                    labl2 = "ExchangeRegulated";
                }
            }

            if (dif != 0 && UnicontaMessageBox.Show(string.Concat(Localization.lookup(labl1), "? ", Localization.lookup("Amount"), " = ", (dif / 100d).ToString("N2")),
                     Localization.lookup(labl2), MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                busyIndicator.IsBusy = true;
                var rec = new GLTransClientTotalBank();
                var err = await new PostingAPI(api).BalanceAdjustment(glTran, rec, dif);
                busyIndicator.IsBusy = false;

                if (err != 0)
                    UtilDisplay.ShowErrorCode(err);
                else
                {
                    byte SystemAccount = (byte)SystemAccountTypes.PennyDif;
                    if (glTran._Currency != 0)
                    {
                        if (glTran._Currency == master._Currency)
                        {
                            rec._Currency = glTran._Currency;
                            rec._AmountCurCent = dif;
                        }
                        else
                            SystemAccount = (byte)SystemAccountTypes.ExchangeDif;
                    }
                    rec._DCAccount = (from r in (IEnumerable<GLAccount>)LedgerCache.GetNotNullArray where r._SystemAccount == SystemAccount select r).FirstOrDefault()?._Account;
                    rec._Mark = true;
                    lstAct.Insert(lstAct.IndexOf(glTran) + 1, rec);
                    dgAccountsTransGrid.RefreshData();
                    this.layOutTrans.Caption = string.Format("'{0:N2}'    '({1:N2})'", bnk.Amount, 0d);

                    AddMatch();
                }
            }
        }

        void OpenTranscation(BankStatementLineGridClient rec)
        {
            IdKey Acc;
            switch (rec._AccountType)
            {
                case (byte)GLJournalAccountType.Finans:
                    Acc = LedgerCache?.Get(rec.Account);
                    if (Acc != null)
                        AddDockItem(TabControls.AccountsTransaction, Acc, string.Concat(Localization.lookup("AccountsTransaction"), "/", Acc.KeyStr));
                    break;
                case (byte)GLJournalAccountType.Debtor:
                    Acc = DebtorCache?.Get(rec.Account);
                    if (Acc != null)
                        AddDockItem(TabControls.DebtorTransactions, Acc, string.Concat(Localization.lookup("DebtorTransactions"), "/", Acc.KeyStr));
                    break;
                case (byte)GLJournalAccountType.Creditor:
                    Acc = CreditorCache?.Get(rec.Account);
                    if (Acc != null)
                        AddDockItem(TabControls.CreditorTransactions, Acc, string.Concat(Localization.lookup("CreditorTransactions"), "/", Acc.KeyStr));
                    break;
            }
        }

        async void EditGLDailyJournalLine(GLTransClientTotal selectedGlTrans)
        {
            if (selectedGlTrans.JournalId != 0 && selectedGlTrans.JourRowId != 0)
            {
                var glDlyJournalLine = new GLDailyJournalLineClient();
                glDlyJournalLine.RowId = selectedGlTrans.JourRowId;
                glDlyJournalLine.Journal = NumberConvert.ToString(selectedGlTrans.JournalId);
                var err = await api.Read(glDlyJournalLine);
                if (err == ErrorCodes.Succes)
                    AddDockItem(TabControls.GLDailyJournalLinePage2, glDlyJournalLine, string.Format("{0}: {1}", Localization.lookup("Journalline"), selectedGlTrans.JournalId));
            }
        }

        private void AddVoucher(BankStatementLineGridClient bankStatementLine, string actionType)
        {
            if (actionType == "DragDrop")
            {
                var dragDropWindow = new UnicontaDragDropWindow(false);
                Utility.PauseLastAutoSaveTime();
                dragDropWindow.Closed += delegate
                {
                    if (dragDropWindow.DialogResult == true)
                    {
                        var voucher = new VouchersClient();
                        var fileInfo = dragDropWindow.FileInfoList[0];
                        voucher._Data = fileInfo.FileBytes;
                        voucher._Text = fileInfo.FileName;
                        voucher._Fileextension = DocumentConvert.GetDocumentType(fileInfo.FileExtension);

                        Utility.ImportVoucher(bankStatementLine, api, voucher, true);
                    }
                };
                dragDropWindow.Show();
            }
            else
                Utility.ImportVoucher(bankStatementLine, api, null, false);
        }

        private void SettleOpenTransactionPage(BankStatementLineGridClient selectedItem)
        {
            var dcAccount = selectedItem.Account;
            var dcAccountObj = selectedItem._AccountType == (int)GLJournalAccountType.Debtor ? DebtorCache.Get(dcAccount) : CreditorCache.Get(dcAccount);

            if (dcAccountObj != null)
            {
                var lst = GetSettledItems(selectedItem, dcAccount, selectedItem._AccountType);
                object[] param = new object[5];
                param[0] = dcAccountObj;
                param[1] = (object)selectedItem._AccountType;
                param[2] = selectedItem;
                param[3] = false;
                param[4] = lst;

                AddDockItem(TabControls.SettleOpenTransactionPage, param, true, null, floatingLoc: Utility.GetDefaultLocation());
            }
        }

        private List<string> GetSettledItems(BankStatementLineGridClient currentRow, string account, byte actType)
        {
            var gridRows = (IEnumerable<BankStatementLineGridClient>)dgBankStatementLine.ItemsSource;
            if (gridRows == null)
                return null;

            List<string> lines = null;

            foreach (var row in gridRows)
            {
                if ((row._Settlement != null || row._Invoice != null) && (row._AccountType == actType && row._Account == account))
                {
                    if (!object.ReferenceEquals(row, currentRow))
                    {
                        if (lines == null)
                            lines = new List<string>();
                        if (row._Settlement != null)
                            lines.Add(row._Settlement);
                        else if (row._Invoice != null)
                            lines.Add(row._Invoice);
                    }
                }
            }

            return lines;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AttachVoucherGridPage && argument != null)
            {
                var voucherObj = argument as object[];
                var voucher = voucherObj[0] as VouchersClient;
                if (voucher != null)
                {
                    var openedFrom = voucherObj[1];
                    if (openedFrom == this.ParentControl)
                    {
                        var selectedItem = dgBankStatementLine.SelectedItem as BankStatementLineGridClient;
                        if (selectedItem != null && voucher.RowId != 0)
                        {
                            dgBankStatementLine.SetLoadedRow(selectedItem);
                            selectedItem.DocumentRef = voucher.RowId;
                            if (voucher._Invoice != null)
                                selectedItem.Invoice = voucher._Invoice;
                            if (voucher._Dim1 != null)
                                selectedItem.Dimension1 = voucher._Dim1;
                            if (voucher._Dim2 != null)
                                selectedItem.Dimension2 = voucher._Dim2;
                            if (voucher._Dim3 != null)
                                selectedItem.Dimension3 = voucher._Dim3;
                            if (voucher._Dim4 != null)
                                selectedItem.Dimension4 = voucher._Dim4;
                            if (voucher._Dim5 != null)
                                selectedItem.Dimension5 = voucher._Dim5;
                            dgBankStatementLine.SetModifiedRow(selectedItem);
                        }
                    }
                }
            }

            if (screenName == TabControls.SettleOpenTransactionPage)
            {
                var obj = argument as object[];
                if (obj != null && obj.Length == 2)
                    SetSettlementForBankStatemenLine(obj[0] as BankStatementLineGridClient, obj[1] as string);
            }
        }

        private void SetSettlementForBankStatemenLine(BankStatementLineGridClient bankStatementLine, string settlementStr)
        {
            if (bankStatementLine == null) return;

            dgBankStatementLine.SetLoadedRow(bankStatementLine);
            if (!string.IsNullOrEmpty(settlementStr))
            {
                string[] stlSplit = settlementStr.Split(':');
                var settle = stlSplit[1];
                var settleTypeStr = stlSplit[0];
                SettleValueType settleType;
                if (settleTypeStr == "V")
                    settleType = SettleValueType.Voucher;
                else if (settleTypeStr == "R")
                    settleType = SettleValueType.RowId;
                else
                    settleType = SettleValueType.Invoice;

                if (settleType == SettleValueType.Invoice && settle.IndexOf(';') < 0)
                {
                    bankStatementLine.Invoice = settle;
                    bankStatementLine.Settlement = null;
                }
                else
                {
                    if (settleType != bankStatementLine._SettleValue)
                    {
                        bankStatementLine._SettleValue = settleType;
                        bankStatementLine.NotifyPropertyChanged("SettleValue");
                    }
                    bankStatementLine.Settlement = settle;
                    bankStatementLine.Invoice = null;
                }
            }
            else
            {
                bankStatementLine.Settlement = null;
                bankStatementLine.Invoice = null;
            }
            dgBankStatementLine.SetModifiedRow(bankStatementLine);
        }

        private void ShowAmountWindow()
        {
            CWShowAmount winShowAmount = new CWShowAmount();
            winShowAmount.Closing += delegate
            {
                if (winShowAmount.DialogResult == true)
                {
                    showAmountType = winShowAmount.AmountTypeOption;
                    InitQuery();
                }
            };
            winShowAmount.Show();
        }

        ItemBase ibase;
        bool hideGreen = false;
        void GetShowHideGreenMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ShowHideGreenLines");
        }
        private void setShowHideGreen(bool hideGreen)
        {
            if (ibase == null)
                return;
            if (hideGreen)
            {
                ibase.Caption = string.Format(Localization.lookup("ShowOBJ"), Localization.lookup("Green"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("ShowGreen_32x32");
            }
            else
            {
                ibase.Caption = string.Format(Localization.lookup("HideOBJ"), Localization.lookup("Green"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("HideGreen_32x32");
            }
        }
        async void ChangeVoidState(GLTransClientTotalBank trans)
        {
            PostingAPI pApi = new PostingAPI(api);
            ErrorCodes res = await pApi.ChangeVoidState(trans);
            if (res == ErrorCodes.Succes)
            {
                trans._Void = !trans._Void;
                trans.UpdateVoid();
                UnicontaMessageBox.Show(Localization.lookup(trans._Void ? "TransactionWasReconciled" : "Unreconciled"), Localization.lookup("Message"));
            }
            else
                UtilDisplay.ShowErrorCode(res);
        }

        void TransferBankStatementToJournal()
        {
            CWTransferBankStatement winTransfer = new CWTransferBankStatement(fromDate, toDate, api, master);
            winTransfer.DialogTableId = 2000000016;
            winTransfer.Closed += async delegate
            {
                if (winTransfer.DialogResult == true)
                {
                    master._Journal = winTransfer.Journal;
                    PostingAPI pApi = new PostingAPI(api);
                    var res = await pApi.TransferBankStatementToJournal(master, winTransfer.FromDate, winTransfer.ToDate, winTransfer.BankAsOffset, winTransfer.isMarkLine, winTransfer.HasPhysicalVoucher, winTransfer.AddVoucherNumber);
                    if (res == ErrorCodes.Succes)
                    {
                        string strmsg = string.Format("{0}; {1}! {2} ?", Localization.lookup("GenerateJournalLines"), Localization.lookup("Completed"),
                            string.Format(Localization.lookup("GoTo"), Localization.lookup("JournalLines")));
                        var select = UnicontaMessageBox.Show(strmsg, Localization.lookup("Information"), MessageBoxButton.OKCancel);
                        if (select == MessageBoxResult.OK)
                        {
                            var parms = new[] { new BasePage.ValuePair("Journal", winTransfer.Journal) };
                            var header = string.Concat(Localization.lookup("Journal"), ": ", winTransfer.Journal);
                            AddDockItem(TabControls.GL_DailyJournalLine, null, header, null, true, null, parms);
                        }
                    }
                    else
                        UtilDisplay.ShowErrorCode(res);

                    InitQuery();
                }
            };
            winTransfer.Show();

        }
        static void Join(BankStatementLineGridClient bst, GLTransClientTotalBank act, bool HighlightLine = false)
        {
            bst._JournalPostedId = act._JournalPostedId;
            bst.Voucher = act._Voucher;
            bst.VoucherLine = act._VoucherLine;
            bst.PostedDate = act._Date;
            bst.TransType = act._TransType;
            bst._DocumentRef = act._DocumentRef;
            bst.AccountType = act.DCType;
            bst.Account = act._DCAccount;
            if (HighlightLine)
                bst.IsMatched = true;
            else
            {
                bst._IsMatched = true;
                bst.UpdateState();
            }
            bst.Updated = true;
            bst.Mark = false;
        }

        void AutoReconciliation()
        {
            var lstbsl = ((IEnumerable<BankStatementLineGridClient>)dgBankStatementLine.ItemsSource);
            if (lstbsl == null)
                return;
            var bstList = lstbsl.Where(bl => bl.Mark == false && bl.State == (byte)1).ToList();
            var lstAct = ((IEnumerable<GLTransClientTotalBank>)dgAccountsTransGrid.ItemsSource);
            if (lstAct == null)
                return;
            var actList = lstAct.Where(ac => ac.Mark == false && ac.State == (byte)1).ToList();
            var days = DaysSlip;
            var cnt = AutoReconciliationOne2One(bstList, actList, days);
            cnt += AutoReconciliationOne2Many(bstList, actList, days);
            cnt += AutoReconciliationMany2One(bstList, actList, days);
            UnicontaMessageBox.Show(string.Format("{0}: {1}", Localization.lookup("NumberOfRecords"), cnt), Localization.lookup("Message"));
        }

        int AutoReconciliationOne2One(List<BankStatementLineGridClient> bstList, List<GLTransClientTotalBank> actList, int slip)
        {
            int cnt = 0, i;
            var ch = new[] { ' ', ':' };
            for (int OffsetDays = 0; (OffsetDays <= slip); OffsetDays++)
            {
                for (var b = 0; (b < bstList.Count); b++)
                {
                    var bst = bstList[b];
                    var MinDate = bst.Date;
                    var MaxDate = MinDate;
                    if (OffsetDays > 0)
                    {
                        MinDate = MinDate.AddDays(-OffsetDays);
                        MaxDate = MaxDate.AddDays(OffsetDays);
                    }
                    var Amount = bst._AmountCent;
                    string Text = bst.Text;
                    string[] Split = null;
                    bool TrySplit = true;
                    GLTransClientTotalBank act;

                    int acFound = -1;
                    int TextFound = 0;
                    for (var a = 0; (a < actList.Count); a++)
                    {
                        act = actList[a];
                        if (act._Date > MaxDate)
                            break;

                        var TransAmount = !ShowCurrency ? act._AmountCent : act._AmountCurCent;
                        if (Amount == TransAmount && act._Date >= MinDate)
                        {
                            if (bst._AccountType > 0 && bst.Account != null && act.DCAccount != null && bst.Account != act.DCAccount) // we have a different DC. we cannot match
                                continue;

                            if (Text != null && act._Text != null)
                            {
                                if (Text == act._Text)
                                {
                                    if (TextFound == 10)
                                    {
                                        acFound = -1;
                                        break;
                                    }
                                    TextFound = 10;
                                    acFound = a;
                                    continue;
                                }

                                string s;
                                if (TrySplit && TextFound == 0)
                                {
                                    TrySplit = false;
                                    Split = Text.Split(ch);
                                    if (Split.Length == 1)
                                        Split = null;
                                    else
                                    {
                                        bool any = false;
                                        for (i = 0; i < Split.Length; i++)
                                        {
                                            s = Split[i];
                                            if (s.Length >= 5) // some save a OrderNumber or InvoiceNumber inside text. Here we see if some numbers match
                                            {
                                                for (int j = 0; j < s.Length; j++)
                                                {
                                                    if (!char.IsDigit(s[j]))
                                                    {
                                                        Split[i] = null;
                                                        break;
                                                    }
                                                }
                                                if (Split[i] != null)
                                                    any = true;
                                            }
                                            else
                                                Split[i] = null;
                                        }
                                        if (!any)
                                            Split = null;
                                    }
                                }

                                if (Split != null)
                                {
                                    var t = act._Text;
                                    int nFound = 0;
                                    for (i = 0; i < Split.Length; i++)
                                    {
                                        s = Split[i];
                                        if (s != null && t.IndexOf(s) >= 0)
                                            nFound++;
                                    }
                                    if (nFound > TextFound)
                                    {
                                        TextFound = nFound;
                                        acFound = a;
                                    }
                                    if (nFound > 0)
                                        continue;
                                }
                            }
                            if (acFound < 0)
                                acFound = a;
                            else if (TextFound == 0)
                            {
                                acFound = -1;
                                break;
                            }
                        }
                    }
                    if (acFound >= 0)
                    {
                        act = actList[acFound];
                        Join(bst, act);
                        bst.Trans = new List<GLTransClientTotalBank>(1) { act };
                        act._Reconciled = true;
                        act.Mark = false;
                        act._IsMatched = true;
                        act.StatementLines = new List<BankStatementLineGridClient>(1) { bst };
                        act.UpdateState();
                        cnt++;
                        bstList.RemoveAt(b);
                        b--;
                        actList.RemoveAt(acFound);
                    }
                }
            }
            return cnt;
        }

        int AutoReconciliationOne2Many(List<BankStatementLineGridClient> bstList, List<GLTransClientTotalBank> actList, int slip)
        {
            int cnt = 0;
            var actlen = actList.Count;

            for (int OffsetDays = 0; (OffsetDays <= slip); OffsetDays++)
            {
                for (var b = 0; (b < bstList.Count); b++)
                {
                    var bst = bstList[b];
                    var MinDate = bst.Date;
                    var MaxDate = MinDate;
                    if (OffsetDays > 0)
                    {
                        MinDate = MinDate.AddDays(-OffsetDays);
                        MaxDate = MaxDate.AddDays(OffsetDays);
                    }
                    var Amount = bst._AmountCent;

                    int startIdx = 0;
                    while (startIdx < actlen && actList[startIdx]._Date < MinDate)
                        startIdx++;

                    while (startIdx < actlen)
                    {
                        var act = actList[startIdx];
                        if (act._Date > MaxDate)
                            break;

                        if (bst._AccountType > 0 && bst.Account != null && act.DCAccount != null && bst.Account != act.DCAccount) // we have a different DC. we cannot match
                        {
                            startIdx++;
                            continue;
                        }

                        long sumTrans = 0;
                        int n = startIdx;
                        while (n < actlen)
                        {
                            var act2 = actList[n++];
                            if (act2._Date > MaxDate)
                                break;

                            var newAmount = !ShowCurrency ? act2._AmountCent : act2._AmountCurCent;
                            if ((sumTrans > 0 && newAmount < 0) || (sumTrans < 0 && newAmount > 0))
                                break;
                            sumTrans += newAmount;
                            if (sumTrans == Amount)
                            {
                                var bstLink = new List<BankStatementLineGridClient>(1) { bst };
                                Join(bst, act);
                                bst.Trans = new List<GLTransClientTotalBank>(n - startIdx);
                                while (startIdx < n)
                                {
                                    act = actList[startIdx];
                                    bst.Trans.Add(act);
                                    act._Reconciled = true;
                                    act.Mark = false;
                                    act._IsMatched = true;
                                    act.StatementLines = bstLink;
                                    act.UpdateState();
                                    cnt++;

                                    actList.RemoveAt(startIdx);
                                    n--;
                                    actlen--;
                                }
                                startIdx = actlen; // we will exit

                                bstList.RemoveAt(b);
                                b--;
                                break;
                            }
                            if (Math.Abs(sumTrans) > Math.Abs(Amount))
                                break;
                        }
                        startIdx++;
                    }
                }
            }
            return cnt;
        }

        int AutoReconciliationMany2One(List<BankStatementLineGridClient> bstList, List<GLTransClientTotalBank> actList, int slip)
        {
            int cnt = 0;
            var bstlen = bstList.Count;
            for (int OffsetDays = 0; (OffsetDays <= slip); OffsetDays++)
            {
                for (var a = 0; (a < actList.Count); a++)
                {
                    var act = actList[a];
                    var MinDate = act.Date;
                    var MaxDate = MinDate;
                    if (OffsetDays > 0)
                    {
                        MinDate = MinDate.AddDays(-OffsetDays);
                        MaxDate = MaxDate.AddDays(OffsetDays);
                    }
                    var Amount = !ShowCurrency ? act._AmountCent : act._AmountCurCent;

                    int startIdx = 0;
                    while (startIdx < bstlen && bstList[startIdx]._Date < MinDate)
                        startIdx++;

                    while (startIdx < bstlen)
                    {
                        var bst = bstList[startIdx];
                        if (bst._Date > MaxDate)
                            break;

                        if (bst._AccountType > 0 && bst.Account != null && act.DCAccount != null && bst.Account != act.DCAccount) // we have a different DC. we cannot match
                        {
                            startIdx++;
                            continue;
                        }

                        long sumTrans = 0;
                        int n = startIdx;
                        while (n < bstlen)
                        {
                            var bst2 = bstList[n++];
                            if (bst2._Date > MaxDate)
                                break;

                            var newAmount = bst2._AmountCent;
                            if ((sumTrans > 0 && newAmount < 0) || (sumTrans < 0 && newAmount > 0))
                                break;
                            sumTrans += newAmount;
                            if (sumTrans == Amount)
                            {
                                var actLink = new List<GLTransClientTotalBank>(1) { act };
                                act._Reconciled = true;
                                act.Mark = false;
                                act._IsMatched = true;
                                act.UpdateState();
                                act.StatementLines = new List<BankStatementLineGridClient>(n - startIdx);
                                while (startIdx < n)
                                {
                                    bst = bstList[startIdx];
                                    act.StatementLines.Add(bst);
                                    Join(bst, act);
                                    bst.Trans = actLink;
                                    cnt++;

                                    bstList.RemoveAt(startIdx);
                                    n--;
                                    bstlen--;
                                }
                                startIdx = bstlen; // we will exit

                                actList.RemoveAt(a);
                                a--;
                                break;
                            }
                            if (Math.Abs(sumTrans) > Math.Abs(Amount))
                                break;
                        }
                        startIdx++;
                    }
                }
            }
            return cnt;
        }

        bool AddMatch()
        {
            var lstbsl = ((IEnumerable<BankStatementLineGridClient>)dgBankStatementLine.ItemsSource);
            if (lstbsl == null)
                return false;
            var lstAct = ((IEnumerable<GLTransClientTotalBank>)dgAccountsTransGrid.ItemsSource);
            if (lstAct == null)
                return false;
            var markedbstList = lstbsl.Where(bl => bl.Mark == true && bl.State == (byte)1).ToList();
            var markedactList = lstAct.Where(ac => ac.Mark == true && ac.State == (byte)1).ToList();
            if (markedbstList.Count > 100 && markedactList.Count > 100)
            {
                UtilDisplay.ShowErrorCode(ErrorCodes.TooManyLinesSelected);
                return false;
            }
            if (markedbstList.Count == 0 && markedactList.Count == 0)
            {
                /* take selected item */
                var bl = getBSLSelecteditem();
                if (bl != null && bl.State == (byte)1)
                {
                    markedbstList.Capacity = 1;
                    markedbstList.Add(bl);
                }
                var ac = dgAccountsTransGrid.CurrentItem as GLTransClientTotalBank;
                if (ac != null && ac.State == (byte)1)
                {
                    markedactList.Capacity = 1;
                    markedactList.Add(ac);
                }
            }
            var ShowCurrency = this.ShowCurrency;

            if (markedbstList.Count > 0 && markedactList.Count > 0)
            {
                long sum1 = 0;
                foreach (var rec in markedbstList)
                    sum1 += rec._AmountCent;

                long sum2 = 0;
                foreach (var rec in markedactList)
                    if (ShowCurrency)
                        sum2 += rec._AmountCurCent;
                    else
                        sum2 += rec._AmountCent;

                if (Math.Abs(sum1 - sum2) > this.master._AllowedDifInMatch * 100d)
                {
                    UnicontaMessageBox.Show(string.Format("{0}. {1}={2} {3}={4}", Localization.lookup("ReconcileSumErr"),
                        Localization.lookup("BankStatement"), (sum1 / 100d).ToString("N2"),
                        Localization.lookup("Transactions"), (sum2 / 100d).ToString("N2")),
                        Localization.lookup("Error"), MessageBoxButton.OK);
                    return false;
                }

                if (markedbstList.Count == 1)
                {
                    var bst = markedbstList[0];
                    bst.Updated = true;
                    bst.Mark = false;
                    bst.Trans = markedactList;
                    bool first = true;
                    int BankStatementLineId = 0;
                    foreach (var act in markedactList)
                    {
                        if (first)
                        {
                            first = false;
                            if (act.JournalId != 0)
                            {
                                bst._InJournal = true;
                                BankStatementLineId = ((int)SmallDate.Pack(bst._Date) << 16) + bst.LineNumber;
                                act.BankStatementLine = BankStatementLineId;
                            }
                            Join(bst, act, true);
                        }
                        else if (BankStatementLineId != 0)
                        {
                            if (act.JournalId != 0)
                                act.BankStatementLine = BankStatementLineId;
                            else
                                bst._InJournal = false;
                        }
                        act.StatementLines = markedbstList;
                        act._Reconciled = true;
                        act.Mark = false;
                        act.IsMatched = true;
                    }
                }
                else if (markedactList.Count == 1)
                {
                    var act = markedactList[0];
                    act._Reconciled = true;
                    act.Mark = false;
                    act.IsMatched = true;
                    act.StatementLines = markedbstList;
                    foreach (var bst in markedbstList)
                    {
                        bst._InJournal = (act.JournalId != 0);
                        Join(bst, act, true);
                        bst.Trans = markedactList;
                    }
                }
                else
                {
                    bool first = true;
                    for (var i = 0; (i < markedbstList.Count); i++)
                    {
                        var bst = markedbstList[i];
                        bst.Updated = true;
                        bst.Mark = false;
                        bst.Trans = markedactList;
                        for (var n = 0; (n < markedactList.Count); n++)
                        {
                            var act = markedactList[n];

                            if (n == 0 && markedactList.Count > 1)
                            {
                                // lets see if we can find a one to one match
                                int match = -1;
                                for (var j = 0; (j < markedactList.Count); j++)
                                {
                                    var act2 = markedactList[j];
                                    if (bst._AmountCent != 0 && bst._AmountCent == (!ShowCurrency ? act2._AmountCent : act2._AmountCurCent))
                                    {
                                        if (match < 0)
                                            match = j;
                                        else
                                        {
                                            if (act2._Text != markedactList[match]._Text)
                                            {
                                                if (act2._Text == bst._Text)
                                                {
                                                    match = j;
                                                    continue;
                                                }
                                                if (markedactList[match]._Text == bst._Text)
                                                    continue;
                                            }
                                            var dm = Math.Abs((bst.Date - markedactList[match].Date).Days);
                                            var d2 = Math.Abs((bst.Date - act2.Date).Days);
                                            if (d2 < dm)
                                            {
                                                match = j;
                                                continue;
                                            }
                                            if (d2 > dm)
                                                continue;
                                            match = -1;
                                            break;
                                        }
                                    }
                                }
                                if (match >= 0)
                                {
                                    act = markedactList[match];
                                    act.StatementLines = new List<BankStatementLineGridClient>(1) { bst };
                                    bst.Trans = new List<GLTransClientTotalBank>(1) { act };
                                    act._Reconciled = true;
                                    act.Mark = false;
                                    act.IsMatched = true;
                                    Join(bst, act, true);

                                    markedactList.RemoveAt(match);
                                    markedbstList.RemoveAt(i);
                                    i--;
                                    break;
                                }
                            }

                            act.StatementLines = markedbstList;
                            act._Reconciled = true;
                            act.Mark = false;
                            act.IsMatched = true;
                            if (first)
                            {
                                first = false;
                                Join(bst, act, true);
                            }
                        }
                    }
                }
            }
            return true;
        }

        void RemoveMatch()
        {
            var lstbsl = ((IEnumerable<BankStatementLineGridClient>)dgBankStatementLine.ItemsSource);
            var markedbstList = lstbsl.Where(bl => bl.Mark == true && bl.State != (byte)1).ToList();
            if (markedbstList.Count == 0)
            {
                /* take selected item */
                var bt = getBSLSelecteditem();
                if (bt != null)
                    markedbstList.Add(bt);
            }
            if (markedbstList.Count == 1)
            {
                var bt = markedbstList[0];
                if (bt._Trans != null && bt._Trans.Count == 1)
                {
                    var ac = bt._Trans[0];
                    if (ac.StatementLines != null && ac.StatementLines.Count > 1)
                    {
                        ac.IsCleared = true;
                        markedbstList = ac.StatementLines;
                    }
                }
            }

            foreach (var bst in markedbstList)
            {
                bst._JournalPostedId = 0;
                bst.Voucher = 0;
                bst.VoucherLine = 0;
                bst.PostedDate = DateTime.MinValue;
                bst.TransType = null;
                bst._DocumentRef = 0;
                bst.AccountType = null;
                bst.Account = null;
                bst._InJournal = false;
                if (bst._Trans != null)
                {
                    if (bst._Trans.Count > 1)
                        bst.Updated = true;

                    foreach (var tr in bst._Trans)
                    {
                        if (tr.BankStatementLine != 0)
                        {
                            if (this._JournalsRemoved == null)
                                this._JournalsRemoved = new List<BankStatementAPI.BankRemoveJournal>();
                            var lin = new BankStatementAPI.BankRemoveJournal()
                            {
                                JournalId = tr.JournalId,
                                JourRowId = tr.JourRowId,
                                BankLine = bst
                            };
                            this._JournalsRemoved.Add(lin);
                            tr.BankStatementLine = 0;
                        }
                        else
                            bst.Updated = true;
                        tr._StatementLines = null; tr._Reconciled = false; tr.Mark = false; tr.IsMatched = false;
                    }
                    bst._Trans = null;
                    bst.UpdateState();
                }
                bst.IsMatched = false;
            }
        }

        void setInterval()
        {
            CWInterval winInterval = new CWInterval(fromDate, toDate, DaysSlip, hideVoucher: true);
            winInterval.Closed += delegate
            {
                if (winInterval.DialogResult == true)
                {
                    fromDate = winInterval.FromDate;
                    toDate = winInterval.ToDate;
                    DaysSlip = winInterval.VarianceDays;
                    SetStatusText();
                    saveGrid(true);
                }
            };
            winInterval.Show();
        }

        void SetFilter(int step)
        {
            showAmountType = Localization.lookup("All");
            fromDate = fromDate.AddMonths(step);
            var td = toDate.AddMonths(step);
            toDate = td.AddDays(DateTime.DaysInMonth(td.Year, td.Month) - td.Day);
            SetStatusText();
            saveGrid(true);
        }

        async void saveGrid(bool doReload = false)
        {
            if (getBSLSelecteditem() != null)
                dgBankStatementLine.SelectedItem = null;

            if (doReload)
                busyIndicator.IsBusy = true;

            var err = await dgBankStatementLine.SaveData();
            if (err != ErrorCodes.Succes)
                doReload = false;
            if (BankList == null)
                return;
            List<BankStatementAPI.BankSettle> multiSettleTrans = null;
            List<BankStatementAPI.TransSettle> multiSettleBank = null;
            foreach (var bank in BankList)
            {
                if (!bank.Updated)
                    continue;

                bank.Updated = false;

                var _Trans = bank._Trans;
                if (_Trans != null && _Trans.Count == 1 && _Trans[0].StatementLines != null && _Trans[0].StatementLines.Count > 1)
                {
                    if (multiSettleBank == null)
                        multiSettleBank = new List<BankStatementAPI.TransSettle>();

                    var bs = new BankStatementAPI.TransSettle();
                    var t = _Trans[0];
                    if (t.IsCleared)
                    {
                        t.IsCleared = false;
                        bs.TransLine = t;
                        multiSettleBank.Add(bs);

                        bs = new BankStatementAPI.TransSettle();
                    }

                    bs.TransLine = t;
                    bs.BankLines = t.StatementLines;
                    t.StatementLines.ForEach(tr => tr.Updated = false);
                    multiSettleBank.Add(bs);
                }
                else
                {
                    if (multiSettleTrans == null)
                        multiSettleTrans = new List<BankStatementAPI.BankSettle>();

                    BankStatementAPI.BankSettle bp = new BankStatementAPI.BankSettle();
                    bp.BankLine = bank;
                    bp.TransLines = _Trans;
                    multiSettleTrans.Add(bp);

                    if (_Trans != null)
                    {
                        foreach (var t in _Trans)
                        {
                            if (t.IsCleared)
                            {
                                t.IsCleared = false;
                                var bs = new BankStatementAPI.TransSettle();
                                bs.TransLine = t;
                                if (multiSettleBank == null)
                                    multiSettleBank = new List<BankStatementAPI.TransSettle>();
                                multiSettleBank.Add(bs);
                            }
                        }
                    }
                }
            }
            if (multiSettleTrans != null || multiSettleBank != null || (_JournalsRemoved != null && _JournalsRemoved.Count > 0))
            {
                var bapi = new BankStatementAPI(api);

                err = 0;
                if (_JournalsRemoved != null && _JournalsRemoved.Count > 0)
                {
                    err = await bapi.RemoveJournalBank(_JournalsRemoved);
                    if (err == 0)
                        _JournalsRemoved.Clear();
                }
                if (multiSettleBank != null)
                    err = await bapi.Settle(this.master, multiSettleBank);
                if (err == 0 && multiSettleTrans != null)
                    err = await bapi.Settle(this.master, multiSettleTrans);
                if (err != 0)
                {
                    if (doReload)
                        busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(err);
                    return;
                }
            }

            if (doReload)
            {
                dgAccountsTransGrid.SetSource(null);
                dgBankStatementLine.SetSource(null);
                InitQuery();
            }
        }

        BankStatementLineGridClient getBSLSelecteditem()
        {
            return dgBankStatementLine.SelectedItem as BankStatementLineGridClient;
        }
        void SetStatusText()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            foreach (var grp in groups)
            {
                if (grp.StatusValue == "a" || grp.Caption == Localization.lookup("FromDate"))
                {
                    grp.StatusValue = fromDate.ToString("d");
                    continue;
                }
                else
                if (grp.StatusValue == "b" || grp.Caption == Localization.lookup("ToDate"))
                {
                    grp.StatusValue = toDate.ToString("d");
                }
            }
        }
        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

        List<GLTransClientTotalBank> lastSelected;
        private void dgBankStatementLine_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var selectedbsl = getBSLSelecteditem();
            var oldBsl = e.OldItem as BankStatementLineGridClient;
            if (oldBsl != null)
                oldBsl.IsMatched = false;
            lastSelected?.ForEach(ac => { ac.IsMatched = false; ac.UpdateState(); });
            if (selectedbsl == null)
                return;
            var trans = selectedbsl.Trans;
            if (trans != null && trans.Count > 0)
            {
                dgAccountsTransGrid.CurrentItem = trans[0];
                trans.ForEach(ac => { ac.IsMatched = true; ac.UpdateState(); });
                lastSelected = trans;
            }
            else
            {
                var act = dgAccountsTransGrid.CurrentItem as GLTransClientTotalBank;
                if (act != null && act.StatementLines != null)
                    dgAccountsTransGrid.CurrentItem = null;

                if (selectedbsl._InJournal)
                {
                    var lstAct = ((IEnumerable<GLTransClientTotalBank>)dgAccountsTransGrid.ItemsSource);
                    if (lstAct != null)
                    {
                        var BankStatementLine = selectedbsl.Id;
                        foreach (var r in lstAct)
                        {
                            if (r.BankStatementLine == BankStatementLine)
                            {
                                lastSelected = new List<GLTransClientTotalBank>() { r };
                                dgAccountsTransGrid.CurrentItem = r;
                                break;
                            }
                        }
                    }
                }
            }
            dgBankStatementLine.Readonly = !selectedbsl.AllowEditing;
            MarkCol.ReadOnly = false;
            //  MarkCol.AllowEditing = true;
            var ae = dgBankStatementLine.tableView?.ActiveEditor;
            if (ae != null)
                ae.IsEnabled = selectedbsl.AllowEditing;
        }

        private void CheckEditor_Checked(object sender, RoutedEventArgs e)
        {
            BankStatementClientSetMark(true);
        }

        private void CheckEditor_Unchecked(object sender, RoutedEventArgs e)
        {
            BankStatementClientSetMark(false);
        }

        void BankStatementClientSetMark(bool value)
        {
            var visibleRows = dgBankStatementLine.GetVisibleRows() as IEnumerable<BankStatementLineGridClient>;
            foreach (var row in visibleRows)
            {
                row.Mark = value;
            }
            var val = (from t in visibleRows where t._Mark select t._AmountCent).Sum();
            bankStatAmt = (val / 100d);
            this.layOutBankStat.Caption = string.Format("'{0:N2}'", bankStatAmt);
            this.layOutTrans.Caption = string.Format("'{0:N2}'    '( {1:N2} )'", transAmt, bankStatAmt - transAmt);
        }

        private void CheckEditor_Checked_1(object sender, RoutedEventArgs e)
        {
            GLTransClientTotalSetMark(true);
        }

        private void CheckEditor_Unchecked_1(object sender, RoutedEventArgs e)
        {
            GLTransClientTotalSetMark(false);
        }

        void GLTransClientTotalSetMark(bool value)
        {
            var visibleRows = dgAccountsTransGrid.GetVisibleRows() as IEnumerable<GLTransClientTotalBank>;
            foreach (var row in visibleRows)
                row.Mark = value;
            long val;
            if (ShowCurrency)
                val = (from t in visibleRows where t._Mark select t._AmountCurCent).Sum();
            else
                val = (from t in visibleRows where t._Mark select t._AmountCent).Sum();
            transAmt = (val / 100d);
            this.layOutTrans.Caption = string.Format("'{0:N2}'    '({1:N2})'", transAmt, bankStatAmt - transAmt);
        }

        List<BankStatementLineGridClient> lastSelectedbsl;
        private void dgAccountsTransGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var selectedTrans = dgAccountsTransGrid.CurrentItem as GLTransClientTotalBank;
            var oldTrans = e.OldItem as GLTransClientTotalBank;
            if (oldTrans != null)
                oldTrans.IsMatched = false;
            if (selectedTrans == null)
            {
                lastSelectedbsl?.ForEach(b => b.IsMatched = false);
                return;
            }
            var stline = selectedTrans.StatementLines;
            lastSelectedbsl?.ForEach(b => b.IsMatched = false);
            if (stline != null && stline.Count > 0)
            {
                dgBankStatementLine.CurrentItem = stline[0];
                stline.ForEach(b => b.IsMatched = true);
                lastSelectedbsl = stline;
            }
            else
            {
                var bsl = getBSLSelecteditem();
                if (bsl != null && bsl._Trans != null)
                    dgBankStatementLine.CurrentItem = null;

                if (selectedTrans.BankStatementLine != 0)
                {
                    var lstbsl = ((IEnumerable<BankStatementLineGridClient>)dgBankStatementLine.ItemsSource);
                    if (lstbsl != null)
                    {
                        var BankStatementLine = selectedTrans.BankStatementLine;
                        foreach (var r in lstbsl)
                        {
                            if (r.Id == BankStatementLine)
                            {
                                lastSelectedbsl = new List<BankStatementLineGridClient>() { r };
                                dgBankStatementLine.CurrentItem = r;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                var lstbsl = ((IEnumerable<BankStatementLineGridClient>)dgBankStatementLine.ItemsSource);
                if (lstbsl != null)
                {
                    foreach (var l in lstbsl)
                        if (l.Updated)
                            return true;
                }
                return base.IsDataChaged;
            }
        }

        private void HasOffSetAccount_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallOffsetAccount((sender as System.Windows.Controls.Image).Tag as BankStatementLineGridClient);
        }

        void CallOffsetAccount(BankStatementLineGridClient line)
        {
            if (line != null)
            {
                dgBankStatementLine.SetLoadedRow(line);
                var header = string.Format("{0}:{1} {2}", Localization.lookup("OffsetAccountTemplate"), Localization.lookup("BankStatement"), line._Account);
                AddDockItem(TabControls.GLOffsetAccountTemplate, line, header: header);
            }
        }
    }

    public class BankStatementLineGridClient : BankStatementLineClient
    {
        internal int AccountVersion;
        internal object accntSource;
        public object AccountSource { get { return accntSource; } }

        internal List<GLTransClientTotalBank> _Trans;
        internal List<GLTransClientTotalBank> Trans { get { return _Trans; } set { _Trans = value; NotifyPropertyChanged("State"); } }

        internal bool Updated;
        public void UpdateState() { NotifyPropertyChanged("State"); }

        public long _Total;

        [Display(Name = "Total", ResourceType = typeof(GLDailyJournalText))]
        public double Total { get { return _Total / 100d; } }

        [Display(Name = "Void", ResourceType = typeof(GLDailyJournalLineText))]
        public bool VoidLine { get { return _Void; } set { _Void = value; NotifyPropertyChanged("VoidLine"); NotifyPropertyChanged("State"); } }

        internal bool _IsMatched;
        public bool IsMatched { get { return _IsMatched; } set { _IsMatched = value; NotifyPropertyChanged("IsMatched"); NotifyPropertyChanged("State"); NotifyPropertyChanged("AllowEditing"); } }
        public bool AllowEditing { get { return State == 1 || _Void; } }
        public byte State { get { return _InJournal ? (byte)2 : (_Trans != null || _Primo || this._JournalPostedId != 0 ? (byte)3 : _Void ? (byte)4 : (byte)1); } }
        internal bool _Mark;
        public bool Mark { get { return _Mark; } set { if (_Mark == value) return; _Mark = value; NotifyPropertyChanged("Mark"); } }

        public bool IsNoteReadonly { get { return !AllowEditing; } }
        public bool HasOffSetAccounts { get { return _HasOffsetAccounts; } }
    }

    public class GLTransClientTotalBank : GLTransClientTotal, INotifyPropertyChanged
    {
        internal List<BankStatementLineGridClient> _StatementLines;
        public List<BankStatementLineGridClient> StatementLines { get { return _StatementLines; } set { _StatementLines = value; NotifyPropertyChanged("State"); } }
        public new byte State { get { return this.BankStatementLine != 0 ? (byte)2 : (_StatementLines != null || _Reconciled || _PrimoTrans ? (byte)3 : _Void ? (byte)4 : (byte)1); } }
        public bool AllowEditing { get { return State == 1 || _Void; } }
        internal bool _IsMatched;
        public bool IsMatched { get { return _IsMatched; } set { _IsMatched = value; NotifyPropertyChanged("IsMatched"); NotifyPropertyChanged("State"); NotifyPropertyChanged("AllowEditing"); } }
        internal bool _Mark;
        public bool Mark { get { return _Mark; } set { if (_Mark == value) return; _Mark = value; NotifyPropertyChanged("Mark"); } }
        internal bool IsCleared;



        public void UpdateState()
        {
            NotifyPropertyChanged("State");
        }
        public void UpdateVoid()
        {
            NotifyPropertyChanged("Void");
            UpdateState();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
