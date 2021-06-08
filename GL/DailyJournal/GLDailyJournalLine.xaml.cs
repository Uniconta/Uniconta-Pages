using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
using System.ComponentModel;
using Uniconta.API.GeneralLedger;
using System.Text;
using System.Threading;
using System.Windows.Threading;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using System.ComponentModel.DataAnnotations;
using System.Windows.Data;
using System.Globalization;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.Grid;
using Uniconta.Common.Utility;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLDailyJournalLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(JournalLineGridClient); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return blocked; } }
        public override bool IsAutoSave { get { return _AutoSave; } }
        internal bool _AutoSave;
        bool blocked;
        public bool Blocked { get { return blocked; } set { blocked = value; this.View.AllowEditing = !value; } }

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (JournalLineGridClient)this.SelectedItem;
            if (selectedItem == null || selectedItem.Amount == 0d || (selectedItem._Account == null && selectedItem._OffsetAccount == null))
                return false;
            return true;
        }
        internal UnicontaClient.Pages.GLDailyJournalLine GridBase;

        internal void GoToCol(string col, bool setToPrevious = false)
        {
            var column = this.Columns[col];
            if (column?.Visible == true)
            {
                var visibleIndex = column.VisibleIndex;
                if (visibleIndex > 0)
                {
                    int index = 0;
                    if (setToPrevious)
                        index = 1;/* get one column before because of Enter default movement */
                    column = this.Columns.Where(c => c.VisibleIndex == visibleIndex - index).FirstOrDefault();
                }
                this.CurrentCol = column;
            }
        }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var newRow = (JournalLineGridClient)dataEntity;
            var GridBase = this.GridBase;
            if (GridBase == null)
            {
                newRow._Date = BasePage.GetSystemDefaultDate();
                return;
            }

            var header = (Uniconta.DataModel.GLDailyJournal)this.masterRecord;

            newRow._AccountType = (byte)header._DefaultAccountType;
            newRow._Account = header._Account;
            newRow._OffsetAccountType = (byte)header._DefaultOffsetAccountType;
            newRow._OffsetAccount = header._OffsetAccount;
            if (newRow._TransType == null)
                newRow._TransType = header._TransType;
            newRow._Vat = header._Vat;
            newRow._OffsetVat = header._OffsetVat;
            newRow._Dim1 = header._Dim1;
            newRow._Dim2 = header._Dim2;
            newRow._Dim3 = header._Dim3;
            newRow._Dim4 = header._Dim4;
            newRow._Dim5 = header._Dim5;
            newRow._SettleValue = header._SettleValue;

            var TakeVoucher = !header._GenerateVoucher && !header._ManualAllocation;
            var lst = (IEnumerable<JournalLineGridClient>)this.ItemsSource;
            if (lst == null)
            {
                newRow._Date = BasePage.GetSystemDefaultDate();
            }
            else
            {
                JournalLineGridClient last = null;
                JournalLineGridClient Cur = null;
                int n = -1;
                DateTime LastDateTime = DateTime.MinValue;
                foreach (var journalLine in lst)
                {
                    if (journalLine._Date != DateTime.MinValue && Cur == null)
                        LastDateTime = journalLine._Date;
                    n++;
                    if (n == selectedIndex)
                        Cur = journalLine;
                    last = journalLine;
                }
                if (Cur == null)
                    Cur = last;

                if (Cur != null)
                    newRow.SetTraceSum(Cur._TraceSum);

                if (Cur != null && Math.Abs(GridBase.LineTotal) > 0.005d)
                {
                    newRow._Voucher = Cur._Voucher;
                    newRow._Text = Cur._Text;
                    newRow._TransType = Cur._TransType;
                    newRow._DocumentRef = Cur._DocumentRef;
                    newRow._DocumentDate = Cur._DocumentDate;
                    if (header._Account == null) // i tried this, but we got bad feedback. Erik && header._DefaultAccountType == 0)
                        newRow._AccountType = 0; // typically, if we have unbalance, then we need to add ledger account

                    if (header._GenOffsetAmount)
                    {
                        if (GridBase.LineTotal > 0d)
                            newRow._Credit = GridBase.LineTotal;
                        else
                            newRow._Debit = -GridBase.LineTotal;
                    }
                    TakeVoucher = false;

                    if (!header._ShowUnbalance)
                        newRow._Date = LastDateTime != DateTime.MinValue ? LastDateTime : BasePage.GetSystemDefaultDate();
                    if (header._JumpOnUnbalance)
                        GoToCol("Account");
                }
                else
                {
                    newRow._Date = LastDateTime != DateTime.MinValue ? LastDateTime : BasePage.GetSystemDefaultDate();
                }
            }
            if (TakeVoucher)
            {
                var dif = GridBase.maxVoucher - GridBase.NextVoucherNumber;
                if (GridBase.maxVoucher == 0 || dif < -100 || (dif >= -1 && dif < 100))
                    newRow._Voucher = GridBase.NextVoucherNumber;
                else
                    newRow._Voucher = GridBase.maxVoucher + 1;
            }

            newRow.TakeVoucher = TakeVoucher;
            newRow.SetMaster(header);

            GridBase.RecalculateSum();
        }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            if (copyFromRows.FirstOrDefault() is GLTrans)
            {
                var lst = new List<UnicontaBaseEntity>();
                foreach (var _tr in copyFromRows)
                {
                    var tr = (GLTrans)_tr;
                    lst.Add(new JournalLineGridClient()
                    {
                        _Date = tr._Date,
                        _DocumentDate = tr._DocumentDate,
                        _Account = tr._Account,
                        _Voucher = tr._Voucher,
                        _Invoice = NumberConvert.ToStringNull(tr._Invoice),
                        _VatOperation = tr._VatOperation,
                        _Vat = tr._Vat,
                        _TransType = tr._TransType,
                        _Text = tr._Text,
                        _Currency = tr._Currency,
                        _DocumentRef = tr._DocumentRef,
                        _Dim1 = tr._Dimension1,
                        _Dim2 = tr._Dimension2,
                        _Dim3 = tr._Dimension3,
                        _Dim4 = tr._Dimension4,
                        _Dim5 = tr._Dimension5,
                        Amount = tr._Amount,
                        AmountCur = tr._AmountCur
                    });
                }
                return lst;
            }
            if (copyFromRows.FirstOrDefault() is Document)
            {
                var lst = new List<UnicontaBaseEntity>();
                foreach (var _tr in copyFromRows)
                {
                    var tr = (Document)_tr;
                    var rec = new JournalLineGridClient()
                    {
                        _Account = tr._CostAccount,
                        _DocumentDate = tr._DocumentDate,
                        _Date = tr._PostingDate != DateTime.MinValue ? tr._PostingDate : tr._DocumentDate,
                        _Voucher = tr._Voucher,
                        _Invoice = tr._Invoice,
                        _VatOperation = tr._VatOperation,
                        _Vat = tr._Vat,
                        _TransType = tr._TransType,
                        _Text = tr._Text,
                        _DocumentRef = tr.RowId,
                        _PaymentId = tr._PaymentId,
                        _PaymentMethod = tr._PaymentMethod,
                        _Dim1 = tr._Dim1,
                        _Dim2 = tr._Dim2,
                        _Dim3 = tr._Dim3,
                        _Dim4 = tr._Dim4,
                        _Dim5 = tr._Dim5,
                    };
                    if (tr._CreditorAccount != null)
                    {
                        rec._OffsetAccount = tr._CreditorAccount;
                        rec._OffsetAccountType = 2;
                    }
                    if (tr._Currency > 0 && tr._Currency != api.CompanyEntity._Currency)
                    {
                        rec._Currency = tr._Currency;
                        rec.DebitCur = tr._Amount;
                    }
                    else
                        rec._Debit = tr._Amount;
                    lst.Add(rec);
                }
                return lst;
            }
            return null;
        }

        public override DataTemplate PrintGridFooter(ref object FooterData)
        {
            FooterData = this.FooterData;
            if (FooterData != null)
            {
                var footerTemplate = Page.Resources["ReportFooterTemplate"] as DataTemplate;
                return footerTemplate;
            }
            return null;
        }

        public object FooterData { get; set; }
        public override string SetColumnTooltip(object row, ColumnBase col)
        {
            var jline = row as JournalLineGridClient;
            if (jline != null && col != null)
            {
                switch (col.FieldName)
                {
                    case "Account": return jline.AccountName;
                    case "OffsetAccount": return jline.OffsetAccountName;
                }
            }
            return base.SetColumnTooltip(row, col);
        }
        protected override bool RenderAllColumns { get { return true; } }
    }

    public partial class GLDailyJournalLine : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GL_DailyJournalLine; } }

        Uniconta.DataModel.GLDailyJournal masterRecord;
        NumberSerieAPI numberserieApi;
        int LastVoucher, FirstVoucher;
        internal int NextVoucherNumber, maxVoucher;
        internal double LineTotal;
        bool TwoVatCodes, UseDCVat, NoVATCalculation;

        const int MaxTraceAccount = 6;
        int ActiveTraceAccount;
        internal string[] TraceAccount;
        bool RoundTo100, anyChange;

        public GLDailyJournalLine(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }

        public GLDailyJournalLine(UnicontaBaseEntity master)
            : base(master)
        {
            Init();
            SetJournal(master as Uniconta.DataModel.GLDailyJournal);
        }

        void Init()
        {
            InitializeComponent();
            dgGLDailyJournalLine.api = api;
            dgGLDailyJournalLine.GridBase = this;

            api.AllowBackgroundCrud = true;
            var Comp = api.CompanyEntity;
            RoundTo100 = Comp.RoundTo100;
            if (RoundTo100)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = false;

            if (!Comp._HasWithholding)
                Withholding.Visible = Withholding.ShowInColumnChooser = false;
            else
                Withholding.ShowInColumnChooser = true;

            if (!Comp._UseVatOperation)
                VatOperation.Visible = VatOffsetOperation.Visible = false;
            else
                VatOffsetOperation.Visible = true;

            if (!Comp.Project)
            {
                colPrCategory.Visible = colPrCategory.ShowInColumnChooser = false;
                colProject.Visible = colProject.ShowInColumnChooser = false;
                colProjectText.Visible = colProjectText.ShowInColumnChooser = false;
            }
            else
            {
                colPrCategory.ShowInColumnChooser = true;
                colProject.ShowInColumnChooser = true;
                colProjectText.ShowInColumnChooser = true;
            }

            if (!Comp.ProjectTask)
            {
                Task.Visible = false;
                Task.ShowInColumnChooser = false;
            }
            else
                Task.ShowInColumnChooser = true;

            numberserieApi = new NumberSerieAPI(api);

            localMenu.dataGrid = dgGLDailyJournalLine;
            SetRibbonControl(localMenu, dgGLDailyJournalLine);
            dgGLDailyJournalLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGLDailyJournalLine.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;

            LedgerCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount));
            VatCache = Comp.GetCache(typeof(Uniconta.DataModel.GLVat));
            DebtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor));
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));

#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += RootVisual_KeyDown;
#else
            this.PreviewKeyDown += RootVisual_KeyDown;
