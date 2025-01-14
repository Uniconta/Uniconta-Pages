using UnicontaClient.Models;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.GeneralLedger;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System.Windows.Data;
using DevExpress.Data.Filtering;
using DevExpress.Xpf.Editors;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AccountsStatementGrid : CorasauDataGridClient
    {
        public delegate void PrintClickDelegate();

        public override Type TableType { get { return typeof(GLTransClientTotal); } }

        public bool PageBreak { get; internal set; }

        public AccountsStatementGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public AccountsStatementGrid()
        {
            CustomTableView tv = new CustomTableView();
            tv.AllowEditing = false;
            tv.ShowGroupPanel = false;
            SetTableViewStyle(tv);
            tv.ShowTotalSummary = true;
            tv.ShowGroupFooters = true;
            this.View = tv;
        }
        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            ((CustomTableView)View).HasPageBreak = PageBreak;
            base.PrintGrid(reportName, printparam, format, page);
        }
    }

    public class DataControlDetailDescriptorCls : DataControlDetailDescriptor
    {

    }

    public class AccountStatementList : INotifyPropertyChanged
    {
        public AccountStatementList(GLAccount Acc)
        {
            this.Acc = Acc ?? new GLAccount();
        }
        public readonly GLAccount Acc;

        [Display(Name = "Account", ResourceType = typeof(GLTableText))]
        public string AccountNumber { get { return Acc._Account; } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string Name { get { return Acc._Name; } }

        [Display(Name = "AccountType", ResourceType = typeof(GLTableText))]
        public string AccountType { get { return AppEnums.GLAccountTypes.ToString(Acc._AccountType); } }

        [Display(Name = "Vat", ResourceType = typeof(GLTableText))]
        public string Vat { get { return Acc._Vat; } }

        [Display(Name = "Total", ResourceType = typeof(GLDailyJournalText))]
        public double AccountTotal { get { return _SumAmount; } }

        [Display(Name = "TotalCur", ResourceType = typeof(GLDailyJournalText))]
        public double AccountTotalCur { get { return _SumAmountCur; } }

        [Display(Name = "Currency", ResourceType = typeof(GLDailyJournalText))]
        public string Currency { get { return Acc._Currency > 0 ? Acc._Currency.ToString() : null; } }

        public GLTransClientTotal[] ChildRecords { get; set; }

        public double _SumAmount;
        public double SumAmount { get { return _SumAmount; } }

        public double _SumAmountCur;
        public double SumAmountCur { get { return _SumAmountCur; } }

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class AccountStatement : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string ToAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        public string Journal { get; set; }

        SQLCache accountCache;
        ItemBase ibase, ibaseCurrent;
        ReportAPI transApi;
        Uniconta.DataModel.GLAccount _master;
        static bool pageBreak;
        static int setShowDimOnPrimoIndex;
        static bool IsCollapsed = true;
        public static void SetDateTime(DateEditor frmDateeditor, DateEditor todateeditor)
        {
            if (frmDateeditor.Text == string.Empty)
                DebtorStatement.DefaultFromDate = DateTime.MinValue;
            else
                DebtorStatement.DefaultFromDate = frmDateeditor.DateTime.Date;
            if (todateeditor.Text == string.Empty)
                DebtorStatement.DefaultToDate = DateTime.MinValue;
            else
                DebtorStatement.DefaultToDate = todateeditor.DateTime.Date;
        }

        public AccountStatement(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }

        public AccountStatement(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            _master = syncEntity.Row as Uniconta.DataModel.GLAccount;
            if (_master != null)
                FromAccount = ToAccount = _master._Account;
            Init(null);
            SetHeader();
        }

        public AccountStatement(UnicontaBaseEntity master) : base(master)
        {
            Uniconta.DataModel.GLClosingSheet sheet = null;
            _master = master as Uniconta.DataModel.GLAccount;
            if (_master != null)
                FromAccount = ToAccount = _master._Account;
            else
            {
                sheet = master as Uniconta.DataModel.GLClosingSheet;
                if (sheet != null)
                {
                    FromAccount = sheet._FromAccount;
                    ToAccount = sheet._ToAccount;
                }
            }
            Init(sheet);
        }

        private void SetHeader()
        {
            string key = _master._Account;
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Statement"), key);
            SetHeader(header);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            _master = args as GLAccount;
            if (_master != null)
            {
                FromAccount = ToAccount = _master._Account;
                SetHeader();
                if (cmbFromAccount != null)
                {
                    cmbFromAccount.EditValue = _master._Account;
                    cmbToAccount.EditValue = _master._Account;
                }
                LoadGLTrans();
            }
        }

        void Init(GLClosingSheet sheet)
        {
            this.DataContext = this;
            InitializeComponent();
            cmbFromAccount.api = cmbToAccount.api = api;
            SetRibbonControl(localMenu, dgGLTrans);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            DebtorStatement.SetDefaultDateTime();
            var Pref = api.session.Preference;
            cbxAscending.IsChecked = Pref.Debtor_isAscending;
            cbxSkipBlank.IsChecked = Pref.Debtor_skipBlank;
            cbxPageBreak.IsChecked = pageBreak;
            dgGLTrans.PageBreak = pageBreak;

            cmbShowDimOnPrimo.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("OnePrimo"), Uniconta.ClientTools.Localization.lookup("PrimoPerDim"), Uniconta.ClientTools.Localization.lookup("NoPrimo") };
            cmbShowDimOnPrimo.SelectedIndex = setShowDimOnPrimoIndex;
            txtDateTo.DateTime = DebtorStatement.DefaultToDate;
            txtDateFrm.DateTime = DebtorStatement.DefaultFromDate;
            GetMenuItem();

            var Comp = api.CompanyEntity;
            if (Comp.RoundTo100)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = false;

            accountCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount));
            InitialLoad();
            dgGLTrans.RowDoubleClick += DgGLTrans_RowDoubleClick;
            dgGLTrans.SelectedItemChanged += DgGLTrans_SelectedItemChanged;
            dgGLTrans.MasterRowExpanded += DgGLTrans_MasterRowExpanded;
            dgGLTrans.MasterRowCollapsed += DgGLTrans_MasterRowCollapsed;
            dgGLTrans.ShowTotalSummary();
            transApi = new Uniconta.API.GeneralLedger.ReportAPI(api);
            transApi.SetClosingSheet(sheet);

            if (_master != null)
            {
                cmbFromAccount.EditValue = _master._Account;
                cmbToAccount.EditValue = _master._Account;
                LoadGLTrans();
            }
        }

        bool maintainState;
        private void DgGLTrans_MasterRowCollapsed(object sender, RowEventArgs e)
        {
            maintainState = true;
            SetExpandCurrent();
        }

        private void DgGLTrans_MasterRowExpanded(object sender, RowEventArgs e)
        {
            maintainState = true;
            SetCollapseCurrent();
        }

        private void DgGLTrans_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (dgGLTrans.SelectedItem == null || !dgGLTrans.IsVisible)
                return;
            SetExpandAndCollapseCurrent(false, false);
        }

        private void DgGLTrans_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("VoucherTransactions");
        }
        private void Name_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DgGLTrans_RowDoubleClick();
        }
        void MasterRowExpanded(object sender, RowEventArgs e)
        {
            var detailView = GetDetailView(e.RowHandle);
            if (detailView == null)
                return;
            detailView.ShowSearchPanelMode = ShowSearchPanelMode.Never;
            detailView.SearchPanelHighlightResults = true;
            var searchControl = ribbonControl?.SearchControl;
            if (searchControl != null)
                BindingOperations.SetBinding(detailView, DataViewBase.SearchStringProperty, new Binding("SearchText") { Source = searchControl });
        }

        TableView GetDetailView(int rowHandle)
        {
            var detail = dgGLTrans.GetDetail(rowHandle) as GridControl;
            return detail == null ? null : detail.View as TableView;
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        void SubstituteFilter(object sender, DevExpress.Data.SubstituteFilterEventArgs e)
        {
            if (string.IsNullOrEmpty(ribbonControl?.SearchControl?.SearchText))
                return;
            e.Filter = new GroupOperator(GroupOperatorType.Or, e.Filter, GetDetailFilter(ribbonControl.SearchControl.SearchText));
        }

        List<OperandProperty> operands;
        AggregateOperand GetDetailFilter(string searchString)
        {
            if (operands == null)
            {
                var visibleColumns = childDgGLTrans.Columns.Where(c => c.Visible).Select(c => string.IsNullOrEmpty(c.FieldName) ? c.Name : c.FieldName);
                operands = new List<OperandProperty>();
                foreach (var col in visibleColumns)
                    operands.Add(new OperandProperty(col));
            }
            GroupOperator detailOperator = new GroupOperator(GroupOperatorType.Or);
            foreach (var op in operands)
                detailOperator.Operands.Add(new FunctionOperator(FunctionOperatorType.Contains, op, new OperandValue(searchString)));
            return new AggregateOperand("ChildRecords", Aggregate.Exists, detailOperator);
        }

        public override Task InitQuery()
        {
            return null;
        }

        void InitialLoad()
        {
            TransactionReport.SetDailyJournal(cmbJournal, api);
            SetNoOfDimensions();
            StartLoadCache();
        }

        public override void AssignMultipleGrid(List<CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgGLTrans);
            gridCtrls.Add(childDgGLTrans);
            isChildGridExist = true;
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (accountCache == null)
                accountCache = await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.NumberSerie) });
        }

        void LocalMenu_OnItemClicked(string ActionType)
        {
            GLTransClientTotal selectedItem = dgGLTrans.View.MasterRootRowsContainer.FocusedView.DataControl.CurrentItem as GLTransClientTotal;

            switch (ActionType)
            {
                case "PostedTransaction":
                    if (selectedItem == null)
                        return;
                    string header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem._JournalPostedId);
                    AddDockItem(TabControls.PostedTransactions, selectedItem, header);
                    break;
                case "ViewDownloadRow":
                    var selectedRow = childDgGLTrans.SelectedItem as GLTransClientTotal;
                    if (selectedRow != null)
                        DebtorTransactions.ShowVoucher(childDgGLTrans.syncEntity, api, busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), NumberConvert.ToString(selectedItem._Voucher));
                    AddDockItem(TabControls.AccountsTransaction, childDgGLTrans.syncEntity, vheader);
                    break;
                case "ExpandAndCollapse":
                    if (dgGLTrans.ItemsSource != null)
                    {
                        IsCollapsed = dgGLTrans.IsMasterRowExpanded(0);
                        SetExpandAndCollapse(IsCollapsed);
                    }
                    break;
                case "Search":
                    LoadGLTrans();
                    break;
                case "ExpandCollapseCurrent":
                    if (dgGLTrans.SelectedItem != null)
                        SetExpandAndCollapseCurrent(true, false);
                    else if (childDgGLTrans.SelectedItem != null)
                        SetExpandAndCollapseCurrent(true, true);
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        AccountsTransaction.JournalPosted(selectedItem, api);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void SetExpandAndCollapseCurrent(bool changeState, bool isChild)
        {
            if (ibaseCurrent == null)
                return;

            bool expandState = true;
            int selectedMasterRowHandle;

            if (!isChild)
            {
                var selectedRowHandle = dgGLTrans.GetSelectedRowHandles();
                selectedMasterRowHandle = selectedRowHandle.FirstOrDefault();
                expandState = dgGLTrans.IsMasterRowExpanded(selectedRowHandle.FirstOrDefault());
            }
            else
                selectedMasterRowHandle = ((TableView)dgGLTrans.View.MasterRootRowsContainer.FocusedView).Grid.GetMasterRowHandle();

            if (!expandState)
            {
                if (changeState)
                {
                    ExpandAndCollapseCurrent(false, selectedMasterRowHandle);
                    SetCollapseCurrent();
                }
                else
                    SetExpandCurrent();
            }
            else
            {
                if (changeState)
                {
                    ExpandAndCollapseCurrent(true, selectedMasterRowHandle);
                    SetExpandCurrent();
                }
                else
                    SetCollapseCurrent();
            }
        }

        private void SetCollapseCurrent()
        {
            if (ibaseCurrent != null)
            {
                ibaseCurrent.Caption = Uniconta.ClientTools.Localization.lookup("CollapseCurrent");
                ibaseCurrent.LargeGlyph = Utility.GetGlyph("Collapse_32x32");
            }
        }

        private void SetExpandCurrent()
        {
            if (ibaseCurrent != null)
            {
                ibaseCurrent.Caption = Uniconta.ClientTools.Localization.lookup("ExpandCurrent");
                ibaseCurrent.LargeGlyph = Utilities.Utility.GetGlyph("Expand_32x32");
            }
        }

        private void SetExpandAndCollapse(bool expandState)
        {
            if (ibase == null)
                return;
            if (dgGLTrans.ItemsSource == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !expandState)
                ExpandAndCollapseAll(false);
            else if (expandState)
                ExpandAndCollapseAll(true);

        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ExpandAndCollapse");
            ibaseCurrent = UtilDisplay.GetMenuCommandByName(rb, "ExpandCollapseCurrent");
        }

        int dataRowCount;
        void ExpandAndCollapseAll(bool IsCollapseAll)
        {
            for (int iRow = 0; iRow < dataRowCount; iRow++)
                if (!IsCollapseAll)
                    dgGLTrans.ExpandMasterRow(iRow);
                else
                    dgGLTrans.CollapseMasterRow(iRow);
            if (IsCollapseAll)
            {
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("Expand_32x32");
            }
            else
            {
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("CollapseAll");
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("Collapse_32x32");
            }
        }

        void ExpandAndCollapseCurrent(bool IsCollapseAll, int currentRowHandle)
        {
            if (!IsCollapseAll)
                dgGLTrans.ExpandMasterRow(currentRowHandle);
            else
                dgGLTrans.CollapseMasterRow(currentRowHandle);
        }

        bool SimulatedVisible;
        async void LoadGLTrans()
        {
            childDgGLTrans.SelectedItem = null;

            childDgGLTrans.ClearSorting();
            List<int> dim1 = null, dim2 = null, dim3 = null, dim4 = null, dim5 = null;

            var NumberOfDimensions = api.CompanyEntity.NumberOfDimensions;
            if (NumberOfDimensions >= 1)
                dim1 = TransactionReport.GetRowIDs(cbdim1);
            if (NumberOfDimensions >= 2)
                dim2 = TransactionReport.GetRowIDs(cbdim2);
            if (NumberOfDimensions >= 3)
                dim3 = TransactionReport.GetRowIDs(cbdim3);
            if (NumberOfDimensions >= 4)
                dim4 = TransactionReport.GetRowIDs(cbdim4);
            if (NumberOfDimensions >= 5)
                dim5 = TransactionReport.GetRowIDs(cbdim5);
            AccountStatement.SetDateTime(txtDateFrm, txtDateTo);
            DateTime fromDate = DebtorStatement.DefaultFromDate, toDate = DebtorStatement.DefaultToDate;

            var showDimOnPrimo = cmbShowDimOnPrimo.SelectedIndex;

            pageBreak = cbxPageBreak.IsChecked.GetValueOrDefault();
            setShowDimOnPrimoIndex = showDimOnPrimo;

            string fromAccount = null, toAccount = null;
            var accountObj = cmbFromAccount.EditValue;
            if (accountObj != null)
                fromAccount = accountObj.ToString();

            accountObj = cmbToAccount.EditValue;
            if (accountObj != null)
                toAccount = accountObj.ToString();

            busyIndicator.IsBusy = true;

            if (Simulated.Visible)
                SimulatedVisible = true;
            string journal = cmbJournal.Text;
            Simulated.Visible = SimulatedVisible && !string.IsNullOrWhiteSpace(journal);

            var dimensionParams = BalanceReport.SetDimensionParameters(dim1, dim2, dim3, dim4, dim5, true, true, true, true, true);
            var listTrans = (GLTransClientTotal[])await transApi.GetTransactions(new GLTransClientTotal(), journal, fromAccount, toAccount, fromDate, toDate, dimensionParams, showDimOnPrimo);
            if (accountCache == null)
                accountCache = await transApi.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.GLAccount), api);

            if (listTrans != null)
                FillStatement(listTrans);
            else if (transApi.LastError != 0)
            {
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(transApi.LastError);
            }
            dgGLTrans.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
            if (statementList?.Count == 1)
                IsCollapsed = false;
            ExpandAndCollapseAll(IsCollapsed);
            maintainState = false;
        }
        List<AccountStatementList> statementList = null;
        void FillStatement(GLTransClientTotal[] listTrans)
        {
            var isAscending = cbxAscending.IsChecked.GetValueOrDefault();
            var skipBlank = cbxSkipBlank.IsChecked.GetValueOrDefault();

            var Pref = api.session.Preference;
            Pref.TransactionReport_isAscending = isAscending;
            Pref.TransactionReport_skipBlank = skipBlank;

            statementList = new List<AccountStatementList>(Math.Min(20, dataRowCount));

            string currentItem = string.Empty;
            AccountStatementList masterDbStatement = null;
            var tlst = new List<GLTransClientTotal>(1000);
            long SumAmount = 0, SumAmountCur = 0;
            byte currentCur = 0;
            bool TransFound = false;

            GLTransClientTotal trans;
            for (int n = 0; (n < listTrans.Length); n++)
            {
                trans = listTrans[n];
                if (trans._Account != currentItem)
                {
                    currentItem = trans._Account;

                    if (TransFound)
                    {
                        masterDbStatement._SumAmount = SumAmount / 100d;
                        masterDbStatement._SumAmountCur = SumAmountCur / 100d;
                        masterDbStatement.ChildRecords = tlst.ToArray();
                        statementList.Add(masterDbStatement);
                        if (!isAscending)
                            Array.Reverse(masterDbStatement.ChildRecords);
                    }

                    masterDbStatement = new AccountStatementList((GLAccount)accountCache.Get(currentItem));
                    tlst.Clear();
                    SumAmount = SumAmountCur = 0;
                    currentCur = (byte)masterDbStatement.Acc._Currency;
                    TransFound = !skipBlank;
                }

                if (!TransFound && !trans._PrimoTrans)
                    TransFound = true;

                SumAmount += trans._AmountCent;
                trans._Total = SumAmount;
                masterDbStatement._SumAmount = SumAmount;

                if (trans._AmountCurCent != 0 && trans._Currency == currentCur)
                {
                    SumAmountCur += trans._AmountCurCent;
                    trans._TotalCur = SumAmountCur;
                }

                tlst.Add(trans);
            }

            if (TransFound)
            {
                masterDbStatement._SumAmount = SumAmount / 100d;
                masterDbStatement._SumAmountCur = SumAmountCur / 100d;
                masterDbStatement.ChildRecords = tlst.ToArray();
                statementList.Add(masterDbStatement);
                if (!isAscending)
                    Array.Reverse(masterDbStatement.ChildRecords);
            }
            dataRowCount = statementList.Count;
            dgGLTrans.ItemsSource = null;
            dgGLTrans.ItemsSource = statementList;
            childDgGLTrans.RefreshData();
        }

        private void cbxPageBreak_Click(object sender, RoutedEventArgs e)
        {
            dgGLTrans.PageBreak = (cbxPageBreak.IsChecked == true);
        }

        public override object GetPrintParameter()
        {
            if (!maintainState)
            {
                for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                    dgGLTrans.ExpandMasterRow(rowHandle);
            }
            var FromDate = txtDateFrm.Text == string.Empty ? string.Empty : txtDateFrm.DateTime.ToShortDateString();
            var ToDate = txtDateTo.Text == string.Empty ? string.Empty : txtDateTo.DateTime.ToShortDateString();
            var printData = new PageReportHeader()
            {
                CurDateTime = DateTime.Now.ToString("g"),
                CompanyName = api.CompanyEntity._Name,
                ReportName = Uniconta.ClientTools.Localization.lookup("AccountStatement"),
                Header = string.Format("({0} - {1})", FromDate, ToDate),
                SecondLine = cmbJournal.SelectedItem == null ? "" : Uniconta.ClientTools.Localization.lookup("JournalLinesIncluded")
            };
            return printData;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var c = api.CompanyEntity;
            cldim1.Header = lblDim1.Text = c._Dim1;
            cldim2.Header = lblDim2.Text = c._Dim2;
            cldim3.Header = lblDim3.Text = c._Dim3;
            cldim4.Header = lblDim4.Text = c._Dim4;
            cldim5.Header = lblDim5.Text = c._Dim5;
        }

        private void cmbFromAccount_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            cmbToAccount.SelectedItem = cmbFromAccount.SelectedItem;
        }

        private async void SetNoOfDimensions()
        {
            var api = this.api;
            int noofDimensions = api.CompanyEntity.NumberOfDimensions;
            if (noofDimensions < 5)
            {
                lblDim5.Visibility = Visibility.Collapsed;
                cldim5.Visible = false;
                cbdim5.Visibility = Visibility.Collapsed;
                cldim5.ShowInColumnChooser = false;
            }
            else
            {
                await TransactionReport.SetDimValues(typeof(GLDimType5), cbdim5, api);
                cldim5.ShowInColumnChooser = true;
            }

            if (noofDimensions < 4)
            {
                lblDim4.Visibility = Visibility.Collapsed;
                cldim4.Visible = false;
                cbdim4.Visibility = Visibility.Collapsed;
                cldim4.ShowInColumnChooser = false;
            }
            else
            {
                await TransactionReport.SetDimValues(typeof(GLDimType4), cbdim4, api);
                cldim4.ShowInColumnChooser = true;
            }

            if (noofDimensions < 3)
            {
                lblDim3.Visibility = Visibility.Collapsed;
                cldim3.Visible = false;
                cbdim3.Visibility = Visibility.Collapsed;
                cldim3.ShowInColumnChooser = false;
            }
            else
            {
                await TransactionReport.SetDimValues(typeof(GLDimType3), cbdim3, api);
                cldim3.ShowInColumnChooser = true;
            }

            if (noofDimensions < 2)
            {
                lblDim2.Visibility = Visibility.Collapsed;
                cldim2.Visible = false;
                cbdim2.Visibility = Visibility.Collapsed;
                cldim2.ShowInColumnChooser = false;
            }
            else
            {
                await TransactionReport.SetDimValues(typeof(GLDimType2), cbdim2, api);
                cldim2.ShowInColumnChooser = true;
            }

            if (noofDimensions < 1)
            {
                lblDim1.Visibility = Visibility.Collapsed;
                cldim1.Visible = false;
                cbdim1.Visibility = Visibility.Collapsed;
                cldim1.ShowInColumnChooser = false;
            }
            else
            {
                await TransactionReport.SetDimValues(typeof(GLDimType1), cbdim1, api);
                cldim1.ShowInColumnChooser = true;
            }
        }

        private void HasVoucherImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var gLTransClientTotal = (sender as Image).Tag as GLTransClientTotal;
            if (gLTransClientTotal != null)
                DebtorTransactions.ShowVoucher(childDgGLTrans.syncEntity, api, busyIndicator);
        }
    }
}