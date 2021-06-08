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

        List<VatReportLine> vatlst;
        List<VatSumOperationReport> vatReportSum;
        int vatReportSumSize;

        public VatReport(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            this.DataContext = this;
            dgVatReport.api = api;
            SetRibbonControl(localMenu, dgVatReport);
            localMenu.dataGrid = dgVatReport;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgVatReport.RowDoubleClick += DgVatReport_RowDoubleClick;
            gridControl.BusyIndicator = busyIndicator;
            //this.Loaded += VatReport_Loaded;
            var FromDate = GetSystemDefaultDate();
            FromDate = FromDate.AddDays(1 - FromDate.Day); // first day in current month
            txtDateFrm.DateTime = FromDate;
            var ToDate = FromDate.AddMonths(1);
            ToDate = ToDate.AddDays(-ToDate.Day); // last day in current month
            txtDateTo.DateTime = ToDate;
            SetMenuItem();
        }

        private void DgVatReport_RowDoubleClick()
        {
            localMenu_OnItemClicked("Transactions");
        }

        public override Task InitQuery()
        {
            return null;
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
                UtilDisplay.RemoveMenuCommand(rb, "VatReportUnitedKingdom");
            if (country != CountryCode.Norway)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportNorway");
            else
                vatReportSumSize = 19; // we need 19 columns for Norway
            if (country != CountryCode.Denmark)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportDenmark");
            else
                vatReportSumSize = 20;
            if (country != CountryCode.Estonia)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportEstonia");
            if (country != CountryCode.Netherlands)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportHolland");
            if (country != CountryCode.Iceland)
                UtilDisplay.RemoveMenuCommand(rb, "VatReportIceland");
            else
                vatReportSumSize = 23; // we need 23 columns for Holland
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var fromDate = txtDateFrm.DateTime;
            var toDate = txtDateTo.DateTime;
            var lin = dgVatReport.SelectedItem as VatReportLine;

            object[] param;
            switch (ActionType)
            {
                case "VatReportSpain":
                    {
                        List<VatReportLine> lst = (List<VatReportLine>)dgVatReport.ItemsSource;
                        if (lst == null)
                            return;
                        var array = UnicontaClient.Pages.GL.Reports.VatSpain.calc(lst.ToArray());
                        param = new object[] { api, array };
                        AddDockItem(TabControls.VatReportSpain, param, "Modelo 303", null, closeIfOpened: true);
                        break;
                    }
                case "VatReportNorway":
                    {
                        if (vatReportSum == null)
                            return;
                        param = new object[] { vatReportSum, fromDate, toDate };
                        AddDockItem(TabControls.VatReportNorway, param, "Mva skattemeldingen", null, closeIfOpened: true);
                        break;
                    }
                case "VatReportDenmark":
                    {
                        if (vatReportSum == null)
                            return;
                        param = new object[] { api, this.vatReportSum, fromDate, toDate };
                        AddDockItem(TabControls.VatReportDenmark, param, "Momsopgørelse", null, closeIfOpened: true);
                        break;
                    }
                case "VatReportHolland":
                    {
                        if (vatReportSum == null)
                            return;
                        param = new object[] { vatReportSum, fromDate, toDate };
                        AddDockItem(TabControls.VatReportHolland, param, "BTW Aangifte", null, closeIfOpened: true);
                        break;
                    }
                case "VatReportEstonia":
                    {
                        if (vatReportSum == null)
                            return;
                        param = new object[] { vatReportSum, fromDate, toDate };
                        AddDockItem(TabControls.VatReportEstonia, param, "KM avaldus", null, closeIfOpened: true);
                        break;
                    }
                case "VatReportUnitedKingdom":
                    {
                        if (vatReportSum == null)
                            return;
                        param = new object[] { vatReportSum, fromDate, toDate };
                        AddDockItem(TabControls.VatReportUnitedKingdom, param, "VAT statement", null, closeIfOpened: true);
                        break;
                    }
                case "Transactions":
                    if (lin?.Account != null)
                    {
                        var dt = PropValuePair.GenereteWhereElements("Date", fromDate, CompareOperator.GreaterThanOrEqual);
                        dt.OrList[0].SecundaryValue = NumberConvert.ToString(toDate.Ticks);
                        var filter = new PropValuePair[]
                        {
                            dt,
                            PropValuePair.GenereteWhereElements("Account", lin.AccountNumber, CompareOperator.Equal),
                            PropValuePair.GenereteWhereElements("Vat", lin.Vat != null ? lin.Vat._Vat : "null", CompareOperator.Equal)
                        };
                        AddDockItem(TabControls.AccountsTransaction, new object[] {api, filter}, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Transactions"), lin.AccountNumber));
                    }
                    break;
                case "VatReportIceland":
                    {
                        if (vatReportSum == null)
                            return;
                        param = new object[] { vatReportSum, fromDate, toDate };
                        AddDockItem(TabControls.VatReportIceland, param, "VAT statement", null, closeIfOpened: true);
                        break;
                    }
                default:
                    break;
            }
            gridRibbon_BaseActions(ActionType);
        }

        void VatReport_Loaded(object sender, RoutedEventArgs e)
        {          
            LoadVatReport();
        }     

        private void btnSerach_Click(object sender, RoutedEventArgs e)
        {
            LoadVatReport();
        }

        static VatSumOperationReport CreateSum(VatSumOperationReport[] vatReportSum, int pos, double rate = 0d)
        {
            var sumLine = vatReportSum[pos];
            if (sumLine != null)
                return sumLine;
            return (vatReportSum[pos] = new VatSumOperationReport() { _Line = pos, _Pct = rate });
        }

        static void UpdateSum(VatSumOperationReport[] vatReportSum, VatReportLine vDif, int pos, int pos2, int pos3)
        {
            var sumLine = CreateSum(vatReportSum, pos, vDif._Rate);

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

        private async void LoadVatReport()
        {
            DateTime FromDate, ToDate;

            if (txtDateFrm.Text == string.Empty)
            {
                FromDate = DateTime.Today;
                FromDate = FromDate.AddDays(1 - FromDate.Day); // first day in current month
            }
            else
                FromDate = txtDateFrm.DateTime.Date;
            if (txtDateTo.Text == string.Empty)
            {
                ToDate = DateTime.Today.AddMonths(1);
                ToDate = ToDate.AddDays(-ToDate.Day); // last day in current month
            }
            else
                ToDate = txtDateTo.DateTime.Date;

            busyIndicator.IsBusy = true;

            var qapi = api;
            var country = qapi.CompanyEntity._CountryId;
            var rapi = new ReportAPI(qapi);

            var vatTask = rapi.VatCodeSum(FromDate, ToDate);

            SQLCache accounts = qapi.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await qapi.LoadCache(typeof(Uniconta.DataModel.GLAccount));
            SQLCache vats = qapi.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await qapi.LoadCache(typeof(Uniconta.DataModel.GLVat));
            SQLCache vattypes = qapi.GetCache(typeof(Uniconta.DataModel.GLVatType)) ?? await qapi.LoadCache(typeof(Uniconta.DataModel.GLVatType));

            FinancialBalance[] sumVat = await vatTask;
            if (sumVat == null)
                return;

            Array.Sort(sumVat, new AccountSorter());

            var lst = new List<VatReportLine>(sumVat.Length);

            int decm = 2;
            bool RoundTo100 = false;
            if (!qapi.CompanyEntity.HasDecimals)
            {
                CalculatedVAT.HasDecimals = PostedVAT.HasDecimals = WithoutVAT.HasDecimals = Accumulated.HasDecimals = false;
                RoundTo100 = true;
                decm = 0;
            }

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

                var acStr = Account._Account;
                VatReportLine v = new VatReportLine();
                v.AmountWithVat = sum.Debit;
                v._PostedVAT = sum.Credit;
                v._BaseVAT = sum.AmountBase;

                v.AccountNumber = acStr;
                v.Account = Account;
                v.AccountIsVat = Account._SystemAccount == (byte)SystemAccountTypes.SalesTaxPayable || Account._SystemAccount == (byte)SystemAccountTypes.SalesTaxReceiveable ? (byte)1 : (byte)0;
 
                Vat = (GLVat)vats.Get(sum.VatRowId);
                if (Vat == null)
                    continue;

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
                    v.vatOperation = vattp._Code;
                    v.BaseVatText = vattp._Name;
                }
                lst.Add(v);

                if (PrevAccount != sum.AccountRowId)
                {
                    PrevAccount = sum.AccountRowId;
                    AccsFound.Add(sum.AccountRowId);
                }
            }

            foreach (var acc in (GLAccount[])accounts.GetNotNullArray)
            {
                if (acc._Vat != null)
                    AccsFound.Add(acc.RowId);
            }

            List<int> AccLst = AccsFound.ToList();
            AccsFound.Clear();

            FinancialBalance[] AccTotals = await rapi.GenerateTotal(AccLst, FromDate, ToDate, null, null, 0, true, false);
            SQLCacheTemplate<FinancialBalance> AccLookup = null;
            if (AccTotals != null && AccTotals.Length > 0)
                AccLookup = new SQLCacheTemplate<FinancialBalance>(AccTotals, false);

            lst.Sort(new VatAccountSort());

            FinancialBalance AccTotal;
            int AccountRowId = 0;
            double AmountDif = 0d;
            VatReportLine vDif = new VatReportLine();
            for (i = lst.Count; (--i >= 0); )
            {
                var v = lst[i];
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
                        if (Account?._Vat == null)
                            continue;
                        Vat = (GLVat)vats.Get(Account._Vat);
                        if (Vat == null)
                            continue;

                        vDif = new VatReportLine();
                        vDif.AmountWithout = (AccTotal._Debit - AccTotal._Credit) / 100d;
                        vDif.Account = Account;
                        vDif.AccountNumber = Account._Account;
                        vDif.VatType = (byte)Vat._VatType;
                        lst.Add(vDif);
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

            VatSumOperationReport[] vatReportSum = new VatSumOperationReport[127];

            for (i = 0; (i < lst.Count); i++)
            {
                var v = lst[i];

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
                                var sumLine = CreateSum(vatReportSum, 33);
                                sumLine._Amount -= AmountWithout;
                            }
                            else
                            {
                                var sumLine = CreateSum(vatReportSum, 23);
                                sumLine._Amount += AmountWithout;
                                sumLine = CreateSum(vatReportSum, 31);
                                sumLine._Amount += AmountWithout;
                            }
                        }
                        else
                        {
                            if (v.VatType == 1)
                            {
                                var sumLine = CreateSum(vatReportSum, 34);
                                sumLine._Amount -= AmountWithout;
                            }
                        }
                    }
                }
                else
                {
                    d1 += v.AmountWithVat;
                    d2 += v.AmountWithout;
                    d3 += v._CalculatedVAT;
                    d4 += v._PostedVAT;

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
                vDif.vatOperation = c._Code;
                vDif._Rate = c._Pct1;
                vDif.Text = string.Format("{0}, {1}", c._Code, c._Name);
                vDif.AmountWithVat = VatOperationBases[c._RowNo];
                vDif._PostedVAT = VatOperationValues[c._RowNo];
                vDif.AccountIsVat = 1;
                lst.Add(vDif);

                int pos = c._Position1;
                if (pos > 0)
                    UpdateSum(vatReportSum, vDif, pos, c._Position2, c._Position3);
                pos = c._Position4;
                if (pos > 0)
                    UpdateSum(vatReportSum, vDif, pos, c._Position5, c._Position6);
                pos = c._Position7;
                if (pos > 0)
                    UpdateSum(vatReportSum, vDif, pos, c._Position8, c._Position9);
                pos = c._Position10;
                if (pos > 0)
                    UpdateSum(vatReportSum, vDif, pos, c._Position11, c._Position12);
                pos = c._Position13;
                if (pos > 0)
                    UpdateSum(vatReportSum, vDif, pos, c._Position14, c._Position15);
            }

            List<VatReportLine> vatlst = new List<VatReportLine>();
            this.vatlst = vatlst;

            for (int k = 0; (k < 2); k++)
            {
                vDif = new VatReportLine();
                vDif.Text = string.Empty;
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
                        vDif = new VatReportLine();
                        vDif.Vat = c;
                        vDif.Text = string.Format("{0}, {1}", c._Vat, c._Name);
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

            dgVatReport.ItemsSource = lst;
            if (lst.Count > 0)
                SetMenuItem();

            if (country == CountryCode.Denmark)
            {
                AccLst.Clear();
                foreach (var acc in (GLAccount[])accounts.GetNotNullArray)
                {
                    if (acc._SystemAccount == (byte)SystemAccountTypes.OtherTax)
                        AccLst.Add(acc.RowId);
                }
                if (AccLst != null && AccLst.Count > 0)
                {
                    AccTotals = await rapi.GenerateTotal(AccLst, FromDate, ToDate);
                    if (AccTotals != null && AccTotals.Length > 0)
                    {
                        var otherTaxList = new ReportDataDenmark[AccTotals.Length];
                        i = 14;
                        foreach (var acTot in AccTotals)
                        {
                            var Acc = accounts.Get(acTot.AccountRowId);
                            var lin = new VatSumOperationReport()
                            {
                                _Text = Acc.KeyName,
                                _Amount = (acTot._Debit - acTot._Credit) / 100d,
                                _Line = i
                            };
                            vatReportSum[i] = lin;
                            i++;
                            if (i == 19)
                                break;
                        }
                    }
                }
            }

            for (i = vatReportSumSize + 1; (--i >= 1);)
                if (vatReportSum[i] == null)
                    vatReportSum[i] = new VatSumOperationReport() { _Line = i };

            this.vatReportSum = vatReportSum.Where(r => r != null).ToList();

            dgVatReport.Visibility = Visibility.Visible;
            busyIndicator.IsBusy = false;
        }

        public override object GetPrintParameter()
        {
            VatReportHeader Hdr = new VatReportHeader();
            Hdr.CompanyName = api.CompanyEntity.Name;
            Hdr.ReportName = Uniconta.ClientTools.Localization.lookup("VATReport");
            Hdr.CurDateTime = DateTime.Now.ToString("g");
            Hdr.HeaderParameterTemplateStyle = Application.Current.Resources["VatReportPageHeaderStyle"] as Style;
            Hdr.FromDate = txtDateFrm.Text == string.Empty ? string.Empty : txtDateFrm.DateTime.ToShortDateString();
            Hdr.ToDate = txtDateTo.Text == string.Empty ? string.Empty : txtDateTo.DateTime.ToShortDateString();                 
            return Hdr;
        }
    }
    public class VatReportHeader : PageReportHeader, INotifyPropertyChanged
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }   
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
