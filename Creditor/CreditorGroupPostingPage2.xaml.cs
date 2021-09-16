using UnicontaClient.Models;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.API.System;
using UnicontaClient.Utilities;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    /// <summary>
    /// Interaction logic for CreditorGroupPostingPage2.xaml
    /// </summary>
    public partial class CreditorGroupPostingPage2 : FormBasePage
    {
        DCGroupPostingClient editRow;
        public override string NameOfControl { get { return TabControls.CreditorGroupPostingPage2; } }
        bool isGroupEnabled = true;
        public CreditorGroupPostingPage2(UnicontaBaseEntity sourceData, UnicontaBaseEntity groupMaster,bool isEdit = true) : base(sourceData, isEdit)
        {
            InitializeComponent();
            if (!isEdit)
                editRow = (DCGroupPostingClient)StreamingManager.Clone(sourceData);
            isGroupEnabled = !isEdit;
            InitPage(api, groupMaster);
        }

        public CreditorGroupPostingPage2(CrudAPI crudApi,UnicontaBaseEntity groupMaster):base(crudApi,null)
        {
            InitializeComponent();
            InitPage(crudApi, groupMaster);
        }
        private void InitPage(CrudAPI crudApi, UnicontaBaseEntity groupMaster = null)
        {
            layoutControl = layoutItems;
            leRevenueAccount.api = leRevenueAccount1.api =leRevenueAccount2.api = leRevenueAccount3.api = leRevenueAccount4.api = leInvGroup.api = leGroup.api
               = leVat.api = leVat1.api = leVat2.api = leVat3.api = leVat4.api = crudApi;

            if (LoadedRow == null && editRow == null)
            {
                frmRibbon.DisableButtons("Delete");
                editRow = CreateNew() as DCGroupPostingClient;
                editRow.SetMaster(groupMaster);
                editRow._DCType = 2;
            }
            layoutItems.DataContext = editRow;
            frmRibbon.OnItemClicked += FrmRibbon_OnItemClicked;
            SetControlVisibility(groupMaster);
            SetHeaders();
        }

        private void SetControlVisibility(UnicontaBaseEntity groupMaster)
        {
            if (groupMaster != null)
            {
                if (groupMaster is Uniconta.DataModel.DCGroup)
                {
                    liGroup.Visibility = Visibility.Collapsed;
                    leInvGroup.IsEnabled = isGroupEnabled;
                }
                else if (groupMaster is Uniconta.DataModel.InvGroup)
                {
                    liInventoryGroup.Visibility = Visibility.Collapsed;
                    leGroup.IsEnabled = isGroupEnabled;
                    leGroup.SetForeignKeyRef(typeof(CreditorGroupClient), 0);
                }
            }
            else
                liGroup.Visibility = Visibility.Collapsed;
        }

        private void SetHeaders()
        {
            grpRevenueAccount.Header = Uniconta.ClientTools.Localization.lookup("PurchaseAccount");
            liRevenueAccount.Label = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("Domestic"));
            liRevenueAccount1.Label = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("EUMember"));
            liRevenueAccount2.Label = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("Abroad"));
            liRevenueAccount3.Label = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("NoVATRegistration"));
            liRevenueAccount4.Label = string.Format("{0} ({1})", Uniconta.ClientTools.Localization.lookup("PurchaseAccount"), Uniconta.ClientTools.Localization.lookup("ExemptVat"));
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
