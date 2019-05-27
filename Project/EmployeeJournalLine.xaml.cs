using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EmployeeJournalLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EmployeeJournalLineClient); } }
        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (EmployeeJournalLineClient)this.SelectedItem;
            if (selectedItem == null || selectedItem.Employee == null)
                return false;
            return true;
        }
    }
    public partial class EmployeeJournalLine :GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EmployeeJournalLine; } }

        public EmployeeJournalLine(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgEmployeeJournalLineGrid;
            SetRibbonControl(localMenu, dgEmployeeJournalLineGrid);
            dgEmployeeJournalLineGrid.api = api;
            dgEmployeeJournalLineGrid.UpdateMaster(master);
            dgEmployeeJournalLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;          
		}

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgEmployeeJournalLineGrid.AddRow();
                    break;
                case "CopyRow":
                    dgEmployeeJournalLineGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgEmployeeJournalLineGrid.DeleteRow();
                    break;             
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
	}
}

