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
        CWServerFilter filterDialog = null;
        public FilterSorter PropSort;
        private Dictionary<string, IEnumerable<PropValuePair>> lstOfFilters;
        private Dictionary<string, FilterSorter> lstOfSorters;
        public IEnumerable<PropValuePair> filterValues;
        string selectedDataSourceName;
        Company company;
        DashBoardVM dashBoardVM = new DashBoardVM();
        DashboardState dState = new DashboardState();
        string blankString = new string(' ', 18);
        public DashBoardViewerPage(UnicontaBaseEntity dashBoard) : base(dashBoard)
        {
            company = api.CompanyEntity;
            InitializeComponent();
            InitPage(dashBoard);
        }

        string dashboardName;
        public DashBoardViewerPage(string _dashboard) : base(null)
        {
            company = api.CompanyEntity;
            InitializeComponent();
            dashboardName = _dashboard;
            InitPage(null);
        }

        void InitPage(UnicontaBaseEntity dashBoard)
        {
            dashboardViewerUniconta.ObjectDataSourceLoadingBehavior = DevExpress.DataAccess.DocumentLoadingBehavior.LoadAsIs;
            _selectedDashBoard = dashBoard as DashboardClient;
            dataSourceAndTypeMap = new Dictionary<string, Type>();
            DataContext = dashBoardVM;
            lstOfFilters = new Dictionary<string, IEnumerable<PropValuePair>>();
            lstOfSorters = new Dictionary<string, FilterSorter>();
            dashboardViewerUniconta.Loaded += DashboardViewerUniconta_Loaded;
            dashboardViewerUniconta.DashboardItemMouseUp += DashboardViewerUniconta_DashboardItemMouseUp;
            dashboardViewerUniconta.DashboardLoaded += DashboardViewerUniconta_DashboardLoaded;
            dashboardViewerUniconta.SetInitialDashboardState += DashboardViewerUniconta_SetInitialDashboardState;
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
            }
        }

        private void DashboardViewerUniconta_DashboardItemMouseUp(object sender, DevExpress.DashboardWpf.DashboardItemMouseActionWpfEventArgs e)
        {
            var st = dashboardViewerUniconta.Dashboard.Items.FirstOrDefault(x => x.ComponentName == e.DashboardItemName);
            if (st != null && (((DataDashboardItem)st).DataSource) != null && dataSourceAndTypeMap.ContainsKey((((DataDashboardItem)st).DataSource).ComponentName))
            {
                tableType = dataSourceAndTypeMap[(((DataDashboardItem)st).DataSource).ComponentName];
                selectedDataSourceName = (((DataDashboardItem)st).DataSource).ComponentName;
                dashboardViewerUniconta.TitleContent = string.Format("{0} {1} ( {2} )", Uniconta.ClientTools.Localization.lookup("Company"), company.KeyName, tableType.Name);
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

        bool isDashBoardLaoded = false;
        private void DashboardViewerUniconta_Loaded(object sender, RoutedEventArgs e)
        {
            if (isDashBoardLaoded)
                return;
            Initialise();
            isDashBoardLaoded = true;
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
                    DashBoardName = _selectedDashBoard.Name;
                    if (_selectedDashBoard.Layout != null)
                        ReadDataFromDB(_selectedDashBoard.Layout);
                    dashboardViewerUniconta.Dashboard.Title.Visible = true;
                }
                else
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup(result.ToString()), Uniconta.ClientTools.Localization.lookup("Error"));
            }
            busyIndicator.IsBusy = false;
            return true;
        }

        int refreshTimer = 0;
        private bool ReadDataFromDB(Byte[] selectedDashBoardBinary)
        {
            bool retVal = true;
            try
            {
                lstOfFilters.Clear();

                var cs = StreamingManagerReuse.Create(selectedDashBoardBinary);
                var customReader = cs.ReadStream;
                var version = customReader.readByte();
                if (version < 1 || version > 2)
                    return false;

                var bufferedReport = StreamingManager.readMemory(customReader);
                var st = Compression.UncompressStream(bufferedReport);

                if (customReader.readBoolean())
                {
                    int filterCount = (int)StreamingManager.readNum(customReader);
                    for (int i = 0; i < filterCount; i++)
                    {
                        var key = customReader.readString();
                        List<PropValuePair> propVal = new List<PropValuePair>();
                        var arrpropval = (PropValuePair[])customReader.ToArray(typeof(PropValuePair));
                        propVal = arrpropval.ToList();
                        lstOfFilters.Add(key, arrpropval);
                    }
                }
                if (version == 2 && customReader.readBoolean())
                {
                    int sortCount = (int)StreamingManager.readNum(customReader);
                    for (int i = 0; i < sortCount; i++)
                    {
                        var key = customReader.readString();
                        var arrSort = (SortingProperties[])customReader.ToArray(typeof(SortingProperties));
                        FilterSorter propSort = new FilterSorter(arrSort);
                        if (lstOfSorters != null && !lstOfSorters.ContainsKey(key))
                            lstOfSorters.Add(key, propSort);
                    }
                }
                cs.Release();
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
                dashboardViewerUniconta.LoadDashboard(st);
                st.Release();
                return retVal;
            }
            catch (Exception ex)
            {
                retVal = false;
                UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"));
                dockCtrl?.CloseDockItem();
                busyIndicator.IsBusy = false;
                return retVal;
            }
        }

        private List<Type> GetReportTableTypes()
        {
            var list = new List<Type>();
            try
            {
                var systemTables = Global.GetTables(company);//Standard Tables
                if (systemTables != null && systemTables.Count > 0)
                    list.AddRange(systemTables);
                var userTables = Global.GetUserTables(company);// User tables
                if (userTables != null && userTables.Count > 0)
                    list.AddRange(userTables);
            }
            catch
            {
                throw;

            }
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

        private void dashboardViewerUniconta_AsyncDataLoading(object sender, DataLoadingEventArgs e)
        {
            try
            {
                var typeofTable = GetTypeAssociatedWithDashBoardDataSource(e.DataSourceName, e.DataSourceComponentName);
                selectedDataSourceName = e.DataSourceComponentName;

                if (lstOfFilters.ContainsKey(e.DataSourceComponentName))
                {
                    filterValues = lstOfFilters[e.DataSourceComponentName];
                }
                else
                    filterValues = null;

                if (typeofTable == null && dataSourceAndTypeMap.ContainsKey(e.DataSourceComponentName))
                    typeofTable = dataSourceAndTypeMap[e.DataSourceComponentName];
                if (typeofTable != null)
                {
                    var queryresult = Query(typeofTable, api, filterValues);
                    if (lstOfSorters != null && lstOfSorters.ContainsKey(e.DataSourceComponentName) && lstOfSorters[e.DataSourceComponentName] != null)
                        Array.Sort(queryresult, lstOfSorters[e.DataSourceComponentName]);
                    e.Data = queryresult;
                }
            }
            catch
            {

            }
        }

        private Type GetTypeAssociatedWithDashBoardDataSource(string dataSource, string componentName)
        {
            Type retType = null;
            var ds = dashBoardVM.UnicontaDashboard.DataSources.Where(d => d.Name == dataSource).FirstOrDefault();
            if (ds != null)
            {
                var data = ds.Data;
                PropertyInfo propInfo = null;
                string fullname = null;
                if (data != null)
                {
                    propInfo = ds.Data.GetType().GetProperty("FullName");
                    if (propInfo != null)
                    {
                        fullname = propInfo.GetValue(ds.Data, null) as string;
                    }
                }
                else
                {
                    fullname = string.Concat("Uniconta.ClientTools.DataModel.", dataSource);
                }

                if (fullname != null)
                {
                    fullname = fullname.Replace("[]", "");
                    retType = GetReportTableTypes()?.Where(p => p.FullName == fullname).FirstOrDefault();
                    if (retType == null)
                        retType = GetReportTableTypes()?.Where(p => p.FullName == dataSource).FirstOrDefault();
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
                if (!string.IsNullOrEmpty(selectedDataSourceName) && tableType != null && filterDialog == null)
                {
                    if (lstOfFilters.ContainsKey(selectedDataSourceName))
                    {
                        filterValues = lstOfFilters[selectedDataSourceName];
                    }
                    else
                        filterValues = null;
                    if (lstOfSorters.ContainsKey(selectedDataSourceName))
                    {
                        PropSort = lstOfSorters[selectedDataSourceName];
                    }
                    else
                        PropSort = null;

                    filterDialog = new CWServerFilter(api, tableType, null, null, null);
                    filterDialog.Closing += FilterDialog_Closing;
                }

                filterDialog.Show();
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex, Uniconta.ClientTools.Localization.lookup("Exception"));
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
                    lstOfSorters.Add(selectedDataSourceName, PropSort);
                }
                Initialise();
            }

            e.Cancel = true;
            filterDialog.Hide();
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            filterValues = null;
            PropSort = null;
            Initialise();
        }
    }

    public class DashBoardVM
    {
        public Dashboard UnicontaDashboard { get; set; }
        public ImageSource FilterImage { get { return UnicontaClient.Utilities.Utility.GetGlyph("Filter_32x32.png"); } }
        public ImageSource ClearFilterImage { get { return UnicontaClient.Utilities.Utility.GetGlyph("Filter_Clear_32x32.png"); } }
        public ImageSource RefreshImage { get { return UnicontaClient.Utilities.Utility.GetGlyph("refresh.png"); } }
    }

    public class DashBoardDataSource
    {
        public DashBoardDataSource(Type entityType, CrudAPI api, IEnumerable<PropValuePair> filterValues)
        {
            var entity = Activator.CreateInstance(entityType) as UnicontaBaseEntity;
            var lstEntity = api.QuerySync(entity, null, filterValues);
            Data = lstEntity;
        }

        public UnicontaBaseEntity[] Data
        {
            get; set;
        }
    }
}
