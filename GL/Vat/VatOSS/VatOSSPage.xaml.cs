using UnicontaClient.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Enums;
using Uniconta.DataModel;


using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatOSSGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VatOSSTable); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class VatOSSPage : GridBasePage
    {
        SQLTableCache<GLVat> glVatCache;
     
        public override string NameOfControl
        {
            get { return TabControls.VatOSSPage; }
        }

        public VatOSSPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        private CreateVatOSSFile vatOSSHelper;
        private bool compressed;
        static DateTime DefaultFromDate, DefaultToDate;
        int vatOSSReportType;

        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgVatOSSGrid;
            SetRibbonControl(localMenu, dgVatOSSGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgVatOSSGrid.api = api;
            dgVatOSSGrid.BusyIndicator = busyIndicator;
            dgVatOSSGrid.ShowTotalSummary();
            txtDateFrm.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 01);
            txtDateTo.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));

            cmbVatOSSReportType.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("UnionSchemeOSS"), Uniconta.ClientTools.Localization.lookup("NonUnionSchemeOSS"), Uniconta.ClientTools.Localization.lookup("ImportSchemeOSS") };
            cmbVatOSSReportType.SelectedIndex = 0;

            vatOSSHelper = new CreateVatOSSFile(api);

            if (DefaultFromDate == DateTime.MinValue)
            {
                var now = BasePage.GetSystemDefaultDate();

                var fromDate = new DateTime(now.Year, now.Month, 1);
                fromDate = fromDate.AddMonths(-1);
                
                DefaultFromDate = fromDate;
                DefaultToDate = fromDate.AddMonths(1).AddDays(-1);
            }

            txtDateTo.DateTime = DefaultToDate;
            txtDateFrm.DateTime = DefaultFromDate;
            SetDateTime(txtDateFrm, txtDateTo);

            glVatCache = api.GetCache<Uniconta.DataModel.GLVat>();
        }

        public static void SetDateTime(DateEditor frmDateeditor, DateEditor todateeditor)
        {
            var now = BasePage.GetSystemDefaultDate();
            if (frmDateeditor.Text == string.Empty)
            {
                var fromDate = new DateTime(now.Year, now.Month, 1);
                fromDate = fromDate.AddMonths(-1);
                DefaultFromDate = fromDate;
            }
            else
                DefaultFromDate = frmDateeditor.DateTime.Date;

            if (todateeditor.Text == string.Empty)
                DefaultToDate = DefaultFromDate.AddMonths(1).AddDays(-1);
            else
                DefaultToDate = todateeditor.DateTime.Date;
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override async void LoadCacheInBackGround()
        {
            if (glVatCache == null)
                glVatCache = await api.LoadCache<Uniconta.DataModel.GLVat>().ConfigureAwait(false);
           
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.InvGroup) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgVatOSSGrid.SelectedItem as VatOSSTable;

            switch (ActionType)
            {
                case "DeleteRow":
                    if (selectedItem != null)
                        dgVatOSSGrid.DeleteRow();
                    break;
                case "Validate":
                    if (dgVatOSSGrid.ItemsSource != null)
                        CallValidate(true);
                    break;
                case "Compress":
                    if (dgVatOSSGrid.ItemsSource != null)
                        Compress();
                    break;
                case "Search":
                        btnSearch();
                    break;
                case "CreateFile":
                    if (dgVatOSSGrid.ItemsSource != null)
                        CreateFile();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async void GetVatOSS(DateTime fromDate, DateTime toDate)
        {
            SetDateTime(txtDateFrm, txtDateTo);

            busyIndicator.IsBusy = true;
            List<PropValuePair> propValPair = new List<PropValuePair>();

            if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
            {
                string filter;
                if (fromDate != DateTime.MinValue)
                    filter = String.Format("{0:d}..", fromDate);
                else
                    filter = "..";
                if (toDate != DateTime.MinValue)
                    filter += String.Format("{0:d}", toDate);
                var prop = PropValuePair.GenereteWhereElements("Date", typeof(DateTime), filter);
                propValPair.Add(prop);
            }

            if (glVatCache == null)
                glVatCache = await api.LoadCache<Uniconta.DataModel.GLVat>();

            var vatEUList = glVatCache.Where(s => s._TypeSales == CreateVatOSSFile.VATTYPE_MOSS).Select(x => x._Vat).Distinct();
            if (vatEUList != null && vatEUList.Count() > 0)
            {
                var strLst = string.Join(";", vatEUList);
                propValPair.Add(PropValuePair.GenereteWhereElements(nameof(DebtorInvoiceLines.Vat), typeof(string), strLst));
            }
            var listOfDebInvLines = await api.Query<DebtorInvoiceLines>(propValPair);
            List<VatOSSTable> vatOSSlst = new List<VatOSSTable>();

            if (listOfDebInvLines != null && listOfDebInvLines.Length != 0)
            {
                vatOSSlst = UpdateValues(listOfDebInvLines);
                busyIndicator.IsBusy = false;
            }
            else
            {
                var vatOSS = new VatOSSTable();
                vatOSS._CompanyId = api.CompanyId;
                vatOSS._Date = DefaultFromDate;
                vatOSS.MOSSType = vatOSSReportType == 0 ? CreateVatOSSFile.MOSSTYPE_008 : vatOSSReportType == 1 ? CreateVatOSSFile.MOSSTYPE_021 : CreateVatOSSFile.MOSSTYPE_031;
                vatOSS._MOSSTypeName = GetMOSSTypeName(vatOSS.MOSSType);
                vatOSS.Compressed = true;
                vatOSSlst.Add(vatOSS);

                busyIndicator.IsBusy = false;
                compressed = true;
            }

            var sort = new VatOSSTableTypeSort();
            vatOSSlst.Sort(sort);
            dgVatOSSGrid.ItemsSource = vatOSSlst;
            dgVatOSSGrid.Visibility = Visibility.Visible;
        }

        private string GetMOSSTypeName(string type)
        {
            switch (type)
            {
                case CreateVatOSSFile.MOSSTYPE_001: return string.Format("{0} {1} MSID", Uniconta.ClientTools.Localization.lookup("Sales"), Uniconta.ClientTools.Localization.lookup("InventoryItems").ToLower());
                case CreateVatOSSFile.MOSSTYPE_002: return string.Format("{0} {1} MSID", Uniconta.ClientTools.Localization.lookup("Sales"), Uniconta.ClientTools.Localization.lookup("Service").ToLower());
                case CreateVatOSSFile.MOSSTYPE_003: return string.Format("{0} {1} {2}", Uniconta.ClientTools.Localization.lookup("Sales"), Uniconta.ClientTools.Localization.lookup("InventoryItems").ToLower(), Uniconta.ClientTools.Localization.lookup("BusinessCountry"));
                case CreateVatOSSFile.MOSSTYPE_004: return string.Format("{0} {1} {2}", Uniconta.ClientTools.Localization.lookup("Sales"), Uniconta.ClientTools.Localization.lookup("Service").ToLower(), Uniconta.ClientTools.Localization.lookup("BusinessCountry"));
                case CreateVatOSSFile.MOSSTYPE_005: return string.Format("{0} {1} {2}", Uniconta.ClientTools.Localization.lookup("Sales"), Uniconta.ClientTools.Localization.lookup("InventoryItems").ToLower(), Uniconta.ClientTools.Localization.lookup("ShipmentCountry"));
                case CreateVatOSSFile.MOSSTYPE_006: return Uniconta.ClientTools.Localization.lookup("NoVATRegistration");
                case CreateVatOSSFile.MOSSTYPE_007: return string.Format("{0} {1} {2}/{3}", Uniconta.ClientTools.Localization.lookup("Nothing"), Uniconta.ClientTools.Localization.lookup("Sales").ToLower(), Uniconta.ClientTools.Localization.lookup("BusinessCountry"), Uniconta.ClientTools.Localization.lookup("ShipmentCountry"));
                case CreateVatOSSFile.MOSSTYPE_008: return string.Format("{0} = 0", Uniconta.ClientTools.Localization.lookup("Report"));
                case CreateVatOSSFile.MOSSTYPE_009: return string.Format("{0}", Uniconta.ClientTools.Localization.lookup("NotActive"));
                case CreateVatOSSFile.MOSSTYPE_020: return string.Format("{0} {1} MSID", Uniconta.ClientTools.Localization.lookup("Import"), Uniconta.ClientTools.Localization.lookup("InventoryItems").ToLower());
                case CreateVatOSSFile.MOSSTYPE_021: return string.Format("{0} = 0", Uniconta.ClientTools.Localization.lookup("Report"));
                case CreateVatOSSFile.MOSSTYPE_030: return string.Format("{0} {1} MSID", Uniconta.ClientTools.Localization.lookup("Sales"), Uniconta.ClientTools.Localization.lookup("Service").ToLower());
                case CreateVatOSSFile.MOSSTYPE_031: return string.Format("{0} = 0", Uniconta.ClientTools.Localization.lookup("Report"));
                default: return Uniconta.ClientTools.Localization.lookup("Unknown");
            }
        }

        public List<VatOSSTable> UpdateValues(DebtorInvoiceLines[] listOfDebInvLines)
        {
            var listOfResults = new List<VatOSSTable>(listOfDebInvLines.Length);
            string lastVat = null;
            string lastVatName = null;
            GLVat lastGLVat = null;
            DebtorClient debtor = null;

            foreach (var invLine in listOfDebInvLines)
            {
                if (invLine.NetAmount == 0 || invLine._DCAccount == null)
                    continue;

                if (debtor?._Account != invLine._DCAccount)
                {
                    debtor = invLine.Debtor;
                    if (debtor == null || debtor._Country == api.CompanyEntity._CountryId || !Country2Language.IsEU(debtor._Country))
                        continue;
                }

                if (invLine._Vat == null)
                    continue;
                if (lastVat != invLine._Vat)
                {
                    lastVat = invLine._Vat;
                    lastGLVat = glVatCache.Get(lastVat);
                    if (lastGLVat != null)
                        lastVatName = GetMOSSTypeName(AppEnums.VATMOSSType.ToString((int)lastGLVat._MOSSType));
                }
                if (lastGLVat == null)
                    continue;

                var vatOSS = new VatOSSTable();
                vatOSS._CompanyId = api.CompanyId;
                vatOSS._Account = invLine._DCAccount;
                vatOSS._Date = invLine._Date;
                vatOSS._InvoiceNumber = invLine._InvoiceNumber;
                vatOSS._Item = invLine._Item;
                vatOSS._Vat = lastVat;
                vatOSS._MOSSType = lastGLVat._MOSSType;
                vatOSS._MOSSTypeName = lastVatName;
                vatOSS._VatCountry = lastGLVat._VatCountry;
                vatOSS._BusinessCountry = lastGLVat._BusinessCountry;
                vatOSS._ShipmentCountry = lastGLVat._ShipmentCountry;
                vatOSS._Id = lastGLVat._Id;
                vatOSS._Amount = -invLine.NetAmount;
                vatOSS._VatAmount = lastGLVat.VatAmount(vatOSS._Amount, vatOSS._Date, false, invLine.InvoiceRef._PricesInclVat ? GLVatCalculationMethod.Brutto : GLVatCalculationMethod.Netto);
                listOfResults.Add(vatOSS);
            }

            var search = new VatOSSTable();
            var sort = new VatOSSTableVatSort();
            int pos = 0;
            listOfResults.Sort(sort);

            var glVatLst = glVatCache.OrderBy(s => s._Id);
            string lastId = null;
            foreach (var glvat in glVatLst)
            {
                if (glvat._TypeSales != CreateVatOSSFile.VATTYPE_MOSS || (glvat._BusinessCountry == CountryCode.Unknown && glvat._ShipmentCountry == CountryCode.Unknown))
                    continue;

                if (lastId == glvat._Id)
                    continue;

                lastId = glvat._Id;

                search._Vat = glvat._Vat;
                pos = listOfResults.BinarySearch(search, sort);

                if (pos == -1 && vatOSSReportType == 0)
                {
                    var vatOSS = new VatOSSTable();
                    vatOSS._CompanyId = api.CompanyId;
                    vatOSS._Date = DefaultFromDate;
                    vatOSS.MOSSType = CreateVatOSSFile.MOSSTYPE_007;
                    vatOSS._MOSSTypeName = GetMOSSTypeName(vatOSS.MOSSType);
                    vatOSS.BusinessCountry = glvat._BusinessCountry;
                    vatOSS.ShipmentCountry = glvat._ShipmentCountry;
                    vatOSS.Id = glvat._Id;
                    listOfResults.Add(vatOSS);
                }
            }

            return listOfResults;
        }

        private bool CallPrevalidate()
        {
            return vatOSSHelper.PreValidate();
        }

        private IEnumerable<VatOSSTable> CallValidate(bool onlyValidate)
        {
            if (!CallPrevalidate())
                return null;

            dgVatOSSGrid.Columns.GetColumnByName("SystemInfo").Visible = true;

            var vatOSSLst = dgVatOSSGrid.GetVisibleRows() as IList<VatOSSTable>;

            vatOSSHelper.Validate(vatOSSLst, vatOSSReportType, compressed, onlyValidate);

            if (onlyValidate)
            {
                var countErr = vatOSSLst.Count(s => s.SystemInfo != CreateVatOSSFile.VALIDATE_OK);
                if (countErr == 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateNoError"), Uniconta.ClientTools.Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    UnicontaMessageBox.Show(string.Format("{0} {1}", countErr, Uniconta.ClientTools.Localization.lookup("JournalFailedValidation")), Uniconta.ClientTools.Localization.lookup("Validate"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return vatOSSLst;
        }


        private void Compress()
        {
            try
            {
                if (!CallPrevalidate())
                    return;

                var listVatOSS = vatOSSHelper.CompressVatOSS((IEnumerable<VatOSSTable>)dgVatOSSGrid.GetVisibleRows());
                if (listVatOSS == null || listVatOSS.Count == 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoLinesFound"), Uniconta.ClientTools.Localization.lookup("Warning"));
                else
                {
                    Date.Visible = false;
                    InvoiceNumber.Visible = false;
                    Item.Visible = false;
                    ItemName.Visible = false;
                    Account.Visible = false;
                    DebtorName.Visible = false; 
                    Compressed.Visible = true; 
                    SystemInfo.Visible = true;

                    var sort = new VatOSSTableTypeSort();
                    listVatOSS.Sort(sort);
                    dgVatOSSGrid.ItemsSource = listVatOSS;
                    dgVatOSSGrid.Visibility = Visibility.Visible;
                    dgVatOSSGrid.UpdateTotalSummary();

                    compressed = true;

                    CallValidate(true);

                    ribbonControl.DisableButtons("Compress");
                }
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show(e, Uniconta.ClientTools.Localization.lookup("Exception"));
                throw;
            }
        }

        private void CreateFile()
        {
            try
            {
                if (compressed == false)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompressPosting"), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }

                var listVatOSS = CallValidate(false);
                if (listVatOSS == null)
                    return;

                var countErr = listVatOSS.Count(s => s.SystemInfo != CreateVatOSSFile.VALIDATE_OK);
                var countOk = listVatOSS.Count() - countErr;

                if (listVatOSS.Count() > 0)
                {
                    if (vatOSSHelper.CreateFile(listVatOSS))
                    {
                        var msgTxt = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Exported"), countOk);
                        if (countErr > 0)
                            msgTxt = string.Concat(msgTxt, string.Format("\n{0}: {1}", Uniconta.ClientTools.Localization.lookup("Error"), countErr));
                        UnicontaMessageBox.Show(msgTxt, Uniconta.ClientTools.Localization.lookup("Message"));
                    }
                }
                else
                {
                    UnicontaMessageBox.Show(string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Exported"), countOk), Uniconta.ClientTools.Localization.lookup("Message"));
                }
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show(e, Uniconta.ClientTools.Localization.lookup("Exception"));
                throw;
            }
        }

        public override bool IsDataChaged { get { return false; } }

        private void btnSearch()
        {
            Date.Visible = true;
            InvoiceNumber.Visible = true;
            Item.Visible = true;
            Account.Visible = true;
            Compressed.Visible = false;
            SystemInfo.Visible = false;

            ribbonControl.EnableButtons("Compress");
            compressed = false;
            GetVatOSS(txtDateFrm.DateTime, txtDateTo.DateTime);
        }

        public override bool HandledOnClearFilter()
        {
            btnSearch();
            return true;
        }

        private void cmbVatOSSReportType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var cmbItems = sender as DimComboBoxEditor;
            vatOSSReportType = cmbItems.SelectedIndex;
        }
    }
}


