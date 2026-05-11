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
        SQLTableCache<Uniconta.DataModel.Debtor> debtors;
        SQLTableCache<Uniconta.DataModel.DebtorGroup> debtorGroups;
        SQLTableCache<Uniconta.DataModel.InvItem> items;
        SQLTableCache<Uniconta.DataModel.WorkInstallation> installations;

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
            debtorGroups = api.GetCache<Uniconta.DataModel.DebtorGroup>();
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

            var dateFilter = new List<PropValuePair>();
            if (fromDate != DateTime.MinValue || toDate != DateTime.MinValue)
            {
                string dfilter = (fromDate != DateTime.MinValue ? $"{fromDate:d}" : "") + ".." + (toDate != DateTime.MinValue ? $"{toDate:d}" : "");
                var prop = PropValuePair.GenereteWhereElements(nameof(InvPackagingTransClient._Date), typeof(DateTime), dfilter);
                dateFilter.Add(prop);
            }

            ReportingType reportingType = ReportingType.Packing;

            switch (cmbReportingType.SelectedIndex)
            {
                case 0:
                    reportingType = ReportingType.Packing;
                    PackagingType.Visible = true;
                    PackagingRateLevel.Visible = true;
                    WasteSorting.Visible = true;
                    PackagingConsumer.Visible = true;
                    break;
                case 1:
                    reportingType = ReportingType.Batteries;
                    PackagingType.Visible = true;
                    break;
                case 2:
                    PackagingConsumer.Visible = true;
                    reportingType = ReportingType.Electronic;
                    break;
                case 3:
                    reportingType = ReportingType.OneTimeUsePlastic;
                    break;
            }

            busyIndicator.IsBusy = true;
            busyIndicator.BusyContent = string.Concat(Uniconta.ClientTools.Localization.lookup("Loading"));
            var country = (CountryCode)cmbCountry.SelectedIndex;
            var reporting = AppEnums.PackagingReportingType.ToString((byte)reportingType);
            var reportedByCustomer = ReportedByCustomer.IsChecked.GetValueOrDefault();

            if (reportedByCustomer)
            {
                DebtorAccount.Visible = true;
                DebtorName.Visible = true;
                InvoiceNumber.Visible = true;
            }

            var packLst = (await api.Query<InvPackagingTransClient>(dateFilter)).Where(p => p.ReportingType == reporting && p._ReportedByCustomer == reportedByCustomer && (p.Country == country || p.Country == null)).ToList();

            if (AppendNotPosted.IsChecked.GetValueOrDefault())
            {
                var packLstByJournalAndItem = packLst.GroupBy(l => (l.JournalPostedId, l.Item)).ToDictionary(g => g.Key, g => g.ToArray());

                var invoices = await api.Query<DebtorInvoiceClient>(dateFilter);
                if (invoices.Length > 0)
                {
                    var allLines = await api.Query<DebtorInvoiceLines>(dateFilter);
                    if (allLines == null || allLines.Length == 0)
                    {
                        busyIndicator.IsBusy = false;
                        return;
                    }
                    var linesByJournal = allLines.GroupBy(l => l.JournalPostedId).ToDictionary(g => g.Key, g => g.ToArray());
                    var linesByInvoice = allLines.GroupBy(l => l.InvoiceNumber).ToDictionary(g => g.Key, g => g.ToArray());

                    var packagingProducts = (await api.Query<InvPackagingProductClient>()).Where(p => p.ReportingType == reporting).ToArray();
                    var packagingProductsByItem = new Dictionary<string, List<InvPackagingProductClient>>();
                    if (packagingProducts != null && packagingProducts.Length > 0)
                    {
                        foreach (var p in packagingProducts.Where(p => !string.IsNullOrEmpty(p._Item)))
                        {
                            if (!packagingProductsByItem.TryGetValue(p._Item, out var list))
                            {
                                list = new List<InvPackagingProductClient>();
                                packagingProductsByItem[p._Item] = list;
                            }
                            list.Add(p);
                        }
                    }

                    var itemsWithModel = items?.Where(s => s._PackingModel != null);
                    if (itemsWithModel != null && itemsWithModel.Any())
                    {
                        var invPackModelLines = (await api.Query<InvPackingProductModelLine>()).Where(p => p._Reporting == reportingType).ToArray();
                        if (invPackModelLines != null && invPackModelLines.Length > 0)
                        {
                            var invPackModelLinesDict = invPackModelLines.GroupBy(l => l._Model).ToDictionary(g => g.Key, g => g.ToArray());
                            foreach (var item in itemsWithModel)
                            {
                                if (!invPackModelLinesDict.TryGetValue(item._PackingModel, out var modelLines))
                                    continue;
                                foreach (var line in modelLines)
                                {
                                    packagingProductsByItem.TryGetValue(item._Item, out var existingPacks);
                                    var packList = existingPacks != null ? existingPacks.ToList() : new List<InvPackagingProductClient>();

                                    packList.Add(new InvPackagingProductClient
                                    {
                                        CompanyId = api.CompanyId,
                                        _Item = item._Item,
                                        _Category = line._Category,
                                        _Packaging = line._Packaging,
                                        _WasteSorting = line._WasteSorting,
                                        _PackagingRateLevel = line._PackagingRateLevel,
                                        _PaymentGrouping = line._PaymentGrouping,
                                        _NoReporting = line._NoReporting,
                                        _Weight = line._Weight,
                                        _Price1 = line._Price1,
                                        _Price2 = line._Price2,
                                    });

                                    packagingProductsByItem[item._Item] = packList;
                                }
                            }
                        }
                    }

                    int cntTotal = invoices.Length;
                    int cnt = 0;
                   
                    foreach (var rec in invoices)
                    {
                        cnt++;
                        if (cnt == 1 || cnt == cntTotal || (cnt % 100) == 0)
                            busyIndicator.BusyContent = string.Concat(Uniconta.ClientTools.Localization.lookup("Loading") + " " + NumberConvert.ToString(cnt), " af ", cntTotal);

                        var debtor = (Debtor)debtors.Get(rec._DCAccount);
                        if (debtor == null)
                            continue;

                        var delCountry = ResolveDeliveryCountry(rec, debtor, installations, debtors);
                        if (delCountry != country)
                            continue;

                        string convertAccount = null;
                        int? convertInvoicenumber = null;
                        DebtorInvoiceLines[] lines;
                        if (rec._JournalPostedId != 0)
                        {
                            if (!linesByJournal.TryGetValue(rec._JournalPostedId, out lines))
                                continue;
                        }
                        else
                        {
                            if (!linesByInvoice.TryGetValue((int)rec._InvoiceNumber, out lines))
                                continue;
                            convertAccount = rec.Account;
                            convertInvoicenumber = (int)rec.InvoiceNumber;
                        }

                        var consumer = debtor.GetConsumer();

                        var cntTest = lines.Length;
                        foreach (var line in lines)
                        {
                            var item = (InvItem)items.Get(line._Item);
                            if (item == null)
                                continue;

                            if (!packagingProductsByItem.TryGetValue(line.Item, out var packsForItem))
                                continue;

                            if (packLstByJournalAndItem.TryGetValue((line.JournalPostedId, line.Item), out var packTransLst))
                                continue;

                            foreach (var pack in packsForItem)
                            {
                                var isReportByCustomer = (pack._NoReporting || debtor._NoPackagingReporting) && !debtor._MicroEnterprise;
                                if (!isReportByCustomer && reportedByCustomer)
                                    continue;

                                if (pack._PaymentGrouping != Uniconta.DataModel.PackagingConsumer.None)
                                    consumer = pack._PaymentGrouping == Uniconta.DataModel.PackagingConsumer.Household;

                                packLst.Add(new InvPackagingTransClient
                                {
                                    CompanyId = api.CompanyId,
                                    _Category = pack._Category,
                                    _WasteSorting = pack._WasteSorting,
                                    _PackagingRateLevel = pack._PackagingRateLevel,
                                    _Consumer = consumer,
                                    _Country = delCountry == api.CompanyEntity._CountryId ? 0 : delCountry,
                                    _Item = item._Item,
                                    _Date = line._Date,
                                    _JournalPostedId = line.JournalPostedId,
                                    _Packaging = pack._Packaging,
                                    _Weight = -line.Qty * pack._Weight,
                                    _Price = consumer ? pack.PriceHousehold : pack.PriceBusiness,
                                    IsCreated = true,
                                    _ConvertAccount = convertAccount,
                                    _ConvertInvoiceNumber = convertInvoicenumber
                                });
                            }
                        }
                    }
                }

            }

            if (packLst == null)
                dgInvPackagingTransGrid.ItemsSource = null;
            else
            {
                dgInvPackagingTransGrid.SetSource(packLst.ToArray());
                dgInvPackagingTransGrid.SortBy(JournalPostedId);
            }
            busyIndicator.IsBusy = false;
        }

        private static CountryCode ResolveDeliveryCountry(DebtorInvoiceClient invoice, Debtor debtor, SQLTableCache<WorkInstallation> installations, SQLTableCache<Debtor> debtorsCache)
        {
            var delCountry = debtor != null ? debtor._Country : CountryCode.Unknown;
            if (invoice.DeliveryCountry != null)
                return invoice.DeliveryCountry.GetValueOrDefault();

            if (invoice.Installation != null)
            {
                var ins = (WorkInstallation)installations.Get(invoice.Installation);
                if (ins != null && ins._Country != CountryCode.Unknown)
                    return ins._Country;
            }

            if (invoice.DeliveryAccount != null && invoice.DeliveryAccount != invoice.Account)
            {
                var dc = (Debtor)debtorsCache.Get(invoice.DeliveryAccount);
                if (dc != null)
                    return dc._Country;
            }

            return delCountry;
        }

        private void Compress()
        {
            if (string.IsNullOrWhiteSpace(comp._Id))
            {
                UnicontaMessageBox.Show(string.Concat(Localization.lookup("Company"), " ", string.Format(Localization.lookup("MissingOBJ"), Localization.lookup("CompanyRegNo"))), Localization.lookup("Warning"));
                return;
            }

            try
            {
                var lst = (IEnumerable<InvPackagingTransClient>)dgInvPackagingTransGrid.GetVisibleRows();

                var reportingBatteries = cmbReportingType.SelectedIndex == 1;
                var dict = new Dictionary<InvPackagingTransClient, InvPackagingTransClient>(new CompressCompare(reportingBatteries));
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
                    PackagingType.Visible = reportingBatteries;
                    Country.Visible = false;
                    Item.Visible = false;
                    Name.Visible = false;
                    IsCreated.Visible = false;
                    DebtorAccount.Visible = false;
                    DebtorName.Visible = false;
                    InvoiceNumber.Visible = false;

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
            private readonly bool includePackagingType;

            public CompressCompare(bool _includePackagingType)
            {
                includePackagingType = _includePackagingType;
            }
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

                return !includePackagingType || string.Compare(x.PackagingType, y.PackagingType) == 0;
            }
            public int GetHashCode(InvPackagingTransClient x)
            {
                var hash =  Util.GetHashCode(x.Period) * Util.GetHashCode(x.Category) * Util.GetHashCode(x.WasteSorting) * Util.GetHashCode(x.PackagingRateLevel) * Util.GetHashCode(x.PackagingConsumer);
                return includePackagingType ? hash * Util.GetHashCode(x.PackagingConsumer) : hash;
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

            if (cmbReportingType.SelectedIndex == 1)
            {
                key = "PackagingType";
                if (!dictionaryColumnIndices.ContainsKey(key))
                    dictionaryColumnIndices.Add(key, idx++);
            }

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
            lbAppendNotPosted.Text = string.Format(Localization.lookup("CreateMissingTransactionsOBJ"), cmbReportingType.Text.ToLower());
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (debtors == null)
                debtors = await api.LoadCache<Uniconta.DataModel.Debtor>().ConfigureAwait(false);
            if (debtorGroups == null)
                debtorGroups = await api.LoadCache<Uniconta.DataModel.DebtorGroup>().ConfigureAwait(false);
            if (items == null)
                items = await api.LoadCache<Uniconta.DataModel.InvItem>().ConfigureAwait(false);
            if (installations == null)
                installations = await api.LoadCache<Uniconta.DataModel.WorkInstallation>().ConfigureAwait(false);
        }
    }
}
