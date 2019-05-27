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
using Uniconta.DataModel;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using System.Collections;
using Uniconta.API.GeneralLedger;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public partial class WorkOrderInput : GridBasePage
    {
        // LayoutClient defaultLayout = null;
        public WorkOrderInput(ProjectJournalClient journal, ProjectClient project)
           : base(null)
        {
            InitializeComponent();
            dgProjectJournalLinePageGrid.Visibility = Visibility.Visible;
            InitPage(journal, project);
        }

        UnicontaAPI.Project.API.PostingAPI postingApi;
        ProjectJournalClient masterJournal;
        string CurrentEmployee;

        protected override Filter[] DefaultFilters()
        {
            Filter categoryFilter = new Filter();
            categoryFilter.name = "PrCategory";
            categoryFilter.value = this.cmbCategoryType.EditValue.ToString();
            return new Filter[] { categoryFilter };
        }

        bool hasProjectMaster;
        public void InitPage(ProjectJournalClient journal, ProjectClient project)
        {
            masterJournal = journal;
            localMenu.dataGrid = dgProjectJournalLinePageGrid;
            SetRibbonControl(localMenu, dgProjectJournalLinePageGrid);
            dgProjectJournalLinePageGrid.api = api;
            if (project == null)
                dgProjectJournalLinePageGrid.UpdateMaster(masterJournal);
            else
            {
                dgProjectJournalLinePageGrid.masterRecords = new List<UnicontaBaseEntity>() { journal, project };
                hasProjectMaster = true;
            }
            dgProjectJournalLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectJournalLinePageGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            postingApi = new UnicontaAPI.Project.API.PostingAPI(api);
            BindCategoryType();
        }

        SQLCache ProjectCache, PrStandardCache, ItemsCache, CreditorCache, CategoryCache, PayrollCache, EmployeeCache, warehouse;
        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            CategoryCache = Comp.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.PrCategory), api).ConfigureAwait(false);
            EmployeeCache = Comp.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Employee), api).ConfigureAwait(false);
            PayrollCache = Comp.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory), api).ConfigureAwait(false);
            PrStandardCache = Comp.GetCache(typeof(Uniconta.DataModel.PrStandard)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.PrStandard), api).ConfigureAwait(false);
            ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api).ConfigureAwait(false);
            ItemsCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api).ConfigureAwait(false);
            if (Comp.Warehouse)
                warehouse = Comp.GetCache(typeof(Uniconta.DataModel.InvWarehouse)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvWarehouse), api).ConfigureAwait(false);

            var uid = api.session.User.Uid;
            foreach (var emp in (Uniconta.DataModel.Employee[])EmployeeCache.GetNotNullArray)
                if (emp._Uid == uid)
                {
                    CurrentEmployee = emp._Number;
                    break;
                }
        }

        void BindCategoryType()
        {
            List<string> requiredTypes = new List<string>();
            foreach (var type in AppEnums.CategoryType.Values)
            {
                var index = AppEnums.CategoryType.IndexOf(type);
                if (index == (int)CategoryType.Materials || index == (int)CategoryType.Expenses || index == (int)CategoryType.ExternalWork || index == (int)CategoryType.Labour)
                    requiredTypes.Add(type);
            }
            cmbCategoryType.ItemsSource = requiredTypes;
            cmbCategoryType.SelectedIndex = 0;
        }
        protected override void OnLayoutLoaded()
        {
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            ProjectJournalLineLocal oldselectedItem = e.OldItem as ProjectJournalLineLocal;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            ProjectJournalLineLocal selectedItem = e.NewItem as ProjectJournalLineLocal;
            if (selectedItem != null)
            {
                selectedItem.InsidePropChange = false;
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
        }

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (ProjectJournalLineLocal)sender;
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

                case "Project":
                    var pro = (ProjectClient)ProjectCache.Get(rec._Project);
                    if (pro != null)
                    {
                        if (pro._Dim1 != null) rec.Dimension1 = pro._Dim1;
                        if (pro._Dim2 != null) rec.Dimension2 = pro._Dim2;
                        if (pro._Dim3 != null) rec.Dimension3 = pro._Dim3;
                        if (pro._Dim4 != null) rec.Dimension4 = pro._Dim4;
                        if (pro._Dim5 != null) rec.Dimension5 = pro._Dim5;
                        if (rec._PrCategory != null && rec._Project != null)
                            getCostAndSales(rec);
                        setTask(pro, rec);
                    }
                    break;
                case "PrCategory":
                    if (rec._PrCategory != null && rec._Project != null)
                        getCostAndSales(rec);
                    break;

                case "Employee":
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

                case "Warehouse":
                    if (warehouse != null)
                    {
                        var selected = (InvWarehouse)warehouse.Get(rec._Warehouse);
                        setLocation(selected, rec);
                    }
                    break;

                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;

                case "TimeFrom":
                case "TimeTo":
                    if (rec._TimeTo < rec._TimeFrom)
                    {
                        rec._TimeTo = 0;
                        rec._Qty = 0;
                    }
                    else
                        rec._Qty = Convert.ToDouble(rec._TimeTo - rec._TimeFrom) / 60;
                    break;

                case "Qty":
                    rec._TimeFrom = 0;
                    rec._TimeTo = 0;
                    break;
            }
        }

        async void setLocation(InvWarehouse master, ProjectJournalLineLocal rec)
        {
            if (!string.IsNullOrEmpty(rec._Warehouse) && master != null)
                rec.locationSource = master.Locations ?? await master.LoadLocations(api);
            else
            {
                rec.locationSource = null;
                rec.Location = null;
            }
            rec.NotifyPropertyChanged("LocationSource");
        }

        async void setTask(Uniconta.DataModel.Project project, ProjectJournalLineLocal rec)
        {
            if (!string.IsNullOrEmpty(rec._Project) && project != null)
                rec.taskSource = project.Tasks ?? await project.LoadTasks(api);
            else
            {
                rec.taskSource = null;
                rec.Task = null;
            }
            rec.NotifyPropertyChanged("TaskSource");
        }

        async void PayrollCat(ProjectJournalLineLocal rec, bool AddItem)
        {
            var pay = (Uniconta.DataModel.EmpPayrollCategory)PayrollCache.Get(rec._PayrollCategory);
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

            var rate = (from ct in Rates where ct._Employee == rec._Employee select ct).FirstOrDefault();
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
                rec.SetCost(costPrice);
            if (salesPrice != 0d)
                rec.SetSales(salesPrice);
        }

        void SetItem(ProjectJournalLineLocal rec)
        {
            var item = (InvItem)ItemsCache.Get(rec._Item);
            if (item == null)
                return;

            rec.SetCost(item._CostPrice);
            rec.SetSales(item._SalesPrice1);

            if (item._Dim1 != null) rec.Dimension1 = item._Dim1;
            if (item._Dim2 != null) rec.Dimension2 = item._Dim2;
            if (item._Dim3 != null) rec.Dimension3 = item._Dim3;
            if (item._Dim4 != null) rec.Dimension4 = item._Dim4;
            if (item._Dim5 != null) rec.Dimension5 = item._Dim5;
            if (item._Warehouse != null) rec.Warehouse = item._Warehouse;
            if (item._Location != null) rec.Location = item._Location;

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

        async void getCostAndSales(ProjectJournalLineLocal rec)
        {
            Boolean isValid = false;
            var proj = (Uniconta.DataModel.Project)ProjectCache.Get(rec._Project);
            var Categories = proj.Categories ?? await proj.LoadCategories(api);

            rec.costPct = 0d; rec.salesPct = 0d; rec.costAmount = 0d; rec.salesAmount = 0d;

            var Category = rec._PrCategory;
            var projCat = (from ct in Categories where ct._PrCategory == Category select ct).FirstOrDefault();
            if (projCat != null)
            {
                isValid = true;
                rec.costPct = projCat._CostPctCharge;
                rec.salesPct = projCat._SalesPctCharge;
                rec.costAmount = projCat._CostAmountCharge;
                rec.salesAmount = projCat._SalesAmountCharge;
            }
            else
            {
                var prstd = (PrStandard)PrStandardCache.Get(proj._PrStandard);
                if (prstd == null)
                    return;
                var PrCategories = prstd.Categories ?? await prstd.LoadCategories(api);

                var prCat = (from ct in PrCategories where ct._PrCategory == Category select ct).FirstOrDefault();
                if (prCat != null)
                {
                    isValid = true;
                    rec.costPct = prCat._CostPctCharge;
                    rec.salesPct = prCat._SalesPctCharge;
                    rec.costAmount = prCat._CostAmountCharge;
                    rec.salesAmount = prCat._SalesAmountCharge;
                }
            }
            if (isValid == false)
                rec.PrCategory = null;
        }

        private void Task_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgProjectJournalLinePageGrid.SelectedItem as ProjectJournalLineLocal;
            if (selectedItem?._Project != null)
            {
                var selected = (Uniconta.DataModel.Project)ProjectCache.Get(selectedItem._Project);
                setTask(selected, selectedItem);
            }
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    var row = dgProjectJournalLinePageGrid.AddRow() as ProjectJournalLineLocal;
                    if (row != null)
                        row.Employee = this.CurrentEmployee;
                    break;
                case "CopyRow":
                    dgProjectJournalLinePageGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgProjectJournalLinePageGrid.DeleteRow();
                    break;
                case "CheckJournal":
                    CheckJournal();
                    break;
                case "PostJournal":
                    PostJournal();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        private async void CheckJournal()
        {
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
            busyIndicator.IsBusy = true;

            var lst = (IList)dgProjectJournalLinePageGrid.ItemsSource;
            var cnt = lst.Count;

            this.api.AllowBackgroundCrud = false;
            var savetask = saveGrid();
            if (savetask != null)
                await savetask;
            this.api.AllowBackgroundCrud = true;

            var postingRes = await postingApi.CheckJournal(masterJournal, BasePage.GetSystemDefaultDate(), false, null, cnt);
            busyIndicator.IsBusy = false;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            if (postingRes.Err == ErrorCodes.Succes)
            {
                string msg = Uniconta.ClientTools.Localization.lookup("JournalOK");
                UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
            }
            else
                Utility.ShowJournalError(postingRes, dgProjectJournalLinePageGrid);
        }

        private void PostJournal()
        {
            this.api.AllowBackgroundCrud = false;
            var savetask = saveGrid();
            this.api.AllowBackgroundCrud = true;

            CWPosting postingDialog = new CWPosting(masterJournal);
            postingDialog.Closed += async delegate
            {
                if (postingDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;

                    if (savetask != null)
                        await savetask;

                    var source = (IList)dgProjectJournalLinePageGrid.ItemsSource;
                    var cnt = source.Count;

                    Task<PostingResult> task;

                    if (postingDialog.IsSimulation)
                        task = postingApi.CheckJournal(masterJournal, postingDialog.PostedDate, true, new GLTransClientTotal(), cnt);
                    else
                        task = postingApi.PostDailyJournal(masterJournal, postingDialog.PostedDate, postingDialog.comments, cnt);

                    var postingResult = await task;

                    busyIndicator.IsBusy = false;
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
                    if (postingResult == null)
                        return;

                    if (postingResult.Err != ErrorCodes.Succes)
                        Utility.ShowJournalError(postingResult, dgProjectJournalLinePageGrid);

                    else if (postingDialog.IsSimulation && postingResult.SimulatedTrans != null)
                        AddDockItem(TabControls.SimulatedTransactions, postingResult.SimulatedTrans, Uniconta.ClientTools.Localization.lookup("SimulatedTransactions"), null, true);
                    else
                    {
                        string msg;
                        if (postingResult.JournalPostedlId != 0)
                            msg = string.Format("{0} {1}={2}", Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted"), Uniconta.ClientTools.Localization.lookup("JournalPostedId"), postingResult.JournalPostedlId);
                        else
                            msg = Uniconta.ClientTools.Localization.lookup("JournalHasBeenPosted");
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Message"));
                        if (masterJournal._DeleteLines)
                        {
                            var lst = new List<ProjectJournalLineLocal>();
                            dgProjectJournalLinePageGrid.ItemsSource = lst;
                        }
                    }
                }
            };
            postingDialog.Show();
        }

        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (warehouse == null)
                return;
            var selectedItem = dgProjectJournalLinePageGrid.SelectedItem as ProjectJournalLineLocal;
            if (selectedItem == null)
                return;
            if (selectedItem._Warehouse != null && warehouse != null)
            {
                var selected = (InvWarehouse)warehouse.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }

        private async void cmbCategoryType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (gridControl.HasUnsavedData)
                await saveGrid();
            var catgoryType = AppEnums.CategoryType.IndexOf(this.cmbCategoryType.EditValue.ToString());
            ShowHideColumns(catgoryType);
            var res = await api.Query<ProjectJournalLineLocal>(dgProjectJournalLinePageGrid.masterRecords, null);
            var categoryRows = res.ToList();
            if (res == null)
                return;
            var catCache = new PrCategoryCacheFilter(CategoryCache, (CategoryType)catgoryType);
            foreach (var row in res)
            {
                if (CategoryCache != null)
                {
                    var rowCatCache = CategoryCache.Get(row.PrCategory) as PrCategory;
                    if (rowCatCache != null && (int)rowCatCache._CatType != catgoryType)
                    {
                        categoryRows.Remove(row);
                        continue;
                    }
                    row.PrCategorySource = catCache;
                }
            }
            dgProjectJournalLinePageGrid.SetSource(categoryRows.ToArray());
        }

        void ShowHideColumns(int categoryType)
        {
            foreach (var col in dgProjectJournalLinePageGrid.GetVisibleColumns())
            {
                col.Visible = false;
            }
            switch (categoryType)
            {
                case (int)CategoryType.Materials:
                    colDate.Visible = Employee.Visible = Project.Visible = Task.Visible = PrCategory.Visible = Voucher.Visible = Text.Visible = Item.Visible = Warehouse.Visible = Location.Visible = Qty.Visible = CostPrice.Visible = SalesPrice.Visible = coldim1.Visible = coldim2.Visible = coldim3.Visible = coldim4.Visible = coldim5.Visible = true;
                    break;
                case (int)CategoryType.ExternalWork:
                    colDate.Visible = Employee.Visible = Project.Visible = Task.Visible = PrCategory.Visible = Voucher.Visible = Text.Visible = CreditorAccount.Visible = Qty.Visible = CostPrice.Visible = SalesPrice.Visible = coldim1.Visible = coldim2.Visible = coldim3.Visible = coldim4.Visible = coldim5.Visible = true;
                    break;
                case (int)CategoryType.Labour:
                    colDate.Visible = Employee.Visible = Project.Visible = Task.Visible = PrCategory.Visible = Voucher.Visible = Text.Visible = TimeFrom.Visible = TimeTo.Visible = Qty.Visible = CostPrice.Visible = SalesPrice.Visible = coldim1.Visible = coldim2.Visible = coldim3.Visible = coldim4.Visible = coldim5.Visible = true;
                    break;
                case (int)CategoryType.Expenses:
                    colDate.Visible = Employee.Visible = Project.Visible = Task.Visible = PrCategory.Visible = Voucher.Visible = Text.Visible = CreditorAccount.Visible = Qty.Visible = CostPrice.Visible = SalesPrice.Visible = coldim1.Visible = coldim2.Visible = coldim3.Visible = coldim4.Visible = coldim5.Visible = true;
                    break;
            }
            //if (hasProjectMaster)
            //    Project.Visible = false;
        }
    }
}

