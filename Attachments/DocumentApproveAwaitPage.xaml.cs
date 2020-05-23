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
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class DocumentApproveAwaitGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(DocumentApproveAwaitClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class DocumentApproveAwaitPage : GridBasePage
    {
        public override string NameOfControl { get { return TabControls.DocumentApproveAwaitPage.ToString(); } }

        public DocumentApproveAwaitPage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }

        public DocumentApproveAwaitPage(SynchronizeEntity syncEntity) : base(syncEntity, false)
        {
            Init(syncEntity.Row);
        }

        protected override void SyncEntityMasterRowChanged(UnicontaBaseEntity args)
        {
            Init(args);
            SetHeader(args);
            InitQuery();
        }

        void SetHeader(UnicontaBaseEntity master)
        {
            string header = null;
            var syncMaster = master as VouchersClient;
            if (syncMaster != null)
                header = string.Format("{0}:{1}", Uniconta.ClientTools.Localization.lookup("PendingApproval"), syncMaster.RowId);
            if (header != null)
                SetHeader(header);
        }

        void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            dgDocApprovalAwait.UpdateMaster(master);
            localMenu.dataGrid = dgDocApprovalAwait;
            SetRibbonControl(localMenu, dgDocApprovalAwait);
            dgDocApprovalAwait.api = api;
            dgDocApprovalAwait.BusyIndicator = busyIndicator;
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgDocApprovalAwait.SelectedItem as DocumentApproveAwaitClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    dgDocApprovalAwait.DeleteRow();
                    break;
                case "SaveGrid":
                    dgDocApprovalAwait.SaveData();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
