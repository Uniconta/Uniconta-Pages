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
using Uniconta.ClientTools.Page;
using UnicontaClient.Utilities;
using UnicontaClient.Models;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools;
using Uniconta.API.System;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections;
using System.Diagnostics;
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools.Controls;
#if !SILVERLIGHT
using UnicontaClient;
using ImportingTool.Model;
#else
using UnicontaClient.Controls.Login;
#endif
using System.Threading;
using Uniconta.API.GeneralLedger;
using UnicontaClient.Controls;
using System.Windows;
using DevExpress.Xpf.Editors;
using Uniconta.API.Service;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreateCompany : FormBasePage
    {
        CompanyClient editrow;

        public override void OnClosePage(object[] RefreshParams)
        {
        }

        public override Type TableType { get { return typeof(CompanyClient); } }
        public override string NameOfControl { get { return TabControls.CreateCompany.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CompanyClient)value; } }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        User usermaster;
        public CreateCompany(UnicontaBaseEntity sourcedata)
        {
            usermaster = (User)sourcedata;
            Init();
        }
        public CreateCompany(BaseAPI API)
            : base(API, string.Empty)
        {
            Init();
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtCompanyRegNo, txtCompanyRegNo);
#endif
        }

        private void Init()
        {
            InitializeComponent();
            layoutStDate.Label = Uniconta.ClientTools.Localization.lookup("FromDate");
            layoutEndDate.Label = Uniconta.ClientTools.Localization.lookup("ToDate");
            layoutControl = layoutItems;
            editrow = CreateNew() as CompanyClient;
            var defaultCompany = UtilDisplay.GetDefaultCompany();
            if (defaultCompany != null)
            {
                editrow.Country = defaultCompany._CountryId;
                layoutItems.DataContext = editrow;
                int year = BasePage.GetSystemDefaultDate().Year;
                dateFrm.DateTime = new DateTime(year, 1, 1);
                dateTo.DateTime = new DateTime(year, 12, 31);
                frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
                SetOwnCompany();
                CompanySetupPage.SetCountry(cmbCountry, cmbStandardCompany, editrow, true);
                browseTopLogo.FileSelected += BrowseTopLogo_FileSelected;
#if !SILVERLIGHT
                lblImportInvoice.Label = string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Invoice"));
                BindSetupType();
                grpImportSetup.Header = string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Company"));
                cmbImportFrom.ItemsSource = Enum.GetNames(typeof(ImportFrom));
                cmbImportFrom.SelectedIndexChanged += cmbImportFrom_SelectionChanged;
                cmbImportDimension.ItemsSource = new List<string>() { "Ingen", "Kun Afdeling", "Afdeling, Bærer", "Afdeling, Bærer, Formål" };
                cmbImportDimension.SelectedIndex = 3;

                lblDim1.Label = string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Dimension")), " 1");
                lblDim2.Label = string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Dimension")), " 2");
                lblDim3.Label = string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Dimension")), " 3");
                lblDim4.Label = string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Dimension")), " 4");
                lblDim5.Label = string.Concat(string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Dimension")), " 5");
                txtNavDim1.Text = Uniconta.ClientTools.Localization.lookup("Optional");
                txtNavDim2.Text = Uniconta.ClientTools.Localization.lookup("Optional");
                txtNavDim3.Text = Uniconta.ClientTools.Localization.lookup("Optional");
                txtNavDim4.Text = Uniconta.ClientTools.Localization.lookup("Optional");
                txtNavDim5.Text = Uniconta.ClientTools.Localization.lookup("Optional");
                txtNavErrorAccount.Text = Uniconta.ClientTools.Localization.lookup("Required");
                txtAccountForPrimo.Text = Uniconta.ClientTools.Localization.lookup("Required");

                var navEmailType = new List<string>()
            {
                Uniconta.ClientTools.Localization.lookup("InvoiceEmail"),
                Uniconta.ClientTools.Localization.lookup("ContactEmail")
            };
                cmbInvoiceOrContactMail.ItemsSource = navEmailType;
#endif
            }
            else
                UtilDisplay.ShowErrorCode(ErrorCodes.NoRights);
        }
#if !SILVERLIGHT
        void BindSetupType()
        {
            var ind = string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Company"));
            ind += " (" + Uniconta.ClientTools.Localization.lookup("ConvertNavEcoC5") + ")";
            lstSetupType.ItemsSource = new string[] { ind, Uniconta.ClientTools.Localization.lookup("CopySetup") };
            lstSetupType.SelectedIndexChanged += CmbSetupType_SelectedIndexChanged;
            lstSetupType.SelectedIndex = 1;
        }

        private void CmbSetupType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (lstSetupType.SelectedIndex == 1)/* copy setup */
            {
                grpImportSetup.IsEnabled = false;
                grpImportSetup.Visibility = Visibility.Collapsed;
                grpImportSetup.IsCollapsed = true;

                grpCopySetup.IsEnabled = true;
                grpCopySetup.Visibility = Visibility.Visible;
                grpCopySetup.IsCollapsed = false;

                grpFinancialYear.IsEnabled = true;
                grpFinancialYear.Visibility = Visibility.Visible;
                grpFinancialYear.IsCollapsed = false;

                invoiceDateCounter.EditValue = 5;
            }
            else /*Import*/
            {
                grpCopySetup.IsEnabled = false;
                grpCopySetup.Visibility = Visibility.Collapsed;
                grpCopySetup.IsCollapsed = true;

                grpFinancialYear.IsEnabled = false;
                grpFinancialYear.Visibility = Visibility.Collapsed;
                grpFinancialYear.IsCollapsed = true;

                grpImportSetup.IsEnabled = true;
                grpImportSetup.Visibility = Visibility.Visible;
                grpImportSetup.IsCollapsed = false;

            }
        }

        private void cmbImportFrom_SelectionChanged(object sender, RoutedEventArgs e)
        {
            chkSet0InAct.IsEnabled = true;
            chkInvoiceVatPriceCheck.IsEnabled = false;
            chkInvoiceVatPriceCheck.Visibility = Visibility.Collapsed;
            chkConcatC5ItemNames.IsEnabled = false;
            lblConcatC5ItemNames.Visibility = Visibility.Collapsed;
            chkConcatC5ItemNames.Visibility = Visibility.Collapsed;
            if (cmbImportFrom.SelectedIndex == (int)ImportFrom.c5_Iceland || cmbImportFrom.SelectedIndex == (int)ImportFrom.c5_Danmark) // c5
            {
                cmbImportDimension.IsEnabled = true;

                lblDimC5.Label = "Import Dimensioner:";
                lblDimC5.Visibility = Visibility.Visible;
                cmbImportDimension.Visibility = Visibility.Visible;

                lblDim1.Visibility = Visibility.Collapsed;
                txtNavDim1.Visibility = Visibility.Collapsed;

                lblInvConEmail.Visibility = Visibility.Visible;
                cmbInvoiceOrContactMail.Visibility = Visibility.Visible;

                lblDim2.Visibility = Visibility.Collapsed;
                txtNavDim2.Visibility = Visibility.Collapsed;

                lblDim3.Visibility = Visibility.Collapsed;
                txtNavDim3.Visibility = Visibility.Collapsed;

                lblDim4.Visibility = Visibility.Collapsed;
                txtNavDim4.Visibility = Visibility.Collapsed;

                lblDim5.Visibility = Visibility.Collapsed;
                txtNavDim5.Visibility = Visibility.Collapsed;

                lblerrorAccount.Visibility = Visibility.Collapsed;
                txtNavErrorAccount.Visibility = Visibility.Collapsed;

                lblAccountForPrimo.Visibility = Visibility.Collapsed;
                txtAccountForPrimo.Visibility = Visibility.Collapsed;

                lblSet0InCustAcc.Visibility = Visibility.Collapsed;
                lblSet0InVendAcc.Visibility = Visibility.Collapsed;
                chkSet0InCustAcc.Visibility = Visibility.Collapsed;
                chkSet0InVendAcc.Visibility = Visibility.Collapsed;
                chkConcatC5ItemNames.IsEnabled = true;
                lblConcatC5ItemNames.Visibility = Visibility.Visible;
                chkConcatC5ItemNames.Visibility = Visibility.Visible;
                lblImportInvoice.Visibility = Visibility.Visible;
                chkImportInvoice.IsChecked = true;
            }
            else if (cmbImportFrom.SelectedIndex == (int)ImportFrom.NAV || cmbImportFrom.SelectedIndex == (int)ImportFrom.BC_NAVOnline) //NAV
            {
                cmbImportDimension.IsEnabled = false;

                lblDimC5.Visibility = Visibility.Collapsed;
                cmbImportDimension.Visibility = Visibility.Collapsed;
                lblDim1.Visibility = Visibility.Visible;
                txtNavDim1.Visibility = Visibility.Visible;

                lblInvConEmail.Visibility = Visibility.Collapsed;
                cmbInvoiceOrContactMail.Visibility = Visibility.Collapsed;

                lblDim2.Visibility = Visibility.Visible;
                txtNavDim2.Visibility = Visibility.Visible;

                lblDim3.Visibility = Visibility.Visible;
                txtNavDim3.Visibility = Visibility.Visible;

                lblDim4.Visibility = Visibility.Visible;
                txtNavDim4.Visibility = Visibility.Visible;

                lblDim5.Visibility = Visibility.Visible;
                txtNavDim5.Visibility = Visibility.Visible;

                lblSet0InCustAcc.Visibility = Visibility.Collapsed;
                lblSet0InVendAcc.Visibility = Visibility.Collapsed;
                chkSet0InCustAcc.Visibility = Visibility.Collapsed;
                chkSet0InVendAcc.Visibility = Visibility.Collapsed;

                lblerrorAccount.Visibility = Visibility.Visible;
                txtNavErrorAccount.Visibility = Visibility.Visible;

                lblAccountForPrimo.Visibility = Visibility.Visible;
                txtAccountForPrimo.Visibility = Visibility.Visible;

                lblImportInvoice.Visibility = Visibility.Visible;
                chkImportInvoice.IsChecked = true;
                if (cmbImportFrom.SelectedIndex == (int)ImportFrom.NAV)
                {
                    liDirectory.Visibility = Visibility.Visible;
                    liExcelFile.Visibility = Visibility.Collapsed;
                }
                else
                {
                    liExcelFile.Visibility = Visibility.Visible;
                    liDirectory.Visibility = Visibility.Collapsed;

                }
            }
            else if (cmbImportFrom.SelectedIndex == (int)ImportFrom.dk_Iceland) //DK Iceland
            {
                chkInvoiceVatPriceCheck.IsEnabled = true;
                chkInvoiceVatPriceCheck.Visibility = Visibility.Visible;

                cmbImportDimension.IsEnabled = false;

                lblDimC5.Visibility = Visibility.Collapsed;
                cmbImportDimension.Visibility = Visibility.Collapsed;
                lblDim1.Visibility = Visibility.Collapsed;
                txtNavDim1.Visibility = Visibility.Collapsed;

                lblInvConEmail.Visibility = Visibility.Visible;
                cmbInvoiceOrContactMail.Visibility = Visibility.Visible;

                lblDim2.Visibility = Visibility.Collapsed;
                txtNavDim2.Visibility = Visibility.Collapsed;

                lblDim3.Visibility = Visibility.Collapsed;
                txtNavDim3.Visibility = Visibility.Collapsed;

                lblDim4.Visibility = Visibility.Collapsed;
                txtNavDim4.Visibility = Visibility.Collapsed;

                lblDim5.Visibility = Visibility.Collapsed;
                txtNavDim5.Visibility = Visibility.Collapsed;

                lblSet0InCustAcc.Visibility = Visibility.Collapsed;
                lblSet0InVendAcc.Visibility = Visibility.Collapsed;
                chkSet0InCustAcc.Visibility = Visibility.Collapsed;
                chkSet0InVendAcc.Visibility = Visibility.Collapsed;

                lblerrorAccount.Visibility = Visibility.Visible;
                txtNavErrorAccount.Visibility = Visibility.Visible;

                lblAccountForPrimo.Visibility = Visibility.Visible;
                txtAccountForPrimo.Visibility = Visibility.Visible;

                lblImportInvoice.Visibility = Visibility.Collapsed;
                chkImportInvoice.IsChecked = false;
            }
            else if (cmbImportFrom.SelectedIndex == (int)ImportFrom.Ax30_eCTRL) //eCTRL Axapta 3.0 sp3
            {
                chkSet0InCustAcc.IsEnabled = true;
                chkSet0InVendAcc.IsEnabled = true;

                lblSet0InCustAcc.Visibility = Visibility.Visible;
                lblSet0InVendAcc.Visibility = Visibility.Visible;
                chkSet0InCustAcc.Visibility = Visibility.Visible;
                chkSet0InVendAcc.Visibility = Visibility.Visible;

                lblDim1.Visibility = Visibility.Collapsed;
                txtNavDim1.Visibility = Visibility.Collapsed;

                lblInvConEmail.Visibility = Visibility.Visible;
                cmbInvoiceOrContactMail.Visibility = Visibility.Visible;

                lblDim2.Visibility = Visibility.Collapsed;
                txtNavDim2.Visibility = Visibility.Collapsed;

                lblDim3.Visibility = Visibility.Collapsed;
                txtNavDim3.Visibility = Visibility.Collapsed;

                lblDim4.Visibility = Visibility.Collapsed;
                txtNavDim4.Visibility = Visibility.Collapsed;

                lblDim5.Visibility = Visibility.Collapsed;
                txtNavDim5.Visibility = Visibility.Collapsed;

                lblerrorAccount.Visibility = Visibility.Collapsed;
                txtNavErrorAccount.Visibility = Visibility.Collapsed;

                lblAccountForPrimo.Visibility = Visibility.Collapsed;
                txtAccountForPrimo.Visibility = Visibility.Collapsed;

                lblInvConEmail.Visibility = Visibility.Collapsed;
                cmbInvoiceOrContactMail.Visibility = Visibility.Collapsed;
            }
            else // e-conomic
            {
                cmbImportDimension.IsEnabled = false;
                chkInvoiceVatPriceCheck.IsEnabled = true;
                chkInvoiceVatPriceCheck.Visibility = Visibility.Visible;
                lblDimC5.Visibility = Visibility.Collapsed;
                cmbImportDimension.Visibility = Visibility.Collapsed;

                lblDim1.Visibility = Visibility.Collapsed;
                txtNavDim1.Visibility = Visibility.Collapsed;

                lblInvConEmail.Visibility = Visibility.Visible;
                cmbInvoiceOrContactMail.Visibility = Visibility.Visible;

                lblDim2.Visibility = Visibility.Collapsed;
                txtNavDim2.Visibility = Visibility.Collapsed;

                lblDim3.Visibility = Visibility.Collapsed;
                txtNavDim3.Visibility = Visibility.Collapsed;

                lblDim4.Visibility = Visibility.Collapsed;
                txtNavDim4.Visibility = Visibility.Collapsed;

                lblDim5.Visibility = Visibility.Collapsed;
                txtNavDim5.Visibility = Visibility.Collapsed;

                lblSet0InCustAcc.Visibility = Visibility.Collapsed;
                lblSet0InVendAcc.Visibility = Visibility.Collapsed;
                chkSet0InCustAcc.Visibility = Visibility.Collapsed;
                chkSet0InVendAcc.Visibility = Visibility.Collapsed;

                lblerrorAccount.Visibility = Visibility.Collapsed;
                txtNavErrorAccount.Visibility = Visibility.Collapsed;

                lblAccountForPrimo.Visibility = Visibility.Collapsed;
                txtAccountForPrimo.Visibility = Visibility.Collapsed;

                lblImportInvoice.Visibility = Visibility.Visible;
                chkImportInvoice.IsChecked = true;
            }
            SetCountry();
        }

        void SetCountry()
        {
            var index = cmbImportFrom.SelectedIndex;
            switch (index)
            {
                case (int)ImportFrom.c5_Danmark:
                case (int)ImportFrom.economic_Danmark:
                    editrow.Country = CountryCode.Denmark;
                    break;
                case (int)ImportFrom.c5_Iceland:
                case (int)ImportFrom.dk_Iceland:
                    editrow.Country = CountryCode.Iceland;
                    break;
                case (int)ImportFrom.economic_Norge:
                    editrow.Country = CountryCode.Norway;
                    break;
                case (int)ImportFrom.economic_Sweden:
                    editrow.Country = CountryCode.Sweden;
                    break;
                case (int)ImportFrom.economic_Germany:
                    editrow.Country = CountryCode.Germany;
                    break;
            }
        }
        private void FileBrowse_ButtonClicked(object sender)
        {
            var openFolderDialog = UtilDisplay.LoadFolderBrowserDialog;
            if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                txtImportFromDirectory.Text = openFolderDialog.SelectedPath;
        }
        private void ExcelFileBrowse_ButtonClicked(object sender)
        {

            var openFileDialog = UtilDisplay.LoadOpenFileDialog;
            openFileDialog.Filter = "Excel Files|*.xls;*.xlsx;*.xlsm";
            if (openFileDialog.ShowDialog() == true)
                txtImportFromFile.Text = openFileDialog.FileName;
        }
