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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls;
using UnicontaClient.Controls.Dialogs.DutchXafExport.Model;
using UnicontaClient.Models;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class BankImportMappingGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get
            {
                return typeof(BankImportMapClient);
            }
        }
        public override bool Readonly { get { return false; } }
        protected override bool RenderAllColumns { get { return true; } }
    }

    public partial class BankImportMapping : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.BankImportMapping; } }

        SQLCache LedgerCache, DebtorCache, CreditorCache;
        public BankImportMapping(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public BankImportMapping(UnicontaBaseEntity master)
           : base(master)
        {
            Init(master);
        }
        public void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgBankImportMapping.UpdateMaster(master);
            localMenu.dataGrid = dgBankImportMapping;
            dgBankImportMapping.api = api;
            SetRibbonControl(localMenu, dgBankImportMapping);
            dgBankImportMapping.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgBankImportMapping.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            var Comp = api.CompanyEntity;
            LedgerCache = Comp.GetCache(typeof(GLAccount));
            DebtorCache = Comp.GetCache(typeof(Debtor));
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            if (dgBankImportMapping.masterRecord is VouchersClient)
                HideColumns(this.AccountType, this.AccountTemplate, this.OffsetAccountType, this.Charge, this.RequireVoucher);
            else if (dgBankImportMapping.masterRecord is BankStatementClient)
                HideColumns(this.Creditor, this.CreditorName, this.Approver1, this.Approver2);
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as BankImportMapClient;
            if (oldselectedItem != null)
            {
                oldselectedItem.PropertyChanged -= BankImportMapGridClient_PropertyChanged;
            }

            var selectedItem = e.NewItem as BankImportMapClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += BankImportMapGridClient_PropertyChanged;

                if (selectedItem.accntSource == null)
                    SetAccountSource(selectedItem);
                if (selectedItem.offsetAccntSource == null)
                    SetOffsetAccountSource(selectedItem);
            }
        }

        void BankImportMapGridClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as BankImportMapClient;
            switch (e.PropertyName)
            {
                case "AccountType":
                    SetAccountSource(rec);
                    break;
                case "OffsetAccountType":
                    SetOffsetAccountSource(rec);
                    break;
            }
        }

        private void SetAccountSource(BankImportMapClient rec)
        {
            SQLCache cache;
            switch (rec._AccountType)
            {
                case GLJournalAccountType.Finans:
                    cache = LedgerCache;
                    break;
                case GLJournalAccountType.Debtor:
                    cache = DebtorCache;
                    break;
                case GLJournalAccountType.Creditor:
                    cache = CreditorCache;
                    break;
                default: return;
            }
            rec.accntSource = cache?.GetNotNullArray;
            rec.NotifyPropertyChanged("AccountSource");
        }

        private void SetOffsetAccountSource(BankImportMapClient rec)
        {
            SQLCache cache;
            switch (rec._OffsetAccountType)
            {
                case GLJournalAccountType.Finans:
                    cache = LedgerCache;
                    break;
                case GLJournalAccountType.Debtor:
                    cache = DebtorCache;
                    break;
                case GLJournalAccountType.Creditor:
                    cache = CreditorCache;
                    break;
                default: return;
            }
            rec.offsetAccntSource = cache;
            rec.NotifyPropertyChanged("OffsetAccountSource");
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgBankImportMapping.AddRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "Delete":
                    dgBankImportMapping.DeleteRow();
                    break;
                case "CopyFromAnotherCompany":
                    CopyFromOtherCompany();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async void CopyFromOtherCompany()
        {
            var companyDialog = new CWDefaultCompany(session, null, true);
            companyDialog.HideUserRights = true;
            companyDialog.Closing += async delegate
            {
                if (companyDialog.DialogResult == true)
                {
                    Company selectedCompany = companyDialog.selectedCompany;
                    if (selectedCompany == null)
                        return;
                    busyIndicator.IsBusy = true;
                    var copyCompanyAPI = new CrudAPI(BasePage.session, selectedCompany);
                    var lstEntity = await copyCompanyAPI.Query<BankImportMapClient>(dgBankImportMapping.masterRecord);
                    if (lstEntity?.Length > 0)
                        dgBankImportMapping.PasteRows(lstEntity);
                    busyIndicator.IsBusy = false;
                }
            };
            companyDialog.Show();
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (LedgerCache == null)
                LedgerCache = await api.LoadCache(typeof(GLAccount)).ConfigureAwait(false);
            if (DebtorCache == null)
                DebtorCache = await api.LoadCache(typeof(Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            LoadType(typeof(GLChargeGroup));
        }

        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgBankImportMapping.SelectedItem as BankImportMapClient;
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
            var selectedItem = dgBankImportMapping.SelectedItem as BankImportMapClient;
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

    }
}
