using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Uniconta.ClientTools.Util;

using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PrStandardGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PrStandardClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class StandardClientPage : GridBasePage
    {
        public StandardClientPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            dgPrStandardGrid.api = api;
            SetRibbonControl(localMenu, dgPrStandardGrid);
            localMenu.dataGrid = dgPrStandardGrid;
            dgPrStandardGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.PrCategory));
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            PrStandardClient selectedItem = dgPrStandardGrid.SelectedItem as PrStandardClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgPrStandardGrid.AddRow();
                    break;
                case "CopyRow":
                    //dgPrStandardGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgPrStandardGrid.DeleteRow();
                    break;
                case "StandardCategory":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PrStandardCategoryPage, selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

    }
}