#endif
        private void BrowseTopLogo_FileSelected()
        {
            if (browseTopLogo.FileBytes != null && browseTopLogo.FileBytes.Length > 100 * 1024)
            {
                UnicontaMessageBox.Show(string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("MaxFileSizeLimit"), "100KB"), Uniconta.ClientTools.Localization.lookup("Error"));
                browseTopLogo.ResetControl();
            }
        }

        void SetOwnCompany()
        {
            var companies = CWDefaultCompany.loadedCompanies != null ? CWDefaultCompany.loadedCompanies.ToList() : new List<Company>();
            companies.Insert(0, new Company() { _Name = string.Format("---{0}---", Uniconta.ClientTools.Localization.lookup("Select")) });
            RemoveDemoCompany(companies);
        }
        void RemoveDemoCompany(List<Company> companies)
        {
            companies.RemoveAll(a => a.DemoView == true);
            if (companies != null && companies.Count > 1)
            {
                cmbOwnCompany.ItemsSource = companies;
                cmbOwnCompany.SelectedIndex = 0;
            }
            else
            {
                itemOwncmp.Visibility = Visibility.Collapsed;
                itmlblOR.Visibility = Visibility.Collapsed;
            }
        }
        Company fromCompany;
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    MoveFocus();
                    if (editrow._Name == null)
                    {
#if !SILVERLIGHT
                        int importFrom = cmbImportFrom.SelectedIndex;
                        int setupType = lstSetupType.SelectedIndex;
#else
                        int importFrom = -1;
                        int setupType = 1;
#endif
#if !SILVERLIGHT
                        if (setupType == 1 || importFrom == (int)ImportFrom.dk_Iceland || (importFrom >= (int)ImportFrom.economic_Danmark && importFrom <= (int)ImportFrom.economic_Germany))

#else
                        if (setupType == 1)

#endif
                        {
                            UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("CompanyName")),
                                Uniconta.ClientTools.Localization.lookup("Warning"));
                            return;
                        }
                    }
                    if (editrow._Country == (byte)CountryCode.Unknown)
                    {
                        UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CountryNotSet"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
                        return;
                    }

