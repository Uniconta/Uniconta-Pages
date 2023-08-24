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
using Uniconta.Common.Utility;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

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
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
        }

        private void HideColumns(bool value)
        {
            InvGroup.Visible = value;
            Group.Visible = !value;
        }

        private void SetHeaders()
        {
            RevenueAccount.Header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("Domestic"));
            RevenueAccount1.Header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("EUMember"));
            RevenueAccount2.Header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("Abroad"));
            RevenueAccount3.Header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("NoVATRegistration"));
            RevenueAccount4.Header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("ExemptVat"));
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
                    AddDockItem(TabControls.CreditorGroupPostingPage2, addParam, Uniconta.ClientTools.Localization.lookup("CreditorPosting"), "Add_16x16");
                    break;

                case "EditRow":
                    if (selectedItem == null) return;
                    string grpPostingHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CreditorPosting"), selectedItem._Group);
                    object[] EditParam = new object[3];
                    EditParam[0] = selectedItem;
                    EditParam[1] = dgCreditorGroupPosting.masterRecord;
                    EditParam[2] = true;
                    AddDockItem(TabControls.CreditorGroupPostingPage2, EditParam, grpPostingHeader, "Edit_16x16");
                    break;
                case "CopyRecord":
                    if (selectedItem == null) return;
                    object[] copyParam = new object[3];
                    copyParam[0] = selectedItem;
                    copyParam[1] = dgCreditorGroupPosting.masterRecord;
                    copyParam[2] = false;
                    string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem.Group);
                    AddDockItem(TabControls.CreditorGroupPostingPage2, copyParam, header);
                    break;
                case "CopyRow":
                    dgCreditorGroupPosting.CopyRow();
                    break;
                case "EditAll":
                    if (dgCreditorGroupPosting.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "DeleteRow":
                    dgCreditorGroupPosting.DeleteRow();
                    break;
                case "UndoDelete":
                    dgCreditorGroupPosting.UndoDeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgCreditorGroupPosting.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgCreditorGroupPosting.MakeEditable();
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                editAllChecked = false;
            }
            else
            {
                if (IsDataChaged)
                {
                    string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");
                    CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                    confirmationDialog.Closing += async delegate
                    {
                        if (confirmationDialog.DialogResult == null)
                            return;

                        switch (confirmationDialog.ConfirmationResult)
                        {
                            case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                var err = await dgCreditorGroupPosting.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                break;
                        }
                        editAllChecked = true;
                        dgCreditorGroupPosting.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgCreditorGroupPosting.Readonly = true;
                    dgCreditorGroupPosting.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                }
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgCreditorGroupPosting.HasUnsavedData;
            }
        }
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.CreditorGroupPostingPage2)
                dgCreditorGroupPosting.UpdateItemSource(argument);
        }
    }
}
