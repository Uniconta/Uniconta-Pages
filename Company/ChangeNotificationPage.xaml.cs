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
using Corasau.API.Admin;

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
        public string Table { get { return GetTableName(_TableId); } set { _table = value; _TableId = GetClassId(_table); } }

        string GetTableName(int id)
        {
            if (id != 0)
            {
                foreach (var type in Global.GetStandardRefTables())
                {
                    var table = Activator.CreateInstance(type) as UnicontaBaseEntity;
                    if (id == table.ClassId())
                        return type.Name;
                }

                foreach (var usrType in Global.GetUserTables(Company.Get(CompanyId)))
                {
                    var usrTable = Activator.CreateInstance(usrType) as UnicontaBaseEntity;
                    if (usrTable is TableData tblData && id == tblData.GetClassIdSpecial())
                        return usrType.Name;
                }

                if (id == InvItemStorage.CLASSID)
                    return "InvItemStorage";
            }
            return string.Empty;
        }

        int GetClassId(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                var table = Global.GetStandardRefTables().FirstOrDefault(x => x.Name == key);
                if (table != null)
                {
                    var tableType = Activator.CreateInstance(table) as UnicontaBaseEntity;
                    return tableType.ClassId();
                }

                var usrTable = Global.GetUserTables(Company.Get(CompanyId)).FirstOrDefault(x => x.Name == key);
                if (usrTable != null)
                {
                    var usrTableType = Activator.CreateInstance(usrTable) as UnicontaBaseEntity;
                    if (usrTableType is TableData tblData)
                        return tblData.GetClassIdSpecial();
                }

                if (key == "InvItemStorage")
                    return InvItemStorage.CLASSID;

            }
            return 0;
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
            BindJobs();
        }

        private void BindRefTable()
        {
            var referenceTables = new List<string>(100);
            foreach (Type tabletype in Global.GetStandardRefTables())
                referenceTables.Add(tabletype.Name);

            //UserDefined Tables
            foreach (var userRefTable in Global.GetUserTables(api.CompanyEntity))
                referenceTables.Add(userRefTable.Name);

            referenceTables.Add("InvItemStorage");
            referenceTables.Sort();
            cmbTableTypes.ItemsSource = referenceTables;
        }

        async private void BindJobs()
        {
            var jobApi = new JobAPI(api);
            var jobs = (JobsQueueClient[])await jobApi.GetJobQueueInfo(new JobsQueueClient());
            if (jobs == null || jobs.Length == 0)
                return;

            cmbJobs.ItemsSource = jobs.Select(p => p.Name).ToList();
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
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
