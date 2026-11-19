using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls.Wizard;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DashboardDetailsDialog : ChildWindow
    {
        readonly DashboardDetailsWizardViewModel wizard;
        public UserMenuClient SavedMenu => wizard.SavedMenu;
        public IReadOnlyList<UserMenuClient> SavedMenus => wizard.SavedMenus;

        protected override WindowStartupLocation ChildWindowStartupLocation => WindowStartupLocation.CenterOwner;

        public DashboardDetailsDialog(CrudAPI api, IReadOnlyList<UserMenuClient> dashboardItems = null)
        {
            wizard = new DashboardDetailsWizardViewModel(api, dashboardItems);
            InitializeComponent();
            DataContext = wizard;
            MaxHeight = SystemParameters.WorkArea.Height;
            MaxWidth = SystemParameters.WorkArea.Width;
            wizard.CloseRequested += WizardCloseRequested;
            Closed += (sender, args) => wizard.CloseRequested -= WizardCloseRequested;
        }

        void WizardCloseRequested(object sender, EventArgs e) => SetDialogResult(wizard.IsCompleted);

        protected override void OnClosing(CancelEventArgs e)
        {
            if (wizard.IsBusy) e.Cancel = true;
            base.OnClosing(e);
            if (wizard.IsBusy) e.Cancel = true;
        }

        void Wizard_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            ICommand command = e.Key == Key.Escape ? (ICommand)wizard.CloseCommand
                : e.Key == Key.Enter ? (wizard.IsLastStep ? (ICommand)wizard.FinishCommand : wizard.ContinueCommand) : null;
            if (command?.CanExecute(null) == true)
            {
                command.Execute(null);
                e.Handled = true;
            }
        }

    }

    public sealed class DashboardDetailsWizardViewModel : WizardViewModel
    {
        readonly CrudAPI api;
        readonly DashboardSetupStepViewModel setup;
        readonly IReadOnlyList<UserMenuClient> dashboardItems;

        public DashboardDetailsWizardViewModel(CrudAPI api, IReadOnlyList<UserMenuClient> dashboardItems = null)
            : this(api, new DashboardSetupStepViewModel(api, dashboardItems?.FirstOrDefault()), dashboardItems) { }

        DashboardDetailsWizardViewModel(CrudAPI api, DashboardSetupStepViewModel setup, IReadOnlyList<UserMenuClient> dashboardItems)
            : base(new WizardStepViewModel[] { setup, new DashboardReviewStepViewModel(setup) },
                DashboardSetupStepViewModel.GetTitle(dashboardItems?.FirstOrDefault()),
                Uniconta.ClientTools.Localization.lookup("TileDashboardSetupHelp"))
        {
            this.api = api;
            this.setup = setup;
            this.dashboardItems = dashboardItems;
        }

        public UserMenuClient SavedMenu { get; private set; }
        public IReadOnlyList<UserMenuClient> SavedMenus { get; private set; }

        protected override async Task<bool> CompleteAsync(CancellationToken cancellationToken)
        {
            var definition = new TileDefinition
            {
                IsDashboardDefinition = true,
                DashboardTab = setup.DashboardName.Trim(),
                Title = setup.DashboardName.Trim(),
                Scope = setup.Scope,
                Profile = setup.IsProfile ? setup.Profile : null
            };
            if (dashboardItems != null)
            {
                // Work on copies so cancelling or a failed save leaves the displayed dashboard intact.
                var saved = new List<UserMenuClient>();
                var inserts = new List<UserMenuClient>();
                var updates = new List<UserMenuClient>();
                var deletes = new List<UserMenuClient>();
                UserMenuClient dashboardMenu = null;
                var controller = new TileCRUDController(api);
                foreach (var original in dashboardItems)
                {
                    var tile = TileDefinition.FromUserMenu(original);
                    tile.IsLocked = controller.IsLocked(original);
                    tile.DashboardTab = definition.DashboardTab;
                    tile.Scope = definition.Scope;
                    tile.Profile = definition.Profile;
                    if (tile.IsDashboardDefinition) tile.Title = definition.Title;

                    // User and company tiles use different storage; replace records when crossing that boundary.
                    var scopeChanged = original._IsUserTile != (tile.Scope == TileScope.User);
                    var item = scopeChanged ? new UserMenuClient() : (UserMenuClient)StreamingManager.Clone(original);
                    tile.ApplyTo(item);
                    item._Uid = tile.Scope == TileScope.User ? api.session.User.Uid : 0;
                    if (scopeChanged)
                    {
                        inserts.Add(item);
                        deletes.Add(original);
                    }
                    else
                        updates.Add(item);
                    saved.Add(item);
                    if (tile.IsDashboardDefinition) dashboardMenu = item;
                }
                if (dashboardMenu == null)
                {
                    dashboardMenu = new UserMenuClient();
                    definition.ApplyTo(dashboardMenu);
                    dashboardMenu._Uid = definition.Scope == TileScope.User ? api.session.User.Uid : 0;
                    inserts.Add(dashboardMenu);
                    saved.Add(dashboardMenu);
                }
                var error = await api.MultiCrud(inserts, updates, deletes);
                if (error != ErrorCodes.Succes)
                {
                    Uniconta.ClientTools.InfoLog.Log.Error(error);
                    return false;
                }
                foreach (var item in deletes)
                    TileCRUDController.UpdateTileCache(api.CompanyEntity, item, deleted: true);
                foreach (var item in saved)
                    TileCRUDController.UpdateTileCache(api.CompanyEntity, item);
                SavedMenu = dashboardMenu;
                SavedMenus = saved;
                return true;
            }

            var result = await new TileCRUDController(api).SaveAsync(definition);
            if (result.Item1 != ErrorCodes.Succes)
            {
                Uniconta.ClientTools.InfoLog.Log.Error(result.Item1);
                return false;
            }
            SavedMenu = result.Item2;
            SavedMenus = new[] { SavedMenu };
            return true;
        }
    }

    public sealed class DashboardSetupStepViewModel : WizardStepViewModel
    {
        string dashboardName, profile;
        TileScope scope = TileScope.User;

        public DashboardSetupStepViewModel(CrudAPI api, UserMenuClient dashboardItem = null)
            : base(GetTitle(dashboardItem),
                Uniconta.ClientTools.Localization.lookup("TileDashboardSetupHelp"),
                Uniconta.ClientTools.Localization.lookup("Configuration"))
        {
            Api = api;
            if (dashboardItem != null)
            {
                var definition = TileDefinition.FromUserMenu(dashboardItem);
                dashboardName = definition.DashboardTab;
                scope = definition.Scope;
                profile = definition.Profile;
            }
        }

        internal static string GetTitle(UserMenuClient dashboardItem) => dashboardItem == null
            ? Uniconta.ClientTools.Localization.lookup("TileCreateDashboard")
            : string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), Uniconta.ClientTools.Localization.lookup("Dashboard"));

        internal CrudAPI Api { get; }
        public string DashboardName
        {
            get => dashboardName;
            set
            {
                if (!SetProperty(ref dashboardName, value)) return;
                OnPropertyChanged(nameof(Summary));
                InvalidateCompletion();
            }
        }
        public TileScope Scope
        {
            get => scope;
            set
            {
                if (!SetProperty(ref scope, value)) return;
                OnPropertyChanged(nameof(SelectedScope));
                OnPropertyChanged(nameof(IsProfile));
                OnPropertyChanged(nameof(Summary));
                InvalidateCompletion();
            }
        }
        // Editors can clear their selection while a step's view is created or removed.
        // Keep those transient values out of the saved setup.
        public TileScope? SelectedScope { get => Scope; set { if (value.HasValue) Scope = value.Value; } }
        public UserProfileClient SelectedProfile
        {
            get => Profiles.FirstOrDefault(item => item.Profile == Profile);
            set { if (value != null) Profile = value.Profile; }
        }
        public string Profile
        {
            get => profile;
            set
            {
                if (!SetProperty(ref profile, value)) return;
                OnPropertyChanged(nameof(SelectedProfile));
                OnPropertyChanged(nameof(Summary));
                InvalidateCompletion();
            }
        }
        public IReadOnlyList<UserProfileClient> Profiles { get; private set; } = Array.Empty<UserProfileClient>();
        public bool IsProfile => Scope == TileScope.Profile;
        public IEnumerable<TileChoice<TileScope>> ScopeOptions => TileDashboardAccess.GetScopes(Api)
            .Select(value => new TileChoice<TileScope>(value, value.DisplayName()));
        public override bool CanContinue => !string.IsNullOrWhiteSpace(DashboardName)
            && TileDashboardAccess.GetScopes(Api).Contains(Scope)
            && (!IsProfile || Profiles.Any(item => item.Profile == Profile));

        public string Summary => Uniconta.ClientTools.Localization.lookup("Name") + ": " + DashboardName?.Trim() + "\n" +
            Uniconta.ClientTools.Localization.lookup("TileScope") + ": " + Scope.DisplayName() +
            (IsProfile ? "\n" + Uniconta.ClientTools.Localization.lookup("Profile") + ": " + Profile : "");

        public override async Task EnterAsync(CancellationToken cancellationToken)
        {
            Profiles = await TileDashboardAccess.LoadProfilesAsync(Api);
            OnPropertyChanged(nameof(Profiles));
            OnPropertyChanged(nameof(SelectedProfile));
            OnPropertyChanged(nameof(CanContinue));
        }

        public override async Task<bool> ValidateAsync(CancellationToken cancellationToken)
        {
            ValidationMessage = string.IsNullOrWhiteSpace(DashboardName)
                ? Uniconta.ClientTools.Localization.lookup("TileADashboardNameIsRequired") : null;
            if (ValidationMessage == null && !TileDashboardAccess.GetScopes(Api).Contains(Scope))
                ValidationMessage = Uniconta.ClientTools.Localization.lookup("NoRights");
            if (ValidationMessage == null && IsProfile)
            {
                var profiles = await TileDashboardAccess.LoadProfilesAsync(Api);
                if (string.IsNullOrWhiteSpace(Profile) || !profiles.Any(item => item.Profile == Profile))
                    ValidationMessage = Uniconta.ClientTools.Localization.lookup("TileSelectAProfile");
            }
            return ValidationMessage == null;
        }
    }

    public sealed class DashboardReviewStepViewModel : WizardStepViewModel
    {
        public DashboardReviewStepViewModel(DashboardSetupStepViewModel setup)
            : base(Uniconta.ClientTools.Localization.lookup("TileReviewDashboard"),
                Uniconta.ClientTools.Localization.lookup("TileDashboardReviewHelp"),
                Uniconta.ClientTools.Localization.lookup("TileReview"))
        { Setup = setup; }
        public DashboardSetupStepViewModel Setup { get; }
        public override async Task<bool> ValidateAsync(CancellationToken cancellationToken)
        {
            var valid = await Setup.ValidateAsync(cancellationToken);
            ValidationMessage = Setup.ValidationMessage;
            return valid;
        }

        public override bool CanContinue => base.CanContinue;
    }

    internal static class TileDashboardAccess
    {
        internal static TileScope[] GetScopes(CrudAPI api) =>
            api != null && api.session.User.IsResellerOrOwner(api.CompanyEntity)
                ? new[] { TileScope.User, TileScope.Profile, TileScope.Company }
                : new[] { TileScope.User, TileScope.Profile };

        internal static async Task<UserProfileClient[]> LoadProfilesAsync(CrudAPI api, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (api == null) return Array.Empty<UserProfileClient>();
            var uid = api.session.User.Uid;
            var memberships = await api.Query<UserProfileUserClient>(new[]
            {
                PropValuePair.GenereteWhereElements(nameof(UserProfileUserClient.Uid), uid.ToString(), CompareOperator.Equal)
            });
            cancellationToken.ThrowIfCancellationRequested();
            // A missing membership result must never expose the full company profile cache.
            if (memberships == null) return Array.Empty<UserProfileClient>();
            var profiles = await api.LoadCache<UserProfileClient>();
            cancellationToken.ThrowIfCancellationRequested();
            return GetMemberProfiles(uid, memberships,
                key => profiles?.Get(key) as UserProfileClient);
        }

        internal static UserProfileClient[] GetMemberProfiles(int uid, IEnumerable<UserProfileUserClient> memberships,
            Func<string, UserProfileClient> resolveProfile) => memberships
            .Where(member => member._Uid == uid && !string.IsNullOrWhiteSpace(member._Profile))
            .Select(member => member._Profile).Distinct()
            .Select(resolveProfile).Where(profile => profile != null)
            .OrderBy(profile => profile.KeyStr).ToArray();
    }
}