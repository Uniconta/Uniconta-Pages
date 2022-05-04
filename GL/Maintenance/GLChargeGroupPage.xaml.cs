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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLChargeGroupClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLChargeGroupClient); } }
        public override bool Readonly { get { return false; } }
    }
    /// <summary>
    /// Interaction logic for GLChargeGroupClientGridPage.xaml
    /// </summary>
    public partial class GLChargeGroupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLChargeGroupPage; } }
        public GLChargeGroupPage(CrudAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            InitPage(api);
        }

        private void InitPage(CrudAPI api)
        {
            SetRibbonControl(localMenu, dgChargeGroupClient);
            dgChargeGroupClient.api = api;
            dgChargeGroupClient.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgChargeGroupClient.AddRow();
                    break;
                case "DeleteRow":
                    dgChargeGroupClient.DeleteRow();
                    break;
                case "SaveGrid":
                    dgChargeGroupClient.SaveData();
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
