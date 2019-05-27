using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;
using UnicontaClient.Models;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using MessageBox = System.Windows.Forms.MessageBox;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class Constants
    {
        public const double SalesLimitInvoiceAmount = 1000d;
    }
    public class VatReportEstoniaGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(VatSumOperationReport); }
        }

        public override bool Readonly
        {
            get { return false; }
        }
    }

    public class EEInfLinesGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(EEInfLines);
    }

    public partial class VatReportEstonia : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.VatReportEstonia; }
        }

        #region Member variables
        protected List<VatSumOperationReport> vatSumOperationLst;
        #endregion

        #region Properties
        private List<VatSumOperationReport> VatSumOperationLst
        {
            get
            {
                return vatSumOperationLst;
            }

            set
            {
                vatSumOperationLst = value;
            }
        }

        private DateTime fromDate { get; set; }
        private DateTime toDate { get; set; }

        private List<EEInfLines> InfALines { get; set; }
        private List<EEInfLines> InfBLines { get; set; }
        #endregion

        public VatReportEstonia(List<VatSumOperationReport> vatSumOperationLst, DateTime fromDate, DateTime toDate) : base(null)
        {

            this.fromDate = fromDate;
            this.toDate = toDate;
            InitializeComponent();
          
            dgVatReportEstonia.api = api;
            dgVatReportEstonia.BusyIndicator = busyIndicator;

            if (!vatSumOperationLst.Any(vsop => vsop._Line == 8))
                vatSumOperationLst.Add(new VatSumOperationReport { _Line = 8 });
            if (!vatSumOperationLst.Any(vsop => vsop._Line == 21))
                vatSumOperationLst.Add(new VatSumOperationReport { _Line = 21 });
            if (!vatSumOperationLst.Any(vsop => vsop._Line == 22))
                vatSumOperationLst.Add(new VatSumOperationReport { _Line = 22 });
            if (!vatSumOperationLst.Any(vsop => vsop._Line == 23))
                vatSumOperationLst.Add(new VatSumOperationReport { _Line = 23 });
            if (!vatSumOperationLst.Any(vsop => vsop._Line == 24))
                vatSumOperationLst.Add(new VatSumOperationReport { _Line = 24 });
            vatSumOperationLst = vatSumOperationLst.OrderBy(x => x._Line).ToList();

            this.VatSumOperationLst = vatSumOperationLst;
            foreach (var rec in VatSumOperationLst)
            {
                string s, opr;

                var lin = rec._Line;

                switch (lin)
                {
                    case 1: s = "20% määraga maksustatavad toimingud ja tehingud"; opr = "1"; break;
                    case 2: s = "9% määraga maksustatavad toimingud ja tehingud"; opr = "2"; break;
                    case 3: s = "0% määraga maksustatavad toimingud ja tehingud"; opr = "3"; break;
                    case 4: s = "kauba ühendusesisene käive ja teise liikmesriigi maksukohuslaesele /piiratud maksukohuslasele osutatud teenuste käive kokku sh"; opr = "3.1"; break;
                    case 5: s = "kauba ühendusesisene käive"; opr = "3.1.1"; break;
                    case 6: s = "kauba eksport,sh"; opr = "3.2"; break;
                    case 7: s = "käibemaksutagastusega müük reisijale"; opr = "3.2.1"; break;
                    case 8:
                        s = "Käibemaks kokku"; opr = "4";
                        rec._Amount = VatSumOperationLst[0]._Amount + VatSumOperationLst[1]._Amount;
                        break;
                    case 9: s = "Impordilt tasumisele kuuluv käibemaks"; opr = "4.1"; break;
                    case 10:
                        s = "Kokku sisendkäibemaksu summa,mis on seadusega lubatud maha arvata,sh"; opr = "5";
                        break;
                    case 11:
                        s = "Impordilt tasutud või tasumisele kuuluv käibemaks"; opr = "5.1";
                        break;
                    case 12: s = "Põhivara soetamiselt tasutud või tasumisele kuuluv käibemaks"; opr = "5.2"; break;
                    case 13: s = "ettevõtluses (100%) kasutatava sõiduauto soetamiselt ja sellise sõiduauto tarbeks kaupade soetamiselt ja teenuste saamiselt tasutud või tasumisele kuuluv KM"; opr = "5.3"; break;
                    case 14: s = "osaliselt ettevõtluses  kasutatava sõiduauto soetamiselt ja sellise  sõiduauto tarbeks kaupade soetamiselt ja teenuste saamiselt tasutud või tasumisele kuuluv KM"; opr = "5.4"; break;
                    case 15: s = "Kauba ühendusesisene soetamine ja teise liikmesriigi maksukohustuslaselt saadud teenused kokku"; opr = "6"; break;
                    case 16: s = "Kauba ühendusesisene soetamine ja teise liikmesriigi maksukohustuslaselt saadud teenused kokku"; opr = "6.1"; break;
                    case 17: s = "Muu kauba soetamine ja teenuse saamine, mida maksustatakse käibemaksuga, sh"; opr = "7"; break;
                    case 18: s = "erikorra alusel maksustatava kinnisasja, metalljäätmete, väärismetalli ja metalltoodete soetamine (KMS §41)"; opr = "7.1"; break;
                    case 19: s = "Maksuvaba käiveMaksuvaba käive"; opr = "8"; break;
                    case 20: s = "Erikorra alusel maksustatava kinnisasja, metallijäätmete, väärismetalli ja metalltoodete käive (KMS §41) ning teises liikmesriigis paigaldatava või kokkupandava kauba maksustatav vöörtus"; opr = "9"; break;
                    case 21: s = "Täpsustused +"; opr = "10"; break;
                    case 22: s = "Täpsustused -"; opr = "11"; break;
                    case 23:
                        s = "Tasumisele kuuluv käibemaks (4+4.1-5+10-11)";
                        var posamount = VatSumOperationLst[7]._Amount - VatSumOperationLst[8]._Amount + VatSumOperationLst[20]._Amount - VatSumOperationLst[21]._Amount;
                        rec._Amount = posamount > 0 ? posamount : 0;
                        opr = "12";

                        break;
                    case 24:
                        s = "Enammakstud käibemaks (4+4.1-5+10-11)";
                        var negamount = VatSumOperationLst[7]._Amount - VatSumOperationLst[8]._Amount + VatSumOperationLst[20]._Amount - VatSumOperationLst[21]._Amount;
                        rec._Amount = negamount < 0 ? negamount : 0;
                        opr = "13";

                        break;
                    default: s = null; opr = null; break;
                }
                rec._Text = s;
                rec._UnicontaOperation = opr;
            }

            localMenu.dataGrid = dgVatReportEstonia;
            localMenu.AddRibbonItems(CreateRibbonItems());


            SetRibbonControl(localMenu, dgVatReportEstonia);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;

            dgVatReportEstonia.ItemsSource = VatSumOperationLst;
            dgVatReportEstonia.Visibility = Visibility.Visible;

            txtDateFrm.DateTime = fromDate;
            txtDateTo.DateTime = toDate;

            ribbonControl.DisableButtons(new string[] { "Save" });
        }

        private async Task BindGrid()
        {
            InfALines = new List<EEInfLines>();
            InfBLines = new List<EEInfLines>();
            if (cbInfA.IsChecked.HasValue && cbInfA.IsChecked.Value)
            {
                var infAGenerator = new InfAGeneration(api, fromDate, toDate);
                busyIndicator.IsBusy = true;
                reportGrid.api = api;
                InfALines = await infAGenerator.CalculateInfArved();

                reportGrid.BusyIndicator = busyIndicator;
                reportGrid.ItemsSource = InfALines;
                busyIndicator.IsBusy = false;
                reportGrid.Visibility = Visibility.Visible;

                reportGrid.Columns[0].Header = Uniconta.ClientTools.Localization.lookup("VATNumber");
                reportGrid.Columns[1].Header = Uniconta.ClientTools.Localization.lookup("Name");
                reportGrid.Columns[2].Header = Uniconta.ClientTools.Localization.lookup("InvoiceNumber");
                reportGrid.Columns[3].Header = Uniconta.ClientTools.Localization.lookup("InvoiceDate");
                reportGrid.Columns[8].Header = Uniconta.ClientTools.Localization.lookup("Error");
            }
            if (cbInfB.IsChecked.HasValue && cbInfB.IsChecked.Value)
            {
                var infBGenerator = new InfBGeneration(api, fromDate, toDate);
                reportGridB.BusyIndicator = busyIndicator;

                busyIndicator.IsBusy = true;
                InfBLines = await infBGenerator.CalculateInfOstuarved();
                reportGridB.ItemsSource = InfBLines;
                busyIndicator.IsBusy = false;
                reportGridB.Visibility = Visibility.Visible;

                reportGridB.Columns[0].Header = Uniconta.ClientTools.Localization.lookup("VATNumber");
                reportGridB.Columns[1].Header = Uniconta.ClientTools.Localization.lookup("Name");
                reportGridB.Columns[2].Header = Uniconta.ClientTools.Localization.lookup("InvoiceNumber");
                reportGridB.Columns[3].Header = Uniconta.ClientTools.Localization.lookup("InvoiceDate");
                reportGridB.Columns[7].Header = Uniconta.ClientTools.Localization.lookup("Error");
            }
        }

        List<TreeRibbon> CreateRibbonItems()
        {
            var ribbonItems = new List<TreeRibbon>();

            var editRowItem = new TreeRibbon();
            editRowItem.ActionName = "EditAll";
            editRowItem.Name = "Muuda";
            editRowItem.LargeGlyph = LargeIcon.Edit.ToString();

            var saveRowItem = new TreeRibbon();
            saveRowItem.ActionName = "Save";
            saveRowItem.Name = Uniconta.ClientTools.Localization.lookup("Save");
            saveRowItem.LargeGlyph = LargeIcon.Save.ToString();

            var exportToXml = new TreeRibbon();
            exportToXml.ActionName = "ExportToXml";
            exportToXml.Name = "Expordi XMLi";
            exportToXml.LargeGlyph = LargeIcon.Generate.ToString();

            ribbonItems.Add(editRowItem);
            ribbonItems.Add(saveRowItem);
            ribbonItems.Add(exportToXml);

            return ribbonItems;
        }

        private async void btnSearch_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DateTime fromDate, toDate;
            reportGrid.ItemsSource = null;
            reportGridB.ItemsSource = null;
            if (txtDateFrm.Text == string.Empty)
                fromDate = DateTime.MinValue;
            else
                fromDate = txtDateFrm.DateTime.Date;

            if (txtDateTo.Text == string.Empty)
                toDate = DateTime.MinValue;
            else
                toDate = txtDateTo.DateTime.Date;

            await BindGrid();
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = reportGrid.SelectedItem as EEInfLines;
            switch (ActionType)
            {
                case "EditAll":
                    reportGrid.MakeEditable();
                    reportGridB.MakeEditable();
                    dgVatReportEstonia.MakeEditable();
                    api.AllowBackgroundCrud = false;
                    ribbonControl.EnableButtons(new string[] { "Save" });
                    break;
                case "Save":
                    reportGrid.SaveData();
                    reportGridB.SaveData();
                    reportGrid.Readonly = true;
                    reportGridB.Readonly = true;
                    reportGrid.tableView.CloseEditor();
                    reportGridB.tableView.CloseEditor();
                    api.AllowBackgroundCrud = true;
                    ribbonControl.DisableButtons(new string[] { "Save" });
                    break;
                case "ExportToXml":
                    var sfd = Uniconta.ClientTools.Util.UtilDisplay.LoadSaveFileDialog;
                    var noOfCar50 = int.Parse(tbNumberOfCars50.Text);
                    var noOfCar100 = int.Parse(tbNumberOfCars100.Text);
                    if (!ValidateLinesForXml())
                        return;

                    sfd.Filter = "XML failid(*.xml)| *.xml";
                    sfd.FileName = "KMD_Aruanne_" + fromDate.Year.ToString() + "_" + fromDate.Month.ToString() + ".xml";
                    if (sfd.ShowDialog() == true)
                        new XmlExporter(api).CreateXmlFile(
#if !SILVERLIGHT
                            File.Create(sfd.FileName),
#else
                            sfd.OpenFile(), 
#endif
                            fromDate, toDate, VatSumOperationLst, InfALines, InfBLines, noOfCar50, noOfCar100, !cbInfA.IsChecked.Value, !cbInfB.IsChecked.Value);

                    break;
                default:
                    break;
            }
        }

        private bool ValidateLinesForXml()
        {
            if (InfALines.Any(al => String.IsNullOrEmpty(al.AccountRegNo)))
            {
                UnicontaMessageBox.Show("Müügiarvete aruandes on registrikood/isikukood puudu. Kontrollige väljad üle.", Uniconta.ClientTools.Localization.lookup("Warning"));
                return false;
            }
            if (InfBLines.Any(bl => String.IsNullOrEmpty(bl.AccountRegNo)))
            {
                UnicontaMessageBox.Show("Ostuarvete aruandes on registrikood/isikukood puudu. Kontrollige väljad üle.", Uniconta.ClientTools.Localization.lookup("Warning"));
                return false;
            }
            return true;
        }

        private async void VatReportEstonia_Loaded(object sender, RoutedEventArgs e)
        {
            await BindGrid();
        }
    }

