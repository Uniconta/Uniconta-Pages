using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CrmGroupDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CrmGroupPage : GridBasePage
    {
       
        public CrmGroupPage(BaseAPI API):base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCrmGroup;
            dgCrmGroup.api = api;
            dgCrmGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgCrmGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgCrmGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCrmGroup.SaveData();
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
