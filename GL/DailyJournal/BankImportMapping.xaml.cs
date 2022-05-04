using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class BankImportMappingGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get
            {
                return typeof(BankImportMapGridClient);
            }
        }
        public override bool Readonly { get { return false; } }
        protected override bool RenderAllColumns { get { return true; } }
    }

    public partial class BankImportMapping : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.BankImportMapping; } }

        SQLCache LedgerCache, DebtorCache, CreditorCache;

        public BankImportMapping(UnicontaBaseEntity master)
           : base(master)
        {
            InitializeComponent();
            var masterRecord = master as Uniconta.DataModel.BankImportFormat;
            if (masterRecord == null)
                throw new Exception("This page only supports master BankImportFormat");
            List<UnicontaBaseEntity> masterList = new List<UnicontaBaseEntity>();
            masterList.Add(master);
            localMenu.dataGrid = dgBankImportMapping;
            dgBankImportMapping.api = api;
            dgBankImportMapping.masterRecords = masterList;
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
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            BankImportMapGridClient oldselectedItem = e.OldItem as BankImportMapGridClient;
            if (oldselectedItem != null)
            {
                oldselectedItem.PropertyChanged -= BankImportMapGridClient_PropertyChanged;
            }

            BankImportMapGridClient selectedItem = e.NewItem as BankImportMapGridClient;
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
            var rec = sender as BankImportMapGridClient;
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

        private void SetAccountSource(BankImportMapGridClient rec)
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

        private void SetOffsetAccountSource(BankImportMapGridClient rec)
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (LedgerCache == null)
                LedgerCache = await Comp.LoadCache(typeof(GLAccount), api).ConfigureAwait(false);
            if (DebtorCache == null)
                DebtorCache = await Comp.LoadCache(typeof(Debtor), api).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api).ConfigureAwait(false);
        }

        CorasauGridLookupEditorClient prevAccount;
        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            BankImportMapGridClient selectedItem = dgBankImportMapping.SelectedItem as BankImportMapGridClient;
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
            BankImportMapGridClient selectedItem = dgBankImportMapping.SelectedItem as BankImportMapGridClient;
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


    public class BankImportMapGridClient : BankImportMapClient
    {
        internal object accntSource;
        public object AccountSource { get { return accntSource; } }

        internal object offsetAccntSource;
        public object OffsetAccountSource { get { return offsetAccntSource; } }
    }
}