#region Data classes
    public class EEInfLines
    {
        public EEInfLines()
        {
        }

        public EEInfLines(DebtorClient account, DebtorInvoiceClient invoice)
        {
            ErrorMessage = String.Empty;
            Account = account.Account;
            AccountName = account.Name;
            AccountRegNo = account.VatNumber;
            if (String.IsNullOrEmpty(AccountRegNo))
                ErrorMessage += "Kirjel puudub registrikood või isikukood";

            InvoiceDate = invoice.Date;
            InvoiceNumber = invoice.InvoiceNumber;
            InfAInvoiceSumWoVat = invoice.NetAmount;
        }

        public EEInfLines(CreditorClient account, CreditorInvoiceClient invoice)
        {
            ErrorMessage = String.Empty;
            Account = account.Account;
            AccountName = account.Name;
            AccountRegNo = account.VatNumber;
            if (String.IsNullOrEmpty(AccountRegNo))
                ErrorMessage += "Kirjel puudub registrikood või isikukood";

            InvoiceDate = invoice.Date;
            InvoiceNumber = invoice.InvoiceNumber;
            InfBInvoiceSumWVat = invoice.TotalAmount;
        }

        public string Account { get; set; }

        public string AccountName { get; set; }

        public long InvoiceNumber { get; set; }

        public DateTime InvoiceDate { get; set; }

        /// <summary>
        /// Arve kogusumma ilma käibemaksuta INF A osas
        /// Erisus 01 puhul sisestatakse siis täisarve väärtus või siis arvutatakse välja erisuse maksumäära osa - muud maksud kui on / 
        /// Invoice net amount without VAT for INF A
        /// Exception 01 here it will be full amount 
        /// </summary>
        public double InfAInvoiceSumWoVat { get; set; }

        public double InfBInvoiceSumWVat { get; set; }

        public string InfAVatRate { get; set; }

        public double InfBInvoiceVatAmount { get; set; }

        /// <summary>
        /// Maksustatav määr / Tax rate
        /// </summary>
        public double InfATaxableAmount { get; set; }

        public double InfBPeriodInVatAmoun { get; set; }

        /// <summary>
        ///  Hetke perioodil tekkinud maksustatav sisendkäive
        /// </summary>
        public double InfATaxableRevenue { get; set; }

        /// <summary>
        ///  Erisuse kood / Exception code
        /// </summary>
        public string ExceptionCode { get; set; }

        /// <summary>
        /// Ettevõtte registrikood / Company registry code
        /// </summary>
        public string AccountRegNo { get; set; }

        public int LineNr { get; set; }

        public string ErrorMessage { get; set; }
    }
