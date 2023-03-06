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
    public class ProductionOrderGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProductionOrderGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProductionOrderGroupPage : GridBasePage
    {
        public ProductionOrderGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgProductionOrderGroup;
            dgProductionOrderGroup.api = api;
            dgProductionOrderGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgProductionOrderGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProductionOrderGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgProductionOrderGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgProductionOrderGroup.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
