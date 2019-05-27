using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CrmProductDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmProductClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CrmProductPage : GridBasePage
    {
        public CrmProductPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCrmProduct;
            dgCrmProduct.api = api;
            dgCrmProduct.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmProduct);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType( new Type[] { typeof(Uniconta.DataModel.CrmProduct), typeof(Uniconta.DataModel.CrmInterest) } );
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    var crmProTbl = (IList)dgCrmProduct.ItemsSource;
                    if (crmProTbl.Count < 60)
                        dgCrmProduct.AddRow();
                    break;
                case "DeleteRow":
                    dgCrmProduct.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCrmProduct.SaveData();
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
