using Uniconta.API.Inventory;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Grid.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.ClientTools.Util;
using Uniconta.API.Service;
using System.Windows.Data;
using DevExpress.Data.Filtering;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InventoryStatementGrid : CorasauDataGridClient
    {
        public delegate void PrintClickDelegate();

        public override Type TableType { get { return typeof(InvTransClientTotal); } }

        public bool PageBreak { get; internal set; }

        public InventoryStatementGrid(IDataControlOriginationElement dataControlOriginationElement) : base(dataControlOriginationElement) { }
        public InventoryStatementGrid()
        {
            CustomTableView tv = new CustomTableView();
            tv.AllowEditing = false;
            tv.ShowGroupPanel = false;
            SetTableViewStyle(tv);
            tv.ShowTotalSummary = true;
            tv.ShowGroupFooters = true;
            this.View = tv;
        }
        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            ((CustomTableView)View).HasPageBreak = PageBreak;
            base.PrintGrid(reportName, printparam, format, page);
        }
    }

    public class InvItemStatementList
    {
        internal InvItem itm;
        [Display(Name = "Item", ResourceType = typeof(InventoryText))]
        public string ItemNumber { get { return itm._Item; } }
        [Display(Name = "Name", ResourceType = typeof(InventoryText))]
        public string Name { get { return itm._Name; } }
        [Display(Name = "ItemType", ResourceType = typeof(InventoryText))]
        public string ItemType { get { return AppEnums.ItemType.ToString(itm._ItemType); } }
        public InvTransClientTotal[] ChildRecord { get; set; }

        public long _SumCost;
        public double SumCost { get { return _SumCost / 100d; } }

        public double _SumQty;
        public double SumQty { get { return _SumQty; } }
    }

    public class InvTransClientTotal : InvTransClient
    {
        public long _SumCost;
        public double _SumQty;

        [Display(Name = "SumCost", ResourceType = typeof(InvTransText))]
        public double SumCost { get { return _SumCost / 100d; } }

        [Display(Name = "SumQty", ResourceType = typeof(InvTransText))]
        public double SumQty { get { return _SumQty; } }
    }

    public partial class InventoryStatement : GridBasePage
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItem))]
        public string FromItem { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItem))]
        public string ToItem { get; set; }

        SQLCache ItemCache;
        ItemBase ibase;
        public override string NameOfControl { get { return TabControls.InventoryStatement; } }

        static DateTime DefaultFromDate, DefaultToDate;

        public static void setDateTime(DateEditor frmDateeditor, DateEditor todateeditor)
        {
            if (frmDateeditor.Text == string.Empty)
                DefaultFromDate = DateTime.MinValue;
            else
                DefaultFromDate = frmDateeditor.DateTime.Date;
            if (todateeditor.Text == string.Empty)
                DefaultToDate = DateTime.MinValue;
            else
                DefaultToDate = todateeditor.DateTime.Date;
        }

        public InventoryStatement(BaseAPI API) : base(API, string.Empty)
        {
            this.DataContext = this;
            InitializeComponent();
            cmbFromAccount.api = cmbToAccount.api = api;
            SetRibbonControl(localMenu, dgInvTran);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            if (InventoryStatement.DefaultFromDate == DateTime.MinValue)
            {
                var Now = BasePage.GetSystemDefaultDate();
                InventoryStatement.DefaultToDate = Now;
                InventoryStatement.DefaultFromDate = Now.AddDays(1 - Now.Day).AddMonths(-2);
            }
            var Pref = api.session.Preference;
            cbxAscending.IsChecked = Pref.Inventory_isAscending;
            cbxSkipBlank.IsChecked = Pref.Inventory_skipBlank;

            txtDateTo.DateTime = InventoryStatement.DefaultToDate;
            txtDateFrm.DateTime = InventoryStatement.DefaultFromDate;

            ItemCache = api.GetCache(typeof(Uniconta.DataModel.InvItem));
            if (ItemCache == null)
                api.LoadCache(typeof(Uniconta.DataModel.InvItem));
 
            GetMenuItem();
        }

        void MasterRowExpanded(object sender, RowEventArgs e)
        {
            var detailView = GetDetailView(e.RowHandle);
            if (detailView == null)
                return;
            detailView.ShowSearchPanelMode = ShowSearchPanelMode.Never;
            detailView.SearchPanelHighlightResults = true;
            BindingOperations.SetBinding(detailView, DataViewBase.SearchStringProperty, new System.Windows.Data.Binding("SearchText") { Source = ribbonControl.SearchControl });
        }

        TableView GetDetailView(int rowHandle)
        {
            var detail = childDgInvTrans.GetDetail(rowHandle) as GridControl;
            return detail == null ? null : detail.View as TableView;
        }

        void SubstituteFilter(object sender, DevExpress.Data.SubstituteFilterEventArgs e)
        {
            if (string.IsNullOrEmpty(ribbonControl?.SearchControl?.SearchText))
                return;
            e.Filter = new GroupOperator(GroupOperatorType.Or, e.Filter, GetDetailFilter(ribbonControl.SearchControl.SearchText));
        }

        List<OperandProperty> operands;
        AggregateOperand GetDetailFilter(string searchString)
        {
            if (operands == null)
            {
                var visibleColumns = childDgInvTrans.Columns.Where(c => c.Visible).Select(c => string.IsNullOrEmpty(c.FieldName) ? c.Name : c.FieldName);
                operands = new List<OperandProperty>();
                foreach (var col in visibleColumns)
                    operands.Add(new OperandProperty(col));
            }
            GroupOperator detailOperator = new GroupOperator(GroupOperatorType.Or);
            foreach (var op in operands)
                detailOperator.Operands.Add(new FunctionOperator(FunctionOperatorType.Contains, op, new OperandValue(searchString)));
            return new AggregateOperand("ChildRecord", Aggregate.Exists, detailOperator);
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var Comp = api.CompanyEntity;
            if (!Comp.Location || !Comp.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!Comp.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            if (!Comp.Project)
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
            if (Comp.HideCostPrice)
            {
                CostPrice.Visible = CostPrice.ShowInColumnChooser = false;
                CostValue.Visible = CostValue.ShowInColumnChooser = false;
            }
            Utilities.Utility.SetupVariants(api, colVariant, VariantName, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
        }

        public override void AssignMultipleGrid(List<Uniconta.ClientTools.Controls.CorasauDataGrid> gridCtrls)
        {
            gridCtrls.Add(dgInvTran);
            gridCtrls.Add(childDgInvTrans);
            isChildGridExist = true;
        }

        private async void LoadInvTran()
        {
            InventoryStatement.setDateTime(txtDateFrm, txtDateTo);
            DateTime FromDate = InventoryStatement.DefaultFromDate, ToDate = InventoryStatement.DefaultToDate;

            var fromItem = Convert.ToString(cmbFromAccount.EditValue);
            var toItem = Convert.ToString(cmbToAccount.EditValue);

            var transApi = new ReportAPI(api);
            busyIndicator.IsBusy = true;
            var listtran = (InvTransClientTotal[])await transApi.GetInvTrans(new InvTransClientTotal(), FromDate, ToDate, fromItem, toItem);
            if (listtran != null)
                FillStatement(listtran);
            else if (transApi.LastError != 0)
            {
                busyIndicator.IsBusy = false;
                UtilDisplay.ShowErrorCode(transApi.LastError);
            }
            dgInvTran.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
            if (statementList != null && statementList.Count == 1)
                ExpandAndCollapseAll(false);
        }
        public override object GetPrintParameter()
        {
            for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                dgInvTran.ExpandMasterRow(rowHandle);
            return base.GetPrintParameter();
        }

        List<InvItemStatementList> statementList;
        void FillStatement(InvTransClientTotal[] listtran)
        {
            var isAscending = cbxAscending.IsChecked.GetValueOrDefault();
            var skipBlank = cbxSkipBlank.IsChecked.GetValueOrDefault();

            var Pref = api.session.Preference;
            Pref.Inventory_isAscending = isAscending;
            Pref.Inventory_skipBlank = skipBlank;

            statementList = new List<InvItemStatementList>(Math.Min(20, dataRowCount));

            if (ItemCache == null)
                ItemCache = api.LoadCache(typeof(Uniconta.DataModel.InvItem)).GetAwaiter().GetResult();
            string curItem = " ";
            InvItemStatementList ob = null;
            var tlst = new List<InvTransClientTotal>(100);
            long SumCost = 0;
            double SumQty = 0;

            for (int n = 0; (n < listtran.Length); n++)
            {
                var t = listtran[n];
                if (t._Item != curItem)
                {
                    var ac = ItemCache.Get(t._Item) as InvItem;
                    if (ac == null)
                        continue;

                    if (ob != null)
                    {
                        if (!skipBlank || SumCost != 0 || SumQty != 0 || tlst.Count > 1)
                        {
                            statementList.Add(ob);
                            ob._SumQty = Math.Round(SumQty, ob.itm._Decimals);
                            ob._SumCost = SumCost;
                            ob.ChildRecord = tlst.ToArray();
                            if (!isAscending)
                                Array.Reverse(ob.ChildRecord);
                        }
                    }
                    tlst.Clear();
                    
                    curItem = ac._Item;
                    ob = new InvItemStatementList() { itm = ac };
                    SumCost = 0;
                    SumQty = 0d;
                }
                t._Item = curItem;
                SumCost += t._CostValueCent;
                t._SumCost = SumCost;

                SumQty += t._Qty;
                t._SumQty = SumQty;

                tlst.Add(t);
            }

            if (ob != null && (!skipBlank || SumCost != 0 || SumQty != 0 || tlst.Count > 1))
            { 
                statementList.Add(ob);
                ob._SumQty = Math.Round(SumQty, ob.itm._Decimals);
                ob._SumCost = SumCost;
                ob.ChildRecord = tlst.ToArray();
                if (!isAscending)
                    Array.Reverse(ob.ChildRecord);
            }

            dataRowCount = statementList.Count;
            dgInvTran.ItemsSource = null;
            if (dataRowCount > 0)
            {
                dgInvTran.ItemsSource = statementList;
                childDgInvTrans.RefreshData();
            }
        }

        protected override async Task LoadCacheInBackGroundAsync()
        {
            if (ItemCache == null)
                ItemCache = await api.LoadCache(typeof(Uniconta.DataModel.InvItem)).ConfigureAwait(false);
            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor), typeof(Uniconta.DataModel.Creditor) });
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "ExpandAndCollapse":
                    if (dgInvTran.ItemsSource == null)
                        return;
                    setExpandAndCollapse(dgInvTran.IsMasterRowExpanded(0));
                    break;
                case "Search":
                    LoadInvTran();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void setExpandAndCollapse(bool expandState)
        {
            if (dgInvTran.ItemsSource == null) return;
            if (ibase == null) return;
            if (ibase.Caption == Uniconta.ClientTools.Localization.lookup("ExpandAll") && !expandState)
                ExpandAndCollapseAll(false);
            else if (expandState)
                ExpandAndCollapseAll(true);

        }

        void GetMenuItem()
        {
            ibase = UtilDisplay.GetMenuCommandByName((RibbonBase)localMenu.DataContext, "ExpandAndCollapse");
        }

        int dataRowCount;

        private void cmbFromAccount_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            cmbToAccount.SelectedItem = cmbFromAccount.SelectedItem;
        }

        void ExpandAndCollapseAll(bool IsCollapseAll)
        {
            for (int rowHandle = 0; rowHandle < dataRowCount; rowHandle++)
                if (!IsCollapseAll)
                    dgInvTran.ExpandMasterRow(rowHandle);
                else
                    dgInvTran.CollapseMasterRow(rowHandle);
            if (IsCollapseAll)
            {
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("ExpandAll");
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("Expand_32x32");
            }
            else
            {
                ibase.Caption = Uniconta.ClientTools.Localization.lookup("CollapseAll");
                ibase.LargeGlyph = Utilities.Utility.GetGlyph("Collapse_32x32");
            }
        }
    }
}
