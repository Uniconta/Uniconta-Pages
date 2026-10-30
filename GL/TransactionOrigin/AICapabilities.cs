using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.AI.Services.Context;
using Uniconta.ClientTools.Controls.AI;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public sealed class OpenTransactionOriginCapability : PromptCapabilityBase<OpenTransactionOriginCapability.TransactionSelection>
    {
        private readonly IGLPostingSourceTraceConsumer _consumer;

        //private readonly Func<GLTransClient> selectedTransactionProvider;
        //private readonly Func<GLTransClient, Task<GLTransClient[]>> voucherProvider;
        //private readonly Action<GLTransClient> openTransactionOrigin;

        public OpenTransactionOriginCapability(IGLPostingSourceTraceConsumer consumer)  //IGLPostingSourceTraceConsumer
            : base(nameof(OpenTransactionOriginCapability), Uniconta.ClientTools.Localization.lookup("TransactionOrigin"),
                "Traces the selected posting and explains its account origin in financial-controller language, then offers to open the detailed Transaction origin page. When the request names a GL account or amount, it selects that specific voucher line first.",
                100, "Where does this transaction come from?", "Why is 2000 euro posted on 4100?", "Show the origin of account 4100")
        {
            _consumer = consumer;
        }

        protected override PromptDefinition GetPromptDefinition() => PromptProvider.From(() => TransactionSelectionPrompt);

        protected override async Task<AIAssistantCapabilityResult> ExecuteResponse(IAIAssistantCapabilityContext context, TransactionSelection response)
        {
            var selected = _consumer.GetSelectedVoucherTransaction();
            if (selected == null)
                return AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceNoTransactionIsCurrentlySelected"));

            var traceContext = await _consumer.GetPostingTraceContextAsync(selected, context.AssistantConsumer.Api);
            if (traceContext == null)
                return AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceTheSelectedVoucherCouldNotBeLoaded"));

            var voucherLines = traceContext?.Transactions ?? Array.Empty<GLTransClient>();

            var line = response == null ? null : voucherLines.FirstOrDefault(item =>
                (response.VoucherLine != 0 && item.VoucherLine == response.VoucherLine) ||
                (!string.IsNullOrWhiteSpace(response.Account) && item.Account == response.Account &&
                 (!response.Amount.HasValue || Math.Abs(item.Amount - response.Amount.Value) < 0.005)));

            return await ExplainPostingOriginAsync(context, traceContext, line ?? selected);
        }

        private async Task<AIAssistantCapabilityResult> ExplainPostingOriginAsync(IAIAssistantCapabilityContext context, PostingTraceVoucherContext traceContext, GLTransClient transaction)
        {
            var traceResult = await traceContext.GetResultAsync(transaction);
            if (traceResult == null)
                return AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceTheTransactionOriginCouldNotBeTraced"));

            var prompt = PromptProvider.From(() => PostingOriginExplanationPrompt)
                .Replace("PostingOriginEvidence", PostingOriginAnalysisCapability.BuildEvidence(new[] { traceResult }));
            var conversation = context.UserRequest.GetConversationContext(prompt, context.ConversationHistory);
            var explanation = await context.GPTService.GetResponse<PostingOriginExplanation>(conversation);

            if (string.IsNullOrWhiteSpace(explanation?.Explanation))
                return AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceTheTransactionOriginCouldNotBeExplained"));

            return new AIAssistantCapabilityResult
            {
                Succeeded = true,
                Message = explanation.Explanation + "\n\n" + Uniconta.ClientTools.Localization.lookup("GLTraceDoYouWantToOpenTheDetailedTransaction"),
                Actions = new[]
                {
                    new AIAssistantCapabilityResult.CapabilityAction
                    {
                        Caption = Uniconta.ClientTools.Localization.lookup("GLTraceYesOpenTransactionOrigin"),
                        ExecuteAsync = () => OpenPostingSourceTraceAsync(transaction)
                    },
                    new AIAssistantCapabilityResult.CapabilityAction
                    {
                        Caption = Uniconta.ClientTools.Localization.lookup("No"),
                        ExecuteAsync = () => Task.FromResult(AIAssistantCapabilityResult.Success(Uniconta.ClientTools.Localization.lookup("GLTraceTheTransactionOriginPageWasNotOpened")))
                    }
                }
            };
        }

        private Task<AIAssistantCapabilityResult> OpenPostingSourceTraceAsync(GLTransClient transaction)
        {
            _consumer.ShowPostingSourceTrace(transaction);
            return Task.FromResult(AIAssistantCapabilityResult.Success(Uniconta.ClientTools.Localization.lookup("GLTraceTransactionOriginOpened")));
        }

        [PromptDefinition("GeneralLedger_TransactionOriginSelection", "Prompt used to select the voucher line whose transaction origin should be traced and explained.")]
        public static PromptDefinition TransactionSelectionPrompt => NewPromptDefinition(
            "Extract the voucher-line criteria stated by the user." +
            "Do not infer values that the user did not state. If no specific line is mentioned, return empty/default values.",
            "Extract any explicitly stated voucher line, GL account, and amount from the request.");

        [PromptDefinition("GeneralLedger_TransactionOriginExplanation", "Prompt used to explain deterministic transaction-origin results in financial controller-friendly language.")]
        [PromptDefinitionReplacement("PostingOriginEvidence", "The complete result returned by the deterministic posting-origin resolver.")]
        public static PromptDefinition PostingOriginExplanationPrompt => PromptDefinition.FromMessages(new[]
        {
            new PromptRoleNode { Role = MessageRole.System, Content =
                "You are an experienced financial controller. " +
                "Explain in clear, natural business language where the ledger account came from, using only the deterministic trace evidence supplied by the application. " +

                "The explanation must focus exclusively on the posting setup that actually matched. " +
                "The important origin information is: table, key, and origin field. " +

                "Do NOT mention, summarize, describe, or explain trace steps, trace areas, " +
                "search paths, resolver stages, intermediate checks, or diagnostic metadata. " +

                "Do not refer to the internal trace process when describing a confirmed result. " +
                "Never mention trace step numbers or say that a value was confirmed, found, matched, " +
                "or resolved in a trace step or resolver step. " +

                "State the business origin directly using only the matched table/setup, key, " +
                "origin field, and resulting account. " +
                "These fields may exist in the supplied evidence for diagnostic purposes, but they must not appear in the user-facing explanation. " +

                "Clearly distinguish a confirmed matched setup from settings that were merely checked and did not determine the account. " +

                "If the origin is resolved, state which table/setup matched, which key identified the setup, and which field supplied the posting account. " +
                "If the origin is unresolved, say that the source could not be determined and suggest only checks directly supported by the evidence. " +

                "Do not invent posting rules, configuration, user actions, or explanations that are not explicitly supported by the evidence." },
            new PromptRoleNode { Role = MessageRole.User, Content =
                "Explain where this posting account came from:\n\n{{PostingOriginEvidence}}" }
        }, expectsExplanationInUserLanguage: true);

        public sealed class PostingOriginExplanation
        {
            public string Explanation { get; set; }
        }

        public sealed class TransactionSelection
        {
            public int VoucherLine { get; set; }
            public string Account { get; set; }
            public double? Amount { get; set; }
        }
    }

    public sealed class ReviewVoucherCapability : PromptCapabilityBase<ReviewVoucherCapability.VoucherReviewRequest>
    {
        private readonly IGLPostingSourceTraceConsumer _consumer;

        public ReviewVoucherCapability(IGLPostingSourceTraceConsumer consumer)
            : base(nameof(ReviewVoucherCapability), Uniconta.ClientTools.Localization.lookup("GLTraceReviewVoucher"),
                "Fetches the complete selected voucher and asks the general-ledger assistant to check whether it looks correct for the current company's country.",
                100, "Does this voucher look OK?", "Check this voucher", "Is this posting correct?")
        {
            _consumer = consumer;
        }

        protected override PromptDefinition GetPromptDefinition() => PromptProvider.From(() => VoucherReviewRequestPrompt);

        protected override async Task<AIAssistantCapabilityResult> ExecuteResponse(IAIAssistantCapabilityContext context, VoucherReviewRequest response)
        {
            if (response?.Review != true)
                return AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceAVoucherReviewWasNotRequested"));

            var selected = _consumer.GetSelectedVoucherTransaction();
            if (selected == null)
                return AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceNoTransactionIsCurrentlySelected"));

            var traceContext = await _consumer.GetPostingTraceContextAsync(selected, context.AssistantConsumer.Api);
            var voucherLines = traceContext?.Transactions ?? Array.Empty<GLTransClient>();

            if (voucherLines.Length == 0)
                return AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceTheSelectedVoucherCouldNotBeLoaded"));

            var prompt = PromptProvider.From(() => VoucherReviewPrompt)
                .Replace("Country", context.AssistantConsumer.Api.CompanyEntity._CountryId.ToString())
                .Replace("Voucher", JsonConvert.SerializeObject(voucherLines.Select(ToReviewLine)));
            var conversation = context.UserRequest.GetConversationContext(prompt, context.ConversationHistory);

            var review = await context.GPTService.GetResponse<VoucherReview>(conversation);
            return string.IsNullOrWhiteSpace(review?.Review)
                ? AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("GLTraceTheVoucherCouldNotBeReviewed"))
                : AIAssistantCapabilityResult.Success(review.Review);
        }

        [PromptDefinition("GeneralLedger_VoucherReviewRequest", "Prompt used to confirm that the selected voucher should be reviewed.")]
        public static PromptDefinition VoucherReviewRequestPrompt => NewPromptDefinition(
            "Return only JSON compatible with VoucherReviewRequest: {\"review\":true}. Set review to true when the user asks to check or review the voucher.",
            "Determine whether the user is asking for a voucher review.");

        private static object ToReviewLine(GLTransClient line) => new
        {
            line.Date,
            line.Voucher,
            line.VoucherLine,
            line.Account,
            line.AccountType,
            line.Amount,
            line.AmountCur,
            line.Currency,
            line.Vat,
            line.VatOperation,
            line.Text,
            line.TransType,
            line.Dimension1,
            line.Dimension2,
            line.Dimension3,
            line.Dimension4,
            line.Dimension5,
            line.DCAccount,
            line.Project
        };

        [PromptDefinition("GeneralLedger_VoucherReview", "Prompt used by the general-ledger assistant to review a complete voucher.")]
        [PromptDefinitionReplacement("Country", "The country identifier of the current company.")]
        [PromptDefinitionReplacement("Voucher", "The complete voucher loaded from the database or supplied simulation.")]
        public static PromptDefinition VoucherReviewPrompt => PromptDefinition.FromMessages(new[]
        {
            new PromptRoleNode { Role = MessageRole.System, Content =
                "You are a general-ledger assistant. Check the supplied voucher for accounting plausibility in the company's country {{Country}}. " +
                "Check balance, account choices, VAT, currency, dates, dimensions, and suspicious or missing information. " +
                "Do not claim certainty or invent facts; clearly distinguish errors from items that require supporting-document review. " +
                "Return JSON compatible with VoucherReview: {\"review\":\"concise findings and recommendation\"}." },
            new PromptRoleNode { Role = MessageRole.User, Content = "Review this complete voucher:\n{{Voucher}}" }
        }, expectsExplanationInUserLanguage: true);

        public sealed class VoucherReview
        {
            public string Review { get; set; }
        }

        public sealed class VoucherReviewRequest
        {
            public bool Review { get; set; }
        }
    }
}
