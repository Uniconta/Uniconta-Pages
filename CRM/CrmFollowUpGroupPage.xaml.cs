using UnicontaClient.Pages;
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
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CrmFollowUpGroupDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmFollowUpGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CrmFollowUpGroupPage : GridBasePage
    {
       
        public CrmFollowUpGroupPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCrmFollowUpGroup;
            dgCrmFollowUpGroup.api = api;
            dgCrmFollowUpGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmFollowUpGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgCrmFollowUpGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgCrmFollowUpGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCrmFollowUpGroup.SaveData();
                    break;
                case "RefreshGrid":
                    dgCrmFollowUpGroup.Filter(null);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
