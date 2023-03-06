using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using System.Windows;
using Uniconta.Client.Pages;
using Uniconta.Common.Utility;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Editors;
using System.Globalization;
using System.Windows.Data;
using DevExpress.XtraReports.UI;
using NPOI.SS.Formula.Functions;
using System.ServiceModel;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using RSK.Verktakamidi;
using GagnaskilKlasar;
using static DevExpress.Utils.Svg.CommonSvgImages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GridCreditorTransContr : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransClient); } }
    }
    public partial class CreditorTransactionsContr : GridBasePage
    {
        public override string NameOfControl { get { return "ContractorReportIcelandPage"; } }
        int filterYear;
        static string filterContr = "";

        protected override Filter[] DefaultFilters()
        {
            Filter contrFilter = new Filter() { name = "Account", value = $"{filterContr}" };
            Filter yearFilter = new Filter() { name = "Date", value = $"{filterYear}-01-01..{filterYear}-12-31" };
            Filter typeFilter = new Filter() { name = "PostType", value = "Invoice;CreditNote;Fee;Cancellation;Discount" };//String.Format("{0:d}..", filterDate) };
            if (filterContr != "")
                return new Filter[] { contrFilter, yearFilter, typeFilter };
            else
                return new Filter[] { yearFilter, typeFilter };
        }
        protected override SortingProperties[] DefaultSort()
        {
            SortingProperties dateSort = new SortingProperties("Date") { Ascending = false };
            SortingProperties VoucherSort = new SortingProperties("Voucher");
            return new SortingProperties[] { dateSort, VoucherSort };
        }

        public CreditorTransactionsContr(UnicontaBaseEntity master)
            : base(master)
        {
            Initialize(master);
        }
        public CreditorTransactionsContr(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Initialize(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgCreditorTransContr.UpdateMaster(args);
            SetHeader();
            FilterGrid(gridControl, false, false);
        }
        void SetHeader()
        {
            var syncMaster = dgCreditorTransContr.masterRecord as Uniconta.DataModel.Creditor;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorTransactionsContr"), syncMaster._Account);
            SetHeader(header);
        }
        public CreditorTransactionsContr(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            Initialize(null);
        }

        UnicontaBaseEntity master;
        private void Initialize(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            master = _master;
            dgCreditorTransContr.UpdateMaster(master);
            dgCreditorTransContr.api = api;
            var Comp = api.CompanyEntity;
            filterYear = BasePage.GetSystemDefaultDate().Year - 1;
            txtYearValue.EditValue = DateTime.Parse($"{filterYear}-01-01", CultureInfo.InvariantCulture);
            SetRibbonControl(localMenu, dgCreditorTransContr);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgCreditorTransContr.BusyIndicator = busyIndicator;
            dgCreditorTransContr.ShowTotalSummary();
            if (Comp.RoundTo100)
                Amount.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        public override Task InitQuery()
        {
            return FilterGrid(dgCreditorTransContr, master == null, false);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool showFields = (dgCreditorTransContr.masterRecords == null);
            colAccount.Visible = showFields;
            colName.Visible = showFields;
            Open.Visible = !showFields;
            if (!api.CompanyEntity.Project)
                Project.Visible = false;
            Text.ReadOnly = Invoice.ReadOnly = PostType.ReadOnly = TransType.ReadOnly = showFields;
            if (showFields)
            {
                Open.Visible = false;
                RibbonBase rb = (RibbonBase)localMenu.DataContext;
                UtilDisplay.RemoveMenuCommand(rb, "SaveGrid");
            }
            var credMaster = master as Uniconta.DataModel.Creditor;
            if (credMaster != null)
            {
#if !SILVERLIGHT
                FromDebtor.Visible =
#endif
                dgCreditorTransContr.Readonly = (credMaster._D2CAccount != null);
            }
            else
            {
                dgCreditorTransContr.Readonly = showFields;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            CreditorTransClient selectedItem = dgCreditorTransContr.SelectedItem as CreditorTransClient;
            switch (ActionType)
            {
                case "ViewDownloadRow":
                    if (selectedItem != null)
                        DebtorTransactions.ShowVoucher(dgCreditorTransContr.syncEntity, api, busyIndicator);
                    break;
                case "Settlements":
                    if (selectedItem != null)
                    {
                        string header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("Settlements"), selectedItem._Voucher);
                        AddDockItem(TabControls.CreditorSettlements, dgCreditorTransContr.syncEntity, true, header, null, floatingLoc: Utility.GetDefaultLocation());
                    }
                    break;
                case "VoucherTransactions":
                    if (selectedItem != null)
                    {
                        string vheader = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("VoucherTransactions"), selectedItem._Voucher);
                        AddDockItem(TabControls.AccountsTransaction, dgCreditorTransContr.syncEntity, vheader);
                    }
                    break;
                case "SaveGrid":
                    Save();
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                case "RefreshGrid":
                    FilterGrid(gridControl, master == null, false);
                    break;
                case "Invoices":
                    if (selectedItem != null)
                    {
                        string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("CreditorInvoice"), selectedItem._Account);
                        AddDockItem(TabControls.CreditorInvoice, dgCreditorTransContr.syncEntity, header);
                    }
                    break;
                case "InvoiceLine":
                    if (selectedItem != null)
                        ShowInvoiceLines(selectedItem);
                    break;
                case "ClearFilter":
                    filterContr = "";
                    this.baseRibbon?.FilterGrid?.ClearFilter(false);
                    this.baseRibbon?.SetFilterDefaultFields(DefaultFilters());
                    this.baseRibbon?.FilterGrid.Refresh();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void ShowInvoiceLines(CreditorTransClient creditorTrans)
        {
            var creditorInvoice = await api.Query<CreditorInvoiceClient>(new UnicontaBaseEntity[] { creditorTrans }, null);
            if (creditorInvoice != null && creditorInvoice.Length > 0)
            {
                var credInv = creditorInvoice[0];
                AddDockItem(TabControls.CreditorInvoiceLine, credInv, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), credInv.InvoiceNum));
            }
        }

        async private void JournalPosted(CreditorTransClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result != null && result.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }

        async private void Save()
        {
            SetBusy();
            dgCreditorTransContr.BusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Saving");
            var err = await dgCreditorTransContr.SaveData();
            ClearBusy();
        }

        private void TxtYearValue_OnEditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            var filterDate = (DateTime)e.NewValue;
            filterYear = filterDate.Year;
            this.baseRibbon?.SetFilterDefaultFields(DefaultFilters());
            this.baseRibbon?.FilterGrid.Refresh();
        }

        public static Midi CreateContractorTicketIcelandic(
            long ticketID,
            CreditorClient contractor,
            long amount = 0)
        {
            return new Midi
            {
                id = ticketID,
                Kennitala = ContractorLegalID(contractor),
                Verktakagreidsla = amount,
                VerktakagreidslaSpecified = amount != 0
            };
        }

        private static string ContractorLegalID(CreditorClient contractor)
        {
            var ContractorLegalID = "";
            if (contractor.CompanyRegNo != null)
                ContractorLegalID = contractor.CompanyRegNo;
            else
                ContractorLegalID = contractor.Account;
            ContractorLegalID = new string(ContractorLegalID.ToCharArray().Where(o => char.IsDigit(o)).ToArray());
            ContractorLegalID = ContractorLegalID.Replace("IS", "").Replace("-", "");
            return ContractorLegalID;
        }

        public static Midi CreateContractorTicketNotIcelandic(
            long ticketID,
            CreditorClient contractor,
            long amount = 0)
        {            
            var cityName = contractor._City;
            var streetName = contractor.Address;
            return new Midi
            {
                id = ticketID,
                Kennitala = Contr_Helpers.NON_IS_KENNITALA, // Constant for non-IS contractors
                Verktakagreidsla = amount,
                VerktakagreidslaSpecified = amount != 0,
                ErlendurAdili = new ErlendurAdili
                {
                    Borg = cityName,
                    Gata = streetName,
                    Nafn = contractor._Name,
                    Tin = String.IsNullOrEmpty(contractor._VatNumber) ? "" : contractor._VatNumber,
                    Land = landcode(contractor)
                }
            };
        }

        private static TegLandKodi landcode(CreditorClient contractor)
        {
            string countryCodeIso;
            TegLandKodi landCode;
            countryCodeIso = ((CountryISOCode)contractor.Country).ToString();
            Enum.TryParse(countryCodeIso, true, out landCode);
            return landCode;
        }

        public XmlElement SerializeToXmlElement<T>(T value)
        {
            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var serializer = new XmlSerializer(value.GetType());
            var settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.OmitXmlDeclaration = true;
            XmlDocument el = new XmlDocument();

            using (var stream = new StringWriter())
            using (var writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, value, emptyNamespaces);
                el.LoadXml(stream.ToString());
                return el.DocumentElement;
            }
        }

        private void SendButton_OnClick(object sender, RoutedEventArgs e)
        {
            SetBusy();
            dgCreditorTransContr.BusyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("Sending");
            Send(TegAdgerd.Nyskra);
            if (filterContr != "")
            {
                this.baseRibbon?.SetFilterDefaultFields(DefaultFilters());
                this.baseRibbon?.FilterGrid.Refresh();
            }
            ClearBusy();
        }

        public Verktakagreidsluskra skra(List<Midi> midisList, TegAdgerd adgerd)
        {
            return new Verktakagreidsluskra()
            {
                //Bunkanumer = 444072,
                //BunkanumerSpecified = true,
                KennitalaLaunagreidanda = this.api.CompanyEntity._Id,
                Tekjuar = filterYear.ToString("D4"), //TODO
                Veflykill = (string)txtVeflykill.EditValue ?? "",
                Tegund = TegTegund.Beint,
                //Adgerd = TegAdgerd.Nyskra, //ekki verið að leiðrétta
                Adgerd = adgerd,
                TilForskraningar = TegString_J_N.J, //ALLTAF
                ForritUtgafa = Contr_Helpers.VERSION_ID,
                Midi = midisList.ToArray(),
                Samtalningsblad = new Samtalningsblad()
                {
                    FjoldiLaunthega = midisList.Count,
                    FjoldiMida = midisList.Count,
                    Verktakagreidsla = midisList.Sum(o => o.Verktakagreidsla),
                    VerktakagreidslaSpecified = true
                }
            };
        }

        private async void Send(TegAdgerd adgerd)
        {
            var items = dgCreditorTransContr.VisibleItems.Cast<CreditorTransClient>();
            var vefLykill = txtVeflykill.EditText;
            List<Midi> midisList = new List<Midi>();
            var cli = new ContrClient();
            var RSKmelding = "";

            try
            {
                RSKmelding = CreateTickets(cli, adgerd, items, midisList);

                if (String.IsNullOrEmpty(RSKmelding))
                {
                    var resp = cli.Client.Senda(SerializeToXmlElement(skra(midisList, adgerd)));

                    if (!resp.Tokst)
                    {
                        var hafnadString = "";
                        if (resp.Hafnad != null && resp.Hafnad.Length > 0)
                        {
                            hafnadString = "\n" + string.Join("\n", resp.Hafnad.Select(o => o.ToString()));
                        }
                        UnicontaMessageBox.Show(resp.Villubod + hafnadString + "\n\nAthugasemdir frá RSK:\n" + (resp.Athugasemd ?? "\n") + "\n\n" + string.Join("\n\n", (resp.Athugasemdir ?? new string[] { }).Select(o => o ?? "")), "Villa");
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(resp.Athugasemd))
                        {
                            UnicontaMessageBox.Show(resp.Athugasemd, "Athugasemd");
                        }

                        // SUCCESS


                        // Verktakamiði er gerður út frá hreyfingayfirliti með tegundirnar reikningur, kreditreikningur, afturköllun, afsláttur, þóknun
                        // Svo skoðum við hvernig kreditreikningar eru lesnir inn upp m.t.t. VSK

                        //File.WriteAllBytes(@"E:\Verktakamidi.pdf", resp.PDFKvittun);

                        if ((resp.PDFKvittun != null) && (resp.PDFKvittun.Length > 0))
                        {
                            VouchersClient voucher = new VouchersClient()
                            {
                                VoucherAttachment = resp.PDFKvittun,
                                Text = $"Skil á verktakamiðum {filterYear:D4}",
                                _Content = ContentTypes.Documents,
                                Fileextension = FileextensionsTypes.PDF,
                                DocumentDate = DateTime.Now,
                            };
                            var insertRes = await api.Insert(voucher);
                            if (insertRes == ErrorCodes.Succes)
                                UnicontaMessageBox.Show("Kvittun hefur verið vistuð í Stafræn fylgiskjöl (innhólf)", "Aðgerð tókst", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                cli.Dispose();
                cli = null;
            }
            catch
            {
                cli.Dispose();
                cli = null;
                throw;
            }
        }

        private string CreateTickets(ContrClient cli, TegAdgerd adgerd, IEnumerable<CreditorTransClient> items, List<Midi> midisList)
        {
            string RSKmelding;
            using (CredtorTransValidation errchk = new CredtorTransValidation(this))
            {
                RSKmelding = "";
                if (dgCreditorTransContr.VisibleItems.Count != 0 && errchk.ValidateCompany(cli, ref RSKmelding, adgerd))
                {
                    int ticketNo = 1;
                    foreach (var grouping in items.GroupBy(o => o.Creditor))
                    {
                        var creditor = grouping.Key;
                        if (grouping.Sum(o => o.Amount) >= 0) continue;
                        Midi midi = null;
                        if (creditor.Country == CountryCode.Iceland)
                        {
                            if (ContractorLegalID(creditor).Length == 10)
                                midi = CreateContractorTicketIcelandic(ticketNo, creditor, (long)grouping.Sum(o => -o.Amount));
                            else
                                RSKmelding = "Ógild kennitala";
                        }
                        else
                        {
                            midi = CreateContractorTicketNotIcelandic(ticketNo, creditor,
                                (long)grouping.Sum(o => -o.Amount));
                        }

                        if (midi != null && errchk.ValidateContractor(cli, midi, ref RSKmelding, adgerd, dgCreditorTransContr.VisibleItems.Count))
                        {
                            midisList.Add(midi);
                            ticketNo++;
                        }
                        else
                        {
                            if (!String.IsNullOrEmpty(RSKmelding))
                            {
                                filterContr = grouping.Key.Account;
                                UnicontaMessageBox.Show(RSKmelding + $" á lykli {filterContr}", "Verktakamiðar");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    if (dgCreditorTransContr.VisibleItems.Count == 0)
                        throw new Exception("Engir verktakar fundust");
                    else
                        throw new Exception(RSKmelding);
                }
            }
            return RSKmelding;
        }
    } //...class CreditorTransactionsContr

    public class GroupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Uniconta.DataModel.Creditor)value)._Group;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class Comment
    {
        public string Contr;
        public string Comnt;

        public Comment(string c)
        {
            this.Comnt = c;
        }
    }

//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public class CredtorTransValidation : IDisposable
    {
        private List<Comment> Athugasemdir = new List<Comment>();
        private List<Midi> dummymidisList = new List<Midi>();
        private CreditorTransactionsContr creditorTransContr;

        public void Dispose()
        {
            Athugasemdir = null;
            dummymidisList = null;
        }

        public CredtorTransValidation(CreditorTransactionsContr creditorTransactionsContr)
        {
            this.creditorTransContr = creditorTransactionsContr;
        }

        // This dummyticket is to validate the company.
        private Midi dummymidi()
        {
            return new Midi()
            {
                id = 1,
                Kennitala = Contr_Helpers.NON_IS_KENNITALA,
                Verktakagreidsla = 1,
                VerktakagreidslaSpecified = 1 != 0
            };
        }

        public bool ValidateCompany(ContrClient cli, ref string RSKmelding, TegAdgerd adgerd)
        {
            dummymidisList.Add(dummymidi());
            var dummyskra = creditorTransContr.skra(dummymidisList, adgerd);
            var resp = GetValidation(cli, out RSKmelding, dummyskra);
            dummyskra = null;
            return resp.Tokst;
        }

        public bool ValidateContractor(ContrClient cli, Midi midi, ref string RSKmelding, TegAdgerd adgerd, int filteredTickets)
        {
            RSKmelding = "";
            bool tomur = dummymidisList?.Any() != true;
            if (!tomur)
                dummymidisList.Clear();
            if (midi!=null)
                dummymidisList.Add(midi);

            var dummyskra = creditorTransContr.skra(dummymidisList, adgerd);

            var resp = GetValidation(cli, out RSKmelding, dummyskra);
            dummyskra = null;

            // Hér þarf að túlka ýmsar meldingar frá RSK yfir í eitthvað manneskjulegra.
            if (!String.IsNullOrEmpty(RSKmelding) || (resp.Hafnad != null && resp.Hafnad.Length > 0))
            {
                if (!String.IsNullOrEmpty(RSKmelding))
                {
                    if (RSKmelding.Contains("List of possible elements expected: 'Tin Gata'.") ||
                            RSKmelding.Contains("The 'Tin' element has an invalid value"))
                    {
                        RSKmelding = "Vantar Tax Identification Number (TIN) hjá verktaka";
                    }
                    else if (RSKmelding.Contains("List of possible elements expected: 'Gata'") ||
                            RSKmelding.Contains("The 'Gata' element has an invalid value"))
                    {
                        RSKmelding = "Vantar heimilisfang hjá verktaka";
                    }
                    else if (RSKmelding.Contains("'ErlendurAdili' has invalid child element 'Land'. List of possible elements expected: 'Borg'.") ||
                            RSKmelding.Contains("The 'Borg' element has an invalid value"))
                    {
                        RSKmelding = "Vantar að tilgreina borg verktaka";
                    }
                    else if (RSKmelding.Contains("Ekki er hægt að skila sömu sendingu aftur."))
                    {
                        RSKmelding = "";
                        if (filteredTickets == 1)
                        {
                            RSKmelding = "Búið að skila miða fyrir verktaka";                            
                        }
                        resp.Tokst = false;
                    }
                }

                if (resp.Hafnad != null && resp.Hafnad.Length > 0)
                {
                    RSKmelding = "\n" + string.Join("\n", resp.Hafnad.Select(o => o.ToString()));
                }
            }
            else if (midi.Kennitala == Contr_Helpers.NON_IS_KENNITALA && String.IsNullOrEmpty(RSKmelding))
            {
                if ((CountryCode)midi.ErlendurAdili?.Land == CountryCode.Unknown)
                {
                    RSKmelding = "Óþekkt land";
                    resp.Tokst = false;
                }
            }
            return resp.Tokst;
        }

        private StadfestaKlasi GetValidation(ContrClient cli, out string RSKmelding, Verktakagreidsluskra dummyskra)
        {
            var resp = cli.Client.Villuprofa(creditorTransContr.SerializeToXmlElement(dummyskra));
            RSKmelding = resp.Villubod;
            return resp;
        }
    } //...class CredtorTransValidation

//-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
    public static class Contr_Helpers
    {
        public static string NON_IS_KENNITALA = "9999999999";
        public static string VERSION_ID = "Uniconta V1";
    }
}