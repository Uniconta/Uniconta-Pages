using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
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
using System.Windows.Threading;
using Uniconta.ClientTools.Util;

using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls.Dialogs;
using DevExpress.Xpf.Grid;
using System.ComponentModel;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class LedgerPostingPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.LedgerPostingPage; } }
        DateTime fromDate;
        DateTime toDate;

        BankStatementAPI bankTransApi;
        Uniconta.DataModel.BankStatement master;
        string showAmountType;

        public LedgerPostingPage(UnicontaBaseEntity sourceData)
            : base(sourceData)
        {
            master = sourceData as Uniconta.DataModel.BankStatement;
            DateTime date = DateTime.Today;
            var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            toDate = firstDayOfMonth.AddMonths(1).AddDays(-1);
            fromDate = firstDayOfMonth.AddMonths(-2);
            InitializeComponent();
            dgBankStatementLine.api = api;
            bankTransApi = new BankStatementAPI(api);
            SetRibbonControl(localMenu, dgBankStatementLine);
            dgBankStatementLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgBankStatementLine.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            State.Header = Uniconta.ClientTools.Localization.lookup("Status");
            SetStatusText();
            Mark.Visible = false;
            GetShowHideGreenMenuItem();
            this.showAmountType = Uniconta.ClientTools.Localization.lookup("All");
        }

        protected override void OnLayoutLoaded()
        {
            setDim();
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dg, lookup);
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            BankStatementLineGridClient oldselectedItem = e.OldItem as BankStatementLineGridClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= selectedItem_PropertyChanged;

            BankStatementLineGridClient selectedItem = e.NewItem as BankStatementLineGridClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += selectedItem_PropertyChanged;
                SetAccountSource(selectedItem);
            }
        }

        SQLCache LedgerCache, DebtorCache, CreditorCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            LedgerCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount));
            if (LedgerCache == null)
                LedgerCache = await Comp.LoadCache(typeof(Uniconta.DataModel.GLAccount), api).ConfigureAwait(false);

            DebtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor));
            if (DebtorCache == null)
                DebtorCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);

            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
            if (CreditorCache == null)
                CreditorCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api).ConfigureAwait(false);
        }

        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            BankStatementLineGridClient selectedItem = dgBankStatementLine.SelectedItem as BankStatementLineGridClient;
            if (selectedItem != null)
            {
                SetAccountSource(selectedItem);
                if (prevAccount != null)
                    prevAccount.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevAccount = editor;
                editor.isValidate = true;
            }
        }
        private void SetAccountSource(BankStatementLineGridClient rec)
        {
            SQLCache cache;
            switch (rec._AccountType)
            {
                case (byte)GLJournalAccountType.Finans:
                    cache = LedgerCache;
                    break;
                case (byte)GLJournalAccountType.Debtor:
                    cache = DebtorCache;
                    break;
                case (byte)GLJournalAccountType.Creditor:
                    cache = CreditorCache;
                    break;
                default: return;
            }

            if (cache != null)
            {
                int ver = cache.version + 10000 * (rec._AccountType + 1);
                if (ver != rec.AccountVersion)
                {
                    rec.AccountVersion = ver;
                    rec.accntSource = cache.GetNotNullArray;
                    rec.NotifyPropertyChanged("AccountSource");
                }
            }
        }

        DCAccount getDCAccount(GLJournalAccountType type, string Acc)
        {
            if (type == GLJournalAccountType.Finans || Acc == null)
                return null;
            var cache = (type == GLJournalAccountType.Debtor) ? DebtorCache : CreditorCache;
            return (DCAccount)cache.Get(Acc);
        }
        DCAccount copyDCAccount(BankStatementLineGridClient rec, GLJournalAccountType type, string Acc)
        {
            var dc = getDCAccount(type, Acc);
            if (dc == null)
                return null;
            if (dc._Dim1 != null)
                rec.Dimension1 = dc._Dim1;
            if (dc._Dim1 != null)
                rec.Dimension2 = dc._Dim2;
            if (dc._Dim1 != null)
                rec.Dimension3 = dc._Dim3;
            if (dc._Dim1 != null)
                rec.Dimension4 = dc._Dim4;
            if (dc._Dim1 != null)
                rec.Dimension5 = dc._Dim5;
            return dc;
        }
        async void GetSearchOpenInvoice(BankStatementLineGridClient line, GLJournalAccountType type)
        {
            InvoiceAPI InvApi = new InvoiceAPI(api);
            string strAccount = (string)await InvApi.SearchOpenInvoice(type, line._Invoice);
            if (strAccount != null)
                line.Account = strAccount;
        }

        void selectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as BankStatementLineGridClient;
            switch (e.PropertyName)
            {
                case "AccountType":
                    SetAccountSource(rec);
                    break;
                case "Invoice":
                    if (rec._Invoice != 0)
                    {
                        GLJournalAccountType type = rec._AmountCent > 0 ? GLJournalAccountType.Debtor : GLJournalAccountType.Creditor;
                        rec.AccountType = AppEnums.GLAccountType.ToString((int)type);
                        GetSearchOpenInvoice(rec, type);
                    }
                    break;
                case "Account":
                    if (rec._AccountType != (byte)GLJournalAccountType.Finans)
                    {
                        var dc = copyDCAccount(rec, (GLJournalAccountType)rec._AccountType, rec._Account);
                        if (dc != null)
                        {
                            rec.Vat = null;
                            rec.VatOperation = null;
                        }
                    }
                    else
                    {
                        var Acc = (GLAccount)LedgerCache.Get(rec._Account);
                        if (Acc != null)
                        {
                            if (Acc._IsDCAccount || Acc._MandatoryTax == VatOptions.NoVat)
                            {
                                rec.Vat = null;
                                rec.VatOperation = null;
                            }
                            else
                            {
                                rec.Vat = Acc._Vat;
                                rec.VatOperation = Acc._VatOperation;
                            }
                        }
                    }
                    break;
            }
        }

        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            var bankStmtLines = (BankStatementLineGridClient[])await bankTransApi.GetTransactions(new BankStatementLineGridClient(), master, fromDate, toDate, true);
            if (showAmountType == Uniconta.ClientTools.Localization.lookup("Debit"))
            {
                var stmtLines = bankStmtLines.Where(x => x._AmountCent >= 0).ToArray();
                bankStmtLines = stmtLines;
            }
            else if (showAmountType == Uniconta.ClientTools.Localization.lookup("Credit"))
            {
                var stmtLines = bankStmtLines.Where(x => x._AmountCent <= 0).ToArray();
                bankStmtLines = stmtLines;
            }
            busyIndicator.IsBusy = false;
            dgBankStatementLine.SetSource(bankStmtLines);

            StartLoadCache();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = getBSLSelecteditem();
            dgBankStatementLine.tableView.CloseEditor();
            switch (ActionType)
            {
                case "Save":
                    saveGrid();
                    break;
                case "Pre":
                    SetFilter(-1);
                    break;
                case "Next":
                    SetFilter(1);
                    break;
                case "Interval":
                    setInterval();
                    break;
                case "RefVoucher":
                    if (selectedItem == null)
                        return;

                    var source = (IList)dgBankStatementLine.ItemsSource;
                    if (source != null)
                    {
                        var _refferedVouchers = new List<int>();
                        IEnumerable<BankStatementLineGridClient> gridItems = source.Cast<BankStatementLineGridClient>();
                        foreach (var statementLine in gridItems)
                            if (statementLine._DocumentRef != 0)
                                _refferedVouchers.Add(statementLine._DocumentRef);

                        AddDockItem(TabControls.AttachVoucherGridPage, new object[1] { _refferedVouchers }, true);
                    }
                    break;
                case "ViewVoucher":
                    if (selectedItem == null)
                        return;
                    dgBankStatementLine.syncEntity.Row = selectedItem;
                    busyIndicator.IsBusy = true;
                    ViewVoucher(TabControls.VouchersPage3, dgBankStatementLine.syncEntity);
                    busyIndicator.IsBusy = false;
                    break;

                case "ImportVoucher":
                    CWAddVouchers addVouvhersDialog = new CWAddVouchers(api);
                    addVouvhersDialog.Show();
                    break;

                case "RemoveVoucher":
                    if (selectedItem == null)
                        return;
                    if (selectedItem._DocumentRef != 0)
                        selectedItem.VoucherReference = 0;
                    else
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoVoucherExist"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                    break;
                case "TransferBankStatement":
                    saveGrid();
                    TransferBankStatementToJournal();
                    break;
                case "ShowHideGreenLines":
                    hideGreen = !hideGreen;
                    setShowHideGreen(hideGreen);
                    string filterString = dgBankStatementLine.FilterString;
                    if (filterString.Contains("[State]"))
                        filterString = "";
                    else
                        filterString = "[State]<2";
                    dgBankStatementLine.FilterString = filterString;
                    break;
                case "ShowAmount":
                    ShowAmountWindow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AttachVoucherGridPage && argument != null)
            {
                var voucherObj = argument as object[];
                
                if(voucherObj[0] is VouchersClient)
                {
                    var voucher = voucherObj[0] as VouchersClient;
                    var selectedItem = dgBankStatementLine.SelectedItem as BankStatementLineGridClient;
                    if (selectedItem != null && voucher.RowId != 0)
                    {
                        dgBankStatementLine.SetLoadedRow(selectedItem);
                        selectedItem.VoucherReference = voucher.RowId;
                        if (voucher._Invoice != 0)
                            selectedItem.Invoice = voucher._Invoice;
                        dgBankStatementLine.SetModifiedRow(selectedItem);
                    }
                }
            }

        }
        private void ShowAmountWindow()
        {
            CWShowAmount winShowAmount = new CWShowAmount();
            winShowAmount.Closing += delegate
            {
                if (winShowAmount.DialogResult == true)
                {
                    showAmountType = winShowAmount.AmountTypeOption;
                    InitQuery();
                }
            };
            winShowAmount.Show();
        }

        ItemBase ibase;
        bool hideGreen = false;
        void GetShowHideGreenMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "ShowHideGreenLines");
        }
        private void setShowHideGreen(bool hideGreen)
        {
            if (ibase == null)
                return;
            if (hideGreen)
            {
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ShowOBJ"), Uniconta.ClientTools.Localization.lookup("Green"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph(";component/Assets/img/ShowGreen_32x32.png");
            }
            else
            {
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("Green"));
                ibase.LargeGlyph = Utilities.Utility.GetGlyph(";component/Assets/img/HideGreen_32x32.png");
            }
        }


        void TransferBankStatementToJournal()
        {
            CWTransferBankStatement winTransfer = new CWTransferBankStatement(fromDate, toDate, api, master, IsLedgerPosting:true);
            winTransfer.Closed += async delegate
            {
                if (winTransfer.DialogResult == true)
                {
                    master._Journal = winTransfer.Journal;
                    PostingAPI pApi = new PostingAPI(api);
                    var res = await pApi.TransferBankStatementToJournal(master, winTransfer.FromDate, winTransfer.ToDate, winTransfer.BankAsOffset, winTransfer.isMarkLine);
                    if (res == ErrorCodes.Succes)
                    {
                        string strmsg = string.Format("{0}; {1}! {2} ?", Uniconta.ClientTools.Localization.lookup("GenerateJournalLines"), Uniconta.ClientTools.Localization.lookup("Completed"),
                            string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("JournalLines"))
                            );
                        var select = UnicontaMessageBox.Show(strmsg, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel);
                        if (select == MessageBoxResult.OK)
                        {
                            var jour = new GLDailyJournalClient();
                            jour._Journal = winTransfer.Journal;
                            var err = await api.Read(jour);
                            if (err == ErrorCodes.Succes)
                                AddDockItem(TabControls.GL_DailyJournalLine, jour, null, null, true);
                        }
                    }
                    else
                        UtilDisplay.ShowErrorCode(res);

                    InitQuery();
                }
            };
            winTransfer.Show();

        }

        void setInterval()
        {
            CWInterval winInterval = new CWInterval(fromDate, toDate);
            winInterval.Closed += delegate
            {
                if (winInterval.DialogResult == true)
                {
                    fromDate = winInterval.FromDate;
                    toDate = winInterval.ToDate;
                    SetStatusText();
                    saveGrid(true);
                }
            };
            winInterval.Show();
        }

        void SetFilter(int step)
        {
            showAmountType = Uniconta.ClientTools.Localization.lookup("All");
            fromDate = fromDate.AddMonths(step);
            var toDate = this.toDate.AddMonths(step);
            this.toDate = toDate.AddDays(DateTime.DaysInMonth(toDate.Year, toDate.Month) - toDate.Day);
            SetStatusText();
            saveGrid(true);
        }

        async void saveGrid(bool doReload = false)
        {
            if (getBSLSelecteditem() != null)
                dgBankStatementLine.SelectedItem = null;

            var err = await dgBankStatementLine.SaveData();
            if (err != ErrorCodes.Succes)
                doReload = false;
            if (doReload)
            {
                dgBankStatementLine.SetSource(null);
                InitQuery();
            }
        }

        BankStatementLineGridClient getBSLSelecteditem()
        {
            return dgBankStatementLine.SelectedItem as BankStatementLineGridClient;
        }
        void SetStatusText()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);

            foreach (var grp in groups)
            {
                if (grp.StatusValue == "a" || grp.Caption == Uniconta.ClientTools.Localization.lookup("FromDate"))
                {
                    grp.StatusValue = fromDate.ToString("d");
                    continue;
                }
                else
                if (grp.StatusValue == "b" || grp.Caption == Uniconta.ClientTools.Localization.lookup("ToDate"))
                {
                    grp.StatusValue = toDate.ToString("d");
                }
            }

        }
        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

    }
}
