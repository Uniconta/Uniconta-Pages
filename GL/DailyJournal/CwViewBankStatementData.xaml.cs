using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using System.IO;
using System.Collections.ObjectModel;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using DevExpress.Spreadsheet;
using Uniconta.ClientTools.Util;
using DevExpress.Spreadsheet.Export;
using System.Xml.Linq;
using System.Globalization;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwViewBankStatementData : ChildWindow
    {
        string file;
        char fileDelimiter;
        bool updateDelimiter;

        List<CustomDataColumn> customDataColumnSource;
        BankImportFormatClient bankImportFormat;
        public CwViewBankStatementData(string filePath, BankImportFormatClient bankImportFormatClient) : this(filePath)
        {
            bankImportFormat = bankImportFormatClient;
            if (bankImportFormat != null)
            {
                OKButton.Content = Uniconta.ClientTools.Localization.lookup("Update");
                txtPosition.Visibility = listBoxProperties.Visibility = Visibility.Visible;
                listBoxProperties.ItemTemplate = CreateListBoxDataTemplate(bankImportFormatClient);
            }
        }

        public CwViewBankStatementData(string filePath)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"), Uniconta.ClientTools.Localization.lookup("BankStatement"));
            file = filePath;
            LoadGrid();
            this.Loaded += CW_Loaded;
        }

        private DataTemplate CreateListBoxDataTemplate(BankImportFormatClient bankImportFormatClient)
        {
            var datatemplate = new DataTemplate(typeof(ListBoxEditor));
            var datatemplateItem = new FrameworkElementFactory(typeof(DevExpress.Xpf.Editors.ComboBoxEdit));
            datatemplateItem.SetValue(DevExpress.Xpf.Editors.ComboBoxEdit.PopupHeightProperty, 200d);
            var source = CreatePropertySource(bankImportFormatClient);
            datatemplateItem.SetBinding(DevExpress.Xpf.Editors.LookUpEditBase.SelectedItemProperty, new Binding("ActualDataColumnName"));
            datatemplateItem.SetValue(DevExpress.Xpf.Editors.LookUpEditBase.ItemsSourceProperty, source);
            datatemplate.VisualTree = datatemplateItem;

            return datatemplate;
        }

        private List<string> CreatePropertySource(BankImportFormatClient bankImportFormatClient)
        {
            var propertyInfos = UtilFunctions.GetDisplayAttributeNonReadOnlyPropertiesFromType(bankImportFormatClient.GetType(), true);

            List<string> propertySource = new List<string>(propertyInfos.Count);
            foreach (var propInfo in propertyInfos)
            {
                if (propInfo.PropertyType == typeof(byte))
                {
                    if (propInfo.Name == "SkipLines")
                        continue;

                    propertySource.Add(string.Format("{0} ({1})", propInfo.Name, UtilFunctions.GetDisplayNameFromPropertyInfo(propInfo)));
                }
            }
            return propertySource;
        }

        void LoadGrid()
        {
            if (string.IsNullOrEmpty(file))
            {
                var openFileDialog = UtilDisplay.LoadOpenFileDialog;
                openFileDialog.Multiselect = false;
                openFileDialog.Filter = "CSV Files (*.csv)|*.csv|XLS Files (*.xls)|*.xls|XLSX Files (*.xlsx)|*.xlsx|TXT Files (*.txt)|*.txt |All files (*.*)|*.*";
                bool? userClickedOK = openFileDialog.ShowDialog();
                if (userClickedOK == true)
                    file = openFileDialog.FileName;
                if (string.IsNullOrEmpty(file))
                    return;
            }
            string fileExtension = System.IO.Path.GetExtension(file);
            try
            {
                bool hasType;
                DataTable DataTable;
                if (fileExtension == ".csv")
                {
                    hasType = false;
                    updateDelimiter = true;
                    DataTable = FromCsv(file);
                }
                else if (fileExtension == ".xls" || fileExtension == ".xlsx")
                {
                    hasType = true;
                    IWorkbook workBook = importSpreadSheet.Document;
                    workBook.LoadDocument(file);
                    DevExpress.Spreadsheet.Worksheet worksheet = importSpreadSheet.Document.Worksheets[0];
                    var range = worksheet.GetUsedRange();
                    var dataTable = worksheet.CreateDataTable(range, true);
                    for (int col = 0; col < range.ColumnCount; col++)
                    {
                        CellValueType cellType = range[0, col].Value.Type;
                        for (int r = 1; r < range.RowCount; r++)
                        {
                            if (cellType != range[r, col].Value.Type)
                            {
                                dataTable.Columns[col].DataType = typeof(string);
                                break;
                            }
                        }
                    }
                    DataTableExporter exporter = worksheet.CreateDataTableExporter(range, dataTable, true);
                    exporter.Export();
                    DataTable = exporter.DataTable;
                }
                else if (fileExtension == ".txt")
                {
                    hasType = false;
                    DataSet theDataSet = new DataSet();
                    theDataSet.ReadXml(file);
                    DataTable = theDataSet.Tables[0];
                }
                else
                {
                    file = string.Empty;
                    return;
                }

                dgBankStmt.ItemsSource = DataTable;
                customDataColumnSource = CreateCustomDataColumn(DataTable, hasType).ToList();
                dgBankStmt.ColumnsSource = customDataColumnSource;
                listBoxProperties.ItemsSource = customDataColumnSource;
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
        }

        private IEnumerable<CustomDataColumn> CreateCustomDataColumn(DataTable dataTable, bool setType)
        {
            if (dataTable == null || dataTable.Columns.Count == 0)
                return null;

            var dataColumnList = new List<CustomDataColumn>();
            int index = 1;
            foreach (DataColumn dtCol in dataTable.Columns)
            {
                CustomDataColumn customDtCol;
                if (setType)
                    customDtCol = new CustomDataColumn(dtCol.ColumnName, dtCol.DataType, index);
                else
                    customDtCol = new CustomDataColumn(dtCol.ColumnName, index);

                index++;
                dataColumnList.Add(customDtCol);
            }

            return dataColumnList;
        }

        public DataTable FromCsv(string strFilePath)
        {
            fileDelimiter = GetDelimiter(strFilePath);
            DataSet oDS = new DataSet();
            oDS.Tables.Add("Property");
            var oTable = oDS.Tables[0];
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
                        var colCount = Math.Min(oTable.Columns.Count, 20);
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

                //Removing if Extra Columns were created
                int colIndex = 20;
                while (cellCount < oTable.Columns.Count)
                    oTable.Columns.RemoveAt(--colIndex);

            }
            return oTable;
        }

        bool hasHeaderColumnGenerated;
        private DataColumn[] GenerateColumnHeader(List<string> firstLineValues, out bool showColumnHeaders)
        {
            DataColumn[] dataColumns;
            showColumnHeaders = ValidateColumnHeaderValues(firstLineValues);
            if (!showColumnHeaders)
            {
                int maxIndex = 20;
                dataColumns = new DataColumn[maxIndex];
                for (int icol = 0; icol < maxIndex; icol++)
                    dataColumns[icol] = new DataColumn(string.Concat("Column ", NumberConvert.ToString(icol + 1)));

                dgBankStmt.View.ShowColumnHeaders = showColumnHeaders;
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

                    dataColumns[icol] = new DataColumn(col);
                }
                dgBankStmt.View.ShowColumnHeaders = showColumnHeaders;
            }
            hasHeaderColumnGenerated = true;

            return dataColumns;
        }

        private bool ValidateColumnHeaderValues(List<string> firstLineValues)
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

        private char GetDelimiter(string filePath)
        {
            char delimiter = '\0';
            var rowCount = System.IO.File.ReadAllLines(filePath).Length;
            using (var reader = new StreamReader(filePath))
                delimiter = CSVHelper.DetectDelimiter(reader, rowCount);

            return delimiter;
        }

        private void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { OKButton.Focus(); }));
            if (string.IsNullOrEmpty(file))
                this.Close();
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
                OKButton_Click(null, null);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (customDataColumnSource == null || customDataColumnSource.Count == 0 || bankImportFormat == null)
            {
                DialogResult = true;
                return;
            }
            var bankformatPositionProps = UtilFunctions.GetDisplayAttributeNonReadOnlyPropertiesFromType(bankImportFormat.GetType(), true);

            var strSplit = new StringSplit('(');
            foreach (var propInfo in bankformatPositionProps)
            {
                if (propInfo.PropertyType == typeof(byte))
                {
                    var propName = propInfo.Name;

                    if (propName == "SkipLines")
                        continue;

                    CustomDataColumn selectedCustomCol = null;
                    foreach (var col in customDataColumnSource)
                    {
                        if (col.ActualDataColumnName != null && col.ActualDataColumnName.Contains(propName))
                        {
                            selectedCustomCol = col;
                            break;
                        }
                    }

                    if (selectedCustomCol != null)
                    {
                        var selectedItem = selectedCustomCol.ActualDataColumnName;
                        var propertyName = strSplit.Split(selectedItem).First();

                        var prop = bankImportFormat.GetType().GetProperty(propertyName);
                        if (prop != null)
                            prop.SetValue(bankImportFormat, (byte)selectedCustomCol.DataColumnIndex, null);
                    }
                    else
                        propInfo.SetValue(bankImportFormat, (byte)0, null); // reset the value
                }
            }

            //Setting the delimiter value from the file to the bankformat
            if (updateDelimiter)
            {
                var seperatorProp = bankImportFormat.GetType().GetProperty("Seperator");
                if (seperatorProp != null)
                {
                    var sepValue = (char)seperatorProp.GetValue(bankImportFormat);
                    if (sepValue.CompareTo(fileDelimiter) != 0)
                        seperatorProp.SetValue(bankImportFormat, fileDelimiter, null);

                }
            }

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    public class CustomDataColumn : DataColumn
    {
        public string DataColumnName { get; set; }
        public double DataColumnWidth { get; set; }
        public int DataColumnIndex { get; set; }
        public string DataColumnHeader { get; set; }
        public string ActualDataColumnName { get; set; }

        public CustomDataColumn(string columnName, int index) : base(columnName)
        {
            InitDataColumnFields(columnName, index);
        }

        public CustomDataColumn(string columnName, Type columnType, int index) : base(columnName, columnType)
        {
            InitDataColumnFields(columnName, index);
        }

        private void InitDataColumnFields(string columnName, int index)
        {
            DataColumnName = columnName;
            DataColumnWidth = 150.0;
            DataColumnIndex = index;
            DataColumnHeader = columnName;
        }
    }
}
