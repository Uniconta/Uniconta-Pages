using Uniconta.API.Service;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using Uniconta.ClientTools.Util;
using System.IO;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using Uniconta.API.Plugin;
using Uniconta.API.System;
using System.Text;
using UnicontaISO20022CreditTransfer;
using UnicontaClient.Pages.Creditor.Payments;
using Localization = Uniconta.ClientTools.Localization;
using Uniconta.Common.Utility;
using Uniconta.API.DebtorCreditor;
using UnicontaClient.Pages.Creditor.Payments.Denmark;
using ISO20022CreditTransfer;
using Newtonsoft.Json;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorTransPayment : CreditorTransOpenClient
    {
        [NoSQL]
        [Display(Name = "Amount", ResourceType = typeof(GLDailyJournalText))]
        public double TransAmount 
        { 
            get 
            {
                if (Trans._Currency != 0)
                    return -Trans._AmountCur;
                else
                    return -Trans._Amount;
            }
        }

        [NoSQL]
        [Display(Name = "Remaining", ResourceType = typeof(DCTransText))]
        public double RemainingTransAmount
        {
            get
            {
                if (Trans._Currency != 0)
                    return -_AmountOpenCur;
                else
                    return -_AmountOpen;
            }
        }

        [NoSQL]
        [Display(Name = "PaymentAmount", ResourceType = typeof(DCTransText))]
        public double PaymentAmount
        {
            get
            {
                if (MergedAmount != 0)
                    return MergedAmount;

                if (_PartialPaymentAmount != 0)
                    return _PartialPaymentAmount;

                return RemainingTransAmount - _UsedCashDiscount;
            }
        }

        [NoSQL]
        [Display(Name = "CurBalance", ResourceType = typeof(DCAccountText))]
        public double CurBalance
        {
            get
            {
                var c = Creditor;
                return c != null ? - c._CurBalance : 0d;
            }
        }

        private string _ErrorInfo;
        [Display(Name = "SystemInfo", ResourceType = typeof(DCTransText))]
        public string ErrorInfo { get { return _ErrorInfo; } set { _ErrorInfo = value; NotifyPropertyChanged("ErrorInfo"); } }

        public string CurrencyLocalStr { get { return CurrencyUtil.GetStringFromId(Currency ?? (CompanyId != 0 ? CompanyRef._CurrencyId : Currencies.DKK)); } }

        private string _MergePaymId;
        [Display(Name = "MergePaymId", ResourceType = typeof(DCTransText))]
        public string MergePaymId { get { return _MergePaymId; } set { _MergePaymId = value; NotifyPropertyChanged("MergePaymId"); } }

        private int _RgltryRptgCode;
        [Display(Name = "RegulatoryReportingCode", ResourceType = typeof(DCTransText))]
        public int RgltryRptgCode { get { return _RgltryRptgCode; } set { _RgltryRptgCode = value; NotifyPropertyChanged("RgltryRptgCode"); } }

        private string _RgltryRptgText;
        [StringLength(35)]
        [Display(Name = "RegulatoryReportingText", ResourceType = typeof(DCTransText))]
        public string RgltryRptgText { get { return _RgltryRptgText; } set { _RgltryRptgText = value; NotifyPropertyChanged("RgltryRptgText"); } }

        public string ISOPaymentType;
        public bool internationalPayment;
        public StringBuilder invoiceNumbers;
        public bool hasBeenMerged;
        public StringBuilder rowNumbers;
        public bool settleTypeRowId;
        public double MergedAmount;

        internal Payments thisPage;

        [JsonIgnore]
        [ReportingAttribute]
        public VouchersClient VouchersRef
        {
            get
            {
                return thisPage?.GetDocumentRef(DocumentRef);
            }
        }
    }

    public class PaymentsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransPayment); } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool CanInsert { get { return false; } }
        protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
            }
            else
                base.OnPreviewKeyDown(e);
        }
    }

    public partial class Payments : GridBasePage
    {
        int MaxDocumentRef, MinDocumentRef;
        VouchersClient[] Vouchers;

        private bool glJournalGenerated;

        SQLCache CreditorCache, JournalCache, BankAccountCache, PaymentFormatCache;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string ToAccount { get; set; }

        public override string NameOfControl
        {
            get { return TabControls.Payments.ToString(); }
        }
        public Payments(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public override Task InitQuery() { return null; }

        List<CreditorTransPayment> creditorTransPayList;
        public Payments(List<CreditorTransPayment> list)
           : base(null)
        {
            glJournalGenerated = true;
            creditorTransPayList = list;
            InitPage();
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "GenerateJournalLines", "SaveGrid", "RefreshGrid", "CreditorFilter", "ClearFilter" });
            rowSearch.Height = new GridLength(0d);
            Account.Visible = Name.Visible = false;
        }

        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgCreditorTranOpenGrid;
            dgCreditorTranOpenGrid.api = api;
            SetRibbonControl(localMenu, dgCreditorTranOpenGrid);
            dgCreditorTranOpenGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCreditorTranOpenGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;

            dgCreditorTranOpenGrid.ShowTotalSummary();
            if (toDate == DateTime.MinValue)
                toDate = txtDateTo.DateTime.Date;

            txtDateTo.DateTime = toDate;
            txtDateFrm.DateTime = fromDate;
            SetCreditorFilterUserFields();

            GetMergeUnMergePaymMenuItem();
            GetExpandAndCollapseMenuItem();
            ribbonControl.DisableButtons("ExpandGroups");
            ribbonControl.DisableButtons("CollapseGroups");
            if (glJournalGenerated)
                ribbonControl.DisableButtons("MergeUnMergePaym");

            if (creditorTransPayList != null)
            {
                dgCreditorTranOpenGrid.ItemsSource = creditorTransPayList;
                dgCreditorTranOpenGrid.Visibility = Visibility.Visible;
            }

            RemoveMenuItem();

            var Comp = api.CompanyEntity;
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
            JournalCache = Comp.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
            BankAccountCache = Comp.GetCache(typeof(Uniconta.DataModel.BankStatement));
            PaymentFormatCache = Comp.GetCache(typeof(Uniconta.DataModel.CreditorPaymentFormat));
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;
            if (Comp._CountryId != CountryCode.Norway)
            {
                UtilDisplay.RemoveMenuCommand(rb, "ReadReceipt");
                UtilDisplay.RemoveMenuCommand(rb, "PaymStatusReport");
            }
        }

        public override bool IsDataChaged { get { return false; } }

        static DateTime fromDate;
        static DateTime toDate;

        public Payments(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        TableField[] CreditorUserFields;
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
        CWServerFilter creditorFilterDialog = null;

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorTranOpenGrid.SelectedItem as CreditorTransPayment;

            switch (ActionType)
            {
                case "Search":
                    BtnSearch();
                    break;

                case "SaveGrid":
                    saveGrid();
                    break;

                case "ClearFilter":
                    gridRibbon_BaseActions(ActionType);
                    setMergeUnMergePaym(false);
                    break;

                case "GeneratePaymentFile":
                    if (dgCreditorTranOpenGrid.ItemsSource != null)
                        GeneratePaymentFile();
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgCreditorTranOpenGrid.syncEntity, api, busyIndicator);
                    break;
                case "Validate":
                    if (dgCreditorTranOpenGrid.ItemsSource != null)
                        CallValidatePayment();
                    break;

                case "ReadReceipt":
                    CWReadReceipt readReceiptDialog = new CWReadReceipt(this.api);
                    readReceiptDialog.Show();
                    break;

                case "PaymStatusReport":
                    if (dgCreditorTranOpenGrid.ItemsSource != null)
                        PaymStatusReport();
                    break;

                case "MergeUnMergePaym":
                    if (dgCreditorTranOpenGrid.ItemsSource != null)
                        MergePaym();
                    break;

                case "ExpandGroups":
                    if (dgCreditorTranOpenGrid.ItemsSource != null)
                        ExpandGroups();
                    break;

                case "CollapseGroups":
                    if (dgCreditorTranOpenGrid.ItemsSource != null)
                        CollapseGroups();
                    break;

                case "UncheckAllPaid":
                    UncheckPaid(dgCreditorTranOpenGrid.GetVisibleRows() as IEnumerable<CreditorTransPayment>);
                    break;

                case "UncheckMarkedPaid":
                    UncheckPaid(dgCreditorTranOpenGrid.SelectedItems as IEnumerable<CreditorTransPayment>);
                    break;

                case "UncheckCurrentPaid":
                    if (selectedItem != null)
                        UncheckPaid(new[] { selectedItem });
                    break;

                case "CheckOnholdAll":
                    CheckUncheckOnhold(dgCreditorTranOpenGrid.GetVisibleRows() as IEnumerable<CreditorTransPayment>, true);
                    break;

                case "CheckOnholdMarked":
                    CheckUncheckOnhold(dgCreditorTranOpenGrid.SelectedItems as IEnumerable<CreditorTransPayment>, true);
                    break;

                case "CheckOnholdCurrent":
                    if (selectedItem != null)
                        CheckUncheckOnhold(new[] { selectedItem }, true);
                    break;

                case "UncheckOnholdAll":
                    CheckUncheckOnhold(dgCreditorTranOpenGrid.GetVisibleRows() as IEnumerable<CreditorTransPayment>, false);
                    break;

                case "UncheckOnholdMarked":
                    CheckUncheckOnhold(dgCreditorTranOpenGrid.SelectedItems as IEnumerable<CreditorTransPayment>, false);
                    break;

                case "UncheckOnholdCurrent":
                    if (selectedItem != null)
                        CheckUncheckOnhold(new[] { selectedItem }, false);
                    break;

                #region Generate GL Journal
                case "GenerateJournalLines":
                    if (dgCreditorTranOpenGrid.ItemsSource == null) return;

                    ImportToLine cwLine = new ImportToLine(api);

                    cwLine.DialogTableId = 2000000036;
                    cwLine.Closed
                        += async delegate
                        {
                            if (cwLine.DialogResult == true && !string.IsNullOrEmpty(cwLine.Journal))
                            {
                                ExpandCollapseAllGroups();

                                busyIndicator.IsBusy = true;
                                Uniconta.API.GeneralLedger.PostingAPI posApi = new Uniconta.API.GeneralLedger.PostingAPI(api);
                                long LineNumber = await posApi.MaxLineNumber(cwLine.Journal);

                                NumberSerieAPI numberserieApi = new NumberSerieAPI(posApi);
                                int nextVoucherNumber = 0;

                                var DJclient = (Uniconta.DataModel.GLDailyJournal)JournalCache.Get(cwLine.Journal);

                                if (!DJclient._GenerateVoucher && !DJclient._ManualAllocation && cwLine.AddVoucherNumber)
                                    nextVoucherNumber = (int)await numberserieApi.ViewNextNumber(DJclient._NumberSerie);

                                var credTransPaymLst = dgCreditorTranOpenGrid.GetVisibleRows() as IList<CreditorTransPayment>;
                                var dictPaymTransfer = new Dictionary<long, CreditorTransPayment>(credTransPaymLst.Count);

                                foreach (var rec in credTransPaymLst.OrderBy(x => x.PaymentAmount))
                                {
                                    if (rec._OnHold || (rec.PaymentAmount <= 0d && rec.PaymentRefId == 0))
                                        continue;

                                    var includeCashDisc = rec._UsedCashDiscount != 0 && rec._PartialPaymentAmount == 0;
                                    var paymRefId = rec.PaymentRefId != 0 && !includeCashDisc ? rec.PaymentRefId : -rec.PrimaryKeyId;

                                    CreditorTransPayment mergePaymentRefId;
                                    if (dictPaymTransfer.TryGetValue(paymRefId, out mergePaymentRefId))
                                    {
                                        mergePaymentRefId.MergedAmount += rec.PaymentAmount;
                                        mergePaymentRefId._UsedCashDiscount += includeCashDisc ? rec._UsedCashDiscount : 0;
                                        mergePaymentRefId.settleTypeRowId = true;
                                        mergePaymentRefId.rowNumbers.Append(';').AppendNum(rec.PrimaryKeyId);
                                        mergePaymentRefId.invoiceNumbers.Append(';').Append(rec.InvoiceAN);

                                        mergePaymentRefId.hasBeenMerged = true;
                                    }
                                    else
                                    {
                                        mergePaymentRefId = new CreditorTransPayment();
                                        StreamingManager.Copy(rec, mergePaymentRefId);
                                        mergePaymentRefId.PaymentRefId = paymRefId;
                                        mergePaymentRefId.MergedAmount = rec.PaymentAmount;
                                        mergePaymentRefId.invoiceNumbers = new StringBuilder(rec.InvoiceAN);
                                        mergePaymentRefId.rowNumbers = new StringBuilder();
                                        mergePaymentRefId.rowNumbers.AppendNum(rec.PrimaryKeyId);
                                        mergePaymentRefId.settleTypeRowId = true;

                                        mergePaymentRefId._UsedCashDiscount = includeCashDisc ? rec._UsedCashDiscount : 0;
                                        dictPaymTransfer.Add(paymRefId, mergePaymentRefId);
                                    }
                                }
                                if (dictPaymTransfer.Count == 0)
                                {
                                    busyIndicator.IsBusy = false;
                                    return;
                                }

                                var listLineClient = new List<Uniconta.DataModel.GLDailyJournalLine>(dictPaymTransfer.Count);
                                foreach (var cTOpenClient in dictPaymTransfer.Values)
                                {
                                    var creditor = cTOpenClient.Creditor;
                                    var lineclient = new GLDailyJournalLineClient();
                                    lineclient.SetMaster(DJclient);
                                    lineclient._DCPostType = DCPostType.Payment;
                                    lineclient._LineNumber = ++LineNumber;
                                    lineclient._Date = cTOpenClient.GetPayDate();
                                    lineclient._TransType = cwLine.TransType;
                                    lineclient._AccountType = (byte)GLJournalAccountType.Creditor;
                                    lineclient._Account = cTOpenClient.Account;
                                    lineclient._OffsetAccount = cwLine.BankAccount;
                                    lineclient._Invoice = cTOpenClient.hasBeenMerged ? null : cTOpenClient.InvoiceAN;
                                    if (creditor != null)
                                    {
                                        lineclient._Dim1 = creditor._Dim1;
                                        lineclient._Dim2 = creditor._Dim2;
                                        lineclient._Dim3 = creditor._Dim3;
                                        lineclient._Dim4 = creditor._Dim4;
                                        lineclient._Dim5 = creditor._Dim5;
                                    }

                                    if (cTOpenClient.settleTypeRowId)
                                    {
                                        lineclient._SettleValue = SettleValueType.RowId;
                                        lineclient._Settlements = cTOpenClient.rowNumbers.ToString();
                                    }
                                    else
                                    {
                                        lineclient._Settlements = null;
                                    }

                                    lineclient._UsedCachDiscount = cTOpenClient._UsedCashDiscount;
                                    lineclient._DocumentRef = cTOpenClient.DocumentRef;
                                    var curOpen = cTOpenClient._AmountOpenCur;
                                    if (curOpen != 0d && cTOpenClient.Currency.HasValue)
                                    {
                                        lineclient._Currency = (byte)cTOpenClient.Currency.Value;
                                        lineclient.AmountCur = cTOpenClient.PaymentAmount;
                                        if (cTOpenClient.PaymentAmount == -cTOpenClient._AmountOpen)
                                            lineclient.Amount = -cTOpenClient._AmountOpen;
                                        else
                                            lineclient.Amount = cTOpenClient._AmountOpen * cTOpenClient.PaymentAmount / curOpen; // payAmount different sign than curOpen, so no minus.
                                    }
                                    else
                                        lineclient.Amount = cTOpenClient.PaymentAmount;

                                    if (nextVoucherNumber != 0)
                                    {
                                        lineclient._Voucher = nextVoucherNumber;
                                        nextVoucherNumber++;
                                    }

                                    listLineClient.Add(lineclient);
                                }
                                if (listLineClient.Count > 0)
                                {
                                    ErrorCodes errorCode = await api.Insert(listLineClient);
                                    busyIndicator.IsBusy = false;
                                    if (errorCode != ErrorCodes.Succes)
                                        UtilDisplay.ShowErrorCode(errorCode);
                                    else
                                    {
                                        if (nextVoucherNumber != 0)
                                            numberserieApi.SetNumber(DJclient._NumberSerie, nextVoucherNumber - 1);

                                        var text = string.Concat(Uniconta.ClientTools.Localization.lookup("GenerateJournalLines"), "; ", Uniconta.ClientTools.Localization.lookup("Completed"),
                                            Environment.NewLine, string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Journallines")), " ?");
                                        var select = UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel);
                                        if (select == MessageBoxResult.OK)
                                            AddDockItem(TabControls.GL_DailyJournalLine, DJclient, null, null, true);
                                    }
                                }
                                else
                                {
                                    busyIndicator.IsBusy = false;
                                    UtilDisplay.ShowErrorCode(ErrorCodes.NoLinesToUpdate);
                                }
                            }
                        };
                    cwLine.Show();
                    break;
                #endregion
                case "RefreshGrid":
                    if (dgCreditorTranOpenGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgCreditorTranOpenGrid);

                    BtnSearch();
                    setMergeUnMergePaym(false);

                    break;

                case "CreditorFilter":
                    if (dgCreditorTranOpenGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgCreditorTranOpenGrid);
                    if (creditorFilterDialog == null)
                    {
                        creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), null, null, CreditorUserFields);
                        creditorFilterDialog.GridSource = dgCreditorTranOpenGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        creditorFilterDialog.Closing += creditorFilterDialog_Closing;
                        creditorFilterDialog.Show();
                    }
                    else
                    {
                        creditorFilterDialog.GridSource = dgCreditorTranOpenGrid.ItemsSource as IList<UnicontaBaseEntity>;
                        creditorFilterDialog.Show(true);
                    }
                    setMergeUnMergePaym(false);
                    break;
                case "ClearCreditorFilter":
                    creditorFilterDialog = null;
                    creditorFilterValues = null;
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public IEnumerable<PropValuePair> creditorFilterValues;
        void creditorFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (creditorFilterDialog.DialogResult == true)
            {
                creditorFilterValues = creditorFilterDialog.PropValuePair;
            }
            e.Cancel = true;
            creditorFilterDialog.Hide();
        }


        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (JournalCache == null)
                JournalCache = await api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal)).ConfigureAwait(false);
            if (BankAccountCache == null)
                BankAccountCache = await api.LoadCache(typeof(Uniconta.DataModel.BankStatement)).ConfigureAwait(false);
            if (api.CompanyEntity.CreditorBankApprovement)
                api.LoadCache(typeof(Uniconta.DataModel.CreditorPaymentAccount), true).ConfigureAwait(false);
        }

        ItemBase ibaseExpandGroups;
        ItemBase ibaseCollapseGroups;
        void GetExpandAndCollapseMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibaseExpandGroups = UtilDisplay.GetMenuCommandByName(rb, "ExpandGroups");
            ibaseCollapseGroups = UtilDisplay.GetMenuCommandByName(rb, "CollapseGroups");
        }

        private void ExpandGroups()
        {
            if (ibaseExpandGroups == null)
                return;

            dgCreditorTranOpenGrid.ExpandAllGroups();
        }

        private void CollapseGroups()
        {
            if (ibaseExpandGroups == null)
                return;

            dgCreditorTranOpenGrid.CollapseAllGroups();
        }

        ItemBase ibaseMergePaym;
        bool doMergePaym;
        void GetMergeUnMergePaymMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibaseMergePaym = UtilDisplay.GetMenuCommandByName(rb, "MergeUnMergePaym");
        }
        private void setMergeUnMergePaym(bool doMergePayment)
        {
            doMergePaym = doMergePayment;
            if (ibaseMergePaym == null)
                return;
            if (doMergePaym)
            {
                ibaseMergePaym.Caption = Localization.lookup("UnmergePayments");
                ibaseMergePaym.LargeGlyph = Utilities.Utility.GetGlyph("Match_Remove_32x32");

                dgCreditorTranOpenGrid.GroupBy("MergePaymId");
                var cols = dgCreditorTranOpenGrid.Columns;
                cols.GetColumnByName("PaymentFormat").AllowFocus = false;
                cols.GetColumnByName("PaymentMethod").AllowFocus = false;
                cols.GetColumnByName("PaymentId").AllowFocus = false;
                cols.GetColumnByName("SWIFT").AllowFocus = false;
                cols.GetColumnByName("PaymentDate").AllowFocus = false;

                ribbonControl.EnableButtons("ExpandGroups");
                ribbonControl.EnableButtons("CollapseGroups");
            }
            else
            {
                var source = (IEnumerable<CreditorTransPayment>)dgCreditorTranOpenGrid.ItemsSource;
                if (source != null)
                {
                    foreach (var rec in source)
                        rec.MergePaymId = null;
                }

                ibaseMergePaym.Caption = Localization.lookup("MergePayments");
                ibaseMergePaym.LargeGlyph = Utilities.Utility.GetGlyph("Match_Add_32x32");

                dgCreditorTranOpenGrid.UngroupBy("MergePaymId");
                var cols = dgCreditorTranOpenGrid.Columns;
                cols.GetColumnByName("PaymentFormat").AllowFocus = true;
                cols.GetColumnByName("PaymentMethod").AllowFocus = true;
                cols.GetColumnByName("PaymentId").AllowFocus = true;
                cols.GetColumnByName("SWIFT").AllowFocus = true;
                cols.GetColumnByName("PaymentDate").AllowFocus = true;
                cols.GetColumnByName("MergePaymId").Visible = false;

                ribbonControl.DisableButtons("ExpandGroups");
                ribbonControl.DisableButtons("CollapseGroups");
            }
        }

        void ExpandCollapseAllGroups(bool ExpandAll = true)
        {
            if (doMergePaym)
            {
                if (ExpandAll)
                    dgCreditorTranOpenGrid.ExpandAllGroups();
                else
                    dgCreditorTranOpenGrid.CollapseAllGroups();
            }
        }

        private async void GeneratePaymentFile()
        {
            var savetask = saveGrid();
            if (savetask != null)
                await savetask;

            CWImportPayment cwwin = new CWImportPayment(api);
            cwwin.Closing += async delegate
            {
                if (cwwin.DialogResult != true)
                    return;

                var paymentFormatRec = cwwin.PaymentFormat;
                var paymMethod = (ExportFormatType)paymentFormatRec._ExportFormat;
                var paymentFormat = paymentFormatRec._Format;

                if (cwwin.userPlugin == null)
                {
                    ExpandCollapseAllGroups();

                    var credTransPaymLst = dgCreditorTranOpenGrid.GetVisibleRows() as IList<CreditorTransPayment>;
                    if (await ValidatePayments(credTransPaymLst, paymentFormatRec))
                    {
                        try
                        {
                            List<CreditorTransPayment> paymentListMerged = null;
                            var dictPaym = new Dictionary<String, CreditorTransPayment>();
                            CreditorTransPayment mergePayment;

                            var credTransPaymSelectedLst = credTransPaymLst.Where(s => s.ErrorInfo == BaseDocument.VALIDATE_OK);

                            TransactionAPI tranApi = new TransactionAPI(api);
                            var uniqueEndToEndId = (int)await tranApi.GetPaymentId(credTransPaymSelectedLst.Count());
                            var uniqueFileId = uniqueEndToEndId;

                            if (doMergePaym)
                                credTransPaymSelectedLst = credTransPaymSelectedLst.OrderBy(x => x.MergePaymId).ToList();

                            foreach (var rec in credTransPaymSelectedLst)
                            {
                                if (doMergePaym)
                                {
                                    if (dictPaym.TryGetValue(rec.MergePaymId, out mergePayment))
                                    {
                                        mergePayment.MergedAmount += rec.PaymentAmount;
                                        mergePayment.invoiceNumbers.Append(',').Append(rec.InvoiceAN);

                                        mergePayment.RgltryRptgCode = mergePayment.RgltryRptgCode == 0 ? rec.RgltryRptgCode : mergePayment.RgltryRptgCode;
                                        mergePayment.RgltryRptgText = mergePayment.RgltryRptgText == null ? rec.RgltryRptgText : mergePayment.RgltryRptgText;

                                        if (paymentFormatRec._PaymentGrouping == PaymentGroupingType.All)
                                            mergePayment._PaymentDate = rec._PaymentDate < mergePayment._PaymentDate ? rec._PaymentDate : mergePayment._PaymentDate;

                                        if (rec.MergePaymId == StandardPaymentFunctions.MERGEID_SINGLEPAYMENT)
                                        {
                                            uniqueEndToEndId++;
                                            rec.PaymentRefId = uniqueEndToEndId;
                                        }
                                        else
                                            rec.PaymentRefId = mergePayment.PaymentRefId;
                                    }
                                    else
                                    {
                                        uniqueEndToEndId++;
                                        rec.PaymentRefId = uniqueEndToEndId;

                                        mergePayment = new CreditorTransPayment();
                                        StreamingManager.Copy(rec, mergePayment);
                                        mergePayment.MergePaymId = rec.MergePaymId;
                                        mergePayment.ISOPaymentType = rec.ISOPaymentType;
                                        mergePayment.PaymentRefId = rec._PaymentRefId;
                                        mergePayment.invoiceNumbers = new StringBuilder(rec.InvoiceAN);
                                        mergePayment.MergedAmount = rec.PaymentAmount;
                                        mergePayment.ErrorInfo = rec.ErrorInfo;
                                        mergePayment.RgltryRptgCode = rec.RgltryRptgCode;
                                        mergePayment.RgltryRptgText = rec.RgltryRptgText;


                                        dictPaym.Add(rec.MergePaymId, mergePayment);
                                    }
                                }
                                else
                                    rec.PaymentRefId = uniqueEndToEndId++;
                            }

                            paymentListMerged = dictPaym.Values.ToList();

                            List<CreditorTransPayment> paymentList = null;
                            if (doMergePaym)
                            {
                                foreach (var rec in paymentListMerged)
                                {
                                    if (rec.PaymentAmount <= 0)
                                    {
                                        rec.MergePaymId = Localization.lookup("Excluded");

                                        foreach (var recTrans in credTransPaymSelectedLst.Where(s => s.PaymentRefId == rec.PaymentRefId))
                                        {
                                            recTrans.ErrorInfo = string.Concat(Localization.lookup("MergePayments"), " <= 0");
                                            recTrans.MergePaymId = Localization.lookup("Excluded");
                                        }
                                    }
                                }

                                paymentList = paymentListMerged.Where(s => s.MergePaymId != Localization.lookup("Excluded") && s.MergePaymId != StandardPaymentFunctions.MERGEID_SINGLEPAYMENT).ToList();
                                foreach (var s in credTransPaymSelectedLst)
                                {
                                    if (s.MergePaymId == StandardPaymentFunctions.MERGEID_SINGLEPAYMENT)
                                        paymentList.Add(s);
                                }
                            }
                            else
                                paymentList = credTransPaymSelectedLst.ToList();

                            bool ret = false;
                            if (paymMethod == ExportFormatType.CSV)
                                ret = true;
                            else if (paymMethod == ExportFormatType.Nordea_CSV)
                                ret = NordeaPaymentFormat.GenerateFile(paymentList, api.CompanyEntity, paymentFormatRec, BankAccountCache, CreditorCache, glJournalGenerated);
                            else if (paymMethod == ExportFormatType.DanskeBank_CSV)
                                ret = DanskeBankPayFormat.GenerateFile(paymentList, api.CompanyEntity, paymentFormatRec, BankAccountCache, CreditorCache, glJournalGenerated);
                            else if (paymMethod == ExportFormatType.BankData)
                                ret = BankDataPayFormat.GenerateFile(paymentList, api.CompanyEntity, paymentFormatRec, BankAccountCache, CreditorCache, glJournalGenerated);
                            else if (paymMethod == ExportFormatType.SDC)
                                ret = SDCPayFormat.GenerateFile(paymentList, api.CompanyEntity, paymentFormatRec, BankAccountCache, CreditorCache, glJournalGenerated);
                            else if (paymMethod == ExportFormatType.BEC_CSV)
                                ret = BECPayFormat.GenerateFile(paymentList, api.CompanyEntity, paymentFormatRec, BankAccountCache, CreditorCache, glJournalGenerated);
                            else if (paymMethod == ExportFormatType.ISO20022_DK || paymMethod == ExportFormatType.ISO20022_NL || paymMethod == ExportFormatType.ISO20022_NO || paymMethod == ExportFormatType.ISO20022_DE ||
                                     paymMethod == ExportFormatType.ISO20022_EE || paymMethod == ExportFormatType.ISO20022_SE || paymMethod == ExportFormatType.ISO20022_UK || paymMethod == ExportFormatType.ISO20022_LT ||
                                     paymMethod == ExportFormatType.ISO20022_CH)
                                ret = GeneratePaymentFileISO20022(paymentList, paymentFormatRec, uniqueFileId);

                            if (ret == true)
                                UpdateCreatedPayments(credTransPaymSelectedLst);
                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex);
                        }
                    }
                    else
                    {
                        GeneratePluginPaymentFile(paymentFormatRec, cwwin.userPlugin);
                    }
                }
            };
            cwwin.Show();

        }

        void PaymStatusReport()
        {
            CWPaymStatusReport cwwin = new CWPaymStatusReport(api);

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    if (cwwin.DialogResult != true)
                        return;

                    ISO20022StatusReport statusReport = new ISO20022StatusReport();
                    statusReport.StatusReport(dgCreditorTranOpenGrid, cwwin.FilePath, this.api);
                }
            };
            cwwin.Show();
        }

        void UncheckPaid(IEnumerable<CreditorTransPayment> paymentList)
        {
            if (paymentList != null)
            {
                List<CreditorTransPayment> ListTransPaym = new List<CreditorTransPayment>();
                foreach (var rec in paymentList)
                {
                    if (rec._Paid)
                    {
                        rec.Paid = false;
                        ListTransPaym.Add(rec);
                    }
                }
                if (ListTransPaym.Count > 0)
                    api.Update(ListTransPaym);
            }
        }

        void CheckUncheckOnhold(IEnumerable<CreditorTransPayment> paymentList, bool check)
        {
            if (paymentList != null)
            {
                List<CreditorTransPayment> ListTransPaym = new List<CreditorTransPayment>();
                foreach (var rec in paymentList)
                {
                    if (rec._OnHold != check)
                    {
                        rec.OnHold = check;
                        ListTransPaym.Add(rec);
                    }
                }
                if (ListTransPaym.Count > 0)
                    api.Update(ListTransPaym);
            }
        }

        void MergePaym()
        {
            doMergePaym = !doMergePaym;
            if (doMergePaym)
            {
                CWMergePayment cwwin = new CWMergePayment(this.api);

                cwwin.Closing += async delegate
                {
                    if (cwwin.DialogResult == true)
                    {
                        CreditorPaymentsMerge CredPaymMerge = new CreditorPaymentsMerge(api);
                        if (await CredPaymMerge.MergePayments(dgCreditorTranOpenGrid, cwwin.PaymentFormat, BankAccountCache))
                            setMergeUnMergePaym(doMergePaym);
                    }
                };
                cwwin.Show();
            }
            else
            {
                setMergeUnMergePaym(doMergePaym);
            }
        }

        async Task<Type> LoadPaymentPlugin(Uniconta.DataModel.UserPlugin plugin, CrudAPI api)
        {
            var regPluginPath = CorasauRibbonControl.GetPluginPath();
            if (regPluginPath != null)
            {
                var type = await Plugin.LoadAssembly(plugin, regPluginPath, api);
                return type;
            }
            return null;
        }

        private async void GeneratePluginPaymentFile(CreditorPaymentFormat PaymentSetup, Uniconta.DataModel.UserPlugin userPlugin)
        {
            Type plugin = null;
            if (userPlugin == null)
                return;
            else
                plugin = await LoadPaymentPlugin(userPlugin, api);
            if (plugin == null)
                return;
            var paymentPluginObj = Activator.CreateInstance(plugin) as ICreditorPaymentFormatPlugin;
            if (paymentPluginObj == null)
                return;
            paymentPluginObj.SetAPI(api);
            var fileExtension = paymentPluginObj.FileExtension;
            List<CreditorTransOpenClient> Trans = null;
            if (dgCreditorTranOpenGrid.ItemsSource != null)
            {
                var lst = dgCreditorTranOpenGrid.GetVisibleRows();
                Trans = lst as List<CreditorTransOpenClient>;
                if (Trans == null)
                    Trans = (lst as IEnumerable<CreditorTransOpenClient>).ToList();
            }

            var sfd = UtilDisplay.LoadSaveFileDialog;
            sfd.Filter = UtilFunctions.GetFilteredExtensions(fileExtension);
            bool? userClickedSave = sfd.ShowDialog();

            if (userClickedSave == true)
            {
                try
                {
                    using (Stream stream = File.Create(sfd.FileName))
                    {
                        var errorCode = paymentPluginObj.Generate(Trans, PaymentSetup, stream);
                        stream.Flush();
                        stream.Close();
                        if (errorCode != ErrorCodes.Succes)
                        {
                            var err = paymentPluginObj.GetErrorDescription();
                            UnicontaMessageBox.Show(err, Uniconta.ClientTools.Localization.lookup("Information"));
                        }
                    }
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                    return;
                }
            }
        }

        private async void CallValidatePayment()
        {
            var savetask = saveGrid();
            if (savetask != null)
                await savetask;

            CWValidatePayment cwwin = new CWValidatePayment(api);

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                    ValidatePayments(dgCreditorTranOpenGrid.GetVisibleRows() as IList<CreditorTransPayment>, cwwin.PaymentFormat, true);
            };
            cwwin.Show();
        }

        private async Task<bool> ValidatePayments(IList<CreditorTransPayment> credTransPaymLst, CreditorPaymentFormat credPaymFormat, bool validateOnly = false)
        {
            var paymentISO20022PreValidate = new PaymentISO20022PreValidate();
            var preValidateRes = paymentISO20022PreValidate.PreValidateISO20022(api.CompanyEntity, BankAccountCache, credPaymFormat, glJournalGenerated);

            if (preValidateRes.HasErrors)
            {
                var preErrors = new List<string>();
                foreach (PreCheckError error in preValidateRes.PreCheckErrors)
                {
                    preErrors.Add(error.ToString());
                }

                if (preErrors.Count != 0)
                {
                    CWErrorBox cwError = new CWErrorBox(preErrors.ToArray());
                    cwError.Show();
                    return false;
                }
            }
            else
            {
                var countErr = 0;
                var paymentISO20022Validate = new PaymentISO20022Validate(api, credPaymFormat);
                busyIndicator.IsBusy = true;
                foreach (var rec in credTransPaymLst)
                {
                    rec.ErrorInfo = null;
                    if (rec._OnHold || rec._Paid || (rec.PaymentAmount <= 0d && !doMergePaym) || rec._PaymentFormat != credPaymFormat._Format)
                        continue;

                    if (doMergePaym && rec.MergePaymId == null)
                    {
                        countErr++;
                        rec.ErrorInfo = "Merge of payments has failed";
                    }
                    else
                    {
                        if (credPaymFormat._ExportFormat != (byte)ExportFormatType.CSV)
                        {
                            var validateRes = await paymentISO20022Validate.ValidateISO20022(rec, BankAccountCache, glJournalGenerated);

                            if (validateRes.HasErrors)
                            {
                                countErr++;
                                foreach (CheckError error in validateRes.CheckErrors)
                                {
                                    rec.ErrorInfo += rec.ErrorInfo != null ? Environment.NewLine : null;
                                    rec.ErrorInfo += error.ToString();
                                }
                            }
                            else
                            {
                                rec.ErrorInfo = BaseDocument.VALIDATE_OK;
                            }
                        }
                        else
                            rec.ErrorInfo = BaseDocument.VALIDATE_OK;
                    }
                }
                busyIndicator.IsBusy = false;

                if (!validateOnly && !credTransPaymLst.Any(s => s.ErrorInfo == BaseDocument.VALIDATE_OK))
                {
                    UnicontaMessageBox.Show(Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return false;
                }

                if (validateOnly)
                {
                    if (countErr == 0)
                        UnicontaMessageBox.Show(Localization.lookup("ValidateNoError"), Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        UnicontaMessageBox.Show(string.Format("{0} {1}", countErr, Localization.lookup("JournalFailedValidation")), Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return true;
        }

        private bool GeneratePaymentFileISO20022(IEnumerable<CreditorTransPayment> paymentList, CreditorPaymentFormat credPaymFormat, int uniqueFileId)
        {
            var paymentISO20022 = new PaymentISO20022(api);
            var result = paymentISO20022.GenerateISO20022(paymentList, BankAccountCache, credPaymFormat, uniqueFileId, doMergePaym);

            if (result.NumberOfPayments != 0)
            {
                var saveDialog = UtilDisplay.LoadSaveFileDialog;
                saveDialog.FileName = result.FileName;
                saveDialog.Filter = "XML-File | *.xml";
                bool? dialogResult = saveDialog.ShowDialog();
                if (dialogResult == true)
                {
                    var filename = saveDialog.FileName;
                    result.Document.DocumentElement.SetAttribute(BaseDocument.XMLNS_XSI, BaseDocument.XMLNS_XSI_VALUE);

                    using (TextWriter sw = new StreamWriter(filename, false, result.Encoding))
                    {
                        result.Document.Save(sw);
                    }
                    return true;
                }
            }
            else
            {
                CWMessageBox cwMessage = new CWMessageBox("There are no payments!\nPlease check the System info column.");
                cwMessage.Show();
                return false;
            }

            return false;
        }

        void UpdateCreatedPayments(IEnumerable<CreditorTransPayment> paymentList)
        {
            var lstTransPaym = new List<CreditorTransPayment>();
            foreach (var rec in paymentList)
            {
                if (rec.ErrorInfo != BaseDocument.VALIDATE_OK)
                    continue;

                rec.ErrorInfo = Uniconta.ClientTools.Localization.lookup("Paid");
                rec.Paid = true;
                lstTransPaym.Add(rec);
            }
            if (lstTransPaym.Count > 0)
                api.Update(lstTransPaym);
        }

        #region Build FIK number
        private StringBuilderReuse CalculateFIKchecksum(string paymId)
        {
            var sb = StringBuilderReuse.Create();
            sb.Append(paymId);

            int Id;
            int CodeLen = paymId.Length;

            int Sum = 0;
            int factor = 2;
            for (int i = CodeLen; (--i >= 0);)
            {
                var t = (sb[i] - '0') * factor;
                if (t > 9)
                    t -= 9;
                Sum += t;
                factor = 3 - factor;
            }
            Id = 10 - (Sum % 10);
            if (Id == 10)
                Id = 0;

            sb.AppendNum(Id);

            return sb;
        }

        private void BuildPaymentIdFIK(CreditorTransPayment rec, string transPaymentId, string creditorPaymId)
        {
            if (transPaymentId != null && creditorPaymId != null)
            {
                transPaymentId = transPaymentId.ToUpper();
                if (transPaymentId.IndexOf('+') == -1 && transPaymentId.IndexOf('N') == -1)
                {
                    creditorPaymId = creditorPaymId.IndexOf('+') == -1 ? string.Concat("+", creditorPaymId) : creditorPaymId;
                    rec._PaymentId = string.Concat(transPaymentId, " ", creditorPaymId);
                }
            }
            else
            {
                rec._PaymentId = (creditorPaymId != transPaymentId && transPaymentId != null) ? transPaymentId : creditorPaymId;
            }

            //FIK PaymentId mask is used >>
            if (rec._PaymentId != null && rec._PaymentMethod != PaymentTypes.PaymentMethod4)
            {
                bool removeLast = false;
                var OCRMask = rec._PaymentId ?? string.Empty;
                OCRMask = OCRMask.ToUpper();

                if (OCRMask.IndexOf("N") == -1)
                    return;

                OCRMask = OCRMask.Replace(" ", "");
                OCRMask = OCRMask.Replace("+71<", "");
                OCRMask = OCRMask.Replace(">71<", "");
                OCRMask = OCRMask.Replace("+75<", "");
                OCRMask = OCRMask.Replace(">75<", "");
                OCRMask = OCRMask.Replace("+04<", "");
                OCRMask = OCRMask.Replace(">04<", "");
                OCRMask = OCRMask.Replace("<", "");
                OCRMask = OCRMask.Replace(">", "");

                if (OCRMask.IndexOf("L") != -1)
                {
                    OCRMask = OCRMask.Replace("L", "");
                    removeLast = true;
                }

                var paymIdMask = string.Empty;
                int accountIndex = OCRMask.IndexOf('+');
                if (accountIndex > 0)
                    paymIdMask = OCRMask.Substring(0, accountIndex);

                var invoiceNumberStr = NumberConvert.ToString(rec.Invoice);
                int maskIndexStart = paymIdMask.IndexOf('N');
                if (accountIndex <= 0 || maskIndexStart == -1 || invoiceNumberStr == "0")
                {
                    rec.ErrorInfo = Localization.lookup("FIKFormatNotValid");
                    return;
                }

                var maskIndexEnd = 0;
                for (int i = maskIndexStart; i < paymIdMask.Length; i++)
                {
                    if (paymIdMask[i] == 'N')
                        maskIndexEnd = i;
                }

                int invoiceMaskLength = maskIndexEnd - maskIndexStart + 1;
                if (invoiceMaskLength < invoiceNumberStr.Length)
                {
                    if (removeLast)
                        invoiceNumberStr = invoiceNumberStr.Substring(0, invoiceNumberStr.Length - 1);
                    else
                        invoiceNumberStr = invoiceNumberStr.Substring(invoiceNumberStr.Length - invoiceMaskLength);
                }

                invoiceNumberStr = invoiceNumberStr.PadLeft(invoiceMaskLength, '0');
                var invoiceNumberMask = string.Empty;
                invoiceNumberMask = invoiceNumberMask.PadLeft(invoiceMaskLength, 'N');
                paymIdMask = paymIdMask.Replace(invoiceNumberMask, invoiceNumberStr);

                var FIKString = CalculateFIKchecksum(paymIdMask);
                var fikAccount = OCRMask.Remove(0, accountIndex + 1);

                FIKString.Append(" +").Append(fikAccount);
                rec._PaymentId = FIKString.ToString();
            }
            //FIK PaymentId mask is used <<
        }
        #endregion

        private async void BtnSearch()
        {
            SetBusy();

            if (txtDateFrm.Text == string.Empty)
                fromDate = DateTime.MinValue;
            else
                fromDate = txtDateFrm.DateTime.Date;

            if (txtDateTo.Text == string.Empty)
                toDate = DateTime.MinValue;
            else
                toDate = txtDateTo.DateTime.Date;

            var rapi = new Uniconta.API.DebtorCreditor.ReportAPI(api);
            var t = rapi.GetPaymentTrans(new CreditorTransPayment(), fromDate, toDate, creditorFilterValues);
            var lst = (CreditorTransPayment[])await t;

            if (lst == null || CreditorCache == null)
            {
                ClearBusy();
                return;
            }

            var today = BasePage.GetSystemDefaultDate();
            var company = rapi.CompanyEntity;
            var CountryId = company._CountryId;

            MaxDocumentRef = 0; 
            MinDocumentRef = int.MaxValue;
            foreach (var rec in lst)
            {
                rec.thisPage = this;

                var cred = (CreditorClient)CreditorCache.Get(rec.Account);
                if (cred == null)
                    continue;

                string creditorPaymId = string.Empty;
                CreditorPaymentFormat paymFormatClient = null;
                if (rec._PaymentFormat == null)
                {
                    if (cred._PaymentFormat != null)
                    {
                        rec.PaymentFormat = cred._PaymentFormat;
                    }
                    else
                    {
                        paymFormatClient = StandardPaymentFunctions.GetDefaultCreditorPaymentFormat(PaymentFormatCache);
                        if (paymFormatClient != null)
                            rec.PaymentFormat = paymFormatClient._Format;
                    }
                }

                if (paymFormatClient == null)
                    paymFormatClient = (CreditorPaymentFormat)PaymentFormatCache?.Get(rec._PaymentFormat);

                if (paymFormatClient != null && (paymFormatClient._ExportFormat == (byte)ExportFormatType.ISO20022_SE || paymFormatClient._ExportFormat == (byte)ExportFormatType.ISO20022_NO || paymFormatClient._ExportFormat == (byte)ExportFormatType.ISO20022_EE || paymFormatClient._ExportFormat == (byte)ExportFormatType.ISO20022_CH))
                {
                    if (cred != null)
                    {
                        if (rec._PaymentId == null) // no paymentId on line, we need to take both values from Creditor.
                        {
                            if (cred._PaymentMethod == PaymentTypes.VendorBankAccount && CountryId == CountryCode.Norway)
                                creditorPaymId = string.Empty; //Norway PaymentId is used for Kid-No when PaymentType=VendorBankAccount
                            else if (cred._PaymentMethod == PaymentTypes.IBAN && ((CountryId == CountryCode.Estonia && cred._Country == CountryCode.Estonia) || (CountryId == CountryCode.Switzerland && cred._Country == CountryCode.Switzerland)))
                                creditorPaymId = null; //Estonia and Switzerland PaymentId is used for Payment Reference for Domestic payments  
                            else if ((cred._PaymentMethod == PaymentTypes.PaymentMethod3 || cred._PaymentMethod == PaymentTypes.PaymentMethod5) && CountryId == CountryCode.Sweden)
                                creditorPaymId = string.Empty; //Norway PaymentId is used for OCR-No when PaymentType=PaymentMethod3   //TODO:Undersøg disse - skal stå til NULL
                            else
                                creditorPaymId = cred.PaymentId;

                            rec._PaymentMethod = cred._PaymentMethod;
                            rec._PaymentId = creditorPaymId;
                        }
                    }
                    else
                        creditorPaymId = null;

                    var transPaymentId = rec._PaymentId;

                    rec._PaymentId = (creditorPaymId != transPaymentId && transPaymentId != null) ? transPaymentId : creditorPaymId;
                }
                else
                {
                    if (cred != null)
                    {
                        if (rec._PaymentId == null) // no paymentId on line, we need to take both values from Creditor.
                        {
                            rec._PaymentMethod = cred._PaymentMethod;
                            rec._PaymentId = cred.PaymentId;
                        }

                        creditorPaymId = cred.PaymentId;
                    }
                    else
                        creditorPaymId = null;

                    var transPaymentId = rec._PaymentId;

                    if (rec._PaymentMethod == PaymentTypes.PaymentMethod3 || rec._PaymentMethod == PaymentTypes.PaymentMethod4 || rec._PaymentMethod == PaymentTypes.PaymentMethod5 || rec._PaymentMethod == PaymentTypes.PaymentMethod6)
                        BuildPaymentIdFIK(rec, transPaymentId, creditorPaymId);
                    else
                        rec._PaymentId = (creditorPaymId != transPaymentId && transPaymentId != null) ? transPaymentId : creditorPaymId;
                }

                if (rec._SWIFT == null && rec._PaymentMethod == PaymentTypes.IBAN)
                    rec._SWIFT = cred.SWIFT;

                rec._Message = StandardPaymentFunctions.ExternalMessage(paymFormatClient, rec, company, cred, true);

                if (rec._PaymentDate < today)
                {
                    var paymDate = rec.GetPayDate();
                    rec._PaymentDate = today > paymDate ? today : paymDate;
                    if (paymFormatClient != null && paymFormatClient._PaymentAction != NoneBankDayAction.None)
                        rec._PaymentDate = Uniconta.DirectDebitPayment.Common.AdjustToNextBankDay(CountryId, rec._PaymentDate, paymFormatClient._PaymentAction == NoneBankDayAction.After ? true : false);
                }

                if (rec._CashDiscount != 0d && today <= rec._CashDiscountDate)
                {
                    rec._UsedCashDiscount = rec._UsedCashDiscount != 0 ? rec._UsedCashDiscount : rec._CashDiscount;
                    rec._PaymentDate = rec._CashDiscountDate;
                    if (paymFormatClient != null && paymFormatClient._PaymentAction != NoneBankDayAction.None)
                        rec._PaymentDate = Uniconta.DirectDebitPayment.Common.AdjustToNextBankDay(CountryId, rec._PaymentDate, false);
                }

                if (rec.DocumentRef != 0)
                {
                    MinDocumentRef = Math.Min(rec.DocumentRef, MinDocumentRef);
                    MaxDocumentRef = Math.Max(rec.DocumentRef, MaxDocumentRef);
                }
            }

            dgCreditorTranOpenGrid.ItemsSource = lst;
            ClearBusy();
            dgCreditorTranOpenGrid.Visibility = Visibility.Visible;
        }

        SortDocAttached sort;
        VouchersClient search;
        internal VouchersClient GetDocumentRef(int RowId)
        {
            if (RowId == 0 || MaxDocumentRef == 0)
                return null;
            if (this.Vouchers == null)
            {
                var filter = new[] { PropValuePair.GenereteWhereElements("RowId", typeof(int), NumberConvert.ToString(MinDocumentRef) + ".." + NumberConvert.ToString(MaxDocumentRef)) };
                this.Vouchers = api.Query<VouchersClient>(filter).GetAwaiter().GetResult();
                search = new VouchersClient();
                sort = new SortDocAttached();
                Array.Sort(this.Vouchers, sort);
            }
            search.RowId = RowId;
            var pos = Array.BinarySearch(this.Vouchers, search, sort);
            if (pos >= 0 && pos < this.Vouchers.Length)
                return this.Vouchers[pos];
            return null;
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as CreditorTransPayment;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= PaymentsGrid_PropertyChanged;
            var selectedItem = e.NewItem as CreditorTransPayment;
            if (selectedItem != null)
                selectedItem.PropertyChanged += PaymentsGrid_PropertyChanged;
        }

        private void PaymentsGrid_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as CreditorTransPayment;
            switch (e.PropertyName)
            {
                case "PaymentMethod":
                    if (!api.CompanyEntity.CreditorBankApprovement)
                        return;
                    var cred = (CreditorClient)CreditorCache.Get(rec.Account);
                    if (cred != null && cred.CreditorPaymentAccountRef != null)
                    {
                        var paymFormat = (CreditorPaymentFormat)PaymentFormatCache?.Get(rec._PaymentFormat);
                        if (paymFormat == null || (paymFormat._ExportFormat == (byte)ExportFormatType.ISO20022_SE || paymFormat._ExportFormat == (byte)ExportFormatType.ISO20022_NO || paymFormat._ExportFormat == (byte)ExportFormatType.ISO20022_EE || paymFormat._ExportFormat == (byte)ExportFormatType.ISO20022_CH))
                            return;

                        var credPaymAccount = cred.CreditorPaymentAccountRef;
                        rec.SWIFT = null;
                        switch (rec._PaymentMethod)
                        {
                            case PaymentTypes.VendorBankAccount:
                                rec.PaymentId = credPaymAccount.BankAccount;
                                break;
                            case PaymentTypes.IBAN:
                                rec.PaymentId = credPaymAccount.IBAN;
                                rec.SWIFT = credPaymAccount.SWIFT;
                                break;
                            case PaymentTypes.PaymentMethod3:
                            case PaymentTypes.PaymentMethod4:
                            case PaymentTypes.PaymentMethod5:
                            case PaymentTypes.PaymentMethod6:
                                var paymentId =  string.Concat(credPaymAccount.FIKMask, " +", credPaymAccount.FICreditorNumber);
                                BuildPaymentIdFIK(rec, null, paymentId);
                                break;
                        }
                    }
                    break;
            }
        }
    }
}
