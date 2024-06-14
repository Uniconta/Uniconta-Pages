using DevExpress.Xpf.Grid;
using DevExpress.XtraPrinting.Native;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CWOffers.xaml
    /// </summary>
    public partial class CopyOfferLines : GridBasePage
    {
        CrudAPI crudApi;
        public DebtorOfferLineClient[] OfferLines { get; set; }
        public bool IsDeleteLines { get; set; }
        public override string NameOfControl => TabControls.CopyOfferLines;
        static double pageHeight = 650.0d, pageWidth = 850.0d;
        static Point position = new Point();
        public override void PageClosing()
        {
            if (OfferLines != null && createbuttonclicked)
            {
                var args = new object[2];
                args[0] = IsDeleteLines;
                args[1] = OfferLines;
                globalEvents.OnRefresh(NameOfControl, args);
            }
            position = AttachVoucherGridPage.GetPosition(dockCtrl);
        }
        public CopyOfferLines(CrudAPI api) : base(api, string.Empty)
        {
            DataContext = this;
            InitializeComponent();
            SetHeader(string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("OfferLine")));
            crudApi = api;
            InitPage(null);
        }
        UnicontaBaseEntity order;
        public CopyOfferLines(CrudAPI api, UnicontaBaseEntity order) : base(api, string.Empty)
        {
            DataContext = this;
            InitializeComponent();
            SetHeader(string.Format(Uniconta.ClientTools.Localization.lookup("CopyOBJ"), Uniconta.ClientTools.Localization.lookup("OfferLine")));
            crudApi = api;
            InitPage(order);
        }
        bool fromReservation;
        private void InitPage(UnicontaBaseEntity order)
        {
            this.order = order;
            this.DataContext = this;
            InitializeComponent();
            if (order is ProjectReservation)
            {
                fromReservation = true;
                chkDelete.Visibility = Visibility.Collapsed;
            }
            ((TableView)dgOffersGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            ((TableView)dgOfferLinesGrid.View).RowStyle = Application.Current.Resources["StyleRow"] as Style;
            var comp = crudApi.CompanyEntity;
            dgOffersGrid.BusyIndicator = busyIndicator;
            dgOffersGrid.api = crudApi;
            dgOfferLinesGrid.BusyIndicator = busyIndicator;
            dgOfferLinesGrid.api = crudApi;
            SetRibbonControl(localMenu, dgOffersGrid);
            dgOfferLinesGrid.Readonly = dgOffersGrid.Readonly = true;
            dgOffersGrid.SelectedItemChanged += DgOffersGrid_SelectedItemChanged;
            if (!comp.ItemVariants)
                colVariant.Visible = false;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;

        }
        protected override Filter[] DefaultFilters()
        {
            if (fromReservation)
            {
                var projectFilter = new Filter();
                projectFilter.name = "Account";
                projectFilter.value = (order as ProjectReservation)?.ProjectRef?.Account;
                return new Filter[] { projectFilter };
            }
            return base.DefaultFilters();
        }
        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        private void DgOffersGrid_SelectedItemChanged(object sender, SelectedItemChangedEventArgs e)
        {
            LoadOfferlines(e.NewItem as DebtorOfferClient);
        }

        async private void LoadOfferlines(DebtorOfferClient offerlineClient)
        {
            dgOfferLinesGrid.ItemsSource = null;

            if (offerlineClient == null)
                return;

            dgOfferLinesGrid.UpdateMaster(offerlineClient);
            busyIndicator.IsBusy = true;
            await dgOfferLinesGrid.Filter(null);
            var offerlines = dgOfferLinesGrid.ItemsSource as IEnumerable<DebtorOfferLineClient>;
            if (offerlines != null)
                OfferLines = offerlines.ToArray();
            else
                OfferLines = null;
            busyIndicator.IsBusy = false;
        }

        private void ChildWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                OfferLines = null;
                this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
            }
            else
                if (e.Key == Key.Enter)
            {
                if (CancelButton.IsFocused)
                {
                    OfferLines = null;
                    this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
                    return;
                }
                OKButton_Click(null, null);
            }
        }
        bool createbuttonclicked;
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            createbuttonclicked = true;
            this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            OfferLines = null;
            this.dockCtrl.JustClosePanel(dockCtrl.Activpanel);
        }
        protected override void OnLayoutLoaded()
        {
            AttachVoucherGridPage.SetFloatingHeightAndWidth(dockCtrl, position, NameOfControl);
            base.OnLayoutLoaded();
        }
    }
}
