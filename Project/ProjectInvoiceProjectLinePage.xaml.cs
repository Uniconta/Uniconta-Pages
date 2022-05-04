using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectInvoiceProjectLineLocal : ProjectInvoiceProjectLineClient
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

        public PrCategoryCacheFilter PrCategorySource { get; internal set; }

        internal object _prCategorySource;
        public object ProjectCategorySource { get { return _prCategorySource; } }
    }

    public class ProjectInvoiceProjectLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectInvoiceProjectLineLocal); } }
        public override IComparer GridSorting { get { return new DCProjectOrderLineSort(); } }
        public override string LineNumberProperty { get { return "_LineNumber"; } }
        public override bool AllowSort { get { return false; } }
        public override bool Readonly { get { return false; } }
        public override bool IsAutoSave { get { return false; } }

        public override bool AddRowOnPageDown()
        {
            var selectedItem = (ProjectInvoiceProjectLineLocal)this.SelectedItem;
            if (selectedItem == null || selectedItem._Project == null || (selectedItem._Qty == 0d && selectedItem._SalesPrice == 0d && selectedItem._CostPrice == 0d))
                return false;
            return true;
        }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
<<<<<<<< HEAD:Project/ProjectInvoiceProjectLinePage.xaml.cs
            var newRow = (ProjectInvoiceProjectLineLocal)dataEntity;
            var header = this.masterRecord as Uniconta.DataModel.DebtorOrder;
========
            var newRow = (DebtorOrderProjectLineLocal)dataEntity;
            var header = this.masterRecord as Uniconta.DataModel.DCOrder;
>>>>>>>> 1ce1cb5446e7c9fec3f9092522f9f26e7a727d8e:Debtor/DebtorOrderProjectLinePage.xaml.cs
            if (header != null)
            {
                newRow.SetMaster((UnicontaBaseEntity)header);
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
                ProjectInvoiceProjectLineLocal last = null;
                ProjectInvoiceProjectLineLocal Cur = null;
                int n = -1;
                DateTime LastDateTime = DateTime.MinValue;
                var castItem = lst as IEnumerable<ProjectInvoiceProjectLineLocal>;
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

    public partial class ProjectInvoiceProjectLinePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.ProjectInvoiceProjectLinePage; } }

        SQLCache ItemsCache, ProjectCache, CategoryCache, PrStandardCache;
        Dictionary<string, Uniconta.API.DebtorCreditor.FindPrices> dictPriceLookup;
<<<<<<<< HEAD:Project/ProjectInvoiceProjectLinePage.xaml.cs
        ProjectInvoiceProposal invoiceProposal;
        public ProjectInvoiceProjectLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }

        public ProjectInvoiceProjectLinePage(SynchronizeEntity syncEntity)
          : base(syncEntity, true)
        {
            if (syncEntity != null)
                InitPage(syncEntity.Row);
        }

        private void InitPage(UnicontaBaseEntity master)
========
        DCOrder debtorOrder;
        public DebtorOrderProjectLinePage(UnicontaBaseEntity master) : base(master)
>>>>>>>> 1ce1cb5446e7c9fec3f9092522f9f26e7a727d8e:Debtor/DebtorOrderProjectLinePage.xaml.cs
        {
            InitPage(master);
        }

        public DebtorOrderProjectLinePage(SynchronizeEntity syncEntity)
           : base(syncEntity, true)
        {
            if (syncEntity != null)
                InitPage(syncEntity.Row);
        }

        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgProjInvProjectLineGrid;
            SetRibbonControl(localMenu, dgProjInvProjectLineGrid);
            dgProjInvProjectLineGrid.api = api;
            dgProjInvProjectLineGrid.UpdateMaster(master);
            dgProjInvProjectLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgProjInvProjectLineGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dictPriceLookup = new Dictionary<string, Uniconta.API.DebtorCreditor.FindPrices>();
<<<<<<<< HEAD:Project/ProjectInvoiceProjectLinePage.xaml.cs
            invoiceProposal = master as ProjectInvoiceProposal;
========
            debtorOrder = master as DCOrder;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (rb != null)
                UtilDisplay.RemoveMenuCommand(rb, "Adjustment");
