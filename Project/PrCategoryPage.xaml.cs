using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Navigation;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using System.Threading.Tasks;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.API.GeneralLedger;
using System.Windows.Threading;
using UnicontaClient.Utilities;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using System.Collections;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class PrCategoryGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PrCategoryClient); } }
       
        public override bool Readonly { get { return false; } }
    }
    public partial class PrCategoryPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.PrCategoryPage; } }

        public PrCategoryPage(BaseAPI API) : this(API, string.Empty) { }
        public PrCategoryPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitializeComponent();
            dgProjectCategoryGrid.api = this.api;
            SetRibbonControl(localMenu, dgProjectCategoryGrid);
            dgProjectCategoryGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            //PrCategoryClient selectedItem = dgProjectCategoryGrid.SelectedItem as PrCategoryClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectCategoryGrid.AddRow();
                    break;
                case "CopyRow":
                    dgProjectCategoryGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgProjectCategoryGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.GLAccount));
        }
    }
}