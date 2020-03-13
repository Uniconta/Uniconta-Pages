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
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using System.Collections;
using Uniconta.ClientTools.Util;
using System.IO;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using Uniconta.API.Plugin;
using UnicontaClient.Creditor.Payments;
using Microsoft.Win32;
using Uniconta.API.System;
using DevExpress.Xpf.Grid;
using System.Text;
using System.Text.RegularExpressions;
using UnicontaISO20022CreditTransfer;
using UnicontaClient.Pages.Creditor.Payments;
using Localization = Uniconta.ClientTools.Localization;
using Uniconta.Common.Utility;
#if !SILVERLIGHT
using SecuredClient.Client;
using SecuredClient.Provider;
using SecuredClient.XmlSecurity;
using UnicontaClient.Pages.Creditor.Payments.Denmark;
using ISO20022CreditTransfer;
using DevExpress.XtraEditors.Filtering.Templates;
using Microsoft.Win32;
using Uniconta.DirectDebitPayment;
#endif
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class CreditorTransPayment : CreditorTransOpenClient
    {
        [Display(Name = "UsedCachDiscount", ResourceType = typeof(GLDailyJournalLineText))]
        public double UsedCachDiscount { get { return _UsedCachDiscount; } set { _UsedCachDiscount = value; NotifyPropertyChanged("UsedCachDiscount"); } }
        public double _UsedCachDiscount;

        [Display(Name = "PaymentAmount", ResourceType = typeof(DCTransText))]
        public double PaymentAmount { get { return _PaymentAmount; } set { _PaymentAmount = value; NotifyPropertyChanged("PaymentAmount"); } }
        public double _PaymentAmount;

        [Display(Name = "InvoiceAmount", ResourceType = typeof(DCTransText))]
        public double InvoiceAmount { get { return _InvoiceAmount; } }
        public double _InvoiceAmount;

        [Display(Name = "Remaining", ResourceType = typeof(DCTransText))]
        public double RemainingAmount { get { return _RemainingAmount; } }
        public double _RemainingAmount;

        [Display(Name = "SystemInfo", ResourceType = typeof(DCTransText))]
        public string ErrorInfo { get { return _ErrorInfo; } }
        public string _ErrorInfo;

        internal void NotifyErrorSet()
        {
            NotifyPropertyChanged("ErrorInfo");
        }

        [Display(Name = "Currency", ResourceType = typeof(GLDailyJournalText))]
        public string CurrencyLocal { get { return _CurrencyLocal; } }
        public string _CurrencyLocal;

        [Display(Name = "MergePaymId", ResourceType = typeof(DCTransText))]
        public string MergePaymId { get { return _MergePaymId; } }
        public string _MergePaymId;

        internal void NotifyMergePaymIdSet()
        {
            NotifyPropertyChanged("MergePaymId");
        }

        public string ISOPaymentType;
        public bool internationalPayment;
        public StringBuilder invoiceNumbers;
        public bool hasBeenMerged;
        public StringBuilder rowNumbers;
        public bool settleTypeRowId;
    }

    public class PaymentsGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransPayment); } }
        public override bool Readonly { get { return false; } }
        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Delete && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                e.Handled = true;
            }
            else
                base.OnPreviewKeyDown(e);
        }

        protected override void DataLoaded(UnicontaBaseEntity[] Arr)
        {
            var page = this.Page as Payments;
            page.LoadPayments((IEnumerable<CreditorTransPayment>)Arr);
        }
    }

    public partial class Payments : GridBasePage
    {
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
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "GenerateJournalLines", "SaveGrid", "RefreshGrid", "Filter", "ClearFilter" });
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
            dgCreditorTranOpenGrid.ShowTotalSummary();
            if (toDate == DateTime.MinValue)
                toDate = txtDateTo.DateTime.Date;

            txtDateTo.DateTime = toDate;
            txtDateFrm.DateTime = fromDate;
       
            GetMergeUnMergePaymMenuItem();
            GetExpandAndCollapseMenuItem();
            ribbonControl.DisableButtons("ExpandGroups");
            ribbonControl.DisableButtons("CollapseGroups");
            if (glJournalGenerated)
                ribbonControl.DisableButtons("MergeUnMergePaym");