#endregion

#region Generator classes
    public class InfBaseGeneration
    {
        internal CrudAPI _api;
        internal double currentAccountTotalInvoicesSum = 0, currentAccountTotalCreditInvoicesSum = 0;
        internal DateTime periodFromDate, periodToDate;

        internal List<EEInfLines> InfRead { get; set; }
        public InfBaseGeneration(BaseAPI api, DateTime fromDate, DateTime toDate)
        {
            _api = api as CrudAPI;
            periodFromDate = fromDate;
            periodToDate = toDate;
        }

        /// <summary>
        /// Gets creditor
        /// </summary>
        /// <param name="account">Account reference</param>
        /// <returns>Creditor that is referenced by account</returns>
        internal async Task<CreditorClient> GetCreditorByAccount(string account)
        {
            var crit = new List<PropValuePair>();
            crit.Add(PropValuePair.GenereteWhereElements("Account", typeof(string), account));
            var debtorAccount = await _api.Query<CreditorClient>(crit);
            return debtorAccount.FirstOrDefault();
        }

        private const double VatRate20 = 20;
        private const double VatRate9 = 9;
        private const double VatRate0 = 0;
        internal bool HasExceptionCode(long exCode, GLTransClient glt, GLTransClient[] glTrans = null)
        {
            var result = false;
            switch (exCode)
            {
                case 1:
                    if (glt.VatCode._ModelBoxId1 == 1)
                        return true;
                    break;
                case 2:
                    if (glt.VatCode._ModelBoxId1 == 2)
                        result = true;
                    break;
                case 3:
                    // Kui pole kaasa antud kandeid või erikoodides ei sisaldu antud VATi koodi, siis välja ära.
                    if (glTrans == null || glTrans.Length == 0)
                        return result;
                    // Mul on vaja teada maksukoodi, määra ja summat kandel ja seda igal eraldi
                    // Siis saaks kokku võtta määrad, kui on erinevad, näiteks 9 ja 20 ja üks on negatiivne, siis on erisus
                    // Kui on 9 ja 20 ja lisaks on ka olemas 0, siis on ka erisus
                    if (!(glTrans.Any(g => g.VatCode.Rate == VatRate9) && glTrans.Any(g => g.VatCode.Rate == VatRate20)))
                    {
                        return result;
                    }

                    if (glTrans.FirstOrDefault(t => t.VatCode.Rate == VatRate0) != null)
                    {
                        result = true;
                    }
                    else
                    {
                        var positive20 = glTrans.FirstOrDefault(g => g.Amount > 0 && !String.IsNullOrEmpty(g.Vat) && g.VatCode.Rate == VatRate20);
                        if (positive20 != null)
                        {
                            var negative9 = glTrans.FirstOrDefault(g => g.Amount < 0 && !String.IsNullOrEmpty(g.Vat) && g.VatCode.Rate == VatRate9);
                            return negative9 != null;
                        }

                        var positive9 = glTrans.FirstOrDefault(g => g.Amount > 0 && !String.IsNullOrEmpty(g.Vat) && g.VatCode.Rate == VatRate9);
                        if (positive9 != null)
                        {
                            var negative20 = glTrans.FirstOrDefault(g => g.Amount < 0 && !String.IsNullOrEmpty(g.Vat) && g.VatCode.Rate == VatRate20);
                            return negative20 != null;
                        }
                    }

                    return result;
                default:
                    return result;
            }
            return result;
        }

        internal Dictionary<string, string> ExceptionCodeCheck(GLTransClient[] glTrans = null, bool isPurchaseInvoice = false, CreditorInvoiceClient creditorInvoice = null)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (glTrans != null)
            {
                // 01 on keeruline, aga see on tehtud nüüd
                // 02 on täiesti konkreetse maksukoodiga saadav
                // 03 on teatud reegli alusel võimalik kokku panna.
                foreach (var glt in glTrans)
                {
                    if (glt.Vat == null)
                        continue;
                    if (result.ContainsKey(glt.Vat))
                        continue;
                    var exceptionCodes = new List<string>();

                    if (HasExceptionCode(1, glt, glTrans))
                        exceptionCodes.Add(1.ToString("D2"));
                    if (HasExceptionCode(2, glt))
                        exceptionCodes.Add(2.ToString("D2"));
                    if (HasExceptionCode(3, glt, glTrans))
                        exceptionCodes.Add(3.ToString("D2"));

                    if (exceptionCodes.Count != 0)
                    {
                        result.Add(glt.Vat, String.Join(",", exceptionCodes));
                    }
                }
            }
            else if (creditorInvoice != null)
            {
                if (creditorInvoice.VatCode != null && creditorInvoice.VatCode.ModelBoxId == 11)
                    result.Add(creditorInvoice.Vat, "11");
                else if (creditorInvoice.Vat2Code != null && creditorInvoice.Vat2Code.ModelBoxId == 11)
                    result.Add(creditorInvoice.Vat2, "11");
                else if (creditorInvoice.Vat3Code != null && creditorInvoice.Vat3Code.ModelBoxId == 11)
                    result.Add(creditorInvoice.Vat3, "11");

                if (creditorInvoice.VatCode != null && creditorInvoice.VatCode.ModelBoxId == 12)
                    result.Add(creditorInvoice.Vat, "12");
                else if (creditorInvoice.Vat2Code != null && creditorInvoice.Vat2Code.ModelBoxId == 12)
                    result.Add(creditorInvoice.Vat2, "12");
                else if (creditorInvoice.Vat3Code != null && creditorInvoice.Vat3Code.ModelBoxId == 12)
                    result.Add(creditorInvoice.Vat3, "12");

            }
            return result;
        }
    }

    public class InfAGeneration : InfBaseGeneration
    {
        public InfAGeneration(BaseAPI api, DateTime fromDate, DateTime toDate) : base(api, fromDate, toDate)
        {
        }

        /// <summary>
        /// Gets all debtor invoices in period mandated.
        /// </summary>
        /// <returns></returns>
        public async Task<List<DebtorInvoiceClient>> GetDebtorInvoices()
        {
            var crit = new List<PropValuePair>
            {
                PropValuePair.GenereteWhereElements("Date", typeof(DateTime), $"{periodFromDate.ToString("d/M-yyyy", CultureInfo.InvariantCulture)}..{periodToDate.ToString("d/M-yyyy", CultureInfo.InvariantCulture)}"),
                PropValuePair.GenereteOrderByElement("Account", false)
            };

            var debtorInvoices = await _api.Query<DebtorInvoiceClient>(crit);
            return new List<DebtorInvoiceClient>(debtorInvoices);
        }

        public async Task<List<EEInfLines>> CalculateInfArved()
        {
            InfRead = new List<EEInfLines>();
            var debtorInvoices = await GetDebtorInvoices();

            string currentAccount = String.Empty;
            var currentAccountInvoices = new List<DebtorInvoiceClient>();
            currentAccountTotalInvoicesSum = 0;
            currentAccountTotalCreditInvoicesSum = 0;

            // Kõige pealt uued antud perioodi arved läbi käia.
            foreach (var debtInv in debtorInvoices)
            {
                // Vaatan, kas kliendi konto on sama
                if (currentAccount != debtInv.Account)
                {
                    if (currentAccountInvoices.Count != 0)
                        await LisaArved(currentAccount, currentAccountInvoices);

                    currentAccount = debtInv.Account;
                    currentAccountTotalInvoicesSum = 0;
                    currentAccountTotalCreditInvoicesSum = 0;
                    currentAccountInvoices = new List<DebtorInvoiceClient>();
                }
                // Kui arve summa on suurem nullist, siis lisan deebetsummale juurde, muidu lisan kreeditsummale juurde
                if (debtInv._Total > 0)
                    currentAccountTotalInvoicesSum += debtInv._Total;
                else
                    currentAccountTotalCreditInvoicesSum += debtInv._Total;

                currentAccountInvoices.Add(debtInv);
            }

            await LisaArved(currentAccount, currentAccountInvoices);

            return InfRead;
        }

        private async Task LisaArved(string currentAccount, List<DebtorInvoiceClient> oneAccountInvoices)
        {
            if (currentAccountTotalInvoicesSum >= Constants.SalesLimitInvoiceAmount || Math.Abs(currentAccountTotalCreditInvoicesSum) >= Constants.SalesLimitInvoiceAmount)
            {
                var debtorAccount = oneAccountInvoices[0].Debtor;

                // TODO: Siia lisada see väärtus ka.
                var isNotKmdDeclarable = debtorAccount._EEIsNotVatDeclOrg;
                if (debtorAccount != null && debtorAccount._VatZone == VatZones.Domestic && !isNotKmdDeclarable)
                {
                    foreach (var accountInv in oneAccountInvoices)
                    {
                        await CreateInfLineFromInvoice(accountInv, debtorAccount);
                    }
                }
            }
        }

        private async Task CreateInfLineFromInvoice(DebtorInvoiceClient accountInv, DebtorClient debtorAccount = null)
        {
            EEInfLines uusArve = null;
            var glTrans = await _api.Query<GLTransClient>(accountInv);

            // At the moment here is not needed to have other than only VAT records.
            glTrans = glTrans.Where(gt => !String.IsNullOrEmpty(gt.Vat)).ToArray();
            var exceptionCode = ExceptionCodeCheck(glTrans);
            foreach (var glt in glTrans)
            {
                if (glt.VatCode == null || glt.VatCode.Rate == 0)
                    continue;

                double vatRate = glt.VatCode.Rate;
                // Esimene tingimus, et tavaarved korda saada kõigepealt
                if (glt.AmountVat != 0 && glt.Amount != 0)
                {
                    uusArve = new EEInfLines(debtorAccount, accountInv);
                    uusArve.InfAVatRate = vatRate.ToString();
                    uusArve.LineNr = InfRead.Count + 1;
                    uusArve.InfATaxableRevenue = glt._Amount * -1;

                    if (exceptionCode.ContainsKey(glt.Vat))
                    {
                        uusArve.ExceptionCode = exceptionCode[glt.Vat];
                        if (exceptionCode[glt.Vat].Contains("01"))
                        {
                            if (glt.VatCode.ModelBoxId == 1)
                                uusArve.InfAVatRate = glt.VatCode.Rate.ToString() + "erikord";
                            uusArve.InfAInvoiceSumWoVat = CalculateException01TotalAmount(glTrans, glt, exceptionCode, accountInv.TotalAmount);
                            uusArve.InfATaxableRevenue = glt.Amount * -1;
                        }
                        if (exceptionCode[glt.Vat].Contains("02"))
                        {
                            uusArve.InfATaxableRevenue = 0;
                        }
                    }

                    InfRead.Add(uusArve);
                }
            }
        }

        private double CalculateException01TotalAmount(GLTransClient[] glTrans, GLTransClient currentGlt, Dictionary<string, string> exceptionCode, double invoiceAmount)
        {
            foreach (var gt in glTrans)
            {
                if (gt.AmountVat == 0)
                    continue;
                // Kui käibeka kood ei ole määratud tabelis, kui erisus 1 kood, siis tegelikult tuleb käibekas maha võtta arve kogusummast.
                if (gt.VatCode.ModelBoxId == 1)
                    invoiceAmount += gt.AmountVat;
            }
            return invoiceAmount;
        }
    }

    public class InfBGeneration : InfBaseGeneration
    {
        public InfBGeneration(BaseAPI api, DateTime fromDate, DateTime toDate) : base(api, fromDate, toDate)
        {
        }

        public async Task<List<EEInfLines>> CalculateInfOstuarved()
        {
            List<EEInfLines> arved = new List<EEInfLines>();
            var creditorInvoices = await GetCreditorInvoices();
            var currentAccount = String.Empty;
            currentAccountTotalInvoicesSum = 0;
            currentAccountTotalCreditInvoicesSum = 0;


            List<CreditorInvoiceClient> currentAccountInvoices = new List<CreditorInvoiceClient>();

            foreach (var credInv in creditorInvoices)
            {
                // Vaatan, kas kliendi konto on sama
                if (currentAccount != credInv._DCAccount)
                {
                    if (currentAccountInvoices.Count != 0)
                        await LisaOstuArved(arved, currentAccount, currentAccountInvoices);
                    currentAccount = credInv.Account;
                    currentAccountTotalInvoicesSum = 0;
                    currentAccountTotalCreditInvoicesSum = 0;
                    currentAccountInvoices = new List<CreditorInvoiceClient>();
                }
                if (credInv.TotalAmount > 0)
                    currentAccountTotalInvoicesSum += credInv.TotalAmount;
                else
                    currentAccountTotalCreditInvoicesSum += credInv.TotalAmount;

                currentAccountInvoices.Add(credInv);
            }

            await LisaOstuArved(arved, currentAccount, currentAccountInvoices);

            return arved;
        }

        private async Task LisaOstuArved(List<EEInfLines> arved, string currentAccount, List<CreditorInvoiceClient> oneAccountInvoices)
        {
            if (currentAccountTotalInvoicesSum >= 1000 || Math.Abs(currentAccountTotalCreditInvoicesSum) >= 1000)
            {
                var crit = new List<PropValuePair>();
                var creditorAccount = await GetCreditorByAccount(currentAccount);
                EEInfLines uusArve = null;
                var isNotKmdDeclarable = creditorAccount._EEIsNotVatDeclOrg;
                if (creditorAccount != null && creditorAccount._VatZone == VatZones.Domestic && !isNotKmdDeclarable)
                    foreach (var accountInv in oneAccountInvoices)
                    {
                        var exceptionCode = ExceptionCodeCheck(null, true, accountInv);

                        uusArve = new EEInfLines(creditorAccount, accountInv);

                        uusArve.InfBPeriodInVatAmoun = accountInv.VatAmount;

                        uusArve.LineNr = arved.Count + 1;
                        if (exceptionCode.Count != 0)
                        {
                            uusArve.ExceptionCode = String.Empty;
                            foreach (var key in exceptionCode.Keys)
                            {
                                if (!String.IsNullOrEmpty(uusArve.ExceptionCode))
                                    uusArve.ExceptionCode += ",";
                                uusArve.ExceptionCode += exceptionCode[key];
                            }
                        }
                        arved.Add(uusArve);
                    }
            }
        }

        private async Task<List<CreditorInvoiceClient>> GetCreditorInvoices(bool takeCreditInvoices = false)
        {
            var crit = new List<PropValuePair>
            {
                PropValuePair.GenereteWhereElements("Date", typeof(DateTime), $"{periodFromDate.ToString("d/M-yyyy", CultureInfo.InvariantCulture)}..{periodToDate.ToString("d/M-yyyy", CultureInfo.InvariantCulture)}"),
                PropValuePair.GenereteOrderByElement("Account", false)
            };

            var debtorInvoices = await _api.Query<CreditorInvoiceClient>(crit);
            return new List<CreditorInvoiceClient>(debtorInvoices);
        }
    }

    public class XmlExporter
    {
        private CrudAPI _api;
        private List<EEInfLines> InfALines { get; set; }
        private List<EEInfLines> InfBLines { get; set; }
        private int NoOfCars50 { get; set; }
        private int NoOfCars100 { get; set; }

        private bool IsCashBasedDeclarer { get; set; }
        public XmlExporter(CrudAPI api)
        {
            _api = api;

        }

        public void CreateXmlFile(Stream sfd, DateTime fromDate, DateTime toDate, List<VatSumOperationReport> reportLines, List<EEInfLines> infALines, List<EEInfLines> infBLines, int noOfCar50, int noOfCar100, bool noSales = false, bool noPurchases = false)
        {
            InfALines = infALines;
            InfBLines = infBLines;

            NoOfCars50 = noOfCar50;
            NoOfCars100 = noOfCar100;
            XmlDocument doc = new XmlDocument();
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            XmlSerializer serializer = new XmlSerializer(typeof(VatDeclaration), String.Empty);
            XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8
            };
            StringBuilder sb = new StringBuilder();

            var mainDeclaration = GenerateDeclaration(fromDate, toDate, reportLines, noSales, noPurchases);
            XmlWriter xmlWriter = XmlWriter.Create(sfd, xmlWriterSettings);

            serializer.Serialize(xmlWriter, mainDeclaration, ns);
            xmlWriter.Close();
        }

        private VatDeclaration _mainDeclaration;
        internal VatDeclaration GenerateDeclaration(DateTime fromDate, DateTime toDate, List<VatSumOperationReport> reportLines, bool noSales = false, bool noPurchases = false)
        {
            var declarationType = "1"; //TODO võib olla ka 2, kui on pankrotiperioodil edastatud deklaratsioon
            FillMainPartOfDeclaration(fromDate, declarationType);

            if (InfALines == null || InfALines.Count == 0)
                noSales = true;

            if (InfBLines == null || InfBLines.Count == 0)
                noPurchases = true;

            if (reportLines != null && reportLines.Count > 0)
                _mainDeclaration.declarationBody = GenerateGeneralReportMainPart(reportLines, noSales, noPurchases);
            if (!noSales)
                _mainDeclaration.salesAnnex = GenerateINFAXml(fromDate, toDate, noSales);
            if (!noPurchases)
                _mainDeclaration.purchasesAnnex = GenerateINFBXml(fromDate, toDate, noPurchases);

            return _mainDeclaration;
        }

        private void FillMainPartOfDeclaration(DateTime fromDate, string declarationType)
        {
            _mainDeclaration = new VatDeclaration();
            _mainDeclaration.taxPayerRegCode = _api.CompanyEntity._VatNumber;
            _mainDeclaration.declarationType = declarationType;
            _mainDeclaration.month = fromDate.Month.ToString();
            _mainDeclaration.year = fromDate.Year.ToString();

            // Not mandatory, unless machine to machine 
            _mainDeclaration.submitterPersonCode = "";
        }

        private PurchasesAnnex[] GenerateINFBXml(DateTime fromDate, DateTime toDate, bool noPurchases)
        {
            // If not included, then this has to be there.
            if (noPurchases || InfBLines == null || InfBLines.Count == 0)
            {
                PurchasesAnnex pa = new PurchasesAnnex();
                pa.noPurchases = noPurchases;
                return new PurchasesAnnex[1] { pa };
            }
            var result = new List<PurchasesAnnex>();
            var purchaseLines = new List<PurchaseLine>();
            foreach (var infLine in InfBLines)
            {
                PurchaseLine pl = new PurchaseLine();
                pl.invoiceDate = infLine.InvoiceDate;
                pl.invoiceDateSpecified = true;
                pl.invoiceNumber = infLine.InvoiceNumber.ToString();
                pl.invoiceSumVat = (decimal)infLine.InfBInvoiceSumWVat;
                pl.sellerName = infLine.AccountName;
                pl.sellerRegCode = infLine.AccountRegNo;
                pl.vatInPeriod = (decimal)infLine.InfBPeriodInVatAmoun;
                if (IsCashBasedDeclarer)
                {
                    pl.vatSum = (decimal)infLine.InfBInvoiceVatAmount;
                    pl.vatSumSpecified = true;
                }
                pl.comments = infLine.ExceptionCode;
                purchaseLines.Add(pl);
            }
            result.Add(new PurchasesAnnex { purchaseLine = purchaseLines.ToArray(), noPurchases = false, sumPerPartnerPurchases = false });
            return result.ToArray();
        }

        private SalesAnnex[] GenerateINFAXml(DateTime fromDate, DateTime toDate, bool noSales)
        {
            // If not included, then this has to be there.
            if (noSales || InfALines == null || InfALines.Count == 0)
            {
                SalesAnnex sa = new SalesAnnex();
                sa.noSales = noSales;
                return new SalesAnnex[1] { sa };
            }

            var result = new List<SalesAnnex>();
            var saleLines = new List<SaleLine>();
            foreach (var infLine in InfALines)
            {
                SaleLine sl = new SaleLine();
                sl.invoiceDate = infLine.InvoiceDate;
                sl.invoiceDateSpecified = true;
                sl.invoiceNumber = infLine.InvoiceNumber.ToString();
                sl.invoiceSum = (decimal)infLine.InfAInvoiceSumWoVat;
                sl.buyerName = infLine.AccountName;
                sl.buyerRegCode = infLine.AccountRegNo;
                if (IsCashBasedDeclarer)
                {
                    sl.invoiceSumForRate = (decimal)infLine.InfATaxableAmount;
                    sl.invoiceSumForRateSpecified = true;
                }
                sl.sumForRateInPeriod = (decimal)infLine.InfATaxableRevenue;
                sl.sumForRateInPeriodSpecified = true;
                sl.taxRate = infLine.InfAVatRate;
                sl.comments = infLine.ExceptionCode;
                saleLines.Add(sl);
            }
            result.Add(new SalesAnnex { saleLine = saleLines.ToArray(), noSales = false, sumPerPartnerSales = false });
            return result.ToArray();
        }

        private DeclarationBody GenerateGeneralReportMainPart(List<VatSumOperationReport> reportLines, bool noSales = true, bool noPurchases = true)
        {
            DeclarationBody result = new DeclarationBody();
            result.noSales = noSales;
            result.noPurchases = noPurchases;
            result.sumPerPartnerPurchases = false;
            result.sumPerPartnerSales = false;

            foreach (var reportLine in reportLines)
            {
                switch (reportLine.UnicontaOperation)
                {
                    case "1":
                        result.transactions20 = (decimal)reportLine.AmountBase;
                        result.transactions20Specified = true;
                        break;
                    case "2":
                        result.transactions9 = (decimal)reportLine.AmountBase;
                        result.transactions9Specified = true;
                        break;
                    case "3":
                        result.transactionsZeroVat = (decimal)reportLine.AmountBase;
                        result.transactionsZeroVatSpecified = true;
                        break;
                    case "3.1":
                        result.euSupplyInclGoodsAndServicesZeroVat = (decimal)reportLine.AmountBase;
                        result.euSupplyInclGoodsAndServicesZeroVatSpecified = true;
                        break;
                    case "3.1.1":
                        result.euSupplyGoodsZeroVat = (decimal)reportLine.AmountBase;
                        result.euSupplyGoodsZeroVatSpecified = true;
                        break;
                    case "3.2":
                        result.exportZeroVat = (decimal)reportLine.AmountBase;
                        result.exportZeroVatSpecified = true;
                        break;
                    case "3.2.1":
                        result.salePassengersWithReturnVat = (decimal)reportLine.AmountBase;
                        result.salePassengersWithReturnVatSpecified = true;
                        break;
                    case "5":
                        result.inputVatTotal = (decimal)reportLine.Amount;
                        result.inputVatTotalSpecified = true;
                        break;
                    case "5.1":
                        result.importVat = (decimal)reportLine.Amount;
                        result.importVatSpecified = true;
                        break;
                    case "5.2":
                        result.fixedAssetsVat = (decimal)reportLine.Amount;
                        result.fixedAssetsVatSpecified = true;
                        break;
                    case "5.3":
                        result.carsVat = (decimal)reportLine.Amount;
                        result.carsVatSpecified = true;
                        result.numberOfCars = NoOfCars100.ToString();
                        break;
                    case "5.4":
                        result.carsPartialVat = (decimal)reportLine.Amount;
                        result.carsPartialVatSpecified = true;
                        result.numberOfCarsPartial = NoOfCars50.ToString();
                        break;
                    case "6":
                        result.euAcquisitionsGoodsAndServicesTotal = (decimal)reportLine.AmountBase;
                        result.euAcquisitionsGoodsAndServicesTotalSpecified = true;
                        break;
                    case "6.1":
                        result.euAcquisitionsGoods = (decimal)reportLine.AmountBase;
                        result.euAcquisitionsGoodsSpecified = true;
                        break;
                    case "7":
                        result.acquisitionOtherGoodsAndServicesTotal = (decimal)reportLine.AmountBase;
                        result.acquisitionOtherGoodsAndServicesTotalSpecified = true;
                        break;
                    case "7.1":
                        result.acquisitionImmovablesAndScrapMetalAndGold = (decimal)reportLine.AmountBase;
                        result.acquisitionImmovablesAndScrapMetalAndGoldSpecified = true;
                        break;
                    case "8":
                        result.supplyExemptFromTax = (decimal)reportLine.AmountBase;
                        result.supplyExemptFromTaxSpecified = true;
                        break;
                    case "9":
                        result.supplySpecialArrangements = (decimal)reportLine.AmountBase;
                        result.supplySpecialArrangementsSpecified = true;
                        break;
                    case "10":
                        result.adjustmentsPlus = (decimal)reportLine.AmountBase;
                        result.adjustmentsPlusSpecified = true;
                        break;
                    case "11":
                        result.adjustmentsMinus = (decimal)reportLine.AmountBase;
                        result.adjustmentsMinusSpecified = true;
                        break;
                }
            }

            return result;
        }
    }
