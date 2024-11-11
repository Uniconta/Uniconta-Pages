using Uniconta.API.Service;
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using System;
using System.Collections.Generic;
using Uniconta.ClientTools.Controls;
using DevExpress.Xpf.Grid;
using System.ComponentModel.DataAnnotations;
using Uniconta.Common;
using Uniconta.API.GeneralLedger;
using System.Threading.Tasks;
using Uniconta.DataModel;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.Util;
using Uniconta.Common.Utility;
using DevExpress.CodeParser;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class MarkInvoicePaidGrid : CorasauDataGridClient
    {
        public override bool SingleBufferUpdate { get { return false; } }
        public override Type TableType { get { return typeof(MarkInvoicePaidClient); } }
        public override bool Readonly { get { return false; } }
    }

    public class MarkInvoicePaidClient : DebtorTransOpenClient
    {
        double amountPaid;
        [Display(Name = "AmountPaid", ResourceType = typeof(DCOrderText))]
        public double AmountPaid { get { return amountPaid; } set { amountPaid = value; NotifyPropertyChanged("AmountPaid"); } }
        DateTime datePaid;
        [Display(Name = "DatePaid", ResourceType = typeof(DCOrderText))]
        public DateTime DatePaid { get { return datePaid; } set { datePaid = value; NotifyPropertyChanged("DatePaid"); } }
       
        double cashDiscountGiven;
        [Display(Name = "UsedCachDiscount", ResourceType = typeof(GLDailyJournalLineText))]
        public double CashDiscountGiven { get { return cashDiscountGiven; } set { cashDiscountGiven = value; NotifyPropertyChanged("CashDiscountGiven"); } }
    }

    public partial class MarkInvoicePaidPage : GridBasePage
    {
        public override bool IsDataChaged => false;
        public override string NameOfControl
        {
            get { return TabControls.MarkInvoicePaidPage; }
        }

        protected override SortingProperties[] DefaultSort()
        {
            return new[] { new SortingProperties("DueDate"), new SortingProperties("Date") };
        }

        public MarkInvoicePaidPage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        private void InitPage()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgDebtorTransOpen);
            dgDebtorTransOpen.api = api;
            dgDebtorTransOpen.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var Comp = api.CompanyEntity;
            dgDebtorTransOpen.tableView.ShowTotalSummary = true;
            if (Comp.RoundTo100)
                Amount.HasDecimals = AmountOpen.HasDecimals = false;
            dgDebtorTransOpen.FilterString = $"[PostType] = '{Uniconta.ClientTools.Localization.lookup("Invoice")}'";
        }
        private void PART_Editor_Checked(object sender, System.Windows.RoutedEventArgs e)
        {
            var chkPaid = sender as CheckEditor;
            var rec = dgDebtorTransOpen.SelectedItem as MarkInvoicePaidClient;
            if (rec != null)
            {
                if (chkPaid.IsChecked.GetValueOrDefault() && rec.AmountOpen > 0)
                    rec.AmountPaid = rec.AmountOpen;
                else
                    rec.AmountPaid = 0d;
            }
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorTranPage2)
            {
                api.ForcePrimarySQL = true;
                dgDebtorTransOpen.UpdateItemSource(argument);
            }
        }
        public MarkInvoicePaidPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (!api.CompanyEntity.Project)
                Project.Visible = false;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new[] { typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.PaymentTerm), typeof(Uniconta.DataModel.BankStatement), typeof(Uniconta.DataModel.GLDailyJournal) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDebtorTransOpen.SelectedItem as MarkInvoicePaidClient;
            switch (ActionType)
            {
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorTranPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("TransactionOutstanding"), "Edit_16x16");
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgDebtorTransOpen.syncEntity, api, busyIndicator);
                    break;
                case "PostPayments":
                    if (selectedItem != null)
                        PostPayments();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void PostPayments()
        {
            var postingDialog = new CWPostPayment(api);
            postingDialog.Closing += async delegate
            {
                if (postingDialog.DialogResult == true)
                {
                    var masterRecord = (Uniconta.DataModel.GLDailyJournal)api.GetCache(typeof(Uniconta.DataModel.GLDailyJournal)).Get(postingDialog.Journal);
                    if (masterRecord == null)
                    {
                        UtilDisplay.ShowErrorCode(ErrorCodes.FieldCannotBeBlank);
                        return;
                    }

                    if (string.IsNullOrEmpty(postingDialog.Bank))
                    {
                        var cache = api.GetCache(typeof(Uniconta.DataModel.BankStatement));
                        if (cache != null && cache.Count == 1)
                            postingDialog.Bank = cache.First().KeyStr;
                        else
                        {
                            UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Bank"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                            return;
                        }
                    }
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;

                    var glLines = new List<GLDailyJournalLineClient>();
                    var visibleRows = dgDebtorTransOpen.GetVisibleRows();
                    foreach (var row in visibleRows as IEnumerable<MarkInvoicePaidClient>)
                    {
                        if (row.Paid && row.AmountPaid > 0)
                        {
                            var rec = new GLDailyJournalLineClient()
                            {
                                _Account = row.Account,
                                _AccountType = (byte)GLJournalAccountType.Debtor,
                                _Credit = row.AmountPaid,
                                _Date = row.DatePaid != DateTime.MinValue ? row.DatePaid : postingDialog.PayDate,
                                _OffsetAccount = postingDialog.Bank,
                                _Invoice = row.InvoiceAN,
                                _Settlements = NumberConvert.ToString(row.Trans.RowId),
                                _SettleValue = SettleValueType.RowId,
                                _UsedCachDiscount = row.CashDiscountGiven,
                                _Approved = true
                            };
                            if (row.Currency != null && row.AmountOpenCur != null)
                            {
                                rec._Currency = (byte)row.Currency.GetValueOrDefault();
                                rec._CreditCur = row.AmountOpenCur.GetValueOrDefault();
                                rec._Credit = 0;
                            }
                            glLines.Add(rec);
                        }
                    }

                    Task<PostingResult> task;
                    if (postingDialog.IsSimulation)
                        task = new PostingAPI(api).CheckDailyJournal(masterRecord, postingDialog.PayDate, true, new GLTransClientTotal(), 0, false, glLines);
                    else
                        task = new PostingAPI(api).PostDailyJournal(masterRecord, postingDialog.PayDate, postingDialog.Comments, 0, false, glLines);

                    var postingResult = await task;

                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");

                    if (postingResult == null)
                        return;

                    if (postingResult.Err != ErrorCodes.Succes)
                    {
                        Utility.ShowJournalError(postingResult, dgDebtorTransOpen);
                    }
                    else if (postingResult.SimulatedTrans != null && postingResult.SimulatedTrans.Length > 0)
                    {
                        AddDockItem(TabControls.SimulatedTransactions, new object[] { postingResult.AccountBalance, postingResult.SimulatedTrans }, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                    }
                    else
                    {
                        // everything was posted fine
                        string msg;
                        if (postingResult.JournalPostedlId != 0)
                            msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), NumberConvert.ToString(postingResult.JournalPostedlId));
                        else
                            msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));

                        dgDebtorTransOpen.BeginDataUpdate();
                        for (int i = visibleRows.Count; --i >= 0;)
                        {
                            var row = visibleRows[i] as MarkInvoicePaidClient;
                            if (row.Paid)
                            {
                                var visibleIndex = visibleRows.IndexOf(row);
                                dgDebtorTransOpen.tableView.DeleteRow(dgDebtorTransOpen.GetRowHandleByVisibleIndex(visibleIndex));
                            }
                        }
                        dgDebtorTransOpen.EndDataUpdate();
                    }
                }
            };
            postingDialog.Show();
        }
    }
}
