using Uniconta.DataModel;
using Uniconta.API.GeneralLedger;
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
using Uniconta.ClientTools.Controls;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using System.Windows;
using Uniconta.ClientTools.Page;
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.API.System;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CwEditTransaction.xaml
    /// </summary>
    public partial class CwEditTransaction : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.GLVat))]
        [Display(Name = "Vat", ResourceType = typeof(InputFieldDataText))]
        public string Vat { get; set; }
        
        [Display(Name = "Comment", ResourceType = typeof(InputFieldDataText))]
        public string Comment { get; set; }

        [Display(Name = "Account", ResourceType = typeof(InputFieldDataText))]
        public string Account { get; set; }

        public DCAccount DCAccount;
        SQLCache  DebtorCache, CreditorCache;
        Company Comp;
        bool isCreditor = false;
        public CwEditTransaction(CrudAPI api, bool hideComments= false , bool hideVat= false, bool IsCreditor = false)
        {
            InitializeComponent();
            this.DataContext = this;
            leVat.api = api;
            Comp = api.CompanyEntity;
            isCreditor= IsCreditor;
            this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("EditOBJ"), Uniconta.ClientTools.Localization.lookup("Transaction"));
            if (hideComments && hideVat)
                txtVat.Visibility = leVat.Visibility = txtBlockComments.Visibility = txtComments.Visibility = Visibility.Collapsed;
            else if (!hideComments && !hideVat)
                tbDCtype.Visibility = txtDcType.Visibility = txtAccount.Visibility = leAccount.Visibility = Visibility.Collapsed;
            else if (!hideComments && hideVat)
                txtVat.Visibility = leVat.Visibility= tbDCtype.Visibility = txtDcType.Visibility = txtAccount.Visibility = leAccount.Visibility = Visibility.Collapsed;
           
            tbDCtype.Text = isCreditor ? Uniconta.ClientTools.Localization.lookup("Creditor") : Uniconta.ClientTools.Localization.lookup("Debtor");

            BindLookUpEditor();
        }

        void BindLookUpEditor()
        {
            SQLCache cache;
            if (isCreditor)
            {
                if (CreditorCache == null)
                    CreditorCache = Comp.GetCache(typeof(Uniconta.DataModel.Creditor));
                cache = CreditorCache;
                txtAccount.Text = Uniconta.ClientTools.Localization.lookup("Creditor");
            }
            else
            {
                if (DebtorCache == null)
                    DebtorCache = Comp.GetCache(typeof(Debtor));
                cache = DebtorCache;
                txtAccount.Text=  Uniconta.ClientTools.Localization.lookup("Debtor");
            }

            if (cache != null)
                leAccount.ItemsSource = cache;

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
            if (leAccount.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrWhiteSpace(leAccount.Text))
                {
                    UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Account"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                    return;
                }
                DCAccount = !isCreditor ? (DCAccount)DebtorCache?.Get(Account) : (DCAccount)CreditorCache?.Get(Account);
            }
            if (txtVat.Visibility == Visibility.Visible && string.IsNullOrWhiteSpace(leVat.Text))
            {
                UnicontaMessageBox.Show(string.Format(Uniconta.ClientTools.Localization.lookup("MandatoryField"), (Uniconta.ClientTools.Localization.lookup("Vat"))), Uniconta.ClientTools.Localization.lookup("Warning"));
                return;
            }
            SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
    }
}
