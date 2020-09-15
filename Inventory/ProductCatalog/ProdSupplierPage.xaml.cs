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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProdSupplierPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProdSupplierClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProdSupplierPage : GridBasePage
    {
        public ProdSupplierPage(UnicontaBaseEntity _master)
            : base(_master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgProdSupplier;
            SetRibbonControl(localMenu, dgProdSupplier);
            gridControl.api = api;
            gridControl.BusyIndicator = busyIndicator;
            dgProdSupplier.UpdateMaster(_master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProdSupplier.AddRow();
                    break;
                case "DeleteRow":
                    dgProdSupplier.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
