using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EmpPayrolCategoryPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EmpPayrollCategoryEmployeeClient); } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (EmpPayrollCategoryEmployeeClient)this.SelectedItem;
            return (selectedItem?._PayrollCategory != null);
        }
    }

    public partial class EmpPayrolCategoryPage : GridBasePage
    {
        public EmpPayrolCategoryPage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public EmpPayrolCategoryPage(SynchronizeEntity syncEntity ): base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgEmpPayCatGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgEmpPayCatGrid.masterRecord);

            if (string.IsNullOrEmpty(key)) return;

            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("EmployeeRates"), key);
            SetHeader(header);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            dgEmpPayCatGrid.api = api;
            dgEmpPayCatGrid.UpdateMaster(master);

            SetRibbonControl(localMenu, dgEmpPayCatGrid);
            dgEmpPayCatGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            setDim();

            var master = dgEmpPayCatGrid.masterRecords?.First();
            if (master is Uniconta.DataModel.EmpPayrollCategory)
            {
                colPayrolCat.Visible = false;
                colPayrolCatName.Visible = false;
            }

            if (master is Uniconta.DataModel.Employee)
            {
                colEmployee.Visible = false;
                colEmployeeName.Visible = false;
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            EmpPayrollCategoryEmployeeClient selectedItem = dgEmpPayCatGrid.SelectedItem as EmpPayrollCategoryEmployeeClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgEmpPayCatGrid.AddRow();
                    break;
                case "CopyRow":
                    dgEmpPayCatGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgEmpPayCatGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgEmpPayCatGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void setDim()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