>>>>>>>> 1ce1cb5446e7c9fec3f9092522f9f26e7a727d8e:Debtor/DebtorOrderProjectLinePage.xaml.cs
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, coldim1, coldim2, coldim3, coldim4, coldim5);
        }

        public async override Task InitQuery()
        {
<<<<<<<< HEAD:Project/ProjectInvoiceProjectLinePage.xaml.cs
            await dgProjInvProjectLineGrid.Filter(null);
            // do not reload, since lines has opdated order we pass, and then SQL version might not be up to date
            //if (invoiceProposal != null)
            //    await api.Read(invoiceProposal);
            var itemSource = (IList)dgProjInvProjectLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgProjInvProjectLineGrid.AddFirstRow();
========
            await dgDebtorOrderProjectLineGrid.Filter(null);
            // do not reload, since lines has opdated order we pass, and then SQL version might not be up to date
            //if (debtorOrder != null)
            //    await api.Read((UnicontaBaseEntity)debtorOrder);
            var itemSource = (IList)dgDebtorOrderProjectLineGrid.ItemsSource;
            if (itemSource == null || itemSource.Count == 0)
                dgDebtorOrderProjectLineGrid.AddFirstRow();
>>>>>>>> 1ce1cb5446e7c9fec3f9092522f9f26e7a727d8e:Debtor/DebtorOrderProjectLinePage.xaml.cs
            RecalculateAmount();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
<<<<<<<< HEAD:Project/ProjectInvoiceProjectLinePage.xaml.cs
            dgProjInvProjectLineGrid.UpdateMaster(args);
            invoiceProposal = dgProjInvProjectLineGrid.masterRecord as ProjectInvoiceProposal;
            if (invoiceProposal != null)
            {
                api.Read(invoiceProposal);
                SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("ProjectAdjustments"), ": ", NumberConvert.ToString(invoiceProposal._OrderNumber)));
========
            dgDebtorOrderProjectLineGrid.UpdateMaster(args);
            debtorOrder = dgDebtorOrderProjectLineGrid.masterRecord as Uniconta.DataModel.DCOrder;
            if (debtorOrder != null)
            {
                api.Read((UnicontaBaseEntity)debtorOrder);
                SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("ProjectAdjustments"), ": ", NumberConvert.ToString(debtorOrder._OrderNumber)));
