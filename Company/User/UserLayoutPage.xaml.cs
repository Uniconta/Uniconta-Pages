using UnicontaClient.Models;
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
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class UserLayoutPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(Uniconta.ClientTools.Page.BasePage.LayoutClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class UserLayoutPage : GridBasePage
    {
        public override string NameOfControl
        {
            get { return TabControls.UserLayoutPage.ToString(); }
        }

        public UserLayoutPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            dgUseyLayoutGrid.api = api;
            dgUseyLayoutGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgUseyLayoutGrid);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "DeleteRow":
                    dgUseyLayoutGrid.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
