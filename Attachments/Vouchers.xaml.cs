using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using UnicontaClient.Utilities;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using System.Threading.Tasks;
using Uniconta.API.GeneralLedger;
using System.Windows;
using Uniconta.ClientTools.Util;
using Uniconta.DataModel;
using System.Collections;
using Uniconta.API.Service;
using UnicontaClient.Controls.Dialogs;
using Uniconta.Common.Utility;
using UnicontaClient.Controls;
using DevExpress.Xpf.Bars;
using UnicontaClient.Pages.Attachments;
using Uniconta.API.DebtorCreditor;
using DevExpress.Xpf.Grid;
using Localization = Uniconta.ClientTools.Localization;
using Bilagscan;
using Uniconta.Common.User;
using System.Diagnostics;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VouchersGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VouchersClient); } }
        public override bool Readonly { get { return false; } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool CanInsert { get { return _canInsert; } }
        public override IComparer GridSorting { get { return new SortDocAttached(); } }
        public override bool IsAutoSave { get { return false; } }
        private bool _canInsert;
        async public override Task<ErrorCodes> SaveData()
        {
            SelectedItem = null;
            /*
             * Awaiting the save data as we need to have rowid
             * With row id we would be able to save the buffers seperately
             * So first we save the records and then update the buffers
             */
            var addedRows = AddedRows as List<CorasauDataRow>;
            var addedRowCount = addedRows.Count;

            if (addedRowCount > 0)
            {
                var vouchersClient = new VouchersClient[addedRowCount];
                var buffers = new byte[addedRowCount][];
                int iCtr;
                VouchersClient rec;
                for (iCtr = 0; (iCtr < addedRowCount); iCtr++)
                {
                    rec = addedRows[iCtr].DataItem as VouchersClient;
                    vouchersClient[iCtr] = rec;
                    if (rec._Fileextension == FileextensionsTypes.JPEG ||
                        rec._Fileextension == FileextensionsTypes.BMP ||
                        rec._Fileextension == FileextensionsTypes.GIF ||
                        rec._Fileextension == FileextensionsTypes.TIFF)
                    {
                        rec._ScanDoc = true;
                        var imageBytes = FileBrowseControl.ImageResize(rec._Data, ".jpg");
                        if (imageBytes != null)
                        {
                            rec._Data = imageBytes;
                            rec._Fileextension = FileextensionsTypes.JPEG;
                            rec._NoCompress = true;
                        }
                    }
                    else if (rec._Fileextension == FileextensionsTypes.PDF ||
                             rec._Fileextension == FileextensionsTypes.PNG)
                        rec._ScanDoc = true;

                    if (rec._Data != null && rec._Data.Length > TableAddOnData.MaxDocSize)
                    {
                        rec._SizeKB = (rec._Data.Length + 512) >> 10;
                        rec.NotifyPropertyChanged("SizeKB");
                        return ErrorCodes.FileIsTooBig;
                    }

                    buffers[iCtr] = rec._Data;
                    rec._Data = null;
                }

                var result = await base.SaveData();
                if (result != ErrorCodes.Succes)
                {
                    for (iCtr = 0; (iCtr < addedRowCount); iCtr++)
                    {
                        rec = addedRows[iCtr].DataItem as VouchersClient;
                        rec._Data = buffers[iCtr];
                    }
                    return result;
                }

                Utility.UpdateBuffers(api, buffers, vouchersClient);
                this.RefreshData();

                return 0;
            }
            return await base.SaveData();
        }

        public override IList ModifyDraggedRows(DevExpress.Xpf.Grid.DragDrop.DataControlDropEventArgs e)
        {
            var tableArgs = e as DevExpress.Xpf.Grid.DragDrop.GridDropEventArgs;
            if (tableArgs == null)
                return base.ModifyDraggedRows(e);
            var tr = tableArgs.TargetRow as VouchersClient;
            foreach (var row in e.DraggedRows)
            {
                if (row is VouchersClient dr)
                {
                    this.SetLoadedRow(dr);
                    dr._ViewInFolder = tr._ViewInFolder;
                    this.SetModifiedRow(dr);
                }
                else if (row is UserDocsClient userDoc)
                {
                    var result = api.Read(userDoc).Result;
                    if (result == ErrorCodes.Succes)
                    {
                        var userDocs = new List<UnicontaBaseEntity>() { userDoc };
                        _canInsert = true;
                        PasteRows(userDocs);
                        _canInsert = false;
                    }
                }
            }
            return tableArgs.DraggedRows;
        }

        public override IEnumerable<UnicontaBaseEntity> ConvertPastedRows(IEnumerable<UnicontaBaseEntity> copyFromRows)
        {
            if (copyFromRows.FirstOrDefault() is UserDocsClient)
            {
                var t = api.CompanyEntity.GetUserTypeNotNull(typeof(VouchersClient));
                var lst = new List<UnicontaBaseEntity>(copyFromRows.Count());
                foreach (var row in copyFromRows)
                {
                    var doc = (UserDocsClient)row;
                    var voucher = Activator.CreateInstance(t) as VouchersClient;
                    voucher.SetMaster(api.CompanyEntity);
                    voucher._ScanDoc = true;
                    voucher._Text = doc._Text;
                    voucher._Fileextension = doc.DocumentType;
                    voucher._Data = doc._Data;
                    voucher._Url = doc._Url;
                    lst.Add(voucher);
                }
                return lst;
            }
            return null;
        }
    }

    public partial class Vouchers : GridBasePage
    {
        object cache;
        public static bool IsGridMode = false;
        ItemBase ibaseGridMode;
        RibbonBase rb;

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
            LoadNow(typeof(Uniconta.DataModel.DocumentFolder));

            InitializeComponent();
            cache = VoucherCache.HoldGlobalVoucherCache;
            dgVoucherGrid.RowDoubleClick += dgVoucherGrid_RowDoubleClick;
            localMenu.dataGrid = dgVoucherGrid;

            var api = this.api;
            this.LedgerCache = api.GetCache(typeof(Uniconta.DataModel.GLAccount));
            this.CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor));
            this.PaymentCache = api.GetCache(typeof(Uniconta.DataModel.PaymentTerm));
            this.FolderCache = api.GetCache(typeof(Uniconta.DataModel.DocumentFolder));

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
            rb = (RibbonBase)localMenu.DataContext;
            if (!api.CompanyEntity._UseVatOperation)
                VatOperation.Visible = false;

            PreviewKeyDown += RootVisual_KeyDown;
            dgVoucherGrid.tableView.AllowDragDrop = true;
            dgVoucherGrid.tableView.DropRecord += dgVoucherGridView_DropRecord;
            dgVoucherGrid.tableView.DragRecordOver += dgVoucherGridView_DragRecordOver;

            accordionView.MouseLeftButtonUp += AccordionView_MouseLeftButtonUp;
            dgVoucherGrid.ItemsSourceChanged += DgVoucherGrid_ItemsSourceChanged;
            dgVoucherGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            if (rb != null)
            {
                var homeTab = UtilDisplay.GetTabByName(rb, "Home");
                var scanMail = api.CompanyEntity.ScanEmail;
                if (homeTab != null && !string.IsNullOrEmpty(scanMail))
                    homeTab.Name = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("Home"), scanMail);
            }
            SetFooterDetailText();

            var menuToRemove = new string[2] { "UnicontaScan", "Bilagscan" };
            switch (api.CompanyEntity.DocumentScanner)
            {
                case PayableDocumentScanners.Paperflow:
                    menuToRemove = new string[2]
                    {
                        "UnicontaScan",
                        api.CompanyEntity._AutoScanVoucher ? "DocumentScanUpload" : ""
                    };
                    break;
                case PayableDocumentScanners.Azure:
                    menuToRemove = new string[2]
                    {
                        "Bilagscan",
                        api.CompanyEntity._AutoScanVoucher ? "UnicontaScan" : ""
                    };
                    break;
            }
            UtilDisplay.RemoveMenuCommand(rb, menuToRemove);
        }

        public async override Task InitQuery()
        {
            Task<SQLCache> ft;
            if (FolderCache == null)
                ft = api.LoadCache(typeof(Uniconta.DataModel.DocumentFolder));
            else
                ft = null;

            var t = base.InitQuery();

            if (ft != null)
                FolderCache = await ft;

            await t;

            if (CreditorCache != null)
                CreditorCacheReload(false);

            BindFolders();
            CreateContextMenu();
            accordionView.SelectedItem = ((UnicontaClient.Controls.AccordionFilterGroup)(accordionView.Items.FirstOrDefault())).FilterItems[0];
        }

        List<string> Folders;
        void BindFolders(string deletedItem = null)
        {
            Folders = DocumentFolderClient.GetFolders(FolderCache);
            if (!string.IsNullOrEmpty(deletedItem) && Folders.Contains(deletedItem))
                Folders.Remove(deletedItem);
            accordionView.ItemsSource = null;
            var allItemLabel = Localization.lookup("All");
            var folderGroup = new AccordionFilterGroup(Localization.lookup("Folders"), "ViewInFolder",
                Folders, allItemLabel);

            //Updating the Filter counts
            var folderCount = UpdateFolderItemCount(allItemLabel != null);
            if (folderCount != null && folderCount.Count > 0)
                folderGroup.UpdateFilterCount(folderCount);

            accordionView.ItemsSource = new AccordionFilterGroup[] { folderGroup };
        }

        private IList<int> UpdateFolderItemCount(bool showAll)
        {
            if (Folders == null || dgVoucherGrid.ItemsSource == null)
                return null;

            var itmSrc = (IEnumerable<VouchersClient>)dgVoucherGrid.ItemsSource;
            var updCountFolders = showAll ? new List<int>(Folders.Count + 1) { itmSrc.Count() } : new List<int>(Folders.Count);
            foreach (var fld in Folders)
                updCountFolders.Add(GetCount(fld));

            return updCountFolders;
        }

        private int GetCount(string folderString)
        {
            try
            {
                var itmSrc = (IEnumerable<VouchersClient>)dgVoucherGrid.ItemsSource;
                var filterString = string.Format("[ViewInFolder] = '{0}'", folderString);
                var criteria = DevExpress.Data.Filtering.CriteriaOperator.Parse(filterString, folderString);
                var caseInsensitiveCriteria = DevExpress.Data.Helpers.StringsTolowerCloningHelper.Process(criteria);
                var genericWhere = DevExpress.Data.Utils.CriteriaOperatorToExpressionConverter.GetGenericWhere<VouchersClient>(caseInsensitiveCriteria);
                var count = itmSrc.Where(genericWhere.Compile()).Count();

                return count;
            }
            catch
            {
                return -1;
            }
        }

        private void AccordionView_MouseLeftButtonUp(object sender, MouseEventArgs e)
        {
            var txt = e.OriginalSource as TextBlock;
            if (txt == null)
                return;

            if (dgVoucherGrid.dragStarted)
            {
                var bindingExp = txt.GetBindingExpression(TextBlock.TextProperty);
                if (bindingExp != null && bindingExp.DataItem is AccordionFilterItem)
                {
                    var filter = bindingExp.DataItem as AccordionFilterItem;
                    SetRowsWithFolderValue(filter.FilterName);
                    dgVoucherGrid.dragStarted = false;
                }
            }
        }

        void SetRowsWithFolderValue(string binText)
        {
            int index = Folders.IndexOf(binText);
            if (index > -1)
            {
                var selectedItems = dgVoucherGrid.SelectedItems ?? new List<object> { dgVoucherGrid.SelectedItem };
                if (selectedItems != null)
                {
                    foreach (var row in selectedItems)
                    {
                        var dr = row as VouchersClient;
                        if (dr != null)
                        {
                            if (dr._ViewInFolder != (byte)index)
                            {
                                dgVoucherGrid.SetLoadedRow(dr);
                                dr.ViewInFolder = binText;
                                dgVoucherGrid.SetModifiedRow(dr);
                            }
                        }
                    }
                }
                dgVoucherGrid.RefreshData();
                BindFolders();
            }
        }

        bool load = false;
        private void DgVoucherGrid_ItemsSourceChanged(object sender, DevExpress.Xpf.Grid.ItemsSourceChangedEventArgs e)
        {
            if (!load)
            {
                ChangeScreenViewMode();
                load = true;
            }

            if (Folders != null && Folders.Count > 0)
                BindFolders();
        }

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

                errors = Utility.DropFilesToGrid(dgVoucherGrid, files, true);
            }
            else if (e.Data.GetDataPresent("FileGroupDescriptor"))
                errors = Utility.DropOutlookMailsToGrid(dgVoucherGrid, e.Data, true);

            if (errors != null && errors.Length > 0)
            {
                var cwErrorBox = new CWErrorBox(errors, true);
                cwErrorBox.Show();
            }
            else
            {
                var items = accordionView.Items.Cast<AccordionFilterGroup>();
                if (items.Count() > 0)
                {
                    var grp = items.FirstOrDefault();
                    if (grp != null && grp.FilterItems != null && grp.FilterItems.Count > 0)
                    {
                        var filterItemInbox = grp.FilterItems?.Where(f => f.FilterName == Localization.lookup("Inbox")).FirstOrDefault();
                        accordionView.SelectedItem = filterItemInbox ?? grp.FilterItems[0];
                    }
                }
            }
        }
        void ChangeScreenViewMode()
        {
            if (IsGridMode)
            {
                colAccordion.Width = new GridLength(0);
                accordionView.Visibility = Visibility.Collapsed;
                dgVoucherGrid.FilterString = string.Empty;
            }
            else
            {
                colAccordion.Width = new GridLength();
                accordionView.Visibility = Visibility.Visible;
            }
            if (ibaseGridMode == null)
                ibaseGridMode = UtilDisplay.GetMenuCommandByName(rb, "PageViewMode");

            if (IsGridMode)
                ibaseGridMode.Caption = string.Format(Localization.lookup("ShowOBJ"), Localization.lookup("Folders"));
            else
                ibaseGridMode.Caption = string.Format(Localization.lookup("HideOBJ"), Localization.lookup("Folders"));

        }

        private void accordionView_SelectedItemChanged(object sender, DevExpress.Xpf.Accordion.AccordionSelectedItemChangedEventArgs e)
        {
            if (e.NewItem != null)
            {
                var filterItem = e.NewItem as AccordionFilterItem;
                var currentFilterString = dgVoucherGrid.FilterString;

                if (!string.IsNullOrEmpty(filterItem.FilterString))
                {
                    if (string.IsNullOrEmpty(currentFilterString))
                        dgVoucherGrid.FilterString = filterItem.FilterString;
                    else if (currentFilterString.Contains("[ViewInFolder]"))
                        dgVoucherGrid.FilterString = currentFilterString.Replace(GetViewInFolderCurrentFilterValue(currentFilterString),
                            GetViewInFolderCurrentFilterValue(filterItem.FilterString));
                    else
                        dgVoucherGrid.FilterString = string.Format("{0} And {1}", filterItem.FilterString, currentFilterString);
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentFilterString))
                    {
                        string filterStr = string.Empty;
                        if (currentFilterString.Contains(" And "))
                        {
                            var andFilters = currentFilterString.Split(new string[] { "And" }, StringSplitOptions.None);
                            filterStr = string.Join(" And ", andFilters.Where(x => !x.Contains("[ViewInFolder]")));
                        }
                        else if (currentFilterString.Contains(" Or "))
                        {
                            var orFilters = currentFilterString.Split(new string[] { "Or" }, StringSplitOptions.None);
                            filterStr = string.Join(" Or ", orFilters.Where(x => !x.Contains("[ViewInFolder]")));
                        }
                        dgVoucherGrid.FilterString = filterStr;
                    }
                }
            }
        }

        private string GetViewInFolderCurrentFilterValue(string filterString)
        {
            var viewInFolderIndex = filterString.IndexOf("[ViewInFolder]");

            var startIndex = filterString.IndexOf('\'', viewInFolderIndex);
            var endIndex = filterString.IndexOf('\'', startIndex + 1);

            return filterString.Substring(startIndex, (endIndex - startIndex) + 1);
        }

        SQLCache LedgerCache, CreditorCache, PaymentCache, ProjectCache, TextTypes, FolderCache;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (this.CreditorCache == null)
                this.CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (this.LedgerCache == null)
                this.LedgerCache = await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            if (this.PaymentCache == null)
                this.PaymentCache = await api.LoadCache(typeof(Uniconta.DataModel.PaymentTerm)).ConfigureAwait(false);
            LoadType(new Type[] { typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.GLVat) });
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
            Uniconta.DataModel.GLAccount Acc;
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
                    if (dc._PostingAccount != null)
                        rec.CostAccount = dc._PostingAccount;
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
                    if (dc._Currency != 0)
                        rec.Currency = AppEnums.Currencies.ToString((int)dc._Currency);
                    if (dc._Vat != null && rec._Vat == null)
                    {
                        if (rec._CostAccount != null)
                        {
                            Acc = (Uniconta.DataModel.GLAccount)LedgerCache?.Get(rec._CostAccount);
                            if (Acc != null && Acc._MandatoryTax == VatOptions.NoVat)
                                return;
                        }
                        rec.Vat = dc._Vat;
                        rec.VatOperation = dc._VatOperation;
                    }
                    if (rec._PaymentMethod != dc._PaymentMethod)
                    {
                        rec._PaymentMethod = dc._PaymentMethod;
                        rec.NotifyPropertyChanged("PaymentMethod");
                    }
                    if (dc._Employee != null)
                        rec.Approver1 = dc._Employee;
                    if (dc._TransType != null)
                        rec.TransType = dc._TransType;

                    SetPayDate(PaymentCache, rec, dc);
                    rec.UpdateDefaultText();
                    break;
                case "CostAccount":
                    Acc = (Uniconta.DataModel.GLAccount)LedgerCache?.Get(rec._CostAccount);
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
                    if (Acc._Dim1 != null)
                        rec.Dimension1 = Acc._Dim1;
                    if (Acc._Dim2 != null)
                        rec.Dimension2 = Acc._Dim2;
                    if (Acc._Dim3 != null)
                        rec.Dimension3 = Acc._Dim3;
                    if (Acc._Dim4 != null)
                        rec.Dimension4 = Acc._Dim4;
                    if (Acc._Dim5 != null)
                        rec.Dimension5 = Acc._Dim5;
                    break;
                case "PurchaseNumber":
                    if (rec._PurchaseNumber != 0 && api.CompanyEntity.Purchase)
                        rec.SetPurchaseNumber(api);
                    break;
                case "Invoice":
                    rec.UpdateDefaultText();
                    break;
                case "Project":
                    if (rec._Project != null)
                        lookupProjectDim(rec);
                    break;
                case "TransType":
                    if (rec._TransType != null)
                        SetTransText(rec);
                    break;
            }
        }

        async void SetTransText(VouchersClient rec)
        {
            if (TextTypes == null)
                TextTypes = api.GetCache(typeof(Uniconta.DataModel.GLTransType)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLTransType));
            var t = (Uniconta.DataModel.GLTransType)TextTypes?.Get(rec._TransType);
            if (t != null)
            {
                if (rec._Text == null)
                    rec.NotifyPropertyChanged("Text");
                if (t._AccountType == 0 && t._Account != null)
                    rec.CostAccount = t._Account;
                if (t._OffsetAccount != null)
                {
                    if (t._OffsetAccountType == 0)
                        rec.PayAccount = t._OffsetAccount;
                    else if (rec._CreditorAccount == null && t._OffsetAccountType == GLJournalAccountType.Creditor)
                        rec.CreditorAccount = t._OffsetAccount;
                }
            }
        }
        async void lookupProjectDim(VouchersClient rec)
        {
            if (ProjectCache == null)
                ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project));
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
                if (proj._PersonInCharge != null)
                {
                    if (rec._Approver1 == null)
                        rec.Approver1 = proj._PersonInCharge;
                    else if (rec._Approver1 != proj._PersonInCharge)
                        rec.Approver2 = proj._PersonInCharge;
                }
            }
        }

        int selectedPageIndex;
        private void RibbonControl_SelectedPageChanged(int selectedPageIndex)
        {
            this.selectedPageIndex = selectedPageIndex;
            if (selectedPageIndex == 1)
                dgVoucherGrid.FilterString = "Contains([Envelope],'true')";
            else
                dgVoucherGrid.FilterString = ""; //dgVoucherGrid.ClearFilter();
        }
        void dgVoucherGrid_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("ViewDownloadRow");
        }

        void Vouchers_BeforeClose()
        {
            cache = null;
            dgVoucherGrid.SelectedItem = null;
            this.PreviewKeyDown -= RootVisual_KeyDown;
            this.BeforeClose -= Vouchers_BeforeClose;
        }

        private void RootVisual_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (dgVoucherGrid.CurrentColumn.Name == "HasOffsetAccounts" && e.Key == Key.Down)
                {
                    var currentRow = dgVoucherGrid.SelectedItem as VouchersClient;
                    if (currentRow != null)
                        CallOffsetAccount(currentRow);
                }
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgVoucherGrid.SelectedItem as VouchersClient;

            switch (ActionType)
            {
                case "AddRow":
                    AddRow();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                    {
                        if (selectedItem._InJournal)
                            UtilDisplay.ShowErrorCode(ErrorCodes.CouldNotDeleteRecordThatIsReferred);
                        else
                        {
                            VoucherCache.RemoveGlobalVoucherCache(selectedItem);
                            dgVoucherGrid.DeleteRow();
                        }
                    }
                    break;
                case "Save":
                    Save();
                    break;

                case "RefreshGrid":
                    CreditorCacheReload(true);
                    break;
                case "ViewDownloadRow":
                case "ViewVoucher":
                    if (selectedItem != null)
                        ViewVoucher(TabControls.VouchersPage3, dgVoucherGrid.syncEntity);
                    break;
                case "ViewJournalLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.VoucherJournalLines, dgVoucherGrid.syncEntity, true, string.Format("{0}/{1}", Localization.lookup("Journallines"), selectedItem.RowId), "View_16x16");
                    break;
                case "GenerateJournalLines":
                    // save lines first.
                    dgVoucherGrid.SelectedItem = null;

                    api.AllowBackgroundCrud = false;
                    var task = dgVoucherGrid.SaveData();
                    api.AllowBackgroundCrud = true;

                    CWJournal journals = new CWJournal(api, true);
                    journals.DialogTableId = 2000000015;
                    journals.Closed += async delegate
                    {
                        if (journals.DialogResult == true)
                        {
                            var journalName = journals.Journal;
                            var journalDate = journals.Date;
                            var OnlyApproved = journals.OnlyApproved;

                            var errorCode = await task;  // make sure save is completed.
                            if (errorCode != ErrorCodes.Succes)
                            {
                                UtilDisplay.ShowErrorCode(errorCode);
                                return;
                            }

                            var postingApi = new PostingAPI(api);
                            postingApi.WorkSpace = journals.Workspace;
                            IEnumerable<VouchersClient> lst;
                            if (journals.OnlyCurrentRecord)
                            {
                                if (selectedItem == null)
                                    return;
                                OnlyApproved = false;
                                selectedItem.InJournal = true;
                                lst = new VouchersClient[] { selectedItem };
                            }
                            else
                                lst = dgVoucherGrid.GetVisibleRows() as IEnumerable<VouchersClient>;

                            if (object.ReferenceEquals(lst, dgVoucherGrid.ItemsSource))
                            {
                                task = postingApi.GenerateJournalFromDocument(journalName, journalDate, journals.IsCreditAmount, OnlyApproved, journals.AddVoucherNumber);
                                foreach (var doc in lst)
                                    if (!doc._OnHold)
                                        doc.InJournal = true;
                            }
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
                                task = postingApi.GenerateJournalFromDocument(journalName, journalDate, journals.IsCreditAmount, journals.AddVoucherNumber, lst);
                            }

                            busyIndicator.IsBusy = true;
                            errorCode = await task;
                            busyIndicator.IsBusy = false;

                            if (errorCode != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(errorCode);
                            else if (!journals.OnlyCurrentRecord)
                            {
                                var text = string.Concat(Localization.lookup("TransferedToJournal"), ": ", journalName,
                                    Environment.NewLine, string.Format(Localization.lookup("GoTo"), Localization.lookup("Journallines")), " ?");
                                var select = UnicontaMessageBox.Show(text, Localization.lookup("Information"), MessageBoxButton.OKCancel);
                                if (select == MessageBoxResult.OK)
                                {
                                    var header = string.Concat(Localization.lookup("Journal"), ": ", journalName);
                                    AddDockItem(TabControls.GL_DailyJournalLine, null, header, null, true, null, new[] { new BasePage.ValuePair("Journal", journalName) });
                                }
                            }
                        }
                    };
                    journals.Show();
                    break;
                case "PostJournal":
                    // save lines first.
                    dgVoucherGrid.SelectedItem = null;

                    api.AllowBackgroundCrud = false;
                    var savetask = dgVoucherGrid.SaveData();
                    api.AllowBackgroundCrud = true;

                    CWJournal cwjournal = new CWJournal(api, true);
                    cwjournal.ShowComments(true);
                    cwjournal.DialogTableId = 2000000104;
                    cwjournal.Closed += async delegate
                    {
                        if (cwjournal.DialogResult == true)
                        {
                            var journalName = cwjournal.Journal;
                            var journalDate = cwjournal.Date;
                            var OnlyApproved = cwjournal.OnlyApproved;
                            var comment = cwjournal.comments;
                            var simulate = cwjournal.IsSimulation;
                            var errorCode = await savetask;  // make sure save is completed.
                            if (errorCode != ErrorCodes.Succes)
                            {
                                UtilDisplay.ShowErrorCode(errorCode);
                                return;
                            }
                            busyIndicator.IsBusy = true;

                            var postingApi = new PostingAPI(api);
                            postingApi.WorkSpace = cwjournal.Workspace;
                            IEnumerable<VouchersClient> lst;
                            if (cwjournal.OnlyCurrentRecord)
                            {
                                if (selectedItem == null)
                                    return;
                                OnlyApproved = false;
                                lst = new VouchersClient[] { selectedItem };
                            }
                            else
                                lst = dgVoucherGrid.GetVisibleRows() as IEnumerable<VouchersClient>;
                            PostingResult postingResult;
                            if (object.ReferenceEquals(lst, dgVoucherGrid.ItemsSource))
                            {
                                postingResult = await postingApi.PostJournalFromDocument(journalName, journalDate, comment, simulate, cwjournal.IsCreditAmount, OnlyApproved, new GLTransClientTotal(), null);
                            }
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
                                        VoucherList.Add(doc);
                                    }
                                    lst = VoucherList;
                                }
                                postingResult = await postingApi.PostJournalFromDocument(journalName, journalDate, comment, simulate, cwjournal.IsCreditAmount, OnlyApproved, new GLTransClientTotal(), lst);
                            }

                            busyIndicator.IsBusy = false;
                            if (postingResult == null)
                                return;
                            if (postingResult.Err != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(postingResult.Err);
                            else if (postingResult.SimulatedTrans != null && postingResult.SimulatedTrans.Length > 0)
                                AddDockItem(TabControls.SimulatedTransactions, new object[] { postingResult.AccountBalance, postingResult.SimulatedTrans }, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                            else
                            {
                                string msg;
                                if (postingResult.JournalPostedlId != 0)
                                    msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), NumberConvert.ToString(postingResult.JournalPostedlId));
                                else
                                    msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                                UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                                gridRibbon_BaseActions("RefreshGrid");
                            }
                        }
                    };
                    cwjournal.Show();
                    break;
                case "GeneratePurchaseOrder":
                    if (selectedItem != null)
                        CreatePurchaseOrder(selectedItem);
                    break;

                case "AddEnvelope":
                    AddDockItem(TabControls.VoucherFolderPage, new object[] { dgVoucherGrid.GetChildInstance(), "Create" }, Localization.lookup("Envelope"), "Add_16x16");
                    break;
                case "EditEnvelope":
                    if (selectedItem != null)
                        AddDockItem(TabControls.VoucherFolderPage, new object[] { selectedItem, "Edit" }, selectedItem.Text, "Edit_16x16");
                    break;
                case "EnvelopeWizard":
                    LaunchEnvelopeWizard();
                    break;
                case "OffSetAccount":
                    CallOffsetAccount(selectedItem);
                    break;
                case "DocumentScanUpload":
                    if (api.CompanyEntity.DocumentScanner != PayableDocumentScanners.None)
                        DocumentScanUpload();
                    break;
                case "BilagscanProfile":
                    if (api.CompanyEntity.DocumentScanner == PayableDocumentScanners.Paperflow)
                        Process.Start("https://my.paperflow.com/");
                    break;
                case "BilagscanCreateUser":
                    if (api.CompanyEntity.DocumentScanner == PayableDocumentScanners.Paperflow)
                        CreatePaperFlowUser();
                    break;
                case "SplitPDF":
                    if (selectedItem != null)
                        LoadSplitPDFDialog(selectedItem);
                    break;

                case "JoinPDF":
                    JoinPDFVouchers();
                    break;
                case "PageViewMode":
                    IsGridMode = !IsGridMode;
                    ChangeScreenViewMode();
                    break;
                case "CopyOrMoveVoucher":
                    if (selectedItem != null)
                        CopyOrMoveAttachement(selectedItem);
                    break;
                case "RemoveFromInbox":
                    if (selectedItem != null)
                    {
                        if (selectedItem._Fileextension == FileextensionsTypes.UNK)
                            dgVoucherGrid.DeleteRow();
                        else
                            UpdateInBox(selectedItem);
                    }
                    break;
                case "ResendToApprover":
                    if (selectedItem == null || (selectedItem._Approver1 == null && selectedItem._Approver2 == null))
                        return;
                    var cw = new CwSendEmailToApprover(selectedItem);
                    cw.DialogTableId = 2000000069;
                    cw.Closing += async delegate
                    {
                        if (cw.DialogResult == true)
                        {
                            var docApi = new DocumentAPI(api);
                            var result = await docApi.SendEmail2Approver(selectedItem, cw.Approver, cw.Note);
                            UtilDisplay.ShowErrorCode(result);
                        }
                    };
                    cw.Show();
                    break;
                case "PendingApproval":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DocumentApproveAwaitPage, dgVoucherGrid.syncEntity, string.Format("{0}: {1}", Localization.lookup("PendingApproval"), selectedItem.RowId));
                    break;
                case "ConvertToPDF":
                    if (selectedItem != null)
                        ConvertToPDF(selectedItem);
                    break;
                case "MoveToCompany":
                    if (selectedItem != null)
                        MoveToCompany(selectedItem);
                    break;
                case "CheckIfPosted":
                    if (selectedItem != null)
                        CheckIfPosted(selectedItem);
                    break;
                case "CopyRecord":
                    if (selectedItem != null && !selectedItem.Envelope)
                        CopyRecord(selectedItem);
                    break;
                case "Attach":
                    if (selectedItem != null)
                    {
                        var transDialog = new CWSearchGLTrans(api);
                        transDialog.Closed += delegate
                        {
                            if (transDialog.DialogResult == true)
                            {
                                if (transDialog.SelectedRow != null)
                                    SaveAttachment(transDialog.SelectedRow, selectedItem);
                            }
                        };
                        transDialog.Show();
                    }
                    break;
                case "Transactions":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AccountsTransaction, dgVoucherGrid.SelectedItem, string.Format("{0}: {1}", Localization.lookup("Transactions"), selectedItem.RowId));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void CreditorCacheReload(bool Refresh)
        {
            var lst = dgVoucherGrid.ItemsSource as IEnumerable<VouchersClient>;
            if (lst != null && lst.Count() > 0)
            {
                var cache = this.CreditorCache;
                if (cache != null && lst.Any(r => r._CreditorAccount != null && cache.Get(r._CreditorAccount) == null))
                    await api.UpdateCache(); // no need to specify Creditor, since this takes Debtor, Creditor, InvItem, etc
            }
            if (Refresh)
            {
                if (dgVoucherGrid.HasUnsavedData)
                    Utility.ShowConfirmationOnRefreshGrid(dgVoucherGrid);
                else
                    gridRibbon_BaseActions("RefreshGrid");
            }
        }

        void SaveAttachment(GLTransClient selectedItem, VouchersClient doc)
        {
            if (selectedItem != null && doc != null)
            {
                CWForAllTrans cwconfirm = new CWForAllTrans();
                cwconfirm.Closing += async delegate
                {
                    if (cwconfirm.DialogResult == true)
                    {
                        if (selectedItem._DocumentRef != 0)
                            VoucherCache.RemoveGlobalVoucherCache(selectedItem.CompanyId, selectedItem._DocumentRef);
                        busyIndicator.IsBusy = true;
                        var postingApiInv = new PostingAPI(api);
                        var errorCodes = await postingApiInv.AddPhysicalVoucher(selectedItem, doc, cwconfirm.ForAllTransactions, cwconfirm.AppendDoc);
                        busyIndicator.IsBusy = false;
                        if (errorCodes == ErrorCodes.Succes)
                            dgVoucherGrid.RemoveFocusedRowFromGrid();
                        else
                            UtilDisplay.ShowErrorCode(errorCodes);
                    }
                };
                cwconfirm.Show();
            }
        }
        async void CopyRecord(VouchersClient selectedItem)
        {
            if (selectedItem._Data == null)
                await api.Read(selectedItem);
            var _Data = selectedItem._Data;
            selectedItem._Data = null;
            var voucher = Activator.CreateInstance(selectedItem.GetType()) as VouchersClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, voucher);
            voucher._Created = DateTime.MinValue;
            voucher._Data = selectedItem._Data = _Data;
            voucher._Approved = voucher._Approved2 = false;
            voucher._Approver1 = voucher._Approver2 = null;
            voucher._PurchaseNumber = 0;
            voucher._Amount = 0;
            voucher._CreditorAccount = null;
            voucher._Project = null;
            voucher._InJournal = false;
            voucher._SentToScanner = false;
            dgVoucherGrid.InsertRow(voucher, dgVoucherGrid.View.FocusedRowHandle < 0 ? -1 : dgVoucherGrid.View.FocusedRowHandle);
            dgVoucherGrid.SelectedItem = null;
            dgVoucherGrid.SelectedItem = voucher;
        }

        async void CheckIfPosted(VouchersClient selectedItem)
        {
            if (string.IsNullOrEmpty(selectedItem._Invoice))
            {
                UnicontaMessageBox.Show(string.Format(Localization.lookup("CannotBeBlank"), Localization.lookup("InvoiceNumber")),
                    Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }
            if (string.IsNullOrEmpty(selectedItem._CreditorAccount))
            {
                UnicontaMessageBox.Show(string.Format(Localization.lookup("CannotBeBlank"), Localization.lookup("CreditorsAccount")),
                    Localization.lookup("Information"), MessageBoxButton.OK);
                return;
            }

            var filter = new List<PropValuePair>(5)
            {
                PropValuePair.GenereteWhereElements("Invoice", selectedItem._Invoice, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("CreditorAccount", selectedItem._CreditorAccount, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("InEnvelope", "0", CompareOperator.Equal),
            };
            if (selectedItem.RowId > 3000) // we just search in the last 3000
                filter.Add(PropValuePair.GenereteWhereElements("RowId", NumberConvert.ToString(selectedItem.RowId - 3000), CompareOperator.GreaterThanOrEqual));
            // should be added after first RowId
            filter.Add(PropValuePair.GenereteWhereElements("RowId", selectedItem.RowId, CompareOperator.NotEqual)); // we do not want ourself

            busyIndicator.IsBusy = true;
            var lst = await api.Query(api.CompanyEntity.CreateUserType<VouchersClient>(), null, filter);
            if (lst != null && lst.Length > 0 && (lst.Length > 1 || lst[0].RowId != selectedItem.RowId))
            {
                var header = string.Concat(Localization.lookup("CreditorsAccount"), ": ", selectedItem._CreditorAccount, ", ", Localization.lookup("Invoice"), ": ", selectedItem._Invoice);
                AddDockItem(TabControls.AttachedVouchers, new object[] { lst, api }, header, null, true);
                busyIndicator.IsBusy = false;
            }
            else
            {
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(ErrorCodes.CouldNotFind);
            }
        }

        void MoveToCompany(VouchersClient selectedItem)
        {
            dgVoucherGrid.SaveData();
            var cw = new CWReadOnlyCompany(CWDefaultCompany.loadedCompanies, "MoveToCompany");
            cw.Closed += async delegate
            {
                if (cw.DialogResult == true)
                {
                    var result = await new DocumentAPI(api).DocumentMove(selectedItem, cw.selectedCompany);
                    if (result == ErrorCodes.Succes)
                    {
                        VoucherCache.RemoveGlobalVoucherCache(selectedItem);
                        dgVoucherGrid.UpdateItemSource(3, selectedItem);
                    }
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            cw.Show();
        }

        private async void ConvertToPDF(VouchersClient voucherClient)
        {
            var fileExt = voucherClient.Fileextension;
            if (fileExt != FileextensionsTypes.PDF)
            {
                if (fileExt != FileextensionsTypes.DOC && fileExt != FileextensionsTypes.DOCX && fileExt != FileextensionsTypes.TXT &&
                    fileExt != FileextensionsTypes.CSV && fileExt != FileextensionsTypes.RTF && fileExt != FileextensionsTypes.HTML &&
                    fileExt != FileextensionsTypes.MHT && fileExt != FileextensionsTypes.ODT && fileExt != FileextensionsTypes.JPEG &&
                    fileExt != FileextensionsTypes.PNG && fileExt != FileextensionsTypes.MSG && fileExt != FileextensionsTypes.EML)
                {
                    UnicontaMessageBox.Show(Localization.lookup("ConversionNotSupport"),Localization.lookup("Information"));
                    return;
                }

                if (UnicontaMessageBox.Show(Localization.lookup("AreYouSureToContinue"),
                    Localization.lookup("Confirmation"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;

                busyIndicator.IsBusy = true;
                await ValidateVoucher(voucherClient);
                var pdfBytes = UtilDisplay.ConvertToPDF(voucherClient.Buffer, voucherClient._Fileextension);

                if (pdfBytes == null || pdfBytes.Length == 0)
                {
                    busyIndicator.IsBusy = false;
                    UnicontaMessageBox.Show(Localization.lookup("PdfStreamfailed"), Localization.lookup("Error"));
                    return;
                }

                //To save any information on the editable grid
                saveGrid();

                voucherClient._Fileextension = FileextensionsTypes.PDF;
                voucherClient._Data = pdfBytes;
                voucherClient._NoCompress = true;
                VoucherCache.SetGlobalVoucherCache(voucherClient);
                var result = await api.Update(voucherClient);
                busyIndicator.IsBusy = false;

                if (result != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(result);
                else
                    dgVoucherGrid.Filter(null);
            }
        }
        private async void CreatePurchaseOrder(VouchersClient voucherClient)
        {
            // save lines first.
            dgVoucherGrid.SelectedItem = null;

            api.AllowBackgroundCrud = false;
            var tsk = dgVoucherGrid.SaveData();
            api.AllowBackgroundCrud = true;

            if (voucherClient != null)
            {
                var errorCode = await tsk;  // make sure save is completed.
                if (errorCode != ErrorCodes.Succes)
                    return;

                var orderApi = new OrderAPI(api);
                var dcOrder = this.CreateGridObject(typeof(CreditorOrderClient)) as CreditorOrderClient;
                var result = await orderApi.CreateOrderFromDocument(voucherClient, dcOrder);
                if (result != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(result);
                else
                {
                    ShowPurchaseOrderLines(dcOrder);
                    gridRibbon_BaseActions("RefreshGrid");
                }
            }
        }

        private void ShowPurchaseOrderLines(DCOrder order)
        {
            var confrimationText = string.Format(" {0}. {1}:{2},{3}:{4}\r\n{5}", Localization.lookup("PurchaseOrderCreated"), Localization.lookup("OrderNumber"), order._OrderNumber,
                Localization.lookup("Account"), order._DCAccount, string.Concat(string.Format(Localization.lookup("GoTo"), Localization.lookup("Orderline")), " ?"));

            var confirmationBox = new CWConfirmationBox(confrimationText, string.Empty, false);
            confirmationBox.Closing += delegate
            {
                if (confirmationBox.DialogResult == null)
                    return;

                switch (confirmationBox.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        AddDockItem(TabControls.CreditorOrderLines, order, string.Format("{0}:{1},{2}", Localization.lookup("OrdersLine"), order._OrderNumber, order._DCAccount));
                        break;

                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationBox.Show();
        }

        void AddRow()
        {
            object[] parameters = new object[2];
            parameters[0] = api;
            var accordionItem = accordionView.SelectedItem as AccordionFilterItem;
            if (accordionItem != null && !string.IsNullOrEmpty(accordionItem.FilterName) && Folders.IndexOf(accordionItem.FilterName) > -1)
                parameters[1] = accordionItem.FilterName;
            else
                parameters[1] = "";
            AddDockItem(TabControls.VouchersPage2, parameters, Localization.lookup("Vouchers"), "Add_16x16");
        }
        void UpdateInBox(VouchersClient selectedItem)
        {
            var rec = new DocumentNoRef();
            rec.SetMaster(selectedItem);
            string message = Localization.lookup("RemoveRecordPrompt");
            CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
            confirmationDialog.Closing += async delegate
            {
                if (confirmationDialog.DialogResult == null)
                    return;
                switch (confirmationDialog.ConfirmationResult)
                {
                    case CWConfirmationBox.ConfirmationResultEnum.Yes:
                        if (await api.Delete(rec) == 0)
                        {
                            dgVoucherGrid.UpdateItemSource(3, selectedItem);
                            BindFolders();
                        }
                        break;
                    case CWConfirmationBox.ConfirmationResultEnum.No:
                        break;
                }
            };
            confirmationDialog.Show();
        }

        private void CopyOrMoveAttachement(VouchersClient selectedItem)
        {
            Save();
            var cwAttachedVouchers = new CWAttachedVouchers(api, selectedItem);
            cwAttachedVouchers.Closed += async delegate
            {
                if (cwAttachedVouchers.DialogResult == true)
                {
                    ErrorCodes result;
                    var docAPI = new DocumentAPI(api);
                    busyIndicator.IsBusy = true;
                    result = await docAPI.CreateAttachment(selectedItem, cwAttachedVouchers.SelectRow, cwAttachedVouchers.IsDelete);
                    busyIndicator.IsBusy = false;

                    if (result != ErrorCodes.Succes)
                        UtilDisplay.ShowErrorCode(result);
                    else if (cwAttachedVouchers.IsDelete)
                    {
                        dgVoucherGrid.UpdateItemSource(3, selectedItem);
                        BindFolders();
                    }
                }
            };
            cwAttachedVouchers.Show();
        }

        async void JoinPDFVouchers()
        {
            VouchersClient voucher1 = null, voucher2 = null;
            CWJoinPDFDocument cwJoinPdfDoc = null;

            if (dgVoucherGrid.SelectedItems != null)
            {
                var vouchers = dgVoucherGrid.SelectedItems.Cast<VouchersClient>().ToArray();
                if (vouchers.Length == 0)
                    return;
                voucher1 = vouchers[0];
                await ValidateVoucher(voucher1);

                if (!TryConvertToPdf(voucher1))
                    return;

                if (vouchers.Length >= 2)
                {
                    voucher2 = vouchers[1];
                    await ValidateVoucher(voucher2);

                    if (!TryConvertToPdf(voucher2))
                        return;
                }

                if (voucher1._Fileextension != FileextensionsTypes.PDF || (voucher2 != null && voucher2._Fileextension != FileextensionsTypes.PDF))
                {
                    UnicontaMessageBox.Show(Localization.lookup("InvalidFileFormat"), Localization.lookup("Error"), MessageBoxButton.OK);
                    return;
                }
            }

            //To save any information on the editable grid
            saveGrid();

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
                         deleteMsg = string.Format(Localization.lookup("ConfirmDeleteOBJ"), string.Format("{0} {1}",
                             Localization.lookup("Left"), Localization.lookup("Voucher")));
                     else if (cwJoinPdfDoc.IsRightPdfDelete)
                         deleteMsg = string.Format(Localization.lookup("ConfirmDeleteOBJ"), string.Format("{0} {1}",
                            Localization.lookup("Right"), Localization.lookup("Voucher")));

                     var deleteVoucher = !string.IsNullOrEmpty(deleteMsg) && voucher1.RowId > 0 && UnicontaMessageBox.Show(deleteMsg, Localization.lookup("Warning"), MessageBoxButton.YesNo)
                            == MessageBoxResult.Yes ? true : false;

                     if (cwJoinPdfDoc.IsLeftJoin)
                         UpdateJoinedPDFContents(voucher1, voucher2, mergedContents, deleteVoucher);
                     else
                         UpdateJoinedPDFContents(voucher2, voucher1, mergedContents, deleteVoucher);
                 }
             };

            cwJoinPdfDoc.Show();
        }

        private bool TryConvertToPdf(VouchersClient voucher)
        {
            try
            {
                if (voucher._Data == null)
                {
                    UnicontaMessageBox.Show(string.Format(Localization.lookup("OBJisEmpty"), Localization.lookup("Data")), Localization.lookup("Error"), MessageBoxButton.OK);
                    return false;
                }

                if (voucher._Fileextension == FileextensionsTypes.PDF)
                    return true;
                if (voucher._Fileextension == FileextensionsTypes.JPEG || voucher._Fileextension == FileextensionsTypes.PNG)
                {
                    var pdfBytes = UtilDisplay.ConvertToPDF(voucher.Buffer, voucher._Fileextension);
                    if (pdfBytes != null)
                    {
                        voucher._Data = pdfBytes;
                        voucher._Fileextension = FileextensionsTypes.PDF;
                        return true;
                    }
                }

                UnicontaMessageBox.Show(Localization.lookup("InvalidFileFormat"), Localization.lookup("Error"), MessageBoxButton.OK);
                return false;
            }
            catch
            {
                return false;
            }
        }

        async private Task UpdateJoinedPDFContents(VouchersClient saveVoucher, VouchersClient copiedVoucher, byte[] mergedContents, bool isDeleteVoucher)
        {
            saveVoucher._Data = mergedContents;
            busyIndicator.IsBusy = true;

            if (saveVoucher.RowId > 0)
            {
                VoucherCache.SetGlobalVoucherCache(saveVoucher);
                api.UpdateNoResponse(saveVoucher);
            }
            else
                await api.Insert(saveVoucher);

            if (isDeleteVoucher && copiedVoucher != null)
            {
                VoucherCache.RemoveGlobalVoucherCache(copiedVoucher);
                await api.Delete(copiedVoucher);
            }

            await dgVoucherGrid.Filter(null);
            busyIndicator.IsBusy = false;
        }

        async private Task ValidateVoucher(VouchersClient selectedItem)
        {
            if (selectedItem != null && selectedItem._Data == null)
                await UtilDisplay.GetData(selectedItem, api);
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
                    UnicontaMessageBox.Show(Localization.lookup("InvalidFileFormat"), Localization.lookup("Warning"), MessageBoxButton.OK);
                    return;
                }

                if (selectedItem._Data == null)
                {
                    busyIndicator.IsBusy = true;
                    await UtilDisplay.GetData(selectedItem, api);
                }

                if (selectedItem.Buffer == null)
                {
                    UnicontaMessageBox.Show(Localization.lookup("EmptyTable"), Localization.lookup("Error"), MessageBoxButton.OK);
                    return;
                }

                var cwSplitWindow = new CWSplitPDFDocument(selectedItem.Buffer, selectedItem.RowId);
                cwSplitWindow.Closed += delegate
                {
                    if (cwSplitWindow.DialogResult == true)
                    {
                        var messageBox = new CWConfirmationBox(Localization.lookup("DeleteConfirmation"), Localization.lookup("Delete"), true, string.Format("{0}:",
                            Localization.lookup("Warning")));
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
            catch (Exception ex) { busyIndicator.IsBusy = false; UnicontaMessageBox.Show(ex.Message, Localization.lookup("Exception"), MessageBoxButton.OK); }
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
                    int iVoucher = 0, size = 0;
                    foreach (var fileInfo in selectedFileInfo)
                    {
                        if (fileInfo != null)
                        {
                            var voucher = new VouchersClient();
                            StreamingManager.Copy(selectedItem, voucher);
                            voucher._Fileextension = DocumentConvert.GetDocumentType(fileInfo.FileExtension);
                            voucher._Data = fileInfo.FileBytes;
                            voucher._Text = selectedItem._Text ?? fileInfo.FileName;
                            voucher._Uid = selectedItem._Uid;
                            voucher._Created = selectedItem._Created;
                            voucher._Content = selectedItem._Content;
                            voucher._ScanDoc = true;
                            // voucher._NoCompress = true;
                            voucherClients[iVoucher++] = voucher;
                            size += voucher._Data.Length;
                        }
                    }

                    busyIndicator.IsBusy = true;
                    ErrorCodes result;
                    if (size < TableAddOnData.MaxDocSize - 10000000)
                        result = await api.Insert(voucherClients);
                    else
                        result = ErrorCodes.NoSucces;
                    if (result != ErrorCodes.Succes)
                    {
                        for (int i = 0; i < voucherClients.Length; i++)
                        {
                            result = await api.Insert(voucherClients[i]);
                            if (result != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(result);
                        }
                    }
                    if (result == ErrorCodes.Succes)
                    {
                        if (deleteOriginal)
                        {
                            VoucherCache.RemoveGlobalVoucherCache(selectedItem);
                            await api.Delete(selectedItem);
                        }
                        dgVoucherGrid.Filter(null);
                    }
                }
            }
            catch (Exception ex) { api.ReportException(ex, "Split PDF Failed"); }
            finally { busyIndicator.IsBusy = false; }
        }

        void CreateContextMenu()
        {
            var menu = dgVoucherGrid.tableView.RowCellMenuCustomizations;
            var moveToMenu = new BarSubItem() { Content = Localization.lookup("MoveTo"), Glyph = GlyphHelper.GetIcon("Open_Transaction_16x16") };
            foreach (var bin in Folders)
            {
                var binMenu = new BarButtonItem() { Content = bin };
                binMenu.ItemClick += BinMenu_ItemClick;
                moveToMenu.Items.Add(binMenu);
            }
            menu.Add(moveToMenu);
        }

        private void BinMenu_ItemClick(object sender, ItemClickEventArgs e)
        {
            string bin = Convert.ToString(e.Item.Content);
            if (!string.IsNullOrEmpty(bin))
                SetRowsWithFolderValue(bin);
        }

        //async void CreateAttachment(bool deletePhysicalVoucher)
        //{
        //    var selectedItem = dgVoucherGrid.SelectedItem as VouchersClient;
        //    if (selectedItem == null)
        //        return;
        //    if (string.IsNullOrEmpty(selectedItem.CreditorAccount))
        //    {
        //        UnicontaMessageBox.Show(string.Format(Localization.lookup("CannotBeBlank"), Localization.lookup("CreditorsAccount")));
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
        private void LaunchEnvelopeWizard()
        {
            if (dgVoucherGrid.ItemsSource == null)
                return;

            var EnvelopeWizard = new WizardWindow(new UnicontaClient.Pages.VoucherGridFolderWizard(api), string.Format(Localization.lookup("CreateOBJ"),
                string.Format("{0} {1}", Localization.lookup("Envelope"), Localization.lookup("Wizard"))));

            EnvelopeWizard.Closed += async delegate
            {
                if (EnvelopeWizard.DialogResult == true)
                {
                    var result = await SaveFolderData(EnvelopeWizard.WizardData);
                    if (result == ErrorCodes.Succes)
                        globalEvents.OnRefresh(TabControls.VoucherFolderPage);
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            EnvelopeWizard.Show();
        }

        private Task<ErrorCodes> SaveFolderData(object wizarddata)
        {
            var data = wizarddata as object[];
            if (data != null)
            {
                var voucherClient = new VouchersClient();
                var voucherList = data[0] as IEnumerable<VouchersClient>;
                voucherClient._Text = data[1] as string;
                voucherClient.Content = data[2] as string;
                var documentApi = new DocumentAPI(api);
                return documentApi.CreateEnvelope(voucherClient, voucherList);
            }
            return BaseAPI.RetErr(ErrorCodes.CouldNotSave);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.VouchersPage2 || screenName == TabControls.GLOffsetAccountTemplate)
            {
                dgVoucherGrid.UpdateItemSource(argument);
                Save(false);
            }
            else if (screenName == TabControls.VoucherFolderPage)
            {
                dgVoucherGrid.Filter(null);
                if (selectedPageIndex == 1)
                    dgVoucherGrid.FilterString = "Contains([Envelope],'true')";
            }
            else if (screenName == TabControls.GL_DailyJournalLine)
                gridRibbon_BaseActions("RefreshGrid");
        }

        protected override void OnLayoutLoaded()
        {
            Utility.SetDimensionsGrid(api, clDim1, clDim2, clDim3, clDim4, clDim5);

            if (!api.CompanyEntity.Project)
                Project.ShowInColumnChooser = Project.Visible = ProjectName.ShowInColumnChooser = ProjectName.Visible = PrCategory.ShowInColumnChooser = PrCategory.Visible =
                    WorkSpace.ShowInColumnChooser = WorkSpace.Visible = false;

            base.OnLayoutLoaded();
        }

        private void Offeset_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallOffsetAccount((sender as Image).Tag as VouchersClient);
        }

        private void OpenFolderWindow(object sender, ItemClickEventArgs e)
        {
            MenuBarButtonItem btnmenu = (MenuBarButtonItem)sender;
            string tag = (string)btnmenu.Tag;
            CWAddEditFolder folderdialog = null;
            var selectedItem = accordionView.SelectedItem as AccordionFilterItem;
            var folderName = selectedItem?.FilterName;
            bool standardFolder = AppEnums.ViewBin.Values.Contains(folderName);

            switch (tag)
            {
                case "Add":
                    folderdialog = new CWAddEditFolder(api, null, 0, true);
                    break;
                case "Edit":
                    if (standardFolder || string.IsNullOrEmpty(folderName))
                        return;
                    folderdialog = new CWAddEditFolder(api, folderName, 1, true);
                    break;
                case "Delete":
                    if (standardFolder || string.IsNullOrEmpty(folderName) || dgVoucherGrid.VisibleRowCount > 0)
                        return;
                    folderdialog = new CWAddEditFolder(api, folderName, 2);
                    break;
            }
            folderdialog.Closing += delegate
            {
                folderName = folderdialog.FolderName;
                var action = folderdialog.Action;
                //   FolderCache = api.GetCache(typeof(DocumentFolder));
                if (action == 2)
                    BindFolders(selectedItem?.FilterName);
                else
                    BindFolders();
                dgVoucherGrid.RefreshData();

                var items = (accordionView.Items[0] as UnicontaClient.Controls.AccordionFilterGroup).FilterItems;
                foreach (var item in items)
                {
                    if (item.FilterName == folderName)
                    {
                        accordionView.SelectedItem = item;
                        break;
                    }
                }

                if (action == 1)
                    dgVoucherGrid.FilterString = string.Format("[ViewInFolder] = '{0}'", folderName);
            };
            folderdialog.Show();
        }

        private void CallOffsetAccount(VouchersClient vouchersClientLine)
        {
            if (vouchersClientLine != null)
            {
                dgVoucherGrid.SetLoadedRow(vouchersClientLine);
                var header = string.Format("{0}:{1} {2}", Localization.lookup("OffsetAccountTemplate"), Localization.lookup("Voucher"), vouchersClientLine.RowId);
                AddDockItem(TabControls.GLOffsetAccountTemplate, vouchersClientLine, header: header);
            }
        }

        void SetFooterDetailText()
        {
            txtCreditorName.Text = VouchersClientText.CreditorName;
            txtCostAccount.Text = VouchersClientText.CostAccountName;
            txtPaymentAccount.Text = VouchersClientText.PayAccountName;
            txtApprover1.Text = VouchersClientText.Approver1Name;
            txtApprover2.Text = VouchersClientText.Approver2Name;
        }

        bool IsSending;
        private async void DocumentScanUpload()
        {
            if (IsSending)
                UnicontaMessageBox.Show(Localization.lookup("UpdateInBackground"), Localization.lookup("Information"), MessageBoxButton.OK);

            var lst = ((IEnumerable<VouchersClient>)dgVoucherGrid.GetVisibleRows())
                .Where(s => s._SentToScanner == false && s._Fileextension <= FileextensionsTypes.PDF)
                .ToList();

            if (lst.Count == 0)
            {
                UnicontaMessageBox.Show(Localization.lookup("NoRecordExport"), Localization.lookup("Message"));
                return;
            }

            if (UnicontaMessageBox.Show(string.Concat(Localization.lookup("Upload"), " (", NumberConvert.ToString(lst.Count), " ", Localization.lookup("Vouchers"), ") ?"), Localization.lookup("Information"), MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            try
            {
                IsSending = true;
                Save();

                var rowIds = lst.Select(v => v.RowId).Where(r => r != 0);

                var documentApi = new DocumentAPI(api);

                var err = await documentApi.UploadDocumentsForScan(rowIds);
                if (err == ErrorCodes.NoPaperflowOrgNumber)
                {
                    await GetOrganisationNumber();
                    err = await documentApi.UploadDocumentsForScan(rowIds);
                }
                if (err != 0)
                    UnicontaMessageBox.Show(Localization.lookup(err.ToString()), Localization.lookup("DocumentScannerPay"));
            }
            finally
            {
                IsSending = false;
            }
        }

        private void PrimaryKeyId_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ribbonControl.PerformRibbonAction("ViewDownloadRow");
        }

        private void CreatePaperFlowUser()
        {
            var cwwin = new CWCreateBilagScanUser(api, string.Format(Localization.lookup("CreateOBJ"), Localization.lookup("User").ToLower()));
            cwwin.Closing += async delegate
            {
                if (cwwin.DialogResult == true)
                {
                    var orgNo = await GetOrganisationNumber();
                    var response = await Bilagscan.UserService.InviteToOrganization(cwwin.txtEmail.Text, orgNo);
                    var error = response?.error;
                    if (error != null)
                    {
                        var message = error.message;
                        if (!message.Equals("The e-mail address has already been invited"))
                            UnicontaMessageBox.Show(string.Format("{0}: {1}", Localization.lookup("Bilagscan"), message),
                                Localization.lookup("Warning"));
                    }
                }
            };

            cwwin.Show();

            if (cwwin.DialogResult == true)
                Process.Start("https://my.paperflow.com/");
        }

        private async Task<string> GetOrganisationNumber()
        {
            // Verify organization
            CompanySettingsClient companySettings = new CompanySettingsClient();
            await api.Read(companySettings);
            var orgNo = NumberConvert.ToStringNull(companySettings._OrgNumber);
            if (orgNo == null)
            {
                var noRightsToUpdate = AccessLevel.Get(api.CompanyEntity.Rights, CompanyTasks.AsOwner) < CompanyPermissions.Modify;
                if (noRightsToUpdate)
                {
                    UnicontaMessageBox.Show(string.Format("{0} {1}", Localization.lookup("NoRightsToInsert"), Localization.lookup("Bilagscan")), Localization.lookup("Warning"));
                    return null;
                }

                if (string.IsNullOrEmpty(api.CompanyEntity._Id))
                {
                    UnicontaMessageBox.Show(string.Format(Localization.lookup("CannotBeBlank"), Localization.lookup("CompanyRegNo")), Localization.lookup("Paperflow"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return null;
                }

                if (UnicontaMessageBox.Show(Localization.lookup("BilagscanTerms"), Localization.lookup("Paperflow"), MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                    return null;

                orgNo = await OrganisationService.CreateOrganization(api, companySettings);
                if (orgNo == null)
                {
                    UnicontaMessageBox.Show(string.Format(Localization.lookup("CannotBeBlank"), Localization.lookup("OrgNumber")), Localization.lookup("Paperflow"), MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return null;
                }
            }
            switch (await OrganisationService.VerifyOrganization(orgNo))
            {
                case "NOCOMPANY":
                    UnicontaMessageBox.Show("Plug-in'en kan ikke finde et firma.", "Overfør til Paperflow");
                    break;
                case "NOTABLE":
                    //Tools.CreateUserTable(api);
                    //await Account.CreateOrganization(api, Globals.access_token);
                    break;
                case "NOORGANIZATION":
                    UnicontaMessageBox.Show("Plug-in'en kan ikke finde din organisation hos Paperflow.", "Overfør til Paperflow");
                    break;
            }

            return orgNo;
        }

        private void Save(bool saveGridData = true)
        {
            if (saveGridData)
                dgVoucherGrid.SaveData();
            var selectedFld = accordionView.SelectedItem;
            BindFolders();
            accordionView.SelectedItem = selectedFld;
        }
    }

    public class VoucherThumbnailTemplateSelector : DataTemplateSelector
    {
        public DataTemplate EmptyTemplate { get; set; }
        public DataTemplate PDFTemplate { get; set; }
        public DataTemplate ImageTemplate { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            GridCellData data = (GridCellData)item;
            var row = data.RowData.Row;
            if (row != null)
            {
                var voucherPhoto = row.GetType().GetProperty("Image")?.GetValue(row);
                if (voucherPhoto != null && voucherPhoto is VoucherPhoto vp)
                {
                    if (vp?.Ext == FileextensionsTypes.PDF)
                        return PDFTemplate;
                    else
                        return ImageTemplate;
                }
            }
            return EmptyTemplate;
        }
    }
}