>>>>>>>> 1ce1cb5446e7c9fec3f9092522f9f26e7a727d8e:Debtor/DebtorOrderProjectLinePage.xaml.cs
            }
            InitQuery();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgProjInvProjectLineGrid.AddRow();
                    break;
                case "CopyRow":
                    dgProjInvProjectLineGrid.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    CloseDockItem();
                    break;
                case "DeleteRow":
                    dgProjInvProjectLineGrid.DeleteRow();
                    break;
                case "UpdatePrices":
                    UpdatePrices();
                    break;
                case "Adjustment":
                    AdjustTransactionPerEmployee();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
            RecalculateAmount();
        }
        async void AdjustTransactionPerEmployee()
        {
            // var propValuePair = new List<PropValuePair>() { PropValuePair.GenereteWhereElements("SendToOrder", invoiceProposal._OrderNumber, CompareOperator.Equal) };
            var trans = await api.Query<ProjectTransClient>(invoiceProposal);
            if (trans != null)
            {
                int n;
                ProjectTransClient t;
                double Amountsum = 0;
                for (n = 0; (n < trans.Length); n++)
                {
                    t = trans[n];
                    if (t._Employee != null && t._PrCategory != null && ((PrCategory)CategoryCache.Get(t._PrCategory))._CatType == CategoryType.Labour)
                        Amountsum += t._SalesAmount;
                    else
                        trans[n] = null;
                }
                double adjustment = invoiceProposal._OrderTotal - invoiceProposal._ProjectTotal;
                PrCategory adjustmentcategory = null;
                foreach (var cat in (Uniconta.DataModel.PrCategory[])CategoryCache.GetNotNullArray)
                {
                    if (cat._CatType == CategoryType.Adjustment)
                    {
                        adjustmentcategory = cat;
                        break;
                    }
                }

                // we need to delete all rows in grid
                dgProjInvProjectLineGrid.DeleteAllRows();

                // Now all lines are deleted (not in SQL, just in corasauDataGrid)
                // Now we will insert new lines in corasauDataGrid
                // On 'save' old lines will be delete and new lines added.

                double adjustmentAdded = 0;
                int pos = 0;
                ProjectInvoiceProjectLineLocal line = null;
                for (n = 0; (n < trans.Length); n++)
                {
                    t = trans[n];
                    if (t != null)
                    {
                        var price = Math.Round(adjustment / Amountsum * t._SalesAmount, 2);
                        adjustmentAdded += price;
                        line = ((IEnumerable<ProjectInvoiceProjectLineLocal>)dgProjInvProjectLineGrid.ItemsSource).Where(e => e.Employee == t.Employee).FirstOrDefault();
                        if (line == null)
                        {
                            line = new ProjectInvoiceProjectLineLocal();
                            line.PrCategory = adjustmentcategory?.KeyStr;
                            line._Qty = 1;
                            line._SalesPrice = price;
                            line._Employee = t._Employee;
                            line._Dim1 = t._Dim1;
                            line._Dim2 = t._Dim2;
                            line._Dim3 = t._Dim3;
                            line._Dim4 = t._Dim4;
                            line._Dim5 = t._Dim5;
                            dgProjInvProjectLineGrid.InsertRow(line, pos++);
                        }
                        else
                        {
                            line.SalesPrice += price;
                        }
                    }
                }
                if (line != null)
                    line.SalesPrice += (adjustment - adjustmentAdded);

                dgProjInvProjectLineGrid.ShowTotalSummary();
                RecalculateAmount();
            }
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            ProjectInvoiceProjectLineLocal oldselectedItem = e.OldItem as ProjectInvoiceProjectLineLocal;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= SelectedItem_PropertyChanged;

            ProjectInvoiceProjectLineLocal selectedItem = e.NewItem as ProjectInvoiceProjectLineLocal;
            if (selectedItem != null)
            {
                selectedItem.InsidePropChange = false;
                selectedItem.PropertyChanged += SelectedItem_PropertyChanged;
            }
        }

        private void SelectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = (ProjectInvoiceProjectLineLocal)sender;
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
                    }
                    break;
                case "PrCategory":
                    getCostAndSales(rec);
                    break;
                case "Qty":
                    UpdatePrice(rec);
                    break;
            }
            RecalculateAmount();
        }

        void SetItem(ProjectInvoiceProjectLineLocal rec)
        {
            var item = (InvItem)ItemsCache.Get(rec._Item);
            if (item == null)
                return;

            //  SetPriceLookup(rec)?.SetPriceFromItem(rec, item);

            if (item._Dim1 != null) rec.Dimension1 = item._Dim1;
            if (item._Dim2 != null) rec.Dimension2 = item._Dim2;
            if (item._Dim3 != null) rec.Dimension3 = item._Dim3;
            if (item._Dim4 != null) rec.Dimension4 = item._Dim4;
            if (item._Dim5 != null) rec.Dimension5 = item._Dim5;
            if (item._PrCategory != null)
                rec.PrCategory = item._PrCategory;
        }

        async void getCostAndSales(ProjectInvoiceProjectLineLocal rec)
        {
            var project = rec._Project;
            if (project == null)
                return;
            var proj = (Uniconta.DataModel.Project)ProjectCache.Get(project);
            var Categories = proj.Categories ?? await proj.LoadCategories(api);

            rec.costPct = 0d; rec.salesPct = 0d; rec.costAmount = 0d; rec.salesAmount = 0d;

            var Category = rec._PrCategory;

            var prcategory = (Uniconta.DataModel.PrCategory)CategoryCache.Get(Category);
            if (prcategory != null && prcategory._Forward)
<<<<<<<< HEAD:Project/ProjectInvoiceProjectLinePage.xaml.cs
                rec.ProjectForward = invoiceProposal._Project;
========
                rec.ProjectForward = debtorOrder._Project;
>>>>>>>> 1ce1cb5446e7c9fec3f9092522f9f26e7a727d8e:Debtor/DebtorOrderProjectLinePage.xaml.cs

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

                var prCat = (from ct in PrCategories where ct._PrCategory == Category select ct).FirstOrDefault();
                if (prCat != null)
                {
                    rec.costPct = prCat._CostPctCharge;
                    rec.salesPct = prCat._SalesPctCharge;
                    rec.costAmount = prCat._CostAmountCharge;
                    rec.salesAmount = prCat._SalesAmountCharge;
                }
            }
            RecalculateAmount();
        }

        void UpdatePrices()
        {
            var source = dgProjInvProjectLineGrid.GetVisibleRows() as IEnumerable<ProjectInvoiceProjectLineLocal>;
            foreach (var rec in source)
            {
                dgProjInvProjectLineGrid.SetLoadedRow(rec);
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
                    if (cat != null)
                        rec.PrCategory = cat;
                }
                else
                    getCostAndSales(rec);

                dgProjInvProjectLineGrid.SetModifiedRow(rec);
                rec.InsidePropChange = false;
            }
        }

        void UpdatePrice(ProjectInvoiceProjectLineLocal rec)
        {
            var priceLookup = SetPriceLookup(rec);
            //if (priceLookup != null && priceLookup.UseCustomerPrices)
            //    priceLookup.GetCustomerPrice(rec, false);
        }

        Uniconta.API.DebtorCreditor.FindPrices SetPriceLookup(ProjectInvoiceProjectLineLocal rec)
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
            CategoryCache = api.GetCache(typeof(Uniconta.DataModel.PrCategory)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PrCategory)).ConfigureAwait(false);
            ProjectCache = api.GetCache(typeof(Uniconta.DataModel.Project)) ?? await api.LoadCache(typeof(Uniconta.DataModel.Project)).ConfigureAwait(false);
            ItemsCache = api.GetCache(typeof(Uniconta.DataModel.InvItem)) ?? await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            PrStandardCache = api.GetCache(typeof(Uniconta.DataModel.PrStandard)) ?? await api.LoadCache(typeof(Uniconta.DataModel.PrStandard)).ConfigureAwait(false);
        }

        void RecalculateAmount()
        {
<<<<<<<< HEAD:Project/ProjectInvoiceProjectLinePage.xaml.cs
            var lst = dgProjInvProjectLineGrid.ItemsSource as IEnumerable<ProjectInvoiceProjectLineLocal>;
            if (lst == null)
                return;
            double adjustment = invoiceProposal._OrderTotal - invoiceProposal._ProjectTotal;
========
            var lst = dgDebtorOrderProjectLineGrid.ItemsSource as IEnumerable<DebtorOrderProjectLineLocal>;
            if (lst == null)
                return;
            double adjustment = debtorOrder._OrderTotal - debtorOrder._ProjectTotal;
>>>>>>>> 1ce1cb5446e7c9fec3f9092522f9f26e7a727d8e:Debtor/DebtorOrderProjectLinePage.xaml.cs
            double Amountsum = lst.Sum(x => x._SalesAmount);
            double difference = adjustment - Amountsum;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            var groups = UtilDisplay.GetMenuCommandsByStatus(rb, true);
            var adjust = Uniconta.ClientTools.Localization.lookup("Adjustment");
            var strTotal = Uniconta.ClientTools.Localization.lookup("Total");
            var diff = Uniconta.ClientTools.Localization.lookup("Diff");
            foreach (var grp in groups)
            {
                if (grp.Caption == adjust)
                    grp.StatusValue = adjustment.ToString("N2");
                if (grp.Caption == strTotal)
                    grp.StatusValue = Amountsum.ToString("N2");
                if (grp.Caption == diff)
                    grp.StatusValue = difference.ToString("N2");
            }
        }

        private void SetPrCategorySource(ProjectInvoiceProjectLineLocal rec)
        {
            if (CategoryCache != null && CategoryCache.Count != 0)
            {
                rec._prCategorySource = new PrCategoryRegulationFilter(CategoryCache);
                rec.NotifyPropertyChanged("ProjectCategorySource");
            }
        }

        private void PrCategory_LostFocus(object sender, RoutedEventArgs e)
        {
            SetPrCategoryByLookupText(sender, true);
        }

        void SetPrCategoryByLookupText(object sender, bool isHours)
        {
            var selectedItem = dgProjInvProjectLineGrid.SelectedItem as ProjectInvoiceProjectLineLocal;
            if (selectedItem == null)
                return;
            var le = sender as CorasauGridLookupEditor;
            if (string.IsNullOrEmpty(le.EnteredText))
                return;

            var prCat = CategoryCache?.Get(le.EnteredText);
            if (prCat != null)
            {
<<<<<<<< HEAD:Project/ProjectInvoiceProjectLinePage.xaml.cs
                dgProjInvProjectLineGrid.SetLoadedRow(selectedItem);
========
                dgDebtorOrderProjectLineGrid.SetLoadedRow(selectedItem);
>>>>>>>> 1ce1cb5446e7c9fec3f9092522f9f26e7a727d8e:Debtor/DebtorOrderProjectLinePage.xaml.cs
                selectedItem.PrCategory = prCat.KeyStr;
                le.EditValue = prCat.KeyStr;
            }
            le.EnteredText = null;
        }

        CorasauGridLookupEditorClient prevPrCategory;
        private void PrCategory_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgProjInvProjectLineGrid.SelectedItem as ProjectInvoiceProjectLineLocal;
            if (selectedItem != null)
            {
                SetPrCategorySource(selectedItem);
                if (prevPrCategory != null)
                    prevPrCategory.isValidate = false;
                var editor = (CorasauGridLookupEditorClient)sender;
                prevPrCategory = editor;
                editor.isValidate = true;
            }
        }
    }
}
