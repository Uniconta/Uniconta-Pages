using UnicontaClient.Models;
using UnicontaClient.Utilities;
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
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    
    public partial class IOBSClaimPaymentEntriesPage2 : FormBasePage
    {
        IOBSClaimPaymentEntryClient editrow;
        SQLCache ClaimPayEntryCashe;
        public override void OnClosePage(object[] RefreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, RefreshParams);
        }
        public override string NameOfControl { get { return TabControls.IOBSClaimPaymentEntriesPage2.ToString(); } }
        public override Type TableType { get { return typeof(IOBSClaimPaymentEntryClient); } }
        public override UnicontaBaseEntity ModifiedRow { get { return editrow; } set { editrow = (IOBSClaimPaymentEntryClient)value; } }

        public IOBSClaimPaymentEntriesPage2(UnicontaBaseEntity sourcedata)
             : base(sourcedata, true)
        {
            InitializeComponent();
            InitPage(api);
        }

        public IOBSClaimPaymentEntriesPage2(CrudAPI crudApi, string dummy)
            : base(crudApi, dummy)
        {
            InitializeComponent();
            InitPage(crudApi);
        }

        void InitPage(CrudAPI crudapi)
        {
            ribbonControl = frmRibbon;
            var Comp = api.CompanyEntity;
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            leCustomerNumber.api = crudapi;
            StartLoadCache();
            if (LoadedRow == null)
            {
                frmRibbon.DisableButtons(new string[] { "Delete" });
                editrow = CreateNew() as IOBSClaimPaymentEntryClient;
            }
            layoutItems.DataContext = editrow;

            frmRibbon.OnItemClicked += frmRibbon_OnItemClicked;
        }

        private void frmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        protected override async void LoadCacheInBackGround()
        {
            var api = this.api;
            var Comp = api.CompanyEntity;

            ClaimPayEntryCashe = Comp.GetCache(typeof(Uniconta.DataModel.IOBSClaimPaymentEntry)) ?? await Comp.LoadCache(typeof(Uniconta.DataModel.IOBSClaimPaymentEntry), api).ConfigureAwait(false);

            LoadType(new Type[] { typeof(Uniconta.DataModel.Debtor)});
        }
    }
}
