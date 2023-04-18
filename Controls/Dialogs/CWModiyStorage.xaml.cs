using System;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWModiyStorage : ChildWindow
    {
        CrudAPI api;

        public InvLocation Location { get; set; }
        public InvWarehouse Warehouse { get; set; }
        public bool AllLines { get; set; }
        public CWModiyStorage(CrudAPI api)
        {
            DataContext = this;
            InitializeComponent();

            Title = string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), Uniconta.ClientTools.Localization.lookup("Storage"));
            this.api = api;
            this.Loaded += CWModiyStorage_Loaded;
        }


        private void CWModiyStorage_Loaded(object sender, RoutedEventArgs e)
        {
            SetItemSource();
            Dispatcher.BeginInvoke(new Action(() => { leWarehouselookUp.Focus(); }));
        }

        async private void SetItemSource()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(typeof(InvWarehouse)) ?? await Comp.LoadCache(typeof(InvWarehouse), api);
            leWarehouselookUp.ItemsSource = Cache;
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
                SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        async private void leWarehouselookUp_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            var wareHouse = leWarehouselookUp.SelectedItem as UnicontaBaseEntity;
            if (wareHouse != null)
                leLocationlookUp.ItemsSource = await api.Query<InvLocation>(new UnicontaBaseEntity[] { wareHouse }, null);
            else
                leLocationlookUp.ItemsSource = null;
        }
    }
}

