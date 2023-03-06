using DevExpress.Data;
using DevExpress.Xpf.Grid;
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
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class RemainingBudgetLine : GridBasePage
    {
        SQLCache ProjectCache, EmployeeCache, ItemCache, PayrollCache, BudgetGroupCache;

        public RemainingBudgetLine(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);
        }

        public RemainingBudgetLine(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            localMenu.dataGrid = dgProjectBgtPlangLine;
            dgProjectBgtPlangLine.api = api;
            SetRibbonControl(localMenu, dgProjectBgtPlangLine);
            dgProjectBgtPlangLine.UpdateMaster(master);
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

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectBgtPlangLine.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        private void SetHeader()
        {
            var syncMaster = dgProjectBgtPlangLine.masterRecord as Uniconta.DataModel.TMJournalLine;
            if (syncMaster == null)
                return;

            string header = string.Format("{0} : {1} ({2}: {3} {4}: {5})", Uniconta.ClientTools.Localization.lookup("RemainingBudget"), syncMaster?._Project, Uniconta.ClientTools.Localization.lookup("WorkSpace"),
                                                                                syncMaster?._WorkSpace, Uniconta.ClientTools.Localization.lookup("Task"), syncMaster?._Task);

            SetHeader(header);
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
            var Comp = api.CompanyEntity;
            if (!Comp.ProjectTask)
            {
                Task.Visible = false;
                Task.ShowInColumnChooser = false;
            }
            else
                Task.ShowInColumnChooser = true;
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
                    if (row != null)
                    {
                        sumSales += row.Sales;
                        sumMargin += row.Margin;
                    }
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
        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectBgtPlangLine.SelectedItem as ProjectBudgetLineLocal;
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectBgtPlangLine.AddRow();
                    break;
                case "CopyRow":
                    dgProjectBgtPlangLine.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                    {
                        dgProjectBgtPlangLine.DeleteRow();
                    }
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
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

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
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
        }
    }
}
