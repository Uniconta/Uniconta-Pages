using UnicontaClient.Models;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using System;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.IO;
using UnicontaClient.Pages;
using System.Collections;
using Uniconta.API.System;
using System.Globalization;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using DevExpress.Xpf.Grid;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ExportTransactionGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransClient); } }
        public override IComparer GridSorting { get { return new GLTransDateSort(); } }
        public override string SetColumnTooltip(object row, ColumnBase col)
        {
            var tran = row as GLTransClient;
            if (tran != null && col != null)
            {
                switch (col.FieldName)
                {
                    case "DCAccount": return tran.DCName;
                }
            }
            return base.SetColumnTooltip(row, col);
        }
    }

    public partial class GLTransPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLTransPage; } }

        bool newSupplement;
        string Command;
        private int AccountLength;
        GLTransExportedClient glTransExported;
        SQLCache Accounts, Debtors, Creditors, VATs, Pays; //Datev Zahlungsbedegungen
        public GLTransPage(UnicontaBaseEntity master, string Command) : base(master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgAccountsTransGrid;
            SetRibbonControl(localMenu, dgAccountsTransGrid);
            dgAccountsTransGrid.api = api;
            dgAccountsTransGrid.UpdateMaster(master);
            glTransExported = master as GLTransExportedClient;
            dgAccountsTransGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            Accounts = api.GetCache(typeof(Uniconta.DataModel.GLAccount));
            Debtors = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            Creditors = api.GetCache(typeof(Uniconta.DataModel.Creditor));
            VATs = api.GetCache(typeof(Uniconta.DataModel.GLVat));
            Pays = api.GetCache(typeof(Uniconta.DataModel.PaymentTerm));

            newSupplement = (Command == "ShowSupplement");
            if (newSupplement)
                ribbonControl.DisableButtons("SaveExport");
            if (newSupplement || glTransExported._MaxJournalPostedId == 0)
                ribbonControl.DisableButtons("ExportToDatev");
        }

        public override Task InitQuery()
        {
            if (newSupplement)
                return BindGrid(new [] { PropValuePair.GenereteParameter("Supplement", typeof(Int32), "1") });
            else
                return BindGrid(null);
        }

        Task BindGrid(IEnumerable<PropValuePair> inputs)
        {
            return dgAccountsTransGrid.Filter(inputs);
        }

        protected override async Task LoadCacheInBackGroundAsync()
        {
            if (Accounts == null)
                Accounts = await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            if (Debtors == null)
                Debtors = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (Creditors == null)
                Creditors = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (VATs == null)
                VATs = await api.LoadCache(typeof(Uniconta.DataModel.GLVat)).ConfigureAwait(false);
            if (Pays == null)
                Pays = await api.LoadCache(typeof(Uniconta.DataModel.PaymentTerm)).ConfigureAwait(false);
        }

        protected override void OnLayoutLoaded()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgAccountsTransGrid.SelectedItem as GLTransClient;
            switch (ActionType)
            {
                case "ExportToCSV":
                    dockCtrl.PrintCurrentTabGrids("CSV");
                    if (newSupplement)
                        ribbonControl.EnableButtons("SaveExport");
                    break;
                case "ExportToExcel":
                    dockCtrl.PrintCurrentTabGrids("Excel");
                    if (newSupplement)
                        ribbonControl.EnableButtons("SaveExport");
                    break;
                case "ExportToDatev":
                    if (newSupplement)
                        ribbonControl.EnableButtons("SaveExport");
                    ExportToDatev();
                    break;
                case "SaveExport":
                    CWCommentsDialogBox commentsDialog = new CWCommentsDialogBox(Uniconta.ClientTools.Localization.lookup("Comment"), false, DateTime.MinValue);
                    commentsDialog.mandatorycomments = false;
                    commentsDialog.DialogTableId = 2000000064;
                    commentsDialog.Closing += delegate
                    {
                        if (commentsDialog.DialogResult == true)
                            SaveGLTransExported(commentsDialog.Comments);
                    };
                    commentsDialog.Show();
                    break;
                case "CreateSupplement":
                    CreateSupplement();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async void SaveGLTransExported(string comments)
        {
            var lst = (IEnumerable<GLTransClient>)dgAccountsTransGrid.ItemsSource;
            glTransExported._Lines = lst.Count();
            if (glTransExported._Lines > 0)
            {
                glTransExported._MaxJournalPostedId = (from rec in lst select rec._JournalPostedId).Max();
                glTransExported._Comment = comments;
                if (ErrorCodes.Succes == await api.Insert(glTransExported))
                {
                    ribbonControl.DisableButtons("SaveExport");
                    globalEvents.OnRefresh(NameOfControl, new object[] { 1, glTransExported });
                }
            }
        }

        async void CreateSupplement()
        {
            await BindGrid(newSupplement ? new[] { PropValuePair.GenereteParameter("Supplement", typeof(Int32), "1") } : null);
            if (dgAccountsTransGrid.VisibleItems.Count > 0)
                ribbonControl.EnableButtons("SaveExport");
        }

        #region Datev

        private bool DataIsValidForExport(SQLCache Accounts, SQLCache Debtors, SQLCache Creditors)
        {
            if (glTransExported.ExportedBy == 0)
            {
                UnicontaMessageBox.Show("Bitte vor Export den Export speichern", "", MessageBoxButton.OK);
                return false;
            }

            var AccountLength = (Accounts.GetRecords as GLAccount[])[0]._Account.Length;
            if (AccountLength > 8 || AccountLength < 4)
            {
                UnicontaMessageBox.Show("Ung�ltige Kontol�nge mind. eines Sachkontos", "", MessageBoxButton.OK);
                return false;
            }

            var invalidGLA = (Accounts.GetRecords as GLAccount[]).Where(acc => acc._Account.Length != AccountLength).ToArray();
            if (invalidGLA != null && invalidGLA.Length != 0)
            {
                var err = GetInvalidEntries(invalidGLA, (acc) => acc.AccountNumber);
                UnicontaMessageBox.Show("Fehler im Kontenplan: \n" + err, "", MessageBoxButton.OK);
                return false;
            }

            AccountLength++;

            var invalidDebtors = (Debtors.GetRecords as Debtor[]).Where(d => d._Account.Length != AccountLength)?.ToArray();
            if (invalidDebtors.Length != 0)
            {
                var err = GetInvalidEntries(invalidDebtors, (acc) => acc._Account);
                UnicontaMessageBox.Show("Fehler bei Kunden: \n" + err, "", MessageBoxButton.OK);
                return false;
            }

            var invalidCreditors = (Creditors.GetRecords as Uniconta.DataModel.Creditor[]).Where(d => d._Account.Length != AccountLength)?.ToArray();
            if (invalidCreditors.Length != 0)
            {
                var err = GetInvalidEntries(invalidCreditors, (acc) => acc._Account);
                UnicontaMessageBox.Show("Fehler bei Lieferanten: \n" + err, "", MessageBoxButton.OK);
                return false;
            }
             
            this.AccountLength = AccountLength - 1;
            return true;
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        private string GetInvalidEntries<T>(IEnumerable<T> entities, Func<T, string> GetErrorKey)
        {
            var sb = StringBuilderReuse.Create();

            foreach (var entity in entities)
            {
                sb.Append(GetErrorKey(entity));
                sb.Append(' ').Append('\n');
            }

            return sb.ToStringAndRelease();
        }

        internal class invoiceSort : IComparer<DCInvoice>
        {
            public int Compare(DCInvoice x, DCInvoice y)
            {
                return x._JournalPostedId - y._JournalPostedId;
            }
        }

        async void ExportToDatev()
        {
            if (!DataIsValidForExport(Accounts, Debtors, Creditors))
                return;
            

            var datev = await CreateDatevHeader(api);

            string glTransFileName = string.Format("EXTF_Buchungsstapel_{0}_{1}.csv", glTransExported._ToDate.ToString("ddMMyyyy"), glTransExported._MaxJournalPostedId);
            string DCFileName = string.Format("EXTF_DebitorenKreditoren_{0}_{1}.csv", glTransExported._ToDate.ToString("ddMMyyyy"), glTransExported._MaxJournalPostedId);
            string accountLabelFileName = string.Format("EXTF_Buchungstextkonstanten_{0}_{1}.csv", glTransExported._ToDate.ToString("ddMMyyyy"), glTransExported._MaxJournalPostedId);
            string defaultPath = datev.Path;

            if (String.IsNullOrEmpty(defaultPath))
            {
                UnicontaMessageBox.Show("Im DATEV-Informationen fehlen die Export Ordner", Uniconta.ClientTools.Localization.lookup("Exception"));
                return;
            }
            if (datev.FiscalYearBegin == DateTime.MinValue)
            {
                UnicontaMessageBox.Show("Im DATEV-Informationen fehlen die Anfang des Gesch�ftsjahres", Uniconta.ClientTools.Localization.lookup("Exception"));
                return;
            }
            if (String.IsNullOrEmpty(datev.Consultant))
            {
                UnicontaMessageBox.Show("Im DATEV-Informationen fehlen die Berater", Uniconta.ClientTools.Localization.lookup("Exception"));
                return;
            }
            if (String.IsNullOrEmpty(datev.Client))
            {
                UnicontaMessageBox.Show("Im DATEV-Informationen fehlen die Mandant", Uniconta.ClientTools.Localization.lookup("Exception"));
                return;
            }
            if (String.IsNullOrEmpty(datev.LanguageId))
            {
                UnicontaMessageBox.Show("Im DATEV-Informationen fehlen die Sprache", Uniconta.ClientTools.Localization.lookup("Exception"));
                return;
            }
            if (String.IsNullOrEmpty(datev.DefaultAccount))
            {
                UnicontaMessageBox.Show("Im DATEV-Informationen fehlen die Verrechnungskonto", Uniconta.ClientTools.Localization.lookup("Exception"));
                return;
            }
            
            if (UnicontaMessageBox.Show("DATEV exportieren ?", Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                try
                {
                    var filter = new[]
                    {
                        PropValuePair.GenereteWhereElements("Date", typeof(DateTime), String.Format("{0:d}..{1:d}", glTransExported._FromDate, glTransExported._ToDate)),
                        PropValuePair.GenereteWhereElements("DeliveryDate", typeof(DateTime), "!null")
                    };

                    var invSort = new invoiceSort();

                    var debInvoice = await api.Query<Uniconta.DataModel.DebtorInvoice>(filter);
                    if (debInvoice.Length > 0)
                        Array.Sort(debInvoice, invSort);
                    else
                        debInvoice = null;
                    var creInvoice = await api.Query<Uniconta.DataModel.CreditorInvoice>(filter);
                    if (creInvoice.Length > 0)
                        Array.Sort(creInvoice, invSort);
                    else
                        creInvoice = null;

                    SaveGLTransactions(datev, defaultPath + "/" + glTransFileName, debInvoice, creInvoice, invSort);
                    SaveDebtorCreditor(datev, defaultPath + "/" + DCFileName);
                    SaveGLAccountLabels(datev, defaultPath + "/" + accountLabelFileName);
                    UnicontaMessageBox.Show("DATEV Export erfolgreich", Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    UnicontaMessageBox.Show(ex);
                }
            }
        }

        private void SaveGLTransactions(DatevHeader datev, string fileName, DCInvoice[] debInvoice, DCInvoice[] creInvoice, invoiceSort invSort)
        {
            using (Stream stream = File.Create(fileName))
            {
                var df = new DateveWrapper(api.CompanyEntity, glTransExported, datev, AccountLength)
                {
                    Accounts = Accounts,
                    Debtors = Debtors,
                    Creditors = Creditors,
                    VATs = VATs,
                    Pays = Pays,
                    invSort = invSort,
                    debInvoice = debInvoice,
                    creInvoice = creInvoice
                };

                df.GenerateTransactionFile(dgAccountsTransGrid.ItemsSource as IEnumerable<GLTransClient>, new StreamWriter(stream, Encoding.UTF8));
            }
        }

        private void SaveDebtorCreditor(DatevHeader datev, string fileName)
        {
            using (Stream stream = File.Create(fileName))
            {
                var df = new DateveWrapper(api.CompanyEntity, glTransExported, datev, AccountLength)
                {
                    Accounts = Accounts,
                    Debtors = Debtors,
                    Creditors = Creditors,
                    VATs = VATs,
                    Pays = Pays
                };
                df.GenerateDCDatevFile(new StreamWriter(stream, Encoding.UTF8), Debtors, Creditors, Pays);
            }
        }

        private void SaveGLAccountLabels(DatevHeader datev, string fileName)
        {
            using (Stream stream = File.Create(fileName))
            {
                var df = new DateveWrapper(api.CompanyEntity, glTransExported, datev, AccountLength)
                {
                    Accounts = Accounts,
                    Debtors = Debtors,
                    Creditors = Creditors,
                    VATs = VATs,
                    Pays = Pays
                };
                df.GenerateDatevAccountLabels(new StreamWriter(stream, Encoding.UTF8), Accounts);
            }
        }

        static public async Task<DatevHeader> CreateDatevHeader(CrudAPI api)
        {
            var dh = new DatevHeader();
            var err = await api.Read(dh);
            return dh;
        }

        static public void SaveDatevHeader(DatevHeader dh, CrudAPI api)
        {
            if (dh.RowId == 0)
                api.InsertNoResponse(dh);
            else
                api.UpdateNoResponse(dh);
        }

        public class DatevHeader : CompanyParameter
        {
            public string Consultant, Client, Path, DefaultAccount, LanguageId;
            public DateTime FiscalYearBegin;

            public override int TableId { get { return 10923852; } }
            public override int DataVersion { get { return 1; } }

            public override void LoadData(CustomReader r, int SavedWithVersion)
            {
                if (SavedWithVersion != 1)
                    VersionException.Exit(this, SavedWithVersion);
                Consultant = r.readString();
                Client = r.readString();
                Path = r.readString();
                DefaultAccount = r.readString();
                LanguageId = r.readString();
                if (LanguageId == null)
                    LanguageId = "de-DE";
                FiscalYearBegin = Streaming.readSmallDate(r);
            }

            public override void SaveData(CustomWriter w)
            {
                w.write(Consultant);
                w.write(Client);
                w.write(Path);
                w.write(DefaultAccount);
                w.write(LanguageId);
                Streaming.writeSmallDate(FiscalYearBegin, w);
            }
        }

        public class DateveWrapper
        {
            // TODO: Add Required information
            string consultant, client, defaultContraAccount, LanguageId;
            public DateTime fromDate, toDate, fiscalYearBegin;
            string currency;
            private int AccountLength;

            List<int> EmptyFieldIndicesDatevPosting;
            List<int> EmptyFieldIndicesDCDatev;
            List<int> EmptyFieldIndicesDatevAccountLabel;
            string TwoLetterISOLanguageName;
            byte Country;
            GLTransExportedClient glTransExported;
            public SQLCache Accounts, Debtors, Creditors, VATs, Pays;
            HashSet<string> bankAccounts;
            string[] line;
            internal invoiceSort invSort;
            DebtorInvoice invSearch;
            internal DCInvoice[] debInvoice, creInvoice;

            public DateveWrapper(Company company, GLTransExportedClient glTransExported, DatevHeader datev, int AccountLength)
            {
                this.AccountLength = AccountLength;
                consultant = datev.Consultant;
                client = datev.Client;
                defaultContraAccount = datev.DefaultAccount;
                fiscalYearBegin = datev.FiscalYearBegin;
                LanguageId = datev.LanguageId;

                PopulateEmptyFieldIndexLists();

                Country = company._Country;
                TwoLetterISOLanguageName = ((CountryISOCode)Country).ToString().ToUpper();

                currency = AppEnums.Currencies.ToString(company._Currency);

                this.glTransExported = glTransExported;
                fromDate = glTransExported._FromDate;
                toDate = glTransExported._ToDate;
                line = new string[300];

                invSearch = new DebtorInvoice();
            }

            void PopulateEmptyFieldIndexLists()
            {
                //Postings
                var list = new List<int>(100) { 8, 11, 15, 41, 90, 94, 95, 97, 101, 102, 104, 106, 108, 109, 111, 117, 119};

                list.AddRange(Enumerable.Range(19, 17)); // Start from 19 and count 17 times up... (19 - 35)
                list.AddRange(Enumerable.Range(47, 40)); // Start from 47 and counts 40 times up... (47 - 86) 
                EmptyFieldIndicesDatevPosting = list;


                //DCDatev
                list = new List<int>(200) { 16, 17, 96, 97, 98, 99, 101, 102, 103, 106, 132, 133, 219, 95, 243, 246, 248};

                list.AddRange(Enumerable.Range(2, 12)); // 2 - 13
                list.AddRange(Enumerable.Range(20, 5)); // 20 - 24
                list.AddRange(Enumerable.Range(28, 21)); // 28 - 48
                list.AddRange(Enumerable.Range(51, 9)); // 51 - 59
                list.AddRange(Enumerable.Range(62, 9)); // 62 - 70
                list.AddRange(Enumerable.Range(73, 9)); // 73 - 81
                list.AddRange(Enumerable.Range(84, 9)); // 84 - 92
                list.AddRange(Enumerable.Range(135, 27)); // 135 - 162
                list.AddRange(Enumerable.Range(164, 9)); // 164 - 172
                list.AddRange(Enumerable.Range(175, 9)); // 175 - 183
                list.AddRange(Enumerable.Range(186, 9)); // 186 - 104
                list.AddRange(Enumerable.Range(197, 9)); // 197 - 205
                list.AddRange(Enumerable.Range(208, 9)); // 208 - 216
                list.AddRange(Enumerable.Range(221, 10)); // 222 - 230
                EmptyFieldIndicesDCDatev = list;

                list = new List<int>();
                EmptyFieldIndicesDatevAccountLabel = list;
            }

            public void GenerateDatevAccountLabels(TextWriter fileStream, SQLCache GLAccounts)
            {
                var header = CreateBasicHeader();
                header.DataCategory = 20;
                header.FormatName = "Kontenbeschriftungen";
                header.FormatVersion = 3; //PG von 2 auf 3
                string AccountLabelsHeadline = "Konto;Kontobeschriftung;SprachId;Kontenbeschriftung lang"; //PG ein neue feld

                fileStream.WriteLine(header.getHeaderString());
                fileStream.WriteLine(AccountLabelsHeadline);

                foreach (var gla in GLAccounts.GetRecords as GLAccount[])
                {
                    var acc = new DATEVAccountLabel() { Account = gla, LanguageId = LanguageId };
                    MakeCSV(acc, EmptyFieldIndicesDatevAccountLabel, 4, fileStream); //PG
                }
                fileStream.Close();
            }

            #region DCDatev
            public void GenerateDCDatevFile(TextWriter fileStream, SQLCache Debtors, SQLCache Creditors, SQLCache Payments)
            {
                var header = CreateBasicHeader();
                header.DataCategory = 16;
                header.FormatName = "Debitoren/Kreditoren";
                header.FormatVersion = 5;
                string CustomerVendorHeadline = "Konto;Name (Adressattyp Unternehmen);Unternehmensgegenstand;Name (Adressattyp nat�rl. Person);Vorname (Adressattyp nat�rl. Person);Name (Adressattyp keine Angabe);Adressattyp;Kurzbezeichnung;EU-Land;EU-UStID;Anrede;Titel/Akad. Grad;Adelstitel;Namensvorsatz;Adressart;Stra�e;Postfach;Postleitzahl;Ort;Land;Versandzusatz;Adresszusatz;Abweichende Anrede;Abw. Zustellbezeichnung 1;Abw. Zustellbezeichnung 2;Kennz. Korrespondenzadresse;Adresse G�ltig von;Adresse G�ltig bis;Telefon;Bemerkung (Telefon);Telefon GL;Bemerkung (Telefon GL);E-Mail;Bemerkung (E-Mail);Internet;Bemerkung (Internet);Fax;Bemerkung (Fax);Sonstige;Bemerkung (Sonstige);Bankleitzahl 1;Bankbezeichnung 1;Bank-Kontonummer 1;L�nderkennzeichen 1;IBAN-Nr. 1;IBAN1 korrekt;SWIFT-Code 1;Abw. Kontoinhaber 1;Kennz. Hauptbankverb. 1;Bankverb 1 G�ltig von;Bankverb 1 G�ltig bis;Bankleitzahl 2;Bankbezeichnung 2;Bank-Kontonummer 2;L�nderkennzeichen 2;IBAN-Nr. 2;IBAN2 korrekt;SWIFT-Code 2;Abw. Kontoinhaber 2;Kennz. Hauptbankverb. 2;Bankverb 2 G�ltig von;Bankverb 2 G�ltig bis;Bankleitzahl 3;Bankbezeichnung 3;Bank-Kontonummer 3;L�nderkennzeichen 3;IBAN-Nr. 3;IBAN3 korrekt;SWIFT-Code 3;Abw. Kontoinhaber 3;Kennz. Hauptbankverb. 3;Bankverb 3 G�ltig von;Bankverb 3 G�ltig bis;Bankleitzahl 4;Bankbezeichnung 4;Bank-Kontonummer 4;L�nderkennzeichen 4;IBAN-Nr. 4;IBAN4 korrekt;SWIFT-Code 4;Abw. Kontoinhaber 4;Kennz. Hauptbankverb. 4;Bankverb 4 G�ltig von;Bankverb 4 G�ltig bis;Bankleitzahl 5;Bankbezeichnung 5;Bank-Kontonummer 5;L�nderkennzeichen 5;IBAN-Nr. 5;IBAN5 korrekt;SWIFT-Code 5;Abw. Kontoinhaber 5;Kennz. Hauptbankverb. 5;Bankverb 5 G�ltig von;Bankverb 5 G�ltig bis;Leerfeld;Briefanrede;Gru�formel;Kundennummer;Steuernummer;Sprache;Ansprechpartner;Vertreter;Sachbearbeiter;Diverse-Konto;Ausgabeziel;W�hrungssteuerung;Kreditlimit (Debitor);Zahlungsbedingung;F�lligkeit in Tagen (Debitor);Skonto in Prozent (Debitor);Kreditoren-Ziel 1 Tg.;Kreditoren-Skonto 1 %;Kreditoren-Ziel 2 Tg.;Kreditoren-Skonto 2 %;Kreditoren-Ziel 3 Brutto Tg.;Kreditoren-Ziel 4 Tg.;Kreditoren-Skonto 4 %;Kreditoren-Ziel 5 Tg.;Kreditoren-Skonto 5 %;Mahnung;Kontoauszug;Mahntext 1;Mahntext 2;Mahntext 3;Kontoauszugstext;Mahnlimit Betrag;Mahnlimit %;Zinsberechnung;Mahnzinssatz 1;Mahnzinssatz 2;Mahnzinssatz 3;Lastschrift;Verfahren;Mandantenbank;Zahlungstr�ger;Indiv. Feld 1;Indiv. Feld 2;Indiv. Feld 3;Indiv. Feld 4;Indiv. Feld 5;Indiv. Feld 6;Indiv. Feld 7;Indiv. Feld 8;Indiv. Feld 9;Indiv. Feld 10;Indiv. Feld 11;Indiv. Feld 12;Indiv. Feld 13;Indiv. Feld 14;Indiv. Feld 15;Abweichende Anrede (Rechnungsadresse);Adressart (Rechnungsadresse);Stra�e (Rechnungsadresse);Postfach (Rechnungsadresse);Postleitzahl (Rechnungsadresse);Ort (Rechnungsadresse);Land (Rechnungsadresse);Versandzusatz (Rechnungsadresse);Adresszusatz (Rechnungsadresse);Abw. Zustellbezeichnung 1 (Rechnungsadresse);Abw. Zustellbezeichnung 2 (Rechnungsadresse);Adresse G�ltig von (Rechnungsadresse);Adresse G�ltig bis (Rechnungsadresse);Bankleitzahl 6;Bankbezeichnung 6;Bank-Kontonummer 6;L�nderkennzeichen 6;IBAN-Nr. 6;IBAN6 korrekt;SWIFT-Code 6;Abw. Kontoinhaber 6;Kennz. Hauptbankverb. 6;Bankverb 6 G�ltig von;Bankverb 6 G�ltig bis;Bankleitzahl 7;Bankbezeichnung 7;Bank-Kontonummer 7;L�nderkennzeichen 7;IBAN-Nr. 7;IBAN7 korrekt;SWIFT-Code 7;Abw. Kontoinhaber 7;Kennz. Hauptbankverb. 7;Bankverb 7 G�ltig von;Bankverb 7 G�ltig bis;Bankleitzahl 8;Bankbezeichnung 8;Bank-Kontonummer 8;L�nderkennzeichen 8;IBAN-Nr. 8;IBAN8 korrekt;SWIFT-Code 8;Abw. Kontoinhaber 8;Kennz. Hauptbankverb. 8;Bankverb 8 G�ltig von;Bankverb 8 G�ltig bis;Bankleitzahl 9;Bankbezeichnung 9;Bank-Kontonummer 9;L�nderkennzeichen 9;IBAN-Nr. 9;IBAN9 korrekt;SWIFT-Code 9;Abw. Kontoinhaber 9;Kennz. Hauptbankverb. 9;Bankverb 9 G�ltig von;Bankverb 9 G�ltig bis;Bankleitzahl 10;Bankbezeichnung 10;Bank-Kontonummer 10;L�nderkennzeichen 10;IBAN-Nr. 10;IBAN10 korrekt;SWIFT-Code 10;Abw. Kontoinhaber 10;Kennz. Hauptbankverb. 10;Bankverb 10 G�ltig von;Bankverb 10 G�ltig bis;Nummer Fremdsystem;Insolvent;Mandatsreferenz 1;Mandatsreferenz 2;Mandatsreferenz 3;Mandatsreferenz 4;Mandatsreferenz 5;Mandatsreferenz 6;Mandatsreferenz 7;Mandatsreferenz 8;Mandatsreferenz 9;Mandatsreferenz 10;Verkn�pftes OPOS-Konto;Mahnsperre bis;Lastschriftsperre bis;Zahlungssperre bis;Geb�hrenberechnung;Mahngeb�hr 1;Mahngeb�hr 2;Mahngeb�hr 3;Pauschalenberechnung;Verzugspauschale 1;Verzugspauschale 2;Verzugspauschale 3;Alternativer Suchname;Status;Anschrift manuell ge�ndert(Korrespondenzadresse);Anschrift individuell(Korrespondenzadresse;Anschrift manuell ge�ndert(Rechnungsadresse;Anschrift individuell (Rechnungsadresse);Fristberechnung bei Debitor;Mahnfrist 1;Mahnfrist 2;Mahnfrist 3;Letzte Frist";

                fileStream.WriteLine(header.getHeaderString());
                fileStream.WriteLine(CustomerVendorHeadline);

                buildCSV(Debtors.GetRecords as DCAccount[], Payments, fileStream);
                buildCSV(Creditors.GetRecords as DCAccount[], Payments, fileStream);
                fileStream.Close();
            }

            void buildCSV(DCAccount[] accounts, SQLCache Payments, TextWriter fileStream)
            {
                foreach (var dca in accounts)
                {
                    var acc = new DCDATEV() { Payment = (PaymentTerm)Payments.Get(dca._Payment), dcAccount = dca };
                    if (dca._Country != 0 && (byte)dca._Country != this.Country)
                        acc.TwoLetterISOLanguageName = ((CountryISOCode)dca._Country).ToString().ToUpper();
                    else
                        acc.TwoLetterISOLanguageName = TwoLetterISOLanguageName;
                    MakeCSV(acc, EmptyFieldIndicesDCDatev, 254, fileStream);
                }
            }

            #endregion

            #region Transactions
            public void GenerateTransactionFile(IEnumerable<GLTransClient> trans, TextWriter fileStream)
            {
                var dcSumAccounts = new HashSet<string>();
                var dcVATAccounts = new HashSet<string>();
                this.bankAccounts = new HashSet<string>();
                foreach (var ac in (IEnumerable<GLAccount>)Accounts.GetNotNullArray)
                {
                    if (ac._AccountType == (byte)GLAccountTypes.Debtor || ac._AccountType == (byte)GLAccountTypes.Creditor)
                        dcSumAccounts.Add(ac._Account);
                    else if (ac._AccountType == (byte)GLAccountTypes.Bank)
                        bankAccounts.Add(ac._Account);
                    else if (ac._SystemAccount == (byte)SystemAccountTypes.SalesTaxPayable || ac._SystemAccount == (byte)SystemAccountTypes.VatRounding || ac._SystemAccount == (byte)SystemAccountTypes.SalesTaxReceiveable)
                        dcVATAccounts.Add(ac._Account);
                }

                var header = CreateBasicHeader();
                header.DataCategory = 21;
                header.FormatName = "Buchungsstapel";
                header.FormatVersion = 12; //PG fra 9 til 12
                string EntriesHeadline = "Umsatz (ohne Soll/Haben-Kz);Soll/Haben-Kennzeichen;WKZ Umsatz;Kurs;Basis-Umsatz;WKZ Basis-Umsatz;Konto;Gegenkonto (ohne BU-Schl�ssel);BU-Schl�ssel;Belegdatum;Belegfeld 1;Belegfeld 2;Skonto;Buchungstext;Postensperre;Diverse Adressnummer;Gesch�ftspartnerbank;Sachverhalt;Zinssperre;Beleglink;Beleginfo - Art 1;Beleginfo - Inhalt 1;Beleginfo - Art 2;Beleginfo - Inhalt 2;Beleginfo - Art 3;Beleginfo - Inhalt 3;Beleginfo - Art 4;Beleginfo - Inhalt 4;Beleginfo - Art 5;Beleginfo - Inhalt 5;Beleginfo - Art 6;Beleginfo - Inhalt 6;Beleginfo - Art 7;Beleginfo - Inhalt 7;Beleginfo - Art 8;Beleginfo - Inhalt 8;KOST1 - Kostenstelle;KOST2 - Kostenstelle;Kost-Menge;EU-Land u. UStID;EU-Steuersatz;Abw. Versteuerungsart;Sachverhalt L+L;Funktionserg�nzung L+L;BU 49 Hauptfunktionstyp;BU 49 Hauptfunktionsnummer;BU 49 Funktionserg�nzung;Zusatzinformation - Art 1;Zusatzinformation- Inhalt 1;Zusatzinformation - Art 2;Zusatzinformation- Inhalt 2;Zusatzinformation - Art 3;Zusatzinformation- Inhalt 3;Zusatzinformation - Art 4;Zusatzinformation- Inhalt 4;Zusatzinformation - Art 5;Zusatzinformation- Inhalt 5;Zusatzinformation - Art 6;Zusatzinformation- Inhalt 6;Zusatzinformation - Art 7;Zusatzinformation- Inhalt 7;Zusatzinformation - Art 8;Zusatzinformation- Inhalt 8;Zusatzinformation - Art 9;Zusatzinformation- Inhalt 9;Zusatzinformation - Art 10;Zusatzinformation- Inhalt 10;Zusatzinformation - Art 11;Zusatzinformation- Inhalt 11;Zusatzinformation - Art 12;Zusatzinformation- Inhalt 12;Zusatzinformation - Art 13;Zusatzinformation- Inhalt 13;Zusatzinformation - Art 14;Zusatzinformation- Inhalt 14;Zusatzinformation - Art 15;Zusatzinformation- Inhalt 15;Zusatzinformation - Art 16;Zusatzinformation- Inhalt 16;Zusatzinformation - Art 17;Zusatzinformation- Inhalt 17;Zusatzinformation - Art 18;Zusatzinformation- Inhalt 18;Zusatzinformation - Art 19;Zusatzinformation- Inhalt 19;Zusatzinformation - Art 20;Zusatzinformation- Inhalt 20;St�ck;Gewicht;Zahlweise;Forderungsart;Veranlagungsjahr;Zugeordnete F�lligkeit;Skontotyp;Auftragsnummer;Buchungstyp;Ust-Schl�ssel (Anzahlungen);EU-Land (Anzahlungen);Sachverhalt L+L (Anzahlungen);EU-Steuersatz (Anzahlungen);Erl�skonto (Anzahlungen);Herkunft-Kz;Leerfeld;KOST-Datum;Mandatsreferenz;Skontosperre;Gesellschaftername;Beteiligtennummer;Identifikationsnummer;Zeichnernummer;Postensperre bis;Bezeichnung SoBil-Sachverhalt;Kennzeichen SoBil-Buchung;Festschreibung;Leistungsdatum;Datum Zuord.Steuerperiode;F�lligkeit;Generalumkehr (GU);Steuersatz;Land;Abrechnungsreferenz;BVV-Position;EU-Land u. UStID (Ursprung);EU-Steuersatz (Ursprung)";  //PG Datev 4 Neue

                fileStream.WriteLine(header.getHeaderString());
                fileStream.WriteLine(EntriesHeadline);

                List<GLTransClient> voucher = new List<GLTransClient>(100);
                int LastVoucher = -1;
                bool hasSumAccount = false, hasBankAccount = false;
                bool DefaultContraAccount = false;
                foreach (var tc in trans)
                {
                    if (tc._AmountCent == 0 || tc._PrimoTrans || tc._YearTransfer)
                        continue;

                    if (dcVATAccounts.Contains(tc._Account) && (tc._Vat != null || tc._DCAccount == null || (tc._DCType > 0 && (byte)tc._DCType <= 2)))
                        continue;

                    if (tc._Voucher != LastVoucher)
                    {
                        GenerateDatevPostingLines(voucher, fileStream, !(hasSumAccount || hasBankAccount), DefaultContraAccount);
                        voucher.Clear();
                        LastVoucher = tc.Voucher;
                        hasSumAccount = false;
                    }

                    if (tc._DCType > 0 && (byte)tc._DCType <= 2) 
                    {
                        if (dcSumAccounts.Contains(tc._Account))
                        {
                            hasSumAccount = true;
                            continue;
                        }
                        if (bankAccounts.Contains(tc._Account))
                            hasBankAccount = true;
                    }

                    voucher.Add(tc);
                }
                GenerateDatevPostingLines(voucher, fileStream, ! (hasSumAccount || hasBankAccount), DefaultContraAccount);
                fileStream.Close();
            }

            void GenerateDatevPostingLines(List<GLTransClient> trans, TextWriter w, bool removeOffsetAccount, bool useDefaultContraAccount)
            {
                string OffsetAccount = null;

                if (removeOffsetAccount)
                {
                    for (int i = 0; (i < trans.Count); i++)
                    {
                        if (trans[i]._Vat == null) // we just need to find if we have atleast one without vat, then we look for offset account
                        {
                            if (trans.Count == 1)
                            {
                                OffsetAccount = trans[0]._DCAccount;
                            }
                            else if (trans.Count == 2)
                            {
                                OffsetAccount = trans[0]._Account;
                                trans.RemoveAt(0);
                            }
                            else
                            {
                                int DebetPos = 0, CreditPos = 0, cnt = 0;
                                GLTransClient Debet = null, Credit = null;
                                foreach (var tc in trans)
                                {
                                    cnt++;
                                    if (tc._AmountCent > 0)
                                    {
                                        if (Debet == null)
                                        {
                                            Debet = tc;
                                            DebetPos = cnt;
                                        }
                                        else
                                            DebetPos = 0;
                                    }
                                    else if (tc._AmountCent < 0)
                                    {
                                        if (Credit == null)
                                        {
                                            Credit = tc;
                                            CreditPos = cnt;
                                        }
                                        else
                                            CreditPos = 0;
                                    }
                                }
                                if (DebetPos != 0)
                                {
                                    OffsetAccount = Debet._Account;
                                    if (trans.Count > 1)
                                        trans.RemoveAt(DebetPos - 1);
                                }
                                else if (CreditPos != 0)
                                {
                                    OffsetAccount = Credit._Account;
                                    if (trans.Count > 1)
                                        trans.RemoveAt(CreditPos - 1);
                                }
                            }
                            break; // break loop where we found one without VAT
                        }
                    }
                }

                foreach (var tc in trans)
                {
                    string cAcc;
                    DateTime DueDate = DateTime.MinValue;

                    if ((byte)tc._DCType >= 1 && (byte)tc._DCType <= 2)
                    {
                        cAcc = tc._DCAccount;
                        if (tc._Invoice > 0 && !bankAccounts.Contains(tc._Account)) //Datev Rechnungsnummer
                        {
                            var cache = (byte)tc._DCType == 1 ? this.Debtors : this.Creditors;
                            var rec = (DCAccount)cache.Get(cAcc);
                            var payment = (PaymentTerm)this.Pays.Get(rec?._Payment);
                            if (payment != null)
                                DueDate = payment.GetDueDate(tc._Date);
                        }
                    }
                    else
                        cAcc = OffsetAccount;

                    var post = ToDatevPosting(tc, currency, cAcc, DueDate);
                    MakeCSV(post, EmptyFieldIndicesDatevPosting, 124, w); //PG new Format Version 12 Datev 12 im Version 700
                }
            }

            public DATEVPosting ToDatevPosting(GLTransClient tc, string companyCurr, string dcaccount, DateTime DueDate)
            {
                if (tc._JournalPostedId == 2593)
                {

                    var stop = 2;
                
                }
                
                var vat = (this.VATs.Get(tc._Vat) as GLVat);
                var isAutoAccount = (this.Accounts.Get(tc.Account) as GLAccount)._DATEVAuto;

                var Inv1 = (tc._Invoice > 0) ? tc._Invoice : tc._Voucher;

                var Ptext = tc.Text;    //Datev l�nge auf 60
                if (Ptext != null)
                {
                    Ptext = Ptext.Replace("\"", "");
                    if (Ptext.Length > 60)
                        Ptext = Ptext.Remove(60);
                }

                var entry = new DATEVPosting()
                {
                    Account = tc._Account,
                    KOST1 = tc._Dimension1,
                    KOST2 = tc._Dimension2,
                    VAT = isAutoAccount ? null : vat?._ExternalCode,
                    TransactionValue = Math.Abs(tc._Amount) + (vat?._OffsetAccount == null ? Math.Abs(tc.AmountVat) : 0),
                    TransactionValueCurr = companyCurr,
                    DebitCreditLabel = tc._AmountCent > 0 ? "S" : "H", // H = Creditor (Haben), S = Debitor (Soll)
                    InvoiceField1 = Inv1,

                    KOSTQuantity = (long)tc._Qty,
                    InvoiceDate = tc._Date,
                    DocumentRef = tc._DocumentRef,
                    DueDate = DueDate,
                    Date = tc._Date,
                    ExchangeRate = tc._AmountCurCent == 0 || tc._AmountCent == 0 ? 0d : Math.Round((Math.Abs(tc._AmountCurCent) / (double)Math.Abs(tc._AmountCent)), 6),
                    PostingText = Ptext,                    //Datev l�nge auf 60                    
                    cID = tc.CompanyId  //DT Per                    
                };

                if (dcaccount != null)
                {
                    entry.ContraAccount = dcaccount;
                    entry.EUMemberStateAndVATI = tc.LegalIdent;

                    DCInvoice[] arr;
                    if (tc._DCType == GLTransRefType.Debtor)
                    {                     
                        arr = this.debInvoice;
                        entry.DocumentRef = tc.JournalPostedId; //PG Guid p� Debitor poster!
                    }
                    else if (tc._DCType == GLTransRefType.Creditor)
                        arr = this.creInvoice;
                    else
                        arr = null;
                    if (arr != null)
                    {
                        this.invSearch._JournalPostedId = tc._JournalPostedId;
                        var pos = Array.BinarySearch(arr, invSearch, invSort);
                        if (pos >= 0 && pos < arr.Length)
                            entry.DelvDate = arr[pos]._DeliveryDate;
                    }
                }
                else
                    entry.ContraAccount = defaultContraAccount;

                if (tc._Currency != 0)
                {
                    entry.BaseTransValueCurr = tc.Currency;
                    entry.BaseTransValue = Math.Abs(tc.AmountCur);
                }

                return entry;
            }
            #endregion

            private void MakeCSV(DatevEntity entitiy, List<int> emptyFieldIndices, int length, TextWriter w)
            {
                var line = this.line;
                int i;
                for (i = 0; (i < emptyFieldIndices.Count); i++)
                    line[emptyFieldIndices[i]] = "\"\"";

                entitiy.ToDatevArray(line);

                for (i = 0; (i < length); i++)
                {
                    if (i > 0)
                        w.Write(';');
                    w.Write(line[i]);
                }
                Array.Clear(line, 0, line.Length); // we clear it so we can reuse it in next call
                w.WriteLine();
            }

            private Header CreateBasicHeader()
            {
                var header = new Header()
                {
                    FormatLabel = "EXTF",
                    VersionNumber = 700,
                    CreatedOn = DateTime.Now.ToString("yyyyMMddhhmmss") + "000",
                    Consultant = consultant,
                    Client = client,
                    FYBegin = fiscalYearBegin.ToString("yyyyMMdd"),
                    LedgerAccountLength = AccountLength,
                    DateFrom = fromDate.ToString("yyyyMMdd"),
                    DateTo = toDate.ToString("yyyyMMdd"),
                    CurrencyCode ="EUR" //DT Per
                };
                return header;
            }
        }

        public abstract class DatevEntity
        {
            public abstract void ToDatevArray(string[] lines);

            internal string FirstN(string str, int n)
            {
                if (str == null || str.Length <= n)
                    return str;
                else
                    return str.Substring(0, n);
            }

            internal string AddSingleQuotes(string text)
            {
                if (text != null && text.Length > 0)
                    return "\"" + text + "\"";
                else
                    return "\"\"";

            }
            internal string AddDoubleQuotes(string text)  //DT Per
            {
                if (text != null && text.Length > 0)
                    return "\"" + "\"" + text + "\"" + "\"";
                else
                    return "\"\"" + "\"\"";
            }
            static public Guid GetInvoiceGuid(DCAccount dc, short Date, long Invoice, int JournalPostedId)    //DT Per
            {
                return GetInvoiceGuid(dc.RowId, dc.CompanyId, Date, Invoice, JournalPostedId);
            }

            static public Guid GetInvoiceGuid(int Account, int CompanyId, short Date, long Invoice, int JournalPostedId)   //DT Per
            {
                var i1 = Account ^ 0x5453df31;
                var i2 = (CompanyId << 1) ^ (0x7e094f31 * Account);
                var i3 = (((uint)Date << 16) | (uint)(Invoice & 0xFFFF)) ^ (0x3C234942 * Account);
                var i4 = JournalPostedId ^ (0x21E39982 * Account);
                var docguid = new Guid((uint)i1, (ushort)i2, (ushort)(i2 >> 16), (byte)i3, (byte)(i3 >> 8), (byte)(i3 >> 16), (byte)(i3 >> 24), (byte)i4, (byte)(i4 >> 8), (byte)(i4 >> 16), (byte)(i4 >> 24));
                StringBuilder version = new StringBuilder(Convert.ToString(docguid)); //DT Per version have to be 4.
                version[14] = '4'; 
                var docguidtext = version.ToString();
                docguid = new Guid(docguidtext);
                return docguid;

            }

            static public Guid GetInvoiceGuid(int CompanyId, short Date, int DocumentRef)
            {
                if (DocumentRef == 0)
                    return Guid.Empty;
                const int Account = 1;
                const int Invoice = 0;
                var i1 = Account ^ 0x5453df31;
                var i2 = (CompanyId << 1) ^ (0x7e094f31 * Account);
                var i3 = (((uint)Date << 16) | (uint)(Invoice & 0xFFFF)) ^ (0x3C234942 * Account);
                var i4 = DocumentRef ^ (0x21E39982 * Account);
                var docguid = new Guid((uint)i1, (ushort)i2, (ushort)(i2 >> 16), (byte)i3, (byte)(i3 >> 8), (byte)(i3 >> 16), (byte)(i3 >> 24), (byte)i4, (byte)(i4 >> 8), (byte)(i4 >> 16), (byte)(i4 >> 24));
                StringBuilder version = new StringBuilder(Convert.ToString(docguid)); //DT Per version have to be 4.
                version[14] = '4'; 
                var docguidtext = version.ToString();
                docguid = new Guid(docguidtext);               
                return docguid;
            }
        }

        public class DATEVPosting : DatevEntity
        {
            public double TransactionValue;
            public string DebitCreditLabel;
            public string TransactionValueCurr;
            public double ExchangeRate;
            public double BaseTransValue;
            public string BaseTransValueCurr;
            public string Account;
            public DateTime InvoiceDate, DueDate;
            public string ContraAccount;
            public int InvoiceField1;
            public string PostingText;
            public string KOST1;
            public string KOST2;
            public string VAT;
            public long KOSTQuantity;
            public string EUMemberStateAndVATI;
            public int DocumentRef;
            public DateTime Date;
            public string Status;
            public string logId;
            public Guid LinkGuid; //DT Per
            public int cID;
            public DateTime DelvDate; //DT Per 90

            public override void ToDatevArray(string[] line)
            {
                line[0] = TransactionValue.ToString();
                line[1] = AddSingleQuotes(DebitCreditLabel);
                line[2] = TransactionValueCurr == null ? "\"\"" : AddSingleQuotes(TransactionValueCurr);
                line[3] = ExchangeRate == 0 ? "" : ExchangeRate.ToString();
                line[4] = BaseTransValue == 0 ? "" : BaseTransValue.ToString();
                line[5] = AddSingleQuotes(BaseTransValueCurr);
                line[6] = Account;
                line[7] = ContraAccount;
                line[8] = AddSingleQuotes(VAT);
                line[9] = InvoiceDate == DateTime.MinValue ? "" : InvoiceDate.ToString("ddMM");
                line[10] = InvoiceField1 == 0 ? "\"\"" : AddSingleQuotes(NumberConvert.ToString(InvoiceField1));
                line[11] = DueDate == DateTime.MinValue ? "\"\"" : AddSingleQuotes(DueDate.ToString("ddMMyy"));
                line[19] = AddSingleQuotes("BEDI " + AddDoubleQuotes(Convert.ToString(GetInvoiceGuid(cID, SmallDate.Pack(Date), DocumentRef))));  //DT Per
                line[13] = AddSingleQuotes(PostingText);
                line[36] = AddSingleQuotes(KOST1);
                line[37] = AddSingleQuotes(KOST2);
                line[38] = AddSingleQuotes(""); //DT Per
                line[39] = AddSingleQuotes(EUMemberStateAndVATI);
                line[114] = DelvDate == DateTime.MinValue ? "\"\"" : AddSingleQuotes(DelvDate.ToString("ddMMyyyy"));
                line[120] = AddSingleQuotes(""); //DT PG
                line[121] = AddSingleQuotes(""); //DT PG
                line[122] = AddSingleQuotes(""); //DT PG

            }
        }

        public class DCDATEV : DatevEntity
        {
            public DCAccount dcAccount;
            public PaymentTerm Payment;
            public string TwoLetterISOLanguageName;

            public override void ToDatevArray(string[] line)
            {
                var dcAccount = this.dcAccount;
                line[0] = dcAccount._Account;
                line[1] = AddSingleQuotes(dcAccount._Name);
                var Ust = dcAccount._LegalIdent;
                if (Ust != null && Ust.Length >= 2) //Ust im Stamdaten
                {
                    line[8] = AddSingleQuotes(Ust.Substring(0, 2));
                    line[9] = AddSingleQuotes(Ust.Substring(2));
                }
                line[14] = "\"STR\""; //TODO: Should this be included?
                line[15] = AddSingleQuotes(FirstN(dcAccount._Address1, 36));
                line[17] = AddSingleQuotes(dcAccount._ZipCode);
                line[18] = AddSingleQuotes(dcAccount._City);
                line[19] = AddSingleQuotes(TwoLetterISOLanguageName); //Datev �nderkennzeichnung in Gro�buchstaben
                line[44] = AddSingleQuotes(dcAccount._PaymentId);
                line[46] = AddSingleQuotes(dcAccount._SWIFT);

                if (this.Payment != null)
                {
                    int offset = (dcAccount.__DCType() == 1) ? 0 : 2; // debtor ? creditor
                    line[109 + offset] = NumberConvert.ToString(this.Payment._Days);
                    line[110 + offset] = Payment._CashDiscountPct != 0 ? Convert.ToString(Payment._CashDiscountPct) : "0";
                }
            }
        }

        public class DATEVAccountLabel : DatevEntity
        {
            public GLAccount Account;
            public string LanguageId;

            public override void ToDatevArray(string[] line)
            {
                line[0] = FormatAccount(Account._Account);
                line[1] = AddSingleQuotes(FirstN(Account._Name, 40));
                line[2] = AddSingleQuotes(LanguageId);
                line[3] = AddSingleQuotes("");   //PG

            }

            private string FormatAccount(string a)
            {
                if (a.Length >= 4) return a;
                return a.PadLeft(4, '0');
            }
        }

        class Header
        {   //PG
            public string FormatLabel;
            public int VersionNumber;
            public int DataCategory;
            public string FormatName;
            public int FormatVersion;
            public string CreatedOn;
            public int Imported;
            public string Origin;
            public string ExportedBy;
            public string ImportedBy;
            public string Consultant;
            public string Client;
            public string FYBegin;
            public int LedgerAccountLength;
            public string DateFrom;
            public string DateTo;
            public string Label;
            public string Initials;
            public int RecordType;
            public int AccountingReason;
            public int Locking;
            public string CurrencyCode;            
            public string Reserved27;            
            public string Application;

            private HashSet<int> dataCategories = new HashSet<int>() { 21, 65, 67, 20, 47, 16, 44, 46, 48, 63, 62 };
            private HashSet<string> formatNames = new HashSet<string>() { "Buchungsstapel", "Wiederkehrende Buchungen", "Buchungstextkonstanten", "Kontenbeschriftungen", "Debitoren/Kreditoren",
                                                                "Textschl�ssel", "Zahlungsbedingungen", "Diverse Adressen", "Anlagenbuchf�hrung � Buchungssatzvorlagen", " Anlagenbuchf�hrung � Filialen"};
            private HashSet<int> formatVersions = new HashSet<int>() { 1, 2, 5,  };

            public string getHeaderString()
            {               
                //PG
                return "\"" + FormatLabel + "\"" + ";" +
                        VersionNumber + ";" +
                        DataCategory + ";" +
                        "\"" + FormatName + "\"" + ";" +
                        FormatVersion + ";" +
                        CreatedOn + ";" +
                        /*imported*/ ";" +
                        "\"" + Origin + "\"" + ";" +
                        "\"" + ExportedBy + "\"" + ";" +
                        "\"" + ImportedBy + "\"" + ";" +
                        Consultant + ";" +
                        Client + ";" +
                        FYBegin + ";" +
                        LedgerAccountLength + ";" +
                        DateFrom + ";" +
                        DateTo + ";" +
                        "\"" + Label + "\"" + ";" +
                        "\"" + Initials + "\"" + ";" +
                        RecordType + "1;" +
                        AccountingReason + ";" +
                        Locking + ";" +
                        "\"" + CurrencyCode + "\"" + ";" +
                        ";" +
                        "\"" + "\"" + ";" +
                        ";" + ";" +
                        "\"" + Reserved27 + "\"" + ";" +
                        ";" + ";" +
                        "\"" + "\"" + ";" +
                        "\"" + Application + "\"";
            }
            
            public bool isValidHeader()
            {
                return FormatLabel.Length < 5 &&
                        VersionNumber < 1000 && VersionNumber > -1 &&
                        dataCategories.Contains(DataCategory) &&
                        formatNames.Contains(FormatName) &&
                        formatVersions.Contains(FormatVersion) &&
                        (Origin.Length == 2 || Origin.Length == 0) &&
                        ExportedBy.Length < 26 &&
                        Consultant.Length < 8 &&
                        Client.Length < 6 &&
                        (LedgerAccountLength == 4 || LedgerAccountLength == 8) &&
                        (Locking == 0 || Locking == 1);
            }
        }

        #endregion

        private void HasVoucherImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var gLTransClient = (sender as Image).Tag as GLTransClient;
            if (gLTransClient != null)
                DebtorTransactions.ShowVoucher(dgAccountsTransGrid.syncEntity, api, busyIndicator);
        }
    }
}
