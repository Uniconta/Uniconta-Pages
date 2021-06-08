using UnicontaClient.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Enums;
using Uniconta.DataModel;
using static UnicontaClient.Pages.CreateIntraStatFilePage;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EUSalesWithoutVATGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EUSaleWithoutVAT); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class EUSalesWithoutVATPage : GridBasePage
    {
        SQLTableCache<GLVat> glVatCache;
     
        public override string NameOfControl
        {
            get { return TabControls.EUSalesWithoutVATPage; }
        }

        public EUSalesWithoutVATPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        private CreateEUSaleWithoutVATFile euSalesHelper;
        private string companyRegNo;
        private CountryCode companyCountryId;
        private double sumOfAmount;
        private bool compressed;
        static DateTime DefaultFromDate, DefaultToDate;


        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgEUSalesWithoutVATGrid;
            SetRibbonControl(localMenu, dgEUSalesWithoutVATGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgEUSalesWithoutVATGrid.api = api;
            dgEUSalesWithoutVATGrid.BusyIndicator = busyIndicator;
            dgEUSalesWithoutVATGrid.ShowTotalSummary();
            txtDateFrm.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 01);
            txtDateTo.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            TriangularTradeAmount.Visible = false;
            companyRegNo = Regex.Replace(api.CompanyEntity._Id ?? string.Empty, "[^0-9]", "");
            companyCountryId = api.CompanyEntity._CountryId;

            euSalesHelper = new CreateEUSaleWithoutVATFile(api, companyRegNo, companyCountryId);

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
            StartLoadCache();
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
            var listEuSale = dgEUSalesWithoutVATGrid.GetVisibleRows() as IEnumerable<EUSaleWithoutVAT>;
            var selectedItem = dgEUSalesWithoutVATGrid.SelectedItem as EUSaleWithoutVAT;

            switch (ActionType)
            {
                case "DeleteRow":
                    if (selectedItem != null)
                        dgEUSalesWithoutVATGrid.DeleteRow();
                    break;

                case "Validate":
                    if (listEuSale != null && listEuSale.Count() > 0)
                        CallValidate(true);
                    break;

                case "Compress":
                    if (listEuSale != null && listEuSale.Count() > 0)
                        Compress();
                    break;

                case "Search":
                    if (listEuSale != null)
                        btnSearch();
                    break;

                case "CreateFile":
                    if (listEuSale != null && listEuSale.Count() > 0)
                        CreateFile();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async void GetInvoiceEuSale(DateTime fromDate, DateTime toDate)
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

            var vatEUList = glVatCache.Where(s => s._TypeSales == "s3" || s._TypeSales == "s4" || s._TypeSales == "s7").Select(x => x._Vat).Distinct();
            if (vatEUList != null && vatEUList.Count() > 0)
            {
                var strLst = string.Join(";", vatEUList);
                propValPair.Add(PropValuePair.GenereteWhereElements(nameof(DebtorInvoiceLines.Vat), typeof(string), strLst));
            }
            var listOfDebInvLines = await api.Query<DebtorInvoiceLines>(propValPair);
            List<EUSaleWithoutVAT> listdeb;

            if (listOfDebInvLines != null && listOfDebInvLines.Length != 0)
                listdeb = UpdateValues(listOfDebInvLines);
            else
                listdeb = new List<EUSaleWithoutVAT>();

            busyIndicator.IsBusy = false;

            if (listdeb.Count == 0)
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"));

            dgEUSalesWithoutVATGrid.ItemsSource = listdeb;
            dgEUSalesWithoutVATGrid.Visibility = Visibility.Visible;
        }

        public List<EUSaleWithoutVAT> UpdateValues(DebtorInvoiceLines[] listOfDebInvLines)
        {
            var listOfResults = new List<EUSaleWithoutVAT>(listOfDebInvLines.Length);
            var companyCountryId = api.CompanyEntity._CountryId;

            bool triangularTrade = false;
            string lastVat = null;
            DebtorClient debtor = null;
            string debtorCVR = null;
            bool DebtorOk = false;

            foreach (var invLine in listOfDebInvLines)
            {
                var netAmount = invLine.NetAmount;
                if (netAmount == 0 || invLine._DCAccount == null)
                    continue;

                if (debtor?._Account != invLine._DCAccount)
                {
                    debtor = invLine.Debtor;
                    if (debtor == null || debtor._Country == companyCountryId || !Country2Language.IsEU(debtor._Country))
                    {
                        DebtorOk = false;
                        continue;
                    }

                    DebtorOk = true;
                    debtorCVR = debtor._LegalIdent;
                    if (debtorCVR != null)
                    {
                        long value;
                        if (!long.TryParse(debtorCVR, out value))
                        {
                            debtorCVR = Regex.Replace(debtorCVR, @"[-/ ]", "");
                        }

                        if (char.IsLetter(debtorCVR[0]) && char.IsLetter(debtorCVR[1]))
                            debtorCVR = debtorCVR.Substring(2);
                    }
                }

                if (! DebtorOk)
                    continue;

                if (lastVat != invLine._Vat)
                {
                    lastVat = invLine._Vat;
                    triangularTrade = glVatCache.Get(lastVat)?._TypeSales == "s7";
                }

                var invoice = new EUSaleWithoutVAT();
                invoice._CompanyId = api.CompanyId;
                invoice.CompanyRegNo = companyRegNo;
                invoice.RecordType = "2";
                invoice.ReferenceNumber = "X";
                invoice._Account = invLine._DCAccount;
                invoice._Date = invLine._Date;
                invoice._InvoiceNumber = invLine._InvoiceNumber;
                invoice._Item = invLine._Item;
                invoice._Vat = lastVat;
                invoice._Amount = -netAmount;
                invoice._IsTriangularTrade = triangularTrade;
                invoice._DebtorRegNoFile = debtorCVR;

                if (invLine._Item != null)
                    invoice._ItemOrService = invLine.InvItem._ItemType == 1 ? ItemOrServiceType.Service : ItemOrServiceType.Item;

                sumOfAmount += invoice._Amount;
                listOfResults.Add(invoice);
            };

            return listOfResults;
        }

        private bool CallPrevalidate()
        {
            return euSalesHelper.PreValidate();
        }

        private IEnumerable<EUSaleWithoutVAT> CallValidate(bool onlyValidate)
        {
            if (!CallPrevalidate())
                return null;

            dgEUSalesWithoutVATGrid.Columns.GetColumnByName("SystemInfo").Visible = true;

            var listOfEU = (IEnumerable<EUSaleWithoutVAT>)dgEUSalesWithoutVATGrid.GetVisibleRows();
            euSalesHelper.Validate(listOfEU, compressed, onlyValidate);

            if (onlyValidate)
            {
                var countErr = listOfEU.Count(s => s.SystemInfo != CreateEUSaleWithoutVATFile.VALIDATE_OK);
                if (countErr == 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateNoError"), Uniconta.ClientTools.Localization.lookup("Validation"), MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ValidateFailInLines"), countErr), Uniconta.ClientTools.Localization.lookup("Validation"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return listOfEU;
        }


        private void Compress()
        {
            try
            {
                if (!CallPrevalidate())
                    return;

                var listOfEU = euSalesHelper.CompressEUsale((IEnumerable<EUSaleWithoutVAT>)dgEUSalesWithoutVATGrid.GetVisibleRows());
                if (listOfEU == null || listOfEU.Count == 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoLinesFound"), Uniconta.ClientTools.Localization.lookup("Warning"));
                else
                {
                    var cols = dgEUSalesWithoutVATGrid.Columns;
                    cols.GetColumnByName("InvoiceNumber").Visible = false;
                    cols.GetColumnByName("Item").Visible = false;
                    cols.GetColumnByName("Vat").Visible = false;
                    cols.GetColumnByName("ItemName").Visible = false;
                    cols.GetColumnByName("Compressed").Visible = true;
                    cols.GetColumnByName("TriangularTradeAmount").Visible = true;
                    cols.GetColumnByName("SystemInfo").Visible = true;
                    cols.GetColumnByName("ItemOrService").Visible = false;
                    cols.GetColumnByName("IsTriangularTrade").Visible = false;
                    dgEUSalesWithoutVATGrid.ItemsSource = listOfEU;
                    dgEUSalesWithoutVATGrid.Visibility = Visibility.Visible;
                    dgEUSalesWithoutVATGrid.UpdateTotalSummary();

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

                var listOfEU = CallValidate(false);
                if (listOfEU == null)
                    return;

                var countErr = listOfEU.Count(s => s.SystemInfo != CreateEUSaleWithoutVATFile.VALIDATE_OK);
                var countOk = listOfEU.Count() - countErr;

                if (listOfEU.Count() > 0)
                {
                    if (euSalesHelper.CreateFile(listOfEU))
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
            var cols = dgEUSalesWithoutVATGrid.Columns;
            cols.GetColumnByName("TriangularTradeAmount").Visible = false;
            cols.GetColumnByName("InvoiceNumber").Visible = true;
            cols.GetColumnByName("Item").Visible = true;
            cols.GetColumnByName("ItemName").Visible = true;
            cols.GetColumnByName("Vat").Visible = true;
            cols.GetColumnByName("Compressed").Visible = false;
            cols.GetColumnByName("ItemOrService").Visible = true;
            cols.GetColumnByName("IsTriangularTrade").Visible = true;
            ribbonControl.EnableButtons("Compress");
            compressed = false;
            GetInvoiceEuSale(txtDateFrm.DateTime, txtDateTo.DateTime);
        }

        public override bool HandledOnClearFilter()
        {
            btnSearch();
            return true;
        }
    }
}


