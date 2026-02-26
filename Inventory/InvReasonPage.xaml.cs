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
using Uniconta.DataModel;
using UnicontaClient.Pages;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvReasonPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvReasonClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class InvReasonPage : GridBasePage
    {
        public InvReasonPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        public InvReasonPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvReason;
            dgInvReason.api = api;
            dgInvReason.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgInvReason);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInvReason.AddRow();
                    break;
                case "DeleteRow":
                    dgInvReason.DeleteRow();
                    break;
                case "SaveGrid":
                    dgInvReason.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
