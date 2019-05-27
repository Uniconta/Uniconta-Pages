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
using Uniconta.API.Inventory;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWJoinTwoItems.xaml
    /// </summary>
    public partial class CWJoinTwoItems : ChildWindow
    {
        SQLCache itemCache;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItem))]
        public string FromItem { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(InvItem))]
        public string ToItem { get; set; }

        CrudAPI api;

        public CWJoinTwoItems(CrudAPI crudapi)
        {
            this.api = crudapi;
            InitializeComponent();
            cmbFromItem.api = cmbToItem.api = api;
            this.DataContext = this;
            LoadCacheInBackGround();

            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("JoinTwoOBJ"), Uniconta.ClientTools.Localization.lookup("InventoryItems"));
#if SILVERLIGHT
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }

        protected async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            itemCache = Comp.GetCache(typeof(Uniconta.DataModel.InvItem));
            if (itemCache == null)
                itemCache = await Comp.LoadCache(typeof(Uniconta.DataModel.InvItem), api);

            cmbFromItem.ItemsSource = cmbToItem.ItemsSource = itemCache;
        }
        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { cmbFromItem.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    this.DialogResult = false;
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var fromItem = cmbFromItem.SelectedItem as InvItem;
            var toItem = cmbToItem.SelectedItem as InvItem;

            if (fromItem == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("FromItem"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            if (toItem == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("ToItem"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            CWConfirmationBox confirmationDialog = new CWConfirmationBox(Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"), Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
            confirmationDialog.Closing += delegate
            {
                if (confirmationDialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                    CallJoinTwoItems(fromItem, toItem);
            };
            confirmationDialog.Show();
        }

        async void CallJoinTwoItems(InvItem fromItem, InvItem toItem)
        {
            InventoryAPI inventoryApi = new InventoryAPI(api);
            ErrorCodes res = await inventoryApi.JoinTwoItems(fromItem, toItem);
            if (res == ErrorCodes.Succes)
                this.DialogResult = true;
            UtilDisplay.ShowErrorCode(res);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
