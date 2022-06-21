using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;
using Uniconta.ClientTools.Util;
using System.IO;
using System.Reflection;
using DevExpress.Xpf.Grid.Native;
using DevExpress.Xpf.Grid;
using UnicontaClient.Controls;
using Uniconta.API.DebtorCreditor;
using Uniconta.ClientTools;
using Uniconta.DirectDebitPayment;
using UnicontaClient.Pages.Creditor.Payments;
using Uniconta.API.System;
using Uniconta.Common.Utility;
using DevExpress.DataAccess.DataFederation;
#if !SILVERLIGHT
using Microsoft.Win32;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{


    public class DebtorDirectDebitGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorTransDirectDebit); } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool CanInsert { get { return false; } }

        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            var page = this.Page as DebtorDirectDebit;
            page.LoadPayments((IEnumerable<DebtorTransDirectDebit>)Arr);
        }
    }

    public partial class DebtorDirectDebit : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string FromAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Debtor))]
        public string ToAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.DebtorPaymentFormat))]
        public DebtorPaymentFormatClient xPaymentFormat { get; set; }

        public override string NameOfControl
        {
            get { return TabControls.DebtorDirectDebit; }
        }

        public override bool IsDataChaged { get { return false; } }

        public DebtorDirectDebit(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public override Task InitQuery() { return null; }

        public DebtorDirectDebit(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        SQLCache DebtorCache, JournalCache, BankAccountCache, PaymentFormatCache, InvoiceItemNameGroupCache;
        SQLTableCache<DebtorPaymentMandateClient> MandateCache;

        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgDebtorTranOpenGrid;
            dgDebtorTranOpenGrid.api = api;
            SetRibbonControl(localMenu, dgDebtorTranOpenGrid);
            dgDebtorTranOpenGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgDebtorTranOpenGrid.ShowTotalSummary();

            if (toDate == DateTime.MinValue)
                toDate = txtDateTo.DateTime.Date;

            txtDateTo.DateTime = toDate;
            txtDateFrm.DateTime = fromDate;
  
            GetMergeUnMergePaymMenuItem();
            GetExpandAndCollapseMenuItem();
            ribbonControl.DisableButtons("ExpandGroups");
            ribbonControl.DisableButtons("CollapseGroups");

            var table = dgDebtorTranOpenGrid.tableView; //
            table.ShowingEditor += Table_ShowingEditor;

            var Comp = api.CompanyEntity;

            DebtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor));
            JournalCache = Comp.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
            BankAccountCache = Comp.GetCache(typeof(Uniconta.DataModel.BankStatement));
            PaymentFormatCache = Comp.GetCache(typeof(Uniconta.DataModel.DebtorPaymentFormat));
            MandateCache = api.GetCache<DebtorPaymentMandateClient>();

            InvoiceItemNameGroupCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItemNameGroup));

            StartLoadCache();
            dgDebtorTranOpenGrid.ShowTotalSummary();
            GetShowHideStatusInfoSection();
            SetShowHideStatusInfoSection(true);
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += RootVisual_KeyDown;
#else
            this.PreviewKeyDown += RootVisual_KeyDown;
