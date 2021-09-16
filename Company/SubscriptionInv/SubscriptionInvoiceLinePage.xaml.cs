using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;
using Uniconta.DataModel;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.API.Service;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class SubscriptionInvoiceLineSort : IComparer
    {
        public int Compare(object x, object y)
        {
            return ((SubscriptionInvoiceLine)x)._LineNumber - ((SubscriptionInvoiceLine)y)._LineNumber;
        }
    }
    public class SubscriptionInvoiceLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(SubscriptionInvoiceLineClient); } }
        public override IComparer GridSorting { get { return new SubscriptionInvoiceLineSort(); } }
    }
    public partial class SubscriptionInvoiceLinePage : GridBasePage
    {
        List<UnicontaBaseEntity> masterList;
        public override string NameOfControl { get { return TabControls.SubscriptionInvoiceLinePage.ToString(); } }
        public SubscriptionInvoiceLinePage(BaseAPI API)
            : base(API, string.Empty)
        {
            InitPage(null);
        }
        public SubscriptionInvoiceLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitPage(master);
        }
        public SubscriptionInvoiceLinePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            this.syncEntity = syncEntity;
            InitPage(syncEntity.Row);
        }
        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgSubInvoiceslineGrid.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgSubInvoiceslineGrid.masterRecord as SubscriptionInvoiceClient;
            if (syncMaster == null)
                return;
            string header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), syncMaster.Invoice);
            SetHeader(header);
        }
        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            if (master != null)
            {
                masterList = new List<UnicontaBaseEntity>();
                masterList.Add(master);
                dgSubInvoiceslineGrid.masterRecords = masterList;
            }

            SetRibbonControl(localMenu, dgSubInvoiceslineGrid);
            localMenu.dataGrid = dgSubInvoiceslineGrid;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgSubInvoiceslineGrid.api = api;
            dgSubInvoiceslineGrid.BusyIndicator = busyIndicator;
        }

        protected override Filter[] DefaultFilters()
        {
            Filter dateFilter = new Filter();
            dateFilter.name = "Date";
            dateFilter.value = String.Format("{0:d}..", GetSystemDefaultDate().AddMonths(-1));
            return new Filter[] { dateFilter };
        }
        void localMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }
    }
}
