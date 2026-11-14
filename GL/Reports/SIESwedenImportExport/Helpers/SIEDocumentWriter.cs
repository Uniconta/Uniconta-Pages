using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.SIEModels;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Helpers
{
    /// <summary>
    /// Writes a SieDocument as a SIE file according to the SIE file format version 4B.
    /// Which items are written depends on the document's SIETYP:
    ///   1: balances (#IB, #UB, #RES)
    ///   2: + period balances and budgets (#PSALDO, #PBUDGET) without objects
    ///   3: + dimensions, objects and object balances (#DIM, #UNDERDIM, #OBJEKT, #OIB, #OUB)
    ///   4: + vouchers (#VER, #TRANS)
    /// </summary>
    internal class SIEDocumentWriter
    {
        private readonly SIEDocument _sie;
        private Stream _stream;
        private SIECRC32 _crc;

        private readonly WriteOptions _options;

        public class WriteOptions
        {
            public WriteOptions()
            {
                this.Encoding = SIEEncodingHelpers.GetDefault();
            }

            /// <summary>
            /// Write an opening and closing #KSUMMA item (control total).
            /// </summary>
            public bool WriteKSUMMA { get; set; } = false;

            /// <summary>
            /// The standard only permits codepage 437 (PC8).
            /// </summary>
            public Encoding Encoding { get; set; }
            public string DateFormat = "yyyyMMdd";
        }

        public SIEDocumentWriter(SIEDocument sie, WriteOptions options = null)
        {
            _sie = sie;
            _options = options ?? new WriteOptions();
        }

        public void Write(string file)
        {
            using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Write(stream);
            }
        }

        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            _stream = stream;
            _crc = null;
            WriteCore();
            _stream = null;
        }

        private void WriteCore()
        {
            WriteItem("#FLAGGA", _sie.FLAGGA.ToString(CultureInfo.InvariantCulture));

            if (_options.WriteKSUMMA)
            {
                WriteItem("#KSUMMA");
                _crc = new SIECRC32();
                _crc.Start();
            }

            // Identification items
            // Program name is text (quoted), the version is written as is
            WriteItem("#PROGRAM", (_sie.PROGRAM ?? new List<string>()).Select((p, i) => i == 0 ? Quote(p) : Field(p)).ToArray());
            WriteItem("#FORMAT", "PC8");
            WriteItem("#GEN", Date(_sie.GEN_DATE), string.IsNullOrWhiteSpace(_sie.GEN_NAMN) ? null : Quote(_sie.GEN_NAMN));
            WriteItem("#SIETYP", _sie.SIETYP.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(_sie.PROSA)) WriteItem("#PROSA", Quote(_sie.PROSA));

            var company = _sie.FNAMN ?? new SIECompany();
            if (!string.IsNullOrWhiteSpace(company.OrgType)) WriteItem("#FTYP", Field(company.OrgType));
            if (!string.IsNullOrWhiteSpace(company.Code)) WriteItem("#FNR", Quote(company.Code));
            if (!string.IsNullOrWhiteSpace(company.OrgIdentifier))
                WriteItem("#ORGNR", Field(company.OrgIdentifier), Optional(company.AcquisitionNumber), Optional(company.ActivityNumber));
            if (!string.IsNullOrWhiteSpace(company.SNI)) WriteItem("#BKOD", Field(company.SNI));
            if (!(company.Contact == null && company.Street == null && company.ZipCity == null && company.Phone == null))
                WriteItem("#ADRESS", Quote(company.Contact), Quote(company.Street), Quote(company.ZipCity), Quote(company.Phone));
            WriteItem("#FNAMN", Quote(company.Name));

            if (_sie.RAR != null)
            {
                foreach (var r in _sie.RAR.Values.OrderByDescending(r => r.ID))
                    WriteItem("#RAR", r.ID.ToString(CultureInfo.InvariantCulture), Date(r.Start), Date(r.End));
            }

            if (_sie.TAXAR > 0) WriteItem("#TAXAR", _sie.TAXAR.ToString(CultureInfo.InvariantCulture));
            if (_sie.OMFATTN.HasValue) WriteItem("#OMFATTN", Date(_sie.OMFATTN));
            if (!string.IsNullOrWhiteSpace(_sie.KPTYP)) WriteItem("#KPTYP", Field(_sie.KPTYP));
            if (!string.IsNullOrWhiteSpace(_sie.VALUTA)) WriteItem("#VALUTA", Field(_sie.VALUTA));

            // Chart of accounts
            WriteKONTO();
            if (_sie.SIETYP >= 3)
            {
                WriteDIM();
                WriteUNDERDIM();
            }

            // Balance items
            WritePeriodValue("#IB", _sie.IB, false);
            WritePeriodValue("#UB", _sie.UB, false);
            if (_sie.SIETYP >= 3)
            {
                WritePeriodValue("#OIB", _sie.OIB, true);
                WritePeriodValue("#OUB", _sie.OUB, true);
            }
            WritePeriodValue("#RES", _sie.RES, false);
            if (_sie.SIETYP >= 2)
            {
                WritePeriodSaldo("#PSALDO", _sie.PSALDO);
                WritePeriodSaldo("#PBUDGET", _sie.PBUDGET);
            }

            // Verification items
            if (_sie.SIETYP >= 4) WriteVER();

            if (_options.WriteKSUMMA)
            {
                var checksum = _crc.Checksum();
                _crc = null;
                WriteItem("#KSUMMA", checksum.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void WriteKONTO()
        {
            if (_sie.KONTO == null) return;

            var accounts = _sie.KONTO.Values.OrderBy(k => k.Number, StringComparer.Ordinal).ToList();

            foreach (var k in accounts)
            {
                WriteItem("#KONTO", Field(k.Number), Quote(k.Name));
                if (!string.IsNullOrWhiteSpace(k.Type))
                    WriteItem("#KTYP", Field(k.Number), Field(k.Type));
                if (!string.IsNullOrWhiteSpace(k.Unit))
                    WriteItem("#ENHET", Field(k.Number), Quote(k.Unit));
            }

            foreach (var k in accounts)
            {
                foreach (var s in k.SRU.Where(s => !string.IsNullOrWhiteSpace(s)))
                    WriteItem("#SRU", Field(k.Number), Field(s));
            }
        }

        private void WriteDIM()
        {
            if (_sie.DIM == null) return;
            foreach (var d in _sie.DIM.Values)
            {
                // Reserved dimensions that were neither declared nor used are not written.
                if (d.IsDefault && d.Objects.Count == 0) continue;

                WriteItem("#DIM", Field(d.Number), Quote(d.Name));
                WriteOBJEKT(d);
            }
        }

        private void WriteUNDERDIM()
        {
            if (_sie.UNDERDIM == null) return;
            foreach (var d in _sie.UNDERDIM.Values)
            {
                if (d.IsDefault && d.Objects.Count == 0) continue;

                WriteItem("#UNDERDIM", Field(d.Number), Quote(d.Name), Field(d.SuperDim?.Number));
                WriteOBJEKT(d);
            }
        }

        private void WriteOBJEKT(SIEDimension d)
        {
            foreach (var o in d.Objects.Values)
                WriteItem("#OBJEKT", Field(d.Number), Quote(o.Number), Quote(o.Name));
        }

        private void WritePeriodValue(string name, List<SIEPeriodValue> list, bool withObjects)
        {
            if (list == null) return;
            foreach (var v in list)
            {
                if (withObjects)
                    WriteItem(name, Year(v.YearNr), Field(v.Account.Number), ObjectList(v.Objects), Amount(v.Amount), Quantity(v.Quantity));
                else
                    WriteItem(name, Year(v.YearNr), Field(v.Account.Number), Amount(v.Amount), Quantity(v.Quantity));
            }
        }

        private void WritePeriodSaldo(string name, List<SIEPeriodValue> list)
        {
            if (list == null) return;
            foreach (var v in list)
            {
                var hasObjects = v.Objects != null && v.Objects.Count > 0;

                // Type 2 files carry period balances for the account as a whole only.
                if (_sie.SIETYP == 2 && hasObjects) continue;

                WriteItem(name, Year(v.YearNr), v.Period.ToString(CultureInfo.InvariantCulture), Field(v.Account.Number), ObjectList(v.Objects), Amount(v.Amount), Quantity(v.Quantity));
            }
        }

        private void WriteVER()
        {
            if (_sie.VER == null) return;
            foreach (var v in _sie.VER)
            {
                WriteItem("#VER",
                    Quote(v.Series),
                    Quote(v.Number),
                    Date(v.VoucherDate),
                    Optional(v.Text, true),
                    v.CreatedDate == DateTime.MinValue ? null : Date(v.CreatedDate),
                    Optional(v.CreatedBy, true));

                WriteLine("{");

                foreach (var r in v.Rows)
                {
                    var rowDate = r.RowDate == DateTime.MinValue ? v.VoucherDate : r.RowDate;
                    WriteItem(string.IsNullOrEmpty(r.Token) ? "#TRANS" : r.Token,
                        Field(r.Account.Number),
                        ObjectList(r.Objects),
                        Amount(r.Amount),
                        Date(rowDate),
                        Optional(r.Text, true),
                        Quantity(r.Quantity),
                        Optional(r.CreatedBy, true));
                }

                WriteLine("}");
            }
        }

        #region Field formatting

        /// <summary>
        /// Writes an item. Trailing optional fields (null) are omitted, optional fields in the middle are written as "".
        /// </summary>
        private void WriteItem(string label, params string[] fields)
        {
            int last = fields.Length - 1;
            while (last >= 0 && fields[last] == null) last--;

            var sb = new StringBuilder(label);
            for (int i = 0; i <= last; i++)
            {
                sb.Append(' ');
                sb.Append(fields[i] ?? "\"\"");
            }

            WriteLine(sb.ToString());
        }

        private void WriteLine(string line)
        {
            var bytes = _options.Encoding.GetBytes(line + "\r\n");
            _stream.Write(bytes, 0, bytes.Length);

            if (_crc != null)
                _crc.AddData(new SIEDataItem(line, null));
        }

        /// <summary>
        /// Removes control characters and escapes quotation marks. SIE only escapes quotation marks, so a text
        /// ending in a backslash gets a trailing space; otherwise the closing quotation mark would read as escaped.
        /// </summary>
        private static string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            var sb = new StringBuilder(input.Length + 2);
            foreach (var c in input)
            {
                if (c < 32 || c == 127)
                {
                    sb.Append(' ');
                }
                else if (c == '"')
                {
                    sb.Append("\\\"");
                }
                else
                {
                    sb.Append(c);
                }
            }

            var s = sb.ToString().Trim();
            return s.EndsWith("\\") ? s + " " : s;
        }

        /// <summary>
        /// A text field, always in quotation marks.
        /// </summary>
        private static string Quote(string input)
        {
            return "\"" + Sanitize(input) + "\"";
        }

        /// <summary>
        /// A field that is only quoted when needed (contains white space or is empty).
        /// </summary>
        private static string Field(string input)
        {
            var s = Sanitize(input);
            if (s.Length == 0 || s.Any(c => char.IsWhiteSpace(c) || c == '"' || c == '{' || c == '}'))
                return "\"" + s + "\"";
            return s;
        }

        /// <summary>
        /// An optional field: null when empty so that it can be omitted at the end of an item.
        /// </summary>
        private static string Optional(string input, bool quote = false)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            return quote ? Quote(input) : Field(input);
        }

        private static string Year(int yearNr)
        {
            return yearNr.ToString(CultureInfo.InvariantCulture);
        }

        private static string Amount(decimal amount)
        {
            return Math.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string Quantity(decimal? quantity)
        {
            if (!quantity.HasValue) return null;
            return quantity.Value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string ObjectList(List<SIEObject> objects)
        {
            if (objects == null || objects.Count == 0) return "{}";

            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var o in objects)
            {
                if (!first) sb.Append(' ');
                first = false;
                sb.Append(Field(o.Dimension.Number));
                sb.Append(' ');
                sb.Append(Quote(o.Number));
            }
            sb.Append('}');
            return sb.ToString();
        }

        private string Date(DateTime? date)
        {
            if (date.HasValue && date.Value != DateTime.MinValue)
                return date.Value.ToString(_options.DateFormat, CultureInfo.InvariantCulture);

            return "00000000";
        }

        #endregion
    }
}
