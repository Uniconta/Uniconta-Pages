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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWAppPlugins : ChildWindow
    {
        public AppDLL[] appDllArray;
        public AppDLL selectedAppDll;
        UnicontaBaseEntity _master;
        CrudAPI crudapi;

        public CWAppPlugins(CrudAPI api, UnicontaBaseEntity master)
        {
            crudapi = api;
            _master = master;
            GetData();
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("Plugin");
        }

        private async void GetData()
        {
            var appPlugins = await crudapi.Query<AppDLL>(new UnicontaBaseEntity[] { Utilities.Utility.GetCurrentApp() }, null);
            if (appPlugins == null)
                return;
            appPlugins = appPlugins.OrderBy(x => x._Name).ToArray(); 
            bool disableFound = false;
            var names = new List<string>();
            foreach (var l in appPlugins)
            {
                if (l._Disable)
                    disableFound = true;
                else
                    names.Add(l._Name);
            }
            cmbPlugins.ItemsSource = names;

            if (disableFound)
                appPlugins = appPlugins.Where(x => x._Disable == false).ToArray();
            this.appDllArray = appPlugins;

            selectedAppDll = null;
            var AppDLLRowId = ((Uniconta.DataModel.AppPlugin)_master).AppDLLRowId;
            for (int i = 0; i < appPlugins.Length; i++)
            {
                var p = appPlugins[i];
                if (p.RowId == AppDLLRowId)
                {
                    selectedAppDll = p;
                    cmbPlugins.SelectedIndex = i;
                    break;
                }
            } 
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false); 
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(true);
        }

        private void cmbPlugins_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbPlugins.SelectedIndex != -1)
            {
                selectedAppDll = appDllArray[cmbPlugins.SelectedIndex];
            }
        }
    }
}
