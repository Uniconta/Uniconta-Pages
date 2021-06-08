using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using System.Collections;
using Uniconta.Common;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.Common.Utility;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class OpenTransactionGrid : CorasauDataGridClient
    {
        public Type OpenTransactionType;
        public override Type TableType { get { return OpenTransactionType; } }
    }

    /// <summary>
    /// Interaction logic for SettleOpenTransactionPage.xaml
    /// </summary>
    public partial class SettleOpenTransactionPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.SettleOpenTransactionPage; } }
        public override void PageClosing()
        {
            globalEvents.OnRefresh(NameOfControl, refreshParams);
            base.PageClosing();
        }
        private List<string> selectedInvoices;
        private List<int> selectedVouchers;
        private List<int> selectedRowIds;

        int OpenTransactionType;
        private string settlement;
        private double RemainingAmt, RemainingAmtCur;
        string settleCur;
        GLDailyJournalLineClient SelectedJournalLine;
        BankStatementLineClient SelectedBankStatemenLine;
        bool OffSet, RoundTo100;
        private object[] refreshParams;
        private List<long> markedlist;
        public SettleOpenTransactionPage(UnicontaBaseEntity baseEntity, byte openTransType, UnicontaBaseEntity selectedjournalLine, bool offSet, IEnumerable<string> markedList) : base(null)
        {
            InitializeComponent();
            this.DataContext = this;
            this.OffSet = offSet;
            this.OpenTransactionType = openTransType;
            InitializePage(baseEntity, openTransType);
            if (selectedjournalLine is GLDailyJournalLineClient)
                SelectedJournalLine = selectedjournalLine as GLDailyJournalLineClient;
            else if (selectedjournalLine is BankStatementLineClient)
                SelectedBankStatemenLine = selectedjournalLine as BankStatementLineClient;

            ((DevExpress.Xpf.Grid.TableView)dgOpenTransactionGrid.View).RowStyle = Application.Current.Resources["DisableStyleRow"] as Style;
            InitGridView(markedList);
        }

        private void InitializePage(UnicontaBaseEntity baseEntity, byte openTransType)
        {
            settlement = string.Empty;
            selectedInvoices = new List<string>();
            selectedVouchers = new List<int>();
            selectedRowIds = new List<int>();
            dgOpenTransactionGrid.api = api;
            dgOpenTransactionGrid.UpdateMaster(baseEntity);
            dgOpenTransactionGrid.BusyIndicator = busyIndicator;
            dgOpenTransactionGrid.OpenTransactionType = openTransType == 1 ? typeof(DebtorTransOpenClientExtended) : typeof(CreditorTransOpenClientExtended);
            SetRibbonControl(localMenu, dgOpenTransactionGrid);
            if (openTransType == 1)
            {
                PaymentId.Visible = PaymentId.ShowInColumnChooser = false;
                PaymentMethod.Visible = PaymentMethod.ShowInColumnChooser = false;
            }
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            var Comp = api.CompanyEntity;
            RoundTo100 = Comp.RoundTo100;

#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += SettleOpenTransactionPage_KeyDown;
#else
            this.PreviewKeyDown += SettleOpenTransactionPage_KeyDown;
#endif
            this.BeforeClose += SettleOpenTransactionPage_BeforeClose;
        }

        protected override void OnLayoutLoaded()
        {
            var winSetting = BasePage.session.Preference.GetWindowSetting(TabControls.SettleOpenTransactionPage);
            if (winSetting == null)
            {
                var curpanel = dockCtrl?.Activpanel;
                if (curpanel != null && curpanel.IsFloating)
                    curpanel.MinWidth = 1400;
                curpanel?.UpdateLayout();
            }
            base.OnLayoutLoaded();
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorTranPage2 || screenName == TabControls.CreditorTranOpenPage2)
                dgOpenTransactionGrid.UpdateItemSource(argument);
        }

        public override Task InitQuery()
        {
            return null;
        }
        private void SettleOpenTransactionPage_BeforeClose()
        {
            refreshParams = null;
        }

        private void InitGridView(IEnumerable<string> markedList)
        {
            var settle = SelectedJournalLine?._Settlements ?? SelectedBankStatemenLine?._Settlement;
            if (settle != null)
            {
                var settlements = settle.Split(';');

                if (SelectedJournalLine != null)
                {
                    var SettleValue = SelectedJournalLine._SettleValue;
                    foreach (var p in settlements)
                    {
                        if (string.IsNullOrWhiteSpace(p))
                            continue;
                        if (SettleValue == Uniconta.DataModel.SettleValueType.Invoice)
                            selectedInvoices.Add(p);
                        else
                        {
                            var n = NumberConvert.ToInt(p);
                            if (n == 0)
                                continue;
                            if (SettleValue == Uniconta.DataModel.SettleValueType.Voucher)
                                selectedVouchers.Add((int)n);
                            else if (SettleValue == Uniconta.DataModel.SettleValueType.RowId)
                                selectedRowIds.Add((int)n);
                        }
                    }
                }
                else if (SelectedBankStatemenLine != null)
                    selectedInvoices.AddRange(settlements);
            }
            else
            {
                if (!string.IsNullOrEmpty(SelectedJournalLine?._Invoice))
                    selectedInvoices.Add(SelectedJournalLine._Invoice);
                else if (!string.IsNullOrEmpty(SelectedBankStatemenLine?._Invoice))
                    selectedInvoices.Add(SelectedBankStatemenLine._Invoice);
            }

            markedlist = GetAllReadyMarked(markedList);

            if (OpenTransactionType == 1)
                LoadDebtorTransOpen();
            else if (OpenTransactionType == 2)
                LoadCreditorTransOpen();
            dgOpenTransactionGrid.Focus();

           
        }

        async private void LoadDebtorTransOpen()
        {
            var listSource = await api.Query(new DebtorTransOpenClientExtended(), dgOpenTransactionGrid.masterRecords, null);
            if (listSource != null)
            {
                foreach (var item in listSource)
                {
                    var Voucher = item.Voucher;
                    var Invoice = item.Invoice;
                    var InvoiceAN = item.InvoiceAN;
                    var rowId = item.PrimaryKeyId;

                    item.IsEnabled = !(markedlist != null && (markedlist.Contains(Voucher) || markedlist.Contains(Invoice) || markedlist.Contains(rowId)));
                    item.IsChecked = (selectedVouchers.Contains(Voucher) || selectedInvoices.Contains(InvoiceAN) || markedlist.Contains(Voucher) || markedlist.Contains(Invoice) || selectedRowIds.Contains(rowId));

                    if (selectedVouchers.Contains(Voucher))
                        selectedRowIds.Add(rowId);

                    if (item.IsChecked && item.IsEnabled)
                        CheckBoxClicked(item);
                }
            }
            dgOpenTransactionGrid.SetSource(listSource);
        }

        async private void LoadCreditorTransOpen()
        {
            var listSource = await api.Query(new CreditorTransOpenClientExtended(), dgOpenTransactionGrid.masterRecords, null);
            if (listSource != null)
            {
                foreach (var item in listSource)
                {
                    var Voucher = item.Voucher;
                    var Invoice = item.Invoice;
                    var InvoiceAN = item.InvoiceAN;
                    var rowId = item.PrimaryKeyId;

                    item.IsEnabled = !(markedlist != null && (markedlist.Contains(Voucher) || markedlist.Contains(Invoice) || markedlist.Contains(rowId)));
                    item.IsChecked = (selectedVouchers.Contains(Voucher) || selectedInvoices.Contains(InvoiceAN) || markedlist.Contains(Voucher) || markedlist.Contains(Invoice) || selectedRowIds.Contains(rowId));

                    if (selectedVouchers.Contains(Voucher))
                        selectedRowIds.Add(rowId);

                    if (item.IsChecked && item.IsEnabled)
                        CheckBoxClicked(item);
                }
            }
            dgOpenTransactionGrid.SetSource(listSource);
        }

        private void SettleOpenTransactionPage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.F8)
            {
                Generate();
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                    MultiFlip();
                else
                    FlipSelection();
                e.Handled = true;
            }
        }

        void FlipSelection()
        {
            if (dgOpenTransactionGrid.SelectedItem == null)
                return;
            DebtorTransOpenClientExtended selecteddto = null;
            CreditorTransOpenClientExtended selectedcto = null;
            selecteddto = dgOpenTransactionGrid.SelectedItem as DebtorTransOpenClientExtended;
            if (selecteddto != null)
            {
                selecteddto.IsChecked = !selecteddto.IsChecked;
                CheckBoxClicked(selecteddto);
            }
            else
            {
                selectedcto = dgOpenTransactionGrid.SelectedItem as CreditorTransOpenClientExtended;
                selectedcto.IsChecked = !selectedcto.IsChecked;
                CheckBoxClicked(selectedcto);
            }
        }

        void MultiFlip()
        {
            if (dgOpenTransactionGrid.SelectedItem == null)
                return;
            DebtorTransOpenClientExtended selecteddto = null;
            CreditorTransOpenClientExtended selectedcto = null;
            selecteddto = dgOpenTransactionGrid.SelectedItem as DebtorTransOpenClientExtended;
            var rowIndex = dgOpenTransactionGrid.tableView.FocusedRowHandle;
            if (selecteddto != null)
            {
                bool isChecked = selecteddto.IsChecked;
                selecteddto.IsChecked = !selecteddto.IsChecked;
                CheckBoxClicked(selecteddto);
                for (int i = rowIndex - 1; i > 0; i--)
                {
                    var row = dgOpenTransactionGrid.GetRow(i) as DebtorTransOpenClientExtended;
                    if (row?.IsChecked == isChecked)
                    {
                        row.IsChecked = !row.IsChecked;
                        CheckBoxClicked(row);
                    }
                    else
                        break;
                }
            }
            else
            {
                selectedcto = dgOpenTransactionGrid.SelectedItem as CreditorTransOpenClientExtended;
                bool isChecked = selectedcto.IsChecked;
                selectedcto.IsChecked = !selectedcto.IsChecked;
                CheckBoxClicked(selectedcto);
                for (int i = rowIndex - 1; i > 0; i--)
                {
                    var row = dgOpenTransactionGrid.GetRow(i) as CreditorTransOpenClientExtended;
                    if (row?.IsChecked == isChecked)
                    {
                        row.IsChecked = !row.IsChecked;
                        CheckBoxClicked(row);
                    }
                    else
                        break;
                }
            }
        }

        private List<long> GetAllReadyMarked(IEnumerable<string> marked)
        {
            var markedList = new List<long>();
            if (marked != null && marked.Count() > 0)
            {
                foreach (var mark in marked)
                {
                    string[] markStr = mark.Split(';');
                    markedList.AddRange(markStr.Where(p => !string.IsNullOrEmpty(p)).Select(i => NumberConvert.GetOnlyNumbers(i)));
                }
            }
            return markedList;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgOpenTransactionGrid.SelectedItem;
            switch (ActionType)
            {
                case "Generate":
                    Generate();
                    break;
                case "Cancel":
                    Close();
                    break;
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgOpenTransactionGrid.syncEntity, api, busyIndicator);
                    break;
                case "EditRow":
                    if (selectedItem != null)
                    {
                        if (selectedItem is DebtorTransOpenClientExtended)
                            AddDockItem(TabControls.DebtorTranPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("TransactionOutstanding"), "Edit_16x16.png");
                        else if (selectedItem is CreditorTransOpenClientExtended)
                            AddDockItem(TabControls.CreditorTranOpenPage2, selectedItem, Uniconta.ClientTools.Localization.lookup("AmountToPay"), "Edit_16x16.png");
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }


        private void Close()
        {
            refreshParams = null;
            CloseDockItem();
        }
        private void Generate()
        {
            dgOpenTransactionGrid.tableView.CloseEditor();

            if (selectedInvoices != null && selectedRowIds != null)
            {
                var sb = StringBuilderReuse.Create();
                if (selectedInvoices.Count > 0)
                {
                    sb.Append("I:");
                    foreach (var inv in selectedInvoices)
                        sb.Append(inv).Append(';');
                }
                else if (selectedRowIds.Count > 0)
                {
                    sb.Append("R:");
                    foreach (var row in selectedRowIds)
                        sb.AppendNum(row).Append(';');
                }

                var len = sb.Length;

                if (len > 0 && sb[len - 1] == ';') // remove the last
                    sb.Length--;

                settlement = sb.ToStringAndRelease();

                if (SelectedJournalLine != null)
                {
                    refreshParams = new object[6];
                    refreshParams[0] = SelectedJournalLine;
                    refreshParams[1] = settlement;
                    refreshParams[2] = RemainingAmt;
                    refreshParams[3] = RemainingAmtCur;
                    refreshParams[4] = settleCur;
                    refreshParams[5] = OffSet;
                }
                else if (SelectedBankStatemenLine != null)
                {
                    refreshParams = new object[2];
                    refreshParams[0] = SelectedBankStatemenLine;
                    refreshParams[1] = settlement;
                }
                CloseDockItem();
            }
        }

        bool ResetFields()
        {
            if (selectedVouchers.Count == 0 && selectedInvoices.Count == 0 && selectedRowIds.Count == 0)
            {
                settleCur = null;
                RemainingAmt = 0;
                RemainingAmtCur = 0;
                return true;
            }
            return false;
        }

        void Check(int voucher, string invoice, int rowId, double amtOpen, Currencies? currency, double? amtOpenCur)
        {
            bool isFirst = ResetFields();

            if ((OpenTransactionType == 1 && amtOpen < 0d) || // we want to insert creditnotes and payments first.
                (OpenTransactionType == 2 && amtOpen > 0d))
            {
                if (!selectedVouchers.Contains(voucher))
                    selectedVouchers.Insert(0, voucher);
                if (!string.IsNullOrEmpty(invoice) && !selectedInvoices.Contains(invoice))
                    selectedInvoices.Insert(0, invoice);
                if (!selectedRowIds.Contains(rowId))
                    selectedRowIds.Insert(0, rowId);
            }
            else
            {
                if (!selectedVouchers.Contains(voucher))
                    selectedVouchers.Add(voucher);
                if (!string.IsNullOrEmpty(invoice) && !selectedInvoices.Contains(invoice))
                    selectedInvoices.Add(invoice);
                if (!selectedRowIds.Contains(rowId))
                    selectedRowIds.Insert(0, rowId);
            }

            RemainingAmt += amtOpen;

            if (isFirst && currency != null)
                settleCur = currency.ToString();

            if (settleCur != null)
            {
                if (settleCur == currency.ToString())
                {
                    RemainingAmtCur += Convert.ToDouble(amtOpenCur);
                }
                else
                {
                    RemainingAmtCur = 0d;
                    settleCur = null;
                }
            }
            SetStatusText(RemainingAmt, RemainingAmtCur);
        }

        void UnCheck(int voucher, string invoice, int rowId, double amtOpen, Currencies? currency, double? amtOpenCur)
        {
            bool isFirst = ResetFields();

            selectedVouchers.Remove(voucher);
            selectedInvoices.Remove(invoice);
            selectedRowIds.Remove(rowId);
            RemainingAmt -= amtOpen;

            if (settleCur != null)
                RemainingAmtCur -= Convert.ToDouble(amtOpenCur);

            SetStatusText(RemainingAmt, RemainingAmtCur);
        }
        private void HeaderCheckEditor_Checked(object sender, RoutedEventArgs e)
        {
            AllCheckMarked(true);
        }

        private void HeaderCheckEditor_UnChecked(object sender, RoutedEventArgs e)
        {
            AllCheckMarked(false);
        }

        void AllCheckMarked(bool value)
        {
            var sourceDebTran = dgOpenTransactionGrid.ItemsSource as IEnumerable<DebtorTransOpenClientExtended>;
            IEnumerable<CreditorTransOpenClientExtended> sourceCredTran = null;
            if (sourceDebTran == null)
                sourceCredTran = dgOpenTransactionGrid.ItemsSource as IEnumerable<CreditorTransOpenClientExtended>;
            if (sourceDebTran != null)
            {
                foreach (var row in sourceDebTran)
                {
                    row.IsChecked = value;
                    CheckBoxClicked(row);
                }
            }
            else if (sourceCredTran != null)
            {
                foreach (var row in sourceCredTran)
                {
                    row.IsChecked = value;
                    CheckBoxClicked(row);
                }
            }
        }
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            var chk = sender as CheckBox;
            CheckBoxClicked(chk.Tag);
        }

        private void CheckBoxClicked(object selectedItem)
        {
            var debrow = selectedItem as DebtorTransOpenClientExtended;
            bool isChecked;
            if (debrow != null)
                isChecked = debrow.IsChecked;
            else
            {
                var crerow = selectedItem as CreditorTransOpenClientExtended;
                if (crerow != null)
                    isChecked = crerow.IsChecked;
                else
                    return;
            }
            var row = selectedItem as DCTransOpenClient;
            var amountOpen = row.AmountOpen;
            if (row._CashDiscount != 0)
            {
                DateTime dt = (SelectedJournalLine != null) ? SelectedJournalLine._Date : (SelectedBankStatemenLine != null ? SelectedBankStatemenLine._Date : DateTime.MinValue);
                if (dt <= row._CashDiscountDate)
                {
                    if (amountOpen > 0)
                        amountOpen -= Math.Abs(row._CashDiscount);
                    else
                        amountOpen += Math.Abs(row._CashDiscount);
                }
            }
            if (isChecked)
                Check(row.Voucher, row.InvoiceAN, row.PrimaryKeyId, amountOpen, row.Currency, row.AmountOpenCur);
            else
                UnCheck(row.Voucher, row.InvoiceAN, row.PrimaryKeyId, amountOpen, row.Currency, row.AmountOpenCur);
        }

        public override bool IsDataChaged { get { return false; } }

        void SetStatusText(double amount, double amountCur)
        {
            string format = RoundTo100 ? "N0" : "N2";
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            var amountTxt = Uniconta.ClientTools.Localization.lookup("Amount");
            var amountCurTxt = Uniconta.ClientTools.Localization.lookup("AmountCur");

            foreach (var grp in groups)
            {
                if (grp.Caption == amountTxt)
                    grp.StatusValue = amount.ToString(format);
                else if (grp.Caption == amountCurTxt)
                    grp.StatusValue = amountCur.ToString(format);
            }
        }

        private void HasVoucherImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            busyIndicator.IsBusy = true;
            ViewVoucher(TabControls.VouchersPage3, dgOpenTransactionGrid.syncEntity);
            busyIndicator.IsBusy = false;
        }
    }

    public class DebtorTransOpenClientExtended : DebtorTransOpenClient
    {
        public bool IsEnabled { get; set; }
        bool _ischecked;
        public bool IsChecked { get { return _ischecked; } set { _ischecked = value; NotifyPropertyChanged("IsChecked"); } }
    }

    public class CreditorTransOpenClientExtended : CreditorTransOpenClient
    {
        public bool IsEnabled { get; set; }
        bool _ischecked;
        public bool IsChecked { get { return _ischecked; } set { _ischecked = value; NotifyPropertyChanged("IsChecked"); } }
    }
}
