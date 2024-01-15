using System.Collections.Generic;
using System.Windows;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;
using Uniconta.DataModel;
using System.Windows.Controls;
using Corasau.Client.Utilities;
using System;

namespace Uniconta.WPFClient.Controls.Dialogs
{
    public partial class CWInventoryTransactions : ChildWindow
    {
        string item;
        public InvTransClient invTrans;
        CrudAPI api;
        int movementType;
        public CWInventoryTransactions(CrudAPI _api, string _item, int _movementType)
        {
            this.DataContext = this;
            item = _item;
            api = _api;
            movementType = _movementType;
            InitializeComponent();
#if !WPF
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Title = Uniconta.ClientTools.Localization.lookup("InvTransactions");
            dgInvTransGrid.api = api;
            dgInvTransGrid.BusyIndicator = busyIndicator;
            dgInvTransGrid.Readonly = true;
            this.Loaded += CWInventoryTransactions_Loaded;
            BindGrid();
        }

        private void CWInventoryTransactions_Loaded(object sender, RoutedEventArgs e)
        {
            var company = api.CompanyEntity;
            if (company._Variant1 != null && company.ItemVariants)
                colVariant1.Header = company._Variant1;
            else
                colVariant1.Visible = colVariant1.ShowInColumnChooser = false;

            if (company._Variant2 != null && company.ItemVariants)
                colVariant2.Header = company._Variant2;
            else
                colVariant2.Visible = colVariant2.ShowInColumnChooser = false;

            Corasau.Client.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        async private void BindGrid()
        {
            busyIndicator.IsBusy = true;
            var pair = new PropValuePair[]
            { 
                PropValuePair.GenereteWhereElements("Item", item, CompareOperator.Equal),
                PropValuePair.GenereteWhereElements("Qty", 0d, CompareOperator.GreaterThan),
                PropValuePair.GenereteWhereElements("Date", DateTime.Now.Date.AddYears(-1), CompareOperator.GreaterThanOrEqual),
            };
            await dgInvTransGrid.Filter(pair);
            busyIndicator.IsBusy = false;
            dgInvTransGrid.Visibility = Visibility.Visible;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgInvTransGrid.SelectedItem as InvTransClient;
           
            if (selectedItem != null)
            {
                invTrans = selectedItem;
                this.DialogResult = true;
            }
            else
                MessageBox.Show(Uniconta.ClientTools.Localization.lookup("NoLinesFound"), Uniconta.ClientTools.Localization.lookup("Warning"), MessageBoxButton.OK);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
