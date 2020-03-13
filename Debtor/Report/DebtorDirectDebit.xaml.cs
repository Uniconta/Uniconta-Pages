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
            get { return TabControls.DebtorPayments.ToString(); }
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

        SQLCache DebtorCache, JournalCache, BankAccountCache, PaymentFormatCache, MandateCache, InvoiceItemNameGroupCache;

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
            MandateCache = Comp.GetCache(typeof(Uniconta.DataModel.DebtorPaymentMandate));
            InvoiceItemNameGroupCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItemNameGroup));

            StartLoadCache();
            dgDebtorTranOpenGrid.ShowTotalSummary();
            GetShowHideStatusInfoSection();
            GetShowHideTextSection();
            SetShowHideStatusInfoSection(true);
            SetShowHideTextSection();
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
                MandateCache = await api.LoadCache(typeof(Uniconta.DataModel.DebtorPaymentMandate)).ConfigureAwait(false);
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
                    dgDebtorTranOpenGrid.SelectedItem = null;
                    dgDebtorTranOpenGrid.SaveData();
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
                case "EnableTextSection":
                    hideTextSection = !hideTextSection;
                    SetShowHideTextSection();
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
        bool hideTextSection = true;
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
                hideTextSection = true;
            }
            else
            {
                if (rowgridSplitter.Height.Value == 0 && rowStatusInfoSection.Height.Value == 0)
                {
                    rowgridSplitter.Height = new GridLength(2);
                    rowStatusInfoSection.Height = new GridLength(1, GridUnitType.Auto);
                }
                hideTextSection = true;
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("StatusInfo"));
                textBase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceText"));
                CommentTxt.Visibility = Visibility.Visible;
                HeaderTxt.Text = Uniconta.ClientTools.Localization.lookup("StatusInfo");
                NetsBSText.Visibility = Visibility.Hidden;

                var selectedItem = dgDebtorTranOpenGrid.SelectedItem as DebtorTransDirectDebit;
            }
        }

        ItemBase textBase;

        void GetShowHideTextSection()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            textBase = UtilDisplay.GetMenuCommandByName(rb, "EnableTextSection");
        }

        private void SetShowHideTextSection()
        {
            if (textBase == null)
                return;
            if (hideTextSection)
            {
                rowgridSplitter.Height = new GridLength(0);
                rowStatusInfoSection.Height = new GridLength(0);
                textBase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceText"));
                hideStatusInfoSection = true;
            }
            else
            {
                if (rowgridSplitter.Height.Value == 0 && rowStatusInfoSection.Height.Value == 0)
                {
                    rowgridSplitter.Height = new GridLength(2);
                    rowStatusInfoSection.Height = new GridLength(1, GridUnitType.Auto);
                }

                CommentTxt.Visibility = Visibility.Hidden;
                HeaderTxt.Text = Uniconta.ClientTools.Localization.lookup("InvoiceText");
                NetsBSText.Visibility = Visibility.Visible;
                textBase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("InvoiceText"));
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("StatusInfo"));
                hideStatusInfoSection = true;

                var selectedItem = dgDebtorTranOpenGrid.SelectedItem as DebtorTransDirectDebit;
            }
        }

        private void CallValidatePayment(bool useDialog = true)
        {
            if (useDialog)
            {
                CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("Validate"));
                cwwin.Closing += delegate
                {
                    if (cwwin.DialogResult == true)
                    {
                        ExpandCollapseAllGroups();

                        List<DebtorTransDirectDebit> ListDebTransPaym = new List<DebtorTransDirectDebit>();
                        int index = 0;
                        foreach (var rec in (IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.ItemsSource)
                        {
                            int rowHandle = dgDebtorTranOpenGrid.GetRowHandleByListIndex(index);
                            index++;
                            if (dgDebtorTranOpenGrid.IsRowVisible(rowHandle) && rec._PaymentFormat == cwwin.PaymentFormat._Format && (rec._PaymentStatus == PaymentStatusLevel.None || rec._PaymentStatus == PaymentStatusLevel.Resend || rec._PaymentStatus == PaymentStatusLevel.StopPayment || rec._PaymentStatus == PaymentStatusLevel.FileSent))
                                ListDebTransPaym.Add(rec);
                            else
                                continue;
                        }

                        IEnumerable<DebtorTransDirectDebit> queryPaymentTrans = ListDebTransPaym.AsEnumerable();

                        ValidatePayments(queryPaymentTrans, doMergePaym, cwwin.PaymentFormat, true, false);
                    }
                };
                cwwin.Show();
            }
        }

        private bool ValidatePayments(IEnumerable<DebtorTransDirectDebit> queryPaymentTrans, bool paymentMerged, DebtorPaymentFormat debPaymFormat, bool validateOnly = false, bool skipPrevalidate = false, bool preMerge = false)
        {
            var paymentHelper = Common.DirectPaymentHelperInstance(debPaymFormat);
            paymentHelper.DirectDebitId = debPaymFormat._CredDirectDebitId;

            if (skipPrevalidate == false)
            {
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

            var valErrors = new List<DirectPaymentError>();
            paymentHelper.ValidatePayments(api.CompanyEntity, queryPaymentTrans, debPaymFormat, MandateCache, paymentMerged, preMerge, out valErrors);

            foreach (var rec in queryPaymentTrans.Where(s => s._PaymentStatus != PaymentStatusLevel.FileSent))
            {
                rec.ErrorInfo = Common.VALIDATE_OK;
            }

            foreach (DirectPaymentError error in valErrors)
            {
                var rec = queryPaymentTrans.Where(s => s.PrimaryKeyId == error.RowId).First();
                if (rec.ErrorInfo == Common.VALIDATE_OK)
                    rec.ErrorInfo = error.Message;
                else
                    rec.ErrorInfo += Environment.NewLine + error.Message;
            }

            if (queryPaymentTrans.Where(s => s.ErrorInfo == Common.VALIDATE_OK).Any() == false && validateOnly == false)
            {
                if (doMergePaym == false)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Message"));
                return false;
            }

            if (validateOnly)
            {
                var countErr = queryPaymentTrans.Where(s => (s.ErrorInfo != Common.VALIDATE_OK) && (s.ErrorInfo != null)).Count();

                if (countErr == 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateNoError"),  Uniconta.ClientTools.Localization.lookup("Validation"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ValidateFailInLines"), countErr), Uniconta.ClientTools.Localization.lookup("Validation"), MessageBoxButton.OK, MessageBoxImage.Warning);
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

                cwwin.Closing += delegate
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

                        IEnumerable<DebtorTransDirectDebit> queryPaymentTrans = ListDebTransPaym.AsEnumerable();

                        ValidatePayments(queryPaymentTrans, false, debPaymentFormat, false, false, true);

                        if (Common.MergePayment(api.CompanyEntity, queryPaymentTrans))
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

        //TODO:Overvej at flytte nedenstående metode til Common
        private int nextPaymentFileId;
        private int nextPaymentRefId;
        private async void GetNextPaymentFileId()
        {
            nextPaymentFileId = 1;
            nextPaymentRefId = 1;

            var queryDebtorPaymRef = await api.Query<DebtorPaymentReference>();
            if (queryDebtorPaymRef.Length > 0)
            {
                nextPaymentFileId = queryDebtorPaymRef.Max(s => s._PaymentFileId) + 1;
                nextPaymentRefId = queryDebtorPaymRef.Max(s => s._PaymentRefId);
            }
        }

        private void ExportFile()
        {
            GetNextPaymentFileId();
            
            CWDirectDebit cwwin = new CWDirectDebit(api, Uniconta.ClientTools.Localization.lookup("Export"));
           
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
                        int rowHandle = dgDebtorTranOpenGrid.GetRowHandleByListIndex(index);
                        index++;
                        if (dgDebtorTranOpenGrid.IsRowVisible(rowHandle) && rec._PaymentFormat == debPaymentFormat._Format && (rec._PaymentStatus == PaymentStatusLevel.None || rec._PaymentStatus == PaymentStatusLevel.Resend || rec._PaymentStatus == PaymentStatusLevel.StopPayment))
                            ListDebTransPaym.Add(rec);
                        else
                            continue;
                    }

                    if (ListDebTransPaym.Count == 0)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Warning"));
                        return;
                    }

                    IEnumerable<DebtorTransDirectDebit> queryPaymentTrans = ListDebTransPaym.AsEnumerable();
                    if (ValidatePayments(queryPaymentTrans, doMergePaym, debPaymentFormat, false, false))
                    {
                        try
                        {
                            List<DebtorTransDirectDebit> paymentListMerged = null;
                            var dictPaym = new Dictionary<String, DebtorTransDirectDebit>();
                            DebtorTransDirectDebit mergePayment;

                            var paymNumSeqRefId = nextPaymentRefId;
                            bool stopPaymentTrans = false;
                            bool netsBS = false;

                            if (doMergePaym)
                                queryPaymentTrans = queryPaymentTrans.OrderBy(x => x.MergeDataId).OrderBy(x => x.Invoice).ToList();

                            foreach (var rec in queryPaymentTrans.Where(s => s.ErrorInfo == Common.VALIDATE_OK))
                            {
                                stopPaymentTrans = rec._PaymentStatus == PaymentStatusLevel.StopPayment;
                                netsBS = debPaymentFormat._ExportFormat == (byte)DebtorPaymFormatType.NetsBS;

                                if (doMergePaym || stopPaymentTrans)
                                {
                                    var keyValue = stopPaymentTrans ? NumberConvert.ToString(rec.PaymentRefId) : rec.MergeDataId;

                                    if (dictPaym.TryGetValue(keyValue, out mergePayment))
                                    {
                                        mergePayment.MergedAmount += netsBS ? rec.Amount : rec.PaymentAmount;
                                        mergePayment.hasBeenMerged = true;
                                        mergePayment.invoiceNumbers.Append(',').Append(rec.Invoice);

                                        if (rec.TransactionText != null)
                                        {
                                            mergePayment.TransactionText += mergePayment.TransactionText != null ? (Environment.NewLine + Environment.NewLine + Environment.NewLine + rec.TransactionText) : rec.TransactionText;
                                        }

                                        if (stopPaymentTrans)
                                        {
                                            rec.MergeDataId = NumberConvert.ToString(rec.PaymentRefId);
                                            rec.PaymentRefId = rec.PaymentRefId;
                                        }
                                        else
                                        {
                                            if (keyValue == Common.MERGEID_SINGLEPAYMENT)
                                            {
                                                paymNumSeqRefId++;
                                                rec.PaymentRefId = paymNumSeqRefId;
                                            }
                                            else
                                                rec.PaymentRefId = mergePayment.PaymentRefId;
                                        }
                                    }
                                    else
                                    {
                                        if (!stopPaymentTrans)
                                        {
                                            paymNumSeqRefId++;
                                            rec.PaymentRefId = paymNumSeqRefId;
                                        }

                                        mergePayment = new DebtorTransDirectDebit();
                                        StreamingManager.Copy(rec, mergePayment);
                                        mergePayment.MergeDataId = keyValue;
                                        mergePayment.PaymentRefId = rec.PaymentRefId;
                                        mergePayment.MergedAmount = netsBS ? rec.Amount : rec.PaymentAmount;
                                        mergePayment.TransactionText = rec.TransactionText;
                                        mergePayment.invoiceNumbers = new StringBuilder();
                                        mergePayment.invoiceNumbers.Append(rec.Invoice);
                                        mergePayment.ErrorInfo = rec.ErrorInfo;

                                        dictPaym.Add(keyValue, mergePayment);
                                    }
                                    paymentListMerged = dictPaym.Values.ToList();
                                }
                                else
                                {
                                    paymNumSeqRefId++;
                                    rec.MergedAmount = 0;
                                    rec.PaymentRefId = paymNumSeqRefId;
                                }
                            }

                            List<DebtorTransDirectDebit> paymentList = null;
                            if (doMergePaym)
                            {
                                paymentList = paymentListMerged.Where(s => s.MergeDataId != Uniconta.ClientTools.Localization.lookup("Excluded") && s.MergeDataId != Common.MERGEID_SINGLEPAYMENT && s._PaymentStatus != PaymentStatusLevel.StopPayment).ToList();
                                foreach (var rec in queryPaymentTrans)
                                {
                                    if (rec.MergeDataId == Common.MERGEID_SINGLEPAYMENT && rec._PaymentStatus != PaymentStatusLevel.StopPayment)
                                        paymentList.Add(rec);
                                }
                            }
                            else
                            {
                                paymentList = queryPaymentTrans.Where(s => s.ErrorInfo == Common.VALIDATE_OK && s._PaymentStatus != PaymentStatusLevel.StopPayment).ToList();
                            }

                            if (paymentListMerged != null)
                            {
                                foreach (var rec in paymentListMerged)
                                {
                                    if (rec._PaymentStatus == PaymentStatusLevel.StopPayment)
                                        paymentList.Add(rec);
                                }
                            }

                            var paymentListTotal = queryPaymentTrans.Where(s => s.ErrorInfo == Common.VALIDATE_OK).ToList();

                            var err = await Common.CreatePaymentFile(api, paymentList, MandateCache, DebtorCache, nextPaymentFileId, debPaymentFormat, BankAccountCache);

                            if (err != ErrorCodes.Succes)
                            {
                                UtilDisplay.ShowErrorCode(err);
                            }
                            else
                            {
                                Common.InsertPaymentReference(paymentListTotal.Where(s => s.ErrorInfo == Common.VALIDATE_OK).ToList(), api, nextPaymentFileId);
                            }
                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"));
                        }
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
            var dialogInfo = Common.GetReturnFiles(api, listDebTransPaym, DebtorCache, MandateCache, PaymentFormatCache, debtorPaymFile);

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

                ribbonControl.EnableButtons("ExpandGroups");
                ribbonControl.EnableButtons("CollapseGroups");
            }
            else
            {
                ibaseMergePaym.Caption = Uniconta.ClientTools.Localization.lookup("MergePayments");
                ibaseMergePaym.LargeGlyph = Utility.GetGlyph("Match_Add_32x32.png");

                dgDebtorTranOpenGrid.UngroupBy("MergeDataId");
                dgDebtorTranOpenGrid.Columns.GetColumnByName("PaymentDate").AllowFocus = true;
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
                foreach (var rec in (IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.ItemsSource)
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
                    var dictPaymTransfer = new Dictionary<int, DebtorTransDirectDebit>();
                    DebtorTransDirectDebit mergePaymentRefId;

                    var netsBS = false;
                    int index = 0;
                    foreach (var rec in (IEnumerable<DebtorTransDirectDebit>)dgDebtorTranOpenGrid.ItemsSource)
                    {
                        int rowHandle = dgDebtorTranOpenGrid.GetRowHandleByListIndex(index);
                        index++;
                        if (!dgDebtorTranOpenGrid.IsRowVisible(rowHandle) || rec._PaymentStatus == PaymentStatusLevel.OnHold) //TODO: Pt. tillades alle med undtagelse af OnHold
                            continue;

                        if (rec.PaymentFormat != null)
                        {
                            var paymFormatClient = (DebtorPaymentFormat)PaymentFormatCache.Get(rec.PaymentFormat);
                            netsBS = rec.PaymentFormat == paymFormatClient._Format;
                        }

                        var paymRefId = rec._PaymentRefId != 0 ? rec._PaymentRefId : -rec.PrimaryKeyId;

                        if (dictPaymTransfer.TryGetValue(paymRefId, out mergePaymentRefId))
                        {
                            mergePaymentRefId.MergedAmount += netsBS ? rec.Amount : rec.PaymentAmount;
                            mergePaymentRefId.UsedCachDiscount += rec.UsedCachDiscount;
                            mergePaymentRefId.settleTypeRowId = true;
                            mergePaymentRefId.rowNumbers.Append(';').Append(rec.PrimaryKeyId);
                            mergePaymentRefId.hasBeenMerged = true;
                        }
                        else
                        {
                            mergePaymentRefId = new DebtorTransDirectDebit();
                            StreamingManager.Copy(rec, mergePaymentRefId);
                            mergePaymentRefId._PaymentRefId = paymRefId;
                            mergePaymentRefId.MergedAmount = netsBS ? rec.Amount : rec.PaymentAmount;
                            mergePaymentRefId.rowNumbers = new StringBuilder();
                            mergePaymentRefId.rowNumbers.Append(rec.PrimaryKeyId);
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

            DebtorPaymentFormatClientNets paymentFormatNets = null;

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

                if (paymFormatClient != null && paymFormatClient._ExportFormat == (byte)DebtorPaymFormatType.NetsBS && rec.Invoice != 0 && 
                   (rec._PaymentStatus == PaymentStatusLevel.None || rec._PaymentStatus == PaymentStatusLevel.Resend || rec._PaymentStatus == PaymentStatusLevel.StopPayment))
                {
                    if (paymentFormatNets == null)
                    {
                        paymentFormatNets = new DebtorPaymentFormatClientNets();
                        StreamingManager.Copy(paymFormatClient, paymentFormatNets);
                    }

                    if (company._PaymentCodeOption == 0 && paymentFormatNets._Type == NetsBSType.Total && paymentFormatNets._CreatePaymentId)
                        rec.PaymentId = company.GenerateFIKInstruction(rec.Account, rec.Invoice).ToString();

                    transDD.Add(rec);
                }
            }

            if (transDD != null)
                await CreatePaymentMessage(transDD);
        }

        async Task CreatePaymentMessage(IEnumerable<DebtorTransDirectDebit> lst)
        {
            TransactionAPI tranApi = new TransactionAPI(api);
            var lstInvoiceTxt = await tranApi.CreatePaymentMessage(lst);

            if (lstInvoiceTxt == null)
                return;

            int idx = 0;
            foreach (var rec in lst)
            {
                rec.TransactionText = lstInvoiceTxt[idx];
                idx++;
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


