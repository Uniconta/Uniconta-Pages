using System;
using System.Collections.Generic;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwSelectStandardTable : ChildWindow
    {
        CrudAPI api;
        public Type table;
        bool defaultAll;
        public CwSelectStandardTable(CrudAPI api) : this(api, false)
        {
        }
        public CwSelectStandardTable(CrudAPI api, bool onlyIdKeyTables, bool _defaultAll = false)
        {
            this.DataContext = this;
            defaultAll = _defaultAll;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("Table");
            this.api = api;
            if (onlyIdKeyTables)
                bindRefTable();
            else
                bindTablelist();
        }
        private void bindTablelist()
        {
            var xlist = new List<TableList>(100);
            List<Type> tablestype = Global.GetTables(api.CompanyEntity);
            foreach (var type in tablestype)
            {
                var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                if (clientTableAttr.Length > 0)
                {
                    var attr = (ClientTableAttribute)clientTableAttr[0];
                    if (attr.CanUpdate)
                        xlist.Add(new TableList(type , string.Format("{0} ({1})", type.Name, Uniconta.ClientTools.Localization.lookup(attr.LabelKey))));
                }
                else
                    xlist.Add(new TableList(type, type.Name));
            }
            xlist.Sort(new TableList.Sort());
            cmbStdTables.ItemsSource = xlist;
            cmbStdTables.SelectedIndex = 0;
        }
        private void bindRefTable()
        {
            var xlist = new List<TableList>(100);
            foreach (var type in Global.GetStandardUserRefTables())
            {
                var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                if ( clientTableAttr.Length > 0)
                {
                    if (type == typeof(DebtorOrderClient) || type == typeof(DebtorOfferClient) || type == typeof(CreditorOrderClient) || type == typeof(ProductionOrderClient))
                        continue;
                    var attr = (ClientTableAttribute)clientTableAttr[0];
                    if (attr.CanUpdate)
                        xlist.Add(new TableList(type, string.Format("{0} ({1})", type.Name, Uniconta.ClientTools.Localization.lookup(attr.LabelKey))));
                }
                else
                    xlist.Add(new TableList(type, type.Name));
            }
            if (defaultAll)
                xlist.Add(new TableList(null, null));
            xlist.Sort(new TableList.Sort());
            cmbStdTables.ItemsSource = xlist;
            cmbStdTables.SelectedIndex = 0;
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedTable = cmbStdTables.SelectedItem as TableList;
            table = selectedTable?.Type;
            if (selectedTable != null || defaultAll)
                this.DialogResult = true;
            else
                this.DialogResult = false;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }

    public class TableList
    {
        public TableList(Type t, string Name) { this.Name = Name;  this.Type = t; }
        public Type Type { get; set; }
        public string Name { get; set; }

        public class Sort : IComparer<TableList>
        {
            public int Compare(TableList x, TableList y) { return string.Compare(x.Name, y.Name); }
        }
    }
}
