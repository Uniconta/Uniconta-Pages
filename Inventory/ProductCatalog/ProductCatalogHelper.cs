using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProductCatalogHelper
    {
        static char fileDelimiter;
        public static DataTable FromCsv(string strFilePath)
        {
            try
            {
                fileDelimiter = GetDelimiter(strFilePath);
                DataSet oDS = new DataSet();
                oDS.Tables.Add("Property");
                var oTable = oDS.Tables[0];
                hasHeaderColumnGenerated = false;
                using (var stream = new System.IO.FileStream(strFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    var oSR = UtilFunctions.CreateStreamReader(stream);
                    string line;
                    int cellCount = 0;
                    var stringSplit = new StringSplit(fileDelimiter);
                    var lineValues = new List<string>(10);

                    while ((line = oSR.ReadLine()) != null)
                    {
                        stringSplit.Split(line, lineValues);

                        bool skipRow = false;
                        if (!hasHeaderColumnGenerated)
                            oTable.Columns.AddRange(GenerateColumnHeader(lineValues, out skipRow));

                        if (!skipRow)
                        {
                            int intCounter = 0;
                            var oRows = oTable.NewRow();
                            var colCount = oTable.Columns.Count;
                            foreach (string str in lineValues)
                            {
                                if (intCounter < colCount)
                                {
                                    oRows[intCounter] = str;
                                    intCounter++;
                                }
                            }

                            if (cellCount < intCounter)
                                cellCount = intCounter;
                            oTable.Rows.Add(oRows);
                        }
                    }

                    //Removing Columns not required
                    int colIndex = 0;
                    List<string> coltoBeRemoved = new List<string>(); ;
                    while (colIndex < oTable.Columns.Count)
                    {
                        var col = oTable.Columns[colIndex].ColumnName;
                        if (col != "Action" && col != "Item" &&
                            col != "EAN" && col != "AlternativeItem" &&
                            col != "Name" && col != "SupplierItemId" &&
                            col != "Supplier" && col != "Unit" &&
                            col != "SalesPrice" && col != "SalesPrice" &&
                            col != "ItemGroup" && col != "DiscountGroup" &&
                            col != "WebArg")
                            coltoBeRemoved.Add(oTable.Columns[colIndex].ColumnName);
                        colIndex++;
                    }
                    foreach (var col in coltoBeRemoved)
                        oTable.Columns.Remove(col);
                }
                return oTable;
            }
            catch 
            {
                return null;
            }
        }

        private static char GetDelimiter(string filePath)
        {
            char delimiter = '\0';
            var rowCount = System.IO.File.ReadAllLines(filePath).Length;
            using (var reader = new StreamReader(filePath))
                delimiter = CSVHelper.DetectDelimiter(reader, rowCount);

            return delimiter;
        }

        static bool hasHeaderColumnGenerated;
        private static DataColumn[] GenerateColumnHeader(List<string> firstLineValues, out bool showColumnHeaders)
        {
            DataColumn[] dataColumns;
            showColumnHeaders = ValidateColumnHeaderValues(firstLineValues);
            if (!showColumnHeaders)
            {
                int maxIndex = 35;
                dataColumns = new DataColumn[maxIndex];
                for (int icol = 0; icol < maxIndex; icol++)
                    dataColumns[icol] = new DataColumn(string.Concat("Column ", NumberConvert.ToString(icol + 1)));
            }
            else
            {
                int maxIndex = firstLineValues.Count;
                dataColumns = new DataColumn[maxIndex];
                string[] tempValues = new string[maxIndex];
                for (int icol = 0; icol < maxIndex; icol++)
                {
                    var col = firstLineValues[icol];
                    tempValues[icol] = col;

                    if (tempValues.Where(c => c == col).Count() > 1)
                        col = string.Concat(col, "_", NumberConvert.ToString(icol));
                    if (col.CompareTo("LMnr") == 0)
                        col = "Item";
                    if (col.CompareTo("EANnr") == 0)
                        col = "EAN";
                    if (col.CompareTo("AlternativNr") == 0)
                        col = "AlternativeItem";
                    if (col.CompareTo("Varebetegnelse") == 0)
                        col = "Name";
                    if (col.CompareTo("LeverandørTypenr") == 0)
                        col = "SupplierItemId";
                    if (col.CompareTo("LeverandørNavn") == 0)
                        col = "Supplier";
                    if (col.CompareTo("Enhed") == 0)
                        col = "Unit";
                    if (col.CompareTo("ListeprisPrEnhed") == 0)
                        col = "SalesPrice";
                    if (col.CompareTo("RabatHirakiNavn") == 0)
                        col = "DiscountGroup";
                    if (col.CompareTo("VaregruppeNavn") == 0)
                        col = "ItemGroup";
                    if (col.CompareTo("Deeplink") == 0)
                        col = "WebArg";
                    dataColumns[icol] = new DataColumn(col);
                }
            }
            hasHeaderColumnGenerated = true;
            return dataColumns;
        }

        private static bool ValidateColumnHeaderValues(List<string> firstLineValues)
        {
            bool isValid = true;
            DateTime dt;
            int iVal;
            double dVal;
            long lVal;
            foreach (var val in firstLineValues)
            {
                if (DateTime.TryParse(val, out dt))
                    isValid = false;
                else if (long.TryParse(val, out lVal))
                    isValid = false;
                else if (double.TryParse(val, out dVal))
                    isValid = false;
                else if (int.TryParse(val, out iVal))
                    isValid = false;
                if (!isValid)
                    break;
            }
            return isValid;
        }
    }
}
