using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.AI;
using Uniconta.ClientTools.AI.Files;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.AI.Services;
using Uniconta.ClientTools.AI.Services.Context;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls.Dialogs;
using UnicontaClient.Models;
using UnicontaClient.Pages.GL.TransactionOrigin.Visuals;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public interface IGLPostingSourceTraceConsumer
    {
        GLTransClient GetSelectedVoucherTransaction();
        Task<PostingTraceVoucherContext> GetPostingTraceContextAsync(GLTransClient transaction, CrudAPI api);
        void ShowPostingSourceTrace(GLTransClient selectionOverride);
    }

    public sealed class GLPostingSourceTraceOpenparms
    {
        public IGLPostingSourceTraceConsumer Consumer { get; set; }

        public GLTransClient SelectionOverride { get; set; }

        public DCOrder SimulatedOrder { get; set; }
        public IEnumerable<DCOrderLine> SimulatedOrderLines { get; set; }

        public bool ShowCompleteVoucher => SelectionOverride == null
            && (!System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.LeftShift)
             && !System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.RightShift));
    }

    public sealed class PostingTraceMatchGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(PostingTraceMatchResult); } }
        public override bool Readonly { get { return true; } }
    }

    public class PostingValidationBase
    {
        public bool IsValid { get; set; }
        public string Explanation { get; set; }
        public int CertaintyPercentage { get; set; }

    }

    public sealed class PostingValidationHeader : PostingValidationBase
    {
        public PostingValidationLines Lines { get; set; }
    }

    public sealed class PostingValidationLines : List<PostingValidationBase>
    {
    }

    public partial class GLPostingSourceTrace : FormBasePage, IAIPromptEditDialogModel, IAIAssistantConsumerWithPromptSuggestions
    {
        private GLTransClient editrow;
        private GLTransClient selectedTransaction;
        private GLTransClient[] voucherTransactions;
        private List<PostingTraceResult> traceResults;
        private bool isLoading;
        private Func<GLTransClient, Task<GLTransClient[]>> recordProvider;
        private PostingTraceVoucherContext postingTraceContext;
        private readonly GLPostingSourceTraceOpenparms OpenParms;

        private PromptDefinition pagePrompt;

        public PromptDefinition Prompt { get { return pagePrompt ?? GetPostingValidationPrompt(); } set { pagePrompt = value; } }
        public PromptDefinition OriginalPrompt => GetPostingValidationPrompt();
        public IReadOnlyList<PromptDefinitionReplacementAttribute> Replacements =>
            typeof(GLPostingSourceTrace)
                .GetProperty(nameof(PostingValidationPrompt), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .GetCustomAttributes<PromptDefinitionReplacementAttribute>()
                .ToList();

        private enum TransactionSortOrder
        {
            AccountAscending,
            AccountDescending,
            AccountNameAscending,
            AccountNameDescending,
            AccountTypeAscending,
            AccountTypeDescending
        }

        public override void OnClosePage(object[] refreshParams) { globalEvents.OnRefresh(NameOfControl, refreshParams); }
        public override string NameOfControl { get { return TabControls.GLPostingSourceTrace; } }
        public override Type TableType { get { return typeof(GLTransClient); } }
        public override UnicontaBaseEntity ModifiedRow { get => null; set { } }

        protected override bool IsLayoutSaveRequired() => false;

        //public GLPostingSourceTrace(SynchronizeEntity syncEntity, GLPostingSourceTraceOpenparms openParms) : this(syncEntity, openParms, null, null){}

        /// <summary>
        /// Opens a trace for a simulation while retaining the transient order and invoice lines
        /// used to produce its postings.
        /// </summary>
        public GLPostingSourceTrace(SynchronizeEntity syncEntity, GLPostingSourceTraceOpenparms openParms) : base(true, syncEntity)
        {
            InitializePage();

            OpenParms = openParms;
            recordProvider = async transaction =>
            {
                postingTraceContext = await OpenParms.Consumer.GetPostingTraceContextAsync(transaction, api);
                return postingTraceContext?.Transactions;
            };

            _ = LoadPostingTraceAsync(openParms.SelectionOverride ?? syncEntity.Row as GLTransClient, openParms.ShowCompleteVoucher);
        }

        public GLPostingSourceTrace(UnicontaBaseEntity sourcedata) : base(sourcedata, true)
        {
            InitializePage();
            recordProvider = async transaction => (await api.Query<GLTransClient>(transaction))?.Where(x => x != null).ToArray();
            _ = LoadPostingTraceAsync(sourcedata as GLTransClient, true);
        }

        public GLPostingSourceTrace(CrudAPI api, string dummy) : base(api, dummy) => InitializePage();

        private void InitializePage()
        {
            InitializeComponent();
            BusyIndicator = busyIndicator;
            PopulateTransactionSortSelector();
            visualizer.Initialize(api);

            layoutControl = layoutItems;

            ZoomEnabler.EnableZoom(visualizer);
            matchDataGrid.api = api;
            matchDataGrid.BusyIndicator = BusyIndicator;
            matchDataGrid.Visibility = Visibility.Visible;
            ribbonControl = frmRibbon;
            frmRibbon.OnItemClicked += Ribbon_OnItemClicked;

            GPTServiceFactory.Instance.GetActiveService(api)?.Branding?.UpdateRibbonIcon(frmRibbon, "AskAI");
        }

        private async Task LoadPostingTraceAsync(GLTransClient selectedItem, bool showCompleteVoucher)
        {
            if (selectedItem == null || recordProvider == null)
                return;

            selectedTransaction = selectedItem;
            postingTraceContext = null;
            voucherTransactions = (await recordProvider(selectedTransaction))?.Where(x => x != null).ToArray();

            if (voucherTransactions == null || voucherTransactions.Length == 0)
                voucherTransactions = new[] { selectedTransaction };

            if (postingTraceContext == null)
            {
                var resolver = new InvoicePostingTraceResolver();
                postingTraceContext = new PostingTraceVoucherContext(voucherTransactions,
                    transaction => resolver.TracePostingAsync(transaction, api));
            }

            isLoading = true;
            try
            {
                PopulateInvoiceSelector();
                PopulateTransactionSelector(showCompleteVoucher);
            }
            finally
            {
                isLoading = false;
            }

            await ReloadPostingTraceAsync();
        }

        private async void TransactionSelector_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (!isLoading && selectedTransaction != null)
                await ReloadPostingTraceAsync();
        }

        private async void InvoiceSelector_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (isLoading || selectedTransaction == null)
                return;

            isLoading = true;
            try
            {
                PopulateTransactionSelector(true);
            }
            finally
            {
                isLoading = false;
            }

            await ReloadPostingTraceAsync();
        }

        private void TransactionSortSelector_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (traceResults != null)
                visualizer.Results = OrderTraceResults(traceResults).ToArray();
        }

        private void PopulateTransactionSortSelector()
        {
            AddTransactionSortOption(Uniconta.ClientTools.Localization.lookup("GLTraceAccountAsc"), TransactionSortOrder.AccountAscending);
            AddTransactionSortOption(Uniconta.ClientTools.Localization.lookup("GLTraceAccountDesc"), TransactionSortOrder.AccountDescending);
            AddTransactionSortOption(Uniconta.ClientTools.Localization.lookup("GLTraceAccountNameAsc"), TransactionSortOrder.AccountNameAscending);
            AddTransactionSortOption(Uniconta.ClientTools.Localization.lookup("GLTraceAccountNameDesc"), TransactionSortOrder.AccountNameDescending);
            AddTransactionSortOption(Uniconta.ClientTools.Localization.lookup("GLTraceAccountTypeAsc"), TransactionSortOrder.AccountTypeAscending);
            AddTransactionSortOption(Uniconta.ClientTools.Localization.lookup("GLTraceAccountTypeDesc"), TransactionSortOrder.AccountTypeDescending);
            transactionSortSelector.SelectedIndex = 0;
        }

        private void AddTransactionSortOption(string text, TransactionSortOrder sortOrder)
        {
            transactionSortSelector.Items.Add(new UnicontaComboBoxEditor
            {
                Content = text,
                Tag = sortOrder
            });
        }

        private void PopulateInvoiceSelector()
        {
            invoiceSelector.Items.Clear();
            invoiceSelector.Items.Add(new UnicontaComboBoxEditor
            {
                Content = Uniconta.ClientTools.Localization.lookup("GLTraceAllInvoices"),
                Tag = null
            });

            foreach (var invoice in voucherTransactions
                .Where(x => x != null)
                .Select(x => x.Invoice)
                .Distinct()
                .OrderBy(x => x))
            {
                invoiceSelector.Items.Add(new UnicontaComboBoxEditor
                {
                    Content = invoice.ToString(),
                    Tag = invoice
                });
            }

            invoiceSelector.SelectedIndex = 0;
        }

        private void PopulateTransactionSelector(bool showCompleteVoucher)
        {
            var transactions = GetInvoiceFilteredTransactions().ToArray();

            transactionSelector.Items.Clear();
            transactionSelector.Items.Add(new UnicontaComboBoxEditor
            {
                Content = Uniconta.ClientTools.Localization.lookup("GLTraceEntireVoucher"),
                Tag = transactions
            });

            UnicontaComboBoxEditor selectedComboBoxItem = null;
            foreach (var voucherTransaction in transactions)
            {
                var comboBoxItem = new UnicontaComboBoxEditor
                {
                    Content = FormatTransactionSelectionText(voucherTransaction),
                    Tag = voucherTransaction
                };
                transactionSelector.Items.Add(comboBoxItem);

                if (!showCompleteVoucher && IsSameTransaction(voucherTransaction, selectedTransaction))
                    selectedComboBoxItem = comboBoxItem;
            }

            transactionSelector.SelectedIndex = showCompleteVoucher ? 0 : -1;
            if (selectedComboBoxItem != null)
                transactionSelector.SelectedItem = selectedComboBoxItem;
            else if (!showCompleteVoucher && transactionSelector.Items.Count > 1)
                transactionSelector.SelectedIndex = 1;
            else if (transactionSelector.Items.Count > 0)
                transactionSelector.SelectedIndex = 0;
        }

        private IEnumerable<GLTransClient> GetInvoiceFilteredTransactions()
        {
            var selectedInvoiceItem = invoiceSelector.SelectedItem as UnicontaComboBoxEditor;
            if (selectedInvoiceItem?.Tag == null)
                return voucherTransactions.Where(x => x != null);

            var selectedInvoice = selectedInvoiceItem.Tag;
            return voucherTransactions.Where(x => x != null && selectedInvoice.Equals(x.Invoice));
        }

        private async Task ReloadPostingTraceAsync()
        {
            isLoading = true;
            try
            {
                var selectedItem = transactionSelector.SelectedItem as UnicontaComboBoxEditor;
                var selectedTransactions = selectedItem?.Tag as IEnumerable<GLTransClient>;
                if (selectedTransactions == null && selectedItem?.Tag is GLTransClient selectedVoucherTransaction)
                    selectedTransactions = new[] { selectedVoucherTransaction };
                if (selectedTransactions == null)
                    selectedTransactions = new[] { selectedTransaction };

                var results = new List<PostingTraceResult>();

                foreach (var voucherTransaction in selectedTransactions.Where(x => x != null))
                {
                    if (postingTraceContext != null)
                        results.Add(await postingTraceContext.GetResultAsync(voucherTransaction));
                    else
                        results.Add(await new InvoicePostingTraceResolver().TracePostingAsync(voucherTransaction, api));
                }

                traceResults = results;
                visualizer.Results = OrderTraceResults(traceResults).ToArray();
            }
            finally
            {
                isLoading = false;
            }
        }

        private IEnumerable<GLTransClient> GetSelectedTransactions()
        {
            var selectedItem = transactionSelector.SelectedItem as UnicontaComboBoxEditor;
            var selectedTransactions = selectedItem?.Tag as IEnumerable<GLTransClient>;
            if (selectedTransactions != null)
                return selectedTransactions.Where(x => x != null);

            if (selectedItem?.Tag is GLTransClient selectedVoucherTransaction)
                return new[] { selectedVoucherTransaction };

            return selectedTransaction == null ? Enumerable.Empty<GLTransClient>() : new[] { selectedTransaction };
        }

        private IEnumerable<PostingTraceResult> OrderTraceResults(IEnumerable<PostingTraceResult> results)
        {
            var selectedSortItem = transactionSortSelector.SelectedItem as UnicontaComboBoxEditor;
            var sortOrder = selectedSortItem?.Tag is TransactionSortOrder selectedSortOrder
                ? selectedSortOrder
                : TransactionSortOrder.AccountAscending;

            switch (sortOrder)
            {
                case TransactionSortOrder.AccountDescending:
                    return results.OrderByDescending(x => x.GLTrans.Account);
                case TransactionSortOrder.AccountNameAscending:
                    return results.OrderBy(x => x.GLTrans.Name);
                case TransactionSortOrder.AccountNameDescending:
                    return results.OrderByDescending(x => x.GLTrans.Name);
                case TransactionSortOrder.AccountTypeAscending:
                    return results.OrderBy(x => x.GLTrans.AccountType);
                case TransactionSortOrder.AccountTypeDescending:
                    return results.OrderByDescending(x => x.GLTrans.AccountType);
                default:
                    return results.OrderBy(x => x.GLTrans.Account);
            }
        }

        public override List<IAIAssistantCapability> GetAIAssistantCapabilities(bool hasFileAttached)
        {
            var retVal = base.GetAIAssistantCapabilities(hasFileAttached);

            // Add posting source trace capability
            retVal.Add(new PostingOriginAnalysisCapability(
                () => traceResults,
                context => GetGLStructureFileData(context.AssistantConsumer.Api),
                Prompt));

            return retVal;
        }

        public List<string> GetAIAssistantPromptSuggestions() => new List<string>
        {
            Uniconta.ClientTools.Localization.lookup("GLTraceValidateThePostingWithAI")
        };

        private static bool IsSameTransaction(GLTransClient transaction, GLTransClient selectedTransaction)
        {
            return ReferenceEquals(transaction, selectedTransaction) ||
                   (transaction.Voucher == selectedTransaction.Voucher &&
                    transaction.VoucherLine == selectedTransaction.VoucherLine &&
                    transaction.Account == selectedTransaction.Account &&
                    transaction.Date == selectedTransaction.Date &&
                    transaction.Amount == selectedTransaction.Amount);
        }

        private static string FormatTransactionSelectionText(GLTransClient transaction)
        {
            return string.Format("{0} - {1} - {2} - {3}",
                transaction.Account,
                transaction.Name,
                transaction.Text,
                transaction.Amount);
        }

        protected override async void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            if (args is GLTransClient transaction)
                await LoadPostingTraceAsync(transaction, transactionSelector.SelectedIndex == 0);
        }

        private void Ribbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AskAI":
                    _ = AskAI();
                    break;
                case "Prompt":
                    new CWAIPromptEdit(this).Show();
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        internal static async Task<IFileUploadData> GetGLStructureFileData(CrudAPI api)
        {
            if (api == null)
                throw new ArgumentNullException(nameof(api));

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                var accounts = await api.LoadCache<GLAccountClient>();
                foreach (var account in accounts)
                    writer.WriteLine($"{account.Account} Name: {account.Name} Type: {account.AccountType} Blocked: {account.Blocked}");

                writer.Flush();
                return new FileUploadData
                {
                    FileName = "GeneralLedgerStructure.json",
                    ContentType = "text/plain",
                    FileData = stream.ToArray()
                };
            }
        }

        private async Task AskAI()
        {
            if (!GPTServiceFactory.IsTransactionOriginAnalysisEnabled(Api.CompanyEntity))
                return;

            var transactions = GetSelectedTransactions().ToArray();
            if (transactions.Length == 0)
                return;

            var context = new GPTConversationContextWithEntitiesAndAttachment
            {
                UnicontaBaseEntities = transactions,
                File = await GetGLStructureFileData(api)
            };

            Prompt.Replace(new Dictionary<string, string>
            {
                { "CountryId", api.CompanyEntity._CountryId.ToString() }
            }).EnrichContext(context);

            var response = await api.CompanyEntity.GetGPTService(api).GetResponse<PostingValidationHeader>((IGPTConversationContext)context);
            if (response == null)
                return;

            var message = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceValidationExplanation"), response.CertaintyPercentage, Environment.NewLine, response.Explanation);

            if (response.Lines?.Count > 0)
            {
                foreach (var lineMessage in response.Lines)
                {
                    message += string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceValidationExplanation"), lineMessage.CertaintyPercentage, Environment.NewLine, lineMessage.Explanation);

                }
            }

            if (response.IsValid)
                Uniconta.ClientTools.InfoLog.Log.Info(message);
            else
                Uniconta.ClientTools.InfoLog.Log.Warning(message);
        }

        private static PromptDefinition GetPostingValidationPrompt() => PromptProvider.From(() => PostingValidationPrompt);

        [PromptDefinition("GeneralLedger_PostingValidation", "Prompt used to validate ledger postings against the company chart of accounts.")]
        [PromptDefinitionReplacement("CountryId", "The company country identifier used to interpret the chart of accounts and posting.")]
        public static PromptDefinition PostingValidationPrompt =>
            PromptDefinition.FromMessages(new[]
            {
                new PromptRoleNode
                {
                    Role = MessageRole.Developer,
                    Content =
@"You are an ERP accounting validation assistant.

Validate the supplied general ledger posting against the attached {{CountryId}} chart of accounts. Give a verdict over the total voucher as well as an explanation per transcation line.

Be precise and conservative. Consider debit and credit meaning, account type, VAT, currency, balancing, and the relationship between the transaction lines. Do not infer facts that are not present in the posting or chart of accounts. Explanation must be factual and no more than 100 words. CertaintyPercentage must be an integer from 0 to 100."
                },
                new PromptRoleNode
                {
                    Role = MessageRole.User,
                    Content =
@"Check the supplied posting against the attached {{CountryId}} chart of accounts and identify any mismatches."
                }
            }, expectsExplanationInUserLanguage: true);

    }

    /// <summary>
    /// Explains the deterministic posting origin and validates the selected ledger posting
    /// against the attached company chart of accounts.
    /// </summary>
    public sealed class PostingOriginAnalysisCapability : PromptCapabilityBase<PostingValidationHeader>, IAIAssistantCapabilityButton
    {
        private readonly Func<IEnumerable<PostingTraceResult>> resultProvider;
        private readonly Func<IAIAssistantCapabilityContext, Task<IFileUploadData>> chartOfAccountsProvider;
        private readonly PromptDefinition prompt;

        public PostingOriginAnalysisCapability(
            Func<IEnumerable<PostingTraceResult>> resultProvider,
            Func<IAIAssistantCapabilityContext, Task<IFileUploadData>> chartOfAccountsProvider,
            PromptDefinition prompt)
            : base(
                nameof(PostingOriginAnalysisCapability),
                Uniconta.ClientTools.Localization.lookup("GLTraceAnalyzeThis"),
                "Explains why the selected ledger posting used its accounts and validates the posting " +
                "against the company chart of accounts. It distinguishes deterministic transaction-origin " +
                "evidence from accounting-validation findings.",
                weight: 100,
                "Why was this transaction posted to this account?",
                "Explain where these voucher accounts came from",
                "Which setup selected the account?",
                "Is this voucher posted correctly?",
                "Analyse this posting")
        {
            this.resultProvider = resultProvider ?? throw new ArgumentNullException(nameof(resultProvider));
            this.chartOfAccountsProvider = chartOfAccountsProvider ?? throw new ArgumentNullException(nameof(chartOfAccountsProvider));
            this.prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));
        }

        protected override PromptDefinition GetPromptDefinition()
            => prompt;

        protected override PromptDefinition ReplacePromptValues(PromptDefinition prompt, IAIAssistantCapabilityContext capabilityContext)
            => base.ReplacePromptValues(prompt, capabilityContext)
                   .Replace("CountryId", capabilityContext.AssistantConsumer.Api.CompanyEntity._CountryId.ToString());

        protected override async Task<IGPTConversationContext> EnrichContext(IAIAssistantCapabilityContext capabilityContext, IGPTConversationContext context)
        {
            await base.EnrichContext(capabilityContext, context);

            var attachmentContext = new GPTConversationContextWithEntitiesAndAttachment();
            foreach (var message in context.Messages)
                attachmentContext.Messages.Add(message);

            attachmentContext.UnicontaBaseEntities = (resultProvider() ?? Enumerable.Empty<PostingTraceResult>())
                .Where(result => result?.GLTrans != null)
                .Select(result => result.GLTrans)
                .ToArray();
            attachmentContext.File = await chartOfAccountsProvider(capabilityContext);
            return attachmentContext;
        }

        public static string BuildEvidence(IEnumerable<PostingTraceResult> results)
        {
            var availableResults = (results ?? Enumerable.Empty<PostingTraceResult>()).Where(result => result?.GLTrans != null).ToArray();

            if (availableResults.Length == 0)
                return Uniconta.ClientTools.Localization.lookup("GLTraceNoResults");

            var output = new StringBuilder();

            for (var transactionIndex = 0; transactionIndex < availableResults.Length; transactionIndex++)
            {
                var result = availableResults[transactionIndex];
                var transaction = result.GLTrans;

                output.AppendLine($"TRANSACTION {transactionIndex + 1}");
                output.AppendLine($"Account: {transaction.Account}");
                output.AppendLine($"Account name: {transaction.Name}");
                output.AppendLine($"Account type: {transaction.AccountType}");
                output.AppendLine($"Amount: {transaction.Amount}");
                output.AppendLine($"Text: {transaction.Text}");
                output.AppendLine(
                    $"Voucher: {transaction.Voucher}; " +
                    $"line: {transaction.VoucherLine}; " +
                    $"date: {transaction.Date:yyyy-MM-dd}; " +
                    $"invoice: {transaction.Invoice}");

                var steps =
                    (result.CheckedPostingSources ??
                     Enumerable.Empty<PostingTraceStep>())
                    .Where(step => step != null)
                    .ToArray();

                output.AppendLine(
                    $"Origin resolved: {(result.HasPostingSourceMatch ? "yes" : "no")}");

                if (steps.Length == 0)
                {
                    output.AppendLine("Trace steps: none available");
                }
                else
                {
                    for (var stepIndex = 0;
                         stepIndex < steps.Length;
                         stepIndex++)
                    {
                        var step = steps[stepIndex];

                        output.AppendLine(
                            $"Trace step {stepIndex + 1} " +
                            $"({(step.IsMatch ? "MATCHED" : "CHECKED, NOT MATCHED")}):");

                        output.AppendLine(
                            $"  Area: {PostingTraceTextFormatter.Area(step.Area)}");

                        output.AppendLine(
                            $"  Reason: {PostingTraceTextFormatter.Reason(step.Reason)}");

                        output.AppendLine(
                            $"  Confidence: {PostingTraceTextFormatter.Confidence(step.Confidence)}");

                        output.AppendLine(
                            $"  Candidate account: {step.MatchingAccount}; " +
                            $"name: {step.AccountName}; " +
                            $"type: {step.AccountType}");

                        output.AppendLine(
                            $"  Explanation: " +
                            $"{PostingTraceTextFormatter.Explanation(step.Explanation)}");

                        output.AppendLine(
                            $"  Setup source: " +
                            $"{PostingTraceTextFormatter.SetupArea(step.SourceTable?.Name)}; " +
                            $"key: {step.SourceTableKey}; " +
                            $"field: {step.SourceField}");

                        output.AppendLine(
                            $"  Manual source: {(step.IsManual ? "yes" : "no")}");
                    }
                }

                output.AppendLine();
            }

            return output.ToString().TrimEnd();
        }

        protected override Task<AIAssistantCapabilityResult> ExecuteResponse(IAIAssistantCapabilityContext capabilityContext, PostingValidationHeader response)
        {
            if (response == null)
                return Task.FromResult(AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceNoAnalysis")));

            var message = new StringBuilder()
                .AppendLine(Uniconta.ClientTools.Localization.lookup("GLTracePostingValidation"))
                .AppendLine(string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceValidationConfidence"), response.CertaintyPercentage))
                .AppendLine($"- {response.Explanation}");

            if (response.Lines != null)
            {
                foreach (var line in response.Lines)
                {
                    message.AppendLine()
                        .AppendLine(string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceValidationConfidence"), line.CertaintyPercentage))
                        .AppendLine($"- {line.Explanation}");
                }
            }

            return Task.FromResult(new AIAssistantCapabilityResult
            {
                Message = message.ToString().Trim(),
                Succeeded = response.IsValid
            });
        }
    }
}
