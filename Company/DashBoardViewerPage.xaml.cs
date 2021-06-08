using UnicontaClient.Models;
using DevExpress.DashboardCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
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
using System.Text.RegularExpressions;
using DevExpress.Mvvm.UI.Interactivity;
using DevExpress.Xpf.PivotGrid;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DashBoardViewerPage : ControlBasePage
    {
        public override string NameOfControl { get { return TabControls.DashBoardViewerPage; } }
        Type tableType;
        DashboardClient _selectedDashBoard;
        Dictionary<string, Type> dataSourceAndTypeMap;
        CWServerFilter filterDialog;
        public FilterSorter PropSort;
        private Dictionary<string, IEnumerable<PropValuePair>> lstOfFilters;
        private Dictionary<string, List<FilterProperties>> lstOfNewFilters;
        private Dictionary<string, FilterSorter> lstOfSorters;
        private Dictionary<int, Type[]> ListOfReportTableTypes;
        public IEnumerable<PropValuePair> filterValues;
        string selectedDataSourceName;
        Company company;
        DashboardState dState;
        bool LoadOnOpen;
        List<FixedCompany> fixedCompanies;
        string dashboardName;
        System.Windows.Forms.Timer timer;
        string UserIdPropName;

        public DashBoardViewerPage(UnicontaBaseEntity dashBoard) : base(dashBoard)
        {
            _selectedDashBoard = dashBoard as DashboardClient;
            InitPage();
        }

        public DashBoardViewerPage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage();
        }
       
        void InitPage()
        {
            dState = new DashboardState();
            company = api.CompanyEntity;
            InitializeComponent();
            this.BusyIndicator = busyIndicator;
            dashboardViewerUniconta.ObjectDataSourceLoadingBehavior = DevExpress.DataAccess.DocumentLoadingBehavior.LoadAsIs;
            dataSourceAndTypeMap = new Dictionary<string, Type>();
            lstOfFilters = new Dictionary<string, IEnumerable<PropValuePair>>();
            lstOfNewFilters = new Dictionary<string, List<FilterProperties>>();
            lstOfSorters = new Dictionary<string, FilterSorter>();
            ListOfReportTableTypes = new Dictionary<int, Type[]>();
            LoadListOfTableTypes(company);
            dashboardViewerUniconta.Loaded += DashboardViewerUniconta_Loaded;
            dashboardViewerUniconta.DashboardLoaded += DashboardViewerUniconta_DashboardLoaded;
            dashboardViewerUniconta.SetInitialDashboardState += DashboardViewerUniconta_SetInitialDashboardState;
            dashboardViewerUniconta.DashboardChanged += DashboardViewerUniconta_DashboardChanged;
            dashboardViewerUniconta.DataLoadingError += DashboardViewerUniconta_DataLoadingError;
            dashboardViewerUniconta.Unloaded += DashboardViewerUniconta_Unloaded;
        }

        private void DashboardViewerUniconta_DataLoadingError(object sender, DataLoadingErrorEventArgs e)
        {
            e.Handled = true;
        }

        public override void PageClosing() 
        {
            if (timer != null)
                timer.Tick -= Timer_Tick;
            base.PageClosing();
        }

        public override void SetParameter(IEnumerable<ValuePair> Parameters)
        {
            foreach (var rec in Parameters)
            {
                if (rec.Name == null || string.Compare(rec.Name, "Dashboard", StringComparison.CurrentCultureIgnoreCase) == 0)
                {
                    dashboardName = rec.Value;
                    SetHeader( string.Concat(Uniconta.ClientTools.Localization.lookup("Dashboard"), ": ", dashboardName) );
                }
            }
            base.SetParameter(Parameters);
        }

        private void DashboardViewerUniconta_SetInitialDashboardState(object sender, DevExpress.DashboardWpf.SetInitialDashboardStateWpfEventArgs e)
        {
            e.InitialState = dState;
        }

        private void DashboardViewerUniconta_DashboardLoaded(object sender, DevExpress.DashboardWpf.DashboardLoadedEventArgs e)
        {
            Dashboard dasdboard = e.Dashboard;
            foreach (var item in dasdboard?.Items)
            {
                var name = dasdboard?.CustomProperties.GetValue(item.ComponentName);
                if (name != null)
                {
                    if (name[0] == '&')
                        item.Name = Uniconta.ClientTools.Localization.lookup(name.Substring(1)); ;
                }
            }

            if (!dasdboard.Title.ShowMasterFilterState)
                rowFilter.Visibility = Visibility.Collapsed;

            XElement data = e.Dashboard.UserData;
            if (data != null)
            {
                var state = data.Element("DashboardState");
                if (state != null)
                    dState.LoadFromXml(XDocument.Parse(state.Value));

                var fixedComps = data.Element("FixedCompanies");
                if (fixedComps != null)
                {
                    fixedCompanies = null;
                    var rootelement = XElement.Parse(fixedComps.ToString());
                    foreach (var el in rootelement.Elements())
                    {
                        var fxdComps = XDocument.Parse(el.ToString()).Elements("FixedCompany")
                             .Select(p => new FixedCompany
                             {
                                 CompanyId = int.Parse(p.Element("CompanyId").Value),
                                 DatasourceName = p.Element("DatasourceName").Value
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
                            {
                                openComp.GenerateUserType();
                                LoadListOfTableTypes(openComp);
                            }
                            else
                                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("UserNoAccessToCompany"), Uniconta.ClientTools.Localization.lookup("Information"));
                        }
                    }
                }

                var ldOnOpn = data.Element("LoadOnOpen");
                if (ldOnOpn != null)
                    LoadOnOpen = bool.Parse(ldOnOpn.Value);

                var userId = data.Element("LogedInUserIdFilter");
                if (userId != null)
                    UserIdPropName = userId.Value;

                var filters = data.Element("Filters");
                if (filters != null)
                {
                    var filtersBytes = Convert.FromBase64String(filters.Value);
                    var r = StreamingManagerReuse.Create(filtersBytes);
                    int version = r.readByte();
                    if (version != 0)
                    {
                        if (version < 3 && r.readBoolean())
                        {
                            int filterCount = (int)StreamingManager.readNum(r);
                            for (int i = 0; i < filterCount; i++)
                            {
                                var key = r.readString();
                                List<PropValuePair> propVal = new List<PropValuePair>();
                                var arrpropval = (PropValuePair[])r.ToArray(typeof(PropValuePair));
                                propVal = arrpropval.ToList();
                                lstOfFilters.Add(key, arrpropval);
                            }
                        }
                        else
                        {
                            int filterCount = (int)StreamingManager.readNum(r);
                            for (int i = 0; i < filterCount; i++)
                            {
                                var key = r.readString();
                                var arrFilter = (FilterProperties[])r.ToArray(typeof(FilterProperties));
                                lstOfNewFilters.Add(key, arrFilter.ToList());
                            }
                        }
                    }
                    if (r.readBoolean())
                    {
                        if (version < 3)
                        {
                            int sortCount = (int)r.readNum();
                            for (int i = 0; i < sortCount; i++)
                            {
                                var key = r.readString();
                                var arrSort = (SortingProperties[])r.ToArray(typeof(SortingProperties));
                                FilterSorter propSort = new FilterSorter(arrSort);
                                lstOfSorters.Add(key, propSort);
                            }
                        }
                    }
                    r.Release();
                }
            }
            else
                LoadOnOpen = true;  // for old saved dashboards
            
            if (LoadOnOpen)
                foreach (var ds in e.Dashboard.DataSources)
                    dataSourceLoadingParams.Add(ds.ComponentName);
        }

        void UpdateServerFilter(string datasourceName, Type TableType)
        {
            if (lstOfNewFilters != null && lstOfNewFilters.Count() > 0)
            {
                if (!lstOfFilters.ContainsKey(datasourceName) && lstOfNewFilters.ContainsKey(datasourceName))
                {
                    var filterProp = lstOfNewFilters[datasourceName];
                    lstOfFilters.Add(datasourceName, GetPropValuePairForDataSource(TableType, filterProp));
                }
            }

            if (lstOfFilters.ContainsKey(datasourceName) && !lstOfNewFilters.ContainsKey(datasourceName))
            {
                var filters = Utility.CreateDefaultFilter(lstOfFilters[datasourceName], TableType);
                var filtersProps = filters?.Select(p => new FilterProperties() { PropertyName = p.name, UserInput = p.value, ParameterType = p.parameterType });
                if (filtersProps != null)
                    lstOfNewFilters.Add(datasourceName, filtersProps.ToList());
            }
        }

        private List<PropValuePair> GetPropValuePairForDataSource(Type TableType, List<FilterProperties> filterProps)
        {
            List<PropValuePair> propPairLst = new List<PropValuePair>();
            Filter[] filters = filterProps.Select(p => new Filter() { name = p.PropertyName, value = p.UserInput, parameterType = p.ParameterType }).ToArray();
            var filterSorthelper = new FilterSortHelper(TableType, filters, null);
            List<string> errors;
            propPairLst = filterSorthelper.GetPropValuePair(out errors);
            return propPairLst;
        }

        DashboardItem FocusedItem;
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
                    dashboardViewerUniconta.TitleContent = string.Format("{0} {1} ( {2} )", Uniconta.ClientTools.Localization.lookup("Company"), companyName, tableType.Name);
                }
                else
                    dashboardViewerUniconta.TitleContent = string.Format("{0} {1} ( {2} )", Uniconta.ClientTools.Localization.lookup("Company"), company.KeyName, tableType.Name);
            }
            else if (((DataDashboardItem)FocusedItem).DataSource is DashboardFederationDataSource)
            {
                var dataSource = ((DataDashboardItem)FocusedItem).DataSource;
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
                    }
                }
                name.Append(')');
                dashboardViewerUniconta.TitleContent = name.ToStringAndRelease();
                if (!LoadOnOpen)          // load on open is saved as true. and first time on load user click on federation related dashboardItem 
                    LoadOnOpen = true;
                dashboardViewerUniconta.ReloadData();
            }
            else
            {
                tableType = null;
                selectedDataSourceName = null;
                dashboardViewerUniconta.TitleContent = string.Empty;
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            dashboardViewerUniconta.ReloadData();
        }

        bool isDashBoardLaoded;
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

        private void DashboardViewerUniconta_Unloaded(object sender, RoutedEventArgs e)
        {
            if (isDashBoardLaoded && timer != null)
                timer.Stop();
        }

        private async Task<bool> Initialise()
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
                        ReadDataFromDB(_selectedDashBoard.Layout);
                    dashboardViewerUniconta.TitleContent = _selectedDashBoard.Name;
                }
                else
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(result.ToString()), Uniconta.ClientTools.Localization.lookup("Error"));
            }

            if (_selectedDashBoard == null)
                busyIndicator.IsBusy = false;
            return true;
        }

        private bool ReadDataFromDB(byte[] selectedDashBoardBinary)
        {
            busyIndicator.IsBusy = true;
            bool retVal = true;
            try
            {
                lstOfFilters.Clear();
                lstOfNewFilters.Clear();
                var customReader = StreamingManagerReuse.Create(selectedDashBoardBinary);
                var version = customReader.readByte();
                if (version < 1 || version > 3)
                    return false;

                var bufferedReport = StreamingManager.readMemory(customReader);
                var st = Compression.UncompressStream(bufferedReport);

                if (version < 3 && customReader.readBoolean())
                {
                    int filterCount = (int)customReader.readNum();
                    for (int i = 0; i < filterCount; i++)
                    {
                        var key = customReader.readString();
                        var arrpropval = (PropValuePair[])customReader.ToArray(typeof(PropValuePair));
                        lstOfFilters.Add(key, arrpropval);
                    }
                }
                else if (version == 3 )
                {
                    if (customReader.readBoolean())
                    {
                        int filterCount = (int)customReader.readNum();
                        for (int i = 0; i < filterCount; i++)
                        {
                            var key = customReader.readString();
                            var arrFilter = (FilterProperties[])customReader.ToArray(typeof(FilterProperties));
                            lstOfNewFilters.Add(key, arrFilter.ToList());
                        }
                    }
                    if (customReader.readBoolean())
                    {
                        int filterCount = (int)customReader.readNum();
                        for (int i = 0; i < filterCount; i++)
                        {
                            var key = customReader.readString();
                            List<PropValuePair> propVal = new List<PropValuePair>();
                            var arrpropval = (PropValuePair[])customReader.ToArray(typeof(PropValuePair));
                            propVal = arrpropval.ToList();
                            lstOfFilters.Add(key, arrpropval);
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
                ShowBusyIndicator(); 
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

        void ShowBusyIndicator()
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

        public Task<UnicontaBaseEntity[]> Query(Type type, CrudAPI crudApi, IEnumerable<PropValuePair> filterValue)
        {
            return Task.Run(async () =>
            {
                return await crudApi.Query(Activator.CreateInstance(type) as UnicontaBaseEntity, null, filterValue);
            });
        }

        Dashboard _dashboard;
        private void DashboardViewerUniconta_DashboardChanged(object sender, EventArgs e)
        {
            _dashboard = dashboardViewerUniconta.Dashboard;
        }

        List<string> dataSourceLoadingParams = new List<string>();
        private void dashboardViewerUniconta_AsyncDataLoading(object sender, DataLoadingEventArgs e)
        {
            try
            {
                var dashboard = _dashboard?.DataSources?.FirstOrDefault(x => x.ComponentName == e.DataSourceComponentName);
                var fixedComp = fixedCompanies?.FirstOrDefault(x => x.DatasourceName == e.DataSourceComponentName);
                var compId = fixedComp != null ? fixedComp.CompanyId : this.company.CompanyId;
                var typeofTable = GetTypeAssociatedWithDashBoardDataSource(e.DataSourceName, e.DataSourceComponentName, compId);
                if (typeofTable == null && dataSourceAndTypeMap.ContainsKey(e.DataSourceComponentName))
                    typeofTable = dataSourceAndTypeMap[e.DataSourceComponentName];
                if (typeofTable != null)
                {
                    if (dashboard != null)
                    {
                        UpdateServerFilter(e.DataSourceComponentName, typeofTable);
                        if (lstOfFilters.ContainsKey(e.DataSourceComponentName))
                            filterValues = lstOfFilters[e.DataSourceComponentName];
                        else
                            filterValues = null;
                        if (!LoadOnOpen)
                            e.Data = Activator.CreateInstance(typeofTable);
                        else
                        {
                            if (dataSourceLoadingParams.Contains(e.DataSourceComponentName) && LoadOnOpen)
                            {
                                UnicontaBaseEntity[] data;
                                if (fixedComp == null || this.company.CompanyId == fixedComp.CompanyId)
                                    data = Query(typeofTable, api, filterValues).GetAwaiter().GetResult();
                                else
                                {
                                    var comp = CWDefaultCompany.loadedCompanies.FirstOrDefault(x => x.CompanyId == fixedComp.CompanyId);
                                    var compApi = new CrudAPI(BasePage.session, comp);
                                    data = Query(typeofTable, compApi, filterValues).GetAwaiter().GetResult();
                                }
                                if (lstOfSorters != null && lstOfSorters.ContainsKey(e.DataSourceComponentName))
                                    Array.Sort(data.ToArray(), lstOfSorters[e.DataSourceComponentName]); 
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
                MessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }

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
                    retType = tables?.Where(p => p.FullName == fullname).FirstOrDefault();
                    if (retType == null)
                        retType = tables?.Where(p => p.FullName == dataSource).FirstOrDefault();
                    if (retType == null)
                        retType = tables?.Where(p => p.FullName == tblType).FirstOrDefault();
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFilterDialog();
        }

        public void OpenFilterDialog()
        {
            try
            {
                if (!string.IsNullOrEmpty(selectedDataSourceName) && tableType != null)
                {
                    if (lstOfFilters.ContainsKey(selectedDataSourceName))
                        filterValues = lstOfFilters[selectedDataSourceName];
                    else
                        filterValues = null;

                    List<FilterProperties> filterProps;
                    if (lstOfNewFilters.ContainsKey(selectedDataSourceName))
                        filterProps = lstOfNewFilters[selectedDataSourceName];
                    else
                        filterProps = null;

                    if (lstOfSorters.ContainsKey(selectedDataSourceName))
                        PropSort = lstOfSorters[selectedDataSourceName];
                    else
                        PropSort = null;
                    Filter[] filters = null;
                    SortingProperties[] sorters = null;
                    //if (filterValues != null)
                    //    filters = Utility.CreateDefaultFilter(filterValues, tableType);
                    if (filterValues != null)
                        filters = filterProps.Select(p => new Filter() { name = p.PropertyName, value = p.UserInput, parameterType = p.ParameterType }).ToArray();
                    if (PropSort != null)
                        sorters = Utility.CreateDefaultSort(PropSort);
                    var fixedComp = fixedCompanies?.FirstOrDefault(x => x.DatasourceName == selectedDataSourceName);
                    if (fixedComp == null || this.company.CompanyId == fixedComp.CompanyId)
                        filterDialog = new CWServerFilter(api, tableType, filters, sorters, null);
                    else
                    {
                        var comp = CWDefaultCompany.loadedCompanies.FirstOrDefault(x => x.CompanyId == fixedComp.CompanyId) as Company;
                        var compApi = new CrudAPI(api.session, comp);
                        filterDialog = new CWServerFilter(compApi, tableType, filters, sorters, null);
                    }
                    filterDialog.Closing += FilterDialog_Closing;
                    filterDialog.Show();
                }
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
        }

        private void FilterDialog_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (filterDialog.DialogResult == true)
            {
                var filtersProps = filterDialog.Filters.Select(p => new FilterProperties() { PropertyName = p.name, UserInput = p.value, ParameterType = p.parameterType });
                filterValues = filterDialog.PropValuePair;
                PropSort = filterDialog.PropSort;
                if (lstOfFilters.ContainsKey(selectedDataSourceName))
                {
                    if (filterValues == null || filterValues.Count() == 0)
                        lstOfFilters.Remove(selectedDataSourceName);
                    else
                        lstOfFilters[selectedDataSourceName] = filterValues;
                }
                else
                {
                    if (filterValues != null && filterValues.Count() > 0)
                        lstOfFilters.Add(selectedDataSourceName, filterValues);
                }

                if (lstOfNewFilters.ContainsKey(selectedDataSourceName))
                {
                    if (filtersProps == null || filtersProps.Count() == 0)
                        lstOfNewFilters.Remove(selectedDataSourceName);
                    else
                        lstOfNewFilters[selectedDataSourceName] = filtersProps?.ToList();
                }
                else
                {
                    if (filtersProps != null && filtersProps.Count() > 0)
                        lstOfNewFilters.Add(selectedDataSourceName, filtersProps?.ToList());
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

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedDataSourceName))
            {
                if (lstOfFilters.ContainsKey(selectedDataSourceName))
                    lstOfFilters.Remove(selectedDataSourceName);
                if (lstOfSorters.ContainsKey(selectedDataSourceName))
                    lstOfFilters.Remove(selectedDataSourceName);
                if (lstOfNewFilters.ContainsKey(selectedDataSourceName))
                    lstOfNewFilters.Remove(selectedDataSourceName);
                dashboardViewerUniconta.ReloadData();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            dashboardViewerUniconta.ReloadData();
        }

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

        Task<Company> OpenFixedCompany(Company comp)
        {
            return Task.Run(async () =>
            {
                Company openComp = BasePage.session.GetOpenCompany(comp.CompanyId);
                if (openComp == null)
                {
                    var company = await BasePage.session.OpenCompany(comp.CompanyId, false, comp);
                    return company;
                }
                else
                    return openComp;
            });
        }
    }

    public class FixedCompany
    {
        public int CompanyId { get; set; }
        public string DatasourceName { get; set; }
    }

    public class PivotBehavior : Behavior<PivotGridControl>
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            ((PivotGridControl)AssociatedObject).DataSourceChanged += PivotBehavior_DataSourceChanged;
        }
        protected override void OnDetaching()
        {
            ((PivotGridControl)AssociatedObject).DataSourceChanged -= PivotBehavior_DataSourceChanged;
            base.OnDetaching();
        }
        private void PivotBehavior_DataSourceChanged(object sender, RoutedEventArgs e)
        {
            AssociatedObject.BestFitMaxRowCount = 100;
            ((PivotGridControl)AssociatedObject).BestFit();
        }
    }
}
