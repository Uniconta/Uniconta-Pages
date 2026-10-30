using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.WhereUsed
{
    public sealed class GLVatWhereUsedFinder
    {
        private static readonly SourceDefinition[] Sources =
        {
            new SourceDefinition(typeof(InvGroupClient), "Inventory group VAT", "Group",
                Fields("SalesVat", "Sales VAT"), Fields("PurchaseVat", "Purchase VAT")),
            new SourceDefinition(typeof(DebtorGroupClient), "Debtor group VAT", "Group",
                Fields("SalesVat", "Sales VAT")),
            new SourceDefinition(typeof(CreditorGroupClient), "Creditor group VAT", "Group",
                Fields("PurchaseVat", "Purchase VAT"))
        };

        public async Task<IReadOnlyList<GLAccountWhereUsedResult>> FindWhereUsed(CrudAPI api, GLVatClient vat)
        {
            if (api == null)
                throw new ArgumentNullException(nameof(api));

            return await FindWhereUsed(vat, async type =>
            {
                var cache = api.GetCache(type) ?? await api.LoadCache(type).ConfigureAwait(false);
                return cache?.GetRecords?.Cast<object>();
            }).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<GLAccountWhereUsedResult>> FindWhereUsed(GLVatClient vat, Func<Type, Task<IEnumerable<object>>> loadRecords)
        {
            if (vat == null)
                throw new ArgumentNullException(nameof(vat));
            if (loadRecords == null)
                throw new ArgumentNullException(nameof(loadRecords));
            if (string.IsNullOrWhiteSpace(vat.Vat))
                return new List<GLAccountWhereUsedResult>();

            var results = new List<GLAccountWhereUsedResult>();
            foreach (var source in Sources)
            {
                var records = await loadRecords(source.SourceTable).ConfigureAwait(false);
                if (records == null)
                    continue;

                foreach (var record in records.Where(r => r != null))
                {
                    foreach (var field in source.Fields)
                    {
                        var value = Convert.ToString(GetMemberValue(record, field.Name));
                        if (!string.Equals(value, vat.Vat, StringComparison.OrdinalIgnoreCase))
                            continue;

                        results.Add(new GLAccountWhereUsedResult
                        {
                            Account = vat.Vat,
                            AccountName = vat.Name,
                            SourceTable = source.SourceTable,
                            SourceTableKey = Convert.ToString(GetMemberValue(record, source.KeyMemberName)),
                            SourceField = field.Name,
                            SourceFieldDescription = field.Description,
                            SourceValue = value,
                            Description = string.Format("{0} - {1}", source.Description, field.Description)
                        });
                    }
                }
            }

            return results.OrderBy(r => r.SourceTableName).ThenBy(r => r.SourceTableKey).ThenBy(r => r.SourceField).ToList();
        }

        private static FieldDefinition[] Fields(string prefix, string description)
        {
            return Enumerable.Range(0, 5)
                .Select(index => new FieldDefinition(index == 0 ? prefix : prefix + index,
                    index == 0 ? description + " (domestic)" : string.Format("{0} (VAT zone {1})", description, index)))
                .ToArray();
        }

        private static object GetMemberValue(object record, string memberName)
        {
            var type = record.GetType();
            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(record, null);

            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                     ?? type.GetField("_" + memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field == null ? null : field.GetValue(record);
        }

        private sealed class SourceDefinition
        {
            public SourceDefinition(Type sourceTable, string description, string keyMemberName, params FieldDefinition[][] fields)
            {
                SourceTable = sourceTable;
                Description = description;
                KeyMemberName = keyMemberName;
                Fields = fields.SelectMany(f => f).ToArray();
            }

            public Type SourceTable { get; private set; }
            public string Description { get; private set; }
            public string KeyMemberName { get; private set; }
            public FieldDefinition[] Fields { get; private set; }
        }

        private sealed class FieldDefinition
        {
            public FieldDefinition(string name, string description)
            {
                Name = name;
                Description = description;
            }

            public string Name { get; private set; }
            public string Description { get; private set; }
        }
    }
}
