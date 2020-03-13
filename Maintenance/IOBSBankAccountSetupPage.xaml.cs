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
    public class IOBSBankAccountSetupPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(IOBSBankAccountSetupClient); } }
    }

    public partial class IOBSBankAccountSetupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.IOBSBankAccountSetupPage.ToString(); } }
        public IOBSBankAccountSetupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            dgIOBSBankAccountSetupPageGrid.api = api;
            SetRibbonControl(localMenu, dgIOBSBankAccountSetupPageGrid);
            dgIOBSBankAccountSetupPageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.IOBSBankAccountSetupPage2)
                dgIOBSBankAccountSetupPageGrid.UpdateItemSource(argument);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgIOBSBankAccountSetupPageGrid.SelectedItem as IOBSBankAccountSetupClient;
            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.IOBSBankAccountSetupPage2, api, Uniconta.ClientTools.Localization.lookup("BankAccountSetup"), "Add_16x16.png");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;

                    AddDockItem(TabControls.IOBSBankAccountSetupPage2, selectedItem, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("BankAccountSetup"), selectedItem.BankAccountNumber));
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgIOBSBankAccountSetupPageGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgIOBSBankAccountSetupPageGrid.syncEntity);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
