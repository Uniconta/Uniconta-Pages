using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin
{
    public class InvoicePostingTraceResolver
    {
        private readonly IReadOnlyList<IInvoicePostingTraceResolverWorker> _workers;

        public InvoicePostingTraceResolver(IEnumerable<IInvoicePostingTraceResolverWorker> workers = null)
        {
            _workers = (workers ?? CreateDefaultWorkers()).ToList();
        }

        public async Task<PostingTraceResult> TracePostingAsync(GLTransClient glTrans, CrudAPI api)
        {
            if (glTrans == null)
                throw new ArgumentNullException(nameof(glTrans));

            return await ResolvePostingAsync(glTrans, await BuildContext(glTrans, api));
        }

        /// <summary>
        /// Traces a posting using the lines which were used to create it, when they are
        /// available. This is required for simulations: a simulated posting has no
        /// persisted voucher or invoice from which the lines can be loaded.
        /// </summary>
        public static async Task<PostingTraceResult> TracePostingAsync(GLTransClient glTrans, CrudAPI api, DCOrder postedOrder, IEnumerable<DCOrderLine> postedOrderLines)
        {
            if (glTrans == null)
                throw new ArgumentNullException(nameof(glTrans));

            var resolver = new InvoicePostingTraceResolver();
            var context = await resolver.BuildContext(glTrans, api, postedOrder, postedOrderLines);

            return await resolver.ResolvePostingAsync(glTrans, context);
        }

        private async Task<PostingTraceResult> ResolvePostingAsync(GLTransClient glTrans, PostingTraceContext context)
        {
            var result = new PostingTraceResult(glTrans);

            foreach (var worker in _workers)
            {
                worker.AddTraceSteps(context, result);
            }

            if (!result.HasPostingSourceMatch)
            {// fall back 'match'
                result.Add(new PostingTraceStep
                {
                    IsMatch = true,
                    Area = PostingTraceArea.Unknown,
                    Reason = PostingAccountReason.Unknown,
                    Confidence = ResolutionConfidence.Low,
                    MatchingAccount = glTrans.Account,
                    AccountName = context.GetFinancialAccountName(glTrans.Account),
                    AccountType = context.GetFinancialAccountType(glTrans.Account),
                    Explanation = Uniconta.ClientTools.Localization.lookup("GLTracePostingFoundButNoneCurrently")
                });
            }

            return result;
        }

        protected virtual Task<PostingTraceContext> BuildContext(GLTransClient glTrans, CrudAPI api) => BuildContext(glTrans, api, null, null);

        /// <summary>
        /// Builds a trace context from either persisted invoice data or the transient
        /// order and line snapshot supplied by a simulation caller.
        /// </summary>
        protected virtual async Task<PostingTraceContext> BuildContext(GLTransClient glTrans, CrudAPI api, DCOrder postedOrder, IEnumerable<DCOrderLine> postedOrderLines)
        {
            if (glTrans == null)
                throw new ArgumentNullException(nameof(glTrans));

            if (api == null)
                throw new ArgumentNullException(nameof(api));

            PostingTraceContext context;

            if (postedOrder != null)
            {
                if (postedOrderLines == null)
                {
                    if (postedOrder is DebtorOrder debtorOrder)
                    {
                        postedOrderLines = await api.Query<DebtorOrderLineClient>(debtorOrder);
                    }
                    else
                    if (postedOrder is CreditorOrder creditorOrder)
                    {
                        postedOrderLines = await api.Query<CreditorOrderLineClient>(creditorOrder);
                    }
                }

                context = new PostedOrderPostingTraceContext(postedOrder, postedOrderLines);
            }
            else
            {
                context = new PostingTraceContext
                {
                    CompanyId = glTrans.CompanyId
                };
            }

            context.GLTrans = glTrans;
            context.PostedAccount = glTrans.Account;
            context.InvoiceNumber = glTrans.Invoice.ToString();
            context.Voucher = glTrans.Voucher;
            context.Origin = glTrans.Origin;
            context.Amount = glTrans.Amount;
            context.VatAmount = glTrans.AmountVat;
            context.Date = glTrans.Date;

            await context.Initialize(api);

            return context;
        }

        private static IEnumerable<IInvoicePostingTraceResolverWorker> CreateDefaultWorkers()
        {
            yield return new RevenueAndExpensePostingTraceWorker();
            yield return new InventoryAndCostPostingTraceWorker();
            yield return new VatPostingTraceWorker();
            yield return new DiscountChargeAndPaymentPostingTraceWorker();
        }
    }
}
