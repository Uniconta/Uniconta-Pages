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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

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
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
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
                    AddDockItem(TabControls.DebtorGroupPostingPage2, addParam, Uniconta.ClientTools.Localization.lookup("CustomerPosting"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null) return;
                    string grpPostingHeader = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("CustomerPosting"), selectedItem._Group);
                    object[] EditParam = new object[3];
                    EditParam[0] = selectedItem;
                    EditParam[1] = dgGroupPosting.masterRecord;
                    EditParam[2] = true;
                    AddDockItem(TabControls.DebtorGroupPostingPage2, EditParam, grpPostingHeader, "Edit_16x16");
                    break;
                case "CopyRow":
                    dgGroupPosting.CopyRow();
                    break;
                case "EditAll":
                    if (dgGroupPosting.Visibility == System.Windows.Visibility.Visible)
                        EditAll();
                    break;
                case "CopyRecord":
                    CopyRecord(selectedItem);
                    break;
                case "UndoDelete":
                    dgGroupPosting.UndoDeleteRow();
                    break;
                case "DeleteRow":
                    dgGroupPosting.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyRecord(DCGroupPostingClient selectedItem)
        {
            if (selectedItem == null) return;
            object[] copyParam = new object[3];
            copyParam[0] = selectedItem;
            copyParam[1] = dgGroupPosting.masterRecord;
            copyParam[2] = false;
            string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem._Group);
            AddDockItem(TabControls.DebtorGroupPostingPage2, copyParam, header);
        }

        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgGroupPosting.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgGroupPosting.MakeEditable();
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
                                var err = await dgGroupPosting.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgGroupPosting.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgGroupPosting.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgGroupPosting.Readonly = true;
                    dgGroupPosting.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                }
            }
        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgGroupPosting.HasUnsavedData;
            }
        }
        
        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.DebtorGroupPostingPage2)
                dgGroupPosting.UpdateItemSource(argument);
        }
    }
}
