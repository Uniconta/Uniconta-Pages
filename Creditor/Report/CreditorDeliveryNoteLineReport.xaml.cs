using System;
using System.Collections;
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
using UnicontaClient.Models;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CreditortorDeliveryNoteLineReportGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(CreditorDeliveryNoteLineClient); } }
    }

    public partial class CreditorDeliveryNoteLineReport : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.CreditorDeliveryNoteLineReport; } }

        public CreditorDeliveryNoteLineReport(BaseAPI API) : base(API, string.Empty)
        {
            InitializeComponent();
            InitPage();
        }

        private void InitPage()
        {
            SetRibbonControl(localMenu, dgCreditorDelNoteLine);
            dgCreditorDelNoteLine.api = api;
            dgCreditorDelNoteLine.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            LoadNow(typeof(Uniconta.DataModel.InvItem));
        }

        public override bool CheckIfBindWithUserfield(out bool isReadOnly, out bool useBinding)
        {
            isReadOnly = true;
            useBinding = true;
            return true;
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
            Utility.SetupVariants(api, null, colVariant1, colVariant2, colVariant3, colVariant4, colVariant5, Variant1Name, Variant2Name, Variant3Name, Variant4Name, Variant5Name);
            Utility.SetDimensionsGrid(api, cldim1, cldim2, cldim3, cldim4, cldim5);
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }
    }
}
