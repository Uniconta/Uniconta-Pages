using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using System.Windows;
using Uniconta.DataModel;
using Uniconta.API.Project;
using Uniconta.ClientTools.Util;
using UnicontaClient.Controls.Dialogs;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectBudgetClient); } }
        public override bool Readonly { get { return false; } }
        public override bool AddRowOnPageDown()
        {
            var selectedItem = (ProjectBudgetClient)this.SelectedItem;
            return (selectedItem?._Project != null);
        }
    }

    public partial class ProjectBudgetPage : GridBasePage
    {
        SQLTableCache<Uniconta.DataModel.EmpPayrollCategory> payrollCache;
        SQLTableCache<Uniconta.ClientTools.DataModel.ProjectClient> projectCache;
        SQLTableCache<Uniconta.DataModel.Employee> employeeCache;
        SQLTableCache<Uniconta.DataModel.ProjectGroup> projGroupCache;
        SQLTableCache<Uniconta.DataModel.ProjectBudgetGroup> budgetGroupCache;
        SQLTableCache<Uniconta.DataModel.PrCategory> prCategoryCache;
        SQLTableCache<Uniconta.DataModel.PrWorkSpace> workspaceCache;
        SQLTableCache<Uniconta.DataModel.InvItem> invItemCache;
        SQLTableCache<Uniconta.DataModel.Debtor> debtorCache;

        public override string NameOfControl { get { return TabControls.ProjectBudgetPage; } }
        public ProjectBudgetPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectBudgetPage(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        public ProjectBudgetPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectBudgetGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            var syncMaster = dgProjectBudgetGrid.masterRecord as Uniconta.DataModel.Project;
            if (syncMaster == null) return;
            string header = null;
            header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectBudget"), syncMaster._Name);
            SetHeader(header);
        }

        private void InitPage(UnicontaBaseEntity master = null)
        {
            SetRibbonControl(localMenu, dgProjectBudgetGrid);
            dgProjectBudgetGrid.api = api;
            if (master == null)
                Project.Visible = true;
            else
            {
                Project.Visible = false;
                dgProjectBudgetGrid.UpdateMaster(master);
            }
            dgProjectBudgetGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            projectCache = api.GetCache<Uniconta.ClientTools.DataModel.ProjectClient>();
            payrollCache = api.GetCache<Uniconta.DataModel.EmpPayrollCategory>();
            prCategoryCache = api.GetCache<Uniconta.DataModel.PrCategory>();
            projGroupCache = api.GetCache<Uniconta.DataModel.ProjectGroup>();
            budgetGroupCache = api.GetCache<Uniconta.DataModel.ProjectBudgetGroup>();
            employeeCache = api.GetCache<Uniconta.DataModel.Employee>();
            debtorCache = api.GetCache<Uniconta.DataModel.Debtor>();
            invItemCache = api.GetCache<Uniconta.DataModel.InvItem>();

            StartLoadCache();
        }

        ProjectBudgetClient GetSelectedItem(out string Header)
        {

            var selectedItem = dgProjectBudgetGrid.SelectedItem as ProjectBudgetClient;
            if (selectedItem == null)
            {
                Header = string.Empty;
                return null;
            }
            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("Budget"), selectedItem.Name);
            Header = header;
            return selectedItem;
        }

        bool CopiedData;
        private void localMenu_OnItemClicked(string ActionType)
        {
            string header = string.Empty;
            ProjectBudgetClient selectedItem = GetSelectedItem(out header);

            switch (ActionType)
            {
                case "AddRow":
                    dgProjectBudgetGrid.AddRow();
                    break;
                case "CopyRow":
                    CopiedData = true;
                    dgProjectBudgetGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid(selectedItem);
                    break;
                case "DeleteRow":
                    if (selectedItem != null && UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("ConfirmDeleteOBJ"), selectedItem._Name), Uniconta.ClientTools.Localization.lookup("Confirmation"),
                        MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        dgProjectBudgetGrid.DeleteRow();
                    break;
                case "BudgetLines":
                    if (selectedItem != null)
                        SaveAndOpenLines(selectedItem);
                    break;
                case "Estimation":
                    if (selectedItem != null)
                        SaveAndOpenTree(selectedItem);
                    break;
                case "BudgetCategorySum":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectBudgetCategorySumPage, selectedItem, header);
                    break;
                case "CreateAnchorBudget":
                    if (dgProjectBudgetGrid.ItemsSource != null)
                        CreateAnchorBudget();
                    break;
                case "Check":
                    AddDockItem(TabControls.TMPlanningCheckPage, null);
                    break;
                case "RefreshGrid":
                    if (dgProjectBudgetGrid.HasUnsavedData)
                        Utility.ShowConfirmationOnRefreshGrid(dgProjectBudgetGrid);
                    else
                        gridRibbon_BaseActions(ActionType);
                    break;
                case "ItemCoverage":
                    if (selectedItem != null)
                        SaveAndOpenItemCoverage(selectedItem);
                    break;
                case "ShowInvoice":
                    if (selectedItem != null)
                        GenerateInvoice(selectedItem);
                    break;
                case "SetCurrent":
                    SetCurrentBudgetGroup();
                    break;
                case "CopyEstimation":
                    if (selectedItem != null)
                        CopyEstimation(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void CopyEstimation(ProjectBudgetClient projectBudget)
        {
            var cwProject = new CWProjects(api, string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("Estimation")));
            cwProject.Closed += async delegate
            {
                if (cwProject.DialogResult == true)
                {
                    var project = cwProject.Project;
                    var newProjectBudgetClient = new ProjectBudgetClient();
                    CorasauDataGrid.CopyAndClearRowId(projectBudget, newProjectBudgetClient);
                    newProjectBudgetClient.Project = project;
                    var result = await api.Insert(newProjectBudgetClient);
                    if (result == ErrorCodes.Succes)
                    {
                        var currentProjectBudgetLns = await api.Query<ProjectBudgetLineClient>(projectBudget);

                        if (currentProjectBudgetLns == null || currentProjectBudgetLns.Length == 0)
                            return;

                        foreach (var line in currentProjectBudgetLns)
                            line.SetMaster(newProjectBudgetClient);

                        api.InsertNoResponse(currentProjectBudgetLns);
                    }
                }
            };
            cwProject.Show();
        }
        async void GenerateInvoice(ProjectBudgetClient projectBudget)
        {
            var Comp = api.CompanyEntity;
            var defaultStorage = Comp._PurchaseLineStorage;
            var order = new DebtorOrderClient();
            order.SetMaster(projectBudget.ProjectRef);

            if (!string.IsNullOrEmpty(order._DCAccount))
            {
                if (Utility.HasControlRights("GenerateInvoice", api.CompanyEntity))
                {
                    var debtor = ClientHelper.GetRef(order.CompanyId, typeof(Debtor), order._DCAccount) as Debtor;
                    if (debtor != null)
                    {
                        var InvoiceAccount = order._InvoiceAccount ?? debtor._InvoiceAccount;
                        if (InvoiceAccount != null)
                            debtor = ClientHelper.GetRef(order.CompanyId, typeof(Debtor), InvoiceAccount) as Debtor;
                        if (debtor._PricesInclVat != order._PricesInclVat)
                        {
                            var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("DebtorAndOrderMix"), Uniconta.ClientTools.Localization.lookup("InclVat")),
                            Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                            if (confirmationMsgBox != MessageBoxResult.OK)
                                return;
                        }
                    }

                    var rows = await api.Query<ProjectBudgetLine>(projectBudget);
                    if (rows == null || rows.Length == 0)
                        return;

                    InvItem item;
                    var orderList = new List<DebtorOrderLineClient>(rows.Length);
                    int lineNo = 0;
                    foreach (var line in rows)
                    {
                        if (line._Item != null)
                        {
                            item = (InvItem)invItemCache.Get(line._Item);
                            var orderLine = new DebtorOrderLineClient();
                            orderLine._LineNumber = ++lineNo;
                            orderLine._Item = line._Item;
                            orderLine._Variant1 = line._Variant1;
                            orderLine._Variant2 = line._Variant2;
                            orderLine._Variant3 = line._Variant3;
                            orderLine._Variant4 = line._Variant4;
                            orderLine._Variant5 = line._Variant5;
                            orderLine._Warehouse = item._Warehouse;
                            orderLine._Location = item._Location;
                            orderLine._Qty = line._QtyPurchased;
                            orderLine._QtyNow = orderLine._Qty;
                            orderLine._Storage = defaultStorage;
                            orderLine._DiscountPct = debtor._LineDiscountPct;
                            orderLine._Project = projectBudget.Project;
                            orderLine._PrCategory = line._PrCategory ?? item._PrCategory;
                            orderLine._WorkSpace = line._WorkSpace;
                            orderLine._Task = line._Task;
                            orderLine.SetMaster(order);
                            TableField.SetUserFieldsFromRecord(orderLine, item);
                            orderList.Add(orderLine);
                        }
                    }

                    var dc = order.Debtor;
                    if (dc == null || !Utility.IsExecuteWithBlockedAccount(dc))
                        return;
                    if (!api.CompanyEntity.SameCurrency(order._Currency, dc._Currency))
                    {
                        var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}.\n{1}", string.Format(Uniconta.ClientTools.Localization.lookup("CurrencyMismatch"), dc.Currency, order.Currency),
                        Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Confirmation"), MessageBoxButton.OKCancel);
                        if (confirmationMsgBox != MessageBoxResult.OK)
                            return;
                    }

                    if (api.CompanyEntity._InvoiceUseQtyNow)
                    {
                        foreach (var rec in orderList)
                            rec._QtyNow = rec._Qty;
                    }
                    ShowProformaInvoice(order, orderList);
                    return;
                }
                else
                    UtilDisplay.ShowControlAccessMsg("GenerateInvoice");
            }
        }

        async private void ShowProformaInvoice(DebtorOrderClient order, IEnumerable<DCOrderLineClient> orderLines)
        {
            var invoicePostingResult = SetupInvoicePostingPrintGenerator(order, orderLines, DateTime.Now, true, true, false, false, 0, false, false, false, null, false, null, false);

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
            var result = await invoicePostingResult.Execute();
            busyIndicator.IsBusy = false;

            if (!result)
            {
                var errorMessage = StringBuilderReuse.Create();
                var res = invoicePostingResult.PostingResult.ledgerRes;
                if (res.Err != 0)
                    errorMessage.Append(Uniconta.ClientTools.Localization.lookup("Error")).Append(": ");
                errorMessage.AppendLine(Uniconta.ClientTools.Localization.lookup(res.Err.ToString()));
                UnicontaMessageBox.Show(errorMessage.ToStringAndRelease(), Uniconta.ClientTools.Localization.lookup("Error"));
            }
        }

        private InvoicePostingPrintGenerator SetupInvoicePostingPrintGenerator(DebtorOrderClient dbOrder, IEnumerable<DCOrderLineClient> lines, DateTime generateDate, bool isSimulation, bool showInvoice, bool postOnlyDelivered,
            bool isQuickPrint, int pagePrintCount, bool invoiceSendByEmail, bool invoiceSendByOutlook, bool sendOnlyToEmail, string sendOnlyToEmailList, bool OIOUBLgenerate, IEnumerable<TableAddOnData> attachedDocs, bool returnAsPdf)
        {
            var invoicePostingResult = new InvoicePostingPrintGenerator(api, this);
            invoicePostingResult.SetUpInvoicePosting(dbOrder, lines, CompanyLayoutType.Invoice, generateDate, null, isSimulation, showInvoice, postOnlyDelivered, isQuickPrint, pagePrintCount,
                invoiceSendByEmail, !isSimulation && invoiceSendByOutlook, sendOnlyToEmail, sendOnlyToEmailList, OIOUBLgenerate, null, returnAsPdf);
            return invoicePostingResult;
        }

        #region Create Anchor Budget
        async private void CreateAnchorBudget()
        {
            var budgetLst = dgProjectBudgetGrid.GetVisibleRows() as IList<ProjectBudgetClient>;
            BudgetAPI budgetApi = new BudgetAPI(api);
            var result = await budgetApi.CreateAnchorBudget(budgetLst);

            if (result != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(result);
            else
                UnicontaMessageBox.Show(string.Concat(Uniconta.ClientTools.Localization.lookup("AnchorBudget"), " ", Uniconta.ClientTools.Localization.lookup("Updated").ToLower()),
              Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);

        }
        #endregion
        private void SetCurrentBudgetGroup()
        {
            var cwCreateAnchorBjt = new CwCreateAnchorBudget(api);
            cwCreateAnchorBjt.DialogTableId = 2000000103;
            cwCreateAnchorBjt.Closed += delegate
            {
                if (cwCreateAnchorBjt.DialogResult == true)
                {
                    var selectedBudgetGroup = CwCreateAnchorBudget.Group;
                    var visibleRows = dgProjectBudgetGrid.VisibleItems.Cast<ProjectBudgetClient>();
                    foreach (var visibleRow in visibleRows)
                    {
                        bool isupdated = false;
                        if (visibleRow.Group == selectedBudgetGroup)
                        {
                            if (visibleRow._Current == false)
                            {
                                dgProjectBudgetGrid.SetLoadedRow(visibleRow);
                                visibleRow.Current = true;
                                isupdated = true;
                            }
                        }
                        else if (visibleRow._Current == true)
                        {
                            dgProjectBudgetGrid.SetLoadedRow(visibleRow);
                            visibleRow.Current = false;
                            isupdated = true;
                        }
                        if (isupdated)
                            dgProjectBudgetGrid.SetModifiedRow(visibleRow);
                    }
                }
            };
            cwCreateAnchorBjt.Show();
        }
        async void SaveAndOpenLines(ProjectBudgetClient selectedItem)
        {
            if (dgProjectBudgetGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.ProjectBudgetLinePage, dgProjectBudgetGrid.syncEntity, string.Format("{0} {1}: {2}", Uniconta.ClientTools.Localization.lookup("Budget"), Uniconta.ClientTools.Localization.lookup("Lines"), selectedItem._Project));
        }
        async void SaveAndOpenTree(ProjectBudgetClient selectedItem)
        {
            if (dgProjectBudgetGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.ProjectBudgetEstimation, dgProjectBudgetGrid.syncEntity, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("Estimation"), selectedItem._Project));
        }
        async void SaveAndOpenItemCoverage(ProjectBudgetClient selectedItem)
        {
            if (dgProjectBudgetGrid.HasUnsavedData)
            {
                var tsk = saveGrid(selectedItem);
                if (tsk != null && (CopiedData || selectedItem.RowId == 0))
                    await tsk;
            }
            if (selectedItem.RowId != 0)
                AddDockItem(TabControls.ProjectBudgetItemCoverage, dgProjectBudgetGrid.syncEntity, string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("ItemCoverage"), selectedItem._Project));
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;

            if (projectCache == null)
                projectCache = await api.LoadCache<Uniconta.ClientTools.DataModel.ProjectClient>().ConfigureAwait(false);
            if (payrollCache == null)
                payrollCache = await api.LoadCache<Uniconta.DataModel.EmpPayrollCategory>().ConfigureAwait(false);
            if (prCategoryCache == null)
                prCategoryCache = await api.LoadCache<Uniconta.DataModel.PrCategory>().ConfigureAwait(false);
            if (projGroupCache == null)
                projGroupCache = await api.LoadCache<Uniconta.DataModel.ProjectGroup>().ConfigureAwait(false);
            if (employeeCache == null)
                employeeCache = await api.LoadCache<Uniconta.DataModel.Employee>().ConfigureAwait(false);
            if (budgetGroupCache == null)
                budgetGroupCache = await api.LoadCache<Uniconta.DataModel.ProjectBudgetGroup>().ConfigureAwait(false);
            if (workspaceCache == null)
                workspaceCache = await api.LoadCache<Uniconta.DataModel.PrWorkSpace>().ConfigureAwait(false);
            if (invItemCache == null)
                invItemCache = await api.LoadCache<Uniconta.DataModel.InvItem>().ConfigureAwait(false);
            if (debtorCache == null)
                debtorCache = await api.LoadCache<Uniconta.DataModel.Debtor>().ConfigureAwait(false);

            LoadType(new Type[] { typeof(Uniconta.DataModel.ProjectTask) });

        }
    }
}


