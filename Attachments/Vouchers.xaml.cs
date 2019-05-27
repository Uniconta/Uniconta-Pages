using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Utilities;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using System.IO;
using System.Threading.Tasks;
using Uniconta.API.GeneralLedger;
using System.Windows;
using Uniconta.ClientTools.Util;
using Uniconta.DataModel;
using System.Collections;
using Uniconta.API.Service;
using UnicontaClient.Controls.Dialogs;
using Uniconta.Common.Utility;
#if !SILVERLIGHT
using Bilagscan;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Configuration;
using System.Text;
#endif

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VouchersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VouchersClient); } }
        public override bool Readonly { get { return false; } }
        public override bool CanInsert { get { return false; } }
        public override IComparer GridSorting { get { return new SortDocAttached(); } }

        async public override Task<ErrorCodes> SaveData()
        {
            SelectedItem = null;
#if !SILVERLIGHT
            /*
             * Awaiting the save data as we need to have rowid
             * With row id we would be able to save the buffers seperately
             * So first we save the records and then update the buffers
             */
            var addedRows = AddedRows;
            var addedRowCount = addedRows.Count();

            if (addedRowCount > 0)
            {
                var vouchersClient = new VouchersClient[addedRowCount];
                var buffers = new byte[addedRowCount][];
                int iCtr = 0;

                foreach (var row in addedRows)
                {
                    vouchersClient[iCtr] = row.DataItem as VouchersClient;
                    buffers[iCtr] = vouchersClient[iCtr]._Data;
                    vouchersClient[iCtr]._SizeKB = ((vouchersClient[iCtr]._Data.Length + 512) >> 10);
                    vouchersClient[iCtr]._Data = null;
                    iCtr++;
                }

                var result = await base.SaveData();

                if (result == ErrorCodes.Succes)
                    Utility.UpdateBuffers(api, buffers, vouchersClient);

                await Filter(null);
                
                return result;
            }
            else
                return await base.SaveData();
#else
            return await base.SaveData();
#endif
        }
    }

    public partial class Vouchers : GridBasePage
    {
        object cache;
        public Vouchers(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }

        public Vouchers(UnicontaBaseEntity master)
          : base(master)
        {
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            cache = VoucherCache.HoldGlobalVoucherCache;
            dgVoucherGrid.RowDoubleClick += dgVoucherGrid_RowDoubleClick;
            localMenu.dataGrid = dgVoucherGrid;

            var api = this.api;
            var Comp = api.CompanyEntity;
            this.LedgerCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount));
            this.CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
            this.PaymentCache = Comp.GetCache(typeof(PaymentTerm));

            dgVoucherGrid.api = api;
            dgVoucherGrid.BusyIndicator = busyIndicator;
            var masterrec = new List<UnicontaBaseEntity>() { new Uniconta.DataModel.DocumentNoRef() };
            if (master != null)
                masterrec.Add(master);
            dgVoucherGrid.masterRecords = masterrec;
            SetRibbonControl(localMenu, dgVoucherGrid);
            ribbonControl.SelectedPageChanged += RibbonControl_SelectedPageChanged;

            localMenu.OnItemClicked += localMenu_OnItemClicked;
            this.BeforeClose += Vouchers_BeforeClose;

            RemoveMenuItem();

#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += RootVisual_KeyDown;
#else
            PreviewKeyDown += RootVisual_KeyDown;
            dgVoucherGrid.tableView.AllowDragDrop = true;
            dgVoucherGrid.tableView.DropRecord += dgVoucherGridView_DropRecord;
            dgVoucherGrid.tableView.DragRecordOver += dgVoucherGridView_DragRecordOver;
#endif
            dgVoucherGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            SetFooterDetailText();
        }

#if !SILVERLIGHT
        private void dgVoucherGridView_DragRecordOver(object sender, DevExpress.Xpf.Core.DragRecordOverEventArgs e)
        {
            if (e.Data.GetFormats().Contains("FileName"))
                e.Effects = DragDropEffects.Copy;
            else if (e.Data.GetFormats().Contains("FileGroupDescriptor"))
                e.Effects = DragDropEffects.All;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void dgVoucherGridView_DropRecord(object sender, DevExpress.Xpf.Core.DropRecordEventArgs e)
        {
            dgVoucherGrid.tableView.FocusedRowHandle = e.TargetRowHandle > 0 ? e.TargetRowHandle - 1 : -1;
            string[] errors = null;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files == null || files.Length == 0)
                    return;

                errors = Utility.DropFilesToGrid(dgVoucherGrid, files, true).ToArray();

            }
            else if (e.Data.GetDataPresent("FileGroupDescriptor"))
                errors = Utility.DropOutlookMailsToGrid(dgVoucherGrid, e.Data, true);

            if (errors?.Count() > 0)
            {
                var cwErrorBox = new CWErrorBox(errors.ToArray(), true);
                cwErrorBox.Show();
            }
        }
