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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwTransportRegistration : ChildWindow
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public string Purpose { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Project))]
        public string Project { get; set; }
        public string RegistrationProject { get; set; }
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.EmpPayrollCategory))]
        public string PayType { get; set; }
        //string _ProjectName;
        //public string ProjectName { get { return _ProjectName; } }
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
        public bool ExistingTransportJrnlLine { get; set; }
        List<Address> addressList;
        Uniconta.DataModel.Employee employee;
        Company company;
        Debtor debtor;
        WorkInstallation installation;
        TMJournalLineClientLocal journalline;
        CrudAPI crudApi;
        bool addMileage = false;
        double mileageTotal;
        private bool doCalculate = false;
        private string EMPTYADDRESS = string.Concat("<", Uniconta.ClientTools.Localization.lookup("address")+">");

        public CwTransportRegistration(TMJournalLineClientLocal tmJournalLine, CrudAPI _crudApi, double mileageTotal, bool returning, bool _addMileage = false)
        {
            crudApi = _crudApi;
            journalline = tmJournalLine;
            Returning = returning;
            intializeProperties();
            this.DataContext = this;
            InitializeComponent();
            lePayType.api = leProject.api = crudApi;
            this.Title = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Mileage"), Uniconta.ClientTools.Localization.lookup("ProjectRegistration"));
            employee = tmJournalLine.EmployeeRef;
            company = crudApi.CompanyEntity;
            if (journalline.RowId != 0 && journalline._RegistrationType == RegistrationType.Mileage)
                chkReturning.IsEnabled = false;
            else
            {
                debtor = tmJournalLine.ProjectRef?.Debtor;
                txtProjectName.Text = tmJournalLine.ProjectRef?._Name;
                installation = tmJournalLine.ProjectRef?.InstallationRef;
            }
            addMileage = _addMileage;
            this.mileageTotal = mileageTotal;
            LoadControls();
            this.DataContext = this;
        }

        void LoadControls()
        {
            txtFromAdd.Text = string.Empty;
            txtToAdd.Text = string.Empty;
            var journalline = this.journalline;

            addressList = new List<Address>();

            var sbWork = StringBuilderReuse.Create();
            sbWork.AppendLineConditional(company._Name).AppendLineConditional(company._Address1).AppendLineConditional(company._Address2).AppendLineConditional(company._Address3);
            addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Company"), ContactAddress = sbWork.ToString() });
            sbWork.Clear();

            if (employee != null)
            {
                sbWork.AppendLineConditional(employee._Name).AppendLineConditional(employee._Address1).AppendLineConditional(employee._Address2);
                if (employee._ZipCode != null)
                    sbWork.Append(employee._ZipCode).Append(' ');
                sbWork.Append(employee._City);
                addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Private"), ContactAddress = sbWork.ToString() });
            }
            sbWork.Clear();

            if (debtor != null)
            {
                sbWork.AppendLineConditional(debtor._Name).AppendLineConditional(debtor._Address1).AppendLineConditional(debtor._Address2).AppendLineConditional(debtor._Address3);
                if (debtor._ZipCode != null)
                    sbWork.Append(debtor._ZipCode).Append(' ');
                sbWork.Append(debtor._City);
                addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Debtor"), ContactAddress = sbWork.ToString() });
            }
            else if (installation != null)
            {
                sbWork.AppendLineConditional(installation._Name).AppendLineConditional(installation._Address1).AppendLineConditional(installation._Address2).AppendLineConditional(installation._Address3);
                if (installation._ZipCode != null)
                    sbWork.Append(installation._ZipCode).Append(' ');
                sbWork.Append(installation._City);
                addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Debtor"), ContactAddress = sbWork.ToString() });
            }
            sbWork.Release();
            sbWork = null;

            Address emptyAddress = null;
            if (journalline.RowId != 0 && journalline._RegistrationType == RegistrationType.Mileage && addEmptyAddress)
            {
                emptyAddress = new Address { Name = string.Empty, ContactAddress = string.Empty };
                addressList.Add(emptyAddress);
            }

            addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Other"), ContactAddress = EMPTYADDRESS }); ;

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
                cmbFromAdd.SelectedIndex = 0;
                cmbToAdd.SelectedIndex = 1;
                txtFromAdd.Text = addressList.Count >= 1 ? addressList[0].ContactAddress : string.Empty;

                if (addressList.Count == 4)
                {
                    cmbToAdd.SelectedIndex = 2;
                    txtToAdd.Text = addressList[2].ContactAddress;
                }
                else if (addressList.Count >= 4)
                {
                    cmbToAdd.SelectedIndex = 3;
                    txtToAdd.Text = addressList[3].ContactAddress;
                }
            }


            if (string.IsNullOrEmpty(txtFromAdd.Text))
                txtFromAdd.Text = journalline.AddressFrom;
            if (string.IsNullOrEmpty(txtToAdd.Text))
                txtToAdd.Text = journalline.AddressTo;
        
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
            if (addMileage)
                CalulateMileage();
            else
                mileageTotal = mileageTotal - journalline.Total;

            CalculateTotal();
        }

        SolidColorBrush SetBackGroundColor(NumericUpDownEditor txtHours, int dayStatus)
        {
            var SolidColorBrush = new SolidColorBrush(Colors.White);
#if !SILVERLIGHT
            if (ApplicationThemeHelper.ApplicationThemeName == Theme.MetropolisDarkName)
                SolidColorBrush = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF6E6E6E"));
#endif
            if (dayStatus == 1)
                SolidColorBrush = new SolidColorBrush(Colors.Yellow);
            else if (dayStatus == 2)
                SolidColorBrush = new SolidColorBrush(Colors.Green);
            return SolidColorBrush;
        }

        async void intializeProperties()
        {
            var payrollCategoryLst = await crudApi.LoadCache<Uniconta.DataModel.EmpPayrollCategory>();
            var payrollCategory = payrollCategoryLst.Where(s => s._InternalType == Uniconta.DataModel.InternalType.Mileage).OrderByDescending(s => s._Rate).FirstOrDefault(); //TODO:Mangler håndtering af Høj/Lav sats

            var journalline = this.journalline;
            if (journalline.RowId != 0 && journalline._RegistrationType == RegistrationType.Mileage)
                Project = string.Empty;
            else
                Project = journalline._Project;
            RegistrationProject = payrollCategory?._InternalProject ?? journalline._Project;
            PayType = payrollCategory?._Number;
            if (journalline._RegistrationType == RegistrationType.Mileage)
            {
                Purpose = journalline.Text;
                VechicleRegNo = journalline._VechicleRegNo;
                FromAddress = journalline._AddressFrom;
                ToAddress = journalline._AddressTo;
                Day1 = journalline.Day1;
                Day2 = journalline.Day2;
                Day3 = journalline.Day3;
                Day4 = journalline.Day4;
                Day5 = journalline.Day5;
                Day6 = journalline.Day6;
                Day7 = journalline.Day7;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            if (journalline._RegistrationType == RegistrationType.Mileage)
                ExistingTransportJrnlLine = true;
            FromAddress = txtFromAdd.Text;
            ToAddress = txtToAdd.Text;
            this.DialogResult = true;
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
            txtTotal.Text = total.ToString();

            var calcmileageTotal = Returning ? mileageTotal + total*2 : mileageTotal + total;
            txtMileageBal.EditValue = calcmileageTotal.ToString();
        }

        private void cmb1_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var selectedAddress = cmbFromAdd.SelectedItem as Address;
            if (selectedAddress != null)
            {
                txtFromAdd.Text = selectedAddress.ContactAddress;
                CalulateMileage();
            }
        }

        private void cmb2_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var selectedAddress = cmbToAdd.SelectedItem as Address;
            if (selectedAddress != null)
            {
                txtToAdd.Text = selectedAddress.ContactAddress;
                CalulateMileage();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CalulateMileage();
        }

        void CalulateMileage()
        {
            if (!doCalculate)
                return;

            double distance = 0d;
            if (string.IsNullOrWhiteSpace(txtFromAdd.Text) || string.IsNullOrWhiteSpace(txtToAdd.Text) || txtFromAdd.Text == EMPTYADDRESS || txtToAdd.Text == EMPTYADDRESS)
            { }
#if !SILVERLIGHT
            else
                distance = GoogleMaps.GetDistance(txtFromAdd.Text, txtToAdd.Text, (bool)chkAvdFerr.IsChecked);
#endif
            object dist = distance;
            if (!addMileage)
            {
                if (!txtMonHrs.IsReadOnly)
                    txtMonHrs.EditValue = dist;
                else if (!txtTueHrs.IsReadOnly)
                    txtTueHrs.EditValue = dist;
                else if (!txtWedHrs.IsReadOnly)
                    txtWedHrs.EditValue = dist;
                else if (!txtThuHrs.IsReadOnly)
                    txtThuHrs.EditValue = dist;
                else if (!txtFriHrs.IsReadOnly)
                    txtFriHrs.EditValue = dist;
                else if (!txtSatHrs.IsReadOnly)
                    txtSatHrs.EditValue = dist;
                else if (!txtSunHrs.IsReadOnly)
                    txtSunHrs.EditValue = dist;
            }
            else
            {
                if (!txtMonHrs.IsReadOnly && journalline.Day1 > 0)
                    txtMonHrs.EditValue = dist;
                 if (!txtTueHrs.IsReadOnly && journalline.Day2 > 0)
                    txtTueHrs.EditValue = dist;
                 if (!txtWedHrs.IsReadOnly && journalline.Day3 > 0)
                    txtWedHrs.EditValue = dist;
                 if (!txtThuHrs.IsReadOnly && journalline.Day4 > 0)
                    txtThuHrs.EditValue = dist;
                 if (!txtFriHrs.IsReadOnly && journalline.Day5 > 0)
                    txtFriHrs.EditValue = dist;
                 if (!txtSatHrs.IsReadOnly && journalline.Day6 > 0)
                    txtSatHrs.EditValue = dist;
                 if (!txtSunHrs.IsReadOnly && journalline.Day7 > 0)
                    txtSunHrs.EditValue = dist;
            }
            CalculateTotal();
        }

        internal class Address
        {
            public string Name { get; set; }
            public string ContactAddress { get; set; }
        }
        bool addEmptyAddress = true;
        private void leProject_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            string id = Convert.ToString(e.NewValue);
            var projects = crudApi.CompanyEntity.GetCache(typeof(Uniconta.DataModel.Project));
            var project = (Uniconta.DataModel.Project)projects?.Get(id) as ProjectClient;
            if (project != null)
            {
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
    }
}
