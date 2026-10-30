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
using Uniconta.API.DebtorCreditor;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
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

        private string _selectedFieldPath;

        [Browsable(false)]
        public string SelectedFieldPath
        {
            get => _selectedFieldPath;
            set
            {
                var previousPath = DisplayFieldPath;
                _selectedFieldPath = value;
                if (!string.Equals(previousPath, DisplayFieldPath, StringComparison.Ordinal))
                    FieldDefinition = null;
                NotifyPropertyChanged(nameof(SelectedFieldPath));
                NotifyPropertyChanged(nameof(DisplayFieldPath));
            }
        }

        [Browsable(false)]
        public string DisplayFieldPath => SelectedFieldPath ?? eDeliveryTag?.Name;

        [Browsable(false)]
        public bool CanChooseField { get; set; } = true;

        private eDeliveryFieldDefinition _fieldDefinition;

        [Browsable(false)]
        public eDeliveryFieldDefinition FieldDefinition
        {
            get => _fieldDefinition;
            set
            {
                _fieldDefinition = value;
                NotifyPropertyChanged(nameof(CanRemoveTag));
            }
        }

        [Browsable(false)]
        public bool CanRemoveTag => FieldDefinition != null && !FieldDefinition.IsProtected &&
            !FieldDefinition.IsContainer && FieldDefinition.MinOccurs == 0;
    }

    public class EDeliveryMappingSort : IComparer
    {
        public int Compare(object x, object y)
        {
            var xTag = ((EDeliveryMappingClientExtended)x)?.DisplayFieldPath;
            var yTag = ((EDeliveryMappingClientExtended)y)?.DisplayFieldPath;
            return string.Compare(xTag, yTag, StringComparison.Ordinal);
        }
    }

    public class EDeliveryMappingGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(EDeliveryMappingClientExtended);
        public override IComparer GridSorting => new EDeliveryMappingSort();
        public override bool Readonly => false;
        public override bool IsAutoSave => false;
        public override bool SingleBufferUpdate => false;

        public override string SetColumnTooltip(object row, ColumnBase col)
        {
            switch (col.FieldName)
            {
                case nameof(eDeliveryMappingClient.RemoveTag):
                    if ((row as EDeliveryMappingClientExtended)?.FieldDefinition?.MinOccurs > 0)
                        return Localization.lookup("Required");
                    return Localization.lookup("eDeliveryRemoveTagHelp");
                case nameof(eDeliveryMappingClient.FallbackDefaultValue):
                    return Localization.lookup("eDeliveryFallbackDefaultValueHelp");
                default:
                    return base.SetColumnTooltip(row, col);
            }
        }

        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            base.SetDefaultValues(dataEntity, selectedIndex);
            ((EDeliveryMappingClientExtended)dataEntity).CanChooseField = true;
        }

        protected override Task<ErrorCodes> SaveChanges(List<UnicontaBaseEntity> rowsToInsert,
            List<CrudAPI.UpdatePair> rowsToUpdate, UnicontaBaseEntity[] rowsToUpdateSimple,
            List<UnicontaBaseEntity> rowsToDelete, int NoRowsToUpdate)
        {
            var replacements = rowsToUpdate?.Where(pair =>
                pair.loaded is eDeliveryMappingClient original && pair.modified is eDeliveryMappingClient modified &&
                (original.TableId != modified.TableId || original.TagId != modified.TagId) && original.Group == modified.Group).ToList();
            if ((replacements == null || replacements.Count == 0) &&
                (rowsToInsert == null || rowsToDelete == null))
                return base.SaveChanges(rowsToInsert, rowsToUpdate, rowsToUpdateSimple, rowsToDelete, NoRowsToUpdate);

            // Keep the grid's snapshots and pending changes intact until the transaction succeeds.
            var inserts = new List<UnicontaBaseEntity>(rowsToInsert ?? Enumerable.Empty<UnicontaBaseEntity>());
            var deletes = new List<UnicontaBaseEntity>(rowsToDelete ?? Enumerable.Empty<UnicontaBaseEntity>());
            var updates = new List<CrudAPI.UpdatePair>(rowsToUpdate ?? Enumerable.Empty<CrudAPI.UpdatePair>());
            var changed = replacements != null && replacements.Count > 0;
            if (changed)
            {
                inserts.AddRange(replacements.Select(pair => pair.modified));
                deletes.AddRange(replacements.Select(pair => pair.loaded));
                updates = updates.Except(replacements).ToList();
            }
            // MultiCrud inserts before deleting. Reuse an existing key when a freed tag is selected again.
            for (int i = inserts.Count; --i >= 0;)
            {
                if (!(inserts[i] is eDeliveryMappingClient inserted))
                    continue;
                var index = deletes.FindIndex(record => record is eDeliveryMappingClient deleted &&
                    deleted.CompanyId == inserted.CompanyId && deleted.Group == inserted.Group &&
                    deleted.TagId == inserted.TagId && deleted.TableId == inserted.TableId);
                if (index < 0)
                    continue;

                updates.Add(new CrudAPI.UpdatePair { loaded = deletes[index], modified = inserts[i] });
                inserts.RemoveAt(i);
                deletes.RemoveAt(index);
                changed = true;
            }
            return changed ? api.MultiCrud(inserts, updates, deletes)
                : base.SaveChanges(rowsToInsert, rowsToUpdate, rowsToUpdateSimple, rowsToDelete, NoRowsToUpdate);
        }
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
        private bool _isMappingBusy;

        private readonly Dictionary<string, eDeliveryFieldDefinition[]> _oioUblFieldCache =
            new Dictionary<string, eDeliveryFieldDefinition[]>(StringComparer.Ordinal);
        private eDeliveryFieldDefinition[] _oioUblFields;
        private EDeliveryMappingClientExtended _oioUblMappingRow;
        private int _oioUblFieldRequest;
        private bool _isOioUblFieldLoading;
        private string _oioUblParentPath = string.Empty;
        private int? _lastSelectedRowTableId;
        private bool _shouldRefreshViewer;
        private bool _documentlookupEditorAlreadyFocused;

        public EDeliveryMappingPage(BaseAPI api) : base(api, string.Empty) => Init(null);
        public EDeliveryMappingPage(eDeliveryMappingGroupClient master) : base(null) => Init(master);

        private void Init(eDeliveryMappingGroupClient master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgEdeliveryMappingGrid;
            dgEdeliveryMappingGrid.api = api;
            SetRibbonControl(localMenu, dgEdeliveryMappingGrid);
            dgEdeliveryMappingGrid.BusyIndicator = busyIndicator;
            dgEdeliveryMappingGrid.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent,
                new System.Windows.Input.MouseButtonEventHandler(OioUblFieldButton_PreviewMouseLeftButtonDown), true);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgEdeliveryMappingGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
            dgEdeliveryMappingGrid.tableView.ShowingEditor += MappingTableView_ShowingEditor;
            xmlCache = api.GetCache(typeof(eDeliveryTagTypeClient));
            mapppingGrpCache = api.GetCache(typeof(eDeliveryMappingGroupClient));
            nhrApi = new NHRAPI(api);
            Loaded += async (s, e) => await InitAsync(master);
        }

        private async Task InitAsync(eDeliveryMappingGroupClient master)
        {
            xmlCache = await api.LoadCache(typeof(eDeliveryTagTypeClient), true);
            if (mapppingGrpCache == null)
                mapppingGrpCache = await api.LoadCache(typeof(eDeliveryMappingGroupClient));

            SetMappingGroups(master);
            SyncEntityMasterRowChanged(master);
        }

        public override async Task InitQuery()
        {
            if (dgEdeliveryMappingGrid?.masterRecord == null)
                return;

            await base.InitQuery();
            if (dgEdeliveryMappingGrid.ItemsSource is IEnumerable<EDeliveryMappingClientExtended> rows)
            {
                foreach (var row in rows)
                    SetValue(row);
                var master = dgEdeliveryMappingGrid.masterRecord as eDeliveryMappingGroupClient;
                if (master != null)
                {
                    try
                    {
                        await LoadMappingFieldDefinitionsAsync(rows, parent => GetOioUblFieldsAsync(master._DocVersion, master._DocType, parent));
                    }
                    catch (Exception ex) { api.ReportException(ex); }
                }
            }
        }

        internal static async Task LoadMappingFieldDefinitionsAsync(IEnumerable<EDeliveryMappingClientExtended> rows,
            Func<string, Task<eDeliveryFieldDefinition[]>> load)
        {
            var paths = rows.Where(row => !string.IsNullOrWhiteSpace(row.DisplayFieldPath))
                .Select(row => new { Row = row, Path = row.DisplayFieldPath }).ToList();
            foreach (var group in paths.GroupBy(item => GetOioUblParentPath(item.Path)))
            {
                var fields = await load(group.Key);
                foreach (var item in group)
                    if (string.Equals(item.Row.DisplayFieldPath, item.Path, StringComparison.Ordinal))
                        item.Row.FieldDefinition = fields?.FirstOrDefault(field => field.Path == item.Path);
            }
        }

        private async Task<eDeliveryFieldDefinition[]> GetOioUblFieldsAsync(eDeliveryDocumentVersion version,
            eDeliveryDocumentType type, string parent)
        {
            var cacheKey = version + ":" + type + ":" + parent;
            if (_oioUblFieldCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var fields = await nhrApi.GeteDeliveryDocumentSchema(version, type, parent);
            if (fields != null)
                _oioUblFieldCache[cacheKey] = fields = fields.Where(field => field != null && !string.IsNullOrWhiteSpace(field.Path))
                    .OrderBy(field => field.Path, StringComparer.Ordinal).ToArray();
            return fields;
        }

        private async void OioUblFieldButton_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            while (source != null && source != dgEdeliveryMappingGrid)
            {
                if (source is System.Windows.Controls.Button button && button.Name == "chooseOioUblField")
                {
                    var row = button.Tag as EDeliveryMappingClientExtended;
                    if (row == null)
                        return;

                    // The grid normally uses the first click to focus/activate its cell editor.
                    e.Handled = true;
                    dgEdeliveryMappingGrid.SelectedItem = row;
                    await ShowOioUblFieldPickerAsync(row);
                    return;
                }

                source = source is FrameworkContentElement content
                    ? content.Parent : System.Windows.Media.VisualTreeHelper.GetParent(source);
            }
        }

        private async void ChooseOioUblField_Click(object sender, RoutedEventArgs e) =>
            await ShowOioUblFieldPickerAsync((sender as System.Windows.Controls.Button)?.Tag as EDeliveryMappingClientExtended);

        private async Task ShowOioUblFieldPickerAsync(EDeliveryMappingClientExtended row)
        {
            if (_isMappingBusy || row == null || !row.CanChooseField || oioUblFieldPicker.Visibility == Visibility.Visible)
                return;

            _oioUblMappingRow = row;
            txtOioUblFieldSearch.Text = string.Empty;
            busyIndicator.IsEnabled = false;
            localMenu.IsEnabled = false;
            oioUblFieldPicker.Visibility = Visibility.Visible;
            await LoadOioUblFieldsAsync(GetOioUblParentPath(row.DisplayFieldPath));
        }

        private async Task LoadOioUblFieldsAsync(string parentPath)
        {
            var request = ++_oioUblFieldRequest;
            var master = dgEdeliveryMappingGrid.masterRecord as eDeliveryMappingGroupClient;
            var docType = master?._DocType ?? eDeliveryDocumentType.Invoice;
            var docVersion = master?._DocVersion ?? eDeliveryDocumentVersion.OIOUBL_21;
            _oioUblParentPath = (parentPath ?? string.Empty).Trim('/');

            _isOioUblFieldLoading = true;
            _oioUblFields = null;
            lstOioUblFields.ItemsSource = null;
            txtOioUblFieldSearch.Text = string.Empty;
            txtOioUblGroupPath.Text = _oioUblParentPath;
            oioUblGroupNavigation.IsEnabled = false;
            txtOioUblPickerDocument.Text = Uniconta.ClientTools.AppEnums.eDeliveryDocumentVersion.ToString((int)docVersion)
                + " · " + Uniconta.ClientTools.AppEnums.eDeliveryDocumentType.ToString((int)docType);
            txtOioUblFieldCount.Text = Localization.lookup("LoadingMsg");
            txtOioUblFieldSearch.IsEnabled = false;
            btnUseOioUblField.IsEnabled = false;
            btnRetryOioUblFields.Visibility = Visibility.Collapsed;

            try
            {
                var fields = await GetOioUblFieldsAsync(docVersion, docType, _oioUblParentPath);
                if (request != _oioUblFieldRequest)
                    return;
                if (fields == null)
                {
                    txtOioUblFieldCount.Text = Localization.lookup("Error") + ": " + Localization.lookup(nhrApi.LastError.ToString());
                    btnRetryOioUblFields.Visibility = Visibility.Visible;
                    return;
                }

                _oioUblFields = fields;
                var currentField = fields.FirstOrDefault(field => field.Path == _oioUblMappingRow?.DisplayFieldPath);
                if (_oioUblMappingRow != null && currentField != null)
                    _oioUblMappingRow.FieldDefinition = currentField;
                FilterOioUblFields();
                lstOioUblFields.SelectedItem = fields.FirstOrDefault(f =>
                    string.Equals(f.Path, _oioUblMappingRow?.DisplayFieldPath, StringComparison.Ordinal));
            }
            catch (Exception ex)
            {
                if (request == _oioUblFieldRequest)
                {
                    txtOioUblFieldCount.Text = Localization.lookup("Error") + ": " + ex.Message;
                    btnRetryOioUblFields.Visibility = Visibility.Visible;
                }
            }
            finally
            {
                if (request == _oioUblFieldRequest)
                {
                    _isOioUblFieldLoading = false;
                    oioUblGroupNavigation.IsEnabled = true;
                    btnOioUblGroupUp.IsEnabled = _oioUblParentPath.Length > 0;
                    txtOioUblFieldSearch.IsEnabled = true;
                    btnUseOioUblField.IsEnabled = (lstOioUblFields.SelectedItem as eDeliveryFieldDefinition)?.CanMap == true;
                    if (_oioUblFields != null)
                        txtOioUblFieldSearch.Focus();
                }
            }
        }

        private async void RetryOioUblFields_Click(object sender, RoutedEventArgs e) =>
            await LoadOioUblFieldsAsync(_oioUblParentPath);

        private static string GetOioUblParentPath(string path)
        {
            var separator = path?.LastIndexOf('/') ?? -1;
            return separator < 0 ? string.Empty : path.Substring(0, separator);
        }

        private async void OioUblGroupRoot_Click(object sender, RoutedEventArgs e) =>
            await LoadOioUblFieldsAsync(string.Empty);

        private async void OioUblGroupUp_Click(object sender, RoutedEventArgs e) =>
            await LoadOioUblFieldsAsync(GetOioUblParentPath(_oioUblParentPath));

        private async void OpenOioUblGroup_Click(object sender, RoutedEventArgs e)
        {
            var field = (sender as System.Windows.Controls.Button)?.Tag as eDeliveryFieldDefinition
                ?? lstOioUblFields.SelectedItem as eDeliveryFieldDefinition;
            if (!_isOioUblFieldLoading && field != null && field.IsContainer)
                await LoadOioUblFieldsAsync(field.Path);
        }

        private void OioUblFieldSearch_TextChanged(object sender, TextChangedEventArgs e) =>
            FilterOioUblFields();

        private void FilterOioUblFields()
        {
            if (_oioUblFields == null)
                return;

            var search = txtOioUblFieldSearch.Text.Trim();
            var available = GetAvailableOioUblFields(_oioUblFields,
                dgEdeliveryMappingGrid.ItemsSource as IEnumerable<EDeliveryMappingClientExtended>, _oioUblMappingRow).ToArray();
            var fields = available.Where(f =>
                f.Path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                (f.Name?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
                (f.Description?.IndexOf(search, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0).ToArray();
            lstOioUblFields.ItemsSource = fields;
            txtOioUblFieldCount.Text = Localization.lookup("Fields") + ": " + fields.Length + " / " + available.Length;
        }

        internal static IEnumerable<eDeliveryFieldDefinition> GetAvailableOioUblFields(IEnumerable<eDeliveryFieldDefinition> fields,
            IEnumerable<EDeliveryMappingClientExtended> mappings, EDeliveryMappingClientExtended current)
        {
            var used = new HashSet<string>((mappings ?? Enumerable.Empty<EDeliveryMappingClientExtended>())
                .Where(row => !ReferenceEquals(row, current)).Select(row => row.DisplayFieldPath), StringComparer.Ordinal);
            return fields.Where(field => field != null && !string.IsNullOrWhiteSpace(field.Path) &&
                !field.IsProtected && (field.IsContainer || (field.CanMap && !used.Contains(field.Path))));
        }

        private void OioUblFields_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var field = lstOioUblFields.SelectedItem as eDeliveryFieldDefinition;
            txtOioUblFieldType.Text = field == null ? string.Empty : GetOioUblFieldTypeLabel(field);
            txtOioUblFieldRequirement.Text = field == null ? string.Empty
                : Localization.lookup(field.MinOccurs > 0 ? "Required" : "Optional");
            txtOioUblFieldRepetitions.Text = field == null ? string.Empty : GetOioUblFieldRepetitionsLabel(field);
            if (btnUseOioUblField != null)
                btnUseOioUblField.IsEnabled = !_isOioUblFieldLoading && field?.CanMap == true;
        }

        private static string GetOioUblFieldTypeLabel(eDeliveryFieldDefinition field)
        {
            if (field.IsContainer)
                return Localization.lookup("Group");

            string label;
            switch (field.DataType)
            {
                case "String":
                case "NormalizedString": label = "Text"; break;
                case "Decimal": label = "Decimal"; break;
                case "Boolean": return Localization.lookup("Yes") + "/" + Localization.lookup("No");
                case "Date": label = "Date"; break;
                case "Time": label = "Time"; break;
                case "Base64Binary": label = "Attachment"; break;
                default: label = field.DataType ?? "Unknown"; break;
            }
            return Localization.lookup(label);
        }

        private static string GetOioUblFieldRepetitionsLabel(eDeliveryFieldDefinition field)
        {
            if (!field.MaxOccurs.HasValue)
                return string.Format(Localization.lookup("AtLeastOBJ"), field.MinOccurs);

            if (field.MinOccurs == field.MaxOccurs.Value)
                return string.Format(Localization.lookup("ExactlyOBJ"), field.MinOccurs);
            return string.Format(Localization.lookup("FromTo"), field.MinOccurs, field.MaxOccurs.Value);
        }

        private void UseOioUblField_Click(object sender, RoutedEventArgs e)
        {
            if (_isOioUblFieldLoading || _oioUblMappingRow == null || !_oioUblMappingRow.CanChooseField ||
                !(lstOioUblFields.SelectedItem is eDeliveryFieldDefinition field) || !field.CanMap)
                return;

            var changed = !string.Equals(_oioUblMappingRow.DisplayFieldPath, field.Path, StringComparison.Ordinal) ||
                (field.MinOccurs > 0 && _oioUblMappingRow.RemoveTag);
            if (changed)
                dgEdeliveryMappingGrid.SetLoadedRow(_oioUblMappingRow);

            _oioUblMappingRow.SelectedFieldPath = field.Path;
            _oioUblMappingRow.FieldDefinition = field;
            if (field.MinOccurs > 0)
                _oioUblMappingRow.RemoveTag = false;
            if (changed)
            {
                dgEdeliveryMappingGrid.SetModifiedRow(_oioUblMappingRow);
                MarkNonFallbackChanged();
            }
            CloseOioUblFieldPicker();
        }

        private void CancelOioUblField_Click(object sender, RoutedEventArgs e) => CloseOioUblFieldPicker();

        private void OioUblFieldPicker_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                CloseOioUblFieldPicker();
                e.Handled = true;
            }
        }

        private void CloseOioUblFieldPicker()
        {
            ++_oioUblFieldRequest;
            _isOioUblFieldLoading = false;
            oioUblFieldPicker.Visibility = Visibility.Collapsed;
            busyIndicator.IsEnabled = true;
            localMenu.IsEnabled = true;
            _oioUblMappingRow = null;
            dgEdeliveryMappingGrid.Focus();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            var currentMaster = dgEdeliveryMappingGrid.masterRecord as eDeliveryMappingGroupClient;
            if (args is eDeliveryMappingGroupClient master && master != null &&
                master?.RowId != currentMaster?.RowId)
            {
                dgEdeliveryMappingGrid.UpdateMaster(master);
                InitQuery();
                cmbTableIds.ItemsSource = master?.GetTableAndProperties(api.CompanyEntity)?.OrderBy(x => x.DisplayName)?.ToList();
                ClearGridCache();

                _ignoreMappingGroupSelectionChanged = true;
                try { leMappinggroup.SelectedItem = master; }
                finally { _ignoreMappingGroupSelectionChanged = false; }
            }
        }

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

        private void MappingTableView_ShowingEditor(object sender, ShowingEditorEventArgs e)
        {
            var row = dgEdeliveryMappingGrid.GetRow(e.RowHandle) as EDeliveryMappingClientExtended;
            if (row != null && e.Column.FieldName == nameof(row.RemoveTag) && !row.CanRemoveTag)
                e.Cancel = true;
            if (row?.RemoveTag == true && (e.Column.FieldName == nameof(row.TableId) ||
                e.Column.FieldName == nameof(row.Property) || e.Column.FieldName == nameof(row.FallbackDefaultValue)))
                e.Cancel = true;
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
                    xmlDocument = null;
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
                case "RemoveTag":
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
            if (_isMappingBusy)
                return;

            var selectedItem = dgEdeliveryMappingGrid.SelectedItem as EDeliveryMappingClientExtended;
            switch (ActionType)
            {
                case "AddRow":
                    if (dgEdeliveryMappingGrid.masterRecord == null)
                    {
                        UnicontaMessageBox.Show(Localization.lookup("eDeliveryMappingMissingGroup"), Localization.lookup("EDeliveryMapping"));
                        return;
                    }
                    var lastRowIndex = ((dgEdeliveryMappingGrid.ItemsSource as IList)?.Count ?? 0) - 1;
                    dgEdeliveryMappingGrid.AddRow(null, lastRowIndex);
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
                    _ = saveGrid();
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
            }

            await SaveAndSendOrExportAsync(HasUnsavedChanges ? saveGrid : (Func<Task<ErrorCodes>>)null, () =>
            {
                if (actionType == "SendUBL")
                    Invoices.SendUBL(new DebtorInvoiceClient[1] { invoice }, api, null, busyIndicator);
                else if (actionType == "ExportUBL")
                    Invoices.ExportUBL(new DebtorInvoiceClient[1] { invoice }, api, null, null, null, mapppingGrpCache, false);
            });
        }

        internal static async Task SaveAndSendOrExportAsync(Func<Task<ErrorCodes>> save, Action sendOrExport)
        {
            if (save != null && await save() != ErrorCodes.Succes)
                return;

            sendOrExport();
        }

        public override void PerformAction(ShortcutAction action)
        {
            if (!_isMappingBusy && oioUblFieldPicker.Visibility != Visibility.Visible)
                base.PerformAction(action);
        }

        protected override async Task<ErrorCodes> saveGrid()
        {
            if (_isMappingBusy || oioUblFieldPicker.Visibility == Visibility.Visible)
                return ErrorCodes.NoSucces;

            _isMappingBusy = true;
            LayoutRoot.IsEnabled = false;
            try
            {
                dgEdeliveryMappingGrid.View.PostEditor();
                RemoveEmptyAddedMappingRows();
                ClearLocalChangeTrackingIfGridIsClean();

                // Only skip validation if fallback is the ONLY thing changed, or all rows were deleted.
                var requiresValidation = dgEdeliveryMappingGrid.HasUnsavedData &&
                    (dgEdeliveryMappingGrid.ItemsSource as IList)?.Count > 0 &&
                    (_nonFallbackChanged || !_fallbackChanged);
                return await saveGrid(() => dgEdeliveryMappingGrid.ItemsSource as IEnumerable<EDeliveryMappingClientExtended>,
                    requiresValidation ? ValidateReturnErrMsg : (Func<Task<string>>)null,
                    () => base.saveGrid(), message => UnicontaMessageBox.Show(message, Localization.lookup("Error")));
            }
            finally
            {
                LayoutRoot.IsEnabled = true;
                _isMappingBusy = false;
            }
        }

        internal async Task<ErrorCodes> saveGrid(Func<IEnumerable<EDeliveryMappingClientExtended>> getRows,
            Func<Task<string>> validate, Func<Task<ErrorCodes>> save, Action<string> showError)
        {
            var protectedRow = getRows()?.FirstOrDefault(row => eDeliveryMappingFieldPolicy.IsProtected(row.DisplayFieldPath, true));
            if (protectedRow != null)
            {
                showError(Localization.lookup(ErrorCodes.FieldCannotBeChangedToNewValue.ToString()) + ": " + protectedRow.DisplayFieldPath);
                return ErrorCodes.FieldCannotBeChangedToNewValue;
            }

            if (validate != null)
            {
                var result = await validate();
                if (result != null)
                {
                    showError(result);
                    return ErrorCodes.NoSucces;
                }
            }

            var saveResult = await save();
            if (saveResult != ErrorCodes.Succes)
                return saveResult;

            foreach (var row in getRows() ?? Enumerable.Empty<EDeliveryMappingClientExtended>())
            {
                row.SelectedFieldPath = null;
            }
            _fallbackChanged = false;
            _nonFallbackChanged = false;
            _shouldRefreshViewer = xmlDocument == null;
            return ErrorCodes.Succes;
        }

        private Task<string> ValidateReturnErrMsg() =>
            ValidateReturnErrMsg(dgEdeliveryMappingGrid.ItemsSource as List<EDeliveryMappingClientExtended>,
                dgEdeliveryMappingGrid.masterRecord as eDeliveryMappingGroupClient,
                () => api.LoadCache(typeof(eDeliveryTagTypeClient), true), tag => api.Insert(tag));

        internal async Task<string> ValidateReturnErrMsg(List<EDeliveryMappingClientExtended> mappings,
            eDeliveryMappingGroupClient grp, Func<Task<SQLCache>> loadCache,
            Func<eDeliveryTagTypeClient, Task<ErrorCodes>> insert)
        {
            if (xmlDocument == null)
            {
                if (mappings == null || mappings.Count == 0)
                    return Localization.lookup(ErrorCodes.CouldNotFind.ToString());

                xmlCache = await loadCache();
                if (xmlCache == null)
                    return Localization.lookup(api.LastError.ToString());
                foreach (var row in mappings)
                    row.NotifyPropertyChanged(nameof(row.DisplayFieldPath));

                if (mappings.Any(m => string.IsNullOrWhiteSpace(m.DisplayFieldPath)))
                    return Localization.lookup(ErrorCodes.FieldCannotBeBlank.ToString()) + ": Tag";

                if (mappings.GroupBy(m => m.DisplayFieldPath, StringComparer.Ordinal).Any(g => g.Count() > 1))
                    return Localization.lookup("eDeliveryMappingDuplicateTags");

                if (mappings.Any(m => !m.RemoveTag && (m.TableId == 0 || string.IsNullOrWhiteSpace(m.Property))))
                    return Localization.lookup(ErrorCodes.FieldCannotBeBlank.ToString()) + ": " + Localization.lookup("Field");

                if (invoice == null)
                    return Localization.lookup("ZeroInvoice");

                var mappingsWithTags = mappings
                    .Select(m => new eDeliveryMappingValidationRequest { Mapping = m, TagPath = m.DisplayFieldPath})
                    .ToList();

                var validatedDocument = await nhrApi.GetValidatedeDeliveryMappingDoc(invoice, mappingsWithTags);
                if (validatedDocument == null)
                    return Localization.lookup(nhrApi.LastError.ToString());

                for (int i = 0; i < mappings.Count; i++)
                {
                    var r = mappings[i];
                    var tag = xmlCache?
                        .GetNotNullArray?
                        .Select(t => t as eDeliveryTagTypeClient)?
                        .FirstOrDefault(t => t.Name == mappingsWithTags[i]?.TagPath &&
                                             t.DocVersion == grp.DocVersion &&
                                             t.DocType == grp.DocType);

                    if (tag == null)
                    {
                        tag = new eDeliveryTagTypeClient
                        {
                            Name = mappingsWithTags[i]?.TagPath,
                            DocVersion = grp.DocVersion,
                            DocType = grp.DocType
                        };

                        var err = await insert(tag);
                        if (err != ErrorCodes.Succes)
                        {
                            xmlDocument = null;
                            return Localization.lookup(err.ToString());
                        }

                        // Keep the saved field visible after SelectedFieldPath is cleared.
                        if (xmlCache.Get(tag.RowId) == null)
                            xmlCache.Add(tag);
                    }

                    if (r.TagId != tag.RowId)
                        r.TagId = tag.RowId;
                }

                // Assign after TagId notifications, which invalidate the previous document.
                xmlDocument = validatedDocument;
            }

            return null;
        }

        protected async void ViewVoucher()
        {
            if (_isMappingBusy || oioUblFieldPicker.Visibility == Visibility.Visible)
                return;

            dgEdeliveryMappingGrid.View.PostEditor();
            if (_shouldRefreshViewer)
                this.xmlDocument = null;

            var header = string.Format(Localization.lookup("ViewOBJ"), "XML");
            if (xmlDocument == null)
            {
                _isMappingBusy = true;
                LayoutRoot.IsEnabled = false;
                try
                {
                    var err = await ValidateReturnErrMsg();
                    if (err != null)
                    {
                        UnicontaMessageBox.Show(err, Localization.lookup("Error"));
                        return;
                    }
                }
                finally
                {
                    LayoutRoot.IsEnabled = true;
                    _isMappingBusy = false;
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
            if (dgEdeliveryMappingGrid.ItemsSource is IEnumerable<EDeliveryMappingClientExtended> rows)
                foreach (var row in rows)
                    SetValue(row);
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

            var saved = await saveGrid();
            return saved == ErrorCodes.Succes && !HasUnsavedChanges && !dgEdeliveryMappingGrid.HasUnsavedData;
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
            string.IsNullOrWhiteSpace(row.SelectedFieldPath) &&
            row.TableId == 0 &&
            string.IsNullOrWhiteSpace(row.Property) &&
            !row.RemoveTag;

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

            SetValue(rec, invoice, (CompanyClient)api.CompanyEntity);
            Value.Visible = true;
        }

        internal static void SetValue(EDeliveryMappingClientExtended rec, DebtorInvoiceClient invoice, CompanyClient company)
        {
            if (rec == null)
                return;

            if (rec.RemoveTag)
            {
                rec.Value = null;
                return;
            }

            if (invoice == null)
                return;

            var value = rec.GetTablePropertyValueFromEntity(invoice, company);
            if (string.IsNullOrWhiteSpace(value) && invoice.InvoiceLines != null)
                value = rec.GetTablePropertyValueFromEntity(invoice.InvoiceLines.FirstOrDefault(), company);

            rec.Value = !string.IsNullOrEmpty(value) ? value : "{" + Localization.lookup("Empty") + "}";
        }

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

                var rows = dgEdeliveryMappingGrid.ItemsSource as IEnumerable<EDeliveryMappingClientExtended>;
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
            if (oioUblFieldPicker.Visibility == Visibility.Visible)
                CloseOioUblFieldPicker();
            _oioUblMappingRow = null;
            _fallbackChanged = false;
            _nonFallbackChanged = false;
            _shouldRefreshViewer = true;
            xmlDocument = null;
        }
    }
}
