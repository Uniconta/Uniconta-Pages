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
using System.Collections;
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
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;
#if !SILVERLIGHT
using UnicontaClient.Pages;
#endif

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
#if !SILVERLIGHT
                ((CustomTableView)this.View).HasPageBreak = PageBreak;
#endif
                base.PrintGrid(reportName, printparam, format, page);
            }
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
        public string AccountNumber { get { return deb._Account; } }

        [Display(Name = "Name", ResourceType = typeof(GLTableText))]
        public string Name { get { return deb._Name; } }

        [Display(Name = "LayoutGroup", ResourceType = typeof(DCAccountText))]
        public string LayoutGrp { get { return deb._LayoutGroup; } }

        public List<DebtorTransClientTotal> ChildRecords { get; set; }

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

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string ToAccount { get; set; }

        SQLCache accountCache;
        ItemBase ibase;
        List<DebtorStatementList> statementList;

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
                FromAccount = ToAccount = key;
            }
        }

        void Init()
        {
            this.DataContext = this;
            InitializeComponent();
            SetDebtorFilterUserFields();
            cmbFromAccount.api = cmbToAccount.api = api;
            SetRibbonControl(localMenu, dgDebtorTrans);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgDebtorTrans.OnPrintClick += DgDebtorTrans_OnPrintClick;
            statementList = new List<DebtorStatementList>();

            if (DebtorStatement.DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();
                DebtorStatement.DefaultToDate = now;
                DebtorStatement.DefaultFromDate = now.AddDays(1 - now.Day).AddMonths(-2);
            }
            var Pref = api.session.Preference;
            cbxAscending.IsChecked = Pref.Debtor_isAscending;
            cbxSkipBlank.IsChecked = Pref.Debtor_skipBlank;
            cbxOnlyOpen.IsChecked = Pref.Debtor_OnlyOpen;

            txtDateTo.DateTime = DebtorStatement.DefaultToDate;
            txtDateFrm.DateTime = DebtorStatement.DefaultFromDate;
            cmbPrintintPreview.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Internal"), Uniconta.ClientTools.Localization.lookup("External") };
            cmbPrintintPreview.SelectedIndex = 0;
            GetMenuItem();
            var Comp = api.CompanyEntity;
            accountCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor));
            if (Comp.RoundTo100)
                Amount.HasDecimals = colSumAmount.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;

            dgDebtorTrans.ShowTotalSummary();
#if SILVERLIGHT
            childDgDebtorTrans.CurrentItemChanged += ChildDgDebtorTrans_CurrentItemChanged;