#if !SILVERLIGHT
                    if (lblerrorAccount.Visibility == Visibility.Visible && (txtNavErrorAccount.Text == Uniconta.ClientTools.Localization.lookup("Required") || string.IsNullOrWhiteSpace(txtNavErrorAccount.Text)))
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("CreateOrFindErrorAccount")), 
                            Uniconta.ClientTools.Localization.lookup("Warning"));
                        return;
                    }

                    if (lblAccountForPrimo.Visibility == Visibility.Visible && (txtAccountForPrimo.Text == Uniconta.ClientTools.Localization.lookup("Required") || string.IsNullOrWhiteSpace(txtAccountForPrimo.Text)))
                    {
                        UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("AccountTypeYearResultTransfer")), 
                            Uniconta.ClientTools.Localization.lookup("Warning"));
                        return;
                    }

                    if (lstSetupType.SelectedIndex == 1)
                    {
#endif
                        if (cmbOwnCompany.SelectedIndex > 0)
                            fromCompany = (Company)cmbOwnCompany.SelectedItem;
                        else if (cmbStandardCompany.SelectedIndex > 0)
                        {
                            fromCompany = (Company)cmbStandardCompany.SelectedItem;
                            chkDimensions.IsChecked = false;
                        }
                        if (fromCompany == null && ((IList)cmbStandardCompany.ItemsSource).Count != 0)
                        {
                            CWConfirmationBox dialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("CopySetupCompanyConfirmation"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
                            dialog.Closing += delegate
                            {
                                if (dialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                                    SaveCompany(null);
                            };
                            dialog.Show();
                        }
                        else
                            SaveCompany(fromCompany);
#if !SILVERLIGHT
                    }
                    else
                        SaveCompany(fromCompany);
#endif
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void SaveCompany(Company fromCompany)
        {
            int setupType = 1; /*Copy Setup */
            int importFrom = -1;
            string path = string.Empty;
            bool set0InAccount, set0InCustAcc, set0InVendAcc;
            bool includeVatInPrices, concatC5ItemNames;
            int importDim;
#if !SILVERLIGHT
            setupType = lstSetupType.SelectedIndex;
            importFrom = cmbImportFrom.SelectedIndex;
            if (importFrom == (int)ImportFrom.BC_NAVOnline)
                path = txtImportFromFile.Text;
            else
                path = txtImportFromDirectory.Text;
            set0InAccount = chkSet0InAct.IsChecked.Value;
            set0InCustAcc = chkSet0InCustAcc.IsChecked.Value;
            set0InVendAcc = chkSet0InVendAcc.IsChecked.Value;
            concatC5ItemNames = chkConcatC5ItemNames.IsChecked.Value;
            includeVatInPrices = chkInvoiceVatPriceCheck.IsChecked.Value;
            importDim = cmbImportDimension.SelectedIndex;
            switch ((ImportFrom)importFrom)
            {
                case ImportFrom.c5_Danmark:
                case ImportFrom.c5_Iceland: editrow._ConvertedFrom = (int)ConvertFromType.C5; break;
                case ImportFrom.economic_Danmark:
                case ImportFrom.economic_English:
                case ImportFrom.economic_Norge:
                case ImportFrom.economic_Germany:
                case ImportFrom.economic_Sweden: editrow._ConvertedFrom = (int)ConvertFromType.Eco; break;
                case ImportFrom.BC_NAVOnline:
                case ImportFrom.NAV: editrow._ConvertedFrom = (int)ConvertFromType.Nav; break;
                case ImportFrom.Ax30_eCTRL: editrow._ConvertedFrom = (int)ConvertFromType.eCtrl; break;
                case ImportFrom.dk_Iceland: editrow._ConvertedFrom = (int)ConvertFromType.dk_Iceland; break;
            }
#endif
            if (setupType == 1)
            {
                if (dateFrm.DateTime.Date < new DateTime(2000, 1, 1))
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("YearStartError"), dateFrm.DateTime.Date), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }
            }
            else
            {
                if (importFrom == -1)
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("ImportFrom")),
                        Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }
                if (string.IsNullOrEmpty(path))
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("CannotBeBlank"), Uniconta.ClientTools.Localization.lookup("SelectDirectory")),
                        Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }
            }
            try
            {
                if (setupType == 1)
                {
                    if (fromCompany != null)
                    {
                        editrow.CopyFunctions(fromCompany);
                        busyIndicator.BusyContent = string.Format(Uniconta.ClientTools.Localization.lookup("CopyingCompany"), fromCompany._Name, editrow._Name);
                    }                    
                    busyIndicator.IsBusy = true;
                    if (chkDimensions.IsChecked == true && fromCompany != null)
                    {
                        var dim = await session.GetCompany(fromCompany.CompanyId);
                        if (dim != null)
                        {
                            editrow.NumberOfDimensions = dim.NumberOfDimensions;
                            editrow._Dim1 = dim._Dim1;
                            editrow._Dim2 = dim._Dim2;
                            editrow._Dim3 = dim._Dim3;
                            editrow._Dim4 = dim._Dim4;
                            editrow._Dim5 = dim._Dim5;
                        }
                    }
                }
                else
                    busyIndicator.IsBusy = true;
                ErrorCodes err;
                if (editrow._Name == null)
                    editrow._Name = "import";
                if (usermaster != null)
                    err = await session.CreateCompany(editrow, usermaster);
                else
                    err = await session.CreateCompany(editrow);
                if (err == ErrorCodes.Succes)
                {
                    Company[] companies = await BasePage.session.GetCompanies();
                    UnicontaClient.Controls.CWDefaultCompany.loadedCompanies = companies;

                    if (Utility.GetDefaultCompany() == null)
                        globalEvents.OnRefresh(NameOfControl);
                    if (setupType == 1)/* Import- Financial year not required*/
                    {
                        await SaveFinancialYear(editrow, dateFrm.DateTime.Date, dateTo.DateTime.Date);
                        if (fromCompany != null)
                            await CopyBaseData();
                    }
                    await SaveCompanyLogos();
                    if (setupType != 1)
                    {
#if !SILVERLIGHT
                        var pastYears = string.IsNullOrWhiteSpace(invoiceDateCounter.Text) ? 0 : - (int)NumberConvert.ToInt(invoiceDateCounter.Text);
                        DateTime invoiceFrmDate = DateTime.Today.AddYears(pastYears); 
                        var listOfNavDim = new List<string>();
                        var dim = string.IsNullOrWhiteSpace(txtNavDim1.Text) ? string.Empty : txtNavDim1.Text;
                        listOfNavDim.Add(dim);
                        dim = string.IsNullOrWhiteSpace(txtNavDim2.Text) ? string.Empty : txtNavDim2.Text;
                        listOfNavDim.Add(dim);
                        dim = string.IsNullOrWhiteSpace(txtNavDim3.Text) ? string.Empty : txtNavDim3.Text;
                        listOfNavDim.Add(dim);
                        dim = string.IsNullOrWhiteSpace(txtNavDim4.Text) ? string.Empty : txtNavDim4.Text;
                        listOfNavDim.Add(dim);
                        dim = string.IsNullOrWhiteSpace(txtNavDim5.Text) ? string.Empty : txtNavDim5.Text;
                        listOfNavDim.Add(dim);

                        var arrayOfNavDim = listOfNavDim.ToArray();

                        object[] compParams = new object[18];
                        compParams[0] = path;
                        compParams[1] = editrow;
                        compParams[2] = (ImportFrom)importFrom;
                        compParams[3] = set0InAccount;
                        compParams[4] = importDim;
                        compParams[5] = arrayOfNavDim;
                        compParams[6] = cmbInvoiceOrContactMail.SelectedIndex == 0;
                        compParams[7] = txtNavErrorAccount.Text;
                        compParams[8] = includeVatInPrices;
                        compParams[9] = chkLedgerTransactions.IsChecked.Value;
                        compParams[10] = chkDebtorTransactions.IsChecked.Value;
                        compParams[11] = chkCreditorTransactions.IsChecked.Value;
                        compParams[12] = txtAccountForPrimo.Text;
                        compParams[13] = chkImportInvoice.IsChecked.Value;
                        compParams[14] = set0InCustAcc;
                        compParams[15] = set0InVendAcc;
                        compParams[16] = concatC5ItemNames;
                        compParams[17] = invoiceFrmDate;

                        string header = string.Format(Uniconta.ClientTools.Localization.lookup("ImportOBJ"), Uniconta.ClientTools.Localization.lookup("Company"));
                        AddDockItem(TabControls.ImportFromOtherCompanySetup, compParams, header);
#endif
                    }

                    if (usermaster != null && usermaster.Uid != session.Uid)
                    {
                        busyIndicator.IsBusy = false;
                        return;
                    }

                    if (setupType == 1)
                    {
                        globalEvents.OnRefresh(TabControls.CreateCompany, editrow.RowId);
                        CloseDockItem();
                    }
                }
                else
                {
                    busyIndicator.IsBusy = false;
                    UtilDisplay.ShowErrorCode(err);
                }
            }
            catch (Exception ex)
            {
                BasePage.session.ReportException(ex, "Create Company. Setup Page", 0);
                UnicontaMessageBox.Show(ex);
            }
            if (setupType == 1)
                busyIndicator.IsBusy = false;
        }

        async private Task SaveCompanyLogos()
        {
            var companyDocumentsList = new List<CompanyDocumentClient>();
            if (browseTopLogo.FileBytes != null && browseTopLogo.FileBytes.Length > 0)
                companyDocumentsList.Add(AddLogosToCompanyDocument(browseTopLogo.FileBytes, browseTopLogo.FileExtension, CompanyDocumentUse.TopBarLogo));
            if (browseInvoiceLogo.FileBytes != null && browseInvoiceLogo.FileBytes.Length > 0)
                companyDocumentsList.Add(AddLogosToCompanyDocument(browseInvoiceLogo.FileBytes, browseInvoiceLogo.FileExtension, CompanyDocumentUse.CompanyLogo));
            if (companyDocumentsList.Count > 0)
            {
                var companyApi = new CrudAPI(session, (CompanyClient)ModifiedRow);
                var result = await companyApi.Insert(companyDocumentsList);
                if (result != ErrorCodes.Succes)
                    UtilDisplay.ShowErrorCode(result);
            }
        }

        private CompanyDocumentClient AddLogosToCompanyDocument(byte[] fileBytes, string fileExtension, CompanyDocumentUse documentUseFor)
        {
            var companyDoc = new CompanyDocumentClient();
            companyDoc.UseFor = documentUseFor;
            companyDoc.DocumentData = fileBytes;
            companyDoc.DocumentType = DocumentConvert.GetDocumentType(fileExtension);

            return companyDoc;
        }

        static public Task<ErrorCodes> SaveFinancialYear(Company Comp, DateTime FromDate, DateTime ToDate)
        {
            var fnYear = new CompanyFinanceYear();
            fnYear._FromDate = FromDate;
            fnYear._ToDate = ToDate;
            fnYear._State = FinancePeriodeState.Open;
            fnYear._Current = true;
            var fnAPI = new CrudAPI(session, Comp);
            return fnAPI.Insert(fnYear);
        }

        async Task CopyBaseData()
        {
            CompanyAPI comApi = new CompanyAPI(api);
            ErrorCodes res = await comApi.CopyBaseData(fromCompany, editrow, (bool)chkDimensions.IsChecked, (bool)chkTransType.IsChecked, (bool)chkNumberSerei.IsChecked, (bool)chkPayments.IsChecked, (bool)chkJournal.IsChecked, (bool)chkGroups.IsChecked, (bool)chkGlAccount.IsChecked, (bool)chkVat.IsChecked, CopyProject: (bool)chkProject.IsChecked);
            Thread.Sleep(2 * 1000);
            editrow.ClearCache(typeof(GLVat));
            editrow.ClearCache(typeof(GLAccount));
            busyIndicator.IsBusy = false;
            if (res != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(res);
        }

        private void cmbCountry_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            if (cmbCountry.SelectedIndex >= 0)
            {
                CompanySetupPage.BindStandardCompany(cmbStandardCompany, editrow.Country);
                CompanySetupPage.SetCountry(cmbCountry, cmbStandardCompany, editrow, true);
            }
        }

        private async void Account_Click(object sender, RoutedEventArgs e)
        {
            var company = cmbStandardCompany.SelectedItem as Company;
            if (company == null || company.CompanyId == 0)
                return;
            StandardGLAccountAPI accountApi = new StandardGLAccountAPI(session, company);
            var accounts = await accountApi.Load(new StandardGLAccountClient());
            if (accounts != null)
            {
                CWAccounts accountsWindow = new CWAccounts((StandardGLAccountClient[])accounts);
                accountsWindow.Show();
            }
        }

        private void cmbOwnCompany_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbOwnCompany.SelectedIndex > 0)
            {
                cmbStandardCompany.SelectedIndex = 0;
                cmbStandardCompany.IsEnabled = false;
            }
            else
            {
                cmbStandardCompany.SelectedIndex = 0;
                cmbStandardCompany.IsEnabled = true;
            }
        }

        private void cmbStandardCompany_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbStandardCompany.SelectedIndex > 0)
            {
                cmbOwnCompany.SelectedIndex = 0;
                cmbOwnCompany.IsEnabled = false;
            }
            else
            {
                cmbOwnCompany.SelectedIndex = 0;
                cmbOwnCompany.IsEnabled = true;
            }
        }

        private bool onlyRunOnce;

        private async void txtCVR_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded)
            {
                var cvr = s.Text?.Trim();
                if (cvr == null || cvr.Length < 5)
                    return;

                var allIsLetter = cvr.All(x => char.IsLetter(x));
                if (allIsLetter)
                    return;

                CompanyInfo ci = null;
                try
                {
#if !SILVERLIGHT
                    ci = await CVR.CheckCountry(cvr, editrow.Country);
#else
                    var lookupApi = new Uniconta.API.System.UtilityAPI(api);
                    ci = await lookupApi.LookupCVR(cvr, editrow.Country);
#endif
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                    return;
                }

                if (!onlyRunOnce)
                {
                    if (ci == null)
                        return;

                    if (!string.IsNullOrWhiteSpace(ci?.life?.name))
                    {
                        var address = ci.address;
                        if (address != null)
                        {
                            onlyRunOnce = true;
                            var streetAddress = address.CompleteStreet;
                            if (string.IsNullOrWhiteSpace(editrow.Address1))
                            {
                                editrow.Address1 = streetAddress;
                                editrow.Address2 = address.ZipCity;
                                editrow.Country = address.Country;
                            }
                            else
                            {
                                var result = UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UpdateAddress"), Uniconta.ClientTools.Localization.lookup("Information"), UnicontaMessageBox.YesNo);
                                if (result != UnicontaMessageBox.Yes)
                                    return;
                                {
                                    editrow.Address1 = streetAddress;
                                    editrow.Address2 = address.ZipCity;
                                    editrow.Country = address.Country;
                                }
                            }
                        }
                        if (string.IsNullOrWhiteSpace(editrow._Name))
                            editrow._Name = ci.life.name;

                        if (!string.IsNullOrEmpty(ci.contact?.phone))
                            editrow.Phone = ci.contact.phone;
                    }
                }
                else
                    onlyRunOnce = false;
            }
        }

