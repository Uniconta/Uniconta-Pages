using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml;
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;
using UnicontaClient.Models;
using Localization = Uniconta.ClientTools.Localization;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EDeliveryMappingClientExtended : eDeliveryMappingClient
    {
        private string _Value;

        [Display(Name = "Value", ResourceType = typeof(TableFieldsText))]
        public string Value
        {
            get => _Value;
            set
            {
                _Value = value;
                NotifyPropertyChanged("Value");
            }
        }

        public object PropertySource { get; set; }
    }

    public class EDeliveryMappingSort : IComparer
    {
        public int Compare(object x, object y)
        {
            var xTag = ((EDeliveryMappingClientExtended)x)?.eDeliveryTag?.Name;
            var yTag = ((EDeliveryMappingClientExtended)y)?.eDeliveryTag?.Name;
            return string.Compare(xTag, yTag, StringComparison.Ordinal);
        }
    }

    public class EDeliveryMappingGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(EDeliveryMappingClientExtended);
        public override IComparer GridSorting => new EDeliveryMappingSort();
        public override bool Readonly => false;
        public override bool IsAutoSave => false;
    }

    public partial class EDeliveryMappingPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EDeliveryMappingPage; } }

        private NHRAPI nhrApi;
        private SQLCache mapppingGrpCache;
        private SQLCache xmlCache;

        private DebtorInvoiceClient invoice;
        private string xmlDocument;

        // Either fallback changes (no validation needed) or non-fallback changes (validation needed)
        private bool _fallbackChanged;
        private bool _nonFallbackChanged;
        private bool HasUnsavedChanges => _fallbackChanged || _nonFallbackChanged;
        private bool _ignoreMappingGroupSelectionChanged;

        public EDeliveryMappingPage(BaseAPI api) : base(api, string.Empty) => Init(null);
        public EDeliveryMappingPage(eDeliveryMappingGroupClient master) : base(null) => Init(master);

        private void Init(eDeliveryMappingGroupClient master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgEdeliveryMappingGrid;
            dgEdeliveryMappingGrid.api = api;
            SetRibbonControl(localMenu, dgEdeliveryMappingGrid);
            dgEdeliveryMappingGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgEdeliveryMappingGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            xmlCache = api.GetCache(typeof(eDeliveryTagTypeClient));
            mapppingGrpCache = api.GetCache(typeof(eDeliveryMappingGroupClient));
            nhrApi = new NHRAPI(api);
            Loaded += async (s, e) => await InitAsync(master);
        }

        private async Task InitAsync(eDeliveryMappingGroupClient master)
        {
            if (xmlCache == null)
                xmlCache = await api.LoadCache(typeof(eDeliveryTagTypeClient));
            if (mapppingGrpCache == null)
                mapppingGrpCache = await api.LoadCache(typeof(eDeliveryMappingGroupClient));

            SetMappingGroups(master);
            SyncEntityMasterRowChanged(master);
        }

        public override Task InitQuery()
        {
            if (dgEdeliveryMappingGrid?.masterRecord == null)
                return Task.CompletedTask;
            return base.InitQuery();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            var currentMaster = dgEdeliveryMappingGrid.masterRecord as eDeliveryMappingGroupClient;
            if (args is eDeliveryMappingGroupClient master && master != null &&
                master?.RowId != currentMaster?.RowId)
            {
                dgEdeliveryMappingGrid.UpdateMaster(master);
                InitQuery();
                SetXmlTags(master);
                cmbTableIds.ItemsSource = master?.GetTableAndProperties(api.CompanyEntity)?.OrderBy(x => x.DisplayName)?.ToList();
                ClearGridCache();

                _ignoreMappingGroupSelectionChanged = true;
                try { leMappinggroup.SelectedItem = master; }
                finally { _ignoreMappingGroupSelectionChanged = false; }
            }
        }

        private int? _lastSelectedRowTableId;
        private void DataControl_CurrentItemChanged(object sender, CurrentItemChangedEventArgs e)
        {
            if (e.OldItem is EDeliveryMappingClientExtended oldItem)
                oldItem.PropertyChanged -= eDeliveryMappingClient_PropertyChanged;

            if (e.NewItem is EDeliveryMappingClientExtended newItem)
            {
                _lastSelectedRowTableId = newItem.TableId;
                newItem.PropertyChanged += eDeliveryMappingClient_PropertyChanged;
            }
        }

        private void eDeliveryMappingClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var rec = sender as EDeliveryMappingClientExtended;
            if (rec == null)
                return;

            switch (e.PropertyName)
            {
                case "FallbackDefaultValue":
                    _fallbackChanged = true;
                    _shouldRefreshViewer = true;
                    return;
                case "TagId":
                    MarkNonFallbackChanged();
                    return;
                case "TableId":
                    MarkNonFallbackChanged();
                    if (_lastSelectedRowTableId != rec.TableId)
                    {
                        _lastSelectedRowTableId = rec.TableId;
                        SetProperty(rec, true);
                    }
                    return;
                case "Property":
                    MarkNonFallbackChanged();
                    SetValue(rec);
                    return;
            }
        }

        private void MarkNonFallbackChanged()
        {
            _nonFallbackChanged = true;
            _shouldRefreshViewer = true;
            xmlDocument = null; // Non-fallback changes invalidate the validated doc
        }


        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended;
            switch (ActionType)
            {
                case "AddRow":
                    if (dgEdeliveryMappingGrid.masterRecord == null)
                    {
                        UnicontaMessageBox.Show(Localization.lookup("eDeliveryMappingMissingGroup"), Localization.lookup("EDeliveryMapping"));
                        return;
                    }
                    dgEdeliveryMappingGrid.AddRow();
                    MarkNonFallbackChanged();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                    {
                        selectedItem.PropertyChanged -= eDeliveryMappingClient_PropertyChanged;
                        dgEdeliveryMappingGrid.DeleteRow();
                        MarkNonFallbackChanged();
                    }
                    break;
                case "SaveGrid":
                    _ = ValidateAndSaveAsync();
                    break;
                case "View":
                    ViewVoucher();
                    break;
                case "SendUBL":
                case "ExportUBL":
                    SaveAndSendOrExportAsync(ActionType);
                    break;
                case "DocSendLog":
                    if (invoice != null)
                        AddDockItem(TabControls.DocsSendLogGridPage, invoice);
                    break;
                case "ImportUBL":
                    CustomXmlFile();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private async void SaveAndSendOrExportAsync(string actionType)
        {
            if (invoice == null)
            {
                UnicontaMessageBox.Show(Localization.lookup("ZeroInvoice"), Localization.lookup("Error"));
                return;
            }

            if (HasUnsavedChanges)
            {
                var msg = UnicontaMessageBox.Show(
                    string.Format(Localization.lookup("SaveChangesFor"), Localization.lookup("EDeliveryMapping")),
                    Localization.lookup("EDeliveryMapping"),
                    MessageBoxButton.YesNo);

                if (msg != MessageBoxResult.Yes)
                    return;

                var saved = await ValidateAndSaveAsync();

                // If save failed (changes are still pending), don't proceed
                if (!saved)
                    return;
            }

            if (actionType == "SendUBL")
                Invoices.SendUBL(new DebtorInvoiceClient[1] { invoice }, api, null, busyIndicator);
            else if (actionType == "ExportUBL")
                Invoices.ExportUBL(new DebtorInvoiceClient[1] { invoice }, api, null, null, null, mapppingGrpCache, false);
        }

        private async Task<bool> ValidateAndSaveAsync()
        {
            // Only skip validation if fallback is the ONLY thing changed
            if (_nonFallbackChanged || (!_fallbackChanged && !_nonFallbackChanged))
            {
                var result = await ValidateReturnErrMsg();
                if (result != null)
                {
                    UnicontaMessageBox.Show(result, Localization.lookup("Error"));
                    return false;
                }
            }

            var saveResult = await saveGrid();
            if (saveResult != ErrorCodes.Succes)
                return false;

            _fallbackChanged = false;
            _nonFallbackChanged = false;
            _shouldRefreshViewer = xmlDocument == null;
            return true;
        }

        private async Task<string> ValidateReturnErrMsg()
        {
            if (xmlDocument == null)
            {
                if (!(dgEdeliveryMappingGrid.ItemsSource is List<EDeliveryMappingClientExtended> mappings) ||
                    mappings == null || mappings.Count == 0)
                    return Localization.lookup(ErrorCodes.CouldNotFind.ToString());

                if (mappings.Any(m => m.TagId == 0))
                    return Localization.lookup(ErrorCodes.FieldCannotBeBlank.ToString()) + ": Tag";

                if (mappings.GroupBy(x => x.TagId).Any(g => g.Count() > 1))
                    return Localization.lookup("eDeliveryMappingDuplicateTags");

                xmlDocument = await nhrApi.GetValidatedeDeliveryMappingDoc(invoice, mappings.Select(m => (eDeliveryMapping)m).ToList());
                if (xmlDocument == null)
                    return Localization.lookup(nhrApi.LastError.ToString());
            }

            return null;
        }

        private bool _shouldRefreshViewer;
        protected async void ViewVoucher()
        {
            if (_shouldRefreshViewer)
                this.xmlDocument = null;

            var header = string.Format(Localization.lookup("ViewOBJ"), "XML");
            if (xmlDocument == null)
            {
                var err = await ValidateReturnErrMsg();
                if (err != null)
                {
                    UnicontaMessageBox.Show(err, Localization.lookup("Error"));
                    return;
                }
            }

            var voucher = new VouchersClient
            {
                _Data = Encoding.UTF8.GetBytes(xmlDocument),
                _Fileextension = FileextensionsTypes.XML
            };

            var voucherViewer = new VoucherViewerWindow(invoice, this.api, header);
            voucherViewer._LoadInitMaster(invoice, voucher, 0, true);
            voucherViewer.Owner = UtilDisplay.GetCurentWindow();
            if (VoucherViewerWindow.lastHeight != 0)
            {
                voucherViewer.Width = VoucherViewerWindow.lastWidth;
                voucherViewer.Height = VoucherViewerWindow.lastHeight;
            }
            if (VoucherViewerWindow.isMaximized)
                voucherViewer.WindowState = WindowState.Maximized;

            voucherViewer.Show();
            _shouldRefreshViewer = false;
        }

        private async void CustomXmlFile()
        {
            string file = null;
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "XML files (*.xml)|*.xml";
                    openFileDialog.Title = "Select an XML File";
                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                        return;

                    file = File.ReadAllText(openFileDialog.FileName);
                }
            }
            catch { }
            if (file != null)
            {
                var errCode = await nhrApi.SendOIOUBL(file);
                if (errCode != 0)
                    UnicontaMessageBox.Show(Localization.lookup(errCode.ToString()), Localization.lookup("Error"));
            }
            else
                UnicontaMessageBox.Show(Localization.lookup("ViewerFailed"), Localization.lookup("Error"));
        }

        private void SetMappingGroups(eDeliveryMappingGroupClient master)
        {
            if ((mapppingGrpCache == null || mapppingGrpCache.Count == 0) && master != null)
                if (mapppingGrpCache != null && mapppingGrpCache.Get(master.Name) == null)
                    mapppingGrpCache.Add(master);
                else
                    mapppingGrpCache = new SQLCache(new[] { master });

            if (mapppingGrpCache == null || mapppingGrpCache.Count == 0)
                return;

            leMappinggroup.ItemsSource = mapppingGrpCache;
            if (master != null)
                leMappinggroup.SelectedItem = master;
            else
                leMappinggroup.SelectedItem = mapppingGrpCache.GetNotNullArray
                    .FirstOrDefault(g => ((eDeliveryMappingGroupClient)g).IsDefault);
            if (leMappinggroup.SelectedItem == null)
                leMappinggroup.SelectedItem = mapppingGrpCache.First();
        }

        private async void SetXmlTags(eDeliveryMappingGroupClient master)
        {
            if (xmlCache == null || xmlCache.Count == 0)
            {
                var tags = await api.Query<eDeliveryTagTypeClient>();
                xmlCache = new SQLCache(tags);
            }

            cmbTagIds.ItemsSource = xmlCache?.GetNotNullArray?
                .Select(x => (eDeliveryTagTypeClient)x)?
                .Where(t => t._DocVersion == master._DocVersion && t._DocType == master._DocType)?
                .OrderBy(x => x.Name)?
                .ToList();
        }

        private void liMappinggroup_ButtonClicked(object sender) =>
            AddDockItem(TabControls.EDeliveryMappingGroupPage, (BaseAPI)api);

        private async void leMappinggroup_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (_ignoreMappingGroupSelectionChanged)
                return;

            var newMappingGroup = (sender as LookupEditor).SelectedItem as eDeliveryMappingGroupClient;
            var currentMappingGroup = dgEdeliveryMappingGrid.masterRecord as eDeliveryMappingGroupClient;
            if (newMappingGroup == null || currentMappingGroup?.RowId == newMappingGroup.RowId)
                return;

            if (!await HandleUnsavedChangesBeforeGroupChangeAsync())
            {
                RestoreMappingGroupSelection(currentMappingGroup);
                return;
            }

            var docType = newMappingGroup._DocType;

            UnicontaBaseEntity[] result = null;
            UnicontaBaseEntity selected = null;
            if (docType == eDeliveryDocumentType.Invoice || docType == eDeliveryDocumentType.CreditNote)
            {
                var compareOp = docType == eDeliveryDocumentType.Invoice ?
                    CompareOperator.GreaterThanOrEqual : CompareOperator.LessThan;

                result = api.CompanyEntity.DebtorInvoices?.GetRecords?
                    .Where(i => compareOp == CompareOperator.LessThan ? i.TotalAmount < 0 : i.TotalAmount >= 0)?
                    .OrderByDescending(i => i.Date)?
                    .Take(100)?
                    .ToArray();

                if (result == null || result.Length == 0)
                    result = (await api.Query<DebtorInvoiceClient>(
                        new PropValuePair[1] { PropValuePair.GenereteWhereElements("LineTotal", 0L, compareOp) }))?
                        .OrderByDescending(i => i.Date)?
                        .Take(100)?
                        .ToArray();

                selected = invoice = result?.FirstOrDefault() as DebtorInvoiceClient;
                if (invoice != null && invoice.InvoiceLines == null)
                    invoice.InvoiceLines = await api.Query<DebtorInvoiceLines>(invoice) ?? new DebtorInvoiceLines[0];
            }

            if (selected == null)
            {
                tbDocumentNum.Text = null;
                tbDocumentNum.Visibility = Visibility.Collapsed;

                leDocumentNum.ItemsSource = null;
                leDocumentNum.Visibility = Visibility.Collapsed;
                Value.Visible = false;
            }
            else
            {
                tbDocumentNum.Text = newMappingGroup.DocType;
                tbDocumentNum.Visibility = Visibility.Visible;

                ConfigureDocumentLookupEditor();
                leDocumentNum.ItemsSource = result;
                leDocumentNum.SelectedItem = selected;
                leDocumentNum.Visibility = Visibility.Visible;
            }

            SyncEntityMasterRowChanged(newMappingGroup);
        }

        private async Task<bool> HandleUnsavedChangesBeforeGroupChangeAsync()
        {
            dgEdeliveryMappingGrid.View.PostEditor();
            RemoveEmptyAddedMappingRows();
            ClearLocalChangeTrackingIfGridIsClean();

            if (!HasUnsavedChanges && !dgEdeliveryMappingGrid.HasUnsavedData)
                return true;

            var msg = UnicontaMessageBox.Show(
                string.Format(Localization.lookup("SaveChangesFor"), Localization.lookup("EDeliveryMapping")),
                Localization.lookup("EDeliveryMapping"),
                MessageBoxButton.YesNoCancel);

            if (msg == MessageBoxResult.Cancel)
                return false;

            if (msg == MessageBoxResult.No)
            {
                dgEdeliveryMappingGrid.CancelChanges();
                ClearGridCache();
                return true;
            }

            var saved = await ValidateAndSaveAsync();
            return saved && !HasUnsavedChanges && !dgEdeliveryMappingGrid.HasUnsavedData;
        }

        private void RestoreMappingGroupSelection(eDeliveryMappingGroupClient mappingGroup)
        {
            _ignoreMappingGroupSelectionChanged = true;
            try
            {
                leMappinggroup.SelectedItem = mappingGroup;
            }
            finally
            {
                _ignoreMappingGroupSelectionChanged = false;
            }
        }

        private void RemoveEmptyAddedMappingRows()
        {
            var emptyRows = dgEdeliveryMappingGrid.AddedRows?
                .Select(r => r.DataItem as EDeliveryMappingClientExtended)
                .Where(IsEmptyMappingRow)
                .ToList();

            if (emptyRows == null || emptyRows.Count == 0)
                return;

            var source = dgEdeliveryMappingGrid.ItemsSource as IList;
            if (source == null)
                return;

            foreach (var row in emptyRows)
            {
                var rowIndex = source.IndexOf(row);
                if (rowIndex < 0)
                    continue;

                row.PropertyChanged -= eDeliveryMappingClient_PropertyChanged;
                dgEdeliveryMappingGrid.SelectedItem = row;
                dgEdeliveryMappingGrid.View.FocusedRowHandle = dgEdeliveryMappingGrid.GetRowHandleByListIndex(rowIndex);
                dgEdeliveryMappingGrid.DeleteRow(false);
            }
        }

        private static bool IsEmptyMappingRow(EDeliveryMappingClientExtended row) =>
            row != null &&
            row.TagId == 0 &&
            row.TableId == 0 &&
            string.IsNullOrWhiteSpace(row.Property);

        private void ClearLocalChangeTrackingIfGridIsClean()
        {
            if (dgEdeliveryMappingGrid.HasUnsavedData)
                return;

            _fallbackChanged = false;
            _nonFallbackChanged = false;
        }

        private void SetProperty(EDeliveryMappingClientExtended rec, bool resetProperty)
        {
            if (rec == null)
                return;

            if (resetProperty)
                rec.Property = null;

            rec.PropertySource = rec.eDeliveryMappingGroup?
                   .GetTableAndProperties(api.CompanyEntity)?
                   .Where(t => t.Id == rec.TableId)?
                   .SelectMany(t => t.Properties)?
                   .OrderBy(p => p.DisplayName)?
                   .ToList();

            rec.NotifyPropertyChanged("PropertySource");
        }

        private void SetValue(EDeliveryMappingClientExtended rec)
        {
            if (rec == null || invoice == null)
                return;

            var value = rec.GetTablePropertyValueFromEntity(invoice, (CompanyClient)api.CompanyEntity);
            if (string.IsNullOrWhiteSpace(value) && invoice.InvoiceLines != null)
                value = rec.GetTablePropertyValueFromEntity(invoice.InvoiceLines.FirstOrDefault(), (CompanyClient)api.CompanyEntity);

            rec.Value = !string.IsNullOrEmpty(value) ? value : "{" + Localization.lookup("Empty") + "}";
            Value.Visible = true;
        }

        private bool _documentlookupEditorAlreadyFocused;
        private void ConfigureDocumentLookupEditor()
        {
            var lookup = leDocumentNum;
            lookup.api = api;
            lookup.PopupContentTemplate = System.Windows.Application.Current.Resources["LookUpUrlInvoiceClientPopupContent"] as ControlTemplate;
            lookup.ValueMember = "InvoiceNumber";
            lookup.DisplayMember = "InvoiceNum";
        }

        private void leDocumentNum_GotFocus(object sender, RoutedEventArgs e)
        {
            ConfigureDocumentLookupEditor();

            if (_documentlookupEditorAlreadyFocused)
                return;

            leDocumentNum.SelectedIndexChanged += leDocumentNum_SelectedIndexChanged;
            _documentlookupEditorAlreadyFocused = true;
        }

        private async void leDocumentNum_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var master = (eDeliveryMappingGroupClient)leMappinggroup.SelectedItem;
            if (master == null)
                return;

            var uniEntity = leDocumentNum.SelectedItem as UnicontaBaseEntity;
            if (uniEntity is DebtorInvoiceClient inv &&
                ((master._DocType == eDeliveryDocumentType.Invoice && inv.TotalAmount >= 0) ||
                (master._DocType == eDeliveryDocumentType.CreditNote && inv.TotalAmount < 0)))
            {
                invoice = leDocumentNum.SelectedItem as DebtorInvoiceClient;
                if (invoice != null && invoice.InvoiceLines == null)
                    invoice.InvoiceLines = await api.Query<DebtorInvoiceLines>(invoice) ?? new DebtorInvoiceLines[0];

                var rows = dgEdeliveryMappingGrid.ItemsSource as List<EDeliveryMappingClientExtended>;
                if (rows != null)
                {
                    foreach (var row in rows)
                        SetValue(row);

                    dgEdeliveryMappingGrid.RefreshData();
                }

                _shouldRefreshViewer = true;
            }
        }

        private void PART_Editor_GotFocus(object sender, RoutedEventArgs e) =>
            SetProperty(dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended, false);

        private void ClearGridCache()
        {
            _fallbackChanged = false;
            _nonFallbackChanged = false;
            _shouldRefreshViewer = true;
            xmlDocument = null;
        }
    }
}
