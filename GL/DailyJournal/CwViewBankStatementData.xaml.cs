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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwViewBankStatementData : ChildWindow
    {
        string file;
        public CwViewBankStatementData(string filePath)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"), Uniconta.ClientTools.Localization.lookup("BankStatement"));
            file = filePath;
            var tableView = dgBankStmt.View as TableView;
            tableView.ShowGroupPanel = tableView.AllowEditing= false;
            LoadGrid();
            this.Loaded += CW_Loaded;
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
                if (fileExtension == ".csv")
                {
                    dgBankStmt.ItemsSource = FromCsv(file);
                }
                else if (fileExtension == ".xls" || fileExtension == ".xlsx")
                {
                    IWorkbook workBook = importSpreadSheet.Document;
                    workBook.LoadDocument(file);
                    DevExpress.Spreadsheet.Worksheet worksheet = importSpreadSheet.Document.Worksheets[0];
                    Range range = worksheet.GetUsedRange();
                    DataTable dataTable = worksheet.CreateDataTable(range, true);
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
                    dgBankStmt.ItemsSource = exporter.DataTable;
                }
                else if (fileExtension == ".txt")
                {
                    DataSet theDataSet = new DataSet();
                    theDataSet.ReadXml(file);
                    dgBankStmt.ItemsSource = theDataSet.Tables[0];
                }
                else
                {
                    file = string.Empty;
                    return;
                }

            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"), MessageBoxButton.OK);
            }
        }

        public DataTable FromCsv(string strFilePath)
        {
            string delimStr = ";;";
            char[] delimiter = delimStr.ToCharArray();
            DataSet oDS = new DataSet();
            string strFields = null;
            DataTable oTable = new DataTable();
            DataRow oRows = null;
            Int32 intCounter = 0;
            oDS.Tables.Add("Property");
            StreamReader oSR = new StreamReader(strFilePath);
            oSR.BaseStream.Seek(0, SeekOrigin.Begin);
            foreach (string strFields_loopVariable in oSR.ReadLine().Split(delimiter))
            {
                strFields = strFields_loopVariable;
                oDS.Tables[0].Columns.Add(strFields);
            }
            oTable = oDS.Tables[0];
            while ((oSR.Peek() > -1))
            {

                oRows = oTable.NewRow();
                var colCount = oTable.Columns.Count;
                foreach (string strFields_loopVariable in oSR.ReadLine().Split(delimiter))
                {
                    strFields = Convert.ToString(strFields_loopVariable);
                    if (intCounter < 20 && intCounter < colCount)
                    {
                        oRows[intCounter] = strFields;
                        intCounter = intCounter + 1;
                    }
                }
                intCounter = 0;
                oTable.Rows.Add(oRows);
            }
            return oTable;
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
            this.DialogResult = true;
        }
    }
}