#if SILVERLIGHT
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "ReadReceipt");
#endif
            if (creditorTransPayList != null)
            {
                dgCreditorTranOpenGrid.ItemsSource = creditorTransPayList;
                dgCreditorTranOpenGrid.Visibility = Visibility.Visible;
            }

            var Comp = api.CompanyEntity;

            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
            JournalCache = Comp.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
            BankAccountCache = Comp.GetCache(typeof(Uniconta.DataModel.BankStatement));
            PaymentFormatCache = Comp.GetCache(typeof(Uniconta.DataModel.CreditorPaymentFormat));
        }

        public override bool IsDataChaged { get { return false; } }

        static DateTime fromDate;
        static DateTime toDate;

        protected override Filter[] DefaultFilters()
        {
            var arrFilter = new Filter[1];

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
            SortingProperties dateSort = new SortingProperties("DueDate");
            SortingProperties VoucherSort = new SortingProperties("Date");
            return new SortingProperties[] { dateSort, VoucherSort };
        }

        public Payments(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorTranOpenGrid.SelectedItem as CreditorTransPayment;
            var selectedItems = dgCreditorTranOpenGrid.SelectedItems;


            switch (ActionType)
            {
                case "SaveGrid":
                    dgCreditorTranOpenGrid.SelectedItem = null;
                    dgCreditorTranOpenGrid.SaveData();
                    break;
                case "GeneratePaymentFile":
                    if (dgCreditorTranOpenGrid.ItemsSource == null) return;
                    GeneratePaymentFile();
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null) return;
                    DebtorTransactions.ShowVoucher(dgCreditorTranOpenGrid.syncEntity, api, busyIndicator);
                    break;
#if !SILVERLIGHT
                case "Validate":
                    if (dgCreditorTranOpenGrid.ItemsSource == null) return;
                    CallValidatePayment();
                    break;

                case "ReadReceipt":
                    CWReadReceipt readReceiptDialog = new CWReadReceipt(this.api);
                    readReceiptDialog.Show();
                    break;

                case "PaymStatusReport":
                    if (dgCreditorTranOpenGrid.ItemsSource == null) return;
                    PaymStatusReport();
                    break;

                case "MergeUnMergePaym":
                    if (dgCreditorTranOpenGrid.ItemsSource == null) return;
                    MergePaym();
                    break;

                case "ExpandGroups":
                    if (dgCreditorTranOpenGrid.ItemsSource == null) return;
                    ExpandGroups();
                    break;

                case "CollapseGroups":
                    if (dgCreditorTranOpenGrid.ItemsSource == null) return;
                    CollapseGroups();
                    break;

                case "UncheckAllPaid":
                    var visibleAllRows = (IEnumerable<CreditorTransPayment>)dgCreditorTranOpenGrid.GetVisibleRows();
                    if (visibleAllRows != null)
                        UncheckPaid(visibleAllRows);
                    break;

                case "UncheckMarkedPaid":
                    var markedRows = selectedItems.Cast<CreditorTransPayment>();
                    if (markedRows != null)
                        UncheckPaid(markedRows);
                    break;

                case "UncheckCurrentPaid":
                    if (selectedItems != null)
                    {
                        var selectedRow = new CreditorTransPayment[] { (CreditorTransPayment)dgCreditorTranOpenGrid.SelectedItem };
                        UncheckPaid(selectedRow);
                    }
                    break;

                case "CheckOnholdAll":
                    var checkOnholdAllRows = (IEnumerable<CreditorTransPayment>)dgCreditorTranOpenGrid.GetVisibleRows();
                    if (checkOnholdAllRows != null)
                        CheckUncheckOnhold(checkOnholdAllRows, true); //TODO:Check om if i metode er nødvendig tester på antal
                    break;

                case "CheckOnholdMarked":
                    var checkOnholdMarkedRows = selectedItems.Cast<CreditorTransPayment>();
                    if (checkOnholdMarkedRows != null)
                        CheckUncheckOnhold(checkOnholdMarkedRows, true);
                    break;

                case "CheckOnholdCurrent":
                    if (selectedItems != null)
                    {
                        var selectedRow = new CreditorTransPayment[] { (CreditorTransPayment)dgCreditorTranOpenGrid.SelectedItem };
                        CheckUncheckOnhold(selectedRow, true);
                    }
                    break;

                case "UncheckOnholdAll":
                    var unCheckOnholdAllRows = (IEnumerable<CreditorTransPayment>)dgCreditorTranOpenGrid.GetVisibleRows();
                    if (unCheckOnholdAllRows != null)
                        CheckUncheckOnhold(unCheckOnholdAllRows, false); //TODO:Check om if i metode er nødvendig tester på antal
                    break;

                case "UncheckOnholdMarked":
                    var unCheckOnholdMarkedRows = selectedItems.Cast<CreditorTransPayment>();
                    if (unCheckOnholdMarkedRows != null)
                        CheckUncheckOnhold(unCheckOnholdMarkedRows, false);
                    break;

                case "UncheckOnholdCurrent":
                    if (selectedItems != null)
                    {
                        var selectedRow = new CreditorTransPayment[] { (CreditorTransPayment)dgCreditorTranOpenGrid.SelectedItem };
                        CheckUncheckOnhold(selectedRow, false);
                    }
                    break;

                //case "CheckOnhold":
                //    if (dgCreditorTranOpenGrid.ItemsSource == null) return;
                //    CheckUncheckOnhold(selectedItems, true);
                //    break;

                //case "UncheckOnhold":
                //    if (dgCreditorTranOpenGrid.ItemsSource == null) return;
                //    CheckUncheckOnhold(selectedItems, false);
                //    break;

#endif
                case "GenerateJournalLines":
                    if (dgCreditorTranOpenGrid.ItemsSource == null) return;

                    ImportToLine cwLine = new ImportToLine(api);


#if !SILVERLIGHT
                    cwLine.DialogTableId = 2000000036;
#endif
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
                                var listLineClient = new List<Uniconta.DataModel.GLDailyJournalLine>();

                                if (!DJclient._GenerateVoucher && !DJclient._ManualAllocation && cwLine.AddVoucherNumber)
                                    nextVoucherNumber = (int)await numberserieApi.ViewNextNumber(DJclient._NumberSerie);

                                List<CreditorTransPayment> paymentListTransfer = null;
                                var dictPaymTransfer = new Dictionary<int, CreditorTransPayment>();
                                CreditorTransPayment mergePaymentRefId;

                                int index = 0;
                                foreach (var rec in (IEnumerable<CreditorTransPayment>)dgCreditorTranOpenGrid.ItemsSource) 
                                {
                                    int rowHandle = dgCreditorTranOpenGrid.GetRowHandleByListIndex(index);
                                    index++;
                                    if (!dgCreditorTranOpenGrid.IsRowVisible(rowHandle) || rec._OnHold || (rec._PaymentAmount <= 0d && rec.PaymentRefId == 0))
                                        continue;

                                    var paymRefId = rec.PaymentRefId != 0 ? rec.PaymentRefId : -rec.PrimaryKeyId;

                                    if (dictPaymTransfer.TryGetValue(paymRefId, out mergePaymentRefId)) 
                                    {
                                        mergePaymentRefId._PaymentAmount += rec._PaymentAmount;
                                        mergePaymentRefId._UsedCachDiscount += rec._UsedCachDiscount;

                                        mergePaymentRefId.settleTypeRowId = true;
                                        mergePaymentRefId.rowNumbers.Append(';').Append(rec.PrimaryKeyId);
                                        mergePaymentRefId.invoiceNumbers.Append(';').Append(rec.InvoiceAN);

                                        mergePaymentRefId.hasBeenMerged = true;
                                    }
                                    else
                                    {
                                        mergePaymentRefId = new CreditorTransPayment();
                                        StreamingManager.Copy(rec, mergePaymentRefId);
                                        mergePaymentRefId._CurrencyLocal = rec._CurrencyLocal;
                                        mergePaymentRefId.PaymentRefId = paymRefId;
                                        mergePaymentRefId._PaymentAmount = rec._PaymentAmount; 
                                        mergePaymentRefId.invoiceNumbers = new StringBuilder();
                                        mergePaymentRefId.invoiceNumbers.Append(rec.InvoiceAN);
                                        
                                        mergePaymentRefId.rowNumbers = new StringBuilder();
                                        mergePaymentRefId.rowNumbers.Append(rec.PrimaryKeyId);
                                        mergePaymentRefId.settleTypeRowId = true;
                                        
                                        mergePaymentRefId._UsedCachDiscount = rec._UsedCachDiscount;
                                        dictPaymTransfer.Add(paymRefId, mergePaymentRefId);
                                    }
                                    paymentListTransfer = dictPaymTransfer.Values.ToList();
                                }
                                if (paymentListTransfer == null)
                                {
                                    busyIndicator.IsBusy = false;
                                    return;
                                }

                                foreach (var cTOpenClient in paymentListTransfer)
                                {
                                    var creditor = cTOpenClient.Creditor;
                                    var lineclient = new GLDailyJournalLineClient();
                                    lineclient.SetMaster(DJclient);
                                    lineclient._DCPostType = DCPostType.Payment;
                                    lineclient._LineNumber = ++LineNumber;
                                    lineclient._Date = cTOpenClient.PaymentDate != DateTime.MinValue ? cTOpenClient.PaymentDate : cTOpenClient._DueDate;
                                    lineclient._TransType = cwLine.TransType;
                                    lineclient._AccountType = (byte)GLJournalAccountType.Creditor;
                                    lineclient._Account = cTOpenClient.Account;
                                    lineclient._OffsetAccount = cwLine.BankAccount;
                                    lineclient._Invoice = cTOpenClient.hasBeenMerged ? null : cTOpenClient.InvoiceAN;
                                    lineclient._Dim1 = creditor._Dim1;
                                    lineclient._Dim2 = creditor._Dim2;
                                    lineclient._Dim3 = creditor._Dim3;
                                    lineclient._Dim4 = creditor._Dim4;
                                    lineclient._Dim5 = creditor._Dim5;

                                    if (cTOpenClient.settleTypeRowId)
                                    {
                                        lineclient._SettleValue = SettleValueType.RowId;
                                        lineclient._Settlements = cTOpenClient.rowNumbers.ToString();
                                    }
                                    else
                                    {
                                        lineclient._Settlements = null;
                                    }
                                  
                                    lineclient._UsedCachDiscount = cTOpenClient._UsedCachDiscount;
                                    lineclient._DocumentRef = cTOpenClient.DocumentRef;
                                    var curOpen = cTOpenClient._AmountOpenCur;
                                    if (curOpen != 0d && cTOpenClient.Currency.HasValue)
                                    {
                                        lineclient._Currency = (byte)cTOpenClient.Currency.Value;
                                        lineclient.AmountCur = cTOpenClient._PaymentAmount;
                                        if (cTOpenClient._PaymentAmount == -cTOpenClient._AmountOpen)
                                            lineclient.Amount = -cTOpenClient._AmountOpen;
                                        else
                                            lineclient.Amount = cTOpenClient._AmountOpen * cTOpenClient._PaymentAmount / curOpen; // payAmount different sign than curOpen, so no minus.
                                    }
                                    else
                                        lineclient.Amount = cTOpenClient._PaymentAmount;

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
                                            numberserieApi.SetNumber(DJclient._NumberSerie, nextVoucherNumber-1);

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
                case "RefreshGrid":
                    if (dgCreditorTranOpenGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgCreditorTranOpenGrid);

                    btnSerach_Click(null, null);
                    setMergeUnMergePaym(false);

                    break;

                case "Filter":
                    if (dgCreditorTranOpenGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgCreditorTranOpenGrid);

                    gridRibbon_BaseActions(ActionType);
                    setMergeUnMergePaym(false);
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (JournalCache == null)
                JournalCache = await api.LoadCache(typeof(Uniconta.DataModel.GLDailyJournal)).ConfigureAwait(false);
            if (BankAccountCache == null)
                BankAccountCache = await api.LoadCache(typeof(Uniconta.DataModel.BankStatement)).ConfigureAwait(false);
            if (PaymentFormatCache == null)
                PaymentFormatCache = await api.LoadCache(typeof(Uniconta.DataModel.CreditorPaymentFormat)).ConfigureAwait(false);
        }


        public void CreateHeaderRow(StreamWriter Writer, List<string> dispProps, char seperator, char[] specialChar)
        {
            bool firstColumn = true;
            foreach (var value in dispProps)
            {
                if (!firstColumn)
                    Writer.Write(seperator);
                else
                    firstColumn = false;
                CSVHelper.writeString(Writer, value, specialChar);
            }
            Writer.WriteLine();
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
                ibaseMergePaym.LargeGlyph = Utilities.Utility.GetGlyph("Match_Remove_32x32.png");

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
                        rec._MergePaymId = null;
                }

                ibaseMergePaym.Caption = Localization.lookup("MergePayments");
                ibaseMergePaym.LargeGlyph = Utilities.Utility.GetGlyph("Match_Add_32x32.png");

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

        public void ExportDataGrid(StreamWriter writer, char seperator)
        {
            System.Collections.IList source = (dgCreditorTranOpenGrid.ItemsSource as System.Collections.IList);
            if (source == null || source.Count == 0)
                return;
            List<string> Headers = new List<string>(15);
            var propt = new List<PropertyInfo>(15);
            var type = (CreditorTransOpenClient)source[0];

            foreach (var prop in type.GetType().GetProperties())
            {
                var Name = prop.Name;
                if (Name == "Account" || Name == "AmountOpen" || Name == "PaymentMethod" || Name == "PaymentId" ||
                        Name == "DueDate" || Name == "Comment" || Name == "Voucher" || Name == "Invoice" || Name == "InvoiceAN" ||
                        Name == "Amount" || Name == "TransType" || Name == "Currency" || Name == "PaymentDate" || Name == "Message")
                {
                    propt.Add(prop);
                    Headers.Add(UtilFunctions.GetDisplayNameFromPropertyInfo(prop));
                }
            }

            char[] specialChar = new char[] { '"', seperator };
            CreateHeaderRow(writer, Headers, seperator, specialChar);
            //create row
            foreach (CreditorTransOpenClient data in source)
            {
                var t = data.GetType();
                bool firstColumn = true;
                foreach (PropertyInfo prop in propt)
                {
                    string value = Convert.ToString(prop.GetValue(data, null));

                    if (!firstColumn)
                        writer.Write(seperator);
                    else
                        firstColumn = false;
                    CSVHelper.writeString(writer, value, specialChar);
                }
                writer.WriteLine();
            }
        }

        private async void GeneratePaymentFile()
        {
            CWImportPayment cwwin = new CWImportPayment(api);
//TODO:Overvej hvor WPF skal sættes
#if !SILVERLIGHT 
            var paymentReference = new PaymentReference();
            await paymentReference.PaymentRefSequence(api);

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult != true)
                    return;

                var paymentFormatRec = cwwin.PaymentFormat;
                var paymMethod = paymentFormatRec.PaymentMethod;
                var paymentFormat = paymentFormatRec._Format;

                if (cwwin.userPlugin == null)
                {
                    ExpandCollapseAllGroups();

                    List<CreditorTransPayment> ListCredTransPaym = new List<CreditorTransPayment>();
                    int index = 0;
                    foreach (var rec in (IEnumerable<CreditorTransPayment>)dgCreditorTranOpenGrid.ItemsSource)
                    {
                        int rowHandle = dgCreditorTranOpenGrid.GetRowHandleByListIndex(index);
                        index++;
                        if (!dgCreditorTranOpenGrid.IsRowVisible(rowHandle) || rec._OnHold || rec._Paid || (rec._PaymentAmount <= 0d && !doMergePaym) || rec._PaymentFormat != paymentFormat)
                            continue;

                        ListCredTransPaym.Add(rec);
                    }

                    if (ListCredTransPaym.Count == 0)
                    {
                        UnicontaMessageBox.Show(Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                        return;
                    }

                    IEnumerable<CreditorTransPayment> queryPaymentTrans = ListCredTransPaym.AsEnumerable();
                    if (ValidatePayments(queryPaymentTrans, paymentFormatRec))
                    {
                        try
                        {
                            List<CreditorTransPayment> paymentListMerged = null;
                            var dictPaym = new Dictionary<String, CreditorTransPayment>();
                            CreditorTransPayment mergePayment; 
                            
                            var paymNumSeqRefId = paymentReference.NumberSeqRefId;

                            if (doMergePaym)
                                queryPaymentTrans = queryPaymentTrans.OrderBy(x => x.MergePaymId).ToList();

                            foreach (var rec in queryPaymentTrans.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK)) 
                            {
                                if (doMergePaym)
                                {
                                    if (dictPaym.TryGetValue(rec._MergePaymId, out mergePayment))
                                    {
                                        mergePayment._PaymentAmount += rec._PaymentAmount;
                                        mergePayment.invoiceNumbers.Append(',').Append(rec.InvoiceAN);

                                        if (paymentFormatRec._PaymentGrouping == PaymentGroupingType.All)
                                            mergePayment._PaymentDate = rec._PaymentDate < mergePayment._PaymentDate ? rec._PaymentDate : mergePayment._PaymentDate;

                                        if (rec._MergePaymId == StandardPaymentFunctions.MERGEID_SINGLEPAYMENT)
                                        {
                                            paymNumSeqRefId++;
                                            rec.PaymentRefId = paymNumSeqRefId;
                                        }
                                        else
                                            rec.PaymentRefId = paymNumSeqRefId;
                                    }
                                    else
                                    {
                                        paymNumSeqRefId++;
                                        rec.PaymentRefId = paymNumSeqRefId;

                                        mergePayment = new CreditorTransPayment();
                                        StreamingManager.Copy(rec, mergePayment);
                                        mergePayment._MergePaymId = rec._MergePaymId;
                                        mergePayment._CurrencyLocal = rec._CurrencyLocal;
                                        mergePayment.ISOPaymentType = rec.ISOPaymentType;
                                        mergePayment.PaymentRefId = rec._PaymentRefId;
                                        mergePayment.invoiceNumbers = new StringBuilder();
                                        mergePayment.invoiceNumbers.Append(rec.InvoiceAN);
                                        mergePayment._PaymentAmount = rec._PaymentAmount;
                                        mergePayment._ErrorInfo = rec._ErrorInfo;

                                        dictPaym.Add(rec._MergePaymId, mergePayment);
                                    }
                                    paymentListMerged = dictPaym.Values.ToList();
                                }
                                else
                                {
                                    paymNumSeqRefId++;
                                    rec.PaymentRefId = paymNumSeqRefId;
                                }
                            }

                            List<CreditorTransPayment> paymentList = null;
                            if (doMergePaym)
                            {
                                foreach (var rec in paymentListMerged)
                                {
                                    if (rec._PaymentAmount <= 0)
                                    {
                                        rec._MergePaymId = Localization.lookup("Excluded");

                                        foreach (var recTrans in queryPaymentTrans.Where(s => s.PaymentRefId == rec.PaymentRefId))
                                        {
                                            recTrans._ErrorInfo = "Merged amount is negative or zero";
                                            recTrans._MergePaymId = Localization.lookup("Excluded");
                                            recTrans.NotifyErrorSet();
                                        }
                                    }
                                }

                                paymentList = paymentListMerged.Where(s => s._MergePaymId != Localization.lookup("Excluded") && s.MergePaymId != StandardPaymentFunctions.MERGEID_SINGLEPAYMENT).ToList();
                                foreach (var s in queryPaymentTrans)
                                {
                                    if (s.MergePaymId == StandardPaymentFunctions.MERGEID_SINGLEPAYMENT)
                                        paymentList.Add(s);
                                }
                            }
                            else
                            {
                                paymentList = queryPaymentTrans.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList();
                            }
 
                            var paymentListTotal = queryPaymentTrans.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList();

                            if (paymMethod == ExportFormatType.CSV || paymMethod == ExportFormatType.NETS_Norge)
                            {
                                var sfd = UtilDisplay.LoadSaveFileDialog;
                                if (cwwin.FileOption == "CSV")
                                {
                                    sfd.DefaultExt = "csv";
                                    sfd.Filter = "CSV Files (*.csv)|*.csv|All files (*.*)|*.*";
                                    sfd.FilterIndex = 1;
                                }
                                else
                                    sfd.Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.TXT);

                                bool? userClickedSave = sfd.ShowDialog();

                                if (userClickedSave == true)
                                {
                                    using (Stream stream = File.Create(sfd.FileName))
                                    {
                                        var writer = new StreamWriter(stream);
                                        switch (paymMethod)
                                        {
                                            case ExportFormatType.CSV:
                                                char seperator = UtilFunctions.GetDefaultDeLimiter();
                                                ExportDataGrid(writer, seperator);
                                                break;
                                            case ExportFormatType.NETS_Norge:
                                                var err = NETSNorge.GenerateFile(api, CreditorCache, paymentList, paymentFormatRec, stream);
                                                if (err != ErrorCodes.Succes)
                                                    UtilDisplay.ShowErrorCode(err);
                                                break;
                                        }
                                    }
                                }
                            }
                            else if (paymMethod == ExportFormatType.Nordea_CSV)
                            {
                                NordeaPaymentFormat.GenerateFile(paymentList, paymentListTotal, api, paymentFormatRec, paymentReference, BankAccountCache, CreditorCache, glJournalGenerated);
                            }
                            else if (paymMethod == ExportFormatType.DanskeBank_CSV)
                            {
                                DanskeBankPayFormat.GenerateFile(paymentList, paymentListTotal, api, paymentFormatRec, paymentReference, BankAccountCache, CreditorCache, glJournalGenerated);
                            }
                            else if (paymMethod == ExportFormatType.BankData)
                            {
                                BankDataPayFormat.GenerateFile(paymentList, paymentListTotal, api, paymentFormatRec, paymentReference, BankAccountCache, CreditorCache, glJournalGenerated);
                            }
                            else if (paymMethod == ExportFormatType.SDC)
                            {
                                SDCPayFormat.GenerateFile(paymentList, paymentListTotal, api, paymentFormatRec, paymentReference, BankAccountCache, CreditorCache, glJournalGenerated);
                            }
                            else if (paymMethod == ExportFormatType.BEC_CSV)
                            {
                                BECPayFormat.GenerateFile(paymentList, paymentListTotal, api, paymentFormatRec, paymentReference, BankAccountCache, CreditorCache, glJournalGenerated);
                            }
                            else if (paymMethod == ExportFormatType.ISO20022_DK || paymMethod == ExportFormatType.ISO20022_NL || paymMethod == ExportFormatType.ISO20022_NO || paymMethod == ExportFormatType.ISO20022_DE ||
                                     paymMethod == ExportFormatType.ISO20022_EE || paymMethod == ExportFormatType.ISO20022_SE || paymMethod == ExportFormatType.ISO20022_UK || paymMethod == ExportFormatType.ISO20022_LT)
                            {
                                GeneratePaymentFileISO20022(paymentList, paymentListTotal, paymentFormatRec, paymentReference);
                            }
                            //else if (paymMethod == ExportFormatType.BankConnect)
                            //{
                            //    SendPaymentFileToBankConnect(paymentList, paymentListTotal, paymentFormatRec, paymentReference);
                            //}

                        }
                        catch (Exception ex)
                        {
                            UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"));
                        }
                    }
                    else
                    {
                        GeneratePluginPaymentFile(paymentFormatRec, cwwin.userPlugin);
                    }
                }
                };
                cwwin.Show();
