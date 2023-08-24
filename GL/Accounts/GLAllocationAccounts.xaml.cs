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
using System.Threading.Tasks;

using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class CorasauDataGridAllocationAccounts : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(GLSplitAccountClient); }
        }
    }
    public partial class GLAllocationAccounts : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.GLAllocationAccounts; } }

        public GLAllocationAccounts(UnicontaBaseEntity master) : base(master)
        {
            InitializeComponent();
            InitPage(master);         
           
        }

        public GLAllocationAccounts(SynchronizeEntity syncEntity) : base(syncEntity, true)
        {
            InitializeComponent();
            InitPage(syncEntity.Row);
            SetHeader();
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgGLSplitAccount.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        private void SetHeader()
        {
            string key = Utility.GetHeaderString(dgGLSplitAccount.masterRecord);

            if (string.IsNullOrEmpty(key)) return;

            string header = string.Format("{0} : {1}", Uniconta.ClientTools.Localization.lookup("SetupAllocations"), key);
            SetHeader(header);
        }

        void InitPage(UnicontaBaseEntity masterRecord)
        {

            dgGLSplitAccount.api = api;
            dgGLSplitAccount.UpdateMaster(masterRecord);
            SetRibbonControl(localMenu, dgGLSplitAccount);
            dgGLSplitAccount.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        public override void Utility_Refresh(string screenName, object argument = null)
        {
            if (screenName == TabControls.AllocationAccountPage2)
                dgGLSplitAccount.UpdateItemSource(argument); 
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            GLSplitAccountClient selectedItem = dgGLSplitAccount.SelectedItem as GLSplitAccountClient;
            switch (ActionType)
            {
                case "AddRow":
                    var newItem = dgGLSplitAccount.GetChildInstance();
                    object[] param = new object[2];
                    param[0] = newItem;
                    param[1] = false;
                    AddDockItem(TabControls.AllocationAccountPage2, param, Uniconta.ClientTools.Localization.lookup("SetupAllocations"), "Add_16x16");
                    break;
                case "EditRow":
                    if (selectedItem == null)
                        return;
                    object[] para = new object[2];
                    para[0] = selectedItem;
                    para[1] = true;
                    AddDockItem(TabControls.AllocationAccountPage2, para, string.Format("{0}:{1}, {2}", Uniconta.ClientTools.Localization.lookup("Account"), selectedItem.Account, Uniconta.ClientTools.Localization.lookup("SetupAllocations")));
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
