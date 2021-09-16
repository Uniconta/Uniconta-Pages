using UnicontaClient.Models;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Docking;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Native;
using DevExpress.Xpf.Grid.Printing;
using DevExpress.Xpf.Printing;
using DevExpress.XtraPrinting;
using DevExpress.XtraPrinting.DataNodes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.API.GeneralLedger;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
#if !SILVERLIGHT
using UnicontaClient.Pages;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AccountStatementGrid2 : CorasauDataGridClient
    {
        public delegate void PrintClickDelegate();
        public event PrintClickDelegate OnPrintClick;

        public override Type TableType { get { return typeof(GLTransClientTotal); } }

        public bool PageBreak { get; internal set; }

        public AccountStatementGrid2(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public AccountStatementGrid2()
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
            base.PrintGrid(reportName, printparam, format, page, false);
        }
    }
  
    public class AccountStatementList2 : INotifyPropertyChanged
    {
        public AccountStatementList2(GLAccount Acc)
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

        public List<GLTransClientTotal> ChildRecords { get; set; }

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

    public partial class AccountStatement2 : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string ToAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLDailyJournal))]
        public string Journal { get; set; }

        SQLCache accountCache;
        ItemBase ibase;
        List<AccountStatementList2> statementList;

        static public DateTime DefaultFromDate, DefaultToDate;

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

        GLAccount _master;

        public AccountStatement2(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        public AccountStatement2(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            _master = syncEntity.Row as GLAccount;
            if (_master != null)
            { FromAccount = _master._Account; ToAccount = _master._Account; }
            Init();
            SetHeader();
        }
        protected override void OnLayoutLoaded()
        {
            setDim();
        }
        void setDim()
        {
            var c = api.CompanyEntity;
            cldim1.Header = lblDim1.Text = c._Dim1;
            cldim2.Header = lblDim2.Text = c._Dim2;
            cldim3.Header = lblDim3.Text = c._Dim3;
            cldim4.Header = lblDim4.Text = c._Dim4;
            cldim5.Header = lblDim5.Text = c._Dim5;
        }
        private async Task SetNoOfDimensions()
        {
            var api = this.api;
            int noofDimensions = api.CompanyEntity.NumberOfDimensions;
            if (noofDimensions < 5)
            {
#if SILVERLIGHT
                cbdim5.Visibility = cldim5.Visibility = Visibility.Collapsed;
#endif
                lblDim5.Visibility = Visibility.Collapsed;
                cldim5.Visible = false;
                cbdim5.Visibility = Visibility.Collapsed;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType5), cbdim5, api);

            if (noofDimensions < 4)
            {
#if SILVERLIGHT
                cbdim4.Visibility = cldim4.Visibility = Visibility.Collapsed;
#endif
                lblDim4.Visibility = Visibility.Collapsed;
                cldim4.Visible = false;
                cbdim4.Visibility = Visibility.Collapsed;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType4), cbdim4, api);

            if (noofDimensions < 3)
            {
#if SILVERLIGHT
                cbdim3.Visibility = cldim3.Visibility = Visibility.Collapsed;
#endif
                lblDim3.Visibility = Visibility.Collapsed;
                cldim3.Visible = false;
                cbdim3.Visibility = Visibility.Collapsed;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType3), cbdim3, api);

            if (noofDimensions < 2)
            {
#if SILVERLIGHT
                cbdim2.Visibility = cldim2.Visibility = Visibility.Collapsed;
#endif
                lblDim2.Visibility = Visibility.Collapsed;
                cldim2.Visible = false;
                cbdim2.Visibility = Visibility.Collapsed;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType2), cbdim2, api);

            if (noofDimensions < 1)
            {
#if SILVERLIGHT
                cbdim1.Visibility = cldim1.Visibility = Visibility.Collapsed;
#endif
                lblDim1.Visibility = Visibility.Collapsed;
                cldim1.Visible = false;
                cbdim1.Visibility = Visibility.Collapsed;
            }
            else
                await TransactionReport.SetDimValues(typeof(GLDimType1), cbdim1, api);
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
            { FromAccount = _master._Account; ToAccount = _master._Account; }
            SetHeader();
            if (_master != null)
            {
                cmbFromAccount.EditValue = _master._Account;
                cmbToAccount.EditValue = _master._Account;
                LoadGLTrans();
            }
        }

        void Init()
        {
            this.DataContext = this;
            InitializeComponent();
            cmbFromAccount.api = cmbToAccount.api = api;
            SetRibbonControl(localMenu, dgGLTrans);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            statementList = new List<AccountStatementList2>();

            if (AccountStatement2.DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();
                AccountStatement2.DefaultToDate = now;
                AccountStatement2.DefaultFromDate = now.AddDays(1 - now.Day).AddMonths(-2);
            }
            var Pref = api.session.Preference;
            cbxAscending.IsChecked = Pref.Debtor_isAscending;
            cbxSkipBlank.IsChecked = Pref.Debtor_skipBlank;

            txtDateTo.DateTime = AccountStatement2.DefaultToDate;
            txtDateFrm.DateTime = AccountStatement2.DefaultFromDate;
            GetMenuItem();
            InitialLoad();
            //StartLoadCache();

            var Comp = api.CompanyEntity;
            if (Comp.RoundTo100)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = false;

            if (_master != null)
            {
                cmbFromAccount.EditValue = _master._Account;
                cmbToAccount.EditValue = _master._Account;
                LoadGLTrans();
            }

            dgGLTrans.ShowTotalSummary();
#if SILVERLIGHT
            childDgGLTrans.CurrentItemChanged += ChildDgDebtorTrans_CurrentItemChanged;
#endif
        }

        async void InitialLoad()
        {
            await TransactionReport.SetDailyJournal(cmbJournal, api);
            var t = SetNoOfDimensions();
            StartLoadCache(t);
        }

        public override void AssignMultipleGrid(List<CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgGLTrans);
            gridCtrls.Add(childDgGLTrans);
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            accountCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLAccount), api).ConfigureAwait(false);
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor) });
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
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
                        DebtorTransactions.ShowVoucher(dgGLTrans.syncEntity, api, busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, selectedItem, vheader);
                    break;
                case "ExpandAndCollapse":
                    if (dgGLTrans.ItemsSource != null)
                        SetExpandAndCollapse(dgGLTrans.IsMasterRowExpanded(0));
                    break;
                case "Search":
                    LoadGLTrans();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

