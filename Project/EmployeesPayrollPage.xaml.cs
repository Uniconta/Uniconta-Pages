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
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EmployeePayrollGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EmpPayrollCategoryClient); } }
        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (EmpPayrollCategoryClient)this.SelectedItem;
            return (selectedItem?._Number != null);
        }
    }
    public partial class EmployeesPayrollPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EmployeesPayrollPage; } }
        public EmployeesPayrollPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgEmployeePayrollGrid);
            dgEmployeePayrollGrid.api = api;
            dgEmployeePayrollGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);

            var Comp = api.CompanyEntity;

            if (Comp.TimeManagement)
            {
                Item.Visible = false;
                ItemName.Visible = false;
                SalesPrice.Visible = true;
                PrCategory.Visible = true;
                CategoryName.Visible = true;
                Factor.Visible = true;
                InternalType.Visible = true;
                InternalProject.Visible = true;
                InternalProjectName.Visible = true;
            }
        }

        protected override void LoadCacheInBackGround()
        {
            LoadNow(typeof(Uniconta.DataModel.InvItem));
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            EmpPayrollCategoryClient selectedItem = dgEmployeePayrollGrid.SelectedItem as EmpPayrollCategoryClient;
            switch (ActionType)
            {
                case "AddRow":
                    var rec = dgEmployeePayrollGrid.AddRow() as EmpPayrollCategoryClient;
                    rec.SetMaster(api.CompanyEntity);
                    break;
                case "CopyRow":
                    dgEmployeePayrollGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgEmployeePayrollGrid.DeleteRow();
                    break;
                case "EmpPayrollCategory":
                    if (selectedItem != null)
                        AddDockItem(TabControls.EmpPayrolCategoryPage, dgEmployeePayrollGrid.syncEntity);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
