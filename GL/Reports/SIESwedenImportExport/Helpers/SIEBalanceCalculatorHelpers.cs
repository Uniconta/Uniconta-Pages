using System;
using System.Collections.Generic;
using System.Linq;
using UnicontaClient.Pages.GL.Reports.SIESwedenImportExport.SIEModels;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.Helpers
{
    /// <summary>
    /// A posted general ledger transaction reduced to what the SIE balances need.
    /// </summary>
    public class SieTransRecord
    {
        public string Account;
        public DateTime Date;
        public decimal Amount;
        public decimal Qty;
        /// <summary>
        /// True for opening balance postings (primo). They form #IB and are not period changes.
        /// </summary>
        public bool IsPrimo;
        /// <summary>
        /// Dimension values, index 0 = Uniconta dimension 1. Null or empty when the transaction has no dimension.
        /// </summary>
        public string[] Dimensions;
    }

    /// <summary>
    /// A budget line reduced to what #PBUDGET needs: one amount for one period.
    /// </summary>
    public class SieBudgetRecord
    {
        public string Account;
        public DateTime Date;
        public decimal Amount;
        public decimal Qty;
        public string[] Dimensions;
    }

    /// <summary>
    /// Calculates the balance items of a SIE file (#IB, #UB, #RES, #PSALDO, #OIB, #OUB, #PBUDGET) from
    /// posted transactions.
    ///
    /// Uniconta keeps balances per financial year: the opening balance of a year is posted as primo
    /// transactions at the start of the year, so for a year
    ///   IB  = sum of primo transactions in the year (balance accounts),
    ///   UB  = IB + sum of other transactions in the year up to the cut-off date (balance accounts),
    ///   RES = sum of other transactions in the year up to the cut-off date (profit and loss accounts).
    /// The primo transactions are posted when the year transfer is run. Until then a year has none: its IB is
    /// the UB of the year before, and the result of the year before is added to the year-end result account,
    /// as the year transfer would do.
    /// </summary>
    internal class SIEBalanceCalculator
    {
        private readonly Dictionary<int, SIEBookingYear> _years;
        private readonly DateTime _cutOff;
        private readonly Dictionary<string, SIEAccount> _accounts;
        private readonly Func<string, bool> _isBalanceAccount;
        private readonly Dictionary<string, SIEDimension> _dimensions;
        private readonly bool _withObjects;
        private readonly string _resultAccount;

        /// <param name="years">#RAR items (0 = current year, -1 = previous year, ...)</param>
        /// <param name="cutOff">#OMFATTN: balances of the current year are calculated up to and including this date</param>
        /// <param name="accounts">#KONTO items; accounts missing here are added</param>
        /// <param name="isBalanceAccount">Tells whether an account belongs to the balance sheet</param>
        /// <param name="dimensions">#DIM items keyed by SIE dimension number (see SIEHelpers.ToSieDimensionNumber)</param>
        /// <param name="withObjects">Calculate object balances (SIE type 3 and 4)</param>
        /// <param name="resultAccount">Year-end result account; receives the result of a year that has not been transferred</param>
        public SIEBalanceCalculator(Dictionary<int, SIEBookingYear> years, DateTime cutOff, Dictionary<string, SIEAccount> accounts,
                                    Func<string, bool> isBalanceAccount, Dictionary<string, SIEDimension> dimensions, bool withObjects,
                                    string resultAccount = null)
        {
            _years = years;
            _cutOff = cutOff.Date;
            _accounts = accounts;
            _isBalanceAccount = isBalanceAccount;
            _dimensions = dimensions ?? new Dictionary<string, SIEDimension>();
            _withObjects = withObjects;
            _resultAccount = String.IsNullOrEmpty(resultAccount) ? null : resultAccount;
        }

        public List<SIEPeriodValue> IB { get; } = new List<SIEPeriodValue>();
        public List<SIEPeriodValue> UB { get; } = new List<SIEPeriodValue>();
        public List<SIEPeriodValue> RES { get; } = new List<SIEPeriodValue>();
        public List<SIEPeriodValue> OIB { get; } = new List<SIEPeriodValue>();
        public List<SIEPeriodValue> OUB { get; } = new List<SIEPeriodValue>();
        public List<SIEPeriodValue> PSALDO { get; } = new List<SIEPeriodValue>();
        public List<SIEPeriodValue> PBUDGET { get; } = new List<SIEPeriodValue>();

        /// <summary>
        /// True when the result of a year without year transfer could not be moved to equity, because there is no
        /// year-end result account. #IB of the following years then does not sum to zero.
        /// </summary>
        public bool ResultNotCarried { get; private set; }

        /// <summary>
        /// Calculates all balance items from the transactions. Transactions outside the years are ignored.
        /// </summary>
        /// <param name="earlierYears">Years before the #RAR years. They are only calculated to carry their balance
        /// forward, when the oldest #RAR year has no primo transactions, and are not written to the file.</param>
        public void Calculate(IEnumerable<SieTransRecord> transactions, IEnumerable<SIEBookingYear> earlierYears = null)
        {
            var allTransactions = transactions as IList<SieTransRecord> ?? transactions.ToList();
            var written = new HashSet<SIEBookingYear>(_years.Values);

            //Oldest year first, so that a year without primo transactions can open with the UB of the year before
            var results = new List<Tuple<int, YearTotals>>();
            YearTotals previous = null;
            foreach (var year in _years.Values.Concat(earlierYears ?? Enumerable.Empty<SIEBookingYear>()).Where(y => y.Start.HasValue && y.End.HasValue).OrderBy(y => y.Start.Value))
            {
                var start = year.Start.Value.Date;
                var end = year.End.Value.Date;
                var cut = year.ID == 0 && _cutOff < end ? _cutOff : end;

                var yearTrans = allTransactions.Where(t => t.Date.Date >= start && t.Date.Date <= end).ToList();

                previous = CalculateYear(yearTrans, cut, previous);
                if (written.Contains(year))
                    results.Add(Tuple.Create(year.ID, previous));
            }

            //Items are written for the current year first
            foreach (var result in results.OrderByDescending(r => r.Item1))
            {
                var yearNr = result.Item1;
                var totals = result.Item2;
                IB.AddRange(ToPeriodValues("#IB", yearNr, totals.IB));
                UB.AddRange(ToPeriodValues("#UB", yearNr, totals.UB));
                RES.AddRange(ToPeriodValues("#RES", yearNr, totals.RES));
                OIB.AddRange(ToPeriodValues("#OIB", yearNr, totals.OIB));
                OUB.AddRange(ToPeriodValues("#OUB", yearNr, totals.OUB));
                PSALDO.AddRange(ToPeriodValues("#PSALDO", yearNr, totals.PSALDO));
            }
        }

        private YearTotals CalculateYear(List<SieTransRecord> yearTrans, DateTime cut, YearTotals previousYear)
        {
            var totals = new YearTotals();
            var ib = totals.IB;
            var ub = totals.UB;
            var res = totals.RES;
            var oib = totals.OIB;
            var oub = totals.OUB;
            var psaldo = totals.PSALDO;

            //The year transfer has not been run for this year yet
            if (previousYear != null && !yearTrans.Any(t => t.IsPrimo))
            {
                foreach (var e in previousYear.UB.Entries)
                {
                    ib.Add(e.Account, null, e.Amount, e.Qty);
                    ub.Add(e.Account, null, e.Amount, e.Qty);
                }
                foreach (var e in previousYear.OUB.Entries)
                {
                    oib.Add(e.Account, e.Object, e.Amount, e.Qty);
                    oub.Add(e.Account, e.Object, e.Amount, e.Qty);
                }

                //The year transfer would move the result of the year before to equity, so that IB balances
                var result = previousYear.RES.Entries.Sum(e => e.Amount);
                if (_resultAccount != null)
                {
                    ib.Add(_resultAccount, null, result, 0);
                    ub.Add(_resultAccount, null, result, 0);
                }
                else if (result != 0)
                    ResultNotCarried = true;
            }

            foreach (var t in yearTrans)
            {
                var isBalance = _isBalanceAccount(t.Account);

                if (t.IsPrimo)
                {
                    if (!isBalance) continue;
                    ib.Add(t.Account, null, t.Amount, t.Qty);
                    ub.Add(t.Account, null, t.Amount, t.Qty);
                    if (_withObjects)
                    {
                        foreach (var obj in SingleObjects(t.Dimensions))
                        {
                            oib.Add(t.Account, obj, t.Amount, t.Qty);
                            oub.Add(t.Account, obj, t.Amount, t.Qty);
                        }
                    }
                    continue;
                }

                if (t.Date.Date > cut) continue;

                if (isBalance)
                {
                    ub.Add(t.Account, null, t.Amount, t.Qty);
                    if (_withObjects)
                        foreach (var obj in SingleObjects(t.Dimensions))
                            oub.Add(t.Account, obj, t.Amount, t.Qty);
                }
                else
                {
                    res.Add(t.Account, null, t.Amount, t.Qty);
                }

                var period = SIEHelpers.ToPeriod(t.Date);
                psaldo.Add(t.Account, null, t.Amount, t.Qty, period);
                if (_withObjects)
                    foreach (var obj in SingleObjects(t.Dimensions))
                        psaldo.Add(t.Account, obj, t.Amount, t.Qty, period);
            }

            return totals;
        }

        /// <summary>
        /// Calculates #PBUDGET from budget amounts. Each record is one amount in one period.
        /// </summary>
        public void CalculateBudget(IEnumerable<SieBudgetRecord> budgetLines)
        {
            foreach (var year in _years.Values.OrderByDescending(y => y.ID))
            {
                if (!year.Start.HasValue || !year.End.HasValue) continue;

                var start = year.Start.Value.Date;
                var end = year.End.Value.Date;

                var totals = new Totals();
                foreach (var b in budgetLines.Where(b => b.Date.Date >= start && b.Date.Date <= end))
                {
                    var period = SIEHelpers.ToPeriod(b.Date);
                    totals.Add(b.Account, null, b.Amount, b.Qty, period);
                    if (_withObjects)
                        foreach (var obj in SingleObjects(b.Dimensions))
                            totals.Add(b.Account, obj, b.Amount, b.Qty, period);
                }

                PBUDGET.AddRange(ToPeriodValues("#PBUDGET", year.ID, totals));
            }
        }

        /// <summary>
        /// The objects of a transaction, one per dimension: (SIE dimension number, object number).
        /// </summary>
        private static IEnumerable<Tuple<string, string>> SingleObjects(string[] dimensions)
        {
            if (dimensions == null) yield break;
            for (int i = 0; i < dimensions.Length; i++)
            {
                if (!string.IsNullOrEmpty(dimensions[i]))
                    yield return Tuple.Create(SIEHelpers.ToSieDimensionNumber(i + 1).ToString(), dimensions[i]);
            }
        }

        private IEnumerable<SIEPeriodValue> ToPeriodValues(string token, int yearNr, Totals totals)
        {
            foreach (var entry in totals.Entries)
            {
                var amount = Math.Round(entry.Amount, 2, MidpointRounding.AwayFromZero);
                var qty = Math.Round(entry.Qty, 2, MidpointRounding.AwayFromZero);
                if (amount == 0 && qty == 0) continue;

                yield return new SIEPeriodValue()
                {
                    Token = token,
                    YearNr = yearNr,
                    Period = entry.Period,
                    Account = GetAccount(entry.Account),
                    Amount = amount,
                    Quantity = qty == 0 ? (decimal?)null : qty,
                    Objects = entry.Object == null ? new List<SIEObject>() : new List<SIEObject>() { GetObject(entry.Object) }
                };
            }
        }

        private SIEAccount GetAccount(string number)
        {
            SIEAccount account;
            if (!_accounts.TryGetValue(number, out account))
            {
                account = new SIEAccount() { Number = number, Name = "" };
                _accounts.Add(number, account);
            }
            return account;
        }

        private SIEObject GetObject(Tuple<string, string> key)
        {
            SIEDimension dim;
            if (!_dimensions.TryGetValue(key.Item1, out dim))
            {
                dim = new SIEDimension() { Number = key.Item1, Name = "Dimension " + key.Item1 };
                _dimensions.Add(key.Item1, dim);
            }

            SIEObject obj;
            if (!dim.Objects.TryGetValue(key.Item2, out obj))
            {
                obj = new SIEObject() { Dimension = dim, Number = key.Item2, Name = key.Item2 };
                dim.Objects.Add(key.Item2, obj);
            }
            return obj;
        }

        private class YearTotals
        {
            public readonly Totals IB = new Totals();
            public readonly Totals UB = new Totals();
            public readonly Totals RES = new Totals();
            public readonly Totals OIB = new Totals();
            public readonly Totals OUB = new Totals();
            public readonly Totals PSALDO = new Totals();
        }

        /// <summary>
        /// Accumulates amounts per account, object and period in insertion order.
        /// </summary>
        private class Totals
        {
            public class Entry
            {
                public string Account;
                public Tuple<string, string> Object;
                public int Period;
                public decimal Amount;
                public decimal Qty;
            }

            private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
            private readonly List<Entry> _ordered = new List<Entry>();

            public IEnumerable<Entry> Entries
            {
                get { return _ordered.OrderBy(e => e.Account, StringComparer.Ordinal).ThenBy(e => e.Object == null ? 0 : 1).ThenBy(e => e.Object?.Item1).ThenBy(e => e.Object?.Item2).ThenBy(e => e.Period); }
            }

            public void Add(string account, Tuple<string, string> obj, decimal amount, decimal qty, int period = 0)
            {
                var key = account + "\u0001" + (obj == null ? "" : obj.Item1 + "\u0001" + obj.Item2) + "\u0001" + period;
                Entry entry;
                if (!_entries.TryGetValue(key, out entry))
                {
                    entry = new Entry() { Account = account, Object = obj, Period = period };
                    _entries.Add(key, entry);
                    _ordered.Add(entry);
                }
                entry.Amount += amount;
                entry.Qty += qty;
            }
        }
    }
}
