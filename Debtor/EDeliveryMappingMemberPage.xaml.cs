using System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EDeliveryMappingMemberGrid : CorasauDataGridClient
    {
        public override Type TableType => typeof(eDeliveryMappingMemberClient);
        public override bool Readonly => false;
        public override bool IsAutoSave => false;

    }
    public partial class EDeliveryMappingMemberPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.EDeliveryMappingMemberPage; } }

        public EDeliveryMappingMemberPage(eDeliveryMappingGroupClient master) : base(master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgeDeliveryMappingMemberGrid);
            dgeDeliveryMappingMemberGrid.api = api;
            dgeDeliveryMappingMemberGrid.UpdateMaster(master);
            dgeDeliveryMappingMemberGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgeDeliveryMappingMemberGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }

        void SetHeader()
        {
            string header = string.Empty;
            var group = dgeDeliveryMappingMemberGrid.masterRecord as eDeliveryMappingGroupClient;
            if (group != null)
                header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TaskGroups"), group.KeyName);

            SetHeader(header);
        }

        async void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgeDeliveryMappingMemberGrid.SelectedItem as eDeliveryMappingMemberClient;
            switch (ActionType)
            {
                case "AddRow":
                    dgeDeliveryMappingMemberGrid.AddRow();
                    break;
                case "DeleteRow":
                    if (selectedItem != null)
                        dgeDeliveryMappingMemberGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
