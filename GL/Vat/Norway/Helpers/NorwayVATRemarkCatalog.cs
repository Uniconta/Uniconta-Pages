using System;
using System.Collections.Generic;
using System.Linq;
using UnicontaClient.Pages.GL.Vat.Norway.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Vat.Norway.Helpers
{
    /// <summary>
    /// Skatteetaten's code list merknadTilsvarendeMvaKode: which structured remarks are valid for which SAF-T code, side,
    /// specification and sign, and which sign Skatteetaten expects. The data is in NorwayVATRemarkCodeList
    /// </summary>
    public static class NorwayVATRemarkCatalog
    {
        //The code list marks rows where free text (merknad/beskrivelse) is allowed with this value
        public const string FreeText = "fritekstfelt";

        private sealed class Row
        {
            public string Code;
            public string Remark;
            public string Categories;
            public string Specification;
            public bool Deduction;
            public int DeliveredSign;
            public int ExpectedSign;
        }

        private static readonly Lazy<List<Row>> _rows = new Lazy<List<Row>>(Parse);

        private static List<Row> Parse()
        {
            var rows = new List<Row>();
            foreach (var text in NorwayVATRemarkCodeList.Data.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var columns = text.Split('|');
                if (columns.Length != 7)
                    continue;

                rows.Add(new Row()
                {
                    Code = columns[0],
                    Remark = columns[1],
                    Categories = columns[2],
                    Specification = columns[3],
                    Deduction = columns[4] == "F",
                    DeliveredSign = columns[5] == "-" ? -1 : 1,
                    ExpectedSign = columns[6] == "-" ? -1 : 1
                });
            }
            return rows;
        }

        /// <summary>
        /// Code list category column for the declaration category: alminnelig and primaernaering share one column
        /// </summary>
        private static char CategoryFlag(string category)
        {
            if (category == Models.Validation.Meldingskategori.omvendtAvgiftsplikt.ToString())
                return 'O';
            if (category == Models.Validation.Meldingskategori.kompensasjon.ToString())
                return 'K';
            return 'A';
        }

        /// <summary>
        /// Rows for the code, category and specification. The deduction flag is only used on codes that report a calculated line
        /// and a deduction line under the same code (81, 83, 86, 88, 91); for other codes all rows apply
        /// </summary>
        private static List<Row> Matching(string stdCode, string specification, bool deduction, string category)
        {
            var flag = CategoryFlag(category);
            var spec = (specification ?? "").Trim();

            var rows = _rows.Value.Where(x => x.Code == stdCode &&
                                              x.Categories.IndexOf(flag) >= 0 &&
                                              x.Specification == spec)
                                  .ToList();

            if (rows.Any(x => x.Deduction))
                rows = rows.Where(x => x.Deduction == deduction).ToList();

            return rows;
        }

        /// <summary>
        /// The sign Skatteetaten expects: +1 or -1. Falls back to positive for outgoing lines and negative for deductions
        /// when the code list has no single answer
        /// </summary>
        public static int ExpectedSign(string stdCode, string specification, bool deduction, string category)
        {
            var expected = Matching(stdCode, specification, deduction, category).Select(x => x.ExpectedSign).Distinct().ToList();
            if (expected.Count == 1)
                return expected[0];

            return deduction ? -1 : 1;
        }

        private static int DeliveredSign(NorwayVATReportLine line, string category)
        {
            var sign = NorwayVATReportMapper.ReportedSign(line);
            return sign != 0 ? sign : ExpectedSign(line.StandardVAT, line.Specification, !line.IsOutgoing, category);
        }

        /// <summary>
        /// Structured remarks that are valid for the line's code, side, specification and sign
        /// </summary>
        public static List<string> ValidRemarks(NorwayVATReportLine line, string category)
        {
            if (String.IsNullOrEmpty(line.StandardVAT))
                return new List<string>();

            var sign = DeliveredSign(line, category);
            return Matching(line.StandardVAT, line.Specification, !line.IsOutgoing, category)
                       .Where(x => x.DeliveredSign == sign && x.Remark != FreeText)
                       .Select(x => x.Remark)
                       .Distinct()
                       .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase)
                       .ToList();
        }

        /// <summary>
        /// Free text is allowed when the code list has a "fritekstfelt" row for the line's code, side, specification and sign
        /// </summary>
        public static bool AllowsFreeText(NorwayVATReportLine line, string category)
        {
            if (String.IsNullOrEmpty(line.StandardVAT))
                return false;

            var sign = DeliveredSign(line, category);
            return Matching(line.StandardVAT, line.Specification, !line.IsOutgoing, category)
                       .Any(x => x.DeliveredSign == sign && x.Remark == FreeText);
        }
    }
}
