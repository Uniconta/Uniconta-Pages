using Uniconta.API.DebtorCreditor;
using UnicontaClient.Models;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
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
using Uniconta.ClientTools.Util;
using Uniconta.ClientTools;
using Uniconta.Common.Utility;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditorSettlementGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorTransClient); } }        
        public override bool SingleBufferUpdate { get { return false; } }
    }
    public partial class CreditorSettlements : GridBasePage
    {
        public CreditorSettlements(UnicontaBaseEntity master)
            : base(master)
        {
            Initialize(master);
        }
        public CreditorSettlements(SynchronizeEntity syncEntity)
            : base(syncEntity, true)
        {
            Initialize(syncEntity.Row);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgSettlements.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        void SetHeader()
        {
            var syncMaster = dgSettlements.masterRecord as CreditorTransClient;
            if (syncMaster == null)
                return;
            StreamingManager.Copy(syncMaster as UnicontaBaseEntity, this.master);
            string header = Util.ConcatParenthesis(Uniconta.ClientTools.Localization.lookup("Settlements"), syncMaster.Voucher);
            SetHeader(header);
        }

        public CreditorTransClient master;
        private void Initialize(UnicontaBaseEntity master)
        {
            InitializeComponent();
            rApi = new ReportAPI(api);
            this.master = new CreditorTransClient();
            StreamingManager.Copy(master, this.master);
            localMenu.dataGrid = dgSettlements;
            dgSettlements.api = api;
            SetRibbonControl(localMenu, dgSettlements);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
            dgSettlements.BusyIndicator = busyIndicator;
            var Comp = api.CompanyEntity;
            if (Comp.RoundTo100)
                Amount.HasDecimals = Remaining.HasDecimals = Debit.HasDecimals = Credit.HasDecimals = false;
        }

        ReportAPI rApi;
        public async override Task InitQuery()
        {
            var selectedItem = dgSettlements.SelectedItem;
            var isSettledArray = (CreditorTransClient[])await rApi.GetTransactionsClosed(master);
            if (isSettledArray != null)
            {
                foreach (var rec in isSettledArray)
                    rec.IsSettled = true;
            }
            var hasSettledArray = (CreditorTransClient[])await rApi.GetTransactionsClosedBy(master);
            if (hasSettledArray != null && hasSettledArray.Length > 0)
                dgSettlements.ItemsSource = isSettledArray.Concat(hasSettledArray);
            else
                dgSettlements.ItemsSource = isSettledArray;
            dgSettlements.Visibility = Visibility.Visible;
            if (selectedItem != null)
                dgSettlements.SelectedItem = selectedItem;
        }

        async void Settle(CreditorTransClient trans)
        {
            TransactionAPI tranApi = new TransactionAPI(api);
            ErrorCodes err;
            if (trans.IsSettled)
                err = await tranApi.ReOpen(master, trans);
            else
                err = await tranApi.ReOpen(trans, master);
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            else
                InitQuery();
        }
        private void localMenu_OnItemClicked(string ActionType)
        {
            CreditorTransClient trans = dgSettlements.SelectedItem as CreditorTransClient;
            switch (ActionType)
            {
                case "ReOpen":
                    if (trans != null)
                        Settle(trans);
                    break;
                case "RefreshGrid":
                    InitQuery();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public override string NameOfControl
        {
            get { return TabControls.CreditorSettlements; }
        }
    }
}
