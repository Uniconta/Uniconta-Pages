using DevExpress.Xpf.Grid;
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
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TableForAllDocgrid : GridControl
    {
       
    }
    public partial class CwSelectTableForAllDocumnets : ChildWindow
    {
        CrudAPI api;
        public List<UnicontaBaseEntity> tables;
        public CwSelectTableForAllDocumnets(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("Table");
            this.api = api;
            bindTablelist();
        }
        private void bindTablelist()
        {
            var xlist = new List<TableList>();
            List<Type> tablestype = Global.GetTables(api.CompanyEntity);
            foreach (var type in tablestype)
            {
                var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                if (clientTableAttr.Length > 0)
                {
                    var attr = (ClientTableAttribute)clientTableAttr[0];
                    if (attr.CanUpdate)
                        xlist.Add(new TableList() { Name = string.Format("{0} ({1})", type.Name, Uniconta.ClientTools.Localization.lookup(attr.LabelKey)), Type = type });
                }
                else
                    xlist.Add(new TableList() { Name = type.Name, Type = type });
            }
            xlist.Add(new TableList() { Name = string.Format("{0} ({1})", "DebtorInvoiceClient", Uniconta.ClientTools.Localization.lookup("DebtorInvoice")), Type = typeof(DebtorInvoiceClient) });
            xlist.Add(new TableList() { Name = string.Format("{0} ({1})", "CreditorInvoiceClient", Uniconta.ClientTools.Localization.lookup("CreditorInvoice")), Type = typeof(CreditorInvoiceClient) });
            dgTables.ItemsSource = xlist.OrderBy(x => x.Name).ToList();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            tables = new List<UnicontaBaseEntity>();
            if (!string.IsNullOrEmpty(dgTables.FilterString))
            {
#if !SILVERLIGHT
                 foreach (var row in dgTables.VisibleItems)
                    CreateTableList(row);
#else
                for (int i = 0; i < dgTables.VisibleRowCount; i++)
                {
                    var rowElement = dgTables.View.GetRowElementByRowHandle(i);
                    if (rowElement != null)
                    {
                        var row = rowElement.DataContext;
                        CreateTableList(row);
                    }
                }
#endif
            }
            else
                CreateTableList(dgTables.SelectedItem);
            if (dgTables.ItemsSource !=  null)
                this.DialogResult = true;
            else
                this.DialogResult = false;
        }

        void CreateTableList(object table)
        {
            var tbl = table as TableList;
            var taleType = Activator.CreateInstance(tbl.Type);
            tables.Add(taleType as UnicontaBaseEntity);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
