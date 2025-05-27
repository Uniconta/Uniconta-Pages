using UnicontaClient.Models;
using DevExpress.DashboardCommon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Controls;
using UnicontaClient.Utilities;
using Uniconta.API.Service;
using System.Windows.Threading;
using Uniconta.Common.Utility;
using System.Data;
using Uniconta.ClientTools.Util;
using DevExpress.Data.Filtering;
using DashBoardView;
using Uniconta.ClientTools.Dashboard;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DashBoardViewerPage : ControlBasePage
    {
        #region Fields

        Type tableType;
        DashboardClient _selectedDashBoard;
        Dictionary<string, Type> dataSourceAndTypeMap;
        CWServerFilter filterDialog;
        private FilterSorter PropSort;
        private Dictionary<string, List<FilterProperties>> lstOfNewFilters;
        private Dictionary<string, FilterSorter> lstOfSorters;
        private Dictionary<int, Type[]> ListOfReportTableTypes;
        private Dictionary<string, UnicontaBaseEntity[]> lstOfDataSources;
        private IEnumerable<PropValuePair> filterValues;
        Dictionary<string, List<DashboardUserField>> dashboardUserFields;
        DualKeyDictionary<string, Language, string> dashboardLocalizationLabels;

        string selectedDataSourceName;
        Company company;
        DashboardState dState;
        bool LoadOnOpen;
        List<FixedCompany> fixedCompanies;
        string dashboardName;
        System.Windows.Forms.Timer timer;
        string UserIdPropName;
        UnicontaBaseEntity master;
        string masterField;
        string titleText = string.Empty;
        Dictionary<string, bool> HasNestedField;
        DashboardItem FocusedItem;
        bool isDashBoardLaoded;
        Dashboard _dashboard;
        List<string> dataSourceLoadingParams = new List<string>();

        #endregion

        #region Constructors

        /// <summary>
        /// Default Initialization
        /// </summary>
        /// <param name="API"></param>
        public DashBoardViewerPage(BaseAPI API)
           : base(API, string.Empty)
        {
            InitPage();
        }

        /// <summary>
        /// Initialization with syncEntity
        /// </summary>
        /// <param name="syncEntity">SyncEntity object</param>
        public DashBoardViewerPage(SynchronizeEntity syncEntity) : base(true, syncEntity)
        {
            master = syncEntity.Row;
            this.InSyncMode = true;
            InitPage();
        }

        /// <summary>
        /// Initialization with UnicontaBaseEntity
        /// </summary>
        /// <param name="entity">UnicontaBaseEntity</param>
        public DashBoardViewerPage(UnicontaBaseEntity entity) : base(entity)
        {
            _selectedDashBoard = entity as DashboardClient;
            if (_selectedDashBoard == null)
                master = entity;
            InitPage();
        }

        #endregion

        #region Overriden Members

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            master = args;
            SetHeader(master);
            dashboardViewerUniconta?.ReloadData();
        }
        public override string NameOfControl { get { return TabControls.DashBoardViewerPage; } }

        public override void PageClosing()
        {
            if (timer != null)
                timer.Tick -= Timer_Tick;

            //Removing registered functions
            foreach (var func in CriteriaOperator.GetCustomFunctions())
            {
                if (func is GetAppEnumIndexFunction appEnumFunc)
                    CriteriaOperator.UnregisterCustomFunction(appEnumFunc);
            }

            base.PageClosing();
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (rec.Name == null || string.Compare(rec.Name, "Dashboard", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    dashboardName = rec.Value;
                    SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", dashboardName));
                }
                if (string.Compare(rec.Name, "Field", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    masterField = rec.Value;
                }
            }
            base.SetParameter(Parameters);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Event for dataloading error on dashboard
        /// </summary>
        private void DashboardViewerUniconta_DataLoadingError(object sender, DataLoadingErrorEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// Setting the InitialState of Dashboard viewer
        /// </summary>
        private void DashboardViewerUniconta_SetInitialDashboardState(object sender, DevExpress.DashboardWpf.SetInitialDashboardStateWpfEventArgs e)
        {
            e.InitialState = dState;
        }

        /// <summary>
        /// Dashboard loaded event
        /// </summary>  
        private void DashboardViewerUniconta_DashboardLoaded(object sender, DevExpress.DashboardWpf.DashboardLoadedEventArgs e)
        {
            Dashboard dasdboard = e.Dashboard;
            HasNestedField = new Dictionary<string, bool>();

            if (!dasdboard.Title.ShowMasterFilterState)
                /*localMenu*/
                rowFilter.Visibility = Visibility.Collapsed;

            XElement data = e.Dashboard.UserData;
            if (data != null)
            {
                try
                {
                    var state = data.Element(DashboardCommon.STATE);
                    if (state != null)
                        dState.LoadFromXml(XDocument.Parse(state.Value));
                }
                catch { }

                var fixedComps = data.Element(DashboardCommon.FIXED_COMPANIES);
                if (fixedComps != null)
                {
                    fixedCompanies = null;
                    var rootelement = XElement.Parse(fixedComps.ToString());
                    foreach (var el in rootelement.Elements())
                    {
                        var fxdComps = XDocument.Parse(el.ToString()).Elements(DashboardCommon.FIXED_COMPANY)
                             .Select(p => new FixedCompany
                             {
                                 CompanyId = int.Parse(p.Element(DashboardCommon.FIXED_COMPANY_ID).Value),
                                 DatasourceName = p.Element(DashboardCommon.FIXED_DATASOURCE_NAME).Value
                             }).ToList();
                        if (fixedCompanies != null)
                            fixedCompanies.AddRange(fxdComps);
                        else
                            fixedCompanies = fxdComps;
                    }

                    if (fixedCompanies != null)
                    {
                        for (int i = 0; i < fixedCompanies.Count; i++)
                        {
                            var comp = CWDefaultCompany.loadedCompanies.FirstOrDefault(x => x.CompanyId == fixedCompanies[i].CompanyId) as Company;
                            var openComp = OpenFixedCompany(comp).GetAwaiter().GetResult();
                            if (openComp != null)
                                LoadListOfTableTypes(openComp);
                            else
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UserNoAccessToCompany"), Uniconta.ClientTools.Localization.lookup("Information"));
                        }
                    }
                }

                var ldOnOpn = data.Element(DashboardCommon.LOAD_ON_OPEN);
                if (ldOnOpn != null)
                    LoadOnOpen = bool.Parse(ldOnOpn.Value);
                var tlTxt = data.Element(DashboardCommon.TITLE_TEXT);
                if (tlTxt != null)
                    titleText = tlTxt.Value;
                var userId = data.Element(DashboardCommon.LOGGED_IN_USER_ID);
                if (userId != null)
                    UserIdPropName = userId.Value;

                #region Dashboard user fields

                //loading the dashboard userfields from xml
                dashboardUserFields = DashboardCommon.ConvertXmlForUserFields(data.Element(DashboardCommon.USER_FIELDS));

                #endregion

                #region Dashboard Localization labels

                //loading the localized labels from xml
                dashboardLocalizationLabels = DashboardCommon.ConvertXmlForLocalization(data.Element(DashboardCommon.LOCALIZATION_LABELS));

                #endregion
            }
            else
                LoadOnOpen = true;  // for old saved dashboards

            //Updating the controls
            UpdateControls(dasdboard);

            if (LoadOnOpen)
                foreach (var ds in e.Dashboard.DataSources)
                    dataSourceLoadingParams.Add(ds.ComponentName);
        }

        private void UpdateControls(Dashboard dasdboard)
        {
            foreach (var item in dasdboard?.Items)
            {
                var name = dasdboard?.CustomProperties.GetValue(item.ComponentName);
                var dataSourceComponentName = GetDataSourceComponentName(item);
                if (name != null)
                {
                    if (name[0] == '&' || name[0] == '@')
                        item.Name = GetCustomLocalizedString(name.Substring(1)); ;
                }

                if (item is GridDashboardItem grid)
                {
                    grid.ColumnFilterOptions.UpdateTotals = true;
                    var targetLst = grid.Columns;
                    foreach (var col in targetLst)
                    {
                        if (col is GridDimensionColumn dimCol)
                        {
                            var dimName = dimCol.Dimension.Name;
                            var colName = col.Name;
                            var savedName = col.CustomProperties.GetValue(dimCol.Dimension.UniqueId) ?? string.Empty;

                            if (!string.IsNullOrEmpty(dimName) && (dimName.StartsWith("&") || dimName.StartsWith("@") || savedName.StartsWith("@") || savedName.StartsWith("&")))
                            {
                                if (col.CustomProperties.GetValue(dimCol.Dimension.UniqueId) == null)
                                    col.CustomProperties.SetValue(dimCol.Dimension.UniqueId, dimName);
                                else if ((dimCol.Dimension.Name.StartsWith("@") || dimCol.Dimension.Name.StartsWith("&")) && (savedName.StartsWith("@") || savedName.StartsWith("&")) && string.Compare(dimName, savedName) != 0)
                                    col.CustomProperties.SetValue(dimCol.Dimension.UniqueId, dimName);
                                else
                                    dimName = savedName;
                                dimCol.Dimension.Name = GetCustomLocalizedString(dimName.Substring(1));
                            }
                            IsNestedField(dimCol.Dimension, dataSourceComponentName);
                        }
                        else if (col is GridMeasureColumn meaCol)
                        {
                            var colName = meaCol.Measure.Name;
                            var savedName = col.CustomProperties.GetValue(meaCol.Measure.UniqueId) ?? string.Empty;

                            if (!string.IsNullOrEmpty(colName) && (colName.StartsWith("&") || colName.StartsWith("@")))
                            {
                                if (col.CustomProperties.GetValue(meaCol.Measure.UniqueId) == null)
                                    col.CustomProperties.SetValue(meaCol.Measure.UniqueId, name);
                                else
                                    colName = savedName;
                                meaCol.Measure.Name = GetCustomLocalizedString(colName.Substring(1));
                            }
                            IsNestedField(meaCol.Measure, dataSourceComponentName);
                        }
                    }
                }
                else if (item is PivotDashboardItem pivot)
                {
                    var cols = pivot.Columns.OfType<DataItem>().ToList();
                    if (cols.Count > 0)
                        SetPivotLabelValues(pivot, cols, dataSourceComponentName);
                    var rows = pivot.Rows.OfType<DataItem>().ToList();
                    if (rows.Count > 0)
                        SetPivotLabelValues(pivot, rows, dataSourceComponentName);
                    var values = pivot.Values.OfType<DataItem>().ToList();
                    if (values.Count > 0)
                        SetPivotLabelValues(pivot, values, dataSourceComponentName);
                }
                else if (item is ChartDashboardItem chart)
                {
                    var dimensions = chart.GetDimensions().OfType<DataItem>().ToList();
                    if (dimensions.Count > 0)
                        SetChartLabelValues(chart, dimensions, dataSourceComponentName);
                    var measures = chart.GetMeasures().OfType<DataItem>().ToList();
                    if (measures.Count > 0)
                        SetChartLabelValues(chart, measures, dataSourceComponentName);
                }
                else if (item is PieDashboardItem pie)
                {
                    var dimensions = pie.GetDimensions().OfType<DataItem>().ToList();
                    if (dimensions.Count > 0)
                        SetPieLabelValues(pie, dimensions, dataSourceComponentName);
                    var measures = pie.GetMeasures().OfType<DataItem>().ToList();
                    if (measures.Count > 0)
                        SetPieLabelValues(pie, measures, dataSourceComponentName);
                }
                else if (item is ScatterChartDashboardItem sctChart)
                {
                    var measures = sctChart.GetMeasures().OfType<DataItem>().ToList();
                    if (measures.Count > 0)
                        SetScatterChartLabelValues(sctChart, measures, dataSourceComponentName);
                }
            }
        }

        /// <summary>
        /// DashboardLayout item mouse up event
        /// </summary>
        private void DashboardLayoutItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            DevExpress.DashboardWpf.DashboardLayoutItem item = sender as DevExpress.DashboardWpf.DashboardLayoutItem;
            FocusedItem = dashboardViewerUniconta.Dashboard.Items[item.Name];
            if (FocusedItem != null && (((DataDashboardItem)FocusedItem).DataSource) != null && dataSourceAndTypeMap.ContainsKey((((DataDashboardItem)FocusedItem).DataSource).ComponentName))
            {
                tableType = dataSourceAndTypeMap[(((DataDashboardItem)FocusedItem).DataSource).ComponentName];
                selectedDataSourceName = (((DataDashboardItem)FocusedItem).DataSource).ComponentName;
                var dataSourceName = (((DataDashboardItem)FocusedItem).DataSource).Name;
                int startindex = dataSourceName.IndexOf('(');
                if (startindex >= 0)
                {
                    int Endindex = dataSourceName.IndexOf(')');
                    string companyName = dataSourceName.Substring(startindex + 1, Endindex - startindex - 1);
                    companyName = string.Compare(companyName, company.KeyName) == 0 ? companyName : company.KeyName;
                    if (string.IsNullOrEmpty(titleText))
                        dashboardViewerUniconta.TitleContent = string.Format("{0} {1} ( {2} )", Uniconta.ClientTools.Localization.lookup("Company"), companyName, tableType.Name);
                }
                else
                {
                    if (string.IsNullOrEmpty(titleText))
                        dashboardViewerUniconta.TitleContent = string.Format("{0} {1} ( {2} )", Uniconta.ClientTools.Localization.lookup("Company"), company.KeyName, tableType.Name);
                }
            }
            else if (((DataDashboardItem)FocusedItem).DataSource is DashboardFederationDataSource)
            {
                var dataSource = ((DataDashboardItem)FocusedItem).DataSource;
                var queryTable = ((DevExpress.DataAccess.DataFederation.FederationDataSourceBase)dataSource)?.Queries?.FirstOrDefault();
                var sources = ((DevExpress.DataAccess.DataFederation.FederationDataSourceBase)(dataSource)).Sources;
                var name = StringBuilderReuse.Create();
                name.Append(_selectedDashBoard._Name).Append(" (");
                foreach (var source in sources)
                {
                    if (source.DataSource is DashboardObjectDataSource)
                    {
                        var componentName = ((DashboardObjectDataSource)(source.DataSource)).ComponentName;
                        name.Append(source.Name).Append("  ");
                        if (!dataSourceLoadingParams.Contains(componentName))
                            dataSourceLoadingParams.Add(componentName);

                        if (string.Equals(queryTable?.ToString(), source.Name))
                        {
                            selectedDataSourceName = componentName;
                            tableType = dataSourceAndTypeMap[selectedDataSourceName];
                        }
                    }
                }
                name.Append(')');
                //if (string.IsNullOrEmpty(titleText))
                //    dashboardViewerUniconta.TitleContent = name.ToStringAndRelease();
                if (!LoadOnOpen)          // load on open is saved as true. and first time on load user click on federation related dashboardItem 
                    LoadOnOpen = true;
                dashboardViewerUniconta.ReloadData();
            }
            else
            {
                tableType = null;
                selectedDataSourceName = null;
                if (string.IsNullOrEmpty(titleText))
                    dashboardViewerUniconta.TitleContent = string.Empty;
            }
            btnClearFilter.IsEnabled = btnFilter.IsEnabled = tableType != null && selectedDataSourceName != null ? true : false;
        }

        /// <summary>
        /// Timer Click
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!LoadOnOpen)
            {
                LoadOnOpen = true;
                var componentName = ((DashboardObjectDataSource)(dashboardViewerUniconta.Dashboard?.DataSources?.FirstOrDefault()))?.ComponentName;
                if (!string.IsNullOrEmpty(componentName) && !dataSourceLoadingParams.Contains(componentName))
                    dataSourceLoadingParams.Add(componentName);
            }
            dashboardViewerUniconta.ReloadData();
        }

        /// <summary>
        /// Dashboard viewer loaded event
        /// </summary>
        private void DashboardViewerUniconta_Loaded(object sender, RoutedEventArgs e)
        {
            if (!isDashBoardLaoded)
            {
                isDashBoardLaoded = true;
                Initialise();
            }

            if (isDashBoardLaoded && timer != null)
                timer.Start();
        }

        /// <summary>
        /// Data loading event
        /// </summary>
        private void dashboardViewerUniconta_AsyncDataLoading(object sender, DataLoadingEventArgs e)
        {
            try
            {
                var DataSourceComponentName = e.DataSourceComponentName;
                var dashboard = _dashboard?.DataSources?.FirstOrDefault(x => x.ComponentName == DataSourceComponentName);
                var fixedComp = fixedCompanies?.FirstOrDefault(x => x.DatasourceName == DataSourceComponentName);
                var compId = fixedComp != null ? fixedComp.CompanyId : this.company.CompanyId;
                var typeofTable = GetTypeAssociatedWithDashBoardDataSource(e.DataSourceName, DataSourceComponentName, compId);
                if (typeofTable == null && dataSourceAndTypeMap.ContainsKey(DataSourceComponentName))
                    typeofTable = dataSourceAndTypeMap[DataSourceComponentName];
                if (typeofTable != null)
                {
                    if (dashboard != null)
                    {
                        bool IsDashBoardTableTYpe = false;
                        Type dashbaordTableType = null;
                        CriteriaOperator.RegisterCustomFunction(new GetAppEnumIndexFunction(typeofTable));
                        if (!LoadOnOpen)
                            e.Data = Activator.CreateInstance(typeofTable);
                        else
                        {
                            if (dataSourceLoadingParams.Contains(DataSourceComponentName) && LoadOnOpen)
                            {
                                UnicontaBaseEntity[] data;
                                var dbUserflds = dashboardUserFields?.FirstOrDefault(x => x.Key == DataSourceComponentName).Value;  // Dashbaord user field list for particular datasource
                                if (dbUserflds != null)
                                {
                                    var t = DashboardCommon.GeneraterUserType(typeofTable, dbUserflds);
                                    if (t != null)
                                    {
                                        dashbaordTableType = t;
                                        IsDashBoardTableTYpe = true;
                                    }
                                }
                                var masterRecords = GetMasterRecordsAndFilters(typeofTable, out PropValuePair propValuePair);
                                var type = !IsDashBoardTableTYpe ? typeofTable : dashbaordTableType;

                                if (lstOfNewFilters.ContainsKey(DataSourceComponentName))
                                {
                                    var lst = lstOfNewFilters[DataSourceComponentName];
                                    if (lst != null)
                                    {
                                        foreach (var f in lst)
                                        {
                                            if (f.ParameterType == null)
                                                f.ParameterType = type?.GetProperty(f.PropertyName)?.PropertyType;
                                        }
                                    }
                                    filterValues = UtilDisplay.GetPropValuePair(lstOfNewFilters[DataSourceComponentName]);
                                }
                                else
                                    filterValues = null;

                                if (lstOfSorters.ContainsKey(DataSourceComponentName))
                                    PropSort = lstOfSorters[DataSourceComponentName];
                                else
                                    PropSort = null;

                                if (propValuePair != null)
                                {
                                    var propLst = new List<PropValuePair>(1) { propValuePair };
                                    if (filterValues != null)
                                        filterValues.Union(propLst);
                                    else
                                        filterValues = propLst;
                                }

                                CrudAPI compApi = null;
                                if (fixedComp == null || this.company.CompanyId == fixedComp.CompanyId)
                                {
                                    if (type == typeof(CompanyClient))
                                        data = LoadCurrentUserCompanies(api);
                                    else if (type == typeof(UserClient))
                                        data = LoadCurrentCompanyUser(api);
                                    else
                                        data = Query(type, api, masterRecords, filterValues).GetAwaiter().GetResult();
                                }
                                else
                                {
                                    var comp = CWDefaultCompany.loadedCompanies.FirstOrDefault(x => x.CompanyId == fixedComp.CompanyId);
                                    compApi = new CrudAPI(BasePage.session, comp);
                                    data = Query(type, compApi, masterRecords, filterValues).GetAwaiter().GetResult();
                                }

                                if (lstOfDataSources != null && !lstOfDataSources.ContainsKey(DataSourceComponentName))
                                    lstOfDataSources.Add(DataSourceComponentName, data);

                                if (typeofTable.Equals(typeof(CompanyDocumentClient)))
                                    ReadData(data, compApi ?? api).GetAwaiter().GetResult(); // if there is image in property it will load again;

                                if (HasNestedField.TryGetValue(DataSourceComponentName, out bool nestField))
                                    e.Data = new DashBoardView.DisplayNameProviderTypedListWrapper<DataView>(UtilFunctions.BuildDataTable(data, type, (compApi ?? api).CompanyEntity).DefaultView, new DashBoardView.DisplayNameProviderStub());
                                else
                                    e.Data = data;
                            }
                            else
                                e.Data = Activator.CreateInstance(typeofTable);
                        }
                    }
                    if (TableContainLoginIdProp(typeofTable))
                    {
                        var filter = "[" + UserIdPropName + "] =" + "'" + BasePage.session.LoginId + "'";
                        dashboard.Filter = filter;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }

        /// <summary>
        /// Dashbaord viewer unloaded event
        /// </summary>
        private void DashboardViewerUniconta_Unloaded(object sender, RoutedEventArgs e)
        {
            if (isDashBoardLaoded && timer != null)
                timer.Stop();
        }

        /// <summary>
        /// Filter click event
        /// </summary>
        private void btnFilter_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(selectedDataSourceName) && tableType != null)
                {
                    List<FilterProperties> filterProps;
                    if (lstOfNewFilters.ContainsKey(selectedDataSourceName))
                        filterProps = lstOfNewFilters[selectedDataSourceName];
                    else
                        filterProps = null;

                    if (lstOfSorters.ContainsKey(selectedDataSourceName))
                        PropSort = lstOfSorters[selectedDataSourceName];
                    else
                        PropSort = null;

                    var fixedComp = fixedCompanies?.FirstOrDefault(x => x.DatasourceName == selectedDataSourceName);
                    if (fixedComp == null || this.company.CompanyId == fixedComp?.CompanyId)
                        filterDialog = new CWServerFilter(api, tableType, filterProps?.Select(p => new Filter() { name = p.PropertyName, value = p.UserInput, parameterType = p.ParameterType }).ToArray(),
                            Utility.CreateDefaultSort(PropSort), null);
                    else
                    {
                        var comp = CWDefaultCompany.loadedCompanies.FirstOrDefault(x => x.CompanyId == fixedComp.CompanyId);
                        var compApi = new CrudAPI(api.session, comp);
                        filterDialog = new CWServerFilter(compApi, tableType, filterProps.Select(p => new Filter() { name = p.PropertyName, value = p.UserInput, parameterType = p.ParameterType }).ToArray(),
                            Utility.CreateDefaultSort(PropSort), null);
                    }
                    filterDialog.GridSource = lstOfDataSources[selectedDataSourceName];
                    filterDialog.Closing += FilterDialog_Closing;
                    filterDialog.Show();
                }
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
        }

        /// <summary>
        /// Filter dialog closing event
        /// </summary>
        private void FilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (filterDialog.DialogResult == true)
            {
                var filtersProps = filterDialog.Filters?.Select(p => new FilterProperties() { PropertyName = p.name, UserInput = p.value, ParameterType = p.parameterType });
                filterValues = filterDialog.PropValuePair;
                PropSort = filterDialog.PropSort;

                if (lstOfNewFilters.ContainsKey(selectedDataSourceName))
                {
                    if (filtersProps == null || filtersProps.Count() == 0)
                        lstOfNewFilters.Remove(selectedDataSourceName);
                    else
                        lstOfNewFilters[selectedDataSourceName] = filtersProps.ToList();
                }
                else
                {
                    if (filtersProps != null && filtersProps.Count() > 0)
                        lstOfNewFilters.Add(selectedDataSourceName, filtersProps.ToList());
                }

                if (lstOfSorters.ContainsKey(selectedDataSourceName))
                {
                    if (PropSort == null || PropSort.sortingProperties.Count() == 0)
                        lstOfSorters.Remove(selectedDataSourceName);
                    else
                        lstOfSorters[selectedDataSourceName] = PropSort;
                }
                else
                {
                    if (PropSort != null && PropSort.sortingProperties.Count() > 0)
                        lstOfSorters.Add(selectedDataSourceName, PropSort);
                }
                LoadOnOpen = true;
                DataDashboardItem dataItem = FocusedItem as DataDashboardItem;
                if (dataItem == null) return;
                string dsComponentName = dataItem.DataSource.ComponentName;
                if (!dataSourceLoadingParams.Contains(dsComponentName))
                    dataSourceLoadingParams.Add(dsComponentName);
                dashboardViewerUniconta.ReloadData();
            }
            e.Cancel = true;
            filterDialog.Hide();
        }

        /// <summary>
        /// Fitler clear event
        /// </summary>
        private void btnClearFilter_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDataSourceName))
            {
                if (lstOfSorters.ContainsKey(selectedDataSourceName))
                    lstOfSorters.Remove(selectedDataSourceName);
                if (lstOfNewFilters.ContainsKey(selectedDataSourceName))
                    lstOfNewFilters.Remove(selectedDataSourceName);
                dashboardViewerUniconta.ReloadData();
            }
        }

        /// <summary>
        /// Dashbaord refresh event
        /// </summary>
        private void btnRefresh_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            dashboardViewerUniconta.ReloadData();
        }

        /// <summary>
        /// Settings event for the page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnWinSettg_Click(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
            string name = this.ParentControl != null ? Convert.ToString(this.ParentControl.Tag) : NameOfControl;
            var dockSettingDialog = new CWControlDockSetting(name);
            dockSettingDialog.Show();
        }

        /// <summary>
        /// Dashboard changed event
        /// </summary>
        private void DashboardViewerUniconta_DashboardChanged(object sender, EventArgs e)
        {
            _dashboard = dashboardViewerUniconta.Dashboard;
        }
        #endregion

        #region Methods

        /// <summary>
        /// Setting header
        /// </summary>
        void SetHeader(UnicontaBaseEntity row)
        {
            var keystr = (row as IdKeyName)?.KeyStr;
            string header = string.Empty;
            if (string.IsNullOrEmpty(keystr))
                header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Dashboard"), dashboardName);
            else
                header = string.Format("{0}:{1}/{2} ", Uniconta.ClientTools.Localization.lookup("Dashboard"), dashboardName, keystr);
            SetHeader(header);
        }

        /// <summary>
        /// Initializing the page
        /// </summary>
        void InitPage()
        {
            dState = new DashboardState();
            company = api.CompanyEntity;
            CriteriaOperator.RegisterCustomFunction(new DashBoardView.ExchangeRateFunction(new CrudAPI(api)));
            InitializeComponent();
            this.BusyIndicator = busyIndicator;
            dashboardViewerUniconta.ObjectDataSourceLoadingBehavior = DevExpress.DataAccess.DocumentLoadingBehavior.LoadAsIs;
            dataSourceAndTypeMap = new Dictionary<string, Type>();
            lstOfNewFilters = new Dictionary<string, List<FilterProperties>>();
            lstOfSorters = new Dictionary<string, FilterSorter>();
            ListOfReportTableTypes = new Dictionary<int, Type[]>();
            lstOfDataSources = new Dictionary<string, UnicontaBaseEntity[]>();
            LoadListOfTableTypes(company);
            dashboardViewerUniconta.Loaded += DashboardViewerUniconta_Loaded;
            dashboardViewerUniconta.DashboardLoaded += DashboardViewerUniconta_DashboardLoaded;
            dashboardViewerUniconta.SetInitialDashboardState += DashboardViewerUniconta_SetInitialDashboardState;
            dashboardViewerUniconta.DashboardChanged += DashboardViewerUniconta_DashboardChanged;
            dashboardViewerUniconta.DataLoadingError += DashboardViewerUniconta_DataLoadingError;
            dashboardViewerUniconta.Unloaded += DashboardViewerUniconta_Unloaded;
        }

        /// <summary>
        /// Set Lables on Pivot dashboard item
        /// </summary>
        void SetPivotLabelValues(PivotDashboardItem pivot, List<DataItem> targetLst, string dataSourceName)
        {
            foreach (var col in targetLst)
            {
                var name = col.Name;
                var savedName = pivot.CustomProperties.GetValue(col.UniqueId) ?? string.Empty;

                if ((name != null || !string.IsNullOrEmpty(name)) && (col.Name.StartsWith("&") || col.Name.StartsWith("@") || savedName.StartsWith("@") || savedName.StartsWith("&")))
                {
                    name = savedName;
                    col.Name = GetCustomLocalizedString(name.Substring(1));
                }
                IsNestedField(col, dataSourceName);
            }
        }

        /// <summary>
        /// Set labels on Chart dashboard item
        /// </summary>
        void SetChartLabelValues(ChartDashboardItem chart, List<DataItem> targetLst, string dataSourceName)
        {
            foreach (var col in targetLst)
            {
                var name = col.Name;
                var savedName = chart.CustomProperties.GetValue(col.UniqueId) ?? string.Empty;

                if ((name != null || !string.IsNullOrEmpty(name)) && (col.Name.StartsWith("&") || col.Name.StartsWith("@") || savedName.StartsWith("@") || savedName.StartsWith("&")))
                {
                    name = savedName;
                    col.Name = GetCustomLocalizedString(name.Substring(1));
                }
            }
        }

        /// <summary>
        /// Is NestedField DataItem
        /// </summary>
        private void IsNestedField(DataItem col, string dataSourceName)
        {
            if (string.IsNullOrEmpty(dataSourceName))
                return;
            if (col?.DataMember?.Contains(".") == true && !HasNestedField?.ContainsKey(dataSourceName) == true)
                HasNestedField.Add(dataSourceName, true);
        }

        /// <summary>
        /// Set labels on Pie dashboardItem
        /// </summary>
        void SetPieLabelValues(PieDashboardItem pie, List<DataItem> targetLst, string dataSourceName)
        {
            foreach (var col in targetLst)
            {
                var name = col.Name;
                var savedName = pie.CustomProperties.GetValue(col.UniqueId) ?? string.Empty;

                if ((name != null || !string.IsNullOrEmpty(name)) && (col.Name.StartsWith("&") || col.Name.StartsWith("@") || savedName.StartsWith("@") || savedName.StartsWith("&")))
                {
                    name = savedName;
                    col.Name = GetCustomLocalizedString(name.Substring(1));
                }
                IsNestedField(col, dataSourceName);
            }
        }

        /// <summary>
        /// Set label on ScatterChart labels
        /// </summary>
        void SetScatterChartLabelValues(ScatterChartDashboardItem sctChart, List<DataItem> targetLst, string dataSourceName)
        {
            foreach (var col in targetLst)
            {
                var name = col.Name;
                var savedName = sctChart.CustomProperties.GetValue(col.UniqueId) ?? string.Empty;

                if ((name != null || !string.IsNullOrEmpty(name)) && (col.Name.StartsWith("&") || col.Name.StartsWith("@") || savedName.StartsWith("@") || savedName.StartsWith("&")))
                {
                    name = savedName;
                    col.Name = GetCustomLocalizedString(name.Substring(1));
                }
                IsNestedField(col, dataSourceName);
            }
        }

        /// <summary>
        /// Datasource componenent name
        /// </summary>
        private string GetDataSourceComponentName(DashboardItem item)
        {
            try
            {
                var dataSource = item.GetType().GetProperty(DashboardCommon.PROPERTY_DATASOURCE)?.GetValue(item, null);
                var dataSourceComponentName = dataSource?.GetType().GetProperty(DashboardCommon.PROPERTY_COMPONENT_NAME)?.GetValue(dataSource, null) as string;
                return dataSourceComponentName;
            }
            catch { return null; }
        }

        /// <summary>
        /// Get PropValue pair  list from the datasource selected
        /// </summary>
        private List<PropValuePair> GetPropValuePairForDataSource(Type TableType, List<FilterProperties> filterProps)
        {
            List<PropValuePair> propPairLst = new List<PropValuePair>();
            Filter[] filters = filterProps.Select(p => new Filter() { name = p.PropertyName, value = p.UserInput, parameterType = p.ParameterType }).ToArray();
            var filterSorthelper = new FilterSortHelper(TableType, filters, null);
            List<string> errors;
            propPairLst = filterSorthelper.GetPropValuePair(out errors);
            return propPairLst;
        }

        /// <summary>
        /// Initialized the dashbaord
        /// </summary>
        private async void Initialise()
        {
            busyIndicator.IsBusy = true;
            if (_selectedDashBoard == null)
            {
                int rowId;
                int.TryParse(dashboardName, out rowId);
                var dbrdLst = await api.Query<DashboardClient>() as IEnumerable<UserReportDevExpressClient>;
                if (rowId != 0)
                    _selectedDashBoard = dbrdLst?.FirstOrDefault(x => x.RowId == rowId) as DashboardClient;
                else
                    _selectedDashBoard = dbrdLst?.FirstOrDefault(x => string.Compare(x._Name, dashboardName, StringComparison.CurrentCultureIgnoreCase) == 0) as DashboardClient;
            }
            if (_selectedDashBoard != null)
            {
                var result = await api.Read(_selectedDashBoard);
                if (result == ErrorCodes.Succes)
                {
                    if (_selectedDashBoard.Layout != null)
                    {
                        DevExpress.Utils.DeserializationSettings.InvokeTrusted(() =>
                        {
                            ReadDataFromDB(_selectedDashBoard.Layout);
                        });
                    }
                    dashboardViewerUniconta.TitleContent = !string.IsNullOrEmpty(titleText) ? titleText : _selectedDashBoard.Name;
                }
                else
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(result.ToString()), Uniconta.ClientTools.Localization.lookup("Error"));
                    CloseDockItem();
                }
            }

            if (_selectedDashBoard == null)
                busyIndicator.IsBusy = false;
        }

        /// <summary>
        /// Read dashbaord as bytes
        /// </summary>
        private bool ReadDataFromDB(byte[] selectedDashBoardBinary)
        {
            busyIndicator.IsBusy = true;
            bool retVal = true;
            try
            {
                lstOfNewFilters.Clear();
                var customReader = StreamingManagerReuse.Create(selectedDashBoardBinary);
                var version = customReader.readByte();
                if (version < 1 || version > 3)
                    return false;

                var bufferedReport = StreamingManager.readMemory(customReader);
                var st = Compression.Uncompress(bufferedReport);

                if (customReader.readBoolean())
                {
                    int filterCount = (int)customReader.readNum();
                    if (version < 3)
                    {
                        for (int i = 0; i < filterCount; i++)
                        {
                            var key = customReader.readString();
                            List<FilterProperties> arrFilter;
                            SortingProperties[] sortProps;
                            FilterSortHelper.ConvertPropValuePair((PropValuePair[])customReader.ToArray(typeof(PropValuePair)), out arrFilter, out sortProps);
                            lstOfNewFilters.Add(key, arrFilter);
                        }
                    }
                    else if (version == 3)
                    {
                        for (int i = 0; i < filterCount; i++)
                        {
                            var key = customReader.readString();
                            var arrFilter = (FilterProperties[])customReader.ToArray(typeof(FilterProperties));
                            lstOfNewFilters.Add(key, arrFilter.ToList());
                        }
                    }
                }
                if (customReader.readBoolean())
                {
                    int sortCount = (int)customReader.readNum();
                    for (int i = 0; i < sortCount; i++)
                    {
                        var key = customReader.readString();
                        var arrSort = (SortingProperties[])customReader.ToArray(typeof(SortingProperties));
                        FilterSorter propSort = new FilterSorter(arrSort);
                        if (lstOfSorters != null && !lstOfSorters.ContainsKey(key))
                            lstOfSorters.Add(key, propSort);
                    }
                }
                customReader.Release();

                XDocument xdoc = XDocument.Load(st);
                var element = xdoc.Root.Attributes().Where(x => x.Name == "RefreshTimer").FirstOrDefault();
                if (element != null)
                {
                    int refreshTimer = (int)NumberConvert.ToInt(element.Value);
                    if (refreshTimer > 0)
                    {
                        if (timer == null)
                            timer = new System.Windows.Forms.Timer();
                        timer.Interval = refreshTimer < 300 ? 300 * 1000 : refreshTimer * 1000;
                        timer.Tick += Timer_Tick;
                        timer.Start();
                    }
                }
                SetTimer();
                st.Seek(0, System.IO.SeekOrigin.Begin);
                dashboardViewerUniconta.LoadDashboard(st);
                st.Release();
                return retVal;
            }
            catch (Exception ex)
            {
                ClearBusy();
                UnicontaMessageBox.Show(ex);
                CloseDockItem();
                return false;
            }
        }

        /// <summary>
        /// Set timer for the dashbaord
        /// </summary>
        void SetTimer()
        {
            DispatcherTimer timer = new DispatcherTimer();
            EventHandler tick = null;
            tick = (o, eventArgs) =>
            {
                if (dashboardViewerUniconta.DashboardViewModel != null && !dashboardViewerUniconta.DashboardViewModel.IsLoaded)
                    return;
                ClearBusy();
                timer.Tick -= tick;
                timer.Stop();
            };
            timer.Tick += tick;
            timer.Start();
        }

        /// <summary>
        /// Method to query data for the dashboard data source
        /// </summary>
        /// <param name="type"></param>
        /// <param name="crudApi"></param>
        /// <param name="masters"></param>
        /// <param name="filterValue"></param>
        /// <returns></returns>
        private Task<UnicontaBaseEntity[]> Query(Type type, CrudAPI crudApi, IEnumerable<UnicontaBaseEntity> masters, IEnumerable<PropValuePair> filterValue)
        {
            return Task.Run(async () =>
            {
                //When the Masters and type is same then if we query it will return all the result.
                //So the master is the source itself and we dont query.
                //We just have create an array of the type and it will work.
                if (masters != null && masters.Count() == 1 && masters.First().GetType() == type)
                {
                    var masterEntity = (UnicontaBaseEntity[])Array.CreateInstance(type, 1);
                    var rec = Activator.CreateInstance(type) as UnicontaBaseEntity;
                    StreamingManager.Copy(masters.First(), rec);
                    masterEntity[0] = rec;
                    return masterEntity;
                }

                var data = await crudApi.Query(Activator.CreateInstance(type) as UnicontaBaseEntity, masters, filterValue);

                if (PropSort != null)
                    Array.Sort(data, PropSort);

                return data;
            });
        }

        /// <summary>
        /// Getting current user
        /// </summary>
        /// <param name="crudApi">Api instance</param>
        /// <returns>List of Users</returns>
        private UnicontaBaseEntity[] LoadCurrentCompanyUser(CrudAPI crudApi)
        {
            var currentUser = session.User;
            var userClient = crudApi.CompanyEntity.CreateUserType<UserClient>();
            StreamingManager.Copy(currentUser, userClient);

            return new[] { userClient };
        }

        /// <summary>
        /// Loads Current user companies
        /// </summary>
        private UnicontaBaseEntity[] LoadCurrentUserCompanies(CrudAPI crudApi)
        {
            var currentUserSessionCompanies = CWDefaultCompany.loadedCompanies;

            if (currentUserSessionCompanies == null || currentUserSessionCompanies.Length == 0)
            {
                var cmpUser = crudApi.CompanyEntity.CreateUserType<CompanyClient>();
                if (crudApi.CompanyEntity != null)
                    StreamingManager.Copy(crudApi.CompanyEntity, cmpUser);

                return new CompanyClient[] { cmpUser };
            }

            var companies = new CompanyClient[currentUserSessionCompanies.Length];

            for (int index = 0; index < currentUserSessionCompanies.Length; index++)
            {
                var comp = currentUserSessionCompanies[index];
                var cmpUser = crudApi.CompanyEntity.CreateUserType<CompanyClient>();

                if (comp.CompanyId == crudApi.CompanyEntity.CompanyId)
                    StreamingManager.Copy(crudApi.CompanyEntity, cmpUser);
                else
                    StreamingManager.Copy(comp, cmpUser);

                companies[index] = cmpUser;
            }
            return companies;
        }

        /// <summary>
        /// Method to read uniconta base entity
        /// </summary>
        private Task ReadData(UnicontaBaseEntity[] data, CrudAPI api)
        {
            return Task.Run(async () =>
            {
                bool found = false;
                if (data.Length > 0)
                {
                    foreach (PropertyInfo propertyInfo in data[0].GetType().GetProperties())
                    {
                        if (propertyInfo.PropertyType == typeof(byte[]))
                        {
                            found = true;
                            break;
                        }
                    }

                }
                if (found)
                {
                    foreach (UnicontaBaseEntity row in data)
                    {
                        await api.Read(row);
                    }
                }
            });
        }

        /// <summary>
        /// Method to get the master records and Propvalue pair
        /// </summary>
        List<UnicontaBaseEntity> GetMasterRecordsAndFilters(Type TableTYpe, out PropValuePair propValuePair)
        {
            propValuePair = null;

            if (master == null)
                return null;

            if (masterField != null)
            {
                PropertyInfo propInfo = TableTYpe.GetProperty(masterField);

                if (propInfo != null)
                {
                    var filterValue = master.GetType().GetProperty(masterField).GetValue(master, null);

                    if (filterValue != null)
                        propValuePair = PropValuePair.GenereteWhereElements(masterField, propInfo.PropertyType, filterValue.ToString());
                }
            }

            return new List<UnicontaBaseEntity>(1) { master };
        }

        /// <summary>
        /// Method to check login id property
        /// </summary>

        bool TableContainLoginIdProp(Type retType)
        {
            bool exist = false;
            if (!string.IsNullOrEmpty(UserIdPropName))
            {
                var prop = retType.GetProperty(UserIdPropName);
                if (prop != null)
                    exist = true;
            }
            return exist;
        }

        /// <summary>
        /// Method to get the associated type on the dashbaord datasource
        /// </summary>
        /// <param name="dataSource"></param>
        /// <param name="componentName"></param>
        /// <param name="companyId"></param>
        /// <returns></returns>
        private Type GetTypeAssociatedWithDashBoardDataSource(string dataSource, string componentName, int companyId)
        {
            Type retType = null;
            var ds = _dashboard?.DataSources?.Where(d => d.ComponentName == componentName).FirstOrDefault();
            if (ds != null)
            {
                var data = ds.Data;
                string tblType = null;
                PropertyInfo propInfo = null;
                string fullname = null;
                if (data != null)
                {
                    propInfo = ds.Data.GetType().GetProperty("FullName");
                    if (propInfo != null)
                        fullname = propInfo.GetValue(ds.Data, null) as string;
                    if (fullname == null)
                    {
                        var li = ds.Name.IndexOf('(');
                        tblType = li >= 0 ? ds.Name.Substring(0, li) : ds.Name;
                        fullname = string.Concat("Uniconta.ClientTools.DataModel.", tblType);
                    }
                }
                else
                {
                    var li = ds.Name.IndexOf('(');
                    tblType = li >= 0 ? ds.Name.Substring(0, li) : ds.Name;
                    fullname = string.Concat("Uniconta.ClientTools.DataModel.", tblType);
                }

                if (fullname != null)
                {
                    fullname = fullname.Replace("[]", "");
                    var tables = ListOfReportTableTypes.FirstOrDefault(x => x.Key == companyId).Value;
                    retType = tables.Where(p => p.FullName == fullname).FirstOrDefault();
                    if (retType == null)
                        retType = tables.Where(p => p.FullName == dataSource).FirstOrDefault();
                    if (retType == null)
                        retType = tables.Where(p => p.FullName == tblType).FirstOrDefault();
                    if (retType != null)
                    {
                        if (!dataSourceAndTypeMap.ContainsKey(componentName))
                        {
                            dataSourceAndTypeMap.Add(componentName, retType);
                            tableType = retType;
                        }
                    }
                }
            }
            return retType;
        }

        /// <summary>
        /// List of table types in the company
        /// </summary>
        void LoadListOfTableTypes(Company company)
        {
            if (company != null)
            {
                var list = Global.GetTables(company);
                var userTables = Global.GetUserTables(company);
                if (userTables != null)
                    list.AddRange(userTables);
                if (!ListOfReportTableTypes.ContainsKey(company.CompanyId))
                    ListOfReportTableTypes.Add(company.CompanyId, list.ToArray());
            }
        }

        /// <summary>
        /// Open a fixed company
        /// </summary>
        /// <param name="comp"></param>
        /// <returns></returns>

        Task<Company> OpenFixedCompany(Company comp)
        {
            return Task.Run(async () =>
            {
                return BasePage.session.GetOpenCompany(comp.CompanyId) ?? await BasePage.session.OpenCompany(comp.CompanyId, false, comp);
            });
        }


        /// <summary>
        /// Method to get the custom localized string 
        /// </summary>
        /// <param name="keyString">Key for the string</param>
        /// <returns>Localized value</returns>
        private string GetCustomLocalizedString(string keyString)
        {
            if (dashboardLocalizationLabels != null)
            {
                var lang = (Language)session.User._Language;
                if (dashboardLocalizationLabels.ContainsKey(keyString, lang))
                    return dashboardLocalizationLabels[keyString][lang];
            }
            return Uniconta.ClientTools.Localization.lookup(keyString);
        }
        #endregion
    }
}