#endif
        SQLCache LedgerCache, CreditorCache, PaymentCache, ProjectCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (this.LedgerCache == null)
                this.LedgerCache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLAccount), api).ConfigureAwait(false);
            if (this.PaymentCache == null)
                this.CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api).ConfigureAwait(false);
            if (this.PaymentCache == null)
                this.PaymentCache = Comp.GetCache(typeof(PaymentTerm)) ?? await Comp.LoadCache(typeof(PaymentTerm), api).ConfigureAwait(false);
            LoadType(typeof(Uniconta.DataModel.Employee));
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var Comp = api.CompanyEntity;

            if (Comp._CountryId != CountryCode.Denmark && Comp._CountryId != CountryCode.Iceland && Comp._CountryId != CountryCode.Netherlands && Comp._CountryId != CountryCode.Austria)
            {
                UtilDisplay.RemoveMenuCommand(rb, "BilagscanSendVouchers");
                UtilDisplay.RemoveMenuCommand(rb, "BilagscanReadVouchers");
            }

#if SILVERLIGHT
            UtilDisplay.RemoveMenuCommand(rb,"SplitPDF");
            UtilDisplay.RemoveMenuCommand(rb,"JoinPDF");
#endif
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as VouchersClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            var selectedItem = e.NewItem as VouchersClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
        }

        static public void SetPayDate(SQLCache PaymentCache, VouchersClient rec, Uniconta.DataModel.DCAccount dc)
        {
            var pay = (Uniconta.DataModel.PaymentTerm)PaymentCache?.Get(dc?._Payment);
            if (pay != null)
                rec.PayDate = pay.GetDueDate(rec._DocumentDate != DateTime.MinValue ? rec._DocumentDate : (rec._PostingDate != DateTime.MinValue ? rec._PostingDate : rec.Created));
        }
        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as VouchersClient;
            switch (e.PropertyName)
            {
                case "PostingDate":
                case "DocumentDate":
                    SetPayDate(PaymentCache, rec, (Uniconta.DataModel.DCAccount)CreditorCache?.Get(rec._CreditorAccount));
                    break;
                case "CreditorAccount":
                    var dc = (Uniconta.DataModel.DCAccount)CreditorCache?.Get(rec._CreditorAccount);
                    if (dc == null)
                        return;
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
                    if (dc._PostingAccount != null)
                        rec.CostAccount = dc._PostingAccount;
                    if (dc._Currency != 0)
                        rec.Currency = AppEnums.Currencies.ToString((int)dc._Currency);
                    if (dc._Vat != null)
                    {
                        rec.Vat = dc._Vat;
                        rec.VatOperation = dc._VatOperation;
                    }
                    if (rec._PaymentMethod != dc._PaymentMethod)
                    {
                        rec._PaymentMethod = dc._PaymentMethod;
                        rec.NotifyPropertyChanged("PaymentMethod");
                    }
                    SetPayDate(PaymentCache, rec, dc);
                    rec.UpdateDefaultText();
                    break;
                case "CostAccount":
                    var Acc = (Uniconta.DataModel.GLAccount)LedgerCache?.Get(rec._CostAccount);
                    if (Acc == null)
                        return;
                    if (Acc._PrCategory != null)
                        rec.PrCategory = Acc._PrCategory;
                    if (Acc._MandatoryTax == VatOptions.NoVat)
                    {
                        rec.Vat = null;
                        rec.VatOperation = null;
                    }
                    else
                    {
                        rec.Vat = Acc._Vat;
                        rec.VatOperation = Acc._VatOperation;
                    }
                    if (Acc._DefaultOffsetAccount != null)
                    {
                        if (Acc._DefaultOffsetAccountType == GLJournalAccountType.Creditor)
                            rec.CreditorAccount = Acc._DefaultOffsetAccount;
                        else if (Acc._DefaultOffsetAccountType == GLJournalAccountType.Finans)
                            rec.PayAccount = Acc._DefaultOffsetAccount;
                    }
                    break;
                case "Invoice":
                    rec.UpdateDefaultText();
                    break;
                case "Project":
                    lookupProjectDim(rec);
                    break;
            }
        }

        async void lookupProjectDim(VouchersClient rec)
        {
            if (ProjectCache == null)
            {
                var api = this.api;
                ProjectCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.CompanyEntity.LoadCache(typeof(Uniconta.DataModel.Project), api);
            }
            var proj = (Uniconta.DataModel.Project)ProjectCache?.Get(rec._Project);
            if (proj != null)
            {
                if (proj._Dim1 != null)
                    rec.Dimension1 = proj._Dim1;
                if (proj._Dim2 != null)
                    rec.Dimension2 = proj._Dim2;
                if (proj._Dim3 != null)
                    rec.Dimension3 = proj._Dim3;
                if (proj._Dim4 != null)
                    rec.Dimension4 = proj._Dim4;
                if (proj._Dim5 != null)
                    rec.Dimension5 = proj._Dim5;
            }
        }

        int selectedPageIndex;
        private void RibbonControl_SelectedPageChanged(int selectedPageIndex)
        {
            this.selectedPageIndex = selectedPageIndex;
            if (selectedPageIndex == 1)
                dgVoucherGrid.FilterString = "Contains([Folder],'true')";
            else
                dgVoucherGrid.FilterString = ""; //dgVoucherGrid.ClearFilter();
        }
        void dgVoucherGrid_RowDoubleClick()
        {
            localMenu_OnItemClicked("ViewDownloadRow");
        }

        void Vouchers_BeforeClose()
        {
            cache = null;
            dgVoucherGrid.SelectedItem = null;
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown -= RootVisual_KeyDown;
#else
            this.PreviewKeyDown -= RootVisual_KeyDown;
#endif
            this.BeforeClose -= Vouchers_BeforeClose;
        }

        private void RootVisual_KeyDown(object sender, KeyEventArgs e)
        {
#if !SILVERLIGHT
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (dgVoucherGrid.CurrentColumn.Name == "HasOffsetAccounts" && e.Key == Key.Down)
                {
                    var currentRow = dgVoucherGrid.SelectedItem as VouchersClient;
                    if (currentRow != null)
                        CallOffsetAccount(currentRow);
                }
            }
