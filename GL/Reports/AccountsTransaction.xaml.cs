using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using DevExpress.Xpf.Grid;
using System;
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
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using Uniconta.DataModel;
using System.Collections;
using UnicontaClient.Controls.Dialogs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Uniconta.Client.Pages;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AccountsTransactionGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransClient); } }
        public override string SetColumnTooltip(object row, ColumnBase col)
        {
            var tran = row as GLTransClient;
            if (tran != null && col != null)
            {
                switch (col.FieldName)
                {
                    case "DCAccount": return tran.DCName;
                }
            }
            return base.SetColumnTooltip(row, col);
        }
    }

    public partial class AccountsTransaction : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.AccountsTransaction; } }
        string AccountNumber;
        List<UnicontaBaseEntity> masterlist;
        DateTime filterDate;

        protected override Filter[] DefaultFilters()
        {
            if (masterlist == null || masterlist.First() is GLAccount)
            {
                Filter dateFilter = new Filter() { name = "Date" };
                if (filterDate != DateTime.MinValue)
                    dateFilter.value = String.Format("{0:d}..", filterDate);
                if (masterlist == null && !string.IsNullOrEmpty(this.AccountNumber))
                {
                    Filter AccountFilter = new Filter() { name = "Account", value = AccountNumber };
                    return new Filter[] { dateFilter, AccountFilter };
                }
                if (filterDate != DateTime.MinValue)
                    return new Filter[] { dateFilter };
            }
            return base.DefaultFilters();
        }
        bool IsmenuRemove;
        PostingAPI postingApiInv;
        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties dateSort = new SortingProperties("Date");
            var syncMaster = dgAccountsTransGrid.masterRecord;
            dateSort.Ascending = (syncMaster is GLDailyJournalPosted || syncMaster is GLDailyJournalLine);
            return new SortingProperties[] { dateSort, new SortingProperties("Voucher"), new SortingProperties("VoucherLine") };
        }

        public AccountsTransaction(BaseAPI API) : base(API, string.Empty)
        {
            IsmenuRemove = true;
            InitializePage();
        }

        bool RemoveMenu(UnicontaBaseEntity master)
        {
            return !(master is GLTrans) && !(master is DCTrans) && !(master is DCTransOpen) && !(master is DCInvoice);
        }

        public AccountsTransaction(object objAccount)
            : base(null)
        {
            AccountNumber = (string)objAccount;
            IsmenuRemove = true;
            InitializePage();
        }
        public AccountsTransaction(UnicontaBaseEntity master)
            : base(null)
        {
            if (RemoveMenu(master))
                IsmenuRemove = true;
            var mlist = new List<UnicontaBaseEntity>() { master };
            InitializePage(mlist);
        }
        public AccountsTransaction(UnicontaBaseEntity master1, UnicontaBaseEntity master2)
            : base(null)
        {
            if (RemoveMenu(master1) || RemoveMenu(master2))
                IsmenuRemove = true;
            var mlist = new List<UnicontaBaseEntity>() { master1, master2 };
            InitializePage(mlist);
        }
        public AccountsTransaction(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            if (RemoveMenu(syncEntity.Row))
                IsmenuRemove = true;
            var mlist = new List<UnicontaBaseEntity>() { syncEntity.Row };
            InitializePage(mlist);
            SetPageHeader();
        }

        public AccountsTransaction(CrudAPI api, PropValuePair[] filterArray) : base(api, string.Empty)
        {
            filter = filterArray;
            InitializePage();
        }

        void SetPageHeader()
        {
            var syncMaster = dgAccountsTransGrid.masterRecord;
            if (syncMaster == null)
                return;
            string propertytName;
            if (syncMaster is InvTransClient)
                propertytName = "AccountName";
            else if (syncMaster is GLAccountClient)
                propertytName = "Name";
            else
                propertytName = "Voucher";

            var pInfo = syncMaster.GetType().GetProperty(propertytName);
            if (pInfo != null)
            {
                var voucher = pInfo.GetValue(syncMaster, null);
                string header = string.Empty;
                if (syncMaster is GLAccountClient)
                    header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("AccountsTransaction"), voucher);
                else
                    header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), voucher);
                SetHeader(header);
            }
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgAccountsTransGrid.UpdateMaster(args);
            SetPageHeader();
            BindGrid();
        }
        private void InitializePage(List<UnicontaBaseEntity> masters = null)
        {
            this.DataContext = this;
            InitializeComponent();
            var Comp = this.api.CompanyEntity;
            filterDate = BasePage.GetFilterDate(Comp, masters != null && masters.Count > 0);
            localMenu.dataGrid = dgAccountsTransGrid;
            SetRibbonControl(localMenu, dgAccountsTransGrid);
            if (IsmenuRemove)
                RemoveMenuItem();
            else
            {
                var CountryId = Comp._CountryId;
                if (CountryId == CountryCode.Iceland)
                {
                    if (api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.Reseller)
                        RemoveDeleteVoucher();
                }
                else if ((CountryId == CountryCode.SouthAfrica || CountryId == CountryCode.UnitedKingdom || CountryId == CountryCode.Germany)
                     && api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.Distributor)
                {
                    RemoveDeleteVoucher();
                }
                if (Comp._UseQtyInLedger == false)
                    RemoveChangeQuantity();
            }
            masterlist = masters;
            gridControl.masterRecords = masters;
            dgAccountsTransGrid.api = api;
            if (!Comp.HasDecimals)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = false;
            dgAccountsTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgAccountsTransGrid.RowDoubleClick += DgAccountsTransGrid_RowDoubleClick;
            dgAccountsTransGrid.ShowTotalSummary();
            postingApiInv = new PostingAPI(api);
        }

        IEnumerable<PropValuePair> filter;
        public override Task InitQuery()
        {
            if (filter != null)
                return dgAccountsTransGrid.Filter(filter);
            else
                return Filter();
        }

        private void DgAccountsTransGrid_RowDoubleClick()
        {
            var selectedItem = dgAccountsTransGrid.SelectedItem as GLTransClient;
            if (selectedItem != null)
            {
                string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                AddDockItem(TabControls.AccountsTransaction, selectedItem, vheader);
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, new string[] { "CancelVoucher", "InvertSign", "ChangeDimension", "AddEditNote", "DeleteVoucher", "ChangeText", "ChangeQuantity", "ChangeDate" /*,"RefVoucher", "ImportVoucher", "RemoveVoucher", "PhyslVoucher" */ });
        }

        void RemoveDeleteVoucher()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "DeleteVoucher");
        }
        void RemoveChangeQuantity()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            UtilDisplay.RemoveMenuCommand(rb, "ChangeQuantity");
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor), typeof(Uniconta.DataModel.GLDailyJournal), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLTransType), typeof(Uniconta.DataModel.NumberSerie), typeof(Uniconta.DataModel.FAM) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAccountsTransGrid.SelectedItem as GLTransClient;
            switch (ActionType)
            {
                case "PostedTransaction":
                    if (selectedItem == null)
                        return;
                    string header = string.Format("{0} / {1}", Uniconta.ClientTools.Localization.lookup("PostedTransactions"), selectedItem._JournalPostedId);
                    AddDockItem(TabControls.PostedTransactions, selectedItem, header);
                    break;
                case "ViewDownloadRow":
                    if (selectedItem == null)
                        return;
                    DebtorTransactions.ShowVoucher(dgAccountsTransGrid.syncEntity, api, busyIndicator);
                    break;
                case "VoucherTransactions":
                    if (selectedItem == null)
                        return;
                    string vheader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                    AddDockItem(TabControls.AccountsTransaction, dgAccountsTransGrid.syncEntity, vheader);
                    break;
                case "AccountsTransaction":
                    if (selectedItem != null)
                    {
                        var glAccount = selectedItem.Master;
                        if (glAccount == null) return;
                        string accHeader = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("AccountsTransaction"), selectedItem._Account);
                        AddDockItem(TabControls.AccountsTransaction, glAccount, accHeader);
                    }
                    break;
                case "DragDrop":
                case "ImportVoucher":
                    if (selectedItem == null)
                        return;
                    AddVoucher(selectedItem, ActionType);
                    break;
                case "CancelVoucher":
                    if (selectedItem == null)
                        return;
                    CWCommentsDialogBox commentsDialog = new CWCommentsDialogBox(Uniconta.ClientTools.Localization.lookup("CancelVoucher"),
                        true, selectedItem.Date);