#endif
        }

        public override Task InitQuery()
        {
            if (_master != null)
            {
                cmbFromAccount.EditValue = _master._Account;
                cmbToAccount.EditValue = _master._Account;
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

        async private void PrintData()
        {
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");

            try
            {
                //Get Company related details
                var companyClient = new CompanyClient();
                StreamingManager.Copy(api.CompanyEntity, companyClient);
                byte[] getLogoBytes = await UtilDisplay.GetLogo(api);

#if SILVERLIGHT
                if (dgDebtorTrans.SelectedItem != null)
                {
                    var selectedItem = dgDebtorTrans.SelectedItem as DebtorStatementList;
                    if (selectedItem.ChildRecords.Count == 0) return;

                    var debt = new DebtorClient();
                    StreamingManager.Copy(selectedItem.deb, debt);
                    if (debt != null)
                    {
                        debt.Transactions = selectedItem.ChildRecords;

                        //Setting the Localization for the debtor
                        var debtLocalize = Uniconta.ClientTools.Localization.GetLocalization(UtilDisplay.GetLanguage(debt, api.CompanyEntity));
                        foreach (var rec in debt.Transactions)
                            rec.LocOb = debtLocalize;

                        var debtorMessageClient = await Utility.GetDebtorMessageClient(api, debt._Language, DebtorEmailType.AccountStatement);
                        string messageText = debtorMessageClient?._Text;

                        DebtorStatementCustomPrint dbStatementCustomPrint = new DebtorStatementCustomPrint(api, selectedItem, companyClient, debt,
                                    txtDateFrm.DateTime, txtDateTo.DateTime, getLogoBytes, messageText);

                        object[] obj = new object[1];
                        obj[0] = dbStatementCustomPrint as Controls.CustomPrintTemplateData;
                        if (chkShowCurrency.IsChecked == true)
                            AddDockItem(TabControls.DebtorStatementCurrencyCustomPrintPage, obj, true, string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("PrintPreview"), selectedItem.AccountNumber, selectedItem.Name));
                        else
                            AddDockItem(TabControls.DebtorStatementCustomPrintPage, obj, true, string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("PrintPreview"), selectedItem.AccountNumber, selectedItem.Name));
                    }
                }
#else
                IEnumerable<DebtorStatementList> debtorStatementList = (IEnumerable<DebtorStatementList>)dgDebtorTrans.ItemsSource;
                var layoutSelectedDebtorStatementList = new Dictionary<string, List<DebtorStatementList>>();

                var Comp = api.CompanyEntity;
                var layoutgrpCache = Comp.GetCache(typeof(Uniconta.DataModel.DebtorLayoutGroup));
                if (layoutgrpCache == null)
                    layoutgrpCache = await Comp.LoadCache(typeof(Uniconta.DataModel.DebtorLayoutGroup), api);

                var xtraReports = new List<DevExpress.XtraReports.UI.XtraReport>();
                var iReports = new List<IPrintReport>();
                var marked = debtorStatementList.Any(m => m.Mark == true);

                foreach (var db in debtorStatementList)
                {
                    if (db.ChildRecords.Count == 0 || (marked && !db.Mark))
                        continue;

                    var statementPrint = await GenerateStandardStatementReport(companyClient, txtDateFrm.DateTime, txtDateTo.DateTime, db, getLogoBytes);
                    if (statementPrint == null) continue;

                    var standardReports = new IDebtorStandardReport[1] { statementPrint };

                    IPrintReport standardPrint;
                    if (chkShowCurrency.IsChecked == true)
                        standardPrint = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.StatementCurrency);
                    else
                        standardPrint = new StandardPrintReport(api, standardReports, (byte)Uniconta.ClientTools.Controls.Reporting.StandardReports.Statement);
                    await standardPrint.InitializePrint();

                    if (standardPrint.Report != null)
                        iReports.Add(standardPrint);
                }

                if (iReports.Count > 0)
                {
                    var dockJName = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Preview"), Uniconta.ClientTools.Localization.lookup("Statement"));
                    AddDockItem(UnicontaTabs.StandardPrintReportPage, new object[] { iReports, Uniconta.ClientTools.Localization.lookup("Statement") }, dockJName);
                }
#endif
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                api.ReportException(ex, string.Format("DebtorStatement.PrintData(), CompanyId={0}", api.CompanyId));
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
            }
            finally { busyIndicator.IsBusy = false; }
        }

#if !SILVERLIGHT
        async private Task<IDebtorStandardReport> GenerateStandardStatementReport(CompanyClient companyClient, DateTime defaultFromDate, DateTime defaultToDate, DebtorStatementList selectedItem, byte[] logo)
        {
            var debtorType = Uniconta.Reports.Utilities.ReportUtil.GetUserType(typeof(DebtorClient), api.CompanyEntity);
            var debt = Activator.CreateInstance(debtorType) as DebtorClient;
            StreamingManager.Copy(selectedItem.deb, debt);
            debt.Transactions = selectedItem.ChildRecords;

            //Setting the Localization for the debtor
            var debtLocalize = Uniconta.ClientTools.Localization.GetLocalization(Uniconta.Reports.Utilities.ReportGenUtil.GetLanguage(debt, api.CompanyEntity));
            foreach (var rec in debt.Transactions)
                rec.LocOb = debtLocalize;

            var debtorMessageClient = await Utility.GetDebtorMessageClient(api, debt._Language, DebtorEmailType.AccountStatement);

            return new DebtorStatementReportClient(companyClient, debt, defaultFromDate, defaultToDate, "Statement", logo, debtorMessageClient);
        }
