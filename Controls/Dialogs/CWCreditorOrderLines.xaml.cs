using Corasau.Client.Utilities;
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
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.Common;

namespace Uniconta.WPFClient.Controls.Dialogs
{
    public partial class CWCreditorOrderLines : ChildWindow
    {
        string item;
        public CreditorOrderLineClient creditorOrderLine;
        CrudAPI api;
        public CWCreditorOrderLines(CrudAPI _api, string _item)
        {
            this.DataContext = this;
            item = _item;
            api = _api;
            InitializeComponent();
#if !WPF
            Utility.SetThemeBehaviorOnChildWindow(this);
#endif
            this.Title = Uniconta.ClientTools.Localization.lookup("PurchaseLines");
            dgCreditorOrderLineGrid.api = api;
            dgCreditorOrderLineGrid.BusyIndicator = busyIndicator;
            dgCreditorOrderLineGrid.Readonly = true;
            this.Loaded += CWCreditorOrderLines_Loaded;
            BindGrid();
        }

        private void CWCreditorOrderLines_Loaded(object sender, RoutedEventArgs e)
        {
            var company = api.CompanyEntity;

            if (company._Variant1 != null && company.ItemVariants)
            {
                colVariant1.Header = company._Variant1;
            }
            else
            {
                colVariant1.Visible = colVariant1.ShowInColumnChooser = false;
            }

            if (company._Variant2 != null && company.ItemVariants)
            {
                colVariant2.Header = company._Variant2;
            }
            else
            {
                colVariant2.Visible = colVariant2.ShowInColumnChooser = false;
            }

            Corasau.Client.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        async private void BindGrid()
        {
            busyIndicator.IsBusy = true;
            var pair = new PropValuePair[]
            {
                PropValuePair.GenereteWhereElements("Item", item, CompareOperator.Equal)
            };
            await dgCreditorOrderLineGrid.Filter(pair);
            busyIndicator.IsBusy = false;
            dgCreditorOrderLineGrid.Visibility = Visibility.Visible;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = dgCreditorOrderLineGrid.SelectedItem as CreditorOrderLineClient;
            if (selectedItem != null)
            {
                creditorOrderLine = selectedItem;
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
