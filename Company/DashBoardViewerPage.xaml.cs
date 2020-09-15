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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DashBoardViewerPage : ControlBasePage
    {
        public override string NameOfControl { get { return TabControls.DashBoardViewerPage; } }
        string DashBoardName;
        Type tableType;
        DashboardClient _selectedDashBoard;
        Dictionary<string, Type> dataSourceAndTypeMap;
        CWServerFilter filterDialog;
        public FilterSorter PropSort;
        private Dictionary<string, IEnumerable<PropValuePair>> lstOfFilters;
        private Dictionary<string, FilterSorter> lstOfSorters;
        private Dictionary<int, Type[]> ListOfReportTableTypes;
        public IEnumerable<PropValuePair> filterValues;
        string selectedDataSourceName;
        Company company;
        DashBoardVM dashBoardVM;
        DashboardState dState;
        string blankString;
        bool LoadOnOpen;
        List<FixedCompany> fixedCompanies;
        string dashboardName;

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
            dashBoardVM = new DashBoardVM();
            dState = new DashboardState();
            blankString = new string(' ', 18);
            company = api.CompanyEntity;
            InitializeComponent();
            dashboardViewerUniconta.ObjectDataSourceLoadingBehavior = DevExpress.DataAccess.DocumentLoadingBehavior.LoadAsIs;
            dataSourceAndTypeMap = new Dictionary<string, Type>();
            DataContext = dashBoardVM;
            lstOfFilters = new Dictionary<string, IEnumerable<PropValuePair>>();
            lstOfSorters = new Dictionary<string, FilterSorter>();
            ListOfReportTableTypes = new Dictionary<int, Type[]>();
            LoadListOfTableTypes(BasePage.session.DefaultCompany);
            dashboardViewerUniconta.Loaded += DashboardViewerUniconta_Loaded;
            dashboardViewerUniconta.DashboardLoaded += DashboardViewerUniconta_DashboardLoaded;
            dashboardViewerUniconta.SetInitialDashboardState += DashboardViewerUniconta_SetInitialDashboardState;
            dashboardViewerUniconta.DashboardChanged += DashboardViewerUniconta_DashboardChanged;
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
                            openComp.GenerateUserType();
                            LoadListOfTableTypes(openComp);
                        }
                    }
                }

                var ldOnOpn = data.Element("LoadOnOpen");
                if (ldOnOpn != null)
                    LoadOnOpen = bool.Parse(ldOnOpn.Value);

                var filters = data.Element("Filters");
                if (filters != null)
                {
                    var filtersBytes = Convert.FromBase64String(filters.Value);
                    var customReader = StreamingManagerReuse.Create(filtersBytes);
                    if (customReader.readBoolean())
                    {
                        int filterCount = (int)customReader.readNum();
                        for (int i = 0; i < filterCount; i++)
                        {
                            var key = customReader.readString();
                            var arrpropval = (PropValuePair[])customReader.ToArray(typeof(PropValuePair));
                            lstOfFilters.Add(key, arrpropval);
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
                            lstOfSorters.Add(key, propSort);
                        }
                    }
                    customReader.Release();
                }
            }
            else
                LoadOnOpen = true;  // for old saved dashboards

            if (LoadOnOpen)
                foreach (var ds in e.Dashboard.DataSources)
                    dataSourceLoadingParams.Add(ds.ComponentName);
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
                    dashboardViewerUniconta.TitleContent = string.Format("{0} {1} ( {2} )", Uniconta.ClientTools.Localization.lookup("Company"), companyName, tableType.Name);
                }
                else
                    dashboardViewerUniconta.TitleContent = string.Format("{0} {1} ( {2} )", Uniconta.ClientTools.Localization.lookup("Company"), company.KeyName, tableType.Name);
            }
            else if (((DataDashboardItem)FocusedItem).DataSource is DashboardFederationDataSource)
            {
                var dataSource = ((DataDashboardItem)FocusedItem).DataSource;
                var sources = ((DevExpress.DataAccess.DataFederation.FederationDataSourceBase)(dataSource)).Sources;
                var name = new StringBuilder();
                foreach (var source in sources)
                {
                    var componentName = ((DashboardObjectDataSource)(source.DataSource)).ComponentName;
                    name.Append(source.Name).Append("  ");
                    if (!dataSourceLoadingParams.Contains(componentName))
                        dataSourceLoadingParams.Add(componentName);
                }
                if (!LoadOnOpen)              // load on open is saved as true. and first time on load user click on federation related dashboardItem 
                    LoadOnOpen = true;
                dashboardViewerUniconta.TitleContent = string.Format("{0} ({1})", _selectedDashBoard._Name, name);
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
                    DashBoardName = _selectedDashBoard._Name;
                    if (_selectedDashBoard.Layout != null)
                        ReadDataFromDB(_selectedDashBoard.Layout);
                    dashboardViewerUniconta.TitleContent = _selectedDashBoard.Name;
                    dashboardViewerUniconta.Dashboard.Title.Visible = true;
                }
                else
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(result.ToString()), Uniconta.ClientTools.Localization.lookup("Error"));
            }

            if(_selectedDashBoard== null)
                busyIndicator.IsBusy = false;

            return true;
        }

        int refreshTimer;
        private bool ReadDataFromDB(Byte[] selectedDashBoardBinary)
        {
            bool retVal = true;
            try
            {
                lstOfFilters.Clear();

                var customReader = StreamingManagerReuse.Create(selectedDashBoardBinary);
                var version = customReader.readByte();
                if (version < 1 || version > 2)
                    return false;

                var bufferedReport = StreamingManager.readMemory(customReader);
                var st = Compression.UncompressStream(bufferedReport);

                if (customReader.readBoolean())
                {
                    int filterCount = (int)customReader.readNum();
                    for (int i = 0; i < filterCount; i++)
                    {
                        var key = customReader.readString();
                        var arrpropval = (PropValuePair[])customReader.ToArray(typeof(PropValuePair));
                        lstOfFilters.Add(key, arrpropval);
                    }
                }
                if (version == 2 && customReader.readBoolean())
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
                var exist = xdoc.Root.Attributes().Where(x => x.Name == "RefreshTimer").FirstOrDefault();
                if (exist != null)
                {
                    var element = xdoc.Root.Attribute("RefreshTimer") as XAttribute;
                    if (element != null || element.Name == "RefreshTimer")
                    {
                        refreshTimer = Convert.ToInt16(element.Value);
                        if (refreshTimer > 0)
                        {
                            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                            timer.Interval = refreshTimer < 300 ? 300 * 1000 : refreshTimer * 1000;
                            timer.Tick += Timer_Tick;
                            timer.Start();
                        }
                    }
                }
                st.Seek(0, System.IO.SeekOrigin.Begin);
                ShowBusyIndicator();
                dashboardViewerUniconta.LoadDashboard(st);
                st.Release();
                return retVal;
            }
            catch (Exception ex)
            {
                retVal = false;
                UnicontaMessageBox.Show(ex);
                dockCtrl?.CloseDockItem();
                busyIndicator.IsBusy = false;
                return retVal;
            }
        }

        void ShowBusyIndicator()
        {
            DispatcherTimer timer = new DispatcherTimer();
            EventHandler tick = null;
            tick = (o, eventArgs) =>
            {
                if (!dashboardViewerUniconta.DashboardViewModel.IsLoaded)
                    return;
                busyIndicator.IsBusy = false;
                timer.Tick -= tick;
                timer.Stop();
            };
            timer.Tick += tick;
            timer.Start();
        }

        private List<Type> GetReportTableTypes()
        {
            var list = Global.GetTables(company);//Standard Tables
            var userTables = Global.GetUserTables(company);// User tables
            if (userTables != null)
                list.AddRange(userTables);
            return list;
        }

        public UnicontaBaseEntity[] Query(Type entityType, CrudAPI api, IEnumerable<PropValuePair> filterValue)
        {
            var entity = Activator.CreateInstance(entityType) as UnicontaBaseEntity;
            if (entity != null)
            {
                var lstEntity = api.QuerySync(entity, null, filterValue);
                return lstEntity;
            }
            return null;
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
                var compId = fixedComp != null ? fixedComp.CompanyId : api.CompanyId;
                var typeofTable = GetTypeAssociatedWithDashBoardDataSource(e.DataSourceName, e.DataSourceComponentName, compId);
                if (lstOfFilters.ContainsKey(e.DataSourceComponentName))
                    filterValues = lstOfFilters[e.DataSourceComponentName];
                else
                    filterValues = null;

                if (typeofTable == null && dataSourceAndTypeMap.ContainsKey(e.DataSourceComponentName))
                    typeofTable = dataSourceAndTypeMap[e.DataSourceComponentName];
                if (typeofTable != null)
                {
                    if (dashboard != null)
                    {
                        if (!LoadOnOpen)
                        {
                            var entity = Activator.CreateInstance(typeofTable);
                            e.Data = entity;
                        }
                        else
                        {
                            if (dataSourceLoadingParams.Contains(e.DataSourceComponentName) && LoadOnOpen)
                            {
                                var filterExist = lstOfFilters.ContainsKey(e.DataSourceComponentName);
                                UnicontaBaseEntity[] data;
                                if (api.CompanyEntity.CompanyId == fixedComp?.CompanyId || fixedComp == null)
                                    data = Query(typeofTable, api, filterValues);
                                else
                                {
                                    var comp = CWDefaultCompany.loadedCompanies.FirstOrDefault(x => x.CompanyId == fixedComp?.CompanyId) as Company;
                                    var compApi = new CrudAPI(BasePage.session, comp);
                                    data = Query(typeofTable, compApi, filterValues);
                                }
                                if (lstOfSorters != null && lstOfSorters.ContainsKey(e.DataSourceComponentName))
                                    Array.Sort(data.ToArray(), lstOfSorters[e.DataSourceComponentName]); 
                                e.Data = data;
                            }
                            else 
                            {
                                var entity = Activator.CreateInstance(typeofTable);
                                e.Data = entity;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Uniconta.ClientTools.Localization.lookup("Exception"));
            }
        }

        private Type GetTypeAssociatedWithDashBoardDataSource(string dataSource, string componentName, int companyId)
        {
            Type retType = null;
            var ds = dashBoardVM.UnicontaDashboard.DataSources.Where(d => d.ComponentName == componentName).FirstOrDefault();
            if (ds != null)
            {
                var data = ds.Data;
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
                        string tableType = li >= 0 ? ds.Name.Substring(0, li) : ds.Name;
                        fullname = string.Concat("Uniconta.ClientTools.DataModel.", tableType);
                    }
                }
                else
                {
                    var li = ds.Name.IndexOf('(');
                    string tableType = li >= 0 ? ds.Name.Substring(0, li) : ds.Name;
                    fullname = string.Concat("Uniconta.ClientTools.DataModel.", tableType);
                }

                if (fullname != null)
                {
                    fullname = fullname.Replace("[]", "");
                    var tables = ListOfReportTableTypes.FirstOrDefault(x => x.Key == companyId).Value;
                    retType = tables?.Where(p => p.FullName == fullname).FirstOrDefault();
                    if (retType == null)
                        retType = tables?.Where(p => p.FullName == dataSource).FirstOrDefault();
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
                    if (lstOfSorters.ContainsKey(selectedDataSourceName))
                        PropSort = lstOfSorters[selectedDataSourceName];
                    else
                        PropSort = null;
                    Filter[] filters = null;
                    SortingProperties[] sorters = null;
                    if (filterValues != null)
                        filters = Utility.CreateDefaultFilter(filterValues, tableType);
                    if (PropSort != null)
                        sorters = Utility.CreateDefaultSort(PropSort);
                    var fixedComp = fixedCompanies?.FirstOrDefault(x => x.DatasourceName == selectedDataSourceName);
                    if (fixedComp == null || api.CompanyId == fixedComp.CompanyId)
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
                dashboardViewerUniconta.ReloadData();
            }
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            dashboardViewerUniconta.ReloadData();
        }

        void LoadListOfTableTypes(Company company)
        {
            var list = Global.GetTables(company);
            var userTables = Global.GetUserTables(company);
            if (userTables != null)
                list.AddRange(userTables);
            if (!ListOfReportTableTypes.ContainsKey(company.CompanyId))
                ListOfReportTableTypes.Add(company.CompanyId, list.ToArray());
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

    public class DashBoardVM
    {
        public Dashboard UnicontaDashboard { get; set; }
        public ImageSource FilterImage { get { return UnicontaClient.Utilities.Utility.GetGlyph("Filter_32x32.png"); } }
        public ImageSource ClearFilterImage { get { return UnicontaClient.Utilities.Utility.GetGlyph("Filter_Clear_32x32.png"); } }
        public ImageSource RefreshImage { get { return UnicontaClient.Utilities.Utility.GetGlyph("refresh.png"); } }
    }
}