#endif

        }

#if !SILVERLIGHT
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
                    statusReport.StatusReport(dgCreditorTranOpenGrid, cwwin.browseFile.FilePath, this.api);
                }
            };
            cwwin.Show();
        }

        void UncheckPaid(IEnumerable<CreditorTransPayment> paymentList)
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

        void CheckUncheckOnhold(IEnumerable<CreditorTransPayment> paymentList, bool check)
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

        void MergePaym()
        {
            doMergePaym = !doMergePaym;
            if (doMergePaym)
            {
                CWMergePayment cwwin = new CWMergePayment(this.api);

                cwwin.Closing += delegate
                {
                    if (cwwin.DialogResult == true)
                    {
                        CreditorPaymentsMerge CredPaymMerge = new CreditorPaymentsMerge();
                        if (CredPaymMerge.MergePayments(api.CompanyEntity, dgCreditorTranOpenGrid, cwwin.PaymentFormat, BankAccountCache))
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
#endif
        private async void GeneratePluginPaymentFile(CreditorPaymentFormat PaymentSetup, Uniconta.DataModel.UserPlugin userPlugin)
        {
            Type plugin = null;
            if (userPlugin == null)
                return;
#if !SILVERLIGHT
            else
                plugin = await LoadPaymentPlugin(userPlugin, api);
#endif
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
#if !SILVERLIGHT
                    using (Stream stream = File.Create(sfd.FileName))
#else
                    using (Stream stream = sfd.OpenFile())
#endif
                    {
                        var errorCode = paymentPluginObj.Generate(Trans, PaymentSetup, stream);
                        if (errorCode != ErrorCodes.Succes)
                        {
                            var err = paymentPluginObj.GetErrorDescription();
                            UnicontaMessageBox.Show(err, Uniconta.ClientTools.Localization.lookup("Information"));
                        }
                        stream.Close();
                    }
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
                    return;
                }
            }
        }

#if !SILVERLIGHT
        private void CallValidatePayment()
        {
            CWValidatePayment cwwin = new CWValidatePayment(api);

            cwwin.Closing += delegate
            {
                if (cwwin.DialogResult == true)
                {
                    ExpandCollapseAllGroups();

                    List<CreditorTransPayment> ListCredTransPaym = new List<CreditorTransPayment>();
                    int index = 0;
                    foreach (var rec in (IEnumerable<CreditorTransPayment>)dgCreditorTranOpenGrid.ItemsSource)
                    {
                        int rowHandle = dgCreditorTranOpenGrid.GetRowHandleByListIndex(index);
                        index++;
                        rec._ErrorInfo = string.Empty;
                        rec.NotifyErrorSet();
                        if (!dgCreditorTranOpenGrid.IsRowVisible(rowHandle) || rec._OnHold || rec._Paid || (rec._PaymentAmount <= 0d && !doMergePaym) || rec._PaymentFormat != cwwin.PaymentFormat.Format)
                            continue;

                        ListCredTransPaym.Add(rec);
                    }

                    IEnumerable<CreditorTransPayment> queryPaymentTrans = ListCredTransPaym.AsEnumerable();

                    ValidatePayments(queryPaymentTrans, cwwin.PaymentFormat, true);
                }
            };
            cwwin.Show();
        }

        private bool ValidatePayments(IEnumerable<CreditorTransPayment> queryPaymentTrans, CreditorPaymentFormat credPaymFormat, bool validateOnly = false)
        {
            var paymentISO20022PreValidate = new PaymentISO20022PreValidate();
            var paymentISO20022Validate = new PaymentISO20022Validate();

            var paymentformat = (ExportFormatType)credPaymFormat._ExportFormat;
            if (glJournalGenerated && (paymentformat == ExportFormatType.ISO20022_DK || paymentformat == ExportFormatType.ISO20022_NL ||paymentformat == ExportFormatType.ISO20022_NO || paymentformat == ExportFormatType.ISO20022_DE ||
                                       paymentformat == ExportFormatType.ISO20022_EE || paymentformat == ExportFormatType.ISO20022_SE || paymentformat == ExportFormatType.ISO20022_UK || paymentformat == ExportFormatType.ISO20022_LT))
            {
                UnicontaMessageBox.Show(string.Format("Payment format '{0}' is not available for GL Journal generated payments.", credPaymFormat._ExportFormat), Uniconta.ClientTools.Localization.lookup("Warning"));
                return false;
            }

            var preValidateRes = paymentISO20022PreValidate.PreValidateISO20022(api.CompanyEntity, BankAccountCache, credPaymFormat);

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
                paymentISO20022Validate.CompanyBank(credPaymFormat);

                foreach (var rec in queryPaymentTrans)
                {
                    rec._ErrorInfo = string.Empty;

                    if (doMergePaym && rec._MergePaymId == null)
                    {
                        countErr++;
                        rec._ErrorInfo = "Merge of payments has failed";
                        rec.NotifyErrorSet();
                    }
                    else
                    {
                        var validateRes = paymentISO20022Validate.ValidateISO20022(api.CompanyEntity, rec, BankAccountCache, glJournalGenerated);

                        if (validateRes.HasErrors)
                        {
                            countErr++;
                            foreach (CheckError error in validateRes.CheckErrors)
                            {
                                rec._ErrorInfo += error.ToString() + "\n";
                            }
                        }
                        else
                        {
                            rec._ErrorInfo = BaseDocument.VALIDATE_OK;
                        }
                    }

                    rec.NotifyErrorSet();
                }

                if (queryPaymentTrans.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).Any() == false && validateOnly == false)
                {
                    UnicontaMessageBox.Show(Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"));
                    return false;
                }

                if (validateOnly)
                {
                    if (countErr == 0)
                        UnicontaMessageBox.Show(Localization.lookup("ValidateNoError"), Uniconta.ClientTools.Localization.lookup("Validation"), MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        UnicontaMessageBox.Show(string.Format(Localization.lookup("ValidateFailInLines"), countErr), Uniconta.ClientTools.Localization.lookup("Validation"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            return true;
        }


        private void GeneratePaymentFileISO20022(IEnumerable<CreditorTransPayment> paymentList, IEnumerable<CreditorTransPayment> paymentListTotal, CreditorPaymentFormat credPaymFormat, PaymentReference paymentReference)
        {
            SaveFileDialog saveDialog = null;
            var paymentISO20022 = new PaymentISO20022();
      
            var result = paymentISO20022.GenerateISO20022(api.CompanyEntity, paymentList, BankAccountCache, credPaymFormat, paymentReference.NumberSeqFileId, doMergePaym);
                    
            if (result.NumberOfPayments != 0)
            {
                saveDialog = UtilDisplay.LoadSaveFileDialog;
                saveDialog.FileName = result.FileName;
                saveDialog.Filter = "XML-File | *.xml";
                bool? dialogResult = saveDialog.ShowDialog();
                if (dialogResult == true)
                {
                    string filename = null;
                    filename = saveDialog.FileName;
                   
                    result.Document.DocumentElement.SetAttribute(BaseDocument.XMLNS_XSI, BaseDocument.XMLNS_XSI_VALUE);

                    if (result.Encoding == Encoding.UTF8) //Remove BOM (Byte Order Mark)
                    {
                        using (TextWriter sw = new StreamWriter(filename, false, new UTF8Encoding(false)))
                        {
                            result.Document.Save(sw);
                        }
                    }
                    else
                    {
                        using (TextWriter sw = new StreamWriter(filename, false, result.Encoding))
                        {
                            result.Document.Save(sw);
                        }
                    }

                    paymentReference.InsertPaymentReferenceTask(paymentList.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList(),
                                                                paymentListTotal.Where(s => s._ErrorInfo == BaseDocument.VALIDATE_OK).ToList(), api);
                }
            }
            else
            {
                CWMessageBox cwMessage = new CWMessageBox("There are no payments!\nPlease check the System info column.");
                cwMessage.Show();
            }
        }
#endif

        private StringBuilder calculateFIKchecksum(string paymId)
        {
            var sb = new StringBuilder(40);
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

            sb.Append(Id);

            return sb;
        }

        private void buildPaymentIdFIK(CreditorTransPayment rec, string transPaymentId, string creditorPaymId)
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

                StringBuilder FIKString;

                var paymIdMask = string.Empty;
                int accountIndex = OCRMask.IndexOf('+');
                if (accountIndex > 0)
                {
                    paymIdMask = OCRMask.Substring(0, accountIndex);
                }
                else
                {
                    rec._ErrorInfo = "The FIK mask doesn't have a valid format.";
                    rec.NotifyErrorSet();
                    return;
                }

                int maskIndexStart = paymIdMask.IndexOf('N');
                if (maskIndexStart == -1)
                {
                    rec._ErrorInfo = "The FIK mask doesn't have a valid format.";
                    rec.NotifyErrorSet();
                    return;
                }

                var maskIndexEnd = 0;
                for (int i = maskIndexStart; i < paymIdMask.Length; i++)
                {
                    if (paymIdMask[i] == 'N')
                        maskIndexEnd = i;
                }

                var invoiceNumberStr = NumberConvert.ToString(rec.Invoice);
                if (invoiceNumberStr == "0")
                {
                    rec._ErrorInfo = "The FIK payment id can't be build due to a missing invoice number.";
                    rec.NotifyErrorSet();
                    return;
                }

                int invoiceMaskLength = maskIndexEnd - maskIndexStart + 1;
                if (invoiceMaskLength < invoiceNumberStr.Length)
                    invoiceNumberStr = invoiceNumberStr.Substring(invoiceNumberStr.Length - invoiceMaskLength);

                invoiceNumberStr = invoiceNumberStr.PadLeft(invoiceMaskLength, '0');
                var invoiceNumberMask = string.Empty;
                invoiceNumberMask = invoiceNumberMask.PadLeft(invoiceMaskLength, 'N');
                paymIdMask = paymIdMask.Replace(invoiceNumberMask, invoiceNumberStr);

                FIKString = calculateFIKchecksum(paymIdMask);
                var fikAccount = OCRMask.Remove(0, accountIndex + 1);

                FIKString.Append(" +").Append(fikAccount);
                rec._PaymentId = FIKString.ToString();
            }
            //FIK PaymentId mask is used <<
        }


        void LoadPayments() { LoadPayments((IEnumerable<CreditorTransPayment>)dgCreditorTranOpenGrid.ItemsSource); }
        internal void LoadPayments(IEnumerable<CreditorTransPayment> lst)
        {
            if (lst == null || CreditorCache == null)
                return;

            var today = BasePage.GetSystemDefaultDate();
            var company = api.CompanyEntity;
            var CountryId = company._CountryId;

            foreach (var rec in lst)
            {
                var cred = (Uniconta.DataModel.Creditor)CreditorCache.Get(rec.Account);
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

                if (CountryId == CountryCode.Norway || CountryId == CountryCode.Estonia)
                {
                    if (cred != null)
                    {
                        if (rec._PaymentId == null) // no paymentId on line, we need to take both values from Creditor.
                        {
                            if (cred._PaymentMethod == PaymentTypes.VendorBankAccount && CountryId == CountryCode.Norway)
                                creditorPaymId = string.Empty; //Norway PaymentId is used for Kid-No when PaymentType=VendorBankAccount
                            else if (cred._PaymentMethod == PaymentTypes.IBAN && CountryId == CountryCode.Estonia && cred._Country == CountryCode.Estonia)
                                creditorPaymId = string.Empty; //Estonia PaymentId is used for Payment Reference for Domestic payments  
                            else
                                creditorPaymId = cred._PaymentId;

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
                            rec._PaymentId = cred._PaymentId;
                        }

                        creditorPaymId = cred._PaymentId;
                    }
                    else
                        creditorPaymId = null;

                    var transPaymentId = rec._PaymentId;

                    if (rec._PaymentMethod == PaymentTypes.PaymentMethod3 || rec._PaymentMethod == PaymentTypes.PaymentMethod4 || rec._PaymentMethod == PaymentTypes.PaymentMethod5 || rec._PaymentMethod == PaymentTypes.PaymentMethod6)
                    {
                        buildPaymentIdFIK(rec, transPaymentId, creditorPaymId);
                    }
                    else
                    {
                        rec._PaymentId = (creditorPaymId != transPaymentId && transPaymentId != null) ? transPaymentId : creditorPaymId;
                    }
                }

                if (rec._SWIFT == null)
                    rec._SWIFT = cred._SWIFT;

                string paymMessage = paymFormatClient?._Message ?? string.Empty;

                rec._Message = StandardPaymentFunctions.ExternalMessage(paymMessage, rec, company, cred, true);

                if (rec._PaymentDate < today)
                {
                    var paymDate = rec._PaymentDate == DateTime.MinValue ? rec._DueDate : rec._PaymentDate;
                    rec._PaymentDate = today > paymDate ? today : paymDate;
#if !SILVERLIGHT
                    if (paymFormatClient != null && paymFormatClient._PaymentAction != NoneBankDayAction.None)
                            rec._PaymentDate = Uniconta.DirectDebitPayment.Common.AdjustToNextBankDay(CountryId, rec._PaymentDate, paymFormatClient._PaymentAction == NoneBankDayAction.After ? true : false);
#endif
                }
                if (rec.Currency != null)
                    rec._CurrencyLocal = AppEnums.Currencies.Values[(int)rec.Currency.Value];
                else
                    rec._CurrencyLocal = AppEnums.Currencies.Values[company._Currency];
                rec.NotifyPropertyChanged("CurrencyLocal");

                if (rec.Trans._Currency != 0)
                {
                    rec._PaymentAmount = -rec._AmountOpenCur;
                    rec._RemainingAmount = -rec._AmountOpenCur;
                    rec._InvoiceAmount = -rec.Trans._AmountCur;
                }
                else
                {
                    rec._PaymentAmount = -rec._AmountOpen;
                    rec._RemainingAmount = -rec._AmountOpen;
                    rec._InvoiceAmount = -rec.Trans._Amount;
                }

                if (rec._CashDiscount != 0d && today <= rec._CashDiscountDate)
                {
                    rec._UsedCachDiscount = rec._CashDiscount;
                    rec._PaymentAmount -= Math.Abs(rec._UsedCachDiscount);
                    rec._PaymentDate = rec._CashDiscountDate;
#if !SILVERLIGHT
                    if (paymFormatClient != null && paymFormatClient._PaymentAction != NoneBankDayAction.None)
                        rec._PaymentDate = Uniconta.DirectDebitPayment.Common.AdjustToNextBankDay(CountryId, rec._PaymentDate, false);
#endif
                }

                rec.NotifyPropertyChanged("PaymentAmount");
                rec.NotifyPropertyChanged("RemainingAmount");
                rec.NotifyPropertyChanged("InvoiceAmount");
                rec.NotifyPropertyChanged("PaymentFormat");
            }
        }


        public override bool HandledOnClearFilter()
        {
            setMergeUnMergePaym(false);
            return true;
        }

        private async void btnSerach_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (txtDateFrm.Text == string.Empty)
                fromDate = DateTime.MinValue;
            else
                fromDate = txtDateFrm.DateTime.Date;

            if (txtDateTo.Text == string.Empty)
                toDate = DateTime.MinValue;
            else
                toDate = txtDateTo.DateTime.Date;

            await Filter();
            if (dgCreditorTranOpenGrid.ItemsSource == null)
                return;
        }
    }
}
