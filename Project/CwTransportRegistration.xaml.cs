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

            addressList = new List<Address>();

            var workAddress = new StringBuilder();
            workAddress.AppendLine(company._Name).AppendLine(company._Address1).AppendLine(company._Address2).AppendLine(company._Address3);
            workAddress.Replace(Environment.NewLine + Environment.NewLine + Environment.NewLine, Environment.NewLine);
            workAddress.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Company"), ContactAddress = workAddress?.ToString() });

            var homeAddress = new StringBuilder();
            if (employee != null)
            {
                homeAddress.AppendLine(employee._Name).AppendLine(employee._Address1).AppendLine(employee._Address2).Append(employee._ZipCode != null ? employee._ZipCode + " " : string.Empty).Append(employee._City);
                homeAddress.Replace(Environment.NewLine + Environment.NewLine + Environment.NewLine, Environment.NewLine);
                homeAddress.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
            }
            addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Private"), ContactAddress = homeAddress?.ToString() });

            var otherAddress = new StringBuilder();
            if (debtor != null)
            {
                otherAddress.AppendLine(debtor._Name).AppendLine(debtor._Address1).AppendLine(debtor._Address2).AppendLine(debtor._Address3).Append(debtor._ZipCode != null ? debtor._ZipCode + " " : string.Empty).Append(debtor._City);
                otherAddress.Replace(Environment.NewLine + Environment.NewLine + Environment.NewLine, Environment.NewLine);
                otherAddress.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
                addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Debtor"), ContactAddress = otherAddress?.ToString() });
            }
            else if (installation != null)
            {
                otherAddress.AppendLine(installation._Name).AppendLine(installation._Address1).AppendLine(installation._Address2).AppendLine(installation._Address3).Append(installation._ZipCode != null ? installation._ZipCode + " " : string.Empty).Append(installation._City);
                otherAddress.Replace(Environment.NewLine + Environment.NewLine + Environment.NewLine, Environment.NewLine);
                otherAddress.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine);
                addressList.Add(new Address { Name = Uniconta.ClientTools.Localization.lookup("Debtor"), ContactAddress = otherAddress?.ToString() });
            }

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
                txtFromAdd.Text = addressList.Count >= 1 ? addressList[0]?.ContactAddress : string.Empty;

                if (addressList.Count == 4)
                {
                    cmbToAdd.SelectedIndex = 2;
                    txtToAdd.Text = addressList[2]?.ContactAddress;
                }
                else 
                {
                    cmbToAdd.SelectedIndex = 3;
                    txtToAdd.Text = addressList[3]?.ContactAddress;
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

            if (addMileage)
                CalulateMileage();
            else
                mileageTotal = mileageTotal - journalline.Total;

            CalculateTotal();
        }

        SolidColorBrush SetBackGroundColor(NumericUpDownEditor txtHours, int dayStatus)
        {
            var SolidColorBrush = new SolidColorBrush(Colors.White);
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

            if (journalline.RowId != 0 && journalline._RegistrationType == RegistrationType.Mileage)
                Project = string.Empty;
            else
                Project = journalline.Project;
            RegistrationProject = payrollCategory?._InternalProject ?? journalline?.Project;
            PayType = payrollCategory?._Number;
            if (journalline._RegistrationType == RegistrationType.Mileage)
            {
                Purpose = journalline.Text;
                VechicleRegNo = journalline.VechicleRegNo;
                FromAddress = journalline.AddressFrom;
                ToAddress = journalline.AddressTo;
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
            double distance = 0d;
            if (string.IsNullOrWhiteSpace(txtFromAdd.Text) || string.IsNullOrWhiteSpace(txtToAdd.Text) || txtFromAdd.Text == EMPTYADDRESS || txtToAdd.Text == EMPTYADDRESS)
                distance = 0;
            else
                distance = TMJournalLineHelper.GetDistance(txtFromAdd.Text, txtToAdd.Text, (bool)chkAvdFerr.IsChecked);
    
            if (!addMileage)
            {
                if (!txtMonHrs.IsReadOnly)
                    txtMonHrs.EditValue = distance;
                else if (!txtTueHrs.IsReadOnly)
                    txtTueHrs.EditValue = distance;
                else if (!txtWedHrs.IsReadOnly)
                    txtWedHrs.EditValue = distance;
                else if (!txtThuHrs.IsReadOnly)
                    txtThuHrs.EditValue = distance;
                else if (!txtFriHrs.IsReadOnly)
                    txtFriHrs.EditValue = distance;
                else if (!txtSatHrs.IsReadOnly)
                    txtSatHrs.EditValue = distance;
                else if (!txtSunHrs.IsReadOnly)
                    txtSunHrs.EditValue = distance;
            }
            else
            {
                if (!txtMonHrs.IsReadOnly && journalline.Day1 > 0)
                    txtMonHrs.EditValue = distance;
                 if (!txtTueHrs.IsReadOnly && journalline.Day2 > 0)
                    txtTueHrs.EditValue = distance;
                 if (!txtWedHrs.IsReadOnly && journalline.Day3 > 0)
                    txtWedHrs.EditValue = distance;
                 if (!txtThuHrs.IsReadOnly && journalline.Day4 > 0)
                    txtThuHrs.EditValue = distance;
                 if (!txtFriHrs.IsReadOnly && journalline.Day5 > 0)
                    txtFriHrs.EditValue = distance;
                 if (!txtSatHrs.IsReadOnly && journalline.Day6 > 0)
                    txtSatHrs.EditValue = distance;
                 if (!txtSunHrs.IsReadOnly && journalline.Day7 > 0)
                    txtSunHrs.EditValue = distance;
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
                double distance = 0d;
                if (!txtMonHrs.IsReadOnly)
                {
                    txtMonHrs.Text = distance.ToString();
                    Day1 = distance;
                }
                if (!txtTueHrs.IsReadOnly)
                {
                    txtTueHrs.Text = distance.ToString();
                    Day2 = distance;
                }
                if (!txtWedHrs.IsReadOnly)
                {
                    txtWedHrs.Text = distance.ToString();
                    Day3 = distance;
                }
                if (!txtThuHrs.IsReadOnly)
                {
                    txtThuHrs.Text = distance.ToString();
                    Day4 = distance;
                }
                if (!txtFriHrs.IsReadOnly)
                {
                    txtFriHrs.Text = distance.ToString();
                    Day5 = distance;
                }
                if (!txtSatHrs.IsReadOnly)
                {
                    txtSatHrs.Text = distance.ToString();
                    Day6 = distance;
                }
                if (!txtSunHrs.IsReadOnly)
                {
                    txtSunHrs.Text = distance.ToString();
                    Day7 = distance;
                }
                CalculateTotal();
                CalulateMileage();
            }
        }
    }
}
