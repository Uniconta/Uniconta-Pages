using System;
using System.ComponentModel.DataAnnotations;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls;
using UnicontaClient.Utilities;
using System.Linq;
using System.IO;
using Uniconta.ClientTools.Util;
using Uniconta.WindowsAPI.GL.SAFT;
using Uniconta.ClientTools.Controls;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using System.Xml;
using System.Globalization;
using System.Reflection;


namespace UnicontaClient.Controls.Dialogs
{
    public partial class CwSAFTExport : ChildWindow
    {
        [InputFieldData]
        [Display(Name = "FromDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime FromDate { get; set; }

        [InputFieldData]
        [Display(Name = "ToDate", ResourceType = typeof(InputFieldDataText))]
        public static DateTime ToDate { get; set; }

        readonly CrudAPI api;

        public CwSAFTExport(CrudAPI crudApi)
        {
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("ExportOBJ"), Uniconta.ClientTools.Localization.lookup("AuditStandardSAFT"));

            var accountingYears = crudApi.QuerySync<CompanyFinanceYearClient>();
            var accountYear = accountingYears.Where(y => y.Current == true).FirstOrDefault();

            FromDate = FromDate != DateTime.MinValue ? FromDate : accountYear.FromDate;
            ToDate = ToDate != DateTime.MinValue ? ToDate : accountYear.ToDate;
            this.DataContext = this;
            InitializeComponent();

            fromDate.DateTime = FromDate;
            toDate.DateTime = ToDate;
            api = crudApi;
        }

        private async void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var folderBrowserDialog = UtilDisplay.LoadFolderBrowserDialog;
            var dialogResult = folderBrowserDialog.ShowDialog();
            if (dialogResult != true)
                return;
            if (api.CompanyEntity._CountryId == CountryCode.Germany)
            {
#if MAC
                GermanGDPdUExport.Export(api, FromDate, ToDate, folderBrowserDialog.FolderName);
#else
                GermanGDPdUExport.Export(api, FromDate, ToDate, folderBrowserDialog.SelectedPath);
#endif
                SetDialogResult(true);
            }
            else
            {
#if MAC
                var docInfo = new SAFTDocumentInfo() { Api = api, FromDate = FromDate, ToDate = ToDate, FileName = folderBrowserDialog.FolderName };
#else
                var docInfo = new SAFTDocumentInfo() { Api = api, FromDate = FromDate, ToDate = ToDate, FileName = folderBrowserDialog.SelectedPath };
#endif
                try
                {
                    if (busyIndicator != null)
                        busyIndicator.IsBusy = true;
                    await SAFT.Create(docInfo);
                }
                finally
                {
                    if (busyIndicator != null)
                        busyIndicator.IsBusy = false;
                }
#if MAC
                docInfo.XmlDoc.Save(Path.Combine(folderBrowserDialog.FolderName, docInfo.FileName));
#else
                docInfo.XmlDoc.Save(Path.Combine(folderBrowserDialog.SelectedPath, docInfo.FileName));
#endif
                SetDialogResult(true);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }

    public class GermanGDPdUExport
    {
        private static string cmpName = "";
        private static string cmpAdr = "";
        private static int cmpId;
        private static int Ko;
        private static int Ku;
        private static int Li;
        private static int Stc;
        private static int NoDi;
        private static int An;
        private static int AnB;
        private static int Di;
        private static int KoB;
        private static int KuB;
        private static int LiB;
        private static DateTime St;
        private static DateTime start;
        private static DateTime end;
        private static string ExportPath = "GDPdU_Export";
        private static string DTDName = "gdpdu-01-08-2002.dtd";
        static CrudAPI crudApi;
        public async static void Export(CrudAPI api, DateTime fromDate, DateTime toDate, string exportPath)
        {
            St = DateTime.Now;
            crudApi = api;
            start = fromDate;
            end = toDate;
            ExportPath = exportPath;
            await InitConnection();
            if (!IstOrdnerLeer(ExportPath))
            {
                UnicontaMessageBox.Show("Der Ordner ist nicht leer.", Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            await ExportKonten();
            await ExportKunden();
            await ExportLieferanten();
            await ExportMwSt();
            await ExportDim(NoDi);
            await ExportAnlage();
            await ExportKOBuchungen(start, end);
            await ExportKUBuchungen(start, end);
            await ExportLIBuchungen(start, end);
            await ExportANBuchungen(start, end);
            await GdpduXmlGenerator();
            await Log();
            await DTDGenerator();
            await ZipAndDeleteFiles(ExportPath);
            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("Succes"), Uniconta.ClientTools.Localization.lookup("Information"));

        }

        static async Task InitConnection()
        {
            var cmp = crudApi.CompanyEntity;
            cmpId = cmp.RowId;
            cmpName = cmp.Name;
            cmpAdr = cmp._Address1 + " " + cmp._Address2 + " " + cmp._Address3;
            if (cmp._Dim1 != null)
                NoDi = 1;
            if (cmp._Dim2 != null)
                NoDi = NoDi + 1;
            if (cmp._Dim3 != null)
                NoDi = NoDi + 1;
            if (cmp._Dim4 != null)
                NoDi = NoDi + 1;
            if (cmp._Dim5 != null)
                NoDi = NoDi + 1;
        }

        static async Task ExportKOBuchungen(DateTime start, DateTime end)
        {

            List<PropValuePair> filter = new List<PropValuePair>();

            filter.Add(PropValuePair.GenereteWhereElements("Date", typeof(DateTime), $"{start:dd.MM.yyyy}..{end:dd.MM.yyyy}"));
            var data = await crudApi.Query<GLTransClient>(filter);
            var sb = new StringBuilder();
            sb.AppendLine("Lfdnr;InternLfdnr;Datum;Belegdatum;Beleg;Kontonummer;GegenKontoart;Gegenkonto;Kost1;Kost2;Kost3;Kost4;Kost5;Belegart;Betrag;Steuercode;Steuer;Haben;Soll;Währung;Betragwährung;Habenwährung;Sollwährung;Text;Rechnungsnummer;Ust.-idnr");
            DateTime BelegD;
            var sortData = data.OrderBy(i => i._JournalPostedId);
            int Nr = 0;
            foreach (var d in sortData)
            {
                Nr = Nr + 1;
                if (d.DocumentDate != Convert.ToDateTime("1/1/0001 12:00:00 AM"))
                    BelegD = d.DocumentDate;
                else
                    BelegD = d._Date;


                var WähText = $"{(Currencies)d._Currency:G}";
                if (WähText == "XXX")
                    WähText = "";

                sb.AppendLine($"{Nr};{d._JournalPostedId};{d._Date:dd.MM.yyyy};{BelegD:dd.MM.yyyy};{d._Voucher};{d._Account};{d.DCType};{d.DCAccount};" +
                              $"{d.Dimension1};{d.Dimension2};{d.Dimension3};{d.Dimension4};{d.Dimension5};{d.Origin};{d._Amount:N2};{BereinigeText(d.Vat)};{d.AmountVat:N2};{d.Credit:N2};{d.Debit:N2};{WähText};{d.AmountCur:N2};{d.CreditCur:N2};{d.DebitCur:N2};{BereinigeText(d.Text)};{d._Invoice};{BereinigeText(d.LegalIdent)}");
                KoB = KoB + 1;
            }

            WriteCsv("buchungen.csv", sb.ToString());
        }

        static async Task ExportKUBuchungen(DateTime start, DateTime end)
        {
            List<PropValuePair> filter = new List<PropValuePair>();

            filter.Add(PropValuePair.GenereteWhereElements("Date", typeof(DateTime), $"{start:dd.MM.yyyy}..{end:dd.MM.yyyy}"));
            var data = await crudApi.Query<DebtorTransClient>(filter);
            if (data.Count() > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Lfdnr;InternLfdnr;Datum;Beleg;Kundennummer;Projekt;Belegart;Betrag;Haben;Soll;Währung;Betragwährung;Habenwährung;Sollwährung;Kursregulierung;Text");
                int Nr = 0;
                foreach (var d in data)
                {

                    Nr = Nr + 1;
                    var WähText = $"{(Currencies)d._Currency:G}";
                    if (WähText == "XXX")
                        WähText = "";

                    sb.AppendLine($"{Nr};{d._JournalPostedId};{d._Date:dd.MM.yyyy};{d._Voucher};{d._Account:N2};" +
                                  $"{d._Project};{(DCPostType)d._PostType:G};{d._Amount:N2};{d.Credit ?? 0:N2};{d.Debit ?? 0:N2};{WähText};{d.AmountCur ?? 0:N2};{d.CreditCur ?? 0:N2};{d.DebitCur ?? 0:N2};{d.ExchangeRegulated:N2};{BereinigeText(d.Text)}");

                    KuB = KuB + 1;
                }
                WriteCsv("kundenbuchungen.csv", sb.ToString());
            }
        }
        static async Task ExportLIBuchungen(DateTime start, DateTime end)
        {

            List<PropValuePair> filter = new List<PropValuePair>();
            filter.Add(PropValuePair.GenereteWhereElements("Date", typeof(DateTime), $"{start:dd.MM.yyyy}..{end:dd.MM.yyyy}"));
            var data = await crudApi.Query<CreditorTransClient>(filter);
            if (data.Count() > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Lfdnr;InternLfdnr;Datum;Beleg;Lieferantennummer;Projekt;Belegart;Betrag;Haben;Soll;Währung;Betragwährung;Habenwährung;Sollwährung;Kursregulierung;Text");
                int Nr = 0;
                foreach (var d in data)
                {
                    Nr = Nr + 1;
                    var WähText = $"{(Currencies)d._Currency:G}";
                    if (WähText == "XXX")
                        WähText = "";

                    sb.AppendLine($"{Nr};{d._JournalPostedId};{d._Date:dd.MM.yyyy};{d._Voucher};{d._Account:N2};" +
                                  $"{d._Project};{(DCPostType)d._PostType:G};{d._Amount:N2};{d.Credit ?? 0:N2};{d.Debit ?? 0:N2};{WähText};{d.AmountCur ?? 0:N2};{d.CreditCur ?? 0:N2};{d.DebitCur ?? 0:N2};{d.ExchangeRegulated:N2};{BereinigeText(d.Text)}");

                    LiB = LiB + 1;
                }
                WriteCsv("lieferentenbuchungen.csv", sb.ToString());
            }
        }
        static async Task ExportANBuchungen(DateTime start, DateTime end)
        {
            List<PropValuePair> filter = new List<PropValuePair>();

            filter.Add(PropValuePair.GenereteWhereElements("Date", typeof(DateTime), $"{start:dd.MM.yyyy}..{end:dd.MM.yyyy}"));
            var data = await crudApi.Query<FAMTransClient>(filter);
            if (data.Count() > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Lfdnr;InternLfdnr;Anlagennummer;Datum;Beleg;Kontonummer;Anlagenbuchungsart;Betrag;Währung;Text");
                int Nr = 0;
                foreach (var d in data)
                {
                    Nr = Nr + 1;
                    var WähText = $"{(Currencies)d._Currency:G}";
                    if (WähText == "XXX")
                        WähText = "";

                    sb.AppendLine($"{Nr};{d._JournalPostedId};{d.Asset};{d._Date:dd.MM.yyyy};{d._Voucher};{d._Account:N2};" +
                                  $"{d.AssetPostType};{d._Amount:N2};{WähText};{BereinigeText(d.Text)}");

                    AnB = AnB + 1;
                }
                WriteCsv("anlagebuchungen.csv", sb.ToString());
            }
        }
        static async Task ExportKonten()
        {

            var data = await crudApi.Query<GLAccountClient>();
            var sb = new StringBuilder();
            sb.AppendLine("Kontonummer;Bezeichnung;Kontoart");

            foreach (var d in data)
            {
                sb.AppendLine($"{d._Account};{d._Name};{d.AccountType}");
                Ko = Ko + 1;
            }
            WriteCsv("konten.csv", sb.ToString());

        }

        static async Task ExportKunden()
        {
            var data = await crudApi.Query<DebtorClient>();
            if (data.Count() > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Kundennummer;Name;Adresse1;Adresse2;Adresse3;Adresse4;Landcode;Ust.-idnr");

                foreach (var d in data)
                {
                    sb.AppendLine($"{d._Account};{BereinigeText(d._Name)};{BereinigeText(d._Address1)};{BereinigeText(d._Address2)};{BereinigeText(d._Address3)};{BereinigeText(d._ZipCode + " " + d._City)};{d.Country};{d._LegalIdent}");

                    Ku = Ku + 1;
                }
                WriteCsv("kunden.csv", sb.ToString());
            }
        }

        static async Task ExportLieferanten()
        {
            var data = await crudApi.Query<CreditorClient>();
            if (data.Count() > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Lieferantennummer;Name;Adresse1;Adresse2;Adresse3;Adresse4;Landcode;Ust.-idnr");

                foreach (var d in data)
                {
                    sb.AppendLine($"{d._Account};{BereinigeText(d._Name)};{BereinigeText(d._Address1)};{BereinigeText(d._Address2)};{BereinigeText(d._Address3)};{BereinigeText(d._ZipCode + " " + d._City)};{d.Country};{d._LegalIdent}");
                    Li = Li + 1;
                }
                WriteCsv("lieferanten.csv", sb.ToString());
            }
        }
        static async Task ExportMwSt()
        {
            var data = await crudApi.Query<GLVatClient>();
            var sb = new StringBuilder();
            sb.AppendLine("Steuercode;Name;Type;Pct;Berechnung;Konto;Gegenkonto");

            foreach (var d in data)
            {
                sb.AppendLine($"{d.Vat};{BereinigeText(d._Name)};{BereinigeText(d.VatType)};{d.Rate};{d.Method};{d.Account};{d.OffsetAccount}");

                Stc = Stc + 1;
            }
            WriteCsv("mwst.csv", sb.ToString());

        }
        static async Task ExportDim(int ŃoDi)
        {
            var DimHeader = "Nummer;Name;Gespärt";
            var sb = new StringBuilder();
            if (NoDi > 0)
            {

                var data1 = await crudApi.Query<GLDimType1Client>();

                sb.AppendLine(DimHeader);

                foreach (var d in data1)
                {
                    sb.AppendLine($"{d.KeyStr};{BereinigeText(d._Name)};{d.Blocked}");

                    Di = Di + 1;
                }
                WriteCsv("kost1.csv", sb.ToString());
            }
            if (NoDi > 1)
            {
                var data2 = await crudApi.Query<GLDimType2Client>();
                sb = new StringBuilder();
                sb.AppendLine(DimHeader);

                foreach (var d in data2)
                {
                    sb.AppendLine($"{d.KeyStr};{BereinigeText(d._Name)};{d.Blocked}");

                    Di = Di + 1;
                }
                WriteCsv("kost2.csv", sb.ToString());
            }
            if (NoDi > 2)
            {
                var data3 = await crudApi.Query<GLDimType3Client>();
                sb = new StringBuilder();
                sb.AppendLine(DimHeader);

                foreach (var d in data3)
                {
                    sb.AppendLine($"{d.KeyStr};{BereinigeText(d._Name)};{d.Blocked}");

                    Di = Di + 1;
                }
                WriteCsv("kost3.csv", sb.ToString());
            }
            if (NoDi > 3)
            {
                var data4 = await crudApi.Query<GLDimType4Client>();
                sb = new StringBuilder();
                sb.AppendLine(DimHeader);

                foreach (var d in data4)
                {
                    sb.AppendLine($"{d.KeyStr};{BereinigeText(d._Name)};{d.Blocked}");

                    Di = Di + 1;
                }
                WriteCsv("kost4.csv", sb.ToString());
            }
            if (NoDi > 4)
            {
                var data5 = await crudApi.Query<GLDimType5Client>();
                sb = new StringBuilder();
                sb.AppendLine(DimHeader);

                foreach (var d in data5)
                {
                    sb.AppendLine($"{d.KeyStr};{BereinigeText(d._Name)};{d.Blocked}");

                    Di = Di + 1;
                }
                WriteCsv("kost5.csv", sb.ToString());
            }
        }
        static async Task ExportAnlage()
        {
            var data = await crudApi.Query<FamClient>();
            if (data.Count() > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Anlagennummer;Name;Gruppen;Abschreibungsmethode;Abschreibungpct;Abschreibungszeitraum;Letzteabschreibung");

                foreach (var d in data)
                {
                    sb.AppendLine($"{d.Asset};{BereinigeText(d._Name)};{BereinigeText(d.Group)};{d.DepreciationMethod};{d.DepreciationPercent};{d.DepreciationPeriod};{d.LastDepreciation:dd.MM.yyyy}"); ;

                    An = An + 1;
                }
                WriteCsv("anlage.csv", sb.ToString());
            }
        }
        static async Task Log()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Export für Vorabprüfung aufgerufen am {DateTime.Now} {crudApi.session.LoginId}.");
            sb.AppendLine($"");
            sb.AppendLine($"Periode vom {start} bis {end}");
            sb.AppendLine($"");
            sb.AppendLine($"Mandant {cmpName}");
            sb.AppendLine($"");
            sb.AppendLine($"Tabelle GLAccountClient wurden {Ko} Datensätze exportiert und die Datei konten.csv wurde erstellt.");
            sb.AppendLine($"Tabelle DebtorClient wurden {Ku} Datensätze exportiert und die Datei kunden.csv wurde erstellt.");
            sb.AppendLine($"Tabelle CreditorClient wurden {Li} Datensätze exportiert und die Datei lieferanten.csv wurde erstellt.");
            sb.AppendLine($"Tabelle GLVatClient wurden {Stc} Datensätze exportiert und die Datei mwst.csv wurde erstellt.");
            sb.AppendLine($"Tabelle GLDimType?Client wurden {Di} Datensätze exportiert und die Datei kost1-5.csv wurde erstellt.");
            sb.AppendLine($"Tabelle FamClient wurden {Di} Datensätze exportiert und die Datei anlage.csv wurde erstellt.");
            sb.AppendLine($"Tabelle GLTransClient wurden {KoB} Datensätze exportiert und die Datei buchungen.csv wurde erstellt.");
            sb.AppendLine($"Tabelle DebtorTransClient wurden {KuB} Datensätze exportiert und die Datei kundenbuchungen.csv wurde erstellt.");
            sb.AppendLine($"Tabelle CreditorTransClient wurden {LiB} Datensätze exportiert und die Datei lieferentenbuchungen.csv wurde erstellt.");
            sb.AppendLine($"Tabelle FAMTransClient wurden {AnB} Datensätze exportiert und die Datei alanlagebuchungen.csv wurde erstellt.");
            sb.AppendLine($"");
            sb.AppendLine($"Dauer: {DateTime.Now - St}");
            WriteCsv("log.txt", sb.ToString());
        }


        static void WriteCsv(string fileName, string content)
        {
            var utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

            File.WriteAllText(Path.Combine(ExportPath, fileName), content.ToString(), utf8Encoding);

        }
        static async Task DTDGenerator()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "UnicontaClient.Assets." + DTDName; // Namespace + Dateiname
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string content = reader.ReadToEnd();

                // 2. Inhalt lokal schreiben (in Datei mit dem gleichen Namen)
                File.WriteAllText(ExportPath + "\\" + DTDName, content);
            }
        }
        public static string BereinigeText(string eingabe)
        {
            if (string.IsNullOrEmpty(eingabe))
                return "\"\""; // Leerer String in Anführungszeichen

            string bereinigt = eingabe.Replace(";", "")
                                      .Replace("\r", "")
                                      .Replace("\n", "")
                                      .Replace("\"", ""); // Entfernt vorhandene Anführungszeichen


            return $"\"{FixGermanCharacters(bereinigt)}\""; // Rückgabe mit Anführungszeichen
        }
        static string FixGermanCharacters(string input)
        {
            return input
                .Replace("Ã¤", "ä")
                .Replace("Ã„", "Ä")
                .Replace("Ã¶", "ö")
                .Replace("Ã–", "Ö")
                .Replace("Ã¼", "ü")
                .Replace("Ãœ", "Ü")
                .Replace("ÃŸ", "ß")
                .Replace("â‚¬", "€")
                .Replace("Â", "") // entfernt seltsames Leerzeichen vor Sonderzeichen
                .Replace("â€“", "–") // Gedankenstrich
                .Replace("â€œ", "“").Replace("â€", "”") // Anführungszeichen
                .Replace("â€ž", "„").Replace("â€˜", "‘").Replace("â€™", "’"); // deutsch/englisch Quotes
        }
        static async Task GdpduXmlGenerator()
        {

            string inputFolder = ExportPath;  // <-- Hier anpassen!
            string outputPath = Path.Combine(inputFolder, "Index.xml");

            string[] dateFormats = {
            "dd.MM.yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "MM/dd/yyyy",
            "dd.MM.yy", "yyyyMMdd"
    };

            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false),
                IndentChars = "  ",
                OmitXmlDeclaration = false
            };

