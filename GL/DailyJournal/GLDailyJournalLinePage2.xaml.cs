using Uniconta.API.System;
using UnicontaClient.Models;
using UnicontaClient.Utilities;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
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
using Uniconta.ClientTools;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class GLDailyJournalLinePage2 : FormBasePage
    {
        GLDailyJournalLineClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.GLDailyJournalLinePage2.ToString(); } }

        public override Type TableType { get { return typeof(GLDailyJournalLineClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (GLDailyJournalLineClient)value; } }
        public GLDailyJournalLinePage2(SynchronizeEntity syncEntity)
            : base(true, syncEntity)
        {
            InitializeComponent();
            ModifiedRow = this.syncEntity.Row;
            InitPage(api);
            SetHeader();
        }

        private void SetHeader()
        {
            if (editrow == null)
                return;
            var header = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("AllFields"), editrow._Account);
            SetHeader(header);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            InitMaster(args);
        }

        private void InitMaster(UnicontaBaseEntity row)
        {
            ModifiedRow = row;
            layoutItems.DataContext = null;
            layoutItems.DataContext = editrow;
            SetHeader();
        }

        void InitPage(CrudAPI crudapi)
        {
            leAccount.api = leOffsetAccount.api = lePayment.api = leSplit.api = leTransType.api = leVat.api = leOffsetVat.api = leVatOffsetOperation.api = leVatOperation.api = leWithholding.api =
            leDim1.api = leDim2.api = leDim3.api = leDim4.api = leDim5.api = leProject.api = lePrCaegory.api = leTask.api = leAsset.api = crudapi;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            var comp = api.CompanyEntity;
            if (comp.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, leDim1, leDim2, leDim3, leDim4, leDim5, usedim);
            if (!comp.Project)
                lblProject.Visibility = lblPrCategory.Visibility = lblProjectText.Visibility = lblTask.Visibility = Visibility.Collapsed;
            if(!comp.FixedAsset)
                lblAsset.Visibility = lblAssetPostType.Visibility = Visibility.Collapsed;
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        private void cboffsetAccount_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            SetOffsetAccountSource(editrow._OffsetAccountTypeEnum);
        }
        async private void SetAccountSource(GLJournalAccountType accountType)
        {
            var t = GetAccountType(accountType);
            var api = this.api;
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(t);
            if (Cache == null)
                Cache = await Comp.LoadCache(t, api);
            leAccount.ItemsSource = Cache;
        }
        async private void SetOffsetAccountSource(GLJournalAccountType accountType)
        {
            var t = GetAccountType(accountType);
            var api = this.api;
            var Comp = api.CompanyEntity;
            var Cache = Comp.GetCache(t);
            if (Cache == null)
                Cache = await Comp.LoadCache(t, api);
            leOffsetAccount.ItemsSource = Cache;
        }
        private Type GetAccountType(GLJournalAccountType accountType)
        {
            Type t = null;
            switch (accountType)
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
            return t;
        }

        private void cboAccounttype_SelectedIndexChanged(object sender, RoutedEventArgs e)
        {
            SetAccountSource(editrow._AccountTypeEnum);
        }

    }
}
