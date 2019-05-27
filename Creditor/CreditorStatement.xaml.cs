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

        public List<CreditorTransClientTotal> ChildRecords { get; set; }

        public double _SumAmount;
        public double SumAmount { get { return _SumAmount; } }

        public double _SumAccountCur;
        public double SumAccountCur { get { return _SumAccountCur; } }
    }

    public class CreditorTransClientTotal : CreditorTransClient
    {
        public double _SumAmount;
        public double _SumAmountCur;

        [Display(Name = "Total", ResourceType = typeof(GLDailyJournalText))]
        public double SumAmount { get { return _SumAmount; } }

        [Display(Name = "TotalCur", ResourceType = typeof(GLDailyJournalText))]
        public double SumAmountCur { get { return _SumAmountCur; } }

        [Display(Name = "DueDate", ResourceType = typeof(DCTransText))]
        public DateTime DueDate { get { return _DueDate; } }

        [Display(Name = "Remaining", ResourceType = typeof(DCTransText))]
        [NoSQL]
        public double AmountOpen { get { return _AmountOpen; } }

        [Display(Name = "RemainingCur", ResourceType = typeof(DCTransText))]
        [NoSQL]
        public double AmountOpenCur { get { return _AmountOpenCur; } }
    }

    public partial class CreditorStatement : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string ToAccount { get; set; }

        SQLCache accountCache;
        ItemBase ibase;
        List<CreditorStatementList> statementList;

        static public DateTime DefaultFromDate, DefaultToDate;

        CWServerFilter creditorFilterDialog = null;
        bool creditorFilterCleared;
        TableField[] CreditorUserFields { get; set; }
        IEnumerable<PropValuePair> creditorFilterValues;

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
            statementList = new List<CreditorStatementList>();

            if (CreditorStatement.DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();
                CreditorStatement.DefaultToDate = now;
                CreditorStatement.DefaultFromDate = now.AddDays(1 - now.Day).AddMonths(-2);
            }

            var Pref = api.session.Preference;
            cbxAscending.IsChecked = Pref.Debtor_isAscending;
            cbxSkipBlank.IsChecked = Pref.Debtor_skipBlank;
            cbxOnlyOpen.IsChecked = Pref.Debtor_OnlyOpen;

            txtDateTo.DateTime = CreditorStatement.DefaultToDate;
            txtDateFrm.DateTime = CreditorStatement.DefaultFromDate;

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

#if SILVERLIGHT
            dgChildCreditorTrans.CurrentItemChanged += ChildDgCreditorTrans_CurrentItemChanged;
#endif
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

        protected override async void LoadCacheInBackGround()
        {
            if (accountCache == null)
                accountCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ExpandAndCollapse");
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {

            switch (ActionType)
            {
                case "ExpandAndCollapse":
                    if (dgCreditorTrans.ItemsSource == null)
                        return;
                    SetExpandAndCollapse(dgCreditorTrans.IsMasterRowExpanded(0));
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
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
                ibase.LargeGlyph = Utilities.Utility.GetGlyph(";component/Assets/img/Collapse_32x32.png");
            }
            else
            {
                if (expandState)
                {
                    ExpandAndCollapseAll(true);
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                    ibase.LargeGlyph = Utilities.Utility.GetGlyph(";component/Assets/img/Expand_32x32.png");
                }
            }
        }

        void ExpandAndCollapseAll(bool IsCollapseAll)
        {
            int dataRowCount = statementList.Count;

            for (int iRow = 0; iRow < dataRowCount; iRow++)
                if (!IsCollapseAll)
                    dgCreditorTrans.ExpandMasterRow(iRow);
                else
                    dgCreditorTrans.CollapseMasterRow(iRow);
        }

        async void LoadDCTrans()
        {
            SetExpandAndCollapse(true);
            statementList.Clear();

            CreditorStatement.SetDateTime(txtDateFrm, txtDateTo);
            DateTime fromDate = CreditorStatement.DefaultFromDate, toDate = CreditorStatement.DefaultToDate;
            var isAscending = cbxAscending.IsChecked.Value;
            var skipBlank = cbxSkipBlank.IsChecked.Value;
            var OnlyOpen = cbxOnlyOpen.IsChecked.Value;

            var Pref = api.session.Preference;
            Pref.Debtor_isAscending = isAscending;
            Pref.Debtor_skipBlank = skipBlank;
            Pref.Debtor_OnlyOpen = OnlyOpen;

            var fromAccount = Convert.ToString(cmbFromAccount.EditValue);
            var toAccount = Convert.ToString(cmbToAccount.EditValue);

            busyIndicator.IsBusy = true;
            var transApi = new ReportAPI(api);
            var listTrans = (CreditorTransClientTotal[])await transApi.GetTransWithPrimo(new CreditorTransClientTotal(), fromDate, toDate, fromAccount, toAccount, OnlyOpen, PropWhere: creditorFilterValues);
            if (listTrans != null)
            {
                if (accountCache == null)
                    accountCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor));

                string currentItem = string.Empty;
                CreditorStatementList masterCredStatement = null;
                List<CreditorTransClientTotal> credTransClientChildList = null;
                double SumAmount = 0d, SumAmountCur = 0d;

                foreach (var trans in listTrans)
                {
                    if (trans._Account != currentItem)
                    {
                        currentItem = trans._Account;

                        if (masterCredStatement != null)
                        {
                            if (!skipBlank || SumAmount != 0 || credTransClientChildList.Count > 1)
                                statementList.Add(masterCredStatement);
                            if (!isAscending)
                                credTransClientChildList.Reverse();
                        }

                        masterCredStatement = new CreditorStatementList((Uniconta.DataModel.Creditor)accountCache.Get(currentItem));
                        credTransClientChildList = new List<CreditorTransClientTotal>();
                        masterCredStatement.ChildRecords = credTransClientChildList;
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

                    credTransClientChildList.Add(trans);
                }

                if (masterCredStatement != null)
                {
                    if (!skipBlank || SumAmount != 0 || credTransClientChildList.Count > 1)
                        statementList.Add(masterCredStatement);
                    if (!isAscending)
                        credTransClientChildList.Reverse();
                }

                if (statementList.Any())
                {
                    dgCreditorTrans.ItemsSource = null;
                    dgCreditorTrans.ItemsSource = statementList;
                }
                dgCreditorTrans.Visibility = Visibility.Visible;
                busyIndicator.IsBusy = false;
            }

            if (_master != null)
                SetExpandAndCollapse(false);
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
        public override object GetPrintParameter()
        {
            if (!manualExpanded)
            {
                int dataRowCount = statementList.Count;
                for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                    dgCreditorTrans.ExpandMasterRow(rowHandle);
            }
            return base.GetPrintParameter();
        }
    }
}
