using DevExpress.CodeParser;
using DevExpress.Utils.Compress;
using DevExpress.Xpf.CodeView;
using DevExpress.Xpf.Grid;
using DevExpress.XtraRichEdit.Model;
using Org.BouncyCastle.Bcpg;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Enums;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages;
using Localization = Uniconta.ClientTools.Localization;

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
        SQLTableCache<Debtor> debtors;
        SQLTableCache<InvItem> items;
        SQLTableCache<WorkInstallation> installations;


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
            cmbReportingType.ItemsSource = new string[] { Localization.lookup("Packaging"), Localization.lookup("Batteries"), Localization.lookup("Electronic"), Localization.lookup("SingleUsePlastic") };
            cmbReportingType.SelectedIndex = 0;

            FromDate.DateTime = fromDate == DateTime.MinValue ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, 01) : fromDate;
            ToDate.DateTime = toDate == DateTime.MinValue ? new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month)) : toDate;

            debtors = api.GetCache<Uniconta.DataModel.Debtor>();
            items = api.GetCache<Uniconta.DataModel.InvItem>();
            installations = api.GetCache<Uniconta.DataModel.WorkInstallation>();
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
            compressed = false;

            fromDate = FromDate.DateTime;
            toDate = ToDate.DateTime;

            Date.Visible = true;
            PackagingType.Visible = false;
            PackagingRateLevel.Visible = false;
            WasteSorting.Visible = false;
            PackagingConsumer.Visible = false;
            Item.Visible = true;
            
            Country.Visible = true;
            Period.Visible = false;

            IsCreated.Visible = AppendNotPosted.IsChecked.GetValueOrDefault();

            PropValuePair prop;
            var filters = new List<PropValuePair>();
            if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
            {
                string datefilter = (fromDate != DateTime.MinValue ? $"{fromDate:d}" : "") + ".." + (toDate != DateTime.MinValue ? $"{toDate:d}" : "");
                prop = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Date), typeof(DateTime), datefilter);
                filters.Add(prop);
            }

            if (cmbCountry.SelectedIndex > 0)
            {
                var country = cmbCountry.SelectedIndex == comp._Country ? 0 : cmbCountry.SelectedIndex;
                var countryFilter = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Country), typeof(Int32), Convert.ToString(country));
                filters.Add(countryFilter);
            }

            ReportingType reportingType = ReportingType.Packing;
            if (cmbReportingType.SelectedIndex == 0)
            {
                reportingType = ReportingType.Packing;
                PackagingType.Visible = true;
                PackagingRateLevel.Visible = true;
                WasteSorting.Visible = true;
                PackagingConsumer.Visible = true;

                prop = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Category), typeof(byte), string.Concat(Convert.ToString((byte)InvPackagingCategoryEnum.Aluminum), "..", Convert.ToString((byte)InvPackagingCategoryEnum.PlasticFoam)));
                filters.Add(prop);
            }
            else if (cmbReportingType.SelectedIndex == 1)
            {
                reportingType = ReportingType.Batteries;
                prop = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Category), typeof(byte), string.Concat(Convert.ToString((byte)InvPackagingCategoryEnum.BB), "..", Convert.ToString((byte)InvPackagingCategoryEnum.SLI)));
                filters.Add(prop);
            }
            else if (cmbReportingType.SelectedIndex == 2)
            {
                PackagingConsumer.Visible = true;
                reportingType = ReportingType.Electronic;
                prop = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Category), typeof(byte), string.Concat(Convert.ToString((byte)InvPackagingCategoryEnum.Temp), "..", Convert.ToString((byte)InvPackagingCategoryEnum.Photovoltic)));
                filters.Add(prop);
            }
            else if (cmbReportingType.SelectedIndex == 3)
            {

                reportingType = ReportingType.OneTimeUsePlastic;
                prop = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Category), typeof(byte), string.Concat(Convert.ToString((byte)InvPackagingCategoryEnum.FoodContainers), "..", Convert.ToString((byte)InvPackagingCategoryEnum.FiltersTobacco)));
                filters.Add(prop);
            }

            busyIndicator.IsBusy = true;
            
            var packagingTrans = await api.Query<InvPackagingTransClient>(filters);

            List<InvPackagingTransClient> packLst = null;
            if (AppendNotPosted.IsChecked.GetValueOrDefault())
            {
                filters = new List<PropValuePair>();
                if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
                {
                    string datefilter = (fromDate != DateTime.MinValue ? $"{fromDate:d}" : "") + ".." + (toDate != DateTime.MinValue ? $"{toDate:d}" : "");
                    prop = PropValuePair.GenereteWhereElements(nameof(DebtorInvoice._Date), typeof(DateTime), datefilter);
                    filters.Add(prop);
                }
                var invoices = await api.Query<DebtorInvoiceClient>(filters);
                var mInvoices = new List<DebtorInvoiceClient>();
                if (invoices.Length > 0)
                {
                    SortInvPackJournalPostedId sort;
                    InvPackagingTransClient search;
                    foreach (var rec in invoices)
                    {
                        search = new InvPackagingTransClient();
                        sort = new SortInvPackJournalPostedId();
                        Array.Sort(packagingTrans, sort);

                        search._JournalPostedId = rec._JournalPostedId;
                        var pos = Array.BinarySearch(packagingTrans, search, sort);
                        if (pos >= 0 && pos < packagingTrans.Length)
                            continue;
                        mInvoices.Add(rec);
                    }

                    var cntTotal = mInvoices.Count;
                    if (cntTotal > 0)
                    {
                        int cnt = 0;
                        packLst = new List<InvPackagingTransClient>();
                        var packagingProducts = await api.Query<InvPackagingProductClient>();
                        foreach (var rec in mInvoices)
                        {
                            busyIndicator.IsBusy = true;

                            cnt++;
                            if (cnt == 1 || cnt == cntTotal || (cnt % 100) == 0)
                            {
                                busyIndicator.BusyContent = string.Concat(Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(cnt), " af ", cntTotal);
                                busyIndicator.IsBusy = false;
                            }

                            var debtor = (Debtor)debtors.Get(rec._DCAccount);
                            var lines = await api.Query<DebtorInvoiceLines>(rec);
                            if (lines.Length > 0)
                            {
                                var delCountry = debtor._Country;
                                if (rec.DeliveryCountry != null)
                                    delCountry = rec.DeliveryCountry.GetValueOrDefault();
                                else if (rec.Installation != null)
                                {
                                    var ins = installations.Get(rec.Installation);
                                    if (ins != null && ins._Country != null)
                                        delCountry = ins._Country;
                                }
                                else if (rec.DeliveryAccount != null && rec.DeliveryAccount != rec.Account)
                                {
                                    var dc = debtors.Get(rec.DeliveryAccount);
                                    if (dc != null)
                                        delCountry = dc._Country;
                                }

                                if ((byte)delCountry != cmbCountry.SelectedIndex)
                                    continue;

                                var sortPack = new SortPackProductItem();
                                Array.Sort(packagingProducts, sortPack);

                                InvPackagingProductClient searchPack;
                                foreach (var line in lines)
                                {
                                    var item = (InvItem)items.Get(line._Item);
                                    if (item != null && item._ItemType != (byte)Uniconta.DataModel.ItemType.Service)
                                    {
                                        searchPack = new InvPackagingProductClient();
                                        searchPack._Item = line.Item;
                                        var pos = Array.BinarySearch(packagingProducts, searchPack, sortPack);
                                        if (pos < 0)
                                            pos = ~pos;

                                        while (pos < packagingProducts.Length)
                                        {
                                            var pack = packagingProducts[pos++];
                                            if (pack.Item != line.Item)
                                                break;

                                            if (pack.NoReporting && !debtor._MicroEnterprise || pack._Reporting != reportingType)
                                                continue;

                                            packLst.Add(new InvPackagingTransClient()
                                            {
                                                CompanyId = api.CompanyId,
                                                _Category = pack._Category,
                                                _WasteSorting = pack._WasteSorting,
                                                _PackagingRateLevel = pack._PackagingRateLevel,
                                                _Consumer = pack._PaymentGrouping == Uniconta.DataModel.PackagingConsumer.None ? (debtor._Consumer ? true : false) : pack._PaymentGrouping == Uniconta.DataModel.PackagingConsumer.Business ? true : false,
                                                _Country = delCountry == comp._CountryId ? 0 : delCountry,
                                                _Item = item._Item,
                                                _Date = line._Date,
                                                _JournalPostedId = line.JournalPostedId,
                                                _Packaging = pack._Packaging,
                                                _Weight = -line.Qty * pack._Weight,
                                                IsCreated = true,
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            var packagingTransLst = packagingTrans.ToList();
            if (packLst != null)
                packagingTransLst.AddRange(packLst);

            if (packagingTransLst == null)
                dgInvPackagingTransGrid.ItemsSource = null;
            else
                dgInvPackagingTransGrid.SetSource(packagingTransLst.ToArray());

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
                        found.Qty += rec.Qty;
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
                    Country.Visible = false;
                    Item.Visible = false;
                    Name.Visible = false;
                    IsCreated.Visible = false;

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
                c = string.Compare(x.PackagingRateLevel, y.PackagingRateLevel);
                if (c != 0)
                    return false;
                c = string.Compare(x.PackagingConsumer, y.PackagingConsumer);
                if (c != 0)
                    return false;
                return true;
            }
            public int GetHashCode(InvPackagingTransClient x)
            {
                return Util.GetHashCode(x.Period) * Util.GetHashCode(x.Category) * Util.GetHashCode(x.WasteSorting) * Util.GetHashCode(x.PackagingRateLevel) * Util.GetHashCode(x.PackagingConsumer);
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
            busyIndicator.BusyContent = string.Format(Uniconta.ClientTools.Localization.lookup("ExportingFile"), Uniconta.ClientTools.Localization.lookup("ProducerResponsibility"));

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
            var dictionaryColumnIndices = new Dictionary<string, int>(cmbReportingType.SelectedIndex == 0 ? 7 : 4);

            int idx = 1;
            string key = "Period";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            idx = 2;
            key = "Category";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx);

            if (cmbReportingType.SelectedIndex == 0)
            {
                key = "WasteSorting";
                if (!dictionaryColumnIndices.ContainsKey(key))
                    dictionaryColumnIndices.Add(key, idx++);

                key = "PackagingRateLevel";
                if (!dictionaryColumnIndices.ContainsKey(key))
                    dictionaryColumnIndices.Add(key, idx++);
            }

            if (cmbReportingType.SelectedIndex == 0 || cmbReportingType.SelectedIndex == 2)
            {
                key = "PackagingConsumer";
                if (!dictionaryColumnIndices.ContainsKey(key))
                    dictionaryColumnIndices.Add(key, idx++);
            }

            key = "Unit";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx++);

            key = "Qty";
            if (!dictionaryColumnIndices.ContainsKey(key))
                dictionaryColumnIndices.Add(key, idx++);

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
            cnt = CSVHelper.ExportDataGridToExcel(stream, Headers, corasauBaseEntity, Props, ".xlsx", DateTime.Now.Ticks.ToString());
            writer.Flush();
            return cnt;
        }

        private void cmbReportingType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            lbAppendNotPosted.Text = string.Format(Localization.lookup("CreateNotPostedOBJ"), cmbReportingType.Text.ToLower());
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (debtors == null)
                debtors = await api.LoadCache<Uniconta.DataModel.Debtor>().ConfigureAwait(false);
            if (items == null)
                items = await api.LoadCache<Uniconta.DataModel.InvItem>().ConfigureAwait(false);
            if (installations == null)
                installations = await api.LoadCache<Uniconta.DataModel.WorkInstallation>().ConfigureAwait(false);
        }
    }
}
