using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using UnicontaClient.Controls.Dialogs;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EmployeeGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(EmployeeClient); } }
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class Employee : GridBasePage
    {
        public Employee(BaseAPI API) : this(API, string.Empty)
        {
        }

        public Employee(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitializeComponent();
            dgEmployeeGrid.api = this.api;
            dgEmployeeGrid.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgEmployeeGrid);
            ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            if (!api.CompanyEntity.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.EmployeePage2)
                dgEmployeeGrid.UpdateItemSource(argument);
            if (screenName == TabControls.UserNotesPage || screenName == TabControls.UserDocsPage && argument != null)
                dgEmployeeGrid.UpdateItemSource(argument);
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEmployeeGrid.SelectedItem as EmployeeClient;
            switch (ActionType)
            {
                case "EditAll":
                    if (dgEmployeeGrid.Visibility == Visibility.Visible)
                        EditAll();
                    break;
                case "AddRow":
                    AddDockItem(TabControls.EmployeePage2, api, Uniconta.ClientTools.Localization.lookup("Employee"), "Add_16x16");
                    break;
                case "CopyRecord":
                    if (selectedItem != null)
                        CopyRecord(selectedItem);
                    break;
                case "CopyRow":
                    if (selectedItem == null)
                        return;
                    if (copyRowIsEnabled)
                        dgEmployeeGrid.CopyRow();
                    else
                        CopyRecord(selectedItem);
                    break;
                case "AddLine":
                    dgEmployeeGrid.AddRow();
                    break;
                case "EditRow":
                    if (selectedItem != null)
                        AddDockItem(TabControls.EmployeePage2, new object[] { selectedItem, true }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Employee"), selectedItem._Name));
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgEmployeeGrid.DeleteRow();
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgEmployeeGrid.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgEmployeeGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "EmployeeJournalLine":
                    if (selectedItem != null)
                        AddDockItem(TabControls.EmployeeJournalLine, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("EmployeeJournalLine"), selectedItem._Name));
                    break;
                case "Commission":
                    if (selectedItem != null)
                        AddDockItem(TabControls.EmployeeCommissionPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Commission"), selectedItem._Name));
                    break;
                case "Trans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTransactionPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), selectedItem._Name));
                    break;
                case "TimeSheet":
                    if (selectedItem != null)
                        AddDockItem(TabControls.TMJournalLinePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TimeRegistration"), selectedItem._Name));
                    break;
                case "EmpPayrollCategory":
                    if (selectedItem != null)
                        AddDockItem(TabControls.EmpPayrolCategoryPage, selectedItem);
                    break;
                case "TMEmpCalendarSetup":
                    if (selectedItem != null)
                        AddDockItem(TabControls.TMEmpCalendarSetupPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Calendar"), selectedItem._Name));
                    break;
                case "AccountStat":
                    if (selectedItem != null)
                        AddDockItem(TabControls.SalesRepCustomerStatPage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("SalesRepCustomerStat"), selectedItem._Name));
                    break;
                case "ApproveDocument":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DocumentsApprovalPage, new object[] { api, selectedItem }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ApproveDocument"), selectedItem._Name));
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "UndoDelete":
                    dgEmployeeGrid.UndoDeleteRow();
                    break;
                case "BudgetPanningSchedule":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectBudgetPlanningSchedulePage, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BudgetPlanningSchedule"), selectedItem._Name));
                    break;
                case "ProjectEmployee":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectEmployeePage, dgEmployeeGrid.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Projects"), selectedItem._Name));
                    break;
                case "ChartView":
#if SILVERLIGHT
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("SilverlightSupport"), Uniconta.ClientTools.Localization.lookup("Message"), MessageBoxButton.OK);
#else
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTaskPage, selectedItem, string.Format("{0}({1}): {2}", Uniconta.ClientTools.Localization.lookup("Tasks"), Uniconta.ClientTools.Localization.lookup("EnableChart")
                            , selectedItem._Number));
#endif
                    break;

                case "GridView":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTaskGridPage, selectedItem, string.Format("{0}({1}): {2}", Uniconta.ClientTools.Localization.lookup("Tasks"), Uniconta.ClientTools.Localization.lookup("DataGrid")
                            , selectedItem._Number));
                    break;
                case "DebtorBudgetLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorBudgetLinePage, dgEmployeeGrid.syncEntity);
                    break;
                case "Archived":
                    if (selectedItem != null)
                        AddDockItem(TabControls.AttachedVouchers, dgEmployeeGrid.SelectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Vouchers"), selectedItem._Name));
                    break;
                case "EmployeeRegistrationLinePage":
                    if (selectedItem != null)
                        AddDockItem(TabControls.EmployeeRegistrationLinePage, dgEmployeeGrid.SelectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Register"), selectedItem._Name));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void CopyRecord(EmployeeClient selectedItem)
        {
            var emp = Activator.CreateInstance(selectedItem.GetType()) as EmployeeClient;
            CorasauDataGrid.CopyAndClearRowId(selectedItem, emp);
            string header = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), selectedItem._Name);
            AddDockItem(TabControls.EmployeePage2, new object[] { emp, false }, header);
        }

        bool copyRowIsEnabled;
        bool editAllChecked;
        private void EditAll()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var ibase = UtilDisplay.GetMenuCommandByName(rb, "EditAll");
            if (ibase == null)
                return;
            if (dgEmployeeGrid.Readonly)
            {
                api.AllowBackgroundCrud = false;
                dgEmployeeGrid.MakeEditable();
                UserFieldControl.MakeEditable(dgEmployeeGrid);
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("LeaveEditAll");
                ribbonControl.EnableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                copyRowIsEnabled = true;
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
                                var err = await dgEmployeeGrid.SaveData();
                                if (err != 0)
                                {
                                    api.AllowBackgroundCrud = true;
                                    return;
                                }
                                break;
                            case CWConfirmationBox.ConfirmationResultEnum.No:
                                dgEmployeeGrid.CancelChanges();
                                break;
                        }
                        editAllChecked = true;
                        dgEmployeeGrid.Readonly = true;
                        dgEmployeeGrid.tableView.CloseEditor();
                        ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                        ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                        copyRowIsEnabled = false;
                    };
                    confirmationDialog.Show();
                }
                else
                {
                    dgEmployeeGrid.Readonly = true;
                    dgEmployeeGrid.tableView.CloseEditor();
                    ibase.Caption = Uniconta.ClientTools.Localization.lookup("EditAll");
                    ribbonControl.DisableButtons(new string[] { "AddLine", "CopyRow", "DeleteRow", "UndoDelete", "SaveGrid" });
                    copyRowIsEnabled = false;
                }
            }

        }
        public override bool IsDataChaged
        {
            get
            {
                return editAllChecked ? false : dgEmployeeGrid.HasUnsavedData;
            }
        }

        private void HasDocImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var employeeClient = (sender as Image).Tag as EmployeeClient;
            if (employeeClient != null)
                AddDockItem(TabControls.UserDocsPage, dgEmployeeGrid.syncEntity);
        }

        private void HasNoteImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var employeeClient = (sender as Image).Tag as EmployeeClient;
            if (employeeClient != null)
                AddDockItem(TabControls.UserNotesPage, dgEmployeeGrid.syncEntity);
        }

#if !SILVERLIGHT
        private void HasEmailImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var employeeClient = (sender as TextBlock).Tag as EmployeeClient;
            if (employeeClient != null)
            {
                var mail = string.Concat("mailto:", employeeClient._Email);
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo.FileName = mail;
                proc.Start();
            }
        }
#endif
    }
}

