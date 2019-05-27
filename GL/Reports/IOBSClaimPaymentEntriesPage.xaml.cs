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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class IOBSClaimPaymentEntriesPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(IOBSClaimPaymentEntryClient); } }
    }

    public partial class IOBSClaimPaymentEntriesPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.IOBSClaimPaymentEntriesPage.ToString(); } }
        public IOBSClaimPaymentEntriesPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            dgIOBSClaimPaymentEntriesPageGrid.api = api;
            SetRibbonControl(localMenu, dgIOBSClaimPaymentEntriesPageGrid);
            dgIOBSClaimPaymentEntriesPageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.IOBSClaimPaymentEntriesPage2)
                dgIOBSClaimPaymentEntriesPageGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgIOBSClaimPaymentEntriesPageGrid.SelectedItem as IOBSClaimPaymentEntryClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.IOBSClaimPaymentEntriesPage2, api, Uniconta.ClientTools.Localization.lookup("ClaimPaymentEntry"), ";component/Assets/img/Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] EditParam = new object[2];
                    AddDockItem(TabControls.IOBSClaimPaymentEntriesPage2, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("ClaimPaymentEntry"), selectedItem.BillNumber));
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgIOBSClaimPaymentEntriesPageGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgIOBSClaimPaymentEntriesPageGrid.syncEntity);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
