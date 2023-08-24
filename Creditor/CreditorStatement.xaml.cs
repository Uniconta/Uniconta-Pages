using UnicontaClient.Models;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using System.Threading.Tasks;
using Uniconta.API.Service;
using System.Windows.Data;
using DevExpress.Data.Filtering;
using Uniconta.Common.Utility;
using Newtonsoft.Json;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorStatementGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransClientTotal); } }
        public CreditorStatementGridClient(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public CreditorStatementGridClient()
        {
            CustomTableView tableView = new CustomTableView();
            tableView.AllowEditing = false;
            tableView.ShowGroupPanel = false;
            SetTableViewStyle(tableView);
            tableView.ShowTotalSummary = true;
            tableView.ShowGroupFooters = true;
            this.View = tableView;
        }
    }

    public class CreditorStatementList
    {
        public CreditorStatementList(Uniconta.DataModel.Creditor cred)
        {
            this.cred = cred ?? new Uniconta.DataModel.Creditor();
        }
        public readonly Uniconta.DataModel.Creditor cred;

        [Display(Name = "CAccount", ResourceType = typeof(GLTableText))]
        public string Account { get { return cred._Account; } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string Name { get { return cred._Name; } }

        [Display(Name = "Address1", ResourceType = typeof(DCAccountText))]
        public string Address1 { get { return cred._Address1; } }

        [Display(Name = "City", ResourceType = typeof(DCAccountText))]
        public string City { get { return cred._City; } }

        public CreditorTransClientTotal[] ChildRecords { get; set; }

        public double _SumAmount;
        public double SumAmount { get { return _SumAmount; } }

        public double _SumAccountCur;
        public double SumAccountCur { get { return _SumAccountCur; } }
    }

    public class CreditorTransClientTotal : CreditorTransClient
    {
        public double _SumAmount, _SumAmountCur, _OverDue, _OverDueCur;

        [Display(Name = "Total", ResourceType = typeof(GLDailyJournalText))]
        public double SumAmount { get { return _SumAmount; } }

        [Display(Name = "TotalCur", ResourceType = typeof(GLDailyJournalText))]
        public double SumAmountCur { get { return _SumAmountCur; } }

        [Display(Name = "DueDate", ResourceType = typeof(DCTransText))]
        public DateTime DueDate { get { return _DueDate; } }

        [Display(Name = "Remaining", ResourceType = typeof(DCTransText))]
        [NoSQL]
        public double AmountOpen { get { return _AmountOpen; } }

        [Display(Name = "OverDue", ResourceType = typeof(DCTransText))]
        [NoSQL]
        public double OverDue { get { return _OverDue; } }

        [Display(Name = "RemainingCur", ResourceType = typeof(DCTransText))]
        [NoSQL]
        public double AmountOpenCur { get { return _AmountOpenCur; } }

        [Display(Name = "OverDueCur", ResourceType = typeof(DCTransText))]
        [NoSQL]
        public double OverDueCur { get { return _OverDueCur; } }

        [Display(Name = "OrderNumber", ResourceType = typeof(DCOrderText))]
        [NoSQL]
        public int OrderNumber { get { return _OrderNumber; } set { _OrderNumber = value; NotifyPropertyChanged("OrderNumber"); } }

        [JsonIgnore]
        [ReportingAttribute]
        public CreditorOrderClient Order
        {
            get
            {
                if (_OrderNumber != 0)
                    return ClientHelper.GetRefClient<CreditorOrderClient>(CompanyId, typeof(CreditorOrder), NumberConvert.ToString(_OrderNumber));
                else
                    return null;
            }
        }
    }

    public partial class CreditorStatement : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string ToAccount { get; set; }

        SQLCache accountCache;
        ItemBase ibase, ibaseCurrent;

        static bool IsCollapsed = true;
        static int transaction;
        CWServerFilter creditorFilterDialog = null;
        bool creditorFilterCleared;
        TableField[] CreditorUserFields { get; set; }
        IEnumerable<PropValuePair> creditorFilterValues;
        bool OnlyOpen, OnlyDue = false;
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

        public override string NameOfControl { get { return TabControls.CreditorStatement; } }

        public override void AssignMultipleGrid(List<CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgCreditorTrans);
            gridCtrls.Add(dgChildCreditorTrans);
            isChildGridExist = true;
        }

        Uniconta.DataModel.Creditor _master;
        public CreditorStatement(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
        }

        public CreditorStatement(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            _master = syncEntity.Row as Uniconta.DataModel.Creditor;
            if (_master != null)
            { FromAccount = _master._Account; ToAccount = _master._Account; }
            Init();
            SetHeader();
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
            _master = args as Uniconta.DataModel.Creditor;
            if (_master != null)
            { FromAccount = _master._Account; ToAccount = _master._Account; }
            SetHeader();
            if (_master != null)
            {
                cmbFromAccount.EditValue = _master._Account;
                cmbToAccount.EditValue = _master._Account;
                LoadDCTrans();
            }
        }

        void Init()
        {
            this.DataContext = this;
            InitializeComponent();
            SetCreditorFilterUserFields();
            cmbFromAccount.api = cmbToAccount.api = api;
            SetRibbonControl(localMenu, dgCreditorTrans);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            DebtorStatement.SetDefaultDateTime();
            var Pref = api.session.Preference;
            cbxAscending.IsChecked = Pref.Debtor_isAscending;
            cbxSkipBlank.IsChecked = Pref.Debtor_skipBlank;

            txtDateTo.DateTime = DebtorStatement.DefaultToDate;
            txtDateFrm.DateTime = DebtorStatement.DefaultFromDate;
            cmbTrasaction.ItemsSource = AppEnums.TransToShow.Values;
            cmbTrasaction.SelectedIndex = transaction;
            GetMenuItem();

            var Comp = api.CompanyEntity;
            accountCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));

            if (Comp.RoundTo100)
                Amount.HasDecimals = colSumAmount.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
            if (_master != null)
            {
                cmbFromAccount.EditValue = _master._Account;
                cmbToAccount.EditValue = _master._Account;
                LoadDCTrans();
            }
            dgCreditorTrans.RowDoubleClick += DgCreditorTrans_RowDoubleClick;
            dgCreditorTrans.SelectedItemChanged += DgCreditorTrans_SelectedItemChanged;
            dgCreditorTrans.MasterRowExpanded += DgCreditorTrans_MasterRowExpanded;
            dgCreditorTrans.MasterRowCollapsed += DgCreditorTrans_MasterRowCollapsed;
        }
        bool maintainState;

        private void DgCreditorTrans_MasterRowCollapsed(object sender, RowEventArgs e)
        {
            maintainState = true;
            SetExpandCurrent();
        }

        private void DgCreditorTrans_MasterRowExpanded(object sender, RowEventArgs e)
        {
            maintainState = true; 
            SetCollapseCurrent();
        }

        private void DgCreditorTrans_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (dgCreditorTrans.SelectedItem == null || !dgCreditorTrans.IsVisible)
                return;
            SetExpandAndCollapseCurrent(false, false);
        }

        private void DgCreditorTrans_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("VoucherTransactions");
        }

        void MasterRowExpanded(object sender, RowEventArgs e)
        {
            var detailView = GetDetailView(e.RowHandle);
            if (detailView == null)
                return;
            detailView.ShowSearchPanelMode = ShowSearchPanelMode.Never;
            detailView.SearchPanelHighlightResults = true;
            BindingOperations.SetBinding(detailView, DataViewBase.SearchStringProperty, new Binding("SearchText") { Source = ribbonControl.SearchControl });
        }

        TableView GetDetailView(int rowHandle)
        {
            var detail = dgCreditorTrans.GetDetail(rowHandle) as GridControl;
            return detail == null ? null : detail.View as TableView;
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
                var visibleColumns = dgChildCreditorTrans.Columns.Where(c => c.Visible).Select(c => string.IsNullOrEmpty(c.FieldName) ? c.Name : c.FieldName);
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

        void SetCreditorFilterUserFields()
        {
            var Comp = api.CompanyEntity;
            var creditorRow = new CreditorClient();
            creditorRow.SetMaster(Comp);
            var creditorUserField = creditorRow.UserFieldDef();
            if (creditorUserField != null)
            {
                CreditorUserFields = creditorUserField;
            }
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (accountCache == null)
                accountCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ExpandAndCollapse");
            ibaseCurrent = UtilDisplay.GetMenuCommandByName(rb, "ExpandCollapseCurrent");
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {

            switch (ActionType)
            {
                case "ExpandAndCollapse":
                    if (dgCreditorTrans.ItemsSource == null)
                        return;
                    IsCollapsed = dgCreditorTrans.IsMasterRowExpanded(0);
                    SetExpandAndCollapse(IsCollapsed);
                    break;
                case "ViewDownloadRow":
                    var selectedItem = dgChildCreditorTrans.SelectedItem as CreditorTransClientTotal;
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgChildCreditorTrans.syncEntity, api, busyIndicator);
                    break;
                case "OpenTran":
                    var dc = (dgChildCreditorTrans.SelectedItem as CreditorTransClientTotal)?.Creditor ?? (dgCreditorTrans.SelectedItem as CreditorStatementList)?.cred;
                    if (dc != null)
                        AddDockItem(TabControls.CreditorOpenTransactions, dc, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OpenTrans"), dc._Account));
                    break;
                case "VoucherTransactions":
                    var selItem = dgChildCreditorTrans.SelectedItem as CreditorTransClientTotal;
                    if (selItem != null)
                        AddDockItem(TabControls.AccountsTransaction, dgChildCreditorTrans.syncEntity, Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selItem._Voucher));
                    break;
                case "CreditorFilter":
                    if (creditorFilterDialog == null)
                    {
                        if (creditorFilterCleared)
                            creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), null, null, CreditorUserFields);
                        else
                            creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), null, null, CreditorUserFields);
                        creditorFilterDialog.GridSource = dgCreditorTrans.ItemsSource as IList<UnicontaBaseEntity>;
                        creditorFilterDialog.Closing += creditorFilterDialog_Closing;
                        creditorFilterDialog.Show();
                    }
                    else
                    {
                        creditorFilterDialog.GridSource = dgCreditorTrans.ItemsSource as IList<UnicontaBaseEntity>;
                        creditorFilterDialog.Show(true);
                    }
                    break;
                case "ClearCreditorFilter":
                    creditorFilterDialog = null;
                    creditorFilterValues = null;
                    creditorFilterCleared = true;
                    break;
                case "ExpandCollapseCurrent":
                    if (dgCreditorTrans.SelectedItem != null)
                        SetExpandAndCollapseCurrent(true, false);
                    else if (dgChildCreditorTrans.SelectedItem != null)
                        SetExpandAndCollapseCurrent(true, true);
                    break;
                case "Search":
                    dgChildCreditorTrans.SelectedItem = null;
                    LoadDCTrans();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void SetExpandAndCollapseCurrent(bool changeState, bool IsChild)
        {
            if (ibaseCurrent == null)
                return;

            bool expandState = true;
            int selectedMasterRowhandle;

            if (!IsChild)
            {
                var selectedRowHandle = dgCreditorTrans.GetSelectedRowHandles();
                selectedMasterRowhandle = selectedRowHandle.Length > 0 ? selectedRowHandle[0] : 0;
                expandState = dgCreditorTrans.IsMasterRowExpanded(selectedMasterRowhandle);
            }
            else
                selectedMasterRowhandle = ((TableView)dgCreditorTrans.View.MasterRootRowsContainer.FocusedView).Grid.GetMasterRowHandle();

            if (!expandState)
            {
                if (changeState)
                {
                    ExpandAndCollapseCurrent(false, selectedMasterRowhandle);
                    SetCollapseCurrent();
                }
                else
                    SetExpandCurrent();
            }
            else
            {
                if (changeState)
                {
                    ExpandAndCollapseCurrent(true, selectedMasterRowhandle);
                    SetExpandCurrent();
                }
                else
                    SetCollapseCurrent();
            }
        }

        private void SetCollapseCurrent()
        {
            if (ibaseCurrent == null)
                return;

            ibaseCurrent.Caption = Uniconta.ClientTools.Localization.lookup("CollapseCurrent");
            ibaseCurrent.LargeGlyph = Utilities.Utility.GetGlyph("Collapse_32x32");
        }

        private void SetExpandCurrent()
        {
            if (ibaseCurrent == null)
                return;

            ibaseCurrent.Caption = Uniconta.ClientTools.Localization.lookup("ExpandCurrent");
            ibaseCurrent.LargeGlyph = Utilities.Utility.GetGlyph("Expand_32x32");
        }

        void ExpandAndCollapseCurrent(bool IsCollapseAll, int currentRowHandle)
        {
            if (!IsCollapseAll)
                dgCreditorTrans.ExpandMasterRow(currentRowHandle);
            else
                dgCreditorTrans.CollapseMasterRow(currentRowHandle);
        }

        void creditorFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (creditorFilterDialog.DialogResult == true)
            {
                creditorFilterValues = creditorFilterDialog.PropValuePair;
            }
            e.Cancel = true;
            creditorFilterDialog.Hide();
        }

        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            LoadDCTrans();
        }

        private void SetExpandAndCollapse(bool expandState)
        {
            if (ibase == null)
                return;
            if (dgCreditorTrans.ItemsSource == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !expandState)
                ExpandAndCollapseAll(false);
            else if (expandState)
                ExpandAndCollapseAll(true);
        }

        void ExpandAndCollapseAll(bool IsCollapseAll)
        {
            for (int iRow = 0; iRow < dataRowCount; iRow++)
                if (!IsCollapseAll)
                    dgCreditorTrans.ExpandMasterRow(iRow);
                else
                    dgCreditorTrans.CollapseMasterRow(iRow);
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

        private void cmbTrasaction_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbTrasaction.SelectedIndex == 0)
                OnlyOpen = OnlyDue = false;
            else if (cmbTrasaction.SelectedIndex == 1)
            {
                OnlyOpen = true;
                OnlyDue = false;
            }
            else if (cmbTrasaction.SelectedIndex == 2)
                OnlyOpen = OnlyDue = true;
        }

        async void LoadDCTrans()
        {
            CreditorStatement.SetDateTime(txtDateFrm, txtDateTo);
            DateTime fromDate = DebtorStatement.DefaultFromDate, toDate = DebtorStatement.DefaultToDate;
            transaction = cmbTrasaction.SelectedIndex;

            var fromAccount = Convert.ToString(cmbFromAccount.EditValue);
            var toAccount = Convert.ToString(cmbToAccount.EditValue);

            busyIndicator.IsBusy = true;
            var transApi = new ReportAPI(api);
            var listTrans = (CreditorTransClientTotal[])await transApi.GetTransWithPrimo(new CreditorTransClientTotal(), fromDate, toDate, fromAccount, toAccount, OnlyOpen, null, creditorFilterValues, OnlyDue);
            if (listTrans != null)
            {
                if (accountCache == null)
                    accountCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor));

                FillStatement(listTrans, OnlyOpen, toDate);
            }
            else if (transApi.LastError != 0)
            {
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(transApi.LastError);
            }

            dgCreditorTrans.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;

            if (statementList?.Count == 1)
                IsCollapsed = false;
            ExpandAndCollapseAll(IsCollapsed);
            maintainState = false;
        }
        List<CreditorStatementList> statementList = null;
        void FillStatement(CreditorTransClientTotal[] listTrans, bool OnlyOpen, DateTime toDate)
        {
            var isAscending = cbxAscending.IsChecked.GetValueOrDefault();
            var skipBlank = cbxSkipBlank.IsChecked.GetValueOrDefault();

            var Pref = api.session.Preference;
            Pref.Debtor_isAscending = isAscending;
            Pref.Debtor_skipBlank = skipBlank;
            Pref.Debtor_OnlyOpen = OnlyOpen;

            statementList = new List<CreditorStatementList>(Math.Min(20, dataRowCount));

            string currentItem = string.Empty;
            CreditorStatementList masterCredStatement = null;
            var tlst = new List<CreditorTransClientTotal>(100);
            double SumAmount = 0d, SumAmountCur = 0d;

            for (int n = 0; (n < listTrans.Length); n++)
            {
                var trans = listTrans[n];
                if (trans._Account != currentItem)
                {
                    currentItem = trans._Account;

                    if (masterCredStatement != null)
                    {
                        if (!skipBlank || SumAmount != 0 || tlst.Count > 1)
                        {
                            statementList.Add(masterCredStatement);
                            masterCredStatement.ChildRecords = tlst.ToArray();
                            if (!isAscending)
                                Array.Reverse(masterCredStatement.ChildRecords);
                        }
                    }

                    masterCredStatement = new CreditorStatementList((Uniconta.DataModel.Creditor)accountCache.Get(currentItem));
                    tlst.Clear();
                    SumAmount = SumAmountCur = 0d;
                    if (trans._Text == null && trans._Primo)
                        trans._Text = Uniconta.ClientTools.Localization.lookup("Primo");
                }
                SumAmount += OnlyOpen ? trans._AmountOpen : trans._Amount;
                trans._SumAmount = SumAmount;
                masterCredStatement._SumAmount = SumAmount;

                SumAmountCur += OnlyOpen ? trans._AmountOpenCur : trans._AmountCur;
                trans._SumAmountCur = SumAmountCur;
                masterCredStatement._SumAccountCur = SumAmountCur;

                if (trans._DueDate <= toDate)
                {
                    trans._OverDue = trans._AmountOpen;
                    trans._OverDueCur = trans._AmountOpenCur;
                }

                tlst.Add(trans);
            }

            if (masterCredStatement != null)
            {
                if (!skipBlank || SumAmount != 0 || tlst.Count > 1)
                {
                    statementList.Add(masterCredStatement);
                    masterCredStatement.ChildRecords = tlst.ToArray();
                    if (!isAscending)
                        Array.Reverse(masterCredStatement.ChildRecords);
                }
            }

            dataRowCount = statementList.Count;
            dgCreditorTrans.ItemsSource = null;
            if (dataRowCount > 0)
                dgCreditorTrans.ItemsSource = statementList;
        }

        private void cbxPageBreak_Click(object sender, RoutedEventArgs e)
        {
            var tableView = (CustomTableView)dgCreditorTrans.View;
            if (cbxPageBreak.IsChecked == true)
                tableView.HasPageBreak = true;
            else
                tableView.HasPageBreak = false;
        }

        private void cmbFromAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            cmbToAccount.SelectedItem = cmbFromAccount.SelectedItem;
        }

        int dataRowCount;
        public override object GetPrintParameter()
        {
            if (!maintainState)
            {
                for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                    dgCreditorTrans.ExpandMasterRow(rowHandle);
            }
            return base.GetPrintParameter();
        }
    }
}
