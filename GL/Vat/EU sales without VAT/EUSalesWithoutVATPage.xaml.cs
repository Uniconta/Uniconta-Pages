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
        public override Type TableType { get { return typeof(EUSaleWithoutVATClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class EUSalesWithoutVATPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.EUSalesWithoutVATPage; }
        }

        public EUSalesWithoutVATPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }

        string cvrNumber;
        private double sumOfAmount;

        private void InitPage()
        {
            InitializeComponent();
            localMenu.dataGrid = dgEUSalesWithoutVATGrid;
            SetRibbonControl(localMenu, dgEUSalesWithoutVATGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgEUSalesWithoutVATGrid.api = api;
            dgEUSalesWithoutVATGrid.BusyIndicator = busyIndicator;
            txtDateFrm.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 01);
            txtDateTo.DateTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            TriangularTradeAmount.Visible = false;
            cvrNumber = api.CompanyEntity._Id;
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.InvGroup) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var listEuSale = dgEUSalesWithoutVATGrid.GetVisibleRows() as IEnumerable<EUSaleWithoutVATClient>;
            var selectedItem = dgEUSalesWithoutVATGrid.SelectedItem as EUSaleWithoutVATClient;
            List<EUSaleWithoutVATClient> listOfFiles = null;

            switch (ActionType)
            {
                case "AddRow":
                    var row = dgEUSalesWithoutVATGrid.AddRow() as EUSaleWithoutVATClient;
                    if (row != null)
                    {
                        row.SetCompany(api.CompanyId);
                        row.recNr = "2";
                        row.euSaleDate = txtDateTo.DateTime;
                        row.euCountry = EUCountries.Unknown;
                        row.cvrNummer = Regex.Replace(cvrNumber, "[^0-9]", "");
                    }
                    break;

                case "DeleteRow":
                    if (selectedItem != null)
                        dgEUSalesWithoutVATGrid.DeleteRow();
                    break;

                case "Compress":
                    break;
                case "Validate":
                    if (listEuSale != null)
                    {
                        listOfFiles = ValidateEuSaleList(listEuSale.ToList(), true);
                        if (listOfFiles == null && listOfFiles.Count <= 0)
                        {
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateFail"), Uniconta.ClientTools.Localization.lookup("Error"));
                            break;
                        }
                    }
                    break;
                case "ImportFile":
#if SILVERLIGHT
                    var sfd = new System.Windows.Controls.SaveFileDialog
                    {
                        Filter = UtilFunctions.GetFilteredExtensions(FileextensionsTypes.CSV)
                    };
                    var userClickedSave = sfd.ShowDialog();
#else
                    var userClickedSave = true;
#endif
                    if (listEuSale != null)
                    {
                        try
                        {
                            var result = ValidateEuSaleList(listEuSale.ToList(), false);

                            if (result != null && result.Count > 0)
                                if (userClickedSave != true)
                                    listOfFiles = result;
                                else
                                {
#if SILVERLIGHT
                                    listOfFiles = CreateEUSaleWithoutVATFile.CreateEUSaleWithoutVATfile(result, api, sfd);
#else
                                    listOfFiles = CreateEUSaleWithoutVATFile.CreateEUSaleWithoutVATfile(result, api);
#endif
                                }
                            else
                            {
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("ValidateFail"), Uniconta.ClientTools.Localization.lookup("Error"));
                                break;
                            }
 
                            if (listOfFiles != null && listOfFiles.Count > 0)
                            {
                                var trinagleAmount = listOfFiles.Where(x => x.triangularTradeAmount != 0);

                                if (trinagleAmount != null && trinagleAmount.Count() <= 0)
                                    TriangularTradeAmount.Visible = true;
                                else
                                    TriangularTradeAmount.Visible = false;

                                dgEUSalesWithoutVATGrid.ItemsSource = listOfFiles;
                                dgEUSalesWithoutVATGrid.Visibility = Visibility.Visible;
                            }
                        }
                        catch (Exception e)
                        {
                            UnicontaMessageBox.Show(e.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
                            throw;
                        }
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async void GetInvoiceEuSale(DateTime fromDate, DateTime toDate)
        {
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

            var listOfDebInv = await api.Query<EUSaleWithoutVATClient>( propValPair);
            var listOfDebInvLines = await api.Query<DebtorInvoiceLines>(propValPair);
            var listdeb = new List<EUSaleWithoutVATClient>();

            if ((listOfDebInv != null && listOfDebInv.ToList().Count != 0) && (listOfDebInvLines != null && listOfDebInvLines.ToList().Count != 0))
                listdeb = UpdateValues(listOfDebInv.ToList(), listOfDebInvLines.ToList());

            if (listdeb == null || listdeb?.Count <= 0)
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("zeroRecords"), Uniconta.ClientTools.Localization.lookup("Warning"));

            dgEUSalesWithoutVATGrid.ItemsSource = listdeb;
            dgEUSalesWithoutVATGrid.Visibility = Visibility.Visible;
        }

        public List<EUSaleWithoutVATClient> UpdateValues(List<EUSaleWithoutVATClient> listOfDebInv, List<DebtorInvoiceLines> listOfDebInvLines)
        {
            var listOfResults = new List<EUSaleWithoutVATClient>();

            foreach (var invoice in listOfDebInv)
            {
                var inv = new EUSaleWithoutVATClient();
                inv.SetCompany(invoice.CompanyId);
                inv.recNr = "2";

                inv.euSaleDate = invoice.Date;
                if (inv.euSaleDate == DateTime.MinValue)
                    continue;

                var debtor = invoice.Debtor;

                if (debtor == null)
                    continue;

                inv._DCAccount = invoice._DCAccount;
                

                CountryCode? country = debtor._Country;
                if (country.Value == api.CompanyEntity._CountryId)
                    continue;

                if (Uniconta.Common.Enums.Country2Language.IsEU(country.Value))
                    inv.euCountry = (EUCountries)Enum.Parse(typeof(EUCountries), country.Value.ToString(), true);
                else if (country.Value == CountryCode.Unknown)
                    inv.euCountry = EUCountries.Unknown;
                else
                    continue;

                inv.VATBuyerNummer = debtor.CompanyRegNo;

                if (string.IsNullOrWhiteSpace(inv.vatBuyerNummer))
                    inv.systemField = string.Format(Uniconta.ClientTools.Localization.lookup("MissingOBJ"), Uniconta.ClientTools.Localization.lookup("VATNumber"));

                foreach (var invLine in listOfDebInvLines)
                {
                    var item = invLine.InvItem;

                    if (item == null)
                        continue;

                    if (invLine.InvoiceNumber != invoice.InvoiceNumber || invoice._DCAccount != invLine._DCAccount)
                        continue;

                    inv.interntRefNr = "X";
                    inv._InvoiceNumber = invoice._InvoiceNumber;

                    switch (item._ItemType)
                    {
                        case (byte)Uniconta.DataModel.ItemType.Service:
                            inv.serviceAmount += NetAmount(invLine);
                            break;
                        default:
                            inv.itemAmount += NetAmount(invLine);
                            break;
                    }
                }

                if (inv.itemAmount == 0 && inv.serviceAmount == 0)
                    continue;

                sumOfAmount = sumOfAmount + inv.ItemAmount + inv.ServiceAmount;

                listOfResults.Add(inv);
            }
            return listOfResults;
        }

        public double NetAmount(DebtorInvoiceLines line)
        {
            var p = line._Price * line.InvoiceQty;
            if (p != 0d)
            {
                if (line._DiscountPct != 0 || line._EndDiscountPct != 0)
                    p = p * (100d - line._DiscountPct) * (100d - line._EndDiscountPct) / 10000d;
                return GLVat.Round(p, (line._Flags & 0x80) != 0);
            }
            else if (line._EndDiscountPct == 0d)
                return line._AmountEntered;
            else
                return GLVat.Round(line._AmountEntered * (100d - line._EndDiscountPct) / 100d, (line._Flags & 0x80) != 0);
        }

        public List<EUSaleWithoutVATClient> ValidateEuSaleList(List<EUSaleWithoutVATClient> listOfEU, bool isOnlyValidate)
        {
            if (!Country2Language.IsEU(api.CompanyEntity._CountryId))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("AccountCountryNotEu"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return null;
            }
            if (string.IsNullOrWhiteSpace(cvrNumber))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MissingOBJ"), Uniconta.ClientTools.Localization.lookup("VATNumber")), Uniconta.ClientTools.Localization.lookup("Warning"));
                return null;
            }

            var companyCountry = (EUCountries)Enum.Parse(typeof(EUCountries), api.CompanyEntity._CountryId.ToString(), true);
            var notValidated = new List<IntrastatClient>();
            var ikkeMedtaget = 0;

            foreach (var euSale in listOfEU)
            {
                euSale.cvrNummer = Regex.Replace(cvrNumber, "[^0-9]", "");

                var hasErrors = false;
                euSale.systemInfo = "";
                if (euSale.itemAmount == 0 && euSale.serviceAmount == 0)
                {
                    hasErrors = true;
                    euSale.systemInfo += Uniconta.ClientTools.Localization.lookup("NoValues") + ".\n";
                }
                if (euSale.EUCountry == EUCountries.Unknown)
                {
                    hasErrors = true;
                    euSale.systemInfo = euSale.systemInfo + Uniconta.ClientTools.Localization.lookup("CountryNotSet") + ".\n";
                }
                if (euSale.EUCountry == companyCountry)
                {
                    hasErrors = true;
                    euSale.systemInfo = euSale.systemInfo + Uniconta.ClientTools.Localization.lookup("OwnCountryProblem") + ".\n";
                }

                var vatBuyerNummer = euSale.vatBuyerNummer;

                if (vatBuyerNummer == null || vatBuyerNummer.Length == 0)
                {
                    hasErrors = true;
                    euSale.systemInfo += string.Format(Uniconta.ClientTools.Localization.lookup("MissingOBJ"), Uniconta.ClientTools.Localization.lookup("VATNumber")) + ".\n";
                }
                else
                {
                    if (char.IsLetter(vatBuyerNummer.Substring(0, 2)[0]) && char.IsLetter(vatBuyerNummer.Substring(0, 2)[1]))
                        vatBuyerNummer = vatBuyerNummer.Substring(2);
                    
                     vatBuyerNummer = Regex.Replace(vatBuyerNummer, " ", "");

                    if (vatBuyerNummer.Length > 12)
                    {
                        hasErrors = true;
                        euSale.systemInfo += string.Format(Uniconta.ClientTools.Localization.lookup("FieldTooLongOBJ"), Uniconta.ClientTools.Localization.lookup("VATNumber")) + ".\n";
                    }
                    else
                        euSale.vatBuyerNummer = vatBuyerNummer;
                }

                if (string.IsNullOrWhiteSpace(euSale.systemInfo))
                    euSale.systemInfo = Uniconta.ClientTools.Localization.lookup("OK");

                if (hasErrors)
                    ikkeMedtaget++;

            }

            if (ikkeMedtaget <= 0)
            {
                dgEUSalesWithoutVATGrid.ItemsSource = listOfEU;
                dgEUSalesWithoutVATGrid.Visibility = Visibility.Visible;
                return listOfEU;
            }

            if (isOnlyValidate)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ValidateFailInLines"), ikkeMedtaget), Uniconta.ClientTools.Localization.lookup("Error"));
            }
            else if (!isOnlyValidate)
            {
                var messageOfNotIncluded = string.Format(Uniconta.ClientTools.Localization.lookup("SkipNotValidatedLines"), ikkeMedtaget);

#if !SILVERLIGHT
                var result = UnicontaMessageBox.Show(messageOfNotIncluded, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.YesNo);
                if (MessageBoxResult.No == result)
                    return null;
#else
                var result = UnicontaMessageBox.Show(messageOfNotIncluded, Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OKCancel);
                 if (MessageBoxResult.Cancel == result)
                    return null;
#endif

            }

            dgEUSalesWithoutVATGrid.ItemsSource = listOfEU;
            dgEUSalesWithoutVATGrid.Visibility = Visibility.Visible;

            return listOfEU?.Count > 0 ? listOfEU : null;
        }

        public override bool IsDataChaged { get { return false; } }

        public override bool HandledOnClearFilter()
        {
            BtnSearch_OnClick(null, null);
            return true;
        }

        private void BtnSearch_OnClick(object sender, RoutedEventArgs e)
        {
            TriangularTradeAmount.Visible = false;
            GetInvoiceEuSale(txtDateFrm.DateTime, txtDateTo.DateTime);
        }
    }
}


