using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using System.Collections;
using Uniconta.ClientTools;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class GLTransSumGridClient : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(GLTransSumClient); } }
        public override IComparer GridSorting { get { return new GLTransSumSort(); } }

        public override bool Readonly { get { return true; } }
    }
    /// <summary>
    /// Interaction logic for GLTransSumClientPage.xaml
    /// </summary>
    public partial class GLTransSumClientPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.AccountantClientPage; } }

        public GLTransSumClientPage(UnicontaBaseEntity sourceData) : base(sourceData)
        {
            InitPage(sourceData);
        }

        public GLTransSumClientPage(SynchronizeEntity syncMaster):base(syncMaster,true)
        {
            InitPage(syncMaster.Row);
        }

        private void InitPage(UnicontaBaseEntity masterRow)
        {
            InitializeComponent();
            dgGlTransSumClientGrid.api = api;
            dgGlTransSumClientGrid.BusyIndicator = busyIndicator;
            InitMaster(masterRow);
            SetRibbonControl(localMenu, dgGlTransSumClientGrid);
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            gridRibbon_BaseActions(ActionType);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            InitMaster(args);
            if (args != null)
            {
                var masterClient = args as GLAccountClient;
                var header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("AccountsTotal"), masterClient?._Account);
                SetHeader(header);
            }
            InitQuery();
        }
        private void InitMaster(UnicontaBaseEntity masterRow)
        {
            if (masterRow != null)
                dgGlTransSumClientGrid.UpdateMaster(masterRow);
        }
    }
}
