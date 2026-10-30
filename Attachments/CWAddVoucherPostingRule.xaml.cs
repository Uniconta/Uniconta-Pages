using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Util;
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
using Uniconta.ClientTools;
using System.Windows;
using UnicontaClient.Utilities;
using Uniconta.DataModel;
using System.ComponentModel.DataAnnotations;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWAddVoucherPostingRule : ChildWindow
    {
        private CrudAPI api;

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string Account { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string OffsetAccount { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLVat))]
        public string Vat { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Creditor))]
        public string Creditor { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string Approver1 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string Approver2 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType1))]
        public string Dimension1 { get; set;  }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType2))]
        public string Dimension2 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType3))]
        public string Dimension3 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType4))]
        public string Dimension4 { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLDimType5))]
        public string Dimension5 { get; set; }
        VouchersClient voucher;
        public CWAddVoucherPostingRule(CrudAPI api, VouchersClient voucher)
        {
            this.api = api;
            this.DataContext = this;
            this.voucher = voucher;
            InitializeComponent();
            this.Title = Uniconta.ClientTools.Localization.lookup("AutomaticAccountSelection");
            this.Loaded += CW_Loaded;
            chkEqual.IsChecked = chkContains.IsChecked = ckkStartWith.IsChecked = true;
            txtText.Text = voucher._Text;
            Creditor = leCreditor.Text = voucher._CreditorAccount;
            Approver1 = leApprover1.Text = voucher._Approver1;
            Approver2 = leApprover2.Text = voucher._Approver2;
            Account = leAccount.Text = voucher._CostAccount;
            OffsetAccount = leOffsetAccount.Text = voucher._PayAccount;
            leCreditor.api = leApprover1.api = leApprover2.api = leVat.api = leAccount.api = leOffsetAccount.api = api;
            BindDimension();
        }

        void BindDimension()
        {
            var c = api.CompanyEntity;
            if (c == null)
                return;
            var noofDimensions = c.NumberOfDimensions;

            if (noofDimensions < 5)
            {
                txtDim5.Visibility = leDim5.Visibility = Visibility.Collapsed;
                rowDim5.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
            else
            {
                leDim5.api= api;
                txtDim5.Text= (string)c._Dim5;
            }

            if (noofDimensions < 4)
            {
                txtDim4.Visibility = leDim4.Visibility = Visibility.Collapsed;
                rowDim4.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
            else
            {
                leDim4.api= api;
                txtDim4.Text= (string)c._Dim4;
            }

            if (noofDimensions < 3)
            {
                txtDim3.Visibility = leDim3.Visibility = Visibility.Collapsed;
                rowDim3.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
            else
            {
                leDim3.api= api;
                txtDim3.Text= (string)c._Dim3;
            }

            if (noofDimensions < 2)
            {
                txtDim2.Visibility = leDim2.Visibility = Visibility.Collapsed;
                rowDim2.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
            else
            {
                leDim2.api= api;
                txtDim2.Text= (string)c._Dim2;
            }

            if (noofDimensions < 1)
            {
                txtDim1.Visibility = leDim1.Visibility = Visibility.Collapsed;
                rowDim1.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;
            }
            else
            {
                leDim1.api= api;
                txtDim1.Text= (string)c._Dim1;
            }
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            txtText.KeyDown += txtText_KeyDown;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtText.Text))
                return;

            var bankImportMap = new BankImportMap
            {
                _Text = txtText.Text.Trim(),
                _Equal = chkEqual.IsChecked.GetValueOrDefault(),
                _StartsWith = ckkStartWith.IsChecked.GetValueOrDefault(),
                _Contains = chkContains.IsChecked.GetValueOrDefault(),
                _Dim1 = leDim1.Text,
                _Dim2 = leDim2.Text,
                _Dim3 = leDim3.Text,
                _Dim4 = leDim4.Text,
                _Dim5 = leDim5.Text,
                _Creditor = Creditor,
                _Approver1 = Approver1,
                _Approver2 = Approver2,
                _Vat = Vat,
                _Account = leAccount.Text,
                _OffsetAccount = leOffsetAccount.Text
            };
            bankImportMap.SetMaster(voucher);
            var err = await api.Insert(bankImportMap);
            if (err != ErrorCodes.Succes)
                UtilDisplay.ShowErrorCode(err);
            else
                SetDialogResult(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void txtText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SaveButton_Click(sender, e);
            else if (e.Key == Key.Escape)
                CancelButton_Click(sender, e);
        }
    }
}
