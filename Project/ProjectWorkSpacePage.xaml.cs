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
    public class PrWorkSpaceGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PrWorkSpaceClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool Readonly { get { return false; } }
    }

    public partial class ProjectWorkSpacePage : GridBasePage
    {
        protected override bool IsLayoutSaveRequired() { return false; }

        public ProjectWorkSpacePage(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }

        public ProjectWorkSpacePage(BaseAPI api, string lookupKey)
         : base(api, lookupKey)
        {
            Init();
        }


        void Init()
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgPrWorkSpace);
            dgPrWorkSpace.api = api;
            dgPrWorkSpace.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgPrWorkSpace.AddRow();
                    break;
                case "DeleteRow":
                    dgPrWorkSpace.DeleteRow();
                    break;
                case "SaveGrid":
                    dgPrWorkSpace.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
