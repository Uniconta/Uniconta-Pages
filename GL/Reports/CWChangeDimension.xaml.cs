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
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWChangeDimension : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(GLAccount))]
        public string Account { get; set; }
        public GLAccount GLAccount;
        public int? Dimension1 { get; set; }
        public int? Dimension2 { get; set; }
        public int? Dimension3 { get; set; }
        public int? Dimension4 { get; set; }
        public int? Dimension5 { get; set; }
        CrudAPI api;
        public bool AllLine { get; set; }
        public string Text { get; set; }
        public double Quantity { get; set; }
        public CWChangeDimension(CrudAPI api, bool isChangeDimension = true, bool isChangeText= true)
        {
            this.DataContext = this;
            this.api = api;
            InitializeComponent();
#if !SILVERLIGHT
            if (string.IsNullOrWhiteSpace(leAccount.Text))
                FocusManager.SetFocusedElement(leAccount, leAccount);
#endif
            leAccount.api = api;
            if (isChangeDimension)
            {
                setDim();
                int noofDimensions = api.CompanyEntity.NumberOfDimensions;
                if (noofDimensions >= 1)
                    TransactionReport.SetDimValues(typeof(GLDimType1), lookupDim1, api, true);
                if (noofDimensions >= 2)
                    TransactionReport.SetDimValues(typeof(GLDimType2), lookupDim2, api, true);
                if (noofDimensions >= 3)
                    TransactionReport.SetDimValues(typeof(GLDimType3), lookupDim3, api, true);
                if (noofDimensions >= 4)
                    TransactionReport.SetDimValues(typeof(GLDimType4), lookupDim4, api, true);
                if (noofDimensions >= 5)
                    TransactionReport.SetDimValues(typeof(GLDimType5), lookupDim5, api, true);
                //lookupDim1.SelectedIndex = lookupDim2.SelectedIndex = lookupDim3.SelectedIndex = lookupDim4.SelectedIndex = lookupDim5.SelectedIndex = 0;

                //Text Row
                HideRow(6);
                //Qty Row
                HideRow(7);

                double h = this.Height - 60;
                this.Height = h;
                this.Title = Uniconta.ClientTools.Localization.lookup("ChangeDimension");
            }
            else if(isChangeText)
            {
                //Account Row
                HideRow(0);
                //Dimension1 Row
                HideRow(1);
                //Dimension3 Row
                HideRow(2);
                //Dimension3 Row
                HideRow(3);
                //Dimension4 Row
                HideRow(4);
                //Dimension5 Row
                HideRow(5);
                //Qty Row
                HideRow(7);

                double h = this.Height - 210;
                this.Height = h;
                this.Title = Uniconta.ClientTools.Localization.lookup("ChangeText");
            }
            else
            {
                //Account Row
                HideRow(0);
                //Dimension1 Row
                HideRow(1);
                //Dimension3 Row
                HideRow(2);
                //Dimension3 Row
                HideRow(3);
                //Dimension4 Row
                HideRow(4);
                //Dimension5 Row
                HideRow(5);
                //Text Row
                HideRow(6);

                double h = this.Height - 210;
                this.Height = h;
                this.Title = string.Format(Uniconta.ClientTools.Localization.lookup("ChangeOBJ"), Uniconta.ClientTools.Localization.lookup("Qty"));
            }
            this.Loaded += CW_Loaded;
        }

        private void HideRow(int rowIndex)
        {
            switch(rowIndex)
            {
                case 0:
                    rowAccount.Height = new GridLength(0);
#if !SILVERLIGHT
                    lblAccount.Visibility = Visibility.Hidden;
                    leAccount.Visibility = Visibility.Hidden;
#endif
                    break;
                case 1:
                    rowDim1.Height = new GridLength(0);
#if !SILVERLIGHT
                    lblDim1.Visibility = Visibility.Hidden;
                    lookupDim1.Visibility = Visibility.Hidden;
#endif
                    break;
                case 2:
                    rowDim2.Height = new GridLength(0);
#if !SILVERLIGHT
                    lblDim2.Visibility = Visibility.Hidden;
                    lookupDim2.Visibility = Visibility.Hidden;
#endif
                    break;
                case 3:
                    rowDim3.Height = new GridLength(0);
#if !SILVERLIGHT
                    lblDim3.Visibility = Visibility.Hidden;
                    lookupDim3.Visibility = Visibility.Hidden;
#endif
                    break;
                case 4:
                    rowDim4.Height = new GridLength(0);
#if !SILVERLIGHT
                    lblDim4.Visibility = Visibility.Hidden;
                    lookupDim4.Visibility = Visibility.Hidden;
#endif
                    break;
                case 5:
                    rowDim5.Height = new GridLength(0);
#if !SILVERLIGHT
                    lblDim5.Visibility = Visibility.Hidden;
                    lookupDim5.Visibility = Visibility.Hidden;
#endif
                    break;
                case 6:
                    rowText.Height = new GridLength(0);
#if !SILVERLIGHT
                    lblText.Visibility = Visibility.Collapsed;
                    txtText.Visibility = Visibility.Collapsed;
#endif
                    break;
                case 7:
                    rowQty.Height = new GridLength(0);
#if !SILVERLIGHT
                    lblQty.Visibility = Visibility.Hidden;
                    deQuantity.Visibility = Visibility.Hidden;
#endif
                    break;
            }
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => { leAccount.Focus(); }));
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
                SetDialogResult(true);
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }
        void setDim()
        {
            var c = api.CompanyEntity;
            int noofDimensions = c.NumberOfDimensions;
            lblDim1.Text = (string)c._Dim1;
            lblDim2.Text = (string)c._Dim2;
            lblDim3.Text = (string)c._Dim3;
            lblDim4.Text = (string)c._Dim4;
            lblDim5.Text = (string)c._Dim5;
            int removeheight = 0;
            if (noofDimensions < 5)
            {
                lblDim5.Visibility = lookupDim5.Visibility = Visibility.Collapsed;
                Dimension5 = null;
                rowDim5.Height = new GridLength(0);
                removeheight += 30;
            }
            if (noofDimensions < 4)
            {
                lblDim4.Visibility = lookupDim4.Visibility = Visibility.Collapsed;
                Dimension4 = null;
                rowDim4.Height = new GridLength(0);
                removeheight += 30;
            }
            if (noofDimensions < 3)
            {
                lblDim3.Visibility = lookupDim3.Visibility = Visibility.Collapsed;
                Dimension3 = null;
                rowDim3.Height = new GridLength(0);
                removeheight += 30;
            }
            if (noofDimensions < 2)
            {
                lblDim2.Visibility = lookupDim2.Visibility = Visibility.Collapsed;
                Dimension2 = null;
                rowDim2.Height = new GridLength(0);
                removeheight += 30;
            }
            if (noofDimensions < 1)
            {
                lblDim1.Visibility = lookupDim1.Visibility = Visibility.Collapsed;
                Dimension1 = null;
                rowDim1.Height = new GridLength(0);
                removeheight += 30;
            }
            this.Height = this.Height - removeheight;
        }

        private void leAccount_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            if (leAccount.SelectedItem == null || leAccount.SelectedIndex ==-1) return;
            GLAccount = leAccount.SelectedItem as GLAccountClient;
        }
    }
}

