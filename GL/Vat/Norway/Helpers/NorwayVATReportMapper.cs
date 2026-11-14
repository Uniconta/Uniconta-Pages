using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;
using UnicontaClient.Pages.GL.Vat.Norway.Models;
using UnicontaClient.Pages.GL.Vat.Norway.Models.Validation;
using SubmissionModels = UnicontaClient.Pages.GL.Vat.Norway.Models.Submission;
using ValidationModels = UnicontaClient.Pages.GL.Vat.Norway.Models.Validation;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway.Helpers
{
    public enum NorwayVATSettlementLineKind { ResetVatAccount, Rounding, Settlement }

    /// <summary>
    /// One line of the VAT settlement journal. Amount is debit positive
    /// </summary>
    public sealed class NorwayVATSettlementLine
    {
        public string Account { get; set; }
        public string OffsetAccount { get; set; }
        public double Amount { get; set; }
        public NorwayVATSettlementLineKind Kind { get; set; }
    }

    /// <summary>
    /// Maps Uniconta VAT report data to the Skatteetaten mva-melding model.
    /// Holds no page or session state, so it can be unit tested
    /// </summary>
    public static class NorwayVATReportMapper
    {
        //SAF-T codes reported with a basis even when no VAT is posted (zero rate, outside the VAT act, import without VAT)
        private static readonly HashSet<string> _basisOnlyStdCodes = new HashSet<string>() { "5", "51", "52", "6", "85" };

        //SAF-T code without a rate in Skatteetaten's code lists (turnover outside the VAT act)
        private const string NoRateStdCode = "6";

        //Effective rate (VAT / basis) may differ this much from the code list rate before the line is flagged (rounding on small amounts)
        private const double EffectiveRateTolerance = 0.3;

        private sealed class LinePart
        {
            public VatReportLine Source;
            public bool Outgoing;
            public double Base;
            public double Vat;
            public IEnumerable<VatReportLine> Sources;
        }

        #region Report lines

        //A VAT code can carry several SAF-T codes in one period when a journal line overrides the VAT operation
        private static string StdCodeOf(VatReportLine line) => line.VatOpr?._StdCode ?? "";

        /// <summary>
        /// Keeps the lines posted on VAT accounts and converts them to one report line per VAT code and side (outgoing or deduction).
        /// A VAT account is an account flagged as a VAT system account, or the VAT code's own (offset) account, as the VAT report decides.
        /// Codes that only carry a basis (zero rate, outside scope) take their basis from their revenue or expense lines
        /// </summary>
        public static List<NorwayVATReportLine> ToReportLines(IEnumerable<VatReportLine> vatReportLines)
        {
            //Real lines only. The VAT report adds header, total and difference lines, which have Order <> 0, helptext or no VAT code.
            //A line without a VAT operation is kept: it has no SAF-T code, so validation reports it instead of its VAT being dropped
            var source = vatReportLines.Where(x => x != null &&
                                                   x.Order == 0 &&
                                                   x.helptext == null &&
                                                   x.Account != null &&
                                                   x.Vat != null)
                                       .ToList();

            var vatLines = source.Where(IsVatAccountLine).ToList();

            //Zero rated and out of scope codes report their basis from the revenue or expense lines, as the VAT report does. Every transaction has
            //such a line, while only some may also touch a VAT account, so for these codes the VAT account lines contribute no basis
            //(unless the code has no revenue or expense line at all)
            var basisOnlySource = source.Where(x => !IsVatAccountLine(x) && _basisOnlyStdCodes.Contains(StdCodeOf(x))).ToList();
            var codesWithBasisLines = new HashSet<string>(basisOnlySource.Select(x => x.VatCode + "|" + StdCodeOf(x)));

            var parts = vatLines.Select(x => new LinePart()
            {
                Source = x,
                Outgoing = IsOutgoing(x),
                Base = codesWithBasisLines.Contains(x.VatCode + "|" + StdCodeOf(x)) ? 0d : VatLineBase(x),
                Vat = x.Accumulated,
                Sources = new[] { x }
            }).ToList();

            foreach (var group in basisOnlySource.GroupBy(x => new { x.VatCode, StdCode = StdCodeOf(x) }))
            {
                //The amount posted on the revenue or expense account (VatReportLine.AmountWithVat), the basis the VAT report shows
                var basis = group.Sum(x => x.AmountWithVat);

                //Sales are credits (negative) and are negated when mapped. Purchases (code 85) are debits, so flip them to get a positive basis too
                if (group.First().Vat._VatType == GLVatSaleBuy.Buy)
                    basis = -basis;

                parts.Add(new LinePart()
                {
                    Source = group.First(),
                    Outgoing = true,
                    Base = basis,
                    Vat = 0d,
                    Sources = group.ToList()
                });
            }

            //One line per VAT code, SAF-T code and side. Amounts are summed before rounding
            var lineNo = 0;
            var result = new List<NorwayVATReportLine>();
            foreach (var group in parts.GroupBy(x => new { x.Source.VatCode, StdCode = StdCodeOf(x.Source), x.Outgoing }))
            {
                var first = group.First().Source;
                var accounts = group.SelectMany(x => x.Sources).Select(x => x.Account).Distinct().ToList();
                var baseAmount = Math.Round(group.Sum(x => x.Base), 2);
                var postedVat = Math.Round(group.Sum(x => x.Vat), 2);

                result.Add(new NorwayVATReportLine()
                {
                    LineNo = (lineNo++).ToString(),
                    Account = string.Join(", ", accounts.Select(x => x.AccountNumber)),
                    AccountName = string.Join(", ", accounts.Select(x => x._Name)),
                    VAT = first.VatCode,
                    StandardVAT = first.VatOpr?._StdCode,
                    VATType = first.VatOperation,
                    Rate = first.Rate,
                    BaseAmount = baseAmount,
                    ReportingBaseAmount = RoundToKroner(baseAmount),
                    PostedVAT = postedVat,
                    ReportingPostedVAT = RoundToKroner(postedVat),
                    IsOutgoing = group.Key.Outgoing,
                    //VAT parts have one source line each; basis-only parts carry no VAT
                    PostedVATByAccount = group.Where(x => x.Vat != 0d)
                                              .GroupBy(x => x.Source.Account.AccountNumber)
                                              .ToDictionary(x => x.Key, x => Math.Round(x.Sum(v => v.Vat), 2)),
                    VatRef = first.Vat,
                    AccountRef = first.Account,
                    VatTypeRef = first.VatOpr
                });
            }

            return result;
        }

        /// <summary>
        /// Whole kroner, rounded away from zero (Skatteetaten accepts no decimals)
        /// </summary>
        public static int RoundToKroner(double amount)
        {
            return (int)Math.Round(amount, 0, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// The basis of a VAT account line in the sign of a sale (credit negative), which the mapping negates.
        /// When posting, the server stores the signed amount of the revenue or expense line as basis on both VAT postings
        /// (RecievePostMethod: AmountBase = Amount on the VAT account and the VAT offset account line). On a purchase code's calculated
        /// output VAT (reverse charge, import) the basis is therefore a debit while the VAT is a credit, so it is flipped to match its VAT
        /// </summary>
        public static double VatLineBase(VatReportLine line)
        {
            return IsOutgoing(line) && line.Vat._VatType == GLVatSaleBuy.Buy ? -line._BaseVAT : line._BaseVAT;
        }

        private static string AccountNumberOf(VatReportLine line)
        {
            return line.AccountNumber ?? line.Account?.AccountNumber;
        }

        /// <summary>
        /// The line is on a VAT account: the VAT report marked it, the account is a VAT system account, or it is one of the VAT code's own accounts
        /// </summary>
        public static bool IsVatAccountLine(VatReportLine line)
        {
            if (line.AccountIsVat == 1 || IsVatSystemAccount(line.Account))
                return true;

            var account = AccountNumberOf(line);
            var vat = line.Vat;
            return account != null &&
                   (account == vat._Account || account == vat._AccountRate2 ||
                    account == vat._OffsetAccount || account == vat._OffsetAccountRate2);
        }

        private static bool IsVatSystemAccount(GLAccount account)
        {
            return account != null &&
                   (account.SystemAccountEnum == SystemAccountTypes.SalesTaxPayable ||
                    account.SystemAccountEnum == SystemAccountTypes.SalesTaxReceiveable ||
                    account.SystemAccountEnum == SystemAccountTypes.ManuallyPayableVAT ||
                    account.SystemAccountEnum == SystemAccountTypes.ManuallyReceivableVAT);
        }

        /// <summary>
        /// Decides the side from the posting: on a sales code the code's own account is output VAT. On a purchase code (import, reverse charge)
        /// the own account is the deduction and the offset account is the calculated output VAT. When the account is not one of the code's
        /// accounts, or is both, the system account type decides
        /// </summary>
        public static bool IsOutgoing(VatReportLine line)
        {
            var account = AccountNumberOf(line);
            var vat = line.Vat;

            var isOwn = account != null && (account == vat._Account || account == vat._AccountRate2);
            var isOffset = account != null && (account == vat._OffsetAccount || account == vat._OffsetAccountRate2);

            if (isOwn != isOffset)
                return (vat._VatType == GLVatSaleBuy.Sales) == isOwn;

            return IsOutgoing(line.Account);
        }

        /// <summary>
        /// Fallback by system account: outgoing VAT is posted on the payable accounts
        /// </summary>
        public static bool IsOutgoing(GLAccount account)
        {
            return account.SystemAccountEnum == SystemAccountTypes.SalesTaxPayable ||
                   account.SystemAccountEnum == SystemAccountTypes.ManuallyPayableVAT;
        }

        #endregion

        #region Line checks

        /// <summary>
        /// Rate per SAF-T code from Skatteetaten's code lists mvaKodeSAFT (rate type) and sats (rate and validity).
        /// Null when the code has no rate (code 6) or is unknown
        /// </summary>
        public static double? ExpectedRate(string stdCode, DateTime periodFrom)
        {
            switch (stdCode)
            {
                case "1": case "3": case "14": case "81": case "82": case "86": case "87": case "91": case "92":
                    return 25d;
                case "11": case "15": case "31": case "83": case "84":
                    return 15d;
                case "13": case "33": case "88": case "89":
                    //Temporary reduced rate 1 April 2020 - 30 September 2021
                    return periodFrom >= new DateTime(2020, 4, 1) && periodFrom <= new DateTime(2021, 9, 30) ? 6d : 12d;
                case "12": case "32":
                    return 11.11d;
                case "5": case "51": case "52": case "85":
                    return 0d;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Compares the VAT code's configured rate with the rate the SAF-T code requires, and checks that a zero rated code has no VAT.
        /// A mismatch means the VAT operation is mapped to the wrong SAF-T code, so it blocks submission. Returns null when the line is fine
        /// </summary>
        public static string CheckRate(NorwayVATReportLine line, DateTime periodFrom)
        {
            var expected = ExpectedRate(line.StandardVAT, periodFrom);
            line.ExpectedRate = expected;
            if (expected == null)
                return null;

            //Rate is only sent on outgoing lines. VatReportLine.CalcRate lowers the rate by _Exempt, which only reduces the deduction,
            //so the outgoing side is compared with the VAT code's full rate
            var configuredRate = line.VatRef != null ? line.VatRef.VatPct1(periodFrom) : line.Rate;
            if (line.IsOutgoing && Math.Abs(configuredRate - expected.Value) > 0.01)
                return string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATRateMismatch"),
                                     line.VAT, FormatSats(configuredRate), line.StandardVAT, FormatSats(expected.Value));

            if (expected.Value == 0d)
            {
                return Math.Abs(line.PostedVAT) >= 0.5
                    ? string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATZeroRateHasVat"), line.VAT, line.StandardVAT)
                    : null;
            }

            return null;
        }

        /// <summary>
        /// Compares the effective rate (VAT / basis) with the rate the SAF-T code requires. A difference can be legitimate (a manual VAT correction,
        /// a partial basis), so it is a warning that the user confirms on Send, not a block. Returns null when the line is fine
        /// </summary>
        public static string CheckEffectiveRate(NorwayVATReportLine line, DateTime periodFrom)
        {
            var expected = ExpectedRate(line.StandardVAT, periodFrom);
            if (expected == null || expected.Value == 0d)
                return null;

            if (!line.IsOutgoing && line.VatRef != null && line.VatRef._Exempt != 0d)
                return null;

            if (Math.Abs(line.BaseAmount) < 1d || Math.Abs(line.PostedVAT) < 0.005)
                return null;

            var effective = Math.Round(Math.Abs(line.PostedVAT / line.BaseAmount) * 100d, 2);
            if (Math.Abs(effective - expected.Value) <= EffectiveRateTolerance)
                return null;

            return string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATEffectiveRateMismatch"),
                                 line.VAT, FormatSats(effective), line.StandardVAT, FormatSats(expected.Value));
        }

        /// <summary>
        /// Sign of the line as it is sent: the basis for outgoing lines (the VAT when there is no basis), the VAT for deductions
        /// </summary>
        public static int ReportedSign(NorwayVATReportLine line)
        {
            var vat = -line.ReportingPostedVAT;
            var basis = -line.ReportingBaseAmount;

            if (line.IsOutgoing && basis != 0)
                return Math.Sign(basis);

            return Math.Sign(vat);
        }

        /// <summary>
        /// The line has the opposite sign of what Skatteetaten expects for the code (e.g. more credit notes than sales).
        /// Skatteetaten then requires a remark on the line
        /// </summary>
        public static bool HasOppositeSign(NorwayVATReportLine line, string category)
        {
            var actual = ReportedSign(line);
            if (actual == 0)
                return false;

            var expected = NorwayVATRemarkCatalog.ExpectedSign(line.StandardVAT, line.Specification, !line.IsOutgoing, category);
            return actual != expected;
        }

        /// <summary>
        /// Sets the rate check, the opposite sign flag and the check text on every line
        /// </summary>
        public static void ApplyChecks(IEnumerable<NorwayVATReportLine> lines, DateTime periodFrom, string category)
        {
            foreach (var line in lines)
            {
                var hasCode = !String.IsNullOrEmpty(line.StandardVAT) && !NorwayVATReportConsts.NotReportedStdCodes.Contains(line.StandardVAT);
                line.RateWarning = hasCode ? CheckRate(line, periodFrom) : null;
                line.EffectiveRateWarning = hasCode && line.RateWarning == null ? CheckEffectiveRate(line, periodFrom) : null;
                line.OppositeSign = hasCode && HasOppositeSign(line, category);

                var texts = new List<string>();
                if (!String.IsNullOrEmpty(line.RateWarning))
                    texts.Add(line.RateWarning);
                if (!String.IsNullOrEmpty(line.EffectiveRateWarning))
                    texts.Add(line.EffectiveRateWarning);
                if (line.OppositeSign)
                    texts.Add(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATOppositeSign"), line.VAT, line.StandardVAT));
                line.Check = texts.Count == 0 ? null : string.Join(" ", texts);
            }
        }

        /// <summary>
        /// Checks the remarks: lines with opposite sign need one, a selected remark must be valid for the code, side and sign,
        /// and free text is only allowed where the code list allows it. Returns readable errors, empty when everything is fine
        /// </summary>
        public static List<string> CheckRemarks(IEnumerable<NorwayVATReportLine> lines, string category)
        {
            var errors = new List<string>();
            foreach (var line in lines.Where(x => !String.IsNullOrEmpty(x.StandardVAT)))
            {
                var hasPredefined = !String.IsNullOrWhiteSpace(line.PredefinedComment);
                var hasFreeText = !String.IsNullOrWhiteSpace(line.Comment);

                if (line.OppositeSign && !hasPredefined && !hasFreeText)
                    errors.Add(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATRemarkRequired"), line.VAT, line.StandardVAT));
                else if (hasPredefined && !NorwayVATRemarkCatalog.ValidRemarks(line, category).Contains(line.PredefinedComment))
                    errors.Add(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATRemarkNotValid"), line.PredefinedComment, line.VAT, line.StandardVAT));
                else if (!hasPredefined && hasFreeText && !NorwayVATRemarkCatalog.AllowsFreeText(line, category))
                    errors.Add(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATFreeTextNotAllowed"), line.VAT, line.StandardVAT));
            }
            return errors;
        }

        #endregion

        #region Specification lines

        /// <summary>
        /// Skatteetaten rate code list values are 25, 15, 12, 11,11 and 0 with a decimal comma. Never use the current culture
        /// </summary>
        public static string FormatSats(double rate)
        {
            return rate.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        /// <summary>
        /// An empty string would serialize as an empty element, which fails the code list checks
        /// </summary>
        public static string NullIfEmpty(string value)
        {
            return String.IsNullOrWhiteSpace(value) ? null : value;
        }

        /// <summary>
        /// Converts report lines to MVA specification lines. Lines without a standard code are reported in lineErrors and skipped
        /// </summary>
        public static List<MvaSpesifikasjonslinje> ToSpecificationLines(IEnumerable<NorwayVATReportLine> vatReportLines, List<string> lineErrors)
        {
            var mvaSpecifications = new List<MvaSpesifikasjonslinje>();

            foreach (var vatReportLine in vatReportLines)
            {
                if (String.IsNullOrEmpty(vatReportLine.StandardVAT))
                {
                    lineErrors.Add(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATMissingStdCode"), vatReportLine.VAT, vatReportLine.Account));
                    continue;
                }

                //Codes without VAT treatment are not part of the return
                if (NorwayVATReportConsts.NotReportedStdCodes.Contains(vatReportLine.StandardVAT))
                    continue;

                //Nothing to report
                if (vatReportLine.ReportingPostedVAT == 0 && vatReportLine.ReportingBaseAmount == 0)
                    continue;

                //Uniconta posts credits as negative. Negating gives positive outgoing VAT and negative deductible VAT,
                //which is the sign convention in Skatteetaten's examples
                var vat = -vatReportLine.ReportingPostedVAT;

                var mvaSpecification = new MvaSpesifikasjonslinje();
                mvaSpecification.mvaKode = vatReportLine.StandardVAT;

                mvaSpecification.spesifikasjon = NullIfEmpty(vatReportLine.Specification);
                mvaSpecification.mvaKodeRegnskapsystem = vatReportLine.VAT;
                mvaSpecification.merverdiavgift = vat;

                //Outgoing VAT carries basis and rate. Deductible VAT carries the amount only (rules R065 and R066).
                //The basis keeps its own sign: an opposite sign is flagged on the line and needs a remark, it is never flipped
                if (vatReportLine.IsOutgoing)
                {
                    mvaSpecification.grunnlag = -vatReportLine.ReportingBaseAmount;
                    mvaSpecification.grunnlagSpecified = true;

                    //Send the code list rate when it is known, so a computed rate (11.1111) or a partly exempt rate is never sent.
                    //Code 6 (turnover outside the VAT act) has no rate in Skatteetaten's code list, so it is reported without one
                    if (vatReportLine.StandardVAT != NoRateStdCode)
                        mvaSpecification.sats = FormatSats(vatReportLine.ExpectedRate ?? vatReportLine.Rate);
                }

                //Add comment, prioritize predefined comments
                if (!String.IsNullOrEmpty(vatReportLine.PredefinedComment))
                    mvaSpecification.merknad = new Merknad() { ItemElementName = ItemChoiceType.utvalgtMerknad, Item = vatReportLine.PredefinedComment };
                else if (!String.IsNullOrEmpty(vatReportLine.Comment))
                    mvaSpecification.merknad = new Merknad() { ItemElementName = ItemChoiceType.beskrivelse, Item = vatReportLine.Comment };

                mvaSpecifications.Add(mvaSpecification);
            }

            return mvaSpecifications;
        }

        /// <summary>
        /// Builds the declaration comment from the selected comment type. Returns null when nothing is filled in
        /// </summary>
        public static Merknad ToDeclarationComment(string commentType, string commentOption, string commentFreeText)
        {
            if (String.IsNullOrEmpty(commentType))
                return null;

            if (commentType == ItemChoiceType.utvalgtMerknad.ToString() &&
                !String.IsNullOrEmpty(commentOption))
            {
                return new Merknad()
                {
                    ItemElementName = ItemChoiceType.utvalgtMerknad,
                    Item = commentOption
                };
            }
            else if (commentType == ItemChoiceType.beskrivelse.ToString() &&
                     !String.IsNullOrEmpty(commentFreeText))
            {
                return new Merknad()
                {
                    ItemElementName = ItemChoiceType.beskrivelse,
                    Item = commentFreeText
                };
            }

            return null;
        }

        /// <summary>
        /// Organisation number as digits only. Company id (_Id) holds the Norwegian org number and is preferred;
        /// VAT number (_VatNumber) is the fallback when the id does not give 9 digits. Same order as the PIT Support plugin.
        /// When neither gives 9 digits, the id's digits (or else the VAT number's) are returned and the caller rejects them
        /// </summary>
        public static string ToOrganisationNumber(string companyId, string vatNumber)
        {
            var fromId = Regex.Replace(companyId ?? "", "[^0-9]", "");
            var fromVat = Regex.Replace(vatNumber ?? "", "[^0-9]", "");
            if (fromId.Length == 9)
                return fromId;
            if (fromVat.Length == 9)
                return fromVat;
            return fromId.Length > 0 ? fromId : fromVat;
        }

        #endregion

        #region Settlement

        /// <summary>
        /// Builds the settlement journal lines (debit positive):
        /// one line per VAT account that resets its posted VAT (with øre), the øre difference to the rounding account,
        /// and the reported total (whole kroner) to the settlement account, so the voucher balances.
        /// With a new voucher per line, each line gets the settlement account as offset account instead of a separate settlement line
        /// </summary>
        /// <param name="reportedTotal">fastsattMerverdiavgift: positive = payable, negative = refund</param>
        public static List<NorwayVATSettlementLine> BuildSettlementLines(IEnumerable<NorwayVATReportLine> lines, decimal reportedTotal,
                                                                         string settlementAccount, string roundingAccount, bool newVoucherPerLine)
        {
            //All lines of reported codes, also those that round to zero kroner: their øre must still leave the VAT account
            var reported = lines.Where(x => !String.IsNullOrEmpty(x.StandardVAT) &&
                                            !NorwayVATReportConsts.NotReportedStdCodes.Contains(x.StandardVAT))
                                .ToList();

            var result = reported.SelectMany(x => x.PostedVATByAccount)
                                 .GroupBy(x => x.Key)
                                 .Select(x => new NorwayVATSettlementLine()
                                 {
                                     Account = x.Key,
                                     Amount = -Math.Round(x.Sum(v => v.Value), 2),
                                     Kind = NorwayVATSettlementLineKind.ResetVatAccount
                                 })
                                 .Where(x => x.Amount != 0d)
                                 .OrderBy(x => x.Account)
                                 .ToList();

            var settlement = -(double)reportedTotal;
            var rounding = Math.Round(-(result.Sum(x => x.Amount) + settlement), 2);

            //Each line can be off by at most half a krone after rounding. A larger difference means the books and the return do not match
            var maxRounding = 0.5d * Math.Max(reported.Count, 1) + 0.01d;
            if (Math.Abs(rounding) > maxRounding)
                throw new InvalidOperationException(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATSettlementDifference"),
                                                                  rounding.ToString("N2", CultureInfo.CurrentCulture)));

            if (Math.Abs(rounding) >= 0.005d)
                result.Add(new NorwayVATSettlementLine() { Account = roundingAccount, Amount = rounding, Kind = NorwayVATSettlementLineKind.Rounding });

            if (newVoucherPerLine)
            {
                foreach (var line in result)
                    line.OffsetAccount = settlementAccount;
            }
            else if (Math.Abs(settlement) >= 0.005d)
                result.Add(new NorwayVATSettlementLine() { Account = settlementAccount, Amount = settlement, Kind = NorwayVATSettlementLineKind.Settlement });

            return result;
        }

        #endregion

        #region Validation result

        /// <summary>
        /// Readable text for a validation result: one entry per deviation with code, type, VAT code, reason, details and rule.
        /// The XML path comes last because it is the least useful to a user
        /// </summary>
        public static string BuildValidationText(Valideringsresultat validationResult)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{Uniconta.ClientTools.Localization.lookup("Status")}: {DescribeDeviationType(validationResult.avvikVedMeldingslevering)}");

            if (validationResult.avvik == null || validationResult.avvik.Length == 0)
                return sb.ToString();

            sb.AppendLine();
            var lineNo = 1;
            foreach (var avvik in validationResult.avvik)
            {
                var infos = avvik.avviksinformasjon ?? new Avviksinformasjon[0];
                if (infos.Length == 0)
                {
                    sb.AppendLine($"{lineNo++}. {avvik.stiTilAvvik}");
                    continue;
                }

                foreach (var info in infos)
                {
                    sb.Append($"{lineNo++}. [{info.avvikKode}] {DescribeDeviationType(info.avvikstype)}");
                    //Skatteetaten sends the text "null" when a deviation is not tied to a VAT code
                    if (!String.IsNullOrWhiteSpace(avvik.mvaKode) && !String.Equals(avvik.mvaKode.Trim(), "null", StringComparison.OrdinalIgnoreCase))
                        sb.Append($" - {Uniconta.ClientTools.Localization.lookup("Vat")} {avvik.mvaKode.Trim()}");
                    sb.AppendLine($": {info.begrunnelse}");

                    if (!String.IsNullOrEmpty(info.detaljer))
                        sb.AppendLine($"     {info.detaljer}");
                    if (!String.IsNullOrWhiteSpace(info.regelDefinisjon))
                        AppendRuleDefinition(sb, info.regelDefinisjon);
                    if (!String.IsNullOrEmpty(avvik.stiTilAvvik))
                        sb.AppendLine($"     ({avvik.stiTilAvvik})");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// The rule definition is multi-line source text that starts with a line break. Blank lines at the start and end are dropped,
        /// the common indentation is removed, the first line follows "Rule:" and the rest keep their relative indentation below it
        /// </summary>
        private static void AppendRuleDefinition(StringBuilder sb, string ruleDefinition)
        {
            const string indent = "     ";
            var lines = ruleDefinition.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
                                      .Select(l => l.TrimEnd())
                                      .SkipWhile(l => l.Length == 0)
                                      .Reverse().SkipWhile(l => l.Length == 0).Reverse()
                                      .ToList();

            var commonIndent = lines.Where(l => l.Length > 0).Min(l => l.Length - l.TrimStart().Length);

            sb.AppendLine($"{indent}{Uniconta.ClientTools.Localization.lookup("Rule")}: {lines[0].Substring(commonIndent).TrimStart()}");
            foreach (var line in lines.Skip(1))
                sb.AppendLine(line.Length == 0 ? String.Empty : indent + "  " + line.Substring(commonIndent));
        }

        /// <summary>
        /// Readable deviation type. Only "ugyldig skattemelding" blocks submission
        /// </summary>
        public static string DescribeDeviationType(Avvikstype type)
        {
            switch (type)
            {
                case Avvikstype.ingenavvik: return Uniconta.ClientTools.Localization.lookup("NorwayVATNoDeviation");
                case Avvikstype.avvikendeskattemelding: return Uniconta.ClientTools.Localization.lookup("NorwayVATDeviatingReturn");
                case Avvikstype.mangelfullskattemelding: return Uniconta.ClientTools.Localization.lookup("NorwayVATIncompleteReturn");
                case Avvikstype.ugyldigskattemelding: return Uniconta.ClientTools.Localization.lookup("NorwayVATInvalidReturn");
                default: return type.ToString();
            }
        }

        #endregion

        #region Period calculation

        private static readonly string[] _monthNames = { "januar", "februar", "mars", "april", "mai", "juni", "juli", "august", "september", "oktober", "november", "desember" };

        private enum PeriodKind { Month, HalfMonth, TwoMonths, Quarter, HalfYear, Year }

        /// <summary>
        /// Calculates the Skatteetaten period from the dates alone. The period must be a half month (1-15 or 16-end of month),
        /// or whole months that form a month, a two-month term, a quarter, a half year or a year
        /// </summary>
        /// <param name="isForValidation">Validation and submission use the same period values from different XSD namespaces</param>
        public static Enum CalculatePeriod(DateTime fromDate, DateTime toDate, bool isForValidation)
        {
            fromDate = fromDate.Date;
            toDate = toDate.Date;

            if (toDate < fromDate || fromDate.Year != toDate.Year)
                throw PeriodNotRecognised(fromDate, toDate);

            var lastDayOfToMonth = DateTime.DaysInMonth(toDate.Year, toDate.Month);

            if (fromDate.Day != 1 || toDate.Day != lastDayOfToMonth)
            {
                if (fromDate.Month == toDate.Month && fromDate.Day == 1 && toDate.Day == 15)
                    return ToPeriodEnum(isForValidation, PeriodKind.HalfMonth, "foerstehalvdel" + _monthNames[fromDate.Month - 1]);
                if (fromDate.Month == toDate.Month && fromDate.Day == 16 && toDate.Day == lastDayOfToMonth)
                    return ToPeriodEnum(isForValidation, PeriodKind.HalfMonth, "andrehalvdel" + _monthNames[fromDate.Month - 1]);

                throw PeriodNotRecognised(fromDate, toDate);
            }

            var from = fromDate.Month;
            var to = toDate.Month;
            var range = _monthNames[from - 1] + _monthNames[to - 1];

            switch (to - from + 1)
            {
                case 1:
                    return ToPeriodEnum(isForValidation, PeriodKind.Month, _monthNames[from - 1]);
                case 2:
                    if (from % 2 == 1)
                        return ToPeriodEnum(isForValidation, PeriodKind.TwoMonths, range);
                    break;
                case 3:
                    if ((from - 1) % 3 == 0)
                        return ToPeriodEnum(isForValidation, PeriodKind.Quarter, range);
                    break;
                case 6:
                    if (from == 1 || from == 7)
                        return ToPeriodEnum(isForValidation, PeriodKind.HalfYear, range);
                    break;
                case 12:
                    return ToPeriodEnum(isForValidation, PeriodKind.Year, "aarlig");
            }

            //Without a period element the XML fails schema validation at Skatteetaten, so fail here with a readable reason
            throw PeriodNotRecognised(fromDate, toDate);
        }

        /// <summary>
        /// Compares the dates with the company's VAT period (Company._VatPeriod: 1 = monthly, 2 = two-monthly, 3 = quarterly,
        /// 4 = four-monthly, 5 = half-year, 6 = yearly). Returns a warning for the user, or null when they agree or nothing is set.
        /// The dates decide the period; this only warns that the wrong range may have been picked
        /// </summary>
        public static string CheckCompanyPeriod(DateTime fromDate, DateTime toDate, int companyVATPeriod)
        {
            int expectedMonths;
            switch (companyVATPeriod)
            {
                case 1: expectedMonths = 1; break;
                case 2: expectedMonths = 2; break;
                case 3: expectedMonths = 3; break;
                case 4: return Uniconta.ClientTools.Localization.lookup("NorwayVATPeriodNotSupported");
                case 5: expectedMonths = 6; break;
                case 6: expectedMonths = 12; break;
                default: return null;
            }

            //Half-month terms have no Uniconta setting
            if (fromDate.Day != 1 || toDate.Day != DateTime.DaysInMonth(toDate.Year, toDate.Month))
                return null;

            var months = (toDate.Year - fromDate.Year) * 12 + toDate.Month - fromDate.Month + 1;
            if (months == expectedMonths)
                return null;

            return string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATPeriodMismatch"),
                                 Uniconta.ClientTools.AppEnums.VatPeriod.ToString(companyVATPeriod),
                                 fromDate.ToShortDateString(), toDate.ToShortDateString());
        }

        private static Enum ToPeriodEnum(bool isForValidation, PeriodKind kind, string name)
        {
            Type type;
            switch (kind)
            {
                case PeriodKind.Month:
                    type = isForValidation ? typeof(ValidationModels.SkattleggingsperiodeMaaned) : typeof(SubmissionModels.SkattleggingsperiodeMaaned);
                    break;
                case PeriodKind.HalfMonth:
                    type = isForValidation ? typeof(ValidationModels.SkattleggingsperiodeHalvMaaned) : typeof(SubmissionModels.SkattleggingsperiodeHalvMaaned);
                    break;
                case PeriodKind.TwoMonths:
                    type = isForValidation ? typeof(ValidationModels.SkattleggingsperiodeToMaaneder) : typeof(SubmissionModels.SkattleggingsperiodeToMaaneder);
                    break;
                case PeriodKind.Quarter:
                    type = isForValidation ? typeof(ValidationModels.SkattleggingsperiodeTreMaaneder) : typeof(SubmissionModels.SkattleggingsperiodeTreMaaneder);
                    break;
                case PeriodKind.HalfYear:
                    type = isForValidation ? typeof(ValidationModels.SkattleggingsperiodeSeksMaaneder) : typeof(SubmissionModels.SkattleggingsperiodeSeksMaaneder);
                    break;
                default:
                    type = isForValidation ? typeof(ValidationModels.SkattleggingsperiodeAar) : typeof(SubmissionModels.SkattleggingsperiodeAar);
                    break;
            }

            return (Enum)Enum.Parse(type, name);
        }

        private static InvalidOperationException PeriodNotRecognised(DateTime fromDate, DateTime toDate)
        {
            return new InvalidOperationException(string.Format(Uniconta.ClientTools.Localization.lookup("NorwayVATPeriodNotRecognised"),
                                                               fromDate.ToShortDateString(), toDate.ToShortDateString()));
        }

        #endregion
    }
}
