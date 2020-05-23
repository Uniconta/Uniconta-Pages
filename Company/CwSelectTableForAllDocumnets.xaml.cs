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
            var xlist = new List<TableList>(100);
            var tablestype = Global.Tables; 
            foreach (var type in tablestype)
            {
                if (!typeof(IdKey).IsAssignableFrom(type))
                    continue;

                var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                if (clientTableAttr.Length > 0)
                {
                    var attr = (ClientTableAttribute)clientTableAttr[0];
                    if (attr.CanUpdate)
                        xlist.Add(new TableList(type, string.Format("{0} ({1})", type.Name, Uniconta.ClientTools.Localization.lookup(attr.LabelKey))));
                }
                else
                    xlist.Add(new TableList(type, type.Name));
            }
            GetUserTableList(xlist);
            xlist.Add(new TableList(typeof(DebtorInvoiceClient), string.Format("{0} ({1})", "DebtorInvoiceClient", Uniconta.ClientTools.Localization.lookup("DebtorInvoice"))));
            xlist.Add(new TableList(typeof(CreditorInvoiceClient), string.Format("{0} ({1})", "CreditorInvoiceClient", Uniconta.ClientTools.Localization.lookup("CreditorInvoice")) ));
            xlist.Add(new TableList(typeof(DebtorDeliveryNoteClient), string.Format("{0} ({1})", "DebtorDeliveryNoteClient", Uniconta.ClientTools.Localization.lookup("Packnotes"))));
            xlist.Add(new TableList(typeof(CreditorDeliveryNoteClient), string.Format("{0} ({1})", "CreditorDeliveryNoteClient", Uniconta.ClientTools.Localization.lookup("CreditorPackNotes")) ));
            xlist.Add(new TableList(typeof(InvBOMClient), string.Format("{0} ({1})", "InvBOMClient", Uniconta.ClientTools.Localization.lookup("BOM")) ));
            xlist.Add(new TableList(typeof(ProductionPostedClient), string.Format("{0} ({1})", "ProductionPostedClient", Uniconta.ClientTools.Localization.lookup("ProductionPosted"))));
            xlist.Add(new TableList(typeof(InvSerieBatchClient), string.Format("{0} ({1})", "InvSerieBatchClient", Uniconta.ClientTools.Localization.lookup("SerialBatchNumbers")) ));
            xlist.Sort(new TableList.Sort());
            xlist.Insert(0, new TableList(null, Uniconta.ClientTools.Localization.lookup("AllDocuments")));
            dgTables.ItemsSource = xlist;
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
            if (dgTables.ItemsSource != null)
                this.DialogResult = true;
            else
                this.DialogResult = false;
        }

        void CreateTableList(object table)
        {
            var tbl = table as TableList;
            if (tbl.Type != null)
            {
                var taleType = Activator.CreateInstance(tbl.Type);
                tables.Add(taleType as UnicontaBaseEntity);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        private void GetUserTableList(List<TableList> listTypes)
        {
            var comp = api.CompanyEntity;
            var userTableHeaders = comp.UserTables;

            if (userTableHeaders != null)
            {
                foreach (var tblHeader in userTableHeaders)
                {
                    if (tblHeader._Attachment)
                    {
                        var userTblType = Global.GetUserTable(comp, tblHeader);
                        listTypes.Add(new TableList(userTblType, string.Format("{0} ({1})", userTblType.Name, tblHeader._Prompt ?? tblHeader._Name) ));
                    }
                }
            }
        }
    }
}
