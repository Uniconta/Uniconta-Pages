using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ContactPage2 : FormBasePage
    {
        ContactClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            ((ContactClient)RefreshParams[1]).NotifyPropertyChanged("UserField");
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override Type TableType { get { return typeof(ContactClient); } }
        public override string NameOfControl { get { return TabControls.ContactPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (ContactClient)value; } }
        public ContactPage2(UnicontaBaseEntity sourcedata, bool isEdit, UnicontaBaseEntity master = null)
            : base(sourcedata, isEdit)
        {
            InitPage(api, master);
        }
        public ContactPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitPage(crudApi, null);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtName, txtName);
#endif
        }
        void InitPage(CrudAPI crudapi, UnicontaBaseEntity master)
        {
            InitializeComponent();
            StartLoadCache();
            var Comp = api.CompanyEntity;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                if (editrow == null)
                {
                    editrow = CreateNew() as ContactClient;
                    editrow.SetMaster(master != null ? master : api.CompanyEntity);
                }
                else
                {
                    var dct = editrow._DCType;
                    var dca = editrow._DCAccount;
                    SetTemplate();
                    editrow._DCType = dct;
                    editrow._DCAccount = dca;
                }
                frmRibbon.DisableButtons("Delete");
            }
            lookupDCAccount.api = crudapi;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            if (master == null)
            {
                itmDCType.Visibility = itemDCAccount.Visibility = Visibility.Visible;

            }
            if (Comp.CRM)
            {
                crmGroup.Visibility = Visibility.Visible;
                GetInterestAndProduct();
            }
        }
        async void GetInterestAndProduct()
        {
            var api = this.api;
            var cache = api.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmInterest));
            cmbInterests.ItemsSource = cache.GetKeyList();
            cache = api.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProduct));
            cmbProducts.ItemsSource = cache.GetKeyList();
        }

        SQLCache DebtorCache, CreditorCache, CrmProspectCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            CrmProspectCache = api.GetCache(typeof(Uniconta.DataModel.CrmProspect)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProspect)).ConfigureAwait(false);

            Dispatcher.BeginInvoke(new Action(() => SetAccountSource()));
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save" && !VaidateEAN(editrow._EAN))
                return;
            frmRibbon_BaseActions(ActionType);
        }

        bool VaidateEAN(string ean)
        {
            if (Utility.IsValidEAN(ean, api.CompanyEntity))
                return true;
            else
                UnicontaMessageBox.Show(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("EANinvalid"), ean), Uniconta.ClientTools.Localization.lookup("Error"));
            return false;
        }

        private void lookupDCAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            SetAccountSource();
        }

        private void SetAccountSource()
        {
            SQLCache cache;
            switch (editrow._DCType)
            {
                case 1: cache = DebtorCache; break;
                case 2: cache = CreditorCache; break;
                case 3: cache = CrmProspectCache; break;
                case 4: cache = null; break;
                default: return;
            }
            editrow.accntSource = cache;
            editrow.NotifyPropertyChanged("AccountSource");
            editrow.NotifyPropertyChanged("DCAccount");
        }
#if !SILVERLIGHT
        private void Email_ButtonClicked(object sender)
        {
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }
#endif
    }

}
