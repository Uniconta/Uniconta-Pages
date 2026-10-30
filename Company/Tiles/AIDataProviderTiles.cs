using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Uniconta.ClientTools;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.Controls.AI.Assistant.DataProviders;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.API.System;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    internal sealed class TileMetadataService
    {
        readonly CrudAPI api;
        internal TileMetadataService(CrudAPI api) { this.api = api; }

        internal IReadOnlyList<TileDataSourceMetadata> GetDataSources()
        {
            var retVal = Global.GetTables(api.CompanyEntity)
                .Where(type => type.GetCustomAttributes(typeof(ClientTableAttribute), true)
                    .Cast<ClientTableAttribute>().All(attribute => attribute.CanUpdate))
                .Concat(Global.GetUserTables(api.CompanyEntity).Where(type => typeof(TableDataWithKey).IsAssignableFrom(type)
                                                                           && TileTableCapabilities.SupportsLocalAggregation(type)))
                .Distinct()
                .Select(type => new TileDataSourceMetadata
                {
                    IsTable = true,
                    TableId = GetTableId(type),
                    Name = type.Name,
                    Caption = TileControl.GetTypeDisplayName(type, api.CompanyEntity),
                    Type = type
                }).Where(source => source.TableId > 0)
                .GroupBy(source => source.TableId)
                .Select(group => group.OrderByDescending(source => source.Type == UtilDisplay.GetTypeFromTableId(api.CompanyEntity, group.Key))
                    .ThenByDescending(source => source.Type.Name.EndsWith("Client", StringComparison.Ordinal))
                    .ThenBy(source => source.Name, StringComparer.Ordinal).First())
                .OrderBy(source => source.Caption).ToList();

            var pluginSources = new List<TileDataSourceMetadata>();
            if (!Uniconta.ClientTools.Plugin.DisableAllPlugins)
                foreach (var type in Uniconta.ClientTools.Plugin.TileDataSourceCache.GetSourceTypes())
                {
                    var id = type.FullName;
                    try
                    {
                        // Query metadata from the provider, never from its CLR implementation fields.
                        var provider = TileDataSource.Create(api, new TileDefinition { SourceName = id });
                        pluginSources.Add(new TileDataSourceMetadata { Name = id, Caption = provider.DisplayName, Type = provider.DataType });
                    }
                    catch (Exception exception) { Uniconta.ClientTools.InfoLog.Log.Warning("Tile source metadata unavailable: " + id + ": " + exception.Message); }
                }
            return retVal.Concat(pluginSources)
                .OrderBy(source => source.Caption)
                .ToList();
        }

        internal static IReadOnlyList<Type> GetStandaloneTileTypes()
        {
            // Built-ins and plugin classes share the same registry. Repeated registration is safe.
            var builtIn = typeof(VoucherApprovalTileType);
            Uniconta.ClientTools.Plugin.TileTypeCache.RegisterType(builtIn);
            return Uniconta.ClientTools.Plugin.TileTypeCache.GetTypes()
                .Where(type => !Uniconta.ClientTools.Plugin.DisableAllPlugins || type == builtIn).ToArray();
        }

        internal static Type ResolveStandaloneTileType(string id)
        {
            var available = GetStandaloneTileTypes();
            var type = Uniconta.ClientTools.Plugin.TileTypeCache.Resolve(id);
            if (type == null || !available.Contains(type))
                throw new ArgumentException("The configured tile type is unavailable: " + id);
            return type;
        }

        internal static string GetTileTypeCaption(Type type)
            => type == typeof(VoucherApprovalTileType) ? Uniconta.ClientTools.Localization.lookup("TileVoucherApproval") : type.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? type.Name;

        internal static IReadOnlyList<TileTypeMetadata> GetTileTypeOptions(Type tableType)
        {
            var options = TileTableCapabilities.GetVisualizations(tableType).Select(value => new TileTypeMetadata
            {
                Id = value.ToString(),
                Caption = value.DisplayName(),
                Visualization = value,
                RequiresDataSource = value != TileVisualization.HtmlWidget
            }).ToList();
            options.AddRange(GetStandaloneTileTypes().Select(type => new TileTypeMetadata
            {
                Id = type.FullName,
                Caption = GetTileTypeCaption(type),
                IsCustom = true,
                DataSources = new string[0]
            }));
            return options;
        }

        internal IReadOnlyList<TileTypeMetadata> GetTileTypes()
        {
            var sources = GetDataSources();
            // An IdKey schema exposes every generic visualization; compatibility is listed separately.
            var types = GetTileTypeOptions(typeof(IdKey));
            foreach (var type in types)
                type.DataSources = !type.RequiresDataSource ? new string[0]
                    : sources.Where(source => TileTableCapabilities.GetVisualizations(source.Type).Contains(type.Visualization.Value))
                        .Select(source => source.Name).ToArray();
            return types;
        }

        internal TileDefinition CreateCustomTile(AddTileRequest request)
        {
            var type = ResolveStandaloneTileType(request.TileTypeId);
            if (!string.IsNullOrWhiteSpace(request.DataSource))
                throw new ArgumentException("A standalone tile retrieves its own data; omit DataSource.");
            var definition = new TileDefinition
            {
                TileTypeId = type.FullName,
                Title = string.IsNullOrWhiteSpace(request.Title) ? GetTileTypeCaption(type) : request.Title.Trim(),
                ColumnSpan = request.ColumnSpan ?? 2,
                RowSpan = request.RowSpan ?? 2
            };
            definition.Validate();
            return definition;
        }

        internal static void NormalizeTileTypeRequest(AddTileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.TileTypeId))
                return;

            // The catalog exposes built-in visualization names as IDs as well. They
            // must use chart validation, not the standalone/plugin registry.
            var name = TileTableCapabilities.GetVisualizations(typeof(IdKey))
                .Select(value => value.ToString())
                .FirstOrDefault(value => string.Equals(value, request.TileTypeId.Trim(), StringComparison.OrdinalIgnoreCase));
            if (name == null)
                return;

            if (!string.IsNullOrWhiteSpace(request.Visualization)
                && !string.Equals(name, request.Visualization.Trim(), StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The tile type and visualization do not match. Choose one visualization for the tile.");

            request.Visualization = name;
            request.TileTypeId = null;
        }

        internal static int GetTableId(Type tableType)
        {
            return typeof(TableData).IsAssignableFrom(tableType)
                ? ((TableData)Activator.CreateInstance(tableType)).GetClassIdSpecial()
                : UtilDisplay.GetTableIdFromType(tableType);
        }

        internal TileDataSourceMetadata FindSource(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return GetDataSources().FirstOrDefault(source =>
                string.Equals(source.Name, value, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(source.Caption, value, StringComparison.OrdinalIgnoreCase) ||
                (source.IsTable && string.Equals(source.TableId.ToString(), value, StringComparison.Ordinal)));
        }

        internal IReadOnlyList<TileFieldMetadata> GetFields(TileDataSourceMetadata source)
        {
            if (source?.Type == null) return new TileFieldMetadata[0];
            return source.Type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .Select(property => new TileFieldMetadata
                {
                    Name = property.Name,
                    Caption = GetCaption(property),
                    DataType = (Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType).Name,
                    CanAggregate = IsNumeric(property.PropertyType),
                    CanGroup = IsGroupable(property.PropertyType),
                    Property = property
                }).Where(field => field.CanAggregate || field.CanGroup).OrderBy(field => field.Caption).ToList();
        }

        static string GetCaption(PropertyInfo property)
        {
            var display = property.GetCustomAttribute<DisplayAttribute>();
            var displayName = property.GetCustomAttribute<DisplayNameAttribute>();
            var caption = display?.GetName() ?? displayName?.DisplayName;
            return string.IsNullOrWhiteSpace(caption) ? property.Name : caption;
        }

        internal static bool IsNumeric(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
                   type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
                   type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

        static bool IsGroupable(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsEnum || type == typeof(string) || type == typeof(bool) || type == typeof(DateTime) ||
                   type == typeof(Guid) || type.IsPrimitive || type == typeof(decimal);
        }
    }

    public sealed class TileTypeMetadata
    {
        public string Id { get; set; }
        public string Caption { get; set; }
        public bool IsCustom { get; set; }
        public bool RequiresDataSource { get; set; }
        public string[] DataSources { get; set; }
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public TileVisualization? Visualization { get; set; }
    }
    public sealed class TileDataSourceMetadata
    {
        public bool IsTable { get; set; }
        public int TableId { get; set; }
        public string Name { get; set; }
        public string Caption { get; set; }
        public Type Type { get; set; }

        public string DisplayCaption => $"{Caption} [{Name}]";
    }
    internal sealed class TileFieldMetadata { public string Name; public string Caption; public string DataType; public bool CanAggregate; public bool CanGroup; public PropertyInfo Property; }

    public sealed class TilePageDataRequest : AIDataRequestBase { }
    public sealed class TileDataSourcesRequest : AIDataRequestBase { }
    public sealed class TileTypesRequest : AIDataRequestBase { }
    public sealed class TileFieldsRequest : AIDataRequestBase { [JsonProperty(Required = Required.Always)] public string DataSource { get; set; } }

    internal sealed class TilePageDataProvider : AIDataProviderBase<TilePageDataRequest>
    {
        readonly TilePage page; public TilePageDataProvider(TilePage page) { this.page = page; }
        public override string Name => "CurrentTilePage";
        public override string Description => "Returns the exact IDs and definitions of tiles currently on this dashboard. Use it before identifying a tile to remove or copying an existing tile.";
        protected override Task<AIDataProviderInvokeResult> Invoke(TilePageDataRequest request)
        {
            var data = page.CurrentTiles.Select(menu => { var d = TileDefinition.FromUserMenu(menu); return new { Id = menu.RowId, d.Title, d.TileTypeId, d.SourceName, Visualization = d.Visualization.ToString(), DataSourceId = d.TableId, ValueFields = d.ValueColumns, Aggregation = d.Aggregate.ToString(), d.GroupBy, d.Filter, d.Group, d.ColumnSpan, d.RowSpan }; }).ToArray();
            return Task.FromResult(Result(data.Length, JsonConvert.SerializeObject(data)));
        }
    }

    internal sealed class TileDataSourcesDataProvider : AIDataProviderBase<TileDataSourcesRequest>
    {
        readonly TileMetadataService metadata; public TileDataSourcesDataProvider(TilePage page) { metadata = new TileMetadataService(page.Api); }
        public override string Name => "TileDataSources";
        public override string Description => "Returns the data sources that can be used for tiles. Fetch this before TileFields; never invent a data source.";
        protected override Task<AIDataProviderInvokeResult> Invoke(TileDataSourcesRequest request) { var data = metadata.GetDataSources().Select(x => new { x.Name, x.Caption, x.IsTable, x.TableId }).ToArray(); return Task.FromResult(Result(data.Length, JsonConvert.SerializeObject(data))); }
    }

    internal sealed class TileTypesDataProvider : AIDataProviderBase<TileTypesRequest>
    {
        readonly TileMetadataService metadata;
        public TileTypesDataProvider(TilePage page) { metadata = new TileMetadataService(page.Api); }
        public override string Name => "TileTypes";
        public override string Description => "Returns built-in and custom tile types, stable IDs, whether they require an external source, and supported data source names. For IsCustom=false, copy Visualization into the tile request's Visualization and omit TileTypeId. For IsCustom=true, copy Id into TileTypeId; omit DataSource and table/aggregation fields because standalone tiles retrieve their own data.";
        protected override Task<AIDataProviderInvokeResult> Invoke(TileTypesRequest request)
        {
            var data = metadata.GetTileTypes();
            return Task.FromResult(Result(data.Count, JsonConvert.SerializeObject(data)));
        }
    }

    internal sealed class TileFieldsDataProvider : AIDataProviderBase<TileFieldsRequest>
    {
        readonly TileMetadataService metadata; public TileFieldsDataProvider(TilePage page) { metadata = new TileMetadataService(page.Api); }
        public override string Name => "TileFields";
        public override string Description => "Returns concise field metadata for one exact TileDataSources result, including whether each field supports grouping or numeric aggregation, plus valid visualizations and aggregations.";
        protected override Task<AIDataProviderInvokeResult> Invoke(TileFieldsRequest request)
        {
            var source = metadata.FindSource(request?.DataSource);
            if (source == null) return Task.FromResult(Result(0, JsonConvert.SerializeObject(new { Error = "Unknown data source." })));
            var fields = metadata.GetFields(source).Select(x => new { x.Name, x.Caption, x.DataType, x.CanAggregate, x.CanGroup }).ToArray();
            return Task.FromResult(Result(fields.Length, JsonConvert.SerializeObject(new
            {
                DataSource = source.Name,
                Fields = fields,
                Visualizations = TileTableCapabilities.GetVisualizations(source.Type)
                    .Where(value => value != TileVisualization.HtmlWidget)
                    .Select(value => value.ToString()).ToArray(),
                Aggregations = TileTableCapabilities.GetAggregates(source.Type).Select(value => value.ToString()).ToArray()
            })));
        }
    }

    public sealed class AddTileRequest
    {
        public string TileTypeId { get; set; }
        public string Visualization { get; set; }
        public string DataSource { get; set; }
        public List<string> ValueFields { get; set; } = new List<string>();
        public string Aggregation { get; set; }
        public string GroupByField { get; set; }
        public string SeriesByField { get; set; }
        public string Value2Field { get; set; }
        public string WeightField { get; set; }
        public string Title { get; set; }
        public int? ColumnSpan { get; set; }
        public int? RowSpan { get; set; }
        public List<TileFilterRequest> Filters { get; set; } = new List<TileFilterRequest>();
    }
    public sealed class TileFilterRequest { public string Field { get; set; } public string Value { get; set; } }


    internal sealed class AddTileAICapability : PromptCapabilityBase<AddTileRequest>
    {
        readonly TilePage page;
        public AddTileAICapability(TilePage page) : base(nameof(AddTileAICapability), Uniconta.ClientTools.Localization.lookup("TileAdd"), "Creates a validated dashboard tile with its own yes/no confirmation. When the user asks to add a tile, execute this capability immediately without asking for confirmation first. First use TileTypes to discover types and supported sources; use TileDataSources and TileFields for data-driven tiles. RecordText joins fields from the first matching cached record. Charts use TileVisualization values and can use multiple ValueFields where supported; range charts require Value2Field, and Bubble requires WeightField.", 100, "Show monthly revenue as a spline", "Stack revenue and cost per month", "Show min/max stock levels as a range area") { this.page = page; }
        protected override PromptDefinition GetPromptDefinition() =>
            NewPromptDefinition(
            "Configure a dashboard tile from the user's request and provider metadata. First use TileTypes. For a custom tile (IsCustom=true), copy its exact Id from TileTypes into TileTypeId and omit Visualization and field/aggregation configuration. Omit DataSource for standalone types; they retrieve their own data. Use 2x2 by default. For built-in tiles (IsCustom=false), omit TileTypeId and copy the catalog Visualization into Visualization. For record-count charts use Aggregation=Count and omit ValueFields; use the requested groupable field as GroupByField. Keep the following rules. Use only exact source and field names returned by TileDataSources and TileFields. Choose a TileVisualization name, never a DevExpress type. Number and RecordText have no GroupByField; every chart requires GroupByField. RecordText is available only for SQLCache/IdKey tables: select one or more scalar ValueFields (including text) in display order, concatenate them with spaces from the first locally filtered cache record, and omit Aggregation, SeriesByField, Value2Field and WeightField. Charts that support multiple values may have multiple numeric ValueFields, rendered as separate series. Number, Pie, Donut, NestedDonut, range, Waterfall, Bubble, Funnel, and polar/radar range visualizations accept at most one ValueField. Stacked and full-stacked visualizations require either SeriesByField or multiple ValueFields. Range visualizations require Value2Field. Bubble requires WeightField. Use 1x1 for Number and RecordText, 2x2 for circular/radar/polar charts, and 2x1 for other charts unless dimensions are requested. Return only AddTileRequest JSON.",
            "Create the requested tile. Do not report success; never ask the user for confirmation.");

        protected override Task<AIAssistantCapabilityResult> ExecuteResponse(IAIAssistantCapabilityContext context, AddTileRequest request)
        {
            TileDefinition definition; string summary;
            try { definition = Validate(request, out summary); }
            catch (ArgumentException exception) { return Task.FromResult(AIAssistantCapabilityResult.Fail(exception.Message)); }
            return Task.FromResult(new AIAssistantCapabilityResult { Succeeded = true, Message = Uniconta.ClientTools.Localization.lookup("TileConfirmAdd") + "\n\n" + summary, Actions = Confirm(() => Save(definition), Uniconta.ClientTools.Localization.lookup("TileTheTileWasNotAdded")) });
        }

        private TileDefinition Validate(AddTileRequest request, out string summary)
        {
            if (request == null) throw new ArgumentException("No tile definition was supplied.");
            TileMetadataService.NormalizeTileTypeRequest(request);

            var metadata = new TileMetadataService(page.Api);
            if (!string.IsNullOrWhiteSpace(request.TileTypeId))
            {
                var custom = metadata.CreateCustomTile(request);
                var customSize = new TileLayoutService().Normalize(custom.ColumnSpan, custom.RowSpan, page.AvailableTileColumns);
                custom.ColumnSpan = customSize.ColumnSpan;
                custom.RowSpan = customSize.RowSpan;
                custom.Order = page.CurrentTiles.Select(x => x._LineNumber).DefaultIfEmpty(0).Max() + 1;
                summary = custom.Title + "\n" + custom.TileTypeId;
                return custom;
            }
            var source = metadata.FindSource(request.DataSource);

            if (source == null) throw new ArgumentException("The selected tile data source does not exist.");

            TileVisualization visualization;
            if (!Enum.TryParse(request.Visualization, true, out visualization) || !Enum.IsDefined(typeof(TileVisualization), visualization) ||
                !TileTableCapabilities.GetVisualizations(source.Type).Contains(visualization)) throw new ArgumentException("The selected visualization is not supported.");

            var isRecordText = visualization == TileVisualization.RecordText;
            var aggregate = TileAggregate.Count;
            if (!isRecordText && !Enum.TryParse(request.Aggregation, true, out aggregate)) throw new ArgumentException("The selected aggregation is not supported.");

            var fields = metadata.GetFields(source);

            var values = (request.ValueFields ?? new List<string>())
                .Select(name => fields.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            var group = string.IsNullOrWhiteSpace(request.GroupByField) ? null : fields.FirstOrDefault(x => string.Equals(x.Name, request.GroupByField, StringComparison.OrdinalIgnoreCase));

            var seriesBy = string.IsNullOrWhiteSpace(request.SeriesByField) ? null : fields.FirstOrDefault(x => string.Equals(x.Name, request.SeriesByField, StringComparison.OrdinalIgnoreCase));

            var value2 = string.IsNullOrWhiteSpace(request.Value2Field) ? null : fields.FirstOrDefault(x => string.Equals(x.Name, request.Value2Field, StringComparison.OrdinalIgnoreCase));

            var weight = string.IsNullOrWhiteSpace(request.WeightField) ? null : fields.FirstOrDefault(x => string.Equals(x.Name, request.WeightField, StringComparison.OrdinalIgnoreCase));

            if (values.Any(value => value == null || (isRecordText ? !value.CanGroup : !value.CanAggregate)))
                throw new ArgumentException(isRecordText ? "Every display field must be an existing scalar field." : "Every value field must be an existing numeric field.");

            if (isRecordText && values.Count == 0)
                throw new ArgumentException("At least one display field is required for a record text tile.");

            if (!isRecordText && aggregate != TileAggregate.Count && values.Count == 0)
                throw new ArgumentException("At least one value field is required for " + aggregate + ".");

            if (!visualization.SupportsMultipleValues() && values.Count > 1)
                throw new ArgumentException("This visualization supports only one value field.");

            if (visualization != TileVisualization.Number && !isRecordText && (group == null || !group.CanGroup))
                throw new ArgumentException("This visualization requires an existing groupable Group By field.");

            if (visualization == TileVisualization.Number && group != null)
                throw new ArgumentException("A KPI/Number tile cannot have a Group By field.");

            if (isRecordText && (!string.IsNullOrWhiteSpace(request.GroupByField) || !string.IsNullOrWhiteSpace(request.SeriesByField) ||
                !string.IsNullOrWhiteSpace(request.Value2Field) || !string.IsNullOrWhiteSpace(request.WeightField)))
                throw new ArgumentException("A record text tile cannot have chart grouping or extra chart values.");

            if (visualization.RequiresSeries() && (seriesBy == null || !seriesBy.CanGroup) && values.Count < 2)
                throw new ArgumentException("This visualization requires a Series By field or multiple value fields.");

            if (visualization.RequiresValue2() && (value2 == null || !value2.CanAggregate))
                throw new ArgumentException("This range visualization requires a second numeric value field.");

            if (visualization == TileVisualization.Bubble && (weight == null || !weight.CanAggregate))
                throw new ArgumentException("Bubble charts require a numeric weight field.");

            var filters = new List<string>();

            foreach (var filter in request.Filters ?? new List<TileFilterRequest>())
            {
                var field = fields.FirstOrDefault(x => string.Equals(x.Name, filter?.Field, StringComparison.OrdinalIgnoreCase));

                if (field == null || string.IsNullOrWhiteSpace(filter.Value))
                    throw new ArgumentException("Every filter must use an existing field and a non-empty value.");

                filters.Add(field.Name + ":" + filter.Value);
            }

            var title = string.IsNullOrWhiteSpace(request.Title)
                ? (isRecordText ? string.Join(" / ", values.Select(value => value.Caption)) : (group == null
                    ? string.Format(Uniconta.ClientTools.Localization.lookup("TileAggregateTitle"), aggregate.DisplayName(), values.FirstOrDefault()?.Caption ?? source.Caption)
                    : string.Format(Uniconta.ClientTools.Localization.lookup("TileGroupedAggregateTitle"), aggregate.DisplayName(), values.FirstOrDefault()?.Caption ?? source.Caption, group.Caption)))
                : request.Title.Trim();

            var defaultColumns = visualization == TileVisualization.Number || isRecordText ? 1 : 2;
            var defaultRows = visualization == TileVisualization.Pie || visualization == TileVisualization.Donut ? 2 : 1;

            var requestedColumns = request.ColumnSpan ?? defaultColumns;
            var requestedRows = request.RowSpan ?? defaultRows;

            if (requestedColumns < 1 || requestedRows < 1)
                throw new ArgumentException("Tile spans must be positive grid dimensions.");

            var size = new TileLayoutService().Normalize(requestedColumns, requestedRows, page.AvailableTileColumns);

            var definition = new TileDefinition
            {
                TableId = source.TableId,
                SourceName = source.IsTable ? null : source.Name,
                Title = title,
                Visualization = visualization,
                Aggregate = aggregate,
                ValueColumns = values.Select(value => value.Name).ToList(),
                Value2Field = value2?.Name,
                WeightField = weight?.Name,
                GroupBy = group?.Name,
                ChartGroup = seriesBy?.Name,
                Filter = filters.Count == 0 ? null : string.Join(";;;", filters),
                NavigationTarget = UserControlProperties.GetControlName(source.TableId),
                Scope = TileScope.User,
                Order = page.CurrentTiles.Select(x => x._LineNumber).DefaultIfEmpty(0).Max() + 1,
                ColumnSpan = size.ColumnSpan,
                RowSpan = size.RowSpan
            };

            definition.Validate();
            TileTableCapabilities.Validate(source.Type, definition);

            summary = visualization.DisplayName() + "\n" +
                (values.Count == 0 ? source.Caption : string.Join(", ", values.Select(value => value.Caption))) +
                (isRecordText ? ": " + Uniconta.ClientTools.Localization.lookup("TileFirstMatchingRecord") : ": " + aggregate.DisplayName()) +
                (group == null ? string.Empty : "\n" + Uniconta.ClientTools.Localization.lookup("MenuColumnGroup") + ": " + group.Caption);

            return definition;
        }

        async Task<AIAssistantCapabilityResult> Save(TileDefinition definition) { var error = await page.AddTileAsync(definition); return error == ErrorCodes.Succes ? AIAssistantCapabilityResult.Success(string.Format(Uniconta.ClientTools.Localization.lookup("TileAddedMessage"), definition.Title)) : AIAssistantCapabilityResult.Fail(string.Format(Uniconta.ClientTools.Localization.lookup("TileAddFailed"), error)); }
        static IReadOnlyList<AIAssistantCapabilityResult.CapabilityAction> Confirm(Func<Task<AIAssistantCapabilityResult>> yes, string no) => new[] { new AIAssistantCapabilityResult.CapabilityAction { Caption = Localization.lookup("Yes"), ExecuteAsync = yes }, new AIAssistantCapabilityResult.CapabilityAction { Caption = Localization.lookup("No"), ExecuteAsync = () => Task.FromResult(AIAssistantCapabilityResult.Success(no)) } };
    }

    public sealed class ResizeTileRequest
    {
        public long TileId { get; set; }
        public int ColumnSpan { get; set; }
        public int RowSpan { get; set; }
    }

    internal sealed class ResizeTileAICapability : PromptCapabilityBase<ResizeTileRequest>
    {
        readonly TilePage page;
        public ResizeTileAICapability(TilePage page) : base(nameof(ResizeTileAICapability), Uniconta.ClientTools.Localization.lookup("TileResizeTile"), "Resizes an existing dashboard tile in whole grid cells after confirmation. Use CurrentTilePage to resolve its exact ID and current spans.", 100, "Make the revenue tile wider", "Make the invoice chart 3 by 2") { this.page = page; }
        protected override PromptDefinition GetPromptDefinition() => NewPromptDefinition(
            "Resolve exactly one tile using CurrentTilePage. Return its exact TileId and positive integer ColumnSpan and RowSpan. Interpret wider, taller, smaller, and relative requests from its current spans; never guess when the tile is ambiguous.",
            "Prepare the requested grid resize. It will be validated and confirmed before mutation.");

        protected override Task<AIAssistantCapabilityResult> ExecuteResponse(IAIAssistantCapabilityContext context, ResizeTileRequest request)
        {
            var menu = page.CurrentTiles.SingleOrDefault(x => x.RowId == request.TileId);
            if (menu == null) return Task.FromResult(AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("TileThatTileIsNotPresentOnTheCurrent")));
            if (request.ColumnSpan < 1 || request.RowSpan < 1) return Task.FromResult(AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("TileInvalidSize")));
            var size = new TileLayoutService().Normalize(request.ColumnSpan, request.RowSpan, page.AvailableTileColumns);
            var definition = TileDefinition.FromUserMenu(menu);
            return Task.FromResult(new AIAssistantCapabilityResult
            {
                Succeeded = true,
                Message = string.Format(Uniconta.ClientTools.Localization.lookup("TileConfirmResize"), definition.Title, size.ColumnSpan, size.RowSpan),
                Actions = new[]
                {
                    new AIAssistantCapabilityResult.CapabilityAction { Caption = Localization.lookup("Yes"), ExecuteAsync = async () => { var error = await page.ResizeTileAsync(menu, size.ColumnSpan, size.RowSpan); return error == ErrorCodes.Succes ? AIAssistantCapabilityResult.Success(string.Format(Uniconta.ClientTools.Localization.lookup("TileResizedMessage"), definition.Title)) : AIAssistantCapabilityResult.Fail(string.Format(Uniconta.ClientTools.Localization.lookup("TileResizeFailed"), error)); } },
                    new AIAssistantCapabilityResult.CapabilityAction { Caption = Localization.lookup("No"), ExecuteAsync = () => Task.FromResult(AIAssistantCapabilityResult.Success(Uniconta.ClientTools.Localization.lookup("TileTheTileWasNotResized"))) }
                }
            });
        }
    }

    public sealed class RemoveTileRequest { public long TileId { get; set; } }
    internal sealed class RemoveTileAICapability : PromptCapabilityBase<RemoveTileRequest>
    {
        readonly TilePage page;
        public RemoveTileAICapability(TilePage page) : base(nameof(RemoveTileAICapability), Uniconta.ClientTools.Localization.lookup("TileRemove"), "Removes one existing dashboard tile after explicit confirmation. Use CurrentTilePage to resolve the exact tile ID; never use an index or guess.", 100, "Remove the invoice count tile", "Remove the donut showing revenue") { this.page = page; }
        protected override PromptDefinition GetPromptDefinition() => NewPromptDefinition("Resolve exactly one tile from the CurrentTilePage provider result. Return only RemoveTileRequest JSON with its exact TileId. If ambiguous, do not guess.", "Identify the exact tile the user asked to remove.");
        protected override Task<AIAssistantCapabilityResult> ExecuteResponse(IAIAssistantCapabilityContext context, RemoveTileRequest request)
        {
            var tile = page.CurrentTiles.SingleOrDefault(x => x.RowId == request.TileId);
            if (tile == null) return Task.FromResult(AIAssistantCapabilityResult.Fail(Uniconta.ClientTools.Localization.lookup("TileThatTileIsNotPresentOnTheCurrent")));
            var definition = TileDefinition.FromUserMenu(tile);
            return Task.FromResult(new AIAssistantCapabilityResult { Succeeded = true, Message = Uniconta.ClientTools.Localization.lookup("TileConfirmRemove") + "\n\n" + definition.Title + "\n" + definition.Visualization.DisplayName(), Actions = new[] { new AIAssistantCapabilityResult.CapabilityAction { Caption = Localization.lookup("Yes"), ExecuteAsync = async () => { var error = await page.RemoveTileAsync(tile); return error == ErrorCodes.Succes ? AIAssistantCapabilityResult.Success(string.Format(Uniconta.ClientTools.Localization.lookup("TileRemovedMessage"), definition.Title)) : AIAssistantCapabilityResult.Fail(string.Format(Uniconta.ClientTools.Localization.lookup("TileRemoveFailed"), error)); } }, new AIAssistantCapabilityResult.CapabilityAction { Caption = Localization.lookup("No"), ExecuteAsync = () => Task.FromResult(AIAssistantCapabilityResult.Success(Uniconta.ClientTools.Localization.lookup("TileTheTileWasNotRemoved"))) } } });
        }
    }
}