#endif
            this.BeforeClose += GLDailyJournalLine_BeforeClose;
        }

        void SetJournal(Uniconta.DataModel.GLDailyJournal masterRecord)
        {
            if (masterRecord == null)
                throw new UnicontaException("Journal need a master");
            this.masterRecord = masterRecord;
            dgGLDailyJournalLine._AutoSave = masterRecord._AutoSave;
            dgGLDailyJournalLine.Blocked = masterRecord._Blocked;
            dgGLDailyJournalLine.UpdateMaster(masterRecord);
            if (masterRecord._Journal != null)
                Layout._SubId = masterRecord._Journal.GetHashCode();  // this way we have the same layout across companies if the journal has the same name.
            UseDCVat = masterRecord._UseDCVat;

            if (masterRecord._TraceAccount != null || masterRecord._TraceAccount2 != null)
            {
                TraceAccount = new string[MaxTraceAccount];
                TraceAccount[0] = masterRecord._TraceAccount;
                TraceAccount[1] = masterRecord._TraceAccount2;
                TraceAccount[2] = masterRecord._TraceAccount3;
                TraceAccount[3] = masterRecord._TraceAccount4;
                TraceAccount[4] = masterRecord._TraceAccount5;
                TraceAccount[5] = masterRecord._TraceAccount6;

                ActiveTraceAccount = MaxTraceAccount;
                while (TraceAccount[ActiveTraceAccount - 1] == null)
                {
                    ActiveTraceAccount--;
                    if (ActiveTraceAccount == 0)
                        break;
                }
            }

            this.NoVATCalculation = masterRecord._NoVATCalculation;
            if (!masterRecord._TwoVatCodes)
                OffsetVat.Visible = VatOffsetOperation.Visible = VatAmountOffset.Visible = false;
            else
                TwoVatCodes = true;
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (string.Compare(rec.Name, "Journal", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    var cache = api.GetCache(typeof(Uniconta.DataModel.GLDailyJournal)) ?? api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal)).GetAwaiter().GetResult();
                    SetJournal((Uniconta.DataModel.GLDailyJournal)cache.Get(rec.Value));
                }
            }
            base.SetParameter(Parameters);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (TabControls.SettleOpenTransactionPage == screenName)
            {
                var obj = argument as object[];
                if (obj != null && obj.Length == 6)
                    SetSettlementsForJournalLine(obj[0] as JournalLineGridClient, obj[1] as string, (double)obj[2], (double)obj[3], (string)obj[4], (bool)obj[5]);
            }

            if (screenName == TabControls.GLOffsetAccountTemplate && argument != null)
            {
                dgGLDailyJournalLine.UpdateItemSource(argument);
                RecalculateSum();
            }

            if (screenName == TabControls.AttachVoucherGridPage && argument != null)
            {
                var voucherObj = argument as object[];
                SetAttachedVoucherForJournalLine(voucherObj[0] as VouchersClient);
            }
        }

        private void GLDailyJournalLine_BeforeClose()
        {
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown -= RootVisual_KeyDown;
#else
            this.PreviewKeyDown -= RootVisual_KeyDown;
#endif
            var lines = dgGLDailyJournalLine.ItemsSource as IList;
            int cnt = lines != null ? lines.Count : 0;
            var mClient = masterRecord as GLDailyJournalClient;
            if (mClient != null)
                mClient.NumberOfLines = cnt;
            else
                masterRecord._NumberOfLines = cnt;
        }

        private void RootVisual_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
            {
                var isShiftPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                var isCtrlPressed = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
                if (isShiftPressed)
                    localMenu_OnItemClicked("Account");
                else if (isCtrlPressed)
                    localMenu_OnItemClicked("OffsetAccount");
                else
                {
                    dgGLDailyJournalLine.tableView.CloseEditor();
                    localMenu_OnItemClicked("OpenTran");
                }
            }
            else if (e.Key == Key.F5)
            {
                if (dgGLDailyJournalLine.CurrentColumn == HasVoucher)
                {
                    int focusRowHandle = dgGLDailyJournalLine.View.FocusedRowHandle;
                    int selectedIndex = dgGLDailyJournalLine.GetRowVisibleIndexByHandle(focusRowHandle);
                    if (selectedIndex > 0)
                    {
                        var upperline = dgGLDailyJournalLine.GetRow(focusRowHandle - 1) as JournalLineGridClient;
                        var currentRow = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
                        if (currentRow != null && upperline != null)
                        {
                            dgGLDailyJournalLine.SetLoadedRow(currentRow);
                            currentRow.DocumentRef = upperline.DocumentRef;
                            currentRow.NotifyPropertyChanged("DocumentRef");
                            dgGLDailyJournalLine.SetModifiedRow(currentRow);
                        }
                    }
                }
            }
#if !SILVERLIGHT
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                if (dgGLDailyJournalLine.CurrentColumn.Name == "HasOffsetAccounts" && e.Key == Key.Down)
                {
                    var currentRow = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
                    e.Handled = true;
                    if (currentRow != null)
                        CallOffsetAccount(currentRow);
                }
            }
