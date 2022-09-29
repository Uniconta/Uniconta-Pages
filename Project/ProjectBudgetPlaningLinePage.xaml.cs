using DevExpress.Data;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
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
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetPlaningLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectBudgetLineLocal); } }
        public override IComparer GridSorting { get { return new ProjectBudgetLineSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return false; } }
        public DateTime FromDate { get; set; }
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
                newRow._Date = FromDate.Date != DateTime.MinValue ? FromDate.Date : BasePage.GetSystemDefaultDate().Date; ;
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

    public partial class ProjectBudgetPlaningLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectBudgetPlaningLinePage; } }
        SQLCache ProjectCache, EmployeeCache, ItemCache, PayrollCache, BudgetGroupCache;
        List<PropValuePair> inputs;
        List<UnicontaBaseEntity> inputsMaster;

        int monthNumber, quarterNo, year;
        double normQty, budgetQty;
        DateTime fromDate, toDate;
        
        Uniconta.API.Project.FindPricesEmpl priceLookup;
        public ProjectBudgetPlaningLinePage(string[] filterFld, int[] filterDateValues,double[] qtyValues, DateTime[] dates, CrudAPI api, string budgetGroup)
            : base(api, string.Empty)
        {
            InitializeComponent();
            CreateFiltersForData(filterFld, filterDateValues, qtyValues, dates, budgetGroup);
            InitPage();
        }

        void InitPage()
        {
            localMenu.dataGrid = dgProjectBgtPlangLine;
            dgProjectBgtPlangLine.api = api;
            SetRibbonControl(localMenu, dgProjectBgtPlangLine);
            dgProjectBgtPlangLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectBgtPlangLine.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgProjectBgtPlangLine.ShowTotalSummary();
            dgProjectBgtPlangLine.CustomSummary += dgProjectBudgetLinePageGrid_CustomSummary;
            dgProjectBgtPlangLine.tableView.ShowGroupFooters = true;

            var Comp = api.CompanyEntity;

            ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project));
            EmployeeCache = Comp.GetCache(typeof(Uniconta.DataModel.Employee));
            ItemCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem));
            PayrollCache = Comp.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory));
            BudgetGroupCache = Comp.GetCache(typeof(Uniconta.DataModel.ProjectBudgetGroup));
        }

        async void CreateFiltersForData(string[] filterFld, int[] filterDateValues, double[] qtyValues, DateTime[] dates, string projectBudgetGrp)
        {
            var filterFldName = filterFld[0];
            var filterFldValue = filterFld[1];
            inputs = new List<PropValuePair>();
            inputs.Add(PropValuePair.GenereteWhereElements(filterFldName, typeof(string), filterFldValue));
            inputs.Add(PropValuePair.GenereteWhereElements(nameof(ProjectBudgetLine._AnchorBudget), typeof(bool), 0));

            if (BudgetGroupCache == null)
                BudgetGroupCache = await api.LoadCache(typeof(Uniconta.DataModel.ProjectBudgetGroup));
            inputsMaster = new List<UnicontaBaseEntity>();
            var budgetGrp = (ProjectBudgetGroup)BudgetGroupCache.Get(projectBudgetGrp);
            if (budgetGrp != null)
                inputsMaster.Add((ProjectBudgetGroup)BudgetGroupCache.Get(projectBudgetGrp));

            monthNumber = filterDateValues[0];
            quarterNo = filterDateValues[1];
            year = filterDateValues[2];
            normQty = qtyValues[0];
            budgetQty = qtyValues[1];
            fromDate = dates[0];
            toDate = dates[1];
        }

        public async override Task InitQuery()
        {
            busyIndicator.IsBusy = true;
            var projLst = await api.Query<ProjectBudgetLineLocal>(inputsMaster, inputs);
            IEnumerable<ProjectBudgetLineLocal> lst;
            if (year > 0 && monthNumber > 0)
                lst = projLst?.Where(x => x.Date.Year == year && x.Date.Month == monthNumber);
            else if (year > 0 && quarterNo > 0)
            {
                var months = GetMonthNo(quarterNo);
                lst = projLst?.Where(x => x.Date.Year == year && x.Date.Month >= months[0] && x.Date.Month <= months[1]);
            }
            else if (monthNumber > 0 && quarterNo > 0)
                lst = null;
            else if (monthNumber > 0)
                lst = projLst?.Where(x => x.Date.Month == monthNumber);
            else if (year > 0)
                lst = projLst?.Where(x => x.Date.Year == year);
            else if (quarterNo > 0)
            {
                var months = GetMonthNo(quarterNo);
                lst = projLst?.Where(x => x.Date.Month >= months[0] && x.Date.Month <= months[1]);
                period = string.Format("{0} {1}", months[0], year);
            }
            else
                lst = null;

            dgProjectBgtPlangLine.SetSource(lst != null ? lst.ToArray() : projLst);
            busyIndicator.IsBusy = false;
            SetStatusText(budgetQty);
            dgProjectBgtPlangLine.Visibility = Visibility.Visible;
        }
     
        int[] GetMonthNo(int qtNo)
        {
            int[] months =  null;
            if (qtNo == 1)
                months = new int[] { 1, 3 };
            else if (qtNo == 2)
                months = new int[] { 4, 6 };
            else if (qtNo == 3)
                months = new int[] { 7, 9 };
            else if (qtNo == 4)
                months = new int[] { 10, 12 };
            return months; 
        }

        string period;
        void SetStatusText(double bgtHrs)
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var fmDtText = Uniconta.ClientTools.Localization.lookup("FromDate");
            var toDteText = Uniconta.ClientTools.Localization.lookup("ToDate");
            var normHrtxt = Uniconta.ClientTools.Localization.lookup("NormHours");
            var bgtHrtxt = Uniconta.ClientTools.Localization.lookup("Budget");
            var diftxt = Uniconta.ClientTools.Localization.lookup("Dif");
            foreach (var grp in groups)
            {
                if (grp.Caption == fmDtText)
                    grp.StatusValue = fromDate.Date.ToString("dd/MM/yyyy");
                else if (grp.Caption == toDteText)
                    grp.StatusValue = toDate.Date.ToString("dd/MM/yyyy");
                else if (grp.Caption == normHrtxt)
                    grp.StatusValue = normQty.ToString("N2");
                else if (grp.Caption == bgtHrtxt)
                    grp.StatusValue = bgtHrs.ToString("N2");
                else if (grp.Caption == diftxt)
                {
                    var tot = normQty - bgtHrs;
                    grp.StatusValue = tot.ToString("N2");
                }
                else grp.StatusValue = string.Empty;
            }
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
                    if (row != null)
                    {
                        sumSales += row.Sales;
                        sumCost += row.Cost;
                    }
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                        e.TotalValue = Math.Round((sumSales - sumCost) * 100d / sumSales, 2);
                    break;
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

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
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

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (ProjectBudgetLineLocal)sender;
            switch (e.PropertyName)
            {
                case "Date":
                case "Project":
                    GetEmplPrice(rec);
                    break;
                case "Item":
                    if (!rec.InsidePropChange)
                    {
                        rec.InsidePropChange = true;
                        SetItem(rec);
                        GetEmplPrice(rec);
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
                            GetEmplPrice(rec);
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
                            GetEmplPrice(rec);
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
                    CalculateSum();
                    break;
                case "CostPrice":
                case "SalesPrice":
                    if (rec._Qty == 0d)
                        rec.Qty = 1d;
                    break;
            }
        }

        void CalculateSum()
        {
            var lst = dgProjectBgtPlangLine.ItemsSource as IEnumerable<ProjectBudgetLineLocal>;
            var QtySum = lst.Sum(x => x._Qty);
            SetStatusText(QtySum);
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

            if (item._SalesPrice1 != 0d)
            {
                if (rec._Qty == 0)
                    rec.Qty = 1d;
                rec.SalesPrice = item._SalesPrice1;
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
            var selectedItem = dgProjectBgtPlangLine.SelectedItem as ProjectBudgetLineLocal;
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectBgtPlangLine.FromDate = fromDate;
                    dgProjectBgtPlangLine.AddRow();
                    break;
                case "CopyRow":
                    dgProjectBgtPlangLine.CopyRow();
                    CalculateSum();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                    {
                        dgProjectBgtPlangLine.DeleteRow();
                        CalculateSum();
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }


        void GetEmplPrice(ProjectBudgetLineLocal rec)
        {
            priceLookup.GetEmployeePrice(rec);
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

        private void Task_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgProjectBgtPlangLine.SelectedItem as ProjectBudgetLineLocal;
            if (selectedItem?._Project != null)
            {
                var selected = (ProjectClient)ProjectCache.Get(selectedItem._Project);
                setTask(selected, selectedItem);
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            if (ProjectCache == null)
                ProjectCache = await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
            if (EmployeeCache == null)
                EmployeeCache = await api.LoadCache(typeof(Uniconta.DataModel.Employee)).ConfigureAwait(false);
            if (ItemCache == null)
                ItemCache = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            if (PayrollCache == null)
                PayrollCache = await api.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory)).ConfigureAwait(false);
            if (BudgetGroupCache == null)
                BudgetGroupCache = await api.LoadCache(typeof(Uniconta.DataModel.ProjectBudgetGroup)).ConfigureAwait(false);
            
            if (this.priceLookup == null)
                priceLookup = new Uniconta.API.Project.FindPricesEmpl(api);
        }
    }
}