#endif
            this.BeforeClose += DebtorDirectDebit_BeforeClose;
        }

        private void DebtorDirectDebit_BeforeClose()
        {
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown -= RootVisual_KeyDown;
#else
            this.PreviewKeyDown -= RootVisual_KeyDown;
#endif
        }

        private void RootVisual_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.R && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                localMenu_OnItemClicked("StatusResend");
            }
            else if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                localMenu_OnItemClicked("StatusStopPayment");
            }
        }

        private void Table_ShowingEditor(object sender, DevExpress.Xpf.Grid.ShowingEditorEventArgs e)
        {
            if (e.Column.FieldName == "PaymentDate" || e.Column.FieldName == "PartialPaymentAmount")
            {
                var row = e.Row as DCTransOpen;

                if (row == null || row._PaymentStatus == PaymentStatusLevel.FileSent || row._PaymentStatus == PaymentStatusLevel.Processed || row._PaymentStatus == PaymentStatusLevel.PaymentReceived || row._PaymentStatus == PaymentStatusLevel.PaymentReceivedDiff)
                    e.Cancel = true;
                else
                    e.Cancel = false;

            }
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            if (DebtorCache == null)
                DebtorCache = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (PaymentFormatCache == null)
                PaymentFormatCache = await api.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat)).ConfigureAwait(false);
            if (JournalCache == null)
                JournalCache = await api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal)).ConfigureAwait(false);
            if (BankAccountCache == null)
                BankAccountCache = await api.LoadCache(typeof(Uniconta.DataModel.BankStatement)).ConfigureAwait(false);
            if (MandateCache == null)
                MandateCache = await api.LoadCache<DebtorPaymentMandateClient>().ConfigureAwait(false);

            if (InvoiceItemNameGroupCache == null)
                InvoiceItemNameGroupCache = await api.LoadCache(typeof(Uniconta.DataModel.InvItemNameGroup)).ConfigureAwait(false);
        }

        static DateTime fromDate;
        static DateTime toDate;
        protected override Filter[] DefaultFilters()
        {
            var arrFilter = new Filter[2];

            if (fromDate == DateTime.MinValue && toDate == DateTime.MinValue)
                return null;

            if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
            {
                var dateFilter = new Filter();
                dateFilter.name = "DueDate";

                string filter;
                if (fromDate != DateTime.MinValue)
                    filter = String.Format("{0:d}..", fromDate);
                else
                    filter = "..";
                if (toDate != DateTime.MinValue)
                    filter += String.Format("{0:d}", toDate);
                dateFilter.value = filter;

                arrFilter[0] = dateFilter;
            }

            return arrFilter.Where(element => element != null).Select(element => element).ToArray();
        }

        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties paymDateSort = new SortingProperties("PaymentDate");
            SortingProperties accSort = new SortingProperties("AccountNum");
            return new SortingProperties[] { paymDateSort, accSort };
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorTranOpenGrid.SelectedItem as DebtorTransDirectDebit;
            var selectedItems = dgDebtorTranOpenGrid.SelectedItems;
            switch (ActionType)
            {
                case "SaveGrid":
                    saveGrid();
                    break;
                case "Validate":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    CallValidatePayment();
                    break;
                case "MergeUnMergePaym":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    MergePaym();
                    break;
                case "ExpandGroups":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    ExpandGroups();
                    break;
                case "CollapseGroups":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    CollapseGroups();
                    break;
                case "ExportFile":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    ExportFile();
                    break;
                case "ImportFile":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    ImportFile();
                    break;
                case "StatusNone":
                    if (selectedItems == null) return;
                    ChangeStatus(PaymentStatusLevel.None, selectedItems);
                    break;
                case "StatusResend":
                    if (selectedItems == null) return;
                    ChangeStatus(PaymentStatusLevel.Resend, selectedItems);
                    break;
                case "StatusStopPayment":
                    if (selectedItems == null) return;
                    ChangeStatus(PaymentStatusLevel.StopPayment, selectedItems);
                    break;
                case "StatusOnhold":
                    if (selectedItems == null) return;
                    ChangeStatus(PaymentStatusLevel.OnHold, selectedItems);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null) return;
                    DebtorTransactions.ShowVoucher(dgDebtorTranOpenGrid.syncEntity, api, busyIndicator);
                    break;
                case "GenerateJournalLines":
                    if (dgDebtorTranOpenGrid.ItemsSource == null) return;
                    GenerateJournalLines();
                    break;
                case "Search":
                    btnSearch();
                    break;
                case "RefreshGrid":
                    if (dgDebtorTranOpenGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgDebtorTranOpenGrid);
                    btnSearch();
                    setMergeUnMergePaym(false);
                    break;
                case "Filter":
                    if (dgDebtorTranOpenGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgDebtorTranOpenGrid);

                    gridRibbon_BaseActions(ActionType);
                    setMergeUnMergePaym(false);
                    break;
                case "EnableStatusInfoSection":
                    hideStatusInfoSection = !hideStatusInfoSection;
                    SetShowHideStatusInfoSection(hideStatusInfoSection);
                    break;
                case "ShowInvoiceText":
                    if (selectedItem != null)
                        ShowInvoiceText(selectedItem);
                    break;

                case "FileArchive":
                    AddDockItem(TabControls.DebtorPaymentFileReport, null, Uniconta.ClientTools.Localization.lookup("FileArchive"));
                    break;
               default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

