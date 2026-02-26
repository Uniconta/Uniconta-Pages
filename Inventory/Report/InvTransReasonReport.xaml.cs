using DevExpress.CodeParser;
using DevExpress.Xpf.Grid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
using Uniconta.API.Inventory;
using Uniconta.API.Service;
using Uniconta.API.System;
using Uniconta.Client.Pages;
using Uniconta.ClientTools;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.ClientTools.Util;
using Uniconta.Common;
using Uniconta.DataModel;
using UnicontaClient.Models;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{

    public class InvTransReasonGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(InvTransReasonClient); } }
        public override bool Readonly { get => true; set { } }
    }

    public partial class InvTransReasonReport : GridBasePage
    {
        public InvTransReasonReport(BaseAPI API) : base(API, string.Empty)
        {
            InitPage(null);
        }
        public InvTransReasonReport(UnicontaBaseEntity master)
           : base(master)
        {
            InitPage(master);
        }

        void InitPage(UnicontaBaseEntity master)
        {
            InitializeComponent();
            this.DataContext = this;
            SetRibbonControl(localMenu, dgInvTransReason);
            if (master != null)
                dgInvTransReason.UpdateMaster(master);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgInvTransReason.SelectedItem as InvTransReasonClient;
            switch (ActionType)
            {
                case "CreditNote":
                    if (selectedItem != null)
                        AddDockItem(TabControls.Invoices, selectedItem, $"{Uniconta.ClientTools.Localization.lookup("CreditNote")}_{selectedItem.Item}");
                    break;
                case "PostedBy":
                    if (selectedItem != null)
                        JournalPosted(selectedItem);
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }

        async private void JournalPosted(InvTransReasonClient selectedItem)
        {
            var result = await api.Query(new GLDailyJournalPostedClient(), new UnicontaBaseEntity[] { selectedItem }, null);
            if (result?.Length == 1)
            {
                CWGLPostedClientFormView cwPostedClient = new CWGLPostedClientFormView(result[0]);
                cwPostedClient.Show();
            }
        }
    }
}
