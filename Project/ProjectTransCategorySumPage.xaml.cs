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
using Uniconta.ClientTools.Util;
using UnicontaClient.Utilities;
using System.ComponentModel.DataAnnotations;
using Uniconta.ClientTools;
using Uniconta.DataModel;
using DevExpress.Xpf.Grid;
using DevExpress.Data;
using Uniconta.API.Service;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class ProjectTransCategorySort : IComparer<ProjectTransCategorySumClientLocal>
    {
        public int Compare(ProjectTransCategorySumClientLocal x, ProjectTransCategorySumClientLocal y)
        {
            var c = string.Compare(x._Project, y._Project);
            if (c != 0)
                return c;
            c = string.Compare(x._PrCategory, y._PrCategory);
            if (c != 0)
                return c;
            c = string.Compare(x._Employee, y._Employee);
            if (c != 0)
                return c;
            return string.Compare(x._Item, y._Item);
        }
    }

    public class ProjectTransCategorySumGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(ProjectTransCategorySumClientLocal); } }
        public override bool Readonly { get { return true; } }
        public override IComparer GridSorting { get { return new ProjectTransCategorySumClientSort(); } }
    }
    /// <summary>
    /// Interaction logic for ProjectTransCategorySumPage.xaml
    /// </summary>
    public partial class ProjectTransCategorySumPage : GridBasePage
    {
        CWServerFilter proTransFilterDialog;
        bool proTransFilterCleared;

        ItemBase ibase;
        ItemBase iBudgetbase, iIncludeSubProBase;
        static bool showBudget = true;
        static bool includeSubProject;
        static int InvoicedTrans;
        private SynchronizeEntity syncEntity;
        static DateTime fromDate, toDate;
        public ProjectTransCategorySumPage(UnicontaBaseEntity master)
            : base(master)
        {
            Init(master);
        }

        public ProjectTransCategorySumPage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }

        public ProjectTransCategorySumPage(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Init(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgProjectTransCategorySum.UpdateMaster(args);
            SetHeader();
            LoadGrid();
        }
        void SetHeader()
        {
            var syncMaster = dgProjectTransCategorySum.masterRecord as Uniconta.DataModel.Project;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}/{1}", Localization.lookup("ProjectCategorySum"), syncMaster._Number);
            SetHeader(header);
        }
        UnicontaBaseEntity master;
        private void Init(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            master = _master;
            SetRibbonControl(localMenu, dgProjectTransCategorySum);
            dgProjectTransCategorySum.api = api;
            dgProjectTransCategorySum.UpdateMaster(master);
            dgProjectTransCategorySum.BusyIndicator = busyIndicator;
            dgProjectTransCategorySum.CustomSummary += dgProjectTransCategorySum_CustomSummary;
            dgProjectTransCategorySum.ShowTotalSummary();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            localMenu.OnChecked += LocalMenu_OnChecked;
            GetMenuItem();
            FromDate.DateTime = fromDate;
            ToDate.DateTime = toDate;
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            if (master == null)
                UtilDisplay.RemoveMenuCommand(rb, "InclSubProjects");
        }

        public override Task InitQuery()
        {
            if (master != null)
                return ShowInculdeSubProject();
            else
                return LoadGrid();
        }

        private void LocalMenu_OnChecked(string actionType, bool IsChecked)
        {
            includeSubProject = IsChecked;
            ShowInculdeSubProject();
        }

        double sumQty, sumCost, sumSales, sumMargin, sumBudgetQty, sumBudgetCost, sumBudgetSales, sumMarginRatio, sumSalesValue, sumMarginValue;
        double sumInvoicedQty, sumInvoiced, sumBudgetInvoicedQty, sumBudgetInvoiced;
        bool OnlySum;

        void Set0()
        {
            sumQty = sumCost = sumSales = sumMargin = sumBudgetQty = sumBudgetCost = sumBudgetSales = sumSalesValue = sumMarginValue = 0d;
            sumInvoicedQty = sumInvoiced = sumBudgetInvoicedQty = sumBudgetInvoiced = 0d;
        }
        private void dgProjectTransCategorySum_CustomSummary(object sender, DevExpress.Data.CustomSummaryEventArgs e)
        {
            var fieldName = ((GridSummaryItem)e.Item).FieldName;
            switch (e.SummaryProcess)
            {
                case CustomSummaryProcess.Start:
                    Set0();
                    OnlySum = true;
                    break;
                case CustomSummaryProcess.Calculate:
                    var row = e.Row as ProjectTransCategorySumClientLocal;
                    if (row == null)
                        break;
                    if (row._CatType != CategoryType.Sum)
                    {
                        if (OnlySum)
                        {
                            OnlySum = false;
                            Set0();
                        }
                    }
                    else if (!OnlySum)
                        break;

                    double val = (double)e.FieldValue;
                    switch (fieldName)
                    {
                        case "Qty": sumQty += val; break;
                        case "Cost": sumCost += val; break;
                        case "Sales": sumSales += val; break;
                        case "Margin": sumMargin += val; break;
                        case "BudgetQty": sumBudgetQty += val; break;
                        case "BudgetCost": sumBudgetCost += val; break;
                        case "BudgetSales": sumBudgetSales += val; break;
                        case "Invoiced": sumInvoiced += val; break;
                        case "BudgetInvoiced": sumBudgetInvoiced += val; break;
                        case "InvoicedQty": sumInvoicedQty += val; break;
                        case "BudgetInvoicedQty": sumBudgetInvoicedQty += val; break;
                    }

                    sumSalesValue += row.Sales;
                    sumMarginValue += row.Margin;
                    break;
                case CustomSummaryProcess.Finalize:
                    switch (fieldName)
                    {
                        case "Qty": e.TotalValue = sumQty; break;
                        case "Cost": e.TotalValue = sumCost; break;
                        case "Sales": e.TotalValue = sumSales; break;
                        case "Margin": e.TotalValue = sumMargin; break;
                        case "BudgetQty": e.TotalValue = sumBudgetQty; break;
                        case "BudgetCost": e.TotalValue = sumBudgetCost; break;
                        case "BudgetSales": e.TotalValue = sumBudgetSales; break;
                        case "Invoiced": e.TotalValue = sumInvoiced; break;
                        case "BudgetInvoiced": e.TotalValue = sumBudgetInvoiced; break;
                        case "InvoicedQty": e.TotalValue = sumInvoicedQty; break;
                        case "BudgetInvoicedQty": e.TotalValue = sumBudgetInvoicedQty; break;
                    }
                    if (fieldName == "MarginRatio" && sumSalesValue > 0)
                    {
                        sumMarginRatio = 100 * sumMarginValue / sumSalesValue;
                        e.TotalValue = sumMarginRatio;
                    }
                    break;
            }
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (master == null)
                Project.Visible = true;
            if (!showBudget) // this is default and if user has removed in layout, we do not want to set it
            {
                BudgetQty.Visible = false;
                BudgetCost.Visible = false;
                BudgetSales.Visible = false;
                BudgetInvoiced.Visible = false;
                QtyDiff.Visible = false;
                CostDiff.Visible = false;
                SalesDiff.Visible = false;
            }
            if (api.CompanyEntity.HideCostPrice)
            {
                CostDiff.Visible = CostDiff.ShowInColumnChooser = 
                Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
                BudgetCost.Visible = BudgetCost.ShowInColumnChooser = Cost.Visible = Cost.ShowInColumnChooser = false;
            }
        }

        string[] invoicedTrans;
        void GetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            ibase = UtilDisplay.GetMenuCommandByName(rb, "GroupByCategoryType");
            iBudgetbase = UtilDisplay.GetMenuCommandByName(rb, "ShowBudget");
            iIncludeSubProBase = UtilDisplay.GetMenuCommandByName(rb, "InclSubProjects");

            var comboItem = UtilDisplay.GetMenuCommandByName(rb, "InvoicedTrans");
            if (comboItem != null)
            {
                invoicedTrans = new string[] { Localization.lookup("All"), Localization.lookup("Invoiced"), Localization.lookup("NotInvoiced") };
                comboItem.ComboBoxItemSource = invoicedTrans;
                comboItem.SelectedItem = invoicedTrans[InvoicedTrans]; // show value that is saved in the static
                localMenu.OnSelectedIndexChanged += LocalMenu_OnSelectedIndexChanged;
            }
            ShowBudget();
        }

        private void LocalMenu_OnSelectedIndexChanged(string ActionType, string SelectedItem)
        {
            InvoicedTrans = Array.IndexOf(invoicedTrans, SelectedItem);
        }

        async Task LoadGrid()
        {
            fromDate = FromDate.DateTime;
            toDate = ToDate.DateTime;

            List<PropValuePair> filter = new List<PropValuePair>();
            if (includeSubProject)
            {
                var propValuePairFolder = PropValuePair.GenereteParameter("IncludeSubProject", typeof(string), "1");
                filter.Add(propValuePairFolder);
            }
            if (InvoicedTrans > 0)
            {
                var propValuePairFolder = PropValuePair.GenereteParameter("InvoicedTrans", typeof(string), NumberConvert.ToString(InvoicedTrans));
                filter.Add(propValuePairFolder);
            }
            if (fromDate != DateTime.MinValue)
            {
                var propValuePairFolder = PropValuePair.GenereteParameter("FromDate", typeof(string), NumberConvert.ToString(fromDate.Ticks));
                filter.Add(propValuePairFolder);
            }
            if (toDate != DateTime.MinValue)
            {
                var propValuePairFolder = PropValuePair.GenereteParameter("ToDate", typeof(string), NumberConvert.ToString(toDate.Ticks));
                filter.Add(propValuePairFolder);
            }

            var api = this.api;
            var CompanyId = api.CompanyId;

            var cats = api.CompanyEntity.GetCache(typeof(PrCategory)) ?? await api.CompanyEntity.LoadCache(typeof(PrCategory), api);
            var transTask = api.Query(new ProjectTransCategorySumClientLocal(), dgProjectTransCategorySum.masterRecords, filter);

            var trans = await transTask;
            if (trans == null)
            {
                dgProjectTransCategorySum.SetSource(null);
                return;
            }

            var len = trans.Length;
            var sort = new ProjectTransCategorySort();
            Array.Sort(trans, sort);

            List<ProjectTransCategorySumClientLocal> extras = null, sums = new List<ProjectTransCategorySumClientLocal>();

            if (showBudget)
            {
                var budget = await api.Query(new ProjectBudgetCategorySumClient(), dgProjectTransCategorySum.masterRecords, filter);
                var key = new ProjectTransCategorySumClientLocal();
                foreach (var bc in budget)
                {
                    key._Project = bc._Project;
                    key._PrCategory = bc._PrCategory;
                    var idx = Array.BinarySearch(trans, key, sort);
                    if (idx >= 0 && idx < len)
                    {
                        var t = trans[idx];
                        t._BudgetSales += bc._Sales;
                        t._BudgetCost += bc._Cost;
                        t._BudgetQty += bc._Qty;
                    }
                    else
                    {
                        var prTrans = new ProjectTransCategorySumClientLocal() { _CompanyId = CompanyId, _BudgetSales = bc._Sales, _BudgetCost = bc._Cost, _BudgetQty = bc._Qty, _PrCategory = bc._PrCategory, _Project = bc._Project };

                        var cat = (PrCategory)cats.Get(bc._PrCategory);
                        prTrans._CatType = cat._CatType;
                        if (cat._CatType == CategoryType.Sum || cat._CatType == CategoryType.Header)
                            sums.Add(prTrans);
                        else
                        {
                            if (extras == null)
                                extras = new List<ProjectTransCategorySumClientLocal>();
                            extras.Add(prTrans);
                        }
                    }
                }
                if (extras != null)
                {
                    Array.Resize(ref trans, len + extras.Count);
                    foreach (var sum in extras)
                        trans[len++] = sum;
                    Array.Sort(trans, sort);
                    extras = null;
                }
            }

            foreach (var t in trans)
            {
                var cat = (PrCategory)cats.Get(t._PrCategory);
                if (cat != null && (cat._CatType == CategoryType.Revenue || cat._CatType == CategoryType.OnAccountInvoicing))
                {
                    t._InvoicedQty = -t._Qty;
                    t._Invoiced = -t._Sales;
                    t._BudgetInvoicedQty = -t._BudgetQty;
                    t._BudgetInvoiced = -t._BudgetSales;

                    t._Qty= 0;
                    t._BudgetQty = 0;
                    t._Cost = 0;
                    t._Sales = 0;
                    t._BudgetCost = 0;
                    t._BudgetSales = 0;
                }
            }

            int start = 0;
            while (start < len)
            {
                int end;
                string ProjectNumber;
                if (master == null)
                {
                    ProjectNumber = trans[start]._Project;
                    for (end = start; (end < len && trans[end]._Project == ProjectNumber); end++)
                        ;
                }
                else
                {
                    ProjectNumber = ((Uniconta.DataModel.Project)master)._Number;
                    end = len;
                }

                int headerAddedLast = 0;
                foreach (var cat in (PrCategory[])cats.GetKeyStrRecords)
                {
                    if (cat != null && (cat._CatType == CategoryType.Sum || cat._CatType == CategoryType.Header))
                    {
                        PropValuePair SumList = AccountSum.Generate(cat._Sum);
                        if (SumList != null)
                        {
                            double Sales = 0, Cost = 0, Qty = 0, BudgetSales = 0, BudgetQty = 0, BudgetCost = 0, Invoiced = 0, InvoicedBudget = 0, InvoicedQty = 0, InvoicedBudgetQty = 0;
                            for (int j = start; j < end; j++)
                            {
                                var Acc2 = trans[j];
                                if (AccountSum.IsIncluded(SumList, Acc2._PrCategory))
                                {
                                    Sales += Acc2._Sales;
                                    Cost += Acc2._Cost;
                                    Qty += Acc2._Qty;
                                    BudgetCost += Acc2._BudgetCost;
                                    BudgetSales += Acc2._BudgetSales;
                                    BudgetQty += Acc2._BudgetQty;

                                    Invoiced += Acc2._Invoiced;
                                    InvoicedBudget += Acc2._BudgetInvoiced;
                                    InvoicedQty += Acc2._InvoicedQty;
                                    InvoicedBudgetQty += Acc2._BudgetInvoicedQty;
                                }
                            }

                            var sum = new ProjectTransCategorySumClientLocal() { _CompanyId = CompanyId, _Project = ProjectNumber, _PrCategory = cat._Number, _CatType = cat._CatType };

                            sum._Qty = Math.Round(Qty, 2);
                            sum._BudgetQty = Math.Round(BudgetQty, 2);
                            sum._Sales = Math.Round(Sales, 2);
                            sum._Cost = Math.Round(Cost, 2);
                            sum._BudgetSales = Math.Round(BudgetSales, 2);
                            sum._BudgetCost = Math.Round(BudgetCost, 2);
                            sum._Invoiced = Math.Round(Invoiced, 2);
                            sum._BudgetInvoiced = Math.Round(InvoicedBudget, 2);
                            sum._InvoicedQty = Math.Round(InvoicedQty, 2);
                            sum._BudgetInvoicedQty = Math.Round(InvoicedBudgetQty, 2);
                            if (sum._Qty != 0 || sum._BudgetQty != 0 || sum._Sales != 0 || sum._Cost != 0 || sum._BudgetSales != 0 ||
                                sum._BudgetCost != 0 || sum._Invoiced != 0 || sum._BudgetInvoiced != 0 || sum._InvoicedQty != 0 || sum._BudgetInvoicedQty != 0)
                            {
                                sums.Add(sum);
                                headerAddedLast = 0;
                            }
                            else if (cat._CatType == CategoryType.Header)
                            {
                                sums.Add(sum);
                                headerAddedLast++;
                            }
                        }
                        else if (cat._CatType == CategoryType.Header)
                        {
                            sums.Add(new ProjectTransCategorySumClientLocal() { _CompanyId = CompanyId, _Project = ProjectNumber, _PrCategory = cat._Number, _CatType = CategoryType.Header });
                            headerAddedLast++;
                        }
                    }
                }
                if (headerAddedLast > 0)
                    sums.RemoveRange(sums.Count - headerAddedLast, headerAddedLast);
                start = end;
            }

            if (sums.Count > 0)
            {
                Array.Resize(ref trans, len + sums.Count);
                foreach (var sum in sums)
                    trans[len++] = sum;
                Array.Sort(trans, sort);
            }

            dgProjectTransCategorySum.SetSource(trans);
        }

        bool group = true;
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "GroupByCategoryType":
                    GroupByCatType();
                    break;
                case "ShowBudget":
                    showBudget = !showBudget;
                    ShowBudget();
                    if (showBudget)
                        LoadGrid();
                    break;
                case "RefreshGrid":
                    if (master != null)
                        LoadGrid();
                    else
                        BindGrid();
                    break;
                case "ProjectTransFilter":
                    if (proTransFilterDialog == null)
                    {
                        if (proTransFilterCleared)
                            proTransFilterDialog = new CWServerFilter(api, typeof(ProjectTransClient), null, null, null);
                        else
                            proTransFilterDialog = new CWServerFilter(api, typeof(ProjectTransClient), null, null, null);
                        proTransFilterDialog.GridSource = dgProjectTransCategorySum.ItemsSource as IList<UnicontaBaseEntity>;
                        proTransFilterDialog.Closing += ProTransFilterDialog_Closing;
                        proTransFilterDialog.Show();
                    }
                    else
                    {
                        proTransFilterDialog.GridSource = dgProjectTransCategorySum.ItemsSource as IList<UnicontaBaseEntity>;
                        proTransFilterDialog.Show(true);
                    }
                    break;
                case "ClearProjectTransFilter":
                    proTransFilterDialog = null;
                    proTransFilterValues = null;
                    proTransFilterCleared = true;
                    BindGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public IEnumerable<PropValuePair> proTransFilterValues;
        public FilterSorter proTransPropSort;

        void ProTransFilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (proTransFilterDialog.DialogResult == true)
            {
                proTransFilterValues = proTransFilterDialog.PropValuePair;
                proTransPropSort = proTransFilterDialog.PropSort;
                BindGrid();
            }
