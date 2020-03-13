using UnicontaClient.Models;
using UnicontaClient.Pages;
using UnicontaClient.Utilities;
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
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class IOBSClaimTablePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(IOBSClaimTableClient); } }
    }

    public partial class IOBSClaimTablePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.IOBSClaimTablePage.ToString(); } }

        public IOBSClaimTablePage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        public IOBSClaimTablePage(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgIOBSClaimTablePageGrid.api = api;
            dgIOBSClaimTablePageGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgIOBSClaimTablePageGrid);
            dgIOBSClaimTablePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.IOBSClaimTablePage2)
                dgIOBSClaimTablePageGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgIOBSClaimTablePageGrid.SelectedItem as IOBSClaimTableClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.IOBSClaimTablePage2, api, Uniconta.ClientTools.Localization.lookup("CliamTable"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    AddDockItem(TabControls.IOBSClaimTablePage2, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("CliamTable"), selectedItem.ClaimNumber));
                    break;
                case "ClaimLine":
                    if(selectedItem!= null)
                        AddDockItem(TabControls.IOBSClaimLinePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ClaimLine"), selectedItem.ClaimNumber));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