#if !SILVERLIGHT

        ItemBase ibase;
        bool hideStatusInfoSection = true;
        void GetShowHideStatusInfoSection()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "EnableStatusInfoSection");
        }
        private void SetShowHideStatusInfoSection(bool _hideStatusInfoSection)
        {
            if (ibase == null)
                return;
            if (_hideStatusInfoSection)
            {
                rowgridSplitter.Height = new GridLength(0);
                rowStatusInfoSection.Height = new GridLength(0);
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("StatusInfo"));
            }
            else
            {
                if (rowgridSplitter.Height.Value == 0 && rowStatusInfoSection.Height.Value == 0)
                {
                    rowgridSplitter.Height = new GridLength(2);
                    rowStatusInfoSection.Height = new GridLength(1, GridUnitType.Auto);
                }
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("StatusInfo"));
                CommentTxt.Visibility = Visibility.Visible;
                HeaderTxt.Text = Uniconta.ClientTools.Localization.lookup("StatusInfo");

                var selectedItem = dgDebtorTranOpenGrid.SelectedItem as DebtorTransDirectDebit;
            }
        }

        async Task ShowInvoiceText(DebtorTransDirectDebit trans)
        {
            var paymFormatClient = (DebtorPaymentFormat)PaymentFormatCache.Get(trans.PaymentFormat);

            if (paymFormatClient != null && paymFormatClient._ExportFormat == (byte)DebtorPaymFormatType.NetsBS && trans.Invoice != 0)
            {
                TransactionAPI tranApi = new TransactionAPI(api);
                var lstInvoiceTxt = await tranApi.CreatePaymentMessage(new List<DebtorTransDirectDebit> { trans });

                if (lstInvoiceTxt != null)
                {
                    var invText = lstInvoiceTxt[0];
                    if (invText == "InvoiceNumberMissing")
                        invText = Uniconta.ClientTools.Localization.lookup("InvoiceNumberMissing");

                    trans.TransactionText = invText;
                }
            }

            CWShowDebPaymentFileText cw = new CWShowDebPaymentFileText(trans.TransactionText);
            cw.Show();
        }

        private async void CallValidatePayment(bool useDialog = true)
        {
            if (useDialog)
            {
                var savetask = saveGrid();
                if (savetask != null)
                    await savetask;

                CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("Validate"));
                cwwin.Closing += async delegate
                {
                    if (cwwin.DialogResult == true)
                    {
                        ExpandCollapseAllGroups();

                        List<DebtorTransDirectDebit> ListDebTransPaym = new List<DebtorTransDirectDebit>();

                        var transLines = dgDebtorTranOpenGrid.GetVisibleRows() as IEnumerable<DebtorTransDirectDebit>;  

                        foreach (var rec in transLines)
                        {
                            if (rec._PaymentFormat == cwwin.PaymentFormat._Format && (rec._PaymentStatus == PaymentStatusLevel.None || rec._PaymentStatus == PaymentStatusLevel.Resend || rec._PaymentStatus == PaymentStatusLevel.StopPayment || rec._PaymentStatus == PaymentStatusLevel.FileSent))
                                ListDebTransPaym.Add(rec);
                            else
                                continue;
                        }

                        await ValidatePayments(ListDebTransPaym, doMergePaym, cwwin.PaymentFormat, true, false);
                    }
                };
                cwwin.Show();
            }
        }

        private async Task<bool> ValidatePayments(List<DebtorTransDirectDebit> ListDebTransPaym, bool paymentMerged, DebtorPaymentFormat debPaymFormat, bool validateOnly = false, bool skipPrevalidate = false, bool preMerge = false)
        {
            var paymentHelper = Common.DirectPaymentHelperInstance(debPaymFormat);
            paymentHelper.DirectDebitId = debPaymFormat._CredDirectDebitId;

            if (skipPrevalidate == false)
            {
                JournalCache = JournalCache ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal));

                List<DirectPaymentError> preValErrors;
                paymentHelper.PreValidate(api.CompanyEntity, BankAccountCache, JournalCache, debPaymFormat, out preValErrors);

                if (preValErrors.Count > 0)
                {
                    var preErrors = new List<string>();
                    foreach (DirectPaymentError error in preValErrors)
                        preErrors.Add(error.Message);
                    CWErrorBox cwError = new CWErrorBox(preErrors.ToArray());
                    cwError.Show();
                    return false;
                }
            }

            //Append invisible rows and rows with status Sent, Processed and Information >>
            var transValidation = new List<DebtorTransDirectDebit>();
            int index = 0;
            foreach (var rec in (IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.ItemsSource)
            {
                int rowHandle = dgDebtorTranOpenGrid.GetRowHandleByListIndex(index);
                if (rec._PaymentFormat == debPaymFormat._Format && (rec._PaymentStatus == PaymentStatusLevel.FileSent || rec._PaymentStatus == PaymentStatusLevel.Processed || rec._PaymentStatus == PaymentStatusLevel.Information))
                {
                    if (dgDebtorTranOpenGrid.IsRowVisible(rowHandle) == false)
                        rec.invisibleRow = true;
                    transValidation.Add(rec);
                }

                index++;
            }
            if (transValidation.Count != 0)
                ListDebTransPaym.AddRange(transValidation);
            //Append invisible rows and rows with status Sent, Processed and Information  <<

            await Common.CreateMandates(api, ListDebTransPaym, debPaymFormat, DebtorCache, MandateCache);

            var valErrors = new List<DirectPaymentError>();
            paymentHelper.ValidatePayments(api.CompanyEntity, ListDebTransPaym, debPaymFormat, MandateCache, paymentMerged, preMerge, out valErrors);

            foreach (var rec in ListDebTransPaym)
            {
                if (rec._PaymentStatus == PaymentStatusLevel.FileSent || rec._PaymentStatus == PaymentStatusLevel.Processed || rec._PaymentStatus == PaymentStatusLevel.Information || rec.invisibleRow)
                    continue;

                if (rec.PrimaryKeyId == 0)
                    rec.ErrorInfo = string.Format(Uniconta.ClientTools.Localization.lookup("ActionNotAllowedObj"), Uniconta.ClientTools.Localization.lookup("BalanceMethod"), Uniconta.ClientTools.Localization.lookup("DirectDebit"));
                else
                    rec.ErrorInfo = Common.VALIDATE_OK;
            }

            int countErr = 0;
            foreach (DirectPaymentError error in valErrors)
            {
                var rec = ListDebTransPaym.FirstOrDefault(s => s.PrimaryKeyId == error.RowId && s.invisibleRow == false); 
                if (rec == null)
                    continue;

                if (rec.ErrorInfo == Common.VALIDATE_OK)
                {
                    countErr++; 
                    rec.ErrorInfo = error.Message;
                }
                else
                    rec.ErrorInfo += Environment.NewLine + error.Message;
            }

            if (ListDebTransPaym.FirstOrDefault(s => s.ErrorInfo == Common.VALIDATE_OK) == null && validateOnly == false) 
            {
                if (doMergePaym == false)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Message"));
                return false;
            }

            if (validateOnly)
            {
                if (countErr == 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateNoError"),  Uniconta.ClientTools.Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    UnicontaMessageBox.Show(string.Format("{0} {1}", countErr, Uniconta.ClientTools.Localization.lookup("JournalFailedValidation")), Uniconta.ClientTools.Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return true;
        }
#endif

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

            dgDebtorTranOpenGrid.ExpandAllGroups();
        }

        private void CollapseGroups()
        {
            if (ibaseExpandGroups == null)
                return;

            dgDebtorTranOpenGrid.CollapseAllGroups();
        }


        private void MergePaym()
        {
            doMergePaym = !doMergePaym;
            if (doMergePaym)
            {
                CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("MergePayments"));

                cwwin.Closing += async delegate
                {
                    if (cwwin.DialogResult == true)
                    {
                        var debPaymentFormat = cwwin.PaymentFormat;

                        ExpandCollapseAllGroups();
                        List<DebtorTransDirectDebit> ListDebTransPaym = new List<DebtorTransDirectDebit>();
                        int index = 0;
                        foreach (var rec in (IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.ItemsSource)
                        {
                            rec.ErrorInfo = string.Empty;
                            int rowHandle = dgDebtorTranOpenGrid.GetRowHandleByListIndex(index);
                            index++;
                            if (dgDebtorTranOpenGrid.IsRowVisible(rowHandle) && rec._PaymentFormat == debPaymentFormat._Format && (rec._PaymentStatus == PaymentStatusLevel.None || rec._PaymentStatus == PaymentStatusLevel.Resend || rec._PaymentStatus == PaymentStatusLevel.StopPayment))
                                ListDebTransPaym.Add(rec);
                            else
                            {
                                rec.MergeDataId = Uniconta.ClientTools.Localization.lookup("Excluded");
                                continue;
                            }
                        }

                        Common.MergePayment(api.CompanyEntity, ListDebTransPaym);

                        await ValidatePayments(ListDebTransPaym, false, debPaymentFormat, false, false, true);

                        setMergeUnMergePaym(doMergePaym);
                    }
                    else
                    {
                        doMergePaym = !doMergePaym;
                        return;
                    }
                };
                cwwin.Show();
            }
            else
            {
                setMergeUnMergePaym(doMergePaym);
            }
        }

        private async void ExportFile()
        {
            var savetask = saveGrid();
            if (savetask != null)
                await savetask;

            CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("Export"));
           
            cwwin.Closing += async delegate
            {
                if (cwwin.DialogResult != true)
                    return;

                var debPaymentFormat = cwwin.PaymentFormat;

                ExpandCollapseAllGroups();
                List<DebtorTransDirectDebit> ListDebTransPaym = new List<DebtorTransDirectDebit>();
                var transLines = dgDebtorTranOpenGrid.GetVisibleRows() as IEnumerable<DebtorTransDirectDebit>;  
                foreach (var rec in transLines)
                {
                    if (rec._PaymentFormat == debPaymentFormat._Format && (rec._PaymentStatus == PaymentStatusLevel.None || rec._PaymentStatus == PaymentStatusLevel.Resend || rec._PaymentStatus == PaymentStatusLevel.StopPayment))
                        ListDebTransPaym.Add(rec);
                    else
                        continue;
                }

                if (ListDebTransPaym.Count == 0)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }

                if (await ValidatePayments(ListDebTransPaym, doMergePaym, debPaymentFormat, false, false))
                {

                    try
                    {
                        bool  netsBS = debPaymentFormat._ExportFormat == (byte)DebtorPaymFormatType.NetsBS;

                        var debTransPaymSelectedLst = ListDebTransPaym.Where(s => s.ErrorInfo == Common.VALIDATE_OK);
                       
                        TransactionAPI tranApi = new TransactionAPI(api);
                        ErrorCodes err;
                        if (netsBS)
                            err = await tranApi.CreatePaymentFilePBS(debTransPaymSelectedLst, debPaymentFormat, doMergePaym);
                        else
                        {
                            bool stopPaymentTrans = false;
                            List<DebtorTransDirectDebit> paymentListMerged = null;
                            var dictPaym = new Dictionary<String, DebtorTransDirectDebit>();
                            DebtorTransDirectDebit mergePayment;

                            var uniqueEndToEndId = (int)await tranApi.GetPaymentId(debTransPaymSelectedLst.Count());
                            var uniqueFileId = uniqueEndToEndId;

                            foreach (var rec in debTransPaymSelectedLst)
                            {
                                stopPaymentTrans = rec._PaymentStatus == PaymentStatusLevel.StopPayment;

                                if (doMergePaym || stopPaymentTrans)
                                {
                                    var keyValue = stopPaymentTrans ? NumberConvert.ToString(rec.PaymentRefId) : rec.MergeDataId;

                                    if (dictPaym.TryGetValue(keyValue, out mergePayment))
                                    {
                                        mergePayment.MergedAmount += rec.PaymentAmount;
                                        mergePayment.hasBeenMerged = true;
                                        mergePayment.invoiceNumbers.Append(',').Append(rec.Invoice);

                                        if (rec.TransactionText != null)
                                            mergePayment.TransactionText += mergePayment.TransactionText != null ? (Environment.NewLine + Environment.NewLine + Environment.NewLine + rec.TransactionText) : rec.TransactionText;

                                        if (stopPaymentTrans)
                                        {
                                            rec.MergeDataId = NumberConvert.ToString(rec.PaymentRefId);
                                            rec.PaymentRefId = rec.PaymentRefId;
                                        }
                                        else
                                        {
                                            if (keyValue == Common.MERGEID_SINGLEPAYMENT)
                                            {
                                                uniqueEndToEndId++;
                                                rec.PaymentRefId = uniqueEndToEndId;
                                            }
                                            else
                                                rec.PaymentRefId = mergePayment.PaymentRefId;
                                        }
                                    }
                                    else
                                    {
                                        if (!stopPaymentTrans)
                                        {
                                            uniqueEndToEndId++;
                                            rec.PaymentRefId = uniqueEndToEndId;
                                        }

                                        mergePayment = new DebtorTransDirectDebit();
                                        StreamingManager.Copy(rec, mergePayment);
                                        mergePayment.MergeDataId = keyValue;
                                        mergePayment.PaymentRefId = rec.PaymentRefId;
                                        mergePayment.MergedAmount = rec.PaymentAmount;
                                        mergePayment.TransactionText = rec.TransactionText;
                                        mergePayment.invoiceNumbers = new StringBuilder();
                                        mergePayment.invoiceNumbers.Append(rec.Invoice);
                                        mergePayment.ErrorInfo = rec.ErrorInfo;

                                        dictPaym.Add(keyValue, mergePayment);
                                    }
                                }
                                else
                                {
                                    uniqueEndToEndId++;
                                    rec.MergedAmount = 0;
                                    rec.PaymentRefId = uniqueEndToEndId;
                                }
                            }

                            paymentListMerged = dictPaym.Values.ToList();

                            List<DebtorTransDirectDebit> paymentList = null;
                            if (doMergePaym)
                            {
                                paymentList = paymentListMerged.Where(s => s.MergeDataId != Common.MERGEID_SINGLEPAYMENT && s._PaymentStatus != PaymentStatusLevel.StopPayment).ToList();
                                foreach (var rec in debTransPaymSelectedLst)
                                {
                                    if (rec.MergeDataId == Common.MERGEID_SINGLEPAYMENT && rec._PaymentStatus != PaymentStatusLevel.StopPayment)
                                        paymentList.Add(rec);
                                }
                            }
                            else
                            {
                                paymentList = debTransPaymSelectedLst.Where(s => s.ErrorInfo == Common.VALIDATE_OK && s._PaymentStatus != PaymentStatusLevel.StopPayment).ToList();
                            }

                            if (paymentListMerged != null)
                            {
                                foreach (var rec in paymentListMerged)
                                {
                                    if (rec._PaymentStatus == PaymentStatusLevel.StopPayment)
                                        paymentList.Add(rec);
                                }
                            }

                            err = await Common.CreatePaymentFile(api, paymentList, debTransPaymSelectedLst, MandateCache, DebtorCache, uniqueFileId, debPaymentFormat, BankAccountCache);
                        }
                        if (err != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(err);
                        else if (netsBS)
                        {
                            ribbonControl.DisableButtons("ExportFile");
                            UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("SendEmailMsgOBJ"), 
                                Uniconta.ClientTools.Localization.lookup("DirectDebitCollection")) + Environment.NewLine + Uniconta.ClientTools.Localization.lookup("UpdateInBackground"), Uniconta.ClientTools.Localization.lookup("Message"));
                        }
                    }
                    catch (Exception ex)
                    {
                        UnicontaMessageBox.Show(ex);
                    }
                }
            };
            cwwin.Show();
        }

        private async void ImportFile()
        {
            busyIndicator.IsBusy = true;

            List<DebtorTransDirectDebit> listDebTransPaym = new List<DebtorTransDirectDebit>();
            int index = 0;
            foreach (var rec in (IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.ItemsSource)
            {
                int rowHandle = dgDebtorTranOpenGrid.GetRowHandleByListIndex(index);
                index++;
                listDebTransPaym.Add(rec);
            }

            var pairFile = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements(nameof(DebtorPaymentFileClient._Output), typeof(int), 0),
                PropValuePair.GenereteWhereElements(nameof(DebtorPaymentFileClient.IsOk), typeof(int), 0),
            };

            var debtorPaymFile = await api.Query<DebtorPaymentFileClient>(pairFile);
            var dialogInfo = await Common.GetReturnFiles(api, listDebTransPaym, DebtorCache, MandateCache, PaymentFormatCache, debtorPaymFile);

            busyIndicator.IsBusy = false;

            UnicontaMessageBox.Show(dialogInfo, Uniconta.ClientTools.Localization.lookup("Message"));
        }


        ItemBase ibaseMergePaym;
        bool doMergePaym = false;
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
                ibaseMergePaym.Caption = Uniconta.ClientTools.Localization.lookup("UnmergePayments");
                ibaseMergePaym.LargeGlyph = Utility.GetGlyph("Match_Remove_32x32.png");

                dgDebtorTranOpenGrid.GroupBy("MergeDataId");
                dgDebtorTranOpenGrid.Columns.GetColumnByName("PaymentDate").AllowFocus = false;
                dgDebtorTranOpenGrid.Columns.GetColumnByName("AllowReplacement").AllowFocus = false;

                ribbonControl.EnableButtons("ExpandGroups");
                ribbonControl.EnableButtons("CollapseGroups");
            }
            else
            {
                ibaseMergePaym.Caption = Uniconta.ClientTools.Localization.lookup("MergePayments");
                ibaseMergePaym.LargeGlyph = Utility.GetGlyph("Match_Add_32x32.png");

                dgDebtorTranOpenGrid.UngroupBy("MergeDataId");
                dgDebtorTranOpenGrid.Columns.GetColumnByName("PaymentDate").AllowFocus = true;
                dgDebtorTranOpenGrid.Columns.GetColumnByName("AllowReplacement").AllowFocus = true;
                dgDebtorTranOpenGrid.Columns.GetColumnByName("MergeDataId").Visible = false;

                ribbonControl.DisableButtons("ExpandGroups");
                ribbonControl.DisableButtons("CollapseGroups");
            }
        }

        void ExpandCollapseAllGroups(bool ExpandAll = true)
        {
            if (doMergePaym)
            {
                if (ExpandAll)
                    dgDebtorTranOpenGrid.ExpandAllGroups();
                else
                    dgDebtorTranOpenGrid.CollapseAllGroups();
            }
        }

        void ChangeStatus(PaymentStatusLevel changeToStatus, IList lstTransPaym)
        {
            ExpandCollapseAllGroups();

            List<DebtorTransDirectDebit> ListDebTransPaymStop = new List<DebtorTransDirectDebit>();

            if (changeToStatus == PaymentStatusLevel.StopPayment)
            {
                foreach (var rec in (IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.GetVisibleRows())
                {
                    if (rec._PaymentStatus == PaymentStatusLevel.StopPayment || rec._PaymentStatus == PaymentStatusLevel.OnHold || rec._PaymentStatus == PaymentStatusLevel.None || rec._PaymentStatus == PaymentStatusLevel.PaymentReceived || rec._PaymentStatus == PaymentStatusLevel.PaymentReceivedDiff)
                        continue;
                    else
                        ListDebTransPaymStop.Add(rec);
                }
            }
            Common.ChangeStatus(api, lstTransPaym, changeToStatus, PaymentFormatCache, ListDebTransPaymStop);
        }
      
        private void GenerateJournalLines()
        {
            DirectDebitToJournal cwwin = new DirectDebitToJournal(api); 

            cwwin.Closed += async delegate
            {
                if (cwwin.DialogResult == true && !string.IsNullOrEmpty(cwwin.Journal))
                {
                    ExpandCollapseAllGroups();

                    busyIndicator.IsBusy = true;
                    Uniconta.API.GeneralLedger.PostingAPI posApi = new Uniconta.API.GeneralLedger.PostingAPI(api);
                    long LineNumber = await posApi.MaxLineNumber(cwwin.Journal);

                    NumberSerieAPI numberserieApi = new NumberSerieAPI(posApi);
                    int nextVoucherNumber = 0;

                    var DJclient = (Uniconta.DataModel.GLDailyJournal)JournalCache.Get(cwwin.Journal);
                    var listLineClient = new List<Uniconta.DataModel.GLDailyJournalLine>();

                    if (!DJclient._GenerateVoucher && !DJclient._ManualAllocation)
                        nextVoucherNumber = (int)await numberserieApi.ViewNextNumber(DJclient._NumberSerie);

                    List<DebtorTransDirectDebit> paymentListTransfer = null;
                    var dictPaymTransfer = new Dictionary<long, DebtorTransDirectDebit>();
                    DebtorTransDirectDebit mergePaymentRefId;

                    var netsBS = false;
                    var bsPrincipRemaining = false;
                    int index = 0;
                    foreach (var rec in (IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.ItemsSource)
                    {
                        int rowHandle = dgDebtorTranOpenGrid.GetRowHandleByListIndex(index);
                        index++;
                        if (!dgDebtorTranOpenGrid.IsRowVisible(rowHandle) || rec._PaymentStatus == PaymentStatusLevel.OnHold) 
                            continue;

                        if (rec.PaymentFormat != null)
                        {
                            var paymFormatClient = (DebtorPaymentFormat)PaymentFormatCache.Get(rec.PaymentFormat);
                            netsBS = paymFormatClient._ExportFormat == (byte)DebtorPaymFormatType.NetsBS;
                            if (netsBS)
                            {
                                DebtorPaymentFormatClientNets paymentFormatNets = new DebtorPaymentFormatClientNets();
                                StreamingManager.Copy(paymFormatClient, paymentFormatNets);
                                bsPrincipRemaining = paymentFormatNets._CollectionPrinciple == 1;
                            }
                        }

                        var paymRefId = rec._PaymentRefId != 0 ? rec._PaymentRefId : -rec.PrimaryKeyId;

                        double bsAmount = 0;
                        if (netsBS)
                            bsAmount = bsPrincipRemaining ? rec.AmountOpen : rec.Amount;

                        if (dictPaymTransfer.TryGetValue(paymRefId, out mergePaymentRefId))
                        {
                            mergePaymentRefId.MergedAmount += netsBS ? bsAmount : rec.PaymentAmount;
                            mergePaymentRefId.UsedCachDiscount += rec.UsedCachDiscount;
                            mergePaymentRefId.settleTypeRowId = true;
                            mergePaymentRefId.rowNumbers.Append(';').AppendNum(rec.PrimaryKeyId);
                            mergePaymentRefId.hasBeenMerged = true;
                        }
                        else
                        {
                            mergePaymentRefId = new DebtorTransDirectDebit();
                            StreamingManager.Copy(rec, mergePaymentRefId);
                            mergePaymentRefId._PaymentRefId = paymRefId;
                            mergePaymentRefId.MergedAmount = netsBS ? bsAmount : rec.PaymentAmount;
                            mergePaymentRefId.rowNumbers = new StringBuilder();
                            mergePaymentRefId.rowNumbers.AppendNum(rec.PrimaryKeyId);
                            mergePaymentRefId.settleTypeRowId = true;
                            mergePaymentRefId.UsedCachDiscount = rec.UsedCachDiscount;
                            dictPaymTransfer.Add(paymRefId, mergePaymentRefId);
                        }
                        paymentListTransfer = dictPaymTransfer.Values.ToList();
                    }

                    foreach (var cTOpenClient in paymentListTransfer)
                    {
                        var debtor = cTOpenClient.Debtor;

                        var lineclient = new GLDailyJournalLineClient();
                        lineclient.SetMaster(DJclient);
                        lineclient._DCPostType = DCPostType.Payment;
                        lineclient._LineNumber = ++LineNumber;
                        lineclient._Date = cTOpenClient._PaymentDate != DateTime.MinValue ? cTOpenClient._PaymentDate : cTOpenClient._DueDate;
                        lineclient._TransType = cwwin.TransType;
                        lineclient._AccountType = (byte)GLJournalAccountType.Debtor;
                        lineclient._Account = cTOpenClient.Account;
                        lineclient._OffsetAccount = cwwin.BankAccount;
                        lineclient._Invoice = cTOpenClient.hasBeenMerged ? null : cTOpenClient.InvoiceAN;
                        lineclient._Dim1 = debtor._Dim1;
                        lineclient._Dim2 = debtor._Dim2;
                        lineclient._Dim3 = debtor._Dim3;
                        lineclient._Dim4 = debtor._Dim4;
                        lineclient._Dim5 = debtor._Dim5;

                        if (cTOpenClient.settleTypeRowId)
                        {
                            lineclient._SettleValue = SettleValueType.RowId;
                            lineclient._Settlements = cTOpenClient.rowNumbers.ToString();
                        }
                        else
                        {
                            lineclient._Settlements = null;
                        }
                
                        lineclient._DocumentRef = cTOpenClient.DocumentRef; 
                        lineclient.Amount = -cTOpenClient.MergedAmount;
                        lineclient.UsedCachDiscount = cTOpenClient.UsedCachDiscount;

                        if (nextVoucherNumber != 0)
                        {
                            lineclient._Voucher = nextVoucherNumber;
                            nextVoucherNumber++;
                        }

                        listLineClient.Add(lineclient);
                    }
                    if (listLineClient.Count > 0)
                    {
                        if (nextVoucherNumber != 0)
                            numberserieApi.SetNumber(DJclient._NumberSerie, nextVoucherNumber-1);

                        ErrorCodes errorCode = await api.Insert(listLineClient);
                        busyIndicator.IsBusy = false;
                        if (errorCode != ErrorCodes.Succes)
                            UtilDisplay.ShowErrorCode(errorCode);
                        else
                        {
                            var text = string.Concat(Uniconta.ClientTools.Localization.lookup("TransferedToJournal"), ": ", DJclient._Journal,
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
            cwwin.Show();
        }

        void LoadPayments() { LoadPayments((IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.ItemsSource); }
        internal async void LoadPayments(IEnumerable<DebtorTransDirectDebit> lst)
        {
            if (lst == null)
                return;

            var transDD = new List<DebtorTransDirectDebit>();

            var today = BasePage.GetSystemDefaultDate();
            var company = api.CompanyEntity;

            if (PaymentFormatCache == null)
                PaymentFormatCache = await api.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentFormat));

            foreach (var rec in lst)
            {
                var debtor = (Debtor)DebtorCache.Get(rec.Account);
                if (debtor == null)
                    continue;

                DebtorPaymentFormat paymFormatClient = null;

                if (rec.PaymentFormat == null && debtor._PaymentFormat != null)
                    rec.PaymentFormat = debtor._PaymentFormat;

                if (rec.PaymentFormat != null)
                    paymFormatClient = (DebtorPaymentFormat)PaymentFormatCache.Get(rec.PaymentFormat);

                if (rec.PaymentDate < today && rec._PaymentStatus == PaymentStatusLevel.None)
                {
                    var paymDate = rec._PaymentDate == DateTime.MinValue ? rec._DueDate : rec._PaymentDate;
                    rec.PaymentDate = today > paymDate ? today : paymDate;

                    if (paymFormatClient != null && paymFormatClient._PaymentAction != NoneBankDayAction.None)
                        rec.PaymentDate = Common.AdjustToNextBankDay(company._CountryId, rec._PaymentDate, paymFormatClient._PaymentAction == NoneBankDayAction.After ? true : false);
                }

                if (paymFormatClient != null && paymFormatClient._ExportFormat == (byte)DebtorPaymFormatType.SEPA)
                {
                    rec._Message = Common.MessageFormat(paymFormatClient._Message ?? string.Empty, rec, company);

                    if (rec._CashDiscount != 0d && today <= rec.CashDiscountDate)
                    {
                        rec._UsedCachDiscount = rec._CashDiscount;

                        if (rec.Currency != null)
                            rec._PartialPaymentAmount = rec._AmountOpenCur - Math.Abs(rec._UsedCachDiscount);
                        else
                            rec._PartialPaymentAmount = rec._AmountOpen - Math.Abs(rec._UsedCachDiscount);

                        rec._PaymentDate = rec.CashDiscountDate;

                        if (paymFormatClient != null && paymFormatClient._PaymentAction != NoneBankDayAction.None)
                            rec._PaymentDate = Uniconta.DirectDebitPayment.Common.AdjustToNextBankDay(company._CountryId, rec._PaymentDate, false);
                    }
                }
            }
        }


        public override bool HandledOnClearFilter()
        {
            setMergeUnMergePaym(false);
            return true;
        }

        private void btnSearch()
        {
            if (txtDateFrm.Text == string.Empty)
                fromDate = DateTime.MinValue;
            else
                fromDate = txtDateFrm.DateTime.Date;

            if (txtDateTo.Text == string.Empty)
                toDate = DateTime.MinValue;
            else
                toDate = txtDateTo.DateTime.Date;

            Filter();
        }
    }
}


