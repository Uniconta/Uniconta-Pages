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
using Uniconta.ClientTools.Page;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Pages;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.Common;
using System.Collections;
using System.Text.RegularExpressions;
using Uniconta.ClientTools.Util;
using UnicontaClient;
using UnicontaClient.Models;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreateEmailLisGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmCampaignMemberClient); }}
        public override bool CanInsert { get { return false; } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CreateEmailListPage : GridBasePage
    {
        CWServerFilter debtorFilterDialog = null;
        bool debtorFilterCleared;
        SortingProperties[] debtorDefaultSort;
        Filter[] debtorDefaultFilters;
        TableField[] DebtorUserFields { get; set; }

        CWServerFilter prospectFilterDialog = null;
        bool prospectFilterCleared;
        SortingProperties[] prospectDefaultSort;
        Filter[] prospectDefaultFilters;
        TableField[] ProspectUserFields { get; set; }

        CWServerFilter contactFilterDialog = null;
        bool contactFilterCleared;
        SortingProperties[] contactDefaultSort;
        Filter[] contactDefaultFilters;
        TableField[] ContactUserFields { get; set; }

        CWServerFilter creditorFilterDialog = null;
        bool creditorFilterCleared;
        SortingProperties[] creditorDefaultSort;
        Filter[] creditorDefaultFilters;
        TableField[] CreditorUserFields { get; set; }

        UnicontaBaseEntity master;

        SQLCache InterestCache, ProductCache, Debtors, Creditors, Prospects, Contacts;
        public override string NameOfControl
        {
            get { return TabControls.CreateEmailListPage.ToString(); }
        }

        public override bool IsDataChaged { get { return false; } }

        public CreateEmailListPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        public CreateEmailListPage(UnicontaBaseEntity _master)
           : base(_master)
        {
            master = _master;
            InitPage();
        }

        void InitPage()
        {
            this.DataContext = this;
            InitializeComponent();
            setUserFields();
            dgCreateEmailList.UpdateMaster(master);
            localMenu.dataGrid = dgCreateEmailList;
            dgCreateEmailList.api = api;
            dgCreateEmailList.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCreateEmailList);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            var strMatch = new string[] { Uniconta.ClientTools.Localization.lookup("One"), Uniconta.ClientTools.Localization.lookup("All") };
            cmbInterestMatch.ItemsSource = strMatch;
            cmbInterestMatch.SelectedIndex = 0;
            cmbProductsMatch.ItemsSource = strMatch;
            cmbProductsMatch.SelectedIndex = 0;
            SetFilterDefaultFields();
            SetFilterSortFields();
            GetInterestAndProduct();
            if (master == null)
            {
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "SaveAndExit");
            }
            dgCreateEmailList.ShowTotalSummary();
        }

        public override Task InitQuery() { return null; }

        async void GetInterestAndProduct()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            InterestCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmInterest), api);
            cmbInterests.ItemsSource = InterestCache.GetKeyList();

            ProductCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmProduct), api);
            cmbProducts.ItemsSource = ProductCache.GetKeyList();

            Debtors = Comp.GetCache(typeof(Uniconta.DataModel.Debtor))?? await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api);
            Prospects = Comp.GetCache(typeof(Uniconta.DataModel.CrmProspect)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.CrmProspect), api);
            Creditors = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api);
            Contacts = Comp.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Contact), api);
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreateEmailList.SelectedItem as CrmCampaignMemberClient;
            switch (ActionType)
            {
                case "Search":
                    LoadEmails();
                    break;
                case "DeleteRow":
                    if (selectedItem != null);
                        dgCreateEmailList.DeleteRow();
                    break;

                case "DebtorFilter":

                    if (debtorFilterDialog == null)
                    {
                        if (debtorFilterCleared)
                            debtorFilterDialog = new CWServerFilter(api, typeof(DebtorClient), null, debtorDefaultSort, DebtorUserFields);
                        else
                            debtorFilterDialog = new CWServerFilter(api, typeof(DebtorClient), debtorDefaultFilters, debtorDefaultSort, DebtorUserFields);
                        debtorFilterDialog.Closing += debtorFilterDialog_Closing;
#if !SILVERLIGHT
                        debtorFilterDialog.Show();
                    }
                    else
                        debtorFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    debtorFilterDialog.Show();
#endif
                    break;

                case "ClearDebtorFilter":
                    debtorFilterDialog = null;
                    debtorFilterValues = null;
                    debtorFilterCleared = true;
                    break;

                case "ProspectFilter":

                    if (prospectFilterDialog == null)
                    {
                        if (prospectFilterCleared)
                            prospectFilterDialog = new CWServerFilter(api, typeof(CrmProspectClient), null, prospectDefaultSort, ProspectUserFields);
                        else
                            prospectFilterDialog = new CWServerFilter(api, typeof(CrmProspectClient), prospectDefaultFilters, prospectDefaultSort, ProspectUserFields);
                        prospectFilterDialog.Closing += prospectFilterDialog_Closing;
#if !SILVERLIGHT
                        prospectFilterDialog.Show();
                    }
                    else
                        prospectFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    prospectFilterDialog.Show();
#endif
                    break;

                case "ClearProspectFilter":
                    prospectFilterDialog = null;
                    prospectFilterValues = null;
                    prospectFilterCleared = true;
                    break;

                case "ContactFilter":

                    if (contactFilterDialog == null)
                    {
                        if (contactFilterCleared)
                            contactFilterDialog = new CWServerFilter(api, typeof(ContactClient), null, contactDefaultSort, ContactUserFields);
                        else
                            contactFilterDialog = new CWServerFilter(api, typeof(ContactClient), contactDefaultFilters, contactDefaultSort, ContactUserFields);
                        contactFilterDialog.Closing += contactFilterDialog_Closing;
#if !SILVERLIGHT
                        contactFilterDialog.Show();
                    }
                    else
                        contactFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    contactFilterDialog.Show();
#endif
                    break;

                case "ClearContactFilter":
                    contactFilterDialog = null;
                    contactFilterValues = null;
                    contactFilterCleared = true;
                    break;

                case "CreditorFilter":

                    if (creditorFilterDialog == null)
                    {
                        if (creditorFilterCleared)
                            creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), null, creditorDefaultSort, CreditorUserFields);
                        else
                            creditorFilterDialog = new CWServerFilter(api, typeof(CreditorClient), creditorDefaultFilters, creditorDefaultSort, CreditorUserFields);
                        creditorFilterDialog.Closing += CreditorFilterDialog_Closing;
