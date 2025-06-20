using UnicontaClient.Models;
using DevExpress.Data.Filtering;
using System;
using System.Collections.Generic;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VouchersGridLocal : VouchersGrid
    {
        public override Type TableType { get { return typeof(VouchersClientLocal); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public class GLDailyJournalLineGridLocal : GLDailyJournalLineGrid
    {
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class MatchPhysicalVoucherToGLDailyJournalLines : GridBasePage
    {
        GLDailyJournalClient masterRecord;
        ItemBase ibase;
        static bool AssignText;
        System.Windows.Controls.Orientation orient;
        bool TwoVatCodes, UseDCVat;
        public IEnumerable<JournalLineGridClient> JournalLines { get; set; }
        public MatchPhysicalVoucherToGLDailyJournalLines(UnicontaBaseEntity master)
            : base(null)
        {
            InitializeComponent();
            dgGldailyJournalLinesGrid.UpdateMaster(master);
            dgGldailyJournalLinesGrid.api = api;
            dgvoucherGrid.UpdateMaster(new Uniconta.DataModel.DocumentNoRef());
            dgvoucherGrid.api = api;
            dgvoucherGrid.Readonly = true;
            masterRecord = master as GLDailyJournalClient;
            dgGldailyJournalLinesGrid._AutoSave = masterRecord._AutoSave;
            dgGldailyJournalLinesGrid.tableView.ShowGroupPanel = false;
            var Comp = api.CompanyEntity;
            if (!Comp._UseVatOperation)
                VatOperation.Visible = VatOffsetOperation.Visible = false;
            if (!masterRecord._TwoVatCodes)
                OffsetVat.Visible = VatOffsetOperation.Visible = VatAmountOffset.Visible = false;
            else
                TwoVatCodes = true;
            SetRibbonControl(localMenu, dgGldailyJournalLinesGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgvoucherGrid.RowDoubleClick += dgvoucherGrid_RowDoubleClick;
            dgvoucherGrid.SelectedItemChanged += DgvoucherGrid_SelectedItemChanged;
            dgGldailyJournalLinesGrid.SelectedItemChanged += DgGldailyJournalLinesGrid_SelectedItemChanged;
            dgGldailyJournalLinesGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            GetMenuItem();
            LedgerCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount));
            DebtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor));
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
            PaymentCache = Comp.GetCache(typeof(Uniconta.DataModel.PaymentTerm));
            localMenu.OnChecked += LocalMenu_OnChecked;
            orient = api.session.Preference.BankStatementHorisontal ? System.Windows.Controls.Orientation.Horizontal : System.Windows.Controls.Orientation.Vertical;
            lGroup.Orientation = orient;
            if (!Comp._HasWithholding)
                Withholding.Visible = Withholding.ShowInColumnChooser = false;
            else
                Withholding.ShowInColumnChooser = true;
            if (!Comp.Project)
            {
                colWorkSpace.Visible = colPrCategory.ShowInColumnChooser = false;
                colPrCategory.Visible = colPrCategory.ShowInColumnChooser = false;
                colProject.Visible = colProject.ShowInColumnChooser = false;
                colProjectText.Visible = colProjectText.ShowInColumnChooser = false;
                colEmployee.Visible = colEmployee.ShowInColumnChooser = false;
                colEmployeeName.Visible = colEmployeeName.ShowInColumnChooser = false;
            }
            else
            {
                colWorkSpace.ShowInColumnChooser = true;
                colPrCategory.ShowInColumnChooser = true;
                colProject.ShowInColumnChooser = true;
                colProjectText.ShowInColumnChooser = true;
                colEmployee.ShowInColumnChooser = true;
                colEmployeeName.ShowInColumnChooser = true;
            }

            if (!Comp.ProjectTask)
            {
                Task.Visible = false;
                Task.ShowInColumnChooser = false;
            }
            else
                Task.ShowInColumnChooser = true;
        }

        private void DgvoucherGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var vc = dgvoucherGrid.SelectedItem as VouchersClientLocal;
            if (vc != null && vc.PrimaryKeyId != 0)
            {
                var visibleRows = dgGldailyJournalLinesGrid.GetVisibleRows() as IEnumerable<GLDailyJournalLineClient>;
                dgGldailyJournalLinesGrid.SelectedItem = visibleRows.Where(v => v._DocumentRef == vc.PrimaryKeyId).FirstOrDefault();
            }
        }

        public override void PageClosing()
        {
            if (dgGldailyJournalLinesGrid.IsAutoSave && dgGldailyJournalLinesGrid.HasUnsavedData)
                saveGrid();
            base.PageClosing();
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            JournalLineGridClient oldselectedItem = e.OldItem as JournalLineGridClient;
            if (oldselectedItem != null)
            {
                oldselectedItem.PropertyChanged -= JournalLineGridClient_PropertyChanged;
                var Cache = GetCache(oldselectedItem._AccountType);
                if (Cache != null && Cache.Count > 1000)
                {
                    oldselectedItem.accntSource = null;
                    oldselectedItem.NotifyPropertyChanged("AccountSource");
                }
                Cache = GetCache(oldselectedItem._OffsetAccountType);
                if (Cache != null && Cache.Count > 1000)
                {
                    oldselectedItem.offsetAccntSource = null;
                    oldselectedItem.NotifyPropertyChanged("OffsetAccountSource");
                }
            }
            JournalLineGridClient selectedItem = e.NewItem as JournalLineGridClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += JournalLineGridClient_PropertyChanged;
        }

        DCAccount getDCAccount(GLJournalAccountType type, string Acc)
        {
            if (Acc != null && type != GLJournalAccountType.Finans)
            {
                var cache = (type == GLJournalAccountType.Debtor) ? DebtorCache : CreditorCache;
                return (DCAccount)cache?.Get(Acc);
            }
            return null;
        }
        DCAccount copyDCAccount(JournalLineGridClient rec, GLJournalAccountType type, string Acc, bool OnlyDueDate = false)
        {
            var dc = getDCAccount(type, Acc);
            if (dc == null)
                return null;
            if ((rec._DocumentDate != DateTime.MinValue || rec._Date != DateTime.MinValue) && rec._DCPostType != Uniconta.DataModel.DCPostType.Creditnote && (rec._DCPostType < Uniconta.DataModel.DCPostType.Payment || rec._DCPostType > Uniconta.DataModel.DCPostType.PartialPayment))
            {
                var pay = (Uniconta.DataModel.PaymentTerm)PaymentCache?.Get(dc._Payment);
                if (pay != null)
                    rec.DueDate = pay.GetDueDate(rec._DocumentDate != DateTime.MinValue ? rec._DocumentDate : rec._Date);
            }
            if (OnlyDueDate)
                return dc;

            if (type == GLJournalAccountType.Creditor)
            {
                if (dc._PrCategory != null)
                    rec.PrCategory = dc._PrCategory;
                if (rec._PaymentMethod != dc._PaymentMethod)
                {
                    rec._PaymentMethod = dc._PaymentMethod;
                    rec.NotifyPropertyChanged("PaymentMethod");
                }
            }
            rec.Withholding = dc._Withholding;
            if (dc._Dim1 != null)
                rec.Dimension1 = dc._Dim1;
            if (dc._Dim2 != null)
                rec.Dimension2 = dc._Dim2;
            if (dc._Dim3 != null)
                rec.Dimension3 = dc._Dim3;
            if (dc._Dim4 != null)
                rec.Dimension4 = dc._Dim4;
            if (dc._Dim5 != null)
                rec.Dimension5 = dc._Dim5;
            if (rec.AmountCur == 0)
                rec.Currency = AppEnums.Currencies.ToString((int)dc._Currency);
            return dc;
        }

        void assignOffsetVat(JournalLineGridClient rec, string vat, string vatOperation)
        {
            if (rec._DCPostType >= Uniconta.DataModel.DCPostType.Payment && rec._DCPostType <= Uniconta.DataModel.DCPostType.PartialPayment)
                return;

            if (vat != null)
            {
                if (TwoVatCodes)
                    rec.OffsetVat = vat;
                else
                    rec.Vat = vat;
            }
            if (vatOperation != null)
            {
                if (TwoVatCodes)
                    rec.VatOffsetOperation = vatOperation;
                else
                    rec.VatOperation = vatOperation;
            }
        }

        void JournalLineGridClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as JournalLineGridClient;
            switch (e.PropertyName)
            {
                case "AccountType":
                    SetAccountSource(rec);
                    break;
                case "OffsetAccountType":
                    SetOffsetAccountSource(rec);
                    break;
                case "Account":
                    if (rec._AccountType != (byte)GLJournalAccountType.Finans)
                    {
                        var dc = copyDCAccount(rec, rec._AccountTypeEnum, rec._Account);
                        if (dc != null)
                        {
                            rec.Vat = null;
                            if (dc._PostingAccount != null)
                            {
                                // lets check the currenut offset account if it is a bank account
                                var Acc = (GLAccount)LedgerCache?.Get(rec._OffsetAccount);
                                if (Acc == null || Acc._MandatoryTax != VatOptions.NoVat)  // we will not override a bank account
                                {
                                    if (rec._OffsetAccountType != (byte)GLJournalAccountType.Finans)
                                    {
                                        rec._OffsetAccountType = (byte)GLJournalAccountType.Finans;
                                        rec.NotifyPropertyChanged("OffsetAccountType");
                                    }
                                    var amount = rec.Amount;
                                    if (rec._AccountType == (byte)GLJournalAccountType.Debtor)
                                        amount *= -1d;
                                    if (dc._TransType != null)
                                        rec.TransType = dc._TransType;

                                    rec.TmpOffsetAccount = null;
                                    if (amount < 0) // expense
                                        rec.OffsetAccount = dc._PostingAccount;
                                    else if (amount == 0)
                                        rec.TmpOffsetAccount = dc._PostingAccount;
                                }
                            }

                            if (UseDCVat && rec._OffsetAccountType == (byte)GLJournalAccountType.Finans)
                            {
                                var Acc = (GLAccount)LedgerCache?.Get(rec._OffsetAccount);
                                if (Acc != null && Acc._MandatoryTax != VatOptions.NoVat && Acc._MandatoryTax != VatOptions.Fixed)
                                    assignOffsetVat(rec, dc._Vat, dc._VatOperation);
                            }
                        }
                    }
                    else
                    {
                        var Acc = (GLAccount)LedgerCache?.Get(rec._Account);
                        if (Acc != null)
                        {
                            if (Acc._PrCategory != null)
                                rec.PrCategory = Acc._PrCategory;
                            if (Acc._Currency != 0)
                            {
                                rec._Currency = (byte)Acc._Currency;
                                rec.NotifyPropertyChanged("Currency");
                            }
                            rec.SetGLDim(Acc);
                            if (Acc._MandatoryTax == VatOptions.NoVat)
                            {
                                if (TwoVatCodes)
                                {
                                    rec.Vat = null;
                                    rec.VatOperation = null;
                                }
                            }
                            else if (Acc._IsDCAccount)
                                assignOffsetVat(rec, Acc._Vat, Acc._VatOperation);
                            else
                            {
                                var vat = Acc._Vat;
                                var VatOperation = Acc._VatOperation;
                                if (UseDCVat && Acc._MandatoryTax != VatOptions.Fixed)
                                {
                                    var dc = getDCAccount(rec._OffsetAccountTypeEnum, rec._OffsetAccount);
                                    if (dc != null && dc._Vat != null)
                                    {
                                        vat = dc._Vat;
                                        VatOperation = dc._VatOperation;
                                    }
                                }
                                rec.Vat = vat;
                                rec.VatOperation = VatOperation;
                            }
                            if (Acc._DefaultOffsetAccount != null)
                            {
                                rec._OffsetAccountType = (byte)Acc._DefaultOffsetAccountType; // set before account
                                rec.OffsetAccount = Acc._DefaultOffsetAccount;
                                if (rec._OffsetAccountType == 0)
                                {
                                    var Acc2 = (GLAccount)LedgerCache.Get(rec._OffsetAccount);
                                    if (Acc2 != null)
                                    {
                                        if (TwoVatCodes || (Acc._MandatoryTax != VatOptions.NoVat && Acc._MandatoryTax != VatOptions.Fixed))
                                            assignOffsetVat(rec, Acc2._Vat, Acc2._VatOperation);
                                    }
                                }
                            }
                            if (Acc._DebetCredit > 0)
                                dgGldailyJournalLinesGrid.GoToCol(Acc._DebetCredit == DebitCreditPreference.Debet ? "Debit" : "Credit", true);
                        }
                    }
                    rec.UpdateDefaultText();
                    break;
                case "OffsetAccount":
                    if (rec._OffsetAccountType != (byte)GLJournalAccountType.Finans)
                    {
                        var dc = copyDCAccount(rec, rec._OffsetAccountTypeEnum, rec._OffsetAccount);
                        if (dc != null)
                        {
                            if (dc._TransType != null && rec._TransType == null && rec._Text == null)
                                rec.TransType = dc._TransType;

                            if (dc._PostingAccount != null && rec._Account == null)
                            {
                                if (rec._AccountType != (byte)GLJournalAccountType.Finans)
                                {
                                    rec._AccountType = (byte)GLJournalAccountType.Finans;
                                    rec.NotifyPropertyChanged("AccountType");
                                }
                                rec.Account = dc._PostingAccount;
                                rec.TmpOffsetAccount = null;
                            }

                            rec.OffsetVat = null;
                            if (UseDCVat && rec._AccountType == (byte)GLJournalAccountType.Finans)
                            {
                                var Acc2 = (GLAccount)LedgerCache?.Get(rec._Account);
                                if (Acc2 != null && Acc2._MandatoryTax != VatOptions.NoVat && Acc2._MandatoryTax != VatOptions.Fixed)
                                {
                                    if (dc._Vat != null)
                                        rec.Vat = dc._Vat;
                                    if (dc._VatOperation != null)
                                        rec.VatOperation = dc._VatOperation;
                                }
                            }
                        }
                    }
                    else
                    {
                        var Acc = (GLAccount)LedgerCache?.Get(rec._OffsetAccount);
                        if (Acc != null)
                        {
                            if (Acc._MandatoryTax == VatOptions.NoVat)
                            {
                                if (TwoVatCodes)
                                {
                                    rec.OffsetVat = null;
                                    rec.VatOffsetOperation = null;
                                }
                            }
                            else if (Acc._IsDCAccount)
                            {
                                var Acc2 = (GLAccount)LedgerCache.Get(rec._Account);
                                if (Acc2 == null || (Acc2._Vat == null && Acc2._MandatoryTax != VatOptions.NoVat))
                                {
                                    if (Acc._Vat != null)
                                        rec.Vat = Acc._Vat;
                                    if (Acc._VatOperation != null)
                                        rec.VatOperation = Acc._VatOperation;
                                }
                            }
                            else
                            {
                                var vat = Acc._Vat;
                                var VatOperation = Acc._VatOperation;
                                if (UseDCVat && Acc._MandatoryTax != VatOptions.Fixed)
                                {
                                    var dc = getDCAccount(rec._AccountTypeEnum, rec._Account);
                                    if (dc != null)
                                    {
                                        if (dc._Vat != null)
                                            vat = dc._Vat;
                                        if (dc._VatOperation != null)
                                            VatOperation = dc._VatOperation;
                                    }
                                }
                                if (TwoVatCodes)
                                    assignOffsetVat(rec, vat, VatOperation);
                                else
                                {
                                    var Acc2 = (GLAccount)LedgerCache.Get(rec._Account);
                                    if (Acc2 == null || (Acc2._Vat == null && Acc2._MandatoryTax != VatOptions.NoVat))
                                    {
                                        if (Acc._Vat != null)
                                            rec.Vat = Acc._Vat;
                                        if (Acc._VatOperation != null)
                                            rec.VatOperation = Acc._VatOperation;
                                    }
                                }
                            }
                        }
                    }
                    rec.UpdateDefaultText();
                    break;
            }
        }

        SQLCache LedgerCache, DebtorCache, CreditorCache, ProjectCache, PaymentCache;
        SQLCache GetCache(byte AccountType)
        {
            switch (AccountType)
            {
                case (byte)GLJournalAccountType.Finans:
                    return LedgerCache;
                case (byte)GLJournalAccountType.Debtor:
                    return DebtorCache;
                case (byte)GLJournalAccountType.Creditor:
                    return CreditorCache;
                default: return null;
            }
        }

        private void SetAccountSource(JournalLineGridClient rec)
        {
            var act = rec._AccountType;
            SQLCache cache = GetCache(act);
            if (cache != null)
            {
                int ver = cache.version + 10000 * (act + 1);
                if (ver != rec.AccountVersion || rec.accntSource == null)
                {
                    if (act == (byte)GLJournalAccountType.Finans)
                        rec.accntSource = new LedgerSQLCacheFilter(cache);
                    else
                        rec.accntSource = cache;
                    if (rec.accntSource != null)
                    {
                        rec.AccountVersion = ver;
                        rec.NotifyPropertyChanged("AccountSource");
                    }
                }
            }
        }

        private void SetOffsetAccountSource(JournalLineGridClient rec)
        {
            var act = rec._OffsetAccountType;
            SQLCache cache = GetCache(act);
            if (cache != null)
            {
                int ver = cache.version + 10000 * (act + 1);
                if (ver != rec.OffsetAccountVersion || rec.offsetAccntSource == null)
                {
                    if (act == (byte)GLJournalAccountType.Finans)
                        rec.offsetAccntSource = new LedgerSQLCacheFilter(cache);
                    else
                        rec.offsetAccntSource = cache.GetNotNullArray;
                    if (rec.offsetAccntSource != null)
                    {
                        rec.OffsetAccountVersion = ver;
                        rec.NotifyPropertyChanged("OffsetAccountSource");
                    }
                }
            }
        }

        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            JournalLineGridClient selectedItem = dgGldailyJournalLinesGrid.SelectedItem as JournalLineGridClient;
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

        CorasauGridLookupEditorClient prevOffsetAccount;
        private void OffsetAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            JournalLineGridClient selectedItem = dgGldailyJournalLinesGrid.SelectedItem as JournalLineGridClient;
            if (selectedItem != null)
            {
                SetOffsetAccountSource(selectedItem);
                if (prevOffsetAccount != null)
                    prevOffsetAccount.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevOffsetAccount = editor;
                editor.isValidate = true;
            }
        }

        private void Account_LostFocus(object sender, RoutedEventArgs e)
        {
            SetAccountByLookupText(sender, false);
        }
        private void OffsetAccount_LostFocus(object sender, RoutedEventArgs e)
        {
            SetAccountByLookupText(sender, true);
        }
        void SetAccountByLookupText(object sender, bool offsetAcc)
        {
            JournalLineGridClient selectedItem = dgGldailyJournalLinesGrid.SelectedItem as JournalLineGridClient;
            if (selectedItem == null)
                return;
            var actType = !offsetAcc ? selectedItem._AccountTypeEnum : selectedItem._OffsetAccountTypeEnum;
            if (actType != GLJournalAccountType.Finans)
                return;
            var le = sender as CorasauGridLookupEditor;
            if (string.IsNullOrEmpty(le.EnteredText))
                return;
            var accounts = LedgerCache?.GetNotNullArray as GLAccount[];
            if (accounts != null)
            {
                var act = accounts.Where(ac => string.Compare(ac._Lookup, le.EnteredText, StringComparison.OrdinalIgnoreCase) == 0).FirstOrDefault();
                if (act != null)
                {
                    dgGldailyJournalLinesGrid.SetLoadedRow(selectedItem);
                    if (offsetAcc)
                        selectedItem.OffsetAccount = act.KeyStr;
                    else
                        selectedItem.Account = act.KeyStr;
                    le.EditValue = act.KeyStr;
                    dgGldailyJournalLinesGrid.SetModifiedRow(selectedItem);
                }
            }
            le.EnteredText = null;
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            if (PaymentCache == null)
                PaymentCache = await api.LoadCache(typeof(Uniconta.DataModel.PaymentTerm)).ConfigureAwait(false);
            if (DebtorCache == null)
                DebtorCache = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);

            var header = this.masterRecord;
            if (header._Account != null)
            {
                var rec = GetCache((byte)header._DefaultAccountType)?.Get(header._Account);
                if (rec == null)
                    header._Account = null;
            }
            if (header._OffsetAccount != null)
            {
                var rec = GetCache((byte)header._DefaultOffsetAccountType)?.Get(header._OffsetAccount);
                if (rec == null)
                    header._OffsetAccount = null;
            }
            if (api.CompanyEntity.ProjectTask)
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
        }

        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "Unlinked");
            var rbMenuAssignText = UtilDisplay.GetMenuCommandByName(rb, "AssignText");
            rbMenuAssignText.IsChecked = AssignText;
        }

        private void DgGldailyJournalLinesGrid_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            var gl = dgGldailyJournalLinesGrid.SelectedItem as GLDailyJournalLineClient;
            if (gl != null && gl._DocumentRef != 0)
            {
                var visibleRows = dgvoucherGrid.GetVisibleRows() as IEnumerable<VouchersClientLocal>;
                dgvoucherGrid.SelectedItem = visibleRows.Where(v => v.PrimaryKeyId == gl._DocumentRef).FirstOrDefault();
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            SetDimensions();
        }

        void dgvoucherGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("Attach");
        }

        async void SaveAndRefresh(bool isAttached, GLDailyJournalLineClient jour, VouchersClientLocal voucher)
        {
            busyIndicator.IsBusy = true;
            var err = await dgGldailyJournalLinesGrid.SaveData();
            if (err == ErrorCodes.Succes)
            {
                dgGldailyJournalLinesGrid.UpdateItemSource(2, jour);
                dgvoucherGrid.UpdateItemSource(2, voucher);
            }
            if (!isAttached)
                SetVoucherIsAttached();
            busyIndicator.IsBusy = false;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedVoucher = dgvoucherGrid.SelectedItem as VouchersClientLocal;
            var selectedLine = dgGldailyJournalLinesGrid.SelectedItem as GLDailyJournalLineClient;

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
                        dgGldailyJournalLinesGrid.SetLoadedRow(selectedLine);
                        selectedLine.DocumentRef = 0;
                        dgGldailyJournalLinesGrid.SetModifiedRow(selectedLine);
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
                    api.session.Preference.BankStatementHorisontal = (orient == System.Windows.Controls.Orientation.Horizontal);
                    break;
                case "SendVoucherReminder":
                    if (selectedLine != null)
                        Utility.SendVoucherReminder(api, selectedLine._Date, selectedLine.AmountCur != 0 ? selectedLine.AmountCur : selectedLine.Amount, (Currencies)selectedLine._Currency, selectedLine._Text);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        private void SaveLines()
        {
            dgGldailyJournalLinesGrid.SaveData();
        }
        private void Attach(VouchersClientLocal selectedVoucher, GLDailyJournalLineClient selectedLine, bool isAuto, IEnumerable<VouchersClientLocal> visibleRows = null)
        {
            var selectedRowId = selectedVoucher.RowId;
            if (selectedRowId != 0 && selectedLine._DocumentRef == 0)
            {
                dgGldailyJournalLinesGrid.SetLoadedRow(selectedLine);
                selectedLine.DocumentRef = selectedRowId;
                selectedLine.DocumentDate = selectedVoucher._DocumentDate;
                if (AssignText && selectedVoucher._Text != null)
                    selectedLine.Text = selectedVoucher._Text;
                var amount = selectedLine.Amount;
                selectedLine.Amount = amount != 0d ? amount : selectedVoucher._Amount;
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
                if (selectedVoucher._CostAccount != null && selectedLine._OffsetAccount == null)
                {
                    selectedLine._OffsetAccountType = 0;
                    selectedLine.OffsetAccount = selectedVoucher._CostAccount;
                }

                //dgGldailyJournalLinesGrid.SetModifiedRow(selectedLine);
                if (visibleRows == null)
                    visibleRows = dgvoucherGrid.GetVisibleRows() as IEnumerable<VouchersClientLocal>;
                var voucher = visibleRows.Where(v => v.PrimaryKeyId == selectedRowId).FirstOrDefault();
                if (voucher != null)
                    voucher.IsAttached = true;

                if (!isAuto)
                    SaveAndRefresh(true, selectedLine, selectedVoucher);
            }
        }

        async void AutoMatch()
        {
            var journalLines = dgGldailyJournalLinesGrid.ItemsSource as IEnumerable<JournalLineGridClient>;
            var visibleRowVouchers = dgvoucherGrid.GetVisibleRows() as IEnumerable<VouchersClientLocal>;
            int rowCountUpdate = 0;
            for (int i = 0; (i < 2); i++)
            {
                foreach (var voucher in visibleRowVouchers)
                {
                    if (!voucher.IsAttached && (voucher._Amount != 0 || voucher._Invoice != null))
                    {
                        DateTime date;
                        if (i == 0)
                            date = voucher._PostingDate != DateTime.MinValue ? voucher._PostingDate :
                                (voucher._DocumentDate != DateTime.MinValue ? voucher._DocumentDate : voucher.Created.Date);
                        else
                            date = voucher._PayDate;
                        if (date != DateTime.MinValue)
                        {
                            var inv = voucher._Invoice;
                            var amount = Math.Abs(voucher._Amount);
                            foreach (var p in journalLines)
                                if ((Math.Abs(p.Amount) == amount && p._Date == date)
                                    || (inv != null && inv == p._Invoice))
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

                var err = await dgGldailyJournalLinesGrid.SaveData();
                if (err == ErrorCodes.Succes)
                {
                    dgGldailyJournalLinesGrid.RefreshData();
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
                dgvoucherGrid.MergeColumnFilters("([IsAttached] = False)");
                dgGldailyJournalLinesGrid.MergeColumnFilters("([HasVoucher] = False)");
            }
            else
            {
                dgvoucherGrid.ClearColumnFilter("IsAttached");
                dgGldailyJournalLinesGrid.ClearColumnFilter("HasVoucher");
            }
        }

        private void ViewVoucher(UnicontaBaseEntity baseEntity)
        {
            busyIndicator.IsBusy = true;
            string header = null;
            if (baseEntity is VouchersClientLocal)
            {
                header = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("View"), Uniconta.ClientTools.Localization.lookup("PhysicalVoucher"));
                ViewVoucher(TabControls.VouchersPage3, dgvoucherGrid.syncEntity, header);
            }
            else if (baseEntity is GLDailyJournalLineClient)
            {
                header = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("View"), Uniconta.ClientTools.Localization.lookup("AttachedVoucher"));
                ViewVoucher(TabControls.VouchersPage3, dgGldailyJournalLinesGrid.syncEntity, header);
            }
            busyIndicator.IsBusy = false;
        }

        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            await dgvoucherGrid.Filter(null);
            if (JournalLines == null)
                await dgGldailyJournalLinesGrid.Filter(null);
            else
                dgGldailyJournalLinesGrid.SetSource(JournalLines.ToArray());
            SetVoucherIsAttached();
            busyIndicator.IsBusy = false;
        }

        private void SetVoucherIsAttached()
        {
            var docRefs = new HashSet<int>();
            foreach (var rec in dgGldailyJournalLinesGrid.ItemsSource as IEnumerable<JournalLineGridClient>)
                if (rec._DocumentRef != 0)
                    docRefs.Add(rec._DocumentRef);
            if (docRefs.Count > 0)
                foreach (var voucher in dgvoucherGrid.ItemsSource as IEnumerable<VouchersClientLocal>)
                    voucher.IsAttached = docRefs.Contains(voucher.RowId);
        }

        private void SetDimensions()
        {
            var c = api.CompanyEntity;
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, clDim1, clDim2, clDim3, clDim4, clDim5);
        }

        public override void AssignMultipleGrid(List<CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgGldailyJournalLinesGrid);
            gridCtrls.Add(dgvoucherGrid);
        }

        public override string NameOfControl
        {
            get { return TabControls.MatchPhysicalVoucherToGLDailyJournalLines; }
        }

        CorasauGridLookupEditorClient prevTask;
        private void Task_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgGldailyJournalLinesGrid.SelectedItem as JournalLineGridClient;
            var selected = (ProjectClient)ProjectCache?.Get(selectedItem?._Project);
            if (selected != null)
            {
                setTask(selected, selectedItem);
                if (prevTask != null)
                    prevTask.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevTask = editor;
                editor.isValidate = true;
            }
        }

        async void setTask(ProjectClient project, JournalLineGridClient rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                    rec.taskSource = project.Tasks ?? await project.LoadTasks(api);
                else
                {
                    rec.taskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("TaskSource");
            }
        }
    }

    public class VouchersClientLocal : VouchersClient
    {
        bool isAttached;
        [Display(Name = "Attached", ResourceType = typeof(VouchersClientText))]
        public bool IsAttached { get { return isAttached; } set { isAttached = value; NotifyPropertyChanged("IsAttached"); } }
    }
}