#if !SILVERLIGHT
        private void CorasauLayoutItem_OnButtonClicked(object sender)
        {
            var location = editrow.Address1 + "+" + editrow.Address2 + "+" + editrow.Address3 + "+" + editrow.Country;
            Utility.OpenGoogleMap(location);
        }


        private bool isEnteredDim1 = false;
        private bool isEnteredDim2 = false;
        private bool isEnteredDim3 = false;
        private bool isEnteredDim4 = false;
        private bool isEnteredDim5 = false;
        private bool isEnteredErrorAcc = false;
        private bool isEnteredPrimoAcc = false;

        private void TxtNavDim1_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!isEnteredDim1)
            {
                txtNavDim1.Text = "";
                isEnteredDim1 = true;
                txtNavDim1.FontWeight = FontWeights.Normal;
            }
        }

        private void TxtNavDim2_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!isEnteredDim2)
            {
                txtNavDim2.Text = "";
                txtNavDim2.FontWeight = FontWeights.Normal;
                isEnteredDim2 = true;
            }
        }

        private void TxtNavDim3_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!isEnteredDim3)
            {
                txtNavDim3.Text = "";
                txtNavDim3.FontWeight = FontWeights.Normal;
                isEnteredDim3 = true;
            }
        }

        private void TxtNavDim4_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!isEnteredDim4)
            {
                txtNavDim4.Text = "";
                txtNavDim4.FontWeight = FontWeights.Normal;
                isEnteredDim4 = true;
            }
        }

        private void TxtNavDim5_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!isEnteredDim5)
            {
                txtNavDim5.Text = "";
                txtNavDim5.FontWeight = FontWeights.Normal;
                isEnteredDim5 = true;
            }
        }

        private void TxtAccountForPrimo_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!isEnteredPrimoAcc)
            {
                txtAccountForPrimo.Text = "";
                txtAccountForPrimo.FontWeight = FontWeights.Normal;
                isEnteredPrimoAcc = true;
            }
        }

        private void TxtNavErrorAccount_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (!isEnteredErrorAcc)
            {
                txtNavErrorAccount.Text = "";
                txtNavErrorAccount.FontWeight = FontWeights.Normal;
                isEnteredErrorAcc = true;
            }
        }

        private void ChkLedgerTransactions_OnChecked(object sender, RoutedEventArgs e)
        {
            if (chkLedgerTransactions.IsChecked.Value)
            {
                chkLedgerTransactions.IsChecked = true;

                chkCreditorTransactions.IsEnabled = true;
                chkCreditorTransactions.Visibility = Visibility.Visible;
                chkCreditorTransactions.IsChecked = true;
                lblCredTrans.Visibility = Visibility.Visible;

                chkDebtorTransactions.IsEnabled = true;
                chkDebtorTransactions.Visibility = Visibility.Visible;
                chkDebtorTransactions.IsChecked = true;
                lblDebTrans.Visibility = Visibility.Visible;
            }
        }

        private void ChkLedgerTransactions_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (!chkLedgerTransactions.IsChecked.Value)
            {
                chkLedgerTransactions.IsChecked = false;

                chkCreditorTransactions.IsEnabled = false;
                chkCreditorTransactions.Visibility = Visibility.Collapsed;
                chkCreditorTransactions.IsChecked = false;
                lblCredTrans.Visibility = Visibility.Collapsed;

                chkDebtorTransactions.IsEnabled = false;
                chkDebtorTransactions.Visibility = Visibility.Collapsed;
                chkDebtorTransactions.IsChecked = false;
                lblDebTrans.Visibility = Visibility.Collapsed;

            }
        }
#endif
    }
}
