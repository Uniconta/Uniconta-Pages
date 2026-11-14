using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.AI;
using Uniconta.ClientTools.AI.Prompts;
using Uniconta.ClientTools.AI.Services.Context;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Controls.AI.Assistant.Capabilities;
using Uniconta.ClientTools.Controls.AI.Assistant.DataProviders;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.Common.User;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Pages.Maintenance;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class ConfigureFunctionalityPage : FormBasePage
    {
        Company editrow;

        public ConfigureFunctionalityViewModel Functionality { get; private set; }

        public ConfigureFunctionalityPage(BaseAPI API)
            : this(API.CompanyEntity)
        {
        }
        public ConfigureFunctionalityPage(UnicontaBaseEntity sourceData)
            : base(sourceData, true)
        {
            Functionality = new ConfigureFunctionalityViewModel(editrow, this, AppEnums.DocumentScanner.Values, DocumentScannerChanged,
                !editrow.FullPackage && BasePage.GetSystemDefaultDate() >= new DateTime(2022, 8, 15));
            InitializeComponent();
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            frmRibbon.OnCustomEditorValueChanged += frmRibbon_OnCustomEditorValueChanged;

            ZoomEnabler.EnableZoom(LayoutRoot);

            if (api.session.User._Role < (byte)Uniconta.Common.User.UserRoles.Accountant)
                Loaded += ConfigureFunctionalityPage_Loaded;
        }

        private void frmRibbon_OnCustomEditorValueChanged(string action, string value)
        {
            if (action == "SearchFeatures")
                Functionality.SearchText = value;
        }

        private void ConfigureFunctionalityPage_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("DisclaimerInConfiguration"), Uniconta.ClientTools.Localization.lookup("Information"));
            }));
        }

        private void DocumentScannerChanged(int selectedIndex)
        {
            var selected = (PayableDocumentScanners)selectedIndex;
            if (selected == editrow.DocumentScanner)
                return;

            editrow.DocumentScanner = selected;
            if (selected == 0)
                return;

            var result = UnicontaMessageBox.Show(CompanyClientText.AutoSaveCreditors + "?", Uniconta.ClientTools.Localization.lookup("DocumentScannersPay"), MessageBoxButton.YesNo);
            editrow._PaperFlowSaveCreditors = result == MessageBoxResult.Yes;
        }

        private async void frmRibbon_OnItemClicked(string ActionType)
        {
            if (ActionType == "Save")
            {
                editrow._LogTableChanges = editrow.TraceFieldChanges;
                await saveForm();
                var mainPage = App.MainPageObj;
                mainPage.dockCtrl.CloseAllDocuments(false);
                mainPage.setLeftmenu();
            }
            else if (ActionType == "ExpandAll")
                Functionality.SetExpanded(true);
            else if (ActionType == "CollapseAll")
                Functionality.SetExpanded(false);
            else
                frmRibbon_BaseActions(ActionType);
        }

        public override Type TableType { get { return typeof(Company); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (Company)value; } }
        public override void OnClosePage(object[] refreshParams) { }
        public override string NameOfControl { get { return TabControls.ConfigureFunctionalityPage; } }
        public override bool BeforeSetUserField(ref CorasauLayoutGroup parentGroup) { return false; }

        #region AI
        public override List<IAIAssistantCapability> GetAIAssistantCapabilities(bool hasFileAttached)
        {
            var capabilities = base.GetAIAssistantCapabilities(hasFileAttached);

            capabilities.Add(new SetFunctionalityFeatureAICapability(this, () => saveForm()));

            return capabilities;
        }

        public override IEnumerable<IAIAssistantDataProvider> GetAIAssistantDataProviders()
        {
            var providers = base.GetAIAssistantDataProviders().ToList();

            providers.Add(new ConfigureFunctionalityDataProvider(this));

            return providers;
        }
        #endregion
    }


    public sealed class ConfigureFunctionalityViewModel : NotifyPropertyChangedBase
    {
        string searchText;
        public string SearchText
        {
            get { return searchText; }
            set
            {
                if (searchText == value) return;
                searchText = value;
                OnPropertyChanged(nameof(SearchText));
                foreach (var module in Modules)
                    module.ApplySearch((value ?? string.Empty).Trim());
            }
        }

        public FunctionalityModuleViewModel Basics { get; private set; }
        public FunctionalityModuleViewModel Logistics { get; private set; }
        public FunctionalityModuleViewModel Sales { get; private set; }
        //public FunctionalityModuleViewModel Purchase { get; private set; }
        public FunctionalityModuleViewModel Project { get; private set; }

        public FunctionalityModuleViewModel CRM { get; private set; }
        public FunctionalityModuleViewModel Production { get; private set; }
        public FunctionalityModuleViewModel FixedAssets { get; private set; }

        //public FunctionalityModuleViewModel ProgramModules { get; private set; }

        public FunctionalityModuleViewModel Addins { get; private set; }
        public FunctionalityModuleViewModel AIAgents { get; private set; }

        public IEnumerable<FunctionalityModuleViewModel> Modules
        {
            get { return new[] { Basics, Logistics, Sales, /*Purchase,*/ CRM, Production, FixedAssets, Project, Addins, AIAgents }; }
        }

        public ConfigureFunctionalityViewModel(Company company, object page, IEnumerable documentScannerValues, Action<int> documentScannerChanged, bool showOnlyPosting)
        {
            var basicsModules = Module(company, "Basic",
                F("Ledger", "LedgerFeature"),
                F("Debtor", "Debtor"),
                F("Creditor", "Creditor"),
                F("Inventory", "Inventory"));

            var basicsFeatures = Module(company, "Basic",
                F("Contacts", "Contacts"),
                F("BankStatement", "BankReconciliation"),
                F("PhycicalVoucher", "PhysicalVoucher"),
                F("CollectionLetter", "InterestAndCollection"),
                F("CreditorBankApprovement", "CreditorBankApprovement"),
                F("OnlyPosting", "OnlyPosting"),
                F("Attachments", "Attachments"),
                F("Profiles", "Profiles"),
                F("BusinessRules", "BusinessRule"));

            Basics = new FunctionalityModuleViewModel("Basic",
                new[]
                {
                    new FunctionalityFeatureGroupViewModel(
                        "Modules",
                        basicsModules.FeatureGroups.SelectMany(group => group.Features)),
                    new FunctionalityFeatureGroupViewModel(
                        "Features",
                        basicsFeatures.FeatureGroups.SelectMany(group => group.Features))
                });

            Basics.DescriptionKey = "Ledger_Description";

            Logistics = Module(company, "LogisticModule",
                            F("Storage", "Storage"),
                            F("StorageOnAll", "Allitems"),
                            F("InvPrice", "CustomerPriceLists"),
                            F("CreditorPrice", "VendorPriceLists"),
                            F("InvClientName", "ClientItemNumber"),
                            F("Warehouse", "Warehouse"),
                            F("Location", "Location"),
                            F("InvBOM", "BOM"),
                            F("InvBOMVariant", "InvBOMVariant"),
                            F("ItemVariants", "ItemVariants"),
                            F("UnitConversion", "UnitConversion"),
                            F("InvDuty", "Duty"),
                            F("SerialBatchNumbers", "SerialBatchNumbers"),
                            F("PurchaseAccounts", "PurchaseAccounts"),
                            F("ProductCatalog", "ProdCatalog"));
            //Sales = Module(company, "Orders", F("Order", "SalesOrders"), F("DeliveryAddress", "DeliveryAddr"),
            //    F("Shipments", "Shipments"), F("Packnote", "SalesDeliveryNotes"), F("DebtorBudget", "DebtorBudget"),
            //    F("InvPackaging", "ProducerResponsibility"));
            //Purchase = Module(company, "Purchase", F("Purchase", "PurchaseOrders"), F("PurchaseCharges", "PurchaseCharges"),
            //    F("CreditorPacknote", "SupplierDeliveryNotes"));

            var sales = Module(company, "Orders",
                        F("Order", "SalesOrders"),
                        F("DeliveryAddress", "DeliveryAddr"),
                        F("Shipments", "Shipments"),
                        F("Packnote", "SalesDeliveryNotes"),
                        F("DebtorBudget", "DebtorBudget"),
                        F("InvPackaging", "ProducerResponsibility"));

            var purchase = Module(company, "Purchase",
                           F("Purchase", "PurchaseOrders"),
                           F("PurchaseCharges", "PurchaseCharges"),
                           F("CreditorPacknote", "SupplierDeliveryNotes"));

        Sales = new FunctionalityModuleViewModel("SalesAndPurchase",
            new[]
            {
                new FunctionalityFeatureGroupViewModel("Orders",sales.FeatureGroups.SelectMany(group => group.Features)),
                new FunctionalityFeatureGroupViewModel("Purchase", purchase.FeatureGroups.SelectMany(group => group.Features))
            });


            Project = Module(company, "Project", F("Project", "ProjectManagement"), F("ProjectMaterial", "Materials"),
                                                 F("TimeManagement", "TimeManagement"), F("ProjectTask", "Tasks"), F("ProjectBudget", "ProjectBudget"),
                                                 F("Payroll", "Payroll"), F("MyProjects", "MyProjects"), F("ProjectPlanning", "ProjectBudgetSchedule"));
            CRM = Module(company, "CRM",
                F("CRM", "CRM")
                
                );

            Production = Module(company, "Production", F("Production", "Production") );
            FixedAssets = Module(company, "Asset", F("FixedAsset", "Asset"));
            Addins = new FunctionalityModuleViewModel("Addins",
                new[]
                {
                    new FunctionalityFeatureGroupViewModel("Autobank & Scan",
                        new[]
                        {
                            new FunctionalityFeatureViewModel(company, "_DirectDebit", "AutoBanking"),
                            new FunctionalityFeatureViewModel("DocumentScannersPay", documentScannerValues,
                                (int)company.DocumentScanner, documentScannerChanged)
                        }),
                    new FunctionalityFeatureGroupViewModel("Features",
                        new[]
                        {
                            new FunctionalityFeatureViewModel(company, "IntegrationPack", "IntegrationPack"),
                            new FunctionalityFeatureViewModel(company, "TraceFieldChanges", "FieldChangeLog"),
                            new FunctionalityFeatureViewModel(company, "DATEV", "DATEV")
                        })
                });

            AIAgents = CreateAIAgents(company);

            var logisticsFeatures = Logistics.FeatureGroups.SelectMany(group => group.Features).ToList();

            var storage = logisticsFeatures.First(x => x.PropertyName == "Storage");

            var storageOnAll = logisticsFeatures.First(x => x.PropertyName == "StorageOnAll");

            storageOnAll.CanEdit = storage.IsEnabled;
            storage.ValueChanged += value => storageOnAll.CanEdit = value;

            Basics.FeatureGroups.SelectMany(group => group.Features)
                                .First(x => x.PropertyName == "OnlyPosting")
                                .Visibility = showOnlyPosting ? Visibility.Visible : Visibility.Collapsed;
        }

        static FunctionalityModuleViewModel Module(object entity, string key, params FeatureDefinition[] definitions) 
            => new FunctionalityModuleViewModel(key, definitions.Select(x => new FunctionalityFeatureViewModel(entity, x.Property, x.Key)));

        public static FunctionalityModuleViewModel CreateAIAgents(object page)
        {
            return Module(page, "AIAgents",
                             F("AIAssistant",                "AIAssistant"),
                             F("AccountMatch",               "AccountMatch"),
                             F("TransactionOriginAnalysis",  "TransactionOriginAnalysis"),
                             F("LedgerTrendAnomalyAnalyze",  "LedgerTrendAnomalyAnalyze"),
                             F("DetermineCostAccount",       "DetermineCostAccount"));
        }

        public void SetExpanded(bool isExpanded)
        {
            foreach (var module in Modules)
            {
                module.SetExpanded(isExpanded);
            }
        }

        static FeatureDefinition F(string property, string key) { return new FeatureDefinition(property, key); }

        struct FeatureDefinition
        {
            public readonly string Property;
            public readonly string Key;
            public FeatureDefinition(string property, string key) { Property = property; Key = key; }
        }
    }

    public sealed class FunctionalityModuleViewModel : NotifyPropertyChangedBase
    {
        string searchText = string.Empty;

        public string TitleKey { get; private set; }
        private string DefaultDescription => TitleKey + "_Description";
        private string _descriptionKey;
        public string DescriptionKey { get => _descriptionKey ?? DefaultDescription; set => _descriptionKey = value; }

        public string Title
        {
            get { return Uniconta.ClientTools.Localization.lookup(TitleKey); }
        }

        public string Description
        {
            get { return Uniconta.ClientTools.Localization.lookup(DescriptionKey); }
        }

        public ObservableCollection<FunctionalityFeatureGroupViewModel> FeatureGroups
        {
            get;
            private set;
        }

        public Visibility Visibility
        {
            get
            {
                return searchText.Length == 0 ||
                       FeatureGroups.Any(x => x.Visibility == Visibility.Visible)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        public FunctionalityModuleViewModel(string titleKey, IEnumerable<FunctionalityFeatureViewModel> features) : this(titleKey,
                new[]
                {
                    new FunctionalityFeatureGroupViewModel("Features", features)
                })
        {
        }

        public FunctionalityModuleViewModel(
            string titleKey,
            IEnumerable<FunctionalityFeatureGroupViewModel> groups)
        {
            TitleKey = titleKey;
            FeatureGroups =
                new ObservableCollection<FunctionalityFeatureGroupViewModel>(groups);
        }

        internal void ApplySearch(string text)
        {
            text = text ?? string.Empty;

            if (searchText == text)
                return;

            searchText = text;

            foreach (var group in FeatureGroups)
                group.ApplySearch(text);

            OnPropertyChanged(nameof(Visibility));
        }

        public void SetExpanded(bool isExpanded)
        {
            foreach (var group in FeatureGroups)
                group.SetExpanded(isExpanded);
        }
    }

    public sealed class FunctionalityFeatureGroupViewModel : NotifyPropertyChangedBase
    {
        bool isExpanded;
        string searchText = string.Empty;

        public string TitleKey { get; private set; }

        public string Title
        {
            get { return Uniconta.ClientTools.Localization.lookup(TitleKey); }
        }

        public ObservableCollection<FunctionalityFeatureViewModel> Features { get; private set; }

        public IEnumerable<FunctionalityFeatureViewModel> VisibleFeatures => Features.Where(x => x.Visibility == Visibility.Visible && MatchesSearch(x.Title, x.Description));

        public Visibility Visibility
        {
            get
            {
                return searchText.Length == 0 || VisibleFeatures.Any()
                    ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public bool IsExpanded
        {
            get { return isExpanded || searchText.Length != 0; }
            set { Set(ref isExpanded, value, nameof(IsExpanded)); }
        }

        public FunctionalityFeatureGroupViewModel(
            string titleKey,
            IEnumerable<FunctionalityFeatureViewModel> features)
        {
            TitleKey = titleKey;

            Features = new ObservableCollection<FunctionalityFeatureViewModel>(
                features ?? Enumerable.Empty<FunctionalityFeatureViewModel>());
        }

        bool MatchesSearch(string title, string description)
        {
            return searchText.Length == 0 ||
                (title ?? string.Empty).IndexOf(
                    searchText,
                    StringComparison.CurrentCultureIgnoreCase) >= 0 ||
                (description ?? string.Empty).IndexOf(
                    searchText,
                    StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        internal void ApplySearch(string text)
        {
            text = text ?? string.Empty;

            if (searchText == text)
                return;

            searchText = text;

            if (searchText.Length != 0)
            {
                foreach (var feature in VisibleFeatures)
                    feature.IsDescriptionExpanded = true;
            }

            OnPropertyChanged(nameof(VisibleFeatures));
            OnPropertyChanged(nameof(Visibility));
            OnPropertyChanged(nameof(IsExpanded));
        }

        public void SetExpanded(bool expanded)
        {
            IsExpanded = expanded;

            foreach (var feature in Features)
                feature.IsDescriptionExpanded = expanded;
        }
    }

    public sealed class FunctionalityFeatureViewModel : NotifyPropertyChangedBase
    {
        readonly object entity;
        readonly PropertyInfo property;
        readonly Action<int> selectionChanged;
        int selectedIndex;
        bool canEdit = true;
        bool isDescriptionExpanded;
        Visibility visibility = Visibility.Visible;

        public event Action<bool> ValueChanged;
        public string PropertyName { get; private set; }
        public string TitleKey { get; private set; }
        public string DescriptionKey { get { return TitleKey + "_FeatureDescription"; } }
        public string Title { get { return Uniconta.ClientTools.Localization.lookup(TitleKey); } }
        public string Description { get { return Uniconta.ClientTools.Localization.lookup(DescriptionKey); } }
        public bool IsComboBox { get { return Items != null; } }
        public IEnumerable Items { get; private set; }
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                if (!IsComboBox || selectedIndex == value) return;
                selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
                selectionChanged(value);
            }
        }

        internal object Entity { get { return entity; } }
        internal PropertyInfo Property { get { return property; } }

        public bool IsEnabled
        {
            get { return property != null && Convert.ToBoolean(property.GetValue(entity, null)); }
            set
            {
                if (property == null || IsEnabled == value) return;
                property.SetValue(entity, property.PropertyType == typeof(bool?) ? (bool?)value : value, null);
                OnPropertyChanged(nameof(IsEnabled));
                var handler = ValueChanged;
                if (handler != null) handler(value);
            }
        }
        public bool CanEdit { get { return canEdit; } set { Set(ref canEdit, value, nameof(CanEdit)); } }
        public bool IsDescriptionExpanded { get { return isDescriptionExpanded; } set { Set(ref isDescriptionExpanded, value, nameof(IsDescriptionExpanded)); } }
        public Visibility Visibility { get { return visibility; } set { Set(ref visibility, value, nameof(Visibility)); } }

        public FunctionalityFeatureViewModel(object entity, string propertyName, string titleKey)
        {
            this.entity = entity;
            PropertyName = propertyName;
            TitleKey = titleKey;
            property = entity.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null || !property.CanRead || !property.CanWrite || (property.PropertyType != typeof(bool) && property.PropertyType != typeof(bool?)))
                throw new ArgumentException("Feature property must be a writable Boolean: " + propertyName, nameof(propertyName));
        }

        public FunctionalityFeatureViewModel(string titleKey, IEnumerable items, int selectedIndex, Action<int> selectionChanged)
        {
            TitleKey = titleKey;
            Items = items ?? throw new ArgumentNullException(nameof(items));
            this.selectedIndex = selectedIndex;
            this.selectionChanged = selectionChanged ?? throw new ArgumentNullException(nameof(selectionChanged));
        }
    }

    public abstract class NotifyPropertyChangedBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void Set<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }
        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    /// <summary>
    /// Exposes the functionality represented by the reusable module view models to the assistant.
    /// </summary>
    internal static class ConfigureFunctionalityAIScraper
    {
        internal sealed class Feature
        {
            public string ModuleKey { get; set; }
            public string Module { get; set; }
            public string Key { get; set; }
            public string Caption { get; set; }
            public string DescriptionKey { get; set; }
            public string Description { get; set; }
            public bool Enabled { get; set; }

            internal FunctionalityFeatureViewModel ViewModel { get; set; }
        }

        public static IReadOnlyList<Feature> GetFeatures(ConfigureFunctionalityPage page)
        {
            if (page == null || page.Functionality == null)
                return new Feature[0];

            return page.Functionality.Modules
                .SelectMany(module => module.FeatureGroups
                    .SelectMany(group => group.Features)
                    .Where(feature => !feature.IsComboBox)
                    .Select(feature => new Feature
                    {
                        ModuleKey = module.TitleKey,
                        Module = Uniconta.ClientTools.Localization.lookup(module.TitleKey),
                        Key = feature.TitleKey,
                        Caption = Uniconta.ClientTools.Localization.lookup(feature.TitleKey),
                        DescriptionKey = feature.DescriptionKey,
                        Description = Uniconta.ClientTools.Localization.lookup(feature.DescriptionKey),
                        Enabled = feature.IsEnabled,
                        ViewModel = feature
                    }))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .ToArray();
        }

        public static void SetEnabled(Feature feature, bool enabled)
        {
            if (feature == null)
                throw new ArgumentNullException(nameof(feature));
            if (feature.ViewModel == null)
                throw new InvalidOperationException("The feature is not writable.");

            feature.ViewModel.IsEnabled = enabled;
            feature.Enabled = enabled;
        }

    }

    public sealed class ConfigureFunctionalityDataRequest : AIDataRequestBase
    {
    }
    public sealed class ConfigureFunctionalityDataProvider : IAIAssistantDataProvider
    {
        readonly ConfigureFunctionalityPage page;

        public ConfigureFunctionalityDataProvider(ConfigureFunctionalityPage page)
        {
            this.page = page ?? throw new ArgumentNullException(nameof(page));
        }


        public string Name => "CompanyFunctionality";

        public string UserFriendlyText => "Looking into company functionality";

        public string Description => "Provides all company functionality features currently shown on the Configure Functionality page, including module, feature name, description and whether the feature is enabled. " +
                                     "Use when the user asks which functionality exists, what a feature does, or which features are enabled or disabled.";

        public Type Contract => typeof(ConfigureFunctionalityDataRequest);

        public Task<AIAssistantDataProviderResult> RequestDataAsync(IGPTService gptService, IGPTConversationContext context)
        {
            var features = ConfigureFunctionalityAIScraper.GetFeatures(page)
                .Select(x => new
                {
                    Module = x.Module,
                    Key = x.Key,
                    Caption = x.Caption,
                    Description = x.Description,
                    Enabled = x.Enabled
                })
                .ToArray();

            var json = JsonConvert.SerializeObject(features, Formatting.None);

            return Task.FromResult(new AIAssistantDataProviderResult
            {
                Contract = "{}",
                Data = "Data: " + json,
                ReturnedData = json,
                UserMessage = AIAssistantDataProviderDisplayOptions.Select(this,
                    "Company functionality fetched " + features.Length + " feature(s)")
            });
        }
    }

    /// <summary>
    /// Enables or disables one of the functionality switches discovered on the Configure Functionality page.
    /// The feature is not modified until the user explicitly presses Yes.
    /// </summary>
    public sealed class SetFunctionalityFeatureAICapability : PromptCapabilityBase<SetFunctionalityFeatureAICapability.SetFeatureRequest>
    {
        readonly ConfigureFunctionalityPage page;
        readonly Func<Task> saveAsync;

        public SetFunctionalityFeatureAICapability(ConfigureFunctionalityPage page, Func<Task> saveAsync)
            : base(
                nameof(SetFunctionalityFeatureAICapability),
                "Enable or disable functionality",
                "Enables or disables one company functionality feature shown on the current Configure Functionality page, after explicit user confirmation. " +
                "Use only when the user asks to enable, disable, turn on, or turn off functionality.",
                95,
                "Enable warehouse management",
                "Disable serial numbers",
                "Turn on project budgets")
        {
            this.page = page ?? throw new ArgumentNullException(nameof(page));
            this.saveAsync = saveAsync ?? throw new ArgumentNullException(nameof(saveAsync));
        }

        protected override PromptDefinition GetPromptDefinition()
        {
            return PromptProvider.From(() => SetFunctionalityFeaturePrompt);
        }

        [PromptDefinition(
            "Capability.SetFunctionalityFeatureAICapability",
            "Extracts a requested functionality feature and desired state.")]
        public static PromptDefinition SetFunctionalityFeaturePrompt => NewPromptDefinition(
            "Determine which company functionality feature the user explicitly wants to change and whether it should be enabled or disabled. " +
            "Do not invent a feature. Return only JSON compatible with {\"feature\":\"feature name or key\",\"enabled\":true}.",
            "Extract the requested company functionality change.");

        protected override Task<AIAssistantCapabilityResult> ExecuteResponse(IAIAssistantCapabilityContext context, SetFeatureRequest response)
        {
            if (response == null || string.IsNullOrWhiteSpace(response.Feature))
                return Task.FromResult(AIAssistantCapabilityResult.Fail("A functionality feature is required."));

            var features = ConfigureFunctionalityAIScraper.GetFeatures(page);
            var feature = ResolveFeature(features, response.Feature);

            if (feature == null)
                return Task.FromResult(AIAssistantCapabilityResult.Fail(
                    "Functionality feature '" + response.Feature + "' was not found."));

            if (feature.Enabled == response.Enabled)
            {
                return Task.FromResult(AIAssistantCapabilityResult.Success(
                    feature.Caption + " is already " + (response.Enabled ? "enabled." : "disabled.")));
            }

            var action = response.Enabled ? "Enable" : "Disable";

            return Task.FromResult(new AIAssistantCapabilityResult
            {
                Succeeded = true,
                Message = action + " " + feature.Caption + "?",
                Actions = new[]
                {
                    new AIAssistantCapabilityResult.CapabilityAction
                    {
                        Caption = Uniconta.ClientTools.Localization.lookup("Yes"),
                        ExecuteAsync = () => ApplyAsync(feature.Key, response.Enabled)
                    },
                    new AIAssistantCapabilityResult.CapabilityAction
                    {
                        Caption = Uniconta.ClientTools.Localization.lookup("No"),
                        ExecuteAsync = () => Task.FromResult(
                            AIAssistantCapabilityResult.Success("No functionality was changed."))
                    }
                }
            });
        }

        async Task<AIAssistantCapabilityResult> ApplyAsync(string featureKey, bool enabled)
        {
            // Re-scrape after confirmation so the operation uses the current page state.
            var feature = ConfigureFunctionalityAIScraper.GetFeatures(page)
                .FirstOrDefault(x => string.Equals(x.Key, featureKey, StringComparison.OrdinalIgnoreCase));

            if (feature == null)
                return AIAssistantCapabilityResult.Fail("The functionality feature is no longer available.");

            if (feature.Enabled == enabled)
            {
                return AIAssistantCapabilityResult.Success(
                    feature.Caption + " is already " + (enabled ? "enabled." : "disabled."));
            }

            try
            {
                ConfigureFunctionalityAIScraper.SetEnabled(feature, enabled);

                await saveAsync();

                return AIAssistantCapabilityResult.Success(
                    feature.Caption + " has been " + (enabled ? "enabled." : "disabled."));
            }
            catch (Exception ex)
            {
                return AIAssistantCapabilityResult.Fail(
                    "Unable to change " + feature.Caption + ": " + ex.Message);
            }
        }

        static ConfigureFunctionalityAIScraper.Feature ResolveFeature(
            IReadOnlyList<ConfigureFunctionalityAIScraper.Feature> features,
            string requested)
        {
            requested = requested?.Trim();

            if (string.IsNullOrWhiteSpace(requested))
                return null;

            // Prefer canonical localization key or exact displayed caption.
            var exact = features
                .Where(x =>
                    string.Equals(x.Key, requested, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(x.Caption, requested, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (exact.Length == 1)
                return exact[0];

            // Only accept a partial match when it is unambiguous.
            var partial = features
                .Where(x =>
                    Contains(x.Caption, requested) ||
                    Contains(x.Key, requested) ||
                    Contains(requested, x.Caption))
                .ToArray();

            return partial.Length == 1 ? partial[0] : null;
        }

        static bool Contains(string value, string search)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                   !string.IsNullOrWhiteSpace(search) &&
                   value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public sealed class SetFeatureRequest
        {
            public string Feature { get; set; }
            public bool Enabled { get; set; }
        }
    }

}
