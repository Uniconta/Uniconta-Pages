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
    public class CrmInterestDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmInterestClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CrmInterestPage : GridBasePage
    {
        public CrmInterestPage(BaseAPI API):base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCrmInterest;
            dgCrmInterest.api = api;
            dgCrmInterest.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmInterest);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    var crmIntTbl = (IList)dgCrmInterest.ItemsSource;
                    if (crmIntTbl.Count <= 60)
                        dgCrmInterest.AddRow();
                    break;
                case "DeleteRow":
                    dgCrmInterest.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCrmInterest.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
