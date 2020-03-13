using UnicontaClient.Models;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using System.Collections;
using Uniconta.Common;
using UnicontaClient.Utilities;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Grid Class for Email Setup
    /// </summary>
    public class EmailSetupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CompanySMTPClient); } }
    }
    /// <summary>
    /// Interaction logic for EmailSetupPage.xaml
    /// </summary>
    public partial class EmailSetupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EmailSetupPage; } }
        public EmailSetupPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgEmailSetupGrid);
            localMenu.dataGrid = dgEmailSetupGrid;
            dgEmailSetupGrid.api = api;
            dgEmailSetupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEmailSetupGrid.SelectedItem as CompanySMTPClient;

            switch (ActionType)
            {
                case "AddRow":
                    AddDockItem(TabControls.EmailSetupPage2, api, Uniconta.ClientTools.Localization.lookup("EmailSetup"), "Add_16x16.png");
                    break;

                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    object[] copyParam = new object[2];
                    copyParam[0] = StreamingManager.Clone(selectedItem);
                    copyParam[1] = false;
                    AddDockItem(TabControls.EmailSetupPage2, copyParam, string.Format("{0}: {1}", string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("EmailSetup")), selectedItem._Name), "Copy_16x16.png");
                    break;

                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] editParam = new object[2];
                    editParam[0] = selectedItem;
                    editParam[1] = true;
                    AddDockItem(TabControls.EmailSetupPage2, editParam, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("EmailSetup"), selectedItem._Name), "Edit_16x16.png");
                    break;
                case "ApprovalSetup":
                    var smtps = dgEmailSetupGrid.ItemsSource as IList<CompanySMTPClient>;
                    var approverEmailDialog = new CWApprovalEmailSetup(api, smtps);
                    approverEmailDialog.Show();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.EmailSetupPage2)
                dgEmailSetupGrid.UpdateItemSource(argument);
        }
    }
}
