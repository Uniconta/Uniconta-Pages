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
using Uniconta.API.Inventory;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetItemCoverageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectBudgetLineLocal); } }
        public override IComparer GridSorting { get { return new ProjectBudgetLineSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return true; } }

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

    public partial class ProjectBudgetItemCoverage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectBudgetItemCoverage; } }
        SQLCache ProjectCache, EmployeeCache, ItemCache, PayrollCache;
        int PriceGrp;
        ProjectBudget projectBudget;

        public ProjectBudgetItemCoverage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectBudgetItemCoverage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgPrjBugtItmCov.UpdateMaster(args);
            SetHeader();
            Filter(null);
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgPrjBugtItmCov.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ItemCoverage"), key);
            SetHeader(header);
        }

        Uniconta.DataModel.Project Proj;
        void InitPage(UnicontaBaseEntity master)
        {
            ((TableView)dgPrjBugtItmCov.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            localMenu.dataGrid = dgPrjBugtItmCov;
            dgPrjBugtItmCov.api = api;
            dgPrjBugtItmCov.UpdateMaster(master);
            Proj = master as Uniconta.DataModel.Project;
            if (Proj == null)
            {
                var Pb = master as ProjectBudgetClient;
                if (Pb != null)
                {
                    projectBudget = Pb;
                    Proj = Pb.ProjectRef;
                }
            }
            SetRibbonControl(localMenu, dgPrjBugtItmCov);
            dgPrjBugtItmCov.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            dgPrjBugtItmCov.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgPrjBugtItmCov.ShowTotalSummary();
            dgPrjBugtItmCov.CustomSummary += dgProjectBudgetLinePageGrid_CustomSummary;
            dgPrjBugtItmCov.tableView.ShowGroupFooters = true;
        }

        double sumCost, sumSales;
        private void dgProjectBudgetLinePageGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumCost = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectBudgetLineLocal;
                    if (!row._Disable && !row._Header)
                    {
                        sumSales += row.Sales;
                        sumCost += row.Cost;
                    }
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        e.TotalValue = Math.Round((sumSales - sumCost) * 100d / sumSales, 2);
                    }
                    break;
            }
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

            var master = dgPrjBugtItmCov.masterRecord;
            var masterProject = (master as Uniconta.DataModel.Project);
            if (masterProject == null)
            {
                var budget = (master as ProjectBudget);
                if (budget != null)
                    masterProject = (Uniconta.DataModel.Project)ProjectCache.Get(budget._Project);
            }
            if (masterProject != null)
            {
                var dc = (Uniconta.DataModel.Debtor)Debtors.Get(masterProject._DCAccount);
                if (dc != null)
                    PriceGrp = dc._PriceGroup;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
            if (dgPrjBugtItmCov.masterRecord is Uniconta.DataModel.ProjectBudget)
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
            if (!Comp.Warehouse)
            {
                Warehouse.Visible = false;
                Warehouse.ShowInColumnChooser = false;
            }
            else
                Warehouse.ShowInColumnChooser = true;
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
                    if (rec._Qty != (rec._ToTime - rec._FromTime) / 60d)
                    {
                        rec._FromTime = 0;
                        rec._ToTime = 0;
                    }
                    break;
                case "CostPrice":
                case "SalesPrice":
                    if (rec._Qty == 0d)
                        rec.Qty = 1d;
                    break;
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

            double price = 0d;
            int pg = PriceGrp;
            if (pg == 3)
            {
                price = item._SalesPrice3;
                if (price == 0)
                    pg = 2;
            }
            if (pg == 2)
                price = item._SalesPrice2;
            if (price == 0)
                price = item._SalesPrice1;
            if (price != 0d)
            {
                if (rec._Qty == 0)
                    rec.Qty = 1d;
                rec.SalesPrice = price;
            }

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

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgPrjBugtItmCov.SelectedItem as ProjectBudgetLineLocal;
            switch (ActionType)
            {
                case "AddRow":
                    dgPrjBugtItmCov.AddRow();
                    break;
                case "CopyRow":
                    dgPrjBugtItmCov.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgPrjBugtItmCov.DeleteRow();
                    break;
                case "CreateProductionOrder":
                    if (selectedItem == null)
                        return;
                    if (selectedItem._Item != null && selectedItem.InvItem._ItemType == (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                    {
                        CwPurOrderDfltVal cWindow = new CwPurOrderDfltVal(api, isProdOrder: true);
                        cWindow.Closed += delegate
                        {
                            if (cWindow.DialogResult == true)
                                CreateProductionOrder(cWindow.DefaultProductionOrder, cWindow.CreatePurchaseLines, cWindow.Storage, selectedItem);
                        };
                        cWindow.Show();
                    }
                    break;
                case "InvTrans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InventoryTransactions, dgPrjBugtItmCov.syncEntity);
                    break;
                case "OrderLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.DebtorOrderLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("OrderLines"), selectedItem._Item));
                    break;
                case "PurchaseLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.PurchaseLines, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("PurchaseLines"), selectedItem._Item));
                    break;
                case "ProductionOrders":
                    if (selectedItem._Item != null && selectedItem.InvItem._ItemType == (byte)Uniconta.DataModel.ItemType.ProductionBOM)
                        AddDockItem(TabControls.ProductionOrders, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionOrders"), selectedItem._Item));
                    break;
                case "ProductionLines":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProductionOrderLineReport, selectedItem, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProductionLines"), selectedItem._Item));
                    break;
                case "GenerateJournalLines":
                    dgPrjBugtItmCov.SelectedItem = null;
                    CwMoveBtwWareHouse cwJournal = new CwMoveBtwWareHouse(true,api);
                    cwJournal.Closed += delegate
                    {
                        if (cwJournal.DialogResult == true)
                            MoveToJournal(cwJournal.PrJournal, cwJournal.Warehouse, cwJournal.Location);
                    };
                    cwJournal.Show();
                    break;
                case "Storage":
                    AddDockItem(TabControls.InvItemStoragePage, dgPrjBugtItmCov.syncEntity, true);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                case "InvStockProfile":
                    if (selectedItem != null)
                        AddDockItem(TabControls.InvStorageProfileReport, dgPrjBugtItmCov.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("StockProfile"), selectedItem._Item));
                    break;
                case "CreateOrder":
                    CwPurOrderDfltVal dailog = new CwPurOrderDfltVal(api);
                    dailog.Closed += delegate
                    {
                        if (dailog.DialogResult == true)
                        {
                            CreateOrder(dailog.DefaultCreditorOrder, dailog.OrderLinePerWareHouse, dailog.OrderLinePerLocation);
                        }
                    };
                    dailog.Show();
                    break;
                case "ProposePurchase":
                    foreach (var rec in dgPrjBugtItmCov.GetVisibleRows() as ICollection<ProjectBudgetLineLocal>)
                    {
                        if (rec._Item != null && rec.itm._ItemType == (byte)Uniconta.DataModel.ItemType.Item && rec._Qty > (rec._QtyPurchased + rec._QtyTaken) && rec._Qty > rec.QtyAvailable &&
                            rec.InvPurchaseAccount != null)
                        {
                            rec.Quantity = Math.Round(rec._Qty - rec._QtyPurchased - rec._QtyTaken, rec.Decimals);
                        }
                    }
                    break;
                case "ProposeItemPick":
                    foreach (var rec in dgPrjBugtItmCov.GetVisibleRows() as ICollection<ProjectBudgetLineLocal>)
                    {
                        if (rec._Item != null && rec.itm._ItemType != (byte)Uniconta.DataModel.ItemType.Service && rec._Qty > (rec._QtyPurchased + rec._QtyTaken) && rec._Qty <= rec.QtyAvailable)
                        {
                            rec.Quantity = Math.Round(rec._Qty - rec._QtyPurchased - rec._QtyTaken, rec.Decimals);
                        }
                    }
                    break;
                case "Trans":
                    if (selectedItem != null)
                        AddDockItem(TabControls.ProjectTransactionPage, dgPrjBugtItmCov.syncEntity);
                    break;
                case "AddItems":
                    if (ItemCache == null)
                        return;
                    object[] paramArray = new object[3] { new InvItemSalesCacheFilter(ItemCache), dgPrjBugtItmCov.TableTypeUser, projectBudget };
                    AddDockItem(TabControls.AddMultiInventoryItemsForProject, paramArray, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    break;
                case "UnfoldBOM":
                    if (selectedItem != null)
                        UnfoldBOM(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
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
                dgPrjBugtItmCov.PasteRows(lst);
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

        public override Task InitQuery()
        {
            return Filter(new[] 
            { 
                PropValuePair.GenereteWhereElements("Item", typeof(string), "!null"),
                PropValuePair.GenereteWhereElements("Disable", typeof(bool), "0")
            });
        }

        private void Task_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgPrjBugtItmCov.SelectedItem as ProjectBudgetLineLocal;
            if (selectedItem?._Project != null)
            {
                var selected = (ProjectClient)ProjectCache.Get(selectedItem._Project);
                setTask(selected, selectedItem);
            }
        }
        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgPrjBugtItmCov.Filter(propValuePair);
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        async void CreateOrder(Uniconta.DataModel.CreditorOrder dfltCreditorOrder, bool PrWarehouse, bool PrLocation)
        {
            var rows = dgPrjBugtItmCov.GetVisibleRows() as ICollection<ProjectBudgetLineLocal>;
            if (rows == null || rows.Count == 0)
                return;
            var Orders = (from rec in rows where rec._Item != null && rec._Quantity > 0 && rec.PurchaseNumber != 0 select rec.PurchaseNumber).Distinct().ToList();
            var accounts = (from rec in rows where rec._Item != null && rec._Quantity > 0 && rec.PurchaseNumber == 0 && rec.InvPurchaseAccount != null select rec.InvPurchaseAccount).Distinct().ToList();
            if (accounts.Count == 0 && Orders.Count == 0)
            {
                UtilDisplay.ShowErrorCode(ErrorCodes.NoLinesFound);
                return;
            }

            var Comp = api.CompanyEntity;
            var defaultStorage = Comp._PurchaseLineStorage;
            var CompCur = Comp._Currency;
            var Creditors = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));

            var creditorOrders = new List<CreditorOrderClient>(accounts.Count);
            foreach (var acc in accounts)
            {
                var creditor = (Uniconta.DataModel.Creditor)Creditors.Get(acc);
                if (creditor != null)
                {
                    var ord = this.CreateGridObject(typeof(CreditorOrderClient)) as CreditorOrderClient;
                    ord.SetMaster(creditor);
                    ord._Project = Proj._Number;
                    ord._OurRef = dfltCreditorOrder._OurRef;
                    ord._Remark = dfltCreditorOrder._Remark;
                    ord._Group = dfltCreditorOrder._Group;
                    ord._DeliveryDate = dfltCreditorOrder._DeliveryDate;
                    if (dfltCreditorOrder._Employee != null)
                        ord._Employee = dfltCreditorOrder._Employee;

                    TableField.SetUserFieldsFromRecord(creditor, ord);

                    creditorOrders.Add(ord);

                    if (creditor._DeliveryAddress1 != null)
                    {
                        ord.DeliveryName = creditor._DeliveryName;
                        ord._DeliveryAddress1 = creditor._DeliveryAddress1;
                        ord._DeliveryAddress2 = creditor._DeliveryAddress2;
                        ord._DeliveryAddress3 = creditor._DeliveryAddress3;
                        ord.DeliveryCity = creditor._DeliveryCity;
                        ord.DeliveryZipCode = creditor._DeliveryZipCode;
                        if (creditor._DeliveryCountry != 0)
                            ord._DeliveryCountry = creditor._DeliveryCountry;
                    }
                }
            }

            var oldval = busyIndicator.BusyContent;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("CreatingInProcess");
            busyIndicator.IsBusy = true;

            var HasWarehouse = Comp.Warehouse;
            if (PrLocation)
                PrWarehouse = true;
            if (!PrWarehouse)
                PrLocation = false;

            ErrorCodes result = ErrorCodes.Succes;
            if (creditorOrders.Count > 0)
                result = await api.Insert(creditorOrders);
            if (result == ErrorCodes.Succes)
            {
                if (Orders.Count != 0)
                {
                    var cache = api.GetCache(typeof(CreditorOrder));
                    foreach(var PurchaseNumber in Orders)
                    {
                        var order = cache.Get(NumberConvert.ToString(PurchaseNumber));
                        if (order != null)
                            creditorOrders.Add((CreditorOrderClient)order);
                    }
                }
                var PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(api);
                var _InvoiceUseQtyNowCre = Comp._InvoiceUseQtyNowCre;

                InvItem item;
                int LineStart = 0;
                var orderList = new List<CreditorOrderLineClient>(rows.Count);
                var updateList = new List<ProjectBudgetLineLocal>(rows.Count);
                foreach (var order in creditorOrders)
                {
                    await PriceLookup.OrderChanged(order);
                    var acc = order._DCAccount;
                    var creditor = (Uniconta.DataModel.Creditor)Creditors.Get(acc);
                    int lineNo = 0;
                    foreach (var line in rows)
                    {
                        if (line._Item != null && line._Quantity > 0 && line.InvPurchaseAccount == acc)
                        {
                            item = (InvItem)ItemCache.Get(line._Item);
                            if (HasWarehouse && (!PrWarehouse || !PrLocation))
                            {
                                bool found = false;
                                foreach (var lin in orderList)
                                {
                                    if (lin._Item == line._Item && lin._Variant1 == line._Variant1 && lin._Variant2 == line._Variant2 && lin._Variant3 == line._Variant3 && lin._Variant4 == line._Variant4 && lin._Variant5 == line._Variant5 &&
                                        (!PrWarehouse || lin._Warehouse == item._Warehouse) &&
                                        (!PrLocation || lin._Location == item._Location))
                                    {
                                        lin._Qty += line._Quantity;
                                        if (_InvoiceUseQtyNowCre)
                                            lin._QtyNow = lin._Qty;
                                        found = true;

                                        line.OrderNumber = order._OrderNumber;
                                        line.QtyPurchased += line._Quantity;
                                        line.Quantity = 0;
                                        updateList.Add(line);

                                        break;
                                    }
                                }
                                if (found)
                                    continue;
                            }

                            var orderLine = new CreditorOrderLineClient();
                            if (line.PurchaseNumber == 0)
                                orderLine._LineNumber = ++lineNo;
                            orderLine._Item = line._Item;
                            orderLine._Variant1 = line._Variant1;
                            orderLine._Variant2 = line._Variant2;
                            orderLine._Variant3 = line._Variant3;
                            orderLine._Variant4 = line._Variant4;
                            orderLine._Variant5 = line._Variant5;
                            orderLine._Warehouse = item._Warehouse;
                            orderLine._Location = item._Location;
                            orderLine._Qty = line._Quantity - line._QtyPurchased;
                            if (_InvoiceUseQtyNowCre)
                                orderLine._QtyNow = orderLine._Qty;
                            orderLine._Storage = defaultStorage;
                            orderLine._DiscountPct = creditor._LineDiscountPct;
                            orderLine._Project = Proj._Number;
                            orderLine._PrCategory = line._PrCategory ?? item._PrCategory;
                            orderLine.SetMaster(order);
                            TableField.SetUserFieldsFromRecord(orderLine, item);
                            orderList.Add(orderLine);

                            line.OrderNumber = order._OrderNumber;
                            line.QtyPurchased += line._Quantity;
                            line.Quantity = 0;
                            updateList.Add(line);
                        }
                    }
                    DCOrderLine last = null;
                    while (LineStart < orderList.Count)
                    {
                        var lin = orderList[LineStart++];
                        item = (InvItem)ItemCache.Get(lin._Item);
                        if (item == null)
                            continue;
                        if (PrWarehouse && lin._Warehouse == null)
                        {
                            lin._Warehouse = item._Warehouse;
                            if (PrLocation && lin._Location == null)
                                lin._Location = item._Location;
                        }
                        if (last != null && last._Item == lin._Item)
                        {
                            if (!PrWarehouse && last._Warehouse != lin._Warehouse)
                            {
                                lin._Warehouse = null;
                                lin._Location = null;
                                last._Warehouse = null;
                                last._Location = null;
                            }
                            else if (!PrLocation && last._Location != lin._Location)
                                last._Location = lin._Location = null;
                        }
                        last = lin;
                        if (item._UnitGroup == null || item._PurchaseUnit == 0 || item._PurchaseUnit == item._Unit)
                            lin._Unit = item._Unit;

                        var t = PriceLookup.SetPriceFromItem(lin, item);
                        if (t != null)
                            await t;

                        if (item._UnitGroup != null && (lin._Price == item._PurchasePrice || lin._Price == item._CostPrice))
                        {
                            lin._Price = 0;  // Server will set it
                            lin._Unit = 0;
                        }
                    }
                }
                api.InsertNoResponse(orderList);
                api.UpdateNoResponse(updateList);
            }
            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = oldval;

            UtilDisplay.ShowErrorCode(result);
        }

        async void MoveToJournal(Uniconta.DataModel.PrJournal invJournal, string FromWarehouse, string FromLocation)
        {
            if (invJournal == null) return;
            var PriceLookup = new Uniconta.API.DebtorCreditor.FindPrices(api);
            var order = new DebtorOrder();
            order.SetMaster(Proj);
            await PriceLookup.OrderChanged(order);

            var rows = dgPrjBugtItmCov.GetVisibleRows() as ICollection<ProjectBudgetLineLocal>;
            var invJournalLineList = new List<PrJournalLine>(rows.Count);
            var updateList = new List<ProjectBudgetLineLocal>(rows.Count);
            foreach (var line in rows)
            {
                if (line._Item != null && line._Quantity > 0)
                {
                    var journalLine = new ProjectJournalLineClient();
                    journalLine._Project = Proj._Number;
                    journalLine._Item = line._Item;
                    journalLine._Variant1 = line._Variant1;
                    journalLine._Variant2 = line._Variant2;
                    journalLine._Variant3 = line._Variant3;
                    journalLine._Variant4 = line._Variant4;
                    journalLine._Variant5 = line._Variant5;
                    journalLine._Employee = line._Employee;
                    journalLine._PayrollCategory = line._PayrollCategory;

                    var itm = (InvItem)this.ItemCache.Get(line._Item);
                    if (itm != null)
                    {
                        journalLine._Warehouse = itm._Warehouse;
                        journalLine._Location = itm._Location;
                        journalLine._PrCategory = itm._PrCategory;
                    }

                    if (line._Warehouse != null)
                    {
                        journalLine._Warehouse = line._Warehouse;
                        journalLine._Location = line._Location;
                    }
                    else if (FromWarehouse != null)
                    {
                        journalLine._Warehouse = FromWarehouse;
                        journalLine._Location = FromLocation;
                    }

                    if (line._PrCategory != null)
                        journalLine._PrCategory = line._PrCategory;

                    journalLine._Qty = line._Quantity - line._QtyTaken;
                    journalLine.SetMaster(invJournal);
                    journalLine._Dim1 = invJournal._Dim1;
                    journalLine._Dim2 = invJournal._Dim2;
                    journalLine._Dim3 = invJournal._Dim3;
                    journalLine._Dim4 = invJournal._Dim4;
                    journalLine._Dim5 = invJournal._Dim5;
                    invJournalLineList.Add(journalLine);

                    line.QtyTaken += line._Quantity;
                    line.Quantity = 0;
                    updateList.Add(line);

                    var t = PriceLookup.GetCustomerPrice(journalLine, true);
                    if (t != null)
                        await t;
                }
            }

            ErrorCodes result;
            if (invJournalLineList.Count == 0)
                result = ErrorCodes.NoLinesFound;
            else
            {
                api.AllowBackgroundCrud = true;
                result = await api.Insert(invJournalLineList);
                api.UpdateNoResponse(updateList);
            }
            UtilDisplay.ShowErrorCode(result);
            if(result== ErrorCodes.Succes)
            {
                var jrlName = invJournal.KeyName;
                var text = string.Concat(Uniconta.ClientTools.Localization.lookup("TransferedToJournal"), ": ", jrlName,
                        Environment.NewLine, string.Format(Uniconta.ClientTools.Localization.lookup("GoTo"), Uniconta.ClientTools.Localization.lookup("Journallines")), " ?");
                var select = UnicontaMessageBox.Show(text, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel);
                if (select == MessageBoxResult.OK)
                {
                    var header = string.Concat(Uniconta.ClientTools.Localization.lookup("Journal"), ": ", jrlName);
                    AddDockItem(TabControls.ProjectJournalLinePage, null, header, null, true, null, new[] { new BasePage.ValuePair("Journal", jrlName) });
                }
            }
        }

        async void CreateProductionOrder(Uniconta.DataModel.ProductionOrder dfltProductionOrder, bool createProdLines, int storage, ProjectBudgetLineLocal rec)
        {
            var item = (InvItem)this.ItemCache.Get(rec._Item);
            var ord = this.CreateGridObject(typeof(ProductionOrderClient)) as ProductionOrderClient;
            ord.SetMaster(Proj);
            ord._ProdItem = rec._Item;
            ord._OurRef = dfltProductionOrder._OurRef;
            ord._Remark = dfltProductionOrder._Remark;
            ord._Group = dfltProductionOrder._Group;
            ord._DeliveryDate = dfltProductionOrder._DeliveryDate;
            ord._Shipment = dfltProductionOrder._Shipment;
            ord._Employee = dfltProductionOrder._Employee;
            ord._ProdQty = Math.Max(Math.Max(item._PurchaseQty, item._PurchaseMin), rec._Quantity);
            ord._Storage = (StorageRegister)storage;
            TableField.SetUserFieldsFromRecord(rec, ord);

            busyIndicator.IsBusy = true;
            var result = await api.Insert(ord);
            if (result == ErrorCodes.Succes && createProdLines)
                result = await new ProductionAPI(api).CreateProductionLines(ord, (StorageRegister)storage);
            busyIndicator.IsBusy = false;
            if (result == ErrorCodes.Succes)
            {
                rec.QtyPurchased += rec._Quantity;
                rec.Quantity = 0;
                api.UpdateNoResponse(rec);
            }
            UtilDisplay.ShowErrorCode(result);
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            var param = argument as object[];
            if (param != null)
            {
                if (screenName == TabControls.AddMultiInventoryItemsForProject)
                {
                    var budgetId = (int)NumberConvert.ToInt(Convert.ToString(param[1]));
                    if (projectBudget.RowId == budgetId)
                    {
                        var selectedItem = dgPrjBugtItmCov.SelectedItem as ProjectBudgetLineLocal;
                        if (dgPrjBugtItmCov.isDefaultFirstRow)
                        {
                            dgPrjBugtItmCov.DeleteRow();
                            dgPrjBugtItmCov.isDefaultFirstRow = false;
                        }
                        var lst = param[0] as List<UnicontaBaseEntity>;
                        if (dgPrjBugtItmCov.PasteRows(lst))
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
