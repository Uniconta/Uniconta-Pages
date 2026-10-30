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
    public class VatPostingTraceWorker : InvoicePostingTraceResolverWorker
    {
        public override void AddTraceSteps(IPostingTraceContext context, PostingTraceResult result)
        {
            ResolveVatPaths(context, result);
        }

        #region 10-14 VAT accounts

        protected virtual void ResolveVatPaths(IPostingTraceContext context, PostingTraceResult result)
        {
            DetectManuallySelectedVatCodes(context, result);
            FindVatAccountFollowingMainPosting(context, result);
            FindUnrealizedVatAccount(context, result);
            FindStandardVatAndRate2VatAccounts(context, result);
            FindGroupVATSetupCandidates(context, result);
            FindVatOffsetAccounts(context, result);
            FindVatRoundingAccounts(context, result);
        }

        protected virtual void DetectManuallySelectedVatCodes(IPostingTraceContext context, PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            foreach (var vat in (context.VatPostingsForInvoice ?? Enumerable.Empty<VatPostingWrapper>())
                .Where(v => v != null && v.IsManuallySelected))
            {
                result.Add(new PostingTraceStep
                {
                    Area = PostingTraceArea.StandardVatAccountAndRate2VatAccount,
                    Reason = PostingAccountReason.StandardVatAccount,
                    Confidence = ResolutionConfidence.High,
                    IsManual = true,
                    Explanation = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceManualVatCode"), vat.VatCode, vat.SourceOfVAT),
                    SourceTable = vat.SourceTable,
                    SourceTableKey = vat.SourceTableKey,
                    SourceField = "Vat"
                });
            }
        }

        /// <summary>
        /// 10. VAT account follows posting account.
        ///
        /// Source:
        /// - Original booking account, jour.Account
        /// </summary>
        /// <summary>
        /// 10. VAT account follows posting account.
        ///
        /// Source:
        /// - Original booking account, jour.Account
        /// </summary>
        protected virtual void FindVatAccountFollowingMainPosting(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.VatPostingsForInvoice != null)
            {
                foreach (var vat in context.VatPostingsForInvoice.Where(v => v != null && v.FollowAccount))
                {
                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = context.PostedAccount,
                        Reason = PostingAccountReason.VatFollowsOriginalPostingAccount,
                        Confidence = ResolutionConfidence.Low,
                        Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceVATSetupFollowsOriginalPosting"),
                        SourceTable = vat.SourceTable,
                        SourceTableKey = vat.SourceTableKey,
                        SourceField = "FollowAccount"
                    });
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.VatAccountFollowsPostingAccount,
                ResolutionConfidence.Low,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountOriginalBookingAccount"));
        }

        /// <summary>
        /// 11. Unrealized VAT account.
        ///
        /// Source:
        /// - GLVat.UnrealizedAccount
        /// </summary>
        protected virtual void FindUnrealizedVatAccount(
            IPostingTraceContext context,
            PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.VatPostingsForInvoice != null)
            {
                foreach (var vat in context.VatPostingsForInvoice.Where(v => v != null))
                {
                    if (string.IsNullOrEmpty(vat.UnrealizedAccount))
                        continue;

                    candidates.Add(new PostingAccountCandidate
                    {
                        Account = vat.UnrealizedAccount,
                        Reason = PostingAccountReason.UnrealizedVatAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceVATUnrealizedAccountExplainPosted"),
                        SourceTable = vat.SourceTable,
                        SourceTableKey = vat.SourceTableKey,
                        SourceField = "UnrealizedAccount"
                    });
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.UnrealizedVatAccount,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountUnrealizedVATSetup"));
        }

        /// <summary>
        /// 12. Standard VAT account and rate-2 VAT account.
        ///
        /// Source:
        /// - GLVat.Account
        /// - GLVat.AccountRate2
        /// - or original booking account when GLVat._FollowAccount2 is true
        /// </summary>
        protected virtual void FindStandardVatAndRate2VatAccounts(IPostingTraceContext context, PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.VatPostingsForInvoice != null)
            {
                foreach (var vat in context.VatPostingsForInvoice.Where(v => v != null))
                {
                    if (!string.IsNullOrEmpty(vat.Account))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = vat.Account,
                            IsManual = vat.IsManuallySelected,
                            Reason = PostingAccountReason.StandardVatAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = string.Format(Uniconta.ClientTools.Localization.lookup("GLTraceVatCodeAccountMatch"), vat.VatCode, vat.SourceOfVAT, ManualSelectionExplanation(vat)),
                            SourceTable = vat.SourceTable,
                            SourceTableKey = vat.SourceTableKey,
                            SourceField = nameof(GLVatClient.Account)
                        });
                    }

                    if (!string.IsNullOrEmpty(vat.AccountRate2))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = vat.AccountRate2,
                            Reason = PostingAccountReason.StandardVatRate2Account,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceVATRate2AccountExplain"),
                            SourceTable = vat.SourceTable,
                            SourceTableKey = vat.SourceTableKey,
                            SourceField = nameof(GLVatClient.AccountRate2),
                        });
                    }

                    if (vat.FollowAccount2)
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = context.PostedAccount,
                            Reason = PostingAccountReason.VatRate2FollowsOriginalPostingAccount,
                            Confidence = ResolutionConfidence.Low,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceVATRate2SetupFollows"),
                            SourceTable = vat.SourceTable,
                            SourceTableKey = vat.SourceTableKey,
                            SourceField = "FollowAccount2",
                        });
                    }
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.StandardVatAccountAndRate2VatAccount,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountStandardVATRate"));
        }

        /// <summary>
        /// 12.1 Vat on Item/dc groups.
        /// </summary>
        private void FindGroupVATSetupCandidates(IPostingTraceContext context, PostingTraceResult result)
        {
            var candidates = new List<PostingAccountCandidate>();

            var groups = context.InvGroupsForPosting?.Where(group => group != null).ToList();

            if (groups == null)
                return;

            AddVatCodeCandidates(context, groups.Where(group => !group.IsOnItemLevelOnly), candidates, itemLevelOnly: false);

            if (candidates.Count == 0)
            {
                AddVatCodeCandidates(context, groups.Where(group => group.IsOnItemLevelOnly), candidates, itemLevelOnly: true);
            }

            RecordPossiblePostingSources(context,
                                         result,
                                         candidates,
                                         PostingTraceArea.VatOnGroupSetup,
                                         ResolutionConfidence.Medium,
                                         Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountItemDCGroup"));
        }

        private static string ManualSelectionExplanation(VatPostingWrapper vat) => vat.IsManuallySelected ? Uniconta.ClientTools.Localization.lookup("GLTraceManualVatSuffix") : string.Empty;

        /// <summary>
        /// 13. VAT offset account.
        ///
        /// Source:
        /// - GLVat.OffsetAccount
        /// - GLVat.OffsetAccountRate2
        /// </summary>
        protected virtual void FindVatOffsetAccounts(IPostingTraceContext context, PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            var candidates = new List<PostingAccountCandidate>();

            if (context.VatPostingsForInvoice != null)
            {
                foreach (var vat in context.VatPostingsForInvoice.Where(v => v != null))
                {
                    if (!string.IsNullOrEmpty(vat.OffsetAccount))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = vat.OffsetAccount,
                            Reason = PostingAccountReason.VatOffsetAccount,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceVATOffsetAccountExplainPosted"),
                            SourceTable = vat.SourceTable,
                            SourceTableKey = vat.SourceTableKey,
                            SourceField = "OffsetAccount"
                        });
                    }

                    if (!string.IsNullOrEmpty(vat.OffsetAccountRate2))
                    {
                        candidates.Add(new PostingAccountCandidate
                        {
                            Account = vat.OffsetAccountRate2,
                            Reason = PostingAccountReason.VatOffsetRate2Account,
                            Confidence = ResolutionConfidence.Medium,
                            Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceVATOffsetRate2Account"),
                            SourceTable = vat.SourceTable,
                            SourceTableKey = vat.SourceTableKey,
                            SourceField = "OffsetAccountRate2"
                        });
                    }
                }
            }

            RecordPossiblePostingSources(
                context,
                result,
                candidates,
                PostingTraceArea.VatOffsetAccount,
                ResolutionConfidence.Medium,
                Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountVATOffsetSetup"));
        }

        /// <summary>
        /// 14. VAT rounding account.
        ///
        /// Source:
        /// - VAT account correction
        /// - system VatRounding
        /// - system PennyDif
        /// - VAT account fallback
        /// </summary>
        protected virtual void FindVatRoundingAccounts(IPostingTraceContext context, PostingTraceResult result)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (!string.IsNullOrEmpty(context.SystemVatRoundingAccount))
            {
                RecordPossiblePostingSource(
                    context,
                    result,
                    new PostingAccountCandidate
                    {
                        Account = context.SystemVatRoundingAccount,
                        Reason = PostingAccountReason.VatRoundingSystemVatRoundingAccount,
                        Confidence = ResolutionConfidence.Medium,
                        Explanation = Uniconta.ClientTools.Localization.lookup("GLTraceSystemVATRoundingAccountExplain"),
                        SourceTable = typeof(GLAccountClient),
                        SourceTableKey = context.SystemVatRoundingAccount,
                        SourceField = "VatRounding"
                    },
                    PostingTraceArea.VatRoundingAccount,
                    PostingAccountReason.VatRoundingSystemVatRoundingAccount,
                    ResolutionConfidence.Medium,
                    Uniconta.ClientTools.Localization.lookup("GLTracePostedAccountVATRounding2"));
            }
        }



        #endregion


        private static void AddVatCodeCandidates(IPostingTraceContext context, IEnumerable<GroupPostingWrapper> groups, ICollection<PostingAccountCandidate> candidates, bool itemLevelOnly)
        {
            var vatPostings = context.VatPostingsForInvoice ?? Enumerable.Empty<VatPostingWrapper>();

            foreach (var group in groups)
            {
                var vatCode = group.GetVatCode();

                if (string.IsNullOrWhiteSpace(vatCode))
                    continue;

                var vat = context.AllVatCodes?.FirstOrDefault(v => string.Equals(v?.Vat, vatCode, StringComparison.OrdinalIgnoreCase));

                if (vat == null)
                    continue;

                if (string.IsNullOrWhiteSpace(vat.FollowAccount ? context.PostedAccount : vat.Account))
                    continue;

                candidates.Add(new PostingAccountCandidate
                {
                    Account = vat.FollowAccount ? context.PostedAccount : vat.Account,
                    Reason = itemLevelOnly ? PostingAccountReason.ItemGroupVatCode : PostingAccountReason.DCGroupPostingVatCode,
                    Confidence = ResolutionConfidence.Medium,
                    Explanation = itemLevelOnly ? Uniconta.ClientTools.Localization.lookup("GLTraceVATCodeResolvedOriginalItem") : Uniconta.ClientTools.Localization.lookup("GLTraceVATCodeResolvedApplicableDebtor"),
                    SourceTable = group.SourceTableType,
                    SourceTableKey = group.SourceTableKey,
                    SourceField = group.GetVatCodeFieldName()
                });
            }
        }
    }
}
