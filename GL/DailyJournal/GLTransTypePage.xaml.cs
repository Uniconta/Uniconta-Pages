using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using System.Windows;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLTransTypeGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransTypeGridClient); } }
        public override bool Readonly { get { return false; } }
        protected override bool RenderAllColumns { get { return true; } }
    }
    public partial class GLTransTypePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLTransTypePage; } }

        SQLCache LedgerCache, DebtorCache, CreditorCache;

        public GLTransTypePage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgGLTrans);
            dgGLTrans.api = api;
            dgGLTrans.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgGLTrans.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;

            var Comp = api.CompanyEntity;
            LedgerCache = Comp.GetCache(typeof(GLAccount));
            DebtorCache = Comp.GetCache(typeof(Debtor));
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            GLTransTypeGridClient oldselectedItem = e.OldItem as GLTransTypeGridClient;
            if (oldselectedItem != null)
            {
                oldselectedItem.PropertyChanged -= GLTransTypeGridClient_PropertyChanged;
            }

            GLTransTypeGridClient selectedItem = e.NewItem as GLTransTypeGridClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged += GLTransTypeGridClient_PropertyChanged;

                if (selectedItem.accntSource == null)
                    SetAccountSource(selectedItem);
                if (selectedItem.offsetAccntSource == null)
                    SetOffsetAccountSource(selectedItem);
            }
        }

        void GLTransTypeGridClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as GLTransTypeGridClient;
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

        private void SetAccountSource(GLTransTypeGridClient rec)
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
            if (cache != null)
            {
                int ver = cache.version + 10000 * ((int)rec._AccountType + 1);
                if (ver != rec.AccountVersion || rec.accntSource == null)
                {
                    rec.AccountVersion = ver;
                    rec.accntSource = cache.GetNotNullArray;
                    rec.NotifyPropertyChanged("AccountSource");
                }
            }
        }

        private void SetOffsetAccountSource(GLTransTypeGridClient rec)
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
            if (cache != null)
            {
                int ver = cache.version + 10000 * ((int)rec._OffsetAccountType + 1);
                if (ver != rec.OffsetAccountVersion || rec.offsetAccntSource == null)
                {
                    rec.OffsetAccountVersion = ver;
                    rec.offsetAccntSource = cache;
                    rec.NotifyPropertyChanged("OffsetAccountSource");
                }
            }
        }
        
        private void localMenu_OnItemClicked(string ActionType)
        {
            GLTransTypeGridClient selectedItem = dgGLTrans.SelectedItem as GLTransTypeGridClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgGLTrans.AddRow();
                    break;
                case "SaveGrid":
                    SaveData();
                    break;
                case "DeleteRow":
                    dgGLTrans.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void SaveData()
        {
            var err = await dgGLTrans.SaveData();
            if (err == ErrorCodes.Succes)
                CloseDockItem();
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
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
            GLTransTypeGridClient selectedItem = dgGLTrans.SelectedItem as GLTransTypeGridClient;
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
            GLTransTypeGridClient selectedItem = dgGLTrans.SelectedItem as GLTransTypeGridClient;
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

    public class GLTransTypeGridClient : GLTransTypeClient
    {
        internal int AccountVersion;
        internal object accntSource;
        public object AccountSource { get { return accntSource; } }

        internal int OffsetAccountVersion;
        internal object offsetAccntSource;
        public object OffsetAccountSource { get { return offsetAccntSource; } }
    }
}