#endif
        Task<SQLCache> accountCacheTask;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (accountCache == null)
            {
                accountCacheTask = Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api);
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
                    SetExpandAndCollapse(dgDebtorTrans.IsMasterRowExpanded(0));
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
                case "ViewTransactions":
                    var item = childDgDebtorTrans.SelectedItem as DebtorTransClientTotal;
                    if (item != null)
                        AddDockItem(TabControls.AccountsTransaction, item, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("DebtorTransactions"), item._Account));
                    break;
                case "VoucherTransactions":
                    var selItem = childDgDebtorTrans.SelectedItem as DebtorTransClientTotal;
                    if (selItem != null)
                        AddDockItem(TabControls.AccountsTransaction, childDgDebtorTrans.syncEntity, string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selItem._Voucher));
                    break;
                case "DebtorFilter":

                    if (debtorFilterDialog == null)
                    {
                        if (debtorFilterCleared)
                            debtorFilterDialog = new CWServerFilter(api, typeof(DebtorClient), null, null, DebtorUserFields);
                        else
                            debtorFilterDialog = new CWServerFilter(api, typeof(DebtorClient), null, null, DebtorUserFields);
                        debtorFilterDialog.Closing += debtorFilterDialog_Closing;
#if !SILVERLIGHT
                        debtorFilterDialog.Show();
                    }
                    else
                        debtorFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    debtorFilterDialog.Show();
#endif
                    break;
                case "ClearDebtorFilter":
                    debtorFilterDialog = null;
                    debtorFilterValues = null;
                    debtorFilterCleared = true;
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void debtorFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (debtorFilterDialog.DialogResult == true)
            {
                debtorFilterValues = debtorFilterDialog.PropValuePair;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            debtorFilterDialog.Hide();
#endif
        }

#if SILVERLIGHT
        private void ChildDgDebtorTrans_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            var detailsSelectedItem = e.NewItem as DebtorTransClientTotal;
            childDgDebtorTrans.SelectedItem = detailsSelectedItem;
            childDgDebtorTrans.syncEntity.Row = detailsSelectedItem;
        }
