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
using System.ComponentModel.DataAnnotations;
using UnicontaClient.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CWProductionOrderLine : ChildWindow
    {
        public int get_storage() { return storage; }
        private int storage;

        [InputFieldData]
        [Display(Name = "Storage", ResourceType = typeof(InputFieldDataText))]
        public int Storage { get { return storage; } set { storage = value; } }

        public double quantity { get; set; }
        private bool isQtyVisible = true;
        public int Decimals { get; set; }
        public bool Force { get; set; }

#if !SILVERLIGHT
        protected override int DialogId { get { return DialogTableId; } }
        public int DialogTableId { get; set; }
        protected override bool ShowTableValueButton { get { return true; } }
#endif
        public CWProductionOrderLine(CrudAPI api, bool hideCheckForce): this(api)
        {
            if (hideCheckForce)
            {
                txtCheckForce.Visibility = Visibility.Collapsed;
                chkEditor.Visibility = Visibility.Collapsed;
                rowCheckForce.Height = new GridLength(0.0d);
            }
        }

        public CWProductionOrderLine(CrudAPI api, bool isQuantityVisible = false, string title = null, int Decimal = 0)
        {
            Decimals = Decimal;
            this.DataContext = this;
            InitializeComponent();
            this.Title = string.IsNullOrEmpty(title) ? Uniconta.ClientTools.Localization.lookup("ProductionLines") : title;
#if SILVERLIGHT
            UnicontaClient.Utilities.Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            if (!isQuantityVisible)
            {
                isQtyVisible = false;
                rowQuantity.Height = new GridLength(0.0d);
            }
           
            txtCheckForce.Text = string.Format(Uniconta.ClientTools.Localization.lookup("RecreateOBJ"), Uniconta.ClientTools.Localization.lookup("ProductionLines"));
            var prodReg = AppEnums.ProductionRegister.ToString((int)api.CompanyEntity._OrderLineStorage);
            storageType.ItemsSource = AppEnums.ProductionRegister.Values;
            storageType.SelectedItem = prodReg;
            this.Loaded += CW_Loaded;
        }

        void CW_Loaded(object sender, RoutedEventArgs e)
        {
            storageType.SelectedIndex = storage;
            Dispatcher.BeginInvoke(new Action(() => { storageType.Focus(); }));
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
                if (OKButton.IsFocused)
                    OKButton_Click(null, null);
                else if (CancelButton.IsFocused)
                    this.DialogResult = false;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            quantity = isQtyVisible ? Convert.ToDouble(qtyEditor.Text) : 0.0d;
            storage = AppEnums.ProductionRegister.IndexOf(Convert.ToString(storageType.EditValue));
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
