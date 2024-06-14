using System.Collections.Generic;
using System.Threading.Tasks;
using Uniconta.API.Service;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InvTransMarkedPage : GridBasePage
    {
        public InvTransMarkedPage(BaseAPI api, InvTransClient line): base(api,null)
        {
            InitializeComponent();
            var lines = new List<InvTransClient>() { line };
            dgInvTransGrid.IsProject = false;
            dgInvTransGrid.SetSource(lines.ToArray());
            SetRibbonControl(localMenu, dgInvTransGrid);
            dgInvTransGrid.Readonly = true;
        }

        public override Task InitQuery()
        {
            return null;
        }

        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            var company = api.CompanyEntity;

            if (!company.Location || !company.Warehouse)
                Location.Visible = Location.ShowInColumnChooser = false;
            else
                Location.ShowInColumnChooser = true;
            if (!company.Warehouse)
                Warehouse.Visible = Warehouse.ShowInColumnChooser = false;
            else
                Warehouse.ShowInColumnChooser = true;
            UnicontaClient.Utilities.Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            UnicontaClient.Utilities.Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
            if (company.HideCostPrice)
            {
                Margin.Visible = Margin.ShowInColumnChooser = MarginRatio.Visible = MarginRatio.ShowInColumnChooser =
           CostPrice.Visible = CostPrice.ShowInColumnChooser = CostValue.Visible = CostValue.ShowInColumnChooser = false;
            }
        }
    }
}
