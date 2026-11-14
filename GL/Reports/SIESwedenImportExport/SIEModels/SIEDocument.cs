using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.Helpers;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    [Serializable]
    public class SIEDocument
    {
        /// <summary>
        /// This is where all the callbacks to client code happens.
        /// </summary>
        public SIECallbacks Callbacks = new SIECallbacks();

        /// <summary>
        /// Ignore #BTRANS rows (removed transactions) when reading. The standard allows an importing
        /// program to ignore them.
        /// </summary>
        public bool IgnoreBTRANS = false;
        public bool IgnoreMissingOMFATTNING = false;
        /// <summary>
        /// Ignore #RTRANS rows (supplementary transactions) when reading. Each #RTRANS row is always followed
        /// by an identical #TRANS row, so ignoring #RTRANS keeps the voucher complete.
        /// </summary>
        public bool IgnoreRTRANS = false;
        public bool IgnoreMissingDate = true;
        public bool AllowMissingDate { get => IgnoreMissingDate; set => IgnoreMissingDate = value; }
        public bool AllowUnbalancedVoucher { get; set; }

        public bool AllowUnderDimensions { get; set; }

        public string DateFormat = "yyyyMMdd";
        public Encoding Encoding;

        /// <summary>
        /// The encoding the file was read with. A file that starts with a byte order mark (UTF-8, UTF-16) is read in
        /// that encoding instead of Encoding, and an SIEEncodingException is reported, because the standard
        /// prescribes PC8 (5.8). Null when reading stopped before the end. SetImportedFlag supports the UTF-8 mark
        /// only; in a UTF-16 file the flag is not set and it returns false.
        /// </summary>
        public Encoding FileEncoding { get; private set; }

        /// <summary>
        /// If this is set to true in ReadFile no period values, balances or transactions will be saved in memory.
        /// Use this in combination with callbacks to stream through a file.
        /// </summary>
        public bool StreamValues = false;

        /// <summary>
        /// Calculates KSUMMA
        /// </summary>
        internal SIECRC32 CRC;

        /// <summary>
        /// This is the file currently being read.
        /// </summary>
        private string _fileName;

        private Action<Exception> _exceptionHandler;

        public SIEDocument()
        {
            this.Encoding = SIEEncodingHelpers.GetDefault();
        }


        /// <summary>
        /// #DIM
        /// </summary>
        public Dictionary<string, SIEDimension> DIM { get; set; }

        /// <summary>
        /// #UNDERDIM
        /// </summary>
        public Dictionary<string, SIEDimension> UNDERDIM { get; set; }

        /// <summary>
        /// #DIM or #UNDERDIM, should be empty after reading file.
        /// </summary>
        internal Dictionary<string, SIEDimension> TEMPDIM { get; set; }

        /// <summary>
        /// Dimensions (#DIM and #UNDERDIM) in the order they were declared in the file.
        /// </summary>
        public List<SIEDimension> DeclaredDimensions { get; set; }

        /// <summary>
        /// #FLAGGA
        /// </summary>
        public int FLAGGA { get; set; }

        public SIECompany FNAMN { get; set; }

        /// <summary>
        /// #FORMAT
        /// </summary>
        public string FORMAT { get; set; }

        /// <summary>
        /// #GEN
        /// </summary>
        public DateTime? GEN_DATE { get; set; }

        public string GEN_NAMN { get; set; }

        /// <summary>
        /// #IB
        /// </summary>
        public List<SIEPeriodValue> IB { get; set; }

        /// <summary>
        /// #KONTO
        /// </summary>
        public Dictionary<string, SIEAccount> KONTO { get; set; }

        /// <summary>
        /// #KPTYP
        /// </summary>
        public string KPTYP { get; set; }

        public long KSUMMA { get; set; }

        /// <summary>
        /// True when the file announced a control total with an opening #KSUMMA item.
        /// </summary>
        public bool HasKSUMMA { get { return CRC != null && CRC.Started; } }

        /// <summary>
        /// True when the closing #KSUMMA item with the control total was found.
        /// </summary>
        public bool KSUMMAFound { get; private set; }

        /// <summary>
        /// True when the file has a control total and it matches the calculated one.
        /// </summary>
        public bool KSUMMAValid { get; private set; }

        /// <summary>
        /// #OIB
        /// </summary>
        public List<SIEPeriodValue> OIB { get; set; }

        /// <summary>
        /// #OMFATTN Obligatory when exporting period values
        /// </summary>
        public DateTime? OMFATTN { get; set; }

        /// <summary>
        /// #OUB
        /// </summary>
        public List<SIEPeriodValue> OUB { get; set; }

        /// <summary>
        /// #PBUDGET
        /// </summary>
        public List<SIEPeriodValue> PBUDGET { get; set; }

        /// <summary>
        /// #PROGRAM
        /// </summary>
        public List<string> PROGRAM { get; set; }

        /// <summary>
        /// #PROSA
        /// </summary>
        public string PROSA { get; set; }

        /// <summary>
        /// #PSALDO
        /// </summary>
        public List<SIEPeriodValue> PSALDO { get; set; }

        /// <summary>
        /// #RAR
        /// </summary>
        public Dictionary<int, SIEBookingYear> RAR { get; set; }

        /// <summary>
        /// #RES
        /// </summary>
        public List<SIEPeriodValue> RES { get; set; }

        /// <summary>
        /// #SIETYP
        /// </summary>
        public int SIETYP { get; set; }

        /// <summary>
        /// #TAXAR
        /// </summary>
        public int TAXAR { get; set; }

        /// <summary>
        /// If this is set to true in ReadFile each error will be thrown otherwise they will just be callbacked.
        /// </summary>
        public bool ThrowErrors = true;
        /// <summary>
        /// #UB
        /// </summary>
        public List<SIEPeriodValue> UB { get; set; }

        /// <summary>
        /// Will contain all errors found while reading and validating the document.
        /// </summary>
        public List<Exception> ValidationExceptions { get; set; }

        /// <summary>
        /// Labels found in the file that are not part of the standard. They are ignored as the standard prescribes.
        /// </summary>
        public List<string> UnknownItemTypes { get; set; }

        /// <summary>
        /// #VALUTA
        /// </summary>
        public string VALUTA { get; set; }

        /// <summary>
        /// #VER
        /// </summary>
        public List<SIEVoucher> VER { get; set; }


        /// <summary>
        /// Does a fast scan of the file to get the Sie version it adheres to.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns>-1 if no SIE version was found in the file else SIETYPE is returned.</returns>
        public static int GetSIEVersion(string fileName)
        {
            return GetSIEVersion(fileName, SIEEncodingHelpers.GetDefault());
        }
        public static int GetSIEVersion(string fileName, Encoding encoding)
        {
            int ret = -1;
            foreach (var line in File.ReadLines(fileName, encoding))
            {
                if (line.StartsWith("#SIETYP"))
                {
                    var di = new SIEDataItem(line, null);
                    ret = di.GetInt(0);
                    break;
                }
            }

            return ret;
        }

        public void ReadDocument(string fileName)
        {
            _fileName = fileName;

            using (var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                ReadStreamAux(stream);
            }
        }

        public async Task ReadDocumentAsync(string fileName)
        {
            _fileName = fileName;

            using (var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                await ReadStreamAuxAsync(stream);
            }
        }

        public void ReadDocument(Stream stream)
        {
            _fileName = "(stream)";
            ReadStreamAux(stream);
        }

        public async Task ReadDocumentAsync(Stream stream)
        {
            _fileName = "(stream)";
            await ReadStreamAuxAsync(stream);
        }

        private void ReadStreamAux(Stream stream)
        {
            Initialize();

            using (var sr = new StreamReader(stream, this.Encoding, true))
            {
                if (ParseLines(sr)) return;
                CheckFileEncoding(sr);
            }

            ValidateDocument();
        }

        private async Task ReadStreamAuxAsync(Stream stream)
        {
            Initialize();

            using (var sr = new StreamReader(stream, this.Encoding, true))
            {
                if (await ParseLinesAsync(sr)) return;
                CheckFileEncoding(sr);
            }

            ValidateDocument();
        }

        /// <summary>
        /// The reader uses Encoding unless the file starts with a byte order mark; then it switches to the encoding
        /// of the mark and skips it. Such a file is read, so that its text is not garbled, and reported.
        /// </summary>
        private void CheckFileEncoding(StreamReader sr)
        {
            FileEncoding = sr.CurrentEncoding;
            if (FileEncoding.CodePage != this.Encoding.CodePage)
            {
                var encodingName = FileEncoding.WebName.ToUpperInvariant();
                Callbacks.CallbackException(new SIEEncodingException($"{_fileName} is encoded as {encodingName}, not PC8 (codepage 437)", encodingName));
            }
        }

        private void Initialize()
        {
            // Every error is collected in ValidationExceptions and optionally thrown.
            if (_exceptionHandler != null) 
                Callbacks.SIEException -= _exceptionHandler;

            _exceptionHandler = ex =>
            {
                ValidationExceptions.Add(ex);
                if (ThrowErrors) throw ex;
            };

            Callbacks.SIEException += _exceptionHandler;

            #region Initialize lists
            FNAMN = new SIECompany();

            KONTO = new Dictionary<string, SIEAccount>();
            DIM = new Dictionary<string, SIEDimension>();
            UNDERDIM = new Dictionary<string, SIEDimension>();
            TEMPDIM = new Dictionary<string, SIEDimension>();
            DeclaredDimensions = new List<SIEDimension>();

            OIB = new List<SIEPeriodValue>();
            OUB = new List<SIEPeriodValue>();
            PSALDO = new List<SIEPeriodValue>();
            PBUDGET = new List<SIEPeriodValue>();
            PROGRAM = new List<string>();
            RAR = new Dictionary<int, SIEBookingYear>();
            IB = new List<SIEPeriodValue>();
            UB = new List<SIEPeriodValue>();
            RES = new List<SIEPeriodValue>();

            VER = new List<SIEVoucher>();
            ValidationExceptions = new List<Exception>();
            FileEncoding = null;
            UnknownItemTypes = new List<string>();

            KSUMMA = 0;
            KSUMMAFound = false;
            KSUMMAValid = false;

            InitializeDimensions();

            #endregion //Initialize listst

            CRC = new SIECRC32();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sr"></param>
        /// <returns>true if start of file is not valid SIE-format</returns>
        private bool ParseLines(StreamReader sr)
        {
            bool firstLine = true;
            SIEVoucher curVoucher = null;
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (ParseLine(line, ref curVoucher, ref firstLine)) return true;
            }

            return FinishParsing(firstLine, curVoucher);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="sr"></param>
        /// <returns>true if start of file is not valid SIE-format</returns>
        private async Task<bool> ParseLinesAsync(StreamReader sr)
        {
            bool firstLine = true;
            SIEVoucher curVoucher = null;
            string line;
            while ((line = await sr.ReadLineAsync()) != null)
            {
                if (ParseLine(line, ref curVoucher, ref firstLine)) return true;
            }

            return FinishParsing(firstLine, curVoucher);
        }

        private bool FinishParsing(bool firstLine, SIEVoucher curVoucher)
        {
            if (firstLine)
            {
                Callbacks.CallbackException(new SIEInvalidFileException(_fileName + " is empty"));
                return true;
            }

            if (curVoucher != null)
            {
                Callbacks.CallbackException(new SIEStructureException("Voucher " + curVoucher.Series + "." + curVoucher.Number + " is not closed with '}'", "#VER " + curVoucher.Series + " " + curVoucher.Number));
                CloseVoucher(curVoucher);
            }

            return false;
        }

        private bool ParseLine(string line, ref SIEVoucher curVoucher, ref bool firstLine)
        {
            Callbacks.CallbackLine(line);
            var di = new SIEDataItem(line, this);

            if (firstLine)
            {
                if (di.ItemType == "") return false; // Leading empty lines are permitted
                firstLine = false;
                if (di.ItemType != "#FLAGGA")
                {
                    Callbacks.CallbackException(new SIEInvalidFileException(_fileName));
                    return true;
                }
            }

            if (CRC.Started) CRC.AddData(di);

            SIEPeriodValue pv = null;

            switch (di.ItemType)
            {
                case "#ADRESS":
                    FNAMN.Contact = di.GetString(0);
                    FNAMN.Street = di.GetString(1);
                    FNAMN.ZipCity = di.GetString(2);
                    FNAMN.Phone = di.GetString(3);
                    break;

                case "#BKOD":
                    FNAMN.SNI = di.GetString(0);
                    break;

                case "#BTRANS":
                    if (!IgnoreBTRANS) ParseTRANS(di, curVoucher);
                    break;

                case "#DIM":
                    ParseDimension(di);
                    break;

                case "#UNDERDIM":
                    ParseUnderDimension(di);
                    break;

                case "#ENHET":
                    ParseENHET(di);
                    break;

                case "#FLAGGA":
                    FLAGGA = di.GetInt(0);
                    break;

                case "#FNAMN":
                    FNAMN.Name = di.GetString(0);
                    break;

                case "#FNR":
                    FNAMN.Code = di.GetString(0);
                    break;

                case "#FORMAT":
                    FORMAT = di.GetString(0);
                    break;

                case "#FTYP":
                    FNAMN.OrgType = di.GetString(0);
                    break;

                case "#GEN":
                    GEN_DATE = di.GetDate(0);
                    GEN_NAMN = di.GetString(1);
                    break;

                case "#IB":
                    ParseIB(di);
                    break;

                case "#KONTO":
                    ParseKONTO(di);
                    break;
                case "#KSUMMA":
                    if (CRC.Started)
                    {
                        ParseKSUMMA(di);
                    }
                    else
                    {
                        CRC.Start();
                    }

                    break;
                case "#KTYP":
                    ParseKTYP(di);
                    break;

                case "#KPTYP":
                    KPTYP = di.GetString(0);
                    break;

                case "#OBJEKT":
                    ParseOBJEKT(di);
                    break;

                case "#OIB":
                    pv = ParseOIB_OUB(di);
                    Callbacks.CallbackOIB(pv);
                    if (!StreamValues) OIB.Add(pv);
                    break;

                case "#OUB":
                    pv = ParseOIB_OUB(di);
                    Callbacks.CallbackOUB(pv);
                    if (!StreamValues) OUB.Add(pv);
                    break;

                case "#ORGNR":
                    FNAMN.OrgIdentifier = di.GetString(0);
                    FNAMN.AcquisitionNumber = di.GetString(1);
                    FNAMN.ActivityNumber = di.GetString(2);
                    break;

                case "#OMFATTN":
                    OMFATTN = di.GetDate(0);
                    break;

                case "#PBUDGET":
                    pv = ParsePBUDGET_PSALDO(di);
                    if (pv != null)
                    {
                        Callbacks.CallbackPBUDGET(pv);
                        if (!StreamValues) PBUDGET.Add(pv);
                    }

                    break;

                case "#PROGRAM":
                    PROGRAM = di.Data;
                    break;

                case "#PROSA":
                    PROSA = di.GetString(0);
                    break;

                case "#PSALDO":
                    pv = ParsePBUDGET_PSALDO(di);
                    if (pv != null)
                    {
                        Callbacks.CallbackPSALDO(pv);
                        if (!StreamValues) PSALDO.Add(pv);
                    }

                    break;

                case "#RAR":
                    ParseRAR(di);
                    break;

                case "#RTRANS":
                    if (!IgnoreRTRANS) ParseTRANS(di, curVoucher);
                    break;

                case "#SIETYP":
                    SIETYP = di.GetInt(0);
                    break;

                case "#SRU":
                    ParseSRU(di);
                    break;

                case "#TAXAR":
                    TAXAR = di.GetInt(0);
                    break;

                case "#UB":
                    ParseUB(di);
                    break;

                case "#TRANS":
                    ParseTRANS(di, curVoucher);
                    break;
                case "#RES":
                    ParseRES(di);
                    break;

                case "#VALUTA":
                    VALUTA = di.GetString(0);
                    break;

                case "#VER":
                    if (curVoucher != null)
                    {
                        Callbacks.CallbackException(new SIEStructureException("Voucher " + curVoucher.Series + "." + curVoucher.Number + " is not closed with '}'", "#VER " + curVoucher.Series + " " + curVoucher.Number));
                        CloseVoucher(curVoucher);
                    }
                    curVoucher = ParseVER(di);
                    break;

                case "":
                    //Empty line
                    break;
                case "{":
                    break;
                case "}":
                    if (curVoucher != null) CloseVoucher(curVoucher);
                    curVoucher = null;
                    break;
                default:
                    // The standard requires unknown labels to be ignored (7.1).
                    if (!UnknownItemTypes.Contains(di.ItemType)) UnknownItemTypes.Add(di.ItemType);
                    break;
            }

            return false;
        }


        private void ParseRAR(SIEDataItem di)
        {
            var rar = new SIEBookingYear();
            rar.ID = di.GetInt(0);
            rar.Start = di.GetDate(1);
            rar.End = di.GetDate(2);

            RAR[rar.ID] = rar;
        }

        private void CloseVoucher(SIEVoucher v)
        {
            //Check sum of rows
            decimal check = 0;
            foreach (var r in v.Rows)
            {
                if (r.Token == "#RTRANS" && this.IgnoreRTRANS) continue;
                if (r.Token == "#BTRANS" && this.IgnoreBTRANS) continue;

                check += r.Amount;
            }
            if (check != 0 && !this.AllowUnbalancedVoucher)
            {
                Callbacks.CallbackException(new SIEVoucherMissmatchException(v.Series + "." + v.Number + " Sum is not zero.", v.Series + " " + v.Number));
            }

            Callbacks.CallbackVER(v);
            if (!StreamValues) VER.Add(v);

        }

        private void InitializeDimensions()
        {
            DIM.Add("1", new SIEDimension() { Number = "1", Name = "Resultatenhet", IsDefault = true });
            if (AllowUnderDimensions)
                UNDERDIM.Add("2", new SIEDimension() { Number = "2", Name = "Kostnadsbärare", SuperDim = DIM["1"], IsDefault = true });
            else
                DIM.Add("2", new SIEDimension() { Number = "2", Name = "Kostnadsbärare", SuperDim = DIM["1"], IsDefault = true });
            DIM.Add("3", new SIEDimension() { Number = "3", Name = "Reserverat", IsDefault = true });
            DIM.Add("4", new SIEDimension() { Number = "4", Name = "Reserverat", IsDefault = true });
            DIM.Add("5", new SIEDimension() { Number = "5", Name = "Reserverat", IsDefault = true });
            DIM.Add("6", new SIEDimension() { Number = "6", Name = "Projekt", IsDefault = true });
            DIM.Add("7", new SIEDimension() { Number = "7", Name = "Anställd", IsDefault = true });
            DIM.Add("8", new SIEDimension() { Number = "8", Name = "Kund", IsDefault = true });
            DIM.Add("9", new SIEDimension() { Number = "9", Name = "Leverantör", IsDefault = true });
            DIM.Add("10", new SIEDimension() { Number = "10", Name = "Faktura", IsDefault = true });
            DIM.Add("11", new SIEDimension() { Number = "11", Name = "Reserverat", IsDefault = true });
            DIM.Add("12", new SIEDimension() { Number = "12", Name = "Reserverat", IsDefault = true });
            DIM.Add("13", new SIEDimension() { Number = "13", Name = "Reserverat", IsDefault = true });
            DIM.Add("14", new SIEDimension() { Number = "14", Name = "Reserverat", IsDefault = true });
            DIM.Add("15", new SIEDimension() { Number = "15", Name = "Reserverat", IsDefault = true });
            DIM.Add("16", new SIEDimension() { Number = "16", Name = "Reserverat", IsDefault = true });
            DIM.Add("17", new SIEDimension() { Number = "17", Name = "Reserverat", IsDefault = true });
            DIM.Add("18", new SIEDimension() { Number = "18", Name = "Reserverat", IsDefault = true });
            DIM.Add("19", new SIEDimension() { Number = "19", Name = "Reserverat", IsDefault = true });
        }

        private void ParseDimension(SIEDataItem di)
        {
            var d = di.GetString(0);
            var n = di.GetString(1);

            SIEDimension dim;
            if (TEMPDIM.TryGetValue(d, out dim))
            {
                DIM[d] = dim;
                TEMPDIM.Remove(d);
            }
            else if (UNDERDIM.TryGetValue(d, out dim))
            {
                // Reserved or declared as a sub-dimension; the last declaration wins, so that a dimension number
                // has one dimension and its objects stay together.
                UNDERDIM.Remove(d);
                dim.SuperDim = null;
                DIM[d] = dim;
            }
            else if (!DIM.TryGetValue(d, out dim))
            {
                dim = new SIEDimension() { Number = d };
                DIM.Add(d, dim);
            }

            dim.Name = n;
            dim.IsDefault = false;
            if (!DeclaredDimensions.Contains(dim)) DeclaredDimensions.Add(dim);
        }

        private void ParseUnderDimension(SIEDataItem di)
        {
            var d = di.GetString(0);
            var n = di.GetString(1);
            var p = di.GetString(2);

            if (!AllowUnderDimensions)
            {
                // Treat the sub-dimension as an ordinary dimension.
                ParseDimension(di);
                return;
            }

            SIEDimension dim;
            if (TEMPDIM.TryGetValue(d, out dim))
            {
                UNDERDIM[d] = dim;
                TEMPDIM.Remove(d);
            }
            else if (DIM.TryGetValue(d, out dim))
            {
                // Reserved or declared as a single dimension; the last declaration wins (see ParseDimension).
                DIM.Remove(d);
                UNDERDIM[d] = dim;
            }
            else if (!UNDERDIM.TryGetValue(d, out dim))
            {
                dim = new SIEDimension() { Number = d };
                UNDERDIM.Add(d, dim);
            }

            dim.Name = n;
            dim.IsDefault = false;
            var superDim = di.GetDimension(p);
            dim.SuperDim = superDim == dim ? null : superDim;
            if (!DeclaredDimensions.Contains(dim)) DeclaredDimensions.Add(dim);
        }

        private void ParseENHET(SIEDataItem di)
        {
            GetOrCreateAccount(di.GetString(0)).Unit = di.GetString(1);
        }

        private void ParseIB(SIEDataItem di)
        {
            var v = new SIEPeriodValue()
            {
                YearNr = di.GetInt(0),
                Account = GetOrCreateAccount(di.GetString(1)),
                Amount = di.GetDecimal(2),
                Quantity = di.GetDecimalNull(3),
                Token = di.ItemType
            };
            Callbacks.CallbackIB(v);
            if (!StreamValues) IB.Add(v);
        }

        private void ParseKONTO(SIEDataItem di)
        {
            var account = GetOrCreateAccount(di.GetString(0));
            account.Name = di.GetString(1);
            account.IsDeclared = true;
        }

        private void ParseKSUMMA(SIEDataItem di)
        {
            KSUMMA = di.GetLong(0);
            KSUMMAFound = true;
            long checksum = CRC.Checksum();
            KSUMMAValid = KSUMMA == checksum;
            if (!KSUMMAValid)
            {
                Callbacks.CallbackException(new SIEInvalidChecksumException($"Control total in {_fileName} is {KSUMMA}, calculated {checksum}"));
            }
        }

        private void ParseKTYP(SIEDataItem di)
        {
            GetOrCreateAccount(di.GetString(0)).Type = di.GetString(1);
        }

        private void ParseOBJEKT(SIEDataItem di)
        {
            var dimNumber = di.GetString(0);
            var number = di.GetString(1);
            var name = di.GetString(2);

            var dim = di.GetDimension(dimNumber);

            SIEObject obj;
            if (dim.Objects.TryGetValue(number, out obj))
            {
                obj.Name = name;
                obj.IsDeclared = true;
            }
            else
            {
                dim.Objects.Add(number, new SIEObject() { Dimension = dim, Number = number, Name = name });
            }
        }

        private SIEPeriodValue ParseOIB_OUB(SIEDataItem di)
        {
            if (SIETYP < 3)
            {
                Callbacks.CallbackException(new SIEInvalidFeatureException("Neither OIB or OUB is part of SIE < 3", "#OIB, #OUB"));
            }

            var objOffset = di.HasObjectList ? 1 : 0;

            var v = new SIEPeriodValue()
            {
                YearNr = di.GetInt(0),
                Account = GetOrCreateAccount(di.GetString(1)),
                Amount = di.GetDecimal(2 + objOffset),
                Quantity = di.GetDecimalNull(3 + objOffset),
                Objects = di.GetObjects() ?? new List<SIEObject>(),
                Token = di.ItemType
            };

            return v;
        }

        private SIEPeriodValue ParsePBUDGET_PSALDO(SIEDataItem di)
        {
            if (SIETYP == 1)
            {
                Callbacks.CallbackException(new SIEInvalidFeatureException("Neither PSALDO or PBUDGET is part of SIE 1", "#PSALDO, #PBUDGET"));
            }

            var objOffset = di.HasObjectList ? 1 : 0;
            var objects = di.GetObjects();

            if (SIETYP == 2 && objects != null && objects.Count > 0)
            {
                //Applications reading SIE type 2 should ignore PSALDO containing non empty dimension.
                return null;
            }

            var v = new SIEPeriodValue()
            {
                YearNr = di.GetInt(0),
                Period = di.GetInt(1),
                Account = GetOrCreateAccount(di.GetString(2)),
                Amount = di.GetDecimal(3 + objOffset),
                Quantity = di.GetDecimalNull(4 + objOffset),
                Objects = objects ?? new List<SIEObject>(),
                Token = di.ItemType
            };
            return v;
        }

        private void ParseRES(SIEDataItem di)
        {
            var objOffset = di.HasObjectList ? 1 : 0;
            var v = new SIEPeriodValue()
            {
                YearNr = di.GetInt(0),
                Account = GetOrCreateAccount(di.GetString(1)),
                Amount = di.GetDecimal(2 + objOffset),
                Quantity = di.GetDecimalNull(3 + objOffset),
                Token = di.ItemType
            };
            Callbacks.CallbackRES(v);
            if (!StreamValues) RES.Add(v);
        }

        private void ParseSRU(SIEDataItem di)
        {
            GetOrCreateAccount(di.GetString(0)).SRU.Add(di.GetString(1));
        }

        private void ParseTRANS(SIEDataItem di, SIEVoucher v)
        {
            if (v == null)
            {
                Callbacks.CallbackException(new SIEStructureException(di.ItemType + " outside of a #VER item (raw data: '" + di.RawData + "')", di.RawData));
                return;
            }

            var objOffset = di.HasObjectList ? 1 : 0;

            var rowDate = di.GetDate(2 + objOffset);
            var vr = new SIEVoucherRow()
            {
                Account = GetOrCreateAccount(di.GetString(0)),
                Objects = di.GetObjects() ?? new List<SIEObject>(),
                Amount = di.GetDecimal(1 + objOffset),
                RowDate = rowDate.HasValue ? rowDate.Value : v.VoucherDate,
                Text = di.GetString(3 + objOffset),
                Quantity = di.GetDecimalNull(4 + objOffset),
                CreatedBy = di.GetString(5 + objOffset),
                Token = di.ItemType
            };

            v.Rows.Add(vr);
        }

        private void ParseUB(SIEDataItem di)
        {
            var v = new SIEPeriodValue()
            {
                YearNr = di.GetInt(0),
                Account = GetOrCreateAccount(di.GetString(1)),
                Amount = di.GetDecimal(2),
                Quantity = di.GetDecimalNull(3),
                Token = di.ItemType
            };
            Callbacks.CallbackUB(v);
            if (!StreamValues) UB.Add(v);

        }

        private SIEVoucher ParseVER(SIEDataItem di)
        {
            var voucherDate = di.GetDate(2);
            if (!voucherDate.HasValue) Callbacks.CallbackException(new SIEMissingMandatoryDateException("Voucher date is missing (raw data: '" + di.RawData + "')", di.RawData));

            var createdDate = di.GetDate(4);
            var v = new SIEVoucher()
            {
                Series = di.GetString(0),
                Number = di.GetString(1),
                VoucherDate = voucherDate.HasValue ? voucherDate.Value : new DateTime(),
                Text = di.GetString(3),
                CreatedDate = createdDate.HasValue ? createdDate.Value : new DateTime(),
                CreatedBy = di.GetString(5),
                Token = di.ItemType
            };

            return v;
        }

        private void ValidateDocument()
        {

            AddValidationException((
                !IgnoreMissingDate &&
                !GEN_DATE.HasValue),
                new SIEMissingMandatoryDateException("#GEN Date is missing in " + _fileName, "#GEN"));

            //If there are period values #OMFATTN has to tell the value date.
            AddValidationException(
                (!IgnoreMissingOMFATTNING) &&
                ((SIETYP == 2 || SIETYP == 3) &&
                !OMFATTN.HasValue &&
                (RES.Count > 0 || UB.Count > 0 || OUB.Count > 0)),
                new SIEMissingMandatoryDateException("#OMFATTN is missing in " + _fileName, "#OMFATTN"));

            // A file that announces a control total must end with one, otherwise it is truncated (10.6).
            AddValidationException(
                HasKSUMMA && !KSUMMAFound,
                new SIETruncatedFileException("#KSUMMA announced but no closing #KSUMMA found in " + _fileName + ". The file is truncated."));

            // All TEMPDIMs should have been resolved when read is completed, unless they are reserved numbers (8.4).
            var undeclared = TEMPDIM.Keys.Where(k => !IsReservedDimension(k)).ToList();
            AddValidationException(undeclared.Any(),
                new SIEMissingObjectException("#DIM or #UNDERDIM is missing for dimension(s) " + string.Join(", ", undeclared) + " in " + _fileName, "#DIM " + string.Join(", ", undeclared)));

        }

        private static bool IsReservedDimension(string number)
        {
            int n;
            return int.TryParse(number, out n) && n >= 1 && n <= 19;
        }

        public long CalculateCheckSum()
        {
            return CRC.Checksum();
        }

        /// <summary>
        /// All dimensions that are either declared in the file or referenced by an object list, in a stable order:
        /// declared dimensions in declaration order first, then undeclared (reserved) dimensions by number.
        /// </summary>
        public List<SIEDimension> GetUsedDimensions()
        {
            var result = new List<SIEDimension>(DeclaredDimensions);

            var referenced = DIM.Values.Concat(UNDERDIM.Values).Concat(TEMPDIM.Values)
                .Where(d => !result.Contains(d) && (d.IsUsed || d.Objects.Count > 0))
                .OrderBy(d => { int n; return int.TryParse(d.Number, out n) ? n : int.MaxValue; })
                .ThenBy(d => d.Number);

            result.AddRange(referenced);
            return result;
        }

        /// <summary>
        /// The financial year the data was retrieved from (#RAR 0), if present.
        /// </summary>
        public SIEBookingYear CurrentYear
        {
            get
            {
                SIEBookingYear year;
                if (RAR != null && RAR.TryGetValue(0, out year)) return year;
                return null;
            }
        }

        /// <summary>
        /// Sets the import flag (#FLAGGA 1) in an existing SIE file without rewriting anything else, so that
        /// the control total and all other content stay intact.
        /// </summary>
        /// <returns>true if the flag was set, false if the file has no #FLAGGA 0 item.</returns>
        public static bool SetImportedFlag(string fileName)
        {
            var bytes = File.ReadAllBytes(fileName);

            // Find the first line that starts with #FLAGGA (allowing leading white space). A UTF-8 byte order mark
            // at the start of the file is skipped, as the reader does; "#FLAGGA 1" is the same bytes in UTF-8.
            int lineStart = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            while (lineStart < bytes.Length)
            {
                int lineEnd = Array.IndexOf(bytes, (byte)'\n', lineStart);
                if (lineEnd == -1) lineEnd = bytes.Length;

                int p = lineStart;
                while (p < lineEnd && (bytes[p] == ' ' || bytes[p] == '\t' || bytes[p] == '\r')) p++;

                if (StartsWith(bytes, p, "#FLAGGA"))
                {
                    p += "#FLAGGA".Length;
                    while (p < lineEnd && (bytes[p] == ' ' || bytes[p] == '\t')) p++;
                    if (p < lineEnd && bytes[p] == '0')
                    {
                        bytes[p] = (byte)'1';
                        File.WriteAllBytes(fileName, bytes);
                        return true;
                    }
                    return p < lineEnd && bytes[p] == '1';
                }

                if (p < lineEnd) return false; // First non-empty line is not #FLAGGA
                lineStart = lineEnd + 1;
            }

            return false;
        }

        private SIEAccount GetOrCreateAccount(string number)
        {
            SIEAccount account;
            if (!KONTO.TryGetValue(number, out account))
            {
                account = new SIEAccount() { Number = number, IsDeclared = false };
                KONTO.Add(number, account);
            }
            return account;
        }

        private static bool StartsWith(byte[] bytes, int index, string text)
        {
            if (index + text.Length > bytes.Length) return false;
            for (int i = 0; i < text.Length; i++)
                if (bytes[index + i] != (byte)text[i]) return false;
            return true;
        }

        private void AddValidationException(bool isException, Exception ex)
        {
            if (isException)
            {
                Callbacks.CallbackException(ex);
            }
        }
    }
}
