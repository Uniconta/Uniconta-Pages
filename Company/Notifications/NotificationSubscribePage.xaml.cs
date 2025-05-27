using UnicontaClient.Models;
using UnicontaClient.Pages;
using System;
using System.Collections;
using System.Linq;
using Uniconta.API.Service;
using Uniconta.ClientTools;
using Uniconta.ClientTools.DataModel;
using Uniconta.ClientTools.Page;
using Uniconta.Common;
using UnicontaClient.Controls.Dialogs;
using Uniconta.API.System;
using Uniconta.ClientTools.Controls;
using Uniconta.ClientTools.Util;

using UnicontaClient.Pages;
namespace UnicontaClient.Pages.CustomPage
{
    public class NotificationSubscribeGrid : CorasauDataGridClient
    {
        public override Type TableType { get { return typeof(NotificationSubscribeClient); } }
        public override bool Readonly { get { return false; } }
    }

    public partial class NotificationSubscribePage : GridBasePage
    {

        public NotificationSubscribePage(BaseAPI API) : base(API, string.Empty)
        {
            Init(null);
        }
        public NotificationSubscribePage(UnicontaBaseEntity master) : base(master)
        {
            Init(master);
        }
        UnicontaBaseEntity master;
        private void Init(UnicontaBaseEntity master)
        {
            InitializeComponent();
            this.master = master;
            if (master != null)
                dgNotificationSubscribe.UpdateMaster(master);
            dgNotificationSubscribe.api = api;
            dgNotificationSubscribe.BusyIndicator = busyIndicator;
            SetRibbonControl(localMenu, dgNotificationSubscribe);
            localMenu.OnItemClicked += localMenu_OnItemClicked;
        }
        protected override void OnLayoutLoaded()
        {
            base.OnLayoutLoaded();
            if (master != null)
                Uid.Visible = Uid.ShowInColumnChooser = UserName.Visible = UserName.ShowInColumnChooser = LoginId.Visible = LoginId.ShowInColumnChooser = false;
        }

        private void localMenu_OnItemClicked(string ActionType)
        {
            var selectedItem = dgNotificationSubscribe.SelectedItem as NotificationSubscribeClient;
            switch (ActionType)
            {
                case "DeleteRow":
                    if (selectedItem != null)
                        dgNotificationSubscribe.DeleteRow();
                    break;
                default:
                    gridRibbon_BaseActions(ActionType);
                    break;
            }
        }
    }
}
