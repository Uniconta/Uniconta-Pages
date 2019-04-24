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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Uniconta.API.System;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for DebtorGroupPostingPage2.xaml
    /// </summary>
    public partial class DebtorGroupPostingPage2 : FormBasePage
    {
        DCGroupPostingClient editRow;
        public override string NameOfControl { get { return TabControls.DebtorGroupPostingPage2; } }

        /*For Edit*/
        public DebtorGroupPostingPage2(UnicontaBaseEntity sourceData, bool isEdit = true) : base(sourceData, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
                editRow = (DCGroupPostingClient)StreamingManager.Clone(sourceData);
            InitPage(api);
        }
        public DebtorGroupPostingPage2(CrudAPI crudApi, UnicontaBaseEntity groupMaster) : base(crudApi, null)
        {
            InitializeComponent();
            InitPage(crudApi, groupMaster);
        }
        UnicontaBaseEntity groupMaster;
        private void InitPage(CrudAPI crudApi, UnicontaBaseEntity grMaster = null)
        {
            BusyIndicator = busyIndicator;
            layoutControl = layoutItems;
            this.groupMaster = grMaster;
            leCostAccount.api = leCostAccount1.api = leCostAccount2.api = leCostAccount3.api = leCostAccount4.api = leRevenueAccount.api = leRevenueAccount1.api =
            leRevenueAccount2.api = leRevenueAccount3.api = leRevenueAccount4.api = leInvGroup.api = leGroup.api = leVat.api= leVat1.api = leVat2.api = leVat3.api = leVat4.api = crudApi;

            if (LoadedRow == null && editRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editRow = CreateNew() as DCGroupPostingClient;
                editRow.SetMaster(groupMaster);
                editRow._DCType = 1;
            }
            layoutItems.DataContext = editRow;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            SetControlVisibility(groupMaster);
        }
        public override void RowsPastedDone()
        {
            if (this.groupMaster != null)
                editRow.SetMaster(groupMaster);
        }
        private void SetControlVisibility(UnicontaBaseEntity groupMaster)
        {
            if (groupMaster != null)
            {
                if (groupMaster is Uniconta.DataModel.DCGroup)
                {
                    liGroup.Visibility = Visibility.Collapsed;
                    leInvGroup.IsEnabled = true;
                }
                else if (groupMaster is Uniconta.DataModel.InvGroup)
                {
                    liInventoryGroup.Visibility = Visibility.Collapsed;
                    leGroup.IsEnabled = true;
                    leGroup.SetForeignKeyRef(typeof(DebtorGroupClient), 0);
                }
            }
            else
                liGroup.Visibility = Visibility.Collapsed;
        }

        private void FrmRibbon_OnItemClicked(string ActionType)
        {
            frmRibbon_BaseActions(ActionType);
        }

        public override UnicontaBaseEntity ModifiedRow { get { return editRow; } set { editRow = (DCGroupPostingClient)value; } }

        public override Type TableType { get { return typeof(DCGroupPostingClient); } }

        public override void OnClosePage(object[] refreshParams)
        {
            globalEvents.OnRefresh(NameOfControl, refreshParams);
        }
    }
}
