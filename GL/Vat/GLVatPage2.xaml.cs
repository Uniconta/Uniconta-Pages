using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
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

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class GLVatPage2 : FormBasePage
    {
        GLVatClient editrow;
        SQLCache vatCache;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.GLVatPage2.ToString(); } }

        public override Type TableType { get { return typeof(GLVatClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (GLVatClient)value; } }
        public GLVatPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public GLVatPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtVat, txtVat);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;
            cbMethod.ItemsSource = AppEnums.GLVatCalculationMethod.Values;
            leAccountRate2.api = leOffsetAccountRate2.api = leRate2Vat.api = leRate1Vat.api = leOffsetVatOperation.api=
            leAccount.api = leOffsetAccount.api = leUnrealizedAccount.api = leTypeBuy.api = leTypeSales.api = crudapi;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons( "Delete" );
                editrow =CreateNew() as GLVatClient;
                // editrow._Method = GLVatCalculationMethod.Automatic;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            EnableSalesBuyType();

            if (!crudapi.CompanyEntity._UseUnrealizedVat)
                liUnrealizedAccount.Visibility = Visibility.Collapsed;

            StartLoadCache();
        }
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            cbvattype.Focus();
            frmRibbon_BaseActions(ActionType);
        }
        void EnableSalesBuyType()
        {
            leTypeSales.IsEnabled = false;
            leTypeBuy.IsEnabled = false;
            switch (editrow._VatType)
            {
                case GLVatSaleBuy.Sales:
                    leTypeSales.IsEnabled = true;
                    break;
                case GLVatSaleBuy.Buy:
                    leTypeBuy.IsEnabled = true;
                    break;
            }
        }
        private void cbvattype_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            EnableSalesBuyType();
        }

        private void DateEditor_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            DateEditor de = (DateEditor)sender;
            ItemRateAfterDate.IsEnabled = ItemRate2AfterDate.IsEnabled = de.EditValue == null ? false : true;  
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;
            vatCache = Comp.GetCache(typeof(Uniconta.DataModel.GLVat)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.GLVat), api).ConfigureAwait(false);

            if (editrow.RowId != 0)
            {
                leRate1Vat.cacheFilter =
                leRate2Vat.cacheFilter = new VatCacheFilter(vatCache, editrow._VatType);
            }

            var Cache = Comp.GetCache(typeof(GLVatType)) ?? await Comp.LoadCache(typeof(GLVatType), api).ConfigureAwait(false);
            leTypeSales.cacheFilter = new VatTypeSQLCacheFilter(Cache, GLVatSaleBuy.Sales);
            leTypeBuy.cacheFilter = new VatTypeSQLCacheFilter(Cache, GLVatSaleBuy.Buy);
        }

        private void txtVat_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            var s = sender as TextEditor;
            if (s != null && s.IsLoaded && vatCache != null)
            {
                if (vatCache.GetKeyList().Contains(s.Text))
                {
                    UnicontaMessageBox.Show(string.Format("{0} {1} ", Uniconta.ClientTools.Localization.lookup("Vat"), string.Format(Uniconta.ClientTools.Localization.lookup("AlreadyExistOBJ"), s.Text))
                        , Uniconta.ClientTools.Localization.lookup("Warning"));
                }
            }
        }
    }
}
