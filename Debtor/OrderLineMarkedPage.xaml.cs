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
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class OrderLineMarkedPage : GridBasePage
    {
        public OrderLineMarkedPage(BaseAPI api, DebtorOrderLineClient line)
            : base(api, null)
        {
            InitializeComponent();
            var lines = new DebtorOrderLineClient[] { line };
            dgCreditorOrderLineGrid.SetSource(lines);
            dgCreditorOrderLineGrid.api = this.api;
            SetRibbonControl(localMenu, dgCreditorOrderLineGrid);
            dgCreditorOrderLineGrid.BusyIndicator = busyIndicator;
            dgCreditorOrderLineGrid.Readonly = true;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;
           
            if (!company.Project)
            {
                PrCategory.Visible = PrCategory.ShowInColumnChooser = false;
                Project.Visible = Project.ShowInColumnChooser = false;
            }
            if (!company.Storage)
                Storage.Visible = Storage.ShowInColumnChooser = false;
            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            if (!company.SerialBatchNumbers)
                SerieBatch.Visible = SerieBatch.ShowInColumnChooser = false;

            UnicontaClient.Utilities.Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }
    }
}