#if !SILVERLIGHT
                        creditorFilterDialog.Show();
                    }
                    else
                        creditorFilterDialog.Show(true);
#elif SILVERLIGHT
                    }
                    creditorFilterDialog.Show();
#endif
                    break;

                case "ClearCreditorFilter":
                    creditorFilterDialog = null;
                    creditorFilterValues = null;
                    creditorFilterCleared = true;
                    break;

                case "SaveAndExit":
                    SaveEmailList();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

       

        async void SaveEmailList()
        {
            var rows = (ICollection<CrmCampaignMemberClient>)dgCreateEmailList.GetVisibleRows();
            if (rows.Count == 0)
                return;

            var lst = rows as List<CrmCampaignMemberClient>;
            if (lst == null)
                lst = rows.ToList();

            // lets remove the once we have.
            var list = await api.Query<CrmCampaignMemberClient>(master);
            if (list != null)
            {
                foreach (var rec in list)
                    lst.Remove(rec);
            }
            var result = await api.Insert(lst);
            if (result == ErrorCodes.Succes)
                this.dockCtrl.CloseDockItem();
            else
                UtilDisplay.ShowErrorCode(result);
        }

        public IEnumerable<PropValuePair> debtorFilterValues;
        public FilterSorter debtorPropSort;

        void debtorFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (debtorFilterDialog.DialogResult == true)
            {
                debtorFilterValues = debtorFilterDialog.PropValuePair;
                debtorPropSort = debtorFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            debtorFilterDialog.Hide();
#endif
        }

        public IEnumerable<PropValuePair> prospectFilterValues;
        public FilterSorter prospectPropSort;

        void prospectFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (prospectFilterDialog.DialogResult == true)
            {
                prospectFilterValues = prospectFilterDialog.PropValuePair;
                prospectPropSort = prospectFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            prospectFilterDialog.Hide();
#endif
        }

        public IEnumerable<PropValuePair> contactFilterValues;
        public FilterSorter contactPropSort;

        void contactFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (contactFilterDialog.DialogResult == true)
            {
                contactFilterValues = contactFilterDialog.PropValuePair;
                contactPropSort = contactFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            contactFilterDialog.Hide();
#endif
        }

        public IEnumerable<PropValuePair> creditorFilterValues;
        public FilterSorter creditortPropSort;
        private void CreditorFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (creditorFilterDialog.DialogResult == true)
            {
                creditorFilterValues = creditorFilterDialog.PropValuePair;
                creditortPropSort = creditorFilterDialog.PropSort;
            }