#if !SILVERLIGHT
                    commentsDialog.DialogTableId = 2000000035;
#endif
                    commentsDialog.Closing += async delegate
                    {
                        if (commentsDialog.DialogResult == true)
                        {
                            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                            busyIndicator.IsBusy = true;

                            var comments = commentsDialog.Comments;
                            var date = commentsDialog.Date;
                            var errorCodes = await postingApiInv.CancelVoucher(selectedItem, comments, date);
                            busyIndicator.IsBusy = false;

                            if (errorCodes != ErrorCodes.Succes)
                                UtilDisplay.ShowErrorCode(errorCodes);
                            else
                            {
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("TransCanceled"), Uniconta.ClientTools.Localization.lookup("Error"));
                                BindGrid();
                            }
                        }
                    };
                    commentsDialog.Show();
                    break;
                case "DeleteVoucher":
                    if (selectedItem == null)
                        return;
                    var deleteDialog = new DeletePostedJournal();
                    deleteDialog.Closed += async delegate
                    {
                        if (deleteDialog.DialogResult == true)
                        {
                            PostingAPI pApi = new PostingAPI(api);
                            ErrorCodes res = await pApi.DeletePostedVoucher(selectedItem);
                            UtilDisplay.ShowErrorCode(res);
                            if (res == ErrorCodes.Succes)
                                dgAccountsTransGrid.ItemsSource = new GLTransClient[0];
                        }
                    };
                    deleteDialog.Show();
                    break;
                case "InvertSign":
                    if (selectedItem != null)
                        InvertSign(selectedItem);
                    break;
                case "RefVoucher":
                    if (selectedItem == null)
                        return;

                    var _refferedVouchers = new List<int>();
                    var source = (IList)dgAccountsTransGrid.ItemsSource;
                    if (source != null)
                    {
                        foreach (var statementLine in (IEnumerable<GLTrans>)source)
                            if (statementLine._DocumentRef != 0)
                                _refferedVouchers.Add(statementLine._DocumentRef);
                    }
                    CWAttachVouchers attachVouchersDialog = new CWAttachVouchers(api, _refferedVouchers);
                    attachVouchersDialog.Closing += delegate
                    {
                        if (attachVouchersDialog.DialogResult == true)
                        {
                            if (attachVouchersDialog.VoucherReference != 0 && selectedItem != null)
                                SaveAttachment(selectedItem, attachVouchersDialog.Voucher);
                        }
                    };
                    attachVouchersDialog.Show();
                    break;
                case "RemoveVoucher":
                    if (selectedItem == null || selectedItem._DocumentRef == 0)
                        return;
                    if (UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AskRemoveDocument"), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        postingApiInv.AddPhysicalVoucher(selectedItem, null, true);
                        selectedItem._DocumentRef = 0;
                    }
                    break;
                case "ChangeDimension":
                    if (selectedItem != null)
                    {
                        CWChangeDimension ChangeDimensionDialog = new CWChangeDimension(api, isChangeText: false);
                        ChangeDimensionDialog.Closing += delegate
                        {
                            if (ChangeDimensionDialog.DialogResult == true)
                                SetNewDim(selectedItem, ChangeDimensionDialog);
                        };
                        ChangeDimensionDialog.Show();
                    }
                    break;
                case "ChangeText":
                    if (selectedItem != null)
                    {
                        CWChangeDimension ChangeTextDialog = new CWChangeDimension(api, isChangeDimension: false);
                        ChangeTextDialog.Closing += delegate
                        {
                            if (ChangeTextDialog.DialogResult == true)
                                SetChangeText(selectedItem, ChangeTextDialog);
                        };
                        ChangeTextDialog.Show();
                    }
                    break;
                case "ChangeQuantity":
                    if (selectedItem != null)
                    {
                        CWChangeDimension ChangeQtyDialog = new CWChangeDimension(api, isChangeDimension: false, isChangeText: false);
                        ChangeQtyDialog.Closing += delegate
                        {
                            if (ChangeQtyDialog.DialogResult == true)
                                SetChangeQuantity(selectedItem, ChangeQtyDialog);
                        };
                        ChangeQtyDialog.Show();
                    }
                    break;
                case "AddEditNote":
                    if (selectedItem != null)
                    {
                        CWAddEditNote cwAddEditNote = new CWAddEditNote(api, null, selectedItem);
                        cwAddEditNote.Closed += delegate
                        {
                            if (cwAddEditNote.DialogResult == true)
                            {
                                if (cwAddEditNote.result == ErrorCodes.Succes)
                                {
                                    BindGrid();
                                }
                            }
                        };
                        cwAddEditNote.Show();
                    }
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "ChangeDate":
                    if (selectedItem == null) return;
                    var dateSelector = new CWDateSelector(selectedItem.Date, true);
