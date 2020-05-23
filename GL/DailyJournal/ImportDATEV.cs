using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ImportDATEV
    {
        private CrudAPI _api;
        private SQLCache GLAccCache, DebtorCache, CreditorCache, VATCache;
        int DebtorLower, CreditorLower;
        public HashSet<string> faultyAccounts;
        GLDailyJournalClient GLDailyJournal;

        public ImportDATEV(CrudAPI api, GLDailyJournalClient GLDailyJournal)
        {
            InitCaches(api);
            faultyAccounts = new HashSet<string>();
            _api = api;
            this.GLDailyJournal = GLDailyJournal;
        }

        async private void InitCaches(CrudAPI api)
        {
            GLAccCache = api.GetCache(typeof(Uniconta.DataModel.GLAccount));
            DebtorCache = api.GetCache(typeof(Uniconta.DataModel.Debtor));
            CreditorCache = api.GetCache(typeof(Uniconta.DataModel.Creditor));
            VATCache = api.GetCache(typeof(Uniconta.DataModel.GLVat));
            if (GLAccCache == null)
                GLAccCache =  await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            if (DebtorCache == null)
                DebtorCache = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (VATCache == null)
                VATCache = await api.LoadCache(typeof(Uniconta.DataModel.GLVat)).ConfigureAwait(false);
        }

        async public Task<List<GLDailyJournalLineClient>> CreateJournalLines(FileStream fileStream)
        {
            var reader = Uniconta.ClientTools.Util.UtilFunctions.CreateStreamReader(fileStream);

            var sp = new StringSplit(';');
            var line = new List<string>();

            var rawLine = reader.ReadLine();
            sp.Split(rawLine, line);
            if (! await ValidateHeader(line))
                return null;

            var year = GetYearFromHeader(line);
            var AccLen = SetLimits(line);

            var vats = VATCache.GetRecords as GLVat[];

            // After header we have an empty line.
            reader.ReadLine();

            var _journalLines = new List<GLDailyJournalLineClient>(2000);
            for(;;)
            {
                rawLine = await reader.ReadLineAsync().ConfigureAwait(false);
                if (rawLine == null)
                    break;

                sp.Split(rawLine, line);

                var account = testAccount(line[6], AccLen);
                var contraAccount = testAccount(line[7], AccLen);

                string vatcode = null;
                var extcode = line[8];
                if (! string.IsNullOrEmpty(extcode))
                {
                    for (int i = 0; (i < vats.Length); i++)
                    {
                        var v = vats[i];
                        if (string.Compare(v._ExternalCode, extcode, true) == 0)
                        {
                            vatcode = v._Vat;
                            break;
                        }
                    }
                }

                var journalLine = GetJournalLine(line, year, account, contraAccount, vatcode);
                journalLine.SetMaster(GLDailyJournal);
                _journalLines.Add(journalLine);
            }
            reader.Dispose();

            return _journalLines;
        }

        string testAccount(string ac, int AccLen)
        {
            if (ac == null || ac.Length == 0)
                return null;
            if (ac.Length <= AccLen)
            {
                var rec = GLAccCache.Get(ac);
                if (rec != null)
                    return rec.KeyStr;
                if (ac.Length < AccLen)
                {
                    rec = GLAccCache.Get(ac.PadLeft(AccLen, '0'));
                    if (rec != null)
                        return rec.KeyStr;
                }
            }
            else
            {
                var rec = DebtorCache.Get(ac);
                if (rec != null)
                    return rec.KeyStr;
                rec = CreditorCache.Get(ac);
                if (rec != null)
                    return rec.KeyStr;
            }
            faultyAccounts.Add(ac);
            return ac; 
        }

        private GLDailyJournalLineClient GetJournalLine(List<string> line, int year, string account, string contraAccount, string VAT)
        {
            var date = DateTime.ParseExact(line[9], "ddMM", null);
            date = new DateTime(year, date.Month, date.Day);
            int accInt = (int)NumberConvert.ToInt(account);
            int offaccInt = (int)NumberConvert.ToInt(contraAccount);

            var journalLine = new GLDailyJournalLineClient
            {
                _Date = date,
                _Account = account,
                _OffsetAccount = contraAccount,
                _Vat = VAT,
                // 0-9999=Ledger, 10000-69999=Customer, 70000-99999=Vendor
                _AccountType = (accInt < DebtorLower ? (byte)GLJournalAccountType.Finans : (accInt < CreditorLower ? (byte)GLJournalAccountType.Debtor : (byte)GLJournalAccountType.Creditor)),
                _OffsetAccountType = (offaccInt < DebtorLower ? (byte)GLJournalAccountType.Finans : (offaccInt < CreditorLower ? (byte)GLJournalAccountType.Debtor : (byte)GLJournalAccountType.Creditor)),
                _Text = line[13],
                _Dim1 = line[36],
                _Dim2 = line[37],
            };

            var amount = NumberConvert.ToDoubleNoThousandSeperator(line[0]);
            var amountLin = line[1][0];
            if (amountLin == 'S')
                journalLine._Debit = amount;
            else if (amountLin == 'H')
                journalLine._Credit = amount;

            journalLine._OffsetAccount = contraAccount;

            if (journalLine._AccountType == (byte)GLJournalAccountType.Finans && string.IsNullOrEmpty(journalLine._Vat))
            {
                var gla = GLAccCache.Get(journalLine._Account) as GLAccount;
                if (gla != null && gla._DATEVAuto)
                    journalLine._Vat = gla._Vat;
            }

            return journalLine;
        }

        private int SetLimits(List<string> strings)
        {
            var length = int.Parse(strings[13]);
            int DebtorLower = 1;
            int CreditorLower = 7;

            for (var i = length; (--i >= 0); )
            {
                DebtorLower *= 10;
                CreditorLower *= 10;
            }
            this.DebtorLower = DebtorLower;
            this.CreditorLower = CreditorLower;
            return length;
        }

        private int GetYearFromHeader(List<string> header)
        {
            return DateTime.ParseExact(header[15], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None).Year;
        }

        private async Task<bool> ValidateHeader(List<string> header)
        {
            DateTime date, fromDate, toDate;

            var x1 = DateTime.TryParseExact(header[5], "yyyyMMddhhmmssfff", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
            var x2 = DateTime.TryParseExact(header[12], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDate);
            var x3 = DateTime.TryParseExact(header[15], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out toDate);

            var consultant = header[10];
            var client = header[11];
            var dh = new GLTransPage.DatevHeader();
            var err = await _api.Read(dh);

            if (dh.RowId == 0)
            {
                UnicontaMessageBox.Show("Bitte DATEV-Einrichtung vervollständigen.", "", MessageBoxButton.OK);
                return false;
            }
            // If Client or Consultant does not match the Datev Setup, return false.
            if (!consultant.Equals(dh.Consultant) | !client.Equals(dh.Client))
            {
                UnicontaMessageBox.Show("Fehler in DATEV-Einrichtung.", "", MessageBoxButton.OK);
                return false;
            }
            // Invalid File Format
            if (!x1 || !x2 || !x3)
            {
                UnicontaMessageBox.Show("Falsche DATEV Format.", "", MessageBoxButton.OK);
                return false;
            }

            return true;
        }
    }
}
