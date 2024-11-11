using dk.gov.oiosi.security.oces;
using System;
using System.Collections;
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
using System.Windows.Shapes;
using Uniconta.API.GeneralLedger;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DocumentsApprovalPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VouchersClient); } }
        public override IComparer GridSorting { get { return new SortDocAttached(); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public IList ToListLocal(VouchersClient[] Arr) { return this.ToList(Arr); }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool CanInsert { get { return false; } }
    }
    public partial class DocumentsApprovalPage : GridBasePage
    {
        Uniconta.DataModel.Employee employee;
        public DocumentsApprovalPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }
        public DocumentsApprovalPage(BaseAPI API, Uniconta.DataModel.Employee _employee)
           : base(API, string.Empty)
        {
            employee = _employee;
            InitPage();
        }

        void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgVoucherApproveGrid;
            dgVoucherApproveGrid.api = api;
            dgVoucherApproveGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgVoucherApproveGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgVoucherApproveGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;

            this.LedgerCache = api.GetCache(typeof(Uniconta.DataModel.GLAccount));
            this.CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor));

            docApi = new DocumentAPI(api);
            if (api.CompanyEntity != null && !api.CompanyEntity.Project)
            {
                Project.Visible = Project.ShowInColumnChooser = false;
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
            }
        }

        public override bool IsDataChaged { get { return false; } }

        DocumentAPI docApi;
        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            var voucherClientUserType = dgVoucherApproveGrid.TableTypeUser;
            var voucherClientUserObj = Activator.CreateInstance(voucherClientUserType) as VouchersClient;
            var documents = (VouchersClient[])await docApi.DocumentsForApproval(voucherClientUserObj, employee);
            dgVoucherApproveGrid.ItemsSource = dgVoucherApproveGrid.ToListLocal(documents);
            dgVoucherApproveGrid.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }

        SQLCache LedgerCache, CreditorCache, ProjectCache, TextTypes;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (this.LedgerCache == null)
                this.LedgerCache = api.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            if (this.CreditorCache == null)
                this.CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            LoadType(new Type[] { typeof(Uniconta.DataModel.Employee), typeof(Uniconta.DataModel.GLVat) });
        }

        protected override void OnLayoutLoaded()
        {
            if (!api.CompanyEntity.Project)
                Project.ShowInColumnChooser = Project.Visible = ProjectName.ShowInColumnChooser = ProjectName.Visible = PrCategory.ShowInColumnChooser = PrCategory.Visible =
                    WorkSpace.ShowInColumnChooser = WorkSpace.Visible = false;

            base.OnLayoutLoaded();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgVoucherApproveGrid.SelectedItem as VouchersClient;

            switch (ActionType)
            {
                case "Approve":
                    if (selectedItem != null)
                        ApproveDoc(selectedItem);
                    break;
                case "ApproveWithComments":
                case "Reject":
                case "Await":
                    if (selectedItem != null)
                        ApproveComment(selectedItem, ActionType);
                    break;
                case "ViewVoucher":
                    if (selectedItem != null)
                        ViewVoucher(TabControls.VouchersPage3, dgVoucherApproveGrid.syncEntity);
                    break;
                case "ModifyApprover":
                    if (selectedItem != null)
                        ModifyApprover(selectedItem);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "Archived":
                    ShowArchivedRecords();
                    break;
                case "ViewTransactions":
                    if (selectedItem?._JournalPostedId != 0)
                        AddDockItem(TabControls.AccountsTransaction, dgVoucherApproveGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem.RowId));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ShowArchivedRecords()
        {
            var employee = await api.Query<EmployeeClient>(api.session.User);
            if (employee != null && employee.Length > 0)
            {
                var header = string.Concat(Uniconta.ClientTools.Localization.lookup("PhysicalVouchers"), " : ", employee[0].Name);
                AddDockItem(TabControls.AttachedVouchers, new object[] { employee[0] }, header);
            }
        }

        void ModifyApprover(VouchersClient voucher)
        {
            var cwEmployee = new CWEmployee(api);
            cwEmployee.DialogTableId = 2000000076;
            cwEmployee.Closed += async delegate
            {
                if (cwEmployee.DialogResult == true)
                {
                    var result = await docApi.ChangeApprover(voucher, cwEmployee.Employee, cwEmployee.Comment);
                    if (result == ErrorCodes.Succes)
                        dgVoucherApproveGrid.RemoveFocusedRowFromGrid();
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            cwEmployee.Show();
        }

        async void ApproveDoc(VouchersClient selectedItem)
        {
            if (dgVoucherApproveGrid.HasUnsavedData)
                await dgVoucherApproveGrid.SaveData();
            var result = await docApi.DocumentSetApprove(selectedItem, null, employee);
            if (result == ErrorCodes.Succes)
                dgVoucherApproveGrid.RemoveFocusedRowFromGrid();
            else
                UtilDisplay.ShowErrorCode(result);
        }

        void ApproveComment(VouchersClient selectedItem, string ActionType)
        {
            if (dgVoucherApproveGrid.HasUnsavedData)
                dgVoucherApproveGrid.SaveData();

            CWCommentsDialogBox commentsDialog = new CWCommentsDialogBox(Uniconta.ClientTools.Localization.lookup("Note"), false, DateTime.MinValue);
            commentsDialog.DialogTableId = 2000000068;
            commentsDialog.Closing += async delegate
            {
                if (commentsDialog.DialogResult == true)
                {
                    ErrorCodes result;
                    if (ActionType == "ApproveWithComments")
                        result = await docApi.DocumentSetApprove(selectedItem, commentsDialog.Comments, employee);
                    else if (ActionType == "Reject")
                        result = await docApi.DocumentSetReject(selectedItem, commentsDialog.Comments, employee);
                    else if (ActionType == "Await")
                        result = await docApi.DocumentSetWait(selectedItem, commentsDialog.Comments, employee);
                    else
                        result = ErrorCodes.NoSucces;
                    if (result == ErrorCodes.Succes)
                        dgVoucherApproveGrid.RemoveFocusedRowFromGrid();
                    else
                        UtilDisplay.ShowErrorCode(result);
                }
            };
            commentsDialog.Show();
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

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as VouchersClient;
            switch (e.PropertyName)
            {
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
                    if (dc._Currency != 0)
                        rec.Currency = AppEnums.Currencies.ToString((int)dc._Currency);
                    if (dc._PostingAccount != null)
                        rec.CostAccount = dc._PostingAccount;
                    else if (dc._Vat != null)
                    {
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
                case "PurchaseNumber":
                    if (rec._PurchaseNumber != 0 && api.CompanyEntity.Purchase)
                        rec.SetPurchaseNumber(api);
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

        private void PrimaryKeyId_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            ribbonControl.PerformRibbonAction("ViewVoucher");
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            if (dgVoucherApproveGrid.CurrentColumn?.FieldName == "CreditorOrder.OrderNumber")
            {
                lookup.TableType = typeof(Uniconta.DataModel.CreditorOrder);
            }
            return lookup;
        }

        private void Offeset_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CallOffsetAccount((sender as Image).Tag as VouchersClient);
        }
        private void CallOffsetAccount(VouchersClient vouchersClientLine)
        {
            if (vouchersClientLine != null)
            {
                dgVoucherApproveGrid.SetLoadedRow(vouchersClientLine);
                var header = string.Format("{0}:{1} {2}", Uniconta.ClientTools.Localization.lookup("OffsetAccountTemplate"), Uniconta.ClientTools.Localization.lookup("Voucher"), vouchersClientLine.RowId);
                AddDockItem(TabControls.GLOffsetAccountTemplate, vouchersClientLine, header: header);
            }
        }
    }
}