#endif
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgVoucherGrid.SelectedItem as VouchersClient;

            switch (ActionType)
            {
                case "AddRow":
                    object[] parameters = new object[2];
                    parameters[0] = api;
                    parameters[1] = "";
                    AddDockItem(TabControls.VouchersPage2, parameters, Uniconta.ClientTools.Localization.lookup("Vouchers"), ";component/Assets/img/Add_16x16.png");
                    break;

                case "DeleteRow":
                    dgVoucherGrid.DeleteRow();
                    break;

                case "Save":
                    dgVoucherGrid.SaveData();
                    break;

                case "RefreshGrid":
                    if (dgVoucherGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgVoucherGrid);
                    else
                        dgVoucherGrid.Filter(null);
                    break;

                case "ViewDownloadRow":
                case "ViewVoucher":
                    if (selectedItem != null)
                        ViewVoucher(TabControls.VouchersPage3, dgVoucherGrid.syncEntity);
                    break;
                case "ViewJournalLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.VoucherJournalLines, dgVoucherGrid.syncEntity, true, string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("Journallines"), selectedItem.RowId), ";component/Assets/img/View_16x16.png");
                    break;
                case "GenerateJournalLines":
                    // save lines first.
                    dgVoucherGrid.SelectedItem = null;

                    api.AllowBackgroundCrud = false;
                    var task = dgVoucherGrid.SaveData();
                    api.AllowBackgroundCrud = true;

                    CWJournal journals = new CWJournal(api, true);
#if !SILVERLIGHT
                    journals.DialogTableId = 2000000015;
#endif
                    journals.Closed += async delegate
                    {
                        if (journals.DialogResult == true)
                        {
                            var journalName = CWJournal.Journal;
                            var journalDate = CWJournal.Date;
                            var OnlyApproved = journals.OnlyApproved;

                            var errorCode = await task;  // make sure save is completed.
                            if (errorCode != ErrorCodes.Succes)
                                return;

                            var postingApi = new PostingAPI(api);
                            IEnumerable<VouchersClient> lst;
                            if (journals.OnlyCurrentRecord)
                            {
                                OnlyApproved = false;
                                selectedItem.InJournal = true;
                                lst = new VouchersClient[] { selectedItem };
                            }
                            else
                                lst = dgVoucherGrid.GetVisibleRows() as IEnumerable<VouchersClient>;

                            if (object.ReferenceEquals(lst, dgVoucherGrid.ItemsSource))
                                task = postingApi.GenerateJournalFromDocument(journalName, journalDate, CWJournal.IsCreditAmount, OnlyApproved, CWJournal.AddVoucherNumber);
                            else
                            {
                                // For Visible rows or just selected row.
                                if (OnlyApproved)
                                {
                                    var VoucherList = new List<VouchersClient>();
                                    foreach (var doc in lst)
                                    {
                                        if (doc._OnHold || !doc._Approved || (doc._Approver2 != null && !doc._Approved2))
                                            continue;
                                        doc.InJournal = true;
                                        VoucherList.Add(doc);
                                    }
                                    lst = VoucherList;
                                }
                                task = postingApi.GenerateJournalFromDocument(journalName, journalDate, CWJournal.IsCreditAmount, CWJournal.AddVoucherNumber, lst);
                            }

                            busyIndicator.IsBusy = true;
                            errorCode = await task;
                            busyIndicator.IsBusy = false;

                            if (errorCode != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(errorCode);
                            else if (!journals.OnlyCurrentRecord)
                            {
                                var text = string.Concat(Uniconta.ClientTools.Localization.lookup("TransferedToJournal"), ": ", journalName,
                                    Environment.NewLine, string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Journallines")), " ?");
                                var select = UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel);
                                if (select == MessageBoxResult.OK)
                                {
                                    Uniconta.DataModel.GLDailyJournal jour;

                                    var cache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLDailyJournal));
                                    if (cache != null)
                                        jour = (Uniconta.DataModel.GLDailyJournal)cache.Get(journalName);
                                    else
                                    {
                                        jour = new Uniconta.DataModel.GLDailyJournal();
                                        jour._Journal = journalName;
                                        await api.Read(jour);
                                    }
                                    AddDockItem(TabControls.GL_DailyJournalLine, jour, null, null, true);
                                }
                            }
                        }
                    };
                    journals.Show();
                    break;
                case "AddFolder":
                    var newItemFolder = dgVoucherGrid.GetChildInstance();
                    object[] paramFolder = new object[2];
                    paramFolder[0] = newItemFolder;
                    paramFolder[1] = "Create";
                    AddDockItem(TabControls.VoucherFolderPage, paramFolder, Uniconta.ClientTools.Localization.lookup("Folder"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditFolder":
                    if (selectedItem == null)
                        return;
                    object[] paramFolderEdit = new object[2];
                    paramFolderEdit[0] = selectedItem;
                    paramFolderEdit[1] = "Edit";
                    AddDockItem(TabControls.VoucherFolderPage, paramFolderEdit, selectedItem.Text, ";component/Assets/img/Edit_16x16.png");
                    break;
                case "FolderWizard":
                    LaunchWizardFolder();
                    break;
                case "OffSetAccount":
                    CallOffsetAccount(selectedItem);
                    break;
#if !SILVERLIGHT
                case "BilagscanSendVouchers":
                    //if (UnicontaMessageBox.Show("100%", Uniconta.ClientTools.Localization.lookup("Bilagscan"), MessageBoxButton.YesNoCancel) == MessageBoxResult.Yes)
                    //    SendToBilagscan(true);
                    //else
                    SendToBilagscan(false);
                    break;
                case "BilagscanReadVouchers":
                    RecieveFromBilagscan();
                    break;
                case "SplitPDF":
                    if (selectedItem == null) return;

                    LoadSplitPDFDialog(selectedItem);
                    break;

                case "JoinPDF":
                    JoinPDFVouchers();
                    break;
#endif
                case "CopyOrMoveVoucher":
                    if (selectedItem != null)
                        CopyOrMoveAttachement(selectedItem);
                    break;
                case "RemoveFromInbox":
                    if (selectedItem != null)
                        UpdateInBox(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void UpdateInBox(VouchersClient selectedItem)
        {
            var rec = new DocumentNoRef();
            rec.SetMaster(selectedItem);
            string message = Uniconta.ClientTools.Localization.lookup("RemoveRecordPrompt");
            CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
            confirmationDialog.Closing += async delegate
            {
                if (confirmationDialog.DialogResult == null)
                    return;
                switch (confirmationDialog.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        var err = await api.Delete(rec);
                        if (err == 0)
                            dgVoucherGrid.UpdateItemSource(3, selectedItem);
                        break;
                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationDialog.Show();
        }
       
        private void CopyOrMoveAttachement(VouchersClient selectedItem)
        {
            var cwAttachedVouchers = new CWAttachedVouchers(api);
            cwAttachedVouchers.Closed += async delegate
            {
                if (cwAttachedVouchers.DialogResult == true)
                {
                    ErrorCodes result;
                    var docAPI = new DocumentAPI(api);
                    busyIndicator.IsBusy = true;
                    if (cwAttachedVouchers.IsAccount)
                        result = await docAPI.CreateAttachment(selectedItem, cwAttachedVouchers.Account, cwAttachedVouchers.IsDelete);
                    else
                        result = await docAPI.CreateAttachment(selectedItem, cwAttachedVouchers.Order, cwAttachedVouchers.IsDelete);
                    busyIndicator.IsBusy = false;

                    if (result == ErrorCodes.Succes)
                    {
                        if (cwAttachedVouchers.IsDelete)
                            dgVoucherGrid.UpdateItemSource(3, selectedItem);
                    }
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            cwAttachedVouchers.Show();
        }

#if !SILVERLIGHT
        async void JoinPDFVouchers()
        {
            VouchersClient voucher1 = null, voucher2 = null;
            CWJoinPDFDocument cwJoinPdfDoc = null;

            if (dgVoucherGrid.SelectedItems != null)
            {
                VouchersClient[] vouchers = dgVoucherGrid.SelectedItems.Cast<VouchersClient>().ToArray();
                if (vouchers?.Length == 0)
                    return;
                if (vouchers.Where(v => v._Fileextension != FileextensionsTypes.PDF).Count() > 0)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFileFormat"), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    return;
                }

                if (vouchers.Length >= 2)
                {
                    voucher1 = await ValidateVoucher(vouchers[0]);
                    voucher2 = await ValidateVoucher(vouchers[1]);
                }
                else
                    voucher1 = await ValidateVoucher(vouchers[0]);
            }

            if (voucher1 != null && voucher2 != null)
                cwJoinPdfDoc = new CWJoinPDFDocument(voucher1.Buffer, voucher2.Buffer);
            else if (voucher1 != null)
                cwJoinPdfDoc = new CWJoinPDFDocument(voucher1.Buffer);
            else if (voucher2 != null)
                cwJoinPdfDoc = new CWJoinPDFDocument(null, voucher2.Buffer);
            else
                cwJoinPdfDoc = new CWJoinPDFDocument();

            cwJoinPdfDoc.Closed += delegate
             {
                 if (cwJoinPdfDoc.DialogResult == true)
                 {
                     var mergedContents = cwJoinPdfDoc.MergedPDFContents;
                     var deleteMsg = string.Empty;

                     if (cwJoinPdfDoc.IsLeftPdfDelete)
                         deleteMsg = string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), string.Format("{0} {1}",
                             Uniconta.ClientTools.Localization.lookup("Left"), Uniconta.ClientTools.Localization.lookup("Voucher")));
                     else if (cwJoinPdfDoc.IsRightPdfDelete)
                         deleteMsg = string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), string.Format("{0} {1}",
                            Uniconta.ClientTools.Localization.lookup("Right"), Uniconta.ClientTools.Localization.lookup("Voucher")));

                     var deleteVoucher = !string.IsNullOrEmpty(deleteMsg) && UnicontaMessageBox.Show(deleteMsg, Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.YesNo)
                            == MessageBoxResult.Yes ? true : false;

                     if (cwJoinPdfDoc.IsLeftJoin)
                         UpdateJoinedPDFContents(voucher1, voucher2, mergedContents, deleteVoucher);
                     else
                         UpdateJoinedPDFContents(voucher2, voucher1, mergedContents, deleteVoucher);
                 }
             };

            cwJoinPdfDoc.Show();
        }

        async private Task UpdateJoinedPDFContents(VouchersClient saveVoucher, VouchersClient copiedVoucher, byte[] mergedContents, bool isDeleteVoucher)
        {
            saveVoucher._Data = mergedContents;
            VoucherCache.SetGlobalVoucherCache(saveVoucher);
            api.UpdateNoResponse(saveVoucher);

            busyIndicator.IsBusy = true;
            if (isDeleteVoucher && copiedVoucher != null)
                await api.Delete(copiedVoucher);

            await dgVoucherGrid.Filter(null);
            busyIndicator.IsBusy = false;
        }

        async private Task<VouchersClient> ValidateVoucher(VouchersClient voucher)
        {
            if (voucher != null)
            {
                if (voucher._Data == null)
                    await api.Read(voucher);
            }
            return voucher;
        }

        async void LoadSplitPDFDialog(VouchersClient selectedItem)
        {
            try
            {
                //To save any information on the editable grid
                saveGrid();
                dgVoucherGrid.SelectedItem = selectedItem; // To Ensure that Grid SelectedItem doesn't become null after calling saveGrid()

                if (selectedItem._Fileextension != FileextensionsTypes.PDF)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFileFormat"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                    return;
                }

                busyIndicator.IsBusy = true;
                if (selectedItem._Data == null)
                    await api.Read(selectedItem);

                if (selectedItem.Buffer == null)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("EmptyTable"), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    return;
                }

                var cwSplitWindow = new CWSplitPDFDocument(selectedItem.Buffer, selectedItem.RowId);
                cwSplitWindow.Closed += delegate
                {
                    if (cwSplitWindow.DialogResult == true)
                    {
                        var messageBox = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("DeleteConfirmation"), Uniconta.ClientTools.Localization.lookup("Delete"), true, string.Format("{0}:",
                            Uniconta.ClientTools.Localization.lookup("Warning")));
                        messageBox.Closed += async delegate
                        {
                            if (messageBox.ConfirmationResult != CWConfirmationBox.ConfirmationResultEnum.Cancel)
                                await InsertSplitPDFs(selectedItem, cwSplitWindow.SplittedPDFFileInfoList,
                                    messageBox.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes ? true : false);
                        };
                        messageBox.Show();
                    }
                };
                cwSplitWindow.Show();
            }
            catch (Exception ex) { busyIndicator.IsBusy = false; UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK); }
            finally { busyIndicator.IsBusy = false; }
        }

        private async Task InsertSplitPDFs(VouchersClient selectedItem, List<SelectedFileInfo> selectedFileInfo, bool deleteOriginal)
        {
            try
            {
                var fileCount = selectedFileInfo.Count;

                if (fileCount > 0)
                {
                    var voucherClients = new VouchersClient[fileCount];
                    int iVoucher = 0;
                    foreach (var fileInfo in selectedFileInfo)
                    {
                        if (fileInfo == null)
                            continue;

                        var voucher = new VouchersClient();
                        StreamingManager.Copy(selectedItem, voucher);
                        voucher._Fileextension = DocumentConvert.GetDocumentType(fileInfo.FileExtension);
                        voucher._Data = fileInfo.FileBytes;
                        voucher._Text = string.IsNullOrEmpty(selectedItem.Text) ? fileInfo.FileName : selectedItem.Text;
                        voucherClients[iVoucher++] = voucher;
                    }


                    busyIndicator.IsBusy = true;
                    var result = await api.Insert(voucherClients);
                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else
                    {
                        if (deleteOriginal)
                            await api.Delete(selectedItem);

                        await dgVoucherGrid.Filter(null);
                    }
                }
            }
            catch (Exception ex) { api.ReportException(ex, "Split PDF Failed"); }
            finally { busyIndicator.IsBusy = false; }
        }

