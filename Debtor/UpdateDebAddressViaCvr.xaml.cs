using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using Uniconta.Common.Utility;
using Uniconta.API.Service;
using Uniconta.API.System;

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
        public UpdateDebAddressViaCvr(BaseAPI API) : base(API, string.Empty)
        {
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
                    LoadGrid();
                    break;
                case "SaveGrid":
                    SaveGrid();
                    break;
            }
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
            var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
            var debtorList = await api.Query<DebtorClientLocal>(filter);
            if (debtorList == null || debtorList.Length == 0)
                return;
            int counterFound = 0;
            var newDebList = new List<DebtorClientLocal>();
            foreach (var debtor in debtorList)
            {
                var cvr = debtor._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;
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
                    if (Equal(ci.life.name, debtor._Name) && Equal(streetAddress, debtor._Address1) && Equal(address.street2, debtor._Address2) &&
                               Equal(address.zipcode, debtor._ZipCode) && Equal(ci.industrycode?.code, debtor._IndustryCode))
                        continue;

                    var newDebtor = debtor;
                    newDebtor.NewAddress = streetAddress;
                    newDebtor.NewAddress2 = address.street2;
                    newDebtor.NewZipCode = address.zipcode;
                    newDebtor.NewCity = address.cityname;
                    newDebtor.NewName = ci.life.name;
                    if (IndustryCodes != null)
                        newDebtor.NewIndustryCode = IndustryCodes.Get(ci.industrycode?.code)?.KeyStr;
                    else
                        newDebtor.NewIndustryCode = ci.industrycode?.code;
                    newDebList.Add(newDebtor);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(newDebList.Count);
                    busyIndicator.IsBusy = false;
                }
            }
            ClearBusy();
            dgUpdateDebtorAddress.ItemsSource = newDebList;
            SetStatusText(debtorList.Length, counterFound, newDebList.Count);
        }

        async void LoadCreditorList()
        {
            var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
            var lst = await api.Query<CreditorClientLocal>(filter);
            if (lst == null || lst.Length == 0)
                return;
            int counterFound = 0;
            var newCredList = new List<CreditorClientLocal>();
            foreach (var creditor in lst)
            {
                var cvr = creditor._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;
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
                    if (Equal(ci.life.name, creditor._Name) && Equal(streetAddress, creditor._Address1) && Equal(address.street2, creditor._Address2) &&
                               Equal(address.zipcode, creditor._ZipCode) && Equal(ci.industrycode?.code, creditor._IndustryCode))
                        continue;

                    var newCreditor = creditor;
                    newCreditor.NewAddress = streetAddress;
                    newCreditor.NewAddress2 = address.street2;
                    newCreditor.NewZipCode = address.zipcode;
                    newCreditor.NewCity = address.cityname;
                    newCreditor.NewName = ci.life.name;
                    newCreditor.NewIndustryCode = IndustryCodes.Get(ci.industrycode?.code)?.KeyStr;
                    newCredList.Add(newCreditor);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(newCredList.Count);
                    busyIndicator.IsBusy = false;
                }
            }
            ClearBusy();
            dgUpdateDebtorAddress.ItemsSource = newCredList;
            SetStatusText(lst.Length, counterFound, newCredList.Count);
        }

        async void LoadProspectList()
        {
            var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
            var lst = await api.Query<CrmProspectClientLocal>(filter);
            if (lst == null || lst.Length == 0)
                return;
            int counterFound = 0;
            var newProsList = new List<CrmProspectClientLocal>();
            foreach (var prospect in lst)
            {
                var cvr = prospect._LegalIdent;
                if (cvr == null || cvr.Length < 5)
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
                    if (Equal(ci.life.name, prospect._Name) && Equal(streetAddress, prospect._Address1) && Equal(address.street2, prospect._Address2) &&
                            Equal(address.zipcode, prospect._ZipCode) && Equal(ci.industrycode?.code, prospect._IndustryCode))
                        continue;

                    var newProspect = prospect;
                    newProspect.NewAddress = streetAddress;
                    newProspect.NewAddress2 = address.street2;
                    newProspect.NewZipCode = address.zipcode;
                    newProspect.NewCity = address.cityname;
                    newProspect.NewName = ci.life.name;
                    newProspect.NewIndustryCode = IndustryCodes.Get(ci.industrycode?.code)?.KeyStr;
                    newProsList.Add(newProspect);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(newProsList.Count);
                    busyIndicator.IsBusy = false;
                }
            }
            ClearBusy();
            dgUpdateDebtorAddress.ItemsSource = newProsList;
            SetStatusText(lst.Length, counterFound, newProsList.Count);
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
                lst1[i] = StreamingManager.Clone(item) as DebtorClientLocal;
                item._Address1 = item.NewAddress;
                item._Address2 = item.NewAddress2;
                item._ZipCode = item.NewZipCode;
                item._City = item.NewCity;
                item._Name = item.NewName;
                item._IndustryCode= item.NewIndustryCode;
                lst2[i] = item;
                i++;
            }
            return api.Update(lst1, lst2);
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
                lst1[i] = StreamingManager.Clone(item) as CreditorClientLocal;
                item._Address1 = item.NewAddress;
                item._Address2 = item.NewAddress2;
                item._ZipCode = item.NewZipCode;
                item._City = item.NewCity;
                item._IndustryCode= item.NewIndustryCode;
                lst2[i] = item;
                i++;
            }
            return api.Update(lst1, lst2);
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
                lst1[i] = StreamingManager.Clone(item) as CrmProspectClientLocal;
                item._Address1 = item.NewAddress;
                item._Address2 = item.NewAddress2;
                item._ZipCode = item.NewZipCode;
                item._City = item.NewCity;
                item._IndustryCode= item.NewIndustryCode;
                lst2[i] = item;
                i++;
            }
            return api.Update(lst1, lst2);
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
        public string NewCompanyState { get { return AppEnums.CompanyState.ToString((int)_StateOfCompany); } set { if (value == null) return; _StateOfCompany = (byte)AppEnums.CompanyState.IndexOf(value); NotifyPropertyChanged("NewCompanyState"); } }
    }

    public class CreditorClientLocal : CreditorClient
    {
        private string _address, _address2, _city, _zipCode, _name, _industryCode;
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
        public string NewCompanyState { get { return AppEnums.CompanyState.ToString((int)_StateOfCompany); } set { if (value == null) return; _StateOfCompany = (byte)AppEnums.CompanyState.IndexOf(value); NotifyPropertyChanged("CompanyState"); } }
    }

    public class CrmProspectClientLocal : CrmProspectClient
    {
        private string _address, _address2, _city, _zipCode, _name, _industryCode;
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
        public string NewCompanyState { get { return AppEnums.CompanyState.ToString((int)_StateOfCompany); } set { if (value == null) return; _StateOfCompany = (byte)AppEnums.CompanyState.IndexOf(value); NotifyPropertyChanged("CompanyState"); } }
    }
}