#if !SILVERLIGHT
                    dateSelector.DialogTableId = 2000000058;
#endif
                    dateSelector.Closed += delegate
                     {
                         if (dateSelector.DialogResult == true)
                             SetChangeDate(selectedItem, dateSelector.SelectedDate);
                     };
                    dateSelector.Show();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(GLTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        private void AddVoucher(GLTransClient selectedItem, string actionType)
        {
            CWAddVouchers addVouchersDialog = null;
#if !SILVERLIGHT
            if (actionType == "DragDrop")
            {
                var dragDropWindow = new UnicontaDragDropWindow(false);
                dragDropWindow.Closed += delegate
                {
                    if (dragDropWindow.DialogResult == true)
                    {
                        var vouchersClient = new VouchersClient();
                        var fileInfo = dragDropWindow.FileInfoList?.SingleOrDefault();
                        if (fileInfo != null)
                        {
                            vouchersClient._Data = fileInfo.FileBytes;
                            vouchersClient._Text = fileInfo.FileName;
                            vouchersClient._Fileextension = DocumentConvert.GetDocumentType(fileInfo.FileExtension);
                        }
                        addVouchersDialog = new CWAddVouchers(api, vouchersClient, true);
                    }
                };
                dragDropWindow.Show();
            }
            else
#endif
                addVouchersDialog = new CWAddVouchers(api, false, null);

            if (addVouchersDialog == null) return;

            addVouchersDialog.Closed += delegate
            {
                if (addVouchersDialog.DialogResult == true)
                {
                    if (addVouchersDialog.VoucherRowIds.Length > 0 && addVouchersDialog.vouchersClient != null)
                        SaveAttachment(selectedItem, addVouchersDialog.vouchersClient);
                }
            };
            addVouchersDialog.Show();
        }

        async void SetChangeText(GLTransClient selectedItem, CWChangeDimension ChangeTextDialog)
        {
            busyIndicator.IsBusy = true;
            var errorCodes = await postingApiInv.UpdateTransText(selectedItem, ChangeTextDialog.Text, ChangeTextDialog.AllLine);
            busyIndicator.IsBusy = false;
            UtilDisplay.ShowErrorCode(errorCodes);
            if (errorCodes == ErrorCodes.Succes)
                BindGrid();
        }

        async void SetChangeDate(GLTransClient selectedItem, DateTime NewDate)
        {
            var date = selectedItem._Date;
            var jour = selectedItem._JournalPostedId;
            var voucher = selectedItem._Voucher;

            string dc = null;
            GLTransRefType DCType = 0;
            var lst = (IEnumerable<GLTrans>)dgAccountsTransGrid.ItemsSource;
            foreach (var rec in lst)
            {
                if (rec._DCType > 0 && rec._DCAccount != null && rec._JournalPostedId == jour && rec._Voucher == voucher && rec._Date == date)
                {
                    if (dc == null)
                    {
                        dc = rec._DCAccount;
                        DCType = rec._DCType;
                    }
                    /*
                    else if (rec._DCType != DCType && rec._DCAccount != dc)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("MoreThanOneDC"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }
                    */
                }
            }

            busyIndicator.IsBusy = true;
            selectedItem._DCType = DCType;
            selectedItem._DCAccount = dc;
            var errorCodes = await postingApiInv.UpdateTransDate(selectedItem, NewDate);
            busyIndicator.IsBusy = false;
            UtilDisplay.ShowErrorCode(errorCodes);
            if (errorCodes == ErrorCodes.Succes)
            {
                foreach (var rec in lst)
                {
                    if (rec._DCType > 0 && rec._DCAccount != null && rec._JournalPostedId == jour && rec._Voucher == voucher && rec._Date == date)
                        rec._Date = date;
                }
                foreach (var rec in masterlist)
                {
                    var post = rec as GLTrans;
                    if (post != null)
                        post._Date = NewDate;
                    else
                    {
                        var post2 = rec as DCTrans;
                        if (post2 != null)
                            post2._Date = NewDate;
                        else
                        {
                            var post3 = rec as DCTransOpen;
                            if (post3 != null)
                                post3.Trans._Date = NewDate;
                        }
                    }
                }
                dgAccountsTransGrid.RefreshData();
            }
        }

        async void SetChangeQuantity(GLTransClient selectedItem, CWChangeDimension ChangeTextDialog)
        {
            busyIndicator.IsBusy = true;
            var errorCodes = await postingApiInv.UpdateTransQty(selectedItem, ChangeTextDialog.Quantity, ChangeTextDialog.AllLine);
            busyIndicator.IsBusy = false;
            UtilDisplay.ShowErrorCode(errorCodes);
            if (errorCodes == ErrorCodes.Succes && ribbonControl != null)
                BindGrid();
        }

        async void SetNewDim(GLTransClient selectedItem, CWChangeDimension ChangeDimensionDialog)
        {
            var postingApiInv = this.postingApiInv;
            var nDim = postingApiInv.CompanyEntity.NumberOfDimensions;
            var accs = postingApiInv.CompanyEntity.GetCache(typeof(Uniconta.DataModel.GLAccount));
            var acc = (Uniconta.DataModel.GLAccount)accs.Get(selectedItem._Account);
            if ((nDim >= 1 && ChangeDimensionDialog.Dimension1.HasValue && !acc.GetDimUsed(1)) ||
                (nDim >= 2 && ChangeDimensionDialog.Dimension2.HasValue && !acc.GetDimUsed(2)) ||
                (nDim >= 3 && ChangeDimensionDialog.Dimension3.HasValue && !acc.GetDimUsed(3)) ||
                (nDim >= 4 && ChangeDimensionDialog.Dimension4.HasValue && !acc.GetDimUsed(4)) ||
                (nDim >= 5 && ChangeDimensionDialog.Dimension5.HasValue && !acc.GetDimUsed(5)))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoDimActivedForAccount"), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
            }

            busyIndicator.IsBusy = true;
            var errorCodes = await postingApiInv.SetNewDimensions(selectedItem, ChangeDimensionDialog.AllLine, ChangeDimensionDialog.Dimension1,
                ChangeDimensionDialog.Dimension2, ChangeDimensionDialog.Dimension3, ChangeDimensionDialog.Dimension4, ChangeDimensionDialog.Dimension5, ChangeDimensionDialog.GLAccount);
            busyIndicator.IsBusy = false;
            UtilDisplay.ShowErrorCode(errorCodes);
            if (errorCodes == ErrorCodes.Succes)
                BindGrid();
        }

        void SaveAttachment(GLTransClient selectedItem, VouchersClient doc)
        {
            CWForAllTrans cwconfirm = new CWForAllTrans();
            cwconfirm.Closing += async delegate
            {
                busyIndicator.IsBusy = true;
                var errorCodes = await postingApiInv.AddPhysicalVoucher(selectedItem, doc, cwconfirm.ForAllTransactions, cwconfirm.AppendDoc);
                busyIndicator.IsBusy = false;
                if (errorCodes == ErrorCodes.Succes)
                    BindGrid();
                else
                    UtilDisplay.ShowErrorCode(errorCodes);
            };
            cwconfirm.Show();
        }

        void InvertSign(GLTransClient selectedItem)
        {
            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false, Uniconta.ClientTools.Localization.lookup("InvertSign"));
            dialog.Closing += async delegate
            {
                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                {
                    busyIndicator.IsBusy = true;
                    var errorCodes = await postingApiInv.InvertSignOnVoucher(selectedItem);
                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(errorCodes);
                    if (errorCodes == ErrorCodes.Succes)
                        BindGrid();
                }
            };
            dialog.Show();
        }

        private void BindGrid()
        {
            var pairs = ribbonControl.filterValues;
            var sort = ribbonControl.PropSort;
            if (pairs != null || sort != null)
                ribbonControl.FilterGrid?.Filter(pairs, sort);
            else
                Filter();
        }
        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return AccountsTransaction.HandleLookupOnLocalPage(dgAccountsTransGrid, lookup);
        }

        static public LookUpTable HandleLookupOnLocalPage(CorasauDataGrid grid, LookUpTable lookup)
        {
            var trans = grid.SelectedItem as Uniconta.DataModel.GLTrans;
            if (trans == null)
                return lookup;
            if (grid.CurrentColumn?.Name == "DCAccount")
            {
                switch ((int)trans._DCType)
                {
                    case 1:
                        lookup.TableType = typeof(Uniconta.DataModel.Debtor);
                        break;
                    case 2:
                        lookup.TableType = typeof(Uniconta.DataModel.Creditor);
                        break;
                    case 3:
                        lookup.TableType = typeof(Uniconta.DataModel.FAM);
                        break;
                }
            }
            return lookup;
        }

        private void HasVoucherImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            busyIndicator.IsBusy = true;
            ViewVoucher(TabControls.VouchersPage3, dgAccountsTransGrid.syncEntity);
            busyIndicator.IsBusy = false;
        }

        private void PART_Editor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var glTrans = dgAccountsTransGrid.syncEntity.LoadedRow as GLTransClient;
            if (glTrans._HasNote)
            {
                CWAddEditNote cwAddEditNote = new CWAddEditNote(api, null, glTrans, true);
                cwAddEditNote.Show();
            }
        }
    }
}
