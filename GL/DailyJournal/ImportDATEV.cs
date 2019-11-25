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
        private List<string[]> _lines;
        private SQLCache GLAccCache, DebtorCache, CreditorCache, VATCache;
        int DebtorLower, CreditorLower;

        public ImportDATEV(CrudAPI api, FileStream fileStream)
        {
            _api = api;
            _lines = new List<string[]>();
            using (StreamReader reader = new StreamReader(fileStream))
            {
                while (!reader.EndOfStream)
                    _lines.Add(reader.ReadLine().Split(';'));
            }
            InitCaches();
        }

        async private void InitCaches()
        {
            GLAccCache = await _api.LoadCache(typeof(GLAccount));
            DebtorCache = await _api.LoadCache(typeof(Debtor));
            CreditorCache = await _api.LoadCache(typeof(Uniconta.DataModel.Creditor));
            VATCache = await _api.LoadCache(typeof(GLVat));
        }

        async public Task<GLDailyJournalLineClient[]> CreateJournalLines(GLDailyJournalClient GLDailyJournal)
        {
            if (_lines == null) return null;

            var header = _lines[0];
            var validHeader = await ValidateHeader(header);
            if (!validHeader) return null;

            if(_lines.Skip(1).Any(l => l.Length !=116))
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("InvalidFile"), "", MessageBoxButton.OK);
                return null;
            }

            var year = GetYearFromHeader(_lines[0]);
            SetLimits(_lines[0]);

            var vats = VATCache.GetRecords as GLVat[];
            var faultyAccounts = new HashSet<string>();

            // two first lines are skipped
            var nlines = _lines.Count - 2;
            var _journalLines = new GLDailyJournalLineClient[nlines];
            for (int n = 0; (n < nlines); n++)
            {
                var line = _lines[n + 2];
                var account = line[6];
                var contraAccount = line[7];

                if (!ValidateAccount(account))
                    faultyAccounts.Add(account);
                if (!ValidateAccount(contraAccount))
                    faultyAccounts.Add(contraAccount);

                string vatcode = null;
                if (! string.IsNullOrEmpty(line[8]))
                {
                    var extcode = line[8];
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
                _journalLines[n] = journalLine;
            }

            if (faultyAccounts.Count != 0)
            {
                var sb = new StringBuilder();
                sb.AppendFormat(Uniconta.ClientTools.Localization.lookup("MissingOBJ"), Uniconta.ClientTools.Localization.lookup("Account")).AppendLine(":");
                foreach (var s in faultyAccounts)
                    sb.AppendLine(s);

                UnicontaMessageBox.Show(sb.ToString(), "", MessageBoxButton.OK);
                return null;
            }

            return _journalLines;
        }

        private GLDailyJournalLineClient GetJournalLine(string[] line, int year, string account, string contraAccount,
            string VAT)
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
                _Text = line[13].Replace("\"", ""),
                _Dim1 = line[36].Replace("\"", ""),
                _Dim2 = line[37].Replace("\"", ""),
            };

            var amount = double.Parse(line[0]);
            var amountLin = line[1].Replace("\"", "");
            if (amountLin.Equals("S"))
                journalLine._Debit = amount;
            else if (amountLin.Equals("H"))
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

        private bool ValidateAccount(string account)
        {
            if (GLAccCache.Get(account) != null)
                return true;
            if (DebtorCache.Get(account) != null)
                return true;
            return (CreditorCache.Get(account) != null);
        }

        private void SetLimits(string[] strings)
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
        }

        private int GetYearFromHeader(string[] header)
        {
            return DateTime.ParseExact(header[15], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None).Year;
        }

        private async Task<bool> ValidateHeader(string[] header)
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
