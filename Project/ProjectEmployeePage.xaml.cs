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
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectEmployeePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectEmployeeClient); } }
        public override bool Readonly { get { return false; } }
        public override bool SingleBufferUpdate { get { return false; } }
    }

    public partial class ProjectEmployeePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectEmployeePage; } }

        public ProjectEmployeePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
        }

        public ProjectEmployeePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectEmployeePage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage(null);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectEmployee.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgProjectEmployee.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectEmployee"), key);
            SetHeader(header);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            localMenu.dataGrid = dgProjectEmployee;
            dgProjectEmployee.api = api;
            dgProjectEmployee.UpdateMaster(master);
            SetRibbonControl(localMenu, dgProjectEmployee);
            dgProjectEmployee.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            if (master is Uniconta.DataModel.Employee)
                Employee.Visible= EmployeeName.Visible = false;
            else if (master is Uniconta.DataModel.Project)
                Project.Visible = ProjectName.Visible= false;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectEmployee.AddRow();
                    break;
                case "CopyRow":
                    dgProjectEmployee.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgProjectEmployee.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