#if !SILVERLIGHT
            e.Cancel = true;
            proTransFilterDialog.Hide();
#endif
        }

        private Task Filter(IEnumerable<PropValuePair> propValuePair)
        {
            return dgProjectTransCategorySum.Filter(propValuePair);
        }
        private void BindGrid()
        {
            if (proTransFilterValues == null)
                LoadGrid();
            else
                Filter(proTransFilterValues);
        }

        void ShowBudget()
        {
            if (showBudget)
            {
                BudgetQty.Visible = true;
                BudgetCost.Visible = true;
                BudgetSales.Visible = true;
                QtyDiff.Visible = true;
                CostDiff.Visible = true;
                SalesDiff.Visible = true;
                iBudgetbase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("HideOBJ"), Uniconta.ClientTools.Localization.lookup("Budget"));
                iBudgetbase.LargeGlyph = Utility.GetGlyph("Clear_Budget_32x32");
            }
            else
            {
                BudgetQty.Visible = false;
                BudgetCost.Visible = false;
                BudgetSales.Visible = false;
                QtyDiff.Visible = false;
                CostDiff.Visible = false;
                SalesDiff.Visible = false;
                iBudgetbase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("ViewOBJ"), Uniconta.ClientTools.Localization.lookup("Budget"));
                iBudgetbase.LargeGlyph = Utility.GetGlyph("Budget_32x32");
            }

        }

        Task ShowInculdeSubProject()
        {
            iIncludeSubProBase.IsChecked = includeSubProject;
            return LoadGrid();
        }

        private void GroupByCatType()
        {
            if (dgProjectTransCategorySum.ItemsSource == null) return;
            if (ibase == null) return;
            if (group)
            {
                group = false;
                CatType.GroupIndex = 1;
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("MenuColumnUnGroup"), Uniconta.ClientTools.Localization.lookup("Type"));
                ibase.LargeGlyph = Utility.GetGlyph("Group_by_close-32x32");
            }
            else
            {
                group = true;
                ibase.Caption = string.Format(Uniconta.ClientTools.Localization.lookup("GroupByOBJ"), Uniconta.ClientTools.Localization.lookup("Type"));
                ibase.LargeGlyph = Utility.GetGlyph("Group_by_32x32");
                CatType.GroupIndex = -1;
                CatType.Visible = true;
            }
        }
    }

    public class ProjectTransCategorySumClientLocal : ProjectTransCategorySumClient
    {
        [Display(Name = "QtyDiff", ResourceType = typeof(ProjectTransClientText))]
        public double QtyDiff { get { return _BudgetQty - Qty; } }

        [Display(Name = "SalesDiff", ResourceType = typeof(ProjectTransClientText))]
        public double SalesDiff { get { return _BudgetSales - _Sales; } }

        [Display(Name = "CostDiff", ResourceType = typeof(ProjectTransClientText))]
        public double CostDiff { get { return _BudgetCost - _Cost; } }

        public double _BudgetInvoiced;
        [Display(Name = "BudgetInvoiced", ResourceType = typeof(ProjectTransClientText))]
        public double BudgetInvoiced { get { return _BudgetInvoiced; } }

        public double _Invoiced;
        [Display(Name = "Invoiced", ResourceType = typeof(ProjectTransClientText))]
        public double Invoiced { get { return _Invoiced; } }

        public double _BudgetInvoicedQty;
        [Display(Name = "BudgetInvoicedQty", ResourceType = typeof(ProjectTransClientText))]
        public double BudgetInvoicedQty { get { return _BudgetInvoicedQty; } }

        public double _InvoicedQty;
        [Display(Name = "InvoicedQty", ResourceType = typeof(ProjectTransClientText))]
        public double InvoicedQty { get { return _InvoicedQty; } }
    }
}
