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
    public class InvDiscountGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvDiscountGroupClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class InvDiscountGroupPage : GridBasePage
    {
        public InvDiscountGroupPage(BaseAPI api, string lookupKey) : base(api, lookupKey)
        {
            Init();
        }
        public InvDiscountGroupPage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        void Init()
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvDiscountGroup;
            dgInvDiscountGroup.api = api;
            dgInvDiscountGroup.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgInvDiscountGroup);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInvDiscountGroup.AddRow();
                    break;
                case "DeleteRow":
                    dgInvDiscountGroup.DeleteRow();
                    break;
                case "SaveGrid":
                    dgInvDiscountGroup.SaveData();
                    break;
                case "RefreshGrid":
                    dgInvDiscountGroup.Filter(null);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
