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
    public partial class GLDailyJournalPage2 : FormBasePage
    {
        GLDailyJournalClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.GLDailyJournalPage2.ToString(); } }
        public override Type TableType { get { return typeof(GLDailyJournalClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (GLDailyJournalClient)value; } }
        public GLDailyJournalPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }
        public GLDailyJournalPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(txtJournal, txtJournal);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;
            cbdefaultAccount.ItemsSource = cboffsetAccount.ItemsSource = AppEnums.GLAccountType.Values;
            cbDateFunction.ItemsSource = AppEnums.GLJournalDate.Values;
            numSerielookupeditor.api = dim1lookupeditior.api = dim2lookupeditior.api = dim3lookupeditior.api = dim4lookupeditior.api = dim5lookupeditior.api =
            Vatlookupeditior.api = AccountLookupEditor.api = OffsetAccountLookupEditor.api = crudapi;
            TraceAccountEditior.api = TraceAccountEditior2.api = TraceAccountEditior3.api = TraceAccountEditior4.api = TraceAccountEditior5.api = TraceAccountEditior6.api = leTransType.api= crudapi;
            layoutControl = layoutItems;
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons("Delete" );
                editrow =CreateNew() as GLDailyJournalClient;
            }
            layoutItems.DataContext = editrow;
            Utility.SetDimensions(api, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, dim1lookupeditior, dim2lookupeditior, dim3lookupeditior, dim4lookupeditior, dim5lookupeditior, useDim);
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
            AcItem.ButtonClicked += AcItem_ButtonClicked;

            StartLoadCache();
        }

        void AcItem_ButtonClicked(object sender)
        {
            btnGoNumberSeries(null, null);
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private async void btnGoNumberSeries(object sender, System.Windows.RoutedEventArgs e)
        {
            NumberSerieClient nsc = (NumberSerieClient)await GetReference(editrow.NumberSerie, typeof(NumberSerieClient));
            if (nsc != null)
                AddDockItem(TabControls.NumberSeriePage2, nsc);
        }

        private void cbdefaultAccount_SelectedIndexChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            SetAccountSource();
        }
        private async void SetOffsetAccountSource()
        {
            Type t = null;
            switch (editrow._DefaultOffsetAccountType)
            {
                case GLJournalAccountType.Finans:
                    t = typeof(GLAccount);
                    break;
                case GLJournalAccountType.Debtor:
                    t = typeof(Debtor);
                    break;
                case GLJournalAccountType.Creditor:
                    t = typeof(Uniconta.DataModel.Creditor);
                    break;
            }
            var api = this.api;
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(t);
            if (Cache == null)
                Cache = await Comp.LoadCache(t, api);
            editrow.OffsetAccountSource = Cache;
        }
        private async void SetAccountSource()
        {
            Type t = null;
            switch (editrow._DefaultAccountType)
            {
                case GLJournalAccountType.Finans:
                    t = typeof(GLAccount);
                    break;
                case GLJournalAccountType.Debtor:
                    t = typeof(Debtor);
                    break;
                case GLJournalAccountType.Creditor:
                    t = typeof(Uniconta.DataModel.Creditor);
                    break;
            }
            var api = this.api;
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(t);
            if (Cache == null)
                Cache = await Comp.LoadCache(t, api);
            editrow.AccountSource = Cache;
        }

        private void cboffsetAccount_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            SetOffsetAccountSource();
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var Cache = Comp.GetCache(typeof(Uniconta.DataModel.NumberSerie)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.NumberSerie), api).ConfigureAwait(false);
            var numbers = new NumberSerieSQLCacheFilter(Cache, false);
            numSerielookupeditor.cacheFilter = numbers;

            LoadType(new Type[] { typeof(Uniconta.DataModel.GLAccount), typeof(Uniconta.DataModel.GLVat) });
        }
    }
}