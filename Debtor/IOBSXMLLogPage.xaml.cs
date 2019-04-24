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
    public class IOBSXMLLogPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(IOBSXMLLogClient); } }
    }

    public partial class IOBSXMLLogPage : GridBasePage
    {
        public IOBSXMLLogPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            dgIOBSXMLLogPageGrid.api = api;
            SetRibbonControl(localMenu, dgIOBSXMLLogPageGrid);
            dgIOBSXMLLogPageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }
    }
}
