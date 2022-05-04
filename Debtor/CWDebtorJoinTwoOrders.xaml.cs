using UnicontaClient.Pages;
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
using Uniconta.API.DebtorCreditor;
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
    /// Interaction logic for CWDebtorJoinTwoOrders.xaml
    /// </summary>
    public partial class CWDebtorJoinTwoOrders :  ChildWindow
    {
        SQLCache orderCache;
        [ForeignKeyAttribute(ForeignKeyTable = typeof(DebtorOrder))]
        public string FromOrder { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(DebtorOrder))]
        public string ToOrder { get; set; }

        CrudAPI api;

        public CWDebtorJoinTwoOrders(CrudAPI crudapi)
        {
            InitializeComponent();
            this.api = crudapi;
            InitializeComponent();
            cmbFromOrder.api = cmbToOrder.api = api;
            this.DataContext = this;
            LoadCacheInBackGround();

            this.Title = Uniconta.ClientTools.Localization.lookup("JoinTwoOrders");
#if SILVERLIGHT
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => 
            {
                if (string.IsNullOrWhiteSpace(cmbFromOrder.Text))
                    cmbFromOrder.Focus();
                else
                    OKButton.Focus();
            }));
        }

        protected async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            orderCache = Comp.GetCache(typeof(Uniconta.DataModel.DebtorOrder));
            if (orderCache == null)
                orderCache = await Comp.LoadCache(typeof(Uniconta.DataModel.DebtorOrder), api);

            cmbFromOrder.ItemsSource = cmbToOrder.ItemsSource = orderCache;
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
                if (CancelButton.IsFocused)
                {
                    SetDialogResult(false);
                    return;
                }
                OKButton_Click(null, null);
            }
        }

        CWConfirmationBox confirmationDialog;
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var fromOrder = cmbFromOrder.SelectedItem as DCOrder;
            var toOrder = cmbToOrder.SelectedItem as DCOrder;

            if (fromOrder == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("fromOrder"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            if (toOrder == null)
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("CopyTo"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }

            if (fromOrder._DCAccount != toOrder._DCAccount)
            {
                string msg = string.Format(Uniconta.ClientTools.Localization.lookup("DifferentAccountMessage"), fromOrder._DCAccount, toOrder._DCAccount);
                string.Format("{0}\n{1}", msg, Uniconta.ClientTools.Localization.lookup("AreYouSureToContinue"));
                confirmationDialog = new CWConfirmationBox(msg, Uniconta.ClientTools.Localization.lookup("Confirmation"), false);
                confirmationDialog.Closing += delegate
                {
                    if (confirmationDialog.ConfirmationResult == CWConfirmationBox.ConfirmationResultEnum.Yes)
                    {
                        CallJoinTwoOrder(fromOrder, toOrder);
                    }
                };
                confirmationDialog.Show();
            }
            else
                CallJoinTwoOrder(fromOrder, toOrder);
        }

        void CallJoinTwoOrder(DCOrder fromOrder, DCOrder toOrder)
        {
            DeletePostedJournal delDialog = new DeletePostedJournal(true);
            delDialog.Closed += async delegate
            {
                if (delDialog.DialogResult == true)
                {
                    OrderAPI orderApi = new OrderAPI(api);
                    ErrorCodes res = await orderApi.JoinTwoOrders(fromOrder, toOrder);
                    if (res == ErrorCodes.Succes)
                        SetDialogResult(true);
                    UtilDisplay.ShowErrorCode(res);
                }
            };
            delDialog.Show();    
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
