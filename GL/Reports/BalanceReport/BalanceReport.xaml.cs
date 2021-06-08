using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using DevExpress.Xpf.Editors;
using DevExpress.Xpf.Editors.Settings;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using System.Windows.Data;
using System.Globalization;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class BalanceReportManagerGrid : CorasauDataGridClient
    {
        public delegate void Printclickeddelegate();
        public event Printclickeddelegate PrintClick;
        public BalanceReportManagerGrid()
        {
            TableView tv = new TableView();
            tv.AllowEditing = false;
            tv.ShowGroupPanel = false;
            tv.AllowFixedColumnMenu = true;
            SetTableViewStyle(tv);
            this.View = tv;
            this.LookupTableType = typeof(BalanceClient);
        }
        public override Type TableType
        {
            get { return null; }
        }


        public override void PrintGrid(string reportName, object printparam, string format = null, BasePage page = null, bool showDialogInPrint = true)
        {
            if (format == null)
                PrintClick();
            else
                base.PrintGrid(reportName, printparam, format, page, false);
        }
    }

    public class BalanceReportManagerTextGridColumn : DevExpress.Xpf.Grid.GridColumn
    {
    }
    public class BalanceReportManagerGridColumn : DevExpress.Xpf.Grid.GridColumn
    { }
    public class BalanceReportManagerGridColumnGridControlBand : GridControlBand
    {
        public string CriteriaName;
    }

    internal class BalanceClientSort : IComparer<BalanceClient>
    {
        public int Compare(BalanceClient x, BalanceClient y)
        {
            var c = string.Compare(x.AccountNo, y.AccountNo);
            if (c != 0)
                return c;
            c = string.Compare(x.Dim1, y.Dim1);
            if (c != 0)
                return c;
            c = string.Compare(x.Dim2, y.Dim2);
            if (c != 0)
                return c;
            c = string.Compare(x.Dim3, y.Dim3);
            if (c != 0)
                return c;
            c = string.Compare(x.Dim4, y.Dim4);
            if (c != 0)
                return c;
            return string.Compare(x.Dim5, y.Dim5);
        }
    }

    public class BalanceClientText
    {
        public static string Account { get { return Uniconta.ClientTools.Localization.lookup("AccountNumber"); } }
        public static string Name { get { return Uniconta.ClientTools.Localization.lookup("AccountName"); } }
        public static string Amount { get { return Uniconta.ClientTools.Localization.lookup("Amount"); } }
        public static string Debit { get { return Uniconta.ClientTools.Localization.lookup("Debit"); } }
        public static string Credit { get { return Uniconta.ClientTools.Localization.lookup("Credit"); } }
    }

    public class BalanceClient
    {
        int NumberOfCol;
        public bool _UseExternal;
        public BalanceClient(GLAccount Acc, bool useexternal, int numberOfCol)
        {
            _UseExternal = useexternal;
            this.NumberOfCol = numberOfCol;
            amount = new long[numberOfCol];
            this.AccountRowId = Acc.RowId;
            this.Acc = Acc;
            init();
        }
        public BalanceClient(long[] amount)
        {
            this.NumberOfCol = amount.Length;
            this.amount = amount;
            this.Acc = new GLAccount(); // dummy
            init();
        }

        private void init()
        {
            var header = (Acc == null || Acc.AccountTypeEnum == GLAccountTypes.Header);
            this.Col1 = new CustomColumn(amount, 00, header);
            this.Col2 = new CustomColumn(amount, 01, header);
            this.Col3 = new CustomColumn(amount, 02, header);
            this.Col4 = new CustomColumn(amount, 03, header);
            this.Col5 = new CustomColumn(amount, 04, header);
            this.Col6 = new CustomColumn(amount, 05, header);
            this.Col7 = new CustomColumn(amount, 06, header);
            this.Col8 = new CustomColumn(amount, 07, header);
            this.Col9 = new CustomColumn(amount, 08, header);
            this.Col10 = new CustomColumn(amount, 09, header);
            this.Col11 = new CustomColumn(amount, 10, header);
            this.Col12 = new CustomColumn(amount, 11, header);
            this.Col13 = new CustomColumn(amount, 12, header);
            Columns = new List<CustomColumn>();
            for (int c = 0; c < NumberOfCol; c++)
            {
                Columns.Add(new CustomColumn(amount, c, header));
            }
        }

        public readonly GLAccount Acc;
        public readonly int AccountRowId;

        public GLAccountTypes AccountTypeEnum { get { return (GLAccountTypes)Acc._AccountType; } }

        [Display(Name = "Account", ResourceType = typeof(BalanceClientText))]
        public string AccountNo { get { return Acc._Account; } }

        [Display(Name = "Name", ResourceType = typeof(BalanceClientText))]
        public string AccountName { get { return Acc._Name; } }

        public int[] DimArry;
        public bool PageBreak { get { return Acc._PageBreak; } }
        public bool Hide { get { return Acc._HideInBalance; } }

        public bool UseExternal { get { return _UseExternal; } }

        public void SumUpAmount(long[] sumAmount)
        {
            for (int i = NumberOfCol; (--i >= 0);)
                sumAmount[i] += amount[i];
        }

        public void RemoveColumn(int column)
        {
            NumberOfCol--;
            while (column < NumberOfCol)
            {
                amount[column] = amount[column + 1];
                column++;
            }
        }

        public bool AmountsAreZero()
        {
            for (int i = 0; (i < NumberOfCol); i++)
                if (amount[i] != 0)
                    return false;
            return true;
        }

        internal readonly long[] amount;
        public string ColumnDate { get; set; }
        public string Dim1 { get; set; }
        public string Dim2 { get; set; }
        public string Dim3 { get; set; }
        public string Dim4 { get; set; }
        public string Dim5 { get; set; }

        double? Amount(int arg) { var d = amount[arg]; return (d != 0) ? d / 100d : (double?)null; }
        double? Debit(int arg) { var d = amount[arg]; return (d > 0) ? d / 100d : (double?)null; }
        double? Credit(int arg) { var d = amount[arg]; return (d < 0) ? d / -100d : (double?)null; }

        public List<CustomColumn> Columns { get; set; }
        public CustomColumn Col1 { set; get; }
        public CustomColumn Col2 { set; get; }
        public CustomColumn Col3 { set; get; }
        public CustomColumn Col4 { set; get; }
        public CustomColumn Col5 { set; get; }
        public CustomColumn Col6 { set; get; }
        public CustomColumn Col7 { set; get; }
        public CustomColumn Col8 { set; get; }
        public CustomColumn Col9 { set; get; }
        public CustomColumn Col10 { set; get; }
        public CustomColumn Col11 { set; get; }
        public CustomColumn Col12 { set; get; }
        public CustomColumn Col13 { set; get; }
    }

    public partial class BalanceReport : GridBasePage
    {
        HeaderData hdrData = new HeaderData();
        public Criteria PassedCriteria { get; set; }
        List<SelectedCriteria> CriteriaLst;
        bool dim1details;
        bool dim2details;
        bool dim3details;
        bool dim4details;
        bool dim5details;
        string FromAccount, ToAccount;
        string AppliedTemplate;
        bool Skip0Account;
        bool SkipSumAccount, OnlySumAccounts;
        bool ShowDimName;
        bool UseExternal;
        int ShowType;
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        public override string NameOfControl { get { return TabControls.BalanceReport; } }
        ReportAPI tranApi;
        List<BalanceClient> balanceClient;
        SQLCache dim1, dim2, dim3, dim4, dim5;
        object[] templateReportData;
        const int SizeFactor = 5;
        public BalanceReport(UnicontaBaseEntity sourcedata, Criteria Criteria)
           : base(sourcedata)
        {
            this.PassedCriteria = Criteria;
            Init(sourcedata);
        }
        public BalanceReport(BaseAPI API) : base(API, string.Empty)
        {
            Init();
        }
        void Init(UnicontaBaseEntity master = null)
        {
            this.DataContext = this;
            InitializeComponent();
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            this.Skip0Account = PassedCriteria.Skip0Account;
            this.ShowType = PassedCriteria.ShowType;
            this.SkipSumAccount = PassedCriteria.SkipSumAccount;
            this.OnlySumAccounts = PassedCriteria.OnlySumAccounts;
            this.UseExternal = PassedCriteria.UseExternal;
            this.FromAccount = PassedCriteria.FromAccount;
            this.ShowDimName = PassedCriteria.ShowDimName;
            if (string.IsNullOrWhiteSpace(this.FromAccount))
                this.FromAccount = null;
            this.ToAccount = PassedCriteria.ToAccount;
            if (string.IsNullOrWhiteSpace(this.ToAccount))
                this.ToAccount = null;
            this.dim1details = PassedCriteria.dim1details;
            this.dim2details = PassedCriteria.dim2details;
            this.dim3details = PassedCriteria.dim3details;
            this.dim4details = PassedCriteria.dim4details;
            this.dim5details = PassedCriteria.dim5details;
            CriteriaLst = new List<SelectedCriteria>(PassedCriteria.selectedCriteria);
            this.AppliedTemplate = PassedCriteria.Template;
            if (string.IsNullOrWhiteSpace(this.AppliedTemplate))
                this.AppliedTemplate = null;

            tranApi = new ReportAPI(api);
            if (master is GLClosingSheet)
                tranApi.SetClosingSheet((GLClosingSheet)master);
            balanceClient = new List<BalanceClient>();
            ((TableView)dgBalanceReport.View).RowStyle = Application.Current.Resources["RowStyle"] as Style;
            setColumnDim();
            GenerateBalance();
            ColumndimData();
            dgBalanceReport.api = api;
            SetRibbonControl(null, dgBalanceReport);
            dgBalanceReport.PrintClick += dgBalanceReport_PrintClick;
#if SILVERLIGHT
            Application.Current.RootVisual.KeyDown += Page_KeyDown;
#else
            this.KeyDown += Page_KeyDown;
#endif
            dgBalanceReport.RowDoubleClick += dgBalanceReport_RowDoubleClick;

        }

        private void dgBalanceReport_RowDoubleClick()
        {
            OpenTransactionReport();
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F8)
                OpenTransactionReport();
        }

        void OpenTransactionReport()
        {
            var Acc = (dgBalanceReport.SelectedItem as BalanceClient)?.Acc;
            if (Acc != null && Acc.AccountTypeEnum > GLAccountTypes.CalculationExpression)
            {
                var bandColumn = dgBalanceReport.CurrentColumn?.ParentBand as BalanceReportManagerGridColumnGridControlBand;
                if (bandColumn != null)
                {
                    string criteriaName = bandColumn.CriteriaName;
                    SelectedCriteria criteria = null;
                    for (int i = 0; i < CriteriaLst.Count; i++)
                    {
                        var crit = CriteriaLst[i];
                        if (crit.ColNameNumber == criteriaName)
                        {
                            criteria = crit;
                            break;
                        }
                    }
                    if (criteria?.balanceColumnMethod == BalanceColumnMethod.FromTrans)
                    {
                        var fieldName = dgBalanceReport.CurrentColumn.FieldName;
                        var fName = fieldName.Substring(fieldName.LastIndexOf('.') + 1);
                        if (fName == "Debit" || fName == "Credit" || fName == "Amount")
                        {
                            var args = new object[2];
                            args[0] = Acc;
                            args[1] = true;
                            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("AccountStatement"), "/", Acc._Account);
                            var transactionReport = dockCtrl.AddDockItem(TabControls.TransactionReport, this.ParentControl, args, header) as TransactionReport;
                            if (transactionReport != null)
                                transactionReport.SetControlsAndLoadGLTrans(criteria.FromDate, criteria.ToDate, criteria.Dim1, criteria.Dim2, criteria.Dim3, criteria.Dim4, criteria.Dim5, criteria.journal);
                        }
                    }
                }
            }
        }
        public override Task InitQuery()
        {
            return null;
        }
        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            var bal = dg.SelectedItem as BalanceClient;
            if (bal == null)
                return lookup;
            if (dg.CurrentColumn?.Name == "AccountNo")
                lookup.TableType = typeof(Uniconta.DataModel.GLAccount);
            return lookup;
        }
        void dgBalanceReport_PrintClick()
        {
            if (PassedCriteria.selectedCriteria.Count > 13)
            {
                var confirmationMsgBox = UnicontaMessageBox.Show(string.Format("{0}\n{1}", Uniconta.ClientTools.Localization.lookup("BalanceReportMax13ColPrint"), Uniconta.ClientTools.Localization.lookup("ProceedConfirmation")), Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OKCancel);
                if (confirmationMsgBox == MessageBoxResult.OK)
                    PrintData();
            }
            else
                PrintData();
        }
        void setColumnDim()
        {
            coldim1.Visible = coldim2.Visible = coldim3.Visible = coldim4.Visible = coldim5.Visible = false;
            var c = api.CompanyEntity;
            var dimCount = c.NumberOfDimensions;

            if (this.dim1details && dimCount > 0)
            {
                coldim1.Visible = true;
                coldim1.Header = c._Dim1;
                hdrData.Dim1 = c._Dim1;
            }
            else
                this.dim1details = false;

            if (this.dim2details && dimCount > 1)
            {
                coldim2.Visible = true;
                coldim2.Header = c._Dim2;
                hdrData.Dim2 = c._Dim2;
            }
            else
                this.dim2details = false;

            if (this.dim3details && dimCount > 2)
            {
                coldim3.Visible = true;
                coldim3.Header = c._Dim3;
                hdrData.Dim3 = c._Dim3;
            }
            else
                this.dim3details = false;

            if (this.dim4details && dimCount > 3)
            {
                coldim4.Visible = true;
                coldim4.Header = c._Dim4;
                hdrData.Dim4 = c._Dim4;
            }
            else
                this.dim4details = false;

            if (this.dim5details && dimCount > 4)
            {
                coldim5.Visible = true;
                coldim5.Header = c._Dim5;
                hdrData.Dim5 = c._Dim5;
            }
            else
                dim5details = false;
        }

        async void ColumndimData()
        {
            dim1 = dim2 = dim3 = dim4 = dim5 = null;
            var api = this.api;
            int nDim = api.CompanyEntity.NumberOfDimensions;
            if (nDim >= 5)
                dim5 = api.GetCache(typeof(Uniconta.DataModel.GLDimType5)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDimType5));
            if (nDim >= 4)
                dim4 = api.GetCache(typeof(Uniconta.DataModel.GLDimType4)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDimType4));
            if (nDim >= 3)
                dim3 = api.GetCache(typeof(Uniconta.DataModel.GLDimType3)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDimType3));
            if (nDim >= 2)
                dim2 = api.GetCache(typeof(Uniconta.DataModel.GLDimType2)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDimType2));
            if (nDim >= 1)
                dim1 = api.GetCache(typeof(Uniconta.DataModel.GLDimType1)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLDimType1));
        }

        static IEnumerable<int> ValidateDimValues(List<int> values)
        {
            if (values == null)
                return null;
            if (!values.Contains(-2))
                return values;
            int dim;
            if (values.Contains(0))  // values = -2, blank = 0, if both, then it is the same as "all"
                dim = -1; // all
            else
                dim = -2; // only values
            return new int[] { dim };
        }

        public static List<ReportAPI.DimensionParameter> SetDimensionParameters(List<int> dimval1, List<int> dimval2, List<int> dimval3, List<int> dimval4, List<int> dimval5, bool dim1details, bool dim2details,
        bool dim3details, bool dim4details, bool dim5details)
        {
            var dimParamOne = new ReportAPI.DimensionParameter(1);
            dimParamOne.DimDetails = dim1details;
            dimParamOne.InValues = ValidateDimValues(dimval1);

            var dimParamTwo = new ReportAPI.DimensionParameter(2);
            dimParamTwo.DimDetails = dim2details;
            dimParamTwo.InValues = ValidateDimValues(dimval2);

            var dimParamThree = new ReportAPI.DimensionParameter(3);
            dimParamThree.DimDetails = dim3details;
            dimParamThree.InValues = ValidateDimValues(dimval3);

            var dimParamFour = new ReportAPI.DimensionParameter(4);
            dimParamFour.DimDetails = dim4details;
            dimParamFour.InValues = ValidateDimValues(dimval4);

            var dimParamFive = new ReportAPI.DimensionParameter(5);
            dimParamFive.DimDetails = dim5details;
            dimParamFive.InValues = ValidateDimValues(dimval5);

            var dimParams = new List<ReportAPI.DimensionParameter>(5) { dimParamOne, dimParamTwo, dimParamThree, dimParamFour, dimParamFive };
            return dimParams;
        }

        async void GenerateBalance()
        {
            busyIndicator.IsBusy = true;
            var Cache = api.GetCache(typeof(GLAccount)) ?? api.LoadCache(typeof(GLAccount)).GetAwaiter().GetResult();
            if (balanceClient.Capacity == 0)
                balanceClient.Capacity = Cache.Count;

            var CriteriaList = this.CriteriaLst;
            SelectedCriteria Crit;
            int k, c;

            bool CalcFound = false;
            for (k = CriteriaList.Count; (--k >= 0);)
            {
                var method = CriteriaList[k].balcolMethod;
                if (method > BalanceColumnMethod.FromBudget && method < BalanceColumnMethod.OnlyJournals)
                {
                    CalcFound = true;
                    break;
                }
            }

            int Cols = 0;
            bool first = true;
            bool hiddenFound = false;
            for (k = 0; (k < CriteriaList.Count); k++)
            {
                Crit = CriteriaList[k];
                if (Crit._Hide)
                {
                    hiddenFound = true;
                    if (!CalcFound) // if we do not have calculated columns, we do not need to calculate the hidden column
                    {
                        Cols++;
                        continue;
                    }
                }

                var method = Crit.balcolMethod;
                if (method <= BalanceColumnMethod.FromBudget || method >= BalanceColumnMethod.OnlyJournals)
                {
                    Task<FinancialBalance[]> tsk;
                    bool inTrans;
                    var dimParams = SetDimensionParameters(Crit.dimval1, Crit.dimval2, Crit.dimval3, Crit.dimval4, Crit.dimval5, dim1details, dim2details, dim3details, dim4details, dim5details);
                    if (method == BalanceColumnMethod.FromTrans || method == BalanceColumnMethod.OnlyJournals)
                    {
                        inTrans = true;
                        tsk = tranApi.GenerateTotal(null, Crit.frmdateval, Crit.todateval, Crit.journal, dimParams, Crit.forCompany != null ? Crit.forCompany.CompanyId : 0, !Crit.InclPrimo, method == BalanceColumnMethod.OnlyJournals);
                    }
                    else if (method == BalanceColumnMethod.TransQty)
                    {
                        inTrans = true;
                        tsk = tranApi.GenerateTotalQty(null, Crit.frmdateval, Crit.todateval, Crit.journal, dimParams, Crit.forCompany != null ? Crit.forCompany.CompanyId : 0, false);
                    }
                    else
                    {
                        inTrans = false;
                        tsk = tranApi.GenerateBudgetTotals(Crit.budgetModel, Crit.frmdateval, Crit.todateval, dimParams, method == BalanceColumnMethod.BudgetQty);
                    }
                    var bal = await tsk;
                    if (bal != null)
                    {
                        var balance = tranApi.Format(bal, Cache);
                        for (c = 0; (c < balance.Length); c++)
                            CreateBalanceRow(balance[c], Cols, first);
                    }
                    else
                    {
                        if (inTrans && !string.IsNullOrEmpty(Crit.journal))
                            UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(ErrorCodes.ErrorInJournal.ToString()) + "\n" + Uniconta.ClientTools.Localization.lookup(tranApi.LastError.ToString()), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                        else
                            UtilDisplay.ShowErrorCode(tranApi.LastError);
                    }
                    first = false;
                }
                Cols++;
            }
            ShowColumns();
            List<BalanceClient> BalanceList;
            BalanceClient col;
            long[] amount;
            long d;
            if (AppliedTemplate != null)
                BalanceList = await GenerateTemplateGrid(Cols);
            else
                BalanceList = null;

            if (BalanceList == null)
            {
                AppliedTemplate = null;
                BalanceList = balanceClient;
            }

            if (CalcFound)
            {
                for (k = 0; (k < CriteriaList.Count); k++)
                {
                    Crit = CriteriaList[k];
                    var ColA = Crit.colA - 1;
                    var ColB = Crit.colB - 1;
                    var Method = Crit.balcolMethod;
                    if (Method == BalanceColumnMethod.SumAtoB)
                    {
                        if (ColA < 0)
                            ColA = 0;
                        if (ColB < 0)
                            ColB = k - 1;
                    }
                    if (Method == BalanceColumnMethod.Account100)
                    {
                        if (ColA < 0)
                            ColA = k > 0 ? k - 1 : k + 1;
                        var Acc100 = Crit.Account100;
                        if (!string.IsNullOrEmpty(Acc100))
                        {
                            long Sum100 = 0;
                            for (c = 0; (c < BalanceList.Count); c++)
                            {
                                col = BalanceList[c];
                                if (col.AccountNo == Acc100)
                                    Sum100 += col.amount[ColA];
                            }

                            if (Sum100 != 0)
                            {
                                var half = Math.Abs(Sum100) >> 1;
                                for (c = 0; (c < BalanceList.Count); c++)
                                {
                                    amount = BalanceList[c].amount;
                                    d = 10000 * amount[ColA];
                                    if (d > 0)
                                        d += half;
                                    else
                                        d -= half;
                                    amount[k] = Math.Abs(d / Sum100);
                                }
                            }
                        }
                    }
                    else if ((Method > BalanceColumnMethod.FromBudget && Method < BalanceColumnMethod.OnlyJournals) && ColA >= 0 && ColA < Cols && ColB >= 0 && ColB < Cols)
                    {
                        for (c = 0; (c < BalanceList.Count); c++)
                        {
                            amount = BalanceList[c].amount;
                            switch (Method)
                            {
                                case BalanceColumnMethod.ColAMinusColB:
                                    d = amount[ColA] - amount[ColB];
                                    break;
                                case BalanceColumnMethod.ColAPlusColB:
                                    d = amount[ColA] + amount[ColB];
                                    break;
                                case BalanceColumnMethod.ColAMinusColBDivColB:
                                    d = amount[ColB];
                                    if (d != 0)
                                        d = 10000 * (amount[ColA] - d) / d;  // has to be 10000 since it is in cent
                                    break;
                                case BalanceColumnMethod.ColAMinusColBDivColBPct:
                                    d = amount[ColB];
                                    if (d != 0)
                                        d = 10000 + 10000 * (amount[ColA] - d) / d;  // has to be 10000 since it is in cent
                                    break;
                                case BalanceColumnMethod.SumAtoB:
                                    d = 0;
                                    for (int j = ColA; j <= ColB; j++)
                                        d += amount[j];
                                    break;
                                default:
                                    d = 0;
                                    break;
                            }
                            amount[k] = d;
                        }
                    }
                }
            }

            if (hiddenFound)
            {
                for (k = CriteriaList.Count; (--k >= 0);)
                {
                    if (CriteriaList[k]._Hide)
                    {
                        CriteriaList.RemoveAt(k);
                        for (c = 0; (c < BalanceList.Count); c++)
                            BalanceList[c].RemoveColumn(k);
                    }
                }
            }

            for (k = 0; (k < CriteriaList.Count); k++)
            {
                Crit = CriteriaList[k];
                if (Crit.balcolFormat > BalanceColumnFormat.Decimal2)
                {
                    int factor, multFactor;
                    if (Crit.balcolFormat == BalanceColumnFormat.Decimal1)
                    {
                        factor = 5;
                        multFactor = 10;
                    }
                    else if (Crit.balcolFormat == BalanceColumnFormat.Decimal0)
                    {
                        factor = 50;
                        multFactor = 100;
                    }
                    else
                    {
                        factor = 500;
                        multFactor = 1;
                    }
                    for (c = 0; (c < BalanceList.Count); c++)
                    {
                        amount = BalanceList[c].amount;
                        d = amount[k];
                        if (d > 0)
                            d += factor;
                        else
                            d -= factor;
                        amount[k] = (d / (factor << 1)) * multFactor;
                    }
                }

                if (AppliedTemplate == null && Crit._InvertSign)
                {
                    bool plFound = false;
                    for (c = 0; (c < BalanceList.Count); c++)
                    {
                        col = BalanceList[c];
                        if (col.AccountTypeEnum >= GLAccountTypes.BalanceSheet)
                        {
                            plFound = false;
                            continue;
                        }
                        if (col.AccountTypeEnum >= GLAccountTypes.PL)
                            plFound = true;

                        if (plFound)
                            col.amount[k] *= -1;
                    }
                }
            }

            if (AppliedTemplate == null)
            {
                var OnlySumAccounts = this.OnlySumAccounts;
                var Skip0Account = this.Skip0Account;
                var SkipSumAccount = this.SkipSumAccount;
                int ShowAccType = this.ShowType;
                if (ShowAccType > 2 || ShowAccType == -1)
                    ShowAccType = 0;
                bool lastSkipped = false;

                if (FromAccount != null || ToAccount != null || Skip0Account || SkipSumAccount || OnlySumAccounts || ShowAccType != 0)
                {
                    GLAccount lastAcc;
                    var lst = new List<BalanceClient>(BalanceList.Count);
                    for (c = 0; (c < BalanceList.Count); c++)
                    {
                        col = BalanceList[c];
                        var accType = col.AccountTypeEnum;
                        if (SkipSumAccount && accType <= GLAccountTypes.CalculationExpression)
                            continue;

                        if ((FromAccount == null || string.Compare(col.AccountNo, FromAccount, StringComparison.CurrentCultureIgnoreCase) >= 0) &&
                              (ToAccount == null || string.Compare(col.AccountNo, ToAccount, StringComparison.CurrentCultureIgnoreCase) <= 0))
                        {
                            if (ShowAccType != 0)
                            {
                                if (ShowAccType == 1)
                                {
                                    if (accType >= GLAccountTypes.BalanceSheet)
                                    {
                                        lastSkipped = true;
                                        continue;
                                    }
                                    else if (accType >= GLAccountTypes.PL && accType < GLAccountTypes.BalanceSheet)
                                        lastSkipped = false;
                                }
                                else
                                {
                                    if (accType >= GLAccountTypes.PL && accType < GLAccountTypes.BalanceSheet)
                                    {
                                        lastSkipped = true;
                                        continue;
                                    }
                                    else if (accType >= GLAccountTypes.BalanceSheet)
                                        lastSkipped = false;
                                }
                                if (lastSkipped)
                                {
                                    if (accType == GLAccountTypes.CalculationExpression)
                                        continue;
                                    if (accType == GLAccountTypes.Sum)
                                    {
                                        lastAcc = lst.Count > 0 ? lst[lst.Count - 1].Acc : null;
                                        if (lastAcc != null && lastAcc._AccountType == (byte)GLAccountTypes.Header)
                                        {
                                            var Sum = col.Acc._SumInfo;
                                            if (Sum != null && Sum.StartsWith(lastAcc._Account)) // This sum account starts from this header. Lets remove this header
                                                lst.RemoveAt(lst.Count - 1);
                                        }
                                        continue;
                                    }
                                }
                            }
                            if (!Skip0Account || accType == GLAccountTypes.Header || !col.AmountsAreZero())
                            {
                                if (col.Hide && col.AmountsAreZero())
                                    continue;
                                if (!OnlySumAccounts || col.AccountTypeEnum <= GLAccountTypes.CalculationExpression)
                                    lst.Add(col);
                            }
                            else if (accType == GLAccountTypes.Sum)
                            {
                                // We are skipping a sum. Lets check if the account above is a header. We do not want a header, if we have no details and no sum
                                lastAcc = lst.Count > 0 ? lst[lst.Count - 1].Acc : null;
                                if (lastAcc != null && lastAcc._AccountType == (byte)GLAccountTypes.Header)
                                {
                                    var Sum = col.Acc._SumInfo;
                                    if ((Sum != null && Sum.StartsWith(lastAcc._Account)) || lastAcc._HideInBalance) // This sum account starts from this header. Lets remove this header
                                        lst.RemoveAt(lst.Count - 1);
                                }
                                else
                                    lst.Add(col); // we add sum anyway, since the sum is zero because accounts add up to zero.
                            }
                        }
                    }
                    BalanceList = balanceClient = lst;
                }
                else
                {
                    for (c = BalanceList.Count; (--c > 0);)
                    {
                        if (BalanceList[c].Hide && BalanceList[c].AmountsAreZero())
                            BalanceList.RemoveAt(c);
                    }
                }

                BalanceList.Sort(new BalanceClientSort());

                AccountName.Visible = AccountNo.Visible = true;
                Text.Visible = false;
                dgBalanceReport.ItemsSource = BalanceList;
            }
            dgBalanceReport.Visibility = Visibility.Visible;
        }

        static string setDateFormat(SelectedCriteria objCrit)
        {
            var name = objCrit.criteriaName;
            if (string.IsNullOrWhiteSpace(name))
                name = null;
            string strRes;
            if (objCrit.balanceColumnMethod < BalanceColumnMethod.ColAMinusColB)
                strRes = string.Concat(objCrit.frmdateval.ToString("d"), Environment.NewLine, objCrit.todateval.ToString("d"));
            else
                strRes = string.Concat(string.Empty, Environment.NewLine, string.Empty);
            return string.Concat(name, Environment.NewLine, strRes);
        }

        void AddGridBandColumn(string i, string header, string bandHeader, bool ShowAmount, bool ShowDebitCredit, TextEditSettings editSettings, DateTime fromDate, DateTime toDate, string criteriaName)
        {
            var amountCol = new BalanceReportManagerGridColumn();
            amountCol.Name = "AmountCol" + i;
            var amountBinding = new Binding();
            amountBinding.Path = new PropertyPath(string.Concat("Columns[", i, "].Amount"));
            amountBinding.Mode = BindingMode.OneWay;
            amountCol.Binding = amountBinding;
            // amountCol.FieldName = 
            amountCol.IsSmart = true;
            amountCol.Header = header;
            amountCol.EditSettings = editSettings;
            amountCol.Visible = ShowAmount;

            var amountDebitCol = new BalanceReportManagerGridColumn();
            amountDebitCol.Name = string.Concat("Amount", i, "Debit");

            var debitBinding = new Binding();
            debitBinding.Path = new PropertyPath(string.Concat("Columns[", i, "].Debit"));
            debitBinding.Mode = BindingMode.OneWay;
            amountDebitCol.Binding = debitBinding;
            //  amountDebitCol.FieldName = string.Format("Columns[{0}].Debit", i);
            amountDebitCol.IsSmart = true;
            amountDebitCol.EditSettings = editSettings;
            amountDebitCol.Visible = ShowDebitCredit;
            amountDebitCol.Header = Uniconta.ClientTools.Localization.lookup("Debit");

            var amountCreditCol = new BalanceReportManagerGridColumn();
            amountCreditCol.Name = string.Concat("Amount", i, "Credit");
            //     amountCreditCol.FieldName = string.Format("Columns[{0}].Credit", i);
            var creditBinding = new Binding();
            creditBinding.Path = new PropertyPath(string.Concat("Columns[", i, "].Credit"));
            creditBinding.Mode = BindingMode.OneWay;
            amountCreditCol.Binding = creditBinding;
            amountCreditCol.IsSmart = true;
            amountCreditCol.EditSettings = editSettings;
            amountCreditCol.Visible = ShowDebitCredit;
            amountCreditCol.Header = Uniconta.ClientTools.Localization.lookup("Credit");

            var controlBand = new BalanceReportManagerGridColumnGridControlBand() { Name = "BandColumn" + i, Header = bandHeader, CriteriaName = criteriaName };
            controlBand.Columns.Add(amountCol);
            controlBand.Columns.Add(amountDebitCol);
            controlBand.Columns.Add(amountCreditCol);
            dgBalanceReport.Bands.Add(controlBand);
        }

        private void ShowColumns()
        {
            var AmountHeader = Uniconta.ClientTools.Localization.lookup("Amount");
            TextEditSettings formatter0 = null, formatter1 = null, formatter2 = null;

            int col = -1;
            for (int i = 0; (i < CriteriaLst.Count); i++)
            {
                var Crit = CriteriaLst[i];
                if (Crit._Hide)
                    continue;

                string str = setDateFormat(Crit);
                bool ShowDebitCredit = Crit.ShowDebitCredit;
                bool ShowAmount = !ShowDebitCredit;

                string numberFormat;
                TextEditSettings formatter;
                switch (Crit.balanceColumnFormat)
                {
                    case BalanceColumnFormat.Thousand:
                    case BalanceColumnFormat.Decimal0:
                        formatter = formatter0 ?? (formatter0 = GetColumnFormat("n0"));
                        numberFormat = "N0";
                        break;
                    case BalanceColumnFormat.Decimal1:
                        formatter = formatter1 ?? (formatter1 = GetColumnFormat("n1"));
                        numberFormat = "N1";
                        break;
                    default:
                        formatter = formatter2 ?? (formatter2 = GetColumnFormat("n2"));
                        numberFormat = "N2";
                        break;
                }

                string Header;
                if (Crit.balcolMethod >= BalanceColumnMethod.ColAMinusColBDivColB && Crit.balcolMethod <= BalanceColumnMethod.Account100)
                {
                    ShowDebitCredit = false;
                    ShowAmount = true;
                    Header = "%";
                    if (str == string.Empty)
                        str = Header;
                }
                else
                    Header = AmountHeader;

                col++;
                AddGridBandColumn(NumberConvert.ToString(col), Header, str, ShowAmount, ShowDebitCredit, formatter, Crit.frmdateval, Crit.todateval, Crit.colNameNumber);
                var dccol = ShowDebitCredit == true ? Visibility.Visible : Visibility.Collapsed;
                switch (col)
                {
                    case 0:
                        hdrData.ColName1 = str;
                        hdrData.Col1AmountHeader = Header;
                        hdrData.Col1Format = numberFormat;
                        hdrData.ShowDCCol1 = dccol;
                        break;
                    case 1:
                        hdrData.ColName2 = str;
                        hdrData.Col2AmountHeader = Header;
                        hdrData.Col2Format = numberFormat;
                        hdrData.ShowDCCol2 = dccol;
                        break;
                    case 2:
                        hdrData.ColName3 = str;
                        hdrData.Col3AmountHeader = Header;
                        hdrData.Col3Format = numberFormat;
                        hdrData.ShowDCCol3 = dccol;
                        break;
                    case 3:
                        hdrData.ColName4 = str;
                        hdrData.Col4AmountHeader = Header;
                        hdrData.Col4Format = numberFormat;
                        hdrData.ShowDCCol4 = dccol;
                        break;
                    case 4:
                        hdrData.ColName5 = str;
                        hdrData.Col5AmountHeader = Header;
                        hdrData.Col5Format = numberFormat;
                        hdrData.ShowDCCol5 = dccol;
                        break;
                    case 5:
                        hdrData.ColName6 = str;
                        Header = hdrData.Col6AmountHeader = Header;
                        hdrData.Col6Format = numberFormat;
                        hdrData.ShowDCCol6 = dccol;
                        break;
                    case 6:
                        hdrData.ColName7 = str;
                        Header = hdrData.Col7AmountHeader = Header;
                        hdrData.Col7Format = numberFormat;
                        hdrData.ShowDCCol7 = dccol;
                        break;
                    case 7:
                        hdrData.ColName8 = str;
                        hdrData.Col8AmountHeader = Header;
                        hdrData.Col8Format = numberFormat;
                        hdrData.ShowDCCol8 = dccol;
                        break;
                    case 8:
                        hdrData.ColName9 = str;
                        hdrData.Col9AmountHeader = Header;
                        hdrData.Col9Format = numberFormat;
                        hdrData.ShowDCCol9 = dccol;
                        break;
                    case 9:
                        hdrData.ColName10 = str;
                        hdrData.Col10AmountHeader = Header;
                        hdrData.Col10Format = numberFormat;
                        hdrData.ShowDCCol10 = dccol;
                        break;
                    case 10:
                        hdrData.ColName11 = str;
                        hdrData.Col11AmountHeader = Header;
                        hdrData.Col11Format = numberFormat;
                        hdrData.ShowDCCol11 = dccol;
                        break;
                    case 11:
                        hdrData.ColName12 = str;
                        hdrData.Col12AmountHeader = Header;
                        hdrData.Col12Format = numberFormat;
                        hdrData.ShowDCCol12 = dccol;
                        break;
                    case 12:
                        hdrData.ColName13 = str;
                        hdrData.Col13AmountHeader = Header;
                        hdrData.Col13Format = numberFormat;
                        hdrData.ShowDCCol13 = dccol;
                        break;
                }
            }
            busyIndicator.IsBusy = false;
        }

        TextEditSettings GetColumnFormat(string format)
        {
            return new TextEditSettings() { MaskType = MaskType.Numeric, MaskUseAsDisplayFormat = true, Mask = format, HorizontalContentAlignment = EditSettingsHorizontalAlignment.Right };
        }

        private void CreateBalanceRow(BalanceHeader finbal, int ColNo, bool FirstTime)
        {
            var AccountRowId = finbal.Account.RowId;
            var colCount = PassedCriteria.selectedCriteria.Count;
            BalanceClient client;
            if (finbal.SumOfDimensions != null)
            {
                foreach (var dimValue in finbal.SumOfDimensions)
                {
                    if (!FirstTime)
                    {
                        // We check if we have the same account and dimension
                        bool found = false;
                        var Dimensions = dimValue.Dimensions;
                        foreach (var col in balanceClient)
                        {
                            if (col.AccountRowId == AccountRowId && BalanceHeader.DimensionsEqual(col.DimArry, Dimensions))
                            {
                                col.amount[ColNo] = dimValue._Debit - dimValue._Credit;
                                found = true;
                                break;
                            }
                        }
                        if (found)
                            continue;
                    }

                    client = new BalanceClient(finbal.Account, this.UseExternal, colCount);
                    client.amount[ColNo] = dimValue._Debit - dimValue._Credit;
                    client.DimArry = dimValue.Dimensions;
                    SetDimKeystr(client);
                    balanceClient.Add(client);
                }
            }
            else if (FirstTime)
            {
                client = new BalanceClient(finbal.Account, this.UseExternal, colCount);
                balanceClient.Add(client);
            }
        }

        string FormatKey(IdKey rec)
        {
            if (rec != null)
                return this.ShowDimName ? string.Concat(rec.KeyStr, "-", rec.KeyName) : rec.KeyStr;
            return string.Empty;
        }
        void SetDimKeystr(BalanceClient client)
        {
            int[] dimArr = client.DimArry;
            if (dimArr == null)
                return;
            if (dimArr[0] > 0)
                client.Dim1 = FormatKey(dim1?.Get(dimArr[0]));
            if (dimArr[1] > 0)
                client.Dim2 = FormatKey(dim2?.Get(dimArr[1]));
            if (dimArr[2] > 0)
                client.Dim3 = FormatKey(dim3?.Get(dimArr[2]));
            if (dimArr[3] > 0)
                client.Dim4 = FormatKey(dim4?.Get(dimArr[3]));
            if (dimArr[4] > 0)
                client.Dim5 = FormatKey(dim5?.Get(dimArr[4]));
        }

        async void PrintData()
        {
            if (balanceClient == null || balanceClient.Count == 0)
                return;

            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("GeneratingPage");
            busyIndicator.IsBusy = true;
            LineDetailsData listdata = new LineDetailsData();
            listdata.balClient = balanceClient;
            if (templateReportData == null)
            {
                var arr = balanceClient.ToArray();
                Array.Sort(arr, new BalanceClientSort());
                foreach (BalanceClient blc in arr)
                {
                    BalanceReportdata data = new BalanceReportdata(blc, hdrData);
                    var AcType = blc.AccountTypeEnum;
                    data.isBold = (AcType <= GLAccountTypes.CalculationExpression) ? FontWeights.Bold : FontWeights.Normal;
                    data.IsVisible = (AcType == GLAccountTypes.Header) ? Visibility.Collapsed : Visibility.Visible;
                    data.Line = (AcType == GLAccountTypes.Header) ? Utilities.Utility.GetImageData("Black_Small_Line.png") : null;
                    data.Underline = (AcType == GLAccountTypes.Header) ? "Underline" : null;
                    data.isSumOrExpression = (AcType == GLAccountTypes.CalculationExpression || AcType == GLAccountTypes.Sum) ? Visibility.Visible : Visibility.Collapsed;
                    data.ISPageBreak = blc.PageBreak ? Visibility.Visible : Visibility.Collapsed;
                    listdata.BalanceReportlist.Add(data);
                }
            }
            busyIndicator.IsBusy = false;
            CompanyClient cmplClient = new CompanyClient();
            StreamingManager.Copy(api.CompanyEntity, cmplClient);
            hdrData.Company = cmplClient;
            hdrData.exportServiceUrl = CorasauDataGrid.GetExportServiceConnection(this.api);
            string header = ParentControl.Caption.ToString();
            hdrData.BalanceName = header;
            var frontpageText = string.Empty;
#if !SILVERLIGHT
            DevExpress.XtraReports.UI.XtraReport frontPageReport = null;
#endif
            if (PassedCriteria.ObjBalance._PrintFrontPage)
            {
                frontpageText = PassedCriteria.ObjBalance._FrontPage;
#if !SILVERLIGHT
                frontPageReport = await GetFrontPageXtraReport(cmplClient, frontpageText, PassedCriteria?.ObjBalance?._ReportFrontPage);
#endif
            }
            if (!string.IsNullOrEmpty(PassedCriteria.Template))
            {
#if !SILVERLIGHT
                templateReportData[3] = frontPageReport;
#endif
                AddDockItem(TabControls.BalanceReportTemplatePrint, templateReportData, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("ReportCriteria"), header), null, true);//new templated report                         
            }
            else
            {
                object[] ob = new object[4];
                ob[0] = hdrData;
                ob[1] = listdata;
                ob[2] = PassedCriteria.ObjBalance;
#if !SILVERLIGHT
                ob[3] = frontPageReport;
#endif
                AddDockItem(TabControls.BalanceReportPrint, ob, string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("ReportCriteria"), header), null, true);
            }
        }

