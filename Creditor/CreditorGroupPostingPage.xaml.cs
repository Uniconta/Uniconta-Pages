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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorGroupPostingGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DCGroupPostingClient); } }
    }
    /// <summary>
    /// Interaction logic for CreditorGroupPosting.xaml
    /// </summary>
    public partial class CreditorGroupPostingPage : GridBasePage
    {
        public override string NameOfControl
        {
            get
            {
                return base.NameOfControl;
            }
        }
        public CreditorGroupPostingPage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgCreditorGroupPosting);
            dgCreditorGroupPosting.api = api;
            dgCreditorGroupPosting.BusyIndicator = busyIndicator;
            SetHeaders();
           localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            // dgCreditorGroupPosting.UpdateMaster(master);
            dgCreditorGroupPosting.masterRecords= new List<UnicontaBaseEntity>() { master, new CreditorGroupClient() };
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

        private void SetHeaders()
        {
            RevenueAccount.Header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("Domestic"));
            RevenueAccount1.Header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("EUMember"));
            RevenueAccount2.Header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("Abroad"));
            RevenueAccount3.Header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("NoVATRegistration"));
            RevenueAccount4.Header = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("ExemptVat"));
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgCreditorGroupPosting.SelectedItem as DCGroupPostingClient;
            switch (ActionType)
            {
                case "AddRow":
                    object[] addParam = new object[2];
                    addParam[0] = api;
                    addParam[1] = dgCreditorGroupPosting.masterRecord;

                    AddDockItem(TabControls.CreditorGroupPostingPage2, addParam, Uniconta.ClientTools.Localization.lookup("CreditorPosting"), "Add_16x16.png");
                    break;

                case "EditRow":
                    if (selectedItem == null) return;
                    string grpPostingHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CreditorPosting"), selectedItem._Group);
                    object[] EditParam = new object[2];
                    EditParam[0] = selectedItem;
                    EditParam[1] = true;

                    AddDockItem(TabControls.CreditorGroupPostingPage2, EditParam, grpPostingHeader, "Edit_16x16.png");
                    break;

                case "CopyRow":
                    if (selectedItem == null) return;

                    object[] copyParam = new object[2];
                    copyParam[0] = selectedItem;
                    copyParam[1] = false;
                    string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Group);
                    AddDockItem(TabControls.CreditorGroupPostingPage2, copyParam, header);
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorGroupPostingPage2)
                dgCreditorGroupPosting.UpdateItemSource(argument);
        }
    }
}
