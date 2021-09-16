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
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DebtorProjTaskGroupGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DebtorProjTaskGroupClient); } }
        public override bool Readonly { get { return false; } }
    }
    public partial class DebtorProjTaskGroupPage : GridBasePage
    {
        public DebtorProjTaskGroupPage(BaseAPI api, string lookupKey)
            : base(api, lookupKey)
        {
            InitPage(null);
        }

        public DebtorProjTaskGroupPage(UnicontaBaseEntity master)
            : base(master)
        {
            InitPage(master);
        }

        public DebtorProjTaskGroupPage(SynchronizeEntity syncEntity)
          : base(syncEntity, false)
        {
            InitPage(syncEntity.Row);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            SetRibbonControl(localMenu, dgDebtorProjTask);
            dgDebtorProjTask.api = api;
            dgDebtorProjTask.UpdateMaster(master);
            dgDebtorProjTask.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += LocalMenu_OnItemClicked;
            if (master is Debtor)
                Account.Visible = Account.ShowInColumnChooser = AccountName.Visible = AccountName.ShowInColumnChooser= false;
            else if(master is PrTaskGroup)
                Group.Visible = Group.ShowInColumnChooser = false;
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            dgDebtorProjTask.UpdateMaster(args);
            SetHeader();
            InitQuery();
        }
        
        void SetHeader()
        {
            string header = string.Empty;
            var debtor = dgDebtorProjTask.masterRecord as Debtor;
            if (debtor != null)
                header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TaskGroups"), debtor.KeyName);
            else
            {
                var prTaskGrp = dgDebtorProjTask.masterRecord as PrTaskGroup;
                if(prTaskGrp != null)
                    header = string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("TaskGroups"), prTaskGrp.KeyName);
            }
            SetHeader(header);
        }

        private void LocalMenu_OnItemClicked(string ActionType)
        {
            switch (ActionType)
            {
                case "AddRow":
                    dgDebtorProjTask.AddRow();
                    break;
                case "DeleteRow":
                    dgDebtorProjTask.DeleteRow();
                    break;
                case "SaveGrid":
                    saveGrid();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
