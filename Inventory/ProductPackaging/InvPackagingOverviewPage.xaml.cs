using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPackagingOverviewGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPackagingOverviewLine); } }
        public override bool Readonly { get { return true; } }
    }

    public partial class InvPackagingOverviewPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvPackagingOverviewPage; } }

        InvItem item;

        public InvPackagingOverviewPage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }

        public InvPackagingOverviewPage(BaseAPI API, string lookupKey) : base(API, lookupKey)
        {
            Init(null);
        }

        public InvPackagingOverviewPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        public InvPackagingOverviewPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            Init(syncEntity.Row);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            var newItem = args as InvItem;
            if (newItem == null)
                return;
            item = newItem;
            SetItemHeader();
            LoadGrid();
        }

        void SetItemHeader()
        {
            if (item != null)
                SetHeader(string.Concat(Uniconta.ClientTools.Localization.lookup("PackagingOverview"), ": ", Utility.GetHeaderString(item)));
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            item = master as InvItem;
            localMenu.dataGrid = dgInvPackagingOverviewGrid;
            SetRibbonControl(localMenu, dgInvPackagingOverviewGrid);
            dgInvPackagingOverviewGrid.api = api;
            dgInvPackagingOverviewGrid.BusyIndicator = busyIndicator;
            dgInvPackagingOverviewGrid.ShowTotalSummary();
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            SetItemHeader();
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (item != null)
                Item.Visible = ItemName.Visible = false;
        }

        void ToggleGrouping()
        {
            var grouped = ReportingGroup.GroupIndex >= 0;
            if (grouped)
                dgInvPackagingOverviewGrid.ClearGrouping();
            else
            {
                int idx = 0;
                if (item == null)
                    Item.GroupIndex = idx++;
                ReportingGroup.GroupIndex = idx;
            }
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = false;
            return true;
        }

        public override Task InitQuery()
        {
            return LoadGrid();
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "RefreshGrid":
                    LoadGrid();
                    break;
                case "Grouping":
                    ToggleGrouping();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async Task LoadGrid()
        {
            busyIndicator.IsBusy = true;
            try
            {
                var expander = new PackagingExpander(api);
                await expander.Load();

                var lines = new List<InvPackagingOverviewLine>();
                if (item != null)
                    lines.AddRange(await expander.Expand(item));
                else
                {
                    foreach (var it in expander.ItemsWithPackaging())
                        lines.AddRange(expander.ExpandOwn(it));
                    foreach (var bom in expander.ProductionBOMsWithoutPackaging())
                        lines.AddRange(await expander.Expand(bom));
                }
                var arr = Array.CreateInstance(expander.LineType, lines.Count);
                for (int i = 0; i < lines.Count; i++)
                    arr.SetValue(lines[i], i);
                dgInvPackagingOverviewGrid.SetSource((UnicontaBaseEntity[])arr);
            }
            finally
            {
                busyIndicator.IsBusy = false;
            }
        }
    }
}
