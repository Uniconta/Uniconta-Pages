using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.AI;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.AI.Services;
using Uniconta.ClientTools.AI.Services.Context;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.Wizard;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.Utility;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public sealed class TileDataViewModel : NotifyPropertyChangedViewModel
    {
        readonly CrudAPI api;
        readonly UserMenuClient original, working;
        readonly TileCRUDController service;
        readonly Func<TileDataSourceMetadata, IReadOnlyList<TileFieldMetadata>> getFields;
        readonly TileDefinition draft;
        readonly IReadOnlyList<TileDataSourceMetadata> allSources;
        bool refreshingMetadata;
        IReadOnlyList<TileDataSourceMetadata> sources;
        IReadOnlyList<string> numericFields, groupFields;
        IReadOnlyList<TileAggregate> aggregates;
        IReadOnlyList<TileFieldMetadata> fields;
        ObservableCollection<TileValueFieldViewModel> valueFields;
        WizardViewModel wizard;
        ITileTypeWithParms parameterTile;
        public IReadOnlyList<TileParameterViewModel> Parameters { get; private set; } = Array.Empty<TileParameterViewModel>();
        public bool HasParameters => parameterTile != null;
        internal event Action ParameterTypeChanged;

        void LoadParameters()
        {
            parameterTile = null;
            Parameters = Array.Empty<TileParameterViewModel>();
            var type = draft.IsCustomTile && !Plugin.DisableAllPlugins ? Plugin.TileTypeCache.Resolve(draft.TileTypeId) : null;
            if (type != null && typeof(ITileTypeWithParms).IsAssignableFrom(type))
            {
                parameterTile = (ITileTypeWithParms)Activator.CreateInstance(type);
                parameterTile.Deserialize(draft.TileTypeParameters);
                if (parameterTile.Parameters == null) throw new ArgumentException("The tile type did not provide parameter settings.");
                Parameters = System.ComponentModel.TypeDescriptor.GetProperties(parameterTile.Parameters)
                    .Cast<System.ComponentModel.PropertyDescriptor>()
                    .Where(property => property.IsBrowsable && !property.IsReadOnly)
                    .Select(property => new TileParameterViewModel(parameterTile.Parameters, property, () =>
                    {
                        var step = wizard?.Steps.OfType<TileParametersStepViewModel>().FirstOrDefault();
                        if (step != null) Edited?.Invoke(wizard.Steps.IndexOf(step));
                    }, () => wizard == null || (!wizard.IsBusy && !wizard.IsCompleted && wizard.CurrentStep is TileParametersStepViewModel)))
                    .ToArray();
            }
            OnPropertyChanged(nameof(Parameters));
            OnPropertyChanged(nameof(HasParameters));
            ParameterTypeChanged?.Invoke();
        }

        int LogicalStepIndex => wizard.CurrentStep is TilePlacementStepViewModel ? 2
            : wizard.CurrentStep is TileReviewStepViewModel ? 3
            : wizard.CurrentStep is TileConfigurationStepViewModel ? 1
            : wizard.CurrentStep is TileTypeStepViewModel ? 0 : -1;
        string summary, headingStatus, lastSuggestedConfiguration;
        bool addingGroup;

        internal TileDataViewModel(CrudAPI api, TileCRUDController service, UserMenuClient original,
            UserMenuClient working, TileDefinition definition)
            : this(definition, TileMetadataService.GetTileTypeOptions(typeof(IdKey)),
                new TileMetadataService(api).GetDataSources(), new TileMetadataService(api).GetFields,
                UserControlProperties.ControlList.Keys, TilePage.Groups ?? new List<string>())
        {
            this.api = api ?? throw new ArgumentNullException(nameof(api));
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.original = original ?? throw new ArgumentNullException(nameof(original));
            this.working = working ?? throw new ArgumentNullException(nameof(working));
            EditFiltersCommand = new DevExpress.Mvvm.AsyncCommand(() => wizard.RunOperationAsync(token =>
            {
                EditFilters();
                return Task.CompletedTask;
            }), () => wizard != null && !wizard.IsBusy && ShowData && SelectedSource?.Type != null);
            AddGroupCommand = new DevExpress.Mvvm.DelegateCommand(() => IsAddingGroup = true, () => wizard != null && CanEditStep(2));
            SuggestHeadingCommand = new DevExpress.Mvvm.AsyncCommand(() => wizard.RunOperationAsync(SuggestHeadingAsync, true),
                () => wizard != null && !wizard.IsBusy);
            DeleteCommand = new DevExpress.Mvvm.AsyncCommand(DeleteAsync, () => IsExisting && wizard != null && !wizard.IsBusy);
        }

        internal TileDataViewModel(TileDefinition definition, IReadOnlyList<TileTypeMetadata> tileTypes,
            IReadOnlyList<TileDataSourceMetadata> dataSources,
            Func<TileDataSourceMetadata, IReadOnlyList<TileFieldMetadata>> getFields,
            IEnumerable<string> navigationTargets, IEnumerable<string> groups)
        {
            draft = definition ?? throw new ArgumentNullException(nameof(definition));
            this.getFields = getFields ?? throw new ArgumentNullException(nameof(getFields));
            var types = tileTypes.ToList();
            if (draft.IsCustomTile && !types.Any(t => t.Id == draft.TileTypeId))
                types.Add(new TileTypeMetadata { Id = draft.TileTypeId, Caption = draft.TileTypeId + " " + Uniconta.ClientTools.Localization.lookup("TileUnavailable"), IsCustom = true });
            if (!draft.IsCustomTile && !types.Any(t => !t.IsCustom && t.Visualization == draft.Visualization))
                types.Add(new TileTypeMetadata { Id = draft.Visualization.ToString(), Caption = draft.Visualization + " " + Uniconta.ClientTools.Localization.lookup("TileUnavailable"),
                    Visualization = draft.Visualization, RequiresDataSource = draft.Visualization != TileVisualization.HtmlWidget });
            TileTypes = types;
            var availableSources = dataSources.ToList();
            allSources = availableSources;
            // Missing metadata must not replace or erase an existing draft selection.
            if (SelectedSource == null && (draft.TableId > 0 || !string.IsNullOrWhiteSpace(draft.SourceName)))
                availableSources.Add(new TileDataSourceMetadata
                {
                    Name = draft.SourceName, TableId = draft.TableId, IsTable = string.IsNullOrWhiteSpace(draft.SourceName),
                    Caption = (draft.SourceName ?? draft.TableId.ToString()) + " " + Uniconta.ClientTools.Localization.lookup("TileUnavailable")
                });
            LoadParameters();
            RefreshSources(true);
            RefreshFields(false);
            Groups = new ObservableCollection<string>(new[] { string.Empty }.Concat(groups).Distinct());
            if (!string.IsNullOrWhiteSpace(draft.Group) && !Groups.Contains(draft.Group)) Groups.Add(draft.Group);
            NavigationTargets = navigationTargets.Concat(new[] { draft.NavigationTarget })
                .Where(value => !string.IsNullOrWhiteSpace(value)).Concat(new[] { string.Empty }).Distinct().OrderBy(value => value).ToArray();
        }

        internal CrudAPI Api => api;
        internal UserMenuClient Original => original;
        internal UserMenuClient Working => working;

        public IReadOnlyList<TileTypeMetadata> TileTypes { get; }
        public IReadOnlyList<TileDataSourceMetadata> Sources => sources;
        public IReadOnlyList<TileAggregate> Aggregates => aggregates;
        public IEnumerable<TileChoice<TileAggregate>> AggregateOptions => Aggregates.Select(value => new TileChoice<TileAggregate>(value, value.DisplayName()));
        public IReadOnlyList<string> NumericFields => numericFields;
        public IReadOnlyList<string> GroupFields => groupFields;
        public ObservableCollection<TileValueFieldViewModel> ValueFields => valueFields;
        public ObservableCollection<string> Groups { get; }
        public IReadOnlyList<string> NavigationTargets { get; }
        public TileScope[] Scopes => TileDashboardAccess.GetScopes(api);
        public IReadOnlyList<UserProfileClient> Profiles { get; private set; } = Array.Empty<UserProfileClient>();
        public IEnumerable<TileChoice<TileScope>> ScopeOptions => Scopes.Select(value => new TileChoice<TileScope>(value, value.DisplayName()));
        public bool IsExisting => original != null && original.RowId != 0;
        public bool IsDeleted { get; private set; }
        public string Summary { get => summary; private set => SetProperty(ref summary, value); }
        public string HeadingStatus { get => headingStatus; private set => SetProperty(ref headingStatus, value); }
        public string SelectionSummary => Uniconta.ClientTools.Localization.lookup("TileType") + ": " + SelectedTileType?.Caption + (ShowData && SelectedSource != null ? " · " + SelectedSource.Caption : "");
        public bool IsAddingGroup { get => addingGroup; private set => SetProperty(ref addingGroup, value); }
        public bool ShowSize => draft.IsCustomTile;
        public bool ShowHtml => !ShowSize && Visualization == TileVisualization.HtmlWidget;
        public bool ShowData => !ShowSize && !ShowHtml;
        public bool ShowAggregate => ShowData && Visualization != TileVisualization.RecordText;
        public bool ShowMultipleValues => ShowData && Visualization.SupportsMultipleValues();
        public bool ShowSingleValue => ShowData && !ShowMultipleValues;
        public bool ShowGrouping => ShowAggregate && Visualization != TileVisualization.Number;
        public bool ShowChartGroup => ShowGrouping && Visualization.SupportsChartGroup();
        public bool ShowValue2 => ShowData && Visualization.RequiresValue2();
        public bool ShowWeight => ShowData && Visualization == TileVisualization.Bubble;
        public string ValueFieldLabel => Visualization == TileVisualization.RecordText ? Localization.lookup("Fields") : Uniconta.ClientTools.Localization.lookup("TileValueColumns");
        TileVisualization Visualization => draft.Visualization;

        public TileTypeMetadata SelectedTileType
        {
            get => draft.IsCustomTile ? TileTypes.FirstOrDefault(type => type.IsCustom && type.Id == draft.TileTypeId)
                : TileTypes.FirstOrDefault(type => !type.IsCustom && type.Visualization == draft.Visualization);
            set
            {
                // ItemsSource changes can temporarily clear a two-way selection binding.
                if (!CanEditStep(0) || value == null || SelectedTileType == value) return;
                draft.TileTypeId = value.IsCustom ? value.Id : null;
                draft.TileTypeParameters = null;
                LoadParameters();
                draft.Visualization = value.Visualization ?? TileVisualization.Number;
                if ((ShowSize || (Visualization != TileVisualization.Number && Visualization != TileVisualization.RecordText &&
                    Visualization != TileVisualization.HtmlWidget)) && draft.ColumnSpan == 1 && draft.RowSpan == 1)
                {
                    draft.ColumnSpan = draft.RowSpan = 2;
                    OnPropertyChanged(nameof(ColumnSpan)); OnPropertyChanged(nameof(RowSpan));
                }
                if (!ShowData)
                {
                    draft.TableId = 0;
                    draft.SourceName = null;
                    OnPropertyChanged(nameof(SelectedSource));
                }
                OnPropertyChanged(nameof(SelectedTileType));
                RefreshSources();
                // Keep the selections valid for the newly chosen visualization, without
                // changing stored selections during the initial hydration above.
                RefreshFields(true);
                RefreshPresentation();
                Edited?.Invoke(0);
            }
        }

        public TileDataSourceMetadata SelectedSource
        {
            get => !string.IsNullOrWhiteSpace(draft.SourceName)
                ? allSources.FirstOrDefault(source => !source.IsTable && source.Name == draft.SourceName)
                : allSources.FirstOrDefault(source => source.IsTable && source.TableId == draft.TableId && draft.TableId > 0);
            set
            {
                if (!CanEditStep(1) || value == null || SelectedSource == value) return;
                SetSource(value);
            }
        }

        void SetSource(TileDataSourceMetadata value)
        {
            draft.TableId = value?.IsTable == true ? value.TableId : 0;
            draft.SourceName = value != null && !value.IsTable ? value.Name : null;
            draft.Filter = null;
            draft.NavigationTarget = value?.IsTable == true && UserControlProperties.ControlList != null
                ? UserControlProperties.GetControlName(value.TableId) : null;
            OnPropertyChanged(nameof(SelectedSource));
            OnPropertyChanged(nameof(FilterSummary)); OnPropertyChanged(nameof(NavigationTarget));
            RefreshFields(true);
            RefreshPresentation();
            Edited?.Invoke(1);
        }

        public TileAggregate Aggregate { get => draft.Aggregate; set { if (CanEditStep(1) && draft.Aggregate != value) { draft.Aggregate = value; NotifyEdit(1, nameof(Aggregate)); } } }
        public string SingleValue
        {
            get => draft.ValueColumns?.FirstOrDefault();
            set
            {
                if (!CanEditStep(1) || value == null || SingleValue == value) return;
                draft.ValueColumns = string.IsNullOrWhiteSpace(value) ? new List<string>() : new List<string> { value };
                foreach (var field in ValueFields) field.RefreshSelection();
                NotifyEdit(1, nameof(SingleValue));
            }
        }
        public string GroupBy { get => draft.GroupBy; set { if (CanEditStep(1) && value != null && draft.GroupBy != value) { draft.GroupBy = value; NotifyEdit(1, nameof(GroupBy)); } } }
        public string ChartGroup { get => draft.ChartGroup; set { if (CanEditStep(1) && value != null && draft.ChartGroup != value) { draft.ChartGroup = value; NotifyEdit(1, nameof(ChartGroup)); } } }
        public string Value2Field { get => draft.Value2Field; set { if (CanEditStep(1) && value != null && draft.Value2Field != value) { draft.Value2Field = value; NotifyEdit(1, nameof(Value2Field)); } } }
        public string WeightField { get => draft.WeightField; set { if (CanEditStep(1) && value != null && draft.WeightField != value) { draft.WeightField = value; NotifyEdit(1, nameof(WeightField)); } } }
        public string HtmlSource { get => draft.HtmlSource; set { if (CanEditStep(1) && draft.HtmlSource != value) { draft.HtmlSource = value; NotifyEdit(1, nameof(HtmlSource)); } } }
        public int ColumnSpan { get => draft.ColumnSpan; set { if (CanEditStep(1) && draft.ColumnSpan != value) { draft.ColumnSpan = value; NotifyEdit(1, nameof(ColumnSpan)); } } }
        public int RowSpan { get => draft.RowSpan; set { if (CanEditStep(1) && draft.RowSpan != value) { draft.RowSpan = value; NotifyEdit(1, nameof(RowSpan)); } } }
        public TileScope Scope
        {
            get => draft.Scope;
            set
            {
                if (!CanEditStep(2) || draft.Scope == value) return;
                draft.Scope = value;
                NotifyEdit(2, nameof(Scope));
                OnPropertyChanged(nameof(SelectedScope));
                OnPropertyChanged(nameof(IsProfile));
            }
        }
        public bool IsProfile => Scope == TileScope.Profile;
        public string Profile { get => draft.Profile; set { if (CanEditStep(2) && draft.Profile != value) { draft.Profile = value; NotifyEdit(2, nameof(Profile)); } } }
        // A cleared combo-box binding must not coerce null into the enum's default value.
        public TileScope? SelectedScope { get => draft.Scope; set { if (value.HasValue) Scope = value.Value; } }
        public string DashboardTab { get => draft.DashboardTab; set { if (CanEditStep(2) && value != null && draft.DashboardTab != value) { draft.DashboardTab = value; NotifyEdit(2, nameof(DashboardTab)); } } }
        public string Group
        {
            get => draft.Group;
            set
            {
                if (!CanEditStep(2) || value == null || draft.Group == value) return;
                draft.Group = value;
                NotifyEdit(2, nameof(Group));
                if (!IsAddingGroup) OnPropertyChanged(nameof(SelectedGroup));
            }
        }
        public string SelectedGroup
        {
            get => Groups.Contains(Group ?? string.Empty) ? Group ?? string.Empty : null;
            set { if (!IsAddingGroup && value != null) Group = value; }
        }
        public string NavigationTarget { get => draft.NavigationTarget; set { if (CanEditStep(2) && value != null && draft.NavigationTarget != value) { draft.NavigationTarget = value; NotifyEdit(2, nameof(NavigationTarget)); } } }
        public string Title { get => draft.Title; set { if (CanEditStep(3)) SetTitle(value); } }

        void SetTitle(string value)
        {
            if (draft.Title == value) return;
            draft.Title = value;
            NotifyEdit(3, nameof(Title));
        }

        internal void CommitPlacement()
        {
            if (!string.IsNullOrWhiteSpace(draft.Group) && !Groups.Contains(draft.Group)) Groups.Add(draft.Group);
            IsAddingGroup = false;
            OnPropertyChanged(nameof(SelectedGroup));
        }
        public string FilterSummary => string.IsNullOrWhiteSpace(draft.Filter) ? Uniconta.ClientTools.Localization.lookup("TileNoFilter") : Uniconta.ClientTools.Localization.lookup("TileFilterConfigured");
        public DevExpress.Mvvm.AsyncCommand EditFiltersCommand { get; }
        public DevExpress.Mvvm.DelegateCommand AddGroupCommand { get; }
        public DevExpress.Mvvm.AsyncCommand SuggestHeadingCommand { get; }
        public DevExpress.Mvvm.AsyncCommand DeleteCommand { get; }
        internal event Action<int> Edited;

        internal void Attach(WizardViewModel owner)
        {
            wizard = owner;
            wizard.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(WizardViewModel.IsBusy))
                {
                    RefreshCommands();
                    if (!wizard.IsBusy) RefreshCurrentStep();
                }
            };
            RefreshCommands();
        }

        // Editors can write transient defaults while their step is created or removed.
        // Only the active, enabled step may edit the draft; internal operations write it directly.
        bool CanEditStep(int index) => !refreshingMetadata &&
            (wizard == null || (!wizard.IsBusy && !wizard.IsCompleted && LogicalStepIndex == index));

        void RefreshCurrentStep()
        {
            if (LogicalStepIndex < 0) return;
            var properties = new[]
            {
                new[] { nameof(SelectedTileType) },
                new[] { nameof(SelectedSource), nameof(Aggregate), nameof(SingleValue), nameof(GroupBy), nameof(ChartGroup),
                    nameof(Value2Field), nameof(WeightField), nameof(HtmlSource), nameof(ColumnSpan), nameof(RowSpan) },
                new[] { nameof(SelectedScope), nameof(DashboardTab), nameof(SelectedGroup), nameof(Group), nameof(NavigationTarget) },
                new[] { nameof(Title) }
            };
            refreshingMetadata = true;
            try
            {
                foreach (var property in properties[LogicalStepIndex]) OnPropertyChanged(property);
                if (LogicalStepIndex == 1)
                    foreach (var field in ValueFields) field.RefreshSelection();
            }
            finally { refreshingMetadata = false; }
        }

        void RefreshCommands()
        {
            EditFiltersCommand?.RaiseCanExecuteChanged(); AddGroupCommand?.RaiseCanExecuteChanged();
            SuggestHeadingCommand?.RaiseCanExecuteChanged(); DeleteCommand?.RaiseCanExecuteChanged();
        }

        void NotifyEdit(int step, string property)
        {
            OnPropertyChanged(property);
            Edited?.Invoke(step + (wizard != null && HasParameters && step >= 2 ? 1 : 0));
        }

        void RefreshPresentation()
        {
            foreach (var property in new[] { nameof(ShowSize), nameof(ShowHtml), nameof(ShowData), nameof(ShowAggregate),
                nameof(ShowMultipleValues), nameof(ShowSingleValue), nameof(ShowGrouping), nameof(ShowChartGroup), nameof(ShowValue2), nameof(ShowWeight),
                nameof(ValueFieldLabel), nameof(SelectionSummary) }) OnPropertyChanged(property);
            RefreshCommands();
        }

        void RefreshSources(bool preserveSelection = false)
        {
            var selected = SelectedSource;
            var available = allSources.Where(source => !ShowData || TileTableCapabilities.GetVisualizations(source.Type).Contains(Visualization)).ToList();
            if (selected != null && (preserveSelection || selected.Type == null) && !available.Contains(selected)) available.Add(selected);
            if (selected != null && !available.Contains(selected)) SetSource(null);
            sources = available;
            refreshingMetadata = true;
            try { OnPropertyChanged(nameof(Sources)); }
            finally { refreshingMetadata = false; }
            OnPropertyChanged(nameof(SelectedSource));
        }

        void RefreshFields(bool prune)
        {
            fields = getFields(SelectedSource);
            var groups = fields.Where(f => f.CanGroup).Select(f => f.Name).ToArray();
            var numbers = fields.Where(f => f.CanAggregate).Select(f => f.Name).ToArray();
            var values = Visualization == TileVisualization.RecordText ? groups : numbers;
            if (prune && ShowData)
            {
                draft.ValueColumns = (draft.ValueColumns ?? new List<string>()).Where(values.Contains).ToList();
                if (!groups.Contains(draft.GroupBy)) draft.GroupBy = null;
                if (!groups.Contains(draft.ChartGroup)) draft.ChartGroup = null;
                if (!numbers.Contains(draft.Value2Field)) draft.Value2Field = null;
                if (!numbers.Contains(draft.WeightField)) draft.WeightField = null;
            }
            // Include stored names even if metadata has changed. They are rejected explicitly
            // at validation, rather than removed as a side effect of opening the editor.
            numericFields = new[] { string.Empty }.Concat(numbers).Concat(new[] { SingleValue, draft.Value2Field, draft.WeightField })
                .Where(v => v != null).Distinct().ToArray();
            groupFields = new[] { string.Empty }.Concat(groups).Concat(new[] { draft.GroupBy, draft.ChartGroup })
                .Where(v => v != null).Distinct().ToArray();
            var selected = draft.ValueColumns ?? new List<string>();
            valueFields = new ObservableCollection<TileValueFieldViewModel>(selected.Concat(values).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct()
                .Select(name => new TileValueFieldViewModel(name, values.Contains(name),
                    field => draft.ValueColumns?.Contains(field) == true, UpdateValueSelection)));
            aggregates = TileTableCapabilities.GetAggregates(SelectedSource?.Type);
            if (prune && !aggregates.Contains(draft.Aggregate)) draft.Aggregate = TileAggregate.Count;
            if (prune) NormalizeDraft();
            refreshingMetadata = true;
            try
            {
                foreach (var name in new[] { nameof(NumericFields), nameof(GroupFields), nameof(ValueFields), nameof(Aggregates), nameof(AggregateOptions) }) OnPropertyChanged(name);
            }
            finally { refreshingMetadata = false; }
            foreach (var name in new[] { nameof(Aggregate), nameof(SingleValue), nameof(GroupBy), nameof(ChartGroup),
                nameof(Value2Field), nameof(WeightField) }) OnPropertyChanged(name);
        }

        void UpdateValueSelection(string name, bool selected)
        {
            if (!CanEditStep(1)) return;
            if (draft.ValueColumns == null) draft.ValueColumns = new List<string>();
            if (selected) { if (!draft.ValueColumns.Contains(name)) draft.ValueColumns.Add(name); }
            else draft.ValueColumns.Remove(name);
            NotifyEdit(1, nameof(SingleValue));
        }

        internal string ValidationError()
        {
            try { ValidateDraft(wizard == null || wizard.CurrentStep is TileParametersStepViewModel || wizard.CurrentStep is TileReviewStepViewModel); return null; }
            catch (ArgumentException ex) { return ex.Message; }
        }

        internal TileDefinition PrepareForSave()
        {
            NormalizeDraft();
            ValidateDraft();
            return draft;
        }

        internal async Task LoadProfilesAsync(CancellationToken cancellationToken)
        {
            Profiles = await TileDashboardAccess.LoadProfilesAsync(api, cancellationToken);
            OnPropertyChanged(nameof(Profiles));
        }

        void NormalizeDraft()
        {
            draft.Title = string.IsNullOrWhiteSpace(draft.Title) ? null : draft.Title.Trim();
            draft.DashboardTab = string.IsNullOrWhiteSpace(draft.DashboardTab) ? null : draft.DashboardTab.Trim();
            draft.HtmlSource = draft.HtmlSource?.Trim();
            if (!ShowChartGroup) draft.ChartGroup = null;
            if (!ShowSize && !Visualization.SupportsMultipleValues())
                draft.ValueColumns = (draft.ValueColumns ?? new List<string>()).Take(1).ToList();
            if (!ShowSize && Visualization == TileVisualization.RecordText)
            {
                draft.Aggregate = TileAggregate.Count;
                draft.GroupBy = draft.ChartGroup = draft.Value2Field = draft.WeightField = null;
            }
        }

        void ValidateDraft(bool validateParameters = true)
        {
            if (api != null && !Scopes.Contains(Scope))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("NoRights"));
            if (api != null && IsProfile && !Profiles.Any(item => item.KeyStr == Profile))
                throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileSelectAProfile"));
            if (SelectedTileType == null) throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileSelectATileType"));
            if (ShowData && SelectedSource?.Type == null) throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileSelectAnAvailableDataSource"));
            var parameterError = validateParameters ? Parameters.FirstOrDefault(parameter => !string.IsNullOrEmpty(parameter.Error))?.Error : null;
            if (parameterError != null) throw new ArgumentException(parameterError);
            if (parameterTile != null) draft.TileTypeParameters = parameterTile.Serialize();
            draft.Validate();
            TileTableCapabilities.Validate(SelectedSource?.Type, draft);
            if (ShowSize) TileMetadataService.ResolveStandaloneTileType(draft.TileTypeId);
            if (ShowData)
            {
                var valueNames = Visualization == TileVisualization.RecordText
                    ? fields.Where(f => f.CanGroup).Select(f => f.Name) : fields.Where(f => f.CanAggregate).Select(f => f.Name);
                if ((draft.ValueColumns ?? new List<string>()).Any(value => !string.IsNullOrWhiteSpace(value) && !valueNames.Contains(value)))
                    throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileASelectedValueFieldIsUnavailableUpdateThe"));
                var groupNames = fields.Where(f => f.CanGroup).Select(f => f.Name);
                if (new[] { ShowGrouping ? draft.GroupBy : null, ShowChartGroup ? draft.ChartGroup : null }
                    .Any(value => !string.IsNullOrWhiteSpace(value) && !groupNames.Contains(value)))
                    throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileASelectedGroupingFieldIsUnavailable"));
                var numberNames = fields.Where(f => f.CanAggregate).Select(f => f.Name);
                if (new[] { draft.Value2Field, draft.WeightField }.Any(value => !string.IsNullOrWhiteSpace(value) && !numberNames.Contains(value)))
                    throw new ArgumentException(Uniconta.ClientTools.Localization.lookup("TileASelectedNumericFieldIsUnavailable"));
            }
        }

        internal async Task<bool> SaveAsync()
        {
            var result = await service.SaveAsync(this);
            if (result.Item1 != ErrorCodes.Succes) { UtilDisplay.ShowErrorCode(result.Item1); return false; }
            return true;
        }

        async Task DeleteAsync()
        {
            await wizard.RunOperationAsync(async token =>
            {
                var error = await service.DeleteAsync(original);
                if (error != ErrorCodes.Succes) { UtilDisplay.ShowErrorCode(error); return; }
                IsDeleted = true;
            });
            if (IsDeleted) wizard.CloseCommand.Execute(null);
        }

        void EditFilters()
        {
            var dialog = new CWServerFilter(api, SelectedSource.Type, GetSavedFilters(draft.Filter), null);
            dialog.ShowDialog();
            if (dialog.DialogResult != true) return;
            var stream = StreamingManagerReuse.Create();
            stream.ToStream(dialog._helper.FilterProperties);
            draft.Filter = Convert.ToBase64String(stream.ToByteArrayAndExit(0));
            NotifyEdit(1, nameof(FilterSummary));
        }

        static Filter[] GetSavedFilters(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            try
            {
                var stream = StreamingManagerReuse.Create(Convert.FromBase64String(value));
                return ((FilterProperties[])stream.ToArray(typeof(FilterProperties)))
                    .Select(p => new Filter { name = p.PropertyName, value = p.UserInput, parameterType = p.ParameterType }).ToArray();
            }
            catch
            {
                return Regex.Split(value, ";;;").Where(f => f.IndexOf(':') >= 0).Select(f =>
                {
                    var index = f.IndexOf(':');
                    return new Filter { name = f.Substring(0, index).Trim(), value = f.Substring(index + 1).Trim() };
                }).ToArray();
            }
        }

        string BuildHeadingConfiguration()
        {
            if (ShowSize) return JsonConvert.SerializeObject(new { TileType = SelectedTileType.Caption, Parameters = HasParameters ? string.Join(", ", Parameters.Select(p => p.Caption + ": " + p.Value)) : null, Columns = ColumnSpan, Rows = RowSpan, Scope = Scope.ToString(), Profile = IsProfile ? Profile : null, Group });
            return JsonConvert.SerializeObject(new
            {
                DataSource = ShowHtml ? null : SelectedSource?.Caption,
                Visualization = Visualization.ToString(), Aggregate = ShowAggregate ? Aggregate.ToString() : null,
                ValueFields = ShowHtml ? null : string.Join(", ", ShowMultipleValues
                    ? (IEnumerable<string>)(draft.ValueColumns ?? new List<string>()) : new[] { SingleValue }),
                GroupByField = ShowGrouping ? GroupBy : null, SeriesByField = ShowChartGroup ? ChartGroup : null,
                Value2Field = ShowValue2 ? Value2Field : null, WeightField = ShowWeight ? WeightField : null,
                Filters = string.Join("; ", (GetSavedFilters(draft.Filter) ?? new Filter[0]).Select(f => f.name + ": " + f.value)),
                WidgetUrl = ShowHtml ? HtmlSource : null, Scope = Scope.ToString(), Profile = IsProfile ? Profile : null, Group, DashboardTab,
                GoTo = ShowData ? NavigationTarget : null
            });
        }

        string ReviewFieldValue(string name, object value)
        {
            switch (name)
            {
                case "Visualization": return Visualization.DisplayName();
                case "Aggregate": return Aggregate.DisplayName();
                case "Scope": return Scope.DisplayName();
                default: return value?.ToString();
            }
        }

        static string ReviewFieldCaption(string name)
        {
            switch (name)
            {
                case "TileType": return Uniconta.ClientTools.Localization.lookup("TileType");
                case "Columns": return Uniconta.ClientTools.Localization.lookup("Columns");
                case "Rows": return Uniconta.ClientTools.Localization.lookup("Rows");
                case "Scope": return Uniconta.ClientTools.Localization.lookup("TileScope");
                case "Profile": return Uniconta.ClientTools.Localization.lookup("Profile");
                case "Visualization": return Uniconta.ClientTools.Localization.lookup("TileVisualization");
                case "Aggregate": return Uniconta.ClientTools.Localization.lookup("TileAggregate");
                case "DataSource": return Uniconta.ClientTools.Localization.lookup("TileDataSource");
                case "ValueFields": return Uniconta.ClientTools.Localization.lookup("TileValueColumns");
                case "GroupByField": return Uniconta.ClientTools.Localization.lookup("MenuColumnGroup");
                case "SeriesByField": return Uniconta.ClientTools.Localization.lookup("TileChartGroup");
                case "Value2Field": return Uniconta.ClientTools.Localization.lookup("TileSecondValue");
                case "WeightField": return Uniconta.ClientTools.Localization.lookup("TileBubbleWeight");
                case "Filters": return Uniconta.ClientTools.Localization.lookup("Filter");
                case "WidgetUrl": return Uniconta.ClientTools.Localization.lookup("TileWidgetURL");
                case "Group": return Uniconta.ClientTools.Localization.lookup("Group");
                case "DashboardTab": return Uniconta.ClientTools.Localization.lookup("Dashboard");
                case "GoTo": return Uniconta.ClientTools.Localization.lookup("TileGoTo");
                default: return name;
            }
        }

        internal async Task PrepareReviewAsync(CancellationToken token)
        {
            var configuration = BuildHeadingConfiguration();
            Summary = Uniconta.ClientTools.Localization.lookup("TileType") + ": " + SelectedTileType?.Caption + Environment.NewLine + string.Join(Environment.NewLine,
                JsonConvert.DeserializeObject<Dictionary<string, object>>(configuration)
                    .Where(p => p.Value != null && !string.IsNullOrWhiteSpace(p.Value.ToString()))
                    .Select(p => ReviewFieldCaption(p.Key) + ": " + ReviewFieldValue(p.Key, p.Value)));
            // An intentionally blank title on a saved tile stays blank when reopened.
            if (!IsExisting && string.IsNullOrWhiteSpace(Title) && lastSuggestedConfiguration != configuration)
            {
                // A cancelled automatic suggestion can be bypassed on the next Continue.
                // The explicit suggestion command remains available on the review step.
                lastSuggestedConfiguration = configuration;
                await SuggestHeadingAsync(token);
            }
        }

        async Task SuggestHeadingAsync(CancellationToken token)
        {
            try
            {
                var ai = GPTServiceFactory.Instance.GetActiveService(api);
                if (ai == null) { HeadingStatus = Uniconta.ClientTools.Localization.lookup("TileEnterATitleOrLeaveItBlank"); return; }
                HeadingStatus = Uniconta.ClientTools.Localization.lookup("TileSuggestingATitle");
                var configuration = BuildHeadingConfiguration();
                var originalHeading = Title;
                var context = PromptProvider.From(() => CWTileDetails.TileHeadingPrompt).Replace("TileConfiguration", configuration)
                    .EnrichContext(new GPTConversationContext());
                var result = await ai.GetResponse<TileHeadingResult>(context, token);
                token.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(result?.Heading) && Title == originalHeading && configuration == BuildHeadingConfiguration())
                {
                    SetTitle(result.Heading.Trim());
                    lastSuggestedConfiguration = configuration;
                }
                HeadingStatus = Uniconta.ClientTools.Localization.lookup("TileYouCanEditTheTitleOrLeaveIt");
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { HeadingStatus = Uniconta.ClientTools.Localization.lookup("TileTitleSuggestionCancelled"); throw; }
            catch (Exception ex)
            {
                Uniconta.ClientTools.InfoLog.Log.Warning("Tile heading suggestion unavailable: " + ex.Message);
                HeadingStatus = Uniconta.ClientTools.Localization.lookup("TileTitleSuggestionUnavailableEnterATitleOrLeave");
            }
        }
    }

    public sealed class TileChoice<T>
    {
        public TileChoice(T value, string caption) { Value = value; Caption = caption; }
        public T Value { get; }
        public string Caption { get; }
    }

    public sealed class TileValueFieldViewModel : NotifyPropertyChangedViewModel
    {
        readonly Func<string, bool> isSelected;
        readonly Action<string, bool> changed;
        internal TileValueFieldViewModel(string name, bool available, Func<string, bool> isSelected, Action<string, bool> changed)
        { Name = name; Caption = available ? name : name + " " + Uniconta.ClientTools.Localization.lookup("TileUnavailable"); this.isSelected = isSelected; this.changed = changed; }
        public string Name { get; }
        public string Caption { get; }
        public bool IsSelected
        {
            get => isSelected(Name);
            set { if (IsSelected != value) { changed(Name, value); RefreshSelection(); } }
        }
        internal void RefreshSelection() => OnPropertyChanged(nameof(IsSelected));
    }
}
