using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Uniconta.API.GeneralLedger;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.API.Inventory;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
   
    public partial class CwJoinTwoWarehouseLocation : ChildWindow
    {
        CrudAPI crudApi;
        public Task<ErrorCodes> JoinResult;
        bool isJoinWareHouse= false;
        InvWarehouse wareHouse;
        public CwJoinTwoWarehouseLocation(CrudAPI api, InvWarehouse _warehouse, bool joinWareHouse= true)
        {
            DataContext = this;
            InitializeComponent();
            wareHouse = _warehouse;
            isJoinWareHouse = joinWareHouse;
            crudApi = api;
            Loaded += CwJoinTwoWarehouseLocation_Loaded;
            tbCpyFrmWareHouse.Text = string.Format( "{0} {1}",Uniconta.ClientTools.Localization.lookup("CopyFrom"), Uniconta.ClientTools.Localization.lookup("Warehouse"));
            tbCpyFrmLoc.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("CopyFrom"), Uniconta.ClientTools.Localization.lookup("Location"));
            tbToWareHouse.Text= string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("CopyTo"), Uniconta.ClientTools.Localization.lookup("Warehouse"));
            tbToLoc.Text= string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("CopyTo"), Uniconta.ClientTools.Localization.lookup("Location"));
            if (joinWareHouse)
            {
                Title = string.Format(Uniconta.ClientTools.Localization.lookup("JoinTwoOBJ"), Uniconta.ClientTools.Localization.lookup("Warehouse"));

                rowCpyLoc.Height = new GridLength(0);
                rowToLoc.Height = new GridLength(0);
                double h1 = this.Height - 60;
                this.Height = h1;
                tbCpyFrmLoc.Visibility = cmbFromLocation.Visibility = tbLCToBeDeltd.Visibility =
                tbToLoc.Visibility = cmbToLocation.Visibility = Visibility.Collapsed;
            }
            else
            {
                Title = string.Format(Uniconta.ClientTools.Localization.lookup("JoinTwoOBJ"), Uniconta.ClientTools.Localization.lookup("Location"));
                tbWHToBeDeltd.Visibility = Visibility.Collapsed;
            }
        }

        private void CwJoinTwoWarehouseLocation_Loaded(object sender, RoutedEventArgs e)
        {
            SetItemSource();
            Dispatcher.BeginInvoke(new Action(() => { cmbFromWareHouse.Focus(); }));
        }

        async private void SetItemSource()
        {
            var comp = crudApi.CompanyEntity;
            var wareHouseCache = comp.GetCache(typeof(InvWarehouse)) ?? await comp.LoadCache(typeof(InvWarehouse), crudApi);
            cmbFromWareHouse.ItemsSource = wareHouseCache;
            cmbToWareHouse.ItemsSource = wareHouseCache;
            cmbFromWareHouse.SelectedItem = wareHouse;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        async private void cmbFromWareHouse_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var wareHouse = cmbFromWareHouse.SelectedItem as UnicontaBaseEntity;
            if (wareHouse != null)
                cmbFromLocation.ItemsSource = await crudApi.Query<InvLocation>(new UnicontaBaseEntity[] { wareHouse }, null);
            else
                cmbFromLocation.ItemsSource = null;
        }

        async private void cmbToWareHouse_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var wareHouse = cmbToWareHouse.SelectedItem as UnicontaBaseEntity;
            if (wareHouse != null)
                cmbToLocation.ItemsSource = await crudApi.Query<InvLocation>(new UnicontaBaseEntity[] { wareHouse }, null);
            else
                cmbToLocation.ItemsSource = null;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var copyFromWareHouse = cmbFromWareHouse.SelectedItem as InvWarehouse;
            var copyToWareHouse = cmbToWareHouse.SelectedItem as InvWarehouse;

            var copyFromLocation = cmbFromLocation.SelectedItem as InvLocation;
            var copyToLocation = cmbToLocation.SelectedItem as InvLocation;

            if (copyFromWareHouse != null && isJoinWareHouse)
                CallJoinTwoWareHouseOrLocation(fromWareHouse:copyFromWareHouse, copyToWareHouse:copyToWareHouse);
            else if (copyFromWareHouse != null && copyFromLocation!= null && !isJoinWareHouse)
                CallJoinTwoWareHouseOrLocation(fromLocation:copyFromLocation, copyToWareHouse: copyToWareHouse, copyToLocation: copyToLocation);
            else
                Uniconta.ClientTools.Controls.UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("CopyFrom"))), Uniconta.ClientTools.Localization.lookup("Error"));
        }

        void CallJoinTwoWareHouseOrLocation(InvWarehouse fromWareHouse=null, InvWarehouse copyToWareHouse=null, InvLocation fromLocation=null, InvLocation copyToLocation=null)
        {
            DeletePostedJournal delDialog = new DeletePostedJournal(true);
            delDialog.Closed += delegate
            {
                if (delDialog.DialogResult == true)
                {
                    var invApi = new InventoryAPI(crudApi);
                    if (isJoinWareHouse)
                        JoinResult = invApi.JoinTwoWarehouses(fromWareHouse, copyToWareHouse);
                    else
                        JoinResult = invApi.JoinTwoLocations(fromLocation, copyToWareHouse, copyToLocation);
                    SetDialogResult(true);
                }

            };
            delDialog.Show();
        }
    }
}
