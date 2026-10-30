using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.TransactionOrigin;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CorasauDataGridSimulatedTran : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransClientTotal); } }
        public override DataTemplate PrintGridFooter(ref object FooterData)
        {
            FooterData = this.FooterData;
            if (FooterData != null)
            {
                var footerTemplate = Page.Resources["ReportFooterTemplate"] as DataTemplate;
                return footerTemplate;
            }
            return null;
        }

        public object FooterData { get; set; }
    }

    public partial class SimulatedTransactions : GridBasePage, IGLPostingSourceTraceConsumer
    {
        public override string NameOfControl { get { return TabControls.SimulatedTransactions; } }
        public SimulatedTransactions(UnicontaBaseEntity[] simulatedTransactions)
            : base(simulatedTransactions, 0, 0)
        {
            Initialize(simulatedTransactions);
            RemoveMenuItem();
        }
        bool ShowTotal, ShowVoucherTotal;
        private readonly DCOrder postedOrder;
        private readonly IEnumerable<DCOrderLine> postedOrderLines;
        private PostingTraceVoucherContext postingTraceContext;

        /// <summary>
        /// Creates a simulated transaction view with the transient order and invoice lines which
        /// produced the simulation. The lines are retained for posting-source tracing.
        /// </summary>
        public SimulatedTransactions(UnicontaBaseEntity[] simulatedTransactions, DCOrder postedOrder, IEnumerable<DCOrderLine> postedOrderLines)
            : base(simulatedTransactions, 0, 0)
        {
            this.postedOrder = postedOrder;
            this.postedOrderLines = postedOrderLines?.Where(line => line != null).ToArray();
            Initialize(simulatedTransactions);
            RemoveMenuItem();
        }

        AccountPostingBalance[] balance;
        public SimulatedTransactions(AccountPostingBalance[] balance, UnicontaBaseEntity[] simulatedTransactions)
            : base(simulatedTransactions, 0, 0)
        {
            Initialize(simulatedTransactions);

            if (balance == null || balance.Length == 0)
                RemoveMenuItem();
            else
                this.balance = balance;
        }

        /// <summary>
        /// Creates a simulated transaction view with balance information and the
        /// transient order and invoice lines which produced the simulation.
        /// </summary>
        public SimulatedTransactions(AccountPostingBalance[] balance, UnicontaBaseEntity[] simulatedTransactions, DCOrder postedOrder, IEnumerable<DCOrderLine> postedOrderLines)
            : base(simulatedTransactions, 0, 0)
        {
            this.postedOrder = postedOrder;
            this.postedOrderLines = postedOrderLines?.Where(line => line != null).ToArray();
            Initialize(simulatedTransactions);

            if (balance == null || balance.Length == 0)
                RemoveMenuItem();
            else
                this.balance = balance;
        }

        private void Initialize(UnicontaBaseEntity[] simulatedTransactions)
        {
            InitializeComponent();
            dgSimulatedTran.api = api;
            if (simulatedTransactions != null && simulatedTransactions.Length > 0)
            {
                var lst = new GLTransClientTotal[simulatedTransactions.Length];
                long total = 0, vouchertotal = 0;
                int i = 0;
                int voucher = 0;
                int CompanyId = api.CompanyId;
                foreach (var t in (IEnumerable<GLTransClientTotal>)simulatedTransactions)
                {
                    t.CompanyId = CompanyId;
                    if (t._Voucher != voucher)
                    {
                        voucher = t._Voucher;
                        if (vouchertotal != 0)
                        {
                            lst[i - 1]._VoucherTotal = vouchertotal;
                            vouchertotal = 0;
                            ShowVoucherTotal = true;
                        }
                    }
                    vouchertotal += t._AmountCent;
                    total += t._AmountCent;
                    t._Total = total;
                    lst[i++] = t;
                }
                if (vouchertotal != 0)
                {
                    lst[i - 1]._VoucherTotal = vouchertotal;
                    ShowVoucherTotal = true;
                }
                ShowTotal = (total != 0);
                dgSimulatedTran.ClearSorting();
                dgSimulatedTran.SetSource(lst);
            }
            SetRibbonControl(localMenu, dgSimulatedTran);
            SetPostingSourceTraceAvailability();
            dgSimulatedTran.BusyIndicator = busyIndicator;

            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgSimulatedTran.ShowTotalSummary();
            LedgerCache = this.api.GetCache(typeof(GLAccount));
        }

        private void SetPostingSourceTraceAvailability()
        {
            var ribbon = localMenu.DataContext as RibbonBase;
            var postingSourceTrace = ribbon?.MenuItems.FirstOrDefault(item => Convert.ToString(item.Tag) == "PostingSourceTrace");
            if (postingSourceTrace != null)
                postingSourceTrace.isEditLayout = postedOrder != null || postedOrderLines != null;
        }

        void RemoveMenuItem()
        {
            RibbonBase rb = (RibbonBase)localMenu.DataContext;
            Uniconta.ClientTools.Util.UtilDisplay.RemoveMenuCommand(rb, "AccountPostingBalance");
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AccountPostingBalance":
                    ViewAccountPostingBalance();
                    break;

                case "PostingSourceTrace":
                    ShowPostingSourceTrace();
                    break;

                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        
        public override List<IAIAssistantCapability> GetAIAssistantCapabilities(bool hasFileAttached)
        {
            var capabilities = base.GetAIAssistantCapabilities(hasFileAttached);
            capabilities.Add(new OpenTransactionOriginCapability(this));
            capabilities.Add(new ReviewVoucherCapability(this));
            return capabilities;
        }

        public void ShowPostingSourceTrace(GLTransClient selectionOverride = null)
        {
            if (postedOrder == null && postedOrderLines == null)
                return;

            if (selectionOverride != null)
            {
                var matchingItem = (dgSimulatedTran.ItemsSource as IEnumerable)
                    .OfType<GLTransClient>()
                    .FirstOrDefault(x => x.JourRowId == selectionOverride.JourRowId);

                if (matchingItem != null)
                    dgSimulatedTran.SelectedItem = matchingItem;
            }

            AddDockItem(TabControls.GLPostingSourceTrace,
                        new object[] 
                        { 
                            dgSimulatedTran.syncEntity,                             
                            new GLPostingSourceTraceOpenparms
                            {
                                Consumer = this,
                                SelectionOverride = selectionOverride,
                                SimulatedOrder = postedOrder,
                                SimulatedOrderLines = postedOrderLines
                            }
                        },
                        Uniconta.ClientTools.Localization.lookup("PostingSourceTrace"));
        }

        Task<PostingTraceVoucherContext> IGLPostingSourceTraceConsumer.GetPostingTraceContextAsync(GLTransClient transaction, CrudAPI api)
        {
            if (postingTraceContext == null)
            {
                var transactions = (dgSimulatedTran.ItemsSource as IEnumerable)?.OfType<GLTransClient>().ToArray();
                postingTraceContext = new PostingTraceVoucherContext(transactions,
                    item => InvoicePostingTraceResolver.TracePostingAsync(item, api, postedOrder, postedOrderLines));
            }
            return Task.FromResult(postingTraceContext);
        }
        GLTransClient IGLPostingSourceTraceConsumer.GetSelectedVoucherTransaction() => dgSimulatedTran.SelectedItem as GLTransClient;

        private void ViewAccountPostingBalance()
        {
            if (balance != null)
                AddDockItem(TabControls.AccountPostingBalancePage, new object[2] { api, balance }, true, Uniconta.ClientTools.Localization.lookup("TraceBalance"));
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
        }

        public override Task InitQuery() { return null; }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            Total.Visible = ShowTotal;
            VoucherTotal.Visible = ShowVoucherTotal;
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            var Comp = api.CompanyEntity;
            if (!Comp._UseVatOperation)
                VatOperation.Visible = false;
            if (!Comp.Project)
            {
                ProjectName.Visible = false;
                Project.Visible = false;
                CategoryName.Visible = false;
                PrCategory.Visible = false;
            }
            if (!Comp.HasDecimals)
                Debit.HasDecimals = Credit.HasDecimals = Amount.HasDecimals = AmountBase.HasDecimals = AmountVat.HasDecimals = Total.HasDecimals = false;
        }

        SQLCache LedgerCache;

        public override object GetPrintParameter()
        {
            if (balance != null && balance.Length > 0)
            {
                var fd = new PrintReportFooter();
                dgSimulatedTran.FooterData = fd;

                int n = 0;
                foreach (var bal in balance)
                {
                    var ac = (GLAccount)LedgerCache.Get(bal.AccountRowId);
                    if (ac == null)
                        continue;
                    var name = string.Concat(ac._Account, ", ", ac._Name);
                    var InitialSum = bal.BalanceBefore;
                    var TraceSum = InitialSum + bal.PostingAmount;

                    n++;
                    switch (n)
                    {
                        case 1:
                            fd.TraceSum1Name = name;
                            fd.TraceSum1 = TraceSum;
                            fd.InitialSum1 = InitialSum;
                            break;
                        case 2:
                            fd.TraceSum2Name = name;
                            fd.TraceSum2 = TraceSum;
                            fd.InitialSum2 = InitialSum;
                            break;
                        case 3:
                            fd.TraceSum3Name = name;
                            fd.TraceSum3 = TraceSum;
                            fd.InitialSum3 = InitialSum;
                            break;
                        case 4:
                            fd.TraceSum4Name = name;
                            fd.TraceSum4 = TraceSum;
                            fd.InitialSum4 = InitialSum;
                            break;
                        case 5:
                            fd.TraceSum5Name = name;
                            fd.TraceSum5 = TraceSum;
                            fd.InitialSum5 = InitialSum;
                            break;
                        case 6:
                            fd.TraceSum6Name = name;
                            fd.TraceSum6 = TraceSum;
                            fd.InitialSum6 = InitialSum;
                            break;
                    }
                }
            }
            return base.GetPrintParameter();
        }


    }
}
