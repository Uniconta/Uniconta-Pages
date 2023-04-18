using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CwMoveBtwWareHouse : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.InvJournal))]
        public string Journal { get; set; }
        public Uniconta.DataModel.InvJournal InvJournal;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.PrJournal))]
        public string ProjJournal { get; set; }
        public Uniconta.DataModel.PrJournal PrJournal;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.InvWarehouse))]
        public string Warehouse { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.InvLocation))]
        public string Location { get; set; }

        CrudAPI API;

        bool isPrjJournal = false;
        public CwMoveBtwWareHouse(CrudAPI api)
        {
            this.DataContext = this;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("MoveFromWarehouse");
            API = api;
            lookupJournal.api = leWarehouse.api = leLocation.api = lookupPrJournal.api = api;
            this.Loaded += CW_Loaded;
        }

        public CwMoveBtwWareHouse(bool _isPrjJournal, CrudAPI api) : this(api)
        {
            isPrjJournal = _isPrjJournal;
            if (isPrjJournal)
            {
                this.Title = Uniconta.ClientTools.Localization.lookup("MoveToProject");
                tbInvJournal.Visibility = lookupJournal.Visibility = Visibility.Collapsed;
                tbPrJournal.Visibility = lookupPrJournal.Visibility = Visibility.Visible;
            }
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { lookupJournal.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SetDialogResult(false);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    SetDialogResult(false);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPrjJournal)
            {
                var cache = API.CompanyEntity.GetCache(typeof(Uniconta.DataModel.InvJournal));
                InvJournal = (Uniconta.DataModel.InvJournal)cache?.Get(Journal);
            }
            else
            {
                var cache = API.CompanyEntity.GetCache(typeof(Uniconta.DataModel.PrJournal));
                PrJournal = (Uniconta.DataModel.PrJournal)cache?.Get(ProjJournal);
            }

            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void leWarehouse_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var selectedItem = leWarehouse.SelectedItem as InvWarehouseClient;
            if (selectedItem != null)
                setLocation(selectedItem);
        }

        object locationSource;
        async private void setLocation(InvWarehouseClient master)
        {
            if (API.CompanyEntity.Location)
            {
                if (master != null)
                    locationSource = master.Locations ?? await master.LoadLocations(API);
                else
                    locationSource = API.CompanyEntity.GetCache(typeof(Uniconta.DataModel.InvLocation));

                leLocation.ItemsSource = locationSource;
            }
        }

        private void leLocation_GotFocus(object sender, RoutedEventArgs e)
        {
            var selectedItem = leWarehouse.SelectedItem as InvWarehouseClient;
            if (selectedItem != null)
                setLocation(selectedItem);
        }
    }
}
