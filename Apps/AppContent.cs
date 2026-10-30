using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class AppContent
    {
        #region Properties
        public Uniconta.DataModel.App App { get; set; }

        public int AppId { get => App?.AppId ?? 0; }
        public string AppName { get => App?._Name; }
        public string AppUrl { get => App?._URL; }
        public string AppComment { get => App?._Comment; }

        public byte[] AppLogo { get; set; }

        #endregion

        CrudAPI api;

        #region Itemised List

        public List<AppStandardTable> StandardTables { get; set; }
        public List<AppTable> UserTables { get; set; }
        // List<AppField> fields;
        public List<AppLabel> Labels { get; set; }
        public List<AppMenu> Menus { get; set; }

        public List<AppDashboardClient> Dashboards { get; set; }

        public List<AppReportDevExpressClient> Reports { get; set; }

        public List<AppQTSQLClient> Qts { get; set; }

        public List<AppPluginClient> Plugins { get; set; }

        public List<AppDLLClient> Dlls { get; set; }

        public List<AppSummaryItem> SummaryList
        {
            get
            {
                var list = new List<AppSummaryItem>();

                if (StandardTables != null)
                    list.AddRange(StandardTables
                        .Where(x => x?.AppFields != null)
                        .SelectMany(x => x.AppFields.Select(f => new AppSummaryItem { Type = $"{GetRawTableName(x.TableName)}.Fields", Name = f?.Name, Comment = f?.Prompt })));

                if (UserTables != null)
                    list.AddRange(UserTables
                        .Where(x => x?.AppFields != null)
                        .SelectMany(x => x.AppFields.Select(f => new AppSummaryItem { Type = $"{x.Name}.Fields", Name = f?.Name, Comment = f?.Prompt })));

                if (Labels != null)
                    list.AddRange(Labels.Select(x => new AppSummaryItem { Type = "Label", Name = x.Label, Comment = x.Text }));

                if (Menus != null)
                    list.AddRange(Menus.Select(x => new AppSummaryItem { Type = "Menu", Name = x.Prompt, Comment = x.Argument }));

                if (Dashboards != null)
                    list.AddRange(Dashboards.Select(x => new AppSummaryItem { Type = "Dashboard", Name = x.Name, Comment = x.Comment }));

                if (Reports != null)
                    list.AddRange(Reports.Select(x => new AppSummaryItem { Type = "Report", Name = x.Name, Comment = x.Comment }));

                if (Qts != null)
                    list.AddRange(Qts.Select(x => new AppSummaryItem { Type = "Qt", Name = x.Name, Comment = x.Comment }));

                if (Plugins != null)
                    list.AddRange(Plugins.Select(x => new AppSummaryItem { Type = "Plugin", Name = x.Name, Comment = x.Comment }));

                if (Dlls != null)
                    list.AddRange(Dlls.Select(x => new AppSummaryItem { Type = "Dll", Name = x.Name, Comment = x.Comment }));

                return list;
            }
        }

        #endregion

        #region Initalization

        internal AppContent(Uniconta.DataModel.App app, CrudAPI api)
        {
            this.App = app;
            this.api = api;
        }

        #endregion

        async public Task LoadContent()
        {
            var fieldsAndTablesTask = LoadUserTableAndFields();
            var labelsTask = api.Query<AppLabel>(App, null);
            var menusTask = api.Query<AppMenu>(App, null);
            var standardTablesTask = LoadStandardTableFields();
            var dashboardsTask = api.Query<AppDashboardClient>(App, null);
            var reportsTask = api.Query<AppReportDevExpressClient>(App, null);
            var qtsTask = api.Query<AppQTSQLClient>(App, null);
            var pluginsTask = api.Query<AppPluginClient>(App, null);
            var dllsTask = api.Query<AppDLLClient>(App, null);
            await Task.WhenAll(fieldsAndTablesTask, labelsTask, menusTask, standardTablesTask, dashboardsTask, reportsTask, qtsTask, pluginsTask, dllsTask);
            //await fieldsAndTablesTask;
            Labels = (await labelsTask)?.ToList() ?? new List<AppLabel>();
            Menus = (await menusTask)?.ToList() ?? new List<AppMenu>();
            Dashboards = (await dashboardsTask)?.ToList() ?? new List<AppDashboardClient>();
            Reports = (await reportsTask)?.ToList() ?? new List<AppReportDevExpressClient>();
            Qts = (await qtsTask)?.ToList() ?? new List<AppQTSQLClient>();
            Plugins = (await pluginsTask)?.ToList() ?? new List<AppPluginClient>();
            Dlls = (await dllsTask)?.ToList() ?? new List<AppDLLClient>();
        }

        static string GetRawTableName(string tableName)
        {
            if (string.IsNullOrEmpty(tableName))
                return tableName;

            var index = tableName.IndexOf('(');
            return index >= 0 ? tableName.Substring(0, index).TrimEnd() : tableName;
        }

        async Task LoadUserTableAndFields()
        {
            UserTables = (await api.Query<AppTable>(App, null))?.ToList() ?? new List<AppTable>();

            foreach (var t in UserTables)
            {
                var fields = await api.Query<AppField>(t, null);
                t.AppFields = fields?.ToList();
            }

            //await Task.WhenAll(fieldsTask, tablesTask);

            //var fields = (await fieldsTask)?.ToList() ?? new List<AppField>();
            //tables = (await tablesTask)?.ToList() ?? new List<AppTable>();

            //var fieldsByTableId = fields
            //    .GroupBy(x => x.TableId)
            //    .ToDictionary(x => x.Key, x => x.ToList());

            //foreach (var table in tables)
            //    table.fields = fieldsByTableId.TryGetValue(table.TableId, out var tableFields) ? tableFields : new List<AppField>();
        }

        async Task LoadStandardTableFields()
        {
            var fields = (await api.Query<AppField>(App, null))?.ToList().OrderBy(f => f.TableId);
            StandardTables = new List<AppStandardTable>();
            var lastTableId = 0;
            AppStandardTable st = null;
            foreach (var f in fields)
            {
                if (f.TableId > 60000)
                    break;
                if (lastTableId != f.TableId)
                {
                    st = new AppStandardTable();
                    StandardTables.Add(st);
                }
                lastTableId = f.TableId;
                var type = UtilDisplay.GetTypeFromTableId(UtilDisplay.GetDefaultCompany(), f.TableId);
                if (type != null)
                {
                    var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                    if (clientTableAttr.Length > 0)
                    {
                        var attr = (ClientTableAttribute)clientTableAttr[0];
                        st.TableName = Util.ConcatParenthesis(type.Name, Uniconta.ClientTools.Localization.lookup(attr.LabelKey));
                        if (st.AppFields == null)
                            st.AppFields = new List<AppField>();
                    }
                    else
                        st.TableName = type.Name;
                    st.AppFields.Add(f);

                }
            }

            //await Task.WhenAll(fieldsTask, tablesTask);

            //var fields = (await fieldsTask)?.ToList() ?? new List<AppField>();
            //tables = (await tablesTask)?.ToList() ?? new List<AppTable>();

            //var fieldsByTableId = fields
            //    .GroupBy(x => x.TableId)
            //    .ToDictionary(x => x.Key, x => x.ToList());

            //foreach (var table in tables)
            //    table.fields = fieldsByTableId.TryGetValue(table.TableId, out var tableFields) ? tableFields : new List<AppField>();
        }


    }

    public class AppTable : AppTableHeaderClient
    {
        public List<AppField> AppFields { get; set; }
    }
    public class AppField : AppTableFieldsClient
    {
    }

    public class AppStandardTable
    {
        public string TableName { get; set; }
        public List<AppField> AppFields { get; set; }
    }
    public class AppLabel : AppUserLabelClient
    {

    }
    public class AppMenu : AppUserMenuClient
    {
    }

    public class AppSummaryItem
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
    }
}
