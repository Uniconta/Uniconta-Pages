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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;
using Uniconta.Common;
using UnicontaClient.Pages.Project.TimeManagement;
using Uniconta.ClientTools.Controls;
using System.Globalization;
using static UnicontaClient.Pages.Project.TimeManagement.TMJournalLineHelper;
using Uniconta.Common.Utility;
using DevExpress.Xpf.Core;
using System.Text.RegularExpressions;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwTransportRegistration : ChildWindow
    {

        public List<EmployeeRegistrationLineClient> MileageLst { get; set; }
        public List<EmployeeRegistrationLineClient> MileageReturnLst { get; set; }

        public string Purpose { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        public string Project { get; set; }
        public string RegistrationProject { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.EmpPayrollCategory))]
        public string PayType { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrWorkSpace))]
        public string WorkSpace { get; set; }
        public string PrTask { get; set; }
        public bool Returning { get; set; }
        public string VechicleRegNo { get; set; }
        public double Day1 { get; set; }
        public double Day2 { get; set; }
        public double Day3 { get; set; }
        public double Day4 { get; set; }
        public double Day5 { get; set; }
        public double Day6 { get; set; }
        public double Day7 { get; set; }
        public double Total { get; set; }
        List<Address> addressList;
        Uniconta.DataModel.Employee employee;
        Company company;
        Debtor debtor;
        WorkInstallation installation;
        TMJournalLineClientLocal journalline;
        CrudAPI crudApi;
        SQLCache payrollCache, projectCache;
        SQLTableCache<Uniconta.DataModel.CompanyAddress> companyAddressCache;

        string internalProject;
        EmpPayrollCategory catMileage;

        private bool doCalculate = false;
        private int fromIndex;
        private int toIndex;

        public CwTransportRegistration(TMJournalLineClientLocal tmJournalLine, CrudAPI _crudApi)
        {
            crudApi = _crudApi;
            journalline = tmJournalLine;
            Returning = true;
            company = crudApi.CompanyEntity;
            employee = tmJournalLine.EmployeeRef;
            intializeProperties();
            this.DataContext = this;
            InitializeComponent();
            lePayType.api = leProject.api = leProjectTask.api = leWorkSpace.api = crudApi;
            this.Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Mileage"), Uniconta.ClientTools.Localization.lookup("ProjectRegistration"));

            payrollCache = crudApi.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory));
            projectCache = crudApi.GetCache(typeof(Uniconta.DataModel.Project));
            companyAddressCache = crudApi.GetCache<Uniconta.DataModel.CompanyAddress>();

            debtor = tmJournalLine.ProjectRef?.Debtor;
            txtProjectName.Text = tmJournalLine.ProjectRef?._Name;
            installation = tmJournalLine.ProjectRef?.InstallationRef;

            if (!company.ProjectTask)
            {
                lblProjectTask.Visibility = Visibility.Collapsed;
                leProjectTask.Visibility = Visibility.Collapsed;
            }

            LoadControls();

            lePayType.cacheFilter = new MileagePayrollFilter(payrollCache);
            SetProjectTask();
        }

        void intializeProperties()
        {
            var journalline = this.journalline;
            Project = journalline._Project;
            WorkSpace = journalline._WorkSpace;
            Purpose = journalline._Text;
            VechicleRegNo = employee._VechicleRegNo;

            if (catMileage == null)
            {
                payrollCache = payrollCache ?? crudApi.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory));
                var payrollLst = (EmpPayrollCategory[])payrollCache.GetRecords;
                catMileage = payrollLst.Where(s => s._InternalType == Uniconta.DataModel.InternalType.Mileage).OrderByDescending(s => s._Rate).FirstOrDefault();
            }

            internalProject = catMileage?._InternalProject;
            RegistrationProject = internalProject ?? journalline._Project;
            PrTask = internalProject ?? journalline._Task;

            PayType = catMileage?._Number;
        }

        void LoadControls()
        {
            txtFromName.Text = txtFromAdd1.Text = txtFromAdd2.Text = txtFromZipCode.Text = txtFromCity.Text = string.Empty;
            txtToName.Text = txtToAdd1.Text = txtToAdd2.Text = txtToZipCode.Text = txtToCity.Text = string.Empty;

            var journalline = this.journalline;

            addressList = new List<Address>();

            var compAdr = GetStructuredAddress(company);
            var sbWork = StringBuilderReuse.Create();
            addressList.Add(new Address
            {
                Name = Uniconta.ClientTools.Localization.lookup("Company"),
                AdrName = compAdr.Name,
                Address1 = compAdr.Address1,
                Address2 = compAdr.Address2,
                ZipCode = compAdr.ZipCode,
                City = compAdr.City,
                Country = compAdr.Country,
                IsCompanyAddress = true
            });
            sbWork.Clear();

            companyAddressCache = companyAddressCache ?? crudApi.LoadCache<Uniconta.DataModel.CompanyAddress>().GetAwaiter().GetResult();
            if (companyAddressCache != null)
            {
                foreach (var cAddress in companyAddressCache)
                {
                    sbWork.AppendLineConditional(cAddress._Address2).AppendLineConditional(cAddress._Address3);
                    addressList.Add(new Address 
                    { 
                        Name = string.Concat(Uniconta.ClientTools.Localization.lookup("Company") + " (", cAddress._Name, ")"), 
                        AdrName = cAddress._Name, 
                        Address1 = cAddress._Address1, 
                        Address2 = sbWork.ToString(),
                        ZipCode = cAddress._ZipCode, 
                        City = cAddress._City,
                        Country= cAddress._Country,
                        IsCompanyAddress = true,
                        CompanyAddressNumber = cAddress._Number
                    });
                    sbWork.Clear();
                }
            }

            if (employee != null)
            {
                addressList.Add(new Address 
                { 
                    Name = string.Concat(Uniconta.ClientTools.Localization.lookup("Private")), 
                    AdrName = employee._Name, 
                    Address1 = employee._Address1, 
                    Address2 = employee._Address2, 
                    ZipCode = employee._ZipCode, 
                    City = employee._City,
                    IsPrivateAddress = true,
                });
            }
                sbWork.Clear();

            if (debtor != null)
            {
                addressList.Add(new Address 
                { 
                    Name = string.Concat(Uniconta.ClientTools.Localization.lookup("Debtor")), 
                    AdrName = debtor._Name, 
                    Address1 = debtor._Address1, 
                    Address2 = string.Concat(debtor._Address2, " ", debtor._Address3), 
                    ZipCode = debtor._ZipCode, 
                    City = debtor._City,
                    Country = debtor._Country,
                    DebtorAccount = debtor._Account
                });
            }
            sbWork.Release();
            sbWork = null;

            Address emptyAddress = null;
            if (journalline.RowId != 0 && journalline._RegistrationType == RegistrationType.Mileage && addEmptyAddress)
            {
                emptyAddress = new Address { Name = string.Empty };
                addressList.Add(emptyAddress);
            }

            addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Other") }); ;

            cmbFromAdd.ItemsSource = addressList;
            cmbToAdd.ItemsSource = addressList;
            if (journalline.RowId != 0 && journalline._RegistrationType == RegistrationType.Mileage && addEmptyAddress)
            {
                var index = addressList.IndexOf(emptyAddress);
                cmbFromAdd.SelectedIndex = index;
                cmbToAdd.SelectedIndex = index;
            }
            else
            {
                fromIndex = 0;
                cmbFromAdd.SelectedIndex = fromIndex;

                txtFromName.Text = addressList[0].AdrName;
                txtFromAdd1.Text = addressList[0].Address1?.Replace("\r\n", " ");
                txtFromAdd2.Text = addressList[0].Address2?.Replace("\r\n", " ");
                txtFromZipCode.Text = addressList[0].ZipCode;
                txtFromCity.Text = addressList[0].City;

                toIndex = addressList.Count - 2;
                cmbToAdd.SelectedIndex = toIndex;
                txtToName.Text = addressList[toIndex].AdrName;
                txtToAdd1.Text = addressList[toIndex].Address1?.Replace("\r\n", " ");
                txtToAdd2.Text = addressList[toIndex].Address2?.Replace("\r\n", " ");
                txtToZipCode.Text = addressList[toIndex].ZipCode;
                txtToCity.Text = addressList[toIndex].City;
            }


            txtMonHrs.IsReadOnly = journalline.StatusDay1 == 1 || journalline.StatusDay1 == 2 ? true : false;
            txtTueHrs.IsReadOnly = journalline.StatusDay2 == 1 || journalline.StatusDay2 == 2 ? true : false;
            txtWedHrs.IsReadOnly = journalline.StatusDay3 == 1 || journalline.StatusDay3 == 2 ? true : false;
            txtThuHrs.IsReadOnly = journalline.StatusDay4 == 1 || journalline.StatusDay4 == 2 ? true : false;
            txtFriHrs.IsReadOnly = journalline.StatusDay5 == 1 || journalline.StatusDay5 == 2 ? true : false;
            txtSatHrs.IsReadOnly = journalline.StatusDay6 == 1 || journalline.StatusDay6 == 2 ? true : false;
            txtSunHrs.IsReadOnly = journalline.StatusDay7 == 1 || journalline.StatusDay7 == 2 ? true : false;

            txtMonHrs.Background = SetBackGroundColor(txtMonHrs, journalline.StatusDay1);
            txtTueHrs.Background = SetBackGroundColor(txtTueHrs, journalline.StatusDay2);
            txtWedHrs.Background = SetBackGroundColor(txtWedHrs, journalline.StatusDay3);
            txtThuHrs.Background = SetBackGroundColor(txtThuHrs, journalline.StatusDay4);
            txtFriHrs.Background = SetBackGroundColor(txtFriHrs, journalline.StatusDay5);
            txtSatHrs.Background = SetBackGroundColor(txtSatHrs, journalline.StatusDay6);
            txtSunHrs.Background = SetBackGroundColor(txtSunHrs, journalline.StatusDay7);

            doCalculate = true;
            CalulateMileage();
            CalculateTotal();
        }

        public class MileagePayrollFilter : SQLCacheFilter
        {
            public MileagePayrollFilter(SQLCache cache) : base(cache) { }
            public override bool IsValid(object rec)
            {
                var pay = ((Uniconta.DataModel.EmpPayrollCategory)rec);
                return (pay._InternalType == Uniconta.DataModel.InternalType.Mileage);
            }
        }

        SolidColorBrush SetBackGroundColor(NumericUpDownEditor txtHours, int dayStatus)
        {
            var SolidColorBrush = new SolidColorBrush(Colors.White);

            if (ApplicationThemeHelper.ApplicationThemeName == Theme.MetropolisDarkName)
                SolidColorBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF6E6E6E"));
            if (dayStatus == 1)
                SolidColorBrush = new SolidColorBrush(Colors.Yellow);
            else if (dayStatus == 2)
                SolidColorBrush = new SolidColorBrush(Colors.Green);
            return SolidColorBrush;
        }

        async void SetProjectTask()
        {
            if (crudApi.CompanyEntity.ProjectTask)
            {
                if (Project != null && internalProject == null)
                {
                    var project = (Uniconta.DataModel.Project)projectCache?.Get(Project);
                    var tasks = project.Tasks ?? await project.LoadTasks(crudApi);
                    leProjectTask.ItemsSource = tasks?.Where(s => s.Ended == false && (WorkSpace == null || s._WorkSpace == WorkSpace));
                }
                else
                {
                    leProjectTask.ItemsSource = null;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (!Validate())
            {
                Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), string.Format("{0} & {1}", Uniconta.ClientTools.Localization.lookup("Purpose"), Uniconta.ClientTools.Localization.lookup("VechicleRegNo"))),
                  Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            MileageLst = new List<EmployeeRegistrationLineClient>();
            MileageReturnLst = Returning ? new List<EmployeeRegistrationLineClient>() : null;
            for (int day = 1; day <= 7; day++)
            {
                double dayValue = GetDayValue(day);

                var Mileage = CreateMileageEntry(day, dayValue);
                MileageLst.Add(Mileage);

                if (Returning)
                {
                    var MileageReturn = CreateReturnMileageEntry(Mileage);
                    MileageReturnLst.Add(MileageReturn);
                }
            }

            PrTask = internalProject == null ? leProjectTask.Text : null;
            SetDialogResult(true);
        }

        bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Purpose) || string.IsNullOrWhiteSpace(VechicleRegNo))
                return false;
            return true;
        }

        private EmployeeRegistrationLineClient CreateMileageEntry(int day, double dayValue)
        {
            var mileage = new EmployeeRegistrationLineClient();
            mileage.SetMaster(crudApi.CompanyEntity);
            mileage._Project = Project;
            mileage._Employee = employee._Number;
            mileage._VechicleRegNo = employee._VechicleRegNo;
            mileage._Text = Purpose;

            SetFromToDetails(mileage);

            mileage._Date = journalline.Date.AddDays(day - 1);
            mileage._Qty = dayValue;

            return mileage;
        }

        private EmployeeRegistrationLineClient CreateReturnMileageEntry(EmployeeRegistrationLineClient mileage)
        {
            var mileageReturn = new EmployeeRegistrationLineClient();
            mileageReturn.SetMaster(crudApi.CompanyEntity);
            mileageReturn._Project = Project;
            mileageReturn._Employee = employee._Number;
            mileageReturn._VechicleRegNo = employee._VechicleRegNo;
            mileageReturn._Text = Purpose;
            mileageReturn._Date = mileage._Date;
            mileageReturn._Qty = mileage._Qty;

            SwapFromToDetails(mileage, mileageReturn);

            return mileageReturn;
        }

        private void SetFromToDetails(EmployeeRegistrationLineClient mileage)
        {
            mileage._FromName = txtFromName.Text;
            mileage._FromAddress1 = txtFromAdd1.Text;
            mileage._FromAddress2 = txtFromAdd2.Text;
            mileage._FromZipCode = txtFromZipCode.Text;
            mileage._FromCity = txtFromCity.Text;
            mileage._FromCountry = addressList[fromIndex].Country;
            mileage._FromWork = addressList[fromIndex].IsCompanyAddress;
            mileage._FromHome = addressList[fromIndex].IsPrivateAddress;
            mileage._FromAccount = addressList[fromIndex].DebtorAccount;
            mileage._FromCompanyAddress = addressList[fromIndex].CompanyAddressNumber;

            mileage._ToName = txtToName.Text;
            mileage._ToAddress1 = txtToAdd1.Text;
            mileage._ToAddress2 = txtToAdd2.Text;
            mileage._ToZipCode = txtToZipCode.Text;
            mileage._ToCity = txtToCity.Text;
            mileage._ToCountry = addressList[toIndex].Country;
            mileage._ToWork = addressList[toIndex].IsCompanyAddress;
            mileage._ToHome = addressList[toIndex].IsPrivateAddress;
            mileage._ToAccount = addressList[toIndex].DebtorAccount;
            mileage._ToCompanyAddress = addressList[toIndex].CompanyAddressNumber;
        }

        private void SwapFromToDetails(EmployeeRegistrationLineClient fromMileage, EmployeeRegistrationLineClient toMileage)
        {
            toMileage._FromName = fromMileage._ToName;
            toMileage._FromAddress1 = fromMileage._ToAddress1;
            toMileage._FromAddress2 = fromMileage._ToAddress2;
            toMileage._FromZipCode = fromMileage._ToZipCode;
            toMileage._FromCity = fromMileage._ToCity;
            toMileage._FromWork = fromMileage._ToWork;
            toMileage._FromHome = fromMileage._ToHome;
            toMileage._FromAccount = fromMileage._ToAccount;
            toMileage._FromCompanyAddress = fromMileage._ToCompanyAddress;

            toMileage._ToName = fromMileage._FromName;
            toMileage._ToAddress1 = fromMileage._FromAddress1;
            toMileage._ToAddress2 = fromMileage._FromAddress2;
            toMileage._ToZipCode = fromMileage._FromZipCode;
            toMileage._ToCity = fromMileage._FromCity;
            toMileage._ToWork = fromMileage._FromWork;
            toMileage._ToHome = fromMileage._FromHome;
            toMileage._ToAccount = fromMileage._FromAccount;
            toMileage._ToCompanyAddress = fromMileage._FromCompanyAddress;
        }

        private double GetDayValue(int day)
        {
            switch (day)
            {
                case 1: return Day1;
                case 2: return Day2;
                case 3: return Day3;
                case 4: return Day4;
                case 5: return Day5;
                case 6: return Day6;
                case 7: return Day7;
                default: return 0;
            }
        }

        double total = 0d;
        private void txtThuHrs_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            CalculateTotal();
        }

        private void chkReturning_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            CalculateTotal();
        }

        void CalculateTotal()
        {
            total = Day1 + Day2 + Day3 + Day4 + Day5 + Day6 + Day7;
            var calcmileageTotal = Returning ? total * 2 : total;
            txtMileageBal.EditValue = calcmileageTotal.ToString();
        }

        private void cmb1_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            fromIndex = cmbFromAdd.SelectedIndex;
            var selectedAddress = cmbFromAdd.SelectedItem as Address;
            if (selectedAddress != null)
            {
                txtFromAdd1.Text = selectedAddress.Address1?.Replace("\r\n", " ");
                txtFromAdd2.Text = selectedAddress.Address2?.Replace("\r\n", " ");
                txtFromName.Text = selectedAddress.AdrName;
                txtFromZipCode.Text = selectedAddress.ZipCode;
                txtFromCity.Text = selectedAddress.City;
                CalulateMileage();
            }
        }

        private void cmb2_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            toIndex = cmbToAdd.SelectedIndex;
            var selectedAddress = cmbToAdd.SelectedItem as Address;
            if (selectedAddress != null)
            {
                txtToAdd1.Text = selectedAddress.Address1?.Replace("\r\n", " "); ;
                txtToAdd2.Text = selectedAddress.Address2?.Replace("\r\n", " "); ;
                txtToName.Text = selectedAddress.AdrName;
                txtToZipCode.Text = selectedAddress.ZipCode;
                txtToCity.Text = selectedAddress.City;
                CalulateMileage();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CalulateMileage();
        }

        void CalulateMileage()
        {
            if (!doCalculate || string.IsNullOrWhiteSpace(string.Concat(txtFromAdd1.Text, txtFromAdd2.Text)) || string.IsNullOrWhiteSpace(string.Concat(txtToAdd1.Text, txtToAdd2.Text)))
                return;
            string fromAddress = string.Format("{0} {1} {2} {3} {4}", txtFromName.Text, txtFromAdd1.Text, txtFromAdd2.Text, txtFromCity.Text, txtFromZipCode.Text);
            string toAddress = string.Format("{0} {1} {2} {3} {4}", txtToName.Text, txtToAdd1.Text, txtToAdd2.Text, txtToCity.Text, txtToZipCode.Text);
            double distance = 0d;
            distance = GoogleMaps.GetDistance(fromAddress, toAddress, (bool)chkAvdFerr.IsChecked);

            double dist = distance;
            if (!txtMonHrs.IsReadOnly && journalline.Day1 > 0) txtMonHrs.EditValue = dist;
            if (!txtTueHrs.IsReadOnly && journalline.Day2 > 0) txtTueHrs.EditValue = dist;
            if (!txtWedHrs.IsReadOnly && journalline.Day3 > 0) txtWedHrs.EditValue = dist;
            if (!txtThuHrs.IsReadOnly && journalline.Day4 > 0) txtThuHrs.EditValue = dist;
            if (!txtFriHrs.IsReadOnly && journalline.Day5 > 0) txtFriHrs.EditValue = dist;
            if (!txtSatHrs.IsReadOnly && journalline.Day6 > 0) txtSatHrs.EditValue = dist;
            if (!txtSunHrs.IsReadOnly && journalline.Day7 > 0) txtSunHrs.EditValue = dist;
            CalculateTotal();
        }

        Address GetStructuredAddress(Company company)
        {
            for (int adrCnt = 3; adrCnt > 1; adrCnt--)
            {
                string adr = adrCnt == 3 ? company._Address3 : company._Address2;

                if (adr == null || adr.Length < 8)
                    continue;

                var zipCode = Regex.Replace(adr.Substring(0, 8), "[^0-9]", "");

                int zipLen = GetZipLength(company._CountryId);

                if (zipCode.Length != zipLen)
                    continue;

                var idx = adr.IndexOf(zipCode);
                if (idx == -1)
                    idx = adr.IndexOf(zipCode.Insert(3, " ")); //Sweden

                int extra = 0;
                var city = adr.Substring(idx + zipLen + extra).Trim().TrimEnd(',').TrimEnd(';');
                city = Regex.Replace(city, "[,;]", "");

                var adrNEW = adrCnt == 3 ? company._Address2 : company._Address3;

                return new Address
                {
                    Name = company._Name,
                    Address1 = company._Address1,
                    Address2 = adrNEW,
                    City = city,
                    ZipCode = zipCode,
                };
            }

            return new Address
            {
                Name = company._Name,
                Address1 = company._Address1,
                Address2 = string.Concat(company._Address2, company._Address3),
                City = null,
                ZipCode = null,
            };
        }

        int GetZipLength(CountryCode countryCode)
        {
            switch (countryCode)
            {
                case CountryCode.Denmark:
                case CountryCode.Greenland:
                case CountryCode.Norway: return 4;
                case CountryCode.Sweden:
                case CountryCode.Finland: return 5;
                case CountryCode.FaroeIslands:
                case CountryCode.Iceland: return 3;
                default: return 0;
            }
        }



        internal class Address
        {
            public string Name { get; set; }
            public string AdrName { get; set; }
            public string Address1 { get; set; }
            public string Address2 { get; set; }
            public string ZipCode { get; set; }
            public string City { get; set; }
            public CountryCode Country { get; set; }
            public string DebtorAccount { get; set; }
            public string CompanyAddressNumber { get; set; }
            public bool IsCompanyAddress { get; set; }
            public bool IsPrivateAddress { get; set; }
        }

        bool addEmptyAddress = true;
        private void leProject_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            projectCache = projectCache ?? crudApi.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Project));
            var project = (Uniconta.DataModel.Project)projectCache?.Get(id) as ProjectClient;
            Project = project.Number;
            SetProjectTask();

            if (project != null)
            {
                RegistrationProject = internalProject ?? project.Number;

                txtProjectName.Text = project._Name;
                debtor = project.Debtor;
                installation = project.InstallationRef;
                if (journalline.RowId != 0 && journalline._RegistrationType == RegistrationType.Mileage)
                    addEmptyAddress = false;
                LoadControls();
                if (!txtMonHrs.IsReadOnly)
                {
                    txtMonHrs.Text = "0";
                    Day1 = 0d;
                }
                if (!txtTueHrs.IsReadOnly)
                {
                    txtTueHrs.Text = "0";
                    Day2 = 0d;
                }
                if (!txtWedHrs.IsReadOnly)
                {
                    txtWedHrs.Text = "0";
                    Day3 = 0d;
                }
                if (!txtThuHrs.IsReadOnly)
                {
                    txtThuHrs.Text = "0";
                    Day4 = 0d;
                }
                if (!txtFriHrs.IsReadOnly)
                {
                    txtFriHrs.Text = "0";
                    Day5 = 0d;
                }
                if (!txtSatHrs.IsReadOnly)
                {
                    txtSatHrs.Text = "0";
                    Day6 = 0d;
                }
                if (!txtSunHrs.IsReadOnly)
                {
                    txtSunHrs.Text = "0";
                    Day7 = 0d;
                }
                CalculateTotal();
                CalulateMileage();
            }
        }

        protected async void LoadCacheInBackGround()
        {
            projectCache = projectCache ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
            payrollCache = payrollCache ?? await crudApi.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory)).ConfigureAwait(false);
            companyAddressCache = companyAddressCache ?? await crudApi.LoadCache<Uniconta.DataModel.CompanyAddress>().ConfigureAwait(false);
        }

        private void leWorkSpace_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            WorkSpace = id;
            SetProjectTask();
        }

        private void lePayType_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            PayType = id;

            if (PayType != null)
            {
                payrollCache = payrollCache ?? crudApi.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory));
                catMileage = (Uniconta.DataModel.EmpPayrollCategory)payrollCache.Get(PayType);
                internalProject = catMileage?._InternalProject;
                RegistrationProject = internalProject ?? Project;
            }
        }

        private void btnFromZipCode_Click(object sender, RoutedEventArgs e)
        {
            var location = txtFromAdd1.Text + "+" + txtFromAdd2.Text + "+" + txtFromZipCode.Text + "+" + txtFromCity.Text;
            Utility.OpenGoogleMap(location);
        }

        private void btnToZipCode_Click(object sender, RoutedEventArgs e)
        {
            var location = txtToAdd1.Text + "+" + txtToAdd2.Text + "+" + txtToZipCode.Text + "+" + txtToCity.Text;
            Utility.OpenGoogleMap(location);
        }

    }
}
