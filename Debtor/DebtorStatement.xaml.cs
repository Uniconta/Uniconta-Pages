using UnicontaClient.Models;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Native;
using DevExpress.Xpf.Grid.Printing;
using DevExpress.XtraPrinting.DataNodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.DebtorCreditor;
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
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorStatementGrid : CorasauDataGridClient
    {
        public delegate void PrintClickDelegate();
        public event PrintClickDelegate OnPrintClick;
        public override Type TableType { get { return typeof(DebtorTransClientTotal); } }
        public bool PageBreak { get; internal set; }

        public DebtorStatementGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public DebtorStatementGrid()
        {
            CustomTableView tv = new CustomTableView();
            tv.AllowEditing = false;
            tv.ShowGroupPanel = false;
            SetTableViewStyle(tv);
            tv.ShowTotalSummary = true;
            tv.ShowGroupFooters = true;
            this.View = tv;
        }
        public bool IsStandardPrint;
        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            if (!IsStandardPrint && format == null)
                OnPrintClick();
            else
            {
                ((CustomTableView)this.View).HasPageBreak = PageBreak;
                SelectRowsToPrint();
                base.PrintGrid(reportName, printparam, format, page);
                SelectionMode = MultiSelectMode.Row;
            }
        }

        private void SelectRowsToPrint()
        {
            BeginSelection();
            UnselectAll();
            bool hasMarkedSelections = false;
            SelectionMode = MultiSelectMode.MultipleRow;
            for (int index = 0; index < VisibleRowCount; index++)
            {
                var rowhandle = GetRowHandleByVisibleIndex(index);
                var row = GetRow(rowhandle) as DebtorStatementList;
                if (row.Mark)
                {
                    SelectItem(rowhandle);
                    hasMarkedSelections = true;
                }
            }
            EndSelection();
            ((CustomTableView)this.View).PrintSelectedRowsOnly = hasMarkedSelections;
        }
    }
    public class DebtorStatementList : INotifyPropertyChanged
    {
        public override int GetHashCode() { return deb.GetHashCode(); }
        public override bool Equals(Object obj)
        {
            var ac = obj as DebtorStatementList;
            if (ac != null)
                return ac.deb.RowId == this.deb.RowId;
            else
            {
                var tran = obj as DCTrans;
                if (tran != null)
                    return tran._Account == this.deb._Account;
            }
            return false;
        }
        public DebtorStatementList(Debtor deb)
        {
            this.deb = deb ?? new Debtor();
        }
        public readonly Debtor deb;

        [Display(Name = "DAccount", ResourceType = typeof(GLTableText))]
        public string Account { get { return deb._Account; } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string Name { get { return deb._Name; } }

        [Display(Name = "LayoutGroup", ResourceType = typeof(DCAccountText))]
        public string LayoutGrp { get { return deb._LayoutGroup; } }

        [Display(Name = "Address1", ResourceType = typeof(DCAccountText))]
        public string Address1 { get { return deb._Address1; } }

        [Display(Name = "City", ResourceType = typeof(DCAccountText))]
        public string City { get { return deb._City; } }

        public DebtorTransClientTotal[] ChildRecords { get; set; }

        public double _SumAmount;
        public double SumAmount { get { return _SumAmount; } }

        public double _SumAmountCur;
        public double SumAmountCur { get { return _SumAmountCur; } }

        internal bool _Mark;
        public bool Mark { get { return _Mark; } set { if (_Mark == value) return; _Mark = value; RaisePropertyChanged("Mark"); } }

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public partial class DebtorStatement : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string FromAccount { get; set; }

        SQLCache accountCache;
        ItemBase ibase, ibaseCurrent;

        static public DateTime DefaultFromDate, DefaultToDate;
        static bool IsCollapsed = true;
        static bool pageBreak, showCurrency = false;
        static int printIntPreview = 1, transaction;
        bool OnlyOpen, OnlyDue = false;
        string includedJournals;

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

        public override string NameOfControl { get { return TabControls.DebtorStatement; } }

        Debtor _master;

        CWServerFilter debtorFilterDialog = null;
        bool debtorFilterCleared;
        TableField[] DebtorUserFields { get; set; }
        IEnumerable<PropValuePair> debtorFilterValues;

        public DebtorStatement(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        public DebtorStatement(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            _master = syncEntity.Row as Debtor;
            Init();
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            _master = args as Debtor;
            SetHeader();
            if (_master != null)
                InitQuery();
        }

        private void SetHeader()
        {
            string key = _master?._Account;
            if (key != null)
            {
                string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Statement"), key);
                SetHeader(header);
                FromAccount = key;
            }
        }
        public static void SetDefaultDateTime()
        {
            if (DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();
                DefaultToDate = now;
                var lastMonth = now.AddMonths(-1);
                DefaultFromDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
            }
        }
        void Init()
        {
            this.DataContext = this;
            InitializeComponent();
            SetDebtorFilterUserFields();
            cmbAccounts.api = api;
            SetRibbonControl(localMenu, dgDebtorTrans);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgDebtorTrans.OnPrintClick += DgDebtorTrans_OnPrintClick;

            SetDefaultDateTime();
            var Pref = api.session.Preference;
            cbxAscending.IsChecked = Pref.Debtor_isAscending;
            cbxSkipBlank.IsChecked = Pref.Debtor_skipBlank;
            // cbxOnlyOpen.IsChecked = Pref.Debtor_OnlyOpen;
            cbxPageBreak.IsChecked = pageBreak;
            dgDebtorTrans.PageBreak = pageBreak;
            chkShowCurrency.IsChecked = showCurrency;

            txtDateTo.DateTime = DebtorStatement.DefaultToDate;
            txtDateFrm.DateTime = DebtorStatement.DefaultFromDate;
            cmbPrintintPreview.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Internal"), Uniconta.ClientTools.Localization.lookup("External") };
            cmbPrintintPreview.SelectedIndex = printIntPreview;
            cmbTrasaction.ItemsSource = AppEnums.TransToShow.Values;
            cmbTrasaction.SelectedIndex = transaction;
            GetMenuItem();
            var Comp = api.CompanyEntity;
            accountCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor));
            if (Comp.RoundTo100)
                Amount.HasDecimals = colSumAmount.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;

            TransactionReport.SetDailyJournal(cmbJournals, api);
            dgDebtorTrans.RowDoubleClick += DgDebtorTrans_RowDoubleClick;
            dgDebtorTrans.SelectedItemChanged += DgDebtorTrans_SelectedItemChanged; ;
            dgDebtorTrans.MasterRowExpanded += DgDebtorTrans_MasterRowExpanded; ;
            dgDebtorTrans.MasterRowCollapsed += DgDebtorTrans_MasterRowCollapsed; ;
            dgDebtorTrans.ShowTotalSummary();
        }

        bool maintainState;
        private void DgDebtorTrans_MasterRowCollapsed(object sender, RowEventArgs e)
        {
            maintainState = true;
            SetExpandCurrent();
        }

        private void DgDebtorTrans_MasterRowExpanded(object sender, RowEventArgs e)
        {
            maintainState = true;
            SetCollapseCurrent();
        }

        private void DgDebtorTrans_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            if (dgDebtorTrans.SelectedItem == null || !dgDebtorTrans.IsVisible)
                return;

            SetExpandAndCollapseCurrent(false, false);
        }

        private void DgDebtorTrans_RowDoubleClick()
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
            BindingOperations.SetBinding(detailView, DataViewBase.SearchStringProperty, new System.Windows.Data.Binding("SearchText") { Source = ribbonControl.SearchControl });
        }

        TableView GetDetailView(int rowHandle)
        {
            var detail = dgDebtorTrans.GetDetail(rowHandle) as GridControl;
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
                var visibleColumns = childDgDebtorTrans.Columns.Where(c => c.Visible).Select(c => string.IsNullOrEmpty(c.FieldName) ? c.Name : c.FieldName);
                operands = new List<OperandProperty>();
                foreach (var col in visibleColumns)
                    operands.Add(new OperandProperty(col));
            }
            GroupOperator detailOperator = new GroupOperator(GroupOperatorType.Or);
            foreach (var op in operands)
                detailOperator.Operands.Add(new FunctionOperator(FunctionOperatorType.Contains, op, new OperandValue(searchString)));
            return new AggregateOperand("ChildRecords", Aggregate.Exists, detailOperator);
        }

        async private Task<IEnumerable<IPrintReport>> GeneratePrintReport(IEnumerable<DebtorStatementList> statementList, bool marked, bool hasCurrency, bool applyGridFilter)
        {
            var iprintReportList = new List<IPrintReport>();

            //Get Company related details

            var companyClient = api.CompanyEntity.CreateUserType<CompanyClient>();
            StreamingManager.Copy(api.CompanyEntity, companyClient);
            byte[] getLogoBytes = await UtilCommon.GetLogo(api);
            int rowHandle = -1;
            foreach (var db in statementList)
            {
                rowHandle = rowHandle + 1;

                if (db.ChildRecords.Length == 0 || (marked && !db.Mark))
                    continue;

                if (applyGridFilter)
                {
                    var visibelDetails = dgDebtorTrans.GetVisibleDetail(rowHandle);
                    if (visibelDetails == null || visibelDetails.VisibleItems?.Count == 0)
                        continue;
                }

                var lan = UtilDisplay.GetLanguage(db.deb, companyClient);

                //Setting the Localization for the debtor
                var debtLocalize = Uniconta.ClientTools.Localization.GetLocalization(lan);
                foreach (var rec in db.ChildRecords)
                {
                    rec.LocOb = debtLocalize;
                    if (rec._Primo)
                        rec._Text = debtLocalize.Lookup("Primo");
                }

                var debtorType = Uniconta.Reports.Utilities.ReportUtil.GetUserType(typeof(DebtorClient), api.CompanyEntity);
                var debt = Activator.CreateInstance(debtorType) as DebtorClient;
                StreamingManager.Copy(db.deb, debt);
                debt.Transactions = applyGridFilter ? dgDebtorTrans.GetVisibleDetail(rowHandle).VisibleItems.Cast<DebtorTransClientTotal>() : db.ChildRecords;

                if (lastMessage == null || messageLanguage != lan)
                {
                    messageLanguage = lan;
                    var msg = await UtilCommon.GetDebtorMessageClient(api, lan, DebtorEmailType.AccountStatement);
                    if (msg != null)
                        lastMessage = msg._Text;
                    else
                        lastMessage = string.Empty;
                }

                var statementPrint = new DebtorStatementReportClient(companyClient, debt, txtDateFrm.DateTime, txtDateTo.DateTime, "Statement", getLogoBytes, lastMessage);
                var standardReports = new[] { statementPrint };
                var standardPrint = new StandardPrintReport(api, standardReports, hasCurrency ? (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.StatementCurrency : (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Statement);
                await standardPrint.InitializePrint();

                if (standardPrint.Report != null)
                    iprintReportList.Add(standardPrint);
            }
            return iprintReportList;
        }

        async private void OpenOutlook()
        {
            try
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LaunchingWaitMsg");
                var debtorStatementList = ((IEnumerable<DebtorStatementList>)dgDebtorTrans.ItemsSource).Where(m => m.Mark).FirstOrDefault();
                if (debtorStatementList != null)
                {
                    var hasCurrency = debtorStatementList.ChildRecords.Where(p => p._AmountCur != 0.0d).Any();
                    var debtStatementReport = await GeneratePrintReport(new List<DebtorStatementList>(1) { debtorStatementList }, true, hasCurrency, false);
                    if (debtStatementReport != null && debtStatementReport.Count() == 1)
                        InvoicePostingPrintGenerator.OpenReportInOutlook(api, debtStatementReport.Single(), debtorStatementList.deb,
                            CompanyLayoutType.AccountStatement, DefaultFromDate, DefaultToDate, hasCurrency);
                }

            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
            finally { busyIndicator.IsBusy = false; }
        }

        public override Task InitQuery()
        {
            if (_master != null)
            {
                cmbAccounts.EditValue = _master._Account;
                return LoadDCTrans();
            }
            return null;
        }

        void SetDebtorFilterUserFields()
        {
            var debtorRow = new DebtorClient();
            debtorRow.SetMaster(api.CompanyEntity);
            DebtorUserFields = debtorRow.UserFieldDef();
        }

        private void DgDebtorTrans_OnPrintClick()
        {
            if (dgDebtorTrans.ItemsSource != null)
                PrintData();
        }

        public override void AssignMultipleGrid(List<CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgDebtorTrans);
            gridCtrls.Add(childDgDebtorTrans);
            isChildGridExist = true;
        }

        string lastMessage;
        Language messageLanguage;

        async private void PrintData()
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                var debtorStatementList = dgDebtorTrans.VisibleItems.Cast<DebtorStatementList>();
                var marked = debtorStatementList.Any(m => m.Mark == true);

                var iReports = await GeneratePrintReport(debtorStatementList.ToList(), marked, chkShowCurrency.IsChecked == true, true);

                if (iReports.Count() > 0)
                {
                    var dockJName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), Uniconta.ClientTools.Localization.lookup("Statement"));
                    AddDockItem(UnicontaTabs.StandardPrintReportPage, new object[] { iReports, Uniconta.ClientTools.Localization.lookup("Statement") }, dockJName);
                }
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("DebtorStatement.PrintData(), CompanyId={0}", api.CompanyId));
                UnicontaMessageBox.Show(ex);
            }
            finally { busyIndicator.IsBusy = false; }
        }



        Task<SQLCache> accountCacheTask;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (accountCache == null)
            {
                accountCacheTask = api.LoadCache(typeof(Uniconta.DataModel.Debtor));
                accountCache = await accountCacheTask.ConfigureAwait(false);
                accountCacheTask = null;
            }
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ExpandAndCollapse":
                    if (dgDebtorTrans.ItemsSource == null)
                        return;
                    IsCollapsed = dgDebtorTrans.IsMasterRowExpanded(0);
                    SetExpandAndCollapse(IsCollapsed);
                    break;
                case "ViewDownloadRow":
                    var selectedItem = childDgDebtorTrans.SelectedItem as DebtorTransClientTotal;
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(childDgDebtorTrans.syncEntity, api, busyIndicator);
                    break;
                case "SendAsEmail":
                    if (dgDebtorTrans.ItemsSource == null)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                        return;
                    }
                    SendMail();
                    break;
                case "OpenTran":
                    var dc = (childDgDebtorTrans.SelectedItem as DebtorTransClientTotal)?.Debtor ?? (dgDebtorTrans.SelectedItem as DebtorStatementList)?.deb;
                    if (dc != null)
                        AddDockItem(TabControls.DebtorOpenTransactions, dc, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OpenTrans"), dc._Account));
                    break;
                case "VoucherTransactions":
                    var selItem = childDgDebtorTrans.SelectedItem as DebtorTransClientTotal;
                    if (selItem != null)
                        AddDockItem(TabControls.AccountsTransaction, childDgDebtorTrans.syncEntity, Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selItem._Voucher));
                    break;
                case "DebtorFilter":

                    if (debtorFilterDialog == null)
                    {
                        if (debtorFilterCleared)
                            debtorFilterDialog = new CWServerFilter(api, typeof(DebtorClient), null, null, DebtorUserFields);
                        else
                            debtorFilterDialog = new CWServerFilter(api, typeof(DebtorClient), null, null, DebtorUserFields);
                        debtorFilterDialog.GridSource = dgDebtorTrans.ItemsSource as IList<UnicontaBaseEntity>;
                        debtorFilterDialog.Closing += debtorFilterDialog_Closing;
                        debtorFilterDialog.Show();
                    }
                    else
                    {
                        debtorFilterDialog.GridSource = dgDebtorTrans.ItemsSource as IList<UnicontaBaseEntity>;
                        debtorFilterDialog.Show(true);
                    }
                    break;
                case "ClearDebtorFilter":
                    debtorFilterDialog = null;
                    debtorFilterValues = null;
                    debtorFilterCleared = true;
                    break;
                case "ExpandCollapseCurrent":
                    if (dgDebtorTrans.SelectedItem != null)
                        SetExpandAndCollapseCurrent(true, false);
                    else if (childDgDebtorTrans.SelectedItem != null)
                        SetExpandAndCollapseCurrent(true, true);
                    break;
                case "SendAsOutlook":
                    if (dgDebtorTrans.ItemsSource == null)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                        return;
                    }
                    OpenOutlook();
                    break;
                case "Search":
                    childDgDebtorTrans.SelectedItem = null;
                    LoadDCTrans();
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
            int selectedMasterRowhandle;

            if (!isChild)
            {
                var selectedRowHandle = dgDebtorTrans.GetSelectedRowHandles();
                selectedMasterRowhandle = selectedRowHandle.FirstOrDefault();
                expandState = dgDebtorTrans.IsMasterRowExpanded(selectedRowHandle.FirstOrDefault());

            }
            else
                selectedMasterRowhandle = ((TableView)dgDebtorTrans.View.MasterRootRowsContainer.FocusedView).Grid.GetMasterRowHandle();

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
            ibaseCurrent.LargeGlyph = Utility.GetGlyph("Collapse_32x32");
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
                dgDebtorTrans.ExpandMasterRow(currentRowHandle);
            else
                dgDebtorTrans.CollapseMasterRow(currentRowHandle);
        }

        void debtorFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (debtorFilterDialog.DialogResult == true)
            {
                debtorFilterValues = debtorFilterDialog.PropValuePair;
            }
            e.Cancel = true;
            debtorFilterDialog.Hide();
        }

        private void SetExpandAndCollapse(bool expandState)
        {
            if (ibase == null)
                return;
            if (dgDebtorTrans.ItemsSource == null) return;
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

        void ExpandAndCollapseAll(bool IsCollapseAll)
        {

            for (int iRow = 0; iRow < dataRowCount; iRow++)
                if (!IsCollapseAll)
                    dgDebtorTrans.ExpandMasterRow(iRow);
                else
                    dgDebtorTrans.CollapseMasterRow(iRow);
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

        void SendMail()
        {
            var fromAccount = txtAccount.Text; ;
            var toAccount = string.Empty;
            var lstDebtorTransLst = (IEnumerable<DebtorStatementList>)dgDebtorTrans.ItemsSource;
            var VisibleItems = dgDebtorTrans.VisibleItems;

            if (VisibleItems.Count == 0)
                return;

            if (VisibleItems.Count == 1)
            {
                var cwSendMail = new CWSendInvoice();
                cwSendMail.DialogTableId = 2000000030;
                cwSendMail.Closed += delegate
                  {
                      if (cwSendMail.DialogResult == true)
                      {
                          var rec = (VisibleItems[0] as DebtorStatementList);
                          DoSendAsEmail(new[] { rec }, true, rec.Account, rec.Account, cwSendMail.Emails, cwSendMail.sendOnlyToThisEmail);
                      }
                  };
                cwSendMail.Show();
            }
            else
            {
                CWSendStatementEmail cw = new CWSendStatementEmail();
                var visibleDebtTranLst = VisibleItems.Cast<DebtorStatementList>();
                var visibleCount = VisibleItems.Count;
                cw.DialogTableId = 2000000031;
                cw.UpdateCount(visibleCount, visibleDebtTranLst.Where(p => p.Mark == true).Count());
                cw.Closed += delegate
                {
                    if (cw.DialogResult == true)
                    {
                        if (lstDebtorTransLst.Count() != visibleCount)
                        {
                            checkForMarked = false;
                            if (cw.SendAll)
                                foreach (var item in visibleDebtTranLst)
                                    item._Mark = true;

                            DoSendAsEmail(visibleDebtTranLst, false, fromAccount, toAccount, null, false);
                        }
                        else
                        {
                            checkForMarked = true;
                            DoSendAsEmail(lstDebtorTransLst, cw.SendAll, fromAccount, toAccount, null, false);
                        }
                    }
                };
                cw.Show();
            }
        }

        bool checkForMarked, running;
        async void DoSendAsEmail(IEnumerable<DebtorStatementList> lstDebtorTransLst, bool SendAll, string fromAccount, string toAccount, string emails, bool onlyThisEmail)
        {
            if (running)
                return;
            try
            {
                running = true;
                ribbonControl.DisableButtons(new string[] { "SendAsEmail" });
                var transApi = new ReportAPI(api);
                DebtorStatement.SetDateTime(txtDateFrm, txtDateTo);
                DateTime fromDate = DebtorStatement.DefaultFromDate, toDate = DebtorStatement.DefaultToDate;
                includedJournals = cmbJournals.Text;
                if (!SendAll)
                {
                    // lets check if we will send all anyway.
                    SendAll = checkForMarked;
                    foreach (var t in lstDebtorTransLst)
                    {
                        if (!t._Mark)
                        {
                            SendAll = false;
                            break;
                        }
                    }
                }

                if (SendAll)
                {
                    var skipBlank = cbxSkipBlank.IsChecked.GetValueOrDefault();
                    busyIndicator.IsBusy = true;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    var result = await transApi.DebtorAccountStatement(fromDate, toDate, fromAccount, toAccount, OnlyOpen, null, false, debtorFilterValues, emails, onlyThisEmail, OnlyDue, skipBlank, includedJournals);
                    busyIndicator.IsBusy = false;
                    if (result == ErrorCodes.Succes)
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), Uniconta.ClientTools.Localization.lookup("AccountStatement")),
                                                      Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
                else
                {
                    List<string> errorList = new List<string>();
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;
                    bool RunInBack = false;
                    foreach (var t in lstDebtorTransLst.ToList())
                    {
                        if (!t._Mark)
                            continue;
                        var result = await transApi.DebtorAccountStatement(fromDate, toDate, t.Account, t.Account, OnlyOpen, null, RunInBack, debtorFilterValues, emails, onlyThisEmail, false, false, includedJournals);
                        if (result != ErrorCodes.Succes)
                        {
                            string error = string.Format("{0}: {1}", t.Account, Uniconta.ClientTools.Localization.lookup(result.ToString()));
                            errorList.Add(error);
                            if (!RunInBack)
                                break;
                        }
                        RunInBack = true;
                        emails = null;
                    }
                    busyIndicator.IsBusy = false;

                    if (errorList.Count > 0)
                    {
                        CWErrorBox errorDialog = new CWErrorBox(errorList.ToArray(), true);
                        errorDialog.Show();
                    }
                    else if (RunInBack)
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), Uniconta.ClientTools.Localization.lookup("AccountStatement")),
                                                      Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
                    else
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
            }
            finally
            {
                running = false;
                ribbonControl.EnableButtons(new string[] { "SendAsEmail" });
            }
        }

        private void CheckEditor_Checked(object sender, RoutedEventArgs e)
        {
            DebtorStatementClientSetMark(true);
        }

        private void CheckEditor_Unchecked(object sender, RoutedEventArgs e)
        {
            DebtorStatementClientSetMark(false);
        }
        /* cannot use Grid GetVisibleRows as class is local */
        IEnumerable<DebtorStatementList> GetVisibleRows()
        {
            try
            {
                int n = dgDebtorTrans.VisibleRowCount;
                var lst = dgDebtorTrans.ItemsSource as IEnumerable<DebtorStatementList>;
                if (lst != null && lst.Count() == n)
                    return lst;
                var Arr = new DebtorStatementList[n];
                for (int i = 0; i < n; i++)
                {
                    var dataRow = dgDebtorTrans.GetRow(dgDebtorTrans.GetRowHandleByVisibleIndex(i));
                    Arr.SetValue(dataRow, i);
                }
                return Arr;
            }
            catch
            {
                return null;
            }
        }

        void DebtorStatementClientSetMark(bool value)
        {
            var lst = GetVisibleRows();
            if (lst != null)
            {
                foreach (var row in lst)
                    row.Mark = value;
            }
        }

        async Task LoadDCTrans()
        {
            DebtorStatement.SetDateTime(txtDateFrm, txtDateTo);
            DateTime fromDate = DebtorStatement.DefaultFromDate, toDate = DebtorStatement.DefaultToDate;

            pageBreak = cbxPageBreak.IsChecked.GetValueOrDefault();
            showCurrency = chkShowCurrency.IsChecked.GetValueOrDefault();
            printIntPreview = cmbPrintintPreview.SelectedIndex;
            transaction = cmbTrasaction.SelectedIndex;
            includedJournals = cmbJournals.Text;
            string fromAccount = txtAccount.Text;
            string toAccount = null;
            busyIndicator.IsBusy = true;
            var transApi = new ReportAPI(api);
            var listTrans = (DebtorTransClientTotal[])await transApi.GetTransWithPrimo(new DebtorTransClientTotal(), fromDate, toDate, fromAccount, toAccount, OnlyOpen, null, debtorFilterValues, OnlyDue, includedJournals);
            if (listTrans != null)
            {
                var t = accountCacheTask;
                if (t != null)
                    accountCache = await t;

                FillStatement(listTrans, OnlyOpen, toDate);
            }
            else if (transApi.LastError != 0)
            {
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(transApi.LastError);
            }

            dgDebtorTrans.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
            if (statementList?.Count == 1)
                IsCollapsed = false;
            ExpandAndCollapseAll(IsCollapsed);
            maintainState = false;
        }
        List<DebtorStatementList> statementList = null;
        void FillStatement(DebtorTransClientTotal[] listTrans, bool OnlyOpen, DateTime toDate)
        {
            var isAscending = cbxAscending.IsChecked.GetValueOrDefault();
            var skipBlank = cbxSkipBlank.IsChecked.GetValueOrDefault();

            var Pref = api.session.Preference;
            Pref.Debtor_isAscending = isAscending;
            Pref.Debtor_skipBlank = skipBlank;
            Pref.Debtor_OnlyOpen = OnlyOpen;

            statementList = new List<DebtorStatementList>(Math.Min(20, dataRowCount));

            string currentItem = string.Empty;
            DebtorStatementList masterDbStatement = null;
            var tlst = new List<DebtorTransClientTotal>(100);
            double SumAmount = 0d, SumAmountCur = 0d;

            for (int n = 0; (n < listTrans.Length); n++)
            {
                var trans = listTrans[n];
                if (trans._Account != currentItem)
                {
                    currentItem = trans._Account;

                    if (masterDbStatement != null)
                    {
                        if (!skipBlank || SumAmount != 0 || tlst.Count > 1)
                        {
                            statementList.Add(masterDbStatement);
                            masterDbStatement.ChildRecords = tlst.ToArray();
                            if (!isAscending)
                                Array.Reverse(masterDbStatement.ChildRecords);
                        }
                    }

                    masterDbStatement = new DebtorStatementList((Debtor)accountCache.Get(currentItem));
                    tlst.Clear();
                    SumAmount = SumAmountCur = 0d;
                    if (trans._Text == null && trans._Primo)
                        trans._Text = Uniconta.ClientTools.Localization.lookup("Primo");
                }
                SumAmount += OnlyOpen ? trans._AmountOpen : trans._Amount;
                trans._SumAmount = SumAmount;
                masterDbStatement._SumAmount = SumAmount;

                SumAmountCur += OnlyOpen ? trans._AmountOpenCur : trans._AmountCur;
                trans._SumAmountCur = SumAmountCur;
                masterDbStatement._SumAmountCur = SumAmountCur;

                if (trans._DueDate <= toDate)
                {
                    trans._OverDue = trans._AmountOpen;
                    trans._OverDueCur = trans._AmountOpenCur;
                }

                tlst.Add(trans);
            }

            if (masterDbStatement != null)
            {
                if (!skipBlank || SumAmount != 0 || tlst.Count > 1)
                {
                    statementList.Add(masterDbStatement);
                    masterDbStatement.ChildRecords = tlst.ToArray();
                    if (!isAscending)
                        Array.Reverse(masterDbStatement.ChildRecords);
                }
            }

            dataRowCount = statementList.Count;
            dgDebtorTrans.ItemsSource = null;
            if (dataRowCount > 0)
            {
                if (statementList.Count == 1)
                    statementList[0].Mark = true;
                dgDebtorTrans.ItemsSource = statementList;
                childDgDebtorTrans.RefreshData();
            }
        }

        private void cmbPrintintPreview_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var cmbItems = sender as DimComboBoxEditor;
            dgDebtorTrans.IsStandardPrint = (Convert.ToString(cmbItems.SelectedItem) == Uniconta.ClientTools.Localization.lookup("Internal"));
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

        private void cbxPageBreak_Click(object sender, RoutedEventArgs e)
        {
            dgDebtorTrans.PageBreak = (cbxPageBreak.IsChecked == true);
        }

        private void cmbAccounts_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            txtAccount.Text += $"{cmbAccounts.EditValue};";
        }

        int dataRowCount;
        public override object GetPrintParameter()
        {
            if (!maintainState)
            {
                for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                    dgDebtorTrans.ExpandMasterRow(rowHandle);
            }
            return base.GetPrintParameter();
        }
    }
    public class CustomTableView : TableView, ITableView
    {
        public CustomTableView() : base()
        {
        }
        public CustomTableView(MasterNodeContainer masterRootNode, MasterRowsContainer masterRootDataItem, DataControlDetailDescriptor detailDescriptor) : base(masterRootNode, masterRootDataItem, detailDescriptor)
        {
        }
        public bool HasPageBreak { get; set; }
        protected override PrintingDataTreeBuilderBase CreatePrintingDataTreeBuilder(double totalHeaderWidth, ItemsGenerationStrategyBase itemsGenerationStrategy, MasterDetailPrintInfo masterDetailPrintInfo, BandsLayoutBase bandsLayout)
        {
            return new CustomGridPrintingDataTreeBuilder(HasPageBreak, this, totalHeaderWidth, itemsGenerationStrategy, bandsLayout, masterDetailPrintInfo);
        }
    }
    public class CustomGridPrintingDataTreeBuilder : GridPrintingDataTreeBuilder
    {
        public bool HasPageBreak { get; set; }
        public CustomGridPrintingDataTreeBuilder(bool HasPageBreak, TableView view, double totalHeaderWidth, ItemsGenerationStrategyBase itemsGenerationStrategy, BandsLayoutBase bandsLayout, MasterDetailPrintInfo masterDetailPrintInfo = null) :
           base(view, totalHeaderWidth, itemsGenerationStrategy, bandsLayout, masterDetailPrintInfo)
        {
            this.HasPageBreak = HasPageBreak;
        }

        public override IDataNode CreateMasterDetailPrintingNode(NodeContainer container, RowNode rowNode, IDataNode node, int index, System.Windows.Size pageSize)
        {
            return new CustomGridMasterDetailPrintingNode(HasPageBreak, 1, container, rowNode, this, node, index, pageSize);
        }
    }

    public class CustomGridMasterDetailPrintingNode : GridMasterDetailPrintingNode, IDataNode
    {
        int PrintGroupLevel;
        public bool HasPageBreak { get; set; }
        bool IDataNode.PageBreakBefore
        {
            get
            {
                if (base.nodeContainer.GroupLevel != PrintGroupLevel || !HasPageBreak) return false;
                return base.fIndex > 0;
            }
        }
        public CustomGridMasterDetailPrintingNode(bool HasPageBreak, int groupLevel, NodeContainer parentContainer, RowNode rowNode, GridPrintingDataTreeBuilder treeBuilder, IDataNode parent, int index, System.Windows.Size pageSize) : base(parentContainer, rowNode, treeBuilder, parent, index, pageSize) { this.HasPageBreak = HasPageBreak; }
    }
}
