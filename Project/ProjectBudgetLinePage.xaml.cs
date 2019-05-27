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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectBudgetLineLocal : ProjectBudgetLineClient
    {
        internal bool InsidePropChange;
        internal object taskSource;
        public object TaskSource { get { return taskSource; } }
        public PrCategoryCacheFilter PrCategorySource { get; internal set; }
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

    public partial class ProjectBudgetLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectBudgetLinePage; } }
        SQLCache ProjectCache, EmployeeCache, ItemCache, PayrollCache;
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
        }

        void InitPage(UnicontaBaseEntity master)
        {
            localMenu.dataGrid = dgProjectBudgetLinePageGrid;
            dgProjectBudgetLinePageGrid.api = api;
            dgProjectBudgetLinePageGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgProjectBudgetLinePageGrid);
            dgProjectBudgetLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectBudgetLinePageGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgProjectBudgetLinePageGrid.ShowTotalSummary();
            dgProjectBudgetLinePageGrid.CustomSummary += dgProjectBudgetLinePageGrid_CustomSummary;
            dgProjectBudgetLinePageGrid.tableView.ShowGroupFooters = true;
        }

        double sumMargin, sumSales, sumMarginRatio;
        private void dgProjectBudgetLinePageGrid_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    sumMargin = sumSales = 0d;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectBudgetLineLocal;
                    sumSales += row.Sales;
                    sumMargin += row.Margin;
                    break;
                case CustomSummaryProcess.Finalize:
                    if (fieldName == "MarginRatio" && sumSales > 0)
                    {
                        sumMarginRatio = 100 * sumMargin / sumSales;
                        e.TotalValue = sumMarginRatio;
                    }
                    break;
            }
        }
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (Comp.ProjectTask)
                ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api).ConfigureAwait(false);
            EmployeeCache = Comp.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Employee), api).ConfigureAwait(false);
            if (Comp.Payroll)
                PayrollCache = Comp.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory), api).ConfigureAwait(false);
            ItemCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);
            LoadType(typeof(Uniconta.DataModel.PrCategory));
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
            if (!Comp.Payroll)
            {
                PayrollCategory.Visible = false;
                PayrollCategory.ShowInColumnChooser = false;
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
            var pay = (Uniconta.DataModel.EmpPayrollCategory)PayrollCache?.Get(rec._PayrollCategory);
            if (pay == null)
                return;

            if (pay._Unit != 0 && rec._Unit != pay._Unit)
            {
                rec._Unit = pay._Unit;
                rec.NotifyPropertyChanged("Unit");
            }

            var Rates = pay.Rates ?? await pay.LoadRates(api);

            double costPrice = pay._Rate, salesPrice = pay._SalesPrice;
            string Item = pay._Item;
            if (pay._Dim1 != null) rec.Dimension1 = pay._Dim1;
            if (pay._Dim2 != null) rec.Dimension2 = pay._Dim2;
            if (pay._Dim3 != null) rec.Dimension3 = pay._Dim3;
            if (pay._Dim4 != null) rec.Dimension4 = pay._Dim4;
            if (pay._Dim5 != null) rec.Dimension5 = pay._Dim5;

            var itm = rec._Item;
            var rate = (from ct in Rates where ct._Employee == rec._Employee && (itm == null || itm == ct._Item) select ct).FirstOrDefault();
            if (rate != null)
            {
                if (rate._CostPrice != 0d)
                    costPrice = rate._CostPrice;
                else if (rate._Rate != 0d)
                    costPrice = rate._Rate;
                if (rate._SalesPrice != 0d)
                    salesPrice = rate._SalesPrice;
                if (rate._Item != null)
                    Item = rate._Item;

                if (rate._Dim1 != null) rec.Dimension1 = rate._Dim1;
                if (rate._Dim2 != null) rec.Dimension2 = rate._Dim2;
                if (rate._Dim3 != null) rec.Dimension3 = rate._Dim3;
                if (rate._Dim4 != null) rec.Dimension4 = rate._Dim4;
                if (rate._Dim5 != null) rec.Dimension5 = rate._Dim5;
            }

            if (AddItem && Item != null)
                rec.Item = Item;
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
                if (rec._SalesPrice == 0d)
                    PayrollCat(rec, false);
            }
            if (item._Unit != 0 && rec._Unit != item._Unit)
            {
                rec._Unit = item._Unit;
                rec.NotifyPropertyChanged("Unit");
            }
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
                    dgProjectBudgetLinePageGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgProjectBudgetLinePageGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
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
    }
}