#endif
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);

            var masterRecord = this.masterRecord;
            var DateVisible = (masterRecord._DateFunction == GLJournalDate.Free);
            var VoucherVisible = !masterRecord._GenerateVoucher;
            var TraceVisible = false;
            if (masterRecord._TraceAccount != null)
            {
                if (LedgerCache != null && LedgerCache.Get(masterRecord._TraceAccount) == null)
                    masterRecord._TraceAccount = null;
                else
                    TraceVisible = true;
            }

            Approved.Visible = masterRecord._UseApproved;
            Approved.ShowInColumnChooser = masterRecord._UseApproved;

            colDate.Visible = DateVisible;
            colDate.ShowInColumnChooser = DateVisible;

            colVoucher.Visible = VoucherVisible;
            colVoucher.ShowInColumnChooser = VoucherVisible;

            this.TraceBalance.Visible = TraceVisible;
            this.TraceBalance.ShowInColumnChooser = TraceVisible;

            if (dgGLDailyJournalLine.IsLoadedFromLayoutSaved)
            {
                dgGLDailyJournalLine.ClearSorting();
                dgGLDailyJournalLine.ClearFilter();
                dgGLDailyJournalLine.IsLoadedFromLayoutSaved = false;
            }
        }

        static string formatAcc(GLAccount ac) { return string.Concat("| ", ac._Account, ", ", ac._Name); }

        void setTraceAccount()
        {
            var TraceAccount = this.TraceAccount;
            if (TraceAccount == null)
                return;

            var Ledger = this.LedgerCache;

            var ac = (GLAccount)Ledger.Get(TraceAccount[0]);
            if (ac != null)
                tracAc1.Text = formatAcc(ac);
            else
                tracAc1.Visibility = Visibility.Collapsed;
            ac = (GLAccount)Ledger.Get(TraceAccount[1]);
            if (ac != null)
                tracAc2.Text = formatAcc(ac);
            else
                tracAc2.Visibility = Visibility.Collapsed;
            ac = (GLAccount)Ledger.Get(TraceAccount[2]);
            if (ac != null)
                tracAc3.Text = formatAcc(ac);
            else
                tracAc3.Visibility = Visibility.Collapsed;
            ac = (GLAccount)Ledger.Get(TraceAccount[3]);
            if (ac != null)
                tracAc4.Text = formatAcc(ac);
            else
                tracAc4.Visibility = Visibility.Collapsed;
            ac = (GLAccount)Ledger.Get(TraceAccount[4]);
            if (ac != null)
                tracAc5.Text = formatAcc(ac);
            else
                tracAc5.Visibility = Visibility.Collapsed;
            ac = (GLAccount)Ledger.Get(TraceAccount[5]);
            if (ac != null)
                tracAc6.Text = formatAcc(ac);
            else
                tracAc6.Visibility = Visibility.Collapsed;
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            JournalLineGridClient oldselectedItem = e.OldItem as JournalLineGridClient;
            if (oldselectedItem != null)
            {
                oldselectedItem.PropertyChanged -= JournalLineGridClient_PropertyChanged;
                TestForTakeVoucher(oldselectedItem);

                var Cache = GetCache(oldselectedItem._AccountType);
                if (Cache != null && Cache.Count > 1000)
                {
                    oldselectedItem.accntSource = null;
                    oldselectedItem.NotifyPropertyChanged("AccountSource");
                }
                Cache = GetCache(oldselectedItem._OffsetAccountType);
                if (Cache != null && Cache.Count > 1000)
                {
                    oldselectedItem.offsetAccntSource = null;
                    oldselectedItem.NotifyPropertyChanged("OffsetAccountSource");
                }
            }

            JournalLineGridClient selectedItem = e.NewItem as JournalLineGridClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += JournalLineGridClient_PropertyChanged;

                //if (selectedItem.accntSource == null)
                //    SetAccountSource(selectedItem);
                //if (selectedItem.offsetAccntSource == null)
                //    SetOffsetAccountSource(selectedItem);
            }
        }

        SQLCache LedgerCache, DebtorCache, CreditorCache, VatCache, PaymentCache, TextTypes, credPaymFormatCache, ProjectCache, AssetCache, AssetGroupCache;

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            if (VatCache == null)
                VatCache = await api.LoadCache(typeof(Uniconta.DataModel.GLVat)).ConfigureAwait(false);
            if (DebtorCache == null)
                DebtorCache = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);

            PaymentCache = Comp.GetCache(typeof(Uniconta.DataModel.PaymentTerm)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PaymentTerm)).ConfigureAwait(false);
            TextTypes = Comp.GetCache(typeof(Uniconta.DataModel.GLTransType)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLTransType)).ConfigureAwait(false);
            credPaymFormatCache = Comp.GetCache(typeof(Uniconta.DataModel.CreditorPaymentFormat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CreditorPaymentFormat)).ConfigureAwait(false);

            var header = this.masterRecord;
            if (header._Account != null)
            {
                var rec = GetCache((byte)header._DefaultAccountType)?.Get(header._Account);
                if (rec == null)
                    header._Account = null;
            }
            if (header._OffsetAccount != null)
            {
                var rec = GetCache((byte)header._DefaultOffsetAccountType)?.Get(header._OffsetAccount);
                if (rec == null)
                    header._OffsetAccount = null;
            }
            if (Comp.ProjectTask)
                ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
        }

        public void TestForTakeVoucher(JournalLineGridClient row)
        {
            if (row == null || masterRecord._ManualAllocation)
                return;
            if ((row._Voucher == NextVoucherNumber || row._Voucher == 0) && (row.Amount != 0d))
            {
                if (row.TakeVoucher)
                {
                    if (this.FirstVoucher == 0)
                        this.FirstVoucher = row._Voucher;
                    row.TakeVoucher = false;
                    LastVoucher = NextVoucherNumber;
                    NextVoucherNumber++;
                }
                if (LastVoucher != 0)
                    row.Voucher = LastVoucher;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;

            switch (ActionType)
            {
                case "AddRow":
                    dgGLDailyJournalLine.AddRow();
                    break;
                case "CopyRow":
                    if (selectedItem != null)
                    {
                        var val = selectedItem.Amount;
                        dgGLDailyJournalLine.CopyRow();
                        if (val != 0d)
                            RecalculateSum();
                    }
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                    {
                        var val = selectedItem.Amount;
                        dgGLDailyJournalLine.DeleteRow();
                        if (val != 0d)
                            RecalculateSum();
                    }
                    break;
                case "CheckJournal":
                    CheckJournal();
                    break;
                case "PostJournal":
                    PostJournal();
                    break;
                case "TransType":
                    AddDockItem(TabControls.GLTransTypePage, null, Uniconta.ClientTools.Localization.lookup("TransTypes"));
                    break;
                case "AppendTransType":
                    CWAppendEnumeratedLabel dialog = new CWAppendEnumeratedLabel(api);
                    dialog.Show();
                    break;
                case "AllFields":
                    AddDockItem(TabControls.GLDailyJournalLinePage2, dgGLDailyJournalLine.syncEntity, true);
                    break;
                case "DefaultValues":
                    AddDockItem(TabControls.GLDailyJournalPage2, masterRecord, Uniconta.ClientTools.Localization.lookup("Defaultvalues"));
                    break;
                case "RefVoucher":
                    var source = (IEnumerable<JournalLineGridClient>)dgGLDailyJournalLine.ItemsSource;
                    if (source != null)
                    {
                        var _refferedVouchers = new List<int>();
                        foreach (var journalLine in source)
                            if (journalLine._DocumentRef != 0)
                                _refferedVouchers.Add(journalLine._DocumentRef);

                        AddDockItem(TabControls.AttachVoucherGridPage, new object[] { _refferedVouchers }, true);
                    }
                    break;
                case "ViewVoucher":
                    if (selectedItem == null)
                        return;
                    dgGLDailyJournalLine.syncEntity.Row = selectedItem;
                    dgGLDailyJournalLine.syncEntity.AllowEmpty = true;
                    busyIndicator.IsBusy = true;
                    ViewVoucher(TabControls.VouchersPage3, dgGLDailyJournalLine.syncEntity);
                    busyIndicator.IsBusy = false;
                    break;
                case "DragDrop":
                case "ImportVoucher":
                    if (selectedItem != null)
                    {
                        dgGLDailyJournalLine.SetLoadedRow(selectedItem);
                        AddVoucher(selectedItem, ActionType);
                    }
                    break;

                case "RemoveVoucher":
                    RemoveVoucher(selectedItem);
                    break;
                case "OffsetAccount":
                case "Account":
                case "OpenTran":
                case "All":
                    if (selectedItem == null)
                        return;

                    GLJournalAccountType AcType;
                    string Ac;
                    bool Offset = false;
                    bool showOpen = (ActionType == "OpenTran");

                    if (ActionType == "Account")
                    {
                        Ac = selectedItem._Account;
                        AcType = selectedItem._AccountTypeEnum;
                    }
                    else if (ActionType == "OffsetAccount")
                    {
                        Ac = selectedItem._OffsetAccount;
                        AcType = selectedItem._OffsetAccountTypeEnum;
                        Offset = true;
                    }
                    else if (selectedItem._AccountType != (byte)GLJournalAccountType.Finans)
                    {
                        Ac = selectedItem._Account;
                        AcType = selectedItem._AccountTypeEnum;
                    }
                    else if (selectedItem._OffsetAccountType != (byte)GLJournalAccountType.Finans)
                    {
                        Ac = selectedItem._OffsetAccount;
                        AcType = selectedItem._OffsetAccountTypeEnum;
                        Offset = true;
                    }
                    else
                        break;

                    if (AcType == GLJournalAccountType.Finans)
                    {
                        var AccountObj = LedgerCache.Get(Ac);
                        if (AccountObj != null)
                            AddDockItem(TabControls.TransactionReport, AccountObj, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("AccountsTransaction"), Ac));
                    }
                    else
                    {
                        var AccountObj = AcType == GLJournalAccountType.Debtor ? DebtorCache.Get(Ac) : CreditorCache.Get(Ac);
                        if (AccountObj != null)
                        {
                            if (showOpen)
                            {
                                var lst = GetOtherMarked(selectedItem, Ac, (byte)AcType);
                                AddDockItem(TabControls.SettleOpenTransactionPage, new object[] { AccountObj, IdObject.get((byte)AcType), selectedItem, IdObject.get(Offset), lst }, true);
                            }
                            else
                                AddDockItem(AcType == GLJournalAccountType.Debtor ? TabControls.DebtorTransactions : TabControls.CreditorTransactions, AccountObj, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("AccountsTransaction"), Ac));
                        }
                    }
                    break;
                case "CreatePaymentsFile":
                    CreatePaymentFile();
                    break;
                case "OffSetAccount":
                    CallOffsetAccount(selectedItem);
                    break;
                case "InvertSign":
                    SetInvertSign();
                    break;
                case "RenumberVoucher":
                    RecalculateSum(true);
                    break;
                case "SetText":
                    SetTextValue();
                    break;
                case "SetDate":
                    SetDateValue();
                    break;
                case "SetAmountZero":
                    SetAmountToZero();
                    break;
                case "RemoveAllVouchers":
                    RemoveAllVouchers();
                    break;
                case "UndoDelete":
                    dgGLDailyJournalLine.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void AddVoucher(JournalLineGridClient journalLine, string actionType)
        {
#if !SILVERLIGHT
            if (actionType == "DragDrop")
            {
                var dragDropWindow = new UnicontaDragDropWindow(false);
                dragDropWindow.Closed += delegate
                {
                    if (dragDropWindow.DialogResult == true)
                    {
                        var fileInfo = dragDropWindow.FileInfoList?.FirstOrDefault();
                        if (fileInfo != null)
                        {
                            var voucher = new VouchersClient();
                            voucher._Data = fileInfo.FileBytes;
                            voucher._Text = fileInfo.FileName;
                            voucher._Fileextension = DocumentConvert.GetDocumentType(fileInfo.FileExtension);
                            Utility.ImportVoucher(journalLine, api, voucher, true);
                        }
                    }
                };
                dragDropWindow.Show();
            }
            else
#endif
                Utility.ImportVoucher(journalLine, api, null, false);
        }
        void RemoveAllVouchers()
        {
            var journalLines = dgGLDailyJournalLine.GetVisibleRows() as IEnumerable<JournalLineGridClient>;
            foreach (var line in journalLines)
            {
                if (line == null)
                    continue;

                dgGLDailyJournalLine.SetLoadedRow(line);
                var propertyInfo = line.GetType().GetProperty("DocumentRef");
                if (propertyInfo != null)
                {
                    int docRef = (int)propertyInfo.GetValue(line, null);
                    if (docRef != 0)
                        propertyInfo.SetValue(line, 0, null);
                }

                dgGLDailyJournalLine.SetModifiedRow(line);
            }
            isDataChaged = true;
        }

        void SetAmountToZero()
        {
            var journalLines = dgGLDailyJournalLine.GetVisibleRows() as IEnumerable<JournalLineGridClient>;
            foreach (var line in journalLines)
            {
                dgGLDailyJournalLine.SetLoadedRow(line);
                line.Amount = 0.0d;
                dgGLDailyJournalLine.SetModifiedRow(line);
            }
            isDataChaged = true;
        }

        void SetDateValue()
        {
            var cw = new CWSetDate(true);
            cw.Closing += delegate
            {
                if (cw.DialogResult == true)
                {
                    var days = cw.Days;
                    var date = cw.Date;
                    var journalLines = dgGLDailyJournalLine.GetVisibleRows() as IEnumerable<JournalLineGridClient>;
                    foreach (var line in journalLines)
                    {
                        dgGLDailyJournalLine.SetLoadedRow(line);
                        if (date != DateTime.MinValue)
                            line.Date = date;
                        if (days != 0)
                            line.Date = line._Date.AddDays(days);
                        dgGLDailyJournalLine.SetModifiedRow(line);
                    }
                    isDataChaged = true;
                }
            };
            cw.Show();
        }

        void SetTextValue()
        {
            var cw = new CWSetDate();
            cw.Closing += delegate
            {
                if (cw.DialogResult == true)
                {
                    var txt = cw.Text;
                    if (!string.IsNullOrEmpty(txt))
                    {
                        var journalLines = dgGLDailyJournalLine.GetVisibleRows() as IEnumerable<JournalLineGridClient>;
                        foreach (var line in journalLines)
                        {
                            if (line._Text != txt)
                            {
                                dgGLDailyJournalLine.SetLoadedRow(line);
                                line.Text = txt;
                                dgGLDailyJournalLine.SetModifiedRow(line);
                            }
                        }
                        isDataChaged = true;
                    }
                }
            };
            cw.Show();
        }

        void SetInvertSign()
        {
            var journalLines = dgGLDailyJournalLine.GetVisibleRows() as IEnumerable<JournalLineGridClient>;
            foreach (var line in journalLines)
            {
                dgGLDailyJournalLine.SetLoadedRow(line);
                var debit = line._Debit;
                var credit = line._Credit;
                if (line._Currency != 0)
                {
                    var debitCur = line._DebitCur;
                    line._DebitCur = line._CreditCur;
                    line._CreditCur = debitCur;
                    line.NotifyPropertyChanged("DebitCur");
                    line.NotifyPropertyChanged("CreditCur");
                    line.NotifyPropertyChanged("AmountCur");
                }
                line.Debit = credit;
                line.Credit = debit;
                if (line._HasOffsetAccounts)
                {
                    var offlist = line.GetOffsetAccount(typeof(GLOffsetAccountLine));
                    foreach (var lin in offlist)
                        lin._Amount *= -1;
                    line.SetOffsetAccount(offlist);
                }
                dgGLDailyJournalLine.SetModifiedRow(line);
            }
            isDataChaged = true;
        }
        bool isDataChaged;
        public override bool IsDataChaged
        {
            get
            {
                if (isDataChaged)
                    return true;
                return base.IsDataChaged;
            }
        }

        private void SetAttachedVoucherForJournalLine(VouchersClient vouchersClient)
        {
            var selectedItem = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
            if (selectedItem != null && vouchersClient != null)
            {
                dgGLDailyJournalLine.SetLoadedRow(selectedItem);
                selectedItem.DocumentRef = vouchersClient.RowId;
                if (vouchersClient._Invoice != null)
                    selectedItem.Invoice = vouchersClient._Invoice;
                selectedItem.DocumentDate = vouchersClient._DocumentDate;
                if (selectedItem._Text == null && selectedItem._TransType == null && selectedItem._AccountType == 0 && selectedItem._OffsetAccountType == 0)
                    selectedItem.Text = vouchersClient._Text;
                if (selectedItem.Amount == 0d || selectedItem.AmountSetBySystem)
                {
                    selectedItem.AmountSetBySystem = true;
                    selectedItem.Amount = vouchersClient._Amount;
                }
                dgGLDailyJournalLine.SetModifiedRow(selectedItem);
            }
        }

        static bool showDif(double settle, double journal)
        {
            if (Math.Abs(settle + journal) < 0.01) // they have different signs
                return false;
            return UnicontaMessageBox.Show( string.Format(Uniconta.ClientTools.Localization.lookup("SumSettleDif"), settle.ToString("N2"), journal.ToString("N2")) + "\n" +
                Uniconta.ClientTools.Localization.lookup("UseSettleAmount") + " ?",
                Uniconta.ClientTools.Localization.lookup("Settlement"), UnicontaMessageBox.YesNo) == UnicontaMessageBox.Yes;
        }

        private void SetSettlementsForJournalLine(JournalLineGridClient selectedItem, string settlementStr, double MarkedRemainingAmt, double MarkedRemainingAmtCur, string Currency, bool Offset)
        {
            dgGLDailyJournalLine.SetLoadedRow(selectedItem);
            if (Currency != null)
            {
                if (selectedItem.AmountSetBySystem || (selectedItem.Amount == 0d && selectedItem.AmountCur == 0d) || showDif(MarkedRemainingAmtCur, selectedItem.AmountCur))
                {
                    selectedItem.AmountSetBySystem = true;
                    if (Currency != null)
                        selectedItem.Currency = Currency;
                    selectedItem.AmountCur = Offset ? MarkedRemainingAmtCur : -MarkedRemainingAmtCur;
                }
            }
            else if (selectedItem.AmountSetBySystem || selectedItem.Amount == 0d || showDif(MarkedRemainingAmt, selectedItem.Amount))
            {
                selectedItem.AmountSetBySystem = true;
                selectedItem.Amount = Offset ? MarkedRemainingAmt : -MarkedRemainingAmt;
            }
            //Parsing the Settlement string
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
                    selectedItem.Invoice = settle;
                    selectedItem.Settlements = null;
                }
                else
                {
                    if (settleType != selectedItem._SettleValue)
                    {
                        selectedItem._SettleValue = settleType;
                        selectedItem.NotifyPropertyChanged("SettleValue");
                    }
                    selectedItem.Settlements = settle;
                    selectedItem.Invoice = null;
                }
            }
            else
            {
                selectedItem.Settlements = null;
                selectedItem.Invoice = null;
            }
            dgGLDailyJournalLine.SetModifiedRow(selectedItem);
        }
        protected override Task<ErrorCodes> saveGrid()
        {
            if (dgGLDailyJournalLine.HasUnsavedData)
                anyChange = true;

            JournalLineGridClient selectedItem = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
            if (selectedItem != null)
            {
                var TakeVoucher = selectedItem.TakeVoucher;
                selectedItem.TakeVoucher = false;
                dgGLDailyJournalLine.SelectedItem = null;
                dgGLDailyJournalLine.SelectedItem = selectedItem;
                selectedItem.TakeVoucher = TakeVoucher;
                TestForTakeVoucher(selectedItem);
            }
            return dgGLDailyJournalLine.SaveData();
        }

        List<string> GetOtherMarked(JournalLineGridClient curlin, string acc, byte accType)
        {
            var gridItems = (IEnumerable<JournalLineGridClient>)dgGLDailyJournalLine.ItemsSource;
            if (gridItems == null)
                return null;
            List<string> lines = null;
            foreach (var lin in gridItems)
            {
                if ((lin._Settlements != null || lin._Invoice != null) &&
                   ((lin._AccountType == accType && lin._Account == acc) ||
                    (lin._OffsetAccountType == accType && lin._OffsetAccount == acc)))
                {
                    if (!object.ReferenceEquals(lin, curlin))
                    {
                        if (lines == null)
                            lines = new List<string>();
                        if (lin._Settlements != null)
                            lines.Add(lin._Settlements);
                        else if (lin._Invoice != null)
                            lines.Add(lin._Invoice);
                    }
                }
            }
            return lines;
        }

        private async void CheckJournal()
        {
            var source = (ICollection<JournalLineGridClient>)dgGLDailyJournalLine.ItemsSource;
            if (source == null)
                return;
            var cnt = source.Count;
            if (cnt == 0)
                return;

            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
            busyIndicator.IsBusy = true;

            var Err = await saveGrid();
            if (Err != 0)
            {
                busyIndicator.IsBusy = false;
                return;
            }

            IEnumerable<Uniconta.DataModel.GLDailyJournalLine> glLines = null;
            if (anyChange && cnt < 2000)
                glLines = source;

            var postingApi = new PostingAPI(api);
            var postingRes = await postingApi.CheckDailyJournal(masterRecord, BasePage.GetSystemDefaultDate(), false, null, cnt, false, glLines);
            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

            if (postingRes.Err == ErrorCodes.Succes)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("JournalOK"), Uniconta.ClientTools.Localization.lookup("Message"));
                return;
            }
            Utility.ShowJournalError(postingRes, dgGLDailyJournalLine);
        }

        private void PostJournal()
        {
            var source = (ICollection<JournalLineGridClient>)dgGLDailyJournalLine.ItemsSource;
            if (source == null || source.Count == 0)
                return;

            var saveTask = saveGrid();

            string dateMsg;
            if (masterRecord._DateFunction == GLJournalDate.Free)
            {
                DateTime smallestDate = DateTime.MaxValue;
                DateTime largestDate = DateTime.MinValue;
                foreach (var rec in source)
                {
                    var dt = rec._Date;
                    if (dt != DateTime.MinValue)
                    {
                        if (dt < smallestDate)
                            smallestDate = dt;
                        if (dt > largestDate)
                            largestDate = dt;
                    }
                }
                dateMsg = string.Format(Uniconta.ClientTools.Localization.lookup("PostingDateMsg"), smallestDate.ToShortDateString(), largestDate.ToShortDateString());
            }
            else
                dateMsg = null;
            CWPosting postingDialog = new CWPosting(masterRecord, api.CompanyEntity.Name, dateMsg);
#if !SILVERLIGHT
            postingDialog.DialogTableId = 2000000014;
#endif
            postingDialog.Closed += async delegate
            {
                if (postingDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;

                    var Err = await saveTask;
                    if (Err != 0)
                    {
                        busyIndicator.IsBusy = false;
                        return;
                    }

                    var cnt = source.Count;
                    IEnumerable<Uniconta.DataModel.GLDailyJournalLine> glLines = null;
                    if (anyChange && cnt < 2000)
                        glLines = source;

                    Task<PostingResult> task;
                    var postingApi = new PostingAPI(api);

                    if (postingDialog.IsSimulation)
                        task = postingApi.CheckDailyJournal(masterRecord, postingDialog.PostedDate, true, new GLTransClientTotal(), cnt, false, glLines);
                    else
                        task = postingApi.PostDailyJournal(masterRecord, postingDialog.PostedDate, postingDialog.comments, cnt, false, glLines);

                    var postingResult = await task;

                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                    if (postingResult == null)
                        return;

                    if (postingResult.Err != ErrorCodes.Succes)
                    {
                        if (postingResult.Err == ErrorCodes.NoLinesFound && masterRecord._UseApproved)
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("TransactionsNotApproved"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                        else
                            Utility.ShowJournalError(postingResult, dgGLDailyJournalLine);
                    }

                    else if (postingResult.SimulatedTrans != null && postingResult.SimulatedTrans.Length > 0)
                    {
                        AddDockItem(TabControls.SimulatedTransactions, new object[] { postingResult.AccountBalance, postingResult.SimulatedTrans }, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                    }
                    else
                    {
                        // everything was posted fine
                        string msg;
                        if (postingResult.JournalPostedlId != 0)
                            msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), NumberConvert.ToString(postingResult.JournalPostedlId));
                        else
                            msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));

                        if (masterRecord._DeleteLines)
                        {
                            var EmptyAccountOnHold = masterRecord._EmptyAccountOnHold;
                            var UseApproved = masterRecord._UseApproved;
                            var lst = new List<JournalLineGridClient>();
                            foreach (var journalLine in source)
                                if (journalLine._OnHold || (journalLine._Account == null && EmptyAccountOnHold) || (UseApproved && !journalLine._Approved))
                                    lst.Add(journalLine);

                            dgGLDailyJournalLine.ItemsSource = lst;
                            RecalculateSum();
                        }
                        if (this.TraceAccount != null)
                            LedgerCache.IsUpdated = true; // this will reload account.
                    }
                }
            };
            postingDialog.Show();
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgGLDailyJournalLine.Filter(propValuePair);
        }

        public override async Task InitQuery()
        {
            await Filter(null);

            var itemSource = (IList<JournalLineGridClient>)dgGLDailyJournalLine.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgGLDailyJournalLine.AddFirstRow();
            else
            {
                this.FirstVoucher = itemSource[0]._Voucher;
                dgGLDailyJournalLine.SelectedItem = dgGLDailyJournalLine.GetRow(dgGLDailyJournalLine.GetRowHandleByListIndex(itemSource.Count - 1));
            }

            if (LedgerCache == null || LedgerCache.IsUpdated)
                LedgerCache = await api.LoadCache(typeof(GLAccount), (LedgerCache != null));

            setTraceAccount();
            RecalculateSum();

            var header = this.masterRecord;
            if (!header._GenerateVoucher && !header._ManualAllocation)
            {
                if (NextVoucherNumber == 0)
                    NextVoucherNumber = (int)await numberserieApi.ViewNextNumber(header._NumberSerie);

                if (NextVoucherNumber != 0 && this.maxVoucher != 0)
                {
                    if (this.LineTotal == 0)
                    {
                        if (NextVoucherNumber <= this.maxVoucher)
                            NextVoucherNumber = this.maxVoucher + 1;
                    }
                    else if (NextVoucherNumber < this.maxVoucher)
                        NextVoucherNumber = this.maxVoucher;
                }

                if (itemSource.Count == 1)
                {
                    var row = itemSource.First();
                    if (row._Voucher == 0)
                        row._Voucher = NextVoucherNumber;
                }
            }

            if (header._DefaultAccountType == GLJournalAccountType.Finans && header._Account != null)
            {
                var ac = (GLAccount)LedgerCache.Get(header._Account);
                if (ac != null && ac._Vat != null)
                    header._Vat = ac._Vat;
            }

            header._OffsetVat = null;
            if (header._DefaultOffsetAccountType == GLJournalAccountType.Finans && header._OffsetAccount != null)
            {
                var ac = (GLAccount)LedgerCache.Get(header._OffsetAccount);
                if (ac != null && ac._Vat != null)
                {
                    if (TwoVatCodes)
                        header._OffsetVat = ac._Vat;
                    else
                        header._Vat = ac._Vat;
                }
            }
        }

        void AddTraceValues(string Account, double Amount, double Vat, double[] TraceSum, bool[] AddVatTrace)
        {
            if (TraceSum == null || Account == null)
                return;

            for (int i = ActiveTraceAccount; (--i >= 0);)
            {
                if (Account == TraceAccount[i])
                {
                    TraceSum[i] += Amount;
                    if (AddVatTrace[i])
                        TraceSum[i] += Vat;
                }
            }
        }

        void CalcVatLine(JournalLineGridClient journalLine)
        {
            DateTime Date = DateTime.MinValue;
            var Amount = journalLine._Debit - journalLine._Credit;
            if (journalLine._Date != DateTime.MinValue)
                Date = journalLine._Date;

            var VatCode1 = journalLine._Vat;
            var VatCode2 = journalLine._OffsetVat;
            if (journalLine._AccountType == 0 && journalLine._OffsetAccountType != 0 && VatCode1 == null && VatCode2 != null ||
                journalLine._AccountType != 0 && journalLine._OffsetAccountType == 0 && VatCode1 != null && VatCode2 == null)
            {
                VatCode2 = VatCode1;
                VatCode1 = journalLine._OffsetVat;
            }

            var VatMethod = GLVatCalculationMethod.Brutto;
            if (journalLine._AccountType == 0 && journalLine._Account != null)
            {
                var Acc = (GLAccount)LedgerCache.Get(journalLine._Account);
                if (Acc != null && Acc._MandatoryTax != VatOptions.NoVat)
                    VatMethod = GLVatCalculationMethod.Netto;
            }

            double calcVat1, calcVat2;
            if (journalLine._Account != null)
                CalcExtraVat(VatCode1, Amount, Date, VatMethod, out calcVat1);
            else
                calcVat1 = 0d;

            if (journalLine._OffsetAccount != null)
                CalcExtraVat(VatCode2, Amount, Date, VatMethod, out calcVat2);
            else
                calcVat2 = 0d;

            if (TwoVatCodes)
            {
                journalLine.SetVatAmount(calcVat1);
                journalLine.SetVatAmountOffset(calcVat2);
            }
            else
                journalLine.SetVatAmount(calcVat1 - calcVat2);
        }

        public override void RowsPastedDone() { RecalculateSum(); }

        internal void RecalculateSum()
        {
            RecalculateSum(false);
            if (LedgerCache != null && LedgerCache.IsUpdated)
                api.LoadCache(typeof(GLAccount), true);
        }

        void RecalculateSum(bool Renumber)
        {
            var LedgerCache = this.LedgerCache;
            if (LedgerCache == null)
                return;

            double sumCredit = 0d, sumDebit = 0d;
            var TraceAccount = this.TraceAccount;
            bool[] AddVatTrace = null;
            double[] TraceSum = null;
            if (TraceAccount != null)
            {
                AddVatTrace = new bool[ActiveTraceAccount];
                TraceSum = new double[ActiveTraceAccount];
                for (int i = ActiveTraceAccount; (--i >= 0);)
                {
                    var Acc = (GLAccount)LedgerCache.Get(TraceAccount[i]);
                    if (Acc != null)
                    {
                        TraceSum[i] = Acc.CurBalance;
                        AddVatTrace[i] = Acc._MandatoryTax == VatOptions.NoVat;
                    }
                }
            }

            int NewVoucher = this.FirstVoucher;
            int maxVoucher = 0;
            var TwoVatCodes = this.TwoVatCodes;
            var NoVATCalculation = this.NoVATCalculation;
            bool TraceSumChange = false;
            DateTime Date = DateTime.MinValue;

            var gridItems = (IList<JournalLineGridClient>)dgGLDailyJournalLine.ItemsSource;
            if (gridItems == null)
                return;

            foreach (var journalLine in gridItems)
            {
                if (Renumber && NewVoucher == 0)
                    NewVoucher = Math.Max(1, journalLine._Voucher);

                var Amount = journalLine._Debit - journalLine._Credit;
                if (journalLine._Date != DateTime.MinValue)
                    Date = journalLine._Date;

                var VatCode1 = journalLine._Vat;
                var VatCode2 = journalLine._OffsetVat;
                if (journalLine._AccountType == 0 && journalLine._OffsetAccountType != 0 && VatCode1 == null && VatCode2 != null ||
                    journalLine._AccountType != 0 && journalLine._OffsetAccountType == 0 && VatCode1 != null && VatCode2 == null)
                {
                    VatCode2 = VatCode1;
                    VatCode1 = journalLine._OffsetVat;
                }

                var VatMethod = GLVatCalculationMethod.Brutto;
                if (journalLine._AccountType == 0 && journalLine._Account != null)
                {
                    var Acc = (GLAccount)LedgerCache.Get(journalLine._Account);
                    if (Acc != null && Acc._MandatoryTax != VatOptions.NoVat)
                        VatMethod = GLVatCalculationMethod.Netto;
                }

                double Vat1, Vat2, calcVat1, calcVat2;
                if (journalLine._Account != null)
                {
                    if (NoVATCalculation)
                        calcVat1 = Vat1 = 0;
                    else
                    {
                        Vat1 = CalcExtraVat(VatCode1, Amount, Date, VatMethod, out calcVat1);
                        if (Vat1 != 0 && journalLine._EnteredVatAmount != 0d)
                        {
                            if (Vat1 > 0)
                                Vat1 = Math.Abs(journalLine._EnteredVatAmount);
                            else
                                Vat1 = -Math.Abs(journalLine._EnteredVatAmount);
                            calcVat1 = Vat1;
                        }
                    }

                    if (journalLine._AccountType == 0)
                        AddTraceValues(journalLine._Account, Amount, Vat1, TraceSum, AddVatTrace);
                    if (journalLine._Debit != 0d)
                        sumDebit += journalLine._Debit + Vat1;
                    else
                        sumCredit += journalLine._Credit - Vat1;
                }
                else
                    Vat1 = calcVat1 = 0d;

                if (journalLine._OffsetAccount != null || journalLine._HasOffsetAccounts)
                {
                    if (NoVATCalculation)
                        Vat2 = calcVat2 = 0d;
                    else
                        Vat2 = CalcExtraVat(VatCode2, Amount, Date, VatMethod, out calcVat2);

                    if (journalLine._OffsetAccountType == 0)
                        AddTraceValues(journalLine._OffsetAccount, -Amount, -Vat2, TraceSum, AddVatTrace);

                    if (journalLine._Debit != 0d)
                    {
                        sumCredit += journalLine._Debit + Vat1 + Vat2;
                        if (journalLine._Account != null)
                            sumDebit += Vat2;
                    }
                    else
                    {
                        sumDebit += journalLine._Credit - Vat1 - Vat2;
                        if (journalLine._Account != null)
                            sumCredit -= Vat2;
                    }
                }
                else
                    Vat2 = calcVat2 = 0d;

                if (TwoVatCodes)
                {
                    journalLine.SetVatAmount(calcVat1);
                    journalLine.SetVatAmountOffset(-calcVat2);
                }
                else
                    journalLine.SetVatAmount(calcVat1 - calcVat2);

                var tot = sumDebit - sumCredit;
                if (Math.Abs(tot) < 0.005d)
                {
                    tot = 0d;
                    sumDebit = sumCredit;
                }

                if (Renumber)
                {
                    if (journalLine._Voucher != NewVoucher)
                    {
                        journalLine.Voucher = NewVoucher;
                        dgGLDailyJournalLine.SetModifiedRow(journalLine);
                    }
                    if (tot == 0d)
                        NewVoucher++;
                }
                journalLine.SetTotal(tot);
                maxVoucher = Math.Max(maxVoucher, journalLine._Voucher);

                if (journalLine.SetTraceSum(TraceSum))
                    TraceSumChange = true;
            }

            if (TraceSumChange)
            {
                var selectedItem = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
                if (selectedItem == null && gridItems.Count > 0)
                    selectedItem = gridItems[gridItems.Count-1];
                selectedItem?.NotifyTranceSumChange();
            }

            this.maxVoucher = maxVoucher;
            SetStatusText(Math.Round(sumDebit, 2), Math.Round(sumCredit, 2));
        }

        void SetStatusText(double sumDebit, double sumCredit)
        {
            string format = RoundTo100 ? "N0" : "N2";
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var debtxt = Uniconta.ClientTools.Localization.lookup("Debit");
            var cretxt = Uniconta.ClientTools.Localization.lookup("Credit");
            var diftxt = Uniconta.ClientTools.Localization.lookup("Dif");
            foreach (var grp in groups)
            {
                if (grp.Caption == debtxt)
                    grp.StatusValue = sumDebit.ToString(format);
                else if (grp.Caption == cretxt)
                    grp.StatusValue = sumCredit.ToString(format);
                else if (grp.Caption == diftxt)
                {
                    var tot = sumDebit - sumCredit;
                    if (Math.Abs(tot) < 0.005d)
                        tot = 0d;
                    LineTotal = tot;
                    grp.StatusValue = tot.ToString(format);
                }
                else grp.StatusValue = string.Empty;
            }
        }

        DCAccount getDCAccount(GLJournalAccountType type, string Acc)
        {
            if (Acc != null && type != GLJournalAccountType.Finans)
            {
                var cache = (type == GLJournalAccountType.Debtor) ? DebtorCache : CreditorCache;
                return (DCAccount)cache?.Get(Acc);
            }
            return null;
        }
        DCAccount copyDCAccount(JournalLineGridClient rec, GLJournalAccountType type, string Acc, bool OnlyDueDate = false)
        {
            var dc = getDCAccount(type, Acc);
            if (dc == null)
                return null;
            if ((rec._DocumentDate != DateTime.MinValue || rec._Date != DateTime.MinValue) && rec._DCPostType != Uniconta.DataModel.DCPostType.Creditnote && (rec._DCPostType < Uniconta.DataModel.DCPostType.Payment || rec._DCPostType > Uniconta.DataModel.DCPostType.PartialPayment))
            {
                var pay = (Uniconta.DataModel.PaymentTerm)PaymentCache?.Get(dc._Payment);
                if (pay != null)
                    rec.DueDate = pay.GetDueDate(rec._DocumentDate != DateTime.MinValue ? rec._DocumentDate : rec._Date);
            }
            if (OnlyDueDate)
                return dc;

            if (type == GLJournalAccountType.Creditor)
            {
                if (dc._PrCategory != null)
                    rec.PrCategory = dc._PrCategory;
                if (rec._PaymentMethod != dc._PaymentMethod)
                {
                    rec._PaymentMethod = dc._PaymentMethod;
                    rec.NotifyPropertyChanged("PaymentMethod");
                }
            }
            rec.Withholding = dc._Withholding;
            if (dc._Dim1 != null)
                rec.Dimension1 = dc._Dim1;
            if (dc._Dim2 != null)
                rec.Dimension2 = dc._Dim2;
            if (dc._Dim3 != null)
                rec.Dimension3 = dc._Dim3;
            if (dc._Dim4 != null)
                rec.Dimension4 = dc._Dim4;
            if (dc._Dim5 != null)
                rec.Dimension5 = dc._Dim5;
            if (rec.AmountCur == 0)
                rec.Currency = AppEnums.Currencies.ToString((int)dc._Currency);
            return dc;
        }

        void assignOffsetVat(JournalLineGridClient rec, string vat, string vatOperation)
        {
            if (rec._DCPostType >= Uniconta.DataModel.DCPostType.Payment && rec._DCPostType <= Uniconta.DataModel.DCPostType.PartialPayment)
                return;

            if (vat != null)
            {
                if (TwoVatCodes)
                    rec.OffsetVat = vat;
                else
                    rec.Vat = vat;
            }
            if (vatOperation != null)
            {
                if (TwoVatCodes)
                    rec.VatOffsetOperation = vatOperation;
                else
                    rec.VatOperation = vatOperation;
            }
        }

        void JournalLineGridClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as JournalLineGridClient;
            switch (e.PropertyName)
            {
                case "Account":
                    if (rec._AccountType != (byte)GLJournalAccountType.Finans)
                    {
                        var dc = copyDCAccount(rec, rec._AccountTypeEnum, rec._Account);
                        if (dc != null)
                        {
                            rec.Vat = null;
                            if (dc._PostingAccount != null)
                            {
                                // lets check the currenut offset account if it is a bank account
                                var Acc = (GLAccount)LedgerCache?.Get(rec._OffsetAccount);
                                if (Acc == null || Acc._MandatoryTax != VatOptions.NoVat)  // we will not override a bank account
                                {
                                    if (rec._OffsetAccountType != (byte)GLJournalAccountType.Finans)
                                    {
                                        rec._OffsetAccountType = (byte)GLJournalAccountType.Finans;
                                        rec.NotifyPropertyChanged("OffsetAccountType");
                                    }
                                    var amount = rec.Amount;
                                    if (rec._AccountType == (byte)GLJournalAccountType.Debtor)
                                        amount *= -1d;

                                    rec.TmpOffsetAccount = null;
                                    if (amount < 0) // expense
                                        rec.OffsetAccount = dc._PostingAccount;
                                    else if (amount == 0)
                                        rec.TmpOffsetAccount = dc._PostingAccount;
                                }
                            }

                            if (UseDCVat && rec._OffsetAccountType == (byte)GLJournalAccountType.Finans)
                            {
                                var Acc = (GLAccount)LedgerCache?.Get(rec._OffsetAccount);
                                if (Acc != null && Acc._MandatoryTax != VatOptions.NoVat && Acc._MandatoryTax != VatOptions.Fixed)
                                    assignOffsetVat(rec, dc._Vat, dc._VatOperation);
                            }
                        }
                    }
                    else
                    {
                        var Acc = (GLAccount)LedgerCache?.Get(rec._Account);
                        if (Acc != null)
                        {
                            if (Acc._PrCategory != null)
                                rec.PrCategory = Acc._PrCategory;
                            if (Acc._Currency != 0)
                            {
                                rec._Currency = (byte)Acc._Currency;
                                rec.NotifyPropertyChanged("Currency");
                            }
                            rec.SetGLDim(Acc);
                            if (Acc._MandatoryTax == VatOptions.NoVat)
                            {
                                if (TwoVatCodes)
                                {
                                    rec.Vat = null;
                                    rec.VatOperation = null;
                                }
                            }
                            else if (Acc._IsDCAccount)
                                assignOffsetVat(rec, Acc._Vat, Acc._VatOperation);
                            else
                            {
                                var vat = Acc._Vat;
                                var VatOperation = Acc._VatOperation;
                                if (UseDCVat && Acc._MandatoryTax != VatOptions.Fixed)
                                {
                                    var dc = getDCAccount(rec._OffsetAccountTypeEnum, rec._OffsetAccount);
                                    if (dc != null && dc._Vat != null)
                                    {
                                        vat = dc._Vat;
                                        VatOperation = dc._VatOperation;
                                    }
                                }
                                rec.Vat = vat;
                                rec.VatOperation = VatOperation;
                            }
                            if (Acc._DefaultOffsetAccount != null)
                            {
                                rec._OffsetAccountType = (byte)Acc._DefaultOffsetAccountType; // set before account
                                rec.OffsetAccount = Acc._DefaultOffsetAccount;
                                if (rec._OffsetAccountType == 0)
                                {
                                    var Acc2 = (GLAccount)LedgerCache.Get(rec._OffsetAccount);
                                    if (Acc2 != null)
                                    {
                                        if (TwoVatCodes || (Acc._MandatoryTax != VatOptions.NoVat && Acc._MandatoryTax != VatOptions.Fixed))
                                            assignOffsetVat(rec, Acc2._Vat, Acc2._VatOperation);
                                    }
                                }
                            }
                            if (Acc._DebetCredit > 0)
                                dgGLDailyJournalLine.GoToCol(Acc._DebetCredit == DebitCreditPreference.Debet ? "Debit" : "Credit", true);
                        }
                    }
                    if (rec.Amount != 0d)
                        RecalculateSum();
                    rec.UpdateDefaultText();
                    break;
                case "OffsetAccount":
                    if (rec._OffsetAccountType != (byte)GLJournalAccountType.Finans)
                    {
                        var dc = copyDCAccount(rec, rec._OffsetAccountTypeEnum, rec._OffsetAccount);
                        if (dc != null)
                        {
                            if (dc._PostingAccount != null && rec._Account == null)
                            {
                                if (rec._AccountType != (byte)GLJournalAccountType.Finans)
                                {
                                    rec._AccountType = (byte)GLJournalAccountType.Finans;
                                    rec.NotifyPropertyChanged("AccountType");
                                }
                                rec.Account = dc._PostingAccount;
                                rec.TmpOffsetAccount = null;
                            }

                            rec.OffsetVat = null;
                            if (UseDCVat && rec._AccountType == (byte)GLJournalAccountType.Finans)
                            {
                                var Acc2 = (GLAccount)LedgerCache?.Get(rec._Account);
                                if (Acc2 != null && Acc2._MandatoryTax != VatOptions.NoVat && Acc2._MandatoryTax != VatOptions.Fixed)
                                {
                                    if (dc._Vat != null)
                                        rec.Vat = dc._Vat;
                                    if (dc._VatOperation != null)
                                        rec.VatOperation = dc._VatOperation;
                                }
                            }
                        }
                    }
                    else
                    {
                        var Acc = (GLAccount)LedgerCache?.Get(rec._OffsetAccount);
                        if (Acc != null)
                        {
                            if (Acc._MandatoryTax == VatOptions.NoVat)
                            {
                                if (TwoVatCodes)
                                {
                                    rec.OffsetVat = null;
                                    rec.VatOffsetOperation = null;
                                }
                            }
                            else if (Acc._IsDCAccount)
                            {
                                var Acc2 = (GLAccount)LedgerCache.Get(rec._Account);
                                if (Acc2 == null || (Acc2._Vat == null && Acc2._MandatoryTax != VatOptions.NoVat))
                                {
                                    if (Acc._Vat != null)
                                        rec.Vat = Acc._Vat;
                                    if (Acc._VatOperation != null)
                                        rec.VatOperation = Acc._VatOperation;
                                }
                            }
                            else
                            {
                                var vat = Acc._Vat;
                                var VatOperation = Acc._VatOperation;
                                if (UseDCVat && Acc._MandatoryTax != VatOptions.Fixed)
                                {
                                    var dc = getDCAccount(rec._AccountTypeEnum, rec._Account);
                                    if (dc != null)
                                    {
                                        if (dc._Vat != null)
                                            vat = dc._Vat;
                                        if (dc._VatOperation != null)
                                            VatOperation = dc._VatOperation;
                                    }
                                }
                                if (TwoVatCodes)
                                    assignOffsetVat(rec, vat, VatOperation);
                                else
                                {
                                    var Acc2 = (GLAccount)LedgerCache.Get(rec._Account);
                                    if (Acc2 == null || (Acc2._Vat == null && Acc2._MandatoryTax != VatOptions.NoVat))
                                    {
                                        if (Acc._Vat != null)
                                            rec.Vat = Acc._Vat;
                                        if (Acc._VatOperation != null)
                                            rec.VatOperation = Acc._VatOperation;
                                    }
                                }
                            }
                        }
                    }
                    if (rec.Amount != 0d)
                        RecalculateSum();
                    rec.UpdateDefaultText();
                    break;
                case "AccountType":
                    SetAccountSource(rec);
                    break;
                case "OffsetAccountType":
                    SetOffsetAccountSource(rec);
                    break;
                case "OffsetAccountList":
                    RecalculateSum();
                    break;
                case "Debit":
                    rec._Debit = Math.Round(rec._Debit, 2);
                    if (rec._Debit != 0 && rec.TmpOffsetAccount != null)
                    {
                        var amount = rec._Debit;
                        if (rec._AccountType == (byte)GLJournalAccountType.Debtor)
                            amount *= -1d;
                        else if (rec._AccountType != (byte)GLJournalAccountType.Creditor)
                            amount = 0;
                        if (amount < 0) // expense
                        {
                            rec.OffsetAccount = rec.TmpOffsetAccount;
                            rec.TmpOffsetAccount = null;
                        }
                    }
                    RecalculateSum();
                    TestForTakeVoucher(rec);
                    break;
                case "Credit":
                    rec._Credit = Math.Round(rec._Credit, 2);
                    if (rec._Credit != 0 && rec.TmpOffsetAccount != null)
                    {
                        var amount = -rec._Credit;
                        if (rec._AccountType == (byte)GLJournalAccountType.Debtor)
                            amount *= -1d;
                        else if (rec._AccountType != (byte)GLJournalAccountType.Creditor)
                            amount = 0;
                        if (amount < 0) // expense
                        {
                            rec.OffsetAccount = rec.TmpOffsetAccount;
                            rec.TmpOffsetAccount = null;
                        }
                    }
                    RecalculateSum();
                    TestForTakeVoucher(rec);
                    break;
                case "DebitCur":
                    rec._DebitCur = Math.Round(rec._DebitCur, 2);
                    calcLocalCur(rec, rec._DebitCur, "Debit");
                    break;
                case "CreditCur":
                    rec._CreditCur = Math.Round(rec._CreditCur, 2);
                    calcLocalCur(rec, rec._CreditCur, "Credit");
                    break;
                case "Vat":
                case "OffsetVat":
                    CalcVatLine(rec);
                    break;
                case "Currency":
                    if (rec._Currency > 0 && rec.AmountCur != 0d)
                        calcLocalCur(rec, rec.AmountCur, "Amount");
                    break;
                case "Invoice":
                    if (rec._Invoice != null && (rec._Account == null || rec._OffsetAccount == null))
                        GetSearchOpenInvoice(rec);
                    rec.UpdateDefaultText();
                    break;
                case "Date":
                case "DocumentDate":
                    copyDCAccount(rec, rec._AccountTypeEnum, rec._Account, true);
                    copyDCAccount(rec, rec._OffsetAccountTypeEnum, rec._OffsetAccount, true);
                    break;
                case "Text":
                    SetTransText(rec, getTransText(rec._Text, TextTypes));
                    break;
                case "TransType":
                    SetTransText(rec, (Uniconta.DataModel.GLTransType)TextTypes.Get(rec._TransType));
                    break;
                case "Project":
                    lookupProjectDim(rec);
                    break;
                case "Task":
                    if (string.IsNullOrEmpty(rec._Project))
                        rec._Task = null;
                    break;
                case "Asset":
                case "AssetPostType":
                    if (rec._AssetPostType != 0 && rec._Asset != null)
                        lookupAsset(rec);
                    break;
            }
        }

        async void lookupAsset(JournalLineGridClient rec)
        {
            if (AssetCache == null)
                AssetCache = api.GetCache(typeof(Uniconta.DataModel.FAM)) ?? await api.LoadCache(typeof(Uniconta.DataModel.FAM));
            var asset = (Uniconta.DataModel.FAM)AssetCache.Get(rec._Asset);
            if (asset == null)
                return;
            if (asset._Dim1 != null)
                rec.Dimension1 = asset._Dim1;
            if (asset._Dim2 != null)
                rec.Dimension2 = asset._Dim2;
            if (asset._Dim3 != null)
                rec.Dimension3 = asset._Dim3;
            if (asset._Dim4 != null)
                rec.Dimension4 = asset._Dim4;
            if (asset._Dim5 != null)
                rec.Dimension5 = asset._Dim5;

            if (AssetGroupCache == null)
                AssetGroupCache = api.GetCache(typeof(Uniconta.DataModel.FAMGroup)) ?? await api.LoadCache(typeof(Uniconta.DataModel.FAMGroup));
            var grp = (Uniconta.DataModel.FAMGroup)AssetGroupCache.Get(asset._Group);
            if (grp == null)
                return;
            string acc, offset;
            switch (rec._AssetPostType)
            {
                case FAMTransCodes.Depreciation:
                case FAMTransCodes.ReversedDepreciation:
                    acc = grp._DepreciationAccount;
                    offset = grp._DepreciationOffset;
                    break;
                case FAMTransCodes.Acquisition:
                    acc = grp._AcquisitionAccount;
                    offset = grp._AcquisitionOffset;
                    break;
                case FAMTransCodes.WriteDown:
                    acc = grp._WriteDownAccount;
                    offset = grp._WriteDownOffset;
                    break;
                case FAMTransCodes.WriteOff:
                    acc = grp._WriteOffAccount;
                    offset = grp._WriteOffOffset;
                    break;
                case FAMTransCodes.WriteUp:
                    acc = grp._WriteUpAccount;
                    offset = grp._WriteUpOffset;
                    break;
                case FAMTransCodes.Sale:
                    acc = grp._SalesAccount;
                    offset = grp._SalesOffset;
                    break;
                default:
                    acc = offset = null;
                    break;
            }
            if (acc != null)
            {
                rec._AccountType = 0;
                rec.Account = acc;
            }
            if (offset != null)
            {
                rec._OffsetAccountType = 0;
                rec.OffsetAccount = offset;
            }
        }

        async void lookupProjectDim(JournalLineGridClient rec)
        {
            if (ProjectCache == null)
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project));
            var proj = (Uniconta.DataModel.Project)ProjectCache?.Get(rec._Project);
            if (proj != null)
            {
                if (proj._Dim1 != null)
                    rec.Dimension1 = proj._Dim1;
                if (proj._Dim2 != null)
                    rec.Dimension2 = proj._Dim2;
                if (proj._Dim3 != null)
                    rec.Dimension3 = proj._Dim3;
                if (proj._Dim4 != null)
                    rec.Dimension4 = proj._Dim4;
                if (proj._Dim5 != null)
                    rec.Dimension5 = proj._Dim5;

                setTask(proj as ProjectClient, rec);
            }
        }

        static public Uniconta.DataModel.GLTransType getTransText(string t, SQLCache TextTypes)
        {
            if (!string.IsNullOrWhiteSpace(t) && t.Length <= 10 && TextTypes != null && TextTypes.Get(t) == null)
            {
                foreach (var txt in (IEnumerable<Uniconta.DataModel.GLTransType>)TextTypes.GetNotNullArray)
                    if (string.Compare(txt._Code, t, StringComparison.CurrentCultureIgnoreCase) == 0)
                        return txt;
            }
            return null;
        }

        /*
        void SetText(JournalLineGridClient rec)
        {
            var str = rec._Text;
            if (str == null)
                return;
            if (str.Length >= 4 && api.CompanyEntity._AutoSettlement != 1 && (rec._AccountType != 0 || rec._OffsetAccountType != 0))
            {
                bool found = false;
                if (string.Compare(Uniconta.ClientTools.Localization.lookup("Invoice"), 0, str, 0, 3, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    found = true;
                    rec._DCPostType = Uniconta.DataModel.DCPostType.Invoice;
                }
                else if (string.Compare(Uniconta.ClientTools.Localization.lookup("Payment"), 0, str, 0, 3, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    found = true;
                    rec._DCPostType = Uniconta.DataModel.DCPostType.Payment;
                }
                else if (string.Compare(Uniconta.ClientTools.Localization.lookup("CreditNote"), 0, str, 0, 7, StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    found = true;
                    rec._DCPostType = Uniconta.DataModel.DCPostType.Creditnote;
                }
                if (found)
                {
                    int firstDigit = -1, digitLen = 0;
                    for (int i = 0; (i < str.Length); i++)
                    {
                        var ch = str[i];
                        if (char.IsDigit(ch))
                        {
                            if (firstDigit < 0)
                                firstDigit = i;
                            digitLen++;
                        }
                        else if (firstDigit >= 0)
                        {
                            if (digitLen >= 4)
                                break;
                            firstDigit = -1;
                            digitLen = 0;
                        }
                    }
                    if (firstDigit >= 0)
                    {
                        rec.Invoice = str.Substring(firstDigit, digitLen);
                        rec.Text = null;
                        return;
                    }
                }
            }
            var t = getTransText(str, TextTypes);
            if (t != null)
                SetTransText(rec, t);
        }
        */

        void SetTransText(JournalLineGridClient rec, Uniconta.DataModel.GLTransType t)
        {
            if (t != null)
            {
                rec.Text = t._TransType;
                if (rec._AccountType != (byte)t._AccountType && (t._Account != null || rec._Account == null))
                {
                    rec._AccountType = (byte)t._AccountType;
                    rec.NotifyPropertyChanged("AccountType");
                }
                if (t._Account != null)
                    rec.Account = t._Account;

                if (rec._OffsetAccountType != (byte)t._OffsetAccountType && (t._OffsetAccount != null || rec._OffsetAccount == null))
                {
                    rec._OffsetAccountType = (byte)t._OffsetAccountType;
                    rec.NotifyPropertyChanged("OffsetAccountType");
                }
                if (t._OffsetAccount != null)
                    rec.OffsetAccount = t._OffsetAccount;
            }
        }

        void CreatePaymentFile()
        {
            var gridItems = (IEnumerable<JournalLineGridClient>)dgGLDailyJournalLine.ItemsSource;
            if (gridItems == null)
                return;

            var Comp = api.CompanyEntity;
            var curDate = BasePage.GetSystemDefaultDate();

            var lst = new List<UnicontaClient.Pages.CreditorTransPayment>();
            foreach (var lin in gridItems)
            {
                if (lin._PaymentId == null || lin._AccountType != (byte)GLJournalAccountType.Finans || lin._OffsetAccountType != (byte)GLJournalAccountType.Finans)
                    continue;

                bool bankAsOffsetAccount = false;
                string ledAccBank = null;
                var glAccount = (GLAccount)LedgerCache.Get(lin._OffsetAccount);
                if (glAccount._AccountType == (byte)GLAccountTypes.Bank)
                {
                    bankAsOffsetAccount = true;
                    ledAccBank = glAccount._Account;
                }
                else
                {
                    glAccount = (GLAccount)LedgerCache.Get(lin._Account);
                    if (glAccount._AccountType == (byte)GLAccountTypes.Bank)
                        ledAccBank = glAccount._Account;
                }

                if (ledAccBank == null)
                    continue; //It's expected that each line has a Ledger account of the type 'Bank'. 

                if (lin.Amount < 0 && bankAsOffsetAccount == true || lin.Amount > 0 && bankAsOffsetAccount == false)
                    continue;

                var trans = new CreditorTrans();
                trans._DocumentRef = lin._DocumentRef;
                trans._Date = lin._Date;
                trans._DueDate = lin._DueDate != DateTime.MinValue ? lin._DueDate : lin._Date;
                trans.SetInvoice(lin._Invoice);
                trans._PostType = (byte)Uniconta.DataModel.DCPostType.Invoice;
                trans._Amount = -Math.Abs(lin.Amount);
                trans._Text = lin._Text;
                if (lin._Currency != 0 && lin._Currency != Comp._Currency)
                {
                    trans._Currency = lin._Currency;
                    trans._AmountCur = -Math.Abs(lin.AmountCur);
                }

                var rec = new UnicontaClient.Pages.CreditorTransPayment();
                rec.Trans = trans;
                rec._DueDate = trans._DueDate;
                rec._PaymentDate = curDate > trans._DueDate ? curDate : trans._DueDate;
                rec._PaymentId = lin._PaymentId;
                rec._PaymentMethod = lin._PaymentMethod;
                rec._AmountOpen = trans._Amount;
                rec._AmountOpenCur = trans._AmountCur;

                rec.ErrorInfo = "<DailyJournal Generated>";
                rec._Comment = trans._Text;

                //CreditorPaymentFormat >>
                var paymentFormat = string.Empty;

                foreach (var r in credPaymFormatCache.GetRecords)
                {
                    var credPaymFmt = r as CreditorPaymentFormat;
                    if (credPaymFmt != null)
                    {
                        if (credPaymFmt._BankAccount == ledAccBank)
                        {
                            paymentFormat = credPaymFmt._Format;
                            break;
                        }
                        else if (credPaymFmt._Default)
                        {
                            paymentFormat = credPaymFmt._Format;
                        }
                    }
                }
                rec._PaymentFormat = paymentFormat;
                //CreditorPaymentFormat <<

                lst.Add(rec);
            }

            if (lst.Count > 0)
            {
                object[] param = new object[] { lst };
                AddDockItem(TabControls.Payments, param, Uniconta.ClientTools.Localization.lookup("Payments"));
            }
            else
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordExport"), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
        }

        async void GetSearchOpenInvoice(JournalLineGridClient line)
        {
            GLJournalAccountType type;
            bool setAccount;
            if (line._OffsetAccountType != 0 || line._AccountType != 0)
            {
                if (line._AccountType != 0)
                {
                    if (line._Account != null && line._Account != line.AccountFilledByInvoice) // we have an account
                        return;
                    type = (GLJournalAccountType)line._AccountType;
                    setAccount = true;
                }
                else
                {
                    if (line._OffsetAccount != null && line._OffsetAccount != line.AccountFilledByInvoice)
                        return;
                    type = (GLJournalAccountType)line._OffsetAccountType;
                    setAccount = false;
                }
            }
            else
            {
                if (line._Account != null)
                {
                    var Acc = (GLAccount)LedgerCache.Get(line._Account);
                    if (Acc != null && Acc._AccountType < (byte)GLAccountTypes.BalanceSheet) // we register a cost. Then we do not look for an existing invoice
                        return;
                    type = line.Amount > 0 ? GLJournalAccountType.Debtor : GLJournalAccountType.Creditor;
                    setAccount = false;
                }
                else
                {
                    var Acc = (GLAccount)LedgerCache.Get(line._OffsetAccount);
                    if (Acc != null && Acc._AccountType < (byte)GLAccountTypes.BalanceSheet)
                        return;
                    type = line.Amount > 0 ? GLJournalAccountType.Creditor : GLJournalAccountType.Debtor;
                    setAccount = true;
                }
            }
            var InvApi = new Uniconta.API.DebtorCreditor.InvoiceAPI(api);
            var OpenTrans = await InvApi.SearchOpenInvoiceTrans(type, line._Invoice);
            if (OpenTrans != null)
            {
                var strAccount = OpenTrans.Trans._Account;
                line.AccountFilledByInvoice = strAccount;
                double sign;
                if (setAccount)
                {
                    line.AccountType = AppEnums.GLAccountType.ToString((int)type);
                    line.Account = strAccount;
                    sign = -1d;
                }
                else
                {
                    line.OffsetAccountType = AppEnums.GLAccountType.ToString((int)type);
                    line.OffsetAccount = strAccount;
                    sign = 1d;
                }

                if (line.Amount == 0d)
                {
                    line.Amount = sign * OpenTrans._AmountOpen;
                    if (OpenTrans.Trans._Currency != 0)
                    {
                        line.CurrencyEnum = (Currencies)OpenTrans.Trans._Currency;
                        line.AmountCur = sign * OpenTrans._AmountOpenCur;
                    }
                }
            }
        }

        double LastRate;
        Currencies LastFromCur;
        DateTime LastFromDate;

        async void calcLocalCur(JournalLineGridClient rec, double val, string field)
        {
            if (val != 0d)
            {
                Currencies FromCur;
                Currencies ToCur = api.CompanyEntity._CurrencyId;
                if (rec._Currency == 0)
                {
                    FromCur = ToCur;
                    rec._Currency = (byte)ToCur;
                }
                else
                    FromCur = (Currencies)rec._Currency;

                if (FromCur != ToCur)
                {
                    if (FromCur != LastFromCur || rec._Date != LastFromDate)
                    {
                        LastFromCur = FromCur;
                        LastFromDate = rec._Date;
                        LastRate = await api.session.ExchangeRate(FromCur, ToCur, rec._Date, api.CompanyEntity);

                        // lets read value in case it has change while we awaited
                        var propCur = typeof(JournalLineGridClient).GetProperty(field + "Cur");
                        val = Convert.ToDouble(propCur?.GetValue(rec, null));
                    }
                    if (LastRate == 0d)
                        return;
                    val = Math.Round(val * LastRate, RoundTo100 ? 0 : 2);
                }
                var prop = typeof(JournalLineGridClient).GetProperty(field);
                prop?.SetValue(rec, val, null);
            }
        }

        SQLCache GetCache(byte AccountType)
        {
            switch (AccountType)
            {
                case (byte)GLJournalAccountType.Finans:
                    return LedgerCache;
                case (byte)GLJournalAccountType.Debtor:
                    return DebtorCache;
                case (byte)GLJournalAccountType.Creditor:
                    return CreditorCache;
                default: return null;
            }
        }

        private void SetAccountSource(JournalLineGridClient rec)
        {
            var act = rec._AccountType;
            SQLCache cache = GetCache(act);
            if (cache != null)
            {
                int ver = cache.version + 10000 * (act + 1);
                if (ver != rec.AccountVersion || rec.accntSource == null)
                {
                    if (act == (byte)GLJournalAccountType.Finans)
                        rec.accntSource = new LedgerSQLCacheFilter(cache);
                    else
                        rec.accntSource = cache.GetNotNullArray;
                    if (rec.accntSource != null)
                    {
                        rec.AccountVersion = ver;
                        rec.NotifyPropertyChanged("AccountSource");
                    }
                }
            }
        }

        private void SetOffsetAccountSource(JournalLineGridClient rec)
        {
            var act = rec._OffsetAccountType;
            SQLCache cache = GetCache(act);
            if (cache != null)
            {
                int ver = cache.version + 10000 * (act + 1);
                if (ver != rec.OffsetAccountVersion || rec.offsetAccntSource == null)
                {
                    if (act == (byte)GLJournalAccountType.Finans)
                        rec.offsetAccntSource = new LedgerSQLCacheFilter(cache);
                    else
                        rec.offsetAccntSource = cache.GetNotNullArray;
                    if (rec.offsetAccntSource != null)
                    {
                        rec.OffsetAccountVersion = ver;
                        rec.NotifyPropertyChanged("OffsetAccountSource");
                    }
                }
            }
        }

        public class LedgerSQLCacheFilter : SQLCacheFilter
        {
            public LedgerSQLCacheFilter(SQLCache cache) : base(cache) { }
            public override bool IsValid(object rec)
            {
                var acc = (GLAccount)rec;
                return (acc._AccountType == 0 || acc._AccountType > (byte)GLAccountTypes.CalculationExpression) && !acc._BlockedInJournal && !acc._Blocked;
            }
        }

        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            JournalLineGridClient selectedItem = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
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

        CorasauGridLookupEditorClient prevOffsetAccount;
        private void OffsetAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            JournalLineGridClient selectedItem = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
            if (selectedItem != null)
            {
                SetOffsetAccountSource(selectedItem);
                if (prevOffsetAccount != null)
                    prevOffsetAccount.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevOffsetAccount = editor;
                editor.isValidate = true;
            }
        }

        private void Account_LostFocus(object sender, RoutedEventArgs e)
        {
            SetAccountByLookupText(sender, false);
        }
        private void OffsetAccount_LostFocus(object sender, RoutedEventArgs e)
        {
            SetAccountByLookupText(sender, true);
        }
        void SetAccountByLookupText(object sender, bool offsetAcc)
        {
            JournalLineGridClient selectedItem = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
            if (selectedItem == null)
                return;
            var actType = !offsetAcc ? selectedItem._AccountTypeEnum : selectedItem._OffsetAccountTypeEnum;
            if (actType != GLJournalAccountType.Finans)
                return;
            var le = sender as CorasauGridLookupEditor;
            if (string.IsNullOrEmpty(le.EnteredText))
                return;
            var accounts = LedgerCache?.GetNotNullArray as GLAccount[];
            if (accounts != null)
            {
                var act = accounts.Where(ac => ac._Lookup == le.EnteredText).FirstOrDefault();
                if (act != null)
                {
                    dgGLDailyJournalLine.SetLoadedRow(selectedItem);
                    if (offsetAcc)
                        selectedItem.OffsetAccount = act.KeyStr;
                    else
                        selectedItem.Account = act.KeyStr;
                    le.EditValue = act.KeyStr;
                    dgGLDailyJournalLine.SetModifiedRow(selectedItem);
                }
            }
            le.EnteredText = null;
        }

        double CalcExtraVat(string VatId, double Amount, DateTime Date, GLVatCalculationMethod VatMethod, out double VatOnLine)
        {
            if (VatId == null || VatCache == null)
            {
                VatOnLine = 0d;
                return 0d;
            }
            GLVat vatpost = (GLVat)VatCache.Get(VatId);
            if (vatpost == null)
            {
                VatOnLine = 0d;
                return 0d;
            }

            if (vatpost._Method != GLVatCalculationMethod.Automatic)
                VatMethod = vatpost._Method;

            double ret;
            if ((vatpost._Account != null || vatpost._FollowAccount) && vatpost._OffsetAccount == null)
                ret = vatpost.VatAmount1(Amount, Date, RoundTo100, VatMethod);
            else
                ret = 0d;
            if ((vatpost._AccountRate2 != null || vatpost._FollowAccount2) && vatpost._OffsetAccountRate2 == null)
                ret += vatpost.VatAmount2(Amount, Date, RoundTo100, VatMethod);

            VatOnLine = ret;
            if (VatMethod == GLVatCalculationMethod.Brutto)
                ret = 0d;
            return ret;
        }

        public override object GetPrintParameter()
        {
            if (ActiveTraceAccount == 0 || string.IsNullOrEmpty(tracAc1.Text))
                return base.GetPrintParameter();

            var source = dgGLDailyJournalLine.ItemsSource as IList<JournalLineGridClient>;
            var lastLine = source != null && source.Count > 0 ? source[source.Count - 1] : null;
            if (lastLine?._TraceSum == null)
                return base.GetPrintParameter();

            var LedgerCache = this.LedgerCache;
            var TraceAccount = this.TraceAccount;

            var fd = new PrintReportFooter();
            fd.TraceSum1Name = tracAc1.Text;
            fd.TraceSum1 = lastLine._TraceSum[0];
            fd.InitialSum1 = ((GLAccount)LedgerCache.Get(TraceAccount[0])).CurBalance;

            if (tracAc2.Text != string.Empty)
            {
                fd.TraceSum2Name = tracAc2.Text;
                fd.TraceSum2 = lastLine._TraceSum[1];
                fd.InitialSum2 = ((GLAccount)LedgerCache.Get(TraceAccount[1])).CurBalance;
            }
            if (tracAc3.Text != string.Empty)
            {
                fd.TraceSum3Name = tracAc3.Text;
                fd.TraceSum3 = lastLine._TraceSum[2];
                fd.InitialSum3 = ((GLAccount)LedgerCache.Get(TraceAccount[2])).CurBalance;
            }
            if (tracAc4.Text != string.Empty)
            {
                fd.TraceSum4Name = tracAc4.Text;
                fd.TraceSum4 = lastLine._TraceSum[3];
                fd.InitialSum4 = ((GLAccount)LedgerCache.Get(TraceAccount[3])).CurBalance;
            }
            if (tracAc5.Text != string.Empty)
            {
                fd.TraceSum5Name = tracAc5.Text;
                fd.TraceSum5 = lastLine._TraceSum[4];
                fd.InitialSum5 = ((GLAccount)LedgerCache.Get(TraceAccount[4])).CurBalance;
            }
            if (tracAc6.Text != string.Empty)
            {
                fd.TraceSum6Name = tracAc6.Text;
                fd.TraceSum6 = lastLine._TraceSum[5];
                fd.InitialSum6 = ((GLAccount)LedgerCache.Get(TraceAccount[5])).CurBalance;
            }
            dgGLDailyJournalLine.FooterData = fd;
            return base.GetPrintParameter();
        }

        private void HasOffSetAccount_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallOffsetAccount((sender as Image).Tag as JournalLineGridClient);
        }

        void CallOffsetAccount(JournalLineGridClient line)
        {
            if (line != null)
            {
                dgGLDailyJournalLine.SetLoadedRow(line);
                var header = string.Format("{0}:{1} {2}", Uniconta.ClientTools.Localization.lookup("OffsetAccountTemplate"), Uniconta.ClientTools.Localization.lookup("Journallines"), line._Account);
                AddDockItem(TabControls.GLOffsetAccountTemplate, line, header: header);
            }
        }

        private void HasVoucherImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            busyIndicator.IsBusy = true;
            ViewVoucher(TabControls.VouchersPage3, dgGLDailyJournalLine.syncEntity);
            busyIndicator.IsBusy = false;
        }

        protected override bool LoadTemplateHandledLocally(IEnumerable<UnicontaBaseEntity> templateRows)
        {
            foreach (var gl in (IEnumerable<Uniconta.DataModel.GLDailyJournalLine>)templateRows)
            {
                gl._Date = DateTime.MinValue;
                gl._Voucher = 0;
            }
            return false;
        }

        CorasauGridLookupEditorClient prevTask;
        private void Task_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgGLDailyJournalLine.SelectedItem as JournalLineGridClient;
            var selected = (ProjectClient)ProjectCache?.Get(selectedItem?._Project);
            if (selected != null)
            {
                setTask(selected, selectedItem);
                if (prevTask != null)
                    prevTask.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevTask = editor;
                editor.isValidate = true;
            }
        }

        async void setTask(ProjectClient project, JournalLineGridClient rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                    rec.taskSource = project.Tasks ?? await project.LoadTasks(api);
                else
                {
                    rec.taskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("TaskSource");
            }
        }
    }

    public class JournalLineGridClient : GLDailyJournalLineClient
    {
        internal bool TakeVoucher, AmountSetBySystem;
        internal string AccountFilledByInvoice;
        internal int AccountVersion;
        internal object accntSource;
        public object AccountSource { get { return accntSource; } }

        internal int OffsetAccountVersion;
        internal object offsetAccntSource;
        public object OffsetAccountSource { get { return offsetAccntSource; } }

        internal string TmpOffsetAccount;

        [Display(Name = "Total", ResourceType = typeof(GLDailyJournalText))]
        [NoSQL]
        public double Total { get { return _Total; } }
        double _Total;
        public void SetTotal(double total)
        {
            if (total != _Total)
            {
                _Total = total;
                NotifyPropertyChanged("Total");
            }
        }

        [Display(Name = "TraceBalance", ResourceType = typeof(GLDailyJournalText))]
        [NoSQL]
        public double TraceBalance { get { return _TraceSum != null ? _TraceSum[0] : 0d; } }

        public double[] TraceSum
        {
            get { return _TraceSum; }
            set { }
        }

        public void NotifyTranceSumChange() { NotifyPropertyChanged("TraceSum"); }

        public double[] _TraceSum;
        public bool SetTraceSum(double[] sum)
        {
            if (sum == null)
                return false;
            if (_TraceSum == null)
                _TraceSum = new double[sum.Length];
            bool changed = false;
            for (int i = sum.Length; (--i >= 0);)
            {
                var dif = _TraceSum[i] - sum[i];
                if (dif >= 0.005d || dif <= -0.005d)
                {
                    _TraceSum[i] = sum[i];
                    changed = true;
                    if (i == 0)
                        NotifyPropertyChanged("TraceBalance");
                }
            }
            return changed;
        }

        public void UpdateDefaultText()
        {
            this.NotifyPropertyChanged("DefaultText");
        }
        public string DefaultText
        {
            get
            {
                string s;
                if (_Asset != null)
                {
                    var rec = ClientHelper.GetRef(CompanyId, typeof(FAM), _Asset);
                    if (rec != null)
                        s = string.Concat(rec.KeyStr, ", ", rec.KeyName);
                    else
                        s = null;
                }
                else if (_AccountType > 0)
                    s = AccountName;
                else if (_OffsetAccountType > 0 && _OffsetAccount != null)
                    s = OffsetAccountName;
                else if (_OffsetVat != null && _Vat == null)
                    s = OffsetAccountName;
                else
                    s = AccountName;

                if (_Invoice != null)
                {
                    string t = (_DCPostType == Uniconta.DataModel.DCPostType.Creditnote) ? Uniconta.ClientTools.Localization.lookup("Creditnote") :
                               (_DCPostType == Uniconta.DataModel.DCPostType.Payment) ? Uniconta.ClientTools.Localization.lookup("Payment") :
                                                                                           Uniconta.ClientTools.Localization.lookup("Invoice");
                    s = string.Concat(s, " ", t, ": ", _Invoice);
                }
                if (_TransType == null)
                    return s;
                if (s == null)
                    return _TransType;
                return string.Concat(_TransType, " ", s);
            }
        }

        [Display(Name = "VatAmount", ResourceType = typeof(GLDailyJournalText))]
        [NoSQL]
        public double VatAmount { get { return _VatAmount; } }

        double _VatAmount;
        public void SetVatAmount(double amount)
        {
            if (_VatAmount != amount)
            {
                _VatAmount = amount;
                NotifyPropertyChanged("VatAmount");
            }
        }

        [Display(Name = "VatAmountOffset", ResourceType = typeof(GLDailyJournalText))]
        [NoSQL]
        public double VatAmountOffset { get { return _VatAmountOffset; } }

        double _VatAmountOffset;
        public void SetVatAmountOffset(double amount)
        {
            if (_VatAmountOffset != amount)
            {
                _VatAmountOffset = amount;
                NotifyPropertyChanged("VatAmountOffset");
            }
        }

        public bool HasOffsetAccounts { get { return _HasOffsetAccounts; } }

        internal object taskSource;
        public object TaskSource { get { return taskSource; } }
    }

    public class TraceSumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double[] tsum = value as double[];
            double val = 0d;
            if (tsum != null)
            {
                var n = Uniconta.Common.Utility.NumberConvert.ToInt(System.Convert.ToString(parameter));
                if (n < tsum.Length)
                    val = tsum[n];
            }
            return val == 0d ? string.Empty : val.ToString("n2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PrintReportFooter
    {
        public string TraceSum1Name { get; set; }
        public string TraceSum2Name { get; set; }
        public string TraceSum3Name { get; set; }
        public string TraceSum4Name { get; set; }
        public string TraceSum5Name { get; set; }
        public string TraceSum6Name { get; set; }

        public double? InitialSum1 { get; set; }
        public double? InitialSum2 { get; set; }
        public double? InitialSum3 { get; set; }
        public double? InitialSum4 { get; set; }
        public double? InitialSum5 { get; set; }
        public double? InitialSum6 { get; set; }

        public double? TraceSum1 { get; set; }
        public double? TraceSum2 { get; set; }
        public double? TraceSum3 { get; set; }
        public double? TraceSum4 { get; set; }
        public double? TraceSum5 { get; set; }
        public double? TraceSum6 { get; set; }

        public double? Movement1 { get { return TraceSum1 - InitialSum1; } }
        public double? Movement2 { get { return TraceSum2 - InitialSum2; } }
        public double? Movement3 { get { return TraceSum3 - InitialSum3; } }
        public double? Movement4 { get { return TraceSum4 - InitialSum4; } }
        public double? Movement5 { get { return TraceSum5 - InitialSum5; } }
        public double? Movement6 { get { return TraceSum6 - InitialSum6; } }

    }
}
