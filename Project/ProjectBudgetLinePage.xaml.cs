using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;
using System.Windows.Threading;
using System.Threading;
using UnicontaClient.Utilities;
using System.Windows;
using Uniconta.ClientTools;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using Uniconta.ClientTools.Util;
using System.ComponentModel.DataAnnotations;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetLineLocal : ProjectBudgetLineClient
    {
        internal bool InsidePropChange;
        internal object taskSource;
        public object TaskSource { get { return taskSource; } }
        public PrCategoryCacheFilter PrCategorySource { get; internal set; }

        string _invPurchaseAccount;
        [Display(Name = "InvPurchaseAccount", ResourceType = typeof(InventoryText))]
        public String InvPurchaseAccount
        {
            get
            {
                return _invPurchaseAccount ?? _CreditorAccount ?? _itm._PurchaseAccount;
            }
            set { _invPurchaseAccount = value; NotifyPropertyChanged("InvPurchaseAccount"); }
        }

        internal object invPurchaseAccSource;
        public object InvPurchaseAccSource { get { return invPurchaseAccSource; } }

        int _PurchaseNumber;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(CreditorOrder))]
        [Display(Name = "PurchaseNumber", ResourceType = typeof(VouchersClientText))]
        public int PurchaseNumber { get { return _PurchaseNumber; } set { _PurchaseNumber = value; NotifyPropertyChanged("PurchaseNumber"); } }

        InvItem _itm;
        internal InvItem itm
        {
            get
            {
                if (_itm != null && _itm._Item == _Item)
                    return _itm;
                return (_itm = this.InvItem);
            }
        }

        [AppEnumAttribute(EnumName = "ItemType")]
        [Display(Name = "ItemType", ResourceType = typeof(InventoryText)), Required]
        public string ItemType { get { return itm != null ? AppEnums.ItemType.ToString(itm._ItemType) : null; } }

        public double _Quantity;

        [Display(Name = "ToOrder", ResourceType = typeof(DCOrderText))]
        [NoSQL]
        public double Quantity { get { return _Quantity; } set { _Quantity = value; NotifyPropertyChanged("Quantity"); } }

        [Display(Name = "QtyReserved", ResourceType = typeof(InvItemStorageClientText))]
        [NoSQL]
        public double QtyResv { get { return itm._qtyReserved; } }

        [Display(Name = "QtyOrdered", ResourceType = typeof(InvItemStorageClientText))]
        [NoSQL]
        public double QtyOdr { get { return itm._qtyOrdered; } }

        [Display(Name = "InStock", ResourceType = typeof(InventoryText))]
        [NoSQL]
        public double QtyStock { get { return itm._qtyOnStock; } }

        [Display(Name = "Available", ResourceType = typeof(InvItemStorageClientText))]
        [NoSQL]
        public double QtyAvailable { get { return itm._qtyOnStock + itm._qtyOrdered - itm._qtyReserved; } }
    }

    public class ProjectBudgetLinePageGrid : CorasauDataGridClient
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
                foreach (var journalLine in (IEnumerable<ProjectBudgetLineLocal>)lst)
                {
                    if (journalLine._Date != DateTime.MinValue && Cur == null)
                        LastDateTime = journalLine._Date;
                    n++;
                    if (n == selectedIndex)
                        Cur = journalLine;
                    last = journalLine;
                }

                newRow._Date = LastDateTime != DateTime.MinValue ? LastDateTime : BasePage.GetSystemDefaultDate().Date;
                newRow._Project = last._Project;
                newRow._PrCategory = last._PrCategory;
                newRow.PrCategorySource = last.PrCategorySource;
            }
        }
    }

    public partial class ProjectBudgetLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectBudgetLinePage; } }
        SQLCache ProjectCache, EmployeeCache, ItemCache, PayrollCache;
        UnicontaBaseEntity _master;
        Uniconta.API.DebtorCreditor.FindPrices PriceLookup;

        public ProjectBudgetLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public ProjectBudgetLinePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectBudgetLinePageGrid.UpdateMaster(args);
            SetHeader();
            Filter(null);
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgProjectBudgetLinePageGrid.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("ProjectBudget"), key);
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
                    PriceLookup.OrderChanged(masterProject, BasePage.GetSystemDefaultDate());
            }
        }

        void InitPage(UnicontaBaseEntity master)
        {
            ((TableView)dgProjectBudgetLinePageGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            localMenu.dataGrid = dgProjectBudgetLinePageGrid;
            dgProjectBudgetLinePageGrid.api = api;
            _master = master;

            dgProjectBudgetLinePageGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgProjectBudgetLinePageGrid);
            dgProjectBudgetLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            dgProjectBudgetLinePageGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgProjectBudgetLinePageGrid.ShowTotalSummary();
            dgProjectBudgetLinePageGrid.CustomSummary += dgProjectBudgetLinePageGrid_CustomSummary;
            dgProjectBudgetLinePageGrid.tableView.ShowGroupFooters = true;
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
            if (!Comp.Warehouse)
            {
                Warehouse.Visible = false;
                Warehouse.ShowInColumnChooser = false;
            }
            else
                Warehouse.ShowInColumnChooser = true;
            if (Comp.HideCostPrice)
            {
                CostPrice.Visible = CostPrice.ShowInColumnChooser =
            Cost.Visible = Cost.ShowInColumnChooser = false;
            }
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
                    if (this.PriceLookup != null && this.PriceLookup.UseCustomerPrices)
                        this.PriceLookup.GetCustomerPrice(rec, false);

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

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectBudgetLinePageGrid.AddRow();
                    break;
                case "CopyRow":
                    var row = dgProjectBudgetLinePageGrid.CopyRow() as ProjectBudgetLineLocal;
                    if (row != null)
                    {
                        row.Id = 0;
                        if (row._Date < DateTime.Now && row._Date != DateTime.MinValue)
                            row.Date = DateTime.Now.Date;
                    }
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgProjectBudgetLinePageGrid.DeleteRow();
                    break;
                case "AddItems":
                    if (ItemCache == null)
                        return;
                    object[] paramArray = new object[3] { new InvItemSalesCacheFilter(ItemCache), dgProjectBudgetLinePageGrid.TableTypeUser, _master };
                    AddDockItem(TabControls.AddMultiInventoryItemsForProject, paramArray, true,
                        string.Format(Uniconta.ClientTools.Localization.lookup("AddOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems")), null, floatingLoc: Utility.GetDefaultLocation());
                    break;
                case "UnfoldBOM":
                    var selectedItem = dgProjectBudgetLinePageGrid.SelectedItem as ProjectBudgetLineLocal;
                    if (selectedItem != null)
                        UnfoldBOM(selectedItem);
                    break;
                case "AddPeriod":
                    AddPeriod();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void AddPeriod()
        {
            var prjBudgetLns = dgProjectBudgetLinePageGrid.GetVisibleRows() as IEnumerable<ProjectBudgetLineLocal>;
            if (prjBudgetLns == null || prjBudgetLns.Count() == 0)
                return;

            var cwSelectPeriod = new CWAddPeriod();
            cwSelectPeriod.DialogTableId = 2000000105;
            cwSelectPeriod.Closed += delegate
            {
                if (cwSelectPeriod.DialogResult == true)
                {
                    var periodType = cwSelectPeriod.PeriodType;
                    foreach (ProjectBudgetLineLocal line in prjBudgetLns)
                    {
                        var budgetDt = line.Date;
                        dgProjectBudgetLinePageGrid.SetLoadedRow(line);
                        switch (periodType)
                        {
                            case 0:
                                line._Date = budgetDt.AddDays(cwSelectPeriod.PeriodValue);
                                break;
                            case 1:
                                line._Date = budgetDt.AddMonths(cwSelectPeriod.PeriodValue);
                                break;
                            case 2:
                                line._Date = budgetDt.AddYears(cwSelectPeriod.PeriodValue);
                                break;
                        }
                        line.NotifyPropertyChanged("Date");
                        dgProjectBudgetLinePageGrid.SetModifiedRow(line);
                    }

                    saveGrid();
                }
            };
            cwSelectPeriod.Show();
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

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            var param = argument as object[];
            if (param != null)
            {
                if (screenName == TabControls.AddMultiInventoryItemsForProject)
                {
                    var budgetId = (int)NumberConvert.ToInt(Convert.ToString(param[1]));
                    if (_master is ProjectBudget projectBudget && projectBudget.RowId == budgetId)
                    {
                        var selectedItem = dgProjectBudgetLinePageGrid.SelectedItem as ProjectBudgetLineLocal;
                        if (dgProjectBudgetLinePageGrid.isDefaultFirstRow)
                        {
                            dgProjectBudgetLinePageGrid.DeleteRow();
                            dgProjectBudgetLinePageGrid.isDefaultFirstRow = false;
                        }
                        var lst = param[0] as IEnumerable<UnicontaBaseEntity>;
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
