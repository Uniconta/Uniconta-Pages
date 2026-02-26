using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel.Syndication;
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
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UpdateDebAddressViaCvrGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return GetTableType(); } }
        public override bool Readonly { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool IsAutoSave { get { return false; } }

        public int DCtype;

        Type GetTableType()
        {
            if (DCtype == 0)
                return typeof(DebtorClientLocal);
            else if (DCtype == 1)
                return typeof(CreditorClientLocal);
            else
                return typeof(CrmProspectClientLocal);
        }
    }
    public partial class UpdateDebAddressViaCvr : GridBasePage
    {
        UnicontaBaseEntity[] records;
        bool isSearch;

        static bool? updName, updAddress, updIndustryCode, updCompanyState;
        ItemBase iUpdName, iUpdAddress, iUpdIndustryCode, iUpdCompanyState;


        public UpdateDebAddressViaCvr(BaseAPI API) : base(API, string.Empty)
        {
            InitPage();
        }

        public UpdateDebAddressViaCvr(UnicontaBaseEntity[] records) : base(records, 0,0)
        {
            this.records = records;
            InitPage();
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = false;
            return false;
        }

        public override bool IsCalculatedFieldsToBeHandled()
        {
            return false;
        }

        SQLCache IndustryCodes;
        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgUpdateDebtorAddress;
            SetRibbonControl(localMenu, dgUpdateDebtorAddress);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            localMenu.OnChecked += LocalMenu_OnChecked;
            GetMenuItem();
            updName = updName ?? true;
            updAddress = updAddress ?? true;
            updIndustryCode = updIndustryCode ?? true;
            updCompanyState = updCompanyState ?? true;

            SetFieldsVisibility();
            IndustryCodes = api.GetCache(typeof(IndustryCode));
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Remove":
                    dgUpdateDebtorAddress.RemoveFocusedRowFromGrid();
                    break;
                case "Search":
                    isSearch = true && records != null;
                    LoadGrid();
                    break;
                case "SaveGrid":
                    SaveGrid();
                    break;
            }
        }

        private void LocalMenu_OnChecked(string actionType, bool IsChecked)
        {
            switch (actionType)
            {
                case "UpdName":
                    updName = IsChecked;
                    iUpdName.IsChecked = updName.GetValueOrDefault();
                    
                    break;
                case "UpdAddress":
                    updAddress = IsChecked;
                    iUpdAddress.IsChecked = updAddress.GetValueOrDefault();
                    break;
                case "UpdIndustryCode":
                    updIndustryCode = IsChecked;
                    iUpdIndustryCode.IsChecked = updIndustryCode.GetValueOrDefault();
                    break;
                case "UpdCompanyState":
                    updCompanyState = IsChecked;
                    iUpdCompanyState.IsChecked = updCompanyState.GetValueOrDefault();
                    break;
            }
            SetFieldsVisibility();
        }

        private void SetFieldsVisibility()
        {
            iUpdName.IsChecked = updName.GetValueOrDefault();
            iUpdAddress.IsChecked = updAddress.GetValueOrDefault();
            iUpdIndustryCode.IsChecked = updIndustryCode.GetValueOrDefault();
            iUpdCompanyState.IsChecked = updCompanyState.GetValueOrDefault();

            NewName.Visible = updName.GetValueOrDefault();
            DifName.Visible = updName.GetValueOrDefault();
            NewAddress.Visible = updAddress.GetValueOrDefault();
            Address1.Visible = updAddress.GetValueOrDefault();
            NewAddress2.Visible = updAddress.GetValueOrDefault();
            Address2.Visible = updAddress.GetValueOrDefault();
            DifAddress.Visible = updAddress.GetValueOrDefault();
            NewZipCode.Visible = updAddress.GetValueOrDefault();
            ZipCode.Visible = updAddress.GetValueOrDefault();
            DifZipCode.Visible = updAddress.GetValueOrDefault();
            
            NewCity.Visible = updAddress.GetValueOrDefault();
            City.Visible = updAddress.GetValueOrDefault();
            DifCity.Visible = updAddress.GetValueOrDefault();
            
            NewIndustryCode.Visible = updIndustryCode.GetValueOrDefault();
            IndustryCode.Visible = updIndustryCode.GetValueOrDefault();
            DifIndustryCode.Visible = updIndustryCode.GetValueOrDefault();
            NewCompanyState.Visible = updCompanyState.GetValueOrDefault();
            CompanyState.Visible = updCompanyState.GetValueOrDefault();
            DifCompanyState.Visible = updCompanyState.GetValueOrDefault();
        }


        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            iUpdName = UtilDisplay.GetMenuCommandByName(rb, "UpdName");
            iUpdAddress = UtilDisplay.GetMenuCommandByName(rb, "UpdAddress");
            iUpdIndustryCode = UtilDisplay.GetMenuCommandByName(rb, "UpdIndustryCode");
            iUpdCompanyState = UtilDisplay.GetMenuCommandByName(rb, "UpdCompanyState");
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (rec.Name == null)
                {
                    if (rec.Value == "Debtor")
                    {
                        dgUpdateDebtorAddress.DCtype = 0;
                        SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("Debtor"), ": ", Uniconta.ClientTools.Localization.lookup("UpdateAddress")));
                    }
                    else if (rec.Value == "Creditor")
                    {
                        dgUpdateDebtorAddress.DCtype = 1;
                        SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("Creditor"), ": ", Uniconta.ClientTools.Localization.lookup("UpdateAddress")));
                    }
                    else if (rec.Value == "Prospect")
                    {
                        Account.Visible = false;
                        dgUpdateDebtorAddress.DCtype = 2;
                        SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("Prospect"), ": ", Uniconta.ClientTools.Localization.lookup("UpdateAddress")));
                    }
                }
            }
            base.SetParameter(Parameters);
        }

        public override Task InitQuery()
        {
            if (records != null && records.Length > 0)
                LoadGrid();
            return null;
        }

        protected override async Task LoadCacheInBackGroundAsync()
        {
            if (IndustryCodes == null)
                IndustryCodes = await api.LoadCache(typeof(IndustryCode));
        }

        void LoadGrid()
        {
            SetBusy();
            if (dgUpdateDebtorAddress.DCtype == 0)
                LoadDebtorList();
            else if (dgUpdateDebtorAddress.DCtype == 1)
                LoadCreditorList();
            else
                LoadProspectList();
            dgUpdateDebtorAddress.Visibility = Visibility.Visible;
            ClearBusy();
        }

        static bool Equal(string s1, string s2)
        {
            if (s1 != null)
            {
                s1 = s1.Trim();
                if (s1.Length == 0)
                    s1 = null;
            }
            return string.Compare(s1, s2, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        async void LoadDebtorList()
        {
            DebtorClient[] debtors;
            DebtorClient[] debtorsSelected = null;
            if (records == null || isSearch)
            {
                debtorsSelected = isSearch && records != null ? records.OfType<DebtorClient>().ToArray() : null;
                var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
                debtors = await api.Query<DebtorClient>(filter);
            }
            else
                debtors = records.OfType<DebtorClient>().ToArray();

            if (debtors == null || debtors.Length == 0)
                return;
            int counterFound = 0;
            int cntTotal = 0;
            var newDebList = new List<DebtorClientLocal>();
            foreach (var debtor in debtors)
            {
                var cvr = debtor._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;

                if (isSearch && !debtorsSelected.Any(s => s._Account == debtor._Account))
                    continue;

                cntTotal++;
                busyIndicator.IsBusy = true;
                CompanyInfo ci = null;
                try
                {
                    ci = await CVR.CheckCountry(cvr, debtor._Country);
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                {
                    counterFound++;
                    var address = ci.address;
                    var streetAddress = address.CompleteStreet;

                    string code = null;
                    if (IndustryCodes != null)
                        code = IndustryCodes.Get(ci.industrycode?.code)?.KeyStr;

                    var difName = ci.life.name != null && !Equal(ci.life.name, debtor._Name);
                    var difAddress = streetAddress != null && !Equal(streetAddress, debtor._Address1);  
                    var difAddress2 = address.street2 != null && !Equal(address.street2, debtor._Address2);  
                    var difZipCode = address.zipcode != null && !Equal(address.zipcode, debtor._ZipCode);
                    var difCity = address.cityname != null && !Equal(address.cityname, debtor._City);
                    var difIndustryCode = code != null && !Equal(ci.industrycode?.code, debtor._IndustryCode);
                    var difCompanyState = ci.companystatus != null && ci.companystatus.StatusCode() != debtor._StateOfCompany;

                    if (!difName && !difAddress && !difZipCode && !difCity && !difIndustryCode && !difCompanyState)
                        continue;

                    var newDebtor = new DebtorClientLocal();
                    StreamingManager.Copy(debtor, newDebtor);

                    newDebtor.NewAddress = streetAddress?.Substring(0, Math.Min(60, streetAddress.Length));
                    newDebtor.NewAddress2 = address.street2;
                    newDebtor.NewZipCode = address.zipcode;
                    newDebtor.NewCity = address.cityname?.Substring(0, Math.Min(30, address.cityname.Length));
                    newDebtor.NewName = ci.life.name;

                    newDebtor.DifName = difName;
                    newDebtor.DifAddress = difAddress;
                    newDebtor.DifAddress2 = difAddress2;
                    newDebtor.DifZipCode = difZipCode;
                    newDebtor.DifCity = difCity;
                    newDebtor.DifIndustryCode = difIndustryCode;
                    newDebtor.DifCompanyState = difCompanyState;

                    if (IndustryCodes != null)
                        newDebtor.NewIndustryCode = code;
                    else
                        newDebtor.NewIndustryCode = ci.industrycode?.code;
                    var state = ci.companystatus;
                    if (state != null)
                    {
                        newDebtor._companyState = state.StatusCode();
                        newDebtor.NotifyPropertyChanged("NewCompanyState");
                    }
                  
                    
                    newDebList.Add(newDebtor);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(newDebList.Count);
                    busyIndicator.IsBusy = false;
                }
            }

            ClearBusy();
            dgUpdateDebtorAddress.ItemsSource = newDebList;
            SetStatusText(cntTotal, counterFound, newDebList.Count);
        }

        async void LoadCreditorList()
        {
            CreditorClient[] creditors;
            CreditorClient[] creditorsSelected = null;
            if (records == null || isSearch)
            {
                creditorsSelected = isSearch && records != null ? records.OfType<CreditorClient>().ToArray() : null;
                var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
                creditors = await api.Query<CreditorClient>(filter);
            }
            else
                creditors = records.OfType<CreditorClient>().ToArray();

            if (creditors == null || creditors.Length == 0)
                return;
            int counterFound = 0;
            int cntTotal = 0;
            var newCredList = new List<CreditorClientLocal>();

            foreach (var creditor in creditors)
            {
                var cvr = creditor._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;

                if (isSearch && !creditorsSelected.Any(s => s._Account == creditor._Account))
                    continue;

                cntTotal++;
                busyIndicator.IsBusy = true;
                CompanyInfo ci = null;
                try
                {
                    ci = await CVR.CheckCountry(cvr, creditor._Country);
                }
                catch { }
                if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                {
                    counterFound++;
                    var address = ci.address;
                    var streetAddress = address.CompleteStreet;

                    string code = null;
                    if (IndustryCodes != null)
                        code = IndustryCodes.Get(ci.industrycode?.code)?.KeyStr;

                    var difName = ci.life.name != null && !Equal(ci.life.name, creditor._Name);
                    var difAddress = streetAddress != null && !Equal(streetAddress, creditor._Address1);
                    var difAddress2 = address.street2 != null && !Equal(address.street2, creditor._Address2);
                    var difZipCode = address.zipcode != null && !Equal(address.zipcode, creditor._ZipCode);
                    var difCity = address.cityname != null && !Equal(address.cityname, creditor._City);
                    var difIndustryCode = code != null && !Equal(ci.industrycode?.code, creditor._IndustryCode);
                    var difCompanyState = ci.companystatus != null && ci.companystatus.StatusCode() != creditor._StateOfCompany;

                    if (!difName && !difAddress && !difZipCode && !difCity && !difIndustryCode && !difCompanyState)
                        continue;

                    var newCreditor = new CreditorClientLocal();
                    StreamingManager.Copy(creditor, newCreditor);

                    newCreditor.NewAddress = streetAddress;
                    newCreditor.NewAddress2 = address.street2;
                    newCreditor.NewZipCode = address.zipcode;
                    newCreditor.NewCity = address.cityname;
                    newCreditor.NewName = ci.life.name;

                    newCreditor.DifName = difName;
                    newCreditor.DifAddress = difAddress;
                    newCreditor.DifAddress2 = difAddress2;
                    newCreditor.DifZipCode = difZipCode;
                    newCreditor.DifCity = difCity;
                    newCreditor.DifIndustryCode = difIndustryCode;
                    newCreditor.DifCompanyState = difCompanyState;

                    if (IndustryCodes != null)
                        newCreditor.NewIndustryCode = code;
                    else
                        newCreditor.NewIndustryCode = ci.industrycode?.code;

                    var state = ci.companystatus;
                    if (state != null)
                    {
                        newCreditor._companyState = state.StatusCode();
                        newCreditor.NotifyPropertyChanged("NewCompanyState");
                    }
                    newCredList.Add(newCreditor);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(newCredList.Count);
                    busyIndicator.IsBusy = false;
                }
            }
            ClearBusy();
            dgUpdateDebtorAddress.ItemsSource = newCredList;
            SetStatusText(cntTotal, counterFound, newCredList.Count);
        }

        async void LoadProspectList()
        {
            CrmProspectClient[] prospects;
            CrmProspectClient[] prospectsSelected = null;
            if (records == null || isSearch)
            {
                prospectsSelected = isSearch && records != null ? records.OfType<CrmProspectClient>().ToArray() : null;
                var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
                prospects = await api.Query<CrmProspectClient>(filter);
            }
            else
                prospects = records.OfType<CrmProspectClient>().ToArray();

            if (prospects == null || prospects.Length == 0)
                return;

            int counterFound = 0;
            int cntTotal = 0;
            var newProsList = new List<CrmProspectClientLocal>();
            foreach (var prospect in prospects)
            {
                var cvr = prospect._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;

                if (isSearch && !prospectsSelected.Any(s => s._RowId == prospect._RowId))
                    continue;

                busyIndicator.IsBusy = true;
                CompanyInfo ci = null;
                try
                {
                    ci = await CVR.CheckCountry(cvr, prospect._Country);
                }
                catch { }
                if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                {
                    counterFound++;
                    var address = ci.address;
                    var streetAddress = address.CompleteStreet;

                    string code = null;
                    if (IndustryCodes != null)
                        code = IndustryCodes.Get(ci.industrycode?.code)?.KeyStr;

                    var difName = ci.life.name != null && !Equal(ci.life.name, prospect._Name);
                    var difAddress = streetAddress != null && !Equal(streetAddress, prospect._Address1);
                    var difAddress2 = address.street2 != null && !Equal(address.street2, prospect._Address2);
                    var difZipCode = address.zipcode != null && !Equal(address.zipcode, prospect._ZipCode);
                    var difCity = address.cityname != null && !Equal(address.cityname, prospect._City);
                    var difIndustryCode = code != null && !Equal(ci.industrycode?.code, prospect._IndustryCode);
                    var difCompanyState = ci.companystatus != null && ci.companystatus.StatusCode() != prospect._StateOfCompany;

                    if (!difName && !difAddress && !difZipCode && !difCity && !difIndustryCode && !difCompanyState)
                        continue;

                    var newProspect = new CrmProspectClientLocal();
                    StreamingManager.Copy(prospect, newProspect);

                    newProspect.NewAddress = streetAddress;
                    newProspect.NewAddress2 = address.street2;
                    newProspect.NewZipCode = address.zipcode;
                    newProspect.NewCity = address.cityname;
                    newProspect.NewName = ci.life.name;

                    newProspect.DifName = difName;
                    newProspect.DifAddress = difAddress;
                    newProspect.DifAddress2 = difAddress2;
                    newProspect.DifZipCode = difZipCode;
                    newProspect.DifCity = difCity;
                    newProspect.DifIndustryCode = difIndustryCode;
                    newProspect.DifCompanyState = difCompanyState;

                    if (IndustryCodes != null)
                        newProspect.NewIndustryCode = code;
                    else
                        newProspect.NewIndustryCode = ci.industrycode?.code;

                    var state = ci.companystatus;
                    if (state != null)
                    {
                        newProspect._companyState = state.StatusCode();
                        newProspect.NotifyPropertyChanged("NewCompanyState");
                    }
                    newProsList.Add(newProspect);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(newProsList.Count);
                    busyIndicator.IsBusy = false;
                }
            }
            ClearBusy();
            dgUpdateDebtorAddress.ItemsSource = newProsList;
            SetStatusText(cntTotal, counterFound, newProsList.Count);
        }

        async void SaveGrid()
        {
            Task<ErrorCodes> t;
            SetBusy();
            if (dgUpdateDebtorAddress.DCtype == 0)
                t = UpdateDebtorList();
            else if (dgUpdateDebtorAddress.DCtype == 1)
                t = UpdateCreditorList();
            else
                t = UpdateProspectList();
            if (t != null)
            {
                var err = await t;
                ClearBusy();
                UtilDisplay.ShowErrorCode(err);
                dgUpdateDebtorAddress.ItemsSource = null;
            }
            else
                ClearBusy();
        }

        Task<ErrorCodes> UpdateDebtorList()
        {
            var lst = dgUpdateDebtorAddress.GetVisibleRows() as ICollection<DebtorClientLocal>;
            if (lst == null || lst.Count == 0)
                return null;
            var lst1 = new DebtorClientLocal[lst.Count];
            var lst2 = new DebtorClientLocal[lst.Count];
            int i = 0;
            foreach (var item in lst)
            {
                var doUpdateName = updName.GetValueOrDefault() && item.DifName;
                var doUpdateAddress = updAddress.GetValueOrDefault() && (item.DifAddress || item.DifAddress2 || item.DifZipCode || item.DifCity);
                var doUpdateIndustryCode = updIndustryCode.GetValueOrDefault() && item.DifIndustryCode;
                var doUpdateCompanyState = updCompanyState.GetValueOrDefault() && item.DifCompanyState;

                if (!doUpdateName && !doUpdateAddress && !doUpdateIndustryCode && !doUpdateCompanyState)
                    continue;

                lst1[i] = StreamingManager.Clone(item) as DebtorClientLocal;
                if (doUpdateName)
                    item._Name = item.NewName;
               
                if (doUpdateAddress)
                {
                    item._Address1 = item.NewAddress;
                    item._Address2 = item.NewAddress2;
                    item._ZipCode = item.NewZipCode;
                    item._City = item.NewCity;
                }
                
                if (doUpdateIndustryCode)
                    item._IndustryCode= item.NewIndustryCode;
                
                if (doUpdateCompanyState)
                    item.CompanyState = item.NewCompanyState;
                
                lst2[i] = item;
                i++;
            }

            if (lst1.Length > 0)
                return api.Update(lst1, lst2);

            return Task.FromResult(ErrorCodes.NoLinesToUpdate);
        }

        Task<ErrorCodes> UpdateCreditorList()
        {
            var lst = dgUpdateDebtorAddress.GetVisibleRows() as ICollection<CreditorClientLocal>;
            if (lst == null || lst.Count == 0)
                return null;
            var lst1 = new CreditorClientLocal[lst.Count];
            var lst2 = new CreditorClientLocal[lst.Count];
            int i = 0;
            foreach (var item in lst)
            {
                var doUpdateName = updName.GetValueOrDefault() && item.DifName;
                var doUpdateAddress = updAddress.GetValueOrDefault() && (item.DifAddress || item.DifAddress2 || item.DifZipCode || item.DifCity);
                var doUpdateIndustryCode = updIndustryCode.GetValueOrDefault() && item.DifIndustryCode;
                var doUpdateCompanyState = updCompanyState.GetValueOrDefault() && item.DifCompanyState;

                if (!doUpdateName && !doUpdateAddress && !doUpdateIndustryCode && !doUpdateCompanyState)
                    continue;

                lst1[i] = StreamingManager.Clone(item) as CreditorClientLocal;
                if (doUpdateName)
                    item._Name = item.NewName;

                if (doUpdateAddress)
                {
                    item._Address1 = item.NewAddress;
                    item._Address2 = item.NewAddress2;
                    item._ZipCode = item.NewZipCode;
                    item._City = item.NewCity;
                }

                if (doUpdateIndustryCode)
                    item._IndustryCode= item.NewIndustryCode;
               
                if (doUpdateCompanyState)
                    item.CompanyState = item.NewCompanyState;

                lst2[i] = item;
                i++;
            }

            if (lst1.Length > 0)
                return api.Update(lst1, lst2);

            return Task.FromResult(ErrorCodes.NoLinesToUpdate);  
        }

        Task<ErrorCodes> UpdateProspectList()
        {
            var lst = dgUpdateDebtorAddress.GetVisibleRows() as ICollection<CrmProspectClientLocal>;
            if (lst == null || lst.Count == 0)
                return null;
            var lst1 = new CrmProspectClientLocal[lst.Count];
            var lst2 = new CrmProspectClientLocal[lst.Count];
            int i = 0;
            foreach (var item in lst)
            {
                var doUpdateName = updName.GetValueOrDefault() && item.DifName;
                var doUpdateAddress = updAddress.GetValueOrDefault() && (item.DifAddress || item.DifAddress2 || item.DifZipCode || item.DifCity);
                var doUpdateIndustryCode = updIndustryCode.GetValueOrDefault() && item.DifIndustryCode;
                var doUpdateCompanyState = updCompanyState.GetValueOrDefault() && item.DifCompanyState;

                if (!doUpdateName && !doUpdateAddress && !doUpdateIndustryCode && !doUpdateCompanyState)
                    continue;

                lst1[i] = StreamingManager.Clone(item) as CrmProspectClientLocal;
                if (doUpdateName)
                    item._Name = item.NewName;

                if (doUpdateAddress)
                {
                    item._Address1 = item.NewAddress;
                    item._Address2 = item.NewAddress2;
                    item._ZipCode = item.NewZipCode;
                    item._City = item.NewCity;
                }

                if (doUpdateIndustryCode)
                    item._IndustryCode= item.NewIndustryCode;

                if (doUpdateCompanyState)
                    item.CompanyState = item.NewCompanyState;
                
                lst2[i] = item;
                i++;
            }

            if (lst1.Length > 0)
                return api.Update(lst1, lst2);

            return Task.FromResult(ErrorCodes.NoLinesToUpdate);
        }

        void SetStatusText(int total, int found, int newRecord)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var foundTxt = Uniconta.ClientTools.Localization.lookup("Available");
            var totalTxt = Uniconta.ClientTools.Localization.lookup("Total");
            var newTxt = Uniconta.ClientTools.Localization.lookup("New");

            foreach (var grp in groups)
            {
                if (grp.Caption == totalTxt)
                    grp.StatusValue = total.ToString();
                else if (grp.Caption == foundTxt)
                    grp.StatusValue = found.ToString();
                else if (grp.Caption == newTxt)
                    grp.StatusValue = newRecord.ToString();
                else
                    grp.StatusValue = string.Empty;
            }
        }
        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var si = dgUpdateDebtorAddress.SelectedItem;
            if (si == null)
                return lookup;
            if (dgUpdateDebtorAddress.CurrentColumn?.Name == "Account")
            {
                if (dgUpdateDebtorAddress.DCtype == 0)
                    lookup.TableType = typeof(Uniconta.DataModel.Debtor);
                else if (dgUpdateDebtorAddress.DCtype == 1)
                    lookup.TableType = typeof(Uniconta.DataModel.Creditor);
                else
                    lookup.TableType = typeof(Uniconta.DataModel.CrmProspect);
            }
            return lookup;
        }
    }

    public class DebtorClientLocal : DebtorClient
    {
        private string _address, _address2, _city, _zipCode, _name, _industryCode;
        private bool _difName, _difAddress, _difAddress2, _difCity, _difZipCode, _difIndustryCode, _difCompanyState;

        public byte _companyState;
        [StringLength(60)]
        [Display(Name = "NewAddress", ResourceType = typeof(DCAccountText))]
        public string NewAddress { get { return _address; } set { _address = value; NotifyPropertyChanged("NewAddress1"); } }

        [StringLength(60)]
        [Display(Name = "NewAddress2", ResourceType = typeof(DCAccountText))]
        public string NewAddress2 { get { return _address2; } set { _address2 = value; NotifyPropertyChanged("NewAddress2"); } }

        [StringLength(12)]
        [Display(Name = "NewZipCode", ResourceType = typeof(DCAccountText))]
        public string NewZipCode { get { return _zipCode; } set { if (_zipCode == value) return; _zipCode = value; NotifyPropertyChanged("NewZipCode"); } }

        [StringLength(30)]
        [Display(Name = "NewCity", ResourceType = typeof(DCAccountText))]
        public string NewCity { get { return _city; } set { _city = value; NotifyPropertyChanged("NewCity"); } }

        [Display(Name = "NewName", ResourceType = typeof(DCAccountText))]
        public string NewName { get { return _name; } set { _name = value; NotifyPropertyChanged("NewName"); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(IndustryCode))]
        [Display(Name = "NewIndustryCode", ResourceType = typeof(DCAccountText))]
        public string NewIndustryCode { get { return _industryCode; } set { _industryCode = value; NotifyPropertyChanged("NewIndustryCode"); } }
      
        [AppEnum(EnumName = "CompanyState")]
        [Display(Name = "NewCompanyState", ResourceType = typeof(DCAccountText))]
        public string NewCompanyState { get { return AppEnums.CompanyState.ToString((int)_companyState); } set { if (value == null) return; _companyState = (byte)AppEnums.CompanyState.IndexOf(value); NotifyPropertyChanged("NewCompanyState"); } }

        [Display(Name = "DifName", ResourceType = typeof(DCAccountText))]
        public bool DifName { get { return _difName; } set { _difName = value; NotifyPropertyChanged("DifName"); } }

        [Display(Name = "DifAddress", ResourceType = typeof(DCAccountText))]
        public bool DifAddress { get { return _difAddress; } set { _difAddress = value; NotifyPropertyChanged("DifAddress"); } }

        [Display(Name = "DifAddress2", ResourceType = typeof(DCAccountText))]
        public bool DifAddress2 { get { return _difAddress2; } set { _difAddress2 = value; NotifyPropertyChanged("DifAddress2"); } }

        [Display(Name = "DifZipCode", ResourceType = typeof(DCAccountText))]
        public bool DifZipCode { get { return _difZipCode; } set { _difZipCode = value; NotifyPropertyChanged("DifZipCode"); } }

        [Display(Name = "DifCity", ResourceType = typeof(DCAccountText))]
        public bool DifCity { get { return _difCity; } set { _difCity = value; NotifyPropertyChanged("DifCity"); } }

        [Display(Name = "DifIndustryCode", ResourceType = typeof(DCAccountText))]
        public bool DifIndustryCode { get { return _difIndustryCode; } set { _difIndustryCode = value; NotifyPropertyChanged("DifIndustryCode"); } }

        [Display(Name = "DifCompanyState", ResourceType = typeof(DCAccountText))]
        public bool DifCompanyState { get { return _difCompanyState; } set { _difCompanyState = value; NotifyPropertyChanged("DifCompanyState"); } }
    }

    public class CreditorClientLocal : CreditorClient
    {
        private string _address, _address2, _city, _zipCode, _name, _industryCode;
        private bool _difName, _difAddress, _difAddress2, _difCity, _difZipCode, _difIndustryCode, _difCompanyState;
        public byte _companyState;
        [StringLength(60)]
        [Display(Name = "NewAddress", ResourceType = typeof(DCAccountText))]
        public string NewAddress { get { return _address; } set { _address = value; NotifyPropertyChanged("NewAddress1"); } }

        [StringLength(60)]
        [Display(Name = "NewAddress2", ResourceType = typeof(DCAccountText))]
        public string NewAddress2 { get { return _address2; } set { _address2 = value; NotifyPropertyChanged("NewAddress2"); } }

        [StringLength(12)]
        [Display(Name = "NewZipCode", ResourceType = typeof(DCAccountText))]
        public string NewZipCode { get { return _zipCode; } set { if (_zipCode == value) return; _zipCode = value; NotifyPropertyChanged("NewZipCode"); } }

        [StringLength(30)]
        [Display(Name = "NewCity", ResourceType = typeof(DCAccountText))]
        public string NewCity { get { return _city; } set { _city = value; NotifyPropertyChanged("NewCity"); } }

        [Display(Name = "NewName", ResourceType = typeof(DCAccountText))]
        public string NewName { get { return _name; } set { _name = value; NotifyPropertyChanged("NewName"); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(IndustryCode))]
        [Display(Name = "NewIndustryCode", ResourceType = typeof(DCAccountText))]
        public string NewIndustryCode { get { return _industryCode; } set { _industryCode = value; NotifyPropertyChanged("NewIndustryCode"); } }
        [AppEnum(EnumName = "CompanyState")]
        [Display(Name = "NewCompanyState", ResourceType = typeof(DCAccountText))]
        public string NewCompanyState { get { return AppEnums.CompanyState.ToString((int)_companyState); } set { if (value == null) return; _companyState = (byte)AppEnums.CompanyState.IndexOf(value); NotifyPropertyChanged("NewCompanyState"); } }

        [Display(Name = "DifName", ResourceType = typeof(DCAccountText))]
        public bool DifName { get { return _difName; } set { _difName = value; NotifyPropertyChanged("DifName"); } }

        [Display(Name = "DifAddress", ResourceType = typeof(DCAccountText))]
        public bool DifAddress { get { return _difAddress; } set { _difAddress = value; NotifyPropertyChanged("DifAddress"); } }

        [Display(Name = "DifAddress2", ResourceType = typeof(DCAccountText))]
        public bool DifAddress2 { get { return _difAddress2; } set { _difAddress2 = value; NotifyPropertyChanged("DifAddress2"); } }

        [Display(Name = "DifZipCode", ResourceType = typeof(DCAccountText))]
        public bool DifZipCode { get { return _difZipCode; } set { _difZipCode = value; NotifyPropertyChanged("DifZipCode"); } }

        [Display(Name = "DifCity", ResourceType = typeof(DCAccountText))]
        public bool DifCity { get { return _difCity; } set { _difCity = value; NotifyPropertyChanged("DifCity"); } }

        [Display(Name = "DifIndustryCode", ResourceType = typeof(DCAccountText))]
        public bool DifIndustryCode { get { return _difIndustryCode; } set { _difIndustryCode = value; NotifyPropertyChanged("DifIndustryCode"); } }

        [Display(Name = "DifCompanyState", ResourceType = typeof(DCAccountText))]
        public bool DifCompanyState { get { return _difCompanyState; } set { _difCompanyState = value; NotifyPropertyChanged("DifCompanyState"); } }
    }

    public class CrmProspectClientLocal : CrmProspectClient
    {
        private string _address, _address2, _city, _zipCode, _name, _industryCode;
        private bool _difName, _difAddress, _difAddress2, _difCity, _difZipCode, _difIndustryCode, _difCompanyState;
        public byte _companyState;
        [StringLength(60)]
        [Display(Name = "NewAddress", ResourceType = typeof(DCAccountText))]
        public string NewAddress { get { return _address; } set { _address = value; NotifyPropertyChanged("NewAddress1"); } }

        [StringLength(60)]
        [Display(Name = "NewAddress2", ResourceType = typeof(DCAccountText))]
        public string NewAddress2 { get { return _address2; } set { _address2 = value; NotifyPropertyChanged("NewAddress2"); } }

        [StringLength(12)]
        [Display(Name = "NewZipCode", ResourceType = typeof(DCAccountText))]
        public string NewZipCode { get { return _zipCode; } set { if (_zipCode == value) return; _zipCode = value; NotifyPropertyChanged("NewZipCode"); } }

        [StringLength(30)]
        [Display(Name = "NewCity", ResourceType = typeof(DCAccountText))]
        public string NewCity { get { return _city; } set { _city = value; NotifyPropertyChanged("NewCity"); } }

        [Display(Name = "NewName", ResourceType = typeof(DCAccountText))]
        public string NewName { get { return _name; } set { _name = value; NotifyPropertyChanged("NewName"); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(IndustryCode))]
        [Display(Name = "NewIndustryCode", ResourceType = typeof(DCAccountText))]
        public string NewIndustryCode { get { return _industryCode; } set { _industryCode = value; NotifyPropertyChanged("NewIndustryCode"); } }
     
        [AppEnum(EnumName = "CompanyState")]
        [Display(Name = "NewCompanyState", ResourceType = typeof(DCAccountText))]
        public string NewCompanyState { get { return AppEnums.CompanyState.ToString((int)_companyState); } set { if (value == null) return; _companyState = (byte)AppEnums.CompanyState.IndexOf(value); NotifyPropertyChanged("NewCompanyState"); } }

        [Display(Name = "DifName", ResourceType = typeof(DCAccountText))]
        public bool DifName { get { return _difName; } set { _difName = value; NotifyPropertyChanged("DifName"); } }

        [Display(Name = "DifAddress", ResourceType = typeof(DCAccountText))]
        public bool DifAddress { get { return _difAddress; } set { _difAddress = value; NotifyPropertyChanged("DifAddress"); } }

        [Display(Name = "DifAddress2", ResourceType = typeof(DCAccountText))]
        public bool DifAddress2 { get { return _difAddress2; } set { _difAddress2 = value; NotifyPropertyChanged("DifAddress2"); } }

        [Display(Name = "DifZipCode", ResourceType = typeof(DCAccountText))]
        public bool DifZipCode { get { return _difZipCode; } set { _difZipCode = value; NotifyPropertyChanged("DifZipCode"); } }

        [Display(Name = "DifCity", ResourceType = typeof(DCAccountText))]
        public bool DifCity { get { return _difCity; } set { _difCity = value; NotifyPropertyChanged("DifCity"); } }

        [Display(Name = "DifIndustryCode", ResourceType = typeof(DCAccountText))]
        public bool DifIndustryCode { get { return _difIndustryCode; } set { _difIndustryCode = value; NotifyPropertyChanged("DifIndustryCode"); } }

        [Display(Name = "DifCompanyState", ResourceType = typeof(DCAccountText))]
        public bool DifCompanyState { get { return _difCompanyState; } set { _difCompanyState = value; NotifyPropertyChanged("DifCompanyState"); } }
    }
}

