using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class LocationClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvLocationClient); } }
        public override IComparer GridSorting { get { return new InvLocationClientSort(); } }
        public override bool Readonly{ get { return false;}}
    }
    public partial class LocationPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.LocationPage; } }
        UnicontaBaseEntity master;
        public LocationPage(UnicontaBaseEntity sourcedata) : base(sourcedata)
        {
            Init(sourcedata);
        }

        public LocationPage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            Init(syncEntity.Row);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgLocationClientGrid.UpdateMaster(args);
            master = args;
            SetHeader();
            InitQuery();
        }

        void SetHeader()
        {
            var syncMaster = master as InvWarehouseClient;
            if (master == null)
                return;
            SetHeader(string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Location"), syncMaster.Name));
        }

        void Init(UnicontaBaseEntity sourcedata)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgLocationClientGrid);
            dgLocationClientGrid.api = api;
            dgLocationClientGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgLocationClientGrid.UpdateMaster(sourcedata);
            master = sourcedata;
            SetHeader();
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgLocationClientGrid.SelectedItem as InvLocationClient;
            switch (ActionType)
            {
                case "AddRow":
                   dgLocationClientGrid.AddRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    dgLocationClientGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
