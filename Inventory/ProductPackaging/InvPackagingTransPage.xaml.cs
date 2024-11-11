using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using System.IO;
using System.Reflection;
using Uniconta.DataModel;
using Uniconta.Common.Enums;
using Localization = Uniconta.ClientTools.Localization;
using Uniconta.ClientTools;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingTransGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackagingTransClient); } }
        public override bool Readonly { get { return IsReadOnly; } }

        public bool IsReadOnly;
    }

    public partial class InvPackagingTransPage : GridBasePage
    {
        static DateTime fromDate, toDate;
        Company comp;
        bool compressed;
        public override string NameOfControl { get { return TabControls.InvPackagingTransPage; } }
        public InvPackagingTransPage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgInvPackagingTransGrid.IsReadOnly = master == null;
            localMenu.dataGrid = dgInvPackagingTransGrid;
            SetRibbonControl(localMenu, dgInvPackagingTransGrid);
            dgInvPackagingTransGrid.api = api;
            dgInvPackagingTransGrid.tableView.AllowEditing = false;
            dgInvPackagingTransGrid.BusyIndicator = busyIndicator;
            dgInvPackagingTransGrid.ShowTotalSummary();

            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            cmbCountry.ItemsSource = AppEnums.Countries.GetLabels();
            comp = api.CompanyEntity;
            cmbCountry.SelectedIndex = comp._Country;

            FromDate.DateTime = fromDate == DateTime.MinValue ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 01) : fromDate;
            ToDate.DateTime = toDate == DateTime.MinValue ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)) : toDate;
        }

        public override Task InitQuery()
        {
            return null;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvPackagingTransGrid.SelectedItem;
            switch (ActionType)
            {
                case "Compress":
                    if (selectedItem != null)
                        Compress();
                    break;

                case "Search":
                    LoadGrid();
                    break;

                case "CreateFile":
                    if (selectedItem != null)
                        CreateFile(selectedItem.GetType());
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async Task LoadGrid()
        {
            fromDate = FromDate.DateTime;
            toDate = ToDate.DateTime;

            Date.Visible = true;
            PackagingType.Visible = true;
            PackagingRateLevel.Visible = true;
            Country.Visible = true;
            Period.Visible = false;

            var filters = new List<PropValuePair>();
            if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
            {
                string datefilter = (fromDate != DateTime.MinValue ? $"{fromDate:d}" : "") + ".." + (toDate != DateTime.MinValue ? $"{toDate:d}" : "");
                var prop = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Date), typeof(DateTime), datefilter);
                filters.Add(prop);
            }

            if (cmbCountry.SelectedIndex > 0)
            {
                var country = cmbCountry.SelectedIndex == comp._Country ? 0 : cmbCountry.SelectedIndex;
                var countryFilter = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Country), typeof(Int32), Convert.ToString(country));
                filters.Add(countryFilter);
            }

            busyIndicator.IsBusy = true;
            var packagingTrans = await api.Query<InvPackagingTransClient>(filters);
            if (packagingTrans == null)
                dgInvPackagingTransGrid.ItemsSource = null;
            else
                dgInvPackagingTransGrid.SetSource(packagingTrans);

            busyIndicator.IsBusy = false;
        }

        private void Compress()
        {
            if (string.IsNullOrWhiteSpace(comp._Id))
            {
                UnicontaMessageBox.Show(string.Format(Localization.lookup("MissingOBJ"), Localization.lookup("CompanyRegNo")), Localization.lookup("Warning"));
                return;
            }

            try
            {
                var lst = (IEnumerable<InvPackagingTransClient>)dgInvPackagingTransGrid.GetVisibleRows();
                var dict = new Dictionary<InvPackagingTransClient, InvPackagingTransClient>(new CompressCompare());

                foreach (var rec in lst)
                {
                    InvPackagingTransClient found;
                    if (dict.TryGetValue(rec, out found))
                        found.Weight += rec.Weight;
                    else
                        dict.Add(rec, rec);
                }

                var dictlst = dict.Values.ToList();
                if (dictlst == null || dictlst.Count == 0)
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoLinesFound"), Uniconta.ClientTools.Localization.lookup("Warning"));
                else
                {
                    Period.Visible = true;
                    Date.Visible = false;
                    JournalPostedId.Visible = false;
                    PackagingType.Visible = false;
                    PackagingRateLevel.Visible = false;
                    Country.Visible = false;

                    dgInvPackagingTransGrid.ItemsSource = dictlst;
                    dgInvPackagingTransGrid.Visibility = Visibility.Visible;
                    dgInvPackagingTransGrid.UpdateTotalSummary();

                    compressed = true;
                }
            }
            catch (Exception e)
            {
                UnicontaMessageBox.Show(e, Uniconta.ClientTools.Localization.lookup("Exception"));
                throw;
            }
        }


        class CompressCompare : IEqualityComparer<InvPackagingTransClient>
        {
            public bool Equals(InvPackagingTransClient x, InvPackagingTransClient y)
            {
                var c = string.Compare(x.Period, y.Period);
                if (c != 0)
                    return false;
                c = string.Compare(x.Category, y.Category);
                if (c != 0)
                    return false;
                c = string.Compare(x.WasteSorting, y.WasteSorting);
                if (c != 0)
                    return false;
                c = string.Compare(x.PackagingConsumer, y.PackagingConsumer);
                if (c != 0)
                    return false;
                return true;
            }
            public int GetHashCode(InvPackagingTransClient x)
            {
                return Util.GetHashCode(x.Period) * Util.GetHashCode(x.Category) * Util.GetHashCode(x.WasteSorting) * Util.GetHashCode(x.PackagingConsumer);
            }
        }


        private void CreateFile(Type recordType)
        {
            if (compressed == false)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("CompressPosting"), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            var mappedItems = MapColumnsToIndices();

            var sfd = UtilDisplay.LoadSaveFileDialog;
            sfd.DefaultExt = "xlsx";
            sfd.Filter = "XLSX Files (*.xlsx)|*.xlsx";
            sfd.FilterIndex = 1;

            bool? userClickedSave = sfd.ShowDialog();
            if (userClickedSave != true)
                return;

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = string.Format(Uniconta.ClientTools.Localization.lookup("ExportingFile"), Uniconta.ClientTools.Localization.lookup("IntraStat"));

            Stream stream = null;
            try
            {
                var lst = (IEnumerable<InvPackagingTransClient>)dgInvPackagingTransGrid.GetVisibleRows();
                stream = File.Create(sfd.FileName);
                var cnt = ExportDataGrid(stream, lst, recordType, mappedItems);

                stream.Flush();
                stream.Close();

                busyIndicator.IsBusy = false;
                UnicontaMessageBox.Show(string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Exported"), cnt), Uniconta.ClientTools.Localization.lookup("Message"));
            }
            catch (Exception ex)
            {
                busyIndicator.IsBusy = false;
                stream?.Dispose();
                UnicontaMessageBox.Show(ex);
            }
        }

        private Dictionary<string, int> MapColumnsToIndices()
        {
            var dictionaryColumnIndices = new Dictionary<string, int>(5);

            int idx = 1;
            string key = "Period";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 2;
            key = "Category";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 3;
            key = "WasteSorting";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 4;
            key = "PackagingConsumer";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 5;
            key = "Weight";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            return dictionaryColumnIndices;
        }

        int ExportDataGrid(Stream stream, IEnumerable<UnicontaBaseEntity> corasauBaseEntity, Type RecordType, Dictionary<string, int> mappedItems)
        {
            var Props = new List<PropertyInfo>();
            var Headers = new List<string>();
            var sortedItems = (from l in mappedItems where l.Value > 0 orderby l.Value ascending select l).ToList();
            foreach (var strKey in sortedItems)
            {
                var pInfo = RecordType.GetProperty(strKey.Key);
                if (pInfo != null)
                {
                    Headers.Add(UtilFunctions.GetDisplayNameFromPropertyInfo(pInfo));
                    Props.Add(pInfo);
                }
            }
            int cnt = 0;
            var writer = new StreamWriter(stream, Encoding.Default);
            cnt = CSVHelper.ExportDataGridToExcel(stream, Headers, corasauBaseEntity, Props, spreadSheet, ".xlsx", "Emballage");
            writer.Flush();
            return cnt;
        }
    }
}
