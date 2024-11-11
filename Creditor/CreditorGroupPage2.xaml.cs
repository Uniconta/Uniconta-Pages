using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using System.Windows.Data;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class CreditorGroupPage2 : FormBasePage
    {
        CreditorGroupClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }

        public override Type TableType { get { return typeof(CreditorGroupClient); } }
        public override string NameOfControl { get { return TabControls.CreditorGroupPage2.ToString(); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (CreditorGroupClient)value; } }
        /*For Edit*/
        public CreditorGroupPage2(UnicontaBaseEntity sourcedata, bool isEdit = true)
            : base(sourcedata, isEdit)
        {
            if (!isEdit)
            {
                editrow = (CreditorGroupClient)StreamingManager.Clone(sourcedata);
                IdKey idkey = (IdKey)editrow;
                if (idkey.KeyStr != null)
                    idkey.KeyStr = null;
            }
            InitPage(api);
        }
        public CreditorGroupPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtGroup, txtGroup);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            InitializeComponent();
            StartLoadCache();
            layoutControl = layoutItems;
            cbRevenueFollowDC.ItemsSource = AppEnums.FollowItemCreditor.Values;
            PriceListlookupeditior.api = layoutGroupLookupEditor.api = itemNameGroupLookupEditor.api =
            SummeryAccountlookupeditior.api = lookupEndDiscountAccount.api = lookupSettlementDiscountAccount.api =
            lePurchaseAccount.api = lePurchaseAccount1.api = lePurchaseAccount2.api = lePurchaseAccount3.api = lePurchaseAccount4.api =
            lePurchaseVat.api = lePurchaseVat1.api = lePurchaseVat2.api = lePurchaseVat3.api = lePurchaseVat4.api = crudapi;
            lePurchaseVatOpr.api = lePurchaseVatOpr1.api = lePurchaseVatOpr2.api = lePurchaseVatOpr3.api = lePurchaseVatOpr4.api =
            leAutoNumber.api= lookupCurrencyAdjustment.api = lookupDiscountAccount.api= lookupDiscountAccountOffset.api = crudapi;

            if (!crudapi.CompanyEntity._UseVatOperation)
            {
                lePurchaseVatOpr.Visibility = lePurchaseVatOpr1.Visibility = lePurchaseVatOpr2.Visibility = lePurchaseVatOpr3.Visibility = lePurchaseVatOpr4.Visibility = Visibility.Collapsed;
            }

            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons("Delete" );
                editrow = CreateNew() as CreditorGroupClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVat)).ConfigureAwait(false);
            if (!Comp._AllowPurchaseVatOnSales)
                lePurchaseVat.cacheFilter = lePurchaseVat1.cacheFilter = lePurchaseVat2.cacheFilter = lePurchaseVat3.cacheFilter = lePurchaseVat4.cacheFilter = new VatCacheFilter(Cache, GLVatSaleBuy.Buy);

            if (Comp._UseVatOperation)
            {
                Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLVatType)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLVatType)).ConfigureAwait(false);
                lePurchaseVatOpr.cacheFilter = lePurchaseVatOpr1.cacheFilter = lePurchaseVatOpr2.cacheFilter = lePurchaseVatOpr3.cacheFilter = lePurchaseVatOpr4.cacheFilter
                    = new VatTypeSQLCacheFilter(Cache, GLVatSaleBuy.Buy);
            }

            Cache = Comp.GetCache(typeof(Uniconta.DataModel.NumberSerie)) ?? await api.LoadCache(typeof(Uniconta.DataModel.NumberSerie)).ConfigureAwait(false);
            leAutoNumber.cacheFilter = new NumberSerieSQLCacheFilter(Cache, true);

            LoadType(typeof(Uniconta.DataModel.GLAccount));
        }

        protected override void OnLayoutCtrlLoaded()
        {
            AdjustLayout();
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (!Comp.InvDuty)
                liExemptDuty.Visibility = Visibility.Collapsed;
        }
    }
}