#endif
        //async void CreateAttachment(bool deletePhysicalVoucher)
        //{
        //    var selectedItem = dgVoucherGrid.SelectedItem as VouchersClient;
        //    if (selectedItem == null)
        //        return;
        //    if (string.IsNullOrEmpty(selectedItem.CreditorAccount))
        //    {
        //        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("CreditorsAccount")));
        //        return;
        //    }
        //    var docAPI = new DocumentAPI(api);
        //    var err = await docAPI.CreateAttachment(selectedItem, deletePhysicalVoucher);
        //    if (err == ErrorCodes.Succes)
        //    {
        //        if (deletePhysicalVoucher)
        //            dgVoucherGrid.UpdateItemSource(3, selectedItem);
        //    }
        //    UtilDisplay.ShowErrorCode(err);
        //}
        private void LaunchWizardFolder()
        {
            if (dgVoucherGrid.ItemsSource == null)
                return;

            var folderWizard = new WizardWindow(new UnicontaClient.Pages.VoucherGridFolderWizard(api), string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"),
                Uniconta.ClientTools.Localization.lookup("Folder")));

            folderWizard.Closed += async delegate
            {
                if (folderWizard.DialogResult == true)
                {
                    var result = await SaveFolderData(folderWizard.WizardData);
                    if (result == ErrorCodes.Succes)
                        globalEvents.OnRefresh(TabControls.VoucherFolderPage);
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            folderWizard.Show();
        }

        async private Task<ErrorCodes> SaveFolderData(object wizarddata)
        {
            ErrorCodes result = ErrorCodes.CouldNotSave;

            if (wizarddata is object[])
            {
                var data = wizarddata as object[];
                var voucherClient = new VouchersClient();
                var voucherList = data[0] as IEnumerable<VouchersClient>;
                voucherClient.Text = data[1] as string;
                voucherClient.Content = data[2] as string;
                var documentApi = new DocumentAPI(api);

                result = await documentApi.CreateFolder(voucherClient, voucherList);
            }
            return result;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.VouchersPage2 || screenName == TabControls.GLOffsetAccountTemplate)
                dgVoucherGrid.UpdateItemSource(argument);
            else if (screenName == TabControls.VoucherFolderPage)
            {
                dgVoucherGrid.Filter(null);
                if (selectedPageIndex == 1)
                    dgVoucherGrid.FilterString = "Contains([Folder],'true')";
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            Utility.SetDimensionsGrid(api, clDim1, clDim2, clDim3, clDim4, clDim5);
            VatOperation.Visible = !Comp._UseVatOperation;
        }

        private void Offeset_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallOffsetAccount((sender as Image).Tag as VouchersClient);
        }

        private void CallOffsetAccount(VouchersClient vouchersClientLine)
        {
            if (vouchersClientLine != null)
            {
                dgVoucherGrid.SetLoadedRow(vouchersClientLine);
                var header = string.Format("{0}:{1} {2}", Uniconta.ClientTools.Localization.lookup("OffsetAccountTemplate"), Uniconta.ClientTools.Localization.lookup("Voucher"), vouchersClientLine.RowId);
                AddDockItem(TabControls.GLOffsetAccountTemplate, vouchersClientLine, header: header);
            }
        }

        public override string NameOfControl
        {
            get { return TabControls.Vouchers.ToString(); }
        }

        void SetFooterDetailText()
        {
            txtCreditorName.Text = VouchersClientText.CreditorName;
            txtCostAccount.Text = VouchersClientText.CostAccountName;
            txtPaymentAccount.Text = VouchersClientText.PayAccountName;
            txtApprover1.Text = VouchersClientText.Approver1Name;
            txtApprover2.Text = VouchersClientText.Approver2Name;
        }

        private async void SendToBilagscan(bool humanValidation)
        {
#if !SILVERLIGHT
            var accessToken = await Account.GetBilagscanAccessToken(api);

            // Verify organization
            switch (await Account.VerifyOrganization(api, accessToken))
            {
                case "NOCOMPANY":
                    UnicontaMessageBox.Show("Plug-in'en kan ikke finde et firma.", "Overfør til Bilagscan");
                    break;
                case "NOTABLE":
                    //Tools.CreateUserTable(api);
                    //await Account.CreateOrganization(api, Globals.access_token);
                    break;
                case "NODATA":
                    if (string.IsNullOrEmpty(api.CompanyEntity._Id))
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("CompanyRegNo")), Uniconta.ClientTools.Localization.lookup("Bilagscan"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        return;
                    }

                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("BilagscanTerms"), Uniconta.ClientTools.Localization.lookup("Bilagscan"), MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                        return;

                    await Account.CreateOrganization(api, accessToken);
                    break;
                case "NOORGANIZATION":
                    UnicontaMessageBox.Show("Plug-in'en kan ikke finde din organisation hos Bilagscan.", "Overfør til Bilagscan");
                    break;
                default:
                    Console.WriteLine("Default case");
                    break;
            }

            var vouchers = dgVoucherGrid.GetVisibleRows();

            for (var i = vouchers.Count - 1; i >= 0; i--)
            {
                var voucher = vouchers[i] as VouchersClient;

                // Remove from list if voucher is already sent to Bilagscan
                if (voucher._SentToBilagscan == true)
                {
                    vouchers.RemoveAt(i);
                }
            }

            string orgNo = string.Empty;

            if (vouchers.Count > 0)
            {
                var filter = new List<PropValuePair>
                {
                    PropValuePair.GenereteWhereElements("CompanyId", typeof(int), api.CompanyEntity.CompanyId.ToString())
                };
                var companySettings = await api.Query<CompanySettingsClient>(filter);

                orgNo = companySettings.FirstOrDefault()._OrgNumber.ToString();

                //if (humanValidation)
                //    await Account.Enable100Percent(orgNo, accessToken);
                //else
                //    await Account.Disable100Percent(orgNo, accessToken);
            }

            var voucherUpdates = new List<VouchersClient>();
            var voucherErrors = new List<VouchersClient>();
            var errorText = new StringBuilder("\r\n" + Uniconta.ClientTools.Localization.lookup("Bilagscan") + "\r\n");
            foreach (var v in vouchers)
            {
                var voucher = v as VouchersClient;

                await api.Read(voucher);
                if (await Bilagscan.Voucher.Upload(voucher, orgNo, accessToken))
                {
                    voucherUpdates.Add(voucher);
                }
                else
                {
                    voucherErrors.Add(voucher);
                    errorText.AppendLine(voucher.Text);
                }
                voucher.SentToBilagscan = true;
                voucher._Data = null;
            }
            if (voucherUpdates.Count > 0)
            {
                await api.Update(voucherUpdates);
            }

            var messageText = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BilagscanSent"), voucherUpdates.Count);
            if (voucherErrors.Count > 0)
            {
                messageText = messageText + "\r\n" + errorText.ToString();
                await api.Update(voucherErrors);
            }

            UnicontaMessageBox.Show(messageText, Uniconta.ClientTools.Localization.lookup("Bilagscan"));
