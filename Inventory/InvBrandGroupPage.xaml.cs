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
    public class InvBrandGroupPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvBrandGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvBrandGroupPage : GridBasePage
    {
        public InvBrandGroupPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        public InvBrandGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvBrandGroup;
            dgInvBrandGroup.api = api;
            dgInvBrandGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgInvBrandGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInvBrandGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgInvBrandGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgInvBrandGroup.SaveData();
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