#if SILVERLIGHT
        private void ChildDgDebtorTrans_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            var detailsSelectedItem = e.NewItem as GLTransClientTotal;
            childDgGLTrans.SelectedItem = detailsSelectedItem;
            childDgGLTrans.syncEntity.Row = detailsSelectedItem;
        }
#endif

        private void SetExpandAndCollapse(bool expandState)
        {
            if (ibase == null)
                return;
            if (dgGLTrans.ItemsSource == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !expandState)
            {
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

        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            LoadGLTrans();
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ExpandAndCollapse");
        }

        void ExpandAndCollapseAll(bool IsCollapseAll)
        {
            int dataRowCount = statementList.Count;

            for (int iRow = 0; iRow < dataRowCount; iRow++)
                if (!IsCollapseAll)
                    dgGLTrans.ExpandMasterRow(iRow);
                else
                    dgGLTrans.CollapseMasterRow(iRow);
        }

        async void LoadGLTrans()
        {
            SetExpandAndCollapse(true);
            statementList.Clear();
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
            AccountStatement2.SetDateTime(txtDateFrm, txtDateTo);
            DateTime fromDate = AccountStatement2.DefaultFromDate, toDate = AccountStatement2.DefaultToDate;

            var isAscending = cbxAscending.IsChecked.Value;
            var skipBlank = cbxSkipBlank.IsChecked.Value;

            var Pref = api.session.Preference;
            Pref.TransactionReport_isAscending = isAscending;
            Pref.TransactionReport_skipBlank = skipBlank;

            string fromAccount = null, toAccount = null;
            var accountObj = cmbFromAccount.EditValue;
            if (accountObj != null)
                fromAccount = accountObj.ToString();

            accountObj = cmbToAccount.EditValue;
            if (accountObj != null)
                toAccount = accountObj.ToString();

            string journal = cmbJournal.Text;
            busyIndicator.IsBusy = true;
            var transApi = new Uniconta.API.GeneralLedger.ReportAPI(api);
            var dimensionParams = BalanceReport.SetDimensionParameters(dim1, dim2, dim3, dim4, dim5, true, true, true, true, true);
            var listTrans = (GLTransClientTotal[])await transApi.GetTransactions(new GLTransClientTotal(), journal, fromAccount, toAccount, fromDate, toDate, dimensionParams, ReportAPI.SimplePrimo);
            if (listTrans != null)
            {
                string currentItem = null;
                AccountStatementList2 masterDbStatement = null;
                List<GLTransClientTotal> dbTransClientChildList = null;
                long SumAmount = 0, SumAmountCur = 0;
                byte currentCur = 0;

                foreach (var trans in listTrans)
                {
                    if (trans._Account != currentItem)
                    {
                        currentItem = trans._Account;

                        if (masterDbStatement != null)
                        {
                            if (!skipBlank || SumAmount != 0 || SumAmountCur != 0 || dbTransClientChildList.Count > 1)
                            {
                                masterDbStatement._SumAmount = SumAmount / 100d;
                                masterDbStatement._SumAmountCur = SumAmountCur / 100d;
                                statementList.Add(masterDbStatement);
                            }
                        }

                        masterDbStatement = new AccountStatementList2((GLAccount)accountCache.Get(currentItem));
                        dbTransClientChildList = new List<GLTransClientTotal>();
                        masterDbStatement.ChildRecords = dbTransClientChildList;
                        SumAmount = SumAmountCur = 0;
                        currentCur = (byte)masterDbStatement.Acc._Currency;
                    }

                    SumAmount += trans._AmountCent;
                    trans._Total = SumAmount;
                    masterDbStatement._SumAmount = SumAmount;

                    if (trans._AmountCurCent != 0 && trans._Currency == currentCur)
                    {
                        SumAmountCur += trans._AmountCurCent;
                        trans._TotalCur = SumAmountCur;
                    }

                    if (isAscending)
                        dbTransClientChildList.Add(trans);
                    else
                        dbTransClientChildList.Insert(0, trans);
                }

                if (masterDbStatement != null)
                {
                    if (!skipBlank || SumAmount != 0 || SumAmountCur != 0 || dbTransClientChildList.Count > 1)
                    {
                        masterDbStatement._SumAmount = SumAmount / 100d;
                        masterDbStatement._SumAmountCur = SumAmountCur / 100d;
                        statementList.Add(masterDbStatement);
                    }
                }

                if (statementList.Count > 0)
                {
                    dgGLTrans.ItemsSource = null;
                    dgGLTrans.ItemsSource = statementList;
                }
                dgGLTrans.Visibility = Visibility.Visible;
            }
            else if (transApi.LastError != 0)
            {
                Uniconta.ClientTools.Util.UtilDisplay.ShowErrorCode(transApi.LastError);
            }
            busyIndicator.IsBusy = false;
            if (_master != null)
                SetExpandAndCollapse(false);
        }


#if !SILVERLIGHT
        private void cbxPageBreak_Click(object sender, RoutedEventArgs e)
        {
            if (cbxPageBreak.IsChecked == true)
                dgGLTrans.PageBreak = true;
            else
                dgGLTrans.PageBreak = false;
        }
#endif
        public override object GetPrintParameter()
        {
            int dataRowCount = statementList.Count;
            for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                dgGLTrans.ExpandMasterRow(rowHandle);

            return base.GetPrintParameter();
        }
    }
}
