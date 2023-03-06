using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Uniconta.ClientTools.Util;

using Uniconta.API.GeneralLedger;
using System.Text;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvPriceListFeeClientGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvPriceListFeeClient); } }
        public override bool Readonly { get { return false; } }
        protected override List<string> GridSkipFields { get { return new List<string>(2) { "Name" }; } }
        protected override bool SetValuesOnPaste { get { return true; } }
        public override bool SingleBufferUpdate { get { return false; } }
        public override bool IsAutoSave { get { return false; } }

        internal byte FixedDCType;
        public override void SetDefaultValues(UnicontaBaseEntity dataEntity, int selectedIndex)
        {
            if (FixedDCType != 0)
                ((Uniconta.DataModel.InvPriceListFee)dataEntity)._DCType = FixedDCType;
        }
    }

    public partial class InvPriceListFeePage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvPriceListFeePage; } }
        public InvPriceListFeePage(UnicontaBaseEntity sourcedata)
            : base(sourcedata)
        {
            InitializeComponent();
            Init(sourcedata);
        }
        public InvPriceListFeePage(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            Init(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgInvPriceListFee.UpdateMaster(args);
            SetHeader();
            BindGrid();
        }

        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgInvPriceListFee.masterRecord);
            if (string.IsNullOrEmpty(key)) return;
            string header = string.Concat(Uniconta.ClientTools.Localization.lookup("InvoiceFee"), ": ", key);
            SetHeader(header);
        }

        private void Init(UnicontaBaseEntity master)
        {
            dgInvPriceListFee.UpdateMaster(master);
            localMenu.dataGrid = dgInvPriceListFee;
            SetRibbonControl(localMenu, dgInvPriceListFee);
            dgInvPriceListFee.api = api;
            dgInvPriceListFee.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;

            var Comp = api.CompanyEntity;
            if (Comp.InvPrice != Comp.CreditorPrice)
                dgInvPriceListFee.FixedDCType = Comp.InvPrice ? (byte)1 : (byte)2;
        }

        Task BindGrid()
        {
            return dgInvPriceListFee.Filter(null);
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            bool ShowDC = true, ShowItem = true;
            var master = dgInvPriceListFee.masterRecords?.First();

            var dc = master as Uniconta.DataModel.DCAccount;
            if (dc != null)
            {
                dgInvPriceListFee.FixedDCType = dc.__DCType();
                ShowDC = false;
                if (dc.__DCType() == 1)
                    PriceListCreditor.Visible = false;
                else
                    PriceListDebtor.Visible = false;
            }
            var pList = master as Uniconta.DataModel.InvPriceList;
            if (pList != null)
            {
                dgInvPriceListFee.FixedDCType = pList.__DCType();
                ShowDC = false;
            }
            if (master is Uniconta.DataModel.InvItem)
                ShowItem = false;

            Item.Visible = Name.Visible = ShowItem;
            if (ShowDC)
                PriceListCreditor.Visible = PriceListDebtor.Visible = ShowDC;

            var Comp = api.CompanyEntity;
            if (!Comp.CreditorPrice)
                PriceListCreditor.Visible = false;
            if (!Comp.InvPrice)
                PriceListDebtor.Visible = false;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgInvPriceListFee.AddRow();
                    break;
                case "CopyRow":
                    dgInvPriceListFee.CopyRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                case "DeleteRow":
                    if (dgInvPriceListFee.SelectedItem != null)
                        dgInvPriceListFee.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override void LoadCacheInBackGround()
        {
            LoadType(new Type[] { typeof(Uniconta.DataModel.InvItem), typeof(Uniconta.DataModel.GLAccount) });
        }
    }
}
