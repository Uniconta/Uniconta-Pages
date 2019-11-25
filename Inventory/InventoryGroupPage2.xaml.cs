using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Windows;
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
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class InventoryGroupPage2 : FormBasePage
    {
        InvGroupClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.InventoryGroupPage2.ToString(); } }

        public override Type TableType { get { return typeof(InvGroupClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (InvGroupClient)value; } }      
        public InventoryGroupPage2(UnicontaBaseEntity sourcedata, bool isEdit)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
            {
                editrow = (InvGroupClient)StreamingManager.Clone(sourcedata);
                editrow.Group = string.Empty;
                editrow.Name = string.Empty;
                IdKey idkey = (IdKey)editrow;
                if (idkey.KeyStr != null)
                    idkey.KeyStr = null;
            }
            InitPage(api);
        }
        public InventoryGroupPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtGroup, txtGroup);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            var Comp = api.CompanyEntity;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            leInvReceipt.api = leCostAccount.api = leInvAccount.api =
            leRevenueAccount.api = leRevenueAccount1.api = leRevenueAccount2.api = leRevenueAccount3.api = leRevenueAccount4.api = leBomIssue.api= leBomReciept.api= leBomIncVal.api= leJournalOffset.api =
            lePurchaseAccount.api = lePurchaseAccount1.api = lePurchaseAccount2.api = lePurchaseAccount3.api = lePurchaseAccount4.api =
            leSalesVat.api = leSalesVat1.api = leSalesVat2.api = leSalesVat3.api = leSalesVat4.api = leLossProfit.api= leRevaluation.api=
            lePurchaseVat.api = lePurchaseVat1.api = lePurchaseVat2.api = lePurchaseVat3.api = lePurchaseVat4.api = leAutoNumber.api = leDutyGroup.api= crudapi;
            cbCountry.ItemsSource = Enum.GetValues(typeof(Uniconta.Common.CountryCode));
            if (editrow == null && LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete" );
                editrow =CreateNew() as InvGroupClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            if (!Comp.InvBOM)
                bomGroup.Visibility = Visibility.Collapsed;
            if (!Comp.InvDuty)
                liDutyGroup.Visibility = Visibility.Collapsed;

            StartLoadCache();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var Cache = Comp.GetCache(typeof(GLVat)) ?? await Comp.LoadCache(typeof(GLVat), api).ConfigureAwait(false);

            var vatsales = new VatCacheFilter(Cache, GLVatSaleBuy.Sales);
            leSalesVat.cacheFilter = leSalesVat1.cacheFilter = leSalesVat2.cacheFilter = leSalesVat3.cacheFilter = leSalesVat4.cacheFilter = vatsales;
            var vatbuy = new VatCacheFilter(Cache, GLVatSaleBuy.Buy);
            lePurchaseVat.cacheFilter = lePurchaseVat1.cacheFilter = lePurchaseVat2.cacheFilter = lePurchaseVat3.cacheFilter = lePurchaseVat4.cacheFilter = vatbuy;

            Cache = Comp.GetCache(typeof(Uniconta.DataModel.NumberSerie)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.NumberSerie), api).ConfigureAwait(false);
            var numbers = new NumberSerieSQLCacheFilter(Cache, true);
            leAutoNumber.cacheFilter = numbers;

            LoadType(typeof(Uniconta.DataModel.GLAccount));
        }
    }
}
