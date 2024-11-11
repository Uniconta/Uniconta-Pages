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
    public class GLInterestGroupClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLInterestGroupClient); } }
        public override bool Readonly { get { return false; } }
    }
    /// <summary>
    /// Interaction logic for GLInterestGroupClientGridPage.xaml
    /// </summary>
    public partial class GLInterestGroupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLInterestGroupPage; } }
        public GLInterestGroupPage(CrudAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            InitPage(api);
        }

        private void InitPage(CrudAPI api)
        {
            SetRibbonControl(localMenu, dgInterestGroupClient);
            dgInterestGroupClient.api = api;
            dgInterestGroupClient.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInterestGroupClient.AddRow();
                    break;
                case "DeleteRow":
                    dgInterestGroupClient.DeleteRow();
                    break;
                case "SaveGrid":
                    dgInterestGroupClient.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
