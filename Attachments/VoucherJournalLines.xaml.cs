using UnicontaClient.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools.Controls;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class VoucherJournalLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLDailyJournalLineClient); } }
    }
    public partial class VoucherJournalLines : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.VoucherJournalLines.ToString(); } }
        public VoucherJournalLines(SynchronizeEntity entity)
            : base(entity, true)
        {
            this.syncEntity = entity;
            InitPage(syncEntity.Row);
            SetHeader();
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgJournalLinesGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var masterClient = dgJournalLinesGrid.masterRecord as VouchersClient;
            if (masterClient == null)
                return;
            string header = string.Format("{0}/{1}", Uniconta.ClientTools.Localization.lookup("Journallines"), masterClient.RowId);
            SetHeader(header);
        }
        private void InitPage(UnicontaBaseEntity master = null)
        {
            InitializeComponent();
            if (master != null)
                dgJournalLinesGrid.UpdateMaster(master);

            SetRibbonControl(null, dgJournalLinesGrid);
            dgJournalLinesGrid.api = api;
            dgJournalLinesGrid.BusyIndicator = busyIndicator;
        }
        protected override void LoadCacheInBackGround()
        {
            LoadType(typeof(Uniconta.DataModel.GLDailyJournal));
        }
    }
}
