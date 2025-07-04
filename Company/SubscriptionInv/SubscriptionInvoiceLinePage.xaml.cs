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
        public int Compare(object _x, object _y)
        {
            var x = (SubscriptionInvoiceLine)_x;
            var y = (SubscriptionInvoiceLine)_y;

            var c = x.Sid - y.Sid;
            if (c != 0)
                return c;
            c = DateTime.Compare(x._Date, y._Date);
            if (c != 0)
                return c;
            return x._LineNumber - y._LineNumber;
        }
    }
    public class SubscriptionInvoiceLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(SubscriptionInvoiceLineClient); } }
        public override IComparer GridSorting { get { return new SubscriptionInvoiceLineSort(); } }
    }
    public partial class SubscriptionInvoiceLinePage : GridBasePage
    {
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
            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("InvoiceNumber"), ": ", syncMaster.Invoice);
            SetHeader(header);
        }
        private void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgSubInvoiceslineGrid.UpdateMaster(master);
            SetRibbonControl(localMenu, dgSubInvoiceslineGrid);
            localMenu.dataGrid = dgSubInvoiceslineGrid;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgSubInvoiceslineGrid.api = api;
            dgSubInvoiceslineGrid.BusyIndicator = busyIndicator;
        }

        protected override Filter[] DefaultFilters()
        {
            if (dgSubInvoiceslineGrid.masterRecords == null)
                return new[] { new Filter() { name = "Date", value = String.Format("{0:d}..", GetSystemDefaultDate().AddMonths(-1)) } };
            else
                return null;
        }
        void localMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }
    }
}