#if !SILVERLIGHT
            e.Cancel = true;
            creditorFilterDialog.Hide();
#endif
        }

        public void SetFilterDefaultFields()
        {
            debtorDefaultFilters = null;
            prospectDefaultFilters = null;
            contactDefaultFilters = null;
            creditorDefaultFilters = null;
        }
        public void SetFilterSortFields()
        {
            debtorDefaultSort = null;
            prospectDefaultSort = null;
            contactDefaultSort = null;
            creditorDefaultSort = null;
        }

        static bool HasMatch(long maskUser, long mask, bool MatchAll)
        {
            if (mask == 0)
                return true;
            if (MatchAll)
                return (maskUser & mask) == mask;
            else
                return (maskUser & mask) != 0;
        }

        async void LoadEmails()
        {
            var emailList = new List<CrmCampaignMemberClient>();
            busyIndicator.IsBusy = true;
            var CompanyId = api.CompanyId;

            long Interests = CrmMember.SetMemberMask(cmbInterests.Text, InterestCache);
            long Products = CrmMember.SetMemberMask(cmbProducts.Text, ProductCache);

            var AllInterests = cmbInterestMatch.SelectedIndex == 1;
            var AllProducts = cmbProductsMatch.SelectedIndex == 1;

            var emailLst = new List<CrmCampaignMemberClient>();

            var CampaignMaster = this.master;
            CrmCampaignMemberClient objEmailList;

            HashSet<string> dublicateEmailCheck;
            if (cbxOnlyEmails.IsChecked == true)
                dublicateEmailCheck = new HashSet<string>(StringNoCaseCompare.GetCompare());
            else
                dublicateEmailCheck = null;

            HashSet<string> dublicatePhoneCheck;
            if (cbxOnlyTelephone.IsChecked == true)
                dublicatePhoneCheck = new HashSet<string>(StringNoCaseCompare.GetCompare());
            else
                dublicatePhoneCheck = null;

            if (cbxDebtor.IsChecked == true)
            {
                IEnumerable<Debtor> debtorEntity;
                if (Debtors != null && (debtorFilterValues == null || !debtorFilterValues.Any()))
                    debtorEntity = (IEnumerable<Debtor>)Debtors.GetNotNullArray;
                else
                    debtorEntity = await api.Query<DebtorClient>(debtorFilterValues);
                if (debtorEntity != null)
                {
                    foreach (var debtor in debtorEntity)
                    {
                        if (!HasMatch(debtor._Interests, Interests, AllInterests))
                            continue;
                        if (!HasMatch(debtor._Products, Products, AllProducts))
                            continue;
                        if (dublicateEmailCheck != null && (debtor._ContactEmail == null || !dublicateEmailCheck.Add(debtor._ContactEmail)))
                            continue;
                        if (dublicatePhoneCheck != null)
                        {
                            if (debtor._Phone != null)
                            {
                                if (!dublicatePhoneCheck.Add(debtor._Phone))
                                    continue;
                            }
                            else if (debtor._MobilPhone == null || !dublicatePhoneCheck.Add(debtor._MobilPhone))
                                continue;
                        }
                        objEmailList = new CrmCampaignMemberClient();
                        objEmailList.dc = debtor;
                        objEmailList.SetMaster(debtor);
                        if (CampaignMaster != null)
                            objEmailList.SetMaster(CampaignMaster);
                        emailLst.Add(objEmailList);
                    }
                }
            }

            if (cbxProspects.IsChecked == true)
            {
                IEnumerable<CrmProspect> prospectEntity;
                if (Prospects != null && (prospectFilterValues == null || !prospectFilterValues.Any()))
                    prospectEntity = (IEnumerable<CrmProspect>)Prospects.GetNotNullArray;
                else
                    prospectEntity = await api.Query<CrmProspectClient>(prospectFilterValues);
                if (prospectEntity != null)
                {
                    foreach (var prospect in prospectEntity)
                    {
                        if (!HasMatch(prospect._Interests, Interests, AllInterests))
                            continue;
                        if (!HasMatch(prospect._Products, Products, AllProducts))
                            continue;
                        if (dublicateEmailCheck != null && (prospect._ContactEmail == null || !dublicateEmailCheck.Add(prospect._ContactEmail)))
                            continue;
                        if (dublicatePhoneCheck != null)
                        {
                            if (prospect._Phone != null)
                            {
                                if (!dublicatePhoneCheck.Add(prospect._Phone))
                                    continue;
                            }
                            else if (prospect._MobilPhone == null || !dublicatePhoneCheck.Add(prospect._MobilPhone))
                                continue;
                        }
                        objEmailList = new CrmCampaignMemberClient();
                        objEmailList.pros = prospect;
                        objEmailList.SetMaster(prospect);
                        if (CampaignMaster != null)
                            objEmailList.SetMaster(CampaignMaster);
                        emailLst.Add(objEmailList);
                    }
                }
            }

            if (cbxContact.IsChecked == true)
            {
                IEnumerable<Contact> contactEntity;
                if (Contacts != null && (contactFilterValues == null || !contactFilterValues.Any()))
                    contactEntity = (IEnumerable<Contact>)Contacts.GetNotNullArray;
                else
                    contactEntity = await api.Query<ContactClient>(contactFilterValues);
                if (contactEntity != null)
                {
                    foreach (var contact in contactEntity)
                    {
                        if (!HasMatch(contact._Interests, Interests, AllInterests))
                            continue;
                        if (!HasMatch(contact._Products, Products, AllProducts))
                            continue;
                        if (dublicateEmailCheck != null && (contact._Email == null || !dublicateEmailCheck.Add(contact._Email)))
                            continue;
                        if (dublicatePhoneCheck != null && (contact._Mobil == null || !dublicatePhoneCheck.Add(contact._Mobil)))
                            continue;
                        objEmailList = new CrmCampaignMemberClient();
                        objEmailList.cont = contact;
                        objEmailList.SetMaster(contact);
                        if (CampaignMaster != null)
                            objEmailList.SetMaster(CampaignMaster);

                        // if contact is also a Debtor, Creditor or Prospect we also set that, so we have adresss information
                        IdKey rec;
                        switch (contact._DCType)
                        {
                            case 1:
                                rec = Debtors.Get(contact._DCAccount);
                                if (rec != null)
                                    objEmailList.dc = (DCAccount)rec;
                                break;
                            case 2:
                                rec = Creditors.Get(contact._DCAccount);
                                if (rec != null)
                                    objEmailList.dc = (DCAccount)rec;
                                break;
                            case 3:
                                rec = Prospects.Get(contact._DCAccount);
                                if (rec != null)
                                    objEmailList.pros = (CrmProspect)rec;
                                break;
                        }
                        emailLst.Add(objEmailList);
                    }
                }
            }

            if (cbxCreditor.IsChecked == true)
            {
                IEnumerable<Uniconta.DataModel.Creditor> creditorEntity;
                if (Creditors != null && (creditorFilterValues == null || !creditorFilterValues.Any()))
                    creditorEntity = (IEnumerable<Uniconta.DataModel.Creditor>)Creditors.GetNotNullArray;
                else
                    creditorEntity = await api.Query<CreditorClient>(creditorFilterValues);
                if (creditorEntity != null)
                {
                    foreach (var creditor in creditorEntity)
                    {
                        if (!HasMatch(creditor._Interests, Interests, AllInterests))
                            continue;
                        if (!HasMatch(creditor._Products, Products, AllProducts))
                            continue;
                        if (dublicateEmailCheck != null && (creditor._ContactEmail == null || !dublicateEmailCheck.Add(creditor._ContactEmail)))
                            continue;
                        if (dublicatePhoneCheck != null)
                        {
                            if (creditor._Phone != null)
                            {
                                if (!dublicatePhoneCheck.Add(creditor._Phone))
                                    continue;
                            }
                            else if (creditor._MobilPhone == null || !dublicatePhoneCheck.Add(creditor._MobilPhone))
                                continue;
                        }
                        objEmailList = new CrmCampaignMemberClient();
                        objEmailList.dc = creditor;
                        objEmailList.SetMaster(creditor);
                        if (CampaignMaster != null)
                            objEmailList.SetMaster(CampaignMaster);
                        emailLst.Add(objEmailList);
                    }
                }
            }

            dgCreateEmailList.ItemsSource = null;

            emailList = emailLst.OrderBy(x => x.Name).ToList();
            dgCreateEmailList.ItemsSource = emailList;

            busyIndicator.IsBusy = false;
            dgCreateEmailList.Visibility = Visibility.Visible;
        }

        private UnicontaBaseEntity[] SortList(UnicontaBaseEntity[] lstEntity, UnicontaBaseEntity obj, FilterSorter propSort)
        {
            int len = lstEntity.Count();
            if (len > 1)
            {
                if (propSort != null)
                    Array.Sort(lstEntity, propSort);
                else
                {
                    var sort = GetValidSort(obj);
                    if (sort != null)
                        Array.Sort(lstEntity, sort);
                }
            }

            return lstEntity;
        }

        public IComparer GridSorting { get { return null; } }
        static IComparer defltCmpKeyStr;

        private IComparer GetValidSort(UnicontaBaseEntity obj)
        {
            var sort = GridSorting;
            if (sort == null)
            {
                if (obj is IdKey)
                {
                    if (defltCmpKeyStr == null)
                        defltCmpKeyStr = Uniconta.Common.SQLCache.KeyStrSorter;
                    sort = defltCmpKeyStr;
                }
            }
            return sort;
        }

        void setUserFields()
        {
            var Comp = api.CompanyEntity;

            var debtorRow = new DebtorClient();
            debtorRow.SetMaster(Comp);
            var debtorUserField = debtorRow.UserFieldDef();
            if (debtorUserField != null)
            {
                DebtorUserFields = debtorUserField;
            }

            var prospectRow = new CrmProspectClient();
            prospectRow.SetMaster(Comp);
            var propspectUserField = prospectRow.UserFieldDef();
            if (propspectUserField != null)
            {
                ProspectUserFields = propspectUserField;
            }

            var contactRow = new ContactClient();
            contactRow.SetMaster(Comp);
            var contactUserField = contactRow.UserFieldDef();
            if (contactUserField != null)
            {
                ProspectUserFields = contactUserField;
            }

            var creditorRow = new CreditorClient();
            creditorRow.SetMaster(Comp);
            var creditorUserField = creditorRow.UserFieldDef();
            if (creditorUserField != null)
                CreditorUserFields = creditorUserField;
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return CreateEmailListPage.HandleLookupOnLocalPage(dgCreateEmailList, lookup);
        }

        static public LookUpTable HandleLookupOnLocalPage(CorasauDataGrid grid, LookUpTable lookup)
        {
            var campaignMem = grid.SelectedItem as CrmCampaignMemberClient;
            if (campaignMem == null)
                return lookup;
            if (grid.CurrentColumn?.Name == "Account")
            {
                switch ((int)campaignMem._DCType)
                {
                    case 1:
                        lookup.TableType = typeof(Uniconta.DataModel.Debtor);
                        break;
                    case 2:
                        lookup.TableType = typeof(Uniconta.DataModel.Creditor);
                        break;
                    case 3:
                        lookup.TableType = typeof(Uniconta.DataModel.CrmProspect);
                        break;
                }
            }
            return lookup;
        }
    }
}
