using UnicontaClient.Models;
using System;
using Uniconta.API.Service;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.ClientTools;
using System.Windows.Controls;
using System.Windows;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvDutyExemptPageGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvDutyExemptClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class InvDutyExemptPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.InvDutyExemptPage; } }
        public InvDutyExemptPage(UnicontaBaseEntity master)
           : base(master)
        {
            Init(master);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            localMenu.dataGrid = dgInvExemptGrid;
            SetRibbonControl(localMenu, dgInvExemptGrid);
            dgInvExemptGrid.api = api;
            dgInvExemptGrid.BusyIndicator = busyIndicator;
            dgInvExemptGrid.UpdateMaster(master);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgInvExemptGrid.View.DataControl.CurrentItemChanged += DataControl_CurrentItemChanged;
        }

        SQLCache DebtorCache, CreditorCache;
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            if (DebtorCache == null)
                DebtorCache = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvExemptGrid.SelectedItem as InvDutyExemptClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    if (selectedItem == null) return;
                    dgInvExemptGrid.DeleteRow(false);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        void DataControl_CurrentItemChanged(object sender, DevExpress.Xpf.Grid.CurrentItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvDutyExemptClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged -= InvDutyExemptClient_PropertyChanged;
            var selectedItem = e.NewItem as InvDutyExemptClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += InvDutyExemptClient_PropertyChanged;
        }

        void InvDutyExemptClient_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var rec = sender as InvDutyExemptClient;
            switch (e.PropertyName)
            {
                case "DCType":
                    SetAccountSource(rec);
                    break;
            }
        }

        private void Account_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvExemptGrid.SelectedItem as InvDutyExemptClient;
            if (selectedItem != null)
                SetAccountSource(selectedItem);
        }

        private void SetAccountSource(InvDutyExemptClient rec)
        {
            var cache = rec._DCType == 1 ? DebtorCache : CreditorCache;
            if (cache != null)
            {
                rec.accntSource = cache.GetNotNullArray;
                rec.NotifyPropertyChanged("AccountSource");
            }
        }
    }
}
