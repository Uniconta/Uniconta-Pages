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
        Uniconta.DataModel.Creditor cred;

        [Display(Name = "CAccount", ResourceType = typeof(GLTableText))]
        public string AccountNumber { get { return cred._Account; } }

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
    }

    public partial class CreditorStatement : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string ToAccount { get; set; }

        SQLCache accountCache;
        ItemBase ibase, ibaseCurrent;

        static public DateTime DefaultFromDate, DefaultToDate;
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
                DefaultFromDate = DateTime.MinValue;
            else
                DefaultFromDate = frmDateeditor.DateTime.Date;
            if (todateeditor.Text == string.Empty)
                DefaultToDate = DateTime.MinValue;
            else
                DefaultToDate = todateeditor.DateTime.Date;
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

            if (CreditorStatement.DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();
                CreditorStatement.DefaultToDate = now;
                CreditorStatement.DefaultFromDate = now.AddDays(1 - now.Day).AddMonths(-2);
            }

            var Pref = api.session.Preference;
            cbxAscending.IsChecked = Pref.Debtor_isAscending;
            cbxSkipBlank.IsChecked = Pref.Debtor_skipBlank;

            txtDateTo.DateTime = CreditorStatement.DefaultToDate;
            txtDateFrm.DateTime = CreditorStatement.DefaultFromDate;
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
#if SILVERLIGHT
            dgChildCreditorTrans.CurrentItemChanged += ChildDgCreditorTrans_CurrentItemChanged;
#endif
        }

        private void DgCreditorTrans_MasterRowCollapsed(object sender, RowEventArgs e)
        {
            SetExpandCurrent();
        }

        private void DgCreditorTrans_MasterRowExpanded(object sender, RowEventArgs e)
        {
            SetCollapseCurrent();
        }

        private void DgCreditorTrans_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
#if SILVERLIGHT
            if(dgCreditorTrans.SelectedItem==null || dgCreditorTrans.Visibility == Visibility.Collapsed)
#else
            if (dgCreditorTrans.SelectedItem == null || !dgCreditorTrans.IsVisible)
#endif
                return;

            SetExpandAndCollapseCurrent(false, false);
        }

        private void DgCreditorTrans_RowDoubleClick()
        {
            LocalMenu_OnItemClicked("VoucherTransactions");
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

#if !SILVERLIGHT

        void SubstituteFilter(object sender, DevExpress.Data.SubstituteFilterEventArgs e)
        {
            if (string.IsNullOrEmpty(ribbonControl.SearchControl.SearchText))
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
#endif
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

        protected override async void LoadCacheInBackGround()
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
                case "ViewTransactions":
                    var item = dgChildCreditorTrans.SelectedItem as CreditorTransClientTotal;
                    if (item != null)
                        AddDockItem(TabControls.AccountsTransaction, item, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("CreditorTransactions"), item._Account));
                    break;
                case "VoucherTransactions":
                    var selItem = dgChildCreditorTrans.SelectedItem as CreditorTransClientTotal;
                    if (selItem != null)
                        AddDockItem(TabControls.AccountsTransaction, dgChildCreditorTrans.syncEntity, string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selItem._Voucher));
                    break;
                case "CreditorFilter":
                    if (creditorFilterDialog == null)
                    {
                        if (creditorFilterCleared)
                            creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), null, null, CreditorUserFields);
                        else
                            creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), null, null, CreditorUserFields);
                        creditorFilterDialog.Closing += creditorFilterDialog_Closing;
#if !SILVERLIGHT
                        creditorFilterDialog.Show();
                    }
                    else
                        creditorFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    creditorFilterDialog.Show();
#endif
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
            ibaseCurrent.LargeGlyph = Utilities.Utility.GetGlyph("Collapse_32x32.png");
        }

        private void SetExpandCurrent()
        {
            if (ibaseCurrent == null)
                return;

            ibaseCurrent.Caption = Uniconta.ClientTools.Localization.lookup("ExpandCurrent");
            ibaseCurrent.LargeGlyph = Utilities.Utility.GetGlyph("Expand_32x32.png");
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
#if !SILVERLIGHT
            e.Cancel = true;
            creditorFilterDialog.Hide();
#endif
        }

#if SILVERLIGHT
        private void ChildDgCreditorTrans_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            var detailsSelectedItem = e.NewItem as CreditorTransClientTotal;
            dgChildCreditorTrans.SelectedItem = detailsSelectedItem;
            dgChildCreditorTrans.syncEntity.Row = detailsSelectedItem;
        }
#endif
        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            LoadDCTrans();
        }

        bool manualExpanded = false;
        private void SetExpandAndCollapse(bool expandState)
        {
            if (ibase == null)
                return;
            if (dgCreditorTrans.ItemsSource == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !expandState)
            {
                manualExpanded = true;
                ExpandAndCollapseAll(false);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("CollapseAll");
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("Collapse_32x32.png");
            }
            else
            {
                if (expandState)
                {
                    ExpandAndCollapseAll(true);
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                    ibase.LargeGlyph = Utilities.Utility.GetGlyph("Expand_32x32.png");
                }
            }
        }

        void ExpandAndCollapseAll(bool IsCollapseAll)
        {
            for (int iRow = 0; iRow < dataRowCount; iRow++)
                if (!IsCollapseAll)
                    dgCreditorTrans.ExpandMasterRow(iRow);
                else
                    dgCreditorTrans.CollapseMasterRow(iRow);
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
            SetExpandAndCollapse(true);

            CreditorStatement.SetDateTime(txtDateFrm, txtDateTo);
            DateTime fromDate = CreditorStatement.DefaultFromDate, toDate = CreditorStatement.DefaultToDate;
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
            dgCreditorTrans.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;

            if (_master != null)
                SetExpandAndCollapse(false);
            else
                SetExpandAndCollapse(IsCollapsed);
        }

        void FillStatement(CreditorTransClientTotal[] listTrans, bool OnlyOpen, DateTime toDate)
        {
            var isAscending = cbxAscending.IsChecked.Value;
            var skipBlank = cbxSkipBlank.IsChecked.Value;

            var Pref = api.session.Preference;
            Pref.Debtor_isAscending = isAscending;
            Pref.Debtor_skipBlank = skipBlank;
            Pref.Debtor_OnlyOpen = OnlyOpen;

            var statementList = new List<CreditorStatementList>(Math.Min(20, dataRowCount));

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
            if (dataRowCount > 0)
            {
                dgCreditorTrans.ItemsSource = null;
                dgCreditorTrans.ItemsSource = statementList;
            }
        }

#if !SILVERLIGHT
        private void cbxPageBreak_Click(object sender, RoutedEventArgs e)
        {
            var tableView = (CustomTableView)dgCreditorTrans.View;
            if (cbxPageBreak.IsChecked == true)
                tableView.HasPageBreak = true;
            else
                tableView.HasPageBreak = false;
        }
#endif
        private void cmbFromAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            cmbToAccount.SelectedItem = cmbFromAccount.SelectedItem;
        }

        int dataRowCount;
        public override object GetPrintParameter()
        {
            if (!manualExpanded)
            {
                for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                    dgCreditorTrans.ExpandMasterRow(rowHandle);
            }
            return base.GetPrintParameter();
        }
    }
}