#endregion

#region XML classes
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlRootAttribute("vatDeclaration", Namespace = "", IsNullable = false)]
    public partial class VatDeclaration
    {

        /// <remarks/>
        public string taxPayerRegCode;

        /// <remarks/>
        public string submitterPersonCode;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
        public string year;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
        public string month;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
        public string declarationType;

        /// <remarks/>
        public DeclarationBody declarationBody;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("salesAnnex")]
        public SalesAnnex[] salesAnnex;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("purchasesAnnex")]
        public PurchasesAnnex[] purchasesAnnex;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class DeclarationBody
    {

        /// <remarks/>
        public bool noSales;

        /// <remarks/>
        public bool noPurchases;

        /// <remarks/>
        public bool sumPerPartnerSales;

        /// <remarks/>
        public bool sumPerPartnerPurchases;

        /// <remarks/>
        public decimal transactions20;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool transactions20Specified;

        /// <remarks/>
        public decimal selfSupply20;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool selfSupply20Specified;

        /// <remarks/>
        public decimal transactions9;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool transactions9Specified;

        /// <remarks/>
        public decimal selfSupply9;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool selfSupply9Specified;

        /// <remarks/>
        public decimal transactions14;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool transactions14Specified;

        /// <remarks/>
        public decimal transactionsZeroVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool transactionsZeroVatSpecified;

        /// <remarks/>
        public decimal euSupplyInclGoodsAndServicesZeroVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool euSupplyInclGoodsAndServicesZeroVatSpecified;

        /// <remarks/>
        public decimal euSupplyGoodsZeroVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool euSupplyGoodsZeroVatSpecified;

        /// <remarks/>
        public decimal exportZeroVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool exportZeroVatSpecified;

        /// <remarks/>
        public decimal salePassengersWithReturnVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool salePassengersWithReturnVatSpecified;

        /// <remarks/>
        public decimal inputVatTotal;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool inputVatTotalSpecified;

        /// <remarks/>
        public decimal importVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool importVatSpecified;

        /// <remarks/>
        public decimal fixedAssetsVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool fixedAssetsVatSpecified;

        /// <remarks/>
        public decimal carsVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool carsVatSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
        public string numberOfCars;

        /// <remarks/>
        public decimal carsPartialVat;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool carsPartialVatSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "integer")]
        public string numberOfCarsPartial;

        /// <remarks/>
        public decimal euAcquisitionsGoodsAndServicesTotal;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool euAcquisitionsGoodsAndServicesTotalSpecified;

        /// <remarks/>
        public decimal euAcquisitionsGoods;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool euAcquisitionsGoodsSpecified;

        /// <remarks/>
        public decimal acquisitionOtherGoodsAndServicesTotal;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool acquisitionOtherGoodsAndServicesTotalSpecified;

        /// <remarks/>
        public decimal acquisitionImmovablesAndScrapMetalAndGold;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool acquisitionImmovablesAndScrapMetalAndGoldSpecified;

        /// <remarks/>
        public decimal supplyExemptFromTax;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool supplyExemptFromTaxSpecified;

        /// <remarks/>
        public decimal supplySpecialArrangements;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool supplySpecialArrangementsSpecified;

        /// <remarks/>
        public decimal adjustmentsPlus;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool adjustmentsPlusSpecified;

        /// <remarks/>
        public decimal adjustmentsMinus;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool adjustmentsMinusSpecified;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class PurchaseLine
    {

        /// <remarks/>
        public string sellerRegCode;

        /// <remarks/>
        public string sellerName;

        /// <remarks/>
        public string invoiceNumber;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime invoiceDate;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool invoiceDateSpecified;

        /// <remarks/>
        public decimal invoiceSumVat;

        /// <remarks/>
        public decimal vatSum;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool vatSumSpecified;

        /// <remarks/>
        public decimal vatInPeriod;

        /// <remarks/>
        public string comments;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class PurchasesAnnex
    {

        /// <remarks/>
        public string groupMemberRegCode;

        /// <remarks/>
        public bool noPurchases;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool noPurchasesSpecified;

        /// <remarks/>
        public bool sumPerPartnerPurchases;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool sumPerPartnerPurchasesSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("purchaseLine")]
        public PurchaseLine[] purchaseLine;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SaleLine
    {

        /// <remarks/>
        public string buyerRegCode;

        /// <remarks/>
        public string buyerName;

        /// <remarks/>
        public string invoiceNumber;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(DataType = "date")]
        public System.DateTime invoiceDate;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool invoiceDateSpecified;

        /// <remarks/>
        public decimal invoiceSum;

        /// <remarks/>
        public string taxRate;

        /// <remarks/>
        public decimal invoiceSumForRate;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool invoiceSumForRateSpecified;

        /// <remarks/>
        public decimal sumForRateInPeriod;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool sumForRateInPeriodSpecified;

        /// <remarks/>
        public string comments;
    }

    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.6.1055.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    public partial class SalesAnnex
    {

        /// <remarks/>
        public string groupMemberRegCode;

        /// <remarks/>
        public bool noSales;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool noSalesSpecified;

        /// <remarks/>
        public bool sumPerPartnerSales;

        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool sumPerPartnerSalesSpecified;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("saleLine")]
        public SaleLine[] saleLine;
    }
#endregion
}
