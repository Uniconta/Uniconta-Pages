using UnicontaClient.Models;
using UnicontaClient.Utilities;
using System;
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
using Uniconta.API.Crm;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CrmFollowUpPage2 : FormBasePage
    {
        CrmFollowUpClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            ((CrmFollowUpClient)RefreshParams[1]).NotifyPropertyChanged("UserField");
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.CrmFollowUpPage2; } }

        public override Type TableType { get { return typeof(CrmFollowUpClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CrmFollowUpClient)value; } }

        UnicontaBaseEntity master;
        CrmCampaignClient campaignClient;
        ProjectClient project;
        CrudAPI crudAPI;
        bool isCopiedRow = false;
        public CrmFollowUpPage2(UnicontaBaseEntity sourcedata, bool isEdit, UnicontaBaseEntity master = null)
            : base(sourcedata, isEdit)
        {
            isCopiedRow = !isEdit;
            this.master = master;
            InitPage(api);
        }
        public CrmFollowUpPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(deCreated, deCreated);
#endif
        }

        private CrmFollowUpClient NewRow;

        public CrmFollowUpPage2(CrudAPI crudApi, UnicontaBaseEntity sourceData) : base(crudApi, null)
        {
            campaignClient = sourceData as CrmCampaignClient;
            project = master as ProjectClient;
            InitPage(crudApi);
        }

        void InitPage(CrudAPI crudapi)
        {
            crudAPI = crudapi;
            var Comp = crudAPI.CompanyEntity;
            InitializeComponent();
            StartLoadCache();
            layoutControl = layoutItems;
            deCreated.IsReadOnly = true;
            deCreated.AllowDefaultButton = false;
            if (campaignClient != null)
            {
                RibbonBase rb = (RibbonBase)frmRibbon.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, new string[] { "Delete", "Layout", "Templates" });
                if (!isCopiedRow)
                {
                    NewRow = CreateNew() as CrmFollowUpClient;
                    NewRow.SetMaster(master ?? crudapi.CompanyEntity);
                }
            }
            if (editrow == null && LoadedRow == null && campaignClient == null)
            {
                frmRibbon.DisableButtons("Delete");
                if (!isCopiedRow)
                {
                    editrow = CreateNew() as CrmFollowUpClient;
                    editrow.SetMaster(master ?? crudapi.CompanyEntity);
                }

                deCreated.IsReadOnly = false;
                deCreated.AllowDefaultButton = true;
                liUpdatedAt.Visibility = Visibility.Collapsed;
            }
            if(isCopiedRow)
                frmRibbon.DisableButtons("Delete");
            lookupDCAccount.api = leGroup.api = leEmployee.api = leOfferNumber.api = crudAPI;
            if (campaignClient == null)
                layoutItems.DataContext = editrow;
            else if (campaignClient != null)
                layoutItems.DataContext = NewRow;

            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            liOfferNumber.ButtonClicked += liOfferNumber_ButtonClicked;
            BindContact();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            itmDCType.Visibility = itemDCAccount.Visibility = (master == null) ? Visibility.Visible : Visibility.Collapsed;
            liContact.Visibility = master == null ? Visibility.Collapsed : Visibility.Visible;
        }

        void BindContact()
        {
            var dcAccount = master as DCAccount;
            if (dcAccount == null)
                return;
            var contactCacheFilter = new ContactCacheFilter(ContactCache, dcAccount.__DCType(), dcAccount._Account);
            editrow.contactSource = contactCacheFilter;
            editrow.NotifyPropertyChanged("ContactSource");
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                if (editrow != null && !CheckMandatoryUserField(editrow))
                    return;
                if (NewRow != null && !CheckMandatoryUserField(NewRow))
                    return;
                if (campaignClient != null)
                {
                    CreateFollowUps();
                    return;
                }
            }
            if (campaignClient == null)
                frmRibbon_BaseActions(ActionType);
        }

        async void CreateFollowUps()
        {
            var err = await (new CrmAPI(crudAPI)).CreateFollowUpMembers(campaignClient, NewRow);
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            else
                CloseDockItem();
        }

        private void lookupDCAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            SetAccountSource();
        }

        SQLCache DebtorCache, CreditorCache, CrmProspectCache, ContactCache, ProjectCache;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            DebtorCache = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api).ConfigureAwait(false);
            CrmProspectCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmProspect)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmProspect), api).ConfigureAwait(false);
            ContactCache = Comp.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Contact), api).ConfigureAwait(false);
            ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api).ConfigureAwait(false);


            Dispatcher.BeginInvoke(new Action(() => SetAccountSource()));
        }

        private void liOfferNumber_ButtonClicked(object sender)
        {
            AddDockItem(TabControls.DebtorOffers, editrow);
        }

        private void SetAccountSource()
        {
            SQLCache cache;
            if (editrow != null)
            {
                switch (editrow._DCType)
                {
                    case CrmCampaignMemberType.Debtor: cache = DebtorCache; break;
                    case CrmCampaignMemberType.Creditor: cache = CreditorCache; break;
                    case CrmCampaignMemberType.Prospect: cache = CrmProspectCache; break;
                    case CrmCampaignMemberType.Contact: cache = ContactCache; break;
                    case CrmCampaignMemberType.Project: cache = ProjectCache; break;
                    default: cache = null; break;
                }
                editrow.accntSource = cache;
                editrow.NotifyPropertyChanged("AccountSource");
                editrow.NotifyPropertyChanged("DCAccount");
            }
            else if (NewRow != null)
            {
                switch (NewRow._DCType)
                {
                    case CrmCampaignMemberType.Debtor: cache = DebtorCache; break;
                    case CrmCampaignMemberType.Creditor: cache = CreditorCache; break;
                    case CrmCampaignMemberType.Prospect: cache = CrmProspectCache; break;
                    case CrmCampaignMemberType.Contact: cache = ContactCache; break;
                    case CrmCampaignMemberType.Project: cache = ProjectCache; break;
                    default: cache = null; break;
                }
                NewRow.accntSource = cache;
                NewRow.NotifyPropertyChanged("AccountSource");
                NewRow.NotifyPropertyChanged("DCAccount");
            }
        }

        private void leContact_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var contact = leContact.SelectedItem as Contact;
            if (contact != null)
                editrow.SetMaster(contact);
            else
            {
                var dcAccount = master as DCAccount;
                editrow._DCType = (CrmCampaignMemberType)dcAccount.__DCType();
                editrow._Account = dcAccount._Account;
                editrow.NotifyPropertyChanged("DCType");
                editrow.NotifyPropertyChanged("DCAccount");
            }
        }

#if !SILVERLIGHT
        private void Email_ButtonClicked(object sender)
        {
            var txtEmail = ((CorasauLayoutItem)sender).Content as TextEditor;
            if (txtEmail == null)
                return;
            var mail = string.Concat("mailto:", txtEmail.Text);
            System.Diagnostics.Process proc = new System.Diagnostics.Process();
            proc.StartInfo.FileName = mail;
            proc.Start();
        }

        private void liZipCode_ButtonClicked(object sender)
        {
            var location = editrow.Address1 + "+" + editrow.Address2 + "+" + editrow.Address3 + "+" + editrow.ZipCode + "+" + editrow.City + "+" + editrow.Country;
            Utility.OpenGoogleMap(location);
        }

        private void liWww_ButtonClicked(object sender)
        {
            Utility.OpenWebSite(editrow.Www);
        }
#endif
    }
}
