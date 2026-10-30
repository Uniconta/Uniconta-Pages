using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.TransactionOrigin.Workers
{
    public class MiscChargePostingTraceWorker : InvoicePostingTraceResolverWorker
    {
        public override void AddTraceSteps(IPostingTraceContext context, PostingTraceResult result)
        {
            MiscChargeAccounts(context, result);
        }

        #region 23 Misc charge
        /// <summary>
        /// 23. Misc purchase charges.
        ///
        /// Source:
        /// </summary>
        protected virtual void MiscChargeAccounts(IPostingTraceContext context, PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.OrderCosts?.Count() > 0)
            {
                foreach (var orderCost in context.OrderCosts.Where(o => o?.Account == context.GLTrans.Account))

                {
                    if (!string.IsNullOrEmpty(orderCost.Account))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = orderCost.Account,
                            Reason = PostingAccountReason.MiscChargesAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceMiscChargeAccountMatch"),
                            SourceTable = typeof(CreditorOrderCostClient),
                            SourceTableKey = orderCost.KeyStr,
                            SourceField = nameof(CreditorOrderCostClient.Account),
                        });
                    }
                }
            }

            RecordPossiblePostingSources(
               context,
               result,
               candidates,
               PostingTraceArea.MiscCharges,
               ResolutionConfidence.Medium,
               Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountCreditorMiscCharges"));

        }

        #endregion
    }
}