#endif
        }

        private bool readingFromBilagscan = false;
        private async void RecieveFromBilagscan()
        {
#if !SILVERLIGHT
            if (!readingFromBilagscan)
            {
                readingFromBilagscan = true;

                var processLines = false;
                var accessToken = await Bilagscan.Account.GetBilagscanAccessToken(api);
                var noOfVouchers = 0;
                var companySettings = await api.Query<CompanySettingsClient>();
                var orgNo = companySettings.FirstOrDefault()._OrgNumber;

                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    var response = await client.GetAsync($"https://api.bilagscan.dk/v1/organizations/" + orgNo.ToString() + "/vouchers?seen=false&count=100&offset=0&sorts=-upload_date&status=successful");
                    var content = await response.Content.ReadAsStringAsync();
                    var vouchers = JsonConvert.DeserializeObject<BilagscanVoucher.Processed>(content);

                    var credCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor));
                    var offsetCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount));
                    var vouchersSeen = new CommaDelimitedStringCollection();

                    var updateLines = new List<UnicontaBaseEntity>();

                    if (vouchers != null && vouchers.data != null)
                    {
                        foreach (var voucher in vouchers.data)
                        {
                            vouchersSeen.Add(voucher.id.ToString());
                            var hint = JsonConvert.DeserializeObject<BilagscanWrite.Hint>(voucher.note);
                            if (hint != null)
                            {
                                var originalVoucher = new Uniconta.ClientTools.DataModel.VouchersClient()
                                {
                                    PrimaryKeyId = hint.RowId
                                };
                                await api.Read(originalVoucher);

                                var voucherNumber = voucher.header_fields.Where(hf => string.Compare(hf.code, "voucher_number", true) == 0).FirstOrDefault().value;
                                originalVoucher.Invoice = NumberConvert.ToInt(voucherNumber);

                                var creditorNumber = voucher.header_fields.Where(hf => string.Compare(hf.code, "company_vat_reg_no", true) == 0).FirstOrDefault().value;
                                originalVoucher._CreditorAccount = creditorNumber;

                                var paymentCodeId = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_code_id", true) == 0).FirstOrDefault().value;

                                var totalAmountInclVat = NumberConvert.ToDoubleNoThousandSeperator(voucher.header_fields.Where(hf => string.Compare(hf.code, "total_amount_incl_vat", true) == 0).FirstOrDefault().value);
                                originalVoucher._Amount = totalAmountInclVat;

                                //var dd = voucher.header_fields.Where(hf => hf.code, "payment_date").FirstOrDefault().value;
                                //var paymentDate = dd == "" ? DateTime.Today : StringSplit.DateParse(dd, DateFormat.ymd);
                                //originalVoucher._PayDate = paymentDate;

                                var paymentId = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_id", true) == 0).FirstOrDefault().value;

                                var country = voucher.header_fields.Where(hf => string.Compare(hf.code, "country", true) == 0).FirstOrDefault().value;
                                var countryCode = CountryCode.Denmark;
                                switch (country)
                                {
                                    case "DK":
                                    default:
                                        countryCode = CountryCode.Denmark;
                                        break;
                                }

                                var dt = voucher.header_fields.Where(hf => string.Compare(hf.code, "invoice_date", true) == 0).FirstOrDefault().value;
                                var invoiceDate = dt == "" ? DateTime.Today : StringSplit.DateParse(dt, DateFormat.ymd);
                                originalVoucher._PostingDate = invoiceDate;

                                var ibanNo = voucher.header_fields.Where(hf => string.Compare(hf.code, "payment_iban", true) == 0).FirstOrDefault().value;

                                var currency = voucher.header_fields.Where(hf => string.Compare(hf.code, "currency", true) == 0).FirstOrDefault().value;
                                originalVoucher.Currency = currency;

                                var paymentMethod = PaymentMethods.FIK;
                                if (countryCode == CountryCode.Denmark && !string.IsNullOrEmpty(paymentCodeId))
                                    paymentMethod = PaymentMethods.FIK;
                                else
                                    paymentMethod = PaymentMethods.IBAN;

                                if (paymentMethod == PaymentMethods.FIK)
                                {
                                    switch (paymentCodeId)
                                    {
                                        case "71":
                                            originalVoucher._PaymentMethod = PaymentTypes.PaymentMethod3;
                                            break;
                                        case "73":
                                            originalVoucher._PaymentMethod = PaymentTypes.PaymentMethod4;
                                            break;
                                        case "75":
                                            originalVoucher._PaymentMethod = PaymentTypes.PaymentMethod5;
                                            break;
                                        case "04":
                                            originalVoucher._PaymentMethod = PaymentTypes.PaymentMethod6;
                                            break;
                                        default:
                                            originalVoucher._PaymentMethod = PaymentTypes.VendorBankAccount;
                                            break;
                                    }

                                    if (!string.IsNullOrEmpty(paymentId))
                                    {
                                        var paymentId1 = paymentId.Substring(0, paymentId.Length - 8);
                                        var paymentId2 = paymentId.Substring(paymentId.Length - 8);

                                        originalVoucher._PaymentId = string.Format("{0}+{1}", paymentId1, paymentId2);
                                    }
                                }
                                else
                                {
                                    originalVoucher._PaymentMethod = PaymentTypes.IBAN;
                                    originalVoucher._PaymentId = ibanNo;
                                }

                                if (hint.CreditorAccount != null)
                                    originalVoucher._CreditorAccount = hint.CreditorAccount;
                                if (hint.Amount != 0)
                                    originalVoucher._Amount = hint.Amount;
                                if (hint.Currency != null)
                                    originalVoucher.Currency = hint.Currency;
                                if (hint.PaymentId != null)
                                {
                                    originalVoucher._PaymentId = hint.PaymentId;
                                    originalVoucher.PaymentMethod = hint.PaymentMethod;
                                }

                                var creditors = credCache.GetRecords as Uniconta.DataModel.Creditor[];
                                Uniconta.DataModel.Creditor creditor = null;

                                foreach (var c in creditors)
                                {
                                    if (c != null)
                                    {
                                        if (c._LegalIdent == creditorNumber)
                                        {
                                            creditor = c;
                                            break;
                                        }
                                    }
                                }

                                if (creditor == null && !string.IsNullOrEmpty(creditorNumber))
                                {
                                    foreach (var c in creditors)
                                    {
                                        if (c != null)
                                        {
                                            if (c._LegalIdent == creditorNumber.Remove(0, 2))
                                            {
                                                creditor = c;
                                                creditorNumber = creditorNumber.Remove(0, 2);
                                                originalVoucher._CreditorAccount = creditorNumber;
                                            }
                                        }
                                    }
                                }

                                if (creditor == null)
                                {
                                    var newCreditor = new CreditorClient()
                                    {
                                        Account = creditorNumber,
                                        CompanyRegNo = creditorNumber,
                                        _PaymentMethod = originalVoucher._PaymentMethod
                                    };
                                    CompanyInfo companyInformation = null;
                                    try
                                    {
                                        companyInformation = await CVR.CheckCountry(creditorNumber, countryCode);
                                    }
                                    catch (Exception ex)
                                    {
                                        UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
                                        return;
                                    }
                                    if (companyInformation != null)
                                    {
                                        if (companyInformation.life != null)
                                            newCreditor.Name = companyInformation.life.name;

                                        if (companyInformation.address != null)
                                        {
                                            newCreditor.Address1 = companyInformation.address.CompleteStreet;
                                            newCreditor.ZipCode = companyInformation.address.zipcode;
                                            newCreditor.City = companyInformation.address.cityname;
                                            newCreditor.Country = companyInformation.address.Country;
                                        }

                                        if (companyInformation.contact != null)
                                        {
                                            newCreditor.Phone = companyInformation.contact.phone;
                                            newCreditor.ContactEmail = companyInformation.contact.email;
                                        }

                                        if (totalAmountInclVat > 0)
                                            originalVoucher.Text = Uniconta.ClientTools.Localization.lookup("Invoice") + ": " + newCreditor.Name;
                                        else
                                            originalVoucher.Text = Uniconta.ClientTools.Localization.lookup("Creditnote") + ": " + newCreditor.Name;
                                    }

                                    await api.Insert(newCreditor);
                                    originalVoucher._CreditorAccount = creditorNumber;
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(creditor._PostingAccount))
                                        originalVoucher._CostAccount = creditor._PostingAccount;

                                    if (totalAmountInclVat > 0)
                                        originalVoucher.Text = Uniconta.ClientTools.Localization.lookup("Invoice") + ": " + creditor._Name;
                                    else
                                        originalVoucher.Text = Uniconta.ClientTools.Localization.lookup("Creditnote") + ": " + creditor._Name;
                                }
                                updateLines.Add(originalVoucher);
                                noOfVouchers += 1;
                            }
                        }
                    }

                    api.UpdateNoResponse(updateLines);

                    if (vouchersSeen.Count != 0)
                    {
                        // Mark vouchers as seen
                        string serializedRequest = "{ \"vouchers\": [ " + vouchersSeen.ToString() + " ] }";
                        var vContent = new StringContent(serializedRequest, Encoding.UTF8, "application/json");
                        response = await client.PostAsync($"https://api.bilagscan.dk/v1/organizations/" + orgNo.ToString() + "/vouchers/seen", vContent);
                        var res = await response.Content.ReadAsStringAsync();
                    }
                }

                if (noOfVouchers == 0)
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("StillProcessingTryAgain"), Uniconta.ClientTools.Localization.lookup("Bilagscan")), Uniconta.ClientTools.Localization.lookup("Bilagscan"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                {
                    UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("NumberOfImportedVouchers"), noOfVouchers), Uniconta.ClientTools.Localization.lookup("Bilagscan"), MessageBoxButton.OK, MessageBoxImage.Information);

                    localMenu_OnItemClicked("RefreshGrid");
                }

                readingFromBilagscan = false;
            }
#endif
        }
    }
}
