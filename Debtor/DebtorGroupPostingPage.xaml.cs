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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorGroupPostingGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DCGroupPostingClient); } }
    }
    /// <summary>
    /// Interaction logic for InventoryGroupPosting.xaml
    /// </summary>
    public partial class DebtorGroupPostingPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DebtorGroupPostingPage; } }

        public DebtorGroupPostingPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgGroupPosting);
            dgGroupPosting.api = api;
            dgGroupPosting.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgGroupPosting.masterRecords = new List<UnicontaBaseEntity>() { master, new DebtorGroupClient() };
            if (master is InvGroupClient)
                HideColumns(false);
            else
                HideColumns(true);
        }

        private void HideColumns(bool value)
        {
            InvGroup.Visible = value;
            Group.Visible = !value;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgGroupPosting.SelectedItem as DCGroupPostingClient;

            switch (ActionType)
            {
                case "AddRow":
                    object[] addParam = new object[2];
                    addParam[0] = api;
                    addParam[1] = dgGroupPosting.masterRecord;
                    AddDockItem(TabControls.DebtorGroupPostingPage2, addParam, Uniconta.ClientTools.Localization.lookup("CustomerPosting"), ";component/Assets/img/Add_16x16.png");
                    break;

                case "EditRow":
                    if (selectedItem == null) return;
                    string grpPostingHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CustomerPosting"), selectedItem.Group);
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;
                    AddDockItem(TabControls.DebtorGroupPostingPage2, EditParam, grpPostingHeader, ";component/Assets/img/Edit_16x16.png");
                    break;

                case "CopyRow":
                    if (selectedItem == null) return;

                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Group);
                    AddDockItem(TabControls.DebtorGroupPostingPage2, copyParam, header);
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorGroupPostingPage2)
                dgGroupPosting.UpdateItemSource(argument);
        }
    }
}
