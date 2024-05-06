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
    public class InvStatisticsGroupPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvStatisticsGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvStatisticsGroupPage : GridBasePage
    {
        public InvStatisticsGroupPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        public InvStatisticsGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvStatisticsGroup;
            dgInvStatisticsGroup.api = api;
            dgInvStatisticsGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgInvStatisticsGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInvStatisticsGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgInvStatisticsGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgInvStatisticsGroup.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
