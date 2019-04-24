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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class DebtorGroupPage2 : FormBasePage
    {
        DebtorGroupClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.DebtorGroupPage2.ToString(); } }

        public override Type TableType { get { return typeof(DebtorGroupClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (DebtorGroupClient)value; } }
        /*For Edit*/
        public DebtorGroupPage2(UnicontaBaseEntity sourcedata, bool isEdit = true)
            : base(sourcedata, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
            {  
                editrow = (DebtorGroupClient)StreamingManager.Clone(sourcedata);
                IdKey idkey = (IdKey)editrow;
                if (idkey.KeyStr != null)
                    idkey.KeyStr = null;
            }
            InitPage(api);
        }
        public DebtorGroupPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtGroup, txtGroup);
#endif
        }
        protected override bool IsLayoutSaveRequired()
        {
            return false;
        }
        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            cbRevenueFollowDC.ItemsSource = AppEnums.FollowItemDebtor.Values;
            lookupEndDiscountAccount.api =
            PriceListlookupeditior.api =
            lookupSummeryRevenue.api = crudapi;
            lookupSummeryRevenue1.api = crudapi;
			lookupSummeryRevenue2.api = crudapi;
            lookupSummeryRevenue3.api = crudapi;
            lookupSummeryRevenue4.api = crudapi;
			SummeryAccountlookupeditior.api = crudapi;
			leSalesVat.api = crudapi;
			leSalesVat1.api = crudapi;
			leSalesVat2.api = crudapi;
            leSalesVat3.api = crudapi;
            leSalesVat4.api = crudapi;
            leSalesVatOpr.api = leSalesVatOpr1.api = leSalesVatOpr2.api = leSalesVatOpr3.api = leSalesVatOpr4.api= lookupCurrencyAdjustment.api = crudapi;
            leAutoNumber.api = crudapi;
            layoutGroupLookupEditor.api = crudapi;
            itemNameGroupLookupEditor.api = crudapi;

            if (!crudapi.CompanyEntity._UseVatOperation)
            {
                leSalesVatOpr.Visibility = leSalesVatOpr1.Visibility = leSalesVatOpr2.Visibility = leSalesVatOpr3.Visibility = leSalesVatOpr4.Visibility = Visibility.Collapsed;
            }

			lookupSettlementDiscountAccount.api = crudapi;

            if (LoadedRow == null && editrow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editrow = CreateNew() as DebtorGroupClient;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            StartLoadCache();
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        protected override void OnLayoutCtrlLoaded()
        {
            AdjustLayout();
        }

        void AdjustLayout()
        {
            var Comp = api.CompanyEntity;
            if (Comp._CountryId != CountryCode.Iceland)
                grpMiscellaneous.Visibility = Visibility.Collapsed;
            if (!Comp.InvDuty)
                liExemptDuty.Visibility = Visibility.Collapsed;
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var Cache = Comp.GetCache(typeof(Uniconta.DataModel.NumberSerie)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.NumberSerie), api).ConfigureAwait(false);
            var numbers = new NumberSerieSQLCacheFilter(Cache, true);
            leAutoNumber.cacheFilter = numbers;

            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat) });
        }
    }
}