#endif

        bool manualExpanded = false;
        private void SetExpandAndCollapse(bool expandState)
        {
            if (ibase == null)
                return;
            if (dgDebtorTrans.ItemsSource == null) return;
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

        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            LoadDCTrans();
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
                    dgDebtorTrans.ExpandMasterRow(iRow);
                else
                    dgDebtorTrans.CollapseMasterRow(iRow);
        }

        void SendMail()
        {
            var fromAccount = Convert.ToString(cmbFromAccount.EditValue);
            var toAccount = Convert.ToString(cmbToAccount.EditValue);

            if (!string.IsNullOrEmpty(toAccount) && fromAccount == toAccount)
            {
                var cwSendMail = new CWSendInvoice();
#if !SILVERLIGHT
                cwSendMail.DialogTableId = 2000000030;
#endif
                cwSendMail.Closed += delegate
                  {
                      if (cwSendMail.DialogResult == true)
                          DoSendAsEmail(true, fromAccount, toAccount, cwSendMail.Emails, cwSendMail.sendOnlyToThisEmail);
                  };
                cwSendMail.Show();
            }
            else
            {
                CWSendStatementEmail cw = new CWSendStatementEmail();
#if !SILVERLIGHT
                cw.DialogTableId = 2000000031;
#endif
                cw.Closed += delegate
                {
                    if (cw.DialogResult == true)
                        DoSendAsEmail(cw.SendAll, fromAccount, toAccount, null, false);
                };
                cw.Show();
            }
        }

        async void DoSendAsEmail(bool SendAll, string fromAccount, string toAccount, string emails, bool onlyThisEmail)
        {
            var transApi = new ReportAPI(api);
            DebtorStatement.SetDateTime(txtDateFrm, txtDateTo);
            DateTime fromDate = DebtorStatement.DefaultFromDate, toDate = DebtorStatement.DefaultToDate;
            bool OnlyOpen = cbxOnlyOpen.IsChecked.Value;
            if (SendAll)
            {
                busyIndicator.IsBusy = true;
                busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                var result = await transApi.DebtorAccountStatement(fromDate, toDate, fromAccount, toAccount, OnlyOpen, null, false, debtorFilterValues, emails, onlyThisEmail);
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
                var lstDebtorTransLst = ((IEnumerable<DebtorStatementList>)dgDebtorTrans.ItemsSource);
                foreach (var t in lstDebtorTransLst)
                {
                    if (!t._Mark)
                        continue;
                    var result = await transApi.DebtorAccountStatement(fromDate, toDate, t.AccountNumber, t.AccountNumber, OnlyOpen, null, RunInBack, debtorFilterValues, emails, onlyThisEmail);
                    if (result != ErrorCodes.Succes)
                    {
                        string error = string.Format("{0}:{1}", t.AccountNumber, Uniconta.ClientTools.Localization.lookup(result.ToString()));
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
            SetExpandAndCollapse(true);
            statementList.Clear();

            DebtorStatement.SetDateTime(txtDateFrm, txtDateTo);
            DateTime fromDate = DebtorStatement.DefaultFromDate, toDate = DebtorStatement.DefaultToDate;

            var isAscending = cbxAscending.IsChecked.Value;
            var skipBlank = cbxSkipBlank.IsChecked.Value;
            var OnlyOpen = cbxOnlyOpen.IsChecked.Value;

            var Pref = api.session.Preference;
            Pref.Debtor_isAscending = isAscending;
            Pref.Debtor_skipBlank = skipBlank;
            Pref.Debtor_OnlyOpen = OnlyOpen;

            string fromAccount = null, toAccount = null;
            var accountObj = cmbFromAccount.EditValue;
            if (accountObj != null)
                fromAccount = accountObj.ToString();

            accountObj = cmbToAccount.EditValue;
            if (accountObj != null)
                toAccount = accountObj.ToString();

            busyIndicator.IsBusy = true;
            var transApi = new ReportAPI(api);
            var listTrans = (DebtorTransClientTotal[])await transApi.GetTransWithPrimo(new DebtorTransClientTotal(), fromDate, toDate, fromAccount, toAccount, OnlyOpen, null, debtorFilterValues);
            if (listTrans != null)
            {
                var t = accountCacheTask;
                if (t != null)
                    accountCache = await t;

                string currentItem = string.Empty;
                DebtorStatementList masterDbStatement = null;
                List<DebtorTransClientTotal> dbTransClientChildList = null;
                double SumAmount = 0d, SumAmountCur = 0d;

                foreach (var trans in listTrans)
                {
                    if (trans._Account != currentItem)
                    {
                        currentItem = trans._Account;

                        if (masterDbStatement != null)
                        {
                            if (!skipBlank || SumAmount != 0 || dbTransClientChildList.Count > 1)
                                statementList.Add(masterDbStatement);
                            if (!isAscending)
                                dbTransClientChildList.Reverse();
                        }

                        masterDbStatement = new DebtorStatementList((Debtor)accountCache.Get(currentItem));
                        dbTransClientChildList = new List<DebtorTransClientTotal>();
                        masterDbStatement.ChildRecords = dbTransClientChildList;
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

                    dbTransClientChildList.Add(trans);
                }

                if (masterDbStatement != null)
                {
                    if (!skipBlank || SumAmount != 0 || dbTransClientChildList.Count > 1)
                        statementList.Add(masterDbStatement);
                    if (!isAscending)
                        dbTransClientChildList.Reverse();
                }

                if (statementList.Any())
                {
                    dgDebtorTrans.ItemsSource = null;
                    dgDebtorTrans.ItemsSource = statementList;
                }
                dgDebtorTrans.Visibility = Visibility.Visible;
            }
            busyIndicator.IsBusy = false;

            if (_master != null)
                SetExpandAndCollapse(false);
        }

        private void cmbPrintintPreview_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var cmbItems = sender as DimComboBoxEditor;
            if (Convert.ToString(cmbItems.SelectedItem) == Uniconta.ClientTools.Localization.lookup("Internal"))
                dgDebtorTrans.IsStandardPrint = true;
            else
                dgDebtorTrans.IsStandardPrint = false;
        }
#if !SILVERLIGHT
        private void cbxPageBreak_Click(object sender, RoutedEventArgs e)
        {
            if (cbxPageBreak.IsChecked == true)
                dgDebtorTrans.PageBreak = true;
            else
                dgDebtorTrans.PageBreak = false;
        }
#endif
        public override object GetPrintParameter()
        {
            if (!manualExpanded)
            {
                int dataRowCount = statementList.Count;
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
#if !SILVERLIGHT
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

        public override IDataNode CreateMasterDetailPrintingNode(NodeContainer container, RowNode rowNode, IDataNode node, int index, Size pageSize)
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
        public CustomGridMasterDetailPrintingNode(bool HasPageBreak, int groupLevel, NodeContainer parentContainer, RowNode rowNode, GridPrintingDataTreeBuilder treeBuilder, IDataNode parent, int index, Size pageSize) : base(parentContainer, rowNode, treeBuilder, parent, index, pageSize) { this.HasPageBreak = HasPageBreak; }
#endif
    }
}
