using UnicontaClient.Models;
using DevExpress.Data.Filtering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using DevExpress.Xpf.Grid;
using System.Windows;
using Uniconta.API.GeneralLedger;
using UnicontaClient.Utilities;
using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class MatchVoucherBSLinesPage : GridBasePage
    {
        BankStatementClient master;
        ItemBase ibase;
        Orientation orient;
        static bool AssignText;
        int DaysSlip;
        bool ShowCurrency;
        BankStatementAPI bankTransApi;
        DateTime fromDate { get { return BankStatementLinePage.fromDate; } set { BankStatementLinePage.fromDate = value; } }
        DateTime toDate { get { return BankStatementLinePage.toDate; } set { BankStatementLinePage.toDate = value; } }

        public MatchVoucherBSLinesPage(UnicontaBaseEntity master)
            : base(null)
        {
            InitializeComponent();
            dgBSLinesGrid.UpdateMaster(master);
            dgBSLinesGrid.api = api;
            dgVoucherGrid.UpdateMaster(new Uniconta.DataModel.DocumentNoRef());
            dgVoucherGrid.api = api;
            if (fromDate == DateTime.MinValue)
            {
                DateTime date = GetSystemDefaultDate();
                var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
                toDate = date;
                fromDate = firstDayOfMonth.AddMonths(-2);
            }
            bankTransApi = new BankStatementAPI(api);
            dgVoucherGrid.Readonly = true;
            this.master = master as BankStatementClient;
            dgBSLinesGrid.tableView.ShowGroupPanel = false;
            SetRibbonControl(localMenu, dgBSLinesGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgVoucherGrid.RowDoubleClick += dgVoucherGrid_RowDoubleClick;
            dgVoucherGrid.SelectedItemChanged += dgVoucherGrid_SelectedItemChanged;
            GetMenuItem();
            localMenu.OnChecked += LocalMenu_OnChecked;
            orient = api.session.Preference.BankStatementHorisontal ? Orientation.Horizontal : Orientation.Vertical;
            lGroup.Orientation = orient;
            dgBSLinesGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            State.Header = Uniconta.ClientTools.Localization.lookup("Status");
            dgBSLinesGrid.View.SearchControl = localMenu.SearchControl;
            this.PreviewKeyDown += RootVisual_KeyDown;
            this.BeforeClose += BankStatementLinePage_BeforeClose;
            UpdateMaster();
        }
        void UpdateMaster()
        {
            bool RoundTo100;
            var Comp = api.CompanyEntity;
            DaysSlip = master._DaysSlip;
            if (Comp.SameCurrency(master._Currency))
                RoundTo100 = Comp.RoundTo100;
            else
            {
                ShowCurrency = true;
                RoundTo100 = !CurrencyUtil.HasDecimals(master._Currency);
            }
            if (RoundTo100)
            {
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = false;
            }
            if (!Comp._UseVatOperation)
            {
                colVatOperation.Visible = VatOperation.Visible = false;
            }
        }
        public override void PageClosing()
        {
            if (dgBSLinesGrid.IsAutoSave && IsDataChaged)
                saveGrid();
            base.PageClosing();
        }
        private void BankStatementLinePage_BeforeClose()
        {
            this.PreviewKeyDown -= RootVisual_KeyDown;
        }
        private void RootVisual_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (dgBSLinesGrid.CurrentColumn.Name == "HasOffsetAccounts" && e.Key == Key.Down)
                {
                    var currentRow = dgBSLinesGrid.SelectedItem as BankStatementLineGridClient;
                    if (currentRow != null)
                        CallOffsetAccount(currentRow);
                }
            }

        }
        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            BankStatementLineGridClient oldselectedItem = e.OldItem as BankStatementLineGridClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= BankStatementLine_PropertyChanged;
            BankStatementLineGridClient selectedItem = e.NewItem as BankStatementLineGridClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += BankStatementLine_PropertyChanged;
        }
        SQLCache LedgerCache, DebtorCache, CreditorCache, ChargeGrp;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            LedgerCache = api.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            ChargeGrp = api.GetCache(typeof(Uniconta.DataModel.GLChargeGroup)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLChargeGroup)).ConfigureAwait(false);
        }
        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            BankStatementLineGridClient selectedItem = dgBSLinesGrid.SelectedItem as BankStatementLineGridClient;
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
            var InvApi = new Uniconta.API.DebtorCreditor.InvoiceAPI(api);
            string strAccount = (string)await InvApi.SearchOpenInvoice(type, line._Invoice);
            if (strAccount != null)
                line.Account = strAccount;
        }
        double bankStatAmt = 0d;
        void BankStatementLine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as BankStatementLineGridClient;
            switch (e.PropertyName)
            {
                case "AccountType":
                    SetAccountSource(rec);
                    break;
                case "Invoice":
                    if (rec._Invoice != null)
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
                case "Charge":
                    var grp = (GLChargeGroup)ChargeGrp?.Get(rec._Charge);
                    rec.ChargeAmount = grp != null ? Math.Round(grp._Amount + (Math.Abs(rec.Amount) + grp._Amount) * grp._Pct / Math.Max(100 - grp._Pct, 1), 2) : 0;
                    break;
                case "Mark":
                    var lstBst = ((IEnumerable<BankStatementLineGridClient>)dgBSLinesGrid.ItemsSource);
                    var val = (from t in lstBst where t._Mark select t._AmountCent).Sum();
                    bankStatAmt = (val / 100d);
                    break;
                case "DocumentRef":
                    dgBSLinesGrid.SetLoadedRow(rec);
                    break;
            }
        }

        public override bool IsDataChaged
        {
            get
            {
                var lstbsl = ((IEnumerable<BankStatementLineGridClient>)dgBSLinesGrid.ItemsSource);
                if (lstbsl != null)
                {
                    foreach (var l in lstbsl)
                        if (l.Updated)
                            return true;
                }
                return base.IsDataChaged;
            }
        }
        private void HasOffSetAccount_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallOffsetAccount((sender as Image).Tag as BankStatementLineGridClient);
        }

        void CallOffsetAccount(BankStatementLineGridClient line)
        {
            if (line != null)
            {
                dgBSLinesGrid.SetLoadedRow(line);
                var header = string.Format("{0}:{1} {2}", Uniconta.ClientTools.Localization.lookup("OffsetAccountTemplate"), Uniconta.ClientTools.Localization.lookup("BankStatement"), line._Account);
                AddDockItem(TabControls.GLOffsetAccountTemplate, line, header: header);
            }
        }
        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "Unlinked");
            var rbMenuAssignText = UtilDisplay.GetMenuCommandByName(rb, "AssignText");
            rbMenuAssignText.IsChecked = AssignText;
        }
        private void dgVoucherGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var vc = dgVoucherGrid.SelectedItem as VouchersClientLocal;
            if (vc != null && vc.PrimaryKeyId != 0)
            {
                var visibleRows = dgBSLinesGrid.GetVisibleRows() as IEnumerable<BankStatementLineGridClient>;
                dgBSLinesGrid.SelectedItem = visibleRows.Where(v => v._DocumentRef == vc.PrimaryKeyId).FirstOrDefault();
            }
        }

        private void dgBSLinesGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var gl = dgBSLinesGrid.SelectedItem as BankStatementLineGridClient;
            if (gl != null && gl._DocumentRef != 0)
            {
                var visibleRows = dgVoucherGrid.GetVisibleRows() as IEnumerable<VouchersClientLocal>;
                dgVoucherGrid.SelectedItem = visibleRows.Where(v => v.PrimaryKeyId == gl._DocumentRef).FirstOrDefault();
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            SetDimensions();
        }

        void dgVoucherGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("Attach");
        }

        async void SaveAndRefresh(bool isAttached, BankStatementLineGridClient jour, VouchersClientLocal voucher)
        {
            busyIndicator.IsBusy = true;
            var err = await dgBSLinesGrid.SaveData();
            if (err == ErrorCodes.Succes)
            {
                dgBSLinesGrid.UpdateItemSource(2, jour);
                dgVoucherGrid.UpdateItemSource(2, voucher);
            }
            if (!isAttached)
                SetVoucherIsAttached();
            busyIndicator.IsBusy = false;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedVoucher = dgVoucherGrid.SelectedItem as VouchersClientLocal;
            var selectedLine = dgBSLinesGrid.SelectedItem as BankStatementLineGridClient;

            switch (ActionType)
            {
                case "ViewPhysicalVoucher":
                case "ViewVoucher":
                    if (selectedVoucher != null)
                        ViewVoucher(selectedVoucher);
                    break;
                case "ViewAttachedVoucher":
                    if (selectedLine != null)
                        ViewVoucher(selectedLine);
                    break;
                case "Attach":
                    if (selectedVoucher != null && selectedLine != null)
                        Attach(selectedVoucher, selectedLine, false);
                    break;
                case "Detach":
                    if (selectedLine == null)
                        return;
                    if (selectedLine._DocumentRef != 0)
                    {
                        dgBSLinesGrid.SetLoadedRow(selectedLine);
                        selectedLine.DocumentRef = 0;
                        dgBSLinesGrid.SetModifiedRow(selectedLine);
                        SaveAndRefresh(false, selectedLine, selectedVoucher);
                    }
                    else
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoVoucherExist"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                    break;
                case "Unlinked":
                    SetUnlinkedAndLinkedAll();
                    break;
                case "AutoMatch":
                    AutoMatch();
                    break;
                case "SaveLines":
                    SaveLines();
                    break;
                case "ChangeOrientation":
                    orient = 1 - orient;
                    lGroup.Orientation = orient;
                    api.session.Preference.BankStatementHorisontal = (orient == Orientation.Horizontal);
                    break;
                case "Interval":
                    setInterval();
                    break;
                case "SendVoucherReminder":
                    if (selectedLine != null)
                        Utility.SendVoucherReminder(api, selectedLine._Date, selectedLine._AmountCur != 0 ? selectedLine._AmountCur : selectedLine._Amount, selectedLine._Currency, selectedLine._Text);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        void setInterval()
        {
            CWInterval winInterval = new CWInterval(fromDate, toDate, DaysSlip, hideVoucher: true);
            winInterval.Closed += delegate
            {
                if (winInterval.DialogResult == true)
                {
                    fromDate = winInterval.FromDate;
                    toDate = winInterval.ToDate;
                    BindBankStatementLines();
                }
            };
            winInterval.Show();
        }
        private void SaveLines()
        {
            dgBSLinesGrid.SaveData();
        }
        private void Attach(VouchersClientLocal selectedVoucher, BankStatementLineGridClient selectedLine, bool isAuto, IEnumerable<VouchersClientLocal> visibleRows = null)
        {
            var selectedRowId = selectedVoucher.RowId;
            if (selectedRowId != 0 && selectedLine._DocumentRef == 0)
            {
                dgBSLinesGrid.SetLoadedRow(selectedLine);
                selectedLine.DocumentRef = selectedRowId;
                // selectedLine.DocumentDate = selectedVoucher._DocumentDate;
                if (AssignText)
                    selectedLine.Text = selectedLine._Text ?? selectedVoucher._Text;
                var amount = selectedLine.Amount;
                if (selectedVoucher._Dim1 != null)
                    selectedLine.Dimension1 = selectedVoucher._Dim1;
                if (selectedVoucher._Dim2 != null)
                    selectedLine.Dimension2 = selectedVoucher._Dim2;
                if (selectedVoucher._Dim3 != null)
                    selectedLine.Dimension3 = selectedVoucher._Dim3;
                if (selectedVoucher._Dim4 != null)
                    selectedLine.Dimension4 = selectedVoucher._Dim4;
                if (selectedVoucher._Dim5 != null)
                    selectedLine.Dimension5 = selectedVoucher._Dim5;
                if (selectedVoucher._CostAccount != null && selectedLine._Account == null)
                {
                    selectedLine._AccountType = 0;
                    selectedLine.Account = selectedVoucher._CostAccount;
                }

                // selectedLine.Amount = amount != 0d ? amount : selectedVoucher._Amount;
                //dgBSLinesGrid.SetModifiedRow(selectedLine);
                if (visibleRows == null)
                    visibleRows = dgVoucherGrid.GetVisibleRows() as IEnumerable<VouchersClientLocal>;
                var voucher = visibleRows.Where(v => v.PrimaryKeyId == selectedRowId).FirstOrDefault();
                if (voucher != null)
                    voucher.IsAttached = true;

                if (!isAuto)
                    SaveAndRefresh(true, selectedLine, selectedVoucher);
            }
        }

        async void AutoMatch()
        {
            var Lines = dgBSLinesGrid.ItemsSource as IEnumerable<BankStatementLineGridClient>;
            var visibleRowVouchers = dgVoucherGrid.GetVisibleRows() as IEnumerable<VouchersClientLocal>;
            int rowCountUpdate = 0;
            for (int i = 0; (i < 2); i++)
            {
                foreach (var voucher in visibleRowVouchers)
                {
                    if (!voucher.IsAttached && voucher._Amount != 0)
                    {
                        DateTime date;
                        if (i == 0)
                            date = voucher._PostingDate != DateTime.MinValue ? voucher._PostingDate :
                                (voucher._DocumentDate != DateTime.MinValue ? voucher._DocumentDate : voucher.Created.Date);
                        else
                            date = voucher._PayDate;
                        if (date != DateTime.MinValue)
                        {
                            var amount = Math.Abs(voucher._Amount);
                            foreach (var p in Lines)
                                if (Math.Abs(p.Amount) == amount && p._Date == date)
                                {
                                    Attach(voucher, p, true, visibleRowVouchers);
                                    rowCountUpdate++;
                                    break;
                                }
                        }
                    }
                }
            }

            if (rowCountUpdate > 0)
            {
                busyIndicator.IsBusy = true;

                var err = await dgBSLinesGrid.SaveData();
                if (err == ErrorCodes.Succes)
                {
                    dgBSLinesGrid.RefreshData();
                    UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("NumberOfRecords"), ": ", rowCountUpdate),
                        Uniconta.ClientTools.Localization.lookup("Information"));
                }
                busyIndicator.IsBusy = false;
            }
        }

        private void LocalMenu_OnChecked(string ActionType, bool IsChecked)
        {
            switch (ActionType)
            {
                case "AssignText":
                    AssignText = IsChecked;
                    break;
            }
        }
        private void SetUnlinkedAndLinkedAll()
        {
            if (ibase == null)
                return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("Unlinked"))
            {
                Unlinked(true);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("All");
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("Account-statement_32x32");
            }
            else if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("All"))
            {
                Unlinked(false);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("Unlinked");
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("Check_Journal_32x32");
            }
        }

        void Unlinked(bool unlink)
        {
            if (unlink)
            {
                dgVoucherGrid.MergeColumnFilters("([IsAttached] = False)");
                dgBSLinesGrid.MergeColumnFilters("([HasVoucher] = False)");
            }
            else
            {
                dgVoucherGrid.ClearColumnFilter("IsAttached");
                dgBSLinesGrid.ClearColumnFilter("HasVoucher");
            }
        }

        private void ViewVoucher(UnicontaBaseEntity baseEntity)
        {
            busyIndicator.IsBusy = true;
            string header = null;
            if (baseEntity is VouchersClientLocal)
            {
                header = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("View"), Uniconta.ClientTools.Localization.lookup("PhysicalVoucher"));
                ViewVoucher(TabControls.VouchersPage3, dgVoucherGrid.syncEntity, header);
            }
            else if (baseEntity is BankStatementLineGridClient)
            {
                header = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("View"), Uniconta.ClientTools.Localization.lookup("AttachedVoucher"));
                ViewVoucher(TabControls.VouchersPage3, dgBSLinesGrid.syncEntity, header);
            }
            busyIndicator.IsBusy = false;
        }

        public override Task InitQuery()
        {
            dgVoucherGrid.Filter(null);
            return BindBankStatementLines();
        }
        async Task BindBankStatementLines()
        {
            busyIndicator.IsBusy = true;
            var bankStmtLines = (BankStatementLineGridClient[])await bankTransApi.GetTransactions(new BankStatementLineGridClient(), master, fromDate, toDate, true, false);
            dgBSLinesGrid.SetSource(bankStmtLines);
            SetVoucherIsAttached();
            busyIndicator.IsBusy = false;
        }
        private void SetVoucherIsAttached()
        {
            var docRefs = new HashSet<int>();
            var bsLines = dgBSLinesGrid.ItemsSource as IEnumerable<BankStatementLineGridClient>;
            if (bsLines != null)
            {
                foreach (var rec in bsLines)
                    if (rec._DocumentRef != 0)
                        docRefs.Add(rec._DocumentRef);
            }
            if (docRefs.Count > 0)
            {
                var vouchers = dgVoucherGrid.ItemsSource as IEnumerable<VouchersClientLocal>;
                if (vouchers != null)
                {
                    foreach (var voucher in vouchers)
                        voucher.IsAttached = docRefs.Contains(voucher.RowId);
                }
            }
        }

        private void SetDimensions()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, clDim1, clDim2, clDim3, clDim4, clDim5);
        }

        public override void AssignMultipleGrid(List<CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgBSLinesGrid);
            gridCtrls.Add(dgVoucherGrid);
        }

        public override string NameOfControl
        {
            get { return TabControls.MatchPhysicalVoucherToBSLines; }
        }
    }
}
