using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.WhereUsed
{
    public sealed class GLAccountWhereUsedFinder
    {
        private static GLAccountSetupDefinition[] CreateSetupDefinitions(Func<Type, Task<IEnumerable<object>>> loadRecords)
        {
            return new[]
            {
                CreateCacheSetupDefinition<GLVatClient>(loadRecords, "VAT posting", "Vat",
                    Field(nameof(GLVatClient.Account), "VAT account"),
                    Field(nameof(GLVatClient.AccountRate2), "VAT rate 2 account"),
                    Field(nameof(GLVatClient.OffsetAccount), "VAT offset account"),
                    Field(nameof(GLVatClient.OffsetAccountRate2), "VAT rate 2 offset account"),
                    Field(nameof(GLVatClient.UnrealizedAccount), "Unrealized VAT account")),

                CreateCacheSetupDefinition<InvGroupClient>(loadRecords, "Inventory group posting", "Group",
                    Field(nameof(InvGroupClient.PurchaseAccount), "Domestic purchase account"),
                    Field(nameof(InvGroupClient.PurchaseAccount1), "EU purchase account"),
                    Field(nameof(InvGroupClient.PurchaseAccount2), "Foreign purchase account"),
                    Field(nameof(InvGroupClient.PurchaseAccount3), "No VAT registration purchase account"),
                    Field(nameof(InvGroupClient.PurchaseAccount4), "VAT-free purchase account"),
                    Field(nameof(InvGroupClient.RevenueAccount), "Domestic sales revenue account"),
                    Field(nameof(InvGroupClient.RevenueAccount1), "EU sales revenue account"),
                    Field(nameof(InvGroupClient.RevenueAccount2), "Foreign sales revenue account"),
                    Field(nameof(InvGroupClient.RevenueAccount3), "No VAT registration sales revenue account"),
                    Field(nameof(InvGroupClient.RevenueAccount4), "VAT-free sales revenue account"),
                    Field(nameof(InvGroupClient.CostAccount), "Cost of goods sold account"),
                    Field(nameof(InvGroupClient.InvAccount), "Inventory account"),
                    Field(nameof(InvGroupClient.LossProfit), "Scrap loss/profit account"),
                    Field(nameof(InvGroupClient.ProfitLossPurchase), "Fixed-cost purchase profit/loss account")),

                CreateCacheSetupDefinition<DCGroupPostingClient>(loadRecords, "Account group/item group posting profile", "KeyStr",
                    Field(nameof(DCGroupPostingClient.RevenueAccount), "Domestic revenue or purchase account"),
                    Field(nameof(DCGroupPostingClient.RevenueAccount1), "EU revenue or purchase account"),
                    Field(nameof(DCGroupPostingClient.RevenueAccount2), "Foreign revenue or purchase account"),
                    Field(nameof(DCGroupPostingClient.RevenueAccount3), "No VAT registration revenue or purchase account"),
                    Field(nameof(DCGroupPostingClient.RevenueAccount4), "VAT-free revenue or purchase account"),
                    Field(nameof(DCGroupPostingClient.CostAccount), "Domestic cost of goods sold account"),
                    Field(nameof(DCGroupPostingClient.CostAccount1), "EU cost of goods sold account"),
                    Field(nameof(DCGroupPostingClient.CostAccount2), "Foreign cost of goods sold account"),
                    Field(nameof(DCGroupPostingClient.CostAccount3), "No VAT registration cost of goods sold account"),
                    Field(nameof(DCGroupPostingClient.CostAccount4), "VAT-free cost of goods sold account")),

                CreateCacheSetupDefinition<DebtorGroupClient>(loadRecords, "Debtor group posting", "Group",
                    Field(nameof(DebtorGroupClient.RevenueAccount), "Domestic sales revenue account"),
                    Field(nameof(DebtorGroupClient.RevenueAccount1), "EU sales revenue account"),
                    Field(nameof(DebtorGroupClient.RevenueAccount2), "Foreign sales revenue account"),
                    Field(nameof(DebtorGroupClient.RevenueAccount3), "No VAT registration sales revenue account"),
                    Field(nameof(DebtorGroupClient.RevenueAccount4), "VAT-free sales revenue account"),
                    Field(nameof(DebtorGroupClient.DiscountAccount), "Line discount account"),
                    Field(nameof(DebtorGroupClient.DiscountAccountOffset), "Line discount offset account"),
                    Field(nameof(DebtorGroupClient.EndDiscountAccount), "End-discount account"),
                    Field(nameof(DebtorGroupClient.SummeryAccount), "Debtor summary/control account")),

                CreateCacheSetupDefinition<CreditorGroupClient>(loadRecords, "Creditor group posting", "Group",
                    Field(nameof(CreditorGroupClient.DiscountAccount), "Line discount account"),
                    Field(nameof(CreditorGroupClient.DiscountAccountOffset), "Line discount offset account"),
                    Field(nameof(CreditorGroupClient.EndDiscountAccount), "End-discount account"),
                    Field(nameof(CreditorGroupClient.SummeryAccount), "Creditor summary/control account")),

                CreateCacheSetupDefinition<CreditorOrderCostClient>(loadRecords, "Purchase charge", "KeyStr",
                    Field(nameof(CreditorOrderCostClient.Account), "Purchase charge account")),

                CreateCacheSetupDefinition<InvDutyGroupLineClient>(loadRecords, "Duty posting", "DutyGroup",
                    Field(nameof(InvDutyGroupLineClient.Account), "Duty account"),
                    Field(nameof(InvDutyGroupLineClient.OffsetAccount), "Duty offset account")),

                CreateCacheSetupDefinition<ProjectGroupClient>(loadRecords, "Project group posting", "KeyStr",
                    Field(nameof(ProjectGroupClient.WorkInProgress), "Work in progress account"),
                    Field(nameof(ProjectGroupClient.WorkInProgressOffset), "Work in progress offset account"),
                    Field(nameof(ProjectGroupClient.Materials), "Materials account"),
                    Field(nameof(ProjectGroupClient.MaterialsOffset), "Materials offset account"),
                    Field(nameof(ProjectGroupClient.Outlay), "Outlay account"),
                    Field(nameof(ProjectGroupClient.OutlayOffset), "Outlay offset account"),
                    Field(nameof(ProjectGroupClient.Adjustment), "Adjustment account"),
                    Field(nameof(ProjectGroupClient.AdjustmentOffset), "Adjustment offset account"),
                    Field(nameof(ProjectGroupClient.ReversedWIP), "Reversed WIP account"),
                    Field(nameof(ProjectGroupClient.ReversedWIPOffset), "Reversed WIP offset account"),
                    Field(nameof(ProjectGroupClient.InvoiceAdjustment), "Invoice adjustment account"),
                    Field(nameof(ProjectGroupClient.InvoiceAdjustmentOffset), "Invoice adjustment offset account")),

                CreateCacheSetupDefinition<PaymentTermClient>(loadRecords, "Payment term posting", "KeyStr",
                    Field(nameof(PaymentTermClient.Account), "Cash or prepayment account"),
                    Field(nameof(PaymentTermClient.OffsetAccount), "Prepayment offset account"))
            };
        }

        private static GLAccountSetupDefinition CreateCacheSetupDefinition<T>(Func<Type, Task<IEnumerable<object>>> loadRecords, string description, string keyMemberName, params GLAccountSetupFieldDefinition[] accountFields)
        {
            return new GLAccountSetupDefinition(
                typeof(T),
                description,
                keyMemberName,
                async () =>
                {
                    return await loadRecords(typeof(T)).ConfigureAwait(false);
                },
                accountFields);
        }

        private static GLAccountSetupFieldDefinition Field(string fieldName, string description)
        {
            return new GLAccountSetupFieldDefinition(fieldName, description);
        }

        public async Task<IReadOnlyList<GLAccountWhereUsedResult>> FindWhereUsed(CrudAPI api, GLAccountClient account)
        {
            if (api == null)
                throw new ArgumentNullException(nameof(api));

            if (account == null)
                throw new ArgumentNullException(nameof(account));

            return await FindWhereUsed(account, async type =>
            {
                var cache = api.GetCache(type) ?? await api.LoadCache(type).ConfigureAwait(false);
                return cache?.GetRecords?.Cast<object>();
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds setup references using caller-supplied records. This allows consumers to reuse
        /// already-loaded data and lets tests execute the same resolver without a live API.
        /// </summary>
        public async Task<IReadOnlyList<GLAccountWhereUsedResult>> FindWhereUsed(GLAccountClient account, Func<Type, Task<IEnumerable<object>>> loadRecords)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));
            if (loadRecords == null)
                throw new ArgumentNullException(nameof(loadRecords));

            var accountNumber = account.Account;
            if (string.IsNullOrWhiteSpace(accountNumber))
                return new List<GLAccountWhereUsedResult>();

            var results = new List<GLAccountWhereUsedResult>();
            await AddSetupFieldMatches(loadRecords, account, accountNumber, results).ConfigureAwait(false);
            AddSystemAccountMatches(account, accountNumber, results);

            return results
                .OrderBy(r => r.SourceTableName)
                .ThenBy(r => r.SourceTableKey)
                .ThenBy(r => r.SourceField)
                .ToList();
        }

        private static async Task AddSetupFieldMatches(Func<Type, Task<IEnumerable<object>>> loadRecords, GLAccountClient account, string accountNumber, List<GLAccountWhereUsedResult> results)
        {
            foreach (var definition in CreateSetupDefinitions(loadRecords))
            {
                var records = await definition.LoadRecords().ConfigureAwait(false);
                if (records == null)
                    continue;

                foreach (var record in records)
                {
                    if (record == null)
                        continue;

                    foreach (var field in definition.AccountFields)
                    {
                        var value = Convert.ToString(GetMemberValue(record, field.FieldName));
                        if (!string.Equals(value, accountNumber, StringComparison.OrdinalIgnoreCase))
                            continue;

                        results.Add(new GLAccountWhereUsedResult
                        {
                            Account = accountNumber,
                            AccountName = account.Name,
                            SourceTable = definition.SourceTable,
                            SourceTableKey = Convert.ToString(GetMemberValue(record, definition.KeyMemberName)) ?? Convert.ToString(GetMemberValue(record, "KeyStr")),
                            SourceField = field.FieldName,
                            SourceFieldDescription = field.Description,
                            SourceValue = value,
                            Description = string.Format("{0} - {1}", definition.Description, field.Description)
                        });
                    }
                }
            }
        }

        private static void AddSystemAccountMatches(GLAccountClient account, string accountNumber, List<GLAccountWhereUsedResult> results)
        {
            if (Convert.ToInt32(account.SystemAccountEnum) == 0)
                return;

            results.Add(new GLAccountWhereUsedResult
            {
                Account = accountNumber,
                AccountName = account.Name,
                SourceTable = typeof(GLAccountClient),
                SourceTableKey = accountNumber,
                SourceField = Convert.ToString(account.SystemAccountEnum),
                SourceFieldDescription = "System account flag",
                SourceValue = accountNumber,
                Description = string.Format("System account - {0}", account.SystemAccountEnum)
            });
        }

        private static object GetMemberValue(object record, string memberName)
        {
            if (record == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            var type = record.GetType();
            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(record, null);

            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? type.GetField("_" + memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field == null ? null : field.GetValue(record);
        }


        private sealed class GLAccountSetupFieldDefinition
        {
            public GLAccountSetupFieldDefinition(string fieldName, string description)
            {
                FieldName = fieldName;
                Description = description;
            }

            public string FieldName { get; private set; }
            public string Description { get; private set; }
        }

        private sealed class GLAccountSetupDefinition
        {
            public GLAccountSetupDefinition(Type sourceTable, string description, string keyMemberName, Func<Task<IEnumerable<object>>> loadRecords, params GLAccountSetupFieldDefinition[] accountFields)
            {
                SourceTable = sourceTable;
                Description = description;
                KeyMemberName = keyMemberName;
                LoadRecords = loadRecords;
                AccountFields = accountFields;
            }

            public Type SourceTable { get; private set; }
            public string Description { get; private set; }
            public string KeyMemberName { get; private set; }
            public Func<Task<IEnumerable<object>>> LoadRecords { get; private set; }
            public GLAccountSetupFieldDefinition[] AccountFields { get; private set; }
        }
    }
}
