using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.Reports.SIESwedenImportExport.SIEModels
{
    public class SIEDataItem
    {
        internal SIEDocument Document { get; set; }

        internal string ItemType { get; set; }

        /// <summary>
        /// Fields of the item. Enclosing quotation marks are removed and escaped quotation marks (\") are unescaped.
        /// An object list is kept as one field including its braces, e.g. {1 "Nord" 6 "0001"}.
        /// </summary>
        internal List<string> Data { get; set; }

        internal string RawData { get; set; }

        internal SIEDataItem(string line, SIEDocument document)
        {
            RawData = line;
            Document = document;
            var l = line.Trim();
            var p = FirstWhiteSpace(l);

            if (p == -1)
            {
                ItemType = l;
                Data = new List<string>();
            }
            else
            {
                ItemType = l.Substring(0, p);
                Data = SplitLine(l.Substring(p + 1, l.Length - (p + 1)));
            }
        }

        private int FirstWhiteSpace(string str)
        {
            int a = str.IndexOf(' ');
            int b = str.IndexOf('\t');

            if (a == -1 && b == -1) return -1;
            if (a == -1) return b;
            if (b == -1) return a;

            return Math.Min(a, b);
        }

        /// <summary>
        /// Splits a line into fields according to the SIE rules: fields are separated by spaces or tabs,
        /// a field may be enclosed in quotation marks and a quotation mark inside a field is escaped with a backslash.
        /// A backslash that is not followed by a quotation mark is kept as is (e.g. in file paths).
        /// </summary>
        private static List<string> SplitLine(string untrimmedData)
        {
            var data = untrimmedData.Trim();
            var ret = new List<string>();

            bool inQuotes = false;
            bool wasQuoted = false;
            bool inObject = false;
            bool hasContent = false;
            var buffer = new StringBuilder();

            for (int i = 0; i < data.Length; i++)
            {
                char c = data[i];

                if (c == '\\' && i + 1 < data.Length && data[i + 1] == '"')
                {
                    // Escaped quotation mark is part of the field
                    buffer.Append('"');
                    hasContent = true;
                    i++;
                    continue;
                }

                if (c == '"' && !inObject)
                {
                    inQuotes = !inQuotes;
                    wasQuoted = true;
                    hasContent = true;
                    continue;
                }

                if (!inQuotes)
                {
                    if (c == '{') inObject = true;
                    if (c == '}') inObject = false;
                }

                if ((c == ' ' || c == '\t') && !inQuotes && !inObject)
                {
                    if (hasContent)
                    {
                        ret.Add(buffer.ToString());
                        buffer.Clear();
                    }
                    hasContent = false;
                    wasQuoted = false;
                    continue;
                }

                buffer.Append(c);
                hasContent = true;
            }

            if (hasContent || wasQuoted)
                ret.Add(buffer.ToString());

            return ret;
        }

        /// <summary>
        /// Returns the text that takes part in the control summation (#KSUMMA) for this item:
        /// the label and all fields without separating white space, enclosing quotation marks and braces.
        /// </summary>
        internal string GetChecksumText()
        {
            var sb = new StringBuilder();
            sb.Append(ItemType);
            foreach (var d in Data)
            {
                if (d.StartsWith("{"))
                {
                    foreach (var o in SplitLine(StripBraces(d)))
                        sb.Append(o);
                }
                else
                {
                    sb.Append(d);
                }
            }
            return sb.ToString();
        }

        private static string StripBraces(string objectList)
        {
            var s = objectList.Trim();
            if (s.StartsWith("{")) s = s.Substring(1);
            if (s.EndsWith("}")) s = s.Substring(0, s.Length - 1);
            return s;
        }

        internal long GetLong(int field)
        {
            if (Data.Count <= field) return 0;

            long i;
            long.TryParse(Data[field], NumberStyles.Integer, CultureInfo.InvariantCulture, out i);
            return i;
        }

        internal int GetInt(int field)
        {
            int? foo = GetIntNull(field);
            return foo.HasValue ? foo.Value : 0;
        }

        internal int? GetIntNull(int field)
        {
            if (Data.Count <= field) return null;

            int i;
            if (!int.TryParse(Data[field], NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
            {
                if (!string.IsNullOrWhiteSpace(Data[field]))
                    Document?.Callbacks.CallbackException(new SIEInvalidFieldException($"'{Data[field]}' is not a valid integer (raw data: '{RawData}')", RawData));
                return null;
            }
            return i;
        }

        internal decimal GetDecimal(int field)
        {
            decimal? foo = GetDecimalNull(field);
            return foo.HasValue ? foo.Value : 0;
        }

        internal decimal? GetDecimalNull(int field)
        {
            if (Data.Count <= field) return null;

            var s = Data[field].Trim();
            if (s.Length == 0) return null;

            // The standard requires a point as decimal separator, but be lenient and accept a comma as well.
            s = s.Replace(',', '.');

            decimal d;
            if (!decimal.TryParse(s, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out d))
            {
                Document?.Callbacks.CallbackException(new SIEInvalidFieldException($"'{Data[field]}' is not a valid amount (raw data: '{RawData}')", RawData));
                return null;
            }
            return d;
        }

        internal string GetString(int field)
        {
            if (Data.Count <= field) return "";

            return Data[field].Trim();
        }

        internal DateTime? GetDate(int field)
        {
            if (Data.Count <= field) return null;

            var fieldDate = Data[field].Trim();

            if (string.IsNullOrEmpty(fieldDate)) return null;

            if (fieldDate == "00000000") return null;

            var dateFormat = Document?.DateFormat ?? "yyyyMMdd";
            DateTime parsedDateTime;

            if (!DateTime.TryParseExact(fieldDate, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDateTime))
            {
                Document?.Callbacks.CallbackException(new SIEDateException($"{fieldDate} is not a valid date (raw data: '{RawData}', date format: {dateFormat})", RawData));
                return null;
            }

            return parsedDateTime;
        }

        /// <summary>
        /// True if the item contains an object list, i.e. a field starting with '{'.
        /// </summary>
        internal bool HasObjectList
        {
            get { return Data.Any(d => d.StartsWith("{")); }
        }

        /// <summary>
        /// Index of the object list field, or -1 if the item has none.
        /// </summary>
        internal int ObjectListIndex
        {
            get { return Data.FindIndex(d => d.StartsWith("{")); }
        }

        /// <summary>
        /// Parses the object list of the item into objects. Returns an empty list when the object list is empty
        /// and null when the item has no object list at all.
        /// </summary>
        internal List<SIEObject> GetObjects()
        {
            var ret = new List<SIEObject>();

            var index = ObjectListIndex;
            if (index == -1) return null;

            var dimData = SplitLine(StripBraces(Data[index]));

            if (dimData.Count % 2 != 0)
            {
                Document.Callbacks.CallbackException(new SIEMissingObjectException($"Object list has an odd number of fields (raw data: '{RawData}')", RawData));
            }

            for (int i = 0; i + 1 < dimData.Count; i += 2)
            {
                var dimNumber = dimData[i];
                var objectNumber = dimData[i + 1];

                var d = GetDimension(dimNumber);
                d.IsUsed = true;

                //Add temporary object if the objects hasn't been loaded yet.
                SIEObject obj;
                if (!d.Objects.TryGetValue(objectNumber, out obj))
                {
                    obj = new SIEObject() { Dimension = d, Number = objectNumber, Name = "", IsDeclared = false };
                    d.Objects.Add(objectNumber, obj);
                }

                ret.Add(obj);
            }

            return ret;
        }

        internal SIEDimension GetDimension(string dimNumber)
        {
            SIEDimension dim;
            if (Document.UNDERDIM.TryGetValue(dimNumber, out dim)) return dim;
            if (Document.DIM.TryGetValue(dimNumber, out dim)) return dim;
            if (Document.TEMPDIM.TryGetValue(dimNumber, out dim)) return dim;

            //Add temporary Dimension if the dimensions hasn't been loaded yet.
            dim = new SIEDimension() { Number = dimNumber, Name = "" };
            Document.TEMPDIM.Add(dimNumber, dim);
            return dim;
        }
    }
}