#if !SILVERLIGHT
        async private Task<DevExpress.XtraReports.UI.XtraReport> GetFrontPageXtraReport(CompanyClient company, string frontPageText, string frontPageTemplate)
        {
            var cmpClientUser = Utilities.Utility.GetCompanyClientUserInstance(company);
            DevExpress.XtraReports.UI.XtraReport balanceFrontPageReport = null;
            var userReportApi = new UserReportAPI(api);
            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = Uniconta.ClientTools.Localization.lookup("LoadingMsg");
            if (!string.IsNullOrEmpty(frontPageTemplate))
            {
                var dataBytes = await userReportApi.LoadForRun(frontPageTemplate, 50);
                if (dataBytes != null && dataBytes.Length > 0)
                    balanceFrontPageReport = Uniconta.Reports.Utilities.ReportUtil.GetXtraReportFromLayout(dataBytes);

                if (balanceFrontPageReport != null)
                {
                    var logoBytes = await UtilDisplay.GetLogo(api);
                    balanceFrontPageReport.DataSource = new IStandardBalanceReportClient[1] { new BalanceFrontPageReportClient(cmpClientUser, logoBytes, Uniconta.ClientTools.Localization.lookup("CoverPage"), frontPageText) };
                }
            }
            busyIndicator.IsBusy = false;

            return balanceFrontPageReport;
        }
#endif
    }

    public class AccountTextDecorationTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string textDecoration = null;
            var bal = value as BalanceClient;
            if (bal?.AccountTypeEnum == GLAccountTypes.Header || bal?.AccountTypeEnum == GLAccountTypes.Sum)
                textDecoration = "Underline";
            return textDecoration;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class AccountFontTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var fontweight = FontWeights.Normal;
            var bal = value as BalanceClient;
            if (bal?.AccountTypeEnum == GLAccountTypes.Header || bal?.AccountTypeEnum == GLAccountTypes.Sum)
                fontweight = FontWeights.Bold;
            return fontweight;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
