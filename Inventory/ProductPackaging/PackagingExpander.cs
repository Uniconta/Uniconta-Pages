using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingOverviewText
    {
        public static string Source { get { return Localization.lookup("Source"); } }
        public static string ReportingGroup { get { return Localization.lookup("ReportingType"); } }
        public static string ItemName { get { return Localization.lookup("ItemName"); } }
    }

    public class InvPackagingOverviewLine : InvPackagingProductClient
    {
        public string _Source;

        [Display(Name = "Source", ResourceType = typeof(InvPackagingOverviewText))]
        public string Source { get { return _Source; } }

        [Display(Name = "ItemName", ResourceType = typeof(InvPackagingOverviewText))]
        public string ItemName { get { return ClientHelper.GetName(CompanyId, typeof(InvItem), _Item); } }

        [Display(Name = "ReportingGroup", ResourceType = typeof(InvPackagingOverviewText))]
        public string ReportingGroup { get { return string.Concat(AppEnums.PackagingReportingType.ToString((byte)_Reporting), " (", Unit, ")"); } }
    }

    public class PackagingExpander
    {
        readonly CrudAPI api;
        SQLCache items;
        Dictionary<string, List<InvPackagingProductClient>> ownLinesByItem;
        Dictionary<string, InvPackingProductModelLine[]> modelLinesByModel;
        readonly Dictionary<string, InvBOMClient[]> bomLinesCache = new Dictionary<string, InvBOMClient[]>();

        public PackagingExpander(CrudAPI api) { this.api = api; }

        public Type LineType { get; private set; } = typeof(InvPackagingOverviewLine);

        InvPackagingOverviewLine NewLine() { return (InvPackagingOverviewLine)Activator.CreateInstance(LineType); }

        static void CopyUserFields(UnicontaBaseEntity from, InvPackagingOverviewLine to)
        {
            if (from is ITableFieldData src && src.UserFieldDef() != null)
                ((ITableFieldData)to).UserField = src.UserField;
        }

        public async Task Load()
        {
            LineType = Global.GetTableWithUserFields(api.CompanyEntity, typeof(InvPackagingOverviewLine), out _) ?? typeof(InvPackagingOverviewLine);
            items = api.GetCache(typeof(InvItem)) ?? await api.LoadCache(typeof(InvItem));

            var own = await api.Query<InvPackagingProductClient>();
            ownLinesByItem = (own ?? Array.Empty<InvPackagingProductClient>())
                .Where(p => !string.IsNullOrEmpty(p._Item) && p._Variant == 0)
                .GroupBy(p => p._Item)
                .ToDictionary(g => g.Key, g => g.ToList());

            var modelLines = await api.Query<InvPackingProductModelLine>();
            modelLinesByModel = (modelLines ?? Array.Empty<InvPackingProductModelLine>())
                .Where(l => l._Model != null)
                .GroupBy(l => l._Model)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public IEnumerable<InvItem> ItemsWithPackaging()
        {
            var result = new List<InvItem>();
            foreach (var key in ownLinesByItem.Keys)
            {
                var item = (InvItem)items.Get(key);
                if (item != null)
                    result.Add(item);
            }
            foreach (InvItem item in items.GetNotNullArray)
                if (item._PackingModel != null && !ownLinesByItem.ContainsKey(item._Item))
                    result.Add(item);
            return result;
        }

        public IEnumerable<InvItem> ProductionBOMsWithoutPackaging()
        {
            foreach (InvItem item in items.GetNotNullArray)
                if (item._ItemType == (byte)ItemType.ProductionBOM && item._PackingModel == null && !ownLinesByItem.ContainsKey(item._Item))
                    yield return item;
        }

        public List<InvPackagingOverviewLine> ExpandOwn(InvItem item)
        {
            var result = new List<InvPackagingOverviewLine>();
            if (ownLinesByItem.TryGetValue(item._Item, out var ownLines))
            {
                foreach (var line in ownLines)
                {
                    if (line._PackingModel == null)
                        result.Add(Copy(line, item._Item, line._Weight, Localization.lookup("Line")));
                    else
                    {
                        var modelQty = line._Weight > 0 ? line._Weight : 1d;
                        AddModel(result, item._Item, line._PackingModel, modelQty,
                            string.Format(Localization.lookup("PackagingSourceModelOBJ"), line._PackingModel, modelQty.ToString("0.####")), line);
                    }
                }
            }
            if (item._PackingModel != null)
                AddModel(result, item._Item, item._PackingModel, 1d, string.Format(Localization.lookup("PackagingSourceItemModelOBJ"), item._PackingModel), null);
            return result;
        }

        public async Task<List<InvPackagingOverviewLine>> Expand(InvItem item)
        {
            var result = ExpandOwn(item);
            if (result.Count == 0 && item._ItemType == (byte)ItemType.ProductionBOM)
                await ExpandBOM(result, item, item, 1d, 0);
            return result;
        }

        async Task ExpandBOM(List<InvPackagingOverviewLine> result, InvItem topItem, InvItem bomItem, double qty, int level)
        {
            if (level > 25) // guard against circular BOM data, as on the server
                return;

            foreach (var bom in await GetBOMLines(bomItem))
            {
                var part = (InvItem)items.Get(bom._ItemPart);
                if (part == null)
                    continue;

                var partQty = bom.GetBOMQty(qty);
                var partLines = ExpandOwn(part);
                if (partLines.Count > 0)
                {
                    var source = string.Format(Localization.lookup("PackagingSourceBOMOBJ"), part._Item, partQty.ToString("0.####"));
                    foreach (var line in partLines)
                        result.Add(Copy(line, topItem._Item, line._Weight * partQty, string.Concat(source, " - ", line._Source)));
                }
                else if (part._ItemType == (byte)ItemType.ProductionBOM)
                    await ExpandBOM(result, topItem, part, partQty, level + 1);
            }
        }

        async Task<InvBOMClient[]> GetBOMLines(InvItem bomItem)
        {
            if (bomLinesCache.TryGetValue(bomItem._Item, out var lines))
                return lines;
            lines = await api.Query<InvBOMClient>(bomItem) ?? Array.Empty<InvBOMClient>();
            bomLinesCache[bomItem._Item] = lines;
            return lines;
        }

        void AddModel(List<InvPackagingOverviewLine> result, string item, string model, double modelQty, string source, InvPackagingProductClient userFieldsFrom)
        {
            if (!modelLinesByModel.TryGetValue(model, out var modelLines))
                return;
            foreach (var line in modelLines)
            {
                var row = NewLine();
                row.CompanyId = api.CompanyId;
                row._Item = item;
                row._Reporting = line._Reporting;
                row._Category = line._Category;
                row._Packaging = line._Packaging;
                row._WasteSorting = line._WasteSorting;
                row._PackagingRateLevel = line._PackagingRateLevel;
                row._PaymentGrouping = line._PaymentGrouping;
                row._NoReporting = line._NoReporting;
                row._Weight = line._Weight * modelQty;
                row._Price1 = line._Price1;
                row._Price2 = line._Price2;
                row._Text = line._Text;
                row._Source = source;
                if (userFieldsFrom != null)
                    CopyUserFields(userFieldsFrom, row);
                result.Add(row);
            }
        }

        InvPackagingOverviewLine Copy(InvPackagingProductClient line, string item, double weight, string source)
        {
            var row = NewLine();
            row.CompanyId = api.CompanyId;
            row._Item = item;
            row._Reporting = line._Reporting;
            row._Category = line._Category;
            row._Packaging = line._Packaging;
            row._WasteSorting = line._WasteSorting;
            row._PackagingRateLevel = line._PackagingRateLevel;
            row._PaymentGrouping = line._PaymentGrouping;
            row._NoReporting = line._NoReporting;
            row._Weight = weight;
            row._Price1 = line._Price1;
            row._Price2 = line._Price2;
            row._Text = line._Text;
            row._Source = source;
            CopyUserFields(line, row);
            return row;
        }
    }
}
