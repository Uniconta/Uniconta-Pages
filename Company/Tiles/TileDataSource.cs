using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{




    public abstract class TileDataSource : ITileDataSource
    {
        protected TileDataSource(CrudAPI api, TileDefinition definition)
        {
            Api = api;
            Definition = definition;
        }

        protected CrudAPI Api { get; private set; }
        protected TileDefinition Definition { get; private set; }
        public abstract Type DataType { get; }
        public abstract string DisplayName { get; }
        public abstract Task<TileDataSourceResult> FetchAsync();

        internal static ITileDataSource Create(CrudAPI api, TileDefinition definition)
        {
            if (!string.IsNullOrWhiteSpace(definition.SourceName))
            {
                var type = Uniconta.ClientTools.Plugin.DisableAllPlugins ? null : Uniconta.ClientTools.Plugin.TileDataSourceCache.Resolve(definition.SourceName);
                if (type == null) throw new InvalidOperationException("The configured tile data source is unavailable: " + definition.SourceName);
                var constructor = type.GetConstructor(new[] { typeof(CrudAPI), typeof(TileDefinition) });
                if (constructor != null) return (ITileDataSource)constructor.Invoke(new object[] { api, definition });
                constructor = type.GetConstructor(new[] { typeof(CrudAPI) });
                if (constructor != null) return (ITileDataSource)constructor.Invoke(new object[] { api });
                constructor = type.GetConstructor(Type.EmptyTypes);
                if (constructor != null) return (ITileDataSource)constructor.Invoke(new object[0]);
                throw new InvalidOperationException("The tile data source has no supported public constructor.");
            }

            var tableType = UtilDisplay.GetTypeFromTableId(api.CompanyEntity, definition.TableId);
            if (tableType == null || !typeof(UnicontaBaseEntity).IsAssignableFrom(tableType))
                throw new InvalidOperationException("The configured table is no longer available.");

            var sourceType = typeof(TileDataSourceTable<>).MakeGenericType(tableType);
            return (TileDataSource)Activator.CreateInstance(sourceType, api, definition);
        }
    }

    internal static class TileTypeFactory
    {

        internal static Type Validate(TileDefinition definition)
        {
            var type = Uniconta.ClientTools.Plugin.DisableAllPlugins ? null : Uniconta.ClientTools.Plugin.TileTypeCache.Resolve(definition.TileTypeId);
            
            if (type == null) throw new ArgumentException("The configured tile type is unavailable: " + definition.TileTypeId);
            
            var sourceType = Uniconta.ClientTools.Plugin.TileDataSourceCache.Resolve(definition.SourceName);

            if (!string.IsNullOrWhiteSpace(definition.SourceName) 
            &&  sourceType == null)
                throw new ArgumentException("The configured tile data source is unavailable: " + definition.SourceName);

            return type;
        }

        internal static ITileType Create(CrudAPI api, TileDefinition definition)
        {
            var tile = (ITileType)Activator.CreateInstance(Validate(definition));
            tile.Initialize(api);
            (tile as ITileTypeWithParms)?.Deserialize(definition.TileTypeParameters);
            return tile;
        }
    }

    public abstract class TileDataSourceTableBase<T> : TileDataSource where T : class, UnicontaBaseEntity
    {
        protected TileDataSourceTableBase(CrudAPI api, TileDefinition definition) : base(api, definition) { }

        public override Type DataType { get { return typeof(T); } }
        public override string DisplayName { get { return TileControl.GetTypeDisplayName(typeof(T), Api.CompanyEntity); } }

        protected virtual T CreateTable()
        {
            return (T)Activator.CreateInstance(typeof(T));
        }

        public override async Task<TileDataSourceResult> FetchAsync()
        {
            var table = CreateTable();
            table.CompanyId = Api.CompanyEntity.CompanyId;

            TileTableCapabilities.Validate(typeof(T), Definition);

            var filter = GetFilter(Definition.Filter);
            var isCount = Definition.Visualization == TileVisualization.Number &&
                          Definition.Aggregate == TileAggregate.Count;

            var userTable = table as TableDataWithKey;
            var cache = userTable == null ? Api.GetCache(typeof(T)) : Api.CompanyEntity.GetUserTableCache(userTable.TableNo);

            // Count can use the existing cache if available, otherwise let SQL do the counting without loading the whole table.
            if (isCount && cache == null)
                return new TileDataSourceResult(await Api.Count(table, null, filter), null);

            if (TileTableCapabilities.SupportsLocalAggregation(typeof(T)))
            {
                cache = cache ?? (userTable == null ? await Api.LoadCache(typeof(T)) : await Api.CompanyEntity.LoadUserTableCache(typeof(T), Api));

                if (cache == null)
                {
                    if (Definition.Visualization == TileVisualization.RecordText)
                        throw new InvalidOperationException("The configured table cache could not be loaded.");
                    Uniconta.ClientTools.InfoLog.Log.Error("The configured table cache could not be loaded.");
                    return null;
                }

                // Work on a snapshot; predicates and aggregates must never change the shared cache.
                var cachedRows = (cache.GetNotNullArray ?? new IdKey[0])
                    .Take(cache.Count)
                    .Where(row => row != null)
                    .Cast<UnicontaBaseEntity>()
                    .ToArray();

                // User-table caches mix generated and plugin types; use the selected field schema without changing cached records.
                if (userTable != null)
                    cachedRows = cachedRows.Select(row =>
                    {
                        if (row.GetType() == typeof(T))
                            return row;
                        var typedRow = CreateTable();
                        StreamingManager.Copy(row, typedRow);
                        return (UnicontaBaseEntity)typedRow;
                    }).ToArray();

                IEnumerable<UnicontaBaseEntity> filteredRows = cachedRows;

                if (filter != null && filter.Count > 0 && cachedRows.Length > 0)
                {
                    var predicate = new DynamicWhere(cachedRows[0], Api.CompanyId);
                    predicate.AddWhere(filter);

                    // Preserve the runtime client type, including company-specific user fields.
                    var typedRows = Array.CreateInstance(cachedRows[0].GetType(), cachedRows.Length);
                    Array.Copy(cachedRows, typedRows, cachedRows.Length);

                    filteredRows = predicate
                        .GenerateQueryGeneric(typedRows.AsQueryable())
                        .Cast<UnicontaBaseEntity>();
                }

                // A sorted record-text tile selects its first row after ordering the filtered result.
                var resultRows = Definition.Visualization == TileVisualization.RecordText && !(Definition.Sorting?.Length > 0)
                    ? filteredRows.Take(1).ToArray() : filteredRows.ToArray();

                return isCount
                    ? new TileDataSourceResult(resultRows.LongLength, null)
                    : new TileDataSourceResult(null, resultRows);
            }

            // A count without a cache was already handled server-side above.
            Uniconta.ClientTools.InfoLog.Log.Error("Aggregation and chart tiles require an IdKey table.");
            return null;
        }

        List<PropValuePair> GetFilter(string filterString)
        {
            if (string.IsNullOrEmpty(filterString))
                return null;
            try
            {
                var buf = Convert.FromBase64String(filterString);
                var cs = StreamingManagerReuse.Create(buf);
                var filterValues = ((FilterProperties[])cs.ToArray(typeof(FilterProperties))).ToList();
                return UtilDisplay.GetPropValuePair(filterValues);
            }
            catch
            {
                var filters = Regex.Split(filterString, ";;;");
                var gridFilter = new List<PropValuePair>();
                foreach (var filter in filters)
                {
                    var idx = filter.IndexOf(':');
                    if (idx < 0)
                        continue;
                    var propertyName = filter.Substring(0, idx).Trim();
                    var filterValue = filter.Substring(idx + 1).Trim();
                    if (string.IsNullOrEmpty(filterValue))
                        continue;
                    var property = typeof(T).GetProperty(propertyName) ?? CorasauDataGridTemplateColumn.GetPropertyFromFieldName(typeof(T), propertyName);
                    gridFilter.Add(PropValuePair.GenereteWhereElements(propertyName, property == null ? typeof(string) : property.PropertyType, filterValue));
                }
                return gridFilter;
            }
        }
    }

    internal static class TileRecordText
    {
        internal static string Format(UnicontaBaseEntity record, IEnumerable<string> fields)
        {
            if (record == null)
                return string.Empty;
            var rowType = record.GetType();
            var values = (fields ?? new string[0]).Where(field => !string.IsNullOrWhiteSpace(field)).Select(field =>
            {
                var property = rowType.GetProperty(field) ?? CorasauDataGridTemplateColumn.GetPropertyFromFieldName(rowType, field);
                if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                    throw new InvalidOperationException("The configured display field is no longer available: " + field);
                return Convert.ToString(property.GetValue(record), CultureInfo.CurrentCulture);
            });
            return string.Join(" ", values.Where(value => !string.IsNullOrWhiteSpace(value)));
        }
    }


    internal sealed class TileDataSourceTable<T> : TileDataSourceTableBase<T> where T : class, UnicontaBaseEntity
    {
        public TileDataSourceTable(CrudAPI api, TileDefinition definition) : base(api, definition) { }
    }

    public abstract class TileDataSourcePluginBase : TileDataSource
    {
        protected TileDataSourcePluginBase(CrudAPI api, TileDefinition definition) : base(api, definition) { }

    }
}
