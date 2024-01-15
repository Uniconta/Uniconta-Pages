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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.Common.Utility;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWSetMaster.xaml
    /// </summary>
    public partial class CWSetMaster : ChildWindow
    {
        public UnicontaBaseEntity Master;
        List<Type> TableTypes;
        CrudAPI crudApi;
        public CWSetMaster(CrudAPI api, Type masterType, string masterName = null,bool isMstTbleTypeVisible = false)
        {
            InitializeComponent();
            this.DataContext = this;
            leMaster.api = api;
            crudApi = api;
            this.Title = Uniconta.ClientTools.Localization.lookup("Message");
            txtMaster.Text = masterName == null ? Uniconta.ClientTools.Localization.lookup("Master") : masterName;
            if (isMstTbleTypeVisible)
            {
                var xlist = new List<string>();
                TableTypes = new List<Type>() { typeof(CreditorPriceListClient), typeof(DebtorPriceListClient) };
                foreach (var type in TableTypes)
                {
                    var clientTableAttr = type.GetCustomAttributes(typeof(ClientTableAttribute), true);
                    if (clientTableAttr.Length == 0)
                        xlist.Add(type.Name);
                    else
                    {
                        var attr = (ClientTableAttribute)clientTableAttr[0];
                        xlist.Add(Util.ConcatParenthesis(type.Name, Uniconta.ClientTools.Localization.lookup(attr.LabelKey)));
                    }
                }
                cmbTableType.ItemsSource = xlist.OrderBy(x => x).ToList();
            }
            else
            {
                rowMasterType.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
                SetSource(api, masterType);
            }

            this.Loaded += CWSetMaster_Loaded;
        }

        private void CWSetMaster_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { leMaster.Focus(); }));
        }

        async private void SetSource(CrudAPI api, Type masterType)
        {
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(masterType);
            if (Cache == null)
                Cache = await Comp.LoadCache(masterType, api);
            leMaster.ItemsSource = Cache;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Master = leMaster.SelectedItem as UnicontaBaseEntity;
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CancelButton_Click(null, null);
            else if (e.Key == Key.Enter)
                OKButton_Click(null, null);
        }

        private void cmbTableType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbTableType.SelectedIndex == -1) return;
            var selectedItem = cmbTableType.SelectedItem.ToString();
            var selectedType = string.Empty;
            var endIndex = selectedItem.IndexOf('(') - 1;
            if (endIndex > 0)
                selectedType = selectedItem.Substring(0, endIndex).Replace(" ", "");
            else
                selectedType = selectedItem;
            Type sltype = (from l in TableTypes where l.Name.Split('.').Last() == selectedType select l).SingleOrDefault();
            if (sltype == null) return;
            SetSource(crudApi, sltype);
        }
    }
}
