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
using Uniconta.API.Crm;
using Uniconta.API.System;
using Uniconta.API.Inventory;
using System.Collections.ObjectModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreateEmailLisGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmCampaignMemberClient); } }
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

        CWServerFilter invItemFilterDialog = null;
        bool invItemFilterCleared;
        SortingProperties[] invItemDefaultSort;
        Filter[] invItemDefaultFilters;

        CWServerFilter invTransFilterDialog = null;
        bool invTransFilterCleared;
        SortingProperties[] invTransDefaultSort;
        Filter[] invTransDefaultFilters;

        UnicontaBaseEntity master;

        SQLCache InterestCache, ProductCache, Debtors, Creditors, Prospects, Contacts;

        static bool isDebtor, isCreditor, isProspect, isContact, isSearchInInvLns, isOnlyRcdsWthEmail, isTelephone, isDefaultValIsSet;
        static DateTime fromDate, toDate;
        static int interestMatchg, ProdMatchg;
        static ObservableCollection<object> interestValues, productValues;
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

            SetDefaultValuesToSearchFilterControls();
        }

        void SetDefaultValuesToSearchFilterControls()
        {
            if (isDefaultValIsSet)
            {
                cbxDebtor.IsChecked = isDebtor;
                cbxCreditor.IsChecked = isCreditor;
                cbxProspects.IsChecked = isProspect;
                cbxContact.IsChecked = isContact;
                cbxSearchInvLines.IsChecked = isSearchInInvLns;
                cbxOnlyEmails.IsChecked = isOnlyRcdsWthEmail;
                cbxOnlyTelephone.IsChecked = isTelephone;
                if (fromDate.Date != DateTime.MinValue)
                    txtDateFrm.DateTime = fromDate.Date;
                if (toDate.Date != DateTime.MinValue)
                    txtDateTo.DateTime = toDate.Date;
                cmbInterestMatch.SelectedIndex = interestMatchg;
                cmbProductsMatch.SelectedIndex = ProdMatchg;
                cmbInterests.EditValue = interestValues;
                cmbProducts.EditValue = productValues;
            }
        }

        public override Task InitQuery() { return null; }

        async void GetInterestAndProduct()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            InterestCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmInterest)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmInterest));
            cmbInterests.ItemsSource = InterestCache?.GetKeyList();

            ProductCache = Comp.GetCache(typeof(Uniconta.DataModel.CrmProduct)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProduct));
            cmbProducts.ItemsSource = ProductCache?.GetKeyList();

            Debtors = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Debtor));
            Prospects = Comp.GetCache(typeof(Uniconta.DataModel.CrmProspect)) ?? await api.LoadCache(typeof(Uniconta.DataModel.CrmProspect));
            Creditors = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Creditor));
            Contacts = Comp.GetCache(typeof(Uniconta.DataModel.Contact)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Contact));
        }

        void GetDafaultSearchFilterValues()
        {
            isDefaultValIsSet = true;
            isDebtor = (bool)cbxDebtor.IsChecked;
            isCreditor = (bool)cbxCreditor.IsChecked;
            isProspect = (bool)cbxProspects.IsChecked;
            isContact = (bool)cbxContact.IsChecked;
            isSearchInInvLns = (bool)cbxSearchInvLines.IsChecked;
            isOnlyRcdsWthEmail = (bool)cbxOnlyEmails.IsChecked;
            isTelephone = (bool)cbxOnlyTelephone.IsChecked;
            fromDate = txtDateFrm.DateTime;
            toDate = txtDateTo.DateTime;
            interestMatchg = cmbInterestMatch.SelectedIndex;
            ProdMatchg = cmbProductsMatch.SelectedIndex;
            interestValues = cmbInterests.SelectedItems ;
            productValues = cmbProducts.SelectedItems ;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreateEmailList.SelectedItem as CrmCampaignMemberClient;
            switch (ActionType)
            {
                case "Search":
                    GetDafaultSearchFilterValues();
                    LoadEmails();
                    break;
                case "DeleteRow":
                    if (selectedItem != null) ;
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
                        debtorFilterDialog.Show();
                    }
                    else
                        debtorFilterDialog.Show(true);
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
                        prospectFilterDialog.Show();
                    }
                    else
                        prospectFilterDialog.Show(true);
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
                        contactFilterDialog.Show();
                    }
                    else
                        contactFilterDialog.Show(true);
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
                        creditorFilterDialog.Show();
                    }
                    else
                        creditorFilterDialog.Show(true);
                    break;
                case "ClearCreditorFilter":
                    creditorFilterDialog = null;
                    creditorFilterValues = null;
                    creditorFilterCleared = true;
                    break;
                case "InvItemFilter":
                    if (invItemFilterDialog == null)
                    {
                        if (invItemFilterCleared)
                            invItemFilterDialog = new CWServerFilter(api, typeof(InvItemClient), null, invItemDefaultSort, null);
                        else
                            invItemFilterDialog = new CWServerFilter(api, typeof(InvItemClient), invItemDefaultFilters, invItemDefaultSort, null);
                        invItemFilterDialog.Closing += invItemFilterDialog_Closing;
                        invItemFilterDialog.Show();
                    }
                    else
                        invItemFilterDialog.Show(true);
                    break;
                case "ClearInvItemFilter":
                    invItemFilterDialog = null;
                    invItemFilterValues = null;
                    invItemFilterCleared = true;
                    break;
                case "InvTransFilter":
                    if (invTransFilterDialog == null)
                    {
                        if (invTransFilterCleared)
                            invTransFilterDialog = new CWServerFilter(api, typeof(InvTransClient), null, invTransDefaultSort, null);
                        else
                            invTransFilterDialog = new CWServerFilter(api, typeof(InvTransClient), invTransDefaultFilters, invTransDefaultSort, null);
                        invTransFilterDialog.Closing += invTransFilterDialog_Closing;
                        invTransFilterDialog.Show();
                    }
                    else
                        invTransFilterDialog.Show(true);
                    break;
                case "ClearInvTransFilter":
                    invTransFilterDialog = null;
                    invTransFilterValues = null;
                    invTransFilterCleared = true;
                    break;
                case "SaveAndExit":
                    SaveEmailList();
                    break;
                case "SendEmail":
                    SendEmail();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void SendEmail()
        {
            var rows = (ICollection<CrmCampaignMemberClient>)dgCreateEmailList.GetVisibleRows();
            if (rows.Count == 0)
                return;

            var cwSendEmail = new CwSendEmail(api);
            cwSendEmail.Closed += async delegate
            {
                if (cwSendEmail.DialogResult == true && cwSendEmail.CompanySMTP != null)
                {
                    var crmAPI = new CrmAPI(api);
                    ErrorCodes res;
                    if (cwSendEmail.SendTestEmail)
                        res = await crmAPI.SendMailTest(cwSendEmail.CompanySMTP, cwSendEmail.Email, cwSendEmail.Name);
                    else
                        res = await crmAPI.SendMail(cwSendEmail.CompanySMTP, rows, cwSendEmail.FollowUp);
                    UtilDisplay.ShowErrorCode(res);
                }
            };
            cwSendEmail.Show();
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
                this.CloseDockItem();
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
            e.Cancel = true;
            contactFilterDialog.Hide();
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
            e.Cancel = true;
            creditorFilterDialog.Hide();
        }

        public IEnumerable<PropValuePair> invItemFilterValues;
        public FilterSorter invItemPropSort;

        private void cbxSearchInvLines_Checked(object sender, RoutedEventArgs e)
        {
            var isProspectSel = (bool)cbxSearchInvLines.IsChecked;
            cbxProspects.IsChecked = !isProspectSel;
        }

        void invItemFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (invItemFilterDialog.DialogResult == true)
            {
                invItemFilterValues = invItemFilterDialog.PropValuePair;
                invItemPropSort = invItemFilterDialog.PropSort;
            }
            e.Cancel = true;
            invItemFilterDialog.Hide();
        }

        public IEnumerable<PropValuePair> invTransFilterValues;
        public FilterSorter invTransPropSort;

        void invTransFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (invTransFilterDialog.DialogResult == true)
            {
                invTransFilterValues = invTransFilterDialog.PropValuePair;
                invTransPropSort = invTransFilterDialog.PropSort;
            }
            e.Cancel = true;
            invTransFilterDialog.Hide();
        }

        public void SetFilterDefaultFields()
        {
            debtorDefaultFilters = null;
            prospectDefaultFilters = null;
            contactDefaultFilters = null;
            creditorDefaultFilters = null;
            invItemDefaultFilters = null;
            invTransDefaultFilters = null;
        }
        public void SetFilterSortFields()
        {
            debtorDefaultSort = null;
            prospectDefaultSort = null;
            contactDefaultSort = null;
            creditorDefaultSort = null;
            invItemDefaultSort = null;
            invTransDefaultSort = null;
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
            busyIndicator.IsBusy = true;
            var CompanyId = api.CompanyId;

            long Interests = CrmMember.SetMemberMask(cmbInterests.Text, InterestCache);
            long Products = CrmMember.SetMemberMask(cmbProducts.Text, ProductCache);

            var AllInterests = cmbInterestMatch.SelectedIndex == 1;
            var AllProducts = cmbProductsMatch.SelectedIndex == 1;

            var emailLst = new List<CrmCampaignMemberClient>(1050);

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

            HashSet<int> accountWithTrans;
            if (cbxSearchInvLines.IsChecked == true)
                accountWithTrans = await (new Uniconta.API.Inventory.TransactionsAPI(api)).FindAccountsWithTrans(invItemFilterValues, invTransFilterValues, txtDateFrm.DateTime, txtDateTo.DateTime);
            else
                accountWithTrans = null;

            if (cbxDebtor.IsChecked == true)
            {
                IEnumerable<Debtor> debtorEntity;
                if (Debtors != null && (debtorFilterValues == null || debtorFilterValues.Count() == 0))
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
                        if (accountWithTrans != null && !accountWithTrans.Contains(debtor.RowId))
                            continue;
                        if (dublicatePhoneCheck != null)
                        {
                            if (dublicateEmailCheck != null && (debtor._ContactEmail == null || dublicateEmailCheck.Contains(debtor._ContactEmail)))
                                continue;
                            if (debtor._Phone != null)
                            {
                                if (!dublicatePhoneCheck.Add(debtor._Phone))
                                    continue;
                            }
                            else if (debtor._MobilPhone == null || !dublicatePhoneCheck.Add(debtor._MobilPhone))
                                continue;
                        }
                        if (dublicateEmailCheck != null && (debtor._ContactEmail == null || !dublicateEmailCheck.Add(debtor._ContactEmail)))
                            continue;

                        objEmailList = new CrmCampaignMemberClient();
                        objEmailList.dc = debtor;
                        objEmailList.SetMaster(debtor);
                        if (CampaignMaster != null)
                            objEmailList.SetMaster(CampaignMaster);
                        emailLst.Add(objEmailList);
                    }
                }
            }

            if (cbxCreditor.IsChecked == true)
            {
                IEnumerable<Uniconta.DataModel.Creditor> creditorEntity;
                if (Creditors != null && (creditorFilterValues == null || creditorFilterValues.Count() == 0))
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
                        if (accountWithTrans != null && !accountWithTrans.Contains(creditor.RowId))
                            continue;
                        if (dublicatePhoneCheck != null)
                        {
                            if (dublicateEmailCheck != null && (creditor._ContactEmail == null || dublicateEmailCheck.Contains(creditor._ContactEmail)))
                                continue;
                            if (creditor._Phone != null)
                            {
                                if (!dublicatePhoneCheck.Add(creditor._Phone))
                                    continue;
                            }
                            else if (creditor._MobilPhone == null || !dublicatePhoneCheck.Add(creditor._MobilPhone))
                                continue;
                        }
                        if (dublicateEmailCheck != null && (creditor._ContactEmail == null || !dublicateEmailCheck.Add(creditor._ContactEmail)))
                            continue;

                        objEmailList = new CrmCampaignMemberClient();
                        objEmailList.dc = creditor;
                        objEmailList.SetMaster(creditor);
                        if (CampaignMaster != null)
                            objEmailList.SetMaster(CampaignMaster);
                        emailLst.Add(objEmailList);
                    }
                }
            }

            if (cbxProspects.IsChecked == true)
            {
                IEnumerable<CrmProspect> prospectEntity;
                if (Prospects != null && (prospectFilterValues == null || prospectFilterValues.Count() == 0))
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
                        if (dublicatePhoneCheck != null)
                        {
                            if (dublicateEmailCheck != null && (prospect._ContactEmail == null || dublicateEmailCheck.Contains(prospect._ContactEmail)))
                                continue;
                            if (prospect._Phone != null)
                            {
                                if (!dublicatePhoneCheck.Add(prospect._Phone))
                                    continue;
                            }
                            else if (prospect._MobilPhone == null || !dublicatePhoneCheck.Add(prospect._MobilPhone))
                                continue;
                        }
                        if (dublicateEmailCheck != null && (prospect._ContactEmail == null || !dublicateEmailCheck.Add(prospect._ContactEmail)))
                            continue;
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
                if (Contacts != null && (contactFilterValues == null || contactFilterValues.Count() == 0))
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
                        if (dublicateEmailCheck != null && (contact._Email == null || dublicateEmailCheck.Contains(contact._Email)))
                            continue;
                        if (dublicatePhoneCheck != null && (contact._Mobil == null || dublicatePhoneCheck.Contains(contact._Mobil)))
                            continue;

                        // if contact is also a Debtor, Creditor or Prospect we also set that, so we have adresss information
                        IdKey rec = null;
                        switch (contact._DCType)
                        {
                            case 1:
                                rec = Debtors.Get(contact._DCAccount);
                                if (rec != null)
                                {
                                    if (accountWithTrans != null && !accountWithTrans.Contains(rec.RowId))
                                        continue;
                                }
                                break;
                            case 2:
                                rec = Creditors.Get(contact._DCAccount);
                                if (rec != null)
                                {
                                    if (accountWithTrans != null && !accountWithTrans.Contains(rec.RowId))
                                        continue;
                                }
                                break;
                            case 3:
                                rec = Prospects.Get(contact._DCAccount);
                                break;
                        }
                        if (dublicateEmailCheck != null && (contact._Email == null || !dublicateEmailCheck.Add(contact._Email)))
                            continue;
                        if (dublicatePhoneCheck != null && (contact._Mobil == null || !dublicatePhoneCheck.Add(contact._Mobil)))
                            continue;

                        objEmailList = new CrmCampaignMemberClient();
                        if (contact._DCType <= 2)
                            objEmailList.dc = rec as DCAccount;
                        else
                            objEmailList.pros = rec as CrmProspect;
                        objEmailList.cont = contact;
                        objEmailList.SetMaster(contact);
                        if (CampaignMaster != null)
                            objEmailList.SetMaster(CampaignMaster);
                        emailLst.Add(objEmailList);
                    }
                }
            }

            if (emailLst.Count == 0)
                UtilDisplay.ShowErrorCode(ErrorCodes.NoLinesFound);
            else
                emailLst.Sort(new CrmCampaignMemberSort());
            dgCreateEmailList.ItemsSource = null;
            dgCreateEmailList.ItemsSource = emailLst;

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
