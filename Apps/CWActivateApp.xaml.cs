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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Pages;

namespace UnicontaClient.Controls.Dialogs
{
    public partial class CWActivateApp : ChildWindow
    {
        CrudAPI api;

        public CWActivateApp(CrudAPI api)
        {
            DataContext = this;
            InitializeComponent();

            Title = Uniconta.ClientTools.Localization.lookup("Activate");
            this.api = api;
            this.Loaded += CWActivateApp_Loaded;
        }


        private void CWActivateApp_Loaded(object sender, RoutedEventArgs e)
        {
            SetItemSource();
            Dispatcher.BeginInvoke(new Action(() => { cmbApps.Focus(); }));
        }

        async private void SetItemSource()
        {
            var api = this.api;
            var apps = await api.Query<AppClient>();
            cmbApps.ItemsSource = apps;
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var app = cmbApps.SelectedItem as AppClient;
            if (app != null)
            {
                AppsPage.SetAppAsActive(app);
                SetDialogResult(true);
            }
            SetDialogResult(false);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

    }
}

