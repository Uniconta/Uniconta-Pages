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
using Uniconta.ClientTools.Controls;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public partial class BankStatementPage2 : FormBasePage
    {
        BankStatementClient editrow;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);

        }
        public override string NameOfControl { get { return TabControls.BankStatementPage2.ToString(); } }
        public override Type TableType { get { return typeof(BankStatementClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (BankStatementClient)value; } }

        bool editMode = false;
        SQLCache BankStmtCache;
        public BankStatementPage2(UnicontaBaseEntity sourcedata)
            : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
            leAccount.IsEnabled = false;
            editMode = true;
        }
        public BankStatementPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
#if !SILVERLIGHT
            FocusManager.SetFocusedElement(leAccount, leAccount);
#endif
        }
        void InitPage(CrudAPI crudapi)
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            leAccount.api = lkJournal.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = crudapi;
            if (crudapi.CompanyEntity.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow =CreateNew() as BankStatementClient;
                editrow._DaysSlip = 3;
            }
            layoutItems.DataContext = editrow;
            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;

            StartLoadCache();
        }
        private void frmRibbon_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "Save":
                    if (!editMode)
                    {
                        if (BankStmtCache == null) return;
                        var list = (IEnumerable<Uniconta.DataModel.BankStatement>)BankStmtCache.GetNotNullArray;
                        var exist = list.Where(x => x._Account == editrow._Account);

                        if (exist.Count() == 0)
                            frmRibbon_BaseActions(ActionType);
                        else
                        {
                            var msg = string.Format(Uniconta.ClientTools.Localization.lookup("AlreadyExistOBJ"), editrow._Account);
                            UnicontaMessageBox.Show(msg, Uniconta.ClientTools.Localization.lookup("Information"), MessageBoxButton.OK);
                        }
                    }
                    else
                        frmRibbon_BaseActions(ActionType);
                    break;
                default:
                    frmRibbon_BaseActions(ActionType);
                    break;
            }
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            var Cache = Comp.GetCache(typeof(Uniconta.DataModel.GLAccount));
            if (Cache == null)
                Cache = await Comp.LoadCache(typeof(Uniconta.DataModel.GLAccount), api).ConfigureAwait(false);

            BankStmtCache = Comp.GetCache(typeof(Uniconta.DataModel.BankStatement));
            if (BankStmtCache == null)
                BankStmtCache = await Comp.LoadCache(typeof(Uniconta.DataModel.BankStatement), api).ConfigureAwait(false);

            var banks = new LedgerBankFilter(Cache);
            leAccount.cacheFilter = banks;
        }
    }
}
