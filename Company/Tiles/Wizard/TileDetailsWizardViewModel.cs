using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls.Wizard;
using Uniconta.ClientTools.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public sealed class TileDetailsWizardViewModel : WizardViewModel
    {
        public TileDetailsWizardViewModel(CrudAPI api, UserMenuClient row, TileScope? scope = null, string dashboardTab = null, string profile = null)
            : this(new TileCRUDController(api).Load(row, scope, dashboardTab, profile)) { }

        TileDetailsWizardViewModel(TileDataViewModel data) : 
        base(new WizardStepViewModel[] 
            { 
                new TileTypeStepViewModel(data), new TileConfigurationStepViewModel(data),
                new TilePlacementStepViewModel(data), new TileReviewStepViewModel(data) 
            },
            data.IsExisting ? Uniconta.ClientTools.Localization.lookup("TileEditYourTile") : Uniconta.ClientTools.Localization.lookup("TileCreateYourTile"), Uniconta.ClientTools.Localization.lookup("TileConfigureATileForYourDashboard"), logo: null /* no logo */)
        {
            Data = data;
            data.Attach(this);
            var parameters = new TileParametersStepViewModel(data);
            data.ParameterTypeChanged += () => SetOptionalStep(2, parameters, data.HasParameters);
            SetOptionalStep(2, parameters, data.HasParameters);
            data.Edited += index => ((TileDetailsStepViewModel)Steps[index]).Invalidate();
            if (data.IsExisting)
                MarkStepsCompleted();
        }

        public TileDataViewModel Data { get; }
        protected override Task<bool> CompleteAsync(CancellationToken cancellationToken) => Data.SaveAsync();
    }

    public abstract class TileDetailsStepViewModel : WizardStepViewModel
    {
        protected TileDetailsStepViewModel(TileDataViewModel data, string title, string description, string navigationTitle) : base(title, description, navigationTitle) { Data = data; }
        public TileDataViewModel Data { get; }
        public override bool SupportsCancellation => true;
        internal void Invalidate() => InvalidateCompletion();
        public override async Task<bool> ValidateAsync(CancellationToken cancellationToken)
        {
            if (Data.IsProfile) await Data.LoadProfilesAsync(cancellationToken);
            ValidationMessage = Data.ValidationError();
            return string.IsNullOrEmpty(ValidationMessage);
        }
    }
    public sealed class TileTypeStepViewModel : TileDetailsStepViewModel
    {
        public TileTypeStepViewModel(TileDataViewModel data) : base(data, Uniconta.ClientTools.Localization.lookup("TileWhatKindOfTileDoYouWant"), Uniconta.ClientTools.Localization.lookup("TileChooseAVisualizationOrAStandaloneTile"), Uniconta.ClientTools.Localization.lookup("TileType")) { }
        public override bool CanContinue => Data.SelectedTileType != null;
        public override Task<bool> ValidateAsync(CancellationToken cancellationToken)
        {
            ValidationMessage = CanContinue ? null : Uniconta.ClientTools.Localization.lookup("TileSelectATileType");
            return Task.FromResult(CanContinue);
        }
    }
    public sealed class TileConfigurationStepViewModel : TileDetailsStepViewModel
    {
        public TileConfigurationStepViewModel(TileDataViewModel data) : base(data, Uniconta.ClientTools.Localization.lookup("TileConfigureYourTile"), Uniconta.ClientTools.Localization.lookup("TileChooseTheDataAndSettingsForThisTile"), Uniconta.ClientTools.Localization.lookup("Configuration")) { }
    }

    public sealed class TileParametersStepViewModel : TileDetailsStepViewModel
    {
        public TileParametersStepViewModel(TileDataViewModel data)
            : base(data, Uniconta.ClientTools.Localization.lookup("Parameters"), null, Uniconta.ClientTools.Localization.lookup("Parameters")) { }
    }

    public sealed class TilePlacementStepViewModel : TileDetailsStepViewModel
    {
        public TilePlacementStepViewModel(TileDataViewModel data) : base(data, Uniconta.ClientTools.Localization.lookup("TileChooseWhereItBelongs"), Uniconta.ClientTools.Localization.lookup("TileSetTheScopeDashboardGroupAndNavigation"), Uniconta.ClientTools.Localization.lookup("TilePlacement")) { }
        public override Task EnterAsync(CancellationToken cancellationToken) => Data.LoadProfilesAsync(cancellationToken);
        public override Task LeaveAsync(CancellationToken cancellationToken)
        {
            Data.CommitPlacement();
            return Task.CompletedTask;
        }
    }

    public sealed class TileReviewStepViewModel : TileDetailsStepViewModel
    {
        public TileReviewStepViewModel(TileDataViewModel data) : base(data, Uniconta.ClientTools.Localization.lookup("TileReviewYourTile"), Uniconta.ClientTools.Localization.lookup("TileReviewTheConfigurationAndFinishToSaveYour"), Uniconta.ClientTools.Localization.lookup("TileReview")) { }
        public override Task EnterAsync(CancellationToken cancellationToken) => Data.PrepareReviewAsync(cancellationToken);
    }
}
