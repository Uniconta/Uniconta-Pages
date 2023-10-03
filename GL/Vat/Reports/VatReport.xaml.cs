using Uniconta.ClientTools.Page;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using UnicontaClient.Pages.GL.ChartOfAccount.Reports;
using Uniconta.ClientTools.DataModel;
using UnicontaClient.Models;
using Uniconta.Common;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using System.ComponentModel;
using Uniconta.ClientTools.Util;
using DevExpress.Xpf.Grid;
using System.Threading.Tasks;
using Uniconta.API.Service;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class VatReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(VatReportLine); } }
    }

    internal class AccountSorter : IComparer<FinancialBalance>
    {
        public int Compare(FinancialBalance x, FinancialBalance y)
        {
            var c = x.AccountRowId - y.AccountRowId;
            if (c != 0)
                return c;
            return x.VatRowId - y.VatRowId;
        }
    }

    public partial class VatReport : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.VATReport; } }

        GLVatReported master;
        List<VatSumOperationReport> sumPeriod, sumPrevPeriod;
        List<VatReportLine> lstPeriod, lstPrevPeriod;

        static DateTime localFromDate, localToDate;
        public VatReport(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public VatReport(UnicontaBaseEntity master)
            : base(master)
        {
            Init((GLVatReported)master);
        }

        void Init(GLVatReported master)
        {
            InitializeComponent();
            this.DataContext = this;
            dgVatReport.api = api;
            SetRibbonControl(localMenu, dgVatReport);
            localMenu.dataGrid = dgVatReport;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgVatReport.RowDoubleClick += DgVatReport_RowDoubleClick;
            gridControl.BusyIndicator = busyIndicator;
            if (master != null)
            {
                this.master = master;
                localFromDate = master._FromDate;
                localToDate = master._ToDate;
                if (master._MaxJournalPostedId > 0)
                {
                    txtDateFrm.IsReadOnly = true;
                    txtDateTo.IsReadOnly = true;
                    cmbJournal.IsReadOnly = true;
                    if (master._Data != null)
                    {
                        var cs = StreamingManagerReuse.Create(master._Data);
                        cs.CompanyId = api.CompanyId;
                        this.lstPeriod = ((VatReportLine[])cs.ToArray(typeof(VatReportLine)))?.ToList();
                        this.sumPeriod = ((VatSumOperationReport[])cs.ToArray(typeof(VatSumOperationReport)))?.ToList();
                        this.lstPrevPeriod = ((VatReportLine[])cs.ToArray(typeof(VatReportLine)))?.ToList();
                        this.sumPrevPeriod = ((VatSumOperationReport[])cs.ToArray(typeof(VatSumOperationReport)))?.ToList();
                        cs.Release();
                    }
                }
            }
            if (localFromDate == DateTime.MinValue)
            {
                var fromDate = GetSystemDefaultDate();
                localFromDate = fromDate.AddDays(1 - fromDate.Day); // first day in current month
            }
            txtDateFrm.DateTime = localFromDate;
            if (localToDate == DateTime.MinValue)
            {
                var toDate = localFromDate.AddMonths(1);
                localToDate = toDate.AddDays(-toDate.Day); // last day in current month
            }
            txtDateTo.DateTime = localToDate;
            vatCountry.ItemsSource = Enum.GetValues(typeof(CountryCode));
            vatCountry.SelectedIndex = api.CompanyEntity._Country;
            LoadJournals();
            SetMenuItem();
        }

        void LoadJournals()
        {
            TransactionReport.SetDailyJournal(cmbJournal, api);
        }

        private void DgVatReport_RowDoubleClick()
        {
            ribbonControl.PerformRibbonAction("Transactions");
        }

        public override Task InitQuery()
        {
            if (master != null)
            {
                if (master._Data != null)
                {
                    rdbVatPeriod.IsChecked = true;
                    this.dgVatReport.ItemsSource = this.lstPeriod;
                    this.dgVatReport.Visibility = Visibility.Visible;
                    this.periodGrid.Visibility = Visibility.Visible;
                }
                else
                    return LoadVatReport();
            }
            return null;
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new[] { typeof(Uniconta.DataModel.GLVatType), typeof(Uniconta.DataModel.GLVat), typeof(Uniconta.DataModel.GLAccount) });
        }

        void SetMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;

            var country = api.CompanyEntity._CountryId;
            if (country != CountryCode.Spain)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportSpain");
            if (country != CountryCode.Estonia)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportEstonia");
            if (country != CountryCode.UnitedKingdom)
            {
                UtilDisplay.RemoveMenuCommand(rb, "VatReportUnitedKingdom");
                UtilDisplay.RemoveMenuCommand(rb, "UKVatReturn");
            }
            if (country != CountryCode.Norway)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportNorway");
            if (country != CountryCode.Denmark)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportDenmark");
            if (country != CountryCode.Estonia)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportEstonia");
            if (country != CountryCode.Netherlands)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportHolland");
            if (country != CountryCode.Iceland)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportIceland");
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var fromDate = txtDateFrm.DateTime;
            var toDate = txtDateTo.DateTime;
            List<VatSumOperationReport> sum = this.sumPeriod;
            if (rdbLastPeriod.IsChecked == true)
            {
                sum = this.sumPrevPeriod;
                var _fromDate = fromDate;
                do
                {
                    fromDate = fromDate.AddMonths(-1);
                    toDate = toDate.AddMonths(-1);
                } while (toDate > _fromDate);
            }

            var lin = dgVatReport.SelectedItem as VatReportLine;

            switch (ActionType)
            {
                case "VatReportSpain":
                    {
                        List<VatReportLine> lst = (List<VatReportLine>)dgVatReport.ItemsSource;
                        if (lst == null)
                            return;
                        var array = UnicontaClient.Pages.GL.Reports.VatSpain.calc(lst.ToArray());
                        AddDockItem(TabControls.VatReportSpain, new object[] { api, array }, "Modelo 303", null, closeIfOpened: true);
                        break;
                    }
                case "VatReportNorway":
                    if (sum != null)
                        AddDockItem(TabControls.VatReportNorway, new object[] { sum, fromDate, toDate }, "Mva skattemeldingen", null, closeIfOpened: true);
                    break;
                /*
                case "OLDVatReportDenmark":
                    if (sum != null)
                        AddDockItem(TabControls.VatReportDenmark, new object[] { api, sum, fromDate, toDate }, "Momsopgørelse", null, closeIfOpened: true);
                    break;
                */
                case "VatReportDenmark":
                    if (sum != null)
                        NewVatReport();
                    break;
                case "VatReportHolland":
                    if (sum != null)
                        AddDockItem(TabControls.VatReportHolland, new object[] { sum, fromDate, toDate }, "BTW Aangifte", null, closeIfOpened: true);
                    break;
                case "VatReportEstonia":
                    if (sum != null)
                        AddDockItem(TabControls.VatReportEstonia, new object[] { sum, fromDate, toDate }, "KM avaldus", null, closeIfOpened: true);
                    break;
                case "VatReportUnitedKingdom":
                    if (sum != null)
                        AddDockItem(TabControls.VatReportUnitedKingdom, new object[] { sum, fromDate, toDate }, "VAT statement", null, closeIfOpened: true);
                    break;
                case "Transactions":
                    if (lin?.Account != null)
                    {
                        if (string.IsNullOrEmpty(cmbJournal.Text))
                        {
                            var dt = PropValuePair.GenereteWhereElements("Date", fromDate, CompareOperator.GreaterThanOrEqual);
                            dt.OrList[0].SecundaryValue = NumberConvert.ToString(toDate.Ticks);
                            var filter = new List<PropValuePair>(4)
                            {
                                dt,
                                PropValuePair.GenereteWhereElements("Account", lin.AccountNumber, CompareOperator.Equal),
                                PropValuePair.GenereteWhereElements("Vat", lin.Vat != null ? lin.Vat._Vat : "null", CompareOperator.Equal)
                            };
                            if (master != null && master._MaxJournalPostedId != 0)
                            {
                                var node = PropValuePair.GenereteWhereElements("JournalPostedId", NumberConvert.ToString(master._MaxJournalPostedId), CompareOperator.LessThanOrEqual);
                                filter.Add(node);

                                if (rdbLastPeriod.IsChecked == true)
                                {
                                    var rec = new GLVatReported() { _ToDate = toDate };
                                    api.Read(rec).GetAwaiter().GetResult();
                                    if (rec._MaxJournalPostedId != 0)
                                    {
                                        var f = node.OrList[0];
                                        f.Opr = CompareOperator.GreaterThanOrEqual;
                                        f.SecundaryValue = NumberConvert.ToString(rec._MaxJournalPostedId + 1);
                                    }
                                }
                            }
                            AddDockItem(TabControls.AccountsTransaction, new object[] { api, filter.ToArray() }, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), lin.AccountNumber));
                        }
                        else
                        {
                            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("AccountStatement"), "/", lin.Account.AccountNumber);
                            var transactionReport = dockCtrl.AddDockItem(TabControls.TransactionReport, this.ParentControl, new object[] { lin.Account, IdObject.get(true) }, header) as TransactionReport;
                            if (transactionReport != null)
                                transactionReport.SetControlsAndLoadGLTrans(fromDate, toDate, null, null, null, null, null, cmbJournal.Text);
                        }
                    }
                    break;
                case "VatReportIceland":
                    if (sum != null)
                        AddDockItem(TabControls.VatReportIceland, new object[] { sum, fromDate, toDate }, "VAT statement", null, closeIfOpened: true);
                    break;
                case "UKVatReturn":
                    string vrn = api.CompanyEntity._Id;
                    if (string.IsNullOrWhiteSpace(vrn))
                    {
                        const string msg = "VAT number has not been found. Please check your company setup before continue.";
                        UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Information"));
                        return;
                    }
                    AddDockItem(TabControls.AccountSelectionView, new object[] { vrn }, Uniconta.ClientTools.Localization.lookup("UKVatReturn"), null, closeIfOpened: true);
                    break;
                case "Search":
                    LoadVatReport();
                    break;
                case "Save":
                    SaveVatReport();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void SaveVatReport()
        {
            if (master == null || master._MaxJournalPostedId == 0)
            {
                var cwTypeConfirmationBox = new CWTypeConfirmationBox(string.Format(Uniconta.ClientTools.Localization.lookup("SaveOBJ") + "?", Uniconta.ClientTools.Localization.lookup("VATsettlements")));
                cwTypeConfirmationBox.Closed += CwTypeConfirmationBox_Closed;
                cwTypeConfirmationBox.Show();
            }
        }

        private void CwTypeConfirmationBox_Closed(object sender, EventArgs e)
        {
            var cwTypeConfirmationBox = sender as CWTypeConfirmationBox;
            if (cwTypeConfirmationBox != null && cwTypeConfirmationBox.DialogResult == true)
            {
                if (master == null)
                    master = new GLVatReportedClient();
                master._FromDate = txtDateFrm.DateTime;
                master._ToDate = txtDateTo.DateTime;
                master._Comment = cwTypeConfirmationBox.ConfirmationComment;
                save();
            }
        }

        void NewVatReport()
        {
            var vatArray1 = new double[40];
            var vatArray2 = new double[40];
            var OtherTaxName = new string[10];

            VatSumOperationReport.SumArray(vatArray1, this.sumPeriod, OtherTaxName);
            VatSumOperationReport.SumArray(vatArray2, this.sumPrevPeriod, null);
            GLVatReported rec = master;
            if (rec != null)
            {
                if (rec._MaxJournalPostedId == 0)
                {
                    rec._FromDate = txtDateFrm.DateTime;
                    rec._ToDate = txtDateTo.DateTime;
                }
            }
            else
                rec = new GLVatReported() { _FromDate = txtDateFrm.DateTime, _ToDate = txtDateTo.DateTime };
            AddDockItem(TabControls.VATSettlementReport, new object[] { rec, vatArray1, vatArray2, OtherTaxName }, Uniconta.ClientTools.Localization.lookup("VATSettlementReport"), null, closeIfOpened: true);
        }

        async void save()
        {
            busyIndicator.IsBusy = true;
            if (sumPeriod == null)
                await LoadVatReport(false);
            if (sumPrevPeriod == null)
                await LoadVatReport(true);
            var cs = StreamingManagerReuse.Create();
            cs.ToStream(lstPeriod);
            cs.ToStream(sumPeriod);
            cs.ToStream(lstPrevPeriod);
            cs.ToStream(sumPrevPeriod);
            master._Data = cs.ToByteArrayAndExit(0);
            var ret = await api.Insert(master);
            busyIndicator.IsBusy = false;
            if (ret == 0)
            {
                var args = new object[2];
                args[0] = 1;
                args[1] = master;
                globalEvents.OnRefresh(NameOfControl, args);
            }
            UtilDisplay.ShowErrorCode(ret);
        }

        async Task LoadVatReport(bool PrevPeriod = false)
        {
            DateTime FromDate, ToDate;

            if (string.IsNullOrEmpty(txtDateFrm.Text))
            {
                FromDate = DateTime.Today;
                FromDate = FromDate.AddDays(1 - FromDate.Day); // first day in current month
            }
            else
                FromDate = localFromDate = txtDateFrm.DateTime.Date;
            if (string.IsNullOrEmpty(txtDateTo.Text))
            {
                ToDate = DateTime.Today.AddMonths(1);
                ToDate = ToDate.AddDays(-ToDate.Day); // last day in current month
            }
            else
                ToDate = localToDate = txtDateTo.DateTime.Date;

            busyIndicator.IsBusy = true;
            string Journal = cmbJournal.Text;

            if (!api.CompanyEntity.HasDecimals)
                CalculatedVAT.HasDecimals = PostedVAT.HasDecimals = WithoutVAT.HasDecimals = Accumulated.HasDecimals = false;

            var calc = new VatCalc() { PrevPeriod = PrevPeriod };
            var res = await calc.LoadVatReport(master, FromDate, ToDate, (CountryCode)Math.Max(0, vatCountry.SelectedIndex), api, Journal);
            busyIndicator.IsBusy = false;
            if (res != 0)
            {
                UtilDisplay.ShowErrorCode(res);
                return;
            }

            if (!PrevPeriod)
            {
                this.sumPeriod = calc.sumPeriod;
                this.lstPeriod = calc.lst;
                this.dgVatReport.ItemsSource = calc.lst;
                rdbVatPeriod.IsChecked = true;
            }
            else
            {
                this.sumPrevPeriod = calc.sumPeriod;
                this.lstPrevPeriod = calc.lst;
                this.dgVatReport.ItemsSource = null;
            }
            if (calc.lst.Count > 0)
                SetMenuItem();
            this.dgVatReport.ItemsSource = calc.lst;
            this.dgVatReport.Visibility = Visibility.Visible;

            periodGrid.Visibility = Visibility.Visible;
        }

        public class VatCalc
        {
            public List<VatSumOperationReport> sumPeriod;
            public List<VatReportLine> lst;
            public bool PrevPeriod;

            static VatSumOperationReport CreateSum(VatSumOperationReport[] sumPeriod, int pos, double rate = 0d)
            {
                var sumLine = sumPeriod[pos];
                if (sumLine != null)
                    return sumLine;
                return (sumPeriod[pos] = new VatSumOperationReport() { _Line = pos, _Pct = rate });
            }

            static void UpdateSum(VatSumOperationReport[] sumPeriod, VatReportLine vDif, int pos, int pos2, int pos3)
            {
                var sumLine = CreateSum(sumPeriod, pos, vDif._Rate);

                if (pos2 == 1)
                    sumLine._AmountBase += vDif.AmountWithVat;
                else if (pos2 == -1)
                    sumLine._AmountBase -= vDif.AmountWithVat;
                else if (pos2 == 2)
                    sumLine._AmountBase += vDif._PostedVAT;
                else if (pos2 == -2)
                    sumLine._AmountBase -= vDif._PostedVAT;

                if (pos3 == 2)
                    sumLine._Amount += vDif._PostedVAT;
                else if (pos3 == -2)
                    sumLine._Amount -= vDif._PostedVAT;
                else if (pos3 == 1)
                    sumLine._Amount += vDif.AmountWithVat;
                else if (pos3 == -1)
                    sumLine._Amount -= vDif.AmountWithVat;
            }

            public async Task<ErrorCodes> LoadVatReport(GLVatReported VatReported, DateTime FromDate, DateTime ToDate, CountryCode vatCountry, CrudAPI qapi, string Journal)
            {
                var country = qapi.CompanyEntity._CountryId;
                var rapi = new ReportAPI(qapi);
                var vatTask = VatReported != null ? rapi.VatCodeSum(VatReported, PrevPeriod) : rapi.VatCodeSum(FromDate, ToDate, Journal, PrevPeriod);

                SQLCache accounts = qapi.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await qapi.LoadCache(typeof(Uniconta.DataModel.GLAccount));
                SQLCache vats = qapi.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await qapi.LoadCache(typeof(Uniconta.DataModel.GLVat));
                SQLCache vattypes = qapi.GetCache(typeof(Uniconta.DataModel.GLVatType)) ?? await qapi.LoadCache(typeof(Uniconta.DataModel.GLVatType));

                FinancialBalance[] sumVat = await vatTask;
                if (sumVat == null || accounts == null || vats == null || vattypes == null)
                {
                    return rapi.LastError;
                }

                Array.Sort(sumVat, new AccountSorter());

                VatReportLine v;
                var lst = new List<VatReportLine>(sumVat.Length);

                int decm = 2;
                bool RoundTo100 = false;
                if (!qapi.CompanyEntity.HasDecimals)
                {
                    RoundTo100 = true;
                    decm = 0;
                }

                var useMOSEU = (vatCountry == CountryCode.Denmark);

                var AccsFound = new HashSet<int>();
                GLAccount Account;
                GLVat Vat;
                int i;
                bool HasOffsetType0 = false, HasOffsetType1 = false;
                int PrevAccount = 0;
                for (i = 0; (i < sumVat.Length); i++)
                {
                    var sum = sumVat[i];
                    Account = (GLAccount)accounts.Get(sum.AccountRowId);
                    if (Account == null) // || Account._SystemAccount == (byte)SystemAccountTypes.SalesTaxOffset)
                        continue;
                    //if (sum._Debit == 0 && sum._Credit == 0)
                    //    continue;

                    Vat = (GLVat)vats.Get(sum.VatRowId);
                    if (Vat == null)
                        continue;

                    var acStr = Account._Account;
                    v = new VatReportLine();
                    v.AmountWithVat = sum.Debit;
                    v._PostedVAT = sum.Credit;
                    v._BaseVAT = sum.AmountBase;

                    v.AccountNumber = acStr;
                    v.Account = Account;
                    v.AccountIsVat = Account._SystemAccount == (byte)SystemAccountTypes.SalesTaxPayable ||
                        Account._SystemAccount == (byte)SystemAccountTypes.SalesTaxReceiveable ||
                        Account._SystemAccount == (byte)SystemAccountTypes.ManuallyReceivableVAT ||
                        Account._SystemAccount == (byte)SystemAccountTypes.ManuallyPayableVAT ? (byte)1 : (byte)0;

                    if (Vat._Account == acStr || Vat._OffsetAccount == acStr)
                    {
                        if (Vat._Account == acStr)
                        {
                            v.AccountIsVat = 1;
                            v.IsOffsetAccount = 0;
                        }
                        else
                        {
                            v.AccountIsVat = 1;
                            v.IsOffsetAccount = 1;
                            if (Vat._VatType == 0)
                                HasOffsetType0 = true;
                            else
                                HasOffsetType1 = true;
                        }
                    }
                    else
                    {
                        foreach (var va in (IEnumerable<GLVat>)vats.GetNotNullArray)
                        {
                            if (va._Account == acStr)
                            {
                                v.AccountIsVat = 1;
                                v.IsOffsetAccount = 0;
                                break;
                            }
                            if (va._OffsetAccount == acStr)
                            {
                                v.AccountIsVat = 1;
                                v.IsOffsetAccount = 1;
                                if (va._VatType == 0)
                                    HasOffsetType0 = true;
                                else
                                    HasOffsetType1 = true;
                                // do not break in case we foind the same account as _Account
                            }
                        }
                    }

                    v.Vat = Vat;
                    v.VatType = (byte)Vat._VatType;
                    if (v.IsOffsetAccount == 1 && Vat._OffsetVatOperation != null)
                        v._VatOperation = (byte)vattypes.Get(Vat._OffsetVatOperation).RowId;
                    else
                        v._VatOperation = sum.VatOperation != 0 ? sum.VatOperation : Vat._VatOperationCode;
                    v._CalculatedVAT = Vat.VatAmount1(v.AmountWithVat, FromDate, RoundTo100, GLVatCalculationMethod.Netto);

                    v.FromDate = FromDate;
                    v.CalcRate(RoundTo100);

                    var vattp = (GLVatType)vattypes.Get(v._VatOperation);
                    if (vattp != null)
                    {
                        v.VatOpr = vattp;
                        v.BaseVatText = vattp._Name;
                        v._setBaseText = true;
                    }
                    lst.Add(v);

                    if (PrevAccount != sum.AccountRowId)
                    {
                        PrevAccount = sum.AccountRowId;
                        AccsFound.Add(sum.AccountRowId);
                    }
                }

                VatReportLine vDif;
                List<int> AccLst = null;
                FinancialBalance[] AccTotals;
                if (!PrevPeriod)
                {
                    var arr = (GLAccount[])accounts.GetNotNullArray;
                    for (i = 0; (i < arr.Length); i++)
                    {
                        Account = arr[i];
                        if (Account._Vat != null)
                        {
                            Vat = (GLVat)vats.Get(Account._Vat);
                            if (Vat == null)
                                continue;
                            if (vatCountry != 0 && vatCountry != (Vat._VatCountry != 0 ? Vat._VatCountry : country))
                            {
                                if (!useMOSEU)
                                    continue;
                            }
                            AccsFound.Add(Account.RowId);
                        }
                        else if (Account._SystemAccount == (byte)SystemAccountTypes.ManuallyReceivableVAT ||
                                Account._SystemAccount == (byte)SystemAccountTypes.ManuallyPayableVAT)
                            AccsFound.Add(Account.RowId);
                    }

                    AccLst = AccsFound.ToList();
                    AccsFound.Clear();

                    AccTotals = await (VatReported != null ?
                        rapi.GenerateTotal(AccLst, VatReported, PrevPeriod) :
                        rapi.GenerateTotal(AccLst, FromDate, ToDate, Journal, null, 0, true, false));

                    SQLCacheTemplate<FinancialBalance> AccLookup = null;
                    if (AccTotals != null && AccTotals.Length > 0)
                        AccLookup = new SQLCacheTemplate<FinancialBalance>(AccTotals, false);

                    lst.Sort(new VatAccountSort());

                    FinancialBalance AccTotal;
                    int AccountRowId = 0;
                    double AmountDif = 0d;
                    vDif = new VatReportLine();
                    for (i = lst.Count; (--i >= 0);)
                    {
                        v = lst[i];
                        if (v.Account.RowId != AccountRowId)
                        {
                            if (AmountDif > 0.005d || AmountDif < -0.005d)
                            {
                                vDif.AmountWithout = GLVat.Round(AmountDif);
                                lst.Add(vDif);
                                vDif = new VatReportLine();
                            }

                            AccountRowId = v.Account.RowId;
                            AccTotal = AccLookup?.Get(AccountRowId);
                            if (AccTotal != null)
                                AmountDif = (AccTotal._Debit - AccTotal._Credit) / 100d;
                            else
                                AmountDif = 0d;

                            AccsFound.Add(AccountRowId);
                        }
                        AmountDif -= v.AmountWithVat;
                        vDif.Account = v.Account;
                        vDif.AccountNumber = v.AccountNumber;
                        vDif.VatType = v.VatType;
                        vDif.AccountIsVat = v.AccountIsVat;
                        vDif.IsOffsetAccount = v.IsOffsetAccount;
                    }

                    if (AmountDif > 0.005d || AmountDif < -0.005d)
                    {
                        vDif.AmountWithout = GLVat.Round(AmountDif);
                        lst.Add(vDif);
                    }

                    if (AccLookup != null)
                    {
                        // Add account, that has a VAT-code but no transactios with VAT.
                        for (i = 0; (i < AccTotals.Length); i++)
                        {
                            AccTotal = AccTotals[i];
                            if (!AccsFound.Contains(AccTotal.RowId) && (AccTotal._Debit - AccTotal._Credit) != 0)
                            {
                                Account = (GLAccount)accounts.Get(AccTotal.RowId);
                                if (Account != null)
                                {
                                    Vat = (GLVat)vats.Get(Account._Vat);
                                    if (Vat != null)
                                    {
                                        vDif = new VatReportLine() { VatType = (byte)Vat._VatType };
                                    }
                                    else if (Account._SystemAccount == (byte)SystemAccountTypes.ManuallyReceivableVAT ||
                                             Account._SystemAccount == (byte)SystemAccountTypes.ManuallyPayableVAT)
                                    {
                                        vDif = new VatReportLine() { AccountIsVat = 1, VatType = Account._SystemAccount == (byte)SystemAccountTypes.ManuallyReceivableVAT ? (byte)1 : (byte)0 };
                                    }
                                    else
                                        continue;

                                    vDif.AmountWithout = (AccTotal._Debit - AccTotal._Credit) / 100d;
                                    vDif.Account = Account;
                                    vDif.AccountNumber = Account._Account;
                                    lst.Add(vDif);
                                }
                            }
                        }
                    }
                }

                var removed = new List<VatReportLine>();
                if (vatCountry != 0)
                {
                    for (i = lst.Count; (--i >= 0);)
                    {
                        v = lst[i];
                        Vat = (GLVat)vats.Get(v.VatCode);
                        if (Vat != null)
                        {
                            if (vatCountry != (Vat._VatCountry != 0 ? Vat._VatCountry : country))
                            {
                                v.hidden = true;
                                removed.Add(v);
                                lst.RemoveAt(i);
                            }
                        }
                    }
                }

                vDif = null;

                for (int k = 0; (k < 4); k++)
                {
                    string helptext;
                    if (k == 0)
                        helptext = "sales";
                    else if (k == 1)
                        helptext = "buy";
                    else if (k == 2)
                        helptext = "sales vat";
                    else
                        helptext = "buy vat";

                    for (int n = 0; n < 3; n++)
                    {
                        vDif = new VatReportLine();
                        vDif.helptext = helptext;
                        vDif.Order = (n > 0) ? n : -1;  // header, total, empty
                        vDif.VatType = (byte)(k % 2);
                        vDif.AccountIsVat = (byte)(k / 2);
                        vDif.isvatotal = (k >= 2 && n == 1);
                        lst.Add(vDif);

                        if (vDif.AccountIsVat == 1)
                        {
                            if ((HasOffsetType0 && vDif.VatType == 0) || (HasOffsetType1 && vDif.VatType == 1))
                            {
                                vDif = new VatReportLine();
                                vDif.helptext = "import vat";
                                vDif.Order = (n > 0) ? n : -1;  // header, total, empty
                                vDif.VatType = (byte)(k % 2);
                                vDif.AccountIsVat = 1;
                                vDif.IsOffsetAccount = 1;
                                vDif.isvatotal = true;
                                lst.Add(vDif);
                            }
                        }
                    }
                }
                vDif = null;

                lst.Sort(new VatTotalsSort());
                double d1 = 0d, d2 = 0d, d3 = 0d, d4 = 0d;

                double[] VatOperationValues = new double[256];
                double[] VatOperationBases = new double[256];

                double[] VatValues = new double[256];
                double[] VatBases = new double[256];

                VatSumOperationReport[] sumPeriod = new VatSumOperationReport[127];

                var loopLst = lst;
                if (removed.Count != 0)
                {
                    removed.AddRange(lst);
                    removed.Sort(new VatTotalsSort());
                    loopLst = removed;
                }
                for (i = 0; (i < loopLst.Count); i++)
                {
                    v = loopLst[i];

                    if (v.Order == 1) // total
                    {
                        v.AmountWithVat = Math.Round(d1, decm);
                        var AmountWithout = Math.Round(d2, decm);
                        v.AmountWithout = AmountWithout;
                        v._CalculatedVAT = Math.Round(d3, decm);
                        v._PostedVAT = Math.Round(d4, decm);
                        d1 = d2 = d3 = d4 = 0d;

                        if (country == CountryCode.Denmark && AmountWithout != 0 && v.AccountIsVat == 1)
                        {
                            if (v.IsOffsetAccount == 0)
                            {
                                // here we sum up the amount without vatcodes
                                if (v.VatType == 0)
                                {
                                    var sumLine = CreateSum(sumPeriod, 33);
                                    sumLine._Amount -= AmountWithout;
                                }
                                else
                                {
                                    var sumLine = CreateSum(sumPeriod, 23);
                                    sumLine._Amount += AmountWithout;
                                    sumLine = CreateSum(sumPeriod, 31);
                                    sumLine._Amount += AmountWithout;
                                }
                            }
                            else
                            {
                                if (v.VatType == 1)
                                {
                                    var sumLine = CreateSum(sumPeriod, 34);
                                    sumLine._Amount -= AmountWithout;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!v.hidden)
                        {
                            d1 += v.AmountWithVat;
                            d2 += v.AmountWithout;
                            d3 += v._CalculatedVAT;
                            d4 += v._PostedVAT;
                        }

                        if (v.AccountIsVat == 1)
                        {
                            if (v.IsOffsetAccount == 0)
                            {
                                VatOperationValues[v._VatOperation] += v.AmountWithVat;
                                if (v.Vat != null)
                                    VatValues[v.Vat.RowId] += v.AmountWithVat;
                            }
                        }
                        else
                        {
                            VatOperationBases[v._VatOperation] += v.AmountWithVat;
                            if (v.Vat != null)
                                VatBases[v.Vat.RowId] += v.AmountWithVat;
                        }
                    }
                }

                vDif = new VatReportLine();
                vDif.Text = string.Format("{0}: {1}, {2}", Uniconta.ClientTools.Localization.lookup("FinalVatStatus"), Uniconta.ClientTools.Localization.lookup("AmountBase"), Uniconta.ClientTools.Localization.lookup("VATamount"));
                lst.Add(vDif);

                foreach (var c in (GLVatType[])vattypes.GetNotNullArray)
                {
                    if (c == null)
                        continue;

                    vDif = new VatReportLine();
                    vDif.VatOpr = c;
                    vDif._Rate = c._Pct1;
                    vDif.Text = string.Concat(c._Code, ", ", c._Name);
                    vDif._setText = true;
                    vDif.AmountWithVat = VatOperationBases[c._RowNo];
                    vDif._PostedVAT = VatOperationValues[c._RowNo];
                    vDif.AccountIsVat = 1;
                    lst.Add(vDif);

                    int pos = c._Position1;
                    if (pos > 0)
                        UpdateSum(sumPeriod, vDif, pos, c._Position2, c._Position3);
                    pos = c._Position4;
                    if (pos > 0)
                        UpdateSum(sumPeriod, vDif, pos, c._Position5, c._Position6);
                    pos = c._Position7;
                    if (pos > 0)
                        UpdateSum(sumPeriod, vDif, pos, c._Position8, c._Position9);
                    pos = c._Position10;
                    if (pos > 0)
                        UpdateSum(sumPeriod, vDif, pos, c._Position11, c._Position12);
                    pos = c._Position13;
                    if (pos > 0)
                        UpdateSum(sumPeriod, vDif, pos, c._Position14, c._Position15);
                }

                List<VatReportLine> vatlst = new List<VatReportLine>();
                for (int k = 0; (k < 2); k++)
                {
                    vDif = new VatReportLine();
                    vDif.Text = " ";
                    lst.Add(vDif);

                    vDif = new VatReportLine();
                    vDif.VatType = (byte)k;
                    vDif.Order = -1;
                    vDif.AccountIsVat = 1;
                    lst.Add(vDif);

                    foreach (var c in (GLVat[])vats.GetNotNullArray)
                    {
                        if (c != null && (int)c._VatType == k)
                        {
                            if (vatCountry != 0 && vatCountry != (c._VatCountry != 0 ? c._VatCountry : country))
                            {
                                if (!useMOSEU)
                                    continue;
                            }

                            vDif = new VatReportLine();
                            vDif.Vat = c;
                            vDif.Text = string.Concat(c._Vat, ", ", c._Name);
                            vDif._setText = true;
                            vDif.AmountWithVat = VatBases[c.RowId];
                            vDif._PostedVAT = VatValues[c.RowId];
                            vDif.AccountIsVat = 1;
                            vDif.FromDate = FromDate;
                            vDif.CalcRate(RoundTo100);
                            lst.Add(vDif);
                            vatlst.Add(vDif);
                        }
                    }
                }

                if (country == CountryCode.Denmark && !this.PrevPeriod)
                {
                    if (AccLst != null)
                        AccLst.Clear();
                    else
                        AccLst = new List<int>(10);
                    foreach (var acc in (GLAccount[])accounts.GetNotNullArray)
                    {
                        if (acc._SystemAccount == (byte)SystemAccountTypes.OtherTax)
                        {
                            if (acc._Name.IndexOf("olie", StringComparison.CurrentCultureIgnoreCase) >= 0)
                                acc._SystemAccount = (byte)SystemAccountTypes.OilDuty;
                            else if (acc._Name.IndexOf("vand", StringComparison.CurrentCultureIgnoreCase) >= 0)
                                acc._SystemAccount = (byte)SystemAccountTypes.WaterDuty;
                            else if (acc._Name.IndexOf("CO2", StringComparison.CurrentCultureIgnoreCase) >= 0)
                                acc._SystemAccount = (byte)SystemAccountTypes.CO2Duty;
                            else if (acc._Name.IndexOf("gas", StringComparison.CurrentCultureIgnoreCase) >= 0)
                                acc._SystemAccount = (byte)SystemAccountTypes.GasDuty;
                            else if (acc._Name.IndexOf("kul", StringComparison.CurrentCultureIgnoreCase) >= 0)
                                acc._SystemAccount = (byte)SystemAccountTypes.CoalDuty;
                            else if (acc._Name.IndexOf("el", StringComparison.CurrentCultureIgnoreCase) >= 0)
                                acc._SystemAccount = (byte)SystemAccountTypes.ElectricityDuty;
                        }
                        if (acc._SystemAccount >= (byte)SystemAccountTypes.OilDuty && acc._SystemAccount <= (byte)SystemAccountTypes.WaterDuty)
                            AccLst.Add(acc.RowId);
                    }

                    if (AccLst != null && AccLst.Count > 0)
                    {
                        AccTotals = await rapi.GenerateTotal(AccLst, FromDate, ToDate);
                        if (AccTotals != null && AccTotals.Length > 0)
                        {
                            var otherTaxList = new ReportDataDenmark[AccTotals.Length];
                            foreach (var acTot in AccTotals)
                            {
                                var Acc = (GLAccount)accounts.Get(acTot.AccountRowId);
                                i = (int)Acc._SystemAccount - (int)SystemAccountTypes.OilDuty + 14;
                                sumPeriod[i] = new VatSumOperationReport()
                                {
                                    Acc = Acc,
                                    _Amount = (acTot._Debit - acTot._Credit) / 100d,
                                    _Line = i
                                };
                            }
                        }
                    }
                }

                int sumPeriodSize;
                if (country == CountryCode.Norway)
                    sumPeriodSize = 19; // we need 19 columns for Norway
                else if (country == CountryCode.Denmark)
                    sumPeriodSize = 20;
                else if (country == CountryCode.Iceland)
                    sumPeriodSize = 23; // we need 23 columns for Holland
                else
                    sumPeriodSize = 0;

                for (i = sumPeriodSize + 1; (--i >= 1);)
                    if (sumPeriod[i] == null)
                        sumPeriod[i] = new VatSumOperationReport() { _Line = i };

                this.sumPeriod = sumPeriod.Where(r => r != null).ToList();
                this.lst = lst;

                return 0;
            }
        }

        private void vatPeriod_Click(object sender, RoutedEventArgs e)
        {
            if (rdbVatPeriod.IsChecked == true)
            {
                dgVatReport.ItemsSource = null;
                this.dgVatReport.ItemsSource = this.lstPeriod;
            }
            else if (rdbLastPeriod.IsChecked == true)
            {
                if (this.lstPrevPeriod != null)
                {
                    dgVatReport.ItemsSource = null;
                    this.dgVatReport.ItemsSource = this.lstPrevPeriod;
                }
                else
                    LoadVatReport(true);
            }
        }

        public override object GetPrintParameter()
        {
            return new VatReportHeader
            {
                CompanyName = api.CompanyEntity.Name,
                ReportName = Uniconta.ClientTools.Localization.lookup("VATReport"),
                CurDateTime = DateTime.Now.ToString("g"),
                HeaderParameterTemplateStyle = Application.Current.Resources["VatReportPageHeaderStyle"] as Style,
                FromDate = txtDateFrm.Text == string.Empty ? string.Empty : txtDateFrm.DateTime.ToShortDateString(),
                ToDate = txtDateTo.Text == string.Empty ? string.Empty : txtDateTo.DateTime.ToShortDateString()
            };
        }
    }
    public class VatReportHeader : PageReportHeader, INotifyPropertyChanged
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
