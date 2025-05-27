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
using Uniconta.ClientTools.DataModel;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.API.GeneralLedger;
using Uniconta.Common;

namespace UnicontaClient.Controls.Dialogs
{
    /// <summary>
    /// Interaction logic for CWAttachedVouchers.xaml
    /// </summary>
    public partial class CWAttachedVouchers : ChildWindow
    {
        public bool IsDelete;
        public IdKey SelectRow;
        static int lastCopyMove;

        int SalesIndex, PurchaseIndex, ProjectIndex, ProdIndex;
        private CrudAPI Api;
        public CWAttachedVouchers(CrudAPI api, Document doc)
        {
            InitializeComponent();

            Title = string.Format(Uniconta.ClientTools.Localization.lookup("CopyOrMoveOBJ"), Uniconta.ClientTools.Localization.lookup("Attachments"));
            Api = api;
            cmbActions.ItemsSource = new string[] { Uniconta.ClientTools.Localization.lookup("Copy"), Uniconta.ClientTools.Localization.lookup("Move") };
            cmbActions.SelectedIndex = lastCopyMove - 1;

            var src = new List<string>(9)
            {
                Uniconta.ClientTools.Localization.lookup("Chartofaccount"),
                Uniconta.ClientTools.Localization.lookup("Debtor"),
                Uniconta.ClientTools.Localization.lookup("Creditor"),
                Uniconta.ClientTools.Localization.lookup("CompanyDocuments"),
                Uniconta.ClientTools.Localization.lookup("Item"),
                Uniconta.ClientTools.Localization.lookup("Employee"),
            };
            if (api.CompanyEntity.Order)
            {
                SalesIndex = src.Count;
                src.Add(Uniconta.ClientTools.Localization.lookup("SalesOrder"));
                src.Add(Uniconta.ClientTools.Localization.lookup("Offer"));
            }
            if (api.CompanyEntity.Purchase)
            {
                PurchaseIndex = src.Count;
                src.Add(Uniconta.ClientTools.Localization.lookup("PurchaseOrder"));
            }
            if (api.CompanyEntity.Project)
            {
                ProjectIndex = src.Count;
                src.Add(Uniconta.ClientTools.Localization.lookup("Project"));
            }
            if (api.CompanyEntity.Production)
            {
                ProdIndex = src.Count;
                src.Add(Uniconta.ClientTools.Localization.lookup("Production"));
            }

            cmbEntityType.ItemsSource = src;

            leEntitySource.api = api;
            txtEntityType.Text = string.Format(Uniconta.ClientTools.Localization.lookup("Account"), "/", Uniconta.ClientTools.Localization.lookup("Order"));
            if (doc?._CreditorAccount != null)
            {
                leEntitySource.Text = doc._CreditorAccount;
                cmbEntityType.SelectedIndex = 2;
            }
            else
            {
                cmbEntityType.SelectedIndex = -1;
            }
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var entityIndex = cmbEntityType.SelectedIndex;
            var actionIndex = cmbActions.SelectedIndex;
            lastCopyMove = actionIndex + 1;

            if (actionIndex == -1 || entityIndex == -1)
            {
                UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                return;
            }

            IsDelete = (actionIndex > 0);

            try
            {
                Type t = GetType(entityIndex);
                SelectRow = ClientHelper.GetRef(Api.CompanyId, t, leEntitySource.Text);
                if (SelectRow == null)
                {
                    UnicontaMessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoRecordSelected"), Uniconta.ClientTools.Localization.lookup("Error"), MessageBoxButton.OK);
                    return;
                }
                SetDialogResult(true);
            }
            catch (Exception ex)
            {
                UnicontaMessageBox.Show(ex);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        async private void cmbEntityType_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (cmbEntityType.SelectedItem == null)
                return;

            txtEntityType.Text = cmbEntityType.SelectedItem.ToString();
            Type t = GetType(cmbEntityType.SelectedIndex);
            if (t == null)
                return;
            busyIndicator.IsBusy = true;
            var cache = Api.GetCache(t) ?? await Api.LoadCache(t);
            leEntitySource.ItemsSource = cache;
            busyIndicator.IsBusy = false;
        }

        Type GetType(int index)
        {
            if (index == 0)
                return typeof(Uniconta.DataModel.GLAccount);
            if (index == 1)
                return typeof(Uniconta.DataModel.Debtor);
            if (index == 2)
                return typeof(Uniconta.DataModel.Creditor);
            if (index == 3)
                return typeof(Uniconta.DataModel.CompanyFolder);
            if (index == 4)
                return typeof(Uniconta.DataModel.InvItem);
            if (index == 5)
                return typeof(Uniconta.DataModel.Employee); 
            if (index == ProjectIndex)
                return typeof(Uniconta.DataModel.Project);
            if (index == SalesIndex)
                return typeof(Uniconta.DataModel.DebtorOrder);
            if (index == SalesIndex + 1)
                return typeof(Uniconta.DataModel.DebtorOffer);
            if (index == PurchaseIndex)
                return typeof(Uniconta.DataModel.CreditorOrder);
            if (index == ProdIndex)
                return typeof(Uniconta.DataModel.ProductionOrder);
            return null;
        }
    }
}
