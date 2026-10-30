using System;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage.GL.WhereUsed
{
    public sealed class GLAccountWhereUsedResult : UnicontaBaseEntity
    {
        public string Account { get; set; }
        public string AccountName { get; set; }
        public Type SourceTable { get; set; }
        public string SourceTableName => SourceTable == null ? null : SourceTable.Name;
        public string SourceTableKey { get; set; }
        public string SourceField { get; set; }
        public string SourceFieldDescription { get; set; }
        public string SourceValue { get; set; }
        public string Description { get; set; }
        public int CompanyId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Type BaseEntityType() => typeof(GLAccountWhereUsedResult);

        public int ClassId() => -1;

        public void loadFields(CustomReader r, int SavedWithVersion) { }

        public void saveFields(CustomWriter w, int SaveVersion) { }

        public int Version(int ClientAPIVersion) => -1;
    }
}
