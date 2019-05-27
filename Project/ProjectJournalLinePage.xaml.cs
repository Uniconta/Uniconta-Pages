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
using Uniconta.API.Project;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectJournalLineLocal : ProjectJournalLineClient
    {
        internal bool InsidePropChange;
        public double costPct, salesPct, costAmount, salesAmount;

        public void SetCost(double cost)
        {
            if (cost != 0d)
                this.CostPrice = Math.Round(cost * (1d + costPct / 100d) + costAmount, 2);
        }
        public void SetSales(double sales)
        {
            if (sales != 0d)
                this.SalesPrice = Math.Round(sales * (1d + salesPct / 100d) + salesAmount, 2);
        }

        internal object locationSource;
        public object LocationSource { get { return locationSource; } }

        internal object taskSource;
        public object TaskSource { get { return taskSource; } }

        public PrCategoryCacheFilter PrCategorySource { get; internal set; }
    }

    public class ProjectJournalLinePageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectJournalLineLocal); } }
        public override IComparer GridSorting { get { return new ProjectJournalLineSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return true; } }

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (ProjectJournalLineLocal)this.SelectedItem;
            if (selectedItem == null || selectedItem._Project == null || (selectedItem._Qty == 0d && selectedItem._SalesPrice == 0d && selectedItem._CostPrice == 0d))
                return false;
            return true;
        }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            var newRow = (ProjectJournalLineLocal)dataEntity;
            var header = this.masterRecord as Uniconta.DataModel.PrJournal;
            if (header != null)
            {
                newRow.SetMaster(header);
                newRow._Dim1 = header._Dim1;
                newRow._Dim2 = header._Dim2;
                newRow._Dim3 = header._Dim3;
                newRow._Dim4 = header._Dim4;
                newRow._Dim5 = header._Dim5;
                newRow._Employee = header._Employee;
                newRow._TransType = header._TransType;
            }

            var lst = (IList)this.ItemsSource;
            if (lst == null || lst.Count == 0)
            {
                newRow._Date = BasePage.GetSystemDefaultDate().Date;
            }
            else
            {
                ProjectJournalLineLocal last = null;
                ProjectJournalLineLocal Cur = null;
                int n = -1;
                DateTime LastDateTime = DateTime.MinValue;
                var castItem = lst as IEnumerable<ProjectJournalLineLocal>;
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
                newRow._Invoiceable = last._Invoiceable;
                newRow.PrCategorySource = last.PrCategorySource;
            }
        }
    }

    public partial class ProjectJournalLinePage : GridBasePage
    {
        SQLCache ItemsCache, ProjectCache, CreditorCache, CategoryCache, EmployeeCache, WarehouseCache, PayrollCache, PrStandardCache;
        UnicontaAPI.Project.API.PostingAPI postingApi;
        Uniconta.DataModel.PrJournal masterJournal;
        Dictionary<string, Uniconta.API.DebtorCreditor.FindPrices> dictPriceLookup;

        public ProjectJournalLinePage(UnicontaBaseEntity master)
            : base(master)
        {
            InitializeComponent();
            masterJournal = (Uniconta.DataModel.PrJournal)master;
            localMenu.dataGrid = dgProjectJournalLinePageGrid;
            SetRibbonControl(localMenu, dgProjectJournalLinePageGrid);
            dgProjectJournalLinePageGrid.api = api;
            dgProjectJournalLinePageGrid.UpdateMaster(masterJournal);
            dgProjectJournalLinePageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjectJournalLinePageGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            postingApi = new UnicontaAPI.Project.API.PostingAPI(api);
            dictPriceLookup = new Dictionary<string, Uniconta.API.DebtorCreditor.FindPrices>();
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = false;
            useBinding = true;
            return true;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Approved.Visible = masterJournal._UseApproved;
            Approved.ShowInColumnChooser = !masterJournal._UseApproved;
            var Comp = api.CompanyEntity;
            if (!Comp.Payroll)
            {
                PayrollCategory.Visible = false;
                PayrollCategory.ShowInColumnChooser = false;
            }
            if (!Comp.ProjectTask)
            {
                Task.Visible = false;
                Task.ShowInColumnChooser = false;
            }
            if (!Comp.Warehouse)
            {
                Warehouse.Visible = false;
                Warehouse.ShowInColumnChooser = false;
            }
            if (!Comp.Location)
            {
                Location.Visible = false;
                Location.ShowInColumnChooser = false;
            }
            UnicontaClient.Utilities.Utility.SetupVariants(api, colVariant, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
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
                        getCostAndSales(rec);
                        rec.InsidePropChange = false;
                    }
                    break;
                case "Project":
                    var pro = (Uniconta.DataModel.Project)ProjectCache.Get(rec._Project);
                    if (pro != null)
                    {
                        if (pro._Dim1 != null) rec.Dimension1 = pro._Dim1;
                        if (pro._Dim2 != null) rec.Dimension2 = pro._Dim2;
                        if (pro._Dim3 != null) rec.Dimension3 = pro._Dim3;
                        if (pro._Dim4 != null) rec.Dimension4 = pro._Dim4;
                        if (pro._Dim5 != null) rec.Dimension5 = pro._Dim5;
                        getCostAndSales(rec);
                        setTask(pro, rec);
                    }
                    break;
                case "PrCategory":
                    getCostAndSales(rec);
                    SetInvoiceable(rec);
                    break;
                case "Employee":
                    if (rec._Employee != null)
                    {
                        var emp = (Uniconta.DataModel.Employee)EmployeeCache?.Get(rec._Employee);
                        if (emp?._PayrollCategory != null)
                            rec.PayrollCategory = emp._PayrollCategory;
                        if (!rec.InsidePropChange)
                        {
                            rec.InsidePropChange = true;
                            PayrollCat(rec, true); //rec.InsidePropChange = false; done inside method
                        }
                    }
                    break;
                case "PayrollCategory":
                    if (rec._Employee != null && rec._PayrollCategory != null)
                    {
                        if (!rec.InsidePropChange)
                        {
                            rec.InsidePropChange = true;
                            PayrollCat(rec, true); //rec.InsidePropChange = false; done inside method
                        }
                    }
                    break;
                case "Warehouse":
                    if (WarehouseCache != null)
                    {
                        var selected = (InvWarehouse)WarehouseCache.Get(rec._Warehouse);
                        setLocation(selected, rec);
                    }
                    break;
                case "Location":
                    if (string.IsNullOrEmpty(rec._Warehouse))
                        rec._Location = null;
                    break;
                case "TimeFrom":
                case "TimeTo":
                    if (rec._TimeTo >= rec._TimeFrom)
                        rec.Qty = (rec._TimeTo - rec._TimeFrom) / 60d;
                    break;

                case "Qty":
                    UpdatePrice(rec);
                    if (rec._Qty != (rec._TimeTo - rec._TimeFrom) / 60d)
                    {
                        rec._TimeFrom = 0;
                        rec._TimeTo = 0;
                    }
                    break;
            }
        }
        async void setLocation(InvWarehouse master, ProjectJournalLineLocal rec)
        {
            if (api.CompanyEntity.Location)
            {
                if (master != null)
                    rec.locationSource = master.Locations ?? await master.LoadLocations(api);
                else
                {
                    rec.locationSource = null;
                    rec.Location = null;
                }
                rec.NotifyPropertyChanged("LocationSource");
            }
        }

        async void setTask(Uniconta.DataModel.Project project, ProjectJournalLineLocal rec)
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

        private void Task_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgProjectJournalLinePageGrid.SelectedItem as ProjectJournalLineLocal;
            if (selectedItem?._Project != null)
            {
                var selected = (Uniconta.DataModel.Project)ProjectCache.Get(selectedItem._Project);
                setTask(selected, selectedItem);
            }
        }

        void SetCat(ProjectJournalLineLocal rec, string cat)
        {
            if (cat != null && cat != rec._PrCategory)
            {
                rec.PrCategory = cat;
                SetInvoiceable(rec);
            }
        }

        void SetInvoiceable(ProjectJournalLineLocal rec)
        {
            var Cat = (PrCategory)CategoryCache.Get(rec._PrCategory);
            if (Cat != null)
                rec.Invoiceable = Cat._Invoiceable;
        }

        async void PayrollCat(ProjectJournalLineLocal rec, bool AddItem)
        {
            var pay = (Uniconta.DataModel.EmpPayrollCategory)PayrollCache?.Get(rec._PayrollCategory);
            if (pay == null)
                return;

            if (pay._Unit != 0 && rec._Unit != pay._Unit)
            {
                rec._Unit = pay._Unit;
                rec.NotifyPropertyChanged("Unit");
            }
            if (AddItem && !pay._Invoiceable)
                rec.Invoiceable = false;

            if (pay._PrCategory != null)
                rec.PrCategory = pay._PrCategory;

            double costPrice = pay._Rate, salesPrice = pay._SalesPrice;
            string Item = pay._Item;
            if (pay._Dim1 != null) rec.Dimension1 = pay._Dim1;
            if (pay._Dim2 != null) rec.Dimension2 = pay._Dim2;
            if (pay._Dim3 != null) rec.Dimension3 = pay._Dim3;
            if (pay._Dim4 != null) rec.Dimension4 = pay._Dim4;
            if (pay._Dim5 != null) rec.Dimension5 = pay._Dim5;

            var Rates = pay.Rates ?? await pay.LoadRates(api);

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
                rec.SetCost(costPrice);
            if (salesPrice != 0d)
                rec.SetSales(salesPrice);

            rec.InsidePropChange = false;
        }

        void SetItem(ProjectJournalLineLocal rec)
        {
            var item = (InvItem)ItemsCache.Get(rec._Item);
            if (item == null)
                return;

            SetPriceLookup(rec)?.SetPriceFromItem(rec, item);

            if (item._Dim1 != null) rec.Dimension1 = item._Dim1;
            if (item._Dim2 != null) rec.Dimension2 = item._Dim2;
            if (item._Dim3 != null) rec.Dimension3 = item._Dim3;
            if (item._Dim4 != null) rec.Dimension4 = item._Dim4;
            if (item._Dim5 != null) rec.Dimension5 = item._Dim5;
            if (item._Warehouse != null) rec.Warehouse = item._Warehouse;
            if (item._Location != null) rec.Location = item._Location;

            SetCat(rec, item._PrCategory);
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
            var project = rec._Project;
            if (project == null)
                return;
            var proj = (Uniconta.DataModel.Project)ProjectCache.Get(project);
            var Categories = proj.Categories ?? await proj.LoadCategories(api);

            rec.costPct = 0d; rec.salesPct = 0d; rec.costAmount = 0d; rec.salesAmount = 0d;
            if (Categories == null)
                return;

            var Category = rec._PrCategory;
            var projCat = (from ct in Categories where ct._PrCategory == Category select ct).FirstOrDefault();
            if (projCat != null)
            {
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
                if (PrCategories == null)
                    return;

                var prCat = (from ct in PrCategories where ct._PrCategory == Category select ct).FirstOrDefault();
                if (prCat != null)
                {
                    rec.costPct = prCat._CostPctCharge;
                    rec.salesPct = prCat._SalesPctCharge;
                    rec.costAmount = prCat._CostAmountCharge;
                    rec.salesAmount = prCat._SalesAmountCharge;
                }
            }
        }

        void UpdatePrice(ProjectJournalLineLocal rec)
        {
            var priceLookup = SetPriceLookup(rec);
            if (priceLookup != null && priceLookup.UseCustomerPrices)
                priceLookup.GetCustomerPrice(rec, false);
        }

        Uniconta.API.DebtorCreditor.FindPrices SetPriceLookup(ProjectJournalLineLocal rec)
        {
            var proj = (Uniconta.DataModel.Project)ProjectCache.Get(rec._Project);
            if (proj != null)
            {
                if (dictPriceLookup.ContainsKey(proj._DCAccount))
                    return dictPriceLookup[proj._DCAccount];

                var order = new DebtorOrder() { _DCAccount = proj._DCAccount };
                var priceLookup = new Uniconta.API.DebtorCreditor.FindPrices(order, api);
                dictPriceLookup.Add(proj._DCAccount, priceLookup);
                return priceLookup;
            }
            return null;
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            ProjectCache = Comp.GetCache(typeof(Uniconta.DataModel.Project)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Project), api).ConfigureAwait(false);
            ItemsCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api).ConfigureAwait(false);
            CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Creditor), api).ConfigureAwait(false);
            CategoryCache = Comp.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.PrCategory), api).ConfigureAwait(false);
            EmployeeCache = Comp.GetCache(typeof(Uniconta.DataModel.Employee)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.Employee), api).ConfigureAwait(false);
            if (Comp.Payroll)
                PayrollCache = Comp.GetCache(typeof(Uniconta.DataModel.EmpPayrollCategory)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.EmpPayrollCategory), api).ConfigureAwait(false);
            PrStandardCache = Comp.GetCache(typeof(Uniconta.DataModel.PrStandard)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.PrStandard), api).ConfigureAwait(false);
            if (Comp.Warehouse)
                WarehouseCache = Comp.GetCache(typeof(Uniconta.DataModel.InvWarehouse)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.InvWarehouse), api).ConfigureAwait(false);
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgProjectJournalLinePageGrid.SelectedItem as ProjectJournalLineLocal;
            switch (ActionType)
            {
                case "AddRow":
                    dgProjectJournalLinePageGrid.AddRow();
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
                case "UpdatePrices":
                    UpdatePrices();
                    break;
                case "Storage":
                    ViewStorage();
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

        async void UnfoldBOM(ProjectJournalLineLocal selectedItem)
        {
            var items = this.ItemsCache;
            var item = (InvItem)items.Get(selectedItem._Item);
            if (item == null || item._ItemType != (byte)ItemType.ProductionBOM)
                return;

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            var list = await api.Query<InvBOM>(selectedItem);
            if (list != null && list.Length > 0)
            {
                Array.Sort(list, new InvBOMSort());
                var lst = new List<UnicontaBaseEntity>();
                foreach (var bom in list)
                {
                    var invJournalLine = new ProjectJournalLineLocal();
                    invJournalLine._Item = bom._ItemPart;
                    item = (InvItem)items.Get(bom._ItemPart);
                    invJournalLine.SetItemValues(item);
                    invJournalLine._Date = selectedItem._Date;
                    invJournalLine._Dim1 = selectedItem._Dim1;
                    invJournalLine._Dim2 = selectedItem._Dim2;
                    invJournalLine._Dim3 = selectedItem._Dim3;
                    invJournalLine._Dim4 = selectedItem._Dim4;
                    invJournalLine._Dim5 = selectedItem._Dim5;
                    invJournalLine._Variant1 = bom._Variant1;
                    invJournalLine._Variant2 = bom._Variant2;
                    invJournalLine._Variant3 = bom._Variant3;
                    invJournalLine._Variant4 = bom._Variant4;
                    invJournalLine._Variant5 = bom._Variant5;
                    invJournalLine._Warehouse = item._Warehouse;
                    invJournalLine._Location = item._Location;
                    invJournalLine._CostPrice = item._CostPrice;
                    invJournalLine._Qty = -Math.Round(bom.GetBOMQty(selectedItem._Qty), item._Decimals);
                    lst.Add(invJournalLine);
                }
                dgProjectJournalLinePageGrid.PasteRows(lst);
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

        async void ViewStorage()
        {
            var savetask = saveGridLocal(); // we need to wait until it is saved, otherwise Storage is not updated
            if (savetask != null)
                await savetask;
            AddDockItem(TabControls.InvItemStoragePage, dgProjectJournalLinePageGrid.syncEntity, true);
        }

        bool refreshOnHand;
        Task<ErrorCodes> saveGridLocal()
        {
            var prOrderLine = dgProjectJournalLinePageGrid.SelectedItem as ProjectJournalLineLocal;
            refreshOnHand = prOrderLine != null && prOrderLine.RowId == 0;
            dgProjectJournalLinePageGrid.SelectedItem = null;
            dgProjectJournalLinePageGrid.SelectedItem = prOrderLine;
            if (dgProjectJournalLinePageGrid.HasUnsavedData)
                return saveGrid();
            return null;
        }

        void UpdatePrices()
        {
            var source = dgProjectJournalLinePageGrid.GetVisibleRows() as IEnumerable<ProjectJournalLineLocal>;
            foreach (var rec in source)
            {
                dgProjectJournalLinePageGrid.SetLoadedRow(rec);
                var pro = (Uniconta.DataModel.Project)ProjectCache.Get(rec._Project);
                if (pro != null)
                {
                    if (pro._Dim1 != null) rec.Dimension1 = pro._Dim1;
                    if (pro._Dim2 != null) rec.Dimension2 = pro._Dim2;
                    if (pro._Dim3 != null) rec.Dimension3 = pro._Dim3;
                    if (pro._Dim4 != null) rec.Dimension4 = pro._Dim4;
                    if (pro._Dim5 != null) rec.Dimension5 = pro._Dim5;
                }

                rec.InsidePropChange = true;
                if (rec._Item != null)
                {
                    var cat = rec._PrCategory;
                    SetItem(rec);
                    SetCat(rec, cat);
                }
                else
                {
                    getCostAndSales(rec);
                    if (rec._Employee != null)
                    {
                        var emp = (Uniconta.DataModel.Employee)EmployeeCache?.Get(rec._Employee);
                        if (emp?._PayrollCategory != null)
                            rec.PayrollCategory = emp._PayrollCategory;
                        if (!rec.InsidePropChange)
                        {
                            rec.InsidePropChange = true;
                            PayrollCat(rec, true); //rec.InsidePropChange = false; done inside method
                        }
                    }
                    else if (rec._PayrollCategory != null)
                        PayrollCat(rec, true);
                }
                dgProjectJournalLinePageGrid.SetModifiedRow(rec);
                rec.InsidePropChange = false;
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
#if !SILVERLIGHT
            postingDialog.DialogTableId = 2000000040;
#endif
            postingDialog.Closed += async delegate
            {
                if (postingDialog.DialogResult == true)
                {
                    busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("SendingWait");
                    busyIndicator.IsBusy = true;

                    if (savetask != null)
                        await savetask;

                    var source = dgProjectJournalLinePageGrid.ItemsSource as IEnumerable<ProjectJournalLineLocal>;
                    var cnt = source.Count();

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
                            var EmptyAccountOnHold = masterJournal._EmptyAccountOnHold;
                            var UseApproved = masterJournal._UseApproved;
                            var lst = new List<ProjectJournalLineLocal>();
                            foreach (var journalLine in source)
                                if (journalLine._OnHold || (journalLine._Project == null && EmptyAccountOnHold) || (UseApproved && !journalLine._Approved))
                                    lst.Add(journalLine);

                            dgProjectJournalLinePageGrid.ItemsSource = lst;
                        }
                    }
                }
            };
            postingDialog.Show();
        }

        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            if (WarehouseCache == null)
                return;

            var selectedItem = dgProjectJournalLinePageGrid.SelectedItem as ProjectJournalLineLocal;
            if (selectedItem?._Warehouse != null )
            {
                var selected = (InvWarehouse)WarehouseCache.Get(selectedItem._Warehouse);
                setLocation(selected, selectedItem);
            }
        }

        protected override bool LoadTemplateHandledLocally(IEnumerable<UnicontaBaseEntity> templateRows)
        {
            foreach (var gl in (IEnumerable<Uniconta.DataModel.PrJournalLine>)templateRows)
            {
                gl._Date = DateTime.MinValue;
                gl._Voucher = 0;
            }
            return false;
        }
    }
}
