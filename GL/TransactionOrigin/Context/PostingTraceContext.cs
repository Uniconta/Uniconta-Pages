using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    /// <summary>
    /// Supplies the transactions and lazily cached trace results for one voucher.
    /// The owning page keeps this context alive so the screen and assistant share
    /// the same resolver work.
    /// </summary>
    public sealed class PostingTraceVoucherContext
    {
        private readonly Func<GLTransClient, Task<PostingTraceResult>> traceProvider;
        private readonly Dictionary<GLTransClient, Task<PostingTraceResult>> results = new Dictionary<GLTransClient, Task<PostingTraceResult>>();

        public PostingTraceVoucherContext(IEnumerable<GLTransClient> transactions,
            Func<GLTransClient, Task<PostingTraceResult>> traceProvider)
        {
            Transactions = (transactions ?? Enumerable.Empty<GLTransClient>()).Where(transaction => transaction != null).ToArray();

            this.traceProvider = traceProvider ?? throw new ArgumentNullException(nameof(traceProvider));
        }

        public GLTransClient[] Transactions { get; }

        public Task<PostingTraceResult> GetResultAsync(GLTransClient transaction)
        {
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            lock (results)
            {
                if (!results.TryGetValue(transaction, out var result))
                {
                    result = traceProvider(transaction);
                    results.Add(transaction, result);
                }
                return result;
            }
        }
    }

    public abstract class PostingTraceLine
    {
        public abstract InvItemClient InvItem { get; }
        public abstract string Project { get; }
        public abstract string Vat { get; }
        public abstract string PostingAccount { get; }
        public abstract string SourceLineId { get; }
    }

    public sealed class InvoicePostingTraceLine : PostingTraceLine
    {
        private readonly InvTransInvoice line;

        public InvoicePostingTraceLine(InvTransInvoice line) => this.line = line;
        public override InvItemClient InvItem => line.InvItem;
        public override string Project => line._Project;
        public override string Vat => line.Vat;
        public override string PostingAccount => line._PostingAccount;
        public override string SourceLineId => line._InvoiceRowId.ToString();
    }

    public sealed class CreditorOrderPostingTraceLine : PostingTraceLine
    {
        private readonly CreditorOrderLineClient line;
        public CreditorOrderPostingTraceLine(CreditorOrderLineClient line) => this.line = line;
        public override InvItemClient InvItem => line.InvItem;
        public override string Project => line._Project;
        public override string Vat => line.Vat;
        public override string PostingAccount => line._PostingAccount;
        public override string SourceLineId => line.RowId.ToString();
    }

    public sealed class DebtorOrderPostingTraceLine : PostingTraceLine
    {
        private readonly DebtorOrderLineClient line;
        public DebtorOrderPostingTraceLine(DebtorOrderLineClient line) => this.line = line;
        public override InvItemClient InvItem => line.InvItem;
        public override string Project => line._Project;
        public override string Vat => line.Vat;
        public override string PostingAccount => line._PostingAccount;
        public override string SourceLineId => line.RowId.ToString();
    }

    /// <summary>
    /// Data already loaded for resolving a single posting trace.
    /// Workers must use this contract exclusively and must not fetch data themselves.
    /// </summary>
    public interface IPostingTraceContext
    {
        GLTransClient GLTrans { get; }
        GLTransRefType DCType { get; }
        bool IsDebtor { get; }
        string PostedAccount { get; }
        DCGroup DCGroup { get; }
        DCAccount DCAccount { get; }
        DCOrder Order { get; }
        DCInvoiceClient Invoice { get; }
        IEnumerable<PostingTraceLine> InvoiceLines { get; }
        List<GroupPostingWrapper> InvGroupsForPosting { get; }
        List<GroupPostingWrapper> PurchaseProjectStockConsumptionGroups { get; }
        List<GroupPostingWrapper> FixedCostPurchaseProfitLossGroups { get; }
        List<CreditorOrderCost> PurchaseChargeCosts { get; }
        IEnumerable<InvDutyGroupLineClient> InvDuties { get; }
        IEnumerable<CreditorOrderCostClient> OrderCosts { get; }
        IEnumerable<PaymentTermClient> PaymentTerms { get; }
        List<ProjectGroupClient> ProjectGroupsForPosting { get; }
        List<VatPostingWrapper> VatPostingsForInvoice { get; }
        IEnumerable<GLVatClient> AllVatCodes { get; }
        string SystemVatRoundingAccount { get; }
        string SystemPennyDifferenceAccount { get; }
        string SystemErrorAccount { get; }
        string SystemWithholdingSalesAccount { get; }
        string SystemWithholdingBuyAccount { get; }

        string GetFinancialAccountName(string account);
        string GetFinancialAccountType(string account);
    }

    public class PostingTraceContext : IPostingTraceContext
    {
        public GLTransClient GLTrans { get; set; }

        public virtual GLTransRefType DCType => GLTrans._DCType;

        protected virtual string DCAccountNumber => GLTrans?.DCAccount;

        protected virtual bool ShouldLoadOrderCosts => GLTrans != null && GLTrans._Origin == LedgerPostingType.PurchaseCharges;

        public bool IsDebtor => DCType == GLTransRefType.Debtor;

        public int CompanyId { get; set; }

        public string PostedAccount { get; set; }

        public string InvoiceNumber { get; set; }

        public int Voucher { get; set; }

        public string Origin { get; set; }

        public double Amount { get; set; }

        public double VatAmount { get; set; }

        public DateTime Date { get; set; }

        public DCGroup DCGroup { get; set; }
        public DCAccount DCAccount { get; set; }
        public DCOrder Order { get; set; }

        public DCInvoiceClient Invoice { get; set; }
        public IEnumerable<PostingTraceLine> InvoiceLines { get; set; }

        public IEnumerable<InvGroupClient> InvGroup { get; set; }
        public List<GroupPostingWrapper> InvGroupsForPosting { get; set; }

        public List<GroupPostingWrapper> PurchaseProjectStockConsumptionGroups { get; set; }
        public List<GroupPostingWrapper> FixedCostPurchaseProfitLossGroups { get; set; }

        public IEnumerable<DCGroupPostingClient> AllDCGroupPostings { get; set; }
        public IEnumerable<DCGroupPostingClient> DCGroupPostings { get; set; }


        public IEnumerable<CreditorOrderCostLineClient> PurchaseChargeLines { get; set; }
        public SQLCache PurchaseChargeCostGroups { get; set; }
        public List<CreditorOrderCost> PurchaseChargeCosts { get; set; }

        public IEnumerable<InvDutyGroupLineClient> InvDuties { get; set; }
        public IEnumerable<CreditorOrderCostClient> OrderCosts { get; set; }
        public IEnumerable<PaymentTermClient> PaymentTerms { get; set; }



        public List<ProjectGroupClient> ProjectGroupsForPosting { get; set; }

        public IEnumerable<GLVatClient> AllVatCodes { get; set; }

        public IEnumerable<GLAccountClient> AllAccounts { get; set; }
        public List<VatPostingWrapper> VatPostingsForInvoice { get; set; }

        public string SystemVatRoundingAccount { get; set; }
        public string SystemPennyDifferenceAccount { get; set; }
        public string SystemErrorAccount { get; set; }

        public string SystemWithholdingSalesAccount { get; set; }
        public string SystemWithholdingBuyAccount { get; set; }

        public async Task Initialize(CrudAPI api)
        {
            if (api == null)
                throw new ArgumentNullException(nameof(api));
            //GLDailyJournalPostedClient

            /* master data first */
            await LoadSystemData(api);

            await LoadInvoiceData(api);

            BuildVatPostingsForInvoice();
            await BuildDCGroupPostings(api);

            await Task.WhenAll(  // paralallalllalalalism baby!
                LoadPurchaseCharges(api),
                LoadInvDuties(api),
                LoadCostOrderClient(api),
                LoadPaymentTerms(api),
                LoadProjectGroupsForPosting(api)
            );
        }

        private async Task LoadSystemData(CrudAPI api)
        {
            AllVatCodes = await api.LoadCache<GLVatClient>();

            AllAccounts = await api.LoadCache<GLAccountClient>();

            var allGls = AllAccounts ?? Enumerable.Empty<GLAccountClient>();

            SystemVatRoundingAccount = allGls.FirstOrDefault(g => g.SystemAccountEnum == SystemAccountTypes.VatRounding)?.Account;
            SystemPennyDifferenceAccount = allGls.FirstOrDefault(g => g.SystemAccountEnum == SystemAccountTypes.PennyDif)?.Account;
            SystemErrorAccount = allGls.FirstOrDefault(g => g.SystemAccountEnum == SystemAccountTypes.ErrorAccount)?.Account;
            SystemWithholdingSalesAccount = allGls.FirstOrDefault(g => g.SystemAccountEnum == SystemAccountTypes.WithholdingSales)?.Account;
            SystemWithholdingBuyAccount = allGls.FirstOrDefault(g => g.SystemAccountEnum == SystemAccountTypes.WithholdingBuy)?.Account;
        }


        public GLAccountClient FindFinancialAccount(string account)
        {
            if (string.IsNullOrWhiteSpace(account) || AllAccounts == null)
                return null;

            return AllAccounts.FirstOrDefault(g => g != null && g.Account == account);
        }

        public string GetFinancialAccountName(string account)
        {
            return Convert.ToString(GetAccountProperty(FindFinancialAccount(account), "Name"));
        }

        public string GetFinancialAccountType(string account)
        {
            return Convert.ToString(GetAccountProperty(FindFinancialAccount(account), "AccountType"));
        }

        private static object GetAccountProperty(object account, string propertyName)
        {
            if (account == null || string.IsNullOrWhiteSpace(propertyName))
                return null;

            var property = account.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            return property == null ? null : property.GetValue(account, null);
        }

        private async Task LoadPurchaseCharges(CrudAPI api)
        {
            PurchaseChargeLines = new List<CreditorOrderCostLineClient>();

            if (DCType != GLTransRefType.Creditor)
                return;

            if (Order == null)
                return;

            PurchaseChargeCostGroups = await api.LoadCache(typeof(CreditorOrderCostClient));
            PurchaseChargeLines = await api.Query<CreditorOrderCostLineClient>(Order as UnicontaBaseEntity);
            PurchaseChargeCosts = new List<CreditorOrderCost>();

            foreach (var line in PurchaseChargeLines.Where(l => l != null))
            {
                if (string.IsNullOrEmpty(line.CostGroup))
                    continue;

                var cost = PurchaseChargeCostGroups.Get(line.CostGroup) as CreditorOrderCost;

                if (cost == null)
                    continue;

                PurchaseChargeCosts.Add(cost);
            }
        }

        private async Task LoadInvDuties(CrudAPI api)
        {
            if (InvoiceLines == null)
                return;

            var dutyGroupKeys = new HashSet<string>(
                InvoiceLines
                    .Where(l => l != null)
                    .Where(l => l.InvItem != null)
                    .Where(l => !string.IsNullOrEmpty(l.InvItem._DutyGroup))
                    .Select(l => l.InvItem._DutyGroup));

            if (dutyGroupKeys.Count == 0)
                return;

            var dutyLines = await api.Query<InvDutyGroupLineClient>();

            if (dutyLines == null)
                return;

            InvDuties = dutyLines
                .Where(l => l != null)
                .Where(l => !string.IsNullOrEmpty(l.DutyGroup))
                .Where(l => dutyGroupKeys.Contains(l.DutyGroup))
                .ToList();
        }

        private async Task LoadPaymentTerms(CrudAPI api)
        {
            PaymentTerms = await api.LoadCache<PaymentTermClient>();
        }

        private async Task LoadCostOrderClient(CrudAPI api)
        {
            if (!ShouldLoadOrderCosts)
                return;

            OrderCosts = await api.LoadCache<CreditorOrderCostClient>();
        }

        private async Task LoadProjectGroupsForPosting(CrudAPI api)
        {
            ProjectGroupsForPosting = new List<ProjectGroupClient>();

            if (InvoiceLines == null)
                return;

            var projectKeys = new HashSet<string>(
                InvoiceLines
                    .Where(l => l != null)
                    .Where(l => !string.IsNullOrEmpty(l.Project))
                    .Select(l => l.Project));

            if (projectKeys.Count == 0)
                return;

            var projects = await api.LoadCache<ProjectClient>();

            if (projects == null)
                return;

            var relevantProjects = projects
                .Where(p => p != null)
                .Where(p => projectKeys.Contains(p._Number))
                .ToList();

            if (relevantProjects.Count == 0)
                return;

            foreach (var project in relevantProjects)
            {
                if (project.ProjectGroup == null)
                    continue;

                if (ProjectGroupsForPosting.Any(g => g != null && g.KeyStr == project.ProjectGroup.KeyStr))
                    continue;

                ProjectGroupsForPosting.Add(project.ProjectGroup);
            }
        }

        protected virtual async Task LoadInvoiceData(CrudAPI api)
        {
            if (DCType == GLTransRefType.Debtor)
            {
                Invoice = (await api.Query<DebtorInvoiceClient>(new[]
                {
                    PropValuePair.GenereteWhereElements(typeof(DebtorInvoiceClient).GetProperty(nameof(DebtorInvoiceClient.InvoiceNumber)), GLTrans.Invoice.ToString()),
                    PropValuePair.GenereteWhereElements(typeof(DebtorInvoiceClient).GetProperty(nameof(DebtorInvoiceClient.Voucher)), GLTrans.Voucher.ToString())
                })).FirstOrDefault();

                await LoadDCAccount(api);

                if (Invoice != null)
                {
                    Order = (await api.Query<DebtorOrderClient>(new[]
                    {
                        PropValuePair.GenereteWhereElements(typeof(DebtorOrderClient).GetProperty(nameof(DebtorOrderClient.OrderNumber)), Invoice.OrderNumber.ToString())
                    })).FirstOrDefault();

                    var invoiceLines = await api.Query<DebtorInvoiceLines>(Invoice);

                    InvoiceLines = invoiceLines
                        .Where(line => line != null)
                        .Select(line => new InvoicePostingTraceLine(line))
                        .ToList();
                }
            }
            else if (DCType == GLTransRefType.Creditor)
            {
                Invoice = (await api.Query<CreditorInvoiceClient>(new[]
                {
                    PropValuePair.GenereteWhereElements(typeof(CreditorInvoiceClient).GetProperty(nameof(CreditorInvoiceClient.InvoiceNumber)), GLTrans.Invoice.ToString()),
                    PropValuePair.GenereteWhereElements(typeof(CreditorInvoiceClient).GetProperty(nameof(CreditorInvoiceClient.Voucher)), GLTrans.Voucher.ToString())
                })).FirstOrDefault();

                await LoadDCAccount(api);

                if (Invoice != null)
                {
                    Order = (await api.Query<CreditorOrderClient>(new[]
                    {
                        PropValuePair.GenereteWhereElements(typeof(CreditorOrderClient).GetProperty(nameof(CreditorOrderClient.OrderNumber)), Invoice.OrderNumber.ToString())
                    })).FirstOrDefault();

                    var invoiceLines = await api.Query<CreditorInvoiceLines>(Invoice);

                    InvoiceLines = invoiceLines
                        .Where(line => line != null)
                        .Select(line => new InvoicePostingTraceLine(line))
                        .ToList();
                }
            }

            InvoiceLines = InvoiceLines ?? Enumerable.Empty<PostingTraceLine>();
        }

        protected async Task LoadDCAccount(CrudAPI api)
        {
            var accountNumber = DCAccountNumber;

            if (string.IsNullOrWhiteSpace(accountNumber))
                return;

            if (DCType == GLTransRefType.Debtor)
            {
                var debtors = await api.LoadCache<DebtorClient>();
                DCAccount = debtors?.FirstOrDefault(debtor => debtor != null && debtor.Account == accountNumber);

                DCGroup = (DCAccount as DebtorClient)?.DebtorGroup;
            }
            else if (DCType == GLTransRefType.Creditor)
            {
                var creditors = await api.LoadCache<CreditorClient>();
                DCAccount = creditors?.FirstOrDefault(creditor => creditor != null && creditor.Account == accountNumber);

                DCGroup = (DCAccount as CreditorClient)?.CreditorGroup;
            }
        }

        private async Task BuildDCGroupPostings(CrudAPI api)
        {
            AllDCGroupPostings = await api.Query<DCGroupPostingClient>();

            // InvItemGroups
            InvGroup = InvoiceLines.Where(i => i != null && i.InvItem != null
                                        && i.InvItem.InventoryGroup != null)
                                    .Select(i => i.InvItem.InventoryGroup)
                                    .GroupBy(g => g.Group)
                                    .Select(g => g.First())
                                    .ToList();

            var invGroupKeys = new HashSet<string>(InvGroup.Where(g => g != null && !string.IsNullOrEmpty(g.Group)).Select(g => g.Group));

            // overridden groups
            if (DCGroup != null)
            {
                DCGroupPostings = AllDCGroupPostings
                    .Where(p => p != null
                                && (!string.IsNullOrEmpty(p.InvGroup)
                                && invGroupKeys.Contains(p.InvGroup)) || p.Group == DCGroup._Group)
                    .ToList();
            }

            BuidlInvGroupsForPosting();
        }

        private void BuidlInvGroupsForPosting()
        {
            InvGroupsForPosting = new List<GroupPostingWrapper>();

            if (DCGroup != null)
            {
                var invGroupKeys = new HashSet<string>(InvGroup.Where(g => g != null && !string.IsNullOrEmpty(g.Group)).Select(g => g.Group));

                DCGroupPostings = AllDCGroupPostings
                    .Where(p => (!string.IsNullOrEmpty(p?.InvGroup) && invGroupKeys.Contains(p.InvGroup)) && p.Group == DCGroup._Group)
                    .ToList();

                var overriddenInvGroups = new HashSet<string>(DCGroupPostings.Where(p => !string.IsNullOrEmpty(p.InvGroup)).Select(p => p.InvGroup));

                foreach (var posting in DCGroupPostings)
                {
                    InvGroupsForPosting.Add(GroupPostingWrapper.From(posting, DCAccount._VatZone));
                }

                // Add InvGroup base wrappers only when not overridden by DCGroupPosting
                foreach (var invGroup in InvGroup)
                {
                    if (invGroup == null || string.IsNullOrEmpty(invGroup.Group))
                        continue;


                    var wrapper = GroupPostingWrapper.From(invGroup, DCAccount._VatZone, IsDebtor);
                    wrapper.IsOnItemLevelOnly = overriddenInvGroups.Contains(invGroup.Group);

                    InvGroupsForPosting.Add(wrapper);
                }

                InvGroupsForPosting.Add(GroupPostingWrapper.From(DCGroup, DCAccount._VatZone));
            }
            BuildPurchaseProjectStockConsumptionGroups();
            BuildFixedCostPurchaseProfitLossGroups();
        }

        private void BuildVatPostingsForInvoice()
        {
            VatPostingsForInvoice = new List<VatPostingWrapper>();  // start empty

            if (InvoiceLines == null || AllVatCodes == null)
                return;

            var vatLineSources = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var vatHeaderSources = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            Action<string, string> addLineSource = (vatCode, lineId) =>
            {
                if (string.IsNullOrEmpty(vatCode) || string.IsNullOrEmpty(lineId))
                    return;

                List<string> lines;
                if (!vatLineSources.TryGetValue(vatCode, out lines))
                {
                    lines = new List<string>();
                    vatLineSources.Add(vatCode, lines);
                }

                if (!lines.Contains(lineId))
                    lines.Add(lineId);
            };

            Action<string, string> addHeaderSource = (vatCode, source) =>
            {
                if (string.IsNullOrEmpty(vatCode) || string.IsNullOrEmpty(source))
                    return;

                List<string> sources;
                if (!vatHeaderSources.TryGetValue(vatCode, out sources))
                {
                    sources = new List<string>();
                    vatHeaderSources.Add(vatCode, sources);
                }

                sources.Add(source);
            };

            foreach (var line in InvoiceLines.Where(l => l != null && !string.IsNullOrEmpty(l.Vat)))
            {
                addLineSource(line.Vat, line.SourceLineId);
            }

            if (Invoice != null)
            {
                addHeaderSource(Invoice.Vat, Uniconta.ClientTools.Localization.lookup("GLTraceInvoiceHeaderVat"));
                addHeaderSource(Invoice.Vat2, Uniconta.ClientTools.Localization.lookup("GLTraceInvoiceHeaderVat2"));
                addHeaderSource(Invoice.Vat3, Uniconta.ClientTools.Localization.lookup("GLTraceInvoiceHeaderVat3"));
            }

            VatPostingsForInvoice = AllVatCodes.Where(v => !string.IsNullOrEmpty(v?.Vat) && (vatLineSources.ContainsKey(v.Vat) || vatHeaderSources.ContainsKey(v.Vat)))
                .Select(v =>
                {
                    var wrapper = new VatPostingWrapper(v);

                    var sourceParts = new List<string>();

                    List<string> lines;
                    if (vatLineSources.TryGetValue(v.Vat, out lines))
                    {
                        if (lines.Count > 5)
                            sourceParts.Add(string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceInvoiceLinesWithVat"), lines.Count));
                        else
                            sourceParts.Add(string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceInvoiceLines"), string.Join(",", lines)));
                    }

                    List<string> headerSources;
                    if (vatHeaderSources.TryGetValue(v.Vat, out headerSources))
                        sourceParts.AddRange(headerSources);

                    wrapper.SourceOfVAT = string.Join(", ", sourceParts);
                    wrapper.IsManuallySelected = IsManuallySelectedVatCode(v.Vat);

                    return wrapper;
                })
                .ToList();
        }

        private bool IsManuallySelectedVatCode(string vatCode)
        {
            if (string.IsNullOrWhiteSpace(vatCode))
                return false;

            return !InvoiceLines
                .Where(line => line != null && string.Equals(line.Vat, vatCode, StringComparison.OrdinalIgnoreCase))
                .Any(LineUsesConfiguredVatCode)
                && !HeaderUsesConfiguredVatCode(vatCode);
        }

        private bool LineUsesConfiguredVatCode(PostingTraceLine line)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.Vat))
                return false;

            var itemGroup = line.InvItem?.InventoryGroup;
            if (itemGroup != null && string.Equals(GetItemGroupVatCode(itemGroup), line.Vat, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
            //var postingAccount = FindFinancialAccount(line.PostingAccount);
            //return postingAccount != null && string.Equals(postingAccount.Vat, line.Vat, StringComparison.OrdinalIgnoreCase);
        }

        private bool HeaderUsesConfiguredVatCode(string vatCode)
        {
            if (DCAccount != null && string.Equals(DCAccount._Vat, vatCode, StringComparison.OrdinalIgnoreCase))
                return true;

            if (DCGroup != null && string.Equals(GetVatZoneProperty(DCGroup, "Vat"), vatCode, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
            //var postedAccount = FindFinancialAccount(PostedAccount);
            //return postedAccount != null && string.Equals(postedAccount.Vat, vatCode, StringComparison.OrdinalIgnoreCase);
        }

        private string GetItemGroupVatCode(InvGroupClient itemGroup)
        {
            return GetVatZoneProperty(itemGroup, IsDebtor ? "SalesVat" : "PurchaseVat");
        }

        private string GetVatZoneProperty(object setup, string propertyPrefix)
        {
            if (setup == null || string.IsNullOrWhiteSpace(propertyPrefix))
                return null;

            var vatZoneIndex = DCAccount == null ? 0 : (int)DCAccount._VatZone;
            var propertyName = vatZoneIndex == 0 ? propertyPrefix : propertyPrefix + vatZoneIndex;

            return Convert.ToString(GetAccountProperty(setup, propertyName));
        }

        private void BuildPurchaseProjectStockConsumptionGroups()
        {
            PurchaseProjectStockConsumptionGroups = new List<GroupPostingWrapper>();

            if (DCType != GLTransRefType.Creditor)
                return;

            if (InvoiceLines == null)
                return;

            foreach (var line in InvoiceLines.Where(IsPurchaseProjectStockConsumptionLine))
            {
                var wrapper = ResolvePostingWrapperForLine(line);

                if (wrapper == null)
                    continue;

                if (!PurchaseProjectStockConsumptionGroups.Any(g =>
                    g.SourceTableType == wrapper.SourceTableType &&
                    g.SourceTableKey == wrapper.SourceTableKey))
                {
                    PurchaseProjectStockConsumptionGroups.Add(wrapper);
                }
            }
        }

        private void BuildFixedCostPurchaseProfitLossGroups()
        {
            FixedCostPurchaseProfitLossGroups = new List<GroupPostingWrapper>();

            if (DCType != GLTransRefType.Creditor)
                return;

            if (InvoiceLines == null)
                return;

            foreach (var line in InvoiceLines.Where(IsFixedCostPurchaseProfitLossLine))
            {
                var wrapper = ResolvePostingWrapperForLine(line);

                if (wrapper == null)
                    continue;

                if (!FixedCostPurchaseProfitLossGroups.Any(g =>
                    g.SourceTableType == wrapper.SourceTableType &&
                    g.SourceTableKey == wrapper.SourceTableKey))
                {
                    FixedCostPurchaseProfitLossGroups.Add(wrapper);
                }
            }
        }

        private GroupPostingWrapper ResolvePostingWrapperForLine(PostingTraceLine line)
        {
            if (line == null)
                return null;

            if (line.InvItem == null || line.InvItem.InventoryGroup == null)
                return null;

            var invGroup = line.InvItem.InventoryGroup.Group;

            if (string.IsNullOrEmpty(invGroup))
                return null;

            var dcGroup = DCGroup == null ? null : DCGroup._Group;

            DCGroupPostingClient posting = null;

            if (DCGroupPostings != null)
            {
                posting = DCGroupPostings
                    .Where(p => p != null)
                    .Where(p => p._DCType == (byte)DCType)
                    .Where(p => string.IsNullOrEmpty(dcGroup) || p.Group == dcGroup)
                    .Where(p => p.InvGroup == invGroup)
                    .FirstOrDefault();

                if (posting == null)
                {
                    posting = DCGroupPostings
                        .Where(p => p != null)
                        .Where(p => p._DCType == (byte)DCType)
                        .Where(p => string.IsNullOrEmpty(dcGroup) || p.Group == dcGroup)
                        .Where(p => string.IsNullOrEmpty(p.InvGroup))
                        .FirstOrDefault();
                }
            }

            if (posting != null)
                return GroupPostingWrapper.From(posting, DCAccount._VatZone);

            return GroupPostingWrapper.From(line.InvItem.InventoryGroup, DCAccount._VatZone, IsDebtor);
        }

        private bool IsPurchaseProjectStockConsumptionLine(PostingTraceLine line)
        {
            if (line == null)
                return false;

            if (DCType != GLTransRefType.Creditor)
                return false;

            if (line.InvItem == null)
                return false;

            if (string.IsNullOrEmpty(line.Project))
                return false;

            // !! TODO: Replace with the actual stock-update condition if available. !!
            // For now, having an item + inventory group is the minimum useful signal.
            if (line.InvItem.InventoryGroup == null)
                return false;

            return true;
        }

        private bool IsFixedCostPurchaseProfitLossLine(PostingTraceLine line)
        {
            if (line == null)
                return false;

            if (DCType != GLTransRefType.Creditor)
                return false;

            if (line.InvItem == null)
                return false;

            if (line.InvItem.InventoryGroup == null)
                return false;

            // TODO:
            // This path should only be true when the purchase line can create
            // a fixed-cost purchase price/cost difference.
            //
            // Required real checks are probably something like:
            // - item uses fixed cost / fixed cost price model
            // - purchase cost differs from expected fixed cost
            // - cost difference is non-zero
            //
            // Until those fields are identified, keep this false to avoid false positives.

            return false;
        }

    }

    /// <summary>
    /// Posting trace context for an order that is currently being posted or simulated.
    /// The debtor/creditor type, company and account are inferred from the supplied order.
    /// No posted invoice lookup is performed.
    /// </summary>
    public sealed class PostedOrderPostingTraceContext : PostingTraceContext
    {
        private readonly DCOrder postedOrder;
        private readonly IEnumerable<DCOrderLine> postedOrderLines;
        private readonly GLTransRefType dcType;
        private readonly string dcAccountNumber;


        public PostedOrderPostingTraceContext(DCOrder postedOrder, IEnumerable<DCOrderLine> postedOrderLines)
        {
            if (postedOrder == null)
                throw new ArgumentNullException(nameof(postedOrder));

            if (postedOrderLines == null)
                throw new ArgumentNullException(nameof(postedOrderLines));

            this.postedOrder = postedOrder;
            this.postedOrderLines = postedOrderLines.Where(line => line != null).ToList();

            dcType = postedOrder is DebtorOrder ? GLTransRefType.Debtor : GLTransRefType.Creditor;
            dcAccountNumber = postedOrder._DCAccount;

            Order = postedOrder;
            CompanyId = postedOrder.CompanyId;
        }

        public override GLTransRefType DCType => dcType;

        protected override string DCAccountNumber => dcAccountNumber;

        protected override bool ShouldLoadOrderCosts => false;

        protected override async Task LoadInvoiceData(CrudAPI api)
        {
            Invoice = null;
            Order = postedOrder;

            if (DCType == GLTransRefType.Debtor)
            {
                InvoiceLines = postedOrderLines
                    .OfType<DebtorOrderLineClient>()
                    .Select(line => new DebtorOrderPostingTraceLine(line))
                    .ToList();
            }
            else
            {
                InvoiceLines = postedOrderLines
                    .OfType<CreditorOrderLineClient>()
                    .Select(line => new CreditorOrderPostingTraceLine(line))
                    .ToList();
            }

            await LoadDCAccount(api);
        }
    }

}
