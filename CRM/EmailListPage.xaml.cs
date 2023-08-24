using UnicontaClient.Models;
using UnicontaClient.Pages;
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
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using Uniconta.DataModel;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools;
using Uniconta.API.Crm;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class EmailListPageGrid : CorasauDataGridClient
    {
        public override Type TableType
        {
            get { return typeof(CrmCampaignMemberClient); }
        }
    }

    public partial class EmailListPage : GridBasePage
    {
        UnicontaBaseEntity master;
        public EmailListPage(UnicontaBaseEntity _master)
           : base(_master)
        {
            InitPage(_master);
        }
        void InitPage(UnicontaBaseEntity _master)
        {
            InitializeComponent();
            master = _master;
            dgEmailList.api = api;
            localMenu.dataGrid = dgEmailList;
            dgEmailList.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgEmailList);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }

        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgEmailList.SelectedItem as CrmCampaignMemberClient;
            switch (ActionType)
            {
                case "FollowUp":
                    if (selectedItem != null)
                    {
                        var followUpHeader = string.Format("{0}:{2} {1}", Uniconta.ClientTools.Localization.lookup("FollowUp"), selectedItem._Name, Uniconta.ClientTools.Localization.lookup("EmailList"));
                        AddDockItem(TabControls.CrmFollowUpPage, dgEmailList.syncEntity, followUpHeader);
                    }
                    break;
                case "AddNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserNotesPage, dgEmailList.syncEntity);
                    break;
                case "AddDoc":
                    if (selectedItem != null)
                        AddDockItem(TabControls.UserDocsPage, dgEmailList.syncEntity, string.Format("{0}: {1}", Uniconta.ClientTools.Localization.lookup("Documents"), selectedItem._Name));
                    break;
                case "SendEmail":
                    SendEmail();
                    break;
                case "Remove":
                    dgEmailList.RemoveFocusedRowFromGrid();
                    break;  
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        public async override Task InitQuery()
        {
            var crmCampMemList = await api.Query<CrmCampaignMemberClient>(new UnicontaBaseEntity[] { master }, null);
            if (crmCampMemList != null && crmCampMemList.Length > 0)
            {
                Array.Sort(crmCampMemList, new CrmCampaignMemberSort());
                dgEmailList.ItemsSource = null;
                dgEmailList.ItemsSource = crmCampMemList;
            }

            busyIndicator.IsBusy = false;
            dgEmailList.Visibility = Visibility.Visible;
        }

           void SendEmail()
        {
            var rows = (ICollection<CrmCampaignMemberClient>)dgEmailList.GetVisibleRows();
            if (rows.Count == 0)
                return;

            var cwSendEmail = new CwSendEmail(api);
            cwSendEmail.Closed += async delegate
            {
                if (cwSendEmail.DialogResult == true && cwSendEmail.CompanySMTP != null)
                {
                    var crmAPI = new CrmAPI(api);
                    ErrorCodes res;
                    if (cwSendEmail.SendTestEmail)
                        res = await crmAPI.SendMailTest(cwSendEmail.CompanySMTP, cwSendEmail.Email, cwSendEmail.Name);
                    else
                        res = await crmAPI.SendMail(cwSendEmail.CompanySMTP, rows, cwSendEmail.FollowUp);
                    UtilDisplay.ShowErrorCode(res);
                }
            };
            cwSendEmail.Show();
        }

        protected override LookUpTable HandleLookupOnLocalPage(LookUpTable lookup, CorasauDataGrid dg)
        {
            return CreateEmailListPage.HandleLookupOnLocalPage(dgEmailList, lookup);
        }
    }
}
