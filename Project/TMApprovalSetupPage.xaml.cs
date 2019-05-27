using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using DevExpress.XtraGrid;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class TMapprovalSetupPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(TMApprovalSetupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class TMApprovalSetupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.TMApprovalSetupPage; } }
        public TMApprovalSetupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgTMapprovalSetupGrid;
            SetRibbonControl(localMenu, dgTMapprovalSetupGrid);
            dgTMapprovalSetupGrid.api = api;
            dgTMapprovalSetupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.Employee) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgTMapprovalSetupGrid.SelectedItem as TMApprovalSetupClient;
            switch (ActionType)
            {
                case "AddRow":
                    var row = dgTMapprovalSetupGrid.AddRow() as TMApprovalSetupClient;
                    row.SetMaster(api.CompanyEntity);
                    break;
                case "CopyRow":
                    dgTMapprovalSetupGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgTMapprovalSetupGrid.SaveData();
                    break;
                case "DeleteRow":
                    if (selectedItem == null) return;
                        dgTMapprovalSetupGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
