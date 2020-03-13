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
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvSerieBatchStorageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvSerieBatchStorageClient); } }
        public override bool Readonly { get { return true; } }
    }
    public partial class InvSerieBatchStoragePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvSerieBatchStorage; } }

        public InvSerieBatchStoragePage(SynchronizeEntity sync) : base(sync, true)
        {
            this.syncEntity = sync;
            Init();
        }

        private void Init()
        {
            InitializeComponent();
            dgInvSerieBatchStorageGrid.UpdateMaster(this.syncEntity.Row);
            SetRibbonControl(localMenu, dgInvSerieBatchStorageGrid);
            dgInvSerieBatchStorageGrid.api = api;
            dgInvSerieBatchStorageGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvSerieBatchStorageGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgInvSerieBatchStorageGrid.masterRecord as Uniconta.DataModel.InvSerieBatch;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("BatchLocations"), syncMaster._Number);
            SetHeader(header);
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }
    }
}
