using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Helpers
{
    public static class SIEHelpers
    {
        /// <summary>
        /// Gets the organisation type names as set by Bolagsverket
        /// </summary>
        public static Dictionary<string, string> GetOrganisationTypes()
        {
            var organisationTypeNames = new Dictionary<string, string>();

            organisationTypeNames.Add("AB", "Aktiebolag.");
            organisationTypeNames.Add("E", "Enskild näringsidkare.");
            organisationTypeNames.Add("HB", "Handelsbolag.");
            organisationTypeNames.Add("KB", "Kommanditbolag.");
            organisationTypeNames.Add("EK", "Ekonomisk förening.");
            organisationTypeNames.Add("KHF", "Kooperativ hyresrättsförening.");
            organisationTypeNames.Add("BRF", "Bostadsrättsförening.");
            organisationTypeNames.Add("BF", "Bostadsförening.");
            organisationTypeNames.Add("SF", "Sambruksförening.");
            organisationTypeNames.Add("I", "Ideell förening som bedriver näring.");
            organisationTypeNames.Add("S", "Stiftelse som bedriver näring.");
            organisationTypeNames.Add("FL", "Filial till utländskt bolag.");
            organisationTypeNames.Add("BAB", "Bankaktiebolag.");
            organisationTypeNames.Add("MB", "Medlemsbank.");
            organisationTypeNames.Add("SB", "Sparbank.");
            organisationTypeNames.Add("BFL", "Utländsk banks filial.");
            organisationTypeNames.Add("FAB", "Försäkringsaktiebolag.");
            organisationTypeNames.Add("OFB", "Ömsesidigt försäkringsbolag.");
            organisationTypeNames.Add("SE", "Europabolag.");
            organisationTypeNames.Add("SCE", "Europakooperativ.");
            organisationTypeNames.Add("TSF", "Trossamfund.");
            organisationTypeNames.Add("X", "Annan företagsform.");

            return organisationTypeNames;
        }

        #region Account types

        /// <summary>
        /// Maps a Uniconta account type to the SIE account type (#KTYP): T (asset), S (debt), K (cost) or I (income).
        /// Returns null for header, sum and calculation accounts.
        /// </summary>
        public static string ToSieAccountType(GLAccountTypes type)
        {
            switch (type)
            {
                case GLAccountTypes.Revenue:
                case GLAccountTypes.Income:
                    return "I";

                case GLAccountTypes.PL:
                case GLAccountTypes.Cost:
                case GLAccountTypes.CostOfGoodSold:
                case GLAccountTypes.Expense:
                case GLAccountTypes.Depreciasions:
                    return "K";

                case GLAccountTypes.Liability:
                case GLAccountTypes.Equity:
                case GLAccountTypes.Creditor:
                case GLAccountTypes.ShareCapital:
                case GLAccountTypes.LongTermDebt:
                case GLAccountTypes.ShortTermDebt:
                case GLAccountTypes.Provisions:
                    return "S";

                case GLAccountTypes.BalanceSheet:
                case GLAccountTypes.Asset:
                case GLAccountTypes.FixedAssets:
                case GLAccountTypes.CurrentAsset:
                case GLAccountTypes.Inventory:
                case GLAccountTypes.Debtor:
                case GLAccountTypes.LiquidAsset:
                case GLAccountTypes.Bank:
                case GLAccountTypes.WIP:
                    return "T";

                default:
                    return null;
            }
        }

        /// <summary>
        /// Maps a SIE account type (#KTYP) to a Uniconta account type. When the SIE type is missing the type is
        /// derived from the account class of the BAS chart of accounts (first digit of the account number).
        /// </summary>
        public static GLAccountTypes ToUnicontaAccountType(string sieType, string accountNumber)
        {
            switch ((sieType ?? "").Trim().ToUpperInvariant())
            {
                case "T": return GLAccountTypes.Asset;
                case "S": return GLAccountTypes.Liability;
                case "K": return GLAccountTypes.Expense;
                case "I": return GLAccountTypes.Revenue;
            }

            var first = string.IsNullOrEmpty(accountNumber) ? '\0' : accountNumber[0];
            switch (first)
            {
                case '1': return GLAccountTypes.Asset;
                case '2': return GLAccountTypes.Liability;
                case '3': return GLAccountTypes.Revenue;
                case '4':
                case '5':
                case '6':
                case '7': return GLAccountTypes.Expense;
                case '8': return GLAccountTypes.PL;
                default: return GLAccountTypes.BalanceSheet;
            }
        }

        /// <summary>
        /// Tells whether a posted transaction is an opening entry of its financial year rather than a voucher.
        /// Uniconta marks the opening balances as primo transactions, but the year transfer it posts at the start
        /// of the year (result to equity, roll-up of asset and inventory sub-accounts) is posted without a voucher
        /// number and is not always marked. Those rows form #IB together with the primo rows and must not be
        /// exported as #VER or counted in #PSALDO.
        /// </summary>
        public static bool IsOpeningEntry(bool primoTrans, bool yearTransfer, LedgerPostingType origin, int voucher, DateTime date, ICollection<DateTime> yearStarts)
        {
            if (primoTrans)
                return true;

            if (yearStarts == null || !yearStarts.Contains(date.Date))
                return false;

            return yearTransfer ||
                   voucher == 0 ||
                   origin == LedgerPostingType.Primo ||
                   origin == LedgerPostingType.TransferPrimo ||
                   origin == LedgerPostingType.TransferPL;
        }

        /// <summary>
        /// Tells whether a voucher range starts before the financial year of its to-date (#RAR 0), so that the
        /// vouchers of a type 4 file would belong to more than one financial year. SIE-gruppen's file test cannot
        /// process such files (internal server error), so the export warns about them.
        /// </summary>
        public static bool SpansFinancialYears(DateTime fromDate, DateTime? currentYearStart)
        {
            return currentYearStart.HasValue && fromDate.Date < currentYearStart.Value.Date;
        }

        /// <summary>
        /// True for account types that belong to the balance sheet.
        /// </summary>
        public static bool IsBalanceAccount(GLAccountTypes type)
        {
            return type >= GLAccountTypes.BalanceSheet;
        }

        /// <summary>
        /// True for account types that belong to the profit and loss statement.
        /// </summary>
        public static bool IsProfitLossAccount(GLAccountTypes type)
        {
            return type >= GLAccountTypes.PL && type < GLAccountTypes.BalanceSheet;
        }

        #endregion

        #region Company identifiers

        /// <summary>
        /// Formats a Swedish corporate identification number as NNNNNN-NNNN as required by #ORGNR.
        /// Accepts organisation numbers with or without hyphen or century prefix (16) and Swedish VAT numbers (SE...01).
        /// Other values are returned unchanged.
        /// </summary>
        public static string FormatOrgNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;

            var s = value.Trim();
            var upper = s.ToUpperInvariant();
            if (upper.StartsWith("SE")) s = s.Substring(2);

            var digits = new string(s.Where(char.IsDigit).ToArray());
            if (digits.Length == s.Count(c => !char.IsWhiteSpace(c) && c != '-'))
            {
                if (upper.StartsWith("SE") && digits.Length == 12 && digits.EndsWith("01"))
                    digits = digits.Substring(0, 10);
                if (digits.Length == 12 && (digits.StartsWith("16") || digits.StartsWith("19") || digits.StartsWith("20")))
                    digits = digits.Substring(2);
                if (digits.Length == 10)
                    return digits.Substring(0, 6) + "-" + digits.Substring(6);
            }

            return value.Trim();
        }

        /// <summary>
        /// Digits of an identifier, used to compare identifiers written in different formats.
        /// </summary>
        public static string DigitsOnly(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return new string(value.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// SNI industry code for #BKOD: digits only (e.g. "82.300" -> "82300"), or null when there is none.
        /// </summary>
        public static string FormatSniCode(string industryCode)
        {
            var digits = DigitsOnly(industryCode);
            return digits.Length == 0 ? null : digits;
        }

        #endregion

        /// <summary>
        /// Removes characters that are not allowed in file names.
        /// </summary>
        public static string SafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var c in name)
                sb.Append(invalid.Contains(c) ? '_' : c);
            return sb.ToString().Trim();
        }

        /// <summary>
        /// Series for vouchers posted without a number series. The series is compulsory in export files, and
        /// "A" is the first series of the standard. When the company uses "A" itself, the first letter not in
        /// use is taken, so that series and number stay unique in the file.
        /// </summary>
        public static string GetFallbackSeries(ICollection<string> usedSeries)
        {
            for (var c = 'A'; c <= 'Z'; c++)
            {
                var series = c.ToString();
                if (usedSeries == null || !usedSeries.Contains(series))
                    return series;
            }

            var n = 1;
            while (usedSeries.Contains("A" + n))
                n++;
            return "A" + n;
        }

        /// <summary>
        /// SIE dimension number of a Uniconta dimension in exported files. Dimension 1 is written as SIE dimension 1
        /// (kostnadsställe). SIE reserves 2 for a sub-dimension of 1 and 3-19 for fixed meanings, so Uniconta
        /// dimensions 2-5 are written as the freely usable SIE dimensions 20-23.
        /// </summary>
        public static int ToSieDimensionNumber(int unicontaDimension)
        {
            return unicontaDimension == 1 ? 1 : unicontaDimension + 18;
        }

        /// <summary>
        /// Suggests a Uniconta dimension for each SIE dimension of an imported file: first a dimension with the same
        /// name, then the dimension with the same number. 0 means not imported.
        /// </summary>
        /// <param name="sieDimensions">SIE dimension number and name, in the order shown to the user</param>
        /// <param name="unicontaDimensionNames">Names of Uniconta dimension 1..N</param>
        public static Dictionary<string, int> SuggestDimensionMap(IList<KeyValuePair<string, string>> sieDimensions, IList<string> unicontaDimensionNames)
        {
            var map = sieDimensions.ToDictionary(d => d.Key, d => 0);
            var taken = new HashSet<int>();

            foreach (var dim in sieDimensions)
            {
                for (int i = 0; i < unicontaDimensionNames.Count; i++)
                {
                    if (!taken.Contains(i + 1) && !String.IsNullOrWhiteSpace(dim.Value) &&
                        String.Equals(dim.Value.Trim(), unicontaDimensionNames[i]?.Trim(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        map[dim.Key] = i + 1;
                        taken.Add(i + 1);
                        break;
                    }
                }
            }

            foreach (var dim in sieDimensions)
            {
                int number;
                if (map[dim.Key] == 0 && int.TryParse(dim.Key, out number) &&
                    number >= 1 && number <= unicontaDimensionNames.Count && !taken.Contains(number))
                {
                    map[dim.Key] = number;
                    taken.Add(number);
                }
            }

            return map;
        }

        /// <summary>
        /// Period in the form YYYYMM as used by #PSALDO and #PBUDGET.
        /// </summary>
        public static int ToPeriod(DateTime date)
        {
            return date.Year * 100 + date.Month;
        }
    }
}