            var csvFiles = Directory.GetFiles(inputFolder, "*.csv");

            using (var stream = new FileStream(outputPath, FileMode.Create))
            using (var writer = XmlWriter.Create(stream, settings))
            {
                writer.WriteStartDocument();
                writer.WriteRaw("\n<!DOCTYPE DataSet SYSTEM " + BereinigeText(DTDName) + ">\r\n");
                writer.WriteStartElement("DataSet");

                // Version
                writer.WriteElementString("Version", "1.0");

                // DataSupplier

                writer.WriteStartElement("DataSupplier");
                writer.WriteElementString("Name", cmpName);
                writer.WriteElementString("Location", cmpAdr);
                writer.WriteElementString("Comment", "Exportiert am " + DateTime.Now.ToString("yyyy-MM-dd"));
                writer.WriteEndElement();

                // Media
                writer.WriteStartElement("Media");
                writer.WriteElementString("Name", "ExportDatentraeger");


                foreach (var file in csvFiles)
                {

                    string fileName = Path.GetFileName(file);
                    var lines = File.ReadAllLines(file);
                    if (lines.Length < 2) continue;

                    string[] header = lines[0].Split(';');
                    int colCount = header.Length;


                    // Datenzeilen
                    var dataLines = lines.Skip(1)
                                         .Select(l => l.Split(';'))
                                         .Where(arr => arr.Length == colCount)
                                         .ToList();

                    var columnTypes = new string[colCount];
                    var columnL = new string[colCount];

                    for (int i = 0; i < colCount; i++)
                    {
                        bool allNumeric = true;
                        bool decm = true;
                        bool allDate = true;
                        int alpL = 0;

                        foreach (var row in dataLines)
                        {
                            string value = row[i].Trim();

                            // Prüfen auf Zahl
                            if (value != "" && !double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                                allNumeric = false;

                            // Prüfen auf Datum
                            if (!DateTime.TryParseExact(value, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                                allDate = false;

                            // Prüfen auf Decimal
                            if (!decimal.TryParse(value, out _))
                                decm = false;

                            if (allNumeric == false && allDate == false && decm == false && alpL < value.Length)
                                alpL = value.Length;
                        }

                        if (allNumeric)
                            columnTypes[i] = "Numeric";
                        else if (allDate)
                            columnTypes[i] = "Date";
                        else if (decm)
                            columnTypes[i] = "Decimal";
                        else
                        {
                            columnTypes[i] = "AlphaNumeric";
                            columnL[i] = Convert.ToString(alpL);
                        }
                    }
                    var Name = "";
                    var Desch = "";
                    if (fileName == "konten.csv")
                    {
                        Name = "Sachkontenplan";
                        Desch = "Kontenplan der Sachkonten";
                    }
                    else if (fileName == "kunden.csv")
                    {
                        Name = "Debitoren";
                        Desch = "Debitoren Stammdaten";
                    }
                    else if (fileName == "lieferanten.csv")
                    {
                        Name = "Kreditoren";
                        Desch = "Kreditoren Stammdaten";
                    }
                    else if (fileName == "mwst.csv")
                    {
                        Name = "Steuercodes";
                        Desch = "Steuercodes Stammdaten";
                    }
                    else if (fileName == "dim*.csv")
                    {
                        Name = "Dimension";
                        Desch = "Dimension Stammdaten";
                    }
                    else if (fileName == "anlage.csv")
                    {
                        Name = "Anlage";
                        Desch = "Anlage Stammdaten";
                    }
                    else if (fileName == "buchungen.csv")
                    {
                        Name = "Kontobuchungen";
                        Desch = "Kontobuchungen";
                    }
                    else if (fileName == "kundenbuchungen.csv")
                    {
                        Name = "Debitorenposten";
                        Desch = "Debitorenposten";
                    }
                    else if (fileName == "lieferentenbuchungen.csv")
                    {
                        Name = "Kreditorenposten";
                        Desch = "Kreditorenposten";
                    }
                    else if (fileName == "anlagebuchungen.csv")
                    {
                        Name = "Anlagenposten";
                        Desch = "Anlagenposten";
                    }
                    // Tabelle schreiben
                    writer.WriteStartElement("Table");
                    writer.WriteElementString("URL", fileName);
                    writer.WriteElementString("Name", Name);
                    writer.WriteElementString("Description", Desch);
                    writer.WriteElementString("UTF8", "");
                    writer.WriteElementString("DecimalSymbol", ",");
                    writer.WriteElementString("DigitGroupingSymbol", ".");

                    writer.WriteStartElement("VariableLength");

                    for (int i = 0; i < colCount; i++)
                    {
                        writer.WriteStartElement("VariableColumn");
                        writer.WriteElementString("Name", header[i]);
                        writer.WriteElementString("Description", header[i] + " Beschreibung");

                        switch (columnTypes[i])
                        {
                            case "Numeric":
                                {
                                    writer.WriteElementString("Numeric", string.Empty);
                                    writer.WriteEndElement();
                                    break;
                                }
                            case "Date":
                                {
                                    writer.WriteStartElement("Date", String.Empty);
                                    writer.WriteElementString("Format", "DD.MM.YYYY");
                                    writer.WriteEndElement();
                                    writer.WriteEndElement();
                                    break;
                                }
                            case "Decimal":
                                {
                                    writer.WriteStartElement("Numeric", string.Empty);
                                    writer.WriteElementString("Accuracy", "2");
                                    writer.WriteEndElement();
                                    writer.WriteEndElement();
                                    break;
                                }
                            default:
                                {
                                    writer.WriteElementString("AlphaNumeric", string.Empty);
                                    writer.WriteElementString("MaxLength", columnL[i]);
                                    writer.WriteEndElement();
                                    break;
                                }
                        }

                    }

                    writer.WriteEndElement(); // VariableLength
                    writer.WriteEndElement(); // Table

                    //Header - Zeile aus CSV entfernen
                    File.WriteAllLines(file, lines.Skip(1));
                }

                writer.WriteEndElement(); // Media
                writer.WriteEndElement(); // DataSet
                writer.WriteEndDocument();
            }
        }



        static async Task ZipAndDeleteFiles(string folderPath)
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);

            try
            {
                // Kopiere alle Dateien ins temporäre Verzeichnis
                foreach (string file in Directory.GetFiles(folderPath))
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(tempFolder, fileName);
                    File.Copy(file, destFile);
                }
                foreach (string file in Directory.GetFiles(folderPath))
                {
                    File.Delete(file);
                }

                FastZip zip = new FastZip();
                string filename =  string.Concat("GDPdU", "_", cmpId, "_", DateTime.Now.ToString("ddMMyyHHmmss"), ".zip");
                zip.CreateZip(folderPath + "\\" + filename, tempFolder, false, null);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }
        static bool IstOrdnerLeer(string pfad)
        {
            if (!Directory.Exists(pfad))
            {
                return false;
            }
            return Directory.GetFiles(pfad).Length == 0 &&
                   Directory.GetDirectories(pfad).Length == 0;
        }
    }
}