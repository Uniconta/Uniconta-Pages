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
    public class CrmCampaignGroupDataGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CrmCampaignGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class CrmCampaignGroupPage :  GridBasePage
    {
        public CrmCampaignGroupPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgCrmCampaignGroup;
            dgCrmCampaignGroup.api = api;
            dgCrmCampaignGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgCrmCampaignGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgCrmCampaignGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgCrmCampaignGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgCrmCampaignGroup.SaveData();
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
