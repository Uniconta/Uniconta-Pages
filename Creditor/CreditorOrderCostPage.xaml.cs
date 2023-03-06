using UnicontaClient.Pages;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorOrderCostGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorOrderCostClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CreditorOrderCostPage : GridBasePage
    {
        public CreditorOrderCostPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCreditorOrderCost;
            dgCreditorOrderCost.api = api;
            dgCreditorOrderCost.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCreditorOrderCost);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void LoadCacheInBackGround() { LoadType(typeof(Uniconta.DataModel.GLAccount)); }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    var row = dgCreditorOrderCost.AddRow() as CreditorOrderCostClient;
                    row._CompanyId = api.CompanyId;
                    break;
                case "DeleteRow":
                    dgCreditorOrderCost.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCreditorOrderCost.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
