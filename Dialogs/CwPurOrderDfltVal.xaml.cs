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
using Uniconta.ClientTools.Page;

namespace UnicontaClient.Pages  
{
    public partial class CwPurOrderDfltVal : ChildWindow
    {
        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.Employee))]
        public string Employee { get; set; }

        [ForeignKeyAttribute(ForeignKeyTable = typeof(Uniconta.DataModel.ShipmentType))]
        public string Shipment { get; set; }

        public string Group { get; set; }

        static DateTime delDate;
        CrudAPI API;
        bool IsProdOrder;
        public CreditorOrderClient DefaultCreditorOrder;
        public ProductionOrderClient DefaultProductionOrder;
        public int Storage;
        public bool CreatePurchaseLines, OrderLinePerWareHouse, OrderLinePerLocation;
        public bool WarehouseCheck, LocationCheck;
        public CwPurOrderDfltVal(CrudAPI api, bool isProdOrder = false)
        {
            this.DataContext = this;
            IsProdOrder = isProdOrder;
            InitializeComponent();
            this.Title = isProdOrder == false ? Uniconta.ClientTools.Localization.lookup("PurchaseOrder") : Uniconta.ClientTools.Localization.lookup("ProductionOrder");
            API = api;
            leGroup.api = leShipment.api = leEmployee.api = API;

            deDeliveryDate.DateTime = BasePage.GetSystemDefaultDate();

            if (delDate == DateTime.MinValue)
                delDate = BasePage.GetSystemDefaultDate();
            deDeliveryDate.DateTime = delDate;

            if (!isProdOrder)
                leGroup.SetForeignKeyRef(typeof(Uniconta.DataModel.CreditorOrderGroup),0);
            else
                leGroup.SetForeignKeyRef(typeof(Uniconta.DataModel.ProductionOrderGroup), 0);


            if (isProdOrder)
            {
                txbCreateProdLine.Text = string.Format(Uniconta.ClientTools.Localization.lookup("CreateOBJ"), Uniconta.ClientTools.Localization.lookup("ProductionLines"));

                var prodReg = AppEnums.ProductionRegister.ToString((int)api.CompanyEntity._OrderLineStorage);
                storageType.ItemsSource = AppEnums.ProductionRegister.Values;
                storageType.SelectedItem = prodReg;
              
                rowChkOrderLnePrWh.Height = new GridLength(0);
                rowChkOrderLnePerLoc.Height = new GridLength(0);
                double h = this.Height - 60;
                this.Height = h;
            }
            else
            {
                rowChkProdLines.Height = new GridLength(0);
                double h = this.Height - 30;
                this.Height = h;

                if(!api.CompanyEntity.Warehouse)
                {
                    rowChkOrderLnePrWh.Height = new GridLength(0);
                    rowChkOrderLnePerLoc.Height = new GridLength(0);
                    double h1 = this.Height - 60;
                    this.Height = h1;
                }
                else
                {
                    txtOrderLinePrWH.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Orderline"),
                        string.Format(Uniconta.ClientTools.Localization.lookup("PerOBJ"), Uniconta.ClientTools.Localization.lookup("Warehouse")));

                    txtOrderLinePrLoc.Text = string.Format("{0} {1}", Uniconta.ClientTools.Localization.lookup("Orderline"),
                     string.Format(Uniconta.ClientTools.Localization.lookup("PerOBJ"), Uniconta.ClientTools.Localization.lookup("Location")));
                }
            }

            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            chkOrderLinePrLoc.IsChecked = LocationCheck;
            chkOrderLinePrWH.IsChecked = WarehouseCheck;
            Dispatcher.BeginInvoke(new Action(() => { txtOurRef.Focus(); }));
        }

        private void ChildWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetDialogResult(false);
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsProdOrder)
            {
                DefaultCreditorOrder = new CreditorOrderClient();
                DefaultCreditorOrder._OurRef = txtOurRef.Text; 
                DefaultCreditorOrder._Remark = txtRemark.Text;
                DefaultCreditorOrder._Group = Group;
                DefaultCreditorOrder._DeliveryDate = deDeliveryDate.DateTime;
                DefaultCreditorOrder._Shipment = Shipment;
                DefaultCreditorOrder._Employee = Employee;
                OrderLinePerWareHouse = chkOrderLinePrWH.IsChecked.GetValueOrDefault();
                OrderLinePerLocation = chkOrderLinePrLoc.IsChecked.GetValueOrDefault();
            }
            else
            {
                DefaultProductionOrder = new ProductionOrderClient();
                DefaultProductionOrder._OurRef = txtOurRef.Text; 
                DefaultProductionOrder._Remark = txtRemark.Text;
                DefaultProductionOrder._Group = Group;
                DefaultProductionOrder._DeliveryDate = deDeliveryDate.DateTime;
                DefaultProductionOrder._Shipment = Shipment;
                DefaultProductionOrder._Employee = Employee;
                Storage = AppEnums.ProductionRegister.IndexOf(Convert.ToString(storageType.EditValue));
                CreatePurchaseLines = (bool)chkCreateProdLines.IsChecked;
            }
            SetDialogResult(true);
        }

        private void chkCreateProdLines_Checked(object sender, RoutedEventArgs e)
        {
            txbStorage.Visibility = Visibility.Visible;
            storageType.Visibility = Visibility.Visible;
        }

        private void chkCreateProdLines_Unchecked(object sender, RoutedEventArgs e)
        {
            txbStorage.Visibility = Visibility.Collapsed;
            storageType.Visibility = Visibility.Collapsed;
        }
    }
}
