using UnicontaClient.Pages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ChangeNotificationDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TableChangeEventLocalClient); } }
        public override bool Readonly { get { return false; } }
    }

    public class TableChangeEventLocalClient : TableChangeEventClient
    {
        public string _table;
        
        [Display(Name = "Table", ResourceType = typeof(TableChangeEventClientText))]
        public string Table { get { return GetTableName(_TableId); } set { _table = value;  _TableId = GetClassId(_table);} }

        string GetTableName(int id)
        {
            string tableName = string.Empty;
            if(id!=0)
            {
                foreach (var type in Global.GetStandardRefTables())
                {
                    var table = Activator.CreateInstance(type) as UnicontaBaseEntity;
                    if (id == table.ClassId())
                    {
                        tableName= type.Name;
                        break;
                    }
                }
            }
            return tableName;
        }
        int GetClassId(string key)
        {
            int id = 0;
            if(!string.IsNullOrEmpty(key))
            {
                var table = Global.GetStandardRefTables().Where(x => x.Name == key).FirstOrDefault();
                if(table!= null)
                {
                    var tableType = Activator.CreateInstance(table) as UnicontaBaseEntity;
                    id = tableType.ClassId();
                }
            }
            return id;
        }
    }
    public partial class ChangeNotificationPage : GridBasePage
    {
        public ChangeNotificationPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgChangeNotification;
            dgChangeNotification.api = api;
            dgChangeNotification.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgChangeNotification);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            BindRefTable();
        }

        private void BindRefTable()
        {
            var referenceTables = new List<string>();
            foreach (Type tabletype in Global.GetStandardRefTables())
            {
                if (tabletype == typeof(DebtorOrder) || tabletype == typeof(DebtorOffer) || tabletype == typeof(CreditorOrder) || tabletype == typeof(ProductionOrder))
                    continue;
                referenceTables.Add(tabletype.Name);
            }
            cmbTableTypes.ItemsSource = referenceTables;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgChangeNotification.AddRow();
                    break;
                case "DeleteRow":
                    dgChangeNotification.DeleteRow();
                    break;
                case "SaveGrid":
                    dgChangeNotification.SaveData();
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
