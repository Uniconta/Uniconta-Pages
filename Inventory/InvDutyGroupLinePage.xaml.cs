using UnicontaClient.Models;
using UnicontaClient.Pages;
using DevExpress.Xpf.Grid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class InvDutyGroupLineGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvDutyGroupLineClient); } }
        public override bool Readonly { get { return false; } }
    }

    /// <summary>
    /// Interaction logic for InvDutyGroupLinePage.xaml
    /// </summary>
    public partial class InvDutyGroupLinePage : GridBasePage
    {
        SQLCache DebtorCache, CreditorCache, DebtorGroupCache, CreditorGroupCache;
        public override string NameOfControl { get { return TabControls.InvDutyGroupLinePage; } }
        public InvDutyGroupLinePage(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            ((TableView)dgInvDutyGroupLineGrid.View).RowStyle = System.Windows.Application.Current.Resources["GridRowControlCustomHeightStyle"] as Style;
            dgInvDutyGroupLineGrid.UpdateMaster(master);
            localMenu.dataGrid = dgInvDutyGroupLineGrid;
            SetRibbonControl(localMenu, dgInvDutyGroupLineGrid);
            dgInvDutyGroupLineGrid.api = api;
            dgInvDutyGroupLineGrid.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            dgInvDutyGroupLineGrid.SelectedItemChanged += DgInvDutyGroupLineGrid_SelectedItemChanged;
        }

        private void DgInvDutyGroupLineGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            var oldselectedItem = e.OldItem as InvDutyGroupLineClient;
            if (oldselectedItem != null)
                oldselectedItem.PropertyChanged += selectedItem_PropertyChanged;
            var selectedItem = e.NewItem as InvDutyGroupLineClient;
            if (selectedItem != null)
                selectedItem.PropertyChanged += selectedItem_PropertyChanged;
        }

        private void selectedItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var selectedItem = sender as InvDutyGroupLineClient;
            switch (e.PropertyName)
            {
                case nameof(selectedItem.Sales):
                    selectedItem.SalesAccount = selectedItem.SalesGroup = null;
                    selectedItem.SalesAccountSource = selectedItem.SalesGroupSource = null;
                    SetSalesAccountSource(selectedItem);
                    SetSalesGroupSource(selectedItem);
                    break;
            }
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch(ActionType)
            {
                case "AddRow":
                    dgInvDutyGroupLineGrid.AddRow();
                    break;
                case "CopyRow":
                    dgInvDutyGroupLineGrid.CopyRow();
                    break;
                case "SaveGrid":
                    dgInvDutyGroupLineGrid.SaveData();
                    break;
                case "DeleteRow":
                    var selectedItem = dgInvDutyGroupLineGrid.SelectedItem as InvDutyGroupLineClient;
                    if (selectedItem != null)
                        dgInvDutyGroupLineGrid.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            if (DebtorCache == null)
                DebtorCache = await api.LoadCache(typeof(Uniconta.DataModel.Debtor)).ConfigureAwait(false);
            if (CreditorCache == null)
                CreditorCache = await api.LoadCache(typeof(Uniconta.DataModel.Creditor)).ConfigureAwait(false);
            if (DebtorGroupCache == null)
                DebtorGroupCache = await api.LoadCache(typeof(Uniconta.DataModel.DebtorGroup)).ConfigureAwait(false);
            if (CreditorGroupCache == null)
                CreditorGroupCache = await api.LoadCache(typeof(Uniconta.DataModel.CreditorGroup)).ConfigureAwait(false);
        }
        private void SalesAccount_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvDutyGroupLineGrid.SelectedItem as InvDutyGroupLineClient;
            if (selectedItem != null)
                SetSalesAccountSource(selectedItem);
        }

        private void SalesGroup_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvDutyGroupLineGrid.SelectedItem as InvDutyGroupLineClient;
            if (selectedItem != null)
                SetSalesGroupSource(selectedItem);
        }

        private void SetSalesAccountSource(InvDutyGroupLineClient selectedItem)
        {
            SQLCache cache = selectedItem._Sales ? DebtorCache : CreditorCache;
            if (cache != null && selectedItem.SalesAccountSource == null)
                selectedItem.SalesAccountSource = cache.GetNotNullArray;
        }

        private void SetSalesGroupSource(InvDutyGroupLineClient selectedItem)
        {
            SQLCache cache = selectedItem._Sales ? DebtorGroupCache : CreditorGroupCache;
            if (cache != null && selectedItem.SalesGroupSource == null)
                selectedItem.SalesGroupSource = cache.GetNotNullArray;
        }
    }
}
