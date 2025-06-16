using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
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
            return xTag.CompareTo(yTag);
        }
    }

    public class EdeliveryMappingGroupGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(eDeliveryMappingGroupClient);
        public override IComparer GridSorting => new EDeliveryMappingGroupSort();
        public override bool Readonly => false;
        public override bool IsAutoSave => false;
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
            var selectedItem = dgEdeliveryMappingGroupGrid.SelectedItem as eDeliveryMappingGroupClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgEdeliveryMappingGroupGrid.AddRow();
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

                        AddDockItem(TabControls.EDeliveryMappingMemberPage, selectedItem,
                            string.Format("{0}: {1}", Localization.lookup("eDeliveryMappingGroup"), selectedItem._Name));
                    }
                    break;
                case "EDeliveryMapping":
                    if (selectedItem != null)
                        AddDockItem(TabControls.EDeliveryMappingPage, selectedItem);
                    break;
                case "SaveGrid":
                    var rows = dgEdeliveryMappingGroupGrid.ItemsSource as List<eDeliveryMappingGroupClient>;
                    if (rows == null || rows.Count == 0)
                        return;

                    var multipleDefault = rows
                        .GroupBy(x => x.IsDefault.ToString() + x.DocType + x.DocVersion)
                        .Any(x => x.Count() > 1);

                    if (multipleDefault)
                    {
                        UnicontaMessageBox.Show(Localization.lookup("eDeliveryMappingMultipleDefaultGroup"),
                            Localization.lookup("Error"));
                        return;
                    }

                    gridRibbon_BaseActions(ActionType);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as eDeliveryMappingGroupClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged += eDeliveryMappingClient_PropertyChanged;
            var selectedItem = e.NewItem as eDeliveryMappingGroupClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += eDeliveryMappingClient_PropertyChanged;
        }

        private void eDeliveryMappingClient_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var rec = sender as eDeliveryMappingGroupClient;
            switch (e.PropertyName)
            {
                case "IsDefault":
                    if (rec.IsDefault)
                    {
                        var rows = dgEdeliveryMappingGroupGrid.ItemsSource as IEnumerable<eDeliveryMappingGroupClient>;
                        dgEdeliveryMappingGroupGrid.ItemsSource = rows?
                            .Select(x =>
                            {
                                if (x._DocVersion == rec._DocVersion && x._DocType == rec._DocType)
                                    x.IsDefault = false;

                                return x;
                            })?
                            .ToList();
                    }
                    break;
            }
        }
    }
}
