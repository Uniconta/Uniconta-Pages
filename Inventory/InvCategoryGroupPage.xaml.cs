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
    public class InvCategoryGroupPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvCategoryGroupClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvCategoryGroupPage : GridBasePage
    {
        public InvCategoryGroupPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        public InvCategoryGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvCategoryGroup;
            dgInvCategoryGroup.api = api;
            dgInvCategoryGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgInvCategoryGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInvCategoryGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgInvCategoryGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgInvCategoryGroup.SaveData();
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
