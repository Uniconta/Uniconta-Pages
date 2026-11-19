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
        [Display(Name = "Table", ResourceType = typeof(TableChangeEventClientText))]
        public int Table { get { return _TableId; } set { _TableId = value; NotifyPropertyChanged("Table"); } }
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
            var comp = api.CompanyEntity;
            var referenceTables = new List<KeyValuePair<string, int>>(300);
            var ids = new HashSet<int>();
            foreach (Type tabletype in Global.GetTables(comp, true))
            {
                if (typeof(IRowId).IsAssignableFrom(tabletype) && Activator.CreateInstance(tabletype) is UnicontaBaseEntity rec && ids.Add(rec.ClassId()))
                    referenceTables.Add(new KeyValuePair<string, int>(tabletype.Name, rec.ClassId()));
            }

            //UserDefined Tables
            foreach (var userRefTable in Global.GetUserTables(comp))
            {
                if (Activator.CreateInstance(userRefTable) is TableData tblData && ids.Add(tblData.GetClassIdSpecial()))
                    referenceTables.Add(new KeyValuePair<string, int>(userRefTable.Name, tblData.GetClassIdSpecial()));
            }

            if (ids.Add(InvItemStorage.CLASSID))
                referenceTables.Add(new KeyValuePair<string, int>("InvItemStorage", InvItemStorage.CLASSID));

            referenceTables.Sort((a, b) => string.Compare(a.Key, b.Key));
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
