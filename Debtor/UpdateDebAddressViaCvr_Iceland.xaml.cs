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
using Newtonsoft.Json;
using System.Net.Http;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UpdateDebAddressViaCvrGrid_Iceland : CorasauDataGridClient
    {
        public override Type TableType { get { return GetTableType(); } }
        public override bool Readonly { get { return false; } }
        public override bool CanDelete { get { return false; } }
        public override bool IsAutoSave { get { return false; } }

        public int DCtype;

        Type GetTableType()
        {
            if (DCtype == 0)
                return typeof(DebtorClientLocal_Iceland);
            else
                return null;
        }
    }
    public partial class UpdateDebAddressViaCvr_Iceland : GridBasePage
    {
        public UpdateDebAddressViaCvr_Iceland(BaseAPI API) : base(API, string.Empty)
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
            localMenu.dataGrid = dgUpdateDebtorAddress_Iceland;
            SetRibbonControl(localMenu, dgUpdateDebtorAddress_Iceland);
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
                    dgUpdateDebtorAddress_Iceland.RemoveFocusedRowFromGrid();
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
                        dgUpdateDebtorAddress_Iceland.DCtype = 0;
                        SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("Debtor"), ": ", Uniconta.ClientTools.Localization.lookup("UpdateAddress")));
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

        public virtual void LoadGrid()
        {
            SetBusy();
            if (dgUpdateDebtorAddress_Iceland.DCtype == 0)
                LoadDebtorList();
            dgUpdateDebtorAddress_Iceland.Visibility = Visibility.Visible;
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
            return s1 == s2;
        }

        public virtual async void LoadDebtorList()
        {
            var filter = new[] { PropValuePair.GenereteWhereElements("LegalIdent", typeof(string), "!null") };
            var debtorList = await api.Query<DebtorClientLocal_Iceland>(filter);
            if (debtorList == null || debtorList.Length == 0)
                return;
            int counterFound = 0;
            var newDebList = new List<DebtorClientLocal_Iceland>();
            CompanyCloud cc = new CompanyCloud();
            foreach (var debtor in debtorList)
            {
                var cvr = debtor._LegalIdent;
                if (cvr == null || cvr.Length < 5)
                    continue;
                busyIndicator.IsBusy = true;
                CompanyInfoIceland ci = null;
                try
                {
                    ci = await cc.GetIslandCompanyInfo(cvr);
                }
                catch { }

                if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                {
                    counterFound++;
                    var address = ci.address;
                    var streetAddress = address.CompleteStreet;
                    if (Equal(ci.life.name, debtor._Name) && Equal(streetAddress, debtor._Address1) && Equal(address.street2, debtor._Address2) &&
                               Equal(address.zipcode, debtor._ZipCode) && ci._invoiceinxml == debtor._InvoiceInXML && Equal(debtor.VatNumber, ci.vat) &&
                               Equal(ci.industrycode?.code, debtor._IndustryCode) &&
                               Equal(ci.companystatus.StatusCode().ToString(), debtor._StateOfCompany.ToString()))
                        continue;

                    var newDebtor = debtor;
                    newDebtor.NewAddress = streetAddress;
                    newDebtor.NewAddress2 = address.street2;
                    newDebtor.NewZipCode = address.zipcode;
                    newDebtor.NewCity = address.cityname;
                    newDebtor.NewName = ci.life.name;
                    newDebtor.NewVatNumber = ci.vat;

                    if (ci._invoiceinxml)
                        newDebtor.NewInvoiceInXML = "Rafrænn reikningur";
                    else
                        newDebtor.NewInvoiceInXML = "Enginn";
                    if (ci.industrycode != null)
                        newDebtor.NewIndustryCode = ci.industrycode.code;
                    newDebtor.NewCompanyState = ci.companystatus.StatusCode().ToString();
                    newDebList.Add(newDebtor);
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(newDebList.Count);
                    busyIndicator.IsBusy = false;
                }
            }
            ClearBusy();
            dgUpdateDebtorAddress_Iceland.ItemsSource = newDebList;
            SetStatusText(debtorList.Length, counterFound, newDebList.Count);
        }

        async void SaveGrid()
        {
            Task<ErrorCodes> t = null;
            SetBusy();
            if (dgUpdateDebtorAddress_Iceland.DCtype == 0)
                t = UpdateDebtorList();
            if (t != null)
            {
                var err = await t;
                ClearBusy();
                UtilDisplay.ShowErrorCode(err);
                dgUpdateDebtorAddress_Iceland.ItemsSource = null;
            }
            else
                ClearBusy();
        }

        public virtual Task<ErrorCodes> UpdateDebtorList()
        {
            var lst = dgUpdateDebtorAddress_Iceland.GetVisibleRows() as ICollection<DebtorClientLocal_Iceland>;
            if (lst == null || lst.Count == 0)
                return null;
            var lst1 = new DebtorClientLocal_Iceland[lst.Count];
            var lst2 = new DebtorClientLocal_Iceland[lst.Count];
            int i = 0;
            foreach (var item in lst)
            {
                lst1[i] = StreamingManager.Clone(item) as DebtorClientLocal_Iceland;
                item._Address1 = item.NewAddress;
                item._Address2 = item.NewAddress2;
                item._ZipCode = item.NewZipCode;
                item._City = item.NewCity;
                item._Name = item.NewName;
                item._InvoiceInXML = item.NewInvoiceInXML != "Enginn";
                item._IndustryCode = item.NewIndustryCode;
                item._VatNumber = item.NewVatNumber;
                item.CompanyState = setats(item.NewCompanyState);
                lst2[i] = item;
                i++;
            }
            return api.Update(lst1, lst2);
        }

        private string setats(string value)
        {
            switch (value)
            {
                case "Virkt": return "0";
                case "Félagsslit": return "1";
                case "Gjaldþrota": return "2";
                case "Samruni": return "6";
                case "Óskilgreint": return "4";
                default: return "";
            }
        }

        public void SetStatusText(int total, int found, int newRecord)
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

        internal class CompanyCloud
        {
            private HttpClient httpClient = null;

            internal CompanyCloud()
            {
                httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://functionapp1net20250602100734.azurewebsites.net/api/getComp");
            }

            internal async Task<CompanyInfoIceland> GetIslandCompanyInfo(string cvrNumber)
            {
                CompanyCloudData ccData = JsonConvert.DeserializeObject<CompanyCloudData>(await httpClient.GetAsync("?cvrNumber=" + cvrNumber).Result.Content.ReadAsStringAsync());
                if (ccData == null)
                    return null;

                AddressCompany addressCompany = new AddressCompany();
                addressCompany.street = ccData.Street?.Trim();
                addressCompany.zipcode = ccData.Zipcode?.Trim();
                addressCompany.cityname = ccData.City?.Trim();
                Life life = new Life
                {
                    name = ccData.Name
                };
                CompanyInfoIceland companyInfo = new CompanyInfoIceland();
                companyInfo.address = addressCompany;
                companyInfo.life = life;
                companyInfo.vat = ccData.VAT;
                companyInfo._invoiceinxml = ccData.UniMsg != "";
                if (!string.IsNullOrEmpty(ccData.Isat))
                {
                    companyInfo.industrycode = new Industrycode();
                    companyInfo.industrycode.code = ccData.Isat;
                }
                companyInfo.companystatus = new Companystatus();
                companyInfo.companystatus.text = ccData.CompanyStat;
                return companyInfo;
            }
        }
    }

    public class DebtorClientLocal_Iceland : DebtorClient
    {
        private string _address, _address2, _city, _zipCode, _name, _industryCode;
        private new string _InvoiceInXML, _VatNumber;
        private string _CompanyState;

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

        [StringLength(20)]
        [Display(Name = "NewInvoiceInXML", ResourceType = typeof(DebCred_AccountText))]
        public string NewInvoiceInXML { get { return _InvoiceInXML; } set { _InvoiceInXML = value; NotifyPropertyChanged("NewInvoiceInXML"); } }

        [StringLength(20)]
        [Display(Name = "NewVatNumber", ResourceType = typeof(DebCred_AccountText))]
        public string NewVatNumber { get { return _VatNumber; } set { _VatNumber = value; NotifyPropertyChanged("NewVatNumber"); } }

        [Display(Name = "NewName", ResourceType = typeof(DCAccountText))]
        public string NewName { get { return _name; } set { _name = value; NotifyPropertyChanged("NewName"); } }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(IndustryCode))]
        [Display(Name = "NewIndustryCode", ResourceType = typeof(DCAccountText))]
        public string NewIndustryCode { get { return _industryCode; } set { _industryCode = value; NotifyPropertyChanged("NewIndustryCode"); } }

        [StringLength(20)]
        [Display(Name = "NewCompanyState", ResourceType = typeof(DebCred_AccountText))]
        public string NewCompanyState { get { return _CompanyState; } set { _CompanyState = states(value); NotifyPropertyChanged("NewCompanyState"); } }

        private string states(string value)
        {
            switch (value)
            {
                case "0": return "Virkt";
                case "1": return "Félagsslit";
                case "2": return "Gjaldþrota";
                case "6": return "Samruni";
                case "4": return "Óskilgreint";
                default: return "Óþekkt";
            }
        }
    }

    public class DebCred_AccountText : DCAccountText
    {
        public static string NewInvoiceInXML => string.Format(Uniconta.ClientTools.Localization.lookup("NewOBJ"), Uniconta.ClientTools.Localization.lookup("einvoice"));
        public static string NewVatNumber => string.Format(Uniconta.ClientTools.Localization.lookup("NewOBJ"), Uniconta.ClientTools.Localization.lookup("VatNumber"));
        public static string NewCompanyState => string.Format(Uniconta.ClientTools.Localization.lookup("NewOBJ"), Uniconta.ClientTools.Localization.lookup("CompanyState"));
    }

    public class CompanyCloudData : RSK_companyData
    {
        public string UniMsg { get; set; }
        public string Isat { get; set; }
    }

    public class CompanyInfoIceland : CompanyInfo
    {
        public bool _invoiceinxml { get; set; }
    }
}