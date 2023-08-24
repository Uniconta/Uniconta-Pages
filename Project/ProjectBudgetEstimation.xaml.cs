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
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using System.Windows.Threading;
using System.Threading;
using UnicontaClient.Utilities;
using System.Windows;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using Uniconta.ClientTools.Util;
using System.ComponentModel.DataAnnotations;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Reports.UserDesigner.Native;
using DevExpress.Mvvm.Xpf;
using DevExpress.CodeParser;
using DevExpress.XtraRichEdit.Model;
using DevExpress.Xpf.Docking.VisualElements;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetEstimationGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectBudgetLineLocal); } }
        public override IComparer GridSorting { get { return new ProjectBudgetLineSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return true; } }
        public override bool ShowTreeListView => true;
        public override TreeListView SetTreeListViewFromPage
        {
            get
            {
                var tv = base.SetTreeListViewFromPage;
                tv.KeyFieldName = "Id";
                tv.ShowIndicator = false;
                tv.ParentFieldName = "MasterId";
                tv.TreeDerivationMode = TreeDerivationMode.Selfreference;
                tv.ShowTotalSummary = false;
               // tv.ShowNodeFooters = true;
                var sumQty = new TreeListSummaryItem() { DisplayFormat = "N2", FieldName = "Qty", SummaryType = SummaryItemType.Custom };
                var sumCost = new TreeListSummaryItem() { DisplayFormat = "N2", FieldName = "Cost", SummaryType = SummaryItemType.Custom };
                var sumSales = new TreeListSummaryItem() { DisplayFormat = "N2", FieldName = "Sales", SummaryType = SummaryItemType.Custom };
                tv.NodeSummary.Add(sumQty);
                tv.NodeSummary.Add(sumCost);
                tv.NodeSummary.Add(sumSales);
                tv.AllowRecursiveNodeSummaryCalculation = true;
                tv.CustomSummary += Tv_CustomSummary;

                var formatConditionHeader = new FormatCondition()
                {
                    ValueRule = DevExpress.Xpf.Core.ConditionalFormatting.ConditionRule.Equal,
                    FieldName = "Header",
                    Value1 = true,
                    Format = new DevExpress.Xpf.Core.ConditionalFormatting.Format()
                    {
                        Foreground = new SolidColorBrush(Colors.Blue),
                        Background = new SolidColorBrush(Colors.LightBlue)
                    },
                    // PredefinedFormatName = "LightRedFillWithDarkRedText",
                    ApplyToRow = true
                };
                var formatConditionDisable = new FormatCondition()
                {
                    ValueRule = DevExpress.Xpf.Core.ConditionalFormatting.ConditionRule.Equal,
                    FieldName = "Disable",
                    Value1 = true,
                    Format = new DevExpress.Xpf.Core.ConditionalFormatting.Format()
                    {
                        Foreground = new SolidColorBrush(Colors.Red),
                        Background = new SolidColorBrush(Color.FromRgb(255, 165, 165))
                    },
                    ApplyToRow = true
                };
                tv.FormatConditions.Add(formatConditionHeader);
                tv.FormatConditions.Add(formatConditionDisable);
                return tv;
            }
        }

       // double sumCost, sumSales, sumQty;
        private void Tv_CustomSummary(object sender, DevExpress.Xpf.Grid.TreeList.TreeListCustomSummaryEventArgs e)
        {
            var sumItem = e.SummaryItem;
            if (e.IsNodeSummary)
            {
                if (e.SummaryProcess == CustomSummaryProcess.Start)
                {
                    e.TotalValue = 0;
                }
                if (e.SummaryProcess == CustomSummaryProcess.Calculate)
                {
                    var row = e.Node.Content as ProjectBudgetLineLocal;
                    if (!row.Disable && !row.Header)
                    {
                        e.TotalValue = Convert.ToDouble(e.TotalValue) + (double)e.FieldValue;
                    }
                }
            }
            //else if (e.IsTotalSummary)
            //{
            //    var fieldName = sumItem.FieldName;
            //    switch (e.SummaryProcess)
            //    {
            //        case CustomSummaryProcess.Start:
            //            sumCost = sumSales = sumQty = 0d;
            //            break;
            //        case CustomSummaryProcess.Calculate:
            //            var row = e.Node.Content as ProjectBudgetLineLocal;
            //            if (!row.Disable && !row.Header)
            //            {
            //                sumSales += row.Sales;
            //                sumCost += row.Cost;
            //                sumQty += row.Qty;
            //            }
            //            break;
            //        case CustomSummaryProcess.Finalize:
            //            if (fieldName == "MarginRatio" && sumSales > 0)
            //            {
            //                e.TotalValue = Math.Round((sumSales - sumCost) * 100d / sumSales, 2);
            //            }
            //            if (fieldName == "Cost")
            //            {
            //                e.TotalValue = sumCost;
            //            }
            //            if (fieldName == "Sales")
            //            {
            //                e.TotalValue = sumSales;
            //            }
            //            if (fieldName == "Qty")
            //            {
            //                e.TotalValue = sumQty;
            //            }
            //            break;
            //    }
            //}
        }

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (ProjectBudgetLineLocal)this.SelectedItem;
            if (selectedItem == null || selectedItem._Project == null || (selectedItem._Qty == 0d && selectedItem._SalesPrice == 0d && selectedItem._CostPrice == 0d))
                return false;
            return true;
        }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var newRow = (ProjectBudgetLineLocal)dataEntity;
            var lst = (IList)this.ItemsSource;
            if (lst == null || lst.Count == 0)
            {
                newRow._Date = BasePage.GetSystemDefaultDate().Date;
            }
            else
            {
                ProjectBudgetLineLocal last = null;
                ProjectBudgetLineLocal Cur = null;
                int n = -1;
                DateTime LastDateTime = DateTime.MinValue;
                var castItem = lst.Cast<ProjectBudgetLineLocal>();
                foreach (var journalLine in castItem)
                {
                    if (journalLine._Date != DateTime.MinValue && Cur == null)
                        LastDateTime = journalLine._Date;
                    n++;
                    if (n == selectedIndex)
                        Cur = journalLine;
                    last = journalLine;
                }
                if (Cur == null)
                    Cur = last;

                newRow._Date = LastDateTime != DateTime.MinValue ? LastDateTime : BasePage.GetSystemDefaultDate().Date;
                newRow._Project = last._Project;
                newRow._PrCategory = last._PrCategory;
                newRow.PrCategorySource = last.PrCategorySource;
            }
        }
    }

    public partial class ProjectBudgetEstimationPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectBudgetEstimation; } }
        SQLCache ProjectCache, EmployeeCache, ItemCache, PayrollCache;

        UnicontaBaseEntity master;
        Uniconta.API.DebtorCreditor.FindPrices PriceLookup;
        public ProjectBudgetEstimationPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectBudgetEstimationPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            // dgProjectBudgetLinePageGrid.UpdateMaster(args);
            SetHeader();
            this.master = args;
            InitQuery();
        }
        public override async Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            dgProjectBudgetLinePageGrid.ItemsSource = null;
            var lines = await api.Query<ProjectBudgetLineLocal>(new UnicontaBaseEntity[] { master }, null);
            if (lines?.Length > 0)
            {
                Array.Sort(lines, new ProjectBudgetLineSort(lines));
                var maxId = lines.Select(s => s.Id).Max();
                foreach (var line in lines)
                {
                    if (line.Id == 0)
                        line.Id = ++maxId;
                }
                dgProjectBudgetLinePageGrid.MaxId = maxId;
                dgProjectBudgetLinePageGrid.SetSource(lines);
            }
            dgProjectBudgetLinePageGrid.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgProjectBudgetLinePageGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Estimation"), key);
            SetHeader(header);
            if (PriceLookup != null)
            {
                var master = dgProjectBudgetLinePageGrid.masterRecord;
                var masterProject = (master as Uniconta.DataModel.Project);
                if (masterProject == null)
                {
                    var budget = (master as ProjectBudget);
                    if (budget != null)
                        masterProject = (Uniconta.DataModel.Project)ProjectCache.Get(budget._Project);
                }
                if (masterProject != null)
                    PriceLookup.OrderChanged(masterProject);
            }
        }

        void InitPage(UnicontaBaseEntity master)
        {
            this.master = master;
            ((TreeListView)dgProjectBudgetLinePageGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgProjectBudgetLinePageGrid;
            dgProjectBudgetLinePageGrid.api = api;
            dgProjectBudgetLinePageGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgProjectBudgetLinePageGrid);
            dgProjectBudgetLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            dgProjectBudgetLinePageGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgProjectBudgetLinePageGrid.Loaded += DgProjectBudgetLinePageGrid_Loaded;
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api).ConfigureAwait(false);
            EmployeeCache = Comp.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Employee), api).ConfigureAwait(false);
            if (Comp.Payroll)
                PayrollCache = Comp.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory), api).ConfigureAwait(false);
            ItemCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);
            var Debtors = Comp.GetCache(typeof(Uniconta.DataModel.Debtor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Debtor), api).ConfigureAwait(false);
            LoadType(typeof(Uniconta.DataModel.PrCategory));

            var master = dgProjectBudgetLinePageGrid.masterRecord;
            var masterProject = (master as Uniconta.DataModel.Project);
            if (masterProject == null)
            {
                var budget = (master as ProjectBudget);
                if (budget != null)
                    masterProject = (Uniconta.DataModel.Project)ProjectCache.Get(budget._Project);
            }
            if (masterProject != null)
                PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(masterProject, api);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
            if (dgProjectBudgetLinePageGrid.masterRecord is Uniconta.DataModel.ProjectBudget)
            {
                this.Project.Visible = false;
                this.ProjectName.Visible = false;
            }
            var Comp = api.CompanyEntity;
            if (!Comp.ProjectTask)
            {
                Task.Visible = false;
                Task.ShowInColumnChooser = false;
            }
            else
                Task.ShowInColumnChooser = true;
            if (!Comp.Payroll)
            {
                PayrollCategory.Visible = false;
                PayrollCategory.ShowInColumnChooser = false;
            }
            else
                PayrollCategory.ShowInColumnChooser = true;
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            ProjectBudgetLineLocal oldselectedItem = e.OldItem as ProjectBudgetLineLocal;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            ProjectBudgetLineLocal selectedItem = e.NewItem as ProjectBudgetLineLocal;
            if (selectedItem != null)
            {
                selectedItem.InsidePropChange = false;
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
            ResetSum();
        }

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (ProjectBudgetLineLocal)sender;
            switch (e.PropertyName)
            {
                case "Item":
                    if (!rec.InsidePropChange)
                    {
                        rec.InsidePropChange = true;
                        SetItem(rec);
                        rec.InsidePropChange = false;
                    }
                    break;
                case "Employee":
                    if (rec._Employee != null)
                    {
                        var emp = (Uniconta.DataModel.Employee)EmployeeCache?.Get(rec._Employee);
                        if (emp?._PayrollCategory != null)
                            rec.PayrollCategory = emp._PayrollCategory;
                        else if (!rec.InsidePropChange)
                        {
                            rec.InsidePropChange = true;
                            PayrollCat(rec, true);
                            rec.InsidePropChange = false;
                        }
                    }
                    break;
                case "PayrollCategory":
                    if (rec._Employee != null && rec._PayrollCategory != null)
                    {
                        if (!rec.InsidePropChange)
                        {
                            rec.InsidePropChange = true;
                            PayrollCat(rec, true);
                            rec.InsidePropChange = false;
                        }
                    }
                    break;
                case "TimeFrom":
                case "TimeTo":
                    if (rec._ToTime >= rec._FromTime)
                        rec.Qty = (rec._ToTime - rec._FromTime) / 60d;
                    break;

                case "Qty":
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);
                    if (rec._Qty != (rec._ToTime - rec._FromTime) / 60d)
                    {
                        rec._FromTime = 0;
                        rec._ToTime = 0;
                    }
                    ResetSum();
                    break;
                case "CostPrice":
                case "SalesPrice":
                    if (rec._Qty == 0d)
                        rec.Qty = 1d;
                    ResetSum();
                    break;
                case "Disable":
                    var node = dgProjectBudgetLinePageGrid.treeListView.GetNodeByContent(sender);
                    DisableChildNodes(node);
                    ResetSum();
                    break;
                case "Header":
                    if (rec._Header)
                    {
                        var prCategoryCache = api.CompanyEntity.GetCache(typeof(Uniconta.DataModel.PrCategory));
                        var category = ((PrCategory[])prCategoryCache.GetRecords)?.Where(p => p._Default == true).FirstOrDefault();
                        if (category != null)
                            rec.PrCategory = category.KeyStr;
                    }
                    ResetSum();
                    break;
            }

        }
        void DisableChildNodes(TreeListNode parentNode)
        {
            var nodes = parentNode?.Nodes;
            if (nodes?.Count > 0)
            {
                var parentRow = parentNode.Content as ProjectBudgetLineLocal;
                foreach (var node in nodes)
                {
                    var child = node.Content;
                    (child as ProjectBudgetLineLocal).Disable = parentRow.Disable;
                    DisableChildNodes(node);
                }
            }
        }
        async void PayrollCat(ProjectBudgetLineLocal rec, bool AddItem)
        {
            double costPrice = 0, salesPrice = 0;
            var emp = (Uniconta.DataModel.Employee)EmployeeCache?.Get(rec._Employee);
            if (emp != null)
            {
                costPrice = emp._CostPrice;
                salesPrice = emp._SalesPrice;
            }

            var pay = (Uniconta.DataModel.EmpPayrollCategory)PayrollCache?.Get(rec._PayrollCategory);
            if (pay != null)
            {
                if (pay._Unit != 0 && rec._Unit != pay._Unit)
                {
                    rec._Unit = pay._Unit;
                    rec.NotifyPropertyChanged("Unit");
                }

                if (pay._PrCategory != null)
                    rec.PrCategory = pay._PrCategory;

                if (pay._Rate != 0)
                    costPrice = pay._Rate;
                if (pay._SalesPrice != 0)
                    salesPrice = pay._SalesPrice;

                string Item = pay._Item;
                if (pay._Dim1 != null) rec.Dimension1 = pay._Dim1;
                if (pay._Dim2 != null) rec.Dimension2 = pay._Dim2;
                if (pay._Dim3 != null) rec.Dimension3 = pay._Dim3;
                if (pay._Dim4 != null) rec.Dimension4 = pay._Dim4;
                if (pay._Dim5 != null) rec.Dimension5 = pay._Dim5;

                if (emp != null)
                {
                    Uniconta.DataModel.EmpPayrollCategoryEmployee found = null;
                    var Rates = pay.Rates ?? await pay.LoadRates(api);
                    foreach (var rate in Rates)
                    {
                        if (rate._ValidFrom != DateTime.MinValue && rate._ValidFrom > rec._Date)
                            continue;
                        if (rate._ValidTo != DateTime.MinValue && rate._ValidTo < rec._Date)
                            continue;
                        if (rate._Employee != emp._Number)
                            continue;
                        if (rate._Project != null)
                        {
                            if (rate._Project == rec._Project)
                            {
                                found = rate;
                                break;
                            }
                        }
                        else if (found == null)
                            found = rate;
                    }

                    if (found != null)
                    {
                        if (found._CostPrice != 0d)
                            costPrice = found._CostPrice;
                        else if (found._Rate != 0d)
                            costPrice = found._Rate;
                        if (found._SalesPrice != 0d)
                            salesPrice = found._SalesPrice;
                        if (found._Item != null)
                            Item = found._Item;

                        if (found._Dim1 != null) rec.Dimension1 = found._Dim1;
                        if (found._Dim2 != null) rec.Dimension2 = found._Dim2;
                        if (found._Dim3 != null) rec.Dimension3 = found._Dim3;
                        if (found._Dim4 != null) rec.Dimension4 = found._Dim4;
                        if (found._Dim5 != null) rec.Dimension5 = found._Dim5;
                    }
                }

                if (AddItem && Item != null)
                    rec.Item = Item;
            }
            if (costPrice != 0d)
            {
                if (rec._Qty == 0)
                    rec.Qty = 1d;
                rec.CostPrice = costPrice;
            }
            if (salesPrice != 0d)
            {
                if (rec._Qty == 0)
                    rec.Qty = 1d;
                rec.SalesPrice = salesPrice;
            }
        }

        void SetItem(ProjectBudgetLineLocal rec)
        {
            var item = (InvItem)ItemCache.Get(rec._Item);
            if (item == null)
                return;

            if (item._CostPrice != 0d)
            {
                if (rec._Qty == 0)
                    rec.Qty = 1d;
                rec.CostPrice = item._CostPrice;
            }

            var _priceLookup = this.PriceLookup;
            this.PriceLookup = null; // avoid that we call priceupdated in property change on Qty
            _priceLookup?.SetPriceFromItem(rec, item);
            this.PriceLookup = _priceLookup;

            if (item._Dim1 != null) rec.Dimension1 = item._Dim1;
            if (item._Dim2 != null) rec.Dimension2 = item._Dim2;
            if (item._Dim3 != null) rec.Dimension3 = item._Dim3;
            if (item._Dim4 != null) rec.Dimension4 = item._Dim4;
            if (item._Dim5 != null) rec.Dimension5 = item._Dim5;

            if (item._PrCategory != null)
                rec.PrCategory = item._PrCategory;
            if (item._PayrollCategory != null)
            {
                rec.PayrollCategory = item._PayrollCategory;
                PayrollCat(rec, false);
            }
            if (item._Unit != 0 && rec._Unit != item._Unit)
            {
                rec._Unit = item._Unit;
                rec.NotifyPropertyChanged("Unit");
            }

            globalEvents.NotifyRefreshViewer(NameOfControl, item);
        }

        async void setTask(ProjectClient project, ProjectBudgetLineLocal rec)
        {
            if (api.CompanyEntity.ProjectTask)
            {
                if (project != null)
                    rec.taskSource = project.Tasks ?? await project.LoadTasks(api);
                else
                {
                    rec.taskSource = null;
                    rec.Task = null;
                }
                rec.NotifyPropertyChanged("TaskSource");
            }
        }

        async private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectBudgetLinePageGrid.SelectedItem as ProjectBudgetLineLocal;
            switch (ActionType)
            {
                case "Preview":
                    ProjectEstimateReportPreview();
                    break;
                case "AddRow":
                    dgProjectBudgetLinePageGrid.AddRow();
                    break;
                case "CopyRow":
                    dgProjectBudgetLinePageGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgProjectBudgetLinePageGrid.DeleteRow();
                    break;
                case "RefreshGrid":
                    if (dgProjectBudgetLinePageGrid.HasUnsavedData)
                    {
                        string message = Uniconta.ClientTools.Localization.lookup("SaveChangesPrompt");

                        CWConfirmationBox confirmationDialog = new CWConfirmationBox(message);
                        confirmationDialog.Closing += delegate
                        {
                            switch (confirmationDialog.ConfirmationResult)
                            {
                                case CWConfirmationBox.ConfirmationResultEnum.Yes:
                                    saveGrid();
                                    break;
                                case CWConfirmationBox.ConfirmationResultEnum.No:
                                    InitQuery();
                                    break;
                            }
                        };
                        confirmationDialog.Show();
                    }
                    break;
                case "ExpandAll":
                    dgProjectBudgetLinePageGrid.treeListView.ExpandAllNodes();
                    break;
                case "CollapseAll":
                    dgProjectBudgetLinePageGrid.treeListView.CollapseAllNodes();
                    break;
                case "UnfoldBOM":
                    if (selectedItem != null)
                        UnfoldBOM(selectedItem);
                    break;
                case "AddItems":
                    if (ItemCache == null)
                        return;
                    object[] paramArray = new object[3] { new InvItemSalesCacheFilter(ItemCache), dgProjectBudgetLinePageGrid.TableTypeUser, this.master };
                    AddDockItem(TabControls.AddMultiInventoryItemsForProject, paramArray, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        List<ProjectBudgetLineLocal> reportSource;

        private void GenerateProjectEstimateReportSource(TreeListNodeCollection nodes)
        {
            foreach (var node in nodes)
            {
                reportSource.Add(node.Content as ProjectBudgetLineLocal);

                if (node.HasChildren && node.IsExpanded)
                    GenerateProjectEstimateReportSource(node.Nodes);
            }
        }

        async private void ProjectEstimateReportPreview()
        {
            reportSource = new List<ProjectBudgetLineLocal>();
            GenerateProjectEstimateReportSource(dgProjectBudgetLinePageGrid.treeListView.Nodes);

            if (reportSource.Count == 0)
                return;

            var project = reportSource.First().Project;
            if (string.IsNullOrEmpty(project))
                return;

            var projectCache = api.GetCache(typeof(ProjectClient)) ?? api.LoadCache(typeof(ProjectClient)).GetAwaiter().GetResult();
            var prj = projectCache.Get(project) as ProjectClient;
            var deb = prj._DCAccount;

            if (string.IsNullOrEmpty(deb))
                return;

            var debtCache = api.GetCache(typeof(DebtorClient)) ?? api.LoadCache(typeof(DebtorClient)).GetAwaiter().GetResult();
            var db = debtCache.Get(deb) as DebtorClient;
            var companyClient = UtilCommon.GetCompanyClientUserInstance(api.CompanyEntity);
            var getLogo = await UtilCommon.GetLogo(api);

            var projectEstimateObj = Activator.CreateInstance(typeof(StandardProjectEstimateClient)) as UserReportDevExpressClient;
            if (projectEstimateObj == null)
                return;

            var repSrc = new ProjectBudgetEstimateStandardReportClient(companyClient, getLogo, Uniconta.ClientTools.Localization.lookup("ProjectEstimate"), prj, reportSource.ToArray(), db);
            var projectEstimateReports = await api.Query<UserReportDevExpressClient>(projectEstimateObj, null, null);
            string reportName = null;
            if (projectEstimateReports != null && projectEstimateReports.Length > 0)
            {
                var reportNames = projectEstimateReports.Select(p => p.Name).ToArray();
                var cwComboBox = new CWComboBoxSelector(string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("ProjectEstimate"),
                Uniconta.ClientTools.Localization.lookup("Report")), reportNames);
                cwComboBox.Closed += async delegate
                {
                    if (cwComboBox.DialogResult == true)
                    {
                        reportName = reportNames[cwComboBox.SelectedItemIndex];
                        await ShowPreview(new StandardPrintReport(api, new[] { repSrc }, 99, reportName), prj.Name);
                    }
                };
                cwComboBox.Show();
            }
            else
                await ShowPreview(new StandardPrintReport(api, new[] { repSrc }, 99), prj.Name);
        }

        async private Task ShowPreview(IPrintReport report, string projectName)
        {
            await report.InitializePrint();
            var dockname = string.Format("{0}: {1}-{2}", Uniconta.ClientTools.Localization.lookup("ShowPrint"), Uniconta.ClientTools.Localization.lookup("Estimation"), projectName);
            AddDockItem(TabControls.StandardPrintReportPage, new object[] { new IPrintReport[] { report }, Uniconta.ClientTools.Localization.lookup("ProjectEstimate") }, dockname);
        }

        async void UnfoldBOM(ProjectBudgetLineLocal selectedItem)
        {
            var items = this.ItemCache;
            var item = (InvItem)items.Get(selectedItem._Item);
            if (item == null || item._ItemType < (byte)Uniconta.DataModel.ItemType.BOM)
                return;

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            var list = await api.Query<InvBOM>(selectedItem);
            if (list != null && list.Length > 0)
            {
                Array.Sort(list, new InvBOMSort());
                var lst = new List<UnicontaBaseEntity>(list.Length);
                foreach (var bom in list)
                {
                    item = (InvItem)items.Get(bom._ItemPart);
                    if (item == null)
                        continue;
                    var projBudgetLine = new ProjectBudgetLineLocal();
                    projBudgetLine._Item = bom._ItemPart;
                    projBudgetLine._Project = selectedItem._Project;
                    projBudgetLine._Date = selectedItem._Date;
                    projBudgetLine._Dim1 = selectedItem._Dim1;
                    projBudgetLine._Dim2 = selectedItem._Dim2;
                    projBudgetLine._Dim3 = selectedItem._Dim3;
                    projBudgetLine._Dim4 = selectedItem._Dim4;
                    projBudgetLine._Dim5 = selectedItem._Dim5;
                    projBudgetLine._Employee = selectedItem._Employee;
                    projBudgetLine._Task = selectedItem._Task;
                    projBudgetLine._Variant1 = bom._Variant1;
                    projBudgetLine._Variant2 = bom._Variant2;
                    projBudgetLine._Variant3 = bom._Variant3;
                    projBudgetLine._Variant4 = bom._Variant4;
                    projBudgetLine._Variant5 = bom._Variant5;
                    projBudgetLine._PrCategory = item._PrCategory ?? selectedItem._PrCategory;
                    projBudgetLine._PayrollCategory = item._PayrollCategory ?? selectedItem._PayrollCategory;
                    projBudgetLine._Warehouse = bom._Warehouse ?? item._Warehouse ?? selectedItem._Warehouse;
                    projBudgetLine._Location = bom._Location ?? item._Location ?? selectedItem._Location;
                    projBudgetLine._CostPrice = item._CostPrice;
                    projBudgetLine._Qty = Math.Round(bom.GetBOMQty(selectedItem._Qty), item._Decimals);
                    SetItem(projBudgetLine);
                    lst.Add(projBudgetLine);
                }
                dgProjectBudgetLinePageGrid.PasteRows(lst);
                this.DataChanged = true;
            }
            busyIndicator.IsBusy = false;
        }

        bool DataChanged;
        public override bool IsDataChaged
        {
            get
            {
                if (DataChanged)
                    return true;
                return base.IsDataChaged;
            }
        }
        private void Task_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgProjectBudgetLinePageGrid.SelectedItem as ProjectBudgetLineLocal;
            if (selectedItem?._Project != null)
            {
                var selected = (ProjectClient)ProjectCache.Get(selectedItem._Project);
                setTask(selected, selectedItem);
            }
        }
        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgProjectBudgetLinePageGrid.Filter(propValuePair);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }


        private void ResetSum()
        {
            ResetSum(dgProjectBudgetLinePageGrid);
        }

        public static void ResetSum(CorasauDataGrid dataGrid)
        {
            var tv = dataGrid.treeListView;
            tv.UpdateNodeSummary();
            TreeListNodeIterator nodeIterator = new TreeListNodeIterator(tv.Nodes, false);
            var qtySummary = tv.NodeSummary[0];
            var costSummary = tv.NodeSummary[1];
            var salesSummary = tv.NodeSummary[2];
            while (nodeIterator.MoveNext())
            {
                var node = nodeIterator.Current;
                if (node.Nodes.Count == 0)
                    continue;
                var row = node.Content as ProjectBudgetLineLocal;
                if (row?.Header == true)
                {
                    //  row.Qty = Convert.ToDouble(tv.GetNodeSummaryValue(node, qtySummary));
                    row.Cost = Convert.ToDouble(tv.GetNodeSummaryValue(node, costSummary));
                    row.Sales = Convert.ToDouble(tv.GetNodeSummaryValue(node, salesSummary));
                }
            }
        }

        bool firstLoad;
        private void DgProjectBudgetLinePageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            ResetSum();
            if (!firstLoad)
            {
                dgProjectBudgetLinePageGrid.treeListView.ExpandAllNodes();
                firstLoad = true;
            }
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            var param = argument as object[];
            if (param != null)
            {
                if (screenName == TabControls.AddMultiInventoryItemsForProject)
                {
                    var budgetId = (int)NumberConvert.ToInt(Convert.ToString(param[1]));
                    if (this.master is ProjectBudget projectBudget && projectBudget.RowId == budgetId)
                    {
                        var selectedItem = dgProjectBudgetLinePageGrid.SelectedItem as ProjectBudgetLineLocal;
                        if (dgProjectBudgetLinePageGrid.isDefaultFirstRow)
                        {
                            dgProjectBudgetLinePageGrid.DeleteRow();
                            dgProjectBudgetLinePageGrid.isDefaultFirstRow = false;
                        }
                        var lst = param[0] as List<UnicontaBaseEntity>;
                        if (dgProjectBudgetLinePageGrid.PasteRows(lst))
                        {
                            foreach (var r in lst)
                            {
                                var rec = (ProjectBudgetLineLocal)r;
                                if (selectedItem != null)
                                {
                                    rec.Date = selectedItem._Date;
                                    rec.Project = selectedItem._Project;
                                    rec.PrCategory = selectedItem._PrCategory;
                                    rec.PrCategorySource = selectedItem.PrCategorySource;
                                }
                                SetItem(rec);
                            }
                        }
                    }
                }
            }
        }
    }
}
