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
            layoutControl = layoutItems;
            leAccount.api = lkJournal.api = cmbDim1.api = cmbDim2.api = cmbDim3.api = cmbDim4.api = cmbDim5.api = crudapi;
            if (crudapi.CompanyEntity.NumberOfDimensions == 0)
                usedim.Visibility = Visibility.Collapsed;
            else
                Utility.SetDimensions(crudapi, lbldim1, lbldim2, lbldim3, lbldim4, lbldim5, cmbDim1, cmbDim2, cmbDim3, cmbDim4, cmbDim5, usedim);

            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons( "Delete" );
                editrow =CreateNew() as BankStatementClient;
                editrow._DaysSlip = 3;
                editrow._BankAsOffset = true;
                editrow._BankConnect2Journal = false;
            }
            if (crudapi.CompanyEntity._DirectDebit == false)
            {
                grpBankConnect.Visibility = Visibility.Collapsed;
                editrow._BankConnect2Journal = false;
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
                    if(editrow.BankConnect2Journal == true && string.IsNullOrWhiteSpace(lkJournal.Text))
                    {
                        var errTxt = string.Format("{0} ({1}: {2})",Uniconta.ClientTools.Localization.lookup("FieldCannotBeEmpty"),
                         Uniconta.ClientTools.Localization.lookup("Field"), Uniconta.ClientTools.Localization.lookup("Journal"));
                         UnicontaMessageBox.Show(errTxt, string.Concat(Uniconta.ClientTools.Localization.lookup("Error"), " - ", string.Concat(Uniconta.ClientTools.Localization.lookup("BankConnect"))));
                        return;
                    }
                    if (!editMode)
                    {
                        if (BankStmtCache == null) return;
                        var list = (IEnumerable<Uniconta.DataModel.BankStatement>)BankStmtCache.GetNotNullArray;
                        var exist = list.Any(x => x._Account == editrow._Account);
                        if (!exist)
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

        protected override async System.Threading.Tasks.Task LoadCacheInBackGroundAsync()
        {
            var api = this.api;
            var Cache = api.GetCache(typeof(Uniconta.DataModel.GLAccount)) ?? await api.LoadCache(typeof(Uniconta.DataModel.GLAccount)).ConfigureAwait(false);
            leAccount.cacheFilter = new LedgerBankFilter(Cache);
            BankStmtCache = api.GetCache(typeof(Uniconta.DataModel.BankStatement)) ?? await api.LoadCache(typeof(Uniconta.DataModel.BankStatement)).ConfigureAwait(false);
        }
    }
}
