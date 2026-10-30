using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EDeliveryMappingGroupSort : IComparer
    {
        public int Compare(object x, object y)
        {
            var xTag = ((eDeliveryMappingGroupClient)x).Name;
            var yTag = ((eDeliveryMappingGroupClient)y).Name;
            return string.Compare(xTag, yTag, StringComparison.CurrentCulture);
        }
    }

    public class EdeliveryMappingGroupGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(eDeliveryMappingGroupClient);
        public override IComparer GridSorting => new EDeliveryMappingGroupSort();
        public override bool Readonly => false;
        public override bool IsAutoSave => false;

        internal UnicontaBaseEntity AddGroupRow() => AddRow(null, ((ItemsSource as IList)?.Count ?? 0) - 1);
    }

    public partial class EDeliveryMappingGroupPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EDeliveryMappingGroupPage; } }

        public EDeliveryMappingGroupPage(BaseAPI api) : base(api, string.Empty)
        {
            InitializeComponent();
            localMenu.dataGrid = dgEdeliveryMappingGroupGrid;
            dgEdeliveryMappingGroupGrid.api = this.api;
            SetRibbonControl(localMenu, dgEdeliveryMappingGroupGrid);
            dgEdeliveryMappingGroupGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgEdeliveryMappingGroupGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }

        void LocalMenu_OnItemClicked(string ActionType)
        {
            dgEdeliveryMappingGroupGrid.View.PostEditor();
            var selectedItem = dgEdeliveryMappingGroupGrid.SelectedItem as eDeliveryMappingGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgEdeliveryMappingGroupGrid.AddGroupRow();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgEdeliveryMappingGroupGrid.DeleteRow();
                    break;
                case "Debtors":
                    if (selectedItem != null)
                    {
                        if (selectedItem.IsDefault)
                        {
                            UnicontaMessageBox.Show(Localization.lookup("eDeliveryMappingMemberDefaultGroup"),
                                Localization.lookup("eDeliveryMappingGroup"));
                            return;
                        }

                        _ = OpenChildPageWithSavedGroupAsync(selectedItem, true);
                    }
                    break;
                case "EDeliveryMapping":
                    if (selectedItem != null)
                        _ = OpenChildPageWithSavedGroupAsync(selectedItem);
                    break;
                case "SaveGrid":
                    gridRibbon_BaseActions(ActionType);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override Task<ErrorCodes> saveGrid()
        {
            dgEdeliveryMappingGroupGrid.View.PostEditor();
            if (HasMultipleDefaults(dgEdeliveryMappingGroupGrid.ItemsSource as IEnumerable<eDeliveryMappingGroupClient>))
            {
                UnicontaMessageBox.Show(Localization.lookup("eDeliveryMappingMultipleDefaultGroup"), Localization.lookup("Error"));
                return Task.FromResult(ErrorCodes.NoSucces);
            }
            return base.saveGrid();
        }

        internal static bool HasMultipleDefaults(IEnumerable<eDeliveryMappingGroupClient> rows) =>
            rows != null && rows.Where(row => row.IsDefault)
                .GroupBy(row => new { row._DocVersion, row._DocType }).Any(group => group.Count() > 1);

        internal static IEnumerable<eDeliveryMappingGroupClient> GetOtherDefaultGroups(
            IEnumerable<eDeliveryMappingGroupClient> rows, eDeliveryMappingGroupClient selected) =>
            (rows ?? Enumerable.Empty<eDeliveryMappingGroupClient>()).Where(row =>
                !ReferenceEquals(row, selected) && row.IsDefault &&
                row._DocVersion == selected._DocVersion && row._DocType == selected._DocType);

        private async Task OpenChildPageWithSavedGroupAsync(eDeliveryMappingGroupClient group, bool members = false)
        {
           if (group.RowId == 0 || dgEdeliveryMappingGroupGrid.HasUnsavedData)
            {
                var msg = UnicontaMessageBox.Show(
                    string.Format(Localization.lookup("SaveChangesFor"), Localization.lookup("eDeliveryMappingGroup")),
                    Localization.lookup("eDeliveryMappingGroup"),
                    System.Windows.MessageBoxButton.YesNo);

                if (msg != System.Windows.MessageBoxResult.Yes)
                    return;

                var saveResult = await saveGrid();
                if (saveResult != ErrorCodes.Succes)
                    return;
            }

            if (members)
                AddDockItem(TabControls.EDeliveryMappingMemberPage, group,
                    string.Format("{0}: {1}", Localization.lookup("eDeliveryMappingGroup"), group._Name));
            else
                AddDockItem(TabControls.EDeliveryMappingPage, group);
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as eDeliveryMappingGroupClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= eDeliveryMappingClient_PropertyChanged;
            var selectedItem = e.NewItem as eDeliveryMappingGroupClient;
            if (selectedItem != null)
            {
                selectedItem.PropertyChanged -= eDeliveryMappingClient_PropertyChanged;
                selectedItem.PropertyChanged += eDeliveryMappingClient_PropertyChanged;
            }
        }

        private void eDeliveryMappingClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var rec = sender as eDeliveryMappingGroupClient;
            switch (e.PropertyName)
            {
                case "IsDefault":
                case "DocVersion":
                case "DocType":
                    if (rec.IsDefault)
                    {
                        var rows = dgEdeliveryMappingGroupGrid.ItemsSource as IEnumerable<eDeliveryMappingGroupClient>;
                        foreach (var other in GetOtherDefaultGroups(rows, rec).ToArray())
                        {
                            dgEdeliveryMappingGroupGrid.SetLoadedRow(other);
                            other.IsDefault = false;
                            dgEdeliveryMappingGroupGrid.SetModifiedRow(other);
                        }
                    }
                    break;
            }
        }

    }
}
